using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

public class ClanMembersSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<ClanLordItemVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanLordItemVM x, ClanLordItemVM y);
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(ClanLordItemVM x, ClanLordItemVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class ItemLocationComparer : ItemComparerBase
	{
		public override int Compare(ClanLordItemVM x, ClanLordItemVM y)
		{
			int num = GetDistanceToMainHero(y).CompareTo(GetDistanceToMainHero(x));
			if (_isAcending)
			{
				return num * -1;
			}
			return num;
		}

		private float GetDistanceToMainHero(ClanLordItemVM item)
		{
			return item.GetHero().GetCampaignPosition().Distance(Hero.MainHero.GetCampaignPosition());
		}
	}

	private readonly MBBindingList<MBBindingList<ClanLordItemVM>> _listsToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemLocationComparer _locationComparer;

	private int _nameState;

	private int _locationState;

	private bool _isNameSelected;

	private bool _isLocationSelected;

	private string _nameText;

	private string _locationText;

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
	public int LocationState
	{
		get
		{
			return _locationState;
		}
		set
		{
			if (value != _locationState)
			{
				_locationState = value;
				OnPropertyChangedWithValue(value, "LocationState");
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
	public bool IsLocationSelected
	{
		get
		{
			return _isLocationSelected;
		}
		set
		{
			if (value != _isLocationSelected)
			{
				_isLocationSelected = value;
				OnPropertyChangedWithValue(value, "IsLocationSelected");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string LocationText
	{
		get
		{
			return _locationText;
		}
		set
		{
			if (value != _locationText)
			{
				_locationText = value;
				OnPropertyChangedWithValue(value, "LocationText");
			}
		}
	}

	public ClanMembersSortControllerVM(MBBindingList<MBBindingList<ClanLordItemVM>> listsToControl)
	{
		_listsToControl = listsToControl;
		_nameComparer = new ItemNameComparer();
		_locationComparer = new ItemLocationComparer();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = GameTexts.FindText("str_sort_by_name_label").ToString();
		LocationText = GameTexts.FindText("str_tooltip_label_location").ToString();
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
		foreach (MBBindingList<ClanLordItemVM> item in _listsToControl)
		{
			item.Sort(_nameComparer);
		}
		IsNameSelected = true;
	}

	public void ExecuteSortByLocation()
	{
		int locationState = LocationState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		LocationState = (locationState + 1) % 3;
		if (LocationState == 0)
		{
			LocationState++;
		}
		_locationComparer.SetSortMode(LocationState == 1);
		foreach (MBBindingList<ClanLordItemVM> item in _listsToControl)
		{
			item.Sort(_locationComparer);
		}
		IsLocationSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		LocationState = (int)state;
		IsNameSelected = false;
		IsLocationSelected = false;
	}

	public void ResetAllStates()
	{
		SetAllStates(CampaignUIHelper.SortState.Default);
	}
}
