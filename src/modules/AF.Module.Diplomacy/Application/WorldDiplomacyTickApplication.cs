using System.Diagnostics;

namespace AnimusForge;

internal interface IWorldDiplomacyTickSource
{
    bool HasOwner { get; }
    bool IsEnabled { get; }
    bool DisabledStateApplied { get; }
    bool MonitorTimings { get; }
    void ReportSlowPhase(string phase, double milliseconds);
    void AdvanceMaterialCapture();
    void ProcessComposePopup();
    void ClearDisabledState();
}

internal static class WorldDiplomacyTickApplication
{
    internal static void Run<TSource>(ref TSource source, IWorldDiplomacyOrchestration orchestration)
        where TSource : struct, IWorldDiplomacyTickSource
    {
        if (!source.HasOwner || orchestration == null) return;
        bool monitor = source.MonitorTimings;
        long started = monitor ? Stopwatch.GetTimestamp() : 0;
        source.AdvanceMaterialCapture();
        if (monitor) Measure(ref source, "material_capture", ref started);
        (orchestration as WorldDiplomacyOrchestration)?.AdvanceEventRuntime();
        if (monitor) Measure(ref source, "event_runtime", ref started);
        source.ProcessComposePopup();
        if (monitor) Measure(ref source, "compose_popup", ref started);
        if (!source.IsEnabled)
        {
            if (!source.DisabledStateApplied) orchestration.HandleDisabledState();
            orchestration.ProcessCompletedJobs();
            if (monitor) Measure(ref source, "disabled_completed", ref started);
            return;
        }
        source.ClearDisabledState();
        orchestration.ProcessCompletedJobs();
        if (monitor) Measure(ref source, "completed_jobs", ref started);
        orchestration.TryScheduleTokenCompression();
        if (monitor) Measure(ref source, "compression_schedule", ref started);
        orchestration.TryStartNextLlmJob();
        if (monitor) Measure(ref source, "dispatch", ref started);
        orchestration.PollNotifications();
        if (monitor) Measure(ref source, "notifications", ref started);
    }

    private static void Measure<TSource>(ref TSource source, string phase, ref long started)
        where TSource : struct, IWorldDiplomacyTickSource
    {
        long now = Stopwatch.GetTimestamp();
        double milliseconds = (now - started) * 1000.0 / Stopwatch.Frequency;
        if (milliseconds >= 25) source.ReportSlowPhase(phase, milliseconds);
        started = Stopwatch.GetTimestamp();
    }
}
