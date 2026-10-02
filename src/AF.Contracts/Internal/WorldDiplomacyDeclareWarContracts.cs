namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyDeclareWarDeclarerKind : byte
{
    None = 0,
    PlayerKingdom = 1,
    NpcKingdom = 2
}

public enum WorldDiplomacyOralDeclareWarResolutionStatus : byte
{
    BadPayloadFormat = 0,
    EmptyKingdomId = 1,
    NpcKingdomUnavailable = 2,
    PlayerKingdomUnavailable = 3,
    PlayerDeclarerMismatch = 4,
    NpcSpeakerNotRuler = 5,
    NpcKingdomMismatch = 6,
    Ready = 7
}

public readonly struct WorldDiplomacyDeclareWarCommand
{
    public WorldDiplomacyDeclareWarCommand(
        string declarerKingdomId,
        string targetKingdomId,
        string speakerHeroId,
        WorldDiplomacyDeclareWarDeclarerKind declarerKind)
    {
        DeclarerKingdomId = NormalizeId(declarerKingdomId);
        TargetKingdomId = NormalizeId(targetKingdomId);
        SpeakerHeroId = NormalizeId(speakerHeroId);
        DeclarerKind = declarerKind;
    }

    public string DeclarerKingdomId { get; }
    public string TargetKingdomId { get; }
    public string SpeakerHeroId { get; }
    public WorldDiplomacyDeclareWarDeclarerKind DeclarerKind { get; }
    public bool IsValid => DeclarerKind != WorldDiplomacyDeclareWarDeclarerKind.None
        && DeclarerKingdomId.Length > 0
        && TargetKingdomId.Length > 0
        && SpeakerHeroId.Length > 0;

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyDeclareWarExecutionStatus : byte
{
    Applied = 0,
    InvalidCommand = 1,
    NotMainThread = 2,
    DeclarerUnavailable = 3,
    TargetUnavailable = 4,
    SpeakerUnauthorized = 5,
    DeclarerEliminated = 6,
    TargetEliminated = 7,
    SameKingdom = 8,
    AlreadyAtWar = 9,
    Allied = 10,
    FailedBeforeStart = 11,
    UnknownAfterStart = 12,
    ActionNotApplied = 13
}

public readonly struct WorldDiplomacyDeclareWarExecutionReceipt
{
    public WorldDiplomacyDeclareWarExecutionReceipt(
        WorldDiplomacyDeclareWarExecutionStatus status,
        string declarerKingdomId,
        string targetKingdomId,
        string errorCode)
    {
        Status = status;
        DeclarerKingdomId = declarerKingdomId ?? "";
        TargetKingdomId = targetKingdomId ?? "";
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyDeclareWarExecutionStatus Status { get; }
    public string DeclarerKingdomId { get; }
    public string TargetKingdomId { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyDeclareWarExecutionStatus.Applied;
}

public interface IWorldDiplomacyDeclareWarGameActionPort
{
    WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command);
}

public readonly struct WorldDiplomacyOralDeclareWarResolution
{
    private WorldDiplomacyOralDeclareWarResolution(
        WorldDiplomacyOralDeclareWarResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyDeclareWarCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralDeclareWarResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyDeclareWarCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralDeclareWarResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralDeclareWarResolution Rejected(
        WorldDiplomacyOralDeclareWarResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralDeclareWarResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralDeclareWarResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyDeclareWarCommand command)
    {
        return new WorldDiplomacyOralDeclareWarResolution(
            WorldDiplomacyOralDeclareWarResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
