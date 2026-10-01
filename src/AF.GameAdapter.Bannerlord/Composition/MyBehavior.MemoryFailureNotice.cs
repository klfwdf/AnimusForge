using System;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private sealed class MemoryFailureNotice
    {
        internal readonly string Title;
        internal readonly string Message;
        internal readonly long Generation;

        internal MemoryFailureNotice(string title, string message, long generation)
        {
            Title = title;
            Message = message ?? "";
            Generation = generation;
        }
    }

    // At most one pending failure per owner. This is not a general game-action queue.
    private MemoryFailureNotice _pendingMemoryFailureNotice;
    private long _memoryFailurePopupRevision;

    private bool IsCurrentMemoryFailureOwner(long generation)
    {
        return ReferenceEquals(Instance, this) && SaveRuntimeGuard.IsCurrentGeneration(generation);
    }

    private bool IsCurrentMemoryFailureCampaign()
    {
        try { return ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this); }
        catch (Exception) { return false; }
    }

    private void PublishMemoryFailureNotice(string title, string message, long generation)
    {
        ObserveMemoryFailureNotice(title, message);
        if (!IsCurrentMemoryFailureOwner(generation) || Volatile.Read(ref _memorySummaryFailurePopupActive)) return;
        MemoryFailureNotice pending = Volatile.Read(ref _pendingMemoryFailureNotice);
        if (pending == null || !SaveRuntimeGuard.IsCurrentGeneration(pending.Generation))
        {
            // First valid failure wins. A retired save's slot cannot suppress the current save.
            Interlocked.CompareExchange(ref _pendingMemoryFailureNotice,
                new MemoryFailureNotice(title, message, generation), pending);
        }
        if (TWParallel.IsMainThread()) ProcessPendingMemoryFailureNotice();
    }

    private void ProcessPendingMemoryFailureNotice()
    {
        if (!TWParallel.IsMainThread()) return;
        MemoryFailureNotice notice = Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);
        if (notice == null || !IsCurrentMemoryFailureOwner(notice.Generation) || _memorySummaryFailurePopupActive
            || !IsCurrentMemoryFailureCampaign()) return;
        long revision = Interlocked.Increment(ref _memoryFailurePopupRevision);
        try
        {
            Volatile.Write(ref _memorySummaryFailurePopupActive, true);
            InformationManager.ShowInquiry(new InquiryData(notice.Title, notice.Message,
                isAffirmativeOptionShown: true, isNegativeOptionShown: false, "知道了", "",
                () => CompleteMemoryFailureNotice(notice.Generation, revision), null), pauseGameActiveState: true);
        }
        catch (Exception)
        {
            // ShowInquiry can fail after publishing a callback; only this presentation may be released.
            CompleteMemoryFailureNotice(notice.Generation, revision);
            ObserveMemoryFailureNotice(notice.Title, "memory failure inquiry could not be displayed");
        }
    }

    private void CompleteMemoryFailureNotice(long generation, long revision)
    {
        if (!TWParallel.IsMainThread() || !IsCurrentMemoryFailureOwner(generation)
            || revision != Interlocked.Read(ref _memoryFailurePopupRevision)
            || !IsCurrentMemoryFailureCampaign()) return;
        Volatile.Write(ref _memorySummaryFailurePopupActive, false);
        Interlocked.Increment(ref _memoryFailurePopupRevision);
    }

    private void ResetMemoryFailureNotices()
    {
        Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);
        Interlocked.Increment(ref _memoryFailurePopupRevision);
        Volatile.Write(ref _memorySummaryFailurePopupActive, false);
    }

    private static void ObserveMemoryFailureNotice(string title, string message)
    {
        try { Logger.Log("CompressedMemory", "[BLOCK] " + title + " :: " + (message ?? "")); }
        catch (Exception)
        {
            // Optional diagnostics must not hide an actionable memory failure.
            return;
        }
    }
}
