using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

public sealed class VillageAssistanceStore
{
    private readonly Dictionary<string, int> _values;
    private readonly HashSet<int> _approvedSceneTurns = new HashSet<int>();
    private readonly HashSet<int> _aidSceneTurns = new HashSet<int>();
    public VillageAssistanceStore(Dictionary<string, int> values) => _values = values ?? throw new ArgumentNullException(nameof(values));
    public int Get(string villageId, int initial)
    {
        if (string.IsNullOrWhiteSpace(villageId)) return 0;
        if (!_values.TryGetValue(villageId, out int value)) _values[villageId] = value = Math.Max(0, Math.Min(5, initial));
        return _values[villageId] = Math.Max(0, Math.Min(VillageGovernanceRules.AssistanceMaximum, value));
    }
    public bool Gain(string villageId, int initial)
    {
        if (string.IsNullOrWhiteSpace(villageId)) return false;
        int value = Get(villageId, initial);
        if (value >= VillageGovernanceRules.AssistanceMaximum) return false;
        _values[villageId] = value + 1;
        return true;
    }
    public bool TrySpend(string villageId, int initial)
    {
        int value = Get(villageId, initial);
        if (value <= 0) return false;
        _values[villageId] = value - 1;
        return true;
    }
    public void ResetSceneTurns() { _approvedSceneTurns.Clear(); _aidSceneTurns.Clear(); }
    public bool GainForSceneTurn(string villageId, int initial, int sceneTurn)
    {
        if (sceneTurn > 0 && (_approvedSceneTurns.Contains(sceneTurn) || _aidSceneTurns.Contains(sceneTurn))) return false;
        if (!Gain(villageId, initial)) return false;
        if (sceneTurn > 0) _approvedSceneTurns.Add(sceneTurn);
        return true;
    }
    public bool CanSpendForSceneTurn(string villageId, int initial, int sceneTurn)
        => Get(villageId, initial) > 0 && (sceneTurn <= 0 || !_approvedSceneTurns.Contains(sceneTurn));
    public bool SpendForSceneTurn(string villageId, int initial, int sceneTurn)
    {
        if (!CanSpendForSceneTurn(villageId, initial, sceneTurn) || !TrySpend(villageId, initial)) return false;
        if (sceneTurn > 0) _aidSceneTurns.Add(sceneTurn);
        return true;
    }
}

public sealed class VillageDispositionRewardPlan
{
    internal VillageDispositionRewardPlan(VillageDispositionActionKind kind, int gold, int food,
        Dictionary<string, int> goods, Dictionary<string, int> livestock, int basicRecruits, int extraRecruits, int slaves)
    {
        Kind = kind; Gold = gold; Food = food; Goods = goods; Livestock = livestock;
        BasicRecruits = basicRecruits; ExtraRecruits = extraRecruits; Slaves = slaves;
    }
    public VillageDispositionActionKind Kind { get; }
    public int Gold { get; }
    public int Food { get; }
    public IReadOnlyDictionary<string, int> Goods { get; }
    public IReadOnlyDictionary<string, int> Livestock { get; }
    public int BasicRecruits { get; }
    public int ExtraRecruits { get; }
    public int Slaves { get; }
    public bool HasRewards => Gold > 0 || Food > 0 || BasicRecruits > 0 || ExtraRecruits > 0 || Slaves > 0
        || Goods.Values.Any(value => value > 0) || Livestock.Values.Any(value => value > 0);
}

/// <summary>One frozen village budget; forced/voluntary claims draw from the same pools.</summary>
public sealed class VillageDispositionEconomy
{
    private readonly int _gold, _food, _basicRecruits, _forcedExtra, _voluntaryExtra, _slaves;
    private readonly Dictionary<string, int> _goods, _livestock;
    private int _goldTaken, _foodTaken, _basicTaken, _extraTaken, _slavesTaken;
    private readonly Dictionary<string, int> _goodsTaken = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _livestockTaken = new Dictionary<string, int>(StringComparer.Ordinal);

    public VillageDispositionEconomy(double hearth, double ransomDraw, double foodDraw, int nativeBasicRecruits,
        IReadOnlyDictionary<string, int> nativeOtherLootMaximum, IReadOnlyDictionary<string, int> nativeLivestockMaximum)
    {
        Households = Count(hearth);
        _gold = Count(Households * (5d + 3d * Unit(ransomDraw)));
        _food = Count(Households * (1d + Unit(foodDraw)));
        _basicRecruits = Math.Max(0, nativeBasicRecruits);
        _forcedExtra = Count(Households * 0.7d); _voluntaryExtra = Count(Households * 0.9d); _slaves = Count(Households * 0.8d);
        _goods = Scale(nativeOtherLootMaximum, 0.7d); _livestock = Scale(nativeLivestockMaximum, 1d);
    }

    public int Households { get; }
    public VillageDispositionRewardPlan Prepare(VillageDispositionActionKind kind)
    {
        bool all = kind == VillageDispositionActionKind.Plunder || kind == VillageDispositionActionKind.MassacreAndSeize || kind == VillageDispositionActionKind.Destroy;
        double factor = kind == VillageDispositionActionKind.Destroy ? 0.6d : 1d;
        bool gold = all || kind == VillageDispositionActionKind.ForcedRansom || kind == VillageDispositionActionKind.VoluntaryRansom;
        bool food = all || kind == VillageDispositionActionKind.ForcedFood || kind == VillageDispositionActionKind.VoluntaryFood;
        bool goods = all || kind == VillageDispositionActionKind.ForcedGoods || kind == VillageDispositionActionKind.VoluntaryGoods;
        bool animals = all || kind == VillageDispositionActionKind.ForcedLivestock || kind == VillageDispositionActionKind.VoluntaryLivestock;
        bool recruit = kind == VillageDispositionActionKind.ForcedRecruitment || kind == VillageDispositionActionKind.VolunteerFollowers;
        int extra = kind == VillageDispositionActionKind.VolunteerFollowers ? _voluntaryExtra : _forcedExtra;
        return new VillageDispositionRewardPlan(kind,
            gold ? Math.Max(0, Count(_gold * factor) - _goldTaken) : 0,
            food ? Math.Max(0, Count(_food * factor) - _foodTaken) : 0,
            goods ? Remaining(_goods, _goodsTaken, factor) : new Dictionary<string, int>(),
            animals ? Remaining(_livestock, _livestockTaken, factor) : new Dictionary<string, int>(),
            recruit ? Math.Max(0, _basicRecruits - _basicTaken) : 0,
            recruit ? Math.Max(0, extra - _extraTaken) : 0,
            kind == VillageDispositionActionKind.Enslave ? Math.Max(0, _slaves - _slavesTaken) : 0);
    }

    public bool Commit(VillageDispositionRewardPlan plan)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        VillageDispositionRewardPlan current = Prepare(plan.Kind);
        if (!plan.HasRewards || plan.Gold != current.Gold || plan.Food != current.Food
            || plan.BasicRecruits != current.BasicRecruits || plan.ExtraRecruits != current.ExtraRecruits || plan.Slaves != current.Slaves
            || !Same(plan.Goods, current.Goods) || !Same(plan.Livestock, current.Livestock)) return false;
        _goldTaken += plan.Gold; _foodTaken += plan.Food; _basicTaken += plan.BasicRecruits;
        _extraTaken += plan.ExtraRecruits; _slavesTaken += plan.Slaves;
        foreach (var entry in plan.Goods) Add(_goodsTaken, entry.Key, entry.Value);
        foreach (var entry in plan.Livestock) Add(_livestockTaken, entry.Key, entry.Value);
        if (plan.Kind == VillageDispositionActionKind.Destroy) DestroyRemainingResources();
        return true;
    }

    // Destruction yields only 60%; the other 40% is destroyed, not a second haul.
    public void DestroyRemainingResources()
    {
        _goldTaken = _gold; _foodTaken = _food;
        foreach (var entry in _goods) _goodsTaken[entry.Key] = entry.Value;
        foreach (var entry in _livestock) _livestockTaken[entry.Key] = entry.Value;
    }

    public static float HearthAfterSlaves(float currentHearth, int acceptedSlaves) => Math.Max(0f, currentHearth - Math.Max(0, acceptedSlaves) * 0.5f);
    public static float HearthAfterDestruction(float currentHearth, float initialHearth, bool pureDestruction)
        => Math.Max(0f, Math.Min(currentHearth, initialHearth * (pureDestruction ? 0.01f : 0.1f)));

    // Native raid upper envelope, not a simulated raid: no RNG, inventory mutation or MapEvent is started.
    public static double MaximumRaidHearthLoss(double hearth, double remainingHitPoints)
        => Math.Max(0d, hearth) * 0.5d * Unit(remainingHitPoints);
    public static int MaximumRaidStockLoot(int stock, double remainingHitPoints, bool canLoot, int foodMultiplier = 1)
        => canLoot ? Count(Math.Min(Math.Max(0, stock), Math.Ceiling((float)Math.Max(0, stock) * (float)Unit(remainingHitPoints))) * Math.Max(1, foodMultiplier)) : 0;
    public static int MaximumRaidProductionLoot(double hearthLoss, double productionRate, double lootMultiplier)
        => Count(Math.Max(0d, hearthLoss) * Math.Max(0d, productionRate) / 60d * Math.Max(0d, lootMultiplier));

    public static int Count(double value) => double.IsNaN(value) || value <= 0 ? 0 : (int)Math.Min(int.MaxValue, Math.Floor(value));
    public static IReadOnlyList<int> AllocateByWeights(int total, IReadOnlyList<double> weights)
    {
        var result = new int[weights.Count];
        var normalized = weights.Select(value => double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, value)).ToArray();
        double sum = normalized.Sum();
        if (total <= 0 || sum <= 0) return result;
        var remainders = new double[weights.Count];
        int remaining = total;
        for (int i = 0; i < weights.Count; i++)
        {
            double exact = total * (normalized[i] / sum);
            result[i] = Math.Min(remaining, Count(exact));
            remaining -= result[i]; remainders[i] = exact - result[i];
        }
        foreach (int index in Enumerable.Range(0, weights.Count).Where(i => normalized[i] > 0)
            .OrderByDescending(i => remainders[i]).ThenBy(i => i))
        {
            if (remaining <= 0) break;
            result[index]++; remaining--;
        }
        return result;
    }
    private static double Unit(double value) => double.IsNaN(value) ? 0 : Math.Max(0d, Math.Min(1d, value));
    private static Dictionary<string, int> Scale(IReadOnlyDictionary<string, int> source, double factor)
        => (source ?? new Dictionary<string, int>()).Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => Count(pair.Value * factor), StringComparer.Ordinal);
    private static Dictionary<string, int> Remaining(Dictionary<string, int> full, Dictionary<string, int> taken, double factor)
        => full.ToDictionary(pair => pair.Key, pair => Math.Max(0, Count(pair.Value * factor) - (taken.TryGetValue(pair.Key, out int value) ? value : 0)), StringComparer.Ordinal);
    private static void Add(Dictionary<string, int> target, string key, int amount) => target[key] = (target.TryGetValue(key, out int value) ? value : 0) + amount;
    private static bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
        => a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out int value) && value == pair.Value);
}

/// <summary>Scene-wide maximum negative severity, not a penalty for every resource bucket.</summary>
public sealed class VillageDispositionPoliticalLedger
{
    private readonly Dictionary<string, int> _targets = new Dictionary<string, int>(StringComparer.Ordinal);
    public void Clear() => _targets.Clear();
    public int AdditionalPenalty(string heroId, int desired, bool merciful)
    {
        if (string.IsNullOrWhiteSpace(heroId) || desired >= 0) return 0;
        if (merciful) desired *= 2;
        int old = _targets.TryGetValue(heroId, out int value) ? value : 0;
        return Math.Min(old, desired) - old;
    }
    public void CommitPenalty(string heroId, int desired, bool merciful)
    {
        if (string.IsNullOrWhiteSpace(heroId) || desired >= 0) return;
        if (merciful) desired *= 2;
        _targets[heroId] = Math.Min(_targets.TryGetValue(heroId, out int value) ? value : 0, desired);
    }
}
