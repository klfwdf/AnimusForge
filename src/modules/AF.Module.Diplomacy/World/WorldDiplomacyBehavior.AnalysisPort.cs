using Newtonsoft.Json.Linq;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class AnalysisPort : IWorldDiplomacyAnalysisPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal AnalysisPort(WorldDiplomacyBehavior owner) { _owner = owner; Execution = new DocumentExecutionPort(owner); }
        public WorldDiplomacyStorage Storage => _owner._storage;
        public IWorldDiplomacyDocumentExecutionPort Execution { get; }
        public int MaxAutomaticReplyDepth => WorldDiplomacyBehavior.MaxAutomaticReplyDepth;
        public string KingdomName(string id) { var kingdom = ResolveKingdom(id); return kingdom == null ? string.Empty : WorldDiplomacyBehavior.KingdomName(kingdom); }
    }
}
