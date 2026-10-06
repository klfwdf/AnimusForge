using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CaravanModel : MBGameModel<CaravanModel>
{
	public abstract int MaxNumberOfItemsToBuyFromSingleCategory { get; }

	public abstract int GetMaxGoldToSpendOnOneItemCategory(MobileParty caravan, ItemCategory itemCategory);

	public abstract int GetInitialTradeGold(Hero owner, bool isNavalCaravan, bool eliteCaravan);

	public abstract int GetCaravanFormingCost(bool eliteCaravan, bool navalCaravan);

	public abstract int GetPowerChangeAfterCaravanCreation(Hero hero, MobileParty caravanParty);

	public abstract bool CanHeroCreateCaravan(Hero hero);

	public abstract float GetEliteCaravanSpawnChance(Hero hero);
}
