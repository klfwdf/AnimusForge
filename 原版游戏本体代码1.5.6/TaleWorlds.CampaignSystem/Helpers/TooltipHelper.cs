using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public class TooltipHelper
{
	public static TextObject GetSendTroopsPowerContextTooltipForMapEvent()
	{
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		MapEvent.PowerCalculationContext simulationContext = playerMapEvent.SimulationContext;
		string text = simulationContext.ToString();
		if (simulationContext == MapEvent.PowerCalculationContext.Village || simulationContext == MapEvent.PowerCalculationContext.NavalRaid || simulationContext == MapEvent.PowerCalculationContext.RiverCrossingBattle || simulationContext == MapEvent.PowerCalculationContext.Siege)
		{
			text += ((playerMapEvent.PlayerSide == playerMapEvent.AttackerSide.MissionSide) ? "Attacker" : "Defender");
		}
		return GameTexts.FindText("str_simulation_tooltip", text);
	}

	public static TextObject GetSendTroopsPowerContextTooltipForSiege()
	{
		return GameTexts.FindText("str_simulation_tooltip", (PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? "SiegeAttacker" : "SiegeDefender");
	}
}
