using System;
using System.Reflection;

internal static class MemoryOwnerReceiptReplay
{
    public static void Run(Assembly production)
    {
        Type campaign = Assembly.Load("TaleWorlds.CampaignSystem").GetType("TaleWorlds.CampaignSystem.Campaign", true);
        Require(campaign.GetProperty("Current").GetValue(null) == null, "Replay must not run in a live Campaign.");
        Type facadeType = production.GetType("AnimusForge.Refactor.Adapters.MyBehaviorMemoryFacade", true);
        Type commitType = production.GetType("AnimusForge.Refactor.Contracts.InteractionMemoryCommit", true);
        Type channelType = production.GetType("AnimusForge.Refactor.Contracts.InteractionChannel", true);
        Type cache = production.GetType("AnimusForge.Refactor.Runtime.MemoryCommitReceiptCache", true);
        Type ownerType = production.GetType("AnimusForge.MyBehavior", true);
        Type parallel = Assembly.Load("TaleWorlds.Library").GetType("TaleWorlds.Library.TWParallel", true);
        Require((bool)parallel.GetMethod("IsMainThread").Invoke(null, null), "Expected standalone default thread driver for the missing-Campaign test.");
        const string subject = "af_nonhero:local7d-fixture";
        object facade = Activator.CreateInstance(facadeType, subject, "Fixture NPC");
        foreach (string channel in new[] { "NativeConversation", "SceneShout", "Courier" })
        {
            string commitId = "missing-owner-" + channel + "-" + Guid.NewGuid().ToString("N");
            object commit = Activator.CreateInstance(commitType, commitId, Enum.Parse(channelType, channel),
                "fixture-session", subject, "player fixture", "assistant fixture", null);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                object result = facadeType.GetMethod("Commit").Invoke(facade, new[] { commit });
                bool written = (bool)result.GetType().GetProperty("HistoryWritten").GetValue(result);
                string status = result.GetType().GetProperty("Status").GetValue(result).ToString();
                Require(!written, channel + "/missing Campaign falsely acknowledged history: " + status);
                Require((string)result.GetType().GetProperty("ErrorCode").GetValue(result) == "memory_owner_missing",
                    "Missing-Campaign test did not reach the actual owner guard.");
                Require(!(bool)cache.GetMethod("Contains").Invoke(null, new object[] { commitId }),
                    channel + "/missing Campaign retained a success receipt");
            }
        }
        string staleCacheCommitId = "stale-process-cache-" + Guid.NewGuid().ToString("N");
        Require((bool)cache.GetMethod("TryAccept").Invoke(null, new object[] { staleCacheCommitId }),
            "Could not establish stale process-cache fixture.");
        object staleCacheCommit = Activator.CreateInstance(commitType, staleCacheCommitId,
            Enum.Parse(channelType, "NativeConversation"), "fixture-session", subject,
            "different player payload", "different assistant payload", null);
        object staleCacheResult = facadeType.GetMethod("Commit").Invoke(facade, new[] { staleCacheCommit });
        Require((string)staleCacheResult.GetType().GetProperty("ErrorCode").GetValue(staleCacheResult)
            == "memory_owner_missing",
            "Process cache bypassed the persistent owner/payload validation.");
        MethodInfo strictAppend = ownerType.GetMethod("CommitExternalDialogueHistory");
        object[] appendArgs = { subject, true, "Fixture NPC", "player", "assistant", "confirmed fixture fact" };
        object absentOwner = strictAppend.Invoke(null, appendArgs);
        Require((string)absentOwner.GetType().GetProperty("ErrorCode").GetValue(absentOwner) == "memory_owner_missing", "Direct strict append bypassed absent owner.");
        foreach (string name in new[] { "AppendExternalDialogueHistory", "AppendExternalSceneDialogueHistory", "AppendExternalNonHeroDialogueHistory", "AppendExternalNonHeroSceneDialogueHistory" })
            Require(ownerType.GetMethod(name).ReturnType == typeof(void), "Public compatibility signature changed: " + name);

        // Simulate a mismatched host thread id only inside this no-Campaign replay.
        // The default managed driver returns zero on every thread, so this is a
        // guard fixture, not proof of an initialized Bannerlord thread scheduler.
        FieldInfo mainThreadId = parallel.GetField("_mainThreadId", BindingFlags.NonPublic | BindingFlags.Static);
        object originalThreadId = mainThreadId.GetValue(null);
        try
        {
            mainThreadId.SetValue(null, 1UL);
            object wrongThread = strictAppend.Invoke(null, appendArgs);
            Require((string)wrongThread.GetType().GetProperty("ErrorCode").GetValue(wrongThread) == "memory_not_main_thread", "Strict owner did not reject wrong thread.");
            string commitId = "thread-guard-" + Guid.NewGuid().ToString("N");
            object commit = Activator.CreateInstance(commitType, commitId, Enum.Parse(channelType, "NativeConversation"),
                "fixture-session", subject, "player fixture", "assistant fixture", null);
            object result = facadeType.GetMethod("Commit").Invoke(facade, new[] { commit });
            Require((string)result.GetType().GetProperty("ErrorCode").GetValue(result) == "memory_not_main_thread", "Facade did not reject wrong thread.");
            Require(!(bool)cache.GetMethod("Contains").Invoke(null, new object[] { commitId }), "Thread rejection retained a success receipt.");
        }
        finally { mainThreadId.SetValue(null, originalThreadId); }
        Console.WriteLine("PASS productionMemoryOwnerReceipt missingCampaign=7 noFalseReceipt=7 threadGuardFixture=2 voidCompatibility=4 liveCampaign=NOT-RUN");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
