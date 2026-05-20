using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

public sealed class CourierFoodConsumptionModel : MobilePartyFoodConsumptionModel
{
	private readonly MobilePartyFoodConsumptionModel _inner;

	public CourierFoodConsumptionModel(MobilePartyFoodConsumptionModel inner)
	{
		_inner = inner ?? new DefaultMobilePartyFoodConsumptionModel();
	}

	public override int NumberOfMenOnMapToEatOneFood => _inner.NumberOfMenOnMapToEatOneFood;

	public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
	{
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return new ExplainedNumber(0f, includeDescription, null);
		}
		return _inner.CalculateDailyBaseFoodConsumptionf(party, includeDescription);
	}

	public override ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber baseConsumption)
	{
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			return new ExplainedNumber(0f, false, null);
		}
		return _inner.CalculateDailyFoodConsumptionf(party, baseConsumption);
	}

	public override bool DoesPartyConsumeFood(MobileParty mobileParty)
	{
		if (CourierDeliveryBehavior.IsCourierParty(mobileParty))
		{
			return false;
		}
		return _inner.DoesPartyConsumeFood(mobileParty);
	}
}
