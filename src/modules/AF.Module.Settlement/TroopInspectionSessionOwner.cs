namespace AnimusForge.Refactor.Modules;

// Owns transient inspection admission and teardown state; game resources stay in the host.
internal sealed class TroopInspectionSessionOwner<TRuntime, TSelection, TMission>
    where TRuntime : class where TSelection : class where TMission : class
{
    internal TRuntime Runtime { get; set; }
    internal TSelection PendingSelection { get; set; }
    internal TMission ActiveMission { get; set; }
    internal bool IsOpening { get; set; }
    internal bool CleanupDone { get; set; }
    internal bool Queued { get; private set; }
    internal float QueuedAt { get; private set; }
    internal bool NeedsEngineTick => Queued || Runtime != null || IsOpening;

    internal void BeginLocalSelection()
    {
        IsOpening = true;
        CleanupDone = false;
        ActiveMission = null;
        Runtime = null;
    }

    internal void BeginExternalPreparation()
    {
        CleanupDone = false;
        ActiveMission = null;
        PendingSelection = null;
    }

    internal void Queue(float now, float delaySeconds)
    {
        Queued = true;
        QueuedAt = now + delaySeconds;
    }

    internal bool IsQueuedOpenReady(float now, bool missionActive)
    {
        if (!Queued) return false;
        if (Runtime == null)
        {
            Queued = false;
            return false;
        }
        return now >= QueuedAt && !missionActive;
    }

    internal bool BeginQueuedOpen()
    {
        if (!Queued || Runtime == null) return false;
        Queued = false;
        IsOpening = true;
        return true;
    }

    internal void ResetPendingSelection()
    {
        PendingSelection = null;
        IsOpening = false;
        Queued = false;
    }

    internal bool BeginCleanup()
    {
        bool alreadyDone = CleanupDone;
        CleanupDone = true;
        ActiveMission = null;
        return alreadyDone;
    }

    internal void ReleaseTransient()
    {
        PendingSelection = null;
        Queued = false;
        Runtime = null;
    }

    internal void ResetForNewCampaign()
    {
        Runtime = null;
        PendingSelection = null;
        ActiveMission = null;
        IsOpening = false;
        Queued = false;
        QueuedAt = 0f;
        CleanupDone = false;
    }
}
