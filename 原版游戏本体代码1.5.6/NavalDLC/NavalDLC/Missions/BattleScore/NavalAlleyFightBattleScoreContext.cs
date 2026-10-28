using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.BattleScore;

namespace NavalDLC.Missions.BattleScore;

public class NavalAlleyFightBattleScoreContext : BattleScoreContext
{
	public override bool IsPowerComparisonRelevant => false;

	public NavalAlleyFightBattleScoreContext(Mission mission)
	{
	}

	public override Banner GetAttackerBanner()
	{
		return null;
	}

	public override Banner GetDefenderBanner()
	{
		return null;
	}
}
