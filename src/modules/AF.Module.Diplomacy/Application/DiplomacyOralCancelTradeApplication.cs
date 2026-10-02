using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyOralCancelTradeSource
{
    DiplomacyOralPairSnapshot Capture();
    WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command);
    bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralCancelTradeApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralCancelTradeSource
    {
        DiplomacyOralPairSnapshot snapshot = source.Capture();
        WorldDiplomacyOralCancelTradeResolution resolution = WorldDiplomacyOralCancelTradeRules.ResolveCommand(
            payload, snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.NpcKingdomExists,
            snapshot.NpcKingdomId, snapshot.SpeakerHeroId);
        if (!resolution.IsReady)
        {
            source.Log($"[CancelTrade] Rejected status={resolution.Status}");
            return "";
        }
        WorldDiplomacyCancelTradeExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log($"[CancelTrade] Rejected status={receipt.Status} code={receipt.ErrorCode}");
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.PlayerKingdomId, receipt.NpcKingdomId,
            out string playerId, out string npcId))
        {
            source.Log("[CancelTrade] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[CancelTrade] {playerId} <-> {npcId}");
        source.NotifyResolved();
        return "";
    }
}
