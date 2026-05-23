using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class EnterSettlementAction
{
	private enum EnterSettlementDetail
	{
		WarParty,
		PartyEntersAlley,
		Character,
		Prisoner
	}

	private static void ApplyInternal(Hero hero, MobileParty mobileParty, Settlement settlement, EnterSettlementDetail detail, object subject = null, bool isPlayerInvolved = false)
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
			if (detail == EnterSettlementDetail.Prisoner)
			{
				if (hero != null)
				{
					CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, null, hero, takenFromDungeon: false);
				}
				if (mobileParty != null)
				{
					CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, mobileParty.PrisonRoster.ToFlattenedRoster(), null, takenFromDungeon: false);
				}
			}
			Hero hero2 = ((mobileParty != null) ? mobileParty.LeaderHero : hero);
			if (hero2 != null)
			{
				float currentTime = Campaign.CurrentTime;
				if (hero2.Clan == settlement.OwnerClan && hero2.Clan?.Leader == hero2)
				{
					settlement.LastVisitTimeOfOwner = currentTime;
				}
			}
			if (mobileParty == MobileParty.MainParty && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					ApplyForParty(attachedParty, settlement);
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
			Debug.Print($"Player has entered {settlement.StringId}: {settlement}");
		}
	}

	public static void ApplyForParty(MobileParty mobileParty, Settlement settlement)
	{
		if (mobileParty != null && mobileParty.Army != null && mobileParty.Army.LeaderParty != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.Army.LeaderParty.CurrentSettlement == settlement && mobileParty.AttachedTo == null)
		{
			mobileParty.Army.AddPartyToMergedParties(mobileParty);
		}
		bool flag = mobileParty.IsCurrentlyAtSea && mobileParty.IsTargetingPort;
		mobileParty.IsCurrentlyAtSea = !mobileParty.HasLandNavigationCapability || (flag && settlement.IsVillage);
		mobileParty.CurrentSettlement = settlement;
		if (flag && mobileParty.Ships.Any() && !mobileParty.Anchor.IsAtSettlement(settlement))
		{
			mobileParty.Anchor.SetSettlement(settlement);
		}
		settlement.SettlementComponent.OnPartyEntered(mobileParty);
		ApplyInternal(mobileParty.LeaderHero, mobileParty, settlement, EnterSettlementDetail.WarParty);
	}

	public static void ApplyForPartyEntersAlley(MobileParty party, Settlement settlement, Alley alley, bool isPlayerInvolved = false)
	{
		ApplyInternal(null, party, settlement, EnterSettlementDetail.PartyEntersAlley, alley, isPlayerInvolved);
	}

	public static void ApplyForCharacterOnly(Hero hero, Settlement settlement)
	{
		hero.StayingInSettlement = settlement;
		ApplyInternal(hero, null, settlement, EnterSettlementDetail.Character);
	}

	public static void ApplyForPrisoner(Hero hero, Settlement settlement)
	{
		hero.ChangeState(Hero.CharacterStates.Prisoner);
		ApplyInternal(hero, null, settlement, EnterSettlementDetail.Prisoner);
	}
}
