using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Process-local, bounded duplicate suppression for detached memory commits.
/// Receipts are not save data and do not replace MyBehavior's persistence
/// authority. The cache is touched once per interaction commit, never from a
/// tick or a prompt-generation loop.
/// </summary>
public static class MemoryCommitReceiptCache
{
    private const int Capacity = 512;
    private static readonly object Sync = new object();
    private static readonly HashSet<string> Committed = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Queue<string> Order = new Queue<string>();

    public static bool TryAccept(string commitId)
    {
        if (string.IsNullOrWhiteSpace(commitId))
        {
            return false;
        }

        string normalized = commitId.Trim();
        lock (Sync)
        {
            if (!Committed.Add(normalized))
            {
                return false;
            }

            Order.Enqueue(normalized);
            while (Order.Count > Capacity)
            {
                Committed.Remove(Order.Dequeue());
            }
            return true;
        }
    }

    public static bool Contains(string commitId)
    {
        if (string.IsNullOrWhiteSpace(commitId))
        {
            return false;
        }

        lock (Sync)
        {
            return Committed.Contains(commitId.Trim());
        }
    }

    public static void ClearForTests()
    {
        lock (Sync)
        {
            Committed.Clear();
            Order.Clear();
        }
    }
}
