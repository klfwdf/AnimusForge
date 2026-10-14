using Newtonsoft.Json;
using System.Collections.Generic;

namespace AnimusForge;

// Stored inside the existing chunked diplomacy save, never a second treaty store.
public sealed class WorldDiplomacyPlayerWait
{
    [JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
    [JsonProperty("notifiedHour")] public double NotifiedHour { get; set; } = -1;
    [JsonProperty("answerDocumentId")] public string AnswerDocumentId { get; set; } = "";
    [JsonProperty("closureDocumentId")] public string ClosureDocumentId { get; set; } = "";
    [JsonProperty("requiresAnswer")] public bool RequiresAnswer { get; set; } = true;
}

public sealed class WorldDiplomacyPlayerCourtWork
{
    [JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
    [JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
    [JsonProperty("purpose")] public string Purpose { get; set; } = "";
    [JsonProperty("status")] public string Status { get; set; } = "pending";
}

public sealed class WorldDiplomacyPlayerAgendaCandidate
{
    [JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
    [JsonProperty("playerKingdomId")] public string PlayerKingdomId { get; set; } = "";
    [JsonProperty("lastObservedDay")] public int LastObservedDay { get; set; }
    [JsonProperty("lastEvaluatedDay")] public int LastEvaluatedDay { get; set; } = -1;
    [JsonProperty("sourceKeys")] public List<string> SourceKeys { get; set; } = new();
}

public sealed class WorldDiplomacyPlayerDisputeRefusal
{
    [JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
    [JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
    [JsonProperty("roundId")] public string RoundId { get; set; } = "";
    [JsonProperty("topic")] public string Topic { get; set; } = "";
    [JsonProperty("publicScope")] public string PublicScope { get; set; } = "";
    [JsonProperty("untilDay")] public int UntilDay { get; set; }
}
