using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.CoupSystem;

internal sealed class CoupLoyalistCandidate
{
    public string ClanId;
    public bool FormerRulingClan;
    public int RelationToOldKing;
    public int RelationToNewKing;
    public int Fortifications;
    public int ClanTier;
}

// Political eligibility (living free leader, land, realm, faction type) is checked by AF.
// Only the reason to oppose a coup and the stable leader ordering belong here.
internal static class CoupLoyalistPolicy
{
    internal static bool Opposes(bool formerRulingClan, int oldRelation, int newRelation)
        => formerRulingClan || oldRelation > newRelation;

    internal static List<CoupLoyalistCandidate> Rank(IEnumerable<CoupLoyalistCandidate> candidates)
        => candidates.Where(c => c != null && Opposes(c.FormerRulingClan, c.RelationToOldKing, c.RelationToNewKing))
            .OrderByDescending(c => c.FormerRulingClan)
            .ThenByDescending(c => c.RelationToOldKing - c.RelationToNewKing)
            .ThenByDescending(c => c.Fortifications).ThenByDescending(c => c.ClanTier)
            .ThenBy(c => c.ClanId, StringComparer.Ordinal).ToList();
}
