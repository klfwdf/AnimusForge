namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal struct TickSource : IWorldDiplomacyTickSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal TickSource(WorldDiplomacyBehavior owner) => _owner = owner;
        public bool HasOwner => _owner != null;
        public bool IsEnabled => IsWorldDiplomacyEnabled();
        public bool DisabledStateApplied => _owner._disabledStateApplied;
        public void ProcessComposePopup() => WorldDiplomacyBehavior.ProcessComposePopup();
        public void ApplyDisabledState() => _owner.HandleDisabledState();
        public void ClearDisabledState() => _owner._disabledStateApplied = false;
        public void ProcessCompletedJobs() => _owner.ProcessCompletedJobs();
        public void TryScheduleTokenCompression() => _owner.TryScheduleTokenCompression();
        public void TryStartNextLlmJob() => _owner.TryStartNextLlmJob();
        public void TryPublishPendingNotifications() => _owner.TryPublishPendingNotifications();
    }
}
