namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralCancelTradeResolutionStatus : byte
{
    BadPayloadFormat = 0,
    EmptyKingdomId = 1,
    PlayerKingdomUnavailable = 2,
    NpcKingdomUnavailable = 3,
    KingdomPairMismatch = 4,
    Ready = 5
}

public readonly struct WorldDiplomacyCancelTradeCommand
{
    public WorldDiplomacyCancelTradeCommand(
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
    public bool IsValid => PlayerKingdomId.Length > 0
        && NpcKingdomId.Length > 0
        && SpeakerHeroId.Length > 0;

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyCancelTradeExecutionStatus : byte
{
    Applied = 0,
    InvalidCommand = 1,
    NotMainThread = 2,
    PlayerKingdomUnavailable = 3,
    NpcKingdomUnavailable = 4,
    PlayerKingdomEliminated = 5,
    SameKingdom = 6,
    PlayerUnauthorized = 7,
    SpeakerUnauthorized = 8,
    TradeBehaviorUnavailable = 9,
    NotTrading = 10,
    RejectedBeforeStart = 11,
    UnknownAfterStart = 12,
    ActionNotApplied = 13
}

public readonly struct WorldDiplomacyCancelTradeExecutionReceipt
{
    public WorldDiplomacyCancelTradeExecutionReceipt(
        WorldDiplomacyCancelTradeExecutionStatus status,
        string playerKingdomId,
        string npcKingdomId,
        string errorCode)
    {
        Status = status;
        PlayerKingdomId = playerKingdomId ?? "";
        NpcKingdomId = npcKingdomId ?? "";
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyCancelTradeExecutionStatus Status { get; }
    public string PlayerKingdomId { get; }
    public string NpcKingdomId { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyCancelTradeExecutionStatus.Applied;
}

public interface IWorldDiplomacyCancelTradeGameActionPort
{
    WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command);
}

public readonly struct WorldDiplomacyOralCancelTradeResolution
{
    private WorldDiplomacyOralCancelTradeResolution(
        WorldDiplomacyOralCancelTradeResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyCancelTradeCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralCancelTradeResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyCancelTradeCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralCancelTradeResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralCancelTradeResolution Rejected(
        WorldDiplomacyOralCancelTradeResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralCancelTradeResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralCancelTradeResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyCancelTradeCommand command)
    {
        return new WorldDiplomacyOralCancelTradeResolution(
            WorldDiplomacyOralCancelTradeResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
