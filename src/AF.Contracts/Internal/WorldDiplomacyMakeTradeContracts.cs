namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralMakeTradeResolutionStatus : byte
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

public readonly struct WorldDiplomacyMakeTradeCommand
{
    public WorldDiplomacyMakeTradeCommand(
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
    public bool IsValid => PlayerKingdomId.Length > 0
        && NpcKingdomId.Length > 0
        && SpeakerHeroId.Length > 0;

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyMakeTradeExecutionStatus : byte
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
    TradeBehaviorUnavailable = 10,
    TradeModelUnavailable = 11,
    AlreadyTrading = 12,
    RejectedBeforeStart = 13,
    UnknownAfterStart = 14,
    ActionNotApplied = 15
}

public readonly struct WorldDiplomacyMakeTradeExecutionReceipt
{
    public WorldDiplomacyMakeTradeExecutionReceipt(
        WorldDiplomacyMakeTradeExecutionStatus status,
        string playerKingdomId,
        string npcKingdomId,
        int appliedDurationDays,
        string errorCode)
    {
        Status = status;
        PlayerKingdomId = playerKingdomId ?? "";
        NpcKingdomId = npcKingdomId ?? "";
        AppliedDurationDays = appliedDurationDays;
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyMakeTradeExecutionStatus Status { get; }
    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public int AppliedDurationDays { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyMakeTradeExecutionStatus.Applied;
}

public interface IWorldDiplomacyMakeTradeGameActionPort
{
    WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command);
}

public readonly struct WorldDiplomacyOralMakeTradeResolution
{
    private WorldDiplomacyOralMakeTradeResolution(
        WorldDiplomacyOralMakeTradeResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyMakeTradeCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralMakeTradeResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyMakeTradeCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralMakeTradeResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralMakeTradeResolution Rejected(
        WorldDiplomacyOralMakeTradeResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralMakeTradeResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralMakeTradeResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyMakeTradeCommand command)
    {
        return new WorldDiplomacyOralMakeTradeResolution(
            WorldDiplomacyOralMakeTradeResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
