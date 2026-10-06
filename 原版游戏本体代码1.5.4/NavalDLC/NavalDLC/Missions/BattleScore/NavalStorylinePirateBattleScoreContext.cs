using SandBox.Missions.BattleScore;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.BattleScore;

public class NavalStorylinePirateBattleScoreContext : SandboxMissionBattleScoreContext
{
	public override bool IsPowerComparisonRelevant => false;

	public NavalStorylinePirateBattleScoreContext(Mission mission)
		: base(mission)
	{
	}
}
