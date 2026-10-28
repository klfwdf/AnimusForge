using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IEmptyClanPartiesCampaignBehavior
{
	void TransferCachedLordPartyToNewPartyForPlayerClan(Hero cachedPartyLeader, PartyBase newParty);

	void DisbandCachedLordPartyForPlayerClan(Hero hero);

	int GetShipCountForCachedLordPartyForPlayerClan(Hero hero);

	MBReadOnlyList<Ship> GetShipsForCachedLordPartyForPlayerClan(Hero hero);

	MBReadOnlyList<Hero> GetEmptyClanPartyLeaders();
}
