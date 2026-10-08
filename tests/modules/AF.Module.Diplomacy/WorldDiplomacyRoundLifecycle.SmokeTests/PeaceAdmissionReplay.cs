using AnimusForge;
using Newtonsoft.Json.Linq;
internal static class PeaceAdmissionReplay
{
    internal sealed class Port : IWorldDiplomacyPeaceAdmissionPort
    {
        internal bool War = true, Ruler = true;
        internal int Reads, Count = 3, Cap = 100;
        internal string Owner = "a";
        internal float Score = 95;
        internal readonly List<WorldDiplomacyCessionCandidate> Lost = new();
        internal readonly List<WorldDiplomacyCessionCandidate> Owned = new();
        public string KingdomId(string id) { Reads++; return id is "a" or "b" ? id : null; }
        public bool AtWar(string first, string second) => War;
        public string SettlementId(string id) => id is "castle" or "town" ? id : null;
        public string SettlementOwner(string id) => Owner;
        public bool HasRuler(string id) => Ruler;
        public float CessionScore(string first, string second, string from) => Score;
        public IEnumerable<WorldDiplomacyCessionCandidate> LostSettlements(string original, string current) { Reads++; return Lost; }
        public IEnumerable<WorldDiplomacyCessionCandidate> OwnedSettlements(string owner, string receiver) { Reads++; return Owned; }
        public int FiefCount(string id) => Count;
        public float CastleThreshold => 90;
        public float TownThreshold => 95;
        public int MaxCandidates => 5;
        public int ClampTribute(string payer, int amount) => Math.Min(Cap, amount);
        public int ResolveDuration(string token, bool hasTribute) => token == "0" ? (hasTribute ? 100 : 0) : int.Parse(token);
    }
    internal static void Run()
    {
        var p = new Port();
        foreach (string raw in new[] { "{}", "{\"peace_terms\":null}", "{\"peace_terms\":[]}" })
            Test.True(WorldDiplomacyPeaceAdmissionApplication.ParseAndValidatePeaceTerms(p, JObject.Parse(raw), "a", "b") == null && p.Reads == 0,
                "malformed peace shape rejects before live party or candidate reads");
        var json = JObject.Parse("{\"peace_terms\":{\"tribute_payer_kingdom_id\":\"a\",\"tribute_receiver_kingdom_id\":\"b\",\"daily_tribute\":150}}");
        var terms = WorldDiplomacyPeaceAdmissionApplication.ParseAndValidatePeaceTerms(p, json, "a", "b");
        Test.True(terms.DailyTribute == 150 && terms.DurationDays == 100 && terms.TributePayerKingdomId == "a", "parsing preserves explicit amount and defaults only omitted duration");
        p.War = false; p.Reads = 0;
        Test.True(WorldDiplomacyPeaceAdmissionApplication.ParseAndValidatePeaceTerms(p, json, "a", "b") == null && p.Reads == 0,
            "peaceful pair cannot produce war settlement terms");
        p.War = true;
        p.Lost.Add(new("town", "a", false, true, true, false));
        p.Owned.Add(new("castle", "a", true, false, true, false));
        p.Owned.Add(new("town", "a", false, true, true, false));
        p.Owned.Add(new("siege", "a", true, false, true, true));
        Test.True(WorldDiplomacyPeaceAdmissionApplication.BuildCessionCandidates(p, "a", "b", 94).SequenceEqual(new[] { "castle" }), "castle threshold cannot unlock towns or besieged holdings");
        Test.True(WorldDiplomacyPeaceAdmissionApplication.BuildCessionCandidates(p, "a", "b", 95).SequenceEqual(new[] { "town", "castle" }), "lost holdings precede culture matches and duplicates are removed");
        p.Count = 1;
        Test.True(WorldDiplomacyPeaceAdmissionApplication.BuildCessionCandidates(p, "a", "b", 100).Count == 0, "last fief is never offered for cession");
        p.Count = 8;
        for (int i = 0; i < 8; i++) p.Owned.Add(new("extra" + i, "a", false, false, true, false));
        Test.True(WorldDiplomacyPeaceAdmissionApplication.BuildCessionCandidates(p, "a", "b", 100).Count == 5, "cession selection retains the five-candidate bound");
        var offer = new WorldDiplomacyRoundOffer { SourceActionId = "peace" };
        var source = new WorldDiplomacyDocument { AuthorKingdomId = "a", IsReadyForPublication = true,
            Actions = new() { new() { ActionId = "peace", PeaceTerms = terms } } };
        Test.True(WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, offer, source, "a", "b"), "exact published action retains executable terms");
        p.Cap = 50;
        Test.True(WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, offer, source, "a", "b"), "AI suggested tribute cap does not rewrite or invalidate explicit negotiated amount");
        p.Cap = 100; offer.SourceActionId = "missing";
        Test.True(!WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, offer, source, "a", "b"), "unknown action cannot borrow another action's peace terms");
        offer.SourceActionId = "peace"; terms.CessionFromKingdomId = "a"; terms.CessionToKingdomId = "b"; terms.CessionSettlementId = "castle";
        var invalidJson = JObject.Parse("{\"peace_terms\":{\"tribute_payer_kingdom_id\":\"outside\",\"tribute_receiver_kingdom_id\":\"b\",\"daily_tribute\":150,\"duration_days\":0,\"cession_from_kingdom_id\":\"a\",\"cession_to_kingdom_id\":\"b\",\"cession_settlement_id\":\"missing\"}}");
        var invalidTerms = WorldDiplomacyPeaceAdmissionApplication.ParseAndValidatePeaceTerms(p, invalidJson, "a", "b");
        Test.True(invalidTerms.DailyTribute == 150 && invalidTerms.DurationDays == 0
            && invalidTerms.TributePayerKingdomId == "outside" && invalidTerms.CessionSettlementId == "missing",
            "invalid explicit clauses remain visible rather than disappearing or becoming defaults");
        var invalidSource = new WorldDiplomacyDocument { AuthorKingdomId = "a", IsReadyForPublication = true, PeaceTerms = invalidTerms };
        Test.True(!WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, new(), invalidSource, "a", "b"),
            "invalid clauses cannot execute as a reduced peace agreement");
        p.Owner = "b";
        Test.True(!WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, offer, source, "a", "b"), "changed land ownership invalidates the offered cession");
        p.Owner = "a"; p.Ruler = false;
        Test.True(!WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(p, offer, source, "a", "b"), "cession requires a current receiving ruler");
    }
}
