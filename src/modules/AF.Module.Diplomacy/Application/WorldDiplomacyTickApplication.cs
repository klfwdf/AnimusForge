namespace AnimusForge;

internal interface IWorldDiplomacyTickSource
{
    bool HasOwner { get; }
    bool IsEnabled { get; }
    bool DisabledStateApplied { get; }
    void ProcessComposePopup();
    void ClearDisabledState();
}

internal static class WorldDiplomacyTickApplication
{
    internal static void Run<TSource>(ref TSource source, IWorldDiplomacyOrchestration orchestration)
        where TSource : struct, IWorldDiplomacyTickSource
    {
        if (!source.HasOwner || orchestration == null) return;
        source.ProcessComposePopup();
        if (!source.IsEnabled)
        {
            if (!source.DisabledStateApplied) orchestration.HandleDisabledState();
            orchestration.ProcessCompletedJobs();
            return;
        }
        source.ClearDisabledState();
        orchestration.ProcessCompletedJobs();
        orchestration.TryScheduleTokenCompression();
        orchestration.TryStartNextLlmJob();
        orchestration.PollNotifications();
    }
}
