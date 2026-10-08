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
        public int AnalysisMaxTokens => WorldDiplomacyLlmClient.GetConfiguredOutputTokenLimit();
        public (int minimum, int maximum) CharacterRange() { GetDiplomaticDeclarationCharacterRange(out int min, out int max); return (min, max); }
        public bool KingdomExists(string id) => ResolveKingdom(id) != null;
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public string CommonContract(WorldDiplomacyRound round) => _owner.GetCommonDiplomacyContract(round);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public bool TryBuildProfile(string authorId, string marker, out string prompt) => TryBuildKingdomStrategicProfilePrompt(ResolveKingdom(authorId), marker, out prompt);
        public void LogProfile(WorldDiplomacyJob job, string prompt) => LogKingdomStrategicProfileInjection(job, prompt);
    }
}
