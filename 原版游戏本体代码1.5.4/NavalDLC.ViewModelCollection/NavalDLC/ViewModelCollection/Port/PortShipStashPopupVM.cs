using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class PortShipStashPopupVM : ViewModel
{
	private Action<List<Ship>> _onClosed;

	private Func<Ship, ShipItemVM> _retrieveShipItemVM;

	private readonly MBBindingList<PortShipStashItemVM> _emptyShipsList = new MBBindingList<PortShipStashItemVM>();

	private List<Ship> _pendingTakes;

	private bool _isOpen;

	private string _title;

	private string _takeToPartyLbl;

	private string _noShipInTheSettlementStash;

	private MBBindingList<PortShipStashSettlementGroupVM> _groups;

	private bool _canTakeToParty;

	private MBBindingList<PortShipStashItemVM> _selectedGroupShips;

	private bool _hasNoShipsInSelectedGroup;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	[DataSourceProperty]
	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			if (value != _isOpen)
			{
				_isOpen = value;
				OnPropertyChangedWithValue(value, "IsOpen");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string TakeToPartyLbl
	{
		get
		{
			return _takeToPartyLbl;
		}
		set
		{
			if (value != _takeToPartyLbl)
			{
				_takeToPartyLbl = value;
				OnPropertyChangedWithValue(value, "TakeToPartyLbl");
			}
		}
	}

	[DataSourceProperty]
	public string NoShipInTheSettlementStash
	{
		get
		{
			return _noShipInTheSettlementStash;
		}
		set
		{
			if (value != _noShipInTheSettlementStash)
			{
				_noShipInTheSettlementStash = value;
				OnPropertyChangedWithValue(value, "NoShipInTheSettlementStash");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PortShipStashSettlementGroupVM> Groups
	{
		get
		{
			return _groups;
		}
		set
		{
			if (value != _groups)
			{
				_groups = value;
				OnPropertyChangedWithValue(value, "Groups");
			}
		}
	}

	[DataSourceProperty]
	public bool CanTakeToParty
	{
		get
		{
			return _canTakeToParty;
		}
		set
		{
			if (value != _canTakeToParty)
			{
				_canTakeToParty = value;
				OnPropertyChangedWithValue(value, "CanTakeToParty");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PortShipStashItemVM> SelectedGroupShips
	{
		get
		{
			return _selectedGroupShips;
		}
		set
		{
			if (value != _selectedGroupShips)
			{
				_selectedGroupShips = value;
				OnPropertyChangedWithValue(value, "SelectedGroupShips");
			}
		}
	}

	[DataSourceProperty]
	public bool HasNoShipsInSelectedGroup
	{
		get
		{
			return _hasNoShipsInSelectedGroup;
		}
		set
		{
			if (value != _hasNoShipsInSelectedGroup)
			{
				_hasNoShipsInSelectedGroup = value;
				OnPropertyChangedWithValue(value, "HasNoShipsInSelectedGroup");
			}
		}
	}

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

	public PortShipStashPopupVM()
	{
		Groups = new MBBindingList<PortShipStashSettlementGroupVM>();
		SelectedGroupShips = _emptyShipsList;
		_pendingTakes = new List<Ship>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		foreach (PortShipStashSettlementGroupVM group in Groups)
		{
			foreach (PortShipStashItemVM ship in group.Ships)
			{
				ship.RefreshValues();
			}
		}
	}

	public void Open(Settlement currentSettlement, PortScreenHandler portScreenHandler, Func<Ship, ShipItemVM> retrieveShipItemVM, Action<List<Ship>> onClosed)
	{
		_onClosed = onClosed;
		Title = new TextObject("{=5NeHXwft}Moored Ships").ToString();
		TakeToPartyLbl = new TextObject("{=JJ6ssNiC}Take to Party").ToString();
		NoShipInTheSettlementStash = new TextObject("{=iMvaVrgG}No ships are moored at the current settlement").ToString();
		_retrieveShipItemVM = retrieveShipItemVM;
		CanTakeToParty = false;
		SelectedGroupShips = _emptyShipsList;
		HasNoShipsInSelectedGroup = true;
		_pendingTakes.Clear();
		Groups.Clear();
		BuildGroups(currentSettlement, portScreenHandler);
		RefreshValues();
		IsOpen = true;
	}

	public void ExecuteTakeToParty()
	{
		foreach (PortShipStashSettlementGroupVM group in Groups)
		{
			for (int num = group.Ships.Count - 1; num >= 0; num--)
			{
				PortShipStashItemVM portShipStashItemVM = group.Ships[num];
				if (portShipStashItemVM.IsSelected)
				{
					_pendingTakes.Add(portShipStashItemVM.CurrentShip);
				}
			}
		}
		Close();
	}

	public void ExecuteCancel()
	{
		_pendingTakes.Clear();
		Close();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Groups.Clear();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	private void SelectGroup(PortShipStashSettlementGroupVM groupToSelect)
	{
		foreach (PortShipStashSettlementGroupVM group in Groups)
		{
			group.IsSelected = false;
		}
		if (groupToSelect != null)
		{
			groupToSelect.IsSelected = true;
			SelectedGroupShips = groupToSelect.Ships;
		}
		else
		{
			SelectedGroupShips = _emptyShipsList;
		}
		HasNoShipsInSelectedGroup = SelectedGroupShips.Count == 0;
	}

	private void BuildGroups(Settlement currentSettlement, PortScreenHandler portScreenHandler)
	{
		PortShipStashSettlementGroupVM groupToSelect = null;
		if (currentSettlement != null)
		{
			PortShipStashSettlementGroupVM portShipStashSettlementGroupVM = new PortShipStashSettlementGroupVM(currentSettlement, SelectGroup);
			foreach (Ship item in currentSettlement.ShipStash.Where((Ship x) => !portScreenHandler.ShipsToRetrieveFromStash.Contains(x)).ToList())
			{
				portShipStashSettlementGroupVM.Ships.Add(new PortShipStashItemVM(_retrieveShipItemVM(item), isEnabled: true, OnItemToggleSelect));
			}
			if (portScreenHandler.ShipsToStash != null && portScreenHandler.ShipsToStash.Count > 0)
			{
				foreach (Ship item2 in portScreenHandler.ShipsToStash)
				{
					portShipStashSettlementGroupVM.Ships.Add(new PortShipStashItemVM(_retrieveShipItemVM(item2), isEnabled: true, OnItemToggleSelect));
				}
			}
			Groups.Add(portShipStashSettlementGroupVM);
			groupToSelect = portShipStashSettlementGroupVM;
		}
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (settlement == currentSettlement || settlement.ShipStash == null || settlement.ShipStash.Count == 0)
			{
				continue;
			}
			PortShipStashSettlementGroupVM portShipStashSettlementGroupVM2 = new PortShipStashSettlementGroupVM(settlement, SelectGroup);
			foreach (Ship item3 in settlement.ShipStash)
			{
				portShipStashSettlementGroupVM2.Ships.Add(new PortShipStashItemVM(item3, isEnabled: false, OnItemToggleSelect));
			}
			Groups.Add(portShipStashSettlementGroupVM2);
		}
		SelectGroup(groupToSelect);
	}

	private void OnItemToggleSelect(PortShipStashItemVM item)
	{
		RefreshCanTakeToParty();
	}

	private void RefreshCanTakeToParty()
	{
		bool canTakeToParty = false;
		foreach (PortShipStashItemVM selectedGroupShip in SelectedGroupShips)
		{
			if (selectedGroupShip.IsSelected)
			{
				canTakeToParty = true;
				break;
			}
		}
		CanTakeToParty = canTakeToParty;
	}

	private void Close()
	{
		IsOpen = false;
		Groups.Clear();
		SelectedGroupShips = _emptyShipsList;
		_onClosed?.Invoke(_pendingTakes);
		_onClosed = null;
	}
}
