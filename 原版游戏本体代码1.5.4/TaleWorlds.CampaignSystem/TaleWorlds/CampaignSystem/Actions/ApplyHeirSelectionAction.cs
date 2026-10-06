using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ApplyHeirSelectionAction
{
	private static void ApplyInternal(Hero heir, bool isRetirement = false)
	{
		if (heir.PartyBelongedTo != null && heir.PartyBelongedTo.IsCaravan)
		{
			Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(heir.PartyBelongedTo, MobileParty.NavigationType.All, (Settlement s) => (s.IsTown || s.IsCastle) && !FactionManager.IsAtWarAgainstFaction(s.MapFaction, heir.MapFaction));
			if (settlement == null)
			{
				settlement = SettlementHelper.FindNearestSettlementToMobileParty(heir.PartyBelongedTo, MobileParty.NavigationType.All, (Settlement s) => s.IsVillage || (!s.IsHideout && !s.IsFortification));
			}
			DestroyPartyAction.Apply(null, heir.PartyBelongedTo);
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(heir, settlement);
		}
		TransferCaravanOwnerships(heir);
		ChangeClanLeaderAction.ApplyWithSelectedNewLeader(Clan.PlayerClan, heir);
		if (isRetirement)
		{
			DisableHeroAction.Apply(Hero.MainHero);
			if (heir.PartyBelongedTo != MobileParty.MainParty)
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(CharacterObject.PlayerCharacter);
			}
			LogEntry.AddLogEntry(new PlayerRetiredLogEntry(Hero.MainHero));
			TextObject textObject = new TextObject("{=0MTzaxau}{?CHARACTER.GENDER}She{?}He{\\?} retired from adventuring, and was last seen with a group of mountain hermits living a life of quiet contemplation.");
			textObject.SetCharacterProperties("CHARACTER", Hero.MainHero.CharacterObject);
			Hero.MainHero.EncyclopediaText = textObject;
		}
		else
		{
			KillCharacterAction.ApplyByDeathMarkForced(Hero.MainHero, showNotification: true);
		}
		if (heir.CurrentSettlement != null && heir.PartyBelongedTo != null)
		{
			LeaveSettlementAction.ApplyForCharacterOnly(heir);
			LeaveSettlementAction.ApplyForParty(heir.PartyBelongedTo);
		}
		for (int num = Hero.MainHero.OwnedWorkshops.Count - 1; num >= 0; num--)
		{
			ChangeOwnerOfWorkshopAction.ApplyByDeath(Hero.MainHero.OwnedWorkshops[num], heir);
		}
		if (heir.PartyBelongedTo != MobileParty.MainParty)
		{
			for (int num2 = MobileParty.MainParty.MemberRoster.Count - 1; num2 >= 0; num2--)
			{
				TroopRosterElement elementCopyAtIndex = MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(num2);
				if (elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.HeroObject != Hero.MainHero)
				{
					MakeHeroFugitiveAction.Apply(elementCopyAtIndex.Character.HeroObject);
				}
			}
		}
		if (MobileParty.MainParty.Army != null)
		{
			DisbandArmyAction.ApplyByUnknownReason(MobileParty.MainParty.Army);
		}
		ChangePlayerCharacterAction.Apply(heir);
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
	}

	public static void ApplyByDeath(Hero heir)
	{
		ApplyInternal(heir);
	}

	public static void ApplyByRetirement(Hero heir)
	{
		ApplyInternal(heir, isRetirement: true);
	}

	private static void TransferCaravanOwnerships(Hero newLeader)
	{
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsCaravan)
			{
				CaravanPartyComponent.TransferCaravanOwnership(hero.PartyBelongedTo, newLeader, hero.PartyBelongedTo.HomeSettlement);
			}
		}
	}
}
