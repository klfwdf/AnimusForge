using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;

public class KingdomClanSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<KingdomClanItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(KingdomClanItemVM x, KingdomClanItemVM y);

		protected int ResolveEquality(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			return x.Clan.Name.ToString().CompareTo(y.Clan.Name.ToString());
		}
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			if (_isAscending)
			{
				return y.Clan.Name.ToString().CompareTo(x.Clan.Name.ToString()) * -1;
			}
			return y.Clan.Name.ToString().CompareTo(x.Clan.Name.ToString());
		}
	}

	public class ItemTypeComparer : ItemComparerBase
	{
		public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			int num = y.ClanType.CompareTo(x.ClanType);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemInfluenceComparer : ItemComparerBase
	{
		public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			int num = y.Influence.CompareTo(x.Influence);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemMembersComparer : ItemComparerBase
	{
		public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			int num = y.Members.Count.CompareTo(x.Members.Count);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	public class ItemFiefsComparer : ItemComparerBase
	{
		public override int Compare(KingdomClanItemVM x, KingdomClanItemVM y)
		{
			int num = y.Fiefs.Count.CompareTo(x.Fiefs.Count);
			if (num != 0)
			{
				return num * ((!_isAscending) ? 1 : (-1));
			}
			return ResolveEquality(x, y);
		}
	}

	private readonly MBBindingList<KingdomClanItemVM> _listToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemTypeComparer _typeComparer;

	private readonly ItemInfluenceComparer _influenceComparer;

	private readonly ItemMembersComparer _membersComparer;

	private readonly ItemFiefsComparer _fiefsComparer;

	private int _influenceState;

	private int _fiefsState;

	private int _membersState;

	private int _nameState;

	private int _typeState;

	private bool _isNameSelected;

	private bool _isTypeSelected;

	private bool _isFiefsSelected;

	private bool _isMembersSelected;

	private bool _isDistanceSelected;

	[DataSourceProperty]
	public int InfluenceState
	{
		get
		{
			return _influenceState;
		}
		set
		{
			if (value != _influenceState)
			{
				_influenceState = value;
				OnPropertyChangedWithValue(value, "InfluenceState");
			}
		}
	}

	[DataSourceProperty]
	public int FiefsState
	{
		get
		{
			return _fiefsState;
		}
		set
		{
			if (value != _fiefsState)
			{
				_fiefsState = value;
				OnPropertyChangedWithValue(value, "FiefsState");
			}
		}
	}

	[DataSourceProperty]
	public int MembersState
	{
		get
		{
			return _membersState;
		}
		set
		{
			if (value != _membersState)
			{
				_membersState = value;
				OnPropertyChangedWithValue(value, "MembersState");
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
	public bool IsFiefsSelected
	{
		get
		{
			return _isFiefsSelected;
		}
		set
		{
			if (value != _isFiefsSelected)
			{
				_isFiefsSelected = value;
				OnPropertyChangedWithValue(value, "IsFiefsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMembersSelected
	{
		get
		{
			return _isMembersSelected;
		}
		set
		{
			if (value != _isMembersSelected)
			{
				_isMembersSelected = value;
				OnPropertyChangedWithValue(value, "IsMembersSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInfluenceSelected
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
				OnPropertyChangedWithValue(value, "IsInfluenceSelected");
			}
		}
	}

	public KingdomClanSortControllerVM(ref MBBindingList<KingdomClanItemVM> listToControl)
	{
		_listToControl = listToControl;
		_influenceComparer = new ItemInfluenceComparer();
		_membersComparer = new ItemMembersComparer();
		_nameComparer = new ItemNameComparer();
		_fiefsComparer = new ItemFiefsComparer();
		_typeComparer = new ItemTypeComparer();
	}

	public void SortByCurrentState()
	{
		if (IsNameSelected)
		{
			_listToControl.Sort(_nameComparer);
		}
		else if (IsTypeSelected)
		{
			_listToControl.Sort(_typeComparer);
		}
		else if (IsInfluenceSelected)
		{
			_listToControl.Sort(_influenceComparer);
		}
		else if (IsMembersSelected)
		{
			_listToControl.Sort(_membersComparer);
		}
		else if (IsFiefsSelected)
		{
			_listToControl.Sort(_fiefsComparer);
		}
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

	private void ExecuteSortByInfluence()
	{
		int influenceState = InfluenceState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		InfluenceState = (influenceState + 1) % 3;
		if (InfluenceState == 0)
		{
			InfluenceState++;
		}
		_influenceComparer.SetSortMode(InfluenceState == 1);
		_listToControl.Sort(_influenceComparer);
		IsInfluenceSelected = true;
	}

	private void ExecuteSortByMembers()
	{
		int membersState = MembersState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		MembersState = (membersState + 1) % 3;
		if (MembersState == 0)
		{
			MembersState++;
		}
		_membersComparer.SetSortMode(MembersState == 1);
		_listToControl.Sort(_membersComparer);
		IsMembersSelected = true;
	}

	private void ExecuteSortByFiefs()
	{
		int fiefsState = FiefsState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		FiefsState = (fiefsState + 1) % 3;
		if (FiefsState == 0)
		{
			FiefsState++;
		}
		_fiefsComparer.SetSortMode(FiefsState == 1);
		_listToControl.Sort(_fiefsComparer);
		IsFiefsSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		InfluenceState = (int)state;
		FiefsState = (int)state;
		MembersState = (int)state;
		NameState = (int)state;
		TypeState = (int)state;
		IsInfluenceSelected = false;
		IsFiefsSelected = false;
		IsNameSelected = false;
		IsMembersSelected = false;
		IsTypeSelected = false;
	}
}
