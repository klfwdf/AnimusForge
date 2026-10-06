using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyBadgeItemVM : ViewModel
{
	private readonly Func<Badge, bool> _hasPlayerEarnedBadge;

	private readonly Action _onSelectedBadgeChange;

	private readonly Action<MPLobbyBadgeItemVM> _onInspected;

	private MPLobbyAchievementBadgeGroupVM _group;

	private Action<MPLobbyAchievementBadgeGroupVM> _onBadgeProgressInfoRequested;

	private const string PlaytimeConditionID = "Playtime";

	private string _name;

	private string _description;

	private string _badgeConditionsText;

	private string _badgeId;

	private bool _isEarned;

	private bool _isSelected;

	private bool _hasNotification;

	private bool _isBeingChanged;

	private bool _isFocused;

	private MBBindingList<StringPairItemVM> _conditions;

	private InputKeyItemVM _inspectProgressKey;

	public Badge Badge { get; private set; }

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string BadgeConditionsText
	{
		get
		{
			return _badgeConditionsText;
		}
		set
		{
			if (value != _badgeConditionsText)
			{
				_badgeConditionsText = value;
				OnPropertyChangedWithValue(value, "BadgeConditionsText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> Conditions
	{
		get
		{
			return _conditions;
		}
		set
		{
			if (value != _conditions)
			{
				_conditions = value;
				OnPropertyChangedWithValue(value, "Conditions");
			}
		}
	}

	[DataSourceProperty]
	public string BadgeId
	{
		get
		{
			return _badgeId;
		}
		set
		{
			if (value != _badgeId)
			{
				_badgeId = value;
				OnPropertyChangedWithValue(value, "BadgeId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEarned
	{
		get
		{
			return _isEarned;
		}
		set
		{
			if (value != _isEarned)
			{
				_isEarned = value;
				OnPropertyChangedWithValue(value, "IsEarned");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool HasNotification
	{
		get
		{
			return _hasNotification;
		}
		set
		{
			if (value != _hasNotification)
			{
				_hasNotification = value;
				OnPropertyChangedWithValue(value, "HasNotification");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBeingChanged
	{
		get
		{
			return _isBeingChanged;
		}
		set
		{
			if (value != _isBeingChanged)
			{
				_isBeingChanged = value;
				OnPropertyChangedWithValue(value, "IsBeingChanged");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM InspectProgressKey
	{
		get
		{
			return _inspectProgressKey;
		}
		set
		{
			if (value != _inspectProgressKey)
			{
				_inspectProgressKey = value;
				OnPropertyChangedWithValue(value, "InspectProgressKey");
			}
		}
	}

	public MPLobbyBadgeItemVM(Badge badge, Action onSelectedBadgeChange, Func<Badge, bool> hasPlayerEarnedBadge, Action<MPLobbyBadgeItemVM> onInspected)
	{
		_hasPlayerEarnedBadge = hasPlayerEarnedBadge;
		_onSelectedBadgeChange = onSelectedBadgeChange;
		_onInspected = onInspected;
		Badge = badge;
		Conditions = new MBBindingList<StringPairItemVM>();
		UpdateWith(Badge);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BadgeConditionsText = GameTexts.FindText("str_multiplayer_badge_conditions").ToString();
	}

	public void RefreshKeyBindings(HotKey inspectProgressKey)
	{
		InspectProgressKey = InputKeyItemVM.CreateFromHotKey(inspectProgressKey, isConsoleOnly: false);
	}

	public override void OnFinalize()
	{
		InspectProgressKey?.OnFinalize();
	}

	public void UpdateWith(Badge badge)
	{
		Badge = badge;
		BadgeId = ((Badge == null) ? "none" : Badge.StringId);
		UpdateIsSelected();
		IsEarned = _hasPlayerEarnedBadge(badge);
		RefreshProperties();
	}

	private void RefreshProperties()
	{
		Conditions.Clear();
		if (Badge != null)
		{
			Name = Badge.Name.ToString();
			Description = Badge.Description.ToString();
			if (!(Badge is ConditionalBadge conditionalBadge) || conditionalBadge.BadgeConditions.Count <= 0 || Badge.IsTimed)
			{
				return;
			}
			{
				foreach (BadgeCondition badgeCondition in conditionalBadge.BadgeConditions)
				{
					if (badgeCondition.Type == ConditionType.PlayerDataNumeric)
					{
						int num = NetworkMain.GameClient.PlayerData.GetBadgeConditionNumericValue(badgeCondition);
						if (badgeCondition.StringId.Equals("Playtime"))
						{
							num /= 3600;
						}
						Conditions.Add(new StringPairItemVM(badgeCondition.Description.ToString(), num.ToString()));
					}
				}
				return;
			}
		}
		Name = new TextObject("{=koX9okuG}None").ToString();
		Description = new TextObject("{=gcl2duJH}Reset your badge").ToString();
	}

	private async void ExecuteSetAsActive()
	{
		IsBeingChanged = true;
		if (Badge == null)
		{
			await NetworkMain.GameClient.UpdateShownBadgeId("");
		}
		else if (IsEarned)
		{
			await NetworkMain.GameClient.UpdateShownBadgeId(Badge.StringId);
		}
		else
		{
			InformationManager.ShowInquiry(new InquiryData(string.Empty, new TextObject("{=B1KQ4i9q}Badge is not earned yet. Please check conditions.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), string.Empty, null, null));
		}
		IsBeingChanged = false;
		_onSelectedBadgeChange();
	}

	private void ExecuteShowProgression()
	{
		if (Badge is ConditionalBadge)
		{
			_onBadgeProgressInfoRequested?.Invoke(_group);
		}
	}

	public void UpdateIsSelected()
	{
		if (Badge == null)
		{
			IsSelected = string.IsNullOrEmpty(NetworkMain.GameClient.PlayerData?.ShownBadgeId);
		}
		else
		{
			IsSelected = Badge.StringId == NetworkMain.GameClient.PlayerData?.ShownBadgeId;
		}
	}

	public void SetGroup(MPLobbyAchievementBadgeGroupVM group, Action<MPLobbyAchievementBadgeGroupVM> onBadgeProgressInfoRequested)
	{
		_group = group;
		_onBadgeProgressInfoRequested = onBadgeProgressInfoRequested;
	}

	private void ExecuteGainFocus()
	{
		IsFocused = true;
		if (HasNotification)
		{
			HasNotification = false;
		}
		_onInspected?.Invoke(this);
	}

	private void ExecuteLoseFocus()
	{
		IsFocused = false;
		_onInspected?.Invoke(null);
	}
}
