using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Stable facts, legacy diagnostics, and input cues retain their original values.
internal static class PolicyNpcCompatibilityTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("PolicyNpcCompatibility.json"), Encoding.UTF8)));

    internal static string Get(string id) => Values.Value[id];

    private static readonly string[] EnglishGovernanceTerms =
    {
            "legitimacy", "royal power", "rule", "emperor", "empress", "grand duke", "khan", "high king",
            "senate", "boyar", "noyan", "vassal", "noble", "clan", "tribe", "chieftain",
            "army", "retinue", "succession", "autonomy", "land", "tax", "policy", "law", "right",
            "interest", "support", "opposition", "tension", "controversy", "assessment", "loyalty",
            "people", "merchant", "farmer", "religion", "faith", "tradition", "renown", "prestige", "power", "politic"
    };

    private static readonly string[] EnglishGeographyTerms =
    {
            "located", "east", "west", "south", "north", "plateau", "mountain", "ridge", "river", "lake",
            "canyon", "terrain", "landform", "flows into", "originates", "bay", "climate", "forest", "steppe"
    };

    private static readonly Lazy<string[]> Governance = new Lazy<string[]>(() =>
        Get("Knowledge_GovernanceTerms").Split(',').Concat(EnglishGovernanceTerms).ToArray());
    private static readonly Lazy<string[]> Geography = new Lazy<string[]>(() =>
        Get("Knowledge_GeographyTerms").Split(',').Concat(EnglishGeographyTerms).ToArray());

    // Each semantic cue contributes at most once even when several word forms occur.
    private static readonly Lazy<Dictionary<string, string[]>> EnglishForms =
        new Lazy<Dictionary<string, string[]>>(() =>
        {
            var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (string term in EnglishGovernanceTerms.Concat(EnglishGeographyTerms))
            {
                var forms = new List<string> { term, term + "s" };
                if (term.EndsWith("y", StringComparison.Ordinal))
                    forms.Add(term.Substring(0, term.Length - 1) + "ies");
                if (term == "politic") forms.AddRange(new[] { "political", "politics" });
                if (term == "rule") forms.AddRange(new[] { "ruler", "rulers", "ruling" });
                if (term == "legitimacy") forms.Add("legitimate");
                result[term] = forms.Distinct(StringComparer.Ordinal).ToArray();
            }
            return result;
        });

    internal static bool ContainsKnowledgeCue(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term)) return false;
        if (!EnglishForms.Value.TryGetValue(term, out string[] forms))
            return text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        foreach (string form in forms)
        {
            int start = 0;
            while (start < text.Length)
            {
                int index = text.IndexOf(form, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
                int end = index + form.Length;
                if ((index == 0 || !IsAsciiLetter(text[index - 1]))
                    && (end == text.Length || !IsAsciiLetter(text[end]))) return true;
                start = index + 1;
            }
        }
        return false;
    }

    private static bool IsAsciiLetter(char value)
        => (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z');

    internal static string[] GovernanceTerms => Governance.Value;
    internal static string[] GeographyTerms => Geography.Value;
}
