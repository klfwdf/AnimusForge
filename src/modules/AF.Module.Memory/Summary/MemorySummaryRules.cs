using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Runtime;

// Pure rules over already captured values. Settings and game-derived names are read
// by the main-thread caller, never by this owner or by the HTTP continuation.
internal static class MemorySummaryRules
{
    internal static string WritingRequirements(string requirements)
    {
        string text = (requirements ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : MemoryTextResources.Prompt("AF_MEM_Prompt_WritingRequirements_Header") + text + "\n";
    }

    internal static string DailySystemPrompt(int sourceChars, int denominator, string playerHistoryName, string requirements)
    {
        int targetChars = Math.Max(80, sourceChars / Math.Max(1, denominator));
        return PromptComposerTexts.ApplyOutputLanguage(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_Role")
            + WritingRequirements(requirements)
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_FixedRules")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_TitleRule")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_SummaryDateRule")
            + MemoryTextResources.Prompt("MEM_Prompt_089")
            + MemoryTextResources.Prompt("MEM_Prompt_090")
            + MemoryTextResources.Prompt("MEM_Prompt_091")
            + MemoryTextResources.Prompt("MEM_Prompt_092")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_PublicityRule")
            + MemoryTextResources.Prompt("MEM_Prompt_093")
            + MemoryTextResources.Prompt("MEM_Prompt_094")
            + MemoryTextResources.Prompt("MEM_Prompt_095") + playerHistoryName + MemoryTextResources.Prompt("MEM_Prompt_096")
            + MemoryTextResources.Prompt("MEM_Prompt_097") + targetChars + MemoryTextResources.Prompt("MEM_Prompt_098")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_System_AfefRule"));
    }

    internal static string MajorSystemPrompt(int targetChars, string requirements)
    {
        int clampedTarget = Math.Max(180, Math.Min(700, targetChars));
        return PromptComposerTexts.ApplyOutputLanguage(MemoryTextResources.Prompt("AF_MEM_Prompt_Major_System_Role")
            + WritingRequirements(requirements)
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Major_System_FixedRules")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Major_System_MergeRule")
            + MemoryTextResources.Prompt("MEM_Prompt_099") + clampedTarget + MemoryTextResources.Prompt("MEM_Prompt_100")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Major_System_PreserveRule")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Major_System_NoFabricationRule"));
    }

    internal static string OverviewSystemPrompt(int targetChars, string requirements)
    {
        int clampedTarget = Math.Max(100, Math.Min(1000, targetChars));
        return PromptComposerTexts.ApplyOutputLanguage(MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_System_Role")
            + WritingRequirements(requirements)
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_System_FixedRules")
            + MemoryTextResources.Prompt("MEM_Prompt_099") + clampedTarget + MemoryTextResources.Prompt("MEM_Prompt_101")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_System_MergeRule")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_System_PreserveRule")
            + MemoryTextResources.Prompt("MEM_Prompt_102")
            + MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_System_NoFabricationRule"));
    }

    internal static string DailyUserPrompt(string date, string hours, string heroName, int trust,
        IReadOnlyList<string> scenes, string playerDisplayName, string playerHistoryName,
        IEnumerable<string> normalLines, IEnumerable<string> afefLines, bool hasAfef)
    {
        string scene = scenes != null && scenes.Count > 0 ? string.Join(" / ", scenes) : MemoryTextResources.Prompt("MEM_Prompt_103");
        var text = new StringBuilder();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_MetadataHeader"));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_104") + date);
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_105") + hours);
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_Npc").Replace("{HERO_NAME}", heroName ?? ""));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_106") + trust + MemoryTextResources.Prompt("MEM_Prompt_107"));
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_ContentHeader"));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_108") + (scenes != null && scenes.Count > 0 ? scene : MemoryTextResources.Prompt("MEM_Prompt_109")));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_110") + date + " " + hours);
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_111") + date + MemoryTextResources.Prompt("MEM_Prompt_112") + hours + MemoryTextResources.Prompt("MEM_Prompt_113") + scene + MemoryTextResources.Prompt("MEM_Prompt_114"));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_115") + playerDisplayName + MemoryTextResources.Prompt("MEM_Prompt_116") + playerDisplayName + MemoryTextResources.Prompt("MEM_Prompt_117"));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_118") + playerHistoryName + MemoryTextResources.Prompt("MEM_Prompt_119"));
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_HistoryHeader"));
        foreach (string line in normalLines ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(line)) text.AppendLine(line);
        if (hasAfef)
        {
            text.AppendLine();
            text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_AfefHeader"));
            foreach (string line in afefLines ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(line)) text.AppendLine(line);
        }
        return text.ToString().Trim();
    }

    internal static string MajorUserPrompt(string heroName, bool hasExistingSummary, string existingSummary,
        IEnumerable<string> sourceLines, int targetChars)
    {
        var text = new StringBuilder();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_Npc").Replace("{HERO_NAME}", heroName ?? ""));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_120") + Math.Max(180, Math.Min(700, targetChars)) + MemoryTextResources.Prompt("MEM_Prompt_121"));
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Major_User_ExistingHeader"));
        text.AppendLine(hasExistingSummary ? existingSummary
            : MemoryTextResources.Prompt("AF_MEM_Prompt_Major_User_NoExisting"));
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Major_User_NewActionsHeader"));
        foreach (string line in sourceLines ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(line)) text.AppendLine("- " + line);
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Major_User_OutputInstruction"));
        return text.ToString().Trim();
    }

    internal static string OverviewUserPrompt(string heroName, bool hasExistingSummary, string existingSummary,
        IEnumerable<string> blockTexts, int targetChars)
    {
        var text = new StringBuilder();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Daily_User_Npc").Replace("{HERO_NAME}", heroName ?? ""));
        text.AppendLine(MemoryTextResources.Prompt("MEM_Prompt_120") + Math.Max(100, Math.Min(1000, targetChars)) + MemoryTextResources.Prompt("MEM_Prompt_121"));
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_User_Explanation"));
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_User_ExistingHeader"));
        text.AppendLine(hasExistingSummary ? existingSummary
            : MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_User_NoExisting"));
        text.AppendLine();
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_User_BlocksHeader"));
        foreach (string block in blockTexts ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(block)) { text.AppendLine(block); text.AppendLine(); }
        text.AppendLine(MemoryTextResources.Prompt("AF_MEM_Prompt_Overview_User_OutputInstruction"));
        return text.ToString().Trim();
    }

    internal static bool TryReadSummary(JObject obj, out string summary, out string error)
    {
        summary = Read(obj, "summary_content", "summaryContent", "summary", "content").Trim();
        error = string.IsNullOrWhiteSpace(summary) ? MemoryTextResources.Raw("MEM_Raw_116") : "";
        return error.Length == 0;
    }

    internal static bool TryReadDaily(JObject obj, Func<string, string> stripTitle,
        Func<string, string> normalizePublicity, Func<string, string> renderHistory,
        out DailyFields fields, out string error)
    {
        fields = null;
        string title = stripTitle(Read(obj, "rich_title", "richTitle", "title"));
        string summary = Read(obj, "summary_content", "summaryContent", "summary", "content").Trim();
        string publicity = normalizePublicity(Read(obj, "player_publicity", "playerPublicity", "publicity"));
        string history = Read(obj, "player_history_material", "playerHistoryMaterial", "history_material", "historyMaterial").Trim();
        if ((!string.Equals(publicity, "public", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(publicity, "leaked_public", StringComparison.OrdinalIgnoreCase)) || IsEmptyMarker(history)) history = "";
        if (history.Length > 0) history = renderHistory(history);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(summary))
        { error = MemoryTextResources.Raw("MEM_Raw_117"); return false; }
        fields = new DailyFields(title, summary, publicity, history,
            Read(obj, "publicity_reason", "publicityReason", "reason").Trim());
        error = "";
        return true;
    }

    internal static bool IsEmptyMarker(string text)
    {
        string value = (text ?? "").Trim();
        return value.Length == 0 || string.Equals(value, MemoryTextResources.Raw("MEM_Raw_123"), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(JObject obj, params string[] names)
    {
        if (obj == null) return "";
        foreach (string name in names)
            foreach (JProperty property in obj.Properties())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value?.ToString() ?? "";
        return "";
    }

    internal sealed class DailyFields
    {
        internal readonly string Title, Summary, Publicity, History, Reason;
        internal DailyFields(string title, string summary, string publicity, string history, string reason)
        { Title = title; Summary = summary; Publicity = publicity; History = history; Reason = reason; }
    }
}
