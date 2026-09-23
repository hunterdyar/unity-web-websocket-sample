using System;
using System.Collections.Generic;
using System.Linq;
using Connection;
using UnityEngine;
using NativeWebSocket;
using UnityEditor.VersionControl;
using UnityEngine.Assertions;

namespace WSClientSample
{
	//this class will also handle "compacting" the base of operations down to a sampleOp.
	[CreateAssetMenu(fileName = "GameData",menuName = "WS Sample/Game Data (ws synced object)",order = 1)]
	public class GameDataStore : ScriptableObject
	{
		public Action<ConnectionStatus> OnConnectionStatusChanged;
		//Network
		public ConnectionStatus ConnectionStatus = ConnectionStatus.Idle;
		public SocketSettings _socketSettings;
		public string localStatus = "";
		private WebSocket _websocket;

		//this is it! Everything else is just about updating these things!
		public ClientData MyData => _myData;
		private ClientData _myData;
		
		public GameData GameData => _gameData;
		public GameData _gameData = new GameData();
		
		[Tooltip("Time in seconds after connection failure to attempt again.")]
		public int reconnectionDelay = 30;

		public void InitAndConnect()
		{
			_myData = new ClientData();
			//i'm just going to generate some random characters for the id's. whatever, it'll probably be fine. i'm sure nothing will ever randomly conflict >_<
			_myData.clientID = Guid.NewGuid().ToString().Substring(0,8);
			
			if (_socketSettings.connectionURL == "")
			{
				Debug.Log($"Connection URL for {this} is empty. Not bothering to try to connect.");
				return;
			}
			
			SetConnectionStatus(ConnectionStatus = ConnectionStatus.Idle);
			localStatus = "Initialized";
			_websocket = new WebSocket(_socketSettings.connectionURL);
			_socketSettings.AddRecent(_socketSettings.connectionURL);

			_websocket.OnOpen += () =>
			{
				Debug.Log("Connection open!");
				SetConnectionStatus(ConnectionStatus = ConnectionStatus.Connected);
				
				//tell the server we exist. This will act as a 'new client' message
				var m = new Message()
				{
					type = MessageType.SetClient,
					ClientData = _myData,
					clientID = _myData.clientID
				}.ToJson();
				_websocket.SendText(m);
				
			};

			_websocket.OnError += (e) => { Debug.Log("Error! " + e); };

			_websocket.OnClose += (e) =>
			{
				
				Debug.Log($"Connection closed! Code: {e}");
				SetConnectionStatus(ConnectionStatus.Disconnected);
			};

			_websocket.OnMessage += OnReceiveFromServer;

			SetConnectionStatus(ConnectionStatus.AttemptingToConnect);
			_websocket.Connect();
		}

		private void SetConnectionStatus(ConnectionStatus status)
		{
			ConnectionStatus = status;
			OnConnectionStatusChanged?.Invoke(ConnectionStatus);
		}

		public void UpdateClientMessage(string message)
		{
			MyData.message = message;
			if (ConnectionStatus == ConnectionStatus.Connected)
			{
				UpdateClientData(MyData);
			}
			else
			{
				Debug.Log("Trying to send update before connected.");
			}
		}
		public void UpdateClientData(ClientData clientData)
		{
			_myData = clientData;
			//we are storing local truth twice, because... listen, shut up. whatever. it's a sample project don't yell at me.
			_gameData.SetClientData(_myData.clientID, _myData);

			var m = new Message()
			{
				type = MessageType.SetClient,
				clientID = _myData.clientID,
				ClientData = clientData,
			};
			_websocket.SendText(m.ToJson());
		}

		public void AskForServerData()
		{
			var m = new Message()
			{
				type = MessageType.GetServerData,
				clientID = _myData.clientID,
			};
			_websocket.SendText(m.ToJson());
		}
		
		public void GetDataAboutClient(string clientID)
		{
			var m = new Message()
			{
				type = MessageType.GetClientData,
				clientID = clientID,
			};
			_websocket.SendText(m.ToJson());
			//server will reply with 'client sends update'
		}
		
		private void OnReceiveFromServer(byte[] data)
		{
			string text = System.Text.Encoding.UTF8.GetString(data);
			var m = System.Text.Json.JsonSerializer.Deserialize<Message>(text);
			switch (m.type)
			{
				case MessageType.ClientSendsUpdate:
					if (m.clientID == _myData.clientID)
					{
						if (m.clientID == null)
						{
							Debug.LogWarning("bad message from server. ignoring.");
							return;
						}

						//we are ourselves... are we authoratative? if so, ignore. if not, get the update about what the server is telling about us.
						_myData = m.ClientData;
						_gameData.SetClientData(m.clientID, m.ClientData);
						return;
					}
					else
					{
						//we learn about another client. We could have asked for this information via GetClientData.
						_gameData.SetClientData(m.clientID, m.ClientData);
					}
					break;
				case MessageType.ServerUpdate:
					if (m.ServerData == null)
					{
						Debug.LogWarning("Bad message from server. ignoring");
						return;
					}
					_gameData.serverData = m.ServerData;
					break;
				case MessageType.Error:
					Debug.LogError("Error from server: " + m.Data);
					break;
				case MessageType.ClientRemoved:
					if (m.clientID == _myData.clientID)
					{
						//we are removing? ourselves? idk, ignore
						return;
					}
					else
					{
						//we learn about another client, I guess?
						_gameData.RemoveClient(m.clientID);
					}
					break;
				case MessageType.SetClient:
				case MessageType.GetClientData:
				case MessageType.GetServerData:
					//these are messages that WE should be sending THE SERVER, and it should not be sending us!
					Debug.LogWarning("Got ... weird message? from server? identity crisis?");
					break;
			}
		}

		public void UpdateServer(ServerData serverData)
		{
			//uhhh we aren't actually supposed to be able to do this but it's a testing project.
			_websocket.SendText(new Message()
			{
				type = MessageType.ServerUpdate,
				ServerData = serverData,
				clientID = _myData.clientID,
			}.ToJson());
		}
		//Close the connection
		public void Stop()
		{
			//first, let's tell the server that we are piecing out formally and cleanly. 
			_websocket.SendText(new Message()
			{
				type = MessageType.ClientRemoved,
				ClientData = _myData,
			}.ToJson());
			
			localStatus = "Stopped";
			if (_websocket != null)
			{
				_websocket.DispatchMessageQueue();
				_websocket.Close();
			}
			else
			{
				SetConnectionStatus(ConnectionStatus.Disconnected);
			}
		}

		public void DispatchMessageQueue()
		{
			if (_websocket != null)
			{
				_websocket.DispatchMessageQueue();
			}
		}
	}
}