using System;
using System.Threading.Tasks;
using TaleWorlds.Network;

namespace TaleWorlds.Diamond.Socket;

public abstract class ClientSocketSession : ClientsideSession, IClientSession
{
	private string _address;

	private int _port;

	private IClient _client;

	protected ClientSocketSession(IClient client, string address, int port)
	{
		_client = client;
		_address = address;
		_port = port;
		AddMessageHandler<SocketMessage>(HandleSocketMessage);
	}

	private void HandleSocketMessage(SocketMessage socketMessage)
	{
		Message message = socketMessage.Message;
		_client.HandleMessage(message);
	}

	protected override void OnConnected()
	{
		base.OnConnected();
		_client.OnConnected();
	}

	protected override void OnCantConnect()
	{
		base.OnCantConnect();
		_client.OnCantConnect();
	}

	protected override void OnDisconnected()
	{
		base.OnDisconnected();
		_client.OnDisconnected();
	}

	void IClientSession.Connect()
	{
		Connect(_address, _port);
	}

	void IClientSession.Disconnect()
	{
		SendDisconnectMessage();
	}

	Task<TReturn> IClientSession.CallFunction<TReturn>(Message message)
	{
		throw new NotImplementedException();
	}

	void IClientSession.SendMessage(Message message)
	{
		SendMessage(new SocketMessage(message));
	}

	Task<LoginResult> IClientSession.Login(LoginMessage message)
	{
		throw new NotImplementedException();
	}

	Task<bool> IClientSession.CheckConnection()
	{
		return Task.FromResult(result: true);
	}
}
