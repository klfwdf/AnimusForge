using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyPeaceTerms
{
	[JsonProperty("tributePayerKingdomId")]
	public string TributePayerKingdomId { get; set; } = "";

	[JsonProperty("tributeReceiverKingdomId")]
	public string TributeReceiverKingdomId { get; set; } = "";

	[JsonProperty("dailyTribute")]
	public int DailyTribute { get; set; }

	[JsonProperty("durationDays")]
	public int DurationDays { get; set; }

	[JsonProperty("cessionFromKingdomId")]
	public string CessionFromKingdomId { get; set; } = "";

	[JsonProperty("cessionToKingdomId")]
	public string CessionToKingdomId { get; set; } = "";

	[JsonProperty("cessionSettlementId")]
	public string CessionSettlementId { get; set; } = "";
}

public sealed class WorldDiplomacyWarLedger
{
	[JsonProperty("pairKey")]
	public string PairKey { get; set; } = "";

	[JsonProperty("firstKingdomId")]
	public string FirstKingdomId { get; set; } = "";

	[JsonProperty("secondKingdomId")]
	public string SecondKingdomId { get; set; } = "";

	[JsonProperty("startedDay")]
	public int StartedDay { get; set; }

	[JsonProperty("settlementChanges")]
	public List<WorldDiplomacySettlementChange> SettlementChanges { get; set; } = new List<WorldDiplomacySettlementChange>();

	[JsonProperty("firstLastForcedPeaceProposalDay")]
	public int FirstLastForcedPeaceProposalDay { get; set; }

	[JsonProperty("secondLastForcedPeaceProposalDay")]
	public int SecondLastForcedPeaceProposalDay { get; set; }
}

public sealed class WorldDiplomacySettlementChange
{
	[JsonProperty("settlementId")]
	public string SettlementId { get; set; } = "";

	[JsonProperty("settlementName")]
	public string SettlementName { get; set; } = "";

	[JsonProperty("originalKingdomId")]
	public string OriginalKingdomId { get; set; } = "";

	[JsonProperty("currentKingdomId")]
	public string CurrentKingdomId { get; set; } = "";

	[JsonProperty("lastChangedDay")]
	public int LastChangedDay { get; set; }

	[JsonProperty("captureCount")]
	public int CaptureCount { get; set; }
}

public sealed class WarPressureEntry
{
	[JsonProperty("lastIntent")]
	public string LastIntent { get; set; } = "";

	[JsonProperty("consecutiveSimilarCount")]
	public int ConsecutiveSimilarCount { get; set; }

	[JsonProperty("isEscalationArmed")]
	public bool IsEscalationArmed { get; set; }

	[JsonProperty("armedDay")]
	public int ArmedDay { get; set; }

	[JsonProperty("needsFreshEscalation")]
	public bool NeedsFreshEscalation { get; set; }

	[JsonProperty("sourceKingdomId")]
	public string SourceKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("value")]
	public int Value { get; set; }

	[JsonProperty("lastUpdatedDay")]
	public int LastUpdatedDay { get; set; }

	[JsonProperty("lastReason")]
	public string LastReason { get; set; } = "";

	[JsonProperty("lastBlockReason")]
	public string LastBlockReason { get; set; } = "";
}
