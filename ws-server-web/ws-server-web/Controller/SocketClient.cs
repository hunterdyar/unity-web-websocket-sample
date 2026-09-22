using System.Net.WebSockets;
using ws_server_web.Datashare;
using ws_server_web.Models;

namespace ws_server_web;

public class SocketClient : WebSocketController
{
	private static Action<byte[], string> OnEvent;
	private static readonly byte[] ConfirmChangePacket = new []{ (byte)MessageType.ChangeConfirm };

	//this datastore needs to get initialized in program and a reference injected to here.
	private ListDataStore<byte[]> _dataStore;
	private string storeID;
	public SocketClient(WebSocket socket, string storeID) : base(socket)
	{
		//configure actions/events. ours and the datastore.
		OnEvent+= OnEventFromAnyClient;
		
		if (DataStoreHub.TryGetDataStore(storeID, out ListDataStore<byte[]> ds))
		{
			_dataStore = ds;
			_dataStore.OnItemAdded += OnItemAddedFromOtherClient;
			_dataStore.OnItemRemoved += OnItemRemovedFromOtherClient;
			_dataStore.OnItemChanged += OnItemChangedFromOtherClient;
			_dataStore.OnAllClientsUpdateAll += OnAllClientsUpdateAll;
			_dataStore.OnClear += OnClear;
			this.storeID = storeID;
		}
		else
		{
			Console.Error.WriteLine($"Error, bad store id {storeID}");
		}
	}

	private async void OnEventFromAnyClient(byte[] data, string client)
	{
		if (client != ClientID)
		{
			//note: byte 0 is the 'event' message type. We assume this and pass along.
			await Send(data);
		}
	}

	private async void OnAllClientsUpdateAll()
	{
		await SendAllData();
	}

	private async void OnClear(string client)
	{
		if (client == ClientID)
		{
			return;
		}

		await Send(new []{(byte)MessageType.Clear});
	}

	private async void OnItemAddedFromOtherClient(uint itemID, byte[] data, string client)
	{
		//we added this! ignore!
		if (client == ClientID)
		{
			return;
		}

		//todo: the first 4 bytes should be the id, and we should save it. (uint is 32 bits = 4 bytes)
		var packet = new byte[data.Length + 5];
		packet[0] = (byte)MessageType.Add; //set message
		BitConverter.GetBytes(itemID).CopyTo(packet, 1); //set id.
		data.CopyTo(packet, 5); //copy the rest over after the id.
		await Send(packet);
	}

	private async void OnItemChangedFromOtherClient(uint itemID, byte[] data, string client)
	{
		//we added this! ignore!
		if (client == ClientID)
		{
			return;
		}
		
		var packet = new byte[data.Length + 5];
		packet[0] = (byte)MessageType.Change; //set message
		BitConverter.GetBytes(itemID).CopyTo(packet, 1); //set id.
		data.CopyTo(packet, 5); //copy the rest.
		await Send(packet);
	}

	private async void OnItemRemovedFromOtherClient(uint itemID, string client)
	{
		if (client == ClientID)
		{
			return;
		}

		var packet = new byte[5];
		packet[0] = (byte)MessageType.Remove; //set message
		BitConverter.GetBytes(itemID).CopyTo(packet, 1); //set id.
		await Send(packet);
	}
	
	protected override async Task OnReceive(byte[] data)
	{
		if (data.Length == 0)
		{
			return;
		}
		var messageType = (MessageType)data[0];

		switch (messageType)
		{
			case MessageType.Echo:
				await Send(data);
				break;
			case MessageType.Add:
				//remove instruction byte and add.
				var message = new byte[data.Length - 1];
				Array.ConstrainedCopy(data, 1, message, 0, data.Length - 1);
				var id = _dataStore.AddItem(message, ClientID);
				var packet = new byte[5];
				BitConverter.GetBytes(id).CopyTo(packet, 1);
				packet[0] = (byte)MessageType.IDReply;
				await Send(packet);
				break;
			case MessageType.Change:
				//[change][id][newData]
				var idbytes = new ArraySegment<byte>(data, 1, 4);
				id = BitConverter.ToUInt32(idbytes);
				message = new byte[data.Length - 5];
				Array.ConstrainedCopy(data, 5, message, 0, data.Length - 5);
				_dataStore.ChangeItem(id,message, ClientID);
				await Send(ConfirmChangePacket);
				break;
			case MessageType.Remove:
				idbytes = new ArraySegment<byte>(data, 1, 4);
				id = BitConverter.ToUInt32(idbytes);
				_dataStore.RemoveItem(id,ClientID);
				break;
			case MessageType.GetAll:
				//asked for all data. reply with all data.
				await SendAllData();
				break;
			case MessageType.Clear:
				_dataStore.Clear(ClientID);
				break;
			case MessageType.Event:
				OnEvent?.Invoke(data,ClientID);
				break;
		}
	}


	private async Task SendAllData()
	{
		if (_dataStore == null)
		{
			Console.WriteLine("data store null? shit!");
			return;
		}
		var allDataSet = _dataStore.GetAllRawData();
		List<byte> allData = new List<byte>();
		allData.Add((byte)MessageType.GetAll);
		foreach (var item in allDataSet)
		{
			allData.AddRange(BitConverter.GetBytes(item.Item1));
			allData.AddRange(item.Item2);
		}

		await Send(allData.ToArray());
	}

	protected override void OnHandleStart()
	{
		DataStoreHub.ConnectionDelta(storeID,1);
		base.OnHandleStart();
	}

	protected override void OnHandleEnd()
	{
		DataStoreHub.ConnectionDelta(storeID, -1);
		base.OnHandleEnd();
	}
}