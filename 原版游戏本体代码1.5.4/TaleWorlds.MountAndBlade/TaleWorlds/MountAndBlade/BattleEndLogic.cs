using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace TaleWorlds.MountAndBlade;

public class BattleEndLogic : MissionLogic, IBattleEndLogic
{
	public enum ExitResult
	{
		False,
		NeedsPlayerConfirmation,
		SurrenderSiege,
		True
	}

	private IMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private MissionTime _enemySideNotYetRetreatingTime;

	private MissionTime _playerSideNotYetRetreatingTime;

	private BasicMissionTimer _checkRetreatingTimer;

	private bool _isEnemySideRetreating;

	private bool _isPlayerSideRetreating;

	private bool _isEnemySideDepleted;

	private bool _isPlayerSideDepleted;

	private bool _isEnemyDefenderPulledBack;

	private bool _canCheckForEndCondition = true;

	private bool _canCheckForEndConditionSiege;

	private bool _enemyDefenderPullbackEnabled;

	private int _troopNumberNeededForEnemyDefenderPullBack;

	private bool _missionEndedMessageShown;

	private bool _victoryReactionsActivated;

	private bool _victoryReactionsActivatedForRetreating;

	private bool _scoreBoardOpenedOnceOnMissionEnd;

	public bool PlayerVictory
	{
		get
		{
			if (_isEnemySideRetreating || _isEnemySideDepleted)
			{
				return !_isEnemyDefenderPulledBack;
			}
			return false;
		}
	}

	public bool EnemyVictory
	{
		get
		{
			if (!_isPlayerSideRetreating)
			{
				return _isPlayerSideDepleted;
			}
			return true;
		}
	}

	public bool IsEnemySideRetreating => _isEnemySideRetreating;

	private bool _notificationsDisabled { get; set; }

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_checkRetreatingTimer = new BasicMissionTimer();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
	}

	public override void OnMissionTick(float dt)
	{
		if (base.Mission.IsMissionEnding)
		{
			if (_notificationsDisabled)
			{
				_scoreBoardOpenedOnceOnMissionEnd = true;
			}
			if (_missionEndedMessageShown && !_scoreBoardOpenedOnceOnMissionEnd)
			{
				if (_checkRetreatingTimer.ElapsedTime > 7f)
				{
					CheckIsEnemySideRetreatingOrOneSideDepleted();
					_checkRetreatingTimer.Reset();
					if (base.Mission.MissionResult != null && base.Mission.MissionResult.PlayerDefeated)
					{
						GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_lost_press_tab_to_view_results"));
					}
					else if (base.Mission.MissionResult != null && base.Mission.MissionResult.PlayerVictory)
					{
						if (_isEnemySideDepleted)
						{
							GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
							MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_won_press_tab_to_view_results"));
						}
					}
					else
					{
						GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_finished_press_tab_to_view_results"));
					}
				}
			}
			else if (_checkRetreatingTimer.ElapsedTime > 3f && !_scoreBoardOpenedOnceOnMissionEnd)
			{
				if (base.Mission.MissionResult != null && base.Mission.MissionResult.PlayerDefeated)
				{
					if (_isPlayerSideDepleted)
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_lost"));
					}
					else if (_isPlayerSideRetreating)
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_friendlies_are_fleeing_you_lost"));
					}
				}
				else if (base.Mission.MissionResult != null && base.Mission.MissionResult.PlayerVictory)
				{
					if (_isEnemySideDepleted)
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_won"));
					}
					else if (_isEnemySideRetreating)
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_enemies_are_fleeing_you_won"));
					}
				}
				else
				{
					MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_finished"));
				}
				_missionEndedMessageShown = true;
				_checkRetreatingTimer.Reset();
			}
			if (_victoryReactionsActivated)
			{
				return;
			}
			AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
			if (missionBehavior != null)
			{
				CheckIsEnemySideRetreatingOrOneSideDepleted();
				if (_isEnemySideDepleted)
				{
					missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(base.Mission.PlayerTeam.Side);
					_victoryReactionsActivated = true;
				}
				else if (_isPlayerSideDepleted)
				{
					missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(base.Mission.PlayerEnemyTeam.Side);
					_victoryReactionsActivated = true;
				}
				else if (_isEnemySideRetreating && !_victoryReactionsActivatedForRetreating)
				{
					missionBehavior.SetTimersOfVictoryReactionsOnRetreat(base.Mission.PlayerTeam.Side);
					_victoryReactionsActivatedForRetreating = true;
				}
				else if (_isPlayerSideRetreating && !_victoryReactionsActivatedForRetreating)
				{
					missionBehavior.SetTimersOfVictoryReactionsOnRetreat(base.Mission.PlayerEnemyTeam.Side);
					_victoryReactionsActivatedForRetreating = true;
				}
			}
		}
		else if (_checkRetreatingTimer.ElapsedTime > 1f)
		{
			CheckIsEnemySideRetreatingOrOneSideDepleted();
			_checkRetreatingTimer.Reset();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (_enemyDefenderPullbackEnabled && _troopNumberNeededForEnemyDefenderPullBack > 0 && affectedAgent.IsHuman && agentState == AgentState.Routed && affectedAgent.Team != null && affectedAgent.Team.Side == BattleSideEnum.Defender && affectedAgent.Team.Side != base.Mission.PlayerTeam.Side)
		{
			_troopNumberNeededForEnemyDefenderPullBack--;
			_isEnemyDefenderPulledBack = _troopNumberNeededForEnemyDefenderPullBack <= 0;
		}
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		bool flag = false;
		if (_isEnemySideDepleted && _isEnemyDefenderPulledBack)
		{
			missionResult = MissionResult.CreateDefenderPushedBack();
			flag = true;
		}
		else if (_isEnemySideRetreating || _isEnemySideDepleted)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission, _isEnemySideRetreating);
			flag = true;
		}
		else if (_isPlayerSideRetreating || _isPlayerSideDepleted)
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			flag = true;
		}
		if (flag)
		{
			_missionAgentSpawnLogic.StopSpawner(BattleSideEnum.Attacker);
			_missionAgentSpawnLogic.StopSpawner(BattleSideEnum.Defender);
		}
		return flag;
	}

	protected override void OnEndMission()
	{
		if (!_isEnemySideRetreating)
		{
			return;
		}
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			bool flag = activeAgent.GetMorale() < 0.01f;
			activeAgent.Origin?.SetRouted(!flag);
		}
	}

	public void ChangeCanCheckForEndCondition(bool canCheckForEndCondition)
	{
		_canCheckForEndCondition = canCheckForEndCondition;
	}

	public ExitResult TryExit()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return ExitResult.False;
		}
		if (base.Mission.MissionEnded || (!PlayerVictory && !EnemyVictory))
		{
			Agent mainAgent = base.Mission.MainAgent;
			if (mainAgent == null || !mainAgent.IsActive() || !base.Mission.IsPlayerCloseToAnEnemy())
			{
				if (!base.Mission.MissionEnded && !_isEnemySideRetreating)
				{
					if (Mission.Current.IsSiegeBattle && base.Mission.PlayerTeam.IsDefender)
					{
						return ExitResult.SurrenderSiege;
					}
					return ExitResult.NeedsPlayerConfirmation;
				}
				base.Mission.EndMission();
				return ExitResult.True;
			}
		}
		return ExitResult.False;
	}

	public void EnableEnemyDefenderPullBack(int neededTroopNumber)
	{
		_enemyDefenderPullbackEnabled = true;
		_troopNumberNeededForEnemyDefenderPullBack = neededTroopNumber;
	}

	public void SetNotificationDisabled(bool value)
	{
		_notificationsDisabled = value;
	}

	private void CheckIsEnemySideRetreatingOrOneSideDepleted()
	{
		if (!_canCheckForEndConditionSiege)
		{
			_canCheckForEndConditionSiege = base.Mission.GetMissionBehavior<BattleDeploymentHandler>() == null;
		}
		else
		{
			if (!_canCheckForEndCondition)
			{
				return;
			}
			BattleSideEnum side = base.Mission.PlayerTeam.Side;
			BattleSideEnum oppositeSide = side.GetOppositeSide();
			_isPlayerSideDepleted = _missionAgentSpawnLogic.IsSideDepleted(side);
			_isEnemySideDepleted = _missionAgentSpawnLogic.IsSideDepleted(oppositeSide);
			if (_isEnemySideDepleted || _isPlayerSideDepleted || base.Mission.GetMissionBehavior<HideoutPhasedMissionController>() != null)
			{
				return;
			}
			float num = _missionAgentSpawnLogic.GetReinforcementInterval(side) + 3f;
			if (base.Mission.MainAgent != null && base.Mission.MainAgent.IsPlayerControlled && base.Mission.MainAgent.IsActive())
			{
				_playerSideNotYetRetreatingTime = MissionTime.Now;
			}
			else
			{
				bool flag = true;
				foreach (Team team in base.Mission.Teams)
				{
					if (!team.IsFriendOf(base.Mission.PlayerTeam))
					{
						continue;
					}
					foreach (Agent activeAgent in team.ActiveAgents)
					{
						if (!activeAgent.IsRunningAway)
						{
							flag = false;
							break;
						}
					}
				}
				if (!flag)
				{
					_playerSideNotYetRetreatingTime = MissionTime.Now;
				}
			}
			if (_playerSideNotYetRetreatingTime.ElapsedSeconds > num)
			{
				_isPlayerSideRetreating = true;
			}
			if (oppositeSide == BattleSideEnum.Defender && _enemyDefenderPullbackEnabled)
			{
				return;
			}
			float num2 = _missionAgentSpawnLogic.GetReinforcementInterval(oppositeSide) + 3f;
			bool flag2 = true;
			foreach (Team team2 in base.Mission.Teams)
			{
				if (!team2.IsEnemyOf(base.Mission.PlayerTeam))
				{
					continue;
				}
				foreach (Agent activeAgent2 in team2.ActiveAgents)
				{
					if (!activeAgent2.IsRunningAway)
					{
						flag2 = false;
						break;
					}
				}
			}
			if (!flag2)
			{
				_enemySideNotYetRetreatingTime = MissionTime.Now;
			}
			if (_enemySideNotYetRetreatingTime.ElapsedSeconds > num2)
			{
				_isEnemySideRetreating = true;
			}
		}
	}
}
