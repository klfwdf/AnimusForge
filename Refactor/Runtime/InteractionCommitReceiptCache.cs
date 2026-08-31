using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Bounded process-local request receipts, separate from the memory owner's
/// append receipts. Reserve before calling any owner; retain failed outcomes
/// too because an owner may have applied effects before failing. Only terminal
/// entries can be evicted. This is not a durable economic transaction journal.
/// </summary>
internal static class InteractionCommitReceiptCache
{
    internal const int Capacity = 512;
    private static readonly object Sync = new object();
    private static readonly Dictionary<string, Reservation> Entries = new Dictionary<string, Reservation>(StringComparer.Ordinal);
    private static readonly Queue<string> Completed = new Queue<string>();

    internal sealed class Reservation
    {
        internal string Key;
        internal string Fingerprint;
        internal InteractionCommitResult Result;
    }

    internal static bool TryBegin(string key, string fingerprint, out Reservation reservation, out InteractionCommitResult previous)
    {
        lock (Sync)
        {
            reservation = null;
            previous = null;
            if (Entries.TryGetValue(key, out Reservation existing))
            {
                previous = !string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)
                    ? Failure("commit_request_mismatch", existing.Result?.ActionsExecuted ?? false, existing.Result?.HistoryWritten ?? false)
                    : existing.Result == null
                        ? Failure("commit_in_progress")
                        : existing.Result.AsDuplicate();
                return false;
            }
            if (Entries.Count >= Capacity)
            {
                if (Completed.Count == 0)
                {
                    previous = Failure("commit_receipt_capacity");
                    return false;
                }
                Entries.Remove(Completed.Dequeue());
            }
            reservation = new Reservation { Key = key, Fingerprint = fingerprint };
            Entries.Add(key, reservation);
            return true;
        }
    }

    internal static void Complete(Reservation reservation, InteractionCommitResult result)
    {
        lock (Sync)
        {
            if (!Entries.TryGetValue(reservation.Key, out Reservation current)
                || !ReferenceEquals(current, reservation) || current.Result != null)
            {
                throw new InvalidOperationException("Commit receipt reservation is not active.");
            }
            current.Result = result ?? Failure("missing_commit_receipt");
            Completed.Enqueue(reservation.Key);
        }
    }

    internal static void ClearForTests()
    {
        lock (Sync)
        {
            Entries.Clear();
            Completed.Clear();
        }
    }

    private static InteractionCommitResult Failure(string code, bool actionsExecuted = false, bool historyWritten = false)
        => new InteractionCommitResult(InteractionStatus.NonRetryableFailure, historyWritten, actionsExecuted, code);
}
