using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanLeaderboardSortControllerVM : ViewModel
{
	private enum SortState
	{
		Default,
		Ascending,
		Descending
	}

	private abstract class ItemComparerBase : IComparer<ClanLeaderboardEntry>
	{
		protected bool _isAcending;

		public void SetSortMode(bool isAcending)
		{
			_isAcending = isAcending;
		}

		public abstract int Compare(ClanLeaderboardEntry x, ClanLeaderboardEntry y);
	}

	private class ItemNameComparer : ItemComparerBase
	{
		public override int Compare(ClanLeaderboardEntry x, ClanLeaderboardEntry y)
		{
			if (_isAcending)
			{
				return y.Name.CompareTo(x.Name) * -1;
			}
			return y.Name.CompareTo(x.Name);
		}
	}

	private class ItemWinComparer : ItemComparerBase
	{
		public override int Compare(ClanLeaderboardEntry x, ClanLeaderboardEntry y)
		{
			if (_isAcending)
			{
				return y.WinCount.CompareTo(x.WinCount) * -1;
			}
			return y.WinCount.CompareTo(x.WinCount);
		}
	}

	private class ItemLossComparer : ItemComparerBase
	{
		public override int Compare(ClanLeaderboardEntry x, ClanLeaderboardEntry y)
		{
			if (_isAcending)
			{
				return y.LossCount.CompareTo(x.LossCount) * -1;
			}
			return y.LossCount.CompareTo(x.LossCount);
		}
	}

	private readonly ClanLeaderboardEntry[] _listToControl;

	private readonly ItemNameComparer _nameComparer;

	private readonly ItemWinComparer _winComparer;

	private readonly ItemLossComparer _lossComparer;

	private Action _onSorted;

	private int _nameState;

	private int _winState;

	private int _lossState;

	private bool _isNameSelected;

	private bool _isWinSelected;

	private bool _isLossSelected;

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
	public int WinState
	{
		get
		{
			return _winState;
		}
		set
		{
			if (value != _winState)
			{
				_winState = value;
				OnPropertyChangedWithValue(value, "WinState");
			}
		}
	}

	[DataSourceProperty]
	public int LossState
	{
		get
		{
			return _lossState;
		}
		set
		{
			if (value != _lossState)
			{
				_lossState = value;
				OnPropertyChangedWithValue(value, "LossState");
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
	public bool IsWinSelected
	{
		get
		{
			return _isWinSelected;
		}
		set
		{
			if (value != _isWinSelected)
			{
				_isWinSelected = value;
				OnPropertyChangedWithValue(value, "IsWinSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLossSelected
	{
		get
		{
			return _isLossSelected;
		}
		set
		{
			if (value != _isLossSelected)
			{
				_isLossSelected = value;
				OnPropertyChangedWithValue(value, "IsLossSelected");
			}
		}
	}

	public MPLobbyClanLeaderboardSortControllerVM(ref ClanLeaderboardEntry[] listToControl, Action onSorted)
	{
		_listToControl = listToControl;
		_winComparer = new ItemWinComparer();
		_lossComparer = new ItemLossComparer();
		_nameComparer = new ItemNameComparer();
		_onSorted = onSorted;
	}

	private void ExecuteSortByName()
	{
		int nameState = NameState;
		SetAllStates(SortState.Default);
		NameState = (nameState + 1) % 3;
		if (NameState == 0)
		{
			NameState++;
		}
		_nameComparer.SetSortMode(NameState == 1);
		Array.Sort(_listToControl, _nameComparer);
		IsNameSelected = true;
		_onSorted();
	}

	private void ExecuteSortByWin()
	{
		int winState = WinState;
		SetAllStates(SortState.Default);
		WinState = (winState + 1) % 3;
		if (WinState == 0)
		{
			WinState++;
		}
		_winComparer.SetSortMode(WinState == 1);
		Array.Sort(_listToControl, _winComparer);
		IsWinSelected = true;
		_onSorted();
	}

	private void ExecuteSortByLoss()
	{
		int lossState = LossState;
		SetAllStates(SortState.Default);
		LossState = (lossState + 1) % 3;
		if (LossState == 0)
		{
			LossState++;
		}
		_lossComparer.SetSortMode(LossState == 1);
		Array.Sort(_listToControl, _lossComparer);
		IsLossSelected = true;
		_onSorted();
	}

	private void SetAllStates(SortState state)
	{
		NameState = (int)state;
		WinState = (int)state;
		LossState = (int)state;
		IsNameSelected = false;
		IsWinSelected = false;
		IsLossSelected = false;
	}
}
