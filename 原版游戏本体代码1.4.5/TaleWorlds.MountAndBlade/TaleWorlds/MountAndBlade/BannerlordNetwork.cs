using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public static class BannerlordNetwork
{
	public const int DefaultPort = 9999;

	public static LobbyMissionType LobbyMissionType { get; private set; }

	private static PlayerConnectionInfo CreateServerPeerConnectionInfo()
	{
		LobbyClient gameClient = NetworkMain.GameClient;
		PlayerConnectionInfo playerConnectionInfo = new PlayerConnectionInfo(gameClient.PlayerID);
		PlayerData playerData = gameClient.PlayerData;
		playerConnectionInfo.AddParameter("PlayerData", playerData);
		playerConnectionInfo.AddParameter("UsedCosmetics", gameClient.UsedCosmetics);
		playerConnectionInfo.Name = gameClient.Name;
		return playerConnectionInfo;
	}

	public static void CreateServerPeer()
	{
		if (MBCommon.CurrentGameType == MBCommon.GameType.MultiClientServer)
		{
			GameNetwork.AddNewPlayerOnServer(CreateServerPeerConnectionInfo(), serverPeer: true, isAdmin: true);
		}
	}

	public static void StartMultiplayerLobbyMission(LobbyMissionType lobbyMissionType)
	{
		LobbyMissionType = lobbyMissionType;
	}

	public static void EndMultiplayerLobbyMission()
	{
		if (Game.Current.GameStateManager.ActiveState is MissionState { CurrentMission: not null } missionState && !missionState.CurrentMission.MissionEnded)
		{
			if (missionState.CurrentMission.CurrentState != Mission.State.Continuing)
			{
				Debug.Print("Remove From Game: Begin delayed disconnect from server.".ToUpper(), 0, Debug.DebugColor.White, 17179869184uL);
				missionState.BeginDelayedDisconnectFromMission();
			}
			else
			{
				Debug.Print("Remove From Game: Begin instant disconnect from server.".ToUpper(), 0, Debug.DebugColor.White, 17179869184uL);
				missionState.CurrentMission.EndMission();
			}
			MBDebug.Print("Starting to clean up the current mission now.", 0, Debug.DebugColor.White, 17179869184uL);
		}
		Game.Current.GetGameHandler<ChatBox>()?.ResetMuteList();
	}
}
