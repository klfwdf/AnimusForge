using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;

// Second-round audit fixes that live in pure rules: strict tag enum names,
// reopened-round close status, newest-first signal retention, key-based
// context dedupe and surrogate-safe text limits.
internal static class AuditRoundTwoReplay
{
    internal static void Run()
    {
        TagEnumNames(); ReopenedRoundStatus(); SignalRetention(); SignalContextDedupe(); SurrogateLimit();
        Console.WriteLine("PASS audit round two: tag enum names, reopened status, signal retention, context dedupe, surrogate limit");
    }

    private static void TagEnumNames()
    {
        Test.True(DialogueTagPayload.TryParse("action=peace;move=discussion;target=k", out var ok) && ok.Action == DialogueDiplomaticAction.Peace,
            "a member name parses case-insensitively");
        Test.True(!DialogueTagPayload.TryParse("action=0;move=discussion;target=k", out _), "a numeric action is rejected");
        Test.True(!DialogueTagPayload.TryParse("action=peace, trade;move=discussion;target=k", out _), "a comma-combined action is rejected");
        Test.True(!DialogueTagPayload.TryParse("action=peace;move=1;target=k", out _), "a numeric move is rejected");
    }

    private static void ReopenedRoundStatus()
    {
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus("", "continue", 1, 1) == WorldDiplomacyClosedRoundStatus.Resolved,
            "a reopened round (continue) still receives a terminal status on close");
        Test.True(WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus("", "resolved", 0, 0) == WorldDiplomacyClosedRoundStatus.KeepCurrent,
            "an already terminal status is kept");
    }

    private static void SignalRetention()
    {
        var signals = Enumerable.Range(1, 5).Select(day => new WorldDiplomacyPolicySignal { SignalKey = "s" + day, PublishedDay = day }).ToList();
        var kept = WorldDiplomacyRoundLifecycleRules.SelectRetainedPolicySignals(signals, 5, 30, 2);
        Test.True(kept.Count == 2 && kept[0].SignalKey == "s4" && kept[1].SignalKey == "s5",
            "over the cap the newest signals are retained, still in publication order");
    }

    private static void SignalContextDedupe()
    {
        var round = new WorldDiplomacyRound { RoundId = "r", ExternalOpeningContext = "mentions sig_x in prose" };
        var signal = new WorldDiplomacyPolicySignal { SignalKey = "sig_x", PolicyId = "p", IssuerKingdomId = "a", TargetKingdomId = "b" };
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(round, signal);
        string context = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(signal);
        Test.True(string.IsNullOrWhiteSpace(context) || round.ExternalOpeningContext.Contains(context),
            "prose that merely contains the key does not suppress the signal's context");
        string once = round.ExternalOpeningContext;
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(round, signal);
        Test.True(round.ExternalOpeningContext == once, "re-attaching the same key adds nothing");
    }

    private static void SurrogateLimit()
    {
        string text = new string('a', 9) + "😀";
        Test.True(WorldDiplomacyTextRules.Limit(text, 10).Length == 9, "a cut inside a surrogate pair drops the lone high surrogate");
        Test.True(WorldDiplomacyTextRules.Limit(text, 11) == text, "text within the limit is unchanged");
    }
}
