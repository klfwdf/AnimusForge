using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.Actions;

public static class LeaveSettlementAction
{
	public static void ApplyForParty(MobileParty mobileParty)
	{
		Settlement currentSettlement = mobileParty.CurrentSettlement;
		if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
		{
			foreach (MobileParty attachedParty in mobileParty.Army.LeaderParty.AttachedParties)
			{
				if (attachedParty == MobileParty.MainParty && PlayerEncounter.Current != null)
				{
					PlayerEncounter.Finish();
				}
				else if (attachedParty.CurrentSettlement == currentSettlement)
				{
					ApplyForParty(attachedParty);
				}
			}
		}
		mobileParty.CurrentSettlement = null;
		if (mobileParty.IsCurrentlyAtSea)
		{
			mobileParty.Anchor.ResetPosition();
		}
		currentSettlement.SettlementComponent.OnPartyLeft(mobileParty);
		CampaignEventDispatcher.Instance.OnSettlementLeft(mobileParty, currentSettlement);
	}

	public static void ApplyForCharacterOnly(Hero hero)
	{
		Settlement currentSettlement = hero.CurrentSettlement;
		hero.StayingInSettlement = null;
		Location location = currentSettlement.LocationComplex?.GetLocationOfCharacter(hero);
		if (location != null && location.GetLocationCharacter(hero) != null)
		{
			currentSettlement.LocationComplex.RemoveCharacterIfExists(hero);
			PlayerEncounter.LocationEncounter?.RemoveAccompanyingCharacter(hero);
		}
	}
}
