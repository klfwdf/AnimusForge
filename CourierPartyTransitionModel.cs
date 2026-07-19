using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
#if BANNERLORD_1_4_OR_GREATER
using TaleWorlds.CampaignSystem.Settlements;
#endif

namespace AnimusForge;

public sealed class CourierPartyTransitionModel : PartyTransitionModel
{
	private readonly PartyTransitionModel _inner;

	public CourierPartyTransitionModel(PartyTransitionModel inner)
	{
		_inner = inner ?? new DefaultPartyTransitionModel();
	}

	public override CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty)
	{
		// Courier boats are created beside the courier for this route. The vanilla fallback
		// treats an unanchored fleet as hours away, which leaves the land icon fading for hours.
		return CourierDeliveryBehavior.IsCourierParty(mobileParty)
			? CampaignTime.Zero
			: _inner.GetTransitionTimeForEmbarking(mobileParty);
	}

	public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
	{
		return CourierDeliveryBehavior.IsCourierParty(mobileParty)
			? CampaignTime.Zero
			: _inner.GetTransitionTimeDisembarking(mobileParty);
	}

#if BANNERLORD_1_4_OR_GREATER
	public override CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement)
	{
		return _inner.GetFleetTravelTimeToSettlement(mobileParty, targetSettlement);
	}
#else
	public override CampaignTime GetFleetTravelTimeToPoint(MobileParty owner, CampaignVec2 target)
	{
		return _inner.GetFleetTravelTimeToPoint(owner, target);
	}
#endif
}
