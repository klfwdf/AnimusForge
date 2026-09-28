using System;
using System.Reflection;

internal static class IssueAlternativeDispatchOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly)
    {
        Type bridge = assembly.GetType("AnimusForge.VanillaIssueOfferBridge", true);
        Type ownerType = bridge.GetNestedType("IssueAlternativeDispatchOwner", BindingFlags.NonPublic);
        Type pendingType = bridge.GetNestedType("PendingAlternativeDispatch", BindingFlags.NonPublic);
        if (ownerType == null || pendingType == null || bridge.GetField("_dispatchOwner", Members)?.FieldType != ownerType)
            throw new InvalidOperationException("Issue dispatch: production bridge does not own pending state");
        object owner = Activator.CreateInstance(ownerType, true);
        object first = Activator.CreateInstance(pendingType, true);
        object second = Activator.CreateInstance(pendingType, true);
        object Call(string name, params object[] args) => ownerType.GetMethod(name, Members).Invoke(owner, args);
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Issue dispatch: " + label); }
        Check((bool)Call("TryBegin", first), "first screen admitted");
        Check(!(bool)Call("TryBegin", second), "duplicate screen rejected");
        Check(!(bool)Call("IsCurrent", second), "other screen has no authority");
        object[] wrong = { second, null };
        Check(!(bool)Call("TryTake", wrong) && (bool)Call("IsCurrent", first), "wrong callback cannot clear current screen");
        object[] correct = { first, null };
        Check((bool)Call("TryTake", correct) && ReferenceEquals(correct[1], first), "current callback consumes once");
        Check(!(bool)Call("TryTake", correct), "duplicate callback rejected");
        Check((bool)Call("TryBegin", first), "screen may reopen after close");
        Call("Clear");
        Check((bool)Call("TryBegin", second), "campaign reset admits new screen");
        Check(!(bool)Call("TryTake", correct) && (bool)Call("IsCurrent", second), "late old callback cannot clear new screen");
        object bridgeOwner = bridge.GetField("_dispatchOwner", Members).GetValue(null);
        ownerType.GetMethod("Clear", Members).Invoke(bridgeOwner, null);
        Check((bool)ownerType.GetMethod("TryBegin", Members).Invoke(bridgeOwner, new[] { first }), "production bridge admits screen");
        bridge.GetMethod("ClearPendingAlternativeDispatchForCampaign", Members).Invoke(null, null);
        Check((bool)ownerType.GetMethod("TryBegin", Members).Invoke(bridgeOwner, new[] { second }), "production campaign reset admits replacement");
        bridge.GetMethod("OnAlternativePartyScreenClosed", Members).Invoke(null, new object[] { null, null, null, null, null, null, false, first });
        Check((bool)ownerType.GetMethod("IsCurrent", Members).Invoke(bridgeOwner, new[] { second }), "production stale callback leaves new screen intact");
        bridge.GetMethod("ClearPendingAlternativeDispatchForCampaign", Members).Invoke(null, null);
        Check(!(bool)bridge.GetMethod("CompleteAlternativeDispatch", Members).Invoke(null, new object[] { null, null, null }), "missing issue never reports successful dispatch");
        Console.WriteLine("PASS issueAlternativeDispatchOwnerReplay duplicate=1 exactCallback=1 clear=1 staleCallback=1 bridgeReset=1 hostCallback=1 missingIssue=1; no live quest/party-screen acceptance");
    }
}
