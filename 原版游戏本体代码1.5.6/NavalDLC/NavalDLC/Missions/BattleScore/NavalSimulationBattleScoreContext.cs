using SandBox.Missions.BattleScore;
using TaleWorlds.CampaignSystem;

namespace NavalDLC.Missions.BattleScore;

public class NavalSimulationBattleScoreContext : SandboxSimulationBattleScoreContext
{
	public NavalSimulationBattleScoreContext(BattleSimulation battleSimulation)
		: base(battleSimulation)
	{
	}
}
