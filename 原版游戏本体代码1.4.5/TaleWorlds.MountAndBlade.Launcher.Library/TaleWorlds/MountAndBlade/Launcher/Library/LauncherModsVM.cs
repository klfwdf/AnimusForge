using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherModsVM : ViewModel
{
	private UserData _userData;

	private List<ModuleInfo> _modulesCache;

	private UserDataManager _userDataManager;

	private LauncherModsDLLManager _dllManager;

	private MBBindingList<LauncherModuleVM> _modules;

	private bool _isDisabledOnMultiplayer;

	private string _nameCategoryText;

	private string _versionCategoryText;

	public string ModuleListCode
	{
		get
		{
			string text = "_MODULES_";
			IEnumerable<ModuleInfo> modulesTemp = from m in Modules.ToList()
				select m.Info;
			IList<ModuleInfo> list = MBMath.TopologySort(modulesTemp, (ModuleInfo module) => ModuleHelper.GetDependentModulesOf(modulesTemp, module));
			for (int num = 0; num < list.Count; num++)
			{
				ModuleInfo moduleInfo = list[num];
				if (moduleInfo.IsSelected)
				{
					text = text + "*" + moduleInfo.Id;
				}
			}
			return text + "*_MODULES_";
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabledOnMultiplayer;
		}
		set
		{
			if (value != _isDisabledOnMultiplayer)
			{
				_isDisabledOnMultiplayer = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public string NameCategoryText
	{
		get
		{
			return _nameCategoryText;
		}
		set
		{
			if (value != _nameCategoryText)
			{
				_nameCategoryText = value;
				OnPropertyChangedWithValue(value, "NameCategoryText");
			}
		}
	}

	[DataSourceProperty]
	public string VersionCategoryText
	{
		get
		{
			return _versionCategoryText;
		}
		set
		{
			if (value != _versionCategoryText)
			{
				_versionCategoryText = value;
				OnPropertyChangedWithValue(value, "VersionCategoryText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<LauncherModuleVM> Modules
	{
		get
		{
			return _modules;
		}
		set
		{
			if (value != _modules)
			{
				_modules = value;
				OnPropertyChangedWithValue(value, "Modules");
			}
		}
	}

	public LauncherModsVM(UserDataManager userDataManager)
	{
		_userDataManager = userDataManager;
		_userData = _userDataManager.UserData;
		_modulesCache = ModuleHelper.GetModulesForLauncher();
		_dllManager = new LauncherModsDLLManager(_userData, _modulesCache.SelectMany((ModuleInfo m) => m.SubModules).ToList());
		Modules = new MBBindingList<LauncherModuleVM>();
		IsDisabled = true;
		NameCategoryText = "Name";
		VersionCategoryText = "Version";
		if (_dllManager.ShouldUpdateSaveData)
		{
			_userDataManager.SaveUserData();
		}
	}

	public void Refresh(bool isDisabled, bool isMultiplayer)
	{
		Modules.Clear();
		IsDisabled = isDisabled;
		LoadSubModules(isMultiplayer);
	}

	private void LoadSubModules(bool isMultiplayer)
	{
		Modules.Clear();
		UserGameTypeData obj = (isMultiplayer ? _userData.MultiplayerData : _userData.SingleplayerData);
		List<ModuleInfo> unorderedModList = new List<ModuleInfo>();
		foreach (UserModData mod in obj.ModDatas)
		{
			ModuleInfo moduleInfo = _modulesCache.Find((ModuleInfo m) => m.Id == mod.Id);
			if (moduleInfo != null && !unorderedModList.Contains(moduleInfo) && IsVisible(isMultiplayer, moduleInfo))
			{
				unorderedModList.Add(moduleInfo);
			}
		}
		foreach (ModuleInfo item2 in _modulesCache)
		{
			if (!unorderedModList.Contains(item2) && IsVisible(isMultiplayer, item2))
			{
				unorderedModList.Add(item2);
			}
		}
		foreach (ModuleInfo item3 in MBMath.TopologySort(unorderedModList, (ModuleInfo module) => ModuleHelper.GetDependentModulesOf(unorderedModList, module)))
		{
			UserModData userModData = _userData.GetUserModData(isMultiplayer, item3.Id);
			bool flag = false;
			flag = ((!_userDataManager.HasUserData() || userModData == null) ? (item3.IsRequiredOfficial || item3.IsDefault) : (item3.IsRequiredOfficial || userModData.IsSelected || userModData.IsUpdatedToBeDefault(item3)));
			item3.IsSelected = item3.IsNative || (flag && AreAllDependenciesOfModulePresent(item3));
			LauncherModuleVM item = new LauncherModuleVM(item3, ChangeLoadingOrderOf, ChangeIsSelectedOf, AreAllDependenciesOfModulePresent, GetSubModuleVerifyData);
			Modules.Add(item);
		}
	}

	private bool IsVisible(bool isMultiplayer, ModuleInfo moduleInfo)
	{
		if (!moduleInfo.IsNative && (!isMultiplayer || !moduleInfo.HasMultiplayerCategory))
		{
			if (!isMultiplayer)
			{
				return moduleInfo.Category == ModuleCategory.Singleplayer;
			}
			return false;
		}
		return true;
	}

	private void ChangeLoadingOrderOf(LauncherModuleVM targetModule, int insertIndex, string tag)
	{
		if (insertIndex >= Modules.IndexOf(targetModule))
		{
			insertIndex--;
		}
		insertIndex = (int)MathF.Clamp(insertIndex, 0f, Modules.Count - 1);
		int index = Modules.IndexOf(targetModule);
		Modules.RemoveAt(index);
		Modules.Insert(insertIndex, targetModule);
		IEnumerable<ModuleInfo> modulesTemp = from m in Modules.ToList()
			select m.Info;
		Modules.Clear();
		foreach (ModuleInfo item in MBMath.TopologySort(modulesTemp, (ModuleInfo module) => ModuleHelper.GetDependentModulesOf(modulesTemp, module)))
		{
			Modules.Add(new LauncherModuleVM(item, ChangeLoadingOrderOf, ChangeIsSelectedOf, AreAllDependenciesOfModulePresent, GetSubModuleVerifyData));
		}
	}

	private void ChangeIsSelectedOf(LauncherModuleVM targetModule)
	{
		if (!AreAllDependenciesOfModulePresent(targetModule.Info))
		{
			return;
		}
		targetModule.IsSelected = !targetModule.IsSelected;
		if (targetModule.IsSelected)
		{
			foreach (LauncherModuleVM module in Modules)
			{
				module.IsSelected = module.IsSelected || targetModule.Info.DependedModules.Any((DependedModule d) => d.ModuleId == module.Info.Id && !d.IsOptional);
				if (module.Info.IncompatibleModules.Any((DependedModule i) => i.ModuleId == targetModule.Info.Id))
				{
					module.IsSelected = false;
				}
			}
			return;
		}
		foreach (LauncherModuleVM module2 in Modules)
		{
			module2.IsSelected &= !module2.Info.DependedModules.Any((DependedModule d) => d.ModuleId == targetModule.Info.Id && !d.IsOptional);
		}
	}

	private bool AreAllDependenciesOfModulePresent(ModuleInfo info)
	{
		foreach (DependedModule dependentModule in info.DependedModules)
		{
			if (!dependentModule.IsOptional && !_modulesCache.Any((ModuleInfo m) => m.Id == dependentModule.ModuleId))
			{
				return false;
			}
		}
		for (int num = 0; num < info.IncompatibleModules.Count; num++)
		{
			DependedModule module = info.IncompatibleModules[num];
			LauncherModuleVM? launcherModuleVM = _modules.FirstOrDefault((LauncherModuleVM m) => m.Info.Id == module.ModuleId);
			if (launcherModuleVM != null && launcherModuleVM.IsSelected)
			{
				return false;
			}
		}
		return true;
	}

	private LauncherDLLData GetSubModuleVerifyData(SubModuleInfo subModule)
	{
		return _dllManager.GetSubModuleVerifyData(subModule);
	}
}
