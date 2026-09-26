using System;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Main-thread command port; canonical records are confined to application/adapter scope.
internal interface IWorldDiplomacyPlayerWorld
{
    WorldDiplomacyPlayerContext Player { get; }
    bool KingdomExists(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyRound EnsureActiveRound(string author, string target, bool isPlayerInsertion);
    WorldDiplomacyDocument CreateDocument(string author, string target, string title, string body, string origin,
        bool isPlayerAuthored, bool isResponse, string exchangeId);
    void AddDocument(WorldDiplomacyDocument document);
    int CurrentDay();
    void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document);
    void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority);
}

internal static class WorldDiplomacyPlayerApplication
{
    internal static string Execute(IWorldDiplomacyPlayerWorld world, WorldDiplomacyPlayerDocumentCommand command)
    {
        if (command == null || command.Generation != world.Player.Generation) return "";
        if (!command.IsReply) return SubmitPlayerDocument(world, command.Body);
        // Resolve the original identities at submission; never retain a record in a UI callback.
        WorldDiplomacyDocument source = world.ResolveDocument(command.SourceDocumentId);
        WorldDiplomacyRound round = world.ResolveRound(command.RoundId);
        if (source == null || round == null || !string.Equals(source.RoundId, round.RoundId, StringComparison.Ordinal)) return "";
        return SubmitPlayerReply(world, command.Body, source, round);
    }
    internal static string SubmitPlayerDocument(IWorldDiplomacyPlayerWorld world, string body)
    {
        string cleanBody = WorldDiplomacyTextRules.NormalizeBody(body);
        if (string.IsNullOrWhiteSpace(cleanBody))
        {
            return "外交宣言正文不能为空。";
        }
        WorldDiplomacyPlayerContext player = world.Player;
        string playerKingdom = player.KingdomId;
        if (!player.IsRuler)
        {
            return "你当前不再是王国统治者，外交宣言没有发布。";
        }
        if (!player.Independent)
        {
            return "我国的外交事务由" + player.RepresentativeName + "掌管，外交宣言没有发布。";
        }
        WorldDiplomacyRound round = world.EnsureActiveRound(playerKingdom, null, isPlayerInsertion: true);
        WorldDiplomacyDocument document = world.CreateDocument(
            playerKingdom,
            null,
            "外交宣言",
            cleanBody,
            "player",
            isPlayerAuthored: true,
            isResponse: false,
            exchangeId: round?.RoundId ?? "");
        document.RoundId = round?.RoundId ?? "";
        WorldDiplomacyResultSettlementSlot playerSettlementSlot = round?.ResultSettlementPending == true
            ? WorldDiplomacyRoundLifecycleRules.SelectWaitingPlayerSettlementSlot(
                round.ResultSettlementSlots, round.ResultSettlementCurrentSlotId, playerKingdom)
            : null;
        if (playerSettlementSlot != null)
        {
            document.ResultSettlementSlotId = playerSettlementSlot.SlotId ?? "";
        }
        world.AddDocument(document);
        if (round != null)
        {
            round.RootDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.RootDocumentId, document.DocumentId);
            round.LastActivityDay = world.CurrentDay();
            WorldDiplomacyStructureRules.EnsureRoundParticipant(round, playerKingdom, "active", mandatoryReply: false);
        }
        world.PublishPlayerAuthoredDocumentImmediately(document);
        world.EnqueueAnalysisJob(document, priority: 100);
        return "外交宣言已经公开发布；系统正在后台解析其对象、诉求与外交动作。";
    }
    internal static string SubmitPlayerReply(IWorldDiplomacyPlayerWorld world, string body,
        WorldDiplomacyDocument sourceDocument, WorldDiplomacyRound round)
    {
        WorldDiplomacyPlayerContext context = world.Player;
        string player = context.KingdomId;
        string target = world.KingdomExists(sourceDocument.AuthorKingdomId) ? sourceDocument.AuthorKingdomId : null;
        if (player == null || target == null || !context.Independent)
        {
            if (player != null && !context.Independent)
            {
                return "我国的外交事务由" + context.RepresentativeName + "掌管，不能独立回应外交宣言。";
            }
            return "";
        }
        WorldDiplomacyDocument response = world.CreateDocument(
            player,
            target,
            "外交回应",
            WorldDiplomacyTextRules.NormalizeBody(body),
            "player_response",
            isPlayerAuthored: true,
            isResponse: true,
            exchangeId: round.RoundId);
        response.RoundId = round.RoundId;
        response.SourceDocumentId = sourceDocument.DocumentId;
        response.AutomaticReplyDepth = Math.Max(1, sourceDocument.AutomaticReplyDepth + 1);
        world.AddDocument(response);
        WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, player, "active", mandatoryReply: false);
        participant.MandatoryReplyPending = false;
        participant.LastTriggeredDocumentId = sourceDocument.DocumentId;
        round.LastActivityDay = world.CurrentDay();
        world.PublishPlayerAuthoredDocumentImmediately(response);
        world.EnqueueAnalysisJob(response, priority: 100);
        return "外交回应已经公开发布；系统正在后台解析其诉求与外交动作。";
    }
}
