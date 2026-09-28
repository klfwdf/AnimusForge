using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private sealed class JobPreparationPort : IWorldDiplomacyJobPreparationPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal JobPreparationPort(WorldDiplomacyBehavior owner) { _owner = owner; }
        public WorldDiplomacyStorage Storage => _owner._storage;
        public int GenerationMaxTokens => WorldDiplomacyBehavior.GenerationMaxTokens;
        public int AnalysisMaxTokens => WorldDiplomacyBehavior.AnalysisMaxTokens;
        public (int minimum, int maximum) CharacterRange() { GetDiplomaticDeclarationCharacterRange(out int min, out int max); return (min, max); }
        public bool KingdomExists(string id) => ResolveKingdom(id) != null;
        public List<string> SettlementTargets(WorldDiplomacyRound round, string author) => _owner.GetResultSettlementActionableTargets(round, ResolveKingdom(author)).Select(x => x.StringId).ToList();
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public string CommonContract(WorldDiplomacyRound round) => _owner.GetCommonDiplomacyContract(round);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public string RelayPrompt(WorldDiplomacyRound round, WorldDiplomacyJob job, WorldDiplomacyDocument source)
            => _owner.BuildRelayConversationTurnPrompt(round, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), prioritySource: source, priorityResponseOnly: job.IsExternalResponseOnly);
        public string GenerationPrompt(WorldDiplomacyJob job, WorldDiplomacyExchange exchange, WorldDiplomacyDocument source, List<string> candidates)
            => _owner.BuildGenerationPrompt(ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), exchange, job.IsResponse, source, job.IsReminder, job.RoundId, job.AllowUntargeted, candidates, job.IsExternalResponseOnly);
        public string LegalSignature(WorldDiplomacyJob job) => _owner.BuildGenerationLegalActionSignature(job);
        public void CaptureHistory(WorldDiplomacyJob job) => _owner.CaptureCanonicalHistoryForJob(job, syncSources: false);
        public string AnalysisPrompt(WorldDiplomacyDocument document) => _owner.BuildAnalysisPrompt(document);
        public string RoundPlanSystemPrompt(WorldDiplomacyRound round) => _owner.BuildRoundPlanSystemPrompt(round);
        public string RoundPlanPrompt(WorldDiplomacyDocument document, List<string> candidates) => _owner.BuildRoundPlanPrompt(document, candidates);
        public bool TryBuildProfile(string authorId, string marker, out string prompt) => TryBuildKingdomStrategicProfilePrompt(ResolveKingdom(authorId), marker, out prompt);
        public void LogProfile(WorldDiplomacyJob job, string prompt) => LogKingdomStrategicProfileInjection(job, prompt);
    }
}
