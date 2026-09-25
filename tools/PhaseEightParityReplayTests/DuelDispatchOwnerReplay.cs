using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal static class DuelDispatchOwnerReplay
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly assembly)
    {
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Duel dispatch owner: " + label);
        }

        Type bridge = assembly.GetType("AnimusForge.Refactor.Runtime.FeatureBridgeRuntime", true);
        FieldInfo enabledField = bridge.GetField("_enabledById", Static);
        bridge.GetMethod("IsEnabled", Static).Invoke(null, new object[] { "scene-duel" });
        object originalEnabled = enabledField.GetValue(null);
        var enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (object entry in (IEnumerable)originalEnabled)
        {
            Type pair = entry.GetType();
            enabled[(string)pair.GetProperty("Key").GetValue(entry)] = (bool)pair.GetProperty("Value").GetValue(entry);
        }
        enabled["scene-duel"] = true;
        enabledField.SetValue(null, enabled);
        try
        {
            Type behavior = assembly.GetType("AnimusForge.DuelBehavior", true);
            Type contextType = assembly.GetType("AnimusForge.Refactor.Runtime.DetachedDuelDispatchContext", true);
            Type channelType = assembly.GetType("AnimusForge.Refactor.Runtime.DuelOutcomeChannel", true);
            object owner = behavior.GetMethod("CreateDetachedDuelDispatchOwnerForExternal", Static).Invoke(null, null);
            long generation = (long)assembly.GetType("AnimusForge.SaveRuntimeGuard", true)
                .GetMethod("CaptureGeneration", Static).Invoke(null, null);

            object Context(string requestId, string fingerprint, long requestGeneration)
            {
                object[] values = {
                    requestId, "j13e1-trace", Enum.Parse(channelType, "NativeConversation"),
                    "j13e1-session", "j13e1-subject", requestGeneration, requestGeneration,
                    fingerprint, null, null
                };
                Check((bool)contextType.GetMethod("TryCreate", Static).Invoke(null, values),
                    "context rejected: " + values[9]);
                return values[8];
            }

            (bool accepted, bool dispatch, string error) Queue(object context)
            {
                object[] values = { context, false, null };
                bool accepted = (bool)owner.GetType().GetMethod("TryQueue", Instance).Invoke(owner, values);
                return (accepted, (bool)values[1], (string)values[2]);
            }

            string State(object context)
                => contextType.GetMethod("Snapshot", Instance).Invoke(context, null)
                    ?.GetType().GetProperty("State", Instance).GetValue(
                        contextType.GetMethod("Snapshot", Instance).Invoke(context, null)).ToString();

            MethodInfo ready = behavior.GetMethod("IsDetachedDuelDispatchReadyForDelayedHost", Static);
            enabled["scene-duel"] = false;
            object disabled = Context("j13e1-disabled", new string('E', 64), generation);
            var disabledQueue = Queue(disabled);
            Check(!disabledQueue.accepted && !disabledQueue.dispatch
                && disabledQueue.error == "duel.bridge_disabled" && State(disabled) == "Rejected",
                "disabled SceneDuel bridge admitted an exact request");
            enabled["scene-duel"] = true;
            object first = Context("j13e1-first", new string('A', 64), generation);
            var queued = Queue(first);
            Check(queued.accepted && queued.dispatch && queued.error == string.Empty && State(first) == "Queued",
                "new exact request did not queue once");
            Check(!(bool)ready.Invoke(null, new[] { first }), "delayed host started before acceptance");
            contextType.GetMethod("MarkHostAccepted", Instance).Invoke(first, null);
            Check((bool)ready.Invoke(null, new[] { first }), "accepted queued request is not ready");

            object duplicate = Context("j13e1-first", new string('A', 64), generation);
            var replay = Queue(duplicate);
            Check(replay.accepted && !replay.dispatch && State(duplicate) == "Queued",
                "duplicate exact request redispatched");
            object conflict = Context("j13e1-first", new string('B', 64), generation);
            var conflicting = Queue(conflict);
            Check(!conflicting.accepted && !conflicting.dispatch && State(conflict) == "Rejected",
                "same DuelId with different action fingerprint was accepted");
            object stale = Context("j13e1-stale", new string('C', 64), generation + 1);
            var staleQueue = Queue(stale);
            Check(!staleQueue.accepted && !staleQueue.dispatch && staleQueue.error == "duel.dispatch_stale_generation",
                "stale save/runtime generation was accepted");

            contextType.GetMethod("MarkSideEffectBoundaryCrossed", Instance).Invoke(first, null);
            behavior.GetMethod("AbortDetachedDuelDispatch", Static).Invoke(null, new[] { first, "j13e1_abort" });
            Check(State(first) == "UnknownAfterStart" && !(bool)ready.Invoke(null, new[] { first }),
                "crossed side effect boundary did not terminalize as unknown");
            object cancelled = Context("j13e1-cancelled", new string('F', 64), generation);
            Check(Queue(cancelled).dispatch, "pre-start cancellation fixture was not queued");
            owner.GetType().GetMethod("Cancel", Instance).Invoke(owner, new[] { cancelled, "j13e1_cancelled" });
            Check(State(cancelled) == "Rejected" && !(bool)ready.Invoke(null, new[] { cancelled }),
                "pre-start cancellation can still start delayed Duel");
            Check(!Queue(Context("j13e1-cancelled", new string('F', 64), generation)).dispatch,
                "cancelled request redispatched on reentry");
            object rejected = Context("j13e1-rejected", new string('D', 64), generation);
            owner.GetType().GetMethod("Reject", Instance).Invoke(owner, new[] { rejected, "j13e1_rejected" });
            Check(State(rejected) == "Rejected" && !(bool)ready.Invoke(null, new[] { rejected }),
                "explicit rejection can still start delayed Duel");

            Console.WriteLine("PASS DuelDispatchOwnerReplay current-DLL exact/duplicate/conflict/stale/disabled/ready/unknown/cancel/reentry/reject; Mission/live=NOT_RUN");
        }
        finally
        {
            enabledField.SetValue(null, originalEnabled);
        }
    }
}
