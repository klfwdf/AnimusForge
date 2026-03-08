using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting
{
	// Token: 0x02000113 RID: 275
	public class SmeltingVM : ViewModel
	{
		// Token: 0x06001916 RID: 6422 RVA: 0x0005F0E4 File Offset: 0x0005D2E4
		public SmeltingVM(Action updateValuesOnSelectItemAction, Action updateValuesOnSmeltItemAction)
		{
			this.SortController = new SmeltingSortControllerVM();
			this._updateValuesOnSelectItemAction = updateValuesOnSelectItemAction;
			this._updateValuesOnSmeltItemAction = updateValuesOnSmeltItemAction;
			this._playerItemRoster = MobileParty.MainParty.ItemRoster;
			this._smithingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			IViewDataTracker campaignBehavior = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this._lockedItemIDs = campaignBehavior.GetInventoryLocks().ToList<string>();
			this.RefreshList();
			this.RefreshValues();
		}

		// Token: 0x06001917 RID: 6423 RVA: 0x0005F158 File Offset: 0x0005D358
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SelectAllHint = new HintViewModel(new TextObject("{=k1E9DuKi}Select All", null), null);
			SmeltingItemVM currentSelectedItem = this.CurrentSelectedItem;
			if (currentSelectedItem != null)
			{
				currentSelectedItem.RefreshValues();
			}
			this.SmeltableItemList.ApplyActionOnAllItems(delegate(SmeltingItemVM x)
			{
				x.RefreshValues();
			});
			this.SortController.RefreshValues();
		}

		// Token: 0x06001918 RID: 6424 RVA: 0x0005F1C8 File Offset: 0x0005D3C8
		internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
		{
		}

		// Token: 0x06001919 RID: 6425 RVA: 0x0005F1CC File Offset: 0x0005D3CC
		public void RefreshList()
		{
			this.SmeltableItemList = new MBBindingList<SmeltingItemVM>();
			this.SortController.SetListToControl(this.SmeltableItemList);
			for (int i = 0; i < this._playerItemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = this._playerItemRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.EquipmentElement.Item.IsCraftedWeapon)
				{
					bool isLocked = this.IsItemLocked(elementCopyAtIndex.EquipmentElement);
					SmeltingItemVM smeltingItemVM = new SmeltingItemVM(elementCopyAtIndex.EquipmentElement, new Action<SmeltingItemVM>(this.OnItemSelection), new Action<SmeltingItemVM, bool>(this.ProcessLockItem), isLocked, elementCopyAtIndex.Amount);
					string id = smeltingItemVM.Visual.Id;
					SmeltingItemVM currentSelectedItem = this.CurrentSelectedItem;
					string b;
					if (currentSelectedItem == null)
					{
						b = null;
					}
					else
					{
						ItemImageIdentifierVM visual = currentSelectedItem.Visual;
						b = ((visual != null) ? visual.Id : null);
					}
					if (id == b)
					{
						this.OnItemSelection(smeltingItemVM);
					}
					this.SmeltableItemList.Add(smeltingItemVM);
				}
			}
			if (this.SmeltableItemList.Count == 0)
			{
				this.CurrentSelectedItem = null;
			}
		}

		// Token: 0x0600191A RID: 6426 RVA: 0x0005F2D0 File Offset: 0x0005D4D0
		private void OnItemSelection(SmeltingItemVM newItem)
		{
			if (newItem != this.CurrentSelectedItem)
			{
				if (this.CurrentSelectedItem != null)
				{
					this.CurrentSelectedItem.IsSelected = false;
				}
				this.CurrentSelectedItem = newItem;
				this.CurrentSelectedItem.IsSelected = true;
			}
			this._updateValuesOnSelectItemAction();
			WeaponDesign weaponDesign = this.CurrentSelectedItem.EquipmentElement.Item.WeaponDesign;
			this.WeaponTypeName = ((weaponDesign != null) ? weaponDesign.Template.TemplateName.ToString() : null) ?? string.Empty;
			WeaponDesign weaponDesign2 = this.CurrentSelectedItem.EquipmentElement.Item.WeaponDesign;
			this.WeaponTypeCode = ((weaponDesign2 != null) ? weaponDesign2.Template.StringId : null) ?? string.Empty;
		}

		// Token: 0x0600191B RID: 6427 RVA: 0x0005F390 File Offset: 0x0005D590
		public void TrySmeltingSelectedItems(Hero currentCraftingHero)
		{
			if (this._currentSelectedItem != null)
			{
				if (this._currentSelectedItem.IsLocked)
				{
					string text = new TextObject("{=wMiLUTNY}Are you sure you want to smelt this weapon? It is locked in the inventory.", null).ToString();
					InformationManager.ShowInquiry(new InquiryData("", text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
					{
						this.SmeltSelectedItems(currentCraftingHero);
					}, null, "", 0f, null, null, null), false, false);
					return;
				}
				this.SmeltSelectedItems(currentCraftingHero);
			}
		}

		// Token: 0x0600191C RID: 6428 RVA: 0x0005F434 File Offset: 0x0005D634
		private void ProcessLockItem(SmeltingItemVM item, bool isLocked)
		{
			if (item == null)
			{
				return;
			}
			string itemLockStringID = CampaignUIHelper.GetItemLockStringID(item.EquipmentElement);
			if (isLocked && !this._lockedItemIDs.Contains(itemLockStringID))
			{
				this._lockedItemIDs.Add(itemLockStringID);
				return;
			}
			if (!isLocked && this._lockedItemIDs.Contains(itemLockStringID))
			{
				this._lockedItemIDs.Remove(itemLockStringID);
			}
		}

		// Token: 0x0600191D RID: 6429 RVA: 0x0005F490 File Offset: 0x0005D690
		private void SmeltSelectedItems(Hero currentCraftingHero)
		{
			if (this._currentSelectedItem != null && this._smithingBehavior != null)
			{
				ICraftingCampaignBehavior smithingBehavior = this._smithingBehavior;
				if (smithingBehavior != null)
				{
					smithingBehavior.DoSmelting(currentCraftingHero, this._currentSelectedItem.EquipmentElement);
				}
			}
			this.RefreshList();
			this.SortController.SortByCurrentState();
			if (this.CurrentSelectedItem != null)
			{
				int num = this.SmeltableItemList.FindIndex((SmeltingItemVM i) => i.EquipmentElement.Item == this.CurrentSelectedItem.EquipmentElement.Item);
				SmeltingItemVM newItem = ((num != -1) ? this.SmeltableItemList[num] : this.SmeltableItemList.FirstOrDefault<SmeltingItemVM>());
				this.OnItemSelection(newItem);
			}
			this._updateValuesOnSmeltItemAction();
		}

		// Token: 0x0600191E RID: 6430 RVA: 0x0005F52C File Offset: 0x0005D72C
		private bool IsItemLocked(EquipmentElement equipmentElement)
		{
			string itemLockStringID = CampaignUIHelper.GetItemLockStringID(equipmentElement);
			return this._lockedItemIDs.Contains(itemLockStringID);
		}

		// Token: 0x0600191F RID: 6431 RVA: 0x0005F54C File Offset: 0x0005D74C
		public void SaveItemLockStates()
		{
			Campaign.Current.GetCampaignBehavior<IViewDataTracker>().SetInventoryLocks(this._lockedItemIDs);
		}

		// Token: 0x1700086B RID: 2155
		// (get) Token: 0x06001920 RID: 6432 RVA: 0x0005F563 File Offset: 0x0005D763
		// (set) Token: 0x06001921 RID: 6433 RVA: 0x0005F56B File Offset: 0x0005D76B
		[DataSourceProperty]
		public string WeaponTypeName
		{
			get
			{
				return this._weaponTypeName;
			}
			set
			{
				if (value != this._weaponTypeName)
				{
					this._weaponTypeName = value;
					base.OnPropertyChangedWithValue<string>(value, "WeaponTypeName");
				}
			}
		}

		// Token: 0x1700086C RID: 2156
		// (get) Token: 0x06001922 RID: 6434 RVA: 0x0005F58E File Offset: 0x0005D78E
		// (set) Token: 0x06001923 RID: 6435 RVA: 0x0005F596 File Offset: 0x0005D796
		[DataSourceProperty]
		public string WeaponTypeCode
		{
			get
			{
				return this._weaponTypeCode;
			}
			set
			{
				if (value != this._weaponTypeCode)
				{
					this._weaponTypeCode = value;
					base.OnPropertyChangedWithValue<string>(value, "WeaponTypeCode");
				}
			}
		}

		// Token: 0x1700086D RID: 2157
		// (get) Token: 0x06001924 RID: 6436 RVA: 0x0005F5B9 File Offset: 0x0005D7B9
		// (set) Token: 0x06001925 RID: 6437 RVA: 0x0005F5C1 File Offset: 0x0005D7C1
		[DataSourceProperty]
		public SmeltingItemVM CurrentSelectedItem
		{
			get
			{
				return this._currentSelectedItem;
			}
			set
			{
				if (value != this._currentSelectedItem)
				{
					this._currentSelectedItem = value;
					base.OnPropertyChangedWithValue<SmeltingItemVM>(value, "CurrentSelectedItem");
					this.IsAnyItemSelected = value != null;
				}
			}
		}

		// Token: 0x1700086E RID: 2158
		// (get) Token: 0x06001926 RID: 6438 RVA: 0x0005F5E9 File Offset: 0x0005D7E9
		// (set) Token: 0x06001927 RID: 6439 RVA: 0x0005F5F1 File Offset: 0x0005D7F1
		[DataSourceProperty]
		public bool IsAnyItemSelected
		{
			get
			{
				return this._isAnyItemSelected;
			}
			set
			{
				if (value != this._isAnyItemSelected)
				{
					this._isAnyItemSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyItemSelected");
				}
			}
		}

		// Token: 0x1700086F RID: 2159
		// (get) Token: 0x06001928 RID: 6440 RVA: 0x0005F60F File Offset: 0x0005D80F
		// (set) Token: 0x06001929 RID: 6441 RVA: 0x0005F617 File Offset: 0x0005D817
		[DataSourceProperty]
		public MBBindingList<SmeltingItemVM> SmeltableItemList
		{
			get
			{
				return this._smeltableItemList;
			}
			set
			{
				if (value != this._smeltableItemList)
				{
					this._smeltableItemList = value;
					base.OnPropertyChangedWithValue<MBBindingList<SmeltingItemVM>>(value, "SmeltableItemList");
				}
			}
		}

		// Token: 0x17000870 RID: 2160
		// (get) Token: 0x0600192A RID: 6442 RVA: 0x0005F635 File Offset: 0x0005D835
		// (set) Token: 0x0600192B RID: 6443 RVA: 0x0005F63D File Offset: 0x0005D83D
		[DataSourceProperty]
		public HintViewModel SelectAllHint
		{
			get
			{
				return this._selectAllHint;
			}
			set
			{
				if (value != this._selectAllHint)
				{
					this._selectAllHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SelectAllHint");
				}
			}
		}

		// Token: 0x17000871 RID: 2161
		// (get) Token: 0x0600192C RID: 6444 RVA: 0x0005F65B File Offset: 0x0005D85B
		// (set) Token: 0x0600192D RID: 6445 RVA: 0x0005F663 File Offset: 0x0005D863
		[DataSourceProperty]
		public SmeltingSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<SmeltingSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x04000B8A RID: 2954
		private ItemRoster _playerItemRoster;

		// Token: 0x04000B8B RID: 2955
		private Action _updateValuesOnSelectItemAction;

		// Token: 0x04000B8C RID: 2956
		private Action _updateValuesOnSmeltItemAction;

		// Token: 0x04000B8D RID: 2957
		private List<string> _lockedItemIDs;

		// Token: 0x04000B8E RID: 2958
		private readonly ICraftingCampaignBehavior _smithingBehavior;

		// Token: 0x04000B8F RID: 2959
		private string _weaponTypeName;

		// Token: 0x04000B90 RID: 2960
		private string _weaponTypeCode;

		// Token: 0x04000B91 RID: 2961
		private SmeltingItemVM _currentSelectedItem;

		// Token: 0x04000B92 RID: 2962
		private MBBindingList<SmeltingItemVM> _smeltableItemList;

		// Token: 0x04000B93 RID: 2963
		private SmeltingSortControllerVM _sortController;

		// Token: 0x04000B94 RID: 2964
		private HintViewModel _selectAllHint;

		// Token: 0x04000B95 RID: 2965
		private bool _isAnyItemSelected;
	}
}
