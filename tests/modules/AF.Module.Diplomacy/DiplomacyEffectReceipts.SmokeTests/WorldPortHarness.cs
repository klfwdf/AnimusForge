using TaleWorlds.CampaignSystem;

namespace AnimusForge
{
    // Only the surrounding engine/identity shell is substituted. OfferActionPort
    // itself is source-linked from production so receipt mapping is exercised.
    public sealed partial class WorldDiplomacyBehavior
    {
        internal static IWorldDiplomacyImmediateActionPort NewImmediatePort() => new ImmediateActionPort(new());
        private bool CanDeclareWar(Kingdom a,Kingdom b,out string reason,bool enforcing,bool playerAuthored = false) { reason="";return !Engine.AtWar; }
        private static bool CanAiAuthorDiplomaticDocument(Kingdom a,out string reason) { reason="";return true; }
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
    internal sealed class WorldDiplomacyStorage { internal List<object> DiplomaticThreats=new(); internal object WarPressure=new(); internal Dictionary<string,int> LastOffensiveWarDayByKingdom=new(); }
    internal sealed class WorldDiplomacyRound { }
    internal sealed class WorldDiplomacyDocument { internal string DocumentId="",MechanicalResult=""; internal bool IsPlayerAuthored,ChangedDiplomaticState; }
    internal sealed class WorldDiplomacyRoundOffer { public string ProposerKingdomId = "", TargetKingdomId = ""; }
}
namespace AnimusForge.Refactor.Domain
{
    internal static class WorldDiplomacyRoundLifecycleRules { internal static bool IsEnforcingRejectedUltimatum(object threats,string a,string b)=>false; }
    internal static class WorldDiplomacyWarPressureRules { internal static void ClearWarPressure(object pressure,string a,string b,int day) { } }
    internal static class WorldDiplomacyIntentVocabulary { internal static string NormalizeIntent(string value) => value; }
}
