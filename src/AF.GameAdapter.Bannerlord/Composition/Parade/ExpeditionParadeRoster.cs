using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

internal static class ExpeditionParadeRoster
{
    private sealed class Source
    {
        internal MobileParty Party;
        internal Banner Banner;
        internal List<TroopRosterElement> Entries;
        internal int Healthy => Entries.Sum(x => Math.Max(0, x.Number - x.WoundedNumber));
    }

    internal static List<ExpeditionParadeFormationSpec> Capture(Settlement settlement)
    {
        var sources = new List<Source>();
        var seen = new HashSet<MobileParty>();
        void Add(MobileParty party, Banner banner)
        {
            if (party?.MemberRoster == null || !seen.Add(party)) return;
            var entries = party.MemberRoster.GetTroopRoster()
                .Where(x => x.Character != null && !x.Character.IsHero && x.Number > x.WoundedNumber)
                .OrderBy(x => x.Character.StringId, StringComparer.Ordinal).ToList();
            if (entries.Count > 0) sources.Add(new Source { Party = party, Banner = banner ?? Clan.PlayerClan?.Banner, Entries = entries });
        }
        MobileParty main = MobileParty.MainParty;
        Add(main, Clan.PlayerClan?.Banner);
        Add(settlement?.Town?.GarrisonParty, settlement?.OwnerClan?.Banner);
        if (main?.Army != null && main.Army.LeaderParty == main)
            foreach (MobileParty party in main.Army.Parties.OrderBy(x => x.StringId, StringComparer.Ordinal))
                if (party == main || party.AttachedTo == main || party.CurrentSettlement == settlement)
                    Add(party, party.ActualClan?.Banner);

        int[] sourceQuota = ExpeditionParadeRules.Allocate(sources.Select(x => x.Healthy).ToArray(), ExpeditionParadeRules.DisplayLimit);
        var result = new List<ExpeditionParadeFormationSpec>();
        for (int s = 0; s < sources.Count; s++)
        {
            Source source = sources[s];
            var groups = source.Entries.GroupBy(x => Kind(x.Character)).OrderBy(g => g.Key).ToList();
            int[] groupQuota = ExpeditionParadeRules.Allocate(groups.Select(g => g.Sum(x => x.Number - x.WoundedNumber)).ToArray(), sourceQuota[s]);
            for (int g = 0; g < groups.Count; g++)
            {
                var entries = groups[g].ToList();
                int[] troopQuota = ExpeditionParadeRules.Allocate(entries.Select(x => x.Number - x.WoundedNumber).ToArray(), groupQuota[g]);
                var troops = new List<CharacterObject>();
                for (int t = 0; t < entries.Count; t++)
                    for (int n = 0; n < troopQuota[t]; n++) troops.Add(entries[t].Character);
                // Short blocks make the assembly apron and gate tail clearance bounded.
                for (int offset = 0; offset < troops.Count; offset += ExpeditionParadeRules.FormationLimit)
                    result.Add(new ExpeditionParadeFormationSpec {
                        SourceId = source.Party.StringId, SourceName = source.Party.Name.ToString(), Banner = source.Banner,
                        Kind = groups[g].Key, Mounted = groups[g].Key >= ExpeditionParadeFormationKind.Cavalry,
                        Troops = troops.Skip(offset).Take(ExpeditionParadeRules.FormationLimit).ToList()
                    });
            }
        }
        return result;
    }

    private static ExpeditionParadeFormationKind Kind(CharacterObject troop)
    {
        bool mounted = troop.IsMounted && troop.HasMount();
        return mounted ? (troop.IsRanged ? ExpeditionParadeFormationKind.HorseArcher : ExpeditionParadeFormationKind.Cavalry)
            : troop.IsRanged ? ExpeditionParadeFormationKind.Ranged : ExpeditionParadeFormationKind.Infantry;
    }
}
