using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyOralBreakAllianceSource
{
    DiplomacyOralPairSnapshot Capture();
    WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command);
    bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralBreakAllianceApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralBreakAllianceSource
    {
        DiplomacyOralPairSnapshot snapshot = source.Capture();
        WorldDiplomacyOralBreakAllianceResolution resolution = WorldDiplomacyOralBreakAllianceRules.ResolveCommand(
            payload, snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.NpcKingdomExists,
            snapshot.NpcKingdomId, snapshot.SpeakerHeroId);
        if (!resolution.IsReady)
        {
            source.Log($"[BreakAlliance] Rejected status={resolution.Status}");
            return "";
        }
        WorldDiplomacyBreakAllianceExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log($"[BreakAlliance] Rejected status={receipt.Status} code={receipt.ErrorCode}");
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.PlayerKingdomId, receipt.NpcKingdomId,
            out string playerId, out string npcId))
        {
            source.Log("[BreakAlliance] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[BreakAlliance] {playerId} <-> {npcId}");
        source.NotifyResolved();
        return "";
    }
}
