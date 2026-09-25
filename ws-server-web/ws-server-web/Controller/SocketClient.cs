using System.Net.WebSockets;
using System.Text;
using ws_server_web.Datashare;
using ws_server_web.Models;

namespace ws_server_web;

public class SocketClient : WebSocketController
{
	private static Action<string, string> OnEvent;

	//this datastore needs to get initialized in program and a reference injected to here.
	private GameData _gameData;
	private string storeID;
	public SocketClient(WebSocket socket, string storeID) : base(socket)
	{
		//configure actions/events. ours and the datastore.
		OnEvent+= OnEventFromAnyClient;
		
		if (DataStoreHub.TryGetDataStore(storeID, out GameData gameData))
		{
			_gameData = gameData;
			_gameData.OnQuestionUpdated += OnQuestionUpdated;
			this.storeID = storeID;
		}
		else
		{
			Console.Error.WriteLine($"Error, bad store id {storeID}");
		}
	}

	private void OnQuestionUpdated()
	{
		//check that I am not the one that just sent it by comparing question ID;'s....
		Send(new Message()
		{
			type = MessageType.SendQuestionToAllClients,
			Data = _gameData.CurrentQuestion
		}.ToJson());
	}

	private async void OnEventFromAnyClient(string data, string client)
	{
		//WE are clientID, so skip replying to ourselves.
		if (client != ClientID)
		{
			await Send(data);
		}
	}
	
	protected override async Task OnReceive(string data)
	{
		if (data.Length == 0)
		{
			return;
		}

		var m = Message.GetFromJson(data);
		if (m.type == MessageType.Error)
		{
			//give up? report? crash? idk
			return;
		}
		
		//remember, this is data coming from the clients to the server. The action hooks (await Send()) are how we send data out to clients.
		switch (m.type)
		{
			case MessageType.SendQuestionToAllClients:
				//why did i receive this? something went wrong?
				break;
			case MessageType.UpdateTheQuestion:
				//this updates gamedata. gamedata invokes an event that this class (and the other clients that are instances of it)
				//and then they SendQuestionToAllClients.
				_gameData.UpdateQuestion(m.Data);
				break;
		}
	}

	protected override void OnHandleStart()
	{
		DataStoreHub.ConnectionDelta(storeID,1);
		//we should get an ID that is for-sure unique and give it to the client. instead clients are generating their own.
		//i am sure nothing bad can ever happen.
		base.OnHandleStart();
	}

	protected override void OnHandleEnd()
	{
		DataStoreHub.ConnectionDelta(storeID, -1);
		base.OnHandleEnd();
	}
}