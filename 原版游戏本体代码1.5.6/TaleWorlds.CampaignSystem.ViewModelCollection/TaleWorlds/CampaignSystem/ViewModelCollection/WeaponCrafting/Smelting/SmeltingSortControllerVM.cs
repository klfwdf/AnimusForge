using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;

public class SmeltingSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<SmeltingItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(SmeltingItemVM x, SmeltingItemVM y);

		protected int ResolveEquality(SmeltingItemVM x, SmeltingItemVM y)
		{
			return x.Name.CompareTo(y.Name);
		}
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
		{
			if (_isAscending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class ItemYieldComparer : ItemComparerBase
	{
		public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
		{
			int num = y.Yield.Count.CompareTo(x.Yield.Count);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemTypeComparer : ItemComparerBase
	{
		public override int Compare(SmeltingItemVM x, SmeltingItemVM y)
		{
			int itemObjectTypeSortIndex = CampaignUIHelper.GetItemObjectTypeSortIndex(x.EquipmentElement.Item);
			int num = CampaignUIHelper.GetItemObjectTypeSortIndex(y.EquipmentElement.Item).CompareTo(itemObjectTypeSortIndex);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private MBBindingList<SmeltingItemVM> _listToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemYieldComparer _yieldComparer;

	private readonly ItemTypeComparer _typeComparer;

	private int _nameState;

	private int _yieldState;

	private int _typeState;

	private bool _isNameSelected;

	private bool _isYieldSelected;

	private bool _isTypeSelected;

	private string _sortTypeText;

	private string _sortNameText;

	private string _sortYieldText;

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
	public int YieldState
	{
		get
		{
			return _yieldState;
		}
		set
		{
			if (value != _yieldState)
			{
				_yieldState = value;
				OnPropertyChangedWithValue(value, "YieldState");
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
	public bool IsYieldSelected
	{
		get
		{
			return _isYieldSelected;
		}
		set
		{
			if (value != _isYieldSelected)
			{
				_isYieldSelected = value;
				OnPropertyChangedWithValue(value, "IsYieldSelected");
			}
		}
	}

	[DataSourceProperty]
	public string SortTypeText
	{
		get
		{
			return _sortTypeText;
		}
		set
		{
			if (value != _sortTypeText)
			{
				_sortTypeText = value;
				OnPropertyChangedWithValue(value, "SortTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string SortNameText
	{
		get
		{
			return _sortNameText;
		}
		set
		{
			if (value != _sortNameText)
			{
				_sortNameText = value;
				OnPropertyChangedWithValue(value, "SortNameText");
			}
		}
	}

	[DataSourceProperty]
	public string SortYieldText
	{
		get
		{
			return _sortYieldText;
		}
		set
		{
			if (value != _sortYieldText)
			{
				_sortYieldText = value;
				OnPropertyChangedWithValue(value, "SortYieldText");
			}
		}
	}

	public SmeltingSortControllerVM()
	{
		_yieldComparer = new ItemYieldComparer();
		_typeComparer = new ItemTypeComparer();
		_nameComparer = new ItemNameComparer();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SortNameText = new TextObject("{=PDdh1sBj}Name").ToString();
		SortTypeText = new TextObject("{=zMMqgxb1}Type").ToString();
		SortYieldText = new TextObject("{=v3OF6vBg}Yield").ToString();
	}

	public void SetListToControl(MBBindingList<SmeltingItemVM> listToControl)
	{
		_listToControl = listToControl;
	}

	public void SortByCurrentState()
	{
		if (IsNameSelected)
		{
			_listToControl.Sort(_nameComparer);
		}
		else if (IsYieldSelected)
		{
			_listToControl.Sort(_yieldComparer);
		}
		else if (IsTypeSelected)
		{
			_listToControl.Sort(_typeComparer);
		}
	}

	public void ExecuteSortByName()
	{
		int nameState = NameState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		NameState = (nameState + 1) % 3;
		if (NameState == 0)
		{
			NameState++;
		}
		_nameComparer.SetSortMode(NameState == 1);
		_listToControl.Sort(_nameComparer);
		IsNameSelected = true;
	}

	public void ExecuteSortByYield()
	{
		int yieldState = YieldState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		YieldState = (yieldState + 1) % 3;
		if (YieldState == 0)
		{
			YieldState++;
		}
		_yieldComparer.SetSortMode(YieldState == 1);
		_listToControl.Sort(_yieldComparer);
		IsYieldSelected = true;
	}

	public void ExecuteSortByType()
	{
		int typeState = TypeState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		TypeState = (typeState + 1) % 3;
		if (TypeState == 0)
		{
			TypeState++;
		}
		_typeComparer.SetSortMode(TypeState == 1);
		_listToControl.Sort(_typeComparer);
		IsTypeSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		TypeState = (int)state;
		YieldState = (int)state;
		IsNameSelected = false;
		IsTypeSelected = false;
		IsYieldSelected = false;
	}
}
