using System;
using System.Collections.Generic;
using System.Text;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Localization;

namespace AnimusForge;

// Resolve at list/detail/export boundaries; raw snapshot values remain unchanged.
internal static class TagCatalogUiText
{
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
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable(name, value ?? string.Empty);
        return text.ToString();
    }

    internal static string Category(string raw)
    {
        switch (raw)
        {
            case "正文": return Text("AF_UI_TagCatalog_CategoryBody", "Body text");
            case "正文/历史": return Text("AF_UI_TagCatalog_CategoryHistory", "Body/history");
            case "正文/事实": return Text("AF_UI_TagCatalog_CategoryFacts", "Body/facts");
            case "后处理": return Text("AF_UI_TagCatalog_CategoryPostprocess", "Postprocessing");
            case "后处理/规则表": return Text("AF_UI_TagCatalog_CategoryRules", "Postprocessing/rule table");
            case "后处理/接力": return Text("AF_UI_TagCatalog_CategoryRelay", "Postprocessing/relay");
            case "后处理/GCCZ": return Text("AF_UI_TagCatalog_CategoryGccz", "Postprocessing/GCCZ");
            case "后处理/GCCZ村庄": return Text("AF_UI_TagCatalog_CategoryVillage", "Postprocessing/GCCZ villages");
            case null:
            case "标签": return Text("AF_UI_Terminal_Tag", "Tag");
            default: return raw;
        }
    }

    internal static string Tag(string raw)
        => raw == "正文（自然语言）" ? Text("AF_UI_TagCatalog_NaturalLanguage", "Body text (natural language)") : raw ?? string.Empty;

    internal static string Description(string raw)
    {
        string description = (raw ?? string.Empty).Trim();
        switch (description)
        {
            case "不是动作标签。标签测试输入框里除动作标签外的文本会作为 NPC 可见正文显示或写入历史。":
                return Text("AF_UI_TagCatalog_DescriptionBody", "This is not an action tag. Text other than action tags in the tag-test input is displayed as NPC-visible body text or written to history.");
            case "内部正文分隔标记。通常不是玩家手写动作标签。":
                return Text("AF_UI_TagCatalog_DescriptionContent", "Internal body-text delimiter. Players normally do not enter this as an action tag.");
            case "玩家行为事实写入标记，用于告诉 LLM 已实际发生的玩家行为。":
                return Text("AF_UI_TagCatalog_DescriptionPlayerFact", "Player-action fact marker informing the LLM of player actions that have actually occurred.");
            case "NPC行为事实写入标记，用于告诉 LLM 已实际发生的 NPC 行为。":
                return Text("AF_UI_TagCatalog_DescriptionNpcFact", "NPC-action fact marker informing the LLM of NPC actions that have actually occurred.");
        }
        const string villagePrefix = "仅在有权限的普通村庄场景中，由玩家明确命令触发：";
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
        if (raw == "内置说明") return Text("AF_UI_TagCatalog_SourceBuiltIn", "Built-in documentation");
        const string prefix = "程序集:";
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
