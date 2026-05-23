using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;

public class GameMenuTroopSelectionVM : ViewModel
{
	private readonly Action<TroopRoster> _onDone;

	private readonly TroopRoster _fullRoster;

	private readonly TroopRoster _initialSelections;

	private readonly Func<CharacterObject, bool> _canChangeChangeStatusOfTroop;

	private readonly int _maxSelectableTroopCount;

	private readonly int _minSelectableTroopCount;

	private readonly TextObject _titleTextObject = new TextObject("{=uQgNPJnc}Manage Troops");

	private readonly TextObject _chosenTitleTextObject = new TextObject("{=InqmgBiF}Chosen Crew");

	private int _currentTotalSelectedTroopCount;

	public bool IsFiveStackModifierActive;

	public bool IsEntireStackModifierActive;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _resetInputKey;

	private bool _isEnabled;

	private bool _isDoneEnabled;

	private HintViewModel _doneHint;

	private string _doneText;

	private string _cancelText;

	private string _titleText;

	private string _clearSelectionText;

	private string _currentSelectedAmountText;

	private string _currentSelectedAmountTitle;

	private MBBindingList<TroopSelectionItemVM> _troops;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

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
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
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
	public bool IsDoneEnabled
	{
		get
		{
			return _isDoneEnabled;
		}
		set
		{
			if (value != _isDoneEnabled)
			{
				_isDoneEnabled = value;
				OnPropertyChangedWithValue(value, "IsDoneEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DoneHint
	{
		get
		{
			return _doneHint;
		}
		set
		{
			if (value != _doneHint)
			{
				_doneHint = value;
				OnPropertyChangedWithValue(value, "DoneHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TroopSelectionItemVM> Troops
	{
		get
		{
			return _troops;
		}
		set
		{
			if (value != _troops)
			{
				_troops = value;
				OnPropertyChangedWithValue(value, "Troops");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
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
	public string ClearSelectionText
	{
		get
		{
			return _clearSelectionText;
		}
		set
		{
			if (value != _clearSelectionText)
			{
				_clearSelectionText = value;
				OnPropertyChangedWithValue(value, "ClearSelectionText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentSelectedAmountText
	{
		get
		{
			return _currentSelectedAmountText;
		}
		set
		{
			if (value != _currentSelectedAmountText)
			{
				_currentSelectedAmountText = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedAmountText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentSelectedAmountTitle
	{
		get
		{
			return _currentSelectedAmountTitle;
		}
		set
		{
			if (value != _currentSelectedAmountTitle)
			{
				_currentSelectedAmountTitle = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedAmountTitle");
			}
		}
	}

	public GameMenuTroopSelectionVM(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
	{
		_canChangeChangeStatusOfTroop = canChangeChangeStatusOfTroop;
		_onDone = onDone;
		_fullRoster = fullRoster;
		_initialSelections = initialSelections;
		_maxSelectableTroopCount = maxSelectableTroopCount;
		_minSelectableTroopCount = minSelectableTroopCount;
		DoneHint = new HintViewModel();
		InitList();
		RefreshValues();
		OnCurrentSelectedAmountChange();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = _titleTextObject.ToString();
		CurrentSelectedAmountTitle = _chosenTitleTextObject.ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		ClearSelectionText = new TextObject("{=QMNWbmao}Clear Selection").ToString();
		RefreshDoneHint();
	}

	private void RefreshDoneHint()
	{
		if (IsDoneEnabled)
		{
			DoneHint.HintText = TextObject.GetEmpty();
		}
		else if (_currentTotalSelectedTroopCount < _minSelectableTroopCount)
		{
			DoneHint.HintText = new TextObject("{=LlV29O9B}You must select at least {TROOP_COUNT} troops").SetTextVariable("TROOP_COUNT", _minSelectableTroopCount);
		}
		else
		{
			DoneHint.HintText = new TextObject("{=TdWQM7QZ}You must select less than {TROOP_COUNT} troops").SetTextVariable("TROOP_COUNT", _maxSelectableTroopCount);
		}
	}

	protected virtual void InitList()
	{
		Troops = new MBBindingList<TroopSelectionItemVM>();
		_currentTotalSelectedTroopCount = 0;
		foreach (TroopRosterElement item in _fullRoster.GetTroopRoster())
		{
			TroopSelectionItemVM troopSelectionItemVM = new TroopSelectionItemVM(item, OnAddTroop, OnRemoveTroop);
			troopSelectionItemVM.IsLocked = !_canChangeChangeStatusOfTroop(item.Character) || item.Number - item.WoundedNumber <= 0;
			Troops.Add(troopSelectionItemVM);
			int troopCount = _initialSelections.GetTroopCount(item.Character);
			if (troopCount > 0)
			{
				troopSelectionItemVM.CurrentAmount = troopCount;
				_currentTotalSelectedTroopCount += troopCount;
			}
		}
		Troops.Sort(new TroopItemComparer());
	}

	private void OnRemoveTroop(TroopSelectionItemVM troopItem)
	{
		if (troopItem.CurrentAmount > 0)
		{
			int num = 1;
			if (IsEntireStackModifierActive)
			{
				num = troopItem.CurrentAmount;
			}
			else if (IsFiveStackModifierActive)
			{
				num = TaleWorlds.Library.MathF.Min(troopItem.CurrentAmount, 5);
			}
			troopItem.CurrentAmount -= num;
			_currentTotalSelectedTroopCount -= num;
		}
		OnCurrentSelectedAmountChange();
	}

	private void OnAddTroop(TroopSelectionItemVM troopItem)
	{
		if (troopItem.CurrentAmount < troopItem.MaxAmount && _currentTotalSelectedTroopCount < _maxSelectableTroopCount)
		{
			int num = 1;
			if (IsEntireStackModifierActive)
			{
				num = TaleWorlds.Library.MathF.Min(troopItem.MaxAmount - troopItem.CurrentAmount, _maxSelectableTroopCount - _currentTotalSelectedTroopCount);
			}
			else if (IsFiveStackModifierActive)
			{
				num = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Min(troopItem.MaxAmount - troopItem.CurrentAmount, _maxSelectableTroopCount - _currentTotalSelectedTroopCount), 5);
			}
			troopItem.CurrentAmount += num;
			_currentTotalSelectedTroopCount += num;
		}
		OnCurrentSelectedAmountChange();
	}

	private void OnCurrentSelectedAmountChange()
	{
		foreach (TroopSelectionItemVM troop in Troops)
		{
			troop.IsRosterFull = _currentTotalSelectedTroopCount >= _maxSelectableTroopCount;
		}
		GameTexts.SetVariable("LEFT", _currentTotalSelectedTroopCount);
		GameTexts.SetVariable("RIGHT", _maxSelectableTroopCount);
		CurrentSelectedAmountText = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
		IsDoneEnabled = _currentTotalSelectedTroopCount <= _maxSelectableTroopCount && _currentTotalSelectedTroopCount >= _minSelectableTroopCount;
		RefreshDoneHint();
	}

	private void OnDone()
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopSelectionItemVM troop in Troops)
		{
			if (troop.CurrentAmount > 0)
			{
				troopRoster.AddToCounts(troop.Troop.Character, troop.CurrentAmount);
			}
		}
		IsEnabled = false;
		_onDone.DynamicInvokeWithLog(troopRoster);
	}

	public void ExecuteDone()
	{
		if (GetAvailableSelectableTroopCount() > 0)
		{
			string text = new TextObject("{=z2Slmx4N}There are still some room for more soldiers. Do you want to proceed?").ToString();
			InformationManager.ShowInquiry(new InquiryData(TitleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), OnDone, null));
		}
		else
		{
			OnDone();
		}
	}

	private int GetAvailableSelectableTroopCount()
	{
		int num = 0;
		foreach (TroopSelectionItemVM troop in Troops)
		{
			if (!troop.IsLocked && troop.CurrentAmount < troop.MaxAmount)
			{
				num += troop.MaxAmount - troop.CurrentAmount;
			}
		}
		if (_currentTotalSelectedTroopCount + num > _maxSelectableTroopCount)
		{
			num = _maxSelectableTroopCount - _currentTotalSelectedTroopCount;
		}
		return num;
	}

	public void ExecuteCancel()
	{
		IsEnabled = false;
	}

	public void ExecuteReset()
	{
		InitList();
		OnCurrentSelectedAmountChange();
	}

	public void ExecuteClearSelection()
	{
		Troops.ApplyActionOnAllItems(delegate(TroopSelectionItemVM troopItem)
		{
			if (_canChangeChangeStatusOfTroop(troopItem.Troop.Character))
			{
				int currentAmount = troopItem.CurrentAmount;
				for (int i = 0; i < currentAmount; i++)
				{
					troopItem.ExecuteRemove();
				}
			}
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		ResetInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
