using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C8 RID: 456
	public abstract class SettlementLoyaltyModel : MBGameModel<SettlementLoyaltyModel>
	{
		// Token: 0x17000754 RID: 1876
		// (get) Token: 0x06001DC2 RID: 7618
		public abstract int SettlementLoyaltyChangeDueToSecurityThreshold { get; }

		// Token: 0x17000755 RID: 1877
		// (get) Token: 0x06001DC3 RID: 7619
		public abstract int MaximumLoyaltyInSettlement { get; }

		// Token: 0x17000756 RID: 1878
		// (get) Token: 0x06001DC4 RID: 7620
		public abstract int LoyaltyDriftMedium { get; }

		// Token: 0x17000757 RID: 1879
		// (get) Token: 0x06001DC5 RID: 7621
		public abstract float HighLoyaltyProsperityEffect { get; }

		// Token: 0x17000758 RID: 1880
		// (get) Token: 0x06001DC6 RID: 7622
		public abstract int LowLoyaltyProsperityEffect { get; }

		// Token: 0x17000759 RID: 1881
		// (get) Token: 0x06001DC7 RID: 7623
		public abstract int MilitiaBoostPercentage { get; }

		// Token: 0x1700075A RID: 1882
		// (get) Token: 0x06001DC8 RID: 7624
		public abstract float HighSecurityLoyaltyEffect { get; }

		// Token: 0x1700075B RID: 1883
		// (get) Token: 0x06001DC9 RID: 7625
		public abstract float LowSecurityLoyaltyEffect { get; }

		// Token: 0x1700075C RID: 1884
		// (get) Token: 0x06001DCA RID: 7626
		public abstract float GovernorSameCultureLoyaltyEffect { get; }

		// Token: 0x1700075D RID: 1885
		// (get) Token: 0x06001DCB RID: 7627
		public abstract float GovernorDifferentCultureLoyaltyEffect { get; }

		// Token: 0x1700075E RID: 1886
		// (get) Token: 0x06001DCC RID: 7628
		public abstract float SettlementOwnerDifferentCultureLoyaltyEffect { get; }

		// Token: 0x1700075F RID: 1887
		// (get) Token: 0x06001DCD RID: 7629
		public abstract int ThresholdForTaxBoost { get; }

		// Token: 0x17000760 RID: 1888
		// (get) Token: 0x06001DCE RID: 7630
		public abstract int RebellionStartLoyaltyThreshold { get; }

		// Token: 0x17000761 RID: 1889
		// (get) Token: 0x06001DCF RID: 7631
		public abstract int ThresholdForTaxCorruption { get; }

		// Token: 0x17000762 RID: 1890
		// (get) Token: 0x06001DD0 RID: 7632
		public abstract int ThresholdForHigherTaxCorruption { get; }

		// Token: 0x17000763 RID: 1891
		// (get) Token: 0x06001DD1 RID: 7633
		public abstract int ThresholdForProsperityBoost { get; }

		// Token: 0x17000764 RID: 1892
		// (get) Token: 0x06001DD2 RID: 7634
		public abstract int ThresholdForProsperityPenalty { get; }

		// Token: 0x17000765 RID: 1893
		// (get) Token: 0x06001DD3 RID: 7635
		public abstract int AdditionalStarvationPenaltyStartDay { get; }

		// Token: 0x17000766 RID: 1894
		// (get) Token: 0x06001DD4 RID: 7636
		public abstract int AdditionalStarvationLoyaltyEffect { get; }

		// Token: 0x17000767 RID: 1895
		// (get) Token: 0x06001DD5 RID: 7637
		public abstract int RebelliousStateStartLoyaltyThreshold { get; }

		// Token: 0x17000768 RID: 1896
		// (get) Token: 0x06001DD6 RID: 7638
		public abstract int LoyaltyBoostAfterRebellionStartValue { get; }

		// Token: 0x17000769 RID: 1897
		// (get) Token: 0x06001DD7 RID: 7639
		public abstract float ThresholdForNotableRelationBonus { get; }

		// Token: 0x1700076A RID: 1898
		// (get) Token: 0x06001DD8 RID: 7640
		public abstract int DailyNotableRelationBonus { get; }

		// Token: 0x06001DD9 RID: 7641
		public abstract ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false);

		// Token: 0x06001DDA RID: 7642
		public abstract void CalculateGoldGainDueToHighLoyalty(Town town, ref ExplainedNumber explainedNumber);

		// Token: 0x06001DDB RID: 7643
		public abstract void CalculateGoldCutDueToLowLoyalty(Town town, ref ExplainedNumber explainedNumber);
	}
}
