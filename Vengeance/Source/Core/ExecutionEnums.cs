namespace RichExecutions.Core;

public enum ExecutionTone
{
    Judicial = 0,
    Spectacle = 1,
    Terror = 2
}

public enum ExecutionSceneMode
{
    Automatic = 0,
    CustomPreset = 1
}

public enum EvidenceStrength
{
    None = 0,
    Circumstantial = 1,
    Strong = 2
}

public enum LegitimacyTier
{
    Illegal = 0,
    Disputed = 1,
    Legal = 2
}

public enum ExecutionActor
{
    Undecided = 0,
    Executioner = 1,
    Player = 2
}

public enum PrisonerSource
{
    PlayerParty = 0,
    TownDungeon = 1
}

public enum VenueAuthority
{
    None = 0,
    AlliedTown = 1,
    OwnTown = 2
}

public enum VictimPoliticalStatus
{
    BanditOrRebel = 0,
    WarEnemy = 1,
    Neutral = 2,
    SameKingdom = 3
}

public enum ExecutionSessionState
{
    Preparing = 0,
    WaitingForPlayer = 1,
    CeremonyPreparation = 2,
    // Retained as an ABI/source-compatibility alias for extensions compiled
    // before camera ownership was removed. No camera is created in this state.
    CameraPreparation = CeremonyPreparation,
    Execution = 3,
    CrowdReaction = 4,
    Aftermath = 5,
    Cancelled = 6
}

public enum ExecutionValidationStage
{
    Confirmation = 0,
    MissionStart = 1,
    LethalFrame = 2
}

public enum ExecutionFailureReason
{
    None = 0,
    InvalidRequest = 1,
    PlayerUnavailable = 2,
    VictimUnavailable = 3,
    VictimProtected = 4,
    VenueUnavailable = 5,
    InsufficientAuthority = 6,
    InsufficientInfluence = 7,
    OwnerRelationTooLow = 8,
    CancelledByExtension = 9,
    ScenePlacementFailed = 10,
    SessionNotActive = 11,
    CommitInProgress = 12,
    VanillaActionRejected = 13,
    UnexpectedError = 14
}

public enum CrowdReactionType
{
    Cheer = 0,
    Divided = 1,
    Panic = 2
}
