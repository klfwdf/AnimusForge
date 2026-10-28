using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public class MPCustomGameSortControllerVM : ViewModel
{
	public enum SortState
	{
		Default,
		Ascending,
		Descending
	}

	public enum CustomServerSortOption
	{
		SortOptionsBeginExclusive = -1,
		Name,
		GameType,
		PlayerCount,
		PasswordProtection,
		FirstFaction,
		SecondFaction,
		Region,
		PremadeMatchType,
		Host,
		Ping,
		Favorite,
		SortOptionsEndExclusive
	}

	private abstract class ItemComparer : IComparer<MPCustomGameItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y);
	}

	private class ServerNameComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.NameText.CompareTo(x.NameText) * -1;
			}
			return y.NameText.CompareTo(x.NameText);
		}
	}

	private class GameTypeComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.GameTypeText.CompareTo(x.GameTypeText) * -1;
			}
			return y.GameTypeText.CompareTo(x.GameTypeText);
		}
	}

	private class PlayerCountComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.PlayerCount.CompareTo(x.PlayerCount) * -1;
			}
			return y.PlayerCount.CompareTo(x.PlayerCount);
		}
	}

	private class PasswordComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.IsPasswordProtected.CompareTo(x.IsPasswordProtected) * -1;
			}
			return y.IsPasswordProtected.CompareTo(x.IsPasswordProtected);
		}
	}

	private class FirstFactionComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.FirstFactionName.CompareTo(x.FirstFactionName) * -1;
			}
			return y.FirstFactionName.CompareTo(x.FirstFactionName);
		}
	}

	private class SecondFactionComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.SecondFactionName.CompareTo(x.SecondFactionName) * -1;
			}
			return y.SecondFactionName.CompareTo(x.SecondFactionName);
		}
	}

	private class RegionComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.RegionName.CompareTo(x.RegionName) * -1;
			}
			return y.RegionName.CompareTo(x.RegionName);
		}
	}

	private class PremadeMatchTypeComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (_isAscending)
			{
				return y.PremadeMatchTypeText.CompareTo(x.PremadeMatchTypeText) * -1;
			}
			return y.PremadeMatchTypeText.CompareTo(x.PremadeMatchTypeText);
		}
	}

	private class HostComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			if (!(y.HostText == x.HostText))
			{
				return (y.HostText?.CompareTo(x.HostText) ?? (-1)) * ((!_isAscending) ? 1 : (-1));
			}
			return 0;
		}
	}

	private class PingComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			int num = ((!_isAscending) ? 1 : (-1));
			if (y.PingText == x.PingText)
			{
				return 0;
			}
			if (y.PingText == "-" || y.PingText == null)
			{
				return num;
			}
			if (x.PingText == "-" || x.PingText == null)
			{
				return num * -1;
			}
			return (int)(long.Parse(y.PingText) - long.Parse(x.PingText)) * num;
		}
	}

	private class FavoriteComparer : ItemComparer
	{
		public override int Compare(MPCustomGameItemVM x, MPCustomGameItemVM y)
		{
			return y.IsFavorite.CompareTo(x.IsFavorite) * ((!_isAscending) ? 1 : (-1));
		}
	}

	private MBBindingList<MPCustomGameItemVM> _listToControl;

	private readonly ItemComparer[] _sortComparers;

	private readonly int _numberOfSortOptions;

	private bool _isPremadeMatchesList;

	private bool _isPingInfoAvailable;

	private int _currentSortState;

	private bool _isFavoritesSelected;

	private bool _isServerNameSelected;

	private bool _isGameTypeSelected;

	private bool _isPlayerCountSelected;

	private bool _isPasswordSelected;

	private bool _isFirstFactionSelected;

	private bool _isSecondFactionSelected;

	private bool _isRegionSelected;

	private bool _isPremadeMatchTypeSelected;

	private bool _isHostSelected;

	private bool _isPingSelected;

	public CustomServerSortOption? CurrentSortOption { get; private set; }

	[DataSourceProperty]
	public bool IsPremadeMatchesList
	{
		get
		{
			return _isPremadeMatchesList;
		}
		set
		{
			if (value != _isPremadeMatchesList)
			{
				_isPremadeMatchesList = value;
				OnPropertyChanged("IsPremadeMatchesList");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPingInfoAvailable
	{
		get
		{
			return _isPingInfoAvailable;
		}
		set
		{
			if (value != _isPingInfoAvailable)
			{
				_isPingInfoAvailable = value;
				OnPropertyChanged("IsPingInfoAvailable");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentSortState
	{
		get
		{
			return _currentSortState;
		}
		set
		{
			if (value != _currentSortState)
			{
				_currentSortState = value;
				OnPropertyChanged("CurrentSortState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFavoritesSelected
	{
		get
		{
			return _isFavoritesSelected;
		}
		set
		{
			if (value != _isFavoritesSelected)
			{
				_isFavoritesSelected = value;
				OnPropertyChanged("IsFavoritesSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsServerNameSelected
	{
		get
		{
			return _isServerNameSelected;
		}
		set
		{
			if (value != _isServerNameSelected)
			{
				_isServerNameSelected = value;
				OnPropertyChanged("IsServerNameSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPasswordSelected
	{
		get
		{
			return _isPasswordSelected;
		}
		set
		{
			if (value != _isPasswordSelected)
			{
				_isPasswordSelected = value;
				OnPropertyChanged("IsPasswordSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerCountSelected
	{
		get
		{
			return _isPlayerCountSelected;
		}
		set
		{
			if (value != _isPlayerCountSelected)
			{
				_isPlayerCountSelected = value;
				OnPropertyChanged("IsPlayerCountSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFirstFactionSelected
	{
		get
		{
			return _isFirstFactionSelected;
		}
		set
		{
			if (value != _isFirstFactionSelected)
			{
				_isFirstFactionSelected = value;
				OnPropertyChanged("IsFirstFactionSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGameTypeSelected
	{
		get
		{
			return _isGameTypeSelected;
		}
		set
		{
			if (value != _isGameTypeSelected)
			{
				_isGameTypeSelected = value;
				OnPropertyChanged("IsGameTypeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSecondFactionSelected
	{
		get
		{
			return _isSecondFactionSelected;
		}
		set
		{
			if (value != _isSecondFactionSelected)
			{
				_isSecondFactionSelected = value;
				OnPropertyChanged("IsSecondFactionSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRegionSelected
	{
		get
		{
			return _isRegionSelected;
		}
		set
		{
			if (value != _isRegionSelected)
			{
				_isRegionSelected = value;
				OnPropertyChanged("IsRegionSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPremadeMatchTypeSelected
	{
		get
		{
			return _isPremadeMatchTypeSelected;
		}
		set
		{
			if (value != _isPremadeMatchTypeSelected)
			{
				_isPremadeMatchTypeSelected = value;
				OnPropertyChanged("IsPremadeMatchTypeSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHostSelected
	{
		get
		{
			return _isHostSelected;
		}
		set
		{
			if (value != _isHostSelected)
			{
				_isHostSelected = value;
				OnPropertyChanged("IsHostSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPingSelected
	{
		get
		{
			return _isPingSelected;
		}
		set
		{
			if (value != _isPingSelected)
			{
				_isPingSelected = value;
				OnPropertyChanged("IsPingSelected");
			}
		}
	}

	public MPCustomGameSortControllerVM(ref MBBindingList<MPCustomGameItemVM> listToControl, MPCustomGameVM.CustomGameMode customGameMode)
	{
		_listToControl = listToControl;
		IsPremadeMatchesList = customGameMode == MPCustomGameVM.CustomGameMode.PremadeGame;
		IsPingInfoAvailable = MPCustomGameVM.IsPingInfoAvailable && !IsPremadeMatchesList;
		_numberOfSortOptions = 11;
		_sortComparers = new ItemComparer[_numberOfSortOptions];
		for (CustomServerSortOption customServerSortOption = CustomServerSortOption.Name; customServerSortOption < CustomServerSortOption.SortOptionsEndExclusive; customServerSortOption++)
		{
			ItemComparer sortComparer = GetSortComparer(customServerSortOption);
			if (sortComparer != null)
			{
				_sortComparers[(int)customServerSortOption] = sortComparer;
			}
			else
			{
				Debug.FailedAssert("No valid comparer for custom server sort option: " + customServerSortOption, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\CustomGame\\MPCustomGameSortControllerVM.cs", ".ctor", 59);
			}
		}
		RefreshValues();
	}

	private ItemComparer GetSortComparer(CustomServerSortOption option)
	{
		switch (option)
		{
		case CustomServerSortOption.SortOptionsBeginExclusive:
		case CustomServerSortOption.SortOptionsEndExclusive:
			return null;
		case CustomServerSortOption.Name:
			return _sortComparers[(int)option] ?? new ServerNameComparer();
		case CustomServerSortOption.GameType:
			return _sortComparers[(int)option] ?? new GameTypeComparer();
		case CustomServerSortOption.PlayerCount:
			return _sortComparers[(int)option] ?? new PlayerCountComparer();
		case CustomServerSortOption.PasswordProtection:
			return _sortComparers[(int)option] ?? new PasswordComparer();
		case CustomServerSortOption.FirstFaction:
			return _sortComparers[(int)option] ?? new FirstFactionComparer();
		case CustomServerSortOption.SecondFaction:
			return _sortComparers[(int)option] ?? new SecondFactionComparer();
		case CustomServerSortOption.Region:
			return _sortComparers[(int)option] ?? new RegionComparer();
		case CustomServerSortOption.PremadeMatchType:
			return _sortComparers[(int)option] ?? new PremadeMatchTypeComparer();
		case CustomServerSortOption.Host:
			return _sortComparers[(int)option] ?? new HostComparer();
		case CustomServerSortOption.Ping:
			return _sortComparers[(int)option] ?? new PingComparer();
		case CustomServerSortOption.Favorite:
			return _sortComparers[(int)option] ?? new FavoriteComparer();
		default:
			return null;
		}
	}

	public void InitializeWithSortState(CustomServerSortOption? sortOption, SortState sortState = SortState.Default)
	{
		SetSortOption(sortOption);
		CurrentSortState = (int)sortState;
		SortByCurrentState();
	}

	private void SetSortOption(CustomServerSortOption? sortOption)
	{
		if (CurrentSortOption != sortOption)
		{
			CurrentSortOption = sortOption;
			RefreshSelectedStates();
		}
	}

	public void SortByCurrentState()
	{
		if (CurrentSortOption.HasValue)
		{
			ItemComparer sortComparer = GetSortComparer(CurrentSortOption.Value);
			SortState currentSortState = (SortState)CurrentSortState;
			if (sortComparer != null)
			{
				sortComparer.SetSortMode(currentSortState != SortState.Descending);
				_listToControl.Sort(sortComparer);
			}
		}
	}

	private void SortWithOptionAux(CustomServerSortOption option)
	{
		if (option != CurrentSortOption)
		{
			CurrentSortState = 1;
		}
		else
		{
			CurrentSortState = (CurrentSortState + 1) % 3;
		}
		SetSortOption(option);
		SortByCurrentState();
	}

	public void ExecuteSortByFavorites()
	{
		SortWithOptionAux(CustomServerSortOption.Favorite);
	}

	public void ExecuteSortByServerName()
	{
		SortWithOptionAux(CustomServerSortOption.Name);
	}

	public void ExecuteSortByGameType()
	{
		SortWithOptionAux(CustomServerSortOption.GameType);
	}

	public void ExecuteSortByPlayerCount()
	{
		SortWithOptionAux(CustomServerSortOption.PlayerCount);
	}

	public void ExecuteSortByPassword()
	{
		SortWithOptionAux(CustomServerSortOption.PasswordProtection);
	}

	public void ExecuteSortByFirstFaction()
	{
		SortWithOptionAux(CustomServerSortOption.FirstFaction);
	}

	public void ExecuteSortBySecondFaction()
	{
		SortWithOptionAux(CustomServerSortOption.SecondFaction);
	}

	public void ExecuteSortByRegion()
	{
		SortWithOptionAux(CustomServerSortOption.Region);
	}

	public void ExecuteSortByPremadeMatchType()
	{
		SortWithOptionAux(CustomServerSortOption.PremadeMatchType);
	}

	public void ExecuteSortByHost()
	{
		SortWithOptionAux(CustomServerSortOption.Host);
	}

	public void ExecuteSortByPing()
	{
		SortWithOptionAux(CustomServerSortOption.Ping);
	}

	private void RefreshSelectedStates()
	{
		IsFavoritesSelected = CurrentSortOption == CustomServerSortOption.Favorite;
		IsServerNameSelected = CurrentSortOption == CustomServerSortOption.Name;
		IsGameTypeSelected = CurrentSortOption == CustomServerSortOption.GameType;
		IsPlayerCountSelected = CurrentSortOption == CustomServerSortOption.PlayerCount;
		IsPasswordSelected = CurrentSortOption == CustomServerSortOption.PasswordProtection;
		IsFirstFactionSelected = CurrentSortOption == CustomServerSortOption.FirstFaction;
		IsSecondFactionSelected = CurrentSortOption == CustomServerSortOption.SecondFaction;
		IsRegionSelected = CurrentSortOption == CustomServerSortOption.Region;
		IsPremadeMatchTypeSelected = CurrentSortOption == CustomServerSortOption.PremadeMatchType;
		IsHostSelected = CurrentSortOption == CustomServerSortOption.Host;
		IsPingSelected = CurrentSortOption == CustomServerSortOption.Ping;
	}
}
