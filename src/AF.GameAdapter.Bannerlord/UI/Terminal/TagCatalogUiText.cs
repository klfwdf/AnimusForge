using System;
using System.Collections.Generic;
using System.Text;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Localization;

namespace AnimusForge;

// Resolve at list/detail/export boundaries; raw snapshot values remain unchanged.
internal static class TagCatalogUiText
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static string RenderLiteral(TextObject text, params (string Name, object Value)[] values)
    {
        for (int i = 0; i < values.Length; i++)
            if (values[i].Value is TextObject nested)
                text.SetTextVariable(values[i].Name, nested);
            else
                text.SetTextVariable(values[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => values[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }

    private static readonly HashSet<string> VillageActions = CreateVillageActions();

    private static HashSet<string> CreateVillageActions()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var action in VillageAftermathActionTagCatalog.GetCanonicalOrder()) names.Add(action.ToString());
        return names;
    }
    private static string Text(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    private static string Format(string id, string fallback, string name, string value)
    {
        return RenderLiteral(new TextObject("{=" + id + "}" + fallback), (name, value ?? string.Empty));
    }

    internal static string Category(string raw)
    {
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryBody")) return Text("AF_UI_TagCatalog_CategoryBody", "Body text");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryHistory")) return Text("AF_UI_TagCatalog_CategoryHistory", "Body/history");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryFacts")) return Text("AF_UI_TagCatalog_CategoryFacts", "Body/facts");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryPostprocess")) return Text("AF_UI_TagCatalog_CategoryPostprocess", "Postprocessing");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryRules")) return Text("AF_UI_TagCatalog_CategoryRules", "Postprocessing/rule table");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryRelay")) return Text("AF_UI_TagCatalog_CategoryRelay", "Postprocessing/relay");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryGccz")) return Text("AF_UI_TagCatalog_CategoryGccz", "Postprocessing/GCCZ");
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryVillage")) return Text("AF_UI_TagCatalog_CategoryVillage", "Postprocessing/GCCZ villages");
        if (raw == null || raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_CategoryTag")) return Text("AF_UI_Terminal_Tag", "Tag");
        return raw;
    }

    internal static string Tag(string raw)
        => raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_NaturalLanguage") ? Text("AF_UI_TagCatalog_NaturalLanguage", "Body text (natural language)") : raw ?? string.Empty;

    internal static string Description(string raw)
    {
        string description = (raw ?? string.Empty).Trim();
        switch (description)
        {
            case "Use when an eligible speaker on either side directly acknowledges the player's current mercy decision. Soldier complaints do not cancel a valid player order.":
                return Text("AF_UI_GcczRule_Mercy", "Use when an eligible speaker on either side directly acknowledges the player's current mercy decision. Soldier complaints do not cancel a valid player order.");
            case "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current relief, protection, discipline, food, gold, or supply arrangement. Other roles may only suggest it.":
                return Text("AF_UI_GcczRule_Relief", "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current relief, protection, discipline, food, gold, or supply arrangement. Other roles may only suggest it.");
            case "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public reassurance, speech, stabilization, or cooperation plan. Other roles may only suggest it.":
                return Text("AF_UI_GcczRule_Inspire", "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public reassurance, speech, stabilization, or cooperation plan. Other roles may only suggest it.");
            case "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public oath or civilian allegiance arrangement. Other roles may only suggest it.":
                return Text("AF_UI_GcczRule_RallyOath", "Use only when a settlement notable/headman or ordinary civilian directly accepts the player's current public oath or civilian allegiance arrangement. Other roles may only suggest it.");
            case "Use only when appeasement is pending and an allied ordinary soldier directly accepts the player's current compensation, discipline explanation, or loot arrangement.":
                return Text("AF_UI_GcczRule_SoldierAppeasement", "Use only when appeasement is pending and an allied ordinary soldier directly accepts the player's current compensation, discipline explanation, or loot arrangement.");
            case "Use when an eligible responder directly accepts, relays, or executes the player's current order to gather hidden residents. Decide from full semantics, never fixed words.":
                return Text("AF_UI_GcczRule_GatherCivilians", "Use when an eligible responder directly accepts, relays, or executes the player's current order to gather hidden residents. Decide from full semantics, never fixed words.");
            case "Use only when a defeated civilian, merchant, artisan, headman, or notable directly responds to the player's current demand for gold or goods. This is partial robbery, not full-town plunder.":
                return Text("AF_UI_GcczRule_CivilianRobbery", "Use only when a defeated civilian, merchant, artisan, headman, or notable directly responds to the player's current demand for gold or goods. This is partial robbery, not full-town plunder.");
            case "Use only when an allied ordinary soldier directly receives the player's current full-town plunder order. The soldier may complain but cannot refuse, delay, redirect, or replace a valid order.":
                return Text("AF_UI_GcczRule_Plunder", "Use only when an allied ordinary soldier directly receives the player's current full-town plunder order. The soldier may complain but cannot refuse, delay, redirect, or replace a valid order.");
            case "Use only when an allied ordinary soldier directly receives the player's current massacre order. Negative wording does not cancel a valid order; a later eligible order may stop before all captured targets die.":
                return Text("AF_UI_GcczRule_Massacre", "Use only when an allied ordinary soldier directly receives the player's current massacre order. Negative wording does not cancel a valid order; a later eligible order may stop before all captured targets die.");
            case "Use only when an allied ordinary soldier directly receives the player's current order to kill the original residents and repopulate with the player's culture. Suggestions and NPC-to-NPC speech never execute it.":
                return Text("AF_UI_GcczRule_CulturalRepopulation", "Use only when an allied ordinary soldier directly receives the player's current order to kill the original residents and repopulate with the player's culture. Suggestions and NPC-to-NPC speech never execute it.");
            case "Use only when an allied ordinary soldier directly receives the player's current order to stop an active incomplete massacre. Existing deaths, loot, and consequences remain.":
                return Text("AF_UI_GcczRule_StopMassacre", "Use only when an allied ordinary soldier directly receives the player's current order to stop an active incomplete massacre. Existing deaths, loot, and consequences remain.");
            case "Use only when an authorized responder directly accepts the player's current non-destructive town culture decree. It changes only town culture and never means killing, expulsion, repopulation, or colonization.":
                return Text("AF_UI_GcczRule_ConstructiveCultureChange", "Use only when an authorized responder directly accepts the player's current non-destructive town culture decree. It changes only town culture and never means killing, expulsion, repopulation, or colonization.");
        }
        if (description == AdapterPromptCaptureTexts.PcapCompat("TagCat_DescBody"))
            return Text("AF_UI_TagCatalog_DescriptionBody", "This is not an action tag. Text other than action tags in the tag-test input is displayed as NPC-visible body text or written to history.");
        if (description == AdapterPromptCaptureTexts.PcapCompat("TagCat_DescContent"))
            return Text("AF_UI_TagCatalog_DescriptionContent", "Internal body-text delimiter. Players normally do not enter this as an action tag.");
        if (description == AdapterPromptCaptureTexts.PcapCompat("TagCat_DescPlayerFact"))
            return Text("AF_UI_TagCatalog_DescriptionPlayerFact", "Player-action fact marker informing the LLM of player actions that have actually occurred.");
        if (description == AdapterPromptCaptureTexts.PcapCompat("TagCat_DescNpcFact"))
            return Text("AF_UI_TagCatalog_DescriptionNpcFact", "NPC-action fact marker informing the LLM of NPC actions that have actually occurred.");
        string villagePrefix = AdapterPromptCaptureTexts.PcapCompat("TagCat_VillagePrefix");
        if (description.StartsWith(villagePrefix, StringComparison.Ordinal))
        {
            string action = description.Substring(villagePrefix.Length);
            if (VillageActions.Contains(action))
                return Format("AF_UI_TagCatalog_DescriptionVillage", "Triggered only by an explicit player command in an authorized ordinary village scene: {ACTION}", "ACTION", VillageActionDisplay(action));
        }
        return description;
    }

    private static string VillageActionDisplay(string action)
    {
        switch (action)
        {
            case "GatherElders": return Text("AF_UI_TagCatalog_VillageActionGatherElders", "Gather elders");
            case "RestrainTroops": return Text("AF_UI_TagCatalog_VillageActionRestrainTroops", "Enforce troop discipline");
            case "Pacify": return Text("AF_UI_TagCatalog_VillageActionPacify", "Pacify the village");
            case "Relief": return Text("AF_UI_TagCatalog_VillageActionRelief", "Provide relief");
            case "Fine": return Text("AF_UI_TagCatalog_VillageActionFine", "Levy fines");
            case "RequisitionFood": return Text("AF_UI_TagCatalog_VillageActionRequisitionFood", "Requisition food");
            case "RequisitionProduce": return Text("AF_UI_TagCatalog_VillageActionRequisitionProduce", "Requisition produce");
            case "RequisitionLivestock": return Text("AF_UI_TagCatalog_VillageActionRequisitionLivestock", "Requisition livestock");
            case "LevyRecruits": return Text("AF_UI_TagCatalog_VillageActionLevyRecruits", "Levy recruits");
            case "PunishRingleader": return Text("AF_UI_TagCatalog_VillageActionPunishRingleader", "Punish the ringleader");
            case "ConfiscateProperty": return Text("AF_UI_TagCatalog_VillageActionConfiscateProperty", "Confiscate village property");
            case "DestroyLivelihood": return Text("AF_UI_TagCatalog_VillageActionDestroyLivelihood", "Destroy livelihoods");
            case "Massacre": return Text("AF_UI_TagCatalog_VillageActionMassacre", "Massacre");
            case "CulturalReform": return Text("AF_UI_TagCatalog_VillageActionCulturalReform", "Enforce cultural reform");
            default: return action;
        }
    }

    internal static string Source(string raw)
    {
        if (raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_BuiltInSource")) return Text("AF_UI_TagCatalog_SourceBuiltIn", "Built-in documentation");
        string prefix = AdapterPromptCaptureTexts.PcapCompat("TagCat_AssemblyPrefix");
        return raw != null && raw.StartsWith(prefix, StringComparison.Ordinal)
            ? Format("AF_UI_TagCatalog_SourceAssembly", "Assembly: {SOURCE}", "SOURCE", raw.Substring(prefix.Length))
            : raw ?? string.Empty;
    }

    internal static string SearchTerms(AnimusForgeTagCatalogEntry entry)
    {
        // Retain original aliases and include full translated text beyond the shortened hint.
        var text = new StringBuilder(entry.Description ?? string.Empty);
        text.Append(' ').Append(string.Join(" ", entry.Sources));
        text.Append(' ').Append(entry.Tag).Append(' ').Append(entry.Category);
        text.Append(' ').Append(Description(entry.Description));
        foreach (string source in entry.Sources) text.Append(' ').Append(Source(source));
        return text.ToString();
    }

    internal static string ExportEmpty => Text("AF_UI_TagCatalog_ExportEmpty", "There are no tags to export.");
    internal static string ExportRootMissing => Text("AF_UI_TagCatalog_ExportRootMissing", "No writable AnimusForge module directory was found.");
    internal static string ExportNameConflict => Text("AF_UI_TagCatalog_ExportNameConflict", "Too many export filename conflicts. Try again later.");
    internal static string ExportTitle => Text("AF_UI_TagCatalog_ExportTitle", "AnimusForge tag catalog");
    internal static string ExportTime(string value) => Format("AF_UI_TagCatalog_ExportTime", "Exported at: {VALUE}", "VALUE", value);
    internal static string ExportRoot(string value) => Format("AF_UI_TagCatalog_ExportRoot", "Module directory: {VALUE}", "VALUE", value);
    internal static string ExportTotal(string value) => Format("AF_UI_TagCatalog_ExportTotal", "Total tags: {VALUE}", "VALUE", value);
    internal static string ExportBody(string value) => Format("AF_UI_TagCatalog_ExportBody", "Body/history: {VALUE}", "VALUE", value);
    internal static string ExportPostprocess(string value) => Format("AF_UI_TagCatalog_ExportPostprocess", "Postprocessing: {VALUE}", "VALUE", value);
    internal static string ExportFiles(string value) => Format("AF_UI_TagCatalog_ExportFiles", "Scanned files: {VALUE}", "VALUE", value);
    internal static string ExportSources => Text("AF_UI_TagCatalog_ExportSources", "Scan sources:");
    internal static string ExportExplanation => Text("AF_UI_TagCatalog_ExportExplanation", "This file was exported from the in-game AnimusForge terminal (U key). The catalog is extracted from current module files, current assembly strings, and built-in runtime rules.");
    internal static string DescriptionLabel => Text("AF_UI_Terminal_DescriptionLabel", "Description:");
    internal static string SourcesLabel => Text("AF_UI_Terminal_SourcesLabel", "Sources:");
}
