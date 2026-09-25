using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyRoundSummary
{
	[JsonProperty("archiveSchemaVersion")] public int ArchiveSchemaVersion { get; set; }
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("summary")] public string Summary { get; set; } = "";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
	[JsonProperty("sourceDocumentIds")] public List<string> SourceDocumentIds { get; set; } = new List<string>();
	[JsonProperty("facts")] public List<WorldDiplomacyRoundFact> Facts { get; set; } = new List<WorldDiplomacyRoundFact>();
	[JsonProperty("kingdomIds")] public List<string> KingdomIds { get; set; } = new List<string>();
	[JsonProperty("isTokenCompressed")] public bool IsTokenCompressed { get; set; }
	[JsonProperty("compressionBatchId")] public string CompressionBatchId { get; set; } = "";
}

public sealed class WorldDiplomacyRoundFact
{
	[JsonProperty("kind")] public string Kind { get; set; } = "declaration";
	[JsonProperty("text")] public string Text { get; set; } = "";
	[JsonProperty("sourceDocumentIds")] public List<string> SourceDocumentIds { get; set; } = new List<string>();
	[JsonProperty("kingdomIds")] public List<string> KingdomIds { get; set; } = new List<string>();
}

public sealed class WorldDiplomacyPolicySignal
{
	[JsonProperty("signalKey")] public string SignalKey { get; set; } = "";
	[JsonProperty("policyId")] public string PolicyId { get; set; } = "";
	[JsonProperty("policyKind")] public string PolicyKind { get; set; } = "kingdom";
	[JsonProperty("policyName")] public string PolicyName { get; set; } = "";
	[JsonProperty("policySummary")] public string PolicySummary { get; set; } = "";
	[JsonProperty("issuerKingdomId")] public string IssuerKingdomId { get; set; } = "";
	[JsonProperty("issuerKingdomName")] public string IssuerKingdomName { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomName")] public string TargetKingdomName { get; set; } = "";
	[JsonProperty("directEffect")] public string DirectEffect { get; set; } = "";
	[JsonProperty("publishedDay")] public int PublishedDay { get; set; }
}

public sealed class WorldDiplomacyCompressionSummary
{
	[JsonProperty("batchId")] public string BatchId { get; set; } = "";
	[JsonProperty("summary")] public string Summary { get; set; } = "";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
	[JsonProperty("startDay")] public int StartDay { get; set; }
	[JsonProperty("endDay")] public int EndDay { get; set; }
	[JsonProperty("tokenCount")] public long TokenCount { get; set; }
	[JsonProperty("sourceRoundIds")] public List<string> SourceRoundIds { get; set; } = new List<string>();
	[JsonProperty("kingdomIds")] public List<string> KingdomIds { get; set; } = new List<string>();
	[JsonProperty("confirmedResults")] public List<string> ConfirmedResults { get; set; } = new List<string>();
}
