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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F6 RID: 1014
	public class HideoutCampaignBehavior : CampaignBehaviorBase, IHideoutCampaignBehavior
	{
		// Token: 0x17000E14 RID: 3604
		// (get) Token: 0x06003F47 RID: 16199 RVA: 0x0011E14C File Offset: 0x0011C34C
		private static float IncreaseRelationWithVillageNotableMaximumDistanceAsDays
		{
			get
			{
				return 0.5f;
			}
		}

		// Token: 0x17000E15 RID: 3605
		// (get) Token: 0x06003F48 RID: 16200 RVA: 0x0011E153 File Offset: 0x0011C353
		private int CanAttackHideoutStart
		{
			get
			{
				return Campaign.Current.Models.HideoutModel.CanAttackHideoutStartTime;
			}
		}

		// Token: 0x17000E16 RID: 3606
		// (get) Token: 0x06003F49 RID: 16201 RVA: 0x0011E169 File Offset: 0x0011C369
		private int CanAttackHideoutEnd
		{
			get
			{
				return Campaign.Current.Models.HideoutModel.CanAttackHideoutEndTime;
			}
		}

		// Token: 0x06003F4A RID: 16202 RVA: 0x0011E180 File Offset: 0x0011C380
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.HourlyTickSettlement));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase>(this.OnHideoutSpotted));
			CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, new Action<PartyBase, ItemRoster>(this.OnCollectLootItems));
		}

		// Token: 0x06003F4B RID: 16203 RVA: 0x0011E217 File Offset: 0x0011C417
		public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06003F4C RID: 16204 RVA: 0x0011E220 File Offset: 0x0011C420
		public void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06003F4D RID: 16205 RVA: 0x0011E22C File Offset: 0x0011C42C
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

		// Token: 0x06003F4E RID: 16206 RVA: 0x0011E2F6 File Offset: 0x0011C4F6
		private void OnHideoutSpotted(PartyBase party, PartyBase hideout)
		{
			SkillLevelingManager.OnHideoutSpotted(party.MobileParty, hideout);
		}

		// Token: 0x06003F4F RID: 16207 RVA: 0x0011E304 File Offset: 0x0011C504
		private int GetItemValueForHideoutLoot(ItemObject itemToLoot)
		{
			return Campaign.Current.Models.TradeItemPriceFactorModel.GetTheoreticalMaxItemMarketValue(itemToLoot) + 1;
		}

		// Token: 0x06003F50 RID: 16208 RVA: 0x0011E320 File Offset: 0x0011C520
		private void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
		{
			if (winnerParty == PartyBase.MainParty)
			{
				MapEvent mapEvent = MobileParty.MainParty.MapEvent;
				if (mapEvent.IsHideoutBattle && mapEvent.MapEventSettlement == Settlement.CurrentSettlement)
				{
					int num = 0;
					foreach (MapEventParty mapEventParty in mapEvent.GetMapEventSide(mapEvent.PlayerSide).Parties)
					{
						if (mapEventParty.Party == PartyBase.MainParty)
						{
							num = mapEventParty.PlunderedGold;
							break;
						}
					}
					int totalLootedValue = 0;
					float targetValue = (float)(num * (this._initialHideoutPopulation * 30));
					targetValue = MathF.Clamp(targetValue, (float)this._minimumHideoutLootTargetValue, 3500f);
					if ((float)totalLootedValue < targetValue)
					{
						int num2 = 0;
						ItemObject itemObject;
						while (num2 < this._potentialLootItems.Count && gainedLoots.Count < 5 && (float)totalLootedValue < targetValue)
						{
							itemObject = this._potentialLootItems[num2];
							int itemValueForHideoutLoot = this.GetItemValueForHideoutLoot(itemObject);
							if ((float)itemValueForHideoutLoot <= targetValue - (float)totalLootedValue)
							{
								gainedLoots.AddToCounts(itemObject, 1);
								totalLootedValue += itemValueForHideoutLoot;
							}
							num2++;
						}
						Func<ItemRosterElement, bool> <>9__0;
						do
						{
							Func<ItemRosterElement, bool> predicate;
							if ((predicate = <>9__0) == null)
							{
								predicate = (<>9__0 = (ItemRosterElement x) => !x.EquipmentElement.Item.NotMerchandise && !x.EquipmentElement.IsQuestItem && !x.EquipmentElement.Item.IsBannerItem && (float)this.GetItemValueForHideoutLoot(x.EquipmentElement.Item) <= targetValue - (float)totalLootedValue);
							}
							itemObject = gainedLoots.GetRandomElementWithPredicate(predicate).EquipmentElement.Item;
							if (itemObject != null)
							{
								gainedLoots.AddToCounts(itemObject, 1);
								totalLootedValue += this.GetItemValueForHideoutLoot(itemObject);
							}
						}
						while (itemObject != null);
					}
					if (!PlayerEncounter.Current.ForceHideoutSendTroops)
					{
						foreach (MapEventParty mapEventParty2 in PlayerEncounter.Battle.DefenderSide.Parties)
						{
							foreach (TroopRosterElement troopRosterElement in mapEventParty2.Party.MemberRoster.GetTroopRoster())
							{
								float expectedLootedItemValueFromCasualty = Campaign.Current.Models.BattleRewardModel.GetExpectedLootedItemValueFromCasualty(Hero.MainHero, troopRosterElement.Character);
								EquipmentElement lootedItemFromTroop = Campaign.Current.Models.BattleRewardModel.GetLootedItemFromTroop(troopRosterElement.Character, expectedLootedItemValueFromCasualty);
								if (lootedItemFromTroop.Item != null)
								{
									gainedLoots.AddToCounts(lootedItemFromTroop, 1);
								}
							}
						}
					}
					this._initialHideoutPopulation = 0;
				}
			}
		}

		// Token: 0x06003F51 RID: 16209 RVA: 0x0011E5F0 File Offset: 0x0011C7F0
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<float>("_hideoutWaitProgressHours", ref this._hideoutWaitProgressHours);
			dataStore.SyncData<float>("_hideoutWaitTargetHours", ref this._hideoutWaitTargetHours);
			dataStore.SyncData<float>("_hideoutSendTroopsWaitProgressHour", ref this._hideoutSendTroopsWaitProgressHour);
		}

		// Token: 0x06003F52 RID: 16210 RVA: 0x0011E628 File Offset: 0x0011C828
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			foreach (ItemObject itemObject in Campaign.Current.ObjectManager.GetObjectTypeList<ItemObject>())
			{
				if (itemObject.IsTradeGood)
				{
					int itemValueForHideoutLoot = this.GetItemValueForHideoutLoot(itemObject);
					if (itemValueForHideoutLoot >= this._minimumHideoutLootTargetValue && itemValueForHideoutLoot <= 3500)
					{
						this._potentialLootItems.Add(itemObject);
					}
				}
			}
			this._potentialLootItems = (from x in this._potentialLootItems
				orderby x.Value descending
				select x).ToList<ItemObject>();
			if (this._potentialLootItems.Count > 0)
			{
				this._minimumHideoutLootTargetValue = this.GetItemValueForHideoutLoot(this._potentialLootItems[this._potentialLootItems.Count - 1]);
			}
		}

		// Token: 0x06003F53 RID: 16211 RVA: 0x0011E714 File Offset: 0x0011C914
		protected void AddGameMenus(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenu("hideout_place", "{=!}{HIDEOUT_TEXT}", new OnInitDelegate(this.game_menu_hideout_place_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_place", "attack", "{=p5GkeK8F}Sneak in now", new GameMenuOption.OnConditionDelegate(this.game_menu_hideout_sneak_in_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_sneak_in_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_place", "assault", "{=*}Assault hideout", new GameMenuOption.OnConditionDelegate(this.game_menu_assault_hideout_parties_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_assault_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_place", "wait", "{=!}{WAIT_OPTION}", new GameMenuOption.OnConditionDelegate(this.game_menu_wait_until_nightfall_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_wait_until_nightfall_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_place", "send_troops", "{=qPwxYFQS}Send troops to clear", new GameMenuOption.OnConditionDelegate(this.game_menu_send_troops_hideout_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_send_troops_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_place", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_hideout_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddWaitGameMenu("hideout_wait", "{=!}{WAIT_TEXT}", new OnInitDelegate(this.hideout_wait_menu_on_init), new OnConditionDelegate(this.hideout_wait_menu_on_condition), new OnConsequenceDelegate(this.hideout_wait_menu_on_consequence), new OnTickDelegate(this.hideout_wait_menu_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameMenu.MenuOverlayType.None, this._hideoutWaitTargetHours, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_wait", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_hideout_after_wait_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddGameMenu("hideout_after_wait", "{=!}{HIDEOUT_TEXT}", new OnInitDelegate(this.hideout_after_wait_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_wait", "attack", "{=Abcgrf4j}Sneak in", new GameMenuOption.OnConditionDelegate(this.game_menu_hideout_sneak_in_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_sneak_in_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_wait", "assault", "{=*}Assault hideout", new GameMenuOption.OnConditionDelegate(this.game_menu_assault_hideout_parties_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_assault_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_wait", "send_troops", "{=qPwxYFQS}Send troops to clear", new GameMenuOption.OnConditionDelegate(this.game_menu_send_troops_hideout_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_send_troops_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_wait", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_hideout_after_wait_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddGameMenu("hideout_after_defeated_and_saved", "{=1zLZf5rw}The rest of your men rushed to your help, dragging you out to safety and driving the bandits back into hiding.", new OnInitDelegate(this.game_menu_hideout_after_defeated_and_saved_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_defeated_and_saved", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_hideout_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddGameMenu("hideout_after_found_by_sentries", "{=n0ynsBPx}Sentries detected you and alerted the rest of the bandits. Bandits moved back into hiding before you could round up your troops.", new OnInitDelegate(this.game_menu_hideout_after_defeated_and_saved_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_after_found_by_sentries", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_hideout_leave_on_consequence), true, -1, false, null);
			campaignGameStarter.AddWaitGameMenu("hideout_send_troops_wait", "{=QOT7PSUp}Your troops are clearing the hideout.", new OnInitDelegate(this.hideout_send_troops_wait_menu_on_init), null, null, new OnTickDelegate(this.hideout_send_troops_wait_menu_tick), GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 6f, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("hideout_send_troops_wait", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.hideout_send_troops_wait_leave_on_consequence), true, -1, false, null);
		}

		// Token: 0x06003F54 RID: 16212 RVA: 0x0011EA97 File Offset: 0x0011CC97
		public int GetInitialHideoutPopulation()
		{
			return this._initialHideoutPopulation;
		}

		// Token: 0x06003F55 RID: 16213 RVA: 0x0011EA9F File Offset: 0x0011CC9F
		private void hideout_wait_menu_on_init(MenuCallbackArgs args)
		{
			this.UpdateHideoutWaitProgress(args);
		}

		// Token: 0x06003F56 RID: 16214 RVA: 0x0011EAA8 File Offset: 0x0011CCA8
		private bool IsItNighttimeNow()
		{
			float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
			return (this.CanAttackHideoutStart > this.CanAttackHideoutEnd && (currentHourInDay >= (float)this.CanAttackHideoutStart || currentHourInDay <= (float)this.CanAttackHideoutEnd)) || (this.CanAttackHideoutStart < this.CanAttackHideoutEnd && currentHourInDay >= (float)this.CanAttackHideoutStart && currentHourInDay <= (float)this.CanAttackHideoutEnd);
		}

		// Token: 0x06003F57 RID: 16215 RVA: 0x0011EB10 File Offset: 0x0011CD10
		public bool hideout_wait_menu_on_condition(MenuCallbackArgs args)
		{
			return true;
		}

		// Token: 0x06003F58 RID: 16216 RVA: 0x0011EB13 File Offset: 0x0011CD13
		public void hideout_wait_menu_on_tick(MenuCallbackArgs args, CampaignTime campaignTime)
		{
			this._hideoutWaitProgressHours += (float)campaignTime.ToHours;
			this.UpdateHideoutWaitProgress(args);
		}

		// Token: 0x06003F59 RID: 16217 RVA: 0x0011EB34 File Offset: 0x0011CD34
		private void UpdateHideoutWaitProgress(MenuCallbackArgs args)
		{
			TextObject text = TextObject.GetEmpty();
			if (!this.IsItNighttimeNow())
			{
				text = new TextObject("{=VLLAOXve}Waiting until nightfall to ambush.", null);
			}
			else
			{
				text = new TextObject("{=*}Waiting until day to assault.", null);
			}
			MBTextManager.SetTextVariable("WAIT_TEXT", text, false);
			if (this._hideoutWaitTargetHours.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				this.CalculateHideoutAttackTime();
			}
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(this._hideoutWaitProgressHours / this._hideoutWaitTargetHours);
		}

		// Token: 0x06003F5A RID: 16218 RVA: 0x0011EBAF File Offset: 0x0011CDAF
		public void hideout_wait_menu_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("hideout_after_wait");
		}

		// Token: 0x06003F5B RID: 16219 RVA: 0x0011EBBB File Offset: 0x0011CDBB
		private bool leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003F5C RID: 16220 RVA: 0x0011EBC8 File Offset: 0x0011CDC8
		[GameMenuInitializationHandler("hideout_wait")]
		[GameMenuInitializationHandler("hideout_after_wait")]
		[GameMenuInitializationHandler("hideout_after_defeated_and_saved")]
		private static void game_menu_hideout_ui_place_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			args.MenuContext.SetBackgroundMeshName(currentSettlement.Hideout.WaitMeshName);
		}

		// Token: 0x06003F5D RID: 16221 RVA: 0x0011EBF4 File Offset: 0x0011CDF4
		[GameMenuInitializationHandler("hideout_place")]
		private static void game_menu_hideout_sound_place_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetPanelSound("event:/ui/panels/settlement_hideout");
			Settlement currentSettlement = Settlement.CurrentSettlement;
			args.MenuContext.SetBackgroundMeshName(currentSettlement.Hideout.WaitMeshName);
		}

		// Token: 0x06003F5E RID: 16222 RVA: 0x0011EC2D File Offset: 0x0011CE2D
		private void game_menu_hideout_after_defeated_and_saved_on_init(MenuCallbackArgs args)
		{
			if (!Settlement.CurrentSettlement.IsHideout)
			{
				return;
			}
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				PlayerEncounter.EnterSettlement();
			}
		}

		// Token: 0x06003F5F RID: 16223 RVA: 0x0011EC50 File Offset: 0x0011CE50
		private void game_menu_hideout_place_on_init(MenuCallbackArgs args)
		{
			if (!Settlement.CurrentSettlement.IsHideout)
			{
				return;
			}
			this._hideoutWaitProgressHours = 0f;
			this._hideoutSendTroopsWaitProgressHour = 0f;
			if (!this.IsItNighttimeNow())
			{
				this.CalculateHideoutAttackTime();
			}
			else
			{
				this._hideoutWaitTargetHours = 0f;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			int num = 0;
			foreach (MobileParty mobileParty in currentSettlement.Parties)
			{
				num += mobileParty.MemberRoster.TotalManCount - mobileParty.MemberRoster.TotalWounded;
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
			bool flag = !currentSettlement.Hideout.NextPossibleAttackTime.IsPast;
			if (flag)
			{
				GameTexts.SetVariable("HIDEOUT_TEXT", "{=KLWn6yZQ}{HIDEOUT_DESCRIPTION} The remains of a fire suggest that it's been recently occupied, but its residents - whoever they are - are well-hidden for now.");
			}
			else if (num > 0)
			{
				GameTexts.SetVariable("HIDEOUT_TEXT", "{=prcBBqMR}{HIDEOUT_DESCRIPTION} You see armed men moving about. As you listen quietly, you hear scraps of conversation about raids, ransoms, and the best places to waylay travellers.");
			}
			else
			{
				GameTexts.SetVariable("HIDEOUT_TEXT", "{=gywyEgZa}{HIDEOUT_DESCRIPTION} There seems to be no one inside.");
			}
			if (!flag && num > 0 && Hero.MainHero.IsWounded)
			{
				GameTexts.SetVariable("HIDEOUT_TEXT", "{=fMekM2UH}{HIDEOUT_DESCRIPTION} You can not attack since your wounds do not allow you.");
			}
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				PlayerEncounter.EnterSettlement();
			}
			bool isInfested = Settlement.CurrentSettlement.Hideout.IsInfested;
			Settlement settlement = (Settlement.CurrentSettlement.IsHideout ? Settlement.CurrentSettlement : null);
			if (PlayerEncounter.Battle != null)
			{
				bool flag2 = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Current.PlayerSide;
				PlayerEncounter.Update();
				if (flag2 && PlayerEncounter.Battle == null && settlement != null)
				{
					this.SetCleanHideoutRelations(settlement);
				}
			}
		}

		// Token: 0x06003F60 RID: 16224 RVA: 0x0011EEAC File Offset: 0x0011D0AC
		private void CalculateHideoutAttackTime()
		{
			float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
			if (this.IsItNighttimeNow())
			{
				this._hideoutWaitTargetHours = (((float)this.CanAttackHideoutEnd > currentHourInDay) ? ((float)this.CanAttackHideoutEnd - currentHourInDay) : ((float)CampaignTime.HoursInDay - currentHourInDay + (float)this.CanAttackHideoutEnd));
				return;
			}
			this._hideoutWaitTargetHours = (((float)this.CanAttackHideoutStart > currentHourInDay) ? ((float)this.CanAttackHideoutStart - currentHourInDay) : ((float)CampaignTime.HoursInDay - currentHourInDay + (float)this.CanAttackHideoutStart));
		}

		// Token: 0x06003F61 RID: 16225 RVA: 0x0011EF28 File Offset: 0x0011D128
		private void SetCleanHideoutRelations(Settlement hideout)
		{
			List<Settlement> list = new List<Settlement>();
			float num = HideoutCampaignBehavior.IncreaseRelationWithVillageNotableMaximumDistanceAsDays * Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay;
			foreach (Village village in Campaign.Current.AllVillages)
			{
				if (village.Settlement.Position.DistanceSquared(hideout.Position) <= num * num)
				{
					list.Add(village.Settlement);
				}
			}
			foreach (Settlement settlement in list)
			{
				if (settlement.Notables.Count > 0)
				{
					ChangeRelationAction.ApplyPlayerRelation(settlement.Notables.GetRandomElement<Hero>(), 2, true, false);
				}
			}
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
			{
				Town town = SettlementHelper.FindNearestTownToSettlement(hideout, MobileParty.NavigationType.All, null);
				Hero leader = town.OwnerClan.Leader;
				if (leader == Hero.MainHero)
				{
					town.Loyalty += 1f;
				}
				else
				{
					ChangeRelationAction.ApplyPlayerRelation(leader, (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus, true, true);
				}
			}
			MBTextManager.SetTextVariable("RELATION_VALUE", (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
			MBInformationManager.AddQuickInformation(new TextObject("{=o0qwDa0q}Your relation increased by {RELATION_VALUE} with nearby notables.", null), 0, null, null, "");
		}

		// Token: 0x06003F62 RID: 16226 RVA: 0x0011F0A4 File Offset: 0x0011D2A4
		private void hideout_after_wait_menu_on_init(MenuCallbackArgs args)
		{
			TextObject text = TextObject.GetEmpty();
			if (this.IsItNighttimeNow())
			{
				text = new TextObject("{=VbU8Ue0O}After waiting for a while you find a good opportunity to close in undetected beneath the shroud of the night.", null);
			}
			else
			{
				text = new TextObject("{=*}After waiting for a while you find a good opportunity to assault the camp.", null);
			}
			MBTextManager.SetTextVariable("HIDEOUT_TEXT", text, false);
		}

		// Token: 0x06003F63 RID: 16227 RVA: 0x0011F0E8 File Offset: 0x0011D2E8
		private bool game_menu_hideout_sneak_in_on_condition(MenuCallbackArgs args)
		{
			Hideout hideout = Settlement.CurrentSettlement.Hideout;
			object obj;
			if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction)
			{
				if (Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
				{
					obj = hideout.NextPossibleAttackTime.IsPast;
					goto IL_62;
				}
			}
			obj = 0;
			IL_62:
			object obj2 = obj;
			if (obj2 != null)
			{
				if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=pM9GOxrV}You are wounded, you can't sneak in!", null);
				}
				else
				{
					int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, false);
					if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
					{
						args.IsEnabled = false;
						args.Tooltip = new TextObject("{=XasRXCod}You should have more than {AMOUNT} healthy troops in your party to attack!", null);
						args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
					}
					else if (!this.IsItNighttimeNow())
					{
						args.Tooltip = new TextObject("{=*}Bandits are awake and too aware to be ambushed.", null);
						args.IsEnabled = false;
					}
				}
			}
			args.optionLeaveType = GameMenuOption.LeaveType.SneakIn;
			return obj2 != null;
		}

		// Token: 0x06003F64 RID: 16228 RVA: 0x0011F204 File Offset: 0x0011D404
		private bool game_menu_assault_hideout_parties_on_condition(MenuCallbackArgs args)
		{
			Hideout hideout = Settlement.CurrentSettlement.Hideout;
			object obj;
			if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction)
			{
				if (Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
				{
					obj = hideout.NextPossibleAttackTime.IsPast;
					goto IL_62;
				}
			}
			obj = 0;
			IL_62:
			object obj2 = obj;
			if (obj2 != null)
			{
				if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=*}You are wounded, you can't assault the hideout!", null);
				}
				else
				{
					int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, true);
					if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
					{
						args.IsEnabled = false;
						args.Tooltip = new TextObject("{=XasRXCod}You should have more than {AMOUNT} healthy troops in your party to attack!", null);
						args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
					}
					else if (this.IsItNighttimeNow())
					{
						args.Tooltip = new TextObject("{=*}A frontal assault against the hideout is too dangerous.", null);
						args.IsEnabled = false;
					}
				}
				args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			}
			return obj2 != null;
		}

		// Token: 0x06003F65 RID: 16229 RVA: 0x0011F320 File Offset: 0x0011D520
		private void game_menu_encounter_sneak_in_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args, delegate(TroopRoster x)
			{
				this.OnTroopRosterManageDone(x, false);
			}, false);
		}

		// Token: 0x06003F66 RID: 16230 RVA: 0x0011F336 File Offset: 0x0011D536
		private void game_menu_encounter_assault_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args, delegate(TroopRoster x)
			{
				this.OnTroopRosterManageDone(x, true);
			}, true);
		}

		// Token: 0x06003F67 RID: 16231 RVA: 0x0011F34C File Offset: 0x0011D54C
		private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args, Action<TroopRoster> onDone, bool isDirectAssault)
		{
			BanditDensityModel banditDensityModel = Campaign.Current.Models.BanditDensityModel;
			int maximumTroopCountForHideoutMission = banditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault);
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, maximumTroopCountForHideoutMission, true);
			troopRoster.Add(strongestAndPriorTroops);
			int maximumTroopCountForHideoutMission2 = banditDensityModel.GetMaximumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault);
			args.MenuContext.OpenTroopSelection(MobileParty.MainParty.MemberRoster, troopRoster, new Func<CharacterObject, bool>(this.CanChangeStatusOfTroop), onDone, maximumTroopCountForHideoutMission2, banditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isDirectAssault));
		}

		// Token: 0x06003F68 RID: 16232 RVA: 0x0011F3D0 File Offset: 0x0011D5D0
		private bool game_menu_send_troops_hideout_on_condition(MenuCallbackArgs args)
		{
			Hideout hideout = Settlement.CurrentSettlement.Hideout;
			object obj;
			if (Settlement.CurrentSettlement.MapFaction != PartyBase.MainParty.MapFaction)
			{
				if (Settlement.CurrentSettlement.Parties.Any((MobileParty x) => x.IsBandit))
				{
					obj = hideout.NextPossibleAttackTime.IsPast;
					goto IL_62;
				}
			}
			obj = 0;
			IL_62:
			object obj2 = obj;
			if (obj2 != null)
			{
				int minimumTroopCountForHideoutMission = Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, false);
				if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumTroopCountForHideoutMission)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=yUbdUFSC}You should have more than {AMOUNT} healthy troops in your party to send your troops!", null);
					args.Tooltip.SetTextVariable("AMOUNT", minimumTroopCountForHideoutMission);
				}
				args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
			}
			return obj2 != null;
		}

		// Token: 0x06003F69 RID: 16233 RVA: 0x0011F4A1 File Offset: 0x0011D6A1
		private void game_menu_encounter_send_troops_on_consequence(MenuCallbackArgs args)
		{
			this.UpdateInitialHideoutPopulation();
			PlayerEncounter.Current.ForceHideoutSendTroops = true;
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06003F6A RID: 16234 RVA: 0x0011F4C0 File Offset: 0x0011D6C0
		private void ArrangeHideoutTroopCountsForMission()
		{
			int numberOfMinimumBanditTroopsInHideoutMission = Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditTroopsInHideoutMission;
			int num = Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForFirstFightInHideout + Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForBossFightInHideout;
			MBList<MobileParty> mblist = (from x in Settlement.CurrentSettlement.Parties
				where x.IsBandit || x.IsBanditBossParty
				select x).ToMBList<MobileParty>();
			int num2 = mblist.Sum((MobileParty x) => x.MemberRoster.TotalHealthyCount);
			if (num2 > num)
			{
				int i = num2 - num;
				mblist.RemoveAll((MobileParty x) => x.IsBanditBossParty || x.MemberRoster.TotalHealthyCount == 1);
				while (i > 0)
				{
					if (mblist.Count <= 0)
					{
						return;
					}
					MobileParty randomElement = mblist.GetRandomElement<MobileParty>();
					List<TroopRosterElement> troopRoster = randomElement.MemberRoster.GetTroopRoster();
					List<ValueTuple<TroopRosterElement, float>> list = new List<ValueTuple<TroopRosterElement, float>>();
					foreach (TroopRosterElement item in troopRoster)
					{
						list.Add(new ValueTuple<TroopRosterElement, float>(item, (float)(item.Number - item.WoundedNumber)));
					}
					TroopRosterElement troopRosterElement = MBRandom.ChooseWeighted<TroopRosterElement>(list);
					randomElement.MemberRoster.AddToCounts(troopRosterElement.Character, -1, false, 0, 0, true, -1);
					i--;
					if (randomElement.MemberRoster.TotalHealthyCount == 1)
					{
						mblist.Remove(randomElement);
					}
				}
			}
			else if (num2 < numberOfMinimumBanditTroopsInHideoutMission)
			{
				int num3 = numberOfMinimumBanditTroopsInHideoutMission - num2;
				mblist.RemoveAll((MobileParty x) => x.MemberRoster.GetTroopRoster().All((TroopRosterElement y) => y.Number == 0 || y.Character.Culture.BanditBoss == y.Character || y.Character.IsHero));
				while (num3 > 0 && mblist.Count > 0)
				{
					MobileParty randomElement2 = mblist.GetRandomElement<MobileParty>();
					List<TroopRosterElement> troopRoster2 = randomElement2.MemberRoster.GetTroopRoster();
					List<ValueTuple<TroopRosterElement, float>> list2 = new List<ValueTuple<TroopRosterElement, float>>();
					foreach (TroopRosterElement troopRosterElement2 in troopRoster2)
					{
						list2.Add(new ValueTuple<TroopRosterElement, float>(troopRosterElement2, (float)(troopRosterElement2.Number * ((troopRosterElement2.Character.Culture.BanditBoss == troopRosterElement2.Character || troopRosterElement2.Character.IsHero) ? 0 : 1))));
					}
					TroopRosterElement troopRosterElement3 = MBRandom.ChooseWeighted<TroopRosterElement>(list2);
					randomElement2.MemberRoster.AddToCounts(troopRosterElement3.Character, 1, false, 0, 0, true, -1);
					num3--;
				}
			}
		}

		// Token: 0x06003F6B RID: 16235 RVA: 0x0011F770 File Offset: 0x0011D970
		private void OnTroopRosterManageDone(TroopRoster hideoutTroops, bool isDirectAssault)
		{
			this.ArrangeHideoutTroopCountsForMission();
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
				this.AdjustTroopCountForHideoutAssault();
			}
			this.UpdateInitialHideoutPopulation();
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("hideout_center");
			if (isDirectAssault)
			{
				CampaignMission.OpenHideoutBattleMission(locationWithId.GetSceneName(0), (hideoutTroops != null) ? hideoutTroops.ToFlattenedRoster() : null, false);
				return;
			}
			CampaignMission.OpenHideoutAmbushMission(locationWithId.GetSceneName(0), (hideoutTroops != null) ? hideoutTroops.ToFlattenedRoster() : null, locationWithId);
		}

		// Token: 0x06003F6C RID: 16236 RVA: 0x0011F84C File Offset: 0x0011DA4C
		private void AdjustTroopCountForHideoutAssault()
		{
			int num = 0;
			MapEventParty mapEventParty = null;
			foreach (MapEventParty mapEventParty2 in MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender))
			{
				if (mapEventParty2.Party.IsMobile)
				{
					if (mapEventParty == null)
					{
						mapEventParty = mapEventParty2;
					}
					num += mapEventParty2.Party.MemberRoster.TotalHealthyCount;
				}
			}
			if (mapEventParty != null && num < 25)
			{
				int count = 25 - num;
				CharacterObject banditBandit = mapEventParty.Party.Culture.BanditBandit;
				mapEventParty.Party.MemberRoster.AddToCounts(banditBandit, count, false, 0, 0, true, -1);
			}
		}

		// Token: 0x06003F6D RID: 16237 RVA: 0x0011F900 File Offset: 0x0011DB00
		private void UpdateInitialHideoutPopulation()
		{
			this._initialHideoutPopulation = 0;
			foreach (MobileParty mobileParty in Settlement.CurrentSettlement.Parties)
			{
				if (mobileParty.IsBandit)
				{
					foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
					{
						int num = troopRosterElement.Number - troopRosterElement.WoundedNumber;
						this._initialHideoutPopulation += num;
					}
				}
			}
		}

		// Token: 0x06003F6E RID: 16238 RVA: 0x0011F9C0 File Offset: 0x0011DBC0
		private bool CanChangeStatusOfTroop(CharacterObject character)
		{
			return !character.IsPlayerCharacter && !character.IsNotTransferableInHideouts;
		}

		// Token: 0x06003F6F RID: 16239 RVA: 0x0011F9D8 File Offset: 0x0011DBD8
		private bool game_menu_talk_to_leader_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			PartyBase party = Settlement.CurrentSettlement.Parties[0].Party;
			return party != null && party.LeaderHero != null && party.LeaderHero != Hero.MainHero;
		}

		// Token: 0x06003F70 RID: 16240 RVA: 0x0011FA20 File Offset: 0x0011DC20
		private void game_menu_talk_to_leader_on_consequence(MenuCallbackArgs args)
		{
			PartyBase party = Settlement.CurrentSettlement.Parties[0].Party;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(party), party, false, false, false, false, false, false);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
		}

		// Token: 0x06003F71 RID: 16241 RVA: 0x0011FA80 File Offset: 0x0011DC80
		private bool game_menu_wait_until_nightfall_on_condition(MenuCallbackArgs args)
		{
			TextObject text = TextObject.GetEmpty();
			if (this.IsItNighttimeNow())
			{
				text = new TextObject("{=*}Wait until day to assault", null);
			}
			else
			{
				text = new TextObject("{=JYH6FF35}Wait until nightfall to sneak in", null);
			}
			MBTextManager.SetTextVariable("WAIT_OPTION", text, false);
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return Settlement.CurrentSettlement.Parties.Any((MobileParty t) => t != MobileParty.MainParty) && Settlement.CurrentSettlement.Hideout.NextPossibleAttackTime.IsPast;
		}

		// Token: 0x06003F72 RID: 16242 RVA: 0x0011FB12 File Offset: 0x0011DD12
		private void game_menu_wait_until_nightfall_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("hideout_wait");
		}

		// Token: 0x06003F73 RID: 16243 RVA: 0x0011FB1E File Offset: 0x0011DD1E
		private void game_menu_hideout_leave_on_consequence(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.CurrentSettlement != null)
			{
				PlayerEncounter.LeaveSettlement();
			}
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003F74 RID: 16244 RVA: 0x0011FB37 File Offset: 0x0011DD37
		private void game_menu_hideout_after_wait_leave_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("hideout_place");
		}

		// Token: 0x06003F75 RID: 16245 RVA: 0x0011FB43 File Offset: 0x0011DD43
		private void hideout_send_troops_wait_menu_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.Hideout.WaitMeshName);
			this.UpdateSendTroopsToClearProgress(args);
			if (args.MenuContext.GameMenu.Progress >= 1f)
			{
				PlayerEncounter.Update();
			}
		}

		// Token: 0x06003F76 RID: 16246 RVA: 0x0011FB84 File Offset: 0x0011DD84
		private void hideout_send_troops_wait_menu_tick(MenuCallbackArgs args, CampaignTime campaignTime)
		{
			this._hideoutSendTroopsWaitProgressHour += (float)campaignTime.ToHours;
			this.UpdateSendTroopsToClearProgress(args);
			if (args.MenuContext.GameMenu.Progress >= 1f)
			{
				PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.PlayerSide);
				CampaignEventDispatcher.Instance.OnHideoutBattleCompleted(PlayerEncounter.Battle.PlayerSide, (HideoutEventComponent)PlayerEncounter.Battle.Component);
				PlayerEncounter.Update();
				Settlement encounterSettlement = PlayerEncounter.EncounterSettlement;
				if (encounterSettlement == null)
				{
					return;
				}
				encounterSettlement.Party.SetVisualAsDirty();
			}
		}

		// Token: 0x06003F77 RID: 16247 RVA: 0x0011FC14 File Offset: 0x0011DE14
		private void UpdateSendTroopsToClearProgress(MenuCallbackArgs args)
		{
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(this._hideoutSendTroopsWaitProgressHour / 6f);
		}

		// Token: 0x06003F78 RID: 16248 RVA: 0x0011FC32 File Offset: 0x0011DE32
		private void hideout_send_troops_wait_leave_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x040012C2 RID: 4802
		private const int HideoutClearRelationEffect = 2;

		// Token: 0x040012C3 RID: 4803
		private const int HideoutLootTargetValueMultiplier = 30;

		// Token: 0x040012C4 RID: 4804
		private int _minimumHideoutLootTargetValue = 350;

		// Token: 0x040012C5 RID: 4805
		private const int MaximumHideoutLootTargetValue = 3500;

		// Token: 0x040012C6 RID: 4806
		private const int MaximumHideoutExtraLootTypeCount = 5;

		// Token: 0x040012C7 RID: 4807
		private const float HideoutSendTroopsWaitTargetHour = 6f;

		// Token: 0x040012C8 RID: 4808
		private float _hideoutWaitProgressHours;

		// Token: 0x040012C9 RID: 4809
		private float _hideoutWaitTargetHours;

		// Token: 0x040012CA RID: 4810
		private float _hideoutSendTroopsWaitProgressHour;

		// Token: 0x040012CB RID: 4811
		private int _initialHideoutPopulation;

		// Token: 0x040012CC RID: 4812
		private List<ItemObject> _potentialLootItems = new List<ItemObject>();
	}
}
