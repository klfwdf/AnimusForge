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
    int CurrentDay();
}

internal static class WorldDiplomacyPlayerApplication
{
    internal static bool CanRetryAnalysis(WorldDiplomacyDocument document, WorldDiplomacyPlayerContext player)
        => document?.IsPlayerAuthored == true && document.IsReadyForPublication
            && document.AnalysisStatus == "analysis_failed" && !document.PlayerAnalysisCommitted
            && !document.ChangedDiplomaticState && !document.HistoryResultRecorded
            && player?.IsRuler == true && player.Independent && player.KingdomId == document.AuthorKingdomId;
    internal static string Execute(IWorldDiplomacyPlayerWorld world, WorldDiplomacyPlayerDocumentCommand command,
        IWorldDiplomacyOrchestration orchestration)
    {
        if (command == null || orchestration == null || command.Generation != world.Player.Generation) return "";
        if (!command.IsReply) return SubmitPlayerDocument(world, command.Body, orchestration);
        // Resolve the original identities at submission; never retain a record in a UI callback.
        WorldDiplomacyDocument source = world.ResolveDocument(command.SourceDocumentId);
        WorldDiplomacyRound round = world.ResolveRound(command.RoundId);
        if (source == null || round == null || !string.Equals(source.RoundId, round.RoundId, StringComparison.Ordinal)) return "";
        return SubmitPlayerReply(world, command.Body, source, round, orchestration);
    }
    internal static string SubmitPlayerDocument(IWorldDiplomacyPlayerWorld world, string body,
        IWorldDiplomacyOrchestration orchestration)
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
        WorldDiplomacyRound round = orchestration.EnsureActiveRound(playerKingdom, null, isPlayerInsertion: true);
        WorldDiplomacyDocument document = orchestration.CreateDocument(
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
        orchestration.AddDocument(document);
        if (round != null)
        {
            round.RootDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.RootDocumentId, document.DocumentId);
            round.LastActivityDay = world.CurrentDay();
            WorldDiplomacyStructureRules.EnsureRoundParticipant(round, playerKingdom, "active", mandatoryReply: false);
        }
        orchestration.PublishPlayerAuthoredDocumentImmediately(document);
        orchestration.EnqueueAnalysisJob(document, priority: 100);
        return "外交宣言已经公开发布；系统正在后台解析其对象、诉求与外交动作。";
    }
    internal static string SubmitPlayerReply(IWorldDiplomacyPlayerWorld world, string body,
        WorldDiplomacyDocument sourceDocument, WorldDiplomacyRound round,
        IWorldDiplomacyOrchestration orchestration)
    {
        WorldDiplomacyPlayerContext context = world.Player;
        string cleanBody = WorldDiplomacyTextRules.NormalizeBody(body);
        if (string.IsNullOrWhiteSpace(cleanBody)) return "外交回应正文不能为空。";
        if (!context.IsRuler) return "你当前不再是王国统治者，外交回应没有发布。";
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
        if (!WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State))
        {
            string previousRoundId = round.RoundId;
            round = orchestration.EnsureActiveRound(player, target, isPlayerInsertion: true);
            if (round != null) round.ExternalOpeningContext = "玩家回应独立成案；原事件=" + previousRoundId
                + "；背景公文=" + sourceDocument.DocumentId + "。原事件已结束，旧提案不因此恢复有效。";
        }
        if (round == null) return "外交回应暂未发布：无法建立交涉回合。";
        WorldDiplomacyDocument response = orchestration.CreateDocument(
            player,
            target,
            "外交回应",
            cleanBody,
            "player_response",
            isPlayerAuthored: true,
            isResponse: true,
            exchangeId: round.RoundId);
        response.RoundId = round.RoundId;
        response.SourceDocumentId = sourceDocument.DocumentId;
        response.AutomaticReplyDepth = Math.Max(1, sourceDocument.AutomaticReplyDepth + 1);
        round.RootDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(round.RootDocumentId, response.DocumentId);
        orchestration.AddDocument(response);
        WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, player, "active", mandatoryReply: false);
        participant.MandatoryReplyPending = false;
        participant.LastTriggeredDocumentId = sourceDocument.DocumentId;
        round.LastActivityDay = world.CurrentDay();
        orchestration.PublishPlayerAuthoredDocumentImmediately(response);
        orchestration.EnqueueAnalysisJob(response, priority: 100);
        return "外交回应已经公开发布；系统正在后台解析其诉求与外交动作。";
    }
}
