namespace AnimusForge;

/// <summary>
/// Immutable scalar projection used to choose one recent known diplomacy document.
/// </summary>
public readonly struct WorldDiplomacyProactiveDocumentCandidate
{
    public WorldDiplomacyProactiveDocumentCandidate(
        bool isReadyForPublication,
        bool isCompressed,
        int day,
        long createdUtcTicks,
        bool isKnown,
        bool isAuthoredByPlayerKingdom,
        bool isTargetingPlayerKingdom,
        bool isAddressedToPlayerKingdom,
        bool mentionsPlayerKingdom,
        bool isMajor,
        bool hasMechanicalResult)
    {
        IsReadyForPublication = isReadyForPublication;
        IsCompressed = isCompressed;
        Day = day;
        CreatedUtcTicks = createdUtcTicks;
        IsKnown = isKnown;
        IsAuthoredByPlayerKingdom = isAuthoredByPlayerKingdom;
        IsTargetingPlayerKingdom = isTargetingPlayerKingdom;
        IsAddressedToPlayerKingdom = isAddressedToPlayerKingdom;
        MentionsPlayerKingdom = mentionsPlayerKingdom;
        IsMajor = isMajor;
        HasMechanicalResult = hasMechanicalResult;
    }

    public bool IsReadyForPublication { get; }
    public bool IsCompressed { get; }
    public int Day { get; }
    public long CreatedUtcTicks { get; }
    public bool IsKnown { get; }
    public bool IsAuthoredByPlayerKingdom { get; }
    public bool IsTargetingPlayerKingdom { get; }
    public bool IsAddressedToPlayerKingdom { get; }
    public bool MentionsPlayerKingdom { get; }
    public bool IsMajor { get; }
    public bool HasMechanicalResult { get; }
}

/// <summary>
/// Immutable scalar projection used to retain the three newest known documents in a round.
/// </summary>
public readonly struct WorldDiplomacyProactiveRelatedDocumentCandidate
{
    public WorldDiplomacyProactiveRelatedDocumentCandidate(
        bool isCompressed,
        bool isKnown,
        bool isSameRound,
        int day,
        long createdUtcTicks)
    {
        IsCompressed = isCompressed;
        IsKnown = isKnown;
        IsSameRound = isSameRound;
        Day = day;
        CreatedUtcTicks = createdUtcTicks;
    }

    public bool IsCompressed { get; }
    public bool IsKnown { get; }
    public bool IsSameRound { get; }
    public int Day { get; }
    public long CreatedUtcTicks { get; }
}

public static class WorldDiplomacyProactiveDocumentSelectionRules
{
    public static string BuildDiscussionStableKey(string roundId, string documentId)
    {
        string scopeId = !string.IsNullOrWhiteSpace(roundId)
            ? roundId.Trim()
            : !string.IsNullOrWhiteSpace(documentId) ? documentId.Trim() : "";
        return "world_diplomacy:" + scopeId + ":" + (documentId ?? "");
    }

    public static bool IsEligibleRelatedDocument(
        WorldDiplomacyProactiveRelatedDocumentCandidate candidate)
    {
        return !candidate.IsCompressed
            && candidate.IsKnown
            && candidate.IsSameRound;
    }

    /// <summary>
    /// Returns a zero-based insertion slot for a stable newest-three selection, or -1 when discarded.
    /// </summary>
    public static int GetRelatedDocumentInsertionIndex(
        WorldDiplomacyProactiveRelatedDocumentCandidate candidate,
        int currentCount,
        WorldDiplomacyProactiveRelatedDocumentCandidate first,
        WorldDiplomacyProactiveRelatedDocumentCandidate second,
        WorldDiplomacyProactiveRelatedDocumentCandidate third)
    {
        if (currentCount < 0 || currentCount > 3)
        {
            return -1;
        }
        if (currentCount == 0 || IsStrictlyNewerRelatedDocument(candidate, first))
        {
            return 0;
        }
        if (currentCount == 1 || IsStrictlyNewerRelatedDocument(candidate, second))
        {
            return 1;
        }
        if (currentCount == 2 || IsStrictlyNewerRelatedDocument(candidate, third))
        {
            return 2;
        }
        return -1;
    }

    public static float CalculateUrgency(
        bool hasMechanicalResult,
        bool isTargetingPlayerKingdom,
        bool isMajor)
    {
        return hasMechanicalResult ? 82f
            : isTargetingPlayerKingdom ? 74f
            : isMajor ? 64f
            : 58f;
    }

    public static bool MeetsBaseEligibility(
        WorldDiplomacyProactiveDocumentCandidate candidate,
        int earliestDay)
    {
        return candidate.IsReadyForPublication
            && !candidate.IsCompressed
            && candidate.Day >= earliestDay
            && candidate.IsKnown;
    }

    public static bool IsEligible(
        WorldDiplomacyProactiveDocumentCandidate candidate,
        int earliestDay)
    {
        return MeetsBaseEligibility(candidate, earliestDay)
            && (candidate.IsAuthoredByPlayerKingdom
                || candidate.IsTargetingPlayerKingdom
                || candidate.IsAddressedToPlayerKingdom
                || candidate.MentionsPlayerKingdom
                || candidate.IsMajor);
    }

    /// <summary>
    /// Returns true only for a strict priority improvement. Equal candidates retain source order.
    /// </summary>
    public static bool IsStrictlyBetter(
        WorldDiplomacyProactiveDocumentCandidate candidate,
        WorldDiplomacyProactiveDocumentCandidate incumbent)
    {
        if (candidate.HasMechanicalResult != incumbent.HasMechanicalResult)
        {
            return candidate.HasMechanicalResult;
        }
        if (candidate.IsTargetingPlayerKingdom != incumbent.IsTargetingPlayerKingdom)
        {
            return candidate.IsTargetingPlayerKingdom;
        }
        if (candidate.Day != incumbent.Day)
        {
            return candidate.Day > incumbent.Day;
        }
        if (candidate.CreatedUtcTicks != incumbent.CreatedUtcTicks)
        {
            return candidate.CreatedUtcTicks > incumbent.CreatedUtcTicks;
        }
        return false;
    }

    private static bool IsStrictlyNewerRelatedDocument(
        WorldDiplomacyProactiveRelatedDocumentCandidate candidate,
        WorldDiplomacyProactiveRelatedDocumentCandidate incumbent)
    {
        if (candidate.Day != incumbent.Day)
        {
            return candidate.Day > incumbent.Day;
        }
        return candidate.CreatedUtcTicks > incumbent.CreatedUtcTicks;
    }
}
