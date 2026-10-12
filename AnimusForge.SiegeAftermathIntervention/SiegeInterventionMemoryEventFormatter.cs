using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free formatter for a single GCCZ memory event.
/// TownSceneMemoryStore owns sequencing, duplicate suppression, and trimming.
/// </summary>
public static class SiegeInterventionMemoryEventFormatter
{
    private static readonly Regex SequencePrefixRegex = new Regex("^\\s*\\d+\\s*[\\.．、:]\\s*", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);

    public static string FormatEntry(string kind, string detail)
        => FormatEntry(kind, detail, false);

    public static string FormatEntry(string kind, string detail, bool isEnglish)
    {
        string fallbackKind = isEnglish ? "Disposition" : SiegeAftermathCompatibilityTexts.Raw("memory_event_fallback_kind");
        string separator = isEnglish ? ": " : "：";
        string normalizedKind = string.IsNullOrWhiteSpace(kind) ? fallbackKind : kind.Trim();
        string normalizedDetail = string.IsNullOrWhiteSpace(detail) ? normalizedKind : detail.Trim();
        normalizedDetail = SiegeActionTagCatalog.StripRecognizedTags(normalizedDetail);
        normalizedDetail = WhitespaceRegex.Replace(normalizedDetail.Replace("\r", " ").Replace("\n", " "), " ").Trim();
        return normalizedKind + separator + normalizedDetail;
    }

    public static string StripSequencePrefix(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return string.Empty;
        }

        return SequencePrefixRegex.Replace(entry.Trim(), string.Empty).Trim();
    }
}
