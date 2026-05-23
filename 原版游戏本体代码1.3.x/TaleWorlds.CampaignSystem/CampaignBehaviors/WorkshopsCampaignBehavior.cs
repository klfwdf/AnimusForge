using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000452 RID: 1106
	public class WorkshopsCampaignBehavior : CampaignBehaviorBase, IWorkshopWarehouseCampaignBehavior
	{
		// Token: 0x060046FF RID: 18175 RVA: 0x00162DF8 File Offset: 0x00160FF8
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterSessionLaunched));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, new Action<Town>(this.DailyTickTown));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.WorkshopOwnerChangedEvent.AddNonSerializedListener(this, new Action<Workshop, Hero>(this.OnWorkshopOwnerChanged));
			CampaignEvents.WorkshopTypeChangedEvent.AddNonSerializedListener(this, new Action<Workshop>(this.OnWorkshopTypeChanged));
		}

		// Token: 0x06004700 RID: 18176 RVA: 0x00162EEB File Offset: 0x001610EB
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<KeyValuePair<Settlement, ItemRoster>[]>("_warehouseRosterPerSettlement", ref this._warehouseRosterPerSettlement);
			dataStore.SyncData<WorkshopsCampaignBehavior.WorkshopData[]>("_workshopData", ref this._workshopData);
		}

		// Token: 0x06004701 RID: 18177 RVA: 0x00162F11 File Offset: 0x00161111
		private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
		{
			if (i >= 10)
			{
				if (i == 10)
				{
					this.InitializeBehaviorData();
					this.FillItemsInAllCategories();
					this.InitializeWorkshops();
					this.BuildWorkshopsAtGameStart();
				}
				if (i % 20 == 0)
				{
					this.RunTownShopsAtGameStart();
				}
			}
		}

		// Token: 0x06004702 RID: 18178 RVA: 0x00162F44 File Offset: 0x00161144
		private void InitializeBehaviorData()
		{
			if (this._workshopData == null)
			{
				this._workshopData = new WorkshopsCampaignBehavior.WorkshopData[Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave];
			}
			if (this._warehouseRosterPerSettlement == null)
			{
				this._warehouseRosterPerSettlement = new KeyValuePair<Settlement, ItemRoster>[Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave];
			}
		}

		// Token: 0x06004703 RID: 18179 RVA: 0x00162FA0 File Offset: 0x001611A0
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeBehaviorData();
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0", 0))
			{
				foreach (Workshop workshop in Hero.MainHero.OwnedWorkshops)
				{
					this.AddNewWorkshopData(workshop);
					this.AddNewWarehouseDataIfNeeded(workshop.Settlement);
				}
			}
			if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.9.35637", 0)))
			{
				for (int i = 0; i < this._workshopData.Length; i++)
				{
					if (this._workshopData[i] != null && this._workshopData[i].Workshop.Owner != Hero.MainHero)
					{
						this._workshopData[i] = null;
					}
				}
			}
			this.EnsureBehaviorDataSize();
			this.FillItemsInAllCategories();
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0", 0))
			{
				this.RemoveDeadOwnersFromWorkshops();
			}
		}

		// Token: 0x06004704 RID: 18180 RVA: 0x001630B0 File Offset: 0x001612B0
		private void RemoveDeadOwnersFromWorkshops()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsTown)
				{
					foreach (Workshop workshop in settlement.Town.Workshops)
					{
						if (workshop.Owner.IsDead)
						{
							Debug.FailedAssert("Workshop owner is dead", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\WorkshopsCampaignBehavior.cs", "RemoveDeadOwnersFromWorkshops", 176);
							Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
							ChangeOwnerOfWorkshopAction.ApplyByDeath(workshop, notableOwnerForWorkshop);
						}
					}
				}
			}
		}

		// Token: 0x06004705 RID: 18181 RVA: 0x0016316C File Offset: 0x0016136C
		private void EnsureBehaviorDataSize()
		{
			if (this._workshopData.Length < Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave)
			{
				WorkshopsCampaignBehavior.WorkshopData[] array = new WorkshopsCampaignBehavior.WorkshopData[Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave];
				for (int i = 0; i < Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave; i++)
				{
					if (i < this._workshopData.Length)
					{
						array[i] = this._workshopData[i];
					}
					else
					{
						array[i] = null;
					}
				}
				this._workshopData = array;
			}
			if (this._warehouseRosterPerSettlement.Length < Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave)
			{
				KeyValuePair<Settlement, ItemRoster>[] array2 = new KeyValuePair<Settlement, ItemRoster>[Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave];
				for (int j = 0; j < Campaign.Current.Models.WorkshopModel.MaximumWorkshopsPlayerCanHave; j++)
				{
					if (j < this._warehouseRosterPerSettlement.Length && this._warehouseRosterPerSettlement[j].Key != null && this._warehouseRosterPerSettlement[j].Value != null)
					{
						array2[j] = this._warehouseRosterPerSettlement[j];
					}
				}
				this._warehouseRosterPerSettlement = array2;
			}
		}

		// Token: 0x06004706 RID: 18182 RVA: 0x00163298 File Offset: 0x00161498
		private void OnWorkshopTypeChanged(Workshop workshop)
		{
			if (workshop.Owner == Hero.MainHero)
			{
				this.RemoveWorkshopData(workshop);
				this.AddNewWorkshopData(workshop);
			}
		}

		// Token: 0x06004707 RID: 18183 RVA: 0x001632B8 File Offset: 0x001614B8
		private void OnWorkshopOwnerChanged(Workshop workshop, Hero oldOwner)
		{
			Hero owner = workshop.Owner;
			if (owner == Hero.MainHero)
			{
				this.AddNewWarehouseDataIfNeeded(workshop.Settlement);
				this.AddNewWorkshopData(workshop);
				return;
			}
			if (oldOwner == Hero.MainHero && Clan.PlayerClan.Leader != owner)
			{
				if (Hero.MainHero.OwnedWorkshops.All((Workshop x) => x.Settlement != workshop.Settlement))
				{
					if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == workshop.Settlement)
					{
						this.TransferWarehouseToPlayerParty(Settlement.CurrentSettlement);
					}
					this.RemoveWarehouseData(workshop.Settlement);
				}
				this.RemoveWorkshopData(workshop);
			}
		}

		// Token: 0x06004708 RID: 18184 RVA: 0x00163378 File Offset: 0x00161578
		private void DailyTickTown(Town town)
		{
			foreach (Workshop workshop in town.Workshops)
			{
				if (!town.InRebelliousState)
				{
					this.RunTownWorkshop(town, workshop);
				}
				this.HandleDailyExpense(workshop);
			}
		}

		// Token: 0x06004709 RID: 18185 RVA: 0x001633B8 File Offset: 0x001615B8
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (!victim.IsHumanPlayerCharacter)
			{
				foreach (Workshop workshop in victim.OwnedWorkshops.ToList<Workshop>())
				{
					Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
					ChangeOwnerOfWorkshopAction.ApplyByDeath(workshop, notableOwnerForWorkshop);
				}
			}
		}

		// Token: 0x0600470A RID: 18186 RVA: 0x00163430 File Offset: 0x00161630
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			this.TransferPlayerWorkshopsIfNeeded();
		}

		// Token: 0x0600470B RID: 18187 RVA: 0x00163438 File Offset: 0x00161638
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			this.TransferPlayerWorkshopsIfNeeded();
		}

		// Token: 0x0600470C RID: 18188 RVA: 0x00163440 File Offset: 0x00161640
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newSettlementOwner, Hero oldSettlementOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement.IsTown)
			{
				foreach (Workshop workshop in settlement.Town.Workshops)
				{
					if (workshop.Owner != null && workshop.Owner.MapFaction.IsAtWarWith(newSettlementOwner.MapFaction) && workshop.Owner.GetPerkValue(DefaultPerks.Trade.RapidDevelopment))
					{
						GiveGoldAction.ApplyBetweenCharacters(null, workshop.Owner, MathF.Round(DefaultPerks.Trade.RapidDevelopment.PrimaryBonus), false);
					}
				}
				this.TransferPlayerWorkshopsIfNeeded();
			}
		}

		// Token: 0x0600470D RID: 18189 RVA: 0x001634C7 File Offset: 0x001616C7
		private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeGameMenus(campaignGameStarter);
		}

		// Token: 0x0600470E RID: 18190 RVA: 0x001634D0 File Offset: 0x001616D0
		protected void InitializeGameMenus(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenuOption("town", "manage_warehouse", "{=LK4kNZkb}Enter the warehouse", new GameMenuOption.OnConditionDelegate(this.warehouse_manage_on_condition), new GameMenuOption.OnConsequenceDelegate(this.warehouse_manage_on_consequence), false, 8, false, null);
			campaignGameStarter.AddPlayerLine("workshop_worker_manage_warehouse", "player_options", "warehouse", "{=mBnoWa8R}I would like to access the Warehouse.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("workshop_worker_manage_warehouse_answer", "warehouse", "player_options", "{=Y4LhmAdi}Sure, boss. Go ahead.", null, new ConversationSentence.OnConsequenceDelegate(this.warehouse_manage_on_consequence), 100, null);
		}

		// Token: 0x0600470F RID: 18191 RVA: 0x0016355C File Offset: 0x0016175C
		private void warehouse_manage_on_consequence()
		{
			InventoryLogic.CapacityData otherSideCapacity = new InventoryLogic.CapacityData(new Func<int>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityDelegate|21_0), new Func<TextObject>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityExceededWarningDelegate|21_1), new Func<TextObject>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityExceededHintDelegate|21_2), false);
			InventoryScreenHelper.OpenScreenAsWarehouse(this.GetWarehouseRoster(Settlement.CurrentSettlement), otherSideCapacity);
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		// Token: 0x06004710 RID: 18192 RVA: 0x001635C0 File Offset: 0x001617C0
		private void warehouse_manage_on_consequence(MenuCallbackArgs args)
		{
			InventoryLogic.CapacityData otherSideCapacity = new InventoryLogic.CapacityData(new Func<int>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityDelegate|22_0), new Func<TextObject>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityExceededWarningDelegate|22_1), new Func<TextObject>(WorkshopsCampaignBehavior.<>c.<>9.<warehouse_manage_on_consequence>g__CapacityExceededHintDelegate|22_2), false);
			InventoryScreenHelper.OpenScreenAsWarehouse(this.GetWarehouseRoster(Settlement.CurrentSettlement), otherSideCapacity);
		}

		// Token: 0x06004711 RID: 18193 RVA: 0x00163615 File Offset: 0x00161815
		private bool warehouse_manage_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Warehouse;
			return this.GetWarehouseRoster(Settlement.CurrentSettlement) != null;
		}

		// Token: 0x06004712 RID: 18194 RVA: 0x00163630 File Offset: 0x00161830
		bool IWorkshopWarehouseCampaignBehavior.IsGettingInputsFromWarehouse(Workshop workshop)
		{
			WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop = this.GetDataOfWorkshop(workshop);
			return dataOfWorkshop != null && dataOfWorkshop.IsGettingInputsFromWarehouse;
		}

		// Token: 0x06004713 RID: 18195 RVA: 0x00163650 File Offset: 0x00161850
		void IWorkshopWarehouseCampaignBehavior.SetIsGettingInputsFromWarehouse(Workshop workshop, bool isActive)
		{
			WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop = this.GetDataOfWorkshop(workshop);
			if (dataOfWorkshop != null)
			{
				dataOfWorkshop.IsGettingInputsFromWarehouse = isActive;
			}
		}

		// Token: 0x06004714 RID: 18196 RVA: 0x00163670 File Offset: 0x00161870
		float IWorkshopWarehouseCampaignBehavior.GetStockProductionInWarehouseRatio(Workshop workshop)
		{
			WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop = this.GetDataOfWorkshop(workshop);
			if (dataOfWorkshop != null)
			{
				return dataOfWorkshop.StockProductionInWarehouseRatio;
			}
			return 0f;
		}

		// Token: 0x06004715 RID: 18197 RVA: 0x00163694 File Offset: 0x00161894
		void IWorkshopWarehouseCampaignBehavior.SetStockProductionInWarehouseRatio(Workshop workshop, float ratio)
		{
			WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop = this.GetDataOfWorkshop(workshop);
			if (dataOfWorkshop != null)
			{
				dataOfWorkshop.StockProductionInWarehouseRatio = ratio;
			}
		}

		// Token: 0x06004716 RID: 18198 RVA: 0x001636B4 File Offset: 0x001618B4
		int IWorkshopWarehouseCampaignBehavior.GetInputCount(Workshop workshop)
		{
			ItemRoster warehouseRoster = this.GetWarehouseRoster(workshop.Settlement);
			int num = 0;
			List<ItemCategory> list = new List<ItemCategory>();
			foreach (WorkshopType.Production production in workshop.WorkshopType.Productions)
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
				{
					ItemCategory item = valueTuple.Item1;
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			foreach (ItemCategory itemCategory in list)
			{
				foreach (ItemRosterElement itemRosterElement in warehouseRoster)
				{
					if (itemRosterElement.EquipmentElement.Item.ItemCategory == itemCategory)
					{
						num += itemRosterElement.Amount;
						break;
					}
				}
			}
			return num;
		}

		// Token: 0x06004717 RID: 18199 RVA: 0x00163804 File Offset: 0x00161A04
		ExplainedNumber IWorkshopWarehouseCampaignBehavior.GetInputDailyChange(Workshop workshop)
		{
			ItemRoster warehouseRoster = this.GetWarehouseRoster(workshop.Settlement);
			ExplainedNumber result = new ExplainedNumber(0f, true, null);
			Dictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
			foreach (WorkshopType.Production production in workshop.WorkshopType.Productions)
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
				{
					float num = Campaign.Current.Models.WorkshopModel.GetEffectiveConversionSpeedOfProduction(workshop, production.ConversionSpeed, false).ResultNumber * (float)valueTuple.Item2;
					ItemCategory item = valueTuple.Item1;
					float num2;
					if (!dictionary.TryGetValue(item, out num2))
					{
						dictionary.Add(item, num);
					}
					else
					{
						dictionary[item] = num2 + num;
					}
				}
			}
			foreach (KeyValuePair<ItemCategory, float> keyValuePair in dictionary)
			{
				int num3 = 0;
				foreach (ItemRosterElement itemRosterElement in warehouseRoster)
				{
					if (itemRosterElement.EquipmentElement.Item.ItemCategory == keyValuePair.Key)
					{
						num3 += itemRosterElement.Amount;
					}
				}
				if (num3 > 0)
				{
					TextObject textObject = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null);
					textObject.SetTextVariable("RANK", keyValuePair.Key.GetName());
					textObject.SetTextVariable("NUMBER", num3);
					result.Add(-keyValuePair.Value, textObject, null);
				}
			}
			return result;
		}

		// Token: 0x06004718 RID: 18200 RVA: 0x00163A08 File Offset: 0x00161C08
		int IWorkshopWarehouseCampaignBehavior.GetOutputCount(Workshop workshop)
		{
			ItemRoster warehouseRoster = this.GetWarehouseRoster(workshop.Settlement);
			int num = 0;
			List<ItemCategory> list = new List<ItemCategory>();
			foreach (WorkshopType.Production production in workshop.WorkshopType.Productions)
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Outputs)
				{
					ItemCategory item = valueTuple.Item1;
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			foreach (ItemCategory itemCategory in list)
			{
				foreach (ItemRosterElement itemRosterElement in warehouseRoster)
				{
					if (itemRosterElement.EquipmentElement.Item.ItemCategory == itemCategory)
					{
						num += itemRosterElement.Amount;
						break;
					}
				}
			}
			return num;
		}

		// Token: 0x06004719 RID: 18201 RVA: 0x00163B58 File Offset: 0x00161D58
		ExplainedNumber IWorkshopWarehouseCampaignBehavior.GetOutputDailyChange(Workshop workshop)
		{
			ExplainedNumber result = new ExplainedNumber(0f, true, null);
			ItemRoster warehouseRoster = this.GetWarehouseRoster(workshop.Settlement);
			Dictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
			foreach (WorkshopType.Production production in workshop.WorkshopType.Productions)
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Outputs)
				{
					ItemCategory item = valueTuple.Item1;
					if (item.IsTradeGood)
					{
						int item2 = valueTuple.Item2;
						float num = Campaign.Current.Models.WorkshopModel.GetEffectiveConversionSpeedOfProduction(workshop, production.ConversionSpeed, false).ResultNumber * (float)item2 * this.GetDataOfWorkshop(workshop).StockProductionInWarehouseRatio;
						float num2;
						if (!dictionary.TryGetValue(item, out num2))
						{
							dictionary.Add(item, num);
						}
						else
						{
							dictionary[item] = num2 + num;
						}
					}
				}
			}
			foreach (KeyValuePair<ItemCategory, float> keyValuePair in dictionary)
			{
				int num3 = 0;
				foreach (ItemRosterElement itemRosterElement in warehouseRoster)
				{
					if (itemRosterElement.EquipmentElement.Item.ItemCategory == keyValuePair.Key)
					{
						num3 += itemRosterElement.Amount;
					}
				}
				TextObject textObject = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null);
				textObject.SetTextVariable("RANK", keyValuePair.Key.GetName());
				textObject.SetTextVariable("NUMBER", num3);
				result.Add(keyValuePair.Value, textObject, null);
			}
			return result;
		}

		// Token: 0x0600471A RID: 18202 RVA: 0x00163D78 File Offset: 0x00161F78
		bool IWorkshopWarehouseCampaignBehavior.IsRawMaterialsSufficientInTownMarket(Workshop workshop)
		{
			for (int i = 0; i < workshop.WorkshopType.Productions.Count; i++)
			{
				WorkshopType.Production production = workshop.WorkshopType.Productions[i];
				int num;
				if (this.DetermineItemRosterHasSufficientInputs(production, workshop.Settlement.Town.Owner.ItemRoster, workshop.Settlement.Town, out num))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600471B RID: 18203 RVA: 0x00163DE0 File Offset: 0x00161FE0
		public float GetWarehouseItemRosterWeight(Settlement settlement)
		{
			ItemRoster warehouseRoster = this.GetWarehouseRoster(settlement);
			float num = 0f;
			foreach (ItemRosterElement itemRosterElement in warehouseRoster)
			{
				num += itemRosterElement.GetRosterElementWeight();
			}
			return num;
		}

		// Token: 0x0600471C RID: 18204 RVA: 0x00163E38 File Offset: 0x00162038
		private bool TickOneProductionCycleForPlayerWorkshop(WorkshopType.Production production, Workshop workshop)
		{
			bool flag = false;
			int inputMaterialCost = 0;
			Town town = workshop.Settlement.Town;
			WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop = this.GetDataOfWorkshop(workshop);
			bool flag2 = dataOfWorkshop.IsGettingInputsFromWarehouse;
			if (flag2)
			{
				flag = this.DetermineItemRosterHasSufficientInputs(production, this.GetWarehouseRoster(workshop.Settlement), town, out inputMaterialCost);
				if (flag)
				{
					inputMaterialCost = 0;
				}
				else
				{
					flag2 = false;
				}
			}
			if (!flag)
			{
				flag = this.DetermineItemRosterHasSufficientInputs(production, town.Owner.ItemRoster, town, out inputMaterialCost);
			}
			if (flag)
			{
				int outputIncome;
				List<EquipmentElement> itemsToProduce = this.GetItemsToProduce(production, workshop, out outputIncome);
				bool flag3;
				if (!production.Inputs.Any((ValueTuple<ItemCategory, int> x) => !x.Item1.IsTradeGood))
				{
					flag3 = !production.Outputs.Any((ValueTuple<ItemCategory, int> x) => !x.Item1.IsTradeGood);
				}
				else
				{
					flag3 = false;
				}
				bool effectCapital = flag3;
				float num = dataOfWorkshop.StockProductionInWarehouseRatio;
				bool allOutputsWillBeSentToWarehouse = num.ApproximatelyEqualsTo(1f, 1E-05f);
				if (this.CanPlayerWorkshopProduceThisCycle(production, workshop, inputMaterialCost, outputIncome, effectCapital, allOutputsWillBeSentToWarehouse))
				{
					Dictionary<ItemObject, int> dictionary = new Dictionary<ItemObject, int>();
					foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
					{
						if (flag2)
						{
							this.ConsumeInputFromWarehouse(valueTuple.Item1, valueTuple.Item2, workshop);
						}
						else
						{
							this.ConsumeInputFromTownMarket(valueTuple.Item1, valueTuple.Item2, town, workshop, effectCapital);
						}
					}
					foreach (EquipmentElement equipmentElement in itemsToProduce)
					{
						WorkshopsCampaignBehavior.WorkshopData dataOfWorkshop2 = this.GetDataOfWorkshop(workshop);
						if (equipmentElement.Item.IsTradeGood && this.CanItemFitInWarehouse(workshop.Settlement, equipmentElement))
						{
							this.AddOutputProgressForWarehouse(workshop, num);
							if (dataOfWorkshop2.ProductionProgressForWarehouse >= 1f)
							{
								this.ProduceAnOutputToWarehouse(equipmentElement, workshop);
								this.AddOutputProgressForWarehouse(workshop, -1f);
								if (dictionary.ContainsKey(equipmentElement.Item))
								{
									Dictionary<ItemObject, int> dictionary2 = dictionary;
									ItemObject item = equipmentElement.Item;
									int num2 = dictionary2[item];
									dictionary2[item] = num2 + 1;
								}
								else
								{
									dictionary.Add(equipmentElement.Item, 1);
								}
							}
						}
						else
						{
							num = 0f;
						}
						this.AddOutputProgressForTown(workshop, 1f - num);
						if (dataOfWorkshop2.ProductionProgressForTown >= 1f)
						{
							this.ProduceAnOutputToTown(equipmentElement, workshop, effectCapital);
							SkillLevelingManager.OnProductionProducedToWarehouse(equipmentElement);
							this.AddOutputProgressForTown(workshop, -1f);
							if (dictionary.ContainsKey(equipmentElement.Item))
							{
								Dictionary<ItemObject, int> dictionary3 = dictionary;
								ItemObject item = equipmentElement.Item;
								int num2 = dictionary3[item];
								dictionary3[item] = num2 + 1;
							}
							else
							{
								dictionary.Add(equipmentElement.Item, 1);
							}
						}
					}
					foreach (KeyValuePair<ItemObject, int> keyValuePair in dictionary)
					{
						ItemObject key = keyValuePair.Key;
						int value = keyValuePair.Value;
						CampaignEventDispatcher.Instance.OnItemProduced(key, workshop.Settlement, value);
					}
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600471D RID: 18205 RVA: 0x001641A0 File Offset: 0x001623A0
		private void ProduceAnOutputToWarehouse(EquipmentElement outputItem, Workshop workshop)
		{
			this.GetWarehouseRoster(workshop.Settlement).AddToCounts(outputItem, 1);
		}

		// Token: 0x0600471E RID: 18206 RVA: 0x001641B8 File Offset: 0x001623B8
		private void ConsumeInputFromWarehouse(ItemCategory productionInput, int inputCount, Workshop workshop)
		{
			ItemRoster warehouseRoster = this.GetWarehouseRoster(workshop.Settlement);
			int num = inputCount;
			for (int i = 0; i < warehouseRoster.Count; i++)
			{
				if (num == 0)
				{
					return;
				}
				ItemObject itemAtIndex = warehouseRoster.GetItemAtIndex(i);
				if (itemAtIndex.ItemCategory == productionInput)
				{
					int elementNumber = warehouseRoster.GetElementNumber(i);
					int num2 = MathF.Min(num, elementNumber);
					num -= num2;
					warehouseRoster.AddToCounts(itemAtIndex, -inputCount);
					CampaignEventDispatcher.Instance.OnItemConsumed(itemAtIndex, workshop.Settlement, inputCount);
				}
			}
		}

		// Token: 0x0600471F RID: 18207 RVA: 0x00164230 File Offset: 0x00162430
		private bool CanPlayerWorkshopProduceThisCycle(WorkshopType.Production production, Workshop workshop, int inputMaterialCost, int outputIncome, bool effectCapital, bool allOutputsWillBeSentToWarehouse)
		{
			float num = (workshop.WorkshopType.IsHidden ? ((float)inputMaterialCost) : ((float)inputMaterialCost + 200f / production.ConversionSpeed));
			if (Campaign.Current.GameStarted && (float)outputIncome <= num)
			{
				return false;
			}
			if (workshop.Capital < inputMaterialCost)
			{
				return false;
			}
			if (effectCapital)
			{
				bool flag = workshop.Settlement.Town.Gold >= outputIncome;
				bool flag2 = !this.IsWarehouseAtLimit(workshop.Settlement);
				if (!flag && (!allOutputsWillBeSentToWarehouse || flag2))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06004720 RID: 18208 RVA: 0x001642B8 File Offset: 0x001624B8
		private void HandlePlayerWorkshopExpense(Workshop shop)
		{
			int expense = shop.Expense;
			if (shop.Capital > Campaign.Current.Models.WorkshopModel.CapitalLowLimit)
			{
				shop.ChangeGold(-expense);
				return;
			}
			if (shop.Owner.Gold >= expense)
			{
				shop.Owner.Gold -= expense;
				return;
			}
			if (shop.Capital >= expense)
			{
				shop.ChangeGold(-expense);
				return;
			}
			this.ChangeWorkshopOwnerByBankruptcy(shop);
		}

		// Token: 0x06004721 RID: 18209 RVA: 0x0016432C File Offset: 0x0016252C
		private bool TickOneProductionCycleForNotableWorkshop(WorkshopType.Production production, Workshop workshop)
		{
			Town town = workshop.Settlement.Town;
			int inputMaterialCost = 0;
			if (!this.DetermineItemRosterHasSufficientInputs(production, town.Owner.ItemRoster, town, out inputMaterialCost))
			{
				return false;
			}
			int outputIncome;
			List<EquipmentElement> itemsToProduce = this.GetItemsToProduce(production, workshop, out outputIncome);
			bool flag;
			if (!production.Inputs.Any((ValueTuple<ItemCategory, int> x) => !x.Item1.IsTradeGood))
			{
				flag = !production.Outputs.Any((ValueTuple<ItemCategory, int> x) => !x.Item1.IsTradeGood);
			}
			else
			{
				flag = false;
			}
			bool effectCapital = flag;
			if (this.CanNotableWorkshopProduceThisCycle(production, workshop, inputMaterialCost, outputIncome, effectCapital))
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
				{
					this.ConsumeInputFromTownMarket(valueTuple.Item1, valueTuple.Item2, town, workshop, effectCapital);
				}
				foreach (EquipmentElement outputItem in itemsToProduce)
				{
					this.ProduceAnOutputToTown(outputItem, workshop, effectCapital);
					CampaignEventDispatcher.Instance.OnItemProduced(outputItem.Item, workshop.Settlement, 1);
				}
				return true;
			}
			return false;
		}

		// Token: 0x06004722 RID: 18210 RVA: 0x00164494 File Offset: 0x00162694
		private bool CanNotableWorkshopProduceThisCycle(WorkshopType.Production production, Workshop workshop, int inputMaterialCost, int outputIncome, bool effectCapital)
		{
			float num = (workshop.WorkshopType.IsHidden ? ((float)inputMaterialCost) : ((float)inputMaterialCost + 200f / production.ConversionSpeed));
			return (!Campaign.Current.GameStarted || (float)outputIncome > num) && (workshop.Settlement.Town.Gold >= outputIncome || !effectCapital) && workshop.Capital >= inputMaterialCost;
		}

		// Token: 0x06004723 RID: 18211 RVA: 0x00164500 File Offset: 0x00162700
		private void HandleNotableWorkshopExpense(Workshop shop)
		{
			int expense = shop.Expense;
			if (shop.Capital >= expense)
			{
				shop.ChangeGold(-expense);
				return;
			}
			this.ChangeWorkshopOwnerByBankruptcy(shop);
		}

		// Token: 0x06004724 RID: 18212 RVA: 0x00164530 File Offset: 0x00162730
		private WorkshopsCampaignBehavior.WorkshopData GetDataOfWorkshop(Workshop workshop)
		{
			for (int i = 0; i < this._workshopData.Length; i++)
			{
				WorkshopsCampaignBehavior.WorkshopData workshopData = this._workshopData[i];
				if (workshopData != null && workshopData.Workshop == workshop)
				{
					return workshopData;
				}
			}
			return null;
		}

		// Token: 0x06004725 RID: 18213 RVA: 0x00164568 File Offset: 0x00162768
		private List<EquipmentElement> GetItemsToProduce(WorkshopType.Production production, Workshop workshop, out int income)
		{
			List<EquipmentElement> list = new List<EquipmentElement>();
			income = 0;
			for (int i = 0; i < production.Outputs.Count; i++)
			{
				int item = production.Outputs[i].Item2;
				for (int j = 0; j < item; j++)
				{
					EquipmentElement randomItem = this.GetRandomItem(production.Outputs[i].Item1, workshop.Settlement.Town);
					if (!randomItem.IsEmpty)
					{
						list.Add(randomItem);
						income += workshop.Settlement.Town.GetItemPrice(randomItem, null, true);
					}
					else
					{
						Debug.FailedAssert("Workshop produces empty items", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\WorkshopsCampaignBehavior.cs", "GetItemsToProduce", 918);
					}
				}
			}
			return list;
		}

		// Token: 0x06004726 RID: 18214 RVA: 0x00164628 File Offset: 0x00162828
		private void ProduceAnOutputToTown(EquipmentElement outputItem, Workshop workshop, bool effectCapital)
		{
			Town town = workshop.Settlement.Town;
			int itemPrice = town.GetItemPrice(outputItem, null, false);
			town.Owner.ItemRoster.AddToCounts(outputItem, 1);
			if (Campaign.Current.GameStarted && effectCapital)
			{
				int num = MathF.Min(1000, itemPrice);
				workshop.ChangeGold(num);
				town.ChangeGold(-num);
			}
		}

		// Token: 0x06004727 RID: 18215 RVA: 0x00164688 File Offset: 0x00162888
		private void ConsumeInputFromTownMarket(ItemCategory productionInput, int productionInputCount, Town town, Workshop workshop, bool effectCapital)
		{
			ItemRoster itemRoster = town.Owner.ItemRoster;
			int num = itemRoster.FindIndex((ItemObject x) => x.ItemCategory == productionInput);
			if (num >= 0)
			{
				ItemObject itemAtIndex = itemRoster.GetItemAtIndex(num);
				if (Campaign.Current.GameStarted && effectCapital)
				{
					int itemPrice = town.GetItemPrice(itemAtIndex, null, false);
					workshop.ChangeGold(-itemPrice);
					town.ChangeGold(itemPrice);
				}
				itemRoster.AddToCounts(itemAtIndex, -productionInputCount);
				CampaignEventDispatcher.Instance.OnItemConsumed(itemAtIndex, town.Owner.Settlement, productionInputCount);
			}
		}

		// Token: 0x06004728 RID: 18216 RVA: 0x0016471A File Offset: 0x0016291A
		private bool IsItemPreferredForTown(ItemObject item, Town townComponent)
		{
			return item.Culture == null || item.Culture.StringId == "neutral_culture" || item.Culture == townComponent.Culture;
		}

		// Token: 0x06004729 RID: 18217 RVA: 0x0016474C File Offset: 0x0016294C
		private bool DetermineItemRosterHasSufficientInputs(WorkshopType.Production production, ItemRoster itemRoster, Town town, out int inputMaterialCost)
		{
			List<ValueTuple<ItemCategory, int>> inputs = production.Inputs;
			inputMaterialCost = 0;
			foreach (ValueTuple<ItemCategory, int> valueTuple in inputs)
			{
				ItemCategory item = valueTuple.Item1;
				int num = valueTuple.Item2;
				for (int i = 0; i < itemRoster.Count; i++)
				{
					ItemObject itemAtIndex = itemRoster.GetItemAtIndex(i);
					if (itemAtIndex.ItemCategory == item)
					{
						int elementNumber = itemRoster.GetElementNumber(i);
						int num2 = MathF.Min(num, elementNumber);
						num -= num2;
						inputMaterialCost += town.GetItemPrice(itemAtIndex, null, false) * num2;
					}
				}
				if (num > 0)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600472A RID: 18218 RVA: 0x00164808 File Offset: 0x00162A08
		private void AddOutputProgressForWarehouse(Workshop workshop, float progressToAdd)
		{
			this.GetDataOfWorkshop(workshop).ProductionProgressForWarehouse += progressToAdd;
		}

		// Token: 0x0600472B RID: 18219 RVA: 0x0016481E File Offset: 0x00162A1E
		private void AddOutputProgressForTown(Workshop workshop, float progressToAdd)
		{
			this.GetDataOfWorkshop(workshop).ProductionProgressForTown += progressToAdd;
		}

		// Token: 0x0600472C RID: 18220 RVA: 0x00164834 File Offset: 0x00162A34
		private bool CanItemFitInWarehouse(Settlement settlement, EquipmentElement equipmentElement)
		{
			return this.GetWarehouseItemRosterWeight(settlement) + equipmentElement.Weight <= (float)Campaign.Current.Models.WorkshopModel.WarehouseCapacity;
		}

		// Token: 0x0600472D RID: 18221 RVA: 0x0016485F File Offset: 0x00162A5F
		private bool IsWarehouseAtLimit(Settlement settlement)
		{
			return this.GetWarehouseItemRosterWeight(settlement) >= (float)Campaign.Current.Models.WorkshopModel.WarehouseCapacity;
		}

		// Token: 0x0600472E RID: 18222 RVA: 0x00164884 File Offset: 0x00162A84
		private void AddNewWorkshopData(Workshop workshop)
		{
			for (int i = 0; i < this._workshopData.Length; i++)
			{
				if (this._workshopData[i] == null)
				{
					this._workshopData[i] = new WorkshopsCampaignBehavior.WorkshopData(workshop);
					return;
				}
			}
		}

		// Token: 0x0600472F RID: 18223 RVA: 0x001648C0 File Offset: 0x00162AC0
		private void RemoveWorkshopData(Workshop workshop)
		{
			for (int i = 0; i < this._workshopData.Length; i++)
			{
				WorkshopsCampaignBehavior.WorkshopData workshopData = this._workshopData[i];
				if (workshopData != null && workshopData.Workshop == workshop)
				{
					this._workshopData[i] = null;
					return;
				}
			}
		}

		// Token: 0x06004730 RID: 18224 RVA: 0x00164900 File Offset: 0x00162B00
		private ItemRoster GetWarehouseRoster(Settlement settlement)
		{
			foreach (KeyValuePair<Settlement, ItemRoster> keyValuePair in this._warehouseRosterPerSettlement)
			{
				if (keyValuePair.Key == settlement)
				{
					return keyValuePair.Value;
				}
			}
			return null;
		}

		// Token: 0x06004731 RID: 18225 RVA: 0x00164940 File Offset: 0x00162B40
		private void FillItemsInAllCategories()
		{
			foreach (ItemObject itemObject in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
			{
				if (this.IsProducable(itemObject))
				{
					ItemCategory itemCategory = itemObject.ItemCategory;
					if (itemCategory != null)
					{
						List<ItemObject> list;
						if (!this._itemsInCategory.TryGetValue(itemCategory, out list))
						{
							list = new List<ItemObject>();
							this._itemsInCategory[itemCategory] = list;
						}
						list.Add(itemObject);
					}
				}
			}
		}

		// Token: 0x06004732 RID: 18226 RVA: 0x001649D4 File Offset: 0x00162BD4
		private bool IsProducable(ItemObject item)
		{
			return !item.MultiplayerItem && !item.NotMerchandise && !item.IsCraftedByPlayer;
		}

		// Token: 0x06004733 RID: 18227 RVA: 0x001649F4 File Offset: 0x00162BF4
		private void RemoveWarehouseData(Settlement settlement)
		{
			for (int i = 0; i < this._warehouseRosterPerSettlement.Length; i++)
			{
				if (this._warehouseRosterPerSettlement[i].Key == settlement)
				{
					this._warehouseRosterPerSettlement[i] = new KeyValuePair<Settlement, ItemRoster>(null, null);
					return;
				}
			}
		}

		// Token: 0x06004734 RID: 18228 RVA: 0x00164A3C File Offset: 0x00162C3C
		private void AddNewWarehouseDataIfNeeded(Settlement settlement)
		{
			bool flag = false;
			foreach (KeyValuePair<Settlement, ItemRoster> keyValuePair in this._warehouseRosterPerSettlement)
			{
				if (keyValuePair.Key == settlement)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				for (int j = 0; j < this._warehouseRosterPerSettlement.Length; j++)
				{
					KeyValuePair<Settlement, ItemRoster> keyValuePair2 = this._warehouseRosterPerSettlement[j];
					if (keyValuePair2.Value == null)
					{
						this._warehouseRosterPerSettlement[j] = new KeyValuePair<Settlement, ItemRoster>(settlement, new ItemRoster());
						return;
					}
				}
			}
		}

		// Token: 0x06004735 RID: 18229 RVA: 0x00164AC4 File Offset: 0x00162CC4
		private EquipmentElement GetRandomItem(ItemCategory itemGroupBase, Town townComponent)
		{
			EquipmentElement randomItemAux = this.GetRandomItemAux(itemGroupBase, townComponent);
			if (randomItemAux.Item != null)
			{
				return randomItemAux;
			}
			return this.GetRandomItemAux(itemGroupBase, null);
		}

		// Token: 0x06004736 RID: 18230 RVA: 0x00164AF0 File Offset: 0x00162CF0
		private EquipmentElement GetRandomItemAux(ItemCategory itemGroupBase, Town townComponent = null)
		{
			ItemObject itemObject = null;
			ItemModifier itemModifier = null;
			List<ValueTuple<ItemObject, float>> list = new List<ValueTuple<ItemObject, float>>();
			List<ItemObject> list2;
			if (this._itemsInCategory.TryGetValue(itemGroupBase, out list2))
			{
				foreach (ItemObject itemObject2 in list2)
				{
					if ((townComponent == null || this.IsItemPreferredForTown(itemObject2, townComponent)) && itemObject2.ItemCategory == itemGroupBase)
					{
						float item = 1f / ((float)MathF.Max(100, itemObject2.Value) + 100f);
						list.Add(new ValueTuple<ItemObject, float>(itemObject2, item));
					}
				}
				itemObject = MBRandom.ChooseWeighted<ItemObject>(list);
				ItemModifierGroup itemModifierGroup;
				if (itemObject == null)
				{
					itemModifierGroup = null;
				}
				else
				{
					ItemComponent itemComponent = itemObject.ItemComponent;
					itemModifierGroup = ((itemComponent != null) ? itemComponent.ItemModifierGroup : null);
				}
				ItemModifierGroup itemModifierGroup2 = itemModifierGroup;
				if (itemModifierGroup2 != null)
				{
					itemModifier = itemModifierGroup2.GetRandomItemModifierProductionScoreBased();
				}
			}
			return new EquipmentElement(itemObject, itemModifier, null, false);
		}

		// Token: 0x06004737 RID: 18231 RVA: 0x00164BD0 File Offset: 0x00162DD0
		private void TransferPlayerWorkshopsIfNeeded()
		{
			int count = Hero.MainHero.OwnedWorkshops.Count;
			List<Workshop> list = Hero.MainHero.OwnedWorkshops.ToList<Workshop>();
			for (int i = 0; i < count; i++)
			{
				Workshop workshop = list[i];
				if (workshop.Settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
					if (notableOwnerForWorkshop != null)
					{
						WorkshopType workshopType = this.DecideBestWorkshopType(workshop.Settlement, false, workshop.WorkshopType);
						ChangeOwnerOfWorkshopAction.ApplyByWar(workshop, notableOwnerForWorkshop, workshopType);
					}
				}
			}
		}

		// Token: 0x06004738 RID: 18232 RVA: 0x00164C68 File Offset: 0x00162E68
		private void ChangeWorkshopOwnerByBankruptcy(Workshop workshop)
		{
			int costForNotable = Campaign.Current.Models.WorkshopModel.GetCostForNotable(workshop);
			Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
			WorkshopType workshopType = this.DecideBestWorkshopType(workshop.Settlement, false, workshop.WorkshopType);
			ChangeOwnerOfWorkshopAction.ApplyByBankruptcy(workshop, notableOwnerForWorkshop, workshopType, costForNotable);
		}

		// Token: 0x06004739 RID: 18233 RVA: 0x00164CBE File Offset: 0x00162EBE
		private void HandleDailyExpense(Workshop shop)
		{
			if (!shop.WorkshopType.IsHidden)
			{
				if (shop.Owner != Hero.MainHero)
				{
					this.HandleNotableWorkshopExpense(shop);
					return;
				}
				this.HandlePlayerWorkshopExpense(shop);
			}
		}

		// Token: 0x0600473A RID: 18234 RVA: 0x00164CEC File Offset: 0x00162EEC
		private float FindTotalInputDensityScore(Settlement settlement, WorkshopType workshopType, IDictionary<ItemCategory, float> productionDict, bool atGameStart)
		{
			float num = 0f;
			for (int i = 0; i < settlement.Town.Workshops.Length; i++)
			{
				Workshop workshop = settlement.Town.Workshops[i];
				if (workshop.WorkshopType != null && !workshop.WorkshopType.IsHidden)
				{
					if (workshop.WorkshopType == workshopType)
					{
						num += 1f;
					}
					else
					{
						ValueTuple<float, float> inputOutputSimilarityForWorkshopTypes = this.GetInputOutputSimilarityForWorkshopTypes(workshopType, workshop.WorkshopType);
						float item = inputOutputSimilarityForWorkshopTypes.Item1;
						float item2 = inputOutputSimilarityForWorkshopTypes.Item2;
						num += item;
						num += item2;
					}
				}
			}
			float num2 = 0.01f;
			float num3 = 0f;
			foreach (WorkshopType.Production production in workshopType.Productions)
			{
				bool flag = false;
				using (List<ValueTuple<ItemCategory, int>>.Enumerator enumerator2 = production.Outputs.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.Item1.IsTradeGood)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
					{
						ItemCategory item3 = valueTuple.Item1;
						int item4 = valueTuple.Item2;
						float num4;
						if (productionDict.TryGetValue(item3, out num4))
						{
							num2 += num4 / (production.ConversionSpeed * (float)item4);
						}
						if (!atGameStart)
						{
							float priceFactor = settlement.Town.MarketData.GetPriceFactor(item3);
							num3 += Math.Max(0f, 1f - priceFactor);
						}
					}
				}
			}
			float num5 = 1f + num * 6f;
			num2 *= (float)workshopType.Frequency * (1f / (float)Math.Pow((double)num5, 3.0));
			num2 += num3;
			num2 = MathF.Pow(num2, 0.6f);
			return num2;
		}

		// Token: 0x0600473B RID: 18235 RVA: 0x00164F00 File Offset: 0x00163100
		private ValueTuple<float, float> GetInputOutputSimilarityForWorkshopTypes(WorkshopType workshop, WorkshopType otherWorkshop)
		{
			List<ItemCategory> list = new List<ItemCategory>();
			List<ItemCategory> list2 = new List<ItemCategory>();
			ValueTuple<Dictionary<ItemCategory, float>, Dictionary<ItemCategory, float>> inputOutputProductionForWorkshop = this.GetInputOutputProductionForWorkshop(workshop, ref list2, ref list);
			Dictionary<ItemCategory, float> item = inputOutputProductionForWorkshop.Item1;
			Dictionary<ItemCategory, float> item2 = inputOutputProductionForWorkshop.Item2;
			ValueTuple<Dictionary<ItemCategory, float>, Dictionary<ItemCategory, float>> inputOutputProductionForWorkshop2 = this.GetInputOutputProductionForWorkshop(otherWorkshop, ref list2, ref list);
			Dictionary<ItemCategory, float> item3 = inputOutputProductionForWorkshop2.Item1;
			Dictionary<ItemCategory, float> item4 = inputOutputProductionForWorkshop2.Item2;
			float num = item.SumQ((KeyValuePair<ItemCategory, float> x) => x.Value);
			float num2 = item3.SumQ((KeyValuePair<ItemCategory, float> x) => x.Value);
			float num3 = 0f;
			foreach (ItemCategory key in list2)
			{
				if (item.ContainsKey(key) && item3.ContainsKey(key))
				{
					float val = item[key] / num;
					float val2 = item3[key] / num2;
					num3 += Math.Min(val, val2);
				}
			}
			float num4 = item2.SumQ((KeyValuePair<ItemCategory, float> x) => x.Value);
			float num5 = item4.SumQ((KeyValuePair<ItemCategory, float> x) => x.Value);
			float num6 = 0f;
			foreach (ItemCategory key2 in list)
			{
				if (item2.ContainsKey(key2) && item4.ContainsKey(key2))
				{
					float val3 = item2[key2] / num4;
					float val4 = item4[key2] / num5;
					num6 += Math.Min(val3, val4);
				}
			}
			return new ValueTuple<float, float>(num3, num6);
		}

		// Token: 0x0600473C RID: 18236 RVA: 0x001650F0 File Offset: 0x001632F0
		private ValueTuple<Dictionary<ItemCategory, float>, Dictionary<ItemCategory, float>> GetInputOutputProductionForWorkshop(WorkshopType workshop, ref List<ItemCategory> allInputItems, ref List<ItemCategory> allOutputItems)
		{
			Dictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
			Dictionary<ItemCategory, float> dictionary2 = new Dictionary<ItemCategory, float>();
			foreach (WorkshopType.Production production in workshop.Productions)
			{
				foreach (ValueTuple<ItemCategory, int> valueTuple in production.Inputs)
				{
					if (valueTuple.Item1.IsTradeGood)
					{
						if (dictionary.ContainsKey(valueTuple.Item1))
						{
							Dictionary<ItemCategory, float> dictionary3 = dictionary;
							ItemCategory item = valueTuple.Item1;
							dictionary3[item] += (float)valueTuple.Item2 * production.ConversionSpeed;
						}
						else
						{
							dictionary.Add(valueTuple.Item1, (float)valueTuple.Item2 * production.ConversionSpeed);
						}
						if (!allInputItems.Contains(valueTuple.Item1))
						{
							allInputItems.Add(valueTuple.Item1);
						}
					}
				}
				foreach (ValueTuple<ItemCategory, int> valueTuple2 in production.Outputs)
				{
					if (valueTuple2.Item1.IsTradeGood)
					{
						if (dictionary2.ContainsKey(valueTuple2.Item1))
						{
							Dictionary<ItemCategory, float> dictionary3 = dictionary2;
							ItemCategory item = valueTuple2.Item1;
							dictionary3[item] += (float)valueTuple2.Item2 * production.ConversionSpeed;
						}
						else
						{
							dictionary2.Add(valueTuple2.Item1, (float)valueTuple2.Item2 * production.ConversionSpeed);
						}
						if (!allOutputItems.Contains(valueTuple2.Item1))
						{
							allOutputItems.Add(valueTuple2.Item1);
						}
					}
				}
			}
			return new ValueTuple<Dictionary<ItemCategory, float>, Dictionary<ItemCategory, float>>(dictionary, dictionary2);
		}

		// Token: 0x0600473D RID: 18237 RVA: 0x00165314 File Offset: 0x00163514
		private void BuildWorkshopForHeroAtGameStart(Hero ownerHero)
		{
			Settlement bornSettlement = ownerHero.BornSettlement;
			WorkshopType workshopType = this.DecideBestWorkshopType(bornSettlement, true, null);
			if (workshopType != null)
			{
				int num = -1;
				for (int i = 0; i < bornSettlement.Town.Workshops.Length; i++)
				{
					if (bornSettlement.Town.Workshops[i].WorkshopType == null)
					{
						num = i;
						break;
					}
				}
				if (num >= 0)
				{
					InitializeWorkshopAction.ApplyByNewGame(bornSettlement.Town.Workshops[num], ownerHero, workshopType);
				}
			}
		}

		// Token: 0x0600473E RID: 18238 RVA: 0x00165388 File Offset: 0x00163588
		private WorkshopType DecideBestWorkshopType(Settlement currentSettlement, bool atGameStart, WorkshopType workshopToExclude = null)
		{
			IDictionary<ItemCategory, float> dictionary = new Dictionary<ItemCategory, float>();
			foreach (Village village in from x in Village.All
				where x.TradeBound == currentSettlement
				select x)
			{
				foreach (ValueTuple<ItemObject, float> valueTuple in village.VillageType.Productions)
				{
					ItemCategory itemCategory = valueTuple.Item1.ItemCategory;
					if (itemCategory != DefaultItemCategories.Grain || village.VillageType.PrimaryProduction == DefaultItems.Grain)
					{
						float item = valueTuple.Item2;
						if (itemCategory == DefaultItemCategories.Cow)
						{
							itemCategory = DefaultItemCategories.Hides;
						}
						if (itemCategory == DefaultItemCategories.Sheep)
						{
							itemCategory = DefaultItemCategories.Wool;
						}
						float num;
						if (dictionary.TryGetValue(itemCategory, out num))
						{
							dictionary[itemCategory] = num + item;
						}
						else
						{
							dictionary.Add(itemCategory, item);
						}
					}
				}
			}
			Dictionary<WorkshopType, float> dictionary2 = new Dictionary<WorkshopType, float>();
			float num2 = 0f;
			foreach (WorkshopType workshopType in WorkshopType.All)
			{
				if (!workshopType.IsHidden && (workshopToExclude == null || workshopToExclude != workshopType))
				{
					float num3 = this.FindTotalInputDensityScore(currentSettlement, workshopType, dictionary, atGameStart);
					dictionary2.Add(workshopType, num3);
					num2 += num3;
				}
			}
			float num4 = num2 * MBRandom.RandomFloat;
			WorkshopType workshopType2 = null;
			foreach (WorkshopType workshopType3 in WorkshopType.All)
			{
				if (!workshopType3.IsHidden && (workshopToExclude == null || workshopToExclude != workshopType3))
				{
					num4 -= dictionary2[workshopType3];
					if (num4 < 0f)
					{
						workshopType2 = workshopType3;
						break;
					}
				}
			}
			if (workshopType2 == null)
			{
				workshopType2 = WorkshopType.All[MBRandom.RandomInt(1, WorkshopType.All.Count)];
			}
			return workshopType2;
		}

		// Token: 0x0600473F RID: 18239 RVA: 0x001655D0 File Offset: 0x001637D0
		private void InitializeWorkshops()
		{
			foreach (Town town in Town.AllTowns)
			{
				town.InitializeWorkshops(Campaign.Current.Models.WorkshopModel.DefaultWorkshopCountInSettlement);
			}
		}

		// Token: 0x06004740 RID: 18240 RVA: 0x00165634 File Offset: 0x00163834
		private void BuildWorkshopsAtGameStart()
		{
			foreach (Town town in Town.AllTowns)
			{
				this.BuildArtisanWorkshop(town);
				for (int i = 1; i < town.Workshops.Length; i++)
				{
					Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(town.Workshops[i]);
					this.BuildWorkshopForHeroAtGameStart(notableOwnerForWorkshop);
				}
			}
		}

		// Token: 0x06004741 RID: 18241 RVA: 0x001656C0 File Offset: 0x001638C0
		private void BuildArtisanWorkshop(Town town)
		{
			Hero hero = town.Settlement.Notables.FirstOrDefault((Hero x) => x.IsArtisan);
			if (hero == null)
			{
				hero = town.Settlement.Notables.FirstOrDefault<Hero>();
			}
			if (hero != null)
			{
				WorkshopType type = WorkshopType.Find("artisans");
				town.Workshops[0].InitializeWorkshop(hero, type);
			}
		}

		// Token: 0x06004742 RID: 18242 RVA: 0x00165730 File Offset: 0x00163930
		private void RunTownShopsAtGameStart()
		{
			foreach (Town town in Town.AllTowns)
			{
				foreach (Workshop workshop in town.Workshops)
				{
					this.RunTownWorkshop(town, workshop);
				}
			}
		}

		// Token: 0x06004743 RID: 18243 RVA: 0x001657A0 File Offset: 0x001639A0
		private void RunTownWorkshop(Town townComponent, Workshop workshop)
		{
			WorkshopType workshopType = workshop.WorkshopType;
			bool flag = false;
			for (int i = 0; i < workshopType.Productions.Count; i++)
			{
				float num = workshop.GetProductionProgress(i);
				if (num > 1f)
				{
					num = 1f;
				}
				num += Campaign.Current.Models.WorkshopModel.GetEffectiveConversionSpeedOfProduction(workshop, workshopType.Productions[i].ConversionSpeed, false).ResultNumber;
				if (num >= 1f)
				{
					bool flag2 = true;
					while (flag2 && num >= 1f)
					{
						flag2 = ((workshop.Owner == Hero.MainHero) ? this.TickOneProductionCycleForPlayerWorkshop(workshopType.Productions[i], workshop) : this.TickOneProductionCycleForNotableWorkshop(workshopType.Productions[i], workshop));
						if (flag2)
						{
							flag = true;
						}
						num -= 1f;
					}
				}
				workshop.SetProgress(i, num);
			}
			if (flag)
			{
				workshop.UpdateLastRunTime();
			}
		}

		// Token: 0x06004744 RID: 18244 RVA: 0x00165890 File Offset: 0x00163A90
		public void TransferWarehouseToPlayerParty(Settlement settlement)
		{
			foreach (ItemRosterElement itemRosterElement in this.GetWarehouseRoster(settlement))
			{
				MobileParty.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, itemRosterElement.Amount);
			}
			this.RemoveWarehouseData(settlement);
		}

		// Token: 0x040013BF RID: 5055
		private KeyValuePair<Settlement, ItemRoster>[] _warehouseRosterPerSettlement;

		// Token: 0x040013C0 RID: 5056
		private WorkshopsCampaignBehavior.WorkshopData[] _workshopData;

		// Token: 0x040013C1 RID: 5057
		private readonly Dictionary<ItemCategory, List<ItemObject>> _itemsInCategory = new Dictionary<ItemCategory, List<ItemObject>>();

		// Token: 0x02000868 RID: 2152
		public class WorkshopsCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006763 RID: 26467 RVA: 0x001C3960 File Offset: 0x001C1B60
			public WorkshopsCampaignBehaviorTypeDefiner()
				: base(155828)
			{
			}

			// Token: 0x06006764 RID: 26468 RVA: 0x001C396D File Offset: 0x001C1B6D
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(WorkshopsCampaignBehavior.WorkshopData), 10, null);
			}

			// Token: 0x06006765 RID: 26469 RVA: 0x001C3982 File Offset: 0x001C1B82
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<Workshop, WorkshopsCampaignBehavior.WorkshopData>));
				base.ConstructContainerDefinition(typeof(WorkshopsCampaignBehavior.WorkshopData[]));
			}
		}

		// Token: 0x02000869 RID: 2153
		internal class WorkshopData
		{
			// Token: 0x06006766 RID: 26470 RVA: 0x001C39A4 File Offset: 0x001C1BA4
			public WorkshopData(Workshop workshop)
			{
				this.Workshop = workshop;
			}

			// Token: 0x06006767 RID: 26471 RVA: 0x001C39B3 File Offset: 0x001C1BB3
			public override string ToString()
			{
				return this.Workshop.WorkshopType.ToString() + " in " + this.Workshop.Settlement.GetName();
			}

			// Token: 0x06006768 RID: 26472 RVA: 0x001C39DF File Offset: 0x001C1BDF
			internal static void AutoGeneratedStaticCollectObjectsWorkshopData(object o, List<object> collectedObjects)
			{
				((WorkshopsCampaignBehavior.WorkshopData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06006769 RID: 26473 RVA: 0x001C39ED File Offset: 0x001C1BED
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Workshop);
			}

			// Token: 0x0600676A RID: 26474 RVA: 0x001C39FB File Offset: 0x001C1BFB
			internal static object AutoGeneratedGetMemberValueWorkshop(object o)
			{
				return ((WorkshopsCampaignBehavior.WorkshopData)o).Workshop;
			}

			// Token: 0x0600676B RID: 26475 RVA: 0x001C3A08 File Offset: 0x001C1C08
			internal static object AutoGeneratedGetMemberValueIsGettingInputsFromWarehouse(object o)
			{
				return ((WorkshopsCampaignBehavior.WorkshopData)o).IsGettingInputsFromWarehouse;
			}

			// Token: 0x0600676C RID: 26476 RVA: 0x001C3A1A File Offset: 0x001C1C1A
			internal static object AutoGeneratedGetMemberValueProductionProgressForWarehouse(object o)
			{
				return ((WorkshopsCampaignBehavior.WorkshopData)o).ProductionProgressForWarehouse;
			}

			// Token: 0x0600676D RID: 26477 RVA: 0x001C3A2C File Offset: 0x001C1C2C
			internal static object AutoGeneratedGetMemberValueProductionProgressForTown(object o)
			{
				return ((WorkshopsCampaignBehavior.WorkshopData)o).ProductionProgressForTown;
			}

			// Token: 0x0600676E RID: 26478 RVA: 0x001C3A3E File Offset: 0x001C1C3E
			internal static object AutoGeneratedGetMemberValueStockProductionInWarehouseRatio(object o)
			{
				return ((WorkshopsCampaignBehavior.WorkshopData)o).StockProductionInWarehouseRatio;
			}

			// Token: 0x040023BA RID: 9146
			[SaveableField(1)]
			public Workshop Workshop;

			// Token: 0x040023BB RID: 9147
			[SaveableField(2)]
			public bool IsGettingInputsFromWarehouse;

			// Token: 0x040023BC RID: 9148
			[SaveableField(3)]
			public float ProductionProgressForWarehouse;

			// Token: 0x040023BD RID: 9149
			[SaveableField(4)]
			public float ProductionProgressForTown;

			// Token: 0x040023BE RID: 9150
			[SaveableField(5)]
			public float StockProductionInWarehouseRatio;
		}
	}
}
