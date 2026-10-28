using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions.BattleScore;

namespace SandBox.Missions.BattleScore;

public class SandboxSimulationBattleScoreContext : BattleScoreContext
{
	private readonly BattleSimulation _battleSimulation;

	public override bool IsPowerComparisonRelevant => true;

	public SandboxSimulationBattleScoreContext(BattleSimulation battleSimulation)
	{
		_battleSimulation = battleSimulation;
	}

	public override Banner GetAttackerBanner()
	{
		return _battleSimulation.MapEvent.AttackerSide.LeaderParty.Banner;
	}

	public override Banner GetDefenderBanner()
	{
		return _battleSimulation.MapEvent.DefenderSide.LeaderParty.Banner;
	}
}
