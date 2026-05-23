using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B8 RID: 1208
	public static class LeaveSettlementAction
	{
		// Token: 0x060049F3 RID: 18931 RVA: 0x00174304 File Offset: 0x00172504
		public static void ApplyForParty(MobileParty mobileParty)
		{
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
			{
				foreach (MobileParty mobileParty2 in mobileParty.Army.LeaderParty.AttachedParties)
				{
					if (mobileParty2 == MobileParty.MainParty && PlayerEncounter.Current != null)
					{
						PlayerEncounter.Finish(true);
					}
					else if (mobileParty2.CurrentSettlement == currentSettlement)
					{
						LeaveSettlementAction.ApplyForParty(mobileParty2);
					}
				}
			}
			if (mobileParty == MobileParty.MainParty && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty))
			{
				mobileParty.SetMoveModeHold();
			}
			mobileParty.CurrentSettlement = null;
			if (mobileParty.IsCurrentlyAtSea)
			{
				mobileParty.Anchor.ResetPosition();
			}
			currentSettlement.SettlementComponent.OnPartyLeft(mobileParty);
			CampaignEventDispatcher.Instance.OnSettlementLeft(mobileParty, currentSettlement);
		}

		// Token: 0x060049F4 RID: 18932 RVA: 0x00174400 File Offset: 0x00172600
		public static void ApplyForCharacterOnly(Hero hero)
		{
			Settlement currentSettlement = hero.CurrentSettlement;
			hero.StayingInSettlement = null;
			LocationComplex locationComplex = currentSettlement.LocationComplex;
			Location location = ((locationComplex != null) ? locationComplex.GetLocationOfCharacter(hero) : null);
			if (location != null && location.GetLocationCharacter(hero) != null)
			{
				currentSettlement.LocationComplex.RemoveCharacterIfExists(hero);
				LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
				if (locationEncounter == null)
				{
					return;
				}
				locationEncounter.RemoveAccompanyingCharacter(hero);
			}
		}
	}
}
