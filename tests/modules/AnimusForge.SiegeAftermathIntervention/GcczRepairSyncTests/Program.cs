using AnimusForge.SiegeAftermathIntervention;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition) throw new Exception("FAIL: " + name);
    }

    private static void Main()
    {
        // Reuse committed standalone tests against the core actually compiled by AF.
        SiegeNativeMovementOrdersTests.Run(Check);
        SiegeStuckRecoveryTests.Run(Check);

        Check(SiegeCastlePrisonerDispositionProfile.ResolveStageableRecruitCount(30, 12, 0) == 12, "capacity trims the first group");
        Check(SiegeCastlePrisonerDispositionProfile.ResolveStageableRecruitCount(15, 20, 5) == 15, "capacity subtracts previously promised groups");
        Check(SiegeCastlePrisonerDispositionProfile.ResolveStageableRecruitCount(15, 20, 20) == 0, "full party stages no new recruits");
        Check(SiegeCastlePrisonerDispositionProfile.ResolveStageableRecruitCount(-1, 20, 0) == 0, "negative recruitment is rejected");
        Check(SiegeCastlePrisonerDispositionProfile.BuildRecruitCapacityTrimMessage(30, 12, 12).Contains("未分配"), "unselected remainder remains available");

        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(0, 90, 40, 40) == 90, "full recruitment keeps full morale penalty");
        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(0, 90, 40, 4) == 9, "partial recruitment uses actual joined count");
        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(0, 90, 40, 0) == 0, "no joined recruits incur no recruitment penalty");
        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(30, 90, 40, 0) == 30, "other unrest survives failed recruitment");
        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(30, 60, 10, 10) == 60, "different concerns use the existing strongest-penalty rule");
        Check(SiegeCastleSoldierReactionProfile.ResolveExitMoralePenalty(0, 90, 3, 1) == 30, "fractional morale rounds up");
        Check(!SiegeCastleSoldierReactionProfile.ShouldReopenAppeasement(true, 90, 30), "weaker concerns preserve previous appeasement");
        Check(SiegeCastleSoldierReactionProfile.ShouldReopenAppeasement(true, 60, 90), "stronger concerns reopen appeasement");

        var ledger = new TownOperationLedger();
        Check(ledger.BeginAtrocity(TownOperationKind.Colonization) && ledger.SealVictimSnapshot(), "create existing sealed colonization ledger");
        var state = new TownColonizationStateMachine();
        Check(!state.CanRequest("", "culture"), "empty settlement is not accepted");
        Check(state.Request("town", "culture", ledger.Snapshot()), "first culture request starts existing owner");
        Check(state.CanRequest("town", "culture"), "same pending request is idempotent");
        Check(!state.CanRequest("town", "other"), "different culture cannot overwrite pending work");
        Check(state.PrepareSceneExitCommit(), "existing scene exit seals the request");
        Check(!state.CanRequest("town", "culture"), "ready operation cannot be restarted");
        Check(state.TryCommit() && !state.TryCommit(), "culture commit remains single use");
        Check(!state.CanRequest("town", "culture"), "completed operation cannot be replayed");

        var budget = new SiegeNpcResponseEventBudget();
        budget.BeginScene();
        Check(budget.TryClaim("event", "a", SiegeNpcResponseEventOrigin.PlayerUtterance, true, 2, 10, 0).Allowed, "unlimited event admits first speaker");
        Check(budget.TryClaim("event", "b", SiegeNpcResponseEventOrigin.PlayerUtterance, false, 2, 10, 0).Allowed, "new finite setting admits only remaining allowance");
        Check(!budget.TryClaim("event", "c", SiegeNpcResponseEventOrigin.PlayerUtterance, false, 2, 10, 0).Allowed, "lowered finite setting stops further speakers");
        Console.WriteLine("PASS GCCZ repair sync assertions=" + _checks);
    }
}
