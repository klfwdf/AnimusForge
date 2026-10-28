using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyTransitionModel : PartyTransitionModel
{
	public override CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement)
	{
		return CampaignTime.Never;
	}

	public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
	{
		return CampaignTime.Never;
	}

	public override CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty)
	{
		return CampaignTime.Never;
	}
}
