using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BF RID: 447
	public abstract class TradeAgreementModel : MBGameModel<TradeAgreementModel>
	{
		// Token: 0x06001D91 RID: 7569
		public abstract CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom);

		// Token: 0x06001D92 RID: 7570
		public abstract int GetMaximumTradeAgreementCount(Kingdom kingdom);

		// Token: 0x06001D93 RID: 7571
		public abstract int GetInfluenceCostOfProposingTradeAgreement(Clan clan);

		// Token: 0x06001D94 RID: 7572
		public abstract float GetScoreOfStartingTradeAgreement(Kingdom kingdom, Kingdom targetKingdom, Clan clan, out TextObject explanation, bool includeExplanation = false);

		// Token: 0x06001D95 RID: 7573
		public abstract bool CanMakeTradeAgreement(Kingdom kingdom, Kingdom other, bool checkOtherSideTradeSupport, out TextObject reason, bool includeReason = false);
	}
}
