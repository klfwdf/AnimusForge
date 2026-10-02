namespace AnimusForge;

/// <summary>
/// Immutable scalar view of the live hero, clan, and player-kingdom facts needed for proactive discussion eligibility.
/// </summary>
public readonly struct WorldDiplomacyProactiveSpeakerCandidate
{
    public WorldDiplomacyProactiveSpeakerCandidate(
        bool heroExists,
        bool playerKingdomExists,
        bool playerKingdomIsEliminated,
        bool clanExists,
        bool clanBelongsToPlayerKingdom,
        bool isPlayerClan,
        bool isUnderMercenaryService,
        bool isClanTypeMercenary,
        bool isLord)
    {
        HeroExists = heroExists;
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomIsEliminated = playerKingdomIsEliminated;
        ClanExists = clanExists;
        ClanBelongsToPlayerKingdom = clanBelongsToPlayerKingdom;
        IsPlayerClan = isPlayerClan;
        IsUnderMercenaryService = isUnderMercenaryService;
        IsClanTypeMercenary = isClanTypeMercenary;
        IsLord = isLord;
    }

    public bool HeroExists { get; }
    public bool PlayerKingdomExists { get; }
    public bool PlayerKingdomIsEliminated { get; }
    public bool ClanExists { get; }
    public bool ClanBelongsToPlayerKingdom { get; }
    public bool IsPlayerClan { get; }
    public bool IsUnderMercenaryService { get; }
    public bool IsClanTypeMercenary { get; }
    public bool IsLord { get; }
}

public static class WorldDiplomacyProactiveSpeakerEligibilityRules
{
    public static bool IsEligible(WorldDiplomacyProactiveSpeakerCandidate candidate)
    {
        return candidate.HeroExists
            && candidate.PlayerKingdomExists
            && !candidate.PlayerKingdomIsEliminated
            && candidate.ClanExists
            && candidate.ClanBelongsToPlayerKingdom
            && !candidate.IsPlayerClan
            && !candidate.IsUnderMercenaryService
            && !candidate.IsClanTypeMercenary
            && candidate.IsLord;
    }
}
