using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class KingdomOwnerReplay
{
    private const BindingFlags M = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static void Run(Assembly assembly)
    {
        Type policy = assembly.GetType("AnimusForge.KingdomStabilityPolicy", true);
        Type state = assembly.GetType("AnimusForge.KingdomStabilityOwner", true);
        Type cursor = assembly.GetType("AnimusForge.KingdomMaintenanceOwner`1", true).MakeGenericType(typeof(string));
        object Call(Type type, object owner, string method, params object[] args) => type.GetMethod(method, M).Invoke(owner, args);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Kingdom: " + label); }
        int[] bounds = { -1, 0, 9, 10, 24, 25, 39, 40, 59, 60, 74, 75, 89, 90, 100, 101 };
        int[] tiers = { 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 6 };
        int[][] loyalty = { new[]{-9,-6,-3,-2,-1,0}, new[]{-6,-4,-2,-1,0,0}, new[]{-3,-2,-1,0,0,0}, new[]{0,0,0,0,0,0}, new[]{3,2,1,0,0,0}, new[]{6,4,2,1,0,0}, new[]{9,6,3,2,1,0} };
        int[] balance = { 5,3,0,0,0,-3,-5 };
        float[] chances = { .25f,.05f,.005f,0,0,0,0 };
        for (int index = 0; index < bounds.Length; index++)
        {
            int value = bounds[index], tier = tiers[index];
            Check(Convert.ToInt32(Call(policy, null, "GetKingdomStabilityTier", value)) == tier, "tier boundary " + value);
            Check((int)Call(policy, null, "GetKingdomStabilityWeeklyBalancingDelta", value) == balance[tier], "weekly balancing");
            Check((float)Call(policy, null, "GetKingdomRebellionWeeklyChance", value) == chances[tier], "rebellion chance");
            for (int clans = -1; clans <= 6; clans++)
                Check((int)Call(policy, null, "GetLowClanCountRoyalDomainLoyaltyAdjustment", value, clans) == loyalty[tier][Math.Max(0, Math.Min(5, clans))], "royal loyalty boundaries");
        }
        Check((int)Call(policy, null, "GetKingdomStabilityRelationTargetOffset", 49) == 0
            && (int)Call(policy, null, "GetKingdomStabilityRelationTargetOffset", 1) == -24
            && (int)Call(policy, null, "GetKingdomStabilityRelationTargetOffset", int.MinValue) == -25
            && (int)Call(policy, null, "GetKingdomStabilityRelationTargetOffset", int.MaxValue) == 25, "integer truncation and clamping");
        object owner = Activator.CreateInstance(state, true);
        Check((int)Call(state, owner, "Get", "missing") == 50, "default stability");
        Call(state, owner, "Set", " K ", 1000);
        Check((int)Call(state, owner, "Get", "k") == 100, "normalized saved identity and clamp");
        Call(state, owner, "Set", "k", -100);
        Check((int)Call(state, owner, "Get", "K") == 0, "minimum");
        var offsets = (Dictionary<string, int>)state.GetField("RelationOffsets", M).GetValue(owner);
        var relations = new Dictionary<string, int> { ["k|a|b"] = 95, ["k|a|c"] = -95, ["other|a|b"] = 42 };
        offsets["other|a|b"] = 9;
        Func<string, int, int?> apply = (key, desired) =>
        {
            if (!relations.TryGetValue(key, out int current)) return null;
            offsets.TryGetValue(key, out int previous);
            object[] args = { current, previous, desired, 0 };
            relations[key] = (int)Call(state, null, "ResolveRelation", args);
            return (int)args[3];
        };
        var wanted = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase) { ["k|a|b"] = 25, ["k|a|c"] = -25 };
        Call(state, owner, "Reconcile", "k", wanted, apply);
        Check(relations["k|a|b"] == 100 && offsets["k|a|b"] == 5 && relations["k|a|c"] == -100 && offsets["k|a|c"] == -5, "saturation books actual applied amount");
        Call(state, owner, "Reconcile", "k", wanted, apply);
        Check(relations["k|a|b"] == 100 && offsets["k|a|b"] == 5, "duplicate relation event idempotent");
        relations["k|a|b"] -= 10;
        wanted.Clear();
        Call(state, owner, "Reconcile", "k", wanted, apply);
        Check(relations["k|a|b"] == 85 && relations["k|a|c"] == -95 && offsets.Count == 1 && offsets["other|a|b"] == 9, "revoke preserves unrelated changes and kingdom");
        offsets["k|gone"] = 4; wanted["k|new-missing"] = 7;
        Call(state, owner, "Reconcile", "k", wanted, apply);
        Check(offsets.Count == 1, "invalid old pair removed and unavailable new pair skipped");
        int stability = 50, writes = 0;
        Action<int> write = value => { stability = value; writes++; };
        Call(state, owner, "ApplyWeeklyDelta", "weekly:k:1", stability, 5, write);
        Call(state, owner, "ApplyWeeklyDelta", "WEEKLY:K:1", stability, 5, write);
        Check(stability == 55 && writes == 1, "weekly delta once by stable event identity");
        stability += 10;
        Call(state, owner, "ApplyWeeklyDelta", "weekly:k:1", stability, -3, write);
        Check(stability == 57 && writes == 2, "replacement retracts only original delta");
        object maintenance = Activator.CreateInstance(cursor, true);
        int captures = 0;
        Func<List<string>> capture = () => { captures++; return new List<string>{"a","b"}; };
        Call(cursor, maintenance, "BeginWeek", 7, capture);
        object[] take = { null };
        Check((bool)Call(cursor, maintenance, "TryTake", take) && (string)take[0] == "a", "first kingdom");
        Call(cursor, maintenance, "BeginWeek", 7, capture);
        Check(captures == 1 && (bool)Call(cursor, maintenance, "TryTake", take) && (string)take[0] == "b", "same week resumes without recapture");
        Check(!(bool)Call(cursor, maintenance, "TryTake", take) && (bool)cursor.GetProperty("Complete", M).GetValue(maintenance), "week finite completion");
        Call(cursor, maintenance, "ResetWeek"); Call(cursor, maintenance, "BeginWeek", 8, capture);
        Check(captures == 2 && (bool)Call(cursor, maintenance, "TryTake", take) && (string)take[0] == "a", "load reset discards cursor");
        var visited = new List<int>(); Action<int> visit = visited.Add;
        Check(!(bool)Call(cursor, maintenance, "AdvanceRelations", 3, visit), "one relation kingdom per slice");
        try { Call(cursor, maintenance, "AdvanceRelations", 3, (Action<int>)(_ => throw new InvalidOperationException("fixture"))); }
        catch (TargetInvocationException) { }
        Check(!(bool)Call(cursor, maintenance, "AdvanceRelations", 3, visit) && visited.SequenceEqual(new[]{0,1}), "failed mutation does not skip relation cursor");
        Check((bool)Call(cursor, maintenance, "AdvanceRelations", 1, visit) && visited.Last() == 0, "shrinking kingdom list revalidates cursor");
        Type flowType = assembly.GetType("AnimusForge.AutomaticKingdomRebellionOwner`1", true).MakeGenericType(typeof(string));
        object flow = Activator.CreateInstance(flowType, true);
        bool Active() => (bool)flowType.GetProperty("FlowActive", M).GetValue(flow);
        Call(flowType, flow, "Enqueue", "first"); Call(flowType, flow, "Enqueue", "second");
        Check((bool)Call(flowType, flow, "ActivateIfQueued") && Active(), "rebellion queue blocks weekly reports");
        object[] next = { null };
        Check((bool)Call(flowType, flow, "TryDequeue", next) && (string)next[0] == "first", "rebellion FIFO");
        long firstRequest = (long)Call(flowType, flow, "BeginNaming");
        Check(!(bool)Call(flowType, flow, "TryDequeue", next), "naming blocks next rebellion");
        Check((bool)Call(flowType, flow, "CompleteNaming", firstRequest, "first")
            && !(bool)Call(flowType, flow, "CompleteNaming", firstRequest, "duplicate"), "naming result once");
        Check(!(bool)Call(flowType, flow, "TryDequeue", next), "ready result blocks next rebellion until commit");
        Check((bool)Call(flowType, flow, "TryTakeReady", next) && (string)next[0] == "first"
            && !(bool)Call(flowType, flow, "TryTakeReady", next) && Active(), "completion waits for explicit flow continuation");
        Check((bool)Call(flowType, flow, "TryDequeue", next) && (string)next[0] == "second", "continuation resumes next context");
        long canceledRequest = (long)Call(flowType, flow, "BeginNaming");
        Call(flowType, flow, "Cancel");
        Check(!Active() && !(bool)Call(flowType, flow, "CompleteNaming", canceledRequest, "late"), "MCM/load cancellation rejects late naming result");
        long replacementRequest = (long)Call(flowType, flow, "BeginNaming");
        Check(!(bool)Call(flowType, flow, "CompleteNaming", canceledRequest, "old")
            && (bool)Call(flowType, flow, "CompleteNaming", replacementRequest, "replacement"), "old naming cannot occupy replacement completion");
        Call(flowType, flow, "TryTakeReady", next);
        Check(!(bool)Call(flowType, flow, "TryDequeue", next) && !Active(), "empty flow releases weekly scheduling");
        Type host = assembly.GetType("AnimusForge.MyBehavior");
        if (host != null)
        {
            foreach (int value in bounds)
            {
                Check((int)Call(host, null, "GetKingdomStabilityWeeklyBalancingDelta", value) == (int)Call(policy, null, "GetKingdomStabilityWeeklyBalancingDelta", value), "real weekly adapter");
                Check((int)Call(host, null, "GetLowClanCountRoyalDomainLoyaltyAdjustment", value, 2) == (int)Call(policy, null, "GetLowClanCountRoyalDomainLoyaltyAdjustment", value, 2), "real loyalty/model adapter");
            }
            object behavior = RuntimeHelpers.GetUninitializedObject(host);
            host.GetField("_lastProcessedKingdomRebellionWeek", M).SetValue(behavior, 9);
            Check((bool)Call(host, behavior, "ProcessWeeklyKingdomRebellionsSlice", 9)
                && (bool)Call(host, behavior, "ProcessWeeklyKingdomRebellionsSlice", 0)
                && (int)host.GetField("_lastProcessedKingdomRebellionWeek", M).GetValue(behavior) == 9, "real saved completed week blocks duplicate/invalid work");
        }
        Console.WriteLine("PASS KingdomOwnerReplay stability/royal-loyalty/relation-saturation/duplicate/revoke/missing/weekly-delta/cursor/reset/saved-week/auto-flow/canceled-naming live=NOT_RUN");
    }
}
