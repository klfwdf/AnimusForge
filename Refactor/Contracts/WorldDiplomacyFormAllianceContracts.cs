namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralFormAllianceResolutionStatus : byte
{
    BadPayloadFormat = 0,
    EmptyKingdomId = 1,
    PlayerKingdomUnavailable = 2,
    NpcKingdomUnavailable = 3,
    PlayerNotRuler = 4,
    NpcSpeakerNotRuler = 5,
    KingdomPairMismatch = 6,
    Ready = 7
}

public readonly struct WorldDiplomacyFormAllianceCommand
{
    public WorldDiplomacyFormAllianceCommand(
        string playerKingdomId,
        string npcKingdomId,
        string speakerHeroId,
        string durationToken)
    {
        PlayerKingdomId = Normalize(playerKingdomId);
        NpcKingdomId = Normalize(npcKingdomId);
        SpeakerHeroId = Normalize(speakerHeroId);
        DurationToken = Normalize(durationToken);
    }

    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public string SpeakerHeroId { get; }
    public string DurationToken { get; }
    public bool IsValid => !string.IsNullOrEmpty(PlayerKingdomId)
        && !string.IsNullOrEmpty(NpcKingdomId)
        && !string.IsNullOrEmpty(SpeakerHeroId);

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyFormAllianceExecutionStatus : byte
{
    Applied = 0,
    InvalidCommand = 1,
    NotMainThread = 2,
    PlayerKingdomUnavailable = 3,
    NpcKingdomUnavailable = 4,
    PlayerKingdomEliminated = 5,
    NpcKingdomEliminated = 6,
    SameKingdom = 7,
    PlayerUnauthorized = 8,
    SpeakerUnauthorized = 9,
    AllianceBehaviorUnavailable = 10,
    AlreadyAllied = 11,
    PlayerAllianceLimitReached = 12,
    NpcAllianceLimitReached = 13,
    RejectedBeforeStart = 14,
    UnknownAfterStart = 15,
    ActionNotApplied = 16
}

public readonly struct WorldDiplomacyFormAllianceExecutionReceipt
{
    public WorldDiplomacyFormAllianceExecutionReceipt(
        WorldDiplomacyFormAllianceExecutionStatus status,
        string playerKingdomId,
        string npcKingdomId,
        string errorCode)
    {
        Status = status;
        PlayerKingdomId = playerKingdomId ?? "";
        NpcKingdomId = npcKingdomId ?? "";
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyFormAllianceExecutionStatus Status { get; }
    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyFormAllianceExecutionStatus.Applied;
}

public interface IWorldDiplomacyFormAllianceGameActionPort
{
    WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command);
}

public readonly struct WorldDiplomacyOralFormAllianceResolution
{
    private WorldDiplomacyOralFormAllianceResolution(
        WorldDiplomacyOralFormAllianceResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyFormAllianceCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralFormAllianceResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyFormAllianceCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralFormAllianceResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralFormAllianceResolution Rejected(
        WorldDiplomacyOralFormAllianceResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralFormAllianceResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralFormAllianceResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyFormAllianceCommand command)
    {
        return new WorldDiplomacyOralFormAllianceResolution(
            WorldDiplomacyOralFormAllianceResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
