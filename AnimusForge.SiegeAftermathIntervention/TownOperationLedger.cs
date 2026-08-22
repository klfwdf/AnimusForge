using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownOperationKind
{
    None = 0,
    Plunder = 1,
    Massacre = 2,
    Colonization = 3,
}

public enum TownOperationState
{
    None = 0,
    Active = 1,
    Stopped = 2,
    Completed = 3,
}

public enum TownOperationTargetKind
{
    FullTown = 0,
    Merchant = 1,
    Notable = 2,
    Civilian = 3,
}

public enum TownOperationAcquisitionSource
{
    Unknown = 0,
    DirectRobbery = 1,
    SoldierPlunder = 2,
    ExitSweep = 3,
    SettlementTreasury = 4,
    SettlementInventory = 5,
}

/// <summary>
/// Scene-scoped source of truth for unique operation targets and incremental plunder value.
/// </summary>
public sealed class TownOperationLedger
{
    public const int FullProgressBasisPoints = 10000;

    private readonly Dictionary<string, MutableTarget> _targets =
        new Dictionary<string, MutableTarget>(StringComparer.OrdinalIgnoreCase);
    private long _totalAvailableValue;
    private long _acquiredGold;
    private long _acquiredItemValue;
    private int _acquiredItemCount;
    private int _committedProgressBasisPoints;
    private bool _targetSnapshotSealed;
    private bool _fullOutcomeForced;

    public TownOperationKind Kind { get; private set; }

    public TownOperationState State { get; private set; }

    public bool Begin(TownOperationKind kind, long totalAvailableValue)
    {
        if (kind == TownOperationKind.None || State == TownOperationState.Completed)
        {
            return false;
        }
        if (State == TownOperationState.None)
        {
            Kind = kind;
            State = TownOperationState.Active;
            _totalAvailableValue = Math.Max(0L, totalAvailableValue);
            return true;
        }
        if (Kind != kind)
        {
            return false;
        }
        if (State == TownOperationState.Stopped)
        {
            State = TownOperationState.Active;
        }
        return State == TownOperationState.Active;
    }

    public bool RegisterTarget(string targetId, TownOperationTargetKind targetKind)
    {
        string key = NormalizeKey(targetId);
        if (string.IsNullOrWhiteSpace(key)
            || State == TownOperationState.None
            || State == TownOperationState.Completed
            || _targetSnapshotSealed)
        {
            return false;
        }
        if (_targets.ContainsKey(key))
        {
            return false;
        }

        _targets[key] = new MutableTarget(key, targetKind);
        return true;
    }

    public bool SealTargetSnapshot()
    {
        if (State == TownOperationState.None || State == TownOperationState.Completed)
        {
            return false;
        }
        _targetSnapshotSealed = true;
        return true;
    }

    public bool TryClaimTarget(string targetId)
    {
        if (State != TownOperationState.Active
            || !_targets.TryGetValue(NormalizeKey(targetId), out MutableTarget target)
            || target.Claimed
            || target.Completed)
        {
            return false;
        }
        target.Claimed = true;
        return true;
    }

    public bool ReleaseTarget(string targetId)
    {
        if (!_targets.TryGetValue(NormalizeKey(targetId), out MutableTarget target)
            || !target.Claimed
            || target.Completed)
        {
            return false;
        }
        target.Claimed = false;
        return true;
    }

    public bool CompleteTarget(
        string targetId,
        TownOperationAcquisitionSource source,
        long acquiredGold,
        long acquiredItemValue,
        int acquiredItemCount)
    {
        if (!_targets.TryGetValue(NormalizeKey(targetId), out MutableTarget target)
            || !target.Claimed
            || target.Completed)
        {
            return false;
        }

        long gold = Math.Max(0L, acquiredGold);
        long itemValue = Math.Max(0L, acquiredItemValue);
        int itemCount = Math.Max(0, acquiredItemCount);
        if (gold == 0L && itemValue == 0L && itemCount == 0)
        {
            return false;
        }

        target.Claimed = false;
        target.Completed = true;
        target.Source = source;
        target.AcquiredGold = gold;
        target.AcquiredItemValue = itemValue;
        target.AcquiredItemCount = itemCount;
        _acquiredGold = AddClamped(_acquiredGold, gold);
        _acquiredItemValue = AddClamped(_acquiredItemValue, itemValue);
        _acquiredItemCount = itemCount > int.MaxValue - _acquiredItemCount
            ? int.MaxValue
            : _acquiredItemCount + itemCount;
        long totalAcquiredValue = GetTotalAcquiredValue();
        if (totalAcquiredValue > _totalAvailableValue)
        {
            _totalAvailableValue = totalAcquiredValue;
        }
        return gold > 0L || itemValue > 0L || itemCount > 0;
    }

    public bool HasCompletedTarget(string targetId)
    {
        return _targets.TryGetValue(NormalizeKey(targetId), out MutableTarget target) && target.Completed;
    }

    public int GetCompletedTargetCount(TownOperationAcquisitionSource source)
    {
        return _targets.Values.Count(target => target.Completed && target.Source == source);
    }

    public bool Stop()
    {
        if (State != TownOperationState.Active)
        {
            return false;
        }
        foreach (MutableTarget target in _targets.Values)
        {
            target.Claimed = false;
        }
        State = TownOperationState.Stopped;
        return true;
    }

    public bool CompletePartialOutcome()
    {
        if (State == TownOperationState.None || State == TownOperationState.Completed)
        {
            return false;
        }
        foreach (MutableTarget target in _targets.Values)
        {
            target.Claimed = false;
        }
        State = TownOperationState.Completed;
        return true;
    }

    public bool CompleteFullOutcome()
    {
        if (State == TownOperationState.None || State == TownOperationState.Completed)
        {
            return false;
        }
        _fullOutcomeForced = true;
        foreach (MutableTarget target in _targets.Values)
        {
            target.Claimed = false;
        }
        State = TownOperationState.Completed;
        return true;
    }

    public TownOperationProgressCommit CommitCurrentProgress()
    {
        int cumulative = GetProgressBasisPoints();
        int delta = Math.Max(0, cumulative - _committedProgressBasisPoints);
        if (delta > 0)
        {
            _committedProgressBasisPoints = cumulative;
        }
        return new TownOperationProgressCommit(cumulative, delta);
    }

    public TownOperationLedgerSnapshot Snapshot()
    {
        TownOperationTargetSnapshot[] targets = _targets.Values
            .OrderBy(target => target.TargetId, StringComparer.OrdinalIgnoreCase)
            .Select(target => new TownOperationTargetSnapshot(
                target.TargetId,
                target.TargetKind,
                target.Source,
                target.Completed,
                target.AcquiredGold,
                target.AcquiredItemValue,
                target.AcquiredItemCount))
            .ToArray();
        return new TownOperationLedgerSnapshot(
            Kind,
            State,
            _targetSnapshotSealed,
            _fullOutcomeForced,
            _totalAvailableValue,
            _acquiredGold,
            _acquiredItemValue,
            _acquiredItemCount,
            GetProgressBasisPoints(),
            _committedProgressBasisPoints,
            targets);
    }

    public void Reset()
    {
        _targets.Clear();
        Kind = TownOperationKind.None;
        State = TownOperationState.None;
        _totalAvailableValue = 0L;
        _acquiredGold = 0L;
        _acquiredItemValue = 0L;
        _acquiredItemCount = 0;
        _committedProgressBasisPoints = 0;
        _targetSnapshotSealed = false;
        _fullOutcomeForced = false;
    }

    private int GetProgressBasisPoints()
    {
        if (_fullOutcomeForced)
        {
            return FullProgressBasisPoints;
        }
        long acquired = GetTotalAcquiredValue();
        if (acquired <= 0L || _totalAvailableValue <= 0L)
        {
            return 0;
        }
        decimal ratio = Math.Min(1m, acquired / (decimal)_totalAvailableValue);
        return Math.Min(FullProgressBasisPoints, Math.Max(0, (int)Math.Round(
            ratio * FullProgressBasisPoints,
            MidpointRounding.AwayFromZero)));
    }

    private long GetTotalAcquiredValue()
    {
        return AddClamped(_acquiredGold, _acquiredItemValue);
    }

    private static long AddClamped(long current, long addition)
    {
        long safeCurrent = Math.Max(0L, current);
        long safeAddition = Math.Max(0L, addition);
        return safeAddition > long.MaxValue - safeCurrent ? long.MaxValue : safeCurrent + safeAddition;
    }

    private static string NormalizeKey(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    private sealed class MutableTarget
    {
        public MutableTarget(string targetId, TownOperationTargetKind targetKind)
        {
            TargetId = targetId;
            TargetKind = targetKind;
        }

        public string TargetId { get; }

        public TownOperationTargetKind TargetKind { get; }

        public bool Claimed { get; set; }

        public bool Completed { get; set; }

        public TownOperationAcquisitionSource Source { get; set; }

        public long AcquiredGold { get; set; }

        public long AcquiredItemValue { get; set; }

        public int AcquiredItemCount { get; set; }
    }
}

public sealed class TownOperationLedgerSnapshot
{
    public TownOperationLedgerSnapshot(
        TownOperationKind kind,
        TownOperationState state,
        bool targetSnapshotSealed,
        bool fullOutcomeForced,
        long totalAvailableValue,
        long acquiredGold,
        long acquiredItemValue,
        int acquiredItemCount,
        int progressBasisPoints,
        int committedProgressBasisPoints,
        IReadOnlyList<TownOperationTargetSnapshot> targets)
    {
        Kind = kind;
        State = state;
        TargetSnapshotSealed = targetSnapshotSealed;
        FullOutcomeForced = fullOutcomeForced;
        TotalAvailableValue = Math.Max(0L, totalAvailableValue);
        AcquiredGold = Math.Max(0L, acquiredGold);
        AcquiredItemValue = Math.Max(0L, acquiredItemValue);
        AcquiredItemCount = Math.Max(0, acquiredItemCount);
        ProgressBasisPoints = Math.Min(TownOperationLedger.FullProgressBasisPoints, Math.Max(0, progressBasisPoints));
        CommittedProgressBasisPoints = Math.Min(TownOperationLedger.FullProgressBasisPoints, Math.Max(0, committedProgressBasisPoints));
        Targets = targets ?? Array.Empty<TownOperationTargetSnapshot>();
    }

    public TownOperationKind Kind { get; }

    public TownOperationState State { get; }

    public bool TargetSnapshotSealed { get; }

    public bool FullOutcomeForced { get; }

    public long TotalAvailableValue { get; }

    public long AcquiredGold { get; }

    public long AcquiredItemValue { get; }

    public int AcquiredItemCount { get; }

    public int ProgressBasisPoints { get; }

    public int CommittedProgressBasisPoints { get; }

    public IReadOnlyList<TownOperationTargetSnapshot> Targets { get; }

    public long AcquiredValue => AcquiredItemValue > long.MaxValue - AcquiredGold
        ? long.MaxValue
        : AcquiredGold + AcquiredItemValue;

    public int CompletedTargetCount => Targets.Count(target => target.Completed);

    public int MerchantTargetCount => CountCompleted(TownOperationTargetKind.Merchant);

    public int NotableTargetCount => CountCompleted(TownOperationTargetKind.Notable);

    public int CivilianTargetCount => CountCompleted(TownOperationTargetKind.Civilian);

    private int CountCompleted(TownOperationTargetKind kind)
    {
        return Targets.Count(target => target.Completed && target.TargetKind == kind);
    }
}

public sealed class TownOperationTargetSnapshot
{
    public TownOperationTargetSnapshot(
        string targetId,
        TownOperationTargetKind targetKind,
        TownOperationAcquisitionSource source,
        bool completed,
        long acquiredGold,
        long acquiredItemValue,
        int acquiredItemCount)
    {
        TargetId = targetId ?? string.Empty;
        TargetKind = targetKind;
        Source = source;
        Completed = completed;
        AcquiredGold = Math.Max(0L, acquiredGold);
        AcquiredItemValue = Math.Max(0L, acquiredItemValue);
        AcquiredItemCount = Math.Max(0, acquiredItemCount);
    }

    public string TargetId { get; }

    public TownOperationTargetKind TargetKind { get; }

    public TownOperationAcquisitionSource Source { get; }

    public bool Completed { get; }

    public long AcquiredGold { get; }

    public long AcquiredItemValue { get; }

    public int AcquiredItemCount { get; }
}

public readonly struct TownOperationProgressCommit
{
    public TownOperationProgressCommit(int cumulativeBasisPoints, int deltaBasisPoints)
    {
        CumulativeBasisPoints = Math.Min(TownOperationLedger.FullProgressBasisPoints, Math.Max(0, cumulativeBasisPoints));
        DeltaBasisPoints = Math.Min(CumulativeBasisPoints, Math.Max(0, deltaBasisPoints));
    }

    public int CumulativeBasisPoints { get; }

    public int DeltaBasisPoints { get; }

    public bool HasDelta => DeltaBasisPoints > 0;
}
