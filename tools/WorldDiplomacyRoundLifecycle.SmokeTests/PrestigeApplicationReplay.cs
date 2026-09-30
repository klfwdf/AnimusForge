using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
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
        internal bool Fail, UnknownAfterEffect, ReadUnavailable;
        internal Action BeforeEffect;
        public bool CampaignAvailable => true;
        public IEnumerable<string> KingdomIds(bool activeOnly) { Scans++; yield return "k"; }
        public WorldDiplomacyPrestigeCourt CaptureCourt(string id) => id == "k" ? Court : null!;
        public bool HasHero(string id) => Heroes.Contains(id);
        public bool TryReadRelation(string first, string second, out int value)
        { value = Relations.GetValueOrDefault(first + "|" + second); return !ReadUnavailable; }
        public WorldDiplomacyRelationEffectReceipt ChangeRelationAndMeasure(string first, string second, int delta)
        {
            BeforeEffect?.Invoke();
            Calls++;
            if (Fail) return new(true, 0);
            string key = first + "|" + second;
            int before = Relations.GetValueOrDefault(key);
            int after = Math.Clamp(before + delta, -100, 100);
            Relations[key] = after;
            return new(!UnknownAfterEffect, after - before);
        }
        public string KingdomName(string id) => id;
        public int CurrentDay() => Day;
        public void Log(string message) { }
    }
    internal static void Run()
    {
        WorldDiplomacyStorage storage = new();
        var port = new Port();
        var doc = new WorldDiplomacyDocument { DocumentId = "d" };
        port.BeforeEffect = () => Test.True(storage.NationalPrestigeByKingdom["k"] == 50
            && doc.DiplomaticStandingChanges.Count == 1,
            "Application commits the prestige decision before invoking relation effects");
        WorldDiplomacyPrestigeApplication.Apply(ref storage, port, " k ", -50, doc, "reason");
        port.BeforeEffect = null;
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

        restored.InternationalReputationByKingdom["k"] = 95;
        var older = new WorldDiplomacyDocument { DocumentId = "old", AuthorKingdomId = "k", Day = 1,
            IsReadyForPublication = true, InternationalReputationEvaluationDelta = 10 };
        var newer = new WorldDiplomacyDocument { DocumentId = "new", AuthorKingdomId = "k", Day = 2,
            IsReadyForPublication = true, InternationalReputationEvaluationDelta = -10 };
        restored.Documents = new List<WorldDiplomacyDocument> { newer, older };
        WorldDiplomacyPrestigeApplication.RecoverDocuments(ref restored, port);
        Test.True(restored.InternationalReputationByKingdom["k"] == 90
            && older.InternationalReputationSettled && newer.InternationalReputationSettled,
            "real Application recovery settles chronological input order even when storage is reversed");
        WorldDiplomacyPrestigeApplication.RecoverDocuments(ref restored, port);
        Test.True(restored.InternationalReputationByKingdom["k"] == 90,
            "repeating recovery must not reapply document deltas");

        var recovery = new WorldDiplomacyStorage();
        var recoveryPort = new Port { UnknownAfterEffect = true };
        WorldDiplomacyPrestigeApplication.Apply(ref recovery, recoveryPort, "k", -50, null, "test");
        int observed = recoveryPort.Relations["v|r"];
        Test.True(observed != 0 && recovery.NationalPrestigeRelationModifiers[0].PendingEffect != null
            && recovery.NationalPrestigeRelationModifiers[0].AppliedAmount == 0,
            "unknown effect retains evidence without pretending it was applied");
        var afterLoad = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(recovery))!;
        afterLoad.NationalPrestigeRelationModifiers = WorldDiplomacyRoundLifecycleRules.SelectRetainedPrestigeRelationModifiers(afterLoad.NationalPrestigeRelationModifiers);
        recoveryPort.ReadUnavailable = true;
        WorldDiplomacyPrestigeApplication.Reconcile(afterLoad, recoveryPort, "k");
        Test.True(recoveryPort.Calls == 1, "unreadable pending effect cannot be reissued after save/load");
        recoveryPort.ReadUnavailable = false;
        WorldDiplomacyPrestigeApplication.Reconcile(afterLoad, recoveryPort, "k");
        Test.True(recoveryPort.Calls == 1 && recoveryPort.Relations["v|r"] == observed
            && afterLoad.NationalPrestigeRelationModifiers[0].AppliedAmount == observed
            && afterLoad.NationalPrestigeRelationModifiers[0].PendingEffect == null,
            "confirmed pending poststate updates the ledger without repeating its effect");
        Test.True(!JsonConvert.SerializeObject(afterLoad.NationalPrestigeRelationModifiers[0]).Contains("pendingEffect", StringComparison.Ordinal),
            "ordinary confirmed records keep the old serialized field set");
        Test.True(JsonConvert.DeserializeObject<WorldDiplomacyPrestigeRelationModifier>("{\"kingdomId\":\"k\",\"appliedAmount\":0}")!.PendingEffect == null,
            "old records default to no pending effect");
        var pending = recovery.NationalPrestigeRelationModifiers[0];
        recovery.NationalPrestigeRelationModifiers.Add(new() { KingdomId = "k", RulerHeroId = "r", VassalLeaderHeroId = "v" });
        recovery.NationalPrestigeRelationModifiers = WorldDiplomacyRoundLifecycleRules.SelectRetainedPrestigeRelationModifiers(recovery.NationalPrestigeRelationModifiers);
        Test.True(ReferenceEquals(recovery.NationalPrestigeRelationModifiers.Single(), pending),
            "duplicate normalizer cannot discard unknown effect evidence");
        recoveryPort.Court = new("k", false, null!, Array.Empty<string>());
        recoveryPort.Heroes.Remove("r");
        WorldDiplomacyPrestigeApplication.Reconcile(recovery, recoveryPort, "k");
        Test.True(recovery.NationalPrestigeRelationModifiers.Count == 1 && pending.PendingEffect != null,
            "stale zero-booked unknown effect survives missing ruler");
    }
}
