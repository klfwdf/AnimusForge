using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class MarriageAction
{
	private static void ApplyInternal(Hero firstHero, Hero secondHero, bool showNotification)
	{
		if (!Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(firstHero, secondHero))
		{
			Debug.Print("MarriageAction.Apply() called for not suitable couple: " + firstHero.StringId + " and " + secondHero.StringId);
			return;
		}
		firstHero.Spouse = secondHero;
		secondHero.Spouse = firstHero;
		ChangeRelationAction.ApplyRelationChangeBetweenHeroes(firstHero, secondHero, Campaign.Current.Models.MarriageModel.GetEffectiveRelationIncrease(firstHero, secondHero), showQuickNotification: false);
		Clan clanAfterMarriage = Campaign.Current.Models.MarriageModel.GetClanAfterMarriage(firstHero, secondHero);
		if (clanAfterMarriage != firstHero.Clan)
		{
			Hero hero = firstHero;
			firstHero = secondHero;
			secondHero = hero;
		}
		CampaignEventDispatcher.Instance.OnBeforeHeroesMarried(firstHero, secondHero, showNotification);
		if (firstHero.Clan != clanAfterMarriage)
		{
			HandleClanChangeAfterMarriageForHero(firstHero, clanAfterMarriage);
		}
		if (secondHero.Clan != clanAfterMarriage)
		{
			HandleClanChangeAfterMarriageForHero(secondHero, clanAfterMarriage);
		}
		Romance.EndAllCourtships(firstHero);
		Romance.EndAllCourtships(secondHero);
		ChangeRomanticStateAction.Apply(firstHero, secondHero, Romance.RomanceLevelEnum.Marriage);
	}

	private static void HandleClanChangeAfterMarriageForHero(Hero hero, Clan clanAfterMarriage)
	{
		Clan clan = hero.Clan;
		if (hero.GovernorOf != null)
		{
			ChangeGovernorAction.RemoveGovernorOf(hero);
		}
		if (hero.PartyBelongedTo != null)
		{
			if (clan.Kingdom != clanAfterMarriage.Kingdom)
			{
				if (hero.PartyBelongedTo.Army != null)
				{
					if (hero.PartyBelongedTo.Army.LeaderParty == hero.PartyBelongedTo)
					{
						DisbandArmyAction.ApplyByUnknownReason(hero.PartyBelongedTo.Army);
					}
					else
					{
						hero.PartyBelongedTo.Army = null;
					}
				}
				IFaction kingdom = clanAfterMarriage.Kingdom;
				FactionHelper.FinishAllRelatedHostileActionsOfNobleToFaction(hero, kingdom ?? clanAfterMarriage);
			}
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			bool num = hero.PartyBelongedTo.LeaderHero == hero;
			partyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject);
			MakeHeroFugitiveAction.Apply(hero);
			if (num && partyBelongedTo.IsLordParty)
			{
				DisbandPartyAction.StartDisband(partyBelongedTo);
			}
		}
		hero.Clan = clanAfterMarriage;
		foreach (Hero hero2 in clan.Heroes)
		{
			hero2.UpdateHomeSettlement();
		}
		foreach (Hero hero3 in clanAfterMarriage.Heroes)
		{
			hero3.UpdateHomeSettlement();
		}
	}

	public static void Apply(Hero firstHero, Hero secondHero, bool showNotification = true)
	{
		ApplyInternal(firstHero, secondHero, showNotification);
	}
}
