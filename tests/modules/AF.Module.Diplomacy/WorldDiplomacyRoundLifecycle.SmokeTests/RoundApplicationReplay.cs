using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static class RoundApplicationReplay
{
    internal static void Run()
    {
        SelectionAndOpening();
        ClosureAndReload();
        PlayerInsertionAndSettlement();
        PolicyScheduling();
        SuspensionAndLateWork();
    }

    private static WorldDiplomacyRound Open(WorldDiplomacyStorage storage, string id = "r", bool player = false)
    {
        return WorldDiplomacyRoundApplication.EnsureOpen(storage, () =>
            new WorldDiplomacyRoundApplication.RoundOpening(3, id, "a", "b", 10, 7, 30, 2, player));
    }

    private static void SelectionAndOpening()
    {
        foreach (var duration in new[] { (15, 18), (21, 24), (28, 32) })
        {
            var configured = WorldDiplomacyRoundApplication.EnsureOpen(new WorldDiplomacyStorage(), () =>
                new WorldDiplomacyRoundApplication.RoundOpening(3, "configured", "a", "b", 100, duration.Item1, duration.Item2, 2, false));
            Test.True(configured.SoftEndDay == 100 + duration.Item1 && configured.HardEndDay == 100 + duration.Item2,
                "all configured round durations survive application opening");
        }
        var storage = new WorldDiplomacyStorage { NextNormalRoundDay = 10, LastOrdinaryRoundStartedDay = 9, RotationIndex = 1 };
        var visited = new List<string>();
        int worldReads = 0, next = 0, queued = 0, budgets = 0;
        bool budgetAllowed = false;
        void Schedule(bool running = false, int day = 10, bool actionable = true) =>
            WorldDiplomacyRoundApplication.TryScheduleNormal(storage, running, () => day,
                () => { worldReads++; return new[] { "a", "b", "c" }; },
                id => { visited.Add(id); return actionable && id == "c"; },
                () => { budgets++; return budgetAllowed; },
                id => { Test.True(id == "c", "rotation must select the first actionable candidate"); return WorldDiplomacyRoundApplication.EnsureOpen(storage,
                    () => new WorldDiplomacyRoundApplication.RoundOpening(3, "r", id, "b", 10, 7, 30, 2, false)); },
                (id, round) => { queued++; Test.True(ReferenceEquals(round, storage.ActiveRound), "generation belongs to the canonical round"); },
                dayValue => next++, _ => { });
        Schedule(running: true, day: 9); Schedule(day: 9);
        storage.Jobs.AddRange(Enumerable.Range(0, 24).Select(x => new WorldDiplomacyJob { JobId = "pending" + x })); Schedule(); storage.Jobs.Clear();
        Test.True(worldReads == 0 && budgets == 0, "full queue/not-due scheduling never constructs world candidates");
        Schedule();
        Test.True(visited.SequenceEqual(new[] { "b", "c" }) && storage.RotationIndex == 0
                  && storage.ActiveRound == null && queued == 0, "budget exhaustion preserves original rotation-before-budget order");
        visited.Clear(); budgetAllowed = true; Schedule();
        Test.True(visited.SequenceEqual(new[] { "a", "b", "c" }) && queued == 1 && next == 1,
            "selection scans no more than one candidate cycle");
        WorldDiplomacyRound round = storage.ActiveRound;
        Test.True(round.StartedDay == 10 && round.SoftEndDay == 17 && round.HardEndDay == 40
                  && round.RelayPassDurationDays == 2 && round.SchemaVersion == 3, "opening preserves serialized deadlines/schema");
        Test.True(round.Participants.Count == 2 && round.Participants[0].State == "active"
                  && round.Participants[1].State == "observer", "opening assigns initiator and observer roles");
        Test.True(ReferenceEquals(WorldDiplomacyRoundApplication.EnsureOpen(storage,
            () => new WorldDiplomacyRoundApplication.RoundOpening(3, "other", "c", "a", 10, 7, 30, 2, false)), round),
            "the same NPC initiator reuses its original event identity");
        int readsBefore = worldReads; Schedule();
        Test.True(worldReads == readsBefore && queued == 1, "same-day opening is rejected before candidate scans");
        storage.ActiveRound = null; visited.Clear(); Schedule(day: 11, actionable: false);
        Test.True(visited.Count == 3 && next == 2 && queued == 1, "no-result selection defers once without generation");
        WorldDiplomacyRoundApplication.TryScheduleNormal(storage, false, () => 11,
            () => Array.Empty<string>(), _ => throw new Exception(), () => throw new Exception(),
            _ => throw new Exception(), (_, _) => throw new Exception(), _ => next++, _ => { });
        Test.True(next == 3, "empty candidate set schedules the next normal opportunity");
        string[] backlog = Enumerable.Range(0, 4096).Select(i => "kingdom-" + i).ToArray();
        int visits = 0;
        WorldDiplomacyRoundApplication.TryScheduleNormal(storage, false, () => 11, () => backlog,
            _ => { visits++; return false; }, () => throw new Exception("no candidate may consume budget"),
            _ => throw new Exception(), (_, _) => throw new Exception(), _ => { }, _ => { });
        Test.True(visits == backlog.Length, "large candidate backlog is visited once with no repeated rotation scan");
        visits = 0; storage.RotationIndex = 0;
        WorldDiplomacyRoundApplication.TryScheduleNormal(storage, false, () => 11, () => backlog,
            _ => { visits++; return true; }, () => false,
            _ => throw new Exception(), (_, _) => throw new Exception(), _ => { }, _ => { });
        Test.True(visits == 1, "selection stops at the first eligible candidate even in a large backlog");
        var eligible = WorldDiplomacyRoundLifecycleRules.SelectEligibleAiPartyIds(
            new[] { "kZ", "", "kA", "kM" },
            id => id != "kM",
            id => id != "kZ");
        Test.True(eligible.SequenceEqual(new[] { "kA" }),
            "eligible AI party selection is a domain rule over leaf predicates and keeps stable ordering");
        storage.ActiveRound = new WorldDiplomacyRound { State = "closed" };
        Test.True(Open(storage, "player", true).IsPlayerInsertion, "player can open a new round after a closed record");
    }

    private static void ClosureAndReload()
    {
        foreach (var scenario in new[] { ("normal", 0, 0, "closed"), ("technical_cancelled", 0, 0, "aborted"),
                     ("resolved", 1, 1, "resolved"), ("no_result", 0, 2, "deadlocked") })
        {
            var storage = new WorldDiplomacyStorage(); var round = Open(storage);
            round.ExecutedActionCount = scenario.Item2; round.DiplomaticActionAttemptCount = scenario.Item3;
            round.CommonContractSnapshot = "frozen"; round.CommonContractSnapshotInitialized = true;
            round.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open" });
            round.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "accepted" });
            storage.Jobs.Add(new WorldDiplomacyJob { JobId = "mine", RoundId = "r" });
            storage.Jobs.Add(new WorldDiplomacyJob { JobId = "other", RoundId = "other" });
            storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival { RoundId = "r" });
            storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "r", Status = "open" });
            if (scenario.Item1 != "no_result")
            {
                storage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "draft", RoundId = "r", Day = 30 });
                storage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "last", RoundId = "r", Day = 12, IsReadyForPublication = true });
                storage.Documents.Add(new WorldDiplomacyDocument { DocumentId = "first", RoundId = "r", Day = 11, IsReadyForPublication = true });
            }
            var events = new List<string>();
            void Close(string reason) => WorldDiplomacyRoundApplication.Close(storage, reason, () => 20,
                r => { Test.True(r.PendingOffers[0].Status == "expired" && r.PendingOffers[1].Status == "accepted"
                                  && storage.ActiveRound == r, "cooldowns settle after expiry and before archive"); events.Add("cooldown"); },
                d => { Test.True(storage.ActiveRound == null && storage.CompletedRounds.Count == 1 && d == 20, "schedule follows archive"); events.Add("next"); },
                (r, docs) => { Test.True(docs.Select(d => d.DocumentId).SequenceEqual(new[] { "first", "last" })
                                        && r.CommonContractSnapshot == "frozen", "summary sees only published ordered documents and frozen contract"); events.Add("summary"); },
                () => events.Add("compress"), _ => events.Add("log"));
            Close(scenario.Item1);
            Test.True(round.State == "closed" && round.RoundStatus == scenario.Item4 && round.CompletedDay == 20, "close status parity: " + scenario.Item1);
            Test.True(storage.Jobs.Count == 1 && storage.Jobs[0].JobId == "other" && storage.RelayArrivals.Count == 0
                      && storage.PlayerOpportunities[0].Status == "expired", "close retires own jobs and queues while preserving unrelated jobs");
            Test.True(!round.CommonContractSnapshotInitialized && round.CommonContractSnapshot == "", "frozen contract is released after summary");
            string expected = scenario.Item1 == "no_result" ? "cooldown,log,compress" : "cooldown,summary,log,compress";
            Test.True(string.Join(",", events) == expected, "closure effects preserve order and no-document summary gate");
            Test.True(round.FinalDocumentId == (scenario.Item1 == "no_result" ? "" : "last"), "final document excludes unpublished drafts");
            int count = events.Count; Close("duplicate");
            Test.True(events.Count == count && storage.CompletedRounds.Count == 1, "duplicate close does not replay settlement");
            storage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
            Close("after_reload");
            Test.True(events.Count == count && storage.CompletedRounds.Single().CloseReason == scenario.Item1, "closed save roundtrip cannot replay effects");
        }
        var delayed = new WorldDiplomacyStorage(); var active = Open(delayed);
        active.HardEndDay = 15; delayed.Jobs.Add(new WorldDiplomacyJob { JobId = "inflight", RoundId = "r", IsRunning = true });
        int closes = 0;
        void CloseDelayed(string reason) { closes++; WorldDiplomacyRoundApplication.Close(delayed, reason, () => 20, _ => { }, _ => { }, (_, _) => { }, () => { }, _ => { }); }
        void Tick() => WorldDiplomacyRoundApplication.ProcessRoundLifecycle(delayed, () => 20, _ => null, (_, _) => { }, _ => { }, _ => { }, CloseDelayed, _ => { });
        Tick(); Test.True(closes == 0 && delayed.ActiveRound == active, "delayed in-flight work defers hard-end closure");
        delayed.Jobs.Clear(); Tick(); Tick();
        Test.True(closes == 1 && active.CloseReason == "relay_hard_end", "drained hard-end closes once through application");
        active = Open(delayed, "orphan"); active.RelayWaiting = true;
        delayed = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(delayed))!;
        WorldDiplomacyRoundApplication.ReconcileActiveDiplomacyAfterLoad(delayed, () => 12, _ => { }, _ => { }, CloseDelayed, _ => { });
        Test.True(delayed.ActiveRound == null && closes == 2
                  && delayed.CompletedRounds.Last().CloseReason == "technical_missing_root_after_load", "orphaned reloaded rounds take the technical close path");
    }

    private static void PlayerInsertionAndSettlement()
    {
        var storage = new WorldDiplomacyStorage(); var round = Open(storage);
        round.RelayPlanned = true;
        round.RelayRouteKingdomIds.Add("a");
        storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "other", Status = "open" });
        storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "r", Status = "open" });
        var document = new WorldDiplomacyDocument { DocumentId = "player", AuthorKingdomId = "p", TargetKingdomId = "v", AddressedKingdomIds = new List<string> { "v", "b", "v" } };
        void Insert() => WorldDiplomacyRoundApplication.IntegratePlayerDeclaration(storage, round, document, () => 12, () => 4,
            id => id == "v" ? "b" : id, id => id == "p", _ => { });
        Insert(); Insert();
        Test.True(round.RelayRouteKingdomIds.SequenceEqual(new[] { "a", "p", "b" }), "player insertion appends representatives once in stable order");
        Test.True(storage.PlayerOpportunities[0].Status == "open" && storage.PlayerOpportunities[1].Status == "answered", "player answers only the matching round opportunity");
        var player = round.Participants.Single(x => x.KingdomId == "p");
        Test.True(player.IsPlayerAsync && player.LastSpokeDay == 12 && player.SelectedForRelay, "player turn ownership is preserved");
        round.ResultSettlementPending = true; document.AddressedKingdomIds.Add("outside"); Insert();
        Test.True(!round.Participants.Any(x => x.KingdomId == "outside"), "settlement insertion cannot expand the frozen route");
        int relay = 0, settlement = 0, closed = 0;
        round.HardEndDay = 10; round.RelayWaiting = true;
        WorldDiplomacyRoundApplication.AdvanceRelay(round, true, () => 20, _ => settlement++, _ => closed++, (_, _) => relay++);
        Test.True(settlement == 1 && closed == 0 && relay == 0 && !round.RelayWaiting, "settlement owns continuation even after normal hard end");
        round.ResultSettlementPending = false;
        WorldDiplomacyRoundApplication.AdvanceRelay(round, false, () => 20, _ => settlement++, _ => closed++, (_, _) => relay++);
        Test.True(closed == 1 && relay == 0, "ordinary continuation closes at hard end");
        round.HardEndDay = 30;
        WorldDiplomacyRoundApplication.AdvanceRelay(round, true, () => 20, _ => settlement++, _ => closed++, (_, immediate) => { Test.True(immediate, "resume keeps immediate scheduling flag"); relay++; });
        Test.True(relay == 1, "normal continuation schedules exactly one relay hop");
        foreach (WorldDiplomacyMandatoryReplyAction action in Enum.GetValues(typeof(WorldDiplomacyMandatoryReplyAction)))
        {
            var participant = new WorldDiplomacyRoundParticipant { State = "active", MandatoryReplyPending = true };
            bool admitted = WorldDiplomacyRoundApplication.AdmitMandatoryReply(action, round, participant, "b", "blocked", document, _ => { });
            Test.True(admitted == (action == WorldDiplomacyMandatoryReplyAction.Schedule), "mandatory admission dispatch: " + action);
            if (admitted) Test.True(participant.LastTriggeredDocumentId == "player", "mandatory source is frozen before enqueue");
            else if (action != WorldDiplomacyMandatoryReplyAction.JobQueued) Test.True(!participant.MandatoryReplyPending, "rejected or settlement-owned reply clears legacy pending state");
        }
    }

    private static void PolicyScheduling()
    {
        var storage = new WorldDiplomacyStorage();
        var signal = new WorldDiplomacyPolicySignal { SignalKey = "policy" };
        storage.PendingPolicySignals.Add(signal);
        var events = new List<string>();
        var parties = new WorldDiplomacyPolicyRoundApplication.Parties(true, "a", "b", true, issuerIsPlayer: false);
        void Schedule(bool running = false, bool budget = true, bool actionable = true, int day = 10) =>
            WorldDiplomacyPolicyRoundApplication.TrySchedule(storage, _ => parties, _ => actionable, () => running, () => budget, () => day,
                id => { events.Add("open:" + id); return Open(storage); },
                (_, why) => events.Add(why), _ => events.Add("next"), (_, _) => events.Add("enqueue"));
        Schedule(running: true, budget: false); Schedule(budget: false);
        Test.True(events.Count == 0, "policy retains its trigger while generation budget is exhausted");
        Schedule();
        Test.True(string.Join(",", events) == "open:a,next,enqueue,opened_round", "policy chooses AI representative when affected kingdom is player and preserves effect order");
        WorldDiplomacyRound opened = storage.ActiveRound;
        Test.True(opened != null
            && opened.Participants.Any(p => p.KingdomId == "a" && !p.IsPlayerAsync)
            && opened.Participants.Any(p => p.KingdomId == "b" && p.IsPlayerAsync),
            "policy attach installs observer participants with application-owned player-async flags");
        events.Clear(); Schedule();
        Test.True(string.Join(",", events) == "attached_to_active_round", "matching policy attaches without a second round or generation");
        events.Clear(); parties = new WorldDiplomacyPolicyRoundApplication.Parties(true, "x", "y", false); Schedule(budget: false);
        Test.True(events.Count == 0, "unrelated policy retains its work while shared budget is exhausted");
        storage.ActiveRound = null; Schedule(actionable: false, day: 11);
        Test.True(string.Join(",", events) == "no_actionable_diplomatic_target", "no-result policy completes without consuming a normal opening");
        events.Clear(); parties = new WorldDiplomacyPolicyRoundApplication.Parties(false, null, null, false); Schedule();
        Test.True(events.Single() == "invalid_parties", "invalid parties are rejected before scheduling");
        events.Clear(); parties = new WorldDiplomacyPolicyRoundApplication.Parties(true, "a", "a", false); Schedule();
        Test.True(events.Single() == "same_or_invalid_diplomatic_representative", "same representative never opens diplomacy with itself");
    }

    private static void SuspensionAndLateWork()
    {
        var storage = new WorldDiplomacyStorage();
        var exchange = new WorldDiplomacyExchange { ExchangeId = "legacy", State = "waiting_response", ResponseDueDay = 12, CloseDueDay = 14 };
        Test.True(WorldDiplomacyRoundLifecycleRules.SuspendActiveExchangeForPlayerInsertion(exchange, storage.SuspendedExchanges, 10), "legacy player insertion suspends exchange");
        storage = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        var restored = WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(null, storage.SuspendedExchanges, 13);
        Test.True(restored.ResponseDueDay == 15 && restored.CloseDueDay == 17 && restored.State == "waiting_response", "reload resumes suspended exchange with pause duration added once");
        Test.True(WorldDiplomacyRoundLifecycleRules.RestoreSuspendedExchangeIfAny(restored, storage.SuspendedExchanges, 15) == null
                  && restored.ResponseDueDay == 15, "duplicate resume leaves deadlines unchanged");
    }
}
