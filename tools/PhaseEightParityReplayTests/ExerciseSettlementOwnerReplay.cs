using System;
using System.Reflection;

internal static class ExerciseSettlementOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.ExerciseSettlementOwner", true);
        object New() => Activator.CreateInstance(type, true);
        object Get(object owner, string name) => type.GetProperty(name, All).GetValue(owner);
        object Call(object owner, string name, params object[] args) => type.GetMethod(name, All).Invoke(owner, args);
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Exercise settlement owner: " + name);
        }

        object patch = New();
        Call(patch, "MarkVanillaResultPatchHit");
        Check((bool)Call(patch, "TryBeginXpCommit"), "first patch XP attempt rejected");
        Call(patch, "CompleteXpCommit", true, true, false);
        Check(!(bool)Call(patch, "TryBeginXpCommit")
            && !(bool)Call(patch, "TryBeginCleanupXpCommit", false)
            && (bool)Get(patch, "VanillaResultPatchHit")
            && (bool)Get(patch, "XpCommittedByVanillaPatch")
            && (bool)Get(patch, "XpCommitSucceeded"), "successful patch XP repeated during cleanup");
        Check((bool)Call(patch, "TryBeginSettlement") && !(bool)Call(patch, "TryBeginSettlement"),
            "settlement reentered");
        Check((bool)Call(patch, "TryBeginRoutedRestore") && !(bool)Call(patch, "TryBeginRoutedRestore"),
            "routed troop restoration repeated");
        Call(patch, "CompleteSettlement");
        Check(!(bool)Call(patch, "TryBeginSettlement") && !(bool)Call(patch, "TryBeginXpCommit"),
            "completed settlement reopened");

        object partialFailure = New();
        Check((bool)Call(partialFailure, "TryBeginXpCommit"), "partial-failure first attempt rejected");
        Call(partialFailure, "CompleteXpCommit", false, true, false);
        Check(!(bool)Call(partialFailure, "TryBeginCleanupXpCommit", false)
            && !(bool)Get(partialFailure, "XpCommitSucceeded"),
            "uncertain partial XP was retried or reported as success");
        object early = New();
        Check((bool)Call(early, "TryBeginXpCommit"), "early XP first attempt rejected");
        Call(early, "CompleteXpCommit", true, true, true);
        Check((bool)Get(early, "EarlyXpCommittedOnMissionEnd")
            && !(bool)Call(early, "TryBeginCleanupXpCommit", false), "mission-end XP replayed");
        object skipped = New();
        Check(!(bool)Call(skipped, "TryBeginCleanupXpCommit", true)
            && !(bool)Get(skipped, "XpCommitAttempted"), "skip request consumed XP ticket");
        object renown = New();
        Call(renown, "MarkRenownInfluenceSkipped");
        Check((bool)Get(renown, "RenownInfluenceSkipped")
            && (bool)Get(renown, "XpCommittedByVanillaPatch")
            && !(bool)Call(renown, "TryBeginXpCommit"), "1.3 renown path repeated XP");
        object renownAfterPatch = New();
        Call(renownAfterPatch, "MarkVanillaResultPatchHit");
        Call(renownAfterPatch, "MarkRenownInfluenceSkipped");
        Check(!(bool)Get(renownAfterPatch, "XpCommitAttempted")
            && (bool)Call(renownAfterPatch, "TryBeginXpCommit"),
            "renown path bypassed prior vanilla-result ownership");
        Console.WriteLine("PASS ExerciseSettlementOwnerReplay current-DLL XP receipt/partial failure/early commit/renown/one-shot restoration; live rewards=NOT_RUN");
    }
}
