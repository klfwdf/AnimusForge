using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class GuardrailRuleHit { internal string RuleId; }
internal static partial class AIConfigHandler
{
    internal static bool SemanticHit, ThrowSemantic, ThrowLexical, ThrowSticky;
    internal static int SemanticCalls, LexicalCalls, StickyCalls;
    internal static string StickyTarget;
    internal static HashSet<string> Excluded;
    internal static void Reset()
    { SemanticHit = ThrowSemantic = ThrowLexical = ThrowSticky = false; SemanticCalls = LexicalCalls = StickyCalls = 0; StickyTarget = null; Excluded = null; }
    private static HashSet<string> BuildExcludedRuleIdSet(IEnumerable<string> ids, bool applyRuntimeAutoExclusions)
    { Program.Check(!applyRuntimeAutoExclusions, "worker must not read live runtime exclusions"); return new HashSet<string>(ids ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase); }
    private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondary, int cap, bool includeBuiltInRules, IEnumerable<string> excluded, bool applyRuntimeAutoExclusions)
    {
        SemanticCalls++; Program.Check(input == "input" && secondary == "secondary" && cap == 3 && !includeBuiltInRules && !applyRuntimeAutoExclusions, "semantic args preserved");
        if (ThrowSemantic) throw new InvalidOperationException("semantic");
        return SemanticHit ? new List<GuardrailRuleHit> { new GuardrailRuleHit { RuleId = "semantic" } } : new List<GuardrailRuleHit>();
    }
    private static List<GuardrailRuleHit> GetGuardrailLexicalRuleHits(string input, string secondary, int cap, bool includeBuiltInRules, IEnumerable<string> excluded, bool applyRuntimeAutoExclusions)
    {
        LexicalCalls++; Program.Check(input == "input" && secondary == "secondary" && cap == 3 && !includeBuiltInRules && !applyRuntimeAutoExclusions, "lexical args preserved");
        if (ThrowLexical) throw new InvalidOperationException("lexical");
        return new List<GuardrailRuleHit> { new GuardrailRuleHit { RuleId = "lexical" } };
    }
    private static List<GuardrailRuleHit> MergeStickyGuardrailRuleHits(string input, string secondary, List<GuardrailRuleHit> hits, int cap, HashSet<string> excluded, string target, bool applyRuntimeAutoExclusions)
    {
        StickyCalls++; StickyTarget = target; Excluded = excluded;
        Program.Check(!applyRuntimeAutoExclusions && cap == 3, "sticky merge uses captured cap, not runtime exclusions");
        if (ThrowSticky) throw new InvalidOperationException("sticky");
        return hits;
    }
}
internal static class Program
{
    private static int _checks;
    internal static void Check(bool yes, string reason) { _checks++; if (!yes) throw new Exception("FAIL " + reason); }
    private static void Run()
    {
        AIConfigHandler.Reset(); AIConfigHandler.SemanticHit = true;
        var hits = AIConfigHandler.GetMatchedExtraRuleHitsForWorker("input", "secondary", 3, new[] { "ban" }, "npc-key");
        Check(hits.Select(x => x.RuleId).SequenceEqual(new[] { "semantic" }) && AIConfigHandler.SemanticCalls == 1 && AIConfigHandler.LexicalCalls == 0 && AIConfigHandler.StickyCalls == 1, "semantic hit skips lexical and merges sticky");
        Check(AIConfigHandler.StickyTarget == "npc-key" && AIConfigHandler.Excluded.Contains("ban"), "captured sticky target/exclusions retained");
        AIConfigHandler.Reset();
        hits = AIConfigHandler.GetMatchedExtraRuleHitsForWorker("input", "secondary", 3, new[] { "ban" }, null);
        Check(hits.Select(x => x.RuleId).SequenceEqual(new[] { "lexical" }) && AIConfigHandler.SemanticCalls == 1 && AIConfigHandler.LexicalCalls == 1 && AIConfigHandler.StickyCalls == 1 && AIConfigHandler.StickyTarget == "", "semantic miss falls back to lexical; null target normalized");
        AIConfigHandler.Reset(); AIConfigHandler.ThrowSemantic = true;
        Check(AIConfigHandler.GetMatchedExtraRuleHitsForWorker("input", "secondary", 3, null, "npc").Count == 0 && AIConfigHandler.LexicalCalls == 0, "semantic exception returns empty without unsafe fallback");
        AIConfigHandler.Reset(); AIConfigHandler.ThrowLexical = true;
        Check(AIConfigHandler.GetMatchedExtraRuleHitsForWorker("input", "secondary", 3, null, "npc").Count == 0, "lexical exception returns empty");
        AIConfigHandler.Reset(); AIConfigHandler.SemanticHit = true; AIConfigHandler.ThrowSticky = true;
        Check(AIConfigHandler.GetMatchedExtraRuleHitsForWorker("input", "secondary", 3, null, "npc").Count == 0, "sticky exception returns empty");
        Console.WriteLine("PASS prompt-j06-extra-rule-fallback checks=" + _checks + " source=production-method game=stubbed");
    }
    private static int Main() { try { Run(); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; } }
}
