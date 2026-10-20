using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// Audits the detached messages immediately before the primary provider call.
/// It never rewrites history, player input, names or unknown legacy facts.
/// </summary>
internal static class PromptLanguageAudit
{
    private static readonly Regex Han = new Regex(@"[\p{IsCJKUnifiedIdeographsExtensionA}\p{IsCJKUnifiedIdeographs}\p{IsCJKCompatibilityIdeographs}]", RegexOptions.Compiled);
    private static readonly Regex StableProtocol = new Regex(
        @"(?:\[AFEF (?:NPC_ACTION|PLAYER_ACTION)\]|\[(?:ACTION|A|AD|ADP|ASS|GUI|ATT|ATP|RELAY|FOL|STP|END):[^\]]*\]|\[\[(?:AFI_PROTOCOL_[A-Z_]+)\]\])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static PromptLanguageAuditResult Inspect(IEnumerable<object> messages)
    {
        PromptLanguageAuditResult result = new PromptLanguageAuditResult();
        foreach (object message in messages ?? Array.Empty<object>())
        {
            if (!PrimaryChatMessagePolicy.TryReadMessage(message, out string role, out string content)) continue;
            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
            {
                string fixedText = StableProtocol.Replace(content ?? "", "");
                if (Han.IsMatch(fixedText)) result.UnprojectedSystemMessageCount++;
            }
            else if (Han.IsMatch(content ?? ""))
            {
                result.DynamicMessageCount++;
            }
        }
        return result;
    }

    internal static void Log(string channel, IEnumerable<object> messages)
    {
        PromptLanguageAuditResult result = Inspect(messages);
        if (result.UnprojectedSystemMessageCount == 0 && result.DynamicMessageCount == 0) return;
        Logger.Log("PromptLanguageAudit", "channel=" + (channel ?? "")
            + " unprojectedSystem=" + result.UnprojectedSystemMessageCount
            + " dynamicOriginal=" + result.DynamicMessageCount
            + " action=preserve_original_dynamic_text");
    }

    internal static void Log(string channel, JArray messages)
    {
        PromptLanguageAuditResult result = new PromptLanguageAuditResult();
        foreach (JToken token in messages ?? new JArray())
        {
            string role = token?["role"]?.ToString() ?? "";
            string content = token?["content"]?.ToString() ?? "";
            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
            {
                if (Han.IsMatch(StableProtocol.Replace(content, ""))) result.UnprojectedSystemMessageCount++;
            }
            else if (Han.IsMatch(content)) result.DynamicMessageCount++;
        }
        if (result.UnprojectedSystemMessageCount == 0 && result.DynamicMessageCount == 0) return;
        Logger.Log("PromptLanguageAudit", "channel=" + (channel ?? "")
            + " unprojectedSystem=" + result.UnprojectedSystemMessageCount
            + " dynamicOriginal=" + result.DynamicMessageCount
            + " action=preserve_original_dynamic_text");
    }

    internal static void LogPair(string channel, string system, string user)
    {
        Log(channel, new JArray
        {
            new JObject { ["role"] = "system", ["content"] = system ?? "" },
            new JObject { ["role"] = "user", ["content"] = user ?? "" }
        });
    }
}

internal sealed class PromptLanguageAuditResult
{
    internal int UnprojectedSystemMessageCount;
    internal int DynamicMessageCount;
}
