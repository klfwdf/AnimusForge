using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B9 RID: 1209
	public static class LiftSiegeAction
	{
		// Token: 0x060049F5 RID: 18933 RVA: 0x00174457 File Offset: 0x00172657
		private static void ApplyInternal(MobileParty side1Party, Settlement settlement)
		{
			settlement.SiegeEvent.BesiegerCamp.RemoveAllSiegeParties();
		}

		// Token: 0x060049F6 RID: 18934 RVA: 0x00174469 File Offset: 0x00172669
		public static void GetGameAction(MobileParty side1Party)
		{
			LiftSiegeAction.ApplyInternal(side1Party, side1Party.BesiegedSettlement);
		}
	}
}
