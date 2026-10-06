using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;

public class SmeltingVM : ViewModel
{
	private ItemRoster _playerItemRoster;

	private Action _updateValuesOnSelectItemAction;

	private Action _updateValuesOnSmeltItemAction;

	private List<string> _lockedItemIDs;

	private readonly ICraftingCampaignBehavior _smithingBehavior;

	private string _weaponTypeName;

	private string _weaponTypeCode;

	private SmeltingItemVM _currentSelectedItem;

	private MBBindingList<SmeltingItemVM> _smeltableItemList;

	private SmeltingSortControllerVM _sortController;

	private HintViewModel _selectAllHint;

	private bool _isAnyItemSelected;

	[DataSourceProperty]
	public string WeaponTypeName
	{
		get
		{
			return _weaponTypeName;
		}
		set
		{
			if (value != _weaponTypeName)
			{
				_weaponTypeName = value;
				OnPropertyChangedWithValue(value, "WeaponTypeName");
			}
		}
	}

	[DataSourceProperty]
	public string WeaponTypeCode
	{
		get
		{
			return _weaponTypeCode;
		}
		set
		{
			if (value != _weaponTypeCode)
			{
				_weaponTypeCode = value;
				OnPropertyChangedWithValue(value, "WeaponTypeCode");
			}
		}
	}

	[DataSourceProperty]
	public SmeltingItemVM CurrentSelectedItem
	{
		get
		{
			return _currentSelectedItem;
		}
		set
		{
			if (value != _currentSelectedItem)
			{
				_currentSelectedItem = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedItem");
				IsAnyItemSelected = value != null;
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyItemSelected
	{
		get
		{
			return _isAnyItemSelected;
		}
		set
		{
			if (value != _isAnyItemSelected)
			{
				_isAnyItemSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SmeltingItemVM> SmeltableItemList
	{
		get
		{
			return _smeltableItemList;
		}
		set
		{
			if (value != _smeltableItemList)
			{
				_smeltableItemList = value;
				OnPropertyChangedWithValue(value, "SmeltableItemList");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SelectAllHint
	{
		get
		{
			return _selectAllHint;
		}
		set
		{
			if (value != _selectAllHint)
			{
				_selectAllHint = value;
				OnPropertyChangedWithValue(value, "SelectAllHint");
			}
		}
	}

	[DataSourceProperty]
	public SmeltingSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	public SmeltingVM(Action updateValuesOnSelectItemAction, Action updateValuesOnSmeltItemAction)
	{
		SortController = new SmeltingSortControllerVM();
		_updateValuesOnSelectItemAction = updateValuesOnSelectItemAction;
		_updateValuesOnSmeltItemAction = updateValuesOnSmeltItemAction;
		_playerItemRoster = MobileParty.MainParty.ItemRoster;
		_smithingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		IViewDataTracker campaignBehavior = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		_lockedItemIDs = campaignBehavior.GetInventoryLocks().ToList();
		RefreshList();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SelectAllHint = new HintViewModel(new TextObject("{=k1E9DuKi}Select All"));
		CurrentSelectedItem?.RefreshValues();
		SmeltableItemList.ApplyActionOnAllItems(delegate(SmeltingItemVM x)
		{
			x.RefreshValues();
		});
		SortController.RefreshValues();
	}

	internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
	{
	}

	public void RefreshList()
	{
		SmeltableItemList = new MBBindingList<SmeltingItemVM>();
		SortController.SetListToControl(SmeltableItemList);
		for (int i = 0; i < _playerItemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = _playerItemRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.EquipmentElement.Item.IsCraftedWeapon)
			{
				bool isLocked = IsItemLocked(elementCopyAtIndex.EquipmentElement);
				SmeltingItemVM smeltingItemVM = new SmeltingItemVM(elementCopyAtIndex.EquipmentElement, OnItemSelection, ProcessLockItem, isLocked, elementCopyAtIndex.Amount);
				if (smeltingItemVM.Visual.Id == CurrentSelectedItem?.Visual?.Id)
				{
					OnItemSelection(smeltingItemVM);
				}
				SmeltableItemList.Add(smeltingItemVM);
			}
		}
		if (SmeltableItemList.Count == 0)
		{
			CurrentSelectedItem = null;
		}
	}

	private void OnItemSelection(SmeltingItemVM newItem)
	{
		if (newItem != CurrentSelectedItem)
		{
			if (CurrentSelectedItem != null)
			{
				CurrentSelectedItem.IsSelected = false;
			}
			CurrentSelectedItem = newItem;
			CurrentSelectedItem.IsSelected = true;
		}
		_updateValuesOnSelectItemAction();
		WeaponTypeName = CurrentSelectedItem.EquipmentElement.Item.WeaponDesign?.Template.TemplateName.ToString() ?? string.Empty;
		WeaponTypeCode = CurrentSelectedItem.EquipmentElement.Item.WeaponDesign?.Template.StringId ?? string.Empty;
	}

	public void TrySmeltingSelectedItems(Hero currentCraftingHero)
	{
		if (_currentSelectedItem == null)
		{
			return;
		}
		if (_currentSelectedItem.IsLocked)
		{
			string text = new TextObject("{=wMiLUTNY}Are you sure you want to smelt this weapon? It is locked in the inventory.").ToString();
			InformationManager.ShowInquiry(new InquiryData("", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				SmeltSelectedItems(currentCraftingHero);
			}, null));
		}
		else
		{
			SmeltSelectedItems(currentCraftingHero);
		}
	}

	private void ProcessLockItem(SmeltingItemVM item, bool isLocked)
	{
		if (item != null)
		{
			string itemLockStringID = CampaignUIHelper.GetItemLockStringID(item.EquipmentElement);
			if (isLocked && !_lockedItemIDs.Contains(itemLockStringID))
			{
				_lockedItemIDs.Add(itemLockStringID);
			}
			else if (!isLocked && _lockedItemIDs.Contains(itemLockStringID))
			{
				_lockedItemIDs.Remove(itemLockStringID);
			}
		}
	}

	private void SmeltSelectedItems(Hero currentCraftingHero)
	{
		if (_currentSelectedItem != null && _smithingBehavior != null)
		{
			_smithingBehavior?.DoSmelting(currentCraftingHero, _currentSelectedItem.EquipmentElement);
		}
		RefreshList();
		SortController.SortByCurrentState();
		if (CurrentSelectedItem != null)
		{
			int num = SmeltableItemList.FindIndex((SmeltingItemVM i) => i.EquipmentElement.Item == CurrentSelectedItem.EquipmentElement.Item);
			SmeltingItemVM newItem = ((num != -1) ? SmeltableItemList[num] : SmeltableItemList.FirstOrDefault());
			OnItemSelection(newItem);
		}
		_updateValuesOnSmeltItemAction();
	}

	private bool IsItemLocked(EquipmentElement equipmentElement)
	{
		string itemLockStringID = CampaignUIHelper.GetItemLockStringID(equipmentElement);
		return _lockedItemIDs.Contains(itemLockStringID);
	}

	public void SaveItemLockStates()
	{
		Campaign.Current.GetCampaignBehavior<IViewDataTracker>().SetInventoryLocks(_lockedItemIDs);
	}
}
