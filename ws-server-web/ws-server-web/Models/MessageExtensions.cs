using System.Text;
using Microsoft.Extensions.Primitives;

namespace ws_server_web.Models;

//just some convenience functions for debugging. 
//the sausage is not made here.
public static class MessageExtensions
{
	public static MessageType GetMessageType(this byte[] message)
	{
		return (MessageType)message[0];
	}
	
	public static string PrettyPrint(this byte[] message)
	{
		StringBuilder sb = new StringBuilder();
		var mt = message.GetMessageType();
		switch (mt)
		{
			case MessageType.Echo:
				sb.Append("Echo");
				break;
			case MessageType.Add:
				sb.Append("Add: ");
				var ar = new ArraySegment<byte>(message);
				var s = ar;
				break;
			case MessageType.Remove:
				sb.Append("Remove");
				break;
			case MessageType.GetAll:
				sb.Append("Get All");
				break;
		}

		return sb.ToString();
	}
}