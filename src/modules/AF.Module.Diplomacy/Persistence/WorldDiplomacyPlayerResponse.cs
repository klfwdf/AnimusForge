using Newtonsoft.Json;
namespace AnimusForge;
public sealed class WorldDiplomacyPlayerResponse
{
    [JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
    [JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
    [JsonProperty("originalRoundId")] public string OriginalRoundId { get; set; } = "";
    [JsonProperty("answerDocumentId")] public string AnswerDocumentId { get; set; } = "";
    [JsonProperty("status")] public string Status { get; set; } = "pending";
    [JsonProperty("retryNotBeforeDay")] public int RetryNotBeforeDay { get; set; } = -1;
    [JsonProperty("failureReason")] public string FailureReason { get; set; } = "";
    [JsonProperty("createdDay")] public int CreatedDay { get; set; }
}
