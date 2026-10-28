namespace TaleWorlds.MountAndBlade;

public abstract class MissionNetwork : MissionLogic, IUdpNetworkHandler
{
	private GameNetwork.NetworkMessageHandlerRegistererContainer _missionNetworkMessageHandlerRegisterer;

	public override void OnAfterMissionCreated()
	{
		_missionNetworkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegistererContainer();
		AddRemoveMessageHandlers(_missionNetworkMessageHandlerRegisterer);
		_missionNetworkMessageHandlerRegisterer.RegisterMessages();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		GameNetwork.AddNetworkHandler(this);
	}

	public override void OnRemoveBehavior()
	{
		GameNetwork.RemoveNetworkHandler(this);
		base.OnRemoveBehavior();
	}

	protected virtual void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
	}

	public virtual void OnPlayerConnectedToServer(NetworkCommunicator networkPeer)
	{
	}

	public virtual void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.OnUdpNetworkHandlerTick(float dt)
	{
		OnUdpNetworkHandlerTick();
	}

	void IUdpNetworkHandler.OnUdpNetworkHandlerClose()
	{
		OnUdpNetworkHandlerClose();
		_missionNetworkMessageHandlerRegisterer?.UnregisterMessages();
	}

	void IUdpNetworkHandler.HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
		HandleNewClientConnect(clientConnectionInfo);
	}

	void IUdpNetworkHandler.HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		HandleEarlyNewClientAfterLoadingFinished(networkPeer);
	}

	void IUdpNetworkHandler.HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		HandleNewClientAfterLoadingFinished(networkPeer);
	}

	void IUdpNetworkHandler.HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		HandleLateNewClientAfterLoadingFinished(networkPeer);
	}

	void IUdpNetworkHandler.HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		HandleNewClientAfterSynchronized(networkPeer);
	}

	void IUdpNetworkHandler.HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		HandleLateNewClientAfterSynchronized(networkPeer);
	}

	void IUdpNetworkHandler.HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
		HandleEarlyPlayerDisconnect(networkPeer);
	}

	void IUdpNetworkHandler.HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		HandlePlayerDisconnect(networkPeer);
	}

	void IUdpNetworkHandler.OnEveryoneUnSynchronized()
	{
	}

	void IUdpNetworkHandler.OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.OnDisconnectedFromServer()
	{
	}

	protected virtual void OnUdpNetworkHandlerTick()
	{
	}

	protected virtual void OnUdpNetworkHandlerClose()
	{
	}

	protected virtual void HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
	}

	protected virtual void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}

	protected virtual void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}
}
