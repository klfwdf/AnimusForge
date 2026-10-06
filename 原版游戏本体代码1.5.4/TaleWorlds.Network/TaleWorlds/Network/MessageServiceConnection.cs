using System.Threading.Tasks;

namespace TaleWorlds.Network;

public abstract class MessageServiceConnection
{
	public delegate Task ClosedDelegate();

	public delegate void StateChangedDelegate(ConnectionState oldState, ConnectionState newState);

	public ConnectionState State;

	public ConnectionState OldState;

	public string Address { get; protected set; }

	public event ClosedDelegate Closed;

	public event StateChangedDelegate StateChanged;

	public MessageServiceConnection()
	{
	}

	public abstract Task SendAsync(string text);

	public abstract void Init(string address, string token);

	public abstract void RegisterProxyClient(string name, IMessageProxyClient playerClient);

	public abstract Task StartAsync();

	public abstract Task StopAsync();

	protected void InvokeClosed()
	{
		this.Closed?.Invoke();
	}

	protected void InvokeStateChanged(ConnectionState oldState, ConnectionState newState)
	{
		State = newState;
		OldState = oldState;
		this.StateChanged?.Invoke(oldState, newState);
	}
}
