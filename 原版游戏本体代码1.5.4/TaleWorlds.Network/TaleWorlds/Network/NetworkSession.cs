using System;

namespace TaleWorlds.Network;

public abstract class NetworkSession
{
	public delegate void ComponentMessageHandlerDelegate(NetworkMessage networkMessage);

	public const double AliveMessageIntervalInSecs = 5.0;

	private MessageContractHandlerManager _messageContractHandlerManager;

	private TcpSocket _socket;

	public bool IsActive => Socket != null;

	internal TcpSocket Socket
	{
		get
		{
			return _socket;
		}
		set
		{
			_socket = value;
			OnSocketSet();
		}
	}

	public string Address => Socket.IPAddress;

	public int LastMessageSentTime => Socket.LastMessageSentTime;

	public bool IsConnected
	{
		get
		{
			if (Socket != null)
			{
				return Socket.IsConnected;
			}
			return false;
		}
	}

	protected NetworkSession()
	{
		_messageContractHandlerManager = new MessageContractHandlerManager();
	}

	public void SendDisconnectMessage()
	{
		Socket.SendDisconnectMessage();
	}

	protected internal virtual void OnConnected()
	{
	}

	protected internal virtual void OnSocketSet()
	{
	}

	protected internal virtual void OnDisconnected()
	{
	}

	protected internal virtual void OnCantConnect()
	{
	}

	protected internal virtual void OnMessageReceived(INetworkMessageReader networkMessage)
	{
	}

	public virtual void Tick()
	{
	}

	internal void HandleMessage(MessageContract messageContract)
	{
		_messageContractHandlerManager.HandleMessage(messageContract);
	}

	public void AddMessageHandler<T>(MessageContractHandlerDelegate<T> handler) where T : MessageContract
	{
		_messageContractHandlerManager.AddMessageHandler(handler);
	}

	internal Type GetMessageContractType(byte id)
	{
		return _messageContractHandlerManager.GetMessageContractType(id);
	}

	internal bool ContainsMessageHandler(byte id)
	{
		return _messageContractHandlerManager.ContainsMessageHandler(id);
	}

	public void SendMessage(MessageContract message)
	{
		if (IsActive)
		{
			NetworkMessage networkMessage = NetworkMessage.CreateForWriting();
			networkMessage.BeginWrite();
			networkMessage.Write(message);
			networkMessage.FinalizeWrite();
			int dataLength = networkMessage.DataLength;
			networkMessage.DataLength = dataLength;
			networkMessage.UpdateHeader();
			Socket.SendMessage(networkMessage.MessageBuffer);
		}
	}

	protected void SendPlainMessage(MessageContract message)
	{
		NetworkMessage networkMessage = NetworkMessage.CreateForWriting();
		networkMessage.BeginWrite();
		networkMessage.Write(message);
		networkMessage.FinalizeWrite();
		networkMessage.UpdateHeader();
		Socket.SendMessage(networkMessage.MessageBuffer);
	}
}
