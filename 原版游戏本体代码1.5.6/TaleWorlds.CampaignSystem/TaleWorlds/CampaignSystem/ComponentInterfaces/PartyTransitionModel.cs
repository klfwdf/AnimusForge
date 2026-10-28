using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyTransitionModel : MBGameModel<PartyTransitionModel>
{
	public abstract CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty);

	public abstract CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty);

	public abstract CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement);
}
