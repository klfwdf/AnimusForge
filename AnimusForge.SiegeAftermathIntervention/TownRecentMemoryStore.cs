using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

// Value-only, campaign-thread owned. Seven calendar days, including the current day.
// Facts are separate from ruler narratives and never consume a ruler-history slot.
public sealed class TownRecentMemoryStore
{
    public const int RetentionDays = 7;
    public TownRecentMemorySnapshot State { get; private set; } = new TownRecentMemorySnapshot();
    private readonly Dictionary<string, TownRecentMemoryTown> _towns = new Dictionary<string, TownRecentMemoryTown>(StringComparer.Ordinal);

    public void Start(double now)
    {
        if (State.TrackingStarted < 0) State.TrackingStarted = now;
    }

    public bool Add(TownRecentMemoryFact fact, double now)
    {
        if (!ValidFact(fact) || !InWindow(fact.Day, now)) return false;
        Start(now);
        var town = Get(fact.TownId);
        Prune(town, now);
        if (town.Facts.Any(f => f.Id == fact.Id)) return false;
        town.Facts.Add(fact);
        return true;
    }

    public bool Arrival(string townId, string receipt, bool caravan, double now)
    {
        if (string.IsNullOrWhiteSpace(townId) || string.IsNullOrWhiteSpace(receipt)) return false;
        Start(now);
        var town = Get(townId);
        Prune(town, now);
        int day = (int)Math.Floor(now);
        var bucket = town.Arrivals.FirstOrDefault(b => b.Day == day);
        if (bucket == null) town.Arrivals.Add(bucket = new TownRecentArrivalDay { Day = day });
        if (!bucket.Receipts.Add(receipt)) return false;
        if (caravan) bucket.Caravans++; else bucket.Villagers++;
        return true;
    }

    public TownRecentMemoryTown Read(string townId, double now)
    {
        Start(now);
        var town = Get(townId);
        Prune(town, now);
        return town;
    }

    public bool HasCompleteArrivalWindow(double now)
        => State.TrackingStarted >= 0 && State.TrackingStarted <= Math.Floor(now) - RetentionDays + 1;

    public void PruneAll(double now)
    {
        foreach (var town in State.Towns) Prune(town, now);
    }

    public bool Restore(TownRecentMemorySnapshot snapshot)
    {
        // Validate the whole namespace before publishing any state. Unknown schemas stay with the adapter.
        if (snapshot == null || snapshot.Version != 1 || !Finite(snapshot.TrackingStarted)
            || snapshot.TrackingStarted < -1 || snapshot.Towns == null) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var town in snapshot.Towns)
        {
            if (town == null || string.IsNullOrWhiteSpace(town.TownId) || !ids.Add(town.TownId)
                || town.Facts == null || town.Arrivals == null) return false;
            var facts = new HashSet<string>(StringComparer.Ordinal);
            if (town.Facts.Any(f => !ValidFact(f) || f.TownId != town.TownId || !facts.Add(f.Id))) return false;
            var days = new HashSet<int>();
            if (town.Arrivals.Any(b => b == null || b.Day < 0 || !days.Add(b.Day) || b.Villagers < 0 || b.Caravans < 0
                || b.Receipts == null || b.Receipts.Any(string.IsNullOrWhiteSpace)
                || (long)b.Villagers + b.Caravans != b.Receipts.Count)) return false;
        }
        State = snapshot;
        _towns.Clear();
        foreach (var town in snapshot.Towns) _towns.Add(town.TownId, town);
        return true;
    }

    public static bool InWindow(int day, double now) => Finite(now) && day <= Math.Floor(now) && day > Math.Floor(now) - RetentionDays;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static bool ValidFact(TownRecentMemoryFact f) => f != null && !string.IsNullOrWhiteSpace(f.Id)
        && !string.IsNullOrWhiteSpace(f.TownId) && !string.IsNullOrWhiteSpace(f.TownName)
        && !string.IsNullOrWhiteSpace(f.ActorName) && f.Day >= 0 && f.Year >= 0 && f.Season >= 0 && f.Season < 4
        && f.DayOfSeason > 0 && TownRecentMemoryText.ActionKeys.Contains(f.Action)
        && (f.VillageName != null) && (!f.Action.StartsWith("village.", StringComparison.Ordinal) || f.VillageName.Length > 0);

    private TownRecentMemoryTown Get(string id)
    {
        if (!_towns.TryGetValue(id, out var town))
        {
            town = new TownRecentMemoryTown { TownId = id };
            _towns.Add(id, town);
            State.Towns.Add(town);
        }
        return town;
    }

    private static void Prune(TownRecentMemoryTown town, double now)
    {
        town.Facts.RemoveAll(f => !InWindow(f.Day, now));
        town.Arrivals.RemoveAll(b => !InWindow(b.Day, now));
    }

    // Stable AF receipt IDs; GCCZ will supply these only after a negative disposition completes.
    // No dependency on the separately developed village implementation or its enum layout.
    public static bool IsRecordedVillageAction(string actionId)
        => actionId == "ForcedRansom" || actionId == "ForcedFood"
        || actionId == "ForcedGoods" || actionId == "ForcedLivestock"
        || actionId == "Plunder" || actionId == "Enslave"
        || actionId == "ForcedRecruitment" || actionId == "MassacreAndSeize"
        || actionId == "Destroy";
}

public sealed class TownRecentMemorySnapshot
{
    public int Version { get; set; } = 1;
    public double TrackingStarted { get; set; } = -1;
    public bool NativeHistoryImported { get; set; }
    public List<TownRecentMemoryTown> Towns { get; set; } = new List<TownRecentMemoryTown>();
}

public sealed class TownRecentMemoryTown
{
    public string TownId { get; set; }
    public List<TownRecentMemoryFact> Facts { get; set; } = new List<TownRecentMemoryFact>();
    public List<TownRecentArrivalDay> Arrivals { get; set; } = new List<TownRecentArrivalDay>();
}

public sealed class TownRecentMemoryFact
{
    public string Id { get; set; }
    public string TownId { get; set; }
    public string TownName { get; set; }
    public string VillageName { get; set; } = "";
    public string ActorName { get; set; }
    public string Action { get; set; }
    public int Day { get; set; }
    public int Year { get; set; }
    public int Season { get; set; }
    public int DayOfSeason { get; set; }
}

public sealed class TownRecentArrivalDay
{
    public int Day { get; set; }
    public int Villagers { get; set; }
    public int Caravans { get; set; }
    public HashSet<string> Receipts { get; set; } = new HashSet<string>(StringComparer.Ordinal);
}
