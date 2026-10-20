using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using TaleWorlds.Localization;

namespace AnimusForge;

// UI views only: no formatter, business JSON or alternate settings instances.
// Rebuild on MCM opening, before its parallel VM construction, never on Tick.
internal static class AnimusForgeMcmPages
{
    internal const string ApiId = "AnimusForge_Api_UI_v1";
    internal const string TerminalId = "AnimusForge_Terminal_UI_v1";
    private static FluentGlobalSettings _api, _terminal;

    internal static void EnsurePatched(Harmony harmony)
    {
        var target = AccessTools.Method(AccessTools.TypeByName("MCM.UI.GUI.ViewModels.ModOptionsVM"), "InitializeModSettings");
        if (target == null) throw new MissingMethodException("MCM page initialization is unavailable.");
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(AnimusForgeMcmPages), nameof(RegisterPages)));
    }

    private static void RegisterPages()
    {
        AnimusForgeMcmVisibility.RestoredPagesReady = false;
        var owners = new[] { "AnimusForge_global_settings", "AnimusForge_DialogueUI_v1", "AnimusForge_Illustrator_v1", "AnimusForge_Coup_v1" }
            .Select(id => BaseSettingsProvider.Instance?.GetSettings(id)).ToArray();
        // Keep the original frontend when a dependency/owner is unavailable.
        if (owners.Any(owner => owner == null)) return;
        var pages = CreatePages(owners);
        _api?.Unregister();
        _terminal?.Unregister();
        _api = pages[0];
        _terminal = pages[1];
        _api.Register();
        _terminal.Register();
        AnimusForgeMcmVisibility.RestoredPagesReady =
            ReferenceEquals(BaseSettingsProvider.Instance.GetSettings(ApiId), _api) &&
            ReferenceEquals(BaseSettingsProvider.Instance.GetSettings(TerminalId), _terminal);
    }

    internal static FluentGlobalSettings[] CreatePages(BaseSettings[] owners)
    {
        var api = new List<ISettingsPropertyDefinition>();
        var terminal = new List<ISettingsPropertyDefinition>();
        var defaults = new Dictionary<string, object>(StringComparer.Ordinal);
        var definitions = TerminalSettingsRegistry.AllDefinitions.ToDictionary(d => d.Id, StringComparer.Ordinal);
        foreach (BaseSettings owner in owners)
        {
            string prefix = Prefix(owner.Id);
            BaseSettings defaultOwner = owner.GetBuiltInPresets().First(p => p.Id == BaseSettings.DefaultPresetId).LoadPreset();
            foreach (var property in defaultOwner.GetAllSettingPropertyDefinitions())
                defaults[prefix + property.Id] = property.PropertyReference.Value;
            foreach (var property in owner.GetAllSettingPropertyDefinitions())
            {
                string id = prefix + property.Id;
                if (!TerminalSettingsCoverage.IsVisible(id) || !definitions.TryGetValue(id, out var def)) continue;
                bool isApi = AnimusForgeMcmVisibility.IsVisibleProperty(owner.Id, property.Id, property.GroupName);
                string group = isApi ? ApiGroup(owner.Id, property) :
                    new TextObject(TerminalCategories.DisplayText(def.TabCategory)).ToString() + "/" + def.GroupName;
                int order = isApi ? ApiOrder(owner.Id, property) : TerminalSettingsCoverage.ViewOrder(id);
                // Retain the original PropertyRef: native dropdown refresh, setters,
                // UndoRedoStack and terminal all address the exact same owner.
                var metadata = new Metadata(id, def, property, group, order);
                var projected = new SettingsPropertyDefinition(
                    SettingsUtils.GetPropertyDefinitionWrappers(property).Concat(new IPropertyDefinitionBase[] { metadata }),
                    metadata, property.PropertyReference, '/');
                (isApi ? api : terminal).Add(projected);
            }
        }
        void Save(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != BaseSettings.SaveTriggered) return;
            // Original owners keep their JSON fields, paths and native preset IDs.
            foreach (BaseSettings owner in owners) BaseSettingsProvider.Instance.SaveSettings(owner);
        }
        FluentGlobalSettings Page(string id, string title, List<ISettingsPropertyDefinition> properties) =>
            new FluentGlobalSettings(id, new TextObject(title).ToString(), "AnimusForge", "", "none", 2, '/', Save,
                SettingsUtils.GetSettingsPropertyGroups('/', properties), new[] { new DefaultPreset(defaults) });
        return new[]
        {
            Page(ApiId, "{=AF_MCM_API_PAGE}AnimusForge API settings", api),
            Page(TerminalId, "{=AF_MCM_TERMINAL_PAGE}AnimusForge terminal settings", terminal)
        };
    }

    private static string Prefix(string id) => id == "AnimusForge_DialogueUI_v1" ? "DialogueUi." :
        id == "AnimusForge_Illustrator_v1" ? "Illustrator." : id == "AnimusForge_Coup_v1" ? "Coup." : "";

    private static string ApiGroup(string owner, ISettingsPropertyDefinition property)
    {
        if (owner == "AnimusForge_Illustrator_v1") return "{=AF_TERMINAL_GROUP_IMAGE_API}Image generation API";
        if (property.GroupName.IndexOf("{=AF_MCM_Tts_Group}", StringComparison.Ordinal) >= 0) return "{=AF_TERMINAL_GROUP_TTS_API}TTS API";
        return property.GroupName;
    }

    private static int ApiOrder(string owner, ISettingsPropertyDefinition property) =>
        owner == "AnimusForge_Illustrator_v1" ? 50 : property.GroupOrder;

    private sealed class Metadata : IPropertyDefinitionBase, IPropertyDefinitionWithId, IPropertyGroupDefinition, IPropertyDefinitionGroupToggle
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string HintText { get; }
        public int Order { get; }
        public bool RequireRestart { get; }
        public string GroupName { get; }
        public int GroupOrder { get; }
        // Terminal groups can combine multiple original groups. Keep every
        // enable switch as a visible row instead of one hiding the others.
        public bool IsToggle => false;
        internal Metadata(string id, TerminalSettingDef def, ISettingsPropertyDefinition original, string group, int groupOrder)
        {
            Id = id; DisplayName = def.DisplayName; HintText = def.HintText;
            Order = original.Order; RequireRestart = original.RequireRestart; GroupName = group; GroupOrder = groupOrder;
        }
    }

    private sealed class DefaultPreset : ISettingsPresetBuilder
    {
        private readonly Dictionary<string, object> _values;
        internal DefaultPreset(Dictionary<string, object> values) { _values = values; }
        public ISettingsPresetBuilder SetPropertyValue(string name, object value) { _values[name] = value; return this; }
        public ISettingsPreset Build(BaseSettings detached) => new MemorySettingsPreset(detached.Id,
            BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, () =>
            {
                foreach (var property in detached.GetAllSettingPropertyDefinitions())
                    if (property.SettingType != SettingType.Button && _values.TryGetValue(property.Id, out object value))
                        property.PropertyReference.Value = value is ICloneable cloneable ? cloneable.Clone() : value;
                return detached;
            });
    }
}
