using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal sealed class MentionedWorldEntities
{
    internal List<string> Entities = new List<string>();
    internal MentionedWorldEntities Clone() => new MentionedWorldEntities { Entities = new List<string>(Entities) };
}
internal sealed class PromptBuildRequest
{
    internal bool SuppressDynamicRuleAndLore, HasPrefetchedLore, AllowRulePreprocess;
    internal string Input, NpcLastUtterance, GuardrailStickyTargetKey;
    internal HashSet<string> ExcludedRuleIds;
    internal PromptRuntimeTargetBinding Target;
    internal PromptRuleEligibility Eligibility;
}
internal sealed class PromptRoutingResult { internal List<string> AuxiliaryRuleHitIds; }
internal readonly struct PromptRuntimeTargetBinding { internal readonly string Id; internal PromptRuntimeTargetBinding(string id) { Id = id; } }
internal sealed class PromptRuleEligibility { internal bool IsCaptured = true; }
internal sealed class GuardrailRuleHit { internal string RuleId; }
internal sealed class LoreCandidateRules { internal string Mode; }
internal static class Logger { internal static void Log(string c, string m) { } }
internal static class AIConfigHandler
{
    internal static int CapReads, SemanticCalls, ClearCalls, RuleCap = 3;
    internal static readonly AsyncLocal<string> Target = new AsyncLocal<string>();
    internal static readonly AsyncLocal<PromptRuleEligibility> Eligibility = new AsyncLocal<PromptRuleEligibility>();
    internal static int GuardrailRuleReturnCap { get { CapReads++; return RuleCap; } }
    internal static IDisposable BeginGuardrailRuntimeScope() => new Scope(Target.Value, Eligibility.Value);
    private sealed class Scope : IDisposable
    {
        private readonly string _target; private readonly PromptRuleEligibility _eligibility;
        internal Scope(string target, PromptRuleEligibility eligibility) { _target = target; _eligibility = eligibility; }
        public void Dispose() { Target.Value = _target; Eligibility.Value = _eligibility; }
    }
    internal static void ApplyGuardrailRuntimeTarget(PromptRuntimeTargetBinding target, PromptRuleEligibility eligibility)
    { Target.Value = target.Id; Eligibility.Value = eligibility; }
    internal static void ClearGuardrailRuntimeTarget() { ClearCalls++; Target.Value = null; Eligibility.Value = null; }
    internal static List<GuardrailRuleHit> GetMatchedExtraRuleHitsForWorker(string input, string secondary, int cap, IEnumerable<string> exclusions, string sticky)
    {
        SemanticCalls++;
        Program.Check(Target.Value == "npc" && Eligibility.Value?.IsCaptured == true, "fallback uses captured target and eligibility");
        Program.Check(cap == 3 && sticky == "npc-key" && exclusions.Contains("ban"), "fallback cap/sticky/exclusions captured");
        return new List<GuardrailRuleHit> { new GuardrailRuleHit { RuleId = "fallback" } };
    }
}
internal static class KnowledgeLibraryBehavior
{
    internal static int Calls;
    internal static bool Stale;
    internal static LoreCandidateRules CollectPromptLoreCandidates(MentionedWorldEntities mentions, long version, PromptLoreSettings settings)
    { Calls++; Program.Check(AIConfigHandler.Target.Value == "npc", "Lore uses captured scope"); return Stale ? null : new LoreCandidateRules { Mode = version + ":" + mentions.Entities[0] }; }
}
internal static class WorldEntityRetrievalService
{
    internal sealed class DetachedEntityCandidates { }
    internal sealed class EntityCapture { }
    internal sealed class DetachedEntityMatches { internal string Value; }
    internal static bool Throw;
    internal static int Calls;
    internal static DetachedEntityMatches MatchDetachedCandidates(DetachedEntityCandidates candidates, MentionedWorldEntities mentions, string input, int cap)
    { Calls++; if (Throw) throw new InvalidOperationException("entity failure"); return new DetachedEntityMatches { Value = input + ":" + cap }; }
}
internal partial class MyBehavior { }
internal static class Program
{
    private static int _checks;
    internal static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception("FAIL " + reason); }
    private static int Main()
    {
        try { Run(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
    private static void Run()
    {
        var phase = new PromptBuildPhases
        {
            Request = new PromptBuildRequest { Input = "hello", NpcLastUtterance = "last", AllowRulePreprocess = true,
                ExcludedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ban" }, GuardrailStickyTargetKey = "npc-key",
                Target = new PromptRuntimeTargetBinding("npc"), Eligibility = new PromptRuleEligibility() },
            Routing = new PromptRoutingResult(),
            Retrieval = new PromptRetrievalCapture { AuxiliaryMentions = new MentionedWorldEntities { Entities = new List<string> { "hero" } },
                LoreRuleVersion = 7, LoreSettings = new PromptLoreSettings { Enabled = true },
                EntityCandidates = new WorldEntityRetrievalService.DetachedEntityCandidates(), EntityMaxInjectedEntities = 6 }
        };
        var input = MyBehavior.CreateSharedKnowledgeWorkInput(phase);
        Check(input != null && input.NeedsFallbackExtraRules && input.ExtraRuleReturnCap == 3 && AIConfigHandler.CapReads == 1, "fallback cap captured once");
        phase.Retrieval.AuxiliaryMentions.Entities[0] = "changed";
        phase.Request.ExcludedRuleIds.Clear();
        Check(input.Mentions.Entities[0] == "hero" && input.ExcludedRuleIds.Contains("ban"), "mutable request sets detached");
        var result = Task.Run(() => MyBehavior.RunSharedKnowledgeRetrieval(input)).GetAwaiter().GetResult();
        Check(phase.Retrieval.EntityMatches == null && phase.Retrieval.LoreCandidates == null, "worker does not publish into game capture");
        Check(result.LoreCandidates.Mode == "7:hero" && result.EntityMatches.Value == "hello:6" && result.FallbackExtraRuleHits[0].RuleId == "fallback", "worker returns Lore/entity/rule hits");
        Check(AIConfigHandler.Target.Value == null && AIConfigHandler.Eligibility.Value == null && AIConfigHandler.ClearCalls == 1, "worker ambient scope restored");
        MyBehavior.ApplySharedKnowledgeRetrieval(phase, result);
        Check(ReferenceEquals(phase.Retrieval.EntityMatches, result.EntityMatches) && ReferenceEquals(phase.Retrieval.LoreCandidates, result.LoreCandidates), "owner phase publishes one result");
        phase.Routing.AuxiliaryRuleHitIds = new List<string>();
        var noFallback = MyBehavior.CreateSharedKnowledgeWorkInput(phase);
        Check(!noFallback.NeedsFallbackExtraRules && noFallback.ExtraRuleReturnCap == 0 && AIConfigHandler.CapReads == 1, "normal preselection skips MCM cap read");
        KnowledgeLibraryBehavior.Stale = true; WorldEntityRetrievalService.Throw = true;
        var failed = MyBehavior.RunSharedKnowledgeRetrieval(noFallback);
        Check(failed.LoreCandidates == null && failed.EntityMatches == null && failed.FallbackExtraRuleHits == null, "stale Lore and failed entity stay empty");
        Check(AIConfigHandler.Target.Value == null && AIConfigHandler.Eligibility.Value == null, "exception restores ambient scope");
        phase.Request.SuppressDynamicRuleAndLore = true;
        Check(MyBehavior.CreateSharedKnowledgeWorkInput(phase) == null, "suppressed prompt schedules no work");
        Console.WriteLine("PASS prompt-j06-knowledge-phases checks=" + _checks + " source=production-methods game=stubbed");
    }
}
