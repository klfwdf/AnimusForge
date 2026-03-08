using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041C RID: 1052
	public class NotablesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042A4 RID: 17060 RVA: 0x001411E0 File Offset: 0x0013F3E0
		public NotablesCampaignBehavior()
		{
			this._settlementPassedDaysForWeeklyTick = new Dictionary<Settlement, int>();
		}

		// Token: 0x060042A5 RID: 17061 RVA: 0x001411F4 File Offset: 0x0013F3F4
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(this.WeeklyTick));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
		}

		// Token: 0x060042A6 RID: 17062 RVA: 0x001412BC File Offset: 0x0013F4BC
		private void OnHeroCreated(Hero hero, bool isBornNaturally)
		{
			if (hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Headman)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
				EnterSettlementAction.ApplyForCharacterOnly(hero, hero.HomeSettlement);
				GiveGoldAction.ApplyBetweenCharacters(null, hero, 10000, true);
				CharacterObject template = hero.Template;
				bool flag;
				if (template == null)
				{
					flag = null != null;
				}
				else
				{
					Hero heroObject = template.HeroObject;
					flag = ((heroObject != null) ? heroObject.Clan : null) != null;
				}
				if (flag && hero.Template.HeroObject.Clan.IsMinorFaction)
				{
					hero.SupporterOf = hero.Template.HeroObject.Clan;
					return;
				}
				hero.SupporterOf = HeroHelper.GetRandomClanForNotable(hero);
			}
		}

		// Token: 0x060042A7 RID: 17063 RVA: 0x00141378 File Offset: 0x0013F578
		private void WeeklyTick()
		{
			foreach (Hero hero in Hero.DeadOrDisabledHeroes.ToList<Hero>())
			{
				if (hero.IsDead && hero.IsNotable && hero.DeathDay.ElapsedDaysUntilNow >= 7f)
				{
					Campaign.Current.CampaignObjectManager.UnregisterDeadHero(hero);
				}
			}
		}

		// Token: 0x060042A8 RID: 17064 RVA: 0x00141400 File Offset: 0x0013F600
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.WeeklyTick();
		}

		// Token: 0x060042A9 RID: 17065 RVA: 0x00141408 File Offset: 0x0013F608
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Settlement, int>>("_settlementPassedDaysForWeeklyTick", ref this._settlementPassedDaysForWeeklyTick);
		}

		// Token: 0x060042AA RID: 17066 RVA: 0x0014141C File Offset: 0x0013F61C
		public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.SpawnNotablesAtGameStart();
		}

		// Token: 0x060042AB RID: 17067 RVA: 0x00141424 File Offset: 0x0013F624
		private void DetermineRelation(Hero hero1, Hero hero2, float randomValue, float chanceOfConflict)
		{
			float num = 0.3f;
			if (randomValue < num)
			{
				int num2 = (int)((num - randomValue) * (num - randomValue) / (num * num) * 100f);
				if (num2 > 0)
				{
					hero1.SetPersonalRelation(hero2, num2);
					return;
				}
			}
			else if (randomValue > 1f - chanceOfConflict)
			{
				int num3 = -(int)((randomValue - (1f - chanceOfConflict)) * (randomValue - (1f - chanceOfConflict)) / (chanceOfConflict * chanceOfConflict) * 100f);
				if (num3 < 0)
				{
					hero1.SetPersonalRelation(hero2, num3);
				}
			}
		}

		// Token: 0x060042AC RID: 17068 RVA: 0x00141498 File Offset: 0x0013F698
		public void SetInitialRelationsBetweenNotablesAndLords()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				for (int i = 0; i < settlement.Notables.Count; i++)
				{
					Hero hero = settlement.Notables[i];
					foreach (Hero hero2 in settlement.MapFaction.AliveLords.Union(settlement.MapFaction.DeadLords))
					{
						if (hero2 != hero && hero2 == hero2.Clan.Leader && hero2.MapFaction == settlement.MapFaction)
						{
							float chanceOfConflict = (float)HeroHelper.NPCPersonalityClashWithNPC(hero, hero2) * 0.01f * 2.5f;
							float num = MBRandom.RandomFloat;
							float num2 = Campaign.MapDiagonal;
							foreach (Settlement fromSettlement in hero2.Clan.Settlements)
							{
								float num3 = DistanceHelper.FindClosestDistanceFromSettlementToSettlement(fromSettlement, settlement, MobileParty.NavigationType.All);
								if (num3 < num2)
								{
									num2 = num3;
								}
							}
							float num4 = 0.75f * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
							float num5 = ((num2 < num4) ? (1f - num2 / num4) : 0f);
							float num6 = num5 * MBRandom.RandomFloat + (1f - num5);
							if (MBRandom.RandomFloat < 0.2f)
							{
								num6 = 1f / (0.5f + 0.5f * num6);
							}
							num *= num6;
							if (num > 1f)
							{
								num = 1f;
							}
							this.DetermineRelation(hero, hero2, num, chanceOfConflict);
						}
						for (int j = i + 1; j < settlement.Notables.Count; j++)
						{
							Hero hero3 = settlement.Notables[j];
							float chanceOfConflict2 = (float)HeroHelper.NPCPersonalityClashWithNPC(hero, hero3) * 0.01f * 2.5f;
							float randomValue = MBRandom.RandomFloat;
							if (hero.CharacterObject.Occupation == hero3.CharacterObject.Occupation)
							{
								randomValue = 1f - 0.25f * MBRandom.RandomFloat;
							}
							this.DetermineRelation(hero, hero3, randomValue, chanceOfConflict2);
						}
					}
				}
			}
		}

		// Token: 0x060042AD RID: 17069 RVA: 0x00141740 File Offset: 0x0013F940
		public void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
		{
			if (i == 1)
			{
				this.SetInitialRelationsBetweenNotablesAndLords();
				int num = 50;
				for (int j = 0; j < num; j++)
				{
					foreach (Hero hero in Hero.AllAliveHeroes)
					{
						if (hero.IsNotable)
						{
							this.UpdateNotableSupport(hero);
						}
					}
				}
			}
		}

		// Token: 0x060042AE RID: 17070 RVA: 0x001417B4 File Offset: 0x0013F9B4
		private void DailyTickSettlement(Settlement settlement)
		{
			if (this._settlementPassedDaysForWeeklyTick.ContainsKey(settlement))
			{
				Dictionary<Settlement, int> settlementPassedDaysForWeeklyTick = this._settlementPassedDaysForWeeklyTick;
				int num = settlementPassedDaysForWeeklyTick[settlement];
				settlementPassedDaysForWeeklyTick[settlement] = num + 1;
				if (this._settlementPassedDaysForWeeklyTick[settlement] == CampaignTime.DaysInWeek)
				{
					SettlementHelper.SpawnNotablesIfNeeded(settlement);
					this._settlementPassedDaysForWeeklyTick[settlement] = 0;
					return;
				}
			}
			else
			{
				this._settlementPassedDaysForWeeklyTick.Add(settlement, 0);
			}
		}

		// Token: 0x060042AF RID: 17071 RVA: 0x0014181C File Offset: 0x0013FA1C
		private void UpdateNotableRelations(Hero notable)
		{
			foreach (Clan clan in Clan.All)
			{
				if (clan != Clan.PlayerClan && clan.Leader != null && !clan.IsEliminated)
				{
					int relation = notable.GetRelation(clan.Leader);
					if (relation > 0)
					{
						float num = (float)relation / 1000f;
						if (MBRandom.RandomFloat < num)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, clan.Leader, -20, true);
						}
					}
					else if (relation < 0)
					{
						float num2 = (float)(-(float)relation) / 1000f;
						if (MBRandom.RandomFloat < num2)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, clan.Leader, 20, true);
						}
					}
				}
			}
		}

		// Token: 0x060042B0 RID: 17072 RVA: 0x001418DC File Offset: 0x0013FADC
		private void UpdateNotableSupport(Hero notable)
		{
			if (notable.SupporterOf == null)
			{
				using (IEnumerator<Clan> enumerator = Clan.NonBanditFactions.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Clan clan = enumerator.Current;
						if (clan.Leader != null && clan != Clan.PlayerClan)
						{
							int relation = notable.GetRelation(clan.Leader);
							if (relation > 50)
							{
								float num = (float)(relation - 50) / 2000f;
								if (MBRandom.RandomFloat < num)
								{
									notable.SupporterOf = clan;
								}
							}
						}
					}
					return;
				}
			}
			int relation2 = notable.GetRelation(notable.SupporterOf.Leader);
			if (relation2 < 0 || MBRandom.RandomFloat < (50f - (float)relation2) / 500f)
			{
				bool flag = notable.SupporterOf == Clan.PlayerClan;
				notable.SupporterOf = null;
				if (flag)
				{
					TextObject textObject = new TextObject("{=aaOIjHeP}{NOTABLE.NAME} no longer supports your clan as your relationship deteriorated too much.", null);
					textObject.SetCharacterProperties("NOTABLE", notable.CharacterObject, false);
					InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f, 1f)));
				}
			}
		}

		// Token: 0x060042B1 RID: 17073 RVA: 0x001419F4 File Offset: 0x0013FBF4
		private void DailyTickHero(Hero hero)
		{
			if (hero.IsNotable && hero.CurrentSettlement != null)
			{
				if (MBRandom.RandomFloat < 0.01f)
				{
					this.UpdateNotableRelations(hero);
				}
				this.UpdateNotableSupport(hero);
				this.ManageCaravanExpensesOfNotable(hero);
				this.CheckAndMakeNotableDisappear(hero);
			}
		}

		// Token: 0x060042B2 RID: 17074 RVA: 0x00141A30 File Offset: 0x0013FC30
		private void CheckAndMakeNotableDisappear(Hero notable)
		{
			if (notable.OwnedWorkshops.IsEmpty<Workshop>() && notable.OwnedCaravans.IsEmpty<CaravanPartyComponent>() && notable.OwnedAlleys.IsEmpty<Alley>() && notable.CanDie(KillCharacterAction.KillCharacterActionDetail.Lost) && notable.CanHaveCampaignIssues() && notable.Power < (float)Campaign.Current.Models.NotablePowerModel.NotableDisappearPowerLimit)
			{
				float randomFloat = MBRandom.RandomFloat;
				float notableDisappearProbability = this.GetNotableDisappearProbability(notable);
				if (randomFloat < notableDisappearProbability)
				{
					KillCharacterAction.ApplyByRemove(notable, false, true);
					IssueBase issue = notable.Issue;
					if (issue == null)
					{
						return;
					}
					issue.CompleteIssueWithAiLord(notable.CurrentSettlement.OwnerClan.Leader);
				}
			}
		}

		// Token: 0x060042B3 RID: 17075 RVA: 0x00141AD0 File Offset: 0x0013FCD0
		private void ManageCaravanExpensesOfNotable(Hero notable)
		{
			for (int i = notable.OwnedCaravans.Count - 1; i >= 0; i--)
			{
				CaravanPartyComponent caravanPartyComponent = notable.OwnedCaravans[i];
				int totalWage = caravanPartyComponent.MobileParty.TotalWage;
				if (caravanPartyComponent.MobileParty.PartyTradeGold >= totalWage)
				{
					caravanPartyComponent.MobileParty.PartyTradeGold -= totalWage;
				}
				else
				{
					int num = MathF.Min(totalWage, notable.Gold);
					notable.Gold -= num;
				}
				if (caravanPartyComponent.MobileParty.PartyTradeGold < 5000)
				{
					int num2 = MathF.Min(5000 - caravanPartyComponent.MobileParty.PartyTradeGold, notable.Gold);
					caravanPartyComponent.MobileParty.PartyTradeGold += num2;
					notable.Gold -= num2;
				}
			}
		}

		// Token: 0x060042B4 RID: 17076 RVA: 0x00141BAA File Offset: 0x0013FDAA
		private float GetNotableDisappearProbability(Hero hero)
		{
			return ((float)Campaign.Current.Models.NotablePowerModel.NotableDisappearPowerLimit - hero.Power) / (float)Campaign.Current.Models.NotablePowerModel.NotableDisappearPowerLimit * 0.02f;
		}

		// Token: 0x060042B5 RID: 17077 RVA: 0x00141BE4 File Offset: 0x0013FDE4
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (victim.IsNotable)
			{
				if (victim.Power >= (float)Campaign.Current.Models.NotablePowerModel.NotableDisappearPowerLimit)
				{
					Hero hero = HeroCreator.CreateRelativeNotableHero(victim);
					if (victim.CurrentSettlement != null)
					{
						this.ChangeDeadNotable(victim, hero, victim.CurrentSettlement);
					}
					using (List<CaravanPartyComponent>.Enumerator enumerator = victim.OwnedCaravans.ToList<CaravanPartyComponent>().GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							CaravanPartyComponent caravanPartyComponent = enumerator.Current;
							CaravanPartyComponent.TransferCaravanOwnership(caravanPartyComponent.MobileParty, hero, hero.CurrentSettlement);
						}
						return;
					}
				}
				foreach (CaravanPartyComponent caravanPartyComponent2 in victim.OwnedCaravans.ToList<CaravanPartyComponent>())
				{
					DestroyPartyAction.Apply(null, caravanPartyComponent2.MobileParty);
				}
			}
		}

		// Token: 0x060042B6 RID: 17078 RVA: 0x00141CD8 File Offset: 0x0013FED8
		private void ChangeDeadNotable(Hero deadNotable, Hero newNotable, Settlement notableSettlement)
		{
			EnterSettlementAction.ApplyForCharacterOnly(newNotable, notableSettlement);
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (newNotable != hero)
				{
					int relation = deadNotable.GetRelation(hero);
					if (Math.Abs(relation) >= 20 || (relation != 0 && hero.CurrentSettlement == notableSettlement))
					{
						newNotable.SetPersonalRelation(hero, relation);
					}
				}
			}
			if (deadNotable.Issue != null)
			{
				Campaign.Current.IssueManager.ChangeIssueOwner(deadNotable.Issue, newNotable);
			}
		}

		// Token: 0x060042B7 RID: 17079 RVA: 0x00141D74 File Offset: 0x0013FF74
		private void SpawnNotablesAtGameStart()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsTown)
				{
					int targetNotableCountForSettlement = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Artisan);
					for (int i = 0; i < targetNotableCountForSettlement; i++)
					{
						HeroCreator.CreateNotable(Occupation.Artisan, settlement);
					}
					int targetNotableCountForSettlement2 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Merchant);
					for (int j = 0; j < targetNotableCountForSettlement2; j++)
					{
						HeroCreator.CreateNotable(Occupation.Merchant, settlement);
					}
					int targetNotableCountForSettlement3 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.GangLeader);
					for (int k = 0; k < targetNotableCountForSettlement3; k++)
					{
						HeroCreator.CreateNotable(Occupation.GangLeader, settlement);
					}
				}
				else if (settlement.IsVillage)
				{
					int targetNotableCountForSettlement4 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.RuralNotable);
					for (int l = 0; l < targetNotableCountForSettlement4; l++)
					{
						HeroCreator.CreateNotable(Occupation.RuralNotable, settlement);
					}
					int targetNotableCountForSettlement5 = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(settlement, Occupation.Headman);
					for (int m = 0; m < targetNotableCountForSettlement5; m++)
					{
						HeroCreator.CreateNotable(Occupation.Headman, settlement);
					}
				}
			}
		}

		// Token: 0x04001308 RID: 4872
		private const int CaravanGoldLowLimit = 5000;

		// Token: 0x04001309 RID: 4873
		private const int RemoveNotableCharacterAfterDays = 7;

		// Token: 0x0400130A RID: 4874
		private Dictionary<Settlement, int> _settlementPassedDaysForWeeklyTick;
	}
}
