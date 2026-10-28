using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class EndCaptivityAction
{
	private static void ApplyInternal(Hero prisoner, EndCaptivityDetail detail, Hero facilitatior = null, bool showNotification = true)
	{
		PartyBase partyBelongedToAsPrisoner = prisoner.PartyBelongedToAsPrisoner;
		IFaction capturerFaction = partyBelongedToAsPrisoner?.MapFaction;
		if (prisoner == Hero.MainHero)
		{
			PlayerCaptivity.EndCaptivity();
			if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.IsSettlement)
			{
				MobileParty.MainParty.DisembarkToPosition(partyBelongedToAsPrisoner.Settlement.GatePosition);
			}
			else if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.IsMobile)
			{
				MobileParty.MainParty.IsCurrentlyAtSea = partyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea;
			}
			if (facilitatior != null && detail != EndCaptivityDetail.Death)
			{
				StringHelpers.SetCharacterProperties("FACILITATOR", facilitatior.CharacterObject);
				MBInformationManager.AddQuickInformation(new TextObject("{=xPuSASof}{FACILITATOR.NAME} paid a ransom and freed you from captivity."));
			}
			CampaignEventDispatcher.Instance.OnHeroPrisonerReleased(prisoner, partyBelongedToAsPrisoner, capturerFaction, detail);
			return;
		}
		if (detail == EndCaptivityDetail.Death)
		{
			prisoner.StayingInSettlement = null;
		}
		if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.PrisonRoster.Contains(prisoner.CharacterObject))
		{
			partyBelongedToAsPrisoner.PrisonRoster.RemoveTroop(prisoner.CharacterObject);
		}
		switch (detail)
		{
		case EndCaptivityDetail.Ransom:
		case EndCaptivityDetail.ReleasedAfterPeace:
		case EndCaptivityDetail.ReleasedAfterBattle:
		case EndCaptivityDetail.ReleasedAfterEscape:
		case EndCaptivityDetail.ReleasedByChoice:
		case EndCaptivityDetail.ReleasedByCompensation:
			prisoner.ChangeState(Hero.CharacterStates.Released);
			if (prisoner.IsPlayerCompanion && detail != EndCaptivityDetail.Ransom)
			{
				MakeHeroFugitiveAction.Apply(prisoner);
			}
			break;
		default:
			MakeHeroFugitiveAction.Apply(prisoner);
			break;
		case EndCaptivityDetail.Death:
			return;
		}
		prisoner.CurrentSettlement?.AddHeroWithoutParty(prisoner);
		CampaignEventDispatcher.Instance.OnHeroPrisonerReleased(prisoner, partyBelongedToAsPrisoner, capturerFaction, detail, showNotification);
	}

	public static void ApplyByReleasedAfterBattle(Hero character)
	{
		ApplyInternal(character, EndCaptivityDetail.ReleasedAfterBattle);
	}

	public static void ApplyByRansom(Hero character, Hero facilitator)
	{
		ApplyInternal(character, EndCaptivityDetail.Ransom, facilitator);
	}

	public static void ApplyByPeace(Hero character, Hero facilitator = null)
	{
		ApplyInternal(character, EndCaptivityDetail.ReleasedAfterPeace, facilitator);
	}

	public static void ApplyByEscape(Hero character, Hero facilitator = null, bool showNotification = true)
	{
		ApplyInternal(character, EndCaptivityDetail.ReleasedAfterEscape, facilitator, showNotification);
	}

	public static void ApplyByDeath(Hero character)
	{
		ApplyInternal(character, EndCaptivityDetail.Death);
	}

	public static void ApplyByReleasedByChoice(FlattenedTroopRoster troopRoster)
	{
		foreach (FlattenedTroopRosterElement item in troopRoster)
		{
			if (item.Troop.IsHero)
			{
				ApplyInternal(item.Troop.HeroObject, EndCaptivityDetail.ReleasedByChoice);
			}
		}
		CampaignEventDispatcher.Instance.OnPrisonerReleased(troopRoster);
	}

	public static void ApplyByReleasedByChoice(Hero character, Hero facilitator = null)
	{
		ApplyInternal(character, EndCaptivityDetail.ReleasedByChoice, facilitator);
	}

	public static void ApplyByReleasedByCompensation(Hero character)
	{
		ApplyInternal(character, EndCaptivityDetail.ReleasedByCompensation);
	}
}
