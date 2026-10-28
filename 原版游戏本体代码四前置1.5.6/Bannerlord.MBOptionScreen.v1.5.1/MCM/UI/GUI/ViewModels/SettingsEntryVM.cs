using System;
using MCM.Abstractions;
using TaleWorlds.Library;

namespace MCM.UI.GUI.ViewModels;

internal sealed class SettingsEntryVM : ViewModel
{
	private readonly Action<SettingsEntryVM> _executeSelect;

	private bool? _isSelected;

	public string Id => UnavailableSetting?.Id ?? SettingsVM?.SettingsDefinition.SettingsId ?? "ERROR";

	[DataSourceProperty]
	public string DisplayName => UnavailableSetting?.DisplayName ?? SettingsVM?.DisplayName ?? "ERROR";

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected ?? SettingsVM?.IsSelected ?? false;
		}
		set
		{
			if (SettingsVM != null)
			{
				SettingsVM.IsSelected = value;
			}
			else
			{
				_isSelected = value;
			}
			OnPropertyChanged("IsSelected");
		}
	}

	public SettingsVM? SettingsVM { get; }

	public UnavailableSetting? UnavailableSetting { get; }

	public SettingsEntryVM(UnavailableSetting unavailableSetting, Action<SettingsEntryVM> command)
	{
		UnavailableSetting = unavailableSetting;
		_isSelected = false;
		_executeSelect = command;
	}

	public SettingsEntryVM(SettingsVM settingsVM, Action<SettingsEntryVM> command)
	{
		SettingsVM = settingsVM;
		_executeSelect = command;
	}

	public void ExecuteSelect()
	{
		_executeSelect(this);
	}
}
