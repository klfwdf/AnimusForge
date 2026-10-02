namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class ThreatBindingPort : IWorldDiplomacyThreatBindingPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal ThreatBindingPort(WorldDiplomacyBehavior owner) { _owner = owner; }
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public bool IsPolicyActive(string policyId, string ownerId, string affectedId) => DiplomacyModuleServices.Policy.IsForeignPolicySignalActive(policyId, ownerId, affectedId);
        public string RepresentativeId(string id) => ResolveWorldDiplomacyRepresentative(ResolveKingdom(id))?.StringId;
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public string NewId(string kind) => WorldDiplomacyBehavior.NewId(kind);
        public int EscalationPrestigeReward => WarningEscalationPrestigeReward;
        public int WarPrestigeReward => UltimatumWarPrestigeReward;
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
