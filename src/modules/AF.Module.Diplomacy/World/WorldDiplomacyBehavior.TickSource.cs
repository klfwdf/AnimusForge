namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal struct TickSource : IWorldDiplomacyTickSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal TickSource(WorldDiplomacyBehavior owner) => _owner = owner;
        public bool HasOwner => _owner != null;
        public bool IsEnabled => IsWorldDiplomacyEnabled();
        public bool DisabledStateApplied => _owner._runtime.DisabledStateApplied;
        public bool MonitorTimings => PerfProbe.IsDetailedScopeRecordingActive();
        public void ReportSlowPhase(string phase, double milliseconds) => Log("diplomacy.tick_slow_phase phase=" + phase
            + " elapsedMs=" + milliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        public void AdvanceMaterialCapture()
        {
            if (!IsEnabled) { _owner._eventMaterialCaptures.Dispose(); _owner._historyEmbeddings.Dispose(); }
            else _owner._eventMaterialCaptures.Pump(SaveRuntimeGuard.CaptureGeneration(), Log);
        }
        public void ClearDisabledState() => _owner._runtime.DisabledStateApplied = false;
        public void ProcessComposePopup() => WorldDiplomacyBehavior.ProcessComposePopup();
    }
}
