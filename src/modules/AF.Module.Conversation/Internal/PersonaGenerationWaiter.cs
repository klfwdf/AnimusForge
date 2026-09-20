using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Runtime;

// A caller may stop waiting without cancelling shared persona generation needed by another channel.
// The producer retains its own owner/save guards. There is no queue and no game-object access here.
internal static class PersonaGenerationWaiter
{
    internal static async Task<bool> WaitAsync(Task generation, Func<Task<bool>> isCurrent, Func<bool> hasTime)
    {
        if (generation == null) throw new ArgumentNullException(nameof(generation));
        bool consumed = false;
        try
        {
            while (!generation.IsCompleted)
            {
                if (!hasTime() || !await isCurrent().ConfigureAwait(false)) return false;
                await Task.WhenAny(generation, Task.Delay(500)).ConfigureAwait(false);
            }
            consumed = true;
            await generation.ConfigureAwait(false);
            return true;
        }
        finally
        {
            if (!consumed)
                _ = generation.ContinueWith(failed => { _ = failed.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
