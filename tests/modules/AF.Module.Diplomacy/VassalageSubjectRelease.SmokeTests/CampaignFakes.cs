namespace TaleWorlds.CampaignSystem
{
    internal sealed class Hero { public static Hero MainHero; public bool IsAlive = true; }
    internal sealed class Clan { public Hero Leader; }
    internal sealed class Kingdom { public string StringId; public Clan RulingClan = new(); public bool IsEliminated; }
}
namespace AnimusForge
{
    using TaleWorlds.CampaignSystem;
    // Existing campaign/type/break ports are fakes. The new production release owner is linked unchanged.
    internal sealed class VassalageAgreement
    {
        internal Kingdom Suzerain, Subject;
        public string SuzerainKingdomId => Suzerain.StringId;
        public string VassalKingdomId => Subject.StringId;
        public string ReleaseIdentity = Guid.NewGuid().ToString("N");
        public bool IsValid() => Suzerain != null && Subject != null && Suzerain != Subject;
        public Kingdom ResolveSuzerain() => Suzerain;
        public Kingdom ResolveVassal() => Subject;
    }
    internal static class Logger { public static void Log(string category, string message) { } }
    internal sealed partial class VassalageBehavior
    {
        private readonly Dictionary<string, VassalageAgreement> _agreementsByVassalId = new();
        internal Kingdom Player;
        internal int Breaks;
        internal readonly HashSet<string> Wars = new();
        private static bool IsValidKingdom(Kingdom k) => k != null && !k.IsEliminated;
        private Kingdom GetPlayerKingdom() => Player;
        private VassalageAgreement GetAnyVassalAgreement(Kingdom subject) => subject != null
            && _agreementsByVassalId.TryGetValue(subject.StringId, out var a) && a.IsValid() ? a : null;
        private static string GetKingdomDisplayName(Kingdom k, string fallback) => k?.StringId ?? fallback;
        private void BreakAgreement(VassalageAgreement agreement, string reason, string message)
        { Breaks++; _agreementsByVassalId.Remove(agreement.VassalKingdomId); }
        private bool IsAtWar(Kingdom a, Kingdom b) => a != null && b != null && Wars.Contains(a.StringId + ":" + b.StringId);
        internal VassalageAgreement Sign(Kingdom suzerain, Kingdom subject)
        { var a = new VassalageAgreement { Suzerain = suzerain, Subject = subject }; _agreementsByVassalId[subject.StringId] = a; return a; }
        internal bool HasAgreement(Kingdom subject) => GetAnyVassalAgreement(subject) != null;
        internal bool PendingWarCurrent(string reason, Kingdom suzerain, Kingdom enemy) => IsSubjectWarSyncCurrent(reason, suzerain, enemy);
    }
}
