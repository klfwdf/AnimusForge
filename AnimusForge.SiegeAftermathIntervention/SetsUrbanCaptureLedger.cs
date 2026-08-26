using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Idempotency ledger for one SETS capture operation. Every mutating campaign
/// side effect must pass a TryRecord/TryCommit gate here exactly once; a second
/// attempt returns false and the caller must skip the side effect.
/// </summary>
public sealed class SetsUrbanCaptureLedger
{
    private readonly HashSet<int> _settledAlliedCasualtyAgentIndexes = new HashSet<int>();
    private readonly HashSet<int> _settledDefenderCasualtyAgentIndexes = new HashSet<int>();
    private readonly HashSet<string> _withdrawnReservePhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _victoryCommitted;
    private bool _ownershipCommitted;
    private bool _menuCommitted;
    private bool _villageRewardCommitted;
    private bool _completionCommitted;

    public int SettledAlliedCasualtyCount
    {
        get { return _settledAlliedCasualtyAgentIndexes.Count; }
    }

    public int SettledDefenderCasualtyCount
    {
        get { return _settledDefenderCasualtyAgentIndexes.Count; }
    }

    public bool VictoryCommitted
    {
        get { return _victoryCommitted; }
    }

    public bool OwnershipCommitted
    {
        get { return _ownershipCommitted; }
    }

    public bool MenuCommitted
    {
        get { return _menuCommitted; }
    }

    public bool VillageRewardCommitted
    {
        get { return _villageRewardCommitted; }
    }

    public bool CompletionCommitted
    {
        get { return _completionCommitted; }
    }

    /// <summary>Charge one allied follower casualty to the campaign roster at most once per agent.</summary>
    public bool TryRecordAlliedCasualty(int agentIndex)
    {
        return agentIndex >= 0 && _settledAlliedCasualtyAgentIndexes.Add(agentIndex);
    }

    /// <summary>Charge one defender reserve casualty to its source roster at most once per agent.</summary>
    public bool TryRecordDefenderCasualty(int agentIndex)
    {
        return agentIndex >= 0 && _settledDefenderCasualtyAgentIndexes.Add(agentIndex);
    }

    /// <summary>Record that a reserve phase (garrison/militia/lord_party) was withdrawn from its campaign source once.</summary>
    public bool TryRecordReserveWithdrawal(string phaseKind)
    {
        return !string.IsNullOrWhiteSpace(phaseKind) && _withdrawnReservePhases.Add(phaseKind.Trim());
    }

    public bool HasWithdrawnReserve(string phaseKind)
    {
        return !string.IsNullOrWhiteSpace(phaseKind) && _withdrawnReservePhases.Contains(phaseKind.Trim());
    }

    public bool TryCommitVictory()
    {
        if (_victoryCommitted)
        {
            return false;
        }

        _victoryCommitted = true;
        return true;
    }

    public bool TryCommitOwnership()
    {
        if (_ownershipCommitted)
        {
            return false;
        }

        _ownershipCommitted = true;
        return true;
    }

    public bool TryCommitMenu()
    {
        if (_menuCommitted)
        {
            return false;
        }

        _menuCommitted = true;
        return true;
    }

    public bool TryCommitVillageReward()
    {
        if (_villageRewardCommitted)
        {
            return false;
        }

        _villageRewardCommitted = true;
        return true;
    }

    public bool TryCommitCompletion()
    {
        if (_completionCommitted)
        {
            return false;
        }

        _completionCommitted = true;
        return true;
    }

    /// <summary>
    /// Restore committed stages from a persisted record (load recovery).
    /// Casualty sets intentionally stay empty: agent indexes are mission-scoped
    /// and must never survive a save boundary.
    /// </summary>
    public void RestoreCommittedStages(bool victory, bool ownership, bool menu, bool villageReward, bool completion)
    {
        _victoryCommitted = _victoryCommitted || victory;
        _ownershipCommitted = _ownershipCommitted || ownership;
        _menuCommitted = _menuCommitted || menu;
        _villageRewardCommitted = _villageRewardCommitted || villageReward;
        _completionCommitted = _completionCommitted || completion;
    }
}
