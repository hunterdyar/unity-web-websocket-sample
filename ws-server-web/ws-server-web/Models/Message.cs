using System.Text.Json;
using System.Text.Json.Serialization;
using ws_server_web.Datashare;

namespace ws_server_web.Models;

public enum MessageType
{
	UpdateTheQuestion = 0,//to server from manager
	SendQuestionToAllClients = 1,//from server to all the connections
	Error=10,
}

//A message is what we send all over.
[Serializable]
public class Message
{
	[JsonInclude]
	public MessageType type = MessageType.Error;
	[JsonInclude] public string Data = "";
	
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
				Data = "Failed to deserialize client data"
			};
		}
	}
}