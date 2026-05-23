using System;
using Helpers;
using SandBox.View;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x0200000B RID: 11
	[GameStateScreen(typeof(InventoryState))]
	public class GauntletInventoryScreen : ScreenBase, IInventoryStateHandler, IGameStateListener, IChangeableScreen
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600006C RID: 108 RVA: 0x000053D0 File Offset: 0x000035D0
		// (set) Token: 0x0600006D RID: 109 RVA: 0x000053D8 File Offset: 0x000035D8
		public InventoryState InventoryState { get; private set; }

		// Token: 0x0600006E RID: 110 RVA: 0x000053E4 File Offset: 0x000035E4
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (!this._closed)
			{
				LoadingWindow.DisableGlobalLoadingWindow();
			}
			this._dataSource.IsFiveStackModifierActive = this._gauntletLayer.Input.IsHotKeyDown("FiveStackModifier");
			this._dataSource.IsEntireStackModifierActive = this._gauntletLayer.Input.IsHotKeyDown("EntireStackModifier");
			if (!this._dataSource.IsSearchAvailable || !this._gauntletLayer.IsFocusedOnInput())
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("SwitchAlternative") && this._dataSource != null)
				{
					this._dataSource.CompareNextItem();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyReleased("Exit") || this._gauntletLayer.Input.IsGameKeyReleased(38))
				{
					this.ExecuteCancel();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					this.ExecuteConfirm();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyReleased("Reset"))
				{
					this.HandleResetInput();
					return;
				}
				if (this._gauntletLayer.Input.IsHotKeyPressed("SwitchToPreviousTab"))
				{
					if (!this._dataSource.IsFocusedOnItemList || !Input.IsGamepadActive)
					{
						this.ExecuteSwitchToPreviousTab();
						return;
					}
					if (this._dataSource.CurrentFocusedItem != null && this._dataSource.CurrentFocusedItem.IsTransferable && this._dataSource.CurrentFocusedItem.InventorySide == InventoryLogic.InventorySide.OtherInventory)
					{
						this.ExecuteBuySingle();
						return;
					}
				}
				else if (this._gauntletLayer.Input.IsHotKeyPressed("SwitchToNextTab"))
				{
					if (!this._dataSource.IsFocusedOnItemList || !Input.IsGamepadActive)
					{
						this.ExecuteSwitchToNextTab();
						return;
					}
					if (this._dataSource.CurrentFocusedItem != null && this._dataSource.CurrentFocusedItem.IsTransferable && this._dataSource.CurrentFocusedItem.InventorySide == InventoryLogic.InventorySide.PlayerInventory)
					{
						this.ExecuteSellSingle();
						return;
					}
				}
				else
				{
					if (this._gauntletLayer.Input.IsHotKeyPressed("TakeAll"))
					{
						this.ExecuteTakeAll();
						return;
					}
					if (this._gauntletLayer.Input.IsHotKeyPressed("GiveAll"))
					{
						this.ExecuteGiveAll();
					}
				}
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00005614 File Offset: 0x00003814
		public GauntletInventoryScreen(InventoryState inventoryState)
		{
			this.InventoryState = inventoryState;
			this.InventoryState.Handler = this;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005630 File Offset: 0x00003830
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._inventoryCategory = UIResourceManager.LoadSpriteCategory("ui_inventory");
			InventoryLogic inventoryLogic = this.InventoryState.InventoryLogic;
			Mission mission = Mission.Current;
			this._dataSource = new SPInventoryVM(inventoryLogic, mission != null && mission.DoesMissionRequireCivilianEquipment, new Func<WeaponComponentData, ItemObject.ItemUsageSetFlags>(this.GetItemUsageSetFlag));
			this._dataSource.SetGetKeyTextFromKeyIDFunc(new Func<string, TextObject>(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID));
			this._dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetPreviousCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
			this._dataSource.SetNextCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
			this._dataSource.SetBuyAllInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("TakeAll"));
			this._dataSource.SetSellAllInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("GiveAll"));
			this._gauntletLayer = new GauntletLayer("InventoryScreen", 15, true)
			{
				IsFocusLayer = true
			};
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.AddLayer(this._gauntletLayer);
			ScreenManager.TrySetFocus(this._gauntletLayer);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("InventoryHotKeyCategory"));
			this._gauntletMovie = this._gauntletLayer.LoadMovie("Inventory", this._dataSource);
			this._openedFromMission = this.InventoryState.Predecessor is MissionState;
			UISoundsHelper.PlayUISound("event:/ui/panels/panel_inventory_open");
			this._gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
			InformationManager.HideAllMessages();
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005866 File Offset: 0x00003A66
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this._closed = true;
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, true);
			}
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000072 RID: 114 RVA: 0x0000588E File Offset: 0x00003A8E
		protected override void OnActivate()
		{
			base.OnActivate();
			SPInventoryVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.RefreshCallbacks();
			}
			if (this._gauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._gauntletLayer, false);
				ScreenManager.TrySetFocus(this._gauntletLayer);
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000058C6 File Offset: 0x00003AC6
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._gauntletMovie = null;
			this._inventoryCategory.Unload();
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._gauntletLayer = null;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000058F9 File Offset: 0x00003AF9
		void IGameStateListener.OnActivate()
		{
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.InventoryScreen));
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005910 File Offset: 0x00003B10
		void IGameStateListener.OnDeactivate()
		{
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.None));
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005927 File Offset: 0x00003B27
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005929 File Offset: 0x00003B29
		void IGameStateListener.OnFinalize()
		{
		}

		// Token: 0x06000078 RID: 120 RVA: 0x0000592B File Offset: 0x00003B2B
		public void ExecuteLootingScript()
		{
			this._dataSource.ExecuteBuyAllItems();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005938 File Offset: 0x00003B38
		public void ExecuteSellAllLoot()
		{
			this._dataSource.ExecuteSellAllItems();
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00005945 File Offset: 0x00003B45
		private void HandleResetInput()
		{
			if (!this._dataSource.ItemPreview.IsSelected)
			{
				this._dataSource.ExecuteResetTranstactions();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00005970 File Offset: 0x00003B70
		public void ExecuteCancel()
		{
			if (this._dataSource.ItemPreview.IsSelected)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ClosePreview();
				return;
			}
			if (this._dataSource.IsAnyEquippedItemSelected())
			{
				this._dataSource.ExecuteClearSelectedItem();
				return;
			}
			UISoundsHelper.PlayUISound("event:/ui/default");
			this._dataSource.ExecuteResetAndCompleteTranstactions();
		}

		// Token: 0x0600007C RID: 124 RVA: 0x000059D3 File Offset: 0x00003BD3
		public void ExecuteConfirm()
		{
			if (!this._dataSource.ItemPreview.IsSelected && !this._dataSource.IsDoneDisabled)
			{
				this._dataSource.ExecuteCompleteTranstactions();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00005A0C File Offset: 0x00003C0C
		public void ExecuteSwitchToPreviousTab()
		{
			if (!this._dataSource.ItemPreview.IsSelected)
			{
				MBBindingList<InventoryCharacterSelectorItemVM> itemList = this._dataSource.CharacterList.ItemList;
				if (itemList != null && itemList.Count > 1)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
				this._dataSource.CharacterList.ExecuteSelectPreviousItem();
			}
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00005A68 File Offset: 0x00003C68
		public void ExecuteSwitchToNextTab()
		{
			if (!this._dataSource.ItemPreview.IsSelected)
			{
				MBBindingList<InventoryCharacterSelectorItemVM> itemList = this._dataSource.CharacterList.ItemList;
				if (itemList != null && itemList.Count > 1)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
				}
				this._dataSource.CharacterList.ExecuteSelectNextItem();
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00005AC2 File Offset: 0x00003CC2
		public void ExecuteBuySingle()
		{
			this._dataSource.CurrentFocusedItem.ExecuteBuySingle();
			UISoundsHelper.PlayUISound("event:/ui/transfer");
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00005ADE File Offset: 0x00003CDE
		public void ExecuteSellSingle()
		{
			this._dataSource.CurrentFocusedItem.ExecuteSellSingle();
			UISoundsHelper.PlayUISound("event:/ui/transfer");
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00005AFA File Offset: 0x00003CFA
		public void ExecuteTakeAll()
		{
			if (!this._dataSource.ItemPreview.IsSelected)
			{
				this._dataSource.ExecuteBuyAllItems();
				UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00005B23 File Offset: 0x00003D23
		public void ExecuteGiveAll()
		{
			if (!this._dataSource.ItemPreview.IsSelected)
			{
				this._dataSource.ExecuteSellAllItems();
				UISoundsHelper.PlayUISound("event:/ui/inventory/take_all");
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00005B4C File Offset: 0x00003D4C
		public void ExecuteBuyConsumableItem()
		{
			this._dataSource.ExecuteBuyItemTest();
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00005B59 File Offset: 0x00003D59
		private ItemObject.ItemUsageSetFlags GetItemUsageSetFlag(WeaponComponentData item)
		{
			if (!string.IsNullOrEmpty(item.ItemUsage))
			{
				return MBItem.GetItemUsageSetFlags(item.ItemUsage);
			}
			return (ItemObject.ItemUsageSetFlags)0;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00005B75 File Offset: 0x00003D75
		private void CloseInventoryScreen()
		{
			InventoryScreenHelper.CloseScreen(false);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00005B7D File Offset: 0x00003D7D
		bool IChangeableScreen.AnyUnsavedChanges()
		{
			return this.InventoryState.InventoryLogic.IsThereAnyChanges();
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00005B8F File Offset: 0x00003D8F
		bool IChangeableScreen.CanChangesBeApplied()
		{
			return this.InventoryState.InventoryLogic.CanPlayerCompleteTransaction();
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00005BA1 File Offset: 0x00003DA1
		void IChangeableScreen.ApplyChanges()
		{
			this._dataSource.ItemPreview.Close();
			this.InventoryState.InventoryLogic.DoneLogic();
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00005BC4 File Offset: 0x00003DC4
		void IChangeableScreen.ResetChanges()
		{
			this.InventoryState.InventoryLogic.Reset(true);
		}

		// Token: 0x0400003A RID: 58
		private GauntletMovieIdentifier _gauntletMovie;

		// Token: 0x0400003B RID: 59
		private SPInventoryVM _dataSource;

		// Token: 0x0400003C RID: 60
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400003D RID: 61
		private bool _closed;

		// Token: 0x0400003E RID: 62
		private bool _openedFromMission;

		// Token: 0x0400003F RID: 63
		private SpriteCategory _inventoryCategory;
	}
}
