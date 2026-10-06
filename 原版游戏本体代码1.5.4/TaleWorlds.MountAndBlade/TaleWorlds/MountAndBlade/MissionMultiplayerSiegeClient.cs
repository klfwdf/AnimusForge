using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerSiegeClient : MissionMultiplayerGameModeBaseClient, ICommanderInfo, IMissionBehavior
{
	private const float DefenderMoraleDropThresholdIncrement = 0.2f;

	private const float DefenderMoraleDropThresholdLow = 0.4f;

	private const float DefenderMoraleDropThresholdMedium = 0.6f;

	private const float DefenderMoraleDropThresholdHigh = 0.8f;

	private const float DefenderMoraleDropMediumDuration = 8f;

	private const float DefenderMoraleDropHighDuration = 4f;

	private const float BattleWinLoseAlertThreshold = 0.25f;

	private const float BattleWinLoseLateAlertThreshold = 0.15f;

	private const string BattleWinningSoundEventString = "event:/alerts/report/battle_winning";

	private const string BattleLosingSoundEventString = "event:/alerts/report/battle_losing";

	private const float IndefiniteDurationThreshold = 8f;

	private Team[] _capturePointOwners;

	private FlagCapturePoint _masterFlag;

	private SiegeMissionRepresentative _myRepresentative;

	private SoundEvent _bellSoundEvent;

	private float _remainingTimeForBellSoundToStop = float.MinValue;

	private float _lastBellSoundPercentage = 1f;

	private bool _battleEndingNotificationGiven;

	private bool _battleEndingLateNotificationGiven;

	private Vec3 _retreatHornPosition;

	public override bool IsGameModeUsingGold => true;

	public override bool IsGameModeTactical => true;

	public override bool IsGameModeUsingRoundCountdown => true;

	public override MultiplayerGameType GameType => MultiplayerGameType.Siege;

	public bool AreMoralesIndependent => true;

	public IEnumerable<FlagCapturePoint> AllCapturePoints { get; private set; }

	public event Action<BattleSideEnum, float> OnMoraleChangedEvent;

	public event Action OnFlagNumberChangedEvent;

	public event Action<FlagCapturePoint, Team> OnCapturePointOwnerChangedEvent;

	public event Action<GoldGain> OnGoldGainEvent;

	public event Action<int[]> OnCapturePointRemainingMoraleGainsChangedEvent;

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<SiegeMoraleChangeMessage>(HandleMoraleChangedMessage);
			registerer.RegisterBaseHandler<SyncGoldsForSkirmish>(HandleServerEventUpdateGold);
			registerer.RegisterBaseHandler<FlagDominationFlagsRemovedMessage>(HandleFlagsRemovedMessage);
			registerer.RegisterBaseHandler<FlagDominationCapturePointMessage>(HandleServerEventPointCapturedMessage);
			registerer.RegisterBaseHandler<GoldGain>(HandleServerEventTDMGoldGain);
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.MissionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		_capturePointOwners = new Team[7];
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>();
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (allCapturePoint.GameEntity.HasTag("keep_capture_point"))
			{
				_masterFlag = allCapturePoint;
			}
			else if (allCapturePoint.FlagIndex == 0)
			{
				MatrixFrame globalFrame = allCapturePoint.GameEntity.GetGlobalFrame();
				_retreatHornPosition = globalFrame.origin + globalFrame.rotation.u * 3f;
			}
		}
	}

	private void OnMyClientSynchronized()
	{
		_myRepresentative = GameNetwork.MyPeer.GetComponent<SiegeMissionRepresentative>();
	}

	public override int GetGoldAmount()
	{
		return _myRepresentative.Gold;
	}

	public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
	{
		if (representative != null && base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			representative.UpdateGold(goldAmount);
			base.ScoreboardComponent.PlayerPropertiesChanged(representative.MissionPeer);
		}
	}

	public void OnNumberOfFlagsChanged()
	{
		this.OnFlagNumberChangedEvent?.Invoke();
		SiegeMissionRepresentative myRepresentative = _myRepresentative;
		if (myRepresentative != null && myRepresentative.MissionPeer.Team?.Side == BattleSideEnum.Attacker)
		{
			this.OnGoldGainEvent?.Invoke(new GoldGain(new List<KeyValuePair<ushort, int>>
			{
				new KeyValuePair<ushort, int>(512, 35)
			}));
		}
	}

	public void OnCapturePointOwnerChanged(FlagCapturePoint flagCapturePoint, Team ownerTeam)
	{
		_capturePointOwners[flagCapturePoint.FlagIndex] = ownerTeam;
		this.OnCapturePointOwnerChangedEvent?.Invoke(flagCapturePoint, ownerTeam);
		if (ownerTeam != null && ownerTeam.Side == BattleSideEnum.Defender && _remainingTimeForBellSoundToStop > 8f && flagCapturePoint == _masterFlag)
		{
			_bellSoundEvent.Stop();
			_bellSoundEvent = null;
			_remainingTimeForBellSoundToStop = float.MinValue;
			_lastBellSoundPercentage += 0.2f;
		}
		if (_myRepresentative != null && _myRepresentative.MissionPeer.Team != null)
		{
			MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
			Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
			if (_myRepresentative.MissionPeer.Team == ownerTeam)
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_captured"), position);
			}
			else
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_lost"), position);
			}
		}
	}

	public void OnMoraleChanged(int attackerMorale, int defenderMorale, int[] capturePointRemainingMoraleGains)
	{
		float num = (float)attackerMorale / 360f;
		float num2 = (float)defenderMorale / 360f;
		if (_myRepresentative?.MissionPeer.Team != null && _myRepresentative.MissionPeer.Team.Side != BattleSideEnum.None)
		{
			if ((_capturePointOwners[_masterFlag.FlagIndex] == null || _capturePointOwners[_masterFlag.FlagIndex].Side != BattleSideEnum.Defender) && _remainingTimeForBellSoundToStop < 0f)
			{
				if (num2 > _lastBellSoundPercentage)
				{
					_lastBellSoundPercentage += 0.2f;
				}
				if (num2 <= 0.4f)
				{
					if (_lastBellSoundPercentage > 0.4f)
					{
						_remainingTimeForBellSoundToStop = float.MaxValue;
						_lastBellSoundPercentage = 0.4f;
					}
				}
				else if (num2 <= 0.6f)
				{
					if (_lastBellSoundPercentage > 0.6f)
					{
						_remainingTimeForBellSoundToStop = 8f;
						_lastBellSoundPercentage = 0.6f;
					}
				}
				else if (num2 <= 0.8f && _lastBellSoundPercentage > 0.8f)
				{
					_remainingTimeForBellSoundToStop = 4f;
					_lastBellSoundPercentage = 0.8f;
				}
				if (_remainingTimeForBellSoundToStop > 0f)
				{
					switch (_myRepresentative.MissionPeer.Team.Side)
					{
					case BattleSideEnum.Defender:
						_bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_defender", base.Mission.Scene);
						break;
					case BattleSideEnum.Attacker:
						_bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_attacker", base.Mission.Scene);
						break;
					}
					MatrixFrame globalFrame = _masterFlag.GameEntity.GetGlobalFrame();
					_bellSoundEvent.PlayInPosition(globalFrame.origin + globalFrame.rotation.u * 3f);
				}
			}
			if (!_battleEndingNotificationGiven || !_battleEndingLateNotificationGiven)
			{
				float num3 = ((!_battleEndingNotificationGiven) ? 0.25f : 0.15f);
				MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
				Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
				if (num <= num3 && num2 > num3)
				{
					MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString((_myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Attacker) ? "event:/alerts/report/battle_losing" : "event:/alerts/report/battle_winning"), position);
					if (_myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Attacker)
					{
						MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/multiplayer/retreat_horn_attacker"), _retreatHornPosition);
					}
					else if (_myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Defender)
					{
						MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/multiplayer/retreat_horn_defender"), _retreatHornPosition);
					}
					if (_battleEndingNotificationGiven)
					{
						_battleEndingLateNotificationGiven = true;
					}
					_battleEndingNotificationGiven = true;
				}
				if (num2 <= num3 && num > num3)
				{
					MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString((_myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Defender) ? "event:/alerts/report/battle_losing" : "event:/alerts/report/battle_winning"), position);
					if (_battleEndingNotificationGiven)
					{
						_battleEndingLateNotificationGiven = true;
					}
					_battleEndingNotificationGiven = true;
				}
			}
		}
		this.OnMoraleChangedEvent?.Invoke(BattleSideEnum.Attacker, num);
		this.OnMoraleChangedEvent?.Invoke(BattleSideEnum.Defender, num2);
		this.OnCapturePointRemainingMoraleGainsChangedEvent?.Invoke(capturePointRemainingMoraleGains);
	}

	public Team GetFlagOwner(FlagCapturePoint flag)
	{
		return _capturePointOwners[flag.FlagIndex];
	}

	public override void OnRemoveBehavior()
	{
		base.MissionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		base.OnRemoveBehavior();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_remainingTimeForBellSoundToStop > 0f)
		{
			_remainingTimeForBellSoundToStop -= dt;
			if (_remainingTimeForBellSoundToStop <= 0f || base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing)
			{
				_remainingTimeForBellSoundToStop = float.MinValue;
				_bellSoundEvent.Stop();
				_bellSoundEvent = null;
			}
		}
	}

	public List<ItemObject> GetSiegeMissiles()
	{
		List<ItemObject> list = new List<ItemObject>();
		foreach (WeakGameEntity item3 in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<RangedSiegeWeapon>())
		{
			RangedSiegeWeapon firstScriptOfType = item3.GetFirstScriptOfType<RangedSiegeWeapon>();
			if (!string.IsNullOrEmpty(firstScriptOfType.MissileItemID))
			{
				ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MissileItemID);
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
			foreach (ItemObject item4 in new List<ItemObject>
			{
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleFireProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleFireProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleFireProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleFireProjectileFlyingId)
			})
			{
				if (!list.Contains(item4))
				{
					list.Add(item4);
				}
			}
		}
		foreach (WeakGameEntity item5 in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<StonePile>())
		{
			StonePile firstScriptOfType2 = item5.GetFirstScriptOfType<StonePile>();
			if (!string.IsNullOrEmpty(firstScriptOfType2.GivenItemID))
			{
				ItemObject item2 = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType2.GivenItemID);
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private void HandleMoraleChangedMessage(GameNetworkMessage baseMessage)
	{
		SiegeMoraleChangeMessage siegeMoraleChangeMessage = (SiegeMoraleChangeMessage)baseMessage;
		OnMoraleChanged(siegeMoraleChangeMessage.AttackerMorale, siegeMoraleChangeMessage.DefenderMorale, siegeMoraleChangeMessage.CapturePointRemainingMoraleGains);
	}

	private void HandleServerEventUpdateGold(GameNetworkMessage baseMessage)
	{
		SyncGoldsForSkirmish syncGoldsForSkirmish = (SyncGoldsForSkirmish)baseMessage;
		SiegeMissionRepresentative component = syncGoldsForSkirmish.VirtualPlayer.GetComponent<SiegeMissionRepresentative>();
		OnGoldAmountChangedForRepresentative(component, syncGoldsForSkirmish.GoldAmount);
	}

	private void HandleFlagsRemovedMessage(GameNetworkMessage baseMessage)
	{
		OnNumberOfFlagsChanged();
	}

	private void HandleServerEventPointCapturedMessage(GameNetworkMessage baseMessage)
	{
		FlagDominationCapturePointMessage flagDominationCapturePointMessage = (FlagDominationCapturePointMessage)baseMessage;
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (allCapturePoint.FlagIndex == flagDominationCapturePointMessage.FlagIndex)
			{
				OnCapturePointOwnerChanged(allCapturePoint, Mission.MissionNetworkHelper.GetTeamFromTeamIndex(flagDominationCapturePointMessage.OwnerTeamIndex));
				break;
			}
		}
	}

	private void HandleServerEventTDMGoldGain(GameNetworkMessage baseMessage)
	{
		GoldGain obj = (GoldGain)baseMessage;
		this.OnGoldGainEvent?.Invoke(obj);
	}
}
