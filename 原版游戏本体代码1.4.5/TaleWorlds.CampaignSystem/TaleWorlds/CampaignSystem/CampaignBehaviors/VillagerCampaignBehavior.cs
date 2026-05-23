using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VillagerCampaignBehavior : CampaignBehaviorBase
{
	public class VillagerCampaignBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public VillagerCampaignBehaviorTypeDefiner()
			: base(140000)
		{
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(PlayerInteraction), 1);
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(Dictionary<MobileParty, PlayerInteraction>));
		}
	}

	private enum PlayerInteraction
	{
		None,
		Friendly,
		TradedWith,
		Hostile
	}

	private float _collectFoodTotalWaitHours;

	private float _collectVolunteersTotalWaitHours;

	private float _collectFoodWaitHoursProgress;

	private float _collectVolunteerWaitHoursProgress;

	private Dictionary<MobileParty, CampaignTime> _lootedVillagers = new Dictionary<MobileParty, CampaignTime>();

	private Dictionary<MobileParty, PlayerInteraction> _interactedVillagers = new Dictionary<MobileParty, PlayerInteraction>();

	private Dictionary<Village, CampaignTime> _villageLastVillagerSendTime = new Dictionary<Village, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnLootDistributedToPartyEvent.AddNonSerializedListener(this, OnLootDistributedToParty);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		for (int i = 0; i < siegeEvent.BesiegedSettlement.Parties.Count; i++)
		{
			if (siegeEvent.BesiegedSettlement.Parties[i].IsVillager)
			{
				siegeEvent.BesiegedSettlement.Parties[i].SetMoveModeHold();
			}
		}
	}

	private void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
	{
		if (winnerParty.IsMobile && defeatedParty.IsMobile && defeatedParty.MobileParty.IsVillager)
		{
			SkillLevelingManager.OnLoot(winnerParty.MobileParty, defeatedParty.MobileParty, lootedItems, attacked: true);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_collectFoodWaitHoursProgress", ref _collectFoodWaitHoursProgress);
		dataStore.SyncData("_collectVolunteerWaitHoursProgress", ref _collectVolunteerWaitHoursProgress);
		dataStore.SyncData("_lootedVillagers", ref _lootedVillagers);
		dataStore.SyncData("_interactedVillagers", ref _interactedVillagers);
		dataStore.SyncData("_villageLastVillagerSendTime", ref _villageLastVillagerSendTime);
	}

	private void DeleteExpiredLootedVillagers()
	{
		List<MobileParty> list = new List<MobileParty>();
		foreach (KeyValuePair<MobileParty, CampaignTime> lootedVillager in _lootedVillagers)
		{
			if (CampaignTime.Now - lootedVillager.Value >= CampaignTime.Days(10f))
			{
				list.Add(lootedVillager.Key);
			}
		}
		foreach (MobileParty item in list)
		{
			_lootedVillagers.Remove(item);
		}
	}

	public void DailyTick()
	{
		DeleteExpiredLootedVillagers();
	}

	private void TickVillageThink(Settlement settlement)
	{
		Village village = settlement.Village;
		if (village != null && village.VillageState == Village.VillageStates.Normal && settlement.Party.MapEvent == null)
		{
			ThinkAboutSendingItemToTown(village);
		}
	}

	private void ThinkAboutSendingItemToTown(Village village)
	{
		if (!(MBRandom.RandomFloat < 0.15f))
		{
			return;
		}
		MobileParty mobileParty = village.VillagerPartyComponent?.MobileParty;
		if ((mobileParty != null && (mobileParty.CurrentSettlement != village.Owner.Settlement || mobileParty.MapEvent != null || mobileParty.IsInRaftState)) || village.Owner.MapEvent != null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < village.Owner.ItemRoster.Count; i++)
		{
			num += village.Owner.ItemRoster[i].Amount;
		}
		int warehouseCapacity = village.GetWarehouseCapacity();
		if (num < warehouseCapacity || village.Owner.MapEvent != null)
		{
			return;
		}
		if (mobileParty == null || (_villageLastVillagerSendTime.ContainsKey(village) && _villageLastVillagerSendTime[village].ElapsedDaysUntilNow > 7f && mobileParty.CurrentSettlement != village.Settlement))
		{
			if (village.Hearth > (float)Campaign.Current.Models.PartySizeLimitModel.MinimumNumberOfVillagersAtVillagerParty)
			{
				CreateVillagerParty(village);
			}
		}
		else
		{
			int idealVillagerPartySize = Campaign.Current.Models.PartySizeLimitModel.GetIdealVillagerPartySize(village);
			if (mobileParty.MemberRoster.TotalManCount < idealVillagerPartySize && mobileParty.HomeSettlement.Village.Hearth > 0f)
			{
				AddVillagersToParty(mobileParty, idealVillagerPartySize - mobileParty.MemberRoster.TotalManCount);
			}
		}
		if (mobileParty != null)
		{
			LoadAndSendVillagerParty(village, mobileParty);
		}
	}

	private void AddVillagersToParty(MobileParty villagerParty, int numberOfVillagersToAdd)
	{
		if (numberOfVillagersToAdd > (int)villagerParty.HomeSettlement.Village.Hearth)
		{
			numberOfVillagersToAdd = (int)villagerParty.HomeSettlement.Village.Hearth;
		}
		villagerParty.HomeSettlement.Village.Hearth -= (numberOfVillagersToAdd + 1) / 2;
		CharacterObject character = villagerParty.HomeSettlement.Culture.VillagerPartyTemplate.Stacks.GetRandomElement().Character;
		villagerParty.MemberRoster.AddToCounts(character, numberOfVillagersToAdd);
	}

	private void CreateVillagerParty(Village village)
	{
		MobileParty mobileParty = VillagerPartyComponent.CreateVillagerParty(village.Settlement.Culture.VillagerPartyTemplate.StringId + "_1", village);
		village.Hearth = MathF.Max(0f, village.Hearth - (float)((mobileParty.MemberRoster.TotalManCount + 1) / 2));
		EnterSettlementAction.ApplyForParty(mobileParty, village.Settlement);
	}

	private void LoadAndSendVillagerParty(Village village, MobileParty villagerParty)
	{
		if (!_villageLastVillagerSendTime.ContainsKey(village))
		{
			_villageLastVillagerSendTime.Add(village, CampaignTime.Now);
		}
		else
		{
			_villageLastVillagerSendTime[village] = CampaignTime.Now;
		}
		MoveItemsToVillagerParty(village, villagerParty);
		SendVillagerPartyToTradeBoundTown(villagerParty);
	}

	private static void MoveItemsToVillagerParty(Village village, MobileParty villagerParty)
	{
		ItemRoster itemRoster = village.Settlement.ItemRoster;
		float num = (float)villagerParty.InventoryCapacity - villagerParty.TotalWeightCarried;
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < itemRoster.Count; j++)
			{
				ItemRosterElement itemRosterElement = itemRoster[j];
				ItemObject item = itemRosterElement.EquipmentElement.Item;
				int num2 = MBRandom.RoundRandomized((float)itemRosterElement.Amount * 0.2f);
				if (num2 <= 0)
				{
					continue;
				}
				if (!item.HasHorseComponent && item.Weight * (float)num2 > num)
				{
					num2 = MathF.Ceiling(num / item.Weight);
					if (num2 <= 0)
					{
						continue;
					}
				}
				if (!item.HasHorseComponent)
				{
					num -= (float)num2 * item.Weight;
				}
				villagerParty.Party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, num2);
				itemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num2);
			}
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (_interactedVillagers.ContainsKey(mobileParty))
		{
			_interactedVillagers.Remove(mobileParty);
		}
	}

	private void HourlyTickSettlement(Settlement settlement)
	{
		DestroyVillagerPartyIfMemberCountIsZero(settlement);
		ThinkAboutSendingInsideVillagersToTheirHomeVillage(settlement);
		TickVillageThink(settlement);
	}

	private void DestroyVillagerPartyIfMemberCountIsZero(Settlement settlement)
	{
		Village village = settlement.Village;
		if (village != null && village.VillagerPartyComponent != null && village.VillagerPartyComponent.MobileParty.MapEvent == null && village.VillagerPartyComponent.MobileParty.MemberRoster.TotalHealthyCount == 0)
		{
			DestroyPartyAction.Apply(null, village.VillagerPartyComponent.MobileParty);
		}
	}

	private void HourlyTickParty(MobileParty villagerParty)
	{
		if (!villagerParty.IsVillager || villagerParty.MapEvent != null || !villagerParty.HasLandNavigationCapability)
		{
			return;
		}
		bool flag = false;
		if (villagerParty.HomeSettlement.Village.VillagerPartyComponent == null || villagerParty.HomeSettlement.Village.VillagerPartyComponent.MobileParty != villagerParty)
		{
			DestroyPartyAction.Apply(null, villagerParty);
		}
		else if (villagerParty.DefaultBehavior == AiBehavior.GoToSettlement)
		{
			if (villagerParty.TargetSettlement.IsTown && (villagerParty.TargetSettlement == null || villagerParty.TargetSettlement.IsUnderSiege || FactionManager.IsAtWarAgainstFaction(villagerParty.MapFaction, villagerParty.TargetSettlement.MapFaction)))
			{
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (villagerParty.DefaultBehavior == AiBehavior.GoToSettlement && villagerParty.TargetSettlement == villagerParty.HomeSettlement && villagerParty.HomeSettlement.IsUnderRaid && ((villagerParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty.CurrentSettlement, villagerParty.HomeSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) : Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty, villagerParty.HomeSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out var _)) < Campaign.Current.EstimatedAverageVillagerPartySpeed * 2.5f)
		{
			villagerParty.SetMoveModeHold();
			flag = false;
		}
		if (flag && (villagerParty.CurrentSettlement == null || !villagerParty.CurrentSettlement.IsUnderSiege))
		{
			if (villagerParty.ItemRoster.Count > 1)
			{
				SendVillagerPartyToTradeBoundTown(villagerParty);
			}
			else
			{
				TrySendingVillagerPartyToVillage(villagerParty);
			}
		}
	}

	private void TrySendingVillagerPartyToVillage(MobileParty villagerParty)
	{
		bool flag = true;
		if (villagerParty.HomeSettlement.IsUnderRaid && ((villagerParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty.CurrentSettlement, villagerParty.HomeSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) : Campaign.Current.Models.MapDistanceModel.GetDistance(villagerParty, villagerParty.HomeSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out var _)) < Campaign.Current.EstimatedAverageVillagerPartySpeed * 2.5f)
		{
			flag = false;
		}
		if (flag)
		{
			MoveVillagersToSettlementWithBestNavigationType(villagerParty, villagerParty.HomeSettlement);
		}
	}

	private void SendVillagerPartyToTradeBoundTown(MobileParty villagerParty)
	{
		Settlement tradeBound = villagerParty.HomeSettlement.Village.TradeBound;
		if (tradeBound != null && !tradeBound.IsUnderSiege)
		{
			MoveVillagersToSettlementWithBestNavigationType(villagerParty, tradeBound);
		}
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty != null && mobileParty.IsActive && mobileParty.IsVillager)
		{
			if (settlement.IsTown)
			{
				SellGoodsForTradeAction.ApplyByVillagerTrade(settlement, mobileParty);
			}
			if (settlement.IsVillage && mobileParty.PartyTradeGold != 0)
			{
				int num = Campaign.Current.Models.SettlementTaxModel.CalculateVillageTaxFromIncome(mobileParty.HomeSettlement.Village, mobileParty.PartyTradeGold);
				mobileParty.PartyTradeGold = 0;
				mobileParty.HomeSettlement.Village.TradeTaxAccumulated += num;
			}
			if (settlement.IsTown && settlement.Town.Governor != null && settlement.Town.Governor.GetPerkValue(DefaultPerks.Trade.TravelingRumors))
			{
				settlement.Town.TradeTaxAccumulated += MathF.Round(DefaultPerks.Trade.TravelingRumors.SecondaryBonus);
			}
		}
	}

	private void SetPlayerInteraction(MobileParty mobileParty, PlayerInteraction interaction)
	{
		if (_interactedVillagers.ContainsKey(mobileParty))
		{
			_interactedVillagers[mobileParty] = interaction;
		}
		else
		{
			_interactedVillagers.Add(mobileParty, interaction);
		}
	}

	private PlayerInteraction GetPlayerInteraction(MobileParty mobileParty)
	{
		if (_interactedVillagers.TryGetValue(mobileParty, out var value))
		{
			return value;
		}
		return PlayerInteraction.None;
	}

	private void ThinkAboutSendingInsideVillagersToTheirHomeVillage(Settlement settlement)
	{
		if ((!settlement.IsVillage && !settlement.IsTown) || settlement.IsUnderSiege || settlement.Party.MapEvent != null)
		{
			return;
		}
		for (int i = 0; i < settlement.Parties.Count; i++)
		{
			MobileParty mobileParty = settlement.Parties[i];
			if (mobileParty.IsActive && mobileParty.IsVillager && MBRandom.RandomFloat < 0.2f)
			{
				if (settlement.IsTown)
				{
					MoveVillagersToSettlementWithBestNavigationType(mobileParty, mobileParty.HomeSettlement);
				}
				else if (mobileParty.ItemRoster.Count > 1 && settlement != mobileParty.HomeSettlement)
				{
					SendVillagerPartyToTradeBoundTown(mobileParty);
				}
			}
		}
	}

	private void MoveVillagersToSettlementWithBestNavigationType(MobileParty villagerParty, Settlement settlement)
	{
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(villagerParty, settlement, isTargetingPort: false, out var bestNavigationType, out var _, out var isFromPort);
		SetPartyAiAction.GetActionForVisitingSettlement(villagerParty, settlement, bestNavigationType, isFromPort, isTargetingPort: false);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
		AddMenus(campaignGameStarter);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
	{
		AddVillageFarmerTradeAndLootDialogs(campaignGameSystemStarter);
	}

	private void AddMenus(CampaignGameStarter campaignGameSystemStarter)
	{
	}

	private void take_food_confirm_forget_it_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("village_hostile_action");
	}

	public bool taking_food_from_villagers_wait_on_condition(MenuCallbackArgs args)
	{
		int skillValue = MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Roguery).GetSkillValue(DefaultSkills.Roguery);
		_collectFoodTotalWaitHours = 12 - (int)((float)skillValue / 30f);
		args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(_collectFoodTotalWaitHours, _collectFoodWaitHoursProgress / _collectFoodTotalWaitHours);
		return true;
	}

	public bool press_into_service_confirm_on_condition(MenuCallbackArgs args)
	{
		int skillValue = MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Roguery).GetSkillValue(DefaultSkills.Roguery);
		_collectVolunteersTotalWaitHours = 24 - (int)((float)skillValue / 15f);
		args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(_collectVolunteersTotalWaitHours, _collectFoodWaitHoursProgress / _collectFoodTotalWaitHours);
		return true;
	}

	public void taking_food_from_villagers_wait_on_consequence(MenuCallbackArgs args)
	{
		_ = Settlement.CurrentSettlement.Village;
		GameMenu.ActivateGameMenu("menu_village_take_food_success");
		ChangeVillageStateAction.ApplyBySettingToNormal(MobileParty.MainParty.CurrentSettlement);
	}

	private void press_into_service_confirm_on_consequence(MenuCallbackArgs args)
	{
		_ = Settlement.CurrentSettlement.Village;
		GameMenu.ActivateGameMenu("menu_press_into_service_success");
		ChangeVillageStateAction.ApplyBySettingToNormal(MobileParty.MainParty.CurrentSettlement);
	}

	private void AddVillageFarmerTradeAndLootDialogs(CampaignGameStarter starter)
	{
		starter.AddDialogLine("village_farmer_talk_start", "start", "village_farmer_talk", "{=ddymPMWg}{VILLAGER_GREETING}", village_farmer_talk_start_on_condition, null);
		starter.AddDialogLine("village_farmer_pretalk_start", "village_farmer_pretalk", "village_farmer_talk", "{=cZjaGL9R}Is there anything else I can do it for you?", null, null);
		starter.AddPlayerLine("village_farmer_buy_products", "village_farmer_talk", "village_farmer_player_trade", "{=r46NWboa}I'm going to market too. What kind of products do you have?", village_farmer_buy_products_on_condition, null);
		starter.AddDialogLine("village_farmer_specify_products", "village_farmer_player_trade", "player_trade_decision", "{=BxazyNwY}We have {PRODUCTS}. We can let you have them for {TOTAL_PRICE}{GOLD_ICON}.", null, null);
		starter.AddPlayerLine("player_decided_to_buy", "player_trade_decision", "close_window", "{=HQ6hyVNH}All right. Here is {TOTAL_PRICE}{GOLD_ICON}.", null, conversation_player_decided_to_buy_on_consequence);
		starter.AddPlayerLine("player_decided_not_to_buy", "player_trade_decision", "village_farmer_pretalk", "{=D33fIGQe}Never mind.", null, null);
		starter.AddPlayerLine("village_farmer_loot", "village_farmer_talk", "village_farmer_loot_talk", "{=XaPMUJV0}Whatever you have, I'm taking it. Surrender or die!", village_farmer_loot_on_condition, null, 100, village_farmer_loot_on_clickable_condition);
		starter.AddDialogLine("village_farmer_fight", "village_farmer_loot_talk", "village_farmer_do_not_bribe", "{=ctEEfvsk}What? We're not warriors, but I bet we can take you. If you want our goods, you'll have to fight us![rf:idle_angry][ib:aggressive]", conversation_village_farmer_not_bribe_on_condition, null);
		starter.AddPlayerLine("village_farmer_leave", "village_farmer_talk", "close_window", "{=1IJouNaM}Carry on, then. Farewell.", null, conversation_village_farmer_leave_on_consequence);
		starter.AddPlayerLine("player_decided_to_fight_villagers", "village_farmer_do_not_bribe", "close_window", "{=1r0tDsrR}Attack!", null, conversation_village_farmer_fight_on_consequence);
		starter.AddPlayerLine("player_decided_to_not_fight_villagers_1", "village_farmer_do_not_bribe", "close_window", "{=D33fIGQe}Never mind.", null, conversation_village_farmer_leave_on_consequence);
		starter.AddDialogLine("village_farmer_accepted_to_give_some_goods", "village_farmer_loot_talk", "village_farmer_give_some_goods", "{=dMc3SjOK}We can pay you. {TAKE_MONEY_AND_PRODUCT_STRING}[rf:idle_angry][ib:nervous]", conversation_village_farmer_give_goods_on_condition, null);
		starter.AddPlayerLine("player_decided_to_take_some_goods_villagers", "village_farmer_give_some_goods", "village_farmer_end_talk", "{=VT1hSCaw}All right.", null, null);
		starter.AddPlayerLine("player_decided_to_take_everything_villagers", "village_farmer_give_some_goods", "player_wants_everything_villagers", "{=VpGjkNrF}I want everything.", null, null);
		starter.AddPlayerLine("player_decided_to_not_fight_villagers_2", "village_farmer_give_some_goods", "close_window", "{=D33fIGQe}Never mind.", null, conversation_village_farmer_leave_on_consequence);
		starter.AddDialogLine("village_farmer_fight_no_surrender", "player_wants_everything_villagers", "close_window", "{=wAhXFoNH}You'll have to fight us first![rf:idle_angry][ib:aggressive]", conversation_village_farmer_not_surrender_on_condition, conversation_village_farmer_fight_on_consequence);
		starter.AddDialogLine("village_farmer_accepted_to_give_everything", "player_wants_everything_villagers", "player_decision_to_take_prisoner_villagers", "{=33mKghKQ}Please don't kill us. We surrender.[rf:idle_angry][ib:nervous]", conversation_village_farmer_give_goods_on_condition, null);
		starter.AddPlayerLine("player_do_not_take_prisoner_villagers", "player_decision_to_take_prisoner_villagers", "village_farmer_end_talk_surrender", "{=6kaia5qP}Give me all your wares!", null, null, 100, delegate(out TextObject explanation)
		{
			if (!Hero.MainHero.MapFaction.IsAtWarWith(MobileParty.ConversationParty.MapFaction))
			{
				explanation = new TextObject("{=1LlH1Jof}This action will start a war.");
			}
			else
			{
				explanation = null;
			}
			return true;
		});
		starter.AddPlayerLine("player_decided_to_take_prisoner_2", "player_decision_to_take_prisoner_villagers", "villager_taken_prisoner_warning", "{=g5G8AJ5n}You are my prisoner now.", null, null, 100, delegate(out TextObject explanation)
		{
			if (!Hero.MainHero.MapFaction.IsAtWarWith(MobileParty.ConversationParty.MapFaction))
			{
				explanation = new TextObject("{=1LlH1Jof}This action will start a war.");
			}
			else
			{
				explanation = null;
			}
			return true;
		});
		starter.AddPlayerLine("player_decided_to_take_prisoner_2", "player_decision_to_take_prisoner_villagers", "villager_start_encounter", "{=ha53qb7v}Don't bother pleading for your lives. At them, lads!", null, null, 100, delegate(out TextObject explanation)
		{
			if (!Hero.MainHero.MapFaction.IsAtWarWith(MobileParty.ConversationParty.MapFaction))
			{
				explanation = new TextObject("{=1LlH1Jof}This action will start a war.");
			}
			else
			{
				explanation = null;
			}
			return true;
		});
		starter.AddDialogLine("villager_warn_player_to_take_prisoner", "villager_taken_prisoner_warning", "villager_taken_prisoner_warning_answer", "{=dPOOmYGQ}You think the lords and warriors of the {KINGDOM} won't just stand by idly when their people are kidnapped? You'd best let us go!", conversation_warn_player_on_condition, null);
		starter.AddDialogLine("villager_warn_player_to_take_prisoner_2", "villager_taken_prisoner_warning", "close_window", "{=BvytaDUJ}Heaven protect us from the likes of you.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += conversation_village_farmer_took_prisoner_on_consequence;
		});
		starter.AddPlayerLine("player_decided_to_take_prisoner_continue_2", "villager_taken_prisoner_warning_answer", "close_window", "{=Dfl5WJfN}Enough talking. Now march.", null, conversation_village_farmer_took_prisoner_on_consequence);
		starter.AddPlayerLine("player_decided_to_take_prisoner_leave_2", "villager_taken_prisoner_warning_answer", "village_farmer_loot_talk", "{=BNb88lyN}Never mind. Go on your way.", null, null);
		starter.AddDialogLine("village_farmer_bribery_leave", "village_farmer_end_talk", "close_window", "{=Pa1ZtapI}Okay. Okay then. We're going.", conversation_village_farmer_looted_leave_on_condition, conversation_village_farmer_looted_leave_on_consequence);
		starter.AddDialogLine("village_farmer_surrender_leave", "village_farmer_end_talk_surrender", "close_window", "{=Pa1ZtapI}Okay. Okay then. We're going.", conversation_village_farmer_looted_leave_on_condition, conversation_village_farmer_surrender_leave_on_consequence);
		starter.AddDialogLine("village_farmer_surrender_leave", "villager_start_encounter", "close_window", "{=yoWl6w1I}Heaven will avenge us, you butcher!", null, conversation_village_farmer_fight_forced_on_consequence);
	}

	private bool village_farmer_loot_on_clickable_condition(out TextObject explanation)
	{
		if (_lootedVillagers.ContainsKey(MobileParty.ConversationParty))
		{
			explanation = new TextObject("{=PVPBqy1e}You just looted these people.");
			return false;
		}
		CalculateConversationPartyBribeAmount(out var gold, out var items);
		bool num = gold > 0;
		bool flag = !items.IsEmpty();
		if (!num && !flag)
		{
			explanation = new TextObject("{=pbRwAjUN}They seem to have no valuable goods.");
			return false;
		}
		explanation = null;
		return true;
	}

	private bool village_farmer_talk_start_on_condition()
	{
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		if (PlayerEncounter.Current != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && encounteredParty.IsMobile && encounteredParty.MobileParty.IsVillager)
		{
			PlayerInteraction playerInteraction = GetPlayerInteraction(encounteredParty.MobileParty);
			switch (playerInteraction)
			{
			case PlayerInteraction.None:
			{
				MBTextManager.SetTextVariable("VILLAGE", encounteredParty.MobileParty.HomeSettlement.EncyclopediaLinkWithName);
				Settlement settlement = ((encounteredParty.MobileParty.HomeSettlement.Village.TradeBound != null) ? encounteredParty.MobileParty.HomeSettlement.Village.TradeBound : ((encounteredParty.MobileParty.LastVisitedSettlement == null || !encounteredParty.MobileParty.LastVisitedSettlement.IsTown) ? SettlementHelper.FindNearestTownToSettlement(encounteredParty.MobileParty.HomeSettlement, encounteredParty.MobileParty.NavigationCapability).Settlement : encounteredParty.MobileParty.LastVisitedSettlement));
				MBTextManager.SetTextVariable("TOWN", settlement.EncyclopediaLinkWithName);
				if (encounteredParty.MobileParty.DefaultBehavior == AiBehavior.GoToSettlement && encounteredParty.MobileParty.TargetSettlement.IsTown)
				{
					MBTextManager.SetTextVariable("VILLAGER_STATE", GameTexts.FindText("str_villager_goes_to_town"));
				}
				else
				{
					MBTextManager.SetTextVariable("VILLAGER_STATE", (encounteredParty.MobileParty.PartyTradeGold > 0) ? GameTexts.FindText("str_villager_returns_to_village") : GameTexts.FindText("str_looted_villager_returns_to_village"));
				}
				MBTextManager.SetTextVariable("VILLAGER_GREETING", "{=a7NrxcAD}Greetings, my {?PLAYER.GENDER}lady{?}lord{\\?}. {VILLAGER_PARTY_EXPLANATION}. {VILLAGER_STATE}".ToString());
				TextObject text = new TextObject("{=Epm86qnY}We're farmers from the village of {VILLAGE}");
				if (encounteredParty.MobileParty.HasNavalNavigationCapability)
				{
					text = new TextObject("{=b4fpZGsv}We're fisherman from the village of {VILLAGE}");
				}
				MBTextManager.SetTextVariable("VILLAGER_PARTY_EXPLANATION", text);
				break;
			}
			case PlayerInteraction.Hostile:
				MBTextManager.SetTextVariable("VILLAGER_GREETING", "{=L7AN6ybY}What do you want with us now?");
				break;
			case PlayerInteraction.Friendly:
				MBTextManager.SetTextVariable("VILLAGER_GREETING", "{=5Mu1cdbc}Greetings, once again. How may we help you?");
				break;
			}
			if (playerInteraction == PlayerInteraction.None)
			{
				SetPlayerInteraction(encounteredParty.MobileParty, PlayerInteraction.Friendly);
			}
			return true;
		}
		return false;
	}

	private bool village_farmer_loot_on_condition()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsVillager)
		{
			return MobileParty.ConversationParty.Party.MapFaction != Hero.MainHero.MapFaction;
		}
		return false;
	}

	private void conversation_village_farmer_leave_on_consequence()
	{
		PlayerEncounter.LeaveEncounter = true;
	}

	private bool village_farmer_buy_products_on_condition()
	{
		bool flag = true;
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		if (encounteredParty.MobileParty.IsVillager && !encounteredParty.ItemRoster.IsEmpty())
		{
			int num = 0;
			for (int i = 0; i < encounteredParty.ItemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = encounteredParty.ItemRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.EquipmentElement.Item.ItemCategory != DefaultItemCategories.PackAnimal)
				{
					int num2 = encounteredParty.MobileParty.HomeSettlement.Village.GetItemPrice(elementCopyAtIndex.EquipmentElement, MobileParty.MainParty, isSelling: true);
					int num3 = encounteredParty.MobileParty.HomeSettlement.Village.GetItemPrice(elementCopyAtIndex.EquipmentElement, MobileParty.MainParty, isSelling: true);
					if (MobileParty.MainParty.HasPerk(DefaultPerks.Trade.SilverTongue, checkSecondaryRole: true))
					{
						num2 = MathF.Ceiling((float)num2 * (1f - DefaultPerks.Trade.SilverTongue.SecondaryBonus));
						num3 = MathF.Ceiling((float)num3 * (1f - DefaultPerks.Trade.SilverTongue.SecondaryBonus));
					}
					int elementNumber = encounteredParty.ItemRoster.GetElementNumber(i);
					num += num3 * elementNumber;
					MBTextManager.SetTextVariable("NUMBER_OF", elementNumber);
					MBTextManager.SetTextVariable("ITEM", elementCopyAtIndex.EquipmentElement.Item.Name);
					MBTextManager.SetTextVariable("AMOUNT", num2);
					if (flag)
					{
						MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_number_of_item_and_price").ToString());
						flag = false;
					}
					else
					{
						MBTextManager.SetTextVariable("RIGHT", GameTexts.FindText("str_number_of_item_and_price").ToString());
						MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_LEFT_comma_RIGHT").ToString());
					}
				}
			}
			if (Hero.MainHero.Gold >= num && num > 0)
			{
				MBTextManager.SetTextVariable("PRODUCTS", GameTexts.FindText("str_LEFT_ONLY").ToString());
				MBTextManager.SetTextVariable("TOTAL_PRICE", num);
				return true;
			}
			return false;
		}
		return false;
	}

	private void conversation_player_decided_to_buy_on_consequence()
	{
		if (MobileParty.ConversationParty.IsVillager && MobileParty.ConversationParty.ItemRoster.Count > 0)
		{
			for (int num = MobileParty.ConversationParty.ItemRoster.Count - 1; num >= 0; num--)
			{
				ItemRosterElement elementCopyAtIndex = MobileParty.ConversationParty.ItemRoster.GetElementCopyAtIndex(num);
				if (elementCopyAtIndex.EquipmentElement.Item.ItemCategory != DefaultItemCategories.PackAnimal)
				{
					int itemPrice = MobileParty.ConversationParty.HomeSettlement.Village.GetItemPrice(elementCopyAtIndex.EquipmentElement, MobileParty.MainParty, isSelling: true);
					int elementNumber = MobileParty.ConversationParty.ItemRoster.GetElementNumber(num);
					int num2 = itemPrice * elementNumber;
					if (elementNumber > 0)
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2);
						MobileParty.ConversationParty.PartyTradeGold += num2;
						PartyBase.MainParty.ItemRoster.AddToCounts(MobileParty.ConversationParty.ItemRoster.GetElementCopyAtIndex(num).EquipmentElement, elementNumber);
						MobileParty.ConversationParty.ItemRoster.AddToCounts(MobileParty.ConversationParty.ItemRoster.GetElementCopyAtIndex(num).EquipmentElement, -1 * elementNumber);
					}
				}
			}
		}
		PlayerEncounter.LeaveEncounter = true;
	}

	private bool conversation_village_farmer_not_bribe_on_condition()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsVillager)
		{
			return !IsBribeFeasible();
		}
		return false;
	}

	private bool conversation_village_farmer_not_surrender_on_condition()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsVillager)
		{
			return !IsSurrenderFeasible();
		}
		return false;
	}

	private bool conversation_village_farmer_looted_leave_on_condition()
	{
		if (MobileParty.ConversationParty != null)
		{
			return MobileParty.ConversationParty.IsVillager;
		}
		return false;
	}

	private bool conversation_warn_player_on_condition()
	{
		IFaction mapFaction = MobileParty.ConversationParty.MapFaction;
		MBTextManager.SetTextVariable("KINGDOM", mapFaction.InformalName);
		return !MobileParty.MainParty.MapFaction.IsAtWarWith(MobileParty.ConversationParty.MapFaction);
	}

	private void conversation_village_farmer_took_prisoner_on_consequence()
	{
		ItemRoster itemRoster = new ItemRoster(PlayerEncounter.EncounteredParty.ItemRoster);
		if (itemRoster.Count > 0)
		{
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
			{
				PartyBase.MainParty,
				itemRoster
			} });
			PlayerEncounter.EncounteredParty.ItemRoster.Clear();
		}
		int partyTradeGold = PlayerEncounter.EncounteredParty.MobileParty.PartyTradeGold;
		if (partyTradeGold > 0)
		{
			GiveGoldAction.ApplyForPartyToCharacter(PlayerEncounter.EncounteredParty, Hero.MainHero, partyTradeGold);
		}
		BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, PlayerEncounter.EncounteredParty);
		SetPlayerInteraction(PlayerEncounter.EncounteredParty.MobileParty, PlayerInteraction.Hostile);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement item in PlayerEncounter.EncounteredParty.MemberRoster.GetTroopRoster())
		{
			troopRoster.AddToCounts(item.Character, item.Number);
		}
		PartyScreenHelper.OpenScreenAsLoot(TroopRoster.CreateDummyTroopRoster(), troopRoster, PlayerEncounter.EncounteredParty.Name, troopRoster.TotalManCount);
		SkillLevelingManager.OnLoot(MobileParty.MainParty, PlayerEncounter.EncounteredParty.MobileParty, itemRoster, attacked: false);
		DestroyPartyAction.Apply(MobileParty.MainParty.Party, PlayerEncounter.EncounteredParty.MobileParty);
		PlayerEncounter.LeaveEncounter = true;
	}

	private void conversation_village_farmer_fight_on_consequence()
	{
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Hostile);
	}

	private void conversation_village_farmer_fight_forced_on_consequence()
	{
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Hostile);
		BeHostileAction.ApplyEncounterHostileAction(MobileParty.MainParty.Party, MobileParty.ConversationParty.Party);
	}

	private bool conversation_village_farmer_give_goods_on_condition()
	{
		CalculateConversationPartyBribeAmount(out var gold, out var items);
		bool num = gold > 0;
		bool flag = !items.IsEmpty();
		if (num)
		{
			if (flag)
			{
				TextObject textObject = ((items.Count == 1) ? GameTexts.FindText("str_LEFT_RIGHT") : GameTexts.FindText("str_LEFT_comma_RIGHT"));
				TextObject textObject2 = GameTexts.FindText("str_looted_party_have_money");
				textObject2.SetTextVariable("MONEY", gold);
				textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				textObject2.SetTextVariable("ITEM_LIST", textObject);
				for (int i = 0; i < items.Count; i++)
				{
					ItemRosterElement elementCopyAtIndex = items.GetElementCopyAtIndex(i);
					TextObject textObject3 = GameTexts.FindText("str_offered_item_list");
					textObject3.SetTextVariable("COUNT", elementCopyAtIndex.Amount);
					textObject3.SetTextVariable("ITEM", elementCopyAtIndex.EquipmentElement.Item.Name);
					textObject.SetTextVariable("LEFT", textObject3);
					if (items.Count == 1)
					{
						textObject.SetTextVariable("RIGHT", TextObject.GetEmpty());
					}
					else if (items.Count - 2 > i)
					{
						TextObject textObject4 = GameTexts.FindText("str_LEFT_comma_RIGHT");
						textObject.SetTextVariable("RIGHT", textObject4);
						textObject = textObject4;
					}
					else
					{
						TextObject textObject5 = GameTexts.FindText("str_LEFT_ONLY");
						textObject.SetTextVariable("RIGHT", textObject5);
						textObject = textObject5;
					}
				}
				MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject2);
			}
			else
			{
				TextObject textObject6 = GameTexts.FindText("str_looted_party_have_money_but_no_item");
				textObject6.SetTextVariable("MONEY", gold);
				textObject6.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject6);
			}
		}
		else if (flag)
		{
			TextObject textObject7 = ((items.Count == 1) ? GameTexts.FindText("str_LEFT_RIGHT") : GameTexts.FindText("str_LEFT_comma_RIGHT"));
			TextObject textObject8 = GameTexts.FindText("str_looted_party_have_no_money");
			textObject8.SetTextVariable("ITEM_LIST", textObject7);
			for (int j = 0; j < items.Count; j++)
			{
				ItemRosterElement elementCopyAtIndex2 = items.GetElementCopyAtIndex(j);
				TextObject textObject9 = GameTexts.FindText("str_offered_item_list");
				textObject9.SetTextVariable("COUNT", elementCopyAtIndex2.Amount);
				textObject9.SetTextVariable("ITEM", elementCopyAtIndex2.EquipmentElement.Item.Name);
				textObject7.SetTextVariable("LEFT", textObject9);
				if (items.Count == 1)
				{
					textObject7.SetTextVariable("RIGHT", TextObject.GetEmpty());
				}
				else if (items.Count - 2 > j)
				{
					TextObject textObject10 = GameTexts.FindText("str_LEFT_comma_RIGHT");
					textObject7.SetTextVariable("RIGHT", textObject10);
					textObject7 = textObject10;
				}
				else
				{
					TextObject textObject11 = GameTexts.FindText("str_LEFT_ONLY");
					textObject7.SetTextVariable("RIGHT", textObject11);
					textObject7 = textObject11;
				}
			}
			MBTextManager.SetTextVariable("TAKE_MONEY_AND_PRODUCT_STRING", textObject8);
		}
		return true;
	}

	private void conversation_village_farmer_looted_leave_on_consequence()
	{
		CalculateConversationPartyBribeAmount(out var gold, out var items);
		GiveGoldAction.ApplyForPartyToCharacter(MobileParty.ConversationParty.Party, Hero.MainHero, gold);
		if (!items.IsEmpty())
		{
			for (int num = items.Count - 1; num >= 0; num--)
			{
				GiveItemAction.ApplyForParties(MobileParty.ConversationParty.Party, Hero.MainHero.PartyBelongedTo.Party, items[num]);
			}
		}
		BeHostileAction.ApplyMinorCoercionHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Hostile);
		_lootedVillagers.Add(MobileParty.ConversationParty, CampaignTime.Now);
		SkillLevelingManager.OnLoot(MobileParty.MainParty, MobileParty.ConversationParty, items, attacked: false);
		PlayerEncounter.LeaveEncounter = true;
	}

	private void conversation_village_farmer_surrender_leave_on_consequence()
	{
		ItemRoster itemRoster = new ItemRoster(MobileParty.ConversationParty.ItemRoster);
		if (itemRoster.Count > 0)
		{
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
			{
				PartyBase.MainParty,
				itemRoster
			} });
			MobileParty.ConversationParty.ItemRoster.Clear();
		}
		int partyTradeGold = MobileParty.ConversationParty.PartyTradeGold;
		if (partyTradeGold > 0)
		{
			GiveGoldAction.ApplyForPartyToCharacter(MobileParty.ConversationParty.Party, Hero.MainHero, partyTradeGold);
		}
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Hostile);
		BeHostileAction.ApplyMajorCoercionHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
		_lootedVillagers.Add(MobileParty.ConversationParty, CampaignTime.Now);
		SkillLevelingManager.OnLoot(MobileParty.MainParty, MobileParty.ConversationParty, itemRoster, attacked: false);
		PlayerEncounter.LeaveEncounter = true;
	}

	private bool IsBribeFeasible()
	{
		float resultNumber = Campaign.Current.Models.EncounterModel.GetBribeChance(MobileParty.ConversationParty, MobileParty.MainParty).ResultNumber;
		if (MobileParty.ConversationParty.Party.RandomFloatWithSeed(3u, 1f) > resultNumber)
		{
			return false;
		}
		return true;
	}

	private bool IsSurrenderFeasible()
	{
		float surrenderChance = Campaign.Current.Models.EncounterModel.GetSurrenderChance(MobileParty.ConversationParty, MobileParty.MainParty);
		if (MobileParty.ConversationParty.Party.RandomFloatWithSeed(4u, 1f) > surrenderChance)
		{
			return false;
		}
		return true;
	}

	private void CalculateConversationPartyBribeAmount(out int gold, out ItemRoster items)
	{
		int num = 0;
		ItemRoster itemRoster = new ItemRoster();
		bool flag = false;
		for (int i = 0; i < MobileParty.ConversationParty.ItemRoster.Count; i++)
		{
			num += MobileParty.ConversationParty.ItemRoster.GetElementUnitCost(i) * MobileParty.ConversationParty.ItemRoster.GetElementNumber(i);
			if (!flag && MobileParty.ConversationParty.ItemRoster.GetElementNumber(i) > 0)
			{
				flag = true;
			}
		}
		num += MobileParty.ConversationParty.PartyTradeGold;
		int num2 = MathF.Min((int)((float)num * 0.2f), 2000);
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Roguery.SaltTheEarth))
		{
			num2 = MathF.Round((float)num2 * (1f + DefaultPerks.Roguery.SaltTheEarth.PrimaryBonus));
		}
		int num3 = MathF.Min(MobileParty.ConversationParty.PartyTradeGold, num2);
		if (num3 < num2 && flag)
		{
			ItemRoster itemRoster2 = new ItemRoster(MobileParty.ConversationParty.ItemRoster);
			int num4 = 0;
			while (num3 + num4 < num2)
			{
				ItemRosterElement randomElement = itemRoster2.GetRandomElement();
				num4 += randomElement.EquipmentElement.ItemValue;
				EquipmentElement rosterElement = new EquipmentElement(randomElement.EquipmentElement.Item, randomElement.EquipmentElement.ItemModifier);
				itemRoster.AddToCounts(rosterElement, 1);
				itemRoster2.AddToCounts(rosterElement, -1);
				if (itemRoster2.IsEmpty())
				{
					break;
				}
			}
		}
		gold = num3;
		items = itemRoster;
	}
}
