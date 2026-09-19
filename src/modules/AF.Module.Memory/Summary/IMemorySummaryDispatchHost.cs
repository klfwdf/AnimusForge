using System;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Same-DLL host seam for the memory dispatcher, not a public sub-MOD API.
/// The dispatcher owns queue/budget state; the host owns engine identity and settings.
/// </summary>
internal interface IMemorySummaryDispatchHost
{
    bool IsMainThread { get; }

    // May run on a submitting worker. Must not read Campaign/Hero/Agent properties.
    bool IsOwnerGenerationCurrent(long generation);

    // Runs only after the main-thread check, immediately before executing work.
    bool IsExecutionContextCurrent();

    // Main-thread reads only; preserve the existing dynamic setting semantics.
    double GetBudgetMilliseconds();
    void ReportFailure(Exception error);
}
