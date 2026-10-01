using System;
using System.Collections.Generic;
using System.Linq;
using Entry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
using Section = AnimusForge.MyBehavior.PartyTransferEntrySection;
namespace AnimusForge;
internal sealed class PartyTransferAuthorizationSnapshot
{
    internal List<Entry> HiddenTroops, IndexedTroops, AllTroops, AllPrisoners;
    internal int VolunteerCount;
}
internal static class PartyTransferAuthorizationOwner
{
    internal static bool CanDiscuss(bool lord, bool notable, bool wilderness) => lord || notable || wilderness;
    internal static bool CanExecute(bool mainThread, bool lord, bool notable, bool wilderness)
        => mainThread && CanDiscuss(lord, notable, wilderness);
    internal static int ClampWildernessAmount(int requested, bool hasWildernessSource, bool matchesSource,
        bool representative, int currentStock)
    {
        int amount = Math.Max(0, requested);
        if (!hasWildernessSource) return amount;
        if (!matchesSource) return 0;
        return representative ? Math.Min(amount, Math.Max(0, currentStock - 1)) : amount;
    }
    internal static PartyTransferAuthorizationSnapshot Build(IEnumerable<Entry> entries, int maximumTier,
        bool wilderness, Func<Entry,int> tier)
    {
        var list = (entries ?? Enumerable.Empty<Entry>()).Where(x => x != null).ToList();
        var troops = list.Where(x => x.Section == Section.NpcTroops).ToList();
        var allowed = troops.Where(x => tier(x) > 0 && tier(x) <= maximumTier).ToList();
        var allowedSet = new HashSet<Entry>(allowed);
        var volunteers = list.Where(x => x.Section == Section.NpcVolunteers).ToList();
        var indexed = PartyTransferProjectionOwner.BuildDisplayIndexedPartyTransferEntries(allowed.Concat(volunteers));
        return new PartyTransferAuthorizationSnapshot {
            HiddenTroops = troops.Where(x => !allowedSet.Contains(x)).ToList(),
            IndexedTroops = indexed, VolunteerCount = volunteers.Count,
            AllTroops = wilderness ? indexed : PartyTransferProjectionOwner.BuildDisplayIndexedPartyTransferEntries(troops.Concat(volunteers)),
            AllPrisoners = PartyTransferProjectionOwner.BuildDisplayIndexedPartyTransferEntries(list.Where(x => x.Section == Section.NpcPrisoners))
        };
    }
    internal static IEnumerable<Entry> FilterAllowed(IEnumerable<Entry> entries, int maximumTier, Func<Entry,int> tier)
    {
        var list = (entries ?? Enumerable.Empty<Entry>()).Where(x => x != null).ToList();
        var troops = maximumTier > 0 ? list.Where(x => x.Section == Section.NpcTroops && tier(x) > 0 && tier(x) <= maximumTier) : Enumerable.Empty<Entry>();
        return troops.Concat(list.Where(x => x.Section == Section.NpcVolunteers));
    }
}
