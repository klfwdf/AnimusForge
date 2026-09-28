using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyOralFormAllianceSource
{
    DiplomacyOralRoyalSnapshot Capture();
    WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command);
    bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralFormAllianceApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralFormAllianceSource
    {
        DiplomacyOralRoyalSnapshot snapshot = source.Capture();
        WorldDiplomacyOralFormAllianceResolution resolution = WorldDiplomacyOralFormAllianceRules.ResolveCommand(
            payload, snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.PlayerIsRuler,
            snapshot.NpcKingdomExists, snapshot.NpcKingdomId,
            snapshot.SpeakerHeroId, snapshot.NpcIsRuler);
        if (!resolution.IsReady)
        {
            source.Log($"[FormAlliance] Rejected status={resolution.Status}");
            return "";
        }
        WorldDiplomacyFormAllianceExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log($"[FormAlliance] Rejected status={receipt.Status} code={receipt.ErrorCode}");
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.PlayerKingdomId, receipt.NpcKingdomId,
            out string playerId, out string npcId))
        {
            source.Log("[FormAlliance] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[FormAlliance] {playerId} <-> {npcId}");
        source.NotifyResolved();
        return "";
    }
}
