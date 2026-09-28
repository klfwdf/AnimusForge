using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class PrestigeApplicationReplay
{
    private sealed class Port : IWorldDiplomacyPrestigePort
    {
        internal WorldDiplomacyPrestigeCourt Court = new("k", false, "r", new[] { "v" });
        internal readonly Dictionary<string, int> Relations = new() { ["v|r"] = 0 };
        internal readonly HashSet<string> Heroes = new() { "r", "v", "new-r" };
        internal int Calls, Scans, Day = 12;
        internal bool Fail;
        public bool CampaignAvailable => true;
        public IEnumerable<string> KingdomIds(bool activeOnly) { Scans++; yield return "k"; }
        public WorldDiplomacyPrestigeCourt CaptureCourt(string id) => id == "k" ? Court : null!;
        public bool HasHero(string id) => Heroes.Contains(id);
        public int ChangeRelationAndMeasure(string first, string second, int delta)
        {
            Calls++;
            if (Fail) return 0;
            string key = first + "|" + second;
            int before = Relations.GetValueOrDefault(key);
            int after = Math.Clamp(before + delta, -100, 100);
            Relations[key] = after;
            return after - before;
        }
        public void ChangeRelation(string first, string second, int delta) => ChangeRelationAndMeasure(first, second, delta);
        public string KingdomName(string id) => id;
        public int CurrentDay() => Day;
        public void Log(string message) { }
    }
    internal static void Run()
    {
        WorldDiplomacyStorage storage = new();
        var port = new Port();
        var doc = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyPrestigeApplication.Apply(ref storage, port, " k ", -50, doc, "reason");
        int desired = WorldDiplomacyReputationRules.GetNationalPrestigeRelationTarget(storage.NationalPrestigeByKingdom["k"]);
        Test.True(storage.NationalPrestigeRelationModifiers.Count == 1 && port.Relations["v|r"] == desired
            && storage.NationalPrestigeRelationModifiers[0].AppliedAmount == desired,
            "prestige change records exact measured relation modifier in the canonical store");
        int calls = port.Calls;
        WorldDiplomacyPrestigeApplication.ReconcileAll(storage, port);
        Test.True(port.Calls == calls && port.Scans == 1, "daily reconciliation skips already satisfied relation effects");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        WorldDiplomacyPrestigeApplication.Reconcile(restored, port, "k");
        Test.True(port.Calls == calls, "reload preserves applied modifier and avoids duplicate relation changes");
        port.Court = new("k", false, "new-r", new[] { "v" });
        WorldDiplomacyPrestigeApplication.Reconcile(restored, port, "k");
        Test.True(port.Relations["v|r"] == 0 && port.Relations["v|new-r"] == desired
            && restored.NationalPrestigeRelationModifiers.Count == 1 && restored.NationalPrestigeRelationModifiers[0].RulerHeroId == "new-r",
            "ruler replacement applies new modifier and reverses old modifier before removing stale state");
        port.Court = new("k", false, null!, Array.Empty<string>()); port.Fail = true;
        WorldDiplomacyPrestigeApplication.Reconcile(restored, port, "k");
        Test.True(restored.NationalPrestigeRelationModifiers.Count == 1,
            "failed stale reversal preserves nonzero recovery record");
        port.Fail = false;
        WorldDiplomacyPrestigeApplication.Reconcile(restored, port, "k");
        Test.True(restored.NationalPrestigeRelationModifiers.Count == 0 && port.Relations["v|new-r"] == 0,
            "retry reverses stale relation exactly once");
        restored.NationalPrestigeRelationModifiers.Add(new() { KingdomId = "k", RulerHeroId = "gone", VassalLeaderHeroId = "v", AppliedAmount = -5 });
        WorldDiplomacyPrestigeApplication.Reconcile(restored, port, "k");
        Test.True(restored.NationalPrestigeRelationModifiers.Count == 0, "missing hero permits stale modifier cleanup without effect");
        restored.InternationalReputationByKingdom["k"] = 80;
        WorldDiplomacyPrestigeApplication.NaturalChange(restored, port, true);
        port.Day += 20;
        WorldDiplomacyPrestigeApplication.NaturalChange(restored, port, true);
        Test.True(restored.InternationalReputationByKingdom["k"] == 80
            && restored.InternationalReputationNaturalChangeLastDayByKingdom["k"] == port.Day,
            "disabled daily anchor advances without applying accumulated drift");
        port.Day++;
        WorldDiplomacyPrestigeApplication.NaturalChange(restored, port, false);
        Test.True(restored.InternationalReputationByKingdom["k"] < 80,
            "enabled daily drift resumes from the anchor");
        port.Court = new("k", false, "r", new[] { "v" });
        calls = port.Calls;
        WorldDiplomacyPrestigeApplication.ApplyZeroPrestigePenalty(port, "k", 0);
        Test.True(port.Calls == calls, "nonnegative breach penalty causes no relation effect");
        WorldDiplomacyPrestigeApplication.ApplyZeroPrestigePenalty(port, "k", -5);
        Test.True(port.Calls == calls + 1 && port.Relations["v|r"] == -5, "zero prestige applies one negative effect per eligible vassal");
    }
}
