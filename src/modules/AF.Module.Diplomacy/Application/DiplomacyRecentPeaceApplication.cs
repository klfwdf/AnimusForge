using System;
using System.Collections.Generic;
namespace AnimusForge;
internal interface IDiplomacyRecentPeacePort
{
    void Register(string first, string second, DateTime now);
    bool ShouldBlock(string first, string second, DateTime now);
}
// Transient process-lifetime protection, matching the original guard. It is not save data.
internal sealed class DiplomacyRecentPeaceApplication : IDiplomacyRecentPeacePort
{
    private readonly Dictionary<string, DateTime> registered = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    private DateTime lastSweep;
    public void Register(string first, string second, DateTime now)
    {
        string key = DiplomacyRecentPeaceRules.PairKey(first, second);
        if (key.Length == 0) return;
        // Mutations are infrequent; hostile-action queries do only one indexed lookup, without a sweep/allocation.
        if (DiplomacyRecentPeaceRules.Expired(lastSweep, now))
        {
            List<string> expired = null;
            foreach (var pair in registered)
                if (DiplomacyRecentPeaceRules.Expired(pair.Value, now))
                { if (expired == null) expired = new List<string>(); expired.Add(pair.Key); }
            if (expired != null) foreach (string old in expired) registered.Remove(old);
            lastSweep = now;
        }
        registered[key] = now;
    }
    public bool ShouldBlock(string first, string second, DateTime now)
    {
        string key = DiplomacyRecentPeaceRules.PairKey(first, second);
        if (key.Length == 0 || !registered.TryGetValue(key, out DateTime time)) return false;
        if (!DiplomacyRecentPeaceRules.Expired(time, now)) return true;
        registered.Remove(key);
        return false;
    }
}
