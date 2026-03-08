using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F5 RID: 1013
	public class HeroSpawnCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003F31 RID: 16177 RVA: 0x0011D328 File Offset: 0x0011B528
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(HeroSpawnCampaignBehavior.OnNewGameCreated));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, new Action<Town, Hero, Hero>(this.OnGovernorChanged));
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnNonBanditClanDailyTick));
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(HeroSpawnCampaignBehavior.OnHeroComesOfAge));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroDailyTick));
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.OnCompanionRemoved));
		}

		// Token: 0x06003F32 RID: 16178 RVA: 0x0011D3F0 File Offset: 0x0011B5F0
		private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
		{
			if (i == 0)
			{
				int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
				foreach (Clan clan in Clan.All)
				{
					foreach (Hero hero in clan.Heroes)
					{
						if (hero.Age >= (float)heroComesOfAge && hero.IsAlive && !hero.IsDisabled)
						{
							hero.ChangeState(Hero.CharacterStates.Active);
						}
					}
				}
			}
			int num = Clan.NonBanditFactions.Count<Clan>();
			int num2 = num / 100 + ((num % 100 > i) ? 1 : 0);
			int num3 = num / 100;
			for (int j = 0; j < i; j++)
			{
				num3 += ((num % 100 > j) ? 1 : 0);
			}
			for (int k = 0; k < num2; k++)
			{
				this.TrySpawnHeroesAndParties(Clan.NonBanditFactions.ElementAt(num3 + k), true);
			}
		}

		// Token: 0x06003F33 RID: 16179 RVA: 0x0011D51C File Offset: 0x0011B71C
		private static void OnNewGameCreated(CampaignGameStarter starter)
		{
			foreach (Clan clan in Clan.NonBanditFactions)
			{
				if (!clan.IsEliminated && clan.IsMinorFaction && clan != Clan.PlayerClan)
				{
					HeroSpawnCampaignBehavior.SpawnMinorFactionHeroes(clan, true);
					HeroSpawnCampaignBehavior.CheckAndAssignClanLeader(clan);
					clan.ConsiderAndUpdateHomeSettlement();
				}
			}
		}

		// Token: 0x06003F34 RID: 16180 RVA: 0x0011D58C File Offset: 0x0011B78C
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero.IsActive)
				{
					this.OnHeroDailyTick(hero);
				}
			}
		}

		// Token: 0x06003F35 RID: 16181 RVA: 0x0011D5E8 File Offset: 0x0011B7E8
		private static void OnHeroComesOfAge(Hero hero)
		{
			if (!hero.IsDisabled && hero.HeroState != Hero.CharacterStates.Active && !hero.IsTraveling)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, hero.HomeSettlement);
			}
		}

		// Token: 0x06003F36 RID: 16182 RVA: 0x0011D616 File Offset: 0x0011B816
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003F37 RID: 16183 RVA: 0x0011D618 File Offset: 0x0011B818
		private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			if (!companion.IsFugitive && !companion.IsDead && detail != RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord && detail != RemoveCompanionAction.RemoveCompanionDetail.Death && companion.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
			{
				Settlement settlement = HeroHelper.FindASuitableSettlementToTeleportForHero(companion, 0f);
				if (settlement == null)
				{
					settlement = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown);
				}
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(companion, settlement);
			}
		}

		// Token: 0x06003F38 RID: 16184 RVA: 0x0011D680 File Offset: 0x0011B880
		private void OnHeroDailyTick(Hero hero)
		{
			Settlement settlement = null;
			if (hero.IsFugitive || hero.IsReleased)
			{
				if (!hero.IsSpecial && (hero.IsPlayerCompanion || MBRandom.RandomFloat < 0.3f || (hero.CurrentSettlement != null && hero.CurrentSettlement.MapFaction.IsAtWarWith(hero.MapFaction))))
				{
					settlement = HeroHelper.FindASuitableSettlementToTeleportForHero(hero, 0f);
				}
			}
			else if (hero.IsActive)
			{
				if (hero.CurrentSettlement == null && hero.PartyBelongedTo == null && !hero.IsSpecial && hero.GovernorOf == null)
				{
					if (MobileParty.MainParty.MemberRoster.Contains(hero.CharacterObject))
					{
						MobileParty.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
						MobileParty.MainParty.MemberRoster.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
					}
					else
					{
						settlement = HeroHelper.FindASuitableSettlementToTeleportForHero(hero, 0f);
					}
				}
				else if (HeroSpawnCampaignBehavior.CanHeroMoveToAnotherSettlement(hero))
				{
					settlement = HeroHelper.FindASuitableSettlementToTeleportForHero(hero, 10f);
				}
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

		// Token: 0x06003F39 RID: 16185 RVA: 0x0011D7B0 File Offset: 0x0011B9B0
		private void OnNonBanditClanDailyTick(Clan clan)
		{
			this.TrySpawnHeroesAndParties(clan, false);
		}

		// Token: 0x06003F3A RID: 16186 RVA: 0x0011D7BA File Offset: 0x0011B9BA
		private void TrySpawnHeroesAndParties(Clan clan, bool isNewGame)
		{
			if (!clan.IsEliminated && clan != Clan.PlayerClan)
			{
				if (clan.IsMinorFaction)
				{
					HeroSpawnCampaignBehavior.SpawnMinorFactionHeroes(clan, false);
				}
				this.ConsiderSpawningLordParties(clan, isNewGame);
			}
		}

		// Token: 0x06003F3B RID: 16187 RVA: 0x0011D7E4 File Offset: 0x0011B9E4
		private static bool CanHeroMoveToAnotherSettlement(Hero hero)
		{
			if (hero.Clan != Clan.PlayerClan && !hero.IsTemplate && hero.IsAlive && !hero.IsNotable && !hero.IsHumanPlayerCharacter && !hero.IsPartyLeader && !hero.IsPrisoner && hero.HeroState != Hero.CharacterStates.Disabled && hero.GovernorOf == null && hero.PartyBelongedTo == null && !hero.IsWanderer && hero.PartyBelongedToAsPrisoner == null && hero.CharacterObject.Occupation != Occupation.Special && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				Settlement currentSettlement = hero.CurrentSettlement;
				if (((currentSettlement != null) ? currentSettlement.Town : null) == null || (!hero.CurrentSettlement.Town.HasTournament && !hero.CurrentSettlement.IsUnderSiege))
				{
					return hero.CanMoveToSettlement();
				}
			}
			return false;
		}

		// Token: 0x06003F3C RID: 16188 RVA: 0x0011D8D8 File Offset: 0x0011BAD8
		private static float GetHeroPartyCommandScore(Hero hero)
		{
			return 3f * (float)hero.GetSkillValue(DefaultSkills.Tactics) + 2f * (float)hero.GetSkillValue(DefaultSkills.Leadership) + (float)hero.GetSkillValue(DefaultSkills.Scouting) + (float)hero.GetSkillValue(DefaultSkills.Steward) + (float)hero.GetSkillValue(DefaultSkills.OneHanded) + (float)hero.GetSkillValue(DefaultSkills.TwoHanded) + (float)hero.GetSkillValue(DefaultSkills.Polearm) + (float)hero.GetSkillValue(DefaultSkills.Riding) + ((hero.Clan.Leader == hero) ? 1000f : 0f) + ((hero.GovernorOf == null) ? 500f : 0f) + (float)(hero.IsNoncombatant ? (-5000) : 0);
		}

		// Token: 0x06003F3D RID: 16189 RVA: 0x0011D99C File Offset: 0x0011BB9C
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
				Hero bestAvailableCommander = HeroSpawnCampaignBehavior.GetBestAvailableCommander(clan);
				if (bestAvailableCommander == null)
				{
					break;
				}
				float num2 = HeroSpawnCampaignBehavior.CalculateScoreToCreateParty(clan);
				if (HeroSpawnCampaignBehavior.GetHeroPartyCommandScore(bestAvailableCommander) + num2 > 100f)
				{
					MobileParty mobileParty = this.SpawnLordParty(bestAvailableCommander, isNewGame);
					if (mobileParty != null)
					{
						this.GiveInitialItemsToParty(mobileParty);
					}
				}
			}
		}

		// Token: 0x06003F3E RID: 16190 RVA: 0x0011DA24 File Offset: 0x0011BC24
		private static float CalculateScoreToCreateParty(Clan clan)
		{
			return (float)(clan.Fiefs.Count * 100 - clan.WarPartyComponents.Count * 100) + (float)clan.Gold * 0.01f + (clan.IsMinorFaction ? 200f : 0f) + ((clan.WarPartyComponents.Count > 0) ? 0f : 200f);
		}

		// Token: 0x06003F3F RID: 16191 RVA: 0x0011DA90 File Offset: 0x0011BC90
		private static Hero GetBestAvailableCommander(Clan clan)
		{
			Hero hero = null;
			float num = 0f;
			foreach (Hero hero2 in clan.Heroes)
			{
				if (hero2.IsActive && hero2.IsAlive && hero2.PartyBelongedTo == null && hero2.PartyBelongedToAsPrisoner == null && hero2.CanLeadParty() && hero2.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && hero2.CharacterObject.Occupation == Occupation.Lord)
				{
					float heroPartyCommandScore = HeroSpawnCampaignBehavior.GetHeroPartyCommandScore(hero2);
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
						float heroPartyCommandScore2 = HeroSpawnCampaignBehavior.GetHeroPartyCommandScore(hero3);
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

		// Token: 0x06003F40 RID: 16192 RVA: 0x0011DBFC File Offset: 0x0011BDFC
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
				List<ValueTuple<CharacterObject, float>> list = new List<ValueTuple<CharacterObject, float>>();
				foreach (PartyTemplateStack partyTemplateStack in defaultPartyTemplate.Stacks)
				{
					list.Add(new ValueTuple<CharacterObject, float>(partyTemplateStack.Character, (float)(partyTemplateStack.MinValue + partyTemplateStack.MaxValue) / 2f));
				}
				for (int i = 0; i < num; i++)
				{
					CharacterObject element = MBRandom.ChooseWeighted<CharacterObject>(list);
					mobileParty.AddElementToMemberRoster(element, 1, false);
				}
			}
			return mobileParty;
		}

		// Token: 0x06003F41 RID: 16193 RVA: 0x0011DD78 File Offset: 0x0011BF78
		private void GiveInitialItemsToParty(MobileParty heroParty)
		{
			float num = 2f * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
			foreach (Settlement settlement in Campaign.Current.Settlements)
			{
				if (settlement.IsVillage)
				{
					float num2;
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(heroParty, settlement, false, heroParty.NavigationCapability, out num2);
					if (distance < num)
					{
						foreach (ValueTuple<ItemObject, float> valueTuple in settlement.Village.VillageType.Productions)
						{
							ItemObject item = valueTuple.Item1;
							float item2 = valueTuple.Item2;
							float num3 = ((item.ItemType == ItemObject.ItemTypeEnum.Horse && item.HorseComponent.IsRideable && !item.HorseComponent.IsPackAnimal) ? 7f : (item.IsFood ? 0.1f : 0f));
							float num4 = ((float)heroParty.MemberRoster.TotalManCount + 2f) / 200f;
							float num5 = 1f - distance / num;
							int num6 = MBRandom.RoundRandomized(num3 * item2 * num5 * num4);
							if (num6 > 0)
							{
								heroParty.ItemRoster.AddToCounts(item, num6);
							}
						}
					}
				}
			}
		}

		// Token: 0x06003F42 RID: 16194 RVA: 0x0011DF18 File Offset: 0x0011C118
		private static void CheckAndAssignClanLeader(Clan clan)
		{
			if (clan.Leader == null || clan.Leader.IsDead)
			{
				Hero hero = clan.AliveLords.FirstOrDefault<Hero>();
				if (hero != null)
				{
					clan.SetLeader(hero);
					return;
				}
				Debug.FailedAssert("Cant find a lord to assign as leader to minor faction.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroSpawnCampaignBehavior.cs", "CheckAndAssignClanLeader", 432);
			}
		}

		// Token: 0x06003F43 RID: 16195 RVA: 0x0011DF6A File Offset: 0x0011C16A
		private static Hero CreateMinorFactionHeroFromTemplate(CharacterObject template, Clan faction)
		{
			Hero hero = HeroCreator.CreateSpecialHero(template, null, faction, null, Campaign.Current.GameStarted ? 19 : (-1));
			hero.ChangeState(Campaign.Current.GameStarted ? Hero.CharacterStates.Active : Hero.CharacterStates.NotSpawned);
			hero.IsMinorFactionHero = true;
			return hero;
		}

		// Token: 0x06003F44 RID: 16196 RVA: 0x0011DFA4 File Offset: 0x0011C1A4
		private static void SpawnMinorFactionHeroes(Clan clan, bool firstTime)
		{
			int num = Campaign.Current.Models.MinorFactionsModel.MinorFactionHeroLimit - clan.AliveLords.Count;
			if (num > 0)
			{
				if (firstTime)
				{
					int num2 = 0;
					while (num2 < clan.MinorFactionCharacterTemplates.Count && num > 0)
					{
						HeroSpawnCampaignBehavior.CreateMinorFactionHeroFromTemplate(clan.MinorFactionCharacterTemplates[num2], clan);
						num--;
						num2++;
					}
				}
				if (num > 0)
				{
					if (clan.MinorFactionCharacterTemplates == null || clan.MinorFactionCharacterTemplates.IsEmpty<CharacterObject>())
					{
						Debug.FailedAssert(string.Format("{0} templates are empty!", clan.Name), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroSpawnCampaignBehavior.cs", "SpawnMinorFactionHeroes", 470);
						return;
					}
					for (int i = 0; i < num; i++)
					{
						if (MBRandom.RandomFloat < Campaign.Current.Models.MinorFactionsModel.DailyMinorFactionHeroSpawnChance)
						{
							HeroSpawnCampaignBehavior.CreateMinorFactionHeroFromTemplate(clan.MinorFactionCharacterTemplates.GetRandomElementInefficiently<CharacterObject>(), clan);
						}
					}
				}
			}
		}

		// Token: 0x06003F45 RID: 16197 RVA: 0x0011E084 File Offset: 0x0011C284
		private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
		{
			if (oldGovernor != null && oldGovernor.Clan != null)
			{
				foreach (Hero hero in oldGovernor.Clan.Heroes)
				{
					hero.UpdateHomeSettlement();
				}
			}
			if (newGovernor != null && newGovernor.Clan != null && (oldGovernor == null || newGovernor.Clan != oldGovernor.Clan))
			{
				foreach (Hero hero2 in newGovernor.Clan.Heroes)
				{
					hero2.UpdateHomeSettlement();
				}
			}
		}

		// Token: 0x040012C0 RID: 4800
		private const float DefaultHealingPercentage = 0.015f;

		// Token: 0x040012C1 RID: 4801
		private const float MinimumScoreForSafeSettlement = 10f;
	}
}
