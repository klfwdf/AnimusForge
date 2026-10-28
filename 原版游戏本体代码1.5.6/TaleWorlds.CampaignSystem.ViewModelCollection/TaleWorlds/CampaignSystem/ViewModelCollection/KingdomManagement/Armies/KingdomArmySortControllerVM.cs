using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;

public class KingdomArmySortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<KingdomArmyItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y);

		protected int ResolveEquality(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			return x.ArmyName.CompareTo(y.ArmyName);
		}
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			if (_isAscending)
			{
				return y.ArmyName.CompareTo(x.ArmyName) * -1;
			}
			return y.ArmyName.CompareTo(x.ArmyName);
		}
	}

	public class ItemOwnerComparer : ItemComparerBase
	{
		public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			int num = y.Leader.NameText.ToString().CompareTo(x.Leader.NameText.ToString());
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemStrengthComparer : ItemComparerBase
	{
		public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			int num = y.Strength.CompareTo(x.Strength);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemPartiesComparer : ItemComparerBase
	{
		public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			int num = y.Parties.Count.CompareTo(x.Parties.Count);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemDistanceComparer : ItemComparerBase
	{
		public override int Compare(KingdomArmyItemVM x, KingdomArmyItemVM y)
		{
			int num = y.DistanceToMainParty.CompareTo(x.DistanceToMainParty);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private readonly MBBindingList<KingdomArmyItemVM> _listToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemOwnerComparer _ownerComparer;

	private readonly ItemStrengthComparer _strengthComparer;

	private readonly ItemPartiesComparer _partiesComparer;

	private readonly ItemDistanceComparer _distanceComparer;

	private int _nameState;

	private int _ownerState;

	private int _strengthState;

	private int _partiesState;

	private int _distanceState;

	private bool _isNameSelected;

	private bool _isOwnerSelected;

	private bool _isStrengthSelected;

	private bool _isPartiesSelected;

	private bool _isDistanceSelected;

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
	public int PartiesState
	{
		get
		{
			return _partiesState;
		}
		set
		{
			if (value != _partiesState)
			{
				_partiesState = value;
				OnPropertyChangedWithValue(value, "PartiesState");
			}
		}
	}

	[DataSourceProperty]
	public int StrengthState
	{
		get
		{
			return _strengthState;
		}
		set
		{
			if (value != _strengthState)
			{
				_strengthState = value;
				OnPropertyChangedWithValue(value, "StrengthState");
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
	public int DistanceState
	{
		get
		{
			return _distanceState;
		}
		set
		{
			if (value != _distanceState)
			{
				_distanceState = value;
				OnPropertyChangedWithValue(value, "DistanceState");
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
	public bool IsPartiesSelected
	{
		get
		{
			return _isPartiesSelected;
		}
		set
		{
			if (value != _isPartiesSelected)
			{
				_isPartiesSelected = value;
				OnPropertyChangedWithValue(value, "IsPartiesSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsStrengthSelected
	{
		get
		{
			return _isStrengthSelected;
		}
		set
		{
			if (value != _isStrengthSelected)
			{
				_isStrengthSelected = value;
				OnPropertyChangedWithValue(value, "IsStrengthSelected");
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
	public bool IsDistanceSelected
	{
		get
		{
			return _isDistanceSelected;
		}
		set
		{
			if (value != _isDistanceSelected)
			{
				_isDistanceSelected = value;
				OnPropertyChangedWithValue(value, "IsDistanceSelected");
			}
		}
	}

	public KingdomArmySortControllerVM(ref MBBindingList<KingdomArmyItemVM> listToControl)
	{
		_listToControl = listToControl;
		_ownerComparer = new ItemOwnerComparer();
		_strengthComparer = new ItemStrengthComparer();
		_nameComparer = new ItemNameComparer();
		_partiesComparer = new ItemPartiesComparer();
		_distanceComparer = new ItemDistanceComparer();
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

	private void ExecuteSortByStrength()
	{
		int strengthState = StrengthState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		StrengthState = (strengthState + 1) % 3;
		if (StrengthState == 0)
		{
			StrengthState++;
		}
		_strengthComparer.SetSortMode(StrengthState == 1);
		_listToControl.Sort(_strengthComparer);
		IsStrengthSelected = true;
	}

	private void ExecuteSortByParties()
	{
		int partiesState = PartiesState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		PartiesState = (partiesState + 1) % 3;
		if (PartiesState == 0)
		{
			PartiesState++;
		}
		_partiesComparer.SetSortMode(PartiesState == 1);
		_listToControl.Sort(_partiesComparer);
		IsPartiesSelected = true;
	}

	private void ExecuteSortByDistance()
	{
		int distanceState = DistanceState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		DistanceState = (distanceState + 1) % 3;
		if (DistanceState == 0)
		{
			DistanceState++;
		}
		_distanceComparer.SetSortMode(DistanceState == 1);
		_listToControl.Sort(_distanceComparer);
		IsDistanceSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		OwnerState = (int)state;
		StrengthState = (int)state;
		PartiesState = (int)state;
		DistanceState = (int)state;
		IsNameSelected = false;
		IsOwnerSelected = false;
		IsStrengthSelected = false;
		IsPartiesSelected = false;
		IsDistanceSelected = false;
	}
}
