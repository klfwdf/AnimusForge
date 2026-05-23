using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004AE RID: 1198
	public static class EndMercenaryServiceAction
	{
		// Token: 0x060049BC RID: 18876 RVA: 0x00172E1A File Offset: 0x0017101A
		private static void Apply(Clan clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
		{
			clan.EndMercenaryService(details == EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByLeavingKingdom);
			CampaignEventDispatcher.Instance.OnMercenaryServiceEnded(clan, details);
		}

		// Token: 0x060049BD RID: 18877 RVA: 0x00172E32 File Offset: 0x00171032
		public static void EndByDefault(Clan clan)
		{
			EndMercenaryServiceAction.Apply(clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByDefault);
		}

		// Token: 0x060049BE RID: 18878 RVA: 0x00172E3B File Offset: 0x0017103B
		public static void EndByLeavingKingdom(Clan clan)
		{
			EndMercenaryServiceAction.Apply(clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByLeavingKingdom);
		}

		// Token: 0x060049BF RID: 18879 RVA: 0x00172E44 File Offset: 0x00171044
		public static void EndByBecomingVassal(Clan clan)
		{
			EndMercenaryServiceAction.Apply(clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails.ApplyByBecomingVassal);
		}

		// Token: 0x0200088A RID: 2186
		public enum EndMercenaryServiceActionDetails
		{
			// Token: 0x04002428 RID: 9256
			ApplyByDefault,
			// Token: 0x04002429 RID: 9257
			ApplyByLeavingKingdom,
			// Token: 0x0400242A RID: 9258
			ApplyByBecomingVassal
		}
	}
}
