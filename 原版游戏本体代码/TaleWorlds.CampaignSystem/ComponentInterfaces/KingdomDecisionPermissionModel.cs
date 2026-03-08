using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A8 RID: 424
	public abstract class KingdomDecisionPermissionModel : MBGameModel<KingdomDecisionPermissionModel>
	{
		// Token: 0x06001CD5 RID: 7381
		public abstract bool IsPolicyDecisionAllowed(PolicyObject policy);

		// Token: 0x06001CD6 RID: 7382
		public abstract bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

		// Token: 0x06001CD7 RID: 7383
		public abstract bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

		// Token: 0x06001CD8 RID: 7384
		public abstract bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason);

		// Token: 0x06001CD9 RID: 7385
		public abstract bool IsAnnexationDecisionAllowed(Settlement annexedSettlement);

		// Token: 0x06001CDA RID: 7386
		public abstract bool IsExpulsionDecisionAllowed(Clan expelledClan);

		// Token: 0x06001CDB RID: 7387
		public abstract bool IsKingSelectionDecisionAllowed(Kingdom kingdom);
	}
}
