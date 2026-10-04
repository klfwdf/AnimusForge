namespace AnimusForge;

// The transport lease reads only these detached job identity fields.
internal sealed class WorldDiplomacyJob
{
    public string JobId { get; set; }
    public string Kind { get; set; }
    public string RoundId { get; set; }
    public string ExchangeId { get; set; }
    public int RequestAttempt { get; set; }
    public long RoundConversationRevision { get; set; }
}
