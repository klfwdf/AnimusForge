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
    // Evaluated at request/commit boundaries over this document and round only.
    internal static bool InvolvesPlayer(WorldDiplomacyDocument document, WorldDiplomacyRound round,
        Func<string, bool> isPlayer)
    {
        if (document?.IsPlayerAuthored == true || document?.AnsweredPlayerDocumentIds?.Count > 0) return true;
        if (document != null)
        {
            if (isPlayer(document.AuthorKingdomId) || isPlayer(document.TargetKingdomId)) return true;
            if (document.AddressedKingdomIds != null)
                foreach (string id in document.AddressedKingdomIds) if (isPlayer(id)) return true;
            if (document.Actions != null)
                foreach (var action in document.Actions) if (action != null && isPlayer(action.TargetKingdomId)) return true;
        }
        if (round?.IsPlayerInsertion == true) return true;
        if (round?.RelayRouteKingdomIds != null)
            foreach (string id in round.RelayRouteKingdomIds) if (isPlayer(id)) return true;
        return false;
    }

    internal static bool CanRetryAnalysis(WorldDiplomacyDocument document, WorldDiplomacyPlayerContext player)
        => document?.IsPlayerAuthored == true && document.IsReadyForPublication
            && document.AnalysisStatus == "analysis_failed" && !document.PlayerAnalysisCommitted
            && !document.ChangedDiplomaticState && !document.HistoryResultRecorded
            && player?.IsRuler == true && player.KingdomId == document.AuthorKingdomId;
    internal static string Execute(IWorldDiplomacyPlayerWorld world, WorldDiplomacyPlayerDocumentCommand command,
        IWorldDiplomacyOrchestration orchestration)
    {
        if (command == null || orchestration == null || command.Generation != world.Player.Generation) return "";
        if (!command.IsReply) return SubmitPlayerDocument(world, command.Body, orchestration);
        // Resolve advisory context at submission, without requiring its old round
        // to exist or treating the button as a separate response mechanism.
        WorldDiplomacyDocument source = world.ResolveDocument(command.SourceDocumentId);
        if (source == null) return "原公文已不可用，请重新打开撰写界面。";
        return SubmitPlayerDocument(world, command.Body, orchestration, source);
    }
    internal static string SubmitPlayerDocument(IWorldDiplomacyPlayerWorld world, string body,
        IWorldDiplomacyOrchestration orchestration, WorldDiplomacyDocument sourceDocument = null)
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
        document.SourceDocumentId = sourceDocument?.DocumentId ?? "";
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
}
