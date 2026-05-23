using System;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerTeamDeathmatchClient : MissionMultiplayerGameModeBaseClient
{
	private const string BattleWinningSoundEventString = "event:/alerts/report/battle_winning";

	private const string BattleLosingSoundEventString = "event:/alerts/report/battle_losing";

	private const float BattleWinLoseAlertThreshold = 0.1f;

	private TeamDeathmatchMissionRepresentative _myRepresentative;

	private bool _battleEndingNotificationGiven;

	public override bool IsGameModeUsingGold => true;

	public override bool IsGameModeTactical => false;

	public override bool IsGameModeUsingRoundCountdown => true;

	public override MultiplayerGameType GameType => MultiplayerGameType.TeamDeathmatch;

	public event Action<GoldGain> OnGoldGainEvent;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.MissionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		base.ScoreboardComponent.OnRoundPropertiesChanged += OnTeamScoresChanged;
	}

	public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
	{
		if (representative != null && base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			representative.UpdateGold(goldAmount);
			base.ScoreboardComponent.PlayerPropertiesChanged(representative.MissionPeer);
		}
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<SyncGoldsForSkirmish>(HandleServerEventUpdateGold);
			registerer.RegisterBaseHandler<GoldGain>(HandleServerEventTDMGoldGain);
		}
	}

	private void OnMyClientSynchronized()
	{
		_myRepresentative = GameNetwork.MyPeer.GetComponent<TeamDeathmatchMissionRepresentative>();
	}

	private void HandleServerEventUpdateGold(GameNetworkMessage baseMessage)
	{
		SyncGoldsForSkirmish syncGoldsForSkirmish = (SyncGoldsForSkirmish)baseMessage;
		MissionRepresentativeBase component = syncGoldsForSkirmish.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
		OnGoldAmountChangedForRepresentative(component, syncGoldsForSkirmish.GoldAmount);
	}

	private void HandleServerEventTDMGoldGain(GameNetworkMessage baseMessage)
	{
		GoldGain obj = (GoldGain)baseMessage;
		this.OnGoldGainEvent?.Invoke(obj);
	}

	public override int GetGoldAmount()
	{
		return _myRepresentative.Gold;
	}

	public override void OnRemoveBehavior()
	{
		base.MissionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		base.ScoreboardComponent.OnRoundPropertiesChanged -= OnTeamScoresChanged;
		base.OnRemoveBehavior();
	}

	private void OnTeamScoresChanged()
	{
		if (!GameNetwork.IsDedicatedServer && !_battleEndingNotificationGiven && _myRepresentative.MissionPeer.Team != null && _myRepresentative.MissionPeer.Team.Side != BattleSideEnum.None)
		{
			int intValue = MultiplayerOptions.OptionType.MinScoreToWinMatch.GetIntValue();
			float num = (float)(intValue - base.ScoreboardComponent.GetRoundScore(_myRepresentative.MissionPeer.Team.Side)) / (float)intValue;
			float num2 = (float)(intValue - base.ScoreboardComponent.GetRoundScore(_myRepresentative.MissionPeer.Team.Side.GetOppositeSide())) / (float)intValue;
			MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
			Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
			if (num <= 0.1f && num2 > 0.1f)
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/battle_winning"), position);
				_battleEndingNotificationGiven = true;
			}
			if (num2 <= 0.1f && num > 0.1f)
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/battle_losing"), position);
				_battleEndingNotificationGiven = true;
			}
		}
	}
}
