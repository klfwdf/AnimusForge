using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IDiplomacyOralMakePeaceSource
{
    DiplomacyOralRoyalSnapshot Capture();
    WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command);
    bool TryResolveAppliedEndpoints(string payerId, string receiverId,
        out string resolvedPayerId, out string resolvedReceiverId);
    void NotifyResolved(WorldDiplomacyMakePeaceExecutionReceipt receipt);
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
        if (!receipt.PeaceApplied)
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
        source.NotifyResolved(receipt);
        return "";
    }
}
