using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyTopicUse
{
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("initiatorKingdomId")] public string InitiatorKingdomId { get; set; } = "";
	[JsonProperty("fingerprint")] public string Fingerprint { get; set; } = "";
	[JsonProperty("category")] public string Category { get; set; } = "";
	[JsonProperty("motif")] public string Motif { get; set; } = "";
	[JsonProperty("pairKey")] public string PairKey { get; set; } = "";
	[JsonProperty("day")] public int Day { get; set; }
}

public sealed class WorldDiplomacyRealmRelationProfile
{
	public float AverageRelation { get; set; }
	public float PositiveRatio { get; set; }
	public float HostileRatio { get; set; }
	public float Polarization { get; set; }
	public int RulerRelation { get; set; }
	public float RulerEliteGap { get; set; }
	public int SamplePairCount { get; set; }
}

public sealed class WorldDiplomacyBattleFact
{
	[JsonProperty("battleId")]
	public string BattleId { get; set; } = "";

	[JsonProperty("day")]
	public int Day { get; set; }

	[JsonProperty("gameDate")]
	public string GameDate { get; set; } = "";

	[JsonProperty("battleType")]
	public string BattleType { get; set; } = "";

	[JsonProperty("location")]
	public string Location { get; set; } = "";

	[JsonProperty("attackerKingdomIds")]
	public List<string> AttackerKingdomIds { get; set; } = new List<string>();

	[JsonProperty("defenderKingdomIds")]
	public List<string> DefenderKingdomIds { get; set; } = new List<string>();

	[JsonProperty("attackerLeaderNames")]
	public List<string> AttackerLeaderNames { get; set; } = new List<string>();

	[JsonProperty("defenderLeaderNames")]
	public List<string> DefenderLeaderNames { get; set; } = new List<string>();

	[JsonProperty("winnerSide")]
	public string WinnerSide { get; set; } = "";

	[JsonProperty("isPlayerInvolved")]
	public bool IsPlayerInvolved { get; set; }
}

public sealed class WorldDiplomacyStandingChange
{
	[JsonProperty("kind")] public string Kind { get; set; } = "";
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("kingdomName")] public string KingdomName { get; set; } = "";
	[JsonProperty("before")] public int Before { get; set; }
	[JsonProperty("after")] public int After { get; set; }
	[JsonProperty("delta")] public int Delta { get; set; }
	[JsonProperty("reason")] public string Reason { get; set; } = "";
}

public sealed class WorldDiplomacyPrestigeRelationModifier
{
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("rulerHeroId")] public string RulerHeroId { get; set; } = "";
	[JsonProperty("vassalLeaderHeroId")] public string VassalLeaderHeroId { get; set; } = "";
	[JsonProperty("appliedAmount")] public int AppliedAmount { get; set; }
	[JsonProperty("pendingEffect", NullValueHandling = NullValueHandling.Ignore)]
	public WorldDiplomacyPendingRelationEffect PendingEffect { get; set; }
}

// Optional recovery evidence. Absent in old saves and omitted after confirmation.
public sealed class WorldDiplomacyPendingRelationEffect
{
	[JsonProperty("before")] public int Before { get; set; }
	[JsonProperty("expectedAfter")] public int ExpectedAfter { get; set; }
}

public sealed class NativeDiplomacySignal
{
	[JsonProperty("signalId")]
	public string SignalId { get; set; } = "";

	[JsonProperty("sourceKingdomId")]
	public string SourceKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("action")]
	public string Action { get; set; } = "";

	[JsonProperty("reason")]
	public string Reason { get; set; } = "";

	[JsonProperty("day")]
	public int Day { get; set; }

	[JsonProperty("value")]
	public int Value { get; set; }
}

public sealed class WorldDiplomacyAnnualSummary
{
	[JsonProperty("year")]
	public int Year { get; set; }

	[JsonProperty("summary")]
	public string Summary { get; set; } = "";

	[JsonProperty("majorEvents")]
	public List<string> MajorEvents { get; set; } = new List<string>();

	[JsonProperty("createdDay")]
	public int CreatedDay { get; set; }
}
