using System.Collections.Generic;

namespace AnimusForge;

internal interface IWorldDiplomacyPublicationPort
{
    WorldDiplomacyStorage Storage { get; }
    string ResolveKingdomId(string id);
    bool CanAiAuthor(string authorId, out string reason);
    bool HasAuthority(string kingdomId);
    bool IsPlayerAffiliated(string kingdomId);
    bool IsPlayerKingdom(string kingdomId);
    bool RepresentsAddressedVassal(string kingdomId, WorldDiplomacyDocument document);
    WorldDiplomacyRound ResolveRound(string id);
    string ResolveOriginSettlementId(string authorId);
    WorldDiplomacyPublicationSnapshot CaptureDestinations(string authorId, string originId);
    int CurrentDay { get; }
    int ParticipantLimit { get; }
    int CivilianSpreadDays { get; }
    int CourtDeliveryDays { get; }
    void Log(string message);
}

internal sealed class WorldDiplomacyPublicationSnapshot
{
    internal readonly IReadOnlyList<WorldDiplomacyPropagationApplication.SettlementTarget> Settlements;
    internal readonly IReadOnlyList<WorldDiplomacyPropagationApplication.CourtTarget> Courts;
    internal readonly float MaximumCivilianDistance;
    internal readonly float MaximumCourtDistance;

    internal WorldDiplomacyPublicationSnapshot(
        IReadOnlyList<WorldDiplomacyPropagationApplication.SettlementTarget> settlements,
        IReadOnlyList<WorldDiplomacyPropagationApplication.CourtTarget> courts,
        float maximumCivilianDistance, float maximumCourtDistance)
    {
        Settlements = settlements;
        Courts = courts;
        MaximumCivilianDistance = maximumCivilianDistance;
        MaximumCourtDistance = maximumCourtDistance;
    }
}
