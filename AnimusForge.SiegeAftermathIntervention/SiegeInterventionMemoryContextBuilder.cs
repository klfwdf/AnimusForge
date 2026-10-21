using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for GCCZ per-scene memory context.
/// TownSceneMemoryStore owns collection, duplicate suppression, and trimming.
/// </summary>
public static class SiegeInterventionMemoryContextBuilder
{
    private static readonly char CjkRangeStart = SiegeAftermathCompatibilityTexts.Raw("compat_memory__cjk_range_start_51a75f4634df")[0];
    private static readonly char CjkRangeEnd = SiegeAftermathCompatibilityTexts.Raw("compat_memory__cjk_range_end_3ab1b7b11e2b")[0];
    public const int MaxMemoryEvents = 10;

    public static string Build(IReadOnlyList<string> memoryEvents)
    {
        return Build(memoryEvents, SiegeInterventionMemoryAudience.General);
    }

    public static string Build(IReadOnlyList<string> memoryEvents, SiegeInterventionMemoryAudience audience)
    {
        return Build(memoryEvents, audience, TownPromptTextCatalog.CreateEnglishFallback());
    }

    public static string Build(
        IReadOnlyList<string> memoryEvents,
        SiegeInterventionMemoryAudience audience,
        TownPromptTextCatalog promptText)
    {
        if (memoryEvents == null || memoryEvents.Count == 0)
        {
            return string.Empty;
        }

        TownPromptTextCatalog catalog = TownPromptTextCatalog.Resolve(promptText);
        bool isChinese = IsChineseCatalog(catalog);
        string delimiter = isChinese ? "；" : "; ";
        string heading = !string.IsNullOrWhiteSpace(catalog.SiegeInterventionMemoryHeading)
            ? catalog.SiegeInterventionMemoryHeading
            : (isChinese ? SiegeAftermathCompatibilityTexts.Raw("compat_siege_intervention_memory_context_builder_05871cc5dc56") : "[SIEGE DISPOSITION MEMORY] ");
        string instruction = !string.IsNullOrWhiteSpace(catalog.SiegeInterventionMemoryInstruction)
            ? catalog.SiegeInterventionMemoryInstruction
            : (isChinese
                ? SiegeAftermathCompatibilityTexts.Raw("compat_siege_intervention_memory_context_builder_d75205474b7e")
                : ". These are committed facts that occurred during this town entry disposition; subsequent NPCs must acknowledge the overall situation and not act as though the player never gave these orders or civilians were never assembled.");

        var projectedEvents = new List<string>(memoryEvents.Count);
        foreach (string entry in memoryEvents) projectedEvents.Add(SiegeSceneMemoryPromptProjection.Project(entry, catalog));
        string baseContext = heading + string.Join(delimiter, projectedEvents) + instruction;
        string perspective = BuildPerspective(
            memoryEvents,
            audience,
            catalog,
            isChinese);
        return string.IsNullOrWhiteSpace(perspective) ? baseContext : baseContext + (isChinese ? "" : "\n") + perspective;
    }

    private static string BuildPerspective(
        IReadOnlyList<string> memoryEvents,
        SiegeInterventionMemoryAudience audience,
        TownPromptTextCatalog promptText,
        bool isChinese)
    {
        string joined = string.Join(";", memoryEvents ?? new List<string>());
        if (string.IsNullOrWhiteSpace(joined))
        {
            return string.Empty;
        }

        bool hasMercy = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("action_mercy"), SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ffd017edd459"), SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_d92e6f19cd41"), SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_96d322ea12a9"), "mercy", "relief", "reassurance", "oath");
        bool hasSoldierAppeasement = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("appease_done_title"), SiegeAftermathCompatibilityTexts.Raw("appease_morale_title"), "appeasement", "morale");
        bool hasGather = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("gather_memory_title"), SiegeAftermathCompatibilityTexts.Raw("assembly_memory_title"), "gather", "assembly");
        bool hasRobbery = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_7d5ba7987540"), "robbery");
        bool hasPlunder = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_42f0a44b7d95"), "plunder", "pillage");
        bool hasMassacre = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("loot_action_massacre"), "massacre", "slaughter");
        bool hasRepopulation = ContainsAny(joined, SiegeAftermathCompatibilityTexts.Raw("compat_legacy_town_tag_adapter_ee470e68a8ef"), "repopulation", "colonization");

        if (audience == SiegeInterventionMemoryAudience.AlliedSoldier)
        {
            var parts = new List<string>();
            if (hasRepopulation)
            {
                parts.Add(promptText.RepopulationSoldierMemoryPerspective);
            }
            else if (hasMassacre)
            {
                parts.Add(promptText.MassacreSoldierMemoryPerspective);
            }
            else if (hasPlunder)
            {
                parts.Add(promptText.PlunderSoldierMemoryPerspective);
            }

            if (hasRobbery)
            {
                parts.Add(promptText.RobberySoldierMemoryPerspective);
            }

            if (hasMercy)
            {
                parts.Add(promptText.MercySoldierMemoryPerspective);
            }

            if (hasSoldierAppeasement)
            {
                parts.Add(promptText.SoldierAppeasementMemoryPerspective);
            }

            if (hasGather)
            {
                parts.Add(promptText.GatherSoldierMemoryPerspective);
            }

            string header = !string.IsNullOrWhiteSpace(promptText.MemoryPerspectiveHeaderSoldier)
                ? promptText.MemoryPerspectiveHeaderSoldier
                : (isChinese ? SiegeAftermathCompatibilityTexts.Raw("compat_siege_intervention_memory_context_builder_7f9a59fed88b") : "[SOLDIER PERSPECTIVE MEMORY INTERPRETATION]");
            return BuildPerspectiveBlock(header, parts, isChinese);
        }

        if (audience == SiegeInterventionMemoryAudience.Civilian)
        {
            var parts = new List<string>();
            if (hasRepopulation)
            {
                parts.Add(promptText.RepopulationCivilianMemoryPerspective);
            }
            else if (hasMassacre)
            {
                parts.Add(promptText.MassacreCivilianMemoryPerspective);
            }
            else if (hasPlunder || hasRobbery)
            {
                parts.Add(promptText.PlunderCivilianMemoryPerspective);
            }

            if (hasMercy)
            {
                parts.Add(promptText.MercyCivilianMemoryPerspective);
            }

            if (hasGather)
            {
                parts.Add(promptText.GatherCivilianMemoryPerspective);
            }

            string header = !string.IsNullOrWhiteSpace(promptText.MemoryPerspectiveHeaderCivilian)
                ? promptText.MemoryPerspectiveHeaderCivilian
                : (isChinese ? SiegeAftermathCompatibilityTexts.Raw("compat_siege_intervention_memory_context_builder_8cf509e6e479") : "[CIVILIAN PERSPECTIVE MEMORY INTERPRETATION]");
            return BuildPerspectiveBlock(header, parts, isChinese);
        }

        if (hasMassacre || hasRepopulation || hasPlunder || hasMercy || hasGather)
        {
            return !string.IsNullOrWhiteSpace(promptText.MemoryPriorityNotice)
                ? promptText.MemoryPriorityNotice
                : (isChinese
                    ? SiegeAftermathCompatibilityTexts.Raw("compat_siege_intervention_memory_context_builder_521324bcb4a4")
                    : "[MEMORY PRIORITY] Mercy/relief/reassurance/oath, plunder, massacre, and colonization dispositions take priority over messenger and movement details; subsequent replies must acknowledge the current primary direction.");
        }

        return string.Empty;
    }

    private static string BuildPerspectiveBlock(string title, List<string> parts, bool isChinese)
    {
        if (parts == null || parts.Count == 0)
        {
            return string.Empty;
        }

        string sep = isChinese ? "" : " ";
        return title + sep + string.Join(sep, parts);
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(text) || tokens == null)
        {
            return false;
        }

        foreach (string token in tokens)
        {
            if (!string.IsNullOrWhiteSpace(token) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsChineseCatalog(TownPromptTextCatalog catalog)
    {
        if (!string.IsNullOrEmpty(catalog?.Language)) return catalog.Language == "zh-CN";
        string title = catalog?.SceneSectionTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }
        foreach (char c in title)
        {
            if (c >= CjkRangeStart && c <= CjkRangeEnd)
            {
                return true;
            }
        }
        return false;
    }
}
