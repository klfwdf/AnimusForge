namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralIndependentClanPeaceResolutionStatus : byte
{
    UnexpectedPayload = 0,
    ContextUnavailable = 1,
    MissingIdentity = 2,
    Ready = 3
}

public readonly struct WorldDiplomacyIndependentClanPeaceCommand
{
    public WorldDiplomacyIndependentClanPeaceCommand(
        string playerClanId,
        string targetKingdomId,
        string speakerHeroId)
    {
        PlayerClanId = Normalize(playerClanId);
        TargetKingdomId = Normalize(targetKingdomId);
        SpeakerHeroId = Normalize(speakerHeroId);
    }

    public string PlayerClanId { get; }
    public string TargetKingdomId { get; }
    public string SpeakerHeroId { get; }
    public bool IsValid => PlayerClanId.Length > 0
        && TargetKingdomId.Length > 0
        && SpeakerHeroId.Length > 0;

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyIndependentClanPeaceExecutionStatus : byte
{
    Applied = 0,
    InvalidCommand = 1,
    NotMainThread = 2,
    PlayerClanUnavailable = 3,
    SpeakerUnavailable = 4,
    TargetKingdomUnavailable = 5,
    PlayerClanEliminated = 6,
    TargetKingdomEliminated = 7,
    PlayerUnauthorized = 8,
    SpeakerUnauthorized = 9,
    NotAtWar = 10,
    ConstantWar = 11,
    RejectedBeforeStart = 12,
    UnknownAfterStart = 13,
    ActionNotApplied = 14
}

public readonly struct WorldDiplomacyIndependentClanPeaceExecutionReceipt
{
    public WorldDiplomacyIndependentClanPeaceExecutionReceipt(
        WorldDiplomacyIndependentClanPeaceExecutionStatus status,
        string playerClanId,
        string targetKingdomId,
        string speakerHeroId,
        string errorCode)
    {
        Status = status;
        PlayerClanId = playerClanId ?? "";
        TargetKingdomId = targetKingdomId ?? "";
        SpeakerHeroId = speakerHeroId ?? "";
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyIndependentClanPeaceExecutionStatus Status { get; }
    public string PlayerClanId { get; }
    public string TargetKingdomId { get; }
    public string SpeakerHeroId { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyIndependentClanPeaceExecutionStatus.Applied;
}

public interface IWorldDiplomacyIndependentClanPeaceGameActionPort
{
    WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(
        WorldDiplomacyIndependentClanPeaceCommand command);
}

public readonly struct WorldDiplomacyOralIndependentClanPeaceResolution
{
    private WorldDiplomacyOralIndependentClanPeaceResolution(
        WorldDiplomacyOralIndependentClanPeaceResolutionStatus status,
        WorldDiplomacyIndependentClanPeaceCommand command)
    {
        Status = status;
        Command = command;
    }

    public WorldDiplomacyOralIndependentClanPeaceResolutionStatus Status { get; }
    public WorldDiplomacyIndependentClanPeaceCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralIndependentClanPeaceResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralIndependentClanPeaceResolution Rejected(
        WorldDiplomacyOralIndependentClanPeaceResolutionStatus status)
    {
        return new WorldDiplomacyOralIndependentClanPeaceResolution(status, default);
    }

    public static WorldDiplomacyOralIndependentClanPeaceResolution Ready(
        WorldDiplomacyIndependentClanPeaceCommand command)
    {
        return new WorldDiplomacyOralIndependentClanPeaceResolution(
            WorldDiplomacyOralIndependentClanPeaceResolutionStatus.Ready,
            command);
    }
}
