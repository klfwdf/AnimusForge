using AnimusForge;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json;

internal static partial class EventConvergenceReplay
{
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r, WorldDiplomacyDocument d) FailedWarFixture(bool player)
    {
        var (h, o, r) = Fixture(player ? "p" : "a", "b");
        o.CurrentStorage.DiplomacyNotificationStateSchemaVersion = WorldDiplomacyNotificationStateMigration.CurrentSchemaVersion;
        var d = new WorldDiplomacyDocument {
            DocumentId = "failed-war", RoundId = r.RoundId, AuthorKingdomId = player ? "p" : "a", TargetKingdomId = "b",
            IsPlayerAuthored = player, IsReadyForPublication = true, PlayerLifecycleClassified = true, AnalysisStatus = "success",
            Intent = "declare_war", Commitment = "binding", Title = "Declaration", Body = "We declare war.", MechanicalResult = "Execution did not apply.",
            Actions = new() { new() { ActionId = "action_1", Intent = "declare_war", Commitment = "binding", TargetKingdomId = "b", ExecutionState = "not_applied" } }
        };
        o.AddDocument(d); o.HandleRoundDocumentProcessed(d);
        return (h, o, r, d);
    }

    private static string WarReceipt(WorldDiplomacyDocument d) => JsonConvert.SerializeObject(new {
        d.Title, d.Body, d.Actions, d.ChangedDiplomaticState, d.MechanicalResult, d.AnalysisStatus
    });

    internal static void WarEndingReconciliation()
    {
        bool previousChinese = PromptLanguage.IsChinese;
        try
        {
            foreach (bool chinese in new[] { true, false })
            {
                PromptLanguage.Install(() => chinese ? "zh" : "en", id => id == "zh");
                foreach (string scenario in new[] { "player_at_war", "npc_at_war", "player_failure", "rejected_legacy", "rejected_action", "confirmed_receipt" })
                {
                    bool player = scenario != "npc_at_war";
                    var (h, o, r, d) = FailedWarFixture(player);
                    h.WorldWar = scenario != "player_failure";
                    if (scenario is "rejected_legacy" or "rejected_action")
                    {
                        d.AnalysisStatus = "published_action_rejected";
                        if (scenario == "rejected_legacy") d.Actions.Clear();
                        else d.Actions[0].ExecutionState = "";
                    }
                    if (scenario == "confirmed_receipt")
                    { d.Actions[0].ExecutionState = "applied"; d.Actions[0].ChangedDiplomaticState = true; }
                    string receipt = WarReceipt(d);
                    string expected = scenario == "confirmed_receipt" ? "war_executed"
                        : h.WorldWar ? "war_already_active" : "player_war_not_executed";
                    // Recovery allowance exhaustion must not prevent receipt-only reconciliation.
                    r.EventConvergence.ExecutionRecoveryAttempts = 3;
                    Advance(o, r);
                    Test.True(r.EventConvergence.TechnicalFailure == "" && r.TechnicalHoldReason == ""
                        && r.EventConvergence.Result == expected && r.EventPhase == "closing", scenario + ": held event reaches normal ending");
                    Test.True(r.EventConvergence.ActualWar == (scenario == "confirmed_receipt") && WarReceipt(d) == receipt
                        && h.Effects == 0 && h.Standing.Writes == 0 && h.Draws == 0, scenario + ": preserves receipt without war, cost, prestige or route replay");
                    string explanation = DiplomacyPromptTexts.Get("EndingResult_" + expected, "");
                    Test.True(explanation.Length > 0 && r.EventOutcomeReason == explanation
                        && ((string)Call(o, "BuildEndingContext", r)).Contains(explanation), "real language reader reaches declaration and ending contexts");
                    if (expected == "war_already_active")
                        Test.True(explanation.Contains(chinese ? "不能宣称战争由该公文造成" : "Do not attribute the war to that document"), "CN/EN preserve non-attribution");
                    if (expected == "player_war_not_executed")
                        Test.True(explanation.Contains(chinese ? "AI 不得代发" : "AI must not issue it for the player"), "CN/EN preserve player decision authority");
                    var ending = o.CurrentStorage.Jobs.Single(x => x.ForcedIntent == "ended");
                    o.PrepareSharedRequest(ending);
                    Test.True(ending.UserPrompt.Contains(explanation), "actual ending writer receives the reconciled outcome explanation");
                    ReloadSilence(o); r = o.ResolveRound(r.RoundId); d = o.ResolveDocument(d.DocumentId);
                    Advance(o, r); Advance(o, r);
                    Test.True(r.EventConvergence.Result == expected && h.Effects == 0 && h.Standing.Writes == 0
                        && !o.CurrentStorage.Jobs.Any(x => !string.IsNullOrEmpty(x.EndingRecoveryDocumentId)), "load cannot revive failed-war execution");
                    if (scenario == "rejected_legacy") MaterialCountryThirdReplay.FinishStallBoundary(o, r);
                }
            }
        }
        finally { PromptLanguage.Install(() => previousChinese ? "zh" : "en", id => id == "zh"); }

        foreach (string guard in new[] { "uncertain", "unknown", "partial", "condition", "arrangement", "pending", "unpublished", "foreign_hold", "wrong_target", "unbound_legacy" })
        {
            var (h, o, r, d) = FailedWarFixture(true); h.WorldWar = true;
            if (guard == "uncertain") d.Actions[0].ExecutionState = "uncertain";
            if (guard == "unknown") d.Actions[0].ExecutionState = "";
            if (guard == "partial") d.Actions.Add(new() { Intent = "cancel_trade", ChangedDiplomaticState = true });
            if (guard == "condition") { d.Actions[0].ExecutionState = "deferred_conditions"; d.Actions[0].DependencyActionIds.Add(d.DocumentId + "#action_1"); }
            if (guard == "arrangement") d.DialogueArrangementId = "binding-agreement";
            if (guard == "pending") d.EventExecutionPending = true;
            if (guard == "unpublished") d.IsReadyForPublication = false;
            if (guard == "foreign_hold") r.TechnicalHoldReason = "independent_failure";
            if (guard == "wrong_target") d.Actions[0].TargetKingdomId = "c";
            if (guard == "unbound_legacy") d.Actions.Clear();
            string original = WarReceipt(d);
            Advance(o, r);
            Test.True(r.EventConvergence.TechnicalFailure != "" && !r.EventConvergence.ActualWar
                && h.Effects == 0 && h.Standing.Writes == 0 && WarReceipt(d) == original, "unsafe receipt stays held: " + guard);
        }

        // A formerly valid NPC retry may still be in flight when war starts elsewhere.
        {
            var (h, o, r, d) = FailedWarFixture(false);
            Advance(o, r);
            var retry = o.CurrentStorage.Jobs.Single(x => x.EndingRecoveryDocumentId == d.DocumentId);
            Test.True(o.RequestLeases.TryClaim(retry, 1, 512, 1000, false, out var lease), "claim retry transport before world changes");
            var lateResult = new LlmJobResult { Success = true, OwnershipEpoch = r.OwnershipEpoch,
                SchedulingOwner = r.SchedulingOwner, PlayerAuthorityRulerId = r.PlayerAuthorityRulerId,
                RoundConversationRevision = r.ConversationRevision };
            h.WorldWar = true; r.EventConvergence.NextExecutionRecoveryUtcTicks = 0;
            Advance(o, r);
            Test.True(!o.CurrentStorage.Jobs.Contains(retry) && r.EventConvergence.Result == "war_already_active"
                && !o.AdmitCompletion(retry, lateResult), "retired retry cannot accept a late model completion");
            Test.True(o.RequestLeases.TryRelease(lease.JobId, 1, lease.Attempt) && h.Effects == 0 && h.Standing.Writes == 0,
                "late transport releases its original lease without executing or settling again");
        }
    }
}
