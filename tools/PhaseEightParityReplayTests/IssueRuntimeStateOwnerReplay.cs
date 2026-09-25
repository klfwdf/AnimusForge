using System;
using System.IO;
using System.Reflection;

internal static class IssueRuntimeStateOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly, string repo)
    {
        string ownerPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Runtime/IssueRuntimeStateOwner.cs");
        string hostPath = Path.Combine(repo, "VanillaIssueOfferBridge.cs");
        if (!File.Exists(ownerPath)) throw new InvalidOperationException("Issue runtime state: owner source missing");
        string ownerSource = File.ReadAllText(ownerPath);
        string hostSource = File.ReadAllText(hostPath);
        foreach (string symbol in new[] { "internal static bool TryGetRuntimeState(", "internal static bool TryGetOfferableIssue(",
            "internal static bool TryGetInProgressIssue(", "internal static bool TryGetReadyToTurnInIssue(" })
            if (!ownerSource.Contains(symbol, StringComparison.Ordinal) || hostSource.Contains(symbol, StringComparison.Ordinal))
                throw new InvalidOperationException("Issue runtime state: decision is not uniquely in Issue: " + symbol);
        if (!hostSource.Contains("IssueRuntimeStateOwner.TryGetRuntimeState(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueRuntimeStateOwner.TryGetOfferableIssue(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueRuntimeStateOwner.TryGetReadyToTurnInIssue(", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue runtime state: production consumers not wired");
        Type bridge = assembly.GetType("AnimusForge.VanillaIssueOfferBridge", true);
        Type owner = bridge.GetNestedType("IssueRuntimeStateOwner", BindingFlags.NonPublic);
        if (owner == null) throw new InvalidOperationException("Issue runtime state: owner type missing");
        object[] state = { null, null, null, null };
        if ((bool)owner.GetMethod("TryGetRuntimeState", Members).Invoke(null, state)
            || (string)state[1] != "" || state[2] != null || state[3] != null)
            throw new InvalidOperationException("Issue runtime state: missing giver reported active task");
        object[] offer = { null, null };
        if ((bool)owner.GetMethod("TryGetOfferableIssue", Members).Invoke(null, offer) || offer[1] != null)
            throw new InvalidOperationException("Issue runtime state: missing giver reported offer");
        object[] ready = { null, null, null };
        if ((bool)owner.GetMethod("TryGetReadyToTurnInIssue", Members).Invoke(null, ready)
            || ready[1] != null || ready[2] != null)
            throw new InvalidOperationException("Issue runtime state: missing giver reported turn-in");
        Console.WriteLine("PASS issueRuntimeStateOwnerReplay ownerSource=1 consumers=1 missingState=1 missingOffer=1 missingTurnIn=1; liveQuest=NOT_RUN");
    }
}
