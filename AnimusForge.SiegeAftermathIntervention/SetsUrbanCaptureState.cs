namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Explicit lifecycle states for one SETS hostile urban-capture operation
/// (enemy town or castle only). Owned/attached settlement incidents use
/// SetsOwnedSettlementIncidentProfile and villages use the village reward
/// path; neither creates a capture session.
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

    /// <summary>All objective defenders defeated and reserves exhausted; exactly one victory commit.</summary>
    VictoryReached = 4,

    /// <summary>Mission ended after committed victory; waiting for MapState before campaign side effects.</summary>
    AwaitingMap = 5,

    /// <summary>Settlement ownership transferred to the player clan exactly once.</summary>
    OwnershipCommitted = 6,

    /// <summary>Native settlement-taken menu opened exactly once.</summary>
    NativeMenuOpened = 7,

    /// <summary>Terminal: aftermath handed to GCCZ/native flow.</summary>
    Completed = 8,

    /// <summary>
    /// Terminal-until-operator-review: live world no longer matches the operation
    /// (third-party owner, missing clan, illegal restored combination, retry cap).
    /// No further campaign side effects are permitted from this state.
    /// </summary>
    Suspended = 9
}

/// <summary>Events that drive <see cref="SetsUrbanCaptureState"/> transitions.</summary>
public enum SetsUrbanCaptureEvent
{
    PrepareEntry = 0,
    StartMission = 1,
    StartConflict = 2,
    ReachVictory = 3,
    EndMission = 4,
    CommitOwnership = 5,
    OpenNativeMenu = 6,
    Complete = 7,

    /// <summary>
    /// Abandon without side effects. Legal only before any campaign side effect
    /// (before VictoryReached); later stages must recover or suspend instead.
    /// </summary>
    Abort = 8,

    /// <summary>Force the session into Suspended after unrecoverable drift or retry exhaustion.</summary>
    Suspend = 9
}
