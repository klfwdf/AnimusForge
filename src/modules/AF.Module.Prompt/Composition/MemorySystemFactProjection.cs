using System;
using System.Collections.Generic;

namespace AnimusForge;

// Request/display boundary only. Never used by a save or history mutation owner.
internal static class MemorySystemFactProjection
{
    private sealed class Template
    {
        internal readonly string Key;
        internal readonly string[] Parts;
        internal Template(string key, params string[] parts) { Key = key; Parts = parts; }
    }
    private static readonly Template[] Templates =
    {
        new Template("memory_fact_first_known", PromptComposerTexts.Raw("Template_first_known").Split('|')),
        new Template("memory_fact_first_appearance", PromptComposerTexts.Raw("Template_first_appearance").Split('|')),
        new Template("memory_fact_first_unknown", PromptComposerTexts.Raw("Template_first_unknown").Split('|')),
        new Template("memory_fact_revisit_today", PromptComposerTexts.Raw("Template_revisit_today").Split('|')),
        new Template("memory_fact_revisit_days", PromptComposerTexts.Raw("Template_revisit_days").Split('|')),
        new Template("memory_fact_execution_last", PromptComposerTexts.Raw("Template_execution_last").Split('|')),
        new Template("memory_fact_execution_speech", PromptComposerTexts.Raw("Template_execution_speech").Split('|')),
        new Template("memory_policy_agenda_submission", PromptComposerTexts.Raw("policy_agenda_source_parts").Split('|')),
    };

    private static readonly string[] CourierKeys =
    {
        "memory_courier_player_delivered",
        "memory_courier_player_departed",
        "memory_courier_crew_delivered",
        "memory_courier_gold_delivered",
        "memory_courier_gold_delivered_value",
        "memory_courier_item_delivered",
        "memory_courier_item_delivered_value",
        "memory_courier_troop_delivered",
        "memory_courier_troop_delivered_value",
        "memory_courier_prisoner_counted_delivered",
        "memory_courier_prisoner_delivered",
        "memory_courier_prisoner_counted_delivered_value",
        "memory_courier_prisoner_delivered_value",
        "memory_courier_settlement_delivered",
        "memory_courier_settlement_delivered_value",
        "memory_courier_show_gold_delivered",
        "memory_courier_show_item_delivered",
        "memory_courier_crew_planned",
        "memory_courier_gold_planned",
        "memory_courier_item_planned",
        "memory_courier_troop_planned",
        "memory_courier_prisoner_counted_planned",
        "memory_courier_prisoner_planned",
        "memory_courier_settlement_planned",
        "memory_courier_show_gold_planned",
        "memory_courier_show_item_planned",
        "memory_courier_npc_delivered",
        "memory_courier_npc_departed",
        "memory_courier_war",
        "memory_courier_npc_crew",
        "memory_courier_player_destroyed_before_delivery",
        "memory_courier_player_destroyed_after_delivery",
        "memory_courier_player_missing",
        "memory_courier_npc_destroyed",
        "memory_courier_npc_missing",
    };
    private static readonly Template[] CourierTemplates = BuildCourierTemplates();
    private static Template[] BuildCourierTemplates()
    {
        var templates = new Template[CourierKeys.Length];
        for (int index = 0; index < CourierKeys.Length; index++)
            templates[index] = new Template(CourierKeys[index], System.Text.RegularExpressions.Regex.Split(
                PromptComposerTexts.Raw(CourierKeys[index] + "_source"), @"\{[0-9]+\}"));
        // Match structured quantity/value variants before a generic dynamic name can absorb their suffix.
        Array.Sort(templates, (left, right) =>
        {
            int slots = right.Parts.Length.CompareTo(left.Parts.Length);
            return slots != 0 ? slots : string.Concat(right.Parts).Length.CompareTo(string.Concat(left.Parts).Length);
        });
        return templates;
    }
    private static string ProjectCourierBody(string body)
    {
        foreach (Template template in CourierTemplates)
        {
            if (!TryExtract(body, template, out string[] values)) continue;
            int kindIndex = template.Key == "memory_courier_npc_delivered" || template.Key == "memory_courier_npc_destroyed" ? 2
                : template.Key == "memory_courier_npc_departed" || template.Key == "memory_courier_npc_missing" ? 1 : -1;
            if (kindIndex >= 0)
            {
                for (int kind = 588; kind <= 591; kind++)
                    if (values[kindIndex] == PromptComposerTexts.Raw("memory_courier_raw_" + kind))
                        values[kindIndex] = PromptComposerTexts.Get("memory_courier_value_" + kind);
            }
            if (template.Key == "memory_courier_player_destroyed_before_delivery"
                || template.Key == "memory_courier_player_destroyed_after_delivery"
                || template.Key == "memory_courier_npc_destroyed")
            {
                if (values[1] == PromptComposerTexts.Raw("memory_courier_unknown_faction_source"))
                    values[1] = PromptComposerTexts.Get("memory_courier_value_unknown_faction");
            }
            return Expand(PromptComposerTexts.Get(template.Key), values);
        }
        return body;
    }

    private static readonly Lazy<string[]> RevisitMarkers = new Lazy<string[]>(() => PromptComposerTexts.Raw("RevisitMarkers_Cn").Split(','));
    private static readonly Lazy<string[]> FactPrefixes = new Lazy<string[]>(() => new[]
    {
        PromptComposerTexts.Raw("afef_npc_action"), PromptComposerTexts.Raw("afef_player_action"),
        PromptComposerTexts.Raw("afef_npc_action_bracket"), PromptComposerTexts.Raw("afef_player_action_bracket")
    });
    private static readonly Lazy<string[]> FactScopes = new Lazy<string[]>(() => new[]
    {
        PromptComposerTexts.Raw("main_history_raw_019"),
        PromptComposerTexts.Raw("history_section_scope_current"),
        PromptComposerTexts.Raw("history_section_scope_historical")
    });

    // Compressed records retain their original scope and captured date/scene/speaker.
    // Only known fact bodies are projected when a request reads those records.
    internal static string ProjectRecordedFactForPrompt(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.IndexOf('\n') >= 0)
        {
            string[] lines = text.Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                bool hasCarriageReturn = line.EndsWith("\r", StringComparison.Ordinal);
                lines[index] = hasCarriageReturn
                    ? ProjectRecordedFactForPrompt(line.Substring(0, line.Length - 1)) + "\r"
                    : ProjectRecordedFactForPrompt(line);
            }
            return string.Join("\n", lines);
        }
        int scopeLength = 0;
        foreach (string scope in FactScopes.Value)
            if (text.StartsWith(scope, StringComparison.Ordinal)) { scopeLength = scope.Length; break; }
        string value = scopeLength == 0 ? text : text.Substring(scopeLength);
        string projected = ProjectNpcFact(value);
        if (projected != value) return text.Substring(0, scopeLength) + projected;
        if (!value.StartsWith("[", StringComparison.Ordinal)) return text;

        int endMetadata = -1;
        foreach (string prefix in FactPrefixes.Value)
        {
            string boundary = "] " + prefix;
            int at = value.IndexOf(boundary, StringComparison.Ordinal);
            if (at < 0) continue;
            // Ambiguous captured metadata or quoted facts must not be guessed.
            if (endMetadata >= 0 || value.IndexOf(boundary, at + boundary.Length, StringComparison.Ordinal) >= 0) return text;
            endMetadata = at;
        }
        if (endMetadata < 0) return text;
        int separators = 0;
        for (int index = 1; index < endMetadata; index++)
            if (value[index] == '|' || value[index] == '\uFF5C') separators++;
        if (separators < 2) return text;
        int factStart = endMetadata + 2;
        string fact = value.Substring(factStart);
        projected = ProjectNpcFact(fact);
        return projected == fact ? text : text.Substring(0, scopeLength + factStart) + projected;
    }

    internal static string ProjectNpcFact(string fact)
    {
        if (string.IsNullOrEmpty(fact)) return fact;
        if (fact.IndexOf('\n') >= 0)
        {
            string[] lines = fact.Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                bool hasCarriageReturn = line.EndsWith("\r", StringComparison.Ordinal);
                lines[index] = hasCarriageReturn
                    ? ProjectNpcFact(line.Substring(0, line.Length - 1)) + "\r"
                    : ProjectNpcFact(line);
            }
            return string.Join("\n", lines);
        }
        string prefix = null;
        foreach (string candidate in FactPrefixes.Value)
            if (fact.StartsWith(candidate, StringComparison.Ordinal)) { prefix = candidate; break; }
        if (prefix == null) return fact;
        string body = fact.Substring(prefix.Length).TrimStart();
        string projected = ProjectBody(body);
        return projected == body ? fact : prefix + " " + projected;
    }

    internal static string ProjectBody(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        string gathering = NobleGatheringTextProjection.ProjectFactBodyForPrompt(body);
        if (gathering != body) return gathering;
        string courier = ProjectCourierBody(body);
        if (courier != body) return courier;
        string worldMap = WorldMapFactPromptProjection.ProjectBodyForPrompt(body);
        if (worldMap != body) return worldMap;
        foreach (Template template in Templates)
        {
            if (!TryExtract(body, template, out string[] values)) continue;
            if (template.Key == "memory_fact_revisit_days"
                && (!int.TryParse(values[1], out int days) || days <= 0)) continue;
            return Expand(PromptComposerTexts.Get(template.Key), values);
        }
        // Scene revisit broadcasts add a captured NPC display name before the same
        // fixed body. Retain the name; reject repeated markers rather than guess.
        foreach (string marker in RevisitMarkers.Value)
        {
            int at = body.IndexOf(marker, StringComparison.Ordinal);
            if (at <= 0 || body.IndexOf(marker, at + marker.Length, StringComparison.Ordinal) >= 0) continue;
            string remainder = body.Substring(at);
            string projected = ProjectBody(remainder);
            if (projected != remainder) return body.Substring(0, at) + projected;
        }
        return body;
    }

    private static bool TryExtract(string body, Template template, out string[] values)
    {
        values = null;
        string[] parts = template.Parts;
        if (!body.StartsWith(parts[0], StringComparison.Ordinal)) return false;
        if (parts.Length == 1) { values = Array.Empty<string>(); return body == parts[0]; }
        var captured = new string[parts.Length - 1];
        int position = parts[0].Length;
        for (int index = 1; index < parts.Length; index++)
        {
            int next = body.IndexOf(parts[index], position, StringComparison.Ordinal);
            if (next < position || body.IndexOf(parts[index], next + parts[index].Length, StringComparison.Ordinal) >= 0) return false;
            captured[index - 1] = body.Substring(position, next - position);
            if (captured[index - 1].Length == 0) return false;
            position = next + parts[index].Length;
        }
        if (position != body.Length) return false;
        values = captured;
        return true;
    }

    // Only template placeholders are expanded. Inserted names/quotes never re-enter parsing.
    private static string Expand(string template, IReadOnlyList<string> values)
    {
        var result = new System.Text.StringBuilder(template.Length);
        for (int index = 0; index < template.Length;)
        {
            if (template[index] == '{' && index + 2 < template.Length && template[index + 2] == '}'
                && template[index + 1] >= '0' && template[index + 1] <= '9')
            {
                int value = template[index + 1] - '0';
                if (value < values.Count) { result.Append(values[value]); index += 3; continue; }
            }
            result.Append(template[index++]);
        }
        return result.ToString();
    }
}
