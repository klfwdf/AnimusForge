using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class OrdinaryAdmissionReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        int day = 10, reads = 0, budget = 0, enqueued = 0;
        string author = "a";
        WorldDiplomacyRound Open(string id) => WorldDiplomacyRoundApplication.EnsureOpen(storage,
            () => new WorldDiplomacyRoundApplication.RoundOpening(3, "round-" + id, id, "target", day, 15, 18, 2, false));
        void Normal(int limit = 3) => WorldDiplomacyRoundApplication.TryScheduleNormal(storage, false, () => day,
            () => { reads++; return new[] { author }; }, _ => true, () => { budget++; return true; }, Open,
            (_, _) => enqueued++, _ => { }, _ => { }, limit);
        void Policy(string id, int limit = 3)
        {
            storage.PendingPolicySignals.Add(new() { SignalKey = id });
            WorldDiplomacyPolicyRoundApplication.TrySchedule(storage,
                signal => new(true, "issuer", signal.SignalKey, false), _ => true, () => false,
                () => { budget++; return true; }, () => day, Open,
                (signal, _) => storage.PendingPolicySignals.Remove(signal), _ => { }, (_, _) => enqueued++, limit);
        }
        Normal();
        Test.True(enqueued == 1 && storage.LastOrdinaryRoundStartedDay == 10, "normal opening records durable daily admission");
        Policy("b"); Normal();
        Test.True(enqueued == 1 && reads == 1 && budget == 1 && storage.PendingPolicySignals.Count == 1,
            "normal opening prevents same-day policy opening and world candidate scans, retaining policy");
        storage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        Normal();
        Test.True(enqueued == 1 && reads == 1, "save roundtrip cannot reopen the daily slot");
        day++;
        WorldDiplomacyPolicyRoundApplication.TrySchedule(storage, _ => new(true, "issuer", "b", false), _ => true,
            () => false, () => { budget++; return true; }, () => day, Open,
            (signal, _) => storage.PendingPolicySignals.Remove(signal), _ => { }, (_, _) => enqueued++);
        author = "c"; Normal();
        Test.True(enqueued == 2 && reads == 1 && storage.PendingPolicySignals.Count == 0, "policy opening consumes the shared daily slot");
        day++; Normal(); day++; author = "d";
        int before = budget; Normal(); Policy("d");
        Test.True(enqueued == 3 && budget == before && storage.PendingPolicySignals.Count == 1,
            "full cap preserves pending policies without generation budget");
        Policy("a");
        Test.True(storage.PendingPolicySignals.Count == 1 && storage.ActiveRound.ExternalSignalKeys.Contains("a"),
            "later attachable policy merges at capacity despite blocked queue head");
        var player = WorldDiplomacyRoundApplication.EnsureOpen(storage,
            () => new WorldDiplomacyRoundApplication.RoundOpening(3, "player", "p", "a", day, 15, 18, 2, true));
        var oral = new WorldDiplomacyRound { RoundId = "oral", State = "active", EventSourceType = "dialogue_commitment", StartedDay = day };
        storage.ConcurrentRounds.Add(oral);
        Test.True(player.IsPlayerInsertion && storage.ConcurrentRounds.Contains(oral), "player and oral events remain independent at capacity");
        int live = WorldDiplomacyLiveRoundRules.Live(storage).Count(); Normal(1);
        Test.True(WorldDiplomacyLiveRoundRules.Live(storage).Count() == live, "lowering cap preserves all existing events");
        storage.ActiveRound.State = "closed"; Normal();
        Test.True(enqueued == 4 && storage.LastOrdinaryRoundStartedDay == day, "closing frees ordinary capacity on an unused day");
        var newest = WorldDiplomacyLiveRoundRules.Live(storage).Single(x => x.RoundId == "round-d");
        newest.State = "closed"; author = "e"; Normal();
        Test.True(enqueued == 4, "zero-document aborted opening still consumes the daily slot");
        var legacy = new WorldDiplomacyStorage { NextNormalRoundDay = 999, LastAppliedRoundIntervalDays = 100,
            ActiveRound = new() { RoundId = "old", StartedDay = 6 } };
        legacy.CompletedRounds.Add(new() { StartedDay = 9, State = "closed" });
        legacy.CompletedRounds.Add(new() { StartedDay = 12, IsPlayerInsertion = true });
        legacy.ConcurrentRounds.Add(new() { StartedDay = 12, EventSourceType = "dialogue_commitment" });
        WorldDiplomacyLiveRoundRules.InitializeOrdinaryAdmission(legacy);
        Test.True(legacy.LastOrdinaryRoundStartedDay == 9 && legacy.NextNormalRoundDay == 10 && legacy.LastAppliedRoundIntervalDays == 1,
            "old saves migrate ordinary history once and ignore exempt events and obsolete intervals");
        legacy.CompletedRounds.Add(new() { StartedDay = 100 });
        WorldDiplomacyLiveRoundRules.InitializeOrdinaryAdmission(legacy);
        Test.True(legacy.LastOrdinaryRoundStartedDay == 9, "migration does not rescan archive on future scheduling passes");
        legacy.ActiveRound.State = "closed"; legacy.ConcurrentRounds.Add(null!);
        Test.True(WorldDiplomacyLiveRoundRules.CanStartOrdinary(legacy, 10, 1), "closed, null and exempt records do not consume capacity");
        DiplomacyRoundWorkRules.MergeQueuedSpeaker(null!, Array.Empty<string>());
    }
}
