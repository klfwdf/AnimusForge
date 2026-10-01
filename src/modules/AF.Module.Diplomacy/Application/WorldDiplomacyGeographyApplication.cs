using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
internal interface IWorldDiplomacyGeographyPort
{
    bool OriginAvailable { get; }
    IReadOnlyList<WorldDiplomacySettlementDistance> Settlements();
    float SettlementDistance(int index);
    IReadOnlyList<WorldDiplomacyKingdomDestination> Kingdoms();
    WorldDiplomacyCourtDistance Court(int index);
}
internal static class WorldDiplomacyGeographyApplication
{
    private static List<WorldDiplomacySettlementDistance> ReadSettlements(IWorldDiplomacyGeographyPort port, bool publication)
    {
        var raw = port.Settlements();
        var selected = publication ? WorldDiplomacyGeographyRules.Civilian(raw) : raw.Where(x => !string.IsNullOrWhiteSpace(x.Id));
        return selected.Select(x => new WorldDiplomacySettlementDistance
        { Id=x.Id, IsOrigin=x.IsOrigin, IsHideout=x.IsHideout, Distance=port.SettlementDistance(x.Index) }).ToList();
    }
    private static List<WorldDiplomacyCourtDistance> ReadCourts(IWorldDiplomacyGeographyPort port, bool excludeAuthor) =>
        WorldDiplomacyGeographyRules.Courts(port.Kingdoms(), excludeAuthor).Select(x => port.Court(x.Index)).ToList();
    internal static List<WorldDiplomacyPropagationApplication.CourtTarget> CourtTargets(IWorldDiplomacyGeographyPort port) =>
        ReadCourts(port, false).Select(x => new WorldDiplomacyPropagationApplication.CourtTarget
        { KingdomId=x.KingdomId, SettlementId=x.SettlementId, IsPlayerAffiliated=x.IsPlayerAffiliated }).ToList();
    internal static WorldDiplomacyPublicationSnapshot Publication(IWorldDiplomacyGeographyPort port)
    {
        var settlements = ReadSettlements(port, true);
        float civilianMaximum = WorldDiplomacyGeographyRules.CivilianMaximum(settlements);
        var courts = ReadCourts(port, true);
        float courtMaximum = WorldDiplomacyGeographyRules.CourtMaximum(courts);
        return new WorldDiplomacyPublicationSnapshot(settlements.Select(x => new WorldDiplomacyPropagationApplication.SettlementTarget
        { Id=x.Id, IsOrigin=x.IsOrigin, Distance=WorldDiplomacyGeographyRules.PublicationSettlementDistance(x,port.OriginAvailable,civilianMaximum) }).ToList(),
            courts.Select(x => new WorldDiplomacyPropagationApplication.CourtTarget
            { KingdomId=x.KingdomId, SettlementId=x.SettlementId, IsPlayerAffiliated=x.IsPlayerAffiliated,
                Distance=WorldDiplomacyGeographyRules.PublicationCourtDistance(x,courtMaximum) }).ToList(), civilianMaximum, courtMaximum);
    }
    internal static WorldDiplomacyPropagationApplication.DistanceSnapshot Recalculation(IWorldDiplomacyGeographyPort port)
    {
        if (!port.OriginAvailable) return null;
        var settlements = ReadSettlements(port, false); var courts = ReadCourts(port, false);
        var settlementDistances = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in settlements)
            if (!string.IsNullOrWhiteSpace(item.Id) && !settlementDistances.ContainsKey(item.Id)) settlementDistances.Add(item.Id,item.Distance);
        var courtDistances = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in courts)
            if (item.DistanceKnown && !courtDistances.ContainsKey(item.KingdomId)) courtDistances.Add(item.KingdomId,item.Distance);
        return new WorldDiplomacyPropagationApplication.DistanceSnapshot
        { SettlementDistances=settlementDistances, CourtDistances=courtDistances,
            MaxCivilianDistance=WorldDiplomacyGeographyRules.CivilianMaximum(settlements), MaxCourtDistance=WorldDiplomacyGeographyRules.CourtMaximum(courts) };
    }
}
