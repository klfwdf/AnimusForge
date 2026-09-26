using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

internal static class PromptRuleTextEvidence
{
    internal static List<string> SemanticSeeds(string ruleTag, string ruleInstruction, IReadOnlyList<string> triggerKeywords)
    {
        var seeds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (triggerKeywords != null)
                foreach (string keyword in triggerKeywords) Add(keyword);
            if (string.Equals((ruleTag ?? "").Trim(), "reward", StringComparison.OrdinalIgnoreCase))
                Add(InstructionSeed(ruleTag, ruleInstruction));
            if (seeds.Count == 0) Add(ruleTag);
        }
        catch { }
        return seeds;

        void Add(string raw)
        {
            string text = Normalize(raw);
            if (text.Length > 260) text = text.Substring(0, 260);
            if (text.Length > 0 && seen.Add(text)) seeds.Add(text);
        }
    }

    internal static string RerankText(string id, string group, string instruction, IReadOnlyList<string> triggerKeywords)
    {
        try
        {
            string ruleId = Normalize(id);
            string ruleGroup = Normalize(group);
            string purpose = Normalize(InstructionSeed(id, instruction));
            var keywords = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (triggerKeywords != null)
                foreach (string keyword in triggerKeywords)
                {
                    string text = Normalize(keyword);
                    if (text.Length > 48) text = text.Substring(0, 48);
                    if (text.Length > 0 && seen.Add(text)) keywords.Add(text);
                }
            if (keywords.Count > 6) keywords = keywords.Take(6).ToList();
            var builder = new StringBuilder();
            if (ruleGroup.Length > 0) builder.AppendLine("规则组: " + ruleGroup);
            if (ruleId.Length > 0) builder.AppendLine("规则ID: " + ruleId);
            if (purpose.Length > 0) builder.AppendLine("用途: " + purpose);
            if (keywords.Count > 0) builder.AppendLine("触发词: " + string.Join(" / ", keywords));
            return Normalize(builder.ToString());
        }
        catch { return ""; }
    }

    private static string InstructionSeed(string ruleTag, string ruleInstruction)
    {
        string tag = Normalize(ruleTag);
        string instruction = Normalize(ruleInstruction);
        if (instruction.Length == 0) return tag;
        int end = instruction.IndexOfAny(new[] { '。', '！', '!', '？', '?', '\n', '\r', ';', '；' });
        if (end > 0) instruction = instruction.Substring(0, end);
        if (instruction.Length > 120) instruction = instruction.Substring(0, 120);
        return tag.Length == 0 ? instruction : tag + " " + instruction;
    }

    private static string Normalize(string value)
    {
        string text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
