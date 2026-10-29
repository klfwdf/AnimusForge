using Newtonsoft.Json;
namespace AnimusForge;
public sealed class WorldDiplomacyPlayerResponse
{
    [JsonProperty("requestKind")] public string RequestKind { get; set; } = "case_question";
    [JsonProperty("requestQuote")] public string RequestQuote { get; set; } = "";
    [JsonProperty("participationDecision")] public string ParticipationDecision { get; set; } = "";
    [JsonProperty("decisionReason")] public string DecisionReason { get; set; } = "";
    [JsonProperty("decisionPosition")] public WorldDiplomacyRoundParticipant DecisionPosition { get; set; }
    [JsonProperty("answerDelivered")] public bool AnswerDelivered { get; set; }
    [JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
    [JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
    [JsonProperty("originalRoundId")] public string OriginalRoundId { get; set; } = "";
    [JsonProperty("answerDocumentId")] public string AnswerDocumentId { get; set; } = "";
    [JsonProperty("status")] public string Status { get; set; } = "pending";
    [JsonProperty("retryNotBeforeDay")] public int RetryNotBeforeDay { get; set; } = -1;
    [JsonProperty("failureReason")] public string FailureReason { get; set; } = "";
    [JsonProperty("createdDay")] public int CreatedDay { get; set; }
}

public sealed class DiplomacyResponseRequest
{
    public string KingdomId { get; set; } = "";
    public string Kind { get; set; } = "case_question";
    public string Quote { get; set; } = "";
}
