using System;
using System.Runtime.CompilerServices;

namespace AnimusForge.Refactor.Modules;

// Per-exercise, transient settlement receipt. Never retry an uncertain partial XP commit.
internal sealed class ExerciseSettlementOwner
{
    internal bool SettlementStarted { get; private set; }
    internal bool SettlementDone { get; private set; }
    internal bool VanillaResultPatchHit { get; private set; }
    internal bool XpCommitAttempted { get; private set; }
    internal bool XpCommittedByVanillaPatch { get; private set; }
    internal bool XpCommitSucceeded { get; private set; }
    internal bool EarlyXpCommittedOnMissionEnd { get; private set; }
    internal bool RenownInfluenceSkipped { get; private set; }
    internal bool RoutedTroopsRestored { get; private set; }

    internal void MarkVanillaResultPatchHit() => VanillaResultPatchHit = true;

    internal bool TryBeginXpCommit()
    {
        if (SettlementDone || XpCommitAttempted) return false;
        XpCommitAttempted = true;
        return true;
    }

    internal void CompleteXpCommit(bool succeeded, bool fromVanillaPatch, bool onMissionEnd)
    {
        XpCommitSucceeded = succeeded;
        XpCommittedByVanillaPatch = fromVanillaPatch;
        EarlyXpCommittedOnMissionEnd = succeeded && onMissionEnd;
    }

    internal bool TryBeginCleanupXpCommit(bool skipXpCommit)
        => !skipXpCommit && TryBeginXpCommit();

    internal void MarkRenownInfluenceSkipped()
    {
        RenownInfluenceSkipped = true;
        if (!VanillaResultPatchHit && !XpCommitAttempted)
        {
            XpCommitAttempted = true;
            XpCommittedByVanillaPatch = true;
            XpCommitSucceeded = true;
        }
    }

    internal bool TryBeginSettlement()
    {
        if (SettlementStarted || SettlementDone) return false;
        SettlementStarted = true;
        return true;
    }

    internal bool TryBeginRoutedRestore()
    {
        if (RoutedTroopsRestored) return false;
        RoutedTroopsRestored = true;
        return true;
    }

    internal void CompleteSettlement() => SettlementDone = true;
}

// The orphan recovery path has no live session; its MapEvent identity scopes one XP receipt.
internal sealed class ExerciseOrphanXpOwner<TEvent> where TEvent : class
{
    private readonly ConditionalWeakTable<TEvent, ExerciseSettlementOwner> _receipts = new();

    internal bool CommitOnce(TEvent mapEvent, Func<TEvent, bool> commit)
    {
        if (mapEvent == null) return false;
        ExerciseSettlementOwner receipt = _receipts.GetValue(mapEvent, _ => new ExerciseSettlementOwner());
        if (!receipt.TryBeginXpCommit()) return receipt.XpCommitSucceeded;
        bool succeeded = commit(mapEvent);
        receipt.CompleteXpCommit(succeeded, fromVanillaPatch: true, onMissionEnd: false);
        return succeeded;
    }
}
