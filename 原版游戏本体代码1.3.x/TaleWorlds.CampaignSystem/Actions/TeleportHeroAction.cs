using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004CA RID: 1226
	public static class TeleportHeroAction
	{
		// Token: 0x06004A31 RID: 18993 RVA: 0x00175EBC File Offset: 0x001740BC
		private static void ApplyInternal(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
		{
			CampaignEventDispatcher.Instance.OnHeroTeleportationRequested(hero, targetSettlement, targetParty, detail);
			switch (detail)
			{
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToSettlement:
				if (targetSettlement != null)
				{
					if (!hero.IsActive)
					{
						hero.ChangeState(Hero.CharacterStates.Active);
					}
					if (hero.CurrentSettlement != null)
					{
						LeaveSettlementAction.ApplyForCharacterOnly(hero);
					}
					if (hero.PartyBelongedTo != null)
					{
						if (!hero.PartyBelongedTo.IsActive || hero.PartyBelongedTo.IsCurrentlyEngagingParty || hero.PartyBelongedTo.MapEvent != null)
						{
							return;
						}
						hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
					}
					EnterSettlementAction.ApplyForCharacterOnly(hero, targetSettlement);
					return;
				}
				break;
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToParty:
				if (hero.IsTraveling)
				{
					hero.ChangeState(Hero.CharacterStates.Active);
				}
				AddHeroToPartyAction.Apply(hero, targetParty, true);
				return;
			case TeleportHeroAction.TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader:
				if (hero.IsTraveling)
				{
					hero.ChangeState(Hero.CharacterStates.Active);
				}
				AddHeroToPartyAction.Apply(hero, targetParty, true);
				targetParty.ChangePartyLeader(hero);
				targetParty.PartyComponent.ClearCachedName();
				targetParty.Party.SetCustomName(null);
				targetParty.Party.SetVisualAsDirty();
				if (targetParty.IsDisbanding)
				{
					DisbandPartyAction.CancelDisband(targetParty);
				}
				if (targetParty.Ai.DoNotMakeNewDecisions)
				{
					targetParty.Ai.SetDoNotMakeNewDecisions(false);
					return;
				}
				break;
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlement:
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToParty:
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlementAsGovernor:
			case TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader:
				if (detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlement && hero.CurrentSettlement == targetSettlement)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, targetSettlement);
					return;
				}
				if (hero.GovernorOf != null)
				{
					ChangeGovernorAction.RemoveGovernorOf(hero);
				}
				if (hero.CurrentSettlement != null && hero.CurrentSettlement != targetSettlement)
				{
					LeaveSettlementAction.ApplyForCharacterOnly(hero);
				}
				if (hero.PartyBelongedTo != null)
				{
					if (!hero.PartyBelongedTo.IsActive || (hero.PartyBelongedTo.IsCurrentlyEngagingParty && hero.PartyBelongedTo.MapEvent != null))
					{
						return;
					}
					hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
				}
				if (detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader)
				{
					TextObject textObject = new TextObject("{=ithcVNfA}{CLAN_NAME}{.o} Party", null);
					textObject.SetTextVariable("CLAN_NAME", (targetParty.ActualClan != null) ? targetParty.ActualClan.Name : CampaignData.NeutralFactionName);
					targetParty.Party.SetCustomName(textObject);
				}
				hero.ChangeState(Hero.CharacterStates.Traveling);
				break;
			default:
				return;
			}
		}

		// Token: 0x06004A32 RID: 18994 RVA: 0x001760D1 File Offset: 0x001742D1
		public static void ApplyImmediateTeleportToSettlement(Hero heroToBeMoved, Settlement targetSettlement)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportHeroAction.TeleportationDetail.ImmediateTeleportToSettlement);
		}

		// Token: 0x06004A33 RID: 18995 RVA: 0x001760DC File Offset: 0x001742DC
		public static void ApplyImmediateTeleportToParty(Hero heroToBeMoved, MobileParty party)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, null, party, TeleportHeroAction.TeleportationDetail.ImmediateTeleportToParty);
		}

		// Token: 0x06004A34 RID: 18996 RVA: 0x001760E7 File Offset: 0x001742E7
		public static void ApplyImmediateTeleportToPartyAsPartyLeader(Hero heroToBeMoved, MobileParty party)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, null, party, TeleportHeroAction.TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader);
		}

		// Token: 0x06004A35 RID: 18997 RVA: 0x001760F2 File Offset: 0x001742F2
		public static void ApplyDelayedTeleportToSettlement(Hero heroToBeMoved, Settlement targetSettlement)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlement);
		}

		// Token: 0x06004A36 RID: 18998 RVA: 0x001760FD File Offset: 0x001742FD
		public static void ApplyDelayedTeleportToParty(Hero heroToBeMoved, MobileParty party)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, null, party, TeleportHeroAction.TeleportationDetail.DelayedTeleportToParty);
		}

		// Token: 0x06004A37 RID: 18999 RVA: 0x00176108 File Offset: 0x00174308
		public static void ApplyDelayedTeleportToSettlementAsGovernor(Hero heroToBeMoved, Settlement targetSettlement)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportHeroAction.TeleportationDetail.DelayedTeleportToSettlementAsGovernor);
		}

		// Token: 0x06004A38 RID: 19000 RVA: 0x00176113 File Offset: 0x00174313
		public static void ApplyDelayedTeleportToPartyAsPartyLeader(Hero heroToBeMoved, MobileParty party)
		{
			TeleportHeroAction.ApplyInternal(heroToBeMoved, null, party, TeleportHeroAction.TeleportationDetail.DelayedTeleportToPartyAsPartyLeader);
		}

		// Token: 0x02000899 RID: 2201
		public enum TeleportationDetail
		{
			// Token: 0x0400246E RID: 9326
			ImmediateTeleportToSettlement,
			// Token: 0x0400246F RID: 9327
			ImmediateTeleportToParty,
			// Token: 0x04002470 RID: 9328
			ImmediateTeleportToPartyAsPartyLeader,
			// Token: 0x04002471 RID: 9329
			DelayedTeleportToSettlement,
			// Token: 0x04002472 RID: 9330
			DelayedTeleportToParty,
			// Token: 0x04002473 RID: 9331
			DelayedTeleportToSettlementAsGovernor,
			// Token: 0x04002474 RID: 9332
			DelayedTeleportToPartyAsPartyLeader
		}
	}
}
