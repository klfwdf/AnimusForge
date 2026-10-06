using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultHeroAgentLocationModel : HeroAgentLocationModel
{
	public override bool WillBeListedInOverlay(LocationCharacter locationCharacter)
	{
		Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
		if (locationCharacter.Character.IsHero && locationCharacter.Character.HeroObject.PartyBelongedTo != MobileParty.MainParty)
		{
			return locationCharacter.Character.HeroObject.CurrentSettlement == settlement;
		}
		return false;
	}

	public override Location GetLocationForHero(Hero hero, Settlement settlement, out HeroLocationDetail heroLocationDetail)
	{
		heroLocationDetail = HeroLocationDetail.None;
		if (hero == Hero.MainHero || hero.IsDead || hero.CurrentSettlement == null || hero.CurrentSettlement != settlement || (!settlement.IsFortification && !settlement.IsVillage))
		{
			return null;
		}
		IAlleyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
		if (campaignBehavior != null && campaignBehavior.IsHeroAlleyLeaderOfAnyPlayerAlley(hero))
		{
			heroLocationDetail = HeroLocationDetail.Wanderer;
			return settlement.LocationComplex.GetLocationWithId("alley");
		}
		int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		bool flag = hero.GovernorOf != null && hero.GovernorOf == settlement.Town;
		if (settlement.IsFortification)
		{
			Hero hero2 = (settlement.MapFaction.IsKingdomFaction ? ((Kingdom)settlement.MapFaction).Leader : settlement.OwnerClan.Leader);
			Hero hero3 = (settlement.MapFaction.IsKingdomFaction ? ((Kingdom)settlement.MapFaction).Leader.Spouse : settlement.OwnerClan.Leader.Spouse);
			bool flag2 = hero == hero2;
			bool flag3 = hero == hero3;
			bool flag4 = settlement.HeroesWithoutParty.Contains(hero) && hero.Age >= (float)heroComesOfAge && !hero.IsPrisoner && !hero.IsNotable && ((!hero.IsWanderer && hero.Clan != Clan.PlayerClan) || flag);
			if (flag2 || flag3 || flag4)
			{
				heroLocationDetail = ((flag2 || flag3) ? HeroLocationDetail.SettlementKingQueen : HeroLocationDetail.NobleBelongingToNoParty);
				return settlement.LocationComplex.GetLocationWithId("lordshall");
			}
			if (hero.IsPrisoner && settlement.SettlementComponent.GetPrisonerHeroes().Contains(hero.CharacterObject))
			{
				heroLocationDetail = HeroLocationDetail.Prisoner;
				return settlement.LocationComplex.GetLocationWithId("prison");
			}
		}
		else if (settlement.HeroesWithoutParty.Contains(hero))
		{
			heroLocationDetail = HeroLocationDetail.PartylessHeroInsideVillage;
			return settlement.LocationComplex.GetLocationWithId("village_center");
		}
		if (hero.Clan == Clan.PlayerClan && hero.IsLord && hero.IsAlive && hero.Age >= (float)heroComesOfAge && !hero.IsPrisoner && hero.CurrentSettlement == settlement && !flag && !hero.IsPartyLeader)
		{
			heroLocationDetail = HeroLocationDetail.PlayerClanMember;
			if (settlement.IsFortification)
			{
				Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(settlement, out var accessDetails);
				if (accessDetails.AccessLevel != SettlementAccessModel.AccessLevel.FullAccess)
				{
					if (!settlement.IsTown)
					{
						return settlement.LocationComplex.GetLocationWithId("center");
					}
					return settlement.LocationComplex.GetLocationWithId("tavern");
				}
				return settlement.LocationComplex.GetLocationWithId("lordshall");
			}
			return settlement.LocationComplex.GetLocationWithId("village_center");
		}
		if (Hero.MainHero.CompanionsInParty.Contains(hero) && !hero.IsWounded && !PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Exists((AccompanyingCharacter x) => x.LocationCharacter.Character.HeroObject == hero) && !hero.IsPartyLeader)
		{
			heroLocationDetail = HeroLocationDetail.MainPartyCompanion;
			if (!settlement.IsFortification)
			{
				return settlement.LocationComplex.GetLocationWithId("village_center");
			}
			return settlement.LocationComplex.GetLocationWithId("center");
		}
		if (hero.IsNotable && !hero.IsPartyLeader)
		{
			heroLocationDetail = HeroLocationDetail.Notable;
			if (!settlement.IsFortification)
			{
				return settlement.LocationComplex.GetLocationWithId("village_center");
			}
			return settlement.LocationComplex.GetLocationWithId("center");
		}
		if (settlement.HeroesWithoutParty.Contains(hero) && (hero.IsWanderer || hero.IsPlayerCompanion) && (hero.GovernorOf == null || hero.GovernorOf != settlement.Town))
		{
			heroLocationDetail = HeroLocationDetail.Wanderer;
			if (settlement.IsCastle)
			{
				return settlement.LocationComplex.GetLocationWithId("center");
			}
			if (settlement.IsTown)
			{
				return settlement.LocationComplex.GetLocationWithId("tavern");
			}
			return settlement.LocationComplex.GetLocationWithId("village_center");
		}
		foreach (MobileParty party in settlement.Parties)
		{
			if (party.LeaderHero != null && party.LeaderHero == hero)
			{
				heroLocationDetail = HeroLocationDetail.PartyLeader;
				return settlement.IsFortification ? settlement.LocationComplex.GetLocationWithId("lordshall") : (settlement.IsVillage ? settlement.LocationComplex.GetLocationWithId("village_center") : settlement.LocationComplex.GetLocationWithId("center"));
			}
		}
		return null;
	}
}
