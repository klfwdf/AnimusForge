namespace TaleWorlds.MountAndBlade;

public interface IGameNetworkHandler
{
	void OnNewPlayerConnect(PlayerConnectionInfo playerConnectionInfo, NetworkCommunicator networkPeer);

	void OnInitialize();

	void OnPlayerConnectedToServer(NetworkCommunicator peer);

	void OnPlayerDisconnectedFromServer(NetworkCommunicator peer);

	void OnDisconnectedFromServer();

	void OnStartMultiplayer();

	void OnStartReplay();

	void OnEndMultiplayer();

	void OnEndReplay();

	void OnHandleConsoleCommand(string command);
}
