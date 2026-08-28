using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace AnimusForge;

public static class AIConfigHandler
{
	private const int ActionPostprocessRequestTimeoutMilliseconds = DuelSettings.LlmRequestTimeoutMilliseconds;
	private const string EmbeddedPreprocessPromptsResourceName = "AnimusForge.Defaults.PreprocessPrompts.json";
	private const string EmbeddedRpItemIntroductionPromptsResourceName = "AnimusForge.Defaults.RpItemIntroductionPrompts.json";
	private const string KingAbdicateToPlayerActionTag = "[ACTION:KING_ABDICATE_TO_PLAYER]";
	private static readonly Lazy<JObject> EmbeddedPreprocessPromptsDefaults = new Lazy<JObject>(LoadEmbeddedDefaultPreprocessPrompts, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly Lazy<RpItemIntroductionPromptsConfigModel> EmbeddedRpItemIntroductionPromptsDefaults = new Lazy<RpItemIntroductionPromptsConfigModel>(LoadEmbeddedDefaultRpItemIntroductionPrompts, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly Encoding StrictUtf8Encoding = new UTF8Encoding(false, true);
	private static volatile string _preprocessPromptsLoadError = "";
	private static int _rpItemIntroductionPromptsFallbackLogged;
	private static int _rpItemIntroductionPromptBuildFailureLogged;
	private sealed class ActionPostprocessHistoryEntry
	{
		public int Index;

		public string Text;

		public bool IsRoleMessage;
	}

	private sealed class GuardrailRuleEval
	{
		public string RuleTag;

		public string MatchedSeed;

		public string MatchedIntent;

		public float RawInput;

		public float RawContext;

		public float MixedRaw;

		public float AmpScore;

		public float RerankScore;

		public float Delta;

		public float Mean;

		public float MaxOther;

		public string MaxOtherTag;

		public int Rank;

		public bool Candidate;

		public bool AbsHit;

		public bool RelHit;

		public bool HighAmpHit;

		public bool ForceHit;

		public float TopGap;

		public float IntentEvidence;

		public float IntentGate;

		public string IntentSeed;

		public bool LexicalAnchor;

		public string RejectReason;

		public string MatchMode;

		public bool Hit;
	}

	private sealed class GuardrailEvalSnapshot
	{
		public string Key;

		public string MatchMode = "none";

		public int IntentCount;

		public int RecallPerIntent;

		public int RerankPerIntent;

		public int ReturnCap;

		public MentionedWorldEntities MentionedEntities = new MentionedWorldEntities();

		public Dictionary<string, GuardrailRuleEval> Rules = new Dictionary<string, GuardrailRuleEval>(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class GuardrailAuxiliaryTopic
	{
		public int Number;

		public string Label;

		public string Code;

		public string RuleId;
	}

	private sealed class PreprocessExcludedPromptEntry
	{
		public string RuleId;

		public int TopicNumber;

		public int Priority;

		public string Instruction;
	}

	private sealed class GuardrailIntentInput
	{
		public string Text;

		public float[] Vector;

		public float Weight = 1f;
	}

	private sealed class GuardrailRuleScore
	{
		public GuardrailRulePromptConfig Rule;

		public float RawScore;

		public float FinalScore;

		public string MatchedSeed;

		public string MatchedIntent;
	}

	private sealed class GuardrailRuleAggregate
	{
		public GuardrailRuleEval Eval;

		public float ScoreSum;

		public int HitCount;

		public int BestRank = int.MaxValue;

		public float BestScore;

		public string MatchedSeed;

		public string MatchedIntent;
	}

	private sealed class StickyGuardrailRuleState
	{
		public string RuleId = "";

		public string Group = "";

		public int Priority;

		public float LastScore;

		public string MatchedSeed = "";

		public int RemainingCarryTurns;

		public int MaxCarryTurns;

		public int CarryTurnIndex;
	}

	private static string BuildSemanticHitRateDetail(string detail, string secondaryText)
	{
		string text = (detail ?? "").Trim();
		string text2 = NormalizeSemanticText(secondaryText);
		string text3 = string.IsNullOrWhiteSpace(text2) ? "off" : "on";
		if (text2.Length > 72)
		{
			text2 = text2.Substring(0, 72);
		}
		text2 = text2.Replace("\r", " ").Replace("\n", " ").Trim();
		string value = $"npcRecall={text3} secondaryLen={(string.IsNullOrWhiteSpace(text2) ? 0 : text2.Length)}";
		if (!string.IsNullOrWhiteSpace(text2))
		{
			value = value + " secondaryPreview=" + JsonConvert.ToString(text2);
		}
		return string.IsNullOrWhiteSpace(text) ? value : (text + " " + value);
	}

	private struct GuardrailGateProfile
	{
		public float AmpGate;

		public float ForceHitGate;

		public float RawFloor;

		public float GapBoost;

		public float CenterBoost;

		public float TopGapGate;

		public float AnchorRawFloor;
	}

	private static AIConfigModel _config;

	private static GuardrailConfigModel _guardrail;

	private static ActionPostprocessConfigModel _actionPostprocess;

	private static PreprocessPromptsConfigModel _preprocessPrompts;

	private static ProactiveNpcRequestPromptsConfigModel _proactiveNpcRequestPrompts;

	private static RpItemIntroductionPromptsConfigModel _rpItemIntroductionPrompts;

	private static readonly Regex PreprocessTemplateVariableRegex = new Regex("\\{([a-z][a-z0-9_]*)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex RpItemIntroductionTemplateVariableRegex = new Regex("\\{([^{}]*)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly HashSet<string> RpItemIntroductionTemplateVariables = new HashSet<string>(StringComparer.Ordinal)
	{
		"item_name",
		"giver_name",
		"dialogue"
	};

	private static readonly object _guardrailSemanticLock = new object();

	private static readonly Dictionary<string, float[]> _guardrailPhraseVecCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private static readonly Dictionary<string, float[]> _guardrailInputVecCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private static int _guardrailWarmupState;

	private static long _guardrailWarmupVersion = -1L;

	private static long _guardrailConfigVersion = 1L;

	private static readonly object _preprocessExcludedPromptCacheLock = new object();

	private static long _preprocessExcludedPromptCacheVersion = -1L;

	private static List<PreprocessExcludedPromptEntry> _preprocessExcludedPromptCache = new List<PreprocessExcludedPromptEntry>();

	private const int GuardrailPhraseVecCacheMax = 1024;

	private const int GuardrailInputVecCacheMax = 256;

	private static readonly AsyncLocal<string> _guardrailSemanticRuntimeContext = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetKingdomId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetHeroId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetCharacterId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetTroopId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetUnnamedRank = new AsyncLocal<string>();

	private static readonly AsyncLocal<int> _guardrailRuntimeTargetAgentIndex = new AsyncLocal<int>();

	private static readonly object _stickyGuardrailRuleLock = new object();

	private static readonly Dictionary<string, List<StickyGuardrailRuleState>> _stickyGuardrailRules = new Dictionary<string, List<StickyGuardrailRuleState>>(StringComparer.OrdinalIgnoreCase);

	private static readonly string[] StickyGuardrailFollowUpPhrases = new string[17]
	{
		"然后", "然后呢", "接着呢", "接下来呢", "那然后呢", "那接下来呢", "那我该怎么办", "我该怎么办", "下一步呢", "下一步怎么做",
		"具体怎么做", "具体呢", "细说", "继续说", "继续", "展开说说", "后面呢"
	};

	private const int MaxStickyGuardrailRulesPerTarget = 3;

	private static GuardrailEvalSnapshot _lastGuardrailEval;

	private static readonly Regex AuxiliaryGuardrailNumberRegex = new Regex("\\d+", RegexOptions.Compiled);

	private static readonly object _auxiliaryMentionedEntitiesLock = new object();

	private static readonly Dictionary<string, MentionedWorldEntities> _auxiliaryMentionedEntitiesCache = new Dictionary<string, MentionedWorldEntities>(StringComparer.Ordinal);

	private static readonly Queue<string> _auxiliaryMentionedEntitiesCacheOrder = new Queue<string>();

	private static readonly AsyncLocal<MentionedWorldEntities> _auxiliaryMentionedEntitiesLatest = new AsyncLocal<MentionedWorldEntities>();

	private const int AuxiliaryMentionedEntitiesCacheMax = 64;

	internal static string StrictPreprocessJsonSystemPrompt
	{
		get
		{
			EnsurePreprocessPromptsAvailable();
			return RequirePreprocessPromptValue(_preprocessPrompts?.StrictJson?.SystemPrompt, "StrictJson.SystemPrompt");
		}
	}

	internal static string StrictPreprocessMentionedEntitiesSchema
	{
		get
		{
			EnsurePreprocessPromptsAvailable();
			JObject schema = _preprocessPrompts?.StrictJson?.MentionedEntitiesSchema;
			if (schema == null || !schema.Properties().Any())
			{
				throw new InvalidOperationException("PreprocessPrompts.json 缺少必填项: StrictJson.MentionedEntitiesSchema");
			}
			return schema.ToString(Formatting.None);
		}
	}

	internal static string PreprocessConnectionTestExpectedRuleCode
	{
		get
		{
			EnsurePreprocessPromptsAvailable();
			return RequirePreprocessPromptValue(_preprocessPrompts?.ConnectionTest?.ExpectedRuleCode, "ConnectionTest.ExpectedRuleCode");
		}
	}

	public static string GlobalPrompt => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.GlobalPrompt ?? "");

	public static string GlobalGuardrail => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.GlobalGuardrail ?? "");

	public static string DuelInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.TriggerInstruction ?? "");

	public static string DuelDialogueInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.DialogueInstruction ?? "");

	public static string DuelHealthBlockedInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.HealthBlockedInstruction ?? "");

	public static string DuelHealthBlockedMessage => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.HealthBlockedMessage ?? "");

	public static string DuelLegacyFollowupInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.LegacyFollowupInstruction ?? "");

	public static string FormatDuelHealthTemplate(string template, string npcName, float healthRatio)
	{
		string text = ApplyPlayerDisplayNameToGuardrailText(template ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string name = string.IsNullOrWhiteSpace(npcName) ? "目标NPC" : npcName.Trim();
		string percent = Math.Max(0f, Math.Min(1f, healthRatio)).ToString("P0");
		return text.Replace("{npcName}", name).Replace("{healthPercent}", percent).Replace("{healthRatio}", percent);
	}

	public static List<string> DuelTriggerKeywords => _guardrail?.Duel?.AcceptKeywords ?? new List<string>();

	public static List<PostprocessRuleEntry> DuelPostprocessRules => _guardrail?.Duel?.PostprocessRules ?? new List<PostprocessRuleEntry>();

	public static bool RewardEnabled => _guardrail?.Reward?.IsEnabled == true;

	public static string RewardInstruction => BuildRewardInstructionForExternal();

	public static List<PostprocessRuleEntry> RewardPostprocessRules => _guardrail?.Reward?.PostprocessRules ?? new List<PostprocessRuleEntry>();

	public static List<string> RewardTriggerKeywords => _guardrail?.Reward?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> RewardRuntimeInstructionTemplates => _guardrail?.Reward?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool IsPlayerCompanionOrFamilyTradeTarget(Hero targetHero)
	{
		try
		{
			if (RomanceSystemBehavior.IsPlayerCompanionOrFamily(targetHero))
			{
				return true;
			}
			Hero mainHero = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? mainHero?.Clan;
			return targetHero != null && targetHero != mainHero && playerClan != null && (targetHero.IsPlayerCompanion || targetHero.CompanionOf == playerClan || targetHero.Clan == playerClan);
		}
		catch
		{
			return false;
		}
	}

	public static bool IsPlayerPartyTradeLimitedTarget(Hero targetHero)
	{
		try
		{
			if (targetHero == null || targetHero == Hero.MainHero)
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}
			if (targetHero.PartyBelongedTo == mainParty)
			{
				return true;
			}
			return targetHero.CharacterObject != null && mainParty.MemberRoster != null && mainParty.MemberRoster.FindIndexOfTroop(targetHero.CharacterObject) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerClanLordTarget(Hero targetHero)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			return targetHero != null
				&& playerClan != null
				&& targetHero != Hero.MainHero
				&& targetHero.Clan == playerClan
				&& targetHero.Occupation == Occupation.Lord
				&& targetHero.CompanionOf == null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerPartyTradeLimitedRule(string ruleId)
	{
		string text = (ruleId ?? "").Trim();
		return string.Equals(text, "loan", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "kingdom_agenda", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "diplomacy", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "party_transfer", StringComparison.OrdinalIgnoreCase);
	}

	private static bool ShouldExcludePlayerPartyTradeLimitedRulesForConversationTarget()
	{
		try
		{
			return IsPlayerPartyTradeLimitedTarget(ResolveConversationTargetHero());
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldExcludeRuntimeRuleForConversationTarget(string ruleId)
	{
		return IsPlayerPartyTradeLimitedRule(ruleId) && ShouldExcludePlayerPartyTradeLimitedRulesForConversationTarget();
	}

	private static bool IsSceneMoveRule(string ruleId)
	{
		return string.Equals((ruleId ?? "").Trim(), "scene_mechanism_actions", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSceneAutoGroupRelayRule(string ruleId)
	{
		return string.Equals((ruleId ?? "").Trim(), "scene_auto_group_relay", StringComparison.OrdinalIgnoreCase);
	}

	public static bool ShouldExcludeSceneMoveRuleForCurrentMission()
	{
		try
		{
			return ShouldExcludeSceneMoveRuleForMission(Mission.Current);
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldExcludeSceneMoveRuleForMission(Mission mission)
	{
		if (mission == null)
		{
			return false;
		}
		bool allowSetsOwnedSettlementSceneCommands = false;
		try
		{
			allowSetsOwnedSettlementSceneCommands = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(mission);
		}
		catch
		{
		}
		if (IsPrisonBreakMission(mission))
		{
			return false;
		}
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive || mission.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (TroopInspectionBehavior.ShouldSuppressReinforcementSystem(mission))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (MilitaryExerciseBehavior.IsCurrentExerciseRuntime() && mission.GetMissionBehavior<MilitaryExerciseMissionLogic>() != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MissionMode mode = mission.Mode;
			if ((mode == MissionMode.Battle && !allowSetsOwnedSettlementSceneCommands) || mode == MissionMode.Deployment || mode == MissionMode.Duel || mode == MissionMode.Stealth || mode == MissionMode.Tournament)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (mission.GetMissionBehavior<BattleEndLogic>() != null || mission.GetMissionBehavior<BattleDeploymentMissionController>() != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounterCompat.HasBattleOrEncounteredBattle() || PlayerEncounterCompat.HasCampaignBattleResult())
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsPrisonBreakMission(Mission mission)
	{
		try
		{
			return mission?.GetMissionBehavior<PrisonBreakMissionController>() != null;
		}
		catch
		{
			return false;
		}
	}

	public static bool LoanEnabled => _guardrail?.Loan?.IsEnabled == true;

	public static string LoanInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Loan?.Instruction ?? "");

	public static List<PostprocessRuleEntry> LoanPostprocessRules => _guardrail?.Loan?.PostprocessRules ?? new List<PostprocessRuleEntry>();

	public static List<string> LoanTriggerKeywords => _guardrail?.Loan?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> LoanRuntimeInstructionTemplates => _guardrail?.Loan?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool SurroundingsEnabled => _guardrail?.Surroundings?.IsEnabled == true;

	public static string SurroundingsInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Surroundings?.Instruction ?? "");

	public static List<string> SurroundingsTriggerKeywords => _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>();

	public static string DuelNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.NonHeroInstruction ?? "");

	public static string RewardNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Reward?.NonHeroInstruction ?? "");

	public static string LoanNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Loan?.NonHeroInstruction ?? "");

	public static string GetGuardrailRuleNonHeroInstruction(string ruleTag)
	{
		try
		{
			return ApplyPlayerDisplayNameToGuardrailText(GetRulePromptByTag(ruleTag)?.NonHeroInstruction ?? "");
		}
		catch
		{
			return "";
		}
	}

	public static List<PostprocessRuleEntry> GetGuardrailRulePostprocessRules(string ruleTag)
	{
		try
		{
			GuardrailRulePromptConfig rulePromptByTag = GetRulePromptByTag(ruleTag);
			return rulePromptByTag?.PostprocessRules ?? new List<PostprocessRuleEntry>();
		}
		catch
		{
			return new List<PostprocessRuleEntry>();
		}
	}

	private static bool GuardrailKnowledgeEnabled => _guardrail?.KnowledgeRetrieval?.IsEnabled ?? true;

	private static bool GuardrailKnowledgeSemanticFirst => _guardrail?.KnowledgeRetrieval?.SemanticFirst ?? true;

	private static int GuardrailKnowledgeTopK => ClampKnowledgeTopK(_guardrail?.KnowledgeRetrieval?.SemanticTopK ?? 4);

	public static bool KnowledgeRetrievalEnabled
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var enabled, out var _, out var _))
			{
				return enabled;
			}
			return GuardrailKnowledgeEnabled;
		}
	}

	public static bool KnowledgeSemanticFirst
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var semanticFirst, out var _))
			{
				return semanticFirst;
			}
			return GuardrailKnowledgeSemanticFirst;
		}
	}

	public static int KnowledgeSemanticTopK
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var _, out var topK))
			{
				return topK;
			}
			return GuardrailKnowledgeTopK;
		}
	}

	public static float KnowledgeSemanticMinScore
	{
		get
		{
			try
			{
				return _guardrail?.KnowledgeRetrieval?.SemanticMinScore ?? 0.21f;
			}
			catch
			{
				return 0.21f;
			}
		}
	}

	public static bool KnowledgeRetrievalFromMcm => UseMcmKnowledgeRetrieval();

	public static bool UseAuxiliaryRuleApiRetrieval
	{
		get
		{
			try
			{
				DuelSettings settings = DuelSettings.GetSettings();
				return settings != null && (settings.UseAuxiliaryRuleApi || settings.MemoryPreprocessMode == 1 || settings.MemoryPreprocessMode == 2);
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool ActionPostprocessEnabled => _actionPostprocess?.IsEnabled ?? false;

	public static string ActionPostprocessSystemPrompt => (_actionPostprocess?.SystemPrompt ?? "").Trim();

	public static string ActionPostprocessUserPromptTemplate => (_actionPostprocess?.UserPromptTemplate ?? "").Trim();

	public static string ActionPostprocessFallbackMoodTag => (_actionPostprocess?.FallbackMoodTag ?? "[ACTION:MOOD:NEUTRAL]").Trim();

	public static string GetProactiveNpcRequestOpeningPrompt(string needType)
	{
		return AppendProactiveNpcRequestNaturalExpressionGuide(GetProactiveNpcRequestPromptEntry(needType)?.OpeningPrompt);
	}

	public static string GetProactiveNpcRequestLetterIntent(string needType)
	{
		return AppendProactiveNpcRequestNaturalExpressionGuide(GetProactiveNpcRequestPromptEntry(needType)?.LetterIntent);
	}

	public static string GetProactiveNpcRequestCompanionIntent(string needType)
	{
		return AppendProactiveNpcRequestNaturalExpressionGuide(GetProactiveNpcRequestPromptEntry(needType)?.CompanionIntent);
	}

	private static string AppendProactiveNpcRequestNaturalExpressionGuide(string prompt)
	{
		string normalizedPrompt = prompt?.Trim() ?? "";
		string guide = _proactiveNpcRequestPrompts?.Default?.NaturalExpressionGuide?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(guide))
		{
			return normalizedPrompt;
		}
		return string.IsNullOrWhiteSpace(normalizedPrompt) ? guide : normalizedPrompt + "\n" + guide;
	}

	private static ProactiveNpcRequestPromptEntry GetProactiveNpcRequestPromptEntry(string needType)
	{
		ProactiveNpcRequestPromptsConfigModel config = _proactiveNpcRequestPrompts;
		if (config?.Requests != null && !string.IsNullOrWhiteSpace(needType))
		{
			foreach (KeyValuePair<string, ProactiveNpcRequestPromptEntry> entry in config.Requests)
			{
				if (string.Equals(entry.Key?.Trim(), needType.Trim(), StringComparison.OrdinalIgnoreCase) && entry.Value != null)
				{
					return entry.Value;
				}
			}
		}
		return config?.Default;
	}

	/// <summary>
	/// Renders an immutable prompt snapshot for one RP item introduction request.
	/// This does not read from disk and therefore remains safe for background request preparation.
	/// </summary>
	public static bool TryBuildRpItemIntroductionPromptsForExternal(string itemName, string giverName, string dialogue, out string systemPrompt, out string userPrompt, out string error)
	{
		systemPrompt = "";
		userPrompt = "";
		error = "";
		try
		{
			RpItemIntroductionPromptsConfigModel config = _rpItemIntroductionPrompts;
			if (config == null)
			{
				throw new InvalidOperationException("RP物品介绍提示词尚未加载");
			}
			systemPrompt = RequireRpItemIntroductionPromptValue(config.SystemPrompt, "SystemPrompt");
			userPrompt = RenderRpItemIntroductionPromptTemplate(config.UserPromptTemplate, new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["item_name"] = itemName ?? "",
				["giver_name"] = giverName ?? "",
				["dialogue"] = dialogue ?? ""
			});
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			if (Interlocked.Exchange(ref _rpItemIntroductionPromptBuildFailureLogged, 1) == 0)
			{
				Logger.Log("AIConfig", "[RP物品介绍] 无法渲染提示词，自动介绍已跳过: " + error);
			}
			return false;
		}
	}

	public static List<PostprocessRuleEntry> WildernessPostprocessRules => _actionPostprocess?.WildernessPostprocessRules ?? new List<PostprocessRuleEntry>();

	public static List<PostprocessRuleEntry> RoyalPostprocessRules => _actionPostprocess?.RoyalPostprocessRules ?? new List<PostprocessRuleEntry>();

	public static List<PostprocessRuleEntry> IntimacyPostprocessRules => _actionPostprocess?.IntimacyPostprocessRules ?? new List<PostprocessRuleEntry>();

	public static List<PostprocessRuleEntry> ActionPostprocessMoodRules => _actionPostprocess?.MoodRules ?? new List<PostprocessRuleEntry>();

	public static bool IsRoyalAbdicationPostprocessTargetForExternal(Hero targetHero)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			Clan playerClan = Clan.PlayerClan;
			if (hero == null || hero == Hero.MainHero || playerClan == null || Hero.MainHero == null)
			{
				return false;
			}
			Kingdom playerKingdom = playerClan.Kingdom;
			if (playerKingdom != null && (playerKingdom.RulingClan == playerClan || playerKingdom.Leader == Hero.MainHero))
			{
				return false;
			}
			Clan targetClan = hero.Clan;
			if (targetClan == null || targetClan.IsEliminated || targetClan == playerClan)
			{
				return false;
			}
			Kingdom kingdom = targetClan.Kingdom ?? hero.MapFaction as Kingdom;
			if (kingdom == null || kingdom.IsEliminated || kingdom.RulingClan == playerClan)
			{
				return false;
			}
			return kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsKingdomLordOrKingRuleTargetForPreprocess(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			Clan clan = hero?.Clan;
			Kingdom kingdom = clan?.Kingdom ?? hero?.MapFaction as Kingdom;
			if (hero == null || clan == null || kingdom == null || clan.IsEliminated || kingdom.IsEliminated || clan.IsUnderMercenaryService)
			{
				return false;
			}
			if (kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero || hero.IsFactionLeader)
			{
				return true;
			}
			return hero.IsLord;
		}
		catch
		{
			return false;
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimeRoyalPostprocessRulesForExternal(Hero targetHero)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			// 王位让渡是按身份资格常驻的后处理规则：只检查目标国王与玩家状态，禁止再绑定 diplomacy 或其他前处理话题。
			if (!IsRoyalAbdicationPostprocessTargetForExternal(targetHero))
			{
				return list;
			}
			foreach (PostprocessRuleEntry rule in RoyalPostprocessRules ?? new List<PostprocessRuleEntry>())
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (!string.Equals(tag, KingAbdicateToPlayerActionTag, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				list.Add(new PostprocessRuleEntry
				{
					Tag = KingAbdicateToPlayerActionTag,
					Description = rule?.Description ?? ""
				});
			}
			Logger.Log("AIConfig", "[RoyalPostprocessRules] mode=resident targetHero=" + (targetHero?.StringId ?? "") + " rules=" + ((list.Count == 0) ? "（无）" : string.Join(",", list.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		catch
		{
		}
		return list;
	}

	private static string NormalizeActionPostprocessOptionalValue(string value)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "（无）", StringComparison.Ordinal))
		{
			return "";
		}
		return text;
	}

	private static string ReplaceActionPostprocessOptionalSection(string template, string titleLine, string tokenName, string value)
	{
		string text = template ?? "";
		string text2 = NormalizeActionPostprocessOptionalValue(value);
		string text3 = "{" + (tokenName ?? "") + "}";
		if (string.IsNullOrWhiteSpace(text3))
		{
			return text;
		}
		if (string.IsNullOrEmpty(text2))
		{
			if (!string.IsNullOrWhiteSpace(titleLine))
			{
				string pattern = "(?:\\r?\\n){0,2}" + Regex.Escape(titleLine) + "\\r?\\n" + Regex.Escape(text3) + "(?:\\r?\\n)?";
				text = Regex.Replace(text, pattern, "", RegexOptions.CultureInvariant);
			}
			return text.Replace(text3, "");
		}
		return text.Replace(text3, text2);
	}

	public static string BuildActionPostprocessSystemPrompt(string tagRules, string moodRules, string npcName, string sharedItemList = null, string playerItemList = null, string debtHint = null, string marriagePlayerCandidates = null, string marriageTargetCandidates = null)
	{
		string text = ActionPostprocessSystemPrompt;
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		sharedItemList = NormalizeActionPostprocessNameReferences(sharedItemList, npcName);
		playerItemList = NormalizeActionPostprocessNameReferences(playerItemList, npcName);
		debtHint = NormalizeActionPostprocessNameReferences(debtHint, npcName);
		marriagePlayerCandidates = NormalizeActionPostprocessNameReferences(marriagePlayerCandidates, npcName);
		marriageTargetCandidates = NormalizeActionPostprocessNameReferences(marriageTargetCandidates, npcName);
		text = ReplaceActionPostprocessOptionalSection(text, "{npc_name}的物品清单：", "shared_item_list", sharedItemList);
		text = ReplaceActionPostprocessOptionalSection(text, "玩家可见装备：", "player_item_list", playerItemList);
		text = ReplaceActionPostprocessOptionalSection(text, "债务提示：", "debt_hint", debtHint);
		text = ReplaceActionPostprocessOptionalSection(text, "玩家家族可婚配成员（允许已有配偶，事实清单）：", "marriage_player_candidates", marriagePlayerCandidates);
		text = ReplaceActionPostprocessOptionalSection(text, "对方家族可婚配成员（允许已有配偶，事实清单）：", "marriage_target_candidates", marriageTargetCandidates);
		text = text.Replace("{tag_rules}", string.IsNullOrWhiteSpace(tagRules) ? "（无）" : tagRules.Trim())
			.Replace("{mood_rules}", string.IsNullOrWhiteSpace(moodRules) ? "（无）" : moodRules.Trim())
			.Replace("{npc_name}", "NPC");
		return Regex.Replace(text.Trim(), "(\\r?\\n){3,}", Environment.NewLine + Environment.NewLine);
	}

	public static string BuildActionPostprocessUserPrompt(string userPromptTemplate, string tagRules, string npcName, string historyText, string latestReplyBlock, string sharedItemList = null, string playerItemList = null, string debtHint = null, string marriagePlayerCandidates = null, string marriageTargetCandidates = null, string runtimeContext = null)
	{
		string text = userPromptTemplate ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		sharedItemList = NormalizeActionPostprocessNameReferences(sharedItemList, npcName);
		playerItemList = NormalizeActionPostprocessNameReferences(playerItemList, npcName);
		debtHint = NormalizeActionPostprocessNameReferences(debtHint, npcName);
		marriagePlayerCandidates = NormalizeActionPostprocessNameReferences(marriagePlayerCandidates, npcName);
		marriageTargetCandidates = NormalizeActionPostprocessNameReferences(marriageTargetCandidates, npcName);
		runtimeContext = AppendPersistentActionPostprocessRuntimeContext(runtimeContext);
		runtimeContext = NormalizeActionPostprocessNameReferences(runtimeContext, npcName);
		text = ReplaceActionPostprocessOptionalSection(text, "玩家可见装备：", "player_item_list", playerItemList);
		text = ReplaceActionPostprocessOptionalSection(text, "{npc_name}的物品清单：", "shared_item_list", sharedItemList);
		text = ReplaceActionPostprocessOptionalSection(text, "玩家家族可婚配成员（允许已有配偶，事实清单）：", "marriage_player_candidates", marriagePlayerCandidates);
		text = ReplaceActionPostprocessOptionalSection(text, "对方家族可婚配成员（允许已有配偶，事实清单）：", "marriage_target_candidates", marriageTargetCandidates);
		text = ReplaceActionPostprocessOptionalSection(text, "债务提示：", "debt_hint", debtHint);
		text = ReplaceActionPostprocessOptionalSection(text, "运行时补充事实：", "runtime_context", runtimeContext);
		int maxHistoryEntries = DuelSettings.GetActionPostprocessHistoryEntryLimitForExternal();
		string newValue = PrepareActionPostprocessHistoryText(historyText, maxHistoryEntries, latestReplyBlock);
		text = text.Replace("{tag_rules}", string.IsNullOrWhiteSpace(tagRules) ? "（无）" : tagRules.Trim())
			.Replace("{history}", string.IsNullOrWhiteSpace(newValue) ? "（无）" : newValue)
			.Replace("{reply}", string.IsNullOrWhiteSpace(latestReplyBlock) ? "玩家: （无）\nNPC: （无）" : latestReplyBlock.Trim())
			.Replace("{npc_name}", "NPC");
		return Regex.Replace(text.Trim(), "(\\r?\\n){3,}", Environment.NewLine + Environment.NewLine);
	}

	private static string AppendPersistentActionPostprocessRuntimeContext(string runtimeContext)
	{
		string text = (runtimeContext ?? "").Trim();
		List<string> facts = new List<string>();
		string playerId = "";
		try
		{
			playerId = (Hero.MainHero?.StringId ?? "").Trim();
		}
		catch
		{
			playerId = "";
		}
		if (!string.IsNullOrWhiteSpace(playerId) && !ContainsExactRuntimeEntityId(text, "hero", playerId))
		{
			facts.Add("【固定实体ID】玩家本人固定ID：" + playerId + "。当后处理标签需要指向玩家本人或玩家主队所属英雄时，只能使用此ID；WORLDMAP_ORDER跟随玩家时，目标类型用hero，id填写" + playerId + "；不要猜测玩家ID。");
		}
		string locationFact = BuildActionPostprocessPlayerCurrentLocationFact();
		if (!string.IsNullOrWhiteSpace(locationFact) && text.IndexOf("【玩家当前地点ID】", StringComparison.OrdinalIgnoreCase) < 0)
		{
			facts.Add(locationFact);
		}
		if (facts.Count == 0)
		{
			return text;
		}
		string factText = string.Join(Environment.NewLine, facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
		if (string.IsNullOrWhiteSpace(text))
		{
			return factText.Trim();
		}
		return (text + Environment.NewLine + factText).Trim();
	}

	private static bool ContainsExactRuntimeEntityId(string text, string type, string id)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		string pattern = @"(?<![A-Za-z0-9_.-])" + Regex.Escape(type.Trim() + ":" + id.Trim()) + @"(?![A-Za-z0-9_.-])";
		if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
		{
			return true;
		}
		string structuredPattern = @"(?:固定ID|ID|编号)[:：]\s*(?:" + Regex.Escape(type.Trim()) + @":)?" + Regex.Escape(id.Trim()) + @"(?![A-Za-z0-9_.-])";
		return Regex.IsMatch(text, structuredPattern, RegexOptions.IgnoreCase);
	}

	private static string BuildActionPostprocessPlayerCurrentLocationFact()
	{
		try
		{
			Settlement settlement = null;
			string source = "";
			try
			{
				settlement = Settlement.CurrentSettlement;
				if (settlement != null)
				{
					source = "当前场景定居点";
				}
			}
			catch
			{
				settlement = null;
			}
			if (settlement == null)
			{
				try
				{
					settlement = MobileParty.MainParty?.CurrentSettlement;
					if (settlement != null)
					{
						source = "玩家主队所在定居点";
					}
				}
				catch
				{
					settlement = null;
				}
			}
			if (settlement == null)
			{
				try
				{
					settlement = Hero.MainHero?.CurrentSettlement;
					if (settlement != null)
					{
						source = "玩家英雄所在定居点";
					}
				}
				catch
				{
					settlement = null;
				}
			}
			if (settlement != null)
			{
				string settlementId = (settlement.StringId ?? "").Trim();
				string settlementName = (settlement.Name?.ToString() ?? settlementId).Trim();
				string type = settlement.IsTown ? "城镇" : (settlement.IsCastle ? "城堡" : (settlement.IsVillage ? "村庄" : (settlement.IsHideout ? "藏身处" : "定居点")));
				if (string.IsNullOrWhiteSpace(settlementId))
				{
					return "";
				}
				StringBuilder sb = new StringBuilder();
				sb.Append("【玩家当前地点ID】当前定居点ID：" + settlementId + "；名称：" + settlementName + "；类型：" + type);
				if (!string.IsNullOrWhiteSpace(source))
				{
					sb.Append("；来源：" + source);
				}
				sb.Append("。若玩家在<latest_reply>语境中说“这里”“此地”“本城”“当前地点”“我们所在的地方”，且动作标签需要定居点目标，目标类型写settlement，id填写上述原始ID。");
				return sb.ToString();
			}
			Settlement nearest = ResolveNearestSettlementToMainPartyForPostprocess(out float distance);
			if (nearest != null)
			{
				string settlementId = (nearest.StringId ?? "").Trim();
				string settlementName = (nearest.Name?.ToString() ?? settlementId).Trim();
				if (!string.IsNullOrWhiteSpace(settlementId))
				{
					return "【玩家当前地点ID】当前未处于定居点内；最近定居点ID：" + settlementId + "；名称：" + settlementName + "；距离：" + distance.ToString("0.0") + "。只有当玩家明确说“附近”“最近的定居点”等语义时才使用最近定居点；目标类型写settlement，id填写上述原始ID；不要把它误当作玩家脚下当前地点。";
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static Settlement ResolveNearestSettlementToMainPartyForPostprocess(out float distance)
	{
		distance = 0f;
		try
		{
			MobileParty party = MobileParty.MainParty;
			if (party == null || Settlement.All == null)
			{
				return null;
			}
			Settlement nearest = Settlement.All
				.Where((Settlement x) => x != null && !x.IsHideout)
				.OrderBy((Settlement x) => x.GatePosition.DistanceSquared(party.Position))
				.FirstOrDefault();
			if (nearest != null)
			{
				distance = nearest.GatePosition.Distance(party.Position);
			}
			return nearest;
		}
		catch
		{
			distance = 0f;
			return null;
		}
	}

	public static string PrepareActionPostprocessHistoryText(string historyText)
	{
		return PrepareActionPostprocessHistoryText(historyText, DuelSettings.GetActionPostprocessHistoryEntryLimitForExternal(), null);
	}

	private static string PrepareActionPostprocessHistoryText(string historyText, int maxEntries, string latestReplyBlock)
	{
		string text = (historyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text) || maxEntries <= 0)
		{
			return "";
		}
		List<ActionPostprocessHistoryEntry> entries = BuildActionPostprocessHistoryEntries(text);
		if (entries.Count == 0)
		{
			return "";
		}
		HashSet<string> latestKeys = BuildActionPostprocessLatestReplyEntryKeys(latestReplyBlock);
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<ActionPostprocessHistoryEntry> selected = new List<ActionPostprocessHistoryEntry>();
		SelectActionPostprocessHistoryEntries(entries, selected, seen, latestKeys, maxEntries, roleOnly: true);
		if (selected.Count < maxEntries)
		{
			SelectActionPostprocessHistoryEntries(entries, selected, seen, latestKeys, maxEntries, roleOnly: false);
		}
		selected = selected.OrderBy((ActionPostprocessHistoryEntry x) => x.Index).ToList();
		return Regex.Replace(string.Join("\n", selected.Select((ActionPostprocessHistoryEntry x) => x.Text).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim(), "[ \\t]{2,}", " ");
	}

	private static void SelectActionPostprocessHistoryEntries(List<ActionPostprocessHistoryEntry> entries, List<ActionPostprocessHistoryEntry> selected, HashSet<string> seen, HashSet<string> latestKeys, int maxEntries, bool roleOnly)
	{
		for (int i = (entries?.Count ?? 0) - 1; i >= 0 && selected.Count < maxEntries; i--)
		{
			ActionPostprocessHistoryEntry entry = entries[i];
			if (entry == null || string.IsNullOrWhiteSpace(entry.Text) || entry.IsRoleMessage != roleOnly)
			{
				continue;
			}
			string key = BuildActionPostprocessHistoryEntryKey(entry.Text);
			if (string.IsNullOrWhiteSpace(key) || latestKeys.Contains(key))
			{
				continue;
			}
			if (seen.Add(key))
			{
				selected.Add(entry);
			}
		}
	}

	private static List<ActionPostprocessHistoryEntry> BuildActionPostprocessHistoryEntries(string historyText)
	{
		List<ActionPostprocessHistoryEntry> entries = new List<ActionPostprocessHistoryEntry>();
		string[] array = (historyText ?? "").Split('\n');
		bool skipRecallBlock = false;
		string pendingAfefPrefix = "";
		string pendingRole = "";
		StringBuilder pendingRoleContent = null;
		int nextIndex = 0;
		for (int i = 0; i < array.Length; i++)
		{
			string rawLine = array[i] ?? "";
			string line = rawLine.Trim();
			if (TryParseActionPostprocessRoleMarker(line, out var role))
			{
				FlushActionPostprocessRoleHistoryEntry(entries, ref nextIndex, pendingRole, pendingRoleContent);
				pendingAfefPrefix = "";
				pendingRole = role;
				pendingRoleContent = new StringBuilder();
				continue;
			}
			if (!string.IsNullOrWhiteSpace(pendingRole))
			{
				if (pendingRoleContent == null)
				{
					pendingRoleContent = new StringBuilder();
				}
				pendingRoleContent.AppendLine(rawLine);
				continue;
			}
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			if (TryParseActionPostprocessAfefHeaderLine(line, out var afefPrefix))
			{
				pendingAfefPrefix = afefPrefix;
				continue;
			}
			if (IsActionPostprocessRecallBlockStart(line))
			{
				pendingAfefPrefix = "";
				skipRecallBlock = true;
				continue;
			}
			if (skipRecallBlock)
			{
				if (IsActionPostprocessRecallBlockEnd(line))
				{
					skipRecallBlock = false;
				}
				else
				{
					continue;
				}
			}
			string text = NormalizeActionPostprocessHistoryContent(line);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!string.IsNullOrWhiteSpace(pendingAfefPrefix))
			{
				if (ShouldSkipActionPostprocessHistoryLine(text))
				{
					pendingAfefPrefix = "";
					continue;
				}
				text = NormalizeActionPostprocessHistoryLine(text);
				if (!text.StartsWith("[AFEF", StringComparison.Ordinal))
				{
					text = (pendingAfefPrefix + " " + text).Trim();
				}
				pendingAfefPrefix = "";
			}
			else
			{
				if (ShouldSkipActionPostprocessHistoryLine(text))
				{
					continue;
				}
				text = NormalizeActionPostprocessHistoryLine(text);
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (IsActionPostprocessHistoryEntryStart(text))
			{
				entries.Add(new ActionPostprocessHistoryEntry
				{
					Index = nextIndex++,
					Text = text,
					IsRoleMessage = false
				});
			}
			else if (entries.Count > 0)
			{
				ActionPostprocessHistoryEntry last = entries[entries.Count - 1];
				last.Text = Regex.Replace((last.Text + " " + text).Trim(), "\\s+", " ");
			}
		}
		FlushActionPostprocessRoleHistoryEntry(entries, ref nextIndex, pendingRole, pendingRoleContent);
		return entries;
	}

	private static bool TryParseActionPostprocessAfefHeaderLine(string line, out string prefix)
	{
		string text = (line ?? "").Trim();
		if (text.Equals("【AFEF玩家行为补充】", StringComparison.Ordinal) || text.Equals("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			prefix = "[AFEF玩家行为补充]";
			return true;
		}
		if (text.Equals("【AFEF NPC行为补充】", StringComparison.Ordinal) || text.Equals("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			prefix = "[AFEF NPC行为补充]";
			return true;
		}
		prefix = "";
		return false;
	}

	private static bool TryParseActionPostprocessRoleMarker(string line, out string role)
	{
		role = "";
		Match match = Regex.Match((line ?? "").Trim(), "^#\\d+\\s+role=([^\\s]+)\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return false;
		}
		role = (match.Groups[1].Value ?? "").Trim();
		return !string.IsNullOrWhiteSpace(role);
	}

	private static void FlushActionPostprocessRoleHistoryEntry(List<ActionPostprocessHistoryEntry> entries, ref int nextIndex, string role, StringBuilder content)
	{
		string text = BuildActionPostprocessRoleHistoryEntry(role, content?.ToString() ?? "");
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		entries.Add(new ActionPostprocessHistoryEntry
		{
			Index = nextIndex++,
			Text = text,
			IsRoleMessage = true
		});
	}

	private static string BuildActionPostprocessRoleHistoryEntry(string role, string content)
	{
		string text = (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		string roleText = (role ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text) || text.IndexOf("<latest_reply>", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("REQUEST_BODY:", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "";
		}
		if (roleText == "user")
		{
			string playerText = ExtractActionPostprocessPlayerTextFromRoleContent(text);
			if (string.IsNullOrWhiteSpace(playerText))
			{
				return "";
			}
			return "玩家: " + playerText;
		}
		if (roleText == "assistant")
		{
			string npcText = ExtractActionPostprocessAssistantTextFromRoleContent(text);
			if (string.IsNullOrWhiteSpace(npcText))
			{
				return "";
			}
			return "NPC: " + npcText;
		}
		return "";
	}

	private static string ExtractActionPostprocessPlayerTextFromRoleContent(string content)
	{
		string text = (content ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.StartsWith("[AFEF", StringComparison.Ordinal) || text.StartsWith("【最近对话历史】", StringComparison.Ordinal) || text.StartsWith("【近期对话窗口】", StringComparison.Ordinal))
		{
			return "";
		}
		int directLabelEnd = FindLastActionPostprocessDirectSpeechLabelEnd(text);
		if (directLabelEnd >= 0 && directLabelEnd < text.Length)
		{
			text = text.Substring(directLabelEnd).Trim();
		}
		else
		{
			string latestPlayer = ExtractLatestPlayerUtteranceForActionPostprocess(text);
			if (!string.IsNullOrWhiteSpace(latestPlayer))
			{
				text = latestPlayer;
			}
		}
		text = NormalizeActionPostprocessDialogueText(NormalizeActionPostprocessHistoryContent(text));
		return text.Length > 500 ? (text.Substring(0, 500).TrimEnd() + "…") : text;
	}

	private static int FindLastActionPostprocessDirectSpeechLabelEnd(string text)
	{
		int best = -1;
		foreach (string token in new string[3] { "对你说】", "对NPC说】", "对玩家说】" })
		{
			int index = (text ?? "").LastIndexOf(token, StringComparison.Ordinal);
			if (index >= 0)
			{
				best = Math.Max(best, index + token.Length);
			}
		}
		return best;
	}

	private static string ExtractActionPostprocessAssistantTextFromRoleContent(string content)
	{
		string text = (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text) || text.StartsWith("[ACTION:", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		int contentIndex = text.LastIndexOf("[CONTENT]", StringComparison.OrdinalIgnoreCase);
		if (contentIndex >= 0)
		{
			text = text.Substring(contentIndex + "[CONTENT]".Length).Trim();
		}
		StringBuilder sb = new StringBuilder(text.Length);
		foreach (string line in text.Split('\n'))
		{
			string cleaned = ShoutUtils.StripConversationMetadataPrefix(NormalizeActionPostprocessHistoryContent(line));
			if (!string.IsNullOrWhiteSpace(cleaned))
			{
				sb.Append(cleaned).Append(' ');
			}
		}
		text = NormalizeActionPostprocessDialogueText(sb.ToString());
		return text.Length > 500 ? (text.Substring(0, 500).TrimEnd() + "…") : text;
	}

	private static bool IsActionPostprocessRecallBlockStart(string line)
	{
		return (line ?? "").Trim().IndexOf("你想起之前的对话与互动", StringComparison.Ordinal) >= 0;
	}

	private static bool IsActionPostprocessRecallBlockEnd(string line)
	{
		string text = (line ?? "").Trim();
		return text.StartsWith("【当前场景公共对话与互动】", StringComparison.Ordinal) || text.StartsWith("【最近对话历史】", StringComparison.Ordinal) || text.StartsWith("【近期对话窗口】", StringComparison.Ordinal);
	}

	private static bool ShouldSkipActionPostprocessHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.StartsWith("——", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【", StringComparison.Ordinal) && text.EndsWith("】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("内容：", StringComparison.Ordinal) || text.StartsWith("日期：", StringComparison.Ordinal) || text.StartsWith("AFEF行为补充：", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【场景事实】", StringComparison.Ordinal))
		{
			return true;
		}
		return Regex.IsMatch(text, "^\\d+#标题[：:]", RegexOptions.CultureInvariant);
	}

	private static string NormalizeActionPostprocessHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		text = ShoutUtils.StripConversationMetadataPrefix(text);
		text = Regex.Replace(text, "^\\[AF_SCENE_SESSION:\\d+\\]\\s*", "", RegexOptions.CultureInvariant);
		text = Regex.Replace(text, "^\\[[^\\]\\r\\n]*[｜|][^\\]\\r\\n]*\\]\\s*", "", RegexOptions.CultureInvariant);
		text = NormalizeActionPostprocessAfefHistoryLine(text);
		text = Regex.Replace(text, "^【[^】]*对(?:你|NPC|[^】]+)说】\\s*", "玩家: ", RegexOptions.CultureInvariant);
		text = Regex.Replace(text, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
		return text;
	}

	private static string NormalizeActionPostprocessAfefHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (text.StartsWith("【AFEF玩家行为补充】", StringComparison.Ordinal))
		{
			return "[AFEF玩家行为补充] " + text.Substring("【AFEF玩家行为补充】".Length).Trim();
		}
		if (text.StartsWith("【AFEF NPC行为补充】", StringComparison.Ordinal))
		{
			return "[AFEF NPC行为补充] " + text.Substring("【AFEF NPC行为补充】".Length).Trim();
		}
		return text;
	}

	private static bool IsActionPostprocessHistoryEntryStart(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("[AFEF", StringComparison.Ordinal))
		{
			return true;
		}
		int num = text.IndexOfAny(new char[2] { ':', '：' });
		if (num <= 0)
		{
			return false;
		}
		string speaker = text.Substring(0, num).Trim();
		return speaker.Equals("玩家", StringComparison.OrdinalIgnoreCase) || speaker.Equals("NPC", StringComparison.OrdinalIgnoreCase) || speaker.Equals("你", StringComparison.OrdinalIgnoreCase) || speaker.Contains("对");
	}

	private static HashSet<string> BuildActionPostprocessLatestReplyEntryKeys(string latestReplyBlock)
	{
		HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string line in (latestReplyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
		{
			string key = BuildActionPostprocessHistoryEntryKey(line);
			if (!string.IsNullOrWhiteSpace(key))
			{
				keys.Add(key);
			}
		}
		return keys;
	}

	private static string BuildActionPostprocessHistoryEntryKey(string line)
	{
		string text = NormalizeActionPostprocessHistoryLine(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOfAny(new char[2] { ':', '：' });
		if (num >= 0 && num + 1 < text.Length)
		{
			text = text.Substring(num + 1).Trim();
		}
		text = Regex.Replace(text, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
		return text;
	}

	private static string NormalizeActionPostprocessHistoryContent(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.StartsWith("【", StringComparison.Ordinal) && text.EndsWith("】", StringComparison.Ordinal))
		{
			return text;
		}
		// Parenthesized and asterisk-delimited prose can describe an immediately
		// performed action. The action postprocessor needs that evidence to choose
		// the correct execution tag, so only normalize whitespace here.
		text = Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
		text = text.Trim();
		text = text.TrimStart('，', '。', '、', '；', '：', ',', ';', ':');
		return text.Trim();
	}

	public static string BuildActionPostprocessLatestReplyBlock(string playerText, string npcReplyText, string npcName, string historyText = null)
	{
		string text = NormalizeActionPostprocessDialogueText(playerText);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ExtractLatestPlayerUtteranceForActionPostprocess(historyText);
		}
		string text2 = NormalizeActionPostprocessDialogueText(npcReplyText);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("玩家: ").Append(string.IsNullOrWhiteSpace(text) ? "（无）" : text);
		stringBuilder.AppendLine();
		stringBuilder.Append("NPC: ").Append(string.IsNullOrWhiteSpace(text2) ? "（无）" : text2);
		return stringBuilder.ToString().Trim();
	}

	private static string NormalizeActionPostprocessDialogueText(string text)
	{
		string text2 = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		return Regex.Replace(text2, "\\s+", " ").Trim();
	}

	private static string ExtractLatestPlayerUtteranceForActionPostprocess(string historyText)
	{
		try
		{
			string text = (historyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = array.Length - 1; i >= 0; i--)
			{
				string text2 = (array[i] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2) || text2.StartsWith("【", StringComparison.Ordinal) || text2.StartsWith("[AFEF", StringComparison.Ordinal))
				{
					continue;
				}
				int num = text2.IndexOfAny(new char[2] { ':', '：' });
				if (num < 0 || num + 1 >= text2.Length)
				{
					continue;
				}
				string text3 = text2.Substring(0, num).Trim();
				string text4 = text2.Substring(num + 1).Trim();
				if (string.IsNullOrWhiteSpace(text4))
				{
					continue;
				}
				if (text3.Equals("玩家", StringComparison.OrdinalIgnoreCase) || text3.Equals("你", StringComparison.OrdinalIgnoreCase) || (text3.Contains("对") && text3.EndsWith("说", StringComparison.Ordinal)))
				{
					return NormalizeActionPostprocessDialogueText(text4);
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static bool DuelStakeEnabled => _guardrail?.DuelStake?.IsEnabled == true;

	public static string DuelStakePlayerWinInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.DuelStake?.PlayerWinInstruction ?? "");

	public static string DuelStakeNpcWinInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.DuelStake?.NpcWinInstruction ?? "");

	public static string DuelStakeInstruction
	{
		get
		{
			DuelStakeConfig duelStakeConfig = _guardrail?.DuelStake;
			if (duelStakeConfig == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrEmpty(duelStakeConfig.PlayerWinInstruction))
			{
				stringBuilder.AppendLine(duelStakeConfig.PlayerWinInstruction);
			}
			if (!string.IsNullOrEmpty(duelStakeConfig.NpcWinInstruction))
			{
				stringBuilder.AppendLine(duelStakeConfig.NpcWinInstruction);
			}
			return ApplyPlayerDisplayNameToGuardrailText(stringBuilder.ToString().Trim());
		}
	}

	private static string NormalizeSemanticText(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text2.Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static List<string> SplitGuardrailIntents(string input, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
	{
		List<string> list = new List<string>();
		try
		{
			string text = NormalizeSemanticText(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			List<string> list2 = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in text)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、' || c == '\n' || c == '\r')
				{
					string text2 = stringBuilder.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						list2.Add(text2);
					}
					stringBuilder.Clear();
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			string text3 = stringBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				list2.Add(text3);
			}
			if (list2.Count <= 0)
			{
				list2.Add(text);
			}
			List<string> list3 = new List<string>();
			string[] array = new string[13]
			{
				"然后", "顺便", "另外", "再说", "并且", "而且", "以及", "同时", "还有", "再加上",
				"顺带", "并且还", "以及还"
			};
			for (int j = 0; j < list2.Count; j++)
			{
				string text4 = (list2[j] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text4))
				{
					continue;
				}
				bool flag = false;
				foreach (string text5 in array)
				{
					int num = text4.IndexOf(text5, StringComparison.Ordinal);
					if (num > 1 && num < text4.Length - text5.Length - 1)
					{
						string text6 = text4.Substring(0, num).Trim();
						string text7 = text4.Substring(num + text5.Length).Trim();
						if (text6.Length >= 2)
						{
							list3.Add(text6);
						}
						if (text7.Length >= 2)
						{
							list3.Add(text7);
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list3.Add(text4);
				}
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			list.Add(text);
			hashSet.Add(text);
			for (int l = 0; l < list3.Count; l++)
			{
				if (list.Count >= Math.Max(1, maxParts))
				{
					break;
				}
				string text8 = NormalizeSemanticText(list3[l]);
				if (!string.IsNullOrWhiteSpace(text8) && text8.Length >= 2 && hashSet.Add(text8))
				{
					list.Add(text8);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		if (list.Count <= 0)
		{
			string text9 = NormalizeSemanticText(input);
			if (!string.IsNullOrWhiteSpace(text9))
			{
				list = IntentQueryOptimizer.OptimizeSplitIntents(new List<string> { text9 }, 1);
			}
		}
		return list;
	}

	private static float DotProductNormalized(float[] a, float[] b)
	{
		try
		{
			if (a == null || b == null || a.Length == 0 || b.Length == 0)
			{
				return 0f;
			}
			int num = Math.Min(a.Length, b.Length);
			double num2 = 0.0;
			for (int i = 0; i < num; i++)
			{
				num2 += (double)a[i] * (double)b[i];
			}
			return (float)num2;
		}
		catch
		{
			return 0f;
		}
	}

	private static GuardrailGateProfile GetGuardrailGateProfile(string ruleTag, int inputLen)
	{
		GuardrailGateProfile result = new GuardrailGateProfile
		{
			AmpGate = 0.53f,
			ForceHitGate = 0.56f,
			RawFloor = 0.44f,
			GapBoost = 2.35f,
			CenterBoost = 0.72f,
			TopGapGate = 0.045f,
			AnchorRawFloor = 0.62f
		};
		switch ((ruleTag ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			result.AmpGate = 0.57f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.45f;
			result.CenterBoost = 0.78f;
			result.TopGapGate = 0.055f;
			result.AnchorRawFloor = 0.65f;
			break;
		case "reward":
			result.AmpGate = 0.5f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.55f;
			result.CenterBoost = 0.8f;
			result.TopGapGate = 0.035f;
			result.AnchorRawFloor = 0.45f;
			break;
		case "loan":
			result.AmpGate = 0.58f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.6f;
			result.CenterBoost = 0.8f;
			result.TopGapGate = 0.06f;
			result.AnchorRawFloor = 0.67f;
			break;
		case "surroundings":
			result.AmpGate = 0.54f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.3f;
			result.CenterBoost = 0.7f;
			result.TopGapGate = 0.05f;
			result.AnchorRawFloor = 0.63f;
			break;
		}
		if (result.AmpGate < 0.3f)
		{
			result.AmpGate = 0.3f;
		}
		if (result.AmpGate > 0.95f)
		{
			result.AmpGate = 0.95f;
		}
		if (result.ForceHitGate < 0f)
		{
			result.ForceHitGate = 0f;
		}
		if (result.ForceHitGate > 1f)
		{
			result.ForceHitGate = 1f;
		}
		if (result.RawFloor < 0.1f)
		{
			result.RawFloor = 0.1f;
		}
		if (result.RawFloor > 0.9f)
		{
			result.RawFloor = 0.9f;
		}
		if (result.TopGapGate < 0f)
		{
			result.TopGapGate = 0f;
		}
		if (result.TopGapGate > 0.3f)
		{
			result.TopGapGate = 0.3f;
		}
		if (result.AnchorRawFloor < 0.1f)
		{
			result.AnchorRawFloor = 0.1f;
		}
		if (result.AnchorRawFloor > 0.95f)
		{
			result.AnchorRawFloor = 0.95f;
		}
		return result;
	}

	private static List<string> GetBuiltInIntentAnchorSeeds(string ruleTag)
	{
		List<string> list = new List<string>();
		try
		{
			string text = (ruleTag ?? "").Trim().ToLowerInvariant();
			List<string> guardrailKeywordsByTag = GetGuardrailKeywordsByTag(ruleTag);
			if (guardrailKeywordsByTag != null && guardrailKeywordsByTag.Count > 0)
			{
				list.AddRange(guardrailKeywordsByTag);
			}
			switch (text)
			{
			case "reward":
				list.Add("我想和你做点生意");
				list.Add("我想和你交易");
				list.Add("我们谈谈买卖");
				list.Add("我想买东西");
				list.Add("我想卖东西");
				list.Add("看看你有什么货");
				list.Add("谈个价格");
				list.Add("交换物品");
				break;
			case "loan":
				list.Add("我想借钱周转");
				list.Add("我想赊账");
				list.Add("我欠你钱");
				list.Add("还款期限怎么定");
				list.Add("谈还款日");
				break;
			case "duel":
				list.Add("我想和你决斗");
				list.Add("我们单挑");
				list.Add("来比试一场");
				list.Add("你敢不敢决斗");
				break;
			case "surroundings":
				list.Add("这里是哪里");
				list.Add("附近有什么地方");
				list.Add("离哪座城最近");
				list.Add("这地方属于谁");
				list.Add("往北往南有什么");
				break;
			}
		}
		catch
		{
		}
		return NormalizeStringList(list, 96);
	}

	private static float GetBuiltInIntentEvidenceGate(string ruleTag, int inputLen)
	{
		float num = 0.52f;
		switch ((ruleTag ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			num = 0.52f;
			break;
		case "reward":
			num = 0.47f;
			break;
		case "loan":
			num = 0.52f;
			break;
		case "surroundings":
			num = 0.56f;
			break;
		}
		if (num < 0.2f)
		{
			num = 0.2f;
		}
		if (num > 0.92f)
		{
			num = 0.92f;
		}
		return num;
	}

	private static float ComputeBuiltInIntentSemanticEvidence(string ruleTag, List<GuardrailIntentInput> queryInputs, out string bestSeed)
	{
		bestSeed = "";
		try
		{
			if (queryInputs == null || queryInputs.Count <= 0)
			{
				return 0f;
			}
			List<string> builtInIntentAnchorSeeds = GetBuiltInIntentAnchorSeeds(ruleTag);
			if (builtInIntentAnchorSeeds == null || builtInIntentAnchorSeeds.Count <= 0)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < builtInIntentAnchorSeeds.Count; i++)
			{
				string text = NormalizeSemanticText(builtInIntentAnchorSeeds[i]);
				if (string.IsNullOrWhiteSpace(text) || !TryGetPhraseEmbedding(text, out var vec) || vec == null || vec.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < queryInputs.Count; j++)
				{
					GuardrailIntentInput guardrailIntentInput = queryInputs[j];
					if (guardrailIntentInput?.Vector == null || guardrailIntentInput.Vector.Length == 0)
					{
						continue;
					}
					float num2 = DotProductNormalized(guardrailIntentInput.Vector, vec) * Math.Max(0f, guardrailIntentInput.Weight);
					if (num2 > num)
					{
						num = num2;
						bestSeed = text;
					}
				}
			}
			return num;
		}
		catch
		{
			return 0f;
		}
	}

	private static float ApplyGuardrailAmplifiedScore(float raw, float maxOther, float meanAll, GuardrailGateProfile p)
	{
		float num = ((maxOther <= -0.5f) ? raw : (raw - maxOther));
		float num2 = raw - meanAll;
		float num3 = raw + num * p.GapBoost + num2 * p.CenterBoost;
		if (num3 < 0f)
		{
			num3 = 0f;
		}
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		return num3;
	}

	public static void SetGuardrailSemanticContext(string contextText)
	{
		try
		{
			_guardrailSemanticRuntimeContext.Value = NormalizeGuardrailContextText(contextText);
		}
		catch
		{
		}
	}

	private static string GetRuntimeGuardrailContext()
	{
		try
		{
			string text = NormalizeGuardrailContextText(_guardrailSemanticRuntimeContext.Value);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			if (text.Length > 600)
			{
				text = text.Substring(text.Length - 600);
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeGuardrailContextText(string text)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			string[] array = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split(new char[1] { '\n' }, StringSplitOptions.None);
			List<string> list = new List<string>();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = (array[i] ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(text2);
				}
			}
			return string.Join("\n", list);
		}
		catch
		{
			return NormalizeSemanticText(text);
		}
	}

	private static bool IsBuiltInRuleTag(string tag)
	{
		string text = (tag ?? "").Trim().ToLowerInvariant();
		int result;
		switch (text)
		{
		default:
			result = ((text == "surroundings") ? 1 : 0);
			break;
		case "duel":
		case "reward":
		case "loan":
			result = 1;
			break;
		}
		return (byte)result != 0;
	}

	private static bool IsRuleCurrentlyEligibleForRag(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (ShouldExcludeRuntimeRuleForConversationTarget(text))
		{
			return false;
		}
		if (IsSceneMoveRule(text) && ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			return false;
		}
		if (string.Equals(text, "siege_intervention_aftermath", StringComparison.OrdinalIgnoreCase))
		{
			return AfGcczShoutBridge.IsActive();
		}
		if (string.Equals(text, "kingdom_vassalage", StringComparison.OrdinalIgnoreCase))
		{
			return VassalageBehavior.CanInjectVassalageRuleForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
		}
		if (string.Equals(text, "diplomacy", StringComparison.OrdinalIgnoreCase))
		{
			return DiplomacyBehavior.CanInjectDiplomacyRuleForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
		}
		if (string.Equals(text, "world_diplomacy_discussion", StringComparison.OrdinalIgnoreCase))
		{
			return WorldDiplomacyBehavior.CanDiscussWorldDiplomacyForExternal(ResolveConversationTargetHero());
		}
		if (string.Equals(text, "kingdom_agenda", StringComparison.OrdinalIgnoreCase))
		{
			return IsKingdomLordOrKingRuleTargetForPreprocess(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
		}
		if (IsSceneAutoGroupRelayRule(text))
		{
			return false;
		}
		if (string.Equals(text, "noble_deference", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!string.Equals(text, "vanilla_issue", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		try
		{
			return ResolveConversationTargetHero() != null
				|| ResolveConversationTargetCharacter() != null
				|| !string.IsNullOrWhiteSpace(ResolveRuntimeTargetTroopId())
				|| !string.IsNullOrWhiteSpace(ResolveRuntimeTargetUnnamedRank());
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNobleDeferenceRuntimeEligible(bool hasAnyHero)
	{
		return false;
	}

	private static List<string> NormalizeStringList(List<string> source, int maxLen = 80)
	{
		List<string> list = new List<string>();
		try
		{
			if (source == null || source.Count <= 0)
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < source.Count; i++)
			{
				string text = NormalizeSemanticText(source[i]);
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (maxLen > 0 && text.Length > maxLen)
					{
						text = text.Substring(0, maxLen);
					}
					if (hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static List<string> NormalizeTriggerKeywordList(List<string> source, int minLen = 2, int maxLen = 8)
	{
		List<string> list = new List<string>();
		try
		{
			if (source == null || source.Count <= 0)
			{
				return list;
			}
			if (minLen < 1)
			{
				minLen = 1;
			}
			if (maxLen < minLen)
			{
				maxLen = minLen;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < source.Count; i++)
			{
				string text = NormalizeSemanticText(source[i]);
				if (!string.IsNullOrWhiteSpace(text) && text.Length >= minLen)
				{
					if (text.Length > maxLen)
					{
						text = text.Substring(0, maxLen);
					}
					if (hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static Dictionary<string, string> NormalizeTemplateMap(Dictionary<string, string> source, int maxKeyLen = 80)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (source == null || source.Count <= 0)
			{
				return dictionary;
			}
			foreach (KeyValuePair<string, string> item in source)
			{
				string text = NormalizeSemanticText(item.Key);
				string text2 = (item.Value ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (maxKeyLen > 0 && text.Length > maxKeyLen)
				{
					text = text.Substring(0, maxKeyLen);
				}
				dictionary[text.ToLowerInvariant()] = text2;
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static string NormalizeRuleCode(string code, string id, string label = null)
	{
		string text = (code ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			string text2 = (id ?? "").Trim().ToLowerInvariant();
			text = text2 switch
			{
				"duel" => "DUEL",
				"reward" => "TRADE",
				"loan" => "DEBT",
				"surroundings" => "NEARBY",
				"kingdom_service" => "KINGDOM",
				"lords_hall_access" => "PASSAGE",
				"marriage" => "MARRIAGE",
				"scene_mechanism_actions" => "SCENE_MOVE",
				"party_transfer" => "PARTY_TRANSFER",
				"vanilla_issue" => "ISSUE",
				"npc_major_actions" => "NPC_MAJOR",
				"encounter_release_player" => "MEETING_RELEASE",
				"noble_deference" => "NOBLE_PRESSURE",
				"kingdom_agenda" => "KINGDOM_AGENDA",
				"diplomacy" => "DIPLOMACY",
				_ => ""
			};
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (label ?? id ?? "RULE").Trim();
		}
		text = Regex.Replace(text.ToUpperInvariant(), "[^A-Z0-9_]+", "_").Trim('_');
		return string.IsNullOrWhiteSpace(text) ? "RULE" : text;
	}

	private static GuardrailRulePromptConfig BuildLegacyRulePrompt(string id, bool enabled, string instruction, List<string> triggerKeywords, string group, int priority, int topicNumber, string topicLabel, string code = "", string preprocessExcludedInstruction = "")
	{
		return new GuardrailRulePromptConfig
		{
			Id = (id ?? "").Trim().ToLowerInvariant(),
			IsEnabled = enabled,
			TopicNumber = topicNumber,
			TopicLabel = (topicLabel ?? "").Trim(),
			Code = NormalizeRuleCode(code, id, topicLabel),
			Instruction = (instruction ?? ""),
			PreprocessExcludedInstruction = (preprocessExcludedInstruction ?? ""),
			TriggerKeywords = NormalizeTriggerKeywordList(triggerKeywords),
			Group = (group ?? "").Trim(),
			Priority = priority
		};
	}

	private static GuardrailRulePromptConfig NormalizeCustomRulePrompt(GuardrailRulePromptConfig src, int autoIndex)
	{
		try
		{
			if (src == null)
			{
				return null;
			}
			string text = (src.Id ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "rule_" + autoIndex;
			}
			return new GuardrailRulePromptConfig
			{
				Id = text,
				IsEnabled = src.IsEnabled,
				Group = (src.Group ?? "").Trim(),
				Priority = src.Priority,
				TopicNumber = src.TopicNumber,
				TopicLabel = (src.TopicLabel ?? "").Trim(),
				Code = NormalizeRuleCode(src.Code, text, src.TopicLabel),
				Instruction = (src.Instruction ?? ""),
				NonHeroInstruction = (src.NonHeroInstruction ?? ""),
				PreprocessExcludedInstruction = (src.PreprocessExcludedInstruction ?? ""),
				PostprocessRules = ((src.PostprocessRules != null) ? src.PostprocessRules.Where((PostprocessRuleEntry x) => x != null && !string.IsNullOrWhiteSpace((x.Tag ?? "").Trim())).Select((PostprocessRuleEntry x) => new PostprocessRuleEntry
				{
					Tag = (x.Tag ?? "").Trim(),
					Description = (x.Description ?? "").Trim(),
					SingleFramedNpcDescription = (x.SingleFramedNpcDescription ?? "").Trim()
				}).ToList() : new List<PostprocessRuleEntry>()),
				TriggerKeywords = NormalizeTriggerKeywordList(src.TriggerKeywords),
				RuntimeInstructionTemplates = NormalizeTemplateMap(src.RuntimeInstructionTemplates),
				RuntimeConstraintTemplates = NormalizeTemplateMap(src.RuntimeConstraintTemplates)
			};
		}
		catch
		{
			return null;
		}
	}

	private static Dictionary<string, GuardrailRulePromptConfig> BuildRulePromptRegistry()
	{
		Dictionary<string, GuardrailRulePromptConfig> map = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string duelRegistryInstruction = (_guardrail?.Duel?.TriggerInstruction ?? "").Trim();
			if (string.IsNullOrWhiteSpace(duelRegistryInstruction))
			{
				duelRegistryInstruction = (_guardrail?.Duel?.DialogueInstruction ?? "").Trim();
			}
			upsert(BuildLegacyRulePrompt("duel", _guardrail?.Duel?.IsEnabled ?? true, duelRegistryInstruction, _guardrail?.Duel?.AcceptKeywords ?? new List<string>(), "combat", 90, _guardrail?.Duel?.TopicNumber ?? 0, _guardrail?.Duel?.TopicLabel ?? "", _guardrail?.Duel?.Code ?? "", _guardrail?.Duel?.PreprocessExcludedInstruction ?? ""));
			upsert(BuildLegacyRulePrompt("reward", _guardrail?.Reward?.IsEnabled ?? true, _guardrail?.Reward?.Instruction ?? "", _guardrail?.Reward?.TriggerKeywords ?? new List<string>(), "trade", 80, _guardrail?.Reward?.TopicNumber ?? 0, _guardrail?.Reward?.TopicLabel ?? "", _guardrail?.Reward?.Code ?? "", _guardrail?.Reward?.PreprocessExcludedInstruction ?? ""));
			if (_guardrail?.Loan != null)
			{
				upsert(BuildLegacyRulePrompt("loan", _guardrail.Loan.IsEnabled, _guardrail.Loan.Instruction ?? "", _guardrail.Loan.TriggerKeywords ?? new List<string>(), "finance", 85, _guardrail.Loan.TopicNumber, _guardrail.Loan.TopicLabel ?? "", _guardrail.Loan.Code ?? "", _guardrail.Loan.PreprocessExcludedInstruction ?? ""));
			}
			upsert(BuildLegacyRulePrompt("surroundings", _guardrail?.Surroundings?.IsEnabled ?? true, _guardrail?.Surroundings?.Instruction ?? "", _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>(), "world", 70, _guardrail?.Surroundings?.TopicNumber ?? 0, _guardrail?.Surroundings?.TopicLabel ?? "", _guardrail?.Surroundings?.Code ?? "", _guardrail?.Surroundings?.PreprocessExcludedInstruction ?? ""));
			if (_guardrail?.RulePrompts != null && _guardrail.RulePrompts.Count > 0)
			{
				for (int i = 0; i < _guardrail.RulePrompts.Count; i++)
				{
					GuardrailRulePromptConfig rule = NormalizeCustomRulePrompt(_guardrail.RulePrompts[i], i + 1);
					upsert(rule);
				}
			}
		}
		catch
		{
		}
		return map;
		void upsert(GuardrailRulePromptConfig guardrailRulePromptConfig)
		{
			if (guardrailRulePromptConfig != null)
			{
				string text = (guardrailRulePromptConfig.Id ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					guardrailRulePromptConfig.Id = text;
					guardrailRulePromptConfig.Code = NormalizeRuleCode(guardrailRulePromptConfig.Code, text, guardrailRulePromptConfig.TopicLabel);
					map[text] = guardrailRulePromptConfig;
				}
			}
		}
	}

	private static List<GuardrailRulePromptConfig> GetAllEnabledRulePrompts()
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			return (from r in dictionary.Values
				where r != null && r.IsEnabled && !string.IsNullOrWhiteSpace(r.Id) && IsRuleCurrentlyEligibleForRag(r.Id)
				orderby r.Priority descending
				select r).ThenBy((GuardrailRulePromptConfig r) => r.Id, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return new List<GuardrailRulePromptConfig>();
		}
	}

	private static GuardrailRulePromptConfig GetRulePromptByTag(string ruleTag)
	{
		try
		{
			string text = (ruleTag ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary.TryGetValue(text, out var value) && value != null)
			{
				return value;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetGuardrailInstructionByTag(string ruleTag)
	{
		return GetRulePromptByTag(ruleTag)?.Instruction ?? "";
	}

	private static List<string> GetGuardrailKeywordsByTag(string ruleTag)
	{
		return GetRulePromptByTag(ruleTag)?.TriggerKeywords ?? new List<string>();
	}

	public static string GetGuardrailRuleInstruction(string ruleTag)
	{
		return GetGuardrailInstructionByTag(ruleTag);
	}

	public static List<string> GetGuardrailRuleKeywords(string ruleTag)
	{
		List<string> guardrailKeywordsByTag = GetGuardrailKeywordsByTag(ruleTag);
		return (guardrailKeywordsByTag == null) ? new List<string>() : new List<string>(guardrailKeywordsByTag);
	}

	private static string BuildRuleInstructionSeed(string ruleTag, string ruleInstruction)
	{
		string text = NormalizeSemanticText(ruleTag);
		string text2 = NormalizeSemanticText(ruleInstruction);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		int num = text2.IndexOfAny(new char[9] { '。', '！', '!', '？', '?', '\n', '\r', ';', '；' });
		if (num > 0)
		{
			text2 = text2.Substring(0, num);
		}
		if (text2.Length > 120)
		{
			text2 = text2.Substring(0, 120);
		}
		return string.IsNullOrWhiteSpace(text) ? text2 : (text + " " + text2);
	}

	private static List<string> BuildRuleSemanticSeeds(string ruleTag, string ruleInstruction, List<string> triggerKeywords)
	{
		List<string> seeds = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (triggerKeywords != null)
			{
				for (int i = 0; i < triggerKeywords.Count; i++)
				{
					string text = NormalizeSemanticText(triggerKeywords[i]);
					if (!string.IsNullOrWhiteSpace(text))
					{
						addSeed(text);
					}
				}
			}
			if (string.Equals((ruleTag ?? "").Trim(), "reward", StringComparison.OrdinalIgnoreCase))
			{
				addSeed(BuildRuleInstructionSeed(ruleTag, ruleInstruction));
			}
			if (seeds.Count <= 0)
			{
				addSeed(ruleTag);
			}
		}
		catch
		{
		}
		return seeds;
		void addSeed(string raw)
		{
			string text2 = NormalizeSemanticText(raw);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				if (text2.Length > 260)
				{
					text2 = text2.Substring(0, 260);
				}
				if (seen.Add(text2))
				{
					seeds.Add(text2);
				}
			}
		}
	}

	internal static void TryStartBackgroundSemanticWarmup(string source)
	{
		try
		{
			long num = Volatile.Read(ref _guardrailConfigVersion);
			if (num <= 0)
			{
				num = 1L;
			}
			if (Volatile.Read(ref _guardrailWarmupState) == 2 && Volatile.Read(ref _guardrailWarmupVersion) == num)
			{
				return;
			}
			if (Interlocked.CompareExchange(ref _guardrailWarmupState, 1, 0) != 0)
			{
				return;
			}
			Interlocked.Exchange(ref _guardrailWarmupVersion, num);
			string warmupSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
			Logger.Log("GuardrailWarmup", $"start source={warmupSource} version={num}");
			Task.Run(delegate
			{
				RunGuardrailSemanticWarmup(warmupSource, num);
			});
		}
		catch
		{
		}
	}

	private static void RunGuardrailSemanticWarmup(string source, long version)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		int num = 0;
		int num2 = 0;
		string text = "";
		try
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			for (int i = 0; i < allEnabledRulePrompts.Count; i++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[i];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				List<string> list = BuildRuleSemanticSeeds(guardrailRulePromptConfig.Id, guardrailRulePromptConfig.Instruction ?? "", guardrailRulePromptConfig.TriggerKeywords);
				for (int j = 0; j < list.Count; j++)
				{
					string item = NormalizeSemanticText(list[j]);
					if (!string.IsNullOrWhiteSpace(item))
					{
						hashSet.Add(item);
					}
				}
			}
			num = hashSet.Count;
			foreach (string item3 in hashSet)
			{
				if (TryGetPhraseEmbedding(item3, out var vec) && vec != null && vec.Length != 0)
				{
					num2++;
				}
			}
		}
		catch (Exception ex)
		{
			text = ex.Message ?? "guardrail warmup exception";
		}
		stopwatch.Stop();
		bool flag = Volatile.Read(ref _guardrailConfigVersion) != version;
		if (flag)
		{
			Interlocked.Exchange(ref _guardrailWarmupState, 0);
			Interlocked.Exchange(ref _guardrailWarmupVersion, -1L);
		}
		else
		{
			Interlocked.Exchange(ref _guardrailWarmupState, 2);
		}
		Logger.Log("GuardrailWarmup", $"complete source={source} version={version} stale={flag} ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} seedCount={num} warmed={num2} error={text}");
	}

	private static bool TryGetInputEmbedding(string input, out float[] vec)
	{
		vec = null;
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailInputVecCache.TryGetValue(text, out var value) && value != null && value.Length != 0)
			{
				vec = value;
				return true;
			}
		}
		OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
		if (instance == null || !instance.IsAvailable)
		{
			return false;
		}
		if (!instance.TryGetEmbedding(text, out var vector) || vector == null || vector.Length == 0)
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailInputVecCache.Count >= 256)
			{
				_guardrailInputVecCache.Clear();
			}
			_guardrailInputVecCache[text] = vector;
		}
		vec = vector;
		return true;
	}

	private static bool TryGetPhraseEmbedding(string phraseSeed, out float[] vec)
	{
		vec = null;
		string text = NormalizeSemanticText(phraseSeed);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailPhraseVecCache.TryGetValue(text, out var value) && value != null && value.Length != 0)
			{
				vec = value;
				return true;
			}
		}
		OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
		if (instance == null || !instance.IsAvailable)
		{
			return false;
		}
		if (!instance.TryGetEmbedding(text, out var vector) || vector == null || vector.Length == 0)
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailPhraseVecCache.Count >= 1024)
			{
				_guardrailPhraseVecCache.Clear();
			}
			_guardrailPhraseVecCache[text] = vector;
		}
		vec = vector;
		return true;
	}

	private static string BuildGuardrailEvalKey(string userText, string contextText, string secondaryText)
	{
		string text = NormalizeSemanticText(userText);
		string text2 = NormalizeSemanticText(contextText);
		string text3 = NormalizeSemanticText(secondaryText);
		return text + "||" + text2 + "||" + text3;
	}

	private static bool ContainsAnyIgnoreCase(string source, params string[] patterns)
	{
		string text = (source ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || patterns == null || patterns.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < patterns.Length; i++)
		{
			string value = (patterns[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string ResolveAuxiliaryThinkingControlMode(string apiUrl, string modelName)
	{
		string format = DuelSettings.ResolveThinkingControlFormat(apiUrl, modelName);
		return format == "plain" ? "plain" : format + "_thinking";
	}

	private static bool LooksLikeAuxiliaryThinkingControlError(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		bool flag = ContainsAnyIgnoreCase(text, "thinking", "reasoning_effort", "output_config", "budget_tokens");
		bool flag2 = ContainsAnyIgnoreCase(text, "unsupported", "unknown", "invalid", "unexpected", "not allowed", "not supported", "extra inputs are not permitted");
		return flag && flag2;
	}

	internal static bool LooksLikeAuxiliaryThinkingControlErrorForExternal(
		string responseBody)
	{
		return LooksLikeAuxiliaryThinkingControlError(responseBody);
	}

	private static int ResolveAuxiliaryApiMaxTokens(DuelSettings settings, int fallbackMaxTokens)
	{
		try
		{
			return settings?.GetAuxiliaryApiMaxTokens() ?? DuelSettings.ClampApiMaxTokens(fallbackMaxTokens, DuelSettings.DefaultGeneralApiMaxTokens);
		}
		catch
		{
			return DuelSettings.ClampApiMaxTokens(fallbackMaxTokens, DuelSettings.DefaultGeneralApiMaxTokens);
		}
	}

	private static JObject BuildAuxiliaryRouterRequestPayload(string apiUrl, string modelName, IEnumerable<object> messages, int maxTokens, float temperature, out string controlMode, bool disableThinkingControls = false, bool useConfiguredMaxTokens = true, bool useConfiguredTemperature = true)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		controlMode = ResolveAuxiliaryThinkingControlMode(apiUrl, modelName);
		int normalizedMaxTokens = Math.Max(16, useConfiguredMaxTokens ? ResolveAuxiliaryApiMaxTokens(settings, maxTokens) : maxTokens);
		if (!disableThinkingControls && controlMode == "anthropic_thinking")
		{
			normalizedMaxTokens = Math.Max(2048, normalizedMaxTokens);
		}
		JObject jObject = new JObject
		{
			["model"] = modelName ?? "",
			["messages"] = JArray.FromObject(messages ?? Array.Empty<object>()),
			["stream"] = false,
			["max_tokens"] = normalizedMaxTokens,
			["temperature"] = DuelSettings.ClampApiTemperature(
				useConfiguredTemperature
					? settings?.GetAuxiliaryApiTemperature() ?? temperature
					: temperature)
		};
		if (disableThinkingControls)
		{
			controlMode = "plain";
			return jObject;
		}
		bool thinkingEnabled = settings?.AuxiliaryApiThinkingEnabled ?? false;
		string effort = settings?.GetAuxiliaryApiReasoningEffort() ?? DuelSettings.ReasoningEffortHigh;
		DuelSettings.ApplyThinkingControls(jObject, apiUrl, modelName, thinkingEnabled, effort, out controlMode);
		return jObject;
	}

	public static string BuildAuxiliaryRouterRequestJsonForExternal(string apiUrl, string modelName, IEnumerable<object> messages, int maxTokens, float temperature, out string controlMode, bool disableThinkingControls = false, bool useConfiguredMaxTokens = true, bool useConfiguredTemperature = true)
	{
		JObject payload = BuildAuxiliaryRouterRequestPayload(apiUrl, modelName, messages, maxTokens, temperature, out controlMode, disableThinkingControls, useConfiguredMaxTokens, useConfiguredTemperature);
		return LlmApiCompat.PrepareChatRequestJson(apiUrl, payload);
	}


	private static string BuildAuxiliarySimpleDialogueRequestJson(string apiUrl, string modelName, IEnumerable<object> messages, int maxTokens, float temperature, out string controlMode)
	{
		int requestMaxTokens = Math.Max(Math.Max(16, maxTokens), maxTokens + 512);
		JObject payload = BuildAuxiliaryRouterRequestPayload(apiUrl, modelName, messages, requestMaxTokens, temperature, out controlMode, disableThinkingControls: true);
		if (DuelSettings.ApplyThinkingControls(payload, apiUrl, modelName, thinkingEnabled: false, DuelSettings.ReasoningEffortHigh, out var disabledThinkingMode))
		{
			controlMode = disabledThinkingMode;
		}
		return LlmApiCompat.PrepareChatRequestJson(apiUrl, payload);
	}

	private static bool TryGetAuxiliaryRuleRoutingConfig(out string apiUrl, out string apiKey, out string modelName)
	{
		apiUrl = "";
		apiKey = "";
		modelName = "";
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || (!settings.UseAuxiliaryRuleApi && settings.MemoryPreprocessMode != 1 && settings.MemoryPreprocessMode != 2))
			{
				return false;
			}
			apiUrl = DuelSettings.GetEffectiveApiUrl(settings.AuxiliaryApiUrl ?? "");
			apiKey = (settings.AuxiliaryApiKey ?? "").Trim();
			modelName = settings.GetEffectiveAuxiliaryModelName();
			bool flag = !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
			if (!flag)
			{
				LogAuxiliaryRouterTokenTrace("auxiliary_router_config_invalid", null, "[AUXILIARY ROUTER CONFIG]" + "\n" + "enabled=True" + "\n" + "url=" + (string.IsNullOrWhiteSpace(apiUrl) ? "(empty)" : apiUrl) + "\n" + "model=" + (string.IsNullOrWhiteSpace(modelName) ? "(empty)" : modelName) + "\n" + "apiKey=" + (string.IsNullOrWhiteSpace(apiKey) ? "(empty)" : "(present)") + "\n" + "reason=missing_required_field", 0);
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetDedicatedActionPostprocessConfig(out string apiUrl, out string apiKey, out string modelName)
	{
		apiUrl = "";
		apiKey = "";
		modelName = "";
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return false;
			}
			string text = (settings.ActionPostprocessApiUrl ?? "").Trim();
			string text2 = (settings.ActionPostprocessApiKey ?? "").Trim();
			string text3 = settings.GetEffectiveActionPostprocessModelName();
			string text4 = settings.GetActionPostprocessSelectedModelOption();
			bool flag = !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(text2) || !string.IsNullOrWhiteSpace((settings.ActionPostprocessModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(text4);
			if (!flag)
			{
				return false;
			}
			apiUrl = DuelSettings.GetEffectiveApiUrl(text);
			apiKey = text2;
			modelName = text3;
			bool flag2 = !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
			if (!flag2)
			{
				LogAuxiliaryRouterTokenTrace("action_postprocess_config_invalid", null, "[ACTION POSTPROCESS CONFIG]\nmode=dedicated\nurl=" + (string.IsNullOrWhiteSpace(apiUrl) ? "(empty)" : apiUrl) + "\nmodel=" + (string.IsNullOrWhiteSpace(modelName) ? "(empty)" : modelName) + "\napiKey=" + (string.IsNullOrWhiteSpace(apiKey) ? "(empty)" : "(present)") + "\nreason=missing_required_field", 0);
			}
			return flag2;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetPrimaryChatConfig(out string apiUrl, out string apiKey, out string modelName)
	{
		apiUrl = "";
		apiKey = "";
		modelName = "";
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return false;
			}
			apiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl ?? "");
			apiKey = (settings.ApiKey ?? "").Trim();
			modelName = settings.GetEffectiveMainModelName();
			return !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetActionPostprocessConfig(out string apiUrl, out string apiKey, out string modelName)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		bool flag = settings != null && (!string.IsNullOrWhiteSpace(settings.ActionPostprocessApiUrl) || !string.IsNullOrWhiteSpace(settings.ActionPostprocessApiKey) || !string.IsNullOrWhiteSpace(settings.ActionPostprocessModelName) || !string.IsNullOrWhiteSpace(settings.GetActionPostprocessSelectedModelOption()));
		if (flag)
		{
			if (TryGetDedicatedActionPostprocessConfig(out apiUrl, out apiKey, out modelName))
			{
				return true;
			}
			LogAuxiliaryRouterTokenTrace("action_postprocess_config_fallback_main", null, "[ACTION POSTPROCESS CONFIG]\nmode=fallback_main\nreason=dedicated_config_incomplete", 0);
		}
		return TryGetPrimaryChatConfig(out apiUrl, out apiKey, out modelName);
	}

	public static bool CanUseAuxiliaryActionPostprocess()
	{
		return ActionPostprocessEnabled && TryGetActionPostprocessConfig(out var _, out var _, out var _);
	}

	private static int ResolveActionPostprocessApiMaxTokens(string apiUrl, string apiKey, string modelName, int fallbackMaxTokens)
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return DuelSettings.ClampApiMaxTokens(fallbackMaxTokens, DuelSettings.DefaultGeneralApiMaxTokens);
			}
			string dedicatedUrl = DuelSettings.GetEffectiveApiUrl((settings.ActionPostprocessApiUrl ?? "").Trim());
			string dedicatedKey = (settings.ActionPostprocessApiKey ?? "").Trim();
			string dedicatedModel = settings.GetEffectiveActionPostprocessModelName();
			bool usesDedicated = !string.IsNullOrWhiteSpace(dedicatedUrl)
				&& !string.IsNullOrWhiteSpace(dedicatedKey)
				&& !string.IsNullOrWhiteSpace(dedicatedModel)
				&& string.Equals(dedicatedUrl, apiUrl ?? "", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(dedicatedKey, apiKey ?? "", StringComparison.Ordinal)
				&& string.Equals(dedicatedModel, modelName ?? "", StringComparison.OrdinalIgnoreCase);
			return usesDedicated ? settings.GetActionPostprocessApiMaxTokens() : settings.GetMainApiMaxTokens();
		}
		catch
		{
			return DuelSettings.ClampApiMaxTokens(fallbackMaxTokens, DuelSettings.DefaultGeneralApiMaxTokens);
		}
	}

	private static void ResolveActionPostprocessApiSettings(string apiUrl, string apiKey, string modelName, float fallbackTemperature, out bool thinkingEnabled, out string effort, out float temperature)
	{
		thinkingEnabled = true;
		effort = DuelSettings.ReasoningEffortMax;
		temperature = DuelSettings.ClampApiTemperature(fallbackTemperature);
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return;
			}
			string dedicatedUrl = DuelSettings.GetEffectiveApiUrl((settings.ActionPostprocessApiUrl ?? "").Trim());
			string dedicatedKey = (settings.ActionPostprocessApiKey ?? "").Trim();
			string dedicatedModel = settings.GetEffectiveActionPostprocessModelName();
			bool usesDedicated = !string.IsNullOrWhiteSpace(dedicatedUrl)
				&& !string.IsNullOrWhiteSpace(dedicatedKey)
				&& !string.IsNullOrWhiteSpace(dedicatedModel)
				&& string.Equals(dedicatedUrl, apiUrl ?? "", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(dedicatedKey, apiKey ?? "", StringComparison.Ordinal)
				&& string.Equals(dedicatedModel, modelName ?? "", StringComparison.OrdinalIgnoreCase);
			if (usesDedicated)
			{
				thinkingEnabled = settings.ActionPostprocessApiThinkingEnabled;
				effort = settings.GetActionPostprocessApiReasoningEffort();
				temperature = settings.GetActionPostprocessApiTemperature();
			}
			else
			{
				thinkingEnabled = settings.MainApiThinkingEnabled;
				effort = settings.GetMainApiReasoningEffort();
				temperature = settings.GetMainApiTemperature();
			}
		}
		catch
		{
		}
	}

	private static bool TryGetAuxiliarySimpleDialogueConfig(out string apiUrl, out string apiKey, out string modelName)
	{
		apiUrl = "";
		apiKey = "";
		modelName = "";
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return false;
			}
			apiUrl = DuelSettings.GetEffectiveApiUrl(settings.AuxiliaryApiUrl ?? "");
			apiKey = (settings.AuxiliaryApiKey ?? "").Trim();
			modelName = settings.GetEffectiveAuxiliaryModelName();
			bool flag = !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
			if (!flag)
			{
				LogAuxiliaryRouterTokenTrace("auxiliary_simple_dialogue_config_invalid", null, "[AUXILIARY SIMPLE DIALOGUE CONFIG]\nurl=" + (string.IsNullOrWhiteSpace(apiUrl) ? "(empty)" : apiUrl) + "\nmodel=" + (string.IsNullOrWhiteSpace(modelName) ? "(empty)" : modelName) + "\napiKey=" + (string.IsNullOrWhiteSpace(apiKey) ? "(empty)" : "(present)") + "\nreason=missing_required_field", 0);
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	public static bool CanUseAuxiliarySimpleDialogue()
	{
		return TryGetAuxiliarySimpleDialogueConfig(out var _, out var _, out var _);
	}

	private static string RequirePreprocessPromptValue(string value, string configPath)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("PreprocessPrompts.json 缺少必填项: " + (configPath ?? "unknown"));
		}
		return text;
	}

	private static void EnsurePreprocessPromptsAvailable()
	{
		string loadError = _preprocessPromptsLoadError;
		if (!string.IsNullOrWhiteSpace(loadError))
		{
			throw new InvalidOperationException("PreprocessPrompts.json 加载失败: " + loadError + "。实际路径: " + ResolveModuleDataFilePath("PreprocessPrompts.json"));
		}
	}

	private static PreprocessPromptsConfigModel LoadPreprocessPromptsConfig(string filePath, out bool usedEmbeddedDefaults, out int sourceVersion, out int defaultVersion)
	{
		string sourceJson = File.ReadAllText(filePath, StrictUtf8Encoding);
		JObject sourceObject = JObject.Parse(sourceJson);
		JObject defaultObject = (JObject)EmbeddedPreprocessPromptsDefaults.Value.DeepClone();
		defaultVersion = defaultObject.Value<int?>("Version").GetValueOrDefault();
		if (defaultVersion <= 0)
		{
			throw new InvalidDataException("程序集内置 PreprocessPrompts.json 的 Version 无效");
		}
		sourceVersion = sourceObject.Value<int?>("Version").GetValueOrDefault();
		usedEmbeddedDefaults = sourceVersion < defaultVersion;
		if (usedEmbeddedDefaults)
		{
			sourceObject = defaultObject;
		}
		return sourceObject.ToObject<PreprocessPromptsConfigModel>() ?? new PreprocessPromptsConfigModel();
	}

	private static JObject LoadEmbeddedDefaultPreprocessPrompts()
	{
		using Stream stream = typeof(AIConfigHandler).Assembly.GetManifestResourceStream(EmbeddedPreprocessPromptsResourceName);
		if (stream == null)
		{
			throw new MissingManifestResourceException("找不到程序集内置前处理提示词资源: " + EmbeddedPreprocessPromptsResourceName);
		}
		using StreamReader reader = new StreamReader(stream, StrictUtf8Encoding, detectEncodingFromByteOrderMarks: true);
		return JObject.Parse(reader.ReadToEnd());
	}

	private static RpItemIntroductionPromptsConfigModel LoadRpItemIntroductionPromptsConfig(string filePath, out bool usedEmbeddedDefaults, out string fallbackReason)
	{
		usedEmbeddedDefaults = false;
		fallbackReason = "";
		try
		{
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("找不到 RpItemIntroductionPrompts.json", filePath);
			}
			RpItemIntroductionPromptsConfigModel config = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(ReadStrictUtf8NoBomFile(filePath, "RpItemIntroductionPrompts.json"));
			if (config == null)
			{
				throw new InvalidDataException("RpItemIntroductionPrompts.json 内容为空或不是对象");
			}
			ValidateRpItemIntroductionPromptsConfig(config, "RpItemIntroductionPrompts.json");
			return config;
		}
		catch (Exception diskConfigEx)
		{
			usedEmbeddedDefaults = true;
			fallbackReason = diskConfigEx.Message;
			RpItemIntroductionPromptsConfigModel embeddedConfig = EmbeddedRpItemIntroductionPromptsDefaults.Value;
			ValidateRpItemIntroductionPromptsConfig(embeddedConfig, "程序集内置 RpItemIntroductionPrompts.json");
			return embeddedConfig;
		}
	}

	private static RpItemIntroductionPromptsConfigModel LoadEmbeddedDefaultRpItemIntroductionPrompts()
	{
		using Stream stream = typeof(AIConfigHandler).Assembly.GetManifestResourceStream(EmbeddedRpItemIntroductionPromptsResourceName);
		if (stream == null)
		{
			throw new MissingManifestResourceException("找不到程序集内置 RP物品介绍提示词资源: " + EmbeddedRpItemIntroductionPromptsResourceName);
		}
		using StreamReader reader = new StreamReader(stream, StrictUtf8Encoding, detectEncodingFromByteOrderMarks: false);
		RpItemIntroductionPromptsConfigModel config = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(reader.ReadToEnd());
		if (config == null)
		{
			throw new InvalidDataException("程序集内置 RpItemIntroductionPrompts.json 内容为空或不是对象");
		}
		return config;
	}

	private static string ReadStrictUtf8NoBomFile(string filePath, string displayName)
	{
		byte[] bytes = File.ReadAllBytes(filePath);
		if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
		{
			throw new InvalidDataException((displayName ?? "配置文件") + " 必须使用 UTF-8 无 BOM 编码");
		}
		return StrictUtf8Encoding.GetString(bytes);
	}

	private static void ValidateRpItemIntroductionPromptsConfig(RpItemIntroductionPromptsConfigModel config, string configName)
	{
		if (config == null)
		{
			throw new InvalidDataException((configName ?? "RpItemIntroductionPrompts.json") + " 内容为空");
		}
		if (config.Version != 1)
		{
			throw new InvalidDataException((configName ?? "RpItemIntroductionPrompts.json") + " 的 Version 必须为 1，当前为 " + config.Version);
		}
		RequireRpItemIntroductionPromptValue(config.SystemPrompt, "SystemPrompt");
		string template = RequireRpItemIntroductionPromptValue(config.UserPromptTemplate, "UserPromptTemplate");
		bool hasItemName = false;
		bool hasDialogue = false;
		foreach (Match match in RpItemIntroductionTemplateVariableRegex.Matches(template))
		{
			string variable = match.Groups[1].Value;
			if (!RpItemIntroductionTemplateVariables.Contains(variable))
			{
				throw new InvalidDataException((configName ?? "RpItemIntroductionPrompts.json") + " 的 UserPromptTemplate 包含不支持的占位符: {" + variable + "}。只允许 {item_name}、{giver_name}、{dialogue}");
			}
			hasItemName |= string.Equals(variable, "item_name", StringComparison.Ordinal);
			hasDialogue |= string.Equals(variable, "dialogue", StringComparison.Ordinal);
		}
		if (!hasItemName || !hasDialogue)
		{
			throw new InvalidDataException((configName ?? "RpItemIntroductionPrompts.json") + " 的 UserPromptTemplate 必须包含 {item_name} 和 {dialogue}");
		}
	}

	private static string RequireRpItemIntroductionPromptValue(string value, string fieldName)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidDataException("RpItemIntroductionPrompts.json 缺少必填项: " + (fieldName ?? "unknown"));
		}
		return text;
	}

	private static string RenderRpItemIntroductionPromptTemplate(string template, IDictionary<string, string> values)
	{
		string text = RequireRpItemIntroductionPromptValue(template, "UserPromptTemplate");
		IDictionary<string, string> replacements = values ?? new Dictionary<string, string>(StringComparer.Ordinal);
		return RpItemIntroductionTemplateVariableRegex.Replace(text, delegate(Match match)
		{
			string key = match.Groups[1].Value;
			if (!RpItemIntroductionTemplateVariables.Contains(key) || !replacements.TryGetValue(key, out var value))
			{
				throw new InvalidOperationException("RpItemIntroductionPrompts.json 的 UserPromptTemplate 包含未提供的占位符: {" + key + "}");
			}
			return value ?? "";
		}).Trim();
	}

	private static string RenderPreprocessPromptTemplate(string template, string configPath, IDictionary<string, string> values)
	{
		string text = RequirePreprocessPromptValue(template, configPath);
		IDictionary<string, string> replacements = values ?? new Dictionary<string, string>(StringComparer.Ordinal);
		return PreprocessTemplateVariableRegex.Replace(text, delegate(Match match)
		{
			string key = match.Groups[1].Value;
			if (!replacements.TryGetValue(key, out var value))
			{
				throw new InvalidOperationException("PreprocessPrompts.json 模板存在未知或未提供的占位符: " + configPath + ".{" + key + "}");
			}
			return value ?? "";
		}).Trim();
	}

	private static void ValidateLoadedPreprocessPrompts()
	{
		RequirePreprocessPromptValue(StrictPreprocessJsonSystemPrompt, "StrictJson.SystemPrompt");
		JObject schema = _preprocessPrompts?.StrictJson?.MentionedEntitiesSchema;
		if (!(schema?["entities"] is JArray))
		{
			throw new InvalidOperationException("PreprocessPrompts.json schema 缺少数组: StrictJson.MentionedEntitiesSchema.entities");
		}
		RequirePreprocessPromptValue(_preprocessPrompts?.TopicRouting?.EmptyValue, "TopicRouting.EmptyValue");
		ValidatePreprocessTemplateVariables(_preprocessPrompts?.TopicRouting?.UserPromptTemplate, "TopicRouting.UserPromptTemplate", "topic_list", "routing_guidance", "history", "latest_npc", "latest_player", "top_n", "mentioned_entities_schema");
		RequirePreprocessPromptValue(_preprocessPrompts?.MemorySelection?.ParallelModeInstruction, "MemorySelection.ParallelModeInstruction");
		RequirePreprocessPromptValue(_preprocessPrompts?.MemorySelection?.UnifiedModeInstruction, "MemorySelection.UnifiedModeInstruction");
		RequirePreprocessPromptValue(_preprocessPrompts?.MemorySelection?.EmptyValue, "MemorySelection.EmptyValue");
		ValidatePreprocessTemplateVariables(_preprocessPrompts?.MemorySelection?.UserPromptTemplate, "MemorySelection.UserPromptTemplate", "mode_instruction", "final_count", "latest_player_input", "latest_npc_input", "current_scene", "memory_candidates");
		ValidatePreprocessTemplateVariables(_preprocessPrompts?.MemorySelection?.CandidateLineTemplate, "MemorySelection.CandidateLineTemplate", "memory_id", "game_date", "age_suffix", "hour_range", "rich_title");
		ValidatePreprocessTemplateVariables(_preprocessPrompts?.MemorySelection?.FallbackGameDateTemplate, "MemorySelection.FallbackGameDateTemplate", "game_day");
		RequirePreprocessPromptValue(_preprocessPrompts?.ConnectionTest?.ExpectedRuleCode, "ConnectionTest.ExpectedRuleCode");
		ValidatePreprocessTemplateVariables(_preprocessPrompts?.ConnectionTest?.UserPromptTemplate, "ConnectionTest.UserPromptTemplate", "expected_rule_code", "mentioned_entities_schema");
	}

	private static void ValidatePreprocessTemplateVariables(string template, string configPath, params string[] requiredVariables)
	{
		string text = RequirePreprocessPromptValue(template, configPath);
		HashSet<string> variables = new HashSet<string>(PreprocessTemplateVariableRegex.Matches(text).Cast<Match>().Select((Match x) => x.Groups[1].Value), StringComparer.Ordinal);
		foreach (string requiredVariable in requiredVariables ?? new string[0])
		{
			if (!variables.Contains(requiredVariable))
			{
				throw new InvalidOperationException("PreprocessPrompts.json 模板缺少占位符: " + configPath + ".{" + requiredVariable + "}");
			}
		}
	}

	internal static string BuildAuxiliaryConnectionTestPromptForExternal()
	{
		return RenderPreprocessPromptTemplate(_preprocessPrompts?.ConnectionTest?.UserPromptTemplate, "ConnectionTest.UserPromptTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["expected_rule_code"] = PreprocessConnectionTestExpectedRuleCode,
			["mentioned_entities_schema"] = StrictPreprocessMentionedEntitiesSchema
		});
	}

	internal static string BuildMemoryPreprocessUserPromptForExternal(int mode, int finalCount, string latestPlayerInput, string latestNpcInput, string currentScene, string memoryCandidates)
	{
		MemorySelectionPreprocessPromptConfig config = _preprocessPrompts?.MemorySelection;
		string emptyValue = RequirePreprocessPromptValue(config?.EmptyValue, "MemorySelection.EmptyValue");
		string modeInstruction = mode == 2
			? RequirePreprocessPromptValue(config?.ParallelModeInstruction, "MemorySelection.ParallelModeInstruction")
			: RequirePreprocessPromptValue(config?.UnifiedModeInstruction, "MemorySelection.UnifiedModeInstruction");
		return RenderPreprocessPromptTemplate(config?.UserPromptTemplate, "MemorySelection.UserPromptTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["mode_instruction"] = modeInstruction,
			["final_count"] = Math.Max(1, finalCount).ToString(),
			["latest_player_input"] = string.IsNullOrWhiteSpace(latestPlayerInput) ? emptyValue : latestPlayerInput.Trim(),
			["latest_npc_input"] = string.IsNullOrWhiteSpace(latestNpcInput) ? emptyValue : latestNpcInput.Trim(),
			["current_scene"] = string.IsNullOrWhiteSpace(currentScene) ? emptyValue : currentScene.Trim(),
			["memory_candidates"] = string.IsNullOrWhiteSpace(memoryCandidates) ? emptyValue : memoryCandidates.Trim()
		});
	}

	internal static string BuildMemoryPreprocessCandidateLineForExternal(int memoryId, string gameDate, string ageSuffix, string hourRange, string richTitle)
	{
		return RenderPreprocessPromptTemplate(_preprocessPrompts?.MemorySelection?.CandidateLineTemplate, "MemorySelection.CandidateLineTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["memory_id"] = memoryId.ToString(),
			["game_date"] = (gameDate ?? "").Trim(),
			["age_suffix"] = ageSuffix ?? "",
			["hour_range"] = (hourRange ?? "").Trim(),
			["rich_title"] = (richTitle ?? "").Trim()
		});
	}

	internal static string BuildMemoryPreprocessFallbackGameDateForExternal(int gameDay)
	{
		return RenderPreprocessPromptTemplate(_preprocessPrompts?.MemorySelection?.FallbackGameDateTemplate, "MemorySelection.FallbackGameDateTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["game_day"] = gameDay.ToString()
		});
	}


	private static object[] BuildAuxiliaryRouterMessages(string prompt)
	{
		return new object[2]
		{
			new
			{
				role = "system",
				content = StrictPreprocessJsonSystemPrompt
			},
			new
			{
				role = "user",
				content = NormalizeAuxiliaryRoutingRequestText(prompt ?? "")
			}
		};
	}

	private static object[] BuildAuxiliaryChatMessages(string systemPrompt, string userPrompt)
	{
		return new object[2]
		{
			new
			{
				role = "system",
				content = NormalizeAuxiliaryPlayerReferences(systemPrompt ?? "")
			},
			new
			{
				role = "user",
				content = NormalizeAuxiliaryPlayerReferences(userPrompt ?? "")
			}
		};
	}

	private static object[] NormalizeAuxiliaryChatMessages(IEnumerable<object> messages)
	{
		try
		{
			JArray jArray = JArray.FromObject(messages ?? Array.Empty<object>());
			List<object> list = new List<object>();
			foreach (JToken item in jArray)
			{
				string role = (item?["role"]?.ToString() ?? "").Trim();
				string content = NormalizeAuxiliaryPlayerReferences(item?["content"]?.ToString() ?? "");
				if (!string.IsNullOrWhiteSpace(role) || !string.IsNullOrWhiteSpace(content))
				{
					list.Add(new
					{
						role,
						content
					});
				}
			}
			return list.ToArray();
		}
		catch
		{
			return (messages ?? Array.Empty<object>()).ToArray();
		}
	}

	private static object[] CopyAuxiliaryChatMessagesPreservingNames(IEnumerable<object> messages)
	{
		try
		{
			JArray jArray = JArray.FromObject(messages ?? Array.Empty<object>());
			List<object> list = new List<object>();
			foreach (JToken item in jArray)
			{
				string role = (item?["role"]?.ToString() ?? "").Trim();
				string content = item?["content"]?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(role) || !string.IsNullOrWhiteSpace(content))
				{
					list.Add(new
					{
						role,
						content
					});
				}
			}
			return list.ToArray();
		}
		catch
		{
			return (messages ?? Array.Empty<object>()).ToArray();
		}
	}

	private static string NormalizeAuxiliaryPlayerReferences(string text)
	{
		try
		{
			string text2 = text ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			text2 = ShoutNetwork.ProtectPlayerPersonaRawNameReferencesForExternal(text2, out var rawPlayerPersonaNames);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			try
			{
				string text3 = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text3) && !string.Equals(text3, "玩家", StringComparison.Ordinal))
				{
					hashSet.Add(text3);
				}
			}
			catch
			{
			}
			try
			{
				string text4 = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4) && !string.Equals(text4, "玩家", StringComparison.Ordinal))
				{
					hashSet.Add(text4);
				}
			}
			catch
			{
			}
			foreach (string item in hashSet.OrderByDescending((string x) => x.Length))
			{
				text2 = text2.Replace(item, "玩家");
			}
			text2 = text2.Replace("玩家（玩家）", "玩家").Replace("玩家(玩家)", "玩家");
			return ShoutNetwork.RestorePlayerPersonaRawNameReferencesForExternal(text2, rawPlayerPersonaNames);
		}
		catch
		{
			return KnowledgeLibraryBehavior.StripPlayerPersonaRawNameMarkersForExternal(text ?? "");
		}
	}

	private static string NormalizeAuxiliaryRoutingRequestText(string text)
	{
		try
		{
			string text2 = NormalizeAuxiliaryPlayerReferences(text ?? "");
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			text2 = text2.Replace("（无）", "(none)")
				.Replace("玩家对你说：", "Player says to you: ")
				.Replace("玩家对你说:", "Player says to you: ")
				.Replace("玩家:", "Player:")
				.Replace("玩家：", "Player:")
				.Replace("你:", "Player:")
				.Replace("你：", "Player:")
				.Replace("上一句NPC发言：", "Previous NPC line: ")
				.Replace("[系统事实]", "[System fact]")
				.Replace("某NPC", "NPC");
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	public static string NormalizeActionPostprocessNameReferences(string text, params string[] npcNames)
	{
		try
		{
			string text2 = NormalizeAuxiliaryPlayerReferences(text ?? "");
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string text3 in npcNames ?? Array.Empty<string>())
			{
				string text4 = (text3 ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4) && !string.Equals(text4, "NPC", StringComparison.OrdinalIgnoreCase))
				{
					hashSet.Add(text4);
				}
			}
			foreach (string item in hashSet.OrderByDescending((string x) => x.Length))
			{
				text2 = text2.Replace(item, "NPC");
			}
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	private static string BuildAuxiliaryRouterExceptionText(Exception ex)
	{
		try
		{
			if (ex == null)
			{
				return "unknown_exception";
			}
			StringBuilder stringBuilder = new StringBuilder();
			Exception ex2 = ex;
			int num = 0;
			while (ex2 != null && num < 4)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(" | ");
				}
				stringBuilder.Append(ex2.GetType().Name).Append(": ").Append(ex2.Message);
				ex2 = ex2.InnerException;
				num++;
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return ex?.Message ?? "unknown_exception";
		}
	}

	private static void LogAuxiliaryRouterTokenTrace(string mode, IEnumerable<object> messages, string outputContent, int outputTokens = -1, string requestBody = null)
	{
		try
		{
			int num = Logger.EstimateTokensFromMessages(messages);
			int num2 = ((outputTokens >= 0) ? outputTokens : Logger.EstimateTokens(outputContent));
			Logger.RecordTokenStats(num, num2, messages, outputContent, mode, requestBody);
		}
		catch
		{
		}
	}

	public static bool TryCallAuxiliaryActionPostprocess(string systemPrompt, string userPrompt, int maxTokens, float temperature, out string content, out string error)
	{
		while (true)
		{
			if (TryCallAuxiliaryActionPostprocessOnce(systemPrompt, userPrompt, maxTokens, temperature, out content, out error))
			{
				return true;
			}
			if (!LlmRetryPrompt.PromptRetryBlocking("动作后处理", error))
			{
				return false;
			}
			Logger.Log("AIConfig", "[ActionPostprocess] user requested retry after error: " + error);
		}
	}

	private static bool TryCallAuxiliaryActionPostprocessOnce(string systemPrompt, string userPrompt, int maxTokens, float temperature, out string content, out string error)
	{
		content = "";
		error = "";
		string rawResponse = "";
		if (!ActionPostprocessEnabled)
		{
			error = "postprocess_disabled";
			return false;
		}
		if (!TryGetActionPostprocessConfig(out var apiUrl, out var apiKey, out var modelName))
		{
			error = "action_postprocess_config_invalid";
			return false;
		}
		object[] array = BuildAuxiliaryChatMessages(systemPrompt, userPrompt);
		string requestBodyForTokenStats = "";
		Stopwatch freezeWatchSw = Stopwatch.StartNew();
		FreezeWatchdog.Mark("AuxActionPostprocess.start", "model=" + modelName + " messages=" + array.Length + " timeoutMs=" + ActionPostprocessRequestTimeoutMilliseconds, immediate: true);
		try
		{
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(ActionPostprocessRequestTimeoutMilliseconds);
			int actualMaxTokens = ResolveActionPostprocessApiMaxTokens(apiUrl, apiKey, modelName, maxTokens);
			JObject payload = new JObject
			{
				["model"] = modelName,
				["messages"] = JArray.FromObject(array),
				["stream"] = false,
				["max_tokens"] = Math.Max(16, actualMaxTokens),
				["temperature"] = DuelSettings.ClampApiTemperature(temperature)
			};
			ResolveActionPostprocessApiSettings(apiUrl, apiKey, modelName, temperature, out var thinkingEnabled, out var effort, out var effectiveTemperature);
			payload["temperature"] = effectiveTemperature;
			DuelSettings.ApplyThinkingControls(payload, apiUrl, modelName, thinkingEnabled, effort, out var controlMode);
			string jsonBody = LlmApiCompat.PrepareChatRequestJson(apiUrl, payload);
			requestBodyForTokenStats = jsonBody;
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
			LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage, apiUrl, apiKey);
			httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			FreezeWatchdog.Mark("AuxActionPostprocess.send_begin", "model=" + modelName + " maxTokens=" + Math.Max(16, actualMaxTokens), immediate: true);
			HttpResponseMessage result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage, timeoutCts.Token).GetAwaiter().GetResult();
			FreezeWatchdog.Mark("AuxActionPostprocess.response", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			FreezeWatchdog.Mark("AuxActionPostprocess.content_read_begin", "status=" + (int)result.StatusCode + " thread=" + Thread.CurrentThread.ManagedThreadId);
			string text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			rawResponse = text ?? "";
			FreezeWatchdog.Mark("AuxActionPostprocess.content_read_end", "chars=" + ((text ?? "").Length) + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2) + " thread=" + Thread.CurrentThread.ManagedThreadId);
			if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.BadRequest && controlMode != "plain" && LooksLikeAuxiliaryThinkingControlError(text))
			{
				Logger.Log("AIConfig", "[ActionPostprocess] thinking payload rejected; retrying without thinking controls.");
				JObject retryPayload = JObject.Parse(jsonBody);
				DuelSettings.RemoveThinkingControls(retryPayload);
				string retryBody = retryPayload.ToString(Formatting.None);
				requestBodyForTokenStats = retryBody;
				using HttpRequestMessage httpRequestMessage2 = new HttpRequestMessage(HttpMethod.Post, apiUrl);
				LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage2, apiUrl, apiKey);
				httpRequestMessage2.Content = new StringContent(retryBody, Encoding.UTF8, "application/json");
				FreezeWatchdog.Mark("AuxActionPostprocess.retry_send_begin", "model=" + modelName, immediate: true);
				result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage2, timeoutCts.Token).GetAwaiter().GetResult();
				FreezeWatchdog.Mark("AuxActionPostprocess.retry_response", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				FreezeWatchdog.Mark("AuxActionPostprocess.retry_content_read_begin", "status=" + (int)result.StatusCode + " thread=" + Thread.CurrentThread.ManagedThreadId);
				text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				rawResponse = text ?? "";
				FreezeWatchdog.Mark("AuxActionPostprocess.retry_content_read_end", "chars=" + ((text ?? "").Length) + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2) + " thread=" + Thread.CurrentThread.ManagedThreadId);
				controlMode += "_retry_plain";
			}
			if (!result.IsSuccessStatusCode)
			{
				error = LlmRetryPrompt.BuildFailureDetail("http_" + (int)result.StatusCode, "", rawResponse);
				FreezeWatchdog.Mark("AuxActionPostprocess.http_error", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				LogAuxiliaryRouterTokenTrace("action_postprocess_http_error", array, "[ACTION POSTPROCESS HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nstatus=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\nresponse_body=\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			JObject jObject = JObject.Parse(text);
			content = LlmApiCompat.ExtractAssistantText(jObject).Trim();
			if (string.IsNullOrWhiteSpace(content))
			{
				string emptyContentReason = "empty_content";
				if (LlmApiCompat.IsReasoningOnlyTokenLimitResponse(jObject, out int completionTokens, out int reasoningTokens))
				{
					emptyContentReason = "empty_content_reasoning_token_limit：模型把输出额度全部用于思考，未生成最终content。请降低后处理思维链强度、关闭后处理思维链，或提高最大输出Tokens。completion_tokens=" + completionTokens + "，reasoning_tokens=" + reasoningTokens;
				}
				error = LlmRetryPrompt.BuildFailureDetail(emptyContentReason, "", rawResponse);
				FreezeWatchdog.Mark("AuxActionPostprocess.empty_content", "elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				LogAuxiliaryRouterTokenTrace("action_postprocess_empty_content", array, "[ACTION POSTPROCESS HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nstatus=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\nresponse_body=\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			if (content.IndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase) < 0 || content.IndexOf(']') < 0)
			{
				error = LlmRetryPrompt.BuildFailureDetail("（API响应格式错误）动作后处理未返回任何可解析的 [ACTION:...] 标签。", content, rawResponse);
				FreezeWatchdog.Mark("AuxActionPostprocess.format_error", "contentLen=" + content.Length + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				LogAuxiliaryRouterTokenTrace("action_postprocess_format_error", array, "[ACTION POSTPROCESS PARSE]\nmodel=" + modelName + "\nreason=no_action_tag\nai_response=\n" + content + "\nraw_response=\n" + rawResponse, 0, requestBodyForTokenStats);
				return false;
			}
			FreezeWatchdog.Mark("AuxActionPostprocess.complete", "contentLen=" + content.Length + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			LogAuxiliaryRouterTokenTrace("action_postprocess_http", array, "[ACTION POSTPROCESS HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nai_response=\n" + content + "\nraw_response=\n" + (text ?? ""), Logger.EstimateTokens(content), requestBodyForTokenStats);
			return true;
		}
		catch (OperationCanceledException ex)
		{
			error = LlmRetryPrompt.BuildFailureDetail("timeout_" + ActionPostprocessRequestTimeoutMilliseconds + "ms", content, rawResponse);
			FreezeWatchdog.Mark("AuxActionPostprocess.timeout", "elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			LogAuxiliaryRouterTokenTrace("action_postprocess_timeout", array, "[ACTION POSTPROCESS TIMEOUT]\ntimeoutMs=" + ActionPostprocessRequestTimeoutMilliseconds + "\nerror=" + BuildAuxiliaryRouterExceptionText(ex) + "\nstack=\n" + (ex?.StackTrace ?? ""), 0, requestBodyForTokenStats);
			return false;
		}
		catch (Exception ex)
		{
			error = LlmRetryPrompt.BuildFailureDetail(BuildAuxiliaryRouterExceptionText(ex), content, rawResponse);
			FreezeWatchdog.Mark("AuxActionPostprocess.exception", ex.GetType().Name + ": " + ex.Message + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			LogAuxiliaryRouterTokenTrace("action_postprocess_exception", array, "[ACTION POSTPROCESS EXCEPTION]\nerror=" + error + "\nstack=\n" + (ex?.StackTrace ?? ""), 0, requestBodyForTokenStats);
			return false;
		}
	}

	public static bool TryCallAuxiliarySimpleDialogue(IEnumerable<object> messages, int maxTokens, float temperature, out string content, out string error)
	{
		while (true)
		{
			if (TryCallAuxiliarySimpleDialogueOnce(messages, maxTokens, temperature, out content, out error))
			{
				return true;
			}
			if (!LlmRetryPrompt.PromptRetryBlocking("辅助对话", error))
			{
				return false;
			}
			Logger.Log("AIConfig", "[AuxiliarySimpleDialogue] user requested retry after error: " + error);
		}
	}

	/// <summary>
	/// Executes one auxiliary simple-dialogue request without the interactive retry prompt.
	/// Callers that run background, non-critical work (such as RP item introductions) must use this overload.
	/// </summary>
	public static bool TryCallAuxiliarySimpleDialogueOnceForExternal(IEnumerable<object> messages, int maxTokens, float temperature, out string content, out string error)
	{
		return TryCallAuxiliarySimpleDialogueOnce(messages, maxTokens, temperature, out content, out error);
	}

	private static bool TryCallAuxiliarySimpleDialogueOnce(IEnumerable<object> messages, int maxTokens, float temperature, out string content, out string error)
	{
		content = "";
		error = "";
		string rawResponse = "";
		if (!TryGetAuxiliarySimpleDialogueConfig(out var apiUrl, out var apiKey, out var modelName))
		{
			error = "auxiliary_simple_dialogue_config_invalid";
			return false;
		}
		object[] array = CopyAuxiliaryChatMessagesPreservingNames(messages);
		Logger.RecordMessageDump("auxiliary_simple_dialogue_request", array, "auxiliary_simple_dialogue_request");
		string requestBodyForTokenStats = "";
		Stopwatch freezeWatchSw = Stopwatch.StartNew();
		FreezeWatchdog.Mark("AuxSimpleDialogue.start", "model=" + modelName + " messages=" + array.Length, immediate: true);
		try
		{
			string jsonBody = BuildAuxiliarySimpleDialogueRequestJson(apiUrl, modelName, array, Math.Max(16, maxTokens), temperature, out var controlMode);
			requestBodyForTokenStats = jsonBody;
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
			LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage, apiUrl, apiKey);
			httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			FreezeWatchdog.Mark("AuxSimpleDialogue.send_begin", "model=" + modelName + " maxTokens=" + Math.Max(16, maxTokens), immediate: true);
			HttpResponseMessage result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage).GetAwaiter().GetResult();
			FreezeWatchdog.Mark("AuxSimpleDialogue.response", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			string text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			rawResponse = text ?? "";
			if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.BadRequest && controlMode != "plain" && LooksLikeAuxiliaryThinkingControlError(text))
			{
				Logger.Log("AIConfig", "[AuxiliarySimpleDialogue] thinking payload rejected; retrying without thinking controls.");
				JObject jObject2 = JObject.Parse(jsonBody);
				DuelSettings.RemoveThinkingControls(jObject2);
				string content2 = jObject2.ToString(Formatting.None);
				requestBodyForTokenStats = content2;
				using HttpRequestMessage httpRequestMessage2 = new HttpRequestMessage(HttpMethod.Post, apiUrl);
				LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage2, apiUrl, apiKey);
				httpRequestMessage2.Content = new StringContent(content2, Encoding.UTF8, "application/json");
				FreezeWatchdog.Mark("AuxSimpleDialogue.retry_send_begin", "model=" + modelName, immediate: true);
				result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage2).GetAwaiter().GetResult();
				FreezeWatchdog.Mark("AuxSimpleDialogue.retry_response", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				rawResponse = text ?? "";
				controlMode += "_retry_plain";
			}
			if (!result.IsSuccessStatusCode)
			{
				error = LlmRetryPrompt.BuildFailureDetail("http_" + (int)result.StatusCode, "", rawResponse);
				FreezeWatchdog.Mark("AuxSimpleDialogue.http_error", "status=" + (int)result.StatusCode + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				LogAuxiliaryRouterTokenTrace("auxiliary_simple_dialogue_http_error", array, "[AUXILIARY SIMPLE DIALOGUE HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nstatus=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\nresponse_body=\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			JObject jObject = JObject.Parse(text);
			content = LlmApiCompat.ExtractAssistantText(jObject).Trim();
			if (string.IsNullOrWhiteSpace(content))
			{
				error = LlmRetryPrompt.BuildFailureDetail("empty_content", "", rawResponse);
				FreezeWatchdog.Mark("AuxSimpleDialogue.empty_content", "elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
				LogAuxiliaryRouterTokenTrace("auxiliary_simple_dialogue_empty_content", array, "[AUXILIARY SIMPLE DIALOGUE HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nstatus=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\nresponse_body=\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			FreezeWatchdog.Mark("AuxSimpleDialogue.complete", "contentLen=" + content.Length + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			LogAuxiliaryRouterTokenTrace("auxiliary_simple_dialogue_http", array, "[AUXILIARY SIMPLE DIALOGUE HTTP]\nurl=" + apiUrl + "\nmodel=" + modelName + "\ncontrol_mode=" + controlMode + "\nai_response=\n" + content + "\nraw_response=\n" + (text ?? ""), Logger.EstimateTokens(content), requestBodyForTokenStats);
			return true;
		}
		catch (Exception ex)
		{
			error = LlmRetryPrompt.BuildFailureDetail(BuildAuxiliaryRouterExceptionText(ex), content, rawResponse);
			FreezeWatchdog.Mark("AuxSimpleDialogue.exception", ex.GetType().Name + ": " + ex.Message + " elapsedMs=" + Math.Round(freezeWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			LogAuxiliaryRouterTokenTrace("auxiliary_simple_dialogue_exception", array, "[AUXILIARY SIMPLE DIALOGUE EXCEPTION]\nerror=" + error + "\nstack=\n" + (ex?.StackTrace ?? ""), 0, requestBodyForTokenStats);
			return false;
		}
	}

	public static List<string> GetEnabledGuardrailRuleIdsForExternal()
	{
		try
		{
			return GetAllEnabledRulePrompts().Select((GuardrailRulePromptConfig x) => (x?.Id ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	private static List<PreprocessExcludedPromptEntry> GetConfiguredPreprocessExcludedPromptEntries()
	{
		long version = Volatile.Read(ref _guardrailConfigVersion);
		if (Volatile.Read(ref _preprocessExcludedPromptCacheVersion) == version)
		{
			List<PreprocessExcludedPromptEntry> snapshot = _preprocessExcludedPromptCache;
			if (snapshot != null)
			{
				return snapshot;
			}
		}
		lock (_preprocessExcludedPromptCacheLock)
		{
			if (_preprocessExcludedPromptCacheVersion != version || _preprocessExcludedPromptCache == null)
			{
				Dictionary<string, GuardrailRulePromptConfig> registry = BuildRulePromptRegistry();
				IEnumerable<GuardrailRulePromptConfig> configuredRules = registry != null
					? registry.Values
					: Enumerable.Empty<GuardrailRulePromptConfig>();
				List<PreprocessExcludedPromptEntry> rebuilt = configuredRules
					.Where((GuardrailRulePromptConfig rule) => rule != null && rule.IsEnabled && !string.IsNullOrWhiteSpace(rule.Id))
					.Select((GuardrailRulePromptConfig rule) => new PreprocessExcludedPromptEntry
					{
						RuleId = rule.Id.Trim().ToLowerInvariant(),
						TopicNumber = rule.TopicNumber,
						Priority = rule.Priority,
						Instruction = (rule.PreprocessExcludedInstruction ?? "").Trim()
					})
					.OrderBy((PreprocessExcludedPromptEntry entry) => entry.TopicNumber <= 0 ? int.MaxValue : entry.TopicNumber)
					.ThenByDescending((PreprocessExcludedPromptEntry entry) => entry.Priority)
					.ThenBy((PreprocessExcludedPromptEntry entry) => entry.RuleId, StringComparer.OrdinalIgnoreCase)
					.ToList();
				_preprocessExcludedPromptCache = rebuilt;
				Volatile.Write(ref _preprocessExcludedPromptCacheVersion, version);
			}
			return _preprocessExcludedPromptCache;
		}
	}

	public static List<string> GetConfiguredEnabledGuardrailRuleIdsForExternal()
	{
		try
		{
			return GetConfiguredPreprocessExcludedPromptEntries()
				.Select((PreprocessExcludedPromptEntry entry) => entry?.RuleId ?? "")
				.Where((string ruleId) => !string.IsNullOrWhiteSpace(ruleId))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	public static string BuildPreprocessExcludedRuleBlockForExternal(IEnumerable<string> excludedRuleIds)
	{
		try
		{
			HashSet<string> excluded = new HashSet<string>(
				(excludedRuleIds ?? Enumerable.Empty<string>())
					.Select((string ruleId) => (ruleId ?? "").Trim())
					.Where((string ruleId) => !string.IsNullOrWhiteSpace(ruleId)),
				StringComparer.OrdinalIgnoreCase);
			if (excluded.Count == 0)
			{
				return "";
			}
			List<string> instructions = GetConfiguredPreprocessExcludedPromptEntries()
				.Where((PreprocessExcludedPromptEntry entry) => entry != null
					&& excluded.Contains(entry.RuleId)
					&& !string.IsNullOrWhiteSpace(entry.Instruction))
				.Select((PreprocessExcludedPromptEntry entry) => ApplyPlayerDisplayNameToGuardrailText(entry.Instruction).Trim())
				.Where((string instruction) => !string.IsNullOrWhiteSpace(instruction))
				.ToList();
			if (instructions.Count == 0)
			{
				return "";
			}
			string header = ApplyPlayerDisplayNameToGuardrailText(_guardrail?.PreprocessExcludedSectionHeader ?? "").Trim();
			string sectionInstruction = ApplyPlayerDisplayNameToGuardrailText(_guardrail?.PreprocessExcludedSectionInstruction ?? "").Trim();
			StringBuilder builder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(header))
			{
				builder.AppendLine(header);
			}
			if (!string.IsNullOrWhiteSpace(sectionInstruction))
			{
				builder.AppendLine(sectionInstruction);
			}
			for (int i = 0; i < instructions.Count; i++)
			{
				builder.Append(i + 1).Append(". ").AppendLine(instructions[i]);
			}
			return builder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static List<GuardrailAuxiliaryTopic> GetEligibleAuxiliaryGuardrailTopics(IEnumerable<string> availableRuleIds)
	{
		return GetEligibleAuxiliaryGuardrailTopics(availableRuleIds, applyRuntimeEligibility: true);
	}

	private static List<GuardrailAuxiliaryTopic> GetEligibleAuxiliaryGuardrailTopics(IEnumerable<string> availableRuleIds, bool applyRuntimeEligibility)
	{
		HashSet<string> hashSet = new HashSet<string>((availableRuleIds ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		List<GuardrailAuxiliaryTopic> list = new List<GuardrailAuxiliaryTopic>();
		Dictionary<string, GuardrailRulePromptConfig> dictionary = null;
		try
		{
			dictionary = BuildRulePromptRegistry();
		}
		catch
		{
			dictionary = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
		}
		foreach (GuardrailRulePromptConfig value in dictionary.Values)
		{
			string text = (value?.Id ?? "").Trim();
			string text2 = (value?.TopicLabel ?? "").Trim();
			int num = value?.TopicNumber ?? 0;
			string text3 = NormalizeRuleCode(value?.Code, text, text2);
			if (num > 0 && !string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3) && hashSet.Contains(text) && (!applyRuntimeEligibility || IsRuleCurrentlyEligibleForRag(text)))
			{
				list.Add(new GuardrailAuxiliaryTopic
				{
					Number = num,
					Label = text2,
					Code = text3,
					RuleId = text
				});
			}
		}
		list = list.OrderBy((GuardrailAuxiliaryTopic x) => x.Number).ToList();
		return list;
	}

	private static string BuildAuxiliaryGuardrailHistoryBlock(string runtimeGuardrailContext, string secondaryText, string latestPlayerText, out string latestNpcText)
	{
		latestNpcText = "";
		List<string> list = new List<string>();
		try
		{
			AppendAuxiliaryDialogueHistoryLines(list, GetAuxiliarySceneDialogueHistoryContext());
			bool flag = HasAuxiliaryHistoryDialogueRecord(list);
			AppendAuxiliaryDialogueHistoryLines(list, runtimeGuardrailContext, allowNewDialogueRecords: !flag);
			string text3 = NormalizeSemanticText(secondaryText);
			if (!string.IsNullOrWhiteSpace(text3) && !IsAuxiliarySceneShoutObserverLine(text3))
			{
				latestNpcText = text3;
			}
			if (string.IsNullOrWhiteSpace(latestNpcText))
			{
				latestNpcText = ExtractLatestAuxiliaryNpcUtterance(list, latestPlayerText);
			}
			TrimAuxiliaryLatestDialogueLines(list, latestNpcText, latestPlayerText);
		}
		catch
		{
		}
		if (list.Count <= 0)
		{
			return "(none)";
		}
		int num = Math.Max(3, Math.Min(6, list.Count));
		if (list.Count > num)
		{
			list = list.Skip(list.Count - num).ToList();
		}
		return StripAuxiliaryHistoryInnerThoughts(string.Join("\n", list));
	}

	private static string StripAuxiliaryHistoryInnerThoughts(string historyBlock)
	{
		string text = (historyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = StripAuxiliaryHistoryInnerThoughtsFromLine(array[i]);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine(text2);
			}
		}
		return Regex.Replace(stringBuilder.ToString().Trim(), "[ \\t]{2,}", " ");
	}

	private static string StripAuxiliaryHistoryInnerThoughtsFromLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (IsAuxiliaryAfefFactLine(text))
		{
			return text;
		}
		if (IsAuxiliaryPlayerHistoryLine(text))
		{
			return NormalizeAuxiliaryPlayerRoutingLine(text);
		}
		const string value = "\u0001AF_NONE_ASCII\u0001";
		const string value2 = "\u0001AF_NONE_CN\u0001";
		text = text.Replace("(none)", value).Replace("（无）", value2);
		text = RemoveAuxiliaryInnerThoughtSegments(text, '（', '）');
		text = RemoveAuxiliaryInnerThoughtSegments(text, '(', ')');
		text = Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
		text = text.Replace(value, "(none)").Replace(value2, "（无）");
		text = text.Trim();
		text = text.TrimStart('，', '。', '、', '；', '：', ',', ';', ':');
		return text.Trim();
	}

	private static string RemoveAuxiliaryInnerThoughtSegments(string text, char open, char close)
	{
		string value = text ?? "";
		if (string.IsNullOrEmpty(value) || value.IndexOf(open) < 0)
		{
			return value;
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (c != open)
			{
				stringBuilder.Append(c);
				continue;
			}
			int depth = 1;
			i++;
			for (; i < value.Length; i++)
			{
				char c2 = value[i];
				if (c2 == open)
				{
					depth++;
				}
				else if (c2 == close)
				{
					depth--;
					if (depth <= 0)
					{
						break;
					}
				}
			}
			if (i >= value.Length)
			{
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static string NormalizeAuxiliaryPlayerRoutingLine(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
		return text.Trim();
	}

	private static string ExtractLatestAuxiliaryNpcUtterance(List<string> lines, string latestPlayerText)
	{
		try
		{
			string text = NormalizeSemanticText(latestPlayerText);
			if (lines == null || lines.Count <= 0)
			{
				return "";
			}
			for (int i = lines.Count - 1; i >= 0; i--)
			{
				string text2 = NormalizeSemanticText(lines[i]);
				if (string.IsNullOrWhiteSpace(text2) || IsAuxiliaryPlayerHistoryLine(text2))
				{
					continue;
				}
				if (IsAuxiliarySceneShoutObserverLine(text2))
				{
					continue;
				}
				if (IsAuxiliaryAfefFactLine(text2))
				{
					continue;
				}
				string text3 = ExtractAuxiliaryHistoryUtterance(text2);
				if (!string.IsNullOrWhiteSpace(text) && string.Equals(text3, text, StringComparison.Ordinal))
				{
					continue;
				}
				return text3;
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsAuxiliarySceneShoutObserverLine(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		int startIndex = GetAuxiliaryHistorySpeakerSearchStart(text);
		if (startIndex > 0 && startIndex < text.Length)
		{
			text = text.Substring(startIndex).TrimStart();
		}
		return text.StartsWith("[场景喊话]", StringComparison.Ordinal);
	}

	private static void TrimAuxiliaryLatestDialogueLines(List<string> lines, string latestNpcText, string latestPlayerText)
	{
		try
		{
			if (lines == null || lines.Count <= 0)
			{
				return;
			}
			string text = NormalizeSemanticText(latestNpcText);
			string text2 = NormalizeSemanticText(latestPlayerText);
			int num = 0;
			for (int i = lines.Count - 1; i >= 0 && num < 2; i--)
			{
				string line = lines[i];
				bool flag = !string.IsNullOrWhiteSpace(text) && IsAuxiliaryHistoryUtteranceMatch(line, text);
				bool flag2 = !string.IsNullOrWhiteSpace(text2) && IsAuxiliaryHistoryUtteranceMatch(line, text2);
				if (flag || flag2)
				{
					lines.RemoveAt(i);
					num++;
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsAuxiliaryHistoryUtteranceMatch(string line, string utterance)
	{
		string text = NormalizeSemanticText(utterance);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		if (string.Equals(text2, text, StringComparison.Ordinal))
		{
			return true;
		}
		return string.Equals(ExtractAuxiliaryHistoryUtterance(text2), text, StringComparison.Ordinal);
	}

	private static string ExtractAuxiliaryHistoryUtterance(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string value = "上一句NPC发言：";
		if (text.StartsWith(value, StringComparison.Ordinal))
		{
			return NormalizeSemanticText(text.Substring(value.Length));
		}
		string value2 = "Previous NPC line:";
		if (text.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
		{
			return NormalizeSemanticText(text.Substring(value2.Length));
		}
		int num = FindAuxiliaryHistorySpeakerDelimiter(text);
		if (num >= 0 && num + 1 < text.Length)
		{
			return NormalizeSemanticText(text.Substring(num + 1));
		}
		return text;
	}

	private static bool IsAuxiliaryPlayerHistoryLine(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = GetAuxiliaryHistorySpeakerPrefix(text);
		return text2.Equals("玩家", StringComparison.OrdinalIgnoreCase) || text2.Equals("你", StringComparison.OrdinalIgnoreCase) || text2.Equals("Player", StringComparison.OrdinalIgnoreCase) || text2.Equals("You", StringComparison.OrdinalIgnoreCase) || text2.EndsWith(" says to you", StringComparison.OrdinalIgnoreCase) || (text2.Contains("对") && text2.EndsWith("说", StringComparison.Ordinal));
	}

	private static string GetAuxiliaryHistorySpeakerPrefix(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = FindAuxiliaryHistorySpeakerDelimiter(text);
		if (num >= 0)
		{
			int startIndex = GetAuxiliaryHistorySpeakerSearchStart(text);
			if (startIndex < num)
			{
				return text.Substring(startIndex, num - startIndex).Trim();
			}
			return text.Substring(0, num).Trim();
		}
		int num2 = GetAuxiliaryHistorySpeakerSearchStart(text);
		return num2 < text.Length ? text.Substring(num2).Trim() : text.Trim();
	}

	private static int FindAuxiliaryHistorySpeakerDelimiter(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return -1;
		}
		int startIndex = GetAuxiliaryHistorySpeakerSearchStart(text);
		int num = text.IndexOfAny(new char[2] { ':', '：' }, startIndex);
		if (num >= 0)
		{
			return num;
		}
		return text.IndexOfAny(new char[2] { ':', '：' });
	}

	private static int GetAuxiliaryHistorySpeakerSearchStart(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (!text.StartsWith("[", StringComparison.Ordinal) && !text.StartsWith("AF_SCENE_SESSION", StringComparison.Ordinal) && !text.StartsWith("_SCENE_SESSION", StringComparison.Ordinal) && !text.StartsWith("SCENE_SESSION", StringComparison.Ordinal))
		{
			return 0;
		}
		int num = text.IndexOf(']');
		if (num >= 0 && num + 1 < text.Length && num <= 64)
		{
			return num + 1;
		}
		return 0;
	}

	private static void AppendAuxiliaryDialogueHistoryLines(List<string> lines, string block, bool allowNewDialogueRecords = true)
	{
		if (lines == null)
		{
			return;
		}
		string text = NormalizeGuardrailContextText(block);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		List<string> list = SplitAuxiliaryDialogueHistoryRecords(text);
		for (int i = 0; i < list.Count; i++)
		{
			AppendAuxiliaryDialogueHistoryLine(lines, list[i], allowNewDialogueRecords);
		}
	}

	private static List<string> SplitAuxiliaryDialogueHistoryRecords(string block)
	{
		List<string> list = new List<string>();
		string text = (block ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = NormalizeSemanticText(array[i]);
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (stringBuilder.Length <= 0 || IsAuxiliaryDialogueHistoryRecordStart(text2))
			{
				if (stringBuilder.Length > 0)
				{
					list.Add(stringBuilder.ToString().Trim());
					stringBuilder.Clear();
				}
				stringBuilder.Append(text2);
			}
			else
			{
				stringBuilder.Append(' ').Append(text2);
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString().Trim());
		}
		return list;
	}

	private static bool IsAuxiliaryDialogueHistoryRecordStart(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (IsAuxiliaryAfefFactLine(text) || IsAuxiliarySceneShoutObserverLine(text))
		{
			return true;
		}
		if (text.StartsWith("上一句NPC发言：", StringComparison.Ordinal) || text.StartsWith("Previous NPC line:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("“", StringComparison.Ordinal) || text.StartsWith("\"", StringComparison.Ordinal) || text.StartsWith("'", StringComparison.Ordinal))
		{
			return false;
		}
		int num = FindAuxiliaryHistorySpeakerDelimiter(text);
		if (num <= 0 || num > 96)
		{
			return false;
		}
		string text2 = GetAuxiliaryHistorySpeakerPrefix(text);
		if (string.IsNullOrWhiteSpace(text2) || text2.Length > 64)
		{
			return false;
		}
		if (IsAuxiliaryPlayerHistoryLine(text))
		{
			return true;
		}
		if (text2.Equals("NPC", StringComparison.OrdinalIgnoreCase) || text2.Equals("Assistant", StringComparison.OrdinalIgnoreCase) || text2.Equals("系统", StringComparison.OrdinalIgnoreCase) || text2.Equals("System", StringComparison.OrdinalIgnoreCase) || text2.Equals("旁白", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return text2.IndexOfAny(new char[16] { '。', '！', '？', '；', '，', ',', '.', '!', '?', ';', '“', '”', '"', '\'', '（', '(' }) < 0;
	}

	private static void AppendAuxiliaryDialogueHistoryLine(List<string> lines, string line, bool allowNewDialogueRecords = true)
	{
		if (lines == null)
		{
			return;
		}
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text) || !IsAuxiliaryDialogueHistoryLine(text) || lines.Contains(text))
		{
			return;
		}
		int num = FindAuxiliaryDuplicateHistoryLineIndex(lines, text);
		if (num >= 0)
		{
			if (ShouldPreferAuxiliaryHistoryLine(text, lines[num]))
			{
				lines[num] = text;
			}
			return;
		}
		if (!allowNewDialogueRecords && IsAuxiliaryHistoryDialogueRecord(text))
		{
			return;
		}
		lines.Add(text);
	}

	private static bool HasAuxiliaryHistoryDialogueRecord(List<string> lines)
	{
		if (lines == null || lines.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < lines.Count; i++)
		{
			if (IsAuxiliaryHistoryDialogueRecord(lines[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsAuxiliaryHistoryDialogueRecord(string line)
	{
		string text = GetAuxiliaryHistorySpeakerKind(line);
		return text.Equals("player", StringComparison.Ordinal) || text.Equals("npc", StringComparison.Ordinal);
	}

	private static int FindAuxiliaryDuplicateHistoryLineIndex(List<string> lines, string line)
	{
		if (lines == null || lines.Count <= 0)
		{
			return -1;
		}
		string text = GetAuxiliaryHistorySpeakerKind(line);
		string text2 = NormalizeAuxiliaryHistoryUtteranceForDedupe(line);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return -1;
		}
		for (int i = 0; i < lines.Count; i++)
		{
			string text3 = NormalizeSemanticText(lines[i]);
			if (string.IsNullOrWhiteSpace(text3) || !string.Equals(GetAuxiliaryHistorySpeakerKind(text3), text, StringComparison.Ordinal))
			{
				continue;
			}
			string text4 = NormalizeAuxiliaryHistoryUtteranceForDedupe(text3);
			if (string.Equals(text4, text2, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static string GetAuxiliaryHistorySpeakerKind(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text) || IsAuxiliaryAfefFactLine(text))
		{
			return "";
		}
		if (IsAuxiliaryPlayerHistoryLine(text))
		{
			return "player";
		}
		if (text.StartsWith("上一句NPC发言：", StringComparison.Ordinal) || text.StartsWith("Previous NPC line:", StringComparison.OrdinalIgnoreCase))
		{
			return "npc";
		}
		return FindAuxiliaryHistorySpeakerDelimiter(text) > 0 ? "npc" : "";
	}

	private static string NormalizeAuxiliaryHistoryUtteranceForDedupe(string line)
	{
		string text = NormalizeSemanticText(ExtractAuxiliaryHistoryUtterance(line));
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant).Trim();
	}

	private static bool ShouldPreferAuxiliaryHistoryLine(string candidate, string existing)
	{
		return GetAuxiliaryHistorySpecificityScore(candidate) > GetAuxiliaryHistorySpecificityScore(existing);
	}

	private static int GetAuxiliaryHistorySpecificityScore(string line)
	{
		string text = NormalizeSemanticText(line);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		string text2 = GetAuxiliaryHistorySpeakerPrefix(text);
		int num = 0;
		if (!string.IsNullOrWhiteSpace(text2))
		{
			num += Math.Min(30, text2.Length);
		}
		if (IsAuxiliaryPlayerHistoryLine(text))
		{
			num += 20;
			if (text2.Contains("对") && text2.EndsWith("说", StringComparison.Ordinal))
			{
				num += 30;
			}
			if (!text2.Equals("Player", StringComparison.OrdinalIgnoreCase) && !text2.Equals("You", StringComparison.OrdinalIgnoreCase) && !text2.EndsWith(" says to you", StringComparison.OrdinalIgnoreCase))
			{
				num += 10;
			}
		}
		if (text.StartsWith("AF_SCENE_SESSION", StringComparison.Ordinal) || text.StartsWith("_SCENE_SESSION", StringComparison.Ordinal) || text.StartsWith("SCENE_SESSION", StringComparison.Ordinal))
		{
			num += 5;
		}
		return num;
	}

	private static string GetAuxiliarySceneDialogueHistoryContext()
	{
		try
		{
			int num = ResolveConversationTargetAgentIndex();
			bool nativeConversationInputOpen = ShoutBehavior.IsNativeConversationInputOpenForExternal();
			if (num < 0 && !nativeConversationInputOpen)
			{
				return "";
			}
			List<string> auxiliarySceneDialogueHistoryLinesForExternal = ShoutBehavior.GetAuxiliarySceneDialogueHistoryLinesForExternal(num, 6);
			if (auxiliarySceneDialogueHistoryLinesForExternal == null || auxiliarySceneDialogueHistoryLinesForExternal.Count <= 0)
			{
				return "";
			}
			List<string> list = new List<string>();
			for (int i = 0; i < auxiliarySceneDialogueHistoryLinesForExternal.Count; i++)
			{
				AppendAuxiliaryDialogueHistoryLine(list, auxiliarySceneDialogueHistoryLinesForExternal[i]);
			}
			return (list.Count <= 0) ? "" : string.Join("\n", list);
		}
		catch
		{
			return "";
		}
	}

	private static bool IsAuxiliaryDialogueHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (IsAuxiliaryAfefFactLine(text))
		{
			return true;
		}
		if (text.StartsWith("vanilla_issue:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return true;
	}

	private static bool IsAuxiliaryAfefFactLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal);
	}

	private static string AppendAuxiliaryAfefFactToRoutingContext(string context, string afefLine)
	{
		string text = NormalizeSemanticText(afefLine);
		if (!IsAuxiliaryAfefFactLine(text))
		{
			return NormalizeGuardrailContextText(context);
		}
		string text2 = NormalizeGuardrailContextText(context);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		string[] array = text2.Replace("\r\n", "\n").Replace('\r', '\n').Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			if (string.Equals((array[i] ?? "").Trim(), text, StringComparison.Ordinal))
			{
				return text2;
			}
		}
		return text2.TrimEnd() + "\n" + text;
	}

	private static bool ContainsAuxiliaryHistoryUtterance(List<string> lines, string utterance)
	{
		string text = NormalizeSemanticText(utterance);
		if (string.IsNullOrWhiteSpace(text) || lines == null || lines.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < lines.Count; i++)
		{
			string text2 = NormalizeSemanticText(lines[i]);
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.Equals(text, StringComparison.Ordinal))
			{
				return true;
			}
			if (text2.EndsWith(": " + text, StringComparison.Ordinal) || text2.EndsWith("：" + text, StringComparison.Ordinal))
			{
				return true;
			}
			if (text2.StartsWith("上一句NPC发言：", StringComparison.Ordinal) && string.Equals(text2.Substring("上一句NPC发言：".Length).Trim(), text, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAuxiliaryConversationHistory(List<string> lines)
	{
		try
		{
			if (lines == null || lines.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < lines.Count; i++)
			{
				string text = (lines[i] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
				{
					continue;
				}
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildAuxiliaryGuardrailRoutingPrompt(string userText, string secondaryText, string runtimeGuardrailContext, List<GuardrailAuxiliaryTopic> topics, int topN)
	{
		string text = NormalizeSemanticText(userText);
		bool userTextIsAfefFact = IsAuxiliaryAfefFactLine(text);
		string routingRuntimeContext = userTextIsAfefFact ? AppendAuxiliaryAfefFactToRoutingContext(runtimeGuardrailContext, text) : runtimeGuardrailContext;
		string routingLatestPlayerText = userTextIsAfefFact ? "" : userText;
		string latestNpcText;
		string historyBlock = StripAuxiliaryHistoryInnerThoughts(BuildAuxiliaryGuardrailHistoryBlock(routingRuntimeContext, secondaryText, routingLatestPlayerText, out latestNpcText));
		string text2 = StripAuxiliaryHistoryInnerThoughtsFromLine(NormalizeSemanticText(latestNpcText));
		string text5 = userTextIsAfefFact ? "" : NormalizeAuxiliaryPlayerRoutingLine(text);
		StringBuilder topicList = new StringBuilder();
		for (int i = 0; i < topics.Count; i++)
		{
			GuardrailAuxiliaryTopic guardrailAuxiliaryTopic = topics[i];
			if (guardrailAuxiliaryTopic != null)
			{
				topicList.AppendLine(guardrailAuxiliaryTopic.Code + ": " + guardrailAuxiliaryTopic.Label);
			}
		}
		TopicRoutingPreprocessPromptConfig config = _preprocessPrompts?.TopicRouting;
		string routingGuidance = NormalizeAuxiliaryRoutingRequestText(config?.RoutingGuidance ?? "");
		if (IsWorldDiplomacyTakeoverEnabledForTopicRouting())
		{
			routingGuidance = (routingGuidance
				+ "\n【外交接管话题边界】宣战、议和、结盟、解盟、贸易等国与国外交必须选择 DIPLOMACY（口头外交），不得选择 KINGDOM_AGENDA。"
				+ "玩家即使不是国王，只要是在劝说当前NPC国王让其自己的王国向第三国宣战，也选择 DIPLOMACY。"
				+ "KINGDOM_AGENDA 只用于国内政策、封地处置、驱逐氏族等非外交议程，以及对已经存在的非外交议程拉票。").Trim();
		}
		string emptyValue = RequirePreprocessPromptValue(config?.EmptyValue, "TopicRouting.EmptyValue");
		return RenderPreprocessPromptTemplate(config?.UserPromptTemplate, "TopicRouting.UserPromptTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["topic_list"] = topicList.ToString().TrimEnd(),
			["routing_guidance"] = routingGuidance,
			["history"] = string.IsNullOrWhiteSpace(historyBlock) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(historyBlock),
			["latest_npc"] = string.IsNullOrWhiteSpace(text2) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(text2),
			["latest_player"] = string.IsNullOrWhiteSpace(text5) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(text5),
			["top_n"] = Math.Max(1, topN).ToString(),
			["mentioned_entities_schema"] = StrictPreprocessMentionedEntitiesSchema
		});
	}

	private static bool IsWorldDiplomacyTakeoverEnabledForTopicRouting()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableWorldDiplomacy ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryCallAuxiliaryRuleRouterApi(string apiUrl, string apiKey, string modelName, string prompt, out string content, out string error)
	{
		while (true)
		{
			if (TryCallAuxiliaryRuleRouterApiOnce(apiUrl, apiKey, modelName, prompt, out content, out error))
			{
				return true;
			}
			if (!LlmRetryPrompt.PromptRetryBlocking("前处理规则选择", error))
			{
				return false;
			}
			Logger.Log("AIConfig", "[AuxiliaryRuleRouter] user requested retry after error: " + error);
		}
	}

	private static bool TryCallAuxiliaryRuleRouterApiOnce(string apiUrl, string apiKey, string modelName, string prompt, out string content, out string error)
	{
		content = "";
		error = "";
		string rawResponse = "";
		object[] array = BuildAuxiliaryRouterMessages(prompt);
		string requestBodyForTokenStats = "";
		try
		{
			string jsonBody = BuildAuxiliaryRouterRequestJsonForExternal(apiUrl, modelName, array, 5000, 0f, out var controlMode);
			requestBodyForTokenStats = jsonBody;
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
			LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage, apiUrl, apiKey);
			httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			HttpResponseMessage result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage).GetAwaiter().GetResult();
			string text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			rawResponse = text ?? "";
			if (!result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.BadRequest && controlMode != "plain" && LooksLikeAuxiliaryThinkingControlError(text))
			{
				Logger.Log("AIConfig", "[AuxiliaryRouter] thinking payload rejected; retrying without thinking controls.");
				JObject jObject2 = JObject.Parse(jsonBody);
				DuelSettings.RemoveThinkingControls(jObject2);
				string content2 = jObject2.ToString(Formatting.None);
				requestBodyForTokenStats = content2;
				using HttpRequestMessage httpRequestMessage2 = new HttpRequestMessage(HttpMethod.Post, apiUrl);
				LlmApiCompat.ApplyAuthenticationHeaders(httpRequestMessage2, apiUrl, apiKey);
				httpRequestMessage2.Content = new StringContent(content2, Encoding.UTF8, "application/json");
				result = DuelSettings.GlobalClient.SendAsync(httpRequestMessage2).GetAwaiter().GetResult();
				text = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				rawResponse = text ?? "";
				controlMode += "_retry_plain";
			}
			if (!result.IsSuccessStatusCode)
			{
				error = LlmRetryPrompt.BuildFailureDetail("http_" + (int)result.StatusCode, "", rawResponse);
				LogAuxiliaryRouterTokenTrace("auxiliary_router_http_error", array, "[AUXILIARY ROUTER HTTP]" + "\n" + "url=" + apiUrl + "\n" + "model=" + modelName + "\n" + "control_mode=" + controlMode + "\n" + "status=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\n" + "response_body=" + "\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			JObject jObject = JObject.Parse(text);
			content = LlmApiCompat.ExtractAssistantText(jObject).Trim();
			if (string.IsNullOrWhiteSpace(content))
			{
				error = LlmRetryPrompt.BuildFailureDetail("empty_content", "", rawResponse);
				LogAuxiliaryRouterTokenTrace("auxiliary_router_empty_content", array, "[AUXILIARY ROUTER HTTP]" + "\n" + "url=" + apiUrl + "\n" + "model=" + modelName + "\n" + "control_mode=" + controlMode + "\n" + "status=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\n" + "response_body=" + "\n" + (text ?? ""), 0, requestBodyForTokenStats);
				return false;
			}
			LogAuxiliaryRouterTokenTrace("auxiliary_router_http", array, "[AUXILIARY ROUTER HTTP]" + "\n" + "url=" + apiUrl + "\n" + "model=" + modelName + "\n" + "control_mode=" + controlMode + "\n" + "status=" + (int)result.StatusCode + " " + (result.ReasonPhrase ?? "") + "\n" + "ai_response=" + "\n" + content + "\n" + "raw_response=" + "\n" + (text ?? ""), Logger.EstimateTokens(content), requestBodyForTokenStats);
			return true;
		}
		catch (Exception ex)
		{
			error = LlmRetryPrompt.BuildFailureDetail(BuildAuxiliaryRouterExceptionText(ex), content, rawResponse);
			LogAuxiliaryRouterTokenTrace("auxiliary_router_exception", array, "[AUXILIARY ROUTER EXCEPTION]" + "\n" + "url=" + apiUrl + "\n" + "model=" + modelName + "\n" + "error=" + error + "\n" + "stack=" + "\n" + (ex?.StackTrace ?? ""), 0, requestBodyForTokenStats);
			return false;
		}
	}

	private static string StripAuxiliaryJsonCodeFence(string content)
	{
		string text = (content ?? "").Trim('\uFEFF', '\u200B', '\u200C', '\u200D', ' ', '\t', '\r', '\n');
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int firstLineEnd = text.IndexOf('\n');
			if (firstLineEnd >= 0)
			{
				text = text.Substring(firstLineEnd + 1).Trim();
			}
			int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
			if (lastFence >= 0)
			{
				text = text.Substring(0, lastFence).Trim();
			}
		}
		text = Regex.Replace(text, "^(?:json)\\s*(?=[\\r\\n{\\[])", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
		string jsonPayload = ExtractFirstAuxiliaryJsonPayload(text);
		return string.IsNullOrWhiteSpace(jsonPayload) ? text : jsonPayload;
	}

	internal static bool TryValidateStrictPreprocessJsonEnvelope(string content, bool requireMemoryIds, bool requireMentionedEntities, out JObject root, out string error)
	{
		root = null;
		error = "";
		string text = StripAuxiliaryJsonCodeFence(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "empty_content";
			return false;
		}
		try
		{
			root = JToken.Parse(text) as JObject;
		}
		catch (Exception ex)
		{
			error = "invalid_json:" + ex.GetType().Name;
			return false;
		}
		if (root == null)
		{
			error = "root_not_object";
			return false;
		}
		if (!ValidateStrictPreprocessArray(root, "rule_codes", JTokenType.String, out error))
		{
			return false;
		}
		if (requireMemoryIds && !ValidateStrictPreprocessArray(root, "memory_ids", JTokenType.Integer, out error))
		{
			return false;
		}
		if (!requireMentionedEntities)
		{
			return true;
		}
		JToken mentionedToken = root["mentioned_entities"];
		if (mentionedToken == null || mentionedToken.Type == JTokenType.Null)
		{
			error = "missing_mentioned_entities";
			return false;
		}
		JObject mentionedEntities = mentionedToken as JObject;
		if (mentionedEntities == null)
		{
			error = "mentioned_entities_not_object";
			return false;
		}
		JProperty unexpectedProperty = mentionedEntities.Properties().FirstOrDefault((JProperty x) => !string.Equals(x.Name, "entities", StringComparison.Ordinal));
		if (unexpectedProperty != null)
		{
			error = "mentioned_entities_unexpected_field_" + unexpectedProperty.Name;
			return false;
		}
		return ValidateStrictPreprocessArray(mentionedEntities, "entities", JTokenType.String, out error, "mentioned_entities_");
	}

	private static bool ValidateStrictPreprocessArray(JObject obj, string fieldName, JTokenType itemType, out string error, string errorPrefix = "")
	{
		error = "";
		JToken token = obj[fieldName];
		string field = errorPrefix + fieldName;
		if (token == null || token.Type == JTokenType.Null)
		{
			error = "missing_" + field;
			return false;
		}
		JArray array = token as JArray;
		if (array == null)
		{
			error = field + "_not_array";
			return false;
		}
		for (int i = 0; i < array.Count; i++)
		{
			JToken item = array[i];
			if (item == null || item.Type != itemType)
			{
				error = field + "_item_not_" + (itemType == JTokenType.Integer ? "integer" : "string");
				return false;
			}
		}
		return true;
	}

	private static string ExtractFirstAuxiliaryJsonPayload(string text)
	{
		text = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				string payload = ExtractBalancedAuxiliaryJsonPayload(text, i, '{', '}');
				if (!string.IsNullOrWhiteSpace(payload))
				{
					return payload;
				}
			}
			if (text[i] == '[')
			{
				string payload = ExtractBalancedAuxiliaryJsonPayload(text, i, '[', ']');
				if (!string.IsNullOrWhiteSpace(payload))
				{
					return payload;
				}
			}
		}
		return "";
	}

	private static string ExtractBalancedAuxiliaryJsonPayload(string text, int start, char open, char close)
	{
		text = text ?? "";
		if (start < 0 || start >= text.Length || text[start] != open)
		{
			return "";
		}
		bool inString = false;
		bool escaped = false;
		int depth = 0;
		for (int i = start; i < text.Length; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == open)
			{
				depth++;
				continue;
			}
			if (ch == close)
			{
				depth--;
				if (depth == 0)
				{
					return text.Substring(start, i - start + 1).Trim();
				}
				if (depth < 0)
				{
					return "";
				}
			}
		}
		return "";
	}

	private static bool TryParseAuxiliaryGuardrailRuleCodes(string content, IEnumerable<GuardrailAuxiliaryTopic> topics, out List<string> codes, out string error)
	{
		codes = new List<string>();
		error = "";
		Dictionary<string, string> dictionary = BuildAuxiliaryRuleCodeLookup(topics);
		if (dictionary.Count <= 0)
		{
			error = "no_known_rule_codes";
			return false;
		}
		if (!TryValidateStrictPreprocessJsonEnvelope(content, requireMemoryIds: false, requireMentionedEntities: true, out var root, out error))
		{
			return false;
		}
		JToken token = GetJsonPropertyIgnoreCase(root, "rule_codes");
		List<string> rawCodes = ReadAuxiliaryRuleCodeValues(token, out error);
		if (rawCodes == null)
		{
			return false;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> unknownCodes = new List<string>();
		foreach (string rawCode in rawCodes)
		{
			if (Regex.IsMatch((rawCode ?? "").Trim(), "^(?:[0-9]+|TOPIC_[0-9]+|T[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				error = "numeric_rule_code_not_allowed";
				codes.Clear();
				return false;
			}
			string code = NormalizeRuleCode(rawCode, "", "");
			if (string.IsNullOrWhiteSpace(code))
			{
				continue;
			}
			if (!dictionary.ContainsKey(code))
			{
				if (!unknownCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
				{
					unknownCodes.Add(code);
				}
				continue;
			}
			if (seen.Add(code))
			{
				codes.Add(code);
			}
		}
		if (codes.Count <= 0)
		{
			error = "";
			if (unknownCodes.Count > 0)
			{
				Logger.Log("GuardrailSemantic", "auxiliary_router no_known_rule_codes ignored=" + string.Join(",", unknownCodes.Take(8)));
			}
			return true;
		}
		if (unknownCodes.Count > 0)
		{
			Logger.Log("GuardrailSemantic", "auxiliary_router ignored_unknown_codes=" + string.Join(",", unknownCodes.Take(8)) + " accepted=" + string.Join(",", codes));
		}
		return true;
	}

	private static Dictionary<string, string> BuildAuxiliaryRuleCodeLookup(IEnumerable<GuardrailAuxiliaryTopic> topics)
	{
		Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (GuardrailAuxiliaryTopic topic in topics ?? Enumerable.Empty<GuardrailAuxiliaryTopic>())
		{
			string ruleId = (topic?.RuleId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(ruleId))
			{
				continue;
			}
			AddAuxiliaryRuleCodeLookupKey(lookup, topic.Code, ruleId);
			AddAuxiliaryRuleCodeLookupKey(lookup, ruleId, ruleId);
			AddAuxiliaryRuleCodeLookupKey(lookup, NormalizeRuleCode("", ruleId, topic.Label), ruleId);
		}
		return lookup;
	}

	private static void AddAuxiliaryRuleCodeLookupKey(Dictionary<string, string> lookup, string key, string ruleId)
	{
		if (lookup == null || string.IsNullOrWhiteSpace(ruleId))
		{
			return;
		}
		string normalized = NormalizeRuleCode(key, "", "");
		if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "RULE", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (!lookup.ContainsKey(normalized))
		{
			lookup[normalized] = ruleId;
		}
	}

	private static List<string> ReadAuxiliaryRuleCodeValues(JToken token, out string error)
	{
		error = "";
		List<string> values = new List<string>();
		if (token is JArray array)
		{
			foreach (JToken item in array)
			{
				if (item == null || item.Type == JTokenType.Null)
				{
					continue;
				}
				if (item.Type == JTokenType.String || item.Type == JTokenType.Integer)
				{
					string value = (item.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(value))
					{
						values.Add(value);
					}
					continue;
				}
				if (item is JObject obj)
				{
					JToken valueToken = GetJsonPropertyIgnoreCase(obj, "code", "rule_code", "ruleCode", "topic_code", "topicCode", "id", "rule_id", "ruleId", "topic_id", "topicId", "name", "label", "number");
					string value = (valueToken?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(value))
					{
						values.Add(value);
						continue;
					}
				}
				error = "rule_code_not_string_or_integer";
				return null;
			}
			return values;
		}
		if (token.Type == JTokenType.String || token.Type == JTokenType.Integer)
		{
			foreach (string part in Regex.Split(token.ToString() ?? "", "[,，;；\\s]+"))
			{
				string value = (part ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(value))
				{
					values.Add(value);
				}
			}
			return values;
		}
		error = "rule_codes_not_array_or_string";
		return null;
	}

	private static string BuildAuxiliaryPreprocessFormatError(string reason, string content)
	{
		string detail = "（API响应格式错误）前处理规则选择返回格式错误：" + (string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim()) + "。必须只输出一个 JSON 对象，并包含 rule_codes 字符串数组和 mentioned_entities.entities 字符串数组。";
		return LlmRetryPrompt.BuildFailureDetail(detail, content);
	}

	public static void PublishAuxiliaryMentionedEntitiesForExternal(string userText, string secondaryText, string runtimeGuardrailContext, string content)
	{
		ParseAndPublishAuxiliaryMentionedEntities(userText, secondaryText, runtimeGuardrailContext, content);
	}

	private static MentionedWorldEntities ParseAndPublishAuxiliaryMentionedEntities(string userText, string secondaryText, string runtimeGuardrailContext, string content)
	{
		try
		{
			if (!TryParseAuxiliaryMentionedEntities(content, out var entities, out var error) || entities == null || entities.IsEmpty)
			{
				if (!string.IsNullOrWhiteSpace(error))
				{
					Logger.Log("AuxiliaryEntity", "mentioned_entities skipped parse_error=" + error);
				}
				return new MentionedWorldEntities();
			}
			MergeLatestAuxiliaryMentionedEntities(entities);
			string key = BuildAuxiliaryMentionedEntitiesCacheKey(userText, secondaryText, runtimeGuardrailContext);
			if (string.IsNullOrWhiteSpace(key))
			{
				Logger.Log("AuxiliaryEntity", "mentioned_entities latest_only reason=empty_key " + FormatMentionedEntitiesCounts(entities));
				return entities.Clone();
			}
			lock (_auxiliaryMentionedEntitiesLock)
			{
				if (!_auxiliaryMentionedEntitiesCache.TryGetValue(key, out var existing) || existing == null)
				{
					existing = new MentionedWorldEntities();
					_auxiliaryMentionedEntitiesCache[key] = existing;
					_auxiliaryMentionedEntitiesCacheOrder.Enqueue(key);
				}
				existing.Merge(entities);
				while (_auxiliaryMentionedEntitiesCache.Count > AuxiliaryMentionedEntitiesCacheMax && _auxiliaryMentionedEntitiesCacheOrder.Count > 0)
				{
					string oldKey = _auxiliaryMentionedEntitiesCacheOrder.Dequeue();
					if (!string.Equals(oldKey, key, StringComparison.Ordinal))
					{
						_auxiliaryMentionedEntitiesCache.Remove(oldKey);
					}
				}
			}
			Logger.Log("AuxiliaryEntity", "mentioned_entities published key=" + HashAuxiliaryMentionKey(key) + " " + FormatMentionedEntitiesCounts(entities));
			return entities.Clone();
		}
		catch (Exception ex)
		{
			Logger.Log("AuxiliaryEntity", "mentioned_entities publish exception=" + ex.Message);
			return new MentionedWorldEntities();
		}
	}

	public static void ClearLatestAuxiliaryMentionedEntitiesForExternal()
	{
		try
		{
			_auxiliaryMentionedEntitiesLatest.Value = null;
		}
		catch
		{
		}
	}

	public static MentionedWorldEntities GetAuxiliaryMentionedEntitiesForExternal(string userText, string secondaryText, string runtimeGuardrailContext)
	{
		try
		{
			string key = BuildAuxiliaryMentionedEntitiesCacheKey(userText, secondaryText, runtimeGuardrailContext);
			if (string.IsNullOrWhiteSpace(key))
			{
				return new MentionedWorldEntities();
			}
			lock (_auxiliaryMentionedEntitiesLock)
			{
				if (_auxiliaryMentionedEntitiesCache.TryGetValue(key, out var entities) && entities != null)
				{
					Logger.Log("AuxiliaryEntity", "mentioned_entities get hit key=" + HashAuxiliaryMentionKey(key) + " " + FormatMentionedEntitiesCounts(entities));
					return entities.Clone();
				}
			}
			Logger.Log("AuxiliaryEntity", "mentioned_entities get miss key=" + HashAuxiliaryMentionKey(key));
		}
		catch
		{
		}
		return new MentionedWorldEntities();
	}

	public static MentionedWorldEntities GetLatestAuxiliaryMentionedEntitiesForExternal()
	{
		try
		{
			return _auxiliaryMentionedEntitiesLatest.Value?.Clone() ?? new MentionedWorldEntities();
		}
		catch
		{
			return new MentionedWorldEntities();
		}
	}

	private static void MergeLatestAuxiliaryMentionedEntities(MentionedWorldEntities entities)
	{
		try
		{
			if (entities == null || entities.IsEmpty)
			{
				return;
			}
			MentionedWorldEntities latest = _auxiliaryMentionedEntitiesLatest.Value;
			if (latest == null)
			{
				latest = new MentionedWorldEntities();
				_auxiliaryMentionedEntitiesLatest.Value = latest;
			}
			latest.Merge(entities);
		}
		catch
		{
		}
	}

	private static string FormatMentionedEntitiesCounts(MentionedWorldEntities entities)
	{
		if (entities == null)
		{
			return "entities=0";
		}
		return "entities=" + (entities.Entities?.Count ?? 0);
	}

	private static string HashAuxiliaryMentionKey(string value)
	{
		try
		{
			uint hash = 2166136261u;
			string text = value ?? "";
			for (int i = 0; i < text.Length; i++)
			{
				hash ^= text[i];
				hash *= 16777619u;
			}
			return hash.ToString("x8");
		}
		catch
		{
			return "00000000";
		}
	}

	public static bool TryParseAuxiliaryMentionedEntities(string content, out MentionedWorldEntities entities, out string error)
	{
		entities = new MentionedWorldEntities();
		error = "";
		try
		{
			string text = StripAuxiliaryJsonCodeFence(content);
			if (string.IsNullOrWhiteSpace(text))
			{
				error = "empty_content";
				return false;
			}
			JObject root = JObject.Parse(text);
			JToken token = GetJsonPropertyIgnoreCase(root, "mentioned_entities", "entities", "mentionedEntities");
			if (token == null || token.Type == JTokenType.Null)
			{
				return false;
			}
			if (token is JArray directArray)
			{
				FillMentionedEntityList(entities.Entities, directArray);
				return !entities.IsEmpty;
			}
			JObject obj = token as JObject;
			if (obj == null)
			{
				error = "mentioned_entities_not_object";
				return false;
			}
			FillMentionedEntityList(entities.Entities, GetJsonPropertyIgnoreCase(obj, "entities", "nouns", "keywords", "mentions", "mentioned_terms"));
			if (entities.IsEmpty)
			{
				string[] legacyBuckets = new string[8] { "heroes", "settlements", "clans", "kingdoms", "items", "policies", "troops", "terms" };
				foreach (string legacyBucket in legacyBuckets)
				{
					FillMentionedEntityList(entities.Entities, GetJsonPropertyIgnoreCase(obj, legacyBucket));
				}
			}
			return !entities.IsEmpty;
		}
		catch (Exception ex)
		{
			error = ex.GetType().Name + ":" + ex.Message;
			entities = new MentionedWorldEntities();
			return false;
		}
	}

	private static string BuildAuxiliaryMentionedEntitiesCacheKey(string userText, string secondaryText, string runtimeGuardrailContext)
	{
		string a = NormalizeSemanticText(userText);
		string b = NormalizeSemanticText(secondaryText);
		string c = NormalizeSemanticText(runtimeGuardrailContext);
		if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
		{
			return "";
		}
		return TrimAuxiliaryMentionCachePart(a, 500) + "\n--npc--\n" + TrimAuxiliaryMentionCachePart(b, 500) + "\n--ctx--\n" + TrimAuxiliaryMentionCachePart(c, 300);
	}

	private static string TrimAuxiliaryMentionCachePart(string value, int maxLength)
	{
		string text = (value ?? "").Trim();
		if (text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, maxLength);
	}

	private static JToken GetJsonPropertyIgnoreCase(JObject obj, params string[] names)
	{
		if (obj == null || names == null)
		{
			return null;
		}
		foreach (string name in names)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}
			JProperty prop = obj.Properties().FirstOrDefault((JProperty x) => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
			if (prop != null)
			{
				return prop.Value;
			}
		}
		return null;
	}

	private static void FillMentionedEntityList(List<string> target, JToken token)
	{
		if (target == null || token == null || token.Type == JTokenType.Null)
		{
			return;
		}
		if (token is JArray array)
		{
			foreach (JToken item in array)
			{
				AddMentionedEntityName(target, ExtractMentionedEntityName(item));
				if (target.Count >= 16)
				{
					break;
				}
			}
			return;
		}
		AddMentionedEntityName(target, ExtractMentionedEntityName(token));
	}

	private static string ExtractMentionedEntityName(JToken token)
	{
		if (token == null || token.Type == JTokenType.Null)
		{
			return "";
		}
		if (token is JObject obj)
		{
			JToken name = GetJsonPropertyIgnoreCase(obj, "name", "text", "value", "label", "id");
			return NormalizeMentionedEntityName(name?.ToString() ?? "");
		}
		return NormalizeMentionedEntityName(token.ToString());
	}

	private static void AddMentionedEntityName(List<string> target, string value)
	{
		string text = NormalizeMentionedEntityName(value);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!target.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			target.Add(text);
		}
	}

	private static string NormalizeMentionedEntityName(string value)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = text.Trim(' ', '\t', '\r', '\n', '"', '\'', '“', '”', '‘', '’', '《', '》', '<', '>', '[', ']', '【', '】', '(', ')', '（', '）');
		if (text.Length > 80)
		{
			text = text.Substring(0, 80).Trim();
		}
		return text;
	}

	private static int GetAuxiliaryGuardrailTopicNumberUpperBound()
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			int num = 12;
			if (dictionary != null)
			{
				foreach (GuardrailRulePromptConfig value in dictionary.Values)
				{
					if (value != null && value.TopicNumber > num)
					{
						num = value.TopicNumber;
					}
				}
			}
			return Math.Max(12, num);
		}
		catch
		{
			return 12;
		}
	}

	private static bool TryBuildAuxiliaryGuardrailEvalSnapshot(string userText, string runtimeGuardrailContext, string secondaryText, string cacheKey, out GuardrailEvalSnapshot snapshot, HashSet<string> excludedRuleIds = null, bool applyRuntimeEligibility = true)
	{
		snapshot = null;
		try
		{
			if (!TryGetAuxiliaryRuleRoutingConfig(out var apiUrl, out var apiKey, out var modelName))
			{
				return false;
			}
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			if (allEnabledRulePrompts == null || allEnabledRulePrompts.Count <= 0)
			{
				return false;
			}
			snapshot = new GuardrailEvalSnapshot
			{
				Key = cacheKey,
				MatchMode = "auxiliary_api",
				ReturnCap = GuardrailRuleReturnCap
			};
			for (int i = 0; i < allEnabledRulePrompts.Count; i++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[i];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				string text = guardrailRulePromptConfig.Id.Trim();
				if (excludedRuleIds != null && excludedRuleIds.Contains(text))
				{
					continue;
				}
				snapshot.Rules[text] = new GuardrailRuleEval
				{
					RuleTag = text,
					MatchedIntent = NormalizeSemanticText(userText),
					MatchMode = "auxiliary_api",
					Rank = int.MaxValue,
					RejectReason = "auxiliary_api_miss"
				};
			}
			List<GuardrailAuxiliaryTopic> list = GetEligibleAuxiliaryGuardrailTopics(snapshot.Rules.Keys, applyRuntimeEligibility);
			if (list.Count <= 0)
			{
				snapshot = null;
				return false;
			}
			string text2 = BuildAuxiliaryGuardrailRoutingPrompt(userText, secondaryText, runtimeGuardrailContext, list, snapshot.ReturnCap);
			string content = "";
			List<string> list2;
			while (true)
			{
				if (!TryCallAuxiliaryRuleRouterApi(apiUrl, apiKey, modelName, text2, out content, out var error))
				{
					Logger.Log("GuardrailSemantic", "auxiliary_router failed reason=" + error);
					snapshot = null;
					return false;
				}
				if (TryParseAuxiliaryGuardrailRuleCodes(content, list, out list2, out var parseError))
				{
					break;
				}
				string formatError = BuildAuxiliaryPreprocessFormatError(parseError, content);
				Logger.Log("GuardrailSemantic", "auxiliary_router format_error reason=" + parseError + " raw=" + JsonConvert.ToString(content ?? ""));
				LogAuxiliaryRouterTokenTrace("auxiliary_router_format_error", BuildAuxiliaryRouterMessages(text2), "[AUXILIARY ROUTER PARSE]" + "\n" + "url=" + apiUrl + "\n" + "model=" + modelName + "\n" + "reason=" + parseError + "\n" + "ai_response=" + "\n" + (content ?? ""), 0);
				if (!LlmRetryPrompt.PromptRetryBlocking("前处理规则选择", formatError))
				{
					throw new PreprocessFormatException(formatError);
				}
				Logger.Log("GuardrailSemantic", "auxiliary_router retry_after_format_error reason=" + parseError);
			}
			snapshot.MentionedEntities = ParseAndPublishAuxiliaryMentionedEntities(userText, secondaryText, runtimeGuardrailContext, content);
			if (list2.Count <= 0)
			{
				Logger.Log("GuardrailSemantic", "auxiliary_router no_known_topic raw=" + JsonConvert.ToString(content ?? "") + "; fallback=semantic");
				snapshot = null;
				return false;
			}
			HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> list3 = new List<string>();
			for (int j = 0; j < list2.Count; j++)
			{
				GuardrailAuxiliaryTopic guardrailAuxiliaryTopic = list.FirstOrDefault((GuardrailAuxiliaryTopic x) => x != null && string.Equals(NormalizeRuleCode(x.Code, x.RuleId, x.Label), list2[j], StringComparison.OrdinalIgnoreCase));
				string text3 = guardrailAuxiliaryTopic?.RuleId ?? "";
				if (!string.IsNullOrWhiteSpace(text3) && hashSet2.Add(text3))
				{
					list3.Add(text3);
				}
				if (list3.Count >= snapshot.ReturnCap)
				{
					break;
				}
			}
			PreferDirectDiplomacyTopicOverAgenda(list3);
			if (list3.Count <= 0)
			{
				snapshot = null;
				return false;
			}
			float num = 0f;
			for (int k = 0; k < list3.Count; k++)
			{
				string text4 = list3[k];
				if (!snapshot.Rules.TryGetValue(text4, out var value2))
				{
					continue;
				}
				GuardrailAuxiliaryTopic guardrailAuxiliaryTopic2 = list.FirstOrDefault((GuardrailAuxiliaryTopic x) => x != null && string.Equals(x.RuleId, text4, StringComparison.OrdinalIgnoreCase));
				float num2 = Math.Max(0.2f, 1f - (float)k * 0.08f);
				value2.MatchedSeed = guardrailAuxiliaryTopic2?.Label ?? ("topic_" + (k + 1));
				value2.RawInput = num2;
				value2.MixedRaw = num2;
				value2.AmpScore = num2;
				value2.RerankScore = num2;
				value2.Candidate = true;
				value2.AbsHit = true;
				value2.Hit = true;
				value2.Rank = k + 1;
				value2.RejectReason = $"auxiliary_api_return({k + 1}/{snapshot.ReturnCap})";
				num += num2;
			}
			float num3 = (list3.Count > 0) ? (num / (float)list3.Count) : 0f;
			for (int l = 0; l < list3.Count; l++)
			{
				string text5 = list3[l];
				if (!snapshot.Rules.TryGetValue(text5, out var value3))
				{
					continue;
				}
				value3.Mean = num3;
				if (l == 0 && list3.Count > 1 && snapshot.Rules.TryGetValue(list3[1], out var value4))
				{
					value3.TopGap = Math.Max(0f, value3.AmpScore - value4.AmpScore);
					value3.MaxOther = value4.AmpScore;
					value3.MaxOtherTag = value4.RuleTag;
				}
				else if (list3.Count > 0)
				{
					value3.TopGap = 1f;
					value3.MaxOther = (l > 0 && snapshot.Rules.TryGetValue(list3[0], out var value5)) ? value5.AmpScore : 0f;
					value3.MaxOtherTag = ((l > 0) ? list3[0] : "");
				}
			}
			Logger.Log("GuardrailSemantic", $"auxiliary_router success returnCap={snapshot.ReturnCap} raw={JsonConvert.ToString(content ?? "")} selected={string.Join(",", list3)}");
			return true;
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch (Exception ex)
		{
			Logger.Log("GuardrailSemantic", "auxiliary_router exception=" + ex.Message);
			snapshot = null;
			return false;
		}
	}

	public static bool TryCallAuxiliaryRuleCodesForExternal(string userText, string secondaryText, string runtimeGuardrailContext, int topN, out List<string> ruleIds, out string error)
	{
		return TryCallAuxiliaryRuleCodesForExternal(userText, secondaryText, runtimeGuardrailContext, topN, out ruleIds, out error, null);
	}

	public static bool TryCallAuxiliaryRuleCodesForExternal(string userText, string secondaryText, string runtimeGuardrailContext, int topN, out List<string> ruleIds, out string error, IEnumerable<string> excludedRuleIds)
	{
		ruleIds = new List<string>();
		error = "";
		try
		{
			if (!TryGetAuxiliaryRuleRoutingConfig(out var apiUrl, out var apiKey, out var modelName))
			{
				error = "auxiliary_rule_router_config_invalid";
				return false;
			}
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			if (allEnabledRulePrompts == null || allEnabledRulePrompts.Count <= 0)
			{
				error = "no_enabled_rule_topics";
				return false;
			}
			HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds);
			List<GuardrailAuxiliaryTopic> topics = GetEligibleAuxiliaryGuardrailTopics(allEnabledRulePrompts.Select((GuardrailRulePromptConfig x) => x?.Id ?? "").Where((string x) => !excluded.Contains((x ?? "").Trim())));
			if (topics.Count <= 0)
			{
				error = "no_eligible_rule_topics";
				return false;
			}
			int returnCap = Math.Max(1, topN <= 0 ? GuardrailRuleReturnCap : topN);
			string prompt = BuildAuxiliaryGuardrailRoutingPrompt(userText, secondaryText, runtimeGuardrailContext, topics, returnCap);
			string content = "";
			List<string> codes;
			while (true)
			{
				if (!TryCallAuxiliaryRuleRouterApi(apiUrl, apiKey, modelName, prompt, out content, out error))
				{
					return false;
				}
				if (TryParseAuxiliaryGuardrailRuleCodes(content, topics, out codes, out var parseError))
				{
					break;
				}
				error = BuildAuxiliaryPreprocessFormatError(parseError, content);
				if (!LlmRetryPrompt.PromptRetryBlocking("前处理规则选择", error))
				{
					return false;
				}
				Logger.Log("AIConfig", "[AuxiliaryRuleRouter] user requested retry after format error: " + parseError);
			}
			PublishAuxiliaryMentionedEntitiesForExternal(userText, secondaryText, runtimeGuardrailContext, content);
			if (codes.Count <= 0)
			{
				error = "no_known_rule_codes";
				Logger.Log("AIConfig", "[AuxiliaryRuleRouter] no known rule codes; raw=" + JsonConvert.ToString(content ?? ""));
				return false;
			}
			foreach (string code in codes)
			{
				GuardrailAuxiliaryTopic topic = topics.FirstOrDefault((GuardrailAuxiliaryTopic x) => x != null && string.Equals(NormalizeRuleCode(x.Code, x.RuleId, x.Label), code, StringComparison.OrdinalIgnoreCase));
				string ruleId = topic?.RuleId ?? "";
				if (!string.IsNullOrWhiteSpace(ruleId) && !ruleIds.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
				{
					ruleIds.Add(ruleId);
				}
				if (ruleIds.Count >= returnCap)
				{
					break;
				}
			}
			PreferDirectDiplomacyTopicOverAgenda(ruleIds);
			if (ruleIds.Count <= 0)
			{
				error = "rule_ids_empty";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = BuildAuxiliaryRouterExceptionText(ex);
			return false;
		}
	}

	private static bool TryGetGuardrailEvalSnapshot(string userText, string secondaryText, out GuardrailEvalSnapshot snapshot, IEnumerable<string> excludedRuleIds = null)
	{
		return TryGetGuardrailEvalSnapshot(userText, secondaryText, out snapshot, excludedRuleIds, applyRuntimeAutoExclusions: true);
	}

	private static bool TryGetGuardrailEvalSnapshot(string userText, string secondaryText, out GuardrailEvalSnapshot snapshot, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions)
	{
		snapshot = null;
		List<GuardrailIntentInput> list = new List<GuardrailIntentInput>();
		List<string> list2 = new List<string>();
		try
		{
			string runtimeGuardrailContext = GetRuntimeGuardrailContext();
			HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds, applyRuntimeAutoExclusions);
			string excludeKey = excluded.Count == 0 ? "" : ("|exclude:" + string.Join(",", excluded.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
			string text = BuildGuardrailEvalKey(userText, runtimeGuardrailContext + (UseAuxiliaryRuleApiRetrieval ? ("\n" + GetAuxiliarySceneDialogueHistoryContext()) : ""), secondaryText) + (UseAuxiliaryRuleApiRetrieval ? "|aux" : "|rag") + excludeKey;
			lock (_guardrailSemanticLock)
			{
				if (_lastGuardrailEval != null && string.Equals(_lastGuardrailEval.Key, text, StringComparison.Ordinal))
				{
					snapshot = _lastGuardrailEval;
					return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
				}
			}
			if (UseAuxiliaryRuleApiRetrieval && TryBuildAuxiliaryGuardrailEvalSnapshot(userText, runtimeGuardrailContext, secondaryText, text, out snapshot, excluded, applyRuntimeAutoExclusions))
			{
				lock (_guardrailSemanticLock)
				{
					_lastGuardrailEval = snapshot;
				}
				return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
			}
			appendInputs(SplitGuardrailIntents(userText, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f);
			string text2 = NormalizeSemanticText(secondaryText);
			if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, NormalizeSemanticText(userText), StringComparison.Ordinal))
			{
				appendInputs(SplitGuardrailIntents(text2, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f);
			}
			if (list.Count <= 0)
			{
				try
				{
					Logger.Log("GuardrailSemantic", $"snapshot_fail reason=no_input_embeddings intentCount={list2.Count} input={NormalizeSemanticText(userText)}");
				}
				catch
				{
				}
				return false;
			}
			if (list2.Count > 1)
			{
				try
				{
					Logger.Log("GuardrailSemantic", string.Format("intent_split count={0} intents={1}", list2.Count, string.Join(" || ", list2)));
				}
				catch
				{
				}
			}
			float[] vec2 = null;
			if (!string.IsNullOrWhiteSpace(runtimeGuardrailContext))
			{
				TryGetInputEmbedding(runtimeGuardrailContext, out vec2);
			}
			bool flag = vec2 != null && vec2.Length != 0;
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			if (allEnabledRulePrompts == null || allEnabledRulePrompts.Count <= 0)
			{
				return false;
			}
			GuardrailEvalSnapshot guardrailEvalSnapshot = new GuardrailEvalSnapshot
			{
				Key = text
			};
			List<GuardrailRuleEval> list4 = new List<GuardrailRuleEval>();
			Dictionary<string, GuardrailRulePromptConfig> dictionary = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
			for (int j = 0; j < allEnabledRulePrompts.Count; j++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[j];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				string id = guardrailRulePromptConfig.Id;
				if (excluded.Contains(id))
				{
					continue;
				}
				string ruleInstruction = guardrailRulePromptConfig.Instruction ?? "";
				List<string> list5 = BuildRuleSemanticSeeds(id, ruleInstruction, guardrailRulePromptConfig.TriggerKeywords);
				float num = 0f;
				float num2 = 0f;
				string matchedSeed = "";
				string matchedIntent = "";
				for (int k = 0; k < list5.Count; k++)
				{
					string text3 = list5[k];
					if (string.IsNullOrWhiteSpace(text3) || !TryGetPhraseEmbedding(text3, out var vec3) || vec3 == null || vec3.Length == 0)
					{
						continue;
					}
					for (int l = 0; l < list.Count; l++)
					{
						GuardrailIntentInput guardrailIntentInput2 = list[l];
						if (guardrailIntentInput2?.Vector == null || guardrailIntentInput2.Vector.Length == 0)
						{
							continue;
						}
						float num3 = DotProductNormalized(guardrailIntentInput2.Vector, vec3) * Math.Max(0f, guardrailIntentInput2.Weight);
						if (num3 > num)
						{
							num = num3;
							matchedSeed = text3;
							matchedIntent = guardrailIntentInput2.Text;
						}
					}
					if (flag)
					{
						float num4 = DotProductNormalized(vec2, vec3);
						if (num4 > num2)
						{
							num2 = num4;
						}
					}
				}
				float num5 = 0f;
				float mixedRaw = (flag ? (num * (1f - num5) + num2 * num5) : num);
				GuardrailRuleEval guardrailRuleEval = new GuardrailRuleEval
				{
					RuleTag = id,
					MatchedSeed = matchedSeed,
					MatchedIntent = matchedIntent,
					RawInput = num,
					RawContext = num2,
					MixedRaw = mixedRaw,
					AmpScore = mixedRaw,
					RerankScore = mixedRaw
				};
				list4.Add(guardrailRuleEval);
				dictionary[id] = guardrailRulePromptConfig;
				guardrailEvalSnapshot.Rules[id] = guardrailRuleEval;
			}
			int guardrailReturnCapFromMcm = GuardrailRuleReturnCap;
			int num6 = Math.Max(1, list.Count);
			int guardrailRerankBudget = GetGuardrailRerankBudget(guardrailReturnCapFromMcm);
			int guardrailPerIntentRerank = GetGuardrailPerIntentRerank(guardrailRerankBudget, num6);
			int guardrailPerIntentRecall = GetGuardrailPerIntentRecall(guardrailPerIntentRerank);
			guardrailEvalSnapshot.IntentCount = num6;
			guardrailEvalSnapshot.ReturnCap = guardrailReturnCapFromMcm;
			guardrailEvalSnapshot.RerankPerIntent = guardrailPerIntentRerank;
			guardrailEvalSnapshot.RecallPerIntent = guardrailPerIntentRecall;
			OnnxCrossEncoderReranker onnxCrossEncoderReranker = null;
			bool flag2 = false;
			try
			{
				onnxCrossEncoderReranker = OnnxCrossEncoderReranker.Instance;
				flag2 = onnxCrossEncoderReranker != null && onnxCrossEncoderReranker.IsAvailable;
			}
			catch
			{
				flag2 = false;
			}
			string text4 = (flag2 ? ((list.Count > 1) ? "rerank_multi" : "rerank") : ((list.Count > 1) ? "semantic_multi" : "semantic"));
			guardrailEvalSnapshot.MatchMode = text4;
			Dictionary<string, GuardrailRuleAggregate> dictionary2 = new Dictionary<string, GuardrailRuleAggregate>(StringComparer.OrdinalIgnoreCase);
			for (int k = 0; k < list.Count; k++)
			{
				GuardrailIntentInput guardrailIntentInput = list[k];
				if (guardrailIntentInput?.Vector == null || guardrailIntentInput.Vector.Length == 0)
				{
					continue;
				}
				List<GuardrailRuleScore> list5 = new List<GuardrailRuleScore>();
				for (int l = 0; l < allEnabledRulePrompts.Count; l++)
				{
					GuardrailRulePromptConfig guardrailRulePromptConfig2 = allEnabledRulePrompts[l];
					if (guardrailRulePromptConfig2 == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig2.Id))
					{
						continue;
					}
					string id2 = guardrailRulePromptConfig2.Id;
					if (excluded.Contains(id2))
					{
						continue;
					}
					guardrailEvalSnapshot.Rules.TryGetValue(id2, out var value);
					List<string> list6 = BuildRuleSemanticSeeds(id2, guardrailRulePromptConfig2.Instruction ?? "", guardrailRulePromptConfig2.TriggerKeywords);
					float num7 = 0f;
					string text5 = "";
					for (int m = 0; m < list6.Count; m++)
					{
						string text6 = list6[m];
						if (string.IsNullOrWhiteSpace(text6) || !TryGetPhraseEmbedding(text6, out var vec3) || vec3 == null || vec3.Length == 0)
						{
							continue;
						}
						float num8 = DotProductNormalized(guardrailIntentInput.Vector, vec3) * Math.Max(0f, guardrailIntentInput.Weight);
						if (num8 > num7)
						{
							num7 = num8;
							text5 = text6;
						}
					}
					float num9 = ((flag && value != null) ? value.RawContext : 0f);
					float num10 = 0f;
					float num11 = (flag ? (num7 * (1f - num10) + num9 * num10) : num7);
					list5.Add(new GuardrailRuleScore
					{
						Rule = guardrailRulePromptConfig2,
						RawScore = num11,
						FinalScore = num11,
						MatchedSeed = text5,
						MatchedIntent = guardrailIntentInput.Text
					});
				}
				list5 = list5.OrderByDescending((GuardrailRuleScore x) => x.RawScore).ThenBy((GuardrailRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).Take(guardrailPerIntentRecall).ToList();
				if (list5.Count <= 0)
				{
					continue;
				}
				int num12 = Math.Min(guardrailPerIntentRerank, list5.Count);
				List<GuardrailRuleScore> list7 = new List<GuardrailRuleScore>();
				List<string> rerankTexts = null;
				List<float> rerankScores = null;
				bool flag3 = false;
				if (flag2)
				{
					rerankTexts = new List<string>(num12);
					for (int n = 0; n < num12; n++)
					{
						GuardrailRuleScore guardrailRuleScore = list5[n];
						rerankTexts.Add((guardrailRuleScore?.Rule == null) ? "" : BuildGuardrailRuleRerankText(guardrailRuleScore.Rule));
					}
					flag3 = onnxCrossEncoderReranker.TryScoreBatch(guardrailIntentInput.Text, rerankTexts, out rerankScores) && rerankScores != null && rerankScores.Count == num12;
				}
				for (int n = 0; n < num12; n++)
				{
					GuardrailRuleScore guardrailRuleScore = list5[n];
					if (guardrailRuleScore?.Rule == null)
					{
						continue;
					}
					float num13 = guardrailRuleScore.RawScore;
					if (flag2 && flag3 && rerankTexts != null && n < rerankTexts.Count && !string.IsNullOrWhiteSpace(rerankTexts[n]) && rerankScores != null && n < rerankScores.Count)
					{
						num13 = rerankScores[n] * Math.Max(0f, guardrailIntentInput.Weight);
					}
					list7.Add(new GuardrailRuleScore
					{
						Rule = guardrailRuleScore.Rule,
						RawScore = guardrailRuleScore.RawScore,
						FinalScore = num13,
						MatchedSeed = guardrailRuleScore.MatchedSeed,
						MatchedIntent = guardrailRuleScore.MatchedIntent
					});
				}
				List<GuardrailRuleScore> list8 = SelectGuardrailCandidateScores(list7, (flag2 && flag3) ? "cross_encoder" : "recall_fallback", guardrailIntentInput.Text, num12);
				for (int num14 = 0; num14 < list8.Count; num14++)
				{
					GuardrailRuleScore guardrailRuleScore2 = list8[num14];
					if (guardrailRuleScore2?.Rule == null)
					{
						continue;
					}
					string text8 = (guardrailRuleScore2.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text8) || !guardrailEvalSnapshot.Rules.TryGetValue(text8, out var value2))
					{
						continue;
					}
					if (!dictionary2.TryGetValue(text8, out var value3))
					{
						value3 = new GuardrailRuleAggregate
						{
							Eval = value2
						};
					}
					value3.ScoreSum += guardrailRuleScore2.FinalScore;
					value3.HitCount++;
					if (num14 + 1 < value3.BestRank)
					{
						value3.BestRank = num14 + 1;
					}
					if (guardrailRuleScore2.FinalScore >= value3.BestScore)
					{
						value3.BestScore = guardrailRuleScore2.FinalScore;
						value3.MatchedSeed = guardrailRuleScore2.MatchedSeed;
						value3.MatchedIntent = guardrailRuleScore2.MatchedIntent;
					}
					dictionary2[text8] = value3;
				}
			}
			for (int num15 = 0; num15 < list4.Count; num15++)
			{
				GuardrailRuleEval guardrailRuleEval2 = list4[num15];
				if (guardrailRuleEval2 != null)
				{
					guardrailRuleEval2.Candidate = false;
					guardrailRuleEval2.AmpScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.RerankScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.MatchMode = text4;
				}
			}
			int num16 = 0;
			if (dictionary2.Count > 0)
			{
				int num17 = Math.Max(guardrailReturnCapFromMcm * 2, guardrailPerIntentRerank * Math.Min(list.Count, 3));
				if (num17 < guardrailReturnCapFromMcm)
				{
					num17 = guardrailReturnCapFromMcm;
				}
				if (num17 > 24)
				{
					num17 = 24;
				}
				List<GuardrailRuleAggregate> list9 = (from x in dictionary2.Values
					orderby Math.Min(1f, x.ScoreSum / (float)Math.Max(1, x.HitCount) + (float)(x.HitCount - 1) * 0.08f) descending, x.BestRank
					select x).ThenBy((GuardrailRuleAggregate x) => x?.Eval?.RuleTag ?? "", StringComparer.OrdinalIgnoreCase).Take(num17).ToList();
				num16 = list9.Count;
				for (int num18 = 0; num18 < list9.Count; num18++)
				{
					GuardrailRuleAggregate guardrailRuleAggregate = list9[num18];
					if (guardrailRuleAggregate?.Eval == null)
					{
						continue;
					}
					float num19 = guardrailRuleAggregate.ScoreSum / (float)Math.Max(1, guardrailRuleAggregate.HitCount) + (float)(guardrailRuleAggregate.HitCount - 1) * 0.08f;
					if (num19 > 1f)
					{
						num19 = 1f;
					}
					guardrailRuleAggregate.Eval.Candidate = true;
					guardrailRuleAggregate.Eval.AmpScore = num19;
					guardrailRuleAggregate.Eval.RerankScore = guardrailRuleAggregate.BestScore;
					guardrailRuleAggregate.Eval.MatchMode = text4;
					if (!string.IsNullOrWhiteSpace(guardrailRuleAggregate.MatchedSeed))
					{
						guardrailRuleAggregate.Eval.MatchedSeed = guardrailRuleAggregate.MatchedSeed;
					}
					if (!string.IsNullOrWhiteSpace(guardrailRuleAggregate.MatchedIntent))
					{
						guardrailRuleAggregate.Eval.MatchedIntent = guardrailRuleAggregate.MatchedIntent;
					}
				}
			}
			list4 = list4.OrderByDescending((GuardrailRuleEval x) => x.Candidate ? 1 : 0).ThenByDescending((GuardrailRuleEval x) => x.Candidate ? x.AmpScore : x.MixedRaw).ThenBy((GuardrailRuleEval x) => x.RuleTag, StringComparer.OrdinalIgnoreCase).ToList();
			float num20 = ((list4.Count > 0) ? list4.Average((GuardrailRuleEval x) => x.Candidate ? x.AmpScore : x.MixedRaw) : 0f);
			float num21 = ((list4.Count > 1) ? ((list4[0].Candidate ? list4[0].AmpScore : list4[0].MixedRaw) - (list4[1].Candidate ? list4[1].AmpScore : list4[1].MixedRaw)) : 1f);
			for (int num22 = 0; num22 < list4.Count; num22++)
			{
				GuardrailRuleEval guardrailRuleEval3 = list4[num22];
				guardrailRuleEval3.Mean = num20;
				guardrailRuleEval3.Rank = num22 + 1;
				float num23 = -1f;
				string maxOtherTag = "";
				for (int num24 = 0; num24 < list4.Count; num24++)
				{
					if (num24 == num22)
					{
						continue;
					}
					GuardrailRuleEval guardrailRuleEval4 = list4[num24];
					float num25 = (guardrailRuleEval4.Candidate ? guardrailRuleEval4.AmpScore : guardrailRuleEval4.MixedRaw);
					if (num25 > num23)
					{
						num23 = num25;
						maxOtherTag = guardrailRuleEval4.RuleTag;
					}
				}
				float num26 = guardrailRuleEval3.Candidate ? guardrailRuleEval3.AmpScore : guardrailRuleEval3.MixedRaw;
				float delta = ((num23 < -0.5f) ? num26 : (num26 - num23));
				float num27 = 0f;
				float num28 = 0f;
				string bestSeed = "";
				bool lexicalAnchor = false;
				bool flag3 = guardrailRuleEval3.Candidate && guardrailRuleEval3.Rank <= guardrailReturnCapFromMcm;
				string rejectReason = (flag3 ? (text4 + "_return(" + guardrailRuleEval3.Rank + "/" + guardrailReturnCapFromMcm + ")") : (guardrailRuleEval3.Candidate ? (text4 + "_return_overflow") : (text4 + "_recall_miss")));
				guardrailRuleEval3.MaxOther = num23;
				guardrailRuleEval3.MaxOtherTag = maxOtherTag;
				guardrailRuleEval3.Delta = delta;
				guardrailRuleEval3.TopGap = num21;
				guardrailRuleEval3.IntentEvidence = num27;
				guardrailRuleEval3.IntentGate = num28;
				guardrailRuleEval3.IntentSeed = bestSeed;
				guardrailRuleEval3.LexicalAnchor = lexicalAnchor;
				guardrailRuleEval3.AbsHit = flag3;
				guardrailRuleEval3.RelHit = false;
				guardrailRuleEval3.HighAmpHit = false;
				guardrailRuleEval3.ForceHit = false;
				guardrailRuleEval3.RejectReason = rejectReason;
				guardrailRuleEval3.MatchMode = text4;
				guardrailRuleEval3.Hit = flag3;
			}
			try
			{
				Logger.Log("GuardrailSemantic", $"candidate_pool mode={text4} returnCap={guardrailReturnCapFromMcm} rerankBudget={guardrailRerankBudget} rerankPerIntent={guardrailPerIntentRerank} recallPerIntent={guardrailPerIntentRecall} intents={num6} got={num16}");
			}
			catch
			{
			}
			try
			{
				int count = list4.Count;
				int num29 = 0;
				for (int num30 = 0; num30 < list4.Count; num30++)
				{
					GuardrailRuleEval guardrailRuleEval5 = list4[num30];
					if (guardrailRuleEval5 != null)
					{
						if (guardrailRuleEval5.Hit)
						{
							num29++;
						}
						Logger.RecordHitRate("guardrail", guardrailRuleEval5.RuleTag ?? "__unknown__", guardrailRuleEval5.Hit, BuildSemanticHitRateDetail($"raw={guardrailRuleEval5.RawInput:0.000} rerank={guardrailRuleEval5.RerankScore:0.000} amp={guardrailRuleEval5.AmpScore:0.000} rank={guardrailRuleEval5.Rank} reason={guardrailRuleEval5.RejectReason}", secondaryText), userText);
					}
				}
				Logger.RecordHitRate("guardrail", "__query__", num29 > 0, BuildSemanticHitRateDetail($"hits={num29}/{count} inputLen={userText.Length}", secondaryText), userText);
			}
			catch
			{
			}
			lock (_guardrailSemanticLock)
			{
				_lastGuardrailEval = guardrailEvalSnapshot;
			}
			snapshot = guardrailEvalSnapshot;
			return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("GuardrailSemantic", "snapshot_fail reason=exception msg=" + ex.Message + " stack=" + ex.StackTrace?.Replace("\n", " | ")?.Substring(0, Math.Min(ex.StackTrace?.Length ?? 0, 300)));
			}
			catch
			{
			}
			return false;
		}

		void appendInputs(List<string> intents, int perSourceLimit, float weight)
		{
			if (intents == null || intents.Count <= 0 || weight <= 0f || perSourceLimit <= 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < intents.Count; i++)
			{
				if (list.Count >= IntentQueryOptimizer.MaxCombinedIntentCount)
				{
					break;
				}
				string text4 = NormalizeSemanticText(intents[i]);
				if (!string.IsNullOrWhiteSpace(text4) && TryGetInputEmbedding(text4, out var vec) && vec != null && vec.Length != 0)
				{
					num++;
					if (num > perSourceLimit)
					{
						break;
					}
					list2.Add(text4);
					list.Add(new GuardrailIntentInput
					{
						Text = text4,
						Vector = vec,
						Weight = weight
					});
				}
			}
		}
	}

	private static string BuildGuardrailRuleRerankText(GuardrailRulePromptConfig rule)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			string text = NormalizeSemanticText(rule.Id);
			string text2 = NormalizeSemanticText(rule.Group);
			string text3 = NormalizeSemanticText(BuildRuleInstructionSeed(rule.Id, rule.Instruction));
			List<string> list = NormalizeStringList(rule.TriggerKeywords, 48);
			if (list.Count > 6)
			{
				list = list.Take(6).ToList();
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine("规则组: " + text2);
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.AppendLine("规则ID: " + text);
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine("用途: " + text3);
			}
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("触发词: " + string.Join(" / ", list));
			}
			return NormalizeSemanticText(stringBuilder.ToString());
		}
		catch
		{
			return "";
		}
	}

	private static int GetGuardrailRerankBudget(int returnCap)
	{
		int num = Math.Max(1, returnCap) * 3;
		if (num < 8)
		{
			num = 8;
		}
		if (num > 36)
		{
			num = 36;
		}
		return num;
	}

	private static int GetGuardrailPerIntentRerank(int rerankBudget, int intentCount)
	{
		int num = ((intentCount > 0) ? intentCount : 1);
		int num2 = (int)Math.Round((double)rerankBudget / (double)num, MidpointRounding.AwayFromZero);
		if (num2 < 4)
		{
			num2 = 4;
		}
		if (num2 > 12)
		{
			num2 = 12;
		}
		return num2;
	}

	private static int GetGuardrailPerIntentRecall(int rerankPerIntent)
	{
		int num = (int)Math.Round((double)rerankPerIntent * 2.5, MidpointRounding.AwayFromZero);
		if (num < 10)
		{
			num = 10;
		}
		if (num > 30)
		{
			num = 30;
		}
		return num;
	}

	private static List<GuardrailRuleScore> SelectGuardrailCandidateScores(List<GuardrailRuleScore> scored, string source, string input, int topK)
	{
		List<GuardrailRuleScore> list = new List<GuardrailRuleScore>();
		try
		{
			int num = ((topK <= 0) ? 4 : topK);
			float num2 = 0.21f;
			List<GuardrailRuleScore> list2 = (from x in scored
				where x?.Rule != null && !float.IsNaN(x.FinalScore)
				orderby x.FinalScore descending, x.RawScore descending
				select x).ThenBy((GuardrailRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			float num3 = ((list2.Count > 0) ? list2[0].FinalScore : 0f);
			float num4 = ((list2.Count > 1) ? list2[1].FinalScore : 0f);
			float num5 = ((list2.Count > 0) ? list2[0].RawScore : 0f);
			float num6 = ((list2.Count > 1) ? list2[1].RawScore : 0f);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num7 = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				GuardrailRuleScore guardrailRuleScore = list2[i];
				if (guardrailRuleScore?.Rule == null || guardrailRuleScore.FinalScore < num2)
				{
					continue;
				}
				string text = (guardrailRuleScore.Rule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
				{
					list.Add(guardrailRuleScore);
					num7++;
				}
			}
			if (list.Count < num)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					if (list.Count >= num)
					{
						break;
					}
					GuardrailRuleScore guardrailRuleScore2 = list2[j];
					if (guardrailRuleScore2?.Rule == null)
					{
						continue;
					}
					string text2 = (guardrailRuleScore2.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2) || hashSet.Add(text2))
					{
						list.Add(guardrailRuleScore2);
					}
				}
			}
			try
			{
				Logger.Log("GuardrailSemantic", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
			}
			catch
			{
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool TryGetRuleEval(string userText, string secondaryText, string ruleTag, out GuardrailRuleEval eval, IEnumerable<string> excludedRuleIds = null)
	{
		eval = null;
		if (!TryGetGuardrailEvalSnapshot(userText, secondaryText, out var snapshot, excludedRuleIds) || snapshot == null || snapshot.Rules == null)
		{
			return false;
		}
		string text = NormalizeSemanticText(ruleTag);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return snapshot.Rules.TryGetValue(text, out eval) && eval != null;
	}

	public static bool IsGuardrailSemanticHit(string input, List<string> triggerKeywords, string ruleTag)
	{
		string matchedKeyword;
		float score;
		return IsGuardrailSemanticHit(input, null, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, List<string> triggerKeywords, string ruleTag, out string matchedKeyword, out float score)
	{
		return IsGuardrailSemanticHit(input, null, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, string ruleTag, string ruleInstruction, List<string> triggerKeywords, out string matchedKeyword, out float score)
	{
		return IsGuardrailSemanticHit(input, null, ruleTag, ruleInstruction, triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, string secondaryInput, string ruleTag, string ruleInstruction, List<string> triggerKeywords, out string matchedKeyword, out float score, IEnumerable<string> excludedRuleIds = null)
	{
		matchedKeyword = "";
		score = 0f;
		HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds);
		string normalizedRuleTag = NormalizeSemanticText(ruleTag);
		if (!string.IsNullOrWhiteSpace(normalizedRuleTag) && excluded.Contains(normalizedRuleTag))
		{
			Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=blocked_excluded_rule");
			return false;
		}
		if (ShouldExcludeRuntimeRuleForConversationTarget(ruleTag))
		{
			Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=blocked_runtime_target_rule");
			return false;
		}
		if (IsSceneMoveRule(ruleTag) && ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=blocked_scene_move_mission");
			return false;
		}
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (TryGetRuleEval(text, secondaryInput, ruleTag, out var eval, excluded))
		{
			matchedKeyword = (string.IsNullOrWhiteSpace(eval.MatchedSeed) ? "semantic_seed" : eval.MatchedSeed);
			score = eval.AmpScore;
			string text2 = NormalizeSemanticText(eval.MatchedIntent);
			if (text2.Length > 48)
			{
				text2 = text2.Substring(0, 48);
			}
			Logger.Log("GuardrailSemantic", $"rule={ruleTag} hit={eval.Hit} mode={eval.MatchMode} raw={eval.RawInput:0.000} ctx={eval.RawContext:0.000} mixed={eval.MixedRaw:0.000} rerank={eval.RerankScore:0.000} amp={eval.AmpScore:0.000} delta={eval.Delta:0.000} topGap={eval.TopGap:0.000} rank={eval.Rank} candidate={eval.Candidate} other={eval.MaxOtherTag}@{eval.MaxOther:0.000} mean={eval.Mean:0.000} absHit={eval.AbsHit} relHit={eval.RelHit} highAmpHit={eval.HighAmpHit} forceHit={eval.ForceHit} intentEvidence={eval.IntentEvidence:0.000} intentGate={eval.IntentGate:0.000} lexicalAnchor={eval.LexicalAnchor} intentSeed={eval.IntentSeed} reason={eval.RejectReason} intent={text2}");
			return eval.Hit;
		}
		if (TryLexicalRuleKeywordHit(input, secondaryInput, triggerKeywords, out var lexicalKeyword))
		{
			matchedKeyword = lexicalKeyword;
			score = 1f;
			Logger.Log("GuardrailSemantic", $"rule={ruleTag} hit=True mode=lexical_fallback matched={lexicalKeyword}");
			return true;
		}
		Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=semantic_unavailable");
		try
		{
			Logger.RecordHitRate("guardrail", ruleTag ?? "__unknown__", hit: false, BuildSemanticHitRateDetail("reason=semantic_unavailable", secondaryInput), text);
		}
		catch
		{
		}
		return false;
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, int maxCount = 0, bool includeBuiltInRules = false)
	{
		return GetGuardrailSemanticRuleHits(input, null, maxCount, includeBuiltInRules);
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount = 0, bool includeBuiltInRules = false)
	{
		return GetGuardrailSemanticRuleHits(input, secondaryInput, maxCount, includeBuiltInRules, null);
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds)
	{
		return GetGuardrailSemanticRuleHits(input, secondaryInput, maxCount, includeBuiltInRules, excludedRuleIds, applyRuntimeAutoExclusions: true);
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHitsForPreprocess(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds)
	{
		return GetGuardrailSemanticRuleHits(input, secondaryInput, maxCount, includeBuiltInRules, excludedRuleIds, applyRuntimeAutoExclusions: false);
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHitsForPreprocess(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, out MentionedWorldEntities mentionedEntities)
	{
		return GetGuardrailSemanticRuleHits(input, secondaryInput, maxCount, includeBuiltInRules, excludedRuleIds, applyRuntimeAutoExclusions: false, out mentionedEntities);
	}

	private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions)
	{
		MentionedWorldEntities ignoredMentionedEntities;
		return GetGuardrailSemanticRuleHits(input, secondaryInput, maxCount, includeBuiltInRules, excludedRuleIds, applyRuntimeAutoExclusions, out ignoredMentionedEntities);
	}

	private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions, out MentionedWorldEntities mentionedEntities)
	{
		List<GuardrailRuleHit> list = new List<GuardrailRuleHit>();
		mentionedEntities = new MentionedWorldEntities();
		try
		{
			int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
			string text = NormalizeSemanticText(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds, applyRuntimeAutoExclusions);
			if (!TryGetGuardrailEvalSnapshot(text, secondaryInput, out var snapshot, excluded, applyRuntimeAutoExclusions) || snapshot?.Rules == null || snapshot.Rules.Count <= 0)
			{
				return list;
			}
			mentionedEntities = snapshot.MentionedEntities?.Clone() ?? new MentionedWorldEntities();
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			foreach (KeyValuePair<string, GuardrailRuleEval> rule in snapshot.Rules)
			{
				string text2 = (rule.Key ?? "").Trim().ToLowerInvariant();
				GuardrailRuleEval value = rule.Value;
				if (!string.IsNullOrWhiteSpace(text2) && !excluded.Contains(text2) && value != null && value.Hit && (includeBuiltInRules || !IsBuiltInRuleTag(text2)) && IsRuleCurrentlyEligibleForRag(text2))
				{
					dictionary.TryGetValue(text2, out var value2);
					string text3 = value2?.Instruction ?? "";
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list.Add(new GuardrailRuleHit
						{
							RuleId = text2,
							Group = (value2?.Group ?? ""),
							Priority = (value2?.Priority ?? 0),
							Score = value.AmpScore,
							MatchedSeed = (value.MatchedSeed ?? ""),
							Instruction = text3
						});
					}
				}
			}
			if (IsWorldDiplomacyTakeoverEnabledForTopicRouting()
				&& list.Any(hit => string.Equals(hit?.RuleId, "diplomacy", StringComparison.OrdinalIgnoreCase)))
			{
				list.RemoveAll(hit => string.Equals(hit?.RuleId, "kingdom_agenda", StringComparison.OrdinalIgnoreCase));
			}
			list = (from x in list
				orderby x.Priority descending, x.Score descending
				select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
			if (num > 0 && list.Count > num)
			{
				list = list.Take(num).ToList();
			}
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch
		{
		}
		return list;
	}

	private static void PreferDirectDiplomacyTopicOverAgenda(List<string> ruleIds)
	{
		if (ruleIds == null
			|| !IsWorldDiplomacyTakeoverEnabledForTopicRouting()
			|| !ruleIds.Any(id => string.Equals(id, "diplomacy", StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		ruleIds.RemoveAll(id => string.Equals(id, "kingdom_agenda", StringComparison.OrdinalIgnoreCase));
	}

	private static HashSet<string> BuildExcludedRuleIdSet(IEnumerable<string> excludedRuleIds)
	{
		return BuildExcludedRuleIdSet(excludedRuleIds, applyRuntimeAutoExclusions: true);
	}

	private static HashSet<string> BuildExcludedRuleIdSet(IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions)
	{
		HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (string id in excludedRuleIds ?? Enumerable.Empty<string>())
			{
				string text = (id ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					set.Add(text);
				}
			}
			if (applyRuntimeAutoExclusions && ShouldExcludePlayerPartyTradeLimitedRulesForConversationTarget())
			{
				set.Add("loan");
				set.Add("diplomacy");
				set.Add("kingdom_agenda");
				set.Add("party_transfer");
			}
			if (applyRuntimeAutoExclusions && ShouldExcludeSceneMoveRuleForCurrentMission())
			{
				set.Add("scene_mechanism_actions");
			}
		}
		catch
		{
		}
		return set;
	}

	private static bool TryLexicalRuleKeywordHit(string input, string secondaryInput, List<string> triggerKeywords, out string matchedKeyword)
	{
		matchedKeyword = "";
		try
		{
			if (triggerKeywords == null || triggerKeywords.Count <= 0)
			{
				return false;
			}
			string text = NormalizeSemanticText(input);
			string text2 = NormalizeSemanticText(secondaryInput);
			if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			for (int i = 0; i < triggerKeywords.Count; i++)
			{
				string text3 = NormalizeSemanticText(triggerKeywords[i]);
				if (string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				if ((!string.IsNullOrWhiteSpace(text) && text.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrWhiteSpace(text2) && text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					matchedKeyword = text3;
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static List<GuardrailRuleHit> GetGuardrailLexicalRuleHits(string input, string secondaryInput, int maxCount = 0, bool includeBuiltInRules = false, IEnumerable<string> excludedRuleIds = null)
	{
		List<GuardrailRuleHit> list = new List<GuardrailRuleHit>();
		try
		{
			int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
			HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds);
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary == null || dictionary.Count <= 0)
			{
				return list;
			}
			foreach (GuardrailRulePromptConfig value in dictionary.Values)
			{
				string text = (value?.Id ?? "").Trim().ToLowerInvariant();
				if (value == null || !value.IsEnabled || string.IsNullOrWhiteSpace(text) || excluded.Contains(text) || (!includeBuiltInRules && IsBuiltInRuleTag(text)) || !IsRuleCurrentlyEligibleForRag(text))
				{
					continue;
				}
				if (!TryLexicalRuleKeywordHit(input, secondaryInput, value.TriggerKeywords, out var matchedKeyword))
				{
					continue;
				}
				string text2 = (value.Instruction ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(new GuardrailRuleHit
					{
						RuleId = text,
						Group = (value.Group ?? ""),
						Priority = value.Priority,
						Score = 1f,
						MatchedSeed = matchedKeyword,
						Instruction = text2
					});
				}
			}
			list = (from x in list
				orderby x.Priority descending, x.Score descending
				select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
			if (num > 0 && list.Count > num)
			{
				list = list.Take(num).ToList();
			}
			if (list.Count > 0)
			{
				Logger.Log("GuardrailSemantic", $"lexical_rule_fallback count={list.Count} input={NormalizeSemanticText(input)}");
			}
		}
		catch
		{
		}
		return list;
	}

	private static string ResolveGuardrailStickyTargetKey()
	{
		try
		{
			string text = (_guardrailRuntimeTargetHeroId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "hero:" + text;
			}
		}
		catch
		{
		}
		try
		{
			int value = _guardrailRuntimeTargetAgentIndex.Value;
			if (value >= 0)
			{
				return "agent:" + value;
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (_guardrailRuntimeTargetCharacterId.Value ?? _guardrailRuntimeTargetTroopId.Value ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string text3 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text3) ? ("troop:" + text2) : ("troop:" + text2 + "@" + text3);
			}
		}
		catch
		{
		}
		try
		{
			Hero hero = ResolveConversationTargetHero();
			string text4 = (hero?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return "hero:" + text4;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject characterObject = ResolveConversationTargetCharacter();
			string text5 = (characterObject?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				string text6 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text6) ? ("troop:" + text5) : ("troop:" + text5 + "@" + text6);
			}
		}
		catch
		{
		}
		return "";
	}

	private static int GetStickyGuardrailTurnLimit(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		switch (text)
		{
		case "kingdom_service":
		case "marriage":
			return 3;
		default:
			return 0;
		}
	}

	private static bool IsStickyGuardrailFollowUpInput(string input)
	{
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text) || text.Length > 24)
		{
			return false;
		}
		for (int i = 0; i < StickyGuardrailFollowUpPhrases.Length; i++)
		{
			string value = StickyGuardrailFollowUpPhrases[i];
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool DidGuardrailRuleRecentlyComplete(string ruleId, string secondaryInput)
	{
		string text = (secondaryInput ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		switch ((ruleId ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			return text.IndexOf("[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase) >= 0;
		case "reward":
			return text.IndexOf("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "loan":
			return RewardSystemBehavior.ContainsCanonicalDebtActionTagForExternal(text);
		case "kingdom_service":
			return text.IndexOf("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "kingdom_vassalage":
			return text.IndexOf("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "diplomacy":
			return text.IndexOf("[ACTION:DIPLOMACY:", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "marriage":
			return text.IndexOf("[ACTION:MARRIAGE_", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "party_transfer":
			return text.IndexOf("[ATT:", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ATP:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "lords_hall_access":
			return text.IndexOf("[ACTION:OPEN_LORDS_HALL]", StringComparison.OrdinalIgnoreCase) >= 0;
		default:
			return false;
		}
	}

	private static bool ShouldStartStickyGuardrailRule(string input, string secondaryInput, GuardrailRuleHit hit, int rank, IEnumerable<string> excludedRuleIds = null)
	{
		if (hit == null)
		{
			return false;
		}
		string text = (hit.RuleId ?? "").Trim();
		if (GetStickyGuardrailTurnLimit(text) <= 0 || DidGuardrailRuleRecentlyComplete(text, secondaryInput))
		{
			return false;
		}
		if (hit.Score >= 0.999f)
		{
			return true;
		}
		if (TryGetRuleEval(input, secondaryInput, text, out var eval, excludedRuleIds) && eval != null && eval.Hit)
		{
			if (eval.ForceHit || eval.HighAmpHit || eval.AbsHit)
			{
				return true;
			}
			if (eval.Rank <= 1 && eval.AmpScore >= 0.48f)
			{
				return true;
			}
		}
		if (rank == 0 && hit.Score >= 0.56f)
		{
			return true;
		}
		return hit.Score >= 0.62f;
	}

	private static bool ShouldContinueStickyGuardrailRule(StickyGuardrailRuleState state, GuardrailRulePromptConfig rule, string input, int currentLiveCount, string secondaryInput)
	{
		if (state == null || rule == null || state.RemainingCarryTurns <= 0 || DidGuardrailRuleRecentlyComplete(state.RuleId, secondaryInput))
		{
			return false;
		}
		if (currentLiveCount > 0)
		{
			return false;
		}
		if (TryLexicalRuleKeywordHit(input, null, rule.TriggerKeywords, out var _))
		{
			return true;
		}
		string text = NormalizeSemanticText(input);
		string text2 = NormalizeSemanticText(state.MatchedSeed);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return IsStickyGuardrailFollowUpInput(input);
	}

	private static float ApplyStickyGuardrailScoreDecay(float score, int maxCarryTurns, int carryTurnIndex)
	{
		float num = ((score > 0f) ? score : 0.6f);
		float num2;
		if (maxCarryTurns >= 3)
		{
			num2 = ((carryTurnIndex <= 1) ? 0.78f : ((carryTurnIndex == 2) ? 0.58f : 0.36f));
		}
		else
		{
			num2 = ((carryTurnIndex <= 1) ? 0.72f : 0.45f);
		}
		return Math.Max(0.18f, num * num2);
	}

	private static List<GuardrailRuleHit> MergeStickyGuardrailRuleHits(string input, string secondaryInput, List<GuardrailRuleHit> liveHits, int maxCount, IEnumerable<string> excludedRuleIds = null)
	{
		HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds);
		List<GuardrailRuleHit> list = (liveHits ?? new List<GuardrailRuleHit>()).Where((GuardrailRuleHit x) => x != null && !string.IsNullOrWhiteSpace(x.RuleId) && !excluded.Contains((x.RuleId ?? "").Trim())).OrderByDescending((GuardrailRuleHit x) => x.Priority).ThenByDescending((GuardrailRuleHit x) => x.Score).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
		int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
		string text = ResolveGuardrailStickyTargetKey();
		if (string.IsNullOrWhiteSpace(text))
		{
			return (num > 0 && list.Count > num) ? list.Take(num).ToList() : list;
		}
		Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
		if (dictionary == null || dictionary.Count <= 0)
		{
			return (num > 0 && list.Count > num) ? list.Take(num).ToList() : list;
		}
		HashSet<string> hashSet = new HashSet<string>(list.Select((GuardrailRuleHit x) => (x.RuleId ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
		List<StickyGuardrailRuleState> list2 = new List<StickyGuardrailRuleState>();
		lock (_stickyGuardrailRuleLock)
		{
			_stickyGuardrailRules.TryGetValue(text, out var value);
			value = value ?? new List<StickyGuardrailRuleState>();
			List<StickyGuardrailRuleState> list3 = new List<StickyGuardrailRuleState>();
			for (int i = 0; i < value.Count; i++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState = value[i];
				if (stickyGuardrailRuleState == null || string.IsNullOrWhiteSpace(stickyGuardrailRuleState.RuleId))
				{
					continue;
				}
				string text2 = stickyGuardrailRuleState.RuleId.Trim();
				if (excluded.Contains(text2))
				{
					list3.Add(stickyGuardrailRuleState);
					continue;
				}
				if (hashSet.Contains(stickyGuardrailRuleState.RuleId))
				{
					continue;
				}
				if (GetStickyGuardrailTurnLimit(text2) <= 0 || !dictionary.TryGetValue(text2, out var value2) || value2 == null || !value2.IsEnabled)
				{
					continue;
				}
				if (!ShouldContinueStickyGuardrailRule(stickyGuardrailRuleState, value2, input, list.Count, secondaryInput))
				{
					continue;
				}
				stickyGuardrailRuleState.CarryTurnIndex = Math.Max(1, stickyGuardrailRuleState.MaxCarryTurns - stickyGuardrailRuleState.RemainingCarryTurns + 1);
				list2.Add(new StickyGuardrailRuleState
				{
					RuleId = text2,
					Group = stickyGuardrailRuleState.Group,
					Priority = stickyGuardrailRuleState.Priority,
					LastScore = stickyGuardrailRuleState.LastScore,
					MatchedSeed = stickyGuardrailRuleState.MatchedSeed,
					RemainingCarryTurns = stickyGuardrailRuleState.RemainingCarryTurns,
					MaxCarryTurns = stickyGuardrailRuleState.MaxCarryTurns,
					CarryTurnIndex = stickyGuardrailRuleState.CarryTurnIndex
				});
				stickyGuardrailRuleState.RemainingCarryTurns = Math.Max(0, stickyGuardrailRuleState.RemainingCarryTurns - 1);
				if (stickyGuardrailRuleState.RemainingCarryTurns > 0)
				{
					list3.Add(stickyGuardrailRuleState);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				GuardrailRuleHit guardrailRuleHit = list[j];
				if (!ShouldStartStickyGuardrailRule(input, secondaryInput, guardrailRuleHit, j, excluded))
				{
					continue;
				}
				string text3 = (guardrailRuleHit.RuleId ?? "").Trim();
				int stickyGuardrailTurnLimit = GetStickyGuardrailTurnLimit(text3);
				StickyGuardrailRuleState stickyGuardrailRuleState2 = list3.FirstOrDefault((StickyGuardrailRuleState x) => x != null && string.Equals(x.RuleId, text3, StringComparison.OrdinalIgnoreCase));
				if (stickyGuardrailRuleState2 == null)
				{
					list3.Add(new StickyGuardrailRuleState
					{
						RuleId = text3,
						Group = (guardrailRuleHit.Group ?? ""),
						Priority = guardrailRuleHit.Priority,
						LastScore = guardrailRuleHit.Score,
						MatchedSeed = (guardrailRuleHit.MatchedSeed ?? ""),
						RemainingCarryTurns = stickyGuardrailTurnLimit,
						MaxCarryTurns = stickyGuardrailTurnLimit
					});
					continue;
				}
				stickyGuardrailRuleState2.Group = (guardrailRuleHit.Group ?? stickyGuardrailRuleState2.Group);
				stickyGuardrailRuleState2.Priority = guardrailRuleHit.Priority;
				stickyGuardrailRuleState2.LastScore = guardrailRuleHit.Score;
				stickyGuardrailRuleState2.MatchedSeed = (guardrailRuleHit.MatchedSeed ?? stickyGuardrailRuleState2.MatchedSeed);
				stickyGuardrailRuleState2.RemainingCarryTurns = stickyGuardrailTurnLimit;
				stickyGuardrailRuleState2.MaxCarryTurns = stickyGuardrailTurnLimit;
			}
			list3 = list3.OrderByDescending((StickyGuardrailRuleState x) => x.Priority).ThenByDescending((StickyGuardrailRuleState x) => x.LastScore).ThenBy((StickyGuardrailRuleState x) => x.RuleId, StringComparer.OrdinalIgnoreCase).Take(MaxStickyGuardrailRulesPerTarget).ToList();
			if (list3.Count > 0)
			{
				_stickyGuardrailRules[text] = list3;
			}
			else
			{
				_stickyGuardrailRules.Remove(text);
			}
		}
		if (list2.Count > 0)
		{
			for (int k = 0; k < list2.Count; k++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState3 = list2[k];
				if (stickyGuardrailRuleState3 == null || hashSet.Contains(stickyGuardrailRuleState3.RuleId) || !IsRuleCurrentlyEligibleForRag(stickyGuardrailRuleState3.RuleId) || !dictionary.TryGetValue(stickyGuardrailRuleState3.RuleId, out var value3) || value3 == null)
				{
					continue;
				}
				list.Add(new GuardrailRuleHit
				{
					RuleId = stickyGuardrailRuleState3.RuleId,
					Group = stickyGuardrailRuleState3.Group,
					Priority = stickyGuardrailRuleState3.Priority,
					Score = ApplyStickyGuardrailScoreDecay(stickyGuardrailRuleState3.LastScore, stickyGuardrailRuleState3.MaxCarryTurns, stickyGuardrailRuleState3.CarryTurnIndex),
					MatchedSeed = (stickyGuardrailRuleState3.MatchedSeed ?? ""),
					Instruction = (value3.Instruction ?? "")
				});
			}
		}
		list = list.OrderByDescending((GuardrailRuleHit x) => x.Priority).ThenByDescending((GuardrailRuleHit x) => x.Score).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
		if (num > 0 && list.Count > num)
		{
			list = list.Take(num).ToList();
		}
		try
		{
			Logger.Log("GuardrailSemantic", $"sticky_rule_merge target={text} live={hashSet.Count} sticky={list2.Count} final={list.Count}");
		}
		catch
		{
		}
		return list;
	}

	private static string BuildExtraRuleHitDebugDetail(string input, string secondaryInput, GuardrailRuleHit hit, IEnumerable<string> excludedRuleIds = null)
	{
		try
		{
			if (hit == null)
			{
				return "";
			}
			string text = NormalizeSemanticText(input);
			string text2 = (hit.RuleId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && TryGetRuleEval(text, secondaryInput, text2, out var eval, excludedRuleIds) && eval != null)
			{
				string text3 = NormalizeSemanticText(eval.MatchedIntent);
				if (text3.Length > 48)
				{
					text3 = text3.Substring(0, 48);
				}
				string text4 = NormalizeSemanticText(eval.MatchedSeed);
				if (text4.Length > 32)
				{
					text4 = text4.Substring(0, 32);
				}
				return $" raw={eval.RawInput:0.000} ctx={eval.RawContext:0.000} mixed={eval.MixedRaw:0.000} rerank={eval.RerankScore:0.000} amp={eval.AmpScore:0.000} rank={eval.Rank} candidate={eval.Candidate} other={eval.MaxOtherTag}@{eval.MaxOther:0.000} mean={eval.Mean:0.000} reason={eval.RejectReason} lexicalAnchor={eval.LexicalAnchor} matchedSeed={JsonConvert.ToString(text4)} intent={JsonConvert.ToString(text3)}";
			}
			string text5 = NormalizeSemanticText(hit.MatchedSeed);
			if (text5.Length > 32)
			{
				text5 = text5.Substring(0, 32);
			}
			if (!string.IsNullOrWhiteSpace(text5))
			{
				return " source=lexical_fallback matchedSeed=" + JsonConvert.ToString(text5);
			}
			return " source=unknown";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules = 0)
	{
		return BuildMatchedExtraRuleInstructions(input, null, maxRules, hasAnyHero: true);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules, bool hasAnyHero)
	{
		return BuildMatchedExtraRuleInstructions(input, null, maxRules, hasAnyHero);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, string secondaryInput, int maxRules, bool hasAnyHero)
	{
		return BuildMatchedExtraRuleInstructions(input, secondaryInput, maxRules, hasAnyHero, null);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, string secondaryInput, int maxRules, bool hasAnyHero, IEnumerable<string> excludedRuleIds)
	{
		try
		{
			HashSet<string> excluded = BuildExcludedRuleIdSet(excludedRuleIds);
			List<GuardrailRuleHit> guardrailSemanticRuleHits = GetGuardrailSemanticRuleHits(input, secondaryInput, maxRules, includeBuiltInRules: false, excludedRuleIds: excluded);
			if (guardrailSemanticRuleHits == null || guardrailSemanticRuleHits.Count <= 0)
			{
				guardrailSemanticRuleHits = GetGuardrailLexicalRuleHits(input, secondaryInput, maxRules, includeBuiltInRules: false, excludedRuleIds: excluded);
			}
			guardrailSemanticRuleHits = MergeStickyGuardrailRuleHits(input, secondaryInput, guardrailSemanticRuleHits, maxRules, excluded);
			if (guardrailSemanticRuleHits == null || guardrailSemanticRuleHits.Count <= 0)
			{
				return "";
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = (hasAnyHero ? null : BuildRulePromptRegistry());
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < guardrailSemanticRuleHits.Count; i++)
			{
				GuardrailRuleHit guardrailRuleHit = guardrailSemanticRuleHits[i];
				if (guardrailRuleHit == null)
				{
					continue;
				}
				string text = (guardrailRuleHit.RuleId ?? "").Trim();
				if (string.Equals(text, "noble_deference", StringComparison.OrdinalIgnoreCase) && !IsNobleDeferenceRuntimeEligible(hasAnyHero))
				{
					continue;
				}
				string value = (guardrailRuleHit.Instruction ?? "").Trim();
				if (!hasAnyHero && dictionary != null)
				{
					dictionary.TryGetValue(text, out var value2);
					string text2 = (value2?.NonHeroInstruction ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						value = text2;
					}
				}
				if (hasAnyHero && ShouldExcludeRuntimeRuleForConversationTarget(text))
				{
					continue;
				}
				if ((hasAnyHero || IsPlayerKingdomRecruitmentModeActive()) && string.Equals(text, "kingdom_service", StringComparison.OrdinalIgnoreCase))
				{
					List<string> joinInstructions = new List<string>();
					if (!string.IsNullOrWhiteSpace(value))
					{
						joinInstructions.Add(value.Trim());
					}
					string runtimeKingdomServiceInstruction = BuildRuntimeKingdomServiceInstruction();
					if (!string.IsNullOrWhiteSpace(runtimeKingdomServiceInstruction))
					{
						joinInstructions.Add(runtimeKingdomServiceInstruction.Trim());
					}
					if (hasAnyHero)
					{
						string runtimeHeroJoinPartyInstruction = BuildRuntimeHeroJoinPartyInstructionForExternal();
						if (!string.IsNullOrWhiteSpace(runtimeHeroJoinPartyInstruction))
						{
							joinInstructions.Add(runtimeHeroJoinPartyInstruction.Trim());
						}
					}
					value = string.Join("\n", joinInstructions.Distinct(StringComparer.OrdinalIgnoreCase));
				}
				if (hasAnyHero && string.Equals(text, "kingdom_vassalage", StringComparison.OrdinalIgnoreCase))
				{
					string runtimeVassalageInstruction = VassalageBehavior.BuildRuntimeVassalageInstructionForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
					if (!string.IsNullOrWhiteSpace(runtimeVassalageInstruction))
					{
						value = runtimeVassalageInstruction;
					}
					VassalageDiagnosticLog.Event("preprocess.runtime_instruction", new Dictionary<string, object>
					{
						["ruleId"] = text,
						["hasAnyHero"] = hasAnyHero,
						["targetHero"] = VassalageDiagnosticLog.DescribeHero(ResolveConversationTargetHero()),
						["targetCharacterId"] = ResolveConversationTargetCharacter()?.StringId ?? "",
						["usedRuntimeInstruction"] = !string.IsNullOrWhiteSpace(runtimeVassalageInstruction),
						["instructionPreview"] = runtimeVassalageInstruction
					});
				}
				if (hasAnyHero && string.Equals(text, "diplomacy", StringComparison.OrdinalIgnoreCase))
				{
					string runtimeAnnexationInstruction = KingdomAnnexationBehavior.BuildRuntimeAnnexationInstructionForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
					if (!string.IsNullOrWhiteSpace(runtimeAnnexationInstruction))
					{
						value = string.Join("\n", new string[2] { value, runtimeAnnexationInstruction }.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
					}
				}
				if (hasAnyHero && string.Equals(text, "marriage", StringComparison.OrdinalIgnoreCase))
				{
					string runtimeMarriageInstruction = RomanceSystemBehavior.Instance?.BuildMarriageRuntimeInstruction(ResolveConversationTargetHero()) ?? "";
					if (!string.IsNullOrWhiteSpace(runtimeMarriageInstruction))
					{
						value = runtimeMarriageInstruction;
					}
				}
				if (hasAnyHero && string.Equals(text, "vanilla_issue", StringComparison.OrdinalIgnoreCase))
				{
					value = VanillaIssueOfferBridge.BuildRuntimePromptBlockForExternal(ResolveConversationTargetHero()) ?? "";
				}
				if (string.Equals(text, "meeting_taunt", StringComparison.OrdinalIgnoreCase))
				{
					string text4 = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter(), ResolveConversationTargetAgentIndex());
					if (!string.IsNullOrWhiteSpace(text4))
					{
						value = text4;
					}
				}
				if (hasAnyHero && string.Equals(text, "npc_major_actions", StringComparison.OrdinalIgnoreCase))
				{
					string text5 = MyBehavior.BuildNpcMajorActionsRuntimeInstructionForExternal(ResolveConversationTargetHero());
					if (!string.IsNullOrWhiteSpace(text5))
					{
						value = text5;
					}
				}
				if (string.Equals(text, "lords_hall_access", StringComparison.OrdinalIgnoreCase))
				{
					string text7 = BuildRuntimeLordsHallAccessInstruction();
					if (!string.IsNullOrWhiteSpace(text7))
					{
						value = text7;
					}
				}
				if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.AppendLine("【附加规则:" + text + "】");
					stringBuilder.AppendLine(value);
					string value3 = BuildRuntimeRuleConstraintHint(text);
					if (!string.IsNullOrWhiteSpace(value3))
					{
						stringBuilder.AppendLine(value3);
					}
					try
					{
						Logger.Log("GuardrailSemantic", $"extra_rule_hit rule={text} score={guardrailRuleHit.Score:0.000} group={guardrailRuleHit.Group} priority={guardrailRuleHit.Priority} nonHero={!hasAnyHero}{BuildExtraRuleHitDebugDetail(input, secondaryInput, guardrailRuleHit, excluded)}");
					}
					catch
					{
					}
				}
			}
			return ApplyPlayerDisplayNameToGuardrailText(stringBuilder.ToString().Trim());
		}
		catch
		{
			return "";
		}
	}

	private static string GetKingdomServiceRuntimeTemplate(string stateKey, bool forConstraint)
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary == null || !dictionary.TryGetValue("kingdom_service", out var value) || value == null)
			{
				return "";
			}
			Dictionary<string, string> dictionary2 = (forConstraint ? value.RuntimeConstraintTemplates : value.RuntimeInstructionTemplates);
			if (dictionary2 == null || dictionary2.Count <= 0)
			{
				return "";
			}
			string text = (stateKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text) && dictionary2.TryGetValue(text, out var value2) && !string.IsNullOrWhiteSpace(value2))
			{
				return value2;
			}
			if (dictionary2.TryGetValue("__default__", out var value3) && !string.IsNullOrWhiteSpace(value3))
			{
				return value3;
			}
			if (dictionary2.TryGetValue("default", out var value4) && !string.IsNullOrWhiteSpace(value4))
			{
				return value4;
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string GetRuleRuntimeTemplate(string ruleId, string stateKey, bool forConstraint)
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			string text = (ruleId ?? "").Trim().ToLowerInvariant();
			if (dictionary == null || string.IsNullOrWhiteSpace(text) || !dictionary.TryGetValue(text, out var value) || value == null)
			{
				return "";
			}
			Dictionary<string, string> dictionary2 = (forConstraint ? value.RuntimeConstraintTemplates : value.RuntimeInstructionTemplates);
			if (dictionary2 == null || dictionary2.Count <= 0)
			{
				return "";
			}
			string text2 = (stateKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2) && dictionary2.TryGetValue(text2, out var value2) && !string.IsNullOrWhiteSpace(value2))
			{
				return value2;
			}
			if (dictionary2.TryGetValue("__default__", out var value3) && !string.IsNullOrWhiteSpace(value3))
			{
				return value3;
			}
			if (dictionary2.TryGetValue("default", out var value4) && !string.IsNullOrWhiteSpace(value4))
			{
				return value4;
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	public static void SetGuardrailRuntimeTargetKingdom(string kingdomId)
	{
		try
		{
			_guardrailRuntimeTargetKingdomId.Value = ((kingdomId ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetKingdomId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetHero(string heroId)
	{
		try
		{
			_guardrailRuntimeTargetHeroId.Value = (heroId ?? "").Trim();
		}
		catch
		{
			_guardrailRuntimeTargetHeroId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetCharacter(string characterId)
	{
		try
		{
			_guardrailRuntimeTargetCharacterId.Value = (characterId ?? "").Trim();
		}
		catch
		{
			_guardrailRuntimeTargetCharacterId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetTroop(string troopId)
	{
		try
		{
			_guardrailRuntimeTargetTroopId.Value = ((troopId ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetTroopId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetUnnamedRank(string unnamedRank)
	{
		try
		{
			_guardrailRuntimeTargetUnnamedRank.Value = ((unnamedRank ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetUnnamedRank.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetAgentIndex(int agentIndex)
	{
		try
		{
			_guardrailRuntimeTargetAgentIndex.Value = agentIndex;
		}
		catch
		{
			_guardrailRuntimeTargetAgentIndex.Value = -1;
		}
	}

	internal static int GetGuardrailRuntimeTargetAgentIndexForExternal()
	{
		try
		{
			return _guardrailRuntimeTargetAgentIndex.Value;
		}
		catch
		{
			return -1;
		}
	}

	private static string ApplyRuntimeTemplate(string template, Dictionary<string, string> tokens)
	{
		string text = template ?? "";
		try
		{
			if (string.IsNullOrWhiteSpace(text) || tokens == null || tokens.Count <= 0)
			{
				return text;
			}
			foreach (KeyValuePair<string, string> token in tokens)
			{
				if (!string.IsNullOrWhiteSpace(token.Key))
				{
					text = text.Replace("{" + token.Key + "}", token.Value ?? "");
				}
			}
			return text;
		}
		catch
		{
			return template ?? "";
		}
	}

	private static string ApplyPlayerDisplayNameToGuardrailText(string text)
	{
		try
		{
			string text2 = text ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			string text3 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text3) || string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				return text2;
			}
			const string text4 = "__AFEF_PLAYER_FACT__";
			const string text5 = "__PLAYER_CLAN_FACT__";
			text2 = text2.Replace("[AFEF玩家行为补充]", text4);
			text2 = text2.Replace("【玩家家族可婚配成员（允许已有配偶，事实清单）】", text5);
			text2 = text2.Replace("玩家家族", "__PLAYER_CLAN__");
			text2 = text2.Replace("玩家", text3);
			text2 = text2.Replace("__PLAYER_CLAN__", "玩家家族");
			text2 = text2.Replace(text4, "[AFEF玩家行为补充]");
			text2 = text2.Replace(text5, "【玩家家族可婚配成员（允许已有配偶，事实清单）】");
			return text2;
		}
		catch
		{
			return text ?? "";
		}
	}

	private static string ResolveRewardNpcName(Hero targetHero, CharacterObject targetCharacter = null)
	{
		try
		{
			string text = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Replace("\r", "").Replace("\n", "").Trim();
			return string.IsNullOrWhiteSpace(text) ? "NPC" : text;
		}
		catch
		{
			return "NPC";
		}
	}

	private static string BuildRewardInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
			string text = ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Reward?.Instruction ?? "");
			string text2 = ApplyRuntimeTemplate(text, BuildRewardRuntimeTokens(hero, characterObject));
			string text3 = RewardSystemBehavior.Instance?.BuildNotableMarketRewardInstruction(hero) ?? "";
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = (text2.TrimEnd() + "\n" + text3.Trim()).Trim();
			}
			return text2;
		}
		catch
		{
			return ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Reward?.Instruction ?? "");
		}
	}

	private static int ResolveRuntimeTrustValue(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			if (targetHero != null)
			{
				return RewardSystemBehavior.Instance?.GetEffectiveTrust(targetHero) ?? 0;
			}
			if (targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
			{
				return RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(Settlement.CurrentSettlement, kind);
			}
		}
		catch
		{
		}
		return 0;
	}

	private static Dictionary<string, string> BuildLoanRuntimeTokens(Hero targetHero, CharacterObject targetCharacter = null)
	{
		int num = 0;
		int trustLevelIndex = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			trustLevelIndex = 6;
		}
		string text = trustLevelIndex switch
		{
			1 => "彻底不信", 
			2 => "极度怀疑", 
			3 => "强烈戒备", 
			4 => "不信任", 
			5 => "保留", 
			_ => "观望", 
		};
		string text2 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = text2,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = trustLevelIndex.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num),
			["trustAttitude"] = text
		};
	}

	public static string BuildRuntimeLoanInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
			if (IsPlayerPartyTradeLimitedTarget(hero))
			{
				return "";
			}
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(hero, characterObject));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(LoanRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildLoanRuntimeTokens(hero, characterObject));
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text.Trim();
				}
			}
		}
		catch
		{
		}
		return LoanInstruction;
	}

	private static Dictionary<string, string> BuildRewardRuntimeTokens(Hero targetHero, CharacterObject targetCharacter = null)
	{
		int num = 0;
		int trustLevelIndex = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			trustLevelIndex = 6;
		}
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = text,
			["npcName"] = ResolveRewardNpcName(targetHero, targetCharacter),
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = trustLevelIndex.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num)
		};
	}

	private static Dictionary<string, string> BuildPartyTransferRuntimeTokens(Hero targetHero, CharacterObject targetCharacter = null)
	{
		int num = 0;
		int trustLevelIndex = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			trustLevelIndex = 6;
		}
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = text,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = trustLevelIndex.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num)
		};
	}

	public static string BuildRuntimePartyTransferInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(hero, characterObject));
			}
			catch
			{
				num = 6;
			}
			string text = ResolveRuleRuntimeText("party_transfer", "level_" + num, forConstraint: false, BuildPartyTransferRuntimeTokens(hero, characterObject));
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return GetGuardrailRuleInstruction("party_transfer");
	}

	public static string BuildRuntimeHeroJoinPartyInstructionForExternal(Hero targetHero = null)
	{
		try
		{
			List<string> list = new List<string>();
			string baseInstruction = GetGuardrailRuleInstruction("kingdom_service");
			if (!string.IsNullOrWhiteSpace(baseInstruction))
			{
				list.Add(baseInstruction.Trim());
			}
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			string text = ResolveHeroJoinPartyRuntimeStateKey(hero);
			if (!string.IsNullOrWhiteSpace(text))
			{
				string text2 = ResolveRuleRuntimeText("kingdom_service", text, forConstraint: false, BuildHeroJoinPartyRuntimeTokens(hero));
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(text2.Trim());
				}
			}
			return string.Join("\n", list.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)).Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveHeroJoinPartyRuntimeStateKey(Hero targetHero)
	{
		if (targetHero == null || targetHero == Hero.MainHero)
		{
			return "";
		}
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan != null && targetHero.Clan == playerClan)
			{
				return "already_in_player_family";
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null && targetHero.PartyBelongedTo == MobileParty.MainParty)
			{
				return "already_in_player_party";
			}
		}
		catch
		{
		}
		try
		{
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			Kingdom playerKingdom = playerClan?.Kingdom;
			if (playerKingdom != null && (targetHero.Clan?.Kingdom == playerKingdom || targetHero.MapFaction == playerKingdom))
			{
				return "already_in_player_kingdom";
			}
		}
		catch
		{
		}
		return "";
	}

	private static Dictionary<string, string> BuildHeroJoinPartyRuntimeTokens(Hero targetHero)
	{
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		string npcName = "";
		try
		{
			npcName = (targetHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
		}
		string playerKingdomName = "";
		try
		{
			playerKingdomName = ((Clan.PlayerClan ?? Hero.MainHero?.Clan)?.Kingdom?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = playerName,
			["npcName"] = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName,
			["playerKingdom"] = playerKingdomName
		};
	}

	public static string BuildRuntimeRewardInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		Hero hero = targetHero ?? ResolveConversationTargetHero();
		CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
		try
		{
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(hero, characterObject));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(RewardRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildRewardRuntimeTokens(hero, characterObject));
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (num <= 1)
					{
						return AppendFixedAssetRuntimeInstruction(text, hero, characterObject);
					}
					string text2 = BuildRewardInstructionForExternal(hero, characterObject).Trim();
					return AppendFixedAssetRuntimeInstruction(string.IsNullOrWhiteSpace(text2) ? text.Trim() : (text.Trim() + "\n" + text2), hero, characterObject);
				}
			}
		}
		catch
		{
		}
		return AppendFixedAssetRuntimeInstruction(BuildRewardInstructionForExternal(hero, characterObject), hero, characterObject);
	}

	private static string AppendFixedAssetRuntimeInstruction(string current, Hero targetHero, CharacterObject targetCharacter)
	{
		string text = (current ?? "").Trim();
		if (targetHero == null)
		{
			return text;
		}
		try
		{
			string fixedAssetInstruction = MyBehavior.BuildSettlementTransferRuntimeInstructionForExternal(targetHero, targetCharacter);
			if (!string.IsNullOrWhiteSpace(fixedAssetInstruction))
			{
				return string.IsNullOrWhiteSpace(text) ? fixedAssetInstruction.Trim() : (text + "\n" + fixedAssetInstruction.Trim());
			}
		}
		catch
		{
		}
		return text;
	}

	private static string ResolveKingdomServiceRuntimeText(string stateKey, bool forConstraint, Dictionary<string, string> tokens)
	{
		try
		{
			string kingdomServiceRuntimeTemplate = GetKingdomServiceRuntimeTemplate(stateKey, forConstraint);
			if (!string.IsNullOrWhiteSpace(kingdomServiceRuntimeTemplate))
			{
				string text = ApplyRuntimeTemplate(kingdomServiceRuntimeTemplate, tokens);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static string ResolveRuleRuntimeText(string ruleId, string stateKey, bool forConstraint, Dictionary<string, string> tokens)
	{
		try
		{
			string ruleRuntimeTemplate = GetRuleRuntimeTemplate(ruleId, stateKey, forConstraint);
			if (!string.IsNullOrWhiteSpace(ruleRuntimeTemplate))
			{
				string text = ApplyRuntimeTemplate(ruleRuntimeTemplate, tokens);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsPlayerKingdomRecruitmentModeActive()
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = playerClan?.Kingdom;
			return playerClan != null && kingdom != null && kingdom.RulingClan == playerClan;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerKingdomRecruitmentModeActive(Clan playerClan, Kingdom playerKingdom)
	{
		try
		{
			return playerClan != null && playerKingdom != null && playerKingdom.RulingClan == playerClan;
		}
		catch
		{
			return false;
		}
	}

	private static string GetClanDisplayNameForPrompt(Clan clan)
	{
		try
		{
			string text = clan?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return clan?.StringId ?? "";
	}

	private static Clan ResolveConversationTargetClan()
	{
		try
		{
			Hero hero = ResolveConversationTargetHero();
			if (hero?.Clan != null)
			{
				return hero.Clan;
			}
		}
		catch
		{
		}
		try
		{
			Hero heroObject = ResolveConversationTargetCharacter()?.HeroObject;
			if (heroObject?.Clan != null)
			{
				return heroObject.Clan;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string ResolvePlayerKingdomRecruitmentStateKey(Clan playerClan, Kingdom playerKingdom, Clan targetClan, Hero targetHero)
	{
		if (!IsPlayerKingdomRecruitmentModeActive(playerClan, playerKingdom))
		{
			return "";
		}
		if (targetClan == null)
		{
			return "player_ruler_target_unknown";
		}
		if (targetClan == playerClan)
		{
			return "player_ruler_target_player_clan";
		}
		try
		{
			if (targetClan.IsEliminated)
			{
				return "player_ruler_target_eliminated";
			}
		}
		catch
		{
		}
		if (targetClan.Kingdom == playerKingdom)
		{
			return "player_ruler_target_same_kingdom";
		}
		if (targetHero == null || targetHero.Clan != targetClan || targetClan.Leader != targetHero)
		{
			return "player_ruler_target_not_leader";
		}
		return "player_ruler_target_ready";
	}

	private static Kingdom ResolveConversationTargetKingdom()
	{
		try
		{
			string text = (_guardrailRuntimeTargetKingdomId.Value ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Kingdom kingdom = Kingdom.All?.FirstOrDefault((Kingdom k) => k != null && string.Equals((k.StringId ?? "").Trim().ToLowerInvariant(), text, StringComparison.OrdinalIgnoreCase));
				if (kingdom != null)
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (_guardrailRuntimeTargetHeroId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				Hero hero = Hero.Find(text2) ?? Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
				Kingdom kingdom = hero?.Clan?.Kingdom;
				if (kingdom != null)
				{
					return kingdom;
				}
				Kingdom kingdom2 = hero?.MapFaction as Kingdom;
				if (kingdom2 != null)
				{
					return kingdom2;
				}
			}
		}
		catch
		{
		}
		try
		{
			string text3 = (_guardrailRuntimeTargetCharacterId.Value ?? _guardrailRuntimeTargetTroopId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				CharacterObject characterObject = CharacterObject.All?.FirstOrDefault((CharacterObject x) => x != null && string.Equals((x.StringId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
				Hero heroObject = characterObject?.HeroObject;
				Kingdom kingdom3 = heroObject?.Clan?.Kingdom;
				if (kingdom3 != null)
				{
					return kingdom3;
				}
				Kingdom kingdom4 = heroObject?.MapFaction as Kingdom;
				if (kingdom4 != null)
				{
					return kingdom4;
				}
			}
		}
		catch
		{
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Kingdom kingdom5 = oneToOneConversationHero?.Clan?.Kingdom;
			if (kingdom5 != null)
			{
				return kingdom5;
			}
			Kingdom kingdom6 = oneToOneConversationHero?.MapFaction as Kingdom;
			if (kingdom6 != null)
			{
				return kingdom6;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			Hero heroObject = oneToOneConversationCharacter?.HeroObject;
			Kingdom kingdom7 = heroObject?.Clan?.Kingdom;
			if (kingdom7 != null)
			{
				return kingdom7;
			}
			Kingdom kingdom8 = heroObject?.MapFaction as Kingdom;
			if (kingdom8 != null)
			{
				return kingdom8;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Hero ResolveConversationTargetHero()
	{
		try
		{
			string text = (_guardrailRuntimeTargetHeroId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Hero hero = Hero.Find(text);
				if (hero != null)
				{
					return hero;
				}
				Hero hero2 = Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (hero2 != null)
				{
					return hero2;
				}
			}
		}
		catch
		{
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				return oneToOneConversationHero;
			}
		}
		catch
		{
		}
		try
		{
			Hero heroObject = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter?.HeroObject;
			if (heroObject != null)
			{
				return heroObject;
			}
		}
		catch
		{
		}
		return null;
	}

	private static CharacterObject ResolveConversationTargetCharacter()
	{
		try
		{
			string text = (_guardrailRuntimeTargetCharacterId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				CharacterObject characterObject = CharacterObject.All?.FirstOrDefault((CharacterObject x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (characterObject != null)
				{
					return characterObject;
				}
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter != null)
			{
				return oneToOneConversationCharacter;
			}
		}
		catch
		{
		}
		return null;
	}

	private static int ResolveConversationTargetAgentIndex()
	{
		try
		{
			return _guardrailRuntimeTargetAgentIndex.Value;
		}
		catch
		{
			return -1;
		}
	}

	private static string ResolveRuntimeTargetUnnamedRank()
	{
		try
		{
			return (_guardrailRuntimeTargetUnnamedRank.Value ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveRuntimeTargetTroopId()
	{
		try
		{
			return (_guardrailRuntimeTargetTroopId.Value ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildRuntimeKingdomServiceInstruction()
	{
		try
		{
			Dictionary<string, string> dictionary = BuildKingdomServiceRuntimeTokens(out var playerClan, out var kingdom, out var flag, out var kingdom2, out var flag2, out var num, out var num2, out var num3, out var num4, out var num5, out var num6);
			if (playerClan == null)
			{
				return "";
			}
			if (IsPlayerKingdomRecruitmentModeActive(playerClan, kingdom))
			{
				string text2 = ResolvePlayerKingdomRecruitmentStateKey(playerClan, kingdom, ResolveConversationTargetClan(), ResolveConversationTargetHero());
				if (string.IsNullOrWhiteSpace(text2))
				{
					return "";
				}
				return ResolveKingdomServiceRuntimeText(text2, forConstraint: false, dictionary);
			}
			string text = ResolveKingdomServiceRuntimeStateKey(kingdom, flag, kingdom2, flag2, num, num2, num3, num4, num5, num6);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			return ResolveKingdomServiceRuntimeText(text, forConstraint: false, dictionary);
		}
		catch
		{
			return "";
		}
	}

	public static string BuildRuntimeKingdomServiceInstructionForExternal()
	{
		try
		{
			return BuildRuntimeKingdomServiceInstruction();
		}
		catch
		{
			return "";
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimeKingdomServicePostprocessRules()
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			Dictionary<string, string> dictionary = BuildKingdomServiceRuntimeTokens(out var playerClan, out var kingdom, out var flag, out var kingdom2, out var flag2, out var num, out var num2, out var num3, out var num4, out var num5, out var num6);
			if (playerClan == null)
			{
				Logger.Log("AIConfig", "[KingdomServicePostprocessRules] playerClan=null targetKingdomId=" + ((dictionary != null && dictionary.TryGetValue("targetKingdomId", out var value0)) ? (value0 ?? "") : "") + " playerTier=" + num + " mercTier=" + num2 + " vassalTier=" + num3 + " trustCurrent=" + num6 + " trustMerc=" + num4 + " trustVassal=" + num5);
				return list;
			}
			if (IsPlayerKingdomRecruitmentModeActive(playerClan, kingdom))
			{
				Clan clan = ResolveConversationTargetClan();
				Hero hero = ResolveConversationTargetHero();
				string text4 = ResolvePlayerKingdomRecruitmentStateKey(playerClan, kingdom, clan, hero);
				string text5 = "";
				if (dictionary != null && dictionary.TryGetValue("targetClanId", out var value1))
				{
					text5 = (value1 ?? "").Trim();
				}
				Logger.Log("AIConfig", "[KingdomServicePostprocessRules] player_ruler state=" + text4 + " playerClan=" + (playerClan?.StringId ?? "") + " playerKingdom=" + (kingdom?.StringId ?? "") + " targetClan=" + (clan?.StringId ?? "") + " targetHero=" + (hero?.StringId ?? "") + " targetClanIdToken=" + text5 + " rules=（无，C_J_K已迁移到NPC_JOIN）");
				return list;
			}
			string text = ResolveRuntimeKingdomServiceStateKeyForPostprocess(kingdom, flag, kingdom2, flag2, num, num2, num3, num4, num5, num6);
			if (string.IsNullOrWhiteSpace(text))
			{
				Logger.Log("AIConfig", "[KingdomServicePostprocessRules] empty_state playerClan=" + (playerClan?.StringId ?? "") + " playerKingdom=" + (kingdom?.StringId ?? "") + " targetKingdom=" + (kingdom2?.StringId ?? "") + " isMercenaryService=" + flag + " isSameKingdom=" + flag2 + " playerTier=" + num + " mercTier=" + num2 + " vassalTier=" + num3 + " trustCurrent=" + num6 + " trustMerc=" + num4 + " trustVassal=" + num5);
				return list;
			}
			string text2 = "";
			if (dictionary != null && dictionary.TryGetValue("targetKingdomId", out var value))
			{
				text2 = (value ?? "").Trim();
			}
			Hero targetHero = ResolveConversationTargetHero();
			bool canInjectLeaveCurrent = CanInjectKingdomServiceLeaveCurrentPostprocessTag(kingdom, flag, kingdom2, targetHero);
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in GetGuardrailRulePostprocessRules("kingdom_service"))
			{
				string text3 = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				string description = guardrailRulePostprocessRule?.Description ?? "";
				if (string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				if (!IsPlayerJoinKingdomServicePostprocessTag(text3))
				{
					continue;
				}
				if (!ShouldIncludeKingdomServicePostprocessTag(text, text3, canInjectLeaveCurrent))
				{
					continue;
				}
				if (text3.IndexOf("{targetKingdomId}", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					if (string.IsNullOrWhiteSpace(text2))
					{
						continue;
					}
					text3 = text3.Replace("{targetKingdomId}", text2);
					description = description.Replace("{targetKingdomId}", text2);
				}
				list.Add(new PostprocessRuleEntry
				{
					Tag = text3,
					Description = description
				});
			}
			Logger.Log("AIConfig", "[KingdomServicePostprocessRules] state=" + text + " playerClan=" + (playerClan?.StringId ?? "") + " playerKingdom=" + (kingdom?.StringId ?? "") + " targetKingdom=" + (kingdom2?.StringId ?? "") + " targetKingdomIdToken=" + text2 + " isMercenaryService=" + flag + " isSameKingdom=" + flag2 + " playerTier=" + num + " mercTier=" + num2 + " vassalTier=" + num3 + " trustCurrent=" + num6 + " trustMerc=" + num4 + " trustVassal=" + num5 + " rules=" + ((list.Count == 0) ? "（无）" : string.Join(",", list.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		catch
		{
		}
		return list;
	}

	public static List<PostprocessRuleEntry> BuildRuntimeHeroJoinPartyPostprocessRules(bool includePersonalJoinRule = true, Hero targetHero = null, string entityPostprocessContext = null)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			bool suppressPlayerPartyTarget = IsPlayerPartyTradeLimitedTarget(hero) && IsPlayerClanLordTarget(hero);
			if (suppressPlayerPartyTarget)
			{
				includePersonalJoinRule = false;
			}
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in GetGuardrailRulePostprocessRules("kingdom_service"))
			{
				string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || !IsPersonalHeroJoinPartyPostprocessTag(text))
				{
					continue;
				}
				if (!includePersonalJoinRule)
				{
					continue;
				}
				if (list.Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}
				list.Add(new PostprocessRuleEntry
				{
					Tag = text,
					Description = guardrailRulePostprocessRule?.Description ?? ""
				});
			}
			foreach (PostprocessRuleEntry runtimeClanJoinRule in BuildRuntimeClanJoinKingdomPostprocessRules("kingdom_service", "HeroJoinPartyPostprocessRules", hero, entityPostprocessContext))
			{
				string text2 = (runtimeClanJoinRule?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (list.Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}
				list.Add(runtimeClanJoinRule);
			}
			Logger.Log("AIConfig", "[HeroJoinPartyPostprocessRules] targetHero=" + (hero?.StringId ?? "") + " suppressPlayerPartyTarget=" + suppressPlayerPartyTarget + " includePersonalJoin=" + includePersonalJoinRule + " rules=" + ((list.Count == 0) ? "（无）" : string.Join(",", list.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		catch
		{
		}
		return list;
	}

	private static List<PostprocessRuleEntry> BuildRuntimeClanJoinKingdomPostprocessRules(string sourceRuleId, string logPrefix, Hero targetHero, string entityPostprocessContext)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			if (IsPlayerPartyTradeLimitedTarget(hero))
			{
				Logger.Log("AIConfig", "[" + logPrefix + "] clan_join skipped_player_party_target targetHero=" + (hero?.StringId ?? ""));
				return list;
			}
			Clan clan = hero?.Clan ?? ResolveConversationTargetClan();
			string state = ResolveClanJoinKingdomStateKey(clan, hero);
			if (!string.Equals(state, "clan_join_target_ready", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("AIConfig", "[" + logPrefix + "] clan_join blocked_state=" + state + " targetClan=" + (clan?.StringId ?? "") + " targetHero=" + (hero?.StringId ?? "") + " rules=（无）");
				return list;
			}
			List<Kingdom> retrievedKingdoms = ResolveRetrievedKingdomsForClanJoin(entityPostprocessContext);
			List<Kingdom> eligibleKingdoms = retrievedKingdoms
				.Where((Kingdom x) => x != null && x != clan?.Kingdom && !string.IsNullOrWhiteSpace((x.StringId ?? "").Trim()))
				.ToList();
			if (eligibleKingdoms.Count == 0)
			{
				Logger.Log("AIConfig", "[" + logPrefix + "] clan_join no_retrieved_kingdom targetClan=" + (clan?.StringId ?? "") + " targetHero=" + (hero?.StringId ?? "") + " rules=（无）");
				return list;
			}
			HashSet<string> allowedKingdomIds = new HashSet<string>(eligibleKingdoms.Select((Kingdom x) => (x.StringId ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in GetGuardrailRulePostprocessRules(sourceRuleId))
			{
				string tagTemplate = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				string descriptionTemplate = guardrailRulePostprocessRule?.Description ?? "";
				if (string.IsNullOrWhiteSpace(tagTemplate) || !IsClanJoinKingdomServicePostprocessTag(tagTemplate))
				{
					continue;
				}
				if (tagTemplate.IndexOf("{targetKingdomId}", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				if (list.Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), tagTemplate, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}
				list.Add(new PostprocessRuleEntry
				{
					Tag = tagTemplate,
					Description = descriptionTemplate,
					RuntimeAllowedParameterValues = new HashSet<string>(allowedKingdomIds, StringComparer.OrdinalIgnoreCase)
				});
			}
			Logger.Log("AIConfig", "[" + logPrefix + "] clan_join state=" + state + " targetClan=" + (clan?.StringId ?? "") + " targetHero=" + (hero?.StringId ?? "") + " allowedKingdoms=" + string.Join(",", allowedKingdomIds) + " rules=" + ((list.Count == 0) ? "（无）" : string.Join(",", list.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		catch
		{
		}
		return list;
	}

	private static string ResolveClanJoinKingdomStateKey(Clan targetClan, Hero targetHero)
	{
		if (targetClan == null)
		{
			return "clan_join_target_unknown";
		}
		if (targetClan == Clan.PlayerClan)
		{
			return "clan_join_target_player_clan";
		}
		try
		{
			if (targetClan.IsEliminated)
			{
				return "clan_join_target_eliminated";
			}
		}
		catch
		{
			return "clan_join_target_invalid";
		}
		if (targetHero == null || targetHero.Clan != targetClan || targetClan.Leader != targetHero)
		{
			return "clan_join_target_not_leader";
		}
		return "clan_join_target_ready";
	}

	private static List<Kingdom> ResolveRetrievedKingdomsForClanJoin(string entityPostprocessContext)
	{
		List<Kingdom> list = new List<Kingdom>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string context = entityPostprocessContext ?? "";
			List<string> kingdomIds = new List<string>();
			foreach (Match match in Regex.Matches(context, "(?<![A-Za-z0-9_.\\-])kingdom:([A-Za-z0-9_.\\-]+)", RegexOptions.IgnoreCase))
			{
				string kingdomId = (match?.Groups[1].Value ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(kingdomId))
				{
					kingdomIds.Add(kingdomId);
				}
			}
			foreach (Match sectionMatch in Regex.Matches(context, "【王国】(?<body>[\\s\\S]*?)(?=\\r?\\n【|\\z)", RegexOptions.IgnoreCase))
			{
				string body = sectionMatch?.Groups["body"].Value ?? "";
				foreach (Match idMatch in Regex.Matches(body, "(?:ID|编号)[:：]\\s*(?:kingdom:)?([A-Za-z0-9_.\\-]+)", RegexOptions.IgnoreCase))
				{
					string kingdomId = (idMatch?.Groups[1].Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(kingdomId))
					{
						kingdomIds.Add(kingdomId);
					}
				}
			}
			foreach (string kingdomId in kingdomIds)
			{
				if (!seen.Add(kingdomId))
				{
					continue;
				}
				Kingdom kingdom = Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), kingdomId, StringComparison.OrdinalIgnoreCase));
				if (kingdom != null && !kingdom.IsEliminated)
				{
					list.Add(kingdom);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool IsClanJoinKingdomServicePostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		return text.StartsWith("[A:C_J_K:", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:C_J_P_K]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ACTION:KINGDOM_SERVICE:CLAN_JOIN_PLAYER_KINGDOM:", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPersonalHeroJoinPartyPostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		return text.Equals("[A:H_J_P_P_C/L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_C]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[A:H_J_P_P_L]", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPlayerJoinKingdomServicePostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		if (IsClanJoinKingdomServicePostprocessTag(text))
		{
			return false;
		}
		return IsKingdomServiceMercenaryPostprocessTag(text)
			|| IsKingdomServiceVassalPostprocessTag(text)
			|| IsKingdomServiceLeaveCurrentPostprocessTag(text);
	}

	private static bool IsKingdomServiceMercenaryPostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		return text.Equals("[A:P_J_K_M]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ACTION:KINGDOM_SERVICE:MERCENARY:", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsKingdomServiceVassalPostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		return text.Equals("[A:P_J_K_V]", StringComparison.OrdinalIgnoreCase)
			|| text.StartsWith("[ACTION:KINGDOM_SERVICE:VASSAL:", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsKingdomServiceLeaveCurrentPostprocessTag(string tag)
	{
		string text = (tag ?? "").Trim();
		return text.Equals("[A:P_L_K]", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("[ACTION:KINGDOM_SERVICE:LEAVE:current]", StringComparison.OrdinalIgnoreCase);
	}

	private static bool CanInjectKingdomServiceLeaveCurrentPostprocessTag(Kingdom playerKingdom, bool isMercenaryService, Kingdom targetKingdom, Hero targetHero)
	{
		try
		{
			if (playerKingdom == null || targetKingdom == null || targetHero == null || targetHero == Hero.MainHero)
			{
				return false;
			}
			if (playerKingdom != targetKingdom)
			{
				return false;
			}
			Clan targetClan = targetHero.Clan;
			if (targetClan == null || targetClan.IsEliminated || targetClan.Kingdom != playerKingdom || targetClan.IsUnderMercenaryService)
			{
				return false;
			}
			bool isTargetRuler = targetKingdom.Leader == targetHero || targetKingdom.RulingClan?.Leader == targetHero || targetHero.IsFactionLeader;
			bool isTargetLord = targetHero.IsLord || targetClan.Leader == targetHero;
			return isMercenaryService ? (isTargetRuler || isTargetLord) : isTargetRuler;
		}
		catch
		{
			return false;
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimeLordsHallAccessPostprocessRules()
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			if (!TryBuildLordsHallAccessRuntimeState(out var stateKey, out var _))
			{
				return list;
			}
			string text = (stateKey ?? "").Trim().ToLowerInvariant();
			if (text != "allowed_directly" && text != "denied_but_bribe_available")
			{
				return list;
			}
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in GetGuardrailRulePostprocessRules("lords_hall_access"))
			{
				if (!string.IsNullOrWhiteSpace(guardrailRulePostprocessRule?.Tag))
				{
					list.Add(new PostprocessRuleEntry
					{
						Tag = guardrailRulePostprocessRule.Tag,
						Description = guardrailRulePostprocessRule.Description
					});
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string ResolveRuntimeKingdomServiceStateKeyForPostprocess(Kingdom playerKingdom, bool isMercenaryService, Kingdom targetKingdom, bool isSameKingdom, int playerTier, int mercTier, int vassalTier, int mercTrustMin, int vassalTrustMin, int currentTrust)
	{
		string text = ResolveKingdomServiceRuntimeStateKey(playerKingdom, isMercenaryService, targetKingdom, isSameKingdom, playerTier, mercTier, vassalTier, mercTrustMin, vassalTrustMin, currentTrust);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		switch (text.Trim().ToLowerInvariant())
		{
		case "no_kingdom":
		case "no_kingdom_tier_full":
			return "merc_or_vassal";
		case "no_kingdom_tier_below_merc":
		case "no_kingdom_tier_merc_only":
			return "merc_only";
		case "mercenary_same_kingdom":
		case "mercenary_same_kingdom_tier_vassal_locked":
		case "vassal_same_kingdom":
			return "leave_only";
		case "mercenary_same_kingdom_tier_vassal_ready":
			return "leave_or_vassal";
		case "mercenary_target_unknown":
		case "mercenary_other_kingdom":
		case "vassal_target_unknown":
		case "vassal_other_kingdom":
			return "blocked";
		default:
			return "blocked";
		}
	}

	private static bool ShouldIncludeKingdomServicePostprocessTag(string stateKey, string tag, bool canInjectLeaveCurrent)
	{
		string text = (tag ?? "").Trim();
		switch ((stateKey ?? "").Trim().ToLowerInvariant())
		{
		case "merc_only":
			return IsKingdomServiceMercenaryPostprocessTag(text);
		case "merc_or_vassal":
			return IsKingdomServiceMercenaryPostprocessTag(text) || IsKingdomServiceVassalPostprocessTag(text);
		case "leave_or_vassal":
			return (IsKingdomServiceLeaveCurrentPostprocessTag(text) && canInjectLeaveCurrent) || IsKingdomServiceVassalPostprocessTag(text);
		case "leave_only":
			return IsKingdomServiceLeaveCurrentPostprocessTag(text) && canInjectLeaveCurrent;
		case "player_ruler_target_ready":
			return IsClanJoinKingdomServicePostprocessTag(text);
		default:
			return false;
		}
	}

	private static string ResolveKingdomServiceRuntimeStateKey(Kingdom playerKingdom, bool isMercenaryService, Kingdom targetKingdom, bool isSameKingdom, int playerTier, int mercTier, int vassalTier, int mercTrustMin, int vassalTrustMin, int currentTrust)
	{
		if (playerKingdom == null)
		{
			return "no_kingdom";
		}
		if (isMercenaryService)
		{
			if (targetKingdom == null)
			{
				return "mercenary_target_unknown";
			}
			if (isSameKingdom)
			{
				return "mercenary_same_kingdom_tier_vassal_ready";
			}
			return "mercenary_other_kingdom";
		}
		if (targetKingdom == null)
		{
			return "vassal_target_unknown";
		}
		if (isSameKingdom)
		{
			return "vassal_same_kingdom";
		}
		return "vassal_other_kingdom";
	}

	private static bool ShouldAppendKingdomServiceConstraint(string stateKey)
	{
		switch ((stateKey ?? "").Trim().ToLowerInvariant())
		{
		case "no_player_clan":
		case "no_kingdom_tier_below_merc":
		case "no_kingdom_tier_merc_only":
		case "no_kingdom_tier_full":
			return true;
		default:
			return false;
		}
	}

	private static Dictionary<string, string> BuildKingdomServiceRuntimeTokens(out Clan playerClan, out Kingdom playerKingdom, out bool isMercenaryService, out Kingdom targetKingdom, out bool isSameKingdom, out int playerTier, out int mercTier, out int vassalTier, out int mercTrustMin, out int vassalTrustMin, out int currentTrust)
	{
		playerClan = Clan.PlayerClan;
		playerKingdom = playerClan?.Kingdom;
		isMercenaryService = playerClan?.IsUnderMercenaryService == true;
		targetKingdom = ResolveConversationTargetKingdom();
		isSameKingdom = playerKingdom != null && targetKingdom != null && playerKingdom == targetKingdom;
		Clan clan = ResolveConversationTargetClan();
		Hero hero = ResolveConversationTargetHero();
		playerTier = playerClan?.Tier ?? 0;
		mercTier = 1;
		vassalTier = 2;
		mercTrustMin = 0;
		vassalTrustMin = 0;
		currentTrust = 0;
		try
		{
			mercTier = Campaign.Current?.Models?.ClanTierModel?.MercenaryEligibleTier ?? 1;
		}
		catch
		{
			mercTier = 1;
		}
		try
		{
			vassalTier = Campaign.Current?.Models?.ClanTierModel?.VassalEligibleTier ?? 2;
		}
		catch
		{
			vassalTier = 2;
		}
		if (hero == null)
		{
			hero = targetKingdom?.Leader;
		}
		if (hero == null)
		{
			hero = playerKingdom?.Leader;
		}
		try
		{
			currentTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
		}
		catch
		{
			currentTrust = 0;
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerKingdom"] = (playerKingdom?.Name?.ToString() ?? ""),
			["playerKingdomId"] = (playerKingdom?.StringId ?? ""),
			["targetKingdom"] = (targetKingdom?.Name?.ToString() ?? ""),
			["targetKingdomId"] = (targetKingdom?.StringId ?? ""),
			["targetClan"] = GetClanDisplayNameForPrompt(clan),
			["targetClanId"] = (clan?.StringId ?? ""),
			["targetClanLeader"] = (clan?.Leader?.Name?.ToString() ?? ""),
			["playerTier"] = playerTier.ToString(),
			["mercTier"] = mercTier.ToString(),
			["vassalTier"] = vassalTier.ToString(),
			["trustMerc"] = mercTrustMin.ToString(),
			["trustVassal"] = vassalTrustMin.ToString(),
			["trustCurrent"] = currentTrust.ToString(),
			["trustMercGap"] = Math.Max(0, mercTrustMin - currentTrust).ToString(),
			["trustVassalGap"] = Math.Max(0, vassalTrustMin - currentTrust).ToString()
		};
	}

	private static bool IsRuntimeTargetCastleGuard(int agentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (agentIndex < 0 || agents == null)
			{
				return false;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			if (agent == null || !(agent.Character is CharacterObject characterObject) || !characterObject.IsSoldier)
			{
				return false;
			}
			AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			bool flag = false;
			if (agentNavigator != null)
			{
				flag = agentNavigator.TargetUsableMachine != null && agent.IsUsingGameObject && agentNavigator.TargetUsableMachine.GameEntity.HasTag("sp_guard_castle");
				if (!flag && (agentNavigator.SpecialTargetTag == "sp_guard_castle" || agentNavigator.SpecialTargetTag == "sp_guard"))
				{
					Location lordsHallLocation = LocationComplex.Current?.GetLocationWithId("lordshall");
					MissionAgentHandler missionBehavior = mission.GetMissionBehavior<MissionAgentHandler>();
					if (lordsHallLocation != null && missionBehavior?.TownPassageProps != null)
					{
						UsableMachine usableMachine = missionBehavior.TownPassageProps.FirstOrDefault((UsableMachine x) => x is Passage passage && passage.ToLocation == lordsHallLocation);
						if (usableMachine != null && usableMachine.GameEntity.GlobalPosition.DistanceSquared(agent.Position) < 100f)
						{
							flag = true;
						}
					}
				}
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	private static string DescribeLordsHallAccessReason(SettlementAccessModel.AccessDetails accessDetails)
	{
		try
		{
			switch (accessDetails.AccessLimitationReason)
			{
			case SettlementAccessModel.AccessLimitationReason.ClanTier:
				return "玩家家族等级不足";
			case SettlementAccessModel.AccessLimitationReason.CrimeRating:
				return "玩家在当地有犯罪评级";
			case SettlementAccessModel.AccessLimitationReason.Disguised:
				return "当前只能靠伪装混入";
			case SettlementAccessModel.AccessLimitationReason.LocationEmpty:
				return "领主大厅当前无人可见";
			default:
				return (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess) ? "原生规则允许直接进入" : "原生规则不允许直接进入";
			}
		}
		catch
		{
			return "";
		}
	}

	private static bool TryBuildLordsHallAccessRuntimeState(out string stateKey, out Dictionary<string, string> tokens)
	{
		stateKey = "not_applicable";
		tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			Settlement settlement = Settlement.CurrentSettlement;
			int agentIndex = ResolveConversationTargetAgentIndex();
			string text = ResolveRuntimeTargetUnnamedRank();
			string text2 = ResolveRuntimeTargetTroopId();
			int num = 0;
			int num2 = Hero.MainHero?.Gold ?? 0;
			bool flag = !string.IsNullOrWhiteSpace(text2) && string.Equals(text, "soldier", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace((_guardrailRuntimeTargetHeroId.Value ?? "").Trim());
			bool flag2 = flag && IsRuntimeTargetCastleGuard(agentIndex);
			string text3 = MyBehavior.BuildRuleTargetKeyForExternal(null, null, agentIndex);
			if (string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text2))
			{
				text3 = "troop:" + text2;
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				try
				{
					num = ShoutBehavior.CurrentInstance?.GetRecentNonHeroGoldForRuleTarget(text3) ?? 0;
				}
				catch
				{
					num = 0;
				}
			}
			tokens["settlementName"] = settlement?.Name?.ToString() ?? "";
			tokens["settlementId"] = settlement?.StringId ?? "";
			tokens["troopId"] = text2 ?? "";
			tokens["targetRank"] = text ?? "";
			tokens["targetKey"] = text3 ?? "";
			tokens["prepaidGold"] = num.ToString();
			tokens["playerGold"] = num2.ToString();
			tokens["playerClanTier"] = ((Clan.PlayerClan?.Tier).GetValueOrDefault()).ToString();
			tokens["accessReason"] = "";
			tokens["guideBribeGold"] = "0";
			if (settlement == null || !settlement.IsTown || !flag2)
			{
				stateKey = "not_applicable";
				return true;
			}
			SettlementAccessModel settlementAccessModel = Campaign.Current?.Models?.SettlementAccessModel;
			if (settlementAccessModel == null)
			{
				stateKey = "denied_no_bribe";
				return true;
			}
			SettlementAccessModel.AccessDetails accessDetails = default(SettlementAccessModel.AccessDetails);
			settlementAccessModel.CanMainHeroEnterLordsHall(settlement, out accessDetails);
			bool disableOption = false;
			TextObject disabledText = null;
			bool flag3 = settlementAccessModel.CanMainHeroAccessLocation(settlement, "lordshall", out disableOption, out disabledText);
			int bribeToEnterLordsHall = Campaign.Current?.Models?.BribeCalculationModel?.GetBribeToEnterLordsHall(settlement) ?? 0;
			tokens["accessReason"] = DescribeLordsHallAccessReason(accessDetails);
			tokens["guideBribeGold"] = bribeToEnterLordsHall.ToString();
			if (flag3)
			{
				stateKey = "allowed_directly";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe && bribeToEnterLordsHall > 0)
			{
				stateKey = "denied_but_bribe_available";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise)
			{
				stateKey = "disguise_only";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.LocationEmpty)
			{
				stateKey = "no_one_inside";
				return true;
			}
			stateKey = "denied_no_bribe";
			return true;
		}
		catch
		{
			stateKey = "not_applicable";
			tokens = tokens ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			return false;
		}
	}

	private static string BuildRuntimeLordsHallAccessInstruction()
	{
		try
		{
			if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
			{
				return ResolveRuleRuntimeText("lords_hall_access", stateKey, forConstraint: false, tokens);
			}
		}
		catch
		{
		}
		return "";
	}

	// Used by shout prompt assembly to keep the guard rule always present for gate guards.
	public static string BuildRuntimeLordsHallAccessInstructionForExternal()
	{
		try
		{
			if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
			{
				string text = (stateKey ?? "").Trim().ToLowerInvariant();
				if (string.Equals(text, "not_applicable", StringComparison.OrdinalIgnoreCase))
				{
					return "";
				}
				return ResolveRuleRuntimeText("lords_hall_access", text, forConstraint: false, tokens);
			}
		}
		catch
		{
		}
		return "";
	}

	public static bool IsGuardrailRuleAvailableToPreprocessForExternal(string ruleId, bool hasAnyHero)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			return !string.IsNullOrWhiteSpace(id)
				&& IsRuleCurrentlyEligibleForRag(id)
				&& CanInjectRuleTopicIntoPreprocessForExternal(id, hasAnyHero);
		}
		catch
		{
			return false;
		}
	}

	public static bool CanInjectRuleTopicIntoPreprocessForExternal(string ruleId, bool hasAnyHero)
	{
		try
		{
			string text = (ruleId ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			switch (text)
			{
			case "kingdom_service":
				return true;
			case "siege_intervention_aftermath":
				return AfGcczShoutBridge.IsActive();
			case "kingdom_vassalage":
			{
				bool runtimeEligible = VassalageBehavior.CanInjectVassalageRuleForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
				VassalageDiagnosticLog.Event("preprocess.gate", new Dictionary<string, object>
				{
					["ruleId"] = text,
					["hasAnyHero"] = hasAnyHero,
					["targetHero"] = VassalageDiagnosticLog.DescribeHero(ResolveConversationTargetHero()),
					["targetCharacterId"] = ResolveConversationTargetCharacter()?.StringId ?? "",
					["runtimeEligible"] = runtimeEligible,
					["allowRuleIntoPreprocess"] = runtimeEligible,
					["reason"] = runtimeEligible ? "player_and_target_are_rulers" : "requires_player_and_target_rulers"
				});
				return runtimeEligible;
			}
			case "diplomacy":
				return DiplomacyBehavior.CanInjectDiplomacyRuleForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
			case "world_diplomacy_discussion":
				return WorldDiplomacyBehavior.CanDiscussWorldDiplomacyForExternal(ResolveConversationTargetHero());
			case "kingdom_agenda":
				return IsKingdomLordOrKingRuleTargetForPreprocess(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
			case "marriage":
				return ResolveConversationTargetHero() != null && !string.IsNullOrWhiteSpace(RomanceSystemBehavior.Instance?.BuildMarriageRuntimeInstruction(ResolveConversationTargetHero()));
			case "vanilla_issue":
				return ResolveConversationTargetHero() != null
					|| ResolveConversationTargetCharacter() != null
					|| !string.IsNullOrWhiteSpace(ResolveRuntimeTargetTroopId())
					|| !string.IsNullOrWhiteSpace(ResolveRuntimeTargetUnnamedRank());
			case "npc_major_actions":
				return !string.IsNullOrWhiteSpace(MyBehavior.BuildNpcMajorActionsRuntimeInstructionForExternal(ResolveConversationTargetHero()));
			case "lords_hall_access":
				return !string.IsNullOrWhiteSpace(BuildRuntimeLordsHallAccessInstructionForExternal());
			case "noble_deference":
				return false;
			default:
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private static string BuildRuntimeRuleConstraintHint(string tag)
	{
		try
		{
			string text = (tag ?? "").Trim().ToLowerInvariant();
			if (text == "marriage")
			{
				Hero speaker = ResolveConversationTargetHero();
				return RomanceSystemBehavior.Instance?.BuildMarriageRuntimeConstraintHint(speaker) ?? "";
			}
			if (text == "kingdom_vassalage")
			{
				return VassalageBehavior.BuildRuntimeVassalageConstraintHintForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
			}
			if (text == "diplomacy")
			{
				return KingdomAnnexationBehavior.BuildRuntimeAnnexationConstraintHintForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter());
			}
			if (text == "npc_major_actions")
			{
				return MyBehavior.BuildNpcActionsRuntimeConstraintHintForExternal(ResolveConversationTargetHero(), recentOnly: false);
			}
			if (text == "lords_hall_access")
			{
				if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
				{
					return ResolveRuleRuntimeText("lords_hall_access", stateKey, forConstraint: true, tokens);
				}
				return "";
			}
			if (text != "kingdom_service")
			{
				return "";
			}
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				return ResolveKingdomServiceRuntimeText("no_player_clan", forConstraint: true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			}
			Dictionary<string, string> dictionary = BuildKingdomServiceRuntimeTokens(out playerClan, out var kingdom, out var flag, out var kingdom2, out var flag2, out var num, out var num2, out var num3, out var num4, out var num5, out var num6);
			if (IsPlayerKingdomRecruitmentModeActive(playerClan, kingdom))
			{
				string text3 = ResolvePlayerKingdomRecruitmentStateKey(playerClan, kingdom, ResolveConversationTargetClan(), ResolveConversationTargetHero());
				if (string.IsNullOrWhiteSpace(text3))
				{
					return "";
				}
				return ResolveKingdomServiceRuntimeText(text3, forConstraint: true, dictionary);
			}
			string text2 = ResolveKingdomServiceRuntimeStateKey(kingdom, flag, kingdom2, flag2, num, num2, num3, num4, num5, num6);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return "";
			}
			if (!ShouldAppendKingdomServiceConstraint(text2))
			{
				return "";
			}
			return ResolveKingdomServiceRuntimeText(text2, forConstraint: true, dictionary);
		}
		catch
		{
			return "";
		}
	}

	private static string RuleTopicLabel(string tag)
	{
		return (tag ?? "").Trim().ToLowerInvariant() switch
		{
			"duel" => "决斗挑战", 
			"reward" => "交易/奖励", 
			"loan" => "借贷/赊账", 
			"surroundings" => "地理方位", 
			_ => "当前话题", 
		};
	}

	private static string RuleTopicAskHint(string tag)
	{
		return (tag ?? "").Trim().ToLowerInvariant() switch
		{
			"duel" => "向我发起决斗", 
			"reward" => "谈交易和货物", 
			"loan" => "谈借钱或还款", 
			"surroundings" => "问附近位置", 
			_ => "讨论这件事", 
		};
	}

	public static string BuildGuardrailClarificationHint(string input, bool duelHit, float duelScore, bool rewardHit, float rewardScore, bool loanHit, float loanScore, bool surroundingsHit, float surroundingsScore)
	{
		try
		{
			if (duelHit || rewardHit || loanHit || surroundingsHit)
			{
				return "";
			}
			List<KeyValuePair<string, float>> list = new List<KeyValuePair<string, float>>
			{
				new KeyValuePair<string, float>("duel", duelScore),
				new KeyValuePair<string, float>("reward", rewardScore),
				new KeyValuePair<string, float>("loan", loanScore),
				new KeyValuePair<string, float>("surroundings", surroundingsScore)
			}.OrderByDescending((KeyValuePair<string, float> x) => x.Value).ToList();
			if (list.Count < 2)
			{
				return "";
			}
			KeyValuePair<string, float> keyValuePair = list[0];
			KeyValuePair<string, float> keyValuePair2 = list[1];
			if (keyValuePair.Value < 0.4f)
			{
				return "";
			}
			float num = keyValuePair.Value - keyValuePair2.Value;
			if (num >= 0.07f)
			{
				return "";
			}
			string text = RuleTopicLabel(keyValuePair.Key);
			string text2 = RuleTopicLabel(keyValuePair2.Key);
			string text3 = RuleTopicAskHint(keyValuePair.Key);
			string text4 = RuleTopicAskHint(keyValuePair2.Key);
			return $"[系统-澄清优先] 玩家意图在“{text}”与“{text2}”之间存在歧义（分差={num:0.00}）。本轮先追问一句澄清，不要输出任何 ACTION 标签，也不要直接承诺交易/借贷/决斗。可参考：你是想{text3}，还是在{text4}？";
		}
		catch
		{
			return "";
		}
	}

	private static DuelSettings TryGetMcmSettings()
	{
		try
		{
			return DuelSettings.GetSettings();
		}
		catch
		{
			return null;
		}
	}

	private static bool UseMcmKnowledgeRetrieval()
	{
		try
		{
			DuelSettings duelSettings = TryGetMcmSettings();
			return duelSettings != null;
		}
		catch
		{
			return false;
		}
	}

	private static int ClampKnowledgeTopK(int v)
	{
		if (v < 1)
		{
			v = 1;
		}
		if (v > 12)
		{
			v = 12;
		}
		return v;
	}

	public static int GuardrailRuleReturnCap => GetGuardrailReturnCapFromMcm();

	private static int ClampGuardrailReturnCap(int v)
	{
		if (v < 1)
		{
			v = 1;
		}
		if (v > 12)
		{
			v = 12;
		}
		return v;
	}

	private static int GetGuardrailReturnCapFromMcm()
	{
		try
		{
			DuelSettings duelSettings = TryGetMcmSettings();
			if (duelSettings != null)
			{
				return ClampGuardrailReturnCap(duelSettings.GuardrailDirectTopN);
			}
		}
		catch
		{
		}
		return 4;
	}

	private static bool TryGetKnowledgeFromMcm(out bool enabled, out bool semanticFirst, out int topK)
	{
		enabled = true;
		semanticFirst = true;
		topK = 4;
		try
		{
			if (!UseMcmKnowledgeRetrieval())
			{
				return false;
			}
			DuelSettings duelSettings = TryGetMcmSettings();
			if (duelSettings == null)
			{
				return false;
			}
			try
			{
				enabled = duelSettings.KnowledgeRetrievalEnabled;
			}
			catch
			{
				enabled = true;
			}
			try
			{
				semanticFirst = duelSettings.KnowledgeSemanticFirst;
			}
			catch
			{
				semanticFirst = true;
			}
			try
			{
				int knowledgeDirectTopN = duelSettings.KnowledgeDirectTopN;
				if (knowledgeDirectTopN > 0)
				{
					topK = knowledgeDirectTopN;
				}
				else
				{
					topK = duelSettings.KnowledgeSemanticTopK;
				}
			}
			catch
			{
			}
			topK = ClampKnowledgeTopK(topK);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void ReloadConfig()
	{
		try
		{
			_preprocessPromptsLoadError = "";
			string path = ResolveModuleDataFilePath("AIConfig.json");
			if (!File.Exists(path))
			{
				Logger.Log("AIConfig", "[错误] 找不到 AIConfig.json");
				_config = new AIConfigModel();
			}
			else
			{
				string value = File.ReadAllText(path);
				_config = JsonConvert.DeserializeObject<AIConfigModel>(value) ?? new AIConfigModel();
			}
			string path2 = ResolveModuleDataFilePath("RuleBehaviorPrompts.json");
			string path3 = ResolveModuleDataFilePath("ActionPostprocessPrompts.json");
			string path4 = ResolveModuleDataFilePath("PreprocessPrompts.json");
			string path5 = ResolveModuleDataFilePath("ProactiveNpcRequestPrompts.json");
			string path6 = ResolveModuleDataFilePath("RpItemIntroductionPrompts.json");
			if (!File.Exists(path2))
			{
				Logger.Log("AIConfig", "[错误] 找不到 RuleBehaviorPrompts.json");
				_guardrail = new GuardrailConfigModel();
			}
			else
			{
				string value2 = File.ReadAllText(path2);
				_guardrail = JsonConvert.DeserializeObject<GuardrailConfigModel>(value2) ?? new GuardrailConfigModel();
			}
			if (!File.Exists(path3))
			{
				Logger.Log("AIConfig", "[错误] 找不到 ActionPostprocessPrompts.json");
				_actionPostprocess = new ActionPostprocessConfigModel();
			}
			else
			{
				string value3 = File.ReadAllText(path3);
				_actionPostprocess = JsonConvert.DeserializeObject<ActionPostprocessConfigModel>(value3) ?? new ActionPostprocessConfigModel();
			}
			try
			{
				if (!File.Exists(path4))
				{
					throw new FileNotFoundException("找不到 PreprocessPrompts.json", path4);
				}
				_preprocessPrompts = LoadPreprocessPromptsConfig(path4, out var usedEmbeddedDefaults, out var sourceVersion, out var defaultVersion);
				ValidateLoadedPreprocessPrompts();
				if (usedEmbeddedDefaults)
				{
					Logger.Log("AIConfig", string.Format("[兼容] 检测到旧版 PreprocessPrompts.json (v{0})，其输出 schema 与 v{1} 不兼容；本次运行已采用程序集内置 v{1} 默认提示词，磁盘文件未改写。", sourceVersion, defaultVersion));
				}
			}
			catch (Exception preprocessEx)
			{
				_preprocessPrompts = new PreprocessPromptsConfigModel();
				_preprocessPromptsLoadError = preprocessEx.Message;
				Logger.Log("AIConfig", "[错误] 前处理提示词配置加载失败: " + preprocessEx.Message);
			}
			try
			{
				if (!File.Exists(path5))
				{
					throw new FileNotFoundException("找不到 ProactiveNpcRequestPrompts.json", path5);
				}
				string value5 = File.ReadAllText(path5, Encoding.UTF8);
				_proactiveNpcRequestPrompts = JsonConvert.DeserializeObject<ProactiveNpcRequestPromptsConfigModel>(value5) ?? new ProactiveNpcRequestPromptsConfigModel();
			}
			catch (Exception proactivePromptEx)
			{
				Logger.Log("AIConfig", "[错误] 载入 ProactiveNpcRequestPrompts.json 失败: " + proactivePromptEx.Message);
				_proactiveNpcRequestPrompts = new ProactiveNpcRequestPromptsConfigModel();
			}
			try
			{
				_rpItemIntroductionPrompts = LoadRpItemIntroductionPromptsConfig(path6, out var usedEmbeddedDefaults2, out var fallbackReason);
				Interlocked.Exchange(ref _rpItemIntroductionPromptBuildFailureLogged, 0);
				if (usedEmbeddedDefaults2)
				{
					if (Interlocked.Exchange(ref _rpItemIntroductionPromptsFallbackLogged, 1) == 0)
					{
						Logger.Log("AIConfig", "[RP物品介绍] RpItemIntroductionPrompts.json 无效或缺失，已回退程序集内置默认提示词；磁盘文件未改写。原因: " + fallbackReason);
					}
				}
				else
				{
					Interlocked.Exchange(ref _rpItemIntroductionPromptsFallbackLogged, 0);
				}
			}
			catch (Exception rpItemIntroductionPromptEx)
			{
				_rpItemIntroductionPrompts = new RpItemIntroductionPromptsConfigModel();
				if (Interlocked.Exchange(ref _rpItemIntroductionPromptsFallbackLogged, 1) == 0)
				{
					Logger.Log("AIConfig", "[错误] RP物品介绍提示词配置及其内置默认值均不可用，自动介绍已停用: " + rpItemIntroductionPromptEx.Message);
				}
			}
			lock (_guardrailSemanticLock)
			{
				_guardrailPhraseVecCache.Clear();
				_guardrailInputVecCache.Clear();
				_lastGuardrailEval = null;
			}
			long num = Interlocked.Increment(ref _guardrailConfigVersion);
			Interlocked.Exchange(ref _guardrailWarmupState, 0);
			Interlocked.Exchange(ref _guardrailWarmupVersion, -1L);
			_guardrailSemanticRuntimeContext.Value = "";
			_guardrailRuntimeTargetKingdomId.Value = "";
			int valueOrDefault = (_guardrail?.Duel?.AcceptKeywords?.Count).GetValueOrDefault();
			int valueOrDefault2 = (_guardrail?.Reward?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault3 = (_guardrail?.Loan?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault4 = (_guardrail?.Surroundings?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault5 = (_guardrail?.RulePrompts?.Count).GetValueOrDefault();
			int num2 = 0;
			try
			{
				num2 = GetAllEnabledRulePrompts().Count;
			}
			catch
			{
				num2 = 0;
			}
			string text = (KnowledgeRetrievalFromMcm ? "MCM" : "Guardrail");
			Logger.Log("AIConfig", string.Format("配置加载成功。触发词(决斗/奖励/借贷/地理)={0}/{1}/{2}/{3}，扩展规则={4}，启用规则总数={5}。规则返回上限={6}。知识检索({7})：{8}（语义优先={9}, returnCap={10}）。后处理模板：{11}。", valueOrDefault, valueOrDefault2, valueOrDefault3, valueOrDefault4, valueOrDefault5, num2, GetGuardrailReturnCapFromMcm(), text, KnowledgeRetrievalEnabled ? "开启" : "关闭", KnowledgeSemanticFirst, KnowledgeSemanticTopK, ActionPostprocessEnabled ? "开启" : "关闭"));
			Logger.Log("AIConfig", "配置文件路径：AIConfig=" + path + " RuleBehavior=" + path2 + " ActionPostprocess=" + path3 + " PreprocessPrompts=" + path4 + " RpItemIntroductionPrompts=" + path6);
		}
		catch (Exception ex)
		{
			Logger.Log("AIConfig", "[错误] 加载失败: " + ex.Message);
			_config = new AIConfigModel();
			_guardrail = new GuardrailConfigModel();
			_actionPostprocess = new ActionPostprocessConfigModel();
			_preprocessPrompts = new PreprocessPromptsConfigModel();
			_preprocessPromptsLoadError = ex.Message;
			_proactiveNpcRequestPrompts = new ProactiveNpcRequestPromptsConfigModel();
			_rpItemIntroductionPrompts = new RpItemIntroductionPromptsConfigModel();
		}
	}

	private static string ResolveModuleDataFilePath(string fileName)
	{
		return AnimusForgeModulePaths.GetModuleDataFilePath(fileName);
	}

	public static string GetLoreContext(string inputText, Hero npcHero)
	{
		return GetLoreContext(inputText, npcHero, null, null);
	}

	public static string GetLoreContext(string inputText, Hero npcHero, string secondaryInput)
	{
		return GetLoreContext(inputText, npcHero, secondaryInput, null);
	}

	public static string GetLoreContext(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities)
	{
		if (string.IsNullOrWhiteSpace(inputText))
		{
			try
			{
				if (mentionedEntities == null || mentionedEntities.IsEmpty)
				{
					return "";
				}
			}
			catch
			{
				return "";
			}
		}
		try
		{
			KnowledgeLibraryBehavior instance = KnowledgeLibraryBehavior.Instance;
			if (instance != null)
			{
				string text = instance.BuildLoreContext(inputText, npcHero, secondaryInput, mentionedEntities);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static string GetLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride = null)
	{
		return GetLoreContext(inputText, npcCharacter, kingdomIdOverride, null, null);
	}

	public static string GetLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride, string secondaryInput)
	{
		return GetLoreContext(inputText, npcCharacter, kingdomIdOverride, secondaryInput, null);
	}

	public static string GetLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride, string secondaryInput, MentionedWorldEntities mentionedEntities)
	{
		if (string.IsNullOrWhiteSpace(inputText))
		{
			try
			{
				if (mentionedEntities == null || mentionedEntities.IsEmpty)
				{
					return "";
				}
			}
			catch
			{
				return "";
			}
		}
		try
		{
			KnowledgeLibraryBehavior instance = KnowledgeLibraryBehavior.Instance;
			if (instance != null)
			{
				string text = instance.BuildLoreContext(inputText, npcCharacter, kingdomIdOverride, secondaryInput, mentionedEntities);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}
}
