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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class ClanVariablesCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedEnd);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickClan);
	}

	private void OnNewGameCreatedEnd(CampaignGameStarter starter)
	{
		foreach (Clan item in Clan.All)
		{
			if (item != Clan.PlayerClan)
			{
				UpdateClanSettlementsPaymentLimit(item);
			}
		}
	}

	private void WeeklyTickClan()
	{
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			nonBanditFaction.ConsiderAndUpdateHomeSettlement();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (!settlement.IsFortification)
		{
			return;
		}
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
		if (!oldOwner.Clan.MapFaction.IsKingdomFaction)
		{
			return;
		}
		foreach (Clan clan in oldOwner.Clan.Kingdom.Clans)
		{
			if (clan == oldOwner.Clan || clan == newOwner.Clan || clan.HomeSettlement != settlement)
			{
				continue;
			}
			clan.ConsiderAndUpdateHomeSettlement();
			foreach (Hero hero3 in clan.Heroes)
			{
				hero3.UpdateHomeSettlement();
			}
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		clan.ConsiderAndUpdateHomeSettlement();
		foreach (Settlement settlement in clan.Settlements)
		{
			foreach (Clan item in Clan.All)
			{
				if (clan != item && item.HomeSettlement == settlement)
				{
					item.ConsiderAndUpdateHomeSettlement();
				}
			}
		}
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (oldClan != null && oldClan.Leader == hero && hero.Clan != oldClan)
		{
			ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(oldClan);
		}
	}

	private void UpdateGovernorsOfClan(Clan clan)
	{
		List<Tuple<Town, float>> list = new List<Tuple<Town, float>>();
		foreach (Town fief in clan.Fiefs)
		{
			float num = 0f;
			num += (float)((!fief.IsTown) ? 1 : 3);
			num += TaleWorlds.Library.MathF.Sqrt(fief.Prosperity / 1000f);
			num += (float)fief.Settlement.BoundVillages.Count;
			num *= ((clan.Culture == fief.Settlement.Culture) ? 1f : 0.5f);
			float num2 = (clan.Leader.MapFaction.IsKingdomFaction ? Campaign.Current.Models.MapDistanceModel.GetDistance(fief.Settlement, clan.Leader.MapFaction.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All) : 100f);
			num *= 1f - TaleWorlds.Library.MathF.Sqrt(num2 / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.Default));
			list.Add(new Tuple<Town, float>(fief, num));
		}
		List<Hero> list2 = new List<Hero>();
		for (int i = 0; i < clan.Fiefs.Count; i++)
		{
			Tuple<Town, float> tuple = null;
			float num3 = 0f;
			foreach (Tuple<Town, float> item in list)
			{
				if (item.Item2 > num3)
				{
					num3 = item.Item2;
					tuple = item;
				}
			}
			if (!(num3 > 0.01f))
			{
				continue;
			}
			list.Remove(tuple);
			float num4 = 0f;
			Hero hero = null;
			foreach (Hero aliveLord in clan.AliveLords)
			{
				if (Campaign.Current.Models.ClanPoliticsModel.CanHeroBeGovernor(aliveLord) && aliveLord.PartyBelongedTo == null && aliveLord.Clan != Clan.PlayerClan && !list2.Contains(aliveLord))
				{
					float num5 = ((tuple.Item1.Governor == aliveLord) ? 1f : 0.75f) * Campaign.Current.Models.DiplomacyModel.GetHeroGoverningStrengthForClan(aliveLord);
					if (num5 > num4)
					{
						num4 = num5;
						hero = aliveLord;
					}
				}
			}
			if (hero == null)
			{
				continue;
			}
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

	public void OnNewGameCreated(CampaignGameStarter starter)
	{
		foreach (Kingdom item in Kingdom.All)
		{
			item.CalculateMidSettlement();
		}
		foreach (Clan item2 in Clan.All)
		{
			item2.ConsiderAndUpdateHomeSettlement();
			if (item2 != Clan.PlayerClan && item2.Leader != null && item2.Leader.MapFaction != null && item2.Leader.MapFaction.IsKingdomFaction && item2.Renown > 0f)
			{
				ChangeClanInfluenceAction.Apply(item2, Campaign.Current.Models.ClanTierModel.CalculateInitialInfluence(item2));
			}
			item2.LastFactionChangeTime = CampaignTime.Now;
			item2.CalculateMidSettlement();
		}
		DetermineBasicTroopsForMinorFactions();
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			UpdateGovernorsOfClan(nonBanditFaction);
			if (nonBanditFaction.Kingdom != null && nonBanditFaction.Leader == nonBanditFaction.Kingdom.Leader)
			{
				nonBanditFaction.Kingdom.KingdomBudgetWallet = 2000000;
			}
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.8.0")))
		{
			return;
		}
		foreach (Clan item in Clan.All)
		{
			if (item == Clan.PlayerClan || item.IsBanditFaction || item.Leader.IsAlive)
			{
				continue;
			}
			if (!item.IsEliminated)
			{
				ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(item);
				if (!item.Leader.IsAlive)
				{
					DestroyClanAction.Apply(item);
				}
			}
			else
			{
				if (item.Settlements.Count <= 0)
				{
					continue;
				}
				Clan clan = FactionHelper.ChooseHeirClanForFiefs(item);
				foreach (Settlement item2 in item.Settlements.ToList())
				{
					if (item2.IsTown || item2.IsCastle)
					{
						Hero randomElementWithPredicate = clan.AliveLords.GetRandomElementWithPredicate((Hero x) => !x.IsChild);
						ChangeOwnerOfSettlementAction.ApplyByDestroyClan(item2, randomElementWithPredicate);
					}
				}
			}
		}
		foreach (Kingdom kingdom in Kingdom.All)
		{
			if ((kingdom.IsEliminated || !kingdom.RulingClan.IsEliminated) && kingdom.Leader.MapFaction == kingdom)
			{
				continue;
			}
			IEnumerable<Clan> source = kingdom.Clans.Where((Clan t) => t != kingdom.RulingClan && !t.IsUnderMercenaryService && !t.IsEliminated);
			if (!source.IsEmpty())
			{
				ChangeRulingClanAction.Apply(kingdom, source.FirstOrDefault());
				if (source.Count() > 1)
				{
					kingdom.AddDecision(new KingSelectionKingdomDecision(kingdom.RulingClan, kingdom.RulingClan), ignoreInfluenceCost: true);
				}
			}
			else
			{
				DestroyKingdomAction.Apply(kingdom);
			}
		}
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0"))
		{
			for (int num = Clan.All.Count - 1; num >= 0; num--)
			{
				Clan clan = Clan.All[num];
				if (clan.StringId == "test_clan")
				{
					Campaign.Current.CampaignObjectManager.RemoveClan(clan);
					break;
				}
			}
			foreach (Clan item in Clan.All)
			{
				if (item != Clan.PlayerClan)
				{
					UpdateClanSettlementsPaymentLimit(item);
				}
			}
			foreach (MobileParty item2 in Campaign.Current.GarrisonParties.WhereQ((MobileParty p) => p.CurrentSettlement.OwnerClan == Clan.PlayerClan))
			{
				if (item2.CurrentSettlement.GarrisonWagePaymentLimit == 0)
				{
					item2.CurrentSettlement.SetGarrisonWagePaymentLimit(2000);
				}
			}
		}
		if (!MBSaveLoad.IsUpdatingGameVersion || !(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.9")))
		{
			return;
		}
		foreach (Clan item3 in Clan.All)
		{
			if (item3.Leader != null && item3.Leader.Clan != item3)
			{
				ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(item3);
			}
		}
	}

	private void OnGameLoadFinished()
	{
		foreach (Kingdom item in Kingdom.All)
		{
			item.CalculateMidSettlement();
		}
		foreach (Clan item2 in Clan.All)
		{
			item2.CalculateMidSettlement();
		}
		foreach (Kingdom item3 in Kingdom.All)
		{
			for (int num = item3.Clans.Count - 1; num >= 0; num--)
			{
				if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(item3.Clans[num], item3))
				{
					ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(item3.Clans[num], showNotification: false);
				}
			}
		}
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.14")))
		{
			return;
		}
		for (int num2 = Kingdom.All.Count - 1; num2 >= 0; num2--)
		{
			Kingdom kingdom = Kingdom.All[num2];
			if (!kingdom.IsEliminated)
			{
				foreach (Kingdom sameRulerKingdom in Kingdom.All.Where((Kingdom t) => t != kingdom && t.RulingClan == kingdom.RulingClan).ToList())
				{
					if (sameRulerKingdom.Clans.Count > 1)
					{
						Clan clan = (from t in sameRulerKingdom.Clans
							where t != sameRulerKingdom.RulingClan
							orderby t.CurrentTotalStrength descending
							select t).First();
						ChangeRulingClanAction.Apply(sameRulerKingdom, clan);
					}
					else
					{
						sameRulerKingdom.RulingClan = null;
						DestroyKingdomAction.Apply(sameRulerKingdom);
					}
				}
			}
		}
	}

	private void MakeClanFinancialEvaluation(Clan clan)
	{
		int num = (clan.IsMinorFaction ? 10000 : 30000);
		int num2 = (clan.IsMinorFaction ? 30000 : 90000);
		if (clan.Leader.Gold > num2)
		{
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				warPartyComponent.MobileParty.SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
			}
			return;
		}
		if (clan.Leader.Gold > num)
		{
			foreach (WarPartyComponent warPartyComponent2 in clan.WarPartyComponents)
			{
				float num3 = 600f + (float)(clan.Leader.Gold - num) / (float)(num2 - num) * 600f;
				if (warPartyComponent2.MobileParty.LeaderHero == clan.Leader)
				{
					num3 *= 1.5f;
				}
				warPartyComponent2.MobileParty.SetWagePaymentLimit((int)num3);
			}
			return;
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

	private void DailyTickClan(Clan clan)
	{
		if (!clan.IsBanditFaction)
		{
			if (clan.Kingdom != null)
			{
				if (clan != Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && clan.Kingdom.Leader != Hero.MainHero && MBRandom.RandomFloat < 0.1f)
				{
					clan.MercenaryAwardMultiplier = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, clan.Kingdom);
				}
				if (clan == Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && Campaign.CurrentTime > Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay)
				{
					clan.MercenaryAwardMultiplier = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, clan.Kingdom);
					Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay = Campaign.CurrentTime + 30f * (float)CampaignTime.HoursInDay;
				}
				if (clan != Clan.PlayerClan && clan.IsUnderMercenaryService && clan.Kingdom != null && clan.Kingdom.RulingClan.DebtToKingdom > 10000 && MBRandom.RandomFloat < 0.25f && clan.ShouldStayInKingdomUntil.IsPast)
				{
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(clan);
				}
			}
			if (clan != Clan.PlayerClan)
			{
				MakeClanFinancialEvaluation(clan);
			}
			int num = TaleWorlds.Library.MathF.Round(Campaign.Current.Models.ClanFinanceModel.CalculateClanGoldChange(clan, includeDescriptions: false, applyWithdrawals: true).ResultNumber);
			GiveGoldAction.ApplyBetweenCharacters(null, clan.Leader, num, disableNotification: true);
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
			float resultNumber = Campaign.Current.Models.ClanPoliticsModel.CalculateInfluenceChange(clan).ResultNumber;
			ChangeClanInfluenceAction.Apply(clan, resultNumber);
			if (clan == Clan.PlayerClan)
			{
				TextObject textObject = new TextObject("{=dPD5zood}Daily Gold Change: {CHANGE}{GOLD_ICON}");
				textObject.SetTextVariable("CHANGE", num);
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				InformationManager.DisplayMessage(new InformationMessage(soundEventPath: (num > 0) ? "event:/ui/notification/coins_positive" : ((num == 0) ? string.Empty : "event:/ui/notification/coins_negative"), information: textObject.ToString()));
			}
		}
		if (clan != Clan.PlayerClan)
		{
			UpdateGovernorsOfClan(clan);
			UpdateClanSettlementsPaymentLimit(clan);
			UpdateClanSettlementAutoRecruitment(clan);
		}
	}

	private void UpdateClanSettlementAutoRecruitment(Clan clan)
	{
		if (clan.MapFaction == null || !clan.MapFaction.IsKingdomFaction)
		{
			return;
		}
		foreach (Settlement settlement in clan.Settlements)
		{
			if (settlement.IsFortification && settlement.Town.GarrisonParty != null && !settlement.Town.GarrisonAutoRecruitmentIsEnabled)
			{
				settlement.Town.GarrisonParty.CurrentSettlement.Town.GarrisonAutoRecruitmentIsEnabled = true;
			}
		}
	}

	private void UpdateClanSettlementsPaymentLimit(Clan clan)
	{
		float averageWage = Campaign.Current.AverageWage;
		if (clan.MapFaction == null || (!clan.IsRebelClan && !clan.MapFaction.IsKingdomFaction))
		{
			return;
		}
		float num = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(clan.MapFaction as Kingdom, clan);
		foreach (Town fief in clan.Fiefs)
		{
			float num2 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(fief.OwnerClan);
			float num3 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(fief);
			float num4 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(fief.Settlement);
			float value = num * (((clan.IsRebelClan && !clan.MapFaction.IsKingdomFaction) ? 2f : 1.5f) * num2 * num3 * num4) * averageWage;
			value = TaleWorlds.Library.MathF.Clamp(value, 0f, Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
			fief.Settlement.SetGarrisonWagePaymentLimit((int)value);
		}
	}

	private void DailyTickHero(Hero hero)
	{
		if (hero.IsActive && hero.IsNotable)
		{
			int num = Campaign.Current.Models.ClanFinanceModel.CalculateNotableDailyGoldChange(hero, applyWithdrawals: true);
			if (num > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, hero, num, disableNotification: true);
			}
		}
	}

	private void DetermineBasicTroopsForMinorFactions()
	{
		foreach (Clan item in Clan.All)
		{
			if (!item.IsMinorFaction)
			{
				continue;
			}
			CharacterObject basicTroop = null;
			PartyTemplateObject defaultPartyTemplate = item.DefaultPartyTemplate;
			int num = 50;
			foreach (PartyTemplateStack stack in defaultPartyTemplate.Stacks)
			{
				int level = stack.Character.Level;
				if (level < num)
				{
					num = level;
					basicTroop = stack.Character;
				}
			}
			item.BasicTroop = basicTroop;
		}
	}
}
