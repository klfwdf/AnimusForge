namespace AnimusForge;

internal interface IWorldDiplomacyTickSource
{
    bool HasOwner { get; }
    bool IsEnabled { get; }
    bool DisabledStateApplied { get; }
    void ProcessComposePopup();
    void ApplyDisabledState();
    void ClearDisabledState();
    void ProcessCompletedJobs();
    void TryScheduleTokenCompression();
    void TryStartNextLlmJob();
    void TryPublishPendingNotifications();
}

internal static class WorldDiplomacyTickApplication
{
    internal static void Run<TSource>(ref TSource source)
        where TSource : struct, IWorldDiplomacyTickSource
    {
        if (!source.HasOwner) return;
        source.ProcessComposePopup();
        if (!source.IsEnabled)
        {
            if (!source.DisabledStateApplied) source.ApplyDisabledState();
            source.ProcessCompletedJobs();
            return;
        }
        source.ClearDisabledState();
        source.ProcessCompletedJobs();
        source.TryScheduleTokenCompression();
        source.TryStartNextLlmJob();
        source.TryPublishPendingNotifications();
    }
}
