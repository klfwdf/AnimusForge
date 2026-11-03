using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// Audits the detached messages immediately before the primary provider call.
/// It never rewrites history, player input, names or unknown legacy facts.
/// Under an English game it also records a short sample around the first Chinese run in a system
/// message, so fixed text that still leaks can be located from the log. Runs once per request;
/// each distinct sample is logged once per session (bounded set).
/// </summary>
internal static class PromptLanguageAudit
{
    private const int SampleRadius = 12;
    private const int MaxDistinctSamples = 256;
    private static readonly HashSet<string> LoggedSamples = new HashSet<string>(StringComparer.Ordinal);
    private static readonly object SampleLock = new object();

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
                Match han = Han.Match(fixedText);
                if (han.Success)
                {
                    result.UnprojectedSystemMessageCount++;
                    if (result.FirstSystemSample == null) result.FirstSystemSample = Sample(fixedText, han.Index);
                }
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
        Write(channel, Inspect(messages));
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
                string fixedText = StableProtocol.Replace(content, "");
                Match han = Han.Match(fixedText);
                if (han.Success)
                {
                    result.UnprojectedSystemMessageCount++;
                    if (result.FirstSystemSample == null) result.FirstSystemSample = Sample(fixedText, han.Index);
                }
            }
            else if (Han.IsMatch(content)) result.DynamicMessageCount++;
        }
        Write(channel, result);
    }

    private static void Write(string channel, PromptLanguageAuditResult result)
    {
        if (result.UnprojectedSystemMessageCount == 0 && result.DynamicMessageCount == 0) return;
        bool english = !PromptLanguage.IsChinese;
        string line = "channel=" + (channel ?? "")
            + " language=" + (english ? "en" : "zh")
            + " unprojectedSystem=" + result.UnprojectedSystemMessageCount
            + " dynamicOriginal=" + result.DynamicMessageCount
            + " action=preserve_original_dynamic_text";
        // A Chinese system fragment under an English game is a fixed-text leak worth locating.
        if (english && result.FirstSystemSample != null && ShouldLogSample(result.FirstSystemSample))
            line += " firstSystemSample=\"" + result.FirstSystemSample + "\"";
        Logger.Log("PromptLanguageAudit", line);
    }

    internal static string Sample(string text, int index)
    {
        int start = Math.Max(0, index - SampleRadius);
        int end = Math.Min(text.Length, index + SampleRadius * 2);
        return text.Substring(start, end - start).Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'');
    }

    private static bool ShouldLogSample(string sample)
    {
        lock (SampleLock)
        {
            if (LoggedSamples.Count >= MaxDistinctSamples) return false;
            return LoggedSamples.Add(sample);
        }
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
    internal string FirstSystemSample;
}
