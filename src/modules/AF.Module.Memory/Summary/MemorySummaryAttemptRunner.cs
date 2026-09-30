using System;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Runtime;

// Owns the existing per-job retry sequence, not HTTP transport or persisted state.
// Host callbacks dispatch validation, parsing and all game-dependent work to the
// existing main-thread MemorySummaryDispatcher. No live game object is stored here.
internal static class MemorySummaryAttemptRunner
{
    // Called only inside one accepted dispatcher operation. Validation is lazy so
    // retired/obsolete receipts never rescan live source or enter a writer.
    internal static bool Accept(bool hasJob, bool obsolete, Func<bool> isSourceCurrent,
        bool succeeded, Func<bool> applySuccess, Action recordFailure)
    {
        if (!hasJob || obsolete || !isSourceCurrent()) return false;
        if (succeeded) return applySuccess();
        recordFailure();
        return false;
    }

    internal readonly struct Receipt
    {
        internal readonly bool Current;
        internal readonly bool Parsed;
        internal readonly int? RetryAfterSeconds;
        internal Receipt(bool current, bool parsed, int? retryAfterSeconds)
        { Current = current; Parsed = parsed; RetryAfterSeconds = retryAfterSeconds; }
    }

    internal enum Outcome { Completed, Exhausted, Obsolete }

    internal static async Task<Outcome> RunAsync(int maxAttempts,
        Func<Task<bool>> validateBeforeRetry, Func<bool> isOwnerCurrent,
        Func<Task<Receipt>> executeAttempt, Func<int, Task> delayMilliseconds)
    {
        if (validateBeforeRetry == null) throw new ArgumentNullException(nameof(validateBeforeRetry));
        if (isOwnerCurrent == null) throw new ArgumentNullException(nameof(isOwnerCurrent));
        if (executeAttempt == null) throw new ArgumentNullException(nameof(executeAttempt));
        if (delayMilliseconds == null) throw new ArgumentNullException(nameof(delayMilliseconds));
        int attempts = Math.Max(1, maxAttempts);
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            if (attempt > 1 && !await validateBeforeRetry().ConfigureAwait(false)) return Outcome.Obsolete;
            if (!isOwnerCurrent()) return Outcome.Obsolete;
            Receipt receipt = await executeAttempt().ConfigureAwait(false);
            if (!receipt.Current) return Outcome.Obsolete;
            if (receipt.Parsed) return Outcome.Completed;
            // Preserve the prior delay formula and the original maxAttempts guard.
            if (attempt < maxAttempts)
                await delayMilliseconds(receipt.RetryAfterSeconds.HasValue
                    ? Math.Max(1000, receipt.RetryAfterSeconds.Value * 1000) : 1500).ConfigureAwait(false);
        }
        return Outcome.Exhausted;
    }
}
