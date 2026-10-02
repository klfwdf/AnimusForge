using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyCanonicalHistoryState
{
	[JsonProperty("policyHistorySchemaVersion")]
	public int PolicyHistorySchemaVersion { get; set; }

	[JsonProperty("policyEventRevisions")]
	public Dictionary<string, long> PolicyEventRevisions { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("lastPolicyArtifactRevision")]
	public long LastPolicyArtifactRevision { get; set; }

	[JsonProperty("snapshot")]
	public WorldDiplomacyCanonicalHistorySnapshot Snapshot { get; set; } = new WorldDiplomacyCanonicalHistorySnapshot();

	[JsonProperty("deltaEntries")]
	public List<WorldDiplomacyCanonicalHistoryEntry> DeltaEntries { get; set; } = new List<WorldDiplomacyCanonicalHistoryEntry>();

	[JsonProperty("nextSequence")]
	public long NextSequence { get; set; } = 1L;

	[JsonProperty("revision")]
	public long Revision { get; set; }

	[JsonProperty("estimatedTokens")]
	public long EstimatedTokens { get; set; }

	[JsonProperty("worldWeeklySourceHashes")]
	public Dictionary<string, string> WorldWeeklySourceHashes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("worldWeeklySourceRevisions")]
	public Dictionary<string, long> WorldWeeklySourceRevisions { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("policyRevisionSignatures")]
	public Dictionary<string, string> PolicyRevisionSignatures { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("lastPolicyArtifactSequence")]
	public long LastPolicyArtifactSequence { get; set; }

	[JsonProperty("lastPolicyArtifactLedgerId")]
	public string LastPolicyArtifactLedgerId { get; set; } = "";
}

public sealed class WorldDiplomacyCanonicalHistorySnapshot
{
	[JsonProperty("content")]
	public string Content { get; set; } = "";

	[JsonProperty("coveredThroughSequence")]
	public long CoveredThroughSequence { get; set; }

	[JsonProperty("contentHash")]
	public string ContentHash { get; set; } = "";

	[JsonProperty("createdDay")]
	public int CreatedDay { get; set; } = -1;

	[JsonProperty("estimatedTokens")]
	public long EstimatedTokens { get; set; }

	[JsonProperty("preservedResultSourceIds")]
	public List<string> PreservedResultSourceIds { get; set; } = new List<string>();

	[JsonProperty("protectedFacts")]
	public List<WorldDiplomacyCanonicalProtectedFact> ProtectedFacts { get; set; } = new List<WorldDiplomacyCanonicalProtectedFact>();
}

public sealed class WorldDiplomacyCanonicalProtectedFact
{
	[JsonProperty("kind")]
	public string Kind { get; set; } = "";

	[JsonProperty("sourceKey")]
	public string SourceKey { get; set; } = "";

	[JsonProperty("sourceId")]
	public string SourceId { get; set; } = "";

	[JsonProperty("relatedSourceId")]
	public string RelatedSourceId { get; set; } = "";

	[JsonProperty("sequence")]
	public long Sequence { get; set; }

	[JsonProperty("day")]
	public int Day { get; set; }

	[JsonProperty("gameDate")]
	public string GameDate { get; set; } = "";

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomIds")]
	public List<string> TargetKingdomIds { get; set; } = new List<string>();

	[JsonProperty("intent")]
	public string Intent { get; set; } = "";

	[JsonProperty("commitment")]
	public string Commitment { get; set; } = "";

	[JsonProperty("text")]
	public string Text { get; set; } = "";
}

public sealed class WorldDiplomacyCanonicalHistoryEntry
{
	[JsonProperty("entryId")]
	public string EntryId { get; set; } = "";

	[JsonProperty("sourceKey")]
	public string SourceKey { get; set; } = "";

	[JsonProperty("sequence")]
	public long Sequence { get; set; }

	[JsonProperty("day")]
	public int Day { get; set; }

	[JsonProperty("gameDate")]
	public string GameDate { get; set; } = "";

	[JsonProperty("kind")]
	public string Kind { get; set; } = "";

	[JsonProperty("sourceId")]
	public string SourceId { get; set; } = "";

	[JsonProperty("respondingToOfferDocumentId")]
	public string RespondingToOfferDocumentId { get; set; } = "";

	[JsonProperty("respondingToThreatDocumentId")]
	public string RespondingToThreatDocumentId { get; set; } = "";

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomIds")]
	public List<string> TargetKingdomIds { get; set; } = new List<string>();

	[JsonProperty("intent")]
	public string Intent { get; set; } = "";

	[JsonProperty("commitment")]
	public string Commitment { get; set; } = "";

	[JsonProperty("actionFacts", NullValueHandling = NullValueHandling.Ignore)]
	public List<string> ActionFacts { get; set; }

	[JsonProperty("text")]
	public string Text { get; set; } = "";

	[JsonProperty("verified")]
	public bool Verified { get; set; }

	[JsonProperty("estimatedTokens")]
	public long EstimatedTokens { get; set; }
}
