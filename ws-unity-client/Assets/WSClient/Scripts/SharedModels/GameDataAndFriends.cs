using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WSClientSample
{
// A single datastore of some type.
	public class GameData
	{
		public ServerData serverData = new ServerData();
		public readonly Dictionary<string, ClientData> clientData = new Dictionary<string, ClientData>(); //each client's current data.

		public Action OnNewClient;
		public Action OnClientChanged;
		public Action OnClientRemoved;
		public Action OnServerChanged;

		public void UpdateServerData(ServerData newServerData)
		{
			serverData = newServerData;
			OnServerChanged?.Invoke();
		}

		//we are conflating "update this data"with "i am a new client". which is potentially buggy, but works if everyone behaves nicely.
		public void SetClientData(string clientID, ClientData clientData)
		{
			if (!this.clientData.ContainsKey(clientID))
			{
				this.clientData.Add(clientID, clientData);
				serverData.ClientIDs.Add(clientID); //server data gets a copy
				OnNewClient?.Invoke();
			}
			else
			{
				this.clientData[clientID] = clientData;
				OnClientChanged?.Invoke();
			}
		}

		//goodbye!
		public void RemoveClient(string clientID)
		{
			if (clientData.ContainsKey(clientID))
			{
				clientData.Remove(clientID);
			}

			serverData.ClientIDs.Remove(clientID); //server data gets a copy
			OnClientRemoved?.Invoke();
		}

		public string GetAllRawData()
		{
			throw new NotImplementedException("downloading all data is not currently implemented. It wouldn't be hard to, just another wrapper class to have clients in a list instead of a dictionary.");
		}
	}


	public enum RoundState
	{
		Idle,
		Playing,
		Review,
		DanceBreak,
		EndCredits,
	}

	[Serializable]
	public class ServerData
	{
		[JsonInclude]
		public List<string> ClientIDs = new List<string>();

		[JsonInclude]
		public List<(uint id, int data)> Scores = new List<(uint id, int data)>();

		[JsonInclude]
		public int Round;

		[JsonInclude]
		public RoundState roundState; //an enum! fancy
	}

	[Serializable]
	public class ClientData
	{
		[JsonInclude]
		public string clientID;

		[JsonInclude]
		public string message;

		[JsonInclude]
		public int favoriteNumber;
	}
}