using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardSortControllerVM : ViewModel
{
	private enum SortState
	{
		Default,
		Ascending,
		Descending
	}

	public abstract class ScoreboardUnitItemComparerBase : IComparer<SPScoreboardUnitVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y);
	}

	public class ItemRemainingComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.Remaining.CompareTo(x.Score.Remaining) * -1;
			}
			return y.Score.Remaining.CompareTo(x.Score.Remaining);
		}
	}

	public class ItemKillComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.Kill.CompareTo(x.Score.Kill) * -1;
			}
			return y.Score.Kill.CompareTo(x.Score.Kill);
		}
	}

	public class ItemUpgradeComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.ReadyToUpgrade.CompareTo(x.Score.ReadyToUpgrade) * -1;
			}
			return y.Score.ReadyToUpgrade.CompareTo(x.Score.ReadyToUpgrade);
		}
	}

	public class ItemDeadComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.Dead.CompareTo(x.Score.Dead) * -1;
			}
			return y.Score.Dead.CompareTo(x.Score.Dead);
		}
	}

	public class ItemWoundedComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.Wounded.CompareTo(x.Score.Wounded) * -1;
			}
			return y.Score.Wounded.CompareTo(x.Score.Wounded);
		}
	}

	public class ItemRoutedComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (_isAscending)
			{
				return y.Score.Routed.CompareTo(x.Score.Routed) * -1;
			}
			return y.Score.Routed.CompareTo(x.Score.Routed);
		}
	}

	public class ItemMemberComparer : ScoreboardUnitItemComparerBase
	{
		public override int Compare(SPScoreboardUnitVM x, SPScoreboardUnitVM y)
		{
			if (x.Character.IsPlayerCharacter && !y.Character.IsPlayerCharacter)
			{
				return -1;
			}
			if (!x.Character.IsPlayerCharacter && y.Character.IsPlayerCharacter)
			{
				return 1;
			}
			if (x.IsHero && !y.IsHero)
			{
				return -1;
			}
			if (!x.IsHero && y.IsHero)
			{
				return 1;
			}
			return x.Character.Name.ToString().CompareTo(y.Character.Name.ToString());
		}
	}

	private readonly MBBindingList<SPScoreboardPartyVM> _listToControl;

	private readonly ItemRemainingComparer _remainingComparer;

	private readonly ItemKillComparer _killComparer;

	private readonly ItemUpgradeComparer _upgradeComparer;

	private readonly ItemDeadComparer _deadComparer;

	private readonly ItemWoundedComparer _woundedComparer;

	private readonly ItemRoutedComparer _routedComparer;

	private readonly ItemMemberComparer _memberComparer;

	private int _remainingState;

	private bool _isRemainingSelected;

	private int _killState;

	private bool _isKillSelected;

	private int _upgradeState;

	private bool _isUpgradeSelected;

	private int _deadState;

	private bool _isDeadSelected;

	private int _woundedState;

	private bool _isWoundedSelected;

	private int _routedState;

	private bool _isRoutedSelected;

	[DataSourceProperty]
	public int RemainingState
	{
		get
		{
			return _remainingState;
		}
		set
		{
			if (value != _remainingState)
			{
				_remainingState = value;
				OnPropertyChanged("RemainingState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRemainingSelected
	{
		get
		{
			return _isRemainingSelected;
		}
		set
		{
			if (value != _isRemainingSelected)
			{
				_isRemainingSelected = value;
				OnPropertyChanged("IsRemainingSelected");
			}
		}
	}

	[DataSourceProperty]
	public int KillState
	{
		get
		{
			return _killState;
		}
		set
		{
			if (value != _killState)
			{
				_killState = value;
				OnPropertyChanged("KillState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKillSelected
	{
		get
		{
			return _isKillSelected;
		}
		set
		{
			if (value != _isKillSelected)
			{
				_isKillSelected = value;
				OnPropertyChanged("IsKillSelected");
			}
		}
	}

	[DataSourceProperty]
	public int UpgradeState
	{
		get
		{
			return _upgradeState;
		}
		set
		{
			if (value != _upgradeState)
			{
				_upgradeState = value;
				OnPropertyChanged("UpgradeState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpgradeSelected
	{
		get
		{
			return _isUpgradeSelected;
		}
		set
		{
			if (value != _isUpgradeSelected)
			{
				_isUpgradeSelected = value;
				OnPropertyChanged("IsUpgradeSelected");
			}
		}
	}

	[DataSourceProperty]
	public int DeadState
	{
		get
		{
			return _deadState;
		}
		set
		{
			if (value != _deadState)
			{
				_deadState = value;
				OnPropertyChanged("DeadState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDeadSelected
	{
		get
		{
			return _isDeadSelected;
		}
		set
		{
			if (value != _isDeadSelected)
			{
				_isDeadSelected = value;
				OnPropertyChanged("IsDeadSelected");
			}
		}
	}

	[DataSourceProperty]
	public int WoundedState
	{
		get
		{
			return _woundedState;
		}
		set
		{
			if (value != _woundedState)
			{
				_woundedState = value;
				OnPropertyChanged("WoundedState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWoundedSelected
	{
		get
		{
			return _isWoundedSelected;
		}
		set
		{
			if (value != _isWoundedSelected)
			{
				_isWoundedSelected = value;
				OnPropertyChanged("IsWoundedSelected");
			}
		}
	}

	[DataSourceProperty]
	public int RoutedState
	{
		get
		{
			return _routedState;
		}
		set
		{
			if (value != _routedState)
			{
				_routedState = value;
				OnPropertyChanged("RoutedState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoutedSelected
	{
		get
		{
			return _isRoutedSelected;
		}
		set
		{
			if (value != _isRoutedSelected)
			{
				_isRoutedSelected = value;
				OnPropertyChanged("IsRoutedSelected");
			}
		}
	}

	public SPScoreboardSortControllerVM(ref MBBindingList<SPScoreboardPartyVM> listToControl)
	{
		_listToControl = listToControl;
		_remainingComparer = new ItemRemainingComparer();
		_killComparer = new ItemKillComparer();
		_upgradeComparer = new ItemUpgradeComparer();
		_deadComparer = new ItemDeadComparer();
		_woundedComparer = new ItemWoundedComparer();
		_routedComparer = new ItemRoutedComparer();
		_memberComparer = new ItemMemberComparer();
	}

	public void ExecuteSortByRemaining()
	{
		int remainingState = RemainingState;
		SetAllStates(SortState.Default);
		RemainingState = (remainingState + 1) % 3;
		_remainingComparer.SetSortMode(RemainingState == 1);
		ScoreboardUnitItemComparerBase comparer = _remainingComparer;
		if (RemainingState == 0)
		{
			comparer = _memberComparer;
		}
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsRemainingSelected = RemainingState != 0;
	}

	public void ExecuteSortByKill()
	{
		int killState = KillState;
		SetAllStates(SortState.Default);
		KillState = (killState + 1) % 3;
		ScoreboardUnitItemComparerBase comparer = _killComparer;
		if (KillState == 0)
		{
			comparer = _memberComparer;
		}
		_killComparer.SetSortMode(KillState == 1);
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsKillSelected = KillState != 0;
	}

	public void ExecuteSortByUpgrade()
	{
		int upgradeState = UpgradeState;
		SetAllStates(SortState.Default);
		UpgradeState = (upgradeState + 1) % 3;
		ScoreboardUnitItemComparerBase comparer = _upgradeComparer;
		if (UpgradeState == 0)
		{
			comparer = _memberComparer;
		}
		_upgradeComparer.SetSortMode(UpgradeState == 1);
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsUpgradeSelected = UpgradeState != 0;
	}

	public void ExecuteSortByDead()
	{
		int deadState = DeadState;
		SetAllStates(SortState.Default);
		DeadState = (deadState + 1) % 3;
		ScoreboardUnitItemComparerBase comparer = _deadComparer;
		if (DeadState == 0)
		{
			comparer = _memberComparer;
		}
		_deadComparer.SetSortMode(DeadState == 1);
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsDeadSelected = DeadState != 0;
	}

	public void ExecuteSortByWounded()
	{
		int woundedState = WoundedState;
		SetAllStates(SortState.Default);
		WoundedState = (woundedState + 1) % 3;
		ScoreboardUnitItemComparerBase comparer = _woundedComparer;
		if (WoundedState == 0)
		{
			comparer = _memberComparer;
		}
		_woundedComparer.SetSortMode(WoundedState == 1);
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsWoundedSelected = WoundedState != 0;
	}

	public void ExecuteSortByRouted()
	{
		int routedState = RoutedState;
		SetAllStates(SortState.Default);
		RoutedState = (routedState + 1) % 3;
		ScoreboardUnitItemComparerBase comparer = _routedComparer;
		if (RoutedState == 0)
		{
			comparer = _memberComparer;
		}
		_routedComparer.SetSortMode(RoutedState == 1);
		foreach (SPScoreboardPartyVM item in _listToControl)
		{
			item.Members.Sort(comparer);
		}
		IsRoutedSelected = RoutedState != 0;
	}

	private void SetAllStates(SortState state)
	{
		RemainingState = (int)state;
		KillState = (int)state;
		UpgradeState = (int)state;
		DeadState = (int)state;
		WoundedState = (int)state;
		RoutedState = (int)state;
		IsRemainingSelected = false;
		IsKillSelected = false;
		IsUpgradeSelected = false;
		IsDeadSelected = false;
		IsWoundedSelected = false;
		IsRoutedSelected = false;
	}
}
