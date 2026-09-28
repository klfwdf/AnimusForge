using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class IssueRuntimeStateOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly, string repo)
    {
        string ownerPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Runtime/IssueRuntimeStateOwner.cs");
        string promptPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Runtime/IssueRuntimePromptOwner.cs");
        string actionPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs");
        string hostPath = Path.Combine(repo, "VanillaIssueOfferBridge.cs");
        if (!File.Exists(ownerPath)) throw new InvalidOperationException("Issue runtime state: owner source missing");
        if (!File.Exists(promptPath)) throw new InvalidOperationException("Issue runtime state: prompt owner source missing");
        string ownerSource = File.ReadAllText(ownerPath);
        string promptSource = File.ReadAllText(promptPath);
        string actionSource = File.ReadAllText(actionPath);
        string hostSource = File.ReadAllText(hostPath);
        foreach (string symbol in new[] { "internal static bool TryGetRuntimeState(", "internal static bool TryGetOfferableIssue(",
            "internal static bool TryGetInProgressIssue(", "internal static bool TryGetReadyToTurnInIssue(" })
            if (!ownerSource.Contains(symbol, StringComparison.Ordinal) || hostSource.Contains(symbol, StringComparison.Ordinal))
                throw new InvalidOperationException("Issue runtime state: decision is not uniquely in Issue: " + symbol);
        if (!hostSource.Contains("IssueRuntimeStateOwner.TryGetRuntimeState(", StringComparison.Ordinal)
            || !actionSource.Contains("IssueRuntimeStateOwner.TryGetOfferableIssue(", StringComparison.Ordinal)
            || !actionSource.Contains("IssueRuntimeStateOwner.TryGetReadyToTurnInIssue(", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue runtime state: production consumers not wired");
        foreach (string symbol in new[] { "BuildOfferPromptBlock(", "BuildInProgressPromptBlock(",
            "BuildReadyToTurnInPromptBlock(", "BuildNoAvailableIssuePromptBlock(" })
            if (!promptSource.Contains(symbol, StringComparison.Ordinal) || hostSource.Contains("private static string " + symbol, StringComparison.Ordinal))
                throw new InvalidOperationException("Issue runtime state: prompt decision not in Issue: " + symbol);
        if (!hostSource.Contains("IssueRuntimePromptOwner.TryBuildRuntimePromptBlock(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueRuntimePromptOwner.BuildRuntimePostprocessRules(", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue runtime state: prompt consumers not wired");
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
        Type heroType = owner.GetMethod("TryGetOfferableIssue", Members).GetParameters()[0].ParameterType;
        Type issueBase = owner.GetMethod("TryGetOfferableIssue", Members).GetParameters()[1].ParameterType.GetElementType();
        Type issueType = Assembly.Load("SandBox").GetType("SandBox.Issues.FamilyFeudIssueBehavior+FamilyFeudIssue", true);
        object giver = RuntimeHelpers.GetUninitializedObject(heroType);
        object other = RuntimeHelpers.GetUninitializedObject(heroType);
        object issue = RuntimeHelpers.GetUninitializedObject(issueType);
        heroType.GetProperty("Issue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(giver, issue);
        issueBase.GetField("_issueOwner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(issue, giver);
        FieldInfo stateField = issueBase.GetField("_issueState", BindingFlags.Instance | BindingFlags.NonPublic);
        stateField.SetValue(issue, Enum.Parse(stateField.FieldType, "Ongoing"));
        object[] active = { giver, null };
        if (!(bool)owner.GetMethod("TryGetOfferableIssue", Members).Invoke(null, active) || !ReferenceEquals(active[1], issue))
            throw new InvalidOperationException("Issue runtime state: active matching issue not offered");
        stateField.SetValue(issue, Enum.Parse(stateField.FieldType, "SolvingWithQuestSolution"));
        object[] accepted = { giver, null };
        if ((bool)owner.GetMethod("TryGetOfferableIssue", Members).Invoke(null, accepted))
            throw new InvalidOperationException("Issue runtime state: already accepted quest offered twice");
        stateField.SetValue(issue, Enum.Parse(stateField.FieldType, "Ongoing"));
        issueBase.GetField("_issueOwner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(issue, other);
        object[] wrongOwner = { giver, null };
        if ((bool)owner.GetMethod("TryGetOfferableIssue", Members).Invoke(null, wrongOwner))
            throw new InvalidOperationException("Issue runtime state: other giver's issue offered");
        object[] ready = { null, null, null };
        if ((bool)owner.GetMethod("TryGetReadyToTurnInIssue", Members).Invoke(null, ready)
            || ready[1] != null || ready[2] != null)
            throw new InvalidOperationException("Issue runtime state: missing giver reported turn-in");
        if ((string)bridge.GetMethod("BuildRuntimePromptBlockForExternal", Members).Invoke(null, new object[] { null }) != "")
            throw new InvalidOperationException("Issue runtime state: null Hero prompt changed");
        Console.WriteLine("PASS issueRuntimeStateOwnerReplay ownerSource=1 promptSource=1 consumers=1 missingState=1 missingOffer=1 activeOffer=1 acceptedReject=1 wrongOwner=1 missingTurnIn=1 nullPrompt=1; liveQuest=NOT_RUN");
    }
}
