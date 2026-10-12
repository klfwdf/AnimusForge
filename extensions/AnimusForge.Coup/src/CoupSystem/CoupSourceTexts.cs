using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;

namespace AnimusForge.CoupSystem;

// Stable facts stored under existing receipt/history keys, independent of UI and prompt language.
internal static class CoupSourceTexts
{
    private static readonly string[] LanguagePrefixes = { "", "LegacyEnglish_" };
    private static readonly string[] ProcessKeys = { "ReportStreet", "ReportGate", "ReportHall", "ReportSubdued" };
    private static readonly string[] ResultKeys = { "ReportSuccessCaptured", "ReportSuccessReleased", "ReportFailureWar", "ReportFailureNoWar" };
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("CoupSourceTexts.json"), Encoding.UTF8)));

    internal static string Get(string key) => Values.Value[key];
    internal static string Format(string key, params object[] values) => string.Format(Get(key), values);

    private static readonly Lazy<Dictionary<string, ReportTemplate>> ReportTemplates = new Lazy<Dictionary<string, ReportTemplate>>(() =>
    {
        var result = new Dictionary<string, ReportTemplate>(StringComparer.Ordinal);
        foreach (string prefix in LanguagePrefixes)
            foreach (string key in new[] { "ReportIntro", "ReportNoEntry", "ReportStreet", "ReportGate", "ReportHall", "ReportSubdued",
                "ReportSuccessCaptured", "ReportSuccessReleased", "ReportFailureWar", "ReportFailureNoWar", "ReportCasualties", "BulletinTimeLabel" })
                result[prefix + key] = new ReportTemplate(Get(prefix + key), key);
        return result;
    });

    // Read-only projection of the mod's known four-line report grammar. Never rewrite receipts or unknown history.
    internal static string ReportForPrompt(string source)
    {
        if (string.IsNullOrEmpty(source)) return source;
        string[] lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length != 4) return source;
        foreach (string prefix in LanguagePrefixes)
        {
            if (!TryProject(lines[0], prefix, "ReportIntro", out string intro)
                || !TryProject(lines[3], prefix, "ReportCasualties", out string casualties)) continue;
            string processLabel = Get(prefix + "ReportProcessLabel"), resultLabel = Get(prefix + "ReportResultLabel");
            if (!lines[1].StartsWith(processLabel, StringComparison.Ordinal) || !lines[2].StartsWith(resultLabel, StringComparison.Ordinal)) continue;
            string process = lines[1].Substring(processLabel.Length), result = lines[2].Substring(resultLabel.Length);
            if (!TryProject(process, prefix, "ReportNoEntry", out string projectedProcess))
            {
                string end = Get(prefix + "ReportSentenceEnd");
                if (!process.EndsWith(end, StringComparison.Ordinal)) continue;
                var parts = process.Substring(0, process.Length - end.Length).Split(new[] { Get(prefix + "ReportListSeparator") }, StringSplitOptions.None);
                var projected = new List<string>();
                foreach (string part in parts)
                {
                    if (TryProjectAny(part, prefix, ProcessKeys, out string value)) projected.Add(value);
                    else break;
                }
                if (projected.Count != parts.Length) continue;
                projectedProcess = string.Join(CoupPromptTexts.Get("ReportListSeparator"), projected) + CoupPromptTexts.Get("ReportSentenceEnd");
            }
            if (!TryProjectAny(result, prefix, ResultKeys, out string projectedResult)) continue;
            return intro + "\n" + CoupPromptTexts.Get("ReportProcessLabel") + projectedProcess + "\n"
                + CoupPromptTexts.Get("ReportResultLabel") + projectedResult + "\n" + casualties;
        }
        return source;
    }

    internal static string TimeForPrompt(string source)
    {
        foreach (string prefix in LanguagePrefixes)
            if (TryProject(source ?? "", prefix, "BulletinTimeLabel", out string result)) return result;
        return source;
    }

    private static bool TryProjectAny(string source, string prefix, string[] keys, out string result)
    {
        foreach (string key in keys) if (TryProject(source, prefix, key, out result)) return true;
        result = null; return false;
    }

    private static bool TryProject(string source, string prefix, string key, out string result)
    {
        Match match = ReportTemplates.Value[prefix + key].Pattern.Match(source);
        if (!match.Success) { result = null; return false; }
        var values = new object[ReportTemplates.Value[prefix + key].ArgumentCount];
        for (int i = 0; i < values.Length; i++) values[i] = match.Groups["v" + i].Value;
        result = string.Format(CultureInfo.InvariantCulture, CoupPromptTexts.Get(key), values);
        return true;
    }

    private sealed class ReportTemplate
    {
        internal readonly Regex Pattern;
        internal readonly int ArgumentCount;
        internal ReportTemplate(string source, string key)
        {
            var expression = new StringBuilder("\\A");
            var seen = new HashSet<int>(); int cursor = 0, maximum = -1;
            foreach (Match placeholder in Regex.Matches(source, @"\{([0-9]+)\}"))
            {
                expression.Append(Regex.Escape(source.Substring(cursor, placeholder.Index - cursor)));
                int index = int.Parse(placeholder.Groups[1].Value, CultureInfo.InvariantCulture);
                string valuePattern = key == "ReportHall" || key == "ReportCasualties" || key == "ReportIntro" && index == 4 ? "[0-9]+" : ".*?";
                expression.Append(seen.Add(index) ? "(?<v" + index + ">" + valuePattern + ")" : "\\k<v" + index + ">");
                maximum = Math.Max(maximum, index); cursor = placeholder.Index + placeholder.Length;
            }
            expression.Append(Regex.Escape(source.Substring(cursor))).Append("\\z");
            Pattern = new Regex(expression.ToString(), RegexOptions.CultureInvariant | RegexOptions.Compiled);
            ArgumentCount = maximum + 1;
        }
    }
}
