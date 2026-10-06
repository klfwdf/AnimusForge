using System;
using System.Collections.Concurrent;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Network;

public abstract class ClientsideSession : NetworkSession
{
	private bool _connectionResultHandled;

	private Thread _thread;

	private ConcurrentQueue<MessageBuffer> _incomingMessages;

	private bool _useSessionThread;

	public int Port { get; set; }

	protected void SendMessagePeerAlive()
	{
		base.Socket.SendPeerAliveMessage();
	}

	protected internal override void OnDisconnected()
	{
	}

	protected ClientsideSession()
	{
		_incomingMessages = new ConcurrentQueue<MessageBuffer>();
	}

	public virtual void Connect(string ip, int port, bool useSessionThread = true)
	{
		_useSessionThread = useSessionThread;
		base.Socket = new TcpSocket();
		Port = port;
		base.Socket.MessageReceived += OnSocketMessageReceived;
		base.Socket.Connect(ip, port);
		if (_useSessionThread)
		{
			_thread = new Thread(Process);
			_thread.IsBackground = true;
			_thread.Name = string.Concat(this, " - Client Thread");
			_thread.Start();
		}
	}

	private void OnSocketMessageReceived(MessageBuffer messageBuffer)
	{
		_incomingMessages.Enqueue(messageBuffer);
	}

	public void Process()
	{
		while (ProcessTick())
		{
			Thread.Sleep(1);
		}
	}

	private bool ProcessTick()
	{
		base.Socket.ProcessWrite();
		base.Socket.ProcessRead();
		if (base.Socket != null && base.Socket.IsConnected && (double)((Environment.TickCount - base.Socket.LastMessageSentTime) / 1000) > 5.0)
		{
			SendMessagePeerAlive();
		}
		if (base.Socket.Status == TcpStatus.SocketClosed || base.Socket.Status == TcpStatus.ConnectionClosed)
		{
			return false;
		}
		return true;
	}

	public override void Tick()
	{
		if (base.Socket == null)
		{
			return;
		}
		if (base.Socket.IsConnected)
		{
			if (!_connectionResultHandled)
			{
				Debug.Print("Client connected! Connection result handle begin.");
				OnConnected();
				_connectionResultHandled = true;
			}
			if (!_useSessionThread)
			{
				ProcessTick();
			}
			int count = _incomingMessages.Count;
			for (int i = 0; i < count; i++)
			{
				MessageBuffer result = null;
				_incomingMessages.TryDequeue(out result);
				NetworkMessage networkMessage = NetworkMessage.CreateForReading(result);
				networkMessage.BeginRead();
				byte b = networkMessage.ReadByte();
				Type messageContractType = GetMessageContractType(b);
				MessageContract messageContract = MessageContract.CreateMessageContract(messageContractType);
				Debug.Print(string.Concat("Message with id: ", b, " / contract: ", messageContractType, " received from server"));
				messageContract.DeserializeFromNetworkMessage(networkMessage);
				HandleMessage(messageContract);
			}
		}
		else if (base.Socket.Status == TcpStatus.ConnectionClosed)
		{
			if (!_connectionResultHandled)
			{
				Debug.Print("ClientTcpSession can't connect!");
				_connectionResultHandled = true;
				OnCantConnect();
			}
			else
			{
				Debug.Print("Peer disconnected!");
				OnDisconnected();
			}
			base.Socket.Close();
			_connectionResultHandled = false;
		}
	}
}
