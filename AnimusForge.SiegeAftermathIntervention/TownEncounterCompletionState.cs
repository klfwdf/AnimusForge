using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the side-effect-free summary and encounter-finish transition state for one GCCZ aftermath.
/// </summary>
public sealed class TownEncounterCompletionState
{
    private DateTime _noNativeMenuSinceUtc = DateTime.MinValue;
    private bool _finishMessageClaimed;
    private bool _nativeDevastateSummaryContinueHandled;

    public bool IsSummaryPending { get; private set; }

    public SiegeAftermathResolutionKind SummaryAftermath { get; private set; } = SiegeAftermathResolutionKind.ShowMercy;

    public string SummaryText { get; private set; } = string.Empty;

    public bool IsSummaryMenuPresented { get; private set; }

    public bool IsSummaryContinueRequested { get; private set; }

    public bool IsFinishQueued { get; private set; }

    public SiegeAftermathResolutionKind FinishAftermath { get; private set; } = SiegeAftermathResolutionKind.ShowMercy;

    public int FinishDelayTicks { get; private set; }

    public int FinishAttempts { get; private set; }

    public bool HasPendingTransition => IsSummaryPending || IsFinishQueued;

    public SiegeAftermathResolutionKind PendingAftermath => IsFinishQueued ? FinishAftermath : SummaryAftermath;

    public void BeginSummary(SiegeAftermathResolutionKind aftermath)
    {
        IsSummaryPending = true;
        SummaryAftermath = Normalize(aftermath);
    }

    public void SetSummaryAftermath(SiegeAftermathResolutionKind aftermath)
    {
        SummaryAftermath = Normalize(aftermath);
    }

    public void SetSummaryText(string summaryText)
    {
        SummaryText = summaryText ?? string.Empty;
    }

    public void MarkSummaryMenuPresented()
    {
        IsSummaryMenuPresented = true;
    }

    public void RequestSummaryContinue()
    {
        IsSummaryContinueRequested = true;
    }

    public bool QueueFinish(SiegeAftermathResolutionKind aftermath, int delayTicks, bool forceDelay)
    {
        bool newlyQueued = !IsFinishQueued;
        if (newlyQueued)
        {
            IsFinishQueued = true;
            FinishAttempts = 0;
            _finishMessageClaimed = false;
            ResetNoNativeMenuWait();
        }

        FinishAftermath = Normalize(aftermath);
        if (forceDelay || FinishDelayTicks <= 0)
        {
            FinishDelayTicks = Math.Max(0, delayTicks);
        }

        return newlyQueued;
    }

    public bool TryConsumeFinishDelayTick()
    {
        if (!IsFinishQueued || FinishDelayTicks <= 0)
        {
            return false;
        }

        FinishDelayTicks--;
        return true;
    }

    public int RecordFinishAttempt()
    {
        if (!IsFinishQueued)
        {
            return FinishAttempts;
        }

        FinishAttempts++;
        return FinishAttempts;
    }

    public bool TryClaimFinishMessage()
    {
        if (!IsFinishQueued || _finishMessageClaimed)
        {
            return false;
        }

        _finishMessageClaimed = true;
        return true;
    }

    public void ResetNoNativeMenuWait()
    {
        _noNativeMenuSinceUtc = DateTime.MinValue;
    }

    public bool HasSettledWithoutNativeMenu(DateTime nowUtc, double settleSeconds)
    {
        if (_noNativeMenuSinceUtc == DateTime.MinValue)
        {
            _noNativeMenuSinceUtc = nowUtc;
            return false;
        }

        return (nowUtc - _noNativeMenuSinceUtc).TotalSeconds >= Math.Max(0d, settleSeconds);
    }

    public bool TryHandleNativeDevastateSummaryContinue()
    {
        if (_nativeDevastateSummaryContinueHandled)
        {
            return false;
        }

        _nativeDevastateSummaryContinueHandled = true;
        return true;
    }

    public void ResetNativeDevastateSummaryContinue()
    {
        _nativeDevastateSummaryContinueHandled = false;
    }

    public void ClearTransition(bool preserveSummary)
    {
        if (!preserveSummary)
        {
            IsSummaryPending = false;
            IsSummaryMenuPresented = false;
            IsSummaryContinueRequested = false;
        }

        IsFinishQueued = false;
        FinishAftermath = SiegeAftermathResolutionKind.ShowMercy;
        FinishDelayTicks = 0;
        FinishAttempts = 0;
        _finishMessageClaimed = false;
        ResetNoNativeMenuWait();
        ResetNativeDevastateSummaryContinue();
    }

    public void Reset()
    {
        ClearTransition(preserveSummary: false);
        SummaryAftermath = SiegeAftermathResolutionKind.ShowMercy;
        SummaryText = string.Empty;
    }

    private static SiegeAftermathResolutionKind Normalize(SiegeAftermathResolutionKind aftermath)
    {
        return aftermath == SiegeAftermathResolutionKind.Unknown
            ? SiegeAftermathResolutionKind.ShowMercy
            : aftermath;
    }
}
