using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyShipLimitModel : PartyShipLimitModel
{
	public override int GetIdealShipNumber(MobileParty mobileParty)
	{
		return 0;
	}

	public override int GetIdealShipNumber(Clan clan)
	{
		return 0;
	}

	public override float GetShipPriority(MobileParty mobileParty, Ship ship, bool isSelling)
	{
		return 0f;
	}
}
