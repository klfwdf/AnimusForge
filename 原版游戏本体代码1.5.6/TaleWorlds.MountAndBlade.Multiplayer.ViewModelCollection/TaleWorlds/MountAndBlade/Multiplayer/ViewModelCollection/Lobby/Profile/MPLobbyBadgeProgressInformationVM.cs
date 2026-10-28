using System;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyBadgeProgressInformationVM : ViewModel
{
	private int _shownBadgeIndexOffset;

	private const int MaxShownBadgeCount = 5;

	private readonly Func<string> _getExitText;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	private int _shownBadgeCount;

	private bool _isEnabled;

	private bool _canIncreaseBadgeIndices;

	private bool _canDecreaseBadgeIndices;

	private string _clickToCloseText;

	private string _titleText;

	private MPLobbyAchievementBadgeGroupVM _badgeGroup;

	private MBBindingList<StringPairItemVM> _availableBadgeIDs;

	[DataSourceProperty]
	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChanged("PreviousTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChanged("NextTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public int ShownBadgeCount
	{
		get
		{
			return _shownBadgeCount;
		}
		set
		{
			if (value != _shownBadgeCount)
			{
				_shownBadgeCount = value;
				OnPropertyChangedWithValue(value, "ShownBadgeCount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool CanIncreaseBadgeIndices
	{
		get
		{
			return _canIncreaseBadgeIndices;
		}
		set
		{
			if (value != _canIncreaseBadgeIndices)
			{
				_canIncreaseBadgeIndices = value;
				OnPropertyChangedWithValue(value, "CanIncreaseBadgeIndices");
			}
		}
	}

	[DataSourceProperty]
	public bool CanDecreaseBadgeIndices
	{
		get
		{
			return _canDecreaseBadgeIndices;
		}
		set
		{
			if (value != _canDecreaseBadgeIndices)
			{
				_canDecreaseBadgeIndices = value;
				OnPropertyChangedWithValue(value, "CanDecreaseBadgeIndices");
			}
		}
	}

	[DataSourceProperty]
	public string ClickToCloseText
	{
		get
		{
			return _clickToCloseText;
		}
		set
		{
			if (value != _clickToCloseText)
			{
				_clickToCloseText = value;
				OnPropertyChangedWithValue(value, "ClickToCloseText");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyAchievementBadgeGroupVM BadgeGroup
	{
		get
		{
			return _badgeGroup;
		}
		set
		{
			if (value != _badgeGroup)
			{
				_badgeGroup = value;
				OnPropertyChangedWithValue(value, "BadgeGroup");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> AvailableBadgeIDs
	{
		get
		{
			return _availableBadgeIDs;
		}
		set
		{
			if (value != _availableBadgeIDs)
			{
				_availableBadgeIDs = value;
				OnPropertyChangedWithValue(value, "AvailableBadgeIDs");
			}
		}
	}

	public MPLobbyBadgeProgressInformationVM(Func<string> getExitText)
	{
		_getExitText = getExitText;
		AvailableBadgeIDs = new MBBindingList<StringPairItemVM>();
		ShownBadgeCount = 5;
		for (int i = 0; i < ShownBadgeCount; i++)
		{
			AvailableBadgeIDs.Add(new StringPairItemVM(string.Empty, string.Empty));
		}
	}

	public void OpenWith(MPLobbyAchievementBadgeGroupVM badgeGroup)
	{
		BadgeGroup = badgeGroup;
		TitleText = (BadgeGroup.ShownBadgeItem.Badge as ConditionalBadge).BadgeConditions[0].Description.ToString();
		_shownBadgeIndexOffset = 0;
		RefreshShownBadges();
		ClickToCloseText = _getExitText?.Invoke();
		IsEnabled = true;
	}

	private void RefreshShownBadges()
	{
		int num = BadgeGroup.Badges.IndexOf(BadgeGroup.ShownBadgeItem) + _shownBadgeIndexOffset;
		int num2 = 0;
		int num3 = ShownBadgeCount / 2;
		for (int i = num - num3; i <= num + num3; i++)
		{
			if (i >= 0 && i < BadgeGroup.Badges.Count)
			{
				MPLobbyBadgeItemVM mPLobbyBadgeItemVM = BadgeGroup.Badges[i];
				AvailableBadgeIDs[num2].Value = mPLobbyBadgeItemVM.BadgeId;
				AvailableBadgeIDs[num2].Definition = mPLobbyBadgeItemVM.Name;
			}
			else
			{
				AvailableBadgeIDs[num2].Value = string.Empty;
				AvailableBadgeIDs[num2].Definition = string.Empty;
			}
			num2++;
		}
		CanIncreaseBadgeIndices = BadgeGroup.Badges.IndexOf(BadgeGroup.Badges[num]) < BadgeGroup.Badges.Count - 1;
		CanDecreaseBadgeIndices = num > 0;
	}

	public void ExecuteClosePopup()
	{
		BadgeGroup = null;
		IsEnabled = false;
	}

	public void ExecuteIncreaseActiveBadgeIndices()
	{
		if (CanIncreaseBadgeIndices)
		{
			_shownBadgeIndexOffset++;
			RefreshShownBadges();
		}
	}

	public void ExecuteDecreaseActiveBadgeIndices()
	{
		if (CanDecreaseBadgeIndices)
		{
			_shownBadgeIndexOffset--;
			RefreshShownBadges();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		PreviousTabInputKey?.OnFinalize();
		NextTabInputKey?.OnFinalize();
	}

	public void SetPreviousTabInputKey(HotKey hotKey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotKey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
