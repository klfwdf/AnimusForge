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
        public void ClearDisabledState() => _owner._runtime.DisabledStateApplied = false;
        public void ProcessComposePopup() => WorldDiplomacyBehavior.ProcessComposePopup();
    }
}
