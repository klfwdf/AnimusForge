using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Multiplayer.NetworkComponents;

namespace TaleWorlds.MountAndBlade;

public class GameNetworkHandler : IGameNetworkHandler
{
	void IGameNetworkHandler.OnNewPlayerConnect(PlayerConnectionInfo playerConnectionInfo, NetworkCommunicator networkPeer)
	{
		if (networkPeer != null)
		{
			GameManagerBase.Current.OnPlayerConnect(networkPeer.VirtualPlayer);
		}
	}

	void IGameNetworkHandler.OnInitialize()
	{
		MultiplayerGameTypes.Initialize();
	}

	void IGameNetworkHandler.OnPlayerConnectedToServer(NetworkCommunicator networkPeer)
	{
		if (Mission.Current == null)
		{
			return;
		}
		foreach (MissionBehavior missionBehavior in Mission.Current.MissionBehaviors)
		{
			if (missionBehavior is MissionNetwork missionNetwork)
			{
				missionNetwork.OnPlayerConnectedToServer(networkPeer);
			}
		}
	}

	void IGameNetworkHandler.OnDisconnectedFromServer()
	{
		if (Mission.Current != null)
		{
			BannerlordNetwork.EndMultiplayerLobbyMission();
		}
	}

	void IGameNetworkHandler.OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
		GameManagerBase.Current.OnPlayerDisconnect(networkPeer.VirtualPlayer);
		if (Mission.Current == null)
		{
			return;
		}
		foreach (MissionBehavior missionBehavior in Mission.Current.MissionBehaviors)
		{
			if (missionBehavior is MissionNetwork missionNetwork)
			{
				missionNetwork.OnPlayerDisconnectedFromServer(networkPeer);
			}
		}
	}

	void IGameNetworkHandler.OnStartMultiplayer()
	{
		GameNetwork.AddNetworkComponent<BaseNetworkComponentData>();
		GameNetwork.AddNetworkComponent<BaseNetworkComponent>();
		GameNetwork.AddNetworkComponent<LobbyNetworkComponent>();
		GameNetwork.AddNetworkComponent<MultiplayerPermissionHandler>();
		GameNetwork.AddNetworkComponent<NetworkStatusReplicationComponent>();
		GameManagerBase.Current.OnGameNetworkBegin();
	}

	void IGameNetworkHandler.OnEndMultiplayer()
	{
		GameManagerBase.Current.OnGameNetworkEnd();
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<LobbyNetworkComponent>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<NetworkStatusReplicationComponent>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<MultiplayerPermissionHandler>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<BaseNetworkComponent>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<BaseNetworkComponentData>());
	}

	void IGameNetworkHandler.OnStartReplay()
	{
		GameNetwork.AddNetworkComponent<BaseNetworkComponentData>();
		GameNetwork.AddNetworkComponent<BaseNetworkComponent>();
		GameNetwork.AddNetworkComponent<LobbyNetworkComponent>();
	}

	void IGameNetworkHandler.OnEndReplay()
	{
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<LobbyNetworkComponent>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<BaseNetworkComponent>());
		GameNetwork.DestroyComponent(GameNetwork.GetNetworkComponent<BaseNetworkComponentData>());
	}

	void IGameNetworkHandler.OnHandleConsoleCommand(string command)
	{
		DedicatedServerConsoleCommandManager.HandleConsoleCommand(command);
	}
}
