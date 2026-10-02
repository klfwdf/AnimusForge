namespace AnimusForge;

/// <summary>
/// Immutable scalar view of the live hero/kingdom facts needed for discussion eligibility.
/// </summary>
public readonly struct WorldDiplomacyDiscussionCandidate
{
    public WorldDiplomacyDiscussionCandidate(
        bool heroExists,
        bool kingdomExists,
        bool kingdomIsEliminated,
        bool isLord,
        bool isRulingLeader)
    {
        HeroExists = heroExists;
        KingdomExists = kingdomExists;
        KingdomIsEliminated = kingdomIsEliminated;
        IsLord = isLord;
        IsRulingLeader = isRulingLeader;
    }

    public bool HeroExists { get; }
    public bool KingdomExists { get; }
    public bool KingdomIsEliminated { get; }
    public bool IsLord { get; }
    public bool IsRulingLeader { get; }
}

public static class WorldDiplomacyDiscussionEligibilityRules
{
    public static bool IsEligibleRepresentative(WorldDiplomacyDiscussionCandidate candidate)
    {
        return candidate.HeroExists
            && candidate.KingdomExists
            && !candidate.KingdomIsEliminated
            && (candidate.IsLord || candidate.IsRulingLeader);
    }

    public static bool CanDiscuss(
        WorldDiplomacyDiscussionCandidate candidate,
        bool hasKnownDocument)
    {
        return IsEligibleRepresentative(candidate) && hasKnownDocument;
    }
}
