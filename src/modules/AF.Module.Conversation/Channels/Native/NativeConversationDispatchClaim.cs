using System.Threading;

namespace AnimusForge;

internal enum NativeConversationDispatchState
{
    Queued = 0,
    Started = 1,
    ExpiredBeforeStart = 2
}

/// <summary>
/// The callback and timeout/retirement compete for a single queued operation.
/// A started operation cannot expire: its actual result (including unknown-after-start)
/// must settle the request. This is not an action receipt and does not authorize retries.
/// </summary>
internal struct NativeConversationDispatchClaim
{
    // Keep this value in the request's shared closure; never copy it after publication.
    // The value type replaces the previous captured int without another heap allocation.
    private int _state;

    internal bool TryStart() => Interlocked.CompareExchange(ref _state,
        (int)NativeConversationDispatchState.Started,
        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued;

    internal bool TryExpireBeforeStart() => Interlocked.CompareExchange(ref _state,
        (int)NativeConversationDispatchState.ExpiredBeforeStart,
        (int)NativeConversationDispatchState.Queued) == (int)NativeConversationDispatchState.Queued;
}
