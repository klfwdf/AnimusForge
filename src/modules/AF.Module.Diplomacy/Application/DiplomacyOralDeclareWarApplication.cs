using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal readonly struct DiplomacyOralDeclareWarSnapshot
{
    internal DiplomacyOralDeclareWarSnapshot(bool npcKingdomExists, string npcKingdomId, string speakerHeroId,
        bool playerKingdomExists, string playerKingdomId, bool playerKingdomEliminated, bool npcIsRuler)
    {
        NpcKingdomExists = npcKingdomExists;
        NpcKingdomId = npcKingdomId;
        SpeakerHeroId = speakerHeroId;
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomId = playerKingdomId;
        PlayerKingdomEliminated = playerKingdomEliminated;
        NpcIsRuler = npcIsRuler;
    }
    internal bool NpcKingdomExists { get; }
    internal string NpcKingdomId { get; }
    internal string SpeakerHeroId { get; }
    internal bool PlayerKingdomExists { get; }
    internal string PlayerKingdomId { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal bool NpcIsRuler { get; }
}

internal interface IDiplomacyOralDeclareWarSource
{
    DiplomacyOralDeclareWarSnapshot Capture();
    WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command);
    bool TryResolveAppliedEndpoints(string declarerId, string targetId,
        out string resolvedDeclarerId, out string resolvedTargetId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralDeclareWarApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralDeclareWarSource
    {
        DiplomacyOralDeclareWarSnapshot snapshot = source.Capture();
        WorldDiplomacyOralDeclareWarResolution resolution = WorldDiplomacyOralDeclareWarRules.ResolveCommand(
            payload, snapshot.NpcKingdomExists, snapshot.NpcKingdomId, snapshot.SpeakerHeroId,
            snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.NpcIsRuler);
        if (!resolution.IsReady)
        {
            LogRejection(ref source, snapshot, resolution);
            return "";
        }
        WorldDiplomacyDeclareWarExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log($"[DeclareWar] Rejected status={receipt.Status} code={receipt.ErrorCode}");
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.DeclarerKingdomId, receipt.TargetKingdomId,
            out string declarerId, out string targetId))
        {
            source.Log("[DeclareWar] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[DeclareWar] {declarerId} -> {targetId}");
        source.NotifyResolved();
        return "";
    }

    private static void LogRejection<TSource>(ref TSource source, DiplomacyOralDeclareWarSnapshot snapshot,
        WorldDiplomacyOralDeclareWarResolution resolution) where TSource : struct, IDiplomacyOralDeclareWarSource
    {
        switch (resolution.Status)
        {
            case WorldDiplomacyOralDeclareWarResolutionStatus.BadPayloadFormat:
                source.Log("[DeclareWar] Bad format"); break;
            case WorldDiplomacyOralDeclareWarResolutionStatus.EmptyKingdomId:
                source.Log("[DeclareWar] Empty id(s)"); break;
            case WorldDiplomacyOralDeclareWarResolutionStatus.NpcKingdomUnavailable:
                source.Log("[DeclareWar] NPC has no kingdom"); break;
            case WorldDiplomacyOralDeclareWarResolutionStatus.PlayerKingdomUnavailable:
                source.Log("[DeclareWar] Player has no kingdom"); break;
            case WorldDiplomacyOralDeclareWarResolutionStatus.PlayerDeclarerMismatch:
                source.Log($"[DeclareWar] Declarer {resolution.FirstKingdomId} != player kingdom"); break;
            case WorldDiplomacyOralDeclareWarResolutionStatus.NpcSpeakerNotRuler:
                source.Log("[DeclareWar] NPC not king"); break;
            default:
                source.Log($"[DeclareWar] Neither id matches NPC kingdom {snapshot.NpcKingdomId}"); break;
        }
    }
}
