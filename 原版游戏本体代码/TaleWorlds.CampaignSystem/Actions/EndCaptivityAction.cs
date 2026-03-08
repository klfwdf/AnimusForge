using System;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004AD RID: 1197
	public static class EndCaptivityAction
	{
		// Token: 0x060049B3 RID: 18867 RVA: 0x00172C10 File Offset: 0x00170E10
		private static void ApplyInternal(Hero prisoner, EndCaptivityDetail detail, Hero facilitatior = null, bool showNotification = true)
		{
			PartyBase partyBelongedToAsPrisoner = prisoner.PartyBelongedToAsPrisoner;
			IFaction capturerFaction = ((partyBelongedToAsPrisoner != null) ? partyBelongedToAsPrisoner.MapFaction : null);
			if (prisoner == Hero.MainHero)
			{
				PlayerCaptivity.EndCaptivity();
				if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.IsSettlement)
				{
					MobileParty.MainParty.Position = partyBelongedToAsPrisoner.Settlement.GatePosition;
					MobileParty.MainParty.IsCurrentlyAtSea = false;
				}
				else if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.IsMobile)
				{
					MobileParty.MainParty.IsCurrentlyAtSea = partyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea;
				}
				if (facilitatior != null && detail != EndCaptivityDetail.Death)
				{
					StringHelpers.SetCharacterProperties("FACILITATOR", facilitatior.CharacterObject, null, false);
					MBInformationManager.AddQuickInformation(new TextObject("{=xPuSASof}{FACILITATOR.NAME} paid a ransom and freed you from captivity.", null), 0, null, null, "");
				}
				CampaignEventDispatcher.Instance.OnHeroPrisonerReleased(prisoner, partyBelongedToAsPrisoner, capturerFaction, detail, true);
				return;
			}
			if (detail == EndCaptivityDetail.Death)
			{
				prisoner.StayingInSettlement = null;
			}
			if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.PrisonRoster.Contains(prisoner.CharacterObject))
			{
				partyBelongedToAsPrisoner.PrisonRoster.RemoveTroop(prisoner.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			if (detail != EndCaptivityDetail.Death)
			{
				if (detail <= EndCaptivityDetail.ReleasedByChoice || detail == EndCaptivityDetail.ReleasedByCompensation)
				{
					prisoner.ChangeState(Hero.CharacterStates.Released);
					if (prisoner.IsPlayerCompanion && detail != EndCaptivityDetail.Ransom)
					{
						MakeHeroFugitiveAction.Apply(prisoner, false);
					}
				}
				else
				{
					MakeHeroFugitiveAction.Apply(prisoner, false);
				}
				Settlement currentSettlement = prisoner.CurrentSettlement;
				if (currentSettlement != null)
				{
					currentSettlement.AddHeroWithoutParty(prisoner);
				}
				CampaignEventDispatcher.Instance.OnHeroPrisonerReleased(prisoner, partyBelongedToAsPrisoner, capturerFaction, detail, showNotification);
			}
		}

		// Token: 0x060049B4 RID: 18868 RVA: 0x00172D5C File Offset: 0x00170F5C
		public static void ApplyByReleasedAfterBattle(Hero character)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.ReleasedAfterBattle, null, true);
		}

		// Token: 0x060049B5 RID: 18869 RVA: 0x00172D67 File Offset: 0x00170F67
		public static void ApplyByRansom(Hero character, Hero facilitator)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.Ransom, facilitator, true);
		}

		// Token: 0x060049B6 RID: 18870 RVA: 0x00172D72 File Offset: 0x00170F72
		public static void ApplyByPeace(Hero character, Hero facilitator = null)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.ReleasedAfterPeace, facilitator, true);
		}

		// Token: 0x060049B7 RID: 18871 RVA: 0x00172D7D File Offset: 0x00170F7D
		public static void ApplyByEscape(Hero character, Hero facilitator = null, bool showNotification = true)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.ReleasedAfterEscape, facilitator, showNotification);
		}

		// Token: 0x060049B8 RID: 18872 RVA: 0x00172D88 File Offset: 0x00170F88
		public static void ApplyByDeath(Hero character)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.Death, null, true);
		}

		// Token: 0x060049B9 RID: 18873 RVA: 0x00172D94 File Offset: 0x00170F94
		public static void ApplyByReleasedByChoice(FlattenedTroopRoster troopRoster)
		{
			foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in troopRoster)
			{
				if (flattenedTroopRosterElement.Troop.IsHero)
				{
					EndCaptivityAction.ApplyInternal(flattenedTroopRosterElement.Troop.HeroObject, EndCaptivityDetail.ReleasedByChoice, null, true);
				}
			}
			CampaignEventDispatcher.Instance.OnPrisonerReleased(troopRoster);
		}

		// Token: 0x060049BA RID: 18874 RVA: 0x00172E04 File Offset: 0x00171004
		public static void ApplyByReleasedByChoice(Hero character, Hero facilitator = null)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.ReleasedByChoice, facilitator, true);
		}

		// Token: 0x060049BB RID: 18875 RVA: 0x00172E0F File Offset: 0x0017100F
		public static void ApplyByReleasedByCompensation(Hero character)
		{
			EndCaptivityAction.ApplyInternal(character, EndCaptivityDetail.ReleasedByCompensation, null, true);
		}
	}
}
