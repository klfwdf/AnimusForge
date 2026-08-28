namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Side assigned to an agent by the SETS/AF city-conflict bridge.
/// None means the agent stays outside the custom fight.
/// </summary>
public enum SetsCityConflictSide
{
    None = 0,
    Player = 1,
    Opponent = 2,
}

/// <summary>
/// Routing for a direct player attack inside a player-owned settlement.
/// </summary>
public enum SetsOwnedSettlementAttackRouting
{
    ExistingFlow = 0,
    PassiveSurrender = 1,
    ArmedConflict = 2,
}

/// <summary>
/// Dependency-free allegiance and attack-routing rules shared by SETS and the
/// AF peace-scene conflict adapter. Bannerlord agent/team mutation stays in AF.
/// </summary>
public static class SetsCityConflictPolicy
{
    /// <summary>
    /// Resolves one mutually exclusive side. Priority is deliberate: a selected
    /// SETS follower can never be stolen by an escort detector; the active target
    /// and its genuine escorts oppose the player; remaining settlement authority
    /// follows the settlement owner during armed conflict.
    /// </summary>
    public static SetsCityConflictSide ResolveSide(
        bool settlementControlledByPlayer,
        bool isSelectedEntryFollower,
        bool isActiveTarget,
        bool isTargetEscort,
        bool isSettlementAuthority,
        bool armedConflict)
    {
        if (isSelectedEntryFollower)
        {
            return SetsCityConflictSide.Player;
        }

        if (isActiveTarget || isTargetEscort)
        {
            return SetsCityConflictSide.Opponent;
        }

        if (!armedConflict || !isSettlementAuthority)
        {
            return SetsCityConflictSide.None;
        }

        return settlementControlledByPlayer
            ? SetsCityConflictSide.Player
            : SetsCityConflictSide.Opponent;
    }

    /// <summary>
    /// Ordinary residents may keep the existing surrender/flee response, but an
    /// attacked guard, prison guard, soldier, or lord must enter real armed combat.
    /// Criminal/alley targets remain owned by their existing native routing.
    /// </summary>
    public static SetsOwnedSettlementAttackRouting ResolveOwnedAttackRouting(
        bool settlementControlledByPlayer,
        bool isSettlementAuthority,
        bool isCriminalConflictTarget)
    {
        if (!settlementControlledByPlayer || isCriminalConflictTarget)
        {
            return SetsOwnedSettlementAttackRouting.ExistingFlow;
        }

        return isSettlementAuthority
            ? SetsOwnedSettlementAttackRouting.ArmedConflict
            : SetsOwnedSettlementAttackRouting.PassiveSurrender;
    }

    public static bool ShouldEnsureArmedReadiness(
        bool isSelectedEntryFollower,
        SetsCityConflictSide side,
        bool armedConflict)
    {
        return isSelectedEntryFollower
            && side == SetsCityConflictSide.Player
            && armedConflict;
    }

    public static bool ShouldEscalateForSelectedFollowerSupport(
        bool settlementControlledByPlayer,
        bool hasSelectedEntryFollower)
    {
        return settlementControlledByPlayer && hasSelectedEntryFollower;
    }
}
