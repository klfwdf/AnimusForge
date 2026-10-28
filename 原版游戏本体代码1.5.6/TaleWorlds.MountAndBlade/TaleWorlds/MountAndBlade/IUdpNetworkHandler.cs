namespace TaleWorlds.MountAndBlade;

public interface IUdpNetworkHandler
{
	void OnUdpNetworkHandlerClose();

	void OnUdpNetworkHandlerTick(float dt);

	void HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo);

	void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer);

	void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer);

	void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer);

	void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer);

	void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer);

	void HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer);

	void HandlePlayerDisconnect(NetworkCommunicator networkPeer);

	void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer);

	void OnDisconnectedFromServer();

	void OnEveryoneUnSynchronized();
}
