using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

public class MapTimeControlVM : ViewModel
{
	private bool _mainPartyPreviousTransitioning;

	private Action _onTimeFlowStateChange;

	private Func<MapBarShortcuts> _getMapBarShortcuts;

	private MapBarShortcuts _shortcuts;

	private Action _onCameraReset;

	private CampaignTime _lastSetDate;

	private bool _isSaving;

	private int _timeFlowState = -1;

	private double _time;

	private string _date;

	private string _pausedText;

	private bool _isCurrentlyPausedOnMap;

	private bool _isCenterPanelEnabled;

	private BasicTooltipViewModel _pauseHint;

	private BasicTooltipViewModel _playHint;

	private BasicTooltipViewModel _fastForwardHint;

	private BasicTooltipViewModel _timeOfDayHint;

	public bool IsInBattleSimulation { get; set; }

	public bool IsInRecruitment { get; set; }

	public bool IsEncyclopediaOpen { get; set; }

	public bool IsInArmyManagement { get; set; }

	public bool IsInTownManagement { get; set; }

	public bool IsInHideoutTroopManage { get; set; }

	public bool IsInMap { get; set; }

	public bool IsInCampaignOptions { get; set; }

	public bool IsEscapeMenuOpened { get; set; }

	public bool IsMarriageOfferPopupActive { get; set; }

	public bool IsHeirSelectionPopupActive { get; set; }

	public bool IsMapCheatsActive { get; set; }

	public bool IsMapIncidentActive { get; set; }

	public bool IsOverlayContextMenuEnabled { get; set; }

	[DataSourceProperty]
	public BasicTooltipViewModel TimeOfDayHint
	{
		get
		{
			return _timeOfDayHint;
		}
		set
		{
			if (value != _timeOfDayHint)
			{
				_timeOfDayHint = value;
				OnPropertyChangedWithValue(value, "TimeOfDayHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCurrentlyPausedOnMap
	{
		get
		{
			return _isCurrentlyPausedOnMap;
		}
		set
		{
			if (value != _isCurrentlyPausedOnMap)
			{
				_isCurrentlyPausedOnMap = value;
				OnPropertyChangedWithValue(value, "IsCurrentlyPausedOnMap");
				RefreshPausedText();
			}
		}
	}

	[DataSourceProperty]
	public bool IsCenterPanelEnabled
	{
		get
		{
			return _isCenterPanelEnabled;
		}
		set
		{
			if (value != _isCenterPanelEnabled)
			{
				_isCenterPanelEnabled = value;
				OnPropertyChangedWithValue(value, "IsCenterPanelEnabled");
			}
		}
	}

	[DataSourceProperty]
	public double Time
	{
		get
		{
			return _time;
		}
		set
		{
			if (_time != value)
			{
				_time = value;
				OnPropertyChangedWithValue(value, "Time");
			}
		}
	}

	[DataSourceProperty]
	public string PausedText
	{
		get
		{
			return _pausedText;
		}
		set
		{
			if (_pausedText != value)
			{
				_pausedText = value;
				OnPropertyChangedWithValue(value, "PausedText");
			}
		}
	}

	[DataSourceProperty]
	public string Date
	{
		get
		{
			return _date;
		}
		set
		{
			if (value != _date)
			{
				_date = value;
				OnPropertyChangedWithValue(value, "Date");
			}
		}
	}

	[DataSourceProperty]
	public int TimeFlowState
	{
		get
		{
			return _timeFlowState;
		}
		set
		{
			if (value != _timeFlowState)
			{
				_timeFlowState = value;
				OnPropertyChangedWithValue(value, "TimeFlowState");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel PauseHint
	{
		get
		{
			return _pauseHint;
		}
		set
		{
			if (value != _pauseHint)
			{
				_pauseHint = value;
				OnPropertyChangedWithValue(value, "PauseHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel PlayHint
	{
		get
		{
			return _playHint;
		}
		set
		{
			if (value != _playHint)
			{
				_playHint = value;
				OnPropertyChangedWithValue(value, "PlayHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel FastForwardHint
	{
		get
		{
			return _fastForwardHint;
		}
		set
		{
			if (value != _fastForwardHint)
			{
				_fastForwardHint = value;
				OnPropertyChangedWithValue(value, "FastForwardHint");
			}
		}
	}

	public MapTimeControlVM(Func<MapBarShortcuts> getMapBarShortcuts, Action onTimeFlowStateChange, Action onCameraResetted)
	{
		_onTimeFlowStateChange = onTimeFlowStateChange;
		_getMapBarShortcuts = getMapBarShortcuts;
		_onCameraReset = onCameraResetted;
		IsCenterPanelEnabled = false;
		_lastSetDate = CampaignTime.Zero;
		PlayHint = new BasicTooltipViewModel();
		FastForwardHint = new BasicTooltipViewModel();
		PauseHint = new BasicTooltipViewModel();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		CampaignEvents.OnSaveStartedEvent.AddNonSerializedListener(this, OnSaveStarted);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_shortcuts = _getMapBarShortcuts();
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			PlayHint.SetHintCallback(() => GameTexts.FindText("str_play").ToString());
			FastForwardHint.SetHintCallback(() => GameTexts.FindText("str_fast_forward").ToString());
			PauseHint.SetHintCallback(() => GameTexts.FindText("str_pause").ToString());
		}
		else
		{
			PlayHint.SetHintCallback(delegate
			{
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_play").ToString());
				GameTexts.SetVariable("HOTKEY", _shortcuts.PlayHotkey);
				return GameTexts.FindText("str_hotkey_with_hint").ToString();
			});
			FastForwardHint.SetHintCallback(delegate
			{
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_fast_forward").ToString());
				GameTexts.SetVariable("HOTKEY", _shortcuts.FastForwardHotkey);
				return GameTexts.FindText("str_hotkey_with_hint").ToString();
			});
			PauseHint.SetHintCallback(delegate
			{
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_pause").ToString());
				GameTexts.SetVariable("HOTKEY", _shortcuts.PauseHotkey);
				return GameTexts.FindText("str_hotkey_with_hint").ToString();
			});
		}
		RefreshPausedText();
		Date = CampaignTime.Now.ToString();
		_lastSetDate = CampaignTime.Now;
	}

	private void RefreshPausedText()
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			Debug.FailedAssert("Main party is null when refreshing pause text", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Map\\MapBar\\MapTimeControlVM.cs", "RefreshPausedText", 107);
			PausedText = GameTexts.FindText("str_paused_capital").ToString();
		}
		else if (IsCurrentlyPausedOnMap)
		{
			PausedText = GameTexts.FindText("str_paused_capital").ToString();
		}
		else if (MobileParty.MainParty.IsTransitionInProgress)
		{
			if (mainParty.IsCurrentlyAtSea)
			{
				PausedText = new TextObject("{=g1op0Thi}DISEMBARKING").ToString();
			}
			else
			{
				PausedText = new TextObject("{=Lt0PzKHN}EMBARKING").ToString();
			}
		}
		else
		{
			PausedText = string.Empty;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		_onTimeFlowStateChange = null;
		_getMapBarShortcuts = null;
		_onCameraReset = null;
		CampaignEvents.OnSaveStartedEvent.ClearListeners(this);
		CampaignEvents.OnSaveOverEvent.ClearListeners(this);
	}

	private void OnGamepadActiveStateChanged()
	{
		RefreshValues();
	}

	private void OnSaveStarted()
	{
		_isSaving = true;
	}

	private void OnSaveOver(bool wasSuccessful, string saveName)
	{
		_isSaving = false;
	}

	public void Tick()
	{
		TimeFlowState = (int)Campaign.Current.GetSimplifiedTimeControlMode();
		IsCurrentlyPausedOnMap = (TimeFlowState == 0 || TimeFlowState == 6) && IsCenterPanelEnabled && !IsEscapeMenuOpened && !_isSaving;
		IsCenterPanelEnabled = !IsInBattleSimulation && !IsInRecruitment && !IsEncyclopediaOpen && !IsInTownManagement && !IsInArmyManagement && IsInMap && !IsInCampaignOptions && !IsInHideoutTroopManage && !IsMarriageOfferPopupActive && !IsHeirSelectionPopupActive && !IsMapCheatsActive && !IsMapIncidentActive && !IsOverlayContextMenuEnabled;
		if (MobileParty.MainParty.IsTransitionInProgress != _mainPartyPreviousTransitioning)
		{
			_mainPartyPreviousTransitioning = MobileParty.MainParty.IsTransitionInProgress;
			RefreshPausedText();
		}
	}

	public void Refresh()
	{
		if (!_lastSetDate.StringSameAs(CampaignTime.Now))
		{
			Date = CampaignTime.Now.ToString();
			_lastSetDate = CampaignTime.Now;
		}
		Time = CampaignTime.Now.ToHours % (double)CampaignTime.HoursInDay;
		TimeOfDayHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTimeOfDayAndResetCameraTooltip());
	}

	private void SetTimeSpeed(int speed)
	{
		Campaign.Current.SetTimeSpeed(speed);
		_onTimeFlowStateChange();
	}

	public void ExecuteTimeControlChange(int selectedTimeSpeed)
	{
		if (Campaign.Current.CurrentMenuContext == null || (Campaign.Current.CurrentMenuContext.GameMenu.IsWaitActive && !Campaign.Current.TimeControlModeLock))
		{
			int num = selectedTimeSpeed;
			if (_timeFlowState == 3 && num == 2)
			{
				num = 4;
			}
			else if (_timeFlowState == 4 && num == 1)
			{
				num = 3;
			}
			else if (_timeFlowState == 2 && num == 0)
			{
				num = 6;
			}
			if (num != _timeFlowState)
			{
				TimeFlowState = num;
				SetTimeSpeed(selectedTimeSpeed);
			}
		}
	}

	public void ExecuteResetCamera()
	{
		_onCameraReset();
	}
}
