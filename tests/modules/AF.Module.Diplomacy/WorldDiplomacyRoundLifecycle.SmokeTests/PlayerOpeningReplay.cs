using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class SubmissionWorld(WorldDiplomacyOrchestration owner) : IWorldDiplomacyPlayerWorld
    {
        public WorldDiplomacyPlayerContext Player => new(1, "p", true, true, "p");
        public bool KingdomExists(string id) => !string.IsNullOrEmpty(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => owner.ResolveDocument(id);
        public WorldDiplomacyRound ResolveRound(string id) => owner.ResolveRound(id);
        public int CurrentDay() => 40;
    }

    internal static void PlayerOpening(string incidentPath = null)
    {
        PlayerOpeningLifecycle(incidentPath);
        PlayerOpeningLifecycle(incidentPath, true);
        PlayerOpeningRecovery();
        LoreBudgetAndAudit();
        LoreCitationRepair();
    }

}
