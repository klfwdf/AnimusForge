using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C9 RID: 1225
	public static class TakePrisonerAction
	{
		// Token: 0x06004A2E RID: 18990 RVA: 0x00175D44 File Offset: 0x00173F44
		private static void ApplyInternal(PartyBase capturerParty, Hero prisonerCharacter, bool isEventCalled = true)
		{
			if (prisonerCharacter.PartyBelongedTo != null)
			{
				if (prisonerCharacter.PartyBelongedTo.LeaderHero == prisonerCharacter)
				{
					prisonerCharacter.PartyBelongedTo.RemovePartyLeader();
				}
				prisonerCharacter.PartyBelongedTo.MemberRoster.RemoveTroop(prisonerCharacter.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			prisonerCharacter.CaptivityStartTime = CampaignTime.Now;
			prisonerCharacter.ChangeState(Hero.CharacterStates.Prisoner);
			capturerParty.AddPrisoner(prisonerCharacter.CharacterObject, 1);
			if (prisonerCharacter == Hero.MainHero)
			{
				if (MobileParty.MainParty.IsDisorganized)
				{
					MobileParty.MainParty.SetDisorganized(false);
				}
				PlayerCaptivity.StartCaptivity(capturerParty);
				if (MobileParty.MainParty.IsCurrentlyAtSea)
				{
					for (int i = MobileParty.MainParty.Ships.Count - 1; i >= 0; i--)
					{
						DestroyShipAction.Apply(MobileParty.MainParty.Ships[i]);
					}
				}
			}
			if (capturerParty.IsSettlement && prisonerCharacter.StayingInSettlement != null)
			{
				prisonerCharacter.StayingInSettlement = null;
			}
			if (isEventCalled)
			{
				CampaignEventDispatcher.Instance.OnHeroPrisonerTaken(capturerParty, prisonerCharacter);
			}
		}

		// Token: 0x06004A2F RID: 18991 RVA: 0x00175E3B File Offset: 0x0017403B
		public static void Apply(PartyBase capturerParty, Hero prisonerCharacter)
		{
			TakePrisonerAction.ApplyInternal(capturerParty, prisonerCharacter, true);
		}

		// Token: 0x06004A30 RID: 18992 RVA: 0x00175E48 File Offset: 0x00174048
		public static void ApplyByTakenFromPartyScreen(FlattenedTroopRoster roster)
		{
			foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in roster)
			{
				if (flattenedTroopRosterElement.Troop.IsHero)
				{
					TakePrisonerAction.ApplyInternal(PartyBase.MainParty, flattenedTroopRosterElement.Troop.HeroObject, true);
				}
			}
			CampaignEventDispatcher.Instance.OnPrisonerTaken(roster);
		}
	}
}
