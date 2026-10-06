using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard;

public class TournamentLeaderboardSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<TournamentLeaderboardEntryItemVM>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y);
	}

	public class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	public class ItemPrizeComparer : ItemComparerBase
	{
		public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
		{
			if (_isAcending)
			{
				return y.PrizeValue.CompareTo(x.PrizeValue) * -1;
			}
			return y.PrizeValue.CompareTo(x.PrizeValue);
		}
	}

	public class ItemPlacementComparer : ItemComparerBase
	{
		public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
		{
			if (_isAcending)
			{
				return y.PlacementOnLeaderboard.CompareTo(x.PlacementOnLeaderboard) * -1;
			}
			return y.PlacementOnLeaderboard.CompareTo(x.PlacementOnLeaderboard);
		}
	}

	public class ItemVictoriesComparer : ItemComparerBase
	{
		public override int Compare(TournamentLeaderboardEntryItemVM x, TournamentLeaderboardEntryItemVM y)
		{
			if (_isAcending)
			{
				return y.Victories.CompareTo(x.Victories) * -1;
			}
			return y.Victories.CompareTo(x.Victories);
		}
	}

	private readonly MBBindingList<TournamentLeaderboardEntryItemVM> _listToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemPrizeComparer _prizeComparer;

	private readonly ItemPlacementComparer _placementComparer;

	private readonly ItemVictoriesComparer _victoriesComparer;

	private int _nameState;

	private int _prizeState;

	private int _placementState;

	private int _victoriesState;

	private bool _isNameSelected;

	private bool _isPrizeSelected;

	private bool _isPlacementSelected;

	private bool _isVictoriesSelected;

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
	public int VictoriesState
	{
		get
		{
			return _victoriesState;
		}
		set
		{
			if (value != _victoriesState)
			{
				_victoriesState = value;
				OnPropertyChangedWithValue(value, "VictoriesState");
			}
		}
	}

	[DataSourceProperty]
	public int PrizeState
	{
		get
		{
			return _prizeState;
		}
		set
		{
			if (value != _prizeState)
			{
				_prizeState = value;
				OnPropertyChangedWithValue(value, "PrizeState");
			}
		}
	}

	[DataSourceProperty]
	public int PlacementState
	{
		get
		{
			return _placementState;
		}
		set
		{
			if (value != _placementState)
			{
				_placementState = value;
				OnPropertyChangedWithValue(value, "PlacementState");
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
	public bool IsPrizeSelected
	{
		get
		{
			return _isPrizeSelected;
		}
		set
		{
			if (value != _isPrizeSelected)
			{
				_isPrizeSelected = value;
				OnPropertyChangedWithValue(value, "IsPrizeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlacementSelected
	{
		get
		{
			return _isPlacementSelected;
		}
		set
		{
			if (value != _isPlacementSelected)
			{
				_isPlacementSelected = value;
				OnPropertyChangedWithValue(value, "IsPlacementSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVictoriesSelected
	{
		get
		{
			return _isVictoriesSelected;
		}
		set
		{
			if (value != _isVictoriesSelected)
			{
				_isVictoriesSelected = value;
				OnPropertyChangedWithValue(value, "IsVictoriesSelected");
			}
		}
	}

	public TournamentLeaderboardSortControllerVM(ref MBBindingList<TournamentLeaderboardEntryItemVM> listToControl)
	{
		_listToControl = listToControl;
		_prizeComparer = new ItemPrizeComparer();
		_nameComparer = new ItemNameComparer();
		_placementComparer = new ItemPlacementComparer();
		_victoriesComparer = new ItemVictoriesComparer();
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

	public void ExecuteSortByPrize()
	{
		int prizeState = PrizeState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		PrizeState = (prizeState + 1) % 3;
		if (PrizeState == 0)
		{
			PrizeState++;
		}
		_prizeComparer.SetSortMode(PrizeState == 1);
		_listToControl.Sort(_prizeComparer);
		IsPrizeSelected = true;
	}

	public void ExecuteSortByPlacement()
	{
		int placementState = PlacementState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		PlacementState = (placementState + 1) % 3;
		if (PlacementState == 0)
		{
			PlacementState++;
		}
		_placementComparer.SetSortMode(PlacementState == 1);
		_listToControl.Sort(_placementComparer);
		IsPlacementSelected = true;
	}

	public void ExecuteSortByVictories()
	{
		int victoriesState = VictoriesState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		VictoriesState = (victoriesState + 1) % 3;
		if (VictoriesState == 0)
		{
			VictoriesState++;
		}
		_victoriesComparer.SetSortMode(VictoriesState == 1);
		_listToControl.Sort(_victoriesComparer);
		IsVictoriesSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		NameState = (int)state;
		PrizeState = (int)state;
		PlacementState = (int)state;
		VictoriesState = (int)state;
		IsNameSelected = false;
		IsVictoriesSelected = false;
		IsPrizeSelected = false;
		IsPlacementSelected = false;
	}
}
