using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherConfirmStartVM : ViewModel
{
	private readonly Action _onConfirm;

	private bool _isEnabled;

	private string _description;

	private string _title;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
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
			if (_description != value)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
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
			if (_title != value)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	public LauncherConfirmStartVM(Action onConfirm)
	{
		_onConfirm = onConfirm;
		Title = "CAUTION";
	}

	public void EnableWith(List<SubModuleInfo> unverifiedSubModules, List<DependentVersionMissmatchItem> missmatchedDependentModules)
	{
		IsEnabled = true;
		Description = string.Empty;
		if (unverifiedSubModules.Count > 0)
		{
			Description += "You're loading unverified code from: \n";
			for (int i = 0; i < unverifiedSubModules.Count; i++)
			{
				Description += unverifiedSubModules[i].Name;
				if (i == unverifiedSubModules.Count - 1)
				{
					Description += "\n";
				}
				else
				{
					Description += ", ";
				}
			}
			Description += "\n";
		}
		if (missmatchedDependentModules.Count > 0)
		{
			for (int j = 0; j < missmatchedDependentModules.Count; j++)
			{
				for (int k = 0; k < missmatchedDependentModules[j].MissmatchedDependencies.Count; k++)
				{
					string missmatchedModuleId = missmatchedDependentModules[j].MissmatchedModuleId;
					string moduleId = missmatchedDependentModules[j].MissmatchedDependencies[k].Item1.ModuleId;
					string text = missmatchedDependentModules[j].MissmatchedDependencies[k].Item1.Version.ToString();
					string text2 = missmatchedDependentModules[j].MissmatchedDependencies[k].Item2.ToString();
					Description = Description + missmatchedModuleId + " depends on " + moduleId + "(" + text + "), current version is " + moduleId + "(" + text2 + ")\n";
				}
			}
			Description += "\n";
		}
		Description += "TaleWorlds is not responsible for an unstable experience if it occurs.\n";
		Description += "Are you sure?";
	}

	private void ExecuteConfirm()
	{
		_onConfirm?.Invoke();
		IsEnabled = false;
	}

	private void ExecuteCancel()
	{
		IsEnabled = false;
	}
}
