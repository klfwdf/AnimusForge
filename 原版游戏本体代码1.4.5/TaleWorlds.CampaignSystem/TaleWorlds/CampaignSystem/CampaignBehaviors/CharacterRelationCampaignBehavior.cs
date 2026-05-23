using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CharacterRelationCampaignBehavior : CampaignBehaviorBase
{
	private const int RelationPenaltyFactor = 6;

	private const int RelationIncreaseBetweenHeroesAfterMarriage = 30;

	private const int RelationChangeForLeavingWithRebellion = -40;

	private const int RelationChangeForLeaveKingdom = -20;

	private const int RaidDefenseRelationGainWithVillageNotable = 5;

	private const float ChanceForRelationChange = 0.05f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, UpdateFriendshipAndEnemies);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonerDonatedToSettlement);
		CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnHeroesMarried);
		CampaignEvents.OnHeroUnregisteredEvent.AddNonSerializedListener(this, OnHeroUnregistered);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if ((detail != KillCharacterAction.KillCharacterActionDetail.Executed && detail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent) || killer != Hero.MainHero || victim.Clan == null)
		{
			return;
		}
		int num = 0;
		foreach (Clan item in Clan.All)
		{
			if (item.IsEliminated || item.IsBanditFaction || item == Clan.PlayerClan)
			{
				continue;
			}
			bool showQuickNotification;
			int relationChangeForExecutingHero = Campaign.Current.Models.ExecutionRelationModel.GetRelationChangeForExecutingHero(victim, item.Leader, out showQuickNotification);
			if (relationChangeForExecutingHero != 0)
			{
				Hero leader = item.Leader;
				ChangeRelationAction.ApplyPlayerRelation(leader, relationChangeForExecutingHero, affectRelatives: true, showQuickNotification: false);
				if (showQuickNotification)
				{
					num++;
					TextObject textObject = GameTexts.FindText("str_your_relation_decreased_with_clan");
					textObject.SetTextVariable("CLAN_LEADER", item.Name);
					textObject.SetTextVariable("VALUE", leader.GetRelation(killer));
					textObject.SetTextVariable("MAGNITUDE", MathF.Abs(relationChangeForExecutingHero));
					InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
				}
			}
		}
		if (num > 0)
		{
			TextObject textObject2 = new TextObject("{=oqO9kjeW}The execution has hurt your relations with {COUNT} {?IS_PLURAL}clans{?}clan{\\?}.");
			MBTextManager.SetTextVariable("IS_PLURAL", (num > 1) ? 1 : 0);
			textObject2.SetTextVariable("COUNT", num);
			MBInformationManager.AddQuickInformation(textObject2);
		}
	}

	private void OnHeroUnregistered(Hero hero)
	{
		Campaign.Current.CharacterRelationManager.RemoveHero(hero);
	}

	private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		if (relationChange > 0)
		{
			SkillLevelingManager.OnGainRelation(originalHero, effectiveHeroGainedRelationWith, relationChange, detail);
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.HasWinner)
		{
			return;
		}
		MapEventSide winnerSide = mapEvent.Winner;
		MapEventSide otherSide = winnerSide.OtherSide;
		if (mapEvent.EventType == MapEvent.BattleTypes.FieldBattle || mapEvent.EventType == MapEvent.BattleTypes.Siege || mapEvent.EventType == MapEvent.BattleTypes.SiegeOutside)
		{
			bool flag = false;
			foreach (MapEventParty party3 in otherSide.Parties)
			{
				if (party3.Party.IsMobile && party3.Party.MobileParty.IsLordParty)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				Hero leaderHero = winnerSide.LeaderParty.LeaderHero;
				if (leaderHero != null && leaderHero.GetPerkValue(DefaultPerks.Charm.Oratory))
				{
					Hero randomElementWithPredicate = Hero.AllAliveHeroes.GetRandomElementWithPredicate((Hero x) => x.IsActive && x.IsNotable && x.CurrentSettlement?.MapFaction == winnerSide.LeaderParty.MapFaction);
					if (randomElementWithPredicate != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(winnerSide.LeaderParty.LeaderHero, randomElementWithPredicate, (int)DefaultPerks.Charm.Oratory.SecondaryBonus);
					}
				}
				Hero leaderHero2 = winnerSide.LeaderParty.LeaderHero;
				if (leaderHero2 != null && leaderHero2.GetPerkValue(DefaultPerks.Charm.Warlord))
				{
					Hero randomElementWithPredicate2 = winnerSide.LeaderParty.MapFaction.AliveLords.GetRandomElementWithPredicate((Hero x) => x != winnerSide.LeaderParty.LeaderHero);
					if (randomElementWithPredicate2 != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(winnerSide.LeaderParty.LeaderHero, randomElementWithPredicate2, (int)DefaultPerks.Charm.Warlord.SecondaryBonus);
					}
				}
			}
		}
		int num = winnerSide.CalculateTotalContribution();
		if (num <= 0)
		{
			return;
		}
		MBReadOnlyList<MapEventParty> parties = winnerSide.Parties;
		List<MobileParty> list = new List<MobileParty>();
		List<MobileParty> list2 = new List<MobileParty>();
		foreach (MapEventParty item in parties)
		{
			PartyBase party = item.Party;
			if (party.IsMobile)
			{
				if (party.MobileParty.IsVillager)
				{
					list.Add(party.MobileParty);
				}
				else if (party.MobileParty.IsCaravan)
				{
					list2.Add(party.MobileParty);
				}
			}
		}
		foreach (MapEventParty item2 in parties)
		{
			PartyBase party2 = item2.Party;
			if (party2.LeaderHero == null)
			{
				continue;
			}
			float num2 = (float)item2.ContributionToBattle / (float)num;
			if (!(num2 > 0f))
			{
				continue;
			}
			if (mapEvent.EventType == MapEvent.BattleTypes.Raid && winnerSide.MissionSide == BattleSideEnum.Defender && mapEvent.MapEventSettlement.Notables.Count > 0)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mapEvent.MapEventSettlement.Notables.GetRandomElement(), party2.LeaderHero, 5);
			}
			foreach (MobileParty item3 in list)
			{
				if (item3.HomeSettlement.OwnerClan == party2.LeaderHero.Clan || item3.HomeSettlement.OwnerClan.IsEliminated || party2.LeaderHero.Clan.IsEliminated)
				{
					continue;
				}
				int num3 = MBRandom.RoundRandomized(4f * num2);
				if (num3 > 0)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item3.HomeSettlement.OwnerClan.Leader, party2.LeaderHero.Clan.Leader, num3);
				}
				int num4 = MBRandom.RoundRandomized(2f * num2);
				if (num4 <= 0)
				{
					continue;
				}
				foreach (Hero notable in item3.HomeSettlement.Notables)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, party2.LeaderHero.Clan.Leader, num4);
				}
			}
			foreach (MobileParty item4 in list2)
			{
				if (item4.HomeSettlement != null && item4.HomeSettlement.OwnerClan != null && party2.LeaderHero != null && item4.HomeSettlement.OwnerClan.Leader.Clan != party2.LeaderHero.Clan && item4.Party.Owner != null && item4.Party.Owner != Hero.MainHero && item4.Party.Owner.IsAlive && party2.LeaderHero.Clan.Leader != null && party2.LeaderHero.Clan.Leader.IsAlive && !item4.IsCurrentlyUsedByAQuest)
				{
					int relationChange = MBRandom.RoundRandomized(6f * num2);
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item4.Party.Owner, party2.LeaderHero.Clan.Leader, relationChange);
				}
			}
		}
	}

	private void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
		if (!donatingParty.IsMainParty)
		{
			return;
		}
		foreach (FlattenedTroopRosterElement donatedPrisoner in donatedPrisoners)
		{
			if (donatedPrisoner.Troop.IsHero)
			{
				float num = Campaign.Current.Models.PrisonerDonationModel.CalculateRelationGainAfterHeroPrisonerDonate(donatingParty.Party, donatedPrisoner.Troop.HeroObject, donatedSettlement);
				if (num != 0f)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, donatedSettlement.OwnerClan.Leader, (int)num);
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void UpdateFriendshipAndEnemies(CampaignGameStarter campaignGameStarter)
	{
		List<Hero> list = new List<Hero>(Hero.AllAliveHeroes.Count + Hero.DeadOrDisabledHeroes.Count);
		foreach (Hero aliveHero in Campaign.Current.AliveHeroes)
		{
			if (aliveHero.IsLord && aliveHero != Hero.MainHero && aliveHero.MapFaction != null && aliveHero.MapFaction?.Leader != Hero.MainHero)
			{
				list.Add(aliveHero);
			}
		}
		foreach (Hero deadOrDisabledHero in Campaign.Current.DeadOrDisabledHeroes)
		{
			if (deadOrDisabledHero.IsLord && deadOrDisabledHero != Hero.MainHero && deadOrDisabledHero.MapFaction != null && deadOrDisabledHero.MapFaction?.Leader != Hero.MainHero)
			{
				list.Add(deadOrDisabledHero);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Hero hero = list[i];
			for (int j = i + 1; j < list.Count; j++)
			{
				Hero hero2 = list[j];
				if ((hero2.IsDead && hero2.DeathDay < hero.BirthDay) || (hero.IsDead && hero.DeathDay < hero2.BirthDay))
				{
					continue;
				}
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(hero.MapFaction.FactionMidSettlement, hero2.MapFaction.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
				float num = 1f / (2f + 5f * (distance / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.All)));
				if (hero == hero.MapFaction.Leader || hero2 == hero2.MapFaction.Leader)
				{
					num = MathF.Sqrt(num);
				}
				if ((hero.Clan != null && hero.Clan.Tier >= 5 && hero2.Clan != null && hero2.Clan.Tier >= 5) || (hero.IsKingdomLeader && hero2.IsKingdomLeader))
				{
					num = 1f;
				}
				if (!(MBRandom.RandomFloat < num))
				{
					continue;
				}
				float num2 = 0f;
				int num3 = HeroHelper.NPCPersonalityClashWithNPC(hero, hero2);
				if (hero.IsKingdomLeader && hero2.IsKingdomLeader)
				{
					if (hero.Culture == hero2.Culture)
					{
						hero.SetPersonalRelation(hero2, MathF.Round(MBRandom.RandomFloatRanged(35f, 100f)) * -1);
						continue;
					}
					if (num3 == 0)
					{
						num2 = (((double)MBRandom.RandomFloat > 0.5) ? MathF.Round(MBRandom.RandomFloatRanged(-100f, -35f)) : MathF.Round(MBRandom.RandomFloatRanged(35f, 100f)));
					}
					else
					{
						for (int k = 0; k < 4; k++)
						{
							num2 += MBRandom.RandomFloat * 2f - 1f;
						}
						num2 = MBMath.ClampFloat(num2 * 30f, -100f, 100f);
					}
				}
				else
				{
					for (int l = 0; l < 4; l++)
					{
						num2 += MBRandom.RandomFloat * 2f - 1f;
					}
					num2 = MBMath.ClampFloat(num2 * 30f, -100f, 100f);
				}
				if (num3 == 0)
				{
					hero.SetPersonalRelation(hero2, MathF.Round(num2));
				}
				else if (num3 < 0)
				{
					hero.SetPersonalRelation(hero2, MathF.Abs(MathF.Round(num2)) * -1);
				}
				else
				{
					hero.SetPersonalRelation(hero2, MathF.Abs(MathF.Round(num2)));
				}
			}
		}
	}

	private void DailyTickParty(MobileParty mobileParty)
	{
		if (mobileParty.LeaderHero == null)
		{
			return;
		}
		Settlement currentSettlement = mobileParty.CurrentSettlement;
		if (currentSettlement != null && currentSettlement.IsTown && mobileParty.CurrentSettlement.SiegeEvent == null)
		{
			if (mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Medicine.BestMedicine))
			{
				Hero randomElementWithPredicate = mobileParty.CurrentSettlement.Notables.GetRandomElementWithPredicate((Hero x) => x.Age >= 40f && x.IsAlive);
				if (randomElementWithPredicate != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mobileParty.LeaderHero, randomElementWithPredicate, (int)DefaultPerks.Medicine.BestMedicine.SecondaryBonus);
				}
			}
			if (mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Medicine.GoodLogdings))
			{
				Hero randomElement = TownHelpers.GetHeroesInSettlement(mobileParty.CurrentSettlement, (Hero x) => x.Age >= 40f && x != mobileParty.LeaderHero && x.IsLord).GetRandomElement();
				if (randomElement != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mobileParty.LeaderHero, randomElement, (int)DefaultPerks.Medicine.GoodLogdings.SecondaryBonus);
				}
			}
		}
		if (mobileParty.Army == null || !(MBRandom.RandomFloat < DefaultPerks.Charm.Parade.SecondaryBonus) || !mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Charm.Parade))
		{
			return;
		}
		MobileParty randomElementWithPredicate2 = mobileParty.Army.Parties.GetRandomElementWithPredicate((MobileParty x) => x.MemberRoster.GetTroopRoster().AnyQ((TroopRosterElement y) => y.Character.IsHero && y.Character.Occupation == Occupation.Lord && y.Character.HeroObject != mobileParty.LeaderHero));
		if (randomElementWithPredicate2 != null)
		{
			Hero hero = randomElementWithPredicate2.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement x) => x.Character.IsHero && x.Character.Occupation == Occupation.Lord && x.Character.HeroObject != mobileParty.LeaderHero).Character?.HeroObject;
			if (hero != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mobileParty.LeaderHero, hero, 1);
			}
		}
	}

	private void DailyTick()
	{
		if (Settlement.CurrentSettlement != null && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.ForgivableGrievances) && MBRandom.RandomFloat < DefaultPerks.Charm.ForgivableGrievances.SecondaryBonus)
		{
			MBList<Hero> mBList = new MBList<Hero>();
			foreach (Hero item in SettlementHelper.GetAllHeroesOfSettlement(Settlement.CurrentSettlement, includePrisoners: true))
			{
				if (!item.IsHumanPlayerCharacter && item.GetRelationWithPlayer() < 0f)
				{
					mBList.Add(item);
				}
			}
			if (mBList.Count > 0)
			{
				ChangeRelationAction.ApplyPlayerRelation(mBList.GetRandomElement(), 1);
			}
		}
		SettlementLoyaltyModel settlementLoyaltyModel = Campaign.Current.Models.SettlementLoyaltyModel;
		SettlementSecurityModel settlementSecurityModel = Campaign.Current.Models.SettlementSecurityModel;
		bool flag = false;
		bool flag2 = false;
		foreach (Settlement item2 in Settlement.All)
		{
			if (item2.IsTown)
			{
				if (item2.Town.Security >= (float)settlementSecurityModel.ThresholdForNotableRelationBonus)
				{
					foreach (Hero notable in item2.Notables)
					{
						if ((notable.IsArtisan || notable.IsMerchant) && MBRandom.RandomFloat < 0.05f)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable, settlementSecurityModel.DailyNotableRelationBonus, showQuickNotification: false);
							flag2 = flag2 || item2.OwnerClan.Leader.IsHumanPlayerCharacter;
						}
					}
				}
				else
				{
					if (!(item2.Town.Security < (float)settlementSecurityModel.ThresholdForNotableRelationPenalty))
					{
						continue;
					}
					foreach (Hero notable2 in item2.Notables)
					{
						if ((notable2.IsArtisan || notable2.IsMerchant) && MBRandom.RandomFloat < 0.05f)
						{
							notable2.AddPower(settlementSecurityModel.DailyNotablePowerPenalty);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable2, settlementSecurityModel.DailyNotableRelationPenalty, showQuickNotification: false);
						}
					}
					foreach (Hero notable3 in item2.Notables)
					{
						if (notable3.IsGangLeader && MBRandom.RandomFloat < 0.05f)
						{
							notable3.AddPower(settlementSecurityModel.DailyNotablePowerBonus);
						}
					}
				}
			}
			else
			{
				if (!item2.IsVillage || !(item2.Village.Bound.Town.Loyalty >= settlementLoyaltyModel.ThresholdForNotableRelationBonus))
				{
					continue;
				}
				foreach (Hero notable4 in item2.Notables)
				{
					if ((notable4.IsHeadman || notable4.IsRuralNotable) && MBRandom.RandomFloat < 0.05f)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(item2.OwnerClan.Leader, notable4, settlementLoyaltyModel.DailyNotableRelationBonus, showQuickNotification: false);
						flag = flag || item2.OwnerClan.Leader.IsHumanPlayerCharacter;
					}
				}
			}
		}
		if (flag2)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=ME5hmllb}Your relation with notables in some of your settlements increased due to high security").ToString()));
		}
		if (flag)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=0h5BrVdA}Your relation with notables in some of your settlements increased due to high loyalty").ToString()));
		}
	}

	public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if ((detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege || detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter) && oldOwner != null && oldOwner.MapFaction != null && oldOwner.MapFaction.Leader != oldOwner && oldOwner.IsAlive && oldOwner.MapFaction.Leader != Hero.MainHero)
		{
			float value = settlement.GetValue();
			int num = (int)((1f + MathF.Max(1f, MathF.Sqrt(value / 100000f))) * ((newOwner.MapFaction != oldOwner.MapFaction) ? 1f : 0.5f));
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(oldOwner, oldOwner.MapFaction.Leader, -num, showQuickNotification: false);
			if (capturerHero != null && capturerHero.Clan != capturerHero.MapFaction.Leader.Clan)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(capturerHero, capturerHero.MapFaction.Leader, num / 2, showQuickNotification: false);
			}
			if (oldOwner.Clan != null && settlement != null)
			{
				ChangeClanInfluenceAction.Apply(oldOwner.Clan, settlement.IsTown ? (-50) : (-25));
			}
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		MapEvent mapEvent = raidEvent.MapEvent;
		PartyBase leaderParty = mapEvent.AttackerSide.LeaderParty;
		Hero hero = leaderParty?.LeaderHero;
		PartyBase leaderParty2 = mapEvent.DefenderSide.LeaderParty;
		if (leaderParty == null || leaderParty.MapFaction == mapEvent.MapEventSettlement.MapFaction || winnerSide != BattleSideEnum.Attacker || hero == null || leaderParty2 == null || !leaderParty2.IsSettlement || !leaderParty2.Settlement.IsVillage || leaderParty2.Settlement.OwnerClan == Clan.PlayerClan)
		{
			return;
		}
		int num = -MathF.Ceiling(6f * raidEvent.RaidDamage);
		int num2 = -MathF.Ceiling(6f * raidEvent.RaidDamage * 0.5f);
		if (num < 0)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, leaderParty2.Settlement.OwnerClan.Leader, num);
		}
		if (num2 >= 0)
		{
			return;
		}
		foreach (Hero notable in leaderParty2.Settlement.Notables)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, notable, num2);
		}
	}

	private static void OnHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
	{
		ChangeRelationAction.ApplyRelationChangeBetweenHeroes(firstHero, secondHero, 30, showQuickNotification: false);
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion && detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom)
		{
			return;
		}
		int relationChange = ((detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion) ? (-40) : (-20));
		Hero leader = clan.Leader;
		foreach (Clan clan2 in oldKingdom.Clans)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader, clan2.Leader, relationChange);
		}
	}
}
