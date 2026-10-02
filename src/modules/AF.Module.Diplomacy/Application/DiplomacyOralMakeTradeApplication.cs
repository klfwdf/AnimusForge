using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyOralMakeTradeSource
{
    DiplomacyOralRoyalSnapshot Capture();
    WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command);
    bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralMakeTradeApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralMakeTradeSource
    {
        DiplomacyOralRoyalSnapshot snapshot = source.Capture();
        WorldDiplomacyOralMakeTradeResolution resolution = WorldDiplomacyOralMakeTradeRules.ResolveCommand(
            payload, snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.PlayerIsRuler,
            snapshot.NpcKingdomExists, snapshot.NpcKingdomId,
            snapshot.SpeakerHeroId, snapshot.NpcIsRuler);
        if (!resolution.IsReady)
        {
            source.Log("[MakeTrade] Rejected status=" + resolution.Status);
            return "";
        }
        WorldDiplomacyMakeTradeExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log("[MakeTrade] Rejected status=" + receipt.Status + " code=" + receipt.ErrorCode);
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.PlayerKingdomId, receipt.NpcKingdomId,
            out string playerId, out string npcId))
        {
            source.Log("[MakeTrade] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[MakeTrade] {playerId} <-> {npcId} days={receipt.AppliedDurationDays}");
        source.NotifyResolved();
        return "";
    }
}
