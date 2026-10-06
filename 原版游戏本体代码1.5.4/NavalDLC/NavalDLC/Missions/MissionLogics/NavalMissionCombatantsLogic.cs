using System.Collections.Generic;
using NavalDLC.Missions.AI.Tactics;
using NavalDLC.Missions.AI.TeamAI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalMissionCombatantsLogic : MissionCombatantsLogic
{
	public NavalMissionCombatantsLogic(IEnumerable<IBattleCombatant> battleCombatants, IBattleCombatant playerBattleCombatant, IBattleCombatant defenderLeaderBattleCombatant, IBattleCombatant attackerLeaderBattleCombatant, Mission.MissionTeamAITypeEnum teamAIType, bool isPlayerSergeant)
		: base(battleCombatants, playerBattleCombatant, defenderLeaderBattleCombatant, attackerLeaderBattleCombatant, teamAIType, isPlayerSergeant)
	{
	}

	public override void EarlyStart()
	{
		Mission.Current.MissionTeamAIType = TeamAIType;
		foreach (Team team in Mission.Current.Teams)
		{
			if (TeamAIType == Mission.MissionTeamAITypeEnum.NavalBattle)
			{
				team.AddTeamAI(new TeamAINavalComponent(base.Mission, team, 5f, 1f));
			}
			else if (TeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid)
			{
				if (team.IsAttacker)
				{
					team.AddTeamAI(new TeamAINavalRaidAttackerComponent(base.Mission, team, 5f, 1f));
				}
				else
				{
					team.AddTeamAI(new TeamAINavalRaidDefenderComponent(base.Mission, team, 5f));
				}
			}
		}
		if (Mission.Current.Teams.Count <= 0)
		{
			return;
		}
		foreach (Team team2 in Mission.Current.Teams)
		{
			if (!team2.HasTeamAi)
			{
				continue;
			}
			if (TeamAIType == Mission.MissionTeamAITypeEnum.NavalBattle)
			{
				team2.AddTacticOption(new TacticNavalBalancedOffense(team2));
				if (team2.Side == BattleSideEnum.Defender)
				{
					team2.AddTacticOption(new TacticNavalLineDefense(team2));
				}
			}
			else if (TeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid)
			{
				team2.AddTacticOption(new TacticCharge(team2));
				if (team2.Side == BattleSideEnum.Defender)
				{
					team2.AddTacticOption(new TacticNavalRaidDefense(team2));
				}
			}
		}
		foreach (Team team3 in base.Mission.Teams)
		{
			team3.QuerySystem.Expire();
			team3.ResetTactic();
		}
	}
}
