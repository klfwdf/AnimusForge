using System;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000491 RID: 1169
	public static class ApplyHeirSelectionAction
	{
		// Token: 0x0600493C RID: 18748 RVA: 0x00170234 File Offset: 0x0016E434
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
			ApplyHeirSelectionAction.TransferCaravanOwnerships(heir);
			ChangeClanLeaderAction.ApplyWithSelectedNewLeader(Clan.PlayerClan, heir);
			if (isRetirement)
			{
				DisableHeroAction.Apply(Hero.MainHero);
				if (heir.PartyBelongedTo != MobileParty.MainParty)
				{
					MobileParty.MainParty.MemberRoster.RemoveTroop(CharacterObject.PlayerCharacter, 1, default(UniqueTroopDescriptor), 0);
				}
				LogEntry.AddLogEntry(new PlayerRetiredLogEntry(Hero.MainHero));
				TextObject textObject = new TextObject("{=0MTzaxau}{?CHARACTER.GENDER}She{?}He{\\?} retired from adventuring, and was last seen with a group of mountain hermits living a life of quiet contemplation.", null);
				textObject.SetCharacterProperties("CHARACTER", Hero.MainHero.CharacterObject, false);
				Hero.MainHero.EncyclopediaText = textObject;
			}
			else
			{
				KillCharacterAction.ApplyByDeathMarkForced(Hero.MainHero, true);
			}
			if (heir.CurrentSettlement != null && heir.PartyBelongedTo != null)
			{
				LeaveSettlementAction.ApplyForCharacterOnly(heir);
				LeaveSettlementAction.ApplyForParty(heir.PartyBelongedTo);
			}
			for (int i = Hero.MainHero.OwnedWorkshops.Count - 1; i >= 0; i--)
			{
				ChangeOwnerOfWorkshopAction.ApplyByDeath(Hero.MainHero.OwnedWorkshops[i], heir);
			}
			if (heir.PartyBelongedTo != MobileParty.MainParty)
			{
				for (int j = MobileParty.MainParty.MemberRoster.Count - 1; j >= 0; j--)
				{
					TroopRosterElement elementCopyAtIndex = MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(j);
					if (elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.HeroObject != Hero.MainHero)
					{
						MakeHeroFugitiveAction.Apply(elementCopyAtIndex.Character.HeroObject, false);
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

		// Token: 0x0600493D RID: 18749 RVA: 0x0017049B File Offset: 0x0016E69B
		public static void ApplyByDeath(Hero heir)
		{
			ApplyHeirSelectionAction.ApplyInternal(heir, false);
		}

		// Token: 0x0600493E RID: 18750 RVA: 0x001704A4 File Offset: 0x0016E6A4
		public static void ApplyByRetirement(Hero heir)
		{
			ApplyHeirSelectionAction.ApplyInternal(heir, true);
		}

		// Token: 0x0600493F RID: 18751 RVA: 0x001704B0 File Offset: 0x0016E6B0
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
}
