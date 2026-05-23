using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementLoyaltyModel : MBGameModel<SettlementLoyaltyModel>
{
	public abstract int SettlementLoyaltyChangeDueToSecurityThreshold { get; }

	public abstract int MaximumLoyaltyInSettlement { get; }

	public abstract int LoyaltyDriftMedium { get; }

	public abstract float HighLoyaltyProsperityEffect { get; }

	public abstract int LowLoyaltyProsperityEffect { get; }

	public abstract int MilitiaBoostPercentage { get; }

	public abstract float HighSecurityLoyaltyEffect { get; }

	public abstract float LowSecurityLoyaltyEffect { get; }

	public abstract float GovernorSameCultureLoyaltyEffect { get; }

	public abstract float GovernorDifferentCultureLoyaltyEffect { get; }

	public abstract float SettlementOwnerDifferentCultureLoyaltyEffect { get; }

	public abstract int ThresholdForTaxBoost { get; }

	public abstract int RebellionStartLoyaltyThreshold { get; }

	public abstract int ThresholdForTaxCorruption { get; }

	public abstract int ThresholdForHigherTaxCorruption { get; }

	public abstract int ThresholdForProsperityBoost { get; }

	public abstract int ThresholdForProsperityPenalty { get; }

	public abstract int AdditionalStarvationPenaltyStartDay { get; }

	public abstract int AdditionalStarvationLoyaltyEffect { get; }

	public abstract int RebelliousStateStartLoyaltyThreshold { get; }

	public abstract int LoyaltyBoostAfterRebellionStartValue { get; }

	public abstract float ThresholdForNotableRelationBonus { get; }

	public abstract int DailyNotableRelationBonus { get; }

	public abstract ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false);

	public abstract void CalculateGoldGainDueToHighLoyalty(Town town, ref ExplainedNumber explainedNumber);

	public abstract void CalculateGoldCutDueToLowLoyalty(Town town, ref ExplainedNumber explainedNumber);
}
