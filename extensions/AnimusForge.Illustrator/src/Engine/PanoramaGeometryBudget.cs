using System;

namespace AnimusForge.Illustrator.Engine
{
    internal static class PanoramaGeometryBudget
    {
        internal static void BeforeCopy(int copied, int limit, string phase)
        {
            if (copied >= limit) throw new PanoramaGeometryBudgetExceededException(phase, limit);
        }
    }
    // Only the rendered geometry-copy ceiling permits an optional panorama fallback.
    // Traversal, scene lifetime, cancellation and rendering failures keep their original errors.
    internal sealed class PanoramaGeometryBudgetExceededException : InvalidOperationException
    {
        internal string Phase { get; }
        internal int Limit { get; }
        internal PanoramaGeometryBudgetExceededException(string phase, int limit)
            : base("环境网格副本超过" + limit + "个预算，已放弃整个环境全景。")
        { Phase = phase; Limit = limit; }
    }
}
