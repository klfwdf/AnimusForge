using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

// Optional arguments extend the existing protocol; bare tags retain minimum delivery amounts.
public sealed class FriendlyVillageActionInvocation
{
    private FriendlyVillageActionInvocation(FriendlyVillageAction action, int? amount, string item)
    { Action = action; Amount = amount; Item = item; }
    public FriendlyVillageAction Action { get; }
    public int? Amount { get; }
    public string Item { get; }
    public string Tag => "[VILLAGE_ACTION:" + Action.Name
        + (Amount.HasValue ? "|amount=" + Amount.Value.ToString(CultureInfo.InvariantCulture) : "")
        + (Item.Length > 0 ? "|item=" + Item : "") + "]";

    internal static bool TryCreate(FriendlyVillageAction action, IEnumerable<string> fields, out FriendlyVillageActionInvocation invocation)
    {
        invocation = null;
        int? amount = null; string item = "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string field in fields)
        {
            int split = field.IndexOf('=');
            if (action.Gift < 0 || split < 1) return false;
            string key = field.Substring(0, split).Trim(), value = field.Substring(split + 1).Trim();
            if (!seen.Add(key)) return false;
            if (key.Equals("amount", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)) return false;
                amount = parsed;
            }
            else if (key.Equals("item", StringComparison.OrdinalIgnoreCase))
            {
                if (action.Gift == 0 || value.Length == 0 || value.Length > 128 || value.Any(char.IsControl)
                    || value.IndexOfAny(new[] {'[', ']', '|', '='}) >= 0) return false;
                item = value;
            }
            else return false;
        }
        invocation = new FriendlyVillageActionInvocation(action, amount, item);
        return true;
    }
}

public sealed class VillageAidStock<T>
{
    public T Item { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
    public int Price { get; set; }
    public bool HorseOrPackAnimal { get; set; }
    public float RandomOrder { get; set; }
}

public enum VillageAidPlanFailure { None, BelowMinimum, UnknownOrAmbiguousItem, InsufficientStock }

public static class VillageAidRules
{
    public static int Minimum(int kind) => kind == 0 ? 1000 : kind == 1 ? 10 : kind == 2 ? 5 : kind == 3 ? 3 : int.MaxValue;

    // Input is already filtered to the requested category. Select once; confirmation must not reroll or substitute.
    public static VillageAidPlanFailure Plan<T>(int kind, int amount, string requestedItem,
        IEnumerable<VillageAidStock<T>> stock, out List<KeyValuePair<T, int>> transfer)
    {
        transfer = new List<KeyValuePair<T, int>>();
        if (kind < 1 || kind > 3 || amount < Minimum(kind)) return VillageAidPlanFailure.BelowMinimum;
        var candidates = stock.Where(s => s.Count > 0).ToList();
        if (!string.IsNullOrWhiteSpace(requestedItem))
        {
            var matches = candidates.Where(s => string.Equals(s.Id, requestedItem, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) matches = candidates.Where(s => string.Equals(s.Name, requestedItem, StringComparison.Ordinal)).ToList();
            if (matches.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
                return VillageAidPlanFailure.UnknownOrAmbiguousItem;
            candidates = matches;
        }
        if (candidates.Sum(s => (long)s.Count) < amount) return VillageAidPlanFailure.InsufficientStock;
        var ordered = candidates.OrderBy(s => kind == 3 && s.HorseOrPackAnimal ? 1 : 0)
            .ThenBy(s => kind == 3 && !s.HorseOrPackAnimal ? s.RandomOrder : 0f)
            .ThenBy(s => s.Price).ThenBy(s => s.Id, StringComparer.Ordinal);
        int left = amount;
        foreach (var stack in ordered)
        {
            int take = Math.Min(left, stack.Count);
            transfer.Add(new KeyValuePair<T, int>(stack.Item, take));
            left -= take;
            if (left == 0) break;
        }
        return VillageAidPlanFailure.None;
    }

    // One actual delivery = one count, regardless of its size/type. Persist only the 0/1 remainder.
    public static bool RecordDelivery(IDictionary<string, int> remainder, string villageId)
    {
        bool award = remainder.TryGetValue(villageId, out int previous) && previous == 1;
        remainder[villageId] = award ? 0 : 1;
        return award;
    }
}
