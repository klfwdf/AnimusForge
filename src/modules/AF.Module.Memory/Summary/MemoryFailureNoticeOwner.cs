using System;
using System.Threading;



namespace AnimusForge;

internal sealed class MemoryFailureNoticeOwner
{
 internal bool Active; private MemoryFailureNoticePort _port; internal void Bind(MemoryFailureNoticePort port){_port=port;}
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

    internal void PublishMemoryFailureNotice(string title, string message, long generation)
    {
        _port.Observe(title, message);
        if (!_port.IsCurrentOwner(generation) || Volatile.Read(ref Active)) return;
        MemoryFailureNotice pending = Volatile.Read(ref _pendingMemoryFailureNotice);
        if (pending == null || !SaveRuntimeGuard.IsCurrentGeneration(pending.Generation))
        {
            // First valid failure wins. A retired save's slot cannot suppress the current save.
            Interlocked.CompareExchange(ref _pendingMemoryFailureNotice,
                new MemoryFailureNotice(title, message, generation), pending);
        }
        if (_port.IsMainThread()) ProcessPendingMemoryFailureNotice();
    }

    internal void ProcessPendingMemoryFailureNotice()
    {
        if (!_port.IsMainThread()) return;
        MemoryFailureNotice notice = Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);
        if (notice == null || !_port.IsCurrentOwner(notice.Generation) || Active
            || !_port.IsCurrentCampaign()) return;
        long revision = Interlocked.Increment(ref _memoryFailurePopupRevision);
        try
        {
            Volatile.Write(ref Active, true);
            _port.Show(notice.Title, notice.Message, () => CompleteMemoryFailureNotice(notice.Generation, revision));
        }
        catch (Exception)
        {
            // ShowInquiry can fail after publishing a callback; only this presentation may be released.
            CompleteMemoryFailureNotice(notice.Generation, revision);
            _port.Observe(notice.Title, "memory failure inquiry could not be displayed");
        }
    }

    internal void CompleteMemoryFailureNotice(long generation, long revision)
    {
        if (!_port.IsMainThread() || !_port.IsCurrentOwner(generation)
            || revision != Interlocked.Read(ref _memoryFailurePopupRevision)
            || !_port.IsCurrentCampaign()) return;
        Volatile.Write(ref Active, false);
        Interlocked.Increment(ref _memoryFailurePopupRevision);
    }

    internal void ResetMemoryFailureNotices()
    {
        Interlocked.Exchange(ref _pendingMemoryFailureNotice, null);
        Interlocked.Increment(ref _memoryFailurePopupRevision);
        Volatile.Write(ref Active, false);
    }

}

internal sealed class MemoryFailureNoticePort {internal Func<long,bool> IsCurrentOwner;internal Func<bool> IsCurrentCampaign,IsMainThread;internal Action<string,string> Observe;internal Action<string,string,Action> Show;}
