using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Settlements;

public interface IMarketData
{
	int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty);

	int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty);
}
