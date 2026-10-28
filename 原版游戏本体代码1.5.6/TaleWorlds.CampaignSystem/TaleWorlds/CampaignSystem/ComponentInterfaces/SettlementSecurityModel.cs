using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementSecurityModel : MBGameModel<SettlementSecurityModel>
{
	public abstract int MaximumSecurityInSettlement { get; }

	public abstract int SecurityDriftMedium { get; }

	public abstract float MapEventSecurityEffectRadius { get; }

	public abstract float HideoutClearedSecurityEffectRadius { get; }

	public abstract int HideoutClearedSecurityGain { get; }

	public abstract int ThresholdForTaxCorruption { get; }

	public abstract int ThresholdForHigherTaxCorruption { get; }

	public abstract int ThresholdForTaxBoost { get; }

	public abstract int SettlementTaxBoostPercentage { get; }

	public abstract int SettlementTaxPenaltyPercentage { get; }

	public abstract int ThresholdForNotableRelationBonus { get; }

	public abstract int ThresholdForNotableRelationPenalty { get; }

	public abstract int DailyNotableRelationBonus { get; }

	public abstract int DailyNotableRelationPenalty { get; }

	public abstract int DailyNotablePowerBonus { get; }

	public abstract int DailyNotablePowerPenalty { get; }

	public abstract float GetLootedNearbyPartySecurityEffect(Town town, float sumOfAttackedPartyStrengths);

	public abstract ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false);

	public abstract float GetNearbyBanditPartyDefeatedSecurityEffect(Town town, float sumOfAttackedPartyStrengths);

	public abstract void CalculateGoldGainDueToHighSecurity(Town town, ref ExplainedNumber explainedNumber);

	public abstract void CalculateGoldCutDueToLowSecurity(Town town, ref ExplainedNumber explainedNumber);
}
