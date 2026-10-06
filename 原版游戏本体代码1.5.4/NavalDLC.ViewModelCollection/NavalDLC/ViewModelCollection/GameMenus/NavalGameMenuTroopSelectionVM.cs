using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.GameMenus;

public class NavalGameMenuTroopSelectionVM : GameMenuTroopSelectionVM
{
	private readonly Action<TroopRoster, List<Ship>> _onDone;

	private readonly int _minSelectableShipCount;

	private readonly int _maxSelectableShipCount;

	private readonly bool _anyOtherPartiesOnPlayerSide;

	private readonly List<Ship> _initialShipSelections;

	private string _currentSelectedShipAmountTitle;

	private string _currentSelectedShipAmountText;

	private MBBindingList<NavalGameMenuShipItemVM> _ships;

	[DataSourceProperty]
	public string CurrentSelectedShipAmountTitle
	{
		get
		{
			return _currentSelectedShipAmountTitle;
		}
		set
		{
			if (value != _currentSelectedShipAmountTitle)
			{
				_currentSelectedShipAmountTitle = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedShipAmountTitle");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentSelectedShipAmountText
	{
		get
		{
			return _currentSelectedShipAmountText;
		}
		set
		{
			if (value != _currentSelectedShipAmountText)
			{
				_currentSelectedShipAmountText = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedShipAmountText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalGameMenuShipItemVM> Ships
	{
		get
		{
			return _ships;
		}
		set
		{
			if (value != _ships)
			{
				_ships = value;
				OnPropertyChangedWithValue(value, "Ships");
			}
		}
	}

	public NavalGameMenuTroopSelectionVM(TroopRoster fullRoster, TroopRoster initialTroopSelections, List<Ship> eligibleShips, List<Ship> initialShipSelections, Func<CharacterObject, bool> canChangeChangeStatusOfTroop, Action<TroopRoster, List<Ship>> onDone, int minSelectableTroopCount, int minSelectableShipCount, int maxSelectableShipCount, bool anyOtherPartiesOnPlayerSide)
		: base(fullRoster, initialTroopSelections, canChangeChangeStatusOfTroop, delegate
		{
		}, 0, minSelectableTroopCount)
	{
		_onDone = onDone;
		_minSelectableShipCount = minSelectableShipCount;
		_maxSelectableShipCount = maxSelectableShipCount;
		_anyOtherPartiesOnPlayerSide = anyOtherPartiesOnPlayerSide;
		_initialShipSelections = initialShipSelections;
		Ships = new MBBindingList<NavalGameMenuShipItemVM>();
		for (int num = 0; num < eligibleShips.Count; num++)
		{
			Ships.Add(new NavalGameMenuShipItemVM(eligibleShips[num], OnSelectedShipsChanged)
			{
				IsSelected = _initialShipSelections.Contains(eligibleShips[num])
			});
		}
		OnSelectedShipsChanged();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CurrentSelectedShipAmountTitle = new TextObject("{=4QvmDZoR}Chosen Ships").ToString();
	}

	private List<Ship> GetSelectedShips()
	{
		List<Ship> list = new List<Ship>();
		for (int i = 0; i < Ships.Count; i++)
		{
			if (Ships[i].IsSelected)
			{
				list.Add(Ships[i].Ship);
			}
		}
		return list;
	}

	private void OnSelectedShipsChanged()
	{
		List<Ship> selectedShips = GetSelectedShips();
		int num = 0;
		for (int i = 0; i < selectedShips.Count; i++)
		{
			num += selectedShips[i].ShipHull.MainDeckCrewCapacity;
		}
		UpdateMaxSelectableTroopCount(num);
		bool flag = selectedShips.Count >= _maxSelectableShipCount;
		for (int j = 0; j < Ships.Count; j++)
		{
			Ships[j].IsDisabled = flag && !Ships[j].IsSelected;
		}
		OnCurrentSelectedAmountChange();
	}

	protected override void OnCurrentSelectedAmountChange()
	{
		base.OnCurrentSelectedAmountChange();
		if (Ships != null)
		{
			int count = GetSelectedShips().Count;
			if (count < _minSelectableShipCount || count > _maxSelectableShipCount)
			{
				base.IsDoneEnabled = false;
			}
			GameTexts.SetVariable("LEFT", count);
			GameTexts.SetVariable("RIGHT", _maxSelectableShipCount);
			CurrentSelectedShipAmountText = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
			RefreshDoneHint();
		}
	}

	protected override void RefreshDoneHint()
	{
		if (Ships == null)
		{
			base.RefreshDoneHint();
			return;
		}
		int count = GetSelectedShips().Count;
		if (base.IsDoneEnabled)
		{
			base.DoneHint.HintText = TextObject.GetEmpty();
		}
		else if (count < _minSelectableShipCount)
		{
			base.DoneHint.HintText = new TextObject("{=ibM1yGMC}You must select at least {SHIP_COUNT} {?SHIP_COUNT > 1}ships{?}ship{\\?}").SetTextVariable("SHIP_COUNT", _minSelectableShipCount);
		}
		else if (count > _maxSelectableShipCount)
		{
			base.DoneHint.HintText = new TextObject("{=5xfLMBOu}You cannot select more than {SHIP_COUNT} {?SHIP_COUNT > 1}ships{?}ship{\\?}").SetTextVariable("SHIP_COUNT", _maxSelectableShipCount);
		}
		else
		{
			base.RefreshDoneHint();
		}
	}

	public override void ExecuteReset()
	{
		base.ExecuteReset();
		for (int i = 0; i < Ships.Count; i++)
		{
			Ships[i].IsSelected = _initialShipSelections.Contains(Ships[i].Ship);
		}
		OnSelectedShipsChanged();
	}

	public override void ExecuteClearSelection()
	{
		base.ExecuteClearSelection();
		for (int i = 0; i < Ships.Count; i++)
		{
			Ships[i].IsSelected = false;
		}
		OnSelectedShipsChanged();
	}

	protected override void OnDone()
	{
		TroopRoster troopRoster = BuildSelectedTroopRoster();
		base.IsEnabled = false;
		_onDone.DynamicInvokeWithLog(troopRoster, GetSelectedShips());
	}

	protected override TextObject GetWarningMessageOnDone()
	{
		if (GetAvailableSelectableTroopCount() > 0 && _anyOtherPartiesOnPlayerSide)
		{
			return new TextObject("{=lJRQx5lZ}The remaining room for soldiers will be filled by the other parties on your side. Do you want to proceed?");
		}
		return base.GetWarningMessageOnDone();
	}
}
