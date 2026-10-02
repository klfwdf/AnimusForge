using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using Newtonsoft.Json;

internal static class CourtResponseReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        var round = new WorldDiplomacyRound { RoundId = "r", State = "active" };
        storage.ActiveRound = round;
        var playerDeclaration = new WorldDiplomacyDocument
        {
            DocumentId = "player-d", RoundId = "r", AuthorKingdomId = "player",
            TargetKingdomId = "npc", IsPlayerAuthored = true
        };
        var trace = new List<string>();
        WorldDiplomacyCourtResponseApplication.Receive(
            storage, "npc", playerDeclaration,
            () => { trace.Add("representative"); return false; },
            () => { trace.Add("player-affiliation"); return false; },
            () => { trace.Add("authority"); return true; },
            id => { trace.Add("round:" + id); return round; },
            () => trace.Add("display"),
            (current, participant) =>
            {
                trace.Add("mandatory");
                WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(
                    storage, current, participant, "npc", playerDeclaration,
                    () => false, () => true, () => false,
                    () => (false, (string)null),
                    (receiver, target, d, rId, relayTurn) => { trace.Add("enqueue:" + target); return target; },
                    message => trace.Add("log:" + message), 4);
            },
            () => 10, message => trace.Add("log:" + message));
        Test.True(trace.SequenceEqual(new[]
            { "player-affiliation", "authority", "round:r", "display", "mandatory", "enqueue:player",
              "log:mandatory response queued round=r author=npc target=player source=player-d",
              "log:court received document=player-d receiver=npc direct=True day=10" }),
            "formal player declaration shows delivery before one source-bound priority response is enqueued");
        Test.True(round.Participants.Count == 1 && round.Participants[0].MandatoryReplyPending
                && round.Participants[0].LastTriggeredDocumentId == "player-d",
            "canonical participant obligation binds the exact source before enqueue");
        string saved = JsonConvert.SerializeObject(storage);
        Test.True(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)) == saved,
            "formal response obligation survives a real JSON round trip");

        var received = new WorldDiplomacyDocument { DocumentId = "npc-d", AuthorKingdomId = "npc" };
        int effects = 0;
        WorldDiplomacyCourtResponseApplication.Receive(
            storage, "player", received, () => false, () => true, () => false,
            _ => { effects++; return round; }, () => effects++, (_, __) => effects++,
            () => 10, _ => effects++);
        Test.True(received.HasReachedPlayerCourt && effects == 1,
            "player-affiliated court records formal receipt without an AI response or presentation effect");

        var inactive = new WorldDiplomacyDocument
            { DocumentId = "late", RoundId = "closed", AuthorKingdomId = "player", TargetKingdomId = "npc", IsPlayerAuthored = true };
        effects = 0;
        WorldDiplomacyCourtResponseApplication.Receive(
            storage, "npc", inactive, () => false, () => false, () => true,
            _ => new WorldDiplomacyRound { RoundId = "closed", State = "active" },
            () => effects++, (_, __) => effects++, () => 10, _ => effects++);
        Test.True(effects == 0 && round.Participants.Count == 1,
            "late delivery into a non-active round creates no new obligation or message");

        var playerParticipant = new WorldDiplomacyRoundParticipant
            { KingdomId = "player", MandatoryReplyPending = true };
        effects = 0;
        WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(
            storage, round, playerParticipant, "player", playerDeclaration,
            () => true, () => true, () => false,
            () => { effects++; return (false, (string)null); },
            (_, __, ___, ____, _____) => { effects++; return "npc"; }, _ => effects++, 4);
        Test.True(effects == 0 && !playerParticipant.MandatoryReplyPending,
            "AI cannot author or enqueue a mandatory reply for the player ruler");

        var blockedParticipant = new WorldDiplomacyRoundParticipant
            { KingdomId = "npc", MandatoryReplyPending = true };
        var blockedTrace = new List<string>();
        WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(
            storage, round, blockedParticipant, "npc", playerDeclaration,
            () => false, () => true, () => false,
            () => { blockedTrace.Add("author-check"); return (true, "no authority"); },
            (_, __, ___, ____, _____) => { blockedTrace.Add("enqueue"); return "player"; },
            message => blockedTrace.Add(message), 4);
        Test.True(!blockedParticipant.MandatoryReplyPending && blockedParticipant.State == "observer"
                && blockedTrace.Count == 2 && blockedTrace[0] == "author-check"
                && !blockedTrace.Contains("enqueue"),
            "blocked AI authorship clears the obligation and never queues a job");

        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        Test.True(root != null, "repository located for court response boundary");
        string owner = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCourtResponseApplication.cs"));
        string host = File.ReadAllText(Path.Combine(root.FullName,
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs"));
        Test.True(!owner.Contains("TaleWorlds", StringComparison.Ordinal)
                && !owner.Contains("WorldDiplomacyBehavior", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyCourtResponseApplication.Receive(", StringComparison.Ordinal)
                && host.Contains("WorldDiplomacyCourtResponseApplication.TryScheduleMandatory(", StringComparison.Ordinal),
            "active court receipt and mandatory admission use the game-free application owner");
    }
}
