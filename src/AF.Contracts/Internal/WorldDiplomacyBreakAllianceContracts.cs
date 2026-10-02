namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralBreakAllianceResolutionStatus : byte
{
    BadPayloadFormat = 0,
    EmptyKingdomId = 1,
    PlayerKingdomUnavailable = 2,
    NpcKingdomUnavailable = 3,
    KingdomPairMismatch = 4,
    Ready = 5
}

public readonly struct WorldDiplomacyBreakAllianceCommand
{
    public WorldDiplomacyBreakAllianceCommand(
        string playerKingdomId,
        string npcKingdomId,
        string speakerHeroId)
    {
        PlayerKingdomId = Normalize(playerKingdomId);
        NpcKingdomId = Normalize(npcKingdomId);
        SpeakerHeroId = Normalize(speakerHeroId);
    }

    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public string SpeakerHeroId { get; }
    public bool IsValid => !string.IsNullOrEmpty(PlayerKingdomId)
        && !string.IsNullOrEmpty(NpcKingdomId)
        && !string.IsNullOrEmpty(SpeakerHeroId);

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyBreakAllianceExecutionStatus : byte
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
    NotAllied = 11,
    RejectedBeforeStart = 12,
    UnknownAfterStart = 13,
    ActionNotApplied = 14
}

public readonly struct WorldDiplomacyBreakAllianceExecutionReceipt
{
    public WorldDiplomacyBreakAllianceExecutionReceipt(
        WorldDiplomacyBreakAllianceExecutionStatus status,
        string playerKingdomId,
        string npcKingdomId,
        string errorCode)
    {
        Status = status;
        PlayerKingdomId = playerKingdomId ?? "";
        NpcKingdomId = npcKingdomId ?? "";
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyBreakAllianceExecutionStatus Status { get; }
    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyBreakAllianceExecutionStatus.Applied;
}

public interface IWorldDiplomacyBreakAllianceGameActionPort
{
    WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command);
}

public readonly struct WorldDiplomacyOralBreakAllianceResolution
{
    private WorldDiplomacyOralBreakAllianceResolution(
        WorldDiplomacyOralBreakAllianceResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyBreakAllianceCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralBreakAllianceResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyBreakAllianceCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralBreakAllianceResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralBreakAllianceResolution Rejected(
        WorldDiplomacyOralBreakAllianceResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralBreakAllianceResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralBreakAllianceResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyBreakAllianceCommand command)
    {
        return new WorldDiplomacyOralBreakAllianceResolution(
            WorldDiplomacyOralBreakAllianceResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
