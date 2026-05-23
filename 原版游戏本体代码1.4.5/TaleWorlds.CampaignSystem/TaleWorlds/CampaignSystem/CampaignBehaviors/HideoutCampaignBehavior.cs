using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class HideoutCampaignBehavior : CampaignBehaviorBase, IHideoutCampaignBehavior
{
	private const int HideoutClearRelationEffect = 2;

	private const int HideoutLootTargetValueMultiplier = 135;

	private int _minimumHideoutLootTargetValue = 350;

	private const int MaximumHideoutLootTargetValue = 4750;

	private const int MaximumHideoutExtraLootTypeCount = 5;

	private const float HideoutSendTroopsWaitTargetHour = 6f;

	private float _hideoutWaitProgressHours;

	private float _hideoutWaitTargetHours;

	private float _hideoutSendTroopsWaitProgressHour;

	private int _initialHideoutPopulation;

	private List<ItemObject> _potentialLootItems = new List<ItemObject>();

	private static float IncreaseRelationWithVillageNotableMaximumDistanceAsDays => 0.5f;

	private int CanAttackHideoutStart => Campaign.Current.Models.HideoutModel.CanAttackHideoutStartTime;

	private int CanAttackHideoutEnd => Campaign.Current.Models.HideoutModel.CanAttackHideoutEndTime;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, HourlyTickSettlement);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, OnHideoutSpotted);
		CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
		CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, OnCollectLootItems);
	}

	public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	public void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	public void HourlyTickSettlement(Settlement settlement)
	{
		if (settlement.IsHideout && settlement.Hideout.IsInfested && !settlement.Hideout.IsSpotted)
		{
			float hideoutSpottingDistance = Campaign.Current.Models.MapVisibilityModel.GetHideoutSpottingDistance();
			float num = MobileParty.MainParty.Position.DistanceSquared(settlement.Position);
			float num2 = 1f - num / (hideoutSpottingDistance * hideoutSpottingDistance);
			if (num2 > 0f && settlement.Parties.Count > 0 && MBRandom.RandomFloat < num2 && !settlement.Hideout.IsSpotted)
			{
				settlement.Hideout.IsSpotted = true;
				settlement.IsVisible = true;
				CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, settlement.Party);
			}
		}
	}

	private void OnHideoutSpotted(PartyBase party, PartyBase hideout)
	{
		SkillLevelingManager.OnHideoutSpotted(party.MobileParty, hideout);
	}

	private int GetItemValueForHideoutLoot(ItemObject itemToLoot)
	{
		return Campaign.Current.Models.TradeItemPriceFactorModel.GetTheoreticalMaxItemMarketValue(itemToLoot) + 1;
	}

	private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
		if (battleEndState != HideoutEventComponent.HideoutBattleEndState.Victory && battleEndState != HideoutEventComponent.HideoutBattleEndState.SendTroops)
		{
			_initialHideoutPopulation = 0;
		}
	}

	private void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
		if (winnerParty == PartyBase.MainParty && !PlayerEncounter.Current.ForceHideoutSendTroops)
		{
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			if (mapEvent.IsHideoutBattle && mapEvent.MapEventSettlement == Settlement.CurrentSettlement && IsItNighttimeNow())
			{
				int num = 0;
				foreach (MapEventParty party in mapEvent.GetMapEventSide(mapEvent.PlayerSide).Parties)
				{
					if (party.Party == PartyBase.MainParty)
					{
						num = party.PlunderedGold;
						break;
					}
				}
				int totalLootedValue = 0;
				float targetValue = num * (_initialHideoutPopulation * 135);
				targetValue = TaleWorlds.Library.MathF.Clamp(targetValue, _minimumHideoutLootTargetValue, 4750f);
				if ((float)totalLootedValue < targetValue)
				{
					ItemObject itemObject = null;
					for (int i = 0; i < _potentialLootItems.Count; i++)
					{
						if (gainedLoots.Count >= 5)
						{
							break;
						}
						if (!((float)totalLootedValue < targetValue))
						{
							break;
						}
						itemObject = _potentialLootItems[i];
						int itemValueForHideoutLoot = GetItemValueForHideoutLoot(itemObject);
						if ((float)itemValueForHideoutLoot <= targetValue - (float)totalLootedValue)
						{
							gainedLoots.AddToCounts(itemObject, 1);
							totalLootedValue += itemValueForHideoutLoot;
						}
					}
					do
					{
						itemObject = gainedLoots.GetRandomElementWithPredicate((ItemRosterElement x) => !x.EquipmentElement.Item.NotMerchandise && !x.EquipmentElement.IsQuestItem && !x.EquipmentElement.Item.IsBannerItem && (float)GetItemValueForHideoutLoot(x.EquipmentElement.Item) <= targetValue - (float)totalLootedValue).EquipmentElement.Item;
						if (itemObject != null)
						{
							gainedLoots.AddToCounts(itemObject, 1);
							totalLootedValue += GetItemValueForHideoutLoot(itemObject);
						}
					}
					while (itemObject != null);
				}
			}
		}
		_initialHideoutPopulation = 0;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_hideoutWaitProgressHours", ref _hideoutWaitProgressHours);
		dataStore.SyncData("_hideoutWaitTargetHours", ref _hideoutWaitTargetHours);
		dataStore.SyncData("_hideoutSendTroopsWaitProgressHour", ref _hideoutSendTroopsWaitProgressHour);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		foreach (ItemObject objectType in Campaign.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (objectType.IsTradeGood)
			{
				int itemValueForHideoutLoot = GetItemValueForHideoutLoot(objectType);
				if (itemValueForHideoutLoot >= _minimumHideoutLootTargetValue && itemValueForHideoutLoot <= 4750)
				{
					_potentialLootItems.Add(objectType);
				}
			}
		}
		_potentialLootItems = _potentialLootItems.OrderByDescending((ItemObject x) => x.Value).ToList();
		if (_potentialLootItems.Count > 0)
		{
			_minimumHideoutLootTargetValue = GetItemValueForHideoutLoot(_potentialLootItems[_potentialLootItems.Count - 1]);
		}
	}

	protected void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("hideout_place", "{=!}{HIDEOUT_TEXT}", game_menu_hideout_place_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_place", "attack", "{=p5GkeK8F}Sneak in now", game_menu_hideout_sneak_in_on_condition, game_menu_encounter_sneak_in_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_place", "assault", "{=b4YsZY5H}Assault hideout", game_menu_assault_hideout_parties_on_condition, game_menu_encounter_assault_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_place", "wait", "{=!}{WAIT_OPTION}", game_menu_wait_until_nightfall_on_condition, game_menu_wait_until_nightfall_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_place", "send_troops", "{=*}Send troops to clear ({SUCCESS_CHANCE}%)", game_menu_send_troops_hideout_on_condition, game_menu_encounter_send_troops_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_place", "leave", "{=3sRdGQou}Leave", leave_on_condition, game_menu_hideout_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddWaitGameMenu("hideout_wait", "{=!}{WAIT_TEXT}", hideout_wait_menu_on_init, hideout_wait_menu_on_condition, hideout_wait_menu_on_consequence, hideout_wait_menu_on_tick, GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameMenu.MenuOverlayType.None, _hideoutWaitTargetHours);
		campaignGameStarter.AddGameMenuOption("hideout_wait", "leave", "{=3sRdGQou}Leave", leave_on_condition, game_menu_hideout_after_wait_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("hideout_after_wait", "{=!}{HIDEOUT_TEXT}", hideout_after_wait_menu_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_after_wait", "attack", "{=Abcgrf4j}Sneak in", game_menu_hideout_sneak_in_on_condition, game_menu_encounter_sneak_in_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_after_wait", "assault", "{=b4YsZY5H}Assault hideout", game_menu_assault_hideout_parties_on_condition, game_menu_encounter_assault_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_after_wait", "send_troops", "{=*}Send troops to clear ({SUCCESS_CHANCE}%)", game_menu_send_troops_hideout_on_condition, game_menu_encounter_send_troops_on_consequence);
		campaignGameStarter.AddGameMenuOption("hideout_after_wait", "leave", "{=3sRdGQou}Leave", leave_on_condition, game_menu_hideout_after_wait_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("hideout_after_defeated_and_saved", "{=1zLZf5rw}The rest of your men rushed to your help, dragging you out to safety and driving the bandits back into hiding.", game_menu_hideout_after_defeated_and_saved_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_after_defeated_and_saved", "leave", "{=3sRdGQou}Leave", leave_on_condition, game_menu_hideout_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("hideout_after_found_by_sentries", "{=n0ynsBPx}Sentries detected you and alerted the rest of the bandits. Bandits moved back into hiding before you could round up your troops.", game_menu_hideout_after_defeated_and_saved_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_after_found_by_sentries", "leave", "{=3sRdGQou}Leave", leave_on_condition, game_menu_hideout_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddWaitGameMenu("hideout_send_troops_wait", "{=QOT7PSUp}Your troops are clearing the hideout.", hideout_send_troops_wait_menu_on_init, null, null, hideout_send_troops_wait_menu_tick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 6f);
		campaignGameStarter.AddGameMenuOption("hideout_send_troops_wait", "leave", "{=3sRdGQou}Leave", leave_on_condition, hideout_send_troops_wait_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("hideout_send_troops_result_success", "{=nfgQU4Uk}Your men surprised the bandits, and cut down several before the rest fled in disarray. You don't think they'll be back.", hideout_send_troops_result_success_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_send_troops_result_success", "leave", "{=3sRdGQou}Leave", leave_on_condition, hideout_send_troops_result_success_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("hideout_send_troops_result_failure", "{=b4HypxQ5}Your men failed their approach... The bandits' sentries spotted them before they attacked, and the bandits withdrew in good order, probably to some nearby hiding places. Until they are dealt with properly, their presence will continue to threaten the region. You expect it won't be long before they return to their lair.\r\n", hideout_send_troops_result_failure_on_init);
		campaignGameStarter.AddGameMenuOption("hideout_send_troops_result_failure", "leave", "{=3sRdGQou}Leave", leave_on_condition, hideout_send_troops_result_failure_consequence, isLeave: true);
	}

	private void hideout_send_troops_result_success_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		PlayerEncounter.Finish();
		SetCleanHideoutRelations(currentSettlement);
	}

	private void hideout_send_troops_result_success_on_init(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Battle.WinningSide == BattleSideEnum.None)
		{
			PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.PlayerSide);
			CampaignEventDispatcher.Instance.OnHideoutBattleCompleted(PlayerEncounter.Battle.PlayerSide, (HideoutEventComponent)PlayerEncounter.Battle.Component, HideoutEventComponent.HideoutBattleEndState.SendTroops);
			PlayerEncounter.Update();
			PlayerEncounter.EncounterSettlement?.Party.SetVisualAsDirty();
		}
	}

	private void hideout_send_troops_result_failure_on_init(MenuCallbackArgs args)
	{
		Settlement.CurrentSettlement.Hideout.SetNextPossibleAttackTime(Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);
	}

	private void hideout_send_troops_result_failure_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Finish();
	}

	public int GetInitialHideoutPopulation()
	{
		return _initialHideoutPopulation;
	}

	private void hideout_wait_menu_on_init(MenuCallbackArgs args)
	{
		UpdateHideoutWaitProgress(args);
	}

	private bool IsItNighttimeNow()
	{
		float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
		if (CanAttackHideoutStart <= CanAttackHideoutEnd || (!(currentHourInDay >= (float)CanAttackHideoutStart) && !(currentHourInDay <= (float)CanAttackHideoutEnd)))
		{
			if (CanAttackHideoutStart < CanAttackHideoutEnd)
			{
				if (currentHourInDay >= (float)CanAttackHideoutStart)
				{
					return currentHourInDay <= (float)CanAttackHideoutEnd;
				}
				return false;
			}
			return false;
		}
		return true;
	}

	public bool hideout_wait_menu_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	public void hideout_wait_menu_on_tick(MenuCallbackArgs args, CampaignTime campaignTime)
	{
		_hideoutWaitProgressHours += (float)campaignTime.ToHours;
		UpdateHideoutWaitProgress(args);
	}

	private void UpdateHideoutWaitProgress(MenuCallbackArgs args)
	{
		TextObject empty = TextObject.GetEmpty();
		empty = (IsItNighttimeNow() ? new TextObject("{=GrrWYNIZ}Waiting until morning to begin the assault.") : new TextObject("{=VLLAOXve}Waiting until nightfall to sneak in."));
		MBTextManager.SetTextVariable("WAIT_TEXT", empty);
		if (_hideoutWaitTargetHours.ApproximatelyEqualsTo(0f))
		{
			CalculateHideoutAttackTime();
		}
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(_hideoutWaitProgressHours / _hideoutWaitTargetHours);
	}

	public void hideout_wait_menu_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("hideout_after_wait");
	}

	private bool leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	[GameMenuInitializationHandler("hideout_wait")]
	[GameMenuInitializationHandler("hideout_after_wait")]
	[GameMenuInitializationHandler("hideout_after_defeated_and_saved")]
	private static void game_menu_hideout_ui_place_on_init(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		args.MenuContext.SetBackgroundMeshName(currentSettlement.Hideout.WaitMeshName);
	}

	[GameMenuInitializationHandler("hideout_place")]
	private static void game_menu_hideout_sound_place_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetPanelSound("event:/ui/panels/settlement_hideout");
		Settlement currentSettlement = Settlement.CurrentSettlement;
		args.MenuContext.SetBackgroundMeshName(currentSettlement.Hideout.WaitMeshName);
	}

	private void game_menu_hideout_after_defeated_and_saved_on_init(MenuCallbackArgs args)
	{
		if (Settlement.CurrentSettlement.IsHideout && MobileParty.MainParty.CurrentSettlement == null)
		{
			PlayerEncounter.EnterSettlement();
		}
	}

	private void game_menu_hideout_place_on_init(MenuCallbackArgs args)
	{
		if (!Settlement.CurrentSettlement.IsHideout)
		{
			return;
		}
		_hideoutWaitProgressHours = 0f;
		_hideoutSendTroopsWaitProgressHour = 0f;
		if (!IsItNighttimeNow())
		{
			CalculateHideoutAttackTime();
		}
		else
		{
			_hideoutWaitTargetHours = 0f;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		int num = 0;
		foreach (MobileParty party in currentSettlement.Parties)
		{
			num += party.MemberRoster.TotalManCount - party.MemberRoster.TotalWounded;
		}
		GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=DOmb81Mu}(Undefined hideout type)");
		if (currentSettlement.Culture.StringId.Equals("forest_bandits"))
		{
			GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=cu2cLT5r}You spy though the trees what seems to be a clearing in the forest with what appears to be the outlines of a camp.");
		}
		if (currentSettlement.Culture.StringId.Equals("sea_raiders"))
		{
			GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=bJ6ygV3P}As you travel along the coast, you see a sheltered cove with what appears to the outlines of a camp.");
		}
		if (currentSettlement.Culture.StringId.Equals("mountain_bandits"))
		{
			GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=iyWUDSm8}Passing by the slopes of the mountains, you see an outcrop crowned with the ruins of an ancient fortress.");
		}
		if (currentSettlement.Culture.StringId.Equals("desert_bandits"))
		{
			GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=b3iBOVXN}Passing by a wadi, you see what looks like a camouflaged well to tap the groundwater left behind by rare rainfalls.");
		}
		if (currentSettlement.Culture.StringId.Equals("steppe_bandits"))
		{
			GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "{=5JaGVr0U}While traveling by a low range of hills, you see what appears to be the remains of a campsite in a stream gully.");
		}
		bool num2 = !currentSettlement.Hideout.NextPossibleAttackTime.IsPast;
		if (num2)
		{
			GameTexts.SetVariable("HIDEOUT_TEXT", "{=KLWn6yZQ}{HIDEOUT_DESCRIPTION} The remains of a fire suggest that it's been recently occupied, but its residents - whoever they are - are well-hidden for now. Until they are dealt with properly, their presence will continue to threaten the region.");
		}
		else if (num > 0)
		{
			GameTexts.SetVariable("HIDEOUT_TEXT", "{=prcBBqMR}{HIDEOUT_DESCRIPTION} You see armed men moving about. As you listen quietly, you hear scraps of conversation about raids, ransoms, and the best places to waylay travellers.");
		}
		else
		{
			GameTexts.SetVariable("HIDEOUT_TEXT", "{=gywyEgZa}{HIDEOUT_DESCRIPTION} There seems to be no one inside.");
		}
		if (!num2 && num > 0 && Hero.MainHero.IsWounded)
		{
			GameTexts.SetVariable("HIDEOUT_TEXT", "{=fMekM2UH}{HIDEOUT_DESCRIPTION} You can not attack since your wounds do not allow you.");
		}
		if (MobileParty.MainParty.CurrentSettlement == null)
		{
			PlayerEncounter.EnterSettlement();
		}
		_ = Settlement.CurrentSettlement.Hideout.IsInfested;
		Settlement settlement = (Settlement.CurrentSettlement.IsHideout ? Settlement.CurrentSettlement : null);
		if (PlayerEncounter.Battle != null)
		{
			bool num3 = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Current.PlayerSide;
			PlayerEncounter.Update();
			if (num3 && PlayerEncounter.Battle == null && settlement != null)
			{
				SetCleanHideoutRelations(settlement);
				settlement = null;
			}
		}
	}

	private void CalculateHideoutAttackTime()
	{
		float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
		if (IsItNighttimeNow())
		{
			_hideoutWaitTargetHours = (((float)CanAttackHideoutEnd > currentHourInDay) ? ((float)CanAttackHideoutEnd - currentHourInDay) : ((float)CampaignTime.HoursInDay - currentHourInDay + (float)CanAttackHideoutEnd));
		}
		else
		{
			_hideoutWaitTargetHours = (((float)CanAttackHideoutStart > currentHourInDay) ? ((float)CanAttackHideoutStart - currentHourInDay) : ((float)CampaignTime.HoursInDay - currentHourInDay + (float)CanAttackHideoutStart));
		}
	}

	private void SetCleanHideoutRelations(Settlement hideout)
	{
		List<Settlement> list = new List<Settlement>();
		float num = IncreaseRelationWithVillageNotableMaximumDistanceAsDays * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
		foreach (Village allVillage in Campaign.Current.AllVillages)
		{
			if (allVillage.Settlement.Position.DistanceSquared(hideout.Position) <= num * num)
			{
				list.Add(allVillage.Settlement);
			}
		}
		foreach (Settlement item in list)
		{
			if (item.Notables.Count > 0)
			{
				ChangeRelationAction.ApplyPlayerRelation(item.Notables.GetRandomElement(), 2, affectRelatives: true, showQuickNotification: false);
			}
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
		{
			Town town = SettlementHelper.FindNearestTownToSettlement(hideout, MobileParty.NavigationType.All);
			Hero leader = town.OwnerClan.Leader;
			if (leader == Hero.MainHero)
			{
				town.Loyalty += 1f;
			}
			else
			{
				ChangeRelationAction.ApplyPlayerRelation(leader, (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
			}
		}
		MBTextManager.SetTextVariable("RELATION_VALUE", (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
		MBInformationManager.AddQuickInformation(new TextObject("{=o0qwDa0q}Your relation increased by {RELATION_VALUE} with nearby notables."));
	}

	private void hideout_after_wait_menu_on_init(MenuCallbackArgs args)
	{
		TextObject empty = TextObject.GetEmpty();
		empty = ((!IsItNighttimeNow()) ? new TextObject("{=7ovEa1gB}After waiting for a while you find a good opportunity to assault the camp.") : new TextObject("{=VbU8Ue0O}After waiting for a while you find a good opportunity to close in undetected beneath the shroud of the night."));
		MBTextManager.SetTextVariable("HIDEOUT_TEXT", empty);
	}

	private bool game_menu_hideout_sneak_in_on_condition(MenuCallbackArgs args)
	{
		Hideout hideout = Settlement.CurrentSettlement.Hideout;
		int num;
		if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction && Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
		{
			num = (hideout.NextPossibleAttackTime.IsPast ? 1 : 0);
			if (num != 0)
			{
				if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=pM9GOxrV}You are wounded, you can't sneak in!");
				}
				else
				{
					int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: false);
					if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
					{
						args.IsEnabled = false;
						args.Tooltip = new TextObject("{=XasRXCod}You should have more than {AMOUNT} healthy troops in your party to attack!");
						args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
					}
					else if (!IsItNighttimeNow())
					{
						args.Tooltip = new TextObject("{=beDxk5YB}Bandits are awake and too aware to be ambushed.");
						args.IsEnabled = false;
					}
				}
			}
		}
		else
		{
			num = 0;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.SneakIn;
		return (byte)num != 0;
	}

	private bool game_menu_assault_hideout_parties_on_condition(MenuCallbackArgs args)
	{
		Hideout hideout = Settlement.CurrentSettlement.Hideout;
		int num;
		if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction && Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
		{
			num = (hideout.NextPossibleAttackTime.IsPast ? 1 : 0);
			if (num != 0)
			{
				if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=ZCKKVRjT}You are wounded, you can't assault the hideout!");
				}
				else
				{
					int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: true);
					if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
					{
						args.IsEnabled = false;
						args.Tooltip = new TextObject("{=XasRXCod}You should have more than {AMOUNT} healthy troops in your party to attack!");
						args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
					}
					else if (IsItNighttimeNow())
					{
						args.Tooltip = new TextObject("{=7IXfD2qq}A frontal assault against the hideout is too dangerous.");
						args.IsEnabled = false;
					}
				}
				args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void game_menu_encounter_sneak_in_on_consequence(MenuCallbackArgs args)
	{
		game_menu_encounter_attack_on_consequence(args, delegate(TroopRoster x)
		{
			OnTroopRosterManageDone(x, isDirectAssault: false);
		}, isDirectAssault: false);
	}

	private void game_menu_encounter_assault_on_consequence(MenuCallbackArgs args)
	{
		game_menu_encounter_attack_on_consequence(args, delegate(TroopRoster x)
		{
			OnTroopRosterManageDone(x, isDirectAssault: true);
		}, isDirectAssault: true);
	}

	private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args, Action<TroopRoster> onDone, bool isDirectAssault)
	{
		BanditDensityModel banditDensityModel = Campaign.Current.Models.BanditDensityModel;
		int maximumTroopCountForHideoutMission = banditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault);
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, maximumTroopCountForHideoutMission, includePlayer: true);
		troopRoster.Add(strongestAndPriorTroops);
		int maximumTroopCountForHideoutMission2 = banditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault);
		args.MenuContext.OpenTroopSelection(MobileParty.MainParty.MemberRoster, troopRoster, null, CanChangeStatusOfTroop, onDone, maximumTroopCountForHideoutMission2, banditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault));
	}

	private bool game_menu_send_troops_hideout_on_condition(MenuCallbackArgs args)
	{
		Hideout hideout = Settlement.CurrentSettlement.Hideout;
		args.Tooltip = new TextObject("{=Xum0Iddf}You will gain no loot or experience.");
		int num;
		if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction && Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
		{
			num = (hideout.NextPossibleAttackTime.IsPast ? 1 : 0);
			if (num != 0)
			{
				int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: false);
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=yUbdUFSC}You should have more than {AMOUNT} healthy troops in your party to send your troops!");
					args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
				}
				args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
				float sendTroopsSuccessChance = Campaign.Current.Models.HideoutModel.GetSendTroopsSuccessChance(hideout);
				MBTextManager.SetTextVariable("SUCCESS_CHANCE", TaleWorlds.Library.MathF.Round(sendTroopsSuccessChance * 100f));
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void game_menu_encounter_send_troops_on_consequence(MenuCallbackArgs args)
	{
		UpdateInitialHideoutPopulation();
		PlayerEncounter.Current.ForceHideoutSendTroops = true;
		GameMenu.SwitchToMenu("encounter");
	}

	private void ArrangeHideoutTroopCountsForMission()
	{
		int numberOfMinimumBanditTroopsInHideoutMission = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditTroopsInHideoutMission;
		int num = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForFirstFightInHideout + Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForBossFightInHideout;
		MBList<MobileParty> mBList = Settlement.CurrentSettlement.Parties.Where((MobileParty x) => x.IsBandit || x.IsBanditBossParty).ToMBList();
		int num2 = mBList.Sum((MobileParty x) => x.MemberRoster.TotalHealthyCount);
		if (num2 > num)
		{
			int num3 = num2 - num;
			mBList.RemoveAll((MobileParty x) => x.IsBanditBossParty || x.MemberRoster.TotalHealthyCount == 1);
			while (num3 > 0 && mBList.Count > 0)
			{
				MobileParty randomElement = mBList.GetRandomElement();
				MBList<TroopRosterElement> troopRoster = randomElement.MemberRoster.GetTroopRoster();
				List<(TroopRosterElement, float)> list = new List<(TroopRosterElement, float)>();
				foreach (TroopRosterElement item in troopRoster)
				{
					list.Add((item, item.Number - item.WoundedNumber));
				}
				TroopRosterElement troopRosterElement = MBRandom.ChooseWeighted(list);
				randomElement.MemberRoster.AddToCounts(troopRosterElement.Character, -1);
				num3--;
				if (randomElement.MemberRoster.TotalHealthyCount == 1)
				{
					mBList.Remove(randomElement);
				}
			}
		}
		else
		{
			if (num2 >= numberOfMinimumBanditTroopsInHideoutMission)
			{
				return;
			}
			int num4 = numberOfMinimumBanditTroopsInHideoutMission - num2;
			mBList.RemoveAll((MobileParty x) => x.MemberRoster.GetTroopRoster().All((TroopRosterElement y) => y.Number == 0 || y.Character.Culture.BanditBoss == y.Character || y.Character.IsHero));
			while (num4 > 0 && mBList.Count > 0)
			{
				MobileParty randomElement2 = mBList.GetRandomElement();
				MBList<TroopRosterElement> troopRoster2 = randomElement2.MemberRoster.GetTroopRoster();
				List<(TroopRosterElement, float)> list2 = new List<(TroopRosterElement, float)>();
				foreach (TroopRosterElement item2 in troopRoster2)
				{
					list2.Add((item2, item2.Number * ((item2.Character.Culture.BanditBoss != item2.Character && !item2.Character.IsHero) ? 1 : 0)));
				}
				TroopRosterElement troopRosterElement2 = MBRandom.ChooseWeighted(list2);
				randomElement2.MemberRoster.AddToCounts(troopRosterElement2.Character, 1);
				num4--;
			}
		}
	}

	private void OnTroopRosterManageDone(TroopRoster hideoutTroops, bool isDirectAssault)
	{
		ArrangeHideoutTroopCountsForMission();
		GameMenu.SwitchToMenu("hideout_place");
		Settlement.CurrentSettlement.Hideout.SetNextPossibleAttackTime(Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);
		if (PlayerEncounter.IsActive)
		{
			PlayerEncounter.LeaveEncounter = false;
		}
		else
		{
			PlayerEncounter.Start();
			PlayerEncounter.Current.SetupFields(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
		}
		if (PlayerEncounter.Battle == null)
		{
			PlayerEncounter.StartBattle();
			PlayerEncounter.Update();
		}
		if (isDirectAssault)
		{
			AdjustTroopCountForHideoutAssault();
		}
		UpdateInitialHideoutPopulation();
		Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("hideout_center");
		if (isDirectAssault)
		{
			CampaignMission.OpenHideoutBattleMission(locationWithId.GetSceneName(0), hideoutTroops?.ToFlattenedRoster(), isTutorial: false);
		}
		else
		{
			CampaignMission.OpenHideoutAmbushMission(locationWithId.GetSceneName(0), hideoutTroops?.ToFlattenedRoster(), locationWithId);
		}
	}

	private void AdjustTroopCountForHideoutAssault()
	{
		int num = 0;
		MapEventParty mapEventParty = null;
		foreach (MapEventParty item in MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender))
		{
			if (item.Party.IsMobile)
			{
				if (mapEventParty == null)
				{
					mapEventParty = item;
				}
				num += item.Party.MemberRoster.TotalHealthyCount;
			}
		}
		if (mapEventParty != null && num < 25)
		{
			int count = 25 - num;
			CharacterObject banditBandit = mapEventParty.Party.Culture.BanditBandit;
			mapEventParty.Party.MemberRoster.AddToCounts(banditBandit, count);
		}
	}

	private void UpdateInitialHideoutPopulation()
	{
		_initialHideoutPopulation = 0;
		foreach (MobileParty party in Settlement.CurrentSettlement.Parties)
		{
			if (!party.IsBandit)
			{
				continue;
			}
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				int num = item.Number - item.WoundedNumber;
				_initialHideoutPopulation += num;
			}
		}
	}

	private bool CanChangeStatusOfTroop(CharacterObject character)
	{
		if (!character.IsPlayerCharacter)
		{
			return !character.IsNotTransferableInHideouts;
		}
		return false;
	}

	private bool game_menu_talk_to_leader_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		PartyBase party = Settlement.CurrentSettlement.Parties[0].Party;
		if (party != null && party.LeaderHero != null)
		{
			return party.LeaderHero != Hero.MainHero;
		}
		return false;
	}

	private void game_menu_talk_to_leader_on_consequence(MenuCallbackArgs args)
	{
		PartyBase party = Settlement.CurrentSettlement.Parties[0].Party;
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(party), party);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
	}

	private bool game_menu_wait_until_nightfall_on_condition(MenuCallbackArgs args)
	{
		TextObject empty = TextObject.GetEmpty();
		empty = ((!IsItNighttimeNow()) ? new TextObject("{=JYH6FF35}Wait until nightfall to sneak in") : new TextObject("{=qr1V1Drj}Wait until morning to begin the assault"));
		MBTextManager.SetTextVariable("WAIT_OPTION", empty);
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		if (Settlement.CurrentSettlement.Parties.Any((MobileParty t) => t != MobileParty.MainParty))
		{
			return Settlement.CurrentSettlement.Hideout.NextPossibleAttackTime.IsPast;
		}
		return false;
	}

	private void game_menu_wait_until_nightfall_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("hideout_wait");
	}

	private void game_menu_hideout_leave_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.CurrentSettlement != null)
		{
			PlayerEncounter.LeaveSettlement();
		}
		PlayerEncounter.Finish();
	}

	private void game_menu_hideout_after_wait_leave_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("hideout_place");
	}

	private void hideout_send_troops_wait_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.Hideout.WaitMeshName);
		UpdateSendTroopsToClearProgress(args);
	}

	private void hideout_send_troops_wait_menu_tick(MenuCallbackArgs args, CampaignTime campaignTime)
	{
		_hideoutSendTroopsWaitProgressHour += (float)campaignTime.ToHours;
		UpdateSendTroopsToClearProgress(args);
		if (args.MenuContext.GameMenu.Progress >= 1f)
		{
			ApplySendTroopsResults();
		}
	}

	private void ApplySendTroopsResults()
	{
		if (GetHideoutCanBeSuccessfullyClearedWithSendTroops(Settlement.CurrentSettlement.Hideout))
		{
			GameMenu.SwitchToMenu("hideout_send_troops_result_success");
		}
		else
		{
			GameMenu.SwitchToMenu("hideout_send_troops_result_failure");
		}
	}

	private bool GetHideoutCanBeSuccessfullyClearedWithSendTroops(Hideout hideout)
	{
		return hideout.Settlement.RandomFloatWithSeed((uint)CampaignTime.Now.ToHours) < Campaign.Current.Models.HideoutModel.GetSendTroopsSuccessChance(hideout);
	}

	private void UpdateSendTroopsToClearProgress(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(_hideoutSendTroopsWaitProgressHour / 6f);
	}

	private void hideout_send_troops_wait_leave_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Finish();
	}
}
