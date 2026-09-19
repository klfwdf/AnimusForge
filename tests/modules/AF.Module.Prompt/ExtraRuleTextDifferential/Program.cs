using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge
{
    public sealed class PreprocessFormatException : Exception { }
    public sealed class PostprocessRuleEntry { }
    public sealed class Hero { }
    public sealed class CharacterObject { public string StringId; }
    public sealed class MentionedWorldEntities { public MentionedWorldEntities Clone() => new MentionedWorldEntities(); }
    public static class Logger { public static void Log(string category, string message) { } }
    internal sealed class FakeRevision { internal long Revision = 7; }
    internal sealed class FakePromptConfiguration
    {
        private sealed class Scope : IDisposable { public void Dispose() { } }
        internal IDisposable BeginCapture() => new Scope();
        internal FakeRevision Read() => new FakeRevision();
        internal FakeRevision Capture() => new FakeRevision();
    }
    public static class VassalageBehavior { public static string BuildRuntimeVassalageInstructionForExternal(Hero hero, CharacterObject character) => ""; }
    public static class KingdomAnnexationBehavior { public static string BuildRuntimeAnnexationInstructionForExternal(Hero hero, CharacterObject character) => ""; }
    public sealed class RomanceSystemBehavior
    {
        public static RomanceSystemBehavior Instance;
        public string BuildMarriageRuntimeInstruction(Hero hero) => "";
    }
    public static class VanillaIssueOfferBridge { public static string BuildRuntimePromptBlockForExternal(Hero hero) => ""; }
    public static class SceneTauntBehavior { public static string BuildUnifiedTauntRuntimeInstructionForExternal(Hero hero, CharacterObject character, int agentIndex) => ""; }
    public static class MyBehavior { public static string BuildNpcMajorActionsRuntimeInstructionForExternal(Hero hero) => ""; }
    public static class VassalageDiagnosticLog
    {
        public static void Event(string name, Dictionary<string, object> fields) { }
        public static string DescribeHero(Hero hero) => "";
    }
    internal static partial class AIConfigHandler
    {
        private static readonly FakePromptConfiguration _promptConfiguration = new FakePromptConfiguration();
        private static PromptStickyRuleStore _stickyGuardrailRuleStore = new PromptStickyRuleStore();
        private static bool SemanticScenario;
        private const int GuardrailRuleReturnCap = 3;
        private static int ClampGuardrailReturnCap(int cap) => Math.Max(1, Math.Min(20, cap));
        private static bool ShouldExcludePlayerPartyTradeLimitedRulesForConversationTarget() => false;
        private static bool ShouldExcludeSceneMoveRuleForCurrentMission() => false;
        private static bool IsRuleCurrentlyEligibleForRag(string ruleId) => true;
        private static string ResolveGuardrailStickyTargetKey() => "";
        private static bool DidGuardrailRuleRecentlyComplete(string ruleId, string secondaryInput) => false;
        private static bool TryGetRuleEval(string input, string secondary, string id, out GuardrailRuleEval eval, IEnumerable<string> excluded = null, bool applyRuntimeAutoExclusions = true)
        { eval = null; return false; }
        private static Dictionary<string, GuardrailRulePromptConfig> BuildRulePromptRegistry() => new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["trade_context"] = new GuardrailRulePromptConfig { Id = "trade_context", IsEnabled = true, Instruction = "RULE_TEXT trade permitted under agreed terms.", TriggerKeywords = new List<string> { "barter" }, Group = "custom", Priority = 5 }
        };
        private static bool TryGetGuardrailEvalSnapshot(string input, string secondary, out GuardrailEvalSnapshot snapshot, IEnumerable<string> excluded, bool applyRuntimeAutoExclusions)
        {
            snapshot = new GuardrailEvalSnapshot();
            if (!SemanticScenario) return false;
            snapshot.Rules["trade_context"] = new GuardrailRuleEval { Hit = true, AmpScore = 0.83f, MatchedSeed = "barter" };
            return true;
        }
        private static bool IsNobleDeferenceRuntimeEligible(bool hasAnyHero) => true;
        private static bool ShouldExcludeRuntimeRuleForConversationTarget(string ruleId) => false;
        private static bool IsPlayerKingdomRecruitmentModeActive() => false;
        private static string BuildRuntimeKingdomServiceInstruction() => "";
        private static string BuildRuntimeHeroJoinPartyInstructionForExternal() => "";
        private static string BuildRuntimeLordsHallAccessInstruction() => "";
        private static string BuildRuntimeRuleConstraintHint(string ruleId) => "";
        private static string BuildExtraRuleHitDebugDetail(string input, string secondary, GuardrailRuleHit hit, IEnumerable<string> excluded) => "";
        private static string ApplyPlayerDisplayNameToGuardrailText(string text) => text;
        private static Hero ResolveConversationTargetHero() => null;
        private static CharacterObject ResolveConversationTargetCharacter() => null;
        private static int ResolveConversationTargetAgentIndex() => -1;
        internal static string Render(bool semantic)
        {
            SemanticScenario = semantic;
            _stickyGuardrailRuleStore = new PromptStickyRuleStore();
            string input = "Tell me about Praven, Alda the King; can we barter this item?";
#if CURRENT
            List<GuardrailRuleHit> hits = semantic
                ? GetGuardrailSemanticRuleHits(input, "", 3, false, null)
                : GetMatchedExtraRuleHitsForWorker(input, "", 3, null, "");
            if (hits == null || hits.Count != 1 || hits[0].RuleId != "trade_context") throw new Exception("new production rule selection failed");
            return FormatMatchedExtraRuleInstructions(input, "", true, null, hits);
#else
            return BuildMatchedExtraRuleInstructions(input, "", 3, true, null);
#endif
        }
    }
}

internal static class Program
{
    private static void Main()
    {
        foreach (bool semantic in new[] { true, false })
        {
            string output = AnimusForge.AIConfigHandler.Render(semantic);
            if (!output.Contains("【附加规则:trade_context】") || !output.Contains("RULE_TEXT trade permitted under agreed terms.")) throw new Exception("rule ID/text missing for semantic=" + semantic + ": " + output);
            Console.WriteLine("RESULT_" + (semantic ? "semantic" : "lexical") + "=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(output)));
        }
    }
}
