using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCMobilePartyFoodConsumptionModel : MobilePartyFoodConsumptionModel
{
	private const float PartyFoodConsumptionReductionAtSea = 0.2f;

	private readonly TextObject _partyFoodConsumptionReductionAtSea = new TextObject("{=Z1af4yEX}Food Consumption Reduction At Sea");

	public override int NumberOfMenOnMapToEatOneFood => base.BaseModel.NumberOfMenOnMapToEatOneFood;

	public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
	{
		return base.BaseModel.CalculateDailyBaseFoodConsumptionf(party, includeDescription);
	}

	public override ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber baseConsumption)
	{
		ExplainedNumber stat = base.BaseModel.CalculateDailyFoodConsumptionf(party, baseConsumption);
		if (party.IsCurrentlyAtSea)
		{
			stat.AddFactor(-0.2f, _partyFoodConsumptionReductionAtSea);
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.SmoothOperator, party, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}

	public override bool DoesPartyConsumeFood(MobileParty mobileParty)
	{
		return base.BaseModel.DoesPartyConsumeFood(mobileParty);
	}
}
