using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class NavalBattleEndLogic : MissionLogic, IBattleEndLogic
{
	public enum ExitResult
	{
		False,
		NeedsPlayerConfirmation,
		True
	}

	public const float DefaultContestedIslandsCheckDuration = 20f;

	public const float RetreatCheckDuration = 5f;

	public const float MainAgentConsideredDeadDuration = 20f;

	public const int MinTroopCountForOutOfActionCheck = 3;

	private IMissionAgentSpawnLogic _missionSpawnLogic;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private bool _notificationsDisabled;

	private MissionTime _enemySideNotYetRetreatingTime;

	private MissionTime _playerSideNotYetRetreatingTime;

	private MissionTime _contestedIslandCheckTimer;

	private MissionTime _mainAgentIsDeadTimer;

	private float _contestedIslandsCheckDuration = 20f;

	private bool _isInContestedIslandsCheckPhase;

	private BasicMissionTimer _checkDepletionOrRetreatingTimer;

	private bool _isPlayerSideRetreating;

	private bool _isEnemySideDepleted;

	private bool _isPlayerSideDepleted;

	private bool _missionEndedMessageShown;

	private bool _victoryReactionsActivated;

	private bool _victoryReactionsActivatedForRetreating;

	private bool _scoreBoardOpenedOnceOnMissionEnd;

	public bool PlayerVictory
	{
		get
		{
			if (!IsEnemySideRetreating)
			{
				return _isEnemySideDepleted;
			}
			return true;
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

	public bool IsEnemySideRetreating { get; private set; }

	public bool CanCheckForEndCondition { get; private set; }

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_checkDepletionOrRetreatingTimer = new BasicMissionTimer();
		_missionSpawnLogic = base.Mission.GetMissionBehavior<IMissionAgentSpawnLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic.MissionEndEvent += OnMissionEnd;
	}

	public override void OnDeploymentFinished()
	{
		_contestedIslandCheckTimer = MissionTime.Now;
		_mainAgentIsDeadTimer = MissionTime.Now;
		CanCheckForEndCondition = true;
	}

	public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (base.Mission.IsDeploymentFinished && affectedAgent == Agent.Main)
		{
			_mainAgentIsDeadTimer = MissionTime.Now;
		}
	}

	public override void OnAgentControllerSetToPlayer(Agent agent)
	{
		if (base.Mission.IsDeploymentFinished && agent.IsActive())
		{
			_mainAgentIsDeadTimer = MissionTime.Now;
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!base.Mission.IsDeploymentFinished)
		{
			return;
		}
		if (base.Mission.IsMissionEnding)
		{
			if (_notificationsDisabled)
			{
				_scoreBoardOpenedOnceOnMissionEnd = true;
			}
			if (_missionEndedMessageShown && !_scoreBoardOpenedOnceOnMissionEnd)
			{
				if (_checkDepletionOrRetreatingTimer.ElapsedTime > 7f)
				{
					CheckIsEnemySideRetreatingOrOneSideDepleted(forceCheckContestedIslands: true);
					_checkDepletionOrRetreatingTimer.Reset();
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
			else if (_checkDepletionOrRetreatingTimer.ElapsedTime > 3f && !_scoreBoardOpenedOnceOnMissionEnd)
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
					else if (IsEnemySideRetreating)
					{
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_enemies_are_fleeing_you_won"));
					}
				}
				else
				{
					MBInformationManager.AddQuickInformation(GameTexts.FindText("str_battle_finished"));
				}
				_missionEndedMessageShown = true;
				_checkDepletionOrRetreatingTimer.Reset();
			}
			if (_victoryReactionsActivated)
			{
				return;
			}
			AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
			if (missionBehavior != null)
			{
				CheckIsEnemySideRetreatingOrOneSideDepleted(forceCheckContestedIslands: true);
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
				else if (IsEnemySideRetreating && !_victoryReactionsActivatedForRetreating)
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
		else if (_checkDepletionOrRetreatingTimer.ElapsedTime > 1f)
		{
			CheckIsEnemySideRetreatingOrOneSideDepleted();
			if (_isInContestedIslandsCheckPhase)
			{
				_contestedIslandsCheckDuration = 5f;
			}
			else
			{
				_contestedIslandsCheckDuration = 20f;
			}
			_checkDepletionOrRetreatingTimer.Reset();
		}
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		bool flag = false;
		if (IsEnemySideRetreating || _isEnemySideDepleted)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission, IsEnemySideRetreating);
			flag = true;
		}
		else if (_isPlayerSideRetreating || _isPlayerSideDepleted)
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			flag = true;
		}
		if (flag)
		{
			_missionSpawnLogic.StopSpawner(BattleSideEnum.Attacker);
			_missionSpawnLogic.StopSpawner(BattleSideEnum.Defender);
		}
		return flag;
	}

	public override void OnMissionStateFinalized()
	{
		_navalShipsLogic.MissionEndEvent -= OnMissionEnd;
	}

	private void OnMissionEnd()
	{
		if (IsEnemySideRetreating)
		{
			foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
			{
				activeAgent.Origin?.SetRouted(isOrderRetreat: true);
			}
			MBList<MissionShip> mBList = new MBList<MissionShip>();
			_navalShipsLogic.FillTeamShips(TeamSideEnum.EnemyTeam, mBList);
			MBList<IAgentOriginBase> mBList2 = new MBList<IAgentOriginBase>();
			foreach (MissionShip item in mBList)
			{
				_navalAgentsLogic.FillReservedTroopsOfShip(item, mBList2);
			}
			foreach (IAgentOriginBase item2 in mBList2)
			{
				item2.SetRouted(isOrderRetreat: true);
			}
		}
		if (Campaign.Current == null || PlayerEncounter.Current == null)
		{
			return;
		}
		MBReadOnlyList<MapEventParty> source = new MBReadOnlyList<MapEventParty>();
		if (IsEnemySideRetreating || _isEnemySideDepleted)
		{
			source = PlayerEncounter.Battle.PartiesOnSide(PlayerEncounter.Battle.PlayerSide.GetOppositeSide());
		}
		else if (_isPlayerSideRetreating || _isPlayerSideDepleted)
		{
			source = PlayerEncounter.Battle.PartiesOnSide(PlayerEncounter.Battle.PlayerSide);
		}
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			Ship shipToCapture;
			if ((shipToCapture = allShip.ShipOrigin as Ship) != null && source.ContainsQ((MapEventParty x) => x.Party == shipToCapture.Owner))
			{
				PlayerEncounter.Current.CapturedShipsInEncounter.Add(shipToCapture);
			}
		}
	}

	public ExitResult TryExit()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return ExitResult.False;
		}
		Agent mainAgent = base.Mission.MainAgent;
		if ((mainAgent != null && mainAgent.IsActive() && base.Mission.IsPlayerCloseToAnEnemy()) || (!base.Mission.MissionEnded && (PlayerVictory || EnemyVictory)))
		{
			return ExitResult.False;
		}
		if (!base.Mission.MissionEnded && !IsEnemySideRetreating)
		{
			return ExitResult.NeedsPlayerConfirmation;
		}
		base.Mission.EndMission();
		return ExitResult.True;
	}

	public void SetNotificationDisabled(bool value)
	{
		_notificationsDisabled = value;
	}

	private void CheckIsEnemySideRetreatingOrOneSideDepleted(bool forceCheckContestedIslands = false)
	{
		if (!CanCheckForEndCondition)
		{
			return;
		}
		BattleSideEnum side = base.Mission.PlayerTeam.Side;
		BattleSideEnum oppositeSide = side.GetOppositeSide();
		if (_missionSpawnLogic.IsSideDepleted(side))
		{
			_isPlayerSideDepleted = true;
		}
		if (_missionSpawnLogic.IsSideDepleted(oppositeSide))
		{
			_isEnemySideDepleted = true;
		}
		if (_isEnemySideDepleted || _isPlayerSideDepleted)
		{
			return;
		}
		if (AreAnySideShipsOutOfAction(side, oppositeSide, out var playerShipsOutOfAction, out var enemyShipsOutOfAction))
		{
			_isInContestedIslandsCheckPhase = _contestedIslandCheckTimer.ElapsedSeconds > _contestedIslandsCheckDuration;
			if (forceCheckContestedIslands || _isInContestedIslandsCheckPhase)
			{
				if (!HasAnyContestedIslands(side, oppositeSide))
				{
					Agent main = Agent.Main;
					bool flag = (main == null || !main.IsActive()) && _mainAgentIsDeadTimer.ElapsedSeconds > 20f;
					if (playerShipsOutOfAction && flag)
					{
						_isPlayerSideDepleted = true;
					}
					if (enemyShipsOutOfAction)
					{
						_isEnemySideDepleted = true;
					}
				}
				_contestedIslandCheckTimer = MissionTime.Now;
			}
		}
		else
		{
			_isInContestedIslandsCheckPhase = false;
			_contestedIslandCheckTimer = MissionTime.Now;
		}
		if (_isEnemySideDepleted || _isPlayerSideDepleted)
		{
			return;
		}
		if (base.Mission.MainAgent != null && base.Mission.MainAgent.IsPlayerControlled && base.Mission.MainAgent.IsActive())
		{
			_playerSideNotYetRetreatingTime = MissionTime.Now;
		}
		else
		{
			bool flag2 = true;
			foreach (MissionShip allShip in _navalShipsLogic.AllShips)
			{
				if (allShip.Team != null && allShip.Team.Side == side && !allShip.IsRetreating)
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				_playerSideNotYetRetreatingTime = MissionTime.Now;
			}
		}
		if (_playerSideNotYetRetreatingTime.ElapsedSeconds > 5f)
		{
			_isPlayerSideRetreating = true;
		}
		bool flag3 = true;
		foreach (MissionShip allShip2 in _navalShipsLogic.AllShips)
		{
			if (allShip2.Team != null && allShip2.Team.Side == oppositeSide && !allShip2.IsRetreating)
			{
				flag3 = false;
				break;
			}
		}
		if (!flag3)
		{
			_enemySideNotYetRetreatingTime = MissionTime.Now;
		}
		if (_enemySideNotYetRetreatingTime.ElapsedSeconds > 5f)
		{
			IsEnemySideRetreating = true;
		}
	}

	private bool AreAnySideShipsOutOfAction(BattleSideEnum playerSide, BattleSideEnum enemySide, out bool playerShipsOutOfAction, out bool enemyShipsOutOfAction)
	{
		playerShipsOutOfAction = false;
		enemyShipsOutOfAction = false;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip.Team == null)
			{
				continue;
			}
			if (allShip.Team.Side == playerSide)
			{
				num++;
				bool flag = false;
				if (allShip.IsSunk)
				{
					flag = true;
				}
				else if (_navalAgentsLogic.GetTotalTroopCountOfShip(allShip, spawnableReservesOnly: true) <= 3)
				{
					flag = true;
				}
				if (flag)
				{
					num3++;
				}
			}
			else if (allShip.Team.Side == enemySide)
			{
				num2++;
				bool flag2 = false;
				if (allShip.IsSunk)
				{
					flag2 = true;
				}
				else if (_navalAgentsLogic.GetTotalTroopCountOfShip(allShip, spawnableReservesOnly: true) <= 3)
				{
					flag2 = true;
				}
				if (flag2)
				{
					num4++;
				}
			}
		}
		if (num > 0)
		{
			playerShipsOutOfAction = num3 == num;
		}
		if (num2 > 0)
		{
			enemyShipsOutOfAction = num4 == num2;
		}
		return playerShipsOutOfAction | enemyShipsOutOfAction;
	}

	private bool HasAnyContestedIslands(BattleSideEnum playerSide, BattleSideEnum enemySide)
	{
		ulong num = 0uL;
		ulong num2 = 0uL;
		foreach (Agent allAgent in base.Mission.AllAgents)
		{
			if (!allAgent.IsActive() || !allAgent.IsHuman || allAgent.Team == null)
			{
				continue;
			}
			AgentNavalComponent component = allAgent.GetComponent<AgentNavalComponent>();
			if (component == null)
			{
				continue;
			}
			ulong steppedCombinedShipIsland = component.GetSteppedCombinedShipIsland();
			if (steppedCombinedShipIsland != 0L)
			{
				BattleSideEnum side = allAgent.Team.Side;
				if (side == playerSide)
				{
					num |= steppedCombinedShipIsland;
				}
				else if (side == enemySide)
				{
					num2 |= steppedCombinedShipIsland;
				}
				if ((num & num2) != 0L)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnMissionResultReady(MissionResult missionResult)
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFFFFF7u));
		}
	}
}
