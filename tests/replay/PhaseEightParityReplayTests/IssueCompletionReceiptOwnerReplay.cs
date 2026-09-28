using System;
using System.IO;
using System.Reflection;

internal static class IssueCompletionReceiptOwnerReplay
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static void Run(Assembly assembly, string repo)
    {
        string ownerPath = Path.Combine(repo, "src/modules/AF.Module.Issue/Completion/IssueCompletionReceiptOwner.cs");
        string hostPath = Path.Combine(repo, "VanillaIssuePromptBehavior.cs");
        if (!File.Exists(ownerPath)) throw new InvalidOperationException("Issue completion receipt: owner source missing");
        string ownerSource = File.ReadAllText(ownerPath);
        string hostSource = File.ReadAllText(hostPath);
        if (!ownerSource.Contains("TryBuildCompletionReceipt(", StringComparison.Ordinal)
            || !ownerSource.Contains("BuildMemoryFact(", StringComparison.Ordinal)
            || !hostSource.Contains("IssueCompletionReceiptOwner.TryBuildCompletionReceipt(", StringComparison.Ordinal)
            || hostSource.Contains("string memoryFact =", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue completion receipt: fact decision is not owned by Issue");

        Type owner = assembly.GetType("AnimusForge.IssueCompletionReceiptOwner", true);
        Type details = owner.GetMethod("TryBuildCompletionReceipt", Members).GetParameters()[1].ParameterType;
        object success = Enum.Parse(details, "Success");
        object[] missing = { null, success, null, null };
        if ((bool)owner.GetMethod("TryBuildCompletionReceipt", Members).Invoke(null, missing)
            || missing[2] != null || (string)missing[3] != "")
            throw new InvalidOperationException("Issue completion receipt: missing quest emitted fact");
        string fact = (string)owner.GetMethod("BuildMemoryFact", Members).Invoke(null,
            new object[] { "Test Quest", "任务已成功完成", 120, "final journal" });
        if (!fact.Contains("Test Quest", StringComparison.Ordinal)
            || !fact.Contains("任务已成功完成", StringComparison.Ordinal)
            || !fact.Contains("120 第纳尔已经由系统自动发放", StringComparison.Ordinal)
            || !fact.Contains("最近任务记录：final journal", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue completion receipt: success fact changed");
        string noReward = (string)owner.GetMethod("BuildMemoryFact", Members).Invoke(null,
            new object[] { "", "", 0, "" });
        if (!noReward.Contains("一项原版任务", StringComparison.Ordinal)
            || !noReward.Contains("若有原版任务奖励，也已由系统按原版流程自动结算", StringComparison.Ordinal))
            throw new InvalidOperationException("Issue completion receipt: no-reward fallback changed");
        object cancelled = Enum.Parse(details, "Cancel");
        if ((string)owner.GetMethod("TranslateCompletionDetail", Members).Invoke(null, new[] { cancelled }) != "任务已取消")
            throw new InvalidOperationException("Issue completion receipt: cancellation detail changed");
        Console.WriteLine("PASS issueCompletionReceiptOwnerReplay ownerSource=1 missingQuest=1 success=1 noReward=1 cancelled=1; liveQuest=NOT_RUN");
    }
}
