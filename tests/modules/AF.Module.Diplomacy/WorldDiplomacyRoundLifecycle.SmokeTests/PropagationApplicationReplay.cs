using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

// Differential replay of the pre-DPL-070 production daily method against the
// current application + host adapter. World resolution/court effects are fakes;
// canonical records and all knowledge rules are the real production sources.
internal static partial class PropagationApplicationReplay
{
    internal static void Run()
    {
        Compare("civilian-court-and-missing", s =>
        {
            s.PropagationArrivals.AddRange(new[] { Arrival("d", "village"), Arrival("d", "court", "npc"),
                Arrival("d", "court", "npc"), Arrival("d", "missing"), Arrival("absent", "village"),
                Arrival("d", "court", "missing"), Arrival("d", "missing", "missing") });
        }, h =>
        {
            Test.True(h.State.SettlementKnowledge.Single().SettlementId == "village", "civilian knowledge stays settlement-scoped");
            Test.True(h.Receipts == 1 && h.State.KingdomKnowledge.Count == 1 && h.State.NobleKnowledge.Count == 1,
                "formal delivery deduplicates and missing kingdom falls back to the current settlement owner");
            Test.True(!h.State.Documents[0].HasReachedPlayerCourt, "civilian/non-player deliveries cannot create player formal receipt");
        });
        Compare("player-receipt-recovery", s =>
        {
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(s.KingdomKnowledge, "player", "d", 2);
            s.PropagationArrivals.AddRange(new[] { Arrival("d", "player-court", "player"), Arrival("D", "player-court", "player") });
        }, h =>
        {
            Test.True(h.Receipts == 1 && h.State.Documents[0].HasReachedPlayerCourt,
                "known document repairs a missing player-court receipt exactly once");
            Test.True(h.State.KingdomKnowledge[0].LastUpdatedDay == 2 && h.State.NobleKnowledge[0].LastUpdatedDay == 10,
                "duplicate kingdom knowledge does not change its timestamp while noble receipt does");
        });
        Compare("player-already-received", s =>
        {
            s.Documents[0].HasReachedPlayerCourt = true;
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(s.KingdomKnowledge, "player", "d", 2);
            s.PropagationArrivals.Add(Arrival("d", "player-court", "player"));
        }, h => Test.True(h.Receipts == 0, "already-received player documents do not replay formal effects"));
        Compare("scope-and-identity", s =>
        {
            s.Documents.Add(new WorldDiplomacyDocument { DocumentId = "D", Title = "duplicate" });
            s.PropagationArrivals.AddRange(new[] { Arrival("D", "court", "npc", scope: "CoUrT"),
                Arrival(" d ", "village"), Arrival(" ", "village"), Arrival("d", "village", scope: null),
                Arrival("d", "village", scope: "unknown") });
            s.PropagationArrivals[3].Scope = null;
        }, h => Test.True(h.ReceiptTitles.SequenceEqual(new[] { "first" }), "duplicate document identity keeps the first canonical record"));
        Compare("empty", _ => { }, h => Test.True(h.DocumentReads == 0 && h.Trace.Count == 0, "empty daily queue does no world reads"));
        Compare("future-prefix", s => s.PropagationArrivals.AddRange(new[] { Arrival("d", "village", day: 11), Arrival("d", "court", "npc") }),
            h => Test.True(h.DocumentReads == 0 && h.State.PropagationArrivals.Count == 2, "future head stops traversal without searching the tail"));
        Compare("null-prefix", s => s.PropagationArrivals.AddRange(new[] { null, Arrival("d", "court", "npc") }),
            h => Test.True(h.DocumentReads == 0 && h.State.PropagationArrivals.Count == 2, "null head preserves original stop semantics"));
        Compare("null-after-due", s => s.PropagationArrivals.AddRange(new[] { Arrival("d", "village"), null, Arrival("d", "court", "npc") }),
            h => Test.True(h.DocumentReads == 1 && h.State.PropagationArrivals.Count == 2, "only the due prefix is removed"));
        Compare("daily-cap-and-future-backlog", s =>
        {
            for (int i = 0; i < 1201; i++) s.PropagationArrivals.Add(Arrival("d", "village"));
            for (int i = 0; i < 10000; i++) s.PropagationArrivals.Add(Arrival("d", "court", "npc", 99));
        }, h => Test.True(h.DocumentReads == 1200 && h.State.PropagationArrivals.Count == 10001 && h.State.KingdomKnowledge.Count == 0,
            "daily cap limits actual processed records to 1200 without future effects"));
        Compare("knowledge-retention", s =>
        {
            for (int i = 0; i < 140; i++)
            {
                string id = "d-" + i;
                s.Documents.Add(new WorldDiplomacyDocument { DocumentId = id });
                s.PropagationArrivals.Add(Arrival(id, "village"));
                s.PropagationArrivals.Add(Arrival(id, "court", "npc"));
            }
        }, h => Test.True(h.State.SettlementKnowledge[0].DocumentIds.Count == 64
                          && h.State.KingdomKnowledge[0].DocumentIds.Count == 128
                          && h.State.NobleKnowledge[0].DocumentIds.Count == 128,
            "real knowledge rules preserve location and kingdom retention limits"));
        PropagationFailureClosureReplay.Run();
        Compare("reentrant-enqueue", s => s.PropagationArrivals.Add(Arrival("d", "court", "npc")),
            h => Test.True(h.State.PropagationArrivals.Count == 1 && h.State.SettlementKnowledge.Count == 0,
                "work enqueued by a receipt waits for the next daily pass"),
            h => h.OnReceipt = state => state.PropagationArrivals.Add(Arrival("d", "village")));
        Compare("document-retired-during-delivery", s => s.PropagationArrivals.AddRange(new[] { Arrival("d", "court", "npc"), Arrival("d", "village") }),
            h => Test.True(h.DocumentReads == 2 && h.State.SettlementKnowledge.Count == 0,
                "later items re-resolve canonical document state after a prior receipt"),
            h => h.OnReceipt = state => state.Documents.Clear());
        Compare("document-replaced-during-delivery", s => s.PropagationArrivals.AddRange(new[] { Arrival("d", "court", "npc"), Arrival("d", "player-court", "player") }),
            h => Test.True(h.ReceiptTitles.SequenceEqual(new[] { "first", "replacement" }), "later receipts see the replacement document"),
            h => h.OnReceipt = state => state.Documents = new List<WorldDiplomacyDocument> { new WorldDiplomacyDocument { DocumentId = "d", Title = "replacement" } });

        for (int seed = 0; seed < 24; seed++)
        {
            int scenarioSeed = seed;
            Compare("seed-" + seed, s =>
            {
                var random = new Random(scenarioSeed);
                for (int i = 0; i < 180; i++)
                {
                    string id = random.Next(5) == 0 ? "missing" : random.Next(2) == 0 ? "d" : "D";
                    string settlement = new[] { "village", "court", "player-court", "missing" }[random.Next(4)];
                    string kingdom = new[] { "npc", "player", "missing", "" }[random.Next(4)];
                    s.PropagationArrivals.Add(Arrival(id, settlement, kingdom, random.Next(7, 13), random.Next(2) == 0 ? "court" : "civilian"));
                }
                s.PropagationArrivals = WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(s.PropagationArrivals).ToList();
            }, _ => { });
        }
        ReloadAndRepeat();
        VerifyHostBoundary();
    }

    private static WorldDiplomacyPropagationArrival Arrival(string document, string settlement, string kingdom = "", int day = 10, string scope = null)
        => new WorldDiplomacyPropagationArrival { DocumentId = document, SettlementId = settlement, KingdomId = kingdom,
            DueDay = day, Scope = scope ?? (string.IsNullOrEmpty(kingdom) ? "civilian" : "court"), RoundId = "round" };

    private static void Compare(string name, Action<WorldDiplomacyStorage> arrange, Action<Harness> verify, Action<Harness> configure = null)
    {
        var storage = new WorldDiplomacyStorage();
        storage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d", Title = "first" });
        arrange(storage);
        string json = JsonConvert.SerializeObject(storage);
        var old = new Harness(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(json));
        var current = new Harness(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(json));
        configure?.Invoke(old); configure?.Invoke(current);
        old.Run(legacy: true); current.Run(legacy: false);
        Test.True(JsonConvert.SerializeObject(old.State) == JsonConvert.SerializeObject(current.State), name + ": complete persisted state matches old production");
        Test.True(old.Trace.SequenceEqual(current.Trace) && old.Error == current.Error, name + ": resolver/effect order and errors match old production");
        verify(current);
    }

    private static void ReloadAndRepeat()
    {
        var state = new WorldDiplomacyStorage();
        state.Documents.Add(new WorldDiplomacyDocument { DocumentId = "d" });
        state.PropagationArrivals.Add(Arrival("d", "player-court", "player"));
        var first = new Harness(state); first.Run(false); first.Run(false);
        Test.True(first.Receipts == 1, "repeat daily call cannot replay dequeued delivery");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(state));
        restored.PropagationArrivals.Add(Arrival("d", "player-court", "player"));
        var afterLoad = new Harness(restored); afterLoad.Run(false);
        Test.True(afterLoad.Receipts == 0 && restored.Documents[0].HasReachedPlayerCourt, "save/reload retains receipt and knowledge deduplication");
    }

    private static void VerifyHostBoundary()
    {
        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for propagation boundary");
        string host = File.ReadAllText(Path.Combine(root.FullName, "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        int start = host.IndexOf("public void ProcessPropagationArrivals()", StringComparison.Ordinal);
        int end = host.IndexOf("public void RecalculatePendingPropagationIfNeeded()", start, StringComparison.Ordinal);
        string adapter = host.Substring(start, end - start).Trim().Replace("\r\n", "\n");
        string fixture = File.ReadAllText(Path.Combine(root.FullName, "tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/PropagationHostControls.cs"));
        Test.True(fixture.Replace("ProcessCurrentPropagationArrivals", "ProcessPropagationArrivals").Replace("\r\n", "\n").Contains(adapter),
            "replayed current adapter is verbatim production, including live target fallback and lazy receipt effects");
        Test.True(!adapter.Contains("RemoveRange") && !adapter.Contains("RecordKingdomKnowledge") && !adapter.Contains("newlyKnown"),
            "queue and knowledge algorithms leave the Campaign host");
        string owner = File.ReadAllText(Path.Combine(root.FullName, "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPropagationApplication.cs"));
        Test.True(!owner.Contains("TaleWorlds") && !owner.Contains("Task.Run") && !owner.Contains("WorldDiplomacyBehavior"), "propagation application stays game-free and synchronous");
    }

    private sealed partial class Harness
    {
        private const int MaxPropagationArrivalsPerDay = 1200;
        private readonly WorldDiplomacyStorage _storage;
        internal WorldDiplomacyStorage State => _storage;
        internal readonly List<string> Trace = new List<string>();
        internal readonly List<string> ReceiptTitles = new List<string>();
        internal int DocumentReads, Receipts;
        internal string Error;
        internal Action<WorldDiplomacyStorage> OnReceipt;
        private readonly FixtureHost _host;
        private readonly Dictionary<string, Kingdom> kingdoms = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Settlement> settlements = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
        internal Harness(WorldDiplomacyStorage state)
        {
            _storage = state;
            _host = new FixtureHost(this);
            kingdoms["npc"] = new Kingdom { StringId = "npc" };
            kingdoms["player"] = new Kingdom { StringId = "player" };
            settlements["village"] = new Settlement { StringId = "village" };
            settlements["court"] = new Settlement { StringId = "court", OwnerClan = new Clan { Kingdom = kingdoms["npc"] } };
            settlements["player-court"] = new Settlement { StringId = "player-court", OwnerClan = new Clan { Kingdom = kingdoms["player"] } };
        }
        internal void Run(bool legacy)
        {
            try { if (legacy) ProcessPropagationArrivals(); else ProcessCurrentPropagationArrivals(); }
            catch (Exception ex) { Error = ex.Message; }
        }
        private int CurrentDay() => 10;
        private WorldDiplomacyDocument ResolveDocument(string id)
        {
            DocumentReads++; Trace.Add("document:" + id);
            return WorldDiplomacyRoundLifecycleRules.ResolveDocument(_storage.Documents, id);
        }
        private Kingdom ResolveKingdom(string id)
        {
            Trace.Add("kingdom:" + id); return id != null && kingdoms.TryGetValue(id, out var kingdom) ? kingdom : null;
        }
        private Settlement ResolveSettlementById(string id)
        {
            Trace.Add("settlement:" + id); return id != null && settlements.TryGetValue(id, out var settlement) ? settlement : null;
        }
        private bool IsPlayerAffiliatedKingdom(Kingdom kingdom)
        {
            Trace.Add("is-player:" + kingdom.StringId); return kingdom.StringId == "player";
        }
        private void ProcessCourtArrival(Kingdom kingdom, WorldDiplomacyDocument document)
        {
            Trace.Add("court:" + kingdom.StringId + ":" + document.DocumentId + ":pending=" + _storage.PropagationArrivals.Count);
            Receipts++; ReceiptTitles.Add(document.Title);
            if (kingdom.StringId == "player") document.HasReachedPlayerCourt = true;
            OnReceipt?.Invoke(_storage);
        }
        private sealed class Kingdom { internal string StringId; }
        private sealed class Clan { internal Kingdom Kingdom; }
        private sealed class Settlement { internal string StringId; internal Clan OwnerClan; }
    }
}
