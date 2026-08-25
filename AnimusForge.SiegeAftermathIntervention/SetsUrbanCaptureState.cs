namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Explicit lifecycle states for one SETS settlement-entry capture operation.
/// The hostile urban path and the owned/attached incident path diverge before
/// any ownership side effect and must never cross back.
/// </summary>
public enum SetsUrbanCaptureState
{
    Inactive = 0,

    /// <summary>Mission entry prepared (profile resolved, roster copied) but the mission has not started.</summary>
    EntryPrepared = 1,

    /// <summary>Mission started; followers may spawn; no conflict yet.</summary>
    MissionActive = 2,

    /// <summary>Hostile conflict started from a valid hit; defenders and reserves engaged.</summary>
    ConflictActive = 3,

    /// <summary>Owned or ruler-attached settlement incident triggered. Never leads to ownership transfer.</summary>
    IncidentTriggered = 4,

    /// <summary>All objective defenders defeated and reserves exhausted; exactly one victory commit.</summary>
    VictoryReached = 5,

    /// <summary>Mission ended after victory or incident; waiting for MapState before campaign side effects.</summary>
    AwaitingMap = 6,

    /// <summary>Hostile capture only: settlement ownership transferred to the player clan exactly once.</summary>
    OwnershipCommitted = 7,

    /// <summary>Native settlement-taken menu opened exactly once (hostile town/castle path).</summary>
    MenuOpened = 8,

    /// <summary>Owned-incident menu opened (owned/attached path; ownership untouched).</summary>
    OwnedIncidentMenuOpened = 9,

    /// <summary>Terminal: aftermath handed to GCCZ/native flow or village reward granted.</summary>
    Completed = 10
}

/// <summary>Events that drive <see cref="SetsUrbanCaptureState"/> transitions.</summary>
public enum SetsUrbanCaptureEvent
{
    PrepareEntry = 0,
    StartMission = 1,
    StartConflict = 2,
    TriggerOwnedIncident = 3,
    ReachVictory = 4,
    EndMission = 5,
    CommitOwnership = 6,
    OpenNativeMenu = 7,
    OpenOwnedIncidentMenu = 8,
    GrantVillageReward = 9,
    Complete = 10,

    /// <summary>Abandon without side effects (expired pending entry, normal exit without conflict, corrupt record).</summary>
    Abort = 11
}

/// <summary>How the target settlement relates to the player before entry.</summary>
public enum SetsUrbanCaptureOwnershipClassification
{
    Unknown = 0,

    /// <summary>Enemy or otherwise non-owned settlement; victory may transfer ownership.</summary>
    Hostile = 1,

    /// <summary>Player-clan settlement; incidents never transfer ownership.</summary>
    PlayerOwned = 2,

    /// <summary>Settlement of another clan attached to the player's rule; incidents never transfer ownership.</summary>
    RulerAttached = 3
}
