using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyRelayArrival
{
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("fromKingdomId")] public string FromKingdomId { get; set; } = "";
	[JsonProperty("toKingdomId")] public string ToKingdomId { get; set; } = "";
	[JsonProperty("resultSettlementSlotId")] public string ResultSettlementSlotId { get; set; } = "";
	[JsonProperty("dueDay")] public int DueDay { get; set; }
	[JsonProperty("sequence")] public int Sequence { get; set; }
}

public sealed class WorldDiplomacyPlayerOpportunity
{
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("arrivedDay")] public int ArrivedDay { get; set; }
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("knownDocumentIds")] public List<string> KnownDocumentIds { get; set; } = new List<string>();
}

public sealed class WorldDiplomacyPropagationArrival
{
	[JsonProperty("documentId")] public string DocumentId { get; set; } = "";
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("settlementId")] public string SettlementId { get; set; } = "";
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("scope")] public string Scope { get; set; } = "civilian";
	[JsonProperty("dueDay")] public int DueDay { get; set; }
}

public sealed class WorldDiplomacySettlementKnowledge
{
	[JsonProperty("settlementId")] public string SettlementId { get; set; } = "";
	[JsonProperty("documentIds")] public List<string> DocumentIds { get; set; } = new List<string>();
	[JsonProperty("lastUpdatedDay")] public int LastUpdatedDay { get; set; }
}

public sealed class WorldDiplomacyKingdomKnowledge
{
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("documentIds")] public List<string> DocumentIds { get; set; } = new List<string>();
	[JsonProperty("lastUpdatedDay")] public int LastUpdatedDay { get; set; }
}

public sealed class WorldDiplomacyParticipationRequest
{
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("dueDay")] public int DueDay { get; set; }
	[JsonProperty("triggerDocumentIds")] public List<string> TriggerDocumentIds { get; set; } = new List<string>();
}

public sealed class WorldDiplomacyPendingSpeech
{
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("authorKingdomId")] public string AuthorKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
	[JsonProperty("queuedDay")] public int QueuedDay { get; set; }
	[JsonProperty("priority")] public int Priority { get; set; }
}
