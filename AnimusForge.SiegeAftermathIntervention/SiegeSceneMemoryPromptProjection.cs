using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

// Projects only approved complete scene-event frames at the request boundary.
// The bounded scene store, event sequencing and canonical strings are untouched.
public static class SiegeSceneMemoryPromptProjection
{
    private const string FramePrefix = "SceneMemoryFrame.";
    private const string ValuePrefix = "SceneMemoryValue.";
    private static readonly Regex Slots = new Regex(@"\{([a-zA-Z_][a-zA-Z_0-9]*)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Sequence = new Regex(@"^\s*\d+\s*[\.．、:]\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private sealed class Rule
    {
        internal readonly string[] Parts, Names;
        internal readonly string Target;
        internal readonly string Kind;
        internal Rule(string source, string target, string kind)
        {
            Kind = kind;
            Names = Slots.Matches(source).Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            Parts = Slots.Split(source).Where((_, i) => i % 2 == 0).ToArray();
            Target = target;
        }
        internal bool TryRender(string source, bool projectValues, out string target, int depth = 0)
        {
            target = null;
            if (!source.StartsWith(Parts[0], StringComparison.Ordinal) || !source.EndsWith(Parts[Parts.Length - 1], StringComparison.Ordinal)) return false;
            if (Names.Length == 0) { if (source != Parts[0]) return false; target = Target; return true; }
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            int at = Parts[0].Length, limit = source.Length - Parts[Parts.Length - 1].Length;
            for (int i = 0; i < Names.Length; i++)
            {
                int end = i == Names.Length - 1 ? limit : source.IndexOf(Parts[i + 1], at, StringComparison.Ordinal);
                if (end < at || end > limit || (i < Names.Length - 1 && Parts[i + 1].Length == 0)) return false;
                string value = source.Substring(at, end - at);
                if ((Names[i].EndsWith("Count", StringComparison.Ordinal) || Names[i] == "count" || Names[i] == "value")
                    && (value.Length == 0 || value.Any(c => c < '0' || c > '9'))) return false;
                if (Names[i] == "id" && (value.Length == 0 || value.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')))) return false;
                if (values.TryGetValue(Names[i], out string prior) && prior != value) return false;
                values[Names[i]] = value;
                at = end + Parts[i + 1].Length;
            }
            if (at != source.Length) return false;
            if (projectValues)
                foreach (string key in values.Keys.ToArray())
                    if (key == "action" || key == "branch" || key == "plan" || key == "status" || key == "pool" || key == "notes" || key == "tail" || key == "refund")
                        values[key] = key == "plan" || key == "notes" || key == "refund" ? ProjectList(values[key], key == "notes" ? "note" : key, depth + 1) : ProjectValues(values[key], key, depth + 1);
            target = Slots.Replace(Target, m => values.TryGetValue(m.Groups[1].Value, out string value) ? value : m.Value);
            return true;
        }
    }

    private static readonly Lazy<Rule[]> Frames = new Lazy<Rule[]>(() => Load(FramePrefix), true);
    private static readonly Lazy<Rule[]> Facts = new Lazy<Rule[]>(() => Load("SceneFactFrame."), true);
    private static readonly Lazy<Rule[]> Values = new Lazy<Rule[]>(() => Load(ValuePrefix), true);
    private static Rule[] Load(string prefix)
    {
        var source = TownPromptCatalogResources.Get(false).RuntimeText;
        var target = TownPromptCatalogResources.Get(true).RuntimeText;
        return source.Where(p => p.Key.StartsWith(prefix, StringComparison.Ordinal) && target.ContainsKey(p.Key))
            .Select(p => new Rule(p.Value, target[p.Key], p.Key.Substring(prefix.Length))).OrderBy(r => r.Names.Length).ThenByDescending(r => string.Concat(r.Parts).Length).ToArray();
    }
    private static string ProjectValues(string source, string kind, int depth = 0)
    {
        if (depth > 6) return source;
        foreach (Rule rule in Values.Value)
        {
            bool matches = rule.Kind.StartsWith(kind + ".", StringComparison.Ordinal);
            if (kind == "action") matches |= rule.Kind.StartsWith("suggestion.", StringComparison.Ordinal)
                || rule.Kind.StartsWith("outcome.", StringComparison.Ordinal) && !rule.Kind.StartsWith("outcome.lord_", StringComparison.Ordinal);
            if (matches && rule.TryRender(source, true, out string target, depth)) return target;
        }
        return source;
    }
    private static string ProjectList(string source, string kind, int depth = 0)
    {
        string separatorKey = kind == "refund" ? "SceneMemoryRefundSeparator" : "SceneMemoryPlanSeparator";
        string separator = TownPromptCatalogResources.Get(false).GetRuntimeText(separatorKey);
        string[] parts = source.Split(new[] { separator }, StringSplitOptions.None);
        var projected = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            projected[i] = ProjectValues(parts[i], kind, depth);
            if (projected[i] == parts[i]) return source;
        }
        return string.Join(TownPromptCatalogResources.Get(true).GetRuntimeText(separatorKey), projected);
    }
    public static string ProjectRegularPlan(string original, TownPromptTextCatalog catalog)
        => string.IsNullOrEmpty(original) || catalog?.IsEnglish != true ? original : ProjectList(original, "plan");
    public static string ProjectLordOutcomes(string original, bool isEnglish)
        => string.IsNullOrEmpty(original) || !isEnglish ? original : ProjectList(original, "outcome");
    public static string ProjectFact(string original, bool isEnglish)
    {
        if (string.IsNullOrEmpty(original) || !isEnglish) return original;
        foreach (Rule rule in Facts.Value)
            if (rule.TryRender(original, true, out string target)) return target;
        return original;
    }
    public static string Project(string original, TownPromptTextCatalog catalog)
    {
        if (string.IsNullOrEmpty(original) || catalog?.IsEnglish != true) return original;
        int prefixLength = Sequence.Match(original).Length;
        string body = original.Substring(prefixLength);
        foreach (Rule rule in Frames.Value)
            if (rule.TryRender(body, true, out string target))
                return original.Substring(0, prefixLength) + target;
        return original;
    }
}
