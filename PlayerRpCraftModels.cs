using System.Collections.Generic;

namespace AnimusForge;

internal sealed class PlayerRpCrafterOption
{
	public string HeroId;

	public string DisplayName;

	public int SmithingSkill;

	public int CraftingStamina;

	public int MaxCraftingStamina;

	public bool HasCraftingStamina;

	public int CraftingStaminaCost;

	public bool HasEnoughCraftingStamina;
}

internal sealed class PlayerRpCraftPreview
{
	public long RuntimeGeneration;

	public string CrafterHeroId;

	public string CrafterDisplayName;

	public int CrafterCraftingStamina;

	public int CrafterMaxCraftingStamina;

	public int CraftingStaminaCost;

	public string RequestedName;

	public int InvestedDenars;

	public int CraftedItemValue;

	public int CraftingExperienceBaseAmount;

	public bool IsEquipment;

	public string TemplateStringId;

	public string TemplateDisplayName;

	public int TemplateBaseValue;

	public string TemplateTypeLabel;

	public int TemplateCandidateCount;

	public int SmithingSkill;

	public int GoodWeight;

	public int NormalWeight;

	public int BadWeight;

	public string ConfirmationText;

	public string TemplateSelectionSource;
}

internal sealed class PlayerRpCraftTemplateCandidate
{
	public int Rank;

	public string TemplateStringId;

	public string DisplayName;

	public string TypeLabel;

	public int StandardPrice;

	public float MatchScore;

	public int Suitability;

	public float TieBreaker;
}

internal sealed class PlayerRpCraftTemplateSelectionRequest
{
	public string CrafterHeroId;

	public string RequestedName;

	public int InvestedDenars;

	public bool IsEquipment;

	public string Prompt;

	public string ApiUrl;

	public string ApiKey;

	public string ModelName;

	public string RequestJson;

	public string PlainFallbackRequestJson;

	public string NoTemperatureFallbackRequestJson;

	public string HighTokenFallbackRequestJson;

	public string ReasoningFallbackRequestJson;

	public string ControlMode;

	public List<PlayerRpCraftTemplateCandidate> Candidates = new List<PlayerRpCraftTemplateCandidate>();
}

internal sealed class PlayerRpCraftTemplateSelectionResult
{
	public bool Success;

	public string TemplateStringId;

	public int CandidateRank;

	public string Error;
}

internal sealed class PlayerRpCraftResult
{
	public string GeneratedStringId;

	public string DisplayName;

	public string Outcome;

	public string Message;
}

internal sealed class PlayerRpCraftData
{
	public const int CurrentSchemaVersion = 3;

	public const int CurrentFormulaVersion = 4;

	public int SchemaVersion = CurrentSchemaVersion;

	public int FormulaVersion = CurrentFormulaVersion;

	public string BatchId;

	public string CreatorHeroId;

	public string CrafterHeroId;

	public string CrafterDisplayNameSnapshot;

	public int CrafterCraftingStaminaSnapshot;

	public int CrafterMaxCraftingStaminaSnapshot;

	public int CraftingStaminaCost;

	public int CrafterCraftingStaminaAfterSnapshot;

	public int CraftingExperienceBaseAmount;

	public string OriginalRequestedName;

	public string OriginalTemplateStringId;

	public string EffectiveTemplateStringId;

	public string CraftKind;

	public int InvestedDenars;

	public int CraftedItemValue;

	public int TemplateBaseValue;

	public int PlayerIntelligenceSnapshot;

	public int SmithingSkillSnapshot;

	public int GoodWeight;

	public int NormalWeight;

	public int BadWeight;

	public int Roll;

	public string Outcome;

	public bool Underfunded;

	public double AppliedMultiplier = 1d;

	public int UpgradeLevel;

	public int AppliedBonus;

	public int CreatedDay;

	public int InitialQuantity = 1;

	public PlayerRpCraftItemStatsSnapshot StatsSnapshot;

	public Dictionary<string, PlayerRpCraftInspectionRecord> Inspections =
		new Dictionary<string, PlayerRpCraftInspectionRecord>(System.StringComparer.OrdinalIgnoreCase);
}

internal sealed class PlayerRpCraftInspectionRecord
{
	public string ObserverKey;

	public string ExposureType;

	public int PlayerIntelligence;

	public int ObserverIntelligence;

	public int ChanceWeight;

	public int Roll;

	public bool Detected;

	public int Day;
}
