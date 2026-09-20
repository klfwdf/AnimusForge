using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

internal static class LegacyChannelActionCommitterTests
{
    internal static void Run()
    {
        string raw = "reply [ACTION:MOOD:happy] [ACTION:GIVE_GOLD:25]";
        string[] expectedTags = { "ACTION:MOOD", "ACTION:GIVE_GOLD" };
        foreach (InteractionChannel channel in new[]
        {
            InteractionChannel.NativeConversation,
            InteractionChannel.SceneShout,
            InteractionChannel.Courier
        })
        {
            GameInteractionSnapshot snapshot = Snapshot(channel);
            var owner = new ExactOwner(InteractionStatus.Executed);
            LegacyChannelActionCommitResult committed = new LegacyChannelActionCommitter(
                expectedTags).Commit(raw, snapshot, owner);
            Require(committed.HasActions
                && committed.ActionPlan.Actions.Select(action => action.Tag).SequenceEqual(expectedTags),
                channel + " did not produce the canonical ordered ActionPlan.");
            Require(committed.Execution.Status == InteractionStatus.Executed
                && committed.Execution.ActionsExecuted
                && owner.Calls == 1,
                channel + " did not execute exactly once through the shared boundary.");
            Require(owner.RequestId == InteractionResultCommitter.BuildCanonicalRequestId(snapshot)
                && owner.ActionFingerprint == InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(
                    committed.ActionPlan),
                channel + " did not bind the canonical request/action identity.");
        }

        var blockedOwner = new ExactOwner(InteractionStatus.Executed);
        LegacyChannelActionCommitResult blocked = new LegacyChannelActionCommitter(
            new[] { "ACTION:MOOD" }).Commit(
                "[ACTION:MOOD:happy][ACTION:GIVE_GOLD:1]",
                Snapshot(InteractionChannel.NativeConversation),
                blockedOwner);
        Require(blocked.Execution.Status == InteractionStatus.RejectedByValidation
            && blocked.Execution.ErrorCode == "action_protocol_not_allowed"
            && blockedOwner.Calls == 0,
            "A disallowed protocol tag crossed the shared action boundary.");

        var overflowOwner = new ExactOwner(InteractionStatus.Executed);
        string overflow = string.Concat(Enumerable.Repeat("[ACTION:MOOD:happy]", 65));
        LegacyChannelActionCommitResult overflowed = new LegacyChannelActionCommitter(
            new[] { "ACTION:MOOD" }).Commit(
                overflow,
                Snapshot(InteractionChannel.SceneShout),
                overflowOwner);
        Require(overflowed.Execution.ErrorCode == "action_limit_exceeded"
            && overflowOwner.Calls == 0,
            "An over-limit raw action stream reached the channel owner.");

        var throwingOwner = new ExactOwner(InteractionStatus.Executed, shouldThrow: true);
        LegacyChannelActionCommitResult unknown = new LegacyChannelActionCommitter(
            new[] { "ACTION:MOOD" }).Commit(
                "[ACTION:MOOD:happy]",
                Snapshot(InteractionChannel.Courier),
                throwingOwner);
        Require(unknown.Execution.Status == InteractionStatus.NonRetryableFailure
            && unknown.Execution.EffectState == ActionExecutionEffectState.UnknownAfterStart
            && !unknown.Execution.ActionsExecuted
            && throwingOwner.Calls == 1,
            "A throwing owner was not retained as terminal unknown.");

        var noActionOwner = new ExactOwner(InteractionStatus.Executed);
        LegacyChannelActionCommitResult noActions = new LegacyChannelActionCommitter(
            new[] { "ACTION:MOOD" }).Commit(
                "ordinary visible reply",
                Snapshot(InteractionChannel.NativeConversation),
                noActionOwner);
        Require(!noActions.HasActions
            && noActions.Execution.Status == InteractionStatus.Succeeded
            && noActionOwner.Calls == 0,
            "Action-free text invoked a gameplay owner.");

        Console.WriteLine(
            "PASS legacyChannelActionCommitter cases=8 channels=3 canonicalPlan=1 exactBinding=1 disallowed=1 overflow=1 unknown=1 noAction=1");
    }

    private static GameInteractionSnapshot Snapshot(InteractionChannel channel)
        => new GameInteractionSnapshot(
            new InteractionIdentity("session-" + channel, channel, "hero-1"),
            new TraceContext("trace-" + channel, 4, 9, "test", "1.4"),
            "player input",
            "town-1",
            12,
            8,
            Array.Empty<InteractionCandidate>(),
            new[] { "hero-1" },
            new Dictionary<string, string>());

    private static void Require(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ExactOwner : IRequestBoundActionPlanExecutor
    {
        private readonly InteractionStatus _status;
        private readonly bool _shouldThrow;

        internal ExactOwner(InteractionStatus status, bool shouldThrow = false)
        {
            _status = status;
            _shouldThrow = shouldThrow;
        }

        internal int Calls { get; private set; }
        internal string RequestId { get; private set; }
        internal string ActionFingerprint { get; private set; }

        public InteractionStatus ValidateAndExecute(
            ActionPlan actionPlan,
            GameInteractionSnapshot currentSnapshot)
            => throw new InvalidOperationException("Unbound action execution must not be used.");

        InteractionStatus IRequestBoundActionPlanExecutor.ValidateAndExecute(
            ActionPlan actionPlan,
            GameInteractionSnapshot currentSnapshot,
            string requestId,
            string actionFingerprint)
        {
            Calls++;
            RequestId = requestId;
            ActionFingerprint = actionFingerprint;
            if (_shouldThrow)
            {
                throw new InvalidOperationException("simulated owner failure after start");
            }
            return _status;
        }
    }
}
