using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class AnimusForgeSettlementLoyaltyModel : SettlementLoyaltyModel
{
	private static readonly TextObject KingdomStabilityText = new TextObject("王国稳定度与王室直辖地");

	private readonly SettlementLoyaltyModel _inner;

	public AnimusForgeSettlementLoyaltyModel(SettlementLoyaltyModel inner)
	{
		_inner = inner ?? new DefaultSettlementLoyaltyModel();
	}

	public override int SettlementLoyaltyChangeDueToSecurityThreshold => _inner.SettlementLoyaltyChangeDueToSecurityThreshold;

	public override int MaximumLoyaltyInSettlement => _inner.MaximumLoyaltyInSettlement;

	public override int LoyaltyDriftMedium => _inner.LoyaltyDriftMedium;

	public override float HighLoyaltyProsperityEffect => _inner.HighLoyaltyProsperityEffect;

	public override int LowLoyaltyProsperityEffect => _inner.LowLoyaltyProsperityEffect;

	public override int MilitiaBoostPercentage => _inner.MilitiaBoostPercentage;

	public override float HighSecurityLoyaltyEffect => _inner.HighSecurityLoyaltyEffect;

	public override float LowSecurityLoyaltyEffect => _inner.LowSecurityLoyaltyEffect;

	public override float GovernorSameCultureLoyaltyEffect => _inner.GovernorSameCultureLoyaltyEffect;

	public override float GovernorDifferentCultureLoyaltyEffect => _inner.GovernorDifferentCultureLoyaltyEffect;

	public override float SettlementOwnerDifferentCultureLoyaltyEffect => _inner.SettlementOwnerDifferentCultureLoyaltyEffect;

	public override int ThresholdForTaxBoost => _inner.ThresholdForTaxBoost;

	public override int RebellionStartLoyaltyThreshold => _inner.RebellionStartLoyaltyThreshold;

	public override int ThresholdForTaxCorruption => _inner.ThresholdForTaxCorruption;

	public override int ThresholdForHigherTaxCorruption => _inner.ThresholdForHigherTaxCorruption;

	public override int ThresholdForProsperityBoost => _inner.ThresholdForProsperityBoost;

	public override int ThresholdForProsperityPenalty => _inner.ThresholdForProsperityPenalty;

	public override int AdditionalStarvationPenaltyStartDay => _inner.AdditionalStarvationPenaltyStartDay;

	public override int AdditionalStarvationLoyaltyEffect => _inner.AdditionalStarvationLoyaltyEffect;

	public override int RebelliousStateStartLoyaltyThreshold => _inner.RebelliousStateStartLoyaltyThreshold;

	public override int LoyaltyBoostAfterRebellionStartValue => _inner.LoyaltyBoostAfterRebellionStartValue;

	public override float ThresholdForNotableRelationBonus => _inner.ThresholdForNotableRelationBonus;

	public override int DailyNotableRelationBonus => _inner.DailyNotableRelationBonus;

	public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber result = _inner.CalculateLoyaltyChange(town, includeDescriptions);
		int adjustment = MyBehavior.GetKingdomStabilityRoyalDomainLoyaltyAdjustmentForTown(town);
		if (adjustment != 0)
		{
			result.Add(adjustment, KingdomStabilityText, null);
		}
		return result;
	}

	public override void CalculateGoldGainDueToHighLoyalty(Town town, ref ExplainedNumber explainedNumber)
	{
		_inner.CalculateGoldGainDueToHighLoyalty(town, ref explainedNumber);
	}

	public override void CalculateGoldCutDueToLowLoyalty(Town town, ref ExplainedNumber explainedNumber)
	{
		_inner.CalculateGoldCutDueToLowLoyalty(town, ref explainedNumber);
	}
}
