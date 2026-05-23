using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C6 RID: 1222
	public static class SiegeAftermathAction
	{
		// Token: 0x06004A24 RID: 18980 RVA: 0x00175901 File Offset: 0x00173B01
		private static void ApplyInternal(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
		{
			CampaignEventDispatcher.Instance.OnSiegeAftermathApplied(attackerParty, settlement, aftermathType, previousSettlementOwner, partyContributions);
		}

		// Token: 0x06004A25 RID: 18981 RVA: 0x00175913 File Offset: 0x00173B13
		public static void ApplyAftermath(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
		{
			SiegeAftermathAction.ApplyInternal(attackerParty, settlement, aftermathType, previousSettlementOwner, partyContributions);
		}

		// Token: 0x02000897 RID: 2199
		public enum SiegeAftermath
		{
			// Token: 0x04002468 RID: 9320
			Devastate,
			// Token: 0x04002469 RID: 9321
			Pillage,
			// Token: 0x0400246A RID: 9322
			ShowMercy
		}
	}
}
