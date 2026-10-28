using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyFoodBuyingModel : MBGameModel<PartyFoodBuyingModel>
{
	public abstract float MinimumDaysFoodToLastWhileBuyingFoodFromTown { get; }

	public abstract float MinimumDaysFoodToLastWhileBuyingFoodFromVillage { get; }

	public abstract float LowCostFoodPriceAverage { get; }

	public abstract void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemRosterElement, out float itemElementsPrice);
}
