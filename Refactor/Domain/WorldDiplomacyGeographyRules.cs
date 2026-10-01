using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
internal sealed class WorldDiplomacySettlementDistance
{
    internal string Id;
    internal int Index;
    internal bool IsHideout, IsOrigin;
    internal float Distance;
}
internal sealed class WorldDiplomacyKingdomDestination
{
    internal string Id;
    internal int Index;
    internal bool IsEliminated, IsAuthor;
}
internal sealed class WorldDiplomacyCourtDistance
{
    internal string KingdomId, SettlementId;
    internal bool IsPlayerAffiliated, DistanceKnown;
    internal float Distance;
}
internal static class WorldDiplomacyGeographyRules
{
    internal static List<WorldDiplomacySettlementDistance> Civilian(IReadOnlyList<WorldDiplomacySettlementDistance> raw) =>
        raw.Where(x => !x.IsHideout && !string.IsNullOrWhiteSpace(x.Id)).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
    internal static List<WorldDiplomacyKingdomDestination> Courts(IReadOnlyList<WorldDiplomacyKingdomDestination> raw, bool excludeAuthor) =>
        raw.Where(x => !x.IsEliminated && (!excludeAuthor || !x.IsAuthor) && !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
    internal static float CivilianMaximum(IReadOnlyList<WorldDiplomacySettlementDistance> raw) =>
        raw.Where(x => !x.IsHideout && !string.IsNullOrWhiteSpace(x.Id)).Select(x => x.Distance).DefaultIfEmpty(0f).Max();
    internal static float CourtMaximum(IReadOnlyList<WorldDiplomacyCourtDistance> raw) =>
        raw.Where(x => x.DistanceKnown).Select(x => x.Distance).DefaultIfEmpty(0f).Max();
    internal static float PublicationSettlementDistance(WorldDiplomacySettlementDistance raw, bool originAvailable, float maximum) =>
        raw.IsOrigin || !originAvailable ? maximum : raw.Distance;
    internal static float PublicationCourtDistance(WorldDiplomacyCourtDistance raw, float maximum) => raw.DistanceKnown ? raw.Distance : maximum;
}
