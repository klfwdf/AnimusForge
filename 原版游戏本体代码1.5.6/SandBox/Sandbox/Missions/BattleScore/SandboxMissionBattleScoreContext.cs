using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.BattleScore;

namespace SandBox.Missions.BattleScore;

public class SandboxMissionBattleScoreContext : BattleScoreContext
{
	private readonly Mission _mission;

	public override bool IsPowerComparisonRelevant => true;

	public SandboxMissionBattleScoreContext(Mission mission)
	{
		_mission = mission;
	}

	public override Banner GetAttackerBanner()
	{
		if (Campaign.Current == null)
		{
			return null;
		}
		object obj = PlayerEncounter.Battle?.AttackerSide?.LeaderParty.Banner;
		if (obj == null)
		{
			Mission mission = _mission;
			if (mission == null)
			{
				return null;
			}
			obj = mission.Teams.Attacker.Banner;
		}
		return (Banner)obj;
	}

	public override Banner GetDefenderBanner()
	{
		if (Campaign.Current == null)
		{
			return null;
		}
		object obj = PlayerEncounter.Battle?.DefenderSide?.LeaderParty.Banner;
		if (obj == null)
		{
			Mission mission = _mission;
			if (mission == null)
			{
				return null;
			}
			obj = mission.Teams.Defender.Banner;
		}
		return (Banner)obj;
	}
}
