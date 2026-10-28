using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;

public class SmeltingItemVM : ViewModel
{
	private readonly Action<SmeltingItemVM> _onSelection;

	private readonly Action<SmeltingItemVM, bool> _onItemLockedStateChange;

	private ItemImageIdentifierVM _visual;

	private string _name;

	private int _numOfItems;

	private MBBindingList<CraftingResourceItemVM> _inputMaterials;

	private MBBindingList<CraftingResourceItemVM> _yield;

	private HintViewModel _lockHint;

	private bool _isSelected;

	private bool _isLocked;

	private bool _hasMoreThanOneItem;

	public EquipmentElement EquipmentElement { get; private set; }

	[DataSourceProperty]
	public ItemImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingResourceItemVM> Yield
	{
		get
		{
			return _yield;
		}
		set
		{
			if (value != _yield)
			{
				_yield = value;
				OnPropertyChangedWithValue(value, "Yield");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingResourceItemVM> InputMaterials
	{
		get
		{
			return _inputMaterials;
		}
		set
		{
			if (value != _inputMaterials)
			{
				_inputMaterials = value;
				OnPropertyChangedWithValue(value, "InputMaterials");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfItems
	{
		get
		{
			return _numOfItems;
		}
		set
		{
			if (value != _numOfItems)
			{
				_numOfItems = value;
				OnPropertyChangedWithValue(value, "NumOfItems");
			}
		}
	}

	[DataSourceProperty]
	public bool HasMoreThanOneItem
	{
		get
		{
			return _hasMoreThanOneItem;
		}
		set
		{
			if (value != _hasMoreThanOneItem)
			{
				_hasMoreThanOneItem = value;
				OnPropertyChangedWithValue(value, "HasMoreThanOneItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LockHint
	{
		get
		{
			return _lockHint;
		}
		set
		{
			if (value != _lockHint)
			{
				_lockHint = value;
				OnPropertyChangedWithValue(value, "LockHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked)
			{
				_isLocked = value;
				OnPropertyChangedWithValue(value, "IsLocked");
				_onItemLockedStateChange(this, value);
			}
		}
	}

	public SmeltingItemVM(EquipmentElement equipmentElement, Action<SmeltingItemVM> onSelection, Action<SmeltingItemVM, bool> onItemLockedStateChange, bool isLocked, int numOfItems)
	{
		_onSelection = onSelection;
		_onItemLockedStateChange = onItemLockedStateChange;
		EquipmentElement = equipmentElement;
		Yield = new MBBindingList<CraftingResourceItemVM>();
		InputMaterials = new MBBindingList<CraftingResourceItemVM>();
		LockHint = new HintViewModel(GameTexts.FindText("str_lock_in_inventory").SetTextVariable("TRANSFERABLE", GameTexts.FindText("str_items").ToString()));
		int[] smeltingOutputForItem = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(equipmentElement.Item);
		for (int i = 0; i < smeltingOutputForItem.Length; i++)
		{
			if (smeltingOutputForItem[i] > 0)
			{
				Yield.Add(new CraftingResourceItemVM((CraftingMaterials)i, smeltingOutputForItem[i]));
			}
			else if (smeltingOutputForItem[i] < 0)
			{
				InputMaterials.Add(new CraftingResourceItemVM((CraftingMaterials)i, -smeltingOutputForItem[i]));
			}
		}
		IsLocked = isLocked;
		Visual = new ItemImageIdentifierVM(equipmentElement.Item);
		NumOfItems = numOfItems;
		HasMoreThanOneItem = NumOfItems > 1;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = EquipmentElement.Item.Name.ToString();
	}

	public void ExecuteSelection()
	{
		_onSelection(this);
	}

	public void ExecuteShowItemTooltip()
	{
		InformationManager.ShowTooltip(typeof(ItemObject), EquipmentElement);
	}

	public void ExecuteHideItemTooltip()
	{
		MBInformationManager.HideInformations();
	}
}
