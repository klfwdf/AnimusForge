using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownColonizationState
{
    None = 0,
    Pending = 1,
    ReadyToCommit = 2,
    CancelledToMassacre = 3,
    Committed = 4,
}

public enum TownColonizationCommitReason
{
    None = 0,
    CapturedTargetsEliminated = 1,
    SceneExit = 2,
}

/// <summary>
/// Owns the scene-level colonization lifecycle without performing game runtime side effects.
/// </summary>
public sealed class TownColonizationStateMachine
{
    private TownColonizationState _state;
    private TownColonizationCommitReason _commitReason;
    private string _settlementId = string.Empty;
    private string _targetCultureId = string.Empty;
    private int _capturedTargetCount;
    private bool _settlementOutcomeCommitted;

    public TownColonizationState State => _state;

    public bool IsPending => _state == TownColonizationState.Pending;

    public bool IsCommitted => _state == TownColonizationState.Committed;

    public bool IsSettlementOutcomeCommitted => _settlementOutcomeCommitted;

    public bool ResolvesAsColonization =>
        _state == TownColonizationState.Pending
        || _state == TownColonizationState.ReadyToCommit
        || _state == TownColonizationState.Committed;

    public bool Request(
        string settlementId,
        string targetCultureId,
        TownOperationLedgerSnapshot ledger)
    {
        string normalizedSettlementId = Normalize(settlementId);
        string normalizedCultureId = Normalize(targetCultureId);
        if (string.IsNullOrWhiteSpace(normalizedSettlementId)
            || string.IsNullOrWhiteSpace(normalizedCultureId)
            || ledger == null
            || ledger.Kind != TownOperationKind.Colonization
            || !ledger.VictimSnapshotSealed)
        {
            return false;
        }

        if (_state != TownColonizationState.None)
        {
            return _state == TownColonizationState.Pending
                && string.Equals(_settlementId, normalizedSettlementId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_targetCultureId, normalizedCultureId, StringComparison.OrdinalIgnoreCase);
        }

        _state = TownColonizationState.Pending;
        _settlementId = normalizedSettlementId;
        _targetCultureId = normalizedCultureId;
        _capturedTargetCount = ledger.CapturedVictimCount;
        return true;
    }

    public bool ObserveCapturedTargets(TownOperationLedgerSnapshot ledger)
    {
        if (_state != TownColonizationState.Pending || !HasCompleteNonEmptySnapshot(ledger))
        {
            return false;
        }

        _capturedTargetCount = ledger.CapturedVictimCount;
        _commitReason = TownColonizationCommitReason.CapturedTargetsEliminated;
        _state = TownColonizationState.ReadyToCommit;
        return true;
    }

    public bool CancelBeforeCompletion(TownOperationLedgerSnapshot ledger)
    {
        if (_state != TownColonizationState.Pending
            || ledger == null
            || ledger.Kind != TownOperationKind.Colonization
            || !ledger.VictimSnapshotSealed
            || HasCompleteNonEmptySnapshot(ledger))
        {
            return false;
        }

        _state = TownColonizationState.CancelledToMassacre;
        _commitReason = TownColonizationCommitReason.None;
        return true;
    }

    public bool PrepareSceneExitCommit()
    {
        if (_state != TownColonizationState.Pending)
        {
            return false;
        }

        _state = TownColonizationState.ReadyToCommit;
        _commitReason = TownColonizationCommitReason.SceneExit;
        return true;
    }

    public bool TryCommit()
    {
        if (_state != TownColonizationState.ReadyToCommit)
        {
            return false;
        }

        _state = TownColonizationState.Committed;
        return true;
    }

    public bool TryCommitSettlementOutcome()
    {
        if ((_state != TownColonizationState.ReadyToCommit && _state != TownColonizationState.Committed)
            || _settlementOutcomeCommitted)
        {
            return false;
        }

        _settlementOutcomeCommitted = true;
        return true;
    }

    public TownColonizationSnapshot Snapshot()
    {
        return new TownColonizationSnapshot(
            _state,
            _commitReason,
            _settlementId,
            _targetCultureId,
            _capturedTargetCount,
            _settlementOutcomeCommitted);
    }

    public void Restore(TownColonizationSnapshot snapshot)
    {
        Reset();
        if (!TownColonizationLoadRecoveryPolicy.IsSemanticallyValid(snapshot))
        {
            return;
        }

        _state = snapshot.State;
        _commitReason = snapshot.CommitReason;
        _settlementId = Normalize(snapshot.SettlementId);
        _targetCultureId = Normalize(snapshot.TargetCultureId);
        _capturedTargetCount = Math.Max(0, snapshot.CapturedTargetCount);
        _settlementOutcomeCommitted = snapshot.SettlementOutcomeCommitted;
    }

    public void Reset()
    {
        _state = TownColonizationState.None;
        _commitReason = TownColonizationCommitReason.None;
        _settlementId = string.Empty;
        _targetCultureId = string.Empty;
        _capturedTargetCount = 0;
        _settlementOutcomeCommitted = false;
    }

    private static bool HasCompleteNonEmptySnapshot(TownOperationLedgerSnapshot ledger)
    {
        return ledger != null
            && ledger.Kind == TownOperationKind.Colonization
            && ledger.VictimSnapshotSealed
            && ledger.CapturedVictimCount > 0
            && ledger.KilledVictimCount >= ledger.CapturedVictimCount;
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim();
    }
}

public sealed class TownColonizationSnapshot
{
    public TownColonizationSnapshot(
        TownColonizationState state,
        TownColonizationCommitReason commitReason,
        string settlementId,
        string targetCultureId,
        int capturedTargetCount,
        bool settlementOutcomeCommitted = false)
    {
        State = state;
        CommitReason = commitReason;
        SettlementId = (settlementId ?? string.Empty).Trim();
        TargetCultureId = (targetCultureId ?? string.Empty).Trim();
        CapturedTargetCount = Math.Max(0, capturedTargetCount);
        SettlementOutcomeCommitted = settlementOutcomeCommitted;
    }

    public TownColonizationState State { get; }

    public TownColonizationCommitReason CommitReason { get; }

    public string SettlementId { get; }

    public string TargetCultureId { get; }

    public int CapturedTargetCount { get; }

    public bool SettlementOutcomeCommitted { get; }
}
