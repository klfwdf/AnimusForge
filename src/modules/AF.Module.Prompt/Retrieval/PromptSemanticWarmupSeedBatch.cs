using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Contains only detached strings captured before the background worker starts.
internal sealed class PromptSemanticWarmupSeedBatch
{
    internal long Revision { get; }
    internal IReadOnlyList<string> Seeds { get; }

    internal PromptSemanticWarmupSeedBatch(long revision, IEnumerable<string> seeds)
    {
        Revision = revision;
        Seeds = Array.AsReadOnly((seeds ?? Enumerable.Empty<string>()).ToArray());
    }
}

internal readonly struct PromptSemanticWarmupResult
{
    internal int SeedCount { get; }
    internal int Warmed { get; }
    internal bool Stale { get; }

    internal PromptSemanticWarmupResult(int seedCount, int warmed, bool stale)
    {
        SeedCount = seedCount;
        Warmed = warmed;
        Stale = stale;
    }
}

internal static class PromptSemanticWarmupExecutor
{
    internal static PromptSemanticWarmupResult Run(PromptSemanticWarmupSeedBatch batch,
        Func<long> currentRevision, Func<long, string, bool> embed)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));
        if (currentRevision == null) throw new ArgumentNullException(nameof(currentRevision));
        if (embed == null) throw new ArgumentNullException(nameof(embed));

        int warmed = 0;
        for (int i = 0; i < batch.Seeds.Count; i++)
        {
            if (currentRevision() != batch.Revision)
                return new PromptSemanticWarmupResult(batch.Seeds.Count, warmed, true);
            if (embed(batch.Revision, batch.Seeds[i])) warmed++;
        }
        return new PromptSemanticWarmupResult(batch.Seeds.Count, warmed, currentRevision() != batch.Revision);
    }
}
