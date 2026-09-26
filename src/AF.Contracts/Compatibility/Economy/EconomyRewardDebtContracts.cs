using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Stable capability names for the existing Reward/Trade/Debt authority.
/// These names grant permission to prepare a replay request; they never grant
/// permission to mutate the campaign from a detached worker.
/// </summary>
public static class EconomyRewardDebtCapabilityIds
{
    public const string GiveAsset = "economy.reward.give_asset";
    public const string GiveGold = "economy.reward.give_gold";
    public const string DebtCreate = "economy.debt.create";
    public const string DebtResolve = "economy.debt.resolve";
    public const string SettlementTransfer = "economy.settlement.transfer";
}

public enum EconomyRewardDebtActionKind
{
    GiveAsset,
    GiveGold,
    DebtCreate,
    DebtResolve,
    SettlementTransfer
}

/// <summary>
/// A string/ID-only projection of one existing economy action. It is safe to
/// carry across the detached boundary. Asset, debt and settlement tokens are
/// intentionally not resolved here; the owning game service must revalidate
/// them on the main thread against current state.
/// </summary>
public sealed class EconomyRewardDebtAction
{
    public EconomyRewardDebtAction(
        EconomyRewardDebtActionKind kind,
        string sourceTag,
        string targetId,
        string assetToken,
        string quantityToken,
        string amountToken,
        string debtId,
        string settlementToken,
        string directionToken,
        string capabilityId,
        string dueDaysToken = "",
        string noteToken = "")
    {
        Kind = kind;
        SourceTag = ContractGuard.Required(sourceTag, nameof(sourceTag));
        TargetId = targetId ?? string.Empty;
        AssetToken = assetToken ?? string.Empty;
        QuantityToken = quantityToken ?? string.Empty;
        AmountToken = amountToken ?? string.Empty;
        DebtId = debtId ?? string.Empty;
        SettlementToken = settlementToken ?? string.Empty;
        DirectionToken = directionToken ?? string.Empty;
        CapabilityId = ContractGuard.Required(capabilityId, nameof(capabilityId));
        DueDaysToken = dueDaysToken ?? string.Empty;
        NoteToken = noteToken ?? string.Empty;
    }

    public EconomyRewardDebtActionKind Kind { get; }
    public string SourceTag { get; }
    public string TargetId { get; }
    public string AssetToken { get; }
    public string QuantityToken { get; }
    public string AmountToken { get; }
    public string DebtId { get; }
    public string SettlementToken { get; }
    public string DirectionToken { get; }
    public string CapabilityId { get; }
    public string DueDaysToken { get; }
    public string NoteToken { get; }
}

public sealed class EconomyRewardDebtReplayPlan
{
    public EconomyRewardDebtReplayPlan(
        IEnumerable<EconomyRewardDebtAction> actions,
        IEnumerable<string> exclusionReasons)
    {
        Actions = new List<EconomyRewardDebtAction>(actions ?? Enumerable.Empty<EconomyRewardDebtAction>()).AsReadOnly();
        ExclusionReasons = new List<string>(exclusionReasons ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<EconomyRewardDebtAction> Actions { get; }
    public IReadOnlyList<string> ExclusionReasons { get; }
    public bool HasExcludedActions => ExclusionReasons.Count > 0;
}

public enum EconomyRewardDebtReplayStatus
{
    Applied,
    NoApplicableAction,
    RejectedByCapability,
    RejectedByMainThreadValidation,
    Failed,
    PartiallyApplied,
    UnknownAfterStart
}

/// <summary>
/// Result returned by the main-thread domain owner. ConfirmedFacts are
/// produced only after the owner has applied and verified the action; callers
/// must not synthesize them from a detached plan.
/// </summary>
public sealed class EconomyRewardDebtReplayResult
{
    public EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus status,
        int appliedCount,
        IEnumerable<FactRecord> confirmedFacts,
        string errorCode)
    {
        Status = status;
        AppliedCount = Math.Max(0, appliedCount);
        ConfirmedFacts = new List<FactRecord>(confirmedFacts ?? Enumerable.Empty<FactRecord>()).AsReadOnly();
        ErrorCode = errorCode ?? string.Empty;
    }

    public EconomyRewardDebtReplayStatus Status { get; }
    public int AppliedCount { get; }
    public IReadOnlyList<FactRecord> ConfirmedFacts { get; }
    public string ErrorCode { get; }
}

public interface IEconomyRewardDebtReplayPlanner
{
    EconomyRewardDebtReplayPlan Plan(ActionPlan actionPlan, CapabilitySet capabilities);
}

/// <summary>
/// Main-thread-only port. Implementations resolve live Hero, inventory,
/// debt and settlement state and then delegate to the existing domain owner.
/// </summary>
public interface IEconomyRewardDebtMainThreadPort
{
    EconomyRewardDebtReplayResult Replay(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot currentSnapshot);
}
