using Newtonsoft.Json;

namespace AnimusForge;

public sealed class WorldDiplomacyOfferCooldown
{
	[JsonProperty("proposerKingdomId")] public string ProposerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("domain")] public string Domain { get; set; } = "";
	[JsonProperty("lastFailedRoundDay")] public int LastFailedRoundDay { get; set; } = -1;
	[JsonProperty("sourceRoundId")] public string SourceRoundId { get; set; } = "";
}
