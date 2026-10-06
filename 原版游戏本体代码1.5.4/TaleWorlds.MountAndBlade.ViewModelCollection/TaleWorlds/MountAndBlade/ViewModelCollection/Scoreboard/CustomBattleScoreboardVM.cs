using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class CustomBattleScoreboardVM : ScoreboardBaseVM, IBattleObserver
{
	private SallyOutEndLogic _sallyOutEndLogic;

	private MissionCombatantsLogic _missionCombatantsLogic;

	private float _missionEndScoreboardDelayTimer;

	private float _moraleUpdateTimer = 1f;

	public CustomBattleScoreboardVM(BattleScoreContext scoreboardContext)
		: base(scoreboardContext)
	{
	}

	public override void Initialize(IMissionScreen missionScreen, Mission mission, Action releaseSimulationSources, Action<bool> onToggle)
	{
		base.Initialize(missionScreen, mission, releaseSimulationSources, onToggle);
		base.IsSimulation = false;
		base.SimulationResult = "NotSimulation";
		BattleObserverMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<BattleObserverMissionLogic>();
		_sallyOutEndLogic = Mission.Current.GetMissionBehavior<SallyOutEndLogic>();
		_missionCombatantsLogic = _mission.GetMissionBehavior<MissionCombatantsLogic>();
		if (_missionCombatantsLogic != null)
		{
			PlayerSide = _missionCombatantsLogic.PlayerSide;
			base.Attackers = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "attacker"), ScoreboardContext.GetAttackerBanner(), isSimulation: false, PlayerSide == BattleSideEnum.Attacker);
			base.Defenders = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "defender"), ScoreboardContext.GetDefenderBanner(), isSimulation: false, PlayerSide == BattleSideEnum.Defender);
		}
		PlayerSide = Mission.Current.PlayerTeam.Side;
		missionBehavior?.SetObserver(this);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Defenders?.RefreshValues();
		base.Attackers?.RefreshValues();
	}

	protected override void OnTick(float dt)
	{
		if (!base.IsOver)
		{
			if (!_mission.IsMissionEnding)
			{
				BattleEndLogic battleEndLogic = _battleEndLogic;
				if ((battleEndLogic == null || !battleEndLogic.IsEnemySideRetreating) && (_missionCombatantsLogic == null || _battleEndLogic == null || (!_battleEndLogic.PlayerVictory && !_battleEndLogic.EnemyVictory)))
				{
					SallyOutEndLogic sallyOutEndLogic = _sallyOutEndLogic;
					if (sallyOutEndLogic == null || !sallyOutEndLogic.IsSallyOutOver)
					{
						goto IL_008d;
					}
				}
			}
			if (_missionEndScoreboardDelayTimer < 1.5f)
			{
				_missionEndScoreboardDelayTimer += dt;
			}
			else
			{
				OnBattleOver();
			}
		}
		goto IL_008d;
		IL_008d:
		if (!base.IsSimulation && !base.IsOver)
		{
			base.MissionTimeInSeconds = (int)_mission.CurrentTime;
		}
		if (!base.IsSimulation)
		{
			_moraleUpdateTimer += dt;
			if (_moraleUpdateTimer >= 1f)
			{
				base.Attackers.Morale = GetBattleMoraleOfSide(BattleSideEnum.Attacker);
				base.Defenders.Morale = GetBattleMoraleOfSide(BattleSideEnum.Defender);
				_moraleUpdateTimer = 0f;
			}
		}
	}

	public override void ExecuteFastForwardAction()
	{
		if (base.IsMainCharacterDead)
		{
			Mission.Current.SetFastForwardingFromUI(base.IsFastForwarding);
		}
	}

	public override void ExecuteQuitAction()
	{
		OnExitBattle();
	}

	public void OnBattleOver()
	{
		Mission current = Mission.Current;
		if (current != null && current.MissionEnded)
		{
			base.IsOver = true;
			if (Mission.Current.GetMissionBehavior<SallyOutEndLogic>() != null && !Mission.Current.MissionResult.BattleResolved)
			{
				if (Mission.Current.MissionResult.BattleState == BattleState.DefenderPullBack)
				{
					base.BattleResultIndex = 2;
					base.BattleResult = GameTexts.FindText("str_battle_result_retreat").ToString();
				}
				else if (Mission.Current.MissionResult.PlayerVictory)
				{
					base.BattleResultIndex = 1;
					base.BattleResult = GameTexts.FindText("str_finished").ToString();
				}
				else
				{
					base.BattleResultIndex = 0;
					base.BattleResult = GameTexts.FindText("str_defeat").ToString();
				}
			}
			else
			{
				bool flag = Mission.Current.MissionResult?.PlayerVictory ?? false;
				base.BattleResultIndex = (flag ? 1 : 0);
				base.BattleResult = (flag ? GameTexts.FindText("str_victory").ToString() : GameTexts.FindText("str_defeat").ToString());
			}
		}
		else
		{
			BattleEndLogic battleEndLogic = _battleEndLogic;
			if (battleEndLogic != null && battleEndLogic.IsEnemySideRetreating)
			{
				base.IsOver = true;
			}
		}
	}

	public void OnExitBattle()
	{
		BasicMissionHandler missionBehavior = _mission.GetMissionBehavior<BasicMissionHandler>();
		BattleEndLogic.ExitResult exitResult = ((!_mission.MissionEnded) ? BattleEndLogic.ExitResult.NeedsPlayerConfirmation : BattleEndLogic.ExitResult.True);
		if (exitResult == BattleEndLogic.ExitResult.NeedsPlayerConfirmation)
		{
			OnToggle(obj: false);
			missionBehavior.CreateWarningWidgetForResult(exitResult);
		}
		else
		{
			_mission.EndMission();
		}
	}

	public void TroopNumberChanged(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject character, int number = 0, int numberDead = 0, int numberWounded = 0, int numberRouted = 0, int numberKilled = 0, int numberReadyToUpgrade = 0)
	{
		GetSide(side).UpdateScores(battleCombatant, isPlayerParty: false, character, number, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		base.PowerComparer.Update(base.Defenders.CurrentPower, base.Attackers.CurrentPower, base.Defenders.InitialPower, base.Attackers.InitialPower);
	}

	public void HeroSkillIncreased(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject heroCharacter, SkillObject upgradedSkill)
	{
		GetSide(side).UpdateHeroSkills(battleCombatant, isPlayerParty: false, heroCharacter, upgradedSkill);
	}

	public void BattleResultsReady()
	{
	}

	public void TroopSideChanged(BattleSideEnum prevSide, BattleSideEnum newSide, IBattleCombatant battleCombatant, BasicCharacterObject character)
	{
		SPScoreboardStatsVM scoreToBringOver = GetSide(prevSide).RemoveTroop(battleCombatant, character);
		GetSide(newSide).GetPartyAddIfNotExists(battleCombatant, isPlayerParty: false);
		GetSide(newSide).AddTroop(battleCombatant, character, scoreToBringOver);
	}
}
