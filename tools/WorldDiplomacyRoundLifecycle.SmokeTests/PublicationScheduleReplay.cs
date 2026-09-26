using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class PublicationScheduleReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        var document = new WorldDiplomacyDocument { DocumentId = "d", RoundId = "round", Day = 10 };
        storage.Documents.Add(document);
        storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
            { DocumentId = "old", RoundId = "other", SettlementId = "other", Scope = "civilian", DueDay = 4 });
        storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
            { DocumentId = "D", RoundId = "stale", SettlementId = "stale", Scope = "court", DueDay = 5 });
        WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(storage.SettlementKnowledge, "known", "d", 2);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "known-court", "d", 3);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage.KingdomKnowledge, "player", "d", 3);
        var settlements = new List<WorldDiplomacyPropagationApplication.SettlementTarget>
        {
            new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "origin", IsOrigin = true },
            new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "known", Distance = 5 },
            new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "near", Distance = 10 },
            new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "far", Distance = 20 }
        };
        var courts = new List<WorldDiplomacyPropagationApplication.CourtTarget>
        {
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "known-court", SettlementId = "k", Distance = 10 },
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "player", SettlementId = "p", Distance = 10, IsPlayerAffiliated = true },
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "far-court", SettlementId = "f", Distance = 20 }
        };
        var result = WorldDiplomacyPropagationApplication.SchedulePublication(
            storage, document, 10, 4, 6, settlements, 20, courts, 20);
        Test.True(document.PropagationCompleted, "publication marks the canonical document complete");
        Test.True(result.LatestCivilianDueDay == 14 && result.LatestCourtDueDay == 16,
            "publication preserves distance-based latest due dates");
        Test.True(storage.PropagationArrivals.Count == 5 && storage.PropagationArrivals[0].DocumentId == "old",
            "publication replaces old case-insensitive document arrivals without dropping other work");
        Test.True(storage.PropagationArrivals.Where(x => x.DocumentId == "d").Select(x => x.DueDay)
                .SequenceEqual(new[] { 12, 13, 14, 16 }),
            "publication orders civilian and court arrivals by the original due-day rule");
        Test.True(storage.PropagationArrivals.Any(x => x.KingdomId == "player")
                && storage.PropagationArrivals.All(x => x.KingdomId != "known-court"),
            "missing player formal receipt bypasses known-kingdom skip while other known courts stay skipped");
        string saved = JsonConvert.SerializeObject(storage);
        var loaded = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved);
        Test.True(JsonConvert.SerializeObject(loaded) == saved,
            "scheduled arrivals and completion survive a real JSON round trip");

        var zeroStorage = new WorldDiplomacyStorage();
        var zeroDocument = new WorldDiplomacyDocument { DocumentId = "zero", RoundId = "r" };
        var zeroResult = WorldDiplomacyPropagationApplication.SchedulePublication(
            zeroStorage, zeroDocument, 20, 8, 5,
            new List<WorldDiplomacyPropagationApplication.SettlementTarget>
            {
                new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "only", Distance = 0 }
            }, 0,
            new List<WorldDiplomacyPropagationApplication.CourtTarget>
            {
                new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "other", SettlementId = "", Distance = 0 }
            }, 0);
        Test.True(zeroResult.LatestCivilianDueDay == 21 && zeroResult.LatestCourtDueDay == 25
                && zeroStorage.PropagationArrivals.Count == 2,
            "missing origin or zero-distance world uses the original civilian one-day and court maximum fallbacks");

        var recalculateStorage = new WorldDiplomacyStorage();
        recalculateStorage.Documents.Add(new WorldDiplomacyDocument
            { DocumentId = "recalculate", RoundId = "r", AuthorKingdomId = "author", Day = 10 });
        recalculateStorage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
            { DocumentId = "recalculate", RoundId = "r", SettlementId = "near", Scope = "civilian", DueDay = 99 });
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(
            recalculateStorage.KingdomKnowledge, "known", "recalculate", 10);
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(
            recalculateStorage.KingdomKnowledge, "player", "recalculate", 10);
        var recalculateCourts = new List<WorldDiplomacyPropagationApplication.CourtTarget>
        {
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "author", SettlementId = "origin" },
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "known", SettlementId = "near" },
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "player", SettlementId = "near", IsPlayerAffiliated = true },
            new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "other", SettlementId = "far" }
        };
        var maps = new Dictionary<string, WorldDiplomacyPropagationApplication.DistanceSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["recalculate"] = new WorldDiplomacyPropagationApplication.DistanceSnapshot
            {
                MaxCivilianDistance = 20,
                MaxCourtDistance = 20,
                SettlementDistances = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    { ["origin"] = 0, ["near"] = 10, ["far"] = 20 },
                CourtDistances = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    { ["author"] = 0, ["known"] = 10, ["player"] = 10, ["other"] = 20 }
            }
        };
        WorldDiplomacyPropagationApplication.RecalculatePending(
            recalculateStorage, 12, 4, 6, recalculateCourts, d => maps[d.DocumentId]);
        Test.True(recalculateStorage.PropagationArrivals.Count == 3
                && recalculateStorage.PropagationArrivals[0].DueDay == 12
                && recalculateStorage.PropagationArrivals[1].DueDay == 13
                && recalculateStorage.PropagationArrivals[2].DueDay == 16,
            "settings change reschedules existing civilian work and creates only missing court work");
        Test.True(recalculateStorage.PropagationArrivals.Any(x => x.KingdomId == "player")
                && recalculateStorage.PropagationArrivals.All(x => x.KingdomId != "known" && x.KingdomId != "author"),
            "recalculation keeps author/known skips and restores missing player formal receipt");
        Test.True(recalculateStorage.LastAppliedContinentSpreadDays == 4
                && recalculateStorage.LastAppliedCivilianSpreadDays == 4
                && recalculateStorage.LastAppliedCourtDeliveryDays == 6,
            "recalculation preserves all three persisted settings stamps");
        string recalculatedJson = JsonConvert.SerializeObject(recalculateStorage);
        Test.True(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(recalculatedJson)) == recalculatedJson,
            "recalculated queue survives JSON round trip");

        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for publication source boundary");
        string host = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"));
        int start = host.IndexOf("private void StartDocumentPropagation(", StringComparison.Ordinal);
        int end = host.IndexOf("private void RetryDeferredDocumentPropagation()", start, StringComparison.Ordinal);
        string adapter = host.Substring(start, end - start);
        Test.True(adapter.Contains("WorldDiplomacyPropagationApplication.SchedulePublication(", StringComparison.Ordinal)
                && !adapter.Contains("OrderPropagationArrivalsByDueDate", StringComparison.Ordinal)
                && !adapter.Contains("new WorldDiplomacyPropagationArrival", StringComparison.Ordinal),
            "real publication caller delegates queue construction and replacement to the application");
        int recalculateStart = host.IndexOf("private void RecalculatePendingPropagationIfNeeded()", StringComparison.Ordinal);
        int recalculateEnd = host.IndexOf("private void SynchronizeCourtKnowledge()", recalculateStart, StringComparison.Ordinal);
        string recalculateAdapter = host.Substring(recalculateStart, recalculateEnd - recalculateStart);
        Test.True(recalculateAdapter.Contains("WorldDiplomacyPropagationApplication.RecalculatePending(", StringComparison.Ordinal)
                && !recalculateAdapter.Contains("new WorldDiplomacyPropagationArrival", StringComparison.Ordinal),
            "real settings-change caller delegates queue repair and due-day updates to the application");
    }
}
