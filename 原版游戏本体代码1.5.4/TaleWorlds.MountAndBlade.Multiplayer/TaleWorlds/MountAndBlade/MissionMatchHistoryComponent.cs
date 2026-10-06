using System;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Multiplayer.NetworkComponents;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class MissionMatchHistoryComponent : MissionNetwork
{
	private bool _recordedHistory;

	private MatchHistoryData _matchHistoryData;

	public static MissionMatchHistoryComponent CreateIfConditionsAreMet()
	{
		BaseNetworkComponent networkComponent = GameNetwork.GetNetworkComponent<BaseNetworkComponent>();
		if ((networkComponent == null || networkComponent.ClientIntermissionState == MultiplayerIntermissionState.Idle || NetworkMain.GameClient.IsInGame) && NetworkMain.GameClient.LastBattleIsOfficial)
		{
			return new MissionMatchHistoryComponent();
		}
		Debug.Print($"Failed to create {typeof(MissionMatchHistoryComponent).Name}. NetworkMain.GameClient.IsInGame: {NetworkMain.GameClient.IsInGame}, NetworkMain.GameClient.LastBattleIsOfficial: {NetworkMain.GameClient.LastBattleIsOfficial}");
		return null;
	}

	private MissionMatchHistoryComponent()
	{
		_recordedHistory = false;
		if (MultiplayerLocalDataManager.Instance.MatchHistory.TryGetHistoryData(NetworkMain.GameClient.CurrentMatchId, out var historyData))
		{
			_matchHistoryData = historyData;
		}
		else
		{
			_matchHistoryData = new MatchHistoryData();
			_matchHistoryData.MatchId = NetworkMain.GameClient.CurrentMatchId;
		}
		_matchHistoryData.MatchDate = DateTime.Now;
	}

	private static void PrintDebugLog(string text)
	{
		Debug.Print("[MATCH_HISTORY_COMPONTENT]: " + text, 0, Debug.DebugColor.Yellow);
	}

	public override void OnBehaviorInitialize()
	{
		MultiplayerGameType multiplayerGameType = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>()?.GameType ?? MultiplayerGameType.TeamDeathmatch;
		_matchHistoryData.GameType = multiplayerGameType.ToString();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
		string strValue2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
		string strValue3 = MultiplayerOptions.OptionType.Map.GetStrValue();
		_matchHistoryData.Faction1 = strValue;
		_matchHistoryData.Faction2 = strValue2;
		_matchHistoryData.Map = strValue3;
		MissionPeer.OnTeamChanged += TeamChange;
		base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_matchHistoryData.MatchType = BannerlordNetwork.LobbyMissionType.ToString();
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		registerer.RegisterBaseHandler<MissionStateChange>(HandleServerEventMissionStateChange);
	}

	private void TeamChange(NetworkCommunicator player, Team oldTeam, Team nextTeam)
	{
		_matchHistoryData.AddOrUpdatePlayer(player.VirtualPlayer.Id.ToString(), player.VirtualPlayer.UserName, player.ForcedAvatarIndex, (int)nextTeam.Side);
	}

	private void HandleServerEventMissionStateChange(GameNetworkMessage baseMessage)
	{
		if (((MissionStateChange)baseMessage).CurrentState != MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			return;
		}
		PrintDebugLog("Received mission ending message from server");
		if (_recordedHistory)
		{
			return;
		}
		PrintDebugLog("Match history is eligible for recording after end message");
		MissionScoreboardComponent missionBehavior = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
		if (missionBehavior != null && !missionBehavior.IsOneSided)
		{
			int roundScore = missionBehavior.GetRoundScore(BattleSideEnum.Attacker);
			int roundScore2 = missionBehavior.GetRoundScore(BattleSideEnum.Defender);
			BattleSideEnum matchWinnerSide = missionBehavior.GetMatchWinnerSide();
			_matchHistoryData.WinnerTeam = (int)matchWinnerSide;
			_matchHistoryData.AttackerScore = roundScore;
			_matchHistoryData.DefenderScore = roundScore2;
			MissionScoreboardComponent.MissionScoreboardSide[] sides = missionBehavior.Sides;
			for (int i = 0; i < sides.Length; i++)
			{
				foreach (MissionPeer player in sides[i].Players)
				{
					_matchHistoryData.TryUpdatePlayerStats(player.Peer.Id.ToString(), player.KillCount, player.DeathCount, player.AssistCount);
				}
			}
		}
		MultiplayerLocalDataManager.Instance.MatchHistory.AddEntry(_matchHistoryData);
		_recordedHistory = true;
		PrintDebugLog("Recorded match history after end message");
	}

	public override void OnRemoveBehavior()
	{
		PrintDebugLog("Removing match history behavior");
		if (!_recordedHistory)
		{
			PrintDebugLog("Match history was eligible for recording when removing behavior");
			_matchHistoryData.WinnerTeam = -1;
			MissionScoreboardComponent missionBehavior = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
			if (missionBehavior != null && !missionBehavior.IsOneSided)
			{
				int roundScore = missionBehavior.GetRoundScore(BattleSideEnum.Attacker);
				int roundScore2 = missionBehavior.GetRoundScore(BattleSideEnum.Defender);
				_matchHistoryData.AttackerScore = roundScore;
				_matchHistoryData.DefenderScore = roundScore2;
				MissionScoreboardComponent.MissionScoreboardSide[] sides = missionBehavior.Sides;
				for (int i = 0; i < sides.Length; i++)
				{
					foreach (MissionPeer player in sides[i].Players)
					{
						_matchHistoryData.TryUpdatePlayerStats(player.Peer.Id.ToString(), player.KillCount, player.DeathCount, player.AssistCount);
					}
				}
			}
			MultiplayerLocalDataManager.Instance.MatchHistory.AddEntry(_matchHistoryData);
			PrintDebugLog("Recorded match history after removing behavior");
			_recordedHistory = true;
		}
		MissionPeer.OnTeamChanged -= TeamChange;
		base.OnRemoveBehavior();
	}
}
