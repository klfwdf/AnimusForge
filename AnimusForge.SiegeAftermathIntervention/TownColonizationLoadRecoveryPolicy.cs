using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownColonizationLoadRecoveryKind
{
    None = 0,
    ClearTerminal = 1,
    ResumeFullOutcome = 2,
    ResumeCultureCommit = 3,
    RejectUnsafe = 4,
}

/// <summary>
/// Classifies persisted colonization state without depending on Bannerlord runtime types.
/// </summary>
public static class TownColonizationLoadRecoveryPolicy
{
    public static TownColonizationLoadRecoveryDecision Evaluate(
        TownColonizationSnapshot snapshot,
        string liveSettlementId,
        bool hasNativeAftermathContext)
    {
        if (snapshot == null || snapshot.State == TownColonizationState.None)
        {
            return TownColonizationLoadRecoveryDecision.None;
        }

        if (!IsSemanticallyValid(snapshot))
        {
            return new TownColonizationLoadRecoveryDecision(
                TownColonizationLoadRecoveryKind.RejectUnsafe,
                snapshot);
        }

        if (snapshot.State == TownColonizationState.CancelledToMassacre
            || (snapshot.State == TownColonizationState.Committed && snapshot.SettlementOutcomeCommitted))
        {
            return new TownColonizationLoadRecoveryDecision(
                TownColonizationLoadRecoveryKind.ClearTerminal,
                snapshot);
        }

        if (!hasNativeAftermathContext
            || !string.Equals(snapshot.SettlementId, Normalize(liveSettlementId), StringComparison.OrdinalIgnoreCase))
        {
            return new TownColonizationLoadRecoveryDecision(
                TownColonizationLoadRecoveryKind.RejectUnsafe,
                snapshot);
        }

        if (snapshot.State == TownColonizationState.ReadyToCommit && snapshot.SettlementOutcomeCommitted)
        {
            return new TownColonizationLoadRecoveryDecision(
                TownColonizationLoadRecoveryKind.ResumeCultureCommit,
                snapshot);
        }

        if (snapshot.State == TownColonizationState.Pending)
        {
            return new TownColonizationLoadRecoveryDecision(
                TownColonizationLoadRecoveryKind.ResumeFullOutcome,
                new TownColonizationSnapshot(
                    TownColonizationState.ReadyToCommit,
                    TownColonizationCommitReason.SceneExit,
                    snapshot.SettlementId,
                    snapshot.TargetCultureId,
                    snapshot.CapturedTargetCount,
                    settlementOutcomeCommitted: false));
        }

        return new TownColonizationLoadRecoveryDecision(
            TownColonizationLoadRecoveryKind.ResumeFullOutcome,
            snapshot);
    }

    public static bool IsSemanticallyValid(TownColonizationSnapshot snapshot)
    {
        if (snapshot == null
            || !Enum.IsDefined(typeof(TownColonizationState), snapshot.State)
            || !Enum.IsDefined(typeof(TownColonizationCommitReason), snapshot.CommitReason)
            || snapshot.State == TownColonizationState.None
            || string.IsNullOrWhiteSpace(snapshot.SettlementId)
            || string.IsNullOrWhiteSpace(snapshot.TargetCultureId)
            || snapshot.CapturedTargetCount < 0)
        {
            return false;
        }

        switch (snapshot.State)
        {
            case TownColonizationState.Pending:
                return snapshot.CommitReason == TownColonizationCommitReason.None
                    && !snapshot.SettlementOutcomeCommitted;
            case TownColonizationState.ReadyToCommit:
                return snapshot.CommitReason != TownColonizationCommitReason.None;
            case TownColonizationState.CancelledToMassacre:
                return snapshot.CommitReason == TownColonizationCommitReason.None
                    && !snapshot.SettlementOutcomeCommitted;
            case TownColonizationState.Committed:
                return snapshot.CommitReason != TownColonizationCommitReason.None;
            default:
                return false;
        }
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim();
    }
}

public sealed class TownColonizationLoadRecoveryDecision
{
    public static readonly TownColonizationLoadRecoveryDecision None =
        new TownColonizationLoadRecoveryDecision(TownColonizationLoadRecoveryKind.None, null);

    public TownColonizationLoadRecoveryDecision(
        TownColonizationLoadRecoveryKind kind,
        TownColonizationSnapshot snapshot)
    {
        Kind = kind;
        Snapshot = snapshot;
    }

    public TownColonizationLoadRecoveryKind Kind { get; }

    public TownColonizationSnapshot Snapshot { get; }
}
