using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalDLCSettlementSecurityModel : SettlementSecurityModel
{
	public override int MaximumSecurityInSettlement => base.BaseModel.MaximumSecurityInSettlement;

	public override int SecurityDriftMedium => base.BaseModel.SecurityDriftMedium;

	public override float MapEventSecurityEffectRadius => base.BaseModel.MapEventSecurityEffectRadius;

	public override float HideoutClearedSecurityEffectRadius => base.BaseModel.HideoutClearedSecurityEffectRadius;

	public override int HideoutClearedSecurityGain => base.BaseModel.HideoutClearedSecurityGain;

	public override int ThresholdForTaxCorruption => base.BaseModel.ThresholdForTaxCorruption;

	public override int ThresholdForHigherTaxCorruption => base.BaseModel.ThresholdForHigherTaxCorruption;

	public override int ThresholdForTaxBoost => base.BaseModel.ThresholdForTaxBoost;

	public override int SettlementTaxBoostPercentage => base.BaseModel.SettlementTaxBoostPercentage;

	public override int SettlementTaxPenaltyPercentage => base.BaseModel.SettlementTaxPenaltyPercentage;

	public override int ThresholdForNotableRelationBonus => base.BaseModel.ThresholdForNotableRelationBonus;

	public override int ThresholdForNotableRelationPenalty => base.BaseModel.ThresholdForNotableRelationPenalty;

	public override int DailyNotableRelationBonus => base.BaseModel.DailyNotableRelationBonus;

	public override int DailyNotableRelationPenalty => base.BaseModel.DailyNotableRelationPenalty;

	public override int DailyNotablePowerBonus => base.BaseModel.DailyNotablePowerBonus;

	public override int DailyNotablePowerPenalty => base.BaseModel.DailyNotablePowerPenalty;

	public override ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateSecurityChange(town, includeDescriptions);
		Kingdom kingdom = town.OwnerClan?.Kingdom;
		if (kingdom != null && kingdom.HasPolicy(NavalPolicies.RaidersSpoils))
		{
			result.Add(-town.Settlement.Parties.CountQ((MobileParty x) => x.IsLordParty), NavalPolicies.RaidersSpoils.Name);
		}
		return result;
	}

	public override float GetNearbyBanditPartyDefeatedSecurityEffect(Town town, float sumOfAttackedPartyStrengths)
	{
		return base.BaseModel.GetNearbyBanditPartyDefeatedSecurityEffect(town, sumOfAttackedPartyStrengths);
	}

	public override float GetLootedNearbyPartySecurityEffect(Town town, float sumOfAttackedPartyStrengths)
	{
		return base.BaseModel.GetLootedNearbyPartySecurityEffect(town, sumOfAttackedPartyStrengths);
	}

	public override void CalculateGoldGainDueToHighSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		base.BaseModel.CalculateGoldGainDueToHighSecurity(town, ref explainedNumber);
	}

	public override void CalculateGoldCutDueToLowSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		base.BaseModel.CalculateGoldCutDueToLowSecurity(town, ref explainedNumber);
	}
}
