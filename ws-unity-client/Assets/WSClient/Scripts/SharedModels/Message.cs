using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

//i copied and pasted this file from the server. The important part is that we get the same data after deserializing the text.
//but we can be fancy (efficient) about it if we know there is data to ignore, etc.
namespace WSClientSample
{
	public enum MessageType
	{
		GetClientData, //the client is asking for names.
		GetServerData,
		SetClient,
		ClientSendsUpdate,
		ServerUpdate,
		ClientRemoved,

		//new client will just be 'client sends update' with a new id. not as robust or debug/loggable... but yeah
		Event,
		Error,
	}

//A message is what we send all over.
	[System.Serializable]
	public class Message
	{
		[JsonInclude] public MessageType type = MessageType.Error;

		[JsonInclude] public string clientID = "";

		[JsonInclude] public ClientData? ClientData { get; set; }

		[JsonInclude]
		public ServerData? ServerData { get; set; }

	[JsonInclude]
		public string Data = "";
		
		public string ToJson()
		{
			var d= JsonSerializer.Serialize(this);
			return d;
		}

		public static Message GetFromJson(string json)
		{
			var message = JsonSerializer.Deserialize<Message>(json);
			//replace our data with the received data, without replacing this object (references to this stay alive)
			if (message != null)
			{
				return message;
			}
			else
			{
				Debug.Log("Failed to deserialize client data");
				return new Message()
				{
					type = MessageType.Error,
					Data = "Failed to deserialize client data"
				};
			}
		}
	}
}