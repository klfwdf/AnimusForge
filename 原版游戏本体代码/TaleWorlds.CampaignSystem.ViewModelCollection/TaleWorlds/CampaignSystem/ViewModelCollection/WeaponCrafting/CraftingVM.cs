using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000FE RID: 254
	public class CraftingVM : ViewModel
	{
		// Token: 0x060016A4 RID: 5796 RVA: 0x00057504 File Offset: 0x00055704
		public CraftingVM(Crafting crafting, Action onClose, Action resetCamera, Action onWeaponCrafted, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
		{
			this._crafting = crafting;
			this._onClose = onClose;
			this._resetCamera = resetCamera;
			this._onWeaponCrafted = onWeaponCrafted;
			this._craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			this._getItemUsageSetFlags = getItemUsageSetFlags;
			this.AvailableCharactersForSmithing = new MBBindingList<CraftingAvailableHeroItemVM>();
			this.MainActionHint = new BasicTooltipViewModel();
			this.TutorialNotification = new ElementNotificationVM();
			this.CameraControlKeys = new MBBindingList<InputKeyItemVM>();
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				IEnumerable<Hero> availableHeroesForCrafting = CraftingHelper.GetAvailableHeroesForCrafting();
				Hero activeCraftingHero = this._craftingBehavior.GetActiveCraftingHero();
				foreach (Hero hero in availableHeroesForCrafting)
				{
					CraftingAvailableHeroItemVM craftingAvailableHeroItemVM = new CraftingAvailableHeroItemVM(hero, new Action<CraftingAvailableHeroItemVM>(this.UpdateCraftingHero));
					this.AvailableCharactersForSmithing.Add(craftingAvailableHeroItemVM);
					if (hero == activeCraftingHero)
					{
						this.CurrentCraftingHero = craftingAvailableHeroItemVM;
					}
				}
				if (this.CurrentCraftingHero == null)
				{
					this.CurrentCraftingHero = this.AvailableCharactersForSmithing.FirstOrDefault<CraftingAvailableHeroItemVM>();
				}
			}
			else
			{
				this.CurrentCraftingHero = new CraftingAvailableHeroItemVM(Hero.MainHero, new Action<CraftingAvailableHeroItemVM>(this.UpdateCraftingHero));
			}
			this.UpdateCurrentMaterialsAvailable();
			this.Smelting = new SmeltingVM(new Action(this.OnSmeltItemSelection), new Action(this.UpdateAll));
			this.Refinement = new RefinementVM(new Action(this.OnRefinementSelectionChange), new Func<CraftingAvailableHeroItemVM>(this.GetCurrentCraftingHero));
			this.WeaponDesign = new WeaponDesignVM(this._crafting, this._craftingBehavior, new Action(this.OnRequireUpdateFromWeaponDesign), this._onWeaponCrafted, new Func<CraftingAvailableHeroItemVM>(this.GetCurrentCraftingHero), new Action<CraftingOrder>(this.RefreshHeroAvailabilities), this._getItemUsageSetFlags);
			this.CraftingHeroPopup = new CraftingHeroPopupVM(new Func<MBBindingList<CraftingAvailableHeroItemVM>>(this.GetCraftingHeroes));
			this.UpdateCraftingPerks();
			this.ExecuteSwitchToCrafting();
			this.RefreshValues();
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x060016A5 RID: 5797 RVA: 0x00057708 File Offset: 0x00055908
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_exit", null).ToString();
			this.ResetCameraHint = new HintViewModel(GameTexts.FindText("str_reset_camera", null), null);
			this.CraftingHint = new HintViewModel(GameTexts.FindText("str_crafting", null), null);
			this.RefiningHint = new HintViewModel(GameTexts.FindText("str_refining", null), null);
			this.SmeltingHint = new HintViewModel(GameTexts.FindText("str_smelting", null), null);
			this.RefinementText = GameTexts.FindText("str_crafting_category_refinement", null).ToString();
			this.CraftingText = GameTexts.FindText("str_crafting_category_crafting", null).ToString();
			this.SmeltingText = GameTexts.FindText("str_crafting_category_smelting", null).ToString();
			this.SelectItemToSmeltText = new TextObject("{=rUeWBOOi}Select an item to smelt", null).ToString();
			this.SelectItemToRefineText = new TextObject("{=BqLsZhhr}Select an item to refine", null).ToString();
			this.TutorialNotification.RefreshValues();
			this._availableCharactersForSmithing.ApplyActionOnAllItems(delegate(CraftingAvailableHeroItemVM x)
			{
				x.RefreshValues();
			});
			this._playerCurrentMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM x)
			{
				x.RefreshValues();
			});
			CraftingAvailableHeroItemVM currentCraftingHero = this._currentCraftingHero;
			if (currentCraftingHero != null)
			{
				currentCraftingHero.RefreshValues();
			}
			this.CraftingHeroPopup.RefreshValues();
		}

		// Token: 0x060016A6 RID: 5798 RVA: 0x0005788C File Offset: 0x00055A8C
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.WeaponDesign.OnFinalize();
			this.CraftingHeroPopup.OnFinalize();
			InputKeyItemVM confirmInputKey = this.ConfirmInputKey;
			if (confirmInputKey != null)
			{
				confirmInputKey.OnFinalize();
			}
			InputKeyItemVM exitInputKey = this.ExitInputKey;
			if (exitInputKey != null)
			{
				exitInputKey.OnFinalize();
			}
			InputKeyItemVM previousTabInputKey = this.PreviousTabInputKey;
			if (previousTabInputKey != null)
			{
				previousTabInputKey.OnFinalize();
			}
			InputKeyItemVM nextTabInputKey = this.NextTabInputKey;
			if (nextTabInputKey != null)
			{
				nextTabInputKey.OnFinalize();
			}
			foreach (InputKeyItemVM inputKeyItemVM in this.CameraControlKeys)
			{
				if (inputKeyItemVM != null)
				{
					inputKeyItemVM.OnFinalize();
				}
			}
			Game game = Game.Current;
			if (game == null)
			{
				return;
			}
			EventManager eventManager = game.EventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x060016A7 RID: 5799 RVA: 0x00057964 File Offset: 0x00055B64
		private void OnRequireUpdateFromWeaponDesign()
		{
			CraftingVM.OnItemRefreshedDelegate onItemRefreshed = this.OnItemRefreshed;
			if (onItemRefreshed != null)
			{
				onItemRefreshed(true);
			}
			this.UpdateAll();
		}

		// Token: 0x060016A8 RID: 5800 RVA: 0x0005797E File Offset: 0x00055B7E
		public void OnCraftingLogicRefreshed(Crafting newCraftingLogic)
		{
			this._crafting = newCraftingLogic;
			this.WeaponDesign.OnCraftingLogicRefreshed(newCraftingLogic);
		}

		// Token: 0x060016A9 RID: 5801 RVA: 0x00057994 File Offset: 0x00055B94
		private void UpdateCurrentMaterialCosts()
		{
			for (int i = 0; i < 9; i++)
			{
				this.PlayerCurrentMaterials[i].ResourceAmount = MobileParty.MainParty.ItemRoster.GetItemNumber(this.PlayerCurrentMaterials[i].ResourceItem);
				this.PlayerCurrentMaterials[i].ResourceChangeAmount = 0;
			}
			if (this.IsInSmeltingMode)
			{
				if (this.Smelting.CurrentSelectedItem != null)
				{
					int[] smeltingOutputForItem = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(this.Smelting.CurrentSelectedItem.EquipmentElement.Item);
					for (int j = 0; j < 9; j++)
					{
						this.PlayerCurrentMaterials[j].ResourceChangeAmount = smeltingOutputForItem[j];
					}
					return;
				}
			}
			else
			{
				if (this.IsInRefinementMode)
				{
					RefinementActionItemVM currentSelectedAction = this.Refinement.CurrentSelectedAction;
					if (currentSelectedAction == null)
					{
						return;
					}
					Crafting.RefiningFormula refineFormula = currentSelectedAction.RefineFormula;
					SmithingModel smithingModel = Campaign.Current.Models.SmithingModel;
					for (int k = 0; k < 9; k++)
					{
						this.PlayerCurrentMaterials[k].ResourceChangeAmount = 0;
						if (smithingModel.GetCraftingMaterialItem(refineFormula.Input1) == this.PlayerCurrentMaterials[k].ResourceItem)
						{
							this.PlayerCurrentMaterials[k].ResourceChangeAmount -= refineFormula.Input1Count;
						}
						else if (smithingModel.GetCraftingMaterialItem(refineFormula.Input2) == this.PlayerCurrentMaterials[k].ResourceItem)
						{
							this.PlayerCurrentMaterials[k].ResourceChangeAmount -= refineFormula.Input2Count;
						}
						else if (smithingModel.GetCraftingMaterialItem(refineFormula.Output) == this.PlayerCurrentMaterials[k].ResourceItem)
						{
							this.PlayerCurrentMaterials[k].ResourceChangeAmount += refineFormula.OutputCount;
						}
						else if (smithingModel.GetCraftingMaterialItem(refineFormula.Output2) == this.PlayerCurrentMaterials[k].ResourceItem)
						{
							this.PlayerCurrentMaterials[k].ResourceChangeAmount += refineFormula.Output2Count;
						}
					}
					int[] array = new int[9];
					foreach (CraftingResourceItemVM craftingResourceItemVM in currentSelectedAction.InputMaterials)
					{
						array[(int)craftingResourceItemVM.ResourceMaterial] -= craftingResourceItemVM.ResourceAmount;
					}
					using (IEnumerator<CraftingResourceItemVM> enumerator = currentSelectedAction.OutputMaterials.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							CraftingResourceItemVM craftingResourceItemVM2 = enumerator.Current;
							array[(int)craftingResourceItemVM2.ResourceMaterial] += craftingResourceItemVM2.ResourceAmount;
						}
						return;
					}
				}
				int[] smithingCostsForWeaponDesign = Campaign.Current.Models.SmithingModel.GetSmithingCostsForWeaponDesign(this._crafting.CurrentWeaponDesign);
				for (int l = 0; l < 9; l++)
				{
					this.PlayerCurrentMaterials[l].ResourceChangeAmount = smithingCostsForWeaponDesign[l];
				}
			}
		}

		// Token: 0x060016AA RID: 5802 RVA: 0x00057CD0 File Offset: 0x00055ED0
		private void UpdateCurrentMaterialsAvailable()
		{
			if (this.PlayerCurrentMaterials == null)
			{
				this.PlayerCurrentMaterials = new MBBindingList<CraftingResourceItemVM>();
				for (int i = 0; i < 9; i++)
				{
					this.PlayerCurrentMaterials.Add(new CraftingResourceItemVM((CraftingMaterials)i, 0, 0));
				}
			}
			for (int j = 0; j < 9; j++)
			{
				ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials)j);
				this.PlayerCurrentMaterials[j].ResourceAmount = MobileParty.MainParty.ItemRoster.GetItemNumber(craftingMaterialItem);
			}
		}

		// Token: 0x060016AB RID: 5803 RVA: 0x00057D54 File Offset: 0x00055F54
		private void UpdateAll()
		{
			this.UpdateCurrentMaterialCosts();
			this.UpdateCurrentMaterialsAvailable();
			this.RefreshEnableMainAction();
			this.UpdateCraftingStamina();
			this.UpdateCraftingSkills();
			CraftingOrder order;
			if (!this.IsInCraftingMode)
			{
				order = null;
			}
			else
			{
				CraftingOrderItemVM activeCraftingOrder = this.WeaponDesign.ActiveCraftingOrder;
				order = ((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null);
			}
			this.RefreshHeroAvailabilities(order);
		}

		// Token: 0x060016AC RID: 5804 RVA: 0x00057DA8 File Offset: 0x00055FA8
		private void UpdateCraftingSkills()
		{
			foreach (CraftingAvailableHeroItemVM craftingAvailableHeroItemVM in this.AvailableCharactersForSmithing)
			{
				craftingAvailableHeroItemVM.RefreshSkills();
			}
		}

		// Token: 0x060016AD RID: 5805 RVA: 0x00057DF4 File Offset: 0x00055FF4
		private void UpdateCraftingStamina()
		{
			foreach (CraftingAvailableHeroItemVM craftingAvailableHeroItemVM in this.AvailableCharactersForSmithing)
			{
				craftingAvailableHeroItemVM.RefreshStamina();
			}
		}

		// Token: 0x060016AE RID: 5806 RVA: 0x00057E40 File Offset: 0x00056040
		private void UpdateCraftingPerks()
		{
			foreach (CraftingAvailableHeroItemVM craftingAvailableHeroItemVM in this.AvailableCharactersForSmithing)
			{
				craftingAvailableHeroItemVM.RefreshPerks();
			}
		}

		// Token: 0x060016AF RID: 5807 RVA: 0x00057E8C File Offset: 0x0005608C
		private void RefreshHeroAvailabilities(CraftingOrder order)
		{
			foreach (CraftingAvailableHeroItemVM craftingAvailableHeroItemVM in this.AvailableCharactersForSmithing)
			{
				craftingAvailableHeroItemVM.RefreshOrderAvailability(order);
			}
		}

		// Token: 0x060016B0 RID: 5808 RVA: 0x00057ED8 File Offset: 0x000560D8
		private void RefreshEnableMainAction()
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
			{
				this.IsMainActionEnabled = true;
				return;
			}
			this.IsMainActionEnabled = true;
			if (!this.HaveEnergy())
			{
				this.IsMainActionEnabled = false;
				if (this.MainActionHint != null)
				{
					this.MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=PRE5RKpp}You must rest and spend time before you can do this action.", null).ToString());
				}
			}
			else if (!this.HaveMaterialsNeeded())
			{
				this.IsMainActionEnabled = false;
				if (this.MainActionHint != null)
				{
					this.MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=gduqxfck}You don't have all required materials!", null).ToString());
				}
			}
			if (this.IsInSmeltingMode)
			{
				this.IsMainActionEnabled = this.IsMainActionEnabled && this.Smelting.IsAnyItemSelected;
				this.IsSmeltingItemSelected = this.Smelting.IsAnyItemSelected;
				if (!this.IsSmeltingItemSelected && this.MainActionHint != null)
				{
					this.MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=SzuCFlNq}No item selected.", null).ToString());
					return;
				}
			}
			else if (this.IsInRefinementMode)
			{
				this.IsMainActionEnabled = this.IsMainActionEnabled && this.Refinement.IsValidRefinementActionSelected;
				this.IsRefinementItemSelected = this.Refinement.IsValidRefinementActionSelected;
				if (!this.IsRefinementItemSelected && this.MainActionHint != null)
				{
					this.MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=SzuCFlNq}No item selected.", null).ToString());
					return;
				}
			}
			else
			{
				if (this.WeaponDesign != null)
				{
					if (!this.WeaponDesign.HaveUnlockedAllSelectedPieces())
					{
						this.IsMainActionEnabled = false;
						if (this.MainActionHint != null)
						{
							this.MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=Wir2xZIg}You haven't unlocked some of the selected pieces.", null).ToString());
						}
					}
					else if (!this.WeaponDesign.CanCompleteOrder())
					{
						this.IsMainActionEnabled = false;
						if (this.MainActionHint != null)
						{
							CraftingVM.<>c__DisplayClass21_0 CS$<>8__locals1 = new CraftingVM.<>c__DisplayClass21_0();
							CraftingVM.<>c__DisplayClass21_0 CS$<>8__locals2 = CS$<>8__locals1;
							CraftingOrderItemVM activeCraftingOrder = this.WeaponDesign.ActiveCraftingOrder;
							CS$<>8__locals2.order = ((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null);
							CS$<>8__locals1.item = this._crafting.GetCurrentCraftedItemObject(false, null);
							this.MainActionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetOrderCannotBeCompletedReasonTooltip(CS$<>8__locals1.order, CS$<>8__locals1.item));
						}
					}
				}
				if (this.IsMainActionEnabled && this.MainActionHint != null)
				{
					this.MainActionHint = new BasicTooltipViewModel();
				}
			}
		}

		// Token: 0x060016B1 RID: 5809 RVA: 0x0005814B File Offset: 0x0005634B
		private bool HaveEnergy()
		{
			CraftingAvailableHeroItemVM currentCraftingHero = this.CurrentCraftingHero;
			return ((currentCraftingHero != null) ? currentCraftingHero.Hero : null) == null || this._craftingBehavior.GetHeroCraftingStamina(this.CurrentCraftingHero.Hero) > 10;
		}

		// Token: 0x060016B2 RID: 5810 RVA: 0x0005817D File Offset: 0x0005637D
		private bool HaveMaterialsNeeded()
		{
			return !this.PlayerCurrentMaterials.Any((CraftingResourceItemVM m) => m.ResourceChangeAmount + m.ResourceAmount < 0);
		}

		// Token: 0x060016B3 RID: 5811 RVA: 0x000581AC File Offset: 0x000563AC
		public void UpdateCraftingHero(CraftingAvailableHeroItemVM newHero)
		{
			this.CurrentCraftingHero = newHero;
			CraftingHeroPopupVM craftingHeroPopup = this.CraftingHeroPopup;
			if (craftingHeroPopup != null && craftingHeroPopup.IsVisible)
			{
				this.CraftingHeroPopup.ExecuteClosePopup();
			}
			this.WeaponDesign.OnCraftingHeroChanged(newHero);
			this.Refinement.OnCraftingHeroChanged(newHero);
			this.Smelting.OnCraftingHeroChanged(newHero);
			this.RefreshEnableMainAction();
			this.UpdateCraftingSkills();
		}

		// Token: 0x060016B4 RID: 5812 RVA: 0x00058210 File Offset: 0x00056410
		[return: TupleElementNames(new string[] { "isConfirmSuccessful", "isMainActionExecuted" })]
		public ValueTuple<bool, bool> ExecuteConfirm()
		{
			CraftingHistoryVM craftingHistory = this.WeaponDesign.CraftingHistory;
			if (craftingHistory != null && craftingHistory.IsVisible)
			{
				if (this.WeaponDesign.CraftingHistory.SelectedDesign != null)
				{
					this.WeaponDesign.CraftingHistory.ExecuteDone();
					return new ValueTuple<bool, bool>(true, false);
				}
			}
			else
			{
				CraftingOrderPopupVM craftingOrderPopup = this.WeaponDesign.CraftingOrderPopup;
				if (craftingOrderPopup != null && !craftingOrderPopup.IsVisible)
				{
					WeaponClassSelectionPopupVM weaponClassSelectionPopup = this.WeaponDesign.WeaponClassSelectionPopup;
					if (weaponClassSelectionPopup != null && !weaponClassSelectionPopup.IsVisible)
					{
						CraftingHeroPopupVM craftingHeroPopup = this.CraftingHeroPopup;
						if (craftingHeroPopup != null && !craftingHeroPopup.IsVisible)
						{
							if (this.WeaponDesign.IsInFinalCraftingStage)
							{
								if (this.WeaponDesign.CraftingResultPopup.CanConfirm)
								{
									this.WeaponDesign.CraftingResultPopup.ExecuteFinalizeCrafting();
									return new ValueTuple<bool, bool>(true, false);
								}
							}
							else if (this.IsMainActionEnabled)
							{
								this.ExecuteMainAction();
								return new ValueTuple<bool, bool>(true, true);
							}
						}
					}
				}
			}
			return new ValueTuple<bool, bool>(false, false);
		}

		// Token: 0x060016B5 RID: 5813 RVA: 0x0005830C File Offset: 0x0005650C
		public void ExecuteCancel()
		{
			CraftingHistoryVM craftingHistory = this.WeaponDesign.CraftingHistory;
			if (craftingHistory != null && craftingHistory.IsVisible)
			{
				this.WeaponDesign.CraftingHistory.ExecuteCancel();
				return;
			}
			CraftingHeroPopupVM craftingHeroPopup = this.CraftingHeroPopup;
			if (craftingHeroPopup != null && craftingHeroPopup.IsVisible)
			{
				this.CraftingHeroPopup.ExecuteClosePopup();
				return;
			}
			CraftingOrderPopupVM craftingOrderPopup = this.WeaponDesign.CraftingOrderPopup;
			if (craftingOrderPopup != null && craftingOrderPopup.IsVisible)
			{
				this.WeaponDesign.CraftingOrderPopup.ExecuteCloseWithoutSelection();
				return;
			}
			WeaponClassSelectionPopupVM weaponClassSelectionPopup = this.WeaponDesign.WeaponClassSelectionPopup;
			if (weaponClassSelectionPopup != null && weaponClassSelectionPopup.IsVisible)
			{
				this.WeaponDesign.WeaponClassSelectionPopup.ExecuteClosePopup();
				return;
			}
			if (this.WeaponDesign.IsInFinalCraftingStage)
			{
				if (this.WeaponDesign.CraftingResultPopup.CanConfirm)
				{
					this.WeaponDesign.CraftingResultPopup.ExecuteFinalizeCrafting();
					return;
				}
			}
			else
			{
				this.Smelting.SaveItemLockStates();
				Game.Current.GameStateManager.PopState(0);
			}
		}

		// Token: 0x060016B6 RID: 5814 RVA: 0x00058404 File Offset: 0x00056604
		public void ExecuteMainAction()
		{
			if (this.IsInSmeltingMode)
			{
				this.Smelting.TrySmeltingSelectedItems(this.CurrentCraftingHero.Hero);
			}
			else if (this.IsInRefinementMode)
			{
				this.Refinement.ExecuteSelectedRefinement(this.CurrentCraftingHero.Hero);
			}
			else if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
			{
				CraftingState craftingState;
				if ((craftingState = GameStateManager.Current.ActiveState as CraftingState) != null)
				{
					ItemObject currentCraftedItemObject = craftingState.CraftingLogic.GetCurrentCraftedItemObject(true, null);
					ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(currentCraftedItemObject.WeaponDesign.HashedCode) ?? MBObjectManager.Instance.RegisterObject<ItemObject>(currentCraftedItemObject);
					PartyBase.MainParty.ItemRoster.AddToCounts(item, 1);
					this.WeaponDesign.IsInFinalCraftingStage = false;
				}
			}
			else
			{
				if (!this.HaveMaterialsNeeded() || !this.HaveEnergy())
				{
					return;
				}
				CraftingAvailableHeroItemVM currentCraftingHero = this.GetCurrentCraftingHero();
				Hero hero = ((currentCraftingHero != null) ? currentCraftingHero.Hero : null);
				ItemModifier craftedWeaponModifier = Campaign.Current.Models.SmithingModel.GetCraftedWeaponModifier(this._crafting.CurrentWeaponDesign, hero);
				this._craftingBehavior.SetCurrentItemModifier(craftedWeaponModifier);
				if (this.WeaponDesign.IsInOrderMode)
				{
					WeaponDesignVM weaponDesign = this.WeaponDesign;
					ICraftingCampaignBehavior craftingBehavior = this._craftingBehavior;
					Hero crafterHero = hero;
					CraftingOrderItemVM activeCraftingOrder = this.WeaponDesign.ActiveCraftingOrder;
					weaponDesign.CraftedItemObject = craftingBehavior.CreateCraftedWeaponInCraftingOrderMode(crafterHero, (activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null, this._crafting.CurrentWeaponDesign);
				}
				else
				{
					this.WeaponDesign.CraftedItemObject = this._craftingBehavior.CreateCraftedWeaponInFreeBuildMode(hero, this._crafting.CurrentWeaponDesign, this._craftingBehavior.GetCurrentItemModifier());
				}
				this.WeaponDesign.IsInFinalCraftingStage = true;
				this.WeaponDesign.CreateCraftingResultPopup();
				Action onWeaponCrafted = this._onWeaponCrafted;
				if (onWeaponCrafted != null)
				{
					onWeaponCrafted();
				}
			}
			if (!this.IsInSmeltingMode)
			{
				this.UpdateAll();
			}
		}

		// Token: 0x060016B7 RID: 5815 RVA: 0x000585D6 File Offset: 0x000567D6
		public void ExecuteResetCamera()
		{
			this._resetCamera();
		}

		// Token: 0x060016B8 RID: 5816 RVA: 0x000585E3 File Offset: 0x000567E3
		private CraftingAvailableHeroItemVM GetCurrentCraftingHero()
		{
			return this.CurrentCraftingHero;
		}

		// Token: 0x060016B9 RID: 5817 RVA: 0x000585EB File Offset: 0x000567EB
		private MBBindingList<CraftingAvailableHeroItemVM> GetCraftingHeroes()
		{
			return this.AvailableCharactersForSmithing;
		}

		// Token: 0x060016BA RID: 5818 RVA: 0x000585F3 File Offset: 0x000567F3
		public void SetConfirmInputKey(HotKey hotKey)
		{
			this.ConfirmInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x060016BB RID: 5819 RVA: 0x00058602 File Offset: 0x00056802
		public void SetExitInputKey(HotKey hotKey)
		{
			this.ExitInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x060016BC RID: 5820 RVA: 0x00058611 File Offset: 0x00056811
		public void SetPreviousTabInputKey(HotKey hotKey)
		{
			this.PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x060016BD RID: 5821 RVA: 0x00058620 File Offset: 0x00056820
		public void SetNextTabInputKey(HotKey hotKey)
		{
			this.NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x060016BE RID: 5822 RVA: 0x00058630 File Offset: 0x00056830
		public void AddCameraControlInputKey(HotKey hotKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x060016BF RID: 5823 RVA: 0x00058654 File Offset: 0x00056854
		public void AddCameraControlInputKey(GameKey gameKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x060016C0 RID: 5824 RVA: 0x00058678 File Offset: 0x00056878
		public void AddCameraControlInputKey(GameAxisKey gameAxisKey)
		{
			TextObject forcedName = GameTexts.FindText("str_key_name", "CraftingHotkeyCategory_" + gameAxisKey.Id);
			InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), forcedName, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x1700078A RID: 1930
		// (get) Token: 0x060016C1 RID: 5825 RVA: 0x000586BF File Offset: 0x000568BF
		// (set) Token: 0x060016C2 RID: 5826 RVA: 0x000586C7 File Offset: 0x000568C7
		public InputKeyItemVM ConfirmInputKey
		{
			get
			{
				return this._confirmInputKey;
			}
			set
			{
				if (value != this._confirmInputKey)
				{
					this._confirmInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ConfirmInputKey");
				}
			}
		}

		// Token: 0x1700078B RID: 1931
		// (get) Token: 0x060016C3 RID: 5827 RVA: 0x000586E5 File Offset: 0x000568E5
		// (set) Token: 0x060016C4 RID: 5828 RVA: 0x000586ED File Offset: 0x000568ED
		public InputKeyItemVM ExitInputKey
		{
			get
			{
				return this._exitInputKey;
			}
			set
			{
				if (value != this._exitInputKey)
				{
					this._exitInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ExitInputKey");
				}
			}
		}

		// Token: 0x1700078C RID: 1932
		// (get) Token: 0x060016C5 RID: 5829 RVA: 0x0005870B File Offset: 0x0005690B
		// (set) Token: 0x060016C6 RID: 5830 RVA: 0x00058713 File Offset: 0x00056913
		public InputKeyItemVM PreviousTabInputKey
		{
			get
			{
				return this._previousTabInputKey;
			}
			set
			{
				if (value != this._previousTabInputKey)
				{
					this._previousTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousTabInputKey");
				}
			}
		}

		// Token: 0x1700078D RID: 1933
		// (get) Token: 0x060016C7 RID: 5831 RVA: 0x00058731 File Offset: 0x00056931
		// (set) Token: 0x060016C8 RID: 5832 RVA: 0x00058739 File Offset: 0x00056939
		public InputKeyItemVM NextTabInputKey
		{
			get
			{
				return this._nextTabInputKey;
			}
			set
			{
				if (value != this._nextTabInputKey)
				{
					this._nextTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextTabInputKey");
				}
			}
		}

		// Token: 0x1700078E RID: 1934
		// (get) Token: 0x060016C9 RID: 5833 RVA: 0x00058757 File Offset: 0x00056957
		// (set) Token: 0x060016CA RID: 5834 RVA: 0x0005875F File Offset: 0x0005695F
		[DataSourceProperty]
		public MBBindingList<InputKeyItemVM> CameraControlKeys
		{
			get
			{
				return this._cameraControlKeys;
			}
			set
			{
				if (value != this._cameraControlKeys)
				{
					this._cameraControlKeys = value;
					base.OnPropertyChangedWithValue<MBBindingList<InputKeyItemVM>>(value, "CameraControlKeys");
				}
			}
		}

		// Token: 0x1700078F RID: 1935
		// (get) Token: 0x060016CB RID: 5835 RVA: 0x0005877D File Offset: 0x0005697D
		// (set) Token: 0x060016CC RID: 5836 RVA: 0x00058785 File Offset: 0x00056985
		public bool CanSwitchTabs
		{
			get
			{
				return this._canSwitchTabs;
			}
			set
			{
				if (value != this._canSwitchTabs)
				{
					this._canSwitchTabs = value;
					base.OnPropertyChangedWithValue(value, "CanSwitchTabs");
				}
			}
		}

		// Token: 0x17000790 RID: 1936
		// (get) Token: 0x060016CD RID: 5837 RVA: 0x000587A3 File Offset: 0x000569A3
		// (set) Token: 0x060016CE RID: 5838 RVA: 0x000587AB File Offset: 0x000569AB
		public bool AreGamepadControlHintsEnabled
		{
			get
			{
				return this._areGamepadControlHintsEnabled;
			}
			set
			{
				if (value != this._areGamepadControlHintsEnabled)
				{
					this._areGamepadControlHintsEnabled = value;
					base.OnPropertyChangedWithValue(value, "AreGamepadControlHintsEnabled");
				}
			}
		}

		// Token: 0x17000791 RID: 1937
		// (get) Token: 0x060016CF RID: 5839 RVA: 0x000587C9 File Offset: 0x000569C9
		// (set) Token: 0x060016D0 RID: 5840 RVA: 0x000587D1 File Offset: 0x000569D1
		[DataSourceProperty]
		public MBBindingList<CraftingResourceItemVM> PlayerCurrentMaterials
		{
			get
			{
				return this._playerCurrentMaterials;
			}
			set
			{
				if (value != this._playerCurrentMaterials)
				{
					this._playerCurrentMaterials = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingResourceItemVM>>(value, "PlayerCurrentMaterials");
				}
			}
		}

		// Token: 0x17000792 RID: 1938
		// (get) Token: 0x060016D1 RID: 5841 RVA: 0x000587EF File Offset: 0x000569EF
		// (set) Token: 0x060016D2 RID: 5842 RVA: 0x000587F7 File Offset: 0x000569F7
		[DataSourceProperty]
		public MBBindingList<CraftingAvailableHeroItemVM> AvailableCharactersForSmithing
		{
			get
			{
				return this._availableCharactersForSmithing;
			}
			set
			{
				if (value != this._availableCharactersForSmithing)
				{
					this._availableCharactersForSmithing = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingAvailableHeroItemVM>>(value, "AvailableCharactersForSmithing");
				}
			}
		}

		// Token: 0x17000793 RID: 1939
		// (get) Token: 0x060016D3 RID: 5843 RVA: 0x00058815 File Offset: 0x00056A15
		// (set) Token: 0x060016D4 RID: 5844 RVA: 0x00058820 File Offset: 0x00056A20
		[DataSourceProperty]
		public CraftingAvailableHeroItemVM CurrentCraftingHero
		{
			get
			{
				return this._currentCraftingHero;
			}
			set
			{
				if (value != this._currentCraftingHero)
				{
					if (this._currentCraftingHero != null)
					{
						this._currentCraftingHero.IsSelected = false;
					}
					this._currentCraftingHero = value;
					if (this._currentCraftingHero != null)
					{
						this._currentCraftingHero.IsSelected = true;
					}
					ICraftingCampaignBehavior craftingBehavior = this._craftingBehavior;
					CraftingAvailableHeroItemVM currentCraftingHero = this._currentCraftingHero;
					craftingBehavior.SetActiveCraftingHero((currentCraftingHero != null) ? currentCraftingHero.Hero : null);
					base.OnPropertyChangedWithValue<CraftingAvailableHeroItemVM>(value, "CurrentCraftingHero");
				}
			}
		}

		// Token: 0x17000794 RID: 1940
		// (get) Token: 0x060016D5 RID: 5845 RVA: 0x0005888E File Offset: 0x00056A8E
		// (set) Token: 0x060016D6 RID: 5846 RVA: 0x00058896 File Offset: 0x00056A96
		[DataSourceProperty]
		public CraftingHeroPopupVM CraftingHeroPopup
		{
			get
			{
				return this._craftingHeroPopup;
			}
			set
			{
				if (value != this._craftingHeroPopup)
				{
					this._craftingHeroPopup = value;
					base.OnPropertyChangedWithValue<CraftingHeroPopupVM>(value, "CraftingHeroPopup");
				}
			}
		}

		// Token: 0x17000795 RID: 1941
		// (get) Token: 0x060016D7 RID: 5847 RVA: 0x000588B4 File Offset: 0x00056AB4
		// (set) Token: 0x060016D8 RID: 5848 RVA: 0x000588BC File Offset: 0x00056ABC
		[DataSourceProperty]
		public string CurrentCategoryText
		{
			get
			{
				return this._currentCategoryText;
			}
			set
			{
				if (value != this._currentCategoryText)
				{
					this._currentCategoryText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCategoryText");
				}
			}
		}

		// Token: 0x17000796 RID: 1942
		// (get) Token: 0x060016D9 RID: 5849 RVA: 0x000588DF File Offset: 0x00056ADF
		// (set) Token: 0x060016DA RID: 5850 RVA: 0x000588E7 File Offset: 0x00056AE7
		[DataSourceProperty]
		public string CraftingText
		{
			get
			{
				return this._craftingText;
			}
			set
			{
				if (value != this._craftingText)
				{
					this._craftingText = value;
					base.OnPropertyChangedWithValue<string>(value, "CraftingText");
				}
			}
		}

		// Token: 0x17000797 RID: 1943
		// (get) Token: 0x060016DB RID: 5851 RVA: 0x0005890A File Offset: 0x00056B0A
		// (set) Token: 0x060016DC RID: 5852 RVA: 0x00058912 File Offset: 0x00056B12
		[DataSourceProperty]
		public string SmeltingText
		{
			get
			{
				return this._smeltingText;
			}
			set
			{
				if (value != this._smeltingText)
				{
					this._smeltingText = value;
					base.OnPropertyChangedWithValue<string>(value, "SmeltingText");
				}
			}
		}

		// Token: 0x17000798 RID: 1944
		// (get) Token: 0x060016DD RID: 5853 RVA: 0x00058935 File Offset: 0x00056B35
		// (set) Token: 0x060016DE RID: 5854 RVA: 0x0005893D File Offset: 0x00056B3D
		[DataSourceProperty]
		public string RefinementText
		{
			get
			{
				return this._refinementText;
			}
			set
			{
				if (value != this._refinementText)
				{
					this._refinementText = value;
					base.OnPropertyChangedWithValue<string>(value, "RefinementText");
				}
			}
		}

		// Token: 0x17000799 RID: 1945
		// (get) Token: 0x060016DF RID: 5855 RVA: 0x00058960 File Offset: 0x00056B60
		// (set) Token: 0x060016E0 RID: 5856 RVA: 0x00058968 File Offset: 0x00056B68
		[DataSourceProperty]
		public string MainActionText
		{
			get
			{
				return this._mainActionText;
			}
			set
			{
				if (value != this._mainActionText)
				{
					this._mainActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "MainActionText");
				}
			}
		}

		// Token: 0x1700079A RID: 1946
		// (get) Token: 0x060016E1 RID: 5857 RVA: 0x0005898B File Offset: 0x00056B8B
		// (set) Token: 0x060016E2 RID: 5858 RVA: 0x00058993 File Offset: 0x00056B93
		[DataSourceProperty]
		public bool IsMainActionEnabled
		{
			get
			{
				return this._isMainActionEnabled;
			}
			set
			{
				if (value != this._isMainActionEnabled)
				{
					this._isMainActionEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsMainActionEnabled");
				}
			}
		}

		// Token: 0x1700079B RID: 1947
		// (get) Token: 0x060016E3 RID: 5859 RVA: 0x000589B1 File Offset: 0x00056BB1
		// (set) Token: 0x060016E4 RID: 5860 RVA: 0x000589B9 File Offset: 0x00056BB9
		[DataSourceProperty]
		public int ItemValue
		{
			get
			{
				return this._itemValue;
			}
			set
			{
				if (value != this._itemValue)
				{
					this._itemValue = value;
					base.OnPropertyChangedWithValue(value, "ItemValue");
				}
			}
		}

		// Token: 0x1700079C RID: 1948
		// (get) Token: 0x060016E5 RID: 5861 RVA: 0x000589D7 File Offset: 0x00056BD7
		// (set) Token: 0x060016E6 RID: 5862 RVA: 0x000589DF File Offset: 0x00056BDF
		[DataSourceProperty]
		public HintViewModel CraftingHint
		{
			get
			{
				return this._craftingHint;
			}
			set
			{
				if (value != this._craftingHint)
				{
					this._craftingHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CraftingHint");
				}
			}
		}

		// Token: 0x1700079D RID: 1949
		// (get) Token: 0x060016E7 RID: 5863 RVA: 0x000589FD File Offset: 0x00056BFD
		// (set) Token: 0x060016E8 RID: 5864 RVA: 0x00058A05 File Offset: 0x00056C05
		[DataSourceProperty]
		public HintViewModel RefiningHint
		{
			get
			{
				return this._refiningHint;
			}
			set
			{
				if (value != this._refiningHint)
				{
					this._refiningHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RefiningHint");
				}
			}
		}

		// Token: 0x1700079E RID: 1950
		// (get) Token: 0x060016E9 RID: 5865 RVA: 0x00058A23 File Offset: 0x00056C23
		// (set) Token: 0x060016EA RID: 5866 RVA: 0x00058A2B File Offset: 0x00056C2B
		[DataSourceProperty]
		public HintViewModel SmeltingHint
		{
			get
			{
				return this._smeltingHint;
			}
			set
			{
				if (value != this._smeltingHint)
				{
					this._smeltingHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SmeltingHint");
				}
			}
		}

		// Token: 0x1700079F RID: 1951
		// (get) Token: 0x060016EB RID: 5867 RVA: 0x00058A49 File Offset: 0x00056C49
		// (set) Token: 0x060016EC RID: 5868 RVA: 0x00058A51 File Offset: 0x00056C51
		[DataSourceProperty]
		public HintViewModel ResetCameraHint
		{
			get
			{
				return this._resetCameraHint;
			}
			set
			{
				if (value != this._resetCameraHint)
				{
					this._resetCameraHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetCameraHint");
				}
			}
		}

		// Token: 0x170007A0 RID: 1952
		// (get) Token: 0x060016ED RID: 5869 RVA: 0x00058A6F File Offset: 0x00056C6F
		// (set) Token: 0x060016EE RID: 5870 RVA: 0x00058A77 File Offset: 0x00056C77
		[DataSourceProperty]
		public BasicTooltipViewModel MainActionHint
		{
			get
			{
				return this._mainActionHint;
			}
			set
			{
				if (value != this._mainActionHint)
				{
					this._mainActionHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "MainActionHint");
				}
			}
		}

		// Token: 0x170007A1 RID: 1953
		// (get) Token: 0x060016EF RID: 5871 RVA: 0x00058A95 File Offset: 0x00056C95
		// (set) Token: 0x060016F0 RID: 5872 RVA: 0x00058A9D File Offset: 0x00056C9D
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x170007A2 RID: 1954
		// (get) Token: 0x060016F1 RID: 5873 RVA: 0x00058AC0 File Offset: 0x00056CC0
		// (set) Token: 0x060016F2 RID: 5874 RVA: 0x00058AC8 File Offset: 0x00056CC8
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x060016F3 RID: 5875 RVA: 0x00058AEC File Offset: 0x00056CEC
		public void ExecuteSwitchToCrafting()
		{
			this.IsInSmeltingMode = false;
			this.IsInCraftingMode = true;
			this.IsInRefinementMode = false;
			this.CurrentCategoryText = new TextObject("{=POjDNVW3}Forging", null).ToString();
			this.MainActionText = GameTexts.FindText("str_crafting_category_crafting", null).ToString();
			CraftingVM.OnItemRefreshedDelegate onItemRefreshed = this.OnItemRefreshed;
			if (onItemRefreshed != null)
			{
				onItemRefreshed(true);
			}
			this.UpdateCurrentMaterialCosts();
			this.UpdateAll();
			WeaponDesignVM weaponDesign = this.WeaponDesign;
			if (weaponDesign == null)
			{
				return;
			}
			weaponDesign.ChangeModeIfHeroIsUnavailable();
		}

		// Token: 0x060016F4 RID: 5876 RVA: 0x00058B68 File Offset: 0x00056D68
		public void ExecuteSwitchToSmelting()
		{
			this.IsInSmeltingMode = true;
			this.IsInCraftingMode = false;
			this.IsInRefinementMode = false;
			this.CurrentCategoryText = new TextObject("{=4cU98rkg}Smelting", null).ToString();
			this.MainActionText = GameTexts.FindText("str_crafting_category_smelting", null).ToString();
			CraftingVM.OnItemRefreshedDelegate onItemRefreshed = this.OnItemRefreshed;
			if (onItemRefreshed != null)
			{
				onItemRefreshed(false);
			}
			this.UpdateCurrentMaterialCosts();
			this.Smelting.RefreshList();
			this.UpdateAll();
		}

		// Token: 0x060016F5 RID: 5877 RVA: 0x00058BE0 File Offset: 0x00056DE0
		public void ExecuteSwitchToRefinement()
		{
			this.IsInSmeltingMode = false;
			this.IsInCraftingMode = false;
			this.IsInRefinementMode = true;
			this.CurrentCategoryText = new TextObject("{=p7raHA9x}Refinement", null).ToString();
			this.MainActionText = GameTexts.FindText("str_crafting_category_refinement", null).ToString();
			CraftingVM.OnItemRefreshedDelegate onItemRefreshed = this.OnItemRefreshed;
			if (onItemRefreshed != null)
			{
				onItemRefreshed(false);
			}
			this.UpdateCurrentMaterialCosts();
			this.Refinement.RefreshRefinementActionsList(this.CurrentCraftingHero.Hero);
			this.UpdateAll();
		}

		// Token: 0x060016F6 RID: 5878 RVA: 0x00058C62 File Offset: 0x00056E62
		private void OnRefinementSelectionChange()
		{
			this.UpdateCurrentMaterialCosts();
			this.RefreshEnableMainAction();
		}

		// Token: 0x060016F7 RID: 5879 RVA: 0x00058C70 File Offset: 0x00056E70
		private void OnSmeltItemSelection()
		{
			this.UpdateCurrentMaterialCosts();
			this.RefreshEnableMainAction();
		}

		// Token: 0x060016F8 RID: 5880 RVA: 0x00058C7E File Offset: 0x00056E7E
		public void SetCurrentDesignManually(CraftingTemplate craftingTemplate, ValueTuple<CraftingPiece, int>[] pieces)
		{
			if (!this.IsInCraftingMode)
			{
				this.ExecuteSwitchToCrafting();
			}
			this.WeaponDesign.SetDesignManually(craftingTemplate, pieces, true);
		}

		// Token: 0x170007A3 RID: 1955
		// (get) Token: 0x060016F9 RID: 5881 RVA: 0x00058C9C File Offset: 0x00056E9C
		// (set) Token: 0x060016FA RID: 5882 RVA: 0x00058CA4 File Offset: 0x00056EA4
		[DataSourceProperty]
		public SmeltingVM Smelting
		{
			get
			{
				return this._smelting;
			}
			set
			{
				if (value != this._smelting)
				{
					this._smelting = value;
					base.OnPropertyChangedWithValue<SmeltingVM>(value, "Smelting");
				}
			}
		}

		// Token: 0x170007A4 RID: 1956
		// (get) Token: 0x060016FB RID: 5883 RVA: 0x00058CC2 File Offset: 0x00056EC2
		// (set) Token: 0x060016FC RID: 5884 RVA: 0x00058CCA File Offset: 0x00056ECA
		[DataSourceProperty]
		public WeaponDesignVM WeaponDesign
		{
			get
			{
				return this._weaponDesign;
			}
			set
			{
				if (value != this._weaponDesign)
				{
					this._weaponDesign = value;
					base.OnPropertyChangedWithValue<WeaponDesignVM>(value, "WeaponDesign");
				}
			}
		}

		// Token: 0x170007A5 RID: 1957
		// (get) Token: 0x060016FD RID: 5885 RVA: 0x00058CE8 File Offset: 0x00056EE8
		// (set) Token: 0x060016FE RID: 5886 RVA: 0x00058CF0 File Offset: 0x00056EF0
		[DataSourceProperty]
		public RefinementVM Refinement
		{
			get
			{
				return this._refinement;
			}
			set
			{
				if (value != this._refinement)
				{
					this._refinement = value;
					base.OnPropertyChangedWithValue<RefinementVM>(value, "Refinement");
				}
			}
		}

		// Token: 0x170007A6 RID: 1958
		// (get) Token: 0x060016FF RID: 5887 RVA: 0x00058D0E File Offset: 0x00056F0E
		// (set) Token: 0x06001700 RID: 5888 RVA: 0x00058D16 File Offset: 0x00056F16
		[DataSourceProperty]
		public bool IsInCraftingMode
		{
			get
			{
				return this._isInCraftingMode;
			}
			set
			{
				if (value != this._isInCraftingMode)
				{
					this._isInCraftingMode = value;
					base.OnPropertyChangedWithValue(value, "IsInCraftingMode");
				}
			}
		}

		// Token: 0x170007A7 RID: 1959
		// (get) Token: 0x06001701 RID: 5889 RVA: 0x00058D34 File Offset: 0x00056F34
		// (set) Token: 0x06001702 RID: 5890 RVA: 0x00058D3C File Offset: 0x00056F3C
		[DataSourceProperty]
		public bool IsInSmeltingMode
		{
			get
			{
				return this._isInSmeltingMode;
			}
			set
			{
				if (value != this._isInSmeltingMode)
				{
					this._isInSmeltingMode = value;
					base.OnPropertyChangedWithValue(value, "IsInSmeltingMode");
				}
			}
		}

		// Token: 0x170007A8 RID: 1960
		// (get) Token: 0x06001703 RID: 5891 RVA: 0x00058D5A File Offset: 0x00056F5A
		// (set) Token: 0x06001704 RID: 5892 RVA: 0x00058D62 File Offset: 0x00056F62
		[DataSourceProperty]
		public bool IsInRefinementMode
		{
			get
			{
				return this._isInRefinementMode;
			}
			set
			{
				if (value != this._isInRefinementMode)
				{
					this._isInRefinementMode = value;
					base.OnPropertyChangedWithValue(value, "IsInRefinementMode");
				}
			}
		}

		// Token: 0x170007A9 RID: 1961
		// (get) Token: 0x06001705 RID: 5893 RVA: 0x00058D80 File Offset: 0x00056F80
		// (set) Token: 0x06001706 RID: 5894 RVA: 0x00058D88 File Offset: 0x00056F88
		[DataSourceProperty]
		public bool IsSmeltingItemSelected
		{
			get
			{
				return this._isSmeltingItemSelected;
			}
			set
			{
				if (value != this._isSmeltingItemSelected)
				{
					this._isSmeltingItemSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSmeltingItemSelected");
				}
			}
		}

		// Token: 0x170007AA RID: 1962
		// (get) Token: 0x06001707 RID: 5895 RVA: 0x00058DA6 File Offset: 0x00056FA6
		// (set) Token: 0x06001708 RID: 5896 RVA: 0x00058DAE File Offset: 0x00056FAE
		[DataSourceProperty]
		public bool IsRefinementItemSelected
		{
			get
			{
				return this._isRefinementItemSelected;
			}
			set
			{
				if (value != this._isRefinementItemSelected)
				{
					this._isRefinementItemSelected = value;
					base.OnPropertyChangedWithValue(value, "IsRefinementItemSelected");
				}
			}
		}

		// Token: 0x170007AB RID: 1963
		// (get) Token: 0x06001709 RID: 5897 RVA: 0x00058DCC File Offset: 0x00056FCC
		// (set) Token: 0x0600170A RID: 5898 RVA: 0x00058DD4 File Offset: 0x00056FD4
		[DataSourceProperty]
		public string SelectItemToSmeltText
		{
			get
			{
				return this._selectItemToSmeltText;
			}
			set
			{
				if (value != this._selectItemToSmeltText)
				{
					this._selectItemToSmeltText = value;
					base.OnPropertyChangedWithValue<string>(value, "SelectItemToSmeltText");
				}
			}
		}

		// Token: 0x170007AC RID: 1964
		// (get) Token: 0x0600170B RID: 5899 RVA: 0x00058DF7 File Offset: 0x00056FF7
		// (set) Token: 0x0600170C RID: 5900 RVA: 0x00058DFF File Offset: 0x00056FFF
		[DataSourceProperty]
		public string SelectItemToRefineText
		{
			get
			{
				return this._selectItemToRefineText;
			}
			set
			{
				if (value != this._selectItemToRefineText)
				{
					this._selectItemToRefineText = value;
					base.OnPropertyChangedWithValue<string>(value, "SelectItemToRefineText");
				}
			}
		}

		// Token: 0x170007AD RID: 1965
		// (get) Token: 0x0600170D RID: 5901 RVA: 0x00058E22 File Offset: 0x00057022
		// (set) Token: 0x0600170E RID: 5902 RVA: 0x00058E2A File Offset: 0x0005702A
		[DataSourceProperty]
		public ElementNotificationVM TutorialNotification
		{
			get
			{
				return this._tutorialNotification;
			}
			set
			{
				if (value != this._tutorialNotification)
				{
					this._tutorialNotification = value;
					base.OnPropertyChangedWithValue<ElementNotificationVM>(value, "TutorialNotification");
				}
			}
		}

		// Token: 0x0600170F RID: 5903 RVA: 0x00058E48 File Offset: 0x00057048
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = string.Empty;
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = this._latestTutorialElementID;
				}
			}
		}

		// Token: 0x04000A5F RID: 2655
		private const int _minimumRequiredStamina = 10;

		// Token: 0x04000A60 RID: 2656
		public CraftingVM.OnItemRefreshedDelegate OnItemRefreshed;

		// Token: 0x04000A61 RID: 2657
		private readonly Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

		// Token: 0x04000A62 RID: 2658
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000A63 RID: 2659
		private readonly Action _onClose;

		// Token: 0x04000A64 RID: 2660
		private readonly Action _resetCamera;

		// Token: 0x04000A65 RID: 2661
		private readonly Action _onWeaponCrafted;

		// Token: 0x04000A66 RID: 2662
		private Crafting _crafting;

		// Token: 0x04000A67 RID: 2663
		private InputKeyItemVM _confirmInputKey;

		// Token: 0x04000A68 RID: 2664
		private InputKeyItemVM _exitInputKey;

		// Token: 0x04000A69 RID: 2665
		private InputKeyItemVM _previousTabInputKey;

		// Token: 0x04000A6A RID: 2666
		private InputKeyItemVM _nextTabInputKey;

		// Token: 0x04000A6B RID: 2667
		private MBBindingList<InputKeyItemVM> _cameraControlKeys;

		// Token: 0x04000A6C RID: 2668
		private bool _canSwitchTabs;

		// Token: 0x04000A6D RID: 2669
		private bool _areGamepadControlHintsEnabled;

		// Token: 0x04000A6E RID: 2670
		private string _doneLbl;

		// Token: 0x04000A6F RID: 2671
		private string _cancelLbl;

		// Token: 0x04000A70 RID: 2672
		private HintViewModel _resetCameraHint;

		// Token: 0x04000A71 RID: 2673
		private HintViewModel _smeltingHint;

		// Token: 0x04000A72 RID: 2674
		private HintViewModel _craftingHint;

		// Token: 0x04000A73 RID: 2675
		private HintViewModel _refiningHint;

		// Token: 0x04000A74 RID: 2676
		private BasicTooltipViewModel _mainActionHint;

		// Token: 0x04000A75 RID: 2677
		private int _itemValue = -1;

		// Token: 0x04000A76 RID: 2678
		private string _currentCategoryText;

		// Token: 0x04000A77 RID: 2679
		private string _mainActionText;

		// Token: 0x04000A78 RID: 2680
		private string _craftingText;

		// Token: 0x04000A79 RID: 2681
		private string _smeltingText;

		// Token: 0x04000A7A RID: 2682
		private string _refinementText;

		// Token: 0x04000A7B RID: 2683
		private bool _isMainActionEnabled;

		// Token: 0x04000A7C RID: 2684
		private MBBindingList<CraftingAvailableHeroItemVM> _availableCharactersForSmithing;

		// Token: 0x04000A7D RID: 2685
		private CraftingAvailableHeroItemVM _currentCraftingHero;

		// Token: 0x04000A7E RID: 2686
		private MBBindingList<CraftingResourceItemVM> _playerCurrentMaterials;

		// Token: 0x04000A7F RID: 2687
		private CraftingHeroPopupVM _craftingHeroPopup;

		// Token: 0x04000A80 RID: 2688
		private bool _isInSmeltingMode;

		// Token: 0x04000A81 RID: 2689
		private bool _isInCraftingMode;

		// Token: 0x04000A82 RID: 2690
		private bool _isInRefinementMode;

		// Token: 0x04000A83 RID: 2691
		private SmeltingVM _smelting;

		// Token: 0x04000A84 RID: 2692
		private RefinementVM _refinement;

		// Token: 0x04000A85 RID: 2693
		private WeaponDesignVM _weaponDesign;

		// Token: 0x04000A86 RID: 2694
		private bool _isSmeltingItemSelected;

		// Token: 0x04000A87 RID: 2695
		private bool _isRefinementItemSelected;

		// Token: 0x04000A88 RID: 2696
		private string _selectItemToSmeltText;

		// Token: 0x04000A89 RID: 2697
		private string _selectItemToRefineText;

		// Token: 0x04000A8A RID: 2698
		public ElementNotificationVM _tutorialNotification;

		// Token: 0x04000A8B RID: 2699
		private string _latestTutorialElementID;

		// Token: 0x02000255 RID: 597
		// (Invoke) Token: 0x0600250D RID: 9485
		public delegate void OnItemRefreshedDelegate(bool isItemVisible);
	}
}
