using System.Text.Json;
using ws_server_web.Datashare;

namespace ws_server_web.Models;

public enum MessageType
{
	GetClientData,//the client is asking for names.
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
[Serializable]
public class Message
{
	public MessageType type = MessageType.Error;
	public string clientID = "";

	public ClientData? ClientData;
	public ServerData? ServerData;
	
	public string ToJson()
	{
		return JsonSerializer.Serialize(this);
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
			Console.Error.WriteLine("Failed to deserialize client data");
			return new Message()
			{
				type = MessageType.Error,
			};
		}
	}
}