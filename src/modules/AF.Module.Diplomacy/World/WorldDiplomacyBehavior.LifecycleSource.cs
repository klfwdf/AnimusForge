namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal readonly struct LifecycleSource : IWorldDiplomacyLifecycleSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal LifecycleSource(WorldDiplomacyBehavior owner) => _owner = owner;
        public bool StartAtPeace => IsWorldDiplomacyEnabled() && ShouldStartNewGameAtPeace();
        public void ResetTransientRuntime(string reason) => _owner?.ResetTransientRuntime(reason);
    }
}
