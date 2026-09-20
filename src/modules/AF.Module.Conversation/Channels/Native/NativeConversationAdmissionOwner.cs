using System;
using System.Threading;

namespace AnimusForge;

/// <summary>
/// Owns the process-local Native reservation and its two independent retirement clocks.
/// The host alone captures/validates game objects on the game thread. This owner never
/// dereferences a Hero, Mission or ConversationManager and creates no persistence keys.
/// </summary>
internal sealed class NativeConversationAdmissionOwner<TTicket> where TTicket : class
{
    private TTicket _current;
    private long _conversationEpoch;
    private long _presentationRevision;

    internal TTicket Current => Volatile.Read(ref _current);
    internal long ConversationEpoch => Interlocked.Read(ref _conversationEpoch);
    internal long PresentationRevision => Interlocked.Read(ref _presentationRevision);

    // Reference identity is intentional: a ticket with equal values is still another request.
    internal bool Owns(TTicket ticket) => ticket != null && ReferenceEquals(Current, ticket);
    internal bool IsConversationEpochCurrent(long epoch) => epoch == ConversationEpoch;
    internal bool IsPresentationCurrent(long revision) => revision == PresentationRevision;

    // Called only after the host's current-context and busy checks. Opening consumption
    // follows reservation; it must not run before the host has rejected a busy request.
    internal void ReserveCaptured(TTicket ticket) => Interlocked.Exchange(ref _current, ticket);

    // A late finally/failed opening may release only itself, never a newer reservation.
    internal void Release(TTicket ticket) => Interlocked.CompareExchange(ref _current, null, ticket);

    // Successful admission advances presentation even within one conversation epoch.
    // UI closure/backend completion do not advance the epoch or erase this revision.
    internal long BeginPresentation() => Interlocked.Increment(ref _presentationRevision);

    // Only the real ConversationEnded event uses this transition. A reused game token
    // must not let an old queued request enter the next conversation.
    internal void EndConversation()
    {
        Interlocked.Increment(ref _conversationEpoch);
        Interlocked.Exchange(ref _current, null);
    }
}
