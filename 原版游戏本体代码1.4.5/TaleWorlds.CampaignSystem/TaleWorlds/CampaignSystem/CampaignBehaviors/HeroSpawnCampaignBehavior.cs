using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class HeroSpawnCampaignBehavior : CampaignBehaviorBase
{
	private const float MinimumScoreForSafeSettlement = 10f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnNonBanditClanDailyTick);
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnHeroDailyTick);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
	{
		if (i == 0)
		{
			int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			foreach (Clan item in Clan.All)
			{
				foreach (Hero hero in item.Heroes)
				{
					if (hero.Age >= (float)heroComesOfAge && hero.IsAlive && !hero.IsDisabled)
					{
						hero.ChangeState(Hero.CharacterStates.Active);
					}
				}
			}
		}
		int num = Clan.NonBanditFactions.Count();
		int num2 = num / 100 + ((num % 100 > i) ? 1 : 0);
		int num3 = num / 100;
		for (int j = 0; j < i; j++)
		{
			num3 += ((num % 100 > j) ? 1 : 0);
		}
		for (int k = 0; k < num2; k++)
		{
			TrySpawnHeroesAndParties(Clan.NonBanditFactions.ElementAt(num3 + k), isNewGame: true);
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			if (!nonBanditFaction.IsEliminated && nonBanditFaction.IsMinorFaction && nonBanditFaction != Clan.PlayerClan)
			{
				SpawnMinorFactionHeroes(nonBanditFaction, firstTime: true);
				CheckAndAssignClanLeader(nonBanditFaction);
				nonBanditFaction.ConsiderAndUpdateHomeSettlement();
			}
		}
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero.IsActive)
			{
				AssignSettlementToHeroOnGameStart(allAliveHero);
			}
		}
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (!hero.IsDisabled && hero.HeroState != Hero.CharacterStates.Active && !hero.IsTraveling)
		{
			hero.ChangeState(Hero.CharacterStates.Active);
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, hero.HomeSettlement);
		}
	}

	private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		if (!companion.IsFugitive && !companion.IsDead && detail != RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord && detail != RemoveCompanionAction.RemoveCompanionDetail.Death && companion.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
		{
			Settlement targetSettlement = FindASuitableSettlementToTeleportForHero(companion);
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(companion, targetSettlement);
		}
	}

	private void AssignSettlementToHeroOnGameStart(Hero hero)
	{
		if (hero.CurrentSettlement == null && hero.PartyBelongedTo == null && !hero.IsSpecial && hero.GovernorOf == null)
		{
			if (MobileParty.MainParty.MemberRoster.Contains(hero.CharacterObject))
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject);
				MobileParty.MainParty.MemberRoster.AddToCounts(hero.CharacterObject, 1);
			}
			else
			{
				Settlement targetSettlement = FindASuitableSettlementToTeleportForHero(hero);
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, targetSettlement);
			}
		}
	}

	private void OnHeroDailyTick(Hero hero)
	{
		Settlement settlement = null;
		if (hero.IsFugitive || hero.IsReleased)
		{
			if (!hero.IsSpecial && (hero.IsPlayerCompanion || MBRandom.RandomFloat < 0.3f || (hero.CurrentSettlement != null && hero.CurrentSettlement.MapFaction.IsAtWarWith(hero.MapFaction))))
			{
				settlement = FindASuitableSettlementToTeleportForHero(hero);
			}
		}
		else if (hero.IsActive && CanHeroMoveToAnotherSettlement(hero))
		{
			settlement = FindASuitableSettlementToTeleportForHero(hero, 10f);
		}
		if (settlement != null)
		{
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, settlement);
			if (!hero.IsActive)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
		}
	}

	private void OnNonBanditClanDailyTick(Clan clan)
	{
		TrySpawnHeroesAndParties(clan, isNewGame: false);
	}

	private void TrySpawnHeroesAndParties(Clan clan, bool isNewGame)
	{
		if (!clan.IsEliminated && clan != Clan.PlayerClan)
		{
			if (clan.IsMinorFaction)
			{
				SpawnMinorFactionHeroes(clan, firstTime: false);
			}
			ConsiderSpawningLordParties(clan, isNewGame);
		}
	}

	private bool CanHeroMoveToAnotherSettlement(Hero hero)
	{
		if (hero.Clan != Clan.PlayerClan && !hero.IsTemplate && hero.IsAlive && !hero.IsNotable && !hero.IsHumanPlayerCharacter && !hero.IsPartyLeader && !hero.IsPrisoner && hero.HeroState != Hero.CharacterStates.Disabled && hero.GovernorOf == null && hero.PartyBelongedTo == null && !hero.IsWanderer && hero.PartyBelongedToAsPrisoner == null && hero.CharacterObject.Occupation != Occupation.Special && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && (hero.CurrentSettlement?.Town == null || (!hero.CurrentSettlement.Town.HasTournament && !hero.CurrentSettlement.IsUnderSiege)))
		{
			return hero.CanMoveToSettlement();
		}
		return false;
	}

	private float GetHeroPartyCommandScore(Hero hero)
	{
		return 3f * (float)hero.GetSkillValue(DefaultSkills.Tactics) + 2f * (float)hero.GetSkillValue(DefaultSkills.Leadership) + (float)hero.GetSkillValue(DefaultSkills.Scouting) + (float)hero.GetSkillValue(DefaultSkills.Steward) + (float)hero.GetSkillValue(DefaultSkills.OneHanded) + (float)hero.GetSkillValue(DefaultSkills.TwoHanded) + (float)hero.GetSkillValue(DefaultSkills.Polearm) + (float)hero.GetSkillValue(DefaultSkills.Riding) + ((hero.Clan.Leader == hero) ? 1000f : 0f) + ((hero.GovernorOf == null) ? 500f : 0f) + (float)(hero.IsNoncombatant ? (-5000) : 0);
	}

	private void ConsiderSpawningLordParties(Clan clan, bool isNewGame)
	{
		int partyLimitForTier = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier);
		int count = clan.WarPartyComponents.Count;
		if (count >= partyLimitForTier)
		{
			return;
		}
		int num = partyLimitForTier - count;
		for (int i = 0; i < num; i++)
		{
			Hero bestAvailableCommander = GetBestAvailableCommander(clan);
			if (bestAvailableCommander == null)
			{
				break;
			}
			float num2 = CalculateScoreToCreateParty(clan);
			if (GetHeroPartyCommandScore(bestAvailableCommander) + num2 > 100f)
			{
				MobileParty mobileParty = SpawnLordParty(bestAvailableCommander, isNewGame);
				if (mobileParty != null)
				{
					GiveInitialItemsToParty(mobileParty);
				}
			}
		}
	}

	private float CalculateScoreToCreateParty(Clan clan)
	{
		return (float)(clan.Fiefs.Count * 100 - clan.WarPartyComponents.Count * 100) + (float)clan.Gold * 0.01f + (clan.IsMinorFaction ? 200f : 0f) + ((clan.WarPartyComponents.Count > 0) ? 0f : 200f);
	}

	private Hero GetBestAvailableCommander(Clan clan)
	{
		Hero hero = null;
		float num = 0f;
		foreach (Hero hero2 in clan.Heroes)
		{
			if (hero2.IsActive && hero2.IsAlive && hero2.PartyBelongedTo == null && hero2.PartyBelongedToAsPrisoner == null && hero2.CanLeadParty() && hero2.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && hero2.CharacterObject.Occupation == Occupation.Lord)
			{
				float heroPartyCommandScore = GetHeroPartyCommandScore(hero2);
				if (heroPartyCommandScore > num)
				{
					num = heroPartyCommandScore;
					hero = hero2;
				}
			}
		}
		if (hero != null)
		{
			return hero;
		}
		if (clan != Clan.PlayerClan)
		{
			foreach (Hero hero3 in clan.Heroes)
			{
				if (hero3.IsActive && hero3.IsAlive && hero3.PartyBelongedTo == null && hero3.PartyBelongedToAsPrisoner == null && hero3.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && hero3.CharacterObject.Occupation == Occupation.Lord)
				{
					float heroPartyCommandScore2 = GetHeroPartyCommandScore(hero3);
					if (heroPartyCommandScore2 > num)
					{
						num = heroPartyCommandScore2;
						hero = hero3;
					}
				}
			}
		}
		return hero;
	}

	private MobileParty SpawnLordParty(Hero hero, bool isNewGame)
	{
		if (hero.GovernorOf != null)
		{
			ChangeGovernorAction.RemoveGovernorOf(hero);
		}
		Settlement settlement = SettlementHelper.GetBestSettlementToSpawnAround(hero);
		if (settlement == null || settlement.MapFaction != hero.MapFaction)
		{
			settlement = hero.MapFaction.InitialHomeSettlement;
		}
		if (settlement == null)
		{
			settlement = Settlement.All.First((Settlement x) => x.Culture == hero.Culture);
		}
		MobileParty mobileParty = MobilePartyHelper.SpawnLordParty(hero, settlement.GatePosition, Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) / 2f);
		if (isNewGame)
		{
			int num = (int)((float)(mobileParty.Party.PartySizeLimit - mobileParty.MemberRoster.TotalManCount) * MBRandom.RandomFloatRanged(0.75f, 0.9f));
			PartyTemplateObject defaultPartyTemplate = mobileParty.LordPartyComponent.Owner.Clan.DefaultPartyTemplate;
			List<(CharacterObject, float)> list = new List<(CharacterObject, float)>();
			foreach (PartyTemplateStack stack in defaultPartyTemplate.Stacks)
			{
				list.Add((stack.Character, (float)(stack.MinValue + stack.MaxValue) / 2f));
			}
			for (int num2 = 0; num2 < num; num2++)
			{
				CharacterObject element = MBRandom.ChooseWeighted(list);
				mobileParty.AddElementToMemberRoster(element, 1);
			}
		}
		return mobileParty;
	}

	private void GiveInitialItemsToParty(MobileParty heroParty)
	{
		float num = 2f * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (!settlement.IsVillage)
			{
				continue;
			}
			float estimatedLandRatio;
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(heroParty, settlement, isTargetingPort: false, heroParty.NavigationCapability, out estimatedLandRatio);
			if (!(distance < num))
			{
				continue;
			}
			foreach (var production in settlement.Village.VillageType.Productions)
			{
				ItemObject item = production.Item1;
				float item2 = production.Item2;
				float num2 = ((item.ItemType == ItemObject.ItemTypeEnum.Horse && item.HorseComponent.IsRideable && !item.HorseComponent.IsPackAnimal) ? 7f : (item.IsFood ? 0.1f : 0f));
				float num3 = ((float)heroParty.MemberRoster.TotalManCount + 2f) / 200f;
				float num4 = 1f - distance / num;
				int num5 = MBRandom.RoundRandomized(num2 * item2 * num4 * num3);
				if (num5 > 0)
				{
					heroParty.ItemRoster.AddToCounts(item, num5);
				}
			}
		}
	}

	private void CheckAndAssignClanLeader(Clan clan)
	{
		if (clan.Leader == null || clan.Leader.IsDead)
		{
			Hero hero = clan.AliveLords.FirstOrDefault();
			if (hero != null)
			{
				clan.SetLeader(hero);
			}
			else
			{
				Debug.FailedAssert("Cant find a lord to assign as leader to minor faction.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroSpawnCampaignBehavior.cs", "CheckAndAssignClanLeader", 438);
			}
		}
	}

	private void CreateMinorFactionHeroFromTemplate(CharacterObject template, Clan faction)
	{
		Hero hero = HeroCreator.CreateSpecialHero(template, null, faction, null, Campaign.Current.GameStarted ? 19 : (-1));
		hero.ChangeState(Campaign.Current.GameStarted ? Hero.CharacterStates.Active : Hero.CharacterStates.NotSpawned);
		hero.IsMinorFactionHero = true;
	}

	private void SpawnMinorFactionHeroes(Clan clan, bool firstTime)
	{
		int num = Campaign.Current.Models.MinorFactionsModel.MinorFactionHeroLimit - clan.AliveLords.Count;
		if (num <= 0)
		{
			return;
		}
		if (firstTime)
		{
			for (int i = 0; i < clan.MinorFactionCharacterTemplates.Count; i++)
			{
				if (num <= 0)
				{
					break;
				}
				CharacterObject template = clan.MinorFactionCharacterTemplates[i];
				CreateMinorFactionHeroFromTemplate(template, clan);
				num--;
			}
		}
		if (num <= 0)
		{
			return;
		}
		if (clan.MinorFactionCharacterTemplates == null || clan.MinorFactionCharacterTemplates.IsEmpty())
		{
			Debug.FailedAssert($"{clan.Name} templates are empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroSpawnCampaignBehavior.cs", "SpawnMinorFactionHeroes", 475);
			return;
		}
		for (int j = 0; j < num; j++)
		{
			if (MBRandom.RandomFloat < Campaign.Current.Models.MinorFactionsModel.DailyMinorFactionHeroSpawnChance)
			{
				CharacterObject randomElementInefficiently = clan.MinorFactionCharacterTemplates.GetRandomElementInefficiently();
				CreateMinorFactionHeroFromTemplate(randomElementInefficiently, clan);
			}
		}
	}

	private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		if (oldGovernor != null && oldGovernor.Clan != null)
		{
			foreach (Hero hero in oldGovernor.Clan.Heroes)
			{
				hero.UpdateHomeSettlement();
			}
		}
		if (newGovernor == null || newGovernor.Clan == null || (oldGovernor != null && newGovernor.Clan == oldGovernor.Clan))
		{
			return;
		}
		foreach (Hero hero2 in newGovernor.Clan.Heroes)
		{
			hero2.UpdateHomeSettlement();
		}
	}

	private Settlement FindASuitableSettlementToTeleportForHero(Hero hero, float minimumScore = 0f)
	{
		Settlement settlement = null;
		if (hero.IsNotable)
		{
			Debug.FailedAssert("Notables should not use here anymore, make sure this case is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroSpawnCampaignBehavior.cs", "FindASuitableSettlementToTeleportForHero", 519);
			settlement = hero.BornSettlement;
		}
		else
		{
			List<(Settlement, float)> list = new List<(Settlement, float)>(hero.MapFaction.Fiefs.Count);
			foreach (Town fief in hero.MapFaction.Fiefs)
			{
				float moveScoreForHero = GetMoveScoreForHero(hero, fief);
				list.Add((fief.Settlement, (moveScoreForHero >= minimumScore) ? moveScoreForHero : 0f));
			}
			settlement = MBRandom.ChooseWeighted(list);
			if (settlement == null)
			{
				List<Settlement> list2 = new List<Settlement>();
				List<Settlement> list3 = new List<Settlement>();
				foreach (Town allFief in Town.AllFiefs)
				{
					if (allFief.MapFaction.IsAtWarWith(hero.MapFaction))
					{
						if (allFief.IsTown)
						{
							list3.Add(allFief.Settlement);
						}
					}
					else
					{
						list2.Add(allFief.Settlement);
					}
				}
				foreach (Settlement item in list2)
				{
					float moveScoreForHero2 = GetMoveScoreForHero(hero, item.Town);
					list.Add((item, (moveScoreForHero2 >= minimumScore) ? moveScoreForHero2 : 0f));
				}
				settlement = MBRandom.ChooseWeighted(list);
				if (settlement == null)
				{
					list.Clear();
					foreach (Settlement item2 in list3)
					{
						float moveScoreForHero3 = GetMoveScoreForHero(hero, item2.Town);
						list.Add((item2, (moveScoreForHero3 >= minimumScore) ? moveScoreForHero3 : 0f));
					}
					settlement = MBRandom.ChooseWeighted(list);
				}
			}
		}
		return settlement;
	}

	private float GetMoveScoreForHero(Hero hero, Town fief)
	{
		Clan clan = hero.Clan;
		float num = 1E-06f;
		if (!fief.IsUnderSiege && !fief.MapFaction.IsAtWarWith(hero.MapFaction) && (!fief.IsCastle || hero.Occupation != Occupation.Wanderer || hero.IsPlayerCompanion))
		{
			num = (DiplomacyHelper.IsSameFactionAndNotEliminated(fief.MapFaction, hero.MapFaction) ? 0.01f : 1E-05f);
			if (fief.MapFaction == hero.MapFaction)
			{
				num += 10f;
				if (fief.IsTown)
				{
					num += 100f;
				}
				if (fief.OwnerClan == clan)
				{
					num += (fief.IsTown ? 500f : 100f);
				}
				if (fief.HasTournament)
				{
					num += 400f;
				}
			}
			foreach (Hero item in fief.Settlement.HeroesWithoutParty)
			{
				if (clan != null && item.Clan == clan)
				{
					num += (fief.IsTown ? 100f : 10f);
				}
			}
			if (hero.IsFugitive && hero.HomeSettlement?.Town == fief)
			{
				num += 100f;
			}
			if (fief.Settlement.IsStarving)
			{
				num *= 0.1f;
			}
			if (hero.CurrentSettlement == fief.Settlement)
			{
				num *= 3f;
			}
		}
		return num;
	}
}
