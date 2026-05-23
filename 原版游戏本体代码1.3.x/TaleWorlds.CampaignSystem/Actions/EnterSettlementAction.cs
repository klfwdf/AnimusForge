using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004AF RID: 1199
	public static class EnterSettlementAction
	{
		// Token: 0x060049C0 RID: 18880 RVA: 0x00172E50 File Offset: 0x00171050
		private static void ApplyInternal(Hero hero, MobileParty mobileParty, Settlement settlement, EnterSettlementAction.EnterSettlementDetail detail, object subject = null, bool isPlayerInvolved = false)
		{
			if (mobileParty != null && mobileParty.IsDisbanding && mobileParty.TargetSettlement == settlement)
			{
				DestroyPartyAction.ApplyForDisbanding(mobileParty, settlement);
			}
			else
			{
				CampaignEventDispatcher.Instance.OnBeforeSettlementEntered(mobileParty, settlement, hero);
				CampaignEventDispatcher.Instance.OnSettlementEntered(mobileParty, settlement, hero);
				CampaignEventDispatcher.Instance.OnAfterSettlementEntered(mobileParty, settlement, hero);
				if (detail == EnterSettlementAction.EnterSettlementDetail.Prisoner)
				{
					if (hero != null)
					{
						CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, null, hero, false);
					}
					if (mobileParty != null)
					{
						CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, mobileParty.PrisonRoster.ToFlattenedRoster(), null, false);
					}
				}
				Hero hero2 = ((mobileParty != null) ? mobileParty.LeaderHero : hero);
				if (hero2 != null)
				{
					float currentTime = Campaign.CurrentTime;
					if (hero2.Clan == settlement.OwnerClan)
					{
						Clan clan = hero2.Clan;
						if (((clan != null) ? clan.Leader : null) == hero2)
						{
							settlement.LastVisitTimeOfOwner = currentTime;
						}
					}
				}
				if (mobileParty == MobileParty.MainParty && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
				{
					foreach (MobileParty mobileParty2 in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
					{
						EnterSettlementAction.ApplyForParty(mobileParty2, settlement);
					}
				}
				if (hero != null && mobileParty == null && hero.PartyBelongedTo == null && hero.PartyBelongedToAsPrisoner == null && hero.Clan == Clan.PlayerClan && hero.GovernorOf == null)
				{
					CampaignEventDispatcher.Instance.OnHeroGetsBusy(hero, HeroGetsBusyReasons.BecomeEmissary);
				}
			}
			if (mobileParty != null && mobileParty.IsFleeing())
			{
				mobileParty.Ai.DisableForHours(5);
			}
			if (hero == Hero.MainHero || mobileParty == MobileParty.MainParty)
			{
				Debug.Print(string.Format("Player has entered {0}: {1}", settlement.StringId, settlement), 0, Debug.DebugColor.White, 17592186044416UL);
			}
		}

		// Token: 0x060049C1 RID: 18881 RVA: 0x00173010 File Offset: 0x00171210
		public static void ApplyForParty(MobileParty mobileParty, Settlement settlement)
		{
			if (mobileParty != null && mobileParty.Army != null && mobileParty.Army.LeaderParty != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.Army.LeaderParty.CurrentSettlement == settlement && mobileParty.AttachedTo == null)
			{
				mobileParty.Army.AddPartyToMergedParties(mobileParty);
			}
			bool flag = mobileParty.IsCurrentlyAtSea && settlement.HasPort;
			mobileParty.IsCurrentlyAtSea = !mobileParty.HasLandNavigationCapability;
			mobileParty.CurrentSettlement = settlement;
			if (flag && settlement.IsFortification && mobileParty.Ships.Any<Ship>())
			{
				mobileParty.Anchor.SetSettlement(settlement);
			}
			settlement.SettlementComponent.OnPartyEntered(mobileParty);
			EnterSettlementAction.ApplyInternal(mobileParty.LeaderHero, mobileParty, settlement, EnterSettlementAction.EnterSettlementDetail.WarParty, null, false);
		}

		// Token: 0x060049C2 RID: 18882 RVA: 0x001730D0 File Offset: 0x001712D0
		public static void ApplyForPartyEntersAlley(MobileParty party, Settlement settlement, Alley alley, bool isPlayerInvolved = false)
		{
			EnterSettlementAction.ApplyInternal(null, party, settlement, EnterSettlementAction.EnterSettlementDetail.PartyEntersAlley, alley, isPlayerInvolved);
		}

		// Token: 0x060049C3 RID: 18883 RVA: 0x001730DD File Offset: 0x001712DD
		public static void ApplyForCharacterOnly(Hero hero, Settlement settlement)
		{
			hero.StayingInSettlement = settlement;
			EnterSettlementAction.ApplyInternal(hero, null, settlement, EnterSettlementAction.EnterSettlementDetail.Character, null, false);
		}

		// Token: 0x060049C4 RID: 18884 RVA: 0x001730F1 File Offset: 0x001712F1
		public static void ApplyForPrisoner(Hero hero, Settlement settlement)
		{
			hero.ChangeState(Hero.CharacterStates.Prisoner);
			EnterSettlementAction.ApplyInternal(hero, null, settlement, EnterSettlementAction.EnterSettlementDetail.Prisoner, null, false);
		}

		// Token: 0x0200088B RID: 2187
		private enum EnterSettlementDetail
		{
			// Token: 0x0400242C RID: 9260
			WarParty,
			// Token: 0x0400242D RID: 9261
			PartyEntersAlley,
			// Token: 0x0400242E RID: 9262
			Character,
			// Token: 0x0400242F RID: 9263
			Prisoner
		}
	}
}
