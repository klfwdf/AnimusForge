using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SallyOutMissionNotificationsHandler
{
	private enum NotificationType
	{
		SallyOutObjective,
		BesiegerSideStrenghtening,
		BesiegerSideReinforcementsSpawned,
		BesiegedSideTacticalRetreat,
		BesiegedSidePlayerPullbackRequest,
		SiegeEnginesDestroyed
	}

	private const float NotificationCheckInterval = 5f;

	private DefaultBattleMissionAgentSpawnLogic _spawnLogic;

	private SallyOutMissionController _sallyOutController;

	private bool _isPlayerBesieged;

	private MBReadOnlyList<SiegeWeapon> _besiegerSiegeEngines;

	private Queue<NotificationType> _notificationsQueue;

	private BasicMissionTimer _notificationTimer;

	private bool _notificationTimerEnabled = true;

	private bool _objectiveMessageSent;

	private bool _siegeEnginesDestroyedMessageSent;

	private bool _besiegersStrengtheningMessageSent;

	private int _besiegerSpawnedTroopCount;

	public SallyOutMissionNotificationsHandler(DefaultBattleMissionAgentSpawnLogic spawnLogic, SallyOutMissionController sallyOutController)
	{
		_spawnLogic = spawnLogic;
		_sallyOutController = sallyOutController;
		_spawnLogic.OnReinforcementsSpawned += OnReinforcementsSpawned;
		_spawnLogic.OnInitialTroopsSpawned += OnInitialTroopsSpawned;
		_besiegerSpawnedTroopCount = 0;
		_notificationTimer = new BasicMissionTimer();
		_notificationsQueue = new Queue<NotificationType>();
	}

	public void OnBesiegedSideFallsbackToKeep()
	{
		if (_isPlayerBesieged)
		{
			if (Mission.Current.PlayerTeam.FormationsIncludingEmpty.Any((Formation f) => f.IsAIControlled && f.CountOfUnits > 0))
			{
				_notificationsQueue.Enqueue(NotificationType.BesiegedSideTacticalRetreat);
				if (Mission.Current.MainAgent != null && Mission.Current.MainAgent.IsActive())
				{
					_notificationsQueue.Enqueue(NotificationType.BesiegedSidePlayerPullbackRequest);
				}
			}
		}
		else
		{
			_notificationsQueue.Enqueue(NotificationType.BesiegedSideTacticalRetreat);
		}
	}

	public void OnAfterStart()
	{
		_isPlayerBesieged = Mission.Current.PlayerTeam.Side == BattleSideEnum.Defender;
		SetNotificationTimerEnabled(value: false);
	}

	public void OnMissionEnd()
	{
		_spawnLogic.OnReinforcementsSpawned -= OnReinforcementsSpawned;
		_spawnLogic.OnInitialTroopsSpawned -= OnInitialTroopsSpawned;
	}

	public void OnDeploymentFinished()
	{
		SetNotificationTimerEnabled(value: true);
		_besiegerSiegeEngines = _sallyOutController.BesiegerSiegeEngines;
	}

	public void OnMissionTick(float dt)
	{
		if (_notificationTimerEnabled && _notificationTimer.ElapsedTime >= 5f)
		{
			CheckPeriodicNotifications();
			if (!_notificationsQueue.IsEmpty())
			{
				NotificationType type = _notificationsQueue.Dequeue();
				SendNotification(type);
			}
			_notificationTimer.Reset();
		}
	}

	private void SetNotificationTimerEnabled(bool value, bool resetTimer = true)
	{
		_notificationTimerEnabled = value;
		if (resetTimer)
		{
			_notificationTimer.Reset();
		}
	}

	private void CheckPeriodicNotifications()
	{
		if (!_objectiveMessageSent)
		{
			_notificationsQueue.Enqueue(NotificationType.SallyOutObjective);
			_objectiveMessageSent = true;
		}
		if (!_siegeEnginesDestroyedMessageSent && IsSiegeEnginesDestroyed())
		{
			_notificationsQueue.Enqueue(NotificationType.SiegeEnginesDestroyed);
			_siegeEnginesDestroyedMessageSent = true;
		}
		if (!_besiegersStrengtheningMessageSent && _spawnLogic.NumberOfRemainingDefenderTroops == 0 && _besiegerSpawnedTroopCount >= _spawnLogic.NumberOfActiveDefenderTroops)
		{
			_notificationsQueue.Enqueue(NotificationType.BesiegerSideStrenghtening);
			_besiegersStrengtheningMessageSent = true;
		}
	}

	private void SendNotification(NotificationType type)
	{
		int num = -1;
		if (_isPlayerBesieged)
		{
			switch (type)
			{
			case NotificationType.SallyOutObjective:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_besieged_objective_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/move");
				break;
			case NotificationType.BesiegerSideReinforcementsSpawned:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_enemy_reinforcements_arrived"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/reinforcements");
				break;
			case NotificationType.BesiegerSideStrenghtening:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_enemy_becoming_strong_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/retreat");
				break;
			case NotificationType.SiegeEnginesDestroyed:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_enemy_siege_engines_destroyed_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/move");
				break;
			case NotificationType.BesiegedSideTacticalRetreat:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_allied_troops_tactical_retreat_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/retreat");
				break;
			case NotificationType.BesiegedSidePlayerPullbackRequest:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_allied_pullback_or_take_command_message"));
				break;
			}
		}
		else
		{
			switch (type)
			{
			case NotificationType.SallyOutObjective:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_besieger_objective_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/move");
				break;
			case NotificationType.BesiegerSideReinforcementsSpawned:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_allied_reinforcements_arrived"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/reinforcements");
				break;
			case NotificationType.SiegeEnginesDestroyed:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_sally_out_allied_siege_engines_destroyed_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/move");
				break;
			case NotificationType.BesiegedSideTacticalRetreat:
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_enemy_troops_fall_back_to_keep_message"));
				num = SoundEvent.GetEventIdFromString("event:/alerts/horns/move");
				break;
			}
		}
		if (num >= 0)
		{
			PlayNotificationSound(num);
		}
	}

	private void PlayNotificationSound(int soundId)
	{
		MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
		Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
		MBSoundEvent.PlaySound(soundId, position);
	}

	private void OnInitialTroopsSpawned(BattleSideEnum battleSide, int numberOfTroopsSpawned)
	{
		if (battleSide == BattleSideEnum.Attacker)
		{
			_besiegerSpawnedTroopCount += numberOfTroopsSpawned;
		}
	}

	private void OnReinforcementsSpawned(BattleSideEnum battleSide, int numberOfTroopsSpawned)
	{
		if (battleSide == BattleSideEnum.Attacker)
		{
			_besiegerSpawnedTroopCount += numberOfTroopsSpawned;
			_notificationsQueue.Enqueue(NotificationType.BesiegerSideReinforcementsSpawned);
		}
	}

	private bool IsSiegeEnginesDestroyed()
	{
		if (_besiegerSiegeEngines != null)
		{
			return _besiegerSiegeEngines.All((SiegeWeapon siegeEngine) => siegeEngine.DestructionComponent.IsDestroyed);
		}
		return false;
	}
}
