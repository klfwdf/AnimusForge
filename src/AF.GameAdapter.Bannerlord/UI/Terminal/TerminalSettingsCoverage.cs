using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AnimusForge.CoupSystem;
using AnimusForge.DialogueUI;
using AnimusForge.Illustrator;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base;
using MCM.Common;
using TaleWorlds.Localization;

namespace AnimusForge;

// MCM remains the persistence/metadata owner. Discover only AF's four types once,
// compile property access once, and rebuild localized labels only on language change.
// This never runs on Campaign/Mission Tick or scans other mods' settings.
internal static class TerminalSettingsCoverage
{
    private sealed class Binding
    {
        internal string Id, Owner, Property, Group;
        internal BaseSettingPropertyAttribute Attribute;
        internal int GroupOrder;
        internal TerminalSettingType Kind;
        internal float Min, Max;
        internal string Format;
        internal Func<BaseSettings> ResolveOwner;
        internal Func<BaseSettings, object> Get;
        internal Action<BaseSettings, object> Set;
    }

    private static readonly Lazy<Binding[]> Bindings = new Lazy<Binding[]>(Discover);
    private static readonly Lazy<Dictionary<string, Binding>> BindingById = new Lazy<Dictionary<string, Binding>>(
        () => Bindings.Value.ToDictionary(binding => binding.Id, StringComparer.Ordinal));
    private static readonly HashSet<BaseSettings> DirtyOwners = new HashSet<BaseSettings>();
    private static readonly HashSet<string> UiProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "CourierLetterThemeDropdown", "BubbleFontSize",
        "ShoutInputUiBackgroundDropdown", "EnableDevEditHistory", "ShoutKey",
        "ShoutSpecialMenuKey", "TerminalKey",
        "EnableWorldDiplomacyMapNotifications", "ScreenNotifications",
        "TownAmbientAiShowUsageNotifications"
    };

    internal static string CategoryFor(string owner, string property, string fallback, string group = "")
    {
        if (property.IndexOf("WeeklyReport", StringComparison.Ordinal) >= 0 || property.IndexOf("WorldBulletin", StringComparison.Ordinal) >= 0)
            return TerminalCategories.Diplomacy;
        if (property == "EditCustomDirectorPrompt" || property == "Illustrator.EditCustomDirectorPrompt") return TerminalCategories.Prompt;
        if (property.StartsWith("WorldDiplomacy", StringComparison.Ordinal) || property.StartsWith("EditWorldDiplomacy", StringComparison.Ordinal)
            || property.StartsWith("EnableWorldDiplomacy", StringComparison.Ordinal)) return TerminalCategories.Diplomacy;
        if (group.IndexOf("AF_MCM_Diplomacy_", StringComparison.Ordinal) >= 0) return TerminalCategories.Diplomacy;
        if (group.IndexOf("AF_MCM_Group_News", StringComparison.Ordinal) >= 0) return TerminalCategories.Diplomacy;
        if (owner == nameof(DialogueUiSettings) || (owner == nameof(DuelSettings) && UiProperties.Contains(property)))
            return TerminalCategories.Ui;
        if (owner == nameof(CoupSettings)) return TerminalCategories.Diplomacy;
        if (owner == nameof(IllustratorSettings))
            return group.IndexOf("AF_ILL_GROUP_IMAGE_API", StringComparison.Ordinal) >= 0 ||
                   group.IndexOf("AF_ILL_GROUP_DIRECTOR_API", StringComparison.Ordinal) >= 0 ||
                   property == "ApiBaseUrl" || property == "ApiKey" || property == "UseExactEndpointUrl" ||
                   property == "ModelName" || property == "ModelDropdown" || property == "FetchModelList" ||
                   property == "TestImageApi" || property == "ImageGenerationTimeoutSeconds"
                ? TerminalCategories.Ai : TerminalCategories.Ui;
        if (group.IndexOf("AF_MCM_CivilWar_Group", StringComparison.Ordinal) >= 0 ||
            property.StartsWith("PolicyVote", StringComparison.Ordinal) || property == "EditSettlementBalance" ||
            property == "DisableProsperityFoodConsumption") return TerminalCategories.Diplomacy;
        if (property == "SetsOtherSettlementFollowerLimit") return TerminalCategories.Duel;
        if (group.IndexOf("AF_MCM_TownAmbient_AiGroup", StringComparison.Ordinal) >= 0) return TerminalCategories.Ai;
        if (group.IndexOf("SAX_MCM_Group", StringComparison.Ordinal) >= 0 ||
            group.IndexOf("AF_MCM_TownAmbient_Group", StringComparison.Ordinal) >= 0 ||
            property == "AutoExcludeUnframedShoutParticipants") return TerminalCategories.Scene;
        return fallback;
    }

    internal static bool IsVisible(string id)
    {
        if (id == "TownAmbientAiShowUsageNotifications" || id == "ScreenNotifications"
            || id == "EnemyInterruptRadiusMeters" || id == "DiagnosticsEnabled"
            || id == "Illustrator.UseExactEndpointUrl" || id == "TerminalHotkeyEnabled" || id == "TerminalMapIconEnabled") return false;
        if (id.StartsWith("Illustrator.Director", StringComparison.Ordinal) || id == "Illustrator.FetchDirectorModelList") return false;
        return true;
    }

    internal static string ViewGroup(string id, string fallback)
    {
        if (id.StartsWith("Coup.", StringComparison.Ordinal)) return Label("AF_TERMINAL_GROUP_COUP", "Armed coup");
        if (id == "Illustrator.EditCustomDirectorPrompt") return Label("AF_TERMINAL_GROUP_DIRECTOR_RULES", "Illustration director rules");
        if (id.StartsWith("WorldDiplomacy", StringComparison.Ordinal) || id.StartsWith("EnableWorldDiplomacy", StringComparison.Ordinal)
            || id.StartsWith("EditWorldDiplomacy", StringComparison.Ordinal))
            return id.StartsWith("Edit", StringComparison.Ordinal) || id.EndsWith("Prompt", StringComparison.Ordinal)
                ? Label("AF_TERMINAL_GROUP_DIPLOMACY_PROMPTS", "AI diplomacy prompts")
                : Label("AF_TERMINAL_GROUP_DIPLOMACY_SETTINGS", "AI diplomacy settings");
        if (id.Contains("WeeklyReport") || id.Contains("WorldBulletin")) return Label("AF_TERMINAL_GROUP_NEWS", "Weekly reports and bulletins");
        BindingById.Value.TryGetValue(id, out Binding binding);
        if (binding != null)
        {
            if (binding.Group.IndexOf("AF_MCM_Diplomacy_", StringComparison.Ordinal) >= 0)
                return binding.Property.StartsWith("Edit", StringComparison.Ordinal)
                    ? Label("AF_TERMINAL_GROUP_DIPLOMACY_PROMPTS", "AI diplomacy prompts")
                    : Label("AF_TERMINAL_GROUP_DIPLOMACY_SETTINGS", "AI diplomacy settings");
            if (binding.Group.IndexOf("SAX_MCM_Group", StringComparison.Ordinal) >= 0)
                return Label("AF_TERMINAL_GROUP_NATURAL", "Natural-language actions and battle speech");
            if (binding.Owner == nameof(IllustratorSettings) && binding.Group.IndexOf("AF_ILL_GROUP_IMAGE_API", StringComparison.Ordinal) >= 0)
                return Label("AF_TERMINAL_GROUP_IMAGE_API", "Image generation API");
            if (binding.Group.IndexOf("AF_MCM_Group_News", StringComparison.Ordinal) >= 0)
                return Label("AF_TERMINAL_GROUP_NEWS", "Weekly reports and bulletins");
        }
        return fallback;
    }

    internal static int ViewOrder(string id)
    {
        if (id == "Coup.Enabled" || id == "EnableWorldDiplomacy" || id == "EnableWorldBulletin") return 0;
        if (id == "Coup.MinimumClanTier") return 10;
        if (id == "Coup.MinimumInfluence") return 11;
        if (id == "Coup.MinimumTroops") return 12;
        if (!BindingById.Value.TryGetValue(id, out Binding binding)) return 100;
        if (id.StartsWith("Coup.", StringComparison.Ordinal))
        {
            int section = binding.Group.IndexOf("AF_COUP_GROUP_ASSAULT", StringComparison.Ordinal) >= 0 ? 20
                : binding.Group.IndexOf("AF_COUP_GROUP_GUARDS", StringComparison.Ordinal) >= 0 ? 30 : 40;
            return section + binding.Attribute.Order;
        }
        return 100 + binding.GroupOrder * 100 + binding.Attribute.Order;
    }

    internal static int ViewGroupOrder(string id)
    {
        if (!BindingById.Value.TryGetValue(id, out Binding binding)) return 0;
        if (binding.Owner == nameof(IllustratorSettings) && binding.Group.IndexOf("AF_ILL_GROUP_IMAGE_API", StringComparison.Ordinal) >= 0) return 50;
        return binding.Group.IndexOf("AF_MCM_TownAmbient_AiGroup", StringComparison.Ordinal) >= 0 ? 60 : 0;
    }

    private static string Label(string key, string fallback) => new TextObject("{=" + key + "}" + fallback).ToString();

    internal static void AddMissing(List<TerminalSettingDef> definitions)
    {
        var existing = new HashSet<string>(definitions.Select(d => d.Id), StringComparer.Ordinal);
        foreach (Binding binding in Bindings.Value)
        {
            if (!existing.Add(binding.Id)) continue;
            string category = CategoryFor(binding.Owner, binding.Property, TerminalCategories.Ai, binding.Group);
            Func<DuelSettings, object> get = s => Read(binding, s);
            Action<DuelSettings, object> set = binding.Kind == TerminalSettingType.Button ? null :
                (Action<DuelSettings, object>)((s, value) => Write(binding, s, value));
            Action<DuelSettings> invoke = binding.Kind == TerminalSettingType.Button
                ? (Action<DuelSettings>)(s => (Read(binding, s) as Action)?.Invoke()) : null;
            Func<DuelSettings, Dropdown<string>> dropdown = binding.Kind == TerminalSettingType.Dropdown
                ? (Func<DuelSettings, Dropdown<string>>)(s => ReadDropdown(binding, s)) : null;
            if (dropdown != null) get = s => ReadDropdown(binding, s)?.SelectedValue ?? string.Empty;
            definitions.Add(new TerminalSettingDef(binding.Id, category, LocalizeGroup(binding.Group),
                Localize(binding.Attribute.DisplayName), Localize(binding.Attribute.HintText), binding.Kind,
                binding.Min, binding.Max, binding.Kind == TerminalSettingType.FloatingInteger ? 0.05f : 1f,
                Localize(binding.Format), get, set, invoke, dropdown));
        }
    }

    internal static bool SaveDirtyOwners()
    {
        bool success = true;
        foreach (BaseSettings owner in DirtyOwners.ToArray())
        {
            try
            {
                if (BaseSettingsProvider.Instance == null) { success = false; continue; }
                BaseSettingsProvider.Instance.SaveSettings(owner);
                DirtyOwners.Remove(owner);
            }
            catch (Exception ex)
            {
                success = false;
                Logger.Log("Terminal", "[ERROR] Failed to save settings owner " + owner.Id + ": " + ex);
            }
        }
        return success;
    }

    private static BaseSettings Owner(Binding binding, DuelSettings duel)
        => binding.Owner == nameof(DuelSettings) ? duel : binding.ResolveOwner();

    private static object Read(Binding binding, DuelSettings duel)
    {
        BaseSettings owner = Owner(binding, duel);
        return owner == null ? null : binding.Get(owner);
    }

    private static Dropdown<string> ReadDropdown(Binding binding, DuelSettings duel)
        => Read(binding, duel) as Dropdown<string>;

    private static void Write(Binding binding, DuelSettings duel, object value)
    {
        BaseSettings owner = Owner(binding, duel);
        if (owner == null) throw new InvalidOperationException("Settings owner is unavailable: " + binding.Owner);
        if (binding.Kind == TerminalSettingType.Dropdown)
        {
            var dropdown = binding.Get(owner) as Dropdown<string>;
            if (dropdown == null || !(value is int index) || index < 0 || index >= dropdown.Count) return;
            dropdown.SelectedIndex = index;
            // Some getters normalize to a fresh dropdown. Invoke the original setter
            // so letter themes and model selectors persist their selected index.
            binding.Set(owner, dropdown);
        }
        else binding.Set(owner, value);
        if (binding.Owner != nameof(DuelSettings)) DirtyOwners.Add(owner);
    }

    private static Binding[] Discover()
    {
        var result = new List<Binding>();
        AddOwner<DuelSettings>(result, "", () => DuelSettings.GetSettings());
        AddOwner<DialogueUiSettings>(result, "DialogueUi.", () => DialogueUiSettings.Instance);
        AddOwner<IllustratorSettings>(result, "Illustrator.", () => IllustratorSettings.Instance);
        AddOwner<CoupSettings>(result, "Coup.", () => CoupSettings.Instance);
        return result.OrderBy(b => b.Owner, StringComparer.Ordinal).ThenBy(b => b.GroupOrder)
            .ThenBy(b => b.Group, StringComparer.Ordinal).ThenBy(b => b.Attribute.Order)
            .ThenBy(b => b.Property, StringComparer.Ordinal).ToArray();
    }

    private static void AddOwner<T>(List<Binding> result, string prefix, Func<BaseSettings> resolve) where T : BaseSettings
    {
        foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = property.GetCustomAttributes(typeof(BaseSettingPropertyAttribute), true)
                .Cast<BaseSettingPropertyAttribute>().SingleOrDefault();
            if (attribute == null) continue;
            var group = property.GetCustomAttributes(typeof(SettingPropertyGroupAttribute), true)
                .Cast<SettingPropertyGroupAttribute>().SingleOrDefault();
            var binding = new Binding
            {
                Id = prefix + property.Name, Owner = typeof(T).Name, Property = property.Name,
                Group = group?.GroupName ?? string.Empty, GroupOrder = group?.GroupOrder ?? 0,
                Attribute = attribute, ResolveOwner = resolve, Format = string.Empty
            };
            if (attribute is SettingPropertyBoolAttribute) binding.Kind = TerminalSettingType.Bool;
            else if (attribute is SettingPropertyIntegerAttribute integer)
            {
                binding.Kind = TerminalSettingType.Integer; binding.Min = (float)integer.MinValue;
                binding.Max = (float)integer.MaxValue; binding.Format = integer.ValueFormat;
            }
            else if (attribute is SettingPropertyFloatingIntegerAttribute number)
            {
                binding.Kind = TerminalSettingType.FloatingInteger; binding.Min = (float)number.MinValue;
                binding.Max = (float)number.MaxValue; binding.Format = number.ValueFormat;
            }
            else if (attribute is SettingPropertyDropdownAttribute) binding.Kind = TerminalSettingType.Dropdown;
            else if (attribute is SettingPropertyButtonAttribute) binding.Kind = TerminalSettingType.Button;
            else if (attribute is SettingPropertyTextAttribute) binding.Kind = TerminalSettingType.Text;
            else throw new InvalidOperationException("Unsupported terminal setting: " + typeof(T).Name + "." + property.Name);
            var instance = Expression.Parameter(typeof(BaseSettings), "owner");
            var access = Expression.Property(Expression.Convert(instance, typeof(T)), property);
            binding.Get = Expression.Lambda<Func<BaseSettings, object>>(Expression.Convert(access, typeof(object)), instance).Compile();
            if (binding.Kind != TerminalSettingType.Button)
            {
                var value = Expression.Parameter(typeof(object), "value");
                binding.Set = Expression.Lambda<Action<BaseSettings, object>>(
                    Expression.Assign(access, Expression.Convert(value, property.PropertyType)), instance, value).Compile();
            }
            result.Add(binding);
        }
    }

    private static string Localize(string text)
        => string.IsNullOrEmpty(text) ? string.Empty : new TextObject(text).ToString();

    private static string LocalizeGroup(string text)
    {
        // A slash inside one key's English fallback is part of that translated
        // title. Only /{= introduces another independently localized group.
        string[] parts = (text ?? string.Empty).Split(new[] { "/{=" }, StringSplitOptions.None);
        for (int index = 0; index < parts.Length; index++)
            parts[index] = Localize(index == 0 ? parts[index] : "{=" + parts[index]);
        return TerminalSettingsLocalization.NormalizeGroupDisplay(string.Join("/", parts));
    }
}
