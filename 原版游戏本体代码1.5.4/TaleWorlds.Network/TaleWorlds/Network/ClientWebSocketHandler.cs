using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.Network;

[Obsolete]
public class ClientWebSocketHandler
{
	public delegate void MessageReceivedDelegate(WebSocketMessage message, ClientWebSocketHandler socket);

	public delegate void OnErrorDelegate(ClientWebSocketHandler sender, Exception ex);

	public delegate Task DisconnectedDelegate(ClientWebSocketHandler sender, bool onDisconnectCommand);

	public delegate Task ConnectedDelegate(ClientWebSocketHandler sender);

	private int _messageSentCursor = -1;

	private int _messageQueueCursor = -1;

	private int _lastReceivedMessage = -1;

	private ConcurrentQueue<WebSocketMessage> _outgoingSocketMessageQueue;

	private ConcurrentQueue<WebSocketMessage> _outgoingSocketMessageLog;

	private int logBufferSize = 100;

	private static object consoleLock = new object();

	private const int sendChunkSize = 256;

	private const int receiveChunkSize = 256;

	private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(30000.0);

	private static ClientWebSocket _webSocket = null;

	public bool IsConnected => _webSocket.State == WebSocketState.Open;

	public event MessageReceivedDelegate MessageReceived;

	public event OnErrorDelegate OnError;

	public event DisconnectedDelegate Disconnected;

	public event ConnectedDelegate Connected;

	public ClientWebSocketHandler()
	{
		_outgoingSocketMessageQueue = new ConcurrentQueue<WebSocketMessage>();
		_outgoingSocketMessageLog = new ConcurrentQueue<WebSocketMessage>();
		Connected += ClientWebSocketConnected;
		_webSocket = new ClientWebSocket();
	}

	public async Task Connect(string uri, string token, List<KeyValuePair<string, string>> headers = null)
	{
		_ = 2;
		try
		{
			if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
			{
				_webSocket.Dispose();
				_webSocket = new ClientWebSocket();
			}
			if (_webSocket.State == WebSocketState.None)
			{
				_webSocket.Options.SetRequestHeader("Authorization", "Bearer " + token);
				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						_webSocket.Options.SetRequestHeader(header.Key, header.Value);
					}
				}
			}
			await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
			if (_webSocket.State == WebSocketState.Open)
			{
				if (this.Connected != null)
				{
					await this.Connected(this);
				}
				Debug.Print("WebSocket connected");
			}
			await Task.WhenAll(Receive(_webSocket), Send(_webSocket));
		}
		catch (Exception ex)
		{
			Console.WriteLine("Exception: {0}", ex);
			this.OnError(this, ex);
		}
		finally
		{
			Console.WriteLine("WebSocket closed.");
		}
	}

	private async Task Receive(ClientWebSocket webSocket)
	{
		ArraySegment<byte> inputSegment = new ArraySegment<byte>(new byte[65536]);
		using MemoryStream ms = new MemoryStream();
		while (webSocket.State == WebSocketState.Open)
		{
			try
			{
				WebSocketReceiveResult webSocketReceiveResult = await webSocket.ReceiveAsync(inputSegment, CancellationToken.None);
				if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					try
					{
						if (this.Disconnected != null)
						{
							await this.Disconnected(this, onDisconnectCommand: true);
						}
						await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Endpoint demanded closure", CancellationToken.None);
						Console.WriteLine("Endpoint demanded closure");
						return;
					}
					catch
					{
						return;
					}
				}
				ms.Write(inputSegment.Array, inputSegment.Offset, webSocketReceiveResult.Count);
				if (webSocketReceiveResult.EndOfMessage)
				{
					ms.GetBuffer();
					ms.Seek(0L, SeekOrigin.Begin);
					WebSocketMessage webSocketMessage = WebSocketMessage.ReadFrom(fromServer: true, ms);
					Console.WriteLine("Message:" + ms.Length + " " + webSocketMessage);
					if (this.MessageReceived != null)
					{
						this.MessageReceived(webSocketMessage, this);
					}
					ms.Seek(0L, SeekOrigin.Begin);
				}
			}
			catch (WebSocketException)
			{
				if (this.Disconnected != null)
				{
					await this.Disconnected(this, onDisconnectCommand: false);
				}
				return;
			}
		}
	}

	private async Task Send(ClientWebSocket webSocket)
	{
		while (webSocket.State == WebSocketState.Open)
		{
			if (_outgoingSocketMessageQueue.Count > 0)
			{
				if (_outgoingSocketMessageQueue.TryDequeue(out var webSocketMessage))
				{
					MemoryStream memoryStream = new MemoryStream();
					webSocketMessage.WriteTo(fromServer: false, memoryStream);
					await webSocket.SendAsync(new ArraySegment<byte>(memoryStream.GetBuffer()), WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);
					_messageSentCursor = webSocketMessage.Cursor;
					AddMessageToBuffer(webSocketMessage);
					Debug.Print("message sent to: " + ((webSocketMessage.MessageInfo.DestinationPostBox != null) ? webSocketMessage.MessageInfo.DestinationPostBox : webSocketMessage.MessageInfo.DestinationClientId.ToString()));
				}
				webSocketMessage = null;
			}
			await Task.Delay(10);
		}
	}

	private void AddMessageToBuffer(WebSocketMessage message)
	{
		_outgoingSocketMessageLog.Enqueue(message);
		while (_outgoingSocketMessageLog.Count > logBufferSize)
		{
			WebSocketMessage result = null;
			_outgoingSocketMessageLog.TryDequeue(out result);
		}
	}

	private void ResetMessageQueueByCursor(int serverCursor)
	{
		while (_outgoingSocketMessageLog.Count > 0)
		{
			WebSocketMessage result = null;
			if (_outgoingSocketMessageLog.TryDequeue(out result) && result.Cursor > serverCursor)
			{
				_outgoingSocketMessageQueue.Enqueue(result);
			}
		}
	}

	private Task ClientWebSocketConnected(ClientWebSocketHandler sender)
	{
		SendCursorMessage();
		return Task.FromResult(0);
	}

	private void SendCursorMessage()
	{
		WebSocketMessage item = WebSocketMessage.CreateCursorMessage(_lastReceivedMessage);
		_outgoingSocketMessageQueue.Enqueue(item);
	}

	public async Task Disconnect(string reason, bool onDisconnectCommand)
	{
		try
		{
			if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting))
			{
				await _webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, reason, CancellationToken.None);
			}
		}
		catch (ObjectDisposedException ex)
		{
			Debug.Print(ex.Message);
		}
		if (this.Disconnected != null)
		{
			await this.Disconnected(this, onDisconnectCommand);
		}
	}

	public void SendTextMessage(string postBoxId, string text)
	{
		_messageQueueCursor++;
		WebSocketMessage webSocketMessage = new WebSocketMessage();
		webSocketMessage.SetTextPayload(text);
		webSocketMessage.Cursor = _messageQueueCursor;
		webSocketMessage.MessageType = MessageTypes.Rest;
		webSocketMessage.MessageInfo.DestinationPostBox = postBoxId;
		_outgoingSocketMessageQueue.Enqueue(webSocketMessage);
	}
}
