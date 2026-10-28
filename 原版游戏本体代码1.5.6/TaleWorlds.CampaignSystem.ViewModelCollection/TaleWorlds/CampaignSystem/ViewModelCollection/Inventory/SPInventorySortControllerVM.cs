using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class SPInventorySortControllerVM : ViewModel
{
	public enum InventoryItemSortState
	{
		Default,
		Ascending,
		Descending
	}

	public enum InventoryItemSortOption
	{
		Type,
		Name,
		Quantity,
		Cost
	}

	public abstract class ItemComparer : IComparer<SPItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(SPItemVM x, SPItemVM y);

		protected int ResolveEquality(SPItemVM x, SPItemVM y)
		{
			return x.ItemDescription.CompareTo(y.ItemDescription);
		}
	}

	public class ItemTypeComparer : ItemComparer
	{
		public override int Compare(SPItemVM x, SPItemVM y)
		{
			int itemObjectTypeSortIndex = CampaignUIHelper.GetItemObjectTypeSortIndex(x.ItemRosterElement.EquipmentElement.Item);
			int num = CampaignUIHelper.GetItemObjectTypeSortIndex(y.ItemRosterElement.EquipmentElement.Item).CompareTo(itemObjectTypeSortIndex);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			num = x.ItemCost.CompareTo(y.ItemCost);
			if (num != 0)
			{
				return num;
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemNameComparer : ItemComparer
	{
		public override int Compare(SPItemVM x, SPItemVM y)
		{
			if (_isAscending)
			{
				return y.ItemDescription.CompareTo(x.ItemDescription) * -1;
			}
			return y.ItemDescription.CompareTo(x.ItemDescription);
		}
	}

	public class ItemQuantityComparer : ItemComparer
	{
		public override int Compare(SPItemVM x, SPItemVM y)
		{
			int num = y.ItemCount.CompareTo(x.ItemCount);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemCostComparer : ItemComparer
	{
		public override int Compare(SPItemVM x, SPItemVM y)
		{
			int num = y.ItemCost.CompareTo(x.ItemCost);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private MBBindingList<SPItemVM> _listToControl;

	private ItemTypeComparer _typeComparer;

	private ItemNameComparer _nameComparer;

	private ItemQuantityComparer _quantityComparer;

	private ItemCostComparer _costComparer;

	private int _typeState;

	private int _nameState;

	private int _quantityState;

	private int _costState;

	private bool _isTypeSelected;

	private bool _isNameSelected;

	private bool _isQuantitySelected;

	private bool _isCostSelected;

	public InventoryItemSortOption? CurrentSortOption { get; private set; }

	public InventoryItemSortState? CurrentSortState { get; private set; }

	[DataSourceProperty]
	public int TypeState
	{
		get
		{
			return _typeState;
		}
		set
		{
			if (value != _typeState)
			{
				_typeState = value;
				OnPropertyChangedWithValue(value, "TypeState");
			}
		}
	}

	[DataSourceProperty]
	public int NameState
	{
		get
		{
			return _nameState;
		}
		set
		{
			if (value != _nameState)
			{
				_nameState = value;
				OnPropertyChangedWithValue(value, "NameState");
			}
		}
	}

	[DataSourceProperty]
	public int QuantityState
	{
		get
		{
			return _quantityState;
		}
		set
		{
			if (value != _quantityState)
			{
				_quantityState = value;
				OnPropertyChangedWithValue(value, "QuantityState");
			}
		}
	}

	[DataSourceProperty]
	public int CostState
	{
		get
		{
			return _costState;
		}
		set
		{
			if (value != _costState)
			{
				_costState = value;
				OnPropertyChangedWithValue(value, "CostState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTypeSelected
	{
		get
		{
			return _isTypeSelected;
		}
		set
		{
			if (value != _isTypeSelected)
			{
				_isTypeSelected = value;
				OnPropertyChangedWithValue(value, "IsTypeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNameSelected
	{
		get
		{
			return _isNameSelected;
		}
		set
		{
			if (value != _isNameSelected)
			{
				_isNameSelected = value;
				OnPropertyChangedWithValue(value, "IsNameSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQuantitySelected
	{
		get
		{
			return _isQuantitySelected;
		}
		set
		{
			if (value != _isQuantitySelected)
			{
				_isQuantitySelected = value;
				OnPropertyChangedWithValue(value, "IsQuantitySelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCostSelected
	{
		get
		{
			return _isCostSelected;
		}
		set
		{
			if (value != _isCostSelected)
			{
				_isCostSelected = value;
				OnPropertyChangedWithValue(value, "IsCostSelected");
			}
		}
	}

	public SPInventorySortControllerVM(ref MBBindingList<SPItemVM> listToControl)
	{
		_listToControl = listToControl;
		_typeComparer = new ItemTypeComparer();
		_nameComparer = new ItemNameComparer();
		_quantityComparer = new ItemQuantityComparer();
		_costComparer = new ItemCostComparer();
		RefreshValues();
	}

	public void SortByOption(InventoryItemSortOption sortOption, InventoryItemSortState sortState)
	{
		SetAllStates((sortState != InventoryItemSortState.Ascending) ? InventoryItemSortState.Ascending : InventoryItemSortState.Descending);
		switch (sortOption)
		{
		case InventoryItemSortOption.Type:
			ExecuteSortByType();
			break;
		case InventoryItemSortOption.Name:
			ExecuteSortByName();
			break;
		case InventoryItemSortOption.Quantity:
			ExecuteSortByQuantity();
			break;
		case InventoryItemSortOption.Cost:
			ExecuteSortByCost();
			break;
		}
	}

	public void SortByDefaultState()
	{
		ExecuteSortByType();
	}

	public void SortByCurrentState()
	{
		if (IsTypeSelected)
		{
			_listToControl.Sort(_typeComparer);
			CurrentSortOption = InventoryItemSortOption.Type;
		}
		else if (IsNameSelected)
		{
			_listToControl.Sort(_nameComparer);
			CurrentSortOption = InventoryItemSortOption.Name;
		}
		else if (IsQuantitySelected)
		{
			_listToControl.Sort(_quantityComparer);
			CurrentSortOption = InventoryItemSortOption.Quantity;
		}
		else if (IsCostSelected)
		{
			_listToControl.Sort(_costComparer);
			CurrentSortOption = InventoryItemSortOption.Cost;
		}
	}

	public void ExecuteSortByName()
	{
		int nameState = NameState;
		SetAllStates(InventoryItemSortState.Default);
		NameState = (nameState + 1) % 3;
		if (NameState == 0)
		{
			NameState++;
		}
		_nameComparer.SetSortMode(NameState == 1);
		CurrentSortState = ((NameState == 1) ? InventoryItemSortState.Ascending : InventoryItemSortState.Descending);
		_listToControl.Sort(_nameComparer);
		IsNameSelected = true;
		CurrentSortOption = InventoryItemSortOption.Name;
	}

	public void ExecuteSortByType()
	{
		int typeState = TypeState;
		SetAllStates(InventoryItemSortState.Default);
		TypeState = (typeState + 1) % 3;
		if (TypeState == 0)
		{
			TypeState++;
		}
		_typeComparer.SetSortMode(TypeState == 1);
		CurrentSortState = ((TypeState == 1) ? InventoryItemSortState.Ascending : InventoryItemSortState.Descending);
		_listToControl.Sort(_typeComparer);
		IsTypeSelected = true;
		CurrentSortOption = InventoryItemSortOption.Type;
	}

	public void ExecuteSortByQuantity()
	{
		int quantityState = QuantityState;
		SetAllStates(InventoryItemSortState.Default);
		QuantityState = (quantityState + 1) % 3;
		if (QuantityState == 0)
		{
			QuantityState++;
		}
		_quantityComparer.SetSortMode(QuantityState == 1);
		CurrentSortState = ((QuantityState == 1) ? InventoryItemSortState.Ascending : InventoryItemSortState.Descending);
		_listToControl.Sort(_quantityComparer);
		IsQuantitySelected = true;
		CurrentSortOption = InventoryItemSortOption.Quantity;
	}

	public void ExecuteSortByCost()
	{
		int costState = CostState;
		SetAllStates(InventoryItemSortState.Default);
		CostState = (costState + 1) % 3;
		if (CostState == 0)
		{
			CostState++;
		}
		_costComparer.SetSortMode(CostState == 1);
		CurrentSortState = ((CostState == 1) ? InventoryItemSortState.Ascending : InventoryItemSortState.Descending);
		_listToControl.Sort(_costComparer);
		IsCostSelected = true;
		CurrentSortOption = InventoryItemSortOption.Cost;
	}

	private void SetAllStates(InventoryItemSortState state)
	{
		TypeState = (int)state;
		NameState = (int)state;
		QuantityState = (int)state;
		CostState = (int)state;
		IsTypeSelected = false;
		IsNameSelected = false;
		IsQuantitySelected = false;
		IsCostSelected = false;
		CurrentSortState = state;
	}
}
