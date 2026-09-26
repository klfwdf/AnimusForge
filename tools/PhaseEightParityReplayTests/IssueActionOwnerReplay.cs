using System;
using System.Collections;
using System.IO;
using System.Reflection;

internal static class IssueActionOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly, string repo)
    {
        string ownerPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs");
        string hostPath = Path.Combine(repo, "VanillaIssueOfferBridge.cs");
        string promptPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Runtime/IssueRuntimePromptOwner.cs");
        string turnInPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Actions/IssueTurnInDecisionOwner.cs");
        if (!File.Exists(ownerPath)) throw new InvalidOperationException("Issue actions: owner source missing");
        if (!File.Exists(turnInPath)) throw new InvalidOperationException("Issue actions: turn-in decision owner source missing");
        string ownerSource = File.ReadAllText(ownerPath);
        string hostSource = File.ReadAllText(hostPath);
        string promptSource = File.ReadAllText(promptPath);
        string turnInSource = File.ReadAllText(turnInPath);
        foreach (string symbol in new[] { "TryAcceptIssueSelf(", "TryAcceptIssueWithCompanion(",
            "TryBuildAlternativeCandidates(", "TryTurnInIssue(" })
            if (!ownerSource.Contains(symbol, StringComparison.Ordinal)
                || hostSource.Contains("private static bool " + symbol, StringComparison.Ordinal))
                throw new InvalidOperationException("Issue actions: decision not uniquely in Issue: " + symbol);
        if (!hostSource.Contains("IssueActionOwner.TryAcceptIssueSelf(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueActionOwner.TryAcceptIssueWithCompanion(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueActionOwner.TryTurnInIssue(", StringComparison.Ordinal)
            || !hostSource.Contains("return IssueActionOwner.CompleteAlternativeDispatch(", StringComparison.Ordinal)
            || !promptSource.Contains("IssueActionOwner.TryBuildAlternativeCandidates(", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue actions: production consumers not wired");
        if (!turnInSource.Contains("AnalyzeTurnInOptions(", StringComparison.Ordinal)
            || !turnInSource.Contains("ScorePotentialTurnInOption(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueTurnInDecisionOwner.AnalyzeTurnInOptions(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueTurnInDecisionOwner.ScorePotentialTurnInOption(", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue actions: turn-in choice not owned by Issue");
        Type bridge = assembly.GetType("AnimusForge.VanillaIssueOfferBridge", true);
        Type owner = bridge.GetNestedType("IssueActionOwner", BindingFlags.NonPublic);
        if (owner == null) throw new InvalidOperationException("Issue actions: owner type missing");
        object[] candidates = { null, null, null };
        if ((bool)owner.GetMethod("TryBuildAlternativeCandidates", Members).Invoke(null, candidates)
            || candidates[1] is not IList list || list.Count != 0
            || (string)candidates[2] != "该任务不支持同伴代办。")
            throw new InvalidOperationException("Issue actions: missing issue offered companion dispatch");
        if ((string)owner.GetMethod("GetUnavailableCompanionReason", Members).Invoke(null, new object[] { null }) != "未找到同伴。")
            throw new InvalidOperationException("Issue actions: missing companion accepted");
        if ((bool)bridge.GetMethod("CompleteAlternativeDispatch", Members).Invoke(null, new object[] { null, null, null }))
            throw new InvalidOperationException("Issue actions: missing dispatch produced acceptance");
        Type turnIn = bridge.GetNestedType("IssueTurnInDecisionOwner", BindingFlags.NonPublic);
        if (turnIn == null) throw new InvalidOperationException("Issue actions: turn-in owner type missing");
        MethodInfo score = turnIn.GetMethod("ScorePotentialTurnInOption", Members);
        int positive = (int)score.Invoke(null, new object[] { "I have done it", null, null });
        int negative = (int)score.Invoke(null, new object[] { "not yet", null, null });
        if (positive < 90 || negative >= 0)
            throw new InvalidOperationException("Issue actions: turn-in confidence polarity changed");
        Console.WriteLine("PASS issueActionOwnerReplay source=1 consumers=1 missingIssue=1 missingCompanion=1 noFalseDispatch=1 turnInPolarity=1; liveQuest=NOT_RUN");
    }
}
