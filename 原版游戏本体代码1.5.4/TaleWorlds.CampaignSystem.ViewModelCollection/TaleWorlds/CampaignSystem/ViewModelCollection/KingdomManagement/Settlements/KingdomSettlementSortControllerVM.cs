using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

public class KingdomSettlementSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<KingdomSettlementItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y);

		protected int ResolveEquality(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			return x.Settlement.Name.ToString().CompareTo(y.Settlement.Name.ToString());
		}
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			if (_isAscending)
			{
				return y.Settlement.Name.ToString().CompareTo(x.Settlement.Name.ToString()) * -1;
			}
			return y.Settlement.Name.ToString().CompareTo(x.Settlement.Name.ToString());
		}
	}

	public class ItemClanComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Settlement.OwnerClan.Name.ToString().CompareTo(x.Settlement.OwnerClan.Name.ToString());
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemOwnerComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Owner.NameText.CompareTo(x.Owner.NameText);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemVillagesComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Villages.Count.CompareTo(x.Villages.Count);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemTypeComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Settlement.IsCastle.CompareTo(x.Settlement.IsCastle);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemProsperityComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Prosperity.CompareTo(x.Prosperity);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemFoodComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			float num = ((y.Settlement.Town != null) ? y.Settlement.Town.FoodStocks : 0f);
			float value = ((x.Settlement.Town != null) ? x.Settlement.Town.FoodStocks : 0f);
			int num2 = num.CompareTo(value);
			if (num2 != 0)
			{
				return num2 * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemGarrisonComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Garrison.CompareTo(x.Garrison);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private class ItemDefendersComparer : ItemComparerBase
	{
		public override int Compare(KingdomSettlementItemVM x, KingdomSettlementItemVM y)
		{
			int num = y.Defenders.CompareTo(x.Defenders);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private readonly MBBindingList<KingdomSettlementItemVM> _listToControl;

	private readonly ItemTypeComparer _typeComparer;

	private readonly ItemProsperityComparer _prosperityComparer;

	private readonly ItemDefendersComparer _defendersComparer;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemOwnerComparer _ownerComparer;

	private int _typeState;

	private int _nameState;

	private int _ownerState;

	private int _prosperityState;

	private int _defendersState;

	private bool _isTypeSelected;

	private bool _isNameSelected;

	private bool _isOwnerSelected;

	private bool _isProsperitySelected;

	private bool _isDefendersSelected;

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
	public int OwnerState
	{
		get
		{
			return _ownerState;
		}
		set
		{
			if (value != _ownerState)
			{
				_ownerState = value;
				OnPropertyChangedWithValue(value, "OwnerState");
			}
		}
	}

	[DataSourceProperty]
	public int ProsperityState
	{
		get
		{
			return _prosperityState;
		}
		set
		{
			if (value != _prosperityState)
			{
				_prosperityState = value;
				OnPropertyChangedWithValue(value, "ProsperityState");
			}
		}
	}

	[DataSourceProperty]
	public int DefendersState
	{
		get
		{
			return _defendersState;
		}
		set
		{
			if (value != _defendersState)
			{
				_defendersState = value;
				OnPropertyChangedWithValue(value, "DefendersState");
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
	public bool IsDefendersSelected
	{
		get
		{
			return _isDefendersSelected;
		}
		set
		{
			if (value != _isDefendersSelected)
			{
				_isDefendersSelected = value;
				OnPropertyChangedWithValue(value, "IsDefendersSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOwnerSelected
	{
		get
		{
			return _isOwnerSelected;
		}
		set
		{
			if (value != _isOwnerSelected)
			{
				_isOwnerSelected = value;
				OnPropertyChangedWithValue(value, "IsOwnerSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsProsperitySelected
	{
		get
		{
			return _isProsperitySelected;
		}
		set
		{
			if (value != _isProsperitySelected)
			{
				_isProsperitySelected = value;
				OnPropertyChangedWithValue(value, "IsProsperitySelected");
			}
		}
	}

	public KingdomSettlementSortControllerVM(MBBindingList<KingdomSettlementItemVM> listToControl)
	{
		_listToControl = listToControl;
		_typeComparer = new ItemTypeComparer();
		_prosperityComparer = new ItemProsperityComparer();
		_defendersComparer = new ItemDefendersComparer();
		_ownerComparer = new ItemOwnerComparer();
		_nameComparer = new ItemNameComparer();
	}

	private void ExecuteSortByType()
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

	private void ExecuteSortByName()
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

	private void ExecuteSortByOwner()
	{
		int ownerState = OwnerState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		OwnerState = (ownerState + 1) % 3;
		if (OwnerState == 0)
		{
			OwnerState++;
		}
		_ownerComparer.SetSortMode(OwnerState == 1);
		_listToControl.Sort(_ownerComparer);
		IsOwnerSelected = true;
	}

	private void ExecuteSortByProsperity()
	{
		int prosperityState = ProsperityState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		ProsperityState = (prosperityState + 1) % 3;
		if (ProsperityState == 0)
		{
			ProsperityState++;
		}
		_prosperityComparer.SetSortMode(ProsperityState == 1);
		_listToControl.Sort(_prosperityComparer);
		IsProsperitySelected = true;
	}

	private void ExecuteSortByDefenders()
	{
		int defendersState = DefendersState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		DefendersState = (defendersState + 1) % 3;
		if (DefendersState == 0)
		{
			DefendersState++;
		}
		_defendersComparer.SetSortMode(DefendersState == 1);
		_listToControl.Sort(_defendersComparer);
		IsDefendersSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		TypeState = (int)state;
		NameState = (int)state;
		OwnerState = (int)state;
		ProsperityState = (int)state;
		DefendersState = (int)state;
		IsTypeSelected = false;
		IsNameSelected = false;
		IsProsperitySelected = false;
		IsOwnerSelected = false;
		IsDefendersSelected = false;
	}
}
