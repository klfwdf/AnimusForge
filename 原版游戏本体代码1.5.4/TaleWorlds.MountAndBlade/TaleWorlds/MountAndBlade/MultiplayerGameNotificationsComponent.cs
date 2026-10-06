using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerGameNotificationsComponent : MissionNetwork
{
	private enum MultiplayerNotificationEnum
	{
		[NotificationProperty("str_battle_warmup_ending_in_x_seconds", "event:/ui/mission/multiplayer/lastmanstanding", "")]
		BattleWarmupEnding,
		[NotificationProperty("str_battle_preparation_start", "event:/ui/mission/multiplayer/roundstart", "")]
		BattlePreparationStart,
		[NotificationProperty("str_round_result_win_lose", "event:/ui/mission/multiplayer/victory", "event:/ui/mission/multiplayer/defeat")]
		BattleYouHaveXTheRound,
		[NotificationProperty("str_mp_mission_game_over_draw", "", "")]
		GameOverDraw,
		[NotificationProperty("str_mp_mission_game_over_victory", "", "")]
		GameOverVictory,
		[NotificationProperty("str_mp_mission_game_over_defeat", "", "")]
		GameOverDefeat,
		[NotificationProperty("str_mp_flag_removed", "event:/ui/mission/multiplayer/pointsremoved", "")]
		FlagXRemoved,
		[NotificationProperty("str_sergeant_a_one_flag_remaining", "event:/ui/mission/multiplayer/pointsremoved", "")]
		FlagXRemaining,
		[NotificationProperty("str_sergeant_a_flags_will_be_removed", "event:/ui/mission/multiplayer/pointwarning", "")]
		FlagsWillBeRemoved,
		[NotificationProperty("str_sergeant_a_flag_captured_by_your_team", "event:/ui/mission/multiplayer/pointcapture", "event:/ui/mission/multiplayer/pointlost")]
		FlagXCapturedByYourTeam,
		[NotificationProperty("str_sergeant_a_flag_captured_by_other_team", "event:/ui/mission/multiplayer/pointcapture", "event:/ui/mission/multiplayer/pointlost")]
		FlagXCapturedByOtherTeam,
		[NotificationProperty("str_gold_carried_from_previous_round", "", "")]
		GoldCarriedFromPreviousRound,
		[NotificationProperty("str_player_is_inactive", "", "")]
		PlayerIsInactive,
		[NotificationProperty("str_has_ongoing_poll", "", "")]
		HasOngoingPoll,
		[NotificationProperty("str_too_many_poll_requests", "", "")]
		TooManyPollRequests,
		[NotificationProperty("str_kick_poll_target_not_synced", "", "")]
		KickPollTargetNotSynced,
		[NotificationProperty("str_not_enough_players_to_open_poll", "", "")]
		NotEnoughPlayersToOpenPoll,
		[NotificationProperty("str_player_is_kicked", "", "")]
		PlayerIsKicked,
		[NotificationProperty("str_formation_autofollow_enforced", "", "")]
		FormationAutoFollowEnforced,
		Count
	}

	public static int NotificationCount => 18;

	public void WarmupEnding()
	{
		HandleNewNotification(MultiplayerNotificationEnum.BattleWarmupEnding, 30);
	}

	public void GameOver(Team winnerTeam)
	{
		if (winnerTeam == null)
		{
			HandleNewNotification(MultiplayerNotificationEnum.GameOverDraw);
			return;
		}
		Team syncToTeam = ((winnerTeam.Side == BattleSideEnum.Attacker) ? base.Mission.Teams.Defender : base.Mission.Teams.Attacker);
		HandleNewNotification(MultiplayerNotificationEnum.GameOverVictory, -1, -1, winnerTeam);
		HandleNewNotification(MultiplayerNotificationEnum.GameOverDefeat, -1, -1, syncToTeam);
	}

	public void PreparationStarted()
	{
		HandleNewNotification(MultiplayerNotificationEnum.BattlePreparationStart);
	}

	public void FlagsXRemoved(FlagCapturePoint removedFlag)
	{
		int flagChar = removedFlag.FlagChar;
		HandleNewNotification(MultiplayerNotificationEnum.FlagXRemoved, flagChar);
	}

	public void FlagXRemaining(FlagCapturePoint remainingFlag)
	{
		int flagChar = remainingFlag.FlagChar;
		HandleNewNotification(MultiplayerNotificationEnum.FlagXRemaining, flagChar);
	}

	public void FlagsWillBeRemovedInXSeconds(int timeLeft)
	{
		ShowNotification(MultiplayerNotificationEnum.FlagsWillBeRemoved, timeLeft);
	}

	public void FlagXCapturedByTeamX(SynchedMissionObject flag, Team capturingTeam)
	{
		int param = (flag as FlagCapturePoint)?.FlagChar ?? 65;
		Team syncToTeam = ((capturingTeam.Side == BattleSideEnum.Attacker) ? base.Mission.Teams.Defender : base.Mission.Teams.Attacker);
		HandleNewNotification(MultiplayerNotificationEnum.FlagXCapturedByYourTeam, param, -1, capturingTeam);
		HandleNewNotification(MultiplayerNotificationEnum.FlagXCapturedByOtherTeam, param, -1, syncToTeam);
	}

	public void GoldCarriedFromPreviousRound(int carriedGoldAmount, NetworkCommunicator syncToPeer)
	{
		HandleNewNotification(MultiplayerNotificationEnum.GoldCarriedFromPreviousRound, carriedGoldAmount, -1, null, syncToPeer);
	}

	public void PlayerIsInactive(NetworkCommunicator peer)
	{
		HandleNewNotification(MultiplayerNotificationEnum.PlayerIsInactive, -1, -1, null, peer);
	}

	public void FormationAutoFollowEnforced(NetworkCommunicator peer)
	{
		HandleNewNotification(MultiplayerNotificationEnum.FormationAutoFollowEnforced, -1, -1, null, peer);
	}

	public void PollRejected(MultiplayerPollRejectReason reason)
	{
		switch (reason)
		{
		case MultiplayerPollRejectReason.TooManyPollRequests:
			ShowNotification(MultiplayerNotificationEnum.TooManyPollRequests);
			break;
		case MultiplayerPollRejectReason.HasOngoingPoll:
			ShowNotification(MultiplayerNotificationEnum.HasOngoingPoll);
			break;
		case MultiplayerPollRejectReason.NotEnoughPlayersToOpenPoll:
			ShowNotification(MultiplayerNotificationEnum.NotEnoughPlayersToOpenPoll, 3);
			break;
		case MultiplayerPollRejectReason.KickPollTargetNotSynced:
			ShowNotification(MultiplayerNotificationEnum.KickPollTargetNotSynced);
			break;
		default:
			Debug.FailedAssert(string.Concat("Notification of a PollRejectReason is missing (", reason, ")"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameNotificationsComponent.cs", "PollRejected", 153);
			break;
		}
	}

	public void PlayerKicked(NetworkCommunicator kickedPeer)
	{
		ShowNotification(MultiplayerNotificationEnum.PlayerIsKicked, kickedPeer.Index);
	}

	private void HandleNewNotification(MultiplayerNotificationEnum notification, int param1 = -1, int param2 = -1, Team syncToTeam = null, NetworkCommunicator syncToPeer = null)
	{
		if (syncToPeer != null)
		{
			SendNotificationToPeer(syncToPeer, notification, param1, param2);
		}
		else if (syncToTeam != null)
		{
			SendNotificationToTeam(syncToTeam, notification, param1, param2);
		}
		else
		{
			SendNotificationToEveryone(notification, param1, param2);
		}
	}

	private void ShowNotification(MultiplayerNotificationEnum notification, params int[] parameters)
	{
		if (GameNetwork.IsDedicatedServer)
		{
			return;
		}
		NotificationProperty notificationProperty = (NotificationProperty)notification.GetType().GetField(notification.ToString()).GetCustomAttributesSafe(typeof(NotificationProperty), inherit: false)
			.Single();
		if (notificationProperty != null)
		{
			int[] parameters2 = parameters.Where((int x) => x != -1).ToArray();
			TextObject message = ToNotificationString(notification, notificationProperty, parameters2);
			string soundEventPath = ToSoundString(notification, notificationProperty, parameters2);
			MBInformationManager.AddQuickInformation(message, 0, null, null, soundEventPath);
		}
	}

	private void SendNotificationToEveryone(MultiplayerNotificationEnum message, int param1 = -1, int param2 = -1)
	{
		ShowNotification(message, param1, param2);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new NotificationMessage((int)message, param1, param2));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	private void SendNotificationToPeer(NetworkCommunicator peer, MultiplayerNotificationEnum message, int param1 = -1, int param2 = -1)
	{
		if (peer.IsServerPeer)
		{
			ShowNotification(message, param1, param2);
		}
		else
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new NotificationMessage((int)message, param1, param2));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	private void SendNotificationToTeam(Team team, MultiplayerNotificationEnum message, int param1 = -1, int param2 = -1)
	{
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (!GameNetwork.IsDedicatedServer && missionPeer?.Team != null && missionPeer.Team.IsEnemyOf(team))
		{
			ShowNotification(message, param1, param2);
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component?.Team != null && !component.IsMine && !component.Team.IsEnemyOf(team))
			{
				GameNetwork.BeginModuleEventAsServer(component.Peer);
				GameNetwork.WriteMessage(new NotificationMessage((int)message, param1, param2));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private string ToSoundString(MultiplayerNotificationEnum value, NotificationProperty attribute, params int[] parameters)
	{
		string result = string.Empty;
		if (string.IsNullOrEmpty(attribute.SoundIdTwo))
		{
			result = attribute.SoundIdOne;
		}
		else
		{
			switch (value)
			{
			case MultiplayerNotificationEnum.FlagXCapturedByYourTeam:
				result = attribute.SoundIdOne;
				break;
			case MultiplayerNotificationEnum.FlagXCapturedByOtherTeam:
				result = attribute.SoundIdTwo;
				break;
			case MultiplayerNotificationEnum.BattleYouHaveXTheRound:
			{
				Team team = ((parameters[0] == 0) ? Mission.Current.AttackerTeam : Mission.Current.DefenderTeam);
				Team team2 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				result = attribute.SoundIdOne;
				if (team2 != null && team2 != team)
				{
					result = attribute.SoundIdTwo;
				}
				break;
			}
			}
		}
		return result;
	}

	private TextObject ToNotificationString(MultiplayerNotificationEnum value, NotificationProperty attribute, params int[] parameters)
	{
		if (parameters.Length != 0)
		{
			SetGameTextVariables(value, parameters);
		}
		return GameTexts.FindText(attribute.StringId);
	}

	private void SetGameTextVariables(MultiplayerNotificationEnum message, params int[] parameters)
	{
		if (parameters.Length == 0)
		{
			return;
		}
		switch (message)
		{
		case MultiplayerNotificationEnum.BattleWarmupEnding:
			GameTexts.SetVariable("SECONDS_LEFT", parameters[0]);
			break;
		case MultiplayerNotificationEnum.BattleYouHaveXTheRound:
		{
			Team team = ((parameters[0] == 0) ? Mission.Current.AttackerTeam : Mission.Current.DefenderTeam);
			Team team2 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
			if (team2 != null)
			{
				GameTexts.SetVariable("IS_WINNER", (team2 == team) ? 1 : 0);
			}
			break;
		}
		case MultiplayerNotificationEnum.FlagXRemoved:
			GameTexts.SetVariable("PARAM1", ((char)parameters[0]).ToString());
			break;
		case MultiplayerNotificationEnum.FlagXRemaining:
			GameTexts.SetVariable("PARAM1", ((char)parameters[0]).ToString());
			break;
		case MultiplayerNotificationEnum.FlagsWillBeRemoved:
			GameTexts.SetVariable("PARAM1", parameters[0]);
			break;
		case MultiplayerNotificationEnum.FlagXCapturedByYourTeam:
		case MultiplayerNotificationEnum.FlagXCapturedByOtherTeam:
			GameTexts.SetVariable("PARAM1", ((char)parameters[0]).ToString());
			break;
		case MultiplayerNotificationEnum.GoldCarriedFromPreviousRound:
			GameTexts.SetVariable("PARAM1", parameters[0].ToString());
			break;
		case MultiplayerNotificationEnum.PlayerIsKicked:
			GameTexts.SetVariable("PLAYER_NAME", GameNetwork.FindNetworkPeer(parameters[0]).UserName);
			break;
		case MultiplayerNotificationEnum.NotEnoughPlayersToOpenPoll:
			GameTexts.SetVariable("MIN_PARTICIPANT_COUNT", parameters[0]);
			break;
		case MultiplayerNotificationEnum.BattlePreparationStart:
		case MultiplayerNotificationEnum.GameOverDraw:
		case MultiplayerNotificationEnum.GameOverVictory:
		case MultiplayerNotificationEnum.GameOverDefeat:
		case MultiplayerNotificationEnum.PlayerIsInactive:
		case MultiplayerNotificationEnum.HasOngoingPoll:
		case MultiplayerNotificationEnum.TooManyPollRequests:
		case MultiplayerNotificationEnum.KickPollTargetNotSynced:
			break;
		}
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<NotificationMessage>(HandleServerEventServerMessage);
		}
	}

	private void HandleServerEventServerMessage(GameNetworkMessage baseMessage)
	{
		NotificationMessage notificationMessage = (NotificationMessage)baseMessage;
		ShowNotification((MultiplayerNotificationEnum)notificationMessage.Message, notificationMessage.ParameterOne, notificationMessage.ParameterTwo);
	}

	protected override void HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
		_ = clientConnectionInfo.NetworkPeer.IsServerPeer;
	}

	protected override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		_ = GameNetwork.IsServer;
	}
}
