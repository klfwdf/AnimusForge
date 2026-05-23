using System;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200040D RID: 1037
	public interface ITradeAgreementsCampaignBehavior
	{
		// Token: 0x060040C1 RID: 16577
		void MakeTradeAgreement(Kingdom kingdom1, Kingdom kingdom2, CampaignTime duration);

		// Token: 0x060040C2 RID: 16578
		bool HasTradeAgreement(Kingdom kingdom, Kingdom other);

		// Token: 0x060040C3 RID: 16579
		void EndTradeAgreement(Kingdom kingdom, Kingdom other);

		// Token: 0x060040C4 RID: 16580
		void OnTradeAgreementOfferedToPlayer(Kingdom fromKingdom);

		// Token: 0x060040C5 RID: 16581
		CampaignTime GetTradeAgreementEndDate(Kingdom kingdom, Kingdom other);
	}
}
