using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class TeleportHeroAction
{
	public enum TeleportationDetail
	{
		ImmediateTeleportToSettlement,
		ImmediateTeleportToParty,
		ImmediateTeleportToPartyAsPartyLeader,
		DelayedTeleportToSettlement,
		DelayedTeleportToParty,
		DelayedTeleportToSettlementAsGovernor,
		DelayedTeleportToPartyAsPartyLeader
	}

	private static void ApplyInternal(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportationDetail detail)
	{
		CampaignEventDispatcher.Instance.OnHeroTeleportationRequested(hero, targetSettlement, targetParty, detail);
		switch (detail)
		{
		default:
			return;
		case TeleportationDetail.ImmediateTeleportToSettlement:
			if (targetSettlement == null)
			{
				return;
			}
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
				hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
			}
			EnterSettlementAction.ApplyForCharacterOnly(hero, targetSettlement);
			return;
		case TeleportationDetail.ImmediateTeleportToParty:
			if (hero.IsTraveling)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
			AddHeroToPartyAction.Apply(hero, targetParty);
			return;
		case TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader:
			if (hero.IsTraveling)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
			AddHeroToPartyAction.Apply(hero, targetParty);
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
				targetParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: false);
			}
			return;
		case TeleportationDetail.DelayedTeleportToSettlement:
			if (hero.CurrentSettlement == targetSettlement)
			{
				ApplyImmediateTeleportToSettlement(hero, targetSettlement);
				return;
			}
			break;
		case TeleportationDetail.DelayedTeleportToParty:
		case TeleportationDetail.DelayedTeleportToSettlementAsGovernor:
		case TeleportationDetail.DelayedTeleportToPartyAsPartyLeader:
			break;
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
			hero.PartyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
		}
		if (detail == TeleportationDetail.DelayedTeleportToPartyAsPartyLeader)
		{
			TextObject textObject = new TextObject("{=ithcVNfA}{CLAN_NAME}{.o} Party");
			textObject.SetTextVariable("CLAN_NAME", (targetParty.ActualClan != null) ? targetParty.ActualClan.Name : CampaignData.NeutralFactionName);
			targetParty.Party.SetCustomName(textObject);
		}
		hero.ChangeState(Hero.CharacterStates.Traveling);
	}

	public static void ApplyImmediateTeleportToSettlement(Hero heroToBeMoved, Settlement targetSettlement)
	{
		ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportationDetail.ImmediateTeleportToSettlement);
	}

	public static void ApplyImmediateTeleportToParty(Hero heroToBeMoved, MobileParty party)
	{
		ApplyInternal(heroToBeMoved, null, party, TeleportationDetail.ImmediateTeleportToParty);
	}

	public static void ApplyImmediateTeleportToPartyAsPartyLeader(Hero heroToBeMoved, MobileParty party)
	{
		ApplyInternal(heroToBeMoved, null, party, TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader);
	}

	public static void ApplyDelayedTeleportToSettlement(Hero heroToBeMoved, Settlement targetSettlement)
	{
		ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportationDetail.DelayedTeleportToSettlement);
	}

	public static void ApplyDelayedTeleportToParty(Hero heroToBeMoved, MobileParty party)
	{
		ApplyInternal(heroToBeMoved, null, party, TeleportationDetail.DelayedTeleportToParty);
	}

	public static void ApplyDelayedTeleportToSettlementAsGovernor(Hero heroToBeMoved, Settlement targetSettlement)
	{
		ApplyInternal(heroToBeMoved, targetSettlement, null, TeleportationDetail.DelayedTeleportToSettlementAsGovernor);
	}

	public static void ApplyDelayedTeleportToPartyAsPartyLeader(Hero heroToBeMoved, MobileParty party)
	{
		ApplyInternal(heroToBeMoved, null, party, TeleportationDetail.DelayedTeleportToPartyAsPartyLeader);
	}
}
