using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003DD RID: 989
	public class ClanVariablesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003C4B RID: 15435 RVA: 0x0010165C File Offset: 0x000FF85C
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, new Action<Hero, Clan>(this.OnHeroChangedClan));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedEnd));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(this.WeeklyTickClan));
		}

		// Token: 0x06003C4C RID: 15436 RVA: 0x00101768 File Offset: 0x000FF968
		private void OnNewGameCreatedEnd(CampaignGameStarter starter)
		{
			foreach (Clan clan in Clan.All)
			{
				if (clan != Clan.PlayerClan)
				{
					this.UpdateClanSettlementsPaymentLimit(clan);
				}
			}
		}

		// Token: 0x06003C4D RID: 15437 RVA: 0x001017C4 File Offset: 0x000FF9C4
		private void WeeklyTickClan()
		{
			foreach (Clan clan in Clan.NonBanditFactions)
			{
				clan.ConsiderAndUpdateHomeSettlement();
			}
		}

		// Token: 0x06003C4E RID: 15438 RVA: 0x00101810 File Offset: 0x000FFA10
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003C4F RID: 15439 RVA: 0x00101814 File Offset: 0x000FFA14
		public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.IsFortification)
			{
				newOwner.Clan.ConsiderAndUpdateHomeSettlement();
				foreach (Hero hero in newOwner.Clan.Heroes)
				{
					hero.UpdateHomeSettlement();
				}
				oldOwner.Clan.ConsiderAndUpdateHomeSettlement();
				foreach (Hero hero2 in oldOwner.Clan.Heroes)
				{
					hero2.UpdateHomeSettlement();
				}
				settlement.SetGarrisonWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
				if (oldOwner.Clan.MapFaction.IsKingdomFaction)
				{
					foreach (Clan clan in oldOwner.Clan.Kingdom.Clans)
					{
						if (clan != oldOwner.Clan && clan != newOwner.Clan && clan.HomeSettlement == settlement)
						{
							clan.ConsiderAndUpdateHomeSettlement();
							foreach (Hero hero3 in clan.Heroes)
							{
								hero3.UpdateHomeSettlement();
							}
						}
					}
				}
			}
		}

		// Token: 0x06003C50 RID: 15440 RVA: 0x001019A8 File Offset: 0x000FFBA8
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			clan.ConsiderAndUpdateHomeSettlement();
			foreach (Settlement settlement in clan.Settlements)
			{
				foreach (Clan clan2 in Clan.All)
				{
					if (clan != clan2 && clan2.HomeSettlement == settlement)
					{
						clan2.ConsiderAndUpdateHomeSettlement();
					}
				}
			}
		}

		// Token: 0x06003C51 RID: 15441 RVA: 0x00101A48 File Offset: 0x000FFC48
		private void OnHeroChangedClan(Hero hero, Clan oldClan)
		{
			if (oldClan != null && oldClan.Leader == hero && hero.Clan != oldClan)
			{
				ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(oldClan);
			}
		}

		// Token: 0x06003C52 RID: 15442 RVA: 0x00101A68 File Offset: 0x000FFC68
		private void UpdateGovernorsOfClan(Clan clan)
		{
			List<Tuple<Town, float>> list = new List<Tuple<Town, float>>();
			foreach (Town town in clan.Fiefs)
			{
				float num = 0f;
				num += (float)(town.IsTown ? 3 : 1);
				num += MathF.Sqrt(town.Prosperity / 1000f);
				num += (float)town.Settlement.BoundVillages.Count;
				num *= ((clan.Culture == town.Settlement.Culture) ? 1f : 0.5f);
				float num2 = (clan.Leader.MapFaction.IsKingdomFaction ? Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, clan.Leader.MapFaction.FactionMidSettlement, false, false, MobileParty.NavigationType.All) : 100f);
				num *= 1f - MathF.Sqrt(num2 / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.Default));
				list.Add(new Tuple<Town, float>(town, num));
			}
			List<Hero> list2 = new List<Hero>();
			for (int i = 0; i < clan.Fiefs.Count; i++)
			{
				Tuple<Town, float> tuple = null;
				float num3 = 0f;
				foreach (Tuple<Town, float> tuple2 in list)
				{
					if (tuple2.Item2 > num3)
					{
						num3 = tuple2.Item2;
						tuple = tuple2;
					}
				}
				if (num3 > 0.01f)
				{
					list.Remove(tuple);
					float num4 = 0f;
					Hero hero = null;
					foreach (Hero hero2 in clan.AliveLords)
					{
						if (Campaign.Current.Models.ClanPoliticsModel.CanHeroBeGovernor(hero2) && hero2.PartyBelongedTo == null && hero2.Clan != Clan.PlayerClan && !list2.Contains(hero2))
						{
							float num5 = ((tuple.Item1.Governor == hero2) ? 1f : 0.75f) * Campaign.Current.Models.DiplomacyModel.GetHeroGoverningStrengthForClan(hero2);
							if (num5 > num4)
							{
								num4 = num5;
								hero = hero2;
							}
						}
					}
					if (hero != null)
					{
						if (tuple.Item1.Governor != hero)
						{
							if (hero.GovernorOf != null)
							{
								ChangeGovernorAction.RemoveGovernorOf(hero);
							}
							ChangeGovernorAction.Apply(tuple.Item1, hero);
						}
						list2.Add(hero);
					}
				}
			}
		}

		// Token: 0x06003C53 RID: 15443 RVA: 0x00101D6C File Offset: 0x000FFF6C
		public void OnNewGameCreated(CampaignGameStarter starter)
		{
			foreach (Kingdom kingdom in Kingdom.All)
			{
				kingdom.CalculateMidSettlement();
			}
			foreach (Clan clan in Clan.All)
			{
				clan.ConsiderAndUpdateHomeSettlement();
				if (clan != Clan.PlayerClan && clan.Leader != null && clan.Leader.MapFaction != null && clan.Leader.MapFaction.IsKingdomFaction && clan.Renown > 0f)
				{
					ChangeClanInfluenceAction.Apply(clan, (float)Campaign.Current.Models.ClanTierModel.CalculateInitialInfluence(clan));
				}
				clan.LastFactionChangeTime = CampaignTime.Now;
				clan.CalculateMidSettlement();
			}
			this.DetermineBasicTroopsForMinorFactions();
			foreach (Clan clan2 in Clan.NonBanditFactions)
			{
				this.UpdateGovernorsOfClan(clan2);
				if (clan2.Kingdom != null && clan2.Leader == clan2.Kingdom.Leader)
				{
					clan2.Kingdom.KingdomBudgetWallet = 2000000;
				}
			}
		}

		// Token: 0x06003C54 RID: 15444 RVA: 0x00101ED8 File Offset: 0x001000D8
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0", 0))
			{
				foreach (Clan clan in Clan.All)
				{
					if (clan != Clan.PlayerClan && !clan.IsBanditFaction && !clan.Leader.IsAlive)
					{
						if (!clan.IsEliminated)
						{
							ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(clan);
							if (!clan.Leader.IsAlive)
							{
								DestroyClanAction.Apply(clan);
							}
						}
						else if (clan.Settlements.Count > 0)
						{
							Clan clan2 = FactionHelper.ChooseHeirClanForFiefs(clan);
							foreach (Settlement settlement in clan.Settlements.ToList<Settlement>())
							{
								if (settlement.IsTown || settlement.IsCastle)
								{
									Hero randomElementWithPredicate = clan2.AliveLords.GetRandomElementWithPredicate((Hero x) => !x.IsChild);
									ChangeOwnerOfSettlementAction.ApplyByDestroyClan(settlement, randomElementWithPredicate);
								}
							}
						}
					}
				}
				using (List<Kingdom>.Enumerator enumerator3 = Kingdom.All.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						Kingdom kingdom = enumerator3.Current;
						if ((!kingdom.IsEliminated && kingdom.RulingClan.IsEliminated) || kingdom.Leader.MapFaction != kingdom)
						{
							IEnumerable<Clan> source = from t in kingdom.Clans
								where t != kingdom.RulingClan && !t.IsUnderMercenaryService && !t.IsEliminated
								select t;
							if (!source.IsEmpty<Clan>())
							{
								ChangeRulingClanAction.Apply(kingdom, source.FirstOrDefault<Clan>());
								if (source.Count<Clan>() > 1)
								{
									kingdom.AddDecision(new KingSelectionKingdomDecision(kingdom.RulingClan, kingdom.RulingClan), true);
								}
							}
							else
							{
								DestroyKingdomAction.Apply(kingdom);
							}
						}
					}
				}
			}
		}

		// Token: 0x06003C55 RID: 15445 RVA: 0x00102144 File Offset: 0x00100344
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0", 0))
			{
				for (int i = Clan.All.Count - 1; i >= 0; i--)
				{
					Clan clan = Clan.All[i];
					if (clan.StringId == "test_clan")
					{
						Campaign.Current.CampaignObjectManager.RemoveClan(clan);
						break;
					}
				}
				foreach (Clan clan2 in Clan.All)
				{
					if (clan2 != Clan.PlayerClan)
					{
						this.UpdateClanSettlementsPaymentLimit(clan2);
					}
				}
				foreach (MobileParty mobileParty in Campaign.Current.GarrisonParties.WhereQ((MobileParty p) => p.CurrentSettlement.OwnerClan == Clan.PlayerClan))
				{
					if (mobileParty.CurrentSettlement.GarrisonWagePaymentLimit == 0)
					{
						mobileParty.CurrentSettlement.SetGarrisonWagePaymentLimit(2000);
					}
				}
			}
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9", 0))
			{
				foreach (Clan clan3 in Clan.All)
				{
					if (clan3.Leader != null && clan3.Leader.Clan != clan3)
					{
						ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(clan3);
					}
				}
			}
		}

		// Token: 0x06003C56 RID: 15446 RVA: 0x00102304 File Offset: 0x00100504
		private void OnGameLoadFinished()
		{
			foreach (Kingdom kingdom in Kingdom.All)
			{
				kingdom.CalculateMidSettlement();
			}
			foreach (Clan clan in Clan.All)
			{
				clan.CalculateMidSettlement();
			}
			foreach (Kingdom kingdom2 in Kingdom.All)
			{
				for (int i = kingdom2.Clans.Count - 1; i >= 0; i--)
				{
					if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(kingdom2.Clans[i], kingdom2))
					{
						ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(kingdom2.Clans[i], false);
					}
				}
			}
		}

		// Token: 0x06003C57 RID: 15447 RVA: 0x00102418 File Offset: 0x00100618
		private void MakeClanFinancialEvaluation(Clan clan)
		{
			int num = (clan.IsMinorFaction ? 10000 : 30000);
			int num2 = (clan.IsMinorFaction ? 30000 : 90000);
			if (clan.Leader.Gold > num2)
			{
				using (List<WarPartyComponent>.Enumerator enumerator = clan.WarPartyComponents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						WarPartyComponent warPartyComponent = enumerator.Current;
						warPartyComponent.MobileParty.SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
					}
					return;
				}
			}
			if (clan.Leader.Gold > num)
			{
				using (List<WarPartyComponent>.Enumerator enumerator = clan.WarPartyComponents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						WarPartyComponent warPartyComponent2 = enumerator.Current;
						float num3 = 600f + (float)(clan.Leader.Gold - num) / (float)(num2 - num) * 600f;
						if (warPartyComponent2.MobileParty.LeaderHero == clan.Leader)
						{
							num3 *= 1.5f;
						}
						warPartyComponent2.MobileParty.SetWagePaymentLimit((int)num3);
					}
					return;
				}
			}
			foreach (WarPartyComponent warPartyComponent3 in clan.WarPartyComponents)
			{
				float num4 = 200f + (float)clan.Leader.Gold / (float)num * ((float)clan.Leader.Gold / (float)num) * 400f;
				if (warPartyComponent3.MobileParty.LeaderHero == clan.Leader)
				{
					num4 *= 1.5f;
				}
				warPartyComponent3.MobileParty.SetWagePaymentLimit((int)num4);
			}
		}

		// Token: 0x06003C58 RID: 15448 RVA: 0x001025E0 File Offset: 0x001007E0
		private void DailyTickClan(Clan clan)
		{
			if (!clan.IsBanditFaction)
			{
				if (clan.Kingdom != null)
				{
					if (clan != Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && clan.Kingdom.Leader != Hero.MainHero && MBRandom.RandomFloat < 0.1f)
					{
						clan.MercenaryAwardMultiplier = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, clan.Kingdom, false);
					}
					if (clan == Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && Campaign.CurrentTime > Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay)
					{
						clan.MercenaryAwardMultiplier = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, clan.Kingdom, false);
						Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay = Campaign.CurrentTime + 30f * (float)CampaignTime.HoursInDay;
					}
					if (clan != Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && clan.Kingdom.RulingClan.DebtToKingdom > 10000 && MBRandom.RandomFloat < 0.25f && clan.ShouldStayInKingdomUntil.IsPast)
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(clan, true);
					}
				}
				if (clan != Clan.PlayerClan)
				{
					this.MakeClanFinancialEvaluation(clan);
				}
				int num = MathF.Round(Campaign.Current.Models.ClanFinanceModel.CalculateClanGoldChange(clan, false, true, false).ResultNumber);
				GiveGoldAction.ApplyBetweenCharacters(null, clan.Leader, num, true);
				if (clan.MapFaction.Leader == clan.Leader && clan.Kingdom != null)
				{
					int num2 = ((clan.Kingdom.KingdomBudgetWallet < 2000000) ? 1000 : 0);
					if ((float)clan.Kingdom.KingdomBudgetWallet < 1000000f && MBRandom.RandomFloat < (((float)clan.Kingdom.KingdomBudgetWallet < 100000f) ? 0.01f : 0.005f))
					{
						float randomFloat = MBRandom.RandomFloat;
						num2 = ((randomFloat < 0.1f) ? 400000 : ((randomFloat < 0.3f) ? 200000 : 100000));
					}
					clan.Kingdom.KingdomBudgetWallet += num2;
				}
				float resultNumber = Campaign.Current.Models.ClanPoliticsModel.CalculateInfluenceChange(clan, false).ResultNumber;
				ChangeClanInfluenceAction.Apply(clan, resultNumber);
				if (clan == Clan.PlayerClan)
				{
					TextObject textObject = new TextObject("{=dPD5zood}Daily Gold Change: {CHANGE}{GOLD_ICON}", null);
					textObject.SetTextVariable("CHANGE", num);
					textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
					string soundEventPath = ((num > 0) ? "event:/ui/notification/coins_positive" : ((num == 0) ? string.Empty : "event:/ui/notification/coins_negative"));
					InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), soundEventPath));
				}
			}
			if (clan != Clan.PlayerClan)
			{
				this.UpdateGovernorsOfClan(clan);
				this.UpdateClanSettlementsPaymentLimit(clan);
				this.UpdateClanSettlementAutoRecruitment(clan);
			}
		}

		// Token: 0x06003C59 RID: 15449 RVA: 0x001028BC File Offset: 0x00100ABC
		private void UpdateClanSettlementAutoRecruitment(Clan clan)
		{
			if (clan.MapFaction != null && clan.MapFaction.IsKingdomFaction)
			{
				foreach (Settlement settlement in clan.Settlements)
				{
					if (settlement.IsFortification && settlement.Town.GarrisonParty != null && !settlement.Town.GarrisonAutoRecruitmentIsEnabled)
					{
						settlement.Town.GarrisonParty.CurrentSettlement.Town.GarrisonAutoRecruitmentIsEnabled = true;
					}
				}
			}
		}

		// Token: 0x06003C5A RID: 15450 RVA: 0x0010295C File Offset: 0x00100B5C
		private void UpdateClanSettlementsPaymentLimit(Clan clan)
		{
			float averageWage = Campaign.Current.AverageWage;
			if (clan.MapFaction != null && (clan.IsRebelClan || clan.MapFaction.IsKingdomFaction))
			{
				float num = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(clan.MapFaction as Kingdom, clan);
				foreach (Town town in clan.Fiefs)
				{
					float num2 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(town.OwnerClan);
					float num3 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(town);
					float num4 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(town.Settlement);
					float num5 = num * (((clan.IsRebelClan && !clan.MapFaction.IsKingdomFaction) ? 2f : 1.5f) * num2 * num3 * num4) * averageWage;
					num5 = MathF.Clamp(num5, 0f, (float)Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
					town.Settlement.SetGarrisonWagePaymentLimit((int)num5);
				}
			}
		}

		// Token: 0x06003C5B RID: 15451 RVA: 0x00102A70 File Offset: 0x00100C70
		private void DailyTickHero(Hero hero)
		{
			if (hero.IsActive && hero.IsNotable)
			{
				int num = Campaign.Current.Models.ClanFinanceModel.CalculateNotableDailyGoldChange(hero, true);
				if (num > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, hero, num, true);
				}
			}
		}

		// Token: 0x06003C5C RID: 15452 RVA: 0x00102AB4 File Offset: 0x00100CB4
		private void DetermineBasicTroopsForMinorFactions()
		{
			foreach (Clan clan in Clan.All)
			{
				if (clan.IsMinorFaction)
				{
					CharacterObject basicTroop = null;
					PartyTemplateObject defaultPartyTemplate = clan.DefaultPartyTemplate;
					int num = 50;
					foreach (PartyTemplateStack partyTemplateStack in defaultPartyTemplate.Stacks)
					{
						int level = partyTemplateStack.Character.Level;
						if (level < num)
						{
							num = level;
							basicTroop = partyTemplateStack.Character;
						}
					}
					clan.BasicTroop = basicTroop;
				}
			}
		}
	}
}
