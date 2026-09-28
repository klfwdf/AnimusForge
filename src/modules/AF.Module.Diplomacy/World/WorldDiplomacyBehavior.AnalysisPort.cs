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
        public WorldDiplomacyPeaceTerms ParsePeaceTerms(JObject json, string author, string target) => _owner.ParseAndValidatePeaceTerms(json, ResolveKingdom(author), ResolveKingdom(target));
        public string KingdomName(string id) { var kingdom = ResolveKingdom(id); return kingdom == null ? string.Empty : WorldDiplomacyBehavior.KingdomName(kingdom); }
        public void ScheduleSettlement(WorldDiplomacyRound round) => _owner.ScheduleNextResultSettlementTurn(round);
        public void AdvanceRelay(WorldDiplomacyRound round) => _owner.AdvanceRelay(round);
        public void CompleteExchange(string id, string reason) => _owner.CompleteExchange(id, reason);
        public void CloseRound(string reason) => _owner.CloseActiveRound(reason);
    }
}
