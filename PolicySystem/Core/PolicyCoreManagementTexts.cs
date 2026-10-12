using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.PolicyTargets;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class PolicyCoreManagementTexts
{
    private static readonly Lazy<Dictionary<string, string>> RawValues = new Lazy<Dictionary<string, string>>(
        () => Load("PolicyCoreManagementCompatibility.json"));
    private static readonly Lazy<Dictionary<string, string>> PromptValues = new Lazy<Dictionary<string, string>>(
        () => Load("PolicyCoreManagementPromptTexts.json"));
    private static readonly Lazy<DiagnosticProjection> Diagnostics = new Lazy<DiagnosticProjection>(
        () => new DiagnosticProjection(RawValues.Value));
    private static readonly Lazy<Dictionary<string, TemplateRule>> TargetRules = new Lazy<Dictionary<string, TemplateRule>>(
        () => new[] { "target_source_wrapper", "target_source_prefix", "target_named_ruler_wrapper" }
            .ToDictionary(id => id, id => new TemplateRule(id, Raw(id)), StringComparer.Ordinal));
    private static readonly Lazy<Regex> TargetCountRule = new Lazy<Regex>(() => new Regex(
        "\\A(?<LABEL>.*)" + Regex.Escape(Raw("target_count_suffix")).Replace(
            Regex.Escape("{COUNT}"), "(?<COUNT>[0-9]+)")
        + "(?<DESCRIPTOR>；descriptor=.*)?\\z", RegexOptions.CultureInvariant | RegexOptions.Singleline));

    internal static bool IsEnglishPrompt => string.Equals(Prompt("package_language"), "en", StringComparison.Ordinal);

    internal static string Raw(string id) => RawValues.Value[id];
    internal static string Prompt(string id) => PromptValues.Value[id];

    internal static string FormatPrompt(string id, params string[] variables)
        => FormatLiteral(Prompt(id), variables);

    // Formatting only visits the source template. Inserted names and diagnostics are never parsed again.
    internal static string FormatLiteral(string template, params string[] variables)
    {
        if (variables == null || variables.Length % 2 != 0)
            throw new ArgumentException("Localization variables must be name/value pairs.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < variables.Length; index += 2)
            values[variables[index]] = variables[index + 1] ?? string.Empty;
        template = template ?? string.Empty;
        var result = new StringBuilder(template.Length);
        for (int index = 0; index < template.Length;)
        {
            if (template[index] == '{')
            {
                int end = template.IndexOf('}', index + 1);
                if (end > index && values.TryGetValue(template.Substring(index + 1, end - index - 1), out string value))
                {
                    result.Append(value);
                    index = end + 1;
                    continue;
                }
            }
            result.Append(template[index++]);
        }
        return result.ToString();
    }

    internal static string ProjectDiagnosticForPrompt(string text)
        => !IsEnglishPrompt || string.IsNullOrEmpty(text) ? text : Diagnostics.Value.Project(text);

    internal static string ProjectTargetDisplayNameForPrompt(
        string kind, string text, bool followCurrentRulingClan = false, bool isSemanticTarget = false)
    {
        if (!IsEnglishPrompt || string.IsNullOrEmpty(text) || isSemanticTarget)
            return text;
        if (string.Equals(kind, "source", StringComparison.OrdinalIgnoreCase))
        {
            // The legacy source producer always prepends this wrapper; truncated names retain their remainder.
            if (TargetRules.Value["target_source_wrapper"].TryProject(text, out string full))
                return full;
            if (TargetRules.Value["target_source_prefix"].TryProject(text, out string prefix))
                return prefix;
        }
        else if (string.Equals(kind, "ruler", StringComparison.OrdinalIgnoreCase))
        {
            if (followCurrentRulingClan && string.Equals(text, Raw("target_current_ruler"), StringComparison.Ordinal))
                return Prompt("target_current_ruler");
            if (!followCurrentRulingClan && TargetRules.Value["target_named_ruler_wrapper"].TryProject(text, out string ruler))
                return ruler;
        }
        else if (string.Equals(kind, "selector", StringComparison.OrdinalIgnoreCase)
            && string.Equals(text, Raw("target_legacy_selector_label"), StringComparison.Ordinal))
        {
            return Prompt("target_legacy_selector_label");
        }
        return text;
    }

    internal static string ProjectSelectorDescriptorForPrompt(string text)
        => IsEnglishPrompt && string.Equals(text, Raw("target_legacy_selector_descriptor"), StringComparison.Ordinal)
            ? Prompt("target_legacy_selector_descriptor") : text;

    internal static PolicyTargetHandleDirectory ProjectDirectoryForPrompt(
        PolicyTargetHandleDirectory directory, ISet<string> literalDisplayNameHandles)
    {
        if (!IsEnglishPrompt || directory?.Targets == null)
            return directory;
        Dictionary<string, PolicyTargetHandleDirectoryEntry> projectedTargets = null;
        foreach (KeyValuePair<string, PolicyTargetHandleDirectoryEntry> pair in directory.Targets)
        {
            PolicyTargetHandleDirectoryEntry entry = pair.Value;
            string projectedDescription = ProjectDirectoryDescription(entry,
                literalDisplayNameHandles?.Contains(pair.Key) == true);
            if (entry == null || string.Equals(projectedDescription, entry.Description, StringComparison.Ordinal))
                continue;
            if (projectedTargets == null)
                projectedTargets = new Dictionary<string, PolicyTargetHandleDirectoryEntry>(directory.Targets, directory.Targets.Comparer);
            projectedTargets[pair.Key] = new PolicyTargetHandleDirectoryEntry
            {
                Kind = entry.Kind,
                Description = projectedDescription,
                SelectorId = entry.SelectorId,
                EntityId = entry.EntityId,
                TargetPlanVersion = entry.TargetPlanVersion,
                CurrentSettlementCount = entry.CurrentSettlementCount,
                FollowCurrentRulingClan = entry.FollowCurrentRulingClan
            };
        }
        return projectedTargets == null ? directory : new PolicyTargetHandleDirectory
        {
            StructureVersion = directory.StructureVersion,
            Targets = projectedTargets,
            Capabilities = directory.Capabilities
        };
    }

    private static string ProjectDirectoryDescription(PolicyTargetHandleDirectoryEntry entry, bool literalDisplayName)
    {
        if (entry == null)
            return null;
        string text = entry.Description;
        if (string.IsNullOrEmpty(text))
            return text;
        if (!string.Equals(entry.Kind, "selector", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(entry.Kind, "plan", StringComparison.OrdinalIgnoreCase))
            return ProjectTargetDisplayNameForPrompt(entry.Kind, text, entry.FollowCurrentRulingClan, literalDisplayName);
        Match match = TargetCountRule.Value.Match(text);
        if (!match.Success || !string.Equals(match.Groups["COUNT"].Value,
            entry.CurrentSettlementCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            return text;
        string label = ProjectTargetDisplayNameForPrompt(entry.Kind, match.Groups["LABEL"].Value,
            isSemanticTarget: literalDisplayName);
        string descriptor = match.Groups["DESCRIPTOR"].Value;
        const string descriptorPrefix = "；descriptor=";
        string projectedDescriptor = descriptor.Length == 0 ? string.Empty : "; descriptor="
            + ProjectSelectorDescriptorForPrompt(descriptor.Substring(descriptorPrefix.Length));
        return label + FormatPrompt("target_count_suffix", "COUNT", match.Groups["COUNT"].Value) + projectedDescriptor;
    }

    private static Dictionary<string, string> Load(string fileName)
    {
        Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            File.ReadAllText(AnimusForgeModulePaths.GetModuleDataFilePath(fileName), Encoding.UTF8));
        if (values == null)
            throw new InvalidDataException("Policy text resource must be a JSON object: " + fileName);
        return new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    private sealed class TemplateRule
    {
        private static readonly Regex SlotPattern = new Regex("\\{(?<NAME>[A-Z][A-Z0-9_]*)\\}", RegexOptions.CultureInvariant);
        internal readonly string Id;
        internal readonly string[] Slots;
        internal readonly string Pattern;
        private readonly Regex matcher;

        internal TemplateRule(string id, string template, string groupPrefix = "", bool createMatcher = true)
        {
            Id = id;
            var slots = new List<string>();
            var pattern = new StringBuilder();
            int position = 0;
            foreach (Match slot in SlotPattern.Matches(template))
            {
                string name = slot.Groups["NAME"].Value;
                pattern.Append(Regex.Escape(template.Substring(position, slot.Index - position)));
                pattern.Append("(?<").Append(groupPrefix).Append(name).Append(">.*?)");
                slots.Add(name);
                position = slot.Index + slot.Length;
            }
            pattern.Append(Regex.Escape(template.Substring(position)));
            Pattern = pattern.ToString();
            Slots = slots.ToArray();
            if (createMatcher)
                matcher = new Regex("\\A" + Pattern + "\\z", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        internal bool TryProject(string text, out string projected)
        {
            Match match = matcher.Match(text);
            projected = match.Success ? FormatMatch(match, string.Empty) : text;
            return match.Success;
        }

        internal string FormatMatch(Match match, string prefix)
        {
            var pairs = new string[Slots.Length * 2];
            for (int index = 0; index < Slots.Length; index++)
            {
                pairs[index * 2] = Slots[index];
                pairs[index * 2 + 1] = match.Groups[prefix + Slots[index]].Value;
            }
            return FormatPrompt(Id, pairs);
        }
    }

    private sealed class DiagnosticProjection
    {
        private readonly Dictionary<string, string> exact = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly TemplateRule[] rules;
        private readonly Regex matcher;

        internal DiagnosticProjection(Dictionary<string, string> raw)
        {
            var variableRules = new List<TemplateRule>();
            var branches = new List<string>();
            foreach (KeyValuePair<string, string> entry in raw)
            {
                if (!entry.Key.StartsWith("diagnostic_", StringComparison.Ordinal))
                    continue;
                string prefix = "D" + variableRules.Count.ToString(CultureInfo.InvariantCulture) + "_";
                var rule = new TemplateRule(entry.Key, entry.Value, prefix, false);
                if (rule.Slots.Length == 0)
                {
                    exact[entry.Value] = entry.Key;
                    continue;
                }
                variableRules.Add(rule);
                branches.Add("(?<" + prefix + ">" + rule.Pattern + ")");
            }
            rules = variableRules.ToArray();
            matcher = new Regex("\\A(?:" + string.Join("|", branches) + ")\\z", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        internal string Project(string text)
        {
            if (exact.TryGetValue(text, out string id))
                return Prompt(id);
            Match match = matcher.Match(text);
            if (!match.Success)
                return text;
            for (int index = 0; index < rules.Length; index++)
            {
                string prefix = "D" + index.ToString(CultureInfo.InvariantCulture) + "_";
                if (match.Groups[prefix].Success)
                    return rules[index].FormatMatch(match, prefix);
            }
            return text;
        }
    }
}
