using System;

using System.Collections;
using System.Collections.Concurrent;

using System.Collections.Generic;

using System.Linq;

using System.Reflection;

using System.Threading;

using HarmonyLib;

using MCM.Common;

using TaleWorlds.Localization;



namespace AnimusForge;



public static class McmDropdownRuntimeRefresh

{

	private static readonly object SyncRoot = new object();



	private static readonly List<WeakReference> LiveSettingsPropertyVms = new List<WeakReference>();



	private static readonly List<WeakReference> LiveModOptionsVms = new List<WeakReference>();

	private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
	private static int MainThreadActionCount;



	private static int _refreshPendingTicks;



	private static bool _patched;



	private static bool _selectorVmDirectRefreshDisabled;



	private static bool _selectorVmCompatibilityWarned;

	private static bool _selectorVmFallbackLogged;



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

				Logger.Log("MCM", "[WARN] MCM SettingsPropertyVM not found; skipping dropdown live refresh patch.");

				return;

			}

			Type modOptionsVmType = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.ModOptionsVM");

			Harmony harmony = new Harmony("AnimusForge.mcm.dropdown.refresh");

			Type indexActionType = AccessTools.TypeByName("MCM.UI.Actions.SetSelectedIndexAction");

			ConstructorInfo indexActionCtor = indexActionType == null ? null : AccessTools.Constructor(indexActionType, new[] { typeof(IRef), typeof(object) });

			if (indexActionCtor != null)

				harmony.Patch(indexActionCtor, prefix: new HarmonyMethod(typeof(McmDropdownRuntimeRefresh), nameof(ModelPresetIndexPrefix)));

			else

				Logger.Log("MCM", "[WARN] MCM preset dropdown copy entry not found; dynamic model preset ordering restoration across lists is unavailable.");

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

			Logger.Log("MCM", "[INFO] Enabled MCM dropdown live refresh patch.");

		}

	}



	public static void RequestRefresh()

	{
		if (Volatile.Read(ref _selectorVmDirectRefreshDisabled))
		{
			return;
		}

		while (true)

		{

			int pendingTicks = Volatile.Read(ref _refreshPendingTicks);

			if (pendingTicks >= 3)

			{

				return;

			}

			if (Interlocked.CompareExchange(ref _refreshPendingTicks, 3, pendingTicks) == pendingTicks)

			{

				return;

			}

		}

	}



	// UI choices supply a selector VM; preset copies supply a Dropdown. Only AF's

	// model presets need name mapping. Append missing names to preserve undo indices.

	internal static void ModelPresetIndexPrefix(IRef __0, ref object __1)

	{

		if (!(__0 is PropertyRef property) || !(property.Instance is DuelSettings)

			|| !(__1 is Dropdown<string> source)) return;

		switch (property.PropertyInfo.Name)

		{

			case nameof(DuelSettings.MainModelDropdown):

			case nameof(DuelSettings.AuxiliaryModelDropdown):

			case nameof(DuelSettings.ActionPostprocessModelDropdown):

			case nameof(DuelSettings.EventAndRebellionModelDropdown):

			case nameof(DuelSettings.TownAmbientAiModelDropdown):

				break;

			default: return;

		}

		if (source.SelectedIndex < 0 || source.SelectedIndex >= source.Count

			|| !(__0.Value is Dropdown<string> target)) return;

		string name = source[source.SelectedIndex];

		if (name == McmPromptDefaults.Raw("Mcm_ManualDropdownModelName")) name = "{=AF_MCM_Option_ModelManual}*manual entry*";

		int index = target.FindIndex(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));

		if (index < 0) { target.Add(name); index = target.Count - 1; }

		__1 = new Dropdown<string>(target, index);

	}



    internal static bool PostToMainThread(Action action)
    {
        if (action == null || Interlocked.Increment(ref MainThreadActionCount) > 32)
        {
            if (action != null) Interlocked.Decrement(ref MainThreadActionCount);
            Logger.Log("MCM", "[WARN] Dropped model-list UI callback because the main-thread queue is full.");
            return false;
        }
        MainThreadActions.Enqueue(action);
        return true;
    }

    private static void DrainMainThreadActions()
    {
        for (int i = 0; i < 8 && MainThreadActions.TryDequeue(out Action action); i++)
        {
            Interlocked.Decrement(ref MainThreadActionCount);
            try { action(); }
            catch (Exception ex) { Logger.Log("MCM", "[WARN] Model-list UI callback failed: " + ex); }
        }
    }

	public static void OnApplicationTick()

	{
        DrainMainThreadActions();

		while (true)

		{

			int pendingTicks = Volatile.Read(ref _refreshPendingTicks);

			if (pendingTicks <= 0)

			{

				return;

			}

			if (Interlocked.CompareExchange(ref _refreshPendingTicks, pendingTicks - 1, pendingTicks) == pendingTicks)

			{

				break;

			}

		}

		try

		{

			RefreshAllDropdowns();

		}

		catch (Exception ex)

		{

			Logger.Log("MCM", "[WARN] Exception refreshing MCM dropdown: " + ex);

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

        // Coalesce panel construction into one bounded refresh. The selector's
        // display strings follow the current locale; persisted values/indices stay intact.
        RequestRefresh();

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

					Logger.Log("MCM", "[WARN] Detected an MCM dropdown constructor compatibility difference; live refresh is unavailable; reopen the settings view to load the fetched model list.");

				}

				break;

			}

		}

		// Some MCM 5.11 builds cannot construct the selector item type used by
		// direct Refresh. Rebuilding the entire settings page on the game thread
		// can stall the page after an async model request completes. Keep the new
		// values in DuelSettings/cache and let the next MCM open construct them.
		if (_selectorVmDirectRefreshDisabled)
		{
			if (!_selectorVmFallbackLogged)
			{
				_selectorVmFallbackLogged = true;
				Logger.Log("MCM", "[INFO] Dropdown live refresh is unavailable on this MCM build; current page was left intact. Reopen MCM to load the fetched model list.");
			}
			return;
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

        // MCM constructs property VMs for every installed mod. Refresh only AF
        // owners; constructor notifications must not mutate another mod's UI.
        if (!(propertyReference is PropertyRef afReference) || !IsAnimusForgeOwner(afReference.Instance?.GetType()))
            return;

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

        // Build a detached selector first. MCM's Refresh clears ItemList before constructing
        // items, and its live selection callbacks record UI refreshes as configuration edits.
        ConstructorInfo selectorConstructor = selectorVm.GetType().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(ctor => ctor.GetParameters().Length == 2 && ctor.GetParameters()[1].ParameterType == typeof(int))
            .OrderBy(ctor => ctor.GetParameters()[0].ParameterType == typeof(IEnumerable<TextObject>) ? 0
                : ctor.GetParameters()[0].ParameterType == typeof(IEnumerable<string>) ? 1 : 2)
            .FirstOrDefault(ctor => ctor.GetParameters()[0].ParameterType == typeof(IEnumerable<TextObject>)
                || ctor.GetParameters()[0].ParameterType == typeof(IEnumerable<string>)
                || ctor.GetParameters()[0].ParameterType == typeof(IEnumerable<object>));
        if (selectorConstructor == null) return;

		List<object> optionList = options.ToList();

        if (propertyReference is PropertyRef displayReference)
        {
            string settingsType = displayReference.Instance?.GetType().FullName;
            string propertyName = displayReference.PropertyInfo.Name;
            for (int i = 0; i < optionList.Count; i++)
                optionList[i] = LocalizeOptionForDisplay(settingsType, propertyName, i, optionList[i]);
        }

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

        Type listType = selectorConstructor.GetParameters()[0].ParameterType;
        object displayOptions = listType == typeof(IEnumerable<TextObject>)
            ? (object)optionList.Select(value => value as TextObject ?? new TextObject(value?.ToString() ?? string.Empty)).ToList()
            : listType == typeof(IEnumerable<string>)
                ? (object)optionList.Select(value => value?.ToString() ?? string.Empty).ToList()
                : optionList;
        var liveItems = AccessTools.Property(selectorVm.GetType(), "ItemList")?.GetValue(selectorVm) as IList;
        if (liveItems == null) return;
        if (ReadSelectedIndex(selectorVm) == selectedIndex && liveItems.Count == optionList.Count
            && liveItems.Cast<object>().Select(item => item?.ToString() ?? string.Empty)
                .SequenceEqual(optionList.Select(item => item?.ToString() ?? string.Empty))) return;
        object stagedSelector = selectorConstructor.Invoke(new object[] { displayOptions, selectedIndex });
        var stagedItems = AccessTools.Property(stagedSelector.GetType(), "ItemList")?.GetValue(stagedSelector) as IList;
        if (stagedItems == null || stagedItems.Count != optionList.Count) return;
        // Replace the selector as one datasource change. Clearing a list already bound
        // to Gauntlet exposes an empty intermediate state and stale SelectedItem bindings.
        FieldInfo selectorField = AccessTools.Field(vmType, "_selectorVMWrapper");
        bool canAssign = selectorField != null && selectorField.FieldType.IsInstanceOfType(stagedSelector);
        if (!canAssign && selectorProperty.CanWrite != true) return;
        var detached = new List<Tuple<EventInfo, Delegate>>();
        bool committed = false;
        try
        {
            foreach (var handler in new[] { Tuple.Create("PropertyChanged", "DropdownValue_PropertyChanged"),
                Tuple.Create("PropertyChangedWithValue", "DropdownValue_PropertyChangedWithValue") })
            {
                EventInfo changeEvent = selectorVm.GetType().GetEvent(handler.Item1);
                MethodInfo callback = AccessTools.Method(vmType, handler.Item2);
                if (changeEvent == null || callback == null) continue;
                Delegate listener = Delegate.CreateDelegate(changeEvent.EventHandlerType, settingsPropertyVm, callback);
                changeEvent.RemoveEventHandler(selectorVm, listener);
                detached.Add(Tuple.Create(changeEvent, listener));
            }
            if (vmType.FullName == "MCM.UI.GUI.ViewModels.SettingsPropertyVM" && detached.Count != 2) return;
            if (canAssign) selectorField.SetValue(settingsPropertyVm, stagedSelector);
            else selectorProperty.SetValue(settingsPropertyVm, stagedSelector);
            committed = true;
            // The new selector already has its index, SelectedItem and labels. Its
            // constructor ran without configuration listeners; refresh is display-only.
            MethodInfo notify = AccessTools.Method(typeof(TaleWorlds.Library.ViewModel), "OnPropertyChanged", new[] { typeof(string) });
            notify?.Invoke(settingsPropertyVm, new object[] { "DropdownValue" });
        }
        catch
        {
            if (committed)
            {
                if (canAssign) selectorField.SetValue(settingsPropertyVm, selectorVm);
                else selectorProperty.SetValue(settingsPropertyVm, selectorVm);
                committed = false;
            }
            throw;
        }
        finally
        {
            object listenerTarget = committed ? stagedSelector : selectorVm;
            foreach (var handler in detached) handler.Item1.AddEventHandler(listenerTarget, handler.Item2);
            // The live selector owns prepared items after commit. Keep the old list
            // intact for UI bindings being detached; it becomes collectible normally.
            if (!committed) (stagedSelector as TaleWorlds.Library.ViewModel)?.OnFinalize();
        }

	}



    private static bool IsAnimusForgeOwner(Type type)
    {
        string name = type?.FullName ?? string.Empty;
        return name == "AnimusForge.DuelSettings"
            || name == "AnimusForge.DialogueUI.DialogueUiSettings"
            || name == "AnimusForge.Illustrator.IllustratorSettings"
            || name == "AnimusForge.CoupSystem.CoupSettings";
    }

    internal static object LocalizeOptionForDisplay(string settingsType, string propertyName, int index, object value)
    {
        if (settingsType != "AnimusForge.DialogueUI.DialogueUiSettings") return value;
        string key;
        string fallback;
        if (propertyName == "InterfaceStyleDropdown")
        {
            if (index == 0) { key = "AFD_MCM_Interface_Original"; fallback = "Original"; }
            else if (index == 1) { key = "AFD_MCM_Interface_New"; fallback = "New UI"; }
            else return value;
        }
        else if (propertyName == "ShoutPanelStyleDropdown")
        {
            if (index == 0) { key = "AFD_MCM_ShoutStyle_PausedScroll"; fallback = "Paused scroll"; }
            else if (index == 1) { key = "AFD_MCM_ShoutStyle_Scroll"; fallback = "Scroll"; }
            else if (index == 2) { key = "AFD_MCM_ShoutStyle_SideFolio"; fallback = "Side folio"; }
            else return value;
        }
        else return value;
        return new TextObject("{=" + key + "}" + fallback).ToString();
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

