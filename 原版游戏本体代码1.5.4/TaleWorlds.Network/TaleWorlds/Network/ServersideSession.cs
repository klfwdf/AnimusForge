namespace TaleWorlds.Network;

public abstract class ServersideSession : NetworkSession
{
	public int Index { get; internal set; }

	internal ServersideSessionManager Server { get; private set; }

	protected ServersideSession(ServersideSessionManager server)
	{
		Server = server;
	}

	protected internal override void OnDisconnected()
	{
	}

	protected internal override void OnConnected()
	{
	}

	protected internal override void OnSocketSet()
	{
	}

	internal void InitializeSocket(int id, TcpSocket socket)
	{
		Index = id;
		base.Socket = socket;
		base.Socket.MessageReceived += OnTcpSocketMessageReceived;
		base.Socket.Closed += OnTcpSocketClosed;
	}

	private void OnTcpSocketMessageReceived(MessageBuffer messageBuffer)
	{
		IncomingServerSessionMessage incomingServerSessionMessage = new IncomingServerSessionMessage();
		incomingServerSessionMessage.Peer = this;
		incomingServerSessionMessage.NetworkMessage = NetworkMessage.CreateForReading(messageBuffer);
		Server.AddIncomingMessage(incomingServerSessionMessage);
	}

	private void OnTcpSocketClosed()
	{
		Server.AddDisconnectedPeer(this);
	}
}
