using System;
using System.Collections;
using System.Reflection;

internal static class ProactiveCandidateScanOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly)
    {
        Type host = assembly.GetType("AnimusForge.ProactiveNpcRequestBehavior", true);
        Type ownerType = host.GetNestedType("ProactiveCandidateScanOwner", BindingFlags.NonPublic);
        if (ownerType == null || host.GetField("_candidateScanOwner", Members)?.FieldType != ownerType
            || host.GetField("_candidateScan", Members) != null)
            throw new InvalidOperationException("Proactive scan: production host must use one scan owner");
        Type scanType = host.GetNestedType("ProactiveCandidateScanState", BindingFlags.NonPublic);
        Type candidateType = host.GetNestedType("ProactiveCandidate", BindingFlags.NonPublic);
        Type statsType = host.GetNestedType("CandidateScanStats", BindingFlags.NonPublic);
        object owner = Activator.CreateInstance(ownerType, true);
        object Call(string name, params object[] args) => ownerType.GetMethod(name, Members).Invoke(owner, args);
        object Read(object value, string name) => value.GetType().GetProperty(name, Members).GetValue(value);
        void Set(object value, string name, object item) => value.GetType().GetProperty(name, Members).SetValue(value, item);
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("Proactive scan: " + label); }
        IList Parties(int count)
        {
            IList parties = (IList)Activator.CreateInstance(scanType.GetProperty("Parties", Members).PropertyType);
            for (int i = 0; i < count; i++) parties.Add(null);
            return parties;
        }
        object Candidate(float urgency, float fatigue, float weight, int notoriety, float distance)
        {
            object result = Activator.CreateInstance(candidateType, true);
            Set(result, "NeedUrgency", urgency);
            Set(result, "NeedTypeFatigueMultiplier", fatigue);
            Set(result, "NeedTypeWeightMultiplier", weight);
            Set(result, "EffectiveNotorietyAtRequest", notoriety);
            Set(result, "Distance", distance);
            return result;
        }

        Check(!(bool)Read(owner, "IsRunning"), "starts idle");
        object scan = Call("Start", null, Parties(900), 123L);
        Check(scan != null && ReferenceEquals(scan, Read(owner, "Current")), "start owns exact scan");
        Check((int)Read(scan, "BatchSize") == 16 && (long)Read(scan, "StartedAtUtcTicks") == 123L,
            "snapshot is bounded to 16 parties per frame");
        Check(Call("Start", null, Parties(1), 124L) == null, "duplicate start cannot replace live scan");
        object stale = Activator.CreateInstance(scanType, true);
        Check(!(bool)Call("TryComplete", stale) && ReferenceEquals(scan, Read(owner, "Current")),
            "stale completion cannot retire current scan");

        object stats = Activator.CreateInstance(statsType, true);
        Set(stats, "TotalLordParties", 1);
        object first = Candidate(80f, 1f, 1f, 0, 5f);
        object higherRawUrgency = Candidate(100f, .8f, 1f, 0, 5f);
        object higherNotoriety = Candidate(100f, .8f, 1f, 1, 5f);
        object nearer = Candidate(100f, .8f, 1f, 1, 4f);
        object lower = Candidate(79f, 1f, 1f, 9, 1f);
        Call("Consider", scan, first, stats);
        Call("Consider", stale, lower, stats);
        Check((int)Read(Read(scan, "Stats"), "TotalLordParties") == 1, "stale batch cannot add stats");
        Call("Consider", scan, higherRawUrgency, null);
        Check(ReferenceEquals(Read(scan, "BestCandidate"), higherRawUrgency), "raw urgency breaks weighted tie");
        Call("Consider", scan, higherNotoriety, null);
        Check(ReferenceEquals(Read(scan, "BestCandidate"), higherNotoriety), "notoriety breaks urgency tie");
        Call("Consider", scan, nearer, null);
        Call("Consider", scan, lower, null);
        Check(ReferenceEquals(Read(scan, "BestCandidate"), nearer), "distance breaks final tie; lower weighted score loses");
        Check((bool)Call("TryComplete", scan) && !(bool)Read(owner, "IsRunning"), "exact completion consumes scan");
        Check(!(bool)Call("TryComplete", scan), "completion is single-use");
        object next = Call("Start", null, Parties(0), 125L);
        Check((int)Read(next, "BatchSize") == 1, "empty snapshot still has bounded batch size");
        Call("Clear");
        Check(!(bool)Read(owner, "IsRunning") && !(bool)Call("TryComplete", next), "load/cancel retires active scan");
        Console.WriteLine("PASS proactiveCandidateScanOwnerReplay bounded=1 duplicate=1 stale=1 ranking=1 exactComplete=1 clear=1; no live campaign/frame acceptance");
    }
}
