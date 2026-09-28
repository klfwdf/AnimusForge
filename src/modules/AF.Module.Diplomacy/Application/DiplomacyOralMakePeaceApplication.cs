using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal readonly struct DiplomacyOralRoyalSnapshot
{
    internal DiplomacyOralRoyalSnapshot(bool playerKingdomExists, string playerKingdomId,
        bool playerKingdomEliminated, bool playerIsRuler, bool npcKingdomExists,
        string npcKingdomId, string speakerHeroId, bool npcIsRuler)
    {
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomId = playerKingdomId;
        PlayerKingdomEliminated = playerKingdomEliminated;
        PlayerIsRuler = playerIsRuler;
        NpcKingdomExists = npcKingdomExists;
        NpcKingdomId = npcKingdomId;
        SpeakerHeroId = speakerHeroId;
        NpcIsRuler = npcIsRuler;
    }
    internal bool PlayerKingdomExists { get; }
    internal string PlayerKingdomId { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal bool PlayerIsRuler { get; }
    internal bool NpcKingdomExists { get; }
    internal string NpcKingdomId { get; }
    internal string SpeakerHeroId { get; }
    internal bool NpcIsRuler { get; }
}

internal interface IDiplomacyOralMakePeaceSource
{
    DiplomacyOralRoyalSnapshot Capture();
    WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command);
    bool TryResolveAppliedEndpoints(string payerId, string receiverId,
        out string resolvedPayerId, out string resolvedReceiverId);
    void NotifyResolved();
    void Log(string message);
}

internal static class DiplomacyOralMakePeaceApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralMakePeaceSource
    {
        DiplomacyOralRoyalSnapshot snapshot = source.Capture();
        WorldDiplomacyOralMakePeaceResolution resolution = WorldDiplomacyOralMakePeaceRules.ResolveCommand(
            payload, snapshot.PlayerKingdomExists, snapshot.PlayerKingdomId,
            snapshot.PlayerKingdomEliminated, snapshot.PlayerIsRuler,
            snapshot.NpcKingdomExists, snapshot.NpcKingdomId,
            snapshot.SpeakerHeroId, snapshot.NpcIsRuler);
        if (!resolution.IsReady)
        {
            source.Log($"[MakePeace] Rejected status={resolution.Status}");
            return "";
        }
        WorldDiplomacyMakePeaceExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log($"[MakePeace] Rejected status={receipt.Status} code={receipt.ErrorCode}");
            return "";
        }
        if (!source.TryResolveAppliedEndpoints(receipt.PayerKingdomId, receipt.ReceiverKingdomId,
            out string payerId, out string receiverId))
        {
            source.Log("[MakePeace] Applied but receipt endpoints are unavailable");
            return "";
        }
        source.Log($"[MakePeace] {payerId}->{receiverId} tribute={receipt.AppliedDailyTribute} days={receipt.AppliedDurationDays}");
        source.NotifyResolved();
        return "";
    }
}
