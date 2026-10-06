namespace TaleWorlds.Core;

public abstract class GameManagerComponent : IEntityComponent
{
	public GameManagerBase GameManager { get; internal set; }

	void IEntityComponent.OnInitialize()
	{
		OnInitialize();
	}

	protected virtual void OnInitialize()
	{
	}

	void IEntityComponent.OnFinalize()
	{
		OnFinalize();
	}

	protected virtual void OnFinalize()
	{
	}

	protected internal virtual void OnTick()
	{
	}

	protected internal virtual void OnPlayerDisconnect(VirtualPlayer peer)
	{
	}

	protected internal virtual void OnEarlyPlayerConnect(VirtualPlayer peer)
	{
	}

	protected internal virtual void OnPlayerConnect(VirtualPlayer peer)
	{
	}

	protected internal virtual void OnGameNetworkBegin()
	{
	}

	protected internal virtual void OnGameNetworkEnd()
	{
	}
}
