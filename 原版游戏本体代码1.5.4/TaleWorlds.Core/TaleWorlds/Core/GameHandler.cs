namespace TaleWorlds.Core;

public abstract class GameHandler : IEntityComponent
{
	void IEntityComponent.OnInitialize()
	{
		OnInitialize();
	}

	void IEntityComponent.OnFinalize()
	{
		OnFinalize();
	}

	protected virtual void OnInitialize()
	{
	}

	protected virtual void OnFinalize()
	{
	}

	protected internal virtual void OnTick(float dt)
	{
	}

	protected internal virtual void OnGameStart()
	{
	}

	protected internal virtual void OnGameEnd()
	{
	}

	protected internal virtual void OnGameNetworkBegin()
	{
	}

	protected internal virtual void OnGameNetworkEnd()
	{
	}

	protected internal virtual void OnEarlyPlayerConnect(VirtualPlayer peer)
	{
	}

	protected internal virtual void OnPlayerConnect(VirtualPlayer peer)
	{
	}

	protected internal virtual void OnPlayerDisconnect(VirtualPlayer peer)
	{
	}

	public abstract void OnBeforeSave();

	public abstract void OnAfterSave();
}
