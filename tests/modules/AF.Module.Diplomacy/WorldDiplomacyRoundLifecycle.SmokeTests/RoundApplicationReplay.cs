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
        ClosureAndReload();
        PlayerInsertionAndSettlement();
        SuspensionAndLateWork();
    }

    private static WorldDiplomacyRound Open(WorldDiplomacyStorage storage, string id = "r", bool player = false)
    {
        return WorldDiplomacyRoundApplication.EnsureOpen(storage, () =>
            new WorldDiplomacyRoundApplication.RoundOpening(3, id, "a", "b", 10, 7, 30, 2, player));
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
    }

    private static void PlayerInsertionAndSettlement()
    {
        var storage = new WorldDiplomacyStorage(); var round = Open(storage);
        round.RelayPlanned = true;
        round.RelayRouteKingdomIds.Add("a");
        storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "other", Status = "open" });
        storage.PlayerOpportunities.Add(new WorldDiplomacyPlayerOpportunity { RoundId = "r", Status = "open" });
        var document = new WorldDiplomacyDocument { DocumentId = "player", AuthorKingdomId = "p", TargetKingdomId = "v", AddressedKingdomIds = new List<string> { "v", "b", "v" } };
        void Insert() => WorldDiplomacyRoundApplication.IntegratePlayerDeclaration(storage, round, document, () => 12,
            id => id == "v" ? "b" : id, id => id == "p", _ => { });
        Insert(); Insert();
        Test.True(round.Participants.Any(x => x.KingdomId == "p") && round.Participants.Any(x => x.KingdomId == "b"), "player insertion appends representatives once in stable order");
        Test.True(storage.PlayerOpportunities[0].Status == "open" && storage.PlayerOpportunities[1].Status == "answered", "player answers only the matching round opportunity");
        var player = round.Participants.Single(x => x.KingdomId == "p");
        Test.True(player.IsPlayerAsync && player.LastSpokeDay == 12, "player turn ownership is preserved");
        round.ResultSettlementPending = true; document.AddressedKingdomIds.Add("outside"); Insert();
        Test.True(round.Participants.Any(x => x.KingdomId == "outside"), "legacy settlement flag cannot limit event participants");
        foreach (WorldDiplomacyMandatoryReplyAction action in Enum.GetValues(typeof(WorldDiplomacyMandatoryReplyAction)))
        {
            var participant = new WorldDiplomacyRoundParticipant { State = "active", MandatoryReplyPending = true };
            bool admitted = WorldDiplomacyRoundApplication.AdmitMandatoryReply(action, round, participant, "b", "blocked", document, _ => { });
            Test.True(admitted == (action == WorldDiplomacyMandatoryReplyAction.Schedule), "mandatory admission dispatch: " + action);
            if (admitted) Test.True(participant.LastTriggeredDocumentId == "player", "mandatory source is frozen before enqueue");
            else if (action != WorldDiplomacyMandatoryReplyAction.JobQueued) Test.True(!participant.MandatoryReplyPending, "rejected or settlement-owned reply clears legacy pending state");
        }
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
