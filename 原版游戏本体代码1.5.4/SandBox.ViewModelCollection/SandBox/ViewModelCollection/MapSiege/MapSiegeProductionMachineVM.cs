using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege;

public class MapSiegeProductionMachineVM : ViewModel
{
	private Action<MapSiegeProductionMachineVM> _onSelection;

	private bool _isCancel;

	private int _machineType;

	private int _numberOfMachines;

	private string _machineID;

	private bool _isReserveOption;

	private string _actionText;

	public SiegeEngineType Engine { get; }

	[DataSourceProperty]
	public int MachineType
	{
		get
		{
			return _machineType;
		}
		set
		{
			if (value != _machineType)
			{
				_machineType = value;
				OnPropertyChangedWithValue(value, "MachineType");
			}
		}
	}

	[DataSourceProperty]
	public string MachineID
	{
		get
		{
			return _machineID;
		}
		set
		{
			if (value != _machineID)
			{
				_machineID = value;
				OnPropertyChangedWithValue(value, "MachineID");
			}
		}
	}

	[DataSourceProperty]
	public int NumberOfMachines
	{
		get
		{
			return _numberOfMachines;
		}
		set
		{
			if (value != _numberOfMachines)
			{
				_numberOfMachines = value;
				OnPropertyChangedWithValue(value, "NumberOfMachines");
			}
		}
	}

	[DataSourceProperty]
	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (value != _actionText)
			{
				_actionText = value;
				OnPropertyChangedWithValue(value, "ActionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsReserveOption
	{
		get
		{
			return _isReserveOption;
		}
		set
		{
			if (value != _isReserveOption)
			{
				_isReserveOption = value;
				OnPropertyChangedWithValue(value, "IsReserveOption");
			}
		}
	}

	public MapSiegeProductionMachineVM(SiegeEngineType engineType, int number, Action<MapSiegeProductionMachineVM> onSelection)
	{
		_onSelection = onSelection;
		Engine = engineType;
		NumberOfMachines = number;
		MachineID = engineType.StringId;
		IsReserveOption = false;
	}

	public MapSiegeProductionMachineVM(Action<MapSiegeProductionMachineVM> onSelection, bool isCancel)
	{
		_onSelection = onSelection;
		Engine = null;
		NumberOfMachines = 0;
		MachineID = "reserve";
		IsReserveOption = true;
		_isCancel = isCancel;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ActionText = (_isCancel ? GameTexts.FindText("str_cancel").ToString() : GameTexts.FindText("str_siege_move_to_reserve").ToString());
	}

	public void OnSelection()
	{
		_onSelection(this);
	}

	public void ExecuteShowTooltip()
	{
		if (Engine != null)
		{
			InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineTooltip(Engine));
		}
	}

	public void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}
}
