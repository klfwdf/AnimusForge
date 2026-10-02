using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal readonly struct DiplomacyOralIndependentPeaceSnapshot
{
    internal DiplomacyOralIndependentPeaceSnapshot(bool contextAvailable,
        string playerClanId, string targetKingdomId, string speakerHeroId)
    {
        ContextAvailable = contextAvailable;
        PlayerClanId = playerClanId;
        TargetKingdomId = targetKingdomId;
        SpeakerHeroId = speakerHeroId;
    }
    internal bool ContextAvailable { get; }
    internal string PlayerClanId { get; }
    internal string TargetKingdomId { get; }
    internal string SpeakerHeroId { get; }
}

internal interface IDiplomacyOralIndependentPeaceSource
{
    DiplomacyOralIndependentPeaceSnapshot Capture();
    WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(WorldDiplomacyIndependentClanPeaceCommand command);
    void Log(string message);
}

internal static class DiplomacyOralIndependentPeaceApplication
{
    internal static string Execute<TSource>(ref TSource source, string payload)
        where TSource : struct, IDiplomacyOralIndependentPeaceSource
    {
        DiplomacyOralIndependentPeaceSnapshot snapshot = source.Capture();
        WorldDiplomacyOralIndependentClanPeaceResolution resolution =
            WorldDiplomacyOralIndependentClanPeaceRules.ResolveCommand(
                payload, snapshot.ContextAvailable, snapshot.PlayerClanId,
                snapshot.TargetKingdomId, snapshot.SpeakerHeroId);
        if (!resolution.IsReady)
        {
            source.Log("[IndependentClanPeace] Rejected status=" + resolution.Status);
            return "";
        }
        WorldDiplomacyIndependentClanPeaceExecutionReceipt receipt = source.Execute(resolution.Command);
        if (!receipt.IsApplied)
        {
            source.Log("[IndependentClanPeace] Rejected status=" + receipt.Status
                + " code=" + receipt.ErrorCode);
            return "";
        }
        source.Log("[IndependentClanPeace] success playerClan="
            + receipt.PlayerClanId + " targetKingdom=" + receipt.TargetKingdomId
            + " king=" + receipt.SpeakerHeroId);
        return "";
    }
}
