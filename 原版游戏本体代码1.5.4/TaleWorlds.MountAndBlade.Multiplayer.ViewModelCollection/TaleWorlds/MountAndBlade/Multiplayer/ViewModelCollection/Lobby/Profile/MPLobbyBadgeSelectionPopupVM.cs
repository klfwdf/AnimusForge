using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyBadgeSelectionPopupVM : ViewModel
{
	private Badge[] _playerEarnedBadges;

	private Action _onBadgeNotificationRead;

	private Action<MPLobbyAchievementBadgeGroupVM> _onBadgeProgressInfoRequested;

	private Action _onBadgeSelectionUpdated;

	private HotKey _inspectProgressKey;

	private InputKeyItemVM _cancelInputKey;

	private bool _isEnabled;

	private bool _hasNotifications;

	private string _closeText;

	private string _badgesText;

	private string _specialBadgesText;

	private string _achievementBadgesText;

	private MBBindingList<MPLobbyBadgeItemVM> _badges;

	private MBBindingList<MPLobbyAchievementBadgeGroupVM> _achievementBadgeGroups;

	private MPLobbyBadgeItemVM _inspectedBadge;

	public List<LobbyNotification> ActiveNotifications { get; private set; }

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChanged("CancelInputKey");
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
	public bool HasNotifications
	{
		get
		{
			return _hasNotifications;
		}
		set
		{
			if (value != _hasNotifications)
			{
				_hasNotifications = value;
				OnPropertyChangedWithValue(value, "HasNotifications");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public string BadgesText
	{
		get
		{
			return _badgesText;
		}
		set
		{
			if (value != _badgesText)
			{
				_badgesText = value;
				OnPropertyChangedWithValue(value, "BadgesText");
			}
		}
	}

	[DataSourceProperty]
	public string SpecialBadgesText
	{
		get
		{
			return _specialBadgesText;
		}
		set
		{
			if (value != _specialBadgesText)
			{
				_specialBadgesText = value;
				OnPropertyChangedWithValue(value, "SpecialBadgesText");
			}
		}
	}

	[DataSourceProperty]
	public string AchievementBadgesText
	{
		get
		{
			return _achievementBadgesText;
		}
		set
		{
			if (value != _achievementBadgesText)
			{
				_achievementBadgesText = value;
				OnPropertyChangedWithValue(value, "AchievementBadgesText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyBadgeItemVM> Badges
	{
		get
		{
			return _badges;
		}
		set
		{
			if (value != _badges)
			{
				_badges = value;
				OnPropertyChangedWithValue(value, "Badges");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyAchievementBadgeGroupVM> AchivementBadgeGroups
	{
		get
		{
			return _achievementBadgeGroups;
		}
		set
		{
			if (value != _achievementBadgeGroups)
			{
				_achievementBadgeGroups = value;
				OnPropertyChangedWithValue(value, "AchivementBadgeGroups");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyBadgeItemVM InspectedBadge
	{
		get
		{
			return _inspectedBadge;
		}
		set
		{
			if (value != _inspectedBadge)
			{
				_inspectedBadge = value;
				OnPropertyChangedWithValue(value, "InspectedBadge");
			}
		}
	}

	public MPLobbyBadgeSelectionPopupVM(Action onBadgeNotificationRead, Action onBadgeSelectionUpdated, Action<MPLobbyAchievementBadgeGroupVM> onBadgeProgressInfoRequested)
	{
		_onBadgeNotificationRead = onBadgeNotificationRead;
		_onBadgeSelectionUpdated = onBadgeSelectionUpdated;
		_onBadgeProgressInfoRequested = onBadgeProgressInfoRequested;
		ActiveNotifications = new List<LobbyNotification>();
		Badges = new MBBindingList<MPLobbyBadgeItemVM>();
		AchivementBadgeGroups = new MBBindingList<MPLobbyAchievementBadgeGroupVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = GameTexts.FindText("str_close").ToString();
		BadgesText = new TextObject("{=nqYaiEo2}My Badges").ToString();
		SpecialBadgesText = new TextObject("{=yI9EV0II}Special Badges").ToString();
		AchievementBadgesText = new TextObject("{=n6yb5VCI}Achievement Badges").ToString();
		AchivementBadgeGroups.ApplyActionOnAllItems(delegate(MPLobbyAchievementBadgeGroupVM g)
		{
			g.RefreshValues();
		});
	}

	public void RefreshPlayerData(PlayerData playerData)
	{
		UpdateBadges();
		UpdateBadgeSelection();
	}

	public void RefreshKeyBindings(HotKey inspectProgressKey)
	{
		_inspectProgressKey = inspectProgressKey;
		foreach (MPLobbyAchievementBadgeGroupVM achivementBadgeGroup in AchivementBadgeGroups)
		{
			achivementBadgeGroup.RefreshKeyBindings(inspectProgressKey);
		}
	}

	public async void UpdateBadges(bool shouldClear = false)
	{
		_playerEarnedBadges = await NetworkMain.GameClient.GetPlayerBadges();
		if (shouldClear)
		{
			Badges.Clear();
		}
		if (!Badges.Any((MPLobbyBadgeItemVM b) => b.Badge == null))
		{
			Badges.Add(new MPLobbyBadgeItemVM(null, UpdateBadgeSelection, (Badge b) => true, OnBadgeInspected));
		}
		if (BadgeManager.Badges == null)
		{
			return;
		}
		foreach (Badge badge in BadgeManager.Badges)
		{
			if ((!badge.IsActive || badge.IsVisibleOnlyWhenEarned) && !_playerEarnedBadges.Contains(badge))
			{
				continue;
			}
			if (badge.GroupId != null)
			{
				MPLobbyAchievementBadgeGroupVM mPLobbyAchievementBadgeGroupVM = AchivementBadgeGroups.FirstOrDefault((MPLobbyAchievementBadgeGroupVM g) => g.GroupID == badge.GroupId);
				if (mPLobbyAchievementBadgeGroupVM == null)
				{
					mPLobbyAchievementBadgeGroupVM = new MPLobbyAchievementBadgeGroupVM(badge.GroupId, _onBadgeProgressInfoRequested);
					mPLobbyAchievementBadgeGroupVM.RefreshKeyBindings(_inspectProgressKey);
					AchivementBadgeGroups.Add(mPLobbyAchievementBadgeGroupVM);
					mPLobbyAchievementBadgeGroupVM.OnGroupBadgeAdded(new MPLobbyBadgeItemVM(badge, UpdateBadgeSelection, HasPlayerEarnedBadge, OnBadgeInspected));
					continue;
				}
				MPLobbyBadgeItemVM mPLobbyBadgeItemVM = mPLobbyAchievementBadgeGroupVM.Badges.FirstOrDefault((MPLobbyBadgeItemVM b) => b.Badge == badge);
				if (mPLobbyBadgeItemVM == null)
				{
					mPLobbyAchievementBadgeGroupVM.OnGroupBadgeAdded(new MPLobbyBadgeItemVM(badge, UpdateBadgeSelection, HasPlayerEarnedBadge, OnBadgeInspected));
				}
				else
				{
					mPLobbyBadgeItemVM.UpdateWith(badge);
				}
			}
			else
			{
				MPLobbyBadgeItemVM mPLobbyBadgeItemVM2 = Badges.SingleOrDefault((MPLobbyBadgeItemVM b) => b.Badge == badge);
				if (mPLobbyBadgeItemVM2 == null)
				{
					Badges.Add(new MPLobbyBadgeItemVM(badge, UpdateBadgeSelection, HasPlayerEarnedBadge, OnBadgeInspected));
				}
				else
				{
					mPLobbyBadgeItemVM2.UpdateWith(badge);
				}
			}
		}
	}

	public void UpdateBadgeSelection()
	{
		foreach (MPLobbyBadgeItemVM badge in Badges)
		{
			badge.UpdateIsSelected();
		}
		foreach (MPLobbyAchievementBadgeGroupVM achivementBadgeGroup in AchivementBadgeGroups)
		{
			achivementBadgeGroup.UpdateBadgeSelection();
		}
		_onBadgeSelectionUpdated?.Invoke();
	}

	private bool HasPlayerEarnedBadge(Badge badge)
	{
		return _playerEarnedBadges?.Contains(badge) ?? false;
	}

	public void OnNotificationReceived(LobbyNotification notification)
	{
		string badgeID = notification.Parameters["badge_id"];
		foreach (MPLobbyBadgeItemVM item in Badges.Where((MPLobbyBadgeItemVM badge) => badge.BadgeId == badgeID))
		{
			item.HasNotification = true;
		}
		ActiveNotifications.Add(notification);
		RefreshNotificationInfo();
	}

	public void OnBadgeInspected(MPLobbyBadgeItemVM badge)
	{
		InspectedBadge = badge;
		if (badge == null)
		{
			return;
		}
		string badgeID = badge.BadgeId;
		foreach (LobbyNotification item in ActiveNotifications.Where((LobbyNotification n) => n.Parameters["badge_id"] == badgeID))
		{
			NetworkMain.GameClient.MarkNotificationAsRead(item.Id);
		}
		ActiveNotifications.RemoveAll((LobbyNotification n) => n.Parameters["badge_id"] == badgeID);
		RefreshNotificationInfo();
		_onBadgeNotificationRead?.Invoke();
	}

	private void RefreshNotificationInfo()
	{
		HasNotifications = ActiveNotifications.Count > 0;
	}

	public void Open()
	{
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
