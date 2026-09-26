using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Modules;

// Owns the persistent Taunt penalty balances; the Campaign host keeps the
// original save keys and performs Bannerlord crime/trust side effects.
internal sealed class SceneTauntPenaltyLedgerOwner
{
    private Dictionary<string, float> _deferredCrime = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _trustTenths = new(StringComparer.OrdinalIgnoreCase);

    internal bool HasDeferredCrime => _deferredCrime.Count != 0;

    internal KeyValuePair<string, float>[] PendingCrimeEntries() => _deferredCrime.ToArray();

    internal Dictionary<string, float> CaptureDeferredCrime() =>
        _deferredCrime.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0f)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

    internal void RestoreDeferredCrime(IDictionary<string, float> saved)
    {
        _deferredCrime = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        if (saved == null) return;
        foreach (KeyValuePair<string, float> entry in saved)
        {
            string id = (entry.Key ?? "").Trim();
            float amount = Math.Max(0f, entry.Value);
            if (!string.IsNullOrWhiteSpace(id) && amount > 0f)
                _deferredCrime[id] = amount;
        }
    }

    internal float GetDeferredCrime(string factionId)
    {
        string id = (factionId ?? "").Trim();
        return !string.IsNullOrWhiteSpace(id) && _deferredCrime.TryGetValue(id, out float amount)
            ? Math.Max(0f, amount) : 0f;
    }

    internal float QueueDeferredCrime(string factionId, float amount)
    {
        string id = (factionId ?? "").Trim();
        float add = Math.Max(0f, amount);
        if (string.IsNullOrWhiteSpace(id) || !(add > 0f)) return 0f;
        float total = GetDeferredCrime(id) + add;
        _deferredCrime[id] = total;
        return total;
    }

    internal float ClearDeferredCrime(string factionId)
    {
        string id = (factionId ?? "").Trim();
        float amount = GetDeferredCrime(id);
        if (!string.IsNullOrWhiteSpace(id)) _deferredCrime.Remove(id);
        return amount;
    }

    // Reserve before the native action, matching the old re-entry behavior.
    // The host restores the original amount if that action throws.
    internal float ReserveNativeCommit(string factionId, float nativeCrime, float maxCrime)
    {
        string id = (factionId ?? "").Trim();
        float pending = GetDeferredCrime(id);
        float room = Math.Max(0f, maxCrime - Math.Max(0f, nativeCrime));
        float add = Math.Min(pending, room);
        if (!(add > 0f)) return 0f;
        float remaining = Math.Max(0f, pending - add);
        if (remaining > 0f) _deferredCrime[id] = remaining;
        else _deferredCrime.Remove(id);
        return add;
    }

    internal void RestoreFailedNativeCommit(string factionId, float originalAmount)
    {
        string id = (factionId ?? "").Trim();
        float amount = Math.Max(0f, originalAmount);
        if (!string.IsNullOrWhiteSpace(id) && amount > 0f) _deferredCrime[id] = amount;
    }

    internal Dictionary<string, int> CaptureTrustTenths() =>
        _trustTenths.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

    internal void RestoreTrustTenths(IDictionary<string, int> saved)
    {
        _trustTenths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (saved == null) return;
        foreach (KeyValuePair<string, int> entry in saved)
        {
            string id = (entry.Key ?? "").Trim();
            int tenths = Math.Max(0, entry.Value);
            if (!string.IsNullOrWhiteSpace(id) && tenths > 0)
                _trustTenths[id] = tenths;
        }
    }

    internal int AwardCriminalKnockdownTrust(string settlementId, out int carryTenths)
    {
        string id = (settlementId ?? "").Trim();
        carryTenths = 0;
        if (string.IsNullOrWhiteSpace(id)) return 0;
        _trustTenths.TryGetValue(id, out int previous);
        int total = Math.Max(0, previous) + 13;
        int whole = total / 10;
        carryTenths = total % 10;
        if (carryTenths > 0) _trustTenths[id] = carryTenths;
        else _trustTenths.Remove(id);
        return whole;
    }

    internal void ClearForMainHeroDeath() => _deferredCrime.Clear();
}
