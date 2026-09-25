namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyOralMakePeaceResolutionStatus : byte
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

public readonly struct WorldDiplomacyMakePeaceCommand
{
    public WorldDiplomacyMakePeaceCommand(
        string payerKingdomId,
        string receiverKingdomId,
        string speakerHeroId,
        string amountToken,
        string durationToken)
    {
        PayerKingdomId = Normalize(payerKingdomId);
        ReceiverKingdomId = Normalize(receiverKingdomId);
        SpeakerHeroId = Normalize(speakerHeroId);
        AmountToken = Normalize(amountToken);
        DurationToken = Normalize(durationToken);
    }

    public string PayerKingdomId { get; }
    public string ReceiverKingdomId { get; }
    public string SpeakerHeroId { get; }
    public string AmountToken { get; }
    public string DurationToken { get; }
    public bool IsValid => !string.IsNullOrEmpty(PayerKingdomId)
        && !string.IsNullOrEmpty(ReceiverKingdomId)
        && !string.IsNullOrEmpty(SpeakerHeroId);

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public enum WorldDiplomacyMakePeaceExecutionStatus : byte
{
    Applied = 0,
    InvalidCommand = 1,
    NotMainThread = 2,
    PayerUnavailable = 3,
    ReceiverUnavailable = 4,
    PayerEliminated = 5,
    ReceiverEliminated = 6,
    SameKingdom = 7,
    PlayerUnauthorized = 8,
    SpeakerUnauthorized = 9,
    NotAtWar = 10,
    InvalidTerms = 11,
    RejectedBeforeStart = 12,
    UnknownAfterStart = 13,
    ActionNotApplied = 14
}

public readonly struct WorldDiplomacyMakePeaceExecutionReceipt
{
    public WorldDiplomacyMakePeaceExecutionReceipt(
        WorldDiplomacyMakePeaceExecutionStatus status,
        string payerKingdomId,
        string receiverKingdomId,
        int appliedDailyTribute,
        int appliedDurationDays,
        string errorCode)
    {
        Status = status;
        PayerKingdomId = payerKingdomId ?? "";
        ReceiverKingdomId = receiverKingdomId ?? "";
        AppliedDailyTribute = appliedDailyTribute;
        AppliedDurationDays = appliedDurationDays;
        ErrorCode = errorCode ?? "";
    }

    public WorldDiplomacyMakePeaceExecutionStatus Status { get; }
    public string PayerKingdomId { get; }
    public string ReceiverKingdomId { get; }
    public int AppliedDailyTribute { get; }
    public int AppliedDurationDays { get; }
    public string ErrorCode { get; }
    public bool IsApplied => Status == WorldDiplomacyMakePeaceExecutionStatus.Applied;
}

public interface IWorldDiplomacyMakePeaceGameActionPort
{
    WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command);
}

public readonly struct WorldDiplomacyOralMakePeaceResolution
{
    private WorldDiplomacyOralMakePeaceResolution(
        WorldDiplomacyOralMakePeaceResolutionStatus status,
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyMakePeaceCommand command)
    {
        Status = status;
        FirstKingdomId = firstKingdomId ?? "";
        SecondKingdomId = secondKingdomId ?? "";
        Command = command;
    }

    public WorldDiplomacyOralMakePeaceResolutionStatus Status { get; }
    public string FirstKingdomId { get; }
    public string SecondKingdomId { get; }
    public WorldDiplomacyMakePeaceCommand Command { get; }
    public bool IsReady => Status == WorldDiplomacyOralMakePeaceResolutionStatus.Ready
        && Command.IsValid;

    public static WorldDiplomacyOralMakePeaceResolution Rejected(
        WorldDiplomacyOralMakePeaceResolutionStatus status,
        string firstKingdomId = "",
        string secondKingdomId = "")
    {
        return new WorldDiplomacyOralMakePeaceResolution(
            status,
            firstKingdomId,
            secondKingdomId,
            default);
    }

    public static WorldDiplomacyOralMakePeaceResolution Ready(
        string firstKingdomId,
        string secondKingdomId,
        WorldDiplomacyMakePeaceCommand command)
    {
        return new WorldDiplomacyOralMakePeaceResolution(
            WorldDiplomacyOralMakePeaceResolutionStatus.Ready,
            firstKingdomId,
            secondKingdomId,
            command);
    }
}
