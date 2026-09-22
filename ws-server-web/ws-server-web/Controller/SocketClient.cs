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
			this.storeID = storeID;
		}
		else
		{
			Console.Error.WriteLine($"Error, bad store id {storeID}");
		}
	}

	private async void OnEventFromAnyClient(string data, string client)
	{
		//WE are clientID, so skip replying to ourselves.
		if (client != ClientID)
		{
			await Send(data);
		}
	}

	private async void OnClientChanged(ClientData clientData, string client)
	{
		//it's us! ignore! we are authoritative
		if (client == ClientID)
		{
			return;
		}
		
		//pass the server-updated change (from other client) along to our client (the other device/target/application/unity)

		var m = new Message()
		{
			type = MessageType.ClientSendsUpdate,
			ClientData = clientData,
			clientID = client,//the client to update, in this case.
			ServerData = null,
		};
		
		await Send(m.ToJson());
	}

	private async void OnServerChanged(ServerData serverData)
	{
		//pass along the server-updated change.
		var m = new Message()
		{
			type = MessageType.ServerUpdate,
			ServerData = serverData,
			clientID = ClientID,//let the world know who is passing this along so we don't keep updating ourselves
		};
		await Send(m.ToJson());
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
			case MessageType.GetClientData:
				//client is asking for data. reply with the data.
				string requestedID = m.clientID;
				if (_gameData.clientData.TryGetValue(requestedID, out var clientData))
				{
					await Send(new Message()
					{
						type = MessageType.ClientSendsUpdate,
						ClientData = clientData,
					}.ToJson());
				}
				else
				{
					//can't give you your data :(
					await Send(new Message()
					{
						type = MessageType.Error,
					}.ToJson());
				}
				break;
			case MessageType.GetServerData:
				//i don't need to ask who asked, anybody who wants server data can get it.
				//we reply with the 'server update' message.
				await Send(new Message()
				{
					type = MessageType.ServerUpdate,
					ServerData = _gameData.serverData,
				}.ToJson());
				break;
			case MessageType.ServerUpdate:
				if (m.ServerData == null)
				{
					//invalid message!
					return;
				}

				if (m.clientID == ClientID)
				{
					return;
				}
				//tbh in this model of the game, the server _isn't_ supposed to get updated by clients?
				//what client is telling us server information?
				//but like, we write an admin role? moved game logic to whichever client hit "finish round" first? idk
				_gameData.UpdateServerData(m.ServerData);
				break;
			case MessageType.SetClient:
				//a different client has been changed.
				//if our game model is that the client doesn't know about other clients, we ignore this message.
				//otherwise... we just do the same thing as the ClientSendUpdate case, without the guard check, and pass it along.
				break;
			case MessageType.ClientSendsUpdate:
				//remove instruction byte and add.
				if (ClientID != m.clientID)
				{
					//this client is trying to update someone else! should that be allowed? I don't know, this is a sample!
					_gameData.SetClientData(m.clientID, m.ClientData);

				}
				else
				{
					_gameData.SetClientData(m.clientID, m.ClientData);
				}

				break;
			case MessageType.ClientRemoved:
				if (ClientID != m.clientID)
				{
					_gameData.RemoveClient(m.clientID);
				}
				else
				{
					//we should reply with an error? but for now we will just always do what we are told. hackers, REJOICE
					_gameData.RemoveClient(m.clientID);
				}
				break;
			case MessageType.Event:
				//raw data sent along to all clients. no server data, no nothing.
				//we maybe technically sort of  _ONLY_ need this one to make a working game, it's just weird and messy.
				OnEvent?.Invoke(data,ClientID);
				break;
		}
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