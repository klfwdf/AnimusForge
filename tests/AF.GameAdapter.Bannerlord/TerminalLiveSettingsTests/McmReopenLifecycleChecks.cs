using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AnimusForge;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;

// Actual MCM entry/outer close path; no native renderer or GPU inference.
static class McmReopenLifecycleChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    sealed class OtherSettings : AttributeGlobalSettings<OtherSettings>
    {
        public override string Id => "af.tests.other.settings";
        public override string DisplayName => "Other mod";
        public override string FormatType => "none";
        [MCM.Abstractions.Attributes.v2.SettingPropertyBool("Other enabled")]
        public bool Enabled { get; set; }
    }
    static int Listeners(object owner)
    {
        for (Type type = owner.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField("PropertyChanged", All | BindingFlags.DeclaredOnly);
            if (field != null && typeof(Delegate).IsAssignableFrom(field.FieldType))
                return (field.GetValue(owner) as Delegate)?.GetInvocationList().Length ?? 0;
        }
        throw new Exception("PropertyChanged backing field unavailable");
    }
    internal static void Run(Assembly af, Assembly ui, FluentGlobalSettings[] pages, Action<bool, string> check)
    {
        var refresh = af.GetType("AnimusForge.McmDropdownRuntimeRefresh", true);
        refresh.GetMethod("EnsurePatched", All).Invoke(null, null);
        var tracked = (IList)refresh.GetField("LiveSettingsPropertyVms", All).GetValue(null);
        var dropdowns = (IList)refresh.GetField("LiveDropdownPropertyVms", All).GetValue(null);
        var parentType = ui.GetType("MCM.UI.GUI.ViewModels.ModOptionsVM", true);
        var vmType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsVM", true);
        var entryType = ui.GetType("MCM.UI.GUI.ViewModels.SettingsEntryVM", true);
        object Panel()
        {
            object panel = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(parentType);
            var listField = parentType.GetField("<ModSettingsList>k__BackingField", All);
            listField.SetValue(panel, Activator.CreateInstance(listField.FieldType));
            refresh.GetMethod("ModOptionsVmCtorPostfix", All).Invoke(null, new[] { panel });
            return panel;
        }
        object Open(BaseSettings page, object panel)
        {
            object vm = Activator.CreateInstance(vmType, new object[] { new SettingsDefinition(page.Id), panel });
            object entry = Activator.CreateInstance(entryType, new[] { vm, null });
            var list = parentType.GetProperty("ModSettingsList").GetValue(panel);
            list.GetType().GetMethod("Add").Invoke(list, new[] { entry });
            return vm;
        }
        void Close(object panel) => parentType.GetMethod("OnFinalize").Invoke(panel, null);
        var owners = TerminalProviderProbe.Owners.Values.ToArray();
        var baselineListeners = owners.Select(Listeners).ToArray();
        int baseline = tracked.Count, dropdownBaseline = dropdowns.Count;
        var duel = (DuelSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
        int gpuIndex = duel.RerankerDeviceDropdown.SelectedIndex;
        try
        {
            duel.RerankerDeviceDropdown.SelectedIndex = 1;
            for (int cycle = 1; cycle <= 8; cycle++)
            {
                object panel = Panel();
                foreach (var page in pages) Open(page, panel);
                check(tracked.Count > baseline && dropdowns.Count > dropdownBaseline && dropdowns.Count < tracked.Count,
                    "only AF dropdown rows queued for refresh cycle " + cycle);
                for (int i = 0; i < 3; i++) refresh.GetMethod("OnApplicationTick", All).Invoke(null, null);
                Close(panel);
                check(tracked.Count == baseline && dropdowns.Count == dropdownBaseline, "real outer close releases all AF rows cycle " + cycle);
                check(owners.Select(Listeners).SequenceEqual(baselineListeners), "real outer close detaches subscriptions cycle " + cycle);
                check(duel.RerankerDeviceDropdown.SelectedIndex == 1, "GPU choice preserved without inference cycle " + cycle);
            }
            object first = Panel(), second = Panel();
            Open(pages[0], first);
            int firstCount = tracked.Count - baseline;
            Open(pages[1], second);
            int secondCount = tracked.Count - baseline - firstCount;
            Close(first);
            check(tracked.Count == baseline + secondCount, "closing one panel retains the other panel's rows");
            Close(second);
            check(tracked.Count == baseline, "overlapping panels clean independently");
            Open(pages[0], first);
            check(tracked.Count == baseline && dropdowns.Count == dropdownBaseline, "late asynchronous rows on closed panel immediately retire");
            check(owners.Select(Listeners).SequenceEqual(baselineListeners), "late rows detach owner subscriptions");

            var other = new OtherSettings();
            TerminalProviderProbe.Owners.Add(other.Id, other);
            try
            {
                object panel = Panel(), otherVm = Open(other, panel);
                int otherListeners = Listeners(other);
                foreach (var page in pages) Open(page, panel);
                Close(panel);
                check(Listeners(other) == otherListeners && otherListeners > 0, "AF cleanup leaves other mod subscriptions untouched");
                vmType.GetMethod("OnFinalize").Invoke(otherVm, null);
            }
            finally { TerminalProviderProbe.Owners.Remove(other.Id); }

            var property = pages[1].GetAllSettingPropertyDefinitions().Single(p => p.Id == "MinimumPositiveNpcRelationGain");
            object old = property.PropertyReference.Value, cancelPanel = Panel();
            object cancelVm = Open(pages[1], cancelPanel);
            object stack = vmType.GetProperty("URS").GetValue(cancelVm);
            var actionType = ui.GetType("MCM.UI.Actions.SetValueTypeAction`1", true).MakeGenericType(typeof(int));
            stack.GetType().GetMethod("Do").Invoke(stack, new[] { Activator.CreateInstance(actionType, new object[] { property.PropertyReference, 7 }) });
            parentType.GetMethod("ExecuteCancelInternal").Invoke(cancelPanel, new object[] { false, null });
            check(Equals(property.PropertyReference.Value, old), "native outer cancel still undoes changes after cleanup");
            check(tracked.Count == baseline && dropdowns.Count == dropdownBaseline, "native cancel releases row registries");
        }
        finally { duel.RerankerDeviceDropdown.SelectedIndex = gpuIndex; }
        Console.WriteLine("MCM outer close/reopen lifecycle regression PASS");
    }
}
