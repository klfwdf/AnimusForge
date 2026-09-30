using TaleWorlds.CampaignSystem;

namespace AnimusForge
{
    // Only the surrounding engine/identity shell is substituted. OfferActionPort
    // itself is source-linked from production so receipt mapping is exercised.
    public sealed partial class WorldDiplomacyBehavior
    {
        internal static IWorldDiplomacyOfferActionPort NewOfferPort() => new OfferActionPort(new());
        private readonly WorldDiplomacyStorage _storage = new();
        private static int CurrentDay() => 1;
        private WorldDiplomacyRound ResolveRound(string id) => new();
        private WorldDiplomacyDocument ResolveDocument(string id) => new();
        private static Kingdom ResolveKingdom(string id) => Kingdom.All.FirstOrDefault(k => k.StringId == id)!;
        private static string KingdomName(Kingdom kingdom) => kingdom?.StringId ?? "";
        private static void RunDiplomaticAction(string source, Action action) => action();
        private static void Log(string message) { }
        private WorldDiplomacyCessionReceipt TryApplyValidatedCession(WorldDiplomacyPeaceTerms terms, Kingdom first, Kingdom second)
            => new(false, false, true, "");
    }
    internal sealed class WorldDiplomacyStorage { }
    internal sealed class WorldDiplomacyRound { }
    internal sealed class WorldDiplomacyDocument { }
    internal sealed class WorldDiplomacyRoundOffer { public string ProposerKingdomId = "", TargetKingdomId = ""; }
}
namespace AnimusForge.Refactor.Domain
{
    internal static class WorldDiplomacyIntentVocabulary { internal static string NormalizeIntent(string value) => value; }
}
