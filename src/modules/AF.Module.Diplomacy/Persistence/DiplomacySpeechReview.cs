using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

// Temporary evidence for unfinished commitment extraction, not personal history.
public sealed class DiplomacySpeechReview
{
    [JsonProperty("id")] public string Id { get; set; } = "";
    [JsonProperty("sourceKey")] public string SourceKey { get; set; } = "";
    [JsonProperty("actor")] public string Actor { get; set; } = "";
    [JsonProperty("ruler")] public string Ruler { get; set; } = "";
    [JsonProperty("playerKingdom")] public string PlayerKingdom { get; set; } = "";
    [JsonProperty("playerRuler")] public string PlayerRuler { get; set; } = "";
    [JsonProperty("appliedPayloadKeys")] public List<string> AppliedPayloadKeys { get; set; } = new();
    [JsonProperty("channel")] public string Channel { get; set; } = "";
    [JsonProperty("interaction")] public string Interaction { get; set; } = "";
    [JsonProperty("session")] public string Session { get; set; } = "";
    [JsonProperty("playerText")] public string PlayerText { get; set; } = "";
    [JsonProperty("npcText")] public string NpcText { get; set; } = "";
    [JsonProperty("tagHints")] public string TagHints { get; set; } = "";
    [JsonProperty("status")] public string Status { get; set; } = "pending";
    [JsonProperty("reason")] public string Reason { get; set; } = "";
    [JsonProperty("extractionContractVersion")] public int ExtractionContractVersion { get; set; }
    [JsonProperty("attempts")] public int Attempts { get; set; }
    [JsonProperty("retryDay")] public int RetryDay { get; set; }
    [JsonProperty("applied")] public int Applied { get; set; }
    [JsonProperty("watchedKingdoms")] public List<string> WatchedKingdoms { get; set; } = new();
    [JsonProperty("worldFingerprint")] public string WorldFingerprint { get; set; } = "";
    [JsonProperty("reviewWorldFingerprint")] public string ReviewWorldFingerprint { get; set; } = "";
    [JsonProperty("configurationFingerprint")] public string ConfigurationFingerprint { get; set; } = "";
    [JsonProperty("recoverOnService")] public bool RecoverOnService { get; set; }
    [JsonProperty("serviceRecoveryEpoch")] public long ServiceRecoveryEpoch { get; set; }
    [JsonProperty("approved")] public string Approved { get; set; } = "";
}
