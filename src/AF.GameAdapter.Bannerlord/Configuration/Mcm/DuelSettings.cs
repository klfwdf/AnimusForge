using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyEffects;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings : AttributeGlobalSettings<DuelSettings>
{
	private static DuelSettings _fallbackSettings;

	private static bool _settingsFallbackWarned;

	private static bool _logCleanupDefaultMigrationChecked;

	private static bool _duelRenownDefaultMigrationChecked;

	private static bool _worldDiplomacyCompressionDefaultsMigrationChecked;
	private static bool _adultHeroPersonaDefaultMigrationChecked;

	private static int _playerPolicyEffectModuleDetailCountClampNoticeConsumed;

	private const string LogCleanupDefaultMigrationId = "v0.8.9-force-log-cleanup-3-days";

	private const string LogCleanupDefaultMigrationMarkerFileName = ".log_cleanup_3days_migration_v089";

	private const string WorldDiplomacyCompressionDefaultsMigrationId = "v0.8.9-world-diplomacy-compression-800k-48k";

	private const string WorldDiplomacyCompressionDefaultsMigrationMarkerFileName = ".world_diplomacy_compression_800k_48k_migration_v089";

	private const int LegacyWorldDiplomacyHistoryCompressionTargetThousands = 16;

	private const int LegacyDuelLoserClanRenownPenaltyMinimum = 100;

	private const int LegacyDuelLoserClanRenownPenaltyPercent = 15;

	private const int LegacyDuelLoserClanRenownPenaltyMaximum = 1000;

	private const int DefaultDuelLoserClanRenownPenaltyMinimum = 20;

	private const int DefaultDuelLoserClanRenownPenaltyPercent = 3;

	private const int DefaultDuelLoserClanRenownPenaltyMaximum = 150;

	private const int FeatureDiagnosticLogMinSizeMegabytes = 1;

	private const int FeatureDiagnosticLogMaxSizeMegabytes = 128;

	private const int FeatureDiagnosticLogDefaultSizeMegabytes = 16;

	private static string SourceDefaultPlayerCustomPromptRule => McmPromptDefaults.Raw("DefaultPlayerCustomPromptRule");
	private static string DefaultPlayerCustomPromptRule => McmPromptDefaults.Get("DefaultPlayerCustomPromptRule");

	private const string PlayerCustomPromptRuleFileName = "PlayerCustomPromptRule.txt";



	private const string KingdomRebellionSystemPromptFileName = "KingdomRebellionSystemPrompt.txt";



	private const string WeeklyReportWritingRequirementsFileName = "WeeklyReportWritingRequirements.txt";

	private const string DefaultNpcPersonaGenerationRequirements = "";







	private static readonly string PreviousDefaultWorldDiplomacyPrompt = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyPrompt));

	private static readonly string PreviousDefaultWorldDiplomacyPromptV1 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyPromptV1));

	private static readonly string PreviousDefaultWorldDiplomacyPreferenceV3 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyPreferenceV3));

	private static readonly string PreviousDefaultWorldDiplomacyPreferenceV4 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyPreferenceV4));

	private static readonly string PreviousWorldDiplomacyContractHeaderV2 = LegacySettingsPrompts.Get(nameof(PreviousWorldDiplomacyContractHeaderV2));

	private const int PreviousDefaultWorldDiplomacyPromptV2Length = 2111;

	private const string PreviousDefaultWorldDiplomacyPromptV2Sha256Base64 = "NJ8Pw8CFFw7UTIgR0eZzf+pPQ5W0vFYY6MGJwc1eHTo=";

	private static readonly string WorldDiplomacyPreferenceSectionMarker = McmPromptDefaults.Raw("WorldDiplomacyPreferenceSectionMarker");

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV19 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV19));

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV20 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV20));

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV21 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV21));

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV22 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV22));

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV23 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV23));

	private static readonly string PreviousDefaultWorldDiplomacyContractCoreV24 = LegacySettingsPrompts.Get(nameof(PreviousDefaultWorldDiplomacyContractCoreV24));

	private static string SourceDefaultWorldDiplomacyContractCore => McmPromptDefaults.Raw("DefaultWorldDiplomacyContractCore");
	private static string DefaultWorldDiplomacyContractCore => McmPromptDefaults.Get("DefaultWorldDiplomacyContractCore");

	private static string SourceDefaultWorldDiplomacyPreferenceGuard => McmPromptDefaults.Raw("DefaultWorldDiplomacyPreferenceGuard");
	private static string DefaultWorldDiplomacyPreferenceGuard => McmPromptDefaults.Get("DefaultWorldDiplomacyPreferenceGuard");

	private static string SourceDefaultWorldDiplomacyPreference => McmPromptDefaults.Raw("DefaultWorldDiplomacyPreference");
	private static string DefaultWorldDiplomacyPreference => McmPromptDefaults.Get("DefaultWorldDiplomacyPreference");

	private static string DefaultCustomPolicyGoldCostPromptParagraph => McmPromptDefaults.Raw("DefaultCustomPolicyGoldCostPromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyDualCostPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyDualCostPromptParagraph));

	private static readonly string PreviousDefaultPolicyTownTaxIntentPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultPolicyTownTaxIntentPromptParagraph));

	private static readonly string PreviousDefaultPolicyTownTaxIntentPromptParagraphBeforePlayerTargetRouting = LegacySettingsPrompts.Get(nameof(PreviousDefaultPolicyTownTaxIntentPromptParagraphBeforePlayerTargetRouting));

	private static string DefaultPlayerPolicyTownTaxIntentPromptParagraph => McmPromptDefaults.Raw("DefaultPlayerPolicyTownTaxIntentPromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyTownTaxScalePromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyTownTaxScalePromptParagraph));

	private static readonly string PreviousDefaultCustomPolicyEffectOverviewPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEffectOverviewPromptParagraph));

	private static string DefaultCustomPolicyEffectOverviewPromptParagraph => McmPromptDefaults.Raw("DefaultCustomPolicyEffectOverviewPromptParagraph");

	private static readonly string PreviousDefaultPolicyConstructionSpeedPromptParagraphBeforePlayerAmountScale = LegacySettingsPrompts.Get(nameof(PreviousDefaultPolicyConstructionSpeedPromptParagraphBeforePlayerAmountScale));

	private static string DefaultPlayerPolicyConstructionSpeedPromptParagraph => McmPromptDefaults.Raw("DefaultPlayerPolicyConstructionSpeedPromptParagraph");

	private static readonly string PreviousDefaultPolicyLoyaltyPromptParagraphV2 = LegacySettingsPrompts.Get(nameof(PreviousDefaultPolicyLoyaltyPromptParagraphV2));

	private static string DefaultPolicyLoyaltyPromptParagraph => McmPromptDefaults.Raw("DefaultPolicyLoyaltyPromptParagraph");

	private static string DefaultPolicyStabilityOneTimePromptParagraph => McmPromptDefaults.Raw("DefaultPolicyStabilityOneTimePromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptWithVisibleBuiltIns = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptWithVisibleBuiltIns));

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptWithSeparatedBuiltIns = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptWithSeparatedBuiltIns));

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeTaxIntent = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptBeforeTaxIntent));

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeConstructionSpeed = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptBeforeConstructionSpeed));

	private static readonly string PreviousDefaultCustomPolicyTimingPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyTimingPromptParagraph));

	private static readonly string PreviousDefaultCustomPolicyTimingPromptParagraphV2 = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyTimingPromptParagraphV2));

	private static string DefaultCustomPolicyTimingPromptParagraph => McmPromptDefaults.Raw("DefaultCustomPolicyTimingPromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyLoyaltyPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyLoyaltyPromptParagraph));

	private static readonly string PreviousDefaultCustomPolicyStabilityPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyStabilityPromptParagraph));

	private static readonly string PreviousDefaultCustomPolicyExplicitScalePromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyExplicitScalePromptParagraph));

	private static readonly string PreviousDefaultCustomPolicyExplicitScalePromptParagraphV2 = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyExplicitScalePromptParagraphV2));

	private static string DefaultCustomPolicyExplicitScalePromptParagraph => McmPromptDefaults.Raw("DefaultCustomPolicyExplicitScalePromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeSparseEffects = PreviousDefaultCustomPolicyEvaluatorPromptBeforeTaxIntent
		.Replace(PreviousDefaultCustomPolicyEffectOverviewPromptParagraph, DefaultCustomPolicyEffectOverviewPromptParagraph)
		.Replace(PreviousDefaultCustomPolicyTimingPromptParagraph, PreviousDefaultCustomPolicyTimingPromptParagraphV2)
		.Replace(PreviousDefaultCustomPolicyLoyaltyPromptParagraph, PreviousDefaultPolicyLoyaltyPromptParagraphV2)
		.Replace(PreviousDefaultCustomPolicyStabilityPromptParagraph, DefaultPolicyStabilityOneTimePromptParagraph)
		.Replace(PreviousDefaultCustomPolicyTownTaxScalePromptParagraph, PreviousDefaultPolicyTownTaxIntentPromptParagraphBeforePlayerTargetRouting + "\n\n" + PreviousDefaultPolicyConstructionSpeedPromptParagraphBeforePlayerAmountScale)
		.Replace(PreviousDefaultCustomPolicyExplicitScalePromptParagraph, PreviousDefaultCustomPolicyExplicitScalePromptParagraphV2);

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeVassalIndependence = PreviousDefaultCustomPolicyEvaluatorPromptBeforeSparseEffects
		.Replace("\r\n", "\n")
		.Replace('\r', '\n')
		.Replace(PreviousDefaultPolicyTownTaxIntentPromptParagraphBeforePlayerTargetRouting, DefaultPlayerPolicyTownTaxIntentPromptParagraph)
		.Replace(PreviousDefaultPolicyConstructionSpeedPromptParagraphBeforePlayerAmountScale, DefaultPlayerPolicyConstructionSpeedPromptParagraph)
		.Replace(PreviousDefaultCustomPolicyTimingPromptParagraphV2, DefaultCustomPolicyTimingPromptParagraph)
		.Replace(PreviousDefaultPolicyLoyaltyPromptParagraphV2, DefaultPolicyLoyaltyPromptParagraph)
		.Replace(PreviousDefaultCustomPolicyExplicitScalePromptParagraphV2, DefaultCustomPolicyExplicitScalePromptParagraph);

	private static string DefaultVassalPolicyIndependencePromptParagraph => McmPromptDefaults.Raw("DefaultVassalPolicyIndependencePromptParagraph");

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeEffectModuleRouting = PreviousDefaultCustomPolicyEvaluatorPromptBeforeVassalIndependence
		+ "\n\n" + DefaultVassalPolicyIndependencePromptParagraph;

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer = PreviousDefaultCustomPolicyEvaluatorPromptWithSeparatedBuiltIns
		+ McmPromptDefaults.Raw("PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer_HighestPrinciple")
		+ McmPromptDefaults.Raw("PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer_Evaluation")
		+ McmPromptDefaults.Raw("PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer_Denar")
		+ "\n\n" + DefaultCustomPolicyGoldCostPromptParagraph
		+ McmPromptDefaults.Raw("PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer_Duration")
		+ McmPromptDefaults.Raw("PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer_PublicFeedback")
		+ "\n\n" + DefaultVassalPolicyIndependencePromptParagraph;

	private static string SourceDefaultCustomPolicyEvaluatorPrompt => McmPromptDefaults.Raw("DefaultCustomPolicyEvaluatorPrompt");
	private static string DefaultCustomPolicyEvaluatorPrompt => McmPromptDefaults.Get("DefaultCustomPolicyEvaluatorPrompt");

	private static readonly string LeakedCustomPolicyPoliticalWeightsPromptSuffix = LegacySettingsPrompts.Get(nameof(LeakedCustomPolicyPoliticalWeightsPromptSuffix));

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptBeforeExpandedStats = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptBeforeExpandedStats));

	private static readonly string PreviousDefaultCustomPolicyEvaluatorPromptForMigration = LegacySettingsPrompts.Get(nameof(PreviousDefaultCustomPolicyEvaluatorPromptForMigration));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptForMigration = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptForMigration));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithTechnicalContract = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithTechnicalContract));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeDerivedEvent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeDerivedEvent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithTypedEvent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithTypedEvent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithFreeformDerivedEvent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithFreeformDerivedEvent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithConciseAssociatedEvent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithConciseAssociatedEvent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithCreativeDerivedEvent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithCreativeDerivedEvent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeCreativePremise = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeCreativePremise));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptWithCreativePremise = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptWithCreativePremise));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeConsequentialEvents = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeConsequentialEvents));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeFocusedPolicyAndEvents = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeFocusedPolicyAndEvents));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeKnowledgeGroundedContextRefactor = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeKnowledgeGroundedContextRefactor));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeShortEditablePrompt = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeShortEditablePrompt));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeCompactContext = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeCompactContext));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeStrongerEffectsAndPlainLanguage = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeStrongerEffectsAndPlainLanguage));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeMinimumOneEffects = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeMinimumOneEffects));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeConceptOnlyPrompt = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeConceptOnlyPrompt));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeTownTax = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeTownTax));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeTaxIntent = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeTaxIntent));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeConstructionSpeed = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyPromptBeforeConstructionSpeed));

	private static readonly string PreviousDefaultNpcRulerPolicyNumericPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyNumericPromptParagraph));

	private static string DefaultNpcRulerPolicyNumericPromptParagraph => McmPromptDefaults.Raw("DefaultNpcRulerPolicyNumericPromptParagraph");

	private static readonly string PreviousDefaultNpcRulerPolicyStabilityPromptParagraph = LegacySettingsPrompts.Get(nameof(PreviousDefaultNpcRulerPolicyStabilityPromptParagraph));

	private static readonly string PreviousDefaultNpcRulerPolicyPromptBeforeNaturalEditableLayer = (PreviousDefaultNpcRulerPolicyPromptBeforeTaxIntent
		+ "\n\n" + PreviousDefaultPolicyTownTaxIntentPromptParagraphBeforePlayerTargetRouting
		+ "\n\n" + PreviousDefaultPolicyConstructionSpeedPromptParagraphBeforePlayerAmountScale)
		.Replace(PreviousDefaultNpcRulerPolicyNumericPromptParagraph, DefaultNpcRulerPolicyNumericPromptParagraph)
		.Replace(PreviousDefaultNpcRulerPolicyStabilityPromptParagraph, DefaultPolicyStabilityOneTimePromptParagraph);

	private static string SourceDefaultNpcRulerPolicyPrompt => McmPromptDefaults.Raw("DefaultNpcRulerPolicyPrompt");
	private static string DefaultNpcRulerPolicyPrompt => McmPromptDefaults.Get("DefaultNpcRulerPolicyPrompt");

	private static readonly string LeakedNpcPolicyPoliticalWeightsPromptSuffix = LegacySettingsPrompts.Get(nameof(LeakedNpcPolicyPoliticalWeightsPromptSuffix));

	private const int DefaultCustomPolicyGoldCost = 50000;

	private const int CustomPolicyPublicFeedbackTargetMinChars = 100;

	private const int CustomPolicyPublicFeedbackTargetMaxChars = 1800;

	private const int CustomPolicyPublicFeedbackTargetStepChars = 100;

	private const int DefaultCustomPolicyPublicFeedbackTargetChars = 900;

	private const int NpcRulerPolicyCheckIntervalMinDays = 1;

	private const int NpcRulerPolicyCheckIntervalMaxDays = 30;

	private const int DefaultNpcRulerPolicyCheckIntervalDays = 7;

	private const int NpcRulerPolicyIntervalMinDays = 1;

	private const int NpcRulerPolicyIntervalMaxDays = 30;

	private const int DefaultNpcRulerPolicyIntervalDays = 7;

	private const int NpcRulerPolicyIntervalMinHours = 6;

	private const int NpcRulerPolicyIntervalMaxHours = 720;

	private const int DefaultNpcRulerPolicyIntervalHours = DefaultNpcRulerPolicyIntervalDays * 24;

	public const int DefaultShoutMinTokens = 40;

	public const int DefaultShoutMaxTokens = 200;

	public const int DailyConversationHistoryLineLimitMin = 10;

	public const int DailyConversationHistoryLineLimitMax = 500;

	public const int DefaultDailyConversationHistoryLineLimit = 100;

	public const int ActionPostprocessHistoryEntryLimitMin = 1;

	public const int ActionPostprocessHistoryEntryLimitMax = 100;

	public const int DefaultActionPostprocessHistoryEntryLimit = 100;

	public const int NobleGatheringNpcIntervalMinDays = 7;

	public const int NobleGatheringNpcIntervalMaxDays = 120;

	public const int DefaultNobleGatheringNpcIntervalDays = 21;

	public const int NobleGatheringDurationMinDays = 1;

	public const int NobleGatheringDurationMaxDays = 14;

	public const int DefaultNobleGatheringDurationDays = 5;

	public const int NobleGatheringCostMinimum = 0;

	public const int NobleGatheringCostMaximum = 500000;

	public const int DefaultNobleGatheringCost = 50000;

	public const int NobleGatheringInvitedClanRelationRewardMinimum = 0;

	public const int NobleGatheringInvitedClanRelationRewardMaximum = 20;

	public const int DefaultNobleGatheringInvitedClanRelationReward = 5;

	public const int DefaultWorldDiplomacyMaxConcurrentOrdinaryRounds = 3;

	public const int WorldDiplomacyMaxConcurrentOrdinaryRoundsMax = 12;

	public const int WorldDiplomacyHistoryCompressionTriggerThousandsMin = 64;

	public const int WorldDiplomacyHistoryCompressionTriggerThousandsMax = 900;

	public const int DefaultWorldDiplomacyHistoryCompressionTriggerThousands = 800;

	public const int WorldDiplomacyHistoryCompressionTargetThousandsMin = 8;

	public const int WorldDiplomacyHistoryCompressionTargetThousandsMax = 60;

	public const int DefaultWorldDiplomacyHistoryCompressionTargetThousands = 48;

	public const int WorldDiplomacyDeclarationCharactersMin = 1;

	public const int WorldDiplomacyDeclarationCharactersMax = 1000;

	public const int DefaultWorldDiplomacyDeclarationMinCharacters = 100;

	public const int DefaultWorldDiplomacyDeclarationMaxCharacters = 500;

	public const int WorldDiplomacyThreatComplianceIssuerRelationRewardMin = 0;

	public const int WorldDiplomacyThreatComplianceIssuerRelationRewardMax = 50;

	public const int DefaultWorldDiplomacyThreatComplianceIssuerRelationReward = 10;

	private const string NpcPersonaGenerationRequirementsFileName = "NpcPersonaGenerationRequirements.txt";

	private const string CustomPromptTextStoreFolderName = "CustomPrompts";

	private const string PolicyPromptSubfolderName = "Policy";

	private const string PlayerCustomPromptRuleJsonFileName = "PlayerCustomPromptRule.json";

	private const string KingdomRebellionSystemPromptJsonFileName = "KingdomRebellionSystemPrompt.json";

	private const string WeeklyReportWritingRequirementsJsonFileName = "WeeklyReportWritingRequirements.json";

	private const string NpcPersonaGenerationRequirementsJsonFileName = "NpcPersonaGenerationRequirements.json";

	private const string DailyMemoryCompressionWritingRequirementsJsonFileName = "DailyMemoryCompressionWritingRequirements.json";

	private const string MemoryOverviewCompressionWritingRequirementsJsonFileName = "MemoryOverviewCompressionWritingRequirements.json";

	private const string MajorActionCompressionWritingRequirementsJsonFileName = "MajorActionCompressionWritingRequirements.json";

	private const string CustomPolicyEvaluatorPromptJsonFileName = "CustomPolicyEvaluatorPrompt.json";

	private const string NpcRulerPolicyPromptJsonFileName = "NpcRulerPolicyPrompt.json";

	private const string WorldDiplomacyPromptJsonFileName = "WorldDiplomacyPrompt.json";

	private const int WorldDiplomacyPromptJsonVersion = 5;

	private const string LegacyCustomPromptTextStoreFileName = "CustomPrompts.json";

	private const int CustomPromptTextMaxChars = 60000;

	private const long CustomPromptJsonMaxBytes = 262144L;

	private static readonly Encoding CustomPromptStrictUtf8Encoding = new UTF8Encoding(false, true);

	private static readonly Encoding CustomPromptWriteEncoding = new UTF8Encoding(false);

	private static readonly object CustomPromptTextStoreFileLock = new object();

	private static readonly long CustomPromptTextStoreRefreshIntervalTicks = Stopwatch.Frequency;

	private static bool _customPromptTextStoreFolderHydrated;

	private static long _customPromptTextStoreFolderFingerprint;

	private static CustomPromptTextStoreJson _customPromptTextStoreCached;

	private static long _customPromptTextStoreRevision;

	private static long _customPromptTextStoreNextRefreshTimestamp;

	private long _appliedCustomPromptTextStoreRevision = -1L;

	private sealed class CustomPromptTextStoreJson
	{
		public int Version { get; set; } = 1;

		public string PlayerCustomPromptRule { get; set; }

		public string KingdomRebellionSystemPrompt { get; set; }

		public string WeeklyReportWritingRequirements { get; set; }

		public string WorldBulletinWritingRequirements { get; set; }

		public string NpcPersonaGenerationRequirements { get; set; }

		public string DailyMemoryCompressionWritingRequirements { get; set; }

		public string MemoryOverviewCompressionWritingRequirements { get; set; }

		public string MajorActionCompressionWritingRequirements { get; set; }

		public string CustomPolicyEvaluatorPrompt { get; set; }

		public string NpcRulerPolicyPrompt { get; set; }

		public string WorldDiplomacyPrompt { get; set; }
	}

	private sealed class CustomPromptTextJson
	{
		public int Version { get; set; } = 1;

		public string Text { get; set; }
	}

	private sealed class ModelListFetchResult
	{
		public bool Success;

		public string RequestUrl = "";

		public List<string> Models = new List<string>();

		public HttpStatusCode StatusCode;

		public string ResponseBody = "";

		public string ErrorMessage = "";

		public string ErrorCode = "";

		public IReadOnlyDictionary<string, string> ErrorArguments = new Dictionary<string, string>();
	}

	private sealed class ModelDropdownCacheSnapshot
	{
		public List<string> MainOptions { get; set; } = new List<string>();

		public string MainSelected { get; set; } = "";

		public List<string> AuxiliaryOptions { get; set; } = new List<string>();

		public string AuxiliarySelected { get; set; } = "";

		public List<string> ActionPostprocessOptions { get; set; } = new List<string>();

		public string ActionPostprocessSelected { get; set; } = "";

		public List<string> EventAndRebellionOptions { get; set; } = new List<string>();

		public string EventAndRebellionSelected { get; set; } = "";

		public List<string> TownAmbientAiOptions { get; set; } = new List<string>();

		public string TownAmbientAiSelected { get; set; } = "";

		public string SavedAtUtc { get; set; } = "";
	}

	private const string DefaultDropdownModelName = "gpt-4o-mini";

	private static readonly string ManualDropdownModelName = McmPromptDefaults.Raw("Mcm_ManualDropdownModelName");

	private static readonly string[] RemovedMainModelPresets = new string[2] { "gpt-4o", "gpt-4o-mini" };

	private const string ModelDropdownCacheFileName = "ModelDropdownCache.json";

	private static readonly object ModelDropdownCacheFileLock = new object();

	public static volatile bool ModelDropdownsDirty = false;

	public static event Action OnModelListRefreshed;

	private List<string> _mainApiModelOptions = new List<string>();

	private List<string> _auxiliaryApiModelOptions = new List<string>();

	private List<string> _actionPostprocessApiModelOptions = new List<string>();

	private List<string> _eventAndRebellionApiModelOptions = new List<string>();

	private List<string> _townAmbientAiModelOptions = new List<string>();

	private Dropdown<string> _mainApiModelDropdown = Dropdown<string>.Empty;

	private Dropdown<string> _auxiliaryApiModelDropdown = Dropdown<string>.Empty;

	private Dropdown<string> _actionPostprocessApiModelDropdown = Dropdown<string>.Empty;

	private Dropdown<string> _eventAndRebellionApiModelDropdown = Dropdown<string>.Empty;

	private Dropdown<string> _townAmbientAiModelDropdown = Dropdown<string>.Empty;

	private Dropdown<string> _shoutInputUiBackgroundDropdown = BuildShoutInputUiBackgroundDropdown(ShoutInputUiBackgroundBlack);

	private Dropdown<string> _logCleanupIntervalDropdown = BuildLogCleanupIntervalDropdown(LogCleanupEvery3Days);

	private Dropdown<string> _mainApiReasoningEffortDropdown = BuildReasoningEffortDropdown(ReasoningEffortLow);

	private Dropdown<string> _auxiliaryApiReasoningEffortDropdown = BuildReasoningEffortDropdown(ReasoningEffortLow);

	private Dropdown<string> _actionPostprocessApiReasoningEffortDropdown = BuildReasoningEffortDropdown(ReasoningEffortLow);

	private Dropdown<string> _eventAndRebellionApiReasoningEffortDropdown = BuildReasoningEffortDropdown(ReasoningEffortHigh);

	private bool _modelDropdownCacheHydrated;

	private long _modelDropdownCacheLastWriteUtcTicks;

	private const string AfdianSupportUrl = "https://www.ifdian.net/a/AnimusForge";

	public static readonly string ShoutInputUiBackgroundBlack = McmPromptDefaults.Raw("Mcm_ShoutInputUiBackgroundBlack");

	public static readonly string ShoutInputUiBackgroundWhite = McmPromptDefaults.Raw("Mcm_ShoutInputUiBackgroundWhite");

	public static readonly string ShoutInputUiBackgroundPink = McmPromptDefaults.Raw("Mcm_ShoutInputUiBackgroundPink");

	public static readonly string LogCleanupOff = McmPromptDefaults.Raw("Mcm_LogCleanupOff");

	public static readonly string LogCleanupOnStartup = McmPromptDefaults.Raw("Mcm_LogCleanupOnStartup");

	public static readonly string LogCleanupEvery30Minutes = McmPromptDefaults.Raw("Mcm_LogCleanupEvery30Minutes");

	public static readonly string LogCleanupEveryHour = McmPromptDefaults.Raw("Mcm_LogCleanupEveryHour");

	public static readonly string LogCleanupEvery6Hours = McmPromptDefaults.Raw("Mcm_LogCleanupEvery6Hours");

	public static readonly string LogCleanupEveryDay = McmPromptDefaults.Raw("Mcm_LogCleanupEveryDay");

	public static readonly string LogCleanupEvery3Days = McmPromptDefaults.Raw("Mcm_LogCleanupEvery3Days");

	public static readonly string LogCleanupEveryWeek = McmPromptDefaults.Raw("Mcm_LogCleanupEveryWeek");

	public const string ReasoningEffortLow = "low";

	public const string ReasoningEffortNone = "none";

	public const string ReasoningEffortMinimal = "minimal";

	public const string ReasoningEffortMedium = "medium";

	public const string ReasoningEffortHigh = "high";

	public const string ReasoningEffortXHigh = "xhigh";

	public const string ReasoningEffortMax = "max";

	public const int ApiMaxTokensMinimum = 512;

	public const int ApiMaxTokensMaximum = 64000;

	public const int DefaultGeneralApiMaxTokens = 12000;

	public const int PolicyApiMaxTokensMinimum = 1000;

	public const int PolicyApiMaxTokensMaximum = 100000;

	public const int DefaultPolicyApiMaxTokens = 12000;

	public const int PlayerPolicyEffectPostprocessMaxTokensMinimum = 512;

	public const int PlayerPolicyEffectPostprocessMaxTokensMaximum = 12000;

	public const int DefaultPlayerPolicyEffectPostprocessMaxTokens = 5000;

	public const int PlayerPolicyEffectModuleDetailCountMinimum = 1;

	public const int PlayerPolicyEffectModuleDetailCountMaximum = 30;

	public const int PlayerPolicyEffectModuleEffectiveDetailCountMaximum = 8;

	public const int DefaultPlayerPolicyEffectModuleDetailCount = 6;

	public const string PolicyApiSourceMain = "main";

	public const string PolicyApiSourceAuxiliary = "auxiliary";

	public const string PolicyApiSourceActionPostprocess = "action_postprocess";

	public const string PolicyApiSourceEventAndRebellion = "event_rebellion";

	private const int ConnectionTestMaxTokens = ApiMaxTokensMinimum;

	public const int DefaultEventAndRebellionApiMaxTokens = 12000;

	public const int LlmRequestTimeoutMilliseconds = 480000;

	private const int ModelCatalogRequestTimeoutMilliseconds = 20000;

	public static readonly HttpClient GlobalClient = new HttpClient
	{
		Timeout = TimeSpan.FromMilliseconds(LlmRequestTimeoutMilliseconds)
	};

	public override string Id => "AnimusForge_global_settings";

	public override string DisplayName => new TaleWorlds.Localization.TextObject("{=AF_MCM_Main_DisplayName}AnimusForge Settings").ToString();

	public override string FolderName => "AnimusForge";

	public override string FormatType => "json";

	[SettingPropertyButton("{=AF_MCM_Support_Button}Support the AnimusForge team", -1, true, "", Content = "{=AF_MCM_Support_Content}Open Afdian", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Support_Hint}Opens the Afdian page in the system default browser.")]
	[SettingPropertyGroup("{=AF_MCM_Support_Group}0. Support", GroupOrder = -400)]
	public Action OpenAfdianSupportLink { get; set; }

	[SettingPropertyText("{=AF_MCM_MainApi_Url}API address (Base URL supported)", -1, true, "", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Url_Hint}Enter your endpoint, for example https://api.openai.com/v1 or https://api.openai.com/v1/chat/completions. When you enter /v1, the mod automatically requests /v1/chat/completions.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public string ApiUrl { get; set; } = "https://api.openai.com/v1";

	[SettingPropertyText("{=AF_MCM_MainApi_Key}API key", -1, true, "", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Key_Hint}Enter your API key.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public string ApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_MainApi_Model}Model name", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Model_Hint}For example, gpt-4o-mini. Enter a model supported by your configured endpoint.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public string ModelName
	{
		get => ReadModelNameForPreset(ref _mainApiModelName, _mainApiModelDropdown);
		set => SetModelNameForPreset(value, ref _mainApiModelName, ref _mainApiModelDropdown, ref _mainApiModelOptions);
	}

	[SettingPropertyButton("{=AF_MCM_MainApi_Fetch}Fetch model list", -1, true, "", Content = "{=AF_MCM_MainApi_FetchContent}Fetch", Order = 3)]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public Action FetchMainModelList { get; set; }

	[SettingPropertyDropdown("{=AF_MCM_MainApi_Dropdown}Model name (dropdown)", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Dropdown_Hint}Fetch the model list first, then choose a model. Selecting *manual entry* uses the model name in the text field above.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public Dropdown<string> MainModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = GetMainSelectedModelOption();
			if (_mainApiModelDropdown == null || _mainApiModelDropdown.Count == 0 || IsRemovedMainModelPreset(selectedOption))
			{
				_mainApiModelOptions = FilterRemovedMainModelPresets(_mainApiModelOptions);
				if (IsRemovedMainModelPreset(selectedOption)) selectedOption = ManualDropdownModelName;
				_mainApiModelDropdown = BuildDropdownFromOptions(_mainApiModelOptions, selectedOption, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out var _);
			}
			return _mainApiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			_mainApiModelOptions = FilterRemovedMainModelPresets(_mainApiModelOptions);
			string selectedOption = _mainApiModelName;
			if (IsRemovedMainModelPreset(selectedOption))
			{
				selectedOption = ManualDropdownModelName;
			}
			_mainApiModelDropdown = BuildDropdownFromIncoming(ResolveLoadedModelDropdown(value, _mainApiModelDropdown, _mainApiModelName), _mainApiModelOptions, selectedOption, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out var normalizedSelectedOption);
			_mainApiModelOptions = FilterRemovedMainModelPresets(_mainApiModelOptions);
			if (IsRemovedMainModelPreset(normalizedSelectedOption))
			{
				normalizedSelectedOption = ManualDropdownModelName;
				_mainApiModelDropdown = BuildDropdownFromOptions(_mainApiModelOptions, normalizedSelectedOption, DefaultDropdownModelName, false, out _mainApiModelOptions, out _);
			}
			if (!string.IsNullOrWhiteSpace(normalizedSelectedOption) && !IsManualModelOption(normalizedSelectedOption))
			{
				ModelName = normalizedSelectedOption;
			}
		}
	}

	[SettingPropertyButton("{=AF_MCM_MainApi_Test}Test API connection", -1, true, "", Content = "{=AF_MCM_MainApi_TestContent}Test", Order = 5)]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public Action TestConnection { get; set; }

	[SettingPropertyBool("{=AF_MCM_MainApi_Thinking}Enable reasoning", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Thinking_Hint}When enabled, OpenAI-compatible reasoning requests include thinking.type=enabled and reasoning_effort; Anthropic/Claude requests include thinking.type=enabled and output_config.effort. When disabled, thinking.type=disabled is sent.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public bool MainApiThinkingEnabled { get; set; } = true;

	public string MainApiReasoningEffort { get; set; } = ReasoningEffortLow;

	[SettingPropertyDropdown("{=AF_MCM_MainApi_Reasoning}Reasoning strength", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Reasoning_Hint}Supports minimal/low/medium/high/xhigh/max. Disable Enable reasoning to turn reasoning off; YJ Gemini sends the selected value unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public Dropdown<string> MainApiReasoningEffortDropdown
	{
		get
		{
			_mainApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_mainApiReasoningEffortDropdown);
			MainApiReasoningEffort = ReadReasoningEffortSelection(_mainApiReasoningEffortDropdown);
			return _mainApiReasoningEffortDropdown;
		}
		set
		{
			_mainApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(value);
			MainApiReasoningEffort = ReadReasoningEffortSelection(_mainApiReasoningEffortDropdown);
		}
	}

	[SettingPropertyFloatingInteger("{=AF_MCM_MainApi_Temperature}Temperature", 0f, 2f, "0.00", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Temperature_Hint}Controls response-generation randomness. 0 is more stable and 2 is more varied. Default: 0.80.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public float MainApiTemperature { get; set; } = 0.8f;

	[SettingPropertyInteger("{=AF_MCM_MainApi_MaxTokens}Maximum output tokens", ApiMaxTokensMinimum, ApiMaxTokensMaximum, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_MainApi_MaxTokens_Hint}The max_tokens value for main API response generation. Default: 12000; endpoints that do not support a high limit may reject the request.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public int MainApiMaxTokens { get; set; } = DefaultGeneralApiMaxTokens;

	[SettingPropertyBool("{=AF_MCM_MainApi_Streaming}Enable streaming", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Streaming_Hint}When enabled, main API responses use SSE streaming. Interfaces that support chunks can display text progressively; other channels display it after completion. Postprocessing receives the complete response. This affects only the main response, not preprocessing, postprocessing, or weekly-report APIs. Off by default; applies to the next response request.")]
	[SettingPropertyGroup("{=AF_MCM_MainApi_Group}1. AI core configuration/1. Main API (response generation)", GroupOrder = -300)]
	public bool MainApiStreamingEnabled { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Duel_MinClanTier}Minimum clan tier", 0, 6, "0", Order = 0, RequireRestart = false)]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public int MinimumClanTier { get; set; } = 0;

	[SettingPropertyFloatingInteger("{=AF_MCM_Duel_HealthThreshold}Defeat health threshold", 0.1f, 0.5f, "#0%", Order = 1, RequireRestart = false)]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public float HealthThreshold { get; set; } = 0.35f;

	[SettingPropertyBool("{=AF_MCM_Duel_RenownTransfer}Enable duel clan renown transfer", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Duel_RenownTransfer_Hint}After a formal duel, the winner gains the renown lost by the loser clan. No transfer occurs when both sides belong to the same clan or either side has no clan. Loser renown never falls below 0 and clan tier is not forcibly reduced.")]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public bool EnableDuelLoserClanRenownPenalty { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Duel_RenownMinimum}Minimum renown loss", 0, 1000, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Duel_RenownMinimum_Hint}Minimum renown lost by the loser clan each time. Default 20; if the clan has less, loss is capped at 0.")]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public int DuelLoserClanRenownPenaltyMinimum { get; set; } = DefaultDuelLoserClanRenownPenaltyMinimum;

	[SettingPropertyInteger("{=AF_MCM_Duel_RenownPercent}Renown loss by current percentage (%)", 0, 50, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Duel_RenownPercent_Hint}Calculates a percentage from the loser clan renown before settlement and uses the larger value with the minimum loss. Default: 3%.")]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public int DuelLoserClanRenownPenaltyPercent { get; set; } = DefaultDuelLoserClanRenownPenaltyPercent;

	[SettingPropertyInteger("{=AF_MCM_Duel_RenownMaximum}Maximum renown loss", 0, 5000, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Duel_RenownMaximum_Hint}Maximum clan renown lost in one duel. Default 150; 0 means no cap.")]
	[SettingPropertyGroup("{=AF_MCM_Duel_Group}2. Duel rules")]
	public int DuelLoserClanRenownPenaltyMaximum { get; set; } = DefaultDuelLoserClanRenownPenaltyMaximum;

	[SettingPropertyBool("{=AF_MCM_Items_Restore}Restore NPC equipment after it is given away", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Items_Restore_Hint}When enabled, equipment NPCs lose through gifts, trade, or post-battle loot is restored to its original slot after 3 days; existing replacement equipment is not overwritten. Disabling stops new and existing restoration jobs; re-enabling resumes registered jobs. Enabled by default.")]
	[SettingPropertyGroup("{=AF_MCM_Items_Group}10. Items and trade", GroupOrder = 100)]
	public bool EnableNpcBattleEquipmentRestore { get; set; } = true;

	[SettingPropertyText("{=AF_MCM_Shout_Key}Scene shout key (one uppercase letter)", -1, true, "", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Shout_Key_Hint}Hold this key in a scene to preview and expand shout range; release it to open the speech input. Default: T.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public string ShoutKey { get; set; } = "T";

	[SettingPropertyText("{=AF_MCM_Shout_SpecialKey}Complex interaction key (one uppercase letter)", -1, true, "", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Shout_SpecialKey_Hint}Hold this key in a scene to preview and expand shout range; release it to open the complex interaction menu. Default: Y.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public string ShoutSpecialMenuKey { get; set; } = "Y";

	[SettingPropertyText("{=AF_MCM_Shout_TerminalKey}Terminal key (one uppercase letter)", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Shout_TerminalKey_Hint}Press this key on the campaign map to open the AnimusForge terminal. Default: U.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public string TerminalKey { get; set; } = "U";

	[SettingPropertyText("{=AF_MCM_Shout_GoldKey}Downed-gold pickup key (one uppercase letter)", -1, true, "", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Shout_GoldKey_Hint}During a scene taunt conflict, approach dropped coins after an NPC falls and press this key to collect them. Default: F.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public string SceneTauntGoldPickupKey { get; set; } = "F";

	[SettingPropertyFloatingInteger("{=AF_MCM_Shout_InitialRange}Initial shout range (m)", 1f, 150f, "0.0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Shout_InitialRange_Hint}Base shout range used when T/Y is pressed. Default: 4 m.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public float ShoutInitialRangeMeters { get; set; } = 4f;

	[SettingPropertyFloatingInteger("{=AF_MCM_Shout_MaxRange}Maximum shout range (m)", 1f, 150f, "0.0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Shout_MaxRange_Hint}Maximum shout range while holding T/Y, capped at 150 m. Larger ranges show more preview markers.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public float ShoutMaxRangeMeters { get; set; } = 150f;

	[SettingPropertyInteger("{=AF_MCM_Shout_MinLength}Minimum shout reply length", 1, 500, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Shout_MinLength_Hint}Minimum length of a scene-shout reply. Default: 40.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public int ShoutMinTokens { get; set; } = DefaultShoutMinTokens;

	[SettingPropertyInteger("{=AF_MCM_Shout_MaxLength}Maximum shout reply length", 1, 500, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Shout_MaxLength_Hint}Maximum length of a scene-shout reply. Default: 200; values below the minimum are corrected at runtime.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public int ShoutMaxTokens { get; set; } = DefaultShoutMaxTokens;

	[SettingPropertyInteger("{=AF_MCM_Shout_ThoughtLength}Minimum inner-thought length", 40, 2000, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Shout_ThoughtLength_Hint}Minimum length of the parenthesized inner-thought section in scene-shout replies. Minimum 40; default 200.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public int ShoutThoughtMinTokens { get; set; } = 200;

	[SettingPropertyBool("{=AF_MCM_Shout_DisableThought}Disable inner thought", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Shout_DisableThought_Hint}When enabled, scene-shout requests no longer require inner-thought output and keep only the action and spoken-line format.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public bool DisableShoutInnerThoughtPrompt { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Shout_BubbleFont}Bubble font size", 10, 40, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Shout_BubbleFont_Hint}Sets the font size of text in scene-shout bubbles.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public int BubbleFontSize { get; set; } = 14;

	[SettingPropertyFloatingInteger("{=AF_MCM_Shout_Timeout}Conversation timeout seconds per visible character", 0.5f, 3f, "0.0", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Shout_Timeout_Hint}Extends the idle dismissal time for scene conversations based on visible player and NPC words in the current exchange. Default: 1 second per character.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public float SceneConversationTimeoutSecondsPerVisibleCharacter { get; set; } = 1f;

	public bool EnablePassiveStareReaction { get; set; } = false;

	public int PassiveStareTriggerSeconds { get; set; } = 0;

	public int MinimumPositiveNpcRelationGain { get; set; } = 1;

	[SettingPropertyBool("{=AF_MCM_Shout_DirectAttack}Allow direct player attacks to trigger scene conflict", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Shout_DirectAttack_Hint}When enabled, direct attacks on NPCs in peaceful scenes can trigger this mod scene conflict. When disabled, direct attacks return fully to native damage handling; dialogue arguments and taunts can still escalate conflict.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public bool EnablePeaceSceneConflict { get; set; } = true;

	[SettingPropertyDropdown("{=AF_MCM_Shout_InputBackground}Shout input background", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Shout_InputBackground_Hint}Affects only the shout input box. Default is transparent black; transparent white or pink are also available.")]
	[SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
	public Dropdown<string> ShoutInputUiBackgroundDropdown
	{
		get
		{
			_shoutInputUiBackgroundDropdown = NormalizeShoutInputUiBackgroundDropdown(_shoutInputUiBackgroundDropdown);
			return _shoutInputUiBackgroundDropdown;
		}
		set
		{
			_shoutInputUiBackgroundDropdown = NormalizeShoutInputUiBackgroundDropdown(value);
		}
	}

	public string GetShoutInputUiBackgroundSelection()
	{
		_shoutInputUiBackgroundDropdown = NormalizeShoutInputUiBackgroundDropdown(_shoutInputUiBackgroundDropdown);
		return ReadShoutInputUiBackgroundSelection(_shoutInputUiBackgroundDropdown);
	}

	public string GetMainApiReasoningEffort()
	{
		_mainApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_mainApiReasoningEffortDropdown);
		MainApiReasoningEffort = ReadReasoningEffortSelection(_mainApiReasoningEffortDropdown);
		return MainApiReasoningEffort;
	}

	public string GetAuxiliaryApiReasoningEffort()
	{
		_auxiliaryApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_auxiliaryApiReasoningEffortDropdown);
		AuxiliaryApiReasoningEffort = ReadReasoningEffortSelection(_auxiliaryApiReasoningEffortDropdown);
		return AuxiliaryApiReasoningEffort;
	}

	public string GetActionPostprocessApiReasoningEffort()
	{
		_actionPostprocessApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_actionPostprocessApiReasoningEffortDropdown);
		ActionPostprocessApiReasoningEffort = ReadReasoningEffortSelection(_actionPostprocessApiReasoningEffortDropdown);
		return ActionPostprocessApiReasoningEffort;
	}

	public string GetEventAndRebellionApiReasoningEffort()
	{
		_eventAndRebellionApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_eventAndRebellionApiReasoningEffortDropdown);
		EventAndRebellionApiReasoningEffort = ReadReasoningEffortSelection(_eventAndRebellionApiReasoningEffortDropdown);
		return EventAndRebellionApiReasoningEffort;
	}

	public static float ClampApiTemperature(float temperature)
	{
		return Math.Max(0f, Math.Min(2f, temperature));
	}

	public float GetMainApiTemperature()
	{
		MainApiTemperature = ClampApiTemperature(MainApiTemperature);
		return MainApiTemperature;
	}

	public float GetAuxiliaryApiTemperature()
	{
		AuxiliaryApiTemperature = ClampApiTemperature(AuxiliaryApiTemperature);
		return AuxiliaryApiTemperature;
	}

	public float GetActionPostprocessApiTemperature()
	{
		ActionPostprocessApiTemperature = ClampApiTemperature(ActionPostprocessApiTemperature);
		return ActionPostprocessApiTemperature;
	}

	public float GetEventAndRebellionApiTemperature()
	{
		EventAndRebellionApiTemperature = ClampApiTemperature(EventAndRebellionApiTemperature);
		return EventAndRebellionApiTemperature;
	}

	public static int ClampApiMaxTokens(int maxTokens, int fallback)
	{
		int normalized = maxTokens > 0 ? maxTokens : fallback;
		if (normalized < ApiMaxTokensMinimum)
		{
			normalized = ApiMaxTokensMinimum;
		}
		if (normalized > ApiMaxTokensMaximum)
		{
			normalized = ApiMaxTokensMaximum;
		}
		return normalized;
	}

	public int GetMainApiMaxTokens()
	{
		MainApiMaxTokens = ClampApiMaxTokens(MainApiMaxTokens, DefaultGeneralApiMaxTokens);
		return MainApiMaxTokens;
	}

	public int GetAuxiliaryApiMaxTokens()
	{
		AuxiliaryApiMaxTokens = ClampApiMaxTokens(AuxiliaryApiMaxTokens, DefaultGeneralApiMaxTokens);
		return AuxiliaryApiMaxTokens;
	}

	public int GetActionPostprocessApiMaxTokens()
	{
		ActionPostprocessApiMaxTokens = ClampApiMaxTokens(ActionPostprocessApiMaxTokens, DefaultGeneralApiMaxTokens);
		return ActionPostprocessApiMaxTokens;
	}

	public static bool IsGcczNpcResponseUnlimitedEnabled()
	{
		try
		{
			return GetSettings()?.GcczNpcResponseUnlimited ?? SiegeNpcResponseLimitProfile.DefaultUnlimited;
		}
		catch
		{
			return SiegeNpcResponseLimitProfile.DefaultUnlimited;
		}
	}

	public static int GetGcczNpcResponseLimit()
	{
		try
		{
			return SiegeNpcResponseLimitProfile.ClampResponseLimit(GetSettings()?.GcczNpcResponseLimit ?? SiegeNpcResponseLimitProfile.DefaultResponseLimit);
		}
		catch
		{
			return SiegeNpcResponseLimitProfile.DefaultResponseLimit;
		}
	}

	internal static bool IsGcczDiagnosticLogEnabled()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance?.EnableGcczDiagnosticLog ?? true;
		}
		catch
		{
			return true;
		}
	}

	internal static bool IsWeeklyReportMapNotificationEnabled()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance?.EnableWeeklyReportMapNotifications ?? true;
		}
		catch
		{
			return true;
		}
	}

	internal static bool IsGcczVerboseDiagnosticLogEnabled()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance?.EnableGcczVerboseDiagnosticLog ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static int GetGcczDiagnosticLogMaxSizeMegabytes()
	{
		try
		{
			return ClampFeatureDiagnosticLogSize(GlobalSettings<DuelSettings>.Instance?.GcczDiagnosticLogMaxSizeMegabytes ?? FeatureDiagnosticLogDefaultSizeMegabytes);
		}
		catch
		{
			return FeatureDiagnosticLogDefaultSizeMegabytes;
		}
	}

	internal static bool IsSetsDiagnosticLogEnabled()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance?.EnableSetsDiagnosticLog ?? true;
		}
		catch
		{
			return true;
		}
	}

	internal static bool IsSetsVerboseDiagnosticLogEnabled()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance?.EnableSetsVerboseDiagnosticLog ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static int GetSetsOtherSettlementFollowerLimit()
	{
		try
		{
			return SetsSettlementEntryProfile.ClampOtherSettlementSelectedFollowerLimit(GlobalSettings<DuelSettings>.Instance?.SetsOtherSettlementFollowerLimit ?? SetsSettlementEntryProfile.OtherSettlementSelectedFollowerDefaultLimit);
		}
		catch
		{
			return SetsSettlementEntryProfile.OtherSettlementSelectedFollowerDefaultLimit;
		}
	}

	internal static int GetSetsDiagnosticLogMaxSizeMegabytes()
	{
		try
		{
			return ClampFeatureDiagnosticLogSize(GlobalSettings<DuelSettings>.Instance?.SetsDiagnosticLogMaxSizeMegabytes ?? FeatureDiagnosticLogDefaultSizeMegabytes);
		}
		catch
		{
			return FeatureDiagnosticLogDefaultSizeMegabytes;
		}
	}

	private static int ClampFeatureDiagnosticLogSize(int value)
	{
		return Math.Max(FeatureDiagnosticLogMinSizeMegabytes, Math.Min(FeatureDiagnosticLogMaxSizeMegabytes, value));
	}

	public int GetEventAndRebellionApiMaxTokens()
	{
		EventAndRebellionApiMaxTokens = ClampApiMaxTokens(EventAndRebellionApiMaxTokens, DefaultEventAndRebellionApiMaxTokens);
		return EventAndRebellionApiMaxTokens;
	}

	[SettingPropertyBool("{=AF_MCM_Dev_Trace}Enable full-code interception", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Dev_Trace_Hint}⚠️ Advanced debugging feature!\nWhen enabled, intercepts all UI clicks, state transitions, and low-level code stacks (Trace).\nProduces a very large log and is intended for developers only.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableDeepTrace { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_Data}Enable data management (history/debt/persona/player history)", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Dev_Data_Hint}Adds a Developer data-management entry to the town menu for NPC histories, debts, and persona data; the character screen also exposes player history editing.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableDevEditHistory { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_AdultPersona}Generate persona and background for new adults", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Dev_AdultPersona_Hint}When enabled, the preprocessing API generates persona and history for hero children when they become adults. Existing persona or background is never overwritten.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableAdultHeroPersonaAutoGeneration { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_ModLogic}Write Mod_Logic.txt", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Dev_ModLogic_Hint}Master logic-log switch. When disabled, Mod_Logic.txt is no longer written.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableModLogicLog { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Dev_Verbose}Write verbose debug log", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Dev_Verbose_Hint}Enable only while investigating an issue. Writes detailed Mod_Logic diagnostics and may produce many lines in large campaigns.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableVerboseModLogicLog { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_Observability}Write Observability.jsonl", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Dev_Observability_Hint}Structured observability-log switch. When disabled, Observability.jsonl is no longer written.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableObservabilityLog { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_HitRate}Write HitRate_Stats.txt", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Dev_HitRate_Hint}Hit-rate statistics log switch. When disabled, HitRate_Stats.txt is no longer written.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableHitRateStatsLog { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_Token}Write Token_Stats.txt", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Dev_Token_Hint}Token statistics log switch. When disabled, Token_Stats.txt is no longer written.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableTokenStatsLog { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Dev_Event}Write Event_Logs.txt", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Dev_Event_Hint}Event-system weekly-report log switch. When disabled, Event_Logs.txt is no longer written.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableEventLogs { get; set; } = true;

	[SettingPropertyDropdown("{=AF_MCM_Dev_Cleanup}Schedule cleanup of all logs", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Dev_Cleanup_Hint}Periodically clears current log files under AnimusForge/Logs by real time. Files and the UTF-8 BOM are retained. Default: every 3 days.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public Dropdown<string> LogCleanupIntervalDropdown
	{
		get
		{
			_logCleanupIntervalDropdown = NormalizeLogCleanupIntervalDropdown(_logCleanupIntervalDropdown);
			return _logCleanupIntervalDropdown;
		}
		set
		{
			_logCleanupIntervalDropdown = NormalizeLogCleanupIntervalDropdown(value);
		}
	}

	[SettingPropertyBool("{=AF_MCM_Dev_Performance}Enable performance monitoring", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Dev_Performance_Hint}Every 30 seconds writes FPS, slow frames, and total frame time to Mod_Logic.txt. Tick/Scope timings are collected only with verbose logs enabled. Off by default.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnablePerformanceMonitor { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Dev_Deferred}Defer daily maintenance", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Dev_Deferred_Hint}Daily settlement registers maintenance jobs; memory overview, kingdom maintenance, and weekly-report preparation run later in bounded per-frame batches. Advances on campaign-map ticks and is not paused by travel. Enabled by default.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableDeferredDailyMaintenance { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Dev_Budget}Daily maintenance frame budget (ms)", 1, 10, "0", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Dev_Budget_Hint}When deferred maintenance is enabled, work advances within this per-frame budget only when jobs are pending. Higher values finish faster but can create frame spikes. Default: 3.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public int DailyMaintenanceFrameBudgetMs { get; set; } = 3;

	[SettingPropertyBool("{=AF_MCM_Dev_FreezeDump}Create thread dump when AI chat freezes", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Dev_FreezeDump_Hint}Only while AF free-conversation AI mode or scene shout is processing, creates a compact thread dump after five seconds without a main-thread heartbeat. Disabled for ordinary chat, non-AI play, and native saving. Enabled by default.")]
	[SettingPropertyGroup("{=AF_MCM_Dev_Group}4. Developer options")]
	public bool EnableFreezeDumpCapture { get; set; } = true;

	public string GetLogCleanupIntervalSelection()
	{
		_logCleanupIntervalDropdown = NormalizeLogCleanupIntervalDropdown(_logCleanupIntervalDropdown);
		return ReadLogCleanupIntervalSelection(_logCleanupIntervalDropdown);
	}

	[SettingPropertyInteger("{=AF_MCM_Knowledge_TopN}Knowledge results limit", 1, 12, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Knowledge_TopN_Hint}Maximum related knowledge entries supplied to AI per conversation. Terms are assigned independent slots by importance; later results fill unused slots. Default: 4.")]
	[SettingPropertyGroup("{=AF_MCM_Knowledge_Group}5. Knowledge retrieval (returned)")]
	public int KnowledgeDirectTopN { get; set; } = 4;

    [SettingPropertyDropdown("{=AF_MCM_Knowledge_Device}Reranker inference device", Order = 3, RequireRestart = true, HintText = "{=AF_MCM_Knowledge_Device_Hint}Default CPU. GPU (CUDA) requires NVIDIA and optional CUDA runtime components; vector retrieval remains on CPU. Missing components, incompatible drivers, or GPU failure fall back to CPU. Restart the game fully after changing this value.")]
    [SettingPropertyGroup("{=AF_MCM_Knowledge_Group}5. Knowledge retrieval (returned)")]
    public Dropdown<string> RerankerDeviceDropdown
    {
        get => _rerankerDeviceDropdown;
        set => _rerankerDeviceDropdown = BuildFixedDisplayDropdown(value, 0,
            "{=AF_MCM_Option_DeviceCpu}CPU (default)", "{=AF_MCM_Option_DeviceGpu}GPU (CUDA / NVIDIA)");
    }
    private Dropdown<string> _rerankerDeviceDropdown = BuildFixedDisplayDropdown(null, 0,
        "{=AF_MCM_Option_DeviceCpu}CPU (default)", "{=AF_MCM_Option_DeviceGpu}GPU (CUDA / NVIDIA)");


	[SettingPropertyInteger("{=AF_MCM_Knowledge_Entity}Entity injection limit", 1, 20, "0", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Knowledge_Entity_Hint}Maximum people, places, clans, and kingdoms injected into each AI request. In preprocessing importance order, each term first occupies one highest-scoring entity slot. For people with the same name, a clan explicitly specified by another term takes priority, followed by a kingdom. Their actual campaign-map distance from the current NPC then adds a small bonus of up to 0.15; the final score may exceed 1.00. If scope or position is unavailable, the original score is used. If the highest-scoring entity is already occupied by an earlier term, the first unoccupied entity is used instead. Secondary results are added only when the limit exceeds the term count: first up to 3 for the earliest term, then for subsequent terms in order. Default: 6.")]
	[SettingPropertyGroup("{=AF_MCM_Knowledge_Group}5. Knowledge retrieval (returned)")]
	public int WorldEntityInjectMaxCount { get; set; } = 6;

	[SettingPropertyInteger("{=AF_MCM_Knowledge_Candidates}List candidate display limit", 1, 30, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Knowledge_Candidates_Hint}Number of item, equipment, party, prisoner, asset, and policy candidates injected into AI. Fills to the limit when available; otherwise shows all. Default: 10.")]
	[SettingPropertyGroup("{=AF_MCM_Knowledge_Group}5. Knowledge retrieval (returned)")]
	public int PromptListCandidateMaxCount { get; set; } = 10;

	public int RecentDialogueTurns { get; set; } = 20;

	public int HistoryRecallTopN { get; set; } = 4;

	[SettingPropertyInteger("{=AF_MCM_Memory_History}In-day chat context lines", DailyConversationHistoryLineLimitMin, DailyConversationHistoryLineLimitMax, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Memory_History_Hint}Maximum ordinary chat-history lines carried directly in courier, free-conversation, and scene-shout requests before daily compression. AFEF facts do not count. Default: 100; range 10-500.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int DailyConversationHistoryLineLimit { get; set; } = DefaultDailyConversationHistoryLineLimit;

	[SettingPropertyInteger("{=AF_MCM_Memory_Denominator}Memory compression denominator", 3, 10, "0", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Memory_Denominator_Hint}Daily summary target length is 1/N of the day's valid conversation characters. AFEF lines are excluded and never compressed. Default: 5; minimum summary length 80.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryCompressionDenominator { get; set; } = 5;

	[SettingPropertyInteger("{=AF_MCM_Memory_Rpm}Memory summary RPM", 1, 20, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Memory_Rpm_Hint}Maximum NPC/day-memory summaries per minute after a day changes. Default: 3; maximum 20.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemorySummaryRequestsPerMinute { get; set; } = 3;

	[SettingPropertyInteger("{=AF_MCM_Memory_Candidates}Memory candidate limit", 5, 40, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Memory_Candidates_Hint}Number of compressed-memory candidate blocks entering preprocessing. Default: 20; 15-20 is usually suitable. Rich-title semantic retrieval is used above this count.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryCandidateLimit { get; set; } = 20;

	[SettingPropertyInteger("{=AF_MCM_Memory_Final}Final memory injection count", 2, 20, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Memory_Final_Hint}Number of compressed memory blocks injected into the main chain. Default: 4; never exceeds the memory candidate limit.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryFinalInjectCount { get; set; } = 4;

	[SettingPropertyInteger("{=AF_MCM_Memory_PreprocessMode}Preprocess mode", 1, 2, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Memory_PreprocessMode_Hint}1 = unified mode: rule codes and memory IDs share one preprocessing JSON request; 2 = parallel mode: rule and memory filtering run concurrently. Default: 1.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryPreprocessMode { get; set; } = 1;

	[SettingPropertyInteger("{=AF_MCM_Memory_OverviewStart}Memory overview start block count", 3, 10, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Memory_OverviewStart_Hint}When a Hero NPC reaches this many compressed memory blocks, generate and update the long-term memory overview. Default: 5.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryOverviewStartBlockCount { get; set; } = 5;

	[SettingPropertyInteger("{=AF_MCM_Memory_OverviewTarget}Memory overview target characters", 100, 1000, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Memory_OverviewTarget_Hint}Target character count for the long-term memory overview. Default: 200; shorter saves tokens, longer retains more detail.")]
	[SettingPropertyGroup("{=AF_MCM_Memory_Group}5. Compressed memory")]
	public int MemoryOverviewTargetChars { get; set; } = 200;

	[SettingPropertyInteger("{=AF_MCM_Rules_TopN}Rule results limit", 1, 12, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Rules_TopN_Hint}Maximum additional rules supplied to AI per conversation. Recall and reranking counts are derived automatically and are not padded when highly relevant rules are insufficient. Default: 4.")]
	[SettingPropertyGroup("{=AF_MCM_Rules_Group}6. Rule triggers (returned)")]
	public int GuardrailDirectTopN { get; set; } = 4;

	[SettingPropertyBool("{=AF_MCM_Proactive_Enable}Enable proactive NPC contact", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Enable_Hint}When enabled, NPC parties with a clear need can pursue the player on the campaign map and start a conversation.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public bool EnableProactiveNpcRequests { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Proactive_Test}Test mode", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Test_Hint}Allows Tier 0 players to trigger contact and is useful with high chance and short cooldowns. Off by default for normal play.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public bool ProactiveNpcRequestTestMode { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Proactive_MinTier}Minimum clan tier", 0, 6, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Proactive_MinTier_Hint}Applies when test mode is off. NPC contact does not trigger below this player clan tier.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestMinClanTier { get; set; } = 1;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Chance}Trigger chance per scan", 0, 100, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Chance_Hint}Trigger probability scale for candidate NPCs. Default 35 for normal play; tests may use 100.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestChancePercent { get; set; } = 35;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Interval}Scan interval (hours)", 1, 24, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Interval_Hint}How often to scan nearby NPC parties with a need, in game hours. Default: 6.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestScanIntervalHours { get; set; } = 6;

	[SettingPropertyInteger("{=AF_MCM_Proactive_GlobalCooldown}Global cooldown (hours)", 0, 240, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Proactive_GlobalCooldown_Hint}After any proactive contact starts, no new contact starts during this many hours. Default: 24.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestGlobalCooldownHours { get; set; } = 24;

	[SettingPropertyInteger("{=AF_MCM_Proactive_HeroCooldown}Same-NPC cooldown (days)", 0, 60, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Proactive_HeroCooldown_Hint}After an NPC contacts the player, it cannot return with the same need during this many days. Default: 14.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestHeroCooldownDays { get; set; } = 14;

	[SettingPropertyFloatingInteger("{=AF_MCM_Proactive_Distance}Candidate distance multiplier", 0.5f, 5f, "0.0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Distance_Hint}Maximum candidate distance equals player sight range multiplied by this value. Default: 1.0.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcRequestDistanceMultiplier { get; set; } = 1.0f;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Food}Food threshold (days)", 0, 15, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Food_Hint}A food need can trigger when an NPC party has at most this many food days or is already starving. Default: 2 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestFoodDaysThreshold { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Money}Cash shortage threshold", 1, 50000, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Money_Hint}A cash need can trigger when an NPC lord has less available cash than this value. Runtime respects the native party cash floor. Default: 5000.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestMoneyGoldThreshold { get; set; } = 5000;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Wage}Wage coverage threshold (days)", 0, 30, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Wage_Hint}A cash need can trigger when the lord can cover wages for at most this many days; 0 checks only cash and unpaid wages. Default: 2 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestMoneyWageDaysThreshold { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Troop}Troop ratio threshold (%)", 1, 100, "0", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Troop_Hint}A troop shortage can trigger when current troops are below this percentage of party capacity. Default: 45; tests may use 90 or 100.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestTroopRatioThresholdPercent { get; set; } = 45;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Prisoner}Prisoner capacity threshold (%)", 1, 150, "0", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Prisoner_Hint}An NPC lord can initiate prisoner overload or ransom contact when the current prisoner count reaches this percentage of prisoner capacity. The normal-play default is 90; 100 means nearly full capacity, and values above 100 trigger only when over capacity.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestPrisonerRatioThresholdPercent { get; set; } = 90;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Morale}Low morale threshold", 0, 100, "0", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Morale_Hint}Low-morale contact can trigger at or below this value; 0 disables the need. Default: 35.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestLowMoraleThreshold { get; set; } = 35;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Mount}Mount ratio threshold (%)", 0, 100, "0", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Mount_Hint}Mount shortage can trigger when mounts fall below this percentage of troop count; 0 disables the need. Default: 25.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestMountRatioThresholdPercent { get; set; } = 25;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Overburden}Overburden threshold (%)", 50, 150, "0", Order = 15, RequireRestart = false, HintText = "{=AF_MCM_Proactive_Overburden_Hint}Overburden contact can trigger when total party weight reaches this percentage of inventory capacity. Default: 92.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestOverburdenRatioThresholdPercent { get; set; } = 92;

	[SettingPropertyInteger("{=AF_MCM_Proactive_ClanGold}Clan treasury threshold", 0, 200000, "0", Order = 16, RequireRestart = false, HintText = "{=AF_MCM_Proactive_ClanGold_Hint}Clan financial pressure can trigger below this treasury value; 0 ignores the treasury. Default: 15000.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestClanGoldThreshold { get; set; } = 15000;

	[SettingPropertyInteger("{=AF_MCM_Proactive_ClanDebt}Clan debt threshold", 0, 200000, "0", Order = 17, RequireRestart = false, HintText = "{=AF_MCM_Proactive_ClanDebt_Hint}Clan financial pressure can trigger above this kingdom-debt value; 0 ignores debt. Default: 5000.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestClanDebtThreshold { get; set; } = 5000;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestMarriageAdultClanThreshold}Marriage-pressure adult threshold", 1, 12, "0", Order = 18, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestMarriageAdultClanThreshold_Hint}Inheritance or marriage-pressure contact can trigger when the NPC clan has no more than this many adult core members and at least one adult unmarried member. Default: 3.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestMarriageAdultClanThreshold { get; set; } = 3;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestFiefLoyaltyThreshold}Fief loyalty threshold", 0, 100, "0", Order = 19, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestFiefLoyaltyThreshold_Hint}Governance-concern contact can trigger when NPC clan fief loyalty is at or below this value; 0 ignores loyalty. Default: 35.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestFiefLoyaltyThreshold { get; set; } = 35;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestFiefSecurityThreshold}Fief security threshold", 0, 100, "0", Order = 20, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestFiefSecurityThreshold_Hint}Governance-concern contact can trigger when NPC clan fief security is at or below this value; 0 ignores security. Default: 35.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestFiefSecurityThreshold { get; set; } = 35;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestFiefGarrisonThreshold}Fief garrison threshold", 0, 1000, "0", Order = 21, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestFiefGarrisonThreshold_Hint}Governance-concern contact can trigger when an NPC clan fief garrison is at or below this value; 0 ignores garrisons. Default: 80.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestFiefGarrisonThreshold { get; set; } = 80;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestIsolationInfluenceThreshold}Isolation influence threshold", 0, 500, "0", Order = 22, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestIsolationInfluenceThreshold_Hint}At or below this NPC clan influence, the ally count may trigger contact about a lack of allies; 0 ignores influence. Default: 40.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestIsolationInfluenceThreshold { get; set; } = 40;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestIsolationMaxFriendlyClans}Maximum friendly clans for isolation", 0, 10, "0", Order = 23, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestIsolationMaxFriendlyClans_Hint}When the NPC clan has no more than this many friendly clans in the same kingdom with relations above 20, low influence or hostile relations may trigger contact about a lack of allies. Default: 1.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestIsolationMaxFriendlyClans { get; set; } = 1;

	[SettingPropertyFloatingInteger("{=AF_MCM_ProactiveNpcKnownMajorMultiplier}Known-history need multiplier", 1f, 5f, "0.0", Order = 24, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcKnownMajorMultiplier_Hint}Multiplier for need-driven contact probability when the NPC already knows major events in the player history. Default: 2.0.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcKnownMajorMultiplier { get; set; } = 2f;

	[SettingPropertyFloatingInteger("{=AF_MCM_ProactiveNpcNotorietyChanceMultiplier}Notoriety trigger multiplier", 0f, 3f, "0.0", Order = 25, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcNotorietyChanceMultiplier_Hint}Multiplier converting effective player notoriety into additional proactive NPC contact probability. Default for normal play: 0.35.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcNotorietyChanceMultiplier { get; set; } = 0.35f;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcMinNeedUrgency}Minimum need urgency", 0, 100, "0", Order = 26, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcMinNeedUrgency_Hint}Minimum need urgency required for proactive NPC contact. Test mode treats this as 0 at runtime. Default for normal play: 60.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcMinNeedUrgency { get; set; } = 60;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestTypeFatigueDays}Request-type fatigue (days)", 0, 60, "0", Order = 27, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestTypeFatigueDays_Hint}After the player receives a request of a given type, greatly reduces that type trigger chance for this many game days. 0 disables type fatigue. Default: 10 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestTypeFatigueDays { get; set; } = 10;

	[SettingPropertyFloatingInteger("{=AF_MCM_ProactiveNpcRequestTypeFatigueMultiplier}Fatigued-type probability multiplier", 0f, 1f, "0.00", Order = 28, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestTypeFatigueMultiplier_Hint}Multiplies both need-driven and notoriety-driven probabilities for requests under type fatigue, and reduces that need priority among candidates. Default: 0.20.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcRequestTypeFatigueMultiplier { get; set; } = 0.2f;

	[SettingPropertyFloatingInteger("{=AF_MCM_ProactiveNpcRequestMountShortageWeight}Mount-shortage request weight", 0f, 1f, "0.00", Order = 29, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestMountShortageWeight_Hint}Candidate-weight and trigger-probability multiplier for mount-shortage or horse-purchase requests in map contact and unsolicited letters. Default: 0.35; 0 retains need detection but prevents proactive initiation. Map pursuit in test mode still uses 1.00.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcRequestMountShortageWeight { get; set; } = 0.35f;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestRomanticInteractionGlobalCooldownDays}Romantic interaction global cooldown (days)", 1, 120, "0", Order = 30, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestRomanticInteractionGlobalCooldownDays_Hint}Shared hard cooldown across map pursuit, unsolicited letters, and companion chat for romantic interactions with the opposite sex and private relations above 30. Default: 21 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestRomanticInteractionGlobalCooldownDays { get; set; } = 21;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestGreetingGlobalCooldownDays}Proactive greeting global cooldown (days)", 1, 120, "0", Order = 31, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestGreetingGlobalCooldownDays_Hint}Shared hard cooldown after an NPC with private relations above 20 pursues the player or sends an unsolicited greeting letter. Default: 42 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestGreetingGlobalCooldownDays { get; set; } = 42;

	[SettingPropertyFloatingInteger("{=AF_MCM_ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier}Territorial questioning range multiplier", 0.5f, 10f, "0.0", Order = 32, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier_Hint}When the player is within player sight range times this multiplier of a kingdom settlement, that kingdom lords of a different culture who do not know the player history may proactively question their origins and purpose. Default: 3.0.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public float ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier { get; set; } = 3f;

	[SettingPropertyInteger("{=AF_MCM_ProactiveNpcRequestTerritorialInterrogationGlobalCooldownDays}Territorial questioning global cooldown (days)", 1, 180, "0", Order = 33, RequireRestart = false, HintText = "{=AF_MCM_ProactiveNpcRequestTerritorialInterrogationGlobalCooldownDays_Hint}Shared hard cooldown after territorial questioning is successfully initiated. Default: 42 days.")]
	[SettingPropertyGroup("{=AF_MCM_Proactive_Group}7. Proactive NPC contact")]
	public int ProactiveNpcRequestTerritorialInterrogationGlobalCooldownDays { get; set; } = 42;

	[SettingPropertyBool("{=AF_MCM_Letter_Enable}Enable NPC-initiated letters", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Letter_Enable_Hint}Allows NPCs who know the player and have sufficient bond to send a real courier.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public bool EnableNpcInitiatedLetters { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Letter_Test}Letter test mode", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Letter_Test_Hint}Raises chance to 100% and removes letter cooldowns; NPC knowledge, valid location, and a real motive are still required.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public bool NpcInitiatedLetterTestMode { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Letter_Bond}Minimum bond score", 0, 100, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Letter_Bond_Hint}Minimum candidate score after averaging private relations and letter trust. Default: 10.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterMinBondScore { get; set; } = 10;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Interval}Scan interval (hours)", 1, 168, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Letter_Interval_Hint}How often to scan unsolicited-letter candidates in game hours. Default: 24.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterScanIntervalHours { get; set; } = 24;

	[SettingPropertyFloatingInteger("{=AF_MCM_Letter_Chance}Letter chance multiplier", 0f, 2f, "0.00", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Letter_Chance_Hint}Per-scan chance equals bond score multiplied by this value. Default 0.25, or 25% at score 100.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public float NpcInitiatedLetterChanceMultiplier { get; set; } = 0.25f;

	[SettingPropertyInteger("{=AF_MCM_Letter_GlobalCooldown}Global cooldown (days)", 0, 60, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Letter_GlobalCooldown_Hint}After any NPC sends a letter, prevents a new unsolicited letter for this many game days. Default: 5.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterGlobalCooldownDays { get; set; } = 5;

	[SettingPropertyInteger("{=AF_MCM_Letter_LowCooldown}Low-score NPC cooldown (days)", 1, 120, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Letter_LowCooldown_Hint}Sender cooldown for NPCs with the minimum bond score. Default: 45 days.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterLowScoreCooldownDays { get; set; } = 45;

	[SettingPropertyInteger("{=AF_MCM_Letter_HighCooldown}High-score NPC cooldown (days)", 1, 60, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Letter_HighCooldown_Hint}Sender cooldown for NPCs with a score of 100. Default: 14 days.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterHighScoreCooldownDays { get; set; } = 14;

	[SettingPropertyInteger("{=AF_MCM_Letter_Quiet}Recent-contact quiet period (days)", 0, 30, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Letter_Quiet_Hint}After any channel talks with an NPC, wait at least this many game days before allowing an unsolicited letter. Default: 3.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterQuietDays { get; set; } = 3;

	[SettingPropertyInteger("{=AF_MCM_Letter_TrustCap}Public-trust contribution cap", 0, 100, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Letter_TrustCap_Hint}Cap on positive or negative public-trust contribution to letter trust. Default: ±20.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterPublicTrustCap { get; set; } = 20;

	[SettingPropertyInteger("{=AF_MCM_Letter_Fatigue}Motive fatigue (days)", 0, 120, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Letter_Fatigue_Hint}Duration of reduced weight when the same NPC repeats a letter motive. Default: 30 days.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterMotiveFatigueDays { get; set; } = 30;

	[SettingPropertyFloatingInteger("{=AF_MCM_Letter_FatigueMultiplier}Fatigued-motive weight multiplier", 0f, 1f, "0.00", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Letter_FatigueMultiplier_Hint}Weight multiplier for greeting, status, event, emotional, request, or diplomatic motives during fatigue. Default: 0.25.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public float NpcInitiatedLetterMotiveFatigueMultiplier { get; set; } = 0.25f;

	[SettingPropertyInteger("{=AF_MCM_Letter_TargetChars}Letter target characters", 80, 1000, "0", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Letter_TargetChars_Hint}Target body length for an unsolicited NPC letter. Default: 220 characters.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public int NpcInitiatedLetterTargetChars { get; set; } = 220;

	[SettingPropertyBool("{=AF_MCM_Letter_Debug}Write unsolicited-letter debug log", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Letter_Debug_Hint}Logs candidate scores, chance, motives, cooldowns, and skip reasons to Mod_Logic.txt.")]
	[SettingPropertyGroup("{=AF_MCM_Letter_Group}7. Proactive NPC contact/Unsolicited letters")]
	public bool NpcInitiatedLetterDebugLog { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Companion_Enable}Enable companion proactive chat", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Companion_Enable_Hint}Allows companions, family members, and other Heroes in the player party to request a conversation through a campaign-map notification.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public bool EnableCompanionProactiveChat { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Companion_Test}Companion proactive-chat test mode", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Companion_Test_Hint}Scans every game hour, uses 100% trigger chance, and ignores cooldowns; valid Hero state, safe player state, and a valid motive are still required.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public bool CompanionProactiveChatTestMode { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Proactive_Interval}Scan interval (hours)", 1, 168, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Companion_Interval_Hint}How often to scan Heroes in the player party, in game hours. Default: 24.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatScanIntervalHours { get; set; } = 24;

	[SettingPropertyInteger("{=AF_MCM_Letter_GlobalCooldown}Global cooldown (days)", 0, 120, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Companion_GlobalCooldown_Hint}After a companion proactive-chat notice is created, no new notice is created during this many game days. Default: 21.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatGlobalCooldownDays { get; set; } = 21;

	[SettingPropertyInteger("{=AF_MCM_Companion_HeroCooldown}Same-Hero cooldown (days)", 0, 240, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Companion_HeroCooldown_Hint}After the same Hero requests a conversation, wait this many game days before another request. Default: 48.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatHeroCooldownDays { get; set; } = 48;

	[SettingPropertyInteger("{=AF_MCM_Companion_Quiet}Cross-channel quiet period (days)", 0, 120, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Companion_Quiet_Hint}After any channel talks with the Hero, wait at least this many game days before allowing proactive chat. Default: 21.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatInteractionQuietDays { get; set; } = 21;

	[SettingPropertyInteger("{=AF_MCM_Companion_NoticeLifetime}Notice lifetime (days)", 1, 30, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Companion_NoticeLifetime_Hint}How many game days a companion proactive-chat notice remains valid; after expiry it counts as declined. Default: 3.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatNoticeLifetimeDays { get; set; } = 3;

	[SettingPropertyFloatingInteger("{=AF_MCM_Companion_Chance}Trigger chance multiplier", 0f, 5f, "0.00", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Companion_Chance_Hint}Overall multiplier for the daily companion proactive-chat chance. Default: 1.00.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public float CompanionProactiveChatChanceMultiplier { get; set; } = 1f;

	[SettingPropertyInteger("{=AF_MCM_Letter_Fatigue}Motive fatigue (days)", 0, 120, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Companion_Fatigue_Hint}Duration for which a selected motive type receives reduced weight. Default: 30 days.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatMotiveFatigueDays { get; set; } = 30;

	[SettingPropertyFloatingInteger("{=AF_MCM_Letter_FatigueMultiplier}Fatigued-motive weight multiplier", 0f, 1f, "0.00", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Companion_FatigueMultiplier_Hint}Weight multiplier for motive candidates during fatigue. Default: 0.25.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public float CompanionProactiveChatMotiveFatigueMultiplier { get; set; } = 0.25f;

	[SettingPropertyInteger("{=AF_MCM_Companion_RecentEvent}Recent-event window (days)", 1, 30, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Companion_RecentEvent_Hint}How many recent game days of real Hero or player events can be used as conversation motives. Default: 10.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public int CompanionProactiveChatRecentEventWindowDays { get; set; } = 10;

	[SettingPropertyBool("{=AF_MCM_Companion_Debug}Write companion proactive-chat debug log", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Companion_Debug_Hint}Writes candidates, chance, motives, cooldowns, and notice state to Mod_Logic.txt.")]
	[SettingPropertyGroup("{=AF_MCM_Companion_Group}7. Proactive NPC contact/Companion proactive chat")]
	public bool CompanionProactiveChatDebugLog { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Notoriety_Interval}Player history summary interval (days)", 1, 30, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Notoriety_Interval_Hint}Every how many game days to roll up the player public-history material. Default: 3.")]
	[SettingPropertyGroup("{=AF_MCM_Notoriety_Group}8. Player notoriety")]
	public int PlayerNotorietySummaryIntervalDays { get; set; } = 3;

	[SettingPropertyInteger("{=AF_MCM_Notoriety_Chars}Player history target characters", 80, 1000, "0", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Notoriety_Chars_Hint}Target length and hard cap for the player public major-history summary and its main-chain injection. Default: 300.")]
	[SettingPropertyGroup("{=AF_MCM_Notoriety_Group}8. Player notoriety")]
	public int PlayerNotorietyMajorPromptChars { get; set; } = 300;

	[SettingPropertyFloatingInteger("{=AF_MCM_Notoriety_Distance}Courier recent-action distance multiplier", 0.5f, 10f, "0.0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Notoriety_Distance_Hint}When the target NPC is within player sight range multiplied by this value at the moment a letter is sent, the courier chain can expose the player recent action. Default: 3.0.")]
	[SettingPropertyGroup("{=AF_MCM_Notoriety_Group}8. Player notoriety")]
	public float PlayerNotorietyCourierRecentDistanceMultiplier { get; set; } = 3f;

	[SettingPropertyBool("{=AF_MCM_Notoriety_Debug}Write notoriety debug log", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Notoriety_Debug_Hint}Writes player notoriety records, knowledge checks, summaries, and injection details to Mod_Logic.txt.")]
	[SettingPropertyGroup("{=AF_MCM_Notoriety_Group}8. Player notoriety")]
	public bool PlayerNotorietyDebugLogs { get; set; } = false;

	public bool UseAuxiliaryRuleApi { get; set; } = false;

	[SettingPropertyText("{=AF_MCM_AuxApi_Url}Auxiliary API address (Base URL supported)", -1, true, "", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_AuxApi_Url_Hint}Low-cost endpoint for rule retrieval, rule routing, and lightweight scene conversations; for example, https://api.openai.com/v1. Entering /v1 automatically appends /v1/chat/completions.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public string AuxiliaryApiUrl { get; set; } = "https://api.openai.com/v1";

	[SettingPropertyText("{=AF_MCM_AuxApi_Key}Auxiliary API key", -1, true, "", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_AuxApi_Key_Hint}Enter the auxiliary API key.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public string AuxiliaryApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_AuxApi_Model}Auxiliary model name", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_AuxApi_Model_Hint}Low-cost model for rule retrieval, rule routing, and lightweight scene conversations.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public string AuxiliaryModelName
	{
		get => ReadModelNameForPreset(ref _auxiliaryApiModelName, _auxiliaryApiModelDropdown);
		set => SetModelNameForPreset(value, ref _auxiliaryApiModelName, ref _auxiliaryApiModelDropdown, ref _auxiliaryApiModelOptions);
	}

	[SettingPropertyButton("{=AF_MCM_MainApi_Fetch}Fetch model list", -1, true, "", Content = "{=AF_MCM_MainApi_FetchContent}Fetch", Order = 3)]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public Action FetchAuxiliaryModelList { get; set; }

	[SettingPropertyDropdown("{=AF_MCM_AuxApi_Dropdown}Auxiliary model name (dropdown)", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Dropdown_Hint}Fetch the model list first, then choose a model. Selecting *manual entry* uses the model name in the text field above.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public Dropdown<string> AuxiliaryModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = GetAuxiliarySelectedModelOption();
			if (_auxiliaryApiModelDropdown == null || _auxiliaryApiModelDropdown.Count == 0)
				_auxiliaryApiModelDropdown = BuildDropdownFromOptions(_auxiliaryApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _auxiliaryApiModelOptions, out var _);
			return _auxiliaryApiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = _auxiliaryApiModelName;
			_auxiliaryApiModelDropdown = BuildDropdownFromIncoming(ResolveLoadedModelDropdown(value, _auxiliaryApiModelDropdown, _auxiliaryApiModelName), _auxiliaryApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _auxiliaryApiModelOptions, out var normalizedSelectedOption);
			if (!string.IsNullOrWhiteSpace(normalizedSelectedOption) && !IsManualModelOption(normalizedSelectedOption))
			{
				AuxiliaryModelName = normalizedSelectedOption;
			}
		}
	}

	[SettingPropertyButton("{=AF_MCM_AuxApi_Test}Test auxiliary API connection", -1, true, "", Content = "{=AF_MCM_MainApi_TestContent}Test", Order = 5)]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public Action TestAuxiliaryConnection { get; set; }

	[SettingPropertyBool("{=AF_MCM_MainApi_Thinking}Enable reasoning", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Thinking_Hint}When enabled, OpenAI-compatible reasoning requests include thinking.type=enabled and reasoning_effort; Anthropic/Claude requests include thinking.type=enabled and output_config.effort. When disabled, thinking.type=disabled is sent.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public bool AuxiliaryApiThinkingEnabled { get; set; } = false;

	public string AuxiliaryApiReasoningEffort { get; set; } = ReasoningEffortLow;

	[SettingPropertyDropdown("{=AF_MCM_MainApi_Reasoning}Reasoning strength", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Reasoning_Hint}Supports minimal/low/medium/high/xhigh/max. Disable Enable reasoning to turn reasoning off; YJ Gemini sends the selected value unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public Dropdown<string> AuxiliaryApiReasoningEffortDropdown
	{
		get
		{
			_auxiliaryApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_auxiliaryApiReasoningEffortDropdown);
			AuxiliaryApiReasoningEffort = ReadReasoningEffortSelection(_auxiliaryApiReasoningEffortDropdown);
			return _auxiliaryApiReasoningEffortDropdown;
		}
		set
		{
			_auxiliaryApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(value);
			AuxiliaryApiReasoningEffort = ReadReasoningEffortSelection(_auxiliaryApiReasoningEffortDropdown);
		}
	}

	[SettingPropertyFloatingInteger("{=AF_MCM_MainApi_Temperature}Temperature", 0f, 2f, "0.00", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_AuxApi_Temperature_Hint}Controls randomness for preprocessing, rule routing, and lightweight conversations. 0 is more stable and 2 is more varied. Default: 0.00.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public float AuxiliaryApiTemperature { get; set; } = 0f;

	[SettingPropertyInteger("{=AF_MCM_MainApi_MaxTokens}Maximum output tokens", ApiMaxTokensMinimum, ApiMaxTokensMaximum, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_AuxApi_MaxTokens_Hint}The max_tokens value for preprocessing rule retrieval, rule routing, and lightweight conversation calls. Default: 12000; endpoints that do not support a high limit may reject the request.")]
	[SettingPropertyGroup("{=AF_MCM_AuxApi_Group}1. AI core configuration/2. Preprocessing API (rule retrieval and lightweight conversation)", GroupOrder = -290)]
	public int AuxiliaryApiMaxTokens { get; set; } = DefaultGeneralApiMaxTokens;

	[SettingPropertyText("{=AF_MCM_PostprocessApi_Url}Action postprocess API address (Base URL supported)", -1, true, "", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_Url_Hint}Dedicated endpoint for action and emotion tag postprocessing; for example, https://api.openai.com/v1. Entering /v1 automatically appends /v1/chat/completions. Leave blank to fall back to the main API.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public string ActionPostprocessApiUrl { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_PostprocessApi_Key}Action postprocess API key", -1, true, "", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_Key_Hint}Enter the action postprocess API key. Leave blank to fall back to the main API.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public string ActionPostprocessApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_PostprocessApi_Model}Action postprocess model name", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_Model_Hint}Model used for tag postprocessing. Leave blank to fall back to the main API. A reasoning-capable or more advanced model is recommended for stable tag decisions.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public string ActionPostprocessModelName
	{
		get => ReadModelNameForPreset(ref _actionPostprocessApiModelName, _actionPostprocessApiModelDropdown);
		set => SetModelNameForPreset(value, ref _actionPostprocessApiModelName, ref _actionPostprocessApiModelDropdown, ref _actionPostprocessApiModelOptions);
	}

	[SettingPropertyButton("{=AF_MCM_MainApi_Fetch}Fetch model list", -1, true, "", Content = "{=AF_MCM_MainApi_FetchContent}Fetch", Order = 3)]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public Action FetchActionPostprocessModelList { get; set; }

	[SettingPropertyDropdown("{=AF_MCM_PostprocessApi_Dropdown}Action postprocess model name (dropdown)", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Dropdown_Hint}Fetch the model list first, then choose a model. Selecting *manual entry* uses the model name in the text field above.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public Dropdown<string> ActionPostprocessModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = GetActionPostprocessSelectedModelOption();
			if (_actionPostprocessApiModelDropdown == null || _actionPostprocessApiModelDropdown.Count == 0)
				_actionPostprocessApiModelDropdown = BuildDropdownFromOptions(_actionPostprocessApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _actionPostprocessApiModelOptions, out var _);
			return _actionPostprocessApiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = _actionPostprocessApiModelName;
			_actionPostprocessApiModelDropdown = BuildDropdownFromIncoming(ResolveLoadedModelDropdown(value, _actionPostprocessApiModelDropdown, _actionPostprocessApiModelName), _actionPostprocessApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _actionPostprocessApiModelOptions, out var normalizedSelectedOption);
			if (!string.IsNullOrWhiteSpace(normalizedSelectedOption) && !IsManualModelOption(normalizedSelectedOption))
			{
				ActionPostprocessModelName = normalizedSelectedOption;
			}
		}
	}

	[SettingPropertyButton("{=AF_MCM_PostprocessApi_Test}Test action postprocess API connection", -1, true, "", Content = "{=AF_MCM_MainApi_TestContent}Test", Order = 5)]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public Action TestActionPostprocessConnection { get; set; }

	[SettingPropertyBool("{=AF_MCM_MainApi_Thinking}Enable reasoning", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Thinking_Hint}When enabled, OpenAI-compatible reasoning requests include thinking.type=enabled and reasoning_effort; Anthropic/Claude requests include thinking.type=enabled and output_config.effort. When disabled, thinking.type=disabled is sent.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public bool ActionPostprocessApiThinkingEnabled { get; set; } = false;

	public string ActionPostprocessApiReasoningEffort { get; set; } = ReasoningEffortLow;

	[SettingPropertyDropdown("{=AF_MCM_MainApi_Reasoning}Reasoning strength", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Reasoning_Hint}Supports minimal/low/medium/high/xhigh/max. Disable Enable reasoning to turn reasoning off; YJ Gemini sends the selected value unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public Dropdown<string> ActionPostprocessApiReasoningEffortDropdown
	{
		get
		{
			_actionPostprocessApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_actionPostprocessApiReasoningEffortDropdown);
			ActionPostprocessApiReasoningEffort = ReadReasoningEffortSelection(_actionPostprocessApiReasoningEffortDropdown);
			return _actionPostprocessApiReasoningEffortDropdown;
		}
		set
		{
			_actionPostprocessApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(value);
			ActionPostprocessApiReasoningEffort = ReadReasoningEffortSelection(_actionPostprocessApiReasoningEffortDropdown);
		}
	}

	[SettingPropertyFloatingInteger("{=AF_MCM_MainApi_Temperature}Temperature", 0f, 2f, "0.00", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_Temperature_Hint}Controls randomness for action and emotion tag decisions. Keeping this low is recommended. Default: 0.00.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public float ActionPostprocessApiTemperature { get; set; } = 0f;

	[SettingPropertyInteger("{=AF_MCM_MainApi_MaxTokens}Maximum output tokens", ApiMaxTokensMinimum, ApiMaxTokensMaximum, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_MaxTokens_Hint}The max_tokens value for action and emotion tag decisions. Default: 12000; endpoints that do not support a high limit may reject the request.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public int ActionPostprocessApiMaxTokens { get; set; } = DefaultGeneralApiMaxTokens;

	[SettingPropertyInteger("{=AF_MCM_PostprocessApi_HistoryLimit}Postprocess history entry limit", ActionPostprocessHistoryEntryLimitMin, ActionPostprocessHistoryEntryLimitMax, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_PostprocessApi_HistoryLimit_Hint}Maximum history entries carried in action postprocessing <history>. The current <latest_reply> is retained separately and does not count against this limit. Default: 100; minimum 1, maximum 100. Higher values usually increase request tokens and wait time.")]
	[SettingPropertyGroup("{=AF_MCM_PostprocessApi_Group}1. AI core configuration/3. Postprocessing API (action and emotion tags)", GroupOrder = -280)]
	public int ActionPostprocessHistoryEntryLimit { get; set; } = DefaultActionPostprocessHistoryEntryLimit;

	[SettingPropertyText("{=AF_MCM_EventRebellionApi_Url}Event and rebellion API address (Base URL supported)", -1, true, "", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_Url_Hint}Dedicated endpoint for event-system weekly reports and kingdom-rebellion naming; for example, https://api.openai.com/v1. Entering /v1 automatically appends /v1/chat/completions. Leave blank to fall back to the main API.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public string EventAndRebellionApiUrl { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_EventRebellionApi_Key}Event and rebellion API key", -1, true, "", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_Key_Hint}Enter the event and rebellion API key. Leave blank to fall back to the main API.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public string EventAndRebellionApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_EventRebellionApi_Model}Event and rebellion model name", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_Model_Hint}Model used for event weekly reports and kingdom-rebellion naming. Leave blank to fall back to the main API.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public string EventAndRebellionModelName
	{
		get => ReadModelNameForPreset(ref _eventAndRebellionApiModelName, _eventAndRebellionApiModelDropdown);
		set => SetModelNameForPreset(value, ref _eventAndRebellionApiModelName, ref _eventAndRebellionApiModelDropdown, ref _eventAndRebellionApiModelOptions);
	}

	[SettingPropertyButton("{=AF_MCM_MainApi_Fetch}Fetch model list", -1, true, "", Content = "{=AF_MCM_MainApi_FetchContent}Fetch", Order = 3)]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public Action FetchEventAndRebellionModelList { get; set; }

	[SettingPropertyDropdown("{=AF_MCM_EventRebellionApi_Dropdown}Event and rebellion model name (dropdown)", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Dropdown_Hint}Fetch the model list first, then choose a model. Selecting *manual entry* uses the model name in the text field above.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public Dropdown<string> EventAndRebellionModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = GetEventAndRebellionSelectedModelOption();
			if (_eventAndRebellionApiModelDropdown == null || _eventAndRebellionApiModelDropdown.Count == 0)
				_eventAndRebellionApiModelDropdown = BuildDropdownFromOptions(_eventAndRebellionApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _eventAndRebellionApiModelOptions, out var _);
			return _eventAndRebellionApiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			string selectedOption = _eventAndRebellionApiModelName;
			_eventAndRebellionApiModelDropdown = BuildDropdownFromIncoming(ResolveLoadedModelDropdown(value, _eventAndRebellionApiModelDropdown, _eventAndRebellionApiModelName), _eventAndRebellionApiModelOptions, selectedOption, "", preserveBlankSelection: false, out _eventAndRebellionApiModelOptions, out var normalizedSelectedOption);
			if (!string.IsNullOrWhiteSpace(normalizedSelectedOption) && !IsManualModelOption(normalizedSelectedOption))
			{
				EventAndRebellionModelName = normalizedSelectedOption;
			}
		}
	}

	[SettingPropertyButton("{=AF_MCM_EventRebellionApi_Test}Test event and rebellion API connection", -1, true, "", Content = "{=AF_MCM_MainApi_TestContent}Test", Order = 5)]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public Action TestEventAndRebellionConnection { get; set; }

	[SettingPropertyBool("{=AF_MCM_MainApi_Thinking}Enable reasoning", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_Thinking_Hint}Off by default. Weekly reports and kingdom-rebellion naming require strict formats; enable this only after confirming that the endpoint reliably writes the final answer to content. OpenAI-compatible reasoning requests include thinking.type=enabled and reasoning_effort; Anthropic/Claude requests include thinking.type=enabled and output_config.effort. When disabled, thinking.type=disabled is sent.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public bool EventAndRebellionApiThinkingEnabled { get; set; } = false;

	public string EventAndRebellionApiReasoningEffort { get; set; } = ReasoningEffortHigh;

	[SettingPropertyDropdown("{=AF_MCM_MainApi_Reasoning}Reasoning strength", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_MainApi_Reasoning_Hint}Supports minimal/low/medium/high/xhigh/max. Disable Enable reasoning to turn reasoning off; YJ Gemini sends the selected value unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public Dropdown<string> EventAndRebellionApiReasoningEffortDropdown
	{
		get
		{
			_eventAndRebellionApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(_eventAndRebellionApiReasoningEffortDropdown);
			EventAndRebellionApiReasoningEffort = ReadReasoningEffortSelection(_eventAndRebellionApiReasoningEffortDropdown);
			return _eventAndRebellionApiReasoningEffortDropdown;
		}
		set
		{
			_eventAndRebellionApiReasoningEffortDropdown = NormalizeReasoningEffortDropdown(value);
			EventAndRebellionApiReasoningEffort = ReadReasoningEffortSelection(_eventAndRebellionApiReasoningEffortDropdown);
		}
	}

	[SettingPropertyFloatingInteger("{=AF_MCM_MainApi_Temperature}Temperature", 0f, 2f, "0.00", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_Temperature_Hint}Controls randomness for event weekly reports and kingdom-rebellion naming. 0 is more stable and 2 is more varied. Default: 0.80.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public float EventAndRebellionApiTemperature { get; set; } = 0.8f;

	[SettingPropertyInteger("{=AF_MCM_MainApi_MaxTokens}Maximum output tokens", ApiMaxTokensMinimum, ApiMaxTokensMaximum, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_EventRebellionApi_MaxTokens_Hint}The max_tokens value for event weekly reports, kingdom-rebellion naming, and ruler policy generation. Default: 12000; endpoints that do not support a high limit may reject the request.")]
	[SettingPropertyGroup("{=AF_MCM_EventRebellionApi_Group}1. AI core configuration/4. Event and kingdom-rebellion API (weekly reports and naming)", GroupOrder = -270)]
	public int EventAndRebellionApiMaxTokens { get; set; } = DefaultEventAndRebellionApiMaxTokens;

	[SettingPropertyBool("{=AF_MCM_Tts_EnableSpeech}Enable TTS speech", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Tts_EnableSpeech_Hint}Master switch. When disabled, NPCs stop playing TTS audio and fall back to text bubbles.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public bool EnableTtsSpeech { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Tts_VolcEnabled}Enable Volcengine dedicated mode", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Tts_VolcEnabled_Hint}Selects V1 or V3 HTTP unidirectional synthesis (including Agent Plan) by API URL. Both collect all audio before playback; existing settings are not migrated automatically.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public bool TtsVolcDedicatedEnabled { get; set; } = false;

	[SettingPropertyText("{=AF_MCM_Tts_Url}Volcengine dedicated API address", -1, true, "", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Tts_Url_Hint}V1: https://openspeech.bytedance.com/api/v1/tts; V3: https://openspeech.bytedance.com/api/v3/tts/unidirectional; Agent Plan: https://openspeech.bytedance.com/api/v3/plan/tts/unidirectional (HTTP, not SSE/WS).")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedApiUrl { get; set; } = "https://openspeech.bytedance.com/api/v1/tts";

	[SettingPropertyText("{=AF_MCM_Tts_Token}Volcengine dedicated token / API key", -1, true, "", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Tts_Token_Hint}V1 uses your legacy token; its HTTP header is Authorization: Bearer; followed by the token. V3 uses the corresponding speech-service API key (X-Api-Key). Agent Plan requires its dedicated API key; do not reuse the old token.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_Tts_AppId}Volcengine dedicated App ID", -1, true, "", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Tts_AppId_Hint}V1 requires app.appid. V3 API-key authentication does not use it, so it may be blank. Switching protocols does not clear the old value.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedAppKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_Tts_ResourceId}Volcengine dedicated resource ID", -1, true, "", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Tts_ResourceId_Hint}Written to the X-Api-Resource-Id request header. The resource and voice must be enabled and authorized; synthesis voices from model 2.0 use V3. Accepted values include seed-tts-1.0 / seed-tts-1.0-concurr / seed-tts-2.0 / seed-icl-1.0 / seed-icl-1.0-concurr / seed-icl-2.0.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedResourceId { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_Tts_VoiceType}Volcengine dedicated voice ID", -1, true, "", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Tts_VoiceType_Hint}Maps to voice_type in V1 and speaker in V3. It must match the resource ID and account authorization; the old voice is not replaced automatically.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedSpeaker { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_Tts_ExtraParam}Volcengine dedicated extra parameters (JSON object)", -1, true, "", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Tts_ExtraParam_Hint}Written to request.extra_param in V1 or req_params.additions in V3, both as JSON strings. Use parameters supported by the selected endpoint.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedAdditionsJson { get; set; } = "{}";

	[SettingPropertyText("{=AF_MCM_Tts_AudioFormat}Volcengine dedicated audio format", -1, true, "", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Tts_AudioFormat_Hint}Supports wav or pcm only. V1 requests the selected format unchanged. V3 always receives PCM and wraps it as selected here, retaining the current player.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public string TtsVolcDedicatedAudioFormat { get; set; } = "wav";

	[SettingPropertyInteger("{=AF_MCM_Tts_SampleRate}Volcengine dedicated sample rate", 8000, 24000, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Tts_SampleRate_Hint}Recommended values: 8000 / 16000 / 24000 Hz. V3 also supports 22050 Hz; arbitrary integer sample rates are not accepted by V3.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public int TtsVolcDedicatedSampleRate { get; set; } = 24000;

	[SettingPropertyFloatingInteger("{=AF_MCM_Tts_MainVolume}Main voice volume (winmm)", 0f, 1f, "0.00", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Tts_MainVolume_Hint}Used only by the main playback path for normal conversation/test speech (agentIndex<0). Adjust Lip-sync channel volume for scene shouts.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public float TtsVolcDedicatedVolume { get; set; } = 1f;

	[SettingPropertyBool("{=AF_MCM_Tts_SceneWinmm}Use winmm for scene speech (echo prevention)", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Tts_SceneWinmm_Hint}On: scene NPCs speak through winmm while the lip-sync channel only drives mouth movement and stays muted. Off: scene NPCs speak through the lip-sync channel; adjust Lip-sync channel volume.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public bool TtsSceneUseWinmmAudible { get; set; } = true;

	[SettingPropertyFloatingInteger("{=AF_MCM_Tts_Speed}Volcengine dedicated speech speed", 0.1f, 2f, "0.00", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Tts_Speed_Hint}V1 supports 0.1–2.0 times normal; V3 supports 0.5–2.0 (1.0 is normal). A value that is too low after switching to V3 produces an error and is not changed automatically.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public float TtsVolcDedicatedSpeed { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=AF_MCM_Tts_LipSyncVolume}Lip-sync channel volume", 0f, 1f, "0.00", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Tts_LipSyncVolume_Hint}SoundEvent volume for Rhubarb lip-sync. Used as the audible scene volume only when Use winmm for scene speech is off.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public float TtsLipSyncSoundEventVolume { get; set; } = 0f;

	[SettingPropertyButton("{=AF_MCM_Tts_Test}Test voice", -1, true, "", Content = "{=AF_MCM_Tts_TestContent}Play test", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Tts_Test_Hint}Tests the fixed phrase “At your service, traveler!” using the current API URL and credential. Real synthesis may incur charges.")]
	[SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Volcengine TTS (dedicated)")]
	public Action TestTtsVolcDedicatedVoice { get; set; }

	[SettingPropertyInteger("{=AF_MCM_Marriage_MaxAge}Maximum marriage candidate age", 18, 80, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Marriage_MaxAge_Hint}Filters the family marriage candidate list. Default: 55.")]
	[SettingPropertyGroup("{=AF_MCM_Marriage_Group}8. Marriage rules")]
	public int MarriageCandidateMaxAge { get; set; } = 55;

	[SettingPropertyInteger("{=AF_MCM_Marriage_MaxAgeGap}Maximum age gap from player", 0, 60, "0", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Marriage_MaxAgeGap_Hint}Filters the family marriage candidate list. Default: 25.")]
	[SettingPropertyGroup("{=AF_MCM_Marriage_Group}8. Marriage rules")]
	public int MarriageCandidateMaxAgeGap { get; set; } = 25;

	[SettingPropertyBool("{=AF_MCM_Marriage_OppositeGender}Opposite-gender-only marriage list (legacy compatibility)", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Marriage_OppositeGender_Hint}Kept for legacy configuration compatibility; the current marriage system allows same-gender marriage. Default: off.")]
	[SettingPropertyGroup("{=AF_MCM_Marriage_Group}8. Marriage rules")]
	public bool MarriageRequireOppositeGender { get; set; } = false;

	private string _playerCustomPromptRule = LoadPlayerCustomPromptRuleFromDiskOrDefault();

	public string PlayerCustomPromptRule
	{
		get => _playerCustomPromptRule;
		set => _playerCustomPromptRule = NormalizePlayerCustomPromptRuleText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_PlayerRule_Title}Edit player custom rule text", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Editor_PlayerRule_Hint}You can enter more than the 512-character MCM text-box limit; leave it blank to inject nothing.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditPlayerCustomPromptRule { get; set; }

	private string _kingdomRebellionSystemPrompt = LoadKingdomRebellionSystemPromptFromDiskOrDefault();

	public string KingdomRebellionSystemPrompt
	{
		get => _kingdomRebellionSystemPrompt;
		set => _kingdomRebellionSystemPrompt = NormalizeKingdomRebellionSystemPromptText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_Rebellion_Title}Edit kingdom rebellion system prompt", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Editor_Rebellion_Hint}Open the long-text editor to save the complete system prompt used to name newly founded kingdoms after rebellions. The built-in default may be shortened or rewritten.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditKingdomRebellionSystemPrompt { get; set; }

	private string _weeklyReportWritingRequirements = LoadWeeklyReportWritingRequirementsFromDiskOrDefault();

	public string WeeklyReportWritingRequirements
	{
		get => _weeklyReportWritingRequirements;
		set => _weeklyReportWritingRequirements = NormalizeWeeklyReportWritingRequirementsText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_Weekly_Title}Edit weekly-report writing requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Editor_Weekly_Hint}The default text is built into the code; you can edit or clear it. Output format, REPORT_BLOCK, TAGS, and stability tags remain controlled by built-in rules.")]
	[SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Weekly}Weekly reports", GroupOrder = 0)]
	public Action EditWeeklyReportWritingRequirements { get; set; }

	private string _npcPersonaGenerationRequirements = LoadNpcPersonaGenerationRequirementsFromDiskOrDefault();

	public string NpcPersonaGenerationRequirements
	{
		get => _npcPersonaGenerationRequirements;
		set => _npcPersonaGenerationRequirements = NormalizeNpcPersonaGenerationRequirementsText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_Persona_Title}Edit NPC personality/background requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Editor_Persona_Hint}You can change length, style, source selection, and regeneration de-duplication. Saving blank removes the extra requirements.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditNpcPersonaGenerationRequirements { get; set; }

	private string _dailyMemoryCompressionWritingRequirements = LoadDailyMemoryCompressionWritingRequirementsFromDiskOrDefault();

	public string DailyMemoryCompressionWritingRequirements
	{
		get => _dailyMemoryCompressionWritingRequirements;
		set => _dailyMemoryCompressionWritingRequirements = NormalizeDailyMemoryCompressionWritingRequirementsText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_DailyMemory_Title}Edit daily-memory compression requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Editor_DailyMemory_Hint}Edit only the style, information selection, and organization of the daily summary here. Tag structure, visibility, date and location, AFEF, identity facts, and length rules are fixed by the mod.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditDailyMemoryCompressionWritingRequirements { get; set; }

	private string _memoryOverviewCompressionWritingRequirements = LoadMemoryOverviewCompressionWritingRequirementsFromDiskOrDefault();

	public string MemoryOverviewCompressionWritingRequirements
	{
		get => _memoryOverviewCompressionWritingRequirements;
		set => _memoryOverviewCompressionWritingRequirements = NormalizeMemoryOverviewCompressionWritingRequirementsText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_MemoryOverview_Title}Edit memory-overview compression requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Editor_MemoryOverview_Hint}The SUMMARY tag, dates and locations, fact boundaries, identity rules, and length range are fixed by the mod; conflicting text cannot override hard rules. Leave blank to add no writing preference.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditMemoryOverviewCompressionWritingRequirements { get; set; }

	private string _majorActionCompressionWritingRequirements = LoadMajorActionCompressionWritingRequirementsFromDiskOrDefault();

	public string MajorActionCompressionWritingRequirements
	{
		get => _majorActionCompressionWritingRequirements;
		set => _majorActionCompressionWritingRequirements = NormalizeMajorActionCompressionWritingRequirementsText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Editor_MajorAction_Title}Edit major-action compression requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Editor_MajorAction_Hint}The SUMMARY tag, dates and fact boundaries, rolling-merge behavior, and length range are fixed by the mod; conflicting text cannot override hard rules. Leave blank to add no writing preference.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action EditMajorActionCompressionWritingRequirements { get; set; }

	private string _customPolicyEvaluatorPrompt = LoadCustomPolicyEvaluatorPromptFromDiskOrDefault();

	public string CustomPolicyEvaluatorPrompt
	{
		get => _customPolicyEvaluatorPrompt;
		set => _customPolicyEvaluatorPrompt = NormalizeCustomPolicyEvaluatorPromptText(value);
	}

	public Action EditCustomPolicyEvaluatorPrompt { get; set; }

	private string _npcRulerPolicyPrompt = LoadNpcRulerPolicyPromptFromDiskOrDefault();

	public string NpcRulerPolicyPrompt
	{
		get => _npcRulerPolicyPrompt;
		set => _npcRulerPolicyPrompt = NormalizeNpcRulerPolicyPromptText(value);
	}

	public Action EditNpcRulerPolicyPrompt { get; set; }

	[SettingPropertyButton("{=AF_MCM_Policy_EditPrompts_Title}Policy prompt manager", -1, true, "", Content = "{=AF_MCM_Common_OpenManager}Open manager", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Policy_EditPrompts_Hint}Edit player AI drafting, player-policy evaluation, NPC ruler policy, and the interpretation and evaluation requirements for currently available policy effects in one place.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Group}16. Policy system", GroupOrder = 0)]
	public Action EditPolicyPrompts { get; set; }

	[SettingPropertyButton("{=AF_MCM_Policy_ManageEffects_Title}Policy effect module manager", -1, true, "", Content = "{=AF_MCM_Common_OpenManager}Open manager", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Policy_ManageEffects_Hint}Choose which effect modules can be retrieved for player, local, NPC ruler, and vassal policies. Changes apply only to policies started after saving.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Group}16. Policy system", GroupOrder = 0)]
	public Action ManagePolicyEffectModules { get; set; }

	[SettingPropertyButton("{=AF_MCM_Policy_SettlementBalance_Title}Policy-related settlement caps", -1, true, "", Content = "{=AF_MCM_Policy_AdjustLimits}Adjust caps and capacity", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Policy_SettlementBalance_Hint}Open a separate window to set town/castle prosperity, village hearths, town/castle/village militia, and shared town/castle granary capacity. Applies to all player and NPC settlements; no policy publication is required.\nThese are total caps, not daily gains. Each day uses the native plus policy net change: positive growth stops at the cap, while zero or negative change is unrestricted. Example: with a cap of 10000, 9999 plus 100 reaches 10000; an existing 12000 is not reduced, positive growth pauses, and negative change proceeds normally.\nOnly granary capacity is enabled by default; town and castle final capacity is 1000, without castle or building bonuses. The other six options are disabled by default. Prosperity food consumption is controlled separately by the Disable prosperity food consumption switch and remains native by default.\nEach option can be toggled independently and supports integer sliders and numeric input. Changes apply after saving; Cancel or Esc discards edits; restoring defaults still requires saving.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Group}16. Policy system", GroupOrder = 0)]
	public Action EditSettlementBalance { get; set; }

	[SettingPropertyInteger("{=AF_MCM_Policy_RecentActive_Title}Recent active domestic policies referenced by AI", 0, 10, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Policy_RecentActive_Hint}When evaluating player policies or generating NPC policies, the maximum number of the kingdom's latest active policies AI may reference. Reads newest to oldest regardless of publisher; default: 2. Used to detect duplicates, conflicts, and continuation; it is not a cap on policies that can take effect and does not change existing effects. 0 disables this reference set; fewer policies are not padded; ordinary dialogue is unaffected.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Group}16. Policy system", GroupOrder = 0)]
	public int PolicyRecentActiveCount { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_Policy_RelatedActive_Title}Related active world policies referenced by AI", 0, 10, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Policy_RelatedActive_Hint}In addition to the latest domestic policies above, the maximum number of active policies related to the current policy that AI may reference when evaluating player policies or generating NPC policies. Candidates come from all kingdoms, including the player's, and exclude policies already selected above; the most related are chosen with no minimum similarity threshold. Default: 3, so both sets reference at most 2 plus 3 = 5 policies; this is not a cap on policies that can take effect. 0 disables this set; insufficient candidates are not padded; ordinary dialogue and existing effects are unaffected.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Group}16. Policy system", GroupOrder = 0)]
	public int PolicyRelatedActiveCount { get; set; } = 3;

	[SettingPropertyButton("{=AF_MCM_Prompt_Folder}Custom prompt JSON folder", -1, true, "", Content = "{=AF_MCM_Prompt_OpenFolder}Open folder", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Prompt_Folder_Hint}Open the CustomPrompts folder to edit prompt JSON files directly.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public Action OpenCustomPromptTextStoreFolderAction { get; set; }

	[SettingPropertyBool("{=AF_MCM_Prompt_KeepSceneNarration}Keep scene-shout actions and inner thoughts", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Prompt_KeepSceneNarration_Hint}When disabled, detailed action and inner-thought prompts are still used, but these descriptions are filtered from output. When enabled, actions and inner thoughts are retained.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public bool UseDetailedSceneSpeechPrompt { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Prompt_KeepAsteriskActions}Keep actions enclosed in asterisks", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Prompt_KeepAsteriskActions_Hint}Retain actions enclosed in **...** or *...*, even when keeping scene-shout actions and inner thoughts is disabled.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public bool PreserveSceneAsteriskActions { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Prompt_CustomSceneFormat}Custom format: omit default scene-reply prompt", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Prompt_CustomSceneFormat_Hint}Omit the default one-on-one identity restrictions and reply format, action, inner-thought, and length requirements. Provide a complete format in the player custom rules. Disabled by default.")]
	[SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
	public bool DisableBuiltInSceneReplyFormatPrompt { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Policy_UseAiCost}Evaluate policy cost with AI", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Policy_UseAiCost_Hint}When enabled, AI estimates the denar cost from policy scale and execution difficulty; insufficient denars proportionally reduce all policy effects. When disabled, use the fixed denar cost below.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public bool UseAiEvaluatedCustomPolicyCost { get; set; } = true;

	[SettingPropertyDropdown("{=AF_MCM_Policy_EvaluationApi}Policy evaluation API", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Policy_EvaluationApi_Hint}Reuses the selected channel’s URL, key, model, and endpoint rules; incomplete selections fall back to the main API. Explicitly selecting preprocessing is independent of the normal preprocessing switch.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public Dropdown<string> PlayerPolicyApiSourceDropdown { get; set; } = BuildPolicyApiSourceDropdown(0);

	[SettingPropertyDropdown("{=AF_MCM_Policy_AutoDraftApi}AI drafting API", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Policy_AutoDraftApi_Hint}Selects whether AI drafting uses the main, preprocessing, or postprocessing channel; preprocessing is the default. Incomplete selections fall back to the main API and do not affect policy evaluation.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public Dropdown<string> PlayerPolicyAutoDraftApiSourceDropdown { get; set; } = BuildPlayerPolicyAutoDraftApiSourceDropdown(1);

	[SettingPropertyInteger("{=AF_MCM_Policy_GoldCost}Fixed denar cost", 0, 500000, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Policy_GoldCost_Hint}Applies only when AI policy-cost evaluation is disabled. Denars are deducted when a player policy is published; default 50000, and 0 means free.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public int CustomPolicyGoldCost { get; set; } = DefaultCustomPolicyGoldCost;

	[Obsolete("Player policy influence cost is retained only for legacy settings compatibility and is no longer used.")]
	public int CustomPolicyInfluenceCost { get; set; }

	[SettingPropertyInteger("{=AF_MCM_Policy_FeedbackChars}Public feedback length", CustomPolicyPublicFeedbackTargetMinChars, CustomPolicyPublicFeedbackTargetMaxChars, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Policy_FeedbackChars_Hint}Target length for public feedback after a player policy is published. Default is about 900 characters; adjust from 100 to 1800. Actual length may vary slightly with model output.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public int CustomPolicyPublicFeedbackTargetChars { get; set; } = DefaultCustomPolicyPublicFeedbackTargetChars;

	[SettingPropertyBool("{=AF_MCM_Policy_FollowTokens}Follow selected API token limit", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Policy_FollowTokens_Hint}When enabled, use the maximum output tokens of the effective API source; if it falls back to the main API, use the main API token limit. When disabled, always use the custom policy value below.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public bool PlayerPolicyFollowSelectedApiTokens { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Policy_CustomTokens}Custom maximum output tokens", PolicyApiMaxTokensMinimum, PolicyApiMaxTokensMaximum, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Policy_CustomTokens_Hint}Used only when following the selected API token limit is disabled. Affects player policies only; range 1000–100000 and does not change other API global limits.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public int PlayerPolicyCustomMaxTokens { get; set; } = DefaultPolicyApiMaxTokens;

	[SettingPropertyInteger("{=AF_MCM_Policy_EffectTokens}Effect parsing maximum output tokens", PlayerPolicyEffectPostprocessMaxTokensMinimum, PlayerPolicyEffectPostprocessMaxTokensMaximum, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Policy_EffectTokens_Hint}Maximum output tokens for the player-policy second-stage effect JSON. Default 5000; the effective value never exceeds the current player-policy token limit above.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public int PlayerPolicyEffectPostprocessMaxTokens { get; set; } = DefaultPlayerPolicyEffectPostprocessMaxTokens;

	[SettingPropertyInteger("{=AF_MCM_Policy_EffectDetail}Effect module detail count", PlayerPolicyEffectModuleDetailCountMinimum, PlayerPolicyEffectModuleDetailCountMaximum, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Policy_EffectDetail_Hint}Legacy-compatible value range is 1–30, default 6; at most 8 detail modules enter stage two, and values above 8 execute as 8. This does not limit the ONNX candidate pool. Higher values use more prompt tokens.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public int PlayerPolicyEffectModuleDetailCount { get; set; } = DefaultPlayerPolicyEffectModuleDetailCount;

	[SettingPropertyBool("{=AF_MCM_Policy_VoteInterest}Let lord votes consider interest, relations, and personality", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Policy_VoteInterest_Hint}When enabled, kingdom agenda votes on custom policies also consider effects on the lord’s fiefs and clan, relations with the proposer, and whether policy methods match the lord personality. Settled canvassing promises retain priority.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public bool PolicyVoteInterestEnabled { get; set; } = true;

	[SettingPropertyFloatingInteger("{=AF_MCM_Policy_VoteInterestWeight}Vote interest weight", 0f, 3f, "0.00", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Policy_VoteInterestWeight_Hint}Multiplier for policy effects on the lord’s own interests. At 1.00, a clear fief benefit can offset ordinary dislike of crown policies; 0 ignores interest. Default: 1.00.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public float PolicyVoteInterestWeight { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=AF_MCM_Policy_VoteRelationWeight}Vote relation weight", 0f, 3f, "0.00", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Policy_VoteRelationWeight_Hint}Multiplier for the proposer–lord relation in vote direction. At 1.00, relation +100 is about a moderate benefit and -100 a moderate loss; 0 uses only the native relation influence. Default: 1.00.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public float PolicyVoteRelationWeight { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=AF_MCM_Policy_VotePersonalityWeight}Vote personality weight", 0f, 3f, "0.00", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Policy_VotePersonalityWeight_Hint}Multiplier for matching mercy, honor, generosity, valor, prudence, and policy methods. Default: 1; 0 disables personality scoring. Actual losses still count, and bad traits do not guarantee approval.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Player_Group}16. Policy system/1. Player policy", GroupOrder = 160)]
	public float PolicyVotePersonalityWeight { get; set; } = 1f;

	[SettingPropertyBool("{=AF_MCM_Policy_EnableNpcRuler}Enable NPC ruler policies", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Policy_EnableNpcRuler_Hint}When enabled, NPC kingdoms create and publish policies at the configured interval. When disabled, no new policies are generated; active policies and records remain unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public bool EnableNpcRulerPolicy { get; set; } = true;

	[SettingPropertyDropdown("{=AF_MCM_Policy_RulerApi}API source", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Policy_RulerApi_Hint}Reuses the selected channel’s URL, key, model, and endpoint rules; incomplete selections fall back to the main API. Explicit preprocessing selection is independent of the normal preprocessing switch.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public Dropdown<string> NpcRulerPolicyApiSourceDropdown { get; set; } = BuildPolicyApiSourceDropdown(3);

	[SettingPropertyInteger("{=AF_MCM_Policy_RulerInterval}Policy check interval (days)", NpcRulerPolicyCheckIntervalMinDays, NpcRulerPolicyCheckIntervalMaxDays, "0", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Policy_RulerInterval_Hint}How often to check whether a new NPC policy is needed. Default: 7 days; range 1–30. No policy is generated when no kingdom qualifies.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public int NpcRulerPolicyCheckIntervalDays { get; set; } = DefaultNpcRulerPolicyCheckIntervalDays;

	[Obsolete("NPC ruler policy generation is fixed to one policy per check.")]
	public int NpcRulerPolicyMaxKingdomsPerRequest { get; set; } = 1;

	[SettingPropertyInteger("{=AF_MCM_Policy_RulerCooldown}Same-kingdom policy cooldown (days)", NpcRulerPolicyIntervalMinDays, NpcRulerPolicyIntervalMaxDays, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Policy_RulerCooldown_Hint}Minimum game days between policy drafts for the same NPC kingdom. Default: 7, range 1–30; pending, approved, and rejected normal drafts all count. Player vassal policies are exempt but suppress the target kingdom’s NPC ruler policy for this duration.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public int NpcRulerPolicyIntervalDays { get; set; } = DefaultNpcRulerPolicyIntervalDays;

	[SettingPropertyBool("{=AF_MCM_Policy_RulerFollowTokens}Follow selected API token limit", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Policy_RulerFollowTokens_Hint}When enabled, use the effective API source maximum output tokens; if it falls back to the main API, use the main API limit. When disabled, always use the custom policy value below.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public bool NpcRulerPolicyFollowSelectedApiTokens { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Policy_RulerCustomTokens}Custom maximum output tokens", PolicyApiMaxTokensMinimum, PolicyApiMaxTokensMaximum, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Policy_RulerCustomTokens_Hint}Used only when following the selected API token limit is disabled. Affects NPC ruler policies only; range 1000–100000 and does not change other API global limits.")]
	[SettingPropertyGroup("{=AF_MCM_Policy_Ruler_Group}16. Policy system/2. NPC ruler policy", GroupOrder = 160)]
	public int NpcRulerPolicyCustomMaxTokens { get; set; } = DefaultPolicyApiMaxTokens;

	[Obsolete("Use NpcRulerPolicyIntervalDays / GetNpcRulerPolicyIntervalDaysForExternal instead.")]
	public int NpcRulerPolicyIntervalHours { get; set; } = DefaultNpcRulerPolicyIntervalHours;

	[SettingPropertyBool("{=AF_MCM_Diplomacy_Enable}Enable AI diplomacy", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_Enable_Hint}When enabled, kingdoms publish declarations, negotiate, and take diplomatic actions. Diplomacy and clerk drafting reuse the event/rebellion API and fall back to the main API when incomplete. When disabled, native kingdom diplomacy decisions resume.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Base_Group}17. AI diplomacy (testing)/1. Basics", GroupOrder = 170)]
	public bool EnableWorldDiplomacy { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Diplomacy_NewGamePeace}Peace at the start of a new game", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_NewGamePeace_Hint}Applies only to newly created saves and ends kingdom wars present at campaign start. Disabled by default; changing it does not reset wars in the current save.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Base_Group}17. AI diplomacy (testing)/1. Basics", GroupOrder = 170)]
	public bool WorldDiplomacyStartNewGameAtPeace { get; set; } = false;

	[SettingPropertyBool("{=AF_MCM_Diplomacy_MapNotice}Show declaration delivery notices", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_MapNotice_Hint}Delivered diplomatic declarations show a lower-left text notice instead of a right-side notification. Hiding notices does not affect negotiation, propagation, bulletin material, or kingdom announcements.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Base_Group}17. AI diplomacy (testing)/1. Basics", GroupOrder = 170)]
	public bool EnableWorldDiplomacyMapNotifications { get; set; } = true;

	[Obsolete("Legacy configuration compatibility only; ordinary event capacity and one opening per day replace the interval.")]
	public int WorldDiplomacyRoundIntervalDays { get; set; } = 3;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_MaxRounds}Maximum concurrent ordinary diplomacy events", 1, WorldDiplomacyMaxConcurrentOrdinaryRoundsMax, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_MaxRounds_Hint}Shared by ordinary automatic diplomacy and policy triggers; default 3. When full, policy work remains pending while existing talks finish. Lowering the limit does not cancel existing events. At most one opens per day; player diplomacy and verbal-promise events do not use this slot.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Pacing_Group}17. AI diplomacy (testing)/2. Event pacing", GroupOrder = 170)]
	public int WorldDiplomacyMaxConcurrentOrdinaryRounds { get; set; } = DefaultWorldDiplomacyMaxConcurrentOrdinaryRounds;

	[SettingPropertyDropdown("{=AF_MCM_Diplomacy_Activity}Countries per diplomacy event", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_Activity_Hint}Low allows up to 2 countries, medium up to 3, and high up to 5, including the initiator. Only countries with real diplomatic business are selected. Default: medium; this is per-event participation, not the concurrent-event count.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Pacing_Group}17. AI diplomacy (testing)/2. Event pacing", GroupOrder = 170)]
	public Dropdown<string> WorldDiplomacyActivityDropdown
	{
		get => _worldDiplomacyActivityDropdown;
		set => _worldDiplomacyActivityDropdown = BuildFixedDisplayDropdown(value, 1,
			"{=AF_MCM_Option_DiplomacySmall}Small", "{=AF_MCM_Option_DiplomacyMedium}Medium", "{=AF_MCM_Option_DiplomacyLarge}Large");
	}
	private Dropdown<string> _worldDiplomacyActivityDropdown = BuildFixedDisplayDropdown(null, 1,
		"{=AF_MCM_Option_DiplomacySmall}Small", "{=AF_MCM_Option_DiplomacyMedium}Medium", "{=AF_MCM_Option_DiplomacyLarge}Large");

	[Obsolete("Legacy configuration compatibility only; court and civilian delivery settings control propagation.")]
	public int WorldDiplomacyPropagationSpeedPercent { get; set; } = 100;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_CourtDays}Farthest court delivery (days)", 3, 14, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_CourtDays_Hint}Assigns court delivery time by distance. Controls the farthest court and the relay pace for a new event round; countries do not all wait this many days. Default: 7; lower values produce denser replies.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Propagation_Group}17. AI diplomacy (testing)/3. Propagation", GroupOrder = 170)]
	public int WorldDiplomacyCourtMaxDeliveryDays { get; set; } = 7;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_ContinentDays}Declaration spreads across the continent (days)", 7, 42, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_ContinentDays_Hint}Schedules when local nobles and civilians learn a declaration by distance. Default: 21 days; does not control automatic opening frequency or replace court delivery time.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Propagation_Group}17. AI diplomacy (testing)/3. Propagation", GroupOrder = 170)]
	public int WorldDiplomacyContinentSpreadDays { get; set; } = 21;

	[SettingPropertyDropdown("{=AF_MCM_Diplomacy_RoundLength}Diplomacy event target duration", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_RoundLength_Hint}Typical event closing time: compact about 15 days, standard about 21, and relaxed about 28. Results may end it early or briefly extend cleanup. Default: standard; affects new events only.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Pacing_Group}17. AI diplomacy (testing)/2. Event pacing", GroupOrder = 170)]
	public Dropdown<string> WorldDiplomacyRoundLengthDropdown
	{
		get => _worldDiplomacyRoundLengthDropdown;
		set => _worldDiplomacyRoundLengthDropdown = BuildFixedDisplayDropdown(value, 1,
			"{=AF_MCM_Option_DiplomacyCompact}Compact (about 15 days)", "{=AF_MCM_Option_DiplomacyStandard}Standard (about 21 days)", "{=AF_MCM_Option_DiplomacyRelaxed}Relaxed (about 28 days)");
	}
	private Dropdown<string> _worldDiplomacyRoundLengthDropdown = BuildFixedDisplayDropdown(null, 1,
		"{=AF_MCM_Option_DiplomacyCompact}Compact (about 15 days)", "{=AF_MCM_Option_DiplomacyStandard}Standard (about 21 days)", "{=AF_MCM_Option_DiplomacyRelaxed}Relaxed (about 28 days)");

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_DeclarationMin}Minimum AI declaration characters", WorldDiplomacyDeclarationCharactersMin, WorldDiplomacyDeclarationCharactersMax, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_DeclarationMin_Hint}Shared character range for AI declarations, diplomatic replies, and clerk drafting; punctuation counts. Default minimum 100, adjustable from 1 to 1000; does not limit player editing.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Declaration_Group}17. AI diplomacy (testing)/4. Declarations and drafting", GroupOrder = 170)]
	public int WorldDiplomacyDeclarationMinCharacters { get; set; } = DefaultWorldDiplomacyDeclarationMinCharacters;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_DeclarationMax}Maximum AI declaration characters", WorldDiplomacyDeclarationCharactersMin, WorldDiplomacyDeclarationCharactersMax, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_DeclarationMax_Hint}Shared character range for AI declarations, diplomatic replies, and clerk drafting; punctuation counts. Default maximum 500, adjustable from 1 to 1000; values below the minimum use the minimum.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Declaration_Group}17. AI diplomacy (testing)/4. Declarations and drafting", GroupOrder = 170)]
	public int WorldDiplomacyDeclarationMaxCharacters { get; set; } = DefaultWorldDiplomacyDeclarationMaxCharacters;

	// 仅保留用于兼容旧配置反序列化；自由外交流程不再读取或展示这些设置。
	[Obsolete("Legacy configuration compatibility only; free-form diplomacy no longer forces wars from pressure thresholds.")]
	public bool EnableWorldDiplomacyForcedWar { get; set; } = true;

	[Obsolete("Legacy configuration compatibility only; free-form diplomacy no longer uses a forced-war pressure threshold.")]
	public int WorldDiplomacyWarPressureThreshold { get; set; } = 120;

	[Obsolete("Legacy configuration compatibility only; free-form diplomacy no longer uses a forced-peace pressure threshold.")]
	public int WorldDiplomacyPeacePressureThreshold { get; set; } = 200;

	[Obsolete("Legacy configuration compatibility only; native diplomacy signals use their fixed semantic weight.")]
	public int WorldDiplomacyNativeIntentInfluencePercent { get; set; } = 100;

	[Obsolete("Legacy configuration compatibility only; published declarations use their fixed semantic weight.")]
	public int WorldDiplomacyDocumentInfluencePercent { get; set; } = 100;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_WarCooldown}Proactive-war cooldown (days)", 7, 120, "0", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_WarCooldown_Hint}After a kingdom starts a proactive war, wait at least this many days before starting another. Default: 42; does not limit other diplomatic declarations.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Protection_Group}17. AI diplomacy (testing)/5. War and proposal protection", GroupOrder = 170)]
	public int WorldDiplomacyOffensiveWarCooldownDays { get; set; } = 42;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_PeaceProtection}Peace protection period (days)", 0, 60, "0", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_PeaceProtection_Hint}After two kingdoms make peace, ordinary diplomatic disputes cannot start another war during this period. Default: 21; 0 disables this protection but does not change other war-eligibility rules.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Protection_Group}17. AI diplomacy (testing)/5. War and proposal protection", GroupOrder = 170)]
	public int WorldDiplomacyPeaceProtectionDays { get; set; } = 21;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_ProposalCooldown}Trade/alliance failed-proposal cooldown (days)", 0, 672, "0", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_ProposalCooldown_Hint}After a trade or alliance proposal fails, the same issuer cannot propose the same type to the same target during this period. Reverse proposals and other types are tracked separately. Default: 168; 0 disables.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Protection_Group}17. AI diplomacy (testing)/5. War and proposal protection", GroupOrder = 170)]
	public int WorldDiplomacyTradeAllianceFailedProposalCooldownDays { get; set; } = 168;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_ThreatReward}Threat compliance relation reward", WorldDiplomacyThreatComplianceIssuerRelationRewardMin, WorldDiplomacyThreatComplianceIssuerRelationRewardMax, "0", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_ThreatReward_Hint}After the target kingdom explicitly complies with a condemnation or ultimatum, increase relation between the issuer’s eligible clan leader and the current king. Settled once per threat. Default: 10; 0 disables.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Protection_Group}17. AI diplomacy (testing)/5. War and proposal protection", GroupOrder = 170)]
	public int WorldDiplomacyThreatComplianceIssuerRelationReward { get; set; } = DefaultWorldDiplomacyThreatComplianceIssuerRelationReward;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_CompressTrigger}Archive compression trigger (thousand estimated tokens)", WorldDiplomacyHistoryCompressionTriggerThousandsMin, WorldDiplomacyHistoryCompressionTriggerThousandsMax, "0", Order = 15, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_CompressTrigger_Hint}Start batched compression when the long-term diplomacy archive reaches this estimated size. Default: 800 (about 800k tokens); controls backlog and frequency, not history carried by each declaration. Total declaration input has a separate 32k estimated-token cap, and each compression batch has a separate 128k input cap.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Archive_Group}17. AI diplomacy (testing)/6. Long-term archive (advanced)", GroupOrder = 170)]
	public int WorldDiplomacyHistoryCompressionTriggerThousands { get; set; } = DefaultWorldDiplomacyHistoryCompressionTriggerThousands;

	[SettingPropertyInteger("{=AF_MCM_Diplomacy_CompressTarget}Archive compression target (thousand estimated tokens)", WorldDiplomacyHistoryCompressionTargetThousandsMin, WorldDiplomacyHistoryCompressionTargetThousandsMax, "0", Order = 16, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_CompressTarget_Hint}Target combined size of the long-term snapshot and protected facts after each batch. Default: 48 (about 48k tokens), maximum 60k; declarations use only relevant material. Actual summaries also follow the selected API output-token limit.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Archive_Group}17. AI diplomacy (testing)/6. Long-term archive (advanced)", GroupOrder = 170)]
	public int WorldDiplomacyHistoryCompressionTargetThousands { get; set; } = DefaultWorldDiplomacyHistoryCompressionTargetThousands;

	// 仅保留用于兼容旧 MCM 配置反序列化；新的长期记忆不再按累计 API Tokens 触发。
	public int WorldDiplomacyCompressionThresholdTenThousands { get; set; } = 50;

	private string _worldDiplomacyPrompt = LoadWorldDiplomacyPromptFromDiskOrDefault();

	public string WorldDiplomacyPrompt
	{
		get => _worldDiplomacyPrompt;
		set => _worldDiplomacyPrompt = NormalizeWorldDiplomacyPromptText(value);
	}

	[SettingPropertyButton("{=AF_MCM_Diplomacy_Edit}Edit AI diplomacy and clerk-drafting preferences", -1, true, "", Content = "{=AF_MCM_Diplomacy_Edit_Content}Open editor", Order = 17, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_Edit_Hint}Edit diplomacy decision and writing preferences. Clerk drafting expands the player’s existing content faithfully without adding substantive clauses. Blank adds no preference; fixed facts and output rules remain enforced.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Declaration_Group}17. AI diplomacy (testing)/4. Declarations and drafting", GroupOrder = 170)]
	public Action EditWorldDiplomacyPrompt { get; set; }

	[SettingPropertyButton("{=AF_MCM_Diplomacy_Restore}Restore default diplomacy and drafting preferences", -1, true, "", Content = "{=AF_MCM_Diplomacy_Restore_Content}Restore default", Order = 18, RequireRestart = false, HintText = "{=AF_MCM_Diplomacy_Restore_Hint}Restores the mod default preference text. Only preference text is restored; event limits, propagation, length, and protection settings on this page are unchanged.")]
	[SettingPropertyGroup("{=AF_MCM_Diplomacy_Declaration_Group}17. AI diplomacy (testing)/4. Declarations and drafting", GroupOrder = 170)]
	public Action RestoreDefaultWorldDiplomacyPrompt { get; set; }

	[SettingPropertyBool("{=AF_MCM_Gathering_Enable}Enable noble gatherings", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Gathering_Enable_Hint}Master switch for the gathering system. Disabling stops new gatherings; active gatherings are cancelled at the next safe campaign hour and hosts and guests are sent home. No restart required.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public bool EnableNobleGathering { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Gathering_AutoNpc}Enable automatic NPC gatherings", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_Gathering_AutoNpc_Hint}When enabled, eligible NPCs schedule gatherings by kingdom. Disabling stops new NPC gatherings, while players can still host gatherings manually.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public bool EnableNpcAutomaticNobleGatherings { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Gathering_PlayerInvite}Allow NPC invitations to the player", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Gathering_PlayerInvite_Hint}When enabled, NPC gatherings may invite the player and send a courier. Disabling prevents new invitations only; invitations already sent remain valid.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public bool AllowNpcNobleGatheringPlayerInvitations { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Gathering_ArrivalNotice}Show guest-arrival notices", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Gathering_ArrivalNotice_Hint}Controls the lower-left colored message when guests arrive at a player gathering. Hiding it does not affect arrival checks, gathering state, or rewards.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public bool ShowNobleGatheringGuestArrivalMessages { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_Gathering_NpcInterval}NPC gathering interval (days)", NobleGatheringNpcIntervalMinDays, NobleGatheringNpcIntervalMaxDays, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Gathering_NpcInterval_Hint}Game days between automatic gathering schedules in the same kingdom. Default: 21; calculated per kingdom and used for the next schedule after editing.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public int NpcNobleGatheringIntervalDays { get; set; } = DefaultNobleGatheringNpcIntervalDays;

	[SettingPropertyBool("{=AF_MCM_Gathering_GovernorInvite}Allow NPC invitations to governors", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Gathering_GovernorInvite_Hint}Affects only automatically generated NPC guest lists. When disabled, NPCs do not invite governors governing settlements; players may still invite governors manually.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public bool AllowNpcNobleGatheringGovernorInvitations { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Gathering_Duration}Gathering duration (days)", NobleGatheringDurationMinDays, NobleGatheringDurationMaxDays, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Gathering_Duration_Hint}Shared by player and NPC gatherings. Default: 5 days. Editing affects newly created gatherings only; active gatherings keep their original end date.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public int NobleGatheringDurationDays { get; set; } = DefaultNobleGatheringDurationDays;

	[SettingPropertyInteger("{=AF_MCM_Gathering_Cost}Gathering cost", NobleGatheringCostMinimum, NobleGatheringCostMaximum, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Gathering_Cost_Hint}Shared by player and NPC hosts; deducted from the actual host. Default: 50000 denars; 0 means free.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public int NobleGatheringCost { get; set; } = DefaultNobleGatheringCost;

	[SettingPropertyInteger("{=AF_MCM_Gathering_RelationReward}Relation reward per invited clan", NobleGatheringInvitedClanRelationRewardMinimum, NobleGatheringInvitedClanRelationRewardMaximum, "0", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Gathering_RelationReward_Hint}Shared by player and NPC gatherings; the host and each invited clan leader are settled once per gathering. Default: 5; 0 gives no relation. Affects newly created gatherings only.")]
	[SettingPropertyGroup("{=AF_MCM_Gathering_Group}18. Noble gatherings", GroupOrder = 180)]
	public int NobleGatheringInvitedClanRelationReward { get; set; } = DefaultNobleGatheringInvitedClanRelationReward;

	[SettingPropertyInteger("{=AF_MCM_News_WeeklyLength}Weekly report length preset", 1, 4, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_News_WeeklyLength_Hint}1 = 200–400 characters; 2 = 200–800; 3 = 200–1200; 4 = 200–1500. Shared by world and kingdom weekly reports. Default: 2.")]
	[SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Weekly}Weekly reports", GroupOrder = 0)]
	public int WeeklyReportLengthPreset { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_News_WeeklyRpm}Maximum weekly reports per minute", 1, 20, "0", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_News_WeeklyRpm_Hint}Limits request rate for development-mode weekly reports. Default: 5; maximum 20. Helps with API RPM or concurrency limits.")]
	[SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Weekly}Weekly reports", GroupOrder = 0)]
	public int WeeklyReportRequestsPerMinute { get; set; } = 5;

	[SettingPropertyBool("{=AF_MCM_News_AutoWeekly}Generate weekly reports automatically", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_News_AutoWeekly_Hint}At the start of each new week, settle the previous week and generate the world and kingdom weekly reports. Day 0 writes the opening summary as week 0.")]
	[SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Weekly}Weekly reports", GroupOrder = 0)]
	public bool AutoGenerateWeeklyReports { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_News_PopupFont}Report popup body font size", 12, 36, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_News_PopupFont_Hint}Affects only the large popup body for the latest kingdom weekly report. Default: 18.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public int WeeklyReportPopupBodyFontSize { get; set; } = 18;

	[SettingPropertyBool("{=AF_MCM_News_MapNotice}Weekly/bulletin map notice", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_News_MapNotice_Hint}When enabled, a clickable notice appears at the lower right of the campaign map after a report is generated. Disabled reports do not queue notices.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public bool EnableWeeklyReportMapNotifications { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_News_ReadingXp}Enable report-reading experience", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_News_ReadingXp_Hint}Opening a report from a map notice and staying for 10 seconds accumulates experience by the three-column character count; every 20 reports grant Charm, Leadership, and Steward experience.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public bool EnableWeeklyReportReadingXpReward { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_News_XpPerHundred}Experience per hundred report characters", 0, 100, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_News_XpPerHundred_Hint}Character multiplier for report-reading rewards. Default: 20; each 100 valid characters/words in a column gives 20 points of the corresponding skill experience.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public int WeeklyReportReadingXpPerHundredChars { get; set; } = 20;

	[SettingPropertyInteger("{=AF_MCM_News_XpCap}Per-skill report experience cap", 0, 500, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_News_XpCap_Hint}Maximum experience each skill gains from one report. Default: 100; 0 disables report-reading experience.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public int WeeklyReportReadingXpSkillCap { get; set; } = 100;

	[SettingPropertyBool("{=AF_MCM_News_StabilityRebellion}Enable kingdom stability and rebellion", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_News_StabilityRebellion_Hint}When disabled, this mod no longer triggers kingdom rebellions; kingdom stability no longer affects ruler fief loyalty or applies stability relation modifiers.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public bool EnableKingdomStabilityAndRebellion { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_News_PlayerImmunity}Player-as-ruler stability rebellion immunity", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_News_PlayerImmunity_Hint}When enabled, if the player clan rules a kingdom or the player is its leader, this mod stops applying stability relation, ruler-fief loyalty, and kingdom-rebellion checks to that kingdom. Native low-loyalty settlement rebellions still follow native rules.")]
	[SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
	public bool EnablePlayerKingdomRebellionImmunity { get; set; } = false;

	// Legacy weekly batches only run when the bulletin mode is off.
	internal bool IsLegacyWeeklyAutoGenerationActive()
	{
		return AutoGenerateWeeklyReports && !UseWorldBulletin;
	}

	[SettingPropertyBool("{=gccz_npc_response_unlimited_name}NPC Response Unlimited", Order = 0, RequireRestart = false, HintText = "{=gccz_npc_response_unlimited_hint}Removes the configured numeric cap in active GCCZ town scenes while retaining event deduplication and queue safety.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public bool GcczNpcResponseUnlimited { get; set; } = SiegeNpcResponseLimitProfile.DefaultUnlimited;

	[SettingPropertyInteger("{=gccz_npc_response_limit_name}NPC Response Limit", SiegeNpcResponseLimitProfile.MinResponseLimit, SiegeNpcResponseLimitProfile.MaxResponseLimit, "0", Order = 1, RequireRestart = false, HintText = "{=gccz_npc_response_limit_hint}Limits ambient NPC replies per player utterance, semantic action, or player attack event in an active GCCZ town scene.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public int GcczNpcResponseLimit { get; set; } = SiegeNpcResponseLimitProfile.DefaultResponseLimit;

	[SettingPropertyBool("{=AF_MCM_Gccz_EnableLog}Write GCCZ log", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_Gccz_EnableLog_Hint}Writes the separate GCCZ_Debug.log. Disabling stops new log entries but still allows exporting or clearing the existing log.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public bool EnableGcczDiagnosticLog { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Gccz_VerboseLog}GCCZ verbose log", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_Gccz_VerboseLog_Hint}Records high-frequency diagnostics such as NPC response limits. Enable while troubleshooting; phase changes, outcomes, and exceptions remain logged when disabled.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public bool EnableGcczVerboseDiagnosticLog { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Gccz_LogLimit}GCCZ log size limit (MB)", FeatureDiagnosticLogMinSizeMegabytes, FeatureDiagnosticLogMaxSizeMegabytes, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_Gccz_LogLimit_Hint}When GCCZ_Debug.log reaches the limit it rotates automatically and keeps GCCZ_Debug.previous.log. Default: 16 MB.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public int GcczDiagnosticLogMaxSizeMegabytes { get; set; } = FeatureDiagnosticLogDefaultSizeMegabytes;

	[SettingPropertyButton("{=AF_MCM_Gccz_Export}Export GCCZ_Debug.log", -1, true, "", Content = "{=AF_MCM_Gccz_Export_Content}Export to desktop", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_Gccz_Export_Hint}Copies GCCZ_Debug.log from the module Logs folder to the desktop with a timestamped file name.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ExportGcczDebugLog { get; set; }

	[SettingPropertyButton("{=AF_MCM_Gccz_Clear}Clear GCCZ_Debug.log", -1, true, "", Content = "{=AF_MCM_Gccz_Clear_Content}Clear now", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_Gccz_Clear_Hint}Clears only the current GCCZ_Debug.log and does not delete the previous rotated log.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ClearGcczDebugLog { get; set; }

	[SettingPropertyBool("{=AF_MCM_Sets_EnableLog}Write SETS log", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_Sets_EnableLog_Hint}Writes the separate SETS.log for settlement-entry escort and internal riot diagnostics. Disabling stops new entries but still allows exporting or clearing the existing log.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public bool EnableSetsDiagnosticLog { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_Sets_VerboseLog}SETS verbose log", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_Sets_VerboseLog_Hint}Records high-frequency diagnostics such as per-frame guards, command refreshes, and spawn progress. Enable while troubleshooting; conflict start, waves, victory, and exceptions remain logged when disabled.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public bool EnableSetsVerboseDiagnosticLog { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_Sets_LogLimit}SETS log size limit (MB)", FeatureDiagnosticLogMinSizeMegabytes, FeatureDiagnosticLogMaxSizeMegabytes, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_Sets_LogLimit_Hint}When SETS.log reaches the limit it rotates automatically and keeps SETS.previous.log. Default: 16 MB.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public int SetsDiagnosticLogMaxSizeMegabytes { get; set; } = FeatureDiagnosticLogDefaultSizeMegabytes;

	[SettingPropertyButton("{=AF_MCM_Sets_Export}Export SETS.log", -1, true, "", Content = "{=AF_MCM_Sets_Export_Content}Export to desktop", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_Sets_Export_Hint}Copies SETS.log from the module Logs folder to the desktop with a timestamped file name.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ExportSetsDiagnosticLog { get; set; }

	[SettingPropertyButton("{=AF_MCM_Sets_Clear}Clear SETS.log", -1, true, "", Content = "{=AF_MCM_Sets_Clear_Content}Clear now", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_Sets_Clear_Hint}Clears only the current SETS.log and does not delete the previous rotated log.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ClearSetsDiagnosticLog { get; set; }

	[SettingPropertyButton("{=AF_MCM_Escort_Export}Export noble-prisoner escort log", -1, true, "", Content = "{=AF_MCM_Escort_Export_Content}Export to desktop", Order = 12, RequireRestart = false, HintText = "{=AF_MCM_Escort_Export_Hint}Copies NoblePrisonerEscort.log to the desktop for troubleshooting prisoner spawning, scene executions, and wilderness-meeting retreats.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ExportNoblePrisonerEscortLog { get; set; }

	[SettingPropertyButton("{=AF_MCM_Escort_Clear}Clear noble-prisoner escort log", -1, true, "", Content = "{=AF_MCM_Escort_Clear_Content}Clear now", Order = 13, RequireRestart = false, HintText = "{=AF_MCM_Escort_Clear_Hint}Clears only the current NoblePrisonerEscort.log and does not delete the previous rotated log.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action ClearNoblePrisonerEscortLog { get; set; }

	[SettingPropertyInteger("{=AF_MCM_Sets_OtherFollowerLimit}Other-settlement escort limit", SetsSettlementEntryProfile.OtherSettlementSelectedFollowerMinLimit, SetsSettlementEntryProfile.OtherSettlementSelectedFollowerMaxLimit, "0", Order = 14, RequireRestart = false, HintText = "{=AF_MCM_Sets_OtherFollowerLimit_Hint}Maximum ordinary soldiers configured and brought when entering a town, castle, or village not owned by the player. Default: 10; lowering it trims excess saved configuration.")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public int SetsOtherSettlementFollowerLimit { get; set; } = SetsSettlementEntryProfile.OtherSettlementSelectedFollowerDefaultLimit;

	[SettingPropertyInteger("{=AF_MCM_Intimacy_PregnancyChance}Pregnancy chance (%)", 0, 100, "0", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Intimacy_PregnancyChance_Hint}After the intimacy action tag confirms intercourse and ejaculation occurred this round, the female partner pregnancy chance is applied. 0 means never; 100 means certain. Default: 50%.")]
	[SettingPropertyGroup("{=AF_MCM_Intimacy_Group}15. Intimacy and pregnancy")]
	public int IntimacyPregnancyChancePercent { get; set; } = 50;

	public bool UseMcmKnowledgeRetrieval { get; set; } = true;

	public bool KnowledgeRetrievalEnabled { get; set; } = true;

	public bool KnowledgeSemanticFirst { get; set; } = true;

	public int KnowledgeSemanticTopK { get; set; } = 4;

	public static bool IsBuiltInSceneReplyFormatPromptDisabled()
	{
		try
		{
			return GetSettings()?.DisableBuiltInSceneReplyFormatPrompt ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static DuelSettings GetSettings()
	{
		if (GlobalSettings<DuelSettings>.Instance != null)
		{
			DuelSettings settings = GlobalSettings<DuelSettings>.Instance;
			EnsureAdultHeroPersonaDefaultMigration(settings);
			EnsureCustomPromptTextSettingsLoaded(settings);
			EnsureLogCleanupDefaultMigration(settings);
			EnsureDuelRenownDefaultMigration(settings);
			EnsureWorldDiplomacyCompressionDefaultsMigration(settings);
			return settings;
		}
		try
		{
			if (BaseSettingsProvider.Instance?.GetSettings("AnimusForge_global_settings") is DuelSettings result)
			{
				EnsureAdultHeroPersonaDefaultMigration(result);
				EnsureCustomPromptTextSettingsLoaded(result);
				EnsureLogCleanupDefaultMigration(result);
				EnsureDuelRenownDefaultMigration(result);
				EnsureWorldDiplomacyCompressionDefaultsMigration(result);
				return result;
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("DuelSettings", "[WARN] Failed to read MCM settings from BaseSettingsProvider: " + ex.Message);
			}
			catch
			{
			}
		}
		if (_fallbackSettings == null)
		{
			_fallbackSettings = new DuelSettings();
		}
		EnsureCustomPromptTextSettingsLoaded(_fallbackSettings);
		if (!_settingsFallbackWarned)
		{
			_settingsFallbackWarned = true;
			try
			{
				Logger.Log("DuelSettings", "[WARN] MCM Instance is null; falling back to default settings.");
			}
			catch
			{
			}
		}
		return _fallbackSettings;
	}

	private static void EnsureAdultHeroPersonaDefaultMigration(DuelSettings settings)
	{
		if (settings == null || _adultHeroPersonaDefaultMigrationChecked) return;
		// Guard before saving: provider callbacks can re-enter GetSettings. A failed
		// migration is retried on the next launch, never on every settings read.
		_adultHeroPersonaDefaultMigrationChecked = true;
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(".adult_hero_persona_off_20261007");
			if (File.Exists(markerPath)) return;
			settings.EnableAdultHeroPersonaAutoGeneration = false;
			var provider = BaseSettingsProvider.Instance;
			if (provider == null) return;
			provider.SaveSettings(settings);
			string directory = Path.GetDirectoryName(markerPath);
			if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
			File.WriteAllText(markerPath, "adult-hero-persona-off-20261007", Encoding.UTF8);
			Logger.Log("DuelSettings", "Version migration: disabled automatic personality and background generation for newly adult characters and saved the existing configuration. It can be re-enabled manually in MCM.");
		}
		catch (Exception ex)
		{
			settings.EnableAdultHeroPersonaAutoGeneration = false;
			Logger.Log("DuelSettings", "[WARN] Migration to disable automatic generation for newly adult characters is incomplete; disabled for this session and will retry on the next startup: " + ex.Message);
		}
	}

	private static void EnsureLogCleanupDefaultMigration(DuelSettings settings)
	{
		if (settings == null || _logCleanupDefaultMigrationChecked)
		{
			return;
		}
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(LogCleanupDefaultMigrationMarkerFileName);
			if (File.Exists(markerPath))
			{
				string marker = File.ReadAllText(markerPath, Encoding.UTF8).Trim();
				if (string.Equals(marker, LogCleanupDefaultMigrationId, StringComparison.Ordinal))
				{
					_logCleanupDefaultMigrationChecked = true;
					return;
				}
			}
			if (BaseSettingsProvider.Instance == null)
			{
				return;
			}
			settings.LogCleanupIntervalDropdown = BuildLogCleanupIntervalDropdown(LogCleanupEvery3Days);
			BaseSettingsProvider.Instance.SaveSettings(settings);
			string directoryName = Path.GetDirectoryName(markerPath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(markerPath, LogCleanupDefaultMigrationId, Encoding.UTF8);
			_logCleanupDefaultMigrationChecked = true;
			Logger.Log("DuelSettings", "Version migration: forced the log cleanup interval to every 3 days.");
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("DuelSettings", "[WARN] Failed to migrate the log cleanup interval: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void EnsureDuelRenownDefaultMigration(DuelSettings settings)
	{
		if (settings == null || _duelRenownDefaultMigrationChecked)
		{
			return;
		}
		try
		{
			if (!settings.EnableDuelLoserClanRenownPenalty
				|| settings.DuelLoserClanRenownPenaltyMinimum != LegacyDuelLoserClanRenownPenaltyMinimum
				|| settings.DuelLoserClanRenownPenaltyPercent != LegacyDuelLoserClanRenownPenaltyPercent
				|| settings.DuelLoserClanRenownPenaltyMaximum != LegacyDuelLoserClanRenownPenaltyMaximum)
			{
				_duelRenownDefaultMigrationChecked = true;
				return;
			}
			if (BaseSettingsProvider.Instance == null)
			{
				return;
			}
			settings.DuelLoserClanRenownPenaltyMinimum = DefaultDuelLoserClanRenownPenaltyMinimum;
			settings.DuelLoserClanRenownPenaltyPercent = DefaultDuelLoserClanRenownPenaltyPercent;
			settings.DuelLoserClanRenownPenaltyMaximum = DefaultDuelLoserClanRenownPenaltyMaximum;
			BaseSettingsProvider.Instance.SaveSettings(settings);
			_duelRenownDefaultMigrationChecked = true;
			Logger.Log("DuelSettings", "Version migration: lowered unmodified duel clan renown transfer defaults to a minimum of 20, 3%, and a per-duel maximum of 150. Manual settings remain unchanged.");
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("DuelSettings", "[WARN] Failed to migrate duel clan renown transfer defaults: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void EnsureWorldDiplomacyCompressionDefaultsMigration(DuelSettings settings)
	{
		if (settings == null || _worldDiplomacyCompressionDefaultsMigrationChecked)
		{
			return;
		}
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(WorldDiplomacyCompressionDefaultsMigrationMarkerFileName);
			if (File.Exists(markerPath))
			{
				string marker = File.ReadAllText(markerPath, Encoding.UTF8).Trim();
				if (string.Equals(marker, WorldDiplomacyCompressionDefaultsMigrationId, StringComparison.Ordinal))
				{
					_worldDiplomacyCompressionDefaultsMigrationChecked = true;
					return;
				}
			}
			if (BaseSettingsProvider.Instance == null)
			{
				return;
			}

			bool changed = false;
			if (settings.WorldDiplomacyHistoryCompressionTargetThousands == LegacyWorldDiplomacyHistoryCompressionTargetThousands)
			{
				settings.WorldDiplomacyHistoryCompressionTargetThousands = DefaultWorldDiplomacyHistoryCompressionTargetThousands;
				changed = true;
			}
			if (settings.WorldDiplomacyHistoryCompressionTriggerThousands < WorldDiplomacyHistoryCompressionTriggerThousandsMin
				|| settings.WorldDiplomacyHistoryCompressionTriggerThousands > WorldDiplomacyHistoryCompressionTriggerThousandsMax)
			{
				settings.WorldDiplomacyHistoryCompressionTriggerThousands = DefaultWorldDiplomacyHistoryCompressionTriggerThousands;
				changed = true;
			}
			if (changed)
			{
				BaseSettingsProvider.Instance.SaveSettings(settings);
			}
			string directoryName = Path.GetDirectoryName(markerPath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(markerPath, WorldDiplomacyCompressionDefaultsMigrationId, Encoding.UTF8);
			_worldDiplomacyCompressionDefaultsMigrationChecked = true;
			Logger.Log("DuelSettings", changed
				? "Version migration: AI diplomacy long-term memory uses an 800k compression trigger and a 48k post-compression target; migrated the legacy 16k default while preserving other manual targets."
				: "Version migration: enabled separate compression trigger and post-compression target values for AI diplomacy long-term memory; existing manual settings remain unchanged.");
		}
		catch (Exception ex)
		{
			// Avoid retrying file I/O on every hot-path GetSettings call in this session.
			// A later process start can retry the one-time migration.
			_worldDiplomacyCompressionDefaultsMigrationChecked = true;
			try
			{
				Logger.Log("DuelSettings", "[WARN] Failed to migrate AI diplomacy long-term memory compression defaults: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	public static bool HasLiveMcmInstance()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance != null;
		}
		catch
		{
			return false;
		}
	}

	public static int GetCustomPolicyGoldCostForExternal()
	{
		try
		{
			return ClampCustomPolicyGoldCost(GetSettings()?.CustomPolicyGoldCost ?? DefaultCustomPolicyGoldCost);
		}
		catch
		{
			return DefaultCustomPolicyGoldCost;
		}
	}

	public static string GetPlayerPolicyApiSourceForExternal()
	{
		try
		{
			return ReadPolicyApiSourceSelection(GetSettings()?.PlayerPolicyApiSourceDropdown, 0);
		}
		catch
		{
			return PolicyApiSourceMain;
		}
	}

	public static string GetPlayerPolicyAutoDraftApiSourceForExternal()
	{
		try
		{
			return ReadPlayerPolicyAutoDraftApiSourceSelection(GetSettings()?.PlayerPolicyAutoDraftApiSourceDropdown);
		}
		catch
		{
			return PolicyApiSourceAuxiliary;
		}
	}

	public static string GetPlayerPolicyAutoDraftPromptForExternal()
	{
		return PolicyEffects.PolicyEffectPromptService.GetAutoDraftPrompt();
	}

	internal static void ResolvePolicyApiThinkingForExternal(
		string requestedSource,
		string resolvedRoute,
		out bool thinkingEnabled,
		out string reasoningEffort)
	{
		DuelSettings settings = GetSettings();
		string route = (resolvedRoute ?? string.Empty).Trim();
		string source = (requestedSource ?? string.Empty).Trim().ToLowerInvariant();
		if (string.Equals(route, "main", StringComparison.OrdinalIgnoreCase)
			|| route.EndsWith("_fallback_main", StringComparison.OrdinalIgnoreCase)
			|| route.EndsWith("_partial_fallback_main", StringComparison.OrdinalIgnoreCase))
		{
			source = PolicyApiSourceMain;
		}
		switch (source)
		{
		case PolicyApiSourceAuxiliary:
			thinkingEnabled = settings?.AuxiliaryApiThinkingEnabled ?? false;
			reasoningEffort = settings?.GetAuxiliaryApiReasoningEffort() ?? ReasoningEffortHigh;
			break;
		case PolicyApiSourceActionPostprocess:
			thinkingEnabled = settings?.ActionPostprocessApiThinkingEnabled ?? true;
			reasoningEffort = settings?.GetActionPostprocessApiReasoningEffort() ?? ReasoningEffortMax;
			break;
		case PolicyApiSourceEventAndRebellion:
			thinkingEnabled = settings?.EventAndRebellionApiThinkingEnabled ?? false;
			reasoningEffort = settings?.GetEventAndRebellionApiReasoningEffort() ?? ReasoningEffortHigh;
			break;
		default:
			thinkingEnabled = settings?.MainApiThinkingEnabled ?? true;
			reasoningEffort = settings?.GetMainApiReasoningEffort() ?? ReasoningEffortMax;
			break;
		}
	}

	public static bool GetPlayerPolicyFollowSelectedApiTokensForExternal()
	{
		try
		{
			return GetSettings()?.PlayerPolicyFollowSelectedApiTokens ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static int GetPlayerPolicyCustomMaxTokensForExternal()
	{
		try
		{
			return ClampPolicyApiMaxTokens(GetSettings()?.PlayerPolicyCustomMaxTokens ?? DefaultPolicyApiMaxTokens);
		}
		catch
		{
			return DefaultPolicyApiMaxTokens;
		}
	}

	public static int GetPlayerPolicyEffectPostprocessMaxTokensForExternal()
	{
		try
		{
			int value = GetSettings()?.PlayerPolicyEffectPostprocessMaxTokens ?? DefaultPlayerPolicyEffectPostprocessMaxTokens;
			return Math.Max(PlayerPolicyEffectPostprocessMaxTokensMinimum, Math.Min(PlayerPolicyEffectPostprocessMaxTokensMaximum, value));
		}
		catch
		{
			return DefaultPlayerPolicyEffectPostprocessMaxTokens;
		}
	}

	internal static int GetPolicyRecentActiveCount()
	{
		try { return PolicyHistoryRetrievalService.NormalizeEvaluationReferenceCount(GetSettings()?.PolicyRecentActiveCount ?? 2); }
		catch { return 2; }
	}

	internal static int GetPolicyRelatedActiveCount()
	{
		try { return PolicyHistoryRetrievalService.NormalizeEvaluationReferenceCount(GetSettings()?.PolicyRelatedActiveCount ?? 3); }
		catch { return 3; }
	}

	public static int GetPlayerPolicyEffectModuleDetailCountForExternal()
	{
		try
		{
			int value = GetSettings()?.PlayerPolicyEffectModuleDetailCount ?? DefaultPlayerPolicyEffectModuleDetailCount;
			return Math.Max(PlayerPolicyEffectModuleDetailCountMinimum, Math.Min(PlayerPolicyEffectModuleDetailCountMaximum, value));
		}
		catch
		{
			return DefaultPlayerPolicyEffectModuleDetailCount;
		}
	}

	public static int GetEffectivePlayerPolicyEffectModuleDetailCountForExternal()
	{
		return GetEffectivePlayerPolicyEffectModuleDetailCountForExternal(out _, out _);
	}

	public static int GetEffectivePlayerPolicyEffectModuleDetailCountForExternal(out int configuredValue, out bool wasClamped)
	{
		configuredValue = GetPlayerPolicyEffectModuleDetailCountForExternal();
		int effectiveValue = Math.Min(PlayerPolicyEffectModuleEffectiveDetailCountMaximum, configuredValue);
		wasClamped = effectiveValue != configuredValue;
		return effectiveValue;
	}

	public static bool TryConsumePlayerPolicyEffectModuleDetailCountClampNoticeForExternal(out string message)
	{
		int effectiveValue = GetEffectivePlayerPolicyEffectModuleDetailCountForExternal(out int configuredValue, out bool wasClamped);
		if (!wasClamped
			|| System.Threading.Interlocked.Exchange(ref _playerPolicyEffectModuleDetailCountClampNoticeConsumed, 1) != 0)
		{
			message = "";
			return false;
		}
		message = "Configured effect-module detail count: " + configuredValue.ToString(CultureInfo.InvariantCulture)
            + "; deterministic routing uses " + effectiveValue.ToString(CultureInfo.InvariantCulture)
            + " for this run; the saved MCM value was not changed.";
		return true;
	}

	public static string GetNpcRulerPolicyApiSourceForExternal()
	{
		try
		{
			return ReadPolicyApiSourceSelection(GetSettings()?.NpcRulerPolicyApiSourceDropdown, 3);
		}
		catch
		{
			return PolicyApiSourceEventAndRebellion;
		}
	}

	public static bool GetNpcRulerPolicyFollowSelectedApiTokensForExternal()
	{
		try
		{
			return GetSettings()?.NpcRulerPolicyFollowSelectedApiTokens ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static int GetNpcRulerPolicyCustomMaxTokensForExternal()
	{
		try
		{
			return ClampPolicyApiMaxTokens(GetSettings()?.NpcRulerPolicyCustomMaxTokens ?? DefaultPolicyApiMaxTokens);
		}
		catch
		{
			return DefaultPolicyApiMaxTokens;
		}
	}

	public static int ClampPolicyApiMaxTokens(int maxTokens)
	{
		int normalized = maxTokens > 0 ? maxTokens : DefaultPolicyApiMaxTokens;
		return Math.Max(PolicyApiMaxTokensMinimum, Math.Min(PolicyApiMaxTokensMaximum, normalized));
	}

	public static int GetDailyConversationHistoryLineLimitForExternal()
	{
		try
		{
			return ClampDailyConversationHistoryLineLimit(GetSettings()?.DailyConversationHistoryLineLimit ?? DefaultDailyConversationHistoryLineLimit);
		}
		catch
		{
			return DefaultDailyConversationHistoryLineLimit;
		}
	}

	private static int ClampDailyConversationHistoryLineLimit(int value)
	{
		if (value <= 0)
		{
			return DefaultDailyConversationHistoryLineLimit;
		}
		return Math.Max(DailyConversationHistoryLineLimitMin, Math.Min(DailyConversationHistoryLineLimitMax, value));
	}

	public static int GetActionPostprocessHistoryEntryLimitForExternal()
	{
		try
		{
			return ClampActionPostprocessHistoryEntryLimit(GetSettings()?.ActionPostprocessHistoryEntryLimit ?? DefaultActionPostprocessHistoryEntryLimit);
		}
		catch
		{
			return DefaultActionPostprocessHistoryEntryLimit;
		}
	}

	private static int ClampActionPostprocessHistoryEntryLimit(int value)
	{
		if (value <= 0)
		{
			return DefaultActionPostprocessHistoryEntryLimit;
		}
		return Math.Max(ActionPostprocessHistoryEntryLimitMin, Math.Min(ActionPostprocessHistoryEntryLimitMax, value));
	}

	[Obsolete("Player policy influence cost is no longer used.")]
	public static int GetCustomPolicyInfluenceCostForExternal()
	{
		return 0;
	}

	public static bool IsAiEvaluatedCustomPolicyCostEnabledForExternal()
	{
		try
		{
			return GetSettings()?.UseAiEvaluatedCustomPolicyCost ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static string GetCustomPolicyEvaluatorPromptForExternal(out bool isDefault)
	{
		isDefault = true;
		try
		{
			string raw = GetSettings()?.CustomPolicyEvaluatorPrompt;
			if (raw == null)
			{
				return NormalizeCustomPolicyEvaluatorPromptText(DefaultCustomPolicyEvaluatorPrompt);
			}
			string text = NormalizeCustomPolicyEvaluatorPromptText(raw);
			if (IsBuiltInCustomPolicyEvaluatorPromptText(text))
			{
				isDefault = true;
				return NormalizeCustomPolicyEvaluatorPromptText(DefaultCustomPolicyEvaluatorPrompt);
			}
			isDefault = false;
			return text;
		}
		catch
		{
			isDefault = true;
			return NormalizeCustomPolicyEvaluatorPromptText(DefaultCustomPolicyEvaluatorPrompt);
		}
	}

	public static string GetWorldDiplomacyCommonContractForExternal()
	{
		try
		{
			return BuildWorldDiplomacyCommonContract(GetSettings()?.WorldDiplomacyPrompt ?? DefaultWorldDiplomacyPreference);
		}
		catch
		{
			return BuildWorldDiplomacyCommonContract(DefaultWorldDiplomacyPreference);
		}
	}

	[Obsolete("Use GetWorldDiplomacyCommonContractForExternal instead.")]
	public static string GetWorldDiplomacyPromptForExternal()
	{
		return GetWorldDiplomacyCommonContractForExternal();
	}

	public static int GetCustomPolicyPublicFeedbackTargetCharsForExternal()
	{
		try
		{
			return ClampCustomPolicyPublicFeedbackTargetChars(GetSettings()?.CustomPolicyPublicFeedbackTargetChars ?? DefaultCustomPolicyPublicFeedbackTargetChars);
		}
		catch
		{
			return DefaultCustomPolicyPublicFeedbackTargetChars;
		}
	}

	public static bool IsNpcRulerPolicyEnabledForExternal()
	{
		try
		{
			return GetSettings()?.EnableNpcRulerPolicy ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static int GetNpcRulerPolicyCheckIntervalDaysForExternal()
	{
		try
		{
			return ClampNpcRulerPolicyCheckIntervalDays(GetSettings()?.NpcRulerPolicyCheckIntervalDays ?? DefaultNpcRulerPolicyCheckIntervalDays);
		}
		catch
		{
			return DefaultNpcRulerPolicyCheckIntervalDays;
		}
	}

	public static int GetNpcRulerPolicyIntervalDaysForExternal()
	{
		try
		{
			DuelSettings settings = GetSettings();
			if (settings == null)
			{
				return DefaultNpcRulerPolicyIntervalDays;
			}
#pragma warning disable CS0618
			int days = settings.NpcRulerPolicyIntervalDays;
			if (days <= 0 && settings.NpcRulerPolicyIntervalHours > 0)
			{
				days = (int)Math.Ceiling(ClampNpcRulerPolicyIntervalHours(settings.NpcRulerPolicyIntervalHours) / 24.0);
			}
			else if (settings.NpcRulerPolicyIntervalHours != DefaultNpcRulerPolicyIntervalHours && days == DefaultNpcRulerPolicyIntervalDays)
			{
				days = (int)Math.Ceiling(ClampNpcRulerPolicyIntervalHours(settings.NpcRulerPolicyIntervalHours) / 24.0);
			}
#pragma warning restore CS0618
			return ClampNpcRulerPolicyIntervalDays(days);
		}
		catch
		{
			return DefaultNpcRulerPolicyIntervalDays;
		}
	}

	[Obsolete("Use GetNpcRulerPolicyIntervalDaysForExternal instead.")]
	public static int GetNpcRulerPolicyIntervalHoursForExternal()
	{
		return GetNpcRulerPolicyIntervalDaysForExternal() * 24;
	}

	public static int GetNpcRulerPolicyMaxKingdomsPerRequestForExternal()
	{
		return 1;
	}

	public static string GetNpcRulerPolicyPromptForExternal()
	{
		try
		{
			string raw = GetSettings()?.NpcRulerPolicyPrompt;
			return raw == null ? NormalizeNpcRulerPolicyPromptText(DefaultNpcRulerPolicyPrompt) : NormalizeNpcRulerPolicyPromptText(raw);
		}
		catch
		{
			return NormalizeNpcRulerPolicyPromptText(DefaultNpcRulerPolicyPrompt);
		}
	}

	[Obsolete("Detailed NPC ruler policy logging is no longer configurable.")]
	public static bool IsNpcRulerPolicyDebugLogEnabledForExternal()
	{
		return false;
	}

	private static int ClampCustomPolicyGoldCost(int value)
	{
		return Math.Max(0, Math.Min(500000, value));
	}

	private static int ClampCustomPolicyPublicFeedbackTargetChars(int value)
	{
		if (value <= 0)
		{
			value = DefaultCustomPolicyPublicFeedbackTargetChars;
		}
		int clamped = Math.Max(CustomPolicyPublicFeedbackTargetMinChars, Math.Min(CustomPolicyPublicFeedbackTargetMaxChars, value));
		int rounded = ((clamped + (CustomPolicyPublicFeedbackTargetStepChars / 2)) / CustomPolicyPublicFeedbackTargetStepChars) * CustomPolicyPublicFeedbackTargetStepChars;
		return Math.Max(CustomPolicyPublicFeedbackTargetMinChars, Math.Min(CustomPolicyPublicFeedbackTargetMaxChars, rounded));
	}

	private static int ClampNpcRulerPolicyCheckIntervalDays(int value)
	{
		if (value <= 0)
		{
			value = DefaultNpcRulerPolicyCheckIntervalDays;
		}
		return Math.Max(NpcRulerPolicyCheckIntervalMinDays, Math.Min(NpcRulerPolicyCheckIntervalMaxDays, value));
	}

	private static int ClampNpcRulerPolicyIntervalDays(int value)
	{
		if (value <= 0)
		{
			value = DefaultNpcRulerPolicyIntervalDays;
		}
		return Math.Max(NpcRulerPolicyIntervalMinDays, Math.Min(NpcRulerPolicyIntervalMaxDays, value));
	}

	private static int ClampNpcRulerPolicyIntervalHours(int value)
	{
		if (value <= 0)
		{
			value = DefaultNpcRulerPolicyIntervalHours;
		}
		return Math.Max(NpcRulerPolicyIntervalMinHours, Math.Min(NpcRulerPolicyIntervalMaxHours, value));
	}

	public static bool IsPeaceSceneConflictEnabled()
	{
		try
		{
			return GetSettings()?.EnablePeaceSceneConflict ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static bool IsNpcBattleEquipmentRestoreEnabled()
	{
		try
		{
			return GetSettings()?.EnableNpcBattleEquipmentRestore ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static bool IsKingdomStabilityAndRebellionEnabled()
	{
		try
		{
			return GetSettings()?.EnableKingdomStabilityAndRebellion ?? true;
		}
		catch
		{
			return true;
		}
	}

	public static bool IsPlayerKingdomRebellionImmunityEnabled()
	{
		try
		{
			return GetSettings()?.EnablePlayerKingdomRebellionImmunity ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsNpcTributaryVassalageEnabled()
	{
		return false;
	}

	public static float GetHealthThreshold()
	{
		float num = 0.35f;
		try
		{
			num = GetSettings()?.HealthThreshold ?? 0.35f;
		}
		catch
		{
			num = 0.35f;
		}
		if (float.IsNaN(num) || float.IsInfinity(num))
		{
			num = 0.35f;
		}
		if (num < 0.01f)
		{
			num = 0.01f;
		}
		if (num > 0.95f)
		{
			num = 0.95f;
		}
		return num;
	}

	public static bool TryGetDuelRenownPenaltySettings(out int minimum, out int percent, out int maximum)
	{
		minimum = DefaultDuelLoserClanRenownPenaltyMinimum;
		percent = DefaultDuelLoserClanRenownPenaltyPercent;
		maximum = DefaultDuelLoserClanRenownPenaltyMaximum;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings == null || !settings.EnableDuelLoserClanRenownPenalty)
			{
				return false;
			}
			minimum = Math.Max(0, Math.Min(1000, settings.DuelLoserClanRenownPenaltyMinimum));
			percent = Math.Max(0, Math.Min(50, settings.DuelLoserClanRenownPenaltyPercent));
			maximum = Math.Max(0, Math.Min(5000, settings.DuelLoserClanRenownPenaltyMaximum));
			if (maximum > 0 && maximum < minimum)
			{
				maximum = minimum;
			}
			return minimum > 0 || percent > 0;
		}
		catch
		{
			return false;
		}
	}

	private void OpenPlayerCustomPromptRuleEditor()
	{
		try
		{
			string initialText = PlayerCustomPromptRule ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_PlayerRule_Title", "Edit player custom rule text"), Ui("AF_MCM_Editor_PlayerRule_Description", "This text is injected at the beginning of the dialogue system prompt."), Ui("AF_MCM_Editor_PlayerRule_Hint", "You can enter more than the 512-character MCM text-box limit; leave it blank to inject nothing."), initialText, delegate(string input)
			{
				SavePlayerCustomPromptRuleFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenKingdomRebellionSystemPromptEditor()
	{
		try
		{
			string initialText = KingdomRebellionSystemPrompt ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_Rebellion_Title", "Edit kingdom rebellion system prompt"), Ui("AF_MCM_Editor_Rebellion_Description", "This text becomes the first part of the system prompt for the kingdom-founding name request."), "", initialText, delegate(string input)
			{
				SaveKingdomRebellionSystemPromptFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenWeeklyReportWritingRequirementsEditor()
	{
		try
		{
			string initialText = WeeklyReportWritingRequirements ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_Weekly_Title", "Edit weekly-report writing requirements"), Ui("AF_MCM_Editor_Weekly_Description", "This edits the complete writing-requirements section in the weekly-report system prompt."), Ui("AF_MCM_Editor_Weekly_Hint", "The default text is built into the code; you can edit or clear it. Output format, REPORT_BLOCK, TAGS, and stability tags remain controlled by built-in rules."), initialText, delegate(string input)
			{
				SaveWeeklyReportWritingRequirementsFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenNpcPersonaGenerationRequirementsEditor()
	{
		try
		{
			string initialText = NpcPersonaGenerationRequirements ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_Persona_Title", "Edit NPC personality/background requirements"), Ui("AF_MCM_Editor_Persona_Description", "Regular heroes and promoted companions share these complete writing requirements; built-in text keeps only the generation task, JSON protocol, and fact constraints."), Ui("AF_MCM_Editor_Persona_Hint", "You can change length, style, source selection, and regeneration de-duplication. Saving blank removes the extra requirements."), initialText, delegate(string input)
			{
				SaveNpcPersonaGenerationRequirementsFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenDailyMemoryCompressionWritingRequirementsEditor()
	{
		try
		{
			string initialText = DailyMemoryCompressionWritingRequirements ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_DailyMemory_Title", "Edit daily-memory compression requirements"), Ui("AF_MCM_Editor_DailyMemory_Description", "Edit only the style, information selection, and organization of the daily summary here."), Ui("AF_MCM_Editor_DailyMemory_Hint", "Tag structure, visibility, date and location, AFEF, identity facts, and length rules are fixed by the mod; conflicting text cannot override hard rules. Leave blank to add no writing preference."), initialText, delegate(string input)
			{
				SaveDailyMemoryCompressionWritingRequirementsFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenMemoryOverviewCompressionWritingRequirementsEditor()
	{
		try
		{
			string initialText = MemoryOverviewCompressionWritingRequirements ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_MemoryOverview_Title", "Edit memory-overview compression requirements"), Ui("AF_MCM_Editor_MemoryOverview_Description", "Edit only the style, information selection, and organization of the long-term memory overview here."), Ui("AF_MCM_Editor_MemoryOverview_Hint", "The SUMMARY tag, dates and locations, fact boundaries, identity rules, and length range are fixed by the mod; conflicting text cannot override hard rules. Leave blank to add no writing preference."), initialText, delegate(string input)
			{
				SaveMemoryOverviewCompressionWritingRequirementsFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenMajorActionCompressionWritingRequirementsEditor()
	{
		try
		{
			string initialText = MajorActionCompressionWritingRequirements ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_MajorAction_Title", "Edit major-action compression requirements"), Ui("AF_MCM_Editor_MajorAction_Description", "Edit only the style, information selection, and organization of the NPC major-action summary here."), Ui("AF_MCM_Editor_MajorAction_Hint", "The SUMMARY tag, dates and fact boundaries, rolling-merge behavior, and length range are fixed by the mod; conflicting text cannot override hard rules. Leave blank to add no writing preference."), initialText, delegate(string input)
			{
				SaveMajorActionCompressionWritingRequirementsFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_OpenFailed", "[Prompt extension] Failed to open the long-text editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenCustomPolicyEvaluatorPromptEditor()
	{
		try
		{
			string initialText = CustomPolicyEvaluatorPrompt ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_PlayerPolicy_Title", "Edit player policy evaluation prompt"), Ui("AF_MCM_Editor_PlayerPolicy_Description", "This is the shared evaluation prompt for kingdom and local policies."), Ui("AF_MCM_Editor_PlayerPolicy_Hint", "Use it to adjust cost, duration, public-feedback style, and intensity preferences. Explicit amounts, multipliers, ranges, and numeric constraints are passed to effect postprocessing through numericIntent; the JSON contract remains enforced by the mod. Saving blank restores the default."), initialText, delegate(string input)
			{
				SaveCustomPolicyEvaluatorPromptFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_Policy_OpenFailed", "[Policy system] Failed to open the player policy evaluation prompt editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenWorldDiplomacyPromptEditor()
	{
		try
		{
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_Diplomacy_Title", "Edit AI diplomacy preferences"), Ui("AF_MCM_Editor_Diplomacy_Description", "Adjust interest evaluation, action preferences, and style here."), Ui("AF_MCM_Editor_Diplomacy_Hint", "Leave blank to add no custom preference. The shared contract, long-term historical fact boundaries, MODE, and task output structure remain enforced by the mod."), WorldDiplomacyPrompt ?? "", delegate(string input)
			{
				SaveWorldDiplomacyPromptFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_Diplomacy_OpenFailed", "[AI diplomacy] Failed to open the prompt editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void ConfirmRestoreDefaultWorldDiplomacyPrompt()
	{
		try
		{
			InformationManager.ShowInquiry(new InquiryData(
				Ui("AF_MCM_Editor_Diplomacy_RestoreTitle", "Restore default AI diplomacy preferences"),
				Ui("AF_MCM_Editor_Diplomacy_RestoreBody", "This overwrites the current custom preferences and restores the mod defaults. Continue?"),
				true,
				true,
				Ui("AF_MCM_Editor_RestoreDefault", "Restore default"),
				Ui("AF_MCM_Common_Cancel", "Cancel"),
				RestoreDefaultWorldDiplomacyPromptNow,
				null), true);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_Diplomacy_RestoreOpenFailed", "[AI diplomacy] Failed to open the restore-default confirmation: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void RestoreDefaultWorldDiplomacyPromptNow()
	{
		SaveWorldDiplomacyPromptFromEditor("");
	}

	private void OpenNpcRulerPolicyPromptEditor()
	{
		try
		{
			string initialText = NpcRulerPolicyPrompt ?? "";
			DevTextEditorHelper.ShowLongTextEditor(Ui("AF_MCM_Editor_RulerPolicy_Title", "Edit NPC ruler policy prompt"), Ui("AF_MCM_Editor_RulerPolicy_Description", "This text controls how NPC ruler policies, concurrent world phenomena, and numeric effects are generated."), Ui("AF_MCM_Editor_RulerPolicy_Hint", "You may rewrite it completely. Saving blank restores the default; longer text requires more processing per request. Output format, legal effect targets, and safe numeric application remain enforced by the mod."), initialText, delegate(string input)
			{
				SaveNpcRulerPolicyPromptFromEditor(input);
			}, null, Ui("AF_MCM_Common_Save", "Save"), Ui("AF_MCM_Common_Back", "Back"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_RulerPolicy_OpenFailed", "[Policy system] Failed to open the NPC ruler policy prompt editor: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void OpenCustomPromptTextStoreFolder()
	{
		try
		{
			string directory = GetCustomPromptTextStoreDirectory();
			AnimusForgeDataPaths.EnsureWritableRoot(AnimusForgeDataPaths.GetCurrentRoot());
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			Process.Start(new ProcessStartInfo(directory)
			{
				UseShellExecute = true
			});
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_CustomFolder_Opening", "[Prompt extension] Opening the custom prompt JSON folder."), Color.FromUint(4278255360u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_CustomFolder_OpenFailed", "[Prompt extension] Failed to open the custom prompt JSON folder: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void ExportGcczDebugLogToDesktop()
	{
		ExportDiagnosticLogToDesktop(
			"GCCZ",
			GcczDiagnosticLog.GetDiagnosticLogPath,
			GcczDiagnosticLog.GetDiagnosticLogDirectory,
			GcczDiagnosticLog.ExportLogToDesktop);
	}

	private void ExportSetsDiagnosticLogToDesktop()
	{
		ExportDiagnosticLogToDesktop(
			"SETS",
			SettlementEntryTroopSelectionLog.GetDiagnosticLogPath,
			SettlementEntryTroopSelectionLog.GetDiagnosticLogDirectory,
			SettlementEntryTroopSelectionLog.ExportLogToDesktop);
	}

	private void ClearGcczDebugLogNow()
	{
		ClearDiagnosticLog("GCCZ", GcczDiagnosticLog.GetDiagnosticLogPath, GcczDiagnosticLog.ClearLog);
	}

	private void ClearSetsDiagnosticLogNow()
	{
		ClearDiagnosticLog("SETS", SettlementEntryTroopSelectionLog.GetDiagnosticLogPath, SettlementEntryTroopSelectionLog.ClearLog);
	}

	private void ExportNoblePrisonerEscortLogToDesktop()
	{
		ExportDiagnosticLogToDesktop(
			"NoblePrisonerEscort",
			NoblePrisonerEscortLog.GetDiagnosticLogPath,
			NoblePrisonerEscortLog.GetDiagnosticLogDirectory,
			NoblePrisonerEscortLog.ExportLogToDesktop);
	}

	private void ClearNoblePrisonerEscortLogNow()
	{
		ClearDiagnosticLog("NoblePrisonerEscort", NoblePrisonerEscortLog.GetDiagnosticLogPath, NoblePrisonerEscortLog.ClearLog);
	}

	private static string GetDiagnosticDisplayName(string featureName)
	{
		if (string.Equals(featureName, "NoblePrisonerEscort", StringComparison.Ordinal))
			return Ui("AF_MCM_Diagnostics_NoblePrisonerEscort", "Noble prisoner escort");
		return featureName;
	}

	private static void ExportDiagnosticLogToDesktop(string featureName, Func<string> getSourcePath, Func<string> getSourceDirectory, Func<string> exportLog)
	{
		try
		{
			string displayName = GetDiagnosticDisplayName(featureName);
			string sourcePath = getSourcePath();
			string sourceDirectory = getSourceDirectory();
			string exportPath = exportLog();
			string exportDirectory = Path.GetDirectoryName(exportPath);
			bool directoryOpened = false;
			if (!string.IsNullOrWhiteSpace(exportDirectory) && Directory.Exists(exportDirectory))
			{
				try
				{
					Process.Start(new ProcessStartInfo(exportDirectory)
					{
						UseShellExecute = true
					});
					directoryOpened = true;
				}
				catch (Exception openException)
				{
					Logger.Log("DuelSettings", "[WARN] " + featureName + " log exported, but failed to open its directory: " + openException.Message);
				}
			}
			string openStatus = directoryOpened ? string.Empty : Ui("AF_MCM_Diagnostics_DirectoryNotOpened", "; the directory was not opened automatically");
			InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Diagnostics_Exported", "[{FEATURE}] Log exported to the desktop: {FILE}; source directory: {DIRECTORY}{STATUS}", ("FEATURE", displayName), ("FILE", Path.GetFileName(exportPath)), ("DIRECTORY", sourceDirectory), ("STATUS", openStatus)), Color.FromUint(4278255360u)));
			Logger.Log("DuelSettings", "[" + featureName + "] Log exported successfully source=" + sourcePath + " export=" + exportPath);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Diagnostics_ExportFailed", "[{FEATURE}] Log export failed: ", ("FEATURE", GetDiagnosticDisplayName(featureName))) + ex.Message, Color.FromUint(4294901760u)));
			Logger.Log("DuelSettings", "[WARN] " + featureName + " log export failed: " + ex);
		}
	}

	private static void ClearDiagnosticLog(string featureName, Func<string> getLogPath, Action clearLog)
	{
		try
		{
			string displayName = GetDiagnosticDisplayName(featureName);
			string path = getLogPath();
			clearLog();
			InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Diagnostics_Cleared", "[{FEATURE}] Log cleared: {PATH}", ("FEATURE", displayName), ("PATH", path)), Color.FromUint(4278255360u)));
			Logger.Log("DuelSettings", "[" + featureName + "] Log cleared successfully path=" + path);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Diagnostics_ClearFailed", "[{FEATURE}] Log clear failed: ", ("FEATURE", GetDiagnosticDisplayName(featureName))) + ex.Message, Color.FromUint(4294901760u)));
			Logger.Log("DuelSettings", "[WARN] " + featureName + " log clear failed: " + ex);
		}
	}

	private void OpenAfdianSupportPage()
	{
		OpenAfdianSupportPageForExternal();
	}

	public static void OpenAfdianSupportPageForExternal()
	{
		// 复用缺省浏览器关联的兜底逻辑，确保支持页不会因玩家系统未设置默认浏览器而失效。
		if (ExternalBrowserLauncher.TryOpen(AfdianSupportUrl, out bool usedLocalBrowserFallback, out string failureMessage))
		{
			Logger.Log("DuelSettings", "The user clicked the [Support the author (Afdian)] button.");
			InformationManager.DisplayMessage(new InformationMessage(usedLocalBrowserFallback ? Ui("AF_MCM_Support_LocalBrowserFallback", "[System] The default browser could not start; the support page was opened with the local browser.") : Ui("AF_MCM_Support_Opening", "[System] Opening the support page."), Color.FromUint(4278255360u)));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Support_OpenFailed", "[System] Failed to open the support page: ") + failureMessage, Color.FromUint(4294901760u)));
			Logger.Log("DuelSettings", "[WARN] Failed to open the Afdian page: " + failureMessage);
		}
	}

	private void SavePlayerCustomPromptRuleFromEditor(string input)
	{
		string text = NormalizePlayerCustomPromptRuleText(input);
		bool persistedToFile = TryPersistPlayerCustomPromptRuleFile(text);
		if (persistedToFile && string.IsNullOrWhiteSpace(text)) text = LoadPlayerCustomPromptRuleFromDiskOrDefault();
		PlayerCustomPromptRule = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.PlayerCustomPromptRule = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persistedToFile
				? Ui("AF_MCM_Editor_PlayerPrompt_Saved", "[Prompt extension] Player custom rule text saved.")
				: Ui("AF_MCM_Editor_PlayerPrompt_SessionOnly", "[Prompt extension] Player custom rule text is active for this session, but local persistence failed. Check the log."), persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_PlayerPrompt_SaveFailed", "[Prompt extension] Saving failed; click Save again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void SaveKingdomRebellionSystemPromptFromEditor(string input)
	{
		string text = NormalizeKingdomRebellionSystemPromptText(input);
		bool persistedToFile = TryPersistKingdomRebellionSystemPromptFile(text);
		if (persistedToFile && string.IsNullOrWhiteSpace(text)) text = LoadKingdomRebellionSystemPromptFromDiskOrDefault();
		KingdomRebellionSystemPrompt = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.KingdomRebellionSystemPrompt = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persistedToFile
				? Ui("AF_MCM_Editor_KingdomRebellion_Saved", "[Prompt extension] Kingdom rebellion system prompt saved.")
				: Ui("AF_MCM_Editor_KingdomRebellion_SessionOnly", "[Prompt extension] Kingdom rebellion system prompt is active for this session, but local persistence failed. Check the log."), persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_KingdomRebellion_SaveFailed", "[Prompt extension] Saving the kingdom rebellion system prompt failed; click Save again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void SaveWeeklyReportWritingRequirementsFromEditor(string input)
	{
		string text = NormalizeWeeklyReportWritingRequirementsText(input);
		bool persistedToFile = TryPersistWeeklyReportWritingRequirementsFile(text);
		if (persistedToFile && string.IsNullOrWhiteSpace(text)) text = LoadWeeklyReportWritingRequirementsFromDiskOrDefault();
		WeeklyReportWritingRequirements = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.WeeklyReportWritingRequirements = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persistedToFile
				? Ui("AF_MCM_Editor_WeeklyRequirements_Saved", "[Prompt extension] Weekly report writing requirements saved.")
				: Ui("AF_MCM_Editor_WeeklyRequirements_SessionOnly", "[Prompt extension] Weekly report writing requirements are active for this session, but local persistence failed. Check the log."), persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_WeeklyRequirements_SaveFailed", "[Prompt extension] Saving the weekly report writing requirements failed; click Save again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void SaveNpcPersonaGenerationRequirementsFromEditor(string input)
	{
		string text = NormalizeNpcPersonaGenerationRequirementsText(input);
		bool persistedToFile = TryPersistNpcPersonaGenerationRequirementsFile(text);
		if (persistedToFile && string.IsNullOrWhiteSpace(text)) text = LoadNpcPersonaGenerationRequirementsFromDiskOrDefault();
		NpcPersonaGenerationRequirements = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.NpcPersonaGenerationRequirements = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persistedToFile
				? Ui("AF_MCM_Editor_PersonaRequirements_Saved", "[Prompt extension] NPC personality and background generation requirements saved.")
				: Ui("AF_MCM_Editor_PersonaRequirements_SessionOnly", "[Prompt extension] NPC personality and background generation requirements are active for this session, but local persistence failed. Check the log."), persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_PersonaRequirements_SaveFailed", "[Prompt extension] Saving the NPC personality and background generation requirements failed; click Save again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void SaveDailyMemoryCompressionWritingRequirementsFromEditor(string input)
	{
		string text = NormalizeDailyMemoryCompressionWritingRequirementsText(input);
		bool persisted = TryPersistDailyMemoryCompressionWritingRequirementsFile(text);
		if (persisted && string.IsNullOrWhiteSpace(text)) text = LoadDailyMemoryCompressionWritingRequirementsFromDiskOrDefault();
		DailyMemoryCompressionWritingRequirements = text;
		FinishCompressionWritingRequirementsSave(persisted, delegate(DuelSettings settings)
		{
			settings.DailyMemoryCompressionWritingRequirements = text;
		}, Ui("AF_MCM_Editor_DailyCompression_Name", "daily memory compression writing requirements"));
	}

	private void SaveMemoryOverviewCompressionWritingRequirementsFromEditor(string input)
	{
		string text = NormalizeMemoryOverviewCompressionWritingRequirementsText(input);
		bool persisted = TryPersistMemoryOverviewCompressionWritingRequirementsFile(text);
		if (persisted && string.IsNullOrWhiteSpace(text)) text = LoadMemoryOverviewCompressionWritingRequirementsFromDiskOrDefault();
		MemoryOverviewCompressionWritingRequirements = text;
		FinishCompressionWritingRequirementsSave(persisted, delegate(DuelSettings settings)
		{
			settings.MemoryOverviewCompressionWritingRequirements = text;
		}, Ui("AF_MCM_Editor_MemoryOverviewCompression_Name", "memory overview compression writing requirements"));
	}

	private void SaveMajorActionCompressionWritingRequirementsFromEditor(string input)
	{
		string text = NormalizeMajorActionCompressionWritingRequirementsText(input);
		bool persisted = TryPersistMajorActionCompressionWritingRequirementsFile(text);
		if (persisted && string.IsNullOrWhiteSpace(text)) text = LoadMajorActionCompressionWritingRequirementsFromDiskOrDefault();
		MajorActionCompressionWritingRequirements = text;
		FinishCompressionWritingRequirementsSave(persisted, delegate(DuelSettings settings)
		{
			settings.MajorActionCompressionWritingRequirements = text;
		}, Ui("AF_MCM_Editor_MajorActionCompression_Name", "major-action compression writing requirements"));
	}

	private void FinishCompressionWritingRequirementsSave(bool persistedToFile, Action<DuelSettings> applyToSettings, string displayName)
	{
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				applyToSettings?.Invoke(settings);
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			string message = persistedToFile
				? UiFormat("AF_MCM_Editor_Compression_Saved", "[Prompt extension] {NAME} saved.", ("NAME", displayName))
				: UiFormat("AF_MCM_Editor_Compression_SessionOnly", "[Prompt extension] {NAME} is active for this session, but local persistence failed. Check the log.", ("NAME", displayName));
			InformationManager.DisplayMessage(new InformationMessage(message, persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Editor_Compression_SaveFailed", "[Prompt extension] Saving {NAME} failed; try again in MCM: ", ("NAME", displayName)) + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	internal void SaveCustomPolicyEvaluatorPromptFromEditor(string input)
	{
		string text = NormalizeCustomPolicyEvaluatorPromptText(input);
		bool persistedToFile = TryPersistCustomPolicyEvaluatorPromptFile(text);
		if (persistedToFile && string.IsNullOrWhiteSpace(text)) text = LoadCustomPolicyEvaluatorPromptFromDiskOrDefault();
		CustomPolicyEvaluatorPrompt = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.CustomPolicyEvaluatorPrompt = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persistedToFile
				? Ui("AF_MCM_Editor_PlayerPolicy_Saved", "[Policy system] Player policy evaluation prompt saved.")
				: Ui("AF_MCM_Editor_PlayerPolicy_SessionOnly", "[Policy system] Player policy evaluation prompt is active for this session, but local persistence failed. Check the log."), persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_PlayerPolicy_SaveFailed", "[Policy system] Saving the player policy evaluation prompt failed; try again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private void SaveWorldDiplomacyPromptFromEditor(string input)
	{
		string text = NormalizeWorldDiplomacyPromptText(input);
		bool persisted = TryPersistWorldDiplomacyPromptFile(text);
		if (persisted && string.IsNullOrWhiteSpace(text)) text = LoadWorldDiplomacyPromptFromDiskOrDefault();
		WorldDiplomacyPrompt = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null) settings.WorldDiplomacyPrompt = text;
			BaseSettingsProvider.Instance?.SaveSettings(settings ?? this);
			InformationManager.DisplayMessage(new InformationMessage(persisted
				? Ui("AF_MCM_Editor_Diplomacy_Saved", "[AI diplomacy] Custom preferences saved.")
				: Ui("AF_MCM_Editor_Diplomacy_SessionOnly", "[AI diplomacy] Custom preferences are active for this session, but local persistence failed."), persisted ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_Diplomacy_SaveFailed", "[AI diplomacy] Saving custom preferences failed: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	internal void SaveNpcRulerPolicyPromptFromEditor(string input)
	{
		string text = NormalizeNpcRulerPolicyPromptText(input);
		bool persistedToFile = TryPersistNpcRulerPolicyPromptFile(text);
		NpcRulerPolicyPrompt = text;
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null)
			{
				settings.NpcRulerPolicyPrompt = text;
			}
		}
		catch
		{
		}
		try
		{
			BaseSettingsProvider.Instance?.SaveSettings(GetSettings() ?? this);
			string message = persistedToFile
				? Ui("AF_MCM_Editor_RulerPolicy_Saved", "[Policy system] NPC ruler policy prompt saved.")
				: Ui("AF_MCM_Editor_RulerPolicy_SessionOnly", "[Policy system] NPC ruler policy prompt is active for this session, but local persistence failed. Check the log.");
			InformationManager.DisplayMessage(new InformationMessage(message, persistedToFile ? Color.FromUint(4282569842u) : Color.FromUint(4294967040u)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Editor_RulerPolicy_SaveFailed", "[Policy system] Saving the NPC ruler policy prompt failed; click Save again in MCM: ") + ex.Message, Color.FromUint(4294901760u)));
		}
	}

	private static void EnsureCustomPromptTextSettingsLoaded(DuelSettings settings)
	{
		if (settings == null
			|| !TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store, out long revision, cloneResult: false)
			|| store == null
			|| settings._appliedCustomPromptTextStoreRevision == revision)
		{
			return;
		}
		if (!string.Equals(settings.PlayerCustomPromptRule ?? "", store.PlayerCustomPromptRule ?? "", StringComparison.Ordinal))
		{
			settings.PlayerCustomPromptRule = store.PlayerCustomPromptRule ?? "";
		}
		if (!string.Equals(settings.KingdomRebellionSystemPrompt ?? "", store.KingdomRebellionSystemPrompt ?? "", StringComparison.Ordinal))
		{
			settings.KingdomRebellionSystemPrompt = store.KingdomRebellionSystemPrompt ?? "";
		}
		if (!string.Equals(settings.WeeklyReportWritingRequirements ?? "", store.WeeklyReportWritingRequirements ?? "", StringComparison.Ordinal))
		{
			settings.WeeklyReportWritingRequirements = store.WeeklyReportWritingRequirements ?? "";
		}
		if (!string.Equals(settings.WorldBulletinWritingRequirements ?? "", store.WorldBulletinWritingRequirements ?? "", StringComparison.Ordinal))
		{
			settings.WorldBulletinWritingRequirements = store.WorldBulletinWritingRequirements ?? "";
		}
		if (!string.Equals(settings.NpcPersonaGenerationRequirements ?? "", store.NpcPersonaGenerationRequirements ?? "", StringComparison.Ordinal))
		{
			settings.NpcPersonaGenerationRequirements = store.NpcPersonaGenerationRequirements ?? "";
		}
		if (!string.Equals(settings.DailyMemoryCompressionWritingRequirements ?? "", store.DailyMemoryCompressionWritingRequirements ?? "", StringComparison.Ordinal))
		{
			settings.DailyMemoryCompressionWritingRequirements = store.DailyMemoryCompressionWritingRequirements ?? "";
		}
		if (!string.Equals(settings.MemoryOverviewCompressionWritingRequirements ?? "", store.MemoryOverviewCompressionWritingRequirements ?? "", StringComparison.Ordinal))
		{
			settings.MemoryOverviewCompressionWritingRequirements = store.MemoryOverviewCompressionWritingRequirements ?? "";
		}
		if (!string.Equals(settings.MajorActionCompressionWritingRequirements ?? "", store.MajorActionCompressionWritingRequirements ?? "", StringComparison.Ordinal))
		{
			settings.MajorActionCompressionWritingRequirements = store.MajorActionCompressionWritingRequirements ?? "";
		}
		if (!string.Equals(settings.CustomPolicyEvaluatorPrompt ?? "", store.CustomPolicyEvaluatorPrompt ?? "", StringComparison.Ordinal))
		{
			settings.CustomPolicyEvaluatorPrompt = store.CustomPolicyEvaluatorPrompt ?? "";
		}
		if (!string.Equals(settings.NpcRulerPolicyPrompt ?? "", store.NpcRulerPolicyPrompt ?? "", StringComparison.Ordinal))
		{
			settings.NpcRulerPolicyPrompt = store.NpcRulerPolicyPrompt ?? "";
		}
		if (!string.Equals(settings.WorldDiplomacyPrompt ?? "", store.WorldDiplomacyPrompt ?? "", StringComparison.Ordinal))
		{
			settings.WorldDiplomacyPrompt = store.WorldDiplomacyPrompt ?? "";
		}
		settings._appliedCustomPromptTextStoreRevision = revision;
	}

	private static string LoadPlayerCustomPromptRuleFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.PlayerCustomPromptRule ?? "") : DefaultPlayerCustomPromptRule;
	}

	private static string LoadKingdomRebellionSystemPromptFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.KingdomRebellionSystemPrompt ?? "") : DefaultKingdomRebellionSystemPrompt;
	}

	private static string LoadWeeklyReportWritingRequirementsFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.WeeklyReportWritingRequirements ?? "") : DefaultWeeklyReportWritingRequirements;
	}

	private static string LoadNpcPersonaGenerationRequirementsFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.NpcPersonaGenerationRequirements ?? "") : DefaultNpcPersonaGenerationRequirements;
	}

	private static string LoadDailyMemoryCompressionWritingRequirementsFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.DailyMemoryCompressionWritingRequirements ?? "") : DefaultDailyMemoryCompressionWritingRequirements;
	}

	private static string LoadMemoryOverviewCompressionWritingRequirementsFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.MemoryOverviewCompressionWritingRequirements ?? "") : DefaultMemoryOverviewCompressionWritingRequirements;
	}

	private static string LoadMajorActionCompressionWritingRequirementsFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? (store.MajorActionCompressionWritingRequirements ?? "") : DefaultMajorActionCompressionWritingRequirements;
	}

	private static string LoadCustomPolicyEvaluatorPromptFromDiskOrDefault()
	{
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return DefaultCustomPolicyEvaluatorPrompt;
		}
		string text = NormalizeCustomPolicyEvaluatorPromptText(store.CustomPolicyEvaluatorPrompt ?? "");
		return IsBuiltInCustomPolicyEvaluatorPromptText(text) ? DefaultCustomPolicyEvaluatorPrompt : text;
	}

	private static string LoadNpcRulerPolicyPromptFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store)
			? NormalizeNpcRulerPolicyPromptText(store.NpcRulerPolicyPrompt ?? "")
			: DefaultNpcRulerPolicyPrompt;
	}

	private static string LoadWorldDiplomacyPromptFromDiskOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store)
			? (store.WorldDiplomacyPrompt == null ? DefaultWorldDiplomacyPreference : NormalizeWorldDiplomacyPromptText(store.WorldDiplomacyPrompt))
			: DefaultWorldDiplomacyPreference;
	}

	private static string NormalizePlayerCustomPromptRuleText(string input)
	{
		string text = LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), PlayerCustomPromptRuleJsonFileName);
        return string.Equals(text, SourceDefaultPlayerCustomPromptRule, StringComparison.Ordinal) ? DefaultPlayerCustomPromptRule : text;
	}

	private static string NormalizeKingdomRebellionSystemPromptText(string input)
	{
		string text = LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), KingdomRebellionSystemPromptJsonFileName);
		return LimitCustomPromptText(StripKingdomRebellionOutputFormatBlock(text).Trim(), KingdomRebellionSystemPromptJsonFileName);
	}

	private static string StripKingdomRebellionOutputFormatBlock(string input)
	{
		string text = (input ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] lines = text.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = (lines[i] ?? "").Trim();
			if (!string.Equals(line, McmPromptDefaults.Raw("Mcm_KingdomRebellionOutputFormatFullWidth"), StringComparison.Ordinal) && !string.Equals(line, McmPromptDefaults.Raw("Mcm_KingdomRebellionOutputFormatAscii"), StringComparison.Ordinal))
			{
				continue;
			}
			int nameIndex = -1;
			int shortIndex = -1;
			int loreIndex = -1;
			for (int j = i + 1; j < lines.Length; j++)
			{
				string line2 = (lines[j] ?? "").Trim();
				if (line2.Length == 0)
				{
					continue;
				}
				if (nameIndex < 0 && line2.StartsWith("[NAME]", StringComparison.OrdinalIgnoreCase))
				{
					nameIndex = j;
					continue;
				}
				if (nameIndex >= 0 && shortIndex < 0 && line2.StartsWith("[SHORT]", StringComparison.OrdinalIgnoreCase))
				{
					shortIndex = j;
					continue;
				}
				if (shortIndex >= 0 && loreIndex < 0 && line2.StartsWith("[LORE]", StringComparison.OrdinalIgnoreCase))
				{
					loreIndex = j;
					break;
				}
			}
			if (nameIndex < 0 || shortIndex < 0 || loreIndex < 0)
			{
				continue;
			}
			List<string> kept = new List<string>();
			for (int j = 0; j < lines.Length; j++)
			{
				if (j < i || j > loreIndex)
				{
					kept.Add(lines[j]);
				}
			}
			return string.Join("\n", kept).Trim();
		}
		return text.Trim();
	}

	private static string NormalizeWeeklyReportWritingRequirementsText(string input)
	{
		return LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), WeeklyReportWritingRequirementsJsonFileName);
	}

	private static string NormalizeNpcPersonaGenerationRequirementsText(string input)
	{
		return LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), NpcPersonaGenerationRequirementsJsonFileName);
	}

	private static string NormalizeDailyMemoryCompressionWritingRequirementsText(string input)
	{
		return LimitCustomPromptText(NormalizePromptLineEndings(input).Trim(), DailyMemoryCompressionWritingRequirementsJsonFileName);
	}

	private static string NormalizeMemoryOverviewCompressionWritingRequirementsText(string input)
	{
		return LimitCustomPromptText(NormalizePromptLineEndings(input).Trim(), MemoryOverviewCompressionWritingRequirementsJsonFileName);
	}

	private static string NormalizeMajorActionCompressionWritingRequirementsText(string input)
	{
		return LimitCustomPromptText(NormalizePromptLineEndings(input).Trim(), MajorActionCompressionWritingRequirementsJsonFileName);
	}

	private static string NormalizeCustomPolicyEvaluatorPromptText(string input)
	{
		string text = NormalizePromptLineEndings(input);
		text = RemoveBuiltInPoliticalWeightsPromptLeak(text, DefaultCustomPolicyEvaluatorPrompt, LeakedCustomPolicyPoliticalWeightsPromptSuffix);
        text = RemoveBuiltInPoliticalWeightsPromptLeak(text, SourceDefaultCustomPolicyEvaluatorPrompt, LeakedCustomPolicyPoliticalWeightsPromptSuffix);
		text = RemoveBuiltInPoliticalWeightsPromptLeak(text, PreviousDefaultCustomPolicyEvaluatorPromptBeforeEffectModuleRouting, LeakedCustomPolicyPoliticalWeightsPromptSuffix);
		return LimitCustomPromptText(MigrateLegacyCustomPolicyEvaluatorPromptPrefix(text).Trim(), CustomPolicyEvaluatorPromptJsonFileName);
	}

	private static string NormalizeWorldDiplomacyPromptText(string input)
	{
		string text = LimitCustomPromptText(NormalizePromptLineEndings(input).Trim(), WorldDiplomacyPromptJsonFileName);
		return TryExtractWorldDiplomacyPreference(text, out string preference)
			? LimitCustomPromptText(preference, WorldDiplomacyPromptJsonFileName)
			: text;
	}

	private static string MigrateLegacyWorldDiplomacyPromptText(string input)
	{
		string text = NormalizeWorldDiplomacyPromptText(input);
		if (string.Equals(text, PreviousDefaultWorldDiplomacyPrompt, StringComparison.Ordinal)
			|| string.Equals(text, SourceDefaultWorldDiplomacyPreference, StringComparison.Ordinal)
            || string.Equals(text, PreviousDefaultWorldDiplomacyPromptV1, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultWorldDiplomacyPreferenceV3, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultWorldDiplomacyPreferenceV4, StringComparison.Ordinal))
		{
			return DefaultWorldDiplomacyPreference;
		}
		return text;
	}

	private static string BuildWorldDiplomacyCommonContract(string preference)
	{
		string normalizedPreference = NormalizeWorldDiplomacyPromptText(preference);
		if (string.IsNullOrWhiteSpace(normalizedPreference))
		{
			return DefaultWorldDiplomacyContractCore;
		}
		return DefaultWorldDiplomacyContractCore
			+ "\n" + WorldDiplomacyPreferenceSectionMarker + "\n"
			+ normalizedPreference
			+ "\n" + DefaultWorldDiplomacyPreferenceGuard;
	}

	private static bool TryExtractWorldDiplomacyPreference(string text, out string preference)
	{
		preference = "";
		if (string.IsNullOrWhiteSpace(text)) return false;
		bool knownContract = (text.StartsWith(DefaultWorldDiplomacyContractCore, StringComparison.Ordinal) || text.StartsWith(SourceDefaultWorldDiplomacyContractCore, StringComparison.Ordinal))
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV24, StringComparison.Ordinal)
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV23, StringComparison.Ordinal)
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV22, StringComparison.Ordinal)
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV21, StringComparison.Ordinal)
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV20, StringComparison.Ordinal)
			|| text.StartsWith(PreviousDefaultWorldDiplomacyContractCoreV19, StringComparison.Ordinal)
			|| IsExactPreviousDefaultWorldDiplomacyPromptV2(text);
		if (!knownContract) return false;
		string marker = "\n" + WorldDiplomacyPreferenceSectionMarker + "\n";
		int markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
		if (markerIndex < 0) return false;
		preference = text.Substring(markerIndex + marker.Length).Trim();
		string sourceGuardSuffix = "\n" + SourceDefaultWorldDiplomacyPreferenceGuard;
        if (preference.EndsWith(sourceGuardSuffix, StringComparison.Ordinal))
            preference = preference.Substring(0, preference.Length - sourceGuardSuffix.Length).Trim();
        string guardSuffix = "\n" + DefaultWorldDiplomacyPreferenceGuard;
		if (preference.EndsWith(guardSuffix, StringComparison.Ordinal))
		{
			preference = preference.Substring(0, preference.Length - guardSuffix.Length).Trim();
		}
		return true;
	}

	private static bool IsExactPreviousDefaultWorldDiplomacyPromptV2(string text)
	{
		if (string.IsNullOrEmpty(text)
			|| text.Length != PreviousDefaultWorldDiplomacyPromptV2Length
			|| !text.StartsWith(PreviousWorldDiplomacyContractHeaderV2, StringComparison.Ordinal))
		{
			return false;
		}
		using (SHA256 sha256 = SHA256.Create())
		{
			string hash = Convert.ToBase64String(sha256.ComputeHash(CustomPromptWriteEncoding.GetBytes(text)));
			return string.Equals(hash, PreviousDefaultWorldDiplomacyPromptV2Sha256Base64, StringComparison.Ordinal);
		}
	}

	private static bool IsWorldDiplomacyPromptPath(string path)
	{
		return string.Equals(Path.GetFileName(path ?? ""), WorldDiplomacyPromptJsonFileName, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeNpcRulerPolicyPromptText(string input)
	{
		string text = LimitCustomPromptText(NormalizePromptLineEndings(input), NpcRulerPolicyPromptJsonFileName);
		text = RemoveBuiltInPoliticalWeightsPromptLeak(text, DefaultNpcRulerPolicyPrompt, LeakedNpcPolicyPoliticalWeightsPromptSuffix);
        text = RemoveBuiltInPoliticalWeightsPromptLeak(text, SourceDefaultNpcRulerPolicyPrompt, LeakedNpcPolicyPoliticalWeightsPromptSuffix);
		if (string.IsNullOrWhiteSpace(text)
            || string.Equals(text, SourceDefaultNpcRulerPolicyPrompt, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeNaturalEditableLayer, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptForMigration, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithTechnicalContract, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeDerivedEvent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithTypedEvent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithFreeformDerivedEvent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithConciseAssociatedEvent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithCreativeDerivedEvent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeCreativePremise, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptWithCreativePremise, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeConsequentialEvents, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeFocusedPolicyAndEvents, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeKnowledgeGroundedContextRefactor, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeShortEditablePrompt, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeCompactContext, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeStrongerEffectsAndPlainLanguage, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeMinimumOneEffects, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeConceptOnlyPrompt, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeTownTax, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeTaxIntent, StringComparison.Ordinal)
			|| string.Equals(text, PreviousDefaultNpcRulerPolicyPromptBeforeConstructionSpeed, StringComparison.Ordinal))
		{
			return DefaultNpcRulerPolicyPrompt;
		}
		return text;
	}

	private static string RemoveBuiltInPoliticalWeightsPromptLeak(string text, string cleanBuiltInPrompt, string leakedSuffix)
	{
		string normalized = NormalizePromptLineEndings(text);
		string leakedBuiltIn = NormalizePromptLineEndings((cleanBuiltInPrompt ?? "").Trim() + "\n\n" + (leakedSuffix ?? "").Trim());
		return string.Equals(normalized, leakedBuiltIn, StringComparison.Ordinal)
			? NormalizePromptLineEndings(cleanBuiltInPrompt)
			: normalized;
	}

	private static string NormalizePromptLineEndings(string input)
	{
		return LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), "CustomPrompt");
	}

	private static string LimitCustomPromptText(string text, string source)
	{
		text = text ?? "";
		if (text.Length <= CustomPromptTextMaxChars)
		{
			return text;
		}
		try
		{
			LogPlayerCustomPromptRuleWarning("Custom prompt too long; truncated to prevent startup or request hangs: " + (source ?? "unknown") + " chars=" + text.Length + " max=" + CustomPromptTextMaxChars);
		}
		catch
		{
		}
		return text.Substring(0, CustomPromptTextMaxChars).TrimEnd();
	}

	private static string MigrateLegacyCustomPolicyEvaluatorPromptPrefix(string text)
	{
		text = NormalizePromptLineEndings(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (!LooksLikeLegacyCustomPolicyEvaluatorPrompt(text))
		{
			return text;
		}
		string legacyEnd = NormalizePromptLineEndings(McmPromptDefaults.Raw("Mcm_LegacyCustomPolicyEvaluatorPromptSuffix"));
		int suffixStart = text.LastIndexOf(legacyEnd, StringComparison.Ordinal);
		string suffix = suffixStart >= 0 ? text.Substring(suffixStart + legacyEnd.Length).Trim() : "";
		if (string.IsNullOrWhiteSpace(suffix))
		{
			return DefaultCustomPolicyEvaluatorPrompt;
		}
		return DefaultCustomPolicyEvaluatorPrompt.Trim() + "\n\n" + suffix;
	}

	private static bool LooksLikeLegacyCustomPolicyEvaluatorPrompt(string text)
	{
		return !string.IsNullOrWhiteSpace(text)
			&& text.Contains(McmPromptDefaults.Raw("Mcm_LegacyCustomPolicyEvaluatorPromptTitle"))
			&& (text.Contains(McmPromptDefaults.Raw("Mcm_LegacyCustomPolicyEvaluatorPromptMcmRewrite")) || text.Contains(McmPromptDefaults.Raw("Mcm_LegacyCustomPolicyEvaluatorPromptCalradia")));
	}

	private static bool IsBuiltInCustomPolicyEvaluatorPromptText(string input)
	{
		string text = NormalizeCustomPolicyEvaluatorPromptText(input);
		string currentWording = NormalizePolicyHearthWordingForBuiltInComparison(text);
		return string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(DefaultCustomPolicyEvaluatorPrompt)), StringComparison.Ordinal)
            || string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(SourceDefaultCustomPolicyEvaluatorPrompt)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeNaturalEditableLayer)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeEffectModuleRouting)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeVassalIndependence)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeSparseEffects)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeConstructionSpeed)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeTaxIntent)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptWithSeparatedBuiltIns)), StringComparison.Ordinal)
			|| string.Equals(currentWording, NormalizePolicyHearthWordingForBuiltInComparison(NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptWithVisibleBuiltIns)), StringComparison.Ordinal)
			|| string.Equals(text, NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptBeforeExpandedStats), StringComparison.Ordinal)
			|| string.Equals(text, NormalizeCustomPolicyEvaluatorPromptText(PreviousDefaultCustomPolicyEvaluatorPromptForMigration), StringComparison.Ordinal);
	}

	private static string NormalizePolicyHearthWordingForBuiltInComparison(string input)
	{
		return (input ?? "")
			.Replace(PreviousDefaultCustomPolicyDualCostPromptParagraph, DefaultCustomPolicyGoldCostPromptParagraph)
			.Replace(McmPromptDefaults.Raw("Mcm_PolicyHouseholdScaleVillage"), McmPromptDefaults.Raw("Mcm_PolicyHouseholdScaleVillageNormalized"))
			.Replace(McmPromptDefaults.Raw("Mcm_PolicyHouseholdScale"), McmPromptDefaults.Raw("Mcm_PolicyHouseholdScaleNormalized"));
	}

	private static bool TryReadPlayerCustomPromptRuleFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.PlayerCustomPromptRule ?? "";
		return true;
	}

	private static bool TryReadKingdomRebellionSystemPromptFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.KingdomRebellionSystemPrompt ?? "";
		return true;
	}

	private static bool TryReadWeeklyReportWritingRequirementsFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.WeeklyReportWritingRequirements ?? "";
		return true;
	}

	private static bool TryReadNpcPersonaGenerationRequirementsFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.NpcPersonaGenerationRequirements ?? "";
		return true;
	}

	private static bool TryReadCustomPolicyEvaluatorPromptFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.CustomPolicyEvaluatorPrompt ?? "";
		return true;
	}

	private static bool TryReadNpcRulerPolicyPromptFile(out string text)
	{
		text = "";
		if (!TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store))
		{
			return false;
		}
		text = store.NpcRulerPolicyPrompt ?? "";
		return true;
	}

	private static bool TryPersistPlayerCustomPromptRuleFile(string text)
	{
		return TryPersistCustomPromptTextFile(PlayerCustomPromptRuleJsonFileName, NormalizePlayerCustomPromptRuleText(text));
	}

	private static bool TryPersistKingdomRebellionSystemPromptFile(string text)
	{
		return TryPersistCustomPromptTextFile(KingdomRebellionSystemPromptJsonFileName, NormalizeKingdomRebellionSystemPromptText(text));
	}

	private static bool TryPersistWeeklyReportWritingRequirementsFile(string text)
	{
		return TryPersistCustomPromptTextFile(WeeklyReportWritingRequirementsJsonFileName, NormalizeWeeklyReportWritingRequirementsText(text));
	}

	private static bool TryPersistNpcPersonaGenerationRequirementsFile(string text)
	{
		return TryPersistCustomPromptTextFile(NpcPersonaGenerationRequirementsJsonFileName, NormalizeNpcPersonaGenerationRequirementsText(text));
	}

	private static bool TryPersistDailyMemoryCompressionWritingRequirementsFile(string text)
	{
		return TryPersistCustomPromptTextFile(DailyMemoryCompressionWritingRequirementsJsonFileName, NormalizeDailyMemoryCompressionWritingRequirementsText(text));
	}

	private static bool TryPersistMemoryOverviewCompressionWritingRequirementsFile(string text)
	{
		return TryPersistCustomPromptTextFile(MemoryOverviewCompressionWritingRequirementsJsonFileName, NormalizeMemoryOverviewCompressionWritingRequirementsText(text));
	}

	private static bool TryPersistMajorActionCompressionWritingRequirementsFile(string text)
	{
		return TryPersistCustomPromptTextFile(MajorActionCompressionWritingRequirementsJsonFileName, NormalizeMajorActionCompressionWritingRequirementsText(text));
	}

	private static bool TryPersistCustomPolicyEvaluatorPromptFile(string text)
	{
		return TryPersistPolicyPromptTextFile(CustomPolicyEvaluatorPromptJsonFileName, NormalizeCustomPolicyEvaluatorPromptText(text));
	}

	private static bool TryPersistNpcRulerPolicyPromptFile(string text)
	{
		return TryPersistPolicyPromptTextFile(NpcRulerPolicyPromptJsonFileName, NormalizeNpcRulerPolicyPromptText(text));
	}

	private static bool TryPersistWorldDiplomacyPromptFile(string text)
	{
		return TryPersistCustomPromptTextFile(WorldDiplomacyPromptJsonFileName, NormalizeWorldDiplomacyPromptText(text));
	}

	private static CustomPromptTextStoreJson ReadCustomPromptTextStoreOrDefault()
	{
		return TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store) ? store : BuildDefaultCustomPromptTextStore();
	}

	private static CustomPromptTextStoreJson BuildDefaultCustomPromptTextStore()
	{
		return NormalizeCustomPromptTextStore(new CustomPromptTextStoreJson
		{
			Version = WorldDiplomacyPromptJsonVersion,
			PlayerCustomPromptRule = DefaultPlayerCustomPromptRule,
			KingdomRebellionSystemPrompt = DefaultKingdomRebellionSystemPrompt,
			WeeklyReportWritingRequirements = DefaultWeeklyReportWritingRequirements,
			WorldBulletinWritingRequirements = DefaultWorldBulletinWritingRequirements,
			NpcPersonaGenerationRequirements = DefaultNpcPersonaGenerationRequirements,
			DailyMemoryCompressionWritingRequirements = DefaultDailyMemoryCompressionWritingRequirements,
			MemoryOverviewCompressionWritingRequirements = DefaultMemoryOverviewCompressionWritingRequirements,
			MajorActionCompressionWritingRequirements = DefaultMajorActionCompressionWritingRequirements,
			CustomPolicyEvaluatorPrompt = DefaultCustomPolicyEvaluatorPrompt,
			NpcRulerPolicyPrompt = DefaultNpcRulerPolicyPrompt,
			WorldDiplomacyPrompt = DefaultWorldDiplomacyPreference
		});
	}

	private static CustomPromptTextStoreJson BuildInitialCustomPromptTextStore()
	{
		CustomPromptTextStoreJson store = BuildDefaultCustomPromptTextStore();
		if (TryReadLegacyPromptText(GetPlayerCustomPromptRulePath(), NormalizePlayerCustomPromptRuleText, out string playerRule))
		{
			store.PlayerCustomPromptRule = playerRule;
		}
		if (TryReadLegacyPromptText(GetKingdomRebellionSystemPromptPath(), NormalizeKingdomRebellionSystemPromptText, out string rebellionPrompt))
		{
			store.KingdomRebellionSystemPrompt = rebellionPrompt;
		}
		if (TryReadLegacyPromptText(GetWeeklyReportWritingRequirementsPath(), NormalizeWeeklyReportWritingRequirementsText, out string weeklyRequirements))
		{
			store.WeeklyReportWritingRequirements = weeklyRequirements;
		}
		if (TryReadLegacyPromptText(GetNpcPersonaGenerationRequirementsPath(), NormalizeNpcPersonaGenerationRequirementsText, out string npcRequirements))
		{
			store.NpcPersonaGenerationRequirements = npcRequirements;
		}
		if (TryReadLegacyCustomPromptTextStore(out CustomPromptTextStoreJson legacyStore))
		{
			store = legacyStore;
		}
		return NormalizeCustomPromptTextStore(store);
	}

	private static bool TryReadLegacyPromptText(string path, Func<string, string> normalize, out string text)
	{
		text = "";
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return false;
			}
			if (IsCustomPromptTextFileTooLarge(path))
			{
				LogPlayerCustomPromptRuleWarning("Legacy custom prompt file too large; skipped migration: " + path);
				return false;
			}
			string value = File.ReadAllText(path, CustomPromptStrictUtf8Encoding);
			text = normalize != null ? normalize(value) : (value ?? "").Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store)
	{
		return TryReadCustomPromptTextStore(out store, out _, cloneResult: true);
	}

	private static bool TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store, out long revision, bool cloneResult)
	{
		store = null;
		revision = 0L;
		try
		{
			lock (CustomPromptTextStoreFileLock)
			{
				long now = Stopwatch.GetTimestamp();
				if (now < _customPromptTextStoreNextRefreshTimestamp)
				{
					if (_customPromptTextStoreCached == null)
					{
						return false;
					}
					store = cloneResult ? CloneCustomPromptTextStore(_customPromptTextStoreCached) : _customPromptTextStoreCached;
					revision = _customPromptTextStoreRevision;
					return true;
				}
				_customPromptTextStoreNextRefreshTimestamp = now + CustomPromptTextStoreRefreshIntervalTicks;
				string directory = GetCustomPromptTextStoreDirectory();
				string packagedDirectory = GetPackagedCustomPromptTextStoreDirectory();
				long fingerprint = unchecked(ComputeCustomPromptTextStoreFingerprint(directory) * 31L
					+ ComputeCustomPromptTextStoreFingerprint(packagedDirectory));
				if (_customPromptTextStoreFolderHydrated && _customPromptTextStoreFolderFingerprint == fingerprint && _customPromptTextStoreCached != null)
				{
					store = cloneResult ? CloneCustomPromptTextStore(_customPromptTextStoreCached) : _customPromptTextStoreCached;
					revision = _customPromptTextStoreRevision;
					return true;
				}
				store = BuildDefaultCustomPromptTextStore();
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, PlayerCustomPromptRuleJsonFileName, NormalizePlayerCustomPromptRuleText, store.PlayerCustomPromptRule, out string playerRule))
				{
					store.PlayerCustomPromptRule = playerRule;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, KingdomRebellionSystemPromptJsonFileName, NormalizeKingdomRebellionSystemPromptText, store.KingdomRebellionSystemPrompt, out string rebellionPrompt))
				{
					store.KingdomRebellionSystemPrompt = rebellionPrompt;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, WeeklyReportWritingRequirementsJsonFileName, NormalizeWeeklyReportWritingRequirementsText, store.WeeklyReportWritingRequirements, out string weeklyRequirements))
				{
					store.WeeklyReportWritingRequirements = weeklyRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, WorldBulletinWritingRequirementsJsonFileName, NormalizeWorldBulletinWritingRequirementsText, store.WorldBulletinWritingRequirements, out string bulletinRequirements))
				{
					store.WorldBulletinWritingRequirements = bulletinRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, NpcPersonaGenerationRequirementsJsonFileName, NormalizeNpcPersonaGenerationRequirementsText, store.NpcPersonaGenerationRequirements, out string npcRequirements))
				{
					store.NpcPersonaGenerationRequirements = npcRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, DailyMemoryCompressionWritingRequirementsJsonFileName, NormalizeDailyMemoryCompressionWritingRequirementsText, store.DailyMemoryCompressionWritingRequirements, out string dailyMemoryCompressionRequirements))
				{
					store.DailyMemoryCompressionWritingRequirements = dailyMemoryCompressionRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, MemoryOverviewCompressionWritingRequirementsJsonFileName, NormalizeMemoryOverviewCompressionWritingRequirementsText, store.MemoryOverviewCompressionWritingRequirements, out string memoryOverviewCompressionRequirements))
				{
					store.MemoryOverviewCompressionWritingRequirements = memoryOverviewCompressionRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, MajorActionCompressionWritingRequirementsJsonFileName, NormalizeMajorActionCompressionWritingRequirementsText, store.MajorActionCompressionWritingRequirements, out string majorActionCompressionRequirements))
				{
					store.MajorActionCompressionWritingRequirements = majorActionCompressionRequirements;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, CustomPolicyEvaluatorPromptJsonFileName, NormalizeCustomPolicyEvaluatorPromptText, store.CustomPolicyEvaluatorPrompt, out string customPolicyPrompt))
				{
					store.CustomPolicyEvaluatorPrompt = customPolicyPrompt;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, NpcRulerPolicyPromptJsonFileName, NormalizeNpcRulerPolicyPromptText, store.NpcRulerPolicyPrompt, out string npcRulerPolicyPrompt))
				{
					store.NpcRulerPolicyPrompt = npcRulerPolicyPrompt;
				}
				if (TryReadLayeredCustomPromptTextJsonFile(directory, packagedDirectory, WorldDiplomacyPromptJsonFileName, NormalizeWorldDiplomacyPromptText, DefaultWorldDiplomacyPreference, out string worldDiplomacyPrompt))
				{
					store.WorldDiplomacyPrompt = worldDiplomacyPrompt;
				}
				else
				{
					store.WorldDiplomacyPrompt = DefaultWorldDiplomacyPreference;
				}
				store = NormalizeCustomPromptTextStore(store);
				_customPromptTextStoreFolderHydrated = true;
				_customPromptTextStoreFolderFingerprint = fingerprint;
				_customPromptTextStoreCached = CloneCustomPromptTextStore(store);
				_customPromptTextStoreRevision++;
				revision = _customPromptTextStoreRevision;
				store = cloneResult ? CloneCustomPromptTextStore(_customPromptTextStoreCached) : _customPromptTextStoreCached;
				return true;
			}
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to load the custom prompt JSON directory: " + ex.Message);
			store = null;
			return false;
		}
	}

	private static bool TryPersistCustomPromptTextFile(string fileName, string text)
	{
		try
		{
			string directory = GetCustomPromptTextStoreDirectory();
			AnimusForgeDataPaths.EnsureWritableRoot(AnimusForgeDataPaths.GetCurrentRoot());
			lock (CustomPromptTextStoreFileLock)
			{
				PersistCustomPromptTextFileUnlocked(GetCustomPromptTextFilePath(directory, fileName), text);
				_customPromptTextStoreFolderHydrated = false;
				_customPromptTextStoreFolderFingerprint = 0L;
				_customPromptTextStoreCached = null;
				_customPromptTextStoreNextRefreshTimestamp = 0L;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to persist custom prompt JSON: " + ex.Message);
			return false;
		}
	}

	private static bool TryPersistPolicyPromptTextFile(string fileName, string text)
	{
		try
		{
			string directory = GetCustomPromptTextStoreDirectory();
			AnimusForgeDataPaths.EnsureWritableRoot(AnimusForgeDataPaths.GetCurrentRoot());
			lock (CustomPromptTextStoreFileLock)
			{
				PersistCustomPromptTextFileUnlocked(GetPolicyPromptTextFilePath(directory, fileName), text);
				_customPromptTextStoreFolderHydrated = false;
				_customPromptTextStoreFolderFingerprint = 0L;
				_customPromptTextStoreCached = null;
				_customPromptTextStoreNextRefreshTimestamp = 0L;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to persist policy prompt JSON: " + ex.Message);
			return false;
		}
	}

	private static void PersistCustomPromptTextFileUnlocked(string path, string text)
	{
		if (File.Exists(path) && !TryReadCustomPromptTextJsonFile(path, null, "", out _))
		{
			throw new InvalidOperationException("Existing custom prompt file is corrupted; please back up or repair the file. The original file was not overwritten: " + path);
		}
		WriteCustomPromptTextJsonFileUnlocked(path, text);
	}

	private static void EnsureCustomPromptTextStoreFilesUnlocked(string directory, CustomPromptTextStoreJson initialStore)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			return;
		}
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		MigrateLegacyPolicyPromptFilesUnlocked(directory);
		CustomPromptTextStoreJson normalized = NormalizeCustomPromptTextStore(initialStore);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, PlayerCustomPromptRuleJsonFileName), normalized.PlayerCustomPromptRule);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, KingdomRebellionSystemPromptJsonFileName), normalized.KingdomRebellionSystemPrompt);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, WeeklyReportWritingRequirementsJsonFileName), normalized.WeeklyReportWritingRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, WorldBulletinWritingRequirementsJsonFileName), normalized.WorldBulletinWritingRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, NpcPersonaGenerationRequirementsJsonFileName), normalized.NpcPersonaGenerationRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, DailyMemoryCompressionWritingRequirementsJsonFileName), normalized.DailyMemoryCompressionWritingRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, MemoryOverviewCompressionWritingRequirementsJsonFileName), normalized.MemoryOverviewCompressionWritingRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, MajorActionCompressionWritingRequirementsJsonFileName), normalized.MajorActionCompressionWritingRequirements);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetPolicyPromptTextFilePath(directory, CustomPolicyEvaluatorPromptJsonFileName), normalized.CustomPolicyEvaluatorPrompt);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetPolicyPromptTextFilePath(directory, NpcRulerPolicyPromptJsonFileName), normalized.NpcRulerPolicyPrompt);
		WriteCustomPromptTextJsonFileIfMissingUnlocked(GetCustomPromptTextFilePath(directory, WorldDiplomacyPromptJsonFileName), normalized.WorldDiplomacyPrompt);
	}

	private static void MigrateLegacyPolicyPromptFilesUnlocked(string customPromptDirectory)
	{
		string[] fileNames = new string[]
		{
			CustomPolicyEvaluatorPromptJsonFileName,
			NpcRulerPolicyPromptJsonFileName
		};
		for (int i = 0; i < fileNames.Length; i++)
		{
			try
			{
				string legacyPath = GetCustomPromptTextFilePath(customPromptDirectory, fileNames[i]);
				string policyPath = GetPolicyPromptTextFilePath(customPromptDirectory, fileNames[i]);
				if (!File.Exists(policyPath) && File.Exists(legacyPath))
				{
					string policyDirectory = Path.GetDirectoryName(policyPath);
					if (!string.IsNullOrWhiteSpace(policyDirectory))
					{
						Directory.CreateDirectory(policyDirectory);
					}
					File.Copy(legacyPath, policyPath, overwrite: false);
				}
			}
			catch (Exception ex)
			{
				LogPlayerCustomPromptRuleWarning("Failed to migrate the legacy policy prompt file: " + fileNames[i] + " - " + ex.Message);
			}
		}
	}

	private static void WriteCustomPromptTextJsonFileIfMissingUnlocked(string path, string text)
	{
		if (!File.Exists(path))
		{
			WriteCustomPromptTextJsonFileUnlocked(path, text);
		}
	}

	private static void WriteCustomPromptTextJsonFileUnlocked(string path, string text, bool replaceExisting = true)
	{
		AnimusForgeDataPaths.ValidatePath(path);
		string directoryName = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		CustomPromptTextJson jsonModel = new CustomPromptTextJson
		{
			Version = IsWorldDiplomacyPromptPath(path) ? WorldDiplomacyPromptJsonVersion : 1,
			Text = text ?? ""
		};
		string json = JsonConvert.SerializeObject(jsonModel, Formatting.Indented);
		string candidate = Path.Combine(directoryName, ".afp-" + Guid.NewGuid().ToString("N"));
		try
		{
			using (FileStream stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (StreamWriter writer = new StreamWriter(stream, CustomPromptWriteEncoding, 4096, leaveOpen: true))
			{
				writer.Write(json);
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}
			if (IsCustomPromptTextFileTooLarge(candidate)) throw new InvalidOperationException("Custom prompt JSON exceeds the size limit; the original was not overwritten.");
			JsonConvert.DeserializeObject<CustomPromptTextJson>(File.ReadAllText(candidate, CustomPromptStrictUtf8Encoding));
			AnimusForgeDataPaths.ValidatePath(path);
			if (File.Exists(path))
			{
				if (!replaceExisting) return;
				File.Replace(candidate, path, null);
			}
			else File.Move(candidate, path);
		}
		finally
		{
			if (File.Exists(candidate)) File.Delete(candidate);
		}
	}

	private static CustomPromptTextStoreJson NormalizeCustomPromptTextStore(CustomPromptTextStoreJson store)
	{
		store = store ?? new CustomPromptTextStoreJson();
		string customPolicyEvaluatorPrompt = store.CustomPolicyEvaluatorPrompt == null ? DefaultCustomPolicyEvaluatorPrompt : NormalizeCustomPolicyEvaluatorPromptText(store.CustomPolicyEvaluatorPrompt);
		if (IsBuiltInCustomPolicyEvaluatorPromptText(customPolicyEvaluatorPrompt))
		{
			customPolicyEvaluatorPrompt = DefaultCustomPolicyEvaluatorPrompt;
		}
		string npcRulerPolicyPrompt = store.NpcRulerPolicyPrompt == null ? DefaultNpcRulerPolicyPrompt : NormalizeNpcRulerPolicyPromptText(store.NpcRulerPolicyPrompt);
		string worldDiplomacyPrompt = store.WorldDiplomacyPrompt == null ? DefaultWorldDiplomacyPreference : NormalizeWorldDiplomacyPromptText(store.WorldDiplomacyPrompt);
		return new CustomPromptTextStoreJson
		{
			Version = store.Version <= 0 ? 1 : store.Version,
			PlayerCustomPromptRule = store.PlayerCustomPromptRule == null ? DefaultPlayerCustomPromptRule : NormalizePlayerCustomPromptRuleText(store.PlayerCustomPromptRule),
			KingdomRebellionSystemPrompt = store.KingdomRebellionSystemPrompt == null ? DefaultKingdomRebellionSystemPrompt : NormalizeKingdomRebellionSystemPromptText(store.KingdomRebellionSystemPrompt),
			WeeklyReportWritingRequirements = store.WeeklyReportWritingRequirements == null ? DefaultWeeklyReportWritingRequirements : NormalizeWeeklyReportWritingRequirementsText(store.WeeklyReportWritingRequirements),
			WorldBulletinWritingRequirements = store.WorldBulletinWritingRequirements == null ? DefaultWorldBulletinWritingRequirements : NormalizeWorldBulletinWritingRequirementsText(store.WorldBulletinWritingRequirements),
			NpcPersonaGenerationRequirements = store.NpcPersonaGenerationRequirements == null ? DefaultNpcPersonaGenerationRequirements : NormalizeNpcPersonaGenerationRequirementsText(store.NpcPersonaGenerationRequirements),
			DailyMemoryCompressionWritingRequirements = store.DailyMemoryCompressionWritingRequirements == null ? DefaultDailyMemoryCompressionWritingRequirements : NormalizeDailyMemoryCompressionWritingRequirementsText(store.DailyMemoryCompressionWritingRequirements),
			MemoryOverviewCompressionWritingRequirements = store.MemoryOverviewCompressionWritingRequirements == null ? DefaultMemoryOverviewCompressionWritingRequirements : NormalizeMemoryOverviewCompressionWritingRequirementsText(store.MemoryOverviewCompressionWritingRequirements),
			MajorActionCompressionWritingRequirements = store.MajorActionCompressionWritingRequirements == null ? DefaultMajorActionCompressionWritingRequirements : NormalizeMajorActionCompressionWritingRequirementsText(store.MajorActionCompressionWritingRequirements),
			CustomPolicyEvaluatorPrompt = customPolicyEvaluatorPrompt,
			NpcRulerPolicyPrompt = npcRulerPolicyPrompt,
			WorldDiplomacyPrompt = worldDiplomacyPrompt
		};
	}

	private static CustomPromptTextStoreJson CloneCustomPromptTextStore(CustomPromptTextStoreJson store)
	{
		if (store == null)
		{
			return null;
		}
		return new CustomPromptTextStoreJson
		{
			Version = store.Version,
			PlayerCustomPromptRule = store.PlayerCustomPromptRule,
			KingdomRebellionSystemPrompt = store.KingdomRebellionSystemPrompt,
			WeeklyReportWritingRequirements = store.WeeklyReportWritingRequirements,
			WorldBulletinWritingRequirements = store.WorldBulletinWritingRequirements,
			NpcPersonaGenerationRequirements = store.NpcPersonaGenerationRequirements,
			DailyMemoryCompressionWritingRequirements = store.DailyMemoryCompressionWritingRequirements,
			MemoryOverviewCompressionWritingRequirements = store.MemoryOverviewCompressionWritingRequirements,
			MajorActionCompressionWritingRequirements = store.MajorActionCompressionWritingRequirements,
			CustomPolicyEvaluatorPrompt = store.CustomPolicyEvaluatorPrompt,
			NpcRulerPolicyPrompt = store.NpcRulerPolicyPrompt,
			WorldDiplomacyPrompt = store.WorldDiplomacyPrompt
		};
	}

	private static string GetPlayerCustomPromptRulePath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath(PlayerCustomPromptRuleFileName);
		}
		catch
		{
			return "";
		}
	}

	private static bool TryReadCustomPromptTextJsonFile(string path, Func<string, string> normalize, string fallbackText, out string text)
	{
		text = "";
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return false;
			}
			AnimusForgeDataPaths.ValidatePath(path);
			if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
			{
				LogPlayerCustomPromptRuleWarning("Custom prompt JSON is an unapproved reparse file; preserved it and fell back to defaults: " + Path.GetFileName(path));
				return false;
			}
			if (IsCustomPromptTextFileTooLarge(path))
			{
				LogPlayerCustomPromptRuleWarning("Custom prompt JSON too large; preserved the original and fell back to defaults: " + Path.GetFileName(path));
				return false;
			}
			string json;
			try
			{
				json = CustomPromptStrictUtf8Encoding.GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF');
			}
			catch (DecoderFallbackException ex)
			{
				LogPlayerCustomPromptRuleWarning("Custom prompt JSON is not strict UTF-8; preserved the original and fell back to defaults: " + Path.GetFileName(path) + " - " + ex.GetType().Name);
				return false;
			}
			CustomPromptTextJson parsed;
			try
			{
				parsed = JsonConvert.DeserializeObject<CustomPromptTextJson>(json);
			}
			catch (Exception ex)
			{
				LogPlayerCustomPromptRuleWarning("Custom prompt JSON is malformed; preserved the original and fell back to defaults: " + Path.GetFileName(path) + " - " + ex.GetType().Name);
				return false;
			}
			if (parsed == null || parsed.Text == null)
			{
				LogPlayerCustomPromptRuleWarning("Custom prompt JSON is missing the Text field; preserved the original and fell back to defaults: " + Path.GetFileName(path));
				return false;
			}
			bool migrateWorldDiplomacyPrompt = IsWorldDiplomacyPromptPath(path) && parsed.Version < WorldDiplomacyPromptJsonVersion;
			string sourceText = migrateWorldDiplomacyPrompt
				? MigrateLegacyWorldDiplomacyPromptText(parsed.Text)
				: parsed.Text;
			if (string.Equals(Path.GetFileName(path), WorldBulletinWritingRequirementsJsonFileName, StringComparison.OrdinalIgnoreCase))
				sourceText = MigrateLegacyWorldBulletinWritingRequirements(sourceText);
			text = normalize != null ? normalize(sourceText) : (sourceText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
			return true;
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to read custom prompt JSON: " + Path.GetFileName(path) + " - " + ex.GetType().Name);
			return false;
		}
	}

	private static bool TryReadLayeredCustomPromptTextJsonFile(
		string overrideDirectory, string packagedDirectory, string fileName,
		Func<string, string> normalize, string fallbackText, out string text)
	{
		string overridePath = IsPolicyPromptFileName(fileName)
			? GetPolicyPromptTextFilePath(overrideDirectory, fileName)
			: GetCustomPromptTextFilePath(overrideDirectory, fileName);
		text = null;
		if (TryReadCustomPromptTextJsonFile(GetCustomPromptUserReadPath(overridePath), normalize, fallbackText, out text))
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(packagedDirectory))
		{
			return false;
		}
		string packagedPath = IsPolicyPromptFileName(fileName)
			? GetPolicyPromptTextFilePath(packagedDirectory, fileName)
			: GetCustomPromptTextFilePath(packagedDirectory, fileName);
		return TryReadCustomPromptTextJsonFile(packagedPath, normalize, fallbackText, out text);
	}

	private static readonly Lazy<Dictionary<string, HashSet<string>>> KnownPackagedCustomPromptHashes =
		new Lazy<Dictionary<string, HashSet<string>>>(LoadKnownPackagedCustomPromptHashes);

	private static Dictionary<string, HashSet<string>> LoadKnownPackagedCustomPromptHashes()
	{
		var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		string path = Path.Combine(AnimusForgeDataPaths.GetCurrentRoot(), "ModuleData", "McmKnownPromptDefaults.json");
		if (!File.Exists(path)) return result; // Missing metadata never makes an unknown user file a default.
		JObject document = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
		if ((int?)document["schemaVersion"] != 1 || !(document["files"] is JObject files))
			throw new InvalidDataException("Unsupported packaged prompt-default manifest.");
		foreach (JProperty property in files.Properties())
		{
			string relative = property.Name.Replace('\\', '/');
			if (relative.StartsWith("/", StringComparison.Ordinal) || relative.Contains("..") || relative.Contains(":"))
				throw new InvalidDataException("Invalid packaged prompt-default manifest path.");
			var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!(property.Value is JArray values)) throw new InvalidDataException("Invalid packaged prompt-default hashes.");
			foreach (JToken value in values)
			{
				string hash = (string)value;
				if (hash == null || hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c)))
					throw new InvalidDataException("Invalid packaged prompt-default SHA256.");
				hashes.Add(hash);
			}
			result.Add(relative, hashes);
		}
		return result;
	}

	private static readonly object KnownPackagedPromptHashGate = new object();
	private static readonly Dictionary<string, (long Stamp, long Length, bool Known)> KnownPackagedPromptHashCache =
		new Dictionary<string, (long Stamp, long Length, bool Known)>(StringComparer.OrdinalIgnoreCase);

	internal static bool IsKnownPackagedCustomPromptFile(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
		string root = Path.Combine(AnimusForgeDataPaths.GetCurrentRoot(), "CustomPrompts") + Path.DirectorySeparatorChar;
		string full = Path.GetFullPath(path);
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
		string relative = full.Substring(root.Length).Replace('\\', '/');
		if (!KnownPackagedCustomPromptHashes.Value.TryGetValue(relative, out HashSet<string> hashes)) return false;
		var info = new FileInfo(full);
		lock (KnownPackagedPromptHashGate)
		{
			if (KnownPackagedPromptHashCache.TryGetValue(full, out var cached)
				&& cached.Stamp == info.LastWriteTimeUtc.Ticks && cached.Length == info.Length) return cached.Known;
			using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var algorithm = SHA256.Create())
			{
				string hash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
				bool known = hashes.Contains(hash);
				KnownPackagedPromptHashCache[full] = (info.LastWriteTimeUtc.Ticks, info.Length, known);
				return known;
			}
		}
	}

	internal static string GetCustomPromptUserReadPath(string path)
	{
		if (!IsKnownPackagedCustomPromptFile(path)) return path;
		string root = AnimusForgeDataPaths.GetCurrentRoot();
		string prefix = Path.Combine(root, "CustomPrompts") + Path.DirectorySeparatorChar;
		string relative = Path.GetFullPath(path).Substring(prefix.Length);
		string imported = Path.Combine(root, "CustomPrompts", "LegacyImported", relative);
		return File.Exists(imported) ? imported : null;
	}

	private static bool IsCustomPromptTextFileTooLarge(string path)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length > CustomPromptJsonMaxBytes;
		}
		catch
		{
			return true;
		}
	}

	private static void QuarantineAndRestoreCustomPromptTextFileUnlocked(string path, string fallbackText, string reason)
	{
		QuarantineCustomPromptTextFileUnlocked(path, reason);
		try
		{
			WriteCustomPromptTextJsonFileUnlocked(path, fallbackText ?? "");
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to restore default custom prompt JSON: " + path + " - " + ex.Message);
		}
	}

	private static void BackupCustomPromptMigrationSourceUnlocked(string path, string sourceVersion)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return;
			}
			string safeVersion = SanitizeCustomPromptQuarantineReason(sourceVersion);
			string backupPath = path + ".backup-" + safeVersion;
			if (File.Exists(backupPath))
			{
				FileInfo sourceInfo = new FileInfo(path);
				FileInfo backupInfo = new FileInfo(backupPath);
				if (sourceInfo.Length == backupInfo.Length
					&& HaveSameCustomPromptFileBytes(path, backupPath))
				{
					return;
				}
				string suffix = "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
				backupPath += suffix;
				if (File.Exists(backupPath))
				{
					backupPath += "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
				}
			}
			File.Copy(path, backupPath, false);
			LogPlayerCustomPromptRuleWarning("Backed up custom prompt JSON before migration: " + path + " -> " + backupPath);
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to back up custom prompt JSON before migration: " + path + " - " + ex.Message);
		}
	}

	private static bool HaveSameCustomPromptFileBytes(string firstPath, string secondPath)
	{
		byte[] first = File.ReadAllBytes(firstPath);
		byte[] second = File.ReadAllBytes(secondPath);
		if (first.Length != second.Length) return false;
		for (int i = 0; i < first.Length; i++)
		{
			if (first[i] != second[i]) return false;
		}
		return true;
	}

	private static void QuarantineCustomPromptTextFileUnlocked(string path, string reason)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return;
			}
			string backupPath = BuildCustomPromptQuarantinePath(path, reason);
			string directoryName = Path.GetDirectoryName(backupPath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.Copy(path, backupPath, true);
			LogPlayerCustomPromptRuleWarning("Backed up invalid custom prompt JSON: " + path + " -> " + backupPath);
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to back up invalid custom prompt JSON: " + path + " - " + ex.Message);
		}
	}

	private static string BuildCustomPromptQuarantinePath(string path, string reason)
	{
		string safeReason = SanitizeCustomPromptQuarantineReason(reason);
		string suffix = ".bad-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + safeReason;
		string candidate = path + suffix;
		if (!File.Exists(candidate))
		{
			return candidate;
		}
		return path + suffix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	}

	private static string SanitizeCustomPromptQuarantineReason(string reason)
	{
		reason = (reason ?? "invalid").Trim();
		if (string.IsNullOrWhiteSpace(reason))
		{
			return "invalid";
		}
		StringBuilder builder = new StringBuilder(reason.Length);
		for (int i = 0; i < reason.Length; i++)
		{
			char c = reason[i];
			if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
			{
				builder.Append(c);
			}
			else
			{
				builder.Append('_');
			}
		}
		string text = builder.ToString().Trim('_');
		return string.IsNullOrWhiteSpace(text) ? "invalid" : text;
	}

	private static bool TryReadLegacyCustomPromptTextStore(out CustomPromptTextStoreJson store)
	{
		store = null;
		try
		{
			string path = GetLegacyCustomPromptTextStorePath();
			if (!string.IsNullOrWhiteSpace(path)) AnimusForgeDataPaths.ValidatePath(path);
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				return false;
			}
			if (IsCustomPromptTextFileTooLarge(path))
			{
				LogPlayerCustomPromptRuleWarning("Legacy custom prompt JSON too large; preserved the original and used current defaults: " + path);
				// 不让坏损的 aggregate 默认值覆盖已成功读取的更旧独立文本。
				return false;
			}
			string json;
			try
			{
				json = CustomPromptStrictUtf8Encoding.GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF');
			}
			catch (DecoderFallbackException ex)
			{
				LogPlayerCustomPromptRuleWarning("Legacy custom prompt JSON is not strict UTF-8; preserved the original and used current defaults: " + path + " - " + ex.Message);
				return false;
			}
			CustomPromptTextStoreJson parsed;
			try
			{
				parsed = JsonConvert.DeserializeObject<CustomPromptTextStoreJson>(json);
			}
			catch (Exception ex)
			{
				LogPlayerCustomPromptRuleWarning("Legacy custom prompt JSON is malformed; preserved the original and used current defaults: " + path + " - " + ex.Message);
				return false;
			}
			if (parsed == null)
			{
				LogPlayerCustomPromptRuleWarning("Legacy custom prompt JSON has no valid content; preserved the original and used current defaults: " + path);
				return false;
			}
			bool needsMigration = parsed.Version < WorldDiplomacyPromptJsonVersion;
			if (needsMigration)
			{
				parsed.WorldDiplomacyPrompt = MigrateLegacyWorldDiplomacyPromptText(parsed.WorldDiplomacyPrompt);
			}
			parsed.WorldBulletinWritingRequirements = MigrateLegacyWorldBulletinWritingRequirements(parsed.WorldBulletinWritingRequirements);
			store = NormalizeCustomPromptTextStore(parsed);
			if (needsMigration)
			{
				store.Version = WorldDiplomacyPromptJsonVersion;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to migrate legacy custom prompt JSON: " + ex.Message);
			store = null;
			return false;
		}
	}

	private static void QuarantineAndRestoreLegacyCustomPromptTextStoreUnlocked(string path, string reason)
	{
		QuarantineCustomPromptTextFileUnlocked(path, reason);
		CustomPromptTextStoreJson fallback = BuildDefaultCustomPromptTextStore();
		try
		{
			WriteLegacyCustomPromptTextStoreUnlocked(path, fallback);
		}
		catch (Exception ex)
		{
			LogPlayerCustomPromptRuleWarning("Failed to restore the current JSON defaults for a legacy custom prompt: " + path + " - " + ex.Message);
		}
	}

	private static void WriteLegacyCustomPromptTextStoreUnlocked(string path, CustomPromptTextStoreJson store)
	{
		CustomPromptTextStoreJson normalized = NormalizeCustomPromptTextStore(store);
		normalized.Version = WorldDiplomacyPromptJsonVersion;
		string json = JsonConvert.SerializeObject(normalized, Formatting.Indented);
		File.WriteAllText(path, json, CustomPromptWriteEncoding);
	}

	private static long ComputeCustomPromptTextStoreFingerprint(string directory)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(directory)) return 0L;
			unchecked
			{
				long hash = 17L;
				string[] fileNames = new string[]
				{
					PlayerCustomPromptRuleJsonFileName,
					KingdomRebellionSystemPromptJsonFileName,
					WeeklyReportWritingRequirementsJsonFileName,
					WorldBulletinWritingRequirementsJsonFileName,
					NpcPersonaGenerationRequirementsJsonFileName,
					DailyMemoryCompressionWritingRequirementsJsonFileName,
					MemoryOverviewCompressionWritingRequirementsJsonFileName,
					MajorActionCompressionWritingRequirementsJsonFileName,
					CustomPolicyEvaluatorPromptJsonFileName,
					NpcRulerPolicyPromptJsonFileName,
					WorldDiplomacyPromptJsonFileName
				};
				for (int i = 0; i < fileNames.Length; i++)
				{
					string path = IsPolicyPromptFileName(fileNames[i])
						? GetPolicyPromptTextFilePath(directory, fileNames[i])
						: GetCustomPromptTextFilePath(directory, fileNames[i]);
					if (string.Equals(Path.GetFullPath(directory), GetCustomPromptTextStoreDirectory(), StringComparison.OrdinalIgnoreCase))
						path = GetCustomPromptUserReadPath(path);
					if (!File.Exists(path))
					{
						hash = hash * 31L;
						continue;
					}
					FileInfo fileInfo = new FileInfo(path);
					hash = hash * 31L + fileInfo.LastWriteTimeUtc.Ticks;
					hash = hash * 31L + fileInfo.Length;
				}
				return hash;
			}
		}
		catch
		{
			return 0L;
		}
	}

	private static string GetCustomPromptTextStoreDirectory()
	{
		return AnimusForgeDataPaths.GetOverridePath(
			AnimusForgeDataPaths.GetCurrentRoot(), CustomPromptTextStoreFolderName);
	}

	private static string GetPackagedCustomPromptTextStoreDirectory()
	{
		return AnimusForgeDataPaths.GetPackagedCustomPromptsDirectory(AnimusForgeDataPaths.GetCurrentRoot());
	}

	internal static string GetCustomPromptTextStoreDirectoryForPolicyPrompts()
	{
		return BuildPolicyPromptSubdirectory(GetCustomPromptTextStoreDirectory());
	}

	internal static string GetPackagedCustomPromptTextStoreDirectoryForPolicyPrompts()
	{
		return BuildPolicyPromptSubdirectory(GetPackagedCustomPromptTextStoreDirectory());
	}

	internal static string BuildPolicyPromptSubdirectoryForContractTests(string customPromptDirectory)
	{
		return BuildPolicyPromptSubdirectory(customPromptDirectory);
	}

	private static string BuildPolicyPromptSubdirectory(string customPromptDirectory)
	{
		return string.IsNullOrWhiteSpace(customPromptDirectory)
			? string.Empty
			: Path.Combine(customPromptDirectory, PolicyPromptSubfolderName);
	}

	private static string GetPolicyPromptTextFilePath(string customPromptDirectory, string fileName)
	{
		return GetCustomPromptTextFilePath(BuildPolicyPromptSubdirectory(customPromptDirectory), fileName);
	}

	private static bool IsPolicyPromptFileName(string fileName)
	{
		return string.Equals(fileName, CustomPolicyEvaluatorPromptJsonFileName, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(fileName, NpcRulerPolicyPromptJsonFileName, StringComparison.OrdinalIgnoreCase);
	}

	private static string GetLegacyCustomPromptTextStorePath()
	{
		try
		{
			string moduleRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
			if (!string.IsNullOrWhiteSpace(moduleRoot))
			{
				return Path.Combine(moduleRoot, LegacyCustomPromptTextStoreFileName);
			}
			return AnimusForgeModulePaths.GetLogFilePath(LegacyCustomPromptTextStoreFileName);
		}
		catch
		{
			return "";
		}
	}

	private static string GetCustomPromptTextFilePath(string directory, string fileName)
	{
		string safeFileName = Path.GetFileName((fileName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(safeFileName))
		{
			safeFileName = "Prompt.json";
		}
		return Path.Combine(directory ?? "", safeFileName);
	}

	private static string GetKingdomRebellionSystemPromptPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath(KingdomRebellionSystemPromptFileName);
		}
		catch
		{
			return "";
		}
	}

	private static string GetWeeklyReportWritingRequirementsPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath(WeeklyReportWritingRequirementsFileName);
		}
		catch
		{
			return "";
		}
	}

	private static string GetNpcPersonaGenerationRequirementsPath()
	{
		try
		{
			return AnimusForgeModulePaths.GetLogFilePath(NpcPersonaGenerationRequirementsFileName);
		}
		catch
		{
			return "";
		}
	}

	private static void LogPlayerCustomPromptRuleWarning(string message)
	{
		try
		{
			Logger.Log("DuelSettings", "[WARN] " + message);
		}
		catch
		{
		}
	}

	private void EnsureModelDropdownCacheHydrated()
	{
		string modelDropdownCachePath;
		try { modelDropdownCachePath = GetModelDropdownCachePath(); }
		catch (Exception ex)
		{
			Logger.Log("DuelSettings", "[WARN] Model dropdown cache path unavailable: " + ex.GetType().Name);
			return;
		}
		long num = 0L;
		try
		{
			if (!string.IsNullOrWhiteSpace(modelDropdownCachePath) && File.Exists(modelDropdownCachePath))
			{
				num = File.GetLastWriteTimeUtc(modelDropdownCachePath).Ticks;
			}
		}
		catch
		{
			num = 0L;
		}
		if (_modelDropdownCacheHydrated && num <= _modelDropdownCacheLastWriteUtcTicks)
		{
			return;
		}
		try
		{
			if (string.IsNullOrWhiteSpace(modelDropdownCachePath) || !File.Exists(modelDropdownCachePath))
			{
				_modelDropdownCacheHydrated = true;
				_modelDropdownCacheLastWriteUtcTicks = num;
				return;
			}
			ModelDropdownCacheSnapshot modelDropdownCacheSnapshot = ReadModelDropdownCacheSnapshot(modelDropdownCachePath);
			MergeCachedDropdownState(_mainApiModelOptions, _mainApiModelDropdown, modelDropdownCacheSnapshot.MainOptions, modelDropdownCacheSnapshot.MainSelected, ModelName, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out _mainApiModelDropdown);
			_mainApiModelOptions = FilterRemovedMainModelPresets(_mainApiModelOptions);
			string text = ReadSelectedModelOption(_mainApiModelDropdown);
			if (IsRemovedMainModelPreset(text))
			{
				_mainApiModelDropdown = BuildDropdownFromOptions(_mainApiModelOptions, ManualDropdownModelName, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out var _);
			}
			MergeCachedDropdownState(_auxiliaryApiModelOptions, _auxiliaryApiModelDropdown, modelDropdownCacheSnapshot.AuxiliaryOptions, modelDropdownCacheSnapshot.AuxiliarySelected, AuxiliaryModelName, "", preserveBlankSelection: false, out _auxiliaryApiModelOptions, out _auxiliaryApiModelDropdown);
			MergeCachedDropdownState(_actionPostprocessApiModelOptions, _actionPostprocessApiModelDropdown, modelDropdownCacheSnapshot.ActionPostprocessOptions, modelDropdownCacheSnapshot.ActionPostprocessSelected, ActionPostprocessModelName, "", preserveBlankSelection: false, out _actionPostprocessApiModelOptions, out _actionPostprocessApiModelDropdown);
			MergeCachedDropdownState(_eventAndRebellionApiModelOptions, _eventAndRebellionApiModelDropdown, modelDropdownCacheSnapshot.EventAndRebellionOptions, modelDropdownCacheSnapshot.EventAndRebellionSelected, EventAndRebellionModelName, "", preserveBlankSelection: false, out _eventAndRebellionApiModelOptions, out _eventAndRebellionApiModelDropdown);
			MergeCachedDropdownState(_townAmbientAiModelOptions, _townAmbientAiModelDropdown, modelDropdownCacheSnapshot.TownAmbientAiOptions, modelDropdownCacheSnapshot.TownAmbientAiSelected, TownAmbientAiModelName, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out _townAmbientAiModelDropdown);
			_modelDropdownCacheHydrated = true;
			_modelDropdownCacheLastWriteUtcTicks = num;
		}
		catch (Exception ex)
		{
			_modelDropdownCacheHydrated = true;
			_modelDropdownCacheLastWriteUtcTicks = num;
			Logger.Log("DuelSettings", "[WARN] Failed to load the model dropdown cache: " + ex.Message);
		}
	}

	private void PersistModelDropdownCacheSnapshot()
	{
		try
		{
			string modelDropdownCachePath = GetModelDropdownCachePath();
			AnimusForgeDataPaths.EnsureWritableRoot(AnimusForgeDataPaths.GetCurrentRoot());
			string directoryName = Path.GetDirectoryName(modelDropdownCachePath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			ModelDropdownCacheSnapshot modelDropdownCacheSnapshot = new ModelDropdownCacheSnapshot
			{
				MainOptions = CopyNormalizedModelOptions(_mainApiModelOptions),
				MainSelected = ResolveSelectedOptionForSnapshot(ReadSelectedModelOption(_mainApiModelDropdown), ModelName, DefaultDropdownModelName, preserveBlankSelection: false),
				AuxiliaryOptions = CopyNormalizedModelOptions(_auxiliaryApiModelOptions),
				AuxiliarySelected = ResolveSelectedOptionForSnapshot(ReadSelectedModelOption(_auxiliaryApiModelDropdown), AuxiliaryModelName, "", preserveBlankSelection: false),
				ActionPostprocessOptions = CopyNormalizedModelOptions(_actionPostprocessApiModelOptions),
				ActionPostprocessSelected = ResolveSelectedOptionForSnapshot(ReadSelectedModelOption(_actionPostprocessApiModelDropdown), ActionPostprocessModelName, "", preserveBlankSelection: false),
				EventAndRebellionOptions = CopyNormalizedModelOptions(_eventAndRebellionApiModelOptions),
				EventAndRebellionSelected = ResolveSelectedOptionForSnapshot(ReadSelectedModelOption(_eventAndRebellionApiModelDropdown), EventAndRebellionModelName, "", preserveBlankSelection: false),
				TownAmbientAiOptions = CopyNormalizedModelOptions(_townAmbientAiModelOptions),
				TownAmbientAiSelected = ResolveSelectedOptionForSnapshot(ReadSelectedModelOption(_townAmbientAiModelDropdown), TownAmbientAiModelName, "", preserveBlankSelection: false),
				SavedAtUtc = DateTime.UtcNow.ToString("o")
			};
			string contents = JsonConvert.SerializeObject(modelDropdownCacheSnapshot, Formatting.Indented);
			lock (ModelDropdownCacheFileLock)
			{
				AnimusForgeDataPaths.ValidatePath(modelDropdownCachePath);
				if (File.Exists(modelDropdownCachePath)) ReadModelDropdownCacheSnapshot(modelDropdownCachePath);
				string candidate = Path.Combine(directoryName, ".af-cache-" + Guid.NewGuid().ToString("N") + ".tmp");
				try
				{
					File.WriteAllText(candidate, contents, new UTF8Encoding(false, true));
					ReadModelDropdownCacheSnapshot(candidate);
					AnimusForgeDataPaths.ValidatePath(modelDropdownCachePath);
					if (File.Exists(modelDropdownCachePath)) File.Replace(candidate, modelDropdownCachePath, null);
					else File.Move(candidate, modelDropdownCachePath);
					_modelDropdownCacheLastWriteUtcTicks = File.GetLastWriteTimeUtc(modelDropdownCachePath).Ticks;
				}
				finally { if (File.Exists(candidate)) File.Delete(candidate); }
			}
			_modelDropdownCacheHydrated = true;
		}
		catch (Exception ex)
		{
			Logger.Log("DuelSettings", "[WARN] Failed to persist the model dropdown cache: " + ex.Message);
		}
	}

	private static string GetModelDropdownCachePath()
	{
		return Path.Combine(AnimusForgeDataPaths.GetCacheDirectory(AnimusForgeDataPaths.GetCurrentRoot()), ModelDropdownCacheFileName);
	}

	private static ModelDropdownCacheSnapshot ReadModelDropdownCacheSnapshot(string path)
	{
		AnimusForgeDataPaths.ValidatePath(path);
		string json = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF');
		return JsonConvert.DeserializeObject<ModelDropdownCacheSnapshot>(json)
			?? throw new InvalidDataException("Model dropdown cache is invalid; refusing to overwrite the original.");
	}

	private static List<string> CopyNormalizedModelOptions(IEnumerable<string> options)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (options == null)
		{
			return list;
		}
		foreach (string option in options)
		{
			string text = NormalizeModelOption(option);
			if (!string.IsNullOrWhiteSpace(text) && !IsManualModelOption(text) && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static string ResolveSelectedOptionForSnapshot(string selectedOption, string manualModel, string fallbackModel, bool preserveBlankSelection)
	{
		string text = NormalizeModelOption(selectedOption);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = NormalizeModelOption(manualModel);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return preserveBlankSelection ? string.Empty : NormalizeModelOption(fallbackModel);
	}

	private static void MergeCachedDropdownState(List<string> runtimeOptions, Dropdown<string> runtimeDropdown, IEnumerable<string> cachedOptions, string cachedSelectedOption, string manualModel, string fallbackModel, bool preserveBlankSelection, out List<string> mergedOptions, out Dropdown<string> mergedDropdown)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		void AddMany(IEnumerable<string> source)
		{
			if (source == null)
			{
				return;
			}
			foreach (string item in source)
			{
				string text3 = NormalizeModelOption(item);
				if (!string.IsNullOrWhiteSpace(text3) && !IsManualModelOption(text3) && hashSet.Add(text3))
				{
					list.Add(text3);
				}
			}
		}
		AddMany(runtimeOptions);
		AddMany(ReadDropdownValues(runtimeDropdown));
		AddMany(cachedOptions);
		string text = ResolveHydratedSelectedModelOption(runtimeDropdown, cachedSelectedOption, manualModel, fallbackModel, preserveBlankSelection);
		mergedDropdown = BuildDropdownFromOptions(list, text, fallbackModel, preserveBlankSelection, out mergedOptions, out var _);
	}

	private static string ResolveHydratedSelectedModelOption(Dropdown<string> runtimeDropdown, string cachedSelectedOption, string manualModel, string fallbackModel, bool preserveBlankSelection)
	{
		string selected = ReadSelectedModelOption(runtimeDropdown);
		if (!string.IsNullOrWhiteSpace(selected)) return selected;
		string model = NormalizeModelOption(manualModel);
		return model.Length > 0 ? model : preserveBlankSelection ? string.Empty : NormalizeModelOption(fallbackModel);
	}

	private static string NormalizeModelOption(string value)
	{
		string option = (value ?? "").Trim();
		return option == "{=AF_MCM_Option_ModelManual}*manual entry*" ? ManualDropdownModelName : option;
	}

	private static bool IsManualModelOption(string value)
	{
		return string.Equals(NormalizeModelOption(value), ManualDropdownModelName, StringComparison.Ordinal);
	}

	private static bool ContainsModelOption(IEnumerable<string> options, string candidate)
	{
		string text = NormalizeModelOption(candidate);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (options == null)
		{
			return false;
		}
		foreach (string option in options)
		{
			if (string.Equals(NormalizeModelOption(option), text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRemovedMainModelPreset(string value)
	{
		string text = NormalizeModelOption(value);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		for (int i = 0; i < RemovedMainModelPresets.Length; i++)
		{
			if (string.Equals(text, RemovedMainModelPresets[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static List<string> FilterRemovedMainModelPresets(IEnumerable<string> source)
	{
		List<string> list = new List<string>();
		if (source == null)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in source)
		{
			string text = NormalizeModelOption(item);
			if (!string.IsNullOrWhiteSpace(text) && !IsRemovedMainModelPreset(text) && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static void AddModelOption(List<string> target, HashSet<string> seen, string value)
	{
		string text = NormalizeModelOption(value);
		if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
		{
			target.Add(text);
		}
	}

	private static string ReadSelectedModelOption(Dropdown<string> dropdown)
	{
		if (dropdown == null || dropdown.Count <= 0)
		{
			return null;
		}
		int selectedIndex = dropdown.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= dropdown.Count)
		{
			selectedIndex = 0;
		}
		return NormalizeModelOption(dropdown[selectedIndex]);
	}

	private static string ResolveSelectedModelOption(IEnumerable<string> cachedOptions, Dropdown<string> dropdown, string manualModel, string fallbackModel, bool preserveBlankSelection)
	{
		string text = ReadSelectedModelOption(dropdown);
		if (text != null)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return preserveBlankSelection ? string.Empty : NormalizeModelOption(fallbackModel);
			}
			return text;
		}
		string text2 = NormalizeModelOption(manualModel);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return preserveBlankSelection ? string.Empty : NormalizeModelOption(fallbackModel);
		}
		if (dropdown == null || dropdown.Count <= 0)
		{
			return text2;
		}
		if (string.Equals(text2, NormalizeModelOption(fallbackModel), StringComparison.OrdinalIgnoreCase) || ContainsModelOption(cachedOptions, text2))
		{
			return text2;
		}
		return ManualDropdownModelName;
	}

	private static string ResolveEffectiveModelName(IEnumerable<string> cachedOptions, Dropdown<string> dropdown, string manualModel, string fallbackModel, bool preserveBlankSelection)
	{
		string text = ResolveSelectedModelOption(cachedOptions, dropdown, manualModel, fallbackModel, preserveBlankSelection);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (IsManualModelOption(text))
		{
			return NormalizeModelOption(manualModel);
		}
		return text;
	}

	private static string ResolveSelectedOptionAfterFetch(IEnumerable<string> fetchedModels, string currentSelectedOption, string manualModel, string fallbackModel, bool preserveBlankSelection)
	{
		string text = NormalizeModelOption(currentSelectedOption);
		if (IsManualModelOption(text))
		{
			return ManualDropdownModelName;
		}
		if (!string.IsNullOrWhiteSpace(text) && ContainsModelOption(fetchedModels, text))
		{
			return text;
		}
		string text2 = NormalizeModelOption(manualModel);
		if (!string.IsNullOrWhiteSpace(text2) && ContainsModelOption(fetchedModels, text2))
		{
			return text2;
		}
		string text3 = NormalizeModelOption(fallbackModel);
		if (!string.IsNullOrWhiteSpace(text3) && ContainsModelOption(fetchedModels, text3))
		{
			return text3;
		}
		return preserveBlankSelection ? string.Empty : ManualDropdownModelName;
	}

	public static string NormalizeShoutInputUiBackground(string value)
	{
		string text = (value ?? "").Trim();
		if (string.Equals(text, ShoutInputUiBackgroundWhite, StringComparison.OrdinalIgnoreCase))
		{
			return ShoutInputUiBackgroundWhite;
		}
		if (string.Equals(text, ShoutInputUiBackgroundPink, StringComparison.OrdinalIgnoreCase))
		{
			return ShoutInputUiBackgroundPink;
		}
		return ShoutInputUiBackgroundBlack;
	}

	private static List<string> BuildPolicyApiSourceOptions()
	{
		return new List<string>
		{
			McmPromptDefaults.Raw("Mcm_PolicyApiSourceMain"),
			McmPromptDefaults.Raw("Mcm_PolicyApiSourcePreprocess"),
			McmPromptDefaults.Raw("Mcm_PolicyApiSourcePostprocess"),
			McmPromptDefaults.Raw("Mcm_PolicyApiSourceWeekly")
		};
	}

	private static List<string> BuildPlayerPolicyAutoDraftApiSourceOptions()
	{
		return new List<string>
		{
			McmPromptDefaults.Raw("Mcm_PolicyApiSourceMain"),
			McmPromptDefaults.Raw("Mcm_PolicyApiSourcePreprocess"),
			McmPromptDefaults.Raw("Mcm_PolicyApiSourcePostprocess")
		};
	}

	private static Dropdown<string> BuildPlayerPolicyAutoDraftApiSourceDropdown(int selectedIndex)
	{
		List<string> options = BuildPlayerPolicyAutoDraftApiSourceOptions();
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 1;
		}
		return new Dropdown<string>(BuildPolicyApiSourceDisplayOptions(options.Count), selectedIndex);
	}

	private static string ReadPlayerPolicyAutoDraftApiSourceSelection(Dropdown<string> dropdown)
	{
		int selectedIndex = dropdown?.SelectedIndex ?? 1;
		switch (selectedIndex)
		{
		case 0:
			return PolicyApiSourceMain;
		case 2:
			return PolicyApiSourceActionPostprocess;
		default:
			return PolicyApiSourceAuxiliary;
		}
	}

	internal static IReadOnlyList<string> BuildPlayerPolicyAutoDraftApiSourceOptionsForContractTests()
	{
		return BuildPlayerPolicyAutoDraftApiSourceOptions();
	}

	internal static string ReadPlayerPolicyAutoDraftApiSourceSelectionForContractTests(int? selectedIndex)
	{
		if (!selectedIndex.HasValue || selectedIndex.Value < 0 || selectedIndex.Value > 2)
		{
			return PolicyApiSourceAuxiliary;
		}
		return selectedIndex.Value == 0
			? PolicyApiSourceMain
			: selectedIndex.Value == 2
				? PolicyApiSourceActionPostprocess
				: PolicyApiSourceAuxiliary;
	}

	internal static string ReadPlayerPolicyApiSourceSelectionForContractTests(int selectedIndex)
	{
		return ReadPolicyApiSourceSelection(BuildPolicyApiSourceDropdown(selectedIndex), 0);
	}

	private static Dropdown<string> BuildPolicyApiSourceDropdown(int selectedIndex)
	{
		List<string> options = BuildPolicyApiSourceOptions();
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 0;
		}
		return new Dropdown<string>(BuildPolicyApiSourceDisplayOptions(options.Count), selectedIndex);
	}

	private static List<string> BuildPolicyApiSourceDisplayOptions(int count)
	{
		var options = new List<string>
		{
			"{=AF_MCM_Option_ApiMain}Main API",
			"{=AF_MCM_Option_ApiPreprocess}Preprocessing API",
			"{=AF_MCM_Option_ApiPostprocess}Postprocessing API"
		};
		if (count == 4) options.Add("{=AF_MCM_Option_ApiWeekly}Weekly report API");
		return options;
	}

	private static Dropdown<string> BuildFixedDisplayDropdown(Dropdown<string> incoming, int defaultIndex, params string[] options)
	{
		int index = incoming?.SelectedIndex ?? defaultIndex;
		if (index < 0 || index >= options.Length) index = defaultIndex;
		return new Dropdown<string>(options, index);
	}

	private static string ReadPolicyApiSourceSelection(Dropdown<string> dropdown, int defaultIndex)
	{
		List<string> options = BuildPolicyApiSourceOptions();
		int selectedIndex = dropdown?.SelectedIndex ?? defaultIndex;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = defaultIndex;
		}
		switch (selectedIndex)
		{
		case 1:
			return PolicyApiSourceAuxiliary;
		case 2:
			return PolicyApiSourceActionPostprocess;
		case 3:
			return PolicyApiSourceEventAndRebellion;
		default:
			return PolicyApiSourceMain;
		}
	}

	private static Dropdown<string> BuildShoutInputUiBackgroundDropdown(string selectedValue)
	{
		List<string> options = new List<string>
		{
			ShoutInputUiBackgroundBlack,
			ShoutInputUiBackgroundWhite,
			ShoutInputUiBackgroundPink
		};
		string selected = NormalizeShoutInputUiBackground(selectedValue);
		int selectedIndex = options.FindIndex((string x) => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}
		return BuildShoutInputUiBackgroundDisplayDropdown(selectedIndex);
	}

	private static Dropdown<string> NormalizeShoutInputUiBackgroundDropdown(Dropdown<string> dropdown)
	{
		List<string> options = new List<string>
		{
			ShoutInputUiBackgroundBlack,
			ShoutInputUiBackgroundWhite,
			ShoutInputUiBackgroundPink
		};
		int selectedIndex = dropdown?.SelectedIndex ?? 0;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 0;
		}
		return BuildShoutInputUiBackgroundDisplayDropdown(selectedIndex);
	}

	private static Dropdown<string> BuildShoutInputUiBackgroundDisplayDropdown(int selectedIndex)
		=> new Dropdown<string>(new[]
		{
			"{=AF_MCM_Option_BackgroundBlack}Transparent black",
			"{=AF_MCM_Option_BackgroundWhite}Transparent white",
			"{=AF_MCM_Option_BackgroundPink}Transparent pink"
		}, selectedIndex);

	private static string ReadShoutInputUiBackgroundSelection(Dropdown<string> dropdown)
	{
		Dropdown<string> normalizedDropdown = NormalizeShoutInputUiBackgroundDropdown(dropdown);
		List<string> options = new List<string>
		{
			ShoutInputUiBackgroundBlack,
			ShoutInputUiBackgroundWhite,
			ShoutInputUiBackgroundPink
		};
		int selectedIndex = normalizedDropdown.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 0;
		}
		return options[selectedIndex];
	}

	private static List<string> BuildLogCleanupIntervalOptions()
	{
		return new List<string>
		{
			LogCleanupOff,
			LogCleanupOnStartup,
			LogCleanupEvery30Minutes,
			LogCleanupEveryHour,
			LogCleanupEvery6Hours,
			LogCleanupEveryDay,
			LogCleanupEvery3Days,
			LogCleanupEveryWeek
		};
	}

	private static Dropdown<string> BuildLogCleanupIntervalDropdown(string selectedValue)
	{
		List<string> options = BuildLogCleanupIntervalOptions();
		string selected = NormalizeLogCleanupIntervalSelection(selectedValue);
		int selectedIndex = options.FindIndex((string x) => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}
		return BuildLogCleanupIntervalDisplayDropdown(selectedIndex);
	}

	private static Dropdown<string> NormalizeLogCleanupIntervalDropdown(Dropdown<string> dropdown)
	{
		List<string> options = BuildLogCleanupIntervalOptions();
		int selectedIndex = dropdown?.SelectedIndex ?? 0;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 0;
		}
		return BuildLogCleanupIntervalDisplayDropdown(selectedIndex);
	}

	private static Dropdown<string> BuildLogCleanupIntervalDisplayDropdown(int selectedIndex)
		=> new Dropdown<string>(new[]
		{
			"{=AF_MCM_Option_CleanupOff}Off",
			"{=AF_MCM_Option_CleanupStartup}Each startup",
			"{=AF_MCM_Option_Cleanup30Minutes}Every 30 minutes",
			"{=AF_MCM_Option_CleanupHour}Every hour",
			"{=AF_MCM_Option_Cleanup6Hours}Every 6 hours",
			"{=AF_MCM_Option_CleanupDay}Every day",
			"{=AF_MCM_Option_Cleanup3Days}Every 3 days",
			"{=AF_MCM_Option_CleanupWeek}Every week"
		}, selectedIndex);

	private static string ReadLogCleanupIntervalSelection(Dropdown<string> dropdown)
	{
		Dropdown<string> normalizedDropdown = NormalizeLogCleanupIntervalDropdown(dropdown);
		List<string> options = BuildLogCleanupIntervalOptions();
		int selectedIndex = normalizedDropdown.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 0;
		}
		return options[selectedIndex];
	}

	public static string NormalizeLogCleanupIntervalSelection(string value)
	{
		string text = (value ?? "").Trim();
		if (string.Equals(text, LogCleanupOnStartup, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupOnStartup;
		}
		if (string.Equals(text, LogCleanupEvery30Minutes, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEvery30Minutes;
		}
		if (string.Equals(text, LogCleanupEveryHour, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEveryHour;
		}
		if (string.Equals(text, LogCleanupEvery6Hours, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEvery6Hours;
		}
		if (string.Equals(text, LogCleanupEveryDay, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEveryDay;
		}
		if (string.Equals(text, LogCleanupEvery3Days, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEvery3Days;
		}
		if (string.Equals(text, LogCleanupEveryWeek, StringComparison.OrdinalIgnoreCase))
		{
			return LogCleanupEveryWeek;
		}
		return LogCleanupOff;
	}

	private static List<string> BuildReasoningEffortOptions()
	{
		return new List<string>
		{
			ReasoningEffortLow,
			ReasoningEffortMedium,
			ReasoningEffortHigh,
			ReasoningEffortXHigh,
			ReasoningEffortMax,
			ReasoningEffortMinimal
		};
	}

	private static Dropdown<string> BuildReasoningEffortDropdown(string selectedValue)
	{
		List<string> options = BuildReasoningEffortOptions();
		string selected = NormalizeReasoningEffortSelection(selectedValue);
		int selectedIndex = options.FindIndex((string x) => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0)
		{
			selectedIndex = 2;
		}
		return new Dropdown<string>(options, selectedIndex);
	}

	private static Dropdown<string> NormalizeReasoningEffortDropdown(Dropdown<string> dropdown)
	{
		List<string> options = BuildReasoningEffortOptions();
		int selectedIndex = dropdown?.SelectedIndex ?? 2;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 2;
		}
		return new Dropdown<string>(options, selectedIndex);
	}

	private static string ReadReasoningEffortSelection(Dropdown<string> dropdown)
	{
		Dropdown<string> normalizedDropdown = NormalizeReasoningEffortDropdown(dropdown);
		List<string> options = BuildReasoningEffortOptions();
		int selectedIndex = normalizedDropdown.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= options.Count)
		{
			selectedIndex = 2;
		}
		return options[selectedIndex];
	}

	public static string NormalizeReasoningEffortSelection(string effort)
	{
		string text = (effort ?? "").Trim().ToLowerInvariant();
		switch (text)
		{
		case ReasoningEffortNone:
		case ReasoningEffortMinimal:
		case ReasoningEffortLow:
		case ReasoningEffortMedium:
		case ReasoningEffortHigh:
		case ReasoningEffortXHigh:
		case ReasoningEffortMax:
			return text;
		default:
			return ReasoningEffortHigh;
		}
	}

	public static string NormalizeReasoningEffortForRequest(string effort)
	{
		switch (NormalizeReasoningEffortSelection(effort))
		{
		case ReasoningEffortXHigh:
		case ReasoningEffortMax:
			return ReasoningEffortMax;
		default:
			return ReasoningEffortHigh;
		}
	}

	public static string ResolveThinkingControlFormat(string apiUrl, string modelName)
	{
		if (YjThinkingCompat.IsYjGeminiEndpoint(apiUrl, modelName))
		{
			return "yj";
		}
		string source = ((apiUrl ?? "") + " " + (modelName ?? "")).Trim();
		if (source.IndexOf("anthropic", StringComparison.OrdinalIgnoreCase) >= 0 || source.IndexOf("claude", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "anthropic";
		}
		string model = (modelName ?? "").Trim();
		if (model.IndexOf("glm-4.5", StringComparison.OrdinalIgnoreCase) >= 0
			|| model.IndexOf("glm-4.6", StringComparison.OrdinalIgnoreCase) >= 0
			|| model.IndexOf("glm-4.7", StringComparison.OrdinalIgnoreCase) >= 0
			|| model.IndexOf("glm-5", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "glm";
		}
		if (source.IndexOf("deepseek", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "openai";
		}
		return "plain";
	}

	public static bool ApplyThinkingControls(JObject payload, string apiUrl, string modelName, bool thinkingEnabled, string effort, out string thinkingMode)
	{
		thinkingMode = "plain";
		if (payload == null)
		{
			return false;
		}
		if (YjThinkingCompat.TryApply(payload, apiUrl, modelName, thinkingEnabled, effort, out thinkingMode))
		{
			return true;
		}
		string format = ResolveThinkingControlFormat(apiUrl, modelName);
		if (format == "plain")
		{
			return false;
		}
		string normalizedEffort = NormalizeReasoningEffortForRequest(effort);
		if (string.Equals(NormalizeReasoningEffortSelection(effort), ReasoningEffortNone, StringComparison.OrdinalIgnoreCase))
		{
			payload["thinking"] = new JObject
			{
				["type"] = "disabled"
			};
			payload.Remove("reasoning_effort");
			payload.Remove("output_config");
			thinkingMode = format + "_thinking_disabled";
			return true;
		}
		payload["thinking"] = new JObject
		{
			["type"] = thinkingEnabled ? "enabled" : "disabled"
		};
		if (thinkingEnabled)
		{
			if (format == "anthropic")
			{
				payload["output_config"] = new JObject
				{
					["effort"] = normalizedEffort
				};
			}
			else if (format == "openai")
			{
				payload["reasoning_effort"] = normalizedEffort;
			}
		}
		else
		{
			payload.Remove("reasoning_effort");
			payload.Remove("output_config");
		}
		thinkingMode = format + "_" + (thinkingEnabled ? ("thinking_" + normalizedEffort) : "thinking_disabled");
		return true;
	}

	public static void RemoveThinkingControls(JObject payload)
	{
		if (payload == null)
		{
			return;
		}
		payload.Remove("thinking");
		payload.Remove("reasoning_effort");
		payload.Remove("output_config");
	}

	private static List<string> BuildModelOptionList(IEnumerable<string> candidates, string selectedOption, string fallbackModel, bool preserveBlankSelection)
	{
		List<string> list = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (preserveBlankSelection)
		{
			list.Add(string.Empty);
			seen.Add(string.Empty);
		}
		list.Add(ManualDropdownModelName);
		seen.Add(ManualDropdownModelName);
		if (candidates != null)
		{
			foreach (string candidate in candidates)
			{
				if (string.IsNullOrWhiteSpace(NormalizeModelOption(candidate)) || IsManualModelOption(candidate))
				{
					continue;
				}
				AddModelOption(list, seen, candidate);
			}
		}
		AddModelOption(list, seen, selectedOption);
		if (list.Count == 0)
		{
			list.Add(preserveBlankSelection ? string.Empty : DefaultDropdownModelName);
		}
		return list;
	}

	private static int ResolveModelOptionIndex(List<string> options, string selectedOption)
	{
		if (options == null || options.Count == 0)
		{
			return 0;
		}
		string text = NormalizeModelOption(selectedOption);
		for (int i = 0; i < options.Count; i++)
		{
			if (string.Equals(options[i], text, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return 0;
	}

	private static List<string> ReadDropdownValues(Dropdown<string> dropdown)
	{
		List<string> list = new List<string>();
		if (dropdown == null)
		{
			return list;
		}
		for (int i = 0; i < dropdown.Count; i++)
		{
			list.Add(dropdown[i]);
		}
		return list;
	}

	private static Dropdown<string> BuildDropdownFromOptions(List<string> cachedOptions, string selectedOption, string fallbackModel, bool preserveBlankSelection, out List<string> normalizedOptions, out string normalizedSelectedOption)
	{
		normalizedOptions = BuildModelOptionList(cachedOptions, selectedOption, fallbackModel, preserveBlankSelection);
		int num = ResolveModelOptionIndex(normalizedOptions, selectedOption);
		normalizedSelectedOption = normalizedOptions[num];
		return BuildModelDisplayDropdown(normalizedOptions, num);
	}

	private static Dropdown<string> BuildModelDisplayDropdown(List<string> options, int selectedIndex)
	{
		var display = new List<string>(options.Count);
		foreach (string option in options)
			display.Add(IsManualModelOption(option) ? "{=AF_MCM_Option_ModelManual}*manual entry*" : option);
		return new Dropdown<string>(display, selectedIndex);
	}

	private static Dropdown<string> BuildDropdownFromIncoming(Dropdown<string> incoming, List<string> cachedOptions, string selectedOption, string fallbackModel, bool preserveBlankSelection, out List<string> normalizedOptions, out string normalizedSelectedOption)
	{
		if (incoming != null && incoming.Count > 0)
		{
			List<string> list = ReadDropdownValues(incoming);
			string text = ReadSelectedModelOption(incoming);
			normalizedOptions = BuildModelOptionList(list, text, fallbackModel, preserveBlankSelection);
			int num = ResolveModelOptionIndex(normalizedOptions, text);
			normalizedSelectedOption = normalizedOptions[num];
			return BuildModelDisplayDropdown(normalizedOptions, num);
		}
		return BuildDropdownFromOptions(cachedOptions, selectedOption, fallbackModel, preserveBlankSelection, out normalizedOptions, out normalizedSelectedOption);
	}

	// MCM UI edits SelectedIndex in-place; capture that value before the text field
	// is serialized. Manual mode keeps the independently entered text.
	private static string ReadModelNameForPreset(ref string name, Dropdown<string> dropdown)
	{
		string selected = ReadSelectedModelOption(dropdown);
		if (!string.IsNullOrWhiteSpace(selected) && !IsManualModelOption(selected)) name = selected;
		return name;
	}

	private static void SetModelNameForPreset(string value, ref string name, ref Dropdown<string> dropdown, ref List<string> options)
	{
		name = value ?? "";
		if (dropdown == null || dropdown.Count == 0) return;
		string selected = IsManualModelOption(ReadSelectedModelOption(dropdown)) ? ManualDropdownModelName : NormalizeModelOption(name);
		if (selected.Length == 0) selected = ManualDropdownModelName;
		dropdown = BuildDropdownFromOptions(options, selected, "", false, out options, out _);
	}

	// The real MCM JSON converter mutates and returns the getter's existing object.
	// Its nonzero index is not stable across catalog changes: the text name wins.
	// Index zero is the stable manual sentinel. A separate object carries its own name.
	private static Dropdown<string> ResolveLoadedModelDropdown(Dropdown<string> incoming, Dropdown<string> live, string storedName)
	{
		if (incoming == null || incoming.Count == 0 || !ReferenceEquals(incoming, live)) return incoming;
		string selected = incoming.SelectedIndex == 0 ? ManualDropdownModelName : NormalizeModelOption(storedName);
		if (selected.Length == 0) selected = ManualDropdownModelName;
		return BuildDropdownFromOptions(ReadDropdownValues(incoming), selected, "", false, out _, out _);
	}

	private string _mainApiModelName = "gpt-4o-mini";
	private string _auxiliaryApiModelName = "gpt-4o-mini";
	private string _actionPostprocessApiModelName = "";
	private string _eventAndRebellionApiModelName = "";
	private string _townAmbientAiModelName = "gpt-4o-mini";

	private static string BuildModelListApiUrl(string rawApiUrl)
	{
		return LlmApiCompat.BuildModelListApiUrl(rawApiUrl);
	}

	private static List<string> ParseModelListFromResponse(string responseBody)
	{
		List<string> list = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(responseBody))
		{
			return list;
		}
		try
		{
			void AppendFromArray(JToken token)
			{
				if (!(token is JArray jArray))
				{
					return;
				}
				foreach (JToken item in jArray)
				{
					if (item == null)
					{
						continue;
					}
					if (item.Type == JTokenType.String)
					{
						AddModelOption(list, seen, item.ToString());
						continue;
					}
					AddModelOption(list, seen, item["id"]?.ToString());
					AddModelOption(list, seen, item["name"]?.ToString());
					AddModelOption(list, seen, item["model"]?.ToString());
				}
			}

			JToken jToken = JToken.Parse(responseBody);
			AppendFromArray(jToken);
			AppendFromArray(jToken["data"]);
			AppendFromArray(jToken["models"]);
			AppendFromArray(jToken["result"]?["data"]);
			AppendFromArray(jToken["result"]?["models"]);
		}
		catch
		{
		}
		return list;
	}

	private static async Task<ModelListFetchResult> FetchModelListAsync(string rawApiUrl, string apiKey)
	{
		ModelListFetchResult modelListFetchResult = new ModelListFetchResult();
		try
		{
			modelListFetchResult.RequestUrl = BuildModelListApiUrl(rawApiUrl);
			if (string.IsNullOrWhiteSpace(modelListFetchResult.RequestUrl))
			{
				modelListFetchResult.ErrorCode = ModelCatalogErrorCodes.UrlMissing;
				modelListFetchResult.ErrorMessage = ModelCatalogErrorFormatter.Format(modelListFetchResult.ErrorCode, modelListFetchResult.ErrorArguments);
				return modelListFetchResult;
			}
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				modelListFetchResult.ErrorCode = ModelCatalogErrorCodes.ApiKeyMissing;
				modelListFetchResult.ErrorMessage = ModelCatalogErrorFormatter.Format(modelListFetchResult.ErrorCode, modelListFetchResult.ErrorArguments);
				return modelListFetchResult;
			}
			using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(ModelCatalogRequestTimeoutMilliseconds)))
			{
				ModelCatalogExchange exchange = await new LegacyModelCatalogGateway().FetchModelsAsync(rawApiUrl, apiKey, timeout.Token);
			modelListFetchResult.RequestUrl = exchange.RequestUrl;
			modelListFetchResult.ResponseBody = exchange.ResponseBody;
			if (exchange.Cancelled)
			{
				modelListFetchResult.ErrorCode = exchange.ErrorCode;
				modelListFetchResult.ErrorArguments = exchange.ErrorArguments;
				modelListFetchResult.ErrorMessage = ModelCatalogErrorFormatter.Format(modelListFetchResult.ErrorCode, modelListFetchResult.ErrorArguments, legacyMessage: exchange.ErrorMessage);
				return modelListFetchResult;
			}
			if (!exchange.HasStatusCode)
			{
				modelListFetchResult.ErrorCode = exchange.ErrorCode;
				modelListFetchResult.ErrorArguments = exchange.ErrorArguments;
				modelListFetchResult.ErrorMessage = ModelCatalogErrorFormatter.Format(modelListFetchResult.ErrorCode, modelListFetchResult.ErrorArguments, legacyMessage: exchange.ErrorMessage);
				return modelListFetchResult;
			}
			modelListFetchResult.StatusCode = (HttpStatusCode)exchange.StatusCode;
			if (!exchange.IsSuccessStatusCode)
			{
				modelListFetchResult.ErrorCode = exchange.ErrorCode;
				modelListFetchResult.ErrorArguments = exchange.ErrorArguments;
				modelListFetchResult.ErrorMessage = $"HTTP {exchange.StatusCode} {exchange.ReasonPhrase}";
				return modelListFetchResult;
			}
			modelListFetchResult.Models = ParseModelListFromResponse(modelListFetchResult.ResponseBody);
			if (modelListFetchResult.Models.Count == 0)
			{
				modelListFetchResult.ErrorMessage = Ui("AF_MCM_Api_EmptyModelList", "The API request succeeded, but the model list was empty or failed to parse.");
				return modelListFetchResult;
			}
			modelListFetchResult.Success = true;
			return modelListFetchResult;
			}
		}
		catch (Exception ex)
		{
			modelListFetchResult.ErrorCode = ModelCatalogErrorCodes.TransportFailed;
			modelListFetchResult.ErrorArguments = new Dictionary<string, string>
			{
				["exceptionType"] = ex.GetType().Name
			};
			modelListFetchResult.ErrorMessage = ModelCatalogErrorFormatter.Format(
				modelListFetchResult.ErrorCode,
				modelListFetchResult.ErrorArguments,
				legacyMessage: ex.Message);
			return modelListFetchResult;
		}
	}

	private void ApplyMainModelList(List<string> models)
	{
		EnsureModelDropdownCacheHydrated();
		List<string> list = FilterRemovedMainModelPresets(models ?? new List<string>());
		string selectedOption = ResolveSelectedOptionAfterFetch(list, GetMainSelectedModelOption(), ModelName, DefaultDropdownModelName, preserveBlankSelection: false);
		if (IsRemovedMainModelPreset(selectedOption))
		{
			selectedOption = ManualDropdownModelName;
		}
		_mainApiModelDropdown = BuildDropdownFromOptions(list, selectedOption, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
	}

	private void ApplyAuxiliaryModelList(List<string> models)
	{
		EnsureModelDropdownCacheHydrated();
		List<string> list = models ?? new List<string>();
		string selectedOption = ResolveSelectedOptionAfterFetch(list, GetAuxiliarySelectedModelOption(), AuxiliaryModelName, "", preserveBlankSelection: false);
		_auxiliaryApiModelDropdown = BuildDropdownFromOptions(list, selectedOption, "", preserveBlankSelection: false, out _auxiliaryApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
	}

	private void ApplyActionPostprocessModelList(List<string> models)
	{
		EnsureModelDropdownCacheHydrated();
		List<string> list = models ?? new List<string>();
		string selectedOption = ResolveSelectedOptionAfterFetch(list, GetActionPostprocessSelectedModelOption(), ActionPostprocessModelName, "", preserveBlankSelection: false);
		_actionPostprocessApiModelDropdown = BuildDropdownFromOptions(list, selectedOption, "", preserveBlankSelection: false, out _actionPostprocessApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
	}

	private void ApplyEventAndRebellionModelList(List<string> models)
	{
		EnsureModelDropdownCacheHydrated();
		List<string> list = models ?? new List<string>();
		string selectedOption = ResolveSelectedOptionAfterFetch(list, GetEventAndRebellionSelectedModelOption(), EventAndRebellionModelName, "", preserveBlankSelection: false);
		_eventAndRebellionApiModelDropdown = BuildDropdownFromOptions(list, selectedOption, "", preserveBlankSelection: false, out _eventAndRebellionApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
	}

	public void ForceMainModelDropdownToManual()
	{
		EnsureModelDropdownCacheHydrated();
		_mainApiModelDropdown = BuildDropdownFromOptions(_mainApiModelOptions, ManualDropdownModelName, DefaultDropdownModelName, preserveBlankSelection: false, out _mainApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
		ModelDropdownsDirty = true;
		OnModelListRefreshed?.Invoke();
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void ForceAuxiliaryModelDropdownToManual()
	{
		EnsureModelDropdownCacheHydrated();
		_auxiliaryApiModelDropdown = BuildDropdownFromOptions(_auxiliaryApiModelOptions, ManualDropdownModelName, "", preserveBlankSelection: false, out _auxiliaryApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
		ModelDropdownsDirty = true;
		OnModelListRefreshed?.Invoke();
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void ForceActionPostprocessModelDropdownToManual()
	{
		EnsureModelDropdownCacheHydrated();
		_actionPostprocessApiModelDropdown = BuildDropdownFromOptions(_actionPostprocessApiModelOptions, ManualDropdownModelName, "", preserveBlankSelection: false, out _actionPostprocessApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
		ModelDropdownsDirty = true;
		OnModelListRefreshed?.Invoke();
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void ForceEventAndRebellionModelDropdownToManual()
	{
		EnsureModelDropdownCacheHydrated();
		_eventAndRebellionApiModelDropdown = BuildDropdownFromOptions(_eventAndRebellionApiModelOptions, ManualDropdownModelName, "", preserveBlankSelection: false, out _eventAndRebellionApiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
		ModelDropdownsDirty = true;
		OnModelListRefreshed?.Invoke();
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void SetMainApiReasoningEffortForExternal(string effort)
	{
		MainApiReasoningEffort = NormalizeReasoningEffortSelection(effort);
		_mainApiReasoningEffortDropdown = BuildReasoningEffortDropdown(MainApiReasoningEffort);
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void SetAuxiliaryApiReasoningEffortForExternal(string effort)
	{
		AuxiliaryApiReasoningEffort = NormalizeReasoningEffortSelection(effort);
		_auxiliaryApiReasoningEffortDropdown = BuildReasoningEffortDropdown(AuxiliaryApiReasoningEffort);
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void SetActionPostprocessApiReasoningEffortForExternal(string effort)
	{
		ActionPostprocessApiReasoningEffort = NormalizeReasoningEffortSelection(effort);
		_actionPostprocessApiReasoningEffortDropdown = BuildReasoningEffortDropdown(ActionPostprocessApiReasoningEffort);
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public void SetEventAndRebellionApiReasoningEffortForExternal(string effort)
	{
		EventAndRebellionApiReasoningEffort = NormalizeReasoningEffortSelection(effort);
		_eventAndRebellionApiReasoningEffortDropdown = BuildReasoningEffortDropdown(EventAndRebellionApiReasoningEffort);
		McmDropdownRuntimeRefresh.RequestRefresh();
	}

	public string GetMainSelectedModelOption()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveSelectedModelOption(_mainApiModelOptions, _mainApiModelDropdown, ModelName, DefaultDropdownModelName, preserveBlankSelection: false);
	}

	public string GetAuxiliarySelectedModelOption()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveSelectedModelOption(_auxiliaryApiModelOptions, _auxiliaryApiModelDropdown, AuxiliaryModelName, "", preserveBlankSelection: false);
	}

	public string GetActionPostprocessSelectedModelOption()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveSelectedModelOption(_actionPostprocessApiModelOptions, _actionPostprocessApiModelDropdown, ActionPostprocessModelName, "", preserveBlankSelection: false);
	}

	public string GetEventAndRebellionSelectedModelOption()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveSelectedModelOption(_eventAndRebellionApiModelOptions, _eventAndRebellionApiModelDropdown, EventAndRebellionModelName, "", preserveBlankSelection: false);
	}

	public string GetEffectiveMainModelName()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveEffectiveModelName(_mainApiModelOptions, _mainApiModelDropdown, ModelName, DefaultDropdownModelName, preserveBlankSelection: false);
	}

	public string GetEffectiveAuxiliaryModelName()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveEffectiveModelName(_auxiliaryApiModelOptions, _auxiliaryApiModelDropdown, AuxiliaryModelName, "", preserveBlankSelection: false);
	}

	public string GetEffectiveActionPostprocessModelName()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveEffectiveModelName(_actionPostprocessApiModelOptions, _actionPostprocessApiModelDropdown, ActionPostprocessModelName, "", preserveBlankSelection: false);
	}

	public string GetEffectiveEventAndRebellionModelName()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveEffectiveModelName(_eventAndRebellionApiModelOptions, _eventAndRebellionApiModelDropdown, EventAndRebellionModelName, "", preserveBlankSelection: false);
	}

	private static string GetChannelDisplayName(string channelName)
	{
		if (string.Equals(channelName, McmPromptDefaults.Raw("Mcm_ChannelMain"), StringComparison.Ordinal))
			return Ui("AF_MCM_Api_ChannelMain", "main API");
		if (string.Equals(channelName, McmPromptDefaults.Raw("Mcm_ChannelPreprocess"), StringComparison.Ordinal))
			return Ui("AF_MCM_Api_ChannelPreprocess", "preprocessing API");
		if (string.Equals(channelName, McmPromptDefaults.Raw("Mcm_ChannelPostprocess"), StringComparison.Ordinal))
			return Ui("AF_MCM_Api_ChannelPostprocess", "postprocessing API");
		if (string.Equals(channelName, McmPromptDefaults.Raw("Mcm_ChannelEventRebellion"), StringComparison.Ordinal))
			return Ui("AF_MCM_Api_ChannelEventRebellion", "event/rebellion API");
		if (string.Equals(channelName, McmPromptDefaults.Raw("Mcm_ChannelAmbient"), StringComparison.Ordinal))
			return Ui("AF_MCM_Api_ChannelAmbient", "ambient AI");
		return channelName;
	}

	private void BindModelListActions()
	{
		// Read this settings instance at click time, including unsaved MCM edits.
		FetchMainModelList = () => StartFetchModelList(McmPromptDefaults.Raw("Mcm_ChannelMain"), ApiUrl, ApiKey, ApplyMainModelList);
		FetchAuxiliaryModelList = () => StartFetchModelList(McmPromptDefaults.Raw("Mcm_ChannelPreprocess"), AuxiliaryApiUrl, AuxiliaryApiKey, ApplyAuxiliaryModelList);
		FetchActionPostprocessModelList = () => StartFetchModelList(McmPromptDefaults.Raw("Mcm_ChannelPostprocess"), ActionPostprocessApiUrl, ActionPostprocessApiKey, ApplyActionPostprocessModelList);
		FetchEventAndRebellionModelList = () => StartFetchModelList(McmPromptDefaults.Raw("Mcm_ChannelEventRebellion"), EventAndRebellionApiUrl, EventAndRebellionApiKey, ApplyEventAndRebellionModelList);
		FetchTownAmbientAiModelList = () => StartFetchModelList(McmPromptDefaults.Raw("Mcm_ChannelAmbient"), TownAmbientAiApiUrl, TownAmbientAiApiKey, ApplyTownAmbientAiModelList);
	}

	private void StartFetchModelList(string channelName, string rawApiUrl, string apiKey, Action<List<string>> applyModels)
	{
		LlmRetryPrompt.CaptureMainThreadContext();
		Task.Run(async delegate
		{
			try
			{
				string text = (channelName ?? "").Trim();
				string channelDisplayName = GetChannelDisplayName(text);
				string text2 = (rawApiUrl ?? "").Trim();
				string text3 = (apiKey ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					ShowLlmFailurePopup(UiFormat("AF_MCM_Api_ModelListFailureTitle", "{CHANNEL} model list fetch failed", ("CHANNEL", channelDisplayName)), Ui("AF_MCM_Api_AddressMissing", "The API address is empty, so the model list cannot be fetched."));
					return;
				}
				if (string.IsNullOrWhiteSpace(text3))
				{
					ShowLlmFailurePopup(UiFormat("AF_MCM_Api_ModelListFailureTitle", "{CHANNEL} model list fetch failed", ("CHANNEL", channelDisplayName)), Ui("AF_MCM_Api_KeyMissing", "The API key is empty, so the model list cannot be fetched."));
					return;
				}
				McmDropdownRuntimeRefresh.PostToMainThread(delegate
				{
					InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Api_ModelListFetching", "[System] Fetching the {CHANNEL} model list...", ("CHANNEL", channelDisplayName)), Color.FromUint(4294967040u)));
				});
				ModelListFetchResult modelListFetchResult = await FetchModelListAsync(text2, text3);
				if (!modelListFetchResult.Success)
				{
					string text4 = ModelCatalogErrorFormatter.Format(modelListFetchResult.ErrorCode, modelListFetchResult.ErrorArguments, legacyMessage: modelListFetchResult.ErrorMessage ?? Ui("AF_MCM_Api_UnknownError", "Unknown error."));
					if ((int)modelListFetchResult.StatusCode > 0)
					{
						string text5 = BuildApiErrorHint(modelListFetchResult.RequestUrl, "", modelListFetchResult.StatusCode, modelListFetchResult.ResponseBody);
						if (!string.IsNullOrWhiteSpace(text5))
						{
							text4 = text4 + "；" + text5;
						}
					}
					McmDropdownRuntimeRefresh.PostToMainThread(delegate
					{
						ShowLlmFailurePopup(UiFormat("AF_MCM_Api_ModelListFailureTitle", "{CHANNEL} model list fetch failed", ("CHANNEL", channelDisplayName)), text4, "", modelListFetchResult.ResponseBody);
						Logger.Log("DuelSettings", "[" + text + "] Failed to fetch the model list: " + text4 + " | url=" + (modelListFetchResult.RequestUrl ?? ""));
					});
					return;
				}
				McmDropdownRuntimeRefresh.PostToMainThread(delegate
				{
				applyModels?.Invoke(modelListFetchResult.Models);
				ModelDropdownsDirty = true;
				OnModelListRefreshed?.Invoke();
				McmDropdownRuntimeRefresh.RequestRefresh();
				string text6 = "";
				if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelMain"), StringComparison.Ordinal))
				{
					text6 = GetMainSelectedModelOption();
				}
				else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelPreprocess"), StringComparison.Ordinal))
				{
					text6 = GetAuxiliarySelectedModelOption();
				}
				else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelPostprocess"), StringComparison.Ordinal))
				{
					text6 = GetActionPostprocessSelectedModelOption();
				}
				else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelEventRebellion"), StringComparison.Ordinal))
				{
					text6 = GetEventAndRebellionSelectedModelOption();
				}
				else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelAmbient"), StringComparison.Ordinal))
				{
					text6 = GetTownAmbientAiSelectedModelOption();
				}
				if (IsManualModelOption(text6))
				{
					string text7 = "";
					if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelMain"), StringComparison.Ordinal))
					{
						text7 = NormalizeModelOption(ModelName);
					}
					else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelPreprocess"), StringComparison.Ordinal))
					{
						text7 = NormalizeModelOption(AuxiliaryModelName);
					}
					else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelPostprocess"), StringComparison.Ordinal))
					{
						text7 = NormalizeModelOption(ActionPostprocessModelName);
					}
					else if (string.Equals(text, McmPromptDefaults.Raw("Mcm_ChannelEventRebellion"), StringComparison.Ordinal))
					{
						text7 = NormalizeModelOption(EventAndRebellionModelName);
					}
					text6 = Ui("AF_MCM_Option_ModelManual", "*manual entry*") + " -> " + (string.IsNullOrWhiteSpace(text7) ? Ui("AF_MCM_Option_ModelEmpty", "(empty)") : text7);
				}
				InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Api_ModelListFetched", "[System] {CHANNEL} model list fetched: {COUNT} models; selected: {SELECTED}", ("CHANNEL", channelDisplayName), ("COUNT", modelListFetchResult.Models.Count), ("SELECTED", string.IsNullOrWhiteSpace(text6) ? Ui("AF_MCM_Api_EmptySelection", "empty") : text6)), Color.FromUint(4278255360u)));
				Logger.Log("DuelSettings", "[" + text + "] Model list fetched successfully: count=" + modelListFetchResult.Models.Count + " url=" + (modelListFetchResult.RequestUrl ?? ""));
				});
			}
			catch (Exception ex)
			{
				ShowLlmFailurePopup(Ui("AF_MCM_Api_ModelListExceptionTitle", "Model list fetch error"), ex.Message);
				Logger.Log("DuelSettings", "[Model list fetch exception] " + ex);
			}
		});
	}

	public static string GetEffectiveApiUrl(string rawUrl)
	{
		return LlmApiCompat.GetEffectiveChatApiUrl(rawUrl);
	}

	public static bool ShouldWarnForContextExtractionApi(string rawUrl)
	{
		string text = (rawUrl ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				return false;
			}
			string text2 = (result.Host ?? "").Trim().ToLowerInvariant();
			if (text2 == "ark.cn-beijing.volces.com")
			{
				string text3 = (result.AbsolutePath ?? "").Trim();
				return text3.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
			}
		}
		catch
		{
		}
		return false;
	}

	public static string GetContextExtractionCompatibilityWarningMessage()
	{
		return Ui("AF_MCM_Api_ContextExtractionWarning", "The model used by this endpoint does not meet AnimusForge's context-extraction requirements. You can continue, but the mod is not responsible for unreasonable replies produced after using it.");
	}

	private static string Ui(string id, string englishFallback)
	{
		return DuelSettingsUiLocalization.Text(id, englishFallback);
	}

	private static string UiFormat(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		return DuelSettingsUiLocalization.Format(id, englishFallback, variables);
	}

	private static string TryExtractAssistantReplyText(string responseString)
	{
		return LlmApiCompat.ExtractAssistantText(responseString);
	}

	private static Task<ConfiguredChatValidationExchange> SendConnectionTestRequestAsync(string providerId, string endpoint, string model, string apiKey, JObject payload, CancellationToken cancellationToken)
	{
		int maxTokens = payload?["max_tokens"]?.Value<int>() ?? ApiMaxTokensMinimum;
		LlmProviderSnapshot provider = new LlmProviderSnapshot(
			providerId,
			(endpoint ?? "").Trim(),
			(model ?? "").Trim(),
			LlmRequestTimeoutMilliseconds,
			Math.Max(1, maxTokens));
		LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => apiKey ?? "", disableThinking: true);
		return gateway.SendValidationAsync(provider, payload, cancellationToken);
	}

	private static string BuildApiErrorHint(string effectiveApiUrl, string modelName, HttpStatusCode statusCode, string responseBody)
	{
		if ((int)statusCode == 522)
		{
			return Ui("AF_MCM_Api_Hint522", "522 usually means that the gateway or proxy received your request but could not connect to the upstream server. If you use a self-hosted relay, a Cloudflare domain, or a third-party panel, check that the upstream is online and that the proxy can reach it.");
		}
		if (statusCode != HttpStatusCode.NotFound)
		{
			return null;
		}
		string text = (responseBody ?? "").ToLowerInvariant();
		if (text.Contains("requested entity was not found") || text.Contains("\"status\": \"not_found\""))
		{
			return Ui("AF_MCM_Api_Hint404WithBody", "404 NotFound usually means that the endpoint path or model name does not exist. Check the API address, the automatically completed chat path, and the model name.");
		}
		return Ui("AF_MCM_Api_Hint404", "404 NotFound usually means that the endpoint path or model name does not exist. Check the API URL suffix and the model name.");
	}

	private static Task<ConfiguredChatValidationExchange> SendConnectionTestJsonRequestAsync(string providerId, string endpoint, string model, string apiKey, string preparedJson, CancellationToken cancellationToken)
	{
		LlmProviderSnapshot provider = new LlmProviderSnapshot(
			providerId,
			(endpoint ?? "").Trim(),
			(model ?? "").Trim(),
			LlmRequestTimeoutMilliseconds,
			256);
		LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => apiKey ?? "", disableThinking: true);
		return gateway.SendValidationJsonAsync(provider, preparedJson, cancellationToken);
	}

	private static void ShowLlmFailurePopup(string title, string reason, string modelReply = null, string rawResponse = null)
	{
		LlmRetryPrompt.ShowFailurePopup(title, LlmRetryPrompt.BuildFailureDetail(reason, modelReply, rawResponse));
	}

	public DuelSettings()
	{
		BindModelListActions();
		OpenAfdianSupportLink = delegate
		{
			OpenAfdianSupportPage();
		};
		EditPlayerCustomPromptRule = delegate
		{
			OpenPlayerCustomPromptRuleEditor();
		};
		EditKingdomRebellionSystemPrompt = delegate
		{
			OpenKingdomRebellionSystemPromptEditor();
		};
		EditWorldBulletinWritingRequirements = OpenWorldBulletinWritingRequirementsEditor;
		EditWeeklyReportWritingRequirements = delegate
		{
			OpenWeeklyReportWritingRequirementsEditor();
		};
		EditNpcPersonaGenerationRequirements = delegate
		{
			OpenNpcPersonaGenerationRequirementsEditor();
		};
		EditDailyMemoryCompressionWritingRequirements = delegate
		{
			OpenDailyMemoryCompressionWritingRequirementsEditor();
		};
		EditMemoryOverviewCompressionWritingRequirements = delegate
		{
			OpenMemoryOverviewCompressionWritingRequirementsEditor();
		};
		EditMajorActionCompressionWritingRequirements = delegate
		{
			OpenMajorActionCompressionWritingRequirementsEditor();
		};
		EditCustomPolicyEvaluatorPrompt = delegate
		{
			OpenCustomPolicyEvaluatorPromptEditor();
		};
		EditNpcRulerPolicyPrompt = delegate
		{
			OpenNpcRulerPolicyPromptEditor();
		};
		EditPolicyPrompts = delegate
		{
			PolicyPromptEditorUi.Open(this);
		};
		ManagePolicyEffectModules = delegate
		{
			PolicyEffectModuleManagerUi.Open();
		};
		EditSettlementBalance = SettlementBalancePopup.Open;
		EditWorldDiplomacyPrompt = delegate
		{
			OpenWorldDiplomacyPromptEditor();
		};
		RestoreDefaultWorldDiplomacyPrompt = delegate
		{
			ConfirmRestoreDefaultWorldDiplomacyPrompt();
		};
		OpenCustomPromptTextStoreFolderAction = delegate
		{
			OpenCustomPromptTextStoreFolder();
		};
		ExportGcczDebugLog = delegate
		{
			ExportGcczDebugLogToDesktop();
		};
		ClearGcczDebugLog = delegate
		{
			ClearGcczDebugLogNow();
		};
		ExportSetsDiagnosticLog = delegate
		{
			ExportSetsDiagnosticLogToDesktop();
		};
		ClearSetsDiagnosticLog = delegate
		{
			ClearSetsDiagnosticLogNow();
		};
		ExportNoblePrisonerEscortLog = delegate
		{
			ExportNoblePrisonerEscortLogToDesktop();
		};
		ClearNoblePrisonerEscortLog = delegate
		{
			ClearNoblePrisonerEscortLogNow();
		};
		TestTtsVolcDedicatedVoice = delegate
		{
			try
			{
				DuelSettings runtimeSettings = GetSettings() ?? this;
				if (!ReferenceEquals(runtimeSettings, this))
				{
					bool mismatch = !string.Equals((TtsVolcDedicatedApiUrl ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedApiUrl ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedApiKey ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedApiKey ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedAppKey ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedAppKey ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedResourceId ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedResourceId ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedSpeaker ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedSpeaker ?? "").Trim(), StringComparison.Ordinal);
					if (mismatch)
					{
						Logger.Log("DuelSettings", "[WARN] Current MCM edits differ from runtime settings; the voice test will use runtime settings. Save the settings first.");
					}
				}
				TtsEngine instance = TtsEngine.Instance;
				if (instance == null || !instance.IsReady)
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_NotReady", "[TTS] The engine is not initialized, so it cannot be tested."), Color.FromUint(4294901760u)));
				}
				else if (!runtimeSettings.EnableTtsSpeech)
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_EnableSpeech", "[TTS] Enable TTS speech first."), Color.FromUint(4294901760u)));
				}
				else if (!runtimeSettings.TtsVolcDedicatedEnabled)
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_EnableVolc", "[TTS] Enable Volcengine dedicated mode first."), Color.FromUint(4294901760u)));
				}
				else if (string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedApiUrl))
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_MissingUrl", "[TTS] Enter the Volcengine dedicated API URL first."), Color.FromUint(4294901760u)));
				}
				else if (VolcTtsGateway.GetVersion(runtimeSettings.TtsVolcDedicatedApiUrl) == 0)
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] " + TtsErrorUiText.Describe("tts_endpoint_unsupported"), Color.FromUint(4294901760u)));
				}
				else if (VolcTtsGateway.GetVersion(runtimeSettings.TtsVolcDedicatedApiUrl) == 1 && string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedAppKey))
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_MissingAppId", "[TTS] Enter the Volcengine dedicated App ID first."), Color.FromUint(4294901760u)));
				}
				else if (string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedResourceId))
				{
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_MissingResource", "[TTS] Enter the Volcengine dedicated resource ID first."), Color.FromUint(4294901760u)));
				}
				else
				{
					if (instance.SpeakTestAsync(Ui("AF_MCM_Tts_TestPhrase", "At your service, traveler!"), runtimeSettings.TtsVolcDedicatedSpeed, TtsTestFeedback.ReportFailure))
					{
						InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Tts_TestingVersion", "[TTS] Testing Volcengine V{VERSION}... (scene channel={CHANNEL}, main voice volume={VOLUME}, speed={SPEED}, lip-sync volume={LIP_VOLUME})", ("VERSION", VolcTtsGateway.GetVersion(runtimeSettings.TtsVolcDedicatedApiUrl)), ("CHANNEL", runtimeSettings.TtsSceneUseWinmmAudible ? "winmm" : Ui("AF_MCM_Tts_LipSyncChannel", "lip-sync")), ("VOLUME", runtimeSettings.TtsVolcDedicatedVolume.ToString("F2", CultureInfo.InvariantCulture)), ("SPEED", runtimeSettings.TtsVolcDedicatedSpeed.ToString("F2", CultureInfo.InvariantCulture)), ("LIP_VOLUME", runtimeSettings.TtsLipSyncSoundEventVolume.ToString("F2", CultureInfo.InvariantCulture))), Color.FromUint(4294967040u)));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Tts_TestNotQueued", "[TTS] Test voice was not queued: the queue is full or the engine has stopped. Try again later."), Color.FromUint(4294901760u)));
					}
				}
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Tts_TestException", "[TTS] Volcengine test error: {ERROR}", ("ERROR", ex.Message)), Color.FromUint(4294901760u)));
			}
		};
		TestConnection = delegate
		{
			LlmRetryPrompt.CaptureMainThreadContext();
			ConnectionTestText testText = ConnectionTestText.Capture("Main");
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", testText.Started);
					string effectiveModelName = GetEffectiveMainModelName();
					if (string.IsNullOrWhiteSpace(ApiKey))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_MainTestFailed", "Main API connection test failed"), Ui("AF_MCM_Api_MainKeyMissing", "The main API key is empty."));
					}
					else if (string.IsNullOrWhiteSpace(effectiveModelName))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_MainTestFailed", "Main API connection test failed"), Ui("AF_MCM_Api_MainModelMissing", "The model name is empty. If you selected “*manual entry*” in the dropdown, enter the model name in the text box above."));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_MainTesting", "[System] Calling the main API..."), Color.FromUint(4294967040u)));
						if (ShouldWarnForContextExtractionApi(ApiUrl))
						{
							InformationManager.DisplayMessage(new InformationMessage(GetContextExtractionCompatibilityWarningMessage(), Color.FromUint(4294936576u)));
						}
						string effectiveApiUrl = GetEffectiveApiUrl(ApiUrl);
						JObject requestPayload = new JObject
						{
							["model"] = effectiveModelName,
							["messages"] = new JArray
							{
								new JObject
								{
									["role"] = "user",
									["content"] = testText.PromptUser
								}
							},
							["stream"] = false
						};
						requestPayload["temperature"] = GetMainApiTemperature();
						ApplyThinkingControls(requestPayload, effectiveApiUrl, effectiveModelName, MainApiThinkingEnabled, GetMainApiReasoningEffort(), out var _);
						string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, requestPayload);
						ConfiguredChatValidationExchange response = await SendConnectionTestRequestAsync("main_validation", effectiveApiUrl, effectiveModelName, ApiKey, requestPayload, CancellationToken.None);
						string responseString = response.ResponseBody;
						if (response.Result.Status == LlmResultStatus.Succeeded)
						{
							string aiReply = TryExtractAssistantReplyText(responseString);
							if (!string.IsNullOrWhiteSpace(aiReply))
							{
								InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_MainSuccess", "Connection is working. Main API reply: ") + aiReply.Trim(), Color.FromUint(4278255360u)));
								Logger.Log("DuelSettings", testText.SuccessPrefix + aiReply);
							}
							else
							{
								ShowLlmFailurePopup(Ui("AF_MCM_Api_MainReplyParseFailed", "Main API reply parsing failed"), Ui("AF_MCM_Api_ReplyMissing", "The connection succeeded, but no model reply could be parsed."), "", responseString);
							}
						}
						else
						{
							HttpStatusCode statusCode = (HttpStatusCode)response.StatusCode;
							string text = BuildApiErrorHint(effectiveApiUrl, effectiveModelName, statusCode, responseString);
							string reason = UiFormat("AF_MCM_Api_StatusReason", "Connection failed with status code {STATUS}.", ("STATUS", statusCode)) + (string.IsNullOrWhiteSpace(text) ? "" : ("\n" + Ui("AF_MCM_Api_Troubleshooting", "Troubleshooting suggestion: ") + text));
							ShowLlmFailurePopup(Ui("AF_MCM_Api_MainTestFailed", "Main API connection test failed"), reason, "", responseString);
							Logger.Log("DuelSettings", testText.Failure(statusCode, responseString));
						}
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					ShowLlmFailurePopup(Ui("AF_MCM_Api_MainTestException", "Main API connection test error"), ex2.Message);
					Logger.Log("DuelSettings", testText.ExceptionPrefix + ex2.Message);
				}
			});
		};
		TestAuxiliaryConnection = delegate
		{
			LlmRetryPrompt.CaptureMainThreadContext();
			ConnectionTestText testText = ConnectionTestText.Capture("Preprocess");
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", testText.Started);
					string effectiveModelName = GetEffectiveAuxiliaryModelName();
					if (string.IsNullOrWhiteSpace(AuxiliaryApiKey))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PreprocessTestFailed", "Preprocessing API connection test failed"), Ui("AF_MCM_Api_PreprocessKeyMissing", "The preprocessing API key is empty."));
						return;
					}
					if (string.IsNullOrWhiteSpace(effectiveModelName))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PreprocessTestFailed", "Preprocessing API connection test failed"), Ui("AF_MCM_Api_PreprocessModelMissing", "The preprocessing model name is empty. If you selected “*manual entry*” in the dropdown, enter the model name in the text box above."));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_PreprocessTesting", "[System] Testing the preprocessing API connection..."), Color.FromUint(4294967040u)));
					var requestPayload = new
					{
						model = effectiveModelName,
						messages = new[]
						{
							new
							{
								role = "system",
								content = AIConfigHandler.StrictPreprocessJsonSystemPrompt
							},
							new
							{
								role = "user",
								content = AIConfigHandler.BuildAuxiliaryConnectionTestPromptForExternal()
							}
						}
					};
					string jsonBody = AIConfigHandler.BuildAuxiliaryRouterRequestJsonForExternal(GetEffectiveApiUrl(AuxiliaryApiUrl), effectiveModelName, requestPayload.messages, 2048, 0f, out var controlMode, useConfiguredMaxTokens: false);
					string effectiveApiUrl = GetEffectiveApiUrl(AuxiliaryApiUrl);
					ConfiguredChatValidationExchange response = await SendConnectionTestJsonRequestAsync("auxiliary_validation", effectiveApiUrl, effectiveModelName, AuxiliaryApiKey, jsonBody, CancellationToken.None);
					string responseString = response.ResponseBody;
					if (response.Result.Status == LlmResultStatus.Succeeded)
					{
						string reply = TryExtractAssistantReplyText(responseString);
						string text = (controlMode == "plain") ? "" : " [" + controlMode + "]";
						bool validEnvelope = AIConfigHandler.TryValidateStrictPreprocessJsonEnvelope(reply, requireMemoryIds: false, requireMentionedEntities: true, out var testEnvelope, out var formatError);
						if (validEnvelope)
						{
							JArray testCodes = testEnvelope?["rule_codes"] as JArray;
								validEnvelope = testCodes != null && testCodes.Count == 1 && string.Equals(testCodes[0]?.ToString(), AIConfigHandler.PreprocessConnectionTestExpectedRuleCode, StringComparison.Ordinal);
							if (!validEnvelope)
							{
								formatError = "unexpected_test_rule_codes";
							}
						}
						if (!validEnvelope)
						{
							ShowLlmFailurePopup(Ui("AF_MCM_Api_PreprocessReplyParseFailed", "Preprocessing API reply parsing failed"), UiFormat("AF_MCM_Api_PreprocessInvalidJson", "The preprocessing API is reachable{MODE}, but its JSON envelope is invalid: {ERROR}", ("MODE", text), ("ERROR", formatError)), reply, responseString);
							Logger.Log("DuelSettings", testText.InvalidFormatPrefix + formatError + " | reply=" + (reply ?? ""));
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Api_PreprocessSuccess", "Preprocessing API connection and JSON format are valid{MODE}: {REPLY}", ("MODE", text), ("REPLY", reply.Trim())), Color.FromUint(4278255360u)));
						}
					}
					else
					{
						HttpStatusCode statusCode = (HttpStatusCode)response.StatusCode;
						string hint = BuildApiErrorHint(effectiveApiUrl, effectiveModelName, statusCode, responseString);
						string reason = UiFormat("AF_MCM_Api_StatusReason", "Connection failed with status code {STATUS}.", ("STATUS", statusCode)) + (string.IsNullOrWhiteSpace(hint) ? "" : ("\n" + Ui("AF_MCM_Api_Troubleshooting", "Troubleshooting suggestion: ") + hint));
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PreprocessTestFailed", "Preprocessing API connection test failed"), reason, "", responseString);
						Logger.Log("DuelSettings", testText.Failure(statusCode, responseString));
					}
				}
				catch (Exception ex)
				{
					ShowLlmFailurePopup(Ui("AF_MCM_Api_PreprocessTestException", "Preprocessing API connection test error"), ex.Message);
					Logger.Log("DuelSettings", testText.ExceptionPrefix + ex.Message);
				}
			});
		};
		TestActionPostprocessConnection = delegate
		{
			LlmRetryPrompt.CaptureMainThreadContext();
			ConnectionTestText testText = ConnectionTestText.Capture("Postprocess");
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", testText.Started);
					string effectiveModelName = GetEffectiveActionPostprocessModelName();
					if (string.IsNullOrWhiteSpace(ActionPostprocessApiKey))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PostprocessTestFailed", "Postprocessing API connection test failed"), Ui("AF_MCM_Api_PostprocessKeyMissing", "The postprocessing API key is empty."));
						return;
					}
					if (string.IsNullOrWhiteSpace(effectiveModelName))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PostprocessTestFailed", "Postprocessing API connection test failed"), Ui("AF_MCM_Api_PostprocessModelMissing", "The postprocessing model name is empty. If you selected “*manual entry*” in the dropdown, enter the model name in the text box above."));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_PostprocessTesting", "[System] Testing the postprocessing API connection..."), Color.FromUint(4294967040u)));
					if (ShouldWarnForContextExtractionApi(ActionPostprocessApiUrl))
					{
						InformationManager.DisplayMessage(new InformationMessage(GetContextExtractionCompatibilityWarningMessage(), Color.FromUint(4294936576u)));
					}
					string effectiveApiUrl = GetEffectiveApiUrl(ActionPostprocessApiUrl);
					JObject requestPayload = new JObject
					{
						["model"] = effectiveModelName,
						["messages"] = new JArray
						{
							new JObject
							{
								["role"] = "system",
								["content"] = testText.PromptSystem
							},
							new JObject
							{
								["role"] = "user",
								["content"] = testText.PromptUser
							}
						},
						["stream"] = false,
						["max_tokens"] = ConnectionTestMaxTokens,
						["temperature"] = GetActionPostprocessApiTemperature()
					};
					ApplyThinkingControls(requestPayload, effectiveApiUrl, effectiveModelName, ActionPostprocessApiThinkingEnabled, GetActionPostprocessApiReasoningEffort(), out var _);
					string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, requestPayload);
					ConfiguredChatValidationExchange response = await SendConnectionTestRequestAsync("action_postprocess_validation", effectiveApiUrl, effectiveModelName, ActionPostprocessApiKey, requestPayload, CancellationToken.None);
					string responseString = response.ResponseBody;
					if (response.Result.Status == LlmResultStatus.Succeeded)
					{
						string reply = TryExtractAssistantReplyText(responseString);
						bool recoveredFromReasoningTokenLimit = false;
						string emptyReplyReason = Ui("AF_MCM_Api_ReplyMissing", "The connection succeeded, but no model reply could be parsed.");
						if (string.IsNullOrWhiteSpace(reply) && LlmApiCompat.IsReasoningOnlyTokenLimitResponse(responseString, out int completionTokens, out int reasoningTokens))
						{
							string firstResponseString = responseString;
							emptyReplyReason = UiFormat("AF_MCM_Api_ReasoningOnly", "The connection succeeded, but the model used the entire test output budget for reasoning and produced no final content. completion_tokens={COMPLETION}, reasoning_tokens={REASONING}.", ("COMPLETION", completionTokens), ("REASONING", reasoningTokens));
							Logger.Log("DuelSettings", testText.ReasoningRetryPrefix + completionTokens + " reasoning_tokens=" + reasoningTokens);
							JObject retryPayload = (JObject)requestPayload.DeepClone();
							ApplyThinkingControls(retryPayload, effectiveApiUrl, effectiveModelName, false, ReasoningEffortLow, out var retryControlMode);
							ConfiguredChatValidationExchange retryResponse = await SendConnectionTestRequestAsync("action_postprocess_validation_retry", effectiveApiUrl, effectiveModelName, ActionPostprocessApiKey, retryPayload, CancellationToken.None);
							string retryResponseString = retryResponse.ResponseBody;
							if (retryResponse.Result.Status == LlmResultStatus.Succeeded)
							{
								string retryReply = TryExtractAssistantReplyText(retryResponseString);
								if (!string.IsNullOrWhiteSpace(retryReply))
								{
									reply = retryReply;
									recoveredFromReasoningTokenLimit = true;
									Logger.Log("DuelSettings", testText.RetrySucceededPrefix + retryControlMode + " reply=" + retryReply);
								}
								else
								{
									emptyReplyReason += " " + Ui("AF_MCM_Api_RetryContentEmpty", "Content is still empty after retrying with reasoning disabled.");
									responseString = testText.FirstResponseHeader + firstResponseString + testText.RetryResponseHeader + retryResponseString;
								}
							}
							else
							{
								emptyReplyReason += " " + UiFormat("AF_MCM_Api_RetryFailed", "Retry with reasoning disabled failed with status code {STATUS}.", ("STATUS", (HttpStatusCode)retryResponse.StatusCode));
								responseString = testText.FirstResponseHeader + firstResponseString + testText.RetryResponseHeader + retryResponseString;
							}
						}
						if (string.IsNullOrWhiteSpace(reply))
						{
							ShowLlmFailurePopup(Ui("AF_MCM_Api_PostprocessReplyParseFailed", "Postprocessing API reply parsing failed"), emptyReplyReason, "", responseString);
						}
						else
						{
						string recoveryHint = recoveredFromReasoningTokenLimit ? Ui("AF_MCM_Api_ReasoningRecovery", " (The first reasoning attempt exhausted the test budget; reasoning was disabled for retry. Keep enough maximum output tokens in actual use.)") : "";
						InformationManager.DisplayMessage(new InformationMessage(UiFormat("AF_MCM_Api_PostprocessSuccess", "Postprocessing API connection is working{RECOVERY}: {REPLY}", ("RECOVERY", recoveryHint), ("REPLY", reply.Trim())), Color.FromUint(4278255360u)));
						}
					}
					else
					{
						HttpStatusCode statusCode = (HttpStatusCode)response.StatusCode;
						string hint = BuildApiErrorHint(effectiveApiUrl, effectiveModelName, statusCode, responseString);
						string reason = UiFormat("AF_MCM_Api_StatusReason", "Connection failed with status code {STATUS}.", ("STATUS", statusCode)) + (string.IsNullOrWhiteSpace(hint) ? "" : ("\n" + Ui("AF_MCM_Api_Troubleshooting", "Troubleshooting suggestion: ") + hint));
						ShowLlmFailurePopup(Ui("AF_MCM_Api_PostprocessTestFailed", "Postprocessing API connection test failed"), reason, "", responseString);
						Logger.Log("DuelSettings", testText.Failure(statusCode, responseString));
					}
				}
				catch (Exception ex)
				{
					ShowLlmFailurePopup(Ui("AF_MCM_Api_PostprocessTestException", "Postprocessing API connection test error"), ex.Message);
					Logger.Log("DuelSettings", testText.ExceptionPrefix + ex.Message);
				}
			});
		};
		TestEventAndRebellionConnection = delegate
		{
			LlmRetryPrompt.CaptureMainThreadContext();
			ConnectionTestText testText = ConnectionTestText.Capture("Event");
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", testText.Started);
					string effectiveModelName = GetEffectiveEventAndRebellionModelName();
					if (string.IsNullOrWhiteSpace(EventAndRebellionApiUrl))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_EventTestFailed", "Event/rebellion API connection test failed"), Ui("AF_MCM_Api_EventAddressMissing", "The event/rebellion API address is empty."));
						return;
					}
					if (string.IsNullOrWhiteSpace(EventAndRebellionApiKey))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_EventTestFailed", "Event/rebellion API connection test failed"), Ui("AF_MCM_Api_EventKeyMissing", "The event/rebellion API key is empty."));
						return;
					}
					if (string.IsNullOrWhiteSpace(effectiveModelName))
					{
						ShowLlmFailurePopup(Ui("AF_MCM_Api_EventTestFailed", "Event/rebellion API connection test failed"), Ui("AF_MCM_Api_EventModelMissing", "The event/rebellion model name is empty. If you selected “*manual entry*” in the dropdown, enter the model name in the text box above."));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_EventTesting", "[System] Testing the event/rebellion API connection..."), Color.FromUint(4294967040u)));
					string effectiveApiUrl = GetEffectiveApiUrl(EventAndRebellionApiUrl);
					JObject requestPayload = new JObject
					{
						["model"] = effectiveModelName,
						["messages"] = new JArray
						{
							new JObject
							{
								["role"] = "system",
								["content"] = testText.PromptSystem
							},
							new JObject
							{
								["role"] = "user",
								["content"] = testText.PromptUser
							}
						},
						["stream"] = false,
						["max_tokens"] = 32,
						["temperature"] = GetEventAndRebellionApiTemperature()
					};
					ApplyThinkingControls(requestPayload, effectiveApiUrl, effectiveModelName, EventAndRebellionApiThinkingEnabled, GetEventAndRebellionApiReasoningEffort(), out var _);
					string jsonBody = LlmApiCompat.PrepareChatRequestJson(effectiveApiUrl, requestPayload);
					ConfiguredChatValidationExchange response = await SendConnectionTestRequestAsync("event_rebellion_validation", effectiveApiUrl, effectiveModelName, EventAndRebellionApiKey, requestPayload, CancellationToken.None);
					string responseString = response.ResponseBody;
					if (response.Result.Status == LlmResultStatus.Succeeded)
					{
						string reply = TryExtractAssistantReplyText(responseString);
						if (string.IsNullOrWhiteSpace(reply))
						{
							ShowLlmFailurePopup(Ui("AF_MCM_Api_EventReplyParseFailed", "Event/rebellion API reply parsing failed"), Ui("AF_MCM_Api_ReplyMissing", "The connection succeeded, but no model reply could be parsed."), "", responseString);
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage(Ui("AF_MCM_Api_EventSuccess", "Event/rebellion API connection is working: ") + reply.Trim(), Color.FromUint(4278255360u)));
						}
					}
					else
					{
						HttpStatusCode statusCode = (HttpStatusCode)response.StatusCode;
						string hint = BuildApiErrorHint(effectiveApiUrl, effectiveModelName, statusCode, responseString);
						string reason = UiFormat("AF_MCM_Api_StatusReason", "Connection failed with status code {STATUS}.", ("STATUS", statusCode)) + (string.IsNullOrWhiteSpace(hint) ? "" : ("\n" + Ui("AF_MCM_Api_Troubleshooting", "Troubleshooting suggestion: ") + hint));
						ShowLlmFailurePopup(Ui("AF_MCM_Api_EventTestFailed", "Event/rebellion API connection test failed"), reason, "", responseString);
						Logger.Log("DuelSettings", testText.Failure(statusCode, responseString));
					}
				}
				catch (Exception ex)
				{
					ShowLlmFailurePopup(Ui("AF_MCM_Api_EventTestException", "Event/rebellion API connection test error"), ex.Message);
					Logger.Log("DuelSettings", testText.ExceptionPrefix + ex.Message);
				}
			});
		};
	}
}
