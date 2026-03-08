using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x0200001F RID: 31
	public class TooltipHelper
	{
		// Token: 0x0600010B RID: 267 RVA: 0x0000D46C File Offset: 0x0000B66C
		public static TextObject GetSendTroopsPowerContextTooltipForMapEvent()
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			MapEvent.PowerCalculationContext simulationContext = playerMapEvent.SimulationContext;
			string text = simulationContext.ToString();
			if (simulationContext == MapEvent.PowerCalculationContext.Village || simulationContext == MapEvent.PowerCalculationContext.RiverCrossingBattle || simulationContext == MapEvent.PowerCalculationContext.Siege)
			{
				text += ((playerMapEvent.PlayerSide == playerMapEvent.AttackerSide.MissionSide) ? "Attacker" : "Defender");
			}
			return GameTexts.FindText("str_simulation_tooltip", text);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000D4D1 File Offset: 0x0000B6D1
		public static TextObject GetSendTroopsPowerContextTooltipForSiege()
		{
			return GameTexts.FindText("str_simulation_tooltip", (PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? "SiegeAttacker" : "SiegeDefender");
		}
	}
}
