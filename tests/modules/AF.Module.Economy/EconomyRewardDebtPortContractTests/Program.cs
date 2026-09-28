using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

static void AssertTrue(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

AssertTrue((int)EconomyRewardDebtReplayStatus.Applied == 0
    && (int)EconomyRewardDebtReplayStatus.NoApplicableAction == 1
    && (int)EconomyRewardDebtReplayStatus.RejectedByCapability == 2
    && (int)EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation == 3
    && (int)EconomyRewardDebtReplayStatus.Failed == 4
    && (int)EconomyRewardDebtReplayStatus.PartiallyApplied == 5
    && (int)EconomyRewardDebtReplayStatus.UnknownAfterStart == 6,
    "Economy replay status numeric compatibility changed");
AssertTrue((int)ActionExecutionEffectState.NoConfirmedEffect == 0
    && (int)ActionExecutionEffectState.ConfirmedEffect == 1
    && (int)ActionExecutionEffectState.UnknownAfterStart == 2,
    "Action effect state numeric compatibility changed");

static GameInteractionSnapshot Snapshot(string subject = "npc-1")
{
    InteractionIdentity identity = new InteractionIdentity("economy-session", InteractionChannel.NativeConversation, subject);
    TraceContext trace = new TraceContext("economy-trace", 4, 9, "single-player", "1.4");
    return new GameInteractionSnapshot(identity, trace, "player input", "town-1", 12, 8, Array.Empty<InteractionCandidate>(), Array.Empty<string>(), new Dictionary<string, string>());
}

static EconomyRewardDebtReplayPlan Plan(bool withAction = true, params string[] exclusions)
{
    List<EconomyRewardDebtAction> actions = withAction
        ? new List<EconomyRewardDebtAction>
        {
            new EconomyRewardDebtAction(
                EconomyRewardDebtActionKind.GiveGold,
                "ACTION:GIVE_GOLD",
                "npc-1",
                "GOLD",
                "25",
                "25",
                "",
                "",
                "",
                EconomyRewardDebtCapabilityIds.GiveGold)
        }
        : new List<EconomyRewardDebtAction>();
    return new EconomyRewardDebtReplayPlan(actions, exclusions);
}

EconomyRewardDebtReplayResult Applied() => new EconomyRewardDebtReplayResult(
    EconomyRewardDebtReplayStatus.Applied, 1,
    new[] { new FactRecord("economy.reward", "npc-1", "gold applied") }, "");

LegacyActionTagParser parser = new LegacyActionTagParser();
ActionPlan parsedActionPlan = parser.Parse(
    "[AD:120:30:P:late payment]",
    new PostprocessContext(
        new[] { "economy" },
        new[] { "AD" },
        new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.DebtCreate })));
EconomyRewardDebtReplayPlan parsedDebtPlan = new LegacyEconomyRewardDebtAdapter().Plan(
    parsedActionPlan,
    new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.DebtCreate }));
AssertTrue(parsedDebtPlan.Actions.Count == 1
    && parsedDebtPlan.Actions[0].DueDaysToken == "30"
    && parsedDebtPlan.Actions[0].NoteToken == "late payment",
    "debt due days/note were not preserved from the legacy tag");
ActionPlan singleArgumentGold = parser.Parse(
    "[ACTION:GIVE_GOLD:75]",
    new PostprocessContext(
        new[] { "economy" },
        new[] { "ACTION:GIVE_GOLD" },
        new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.GiveGold })));
EconomyRewardDebtReplayPlan singleArgumentGoldPlan = new LegacyEconomyRewardDebtAdapter().Plan(
    singleArgumentGold,
    new CapabilitySet(new[] { EconomyRewardDebtCapabilityIds.GiveGold }));
AssertTrue(singleArgumentGoldPlan.Actions.Count == 1
    && singleArgumentGoldPlan.Actions[0].AmountToken == "75",
    "single-argument legacy GIVE_GOLD was not preserved");

int callbackCalls = 0;
LegacyEconomyRewardDebtMainThreadPort port = new LegacyEconomyRewardDebtMainThreadPort(
    () => true,
    snapshot => snapshot.Identity.SubjectId == "npc-1",
    (plan, snapshot) => { callbackCalls++; return Applied(); });
EconomyRewardDebtReplayResult applied = port.Replay(Plan(), Snapshot());
AssertTrue(applied.Status == EconomyRewardDebtReplayStatus.Applied && applied.AppliedCount == 1, "valid replay was not applied");
AssertTrue(callbackCalls == 1, "valid replay callback count mismatch");

LegacyEconomyRewardDebtMainThreadPort offThread = new LegacyEconomyRewardDebtMainThreadPort(
    () => false, _ => true, (plan, snapshot) => { callbackCalls++; return Applied(); });
EconomyRewardDebtReplayResult offThreadResult = offThread.Replay(Plan(), Snapshot());
AssertTrue(offThreadResult.Status == EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation && offThreadResult.ErrorCode == "economy.not_main_thread", "off-main replay was not rejected");

LegacyEconomyRewardDebtMainThreadPort staleTarget = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => false, (plan, snapshot) => { callbackCalls++; return Applied(); });
EconomyRewardDebtReplayResult staleResult = staleTarget.Replay(Plan(), Snapshot());
AssertTrue(staleResult.Status == EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation && staleResult.ErrorCode == "economy.target_stale_or_changed", "stale target was not rejected");

EconomyRewardDebtReplayResult excludedResult = port.Replay(Plan(true, "economy.capability_missing:economy.reward.give_gold"), Snapshot());
AssertTrue(excludedResult.Status == EconomyRewardDebtReplayStatus.RejectedByCapability, "capability exclusion did not fail closed");

EconomyRewardDebtReplayResult nonEconomyExcluded = port.Replay(Plan(true, "economy.action_not_applicable:ACTION:DUEL"), Snapshot());
AssertTrue(nonEconomyExcluded.Status == EconomyRewardDebtReplayStatus.Applied, "non-economy exclusion blocked applicable action");

EconomyRewardDebtReplayResult noAction = port.Replay(Plan(false, "economy.action_not_applicable:ACTION:DUEL"), Snapshot());
AssertTrue(noAction.Status == EconomyRewardDebtReplayStatus.NoApplicableAction, "empty applicable plan status mismatch");

LegacyEconomyRewardDebtMainThreadPort throwing = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => throw new InvalidOperationException("fixture"));
EconomyRewardDebtReplayResult thrown = throwing.Replay(Plan(), Snapshot());
AssertTrue(thrown.Status.ToString() == "UnknownAfterStart"
    && thrown.ErrorCode == "economy.domain_replay_exception",
    "domain exception was not isolated as unknown-after-start");

LegacyEconomyRewardDebtMainThreadPort nullResult = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => null);
EconomyRewardDebtReplayResult nullReplay = nullResult.Replay(Plan(), Snapshot());
AssertTrue(nullReplay.Status.ToString() == "UnknownAfterStart"
    && nullReplay.ErrorCode == "economy.domain_replay_null_result",
    "null result after owner start was not isolated as unknown");

LegacyEconomyRewardDebtMainThreadPort badCount = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(EconomyRewardDebtReplayStatus.Applied, 2, Array.Empty<FactRecord>(), ""));
EconomyRewardDebtReplayResult countResult = badCount.Replay(Plan(), Snapshot());
AssertTrue(countResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && countResult.AppliedCount == 0
    && countResult.ConfirmedFacts.Count == 0
    && countResult.ErrorCode == "economy.applied_count_invalid",
    "malformed post-owner count was not isolated as unknown");

LegacyEconomyRewardDebtMainThreadPort appliedWithoutCount = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.Applied, 0, Array.Empty<FactRecord>(), ""));
EconomyRewardDebtReplayResult appliedWithoutCountResult = appliedWithoutCount.Replay(Plan(), Snapshot());
AssertTrue(appliedWithoutCountResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && appliedWithoutCountResult.AppliedCount == 0
    && appliedWithoutCountResult.ConfirmedFacts.Count == 0
    && appliedWithoutCountResult.ErrorCode == "economy.applied_without_effect",
    "Applied+0 was trusted after owner start");

LegacyEconomyRewardDebtMainThreadPort ordinaryFailed = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.Failed, 0, Array.Empty<FactRecord>(), "economy.no_action_applied"));
EconomyRewardDebtReplayResult ordinaryFailedResult = ordinaryFailed.Replay(Plan(), Snapshot());
AssertTrue(ordinaryFailedResult.Status == EconomyRewardDebtReplayStatus.Failed
    && ordinaryFailedResult.ErrorCode == "economy.no_action_applied",
    "consistent no-effect owner failure was changed to unknown");

EconomyRewardDebtReplayPlan twoActionPlan = new EconomyRewardDebtReplayPlan(
    new[] { Plan().Actions[0], Plan().Actions[0] },
    Array.Empty<string>());
LegacyEconomyRewardDebtMainThreadPort legacyShortApplied = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => Applied());
EconomyRewardDebtReplayResult normalizedPartial = legacyShortApplied.Replay(twoActionPlan, Snapshot());
AssertTrue(normalizedPartial.Status == EconomyRewardDebtReplayStatus.PartiallyApplied
    && normalizedPartial.AppliedCount == 1
    && normalizedPartial.ConfirmedFacts.Count == 1,
    "legacy short Applied result was not normalized to structured partial");

LegacyEconomyRewardDebtMainThreadPort explicitPartial = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.PartiallyApplied, 1,
        new[] { new FactRecord("economy.reward", "npc-1", "first applied") },
        "economy.partial_replay"));
EconomyRewardDebtReplayResult partialResult = explicitPartial.Replay(twoActionPlan, Snapshot());
AssertTrue(partialResult.Status == EconomyRewardDebtReplayStatus.PartiallyApplied
    && partialResult.ErrorCode == "economy.partial_replay",
    "explicit structured partial was not preserved");

LegacyEconomyRewardDebtMainThreadPort invalidPartial = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.PartiallyApplied, 1, Array.Empty<FactRecord>(), ""));
EconomyRewardDebtReplayResult invalidPartialResult = invalidPartial.Replay(Plan(), Snapshot());
AssertTrue(invalidPartialResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && invalidPartialResult.AppliedCount == 0
    && invalidPartialResult.ConfirmedFacts.Count == 0
    && invalidPartialResult.ErrorCode == "economy.partial_count_invalid",
    "invalid full-count partial retained an untrusted owner receipt");

LegacyEconomyRewardDebtMainThreadPort failedWithCount = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.Failed, 1,
        new[] { new FactRecord("economy.unknown", "npc-1", "untrusted") },
        "economy.unknown_after_start"));
EconomyRewardDebtReplayResult failedWithCountResult = failedWithCount.Replay(twoActionPlan, Snapshot());
AssertTrue(failedWithCountResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && failedWithCountResult.AppliedCount == 0
    && failedWithCountResult.ConfirmedFacts.Count == 0
    && failedWithCountResult.ErrorCode == "economy.owner_receipt_inconsistent",
    "Failed+count was trusted after the owner returned an inconsistent receipt");

LegacyEconomyRewardDebtMainThreadPort unknownWithUntrustedFact = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.UnknownAfterStart, 0,
        new[] { new FactRecord("economy.unknown", "npc-1", "untrusted") },
        "economy.unknown_after_start"));
EconomyRewardDebtReplayResult unknownWithUntrustedFactResult = unknownWithUntrustedFact.Replay(Plan(), Snapshot());
AssertTrue(unknownWithUntrustedFactResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && unknownWithUntrustedFactResult.AppliedCount == 0
    && unknownWithUntrustedFactResult.ConfirmedFacts.Count == 0
    && unknownWithUntrustedFactResult.ErrorCode == "economy.owner_receipt_inconsistent",
    "count-zero unknown retained untrusted success facts");

LegacyEconomyRewardDebtMainThreadPort unknownWithOverflow = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.UnknownAfterStart, 2,
        new[] { new FactRecord("economy.confirmed", "npc-1", "untrusted overflow") },
        "economy.unknown_after_start"));
EconomyRewardDebtReplayResult unknownWithOverflowResult = unknownWithOverflow.Replay(Plan(), Snapshot());
AssertTrue(unknownWithOverflowResult.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart
    && unknownWithOverflowResult.AppliedCount == 0
    && unknownWithOverflowResult.ConfirmedFacts.Count == 0
    && unknownWithOverflowResult.ErrorCode == "economy.applied_count_invalid",
    "overflowing unknown receipt retained untrusted count/facts");

Console.WriteLine("PASS economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 unknownIsolation=8 noEffectFailure=1 countValidation=1 partialNormalization=4 enumCompatibility=1 debtMetadata=1 singleArgumentGold=1");
