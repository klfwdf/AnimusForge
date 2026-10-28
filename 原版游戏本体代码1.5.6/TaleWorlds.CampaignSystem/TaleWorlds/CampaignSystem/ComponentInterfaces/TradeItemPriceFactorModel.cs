using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class TradeItemPriceFactorModel : MBGameModel<TradeItemPriceFactorModel>
{
	public abstract float GetTradePenalty(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand);

	public abstract float GetBasePriceFactor(ItemCategory itemCategory, float inStoreValue, float supply, float demand, bool isSelling, int transferValue);

	public abstract int GetPrice(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStoreValue, float supply, float demand);

	public abstract int GetTheoreticalMaxItemMarketValue(ItemObject item);
}
