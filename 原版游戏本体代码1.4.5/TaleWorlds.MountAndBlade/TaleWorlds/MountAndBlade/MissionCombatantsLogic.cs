using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class MissionCombatantsLogic : MissionLogic
{
	protected readonly IEnumerable<IBattleCombatant> BattleCombatants;

	protected readonly IBattleCombatant PlayerBattleCombatant;

	protected readonly IBattleCombatant DefenderLeaderBattleCombatant;

	protected readonly IBattleCombatant AttackerLeaderBattleCombatant;

	protected readonly Mission.MissionTeamAITypeEnum TeamAIType;

	protected readonly bool IsPlayerSergeant;

	public BattleSideEnum PlayerSide
	{
		get
		{
			if (PlayerBattleCombatant == null)
			{
				return BattleSideEnum.None;
			}
			if (PlayerBattleCombatant != DefenderLeaderBattleCombatant)
			{
				return BattleSideEnum.Attacker;
			}
			return BattleSideEnum.Defender;
		}
	}

	public MissionCombatantsLogic(IEnumerable<IBattleCombatant> battleCombatants, IBattleCombatant playerBattleCombatant, IBattleCombatant defenderLeaderBattleCombatant, IBattleCombatant attackerLeaderBattleCombatant, Mission.MissionTeamAITypeEnum teamAIType, bool isPlayerSergeant)
	{
		if (battleCombatants == null)
		{
			battleCombatants = new IBattleCombatant[2] { defenderLeaderBattleCombatant, attackerLeaderBattleCombatant };
		}
		BattleCombatants = battleCombatants;
		PlayerBattleCombatant = playerBattleCombatant;
		DefenderLeaderBattleCombatant = defenderLeaderBattleCombatant;
		AttackerLeaderBattleCombatant = attackerLeaderBattleCombatant;
		TeamAIType = teamAIType;
		IsPlayerSergeant = isPlayerSergeant;
	}

	public Banner GetBannerForSide(BattleSideEnum side)
	{
		if (side != BattleSideEnum.Defender)
		{
			return AttackerLeaderBattleCombatant.Banner;
		}
		return DefenderLeaderBattleCombatant.Banner;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		if (!base.Mission.Teams.IsEmpty())
		{
			throw new MBIllegalValueException("Number of teams is not 0.");
		}
		BattleSideEnum playerSide = PlayerBattleCombatant.Side;
		BattleSideEnum oppositeSide = playerSide.GetOppositeSide();
		if (playerSide == BattleSideEnum.Defender)
		{
			AddPlayerTeam(playerSide);
		}
		else
		{
			AddEnemyTeam(oppositeSide);
		}
		if (playerSide == BattleSideEnum.Attacker)
		{
			AddPlayerTeam(playerSide);
		}
		else
		{
			AddEnemyTeam(oppositeSide);
		}
		bool isNavalLandHybridMission = TeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid;
		if (SupportsAllyTeamOnPlayerSide(BattleCombatants.Where((IBattleCombatant cmbt) => cmbt.Side == playerSide), PlayerBattleCombatant, IsPlayerSergeant, isNavalLandHybridMission, out var allyCombatant))
		{
			AddPlayerAllyTeam(playerSide, allyCombatant);
		}
	}

	public override void EarlyStart()
	{
		Mission.Current.MissionTeamAIType = TeamAIType;
		switch (TeamAIType)
		{
		case Mission.MissionTeamAITypeEnum.FieldBattle:
			foreach (Team team2 in Mission.Current.Teams)
			{
				team2.AddTeamAI(new TeamAIGeneral(base.Mission, team2));
			}
			break;
		case Mission.MissionTeamAITypeEnum.Siege:
			foreach (Team team3 in Mission.Current.Teams)
			{
				if (team3.Side == BattleSideEnum.Attacker)
				{
					team3.AddTeamAI(new TeamAISiegeAttacker(base.Mission, team3, 5f, 1f));
				}
				if (team3.Side == BattleSideEnum.Defender)
				{
					team3.AddTeamAI(new TeamAISiegeDefender(base.Mission, team3, 5f, 1f));
				}
			}
			break;
		case Mission.MissionTeamAITypeEnum.SallyOut:
			foreach (Team team4 in Mission.Current.Teams)
			{
				if (team4.Side == BattleSideEnum.Attacker)
				{
					team4.AddTeamAI(new TeamAISallyOutDefender(base.Mission, team4, 5f, 1f));
				}
				else
				{
					team4.AddTeamAI(new TeamAISallyOutAttacker(base.Mission, team4, 5f, 1f));
				}
			}
			break;
		}
		if (Mission.Current.Teams.Count <= 0)
		{
			return;
		}
		switch (Mission.Current.MissionTeamAIType)
		{
		case Mission.MissionTeamAITypeEnum.NoTeamAI:
			foreach (Team team5 in Mission.Current.Teams)
			{
				if (team5.HasTeamAi)
				{
					team5.AddTacticOption(new TacticCharge(team5));
				}
			}
			break;
		case Mission.MissionTeamAITypeEnum.FieldBattle:
			foreach (Team team in Mission.Current.Teams)
			{
				if (!team.HasTeamAi)
				{
					continue;
				}
				int num = BattleCombatants.Where((IBattleCombatant bc) => bc.Side == team.Side).Max((IBattleCombatant bcs) => bcs.GetTacticsSkillAmount());
				team.AddTacticOption(new TacticCharge(team));
				if ((float)num >= 20f)
				{
					team.AddTacticOption(new TacticFullScaleAttack(team));
					if (team.Side == BattleSideEnum.Defender)
					{
						team.AddTacticOption(new TacticDefensiveEngagement(team));
						team.AddTacticOption(new TacticDefensiveLine(team));
					}
					if (team.Side == BattleSideEnum.Attacker)
					{
						team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
					}
				}
				if ((float)num >= 50f)
				{
					team.AddTacticOption(new TacticFrontalCavalryCharge(team));
					if (team.Side == BattleSideEnum.Defender)
					{
						team.AddTacticOption(new TacticDefensiveRing(team));
						team.AddTacticOption(new TacticHoldChokePoint(team));
					}
					if (team.Side == BattleSideEnum.Attacker)
					{
						team.AddTacticOption(new TacticCoordinatedRetreat(team));
					}
				}
			}
			break;
		case Mission.MissionTeamAITypeEnum.Siege:
			foreach (Team team6 in Mission.Current.Teams)
			{
				if (team6.HasTeamAi)
				{
					if (team6.Side == BattleSideEnum.Attacker)
					{
						team6.AddTacticOption(new TacticBreachWalls(team6));
					}
					if (team6.Side == BattleSideEnum.Defender)
					{
						team6.AddTacticOption(new TacticDefendCastle(team6));
					}
				}
			}
			break;
		case Mission.MissionTeamAITypeEnum.SallyOut:
			foreach (Team team7 in Mission.Current.Teams)
			{
				if (team7.HasTeamAi)
				{
					if (team7.Side == BattleSideEnum.Defender)
					{
						team7.AddTacticOption(new TacticSallyOutHitAndRun(team7));
					}
					if (team7.Side == BattleSideEnum.Attacker)
					{
						team7.AddTacticOption(new TacticSallyOutDefense(team7));
					}
					team7.AddTacticOption(new TacticCharge(team7));
				}
			}
			break;
		}
		foreach (Team team8 in base.Mission.Teams)
		{
			team8.QuerySystem.Expire();
			team8.ResetTactic();
		}
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
	}

	public IEnumerable<IBattleCombatant> GetAllCombatants()
	{
		foreach (IBattleCombatant battleCombatant in BattleCombatants)
		{
			yield return battleCombatant;
		}
	}

	protected void AddPlayerTeam(BattleSideEnum playerSide)
	{
		base.Mission.Teams.Add(playerSide, PlayerBattleCombatant.PrimaryColorPair.Item1, PlayerBattleCombatant.PrimaryColorPair.Item2, PlayerBattleCombatant.Banner);
		base.Mission.PlayerTeam = ((playerSide == BattleSideEnum.Attacker) ? base.Mission.AttackerTeam : base.Mission.DefenderTeam);
	}

	protected void AddEnemyTeam(BattleSideEnum enemySide)
	{
		IBattleCombatant battleCombatant = ((enemySide == BattleSideEnum.Attacker) ? AttackerLeaderBattleCombatant : DefenderLeaderBattleCombatant);
		base.Mission.Teams.Add(enemySide, battleCombatant.PrimaryColorPair.Item1, battleCombatant.PrimaryColorPair.Item2, battleCombatant.Banner);
	}

	protected void AddPlayerAllyTeam(BattleSideEnum playerSide, IBattleCombatant allyCombatant)
	{
		base.Mission.Teams.Add(playerSide, allyCombatant.PrimaryColorPair.Item1, allyCombatant.PrimaryColorPair.Item2, allyCombatant.Banner);
		if (playerSide != BattleSideEnum.Attacker)
		{
			_ = base.Mission.DefenderAllyTeam;
		}
		else
		{
			_ = base.Mission.AttackerAllyTeam;
		}
	}

	public static bool SupportsAllyTeamOnPlayerSide(IEnumerable<IBattleCombatant> playerSideBattleCombatants, IBattleCombatant playerBattleCombatant, bool isPlayerSergeant, bool isNavalLandHybridMission, out IBattleCombatant allyCombatant)
	{
		allyCombatant = null;
		if (isNavalLandHybridMission)
		{
			return false;
		}
		BasicCharacterObject general = playerBattleCombatant.General;
		foreach (IBattleCombatant playerSideBattleCombatant in playerSideBattleCombatants)
		{
			if (playerSideBattleCombatant != playerBattleCombatant && (!isPlayerSergeant || playerSideBattleCombatant.General != general) && !playerSideBattleCombatant.IsUnderPlayersCommand(playerBattleCombatant.Side))
			{
				allyCombatant = playerSideBattleCombatant;
				return true;
			}
		}
		return false;
	}
}
