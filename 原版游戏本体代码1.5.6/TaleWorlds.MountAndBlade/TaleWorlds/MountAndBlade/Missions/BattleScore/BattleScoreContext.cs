using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Missions.BattleScore;

public abstract class BattleScoreContext
{
	public abstract bool IsPowerComparisonRelevant { get; }

	public abstract Banner GetAttackerBanner();

	public abstract Banner GetDefenderBanner();
}
