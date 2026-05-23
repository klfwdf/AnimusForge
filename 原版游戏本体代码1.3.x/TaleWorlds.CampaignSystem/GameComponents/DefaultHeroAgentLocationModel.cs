using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000119 RID: 281
	public class DefaultHeroAgentLocationModel : HeroAgentLocationModel
	{
		// Token: 0x060017E9 RID: 6121 RVA: 0x00071E30 File Offset: 0x00070030
		public override bool WillBeListedInOverlay(LocationCharacter locationCharacter)
		{
			Settlement settlement = ((MobileParty.MainParty.CurrentSettlement != null) ? MobileParty.MainParty.CurrentSettlement : MobileParty.MainParty.LastVisitedSettlement);
			return locationCharacter.Character.IsHero && locationCharacter.Character.HeroObject.PartyBelongedTo != MobileParty.MainParty && locationCharacter.Character.HeroObject.CurrentSettlement == settlement;
		}

		// Token: 0x060017EA RID: 6122 RVA: 0x00071E9C File Offset: 0x0007009C
		public override Location GetLocationForHero(Hero hero, Settlement settlement, out HeroAgentLocationModel.HeroLocationDetail heroLocationDetail)
		{
			heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.None;
			if (hero == Hero.MainHero || hero.IsDead || hero.CurrentSettlement == null || hero.CurrentSettlement != settlement || (!settlement.IsFortification && !settlement.IsVillage))
			{
				return null;
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
					heroLocationDetail = ((flag2 || flag3) ? HeroAgentLocationModel.HeroLocationDetail.SettlementKingQueen : HeroAgentLocationModel.HeroLocationDetail.NobleBelongingToNoParty);
					return settlement.LocationComplex.GetLocationWithId("lordshall");
				}
				if (hero.IsPrisoner && settlement.SettlementComponent.GetPrisonerHeroes().Contains(hero.CharacterObject))
				{
					heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.Prisoner;
					return settlement.LocationComplex.GetLocationWithId("prison");
				}
			}
			else if (settlement.HeroesWithoutParty.Contains(hero))
			{
				heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage;
				return settlement.LocationComplex.GetLocationWithId("village_center");
			}
			if (hero.Clan == Clan.PlayerClan && hero.IsLord && hero.IsAlive && hero.Age >= (float)heroComesOfAge && !hero.IsPrisoner && hero.CurrentSettlement == settlement && !flag && !hero.IsPartyLeader)
			{
				heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember;
				if (!settlement.IsFortification)
				{
					return settlement.LocationComplex.GetLocationWithId("village_center");
				}
				SettlementAccessModel.AccessDetails accessDetails;
				Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(settlement, out accessDetails);
				if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess)
				{
					return settlement.LocationComplex.GetLocationWithId("lordshall");
				}
				if (!settlement.IsTown)
				{
					return settlement.LocationComplex.GetLocationWithId("center");
				}
				return settlement.LocationComplex.GetLocationWithId("tavern");
			}
			else if (Hero.MainHero.CompanionsInParty.Contains(hero) && !hero.IsWounded && !PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Exists((AccompanyingCharacter x) => x.LocationCharacter.Character.HeroObject == hero) && !hero.IsPartyLeader)
			{
				heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion;
				if (!settlement.IsFortification)
				{
					return settlement.LocationComplex.GetLocationWithId("village_center");
				}
				return settlement.LocationComplex.GetLocationWithId("center");
			}
			else if (hero.IsNotable && !hero.IsPartyLeader)
			{
				heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.Notable;
				if (!settlement.IsFortification)
				{
					return settlement.LocationComplex.GetLocationWithId("village_center");
				}
				return settlement.LocationComplex.GetLocationWithId("center");
			}
			else
			{
				if (!settlement.HeroesWithoutParty.Contains(hero) || (!hero.IsWanderer && !hero.IsPlayerCompanion) || (hero.GovernorOf != null && hero.GovernorOf == settlement.Town))
				{
					foreach (MobileParty mobileParty in settlement.Parties)
					{
						if (mobileParty.LeaderHero != null && mobileParty.LeaderHero == hero)
						{
							heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.PartyLeader;
							return settlement.IsFortification ? settlement.LocationComplex.GetLocationWithId("lordshall") : (settlement.IsVillage ? settlement.LocationComplex.GetLocationWithId("village_center") : settlement.LocationComplex.GetLocationWithId("center"));
						}
					}
					return null;
				}
				heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.Wanderer;
				if (settlement.IsCastle)
				{
					return settlement.LocationComplex.GetLocationWithId("center");
				}
				if (!settlement.IsTown)
				{
					return settlement.LocationComplex.GetLocationWithId("village_center");
				}
				IAlleyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>();
				if (campaignBehavior != null && campaignBehavior.IsHeroAlleyLeaderOfAnyPlayerAlley(hero))
				{
					return settlement.LocationComplex.GetLocationWithId("alley");
				}
				return settlement.LocationComplex.GetLocationWithId("tavern");
			}
		}
	}
}
