using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class McmDropdownRuntimeRefresh
{
	private static readonly object SyncRoot = new object();

	private static readonly List<WeakReference> LiveSettingsPropertyVms = new List<WeakReference>();

	private static readonly List<WeakReference> LiveModOptionsVms = new List<WeakReference>();

	private static int _refreshPendingTicks;

	private static bool _patched;

	private static bool _selectorVmDirectRefreshDisabled;

	private static bool _selectorVmCompatibilityWarned;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		lock (SyncRoot)
		{
			if (_patched)
			{
				return;
			}
			Type settingsPropertyVmType = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.SettingsPropertyVM");
			if (settingsPropertyVmType == null)
			{
				Logger.Log("MCM", "[WARN] 未找到 MCM SettingsPropertyVM，跳过下拉热刷新补丁。");
				return;
			}
			Type modOptionsVmType = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.ModOptionsVM");
			Harmony harmony = new Harmony("AnimusForge.mcm.dropdown.refresh");
			ConstructorInfo ctor = AccessTools.Constructor(settingsPropertyVmType, new Type[]
			{
				AccessTools.TypeByName("MCM.Abstractions.ISettingsPropertyDefinition"),
				AccessTools.TypeByName("MCM.UI.GUI.ViewModels.SettingsVM")
			});
			MethodInfo onFinalize = AccessTools.Method(settingsPropertyVmType, "OnFinalize");
			ConstructorInfo modOptionsCtor = AccessTools.Constructor(modOptionsVmType, Type.EmptyTypes);
			MethodInfo modOptionsFinalize = AccessTools.Method(modOptionsVmType, "OnFinalize");
			if (ctor != null)
			{
				harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(McmDropdownRuntimeRefresh), nameof(SettingsPropertyVmCtorPostfix)));
			}
			if (onFinalize != null)
			{
				harmony.Patch(onFinalize, prefix: new HarmonyMethod(typeof(McmDropdownRuntimeRefresh), nameof(SettingsPropertyVmFinalizePrefix)));
			}
			if (modOptionsCtor != null)
			{
				harmony.Patch(modOptionsCtor, postfix: new HarmonyMethod(typeof(McmDropdownRuntimeRefresh), nameof(ModOptionsVmCtorPostfix)));
			}
			if (modOptionsFinalize != null)
			{
				harmony.Patch(modOptionsFinalize, prefix: new HarmonyMethod(typeof(McmDropdownRuntimeRefresh), nameof(ModOptionsVmFinalizePrefix)));
			}
			_patched = true;
			Logger.Log("MCM", "[INFO] 已启用 MCM 下拉热刷新补丁。");
		}
	}

	public static void RequestRefresh()
	{
		lock (SyncRoot)
		{
			if (_refreshPendingTicks < 3)
			{
				_refreshPendingTicks = 3;
			}
		}
	}

	public static void OnApplicationTick()
	{
		bool shouldRefresh;
		lock (SyncRoot)
		{
			shouldRefresh = _refreshPendingTicks > 0;
			if (_refreshPendingTicks > 0)
			{
				_refreshPendingTicks--;
			}
		}
		if (!shouldRefresh)
		{
			return;
		}
		try
		{
			RefreshAllDropdowns();
		}
		catch (Exception ex)
		{
			Logger.Log("MCM", "[WARN] 刷新 MCM 下拉时异常: " + ex);
		}
	}

	public static void SettingsPropertyVmCtorPostfix(object __instance)
	{
		if (__instance == null)
		{
			return;
		}
		lock (SyncRoot)
		{
			LiveSettingsPropertyVms.Add(new WeakReference(__instance));
			PruneDeadEntriesNoLock();
		}
	}

	public static void SettingsPropertyVmFinalizePrefix(object __instance)
	{
		if (__instance == null)
		{
			return;
		}
		lock (SyncRoot)
		{
			for (int i = LiveSettingsPropertyVms.Count - 1; i >= 0; i--)
			{
				object target = LiveSettingsPropertyVms[i].Target;
				if (target == null || ReferenceEquals(target, __instance))
				{
					LiveSettingsPropertyVms.RemoveAt(i);
				}
			}
		}
	}

	public static void ModOptionsVmCtorPostfix(object __instance)
	{
		if (__instance == null)
		{
			return;
		}
		lock (SyncRoot)
		{
			LiveModOptionsVms.Add(new WeakReference(__instance));
			PruneDeadEntriesNoLock();
		}
	}

	public static void ModOptionsVmFinalizePrefix(object __instance)
	{
		if (__instance == null)
		{
			return;
		}
		lock (SyncRoot)
		{
			for (int i = LiveModOptionsVms.Count - 1; i >= 0; i--)
			{
				object target = LiveModOptionsVms[i].Target;
				if (target == null || ReferenceEquals(target, __instance))
				{
					LiveModOptionsVms.RemoveAt(i);
				}
			}
		}
	}

	private static void RefreshAllDropdowns()
	{
		List<object> modOptionsTargets = new List<object>();
		List<object> targets = new List<object>();
		lock (SyncRoot)
		{
			for (int i = LiveModOptionsVms.Count - 1; i >= 0; i--)
			{
				object target = LiveModOptionsVms[i].Target;
				if (target == null)
				{
					LiveModOptionsVms.RemoveAt(i);
					continue;
				}
				modOptionsTargets.Add(target);
			}
			for (int i = LiveSettingsPropertyVms.Count - 1; i >= 0; i--)
			{
				object target = LiveSettingsPropertyVms[i].Target;
				if (target == null)
				{
					LiveSettingsPropertyVms.RemoveAt(i);
					continue;
				}
				targets.Add(target);
			}
		}
		foreach (object modOptionsTarget in modOptionsTargets)
		{
			TryRebuildCurrentSettingsPage(modOptionsTarget);
		}
		if (_selectorVmDirectRefreshDisabled)
		{
			return;
		}
		foreach (object target in targets)
		{
			try
			{
				TryRefreshDropdown(target);
			}
			catch (TargetInvocationException ex) when (ex.InnerException is MissingMethodException && ex.ToString().IndexOf("MCMSelectorItemVM", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				_selectorVmDirectRefreshDisabled = true;
				if (!_selectorVmCompatibilityWarned)
				{
					_selectorVmCompatibilityWarned = true;
					Logger.Log("MCM", "[WARN] 检测到 MCM 下拉构造器兼容差异，已停用直接反射刷新并回退为页面重建刷新。");
				}
				break;
			}
		}
	}

	private static void TryRebuildCurrentSettingsPage(object modOptionsVm)
	{
		Type modOptionsVmType = modOptionsVm.GetType();
		PropertyInfo selectedEntryProperty = AccessTools.Property(modOptionsVmType, "SelectedEntry");
		object selectedEntry = selectedEntryProperty?.GetValue(modOptionsVm);
		if (selectedEntry == null)
		{
			return;
		}
		PropertyInfo settingsVmProperty = AccessTools.Property(selectedEntry.GetType(), "SettingsVM");
		object oldSettingsVm = settingsVmProperty?.GetValue(selectedEntry);
		if (oldSettingsVm == null)
		{
			return;
		}
		Type settingsVmType = oldSettingsVm.GetType();
		PropertyInfo settingsDefinitionProperty = AccessTools.Property(settingsVmType, "SettingsDefinition");
		object settingsDefinition = settingsDefinitionProperty?.GetValue(oldSettingsVm);
		if (settingsDefinition == null)
		{
			return;
		}
		ConstructorInfo settingsVmCtor = AccessTools.Constructor(settingsVmType, new Type[]
		{
			settingsDefinition.GetType(),
			modOptionsVmType
		});
		if (settingsVmCtor == null)
		{
			return;
		}
		FieldInfo settingsVmBackingField = AccessTools.Field(selectedEntry.GetType(), "<SettingsVM>k__BackingField");
		if (settingsVmBackingField == null)
		{
			return;
		}
		selectedEntryProperty?.SetValue(modOptionsVm, null);
		MethodInfo finalizeMethod = AccessTools.Method(settingsVmType, "OnFinalize");
		finalizeMethod?.Invoke(oldSettingsVm, null);
		object newSettingsVm = settingsVmCtor.Invoke(new object[] { settingsDefinition, modOptionsVm });
		settingsVmBackingField.SetValue(selectedEntry, newSettingsVm);
		MethodInfo refreshValues = AccessTools.Method(settingsVmType, "RefreshValues", Type.EmptyTypes);
		refreshValues?.Invoke(newSettingsVm, null);
		selectedEntryProperty?.SetValue(modOptionsVm, selectedEntry);
		MethodInfo onPropertyChanged = AccessTools.Method(typeof(TaleWorlds.Library.ViewModel), "OnPropertyChanged", new Type[] { typeof(string) });
		onPropertyChanged?.Invoke(modOptionsVm, new object[] { "SelectedMod" });
		onPropertyChanged?.Invoke(modOptionsVm, new object[] { "SelectedDisplayName" });
		onPropertyChanged?.Invoke(modOptionsVm, new object[] { "SomethingSelected" });
		onPropertyChanged?.Invoke(modOptionsVm, new object[] { "IsSettingVisible" });
	}

	private static void TryRefreshDropdown(object settingsPropertyVm)
	{
		Type vmType = settingsPropertyVm.GetType();
		PropertyInfo isDropdownProperty = AccessTools.Property(vmType, "IsDropdown");
		if (!(isDropdownProperty?.GetValue(settingsPropertyVm) is bool isDropdown) || !isDropdown)
		{
			return;
		}
		PropertyInfo propertyReferenceProperty = AccessTools.Property(vmType, "PropertyReference");
		object propertyReference = propertyReferenceProperty?.GetValue(settingsPropertyVm);
		if (propertyReference == null)
		{
			return;
		}
		PropertyInfo valueProperty = AccessTools.Property(propertyReference.GetType(), "Value");
		object dropdownValue = valueProperty?.GetValue(propertyReference);
		if (dropdownValue == null)
		{
			return;
		}
		IEnumerable<object> options = EnumerateOptions(dropdownValue);
		int selectedIndex = ReadSelectedIndex(dropdownValue);
		PropertyInfo selectorProperty = AccessTools.Property(vmType, "DropdownValue");
		object selectorVm = selectorProperty?.GetValue(settingsPropertyVm);
		if (selectorVm == null)
		{
			return;
		}
		MethodInfo refreshMethod = AccessTools.Method(selectorVm.GetType(), "Refresh", new Type[]
		{
			typeof(IEnumerable<object>),
			typeof(int)
		});
		if (refreshMethod == null)
		{
			return;
		}
		List<object> optionList = options.ToList();
		if (optionList.Count == 0)
		{
			return;
		}
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}
		if (selectedIndex >= optionList.Count)
		{
			selectedIndex = optionList.Count - 1;
		}
		refreshMethod.Invoke(selectorVm, new object[] { optionList, selectedIndex });
		MethodInfo selectorRefreshValues = AccessTools.Method(selectorVm.GetType(), "RefreshValues", Type.EmptyTypes);
		selectorRefreshValues?.Invoke(selectorVm, null);
		MethodInfo propertyRefreshValues = AccessTools.Method(vmType, "RefreshValues", Type.EmptyTypes);
		propertyRefreshValues?.Invoke(settingsPropertyVm, null);
		MethodInfo onPropertyChanged = AccessTools.Method(typeof(TaleWorlds.Library.ViewModel), "OnPropertyChanged", new Type[] { typeof(string) });
		onPropertyChanged?.Invoke(selectorVm, new object[] { "ItemList" });
		onPropertyChanged?.Invoke(selectorVm, new object[] { "SelectedIndex" });
		onPropertyChanged?.Invoke(selectorVm, new object[] { "SelectedItem" });
		onPropertyChanged?.Invoke(selectorVm, new object[] { "HasSingleItem" });
		onPropertyChanged?.Invoke(settingsPropertyVm, new object[] { "DropdownValue" });
		onPropertyChanged?.Invoke(settingsPropertyVm, new object[] { "IsDropdown" });
		onPropertyChanged?.Invoke(settingsPropertyVm, new object[] { "IsDropdownDefault" });
	}

	private static IEnumerable<object> EnumerateOptions(object dropdownValue)
	{
		if (dropdownValue is IEnumerable<object> enumerableObject)
		{
			return enumerableObject;
		}
		if (dropdownValue is IEnumerable enumerable)
		{
			return enumerable.Cast<object>();
		}
		return Array.Empty<object>();
	}

	private static int ReadSelectedIndex(object dropdownValue)
	{
		PropertyInfo selectedIndexProperty = AccessTools.Property(dropdownValue.GetType(), "SelectedIndex");
		if (selectedIndexProperty?.GetValue(dropdownValue) is int selectedIndex)
		{
			return selectedIndex;
		}
		return 0;
	}

	private static void PruneDeadEntriesNoLock()
	{
		for (int i = LiveSettingsPropertyVms.Count - 1; i >= 0; i--)
		{
			if (LiveSettingsPropertyVms[i].Target == null)
			{
				LiveSettingsPropertyVms.RemoveAt(i);
			}
		}
		for (int j = LiveModOptionsVms.Count - 1; j >= 0; j--)
		{
			if (LiveModOptionsVms[j].Target == null)
			{
				LiveModOptionsVms.RemoveAt(j);
			}
		}
	}
}
