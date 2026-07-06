using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Narrow test-only fixed keyword mapper for active GCCZ scenes.
/// It does not define new outcomes; it only converts explicit "GCCZ测试..." player text into
/// the existing postprocess tags so the AF bridge can exercise the same settlement side effects.
/// </summary>
public static class SiegeFixedKeywordActionProfile
{
    public const string TestPrefix = "GCCZ测试";

    public const string DiagnosticSource = "gccz_fixed_keyword_test_action";

    private static readonly SiegeFixedKeywordActionDefinition[] Definitions =
    {
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "宽恕", "[ACTION:宽恕]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "救济", "[ACTION:救济]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "宣抚", "[ACTION:宣抚]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "盟誓", "[ACTION:盟誓]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "安兵", "[ACTION:安兵]", true),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "召集", "[ACTION:召集]", true),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "抢钱", "[ACTION:抢钱]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "搜掠", "[ACTION:搜掠]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "血洗", "[ACTION:血洗]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Town, "殖民", "[ACTION:殖民]", false),

        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "优待战俘", "[ACTION:优待战俘]", true),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "索要赎金", "[ACTION:索要赎金]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "收编领主", "[ACTION:收编领主]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "收编战俘", "[ACTION:收编战俘]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "接收军械", "[ACTION:接收军械]", true),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "战俘劳役", "[ACTION:战俘劳役]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "屠戮守军", "[ACTION:屠戮守军]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "贩卖俘虏", "[ACTION:贩卖俘虏]", false),
        new SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope.Castle, "处决领主", "[ACTION:处决领主]", false),
    };

    public static IReadOnlyList<SiegeFixedKeywordActionDefinition> GetDefinitions()
    {
        return Definitions;
    }

    public static IReadOnlyList<SiegeFixedKeywordActionDefinition> GetDefinitions(SiegeFixedKeywordActionScope scope)
    {
        return Definitions.Where(definition => definition.Scope == scope).ToArray();
    }

    public static bool TryBuildTagText(string playerText, bool castleAftermath, out string tagText, out SiegeFixedKeywordActionDefinition definition)
    {
        tagText = string.Empty;
        definition = default;

        string normalized = NormalizeForMatch(playerText);
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains(NormalizeForMatch(TestPrefix)))
        {
            return false;
        }

        SiegeFixedKeywordActionScope scope = castleAftermath ? SiegeFixedKeywordActionScope.Castle : SiegeFixedKeywordActionScope.Town;
        foreach (SiegeFixedKeywordActionDefinition candidate in Definitions)
        {
            if (candidate.Scope != scope)
            {
                continue;
            }

            if (ContainsFixedKeyword(normalized, candidate))
            {
                tagText = candidate.CanonicalTag;
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    public static string BuildMatchedMemoryText(SiegeFixedKeywordActionDefinition definition, string playerText)
    {
        string scope = definition.Scope == SiegeFixedKeywordActionScope.Castle ? "城堡" : "城镇";
        return "固定词测试触发" + scope + "标签：" + definition.CanonicalTag
            + "；关键词=" + definition.Keyword
            + "；玩家原文=" + NormalizeForLog(playerText);
    }

    public static string BuildMatchedDiagnosticText(SiegeFixedKeywordActionDefinition definition)
    {
        return "source=" + DiagnosticSource
            + " scope=" + definition.Scope
            + " keyword=" + definition.Keyword
            + " tag=" + definition.CanonicalTag
            + " processOnly=" + definition.IsProcessOnly;
    }

    private static bool ContainsFixedKeyword(string normalized, SiegeFixedKeywordActionDefinition definition)
    {
        string keyword = NormalizeForMatch(definition.Keyword);
        string tag = NormalizeForMatch(definition.CanonicalTag);
        return (!string.IsNullOrWhiteSpace(keyword) && normalized.Contains(keyword))
            || (!string.IsNullOrWhiteSpace(tag) && normalized.Contains(tag));
    }

    private static string NormalizeForMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        char[] chars = text
            .Where(c => !char.IsWhiteSpace(c)
                && c != '#'
                && c != '：'
                && c != ':'
                && c != '【'
                && c != '】'
                && c != '['
                && c != ']')
            .ToArray();
        return new string(chars).Trim().ToUpperInvariant();
    }

    private static string NormalizeForLog(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}

public enum SiegeFixedKeywordActionScope
{
    Town,
    Castle,
}

public readonly struct SiegeFixedKeywordActionDefinition
{
    public SiegeFixedKeywordActionDefinition(SiegeFixedKeywordActionScope scope, string keyword, string canonicalTag, bool isProcessOnly)
    {
        Scope = scope;
        Keyword = keyword ?? string.Empty;
        CanonicalTag = canonicalTag ?? string.Empty;
        IsProcessOnly = isProcessOnly;
    }

    public SiegeFixedKeywordActionScope Scope { get; }

    public string Keyword { get; }

    public string CanonicalTag { get; }

    public bool IsProcessOnly { get; }
}
