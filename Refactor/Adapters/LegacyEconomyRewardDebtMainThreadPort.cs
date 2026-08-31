using System;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread-only boundary for replaying the Economy/Reward/Debt projection.
/// This class owns boundary policy only: it rejects missing/stale targets,
/// capability-invalid plans and callback failures, then delegates actual game
/// mutation to the domain owner supplied by the channel. It never resolves a
/// Hero, item, debt or settlement and never mutates campaign state itself.
/// </summary>
public sealed class LegacyEconomyRewardDebtMainThreadPort : IEconomyRewardDebtMainThreadPort
{
    private readonly Func<bool> _isMainThread;
    private readonly Func<GameInteractionSnapshot, bool> _isCurrentTarget;
    private readonly Func<EconomyRewardDebtReplayPlan, GameInteractionSnapshot, EconomyRewardDebtReplayResult> _replay;

    public LegacyEconomyRewardDebtMainThreadPort(
        Func<bool> isMainThread,
        Func<GameInteractionSnapshot, bool> isCurrentTarget,
        Func<EconomyRewardDebtReplayPlan, GameInteractionSnapshot, EconomyRewardDebtReplayResult> replay)
    {
        _isMainThread = isMainThread ?? throw new ArgumentNullException(nameof(isMainThread));
        _isCurrentTarget = isCurrentTarget ?? throw new ArgumentNullException(nameof(isCurrentTarget));
        _replay = replay ?? throw new ArgumentNullException(nameof(replay));
    }

    public EconomyRewardDebtReplayResult Replay(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot currentSnapshot)
    {
        if (plan == null)
        {
            return Rejected(EconomyRewardDebtReplayStatus.Failed, "economy.replay_plan_missing");
        }
        if (currentSnapshot?.Identity == null)
        {
            return Rejected(EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation, "economy.snapshot_missing");
        }

        bool onMainThread;
        try
        {
            onMainThread = _isMainThread();
        }
        catch
        {
            onMainThread = false;
        }
        if (!onMainThread)
        {
            return Rejected(EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation, "economy.not_main_thread");
        }

        bool currentTarget;
        try
        {
            currentTarget = _isCurrentTarget(currentSnapshot);
        }
        catch
        {
            currentTarget = false;
        }
        if (!currentTarget)
        {
            return Rejected(EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation, "economy.target_stale_or_changed");
        }

        if (HasBlockingExclusion(plan))
        {
            return Rejected(EconomyRewardDebtReplayStatus.RejectedByCapability, "economy.plan_excluded");
        }
        if (plan.Actions == null || plan.Actions.Count == 0)
        {
            return Rejected(EconomyRewardDebtReplayStatus.NoApplicableAction, "economy.no_applicable_action");
        }

        EconomyRewardDebtReplayResult result;
        try
        {
            result = _replay(plan, currentSnapshot);
        }
        catch
        {
            return Rejected(EconomyRewardDebtReplayStatus.Failed, "economy.domain_replay_exception");
        }
        if (result == null)
        {
            return Rejected(EconomyRewardDebtReplayStatus.Failed, "economy.domain_replay_null_result");
        }
        if (result.AppliedCount < 0 || result.AppliedCount > plan.Actions.Count)
        {
            return Rejected(EconomyRewardDebtReplayStatus.Failed, "economy.applied_count_invalid");
        }
        if (result.Status == EconomyRewardDebtReplayStatus.Applied
            && result.AppliedCount > 0
            && result.AppliedCount < plan.Actions.Count)
        {
            // Preserve backward compatibility with owners that predate the
            // structured partial status but already returned a short count and
            // owner-confirmed facts.
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.PartiallyApplied,
                result.AppliedCount,
                result.ConfirmedFacts,
                string.IsNullOrWhiteSpace(result.ErrorCode) ? "economy.partial_replay" : result.ErrorCode);
        }
        if (result.Status == EconomyRewardDebtReplayStatus.PartiallyApplied
            && (result.AppliedCount <= 0 || result.AppliedCount >= plan.Actions.Count))
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Failed,
                result.AppliedCount,
                result.ConfirmedFacts,
                "economy.partial_count_invalid");
        }
        if (result.Status == EconomyRewardDebtReplayStatus.Applied && result.AppliedCount == 0)
        {
            return Rejected(EconomyRewardDebtReplayStatus.Failed, "economy.applied_without_effect");
        }
        return result;
    }

    private static bool HasBlockingExclusion(EconomyRewardDebtReplayPlan plan)
    {
        return (plan.ExclusionReasons ?? Array.Empty<string>()).Any(reason =>
            !string.IsNullOrWhiteSpace(reason)
            && (reason.StartsWith("economy.capability_missing", StringComparison.OrdinalIgnoreCase)
                || reason.StartsWith("economy.give_", StringComparison.OrdinalIgnoreCase)
                || reason.StartsWith("economy.debt_", StringComparison.OrdinalIgnoreCase)
                || reason.StartsWith("economy.settlement_transfer.", StringComparison.OrdinalIgnoreCase)));
    }

    private static EconomyRewardDebtReplayResult Rejected(
        EconomyRewardDebtReplayStatus status,
        string errorCode)
    {
        return new EconomyRewardDebtReplayResult(status, 0, Array.Empty<FactRecord>(), errorCode);
    }
}
