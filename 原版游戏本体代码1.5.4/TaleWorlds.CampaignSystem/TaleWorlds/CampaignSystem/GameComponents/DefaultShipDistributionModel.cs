using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultShipDistributionModel : ShipDistributionModel
{
	public override bool CanPartyTakeShip(PartyBase party, Ship ship)
	{
		return false;
	}

	public override bool CanSendShipToParty(Ship ship, MobileParty mobileParty)
	{
		return false;
	}

	public override float GetScoreForPartyShipComposition(MobileParty party, MBReadOnlyList<Ship> shipsToConsider)
	{
		return 0f;
	}
}
