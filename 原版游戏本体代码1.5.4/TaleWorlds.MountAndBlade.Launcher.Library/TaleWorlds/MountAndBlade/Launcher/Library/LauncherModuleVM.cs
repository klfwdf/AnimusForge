using System;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherModuleVM : ViewModel
{
	public readonly ModuleInfo Info;

	private readonly Action<LauncherModuleVM, int, string> _onChangeLoadingOrder;

	private readonly Action<LauncherModuleVM> _onSelect;

	private readonly Func<SubModuleInfo, LauncherDLLData> _querySubmoduleVerifyData;

	private readonly Func<ModuleInfo, bool> _areAllDependenciesPresent;

	private MBBindingList<LauncherSubModule> _subModules;

	private LauncherHintVM _dangerousHint;

	private LauncherHintVM _dependencyHint;

	private string _name;

	private string _versionText;

	private bool _isDisabled;

	private bool _isDangerous;

	private bool _isOfficial;

	private bool _anyDependencyAvailable;

	[DataSourceProperty]
	public MBBindingList<LauncherSubModule> SubModules
	{
		get
		{
			return _subModules;
		}
		set
		{
			if (value != _subModules)
			{
				_subModules = value;
				OnPropertyChangedWithValue(value, "SubModules");
			}
		}
	}

	[DataSourceProperty]
	public LauncherHintVM DangerousHint
	{
		get
		{
			return _dangerousHint;
		}
		set
		{
			if (value != _dangerousHint)
			{
				_dangerousHint = value;
				OnPropertyChangedWithValue(value, "DangerousHint");
			}
		}
	}

	[DataSourceProperty]
	public LauncherHintVM DependencyHint
	{
		get
		{
			return _dependencyHint;
		}
		set
		{
			if (value != _dependencyHint)
			{
				_dependencyHint = value;
				OnPropertyChangedWithValue(value, "DependencyHint");
			}
		}
	}

	[DataSourceProperty]
	public string VersionText
	{
		get
		{
			return _versionText;
		}
		set
		{
			if (value != _versionText)
			{
				_versionText = value;
				OnPropertyChangedWithValue(value, "VersionText");
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

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyDependencyAvailable
	{
		get
		{
			return _anyDependencyAvailable;
		}
		set
		{
			if (value != _anyDependencyAvailable)
			{
				_anyDependencyAvailable = value;
				OnPropertyChangedWithValue(value, "AnyDependencyAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDangerous
	{
		get
		{
			return _isDangerous;
		}
		set
		{
			if (value != _isDangerous)
			{
				_isDangerous = value;
				OnPropertyChangedWithValue(value, "IsDangerous");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOfficial
	{
		get
		{
			return _isOfficial;
		}
		set
		{
			if (value != _isOfficial)
			{
				_isOfficial = value;
				OnPropertyChangedWithValue(value, "IsOfficial");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return Info.IsSelected;
		}
		set
		{
			if ((value || !Info.IsNative) && value != Info.IsSelected)
			{
				UpdateIsDisabled();
				Info.IsSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	public LauncherModuleVM(ModuleInfo moduleInfo, Action<LauncherModuleVM, int, string> onChangeLoadingOrder, Action<LauncherModuleVM> onSelect, Func<ModuleInfo, bool> areAllDependenciesPresent, Func<SubModuleInfo, LauncherDLLData> queryIsSubmoduleDangerous)
	{
		Info = moduleInfo;
		_onSelect = onSelect;
		_onChangeLoadingOrder = onChangeLoadingOrder;
		_querySubmoduleVerifyData = queryIsSubmoduleDangerous;
		_areAllDependenciesPresent = areAllDependenciesPresent;
		SubModules = new MBBindingList<LauncherSubModule>();
		IsOfficial = Info.IsOfficial;
		VersionText = Info.Version.ToString();
		Name = ProcessModuleName(moduleInfo.Name);
		string text = string.Empty;
		if (moduleInfo.DependedModules.Count > 0)
		{
			text += "Depends on: \n";
			foreach (DependedModule dependedModule in moduleInfo.DependedModules)
			{
				text = text + dependedModule.ModuleId + (dependedModule.IsOptional ? " (optional)" : "") + "\n";
			}
			AnyDependencyAvailable = true;
		}
		if (moduleInfo.IncompatibleModules.Count > 0)
		{
			if (AnyDependencyAvailable)
			{
				text += "\n----\n";
			}
			text += "Incompatible with: \n";
			foreach (DependedModule incompatibleModule in moduleInfo.IncompatibleModules)
			{
				text = text + incompatibleModule.ModuleId + "\n";
			}
			AnyDependencyAvailable = true;
		}
		if (moduleInfo.ModulesToLoadAfterThis.Count > 0)
		{
			if (AnyDependencyAvailable)
			{
				text += "\n----\n";
			}
			text += "Needs to load before: \n";
			foreach (DependedModule modulesToLoadAfterThi in moduleInfo.ModulesToLoadAfterThis)
			{
				text = text + modulesToLoadAfterThi.ModuleId + "\n";
			}
			AnyDependencyAvailable = true;
		}
		DependencyHint = new LauncherHintVM(text);
		UpdateIsDisabled();
		bool num = !moduleInfo.SubModules.Any(delegate(SubModuleInfo s)
		{
			LauncherDLLData launcherDLLData2 = _querySubmoduleVerifyData(s);
			return launcherDLLData2 != null && launcherDLLData2.Size == 0;
		});
		string text2 = "";
		if (num)
		{
			text2 = "Dangerous code detected.\n\nTaleWorlds is not responsible for consequences arising from running unverified/unofficial code.";
			foreach (SubModuleInfo subModule in moduleInfo.SubModules)
			{
				SubModules.Add(new LauncherSubModule(subModule));
				LauncherDLLData launcherDLLData = _querySubmoduleVerifyData(subModule);
				if (launcherDLLData != null)
				{
					IsDangerous = IsDangerous || launcherDLLData.IsDangerous;
				}
			}
		}
		else
		{
			IsDangerous = true;
			text2 = "Couldn't verify some or all of the code included in this module.\n\nTaleWorlds is not responsible for consequences arising from running unverified/unofficial code.";
		}
		DangerousHint = new LauncherHintVM(text2);
	}

	private static string ProcessModuleName(string originalModuleName)
	{
		if (originalModuleName == "NavalDLC")
		{
			return "War Sails";
		}
		return originalModuleName;
	}

	private void UpdateIsDisabled()
	{
		IsDisabled = !Debugger.IsAttached && ((Info.IsRequiredOfficial && Info.IsSelected) || !_areAllDependenciesPresent(Info));
	}

	private void ExecuteSelect()
	{
		_onSelect(this);
	}
}
