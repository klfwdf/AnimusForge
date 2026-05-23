using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C9 RID: 457
	public abstract class SettlementSecurityModel : MBGameModel<SettlementSecurityModel>
	{
		// Token: 0x1700076B RID: 1899
		// (get) Token: 0x06001DDD RID: 7645
		public abstract int MaximumSecurityInSettlement { get; }

		// Token: 0x1700076C RID: 1900
		// (get) Token: 0x06001DDE RID: 7646
		public abstract int SecurityDriftMedium { get; }

		// Token: 0x1700076D RID: 1901
		// (get) Token: 0x06001DDF RID: 7647
		public abstract float MapEventSecurityEffectRadius { get; }

		// Token: 0x1700076E RID: 1902
		// (get) Token: 0x06001DE0 RID: 7648
		public abstract float HideoutClearedSecurityEffectRadius { get; }

		// Token: 0x1700076F RID: 1903
		// (get) Token: 0x06001DE1 RID: 7649
		public abstract int HideoutClearedSecurityGain { get; }

		// Token: 0x17000770 RID: 1904
		// (get) Token: 0x06001DE2 RID: 7650
		public abstract int ThresholdForTaxCorruption { get; }

		// Token: 0x17000771 RID: 1905
		// (get) Token: 0x06001DE3 RID: 7651
		public abstract int ThresholdForHigherTaxCorruption { get; }

		// Token: 0x17000772 RID: 1906
		// (get) Token: 0x06001DE4 RID: 7652
		public abstract int ThresholdForTaxBoost { get; }

		// Token: 0x17000773 RID: 1907
		// (get) Token: 0x06001DE5 RID: 7653
		public abstract int SettlementTaxBoostPercentage { get; }

		// Token: 0x17000774 RID: 1908
		// (get) Token: 0x06001DE6 RID: 7654
		public abstract int SettlementTaxPenaltyPercentage { get; }

		// Token: 0x06001DE7 RID: 7655
		public abstract float GetLootedNearbyPartySecurityEffect(Town town, float sumOfAttackedPartyStrengths);

		// Token: 0x17000775 RID: 1909
		// (get) Token: 0x06001DE8 RID: 7656
		public abstract int ThresholdForNotableRelationBonus { get; }

		// Token: 0x17000776 RID: 1910
		// (get) Token: 0x06001DE9 RID: 7657
		public abstract int ThresholdForNotableRelationPenalty { get; }

		// Token: 0x17000777 RID: 1911
		// (get) Token: 0x06001DEA RID: 7658
		public abstract int DailyNotableRelationBonus { get; }

		// Token: 0x17000778 RID: 1912
		// (get) Token: 0x06001DEB RID: 7659
		public abstract int DailyNotableRelationPenalty { get; }

		// Token: 0x17000779 RID: 1913
		// (get) Token: 0x06001DEC RID: 7660
		public abstract int DailyNotablePowerBonus { get; }

		// Token: 0x1700077A RID: 1914
		// (get) Token: 0x06001DED RID: 7661
		public abstract int DailyNotablePowerPenalty { get; }

		// Token: 0x06001DEE RID: 7662
		public abstract ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false);

		// Token: 0x06001DEF RID: 7663
		public abstract float GetNearbyBanditPartyDefeatedSecurityEffect(Town town, float sumOfAttackedPartyStrengths);

		// Token: 0x06001DF0 RID: 7664
		public abstract void CalculateGoldGainDueToHighSecurity(Town town, ref ExplainedNumber explainedNumber);

		// Token: 0x06001DF1 RID: 7665
		public abstract void CalculateGoldCutDueToLowSecurity(Town town, ref ExplainedNumber explainedNumber);
	}
}
