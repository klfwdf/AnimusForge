using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using TaleWorlds.Library;

namespace AnimusForge;

// Frontend projection only. MCM discovery, cached metadata, settings instances,
// presets and serialization retain the complete property set and original values.
internal static class AnimusForgeMcmVisibility
{
    private const string MainId = "AnimusForge_global_settings";
    private const string IllustratorId = "AnimusForge_Illustrator_v1";
    private const string DialogueUiId = "AnimusForge_DialogueUI_v1";
    private const string CoupId = "AnimusForge_Coup_v1";
    private static readonly string[] MainGroups =
    {
        "{=AF_MCM_Support_Group}", "{=AF_MCM_MainApi_Group}", "{=AF_MCM_AuxApi_Group}",
        "{=AF_MCM_PostprocessApi_Group}", "{=AF_MCM_EventRebellionApi_Group}", "{=AF_MCM_Tts_Group}"
    };
    private static readonly string[] IllustratorGroups = { "{=AF_ILL_GROUP_IMAGE_API}", "{=AF_ILL_GROUP_DIRECTOR_API}" };
    private static readonly HashSet<string> AmbientApiProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "TownAmbientAiApiUrl", "TownAmbientAiApiKey", "TownAmbientAiModelName", "FetchTownAmbientAiModelList",
        "TownAmbientAiModelDropdown", "TestTownAmbientAiConnection", "TownAmbientAiMaxOutputTokens"
    };
    private static readonly HashSet<string> MainNonApiProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "ActionPostprocessHistoryEntryLimit", "EnableTtsSpeech", "TtsVolcDedicatedEnabled",
        "TtsSceneUseWinmmAudible", "TtsLipSyncSoundEventVolume"
    };
    private static Func<BaseSettings, IEnumerable<SettingsPropertyGroupDefinition>> _originalGroups;
    private static MethodInfo _groupsMethod;
    private static Type _entryType;
    private static Func<object, string> _entryId;
    private static MethodInfo _entryAddMethod;
    private static MethodInfo _filteredEntryAdd;
    private static bool _patched;

    internal static void EnsurePatched(Harmony harmony)
    {
        if (_patched || harmony == null) return;
        Type vm = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.SettingsVM");
        Type cache = AccessTools.TypeByName("MCM.UI.Utils.SettingPropertyDefinitionCache");
        Type entry = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.SettingsEntryVM");
        Type main = AccessTools.TypeByName("MCM.UI.GUI.ViewModels.ModOptionsVM");
        if (vm == null || cache == null || entry == null || main == null)
            throw new InvalidOperationException("MCM frontend visibility targets are unavailable.");
        MethodInfo groups = AccessTools.Method(cache, "GetSettingPropertyGroups", new[] { typeof(BaseSettings) });
        ConstructorInfo constructor = AccessTools.Constructor(vm, new[] { typeof(SettingsDefinition), main });
        MethodInfo add = typeof(Collection<>).MakeGenericType(entry).GetMethod("Add", new[] { entry });
        MethodInfo idGetter = entry.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod();
        if (groups == null || constructor == null || add == null || idGetter == null)
            throw new InvalidOperationException("MCM frontend visibility signature is unavailable.");
        _groupsMethod = groups;
        _originalGroups = (Func<BaseSettings, IEnumerable<SettingsPropertyGroupDefinition>>)Delegate.CreateDelegate(
            typeof(Func<BaseSettings, IEnumerable<SettingsPropertyGroupDefinition>>), groups);
        _entryType = entry;
        // The closed generic shim caches a typed getter; entry additions never reflect.
        _entryId = (Func<object, string>)typeof(AnimusForgeMcmVisibility).GetMethod(nameof(CreateIdGetter),
            BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(entry).Invoke(null, new object[] { idGetter });
        _entryAddMethod = add;
        _filteredEntryAdd = typeof(AnimusForgeMcmVisibility).GetMethod(nameof(AddFrontendEntry),
            BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(entry);
        MethodInfo entryInsertion = null;
        foreach (MethodInfo method in main.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (!method.Name.StartsWith("<InitializeModSettings>", StringComparison.Ordinal)) continue;
            foreach (CodeInstruction instruction in PatchProcessor.GetOriginalInstructions(method))
            {
                if (!instruction.Calls(add)) continue;
                if (entryInsertion != null) throw new InvalidOperationException("MCM frontend entry insertion is ambiguous.");
                entryInsertion = method;
            }
        }
        if (entryInsertion == null) throw new InvalidOperationException("MCM frontend entry insertion is unavailable.");
        harmony.Patch(constructor, transpiler: new HarmonyMethod(typeof(AnimusForgeMcmVisibility), nameof(FilterConstructorGroups)));
        // Replace only MCM's initialization callsite. Do not patch shared CLR
        // Collection<T> methods, or any other mod's binding lists.
        harmony.Patch(entryInsertion, transpiler: new HarmonyMethod(typeof(AnimusForgeMcmVisibility), nameof(FilterFrontendEntryInsertion)));
        _patched = true;
    }

    private static Func<object, string> CreateIdGetter<T>(MethodInfo method)
    {
        var getter = (Func<T, string>)Delegate.CreateDelegate(typeof(Func<T, string>), method);
        return entry => getter((T)entry);
    }

    private static IEnumerable<CodeInstruction> FilterFrontendEntryInsertion(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(_entryAddMethod))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = _filteredEntryAdd;
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("MCM frontend entry call count changed: " + replaced);
    }

    private static void AddFrontendEntry<T>(Collection<T> list, T entry)
    {
        if (KeepFrontendEntry(entry)) list.Add(entry);
    }

    private static IEnumerable<CodeInstruction> FilterConstructorGroups(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo projection = AccessTools.Method(typeof(AnimusForgeMcmVisibility), nameof(GetFrontendGroups));
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(_groupsMethod))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = projection;
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("MCM settings constructor group call count changed: " + replaced);
    }

    private static IEnumerable<SettingsPropertyGroupDefinition> GetFrontendGroups(BaseSettings settings)
    {
        IEnumerable<SettingsPropertyGroupDefinition> original = _originalGroups(settings);
        string id = settings?.Id;
        if (!IsOwnedId(id)) return original;
        var visible = new List<ISettingsPropertyDefinition>();
        foreach (SettingsPropertyGroupDefinition group in original)
            foreach (ISettingsPropertyDefinition property in SettingsUtils.GetAllSettingPropertyDefinitions(group))
                if (IsVisibleProperty(id, property.Id, property.GroupName)) visible.Add(property);
        // New UI nodes, same original references. Do not prune or clone/mutate
        // the cached source tree: backend save/preset/terminal metadata needs it.
        return SettingsUtils.GetSettingsPropertyGroups(settings.SubGroupDelimiter, visible);
    }

    internal static bool IsVisibleProperty(string settingsId, string propertyId, string rawGroup)
    {
        if (!IsOwnedId(settingsId)) return true;
        if (settingsId == DialogueUiId || settingsId == CoupId) return false;
        if (settingsId == IllustratorId) return HasGroup(rawGroup, IllustratorGroups);
        if (AmbientApiProperties.Contains(propertyId))
            return !string.IsNullOrEmpty(rawGroup) && rawGroup.IndexOf("{=AF_MCM_TownAmbient_AiGroup}", StringComparison.Ordinal) >= 0;
        return !MainNonApiProperties.Contains(propertyId) && HasGroup(rawGroup, MainGroups);
    }

    private static bool HasGroup(string rawGroup, string[] markers)
    {
        if (string.IsNullOrEmpty(rawGroup)) return false;
        foreach (string marker in markers) if (rawGroup.IndexOf(marker, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    private static bool IsOwnedId(string id) => id == MainId || id == IllustratorId || id == DialogueUiId || id == CoupId;

    private static bool KeepFrontendEntry(object __0)
    {
        if (__0 == null || __0.GetType() != _entryType) return true;
        string id = _entryId(__0);
        return id != DialogueUiId && id != CoupId;
    }
}
