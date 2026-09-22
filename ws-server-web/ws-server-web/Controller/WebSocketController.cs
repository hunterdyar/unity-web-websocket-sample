using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace ws_server_web;

//this class takes a socket type, opens and watches it.
//we will extend it in SocketClient.cs, then overwrite the interesting parts to do the specific logic for the application
//while this has the generic logic for staying connection
public class WebSocketController
{
	protected WebSocket _webSocket;
	protected string ClientID;
	protected ArraySegment<byte> Buffer;
	public WebSocketController(WebSocket webSocket)
	{
		_webSocket = webSocket;
		ClientID = Guid.NewGuid().ToString();
		
		//the buffer needs to be large enough to hold the largest data that will be sent at once. I have no clue what that will be!
		//utf8 is basically 8 bits per character, so this should be plenty, and then 32 times that plenty.
		Buffer = new ArraySegment<byte>(new byte[8*1024*32]);
	}
	public async Task Handle()
	{
		OnHandleStart();
		//loop that keeps the connection open
		while (_webSocket.State == WebSocketState.Open)
		{
			var receiveResult = await _webSocket.ReceiveAsync(Buffer, CancellationToken.None);
			if (receiveResult.EndOfMessage)
			{
				var data = Buffer.Slice(0, receiveResult.Count).ToArray();
				var content = System.Text.Encoding.UTF8.GetString(data);
				await OnReceive(content);
			}

			if (receiveResult.MessageType == WebSocketMessageType.Close)
			{
				await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"client requested",CancellationToken.None);
			}
		}

		OnHandleEnd();
	}

	protected virtual void OnHandleStart()
	{
	}

	protected virtual void OnHandleEnd()
	{
	}

	protected virtual async Task OnReceive(string data)
	{
		Console.WriteLine("Received" + data.Length);
	}

	public async Task Send(string packet)
	{
		// var data = new ArraySegment<byte>(packet);
		if (_webSocket.State != WebSocketState.Open)
		{
			return;
		}
		
		await _webSocket.SendAsync(System.Text.Encoding.UTF8.GetBytes(packet), WebSocketMessageType.Text, true, CancellationToken.None);
	}
}