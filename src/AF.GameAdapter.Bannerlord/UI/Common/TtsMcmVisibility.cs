using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MCM.Common;
using TaleWorlds.Library;

namespace AnimusForge;

// Native MCM already binds IsSettingVisible. Keep its search/collapse rules,
// then add only AF's provider mask. Original PropertyRefs/undo stacks stay intact.
internal static class TtsMcmVisibility
{
    private sealed class Row
    {
        internal DuelSettings Owner;
        internal string Id;
    }
    private static readonly object Sync = new object();
    private static readonly ConditionalWeakTable<object, Row> Rows = new ConditionalWeakTable<object, Row>();
    private static readonly List<WeakReference> LiveRows = new List<WeakReference>();
    private static int _pending;
    private static bool _patched;

    internal static void EnsurePatched(Harmony harmony)
    {
        if (_patched) return;
        try
        {
            Type type = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.SettingsPropertyVM");
            var visible = AccessTools.PropertyGetter(type, "IsSettingVisible");
            var changed = AccessTools.Method(type, "OnPropertyChanged", new[] { typeof(object), typeof(PropertyChangedEventArgs) });
            if (visible == null || changed == null) throw new MissingMethodException("MCM TTS visibility entry is unavailable.");
            harmony.Patch(visible, postfix: new HarmonyMethod(typeof(TtsMcmVisibility), nameof(VisiblePostfix)));
            harmony.Patch(changed, postfix: new HarmonyMethod(typeof(TtsMcmVisibility), nameof(ChangedPostfix)));
            _patched = true;
        }
        catch (Exception error)
        {
            // An unsupported MCM build must not disable unrelated dropdowns.
            Logger.Log("MCM", "[WARN] TTS provider visibility unavailable; existing settings UI retained: " + error);
        }
    }

    internal static void Register(object vm, PropertyRef reference)
    {
        if (!_patched || !(reference.Instance is DuelSettings owner)) return;
        string id = reference.PropertyInfo.Name;
        if (id != nameof(DuelSettings.TtsProviderDropdown) && !TtsSettingsVisibility.IsProviderSpecific(id)) return;
        lock (Sync)
        {
            if (Rows.TryGetValue(vm, out _)) return;
            Rows.Add(vm, new Row { Owner = owner, Id = id });
            LiveRows.Add(new WeakReference(vm));
        }
    }

    internal static void Unregister(object vm)
    {
        lock (Sync)
        {
            Rows.Remove(vm);
            LiveRows.RemoveAll(item => !item.IsAlive || ReferenceEquals(item.Target, vm));
        }
    }

    private static void VisiblePostfix(object __instance, ref bool __result)
    {
        if (__result && Rows.TryGetValue(__instance, out Row row))
            __result = TtsSettingsVisibility.IsVisible(row.Id, TtsSettingsVisibility.Provider(row.Owner));
    }

    private static void ChangedPostfix(object __instance)
    {
        if (!Rows.TryGetValue(__instance, out Row row) || row.Id != nameof(DuelSettings.TtsProviderDropdown)) return;
        // Native selector, undo, reset and preset edits share this notification.
        // Coalesce onto the existing main-thread queue; no new Tick scanning/IO.
        if (Interlocked.Exchange(ref _pending, 1) != 0) return;
        if (!McmDropdownRuntimeRefresh.PostToMainThread(Refresh)) Interlocked.Exchange(ref _pending, 0);
    }

    private static void Refresh()
    {
        Interlocked.Exchange(ref _pending, 0);
        object[] live;
        lock (Sync)
        {
            LiveRows.RemoveAll(item => !item.IsAlive);
            live = LiveRows.Select(item => item.Target).Where(item => item != null).ToArray();
        }
        foreach (object vm in live)
            if (Rows.TryGetValue(vm, out _) && vm is ViewModel model)
                model.OnPropertyChanged("IsSettingVisible");
    }
}
