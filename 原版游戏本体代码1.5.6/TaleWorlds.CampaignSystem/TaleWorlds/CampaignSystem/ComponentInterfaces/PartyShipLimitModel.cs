using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyShipLimitModel : MBGameModel<PartyShipLimitModel>
{
	public abstract int GetIdealShipNumber(MobileParty mobileParty);

	public abstract int GetIdealShipNumber(Clan clan);

	public abstract float GetShipPriority(MobileParty mobileParty, Ship ship, bool isSelling);
}
