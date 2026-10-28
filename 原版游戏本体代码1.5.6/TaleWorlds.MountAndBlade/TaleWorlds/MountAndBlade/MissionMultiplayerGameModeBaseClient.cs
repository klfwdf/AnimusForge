using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionMultiplayerGameModeBaseClient : MissionNetwork, ICameraModeLogic
{
	public MissionLobbyComponent MissionLobbyComponent { get; private set; }

	public MissionNetworkComponent MissionNetworkComponent { get; private set; }

	public MissionScoreboardComponent ScoreboardComponent { get; private set; }

	public MultiplayerGameNotificationsComponent NotificationsComponent { get; private set; }

	public MultiplayerWarmupComponent WarmupComponent { get; private set; }

	public IRoundComponent RoundComponent { get; private set; }

	public MultiplayerTimerComponent TimerComponent { get; private set; }

	public abstract bool IsGameModeUsingGold { get; }

	public abstract bool IsGameModeTactical { get; }

	public virtual bool IsGameModeUsingCasualGold => true;

	public abstract bool IsGameModeUsingRoundCountdown { get; }

	public virtual bool IsGameModeUsingAllowCultureChange => false;

	public virtual bool IsGameModeUsingAllowTroopChange => false;

	public abstract MultiplayerGameType GameType { get; }

	public bool IsRoundInProgress
	{
		get
		{
			IRoundComponent roundComponent = RoundComponent;
			if (roundComponent == null)
			{
				return false;
			}
			return roundComponent.CurrentRoundState == MultiplayerRoundState.InProgress;
		}
	}

	public bool IsInWarmup => MissionLobbyComponent.IsInWarmup;

	public float RemainingTime => TimerComponent.GetRemainingTime(GameNetwork.IsClientOrReplay);

	public abstract int GetGoldAmount();

	public virtual SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
	{
		return SpectatorCameraTypes.Invalid;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MissionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		MissionNetworkComponent = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
		ScoreboardComponent = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
		NotificationsComponent = base.Mission.GetMissionBehavior<MultiplayerGameNotificationsComponent>();
		WarmupComponent = base.Mission.GetMissionBehavior<MultiplayerWarmupComponent>();
		RoundComponent = base.Mission.GetMissionBehavior<IRoundComponent>();
		TimerComponent = base.Mission.GetMissionBehavior<MultiplayerTimerComponent>();
	}

	public override void EarlyStart()
	{
		MissionLobbyComponent.MissionType = GameType;
	}

	public bool CheckTimer(out int remainingTime, out int remainingWarningTime, bool forceUpdate = false)
	{
		bool flag = false;
		float f = 0f;
		if (WarmupComponent != null && MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
		{
			flag = !WarmupComponent.IsInWarmup;
		}
		else if (RoundComponent != null)
		{
			flag = !RoundComponent.CurrentRoundState.StateHasVisualTimer();
			f = RoundComponent.LastRoundEndRemainingTime;
		}
		if (forceUpdate || !flag)
		{
			if (flag)
			{
				remainingTime = TaleWorlds.Library.MathF.Ceiling(f);
			}
			else
			{
				remainingTime = TaleWorlds.Library.MathF.Ceiling(RemainingTime);
			}
			remainingWarningTime = GetWarningTimer();
			return true;
		}
		remainingTime = 0;
		remainingWarningTime = 0;
		return false;
	}

	protected virtual int GetWarningTimer()
	{
		return 0;
	}

	public abstract void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount);

	public virtual bool CanRequestTroopChange()
	{
		return false;
	}

	public virtual bool CanRequestCultureChange()
	{
		return false;
	}

	public bool IsClassAvailable(MultiplayerClassDivisions.MPHeroClass heroClass)
	{
		if (Enum.TryParse<FormationClass>(heroClass.ClassGroup.StringId, out var result))
		{
			return MissionLobbyComponent.IsClassAvailable(result);
		}
		Debug.FailedAssert("\"" + heroClass.ClassGroup.StringId + "\" does not match with any FormationClass.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameModeLogics\\ClientGameModeLogics\\MissionMultiplayerGameModeBaseClient.cs", "IsClassAvailable", 116);
		return false;
	}
}
