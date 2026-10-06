namespace TaleWorlds.MountAndBlade;

public abstract class UdpNetworkComponent : IUdpNetworkHandler
{
	private GameNetwork.NetworkMessageHandlerRegistererContainer _missionNetworkMessageHandlerRegisterer;

	protected UdpNetworkComponent()
	{
		_missionNetworkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegistererContainer();
		AddRemoveMessageHandlers(_missionNetworkMessageHandlerRegisterer);
		_missionNetworkMessageHandlerRegisterer.RegisterMessages();
	}

	protected virtual void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
	}

	public virtual void OnUdpNetworkHandlerClose()
	{
		_missionNetworkMessageHandlerRegisterer?.UnregisterMessages();
		GameNetwork.NetworkComponents.Remove(this);
	}

	public virtual void OnUdpNetworkHandlerTick(float dt)
	{
	}

	public virtual void HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
	}

	public virtual void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	public virtual void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	public virtual void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	public virtual void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	public virtual void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	public virtual void OnEveryoneUnSynchronized()
	{
	}

	public void HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}

	public virtual void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}

	public virtual void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
	}

	public virtual void OnDisconnectedFromServer()
	{
	}
}
