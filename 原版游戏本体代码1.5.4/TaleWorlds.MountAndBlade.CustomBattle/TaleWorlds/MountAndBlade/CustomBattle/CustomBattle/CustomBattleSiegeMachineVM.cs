using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public class CustomBattleSiegeMachineVM : ViewModel
{
	private Action<CustomBattleSiegeMachineVM> _onSelection;

	private Action<CustomBattleSiegeMachineVM> _onResetSelection;

	private string _name;

	private bool _isRanged;

	private string _machineID;

	public SiegeEngineType SiegeEngineType { get; private set; }

	[DataSourceProperty]
	public bool IsRanged
	{
		get
		{
			return _isRanged;
		}
		set
		{
			if (value != _isRanged)
			{
				_isRanged = value;
				OnPropertyChangedWithValue(value, "IsRanged");
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

	public CustomBattleSiegeMachineVM(SiegeEngineType machineType, Action<CustomBattleSiegeMachineVM> onSelection, Action<CustomBattleSiegeMachineVM> onResetSelection)
	{
		_onSelection = onSelection;
		_onResetSelection = onResetSelection;
		SetMachineType(machineType);
	}

	public void SetMachineType(SiegeEngineType machine)
	{
		SiegeEngineType = machine;
		Name = ((machine != null) ? machine.StringId : "");
		IsRanged = machine?.IsRanged ?? false;
		MachineID = ((machine != null) ? machine.StringId : "");
	}

	private void OnSelection()
	{
		_onSelection(this);
	}

	private void OnResetSelection()
	{
		_onResetSelection(this);
	}
}
