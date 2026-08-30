using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

static void AssertTrue(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

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
AssertTrue(thrown.Status == EconomyRewardDebtReplayStatus.Failed && thrown.ErrorCode == "economy.domain_replay_exception", "domain exception was not isolated");

LegacyEconomyRewardDebtMainThreadPort badCount = new LegacyEconomyRewardDebtMainThreadPort(
    () => true, _ => true, (plan, snapshot) => new EconomyRewardDebtReplayResult(EconomyRewardDebtReplayStatus.Applied, 2, Array.Empty<FactRecord>(), ""));
EconomyRewardDebtReplayResult countResult = badCount.Replay(Plan(), Snapshot());
AssertTrue(countResult.Status == EconomyRewardDebtReplayStatus.Failed && countResult.ErrorCode == "economy.applied_count_invalid", "invalid applied count was not rejected");

Console.WriteLine("PASS economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 exceptionIsolation=1 countValidation=1 debtMetadata=1 singleArgumentGold=1");
