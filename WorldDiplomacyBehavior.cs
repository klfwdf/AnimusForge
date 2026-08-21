using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge;

public sealed class WorldDiplomacyBehavior : CampaignBehaviorBase
{
	private const string Source = "WorldDiplomacy";
	private const string SaveKey = "_af_world_diplomacy_v1";
	private const int DaysPerYear = 84;
	private const int DefaultApiTimeoutMilliseconds = 90000;
	private const int GenerationMaxTokens = 1800;
	private const int MaxGeneratedDraftRepairAttempts = 1;
	private const int MaxDiplomaticActionsPerDocument = 4;
	private const int AnalysisMaxTokens = 900;
	private const int CompressionOutputTokenReserve = 1024;
	private const int CompressionJobPriority = 1000;
	private const int MaxStoredDocuments = 420;
	private const int MaxStoredAnnualSummaries = 24;
	private const int MaxStoredCompressionSummaries = 24;
	private const int MaxStoredRoundSummaries = 96;
	private const int CompressionRetryInitialHours = 1;
	private const int CompressionRetryMaximumHours = 24;
	private const int MaxPendingJobs = 24;
	private const int NativeWarSignalBase = 24;
	private const int NativeOtherSignalBase = 42;
	private const int FixedMaxConcurrentOffensiveWars = 2;
	private const int FailedServiceCooldownHours = 12;
	private const float CessionCastleUnlockThreshold = 90f;
	private const float CessionTownUnlockThreshold = 95f;
	private const int MaxPeaceCessionCandidates = 5;
	private const int RecentBattleRetentionDays = 21;
	private const int MaxStoredRecentBattles = 96;
	private const int MaxPromptRecentBattles = 5;
	private const int MaxPropagationArrivalsPerDay = 1200;
	private const int MaxAiDocumentsStartedPerDay = 8;
	private const int MaxDiplomacyLlmRequestsPerDay = 12;
	private const int MaxAutomaticDocumentsPerRound = 12;
	private const int MaxAutomaticReplyDepth = 2;
	private const int MaxPriorityPlayerResponsesPerDocument = 3;
	private const int RoundInactivityDays = 7;
	private const int MaxKnownDocumentsPerLocation = 64;
	private const int MaxPendingPolicySignals = 24;
	private const int MaxProcessedPolicySignalKeys = 256;
	private const int PolicyHistorySyncBatchSize = 256;
	private const int PolicyHistoryForceSyncMaxBatches = 40;
	private const int PolicySignalRetentionDays = 21;
	private const int DiplomaticThreatStateSchemaVersion = 3;
	private const int MaxStoredDiplomaticThreats = 96;
	private const int OfferCooldownStateSchemaVersion = 1;
	private const int ResultSettlementStateSchemaVersion = 1;
	private const int MaxStoredOfferCooldowns = 2048;
	private const int DiplomaticThreatRetentionDays = DaysPerYear * 2;
	private const int DefaultNationalPrestige = 100;
	private const int DefaultInternationalReputation = 50;
	private const int MaximumInternationalReputationChangePerDocument = 10;
	private const int InternationalReputationNaturalAnchor = 20;
	private const int InternationalReputationFastDecayMinimum = 71;
	private const int InternationalReputationNormalDecayMinimum = 51;
	private const int InternationalReputationSlowDecayMinimum = 21;
	private const int InternationalReputationWeeklyIntervalDays = 7;
	private const int InternationalReputationSlowDecayIntervalDays = 14;
	private const int InternationalReputationFastDecayStep = 2;
	private const int WarningFollowThroughPrestigePenalty = 10;
	private const int UltimatumFollowThroughPrestigePenalty = 25;
	private const int WarningCompliancePrestigeChange = 5;
	private const int UltimatumCompliancePrestigeChange = 10;
	private const int WarningEscalationPrestigeReward = 3;
	private const int UltimatumWarPrestigeReward = 5;
	private const int ZeroPrestigeWarningBreachRelationPenalty = -2;
	private const int ZeroPrestigeUltimatumBreachRelationPenalty = -5;
	private const int UltimatumComplianceRoyalRelationPenalty = -20;
	private const int DecisionArchitectureVersion = 1;
	private const int HistoryMemorySchemaVersion = 4;
	private const int DiplomacyNotificationStateSchemaVersion = 1;
	private const int DiplomacyPromptContractVersion = 27;
	private const int RelaySchemaVersion = 23;
	private const string CanonicalHistoryCacheAffinityKey = "diplomacy-history:v27";
	private const string CanonicalHistoryContractMarker = "【AI外交长期记忆共同模式】";
	private const string DiplomaticDeclarationWritingContractMarker = "【国家外交公文文体契约】";
	private const string DiplomacyModeDispatchContractMarker = "【AI外交固定任务MODE分派】";
	private const string DiplomaticDeclarationModeContractMarker = "【MODE=DECLARE 固定任务合同】";
	private const string CanonicalHistoryCompressionModeContractMarker = "【MODE=COMPACT 固定任务合同】";
	private const string RoundPlanTaskMarker = "【当前任务：一次性规划外交事件参与国】";
	private const string DiplomacyAnalysisTaskMarker = "【任务：外交宣言语义裁判】";
	private const string KingdomStrategicProfileMarkerPrefix = "【AnimusForge 发文国国家卡：";
	private const string KingdomStrategicIntentRule = "需要为长期战略寻找理由，尤其是战争，应依据当前局势，以现实利益、争端或安全诉求作为公开理由。";
	private const int RelayPassDurationDays = 7;
	private const int RelayTargetDurationDays = 21;
	private const int RelayHardDurationDays = 24;
	private const int MaxRelayParticipants = 12;
	private const int BorderForeignNeighborCount = 2;
	private const int LowInternationalReputationThreshold = 40;
	private const int SevereInternationalReputationThreshold = 20;
	private const int RecentNegativeReputationFactRetentionDays = DaysPerYear;
	private const int MaxPromptRecentNegativeReputationFacts = 2;
	private const int MaxPromptRecentOwnReputationReasons = 2;
	private const float BorderDistanceMedianMultiplier = 3.5f;
	private const float MinimumBorderDistance = 24f;
	private const float MaximumBorderDistance = 72f;
	private static readonly Regex InternalMetricWithNumberRegex = new Regex(
		@"(?:战争进展|战争进度|战局进度|议和开放度|和平开放度|劣势评分|优势评分|战争压力(?:值|分数)?|(?:(?:外交|本国在诸国中的)?(?:声誉|信誉|名誉|威信))(?:值|点数|分数)?|统治者关系(?:值|点数)?|(?:家族|封臣|王族|王室)关系(?:值|点数)?|(?:(?:所有|各)?封臣家族|各?封臣|家族)(?:对|与|同|和)(?:当前)?(?:王族|王室)(?:的)?(?:关系|好感)?|(?:王族|王室)(?:对|与|同|和)(?:(?:所有|各)?封臣家族|各?封臣)(?:的)?(?:关系|好感)?|(?:所有|各)?封臣家族和(?:王族|王室)的关系|与(?:王族|王室)关系|关系点数|好感度|战力值|总战力)[^。\r\n]{0,16}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+)(?:分|点)?|(?:领先|落后|高出|低于)[^。\r\n]{0,8}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+)(?:分|点)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex InternalMetricTermRegex = new Regex(
		@"(?:议和|和平)开放度|(?:优势|劣势)评分|外交(?:声誉|信誉)(?:值|点数|分数)|数值阈值|(?:系统|模型|AI|程序)(?:判定|评分|数据|数值|字段)|游戏(?:机制|数据|数值)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex ConversationalDiplomacyPhraseRegex = new Regex(
		@"让我(?:说说|把话说清楚)|你(?:应该谢我|自己选|真不知道|若知道|说得很重|先把|别急)|我(?:替你|跟你|告诉你|不想要|想要的是)|我们之间的(?:对话|话)|等你(?:答复|回话)|先这样|话说回来|说白了|这没什么好谈的",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex PrivateFirstPersonRegex = new Regex(
		@"我(?!国|方|朝|军|王|邦|境|土)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex DirectSecondPersonRegex = new Regex(
		@"你(?:们|的)?|您(?:的)?",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static bool _patchesApplied;
	private static int _internalDiplomaticActionDepth;

	private readonly ConcurrentQueue<LlmJobResult> _completedJobs = new ConcurrentQueue<LlmJobResult>();
	private readonly HashSet<string> _notifiedDocumentIdsThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, WarSituationSnapshot> _warSituationCache = new Dictionary<string, WarSituationSnapshot>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _courtSettlementCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _realmInstitutionalVoiceCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _canonicalHistorySourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Queue<string> _deferredCanonicalHistoryDocumentIds = new Queue<string>();
	private readonly HashSet<string> _deferredCanonicalHistoryDocumentIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _deferredCanonicalHistoryRetryAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _deferredCanonicalHistoryRetryAfterHour = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, WorldDiplomacyRealmRelationProfile> _realmRelationProfileCache = new Dictionary<string, WorldDiplomacyRealmRelationProfile>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, WorldDiplomacyBorderRelation> _kingdomBorderCache = new Dictionary<string, WorldDiplomacyBorderRelation>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> _offerCooldownByKey = new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
	private int _kingdomBorderCacheDay = -1;
	private float _kingdomBorderDistanceThreshold = MinimumBorderDistance;
	private long _realmInstitutionalVoiceRuleVersion = -1L;

	private WorldDiplomacyStorage _storage = new WorldDiplomacyStorage();
	private bool _llmRequestRunning;
	private string _activeJobId = "";
	private long _activeRequestRuntimeGeneration;
	private bool _disabledStateApplied;
	private MapNotificationView _registeredMapNotificationView;
	private long _runtimeGeneration;
	private bool _nativeDiplomacyDecisionQueueSanitized;
	private int _aiDocumentsStartedDay = -1;
	private int _aiDocumentsStartedToday;
	private int _lastSchedulerDay = -1;
	private string _lastLlmCacheAffinityKey = "";
	private int _llmRequestsStartedDay = -1;
	private int _llmRequestsStartedToday;
	private int _lastLlmBudgetLogDay = -1;
	private long _cacheHitTokensThisSession;
	private long _cacheMissTokensThisSession;
	private long _relayCacheHitTokensThisSession;
	private long _relayCacheMissTokensThisSession;
	private bool? _lastMapNotificationsEnabled;
	private DateTime _nextNotificationPollUtc = DateTime.MinValue;
	private bool _initialPeaceApplicationAttempted;
	private string _canonicalHistoryRenderCacheKey = "";
	private string _canonicalHistoryRenderCache = "";
	private int _lastCanonicalSourceSyncHour = int.MinValue;
	private long _lastObservedWorldWeeklyHistoryRevision = -1L;
	private bool _canonicalHistoryInitializedThisSession;

	public static WorldDiplomacyBehavior Instance { get; private set; }

	public WorldDiplomacyBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		Log("registered");
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			NormalizeStorage();
			string json = JsonConvert.SerializeObject(_storage);
			CampaignSaveChunkHelper.SaveChunkedString(dataStore, SaveKey, json, Source);
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
			string json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, SaveKey, Source);
			_storage = string.IsNullOrWhiteSpace(json)
				? new WorldDiplomacyStorage()
				: JsonConvert.DeserializeObject<WorldDiplomacyStorage>(json) ?? new WorldDiplomacyStorage();
		}
		catch (Exception ex)
		{
			Log("load failed: " + ex.Message);
			_storage = new WorldDiplomacyStorage();
		}
		ResetTransientRuntime("load");
		NormalizeStorage();
	}

	public void OnEngineTick()
	{
		ProcessComposePopup();
		if (!IsWorldDiplomacyEnabled())
		{
			if (!_disabledStateApplied) HandleDisabledState();
			ProcessCompletedJobs();
			return;
		}
		_disabledStateApplied = false;
		ProcessCompletedJobs();
		TryScheduleTokenCompression();
		TryStartNextLlmJob();
		TryPublishPendingNotifications();
	}

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (_patchesApplied)
		{
			return;
		}
		_patchesApplied = true;
		Harmony patcher = harmony ?? new Harmony("com.AnimusForge.world_diplomacy");
		try
		{
			MethodInfo addDecision = AccessTools.Method(typeof(Kingdom), nameof(Kingdom.AddDecision), new[]
			{
				typeof(KingdomDecision),
				typeof(bool)
			});
			if (addDecision != null)
			{
				patcher.Patch(addDecision, prefix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_Kingdom_AddDecision_Prefix)));
				Log("Kingdom.AddDecision diplomacy interception patch applied.");
			}
			else
			{
				Log("Kingdom.AddDecision patch target missing.");
			}
		}
		catch (Exception ex)
		{
			Log("Kingdom.AddDecision patch failed: " + ex.Message);
		}
		try
		{
			Type proposalVmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyProposalActionItemVM");
			if (proposalVmType != null)
			{
				foreach (ConstructorInfo constructor in proposalVmType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					patcher.Patch(constructor, postfix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_DiplomacyProposalActionItem_Constructed_Postfix)));
				}
				MethodInfo executeAction = AccessTools.Method(proposalVmType, "ExecuteAction");
				if (executeAction != null)
				{
					patcher.Patch(executeAction, prefix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_DiplomacyProposalActionItem_Execute_Prefix)));
				}
				MethodInfo refreshValues = AccessTools.Method(proposalVmType, "RefreshValues");
				if (refreshValues != null)
				{
					patcher.Patch(refreshValues, postfix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_DiplomacyProposalActionItem_Constructed_Postfix)));
				}
				Log("kingdom diplomacy proposal button disable patches applied.");
			}
		}
		catch (Exception ex)
		{
			Log("kingdom diplomacy proposal button patches failed: " + ex.Message);
		}
		try
		{
			MethodInfo promptBuilder = typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal", BindingFlags.Public | BindingFlags.Static);
			if (promptBuilder != null)
			{
				patcher.Patch(promptBuilder, postfix: new HarmonyMethod(typeof(WorldDiplomacyBehavior), nameof(Patch_BuildSharedDiplomacyMemory_Postfix)));
				Log("shared three-channel diplomacy memory patch applied.");
			}
		}
		catch (Exception ex)
		{
			Log("shared diplomacy memory patch failed: " + ex.Message);
		}
		WorldDiplomacyUiSprites.EnsurePatched(patcher);
	}

	public static bool OpenComposeFromTerminal(Action onClose = null)
	{
		WorldDiplomacyBehavior behavior = ResolveInstance();
		if (behavior == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("AI 外交功能尚未初始化。"));
			return false;
		}
		return behavior.OpenComposeInternal(onClose);
	}

	public static bool ShowRoyalAnnouncementArchive(Action onClose = null)
	{
		WorldDiplomacyBehavior behavior = ResolveInstance();
		if (behavior == null || Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
		{
			return false;
		}
		try
		{
			return AnimusForgeWorldEventInboxPopup.Show(behavior.BuildRoyalAnnouncementArchiveData(), onClose);
		}
		catch (Exception ex)
		{
			Log("archive open failed: " + ex.Message);
			return false;
		}
	}

	public static void NotifyExternalDiplomacyResolved(string action, Kingdom initiator, Kingdom target, string reason = null)
	{
		try
		{
			ResolveInstance()?.NotifyExternalDiplomacyResolvedInternal(action, initiator, target, reason);
		}
		catch (Exception ex)
		{
			Log("external diplomacy notification failed: " + ex.Message);
		}
	}

	public static List<WorldDiplomacyDocument> GetRecentDocumentsForExternal(int maxCount = 40)
	{
		try
		{
			return ResolveInstance()?.GetRecentDocuments(maxCount) ?? new List<WorldDiplomacyDocument>();
		}
		catch
		{
			return new List<WorldDiplomacyDocument>();
		}
	}

	public static string BuildKingdomDiplomaticStandingEncyclopediaTextForExternal(Kingdom kingdom)
	{
		try
		{
			WorldDiplomacyBehavior behavior = ResolveInstance();
			if (behavior == null || kingdom == null) return "";
			int prestige = behavior.GetNationalPrestige(kingdom.StringId);
			int reputation = behavior.GetInternationalReputation(kingdom.StringId);
			return "【国家威望与国际声誉】\n"
				+ "国家威望：" + prestige.ToString(CultureInfo.InvariantCulture) + "/100\n"
				+ "国际声誉：" + reputation.ToString(CultureInfo.InvariantCulture) + "/100";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildDiplomaticStandingImpactTextForExternal(WorldDiplomacyDocument document)
	{
		if (document == null) return "";
		List<WorldDiplomacyStandingChange> changes = document.DiplomaticStandingChanges
			?? new List<WorldDiplomacyStandingChange>();
		List<WorldDiplomacyStandingChange> authorChanges = changes
			.Where(x => x != null && string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		WorldDiplomacyStandingChange international = authorChanges.LastOrDefault(x =>
			string.Equals(x.Kind, "international_reputation", StringComparison.OrdinalIgnoreCase));
		List<WorldDiplomacyStandingChange> prestigeChanges = authorChanges.Where(x =>
			string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase)).ToList();
		int prestigeDelta = prestigeChanges.Sum(x => x.Delta);
		string prestigeReason = string.Join("；", prestigeChanges.Select(x => x.Reason)
			.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			sb.AppendLine("【外交结果】");
			sb.AppendLine(document.MechanicalResult.Trim());
			sb.AppendLine();
		}
		sb.AppendLine("【外交影响】");
		sb.AppendLine("国际声誉 " + BuildInternationalReputationImpactDeltaText(document, international));
		sb.AppendLine("原因：" + FirstNonEmpty(international?.Reason,
			document.InternationalReputationEvaluationReason,
			"本篇没有形成明确的国际声誉变化。"));
		sb.AppendLine("国家威望 " + FormatSignedStandingDelta(prestigeDelta));
		sb.AppendLine("原因：" + (prestigeChanges.Count == 0 || string.IsNullOrWhiteSpace(prestigeReason)
			? "本篇没有触发国家威望结算。"
			: prestigeReason));
		foreach (WorldDiplomacyStandingChange other in changes.Where(x => x != null
			&& !string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)))
		{
			sb.AppendLine("另受影响：" + FirstNonEmpty(other.KingdomName, other.KingdomId, "未知国家")
				+ "的" + (string.Equals(other.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase) ? "国家威望 " : "国际声誉 ")
				+ FormatSignedStandingDelta(other.Delta) + "（" + FirstNonEmpty(other.Reason, "无说明") + "）");
		}
		return sb.ToString().TrimEnd();
	}

	private static string FormatSignedStandingDelta(int value)
	{
		if (value == 0) return "无变化";
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	private static string BuildInternationalReputationImpactDeltaText(
		WorldDiplomacyDocument document,
		WorldDiplomacyStandingChange change)
	{
		int actualDelta = change?.Delta ?? 0;
		int evaluatedDelta = document?.InternationalReputationEvaluationDelta ?? 0;
		if (actualDelta != 0 || evaluatedDelta == 0) return FormatSignedStandingDelta(actualDelta);
		string boundary = change?.After >= 100 && evaluatedDelta > 0
			? "已达上限100"
			: change?.After <= 0 && evaluatedDelta < 0
				? "已达下限0"
				: "数值边界未产生实际位移";
		return "无实际变化（评价" + FormatSignedStandingDelta(evaluatedDelta) + "，" + boundary + "）";
	}

	public static bool CanDiscussWorldDiplomacyForExternal(Hero hero)
	{
		try
		{
			return ResolveInstance()?.CanDiscussWorldDiplomacy(hero) == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool TryBuildProactiveDiscussionForExternal(Hero hero, out string stableKey, out string fact, out float urgency)
	{
		stableKey = "";
		fact = "";
		urgency = 0f;
		try
		{
			return ResolveInstance()?.TryBuildProactiveDiscussion(hero, out stableKey, out fact, out urgency) == true;
		}
		catch
		{
			stableKey = "";
			fact = "";
			urgency = 0f;
			return false;
		}
	}

	public static bool MarkDocumentReadForExternal(string documentId)
	{
		try
		{
			string cleanId = (documentId ?? "").Trim();
			if (cleanId.StartsWith("diplomacy:", StringComparison.OrdinalIgnoreCase))
			{
				cleanId = cleanId.Substring("diplomacy:".Length);
			}
			WorldDiplomacyDocument document = ResolveInstance()?.ResolveDocument(cleanId);
			if (document == null)
			{
				return false;
			}
			document.IsRead = true;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		_storage = new WorldDiplomacyStorage();
		_storage.HistoryMemorySchemaVersion = HistoryMemorySchemaVersion;
		_storage.PromptContractVersion = DiplomacyPromptContractVersion;
		_storage.DiplomaticThreatStateSchemaVersion = DiplomaticThreatStateSchemaVersion;
		_storage.OfferCooldownStateSchemaVersion = OfferCooldownStateSchemaVersion;
		_storage.ResultSettlementStateSchemaVersion = ResultSettlementStateSchemaVersion;
		_storage.DiplomacyNotificationStateSchemaVersion = DiplomacyNotificationStateSchemaVersion;
		_storage.CanonicalHistory = new WorldDiplomacyCanonicalHistoryState();
		_storage.DecisionArchitectureVersion = DecisionArchitectureVersion;
		_storage.PropagationReliabilityVersion = 1;
		_storage.InitialPeacePending = IsWorldDiplomacyEnabled() && ShouldStartNewGameAtPeace();
		InitializeSchedule();
		ResetTransientRuntime("new-game");
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		NormalizeStorage(allowWorldValidation: true);
		RecoverUnsettledAiInternationalReputation();
		RecoverPlayerCourtReceiptsFromKnowledge();
		InitializeSchedule();
		ResetTransientRuntime("game-loaded");
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		NormalizeStorage(allowWorldValidation: true);
		RecoverUnsettledAiInternationalReputation();
		RecoverPlayerCourtReceiptsFromKnowledge();
		InitializeSchedule();
		ResetTransientRuntime("session-launched");
	}

	private void OnCampaignTick(float dt)
	{
		TryApplyInitialNewGamePeace();
		if (!IsWorldDiplomacyEnabled())
		{
			if (!_disabledStateApplied) HandleDisabledState();
			return;
		}
		_disabledStateApplied = false;
		if (!_nativeDiplomacyDecisionQueueSanitized)
		{
			RemoveQueuedNativeDiplomacyDecisions();
			_nativeDiplomacyDecisionQueueSanitized = true;
		}
		int day = CurrentDay();
		if (_lastSchedulerDay != day)
		{
			_lastSchedulerDay = day;
			RefreshPolicyDiplomacySignals();
			ProcessRelayArrivals();
			ProcessRoundLifecycle();
			TrySchedulePolicyTriggeredRound();
			TryScheduleNormalRound();
		}
	}

	private void OnDailyTick()
	{
		NormalizeStorage(allowWorldValidation: true);
		ReconcileAllNationalPrestigeVassalRelations();
		RetryDeferredCanonicalHistoryEntries();
		RetryDiplomaticThreatDomesticPenalties();
		RetryDiplomaticThreatComplianceConsequences();
		RetryDiplomaticThreatHistoryResults();
		RefreshRoundIntervalScheduleIfNeeded();
		_warSituationCache.Clear();
		_realmRelationProfileCache.Clear();
		_courtSettlementCache.Clear();
		_kingdomBorderCache.Clear();
		_kingdomBorderCacheDay = -1;
		ResetDailyGenerationBudget();
		RecalculatePendingPropagationIfNeeded();
		_lastSchedulerDay = CurrentDay();
		EnsureActiveWarLedgersAndRemoveEndedWars();
		TrimRecentBattleFacts();
		if (!IsWorldDiplomacyEnabled())
		{
			AnchorInternationalReputationNaturalChangeDays();
			if (!_disabledStateApplied) HandleDisabledState();
			return;
		}
		_disabledStateApplied = false;
		RemoveQueuedNativeDiplomacyDecisions();
		_nativeDiplomacyDecisionQueueSanitized = true;
		ProcessInternationalReputationNaturalChange();
		DecayWarPressure();
		RefreshPolicyDiplomacySignals();
		RetryDeferredDocumentPropagation();
		ProcessPropagationArrivals();
		ProcessRelayArrivals();
		RetryDeferredRoundProgress();
		ProcessRoundLifecycle();
		TryScheduleTokenCompression();
		TrySchedulePolicyTriggeredRound();
		TryScheduleNormalRound();
	}

	private void AnchorInternationalReputationNaturalChangeDays()
	{
		if (_storage == null || Campaign.Current == null) return;
		_storage.InternationalReputationNaturalChangeLastDayByKingdom ??=
			new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		int today = CurrentDay();
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated
			&& !string.IsNullOrWhiteSpace(x.StringId)))
		{
			_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdom.StringId] = today;
		}
	}

	private void ProcessInternationalReputationNaturalChange()
	{
		if (_storage == null || Campaign.Current == null) return;
		_storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.InternationalReputationNaturalChangeLastDayByKingdom ??=
			new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		int today = CurrentDay();
		int changedKingdoms = 0;
		int totalAbsoluteChange = 0;
		// This runs once per campaign day over the small live-kingdom set. Catch-up is batched by
		// reputation band, so long time skips never become a per-day or per-point hot loop.
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated
			&& !string.IsNullOrWhiteSpace(x.StringId)))
		{
			string kingdomId = kingdom.StringId;
			if (!_storage.InternationalReputationNaturalChangeLastDayByKingdom.TryGetValue(kingdomId, out int lastDay))
			{
				// Old saves and newly created kingdoms start tracking now; never apply retroactive decay.
				_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdomId] = today;
				continue;
			}
			if (lastDay > today)
			{
				_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdomId] = today;
				continue;
			}

			int before = GetInternationalReputation(kingdomId);
			if (before == InternationalReputationNaturalAnchor)
			{
				// Time spent at the anchor must not accumulate and fire immediately after a declaration.
				_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdomId] = today;
				continue;
			}

			int elapsedDays = today - lastDay;
			int consumedDays;
			int updated = CalculateInternationalReputationNaturalChange(before, elapsedDays, out consumedDays);
			if (updated == InternationalReputationNaturalAnchor)
			{
				_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdomId] = today;
			}
			else if (consumedDays > 0)
			{
				_storage.InternationalReputationNaturalChangeLastDayByKingdom[kingdomId] = lastDay + consumedDays;
			}
			if (updated == before) continue;
			_storage.InternationalReputationByKingdom[kingdomId] = updated;
			changedKingdoms++;
			totalAbsoluteChange += Math.Abs(updated - before);
		}
		if (changedKingdoms > 0)
		{
			Log("international reputation natural change kingdoms="
				+ changedKingdoms.ToString(CultureInfo.InvariantCulture)
				+ " absolute_delta=" + totalAbsoluteChange.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static int CalculateInternationalReputationNaturalChange(
		int currentReputation,
		int elapsedDays,
		out int consumedDays)
	{
		int reputation = Math.Max(0, Math.Min(100, currentReputation));
		int remainingDays = Math.Max(0, elapsedDays);
		consumedDays = 0;
		while (remainingDays > 0 && reputation != InternationalReputationNaturalAnchor)
		{
			int intervalDays;
			int step;
			int maximumTicksInBand;
			if (reputation >= InternationalReputationFastDecayMinimum)
			{
				intervalDays = InternationalReputationWeeklyIntervalDays;
				step = -InternationalReputationFastDecayStep;
				maximumTicksInBand = (reputation - (InternationalReputationFastDecayMinimum - 1)
					+ InternationalReputationFastDecayStep - 1) / InternationalReputationFastDecayStep;
			}
			else if (reputation >= InternationalReputationNormalDecayMinimum)
			{
				intervalDays = InternationalReputationWeeklyIntervalDays;
				step = -1;
				maximumTicksInBand = reputation - (InternationalReputationNormalDecayMinimum - 1);
			}
			else if (reputation >= InternationalReputationSlowDecayMinimum)
			{
				intervalDays = InternationalReputationSlowDecayIntervalDays;
				step = -1;
				maximumTicksInBand = reputation - InternationalReputationNaturalAnchor;
			}
			else
			{
				intervalDays = InternationalReputationWeeklyIntervalDays;
				step = 1;
				maximumTicksInBand = InternationalReputationNaturalAnchor - reputation;
			}

			int availableTicks = remainingDays / intervalDays;
			if (availableTicks <= 0) break;
			int appliedTicks = Math.Min(availableTicks, maximumTicksInBand);
			reputation = Math.Max(0, Math.Min(100, reputation + (step * appliedTicks)));
			int segmentDays = appliedTicks * intervalDays;
			remainingDays -= segmentDays;
			consumedDays += segmentDays;
		}
		return reputation;
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		Kingdom first = faction1 as Kingdom;
		Kingdom second = faction2 as Kingdom;
		if (first == null || second == null || first == second)
		{
			return;
		}
		EnsureWarLedger(first, second);
		ResolveDiplomaticThreatsAfterWarStarted(first, second);
		InvalidateWarSituation(first, second);
	}

	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		Kingdom first = faction1 as Kingdom;
		Kingdom second = faction2 as Kingdom;
		if (first == null || second == null)
		{
			return;
		}
		RemoveWarLedger(first.StringId, second.StringId);
		ClearWarPressure(first.StringId, second.StringId);
		ClearWarPressure(second.StringId, first.StringId);
		InvalidateWarSituation(first, second);
	}

	private void OnSettlementOwnerChanged(
		Settlement settlement,
		bool openToClaim,
		Hero newOwner,
		Hero oldOwner,
		Hero capturerHero,
		ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
		{
			return;
		}
		_kingdomBorderCache.Clear();
		_kingdomBorderCacheDay = -1;
		Kingdom oldKingdom = oldOwner?.Clan?.Kingdom;
		Kingdom newKingdom = newOwner?.Clan?.Kingdom ?? settlement.OwnerClan?.Kingdom;
		if (oldKingdom == null || newKingdom == null || oldKingdom == newKingdom)
		{
			return;
		}
		WorldDiplomacyWarLedger ledger = ResolveWarLedger(oldKingdom.StringId, newKingdom.StringId);
		if (ledger == null && FactionManager.IsAtWarAgainstFaction(oldKingdom, newKingdom))
		{
			ledger = EnsureWarLedger(oldKingdom, newKingdom);
		}
		if (ledger == null)
		{
			return;
		}
		WorldDiplomacySettlementChange change = ledger.SettlementChanges.FirstOrDefault(x => x != null
			&& string.Equals(x.SettlementId, settlement.StringId, StringComparison.OrdinalIgnoreCase));
		if (change == null)
		{
			change = new WorldDiplomacySettlementChange
			{
				SettlementId = settlement.StringId ?? "",
				SettlementName = settlement.Name?.ToString() ?? settlement.StringId ?? "",
				OriginalKingdomId = oldKingdom.StringId ?? ""
			};
			ledger.SettlementChanges.Add(change);
		}
		change.CurrentKingdomId = newKingdom.StringId ?? "";
		change.LastChangedDay = CurrentDay();
		change.CaptureCount++;
		InvalidateWarSituation(oldKingdom, newKingdom);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.HasWinner || mapEvent.IsHideoutBattle)
			{
				return;
			}
			List<string> attackerKingdomIds = ResolveMapEventSideKingdomIds(mapEvent.AttackerSide);
			List<string> defenderKingdomIds = ResolveMapEventSideKingdomIds(mapEvent.DefenderSide);
			if (attackerKingdomIds.Count == 0 || defenderKingdomIds.Count == 0
				|| !attackerKingdomIds.Except(defenderKingdomIds, StringComparer.OrdinalIgnoreCase).Any()
				|| !defenderKingdomIds.Except(attackerKingdomIds, StringComparer.OrdinalIgnoreCase).Any())
			{
				return;
			}
			int day = CurrentDay();
			string stableKey = "battle:" + day.ToString(CultureInfo.InvariantCulture)
				+ ":" + (mapEvent.StringId ?? "")
				+ ":" + string.Join(",", attackerKingdomIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
				+ ":" + string.Join(",", defenderKingdomIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
			_storage.RecentBattles ??= new List<WorldDiplomacyBattleFact>();
			if (_storage.RecentBattles.Any(x => x != null && string.Equals(x.BattleId, stableKey, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			_storage.RecentBattles.Add(new WorldDiplomacyBattleFact
			{
				BattleId = stableKey,
				Day = day,
				GameDate = FormatCampaignDate(day),
				BattleType = ResolveMapEventBattleType(mapEvent),
				Location = mapEvent.MapEventSettlement?.Name?.ToString() ?? "野外",
				AttackerKingdomIds = attackerKingdomIds,
				DefenderKingdomIds = defenderKingdomIds,
				AttackerLeaderNames = ResolveMapEventSideLeaderNames(mapEvent.AttackerSide),
				DefenderLeaderNames = ResolveMapEventSideLeaderNames(mapEvent.DefenderSide),
				WinnerSide = mapEvent.WinningSide == BattleSideEnum.Attacker ? "attacker" : "defender",
				IsPlayerInvolved = mapEvent.IsPlayerMapEvent
			});
			TrimRecentBattleFacts();
		}
		catch (Exception ex)
		{
			Log("record recent battle failed: " + ex.Message);
		}
	}

	private static List<string> ResolveMapEventSideKingdomIds(MapEventSide side)
	{
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddMapEventKingdomId(ids, side?.MapFaction as Kingdom);
		foreach (MapEventParty party in side?.Parties ?? Enumerable.Empty<MapEventParty>())
		{
			Kingdom kingdom = party?.Party?.MapFaction as Kingdom
				?? party?.Party?.Owner?.Clan?.Kingdom
				?? party?.Party?.MobileParty?.ActualClan?.Kingdom
				?? party?.Party?.LeaderHero?.Clan?.Kingdom;
			AddMapEventKingdomId(ids, kingdom);
		}
		return ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static void AddMapEventKingdomId(HashSet<string> target, Kingdom kingdom)
	{
		if (target != null && kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
		{
			target.Add(kingdom.StringId);
		}
	}

	private static List<string> ResolveMapEventSideLeaderNames(MapEventSide side)
	{
		return (side?.Parties ?? Enumerable.Empty<MapEventParty>())
			.Select(x => x?.Party?.LeaderHero?.Name?.ToString())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(6)
			.ToList();
	}

	private static string ResolveMapEventBattleType(MapEvent mapEvent)
	{
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			return "攻守战";
		}
		if (mapEvent?.IsRaid == true)
		{
			return "袭掠战";
		}
		return "野外战斗";
	}

	private void InitializeSchedule()
	{
		int day = CurrentDay();
		int intervalDays = GetRoundIntervalDays();
		if (_storage.NextNormalRoundDay <= 0)
		{
			_storage.NextNormalRoundDay = day + intervalDays;
		}
		if (_storage.LastAppliedRoundIntervalDays <= 0) _storage.LastAppliedRoundIntervalDays = intervalDays;
		if (_storage.LastCompressedYear < 0) _storage.LastCompressedYear = Math.Max(0, day / DaysPerYear - 1);
	}

	private void RefreshRoundIntervalScheduleIfNeeded()
	{
		int currentInterval = GetRoundIntervalDays();
		int previousInterval = _storage.LastAppliedRoundIntervalDays;
		if (previousInterval <= 0)
		{
			_storage.LastAppliedRoundIntervalDays = currentInterval;
			return;
		}
		if (previousInterval == currentInterval) return;
		if (_storage.ActiveRound == null && _storage.NextNormalRoundDay > 0)
		{
			int scheduleBaseDay = _storage.NextNormalRoundDay - previousInterval;
			_storage.NextNormalRoundDay = Math.Max(CurrentDay(), scheduleBaseDay + currentInterval);
			Log("round interval schedule updated old=" + previousInterval.ToString(CultureInfo.InvariantCulture)
				+ " new=" + currentInterval.ToString(CultureInfo.InvariantCulture)
				+ " nextDay=" + _storage.NextNormalRoundDay.ToString(CultureInfo.InvariantCulture));
		}
		_storage.LastAppliedRoundIntervalDays = currentInterval;
	}

	private void ScheduleNextNormalRoundAfter(int baseDay)
	{
		int intervalDays = GetRoundIntervalDays();
		_storage.NextNormalRoundDay = baseDay + intervalDays;
		_storage.LastAppliedRoundIntervalDays = intervalDays;
	}

	private void ResetTransientRuntime(string reason)
	{
		_runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		_llmRequestRunning = false;
		_activeJobId = "";
		_activeRequestRuntimeGeneration = 0L;
		_disabledStateApplied = false;
		while (_completedJobs.TryDequeue(out _))
		{
		}
		_notifiedDocumentIdsThisSession.Clear();
		_registeredMapNotificationView = null;
		_warSituationCache.Clear();
		_realmInstitutionalVoiceCache.Clear();
		_realmRelationProfileCache.Clear();
		_kingdomBorderCache.Clear();
		RebuildOfferCooldownIndex();
		_kingdomBorderCacheDay = -1;
		_realmInstitutionalVoiceRuleVersion = -1L;
		WorldDiplomacyPolicyContext.Clear();
		_lastLlmCacheAffinityKey = "";
		_nativeDiplomacyDecisionQueueSanitized = false;
		_lastSchedulerDay = -1;
		_aiDocumentsStartedDay = -1;
		_aiDocumentsStartedToday = 0;
		_llmRequestsStartedDay = -1;
		_llmRequestsStartedToday = 0;
		_lastLlmBudgetLogDay = -1;
		_cacheHitTokensThisSession = 0;
		_cacheMissTokensThisSession = 0;
		_relayCacheHitTokensThisSession = 0;
		_relayCacheMissTokensThisSession = 0;
		_lastMapNotificationsEnabled = null;
		_nextNotificationPollUtc = DateTime.MinValue;
		_initialPeaceApplicationAttempted = false;
		_canonicalHistorySourceKeys.Clear();
		_deferredCanonicalHistoryDocumentIds.Clear();
		_deferredCanonicalHistoryDocumentIdSet.Clear();
		_deferredCanonicalHistoryRetryAttempts.Clear();
		_deferredCanonicalHistoryRetryAfterHour.Clear();
		foreach (WorldDiplomacyDocument document in _storage.Documents ?? new List<WorldDiplomacyDocument>())
		{
			if (NeedsCanonicalHistoryRetry(document)) EnqueueDeferredCanonicalHistoryRetry(document.DocumentId);
		}
		_canonicalHistoryRenderCacheKey = "";
		_canonicalHistoryRenderCache = "";
		_lastCanonicalSourceSyncHour = int.MinValue;
		_lastObservedWorldWeeklyHistoryRevision = -1L;
		_canonicalHistoryInitializedThisSession = false;
		foreach (WorldDiplomacyJob job in _storage.Jobs)
		{
			if (job != null)
			{
				job.IsRunning = false;
			}
		}
		Log("runtime reset reason=" + reason);
	}

	private static bool ShouldStartNewGameAtPeace()
	{
		try
		{
			return DuelSettings.GetSettings()?.WorldDiplomacyStartNewGameAtPeace ?? false;
		}
		catch
		{
			return true;
		}
	}

	private void TryApplyInitialNewGamePeace()
	{
		if (_initialPeaceApplicationAttempted || !_storage.InitialPeacePending || Campaign.Current == null || !IsWorldDiplomacyEnabled())
		{
			return;
		}
		List<Kingdom> kingdoms = Kingdom.All
			.Where(x => x != null && !x.IsEliminated)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (kingdoms.Count < 2)
		{
			return;
		}
		_initialPeaceApplicationAttempted = true;
		int day = CurrentDay();
		int endedWars = 0;
		for (int firstIndex = 0; firstIndex < kingdoms.Count; firstIndex++)
		{
			for (int secondIndex = firstIndex + 1; secondIndex < kingdoms.Count; secondIndex++)
			{
				Kingdom first = kingdoms[firstIndex];
				Kingdom second = kingdoms[secondIndex];
				if (!FactionManager.IsAtWarAgainstFaction(first, second)) continue;
				try
				{
					RunDiplomaticAction("world_diplomacy_initial_peace", () => MakePeaceAction.Apply(first, second));
					_storage.LastPeaceDayByPair[PairKey(first.StringId, second.StringId)] = day;
					ClearWarPressure(first.StringId, second.StringId);
					ClearWarPressure(second.StringId, first.StringId);
					endedWars++;
				}
				catch (Exception ex)
				{
					Log("initial peace failed pair=" + first.StringId + "|" + second.StringId + " error=" + ex.Message);
				}
			}
		}
		_storage.InitialPeacePending = false;
		_storage.InitialPeaceApplied = true;
		_storage.ActiveWarLedgers.Clear();
		_storage.NativeSignals.Clear();
		RemoveQueuedNativeDiplomacyDecisions();
		_storage.NativeSignals.Clear();
		_storage.WarPressure.Clear();
		_nativeDiplomacyDecisionQueueSanitized = true;
		_warSituationCache.Clear();
		Log("new-game initial peace applied endedWars=" + endedWars.ToString(CultureInfo.InvariantCulture));
	}

	private void HandleDisabledState()
	{
		_disabledStateApplied = true;
		if (_storage.ActiveExchange != null)
		{
			_storage.ActiveExchange.State = "closed_disabled";
			_storage.ActiveExchange.CompletedDay = CurrentDay();
			_storage.ActiveExchange = null;
		}
		_storage.SuspendedExchanges.Clear();
		_storage.Jobs.Clear();
		foreach (WarPressureEntry entry in _storage.WarPressure.Where(x => x != null)) entry.IsEscalationArmed = false;
		_storage.ForcedWarToggleWasEnabled = false;
		// An HTTP task may still be in flight. Keep the runtime request flag until its
		// completion is dequeued, so re-enabling cannot start a second request.
		if (!_llmRequestRunning)
		{
			_activeJobId = "";
			_activeRequestRuntimeGeneration = 0L;
		}
		ScheduleNextNormalRoundAfter(CurrentDay());
		_nativeDiplomacyDecisionQueueSanitized = false;
	}

	private bool OpenComposeInternal(Action onClose)
	{
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (playerKingdom == null || playerKingdom.IsEliminated || playerKingdom.RulingClan?.Leader != Hero.MainHero)
		{
			InformationManager.ShowInquiry(new InquiryData(
				"无法发布外交宣言",
				"只有王国统治者才能发布外交宣言。",
				true,
				false,
				"知道了",
				"",
				onClose,
				null),
				pauseGameActiveState: true);
			return false;
		}
		if (!HasIndependentWorldDiplomacyAuthority(playerKingdom))
		{
			Kingdom suzerain = ResolveWorldDiplomacyRepresentative(playerKingdom);
			InformationManager.ShowInquiry(new InquiryData(
				"无法发布外交宣言",
				"我国的外交事务目前由" + KingdomName(suzerain) + "掌管，不能独立发布外交宣言。",
				true,
				false,
				"知道了",
				"",
				onClose,
				null),
				pauseGameActiveState: true);
			return false;
		}
		return WorldDiplomacyComposePopup.Show(
			"撰写外交宣言",
			"",
			"",
			SubmitPlayerDocument,
			onClose);
	}

	private void SubmitPlayerDocument(string body)
	{
		string cleanBody = NormalizeBody(body);
		if (string.IsNullOrWhiteSpace(cleanBody))
		{
			InformationManager.DisplayMessage(new InformationMessage("外交宣言正文不能为空。"));
			return;
		}
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (playerKingdom == null || playerKingdom.IsEliminated || playerKingdom.RulingClan?.Leader != Hero.MainHero)
		{
			InformationManager.DisplayMessage(new InformationMessage("你当前不再是王国统治者，外交宣言没有发布。"));
			return;
		}
		if (!HasIndependentWorldDiplomacyAuthority(playerKingdom))
		{
			InformationManager.DisplayMessage(new InformationMessage("我国的外交事务由" + KingdomName(ResolveWorldDiplomacyRepresentative(playerKingdom)) + "掌管，外交宣言没有发布。"));
			return;
		}
		WorldDiplomacyRound round = EnsureActiveRound(playerKingdom, null, isPlayerInsertion: true);
		WorldDiplomacyDocument document = CreateDocument(
			playerKingdom,
			null,
			"外交宣言",
			cleanBody,
			"player",
			isPlayerAuthored: true,
			isResponse: false,
			exchangeId: round?.RoundId ?? "");
		document.RoundId = round?.RoundId ?? "";
		WorldDiplomacyResultSettlementSlot playerSettlementSlot = round?.ResultSettlementPending == true
			? (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>()).FirstOrDefault(x => x != null
				&& string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.KingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Status, "waiting_player", StringComparison.OrdinalIgnoreCase))
			: null;
		if (playerSettlementSlot != null)
		{
			document.ResultSettlementSlotId = playerSettlementSlot.SlotId ?? "";
		}
		AddDocument(document);
		if (round != null)
		{
			round.RootDocumentId = FirstNonEmpty(round.RootDocumentId, document.DocumentId);
			round.LastActivityDay = CurrentDay();
			EnsureRoundParticipant(round, playerKingdom.StringId, "active", mandatoryReply: false);
		}
		PublishPlayerAuthoredDocumentImmediately(document);
		EnqueueAnalysisJob(document, priority: 100);
		InformationManager.DisplayMessage(new InformationMessage("外交宣言已经公开发布；系统正在后台解析其对象、诉求与外交动作。"));
	}

	private void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document)
	{
		if (document?.IsPlayerAuthored != true) return;
		document.IsReadyForPublication = true;
		document.AnalysisStatus = "pending_analysis";
		Kingdom author = ResolveKingdom(document.AuthorKingdomId);
		if (author == null) return;
		try
		{
			StartDocumentPropagation(document, author);
		}
		catch (Exception ex)
		{
			// IsReadyForPublication remains true, so the bounded deferred retry path can
			// rebuild geographic propagation without ever hiding the player's document.
			document.PropagationCompleted = false;
			Log("immediate player declaration propagation deferred document=" + document.DocumentId
				+ " error=" + ex.Message);
		}
	}

	private void SuspendActiveExchangeForPlayerInsertion()
	{
		if (_storage.ActiveExchange == null)
		{
			return;
		}
		WorldDiplomacyExchange current = _storage.ActiveExchange;
		current.SuspendedDay = CurrentDay();
		current.StateBeforeSuspension = current.State;
		current.State = "suspended_by_player";
		_storage.SuspendedExchanges.Insert(0, current);
		_storage.ActiveExchange = null;
	}

	private void RestoreSuspendedExchangeIfAny()
	{
		if (_storage.ActiveExchange != null || _storage.SuspendedExchanges.Count == 0)
		{
			return;
		}
		WorldDiplomacyExchange exchange = _storage.SuspendedExchanges[0];
		_storage.SuspendedExchanges.RemoveAt(0);
		int pausedDays = Math.Max(0, CurrentDay() - exchange.SuspendedDay);
		exchange.ResponseDueDay += pausedDays;
		exchange.CloseDueDay += pausedDays;
		exchange.State = string.IsNullOrWhiteSpace(exchange.StateBeforeSuspension) ? "waiting" : exchange.StateBeforeSuspension;
		exchange.StateBeforeSuspension = "";
		_storage.ActiveExchange = exchange;
	}

	private void RefreshPolicyDiplomacySignals()
	{
		_storage.PendingPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		_storage.ProcessedPolicySignalKeys ??= new List<string>();
		_storage.RecentTopicUses ??= new List<WorldDiplomacyTopicUse>();
		HashSet<string> known = new HashSet<string>(_storage.ProcessedPolicySignalKeys, StringComparer.OrdinalIgnoreCase);
		foreach (WorldDiplomacyPolicySignal pending in _storage.PendingPolicySignals.Where(item => item != null))
		{
			known.Add(pending.SignalKey ?? "");
		}

		int day = CurrentDay();
		foreach (WorldDiplomacyPolicySignalSnapshot snapshot in WorldDiplomacyPolicyContext.GetForeignPolicySignals())
		{
			if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.SignalKey) || known.Contains(snapshot.SignalKey)
				|| day - snapshot.PublishedDay > PolicySignalRetentionDays)
			{
				continue;
			}
			_storage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal
			{
				SignalKey = snapshot.SignalKey,
				PolicyId = snapshot.PolicyId,
				PolicyKind = snapshot.PolicyKind,
				PolicyName = snapshot.PolicyName,
				PolicySummary = snapshot.PolicySummary,
				IssuerKingdomId = snapshot.IssuerKingdomId,
				IssuerKingdomName = snapshot.IssuerKingdomName,
				TargetKingdomId = snapshot.TargetKingdomId,
				TargetKingdomName = snapshot.TargetKingdomName,
				DirectEffect = snapshot.DirectEffect,
				PublishedDay = snapshot.PublishedDay
			});
			known.Add(snapshot.SignalKey);
		}
		_storage.PendingPolicySignals = _storage.PendingPolicySignals
			.Where(item => item != null && !string.IsNullOrWhiteSpace(item.SignalKey) && day - item.PublishedDay <= PolicySignalRetentionDays)
			.OrderBy(item => item.PublishedDay)
			.ThenBy(item => item.SignalKey, StringComparer.OrdinalIgnoreCase)
			.Take(MaxPendingPolicySignals)
			.ToList();
	}

	private void TrySchedulePolicyTriggeredRound()
	{
		WorldDiplomacyPolicySignal signal = (_storage.PendingPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
			.FirstOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.SignalKey));
		if (signal == null)
		{
			return;
		}

		Kingdom issuer = ResolveKingdom(signal.IssuerKingdomId);
		Kingdom affected = ResolveKingdom(signal.TargetKingdomId);
		if (issuer == null || affected == null || issuer == affected || issuer.IsEliminated || affected.IsEliminated)
		{
			CompletePolicySignal(signal, "invalid_parties");
			return;
		}
		Kingdom issuerRepresentative = ResolveWorldDiplomacyRepresentative(issuer);
		Kingdom affectedRepresentative = ResolveWorldDiplomacyRepresentative(affected);
		if (issuerRepresentative == null || affectedRepresentative == null || issuerRepresentative == affectedRepresentative)
		{
			CompletePolicySignal(signal, "same_or_invalid_diplomatic_representative");
			return;
		}

		WorldDiplomacyRound activeRound = _storage.ActiveRound;
		if (activeRound != null)
		{
			if (RoundContainsKingdom(activeRound, issuerRepresentative.StringId) || RoundContainsKingdom(activeRound, affectedRepresentative.StringId))
			{
				AttachPolicySignalToRound(activeRound, signal, issuer, affected);
				CompletePolicySignal(signal, "attached_to_active_round");
			}
			return;
		}
		Kingdom author = IsPlayerKingdom(affectedRepresentative) ? issuerRepresentative : affectedRepresentative;
		if (GetActionableDiplomaticTargets(author).Count == 0)
		{
			CompletePolicySignal(signal, "no_actionable_diplomatic_target");
			ScheduleNextNormalRoundAfter(CurrentDay());
			return;
		}
		if (_storage.Jobs.Count > 0 || _llmRequestRunning || !TryConsumeAiDocumentBudget())
		{
			return;
		}

		WorldDiplomacyRound round = EnsureActiveRound(author, null, isPlayerInsertion: false);
		AttachPolicySignalToRound(round, signal, issuer, affected);
		ScheduleNextNormalRoundAfter(CurrentDay());
		EnqueueGenerationJob(author, null, null, isResponse: false, sourceDocument: null,
			priority: 70, roundId: round?.RoundId, allowUntargeted: true);
		CompletePolicySignal(signal, "opened_round");
	}

	private void AttachPolicySignalToRound(WorldDiplomacyRound round, WorldDiplomacyPolicySignal signal, Kingdom issuer, Kingdom affected)
	{
		if (round == null || signal == null || issuer == null || affected == null)
		{
			return;
		}
		round.ExternalSignalKeys ??= new List<string>();
		round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		if (!round.ExternalSignalKeys.Contains(signal.SignalKey, StringComparer.OrdinalIgnoreCase))
		{
			round.ExternalSignalKeys.Add(signal.SignalKey);
		}
		if (!round.AttachedPolicySignals.Any(item => item != null
			&& string.Equals(item.SignalKey, signal.SignalKey, StringComparison.OrdinalIgnoreCase)))
		{
			round.AttachedPolicySignals.Add(ClonePolicySignal(signal));
		}
		string context = BuildPolicySignalContext(signal);
		if (!string.IsNullOrWhiteSpace(context) && (round.ExternalOpeningContext ?? "").IndexOf(signal.SignalKey, StringComparison.OrdinalIgnoreCase) < 0)
		{
			round.ExternalOpeningContext = string.Join("\n", new[] { round.ExternalOpeningContext, context }.Where(text => !string.IsNullOrWhiteSpace(text))).Trim();
		}
		foreach (Kingdom kingdom in new[] { ResolveWorldDiplomacyRepresentative(issuer), ResolveWorldDiplomacyRepresentative(affected) }
			.Where(x => x != null).Distinct())
		{
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdom.StringId, "observer", mandatoryReply: false);
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
		}
	}

	private static WorldDiplomacyPolicySignal ClonePolicySignal(WorldDiplomacyPolicySignal signal)
	{
		if (signal == null) return null;
		return new WorldDiplomacyPolicySignal
		{
			SignalKey = signal.SignalKey ?? "",
			PolicyId = signal.PolicyId ?? "",
			PolicyKind = string.IsNullOrWhiteSpace(signal.PolicyKind) ? "kingdom" : signal.PolicyKind.Trim(),
			PolicyName = signal.PolicyName ?? "",
			PolicySummary = signal.PolicySummary ?? "",
			IssuerKingdomId = signal.IssuerKingdomId ?? "",
			IssuerKingdomName = signal.IssuerKingdomName ?? "",
			TargetKingdomId = signal.TargetKingdomId ?? "",
			TargetKingdomName = signal.TargetKingdomName ?? "",
			DirectEffect = signal.DirectEffect ?? "",
			PublishedDay = Math.Max(0, signal.PublishedDay)
		};
	}

	private static string BuildPolicySignalContext(WorldDiplomacyPolicySignal signal)
	{
		return "【已经发生的公开政策事件】\n"
			+ "事件键=" + (signal.SignalKey ?? "") + "\n"
			+ (signal.IssuerKingdomName ?? signal.IssuerKingdomId) + "已经使《" + (signal.PolicyName ?? "未命名政策") + "》生效，"
			+ "该政策直接影响" + (signal.TargetKingdomName ?? signal.TargetKingdomId) + "。\n"
			+ "政策公开摘要：" + (signal.PolicySummary ?? "") + "\n"
			+ (string.IsNullOrWhiteSpace(signal.DirectEffect) ? "" : "对该国的直接措施：" + signal.DirectEffect + "\n")
			+ "这是已经生效的政策事实，但它尚未自动形成战争、和约、同盟或其他外交结果。统治者可以辩护、评价、反对、要求修改、索取补偿、提出交换条件或借机谋利。";
	}

	private void CompletePolicySignal(WorldDiplomacyPolicySignal signal, string reason)
	{
		if (signal == null)
		{
			return;
		}
		_storage.PendingPolicySignals.RemoveAll(item => item != null && string.Equals(item.SignalKey, signal.SignalKey, StringComparison.OrdinalIgnoreCase));
		_storage.ProcessedPolicySignalKeys.RemoveAll(key => string.Equals(key, signal.SignalKey, StringComparison.OrdinalIgnoreCase));
		_storage.ProcessedPolicySignalKeys.Add(signal.SignalKey ?? "");
		if (_storage.ProcessedPolicySignalKeys.Count > MaxProcessedPolicySignalKeys)
		{
			_storage.ProcessedPolicySignalKeys.RemoveRange(0, _storage.ProcessedPolicySignalKeys.Count - MaxProcessedPolicySignalKeys);
		}
		Log("policy diplomacy signal completed key=" + (signal.SignalKey ?? "") + " reason=" + (reason ?? ""));
	}

	private void TryScheduleNormalRound()
	{
		if (_storage.ActiveRound != null || _storage.Jobs.Count > 0 || _llmRequestRunning)
		{
			return;
		}
		int day = CurrentDay();
		if (day < _storage.NextNormalRoundDay)
		{
			return;
		}
		List<Kingdom> initiators = GetEligibleAiKingdoms();
		if (initiators.Count == 0)
		{
			ScheduleNextNormalRoundAfter(day);
			return;
		}
		int startIndex = Math.Abs(_storage.RotationIndex) % initiators.Count;
		int selectedIndex = -1;
		Kingdom initiator = null;
		for (int offset = 0; offset < initiators.Count; offset++)
		{
			int candidateIndex = (startIndex + offset) % initiators.Count;
			Kingdom candidate = initiators[candidateIndex];
			if (GetActionableDiplomaticTargets(candidate).Count == 0) continue;
			initiator = candidate;
			selectedIndex = candidateIndex;
			break;
		}
		if (initiator == null)
		{
			Log("autonomous diplomacy skipped because no eligible kingdom has an actionable target");
			ScheduleNextNormalRoundAfter(day);
			return;
		}
		_storage.RotationIndex = (selectedIndex + 1) % initiators.Count;
		if (!TryConsumeAiDocumentBudget())
		{
			return;
		}
		WorldDiplomacyRound round = EnsureActiveRound(initiator, null, isPlayerInsertion: false);
		Log("autonomous diplomacy opportunity opened round=" + round.RoundId + " initiator=" + initiator.StringId);
		EnqueueGenerationJob(initiator, null, null, isResponse: false,
			sourceDocument: null, priority: 20, roundId: round?.RoundId, allowUntargeted: true);
	}

	private void EnqueueGenerationJob(
		Kingdom author,
		Kingdom target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		WorldDiplomacyDocument sourceDocument,
		int priority,
		bool externalResponseOnly = false,
		bool isReminder = false,
		string roundId = null,
		bool isRelayTurn = false,
		bool allowUntargeted = false,
		string previousKingdomId = null,
		int scheduledDay = -1,
		string resultSettlementSlotId = null)
	{
		if (author == null || (target == null && !allowUntargeted))
		{
			CompleteExchange(exchange?.ExchangeId, "invalid_generation_parties");
			WorldDiplomacyRound invalidRound = ResolveRound(FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
			if (invalidRound?.ResultSettlementPending == true)
			{
				SkipResultSettlementSlot(invalidRound, resultSettlementSlotId, author?.StringId, "invalid_generation_parties");
				ScheduleNextResultSettlementTurn(invalidRound);
			}
			return;
		}
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
		PruneInvalidOffers(owningRound);
		bool isResultSettlementTurn = owningRound?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(resultSettlementSlotId);
		if (!CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			Log("generation blocked by author authority author=" + (author.StringId ?? "")
				+ " reason=" + authorBlockReason + " source=" + (sourceDocument?.DocumentId ?? ""));
			CompleteExchange(exchange?.ExchangeId, authorBlockReason);
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, author.StringId, authorBlockReason);
					ScheduleNextResultSettlementTurn(owningRound);
				}
				else AdvanceRelay(owningRound);
			}
			return;
		}
		if (!HasIndependentWorldDiplomacyAuthority(author))
		{
			Log("generation skipped for diplomatically controlled vassal author=" + (author.StringId ?? "")
				+ " round=" + (owningRound?.RoundId ?? ""));
			CompleteExchange(exchange?.ExchangeId, "controlled_vassal_has_no_diplomatic_authority");
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, author.StringId, "controlled_vassal");
					ScheduleNextResultSettlementTurn(owningRound);
				}
				else AdvanceRelay(owningRound);
			}
			return;
		}
		bool playerPriorityResponse = externalResponseOnly && sourceDocument?.IsPlayerAuthored == true;
		List<Kingdom> actionableTargets;
		if (playerPriorityResponse)
		{
			Kingdom priorityTarget = ResolveKingdom(sourceDocument.AuthorKingdomId) ?? target;
			actionableTargets = priorityTarget != null
				&& BuildLegalDiplomaticDeclarationIntents(
					owningRound,
					author,
					priorityTarget,
					isRelayTurn,
					resultSettlementSlotId,
					isExternalResponseOnly: true,
					responseSource: sourceDocument).Count > 0
				? new List<Kingdom> { priorityTarget }
				: new List<Kingdom>();
		}
		else if (isRelayTurn && owningRound != null)
		{
			actionableTargets = isResultSettlementTurn
				? GetResultSettlementActionableTargets(owningRound, author)
				: (owningRound.RelayRouteKingdomIds ?? new List<string>())
					.Select(ResolveKingdom)
					.Where(x => x != null && x != author && !x.IsEliminated && HasIndependentWorldDiplomacyAuthority(x))
					.Where(x => BuildLegalDiplomaticDeclarationIntents(
						owningRound, author, x, isRelayTurn: true,
						resultSettlementSlotId: resultSettlementSlotId,
						isExternalResponseOnly: externalResponseOnly,
						responseSource: sourceDocument).Count > 0)
					.Distinct()
					.ToList();
		}
		else if (target != null)
		{
			actionableTargets = BuildLegalDiplomaticActionIntents(owningRound, author, target).Count > 0
				? new List<Kingdom> { target }
				: new List<Kingdom>();
		}
		else
		{
			actionableTargets = GetActionableDiplomaticTargets(author, owningRound);
		}
		if (actionableTargets.Count == 0)
		{
			Log("generation skipped because no actionable diplomatic target remains author=" + author.StringId
				+ " round=" + (owningRound?.RoundId ?? ""));
			CompleteExchange(exchange?.ExchangeId, "no_actionable_diplomatic_target");
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				if (owningRound.ResultSettlementPending)
				{
					SkipResultSettlementSlot(owningRound, resultSettlementSlotId, author.StringId, "no_actionable_target");
					ScheduleNextResultSettlementTurn(owningRound);
				}
				else
				{
					WorldDiplomacyRoundParticipant participant = (owningRound.Participants ?? new List<WorldDiplomacyRoundParticipant>())
						.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
					if (participant != null && !participant.MandatoryReplyPending) participant.State = "withdrawn";
					AdvanceRelay(owningRound);
				}
			}
			else if (owningRound != null && string.IsNullOrWhiteSpace(owningRound.RootDocumentId))
			{
				CloseActiveRound("technical_no_actionable_diplomatic_target");
			}
			return;
		}
		if (owningRound != null)
		{
			if (!playerPriorityResponse && !isResultSettlementTurn
				&& (owningRound.AutomaticCircuitBreakerTripped || owningRound.AutomaticDocumentsStarted >= MaxAutomaticDocumentsPerRound))
			{
				TripAutomaticRoundCircuitBreaker(owningRound, "automatic_document_limit");
				CompleteExchange(exchange?.ExchangeId, "automatic_round_circuit_breaker");
				return;
			}
		}
		string frozenCommonContract = GetCommonDiplomacyContract(owningRound);
		string systemPrompt = isRelayTurn
			? BuildRelayGenerationSystemPrompt(frozenCommonContract)
			: BuildGenerationSystemPrompt(frozenCommonContract);
		SyncCanonicalHistorySources();
		bool includeEmbeddedRoundPlan = !isRelayTurn && !isResponse && owningRound != null && string.IsNullOrWhiteSpace(owningRound.RootDocumentId);
		List<string> roundPlanCandidates = new List<string>();
		if (includeEmbeddedRoundPlan)
		{
			roundPlanCandidates = actionableTargets
				.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.StringId)
				.ToList();
		}
		string dynamicPrompt = isRelayTurn
			? BuildRelayConversationTurnPrompt(owningRound, author, target,
				prioritySource: sourceDocument, priorityResponseOnly: externalResponseOnly)
			: BuildGenerationPrompt(author, target, exchange, isResponse, sourceDocument, isReminder, roundId,
				allowUntargeted, roundPlanCandidates, externalResponseOnly);
		string userPrompt = BuildDeclareModePrompt(dynamicPrompt);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_generate"),
			Kind = "generate",
			Priority = priority,
			CreatedDay = scheduledDay >= 0 ? scheduledDay : CurrentDay(),
			ExchangeId = exchange?.ExchangeId ?? roundId ?? "",
			RoundId = FirstNonEmpty(roundId, exchange?.ExchangeId),
			AuthorKingdomId = author.StringId,
			TargetKingdomId = target?.StringId ?? "",
			SourceDocumentId = sourceDocument?.DocumentId ?? "",
			IsResponse = isResponse,
			ForcedIntent = "",
			IsExternalResponseOnly = externalResponseOnly,
			IsReminder = isReminder,
			IsRelayTurn = isRelayTurn,
			AllowUntargeted = allowUntargeted,
			PreviousKingdomId = previousKingdomId ?? "",
			ResultSettlementSlotId = resultSettlementSlotId ?? "",
			AllowAutonomousNoAction = false,
			CandidateKingdomIds = isResultSettlementTurn
				? actionableTargets.Select(x => x.StringId).ToList()
				: roundPlanCandidates,
			PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(author.StringId),
			PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(author.StringId),
			WasAtWarWhenQueued = target != null && FactionManager.IsAtWarAgainstFaction(author, target),
			SystemPrompt = systemPrompt,
			UserPrompt = userPrompt,
			CacheAffinityKey = CanonicalHistoryCacheAffinityKey,
			ProfiledKingdomId = "",
			MaxTokens = GenerationMaxTokens
		};
		job.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(job);
		if (!EnsureGenerationJobHasKingdomStrategicProfile(job))
		{
			AbandonRejectedGeneration(job, author, target, "missing_kingdom_strategic_profile");
			return;
		}
		CaptureCanonicalHistoryForJob(job, syncSources: false);
		if (owningRound != null && !playerPriorityResponse) owningRound.AutomaticDocumentsStarted++;
		EnqueueJob(job);
	}

	private bool EnsureGenerationJobHasKingdomStrategicProfile(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return true;
		string authorId = (job.AuthorKingdomId ?? "").Trim();
		if (string.IsNullOrEmpty(authorId)) return false;
		string marker = BuildKingdomStrategicProfileMarker(authorId);
		Kingdom author = ResolveKingdom(authorId);
		if (!TryBuildKingdomStrategicProfilePrompt(author, marker, out string profilePrompt)) return false;
		if (string.Equals(job.StrategicProfileKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
			&& GenerationJobContainsKingdomStrategicProfile(job, authorId, marker, profilePrompt)) return true;
		job.StrategicProfileKingdomId = "";
		if (GenerationJobContainsKingdomStrategicProfile(job, authorId, marker, profilePrompt))
		{
			job.StrategicProfileKingdomId = authorId;
			return true;
		}
		if (job.LlmMessages?.Count > 0)
		{
			for (int index = job.LlmMessages.Count - 1; index >= 0; index--)
			{
				WorldDiplomacyLlmMessage message = job.LlmMessages[index];
				if (message == null || !string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) continue;
				message.Content = UpsertKingdomStrategicProfilePrompt(message.Content, profilePrompt, authorId);
				message.StrategicProfileKingdomId = authorId;
				job.UserPrompt = message.Content;
				job.StrategicProfileKingdomId = authorId;
				LogKingdomStrategicProfileInjection(job, profilePrompt);
				return true;
			}
			job.LlmMessages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = profilePrompt, StrategicProfileKingdomId = authorId });
			job.UserPrompt = profilePrompt;
			job.StrategicProfileKingdomId = authorId;
			LogKingdomStrategicProfileInjection(job, profilePrompt);
			return true;
		}
		job.UserPrompt = UpsertKingdomStrategicProfilePrompt(job.UserPrompt, profilePrompt, authorId);
		job.StrategicProfileKingdomId = authorId;
		LogKingdomStrategicProfileInjection(job, profilePrompt);
		return true;
	}

	private static void LogKingdomStrategicProfileInjection(WorldDiplomacyJob job, string profilePrompt)
	{
		Log("strategic profile injected job=" + (job?.JobId ?? "")
			+ " author=" + (job?.AuthorKingdomId ?? "")
			+ " relay=" + (job?.IsRelayTurn == true).ToString()
			+ " chars=" + (profilePrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
	}

	private static bool GenerationJobContainsKingdomStrategicProfile(WorldDiplomacyJob job, string authorId, string marker, string currentProfilePrompt)
	{
		if (job == null || string.IsNullOrEmpty(authorId) || string.IsNullOrEmpty(marker) || string.IsNullOrEmpty(currentProfilePrompt)) return false;
		if (job.LlmMessages?.Count > 0)
		{
			for (int index = job.LlmMessages.Count - 1; index >= 0; index--)
			{
				WorldDiplomacyLlmMessage message = job.LlmMessages[index];
				if (message == null || !string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase)) continue;
				string content = message.Content ?? "";
				int markerIndex = content.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
				if (markerIndex < 0) continue;
				if (content.IndexOf(currentProfilePrompt, markerIndex, StringComparison.Ordinal) != markerIndex) return false;
				message.StrategicProfileKingdomId = authorId;
				return true;
			}
			return false;
		}
		string userPrompt = job.UserPrompt ?? "";
		int promptMarkerIndex = userPrompt.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
		return promptMarkerIndex >= 0
			&& userPrompt.IndexOf(currentProfilePrompt, promptMarkerIndex, StringComparison.Ordinal) == promptMarkerIndex;
	}

	private static string BuildKingdomStrategicProfileMarker(string kingdomId)
	{
		return KingdomStrategicProfileMarkerPrefix + (kingdomId ?? "").Trim() + "】";
	}

	private static bool TryBuildKingdomStrategicProfilePrompt(Kingdom kingdom, string marker, out string prompt)
	{
		prompt = "";
		KingdomStrategicProfileBehavior profiles = KingdomStrategicProfileBehavior.Instance;
		if (kingdom == null || profiles == null
			|| !profiles.TryGetOrCreateEffectiveProfile(kingdom, out string nationalPersonality, out string longTermStrategy))
		{
			return false;
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(marker);
		sb.AppendLine("档案版本=" + StablePromptHash((nationalPersonality ?? "") + "\n" + (longTermStrategy ?? "")));
		sb.AppendLine("国家性格=" + (nationalPersonality ?? ""));
		sb.AppendLine("长期战略=" + (longTermStrategy ?? ""));
		sb.Append(KingdomStrategicIntentRule);
		prompt = sb.ToString();
		return true;
	}

	private static string UpsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId)
	{
		if (string.IsNullOrEmpty(existing)) return profilePrompt ?? "";
		string marker = BuildKingdomStrategicProfileMarker(authorId);
		int markerIndex = existing.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		int ruleIndex = existing.IndexOf(KingdomStrategicIntentRule, markerIndex, StringComparison.Ordinal);
		if (ruleIndex < 0) return InsertKingdomStrategicProfilePrompt(existing, profilePrompt, authorId);
		int endIndex = ruleIndex + KingdomStrategicIntentRule.Length;
		string prefix = existing.Substring(0, markerIndex);
		string suffix = existing.Substring(endIndex).TrimStart('\r', '\n');
		if (!prefix.EndsWith("\n", StringComparison.Ordinal)) prefix += "\n";
		return prefix + profilePrompt.TrimEnd('\r', '\n') + "\n" + suffix;
	}

	private static string InsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId)
	{
		if (string.IsNullOrEmpty(existing)) return profilePrompt ?? "";
		if (string.IsNullOrEmpty(profilePrompt)) return existing;

		int insertionIndex = FindKingdomStrategicProfileInsertionIndex(existing, authorId);
		if (insertionIndex < 0)
		{
			return existing.EndsWith("\n", StringComparison.Ordinal)
				? existing + "\n" + profilePrompt
				: existing + "\n\n" + profilePrompt;
		}

		string prefix = existing.Substring(0, insertionIndex);
		string suffix = existing.Substring(insertionIndex);
		if (!prefix.EndsWith("\n", StringComparison.Ordinal)) prefix += "\n";
		return prefix + profilePrompt.TrimEnd('\r', '\n') + "\n" + suffix;
	}

	private static int FindKingdomStrategicProfileInsertionIndex(string prompt, string authorId)
	{
		if (string.IsNullOrEmpty(prompt)) return -1;

		const string institutionalHeading = "【发文国制度、合法性与礼制声音】";
		const string familyHeading = "【权威人物与亲属关系】";
		int institutionalIndex = prompt.IndexOf(institutionalHeading, StringComparison.Ordinal);
		if (institutionalIndex >= 0)
		{
			int familyIndex = prompt.IndexOf(familyHeading, institutionalIndex + institutionalHeading.Length, StringComparison.Ordinal);
			if (familyIndex >= 0) return familyIndex;
		}

		const string actorProfileHeading = "【本发布国首次进入公文链的稳定决策档案】";
		int actorProfileIndex = prompt.IndexOf(actorProfileHeading, StringComparison.Ordinal);
		if (actorProfileIndex >= 0)
		{
			int actorFamilyIndex = prompt.IndexOf("\n王室与亲属=", actorProfileIndex + actorProfileHeading.Length, StringComparison.Ordinal);
			if (actorFamilyIndex >= 0) return actorFamilyIndex + 1;
		}

		const string relayProfileHeading = "【本发布国当前决策档案】";
		int relayProfileIndex = prompt.IndexOf(relayProfileHeading, StringComparison.Ordinal);
		if (relayProfileIndex >= 0)
		{
			int relayFamilyIndex = prompt.IndexOf("\n王室与亲属=", relayProfileIndex + relayProfileHeading.Length, StringComparison.Ordinal);
			if (relayFamilyIndex >= 0) return relayFamilyIndex + 1;
		}

		string normalizedAuthorId = (authorId ?? "").Trim();
		if (normalizedAuthorId.Length > 0)
		{
			string participantAnchor = "-- " + normalizedAuthorId + "=";
			int participantIndex = prompt.IndexOf(participantAnchor, StringComparison.OrdinalIgnoreCase);
			if (participantIndex >= 0)
			{
				int participantEnd = prompt.IndexOf("\n-- ", participantIndex + participantAnchor.Length, StringComparison.Ordinal);
				if (participantEnd < 0) participantEnd = prompt.Length;
				int participantInstitutionIndex = prompt.IndexOf("\n国家制度与礼制声音=", participantIndex, StringComparison.Ordinal);
				if (participantInstitutionIndex >= 0 && participantInstitutionIndex < participantEnd)
				{
					int participantFamilyIndex = prompt.IndexOf("\n王室与亲属=", participantInstitutionIndex, StringComparison.Ordinal);
					return participantFamilyIndex >= 0 && participantFamilyIndex < participantEnd
						? participantFamilyIndex + 1
						: participantEnd;
				}
			}
		}

		// Keep MODE at the actual tail even for an unfamiliar dynamic prompt layout.
		// This also prevents a late strategic profile from separating the model from
		// the final, live legal-intent list immediately above MODE.
		return prompt.LastIndexOf("【MODE=DECLARE】", StringComparison.Ordinal);
	}

	private void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority)
	{
		if (document == null)
		{
			return;
		}
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(document.RoundId, document.ExchangeId));
		string frozenCommonContract = GetCommonDiplomacyContract(owningRound);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_analyze"),
			Kind = "analyze",
			Priority = priority,
			CreatedDay = CurrentDay(),
			ExchangeId = document.ExchangeId ?? "",
			DocumentId = document.DocumentId ?? "",
			AuthorKingdomId = document.AuthorKingdomId ?? "",
			TargetKingdomId = document.TargetKingdomId ?? "",
			PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(document.AuthorKingdomId),
			PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(document.AuthorKingdomId),
			IsResponse = document.IsResponse,
			SystemPrompt = BuildAnalysisSystemPrompt(frozenCommonContract),
			UserPrompt = BuildAnalysisPrompt(document),
			CacheAffinityKey = "analyze",
			MaxTokens = AnalysisMaxTokens
		};
		EnqueueJob(job);
	}

	private void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)
	{
		int batchSequence = Math.Max(0, _storage.CompressionSequence) + 1;
		string batchId = "diplomacy_compaction_" + batchSequence.ToString(CultureInfo.InvariantCulture);
		int overallTargetTokens = Math.Max(1, targetTokens);
		int protectedBudgetTokens = Math.Max(0, Math.Min(overallTargetTokens - 256, overallTargetTokens / 4));
		List<WorldDiplomacyCanonicalProtectedFact> protectedFacts = SelectCanonicalProtectedFactsWithinTokenBudget(
			BuildCanonicalProtectedFactsThrough(throughSequence), protectedBudgetTokens);
		List<string> preservedResultIds = protectedFacts
			.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
			.Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		long protectedTokens = EstimateHistoryTokens(RenderCanonicalProtectedFacts(protectedFacts, preservedResultIds));
		int configuredOutputTokenLimit = WorldDiplomacyLlmClient.GetConfiguredOutputTokenLimit();
		int outputTokenReserve = Math.Min(CompressionOutputTokenReserve, Math.Max(128, configuredOutputTokenLimit / 8));
		int outputSummaryCapacity = Math.Max(256, configuredOutputTokenLimit - outputTokenReserve);
		long desiredSummaryTokens = Math.Max(256L, overallTargetTokens - protectedTokens - 32L);
		int summaryTargetTokens = (int)Math.Min(desiredSummaryTokens, outputSummaryCapacity);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_compress"),
			Kind = "compress",
			Priority = CompressionJobPriority,
			CreatedDay = CurrentDay(),
			CompressionBatchId = batchId,
			CompressionTokenCount = Math.Max(0L, tokenCount),
			CompressionThroughSequence = Math.Max(0L, throughSequence),
			CompressionOverallTargetTokens = overallTargetTokens,
			CompressionTargetTokens = summaryTargetTokens,
			SystemPrompt = BuildCanonicalHistorySystemPrompt(BuildCommonDiplomacySystemPrefix()),
			UserPrompt = BuildTokenCompressionPrompt(batchId, throughSequence, tokenCount, summaryTargetTokens, protectedTokens),
			CacheAffinityKey = CanonicalHistoryCacheAffinityKey,
			MaxTokens = Math.Min(configuredOutputTokenLimit, summaryTargetTokens + outputTokenReserve)
		};
		CaptureCanonicalHistoryForJob(job, syncSources: false);
		EnqueueJob(job);
		Log("token compression queued batch=" + batchId
			+ " through_sequence=" + throughSequence.ToString(CultureInfo.InvariantCulture)
			+ " estimated_tokens=" + tokenCount.ToString(CultureInfo.InvariantCulture)
			+ " overall_target_tokens=" + overallTargetTokens.ToString(CultureInfo.InvariantCulture)
			+ " protected_tokens=" + protectedTokens.ToString(CultureInfo.InvariantCulture)
			+ " summary_target_tokens=" + summaryTargetTokens.ToString(CultureInfo.InvariantCulture)
			+ " configured_output_token_limit=" + configuredOutputTokenLimit.ToString(CultureInfo.InvariantCulture)
			+ " request_max_tokens=" + job.MaxTokens.ToString(CultureInfo.InvariantCulture));
	}

	private void EnqueueJob(WorldDiplomacyJob job)
	{
		if (job == null || string.IsNullOrWhiteSpace(job.JobId))
		{
			return;
		}
		if (_storage.Jobs.Any(x => x != null && string.Equals(x.JobId, job.JobId, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		_storage.Jobs.Add(job);
		int queueCapacity = MaxPendingJobs + (_storage.Jobs.Any(x => x != null
			&& string.Equals(x.Kind, "compress", StringComparison.OrdinalIgnoreCase)) ? 1 : 0);
		_storage.Jobs = _storage.Jobs
			.Where(x => x != null)
			.OrderByDescending(x => x.Priority)
			.ThenBy(x => x.CreatedDay)
			.ThenBy(x => x.JobId, StringComparer.OrdinalIgnoreCase)
			.Take(queueCapacity)
			.ToList();
	}

	private static string ResolveCacheAffinityKey(WorldDiplomacyJob job)
	{
		if (!string.IsNullOrWhiteSpace(job?.CacheAffinityKey))
		{
			return job.CacheAffinityKey.Trim();
		}
		string kind = (job?.Kind ?? "unknown").Trim().ToLowerInvariant();
		return kind == "generate" ? kind + ":" + (job?.AuthorKingdomId ?? "") : kind;
	}

	private void LogPromptCacheShape(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = BuildLlmMessagesForJob(job);
		string system = messages.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string user = messages.LastOrDefault(x => x != null && string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string frozenContract = ResolveCommonContractForCacheDiagnostics(job, out string contractSource);
		int userPrefix1024Chars = Math.Min(1024, user.Length);
		int userPrefixChars = Math.Min(2048, user.Length);
		int totalChars = messages.Sum(x => x?.Content?.Length ?? 0);
		int expectedCachedMessageCount = UsesCanonicalHistory(job) && messages.Count >= 2 ? 2 : 0;
		int expectedCachedPrefixChars = messages.Take(expectedCachedMessageCount).Sum(x => x?.Content?.Length ?? 0);
		Log("cache-shape kind=" + (job?.Kind ?? "")
			+ " affinity=" + ResolveCacheAffinityKey(job)
			+ " messages=" + messages.Count.ToString(CultureInfo.InvariantCulture)
			+ " totalChars=" + totalChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedMessages=" + expectedCachedMessageCount.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixChars=" + expectedCachedPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixHash=" + StablePromptHashMessagePrefix(messages, expectedCachedMessageCount)
			+ " historyRevision=" + (job?.HistoryRevision ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " historyThroughSequence=" + (job?.HistoryThroughSequence ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " historyEstimatedTokens=" + (job?.HistoryEstimatedTokens ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " snapshotThroughSequence=" + (job?.HistorySnapshotThroughSequence ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " snapshotHash=" + (job?.HistorySnapshotHash ?? "")
			+ " stablePrefixHash=" + (job?.HistoryPrefixHash ?? "")
			+ " contractSource=" + contractSource
			+ " contractState=" + (frozenContract.Length == 0 ? "empty" : "present")
			+ " contractChars=" + frozenContract.Length.ToString(CultureInfo.InvariantCulture)
			+ " contractHash=" + StablePromptHash(frozenContract)
			+ " contractAtTop=" + (frozenContract.Length == 0 ? "n/a_empty" : system.StartsWith(frozenContract, StringComparison.Ordinal).ToString())
			+ " systemChars=" + system.Length.ToString(CultureInfo.InvariantCulture)
			+ " systemHash=" + StablePromptHash(system)
			+ " userChars=" + user.Length.ToString(CultureInfo.InvariantCulture)
			+ " userPrefix1024Hash=" + StablePromptHash(userPrefix1024Chars <= 0 ? "" : user.Substring(0, userPrefix1024Chars))
			+ " userPrefixChars=" + userPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " userPrefixHash=" + StablePromptHash(userPrefixChars <= 0 ? "" : user.Substring(0, userPrefixChars)));
	}

	private void LogPromptCacheUsage(WorldDiplomacyJob job, LlmJobResult result)
	{
		bool usageKnown = result?.PromptCacheHitTokens.HasValue == true
			&& (result.PromptTokens.HasValue
				|| result.PromptCacheMissTokens.HasValue
				|| (result.PromptCacheCreationTokens.HasValue && result.PromptUncachedTokens.HasValue));
		bool breakdownKnown = result?.PromptCacheHitTokens.HasValue == true
			&& result.PromptCacheCreationTokens.HasValue
			&& result.PromptUncachedTokens.HasValue;
		int hit = Math.Max(0, result?.PromptCacheHitTokens ?? 0);
		int creation = Math.Max(0, result?.PromptCacheCreationTokens ?? 0);
		int uncached = Math.Max(0, result?.PromptUncachedTokens ?? 0);
		int denominator = result?.PromptTokens.HasValue == true
			? Math.Max(0, result.PromptTokens.Value)
			: result?.PromptCacheMissTokens.HasValue == true
				? hit + Math.Max(0, result.PromptCacheMissTokens.Value)
				: hit + creation + uncached;
		string rate = !usageKnown || denominator <= 0 ? "n/a" : (100d * hit / denominator).ToString("F1", CultureInfo.InvariantCulture) + "%";
		Log("cache-usage kind=" + (job?.Kind ?? "")
			+ " affinity=" + ResolveCacheAffinityKey(job)
			+ " prompt_tokens=" + (result?.PromptTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " completion_tokens=" + (result?.CompletionTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_cache_hit_tokens=" + (result?.PromptCacheHitTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_cache_miss_tokens=" + (result?.PromptCacheMissTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_cache_creation_tokens=" + (result?.PromptCacheCreationTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_uncached_tokens=" + (result?.PromptUncachedTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " cache_usage_known=" + usageKnown.ToString()
			+ " cache_breakdown_known=" + breakdownKnown.ToString()
			+ " hit_rate=" + rate);
		if (usageKnown)
		{
			_cacheHitTokensThisSession += hit;
			_cacheMissTokensThisSession += Math.Max(0, denominator - hit);
		}
		if (usageKnown && job?.IsRelayTurn == true)
		{
			_relayCacheHitTokensThisSession += hit;
			_relayCacheMissTokensThisSession += Math.Max(0, denominator - hit);
		}
		long overall = _cacheHitTokensThisSession + _cacheMissTokensThisSession;
		long relay = _relayCacheHitTokensThisSession + _relayCacheMissTokensThisSession;
		Log("cache-session overall_hit_rate=" + (overall <= 0 ? "n/a" : (100d * _cacheHitTokensThisSession / overall).ToString("F1", CultureInfo.InvariantCulture) + "%")
			+ " relay_hit_rate=" + (relay <= 0 ? "n/a" : (100d * _relayCacheHitTokensThisSession / relay).ToString("F1", CultureInfo.InvariantCulture) + "%"));
	}

	private static string StablePromptHash(string text)
	{
		unchecked
		{
			ulong hash = AppendStablePromptHash(1469598103934665603UL, text);
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static string StablePromptHashPair(string first, string second)
	{
		unchecked
		{
			ulong hash = AppendStablePromptHash(1469598103934665603UL, first);
			hash = AppendStablePromptHash(hash, "\n");
			hash = AppendStablePromptHash(hash, second);
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static string StablePromptHashMessagePrefix(IReadOnlyList<WorldDiplomacyLlmMessage> messages, int messageCount)
	{
		unchecked
		{
			ulong hash = 1469598103934665603UL;
			int count = Math.Min(Math.Max(0, messageCount), messages?.Count ?? 0);
			for (int i = 0; i < count; i++)
			{
				if (i > 0) hash = AppendStablePromptHash(hash, "\n");
				WorldDiplomacyLlmMessage message = messages[i];
				hash = AppendStablePromptHash(hash, message?.Role);
				hash = AppendStablePromptHash(hash, ":");
				hash = AppendStablePromptHash(hash, message?.Content);
			}
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static ulong AppendStablePromptHash(ulong hash, string text)
	{
		unchecked
		{
			foreach (char ch in text ?? "")
			{
				hash ^= ch;
				hash *= 1099511628211UL;
			}
			return hash;
		}
	}

	private static List<WorldDiplomacyLlmMessage> CloneLlmMessages(IEnumerable<WorldDiplomacyLlmMessage> messages)
	{
		return (messages ?? Enumerable.Empty<WorldDiplomacyLlmMessage>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Role))
			.Select(x => new WorldDiplomacyLlmMessage
			{
				Role = x.Role,
				Content = x.Content ?? "",
				StrategicProfileKingdomId = x.StrategicProfileKingdomId ?? ""
			})
			.ToList();
	}

	private static bool UsesCanonicalHistory(WorldDiplomacyJob job)
	{
		return job != null && (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase));
	}

	private List<WorldDiplomacyLlmMessage> BuildLlmMessagesForJob(WorldDiplomacyJob job)
	{
		if (IsValidSemanticRepairMessageChain(job)) return job.LlmMessages;
		List<WorldDiplomacyLlmMessage> source = new List<WorldDiplomacyLlmMessage>
		{
			new WorldDiplomacyLlmMessage { Role = "system", Content = job?.SystemPrompt ?? "" }
		};
		if (UsesCanonicalHistory(job))
		{
			source.Add(new WorldDiplomacyLlmMessage
			{
				Role = "system",
				Content = BuildCanonicalHistoryBlock(job?.HistoryThroughSequence ?? long.MaxValue)
			});
		}
		source.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = job?.UserPrompt ?? "" });
		return source;
	}

	private static bool IsValidSemanticRepairMessageChain(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = job?.LlmMessages;
		if (job == null
			|| job.SemanticRepairAttempts <= 0
			|| job.SemanticRepairAttempts > MaxGeneratedDraftRepairAttempts
			|| messages == null
			|| messages.Count != 3 + 2 * job.SemanticRepairAttempts
			|| !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyLlmMessage first = messages[0];
		WorldDiplomacyLlmMessage history = messages[1];
		WorldDiplomacyLlmMessage originalTail = messages[2];
		WorldDiplomacyLlmMessage rejected = messages[messages.Count - 2];
		WorldDiplomacyLlmMessage correction = messages[messages.Count - 1];
		return string.Equals(first?.Role, "system", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first?.Content ?? "", job.SystemPrompt ?? "", StringComparison.Ordinal)
			&& string.Equals(history?.Role, "system", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(StablePromptHashPair(first?.Content, history?.Content), job.HistoryPrefixHash ?? "", StringComparison.Ordinal)
			&& string.Equals(originalTail?.Role, "user", StringComparison.OrdinalIgnoreCase)
			&& (originalTail?.Content ?? "").IndexOf("【MODE=DECLARE】", StringComparison.Ordinal) >= 0
			&& string.Equals(rejected?.Role, "assistant", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(correction?.Role, "user", StringComparison.OrdinalIgnoreCase)
			&& (correction?.Content ?? "").IndexOf("【MODE=DECLARE】", StringComparison.Ordinal) >= 0
			&& string.Equals(correction?.Content ?? "", job.UserPrompt ?? "", StringComparison.Ordinal);
	}

	private JArray BuildLlmMessageArray(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> source = BuildLlmMessagesForJob(job);
		JArray messages = new JArray();
		foreach (WorldDiplomacyLlmMessage message in source.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Role)))
		{
			messages.Add(new JObject
			{
				["role"] = message.Role,
				["content"] = message.Content ?? ""
			});
		}
		return messages;
	}

	private void TryStartNextLlmJob()
	{
		if (!IsWorldDiplomacyEnabled() || _llmRequestRunning || _storage.Jobs.Count == 0)
		{
			return;
		}
		int hour = CurrentHour();
		if (_storage.ServiceCooldownUntilHour > hour)
		{
			return;
		}
		List<WorldDiplomacyJob> runnable = _storage.Jobs.Where(x => x != null && !x.IsRunning).ToList();
		int highestPriority = runnable.Count == 0 ? int.MinValue : runnable.Max(x => x.Priority);
		WorldDiplomacyJob job = runnable
			.Where(x => x.Priority == highestPriority)
			.OrderByDescending(x => string.Equals(ResolveCacheAffinityKey(x), _lastLlmCacheAffinityKey, StringComparison.OrdinalIgnoreCase))
			.ThenBy(x => x.CreatedDay)
			.ThenBy(x => x.JobId, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault();
		if (job == null)
		{
			return;
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& HasStaleDiplomaticThreatPresentation(job))
		{
			if (!RefreshDiplomaticThreatPresentationAndPrompt(job))
			{
				CommitFailedJob(job, "stale diplomatic threat presentation could not be rebuilt");
				return;
			}
			Log("refreshed queued generation for current diplomatic threat stage job=" + job.JobId
				+ " author=" + job.AuthorKingdomId);
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& HasStaleDiplomaticActionPresentation(job))
		{
			if (!RefreshDiplomaticActionPresentationAndPrompt(job))
			{
				CommitFailedJob(job, "stale diplomatic action list could not be rebuilt");
				return;
			}
			Log("refreshed queued generation for current legal diplomatic actions job=" + job.JobId
				+ " author=" + job.AuthorKingdomId);
		}
		if (!EnsureCurrentCanonicalPromptContractBeforeSend(job))
		{
			return;
		}
		if (job.LlmMessages?.Count > 0 && !IsValidSemanticRepairMessageChain(job))
		{
			Log("retired invalid persisted LLM message chain job=" + (job.JobId ?? "") + " kind=" + (job.Kind ?? ""));
			job.LlmMessages.Clear();
			job.SemanticRepairAttempts = 0;
			if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& !TryRebuildPendingWorldDiplomacyJob(job))
			{
				CommitFailedJob(job, "invalid persisted LLM message chain could not be rebuilt");
				return;
			}
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& !CanAiAuthorDiplomaticDocument(ResolveKingdom(job.AuthorKingdomId), out string authorBlockReason))
		{
			Log("queued generation cancelled before request job=" + job.JobId + " author=" + (job.AuthorKingdomId ?? "")
				+ " reason=" + authorBlockReason);
			AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), authorBlockReason);
			RemoveJob(job.JobId);
			return;
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& !EnsureGenerationJobHasKingdomStrategicProfile(job))
		{
			AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), "missing_kingdom_strategic_profile");
			RemoveJob(job.JobId);
			return;
		}
		if (string.IsNullOrWhiteSpace(job.SystemPrompt))
		{
			CommitFailedJob(job, "empty prompt");
			return;
		}
		if (!WorldDiplomacyLlmClient.IsConfigured(out string configError))
		{
			CommitFailedJob(job, "api not configured: " + configError);
			return;
		}
		if (!TryConsumeDiplomacyLlmRequestBudget())
		{
			return;
		}
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			if (!EnsureGenerationJobHasKingdomStrategicProfile(job))
			{
				AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), "missing_kingdom_strategic_profile");
				RemoveJob(job.JobId);
				return;
			}
			// Ordinary queued generations consume the newest committed archive at actual send time.
			// Semantic repairs carry explicit messages and intentionally retain their rejected
			// request's frozen prefix.
			if (job.LlmMessages == null || job.LlmMessages.Count == 0)
			{
				CaptureCanonicalHistoryForJob(job, syncSources: true);
			}
		}
		JArray requestMessages = BuildLlmMessageArray(job);
		job.IsRunning = true;
		job.CacheAffinityKey = ResolveCacheAffinityKey(job);
		_lastLlmCacheAffinityKey = job.CacheAffinityKey;
		LogPromptCacheShape(job);
		_llmRequestRunning = true;
		_activeJobId = job.JobId;
		long generation = _runtimeGeneration;
		_activeRequestRuntimeGeneration = generation;
		int requestTimeoutMilliseconds = string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase)
			? DuelSettings.LlmRequestTimeoutMilliseconds
			: DefaultApiTimeoutMilliseconds;
		_ = Task.Run(async delegate
		{
			LlmJobResult result = new LlmJobResult
			{
				JobId = job.JobId,
				RuntimeGeneration = generation
			};
			try
			{
				WorldDiplomacyApiCallResult api = await WorldDiplomacyLlmClient.CallMessagesWithRetriesAsync(
					requestMessages,
					Math.Max(256, job.MaxTokens),
					requestTimeoutMilliseconds,
					Source,
					generation,
					maxAttempts: 2);
				result.Success = api?.Success == true;
				result.Content = api?.Content ?? "";
				result.Error = api?.ErrorMessage ?? "";
				result.IsServiceFailure = api == null || api.IsTimeout || api.IsRateLimit || api.IsQuotaLimit || api.IsAuthFailure;
				result.IsOutputTruncated = api?.IsOutputTruncated == true;
				result.PromptTokens = api?.PromptTokens;
				result.CompletionTokens = api?.CompletionTokens;
				result.PromptCacheHitTokens = api?.PromptCacheHitTokens;
				result.PromptCacheMissTokens = api?.PromptCacheMissTokens;
				result.PromptCacheCreationTokens = api?.PromptCacheCreationTokens;
				result.PromptUncachedTokens = api?.PromptUncachedTokens;
			}
			catch (Exception ex)
			{
				result.Error = ex.ToString();
				result.IsServiceFailure = true;
			}
			_completedJobs.Enqueue(result);
		});
	}

	private static bool HasCurrentCanonicalPromptContract(WorldDiplomacyJob job)
	{
		if (!UsesCanonicalHistory(job)) return true;
		string expectedMode = string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase)
			? "【MODE=COMPACT】"
			: "【MODE=DECLARE】";
		string modePrompt = job.SemanticRepairAttempts > 0 && job.LlmMessages?.Count > 0
			? job.LlmMessages[job.LlmMessages.Count - 1]?.Content ?? ""
			: job.UserPrompt ?? "";
		string systemPrompt = job.SystemPrompt ?? "";
		return string.Equals((job.CacheAffinityKey ?? "").Trim(), CanonicalHistoryCacheAffinityKey, StringComparison.Ordinal)
			&& systemPrompt.IndexOf(DiplomaticDeclarationWritingContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(DiplomacyModeDispatchContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(DiplomaticDeclarationModeContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(CanonicalHistoryCompressionModeContractMarker, StringComparison.Ordinal) >= 0
			&& systemPrompt.IndexOf(CanonicalHistoryContractMarker, StringComparison.Ordinal) >= 0
			&& modePrompt.IndexOf(expectedMode, StringComparison.Ordinal) >= 0
			&& modePrompt.IndexOf(DiplomaticDeclarationWritingContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(DiplomacyModeDispatchContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(DiplomaticDeclarationModeContractMarker, StringComparison.Ordinal) < 0
			&& modePrompt.IndexOf(CanonicalHistoryCompressionModeContractMarker, StringComparison.Ordinal) < 0;
	}

	private bool EnsureCurrentCanonicalPromptContractBeforeSend(WorldDiplomacyJob job)
	{
		if (HasCurrentCanonicalPromptContract(job)) return true;
		if (!UsesCanonicalHistory(job)) return true;
		Log("retired stale canonical prompt contract before send job=" + (job.JobId ?? "")
			+ " kind=" + (job.Kind ?? "")
			+ " affinity=" + (job.CacheAffinityKey ?? ""));
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			if (TryRebuildPendingWorldDiplomacyJob(job)) return true;
			CommitFailedJob(job, "stale canonical prompt contract could not be rebuilt");
			return false;
		}
		_storage.DiplomacyCompressionPending = true;
		_storage.CompressionRetryAfterHour = 0;
		_storage.CompressionRetryAttempts = 0;
		RemoveJob(job.JobId);
		return false;
	}

	private void ProcessCompletedJobs()
	{
		while (_completedJobs.TryDequeue(out LlmJobResult result))
		{
			bool completesActiveRequest = string.Equals(result?.JobId, _activeJobId, StringComparison.OrdinalIgnoreCase)
				&& result.RuntimeGeneration == _activeRequestRuntimeGeneration;
			if (completesActiveRequest)
			{
				_llmRequestRunning = false;
				_activeJobId = "";
				_activeRequestRuntimeGeneration = 0L;
			}
			// A completion from a previous save/runtime may share the same persisted
			// JobId with a rebuilt request. It must not inspect, mutate or remove the
			// current runtime's job.
			if (result == null || result.RuntimeGeneration != _runtimeGeneration
				|| SaveRuntimeGuard.IsStale(result.RuntimeGeneration, "world_diplomacy_commit"))
			{
				continue;
			}
			WorldDiplomacyJob job = _storage.Jobs.FirstOrDefault(x => x != null && string.Equals(x.JobId, result.JobId, StringComparison.OrdinalIgnoreCase));
			if (job == null)
			{
				continue;
			}
			job.IsRunning = false;
			LogPromptCacheUsage(job, result);
			if (result.Success
				&& string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& HasStaleDiplomaticThreatPresentation(job))
			{
				if (!RefreshDiplomaticThreatPresentationAndPrompt(job))
				{
					CommitFailedJob(job, "completed generation used a stale diplomatic threat stage and could not be rebuilt");
				}
				else
				{
					Log("discarded completed generation from stale diplomatic threat stage and rebuilt job=" + job.JobId
						+ " author=" + job.AuthorKingdomId);
				}
				continue;
			}
			if (result.Success
				&& string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& HasStaleDiplomaticActionPresentation(job))
			{
				if (!RefreshDiplomaticActionPresentationAndPrompt(job))
				{
					CommitFailedJob(job, "completed generation used a stale diplomatic action list and could not be rebuilt");
				}
				else
				{
					Log("discarded completed generation from stale diplomatic action list and rebuilt job=" + job.JobId
						+ " author=" + job.AuthorKingdomId);
				}
				continue;
			}
			if (!result.Success)
			{
				if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
					&& result.IsOutputTruncated
					&& !string.IsNullOrWhiteSpace(result.Content))
				{
					_storage.ConsecutiveServiceFailures = 0;
					try
					{
						RejectGeneratedDraftBeforePublication(
							job,
							result.Content,
							ResolveKingdom(job.AuthorKingdomId),
							ResolveKingdom(job.TargetKingdomId),
							"output_truncated",
							null);
						RemoveJob(job.JobId);
					}
					catch (Exception ex)
					{
						CommitFailedJob(job, "truncated generated draft handling failed: " + ex.Message);
					}
					continue;
				}
				if (result.IsServiceFailure)
				{
					_storage.ConsecutiveServiceFailures++;
					if (_storage.ConsecutiveServiceFailures >= 2)
					{
						_storage.ServiceCooldownUntilHour = CurrentHour() + FailedServiceCooldownHours;
						_storage.ConsecutiveServiceFailures = 0;
					}
				}
				CommitFailedJob(job, result.Error);
				continue;
			}
			_storage.ConsecutiveServiceFailures = 0;
			try
			{
				if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
				{
					CommitGeneratedDocument(job, result.Content);
				}
				else if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
				{
					CommitAnalysis(job, result.Content);
				}
				else if (string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase))
				{
					CommitCompression(job, result.Content);
				}
				else if (string.Equals(job.Kind, "round_plan", StringComparison.OrdinalIgnoreCase))
				{
					CommitRoundPlan(job, result.Content);
				}
				else if (string.Equals(job.Kind, "round_compress", StringComparison.OrdinalIgnoreCase))
				{
					CommitRoundCompression(job, result.Content);
				}
				else
				{
					CommitFailedJob(job, "unknown job kind");
					continue;
				}
				RemoveJob(job.JobId);
			}
			catch (Exception ex)
			{
				CommitFailedJob(job, ex.Message);
			}
		}
	}

	private void CommitFailedJob(WorldDiplomacyJob job, string error)
	{
		if (job == null)
		{
			return;
		}
		Log("job failed kind=" + job.Kind + " id=" + job.JobId + " error=" + Limit(error, 600));
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId),
				IsAutonomousOpeningJob(job) ? "autonomous_generation_failed" : "generation_failed");
		}
		else if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
		{
			CommitAnalysis(job, BuildFallbackAnalysisJson(job));
			LogDiplomaticThreatFallbackAnalysisPublished(job);
		}
		else if (string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase))
		{
			_storage.DiplomacyCompressionPending = true;
			_storage.CompressionRetryAttempts = Math.Min(31, Math.Max(0, _storage.CompressionRetryAttempts) + 1);
			int retryHours = _storage.CompressionRetryAttempts >= 6
				? CompressionRetryMaximumHours
				: Math.Min(CompressionRetryMaximumHours,
					CompressionRetryInitialHours << Math.Max(0, _storage.CompressionRetryAttempts - 1));
			_storage.CompressionRetryAfterHour = CurrentHour() + retryHours;
			Log("token compression retained for retry batch=" + (job.CompressionBatchId ?? "")
				+ " attempt=" + _storage.CompressionRetryAttempts.ToString(CultureInfo.InvariantCulture)
				+ " retry_hours=" + retryHours.ToString(CultureInfo.InvariantCulture)
				+ " retry_after_hour=" + _storage.CompressionRetryAfterHour.ToString(CultureInfo.InvariantCulture));
		}
		else if (string.Equals(job.Kind, "round_plan", StringComparison.OrdinalIgnoreCase))
		{
			CommitRoundPlan(job, "{\"topic\":\"外交交涉\",\"selected_kingdom_ids\":[]}");
		}
		else if (string.Equals(job.Kind, "round_compress", StringComparison.OrdinalIgnoreCase))
		{
			CommitRoundCompression(job, BuildFallbackRoundCompressionJson(job));
		}
		RemoveJob(job.JobId);
	}

	private static bool IsAutonomousOpeningJob(WorldDiplomacyJob job)
	{
		return job != null
			&& string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)
			&& !job.IsResponse
			&& job.AllowUntargeted
			&& string.IsNullOrWhiteSpace(job.TargetKingdomId);
	}

	private void CommitGeneratedDocument(WorldDiplomacyJob job, string raw)
	{
		if (job == null) return;
		WorldDiplomacyRound jobRound = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (jobRound?.ResultSettlementPending == true)
		{
			WorldDiplomacyResultSettlementSlot currentSlot = (jobRound.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, job.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.KingdomId, job.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
			if (currentSlot == null
				|| !string.Equals(jobRound.ResultSettlementCurrentSlotId, job.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase))
			{
				Log("stale result-settlement generation discarded job=" + job.JobId
					+ " round=" + jobRound.RoundId + " author=" + (job.AuthorKingdomId ?? ""));
				jobRound.RelayWaiting = false;
				ScheduleNextResultSettlementTurn(jobRound);
				return;
			}
		}
		Kingdom author = ResolveKingdom(job.AuthorKingdomId);
		Kingdom fallbackTarget = ResolveKingdom(job.TargetKingdomId);
		if (author == null)
		{
			AbandonRejectedGeneration(job, null, fallbackTarget, "generated_party_missing");
			return;
		}
		if (!CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			Log("generated declaration discarded at commit job=" + job.JobId + " author=" + author.StringId
				+ " reason=" + authorBlockReason);
			AbandonRejectedGeneration(job, author, fallbackTarget, authorBlockReason);
			return;
		}
		PruneInvalidOffers(jobRound);
		if (!TryParseJsonObject(raw, out JObject json))
		{
			RejectGeneratedDraftBeforePublication(job, raw, author, fallbackTarget, "json_parse_failed", null);
			return;
		}
		if (TryNormalizeInlineResponseBinding(json, out string normalizedBindingKind))
		{
			Log("normalized inline response binding job=" + job.JobId + " kind=" + normalizedBindingKind);
		}
		NormalizeGeneratedDiplomaticEnvelopeShape(job, json);
		if (TryGetGeneratedIntentLegalityViolation(job, json, author, fallbackTarget, out Kingdom generatedTarget, out string legalityReason))
		{
			RejectGeneratedDraftBeforePublication(
				job,
				raw,
				author,
				generatedTarget ?? fallbackTarget,
				legalityReason,
				json);
			return;
		}
		Kingdom target = generatedTarget;
		WorldDiplomacyDocument sourceDocument = ResolveDocument(job.SourceDocumentId);
		string title = FirstNonEmpty(
			ReadString(json, "title"),
			job.IsResponse ? "外交回应" : "王国外交宣言");
		title = Limit(SanitizePublicDiplomacyText(title), 100);
		string body = NormalizeBody(SanitizePublicDiplomacyText(ReadString(json, "body", "public_document", "document")));
		if (string.IsNullOrWhiteSpace(body))
		{
			RejectGeneratedDraftBeforePublication(job, raw, author, target, "empty_public_document", json);
			return;
		}
		WorldDiplomacyDocument document = CreateDocument(
			author,
			target,
			title,
			body,
			job.IsResponse ? "ai_response" : "ai",
			isPlayerAuthored: false,
			isResponse: job.IsResponse,
			exchangeId: job.ExchangeId);
		document.RoundId = FirstNonEmpty(job.RoundId, job.ExchangeId);
		if (job.IsRelayTurn && job.CreatedDay >= 0)
		{
			document.Day = job.CreatedDay;
			document.GameDate = FormatCampaignDate(job.CreatedDay);
		}
		document.HiddenIntent = NormalizeIntent(ReadString(json, "author_intent.intent", "intent", "author_intent"));
		document.HiddenCommitment = NormalizeCommitment(ReadString(json, "author_intent.commitment", "commitment"));
		document.PeaceTerms = target == null ? null : ParseAndValidatePeaceTerms(json, author, target);
		document.SourceDocumentId = job.SourceDocumentId ?? "";
		document.RespondingToOfferDocumentId = ReadString(json, "responding_to_offer_document_id");
		document.RespondingToThreatDocumentId = ReadString(json, "responding_to_threat_document_id");
		document.SourceDocumentId = FirstNonEmpty(
			document.RespondingToOfferDocumentId,
			document.RespondingToThreatDocumentId,
			document.SourceDocumentId);
		document.PresentedThreatDocumentIds = new List<string>(job.PresentedThreatDocumentIds ?? new List<string>());
		document.PresentedThreatFollowThroughDocumentIds = new List<string>(job.PresentedThreatFollowThroughDocumentIds ?? new List<string>());
		document.IsExternalResponseOnly = job.IsExternalResponseOnly;
		document.IsReminder = job.IsReminder;
		document.IsRelayTurn = job.IsRelayTurn;
		document.ResultSettlementSlotId = job.ResultSettlementSlotId ?? "";
		document.RoundParticipation = NormalizeToken(ReadString(json, "round_participation"));
		if (document.RoundParticipation != "withdraw") document.RoundParticipation = "continue";
		document.RoundStatus = NormalizeToken(ReadString(json, "round_status"));
		if (document.RoundStatus != "resolved" && document.RoundStatus != "deadlocked") document.RoundStatus = "continue";
		document.MadeDiplomaticProgress = ReadBool(json, "made_progress");
		document.HasEmbeddedRoundPlan = IsAutonomousOpeningJob(job);
		// Kept only for old saves. New opening documents are always actionable.
		document.IsAutonomousNoActionDeclaration = false;
		if (document.HasEmbeddedRoundPlan)
		{
			// The public title is the authoritative topic. This prevents a hidden round_plan label
			// from leaking a private long-term strategy into later prompts or the player archive.
			document.PlannedRoundTopic = Limit(title, 120);
			document.PlannedKingdomIds = ReadStringList(json, "round_plan.selected_kingdom_ids")
				.Where(x => job.CandidateKingdomIds.Contains(x, StringComparer.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		document.AutomaticReplyDepth = job.IsResponse ? Math.Max(1, (sourceDocument?.AutomaticReplyDepth ?? 0) + 1) : 0;
		if (!TryApplyGeneratedSemanticEnvelope(document, json, author, target, job.AllowUntargeted,
			job.IsRelayTurn))
		{
			RejectGeneratedDraftBeforePublication(job, raw, author, target, "generated_semantic_envelope_incomplete", json);
			return;
		}
		ApplyInternationalReputationEvaluation(document, json);
		if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase)
			&& !IsTerminalNegotiationMove(document.NegotiationMove))
		{
			document.RoundParticipation = "continue";
		}
		AddDocument(document);
		WorldDiplomacyExchange exchange = ResolveExchange(job.ExchangeId);
		if (exchange != null)
		{
			if (job.IsResponse)
			{
				exchange.ResponseDocumentId = document.DocumentId;
				exchange.State = "analyzing_response";
			}
			else
			{
				exchange.SourceDocumentId = document.DocumentId;
				exchange.State = "analyzing_source";
			}
		}
		ProcessAnalyzedDocument(document, document.Intent, document.Commitment, document.RequiresResponse, document.Tone, document.Confidence);
	}

	private bool TryGetGeneratedIntentLegalityViolation(
		WorldDiplomacyJob job,
		JObject json,
		Kingdom author,
		Kingdom fallbackTarget,
		out Kingdom generatedTarget,
		out string reason)
	{
		generatedTarget = null;
		reason = "";
		if (job == null || json == null || author == null || json["actions"] is not JArray actions
			|| actions.Count < 1 || actions.Count > MaxDiplomaticActionsPerDocument)
		{
			reason = "diplomatic_actions_envelope_invalid";
			return true;
		}
		HashSet<string> targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int statementCount = 0;
		int outgoingThreatCount = 0;
		for (int index = 0; index < actions.Count; index++)
		{
			if (actions[index] is not JObject action)
			{
				reason = "diplomatic_action_entry_invalid";
				return true;
			}
			string targetId = ReadString(action, "target_kingdom_id", "target");
			if (string.IsNullOrWhiteSpace(targetId) || !targetIds.Add(targetId))
			{
				reason = "diplomatic_action_target_missing_or_duplicate";
				return true;
			}
			string intent = NormalizeIntent(ReadString(action, "intent", "author_intent.intent"));
			if (intent == "statement") statementCount++;
			if (intent == "warning" || intent == "ultimatum") outgoingThreatCount++;
			JObject single = BuildGeneratedSingleActionEnvelope(json, action);
			if (TryGetGeneratedSingleActionLegalityViolation(
				job,
				single,
				author,
				actions.Count == 1 ? fallbackTarget : null,
				out Kingdom actionTarget,
				out reason))
			{
				reason = "action[" + index.ToString(CultureInfo.InvariantCulture) + "]:" + reason;
				generatedTarget = actionTarget;
				return true;
			}
			CopyDerivedGeneratedActionEnvelope(single, action);
			if (index == 0) generatedTarget = actionTarget;
		}
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			owningRound,
			author,
			job.ResultSettlementSlotId,
			job.IsExternalResponseOnly,
			job.SourceDocumentId,
			requireAnyOpenPeaceOffer: job.IsRelayTurn);
		if (!GeneratedActionsContainRequiredPeaceOfferResponse(actions, requiredPeaceOffer))
		{
			reason = "required_peace_offer_response_missing";
			return true;
		}
		if (GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(actions))
		{
			reason = "multiple_peace_acceptances_have_cross_terms";
			return true;
		}
		if ((statementCount > 0 && actions.Count != 1) || statementCount > 1)
		{
			reason = "statement_must_be_the_only_diplomatic_action";
			return true;
		}
		if (outgoingThreatCount > 1)
		{
			reason = "multiple_outgoing_threats_not_supported";
			return true;
		}
		if (IsAutonomousOpeningJob(job))
		{
			HashSet<string> planned = new HashSet<string>(
				ReadStringList(json, "round_plan.selected_kingdom_ids"),
				StringComparer.OrdinalIgnoreCase);
			if (targetIds.Count + 1 > GetRoundParticipantLimit())
			{
				reason = "autonomous_round_plan_exceeds_participant_limit";
				return true;
			}
			if (targetIds.Any(x => !planned.Contains(x)))
			{
				reason = "autonomous_round_plan_omits_direct_target";
				return true;
			}
		}
		MirrorFirstGeneratedActionEnvelope(json, actions);
		json["addressed_kingdom_ids"] = new JArray(targetIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return false;
	}

	private bool TryGetGeneratedSingleActionLegalityViolation(
		WorldDiplomacyJob job,
		JObject json,
		Kingdom author,
		Kingdom fallbackTarget,
		out Kingdom generatedTarget,
		out string reason)
	{
		generatedTarget = null;
		reason = "";
		if (job == null || json == null || author == null)
		{
			reason = "semantic_envelope_incomplete";
			return true;
		}
		if (!(json["author_intent"] is JObject)
			|| !IsJsonStringArray(json["addressed_kingdom_ids"])
			|| !IsJsonStringArray(json["mentioned_kingdom_ids"])
			|| !(json["round_plan"] is JObject roundPlanEnvelope)
			|| !IsJsonStringArray(roundPlanEnvelope["selected_kingdom_ids"])
			|| !(json["peace_terms"] is JObject)
			|| json["primary_target_kingdom_id"] == null
			|| json["requires_response"] == null
			|| json["tone"] == null
			|| json["confidence"] == null
			|| string.IsNullOrWhiteSpace(ReadString(json, "body", "public_document", "document")))
		{
			reason = "semantic_envelope_incomplete";
			return true;
		}
		string intent = NormalizeIntent(ReadString(json, "author_intent.intent", "intent", "author_intent"));
		string commitment = DefaultCommitmentForIntent(intent);
		if (json["author_intent"] is JObject generatedIntentEnvelope)
		{
			generatedIntentEnvelope["intent"] = intent;
			generatedIntentEnvelope["commitment"] = commitment;
		}
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (!IsSupportedDiplomacyIntent(intent) || !IsSupportedCommitment(commitment))
		{
			reason = "unsupported_intent_or_commitment";
			return true;
		}
		string title = ReadString(json, "title");
		string body = ReadString(json, "body", "public_document", "document");
		string visibleText = title + "\n" + body;
		string targetId = ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
		if (!string.IsNullOrWhiteSpace(targetId))
		{
			generatedTarget = ResolveKingdom(targetId);
			if (generatedTarget == null)
			{
				reason = "target_kingdom_not_found";
				return true;
			}
		}
		else if (!job.AllowUntargeted)
		{
			generatedTarget = fallbackTarget;
		}
		if (generatedTarget == author
			|| generatedTarget?.IsEliminated == true
			|| (generatedTarget != null && !HasIndependentWorldDiplomacyAuthority(generatedTarget)))
		{
			reason = "target_kingdom_not_eligible";
			return true;
		}
		if (!job.IsRelayTurn
			&& !IsAutonomousOpeningJob(job)
			&& !string.IsNullOrWhiteSpace(job.TargetKingdomId)
			&& !string.Equals(generatedTarget?.StringId, job.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			reason = "kingdom_not_in_targeted_generation_scope";
			return true;
		}
		WorldDiplomacyDocument responseSource = ResolveDocument(job.SourceDocumentId);
		bool allowedRoundResponseNoAction = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
			&& IsNonRootAiRelayNoActionAllowed(
				owningRound,
				job.ResultSettlementSlotId,
				author,
				generatedTarget,
				job.IsRelayTurn,
				job.IsExternalResponseOnly,
				responseSource);
		if (!IsActionableDiplomacyIntent(intent) && !allowedRoundResponseNoAction)
		{
			reason = "non_actionable_diplomatic_intent";
			return true;
		}
		List<string> addressedIds = ReadStringList(json, "addressed_kingdom_ids", "addressed");
		List<string> mentionedIds = ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
		foreach (string id in addressedIds.Concat(mentionedIds))
		{
			Kingdom listed = ResolveKingdom(id);
			if (string.IsNullOrWhiteSpace(id) || listed == null || listed == author || listed.IsEliminated
				|| !HasIndependentWorldDiplomacyAuthority(listed))
			{
				reason = "referenced_kingdom_not_eligible";
				return true;
			}
		}
		if (IsAutonomousOpeningJob(job))
		{
			HashSet<string> allowed = new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			if ((generatedTarget != null && !allowed.Contains(generatedTarget.StringId))
				|| addressedIds.Any(id => !allowed.Contains(id))
				|| mentionedIds.Any(id => !allowed.Contains(id)))
			{
				reason = "kingdom_not_in_autonomous_candidate_set";
				return true;
			}
			if (!(json["round_plan"] is JObject roundPlan)
				|| !IsJsonStringArray(roundPlan["selected_kingdom_ids"])
				|| string.IsNullOrWhiteSpace(ReadString(json, "round_plan.topic")))
			{
				reason = "autonomous_round_plan_incomplete";
				return true;
			}
			List<string> plannedIds = ReadStringList(json, "round_plan.selected_kingdom_ids");
			if (plannedIds.Any(id => !allowed.Contains(id)
				|| ResolveKingdom(id) is not Kingdom planned
				|| planned.IsEliminated
				|| !HasIndependentWorldDiplomacyAuthority(planned)))
			{
				reason = "autonomous_round_plan_has_invalid_participant";
				return true;
			}
			HashSet<string> plannedSet = new HashSet<string>(plannedIds, StringComparer.OrdinalIgnoreCase);
			List<string> directIds = addressedIds
				.Concat(generatedTarget == null ? Enumerable.Empty<string>() : new[] { generatedTarget.StringId })
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			int participantLimit = GetRoundParticipantLimit();
			if (plannedSet.Count + 1 > participantLimit || directIds.Count + 1 > participantLimit)
			{
				reason = "autonomous_round_plan_exceeds_participant_limit";
				return true;
			}
			if (directIds.Any(id => !plannedSet.Contains(id)))
			{
				reason = "autonomous_round_plan_omits_direct_target";
				return true;
			}
		}
		bool resultSettlementRelay = job.IsRelayTurn && owningRound?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId);
		HashSet<string> presentedSettlementTargets = resultSettlementRelay
			? new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
			: null;
		bool generatedTargetOutsideScope = generatedTarget != null
			&& !(resultSettlementRelay
				? presentedSettlementTargets.Contains(generatedTarget.StringId)
					&& CanUseResultSettlementTarget(owningRound, author, generatedTarget)
				: RoundRouteContainsKingdom(owningRound, generatedTarget.StringId));
		string generatedTargetScopeId = generatedTarget?.StringId ?? "";
		bool addressedOutsideScope = addressedIds.Any(id => resultSettlementRelay
			? !presentedSettlementTargets.Contains(id)
				|| (!string.IsNullOrWhiteSpace(generatedTargetScopeId)
					&& !string.Equals(id, generatedTargetScopeId, StringComparison.OrdinalIgnoreCase)
					&& !RoundRouteContainsKingdom(owningRound, id))
			: !RoundRouteContainsKingdom(owningRound, id));
		if (job.IsRelayTurn && (generatedTargetOutsideScope || addressedOutsideScope))
		{
			reason = resultSettlementRelay ? "kingdom_not_in_result_settlement_scope" : "kingdom_not_in_relay_route";
			return true;
		}
		bool targetRequired = IsActionableDiplomacyIntent(intent) || allowedRoundResponseNoAction;
		if (targetRequired && (generatedTarget == null || string.IsNullOrWhiteSpace(targetId)))
		{
			reason = "diplomatic_action_has_no_target";
			return true;
		}
		if (generatedTarget != null
			&& !BuildLegalDiplomaticDeclarationIntents(
				owningRound,
				author,
				generatedTarget,
				job.IsRelayTurn,
				job.ResultSettlementSlotId,
				job.IsExternalResponseOnly,
				responseSource)
				.Contains(intent, StringComparer.OrdinalIgnoreCase))
		{
			reason = "intent_not_in_current_legal_action_list";
			return true;
		}
		if (!TryDeriveGeneratedDiplomaticStructure(job, owningRound, json, author, generatedTarget, intent, out reason))
		{
			return true;
		}
		if (!CommitmentMatchesIntent(intent, commitment))
		{
			reason = "intent_commitment_mismatch";
			return true;
		}
		string claimedThreatDocumentId = ReadString(json, "responding_to_threat_document_id");
		if (intent == "comply_ultimatum")
		{
			if (string.IsNullOrWhiteSpace(claimedThreatDocumentId))
			{
				reason = "comply_ultimatum_missing_source_document";
				return true;
			}
			if (!(job.PresentedThreatDocumentIds ?? new List<string>()).Contains(claimedThreatDocumentId, StringComparer.OrdinalIgnoreCase))
			{
				reason = "comply_ultimatum_source_not_presented";
				return true;
			}
		}
		else if (!string.IsNullOrWhiteSpace(claimedThreatDocumentId))
		{
			reason = "non_compliance_claims_threat_source";
			return true;
		}
		if (TryGetDiplomaticStateViolation(intent, author, generatedTarget, out reason)) return true;
		if (TryGetDiplomaticThreatIntentViolation(intent, author, generatedTarget,
			claimedThreatDocumentId, out reason)) return true;

		string proposalIntent = ResponseIntentToProposalIntent(intent);
		if (!string.IsNullOrWhiteSpace(proposalIntent))
		{
			if (generatedTarget == null || generatedTarget == author)
			{
				reason = "offer_response_has_no_valid_proposer";
				return true;
			}
			if (!TryResolveOpenProposalFor(job, author, generatedTarget, proposalIntent, out string openOfferDocumentId))
			{
				reason = "offer_response_without_matching_open_offer";
				return true;
			}
			string claimedOfferDocumentId = ReadString(json, "responding_to_offer_document_id");
			if (string.IsNullOrWhiteSpace(claimedOfferDocumentId))
			{
				reason = "offer_response_missing_source_document";
				return true;
			}
			if (!string.Equals(claimedOfferDocumentId, openOfferDocumentId, StringComparison.OrdinalIgnoreCase))
			{
				reason = "offer_response_source_mismatch";
				return true;
			}
			if (intent == "accept_peace")
			{
				WorldDiplomacyPeaceTerms responseTerms = ParseAndValidatePeaceTerms(json, author, generatedTarget);
				WorldDiplomacyDocument source = ResolveDocument(openOfferDocumentId);
				WorldDiplomacyPeaceTerms offeredTerms = ResolveOfferedPeaceTerms(
					source,
					ReadString(json, "responding_to_offer_action_id"));
				if (responseTerms != null && !ArePeaceTermsEquivalent(responseTerms, offeredTerms))
				{
					reason = "accept_peace_changes_offer_terms";
					return true;
				}
			}
		}
		else if (!string.IsNullOrWhiteSpace(ReadString(json, "responding_to_offer_document_id")))
		{
			string claimedOfferDocumentId = ReadString(json, "responding_to_offer_document_id");
			if (IsProposalIntent(intent)
				&& generatedTarget != null
				&& generatedTarget != author
				&& TryResolveOpenProposalFor(job, author, generatedTarget, intent, out string openOfferDocumentId)
				&& string.Equals(claimedOfferDocumentId, openOfferDocumentId, StringComparison.OrdinalIgnoreCase))
			{
				// A counter-proposal is a new offer, not an acceptance/rejection. DeepSeek often keeps the
				// incoming offer id to express continuity; ownership is already proven above, so normalize
				// the bookkeeping field instead of discarding an otherwise legal public document.
				json["responding_to_offer_document_id"] = "";
				Log("counter-proposal source normalized job=" + job.JobId
					+ " author=" + author.StringId + " target=" + generatedTarget.StringId
					+ " intent=" + intent + " source=" + openOfferDocumentId);
			}
			else
			{
				reason = "non_response_claims_offer_source";
				return true;
			}
		}
		// The LLM's structured author_intent is authoritative for generated declarations.
		// Do not re-infer an action from literary wording: the structured intent is always
		// exposed to players through DocumentTypeLabel, while C# still owns legality and execution.
		if (TryGetPublicPeaceTermsDisclosureViolation(intent, visibleText, json, author, generatedTarget, out reason)) return true;
		if (TryGetImmersionViolation(visibleText, out reason))
		{
			return true;
		}
		if (TryGetRealmIdentityViolation(author, visibleText, out reason))
		{
			return true;
		}
		if (!IsPeaceIntent(intent)) return false;
		if (generatedTarget == null || generatedTarget == author)
		{
			reason = "peace_intent_has_no_valid_target";
			return true;
		}
		if (!FactionManager.IsAtWarAgainstFaction(author, generatedTarget))
		{
			reason = "peace_intent_between_kingdoms_not_at_war";
			return true;
		}
		return false;
	}

	private static bool ArePeaceTermsEquivalent(WorldDiplomacyPeaceTerms first, WorldDiplomacyPeaceTerms second)
	{
		if (first == null || second == null) return first == null && second == null;
		return string.Equals(first.TributePayerKingdomId ?? "", second.TributePayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.TributeReceiverKingdomId ?? "", second.TributeReceiverKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& first.DailyTribute == second.DailyTribute
			&& first.DurationDays == second.DurationDays
			&& string.Equals(first.CessionFromKingdomId ?? "", second.CessionFromKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionToKingdomId ?? "", second.CessionToKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(first.CessionSettlementId ?? "", second.CessionSettlementId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static WorldDiplomacyPeaceTerms ClonePeaceTerms(WorldDiplomacyPeaceTerms source)
	{
		if (source == null) return null;
		return new WorldDiplomacyPeaceTerms
		{
			TributePayerKingdomId = source.TributePayerKingdomId ?? "",
			TributeReceiverKingdomId = source.TributeReceiverKingdomId ?? "",
			DailyTribute = source.DailyTribute,
			DurationDays = source.DurationDays,
			CessionFromKingdomId = source.CessionFromKingdomId ?? "",
			CessionToKingdomId = source.CessionToKingdomId ?? "",
			CessionSettlementId = source.CessionSettlementId ?? ""
		};
	}

	private bool TryGetPublicPeaceTermsDisclosureViolation(string intent, string visibleText, JObject json, Kingdom author, Kingdom target, out string reason)
	{
		reason = "";
		string normalized = NormalizeIntent(intent);
		string text = visibleText ?? "";
		if (normalized != "propose_peace" || json?.SelectToken("peace_terms") is not JObject terms) return false;
		int.TryParse(terms["daily_tribute"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tribute);
		int.TryParse(terms["duration_days"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration);
		if (tribute > 0 && !ContainsWholeNumber(text, tribute))
		{
			reason = "peace_terms_not_visible:tribute";
			return true;
		}
		if (duration > 0 && !ContainsWholeNumber(text, duration))
		{
			reason = "peace_terms_not_visible:duration";
			return true;
		}
		Kingdom payer = ResolveKingdom((terms["tribute_payer_kingdom_id"]?.ToString() ?? "").Trim());
		Kingdom receiver = ResolveKingdom((terms["tribute_receiver_kingdom_id"]?.ToString() ?? "").Trim());
		if (tribute > 0 && payer != null && receiver != null
			&& !ContainsDirectedPeaceTerm(text, payer, receiver, author, target, "支付|缴纳|交付|给付"))
		{
			reason = "peace_terms_not_visible:tribute_direction";
			return true;
		}
		string settlementId = (terms["cession_settlement_id"]?.ToString() ?? "").Trim();
		Settlement settlement = ResolveSettlementById(settlementId);
		if (!string.IsNullOrWhiteSpace(settlementId)
			&& text.IndexOf(settlementId, StringComparison.OrdinalIgnoreCase) < 0
			&& (settlement == null || text.IndexOf(settlement.Name?.ToString() ?? "", StringComparison.OrdinalIgnoreCase) < 0))
		{
			reason = "peace_terms_not_visible:cession";
			return true;
		}
		Kingdom cessionFrom = ResolveKingdom((terms["cession_from_kingdom_id"]?.ToString() ?? "").Trim());
		Kingdom cessionTo = ResolveKingdom((terms["cession_to_kingdom_id"]?.ToString() ?? "").Trim());
		if (settlement != null && cessionFrom != null && cessionTo != null
			&& !ContainsDirectedPeaceTerm(text, cessionFrom, cessionTo, author, target, "割让|移交|交还|归还"))
		{
			reason = "peace_terms_not_visible:cession_direction";
			return true;
		}
		return false;
	}

	private static bool ContainsWholeNumber(string text, int value)
	{
		return Regex.IsMatch(text ?? "", @"(?<!\d)" + Regex.Escape(value.ToString(CultureInfo.InvariantCulture)) + @"(?!\d)", RegexOptions.CultureInvariant);
	}

	private static bool ContainsDirectedPeaceTerm(string text, Kingdom from, Kingdom to, Kingdom author, Kingdom target, string actionPattern)
	{
		if (from == null || to == null) return false;
		string fromPattern = BuildPeaceKingdomReferencePattern(from, author, target);
		string toPattern = BuildPeaceKingdomReferencePattern(to, author, target);
		string body = text ?? "";
		return Regex.IsMatch(body, fromPattern + @"[^。；\n]{0,40}(?:" + actionPattern + @")[^。；\n]{0,40}" + toPattern, RegexOptions.CultureInvariant)
			|| Regex.IsMatch(body, fromPattern + @"[^。；\n]{0,24}(?:向|给|予)" + toPattern + @"[^。；\n]{0,24}(?:" + actionPattern + @")", RegexOptions.CultureInvariant);
	}

	private static string BuildPeaceKingdomReferencePattern(Kingdom kingdom, Kingdom author, Kingdom target)
	{
		string name = Regex.Escape(KingdomName(kingdom));
		if (kingdom == author) return "(?:我国|本国|本王国|" + name + ")";
		if (kingdom == target) return "(?:贵国|" + name + ")";
		return "(?:" + name + ")";
	}

	private static bool CommitmentMatchesIntent(string intent, string commitment)
	{
		string normalizedIntent = NormalizeIntent(intent);
		string normalizedCommitment = NormalizeCommitment(commitment);
		if (IsImmediateIntent(normalizedIntent)) return normalizedCommitment == "binding";
		if (IsProposalIntent(normalizedIntent)) return normalizedCommitment == "proposal";
		if (normalizedIntent.StartsWith("accept_", StringComparison.Ordinal)) return normalizedCommitment == "acceptance";
		if (normalizedIntent.StartsWith("reject_", StringComparison.Ordinal)) return normalizedCommitment == "rejection";
		if (normalizedIntent is "ultimatum" or "comply_ultimatum" or "apology" or "concession") return normalizedCommitment == "binding";
		if (normalizedIntent is "statement" or "condemn" or "warning") return normalizedCommitment == "non_binding";
		return false;
	}

	private bool TryGetDiplomaticStateViolation(string intent, Kingdom author, Kingdom target, out string reason)
	{
		reason = "";
		string normalized = NormalizeIntent(intent);
		if (author == null) return false;
		if (target == null) return false;
		bool atWar = FactionManager.IsAtWarAgainstFaction(author, target);
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		bool allied = alliance != null && alliance.IsAllyWithKingdom(author, target);
		bool trading = trade != null && BannerlordApiCompat.HasTradeAgreement(trade, author, target);
		switch (normalized)
		{
		case "declare_war":
				bool enforcingRejectedUltimatum = IsEnforcingRejectedUltimatum(author, target);
				if (!CanDeclareWar(author, target, out string blockReason, enforcingRejectedUltimatum))
				{
					reason = "declare_war_not_legal:" + blockReason;
					return true;
				}
				break;
			case "break_alliance":
				if (alliance == null) { reason = "alliance_system_unavailable"; return true; }
				if (!allied) { reason = "break_alliance_without_alliance"; return true; }
				break;
			case "cancel_trade":
				if (trade == null) { reason = "trade_system_unavailable"; return true; }
				if (!trading) { reason = "cancel_trade_without_trade_agreement"; return true; }
				break;
			case "propose_peace":
			case "accept_peace":
			case "reject_peace":
				if (!atWar) { reason = "peace_intent_between_kingdoms_not_at_war"; return true; }
				break;
			case "propose_alliance":
				if (alliance == null) { reason = "alliance_system_unavailable"; return true; }
				if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
				if (IsTradeAllianceProposalCoolingDown(author, target, normalized)) { reason = "intent_not_in_current_legal_action_list"; return true; }
				break;
			case "accept_alliance":
				if (alliance == null) { reason = "alliance_system_unavailable"; return true; }
				if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
				break;
			case "propose_trade":
				if (trade == null) { reason = "trade_system_unavailable"; return true; }
				if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
				if (IsTradeAllianceProposalCoolingDown(author, target, normalized)) { reason = "intent_not_in_current_legal_action_list"; return true; }
				break;
			case "accept_trade":
				if (trade == null) { reason = "trade_system_unavailable"; return true; }
				if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
				break;
		}
		return false;
	}

	private bool IsEnforcingRejectedUltimatum(Kingdom author, Kingdom target)
	{
		if (author == null || target == null) return false;
		WorldDiplomacyThreat threat = FindOpenDiplomaticThreat(author.StringId, target.StringId);
		return threat != null
			&& string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase);
	}

	private bool TryGetDiplomaticThreatIntentViolation(
		string intent,
		Kingdom author,
		Kingdom target,
		string claimedThreatDocumentId,
		out string reason)
	{
		reason = "";
		string normalized = NormalizeIntent(intent);
		if (normalized != "warning" && normalized != "ultimatum" && normalized != "comply_ultimatum") return false;
		if (author == null || target == null || author == target)
		{
			reason = "threat_action_has_no_eligible_parties";
			return true;
		}
		if (normalized == "comply_ultimatum")
		{
			if (FactionManager.IsAtWarAgainstFaction(author, target))
			{
				reason = "comply_ultimatum_after_war_started";
				return true;
			}
			WorldDiplomacyThreat incoming = FindOpenDiplomaticThreat(target.StringId, author.StringId);
			if (incoming == null)
			{
				reason = "comply_ultimatum_without_open_threat";
				return true;
			}
			if (!string.Equals(incoming.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
			{
				reason = "comply_ultimatum_after_target_decision";
				return true;
			}
			if (string.IsNullOrWhiteSpace(claimedThreatDocumentId)
				|| !string.Equals(incoming.StageDocumentId, claimedThreatDocumentId, StringComparison.OrdinalIgnoreCase))
			{
				reason = "comply_ultimatum_source_mismatch";
				return true;
			}
			return false;
		}

		if (FactionManager.IsAtWarAgainstFaction(author, target))
		{
			reason = "threat_intent_between_kingdoms_already_at_war";
			return true;
		}
		if (!CanIssueWarThreat(author, target, out string enforcementBlockReason))
		{
			reason = "threat_cannot_be_enforced:" + enforcementBlockReason;
			return true;
		}
		WorldDiplomacyThreat outbound = FindOpenDiplomaticThreatIssuedBy(author.StringId);
		if (normalized == "warning")
		{
			if (outbound != null)
			{
				reason = "issuer_already_has_open_threat";
				return true;
			}
			return false;
		}
		if (outbound == null) return false;
		if (!string.Equals(outbound.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
		{
			reason = "issuer_open_threat_targets_another_kingdom";
			return true;
		}
		if (!string.Equals(outbound.Stage, "warning", StringComparison.OrdinalIgnoreCase))
		{
			reason = "duplicate_open_ultimatum";
			return true;
		}
		if (!string.Equals(outbound.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
		{
			reason = "warning_escalation_requires_target_noncompliance";
			return true;
		}
		return false;
	}

	private bool TryResolveOpenProposalFor(WorldDiplomacyJob job, Kingdom responder, Kingdom proposer, string proposalIntent, out string sourceDocumentId)
	{
		sourceDocumentId = "";
		if (job == null || responder == null || proposer == null || !IsProposalIntent(proposalIntent)) return false;
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		return TryResolveUniqueOpenProposalForRound(round, responder, proposer, proposalIntent, out sourceDocumentId);
	}

	private static bool TryResolveUniqueOpenProposalForRound(
		WorldDiplomacyRound round,
		Kingdom responder,
		Kingdom proposer,
		string proposalIntent,
		out string sourceDocumentId)
	{
		return TryResolveUniqueOpenProposalForRound(
			round,
			responder,
			proposer,
			proposalIntent,
			out sourceDocumentId,
			out _);
	}

	private static bool TryResolveUniqueOpenProposalForRound(
		WorldDiplomacyRound round,
		Kingdom responder,
		Kingdom proposer,
		string proposalIntent,
		out string sourceDocumentId,
		out string sourceActionId)
	{
		sourceDocumentId = "";
		sourceActionId = "";
		if (round == null || responder == null || proposer == null || !IsProposalIntent(proposalIntent)) return false;
		List<WorldDiplomacyRoundOffer> matches = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(x.SourceDocumentId))
			.GroupBy(x => (x.SourceDocumentId ?? "") + "\n" + (x.SourceActionId ?? ""), StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.Take(2)
			.ToList();
		if (matches.Count != 1) return false;
		sourceDocumentId = matches[0].SourceDocumentId ?? "";
		sourceActionId = matches[0].SourceActionId ?? "";
		return true;
	}

	private bool TryDeriveGeneratedDiplomaticStructure(
		WorldDiplomacyJob job,
		WorldDiplomacyRound round,
		JObject json,
		Kingdom author,
		Kingdom target,
		string intent,
		out string reason)
	{
		reason = "";
		if (json == null || author == null)
		{
			reason = "semantic_envelope_incomplete";
			return false;
		}
		json["responding_to_offer_document_id"] = "";
		json["responding_to_offer_action_id"] = "";
		json["responding_to_threat_document_id"] = "";
		json["responding_to_threat_action_id"] = "";
		string proposalIntent = ResponseIntentToProposalIntent(intent);
		if (!string.IsNullOrWhiteSpace(proposalIntent))
		{
			if (target == null || !TryResolveUniqueOpenProposalForRound(
				round, author, target, proposalIntent, out string offerId, out string offerActionId))
			{
				reason = "offer_response_without_unique_open_offer";
				return false;
			}
			json["responding_to_offer_document_id"] = offerId;
			json["responding_to_offer_action_id"] = offerActionId;
			if (string.Equals(intent, "accept_peace", StringComparison.OrdinalIgnoreCase))
			{
				WorldDiplomacyDocument source = ResolveDocument(offerId);
				json["peace_terms"] = BuildPeaceTermsJson(ResolveOfferedPeaceTerms(source, offerActionId));
			}
			return true;
		}
		if (!string.Equals(intent, "comply_ultimatum", StringComparison.OrdinalIgnoreCase)) return true;
		if (target == null)
		{
			reason = "comply_ultimatum_without_open_threat";
			return false;
		}
		HashSet<string> presented = new HashSet<string>(job?.PresentedThreatDocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyThreat> threats = (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& string.Equals(x.IssuerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(x.StageDocumentId)
				&& presented.Contains(x.StageDocumentId))
			.GroupBy(x => (x.StageDocumentId ?? "") + "\n" + (x.StageActionId ?? ""), StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.Take(2)
			.ToList();
		if (threats.Count != 1)
		{
			reason = "comply_ultimatum_without_unique_presented_threat";
			return false;
		}
		json["responding_to_threat_document_id"] = threats[0].StageDocumentId ?? "";
		json["responding_to_threat_action_id"] = threats[0].StageActionId ?? "";
		return true;
	}

	private bool HasOpenProposalForDocument(WorldDiplomacyDocument response, Kingdom responder, Kingdom proposer, string proposalIntent)
	{
		if (response == null || responder == null || proposer == null || !IsProposalIntent(proposalIntent)) return false;
		if (!response.IsPlayerAuthored && string.IsNullOrWhiteSpace(response.RespondingToOfferDocumentId)) return false;
		WorldDiplomacyRound round = ResolveRound(response.RoundId);
		return round?.PendingOffers?.Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase)
			&& (response.IsPlayerAuthored || (string.Equals(response.RespondingToOfferDocumentId, x.SourceDocumentId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(response.RespondingToOfferActionId)
					|| string.Equals(response.RespondingToOfferActionId, x.SourceActionId, StringComparison.OrdinalIgnoreCase))))) == true;
	}

	private static bool TryGetImmersionViolation(string visibleText, out string reason)
	{
		reason = "";
		if (string.IsNullOrWhiteSpace(visibleText)) return false;
		if (InternalMetricTermRegex.IsMatch(visibleText) || InternalMetricWithNumberRegex.IsMatch(visibleText))
		{
			reason = "internal_metric_exposed_in_public_declaration";
			return true;
		}
		if (ContainsAny(visibleText,
			"本回合", "该回合", "此回合", "外交回合", "接力顺序", "接力轮次", "最后行动机会", "程序核验",
			"预先核验", "预核验", "结果路线", "候选路线", "既定外交动作", "程序执行", "游戏外交", "世界状态", "硬目标",
			"提示词", "缓存命中", "JSON字段", "系统字段", "程序字段", "AI模型", "游戏机制"))
		{
			reason = "internal_round_term_exposed_in_public_declaration";
			return true;
		}
		int privateFirstPersonCount = PrivateFirstPersonRegex.Matches(visibleText).Count;
		int directSecondPersonCount = DirectSecondPersonRegex.Matches(visibleText).Count;
		bool hasConversationalPhrase = ConversationalDiplomacyPhraseRegex.IsMatch(visibleText);
		if ((hasConversationalPhrase && privateFirstPersonCount >= 2 && directSecondPersonCount >= 2)
			|| (privateFirstPersonCount >= 4 && directSecondPersonCount >= 4))
		{
			reason = "private_chat_style_in_public_declaration";
			return true;
		}
		return false;
	}

	private static bool TryGetRealmIdentityViolation(Kingdom author, string visibleText, out string reason)
	{
		reason = "";
		if (author == null || string.IsNullOrWhiteSpace(visibleText)) return false;
		string kingdomId = (author.StringId ?? "").Trim().ToLowerInvariant();
		if (!string.Equals(kingdomId, "empire_n", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase)) return false;

		Hero ruler = author.Leader ?? author.RulingClan?.Leader;
		string rulerName = (ruler?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(rulerName))
		{
			string escapedName = Regex.Escape(rulerName);
			string invalidPersonalTitlePattern = "(?:" + escapedName + "(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹)|(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹)(?:阁下|大人)?" + escapedName
				+ "|" + escapedName + "(?:身为|作为|乃是|是)(?:一名|帝国的?)?(?:元老|议员|执政官|国王|女王|大公|可汗|苏丹))";
			if (Regex.IsMatch(visibleText, invalidPersonalTitlePattern, RegexOptions.CultureInvariant))
			{
				reason = "realm_ruler_title_conflicts_with_hard_fact";
				return true;
			}
		}

		if (string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase)
			&& Regex.IsMatch(visibleText, @"(?:南帝国|我国|我朝|本国|本朝)(?:的|之)?(?:元老院|元老议会|元老们)", RegexOptions.CultureInvariant))
		{
			reason = "southern_empire_government_conflicts_with_hard_fact";
			return true;
		}
		if (string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
			&& Regex.IsMatch(visibleText, @"(?:西帝国|我国|我朝|本国|本朝)(?:的|之)?(?:元老院|元老议会|元老们)", RegexOptions.CultureInvariant))
		{
			reason = "western_empire_government_conflicts_with_hard_fact";
			return true;
		}
		return false;
	}

	private void PruneInvalidOffers(WorldDiplomacyRound round)
	{
		if (round?.PendingOffers == null || round.PendingOffers.Count == 0) return;
		// SyncData can run before the Campaign behavior graph and Kingdom objects are ready.
		// Defer all stateful offer validation instead of permanently invalidating valid saved offers.
		if (Campaign.Current == null || !Kingdom.All.Any()) return;
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		Dictionary<string, WorldDiplomacyDocument> documentsById = null;
		int invalidated = 0;
		foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			Kingdom proposer = ResolveKingdom(offer.ProposerKingdomId);
			Kingdom target = ResolveKingdom(offer.TargetKingdomId);
			bool invalid = proposer == null || target == null || proposer == target
				|| proposer.IsEliminated || target.IsEliminated
				|| !HasIndependentWorldDiplomacyAuthority(proposer) || !HasIndependentWorldDiplomacyAuthority(target);
			if (!invalid)
			{
				string intent = NormalizeIntent(offer.Intent);
				bool atWar = FactionManager.IsAtWarAgainstFaction(proposer, target);
				if (intent == "propose_peace")
				{
					documentsById ??= BuildDocumentIndex(_storage.Documents);
					documentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source);
					invalid = !atWar || !AreOfferedPeaceTermsCurrentlyExecutable(offer, source, proposer, target);
				}
				else invalid = intent switch
				{
					"propose_alliance" => alliance == null || atWar || alliance.IsAllyWithKingdom(proposer, target),
					"propose_trade" => trade == null || atWar || BannerlordApiCompat.HasTradeAgreement(trade, proposer, target),
					_ => true
				};
			}
			if (!invalid) continue;
			offer.Status = "invalidated";
			invalidated++;
		}
		foreach (IGrouping<string, WorldDiplomacyRoundOffer> group in round.PendingOffers
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase))
			.GroupBy(x => NormalizeIntent(x.Intent) + "|" + x.ProposerKingdomId + "|" + x.TargetKingdomId, StringComparer.OrdinalIgnoreCase))
		{
			foreach (WorldDiplomacyRoundOffer superseded in group.OrderByDescending(x => x.CreatedDay).ThenByDescending(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase).Skip(1))
			{
				superseded.Status = "superseded";
				invalidated++;
			}
		}
		if (invalidated > 0)
		{
			Log("stale diplomacy offers invalidated round=" + round.RoundId + " count=" + invalidated.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void RejectGeneratedDraftBeforePublication(
		WorldDiplomacyJob job,
		string rejectedRaw,
		Kingdom author,
		Kingdom target,
		string reason,
		JObject parsedJson)
	{
		if (job == null) return;
		if (author == null)
		{
			AbandonRejectedGeneration(job, null, target, string.IsNullOrWhiteSpace(reason) ? "generated_party_missing" : reason);
			return;
		}
		string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "generated_draft_invalid" : reason.Trim();
		string logReason = StripGeneratedActionReasonPrefix(normalizedReason, out int rejectedActionIndex);
		JObject rejectedAction = rejectedActionIndex >= 0
			&& parsedJson?["actions"] is JArray rejectedActions
			&& rejectedActionIndex < rejectedActions.Count
			? rejectedActions[rejectedActionIndex] as JObject
			: null;
		Log("generated declaration rejected before publication job=" + job.JobId
			+ " scope=draft_validation"
			+ " action_index=" + rejectedActionIndex.ToString(CultureInfo.InvariantCulture)
			+ " intent=" + NormalizeIntent(ReadString(rejectedAction ?? parsedJson, "intent", "author_intent.intent"))
			+ " author=" + author.StringId
			+ " target=" + (target?.StringId ?? "")
			+ " reason=" + logReason
			+ " repair_attempt=" + Math.Max(0, job.SemanticRepairAttempts).ToString(CultureInfo.InvariantCulture));
		if (job.SemanticRepairAttempts < MaxGeneratedDraftRepairAttempts
			&& EnqueueGeneratedDeclarationRepair(job, rejectedRaw, author, target, normalizedReason, parsedJson))
		{
			return;
		}
		AbandonRejectedGeneration(job, author, target, normalizedReason);
	}

	private static string StripGeneratedActionReasonPrefix(string reason, out int actionIndex)
	{
		actionIndex = -1;
		string normalized = (reason ?? "").Trim();
		if (!normalized.StartsWith("action[", StringComparison.OrdinalIgnoreCase)) return normalized;
		int close = normalized.IndexOf("]:", StringComparison.Ordinal);
		if (close <= 7
			|| !int.TryParse(normalized.Substring(7, close - 7), NumberStyles.Integer,
				CultureInfo.InvariantCulture, out int parsedIndex)
			|| parsedIndex < 0) return normalized;
		actionIndex = parsedIndex;
		return normalized.Substring(close + 2);
	}

	private bool EnqueueGeneratedDeclarationRepair(
		WorldDiplomacyJob source,
		string rejectedRaw,
		Kingdom author,
		Kingdom target,
		string reason,
		JObject rejectedJson)
	{
		if (source == null || author == null) return false;
		reason = StripGeneratedActionReasonPrefix(reason, out int rejectedActionIndex);
		WorldDiplomacyRound repairRound = ResolveRound(FirstNonEmpty(source.RoundId, source.ExchangeId));
		List<string> authorizedTargetIds = GetAuthorizedGenerationTargetIds(source, repairRound, author);
		if (authorizedTargetIds.Count == 0)
		{
			Log("generated declaration repair skipped because no legal action remains sourceJob=" + source.JobId
				+ " author=" + author.StringId + " reason=" + (reason ?? ""));
			return false;
		}
		Kingdom repairTarget = target != null && authorizedTargetIds.Contains(target.StringId, StringComparer.OrdinalIgnoreCase)
			? target
			: authorizedTargetIds.Count == 1 ? ResolveKingdom(authorizedTargetIds[0]) : null;
		JObject rejectedAction = rejectedActionIndex >= 0
			&& rejectedJson?["actions"] is JArray rejectedActions
			&& rejectedActionIndex < rejectedActions.Count
			? rejectedActions[rejectedActionIndex] as JObject
			: null;
		string rejectedIntent = NormalizeIntent(ReadString(rejectedAction ?? rejectedJson, "intent", "author_intent.intent"));
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			repairRound,
			author,
			source.ResultSettlementSlotId,
			source.IsExternalResponseOnly,
			source.SourceDocumentId,
			requireAnyOpenPeaceOffer: source.IsRelayTurn);
		StringBuilder correctionBuilder = new StringBuilder();
		correctionBuilder.AppendLine("【未发布草稿的硬事实纠正】");
		correctionBuilder.AppendLine("上一份assistant内容只是未发布草稿，不属于外交历史，不得引用、延续或假定其中事件已经发生。");
		correctionBuilder.AppendLine("草稿未通过JSON、字段或事实校验，请按下列说明重新起草。");
		correctionBuilder.AppendLine("当前发文国=" + author.StringId + "=" + KingdomName(author) + "。"
			+ (repairTarget == null ? "本次可从原任务授权范围选择1至4个合法对象，每个对象一项实际外交动作。"
				: "出错项对象国=" + repairTarget.StringId + "=" + KingdomName(repairTarget) + "；实时关系=" + BuildBilateralState(author, repairTarget) + "；其他合法对象可保留。"));
		if (string.Equals(reason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("双方当前没有战争，因此不得提出、接受或拒绝和平，不得写停战、议和、退出战争、归还战争失地或战争补偿。请改选当前可选动作。");
		}
		else if (string.Equals(reason, "non_response_claims_offer_source", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("只有accept_*或reject_*可以绑定提议来源；其他动作不得填写来源，且必须从当前可选动作中选择。");
		}
		else if (string.Equals(reason, "comply_ultimatum_missing_source_document", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "non_compliance_claims_threat_source", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("只有明确且无条件退让时才选择comply_ultimatum；含糊回应、附带条件或反条件都不是退让。");
		}
		else if (string.Equals(reason, "required_peace_offer_response_missing", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine(requiredPeaceOffer == null
				? "原和平提议状态已经变化；只从当前可选动作中重新选择。"
				: "actions必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
					+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
					+ "|提出国=" + requiredPeaceOffer.ProposerKingdomId
					+ "；只能原样接受或明确拒绝，其他合法对象动作可保留。");
		}
		else if (string.Equals(reason, "multiple_peace_acceptances_have_cross_terms", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("同一篇接受多份和平原案时不得包含割地；本篇只保留一份含割地的接受，其他原案改为明确拒绝或留待下一篇处理。");
		}
		else if (reason.StartsWith("offer_response_", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "new_proposal_claims_third_party_offer", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("接受或拒绝只能由原提议对象国对原提出国作出；否则改选当前可选动作。");
		}
		else if (string.Equals(reason, "internal_metric_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("正文泄露了后台态势指标。统治者不知道战争进展分、议和开放度、劣势评分、关系点、压力阈值或总战力数值。保留原本合法的外交行动与精确条款，但把后台指标改写成由战报、军情、领地得失和王庭账簿支撑的自然判断；贡金金额、条约期限和真实事件数量可以保留。");
		}
		else if (string.Equals(reason, "internal_round_term_exposed_in_public_declaration", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("草稿泄露了系统内部的外交调度用语。公开标题和正文不得出现‘回合’、‘接力’、‘最后行动机会’、‘程序核验’等说法；应按语境改写为本次交涉、公文往来、最后立场、正式决定或外交结果。round_*字段仍按JSON契约填写，但绝不能出现在title和body中。");
		}
		else if (string.Equals(reason, "private_chat_style_in_public_declaration", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("草稿把外交公文写成了两位君主的私人对话。保留已有事实、条件和外交意图，但改由发文国、王庭或档案明确给出的制度作为叙述主体。把‘你’改为对方国名或‘贵国’，删除‘让我说说’‘你应该谢我’‘你自己选’等互相回嘴的口语。统治者的个性只体现在国家判断、条件和威慑的分寸中。");
		}
		else if (string.Equals(reason, "realm_ruler_title_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "southern_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "western_empire_government_conflicts_with_hard_fact", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("草稿混淆了发文国的政体与统治者个人头衔。以下是必须逐字服从的王国身份硬事实：" + BuildCanonicalRealmGovernmentHardFact(author, ResolveRealmRulerTitle(author, author.Leader ?? author.RulingClan?.Leader)));
			correctionBuilder.AppendLine("机构名称只能表示国家制度或权力来源，不能替代统治者个人头衔。三大帝国的最高统治者均使用皇帝或女皇称号；不得把任何一位帝国统治者称为元老、议员、执政官、国王、大公或可汗。保留合法外交内容，重新起草整份公文。");
		}
		else if (string.Equals(reason, "json_parse_failed", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "output_truncated", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "generated_semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "empty_public_document", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("只输出一个完整、可解析的JSON对象，不加代码围栏或解释。契约字段必须齐全，字符串正确转义，公开正文不能为空。");
		}
		else if (string.Equals(reason, "unsupported_intent_or_commitment", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "non_actionable_diplomatic_intent", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "intent_commitment_mismatch", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("intent只从当前可选动作中选择；只有当前列出statement时才可使用无动作宣言。");
		}
		else if (string.Equals(reason, "target_kingdom_not_found", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "target_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "referenced_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "kingdom_not_in_autonomous_candidate_set", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "kingdom_not_in_targeted_generation_scope", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "kingdom_not_in_relay_route", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "diplomatic_action_has_no_target", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("actions中的target_kingdom_id只能使用当前列出的合法王国ID；同一对象只能出现一次。");
		}
		else if (string.Equals(reason, "autonomous_round_plan_incomplete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_has_invalid_participant", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_exceeds_participant_limit", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_omits_direct_target", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("自主开场必须同时填写round_plan.topic和selected_kingdom_ids。参与国只能来自候选范围，总数不得超过上限；actions中的全部对象必须列入selected_kingdom_ids。");
		}
		else if (string.Equals(reason, "accept_peace_changes_offer_terms", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("和平原案只能原样接受或明确拒绝，不得附加、改写条款或另提和平方案；来源及原条款由系统自动绑定。");
		}
		else if ((reason ?? "").StartsWith("visible_intent_mismatch:", StringComparison.OrdinalIgnoreCase)
			|| (reason ?? "").StartsWith("peace_terms_not_visible:", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("JSON意图与公开正文必须一致。正式动作要在标题或正文中明确写出；议和提案中的贡金、期限和割地必须逐项公开，不能只藏在JSON字段里。若正文没有实际动作，必须改选当前可选动作。");
		}
		else if ((reason ?? "").StartsWith("declare_war_not_legal:", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "intent_not_in_current_legal_action_list", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "break_alliance_without_alliance", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "cancel_trade_without_trade_agreement", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "alliance_system_unavailable", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "trade_system_unavailable", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("所选外交行动与当前真实关系不相容。保持国家自主判断，但改选当前状态下可以成立的对象与行动；不得把尚未生效的关系写成既成事实。");
		}
		if (!string.IsNullOrWhiteSpace(rejectedIntent)
			&& repairTarget != null
			&& (string.Equals(reason, "intent_not_in_current_legal_action_list", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)))
		{
			correctionBuilder.AppendLine("原草稿组合" + repairTarget.StringId + "=" + rejectedIntent + "无效，不得再次输出；只选当前可选组合。");
		}
		correctionBuilder.AppendLine("重新输出完整JSON并重写title和body；不要提到草稿、纠正、系统或上述错误。");
		correctionBuilder.AppendLine(BuildCurrentLegalDiplomaticOptions(
			repairRound,
			author,
			authorizedTargetIds,
			source.IsRelayTurn,
			source.ResultSettlementSlotId,
			source.IsExternalResponseOnly,
			ResolveDocument(source.SourceDocumentId)));
		string correction = BuildDeclareModePrompt(correctionBuilder.ToString());
		List<WorldDiplomacyLlmMessage> messages = CloneLlmMessages(BuildLlmMessagesForJob(source));
		messages.Add(new WorldDiplomacyLlmMessage { Role = "assistant", Content = rejectedRaw ?? "" });
		messages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = correction });
		bool resultSettlementRepair = source.IsRelayTurn
			&& repairRound?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(source.ResultSettlementSlotId);
		WorldDiplomacyJob repair = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_generate_repair"),
			Kind = "generate",
			Priority = source.Priority + 100,
			CreatedDay = source.CreatedDay,
			ExchangeId = source.ExchangeId ?? "",
			RoundId = source.RoundId ?? "",
			AuthorKingdomId = source.AuthorKingdomId ?? "",
			TargetKingdomId = source.TargetKingdomId ?? "",
			SourceDocumentId = source.SourceDocumentId ?? "",
			IsResponse = source.IsResponse,
			ForcedIntent = "",
			IsExternalResponseOnly = source.IsExternalResponseOnly,
			IsReminder = source.IsReminder,
			IsRelayTurn = source.IsRelayTurn,
			AllowUntargeted = source.AllowUntargeted,
			PreviousKingdomId = source.PreviousKingdomId ?? "",
			ResultSettlementSlotId = source.ResultSettlementSlotId ?? "",
			AllowAutonomousNoAction = false,
			CandidateKingdomIds = resultSettlementRepair
				? new List<string>(authorizedTargetIds)
				: new List<string>(source.CandidateKingdomIds ?? new List<string>()),
			PresentedThreatDocumentIds = new List<string>(source.PresentedThreatDocumentIds ?? new List<string>()),
			PresentedThreatFollowThroughDocumentIds = new List<string>(source.PresentedThreatFollowThroughDocumentIds ?? new List<string>()),
			WasAtWarWhenQueued = source.WasAtWarWhenQueued,
			SystemPrompt = source.SystemPrompt ?? "",
			UserPrompt = correction,
			LlmMessages = messages,
			ProfiledKingdomId = source.ProfiledKingdomId ?? "",
			StrategicProfileKingdomId = source.StrategicProfileKingdomId ?? "",
			CacheAffinityKey = source.CacheAffinityKey ?? "",
			HistoryThroughSequence = source.HistoryThroughSequence,
			HistoryRevision = source.HistoryRevision,
			HistoryPrefixHash = source.HistoryPrefixHash ?? "",
			HistoryEstimatedTokens = source.HistoryEstimatedTokens,
			HistorySnapshotThroughSequence = source.HistorySnapshotThroughSequence,
			HistorySnapshotHash = source.HistorySnapshotHash ?? "",
			MaxTokens = source.MaxTokens,
			SemanticRepairAttempts = source.SemanticRepairAttempts + 1
		};
		repair.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(repair);
		EnqueueJob(repair);
		Log("generated declaration repair queued sourceJob=" + source.JobId + " repairJob=" + repair.JobId + " reason=" + reason);
		return true;
	}

	private List<string> GetAuthorizedGenerationTargetIds(
		WorldDiplomacyJob source,
		WorldDiplomacyRound round,
		Kingdom author)
	{
		if (source == null || author == null) return new List<string>();
		List<string> ids = new List<string>();
		WorldDiplomacyDocument responseSource = ResolveDocument(source.SourceDocumentId);
		bool resultSettlementRepair = source.IsRelayTurn
			&& round?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(source.ResultSettlementSlotId);
		if (!resultSettlementRepair && !string.IsNullOrWhiteSpace(source.TargetKingdomId)) ids.Add(source.TargetKingdomId);
		if (source.AllowUntargeted) ids.AddRange(source.CandidateKingdomIds ?? new List<string>());
		if (source.IsRelayTurn)
		{
			if (resultSettlementRepair)
			{
				ids.AddRange(GetResultSettlementActionableTargets(round, author).Select(x => x.StringId));
			}
			else ids.AddRange(round?.RelayRouteKingdomIds ?? new List<string>());
		}
		if (ids.Count == 0) ids.AddRange(source.CandidateKingdomIds ?? new List<string>());
		return ids
			.Where(x => !string.IsNullOrWhiteSpace(x)
				&& !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Where(x =>
			{
				Kingdom candidate = ResolveKingdom(x);
				return candidate != null
					&& !candidate.IsEliminated
					&& HasIndependentWorldDiplomacyAuthority(candidate)
					&& BuildLegalDiplomaticDeclarationIntents(
						round,
						author,
						candidate,
						source.IsRelayTurn,
						source.ResultSettlementSlotId,
						source.IsExternalResponseOnly,
						responseSource).Count > 0;
			})
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void AbandonRejectedGeneration(WorldDiplomacyJob job, Kingdom author, Kingdom target, string reason)
	{
		if (job == null) return;
		Log("generated declaration abandoned without publication job=" + job.JobId
			+ " author=" + (author?.StringId ?? "") + " target=" + (target?.StringId ?? "")
			+ " reason=" + (reason ?? ""));
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (job.IsRelayTurn && round != null && string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase))
		{
			round.RelayWaiting = false;
			if (round.ResultSettlementPending)
			{
				SkipResultSettlementSlot(round, job.ResultSettlementSlotId, job.AuthorKingdomId, "generation_rejected");
				ScheduleNextResultSettlementTurn(round);
			}
			else AdvanceRelay(round);
			return;
		}
		CompleteExchange(job.ExchangeId, "technical_generation_rejected");
		if (round != null
			&& ReferenceEquals(_storage.ActiveRound, round)
			&& string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			&& string.IsNullOrWhiteSpace(round.RootDocumentId))
		{
			CloseActiveRound("technical_generation_rejected");
		}
	}

	private void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument document, string reason)
	{
		if (document == null) return;
		if (document.IsPlayerAuthored && document.IsReadyForPublication)
		{
			PreservePublishedPlayerDocumentAfterRejectedMechanic(document, reason);
			return;
		}
		Log("invalid generated document suppressed before propagation document=" + document.DocumentId
			+ " author=" + (document.AuthorKingdomId ?? "") + " target=" + (document.TargetKingdomId ?? "")
			+ " reason=" + (reason ?? ""));
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		bool wasRoundRoot = round != null
			&& string.Equals(round.RootDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase);
		_storage.Documents.RemoveAll(x => x != null && string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
		if (wasRoundRoot) round.RootDocumentId = "";
		if (round != null && ResolveDocument(round.RootDocumentId) == null)
		{
			round.RootDocumentId = _storage.Documents
				.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks)
				.Select(x => x.DocumentId)
				.FirstOrDefault() ?? "";
		}
		round?.LlmProfiledKingdomIds?.RemoveAll(x => string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (document.IsPlayerAuthored && document.IsResponse && round != null)
		{
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: true);
			participant.MandatoryReplyPending = true;
			WorldDiplomacyPlayerOpportunity opportunity = (_storage.PlayerOpportunities ?? new List<WorldDiplomacyPlayerOpportunity>())
				.FirstOrDefault(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
			if (opportunity != null) opportunity.Status = "open";
		}
		if (document.IsRelayTurn && round != null && string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase))
		{
			if (round.ResultSettlementPending)
			{
				if (document.IsPlayerAuthored)
				{
					round.RelayWaiting = true;
					RecordPlayerOpportunity(round, ResolveKingdom(document.AuthorKingdomId));
				}
				else
				{
					round.RelayWaiting = false;
					SkipResultSettlementSlot(round, document.ResultSettlementSlotId, document.AuthorKingdomId, "invalid_document");
					ScheduleNextResultSettlementTurn(round);
				}
			}
			else
			{
				round.RelayWaiting = false;
				AdvanceRelay(round);
			}
			return;
		}
		CompleteExchange(document.ExchangeId, "technical_invalid_document_suppressed");
		if (round != null
			&& ReferenceEquals(_storage.ActiveRound, round)
			&& string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			&& string.IsNullOrWhiteSpace(round.RootDocumentId)
			&& !_storage.Documents.Any(x => x != null && x.IsReadyForPublication
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			&& !_storage.Jobs.Any(x => x != null
				&& string.Equals(FirstNonEmpty(x.RoundId, x.ExchangeId), round.RoundId, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)))
		{
			CloseActiveRound(document.IsPlayerAuthored
				? "player_declaration_rejected"
				: "technical_invalid_document_suppressed");
		}
	}

	private bool TryApplyGeneratedSemanticEnvelope(
		WorldDiplomacyDocument document,
		JObject json,
		Kingdom author,
		Kingdom fallbackTarget,
		bool allowUntargeted,
		bool relayTurn)
	{
		if (document == null || json == null || author == null || json["actions"] is not JArray actions
			|| actions.Count < 1 || actions.Count > MaxDiplomaticActionsPerDocument) return false;
		List<WorldDiplomacyDocumentAction> applied = new List<WorldDiplomacyDocumentAction>(actions.Count);
		bool anyRoundResponseNoAction = false;
		bool anyWarResponseNoAction = false;
		for (int index = 0; index < actions.Count; index++)
		{
			if (actions[index] is not JObject actionEnvelope) return false;
			JObject single = BuildGeneratedSingleActionEnvelope(json, actionEnvelope);
			WorldDiplomacyDocument actionDocument = new WorldDiplomacyDocument
			{
				DocumentId = document.DocumentId,
				RoundId = document.RoundId,
				AuthorKingdomId = document.AuthorKingdomId,
				SourceDocumentId = FirstNonEmpty(
					ReadString(actionEnvelope, "responding_to_offer_document_id"),
					ReadString(actionEnvelope, "responding_to_threat_document_id"),
					document.SourceDocumentId),
				ResultSettlementSlotId = document.ResultSettlementSlotId,
				IsExternalResponseOnly = document.IsExternalResponseOnly,
				IsRelayTurn = document.IsRelayTurn
			};
			if (!TryApplyGeneratedSingleActionSemanticEnvelope(
				actionDocument,
				single,
				author,
				actions.Count == 1 ? fallbackTarget : null,
				allowUntargeted: actions.Count == 1 && allowUntargeted,
				relayTurn)) return false;
			WorldDiplomacyDocumentAction action = new WorldDiplomacyDocumentAction
			{
				ActionId = "action_" + (index + 1).ToString(CultureInfo.InvariantCulture),
				TargetKingdomId = actionDocument.TargetKingdomId ?? "",
				TargetKingdomName = actionDocument.TargetKingdomName ?? "",
				Intent = NormalizeIntent(actionDocument.Intent),
				NegotiationMove = NormalizeNegotiationMove(actionDocument.NegotiationMove),
				Commitment = NormalizeCommitment(actionDocument.Commitment),
				RequiresResponse = actionDocument.RequiresResponse,
				RespondingToOfferDocumentId = ReadString(actionEnvelope, "responding_to_offer_document_id"),
				RespondingToOfferActionId = ReadString(actionEnvelope, "responding_to_offer_action_id"),
				RespondingToThreatDocumentId = ReadString(actionEnvelope, "responding_to_threat_document_id"),
				RespondingToThreatActionId = ReadString(actionEnvelope, "responding_to_threat_action_id"),
				PeaceTerms = actionDocument.PeaceTerms
			};
			applied.Add(action);
			anyRoundResponseNoAction |= actionDocument.IsRoundResponseNoActionDeclaration;
			anyWarResponseNoAction |= actionDocument.IsWarResponseNoActionDeclaration;
		}
		document.Actions = applied;
		document.AddressedKingdomIds = NormalizeKingdomIdList(
			applied.Select(x => x.TargetKingdomId),
			author.StringId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(
			ReadStringList(json, "mentioned_kingdom_ids", "mentioned"),
			author.StringId);
		document.Tone = NormalizeTone(ReadString(json, "tone"));
		document.Confidence = Math.Max(0f, Math.Min(1f, ReadFloat(json, "confidence")));
		document.RequiresResponse = applied.Any(x => x.RequiresResponse);
		document.IsRoundResponseNoActionDeclaration = anyRoundResponseNoAction;
		document.IsWarResponseNoActionDeclaration = anyWarResponseNoAction;
		document.AnalysisStatus = "generation_envelope";
		MirrorPrimaryActionToDocument(document, applied[0]);
		return true;
	}

	private bool TryApplyGeneratedSingleActionSemanticEnvelope(
		WorldDiplomacyDocument document,
		JObject json,
		Kingdom author,
		Kingdom fallbackTarget,
		bool allowUntargeted,
		bool relayTurn)
	{
		if (document == null || json == null
			|| !(json["author_intent"] is JObject)
			|| !IsJsonStringArray(json["addressed_kingdom_ids"])
			|| !IsJsonStringArray(json["mentioned_kingdom_ids"])
			|| !(json["round_plan"] is JObject roundPlanEnvelope)
			|| !IsJsonStringArray(roundPlanEnvelope["selected_kingdom_ids"])
			|| !(json["peace_terms"] is JObject)
			|| json["requires_response"] == null
			|| json["tone"] == null
			|| json["confidence"] == null
			|| json["primary_target_kingdom_id"] == null)
		{
			return false;
		}
		string intent = NormalizeIntent(ReadString(json, "author_intent.intent", "intent"));
		string negotiationMove = NormalizeNegotiationMove(ReadString(json, "negotiation_move"));
		string commitment = NormalizeCommitment(ReadString(json, "author_intent.commitment", "commitment"));
		if (!IsSupportedCommitment(commitment))
		{
			return false;
		}
		string generatedTargetId = ReadString(json, "primary_target_kingdom_id");
		Kingdom target = ResolveKingdom(generatedTargetId);
		if (target == null && string.IsNullOrWhiteSpace(generatedTargetId) && !allowUntargeted) target = fallbackTarget;
		if ((target == null && !allowUntargeted) || target == author)
		{
			return false;
		}
		WorldDiplomacyRound envelopeRound = ResolveRound(document.RoundId);
		WorldDiplomacyDocument responseSource = ResolveDocument(document.SourceDocumentId);
		bool allowedRoundResponseNoAction = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
			&& IsNonRootAiRelayNoActionAllowed(
				envelopeRound,
				document.ResultSettlementSlotId,
				author,
				target,
				relayTurn,
				document.IsExternalResponseOnly,
				responseSource);
		bool allowedWarResponseNoAction = allowedRoundResponseNoAction
			&& IsWarResponseNoActionAllowed(envelopeRound, document.ResultSettlementSlotId, author, target);
		if (string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase)
			&& (!IsSupportedNegotiationMove(negotiationMove)
				|| (envelopeRound?.ConsecutiveNoActionPasses >= 2 && !IsTerminalNegotiationMove(negotiationMove))))
		{
			return false;
		}
		if (!IsActionableDiplomacyIntent(intent) && !allowedRoundResponseNoAction)
		{
			return false;
		}
		bool resultSettlementRelay = relayTurn && envelopeRound?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(document.ResultSettlementSlotId);
		if (relayTurn && target != null
			&& !(resultSettlementRelay
				? CanUseResultSettlementTarget(envelopeRound, author, target)
				: RoundRouteContainsKingdom(envelopeRound, target.StringId))) return false;
		document.TargetKingdomId = target?.StringId ?? "";
		document.TargetKingdomName = target == null ? "" : KingdomName(target);
		List<string> addressed = ReadStringList(json, "addressed_kingdom_ids", "addressed");
		List<string> mentioned = ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
		if (addressed.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null)
			|| mentioned.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null))
		{
			return false;
		}
		if (relayTurn && addressed.Any(x => resultSettlementRelay
			? !string.Equals(x, target?.StringId, StringComparison.OrdinalIgnoreCase)
				&& !RoundRouteContainsKingdom(envelopeRound, x)
			: !RoundRouteContainsKingdom(envelopeRound, x))) return false;
		document.AddressedKingdomIds = NormalizeKingdomIdList(addressed.Concat(target == null ? Enumerable.Empty<string>() : new[] { target.StringId }), author.StringId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(mentioned, author.StringId);
		document.Intent = intent;
		document.NegotiationMove = string.Equals(intent, "statement", StringComparison.OrdinalIgnoreCase) ? negotiationMove : "";
		document.Commitment = commitment;
		document.IsRoundResponseNoActionDeclaration = allowedRoundResponseNoAction;
		document.IsWarResponseNoActionDeclaration = allowedWarResponseNoAction;
		document.Tone = NormalizeTone(ReadString(json, "tone"));
		document.Confidence = Math.Max(0f, Math.Min(1f, ReadFloat(json, "confidence")));
		document.RequiresResponse = allowedRoundResponseNoAction
			? false
			: ResolveValidatedResponseObligation(document, intent, ReadBool(json, "requires_response"));
		document.PeaceTerms = target == null ? document.PeaceTerms : (ParseAndValidatePeaceTerms(json, author, target) ?? document.PeaceTerms);
		document.AnalysisStatus = "generation_envelope";
		return true;
	}

	private static void MirrorPrimaryActionToDocument(
		WorldDiplomacyDocument document,
		WorldDiplomacyDocumentAction action)
	{
		if (document == null || action == null) return;
		document.TargetKingdomId = action.TargetKingdomId ?? "";
		document.TargetKingdomName = action.TargetKingdomName ?? "";
		document.Intent = NormalizeIntent(action.Intent);
		document.NegotiationMove = NormalizeNegotiationMove(action.NegotiationMove);
		document.Commitment = NormalizeCommitment(action.Commitment);
		document.HiddenIntent = document.Intent;
		document.HiddenCommitment = document.Commitment;
		document.PeaceTerms = action.PeaceTerms;
		document.RespondingToOfferDocumentId = action.RespondingToOfferDocumentId ?? "";
		document.RespondingToOfferActionId = action.RespondingToOfferActionId ?? "";
		document.RespondingToThreatDocumentId = action.RespondingToThreatDocumentId ?? "";
		document.RespondingToThreatActionId = action.RespondingToThreatActionId ?? "";
		document.SourceDocumentId = FirstNonEmpty(
			document.RespondingToOfferDocumentId,
			document.RespondingToThreatDocumentId,
			document.SourceDocumentId);
	}

	private void CommitAnalysis(WorldDiplomacyJob job, string raw)
	{
		WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
		if (document == null)
		{
			return;
		}
		JObject json = ParseJsonObject(raw);
		string status = NormalizeToken(ReadString(json, "status"));
		string intent = NormalizeIntent(ReadString(json, "intent", "diplomatic_intent"));
		string titleSummary = ReadString(json, "title_summary", "summary_title");
		string targetId = ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
		List<string> addressedIds = ReadStringList(json, "addressed_kingdom_ids", "addressed");
		List<string> mentionedIds = ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
		string commitment = NormalizeCommitment(ReadString(json, "commitment"));
		string tone = NormalizeTone(ReadString(json, "tone"));
		float confidence = ReadFloat(json, "confidence");
		bool requiresResponse = ReadBool(json, "requires_response");
		string respondingToOfferDocumentId = ReadString(json, "responding_to_offer_document_id");
		string respondingToThreatDocumentId = ReadString(json, "responding_to_threat_document_id");
		if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(status, "fallback", StringComparison.OrdinalIgnoreCase))
		{
			if (!document.IsPlayerAuthored)
			{
				document.AnalysisStatus = "no_action";
				SuppressInvalidDocumentBeforePropagation(document, "analysis_status_has_no_publishable_action");
				return;
			}
			// Player speech is already public and authoritative. A no-action or malformed
			// classifier result means "public statement", never "permission denied".
			status = "fallback";
			intent = "statement";
			commitment = "non_binding";
			Log("player declaration analysis downgraded to public statement document=" + document.DocumentId
				+ " reason=analysis_status_" + NormalizeToken(ReadString(json, "status")));
		}
		if (string.IsNullOrWhiteSpace(intent))
		{
			if (!document.IsPlayerAuthored)
			{
				document.AnalysisStatus = "no_action";
				SuppressInvalidDocumentBeforePropagation(document, "analysis_has_no_structured_intent");
				return;
			}
			status = "fallback";
			intent = "statement";
			commitment = "non_binding";
			Log("player declaration analysis supplied no intent; retained as public statement document=" + document.DocumentId);
		}
		if (document.IsPlayerAuthored)
		{
			ReconcilePlayerDeclarationWithOpenOffer(document, intent, ref targetId, ref respondingToOfferDocumentId);
		}
		bool playerPublicIntent = document.IsPlayerAuthored && IsSupportedDiplomacyIntent(intent);
		if ((!IsActionableDiplomacyIntent(intent) && !playerPublicIntent)
			|| !CommitmentMatchesIntent(intent, commitment))
		{
			if (!document.IsPlayerAuthored)
			{
				document.AnalysisStatus = "no_action";
				SuppressInvalidDocumentBeforePropagation(document, "analysis_has_no_actionable_intent");
				return;
			}
			if (!IsSupportedDiplomacyIntent(intent)) intent = "statement";
			commitment = DefaultCommitmentForIntent(intent);
			status = "fallback";
			Log("player declaration analysis normalized without suppressing publication document=" + document.DocumentId
				+ " intent=" + intent + " commitment=" + commitment);
		}
		if (string.IsNullOrWhiteSpace(targetId))
		{
			targetId = document.TargetKingdomId;
		}
		Kingdom target = ResolveKingdom(targetId);
		if (target != null && !string.Equals(target.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			document.TargetKingdomId = target.StringId;
			document.TargetKingdomName = KingdomName(target);
		}
		WorldDiplomacyPeaceTerms analyzedPeaceTerms = ParseAndValidatePeaceTerms(
			json,
			ResolveKingdom(document.AuthorKingdomId),
			target);
		if (document.IsPlayerAuthored
			&& string.Equals(intent, "accept_peace", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(respondingToOfferDocumentId))
		{
			WorldDiplomacyDocument source = ResolveDocument(respondingToOfferDocumentId);
			document.PeaceTerms = ClonePeaceTerms(ResolveOfferedPeaceTerms(
				source,
				document.RespondingToOfferActionId));
		}
		else
		{
			document.PeaceTerms = analyzedPeaceTerms ?? document.PeaceTerms;
		}
		IEnumerable<string> directTargets = addressedIds.Concat(new[] { document.TargetKingdomId });
		document.AddressedKingdomIds = NormalizeKingdomIdList(directTargets, document.AuthorKingdomId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(mentionedIds, document.AuthorKingdomId);
		document.AnalysisStatus = status == "success" ? "success" : "fallback";
		document.Title = !string.IsNullOrWhiteSpace(titleSummary)
			? Limit(SanitizePublicDiplomacyText(titleSummary), 36)
			: (document.IsPlayerAuthored ? BuildFallbackDocumentTitle(document, intent) : document.Title);
		document.Intent = intent;
		document.Commitment = commitment;
		// Player text is immutable once submitted, but publication order is not. A
		// threat decision or follow-through that became due while analysis was queued
		// still applies to this next published declaration.
		document.PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(document.AuthorKingdomId);
		document.PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(document.AuthorKingdomId);
		document.RespondingToOfferDocumentId = respondingToOfferDocumentId ?? "";
		document.RespondingToThreatDocumentId = respondingToThreatDocumentId ?? "";
		if (!string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			document.SourceDocumentId = document.RespondingToOfferDocumentId;
			document.IsResponse = true;
		}
		else if (!string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
		{
			document.SourceDocumentId = document.RespondingToThreatDocumentId;
			document.IsResponse = true;
		}
		document.Tone = tone;
		document.Confidence = confidence;
		document.RequiresResponse = ResolveValidatedResponseObligation(document, intent, requiresResponse);
		ApplyInternationalReputationEvaluation(document, json);
		ProcessAnalyzedDocument(document, intent, commitment, document.RequiresResponse, tone, confidence);
	}

	private void ReconcilePlayerDeclarationWithOpenOffer(
		WorldDiplomacyDocument document,
		string intent,
		ref string targetId,
		ref string respondingToOfferDocumentId)
	{
		WorldDiplomacyRound round = ResolveRound(document?.RoundId);
		if (document == null || round == null) return;
		string proposalIntent = ResponseIntentToProposalIntent(intent);
		if (string.IsNullOrWhiteSpace(proposalIntent)) return;
		string claimedOfferDocumentId = respondingToOfferDocumentId ?? "";
		IEnumerable<WorldDiplomacyRoundOffer> candidates = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(claimedOfferDocumentId))
		{
			candidates = candidates.Where(x => string.Equals(x.SourceDocumentId, claimedOfferDocumentId, StringComparison.OrdinalIgnoreCase));
		}
		string requestedTargetId = FirstNonEmpty(targetId, document.TargetKingdomId);
		if (!string.IsNullOrWhiteSpace(requestedTargetId))
		{
			candidates = candidates.Where(x => string.Equals(x.ProposerKingdomId, requestedTargetId, StringComparison.OrdinalIgnoreCase));
		}
		List<WorldDiplomacyRoundOffer> matches = candidates.Take(2).ToList();
		if (matches.Count != 1) return;
		WorldDiplomacyRoundOffer offer = matches[0];
		targetId = offer.ProposerKingdomId;
		respondingToOfferDocumentId = offer.SourceDocumentId;
		document.RespondingToOfferActionId = offer.SourceActionId ?? "";
		Log("player declaration bound to open offer document=" + document.DocumentId
			+ " offer=" + offer.SourceDocumentId + " intent=" + intent + " proposer=" + offer.ProposerKingdomId);
	}

	private void ProcessAnalyzedDocument(
		WorldDiplomacyDocument document,
		string intent,
		string commitment,
		bool requiresResponse,
		string tone,
		float confidence)
	{
		if (document?.Actions?.Count > 0)
		{
			ProcessAnalyzedMultiActionDocument(document);
			return;
		}
		Kingdom author = ResolveKingdom(document.AuthorKingdomId);
		Kingdom target = ResolveKingdom(document.TargetKingdomId);
		if (author == null)
		{
			return;
		}
		if (!document.IsPlayerAuthored && !HasIndependentWorldDiplomacyAuthority(author))
		{
			Log("controlled vassal document blocked before propagation document=" + document.DocumentId
				+ " author=" + author.StringId);
			SuppressInvalidDocumentBeforePropagation(document, "controlled_vassal_has_no_diplomatic_authority");
			return;
		}
		if (!document.IsPlayerAuthored && !CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			Log("AI document blocked before propagation document=" + document.DocumentId + " author=" + author.StringId
				+ " reason=" + authorBlockReason);
			SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		string normalizedIntent = NormalizeIntent(intent);
		WorldDiplomacyRound owningRound = ResolveRound(document.RoundId);
		PruneInvalidOffers(owningRound);
		bool claimedRoundResponseNoAction = document.IsRoundResponseNoActionDeclaration
			|| document.IsWarResponseNoActionDeclaration;
		bool allowedRoundResponseNoAction = !document.IsPlayerAuthored
			&& claimedRoundResponseNoAction
			&& string.Equals(normalizedIntent, "statement", StringComparison.OrdinalIgnoreCase)
			&& IsNonRootAiRelayNoActionAllowed(
				owningRound,
				document.ResultSettlementSlotId,
				author,
				target,
				document.IsRelayTurn,
				document.IsExternalResponseOnly,
				ResolveDocument(document.SourceDocumentId));
		if (claimedRoundResponseNoAction && !allowedRoundResponseNoAction)
		{
			SuppressInvalidDocumentBeforePropagation(document, "stale_round_response_no_action_declaration");
			return;
		}
		bool allowedPlayerPublicIntent = document.IsPlayerAuthored
			&& IsSupportedDiplomacyIntent(normalizedIntent)
			&& !IsActionableDiplomacyIntent(normalizedIntent);
		bool allowedNoAction = allowedRoundResponseNoAction || allowedPlayerPublicIntent;
		if (!IsActionableDiplomacyIntent(normalizedIntent) && !allowedNoAction)
		{
			SuppressInvalidDocumentBeforePropagation(document, "non_actionable_diplomatic_intent");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：正文必须明确包含一项可执行的外交动作。"));
			}
			return;
		}
		if (allowedPlayerPublicIntent)
		{
			document.IsReadyForPublication = true;
			try
			{
				ApplyDocumentPressure(document);
				ApplyDiplomaticPressureEffect(document);
			}
			catch (Exception ex)
			{
				Log("player public-statement effect failed without hiding declaration document="
					+ document.DocumentId + " intent=" + normalizedIntent + " error=" + ex.Message);
			}
			FinalizePublishedDocumentAfterAnalysis(document, author, target, normalizedIntent, recordNoActionDecision: true);
			return;
		}
		if (owningRound?.ResultSettlementPending == true
			&& target != null
			&& !RoundRouteContainsKingdom(owningRound, target.StringId)
			&& !CanUseResultSettlementTarget(owningRound, author, target))
		{
			Log("result-settlement document target blocked because participant expansion is unavailable document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + target.StringId);
			SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：本次外交事件已无法再加入新的处理国。"));
			}
			return;
		}
		string liveStateBlockReason = "";
		bool invalidLiveTarget = target == null || target == author || target.IsEliminated
			|| !HasIndependentWorldDiplomacyAuthority(target);
		if (invalidLiveTarget
			|| TryGetDiplomaticStateViolation(normalizedIntent, author, target, out liveStateBlockReason))
		{
			if (invalidLiveTarget) liveStateBlockReason = "diplomatic_action_has_no_live_target";
			Log("diplomatic action blocked by final live-state guard document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + (target?.StringId ?? "")
				+ " intent=" + normalizedIntent + " reason=" + liveStateBlockReason);
			SuppressInvalidDocumentBeforePropagation(document, "final_live_state_guard:" + liveStateBlockReason);
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。"));
			}
			return;
		}
		List<string> finalLiveIntents = document.IsPlayerAuthored
			? BuildLegalDiplomaticActionIntents(owningRound, author, target)
			: BuildLegalDiplomaticDeclarationIntents(
				owningRound,
				author,
				target,
				document.IsRelayTurn,
				document.ResultSettlementSlotId,
				document.IsExternalResponseOnly,
				ResolveDocument(document.SourceDocumentId));
		if (!finalLiveIntents.Contains(normalizedIntent, StringComparer.OrdinalIgnoreCase))
		{
			SuppressInvalidDocumentBeforePropagation(document, "final_live_legal_action_guard");
			return;
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			owningRound,
			author,
			document.ResultSettlementSlotId,
			document.IsExternalResponseOnly,
			document.SourceDocumentId,
			requireAnyOpenPeaceOffer: document.IsRelayTurn || document.IsPlayerAuthored);
		if (!DocumentContainsRequiredPeaceOfferResponse(document, requiredPeaceOffer))
		{
			SuppressInvalidDocumentBeforePropagation(document, "required_peace_offer_response_missing");
			if (document.IsPlayerAuthored && !document.IsReadyForPublication)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：本篇必须先接受或拒绝当前和平原案。"));
			}
			return;
		}
		if (normalizedIntent == "propose_peace"
			&& IsImmediateWarResponsePeaceSuppressed(owningRound, document.ResultSettlementSlotId, author, target))
		{
			SuppressInvalidDocumentBeforePropagation(document, "immediate_war_response_peace_suppressed");
			return;
		}
		if (document.IsPlayerAuthored
			&& TryGetPlayerWorldStateIntentViolation(document, normalizedIntent, commitment, author, target, out string playerActionBlockReason))
		{
			Log("player diplomatic action blocked before execution document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + (target?.StringId ?? "")
				+ " intent=" + normalizedIntent + " reason=" + playerActionBlockReason);
			SuppressInvalidDocumentBeforePropagation(document, "player_action_not_executable:" + playerActionBlockReason);
			if (!document.IsReadyForPublication)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：正文中的外交动作与当前真实状态不相容。"));
			}
			return;
		}
		string responseProposalIntent = ResponseIntentToProposalIntent(normalizedIntent);
		if (!document.IsPlayerAuthored && !string.IsNullOrWhiteSpace(responseProposalIntent)
			&& (target == null || !HasOpenProposalForDocument(document, author, target, responseProposalIntent)))
		{
			Log("invalid AI offer response blocked before propagation document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + (target?.StringId ?? "") + " intent=" + normalizedIntent);
			SuppressInvalidDocumentBeforePropagation(document, "offer_ownership_guard");
			return;
		}
		if (!document.IsPlayerAuthored && target != null
			&& IsPeaceIntent(normalizedIntent)
			&& !FactionManager.IsAtWarAgainstFaction(author, target))
		{
			Log("illegal AI peace intent blocked before propagation document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + target.StringId + " intent=" + normalizedIntent);
			SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
			return;
		}
		bool appendedResultSettlementTarget = owningRound?.ResultSettlementPending == true
			&& target != null && !RoundRouteContainsKingdom(owningRound, target.StringId);
		if (appendedResultSettlementTarget
			&& !TryIncludeResultSettlementTarget(owningRound, target.StringId))
		{
			SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			return;
		}
		if (appendedResultSettlementTarget)
		{
			AddOrMergeResultSettlementSlot(owningRound, target.StringId, "route",
				document.DocumentId, author.StringId, prioritize: false);
		}
		// Make the validated declaration minimally publishable before any irreversible game
		// action. Full geographic propagation is filled in below.
		document.IsReadyForPublication = true;
		try
		{
			if (!allowedNoAction)
			{
				ApplyDocumentPressure(document);
				if (target != null && target != author && IsImmediateIntent(normalizedIntent))
				{
					ExecuteImmediateIntent(author, target, normalizedIntent, document);
				}
				ProcessDiplomaticThreatDocument(document, author, target);
				TrySettleRelayOffer(document);
				ApplyDiplomaticPressureEffect(document);
			}
			else if (allowedNoAction)
			{
				// This is mechanically inert, but it is still the kingdom's next published
				// declaration for any already-presented threat decision.
				RecordDiplomaticThreatTargetDecisions(document, author, target, normalizedIntent);
			}
		}
		catch (Exception ex)
		{
			if (string.IsNullOrWhiteSpace(document.MechanicalResult))
			{
				document.MechanicalResult = "外交机制未执行：" + Limit(ex.Message, 180);
			}
			Log("diplomatic mechanism failed without discarding valid declaration document=" + document.DocumentId
				+ " intent=" + normalizedIntent + " error=" + ex.Message);
		}
		FinalizePublishedDocumentAfterAnalysis(document, author, target, normalizedIntent, allowedNoAction);
	}

	private void PreservePublishedPlayerDocumentAfterRejectedMechanic(
		WorldDiplomacyDocument document,
		string reason)
	{
		if (document == null) return;
		string normalizedIntent = NormalizeIntent(document.Intent);
		if (!IsSupportedDiplomacyIntent(normalizedIntent))
		{
			normalizedIntent = "statement";
			document.Intent = normalizedIntent;
		}
		if (!CommitmentMatchesIntent(normalizedIntent, document.Commitment))
		{
			document.Commitment = DefaultCommitmentForIntent(normalizedIntent);
		}
		document.AnalysisStatus = "published_action_rejected";
		if (string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			document.MechanicalResult = "外交动作未执行：当前局势不支持解析出的动作。";
		}
		Log("published player declaration retained after mechanic rejection document=" + document.DocumentId
			+ " intent=" + normalizedIntent + " reason=" + (reason ?? ""));
		InformationManager.DisplayMessage(new InformationMessage(
			"外交宣言已经发布，但其中解析出的外交动作因当前局势不成立而未执行。"));
		FinalizePublishedDocumentAfterAnalysis(
			document,
			ResolveKingdom(document.AuthorKingdomId),
			ResolveKingdom(document.TargetKingdomId),
			normalizedIntent,
			recordNoActionDecision: true);
	}

	private void FinalizePublishedDocumentAfterAnalysis(
		WorldDiplomacyDocument document,
		Kingdom author,
		Kingdom target,
		string normalizedIntent,
		bool recordNoActionDecision)
	{
		if (document == null || author == null) return;
		document.IsReadyForPublication = true;
		if (recordNoActionDecision)
		{
			RecordDiplomaticThreatTargetDecisions(document, author, target, normalizedIntent);
		}
		bool requiredThreatActionDeferred = DeferUnresolvedRequiredThreatAction(document, author, target, normalizedIntent);
		if (!requiredThreatActionDeferred)
		{
			SettleDiplomaticThreatFollowThroughAfterDeclaration(document, author);
		}
		SettleInternationalReputationForDocument(document);
		try
		{
			StartDocumentPropagation(document, author);
		}
		catch (Exception ex)
		{
			document.PropagationCompleted = false;
			Log("valid declaration propagation deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try
		{
			RecordDiplomacyWeeklyMaterial(document);
			ReconcileAnalyzedPlayerDeclarationWithReachedCourts(document);
		}
		catch (Exception ex)
		{
			Log("analyzed player declaration routing refresh deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try
		{
			AppendCanonicalDocumentEvents(document);
			FinalizeDiplomaticThreatHistoryAfterDocument(document);
			FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document);
		}
		catch (Exception ex)
		{
			ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
			Log("canonical history append deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try
		{
			HandleRoundDocumentProcessed(document);
		}
		catch (Exception ex)
		{
			Log("valid declaration round progress deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
	}

	private void ReconcileAnalyzedPlayerDeclarationWithReachedCourts(WorldDiplomacyDocument document)
	{
		if (document?.IsPlayerAuthored != true) return;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null || !ReferenceEquals(_storage.ActiveRound, round)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		foreach (string kingdomId in GetKnownKingdomIdsForDocument(document.DocumentId))
		{
			Kingdom receiver = ResolveKingdom(kingdomId);
			if (receiver == null || string.Equals(receiver.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				|| !HasIndependentWorldDiplomacyAuthority(receiver)) continue;
			bool directlyAddressed = (document.AddressedKingdomIds ?? new List<string>())
				.Contains(receiver.StringId, StringComparer.OrdinalIgnoreCase)
				|| string.Equals(document.TargetKingdomId, receiver.StringId, StringComparison.OrdinalIgnoreCase)
				|| IsDiplomaticRepresentativeForAddressedVassal(receiver, document);
			bool isPrimaryTarget = string.Equals(document.TargetKingdomId, receiver.StringId, StringComparison.OrdinalIgnoreCase);
			if (!directlyAddressed || (!isPrimaryTarget && !DocumentRequiresResponseFrom(document, receiver.StringId))) continue;
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(
				round,
				receiver.StringId,
				"active",
				mandatoryReply: true);
			TryScheduleMandatoryCourtResponse(round, participant, receiver, document);
		}
	}

	private void ProcessAnalyzedMultiActionDocument(WorldDiplomacyDocument document)
	{
		List<WorldDiplomacyDocumentAction> actions = document?.Actions;
		Kingdom author = ResolveKingdom(document?.AuthorKingdomId);
		if (document == null || actions == null || actions.Count < 1
			|| actions.Count > MaxDiplomaticActionsPerDocument || author == null) return;
		if (!document.IsPlayerAuthored && !HasIndependentWorldDiplomacyAuthority(author))
		{
			SuppressInvalidDocumentBeforePropagation(document, "controlled_vassal_has_no_diplomatic_authority");
			return;
		}
		if (!document.IsPlayerAuthored && !CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		string sourceContextDocumentId = document.SourceDocumentId ?? "";
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		PruneInvalidOffers(round);
		List<Kingdom> targets = new List<Kingdom>(actions.Count);
		HashSet<string> uniqueTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> newSettlementTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int statementCount = 0;
		for (int index = 0; index < actions.Count; index++)
		{
			WorldDiplomacyDocumentAction action = actions[index];
			Kingdom target = ResolveKingdom(action?.TargetKingdomId);
			string intent = NormalizeIntent(action?.Intent);
			if (action == null || target == null || target == author || target.IsEliminated
				|| !HasIndependentWorldDiplomacyAuthority(target) || !uniqueTargets.Add(target.StringId))
			{
				SuppressInvalidDocumentBeforePropagation(document, "multi_action_has_invalid_or_duplicate_target");
				return;
			}
			bool noAction = intent == "statement";
			if (noAction) statementCount++;
			bool allowedNoAction = noAction && !document.IsPlayerAuthored
				&& (document.IsRoundResponseNoActionDeclaration || document.IsWarResponseNoActionDeclaration)
				&& IsNonRootAiRelayNoActionAllowed(
					round,
					document.ResultSettlementSlotId,
					author,
					target,
					document.IsRelayTurn,
					document.IsExternalResponseOnly,
					ResolveDocument(document.SourceDocumentId));
			if ((!IsActionableDiplomacyIntent(intent) && !allowedNoAction)
				|| !CommitmentMatchesIntent(intent, action.Commitment))
			{
				SuppressInvalidDocumentBeforePropagation(document, "multi_action_is_not_executable");
				return;
			}
			List<string> finalLiveIntents = document.IsPlayerAuthored
				? BuildLegalDiplomaticActionIntents(round, author, target)
				: BuildLegalDiplomaticDeclarationIntents(
					round,
					author,
					target,
					document.IsRelayTurn,
					document.ResultSettlementSlotId,
					document.IsExternalResponseOnly,
					ResolveDocument(document.SourceDocumentId));
			if (!finalLiveIntents.Contains(intent, StringComparer.OrdinalIgnoreCase))
			{
				SuppressInvalidDocumentBeforePropagation(document, "final_live_legal_action_guard");
				return;
			}
			if (round?.ResultSettlementPending == true && !RoundRouteContainsKingdom(round, target.StringId))
			{
				if (!CanUseResultSettlementTarget(round, author, target))
				{
					SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
					return;
				}
				newSettlementTargets.Add(target.StringId);
			}
			else if (document.IsRelayTurn && round != null && !RoundRouteContainsKingdom(round, target.StringId))
			{
				SuppressInvalidDocumentBeforePropagation(document, "kingdom_not_in_relay_route");
				return;
			}
			if (TryGetDiplomaticStateViolation(intent, author, target, out string liveStateReason))
			{
				SuppressInvalidDocumentBeforePropagation(document, "final_live_state_guard:" + liveStateReason);
				return;
			}
			if (intent == "propose_peace"
				&& IsImmediateWarResponsePeaceSuppressed(round, document.ResultSettlementSlotId, author, target))
			{
				SuppressInvalidDocumentBeforePropagation(document, "immediate_war_response_peace_suppressed");
				return;
			}
			MirrorPrimaryActionToDocument(document, action);
			string proposalIntent = ResponseIntentToProposalIntent(intent);
			if (!document.IsPlayerAuthored && !string.IsNullOrWhiteSpace(proposalIntent)
				&& !HasOpenProposalForDocument(document, author, target, proposalIntent))
			{
				SuppressInvalidDocumentBeforePropagation(document, "offer_ownership_guard");
				return;
			}
			if (!document.IsPlayerAuthored && IsPeaceIntent(intent)
				&& !FactionManager.IsAtWarAgainstFaction(author, target))
			{
				SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
				return;
			}
			targets.Add(target);
		}
		if (statementCount > 0 && actions.Count != 1)
		{
			SuppressInvalidDocumentBeforePropagation(document, "statement_must_be_the_only_diplomatic_action");
			return;
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			round,
			author,
			document.ResultSettlementSlotId,
			document.IsExternalResponseOnly,
			document.SourceDocumentId,
			requireAnyOpenPeaceOffer: document.IsRelayTurn || document.IsPlayerAuthored);
		if (!DocumentContainsRequiredPeaceOfferResponse(document, requiredPeaceOffer))
		{
			SuppressInvalidDocumentBeforePropagation(document, "required_peace_offer_response_missing");
			if (document.IsPlayerAuthored)
			{
				InformationManager.DisplayMessage(new InformationMessage("外交宣言没有发布：本篇必须先接受或拒绝当前和平原案。"));
			}
			return;
		}
		if (DocumentHasUnsafeMultiplePeaceAcceptances(document))
		{
			SuppressInvalidDocumentBeforePropagation(document, "multiple_peace_acceptances_have_cross_terms");
			return;
		}
		if (round?.ResultSettlementPending == true
			&& (round.RelayRouteKingdomIds?.Count ?? 0) + newSettlementTargets.Count > MaxRelayParticipants)
		{
			SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
			return;
		}
		foreach (string targetId in newSettlementTargets.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			if (!TryIncludeResultSettlementTarget(round, targetId))
			{
				SuppressInvalidDocumentBeforePropagation(document, "result_settlement_target_capacity_reached");
				return;
			}
			AddOrMergeResultSettlementSlot(round, targetId, "route", document.DocumentId, author.StringId, prioritize: false);
		}

		document.IsReadyForPublication = true;
		List<string> allAddressed = NormalizeKingdomIdList(actions.Select(x => x.TargetKingdomId), author.StringId);
		for (int index = 0; index < actions.Count; index++)
		{
			WorldDiplomacyDocumentAction action = actions[index];
			Kingdom target = targets[index];
			MirrorPrimaryActionToDocument(document, action);
			document.ProcessingActionId = action.ActionId ?? "";
			document.AddressedKingdomIds = new List<string> { target.StringId };
			document.ChangedDiplomaticState = false;
			document.MechanicalResult = "";
			bool noAction = string.Equals(NormalizeIntent(action.Intent), "statement", StringComparison.OrdinalIgnoreCase);
			try
			{
				if (!noAction && TryGetDiplomaticStateViolation(action.Intent, author, target, out string executionBlockReason))
				{
					document.MechanicalResult = "外交动作未执行：" + executionBlockReason;
					Log("multi-target diplomatic action became invalid during batch execution document="
						+ document.DocumentId + " action=" + action.ActionId + " reason=" + executionBlockReason);
				}
				else if (!noAction)
				{
					ApplyDocumentPressure(document);
					if (IsImmediateIntent(action.Intent)) ExecuteImmediateIntent(author, target, NormalizeIntent(action.Intent), document);
					ProcessDiplomaticThreatDocument(document, author, target, recordTargetDecisions: false);
					TrySettleRelayOffer(document);
					ApplyDiplomaticPressureEffect(document);
				}
			}
			catch (Exception ex)
			{
				if (string.IsNullOrWhiteSpace(document.MechanicalResult))
				{
					document.MechanicalResult = "外交机制未执行：" + Limit(ex.Message, 180);
				}
				Log("multi-target diplomatic action failed without discarding declaration document=" + document.DocumentId
					+ " action=" + action.ActionId + " intent=" + action.Intent + " error=" + ex.Message);
			}
			action.ChangedDiplomaticState = document.ChangedDiplomaticState;
			action.MechanicalResult = document.MechanicalResult ?? "";
			action.PeaceTerms = document.PeaceTerms;
		}
		RecordDiplomaticThreatTargetDecisionsForActions(document, author);
		bool requiredThreatActionDeferred = DeferUnresolvedRequiredThreatAction(
			document,
			author,
			targets[0],
			actions[0].Intent);
		if (!requiredThreatActionDeferred) SettleDiplomaticThreatFollowThroughAfterDeclaration(document, author);

		document.ProcessingActionId = "";
		document.AddressedKingdomIds = allAddressed;
		MirrorPrimaryActionToDocument(document, actions[0]);
		document.SourceDocumentId = FirstNonEmpty(
			actions[0].RespondingToOfferDocumentId,
			actions[0].RespondingToThreatDocumentId,
			sourceContextDocumentId);
		document.ChangedDiplomaticState = actions.Any(x => x.ChangedDiplomaticState);
		document.MechanicalResult = BuildMultiActionMechanicalResult(actions);
		document.RequiresResponse = actions.Any(x => x.RequiresResponse);
		SettleInternationalReputationForDocument(document);
		try
		{
			AppendCanonicalDocumentEvents(document);
			FinalizeDiplomaticThreatHistoryAfterDocument(document);
			FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document);
		}
		catch (Exception ex)
		{
			ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
			Log("canonical history append deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try { StartDocumentPropagation(document, author); }
		catch (Exception ex) { Log("valid multi-target declaration propagation failed document=" + document.DocumentId + " error=" + ex.Message); }
		try { HandleRoundDocumentProcessed(document); }
		catch (Exception ex) { Log("valid multi-target declaration round progress deferred document=" + document.DocumentId + " error=" + ex.Message); }
	}

	private static string BuildMultiActionMechanicalResult(List<WorldDiplomacyDocumentAction> actions)
	{
		if (actions == null || actions.Count == 0) return "";
		return string.Join("；", actions
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.MechanicalResult))
			.Select(x => FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId) + "：" + x.MechanicalResult));
	}

	private bool TryGetPlayerWorldStateIntentViolation(
		WorldDiplomacyDocument document,
		string intent,
		string commitment,
		Kingdom author,
		Kingdom target,
		out string reason)
	{
		reason = "";
		string normalizedIntent = NormalizeIntent(intent);
		string proposalIntent = ResponseIntentToProposalIntent(normalizedIntent);
		bool isOfferResponse = !string.IsNullOrWhiteSpace(proposalIntent);
		bool isNewProposal = IsProposalIntent(normalizedIntent);
		bool isQualitativeCommitment = normalizedIntent is "ultimatum" or "apology" or "concession"
			|| (normalizedIntent == "warning" && target != null);
		bool hasMechanicalEffect = IsImmediateIntent(normalizedIntent) || isOfferResponse || isNewProposal || isQualitativeCommitment;
		if (!hasMechanicalEffect) return false;
		if (document == null || author == null || target == null || author == target
			|| author.IsEliminated || target.IsEliminated
			|| !HasIndependentWorldDiplomacyAuthority(author)
			|| !HasIndependentWorldDiplomacyAuthority(target))
		{
			reason = "player_action_has_no_eligible_parties";
			return true;
		}
		if (!CommitmentMatchesIntent(normalizedIntent, commitment))
		{
			reason = "player_action_commitment_mismatch";
			return true;
		}
		if (normalizedIntent == "comply_ultimatum" && string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
		{
			reason = "player_compliance_missing_source_threat";
			return true;
		}
		if (normalizedIntent == "comply_ultimatum"
			&& !(document.PresentedThreatDocumentIds ?? new List<string>()).Contains(document.RespondingToThreatDocumentId, StringComparer.OrdinalIgnoreCase))
		{
			reason = "player_compliance_source_not_presented";
			return true;
		}
		if (normalizedIntent == "comply_ultimatum" && !string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			reason = "player_compliance_claims_offer_source";
			return true;
		}
		if (normalizedIntent != "comply_ultimatum" && !string.IsNullOrWhiteSpace(document.RespondingToThreatDocumentId))
		{
			reason = "player_non_compliance_claims_threat_source";
			return true;
		}
		if (!isOfferResponse && !string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			reason = "player_non_response_claims_offer_source";
			return true;
		}
		if (TryGetDiplomaticStateViolation(normalizedIntent, author, target, out reason))
		{
			reason = "player_action_" + reason;
			return true;
		}
		if (TryGetDiplomaticThreatIntentViolation(normalizedIntent, author, target,
			document.RespondingToThreatDocumentId, out reason))
		{
			reason = "player_action_" + reason;
			return true;
		}
		if (!isOfferResponse) return false;
		if (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			reason = "player_offer_response_missing_source_offer";
			return true;
		}
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		bool hasExactOpenOffer = round?.PendingOffers?.Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)) == true;
		if (!hasExactOpenOffer)
		{
			reason = "player_offer_response_without_exact_open_offer";
			return true;
		}
		if (normalizedIntent == "accept_peace")
		{
			WorldDiplomacyDocument source = ResolveDocument(document.RespondingToOfferDocumentId);
			WorldDiplomacyPeaceTerms offeredTerms = ResolveOfferedPeaceTerms(
				source,
				document.RespondingToOfferActionId);
			if (!ArePeaceTermsEquivalent(document.PeaceTerms, offeredTerms))
			{
				reason = "player_accept_peace_changes_offer_terms";
				return true;
			}
		}
		return false;
	}

	private bool TryApplyUltimatumComplianceDomesticPenalty(
		WorldDiplomacyThreat threat,
		Kingdom compliantKingdom,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		int newlyAppliedClanCount = 0;
		if (threat == null
			|| compliantKingdom == null
			|| (!string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
			|| string.IsNullOrWhiteSpace(threat.StageDocumentId)
			|| string.IsNullOrWhiteSpace(threat.ComplianceDocumentId)
			|| !string.Equals(threat.TargetKingdomId, compliantKingdom.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		threat.DomesticPenaltyEligibleClanIds ??= new List<string>();
		threat.DomesticPenaltyAppliedClanIds ??= new List<string>();
		threat.DomesticPenaltySkippedClanIds ??= new List<string>();
		if (threat.DomesticPenaltyCompleted)
		{
			affectedClanCount = threat.DomesticPenaltyAppliedClanIds
				.Count(x => !string.IsNullOrWhiteSpace(x));
			return true;
		}

		if (!threat.DomesticPenaltySnapshotCaptured)
		{
			Clan currentRulingClan = compliantKingdom.RulingClan;
			if (currentRulingClan == null || string.IsNullOrWhiteSpace(currentRulingClan.StringId))
			{
				return false;
			}

			HashSet<string> eligibleClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (compliantKingdom.Clans != null)
			{
				for (int index = 0; index < compliantKingdom.Clans.Count; index++)
				{
					Clan clan = compliantKingdom.Clans[index];
					if (clan == null
						|| clan == currentRulingClan
						|| clan.Kingdom != compliantKingdom
						|| clan.IsEliminated
						|| clan.IsUnderMercenaryService
						|| clan.IsClanTypeMercenary
						|| string.IsNullOrWhiteSpace(clan.StringId))
					{
						continue;
					}
					eligibleClanIds.Add(clan.StringId);
				}
			}

			threat.DomesticPenaltyRulingClanId = currentRulingClan.StringId;
			threat.DomesticPenaltyEligibleClanIds = eligibleClanIds.ToList();
			threat.DomesticPenaltyEligibleClanIds.Sort(StringComparer.OrdinalIgnoreCase);
			threat.DomesticPenaltyAppliedClanIds.Clear();
			threat.DomesticPenaltySkippedClanIds.Clear();
			threat.DomesticPenaltySnapshotCaptured = true;
		}

		string rulingClanId = (threat.DomesticPenaltyRulingClanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(rulingClanId) || Campaign.Current == null)
		{
			return false;
		}

		HashSet<string> eligibleIds = new HashSet<string>(
			threat.DomesticPenaltyEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> appliedIds = new HashSet<string>(
			threat.DomesticPenaltyAppliedClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> skippedIds = new HashSet<string>(
			threat.DomesticPenaltySkippedClanIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		if (eligibleIds.Count == 0)
		{
			threat.DomesticPenaltyAppliedClanIds.Clear();
			threat.DomesticPenaltySkippedClanIds.Clear();
			threat.DomesticPenaltyCompleted = true;
			return true;
		}

		HashSet<string> requiredClanIds = new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase)
		{
			rulingClanId
		};
		Dictionary<string, Clan> clansById = new Dictionary<string, Clan>(requiredClanIds.Count, StringComparer.OrdinalIgnoreCase);
		foreach (Clan clan in Clan.All)
		{
			if (clan != null && !string.IsNullOrWhiteSpace(clan.StringId) && requiredClanIds.Contains(clan.StringId))
			{
				clansById[clan.StringId] = clan;
			}
		}
		if (!clansById.TryGetValue(rulingClanId, out Clan rulingClan) || rulingClan == null || rulingClan.IsEliminated)
		{
			foreach (string unresolvedId in eligibleIds.Where(id => !appliedIds.Contains(id))) skippedIds.Add(unresolvedId);
			threat.DomesticPenaltySkippedClanIds = skippedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
			threat.DomesticPenaltyCompleted = true;
			affectedClanCount = appliedIds.Count;
			return true;
		}
		if (rulingClan.Leader == null)
		{
			return false;
		}

		foreach (string eligibleClanId in eligibleIds)
		{
			if (appliedIds.Contains(eligibleClanId) || skippedIds.Contains(eligibleClanId))
			{
				continue;
			}
			if (!clansById.TryGetValue(eligibleClanId, out Clan vassalClan)
				|| vassalClan == null || vassalClan.IsEliminated)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}
			if (vassalClan.Leader == null) continue;
			if (vassalClan.Leader == rulingClan.Leader)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}

			int expectedRelation = int.MinValue;
			try
			{
				int relationBefore = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
				expectedRelation = MBMath.ClampInt(
					relationBefore + UltimatumComplianceRoyalRelationPenalty,
					-100,
					100);
				if (relationBefore > -100)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
						vassalClan.Leader,
						rulingClan.Leader,
						UltimatumComplianceRoyalRelationPenalty,
						showQuickNotification: false);
				}
				int relationAfter = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
				if (relationAfter != expectedRelation)
				{
					Log("ultimatum compliance domestic penalty deferred threat=" + threat.ThreatId
						+ " clan=" + eligibleClanId
						+ " before=" + relationBefore.ToString(CultureInfo.InvariantCulture)
						+ " after=" + relationAfter.ToString(CultureInfo.InvariantCulture)
						+ " expected=" + expectedRelation.ToString(CultureInfo.InvariantCulture));
					continue;
				}
				appliedIds.Add(eligibleClanId);
				newlyAppliedClanCount++;
			}
			catch (Exception ex)
			{
				bool appliedDespiteException = false;
				if (expectedRelation != int.MinValue)
				{
					try
					{
						int relationAfterException = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
						if (relationAfterException <= expectedRelation)
						{
							appliedIds.Add(eligibleClanId);
							newlyAppliedClanCount++;
							appliedDespiteException = true;
						}
					}
					catch
					{
					}
				}
				Log("ultimatum compliance domestic penalty failed threat=" + threat.ThreatId
					+ " clan=" + eligibleClanId + " applied_despite_exception=" + appliedDespiteException
					+ " error=" + ex.Message);
			}
		}

		threat.DomesticPenaltyAppliedClanIds = appliedIds.ToList();
		threat.DomesticPenaltyAppliedClanIds.Sort(StringComparer.OrdinalIgnoreCase);
		threat.DomesticPenaltySkippedClanIds = skippedIds.ToList();
		threat.DomesticPenaltySkippedClanIds.Sort(StringComparer.OrdinalIgnoreCase);
		threat.DomesticPenaltyCompleted = eligibleIds.All(id => appliedIds.Contains(id) || skippedIds.Contains(id));
		affectedClanCount = appliedIds.Count;
		Log("ultimatum compliance domestic penalty threat=" + threat.ThreatId
			+ " kingdom=" + compliantKingdom.StringId
			+ " ruling_clan=" + rulingClanId
			+ " newly_applied=" + newlyAppliedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " applied=" + appliedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skippedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ "/" + eligibleIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " completed=" + threat.DomesticPenaltyCompleted);
		return threat.DomesticPenaltyCompleted;
	}

	private bool TryApplyDiplomaticThreatPolicyConditionCancellation(WorldDiplomacyThreat threat)
	{
		if (threat == null
			|| !string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)) return false;
		if (threat.PolicyConditionCancellationCompleted) return true;
		if (string.IsNullOrWhiteSpace(threat.PolicyConditionPolicyId)
			|| string.IsNullOrWhiteSpace(threat.PolicyConditionOwnerKingdomId))
		{
			threat.PolicyConditionCancellationCompleted = true;
			threat.PolicyConditionCancellationStatus = "not_bound";
			return true;
		}

		bool completed = CustomPolicyBehavior.TryCancelActiveKingdomPolicyForExternal(
			threat.PolicyConditionPolicyId,
			threat.PolicyConditionOwnerKingdomId,
			"外交威慑退让：" + threat.ThreatId,
			out string policyName,
			out string result);
		if (!completed)
		{
			Log("diplomatic threat policy cancellation deferred threat=" + threat.ThreatId
				+ " policy=" + threat.PolicyConditionPolicyId + " result=" + (result ?? ""));
			return false;
		}
		if (!string.IsNullOrWhiteSpace(policyName)) threat.PolicyConditionPolicyName = Limit(policyName.Trim(), 80);
		threat.PolicyConditionCancellationCompleted = true;
		threat.PolicyConditionCancellationStatus = string.IsNullOrWhiteSpace(result) ? "cancelled" : result.Trim().ToLowerInvariant();
		threat.PolicyConditionCancellationDay = CurrentDay();
		threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.PolicyConditionCancellationDay);
		_storage?.PendingPolicySignals?.RemoveAll(signal => signal != null
			&& string.Equals(signal.PolicyId, threat.PolicyConditionPolicyId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(signal.IssuerKingdomId, threat.PolicyConditionOwnerKingdomId, StringComparison.OrdinalIgnoreCase));
		RemoveSettledPolicySignalContextFromActiveRound(
			threat.PolicyConditionPolicyId,
			threat.PolicyConditionOwnerKingdomId);
		InvalidateOtherThreatsBoundToSettledPolicy(threat);
		Log("diplomatic threat policy cancellation settled threat=" + threat.ThreatId
			+ " policy=" + threat.PolicyConditionPolicyId
			+ " owner=" + threat.PolicyConditionOwnerKingdomId
			+ " result=" + threat.PolicyConditionCancellationStatus);
		return true;
	}

	private void RemoveSettledPolicySignalContextFromActiveRound(string policyId, string ownerKingdomId)
	{
		WorldDiplomacyRound round = _storage?.ActiveRound;
		if (round == null || string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(ownerKingdomId)) return;
		round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		List<WorldDiplomacyPolicySignal> removed = round.AttachedPolicySignals
			.Where(signal => signal != null
				&& string.Equals(signal.PolicyId, policyId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(signal.IssuerKingdomId, ownerKingdomId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (removed.Count == 0) return;

		string openingContext = round.ExternalOpeningContext ?? "";
		foreach (WorldDiplomacyPolicySignal signal in removed)
		{
			string context = BuildPolicySignalContext(signal);
			if (!string.IsNullOrWhiteSpace(context)) openingContext = openingContext.Replace(context, "");
		}
		round.ExternalOpeningContext = openingContext.Trim();
		round.AttachedPolicySignals.RemoveAll(signal => signal != null
			&& string.Equals(signal.PolicyId, policyId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(signal.IssuerKingdomId, ownerKingdomId, StringComparison.OrdinalIgnoreCase));
		round.ExternalSignalKeys ??= new List<string>();
		HashSet<string> removedSignalKeys = new HashSet<string>(
			removed.Select(signal => signal.SignalKey).Where(key => !string.IsNullOrWhiteSpace(key)),
			StringComparer.OrdinalIgnoreCase);
		if (removedSignalKeys.Count > 0)
		{
			round.ExternalSignalKeys.RemoveAll(key => removedSignalKeys.Contains(key));
		}
	}

	private void InvalidateOtherThreatsBoundToSettledPolicy(WorldDiplomacyThreat settledThreat)
	{
		if (settledThreat == null || string.IsNullOrWhiteSpace(settledThreat.PolicyConditionPolicyId)) return;
		int day = CurrentDay();
		foreach (WorldDiplomacyThreat other in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && !ReferenceEquals(x, settledThreat) && IsOpenDiplomaticThreat(x)
				&& string.Equals(x.PolicyConditionPolicyId, settledThreat.PolicyConditionPolicyId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.PolicyConditionOwnerKingdomId, settledThreat.PolicyConditionOwnerKingdomId, StringComparison.OrdinalIgnoreCase)))
		{
			other.Status = "invalidated";
			other.ResolutionReason = "bound_policy_cancelled_by_another_complied_threat";
			other.ResolutionRoundId = "";
			other.ResolutionDocumentId = "";
			other.ObligationRoundId = "";
			other.ObligationClaimedDay = 0;
			other.UpdatedDay = Math.Max(other.UpdatedDay, day);
			other.HistoryResultRecorded = true;
			Log("diplomatic threat invalidated after shared policy cancellation threat=" + other.ThreatId
				+ " policy=" + other.PolicyConditionPolicyId
				+ " settled_by=" + settledThreat.ThreatId);
		}
	}

	private bool TryApplyDiplomaticThreatIssuerRelationReward(
		WorldDiplomacyThreat threat,
		Kingdom issuerKingdom,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		int newlyAppliedClanCount = 0;
		if (threat == null
			|| issuerKingdom == null
			|| !string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
			|| string.IsNullOrWhiteSpace(threat.ComplianceDocumentId)
			|| !string.Equals(threat.IssuerKingdomId, issuerKingdom.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		threat.IssuerRewardEligibleClanIds ??= new List<string>();
		threat.IssuerRewardAppliedClanIds ??= new List<string>();
		threat.IssuerRewardSkippedClanIds ??= new List<string>();
		if (threat.IssuerRewardCompleted)
		{
			affectedClanCount = threat.IssuerRewardAppliedClanIds.Count(x => !string.IsNullOrWhiteSpace(x));
			return true;
		}

		if (!threat.IssuerRewardSnapshotCaptured)
		{
			int rewardAmount = GetThreatComplianceIssuerRelationReward();
			threat.IssuerRewardAmount = rewardAmount;
			if (rewardAmount <= 0)
			{
				threat.IssuerRewardSnapshotCaptured = true;
				threat.IssuerRewardCompleted = true;
				threat.IssuerRewardHistoryRecorded = true;
				return true;
			}
			Clan currentRulingClan = issuerKingdom.RulingClan;
			if (currentRulingClan == null || string.IsNullOrWhiteSpace(currentRulingClan.StringId)) return false;

			HashSet<string> eligibleClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (issuerKingdom.Clans != null)
			{
				for (int index = 0; index < issuerKingdom.Clans.Count; index++)
				{
					Clan clan = issuerKingdom.Clans[index];
					if (clan == null
						|| clan == currentRulingClan
						|| clan.Kingdom != issuerKingdom
						|| clan.IsEliminated
						|| clan.IsUnderMercenaryService
						|| clan.IsClanTypeMercenary
						|| string.IsNullOrWhiteSpace(clan.StringId)) continue;
					eligibleClanIds.Add(clan.StringId);
				}
			}
			threat.IssuerRewardRulingClanId = currentRulingClan.StringId;
			threat.IssuerRewardEligibleClanIds = eligibleClanIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
			threat.IssuerRewardAppliedClanIds.Clear();
			threat.IssuerRewardSkippedClanIds.Clear();
			threat.IssuerRewardSnapshotCaptured = true;
		}

		string rulingClanId = (threat.IssuerRewardRulingClanId ?? "").Trim();
		int amount = Math.Max(0, Math.Min(DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax, threat.IssuerRewardAmount));
		if (amount <= 0)
		{
			threat.IssuerRewardCompleted = true;
			threat.IssuerRewardHistoryRecorded = true;
			return true;
		}
		if (rulingClanId.Length == 0 || Campaign.Current == null) return false;

		HashSet<string> eligibleIds = new HashSet<string>(threat.IssuerRewardEligibleClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> appliedIds = new HashSet<string>(threat.IssuerRewardAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skippedIds = new HashSet<string>(threat.IssuerRewardSkippedClanIds, StringComparer.OrdinalIgnoreCase);
		if (eligibleIds.Count == 0)
		{
			threat.IssuerRewardCompleted = true;
			threat.IssuerRewardHistoryRecorded = true;
			return true;
		}

		HashSet<string> requiredClanIds = new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase) { rulingClanId };
		Dictionary<string, Clan> clansById = new Dictionary<string, Clan>(requiredClanIds.Count, StringComparer.OrdinalIgnoreCase);
		foreach (Clan clan in Clan.All)
		{
			if (clan != null && !string.IsNullOrWhiteSpace(clan.StringId) && requiredClanIds.Contains(clan.StringId))
			{
				clansById[clan.StringId] = clan;
			}
		}
		if (!clansById.TryGetValue(rulingClanId, out Clan rulingClan) || rulingClan == null || rulingClan.IsEliminated)
		{
			foreach (string unresolvedId in eligibleIds.Where(id => !appliedIds.Contains(id))) skippedIds.Add(unresolvedId);
			threat.IssuerRewardSkippedClanIds = skippedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
			threat.IssuerRewardCompleted = true;
			affectedClanCount = appliedIds.Count;
			return true;
		}
		if (rulingClan.Leader == null) return false;

		foreach (string eligibleClanId in eligibleIds)
		{
			if (appliedIds.Contains(eligibleClanId) || skippedIds.Contains(eligibleClanId)) continue;
			if (!clansById.TryGetValue(eligibleClanId, out Clan vassalClan)
				|| vassalClan == null || vassalClan.IsEliminated)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}
			if (vassalClan.Leader == null) continue;
			if (vassalClan.Leader == rulingClan.Leader)
			{
				skippedIds.Add(eligibleClanId);
				continue;
			}

			int expectedRelation = int.MinValue;
			try
			{
				int relationBefore = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
				expectedRelation = MBMath.ClampInt(relationBefore + amount, -100, 100);
				if (relationBefore < 100)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
						vassalClan.Leader,
						rulingClan.Leader,
						amount,
						showQuickNotification: false);
				}
				int relationAfter = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
				if (relationAfter != expectedRelation)
				{
					Log("diplomatic threat issuer relation reward deferred threat=" + threat.ThreatId
						+ " clan=" + eligibleClanId
						+ " before=" + relationBefore.ToString(CultureInfo.InvariantCulture)
						+ " after=" + relationAfter.ToString(CultureInfo.InvariantCulture)
						+ " expected=" + expectedRelation.ToString(CultureInfo.InvariantCulture));
					continue;
				}
				appliedIds.Add(eligibleClanId);
				newlyAppliedClanCount++;
			}
			catch (Exception ex)
			{
				bool appliedDespiteException = false;
				if (expectedRelation != int.MinValue)
				{
					try
					{
						int relationAfterException = CharacterRelationManager.GetHeroRelation(vassalClan.Leader, rulingClan.Leader);
						if (relationAfterException >= expectedRelation)
						{
							appliedIds.Add(eligibleClanId);
							newlyAppliedClanCount++;
							appliedDespiteException = true;
						}
					}
					catch
					{
					}
				}
				Log("diplomatic threat issuer relation reward failed threat=" + threat.ThreatId
					+ " clan=" + eligibleClanId + " applied_despite_exception=" + appliedDespiteException
					+ " error=" + ex.Message);
			}
		}

		threat.IssuerRewardAppliedClanIds = appliedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		threat.IssuerRewardSkippedClanIds = skippedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		threat.IssuerRewardCompleted = eligibleIds.All(id => appliedIds.Contains(id) || skippedIds.Contains(id));
		affectedClanCount = appliedIds.Count;
		Log("diplomatic threat issuer relation reward threat=" + threat.ThreatId
			+ " kingdom=" + issuerKingdom.StringId
			+ " ruling_clan=" + rulingClanId
			+ " amount=" + amount.ToString(CultureInfo.InvariantCulture)
			+ " newly_applied=" + newlyAppliedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " applied=" + appliedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skippedIds.Count.ToString(CultureInfo.InvariantCulture)
			+ "/" + eligibleIds.Count.ToString(CultureInfo.InvariantCulture)
			+ " completed=" + threat.IssuerRewardCompleted);
		return threat.IssuerRewardCompleted;
	}

	private void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument document)
	{
		if (document == null || !string.IsNullOrWhiteSpace(document.MechanicalResult)) return;
		string intent = NormalizeIntent(document.Intent);
		if (intent != "apology" && intent != "concession") return;
		Kingdom author = ResolveKingdom(document.AuthorKingdomId);
		Kingdom target = ResolveKingdom(document.TargetKingdomId);
		if (author == null || target == null || author == target) return;
		int reduction = intent == "concession" ? -22 : -16;
		AddWarPressure(author.StringId, target.StringId, reduction, "正式" + (intent == "concession" ? "让步" : "道歉") + "：" + document.Title, intent);
		AddWarPressure(target.StringId, author.StringId, reduction / 2, "对方作出正式" + (intent == "concession" ? "让步" : "道歉"), intent);
	}

	private static void CaptureDiplomaticThreatNonComplianceEvent(WorldDiplomacyThreat threat)
	{
		if (threat == null
			|| !string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
			|| string.IsNullOrWhiteSpace(threat.StageDocumentId)
			|| string.IsNullOrWhiteSpace(threat.TargetDecisionDocumentId)) return;
		threat.NonComplianceEvents ??= new List<WorldDiplomacyThreatNonComplianceEvent>();
		WorldDiplomacyThreatNonComplianceEvent decision = threat.NonComplianceEvents.FirstOrDefault(x => x != null
			&& string.Equals(x.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase));
		if (decision == null)
		{
			decision = new WorldDiplomacyThreatNonComplianceEvent();
			threat.NonComplianceEvents.Add(decision);
		}
		decision.Stage = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? "ultimatum" : "warning";
		decision.StageDocumentId = threat.StageDocumentId ?? "";
		decision.StageActionId = threat.StageActionId ?? "";
		decision.DecisionDocumentId = threat.TargetDecisionDocumentId ?? "";
		decision.DecisionActionId = threat.TargetDecisionActionId ?? "";
		decision.DecisionRoundId = threat.TargetDecisionRoundId ?? "";
		decision.DecisionDay = Math.Max(0, threat.TargetDecisionDay);
		if (threat.NonComplianceHistoryRecorded) decision.HistoryRecorded = true;
	}

	private void RecordDiplomaticThreatTargetDecisions(
		WorldDiplomacyDocument document,
		Kingdom author,
		Kingdom selectedIssuer,
		string intent)
	{
		if (document == null || author == null) return;
		HashSet<string> presented = new HashSet<string>(
			document.PresentedThreatDocumentIds ?? new List<string>(),
			StringComparer.OrdinalIgnoreCase);
		if (presented.Count == 0) return;
		string normalizedIntent = NormalizeIntent(intent);
		string selectedSourceId = normalizedIntent == "comply_ultimatum"
			? (document.RespondingToThreatDocumentId ?? "")
			: "";
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& presented.Contains(x.StageDocumentId))
			.ToList())
		{
			bool targetsIssuer = selectedIssuer != null
				&& string.Equals(threat.IssuerKingdomId, selectedIssuer.StringId, StringComparison.OrdinalIgnoreCase);
			WorldDiplomacyThreatStateRuleResult decision = WorldDiplomacyThreatStateRules.EvaluateTargetDeclaration(
				threat.TargetDecision,
				threat.StageDocumentId,
				currentStageWasPresented: true,
				normalizedIntent,
				selectedSourceId,
				targetsIssuer);
			if (decision != WorldDiplomacyThreatStateRuleResult.MarkTargetNoncomplied) continue;
			threat.TargetDecision = "noncomplied";
			threat.TargetDecisionDocumentId = document.DocumentId ?? "";
			threat.TargetDecisionRoundId = document.RoundId ?? "";
			threat.TargetDecisionDay = CurrentDay();
			threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
			threat.UpdatedDay = CurrentDay();
			CaptureDiplomaticThreatNonComplianceEvent(threat);
			Log("diplomatic threat target noncompliance confirmed threat=" + threat.ThreatId
				+ " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
				+ " document=" + document.DocumentId + " intent=" + normalizedIntent);
		}
	}

	private void RecordDiplomaticThreatTargetDecisionsForActions(
		WorldDiplomacyDocument document,
		Kingdom author)
	{
		if (document == null || author == null || document.Actions == null) return;
		HashSet<string> presented = new HashSet<string>(
			document.PresentedThreatDocumentIds ?? new List<string>(),
			StringComparer.OrdinalIgnoreCase);
		if (presented.Count == 0) return;
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& presented.Contains(x.StageDocumentId))
			.ToList())
		{
			WorldDiplomacyDocumentAction compliance = document.Actions.FirstOrDefault(x => x != null
				&& string.Equals(NormalizeIntent(x.Intent), "comply_ultimatum", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.RespondingToThreatDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(threat.StageActionId)
					? string.IsNullOrWhiteSpace(x.RespondingToThreatActionId)
					: string.Equals(x.RespondingToThreatActionId, threat.StageActionId, StringComparison.OrdinalIgnoreCase)));
			if (compliance != null) continue;
			WorldDiplomacyDocumentAction decisionAction = document.Actions.FirstOrDefault(x => x != null
				&& string.Equals(x.TargetKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase));
			threat.TargetDecision = "noncomplied";
			threat.TargetDecisionDocumentId = document.DocumentId ?? "";
			threat.TargetDecisionActionId = decisionAction?.ActionId ?? "";
			threat.TargetDecisionRoundId = document.RoundId ?? "";
			threat.TargetDecisionDay = CurrentDay();
			threat.ResolutionReason = "target_did_not_comply_in_first_declaration";
			threat.UpdatedDay = CurrentDay();
			CaptureDiplomaticThreatNonComplianceEvent(threat);
			Log("diplomatic threat target noncompliance confirmed threat=" + threat.ThreatId
				+ " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
				+ " document=" + document.DocumentId + " actions=" + document.Actions.Count.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void ProcessDiplomaticThreatDocument(
		WorldDiplomacyDocument document,
		Kingdom author,
		Kingdom target,
		bool recordTargetDecisions = true)
	{
		if (document == null || author == null) return;
		string intent = NormalizeIntent(document.Intent);
		if (recordTargetDecisions) RecordDiplomaticThreatTargetDecisions(document, author, target, intent);
		if (target == null || author == target) return;
		if (intent == "warning" || intent == "ultimatum")
		{
			RegisterOrAdvanceDiplomaticThreat(document, author, target, intent);
			return;
		}
		if (intent == "comply_ultimatum")
		{
			ResolveDiplomaticThreatCompliance(document, author, target);
			return;
		}
		if (intent != "declare_war" || !document.ChangedDiplomaticState) return;

		WorldDiplomacyThreat enforced = (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null
				&& string.Equals(x.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
				&& IsOpenDiplomaticThreat(x))
			.OrderByDescending(x => x.UpdatedDay).ThenByDescending(x => x.CreatedDay).FirstOrDefault();
		if (enforced != null)
		{
			ApplyNationalPrestigeDelta(author.StringId, UltimatumWarPrestigeReward, document,
				"在对方拒绝最后通牒后兑现宣战承诺");
			enforced.Status = "enforced";
			enforced.ResolutionRoundId = document.RoundId ?? "";
			enforced.ResolutionDocumentId = document.DocumentId ?? "";
			enforced.ResolutionActionId = document.ProcessingActionId ?? "";
			enforced.ResolutionReason = "issuer_declared_war";
			enforced.UpdatedDay = CurrentDay();
			enforced.ObligationRoundId = "";
			enforced.ObligationClaimedDay = 0;
		}
		foreach (WorldDiplomacyThreat other in (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Where(x => IsOpenDiplomaticThreat(x)
			&& !ReferenceEquals(x, enforced)
			&& ((string.Equals(x.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(x.IssuerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)))))
		{
			if (string.Equals(other.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(other.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(other.Stage, "warning", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(other.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
			{
				// A rejected warning strictly requires an ultimatum in the issuer's next
				// declaration. Starting the war directly does not erase that broken promise;
				// leave this threat open for the declaration-level reputation settlement below.
				continue;
			}
			InvalidateDiplomaticThreatForNormalization(other, "war_started_by_other_direction", CurrentDay());
		}
	}

	private bool DeferUnresolvedRequiredThreatAction(
		WorldDiplomacyDocument document,
		Kingdom author,
		Kingdom target,
		string intent)
	{
		if (document == null || author == null) return false;
		WorldDiplomacyThreat threat = FindOpenDiplomaticThreatIssuedBy(author.StringId);
		if (threat == null
			|| !string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
			|| !(document.PresentedThreatFollowThroughDocumentIds ?? new List<string>()).Contains(threat.StageDocumentId, StringComparer.OrdinalIgnoreCase)) return false;
		WorldDiplomacyDocumentAction matchingAction = document.Actions?.FirstOrDefault(x => x != null
			&& string.Equals(x.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
		if (document.Actions?.Count > 0 && matchingAction == null) return false;
		if (matchingAction == null && (target == null || author == target
			|| !string.Equals(threat.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))) return false;
		string normalizedIntent = NormalizeIntent(matchingAction?.Intent ?? intent);
		bool changedState = matchingAction?.ChangedDiplomaticState ?? document.ChangedDiplomaticState;
		WorldDiplomacyThreatStateRuleResult result = WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
			threat.TargetDecision,
			threat.Stage,
			threat.StageDocumentId,
			currentStageWasPresented: true,
			normalizedIntent,
			declarationTargetsThreatTarget: true,
			warActionMechanicallySucceeded: changedState);
		if (result != WorldDiplomacyThreatStateRuleResult.MarkFollowThroughSatisfied
			&& result != WorldDiplomacyThreatStateRuleResult.DeferFollowThroughForTechnicalFailure) return false;
		threat.ResolutionReason = "required_action_mechanical_retry";
		threat.UpdatedDay = CurrentDay();
		Log("diplomatic threat next-declaration obligation deferred after unresolved required action threat=" + threat.ThreatId
			+ " stage=" + threat.Stage + " document=" + document.DocumentId + " intent=" + normalizedIntent);
		return true;
	}

	private void SettleDiplomaticThreatFollowThroughAfterDeclaration(WorldDiplomacyDocument document, Kingdom author)
	{
		if (document == null || author == null) return;
		WorldDiplomacyThreat threat = FindOpenDiplomaticThreatIssuedBy(author.StringId);
		if (threat == null
			|| !string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
			|| !(document.PresentedThreatFollowThroughDocumentIds ?? new List<string>()).Contains(threat.StageDocumentId, StringComparer.OrdinalIgnoreCase)) return;
		WorldDiplomacyDocumentAction matchingAction = document.Actions?.FirstOrDefault(x => x != null
			&& string.Equals(x.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
		bool targetsThreatTarget = matchingAction != null
			|| string.Equals(threat.TargetKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
		WorldDiplomacyThreatStateRuleResult result = WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
			threat.TargetDecision,
			threat.Stage,
			threat.StageDocumentId,
			currentStageWasPresented: true,
			NormalizeIntent(matchingAction?.Intent ?? document.Intent),
			targetsThreatTarget,
			matchingAction?.ChangedDiplomaticState ?? document.ChangedDiplomaticState);
		if (result == WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached)
		{
			ApplyDiplomaticThreatReputationPenalty(threat, document);
		}
	}

	private void LogDiplomaticThreatFallbackAnalysisPublished(WorldDiplomacyJob job)
	{
		if (job == null) return;
		WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
		if (document?.IsReadyForPublication != true) return;
		Log("diplomatic threat declaration settled from fallback analysis after service failure author="
			+ (job.AuthorKingdomId ?? "") + " round=" + FirstNonEmpty(job.RoundId, job.ExchangeId)
			+ " document=" + (job.DocumentId ?? ""));
	}

	private bool TryResolvePolicyConditionForThreat(
		WorldDiplomacyDocument document,
		Kingdom threatIssuer,
		Kingdom threatTarget,
		out WorldDiplomacyPolicySignal selected)
	{
		selected = null;
		if (document == null || threatIssuer == null || threatTarget == null || threatIssuer == threatTarget)
		{
			return false;
		}
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		List<WorldDiplomacyPolicySignal> matches = new List<WorldDiplomacyPolicySignal>();
		foreach (WorldDiplomacyPolicySignal signal in round?.AttachedPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
		{
			if (signal == null
				|| string.IsNullOrWhiteSpace(signal.PolicyId)
				|| (!string.IsNullOrWhiteSpace(signal.PolicyKind)
					&& !string.Equals(signal.PolicyKind.Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
				|| string.IsNullOrWhiteSpace(signal.IssuerKingdomId)
				|| string.IsNullOrWhiteSpace(signal.TargetKingdomId)
				|| !WorldDiplomacyPolicyContext.IsForeignPolicySignalActive(
					signal.PolicyId,
					signal.IssuerKingdomId,
					signal.TargetKingdomId))
			{
				continue;
			}
			Kingdom policyOwner = ResolveKingdom(signal.IssuerKingdomId);
			Kingdom affectedKingdom = ResolveKingdom(signal.TargetKingdomId);
			Kingdom policyOwnerRepresentative = ResolveWorldDiplomacyRepresentative(policyOwner);
			Kingdom affectedRepresentative = ResolveWorldDiplomacyRepresentative(affectedKingdom);
			if (policyOwnerRepresentative == null || affectedRepresentative == null
				|| !string.Equals(policyOwnerRepresentative.StringId, threatTarget.StringId, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(affectedRepresentative.StringId, threatIssuer.StringId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			matches.Add(signal);
		}
		matches = matches
			.GroupBy(x => (x.PolicyId ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(x => x.PublishedDay).First())
			.Take(2)
			.ToList();
		if (matches.Count != 1) return false;
		selected = matches[0];
		return true;
	}

	private static void InitializeDiplomaticThreatPolicyCondition(
		WorldDiplomacyThreat threat,
		WorldDiplomacyPolicySignal signal,
		int day)
	{
		if (threat == null) return;
		if (signal == null)
		{
			threat.PolicyConditionCancellationCompleted = true;
			threat.PolicyConditionCancellationStatus = "not_bound";
			return;
		}
		threat.PolicyConditionSignalKey = (signal.SignalKey ?? "").Trim();
		threat.PolicyConditionPolicyId = (signal.PolicyId ?? "").Trim();
		threat.PolicyConditionPolicyName = Limit((signal.PolicyName ?? "").Trim(), 80);
		threat.PolicyConditionOwnerKingdomId = (signal.IssuerKingdomId ?? "").Trim();
		threat.PolicyConditionAffectedKingdomId = (signal.TargetKingdomId ?? "").Trim();
		threat.PolicyConditionBoundDay = Math.Max(0, day);
		threat.PolicyConditionCancellationCompleted = false;
		threat.PolicyConditionCancellationStatus = "pending";
	}

	private bool RegisterOrAdvanceDiplomaticThreat(
		WorldDiplomacyDocument document,
		Kingdom issuer,
		Kingdom target,
		string stage)
	{
		if (document == null || issuer == null || target == null || issuer == target) return false;
		string normalizedStage = NormalizeIntent(stage);
		if (normalizedStage is not "warning" and not "ultimatum"
			|| !string.Equals(NormalizeIntent(document.Intent), normalizedStage, StringComparison.Ordinal)
			|| !string.Equals(document.AuthorKingdomId, issuer.StringId, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(document.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
		{
			Log("diplomatic threat registration blocked by structured identity mismatch document="
				+ document.DocumentId + " stage=" + normalizedStage);
			return false;
		}
		_storage.DiplomaticThreats ??= new List<WorldDiplomacyThreat>();
		WorldDiplomacyThreat existing = FindOpenDiplomaticThreatIssuedBy(issuer.StringId);
		int day = CurrentDay();
		if (existing != null && string.Equals(existing.StageDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(stage, "warning", StringComparison.OrdinalIgnoreCase))
		{
			if (existing != null) return false;
			TryResolvePolicyConditionForThreat(document, issuer, target, out WorldDiplomacyPolicySignal policyCondition);
			WorldDiplomacyThreat warning = new WorldDiplomacyThreat
			{
				ThreatId = NewId("diplomacy_threat"),
				IssuerKingdomId = issuer.StringId ?? "",
				TargetKingdomId = target.StringId ?? "",
				Stage = "warning",
				Status = "open",
				TargetDecision = "pending",
				WarningDocumentId = document.DocumentId ?? "",
				WarningActionId = document.ProcessingActionId ?? "",
				StageDocumentId = document.DocumentId ?? "",
				StageActionId = document.ProcessingActionId ?? "",
				StageRoundId = document.RoundId ?? "",
				CreatedDay = day,
				StageIssuedDay = day,
				UpdatedDay = day
			};
			InitializeDiplomaticThreatPolicyCondition(warning, policyCondition, day);
			_storage.DiplomaticThreats.Add(warning);
			Log("diplomatic warning obligation opened issuer=" + issuer.StringId
				+ " target=" + target.StringId + " document=" + document.DocumentId
				+ " policy=" + warning.PolicyConditionPolicyId);
			return true;
		}

		if (!string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)) return false;
		if (existing != null)
		{
			if (!string.Equals(existing.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(existing.Stage, "warning", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(existing.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)) return false;
			existing.Stage = "ultimatum";
			existing.UltimatumDocumentId = document.DocumentId ?? "";
			existing.UltimatumActionId = document.ProcessingActionId ?? "";
			existing.StageDocumentId = document.DocumentId ?? "";
			existing.StageActionId = document.ProcessingActionId ?? "";
			existing.StageRoundId = document.RoundId ?? "";
			existing.StageIssuedDay = day;
			existing.UpdatedDay = day;
			existing.TargetDecision = "pending";
			existing.TargetDecisionDocumentId = "";
			existing.TargetDecisionActionId = "";
			existing.TargetDecisionRoundId = "";
			existing.TargetDecisionDay = 0;
			existing.NonComplianceHistoryRecorded = false;
			existing.ObligationRoundId = "";
			existing.ObligationClaimedDay = 0;
			existing.ResolutionReason = "";
			ApplyNationalPrestigeDelta(issuer.StringId, WarningEscalationPrestigeReward, document,
				"在谴责遭拒后按承诺升级为最后通牒");
			Log("diplomatic warning escalated to ultimatum threat=" + existing.ThreatId
				+ " issuer=" + issuer.StringId + " target=" + target.StringId);
			return true;
		}

		TryResolvePolicyConditionForThreat(document, issuer, target, out WorldDiplomacyPolicySignal directPolicyCondition);
		WorldDiplomacyThreat ultimatum = new WorldDiplomacyThreat
		{
			ThreatId = NewId("diplomacy_threat"),
			IssuerKingdomId = issuer.StringId ?? "",
			TargetKingdomId = target.StringId ?? "",
			Stage = "ultimatum",
			Status = "open",
			TargetDecision = "pending",
			UltimatumDocumentId = document.DocumentId ?? "",
			UltimatumActionId = document.ProcessingActionId ?? "",
			StageDocumentId = document.DocumentId ?? "",
			StageActionId = document.ProcessingActionId ?? "",
			StageRoundId = document.RoundId ?? "",
			CreatedDay = day,
			StageIssuedDay = day,
			UpdatedDay = day
		};
		InitializeDiplomaticThreatPolicyCondition(ultimatum, directPolicyCondition, day);
		_storage.DiplomaticThreats.Add(ultimatum);
		Log("direct diplomatic ultimatum obligation opened issuer=" + issuer.StringId
			+ " target=" + target.StringId + " document=" + document.DocumentId
			+ " policy=" + ultimatum.PolicyConditionPolicyId);
		return true;
	}

	private bool ResolveDiplomaticThreatCompliance(WorldDiplomacyDocument document, Kingdom compliantKingdom, Kingdom issuer)
	{
		if (document == null || compliantKingdom == null || issuer == null) return false;
		WorldDiplomacyThreat threat = FindOpenDiplomaticThreat(issuer.StringId, compliantKingdom.StringId)
			?? (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => x != null
				&& string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.IssuerKingdomId, issuer.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, compliantKingdom.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ComplianceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(document.ProcessingActionId)
					? string.IsNullOrWhiteSpace(x.ComplianceActionId)
					: string.Equals(x.ComplianceActionId, document.ProcessingActionId, StringComparison.OrdinalIgnoreCase)));
		if (threat != null
			&& string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(threat.ComplianceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
		{
			document.ChangedDiplomaticState = true;
			document.MechanicalResult = "已明确服从" + (threat.Stage == "warning" ? "谴责" : "最后通牒");
			return true;
		}
		if (threat == null
			|| !string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(threat.StageDocumentId, document.RespondingToThreatDocumentId, StringComparison.OrdinalIgnoreCase)
			|| (string.IsNullOrWhiteSpace(threat.StageActionId)
				? !string.IsNullOrWhiteSpace(document.RespondingToThreatActionId)
				: !string.Equals(threat.StageActionId, document.RespondingToThreatActionId, StringComparison.OrdinalIgnoreCase))) return false;

		threat.Status = "complied";
		threat.TargetDecision = "complied";
		threat.TargetDecisionDocumentId = document.DocumentId ?? "";
		threat.TargetDecisionActionId = document.ProcessingActionId ?? "";
		threat.TargetDecisionRoundId = document.RoundId ?? "";
		threat.TargetDecisionDay = CurrentDay();
		threat.ComplianceDocumentId = document.DocumentId ?? "";
		threat.ComplianceActionId = document.ProcessingActionId ?? "";
		threat.ResolutionRoundId = document.RoundId ?? "";
		threat.ResolutionDocumentId = document.DocumentId ?? "";
		threat.ResolutionActionId = document.ProcessingActionId ?? "";
		threat.ResolutionReason = "target_explicitly_complied";
		threat.UpdatedDay = CurrentDay();
		threat.ObligationRoundId = "";
		threat.ObligationClaimedDay = 0;
		threat.IssuerResolutionNoticePending = true;
		int prestigeChange = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
			? UltimatumCompliancePrestigeChange
			: WarningCompliancePrestigeChange;
		ApplyNationalPrestigeDelta(issuer.StringId, prestigeChange, document,
			"迫使" + KingdomName(compliantKingdom) + "服从" + (threat.Stage == "warning" ? "外交谴责" : "最后通牒"));
		ApplyNationalPrestigeDelta(compliantKingdom.StringId, -prestigeChange, document,
			"在压力下服从" + KingdomName(issuer) + "的" + (threat.Stage == "warning" ? "外交谴责" : "最后通牒"));
		bool domesticPenaltyCompleted = TryApplyUltimatumComplianceDomesticPenalty(threat, compliantKingdom, out int affectedClanCount);
		bool policyCancellationCompleted = TryApplyDiplomaticThreatPolicyConditionCancellation(threat);
		bool issuerRewardCompleted = TryApplyDiplomaticThreatIssuerRelationReward(threat, issuer, out int rewardedClanCount);
		document.ChangedDiplomaticState = true;
		document.MechanicalResult = "已明确服从" + (threat.Stage == "warning" ? "谴责" : "最后通牒")
			+ (string.Equals(threat.PolicyConditionCancellationStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
				? "；附带政策《" + FirstNonEmpty(threat.PolicyConditionPolicyName, threat.PolicyConditionPolicyId) + "》已取消"
				: "");
		Log("diplomatic threat complied threat=" + threat.ThreatId
			+ " issuer=" + issuer.StringId + " target=" + compliantKingdom.StringId
			+ " domestic_penalty_completed=" + domesticPenaltyCompleted
			+ " domestic_penalty_applied_clans=" + affectedClanCount.ToString(CultureInfo.InvariantCulture)
			+ " policy_cancellation_completed=" + policyCancellationCompleted
			+ " policy=" + threat.PolicyConditionPolicyId
			+ " issuer_reward_completed=" + issuerRewardCompleted
			+ " issuer_reward_applied_clans=" + rewardedClanCount.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private void ResolveDiplomaticThreatsAfterWarStarted(Kingdom first, Kingdom second)
	{
		if (first == null || second == null || first == second || _storage?.DiplomaticThreats == null) return;
		int day = CurrentDay();
		foreach (WorldDiplomacyThreat threat in _storage.DiplomaticThreats.Where(x => IsOpenDiplomaticThreat(x)
			&& ((string.Equals(x.IssuerKingdomId, first.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, second.StringId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(x.IssuerKingdomId, second.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, first.StringId, StringComparison.OrdinalIgnoreCase)))))
		{
			threat.UpdatedDay = day;
			if (_internalDiplomaticActionDepth > 0)
			{
				// ExecuteImmediateIntent still knows the structured author. It will mark the
				// matching threat enforced after the game action returns.
				threat.ResolutionReason = "war_started_during_structured_declaration_pending";
				continue;
			}
			threat.Status = "invalidated";
			threat.ResolutionReason = "war_started_outside_structured_threat_follow_through";
			threat.ObligationRoundId = "";
			threat.ObligationClaimedDay = 0;
			threat.HistoryResultRecorded = true;
		}
	}

	private void SettleDiplomaticThreatObligationsForClosedRound(
		WorldDiplomacyRound round,
		List<WorldDiplomacyDocument> documents)
	{
		if (round == null || string.IsNullOrWhiteSpace(round.RoundId)) return;
		List<WorldDiplomacyDocument> published = documents ?? new List<WorldDiplomacyDocument>();
		bool retryableAbort = string.Equals(round.RoundStatus, "aborted", StringComparison.OrdinalIgnoreCase)
			|| published.All(x => x == null || !string.Equals(x.AuthorKingdomId, round.InitiatorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (!retryableAbort)
		{
			foreach (WorldDiplomacyThreat notice in (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
				.Where(x => x != null && x.IssuerResolutionNoticePending
					&& string.Equals(x.IssuerKingdomId, round.InitiatorKingdomId, StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(x.ResolutionRoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
			{
				notice.IssuerResolutionNoticePending = false;
				notice.UpdatedDay = CurrentDay();
			}
		}
	}

	private void ApplyDiplomaticThreatReputationPenalty(
		WorldDiplomacyThreat threat,
		WorldDiplomacyDocument document)
	{
		if (threat == null || threat.ReputationPenaltyApplied) return;
		int penalty = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
			? UltimatumFollowThroughPrestigePenalty
			: WarningFollowThroughPrestigePenalty;
		int before = GetNationalPrestige(threat.IssuerKingdomId);
		string prestigeReason = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
			? "最后通牒遭拒后没有在下一篇宣言中宣战"
			: "外交谴责遭拒后没有在下一篇宣言中升级最后通牒";
		int after = ApplyNationalPrestigeDelta(threat.IssuerKingdomId, -penalty, document, prestigeReason);
		if (before == 0)
		{
			ApplyZeroPrestigeBreachRelationPenalty(
				ResolveKingdomIncludingEliminated(threat.IssuerKingdomId),
				string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
					? ZeroPrestigeUltimatumBreachRelationPenalty
					: ZeroPrestigeWarningBreachRelationPenalty);
		}
		threat.Status = "breached";
		threat.ReputationPenaltyApplied = true;
		threat.ReputationPenaltyAmount = Math.Max(0, before - after);
		threat.ResolutionRoundId = document?.RoundId ?? "";
		threat.ResolutionDocumentId = document?.DocumentId ?? "";
		threat.ResolutionReason = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
			? "ultimatum_not_followed_by_war_in_next_declaration"
			: "warning_not_followed_by_ultimatum_in_next_declaration";
		threat.UpdatedDay = CurrentDay();
		threat.ObligationRoundId = "";
		threat.ObligationClaimedDay = 0;
		Log("national prestige penalty threat=" + threat.ThreatId
			+ " issuer=" + threat.IssuerKingdomId + " target=" + threat.TargetKingdomId
			+ " stage=" + threat.Stage + " penalty=" + threat.ReputationPenaltyAmount.ToString(CultureInfo.InvariantCulture)
			+ " prestige=" + after.ToString(CultureInfo.InvariantCulture));
	}

	private void RetryDiplomaticThreatDomesticPenalties()
	{
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null
				&& (string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(x.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
				&& !x.DomesticPenaltyCompleted)
			.OrderBy(x => x.UpdatedDay).ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase).Take(8))
		{
			threat.UpdatedDay = CurrentDay();
			Kingdom compliantKingdom = ResolveKingdomIncludingEliminated(threat.TargetKingdomId);
			bool cannotCaptureEliminatedKingdomSnapshot = compliantKingdom?.IsEliminated == true
				&& !threat.DomesticPenaltySnapshotCaptured && compliantKingdom.RulingClan == null;
			bool completed = compliantKingdom != null && !cannotCaptureEliminatedKingdomSnapshot
				? TryApplyUltimatumComplianceDomesticPenalty(threat, compliantKingdom, out int affectedClanCount)
				: CompleteUnresolvableDiplomaticThreatDomesticPenalty(threat, out affectedClanCount);
			if (!completed) continue;
			WorldDiplomacyDocument document = ResolveDocument(threat.ComplianceDocumentId);
			if (document != null)
			{
				UpdateDiplomaticThreatComplianceDocumentResult(threat);
				try
				{
					AppendCanonicalDocumentEvents(document);
					FinalizeDiplomaticThreatHistoryAfterDocument(document);
				}
				catch (Exception ex)
				{
					ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
					Log("compliance history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
				}
			}
			TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
		}
	}

	private void RetryDiplomaticThreatComplianceConsequences()
	{
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& !x.PolicyConditionCancellationCompleted)
			.OrderBy(x => x.UpdatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.Take(8))
		{
			threat.UpdatedDay = CurrentDay();
			TryApplyDiplomaticThreatPolicyConditionCancellation(threat);
			UpdateDiplomaticThreatComplianceDocumentResult(threat);
		}

		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& !x.IssuerRewardCompleted)
			.OrderBy(x => x.UpdatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.Take(8))
		{
			threat.UpdatedDay = CurrentDay();
			Kingdom issuer = ResolveKingdomIncludingEliminated(threat.IssuerKingdomId);
			bool cannotCaptureEliminatedKingdomSnapshot = issuer?.IsEliminated == true
				&& !threat.IssuerRewardSnapshotCaptured && issuer.RulingClan == null;
			bool completed = issuer != null && !cannotCaptureEliminatedKingdomSnapshot
				? TryApplyDiplomaticThreatIssuerRelationReward(threat, issuer, out int affectedClanCount)
				: CompleteUnresolvableDiplomaticThreatIssuerRelationReward(threat, out affectedClanCount);
			if (!completed) continue;
			TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
		}
	}

	private void UpdateDiplomaticThreatComplianceDocumentResult(WorldDiplomacyThreat threat)
	{
		if (threat == null) return;
		WorldDiplomacyDocument document = ResolveDocument(threat.ComplianceDocumentId);
		if (document == null) return;
		document.ChangedDiplomaticState = true;
		document.MechanicalResult = "已明确服从" + (threat.Stage == "warning" ? "谴责" : "最后通牒")
			+ (string.Equals(threat.PolicyConditionCancellationStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
				? "；附带政策《" + FirstNonEmpty(threat.PolicyConditionPolicyName, threat.PolicyConditionPolicyId) + "》已取消"
				: "");
	}

	private void FinalizeDiplomaticThreatHistoryAfterDocument(WorldDiplomacyDocument document)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Where(x => x != null
			&& (string.Equals(x.ComplianceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.ResolutionDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))))
		{
			if (string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase)
				&& document.HistoryDeclarationRecorded
				&& !threat.HistoryResultRecorded)
			{
				TryAppendDiplomaticThreatHistoryResult(threat);
			}
			else if (document.HistoryResultRecorded)
			{
				threat.HistoryResultRecorded = true;
			}
			if (threat.DomesticPenaltyCompleted)
			{
				TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
			}
			if (threat.IssuerRewardCompleted)
			{
				TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
			}
		}
	}

	private void FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(WorldDiplomacyDocument document)
	{
		if (document?.HistoryDeclarationRecorded != true || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Where(x => x != null
			&& ((x.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>()).Any(decision => decision != null
					&& !decision.HistoryRecorded
					&& string.Equals(decision.DecisionDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
				|| (!x.NonComplianceHistoryRecorded
					&& string.Equals(x.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetDecisionDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)))))
		{
			TryAppendDiplomaticThreatNonComplianceHistoryResult(threat);
		}
	}

	private void RetryDiplomaticThreatHistoryResults()
	{
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null
				&& ((x.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>()).Any(decision => decision != null && !decision.HistoryRecorded)
					|| (string.Equals(x.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
						&& !x.NonComplianceHistoryRecorded)))
			.OrderBy(x => x.UpdatedDay).ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase).Take(8))
		{
			TryAppendDiplomaticThreatNonComplianceHistoryResult(threat);
		}
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && !x.HistoryResultRecorded
				&& (string.Equals(x.Status, "breached", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
					|| (string.Equals(x.Status, "enforced", StringComparison.OrdinalIgnoreCase)
						&& !string.IsNullOrWhiteSpace(x.ResolutionDocumentId))))
			.OrderBy(x => x.UpdatedDay).ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase).Take(8))
		{
			threat.UpdatedDay = CurrentDay();
			if (string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase))
			{
				TryAppendDiplomaticThreatHistoryResult(threat);
				continue;
			}
			WorldDiplomacyDocument source = ResolveDocument(FirstNonEmpty(threat.ComplianceDocumentId, threat.ResolutionDocumentId));
			if (source == null || !source.ChangedDiplomaticState) continue;
			try
			{
				AppendCanonicalDocumentEvents(source);
				FinalizeDiplomaticThreatHistoryAfterDocument(source);
			}
			catch (Exception ex)
			{
				Log("threat history retry failed threat=" + threat.ThreatId + " error=" + ex.Message);
			}
		}
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& x.DomesticPenaltyCompleted && !x.DomesticPenaltyHistoryRecorded)
			.OrderBy(x => x.UpdatedDay).ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase).Take(8))
		{
			TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
		}
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null && string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
				&& x.IssuerRewardCompleted && !x.IssuerRewardHistoryRecorded)
			.OrderBy(x => x.UpdatedDay).ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase).Take(8))
		{
			TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
		}
	}

	private bool CompleteUnresolvableDiplomaticThreatDomesticPenalty(
		WorldDiplomacyThreat threat,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		if (threat == null) return false;
		threat.DomesticPenaltyEligibleClanIds ??= new List<string>();
		threat.DomesticPenaltyAppliedClanIds ??= new List<string>();
		threat.DomesticPenaltySkippedClanIds ??= new List<string>();
		HashSet<string> applied = new HashSet<string>(threat.DomesticPenaltyAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skipped = new HashSet<string>(threat.DomesticPenaltySkippedClanIds, StringComparer.OrdinalIgnoreCase);
		foreach (string clanId in threat.DomesticPenaltyEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)))
		{
			if (!applied.Contains(clanId)) skipped.Add(clanId);
		}
		threat.DomesticPenaltySkippedClanIds = skipped.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		threat.DomesticPenaltySnapshotCaptured = true;
		threat.DomesticPenaltyCompleted = true;
		threat.UpdatedDay = CurrentDay();
		affectedClanCount = applied.Count;
		Log("ultimatum compliance domestic penalty finalized without kingdom object threat=" + threat.ThreatId
			+ " applied=" + applied.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skipped.Count.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private bool CompleteUnresolvableDiplomaticThreatIssuerRelationReward(
		WorldDiplomacyThreat threat,
		out int affectedClanCount)
	{
		affectedClanCount = 0;
		if (threat == null) return false;
		threat.IssuerRewardEligibleClanIds ??= new List<string>();
		threat.IssuerRewardAppliedClanIds ??= new List<string>();
		threat.IssuerRewardSkippedClanIds ??= new List<string>();
		if (!threat.IssuerRewardSnapshotCaptured)
		{
			threat.IssuerRewardAmount = GetThreatComplianceIssuerRelationReward();
		}
		HashSet<string> applied = new HashSet<string>(threat.IssuerRewardAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skipped = new HashSet<string>(threat.IssuerRewardSkippedClanIds, StringComparer.OrdinalIgnoreCase);
		foreach (string clanId in threat.IssuerRewardEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)))
		{
			if (!applied.Contains(clanId)) skipped.Add(clanId);
		}
		threat.IssuerRewardSkippedClanIds = skipped.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		threat.IssuerRewardSnapshotCaptured = true;
		threat.IssuerRewardCompleted = true;
		threat.UpdatedDay = CurrentDay();
		affectedClanCount = applied.Count;
		if (threat.IssuerRewardAmount <= 0 || applied.Count == 0) threat.IssuerRewardHistoryRecorded = true;
		Log("diplomatic threat issuer relation reward finalized without kingdom object threat=" + threat.ThreatId
			+ " applied=" + applied.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skipped.Count.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat)
	{
		if (threat == null || threat.DomesticPenaltyHistoryRecorded || !threat.DomesticPenaltyCompleted
			|| !string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyDocument compliance = ResolveDocument(threat.ComplianceDocumentId);
		if (compliance?.HistoryDeclarationRecorded != true) return;
		try
		{
			int appliedCount = (threat.DomesticPenaltyAppliedClanIds ?? new List<string>())
				.Count(x => !string.IsNullOrWhiteSpace(x));
			int skippedCount = (threat.DomesticPenaltySkippedClanIds ?? new List<string>())
				.Count(x => !string.IsNullOrWhiteSpace(x));
			Kingdom compliant = ResolveKingdomIncludingEliminated(threat.TargetKingdomId);
			string sourceKey = "threat:" + threat.ThreatId + ":domestic_penalty";
			string result = "经游戏机制确认：" + KingdomName(compliant) + "明确退让后，已按每个正式封臣家族与退让时王族关系降低20点（最低为-100）的规则完成国内关系结算；已结算"
				+ appliedCount.ToString(CultureInfo.InvariantCulture) + "个家族"
				+ (skippedCount > 0 ? "，另有" + skippedCount.ToString(CultureInfo.InvariantCulture) + "个已无有效关系对象的家族未执行" : "") + "。";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", sourceKey,
				FirstNonEmpty(threat.ComplianceDocumentId, threat.ThreatId), threat.UpdatedDay,
				FormatCampaignDate(threat.UpdatedDay), threat.TargetKingdomId, new[] { threat.IssuerKingdomId },
				"comply_ultimatum", "binding", result, verified: true,
				respondingToThreatDocumentId: threat.StageDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(sourceKey)
				|| (_storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
					.Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
			{
				threat.DomesticPenaltyHistoryRecorded = true;
			}
		}
		catch (Exception ex)
		{
			Log("domestic penalty history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
		}
	}

	private void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat)
	{
		if (threat == null) return;
		CaptureDiplomaticThreatNonComplianceEvent(threat);
		foreach (WorldDiplomacyThreatNonComplianceEvent decision in (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
			.Where(x => x != null && !x.HistoryRecorded)
			.OrderBy(x => x.DecisionDay)
			.ThenBy(x => x.StageDocumentId, StringComparer.OrdinalIgnoreCase))
		{
			TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, decision);
		}
		WorldDiplomacyThreatNonComplianceEvent current = (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
			.FirstOrDefault(x => x != null
				&& string.Equals(x.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase));
		threat.NonComplianceHistoryRecorded = current?.HistoryRecorded == true;
	}

	private void TryAppendDiplomaticThreatNonComplianceHistoryResult(
		WorldDiplomacyThreat threat,
		WorldDiplomacyThreatNonComplianceEvent decision)
	{
		if (threat == null || decision == null || decision.HistoryRecorded
			|| string.IsNullOrWhiteSpace(decision.StageDocumentId)
			|| string.IsNullOrWhiteSpace(decision.DecisionDocumentId)) return;
		WorldDiplomacyDocument response = ResolveDocument(decision.DecisionDocumentId);
		if (response?.HistoryDeclarationRecorded != true) return;
		try
		{
			Kingdom issuer = ResolveKingdom(threat.IssuerKingdomId);
			Kingdom target = ResolveKingdom(threat.TargetKingdomId);
			string stageLabel = string.Equals(decision.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
				? "战争最后通牒"
				: "谴责";
			string sourceKey = "threat:" + threat.ThreatId + ":target_noncompliance:" + decision.StageDocumentId;
			string result = "经游戏机制确认：" + KingdomName(target) + "在收到" + KingdomName(issuer) + "的" + stageLabel
				+ "后，其第一份已发布公文没有使用comply_ultimatum明确退让，因此已作出不退让决定。";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", sourceKey,
				decision.DecisionDocumentId, decision.DecisionDay,
				FormatCampaignDate(decision.DecisionDay), threat.TargetKingdomId,
				new[] { threat.IssuerKingdomId }, NormalizeIntent(response?.Intent),
				NormalizeCommitment(response?.Commitment), result, verified: true,
				respondingToThreatDocumentId: decision.StageDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(sourceKey)
				|| (_storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
					.Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
			{
				decision.HistoryRecorded = true;
				if (string.Equals(decision.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase))
				{
					threat.NonComplianceHistoryRecorded = true;
				}
			}
		}
		catch (Exception ex)
		{
			Log("threat noncompliance history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
		}
	}

	private void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat)
	{
		if (threat == null || threat.HistoryResultRecorded
			|| !string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyDocument resolution = ResolveDocument(threat.ResolutionDocumentId);
		if (resolution?.HistoryDeclarationRecorded != true) return;
		try
		{
			Kingdom issuer = ResolveKingdom(threat.IssuerKingdomId);
			Kingdom target = ResolveKingdom(threat.TargetKingdomId);
			string stageLabel = string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? "最后通牒" : "谴责";
			string expected = threat.Stage == "ultimatum" ? "宣战" : "升级为最后通牒";
			string sourceKey = "threat:" + threat.ThreatId + ":reputation_penalty";
			string result = "经游戏机制确认：" + KingdomName(issuer) + "未在其下一份已发布公文中对"
				+ KingdomName(target) + expected + "，此前" + stageLabel + "未获兑现，国家威望降低"
				+ threat.ReputationPenaltyAmount.ToString(CultureInfo.InvariantCulture) + "点。";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", sourceKey,
				FirstNonEmpty(threat.ResolutionDocumentId, threat.StageDocumentId, threat.ThreatId),
				threat.UpdatedDay, FormatCampaignDate(threat.UpdatedDay), threat.IssuerKingdomId,
				new[] { threat.TargetKingdomId }, threat.Stage,
				string.Equals(threat.Stage, "warning", StringComparison.OrdinalIgnoreCase) ? "non_binding" : "binding",
				result, verified: true, respondingToThreatDocumentId: threat.StageDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(sourceKey)
				|| (_storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
					.Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
			{
				threat.HistoryResultRecorded = true;
			}
		}
		catch (Exception ex)
		{
			Log("national prestige penalty history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
		}
	}

	private bool HasOpenDiplomaticThreatForRound(string roundId)
	{
		if (string.IsNullOrWhiteSpace(roundId)) return false;
		return (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Any(x => IsOpenDiplomaticThreat(x)
			&& (string.Equals(x.StageRoundId, roundId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.TargetDecisionRoundId, roundId, StringComparison.OrdinalIgnoreCase)));
	}

	private WorldDiplomacyRound EnsureActiveRound(Kingdom initiator, Kingdom target, bool isPlayerInsertion)
	{
		if (_storage.ActiveRound != null && string.Equals(_storage.ActiveRound.State, "active", StringComparison.OrdinalIgnoreCase))
		{
			return _storage.ActiveRound;
		}
		Kingdom roundInitiator = ResolveWorldDiplomacyRepresentative(initiator);
		Kingdom roundTarget = ResolveWorldDiplomacyRepresentative(target);
		int day = CurrentDay();
		int targetDurationDays = GetRoundLengthDays();
		WorldDiplomacyRound round = new WorldDiplomacyRound
		{
			SchemaVersion = RelaySchemaVersion,
			RoundId = NewId("diplomacy_round"),
			InitiatorKingdomId = roundInitiator?.StringId ?? "",
			State = "active",
			StartedDay = day,
			LastActivityDay = day,
			SoftEndDay = day + targetDurationDays,
			HardEndDay = day + GetRoundHardDurationDays(targetDurationDays),
			RelayPassDurationDays = GetCourtMaxDeliveryDays(),
			IsPlayerInsertion = isPlayerInsertion
		};
		_storage.ActiveRound = round;
		EnsureRoundParticipant(round, roundInitiator?.StringId, "active", mandatoryReply: false);
		if (roundTarget != roundInitiator)
		{
			EnsureRoundParticipant(round, roundTarget?.StringId, "observer", mandatoryReply: false);
		}
		return round;
	}

	private string GetDiplomaticThreatTerminalCloseReason(WorldDiplomacyDocument document, out bool resolved)
	{
		resolved = false;
		if (document == null) return "";
		List<WorldDiplomacyThreat> linked = (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => x != null
				&& (string.Equals(x.ComplianceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(x.ResolutionDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)))
			.ToList();
		if (linked.Any(x => string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)))
		{
			resolved = true;
			return "threat_target_complied";
		}
		if (linked.Any(x => string.Equals(x.Status, "enforced", StringComparison.OrdinalIgnoreCase)))
		{
			resolved = true;
			return "threat_followed_by_war";
		}
		if (!linked.Any(x => string.Equals(x.Status, "breached", StringComparison.OrdinalIgnoreCase))) return "";
		bool successfulWar = document.Actions?.Any(x => x != null && x.ChangedDiplomaticState
			&& NormalizeIntent(x.Intent) == "declare_war") == true
			|| (document.ChangedDiplomaticState && NormalizeIntent(document.Intent) == "declare_war");
		if (successfulWar)
		{
			resolved = true;
			return "threat_warning_skipped_but_war_started";
		}
		return "threat_next_declaration_breached";
	}

	private bool TryGetConfirmedRoundResult(
		WorldDiplomacyDocument document,
		WorldDiplomacyRound round,
		out string closeReason,
		out string roundStatus)
	{
		closeReason = "";
		roundStatus = "resolved";
		if (document == null || round == null) return false;
		string threatReason = GetDiplomaticThreatTerminalCloseReason(document, out bool threatResolved);
		if (!string.IsNullOrWhiteSpace(threatReason))
		{
			closeReason = threatReason;
			roundStatus = threatResolved ? "resolved" : "deadlocked";
			return true;
		}
		if (document.Actions?.Count > 0)
		{
			List<string> confirmedReasons = new List<string>();
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null))
			{
				string actionIntent = NormalizeIntent(action.Intent);
				string proposal = ResponseIntentToProposalIntent(actionIntent);
				WorldDiplomacyRoundOffer offer = string.IsNullOrWhiteSpace(proposal)
					? null
					: (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).FirstOrDefault(x => x != null
						&& string.Equals(x.SourceDocumentId, action.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
						&& (string.IsNullOrWhiteSpace(action.RespondingToOfferActionId)
							|| string.Equals(x.SourceActionId, action.RespondingToOfferActionId, StringComparison.OrdinalIgnoreCase)));
				WorldDiplomacyConfirmedResultKind kind = WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(
					new WorldDiplomacyResultObservation(actionIntent, action.ChangedDiplomaticState,
						offer != null, offer?.Status, linkedThreatStatus: "", isExternallyResolvedFact: false));
				if (!WorldDiplomacyResultSettlementRules.IsConfirmedResult(kind)
					&& !(actionIntent == "comply_ultimatum" && action.ChangedDiplomaticState)) continue;
				string actionReason = kind switch
				{
					WorldDiplomacyConfirmedResultKind.OfferAccepted => "offer_accepted",
					WorldDiplomacyConfirmedResultKind.OfferRejected => "offer_rejected",
					_ => actionIntent switch
					{
						"declare_war" => "war_declared",
						"break_alliance" => "alliance_broken",
						"cancel_trade" => "trade_cancelled",
						"comply_ultimatum" => "threat_target_complied",
						"accept_peace" or "accept_alliance" or "accept_trade" => "offer_accepted",
						_ => "diplomatic_result"
					}
				};
				if (!confirmedReasons.Contains(actionReason, StringComparer.OrdinalIgnoreCase)) confirmedReasons.Add(actionReason);
			}
			if (confirmedReasons.Count == 0) return false;
			closeReason = confirmedReasons.Count == 1 ? confirmedReasons[0] : "multiple_diplomatic_results";
			roundStatus = "resolved";
			return true;
		}

		string intent = NormalizeIntent(document.Intent);
		string proposalIntent = ResponseIntentToProposalIntent(intent);
		WorldDiplomacyRoundOffer matchedOffer = string.IsNullOrWhiteSpace(proposalIntent)
			? null
			: (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase));
		WorldDiplomacyConfirmedResultKind resultKind = WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(
			new WorldDiplomacyResultObservation(
				intent,
				document.ChangedDiplomaticState,
				matchedOffer != null,
				matchedOffer?.Status,
				linkedThreatStatus: "",
				isExternallyResolvedFact: string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase)));
		if (!WorldDiplomacyResultSettlementRules.IsConfirmedResult(resultKind))
		{
			if (intent == "comply_ultimatum" && document.ChangedDiplomaticState)
			{
				closeReason = "threat_target_complied";
				return true;
			}
			return false;
		}
		if (resultKind == WorldDiplomacyConfirmedResultKind.OfferAccepted)
		{
			closeReason = "offer_accepted";
			return true;
		}
		if (resultKind == WorldDiplomacyConfirmedResultKind.OfferRejected)
		{
			closeReason = "offer_rejected";
			return true;
		}
		closeReason = intent switch
		{
			"declare_war" => "war_declared",
			"break_alliance" => "alliance_broken",
			"cancel_trade" => "trade_cancelled",
			"accept_peace" or "accept_alliance" or "accept_trade" => "offer_accepted",
			_ => ""
		};
		return !string.IsNullOrWhiteSpace(closeReason);
	}

	private static bool SettlementSlotHasKind(WorldDiplomacyResultSettlementSlot slot, string kind)
	{
		return slot != null && !string.IsNullOrWhiteSpace(kind)
			&& (slot.Kind ?? "").Split('+').Any(x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
	}

	private bool IsWarResponseNoActionAllowed(
		WorldDiplomacyRound round,
		string slotId,
		Kingdom author,
		Kingdom target)
	{
		if (round?.ResultSettlementPending != true || author == null || target == null
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			|| string.IsNullOrWhiteSpace(slotId)
			|| !string.Equals(round.ResultSettlementCurrentSlotId, slotId, StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
			.FirstOrDefault(x => x != null
				&& string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
		if (!SettlementSlotHasKind(slot, "war_response")) return false;
		if (slot.RelatedKingdomIds == null
			|| !slot.RelatedKingdomIds.Contains(target.StringId, StringComparer.OrdinalIgnoreCase)) return false;
		foreach (string sourceDocumentId in slot.SourceDocumentIds ?? new List<string>())
		{
			WorldDiplomacyDocument war = ResolveDocument(sourceDocumentId);
			if (war?.IsReadyForPublication != true
				|| !string.Equals(war.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(war.AuthorKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)) continue;
			if (war.Actions?.Any(x => x != null
				&& x.ChangedDiplomaticState
				&& string.Equals(NormalizeIntent(x.Intent), "declare_war", StringComparison.Ordinal)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)) == true) return true;
			if ((war.Actions == null || war.Actions.Count == 0)
				&& war.ChangedDiplomaticState
				&& string.Equals(NormalizeIntent(war.Intent), "declare_war", StringComparison.Ordinal)
				&& string.Equals(war.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)) return true;
		}
		return false;
	}

	private bool IsImmediateWarResponsePeaceSuppressed(
		WorldDiplomacyRound round,
		string slotId,
		Kingdom author,
		Kingdom target)
	{
		return IsWarResponseNoActionAllowed(
			round,
			FirstNonEmpty(slotId, round?.ResultSettlementCurrentSlotId),
			author,
			target);
	}

	private bool IsNonRootAiRelayNoActionAllowed(
		WorldDiplomacyRound round,
		string resultSettlementSlotId,
		Kingdom author,
		Kingdom target,
		bool isRelayTurn,
		bool isExternalResponseOnly = false,
		WorldDiplomacyDocument responseSource = null)
	{
		if (round == null || author == null || target == null || author == target
			|| IsPlayerKingdom(author) || author.IsEliminated || target.IsEliminated
			|| !HasIndependentWorldDiplomacyAuthority(author)
			|| !HasIndependentWorldDiplomacyAuthority(target)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyDocument root = ResolveDocument(round.RootDocumentId);
		if (root?.IsReadyForPublication != true || !IsActionableDiplomacyIntent(root.Intent)) return false;
		if (isExternalResponseOnly)
		{
			if (responseSource?.IsReadyForPublication != true
				|| !responseSource.IsPlayerAuthored
				|| string.IsNullOrWhiteSpace(responseSource.DocumentId)
				|| !string.Equals(responseSource.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(responseSource.AuthorKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)) return false;
			bool isPrimaryTarget = string.Equals(
				responseSource.TargetKingdomId,
				author.StringId,
				StringComparison.OrdinalIgnoreCase);
			bool isRepresentativeTarget = IsDiplomaticRepresentativeForAddressedVassal(author, responseSource);
			bool isDirectlyAddressed = isPrimaryTarget
				|| isRepresentativeTarget
				|| (responseSource.AddressedKingdomIds ?? new List<string>())
					.Contains(author.StringId, StringComparer.OrdinalIgnoreCase);
			WorldDiplomacyRoundParticipant requiredResponder = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
			if (!isDirectlyAddressed
				|| (!isPrimaryTarget && !isRepresentativeTarget && !responseSource.RequiresResponse)
				|| requiredResponder?.MandatoryReplyPending != true
				|| !string.Equals(requiredResponder.LastTriggeredDocumentId, responseSource.DocumentId, StringComparison.OrdinalIgnoreCase)) return false;
			// Result settlement owns every remaining speaking right. An older external job
			// has no settlement slot and must not manufacture a parallel statement turn.
			if (round.ResultSettlementPending) return false;
			if (!round.RelayPlanned) return !isRelayTurn;
			return isRelayTurn
				&& RoundRouteContainsKingdom(round, author.StringId)
				&& RoundRouteContainsKingdom(round, target.StringId);
		}
		if (!isRelayTurn) return false;
		if (round.ResultSettlementPending)
		{
			string slotId = FirstNonEmpty(resultSettlementSlotId, round.ResultSettlementCurrentSlotId);
			if (string.IsNullOrWhiteSpace(slotId)
				|| !string.Equals(round.ResultSettlementCurrentSlotId, slotId, StringComparison.OrdinalIgnoreCase)
				|| !CanUseResultSettlementTarget(round, author, target)) return false;
			WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
			if (slot == null) return false;
			bool hasRelatedKingdom = slot.RelatedKingdomIds?.Any(x => !string.IsNullOrWhiteSpace(x)) == true;
			return hasRelatedKingdom
				? slot.RelatedKingdomIds.Contains(target.StringId, StringComparer.OrdinalIgnoreCase)
				: RoundRouteContainsKingdom(round, target.StringId);
		}
		if (!round.RelayPlanned || !round.RelayWaiting
			|| !string.IsNullOrWhiteSpace(resultSettlementSlotId)
			|| !RoundRouteContainsKingdom(round, author.StringId)
			|| !RoundRouteContainsKingdom(round, target.StringId)) return false;
		List<string> route = round.RelayRouteKingdomIds ?? new List<string>();
		return round.RelayCursor >= 0 && round.RelayCursor < route.Count
			&& string.Equals(route[round.RelayCursor], author.StringId, StringComparison.OrdinalIgnoreCase);
	}

	private bool CanUseResultSettlementTarget(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target)
	{
		if (round?.ResultSettlementPending != true || author == null || target == null
			|| target == author || target.IsEliminated || !HasIndependentWorldDiplomacyAuthority(target)) return false;
		if (RoundRouteContainsKingdom(round, target.StringId)) return true;
		return (round.RelayRouteKingdomIds?.Count ?? 0) < MaxRelayParticipants;
	}

	private bool TryIncludeResultSettlementTarget(WorldDiplomacyRound round, string kingdomId)
	{
		if (round?.ResultSettlementPending != true || string.IsNullOrWhiteSpace(kingdomId)) return false;
		round.RelayRouteKingdomIds ??= new List<string>();
		if (round.RelayRouteKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase)) return true;
		Kingdom kingdom = ResolveKingdom(kingdomId);
		if (kingdom == null || kingdom.IsEliminated || !HasIndependentWorldDiplomacyAuthority(kingdom)
			|| round.RelayRouteKingdomIds.Count >= MaxRelayParticipants) return false;
		round.RelayRouteKingdomIds.Add(kingdomId);
		round.HardEndDay = Math.Max(round.HardEndDay, CurrentDay() + 3);
		WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdomId, "active", mandatoryReply: false);
		participant.SelectedForRelay = true;
		participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
		Log("round result settlement participant appended round=" + round.RoundId + " kingdom=" + kingdomId);
		AddOrMergeResultSettlementSlot(round, kingdomId, "route",
			round.ResultSettlementTriggerDocumentId, "", prioritize: false);
		return true;
	}

	private void AddOrMergeResultSettlementSlot(
		WorldDiplomacyRound round,
		string kingdomId,
		string kind,
		string sourceDocumentId,
		string relatedKingdomId,
		bool prioritize)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId)
			|| !TryIncludeResultSettlementTarget(round, kingdomId)) return;
		round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
		WorldDiplomacyResultSettlementSlot slot = round.ResultSettlementSlots
			.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (slot == null)
		{
			slot = new WorldDiplomacyResultSettlementSlot
			{
				SlotId = NewId("diplomacy_result_slot"),
				KingdomId = kingdomId,
				Kind = string.IsNullOrWhiteSpace(kind) ? "route" : kind,
				Status = "pending"
			};
			round.ResultSettlementSlots.Add(slot);
		}
		else if (!string.IsNullOrWhiteSpace(kind) && !SettlementSlotHasKind(slot, kind))
		{
			slot.Kind = string.IsNullOrWhiteSpace(slot.Kind) ? kind : slot.Kind + "+" + kind;
		}
		slot.SourceDocumentIds ??= new List<string>();
		slot.RelatedKingdomIds ??= new List<string>();
		if (!string.IsNullOrWhiteSpace(sourceDocumentId)
			&& !slot.SourceDocumentIds.Contains(sourceDocumentId, StringComparer.OrdinalIgnoreCase))
		{
			slot.SourceDocumentIds.Add(sourceDocumentId);
		}
		if (!string.IsNullOrWhiteSpace(relatedKingdomId)
			&& !slot.RelatedKingdomIds.Contains(relatedKingdomId, StringComparer.OrdinalIgnoreCase))
		{
			slot.RelatedKingdomIds.Add(relatedKingdomId);
		}
		if (prioritize)
		{
			round.ResultSettlementSlots.Remove(slot);
			round.ResultSettlementSlots.Insert(0, slot);
		}
	}

	private void InitializeResultSettlementRouteSlots(WorldDiplomacyRound round)
	{
		if (round == null || round.ResultSettlementRouteInitialized || !round.RelayPlanned) return;
		HashSet<string> spoken = new HashSet<string>((_storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.AuthorKingdomId)
			.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		foreach (string kingdomId in round.RelayRouteKingdomIds ?? new List<string>())
		{
			if (!spoken.Contains(kingdomId))
			{
				AddOrMergeResultSettlementSlot(round, kingdomId, "route", round.ResultSettlementTriggerDocumentId, "", prioritize: false);
			}
		}
		round.ResultSettlementRouteInitialized = true;
	}

	private bool IsThreatRelevantToResultSettlement(WorldDiplomacyThreat threat, WorldDiplomacyRound round)
	{
		return threat != null && round != null && IsOpenDiplomaticThreat(threat)
			&& (string.Equals(threat.StageRoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(threat.TargetDecisionRoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(threat.ObligationRoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
	}

	private void RefreshResultSettlementActionSlots(WorldDiplomacyRound round)
	{
		if (round == null || !round.ResultSettlementPending || !round.RelayPlanned) return;
		InitializeResultSettlementRouteSlots(round);
		PruneInvalidOffers(round);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			Kingdom target = ResolveKingdom(offer.TargetKingdomId);
			if (target == null || !HasIndependentWorldDiplomacyAuthority(target)
				|| !TryIncludeResultSettlementTarget(round, target.StringId))
			{
				offer.Status = "invalidated";
				continue;
			}
			AddOrMergeResultSettlementSlot(round, target.StringId, "offer_response",
				offer.SourceDocumentId, offer.ProposerKingdomId, prioritize: true);
		}
		foreach (WorldDiplomacyThreat threat in (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsThreatRelevantToResultSettlement(x, round)))
		{
			if (string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
			{
				AddOrMergeResultSettlementSlot(round, threat.TargetKingdomId, "threat_response",
					threat.StageDocumentId, threat.IssuerKingdomId, prioritize: true);
			}
			else if (string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
			{
				AddOrMergeResultSettlementSlot(round, threat.IssuerKingdomId, "threat_followthrough",
					threat.StageDocumentId, threat.TargetKingdomId, prioritize: true);
			}
		}
	}

	private void AddWarResponseResultSettlementSlot(WorldDiplomacyRound round, WorldDiplomacyDocument document)
	{
		if (round == null || document == null) return;
		round.ResultSettlementWarDocumentIds ??= new List<string>();
		if (document.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null
				&& x.ChangedDiplomaticState
				&& NormalizeIntent(x.Intent) == "declare_war"
				&& !string.IsNullOrWhiteSpace(x.TargetKingdomId)))
			{
				string actionKey = (document.DocumentId ?? "") + "#" + (action.ActionId ?? "");
				if (round.ResultSettlementWarDocumentIds.Contains(actionKey, StringComparer.OrdinalIgnoreCase)) continue;
				round.ResultSettlementWarDocumentIds.Add(actionKey);
				AddOrMergeResultSettlementSlot(round, action.TargetKingdomId, "war_response",
					document.DocumentId, document.AuthorKingdomId, prioritize: true);
			}
			return;
		}
		if (!document.ChangedDiplomaticState || NormalizeIntent(document.Intent) != "declare_war"
			|| string.IsNullOrWhiteSpace(document.TargetKingdomId)) return;
		if (round.ResultSettlementWarDocumentIds.Contains(document.DocumentId, StringComparer.OrdinalIgnoreCase)) return;
		round.ResultSettlementWarDocumentIds.Add(document.DocumentId);
		AddOrMergeResultSettlementSlot(round, document.TargetKingdomId, "war_response",
			document.DocumentId, document.AuthorKingdomId, prioritize: true);
	}

	private void BeginOrExtendRoundResultSettlement(
		WorldDiplomacyRound round,
		WorldDiplomacyDocument document,
		string closeReason,
		string roundStatus)
	{
		if (round == null || document == null) return;
		if (!round.ResultSettlementPending)
		{
			round.ResultSettlementPending = true;
			round.ResultSettlementTriggerDocumentId = document.DocumentId;
			round.ResultSettlementCloseReason = string.IsNullOrWhiteSpace(closeReason) ? "result_settled" : closeReason;
			round.ResultSettlementRoundStatus = string.Equals(roundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
				? "deadlocked" : "resolved";
			round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
			round.ResultSettlementWarDocumentIds ??= new List<string>();
			round.RelayWaiting = false;
			// A result near the old relay deadline must still leave enough bounded time for
			// every selected speaker and every newly addressed action target to answer.
			int settlementWindowDays = Math.Max(14,
				((round.RelayRouteKingdomIds?.Count ?? 0) + 2) * 2);
			round.HardEndDay = Math.Max(round.HardEndDay, CurrentDay() + settlementWindowDays);
			_storage.RelayArrivals.RemoveAll(x => x != null
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
			_storage.Jobs.RemoveAll(x => x != null
				&& string.Equals(x.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(FirstNonEmpty(x.RoundId, x.ExchangeId), round.RoundId, StringComparison.OrdinalIgnoreCase));
			Log("round result settlement opened round=" + round.RoundId
				+ " trigger=" + document.DocumentId + " reason=" + round.ResultSettlementCloseReason);
		}
		else if (string.Equals(roundStatus, "resolved", StringComparison.OrdinalIgnoreCase))
		{
			round.ResultSettlementRoundStatus = "resolved";
		}
		InitializeResultSettlementRouteSlots(round);
		AddWarResponseResultSettlementSlot(round, document);
		RefreshResultSettlementActionSlots(round);
	}

	private void ConsumeResultSettlementSpeaker(WorldDiplomacyRound round, WorldDiplomacyDocument document)
	{
		if (round == null || document == null || !round.ResultSettlementPending) return;
		round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
		WorldDiplomacyResultSettlementSlot slot = !string.IsNullOrWhiteSpace(document.ResultSettlementSlotId)
			? round.ResultSettlementSlots.FirstOrDefault(x => x != null
				&& string.Equals(x.SlotId, document.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase))
			: round.ResultSettlementSlots.FirstOrDefault(x => x != null
				&& string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (slot == null) return;
		round.ResultSettlementSlots.Remove(slot);
		if (string.Equals(round.ResultSettlementCurrentSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase))
		{
			round.ResultSettlementCurrentSlotId = "";
			round.ResultSettlementPlayerWaitingSinceDay = 0;
		}
		round.RelayWaiting = false;
		Log("round result settlement turn consumed round=" + round.RoundId
			+ " slot=" + slot.SlotId + " author=" + document.AuthorKingdomId);
	}

	private void ExpireUnansweredSettlementOffersForNoActionDeclaration(
		WorldDiplomacyRound round,
		WorldDiplomacyDocument document)
	{
		if (round?.ResultSettlementPending != true || document == null
			|| (!document.IsRoundResponseNoActionDeclaration && !document.IsWarResponseNoActionDeclaration)
			|| string.IsNullOrWhiteSpace(document.ResultSettlementSlotId)
			|| string.IsNullOrWhiteSpace(document.AuthorKingdomId)) return;
		WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
			.FirstOrDefault(x => x != null
				&& string.Equals(x.SlotId, document.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (slot == null || slot.SourceDocumentIds == null || slot.SourceDocumentIds.Count == 0) return;
		HashSet<string> sourceIds = new HashSet<string>(
			slot.SourceDocumentIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		if (sourceIds.Count == 0) return;
		int expired = 0;
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& sourceIds.Contains(x.SourceDocumentId ?? "")))
		{
			offer.Status = "expired";
			expired++;
		}
		if (expired > 0)
		{
			Log("round response statement left settlement offers unaccepted round=" + round.RoundId
				+ " slot=" + slot.SlotId
				+ " author=" + document.AuthorKingdomId
				+ " expired=" + expired.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void InvalidateUnserviceableResultSettlementObligations(WorldDiplomacyRound round, string kingdomId, string reason)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId)) return;
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)))
		{
			offer.Status = "invalidated";
		}
		foreach (WorldDiplomacyThreat threat in (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsThreatRelevantToResultSettlement(x, round)))
		{
			bool requiredSpeaker = string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
				? string.Equals(threat.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
				: string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(threat.IssuerKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase);
			if (requiredSpeaker) InvalidateDiplomaticThreatForNormalization(threat, reason, CurrentDay());
		}
	}

	private List<Kingdom> GetResultSettlementActionableTargets(WorldDiplomacyRound round, Kingdom author)
	{
		if (round == null || author == null) return new List<Kingdom>();
		return Kingdom.All
			.Where(x => CanUseResultSettlementTarget(round, author, x))
			.Where(x => BuildLegalDiplomaticDeclarationIntents(
				round, author, x, isRelayTurn: true,
				resultSettlementSlotId: round.ResultSettlementCurrentSlotId).Count > 0)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void SkipResultSettlementSlot(WorldDiplomacyRound round, string slotId, string kingdomId, string reason)
	{
		if (round == null || !round.ResultSettlementPending) return;
		WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
			.FirstOrDefault(x => x != null && ((!string.IsNullOrWhiteSpace(slotId)
				&& string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
				|| (string.IsNullOrWhiteSpace(slotId)
					&& string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase))));
		if (slot == null) return;
		InvalidateUnserviceableResultSettlementObligations(round, slot.KingdomId,
			"result_settlement_" + (reason ?? "technical_skip"));
		round.ResultSettlementSlots.Remove(slot);
		if (string.Equals(round.ResultSettlementCurrentSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase))
		{
			round.ResultSettlementCurrentSlotId = "";
			round.ResultSettlementPlayerWaitingSinceDay = 0;
		}
		round.RelayWaiting = false;
		Log("round result settlement slot skipped round=" + round.RoundId
			+ " slot=" + slot.SlotId + " kingdom=" + slot.KingdomId + " reason=" + (reason ?? ""));
	}

	private void ScheduleNextResultSettlementTurn(WorldDiplomacyRound round)
	{
		if (round == null || !round.ResultSettlementPending || !round.RelayPlanned
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		if (_storage.RelayArrivals.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			|| _storage.Jobs.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))) return;

		RefreshResultSettlementActionSlots(round);
		for (int guard = 0; guard < MaxRelayParticipants + 4; guard++)
		{
			WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null);
			if (slot == null)
			{
				round.RoundStatus = string.Equals(round.ResultSettlementRoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
					? "deadlocked" : "resolved";
				CloseActiveRound(string.IsNullOrWhiteSpace(round.ResultSettlementCloseReason)
					? "result_settled" : round.ResultSettlementCloseReason);
				return;
			}
			round.ResultSettlementCurrentSlotId = slot.SlotId;
			Kingdom receiver = ResolveKingdom(slot.KingdomId);
			if (receiver == null || !HasIndependentWorldDiplomacyAuthority(receiver)
				|| GetResultSettlementActionableTargets(round, receiver).Count == 0)
			{
				SkipResultSettlementSlot(round, slot.SlotId, slot.KingdomId, "no_legal_action");
				RefreshResultSettlementActionSlots(round);
				continue;
			}

			if (IsPlayerKingdom(receiver))
			{
				slot.Status = "waiting_player";
				round.ResultSettlementPlayerWaitingSinceDay = CurrentDay();
				round.RelayWaiting = true;
				WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, receiver.StringId, "active", mandatoryReply: true);
				participant.MandatoryReplyPending = true;
				participant.MandatorySinceDay = CurrentDay();
				participant.LastTriggeredDocumentId = slot.SourceDocumentIds?.FirstOrDefault() ?? round.ResultSettlementTriggerDocumentId;
				RecordPlayerOpportunity(round, receiver);
				return;
			}

			slot.Status = "scheduled";
			string previousKingdomId = slot.RelatedKingdomIds?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
				?? _storage.Documents.Where(x => x != null && x.IsReadyForPublication
					&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks)
					.Select(x => x.AuthorKingdomId).FirstOrDefault() ?? round.InitiatorKingdomId;
			round.RelaySequence++;
			round.RelayWaiting = true;
			_storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival
			{
				RoundId = round.RoundId,
				FromKingdomId = previousKingdomId,
				ToKingdomId = receiver.StringId,
				ResultSettlementSlotId = slot.SlotId,
				DueDay = CurrentDay(),
				Sequence = round.RelaySequence
			});
			_storage.RelayArrivals = _storage.RelayArrivals.OrderBy(x => x.DueDay).ThenBy(x => x.Sequence).ToList();
			return;
		}
	}

	private void HandleRoundDocumentProcessed(WorldDiplomacyDocument document)
	{
		if (document == null || document.RoundProgressHandled) return;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		int successfulMechanicalActionCount = document.Actions?.Count(x => x != null && x.ChangedDiplomaticState)
			?? (document.ChangedDiplomaticState ? 1 : 0);
		bool successfulMechanicalAction = successfulMechanicalActionCount > 0;
		bool substantiveProgress = document.MadeDiplomaticProgress;
		bool isRootDocument = string.IsNullOrWhiteSpace(round.RootDocumentId)
			|| string.Equals(round.RootDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase);
		WorldDiplomacyRoundParticipant participant = null;
		if (!document.RoundAccountingHandled)
		{
			substantiveProgress = IsValidatedSubstantiveProgress(document, round, successfulMechanicalAction);
			int diplomaticActionAttemptCount = document.Actions != null
				? (substantiveProgress ? document.Actions.Count(x => x != null && IsRoundDiplomaticBehaviorIntent(x.Intent)) : 0)
				: (IsValidatedDiplomaticActionAttempt(document, round, successfulMechanicalAction) ? 1 : 0);
			bool diplomaticActionAttempt = diplomaticActionAttemptCount > 0;
			int accountingDay = CurrentDay();
			if (!isRootDocument && !document.IsPlayerAuthored && document.IsRelayTurn)
			{
				participant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
			}
			round.LastActivityDay = accountingDay;
			if (successfulMechanicalAction) round.ExecutedActionCount += successfulMechanicalActionCount;
			document.MadeDiplomaticProgress = substantiveProgress;
			if (substantiveProgress)
			{
				round.SubstantiveProgressCount++;
				round.LastSubstantiveProgressDay = accountingDay;
			}
			if (diplomaticActionAttempt)
			{
				round.DiplomaticActionAttemptCount += diplomaticActionAttemptCount;
				if (round.ConsecutiveNoActionPasses >= 2 && (document.Actions?.Any(x => x != null && IsProposalIntent(x.Intent)) == true
					|| IsProposalIntent(document.Intent)))
				{
					round.HardEndDay = Math.Max(round.HardEndDay, accountingDay + Math.Max(1, round.RelayPassDurationDays));
				}
			}
			if (string.IsNullOrWhiteSpace(round.RootDocumentId))
			{
				round.RootDocumentId = document.DocumentId;
				round.InitiatorKingdomId = document.AuthorKingdomId;
				isRootDocument = true;
			}
			if (participant != null)
			{
				participant.TurnCount++;
				participant.LastSpokeDay = accountingDay;
				if (substantiveProgress) participant.ContributionMade = true;
				if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase))
				{
					participant.State = "withdrawn";
				}
			}
			document.RoundAccountingHandled = true;
			if (substantiveProgress)
			{
				Log("substantive diplomacy progress accepted round=" + round.RoundId
					+ " document=" + document.DocumentId
					+ " intent=" + NormalizeIntent(document.Intent)
					+ " count=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture));
			}
			if (diplomaticActionAttempt)
			{
				Log("diplomatic relation-change attempt accepted round=" + round.RoundId
					+ " document=" + document.DocumentId
					+ " intent=" + NormalizeIntent(document.Intent)
					+ " count=" + round.DiplomaticActionAttemptCount.ToString(CultureInfo.InvariantCulture));
			}
		}
		if (round.ResultSettlementPending)
		{
			ExpireUnansweredSettlementOffersForNoActionDeclaration(round, document);
			ConsumeResultSettlementSpeaker(round, document);
		}
		if (TryGetConfirmedRoundResult(document, round, out string confirmedCloseReason, out string confirmedRoundStatus))
		{
			BeginOrExtendRoundResultSettlement(round, document, confirmedCloseReason, confirmedRoundStatus);
		}
		if (isRootDocument)
		{
			if (document.HasEmbeddedRoundPlan && !round.RelayPlanned)
			{
				CommitEmbeddedRoundPlan(round, document);
			}
			if (!ReferenceEquals(_storage.ActiveRound, round)
				|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase))
			{
				document.RoundProgressHandled = true;
				return;
			}
			if (!round.RelayPlanned) EnqueueRoundPlanJob(round, document);
			else if (round.ResultSettlementPending) ScheduleNextResultSettlementTurn(round);
			document.RoundProgressHandled = true;
			return;
		}
		if (!round.RelayPlanned)
		{
			EnqueueRoundPlanJob(round, ResolveDocument(round.RootDocumentId) ?? document);
			document.RoundProgressHandled = true;
			return;
		}
		if (document.IsPlayerAuthored)
		{
			IntegratePlayerDeclaration(round, document);
			if (round.ResultSettlementPending)
			{
				RefreshResultSettlementActionSlots(round);
				ScheduleNextResultSettlementTurn(round);
			}
			document.RoundProgressHandled = true;
			return;
		}
		if (round.ResultSettlementPending)
		{
			if (document.IsExternalResponseOnly && participant != null) participant.MandatoryReplyPending = false;
			RefreshResultSettlementActionSlots(round);
			ScheduleNextResultSettlementTurn(round);
			document.RoundProgressHandled = true;
			return;
		}
		if (!document.IsRelayTurn)
		{
			document.RoundProgressHandled = true;
			return;
		}
		participant ??= (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
			.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (document.IsExternalResponseOnly)
		{
			if (participant != null) participant.MandatoryReplyPending = false;
			Log("priority player declaration response completed without moving relay cursor round=" + round.RoundId
				+ " document=" + document.DocumentId + " author=" + document.AuthorKingdomId);
			document.RoundProgressHandled = true;
			return;
		}
		round.RelayWaiting = false;
		if (IsTerminalNegotiationMove(document.NegotiationMove))
		{
			round.RoundStatus = string.Equals(document.NegotiationMove, "declare_deadlock", StringComparison.OrdinalIgnoreCase)
				? "deadlocked" : "resolved";
			CloseActiveRound(string.Equals(round.RoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
				? "negotiation_declared_deadlock" : "negotiation_ended");
			document.RoundProgressHandled = true;
			return;
		}
		bool hasValidatedResolution = round.ExecutedActionCount > 0;
		if (string.Equals(document.RoundStatus, "resolved", StringComparison.OrdinalIgnoreCase) && hasValidatedResolution)
		{
			round.RoundStatus = "resolved";
			CloseActiveRound("relay_resolved");
			document.RoundProgressHandled = true;
			return;
		}
		AdvanceRelay(round);
		document.RoundProgressHandled = true;
	}

	private void RetryDeferredRoundProgress()
	{
		WorldDiplomacyRound round = _storage?.ActiveRound;
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		foreach (WorldDiplomacyDocument document in (_storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication && !x.RoundProgressHandled
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).Take(8))
		{
			try
			{
				HandleRoundDocumentProcessed(document);
			}
			catch (Exception ex)
			{
				Log("deferred round progress retry failed document=" + document.DocumentId + " error=" + ex.Message);
			}
		}
	}

	private bool IsValidatedSubstantiveProgress(WorldDiplomacyDocument document, WorldDiplomacyRound round, bool successfulMechanicalAction)
	{
		if (document == null || round == null) return false;
		if (successfulMechanicalAction) return true;
		if (document.Actions?.Count > 0)
		{
			if ((_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Any(x => x != null
				&& string.Equals(x.StageDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))) return true;
			if ((round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
				&& string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase))) return true;
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null))
			{
				string proposal = ResponseIntentToProposalIntent(action.Intent);
				if (string.IsNullOrWhiteSpace(proposal)) continue;
				string expected = action.Intent.StartsWith("accept_", StringComparison.OrdinalIgnoreCase) ? "accepted" : "rejected";
				if ((round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
					&& string.Equals(x.SourceDocumentId, action.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
					&& (string.IsNullOrWhiteSpace(action.RespondingToOfferActionId)
						|| string.Equals(x.SourceActionId, action.RespondingToOfferActionId, StringComparison.OrdinalIgnoreCase))
					&& string.Equals(x.Status, expected, StringComparison.OrdinalIgnoreCase))) return true;
			}
			return false;
		}
		string intent = NormalizeIntent(document.Intent);
		if (intent == "warning" || intent == "ultimatum")
		{
			return (_storage.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Any(x => x != null
				&& string.Equals(x.StageDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
		}
		if (intent == "apology" || intent == "concession")
		{
			return !string.IsNullOrWhiteSpace(document.TargetKingdomId)
				&& !string.Equals(document.AuthorKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
		}
		if (IsProposalIntent(intent))
		{
			return (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
				&& string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase));
		}
		string proposalIntent = ResponseIntentToProposalIntent(intent);
		if (string.IsNullOrWhiteSpace(proposalIntent)) return false;
		string expectedStatus = intent.StartsWith("accept_", StringComparison.OrdinalIgnoreCase) ? "accepted" : "rejected";
		return (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
			&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
			&& (string.IsNullOrWhiteSpace(document.TargetKingdomId)
				|| string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
			&& (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId)
				|| string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase))
			&& string.Equals(x.Status, expectedStatus, StringComparison.OrdinalIgnoreCase));
	}

	private bool IsValidatedDiplomaticActionAttempt(WorldDiplomacyDocument document, WorldDiplomacyRound round, bool successfulMechanicalAction)
	{
		if (document == null || round == null) return false;
		if (successfulMechanicalAction) return true;
		string intent = NormalizeIntent(document.Intent);
		if (!IsRoundDiplomaticBehaviorIntent(intent)) return false;
		return IsValidatedSubstantiveProgress(document, round, successfulMechanicalAction: false);
	}

	private void CommitEmbeddedRoundPlan(WorldDiplomacyRound round, WorldDiplomacyDocument root)
	{
		if (round == null || root == null || round.RelayPlanned
			|| !ReferenceEquals(_storage.ActiveRound, round)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		Kingdom author = ResolveKingdom(root.AuthorKingdomId);
		List<string> candidates = GetRoundPlanActionableParticipants(author, round)
			.Select(x => x.StringId).ToList();
		WorldDiplomacyJob plan = new WorldDiplomacyJob
		{
			RoundId = round.RoundId,
			DocumentId = root.DocumentId,
			AuthorKingdomId = root.AuthorKingdomId,
			CandidateKingdomIds = candidates
		};
		JObject json = new JObject
		{
			["topic"] = FirstNonEmpty(root.PlannedRoundTopic, root.Title, "外交交涉"),
			["selected_kingdom_ids"] = new JArray(root.PlannedKingdomIds ?? new List<string>())
		};
		CommitRoundPlan(plan, json.ToString(Formatting.None));
		Log("embedded round plan committed round=" + round.RoundId
			+ " selected=" + string.Join(",", root.PlannedKingdomIds ?? new List<string>()));
	}

	private void EnqueueRoundPlanJob(WorldDiplomacyRound round, WorldDiplomacyDocument root)
	{
		if (round == null || root == null || round.RelayPlanned
			|| !ReferenceEquals(_storage.ActiveRound, round)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			|| _storage.Jobs.Any(x => x != null && string.Equals(x.Kind, "round_plan", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))) return;
		Kingdom author = ResolveKingdom(root.AuthorKingdomId);
		List<string> candidates = GetRoundPlanActionableParticipants(author, round)
			.Select(x => x.StringId).ToList();
		if (candidates.Count == 0)
		{
			CloseActiveRound("round_plan_no_actionable_participants");
			return;
		}
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_round_plan"),
			Kind = "round_plan",
			Priority = 85,
			CreatedDay = CurrentDay(),
			RoundId = round.RoundId,
			DocumentId = root.DocumentId,
			AuthorKingdomId = root.AuthorKingdomId,
			CandidateKingdomIds = candidates,
			SystemPrompt = BuildRoundPlanSystemPrompt(round),
			UserPrompt = BuildRoundPlanPrompt(root, candidates),
			CacheAffinityKey = "diplomacy-round-plan:v6",
			MaxTokens = AnalysisMaxTokens
		};
		EnqueueJob(job);
	}

	private string BuildRoundPlanSystemPrompt(WorldDiplomacyRound round)
	{
		StringBuilder sb = CreateSystemPromptBuilder(GetCommonDiplomacyContract(round));
		sb.AppendLine(RoundPlanTaskMarker + "最后一条消息的 MODE=ROUND_PLAN 决定本次任务和输出结构。");
		return sb.ToString().TrimEnd();
	}

	private static bool NeedsCanonicalHistoryRetry(WorldDiplomacyDocument document)
	{
		if (document == null || !document.IsReadyForPublication || string.IsNullOrWhiteSpace(document.DocumentId)) return false;
		bool externalResolvedFact = string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase);
		bool declarationPending = !externalResolvedFact
			&& !document.HistoryDeclarationRecorded
			&& !string.IsNullOrWhiteSpace(document.Body);
		bool resultPending = !document.HistoryResultRecorded
			&& (document.ChangedDiplomaticState || externalResolvedFact)
			&& !string.IsNullOrWhiteSpace(document.MechanicalResult);
		return declarationPending || resultPending;
	}

	private void EnqueueDeferredCanonicalHistoryRetry(string documentId)
	{
		string normalizedId = (documentId ?? "").Trim();
		if (normalizedId.Length == 0 || !_deferredCanonicalHistoryDocumentIdSet.Add(normalizedId)) return;
		_deferredCanonicalHistoryDocumentIds.Enqueue(normalizedId);
	}

	private void ScheduleDeferredCanonicalHistoryRetry(string documentId)
	{
		string normalizedId = (documentId ?? "").Trim();
		if (normalizedId.Length == 0) return;
		_deferredCanonicalHistoryRetryAttempts.TryGetValue(normalizedId, out int attempts);
		attempts = Math.Min(30, attempts + 1);
		_deferredCanonicalHistoryRetryAttempts[normalizedId] = attempts;
		int delayHours = Math.Min(24, 1 << Math.Min(4, Math.Max(0, attempts - 1)));
		_deferredCanonicalHistoryRetryAfterHour[normalizedId] = CurrentHour() + delayHours;
		EnqueueDeferredCanonicalHistoryRetry(normalizedId);
	}

	private void RetryDeferredCanonicalHistoryEntries(int maxAttempts = 16)
	{
		int attempts = Math.Min(Math.Max(0, maxAttempts), _deferredCanonicalHistoryDocumentIds.Count);
		for (int i = 0; i < attempts; i++)
		{
			string documentId = _deferredCanonicalHistoryDocumentIds.Dequeue();
			_deferredCanonicalHistoryDocumentIdSet.Remove(documentId);
			WorldDiplomacyDocument document = ResolveDocument(documentId);
			if (!NeedsCanonicalHistoryRetry(document))
			{
				_deferredCanonicalHistoryRetryAttempts.Remove(documentId);
				_deferredCanonicalHistoryRetryAfterHour.Remove(documentId);
				continue;
			}
			if (_deferredCanonicalHistoryRetryAfterHour.TryGetValue(documentId, out int retryAfterHour)
				&& CurrentHour() < retryAfterHour)
			{
				EnqueueDeferredCanonicalHistoryRetry(documentId);
				continue;
			}
			try
			{
				AppendCanonicalDocumentEvents(document);
				FinalizeDiplomaticThreatHistoryAfterDocument(document);
				FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document);
			}
			catch (Exception ex)
			{
				ScheduleDeferredCanonicalHistoryRetry(documentId);
				Log("deferred canonical history retry failed document=" + documentId + " error=" + ex.Message);
				continue;
			}
			if (NeedsCanonicalHistoryRetry(document))
			{
				ScheduleDeferredCanonicalHistoryRetry(documentId);
			}
			else
			{
				_deferredCanonicalHistoryRetryAttempts.Remove(documentId);
				_deferredCanonicalHistoryRetryAfterHour.Remove(documentId);
			}
		}
	}

	private string BuildRoundPlanPrompt(WorldDiplomacyDocument root, List<string> candidateIds)
	{
		StringBuilder sb = new StringBuilder();
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot))
		{
			sb.AppendLine(vassalageSnapshot);
		}
		sb.AppendLine("开场宣言：");
		sb.AppendLine("发起国=" + root.AuthorKingdomId + "=" + root.AuthorKingdomName);
		sb.AppendLine("标题=" + root.Title);
		sb.AppendLine("正文=" + Limit(root.Body, 2200));
		sb.AppendLine("明确指向=" + string.Join(",", root.AddressedKingdomIds ?? new List<string>()));
		sb.AppendLine("提及=" + string.Join(",", root.MentionedKingdomIds ?? new List<string>()));
		sb.AppendLine("本次参与国总数上限（包括发起国）=" + GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture));
		sb.AppendLine("候选国：");
		foreach (string id in candidateIds ?? new List<string>())
		{
			Kingdom kingdom = ResolveKingdom(id);
			if (kingdom == null) continue;
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(ResolveKingdom(root.AuthorKingdomId), kingdom, ResolveRound(root.RoundId)));
			string policy = WorldDiplomacyPolicyContext.BuildSnapshot(id);
			if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("  政策=" + Limit(policy, 500));
		}
		sb.AppendLine("【MODE=ROUND_PLAN】");
		sb.AppendLine("根据开场外交宣言和候选国现实利益，一次选定本次事件参与者；后续不会反复评估观察国。");
		sb.AppendLine("若宣言明确指向某国，该国必须参与。只选确实会介入本次交涉者，不选只会旁观评论者。参与国总数是上限，不必凑满；只可使用候选ID。");
		sb.AppendLine("事件由头不预定结果。参与国应能推动当前合法的结盟、解盟、贸易、断贸、宣战、议和，或提出、接受、拒绝、反提条件。");
		sb.AppendLine("只输出JSON：{\"topic\":\"简短外交议题\",\"selected_kingdom_ids\":[\"ID\"],\"reason\":\"简短理由\"}");
		return sb.ToString().TrimEnd();
	}

	private void CommitRoundPlan(WorldDiplomacyJob job, string raw)
	{
		WorldDiplomacyRound round = ResolveRound(job?.RoundId);
		WorldDiplomacyDocument root = ResolveDocument(job?.DocumentId);
		Kingdom initiator = ResolveKingdom(root?.AuthorKingdomId ?? round?.InitiatorKingdomId);
		if (round == null || root == null || initiator == null || round.RelayPlanned
			|| !ReferenceEquals(_storage.ActiveRound, round)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		JObject json = ParseJsonObject(raw);
		round.RoundTopic = Limit(SanitizePublicDiplomacyText(FirstNonEmpty(ReadString(json, "topic"), root.PlannedRoundTopic, root.Title, "外交交涉")), 120);
		round.TopicCategory = ((root.Actions?.Any(x => x != null && NormalizeIntent(x.Intent) is "warning" or "ultimatum") == true)
			|| NormalizeIntent(root.Intent) is "warning" or "ultimatum")
			? "war_escalation"
			: InferTopicCategory(round.RoundTopic, initiator, ResolveKingdom(root.TargetKingdomId));
		List<string> selected = new List<string>();
		HashSet<string> selectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> candidateSet = new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		foreach (string id in ReadStringList(json, "selected_kingdom_ids"))
		{
			Kingdom selectedKingdom = ResolveKingdom(id);
			if (candidateSet.Contains(id) && selectedKingdom != null && !selectedKingdom.IsEliminated
				&& HasIndependentWorldDiplomacyAuthority(selectedKingdom) && selectedSet.Add(id)) selected.Add(id);
		}
		HashSet<string> mandatoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Kingdom explicitTarget = ResolveWorldDiplomacyRepresentative(ResolveKingdom(root.TargetKingdomId));
		if (explicitTarget != null && explicitTarget != initiator) mandatoryIds.Add(explicitTarget.StringId);
		foreach (string id in _storage.Documents
			.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			.SelectMany(x => x.AddressedKingdomIds ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom mandatory = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
			if (mandatory != null && mandatory != initiator)
			{
				mandatoryIds.Add(mandatory.StringId);
				if (selectedSet.Add(mandatory.StringId)) selected.Add(mandatory.StringId);
			}
		}
		int participantLimit = GetRoundParticipantLimit();
		Kingdom primaryTarget = ResolveKingdom(root.TargetKingdomId);
		List<Kingdom> mandatoryRoute = mandatoryIds.Select(ResolveKingdom).Where(x => x != null && x != initiator)
			.Distinct()
			.OrderByDescending(x => x == primaryTarget)
			.ThenBy(x => CourtDistance(initiator, x))
			.Take(Math.Max(0, participantLimit - 1))
			.ToList();
		int optionalSlots = Math.Max(0, participantLimit - 1 - mandatoryRoute.Count);
		List<Kingdom> optionalRoute = selected.Where(x => !mandatoryIds.Contains(x)).Select(ResolveKingdom)
			.Where(x => x != null && x != initiator && !x.IsEliminated && HasIndependentWorldDiplomacyAuthority(x))
			.Distinct().Take(optionalSlots).ToList();
		List<Kingdom> remaining = mandatoryRoute.Concat(optionalRoute).Distinct().ToList();
		List<string> route = new List<string> { initiator.StringId };
		Kingdom cursor = initiator;
		while (remaining.Count > 0)
		{
			Kingdom next = remaining.OrderBy(x => CourtDistance(cursor, x)).ThenBy(x => x.StringId, StringComparer.OrdinalIgnoreCase).First();
			route.Add(next.StringId);
			remaining.Remove(next);
			cursor = next;
		}
		if (route.Count < 2)
		{
			if (ReferenceEquals(_storage.ActiveRound, round)) CloseActiveRound("round_plan_no_participants");
			return;
		}
		round.SchemaVersion = RelaySchemaVersion;
		round.RelayPlanned = true;
		round.RelayRouteKingdomIds = route;
		round.RelayCursor = 0;
		round.RelayDirection = 1;
		round.RelayPassNumber = 1;
		round.RelayPassStartedDay = CurrentDay();
		round.ActionAttemptCountAtPassStart = round.DiplomaticActionAttemptCount;
		round.ConsecutiveNoActionPasses = 0;
		round.LastAccountedRelayPassNumber = 0;
		round.RelayWaiting = false;
		foreach (string id in route)
		{
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, id, "active", mandatoryReply: false);
			participant.SelectedForRelay = true;
			participant.IsPlayerAsync = IsPlayerKingdom(ResolveKingdom(id));
		}
		round.CachePrefix = "";
		Log("relay round planned round=" + round.RoundId + " route=" + string.Join(">", route)
			+ " participantLimit=" + participantLimit.ToString(CultureInfo.InvariantCulture)
			+ " passDays=" + round.RelayPassDurationDays.ToString(CultureInfo.InvariantCulture)
			+ " targetDays=" + Math.Max(1, round.SoftEndDay - round.StartedDay).ToString(CultureInfo.InvariantCulture));
		if (round.ResultSettlementPending)
		{
			InitializeResultSettlementRouteSlots(round);
			RefreshResultSettlementActionSlots(round);
			ScheduleNextResultSettlementTurn(round);
		}
		else ScheduleNextRelayHop(round);
	}

	private float CourtDistance(Kingdom first, Kingdom second)
	{
		Settlement a = ResolveCourtSettlement(first);
		Settlement b = ResolveCourtSettlement(second);
		return a == null || b == null ? float.MaxValue : a.GatePosition.Distance(b.GatePosition);
	}

	private WorldDiplomacyBorderRelation GetKingdomBorderRelation(Kingdom first, Kingdom second)
	{
		if (first == null || second == null || first == second)
		{
			return new WorldDiplomacyBorderRelation();
		}
		EnsureKingdomBorderCache();
		return _kingdomBorderCache.TryGetValue(PairKey(first.StringId, second.StringId), out WorldDiplomacyBorderRelation relation)
			? relation
			: new WorldDiplomacyBorderRelation();
	}

	private void EnsureKingdomBorderCache()
	{
		int day = CurrentDay();
		if (_kingdomBorderCacheDay == day)
		{
			return;
		}
		_kingdomBorderCacheDay = day;
		_kingdomBorderCache.Clear();
		_kingdomBorderDistanceThreshold = MinimumBorderDistance;
		List<(Kingdom Kingdom, Settlement Settlement)> forts = Kingdom.All
			.Where(x => x != null && !x.IsEliminated)
			.SelectMany(kingdom => kingdom.Fiefs
				.Select(x => x?.Settlement)
				.Where(x => x != null && (x.IsTown || x.IsCastle))
				.Select(settlement => (kingdom, settlement)))
			.GroupBy(x => x.settlement.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
		if (forts.Count < 2)
		{
			return;
		}
		List<float> nearestDistances = new List<float>(forts.Count);
		for (int i = 0; i < forts.Count; i++)
		{
			float nearest = float.MaxValue;
			for (int j = 0; j < forts.Count; j++)
			{
				if (i == j) continue;
				float distance = forts[i].Settlement.GatePosition.Distance(forts[j].Settlement.GatePosition);
				if (distance < nearest) nearest = distance;
			}
			if (nearest < float.MaxValue) nearestDistances.Add(nearest);
		}
		nearestDistances.Sort();
		float median = nearestDistances.Count == 0 ? MinimumBorderDistance
			: nearestDistances[nearestDistances.Count / 2];
		float maximumBorderDistance = Math.Max(MinimumBorderDistance,
			Math.Min(MaximumBorderDistance, median * BorderDistanceMedianMultiplier));
		_kingdomBorderDistanceThreshold = maximumBorderDistance;
		foreach ((Kingdom kingdom, Settlement settlement) in forts)
		{
			foreach ((Kingdom otherKingdom, Settlement otherSettlement, float distance) in forts
				.Where(x => x.Kingdom != kingdom)
				.Select(x => (x.Kingdom, x.Settlement, settlement.GatePosition.Distance(x.Settlement.GatePosition)))
				.OrderBy(x => x.Item3)
				.Take(BorderForeignNeighborCount))
			{
				if (distance > maximumBorderDistance) continue;
				string key = PairKey(kingdom.StringId, otherKingdom.StringId);
				if (_kingdomBorderCache.TryGetValue(key, out WorldDiplomacyBorderRelation existing)
					&& existing.Distance <= distance)
				{
					continue;
				}
				_kingdomBorderCache[key] = new WorldDiplomacyBorderRelation
				{
					SharesBorder = true,
					FirstSettlementId = settlement.StringId ?? "",
					FirstSettlementName = settlement.Name?.ToString() ?? "",
					SecondSettlementId = otherSettlement.StringId ?? "",
					SecondSettlementName = otherSettlement.Name?.ToString() ?? "",
					Distance = distance
				};
			}
		}
		Log("kingdom border cache rebuilt day=" + day.ToString(CultureInfo.InvariantCulture)
			+ " forts=" + forts.Count.ToString(CultureInfo.InvariantCulture)
			+ " threshold=" + maximumBorderDistance.ToString("0.0", CultureInfo.InvariantCulture)
			+ " pairs=" + _kingdomBorderCache.Count.ToString(CultureInfo.InvariantCulture));
	}

	private static string DescribeBorderRelation(WorldDiplomacyBorderRelation relation)
	{
		if (relation?.SharesBorder != true) return "两国当前没有共同边境";
		string first = FirstNonEmpty(relation.FirstSettlementName, relation.FirstSettlementId, "一处边地要塞");
		string second = FirstNonEmpty(relation.SecondSettlementName, relation.SecondSettlementId, "另一处边地要塞");
		return "两国当前接壤，最直接的边地联系位于" + first + "与" + second + "一带";
	}

	private string BuildCurrentGeographicRelations(
		WorldDiplomacyRound round,
		Kingdom author,
		IEnumerable<string> targetKingdomIds = null)
	{
		if (round == null || author == null) return "【当前地理关系】无可核实对象。";
		List<string> lines = new List<string>();
		foreach (string id in (targetKingdomIds ?? round.RelayRouteKingdomIds ?? new List<string>())
			.Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom target = ResolveKingdom(id);
			if (target == null) continue;
			WorldDiplomacyBorderRelation border = GetKingdomBorderRelation(author, target);
			if (border.SharesBorder)
			{
				lines.Add(id + "=" + KingdomName(target) + "；接壤，可称邻国或讨论共同边境；" + DescribeBorderRelation(border));
				continue;
			}

			float courtDistance = CourtDistance(author, target);
			float distanceScale = Math.Max(MinimumBorderDistance, _kingdomBorderDistanceThreshold);
			string distanceBand = courtDistance == float.MaxValue
				? "王庭间距离无法确认"
				: courtDistance <= distanceScale * 2f
					? "距离较近但不接壤"
					: courtDistance <= distanceScale * 4f
						? "距离中等且不接壤"
						: "相距遥远且不接壤";
			lines.Add(id + "=" + KingdomName(target) + "；" + distanceBand + "；不得称为邻国，不得声称拥有共同边境或边界争端");
		}
		return lines.Count == 0
			? "【当前地理关系】无可核实对象。"
			: "【当前地理关系；仅标为接壤的国家才可称邻国】\n" + string.Join("\n", lines);
	}

	private WorldDiplomacyRealmRelationProfile GetRealmRelationProfile(Kingdom source, Kingdom target)
	{
		if (source == null || target == null) return new WorldDiplomacyRealmRelationProfile();
		string key = source.StringId + ">" + target.StringId + ":" + CurrentDay().ToString(CultureInfo.InvariantCulture);
		if (_realmRelationProfileCache.TryGetValue(key, out WorldDiplomacyRealmRelationProfile cached)) return cached;
		List<Clan> sourceClans = source.Clans.Where(x => x != null && !x.IsEliminated)
			.OrderByDescending(x => x == source.RulingClan).ThenByDescending(x => x.Tier).ThenByDescending(x => x.Influence).Take(8).ToList();
		List<Clan> targetClans = target.Clans.Where(x => x != null && !x.IsEliminated)
			.OrderByDescending(x => x == target.RulingClan).ThenByDescending(x => x.Tier).ThenByDescending(x => x.Influence).Take(8).ToList();
		double weightedSum = 0d;
		double weightSum = 0d;
		double positiveWeight = 0d;
		double hostileWeight = 0d;
		List<(double Value, double Weight)> values = new List<(double, double)>();
		foreach (Clan first in sourceClans)
		{
			foreach (Clan second in targetClans)
			{
				int relation;
				try { relation = FactionManager.GetRelationBetweenClans(first, second); }
				catch { relation = 0; }
				double weight = Math.Sqrt(Math.Max(1d, 1d + first.Tier * 0.5d + first.Fiefs.Count * 0.25d)
					* Math.Max(1d, 1d + second.Tier * 0.5d + second.Fiefs.Count * 0.25d));
				weightedSum += relation * weight;
				weightSum += weight;
				if (relation >= 10) positiveWeight += weight;
				if (relation <= -10) hostileWeight += weight;
				values.Add((relation, weight));
			}
		}
		float average = weightSum <= 0d ? GetRulerRelation(source, target) : (float)(weightedSum / weightSum);
		double variance = weightSum <= 0d ? 0d : values.Sum(x => x.Weight * Math.Pow(x.Value - average, 2d)) / weightSum;
		WorldDiplomacyRealmRelationProfile profile = new WorldDiplomacyRealmRelationProfile
		{
			AverageRelation = average,
			PositiveRatio = weightSum <= 0d ? 0f : (float)(positiveWeight / weightSum),
			HostileRatio = weightSum <= 0d ? 0f : (float)(hostileWeight / weightSum),
			Polarization = (float)Math.Sqrt(Math.Max(0d, variance)),
			RulerRelation = GetRulerRelation(source, target),
			SamplePairCount = values.Count
		};
		profile.RulerEliteGap = profile.RulerRelation - profile.AverageRelation;
		_realmRelationProfileCache[key] = profile;
		return profile;
	}

	private static string DescribeRealmRelationProfile(WorldDiplomacyRealmRelationProfile profile)
	{
		if (profile == null || profile.SamplePairCount == 0) return "缺少可靠往来记录";
		string baseAttitude = profile.AverageRelation >= 25f ? "普遍亲近" : profile.AverageRelation >= 8f ? "大体友善"
			: profile.AverageRelation <= -25f ? "普遍敌视" : profile.AverageRelation <= -8f ? "积怨较深" : "总体谨慎";
		if (profile.Polarization >= 28f) baseAttitude += "但国内贵族意见分裂";
		if (profile.RulerEliteGap >= 25f) baseAttitude += "，统治者比本国贵族更亲近对方";
		else if (profile.RulerEliteGap <= -25f) baseAttitude += "，统治者比本国贵族更敌视对方";
		return baseAttitude;
	}

	private static string InferTopicCategory(string topic, Kingdom initiator, Kingdom target)
	{
		string value = topic ?? "";
		if (value.IndexOf("议和", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("停战", StringComparison.OrdinalIgnoreCase) >= 0) return "peace_terms";
		if (ContainsAny(value, "战争", "宣战", "开战", "最后通牒", "谴责", "军事警告", "战争警告", "动武")) return "war_escalation";
		if (ContainsAny(value, "贸易", "通商", "商路", "商贸", "互市", "关税", "商队")) return "trade_order";
		if (value.IndexOf("同盟", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("盟约", StringComparison.OrdinalIgnoreCase) >= 0) return "alliance_duties";
		if (initiator != null && target != null && FactionManager.IsAtWarAgainstFaction(initiator, target)) return "war_conduct";
		return "regional_security";
	}

	private static string BuildRelayGenerationSystemPrompt(string commonContract)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract);
	}

	private string BuildRelayConversationTurnPrompt(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom previous,
		WorldDiplomacyDocument prioritySource = null,
		bool priorityResponseOnly = false)
	{
		PruneInvalidOffers(round);
		StringBuilder sb = new StringBuilder();
		List<string> legalTargetIds = round?.ResultSettlementPending == true
			? GetResultSettlementActionableTargets(round, author).Select(x => x.StringId).ToList()
			: (round?.RelayRouteKingdomIds ?? new List<string>())
				.Where(x => !string.Equals(x, author?.StringId, StringComparison.OrdinalIgnoreCase)).ToList();
		Dictionary<string, List<string>> legalActionsByTarget = BuildLegalDiplomaticDeclarationIntentMap(
			round,
			author,
			legalTargetIds,
			isRelayTurn: true,
			resultSettlementSlotId: round?.ResultSettlementCurrentSlotId,
			isExternalResponseOnly: priorityResponseOnly,
			responseSource: prioritySource);
		legalTargetIds = legalActionsByTarget.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		List<string> exclusivePeaceResponseTargetIds = legalActionsByTarget
			.Where(x => IsExclusivePeaceOfferResponseSet(x.Value))
			.Select(x => x.Key)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
		sb.AppendLine("【本次外交公文动态状态】");
		sb.AppendLine("长期档案中的宣言是已颁布公文，不是君主即时聊天。为当前王国另行起草一份可独立传阅的正式公文；可以进行有意义的谈判往来，但不得重述历史发言或无条件重复立场。机械外交行为才计入回合行动进展，谈判发言不会无限延长回合。");
		sb.AppendLine("议题=" + (round.RoundTopic ?? ""));
		sb.AppendLine("公文送达与发布顺序=" + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
		sb.AppendLine("本篇发布国=" + author.StringId + "=" + KingdomName(author) + "，授权统治者=" + RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			string priorityActionFact = BuildSourceActionFactForTarget(prioritySource, author.StringId);
			sb.AppendLine("【本篇优先任务：回应玩家王国宣言】");
			sb.AppendLine("玩家王国的下列宣言已经送达本国王庭并直接指向本国，本篇必须正面回应它，而不是沿原定公文次序改谈其他国家：来源="
				+ prioritySource.DocumentId + "|发文国=" + prioritySource.AuthorKingdomId + "|标题=" + prioritySource.Title
				+ (string.IsNullOrWhiteSpace(priorityActionFact) ? "" : "|与本国相关动作=" + priorityActionFact));
			sb.AppendLine("必须选择当前可选动作。若玩家发出谴责或最后通牒，只有无条件退让才使用comply_ultimatum；任何其他实际动作都按不退让结算且威慑来源字段留空。");
		}
		AppendDiplomaticAuthorDecisionContext(sb, author, round.RoundId);
		AppendOtherKingdomRelationshipContext(sb, author, legalTargetIds);
		sb.AppendLine("最近送抵本国王庭的公文来源=" + (previous?.StringId ?? "") + "=" + KingdomName(previous));
		sb.AppendLine("送件国只是最近来文来源，不是程序指定对象；本国必须从下方允许动作对象中选择。");
		sb.AppendLine("允许动作对象=" + string.Join(",", legalTargetIds));
		sb.AppendLine(BuildCurrentLegalDiplomaticOptions(legalActionsByTarget));
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			round,
			author,
			round.ResultSettlementCurrentSlotId,
			priorityResponseOnly,
			prioritySource?.DocumentId,
			requireAnyOpenPeaceOffer: true);
		if (exclusivePeaceResponseTargetIds.Count > 0)
		{
			sb.AppendLine("和平原案答复：对象=" + string.Join(",", exclusivePeaceResponseTargetIds)
				+ "；只能选择accept_peace原样接受，或reject_peace明确拒绝；不得附加、修改条款或另提和平方案。");
		}
		if (HasCessionBoundMultiplePeaceAcceptanceOptions(round, author, legalActionsByTarget))
		{
			sb.AppendLine("多份和平原案中含割地：本篇最多接受一份；其他原案可拒绝或留待下一篇。");
		}
		if (requiredPeaceOffer != null)
		{
			sb.AppendLine("本篇必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
				+ "|提出国=" + requiredPeaceOffer.ProposerKingdomId + "。");
		}
		sb.AppendLine("若当前可选动作含statement，它表示一项结构化谈判动作而非机械外交行为，必须填写negotiation_move并在正文中实际完成该动作；公文仍会沿原路线送交下一国。不得用空泛立场冒充新进展。");
		AppendRelayResponseSourceContext(
			sb,
			round,
			author,
			prioritySource,
			requiredPeaceOffer?.SourceDocumentId);
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase);
			sb.AppendLine("待回应提议=" + offer.Intent + "|提出国=" + offer.ProposerKingdomId + "|对象国=" + offer.TargetKingdomId + "|来源=" + offer.SourceDocumentId
				+ (canAnswer ? "|答复资格=本国可以接受或拒绝" : "|答复资格=本国不是对象国，不得接受或拒绝；只能另提新案或改选其他合法动作"));
		}
		int age = Math.Max(0, CurrentDay() - round.StartedDay);
		int targetDays = Math.Max(1, round.SoftEndDay - round.StartedDay);
		int remainingDays = Math.Max(0, targetDays - age);
		sb.AppendLine("本次交涉已经进行=" + age.ToString(CultureInfo.InvariantCulture) + "天；预计时长=" + targetDays.ToString(CultureInfo.InvariantCulture)
			+ "天；距离预计收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前公文往来阶段=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
		AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays);
		AppendOpenOfferAnswerRequirement(sb, round, author);
		if (age * 100 >= targetDays * 85) sb.AppendLine("当前已进入最后阶段：必须选择能够收束局面的当前可选动作。");
		else if (age * 100 >= targetDays * 70) sb.AppendLine("当前已进入回合后段：优先收束分歧并形成明确结果。");
		if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext))
		{
			sb.AppendLine("【本次外交事件已知的外部动向】");
			sb.AppendLine(Limit(round.ExternalOpeningContext, 1800));
		}
		string gatheringContext = NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(round.RelayRouteKingdomIds, 3);
		if (!string.IsNullOrWhiteSpace(gatheringContext))
		{
			sb.AppendLine("【近期相关宴会】");
			sb.AppendLine(Limit(gatheringContext, 900));
			sb.AppendLine("宴会只是当前可利用或评论的公开动向，不预设赞扬、嘲讽或敌意，也不自动产生外交结果。");
		}
		List<string> peaceProposalTargetIds = new List<string>();
		foreach (string id in legalTargetIds)
		{
			Kingdom other = ResolveKingdom(id);
			if (other == null || other == author) continue;
			if (!legalActionsByTarget.TryGetValue(id, out List<string> targetActions)) continue;
			bool includePeaceNegotiationTerms = targetActions.Any(x => string.Equals(
				NormalizeIntent(x),
				"propose_peace",
				StringComparison.OrdinalIgnoreCase));
			AppendDiplomaticTargetDecisionContext(
				sb,
				round,
				author,
				other,
				includePeaceNegotiationTerms,
				targetActions);
			if (includePeaceNegotiationTerms)
			{
				peaceProposalTargetIds.Add(id);
			}
		}
		if (peaceProposalTargetIds.Count > 0)
		{
			sb.AppendLine("当前可提出和平方案的对象="
				+ string.Join(",", peaceProposalTargetIds) + "。");
		}
		return sb.ToString().TrimEnd();
	}

	private static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays)
	{
		if (sb == null || round == null) return;
		sb.AppendLine("公开事件只提供已经发生的背景，不预定结果。本国可对一个或多个对象各选一项当前可选动作；生成动作不等于机制已经执行成功。");
		sb.AppendLine("已经形成的明确外交尝试=" + Math.Max(0, round.SubstantiveProgressCount).ToString(CultureInfo.InvariantCulture)
			+ "次；其中指向关系变更的尝试=" + Math.Max(0, round.DiplomaticActionAttemptCount).ToString(CultureInfo.InvariantCulture)
			+ "次；已经正式生效的外交行动=" + Math.Max(0, round.ExecutedActionCount).ToString(CultureInfo.InvariantCulture) + "次。");
		sb.AppendLine("连续未出现机械外交行为的完整往来阶段=" + Math.Max(0, round.ConsecutiveNoActionPasses).ToString(CultureInfo.InvariantCulture) + "。");
		if (round.ConsecutiveNoActionPasses >= 2 || round.FinalActionOpportunityIssued)
		{
			sb.AppendLine("本篇处于最终解决阶段：必须选择最终提案、接受、拒绝、让步、谴责、最后通牒或其他当前可用机械外交行为；若确实无意继续，只可用statement并将negotiation_move设为end_negotiation或declare_deadlock。不得再输出普通讨论、拖延或重复立场，也不得虚构已经生效的结果。");
		}
		else if (round.ConsecutiveNoActionPasses == 1)
		{
			sb.AppendLine("上一完整往来阶段没有触发机械外交行为。本篇若继续谈判，必须提出新的条件、回答具体问题、给出部分让步、修订条款、设定期限或明确反提案；不得只换一种说法重复原立场。statement只能单独使用。");
		}
		else
		{
			sb.AppendLine("本次交涉仍允许一轮不触发机制的实质谈判。可询问、澄清、陈述理由、回应关切或提出条件，也可直接选择当前可用机械外交行为。");
		}
	}

	private static void AppendOpenOfferAnswerRequirement(StringBuilder sb, WorldDiplomacyRound round, Kingdom author)
	{
		if (sb == null || round == null || author == null) return;
		bool hasAnswerableOffer = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
		if (!hasAnswerableOffer) return;
		sb.AppendLine("本国有尚未答复的正式提议；必须按当前可选动作处理。accept_*只表示无条件接受全部原条件并立即生效。");
	}

	private int FindPriorityThreatRelayIndex(WorldDiplomacyRound round, List<string> route)
	{
		if (round == null || route == null || route.Count < 2) return -1;
		string currentKingdomId = round.RelayCursor >= 0 && round.RelayCursor < route.Count
			? route[round.RelayCursor]
			: "";
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(IsOpenDiplomaticThreat)
			.OrderBy(x => x.UpdatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase))
		{
			string requiredSpeakerId = string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
				? threat.IssuerKingdomId
				: string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
					? threat.TargetKingdomId
					: "";
			if (string.IsNullOrWhiteSpace(requiredSpeakerId)
				|| string.Equals(requiredSpeakerId, currentKingdomId, StringComparison.OrdinalIgnoreCase)) continue;
			int index = route.FindIndex(x => string.Equals(x, requiredSpeakerId, StringComparison.OrdinalIgnoreCase));
			if (index >= 0 && HasIndependentWorldDiplomacyAuthority(ResolveKingdom(requiredSpeakerId))) return index;
		}
		return -1;
	}

	private void ScheduleNextRelayHop(WorldDiplomacyRound round)
	{
		if (round?.ResultSettlementPending == true)
		{
			ScheduleNextResultSettlementTurn(round);
			return;
		}
		if (round == null || !round.RelayPlanned || round.RelayWaiting || round.AutomaticCircuitBreakerTripped
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		if (_storage.RelayArrivals.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			|| _storage.Jobs.Any(x => x != null && x.IsRelayTurn && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))) return;
		List<string> route = round.RelayRouteKingdomIds ?? new List<string>();
		if (route.Count < 2)
		{
			CloseActiveRound("relay_has_no_participants");
			return;
		}
		// Old saves can lose route members when kingdoms are eliminated or become controlled
		// vassals. Never trust the persisted cursor/direction after such a route rewrite.
		if (round.RelayCursor < 0 || round.RelayCursor >= route.Count) round.RelayCursor = 0;
		if (round.RelayDirection != -1 && round.RelayDirection != 1) round.RelayDirection = 1;
		int passDurationDays = round.RelayPassDurationDays > 0 ? round.RelayPassDurationDays : RelayPassDurationDays;
		int nextIndex = FindPriorityThreatRelayIndex(round, route);
		if (nextIndex < 0) nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection);
		if (nextIndex < 0)
		{
			CompleteRelayPassProgressAccounting(round);
			round.RelayDirection = round.RelayDirection >= 0 ? -1 : 1;
			round.RelayPassNumber++;
			round.RelayPassStartedDay += passDurationDays;
			if (round.ConsecutiveNoActionPasses >= 2 && !round.FinalActionOpportunityIssued)
			{
				round.FinalActionOpportunityIssued = true;
				Log("relay final resolution phase opened after consecutive no-action passes round=" + round.RoundId);
			}
			nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection);
		}
		if (nextIndex < 0)
		{
			CloseActiveRound("relay_all_participants_withdrew");
			return;
		}
		int edgeCount = Math.Max(1, route.Count - 1);
		int progress = round.RelayDirection > 0 ? nextIndex : route.Count - 1 - nextIndex;
		int plannedDay = round.RelayPassStartedDay + (int)Math.Ceiling(passDurationDays * Math.Max(1, progress) / (double)edgeCount);
		if (round.FinalActionOpportunityIssued && round.SubstantiveProgressCount <= 0)
		{
			plannedDay = Math.Min(plannedDay, round.HardEndDay);
		}
		round.RelaySequence++;
		round.RelayWaiting = true;
		_storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival
		{
			RoundId = round.RoundId,
			FromKingdomId = route[round.RelayCursor],
			ToKingdomId = route[nextIndex],
			DueDay = plannedDay,
			Sequence = round.RelaySequence
		});
		_storage.RelayArrivals = _storage.RelayArrivals.OrderBy(x => x.DueDay).ThenBy(x => x.Sequence).ToList();
	}

	private void CompleteRelayPassProgressAccounting(WorldDiplomacyRound round)
	{
		if (round == null || round.RelayPassNumber <= 0
			|| round.LastAccountedRelayPassNumber >= round.RelayPassNumber) return;
		bool actionOccurred = round.DiplomaticActionAttemptCount > round.ActionAttemptCountAtPassStart;
		round.ConsecutiveNoActionPasses = actionOccurred
			? 0
			: Math.Min(3, round.ConsecutiveNoActionPasses + 1);
		round.ActionAttemptCountAtPassStart = round.DiplomaticActionAttemptCount;
		round.LastAccountedRelayPassNumber = round.RelayPassNumber;
		Log("relay pass progress accounted round=" + round.RoundId
			+ " pass=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture)
			+ " action=" + actionOccurred
			+ " consecutive_no_action=" + round.ConsecutiveNoActionPasses.ToString(CultureInfo.InvariantCulture));
	}

	private static bool HasOpenRoundOffers(WorldDiplomacyRound round)
	{
		return round?.PendingOffers?.Any(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)) == true;
	}

	private int FindNextRelayIndex(WorldDiplomacyRound round, int start)
	{
		List<string> route = round?.RelayRouteKingdomIds ?? new List<string>();
		for (int index = start; index >= 0 && index < route.Count; index += round.RelayDirection)
		{
			if (!HasIndependentWorldDiplomacyAuthority(ResolveKingdom(route[index])))
			{
				continue;
			}
			WorldDiplomacyRoundParticipant participant = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
				.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, route[index], StringComparison.OrdinalIgnoreCase));
			if (participant == null || !string.Equals(participant.State, "withdrawn", StringComparison.OrdinalIgnoreCase)) return index;
		}
		return -1;
	}

	private void ProcessRelayArrivals()
	{
		List<WorldDiplomacyRelayArrival> due = (_storage.RelayArrivals ?? new List<WorldDiplomacyRelayArrival>())
			.Where(x => x != null && x.DueDay <= CurrentDay()).OrderBy(x => x.DueDay).ThenBy(x => x.Sequence).Take(8).ToList();
		foreach (WorldDiplomacyRelayArrival arrival in due)
		{
			_storage.RelayArrivals.Remove(arrival);
			WorldDiplomacyRound round = ResolveRound(arrival.RoundId);
			if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase) || arrival.Sequence != round.RelaySequence) continue;
			if (round.ResultSettlementPending)
			{
				WorldDiplomacyResultSettlementSlot settlementSlot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
					.FirstOrDefault(x => x != null
						&& string.Equals(x.SlotId, arrival.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.KingdomId, arrival.ToKingdomId, StringComparison.OrdinalIgnoreCase));
				Kingdom settlementReceiver = ResolveKingdom(arrival.ToKingdomId);
				if (settlementSlot == null
					|| !string.Equals(round.ResultSettlementCurrentSlotId, arrival.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase))
				{
					round.RelayWaiting = false;
					ScheduleNextResultSettlementTurn(round);
					continue;
				}
				if (settlementReceiver == null || !HasIndependentWorldDiplomacyAuthority(settlementReceiver))
				{
					SkipResultSettlementSlot(round, settlementSlot.SlotId, settlementSlot.KingdomId, "receiver_ineligible");
					ScheduleNextResultSettlementTurn(round);
					continue;
				}
				List<WorldDiplomacyDocument> settlementRoundDocuments = _storage.Documents
					.Where(x => x != null && x.IsReadyForPublication
						&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(x => x.Day)
					.ThenByDescending(x => x.CreatedUtcTicks)
					.ToList();
				foreach (WorldDiplomacyDocument known in settlementRoundDocuments)
				{
					RecordKingdomKnowledge(settlementReceiver.StringId, known.DocumentId, CurrentDay());
					RecordNobleKnowledge(settlementReceiver.StringId, known.DocumentId, CurrentDay());
					MarkPlayerCourtReachedByRelay(settlementReceiver, known);
				}
				settlementSlot.Status = "inflight";
				WorldDiplomacyDocument settlementSource = settlementRoundDocuments.FirstOrDefault();
				EnqueueGenerationJob(settlementReceiver, ResolveKingdom(arrival.FromKingdomId), null, isResponse: true,
					sourceDocument: settlementSource, priority: 90, roundId: round.RoundId, allowUntargeted: true,
					isRelayTurn: true, previousKingdomId: arrival.FromKingdomId, scheduledDay: arrival.DueDay,
					resultSettlementSlotId: settlementSlot.SlotId);
				continue;
			}
			int index = (round.RelayRouteKingdomIds ?? new List<string>()).FindIndex(x => string.Equals(x, arrival.ToKingdomId, StringComparison.OrdinalIgnoreCase));
			Kingdom receiver = ResolveKingdom(arrival.ToKingdomId);
			Kingdom previous = ResolveKingdom(arrival.FromKingdomId);
			if (index < 0 || receiver == null || !HasIndependentWorldDiplomacyAuthority(receiver))
			{
				round.RelayWaiting = false;
				AdvanceRelay(round);
				continue;
			}
			round.RelayCursor = index;
			List<WorldDiplomacyDocument> relayRoundDocuments = _storage.Documents
				.Where(x => x != null && x.IsReadyForPublication
					&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => x.Day)
				.ThenByDescending(x => x.CreatedUtcTicks)
				.ToList();
			foreach (WorldDiplomacyDocument document in relayRoundDocuments)
			{
				RecordKingdomKnowledge(receiver.StringId, document.DocumentId, CurrentDay());
				RecordNobleKnowledge(receiver.StringId, document.DocumentId, CurrentDay());
				MarkPlayerCourtReachedByRelay(receiver, document);
			}
			if (IsPlayerKingdom(receiver))
			{
				RecordPlayerOpportunity(round, receiver);
				round.RelayWaiting = false;
				AdvanceRelay(round);
				continue;
			}
			WorldDiplomacyDocument source = relayRoundDocuments.FirstOrDefault();
			EnqueueGenerationJob(receiver, previous ?? ResolveKingdom(round.InitiatorKingdomId), null, isResponse: true,
				sourceDocument: source, priority: 75, roundId: round.RoundId, allowUntargeted: true,
				isRelayTurn: true, previousKingdomId: arrival.FromKingdomId, scheduledDay: arrival.DueDay);
		}
	}

	private void MarkPlayerCourtReachedByRelay(Kingdom receiver, WorldDiplomacyDocument document)
	{
		if (receiver == null || document == null || document.IsPlayerAuthored
			|| document.HasReachedPlayerCourt || !IsPlayerAffiliatedKingdom(receiver)) return;
		if (string.Equals(receiver.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			document.HasReachedPlayerCourt = true;
		}
		else
		{
			ProcessCourtArrival(receiver, document);
		}
		Log("formal-player-relay.received document=" + document.DocumentId
			+ " receiver=" + receiver.StringId
			+ " day=" + CurrentDay().ToString(CultureInfo.InvariantCulture));
	}

	private void RecoverPlayerCourtReceiptsFromKnowledge()
	{
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (playerKingdom == null || _storage?.Documents == null) return;
		WorldDiplomacyKingdomKnowledge knowledge = (_storage.KingdomKnowledge ?? new List<WorldDiplomacyKingdomKnowledge>())
			.FirstOrDefault(x => x != null
				&& string.Equals(x.KingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase));
		if (knowledge?.DocumentIds == null || knowledge.DocumentIds.Count == 0) return;
		HashSet<string> knownDocumentIds = new HashSet<string>(knowledge.DocumentIds, StringComparer.OrdinalIgnoreCase);
		int recovered = 0;
		foreach (WorldDiplomacyDocument document in _storage.Documents)
		{
			if (document == null || document.IsPlayerAuthored || !document.IsReadyForPublication
				|| document.HasReachedPlayerCourt || document.FormalNoticeShown
				|| !knownDocumentIds.Contains(document.DocumentId ?? "")) continue;
			document.HasReachedPlayerCourt = true;
			recovered++;
		}
		if (recovered > 0)
		{
			Log("formal-player-receipts.recovered kingdom=" + playerKingdom.StringId
				+ " documents=" + recovered.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void AdvanceRelay(WorldDiplomacyRound round)
	{
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		if (round.ResultSettlementPending)
		{
			round.RelayWaiting = false;
			ScheduleNextResultSettlementTurn(round);
			return;
		}
		if (CurrentDay() >= round.HardEndDay)
		{
			CloseActiveRound("relay_hard_end");
			return;
		}
		round.RelayWaiting = false;
		ScheduleNextRelayHop(round);
	}

	private void RecordPlayerOpportunity(WorldDiplomacyRound round, Kingdom playerKingdom)
	{
		if (round == null || playerKingdom == null) return;
		WorldDiplomacyPlayerOpportunity opportunity = _storage.PlayerOpportunities.FirstOrDefault(x => x != null
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		if (opportunity == null)
		{
			opportunity = new WorldDiplomacyPlayerOpportunity { RoundId = round.RoundId, ArrivedDay = CurrentDay(), Status = "open" };
			_storage.PlayerOpportunities.Add(opportunity);
		}
		opportunity.ArrivedDay = CurrentDay();
		opportunity.Status = "open";
		opportunity.KnownDocumentIds = _storage.Documents.Where(x => x != null && x.IsReadyForPublication && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.DocumentId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void IntegratePlayerDeclaration(WorldDiplomacyRound round, WorldDiplomacyDocument document)
	{
		if (round == null || document == null) return;
		WorldDiplomacyRoundParticipant playerParticipant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
		if (playerParticipant != null)
		{
			playerParticipant.IsPlayerAsync = true;
			playerParticipant.LastSpokeDay = CurrentDay();
			playerParticipant.SelectedForRelay = round.ResultSettlementPending
				? RoundRouteContainsKingdom(round, document.AuthorKingdomId)
				: AddParticipantToRelayRouteIfNeeded(round, document.AuthorKingdomId);
		}
		WorldDiplomacyPlayerOpportunity opportunity = _storage.PlayerOpportunities.FirstOrDefault(x => x != null
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase));
		if (opportunity != null) opportunity.Status = "answered";
		foreach (string id in (document.AddressedKingdomIds ?? new List<string>())
			.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
			if (kingdom == null || (round.ResultSettlementPending && !RoundRouteContainsKingdom(round, kingdom.StringId))) continue;
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdom.StringId, "active", mandatoryReply: false);
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
			participant.SelectedForRelay = round.ResultSettlementPending
				? true
				: AddParticipantToRelayRouteIfNeeded(round, kingdom.StringId);
		}
		Log("player declaration appended to relay round=" + round.RoundId + " document=" + document.DocumentId);
	}

	private static bool RoundContainsKingdom(WorldDiplomacyRound round, string kingdomId)
	{
		return round?.Participants?.Any(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)) == true;
	}

	private static bool RoundRouteContainsKingdom(WorldDiplomacyRound round, string kingdomId)
	{
		return !string.IsNullOrWhiteSpace(kingdomId)
			&& round?.RelayRouteKingdomIds?.Contains(kingdomId, StringComparer.OrdinalIgnoreCase) == true;
	}

	private static WorldDiplomacyRoundParticipant EnsureRoundParticipant(WorldDiplomacyRound round, string kingdomId, string state, bool mandatoryReply)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId))
		{
			return null;
		}
		round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
		WorldDiplomacyRoundParticipant participant = round.Participants.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (participant == null)
		{
			participant = new WorldDiplomacyRoundParticipant { KingdomId = kingdomId, State = state ?? "observer" };
			round.Participants.Add(participant);
		}
		else if (!string.Equals(participant.State, "withdrawn", StringComparison.OrdinalIgnoreCase)
			|| mandatoryReply
			|| string.Equals(state, "active", StringComparison.OrdinalIgnoreCase))
		{
			participant.State = FirstNonEmpty(state, participant.State, "observer");
		}
		participant.MandatoryReplyPending |= mandatoryReply;
		return participant;
	}

	private static bool AddParticipantToRelayRouteIfNeeded(WorldDiplomacyRound round, string kingdomId)
	{
		if (round == null || !round.RelayPlanned || string.IsNullOrWhiteSpace(kingdomId)) return false;
		round.RelayRouteKingdomIds ??= new List<string>();
		if (round.RelayRouteKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase)) return true;
		if (round.RelayRouteKingdomIds.Count >= GetRoundParticipantLimit()) return false;
		round.RelayRouteKingdomIds.Add(kingdomId);
		return true;
	}

	private void ResetDailyGenerationBudget()
	{
		int day = CurrentDay();
		if (_aiDocumentsStartedDay != day)
		{
			_aiDocumentsStartedDay = day;
			_aiDocumentsStartedToday = 0;
		}
	}

	private bool TryConsumeAiDocumentBudget()
	{
		ResetDailyGenerationBudget();
		if (_aiDocumentsStartedToday >= MaxAiDocumentsStartedPerDay)
		{
			return false;
		}
		_aiDocumentsStartedToday++;
		return true;
	}

	private bool TryConsumeDiplomacyLlmRequestBudget()
	{
		int day = CurrentDay();
		if (_llmRequestsStartedDay != day)
		{
			_llmRequestsStartedDay = day;
			_llmRequestsStartedToday = 0;
		}
		if (_llmRequestsStartedToday >= MaxDiplomacyLlmRequestsPerDay)
		{
			if (_lastLlmBudgetLogDay != day)
			{
				_lastLlmBudgetLogDay = day;
				Log("llm daily throughput reached day=" + day.ToString(CultureInfo.InvariantCulture)
					+ " limit=" + MaxDiplomacyLlmRequestsPerDay.ToString(CultureInfo.InvariantCulture)
					+ " action=defer_pending_jobs");
			}
			return false;
		}
		_llmRequestsStartedToday++;
		return true;
	}

	private void StartDocumentPropagation(WorldDiplomacyDocument document, Kingdom author)
	{
		if (document == null || document.PropagationCompleted || author == null)
		{
			return;
		}
		if (!document.IsPlayerAuthored && !CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			SuppressInvalidDocumentBeforePropagation(document, authorBlockReason);
			return;
		}
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null
			&& !string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase))
		{
			round = EnsureActiveRound(author, ResolveKingdom(document.TargetKingdomId), document.IsPlayerAuthored);
			document.RoundId = round?.RoundId ?? "";
			document.ExchangeId = document.RoundId;
		}
		Settlement origin = ResolveCourtSettlement(author);
		document.OriginSettlementId = origin?.StringId ?? "";
		if (!document.IsPlayerAuthored && IsPlayerAffiliatedKingdom(author))
		{
			document.HasReachedPlayerCourt = true;
		}
		document.PropagationStarted = true;
		document.IsReadyForPublication = true;
		if (round != null)
		{
			round.RootDocumentId = FirstNonEmpty(round.RootDocumentId, document.DocumentId);
			round.LastActivityDay = CurrentDay();
			WorldDiplomacyRoundParticipant authorParticipant = EnsureRoundParticipant(round, author.StringId, "active", mandatoryReply: false);
			authorParticipant.SelectedForRelay = true;
			authorParticipant.IsPlayerAsync = IsPlayerKingdom(author);
			AddParticipantToRelayRouteIfNeeded(round, author.StringId);
			authorParticipant.LastSpokeDay = CurrentDay();
			if (document.IsResponse)
			{
				authorParticipant.MandatoryReplyPending = false;
				authorParticipant.LastTriggeredDocumentId = document.SourceDocumentId ?? "";
			}
		}
		RecordSettlementKnowledge(origin?.StringId, document.DocumentId, CurrentDay());
		RecordKingdomKnowledge(author.StringId, document.DocumentId, CurrentDay());
		RecordNobleKnowledge(author.StringId, document.DocumentId, CurrentDay());
		RecordDiplomacyWeeklyMaterial(document);
		List<Settlement> settlements = Settlement.All
			.Where(x => x != null && !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		float maxCivilianDistance = origin == null || settlements.Count == 0
			? 0f
			: settlements.Max(x => origin.GatePosition.Distance(x.GatePosition));
		int civilianSpreadDays = GetCivilianSpreadDays();
		int courtDeliveryDays = GetCourtMaxDeliveryDays();
		int latestCivilianDueDay = CurrentDay();
		List<WorldDiplomacyPropagationArrival> newArrivals = new List<WorldDiplomacyPropagationArrival>(settlements.Count + Math.Max(0, Kingdom.All.Count - 1));
		HashSet<string> knownSettlementIds = GetKnownSettlementIdsForDocument(document.DocumentId);
		HashSet<string> knownKingdomIds = GetKnownKingdomIdsForDocument(document.DocumentId);
		foreach (Settlement settlement in settlements)
		{
			if (origin != null && settlement == origin)
			{
				continue;
			}
			if (knownSettlementIds.Contains(settlement.StringId)) continue;
			float distance = origin == null ? maxCivilianDistance : origin.GatePosition.Distance(settlement.GatePosition);
			int travelDays = maxCivilianDistance <= 0.01f
				? 1
				: CalculatePropagationDays(distance, maxCivilianDistance, civilianSpreadDays);
			latestCivilianDueDay = Math.Max(latestCivilianDueDay, CurrentDay() + travelDays);
			newArrivals.Add(new WorldDiplomacyPropagationArrival
			{
				DocumentId = document.DocumentId,
				RoundId = document.RoundId,
				SettlementId = settlement.StringId,
				Scope = "civilian",
				DueDay = CurrentDay() + travelDays
			});
		}
		List<Tuple<Kingdom, Settlement>> courtDestinations = Kingdom.All
			.Where(x => x != null && !x.IsEliminated && x != author && !string.IsNullOrWhiteSpace(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => Tuple.Create(x, ResolveCourtSettlement(x)))
			.ToList();
		float maxCourtDistance = origin == null
			? 0f
			: courtDestinations.Where(x => x.Item2 != null).Select(x => origin.GatePosition.Distance(x.Item2.GatePosition)).DefaultIfEmpty(0f).Max();
		int latestCourtDueDay = CurrentDay();
		foreach (Tuple<Kingdom, Settlement> destination in courtDestinations)
		{
			bool playerCourtReceiptMissing = IsPlayerAffiliatedKingdom(destination.Item1)
				&& !document.HasReachedPlayerCourt;
			if (knownKingdomIds.Contains(destination.Item1.StringId) && !playerCourtReceiptMissing) continue;
			float distance = origin == null || destination.Item2 == null
				? maxCourtDistance
				: origin.GatePosition.Distance(destination.Item2.GatePosition);
			int travelDays = maxCourtDistance <= 0.01f
				? courtDeliveryDays
				: CalculatePropagationDays(distance, maxCourtDistance, courtDeliveryDays);
			latestCourtDueDay = Math.Max(latestCourtDueDay, CurrentDay() + travelDays);
			newArrivals.Add(new WorldDiplomacyPropagationArrival
			{
				DocumentId = document.DocumentId,
				RoundId = document.RoundId,
				SettlementId = destination.Item2?.StringId ?? "",
				KingdomId = destination.Item1.StringId,
				Scope = "court",
				DueDay = CurrentDay() + travelDays
			});
		}
		List<WorldDiplomacyPropagationArrival> committedArrivals = (_storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
			.Where(x => x != null && !string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
			.Concat(newArrivals)
			.OrderBy(x => x.DueDay)
			.ThenBy(x => IsCourtArrival(x) ? 0 : 1)
			.ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		_storage.PropagationArrivals = committedArrivals;
		document.PropagationCompleted = true;
		Log("propagation started document=" + document.DocumentId
			+ " round=" + document.RoundId
			+ " origin=" + (origin?.StringId ?? "none")
			+ " settlements=" + settlements.Count.ToString(CultureInfo.InvariantCulture)
			+ " civilianDays=" + civilianSpreadDays.ToString(CultureInfo.InvariantCulture)
			+ " latestCivilianDay=" + latestCivilianDueDay.ToString(CultureInfo.InvariantCulture)
			+ " courts=" + courtDestinations.Count.ToString(CultureInfo.InvariantCulture)
			+ " courtDays=" + courtDeliveryDays.ToString(CultureInfo.InvariantCulture)
			+ " latestCourtDay=" + latestCourtDueDay.ToString(CultureInfo.InvariantCulture)
			+ " addressed=" + string.Join(",", document.AddressedKingdomIds ?? new List<string>()));
	}

	private void RetryDeferredDocumentPropagation()
	{
		foreach (WorldDiplomacyDocument document in (_storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication && !x.PropagationCompleted)
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).Take(8))
		{
			Kingdom author = ResolveKingdom(document.AuthorKingdomId);
			if (author == null) continue;
			try
			{
				StartDocumentPropagation(document, author);
			}
			catch (Exception ex)
			{
				Log("deferred propagation retry failed document=" + document.DocumentId + " error=" + ex.Message);
			}
		}
	}

	private bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document)
	{
		if (document == null || !document.PropagationStarted) return false;
		HashSet<string> pendingSettlements = new HashSet<string>((_storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
			.Where(x => x != null && !IsCourtArrival(x)
				&& string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.SettlementId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		HashSet<string> pendingKingdoms = new HashSet<string>((_storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
			.Where(x => x != null && IsCourtArrival(x)
				&& string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.KingdomId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		HashSet<string> knownSettlementIds = GetKnownSettlementIdsForDocument(document.DocumentId);
		HashSet<string> knownKingdomIds = GetKnownKingdomIdsForDocument(document.DocumentId);
		foreach (Settlement settlement in Settlement.All.Where(x => x != null && !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId)))
		{
			if (string.Equals(settlement.StringId, document.OriginSettlementId, StringComparison.OrdinalIgnoreCase)) continue;
			if (!pendingSettlements.Contains(settlement.StringId) && !knownSettlementIds.Contains(settlement.StringId)) return false;
		}
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated
			&& !string.Equals(x.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)))
		{
			if (!pendingKingdoms.Contains(kingdom.StringId) && !knownKingdomIds.Contains(kingdom.StringId)) return false;
		}
		return true;
	}

	private void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument document)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
		{
			return;
		}
		int day = Math.Max(0, document.Day);
		string roundKey = FirstNonEmpty(document.RoundId, document.DocumentId);
		List<WorldDiplomacyDocument> sameDay = _storage.Documents
			.Where(item => item != null && item.IsReadyForPublication && item.Day == day
				&& string.Equals(FirstNonEmpty(item.RoundId, item.DocumentId), roundKey, StringComparison.OrdinalIgnoreCase))
			.OrderBy(item => item.CreatedUtcTicks)
			.Take(6)
			.ToList();
		if (!sameDay.Any(item => string.Equals(item.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)))
		{
			sameDay.Add(document);
		}
		StringBuilder snapshot = new StringBuilder();
		snapshot.Append("外交回合").Append(roundKey).Append("在本日出现以下公开进展：");
		foreach (WorldDiplomacyDocument item in sameDay.Take(6))
		{
			snapshot.Append(" ").Append(FirstNonEmpty(item.AuthorRulerName, item.AuthorKingdomName)).Append("发布《")
				.Append(Limit(item.Title, 80)).Append("》");
			if (!string.IsNullOrWhiteSpace(item.Body))
			{
				snapshot.Append("，核心主张：").Append(Limit(NormalizeBody(item.Body), 180));
			}
			if (item.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(item.MechanicalResult))
			{
				snapshot.Append("；[游戏已执行] ").Append(Limit(item.MechanicalResult, 120));
			}
			snapshot.Append("。");
		}
		snapshot.Append("尚未标注[游戏已执行]的内容只是公开主张、提案、接受或拒绝，不得写成已经完成的外交结果。");

		List<string> relatedKingdomIds = sameDay
			.SelectMany(item => new[] { item.AuthorKingdomId, item.TargetKingdomId }
				.Concat(item.AddressedKingdomIds ?? new List<string>()))
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		string stableBase = "world_diplomacy:" + roundKey + ":day:" + day.ToString(CultureInfo.InvariantCulture);
		string authorKingdomId = (document.AuthorKingdomId ?? "").Trim();
		MyBehavior.RecordWorldDiplomacyWeeklyMaterialForExternal(
			stableBase + ":world",
			"外交宣言进展 - " + Limit(FirstNonEmpty(document.Title, document.AuthorKingdomName), 80),
			snapshot.ToString(),
			authorKingdomId,
			document.AuthorRulerId ?? "",
			authorKingdomId,
			includeInWorld: true,
			day,
			document.GameDate ?? "");
		foreach (string kingdomId in relatedKingdomIds.Where(id => !string.Equals(id, authorKingdomId, StringComparison.OrdinalIgnoreCase)))
		{
			MyBehavior.RecordWorldDiplomacyWeeklyMaterialForExternal(
				stableBase + ":kingdom:" + kingdomId,
				"与本国有关的外交宣言进展",
				snapshot.ToString(),
				kingdomId,
				document.AuthorRulerId ?? "",
				authorKingdomId,
				includeInWorld: false,
				day,
				document.GameDate ?? "");
		}
	}

	private static int CalculatePropagationDays(float distance, float maximumDistance, int maximumDays)
	{
		if (maximumDistance <= 0.01f) return Math.Max(1, maximumDays);
		return Math.Max(1, Math.Min(maximumDays, (int)Math.Ceiling(distance / maximumDistance * maximumDays)));
	}

	private static bool IsCourtArrival(WorldDiplomacyPropagationArrival arrival)
	{
		return string.Equals(arrival?.Scope, "court", StringComparison.OrdinalIgnoreCase);
	}

	private Settlement ResolveCourtSettlement(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return null;
		}
		if (_courtSettlementCache.TryGetValue(kingdom.StringId ?? "", out string cachedId))
		{
			return ResolveSettlementById(cachedId);
		}
		Clan rulingClan = kingdom.RulingClan;
		IEnumerable<Settlement> forts = kingdom.Fiefs.Select(x => x?.Settlement).Where(x => x != null && (x.IsTown || x.IsCastle));
		Settlement court = forts
			.Where(x => x.OwnerClan == rulingClan)
			.OrderByDescending(GetSettlementProsperity)
			.ThenBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault()
			?? forts.OrderByDescending(GetSettlementProsperity).ThenBy(x => x.StringId, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
		_courtSettlementCache[kingdom.StringId ?? ""] = court?.StringId ?? "";
		return court;
	}

	private static float GetSettlementProsperity(Settlement settlement)
	{
		return settlement?.Town?.Prosperity ?? 0f;
	}

	private void ProcessPropagationArrivals()
	{
		int day = CurrentDay();
		List<WorldDiplomacyPropagationArrival> due = _storage.PropagationArrivals
			.TakeWhile(x => x != null && x.DueDay <= day)
			.Take(MaxPropagationArrivalsPerDay)
			.ToList();
		if (due.Count > 0) _storage.PropagationArrivals.RemoveRange(0, due.Count);
		foreach (WorldDiplomacyPropagationArrival arrival in due)
		{
			WorldDiplomacyDocument document = ResolveDocument(arrival.DocumentId);
			if (document == null)
			{
				continue;
			}
			if (IsCourtArrival(arrival))
			{
				Kingdom receiver = ResolveKingdom(arrival.KingdomId) ?? ResolveSettlementById(arrival.SettlementId)?.OwnerClan?.Kingdom;
				if (receiver != null)
				{
					RecordNobleKnowledge(receiver.StringId, document.DocumentId, day);
					bool newlyKnown = RecordKingdomKnowledge(receiver.StringId, document.DocumentId, day);
					if (newlyKnown || (IsPlayerAffiliatedKingdom(receiver) && !document.HasReachedPlayerCourt))
					{
						ProcessCourtArrival(receiver, document);
					}
				}
				continue;
			}
			Settlement settlement = ResolveSettlementById(arrival.SettlementId);
			if (settlement != null) RecordSettlementKnowledge(settlement.StringId, document.DocumentId, day);
		}
	}

	private void RecalculatePendingPropagationIfNeeded()
	{
		int courtDays = GetCourtMaxDeliveryDays();
		int civilianDays = GetCivilianSpreadDays();
		if (_storage.LastAppliedCourtDeliveryDays == courtDays
			&& _storage.LastAppliedCivilianSpreadDays == civilianDays)
		{
			return;
		}
		List<Settlement> settlements = Settlement.All.Where(x => x != null && !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId)).ToList();
		List<Tuple<Kingdom, Settlement>> courts = Kingdom.All
			.Where(x => x != null && !x.IsEliminated && !string.IsNullOrWhiteSpace(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => Tuple.Create(x, ResolveCourtSettlement(x)))
			.ToList();
		List<string> pendingDocumentIds = _storage.PropagationArrivals
			.Where(x => x != null)
			.Select(x => x.DocumentId)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		foreach (string documentId in pendingDocumentIds)
		{
			WorldDiplomacyDocument document = ResolveDocument(documentId);
			Settlement origin = ResolveSettlementById(document?.OriginSettlementId);
			if (document == null || origin == null) continue;
			float maxCourtDistance = courts.Where(x => x.Item2 != null).Select(x => origin.GatePosition.Distance(x.Item2.GatePosition)).DefaultIfEmpty(0f).Max();
			foreach (Tuple<Kingdom, Settlement> court in courts)
			{
				bool playerCourtReceiptMissing = IsPlayerAffiliatedKingdom(court.Item1)
					&& !document.HasReachedPlayerCourt;
				if (string.Equals(court.Item1.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
					|| (HasKingdomKnowledge(court.Item1.StringId, document.DocumentId) && !playerCourtReceiptMissing)
					|| _storage.PropagationArrivals.Any(x => x != null && IsCourtArrival(x)
						&& string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.KingdomId, court.Item1.StringId, StringComparison.OrdinalIgnoreCase))) continue;
				float distance = court.Item2 == null ? maxCourtDistance : origin.GatePosition.Distance(court.Item2.GatePosition);
				int travelDays = maxCourtDistance <= 0.01f ? courtDays : CalculatePropagationDays(distance, maxCourtDistance, courtDays);
				_storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
				{
					DocumentId = document.DocumentId,
					RoundId = document.RoundId,
					SettlementId = court.Item2?.StringId ?? "",
					KingdomId = court.Item1.StringId,
					Scope = "court",
					DueDay = Math.Max(CurrentDay(), document.Day + travelDays)
				});
			}
		}
		foreach (IGrouping<string, WorldDiplomacyPropagationArrival> group in _storage.PropagationArrivals.Where(x => x != null).GroupBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase))
		{
			WorldDiplomacyDocument document = ResolveDocument(group.Key);
			Settlement origin = ResolveSettlementById(document?.OriginSettlementId);
			if (document == null || origin == null) continue;
			float maxCivilianDistance = settlements.Count == 0 ? 0f : settlements.Max(x => origin.GatePosition.Distance(x.GatePosition));
			float maxCourtDistance = courts.Where(x => x.Item2 != null).Select(x => origin.GatePosition.Distance(x.Item2.GatePosition)).DefaultIfEmpty(0f).Max();
			foreach (WorldDiplomacyPropagationArrival arrival in group)
			{
				Settlement destination = ResolveSettlementById(arrival.SettlementId);
				float maximumDistance = IsCourtArrival(arrival) ? maxCourtDistance : maxCivilianDistance;
				int maximumDays = IsCourtArrival(arrival) ? courtDays : civilianDays;
				if (!IsCourtArrival(arrival) && destination == null) continue;
				float distance = destination == null ? maximumDistance : origin.GatePosition.Distance(destination.GatePosition);
				int travelDays = maximumDistance <= 0.01f ? maximumDays : CalculatePropagationDays(distance, maximumDistance, maximumDays);
				arrival.DueDay = Math.Max(CurrentDay(), document.Day + travelDays);
			}
		}
		_storage.PropagationArrivals = _storage.PropagationArrivals
			.OrderBy(x => x.DueDay)
			.ThenBy(x => IsCourtArrival(x) ? 0 : 1)
			.ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		_storage.LastAppliedContinentSpreadDays = civilianDays;
		_storage.LastAppliedCivilianSpreadDays = civilianDays;
		_storage.LastAppliedCourtDeliveryDays = courtDays;
		Log("pending propagation recalculated courtDays=" + courtDays.ToString(CultureInfo.InvariantCulture)
			+ " civilianDays=" + civilianDays.ToString(CultureInfo.InvariantCulture)
			+ " arrivals=" + _storage.PropagationArrivals.Count.ToString(CultureInfo.InvariantCulture));
	}

	private void SynchronizeCourtKnowledge()
	{
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated))
		{
			Settlement court = ResolveCourtSettlement(kingdom);
			WorldDiplomacySettlementKnowledge local = _storage.SettlementKnowledge.FirstOrDefault(x => x != null && string.Equals(x.SettlementId, court?.StringId, StringComparison.OrdinalIgnoreCase));
			foreach (string documentId in local?.DocumentIds ?? new List<string>())
			{
				WorldDiplomacyDocument document = ResolveDocument(documentId);
				if (document != null && RecordKingdomKnowledge(kingdom.StringId, documentId, CurrentDay())) ProcessCourtArrival(kingdom, document);
			}
		}
	}

	private void RecordSettlementKnowledge(string settlementId, string documentId, int day)
	{
		if (string.IsNullOrWhiteSpace(settlementId) || string.IsNullOrWhiteSpace(documentId)) return;
		WorldDiplomacySettlementKnowledge knowledge = _storage.SettlementKnowledge.FirstOrDefault(x => x != null && string.Equals(x.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacySettlementKnowledge { SettlementId = settlementId };
			_storage.SettlementKnowledge.Add(knowledge);
		}
		if (!knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation);
		knowledge.LastUpdatedDay = day;
	}

	private bool RecordKingdomKnowledge(string kingdomId, string documentId, int day)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(documentId)) return false;
		WorldDiplomacyKingdomKnowledge knowledge = _storage.KingdomKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacyKingdomKnowledge { KingdomId = kingdomId };
			_storage.KingdomKnowledge.Add(knowledge);
		}
		if (knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) return false;
		knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation * 2) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation * 2);
		knowledge.LastUpdatedDay = day;
		return true;
	}

	private void RecordNobleKnowledge(string kingdomId, string documentId, int day)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(documentId)) return;
		WorldDiplomacyKingdomKnowledge knowledge = _storage.NobleKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (knowledge == null)
		{
			knowledge = new WorldDiplomacyKingdomKnowledge { KingdomId = kingdomId };
			_storage.NobleKnowledge.Add(knowledge);
		}
		if (!knowledge.DocumentIds.Contains(documentId, StringComparer.OrdinalIgnoreCase)) knowledge.DocumentIds.Add(documentId);
		if (knowledge.DocumentIds.Count > MaxKnownDocumentsPerLocation * 2) knowledge.DocumentIds.RemoveRange(0, knowledge.DocumentIds.Count - MaxKnownDocumentsPerLocation * 2);
		knowledge.LastUpdatedDay = day;
	}

	private void ProcessCourtArrival(Kingdom receiver, WorldDiplomacyDocument document)
	{
		if (receiver == null || document == null || string.Equals(receiver.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)) return;
		bool directlyAddressed = (document.AddressedKingdomIds ?? new List<string>()).Contains(receiver.StringId, StringComparer.OrdinalIgnoreCase)
			|| string.Equals(document.TargetKingdomId, receiver.StringId, StringComparison.OrdinalIgnoreCase)
			|| IsDiplomaticRepresentativeForAddressedVassal(receiver, document);
		if (IsPlayerAffiliatedKingdom(receiver))
		{
			document.HasReachedPlayerCourt = true;
		}
		if (document.IsPlayerAuthored && HasIndependentWorldDiplomacyAuthority(receiver))
		{
			WorldDiplomacyRound round = ResolveRound(document.RoundId);
			bool activeDelivery = round != null && ReferenceEquals(_storage.ActiveRound, round)
				&& string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase);
			if (!activeDelivery) return;
			InformationManager.DisplayMessage(new InformationMessage("你的宣言已传播至" + KingdomName(receiver) + "。"));
			bool isPrimaryTarget = string.Equals(document.TargetKingdomId, receiver.StringId, StringComparison.OrdinalIgnoreCase);
			if (directlyAddressed && (isPrimaryTarget || DocumentRequiresResponseFrom(document, receiver.StringId)))
			{
				WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, receiver.StringId, "active", mandatoryReply: true);
				TryScheduleMandatoryCourtResponse(round, participant, receiver, document);
			}
		}
		Log("court received document=" + document.DocumentId + " receiver=" + receiver.StringId + " direct=" + directlyAddressed + " day=" + CurrentDay().ToString(CultureInfo.InvariantCulture));
	}

	private static bool DocumentRequiresResponseFrom(WorldDiplomacyDocument document, string kingdomId)
	{
		if (document == null || string.IsNullOrWhiteSpace(kingdomId)) return false;
		if (document.Actions?.Count > 0)
		{
			return document.Actions.Any(x => x != null && x.RequiresResponse
				&& string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		}
		return document.RequiresResponse;
	}

	private static List<string> GetDocumentTargetIds(WorldDiplomacyDocument document, bool changedOnly = false)
	{
		if (document == null) return new List<string>();
		if (document.Actions?.Count > 0)
		{
			return document.Actions
				.Where(x => x != null && (!changedOnly || x.ChangedDiplomaticState))
				.Select(x => x.TargetKingdomId)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		IEnumerable<string> targets = string.IsNullOrWhiteSpace(document.TargetKingdomId)
			? Enumerable.Empty<string>()
			: new[] { document.TargetKingdomId };
		if (!changedOnly) targets = targets.Concat(document.AddressedKingdomIds ?? new List<string>());
		return targets.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static bool IsDiplomaticRepresentativeForAddressedVassal(Kingdom receiver, WorldDiplomacyDocument document)
	{
		if (receiver == null || document == null)
		{
			return false;
		}
		IEnumerable<string> addressedIds = (document.AddressedKingdomIds ?? new List<string>())
			.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId)
				? Enumerable.Empty<string>()
				: new[] { document.TargetKingdomId })
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase);
		foreach (string addressedId in addressedIds)
		{
			Kingdom addressed = ResolveKingdom(addressedId);
			if (addressed != null
				&& addressed != receiver
				&& ResolveWorldDiplomacyRepresentative(addressed) == receiver)
			{
				return true;
			}
		}
		return false;
	}

	private void TryScheduleMandatoryCourtResponse(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, Kingdom receiver, WorldDiplomacyDocument trigger)
	{
		bool isPrimaryTarget = trigger != null
			&& string.Equals(trigger.TargetKingdomId, receiver?.StringId, StringComparison.OrdinalIgnoreCase);
		if (round == null || participant == null || receiver == null || trigger == null || IsPlayerKingdom(receiver)
			|| !HasIndependentWorldDiplomacyAuthority(receiver)
			|| !trigger.IsPlayerAuthored || (!isPrimaryTarget && !IsDiplomaticRepresentativeForAddressedVassal(receiver, trigger)
				&& !DocumentRequiresResponseFrom(trigger, receiver.StringId)))
		{
			if (participant != null) participant.MandatoryReplyPending = false;
			return;
		}
		if (HasKingdomRespondedToDocument(receiver.StringId, trigger.DocumentId))
		{
			participant.MandatoryReplyPending = false;
			return;
		}
		if (!CanAiAuthorDiplomaticDocument(receiver, out string authorBlockReason))
		{
			participant.MandatoryReplyPending = false;
			participant.State = "observer";
			Log("mandatory response blocked by author authority round=" + round.RoundId
				+ " author=" + receiver.StringId + " reason=" + authorBlockReason);
			if (string.Equals(authorBlockReason, "ruler_is_prisoner", StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage(
					KingdomName(receiver) + "的统治者目前身陷囹圄，王庭暂时无法正式回应你的宣言。"));
			}
			return;
		}
		if (round.ResultSettlementPending)
		{
			// The settlement queue owns every remaining speaking right. Scheduling the
			// older priority-response path here would create a job without a slot id.
			participant.MandatoryReplyPending = false;
			return;
		}
		if (_storage.Jobs.Any(x => x != null && string.Equals(x.AuthorKingdomId, receiver.StringId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.SourceDocumentId, trigger.DocumentId, StringComparison.OrdinalIgnoreCase))) return;
		int existingResponses = _storage.Documents.Count(x => x != null && x.IsReadyForPublication
			&& string.Equals(x.SourceDocumentId, trigger.DocumentId, StringComparison.OrdinalIgnoreCase));
		int queuedResponses = _storage.Jobs.Count(x => x != null && string.Equals(x.SourceDocumentId, trigger.DocumentId, StringComparison.OrdinalIgnoreCase));
		if (existingResponses + queuedResponses >= MaxPriorityPlayerResponsesPerDocument)
		{
			participant.MandatoryReplyPending = false;
			return;
		}
		Kingdom target = ResolveKingdom(trigger.AuthorKingdomId);
		bool reuseRelayTranscript = round.RelayPlanned;
		participant.LastTriggeredDocumentId = trigger.DocumentId;
		EnqueueGenerationJob(receiver, target, null, isResponse: true, sourceDocument: trigger,
			priority: 95, externalResponseOnly: true, roundId: round.RoundId, isRelayTurn: reuseRelayTranscript,
			previousKingdomId: trigger.AuthorKingdomId, scheduledDay: CurrentDay());
		Log("mandatory response queued round=" + round.RoundId + " author=" + receiver.StringId + " target=" + (target?.StringId ?? "") + " source=" + trigger.DocumentId);
	}

	private bool HasKingdomRespondedToDocument(string kingdomId, string documentId)
	{
		return _storage.Documents.Any(x => x != null
			&& string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
			&& DocumentRespondsTo(x, documentId));
	}

	private static bool DocumentRespondsTo(WorldDiplomacyDocument document, string sourceDocumentId)
	{
		if (document == null || string.IsNullOrWhiteSpace(sourceDocumentId)) return false;
		if (string.Equals(document.SourceDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(document.RespondingToOfferDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(document.RespondingToThreatDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)) return true;
		return document.Actions?.Any(x => x != null
			&& (string.Equals(x.RespondingToOfferDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.RespondingToThreatDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase))) == true;
	}

	private void ProcessRoundLifecycle()
	{
		WorldDiplomacyRound round = _storage.ActiveRound;
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		if (round.AutomaticCircuitBreakerTripped)
		{
			bool hasRunningRoundJob = _storage.Jobs.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
			if (!hasRunningRoundJob)
			{
				if (round.ResultSettlementPending)
				{
					round.ResultSettlementSlots?.Clear();
					round.RoundStatus = string.Equals(round.ResultSettlementRoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
						? "deadlocked" : "resolved";
					CloseActiveRound("result_settlement_circuit_breaker");
				}
				else CloseActiveRound("automatic_request_circuit_breaker");
			}
			return;
		}
		bool pendingRoundJob = _storage.Jobs.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		int day = CurrentDay();
		if (day >= round.HardEndDay)
		{
			if (pendingRoundJob)
			{
				// Game time may continue while the background request is running. Let the
				// already-started final turn finish instead of closing the round underneath it.
				return;
			}
			if (round.ResultSettlementPending)
			{
				round.ResultSettlementSlots?.Clear();
				round.RoundStatus = string.Equals(round.ResultSettlementRoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
					? "deadlocked" : "resolved";
				CloseActiveRound("result_settlement_hard_end");
			}
			else CloseActiveRound("relay_hard_end");
			return;
		}
		if (!round.RelayPlanned)
		{
			WorldDiplomacyDocument root = ResolveDocument(round.RootDocumentId);
			if (root != null && root.IsReadyForPublication) EnqueueRoundPlanJob(round, root);
			return;
		}
		if (round.ResultSettlementPending)
		{
			WorldDiplomacyResultSettlementSlot currentSlot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase));
			if (currentSlot != null && string.Equals(currentSlot.Status, "waiting_player", StringComparison.OrdinalIgnoreCase)
				&& round.ResultSettlementPlayerWaitingSinceDay > 0
				&& day >= round.ResultSettlementPlayerWaitingSinceDay + 5)
			{
				SkipResultSettlementSlot(round, currentSlot.SlotId, currentSlot.KingdomId, "player_timeout");
				currentSlot = null;
			}
			if (!pendingRoundJob && !_storage.RelayArrivals.Any(x => x != null
				&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
			{
				round.RelayWaiting = currentSlot != null
					&& string.Equals(currentSlot.Status, "waiting_player", StringComparison.OrdinalIgnoreCase);
				if (!round.RelayWaiting) ScheduleNextResultSettlementTurn(round);
			}
			return;
		}
		int activeAi = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Count(x => x != null
			&& x.SelectedForRelay && !x.IsPlayerAsync && !string.Equals(x.State, "withdrawn", StringComparison.OrdinalIgnoreCase));
		if (activeAi <= 0)
		{
			CloseActiveRound("relay_all_ai_withdrew");
			return;
		}
		if (!pendingRoundJob && !_storage.RelayArrivals.Any(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
		{
			round.RelayWaiting = false;
			ScheduleNextRelayHop(round);
		}
	}

	private void TripAutomaticRoundCircuitBreaker(WorldDiplomacyRound round, string reason)
	{
		if (round == null || round.AutomaticCircuitBreakerTripped)
		{
			return;
		}
		round.AutomaticCircuitBreakerTripped = true;
		foreach (WorldDiplomacyRoundParticipant participant in round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
		{
			if (participant == null) continue;
			participant.MandatoryReplyPending = false;
			participant.MandatorySinceDay = 0;
		}
		_storage.PendingParticipationEvaluations.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.PendingSpeeches.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.RelayArrivals.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		foreach (WorldDiplomacyPlayerOpportunity opportunity in _storage.PlayerOpportunities.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
		{
			if (string.Equals(opportunity.Status, "open", StringComparison.OrdinalIgnoreCase)) opportunity.Status = "expired";
		}
		Log("round circuit breaker tripped round=" + round.RoundId
			+ " documents=" + round.AutomaticDocumentsStarted.ToString(CultureInfo.InvariantCulture)
			+ " reason=" + (reason ?? ""));
	}

	private void ProcessPlayerMandatoryResponseTimeout(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant)
	{
		if (round == null || participant == null || participant.MandatorySinceDay <= 0) return;
		int day = CurrentDay();
		WorldDiplomacyDocument source = ResolveDocument(participant.LastTriggeredDocumentId);
		if (!participant.ReminderSent && day >= participant.MandatorySinceDay + 3 && source != null && TryConsumeAiDocumentBudget())
		{
			Kingdom author = ResolveKingdom(source.AuthorKingdomId);
			Kingdom player = ResolveKingdom(participant.KingdomId);
			if (author != null && player != null)
			{
				participant.ReminderSent = true;
				EnqueueGenerationJob(author, player, null, isResponse: true, sourceDocument: source, priority: 80, externalResponseOnly: true, isReminder: true, roundId: round.RoundId);
			}
		}
		if (day >= participant.MandatorySinceDay + 5)
		{
			participant.MandatoryReplyPending = false;
			participant.State = "observer";
			round.LastActivityDay = day;
		}
	}

	private bool HasUndeliveredCourtArrivals(string roundId)
	{
		return _storage.PropagationArrivals.Any(x => x != null
			&& IsCourtArrival(x)
			&& string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase));
	}

	private void CloseActiveRound(string reason)
	{
		WorldDiplomacyRound round = _storage.ActiveRound;
		if (round == null) return;
		round.State = "closed";
		round.CompletedDay = CurrentDay();
		round.CloseReason = reason ?? "";
		if ((round.CloseReason ?? "").StartsWith("technical_", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(round.CloseReason, "automatic_request_circuit_breaker", StringComparison.OrdinalIgnoreCase))
		{
			round.RoundStatus = "aborted";
		}
		else if (string.Equals(round.RoundStatus, "active", StringComparison.OrdinalIgnoreCase))
		{
			round.RoundStatus = round.ExecutedActionCount > 0
				? "resolved"
				: round.DiplomaticActionAttemptCount > 0 ? "deadlocked" : "closed";
		}
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase))) offer.Status = "expired";
		SettleTradeAllianceOfferCooldownsForClosedRound(round);
		List<WorldDiplomacyDocument> documents = _storage.Documents.Where(x => x != null && x.IsReadyForPublication
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
		SettleDiplomaticThreatObligationsForClosedRound(round, documents);
		round.FinalDocumentId = documents.LastOrDefault()?.DocumentId ?? "";
		_storage.PendingParticipationEvaluations.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.PendingSpeeches.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.RelayArrivals.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
		foreach (WorldDiplomacyPlayerOpportunity opportunity in _storage.PlayerOpportunities.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
		{
			if (string.Equals(opportunity.Status, "open", StringComparison.OrdinalIgnoreCase)) opportunity.Status = "expired";
		}
		_storage.CompletedRounds.Add(round);
		_storage.ActiveRound = null;
		ScheduleNextNormalRoundAfter(CurrentDay());
		if (documents.Count > 0) CommitLocalRoundSummary(round, documents);
		round.CommonContractSnapshot = "";
		round.CommonContractSnapshotInitialized = false;
		Log("round closed round=" + round.RoundId
			+ " reason=" + round.CloseReason
			+ " documents=" + documents.Count.ToString(CultureInfo.InvariantCulture)
			+ " substantiveProgress=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture)
			+ " diplomaticActionAttempts=" + round.DiplomaticActionAttemptCount.ToString(CultureInfo.InvariantCulture)
			+ " executedActions=" + round.ExecutedActionCount.ToString(CultureInfo.InvariantCulture));
		TryScheduleTokenCompression();
	}

	private void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
	{
		if (round == null || documents == null || documents.Count == 0) return;
		List<WorldDiplomacyDocument> ordered = documents.Where(x => x != null).OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
		List<string> kingdomIds = ordered
			.SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }.Concat(x.AddressedKingdomIds ?? new List<string>()))
			.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		WorldDiplomacyRoundSummary summary = new WorldDiplomacyRoundSummary
		{
			ArchiveSchemaVersion = 1,
			RoundId = round.RoundId ?? "",
			CreatedDay = round.CompletedDay > 0 ? round.CompletedDay : CurrentDay(),
			SourceDocumentIds = ordered.Select(x => x.DocumentId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
			KingdomIds = kingdomIds,
			Summary = BuildLocalRoundSummaryText(round, ordered)
		};
		foreach (WorldDiplomacyDocument document in ordered.Take(48))
		{
			List<string> declarationKingdomIds = new[] { document.AuthorKingdomId }
				.Concat(GetDocumentTargetIds(document))
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			summary.Facts.Add(new WorldDiplomacyRoundFact
			{
				Kind = "declaration",
				Text = "[宣言记录] " + BuildCompactDocumentMemoryLine(document),
				SourceDocumentIds = new List<string> { document.DocumentId },
				KingdomIds = declarationKingdomIds
			});
			if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult))
			{
				summary.Facts.Add(new WorldDiplomacyRoundFact
				{
					Kind = "confirmed_result",
					Text = "[游戏已执行] " + document.MechanicalResult,
					SourceDocumentIds = new List<string> { document.DocumentId },
					KingdomIds = new[] { document.AuthorKingdomId }.Concat(GetDocumentTargetIds(document, changedOnly: true))
						.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
				});
			}
		}
		_storage.RoundSummaries.RemoveAll(x => x != null && string.Equals(x.RoundId, summary.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.RoundSummaries.Add(summary);
		Log("local round archive committed round=" + summary.RoundId
			+ " declarations=" + ordered.Count.ToString(CultureInfo.InvariantCulture)
			+ " confirmed_results=" + summary.Facts.Count(x => string.Equals(x.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture));
	}

	private void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary)
	{
		if (summary == null || summary.ArchiveSchemaVersion >= 1) return;
		WorldDiplomacyRound round = ResolveRound(summary.RoundId);
		List<WorldDiplomacyDocument> documents = _storage.Documents.Where(x => x != null
			&& (string.Equals(x.RoundId, summary.RoundId, StringComparison.OrdinalIgnoreCase)
				|| (summary.SourceDocumentIds ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase)))
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
		if (round == null || documents.Count == 0)
		{
			summary.ArchiveSchemaVersion = 1;
			summary.Summary = "旧版外交摘要，仅表示当时保存的宣言叙述，不能据此认定任何外交机制已经执行：" + Limit(summary.Summary, 1200);
			summary.Facts = (summary.Facts ?? new List<WorldDiplomacyRoundFact>()).Where(x => x != null).Select(x => new WorldDiplomacyRoundFact
			{
				Kind = "declaration",
				Text = "[旧版宣言摘要，不代表游戏已执行] " + Limit(x.Text, 360),
				SourceDocumentIds = x.SourceDocumentIds ?? new List<string>(),
				KingdomIds = x.KingdomIds ?? new List<string>()
			}).ToList();
			return;
		}
		summary.Summary = BuildLocalRoundSummaryText(round, documents);
		summary.SourceDocumentIds = documents.Select(x => x.DocumentId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		summary.KingdomIds = documents.SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }.Concat(x.AddressedKingdomIds ?? new List<string>()))
			.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		summary.Facts = new List<WorldDiplomacyRoundFact>();
		foreach (WorldDiplomacyDocument document in documents.Take(48))
		{
			summary.Facts.Add(new WorldDiplomacyRoundFact
			{
				Kind = "declaration", Text = "[宣言记录] " + BuildCompactDocumentMemoryLine(document),
				SourceDocumentIds = new List<string> { document.DocumentId },
				KingdomIds = new[] { document.AuthorKingdomId }.Concat(GetDocumentTargetIds(document))
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			});
			if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult)) summary.Facts.Add(new WorldDiplomacyRoundFact
			{
				Kind = "confirmed_result", Text = "[游戏已执行] " + document.MechanicalResult,
				SourceDocumentIds = new List<string> { document.DocumentId },
				KingdomIds = new[] { document.AuthorKingdomId }.Concat(GetDocumentTargetIds(document, changedOnly: true))
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			});
		}
		summary.ArchiveSchemaVersion = 1;
	}

	private static string BuildLocalRoundSummaryText(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
	{
		List<string> declarations = (documents ?? new List<WorldDiplomacyDocument>()).Where(x => x != null)
			.Take(12).Select(BuildCompactDocumentMemoryLine).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		List<string> results = (documents ?? new List<WorldDiplomacyDocument>()).Where(x => x?.ChangedDiplomaticState == true && !string.IsNullOrWhiteSpace(x.MechanicalResult))
			.Select(x => x.MechanicalResult.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
		StringBuilder sb = new StringBuilder();
		sb.Append("议题：").Append(FirstNonEmpty(round?.RoundTopic, documents?.FirstOrDefault()?.Title, "外交交涉"));
		if (declarations.Count > 0) sb.Append("。宣言经过：").Append(string.Join("；", declarations));
		sb.Append(results.Count > 0 ? "。游戏确认结果：" + string.Join("；", results) : "。游戏确认结果：没有正式外交机制发生");
		sb.Append("。结束原因：").Append(FirstNonEmpty(round?.CloseReason, "事件结束"));
		return Limit(sb.ToString(), 2400);
	}

	private static string BuildRoundCompressionSystemPrompt()
	{
		return "你是卡拉迪亚外交编年史官。将一个已经自然结束的外交事件压缩为全局编年摘要与可按来源公文过滤的原子事实。不得编造。\n"
			+ "只输出JSON：{\"summary\":\"事件摘要\",\"facts\":[{\"text\":\"原子事实\",\"source_document_ids\":[\"公文ID\"],\"kingdom_ids\":[\"相关王国ID\"]}]}";
	}

	private string BuildRoundCompressionPrompt(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
	{
		StringBuilder sb = new StringBuilder();
		foreach (WorldDiplomacyDocument document in documents.Take(120)) sb.AppendLine("[" + document.DocumentId + "] " + BuildCompactDocumentMemoryLine(document));
		return sb.ToString();
	}

	private void CommitRoundCompression(WorldDiplomacyJob job, string raw)
	{
		JObject json = ParseJsonObject(raw);
		WorldDiplomacyRoundSummary summary = new WorldDiplomacyRoundSummary
		{
			RoundId = job.RoundId ?? "", CreatedDay = CurrentDay(), Summary = NormalizeBody(ReadString(json, "summary")), SourceDocumentIds = job.CompressionDocumentIds ?? new List<string>()
		};
		if (string.IsNullOrWhiteSpace(summary.Summary)) summary.Summary = BuildFallbackRoundSummary(job.CompressionDocumentIds);
		if (json["facts"] is JArray facts)
		{
			foreach (JToken token in facts.Take(32))
			{
				summary.Facts.Add(new WorldDiplomacyRoundFact
				{
					Text = Limit(token?["text"]?.ToString(), 360),
					SourceDocumentIds = ReadTokenStringList(token?["source_document_ids"]),
					KingdomIds = ReadTokenStringList(token?["kingdom_ids"])
				});
			}
		}
		_storage.RoundSummaries.RemoveAll(x => x != null && string.Equals(x.RoundId, summary.RoundId, StringComparison.OrdinalIgnoreCase));
		_storage.RoundSummaries.Add(summary);
	}

	private string BuildFallbackRoundCompressionJson(WorldDiplomacyJob job)
	{
		return new JObject { ["summary"] = BuildFallbackRoundSummary(job.CompressionDocumentIds), ["facts"] = new JArray() }.ToString(Formatting.None);
	}

	private string BuildFallbackRoundSummary(List<string> ids)
	{
		return string.Join("；", _storage.Documents.Where(x => x != null && (ids ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase)).OrderBy(x => x.Day).Take(16).Select(BuildCompactDocumentMemoryLine));
	}

	private static List<string> ReadTokenStringList(JToken token)
	{
		return token is JArray array ? array.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>();
	}

	private bool HasKingdomKnowledge(string kingdomId, string documentId)
	{
		return _storage.KingdomKnowledge.Any(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase) && (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase));
	}

	private HashSet<string> GetKnownSettlementIdsForDocument(string documentId)
	{
		return new HashSet<string>((_storage.SettlementKnowledge ?? new List<WorldDiplomacySettlementKnowledge>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.SettlementId)
				&& (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase))
			.Select(x => x.SettlementId), StringComparer.OrdinalIgnoreCase);
	}

	private HashSet<string> GetKnownKingdomIdsForDocument(string documentId)
	{
		return new HashSet<string>((_storage.KingdomKnowledge ?? new List<WorldDiplomacyKingdomKnowledge>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId)
				&& (x.DocumentIds ?? new List<string>()).Contains(documentId, StringComparer.OrdinalIgnoreCase))
			.Select(x => x.KingdomId), StringComparer.OrdinalIgnoreCase);
	}

	private WorldDiplomacyRound ResolveRound(string roundId)
	{
		if (string.IsNullOrWhiteSpace(roundId)) return null;
		if (_storage.ActiveRound != null && string.Equals(_storage.ActiveRound.RoundId, roundId, StringComparison.OrdinalIgnoreCase)) return _storage.ActiveRound;
		return _storage.CompletedRounds.FirstOrDefault(x => x != null && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase));
	}

	private void TrySettleRelayOffer(WorldDiplomacyDocument document)
	{
		WorldDiplomacyRound round = ResolveRound(document?.RoundId);
		if (round == null || document == null) return;
		round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
		PruneInvalidOffers(round);
		string intent = NormalizeIntent(document.Intent);
		if (IsProposalIntent(intent) && !string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
			Kingdom proposalAuthor = ResolveKingdom(document.AuthorKingdomId);
			Kingdom proposalTarget = ResolveKingdom(document.TargetKingdomId);
			if (TryGetDiplomaticStateViolation(intent, proposalAuthor, proposalTarget, out string proposalBlockReason))
			{
				document.MechanicalResult = "提议未登记：" + proposalBlockReason;
				return;
			}
			// A proposal in the reverse direction is a counter-offer. Retire the superseded offer so
			// later speakers see one current proposal instead of two contradictory open offers.
			foreach (WorldDiplomacyRoundOffer countered in round.PendingOffers.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), intent, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)))
			{
				countered.Status = "countered";
			}
			foreach (WorldDiplomacyRoundOffer superseded in round.PendingOffers.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), intent, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase)))
			{
				superseded.Status = "superseded";
			}
			round.PendingOffers.RemoveAll(x => x != null
				&& string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.SourceActionId ?? "", document.ProcessingActionId ?? "", StringComparison.OrdinalIgnoreCase));
			round.PendingOffers.Add(new WorldDiplomacyRoundOffer
			{
				SourceDocumentId = document.DocumentId,
				SourceActionId = document.ProcessingActionId ?? "",
				ProposerKingdomId = document.AuthorKingdomId,
				TargetKingdomId = document.TargetKingdomId,
				Intent = intent,
				Status = "open",
				CreatedDay = document.Day
			});
			return;
		}
		string proposalIntent = intent switch
		{
			"accept_peace" or "reject_peace" => "propose_peace",
			"accept_alliance" or "reject_alliance" => "propose_alliance",
			"accept_trade" or "reject_trade" => "propose_trade",
			_ => ""
		};
		if (string.IsNullOrWhiteSpace(proposalIntent)) return;
		if (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			document.MechanicalResult = "答复未执行：缺少唯一来源提议";
			return;
		}
		List<WorldDiplomacyRoundOffer> matchingOffers = round.PendingOffers
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Intent, proposalIntent, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(document.TargetKingdomId) || string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
				&& string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(document.RespondingToOfferActionId)
					|| string.Equals(x.SourceActionId, document.RespondingToOfferActionId, StringComparison.OrdinalIgnoreCase)))
			.Take(2).ToList();
		if (matchingOffers.Count != 1)
		{
			document.MechanicalResult = "答复未执行：来源提议已关闭、失效或不唯一";
			return;
		}
		WorldDiplomacyRoundOffer resolvedOffer = matchingOffers[0];
		if (intent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase))
		{
			resolvedOffer.Status = "rejected";
			return;
		}
		WorldDiplomacyDocument source = ResolveDocument(resolvedOffer.SourceDocumentId);
		Kingdom proposer = ResolveKingdom(resolvedOffer.ProposerKingdomId);
		Kingdom target = ResolveKingdom(resolvedOffer.TargetKingdomId);
		if (source == null || proposer == null || target == null)
		{
			resolvedOffer.Status = "invalidated";
			document.MechanicalResult = "接受未执行：原提议或当事国已失效";
			return;
		}
		try
		{
			if (proposalIntent == "propose_peace")
			{
				if (!AreOfferedPeaceTermsCurrentlyExecutable(resolvedOffer, source, proposer, target))
				{
					resolvedOffer.Status = "invalidated";
					document.MechanicalResult = "接受未执行：和平原案条款已无法原样履行";
					return;
				}
				// Acceptance ratifies the source offer exactly.
				document.PeaceTerms = ClonePeaceTerms(ResolveOfferedPeaceTerms(source, resolvedOffer.SourceActionId));
				ExecuteMakePeace(proposer, target, document);
			}
			else if (proposalIntent == "propose_alliance") ExecuteAlliance(proposer, target, document);
			else if (proposalIntent == "propose_trade") ExecuteTradeAgreement(proposer, target, document);
		}
		catch (Exception ex)
		{
			if (HasProposalTakenEffect(proposalIntent, proposer, target))
			{
				document.ChangedDiplomaticState = true;
				document.MechanicalResult = ProposalSuccessResult(proposalIntent);
				resolvedOffer.Status = "accepted";
			}
			else
			{
				document.MechanicalResult = "接受未执行：" + Limit(ex.Message, 180);
				resolvedOffer.Status = "execution_failed";
			}
			Log("offer acceptance execution failed document=" + document.DocumentId + " offer=" + resolvedOffer.SourceDocumentId + " error=" + ex.Message);
			return;
		}
		resolvedOffer.Status = document.ChangedDiplomaticState
			? ((document.MechanicalResult ?? "").IndexOf("交割失败", StringComparison.OrdinalIgnoreCase) >= 0 ? "partially_executed" : "accepted")
			: "execution_failed";
	}

	private static bool HasProposalTakenEffect(string proposalIntent, Kingdom proposer, Kingdom target)
	{
		if (proposer == null || target == null) return false;
		return NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => !FactionManager.IsAtWarAgainstFaction(proposer, target),
			"propose_alliance" => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(proposer, target) == true,
			"propose_trade" => Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>() is ITradeAgreementsCampaignBehavior trade
				&& BannerlordApiCompat.HasTradeAgreement(trade, proposer, target),
			_ => false
		};
	}

	private static string ProposalSuccessResult(string proposalIntent)
	{
		return NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => "双方已达成和平",
			"propose_alliance" => "双方已缔结同盟",
			"propose_trade" => "双方已缔结贸易协定",
			_ => "外交关系已按接受结果生效"
		};
	}

	private void ExecuteImmediateIntent(Kingdom author, Kingdom target, string intent, WorldDiplomacyDocument document)
	{
		if (document != null && !document.IsPlayerAuthored
			&& !CanAiAuthorDiplomaticDocument(author, out string authorBlockReason))
		{
			document.MechanicalResult = "外交行动未执行：发文者当前没有有效的自主发文权限。";
			Log("AI diplomatic action blocked author=" + (author?.StringId ?? "") + " document=" + (document.DocumentId ?? "")
				+ " reason=" + authorBlockReason);
			return;
		}
		if (intent == "declare_war")
		{
			if (!CanDeclareWar(author, target, out string blockReason, IsEnforcingRejectedUltimatum(author, target)))
			{
				document.MechanicalResult = "宣战未执行：" + blockReason;
				return;
			}
			Exception actionError = null;
			try
			{
				RunDiplomaticAction("world_diplomacy_declare_war", () => DeclareWarAction.ApplyByKingdomDecision(author, target));
			}
			catch (Exception ex)
			{
				actionError = ex;
			}
			if (FactionManager.IsAtWarAgainstFaction(author, target))
			{
				document.MechanicalResult = "已宣战";
				document.ChangedDiplomaticState = true;
				ClearWarPressure(author.StringId, target.StringId);
				_storage.LastOffensiveWarDayByKingdom[author.StringId] = CurrentDay();
			}
			else
			{
				document.MechanicalResult = actionError == null
					? "宣战未执行：游戏状态未发生变化"
					: "宣战未执行：" + Limit(actionError.Message, 180);
			}
			if (actionError != null) Log("declare war action raised after live-state check author=" + author.StringId + " target=" + target.StringId + " error=" + actionError.Message);
			return;
		}
		if (intent == "break_alliance")
		{
			IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (alliance == null)
			{
				document.MechanicalResult = "解盟未执行：同盟系统不可用";
				return;
			}
			if (!alliance.IsAllyWithKingdom(author, target))
			{
				document.MechanicalResult = "解盟未执行：双方当前没有同盟";
				return;
			}
			Exception actionError = null;
			try
			{
				RunDiplomaticAction("world_diplomacy_break_alliance", () =>
					PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_break_alliance",
						author,
						target,
						() => alliance.EndAlliance(author, target)));
			}
			catch (Exception ex)
			{
				actionError = ex;
			}
			if (!alliance.IsAllyWithKingdom(author, target))
			{
				document.MechanicalResult = "已解除同盟";
				document.ChangedDiplomaticState = true;
			}
			else
			{
				document.MechanicalResult = actionError == null
					? "解盟未执行：游戏状态未发生变化"
					: "解盟未执行：" + Limit(actionError.Message, 180);
			}
			if (actionError != null) Log("break alliance action raised after live-state check author=" + author.StringId + " target=" + target.StringId + " error=" + actionError.Message);
			return;
		}
		if (intent == "cancel_trade")
		{
			ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (trade == null)
			{
				document.MechanicalResult = "终止贸易未执行：贸易系统不可用";
				return;
			}
			if (!BannerlordApiCompat.HasTradeAgreement(trade, author, target))
			{
				document.MechanicalResult = "终止贸易未执行：双方当前没有贸易协定";
				return;
			}
			Exception actionError = null;
			try
			{
				RunDiplomaticAction("world_diplomacy_cancel_trade", () => trade.EndTradeAgreement(author, target));
			}
			catch (Exception ex)
			{
				actionError = ex;
			}
			if (!BannerlordApiCompat.HasTradeAgreement(trade, author, target))
			{
				document.MechanicalResult = "已终止贸易协定";
				document.ChangedDiplomaticState = true;
			}
			else
			{
				document.MechanicalResult = actionError == null
					? "终止贸易未执行：游戏状态未发生变化"
					: "终止贸易未执行：" + Limit(actionError.Message, 180);
			}
			if (actionError != null) Log("cancel trade action raised after live-state check author=" + author.StringId + " target=" + target.StringId + " error=" + actionError.Message);
		}
	}

	private void ExecuteMakePeace(Kingdom initiator, Kingdom target, WorldDiplomacyDocument document)
	{
		if (!FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			if (document != null) document.MechanicalResult = "议和未执行：双方当前没有战争";
			return;
		}
		WorldDiplomacyPeaceTerms terms = document?.PeaceTerms;
		Kingdom payer = ResolveKingdom(terms?.TributePayerKingdomId) ?? initiator;
		Kingdom receiver = ResolveKingdom(terms?.TributeReceiverKingdomId) ?? target;
		if (payer == receiver || (payer != initiator && payer != target) || (receiver != initiator && receiver != target))
		{
			payer = initiator;
			receiver = target;
		}
		int requestedTribute = Math.Max(0, terms?.DailyTribute ?? 0);
		int requestedDuration = Math.Max(0, terms?.DurationDays ?? 0);
		if (!DiplomacyPeaceTermsService.TryApplyPeace(payer, receiver, requestedTribute, requestedDuration, "world_diplomacy_make_peace", out int appliedTribute, out int appliedDays, out string failureReason))
		{
			document.MechanicalResult = "议和未执行：" + failureReason;
			return;
		}
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			document.MechanicalResult = "议和未执行：游戏状态未发生变化";
			return;
		}
		string pairKey = PairKey(initiator.StringId, target.StringId);
		_storage.LastPeaceDayByPair[pairKey] = CurrentDay();
		ClearWarPressure(initiator.StringId, target.StringId);
		ClearWarPressure(target.StringId, initiator.StringId);
		string cessionResult = TryApplyValidatedCession(terms, initiator, target);
		document.MechanicalResult = "双方已达成和平"
			+ (appliedTribute > 0 ? "；" + KingdomName(payer) + "每日向" + KingdomName(receiver) + "支付" + appliedTribute.ToString(CultureInfo.InvariantCulture) + "第纳尔，共" + appliedDays.ToString(CultureInfo.InvariantCulture) + "天" : "")
			+ cessionResult;
		document.ChangedDiplomaticState = true;
	}

	private WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(JObject json, Kingdom author, Kingdom target)
	{
		if (json == null || author == null || target == null || !FactionManager.IsAtWarAgainstFaction(author, target)) return null;
		if (json.SelectToken("peace_terms") is not JObject token) return null;
		string payerId = token["tribute_payer_kingdom_id"]?.ToString()?.Trim() ?? "";
		string receiverId = token["tribute_receiver_kingdom_id"]?.ToString()?.Trim() ?? "";
		Kingdom payer = ResolveKingdom(payerId);
		Kingdom receiver = ResolveKingdom(receiverId);
		if ((payer != author && payer != target) || (receiver != author && receiver != target) || payer == receiver)
		{
			payer = null;
			receiver = null;
		}
		int.TryParse(token["daily_tribute"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tribute);
		int.TryParse(token["duration_days"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration);
		string cessionFromId = token["cession_from_kingdom_id"]?.ToString()?.Trim() ?? "";
		string cessionToId = token["cession_to_kingdom_id"]?.ToString()?.Trim() ?? "";
		Kingdom cessionFrom = ResolveKingdom(cessionFromId);
		Kingdom cessionTo = ResolveKingdom(cessionToId);
		Settlement cession = ResolveSettlementById(token["cession_settlement_id"]?.ToString());
		if (!IsCessionCurrentlyAllowed(cessionFrom, cessionTo, cession, author, target))
		{
			cessionFrom = null;
			cessionTo = null;
			cession = null;
		}
		if (payer == null && cession == null && tribute <= 0) return null;
		return new WorldDiplomacyPeaceTerms
		{
			TributePayerKingdomId = payer?.StringId ?? "",
			TributeReceiverKingdomId = receiver?.StringId ?? "",
			DailyTribute = payer == null ? 0 : DiplomacyPeaceTermsService.ClampTributeAmount(payer, Math.Max(0, tribute)),
			DurationDays = DiplomacyPeaceTermsService.ResolveDurationDays(duration.ToString(CultureInfo.InvariantCulture), payer != null && tribute > 0),
			CessionFromKingdomId = cessionFrom?.StringId ?? "",
			CessionToKingdomId = cessionTo?.StringId ?? "",
			CessionSettlementId = cession?.StringId ?? ""
		};
	}

	private bool IsCessionCurrentlyAllowed(Kingdom from, Kingdom to, Settlement settlement, Kingdom first, Kingdom second)
	{
		if (from == null || to == null || settlement == null || from == to || (from != first && from != second) || (to != first && to != second) || settlement.OwnerClan?.Kingdom != from) return false;
		WarSituationSnapshot snapshot = GetWarSituation(first, second);
		float score = from == first ? snapshot.AuthorCessionScore : snapshot.TargetCessionScore;
		return BuildCessionCandidates(from, to, score).Contains(settlement);
	}

	private string TryApplyValidatedCession(WorldDiplomacyPeaceTerms terms, Kingdom first, Kingdom second)
	{
		Kingdom from = ResolveKingdom(terms?.CessionFromKingdomId);
		Kingdom to = ResolveKingdom(terms?.CessionToKingdomId);
		Settlement settlement = ResolveSettlementById(terms?.CessionSettlementId);
		if (from == null || to == null || settlement == null || settlement.OwnerClan?.Kingdom != from) return "";
		Hero recipient = to.RulingClan?.Leader;
		if (recipient == null) return "";
		try
		{
			ChangeOwnerOfSettlementAction.ApplyByBarter(recipient, settlement);
			return "；" + from.Name + "割让" + settlement.Name + "给" + to.Name;
		}
		catch (Exception ex)
		{
			Log("peace cession failed settlement=" + settlement.StringId + " error=" + ex.Message);
			return "；领地交割失败";
		}
	}

	private void ExecuteAlliance(Kingdom initiator, Kingdom target, WorldDiplomacyDocument document)
	{
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			if (document != null) document.MechanicalResult = "结盟未执行：双方仍处于战争状态";
			return;
		}
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliance == null || alliance.IsAllyWithKingdom(initiator, target))
		{
			if (document != null) document.MechanicalResult = alliance == null
				? "结盟未执行：同盟系统不可用"
				: "结盟未执行：双方已经结盟";
			return;
		}
		RunDiplomaticAction("world_diplomacy_alliance", () => alliance.StartAlliance(initiator, target));
		if (alliance.IsAllyWithKingdom(initiator, target))
		{
			document.MechanicalResult = "双方已缔结同盟";
			document.ChangedDiplomaticState = true;
		}
		else
		{
			document.MechanicalResult = "结盟未执行：游戏状态未发生变化";
		}
	}

	private void ExecuteTradeAgreement(Kingdom initiator, Kingdom target, WorldDiplomacyDocument document)
	{
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			if (document != null) document.MechanicalResult = "贸易协定未执行：双方仍处于战争状态";
			return;
		}
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		if (trade == null || BannerlordApiCompat.HasTradeAgreement(trade, initiator, target))
		{
			if (document != null) document.MechanicalResult = trade == null
				? "贸易协定未执行：贸易系统不可用"
				: "贸易协定未执行：双方已经有贸易协定";
			return;
		}
		CampaignTime duration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(initiator, target);
		RunDiplomaticAction("world_diplomacy_trade", () => trade.MakeTradeAgreement(initiator, target, duration));
		if (BannerlordApiCompat.HasTradeAgreement(trade, initiator, target))
		{
			document.MechanicalResult = "双方已缔结贸易协定";
			document.ChangedDiplomaticState = true;
		}
		else
		{
			document.MechanicalResult = "贸易协定未执行：游戏状态未发生变化";
		}
	}

	private static void RunDiplomaticAction(string source, Action action)
	{
		if (action == null)
		{
			return;
		}
		_internalDiplomaticActionDepth++;
		try
		{
			MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(source, action);
		}
		finally
		{
			_internalDiplomaticActionDepth = Math.Max(0, _internalDiplomaticActionDepth - 1);
		}
	}

	private bool CanIssueWarThreat(Kingdom initiator, Kingdom target, out string reason)
	{
		reason = "";
		if (initiator == null || target == null || initiator == target || initiator.IsEliminated || target.IsEliminated)
		{
			reason = "王国目标无效";
			return false;
		}
		if (!HasIndependentWorldDiplomacyAuthority(initiator) || !HasIndependentWorldDiplomacyAuthority(target))
		{
			reason = "附庸国没有独立外交权，应由宗主国处理";
			return false;
		}
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			reason = "双方已经处于战争状态";
			return false;
		}
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliance?.IsAllyWithKingdom(initiator, target) == true)
		{
			reason = "双方仍有同盟，必须先正式解除同盟";
			return false;
		}
		int day = CurrentDay();
		int peaceProtectionDays = GetPeaceProtectionDays();
		if (peaceProtectionDays > 0
			&& _storage.LastPeaceDayByPair.TryGetValue(PairKey(initiator.StringId, target.StringId), out int peaceDay)
			&& day - peaceDay < peaceProtectionDays)
		{
			reason = "仍处于和平保护期";
			return false;
		}
		return true;
	}

	private bool CanDeclareWar(Kingdom initiator, Kingdom target, out string reason, bool enforceRejectedUltimatum = false)
	{
		if (!CanIssueWarThreat(initiator, target, out reason))
		{
			return false;
		}
		WorldDiplomacyThreat pendingThreatDecision = FindOpenDiplomaticThreat(initiator.StringId, target.StringId);
		if (pendingThreatDecision != null
			&& string.Equals(pendingThreatDecision.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
		{
			reason = "已发出的谴责或最后通牒仍在等待对象国一次性决定";
			return false;
		}
		if (enforceRejectedUltimatum)
		{
			// A publicly rejected ultimatum overrides AI pacing limits. It does not override
			// hard world facts such as peace protection, alliance, vassalage or an existing war.
			return true;
		}
		int day = CurrentDay();
		int cooldownDays = GetOffensiveWarCooldownDays();
		if (_storage.LastOffensiveWarDayByKingdom.TryGetValue(initiator.StringId, out int lastWarDay)
			&& day - lastWarDay < cooldownDays)
		{
			reason = "主动战争冷却尚未结束";
			return false;
		}
		int activeWars = Kingdom.All.Count(x => x != null
			&& !x.IsEliminated
			&& x != initiator
			&& FactionManager.IsAtWarAgainstFaction(initiator, x));
		if (activeWars >= FixedMaxConcurrentOffensiveWars)
		{
			reason = "当前同时战争数量过多";
			return false;
		}
		return true;
	}

	private void CompleteActiveExchange(string reason)
	{
		CompleteExchange(_storage.ActiveExchange?.ExchangeId, reason);
	}

	private void CompleteExchange(string exchangeId, string reason)
	{
		WorldDiplomacyExchange exchange = ResolveExchange(exchangeId);
		if (exchange == null)
		{
			return;
		}
		exchange.State = "completed";
		exchange.CompletedDay = CurrentDay();
		exchange.CloseReason = reason ?? "";
		if (ReferenceEquals(_storage.ActiveExchange, exchange))
		{
			_storage.ActiveExchange = null;
			ScheduleNextNormalRoundAfter(CurrentDay());
			RestoreSuspendedExchangeIfAny();
			return;
		}
		_storage.SuspendedExchanges.Remove(exchange);
	}

	private void ProcessPlayerResponseTimeouts()
	{
		WorldDiplomacyExchange exchange = _storage.ActiveExchange;
		if (exchange == null || !string.Equals(exchange.State, "waiting_player_response", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		int day = CurrentDay();
		if (!exchange.ReminderSent && day >= exchange.ResponseDueDay)
		{
			Kingdom author = ResolveKingdom(exchange.InitiatorKingdomId);
			Kingdom player = ResolveKingdom(exchange.TargetKingdomId);
			WorldDiplomacyDocument source = ResolveDocument(exchange.SourceDocumentId);
			if (author != null && player != null && source != null)
			{
				exchange.ReminderSent = true;
				exchange.State = "generating_player_reminder";
				EnqueueGenerationJob(author, player, exchange, isResponse: true, sourceDocument: source, priority: 80, externalResponseOnly: true, isReminder: true);
				exchange.State = "waiting_player_response";
			}
		}
		if (day >= exchange.CloseDueDay)
		{
			CompleteActiveExchange("player_no_response");
		}
	}

	private void NotifyExternalDiplomacyResolvedInternal(string action, Kingdom initiator, Kingdom target, string reason)
	{
		if (initiator == null || target == null || initiator == target)
		{
			return;
		}
		string normalizedAction = NormalizeIntent(action);
		if (!IsExternallyResolvedDiplomaticIntent(normalizedAction))
		{
			Log("external resolved diplomacy ignored because action is not an executed result action="
				+ (normalizedAction ?? "") + " initiator=" + (initiator.StringId ?? "")
				+ " target=" + (target.StringId ?? ""));
			return;
		}
		if (string.Equals(normalizedAction, "accept_trade", StringComparison.OrdinalIgnoreCase))
		{
			if (!HasProposalTakenEffect("propose_trade", initiator, target))
			{
				Log("external trade acceptance ignored because the live trade agreement was not created initiator="
					+ initiator.StringId + " target=" + target.StringId);
				return;
			}
			MarkOpenBilateralOffersAccepted(_storage.ActiveRound, initiator, target, WorldDiplomacyOfferDomain.Trade);
			ClearBilateralOfferCooldowns(initiator, target, WorldDiplomacyOfferDomain.Trade);
		}
		else if (string.Equals(normalizedAction, "accept_alliance", StringComparison.OrdinalIgnoreCase))
		{
			if (!HasProposalTakenEffect("propose_alliance", initiator, target))
			{
				Log("external alliance acceptance ignored because the live alliance was not created initiator="
					+ initiator.StringId + " target=" + target.StringId);
				return;
			}
			MarkOpenBilateralOffersAccepted(_storage.ActiveRound, initiator, target, WorldDiplomacyOfferDomain.Alliance);
			ClearBilateralOfferCooldowns(initiator, target, WorldDiplomacyOfferDomain.Alliance);
		}
		WorldDiplomacyDocument fact = CreateDocument(
			initiator,
			target,
			"口头外交结果",
			BuildExternalFactBody(normalizedAction, initiator, target, reason),
			"oral_diplomacy",
			isPlayerAuthored: IsPlayerKingdom(initiator),
			isResponse: false,
			exchangeId: "");
		fact.Intent = normalizedAction;
		fact.Commitment = "binding";
		fact.AnalysisStatus = "external_fact";
		fact.MechanicalResult = "已由口头外交执行";
		fact.ChangedDiplomaticState = true;
		fact.HistoryDeclarationRecorded = true;
		WorldDiplomacyRound activeRound = _storage.ActiveRound;
		WorldDiplomacyRound round = activeRound == null
			? EnsureActiveRound(initiator, target, isPlayerInsertion: IsPlayerKingdom(initiator))
			: CanExternalDiplomacyFactJoinRound(activeRound, initiator, target)
				? activeRound
				: null;
		bool appendedExternalSettlementTarget = round?.ResultSettlementPending == true
			&& !RoundRouteContainsKingdom(round, target.StringId);
		if (appendedExternalSettlementTarget
			&& !TryIncludeResultSettlementTarget(round, target.StringId)) round = null;
		else if (appendedExternalSettlementTarget)
		{
			AddOrMergeResultSettlementSlot(round, target.StringId, "route",
				fact.DocumentId, initiator.StringId, prioritize: false);
		}
		fact.RoundId = round?.RoundId ?? "";
		fact.ExchangeId = fact.RoundId;
		fact.AddressedKingdomIds = new List<string> { target.StringId };
		// This document records a diplomacy action that has already resolved elsewhere; it must not start a reply chain.
		fact.RequiresResponse = false;
		AddDocument(fact);
		if (normalizedAction == "declare_war")
		{
			ClearWarPressure(initiator.StringId, target.StringId);
		}
		try
		{
			StartDocumentPropagation(fact, initiator);
		}
		catch (Exception ex)
		{
			Log("external diplomacy propagation failed document=" + fact.DocumentId + " error=" + ex.Message);
		}
		try
		{
			AppendCanonicalDocumentEvents(fact);
		}
		catch (Exception ex)
		{
			ScheduleDeferredCanonicalHistoryRetry(fact.DocumentId);
			Log("external diplomacy canonical history append deferred document=" + fact.DocumentId + " error=" + ex.Message);
		}
		if (round == null)
		{
			fact.RoundProgressHandled = true;
			Log("external diplomacy fact kept outside unrelated active round document=" + fact.DocumentId
				+ " activeRound=" + (activeRound?.RoundId ?? ""));
			return;
		}
		try
		{
			HandleRoundDocumentProcessed(fact);
		}
		catch (Exception ex)
		{
			Log("external diplomacy round progress deferred document=" + fact.DocumentId + " error=" + ex.Message);
		}
	}

	private bool CanExternalDiplomacyFactJoinRound(
		WorldDiplomacyRound round,
		Kingdom initiator,
		Kingdom target)
	{
		if (round == null || initiator == null || target == null
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return false;
		List<string> route = round.RelayRouteKingdomIds ?? new List<string>();
		bool bothSelected = route.Contains(initiator.StringId, StringComparer.OrdinalIgnoreCase)
			&& route.Contains(target.StringId, StringComparer.OrdinalIgnoreCase);
		if (bothSelected) return true;
		if (round.ResultSettlementPending
			&& route.Contains(initiator.StringId, StringComparer.OrdinalIgnoreCase)
			&& CanUseResultSettlementTarget(round, initiator, target)) return true;
		return (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& ((string.Equals(x.ProposerKingdomId, initiator.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(x.ProposerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, initiator.StringId, StringComparison.OrdinalIgnoreCase))));
	}

	private static bool Patch_Kingdom_AddDecision_Prefix(Kingdom __instance, KingdomDecision kingdomDecision, bool ignoreInfluenceCost)
	{
		try
		{
			if (_internalDiplomaticActionDepth > 0 || !IsWorldDiplomacyEnabled())
			{
				return true;
			}
			WorldDiplomacyBehavior behavior = ResolveInstance();
			if (behavior == null)
			{
				return true;
			}
			return !behavior.CaptureNativeDiplomacyDecision(__instance, kingdomDecision);
		}
		catch (Exception ex)
		{
			Log("native decision prefix failed open: " + ex.Message);
			return true;
		}
	}

	private static void Patch_DiplomacyProposalActionItem_Constructed_Postfix(object __instance)
	{
		if (__instance == null || !IsWorldDiplomacyEnabled())
		{
			return;
		}
		try
		{
			TextObject explanation = new TextObject("该项已由AI外交接管，请在“王国公告”中发布外交宣言。");
			TextObject hint = new TextObject("该项已由AI外交接管，请在“王国公告”中发布外交宣言。");
			AccessTools.Property(__instance.GetType(), "IsEnabled")?.SetValue(__instance, false);
			AccessTools.Property(__instance.GetType(), "Explanation")?.SetValue(__instance, explanation.ToString());
			AccessTools.Property(__instance.GetType(), "Hint")?.SetValue(__instance, new HintViewModel(hint));
		}
		catch (Exception ex)
		{
			Log("disable diplomacy proposal item failed: " + ex.Message);
		}
	}

	private static bool Patch_DiplomacyProposalActionItem_Execute_Prefix()
	{
		if (!IsWorldDiplomacyEnabled())
		{
			return true;
		}
		InformationManager.DisplayMessage(new InformationMessage("该项已由AI外交接管，请在“王国公告”中发布外交宣言。"));
		return false;
	}

	private bool CaptureNativeDiplomacyDecision(Kingdom hostKingdom, KingdomDecision decision)
	{
		if (hostKingdom == null || decision == null)
		{
			return false;
		}
		Kingdom target = null;
		string action = "";
		if (decision is DeclareWarDecision warDecision)
		{
			target = warDecision.FactionToDeclareWarOn as Kingdom;
			action = "declare_war";
		}
		else if (decision is MakePeaceKingdomDecision peaceDecision)
		{
			target = peaceDecision.FactionToMakePeaceWith as Kingdom;
			action = "propose_peace";
		}
		else if (decision is StartAllianceDecision allianceDecision)
		{
			target = allianceDecision.KingdomToStartAllianceWith;
			action = "propose_alliance";
		}
		else if (decision is TradeAgreementDecision tradeDecision)
		{
			target = tradeDecision.TargetKingdom;
			action = "propose_trade";
		}
		else
		{
			return false;
		}
		if (target == null || target == hostKingdom || target.IsEliminated)
		{
			return false;
		}
		Clan proposer = decision.ProposerClan;
		Kingdom sourceKingdom = proposer?.Kingdom ?? hostKingdom;
		bool isIncomingPlayerOffer = IsPlayerKingdom(hostKingdom)
			&& (action == "propose_peace" || action == "propose_alliance" || action == "propose_trade")
			&& target != hostKingdom;
		if (isIncomingPlayerOffer)
		{
			sourceKingdom = target;
			target = hostKingdom;
		}
		if (sourceKingdom == null || sourceKingdom.IsEliminated)
		{
			return false;
		}
		int baseValue = action == "declare_war" ? NativeWarSignalBase : NativeOtherSignalBase;
		int scaledValue = baseValue;
		string reason = BuildNativeDecisionReason(sourceKingdom, target, decision, action);
		_storage.NativeSignals.Add(new NativeDiplomacySignal
		{
			SignalId = NewId("native_signal"),
			SourceKingdomId = sourceKingdom.StringId,
			TargetKingdomId = target.StringId,
			Action = action,
			Reason = reason,
			Day = CurrentDay(),
			Value = scaledValue
		});
		TrimNativeSignals();
		if (action == "declare_war")
		{
			AddWarPressure(sourceKingdom.StringId, target.StringId, scaledValue, "原版宣战决议信号：" + reason);
		}
		Log("captured native diplomacy decision action=" + action + " source=" + sourceKingdom.StringId + " target=" + target.StringId + " value=" + scaledValue);
		return true;
	}

	private void RemoveQueuedNativeDiplomacyDecisions()
	{
		if (Campaign.Current == null)
		{
			return;
		}
		int removedCount = 0;
		foreach (Kingdom kingdom in Kingdom.All)
		{
			if (kingdom == null)
			{
				continue;
			}
			List<KingdomDecision> queuedDiplomacy = kingdom.UnresolvedDecisions
				.Where(IsNativeDiplomacyDecision)
				.ToList();
			foreach (KingdomDecision decision in queuedDiplomacy)
			{
				try
				{
					CaptureNativeDiplomacyDecision(kingdom, decision);
					kingdom.RemoveDecision(decision);
					removedCount++;
				}
				catch (Exception ex)
				{
					Log("remove queued native diplomacy decision failed kingdom="
						+ (kingdom.StringId ?? "") + " type=" + decision.GetType().Name + " error=" + ex.Message);
				}
			}
		}
		if (removedCount > 0)
		{
			Log("removed queued native diplomacy decisions count=" + removedCount.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static bool IsNativeDiplomacyDecision(KingdomDecision decision)
	{
		return decision is DeclareWarDecision
			|| decision is MakePeaceKingdomDecision
			|| decision is StartAllianceDecision
			|| decision is TradeAgreementDecision;
	}

	private static void Patch_BuildSharedDiplomacyMemory_Postfix(
		Hero targetHero,
		string input,
		string extraFact,
		string cultureIdOverride,
		bool hasAnyHero,
		CharacterObject targetCharacter,
		string kingdomIdOverride,
		int targetAgentIndex,
		bool suppressDynamicRuleAndLore,
		bool usePrefetchedLoreContext,
		string prefetchedLoreContext,
		ref MyBehavior.ShoutPromptContext __result)
	{
		try
		{
			if (__result == null)
			{
				return;
			}
			bool discussionHit = (__result.PreprocessRuleIds ?? new List<string>()).Any(id =>
				string.Equals(id, "world_diplomacy_discussion", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(id, "diplomacy", StringComparison.OrdinalIgnoreCase));
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			bool proactiveDiscussion = ProactiveNpcRequestBehavior.IsNeedTypeActiveForExternal("Diplomacy")
				&& ProactiveNpcRequestBehavior.IsActiveRequestHero(hero);
			bool inputRequestsKnownDiplomacy = ResolveInstance()?.ShouldInjectDiplomacyMemoryForInput(hero, kingdomIdOverride, input) == true;
			if (!discussionHit && !proactiveDiscussion && !inputRequestsKnownDiplomacy)
			{
				return;
			}
			string block = ResolveInstance()?.BuildDiplomacyMemoryContext(hero, kingdomIdOverride, input);
			if (!string.IsNullOrWhiteSpace(block))
			{
				__result.Extras = (__result.Extras ?? "").TrimEnd() + "\n\n" + block;
			}
		}
		catch (Exception ex)
		{
			Log("shared memory injection failed: " + ex.Message);
		}
	}

	private bool CanDiscussWorldDiplomacy(Hero hero)
	{
		if (hero == null || hero.Clan?.Kingdom == null || hero.Clan.Kingdom.IsEliminated)
		{
			return false;
		}
		if (!hero.IsLord && hero != hero.Clan.Kingdom.RulingClan?.Leader)
		{
			return false;
		}
		return GetKnownDocumentIdsForHero(hero, hero.Clan.Kingdom.StringId).Count > 0;
	}

	private bool TryBuildProactiveDiscussion(Hero hero, out string stableKey, out string fact, out float urgency)
	{
		stableKey = "";
		fact = "";
		urgency = 0f;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		Clan clan = hero?.Clan;
		if (hero == null || playerKingdom == null || playerKingdom.IsEliminated || clan?.Kingdom != playerKingdom
			|| clan == Clan.PlayerClan || clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || !hero.IsLord)
		{
			return false;
		}

		HashSet<string> knownIds = GetKnownDocumentIdsForHero(hero, playerKingdom.StringId);
		int earliestDay = Math.Max(0, CurrentDay() - 7);
		WorldDiplomacyDocument selected = _storage.Documents
			.Where(document => document != null && document.IsReadyForPublication && !document.IsCompressed
				&& document.Day >= earliestDay && knownIds.Contains(document.DocumentId ?? "")
				&& (string.Equals(document.AuthorKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(document.TargetKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase)
					|| (document.AddressedKingdomIds ?? new List<string>()).Contains(playerKingdom.StringId, StringComparer.OrdinalIgnoreCase)
					|| (document.MentionedKingdomIds ?? new List<string>()).Contains(playerKingdom.StringId, StringComparer.OrdinalIgnoreCase)
					|| IsMajorDiplomaticDocument(document)))
			.OrderByDescending(document => !string.IsNullOrWhiteSpace(document.MechanicalResult))
			.ThenByDescending(document => string.Equals(document.TargetKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase))
			.ThenByDescending(document => document.Day)
			.ThenByDescending(document => document.CreatedUtcTicks)
			.FirstOrDefault();
		if (selected == null)
		{
			return false;
		}

		stableKey = "world_diplomacy:" + FirstNonEmpty(selected.RoundId, selected.DocumentId) + ":" + selected.DocumentId;
		urgency = !string.IsNullOrWhiteSpace(selected.MechanicalResult) ? 82f
			: string.Equals(selected.TargetKingdomId, playerKingdom.StringId, StringComparison.OrdinalIgnoreCase) ? 74f
			: IsMajorDiplomaticDocument(selected) ? 64f : 58f;
		List<WorldDiplomacyDocument> related = _storage.Documents
			.Where(document => document != null && !document.IsCompressed && knownIds.Contains(document.DocumentId ?? "")
				&& string.Equals(document.RoundId, selected.RoundId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(document => document.Day).ThenByDescending(document => document.CreatedUtcTicks)
			.Take(3).ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【本国领主主动讨论的外交局势】");
		sb.AppendLine("你与玩家同属" + KingdomName(playerKingdom) + "。你是来交换判断、讨论本国应如何看待和应对局势，不是代表王国擅自签订协议。");
		foreach (WorldDiplomacyDocument document in related)
		{
			sb.AppendLine("- " + BuildCompactDocumentMemoryLine(document)
				+ (string.IsNullOrWhiteSpace(document.Body) ? "" : "：" + Limit(document.Body, 240)));
		}
		fact = sb.ToString().TrimEnd();
		return true;
	}

	private bool ShouldInjectDiplomacyMemoryForInput(Hero hero, string kingdomIdOverride, string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text)) return false;
		if (new[] { "外交", "宣言", "公文", "王庭", "结盟", "同盟", "议和", "停战", "宣战", "贸易", "通商", "条约", "回应", "条件", "最后通牒" }
			.Any(keyword => text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)) return true;
		HashSet<string> knownIds = GetKnownDocumentIdsForHero(hero, kingdomIdOverride);
		return _storage.Documents.Any(document => document != null && knownIds.Contains(document.DocumentId ?? "")
			&& ((!string.IsNullOrWhiteSpace(document.Title) && text.IndexOf(document.Title, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (!string.IsNullOrWhiteSpace(document.AuthorKingdomName) && text.IndexOf(document.AuthorKingdomName, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (!string.IsNullOrWhiteSpace(document.TargetKingdomName) && text.IndexOf(document.TargetKingdomName, StringComparison.OrdinalIgnoreCase) >= 0)));
	}

	private HashSet<string> GetKnownDocumentIdsForHero(Hero hero, string kingdomIdOverride)
	{
		string kingdomId = FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		HashSet<string> knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Settlement currentSettlement = hero?.CurrentSettlement ?? hero?.PartyBelongedTo?.CurrentSettlement;
		WorldDiplomacySettlementKnowledge localKnowledge = _storage.SettlementKnowledge.FirstOrDefault(x => x != null && string.Equals(x.SettlementId, currentSettlement?.StringId, StringComparison.OrdinalIgnoreCase));
		foreach (string id in localKnowledge?.DocumentIds ?? new List<string>()) knownIds.Add(id);
		bool isKingdomNoble = hero?.IsLord == true && !string.IsNullOrWhiteSpace(hero.Clan?.Kingdom?.StringId);
		if (isKingdomNoble)
		{
			WorldDiplomacyKingdomKnowledge nobleKnowledge = _storage.NobleKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
			foreach (string id in nobleKnowledge?.DocumentIds ?? new List<string>()) knownIds.Add(id);
		}
		bool isRulingFamily = hero?.Clan != null && hero.Clan == hero.Clan.Kingdom?.RulingClan;
		if (isRulingFamily || string.Equals(hero?.StringId, ResolveKingdom(kingdomId)?.RulingClan?.Leader?.StringId, StringComparison.OrdinalIgnoreCase))
		{
			WorldDiplomacyKingdomKnowledge courtKnowledge = _storage.KingdomKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
			foreach (string id in courtKnowledge?.DocumentIds ?? new List<string>()) knownIds.Add(id);
		}
		return knownIds;
	}

	private string BuildDiplomacyMemoryContext(Hero hero, string kingdomIdOverride, string input = "")
	{
		if (_storage.Documents.Count == 0)
		{
			return "";
		}
		string kingdomId = FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		HashSet<string> knownIds = GetKnownDocumentIdsForHero(hero, kingdomIdOverride);
		if (knownIds.Count == 0) return "";
		List<WorldDiplomacyDocument> queryMatches = _storage.Documents
			.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
				&& DiplomacyDocumentQueryRelevance(x, input) > 0)
			.OrderByDescending(x => DiplomacyDocumentQueryRelevance(x, input))
			.ThenByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(2)
			.ToList();
		HashSet<string> selectedIds = new HashSet<string>(queryMatches.Select(x => x.DocumentId), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> direct = _storage.Documents
			.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
				&& !selectedIds.Contains(x.DocumentId ?? "")
				&& (!string.IsNullOrWhiteSpace(kingdomId) && (string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
					|| (x.AddressedKingdomIds ?? new List<string>()).Contains(kingdomId, StringComparer.OrdinalIgnoreCase))))
			.OrderByDescending(x => DiplomacyDocumentQueryRelevance(x, input))
			.ThenByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(3)
			.ToList();
		foreach (WorldDiplomacyDocument document in direct) selectedIds.Add(document.DocumentId ?? "");
		List<WorldDiplomacyDocument> headlines = _storage.Documents
			.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "") && !selectedIds.Contains(x.DocumentId ?? "") && IsMajorDiplomaticDocument(x))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(2)
			.ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【当前人物已获知的王国公告】");
		sb.AppendLine("以下仅是公文传播到此人所在地点后，或传到其所属王庭后由贵族通信网获得的事实；不代表全世界同步知晓，也不是当前对话的新承诺。");
		foreach (WorldDiplomacyDocument document in queryMatches)
		{
			sb.AppendLine("- [当前问题命中] " + BuildDetailedDocumentMemoryLine(document));
		}
		foreach (WorldDiplomacyDocument document in direct)
		{
			sb.AppendLine("- [直接相关] " + BuildDetailedDocumentMemoryLine(document));
		}
		foreach (WorldDiplomacyDocument document in headlines)
		{
			sb.AppendLine("- [世界要闻] " + BuildCompactDocumentMemoryLine(document));
		}
		foreach (WorldDiplomacyRoundSummary summary in _storage.RoundSummaries
			.Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains))
			.OrderByDescending(x => x.CreatedDay).Take(1))
		{
			List<string> visibleFacts = (summary.Facts ?? new List<WorldDiplomacyRoundFact>()).Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains)).Select(FormatRoundFactForPrompt).Where(x => !string.IsNullOrWhiteSpace(x)).Take(6).ToList();
			sb.AppendLine("- [往期外交事件] " + Limit(visibleFacts.Count > 0 ? string.Join("；", visibleFacts) : summary.Summary, 650));
		}
		return sb.ToString().TrimEnd();
	}

	private static int DiplomacyDocumentQueryRelevance(WorldDiplomacyDocument document, string input)
	{
		if (document == null || string.IsNullOrWhiteSpace(input)) return 0;
		int score = 0;
		if (!string.IsNullOrWhiteSpace(document.Title) && input.IndexOf(document.Title, StringComparison.OrdinalIgnoreCase) >= 0) score += 100;
		if (!string.IsNullOrWhiteSpace(document.AuthorKingdomName) && input.IndexOf(document.AuthorKingdomName, StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
		if (!string.IsNullOrWhiteSpace(document.TargetKingdomName) && input.IndexOf(document.TargetKingdomName, StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
		if (input.IndexOf(IntentLabel(document.Intent), StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
		return score;
	}

	private static string BuildDetailedDocumentMemoryLine(WorldDiplomacyDocument document)
	{
		if (document == null) return "";
		string response = document.RequiresResponse ? "；该公文明确等待回应" : "";
		string source = string.IsNullOrWhiteSpace(document.SourceDocumentId) ? "" : "；回应来源=" + document.SourceDocumentId;
		string body = string.IsNullOrWhiteSpace(document.Body) ? "" : "；具体诉求与条件=" + Limit(document.Body, 800);
		return BuildCompactDocumentMemoryLine(document) + response + source + body;
	}

	private void ApplyDocumentPressure(WorldDiplomacyDocument document)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.AuthorKingdomId))
		{
			return;
		}
		int delta = document.Intent switch
		{
			"condemn" => 6,
			"warning" => 10,
			"ultimatum" => 18,
			"reject" => 8,
			"reject_peace" => 8,
			"reject_alliance" => 6,
			"reject_trade" => 4,
			"declare_war" => 0,
			"apology" => -8,
			"concession" => -12,
			"accept_peace" => -20,
			_ => string.Equals(document.Tone, "hostile", StringComparison.OrdinalIgnoreCase) ? 3 : 0
		};
		foreach (string targetId in NormalizeKingdomIdList((document.AddressedKingdomIds ?? new List<string>()).Concat(new[] { document.TargetKingdomId }), document.AuthorKingdomId))
		{
			WarPressureEntry existing = FindWarPressure(document.AuthorKingdomId, targetId);
			int repetition = existing != null && string.Equals(existing.LastIntent, document.Intent, StringComparison.OrdinalIgnoreCase) ? existing.ConsecutiveSimilarCount : 0;
			float repetitionFactor = delta > 0 ? 1f / (1f + repetition * 0.35f) : 1f;
			int scaledDelta = (int)Math.Round(delta * repetitionFactor);
			if (scaledDelta != 0) AddWarPressure(document.AuthorKingdomId, targetId, scaledDelta, "外交宣言：" + document.Title, document.Intent);
		}
	}

	private void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent = "")
	{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || string.Equals(sourceId, targetId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		WarPressureEntry entry = _storage.WarPressure.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
		if (entry == null)
		{
			entry = new WarPressureEntry
			{
				SourceKingdomId = sourceId,
				TargetKingdomId = targetId
			};
			_storage.WarPressure.Add(entry);
		}
		entry.Value = Math.Max(0, Math.Min(300, entry.Value + delta));
		entry.LastUpdatedDay = CurrentDay();
		entry.LastReason = Limit(reason, 300);
		if (!string.IsNullOrWhiteSpace(intent))
		{
			entry.ConsecutiveSimilarCount = string.Equals(entry.LastIntent, intent, StringComparison.OrdinalIgnoreCase) ? Math.Min(8, entry.ConsecutiveSimilarCount + 1) : 0;
			entry.LastIntent = intent;
		}
		if (delta > 0)
		{
			entry.NeedsFreshEscalation = false;
		}
		// 兼容旧存档字段；压力现在只作为LLM可读的定性历史，不再武装任何自动行动。
		entry.IsEscalationArmed = false;
		entry.ArmedDay = 0;
	}

	private void ClearWarPressure(string sourceId, string targetId)
	{
		WarPressureEntry entry = _storage.WarPressure.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
		if (entry != null)
		{
			entry.Value = 0;
			entry.IsEscalationArmed = false;
			entry.LastUpdatedDay = CurrentDay();
			entry.LastReason = "外交行动完成，压力清空";
		}
	}

	private void DecayWarPressure()
	{
		int day = CurrentDay();
		foreach (WarPressureEntry entry in _storage.WarPressure)
		{
			if (entry == null || entry.Value <= 0 || day - entry.LastUpdatedDay < 7)
			{
				continue;
			}
			entry.Value = Math.Max(0, entry.Value - 4);
			entry.IsEscalationArmed = false;
			entry.ArmedDay = 0;
		}
	}

	private WarPressureEntry FindWarPressure(string sourceId, string targetId)
	{
		return _storage.WarPressure.FirstOrDefault(x => x != null && string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
	}

	private void TryScheduleTokenCompression()
	{
		if (!IsWorldDiplomacyEnabled()) return;
		EnsureCanonicalHistoryInitialized();
		SyncCanonicalHistorySources();
		long threshold = GetHistoryCompressionTriggerTokens();
		_storage.DiplomacyCompressionPending = _storage.CanonicalHistory.EstimatedTokens >= threshold;
		if (!_storage.DiplomacyCompressionPending || CurrentHour() < _storage.CompressionRetryAfterHour) return;
		if (_storage.Jobs.Any(x => x != null && string.Equals(x.Kind, "compress", StringComparison.OrdinalIgnoreCase))) return;
		long throughSequence = Math.Max(_storage.CanonicalHistory.Snapshot.CoveredThroughSequence, _storage.CanonicalHistory.NextSequence - 1L);
		EnqueueCompressionJob(throughSequence, _storage.CanonicalHistory.EstimatedTokens, GetHistoryCompressionTargetTokens());
	}

	private void CommitCompression(WorldDiplomacyJob job, string raw)
	{
		if (job == null) throw new InvalidOperationException("missing compression job");
		EnsureCanonicalHistoryInitialized();
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		long cutoff = Math.Max(0L, job.CompressionThroughSequence);
		if (cutoff < history.Snapshot.CoveredThroughSequence) throw new InvalidOperationException("compression cutoff predates current snapshot");
		JObject json = ParseJsonObject(raw);
		string summaryText = NormalizeCanonicalHistoryText(ReadString(json, "summary"));
		if (string.IsNullOrWhiteSpace(summaryText)) throw new InvalidOperationException("compression output has empty summary");
		long covered = json.Value<long?>("covered_through_sequence") ?? -1L;
		if (covered != cutoff) throw new InvalidOperationException("compression output covered_through_sequence mismatch");
		int targetTokens = Math.Max(1, job.CompressionTargetTokens > 0 ? job.CompressionTargetTokens : GetHistoryCompressionTargetTokens());
		long summaryTokens = EstimateHistoryTokens(summaryText);
		if (summaryTokens > targetTokens) throw new InvalidOperationException("compression output exceeds target token budget");
		int overallTargetTokens = Math.Max(1, job.CompressionOverallTargetTokens > 0
			? job.CompressionOverallTargetTokens
			: GetHistoryCompressionTargetTokens());
		int protectedBudgetTokens = Math.Max(0, Math.Min(overallTargetTokens - 256, overallTargetTokens / 4));
		List<WorldDiplomacyCanonicalProtectedFact> protectedFacts = SelectCanonicalProtectedFactsWithinTokenBudget(
			BuildCanonicalProtectedFactsThrough(cutoff), protectedBudgetTokens);
		List<string> preservedResultIds = protectedFacts
			.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
			.Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		List<WorldDiplomacyCanonicalHistoryEntry> compressedEntries = history.DeltaEntries
			.Where(x => x != null && x.Sequence <= cutoff).OrderBy(x => x.Sequence).ToList();
		List<string> sourceIds = compressedEntries.Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		WorldDiplomacyCompressionSummary summary = new WorldDiplomacyCompressionSummary
		{
			BatchId = FirstNonEmpty(job.CompressionBatchId, "diplomacy_compaction_" + (_storage.CompressionSequence + 1).ToString(CultureInfo.InvariantCulture)),
			Summary = summaryText,
			CreatedDay = CurrentDay(),
			StartDay = compressedEntries.Count == 0 ? CurrentDay() : compressedEntries.Min(x => x.Day),
			EndDay = compressedEntries.Count == 0 ? CurrentDay() : compressedEntries.Max(x => x.Day),
			TokenCount = Math.Max(0L, job.CompressionTokenCount),
			SourceRoundIds = sourceIds,
			KingdomIds = compressedEntries.SelectMany(x => (x.TargetKingdomIds ?? new List<string>()).Concat(new[] { x.AuthorKingdomId }))
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			ConfirmedResults = compressedEntries.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
				.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(48).ToList()
		};
		WorldDiplomacyCanonicalHistorySnapshot replacement = new WorldDiplomacyCanonicalHistorySnapshot
		{
			Content = summaryText,
			CoveredThroughSequence = cutoff,
			CreatedDay = CurrentDay(),
			PreservedResultSourceIds = preservedResultIds,
			ProtectedFacts = protectedFacts
		};
		string replacementPayload = RenderCanonicalSnapshotPayload(replacement);
		replacement.ContentHash = StablePromptHash(replacementPayload);
		replacement.EstimatedTokens = EstimateHistoryTokens(replacementPayload);
		if (replacement.EstimatedTokens > overallTargetTokens)
		{
			throw new InvalidOperationException("compressed history exceeds overall target token budget");
		}
		// Commit snapshot and delete only the frozen prefix. Entries appended while the request
		// was running have greater sequence numbers and remain as delta.
		history.Snapshot = replacement;
		history.DeltaEntries.RemoveAll(x => x != null && x.Sequence <= cutoff);
		history.Revision++;
		_storage.CompressionSummaries.RemoveAll(x => x != null && string.Equals(x.BatchId, summary.BatchId, StringComparison.OrdinalIgnoreCase));
		_storage.CompressionSummaries.Add(summary);
		_storage.CompressionSequence = Math.Max(_storage.CompressionSequence + 1, ParseCompressionSequence(summary.BatchId));
		_storage.LastDiplomacyCompressionDay = CurrentDay();
		_storage.CompressionRetryAfterHour = 0;
		_storage.CompressionRetryAttempts = 0;
		InvalidateCanonicalHistoryRenderCache();
		RecalculateCanonicalHistoryTokens();
		Log("token compression committed batch=" + summary.BatchId
			+ " through_sequence=" + cutoff.ToString(CultureInfo.InvariantCulture)
			+ " retained_delta=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
			+ " protected_facts=" + protectedFacts.Count.ToString(CultureInfo.InvariantCulture)
			+ " remaining_tokens=" + history.EstimatedTokens.ToString(CultureInfo.InvariantCulture));
	}

	private static int ParseCompressionSequence(string batchId)
	{
		string text = batchId ?? "";
		int separator = text.LastIndexOf('_');
		return separator >= 0 && int.TryParse(text.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(0, value) : 0;
	}

	private void TryPublishPendingNotifications()
	{
		DateTime nowUtc = DateTime.UtcNow;
		if (nowUtc < _nextNotificationPollUtc) return;
		_nextNotificationPollUtc = nowUtc.AddSeconds(1d);
		foreach (WorldDiplomacyDocument rumor in _storage.Documents
			.Where(x => x != null && !x.IsPlayerAuthored && x.IsReadyForPublication && !x.RumorNotified)
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).Take(3).ToList())
		{
			rumor.RumorNotified = true;
			InformationManager.DisplayMessage(new InformationMessage(BuildDiplomacyRumor(rumor)));
			Log("diplomacy-rumor.shown document=" + rumor.DocumentId + " day=" + CurrentDay().ToString(CultureInfo.InvariantCulture));
		}
		bool enabled = AreMapNotificationsEnabled();
		if (!enabled)
		{
			foreach (WorldDiplomacyDocument document in _storage.Documents.Where(x => x != null
				&& !x.IsPlayerAuthored && x.IsReadyForPublication && x.HasReachedPlayerCourt && !x.FormalNoticeShown))
			{
				document.FormalNoticeShown = true;
				document.IsNotified = true;
			}
			if (_lastMapNotificationsEnabled != false)
			{
				_notifiedDocumentIdsThisSession.Clear();
			}
			_lastMapNotificationsEnabled = false;
			return;
		}
		_lastMapNotificationsEnabled = true;
		if (!CanPublishMapNotification() || !TryEnsureMapNotificationRegistered())
		{
			return;
		}
		foreach (WorldDiplomacyDocument document in _storage.Documents
			.Where(x => x != null
				&& !x.IsPlayerAuthored
				&& x.IsReadyForPublication
				&& x.HasReachedPlayerCourt
				&& !x.IsRead
				&& !x.FormalNoticeShown
				&& !_notifiedDocumentIdsThisSession.Contains(x.DocumentId ?? ""))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.Take(3)
			.ToList())
		{
			try
			{
				_notifiedDocumentIdsThisSession.Add(document.DocumentId);
				MBInformationManager.AddNotice(new WorldDiplomacyMapNotification(
					document.DocumentId,
					BuildDisplayedDocumentTitle(document),
					BuildNotificationDescription(document)));
				document.IsNotified = true;
				document.FormalNoticeShown = true;
				Log("formal-court-notice.shown document=" + document.DocumentId + " realm=" + (Clan.PlayerClan?.Kingdom?.StringId ?? "")
					+ " day=" + CurrentDay().ToString(CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				_notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
				Log("notification publish failed: " + ex.Message);
				break;
			}
		}
	}

	private static string BuildDiplomacyRumor(WorldDiplomacyDocument document)
	{
		string author = FirstNonEmpty(document?.AuthorKingdomName, KingdomName(ResolveKingdom(document?.AuthorKingdomId)), "某国");
		string targetId = FirstNonEmpty(document?.TargetKingdomId, document?.Actions?.FirstOrDefault()?.TargetKingdomId, document?.AddressedKingdomIds?.FirstOrDefault());
		string target = string.IsNullOrWhiteSpace(targetId) ? "" : KingdomName(ResolveKingdom(targetId));
		HashSet<string> intents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (document?.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null)) intents.Add(NormalizeIntent(action.Intent));
		}
		else intents.Add(NormalizeIntent(document?.Intent));
		string subject = intents.Contains("declare_war") ? "战争事宜"
			: intents.Any(x => x == "propose_peace" || x == "accept_peace" || x == "reject_peace") ? "停战事宜"
			: intents.Any(x => x == "propose_alliance" || x == "accept_alliance" || x == "reject_alliance" || x == "break_alliance") ? "盟约事宜"
			: intents.Any(x => x == "propose_trade" || x == "accept_trade" || x == "reject_trade" || x == "cancel_trade") ? "贸易事宜"
			: intents.Any(x => x == "ultimatum" || x == "warning" || x == "comply_ultimatum") ? "最后通牒事宜"
			: intents.Any(x => x == "apology" || x == "concession") ? "外交让步"
			: "当前外交局势";
		return string.IsNullOrWhiteSpace(target)
			? "据传" + author + "王庭发布了一份新的外交宣言，似乎涉及" + subject + "。"
			: "据传" + author + "王庭发布了一份面向" + target + "的外交宣言，似乎涉及" + subject + "。";
	}

	private bool TryEnsureMapNotificationRegistered()
	{
		try
		{
			MapNotificationView view = MapScreen.Instance?.MapNotificationView;
			if (view == null)
			{
				return false;
			}
			if (!ReferenceEquals(_registeredMapNotificationView, view))
			{
				view.RegisterMapNotificationType(typeof(WorldDiplomacyMapNotification), typeof(WorldDiplomacyMapNotificationItemVM));
				_registeredMapNotificationView = view;
				_notifiedDocumentIdsThisSession.Clear();
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("notification registration failed: " + ex.Message);
			return false;
		}
	}

	internal bool OpenDocumentFromNotification(string documentId)
	{
		WorldDiplomacyDocument document = ResolveDocument(documentId);
		if (document == null)
		{
			return false;
		}
		document.IsRead = true;
		Action replyAction = null;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		WorldDiplomacyRoundParticipant playerParticipant = round?.Participants?.FirstOrDefault(x => x != null && IsPlayerKingdom(ResolveKingdom(x.KingdomId)));
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (round != null && playerParticipant?.MandatoryReplyPending == true
			&& HasIndependentWorldDiplomacyAuthority(playerKingdom))
		{
			replyAction = () => OpenPlayerReplyCompose(document);
		}
		string subtitle = document.AuthorKingdomName
			+ " · "
			+ document.AuthorRulerName
			+ " · "
			+ FirstNonEmpty(document.GameDate, FormatCampaignDate(document.Day))
			+ " · "
			+ DocumentTypeLabel(document);
		return CourierLetterReplyPopup.ShowWithReply(
			BuildDisplayedDocumentTitle(document),
			subtitle,
			string.IsNullOrWhiteSpace(document.Body) ? "（该旧公文正文已压缩至年度摘要。）" : FormatDiplomaticBodyForDisplay(document.Body),
			replyAction,
			"回应",
			null,
			"关闭",
			BuildDiplomaticStandingImpactTextForExternal(document));
	}

	private void OpenPlayerReplyCompose(WorldDiplomacyDocument sourceDocument)
	{
		WorldDiplomacyRound round = ResolveRound(sourceDocument?.RoundId);
		if (round == null || sourceDocument == null)
		{
			return;
		}
		WorldDiplomacyComposePopup.Show(
			"回应外交宣言",
			"",
			"",
			delegate(string body)
			{
				Kingdom player = Clan.PlayerClan?.Kingdom;
				Kingdom target = ResolveKingdom(sourceDocument.AuthorKingdomId);
				if (player == null || target == null || !HasIndependentWorldDiplomacyAuthority(player))
				{
					if (player != null && !HasIndependentWorldDiplomacyAuthority(player))
					{
						InformationManager.DisplayMessage(new InformationMessage("我国的外交事务由" + KingdomName(ResolveWorldDiplomacyRepresentative(player)) + "掌管，不能独立回应外交宣言。"));
					}
					return;
				}
				WorldDiplomacyDocument response = CreateDocument(
					player,
					target,
					"外交回应",
					NormalizeBody(body),
					"player_response",
					isPlayerAuthored: true,
					isResponse: true,
					exchangeId: round.RoundId);
				response.RoundId = round.RoundId;
				response.SourceDocumentId = sourceDocument.DocumentId;
				response.AutomaticReplyDepth = Math.Max(1, sourceDocument.AutomaticReplyDepth + 1);
				AddDocument(response);
				WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, player.StringId, "active", mandatoryReply: false);
				participant.MandatoryReplyPending = false;
				participant.LastTriggeredDocumentId = sourceDocument.DocumentId;
				round.LastActivityDay = CurrentDay();
				PublishPlayerAuthoredDocumentImmediately(response);
				EnqueueAnalysisJob(response, priority: 100);
				InformationManager.DisplayMessage(new InformationMessage("外交回应已经公开发布；系统正在后台解析其诉求与外交动作。"));
			},
			null);
	}

	private WorldEventInboxPopupData BuildRoyalAnnouncementArchiveData()
	{
		Dictionary<string, WorldEventCountryData> groups = new Dictionary<string, WorldEventCountryData>(StringComparer.OrdinalIgnoreCase);
		foreach (AnimusForgeWorldEventInboxEntry entry in AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(160))
		{
			if (entry == null)
			{
				continue;
			}
			string kingdomId = FirstNonEmpty(entry.KingdomId, "policy_unknown");
			WorldEventCountryData group = GetOrCreateArchiveGroup(groups, kingdomId, FirstNonEmpty(entry.KingdomName, "未知国家"));
			string date = FirstNonEmpty(entry.GameDate, entry.Day > 0 ? "第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天" : "未知日期");
			group.Records.Add(new WorldEventRecordData
			{
				EventId = entry.EventId ?? "",
				KindLabel = FirstNonEmpty(entry.KindLabel, "自定义政策"),
				HeaderRightText = entry.HeaderRightText ?? "",
				DateText = date,
				TitleText = FirstNonEmpty(entry.Title, entry.KindLabel, "自定义政策"),
				MetaText = date + "  ·  " + FirstNonEmpty(entry.KindLabel, "自定义政策") + "  ·  " + FirstNonEmpty(entry.KingdomName, entry.KingdomId),
				PolicyNameText = "",
				BodyText = FirstNonEmpty(entry.DetailText, entry.Summary, "（无详情）"),
				BodySectionTitleText = FirstNonEmpty(entry.BodySectionTitleText, "公告详情"),
				ImpactSectionTitleText = entry.ImpactSectionTitleText ?? "",
				ImpactText = entry.ImpactText ?? "",
				IndexMetaText = date + "  ·  " + FirstNonEmpty(entry.KindLabel, "自定义政策"),
				UnreadMarkerText = entry.IsRead ? "" : "新",
				IsUnread = !entry.IsRead,
				HasPolicyName = false,
				HasImpact = !string.IsNullOrWhiteSpace(entry.ImpactText)
			});
		}
		foreach (WorldDiplomacyDocument document in _storage.Documents
			.Where(x => x != null && (x.IsPlayerAuthored || x.IsReadyForPublication))
			.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Take(240))
		{
			if (document == null)
			{
				continue;
			}
			WorldEventCountryData group = GetOrCreateArchiveGroup(groups, document.AuthorKingdomId, document.AuthorKingdomName);
			string date = FirstNonEmpty(document.GameDate, FormatCampaignDate(document.Day));
			string typeLabel = DocumentTypeLabel(document);
			string eventMeta = BuildDocumentEventMeta(document);
			string targetSummary = document.Actions?.Count > 1
				? string.Join("、", document.Actions.Where(x => x != null).Select(x => FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId)))
				: document.TargetKingdomName;
			group.Records.Add(new WorldEventRecordData
			{
				EventId = document.DocumentId,
				KindLabel = typeLabel,
				HeaderRightText = FirstNonEmpty(targetSummary, "世界公告"),
				DateText = date,
				TitleText = BuildDisplayedDocumentTitle(document),
				IndexTitleText = BuildArchiveIndexDocumentTitle(document),
				MetaText = date + "  ·  " + typeLabel + "  ·  " + document.AuthorKingdomName + (string.IsNullOrWhiteSpace(targetSummary) ? "" : " → " + targetSummary) + eventMeta,
				PolicyNameText = "",
				BodyText = string.IsNullOrWhiteSpace(document.Body) ? "该旧公文正文已经压缩，可查看对应年度外交摘要。" : FormatDiplomaticBodyForDisplay(document.Body),
				BodySectionTitleText = "公告正文",
				ImpactSectionTitleText = "外交结果与外交影响",
				ImpactText = BuildDiplomaticStandingImpactTextForExternal(document),
				IndexMetaText = "外交宣言：" + typeLabel,
				UnreadMarkerText = document.IsRead ? "" : "新",
				IsUnread = !document.IsRead,
				HasPolicyName = false,
				HasImpact = true
			});
		}
		foreach (WorldDiplomacyAnnualSummary summary in _storage.AnnualSummaries.OrderByDescending(x => x.Year))
		{
			WorldEventCountryData group = GetOrCreateArchiveGroup(groups, "diplomacy_archive", "外交编年档案");
			group.Records.Add(new WorldEventRecordData
			{
				EventId = "diplomacy_summary:" + summary.Year.ToString(CultureInfo.InvariantCulture),
				KindLabel = "年度外交摘要",
				HeaderRightText = "世界共享记忆",
				DateText = "第" + (summary.Year + 1).ToString(CultureInfo.InvariantCulture) + "年",
				TitleText = "第" + (summary.Year + 1).ToString(CultureInfo.InvariantCulture) + "年外交纪要",
				MetaText = "年度压缩档案",
				BodyText = summary.Summary,
				BodySectionTitleText = "年度摘要",
				ImpactSectionTitleText = summary.MajorEvents.Count > 0 ? "重大事件索引" : "",
				ImpactText = string.Join("\n", summary.MajorEvents ?? new List<string>()),
				IndexMetaText = "年度外交摘要",
				HasImpact = summary.MajorEvents.Count > 0
			});
		}
		foreach (WorldDiplomacyCompressionSummary summary in (_storage.CompressionSummaries ?? new List<WorldDiplomacyCompressionSummary>()).OrderByDescending(x => x.CreatedDay))
		{
			WorldEventCountryData group = GetOrCreateArchiveGroup(groups, "diplomacy_archive", "外交编年档案");
			group.Records.Add(new WorldEventRecordData
			{
				EventId = "diplomacy_summary:" + summary.BatchId,
				KindLabel = "外交历史整理",
				HeaderRightText = "长期外交记忆",
				DateText = FormatCampaignDate(summary.CreatedDay),
				TitleText = "外交历史整理档案",
				MetaText = "累计 " + summary.TokenCount.ToString("N0", CultureInfo.InvariantCulture) + " Tokens 后整理",
				BodyText = summary.Summary,
				BodySectionTitleText = "外交纪要",
				ImpactSectionTitleText = summary.ConfirmedResults.Count > 0 ? "游戏确认结果" : "",
				ImpactText = string.Join("\n", summary.ConfirmedResults),
				IndexMetaText = "外交历史整理",
				HasImpact = summary.ConfirmedResults.Count > 0
			});
		}
		WorldEventInboxPopupData data = new WorldEventInboxPopupData
		{
			TitleText = "王国公告",
			SubtitleText = BuildRoyalAnnouncementSubtitle(),
			EmptyStateText = "目前还没有王国公告。",
			CloseText = "关闭",
			Countries = groups.Values
				.OrderBy(x => string.Equals(x.KingdomId, "diplomacy_archive", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
				.ThenBy(x => x.KingdomName, StringComparer.CurrentCulture)
				.ToList()
		};
		foreach (WorldEventCountryData group in data.Countries)
		{
			group.Records = group.Records
				.OrderByDescending(x => ParseDayForArchive(x.DateText))
				.ThenBy(x => x.TitleText, StringComparer.CurrentCulture)
				.ToList();
			group.UnreadCount = group.Records.Count(x => x.IsUnread);
		}
		data.SelectedCountryIndex = Math.Max(0, data.Countries.FindIndex(x => x.Records.Count > 0));
		return data;
	}

	private static WorldEventCountryData GetOrCreateArchiveGroup(Dictionary<string, WorldEventCountryData> groups, string id, string name)
	{
		string key = FirstNonEmpty(id, "unknown");
		if (!groups.TryGetValue(key, out WorldEventCountryData group))
		{
			group = new WorldEventCountryData
			{
				KingdomId = key,
				KingdomName = FirstNonEmpty(name, key, "未知国家")
			};
			groups[key] = group;
		}
		return group;
	}

	private static string BuildCommonDiplomacySystemPrefix()
	{
		return DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? "";
	}

	private string GetCommonDiplomacyContract(WorldDiplomacyRound round)
	{
		return BuildCommonDiplomacySystemPrefix();
	}

	private static bool TryExtractCommonContractFromJob(WorldDiplomacyJob job, out string contract)
	{
		contract = "";
		if (job == null) return false;
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			string generateMessageSystem = job.LlmMessages?.FirstOrDefault(x => x != null
				&& string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content;
			return TryExtractCommonContractBeforeMarker(generateMessageSystem, DiplomaticDeclarationWritingContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, DiplomaticDeclarationWritingContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(generateMessageSystem, CanonicalHistoryContractMarker, out contract)
				|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, CanonicalHistoryContractMarker, out contract);
		}
		string marker;
		if (string.Equals(job.Kind, "round_plan", StringComparison.OrdinalIgnoreCase))
		{
			marker = RoundPlanTaskMarker;
		}
		else if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
		{
			marker = DiplomacyAnalysisTaskMarker;
		}
		else
		{
			return false;
		}
		string messageSystem = job.LlmMessages?.FirstOrDefault(x => x != null
			&& string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content;
		return TryExtractCommonContractBeforeMarker(messageSystem, marker, out contract)
			|| TryExtractCommonContractBeforeMarker(job.SystemPrompt, marker, out contract);
	}

	private static bool TryExtractCommonContractBeforeMarker(string systemPrompt, string marker, out string contract)
	{
		contract = "";
		if (string.IsNullOrEmpty(systemPrompt) || string.IsNullOrEmpty(marker)) return false;
		int markerIndex = systemPrompt.LastIndexOf(marker, StringComparison.Ordinal);
		if (markerIndex < 0) return false;
		contract = systemPrompt.Substring(0, markerIndex).TrimEnd('\r', '\n');
		return true;
	}

	private string ResolveCommonContractForCacheDiagnostics(WorldDiplomacyJob job, out string source)
	{
		if (TryExtractCommonContractFromJob(job, out string jobContract))
		{
			source = "job-system";
			return jobContract;
		}
		source = "current-config";
		return BuildCommonDiplomacySystemPrefix();
	}

	private static StringBuilder CreateSystemPromptBuilder(string commonContract)
	{
		StringBuilder sb = new StringBuilder();
		if (string.IsNullOrEmpty(commonContract)) return sb;
		sb.Append(commonContract);
		char tail = commonContract[commonContract.Length - 1];
		if (tail != '\r' && tail != '\n') sb.AppendLine();
		return sb;
	}

	private static void AppendDiplomaticDeclarationWritingContract(StringBuilder sb)
	{
		if (sb == null) return;
		GetDiplomaticDeclarationCharacterRange(out int minimumCharacters, out int maximumCharacters);
		sb.AppendLine(DiplomaticDeclarationWritingContractMarker);
		sb.AppendLine("本节仅在MODE=DECLARE时生效；MODE=COMPACT时忽略本节，并严格执行system中的MODE=COMPACT固定任务合同与尾部动态参数。");
		sb.AppendLine("不得讨论幕后调度、生成规则、候选方案、数据判定或技术流程；内部字段只出现在JSON结构中，绝不能变成公文内容。");
		sb.Append("body应以王国，王庭，王国，政府等为发言主体,正文必须最少")
			.Append(minimumCharacters.ToString(CultureInfo.InvariantCulture))
			.Append("个中文字符，最多")
			.Append(maximumCharacters.ToString(CultureInfo.InvariantCulture))
			.AppendLine("个中文字符（标点计入）。");
		sb.AppendLine("文风应当符合国家设定，禁止使用文言文，内容要有最终决定（除非还需继续讨论）");
		sb.AppendLine("可以坚定、务实、冷峻、和缓或骄傲，但讥讽也必须是一个国家对另一个国家的公开评价");
		sb.AppendLine("不必讲述自身的文化与状况，只需发言不违反文化与状况，只针对当前外交局面做出必要的回应，尽量说明意图之事，言简意赅即可，不要一大堆废话。");
		sb.AppendLine("不要把供决策的后台态势照抄进正文。不能说战争进展领先多少分、议和开放度或劣势评分达到多少、关系点和战力值是多少；应改成由战报和现实结果支撑的自然判断。精确贡金、停战期限及其他正式条款不受此限制。");
		sb.AppendLine("地理称谓必须服从用户消息中的当前地理关系：只有明确标为接壤的两国才可互称邻国、边境国家或声称拥有共同边界；标为不接壤时，即使关系密切、同属一种文化、曾经统治相邻领土或正在参与同一场交涉，也不得写成邻国或边界争端。不得把供判断的距离档位和地图距离写进正文。");
		sb.AppendLine("严禁抄袭【全局长期外交历史】中的其他公文，必须完全原创公文");
	}

	private static string BuildDiplomaticDeclarationModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【统一任务：公开外交宣言】根据用户消息提供的档案，为当前发布国起草一篇由其统治者授权或署名、面向其他国家的外交措辞");
		sb.AppendLine("一篇宣言可同时对1至4个国家各执行一项动作；actions中每个对象只能出现一次。开场由发布国从候选国中自主选择；");
		sb.AppendLine("不得替其他王国发言，不得编造输入中没有支持的领土、制度、亲属关系、战斗和硬事件。");
		sb.AppendLine("warning表示谴责，不是劝告、关心或善意提醒；正文必须要求停止具体敌对或军事行为，并说明拒不停止将升级最后通牒或正式宣战。ultimatum表示战争最后通牒。");
		sb.AppendLine("actions[].intent只填写该对象当前可用的动作，内部字段由系统按对象和intent确定。statement必须单独使用。");
		sb.AppendLine("回合首篇不得使用statement；仅后续AI轮次的当前可选动作出现statement时可用。此时必须填写negotiation_move，表示有意义的谈判动作，而非机械外交行为。可用值为question|clarification|state_position|justify_demand|acknowledge_concern|dispute_claim|counterproposal|conditional_acceptance|partial_concession|request_concession|revise_terms|request_delay|consult_court|set_deadline|final_offer|withdraw_offer|end_negotiation|declare_deadlock。不得重复上一轮原话。");
		sb.AppendLine("accept_*只表示无条件接受全部原条件并立即生效；仍需议价、审批或修改条款时不得选择，且建立与解除同盟或贸易的措辞不得相反。");
		sb.AppendLine("决策顺序是国家生存与现实利益、长期战略、战争局势与双边关系、国家性格，最后才以对象国国际声誉作为可信度证据。国家性格决定本国多看重守约与可靠，长期战略决定如何利用这份判断；国际声誉只调整对外国承诺的信任、合作条件与外交风险，不得单独触发或阻止宣战，不得覆盖领土、安全或遏制目标。高声誉不等于爱好和平、值得喜欢或不会扩张，低声誉也不自动构成宣战理由；一个守约但强大的扩张国仍可能是必须遏制的威胁。");
		sb.AppendLine("本国国际声誉是会随时间消散、需要由持续行为维护的战略资本，不是最高目标，也不是必须最大化的分数。国家性格决定愿意为信誉付出多少代价，长期战略决定希望维持何种档位：重视贸易、联盟、守约或调停的国家通常更珍惜高声誉，务实、扩张或危急中的国家可以为了生存、领土、安全与遏制主动承受声誉损失。提高声誉必须由实际履约、可执行让步、承担代价或现实成果支撑；重复礼貌表态、空洞承诺和没有进展的宣言不能刷取声誉。声誉得失只在宣言拟定后评估，不得反过来强迫国家改口或沉默。");
		sb.AppendLine("标题应简洁概括事件或决定，通常不超过20个字。");
		sb.AppendLine("先独立完成title、body与actions，再对这篇已经拟定的宣言做事后国际声誉评估。评估不能反过来改变、软化、取消宣言或令国家沉默。每篇宣言都必须产生非零评价：只能填写-10到-1或1到10，不得为0。履约、可执行的妥协、有效调停、承担责任和可靠协作通常提高；违约、反复改条件、欺骗、拖延、滥用威胁和违反停战通常降低。单纯拒绝要求时，根据是否及时、明确、前后一致以及是否给出可继续谈判的说明判定最低幅度±1；重复没有新条件、没有新解释、没有新行动或没有谈判进展的空洞表态应当判-1，不能靠礼貌套话反复获得声誉。reason只写简短事实理由。");
		sb.AppendLine("用户消息含“同次确定本次外交事件参与国”时，round_plan.selected_kingdom_ids必须包含全部动作对象。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏：");
		sb.AppendLine("title与body必须完整表达actions中的全部动作。");
		sb.AppendLine("{\"title\":\"简短标题\",\"body\":\"完整外交措辞正文\",\"actions\":[{\"target_kingdom_id\":\"对象ID\",\"intent\":\"当前可选动作\",\"negotiation_move\":\"statement时必填否则空字符串\",\"peace_terms\":{}}],\"mentioned_kingdom_ids\":[],\"tone\":\"conciliatory|neutral|firm|hostile\",\"round_plan\":{\"topic\":\"议题或空\",\"selected_kingdom_ids\":[\"ID\"]},\"international_reputation_delta\":1,\"international_reputation_reason\":\"事后评估理由\"}");
		return sb.ToString().TrimEnd();
	}

	private static string BuildCanonicalHistoryCompressionModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("只压缩前一条全局长期外交历史，不起草宣言、不执行外交动作，也不引用尾部参数之外的动态国家状态。");
		sb.AppendLine("合并旧快照与增量，保留世界周报中的关键变化、政策生命周期、各国最终宣言立场、提议与答复关系及经游戏机制确认的外交结果。提议、接受、拒绝与确认结果必须保持区别，不得把未执行主张写成现实状态。可合并重复表述，但不得更改或虚构事实。");
		sb.AppendLine("程序会在总预算内另行保留一小段近期已确认结果与答复关联；summary仍须概括完整时间范围，尤其要保留更早的关键结果，但无需逐项复制内部ID。summary不得超过尾部给出的目标上限。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏或解释。covered_through_sequence必须原样填写尾部的覆盖截止seq：{\"summary\":\"压缩后的长期外交历史正文\",\"covered_through_sequence\":0}");
		return sb.ToString().TrimEnd();
	}

	private static string BuildGenerationSystemPrompt(string commonContract)
	{
		return BuildCanonicalHistorySystemPrompt(commonContract);
	}

	private static string BuildCanonicalHistorySystemPrompt(string commonContract)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		AppendDiplomaticDeclarationWritingContract(sb);
		sb.AppendLine(DiplomacyModeDispatchContractMarker);
		sb.AppendLine("最后一条用户消息末尾的MODE是本次唯一任务选择器。只执行同名固定任务合同，其他MODE合同全部忽略；不同合同的动作、字段和JSON结构不得混用。尾部用户消息只提供本次动态事实、参数与MODE，不会覆盖本分派规则。");
		sb.AppendLine(DiplomaticDeclarationModeContractMarker);
		sb.AppendLine("仅当MODE=DECLARE时执行本合同；MODE=COMPACT时完整忽略本节。");
		sb.AppendLine(BuildDiplomaticDeclarationModeContract());
		sb.AppendLine(CanonicalHistoryCompressionModeContractMarker);
		sb.AppendLine("仅当MODE=COMPACT时执行本合同；MODE=DECLARE时完整忽略本节。");
		sb.AppendLine(BuildCanonicalHistoryCompressionModeContract());
		sb.AppendLine(CanonicalHistoryContractMarker);
		sb.AppendLine("下一条系统消息是全局长期外交历史。只把它当作历史事实档案；最后一条用户消息的 MODE 决定本次唯一任务和输出结构。当前动态状态与历史冲突时，以当前动态状态为准。");
		return sb.ToString().TrimEnd();
	}

	private static string BuildDeclareModePrompt(string dynamicPrompt)
	{
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(dynamicPrompt)) sb.AppendLine(dynamicPrompt.Trim());
		sb.AppendLine("【MODE=DECLARE】");
		sb.AppendLine("只激活第一条system消息中的MODE=DECLARE固定任务合同，并只输出该合同规定的JSON对象。");
		return sb.ToString().TrimEnd();
	}

	private List<string> GetPresentedThreatDocumentIds(string authorKingdomId)
	{
		string authorId = (authorKingdomId ?? "").Trim();
		if (authorId.Length == 0) return new List<string>();
		return (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& !string.IsNullOrWhiteSpace(x.StageDocumentId)
				&& (string.Equals(x.IssuerKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
					|| (string.Equals(x.TargetKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))))
			.OrderBy(x => x.StageIssuedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.StageDocumentId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<string> GetPresentedThreatFollowThroughDocumentIds(string authorKingdomId)
	{
		string authorId = (authorKingdomId ?? "").Trim();
		if (authorId.Length == 0) return new List<string>();
		return (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& string.Equals(x.IssuerKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(x.StageDocumentId))
			.OrderBy(x => x.StageIssuedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.StageDocumentId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private bool HasStaleDiplomaticThreatPresentation(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return false;
		List<string> currentPresented = GetPresentedThreatDocumentIds(job.AuthorKingdomId);
		List<string> currentFollowThrough = GetPresentedThreatFollowThroughDocumentIds(job.AuthorKingdomId);
		return !(job.PresentedThreatDocumentIds ?? new List<string>()).SequenceEqual(currentPresented, StringComparer.OrdinalIgnoreCase)
			|| !(job.PresentedThreatFollowThroughDocumentIds ?? new List<string>()).SequenceEqual(currentFollowThrough, StringComparer.OrdinalIgnoreCase);
	}

	private string BuildGenerationLegalActionSignature(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return "";
		Kingdom author = ResolveKingdom(job.AuthorKingdomId);
		if (author == null) return "missing_author";
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		PruneInvalidOffers(round);
		WorldDiplomacyDocument responseSource = ResolveDocument(job.SourceDocumentId);
		List<string> ids = new List<string>();
		if (job.IsRelayTurn && round?.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId))
		{
			ids.AddRange(GetResultSettlementActionableTargets(round, author).Select(x => x.StringId));
		}
		else if (job.IsRelayTurn && round?.RelayRouteKingdomIds != null) ids.AddRange(round.RelayRouteKingdomIds);
		else if (!string.IsNullOrWhiteSpace(job.TargetKingdomId)) ids.Add(job.TargetKingdomId);
		else if (job.CandidateKingdomIds?.Count > 0) ids.AddRange(job.CandidateKingdomIds);
		else if (round?.RelayRouteKingdomIds != null) ids.AddRange(round.RelayRouteKingdomIds);
		StringBuilder state = new StringBuilder();
		state.Append(author.StringId).Append('|').Append(round?.RoundId ?? "")
			.Append("|slot=").Append(job.ResultSettlementSlotId ?? "")
			.Append("|current_slot=").Append(round?.ResultSettlementCurrentSlotId ?? "");
		foreach (WorldDiplomacyPolicySignal signal in (round?.AttachedPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.SignalKey))
			.OrderBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase))
		{
			state.Append("|policy=").Append(signal.SignalKey)
				.Append('@').Append(signal.PolicyId ?? "");
		}
		foreach (string id in ids
			.Where(x => !string.IsNullOrWhiteSpace(x)
				&& !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			Kingdom target = ResolveKingdom(id);
			state.Append('\n').Append(id).Append('=');
			if (target == null || target.IsEliminated)
			{
				state.Append("missing");
				continue;
			}
			List<string> actions = BuildLegalDiplomaticDeclarationIntents(
				round,
				author,
				target,
				job.IsRelayTurn,
				job.ResultSettlementSlotId,
				job.IsExternalResponseOnly,
				responseSource);
			bool firstAction = true;
			foreach (string action in actions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
			{
				if (!firstAction) state.Append(',');
				firstAction = false;
				string normalized = NormalizeIntent(action);
				state.Append(normalized);
				string proposalIntent = ResponseIntentToProposalIntent(normalized);
				if (!string.IsNullOrWhiteSpace(proposalIntent)
					&& TryResolveUniqueOpenProposalForRound(
						round, author, target, proposalIntent, out string offerId, out string offerActionId))
				{
					state.Append('@').Append(offerId).Append('#').Append(offerActionId);
				}
				else if (normalized == "comply_ultimatum")
				{
					WorldDiplomacyThreat incoming = FindOpenDiplomaticThreat(target.StringId, author.StringId);
					if (incoming != null && string.Equals(incoming.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
					{
						state.Append('@').Append(incoming.StageDocumentId ?? "")
							.Append('#').Append(incoming.StageActionId ?? "");
					}
				}
			}
		}
		return StablePromptHash(state.ToString());
	}

	private bool HasStaleDiplomaticActionPresentation(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return false;
		return !string.Equals(
			job.PresentedLegalActionSignature ?? "",
			BuildGenerationLegalActionSignature(job),
			StringComparison.Ordinal);
	}

	private bool RefreshDiplomaticActionPresentationAndPrompt(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return false;
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return TryRebuildPendingWorldDiplomacyJob(job);
	}

	private bool RefreshDiplomaticThreatPresentationAndPrompt(WorldDiplomacyJob job)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return false;
		job.PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(job.AuthorKingdomId);
		job.PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(job.AuthorKingdomId);
		job.LlmMessages?.Clear();
		job.SemanticRepairAttempts = 0;
		job.HistoryPrefixHash = "";
		job.IsRunning = false;
		return TryRebuildPendingWorldDiplomacyJob(job);
	}

	private void AppendDiplomaticThreatDynamicContext(StringBuilder sb, Kingdom author, string roundId)
	{
		if (sb == null || author == null) return;
		List<WorldDiplomacyThreat> threats = _storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>();
		int prestige = GetNationalPrestige(author.StringId);
		int reputation = GetInternationalReputation(author.StringId);
		sb.AppendLine("【本国国家威望、国际声誉趋势与未结威慑；内部动态事实，不得在公文中公开数值】");
		sb.AppendLine("本国当前国家威望=" + prestige.ToString(CultureInfo.InvariantCulture)
			+ "/100（" + DescribeNationalPrestige(prestige) + "）。");
		sb.AppendLine("国家威望衡量本国威慑与承诺是否兑现：威望低会削弱威胁可信度，并按档位动态降低正式封臣家族领袖对国王的关系；恢复威望会撤回这部分动态关系惩罚。");
		sb.AppendLine("外国对本国的公开国际声誉档位=" + DescribeInternationalReputation(reputation)
			+ "；当前自然趋势=" + DescribeInternationalReputationNaturalTrend(reputation)
			+ "。这里不提供精确分数；档位与趋势用于规划如何维护、修复或为核心利益消耗这项战略资本，不得为了声誉而沉默、回避合法立场或机械改选动作。");
		List<string> recentReputationReasons = GetRecentOwnInternationalReputationReasons(author.StringId);
		if (recentReputationReasons.Count == 0)
		{
			sb.AppendLine("本国近期没有可供复盘的已结算国际声誉事件；不得编造得失原因。");
		}
		else
		{
			foreach (string recentReason in recentReputationReasons)
			{
				sb.AppendLine("本国近期国际声誉事实=" + recentReason + "。");
			}
		}
		List<WorldDiplomacyStandingChange> recentChanges = (_storage?.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x?.DiplomaticStandingChanges != null && x.IsReadyForPublication)
			.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks)
			.SelectMany(x => x.DiplomaticStandingChanges.AsEnumerable().Reverse())
			.Where(x => x != null
				&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase))
			.Take(4)
			.ToList();
		foreach (WorldDiplomacyStandingChange change in recentChanges)
		{
			sb.AppendLine("近期国家威望结算=" + FormatSignedDelta(change.Delta) + "；原因=" + change.Reason + "。");
		}

		WorldDiplomacyThreat outbound = threats.FirstOrDefault(x => IsOpenDiplomaticThreat(x)
			&& string.Equals(x.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
		if (outbound != null && string.Equals(outbound.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
		{
			Kingdom target = ResolveKingdom(outbound.TargetKingdomId);
			WorldDiplomacyDocument source = ResolveDocument(outbound.StageDocumentId);
			if (string.Equals(outbound.Stage, "warning", StringComparison.OrdinalIgnoreCase))
			{
				sb.AppendLine("强制后果提示：" + KingdomName(target) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国谴责作出不退让决定。本篇就是本国谴责后的下一份宣言，最好对该国升级为战争最后通牒（intent=ultimatum），否则本篇发布后立即扣除10点国家威望。最后通牒必须延续同一军事争端与核心要求，不得更换事项。原谴责标题="
					+ Limit(source?.Title, 80) + "；原谴责要点=" + Limit(source?.Body, 260) + "。");
			}
			else
			{
				sb.AppendLine("强制后果提示：" + KingdomName(target) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国最后通牒作出不退让决定。本篇就是本国通牒后的下一份宣言，最好对该国宣战（intent=declare_war），否则本篇发布后立即扣除25点国家威望，但也要考虑战争的后果。");
			}
		}
		else if (outbound != null)
		{
			sb.AppendLine("本国已有等待对象国一次性决定的" + (outbound.Stage == "ultimatum" ? "战争最后通牒" : "谴责")
				+ "：对象=" + outbound.TargetKingdomId + "=" + KingdomName(ResolveKingdom(outbound.TargetKingdomId))
				+ "，来源=" + outbound.StageDocumentId + "。对象国尚未发布决定；在其决定前不得重复或提前升级该威慑。");
		}

		foreach (WorldDiplomacyThreat incoming in threats.Where(x => IsOpenDiplomaticThreat(x)
			&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.UpdatedDay))
		{
			WorldDiplomacyDocument source = ResolveDocument(incoming.StageDocumentId);
			sb.AppendLine("本国收到的未结" + (incoming.Stage == "ultimatum" ? "最后通牒" : "谴责")
				+ "：发出国=" + incoming.IssuerKingdomId + "=" + KingdomName(ResolveKingdom(incoming.IssuerKingdomId))
				+ "；来源=" + incoming.StageDocumentId
				+ "；标题=" + Limit(source?.Title, 80)
				+ "；要点=" + Limit(source?.Body, 260)
				+ "。选择intent=comply_ultimatum即为无条件退让；任何其他intent即不退让，后续不能反悔。退让会降低本国国家威望，并使本国每个正式封臣家族与当前王族关系下降20点，最后可能导致内战发生，请根据形势、战事、国家性格与长期战略权衡利弊。");
			if (!string.IsNullOrWhiteSpace(incoming.PolicyConditionPolicyId))
			{
				sb.AppendLine("附带政策条件：若本国选择comply_ultimatum，《"
					+ FirstNonEmpty(incoming.PolicyConditionPolicyName, incoming.PolicyConditionPolicyId)
					+ "》将由机制取消。");
			}
		}

		foreach (WorldDiplomacyThreat notice in threats.Where(x => x != null && x.IssuerResolutionNoticePending
			&& string.Equals(x.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)).Take(4))
		{
			sb.AppendLine("已确认：" + KingdomName(ResolveKingdom(notice.TargetKingdomId)) + "已明确服从本国此前的"
				+ (notice.Stage == "ultimatum" ? "最后通牒" : "谴责") + "，后续宣言无需为该威慑宣战或继续升级，也不会因此扣除国家威望。"
				+ (string.Equals(notice.PolicyConditionCancellationStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
					? "附带政策《" + FirstNonEmpty(notice.PolicyConditionPolicyName, notice.PolicyConditionPolicyId) + "》已经取消。"
					: ""));
		}
	}

	private void AppendDiplomaticThreatAnalysisContext(StringBuilder sb, Kingdom author)
	{
		if (sb == null || author == null) return;
		List<WorldDiplomacyThreat> incoming = (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
			.Where(x => IsOpenDiplomaticThreat(x)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.UpdatedDay).ToList();
		if (incoming.Count == 0) return;
		sb.AppendLine("当前可供语义裁定绑定的未决威慑：");
		foreach (WorldDiplomacyThreat threat in incoming)
		{
			WorldDiplomacyDocument source = ResolveDocument(threat.StageDocumentId);
			sb.AppendLine("- 来源=" + threat.StageDocumentId + "|类型=" + threat.Stage
				+ "|发出国=" + threat.IssuerKingdomId + "=" + KingdomName(ResolveKingdom(threat.IssuerKingdomId))
				+ "|标题=" + Limit(source?.Title, 80) + "|要点=" + Limit(source?.Body, 260));
		}
		sb.AppendLine("只有玩家正文以本国为主语，明确、完整、无条件服从其中一项威慑时才裁定comply_ultimatum并绑定该来源；这是一次性决定，部分接受、原则接受、附带要求、反条件、沉默、第三国叙述或任何其他意图都立即算不退让。");
	}

	private static string DescribeNationalPrestige(int value)
	{
		if (value >= 80) return "威望卓著";
		if (value >= 60) return "威望良好";
		if (value >= 40) return "威望受损";
		if (value >= 20) return "威望低下";
		if (value >= 1) return "威信濒临崩溃";
		return "威信尽失";
	}

	private static string DescribeInternationalReputation(int value)
	{
		if (value >= 80) return "广受信赖";
		if (value >= 60) return "评价良好";
		if (value >= 40) return "褒贬不一";
		if (value >= 20) return "信誉不佳";
		return "普遍不受信任";
	}

	private static string DescribeInternationalReputationNaturalTrend(int value)
	{
		if (value >= InternationalReputationFastDecayMinimum) return "快速消散，需要持续以实际成果维护";
		if (value >= InternationalReputationNormalDecayMinimum) return "正常消散，需要定期以实际行为维护";
		if (value >= InternationalReputationSlowDecayMinimum) return "缓慢消散，正逐步回归常态";
		if (value == InternationalReputationNaturalAnchor) return "保持稳定";
		return "缓慢自然修复，但仍需实际行为才能取得更高评价";
	}

	private static string FormatSignedDelta(int value)
	{
		return value > 0
			? "+" + value.ToString(CultureInfo.InvariantCulture)
			: value.ToString(CultureInfo.InvariantCulture);
	}

	private void AppendDiplomaticAuthorDecisionContext(
		StringBuilder sb,
		Kingdom author,
		string roundId)
	{
		if (sb == null || author == null) return;
		string authorId = author.StringId;
		sb.AppendLine("【发文者稳定档案】");
		sb.AppendLine("发文国：" + KingdomName(author) + "（ID=" + authorId + "），统治者：" + RulerName(author));
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		List<string> currentWars = Kingdom.All
			.Where(x => x != null && x != author && !x.IsEliminated
				&& FactionManager.IsAtWarAgainstFaction(author, x))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.StringId + "=" + KingdomName(x))
			.ToList();
		sb.AppendLine("本国当前交战国=" + (currentWars.Count == 0 ? "[]" : "[" + string.Join("；", currentWars) + "]") + "。此项只陈述战争状态，不授予名单外外交动作。");
		AppendDiplomaticThreatDynamicContext(sb, author, roundId);
		sb.AppendLine("【发文者人格与声音】");
		sb.AppendLine(BuildRulerVoiceContext(author));
		sb.AppendLine("按这位统治者的真实取舍起草国家公文；人格体现于利益、信任、代价与行动分寸，国家立场仍以王国、王庭、贵族和臣民表达。");
		sb.AppendLine("【发文国制度、合法性与礼制声音】");
		sb.AppendLine(BuildRealmInstitutionalVoiceContext(author));
		sb.AppendLine("当前游戏身份与政体硬事实高于检索背景；背景只能补充语气，不得改写统治者头衔、政体或发明机构。");
		sb.AppendLine("【权威人物与亲属关系】");
		sb.AppendLine(BuildAuthorRulerFamilyContext(author));
		sb.AppendLine("只有本段列出的直接亲属关系才是事实；仅在本次外交确实涉及王朝、联姻、人质或王室安全时使用。");
		string policySnapshot = WorldDiplomacyPolicyContext.BuildSnapshot(authorId);
		if (!string.IsNullOrWhiteSpace(policySnapshot))
		{
			sb.AppendLine("【发文国政策快照】");
			sb.AppendLine(policySnapshot);
			sb.AppendLine("政策只用于判断当前目标、利益与压力，不证明未明确提供的外交或军事结果。");
		}
	}

	private void AppendDiplomaticTargetDecisionContext(
		StringBuilder sb,
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target,
		bool includePeaceNegotiationTerms,
		IReadOnlyCollection<string> legalActions)
	{
		if (sb == null || author == null || target == null || author == target) return;
		string authorId = author.StringId;
		string targetId = target.StringId;
		WorldDiplomacyRealmRelationProfile relationProfile = GetRealmRelationProfile(author, target);
		WorldDiplomacyBorderRelation border = GetKingdomBorderRelation(author, target);
		WarSituationSnapshot situation = GetWarSituation(author, target);
		string bilateralFamily = BuildBilateralRulerFamilyContext(author, target);
		string recentBattles = BuildRecentBilateralBattleContext(author, target);
		string nativeReasons = BuildRecentNativeSignalContext(authorId, targetId);
		string targetPolicy = WorldDiplomacyPolicyContext.BuildSnapshot(targetId);
		int relation = GetRulerRelation(author, target);
		int culturalFiefs = CountCulturalClaims(author, target);
		int pressure = GetWarPressure(authorId, targetId);
		bool peaceTermsVisible = includePeaceNegotiationTerms
			&& !IsImmediateWarResponsePeaceSuppressed(
				round,
				round?.ResultSettlementCurrentSlotId,
				author,
				target);

		sb.AppendLine("【对象决策硬事实：" + targetId + "】");
		sb.AppendLine("对象国=" + KingdomName(target) + "（ID=" + targetId + "），统治者=" + RulerName(target));
		int targetPrestige = GetNationalPrestige(targetId);
		int targetReputation = GetInternationalReputation(targetId);
		sb.AppendLine("对象国国家威望=" + targetPrestige.ToString(CultureInfo.InvariantCulture)
			+ "/100（" + DescribeNationalPrestige(targetPrestige) + "）；外国对该国的公开国际声誉档位="
			+ DescribeInternationalReputation(targetReputation)
			+ "。国家威望低意味着其威胁较不可信，但也可能迫使其为避免进一步失威而采取更冒险的兑现行动；国际声誉只用于判断其承诺可信度、合作条件与外交风险，不代表友好、和平倾向或不可宣战。");
		string reputationConflictOpportunity = BuildLowReputationConflictOpportunityContext(target, legalActions);
		if (!string.IsNullOrWhiteSpace(reputationConflictOpportunity))
		{
			sb.AppendLine(reputationConflictOpportunity);
		}
		if (!string.IsNullOrWhiteSpace(bilateralFamily)) sb.AppendLine(bilateralFamily);
		sb.AppendLine("当前关系=" + BuildBilateralState(author, target)
			+ "；两国贵族整体关系=" + DescribeRealmRelationProfile(relationProfile)
			+ "；统治者私人关系=" + DescribeRulerRelation(relation)
			+ "；地理关系=" + (border.SharesBorder ? DescribeBorderRelation(border) : "不接壤")
			+ "；总体军力=" + DescribeStrengthBalance(situation.AuthorStrength, situation.TargetStrength) + "。");
		sb.AppendLine("对象国占有的发文国文化城镇/城堡数量=" + culturalFiefs.ToString(CultureInfo.InvariantCulture)
			+ "；边境与政治压力=" + DescribeWarPressure(pressure) + "。这些只供王庭判断，不得写成分数或门槛。");
		if (!string.IsNullOrWhiteSpace(targetPolicy))
		{
			sb.AppendLine("对象国政策=" + Limit(targetPolicy, 700));
		}
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine("近期原版外交动机素材：");
			sb.AppendLine(Limit(nativeReasons, 800));
		}
		sb.AppendLine("近期双边战斗硬事实：");
		sb.AppendLine(Limit(recentBattles, 1500));
		sb.AppendLine("具体战斗只可引用上列硬事实；未列出的战役、战果、兵力、伤亡或俘虏不得补写。");
		if (situation?.IsAtWar == true)
		{
			sb.AppendLine("战争硬性状态：双方已经交战，不得再次宣战。");
			sb.AppendLine(BuildWarDecisionContext(author, target, peaceTermsVisible));
		}
		else
		{
			sb.AppendLine("战争硬性状态：双方当前没有战争；历史敌意、统一诉求或边境摩擦不等于已经交战。");
		}
	}

	private void AppendRelayResponseSourceContext(
		StringBuilder sb,
		WorldDiplomacyRound round,
		Kingdom author,
		WorldDiplomacyDocument responseSource,
		string requiredSourceDocumentId)
	{
		if (sb == null || round == null || author == null) return;
		HashSet<string> sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> answerableOfferSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> answerablePeaceOfferSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrWhiteSpace(responseSource?.DocumentId)) sourceIds.Add(responseSource.DocumentId);
		WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
			.FirstOrDefault(x => x != null
				&& string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase));
		foreach (string id in slot?.SourceDocumentIds ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(id)) sourceIds.Add(id);
		}
		foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
		{
			if (offer == null
				|| !string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				|| string.IsNullOrWhiteSpace(offer.SourceDocumentId)) continue;
			sourceIds.Add(offer.SourceDocumentId);
			answerableOfferSourceIds.Add(offer.SourceDocumentId);
			if (string.Equals(NormalizeIntent(offer.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase))
			{
				answerablePeaceOfferSourceIds.Add(offer.SourceDocumentId);
			}
		}
		if (sourceIds.Count == 0) return;
		List<WorldDiplomacyDocument> sources = (_storage?.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication && sourceIds.Contains(x.DocumentId))
			.OrderByDescending(x => string.Equals(x.DocumentId, requiredSourceDocumentId, StringComparison.OrdinalIgnoreCase))
			.ThenByDescending(x => string.Equals(x.DocumentId, responseSource?.DocumentId, StringComparison.OrdinalIgnoreCase))
			.ThenByDescending(x => answerablePeaceOfferSourceIds.Contains(x.DocumentId))
			.ThenByDescending(x => answerableOfferSourceIds.Contains(x.DocumentId))
			.ThenByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(4)
			.ToList();
		if (sources.Count == 0 && responseSource?.IsReadyForPublication == true) sources.Add(responseSource);
		if (sources.Count == 0) return;
		sb.AppendLine("【本篇回应依据】");
		if (slot?.RelatedKingdomIds?.Count > 0)
		{
			sb.AppendLine("当前处理义务涉及王国=" + string.Join(",", slot.RelatedKingdomIds
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)));
		}
		foreach (WorldDiplomacyDocument source in sources)
		{
			List<string> actionFacts = source.Actions?.Where(x => x != null)
				.Select(x => (x.TargetKingdomId ?? "") + "=" + NormalizeIntent(x.Intent))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList() ?? new List<string>();
			if (actionFacts.Count == 0 && !string.IsNullOrWhiteSpace(source.Intent))
			{
				actionFacts.Add((source.TargetKingdomId ?? "") + "=" + NormalizeIntent(source.Intent));
			}
			int bodyLimit = string.Equals(source.DocumentId, requiredSourceDocumentId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(source.DocumentId, responseSource?.DocumentId, StringComparison.OrdinalIgnoreCase)
				? 1800
				: 700;
			string peaceOfferTerms = BuildPeaceOfferTermsFact(source, author.StringId);
			sb.AppendLine("- 来源=" + source.DocumentId
				+ "|发文国=" + source.AuthorKingdomId
				+ "|动作=" + string.Join("/", actionFacts)
				+ "|标题=" + Limit(source.Title, 100)
				+ (string.IsNullOrWhiteSpace(peaceOfferTerms) ? "" : "|和平原案条款=" + peaceOfferTerms)
				+ "|正文摘要=" + Limit(source.Body, bodyLimit));
		}
	}

	private static string BuildPeaceOfferTermsFact(
		WorldDiplomacyDocument source,
		string targetKingdomId)
	{
		if (source == null || string.IsNullOrWhiteSpace(targetKingdomId)) return "";
		if (source.Actions?.Count > 0)
		{
			WorldDiplomacyDocumentAction action = ResolveSourceActionForTarget(source, targetKingdomId);
			return action != null
				&& string.Equals(NormalizeIntent(action.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
				? "action=" + (action.ActionId ?? "") + ":" + FormatPeaceTermsForPrompt(action.PeaceTerms)
				: "";
		}
		if (string.Equals(NormalizeIntent(source.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(source.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			return FormatPeaceTermsForPrompt(source.PeaceTerms);
		}
		return "";
	}

	private static WorldDiplomacyDocumentAction ResolveSourceActionForTarget(
		WorldDiplomacyDocument source,
		string targetKingdomId)
	{
		if (source?.Actions == null || source.Actions.Count == 0 || string.IsNullOrWhiteSpace(targetKingdomId)) return null;
		return source.Actions.FirstOrDefault(x => x != null
			&& string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildSourceActionFactForTarget(
		WorldDiplomacyDocument source,
		string targetKingdomId)
	{
		if (source == null) return "";
		WorldDiplomacyDocumentAction action = ResolveSourceActionForTarget(source, targetKingdomId);
		if (action != null)
		{
			return "action=" + (action.ActionId ?? "")
				+ "|对象国=" + (action.TargetKingdomId ?? "")
				+ "|意图=" + NormalizeIntent(action.Intent);
		}
		if (source.Actions?.Count > 0
			|| (!string.IsNullOrWhiteSpace(targetKingdomId)
				&& !string.Equals(source.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))) return "";
		return "对象国=" + (source.TargetKingdomId ?? "") + "|意图=" + NormalizeIntent(source.Intent);
	}

	private static string FormatPeaceTermsForPrompt(WorldDiplomacyPeaceTerms terms)
	{
		if (terms == null) return "无附加贡金或割地条款";
		StringBuilder fact = new StringBuilder();
		if (terms.DailyTribute > 0)
		{
			fact.Append("贡金=").Append(terms.TributePayerKingdomId ?? "")
				.Append("→").Append(terms.TributeReceiverKingdomId ?? "")
				.Append(":每日").Append(terms.DailyTribute.ToString(CultureInfo.InvariantCulture))
				.Append("第纳尔,共").Append(Math.Max(0, terms.DurationDays).ToString(CultureInfo.InvariantCulture)).Append("天");
		}
		if (!string.IsNullOrWhiteSpace(terms.CessionSettlementId))
		{
			if (fact.Length > 0) fact.Append("；");
			fact.Append("割地=").Append(terms.CessionFromKingdomId ?? "")
				.Append("→").Append(terms.CessionToKingdomId ?? "")
				.Append(":").Append(terms.CessionSettlementId);
		}
		return fact.Length == 0 ? "无附加贡金或割地条款" : fact.ToString();
	}

	private string BuildAutonomousOpeningPrompt(Kingdom author, string roundId, List<string> candidateIds)
	{
		if (author == null) return "";
		StringBuilder sb = new StringBuilder();
		AppendDiplomaticAuthorDecisionContext(sb, author, roundId);
		WorldDiplomacyRound round = ResolveRound(roundId);
		if (!string.IsNullOrWhiteSpace(round?.ExternalOpeningContext))
		{
			sb.AppendLine("【已经发生的外部外交事件】");
			sb.AppendLine(Limit(round.ExternalOpeningContext, 1800));
			sb.AppendLine("这是可供本国利用或回应的真实事件，但不预定本国的对象、立场或行动。");
		}
		sb.AppendLine("【同次确定本次外交事件参与国】");
		sb.AppendLine("依据发文国国家卡和当前真实局势，自主决定一个或多个对象、动作与参与国；每个对象只能使用候选ID及其当前可选动作。");
		sb.AppendLine("在同一个JSON中填写round_plan。本次参与国总数上限（包括发起国）=" + GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。直接指向的国家必须列入selected_kingdom_ids；只选择确实需要进入本次连续公文的国家，不要凑满。");
		sb.AppendLine("【可选择的外交对象与即时硬事实】");
		foreach (string id in candidateIds ?? new List<string>())
		{
			Kingdom candidate = ResolveKingdom(id);
			if (candidate == null || candidate == author || candidate.IsEliminated || !HasIndependentWorldDiplomacyAuthority(candidate)) continue;
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(author, candidate, round));
			if (FactionManager.IsAtWarAgainstFaction(author, candidate))
			{
				sb.AppendLine("  战争判断=" + CompactPromptFact(BuildWarNegotiationContext(author, candidate), 900));
			}
		}
		int activity = GetActivityLevel();
		sb.AppendLine(activity switch
		{
			0 => "外交活跃程度为低：优先选择代价较低的提案或答复，但仍必须采取至少一项实际动作。",
			2 => "外交活跃程度为高：更积极寻找推进国家目标的外交机会，但不得无理由发动战争。",
			_ => "外交活跃程度为标准：根据国家目标和局势，自主选择至少一项合作、施压、冲突或关系变更动作。"
		});
		return sb.ToString();
	}

	private string BuildGenerationPrompt(
		Kingdom author,
		Kingdom target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		WorldDiplomacyDocument sourceDocument,
		bool isReminder,
		string roundId,
		bool allowUntargeted,
		List<string> roundPlanCandidateIds,
		bool isExternalResponseOnly)
	{
		if (author == null) return "";
		if (target == null && !isResponse)
		{
			return BuildAutonomousOpeningPrompt(author, roundId, roundPlanCandidateIds);
		}
		if (target == null) return "";
		string authorId = author.StringId;
		string targetId = target.StringId;
		string resolvedRoundId = FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId);
		WorldDiplomacyRound activeRound = ResolveRound(resolvedRoundId);
		List<string> relevantKingdomIds = new List<string> { authorId, targetId };
		if (activeRound?.RelayRouteKingdomIds != null) relevantKingdomIds.AddRange(activeRound.RelayRouteKingdomIds);
		string gatheringSnapshot = NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(relevantKingdomIds, 3);
		StringBuilder sb = new StringBuilder();
		AppendDiplomaticAuthorDecisionContext(sb, author, resolvedRoundId);
		sb.AppendLine("【本篇对象与合法动作】");
		sb.AppendLine("主要对象国：" + KingdomName(target) + "（ID=" + targetId + "），统治者：" + RulerName(target));
		List<string> legalActions = BuildLegalDiplomaticDeclarationIntents(
			activeRound,
			author,
			target,
			isRelayTurn: false,
			isExternalResponseOnly: isExternalResponseOnly,
			responseSource: sourceDocument);
		bool statementAllowed = legalActions.Contains("statement", StringComparer.OrdinalIgnoreCase);
		bool exclusivePeaceResponse = IsExclusivePeaceOfferResponseSet(legalActions);
		bool canProposePeace = legalActions.Any(x => string.Equals(
			NormalizeIntent(x),
			"propose_peace",
			StringComparison.OrdinalIgnoreCase));
		sb.AppendLine("本篇合法动作=" + string.Join("、", legalActions) + "。必须选择其中一项。");
		if (exclusivePeaceResponse)
		{
			sb.AppendLine("和平原案只能选择accept_peace原样接受，或reject_peace明确拒绝；不得附加、修改条款或另提和平方案。");
		}
		WorldDiplomacyRoundOffer requiredPeaceOffer = FindRequiredPeaceOfferResponse(
			activeRound,
			author,
			resultSettlementSlotId: null,
			isExternalResponseOnly: isExternalResponseOnly,
			sourceDocumentId: sourceDocument?.DocumentId,
			requireAnyOpenPeaceOffer: false);
		if (requiredPeaceOffer != null)
		{
			sb.AppendLine("本篇必须答复和平原案：来源=" + requiredPeaceOffer.SourceDocumentId
				+ "|action=" + (requiredPeaceOffer.SourceActionId ?? "")
				+ "|提出国=" + requiredPeaceOffer.ProposerKingdomId + "。");
		}
		if (allowUntargeted)
		{
			sb.AppendLine("程序没有预先锁定对象；可从合法候选中选择一个或多个对象，各执行一项动作。");
		}
		if (!string.IsNullOrWhiteSpace(activeRound?.ExternalOpeningContext))
		{
			sb.AppendLine("【本次外交事件的外部起因】");
			sb.AppendLine(Limit(activeRound.ExternalOpeningContext, 1800));
		}
		if (!string.IsNullOrWhiteSpace(gatheringSnapshot))
		{
			sb.AppendLine("【近期相关宴会】");
			sb.AppendLine(Limit(gatheringSnapshot, 900));
			sb.AppendLine("宴会只是可供统治者利用、评价或回应的公开动向，不预设其态度，也不自动产生任何外交结果。");
		}
		AppendDiplomaticTargetDecisionContext(
			sb,
			activeRound,
			author,
			target,
			includePeaceNegotiationTerms: canProposePeace,
			legalActions: legalActions);
		if (isResponse || isExternalResponseOnly || sourceDocument != null)
		{
			AppendOtherKingdomRelationshipContext(sb, author, new[] { targetId });
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
			sb.AppendLine("【同次确定本次外交事件参与国】");
			sb.AppendLine("在起草开场宣言的同时填写round_plan。本次参与国总数上限（包括发起国）=" + GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。宣言明确指向的王国必须优先入选；其余只选确有战争、同盟、贸易、安全或政治利益且能够采取外交行为者，不要为了热闹选满。候选简表：");
			foreach (string candidateId in roundPlanCandidateIds)
			{
				Kingdom candidate = ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(BuildCompactRoundPlanCandidateLine(author, candidate, activeRound));
			}
		}
		if (activeRound != null)
		{
			int age = Math.Max(0, CurrentDay() - activeRound.StartedDay);
			sb.AppendLine("当前外交事件已经持续" + age.ToString(CultureInfo.InvariantCulture) + "天，软时间尺度为" + Math.Max(1, activeRound.SoftEndDay - activeRound.StartedDay).ToString(CultureInfo.InvariantCulture) + "天。"
				+ (statementAllowed
					? "若当前列出statement，可以完成一项结构化谈判动作；必须填写negotiation_move并提出新内容，不能只换一种说法重复立场。"
					: "接近或超过软尺度时，必须选择能够收束交涉的当前合法动作；不得用最终立场式空话拖延。"));
		}
		if (isResponse && sourceDocument != null)
		{
			sb.AppendLine("下列公开外交宣言已经送达；必须从本篇当前合法动作中选择回应：");
			string sourceActionFact = BuildSourceActionFactForTarget(sourceDocument, author.StringId);
			string sourcePeaceTerms = BuildPeaceOfferTermsFact(sourceDocument, author.StringId);
			sb.AppendLine("来源公文ID：" + sourceDocument.DocumentId + "；提出国=" + sourceDocument.AuthorKingdomId
				+ (string.IsNullOrWhiteSpace(sourceActionFact) ? "" : "；与本国相关动作=" + sourceActionFact));
			if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine("和平原案条款：" + sourcePeaceTerms);
			sb.AppendLine("标题：" + sourceDocument.Title);
			sb.AppendLine("正文：" + Limit(sourceDocument.Body, 2200));
		}
		if (isReminder)
		{
			sb.AppendLine("对象国迟迟没有回应。本篇仍必须采取一项实际动作，不得只催促、抱怨或假定对方已经接受。");
		}
		int activity = GetActivityLevel();
		sb.AppendLine(activity switch
		{
			0 => "外交活跃程度为低：优先克制、审慎和现实利益，但严重矛盾仍可升级。",
			2 => "外交活跃程度为高：应更积极提出可回应的主张、合作或冲突方案，但不得无理由发动战争。",
			_ => "外交活跃程度为标准：在合作、冲突和关系变更动作之间按局势自然选择。"
		});
		return sb.ToString();
	}

	private string BuildCompactRoundPlanCandidateLine(
		Kingdom initiator,
		Kingdom candidate,
		WorldDiplomacyRound round = null)
	{
		List<string> actions = round == null
			? BuildPotentialDiplomaticActionIntents(initiator, candidate)
			: BuildLegalDiplomaticActionIntents(round, initiator, candidate);
		string line = BuildCompactDiplomaticRelationshipLine(initiator, candidate)
			+ "；可选动作=" + DescribePotentialDiplomaticActions(actions);
		string reputationConflictOpportunity = BuildLowReputationConflictOpportunityContext(candidate, actions);
		return string.IsNullOrWhiteSpace(reputationConflictOpportunity)
			? line
			: line + "\n  " + reputationConflictOpportunity;
	}

	private string BuildLowReputationConflictOpportunityContext(
		Kingdom target,
		IEnumerable<string> legalActions)
	{
		if (target == null)
		{
			return "";
		}
		int reputation = GetInternationalReputation(target.StringId);
		if (reputation >= LowInternationalReputationThreshold) return "";
		HashSet<string> legalThreatActions = new HashSet<string>((legalActions ?? Enumerable.Empty<string>())
			.Select(NormalizeIntent)
			.Where(x => x is "warning" or "ultimatum"), StringComparer.OrdinalIgnoreCase);
		if (legalThreatActions.Count == 0)
		{
			return "";
		}

		List<string> recentFacts = GetRecentPublicNegativeReputationFacts(target.StringId);
		if (recentFacts.Count == 0)
		{
			return "【国际声誉冲突机会】该国国际声誉低下，但近期没有可援引的具体公开失信事实。"
				+ "这只能作为审慎、疏远或要求保证的背景，不能单独支持外交警告或战争最后通牒；不得编造违约、背盟、敌对或军事行为。";
		}

		string severity = reputation < SevereInternationalReputationThreshold ? "严重国际失信" : "国际声誉低下";
		string legalThreatSummary = string.Join("/", legalThreatActions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return "【国际声誉冲突机会】该国处于“" + DescribeInternationalReputation(reputation) + "”档位，属于" + severity
			+ "。近期主要失信事实为：" + string.Join("；", recentFacts) + "。"
			+ "这构成可利用的外交冲突机会，但不是强制行动。根据本国国家性格、长期战略、军力与现实利益，"
			+ "可以使用当前合法的" + legalThreatSummary + "进行正式谴责、外交警告或索取保证；"
			+ (reputation < SevereInternationalReputationThreshold && legalThreatActions.Contains("ultimatum")
				? "若失信与现实争端都足够严重，也可发出战争最后通牒；"
				: "若不足以升级，也可暂不行动；")
			+ "不得编造未提供的失信与敌对事实。";
	}

	private List<string> GetRecentPublicNegativeReputationFacts(string kingdomId)
	{
		List<string> facts = new List<string>(MaxPromptRecentNegativeReputationFacts);
		string normalizedKingdomId = (kingdomId ?? "").Trim();
		if (normalizedKingdomId.Length == 0 || _storage?.Documents == null)
		{
			return facts;
		}
		int cutoffDay = CurrentDay() - RecentNegativeReputationFactRetentionDays;
		for (int i = _storage.Documents.Count - 1;
			i >= 0 && facts.Count < MaxPromptRecentNegativeReputationFacts;
			i--)
		{
			WorldDiplomacyDocument document = _storage.Documents[i];
			if (document == null
				|| !document.IsReadyForPublication
				|| !document.InternationalReputationSettled
				|| document.InternationalReputationEvaluationDelta >= 0
				|| document.Day < cutoffDay
				|| !string.Equals(document.AuthorKingdomId, normalizedKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string reason = Limit(document.InternationalReputationEvaluationReason, 120);
			if (string.IsNullOrWhiteSpace(reason))
			{
				continue;
			}
			facts.Add(FirstNonEmpty(document.GameDate, FormatCampaignDate(document.Day))
				+ "《" + Limit(FirstNonEmpty(document.Title, "公开外交宣言"), 50) + "》：" + reason);
		}
		return facts;
	}

	private List<string> GetRecentOwnInternationalReputationReasons(string kingdomId)
	{
		List<string> reasons = new List<string>(MaxPromptRecentOwnReputationReasons);
		string normalizedKingdomId = (kingdomId ?? "").Trim();
		if (normalizedKingdomId.Length == 0 || _storage?.Documents == null)
		{
			return reasons;
		}
		int cutoffDay = CurrentDay() - RecentNegativeReputationFactRetentionDays;
		for (int i = _storage.Documents.Count - 1;
			i >= 0 && reasons.Count < MaxPromptRecentOwnReputationReasons;
			i--)
		{
			WorldDiplomacyDocument document = _storage.Documents[i];
			if (document == null
				|| !document.IsReadyForPublication
				|| !document.InternationalReputationSettled
				|| document.InternationalReputationEvaluationDelta == 0
				|| document.Day < cutoffDay
				|| !string.Equals(document.AuthorKingdomId, normalizedKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string reason = Limit(document.InternationalReputationEvaluationReason, 120);
			if (string.IsNullOrWhiteSpace(reason)) continue;
			string direction = document.InternationalReputationEvaluationDelta > 0 ? "改善" : "受损";
			reasons.Add(FirstNonEmpty(document.GameDate, FormatCampaignDate(document.Day))
				+ "《" + Limit(FirstNonEmpty(document.Title, "公开外交宣言"), 50) + "》：评价方向="
				+ direction + "；原因=" + reason);
		}
		return reasons;
	}

	private void AppendOtherKingdomRelationshipContext(
		StringBuilder sb,
		Kingdom author,
		IEnumerable<string> detailedTargetIds)
	{
		if (sb == null || author == null) return;
		HashSet<string> excludedIds = new HashSet<string>(
			(detailedTargetIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase)
		{
			author.StringId
		};
		List<Kingdom> otherKingdoms = Kingdom.All
			.Where(x => x != null
				&& !x.IsEliminated
				&& HasIndependentWorldDiplomacyAuthority(x)
				&& !excludedIds.Contains(x.StringId))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (otherKingdoms.Count == 0) return;

		sb.AppendLine("【本国与其他王国的关系快照】");
		sb.AppendLine("下列信息只供全局判断，不授予额外动作；动作对象仍以本篇当前可选对象为准。");
		foreach (Kingdom other in otherKingdoms)
		{
			sb.AppendLine(BuildCompactDiplomaticRelationshipLine(author, other));
			if (FactionManager.IsAtWarAgainstFaction(author, other))
			{
				sb.AppendLine("  战争态势=" + CompactPromptFact(BuildWarDecisionContext(author, other, false), 650));
			}
		}
	}

	private string BuildCompactDiplomaticRelationshipLine(Kingdom initiator, Kingdom candidate)
	{
		if (initiator == null || candidate == null) return "";
		string policy = CompactPromptFact(WorldDiplomacyPolicyContext.BuildSnapshot(candidate.StringId), 180);
		StringBuilder sb = new StringBuilder();
		WorldDiplomacyRealmRelationProfile relationProfile = GetRealmRelationProfile(initiator, candidate);
		WorldDiplomacyBorderRelation border = GetKingdomBorderRelation(initiator, candidate);
		WarSituationSnapshot strengthSituation = GetWarSituation(initiator, candidate);
		int candidateReputation = GetInternationalReputation(candidate.StringId);
		sb.Append("- ").Append(candidate.StringId).Append('=').Append(KingdomName(candidate))
			.Append("；与本国=").Append(BuildBilateralState(initiator, candidate))
			.Append("；两国贵族整体关系=").Append(DescribeRealmRelationProfile(relationProfile))
			.Append("；统治者私人关系=").Append(DescribeRulerRelation(GetRulerRelation(initiator, candidate)))
			.Append("；地理关系=").Append(border.SharesBorder ? DescribeBorderRelation(border) : "不接壤")
			.Append("；总体军力=").Append(DescribeStrengthBalance(strengthSituation.AuthorStrength, strengthSituation.TargetStrength))
			.Append("；国家威望=").Append(GetNationalPrestige(candidate.StringId).ToString(CultureInfo.InvariantCulture))
			.Append("；外国对其公开国际声誉档位=").Append(DescribeInternationalReputation(candidateReputation));
		if (!string.IsNullOrWhiteSpace(policy)) sb.Append("；政策倾向=").Append(policy);
		return sb.ToString();
	}

	private string BuildWarDecisionContext(
		Kingdom author,
		Kingdom target,
		bool includePeaceNegotiationTerms)
	{
		WarSituationSnapshot snapshot = GetWarSituation(author, target);
		if (snapshot?.IsAtWar != true) return "";
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【仅供统治者判断的战争态势】战争已经" + DescribeWarDuration(snapshot.WarDays) + "。");
		sb.AppendLine("双方总体军力=" + DescribeStrengthBalance(snapshot.AuthorStrength, snapshot.TargetStrength)
			+ "；近期战局=" + DescribeWarProgress(snapshot.AuthorProgress, snapshot.TargetProgress)
			+ "；发文国=" + DescribeOtherWarBurden(snapshot.AuthorOtherWars)
			+ "；对象国=" + DescribeOtherWarBurden(snapshot.TargetOtherWars) + "。这些是综合判断，只能转写成世界内措辞，不得公开任何评分、分差、开放度或战力数值。");
		if (includePeaceNegotiationTerms)
		{
			List<Settlement> targetCanCede = BuildCessionCandidates(target, author, snapshot.TargetCessionScore);
			List<Settlement> authorCanCede = BuildCessionCandidates(author, target, snapshot.AuthorCessionScore);
			sb.AppendLine("【仅在本篇可选和平动作时使用的议和条件】发文国所受议和压力=" + DescribePeacePressure(snapshot.AuthorPeacePressure)
				+ "；对象国所受议和压力=" + DescribePeacePressure(snapshot.TargetPeacePressure) + "。");
		sb.AppendLine("贡金可与割地并存。参考每日贡金：若发文国付款约" + snapshot.AuthorSuggestedTribute + "，若对象国付款约" + snapshot.TargetSuggestedTribute + "；可以谈判但不得超出任务给出的合法上限。");
		sb.AppendLine("对象国当前可合法提出割让给发文国的领地=" + FormatCessionCandidates(targetCanCede) + "；发文国当前可合法提出割让给对象国的领地=" + FormatCessionCandidates(authorCanCede) + "。清单为空时不得提出或同意割地，也不得编造城名；优先考虑战争中尚未收复的失地。城镇只有在战局严重不利时才会进入清单。");
		}
		return sb.ToString().TrimEnd();
	}

	private string BuildWarNegotiationContext(Kingdom author, Kingdom target)
	{
		return BuildWarDecisionContext(author, target, true);
	}

	private static string DescribeWarDuration(int days)
	{
		if (days < 14) return "持续了不到半个月";
		if (days < 35) return "持续了约一个月";
		if (days < 84) return "持续了数月";
		if (days < DaysPerYear * 2) return "持续了超过一年";
		return "延续了多年";
	}

	private static string DescribePeacePressure(float pressure)
	{
		if (pressure < 55f) return "几乎无意谈和";
		if (pressure < 125f) return "暂不急于谈和，但会衡量条件";
		if (pressure < 210f) return "愿意认真考虑和平条件";
		return "迫切希望以可接受条件结束战争";
	}

	private static string DescribeStrengthBalance(float authorStrength, float targetStrength)
	{
		float ratio = authorStrength / Math.Max(1f, targetStrength);
		if (ratio >= 1.75f) return "发文国明显占优";
		if (ratio >= 1.2f) return "发文国略占优势";
		if (ratio <= 0.57f) return "发文国明显处于劣势";
		if (ratio <= 0.83f) return "发文国略处下风";
		return "大体势均力敌";
	}

	private static string DescribeWarProgress(float authorProgress, float targetProgress)
	{
		float difference = authorProgress - targetProgress;
		if (difference >= 20f) return "发文国取得了明显主动";
		if (difference >= 5f) return "发文国稍占上风";
		if (difference <= -20f) return "发文国明显受挫";
		if (difference <= -5f) return "发文国稍处下风";
		return "尚未分出明显高下";
	}

	private static string DescribeOtherWarBurden(int otherWars)
	{
		if (otherWars <= 0) return "没有其他战线牵制";
		if (otherWars == 1) return "另有一条战线需要兼顾";
		return "正受到多线战争牵制";
	}

	private static string DescribeRulerRelation(int relation)
	{
		if (relation <= -60) return "彼此仇视";
		if (relation <= -20) return "关系紧张";
		if (relation < 20) return "关系冷淡";
		if (relation < 60) return "关系尚可";
		return "彼此亲近";
	}

	private string DescribeWarPressure(int pressure)
	{
		if (pressure < 20) return "压力较低";
		if (pressure < 60) return "摩擦正在积累";
		if (pressure < 120) return "压力很高";
		return "局势已经十分危险";
	}

	private static string FormatCessionCandidates(IEnumerable<Settlement> settlements)
	{
		List<string> values = (settlements ?? Enumerable.Empty<Settlement>()).Where(x => x != null).Select(x => (x.StringId ?? "") + "=" + (x.Name?.ToString() ?? "未知")).ToList();
		return values.Count == 0 ? "[]" : "[" + string.Join("；", values) + "]";
	}

	private static string BuildRulerVoiceContext(Kingdom kingdom)
	{
		Hero ruler = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		if (ruler == null)
		{
			return "RulerPersona{name=未知统治者,culture=" + (kingdom?.Culture?.Name?.ToString() ?? "未知") + ",note=没有可用人物档案，不得编造个人经历}";
		}
		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out string background);
		string compactPersonality = CompactPromptFact(personality, 280);
		string compactBackground = CompactPromptFact(background, 420);
		string title = FirstNonEmpty(
			kingdom?.EncyclopediaRulerTitle?.ToString(),
			ruler.Clan?.Name?.ToString(),
			"统治者");
		return "RulerPersona{name=" + (ruler.Name?.ToString() ?? "未知")
			+ ",kingdom=" + KingdomName(kingdom)
			+ ",culture=" + (kingdom?.Culture?.Name?.ToString() ?? ruler.Culture?.Name?.ToString() ?? "未知")
			+ ",title=" + CompactPromptFact(title, 80)
			+ ",traits=" + BuildRulerVoiceTraitSummary(ruler)
			+ ",personality=" + FirstNonEmpty(compactPersonality, "未提供专属个性档案")
			+ ",background=" + FirstNonEmpty(compactBackground, "未提供专属背景档案，不得自行补写经历")
			+ "}";
	}

	private string BuildRealmInstitutionalVoiceContext(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "";
		}
		Hero ruler = kingdom.Leader ?? kingdom.RulingClan?.Leader;
		string kingdomName = KingdomName(kingdom);
		string cultureName = kingdom.Culture?.Name?.ToString() ?? ruler?.Culture?.Name?.ToString() ?? "未知";
		string rulerTitle = ResolveRealmRulerTitle(kingdom, ruler);
		string governmentHardFact = BuildCanonicalRealmGovernmentHardFact(kingdom, rulerTitle);
		string lore = "";
		try
		{
			KnowledgeLibraryBehavior library = KnowledgeLibraryBehavior.Instance;
			if (library != null && ruler != null)
			{
				long ruleVersion = library.GetRuleDataVersionForExternal();
				if (_realmInstitutionalVoiceRuleVersion != ruleVersion)
				{
					_realmInstitutionalVoiceCache.Clear();
					_realmInstitutionalVoiceRuleVersion = ruleVersion;
				}
				string cacheKey = (kingdom.StringId ?? "") + "|" + (ruler.StringId ?? "") + "|" + (kingdom.Culture?.StringId ?? "") + "|" + rulerTitle;
				if (_realmInstitutionalVoiceCache.TryGetValue(cacheKey, out string cached))
				{
					return cached ?? "";
				}
				MentionedWorldEntities entities = new MentionedWorldEntities();
				foreach (string term in new[]
				{
					kingdomName,
					kingdom.StringId,
					ruler.Name?.ToString(),
					ruler.StringId
				})
				{
					if (!string.IsNullOrWhiteSpace(term)
						&& !entities.Entities.Any(x => string.Equals(x, term, StringComparison.OrdinalIgnoreCase)))
					{
						entities.Entities.Add(term.Trim());
					}
				}
				string query = kingdomName + " " + cultureName + " " + (ruler.Name?.ToString() ?? "")
					+ " 政体 统治合法性 王庭 贵族 议政 继承 外交礼制 国家称谓";
				lore = library.BuildLoreContextWithoutPlayerContext(query, ruler, "world_diplomacy_realm_voice", entities);
				string result = BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore);
				if (_realmInstitutionalVoiceCache.Count >= 32)
				{
					_realmInstitutionalVoiceCache.Clear();
				}
				_realmInstitutionalVoiceCache[cacheKey] = result;
				return result;
			}
		}
		catch
		{
			lore = "";
		}
		return BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore);
	}

	private static string ResolveRealmRulerTitle(Kingdom kingdom, Hero ruler)
	{
		string kingdomId = (kingdom?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.Equals(kingdomId, "empire_n", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase))
		{
			return ruler?.IsFemale == true ? "女皇" : "皇帝";
		}
		return FirstNonEmpty(kingdom?.EncyclopediaRulerTitle?.ToString(), "统治者");
	}

	private static string BuildCanonicalRealmGovernmentHardFact(Kingdom kingdom, string rulerTitle)
	{
		string title = FirstNonEmpty(rulerTitle, "统治者");
		switch ((kingdom?.StringId ?? "").Trim().ToLowerInvariant())
		{
			case "empire_n":
				return "北帝国实行以元老院及元老政治传统为权力基础的帝制；最高统治者个人头衔为" + title + "。元老院是国家机构，不是统治者的个人身份；不得把统治者称为元老、议员或执政官。";
			case "empire_w":
				return "西帝国实行以军队拥立、军功与军人政治传统为合法性基础的帝制；最高统治者个人头衔为" + title + "，不得改称国王、将军、元老或执政官。西帝国不是元老院制。";
			case "empire_s":
				return "南帝国实行以皇室世袭与君主权威为合法性基础的帝制君主制；最高统治者个人头衔为" + title + "，不得改称国王、女王、元老、议员或执政官。南帝国不是元老院制。";
			default:
				return "当前游戏身份确认的最高统治者个人头衔为" + title + "；该头衔是硬事实，任何机构称谓或人物背景都不得将其替换。";
		}
	}

	private static string BuildRealmInstitutionalVoiceText(string kingdomName, string cultureName, string rulerTitle, string governmentHardFact, string lore)
	{
		return "RealmInstitutionalVoice{kingdom=" + kingdomName
			+ ",culture=" + cultureName
			+ ",ruler_title_hard_fact=" + CompactPromptFact(rulerTitle, 80)
			+ ",government_hard_fact=" + CompactPromptFact(governmentHardFact, 520)
			+ ",imported_lore=" + FirstNonEmpty(CompactPromptFact(lore, 1100), "未命中；只可使用王国、王庭、贵族、臣民等中性称谓，不得发明具体制度")
			+ ",precedence=硬事实高于编年史检索片段；若有冲突必须舍弃检索片段，机构名称不得充当统治者个人头衔"
			+ "}";
	}

	private static string BuildRulerVoiceTraitSummary(Hero ruler)
	{
		if (ruler == null)
		{
			return "未知";
		}
		try
		{
			List<string> traits = new List<string>();
			AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Mercy), "仁慈", "冷酷");
			AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Valor), "勇敢", "谨慎避险");
			AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Honor), "重视荣誉与承诺", "善用权谋");
			AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Generosity), "慷慨", "看重积蓄与代价");
			AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Calculating), "精于计算", "直率果断");
			return traits.Count == 0 ? "无明显倾向" : string.Join("、", traits);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static void AppendVoiceTrait(List<string> target, int value, string positive, string negative)
	{
		if (value > 0)
		{
			target?.Add(positive);
		}
		else if (value < 0)
		{
			target?.Add(negative);
		}
	}

	private static string CompactPromptFact(string value, int maxChars)
	{
		string compact = string.Join(" ", (value ?? "")
			.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
		return Limit(compact, Math.Max(0, maxChars));
	}

	private static string BuildAuthorRulerFamilyContext(Kingdom author)
	{
		Hero authorRuler = author?.Leader ?? author?.RulingClan?.Leader;
		return "AuthorRulerFamily{" + BuildHeroFamilySnapshot(authorRuler) + "}";
	}

	private static string BuildBilateralRulerFamilyContext(Kingdom author, Kingdom target)
	{
		Hero authorRuler = author?.Leader ?? author?.RulingClan?.Leader;
		Hero targetRuler = target?.Leader ?? target?.RulingClan?.Leader;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("TargetRulerFamily{" + BuildHeroFamilySnapshot(targetRuler) + "}");
		sb.Append("DirectRelationshipBetweenRulers{" + ResolveDirectHeroRelationship(authorRuler, targetRuler) + "}");
		return sb.ToString();
	}

	private static string BuildHeroFamilySnapshot(Hero hero)
	{
		if (hero == null)
		{
			return "hero=未知,parents=[],spouse=无,children=[]";
		}
		List<string> parents = new List<string>();
		if (hero.Father != null)
		{
			parents.Add("父亲:" + FormatHeroFamilyIdentity(hero.Father));
		}
		if (hero.Mother != null)
		{
			parents.Add("母亲:" + FormatHeroFamilyIdentity(hero.Mother));
		}
		List<string> children = (hero.Children ?? Enumerable.Empty<Hero>())
			.Where(x => x != null)
			.Select(FormatHeroFamilyIdentity)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(16)
			.ToList();
		return "hero=" + FormatHeroFamilyIdentity(hero)
			+ ",parents=[" + string.Join(";", parents) + "]"
			+ ",spouse=" + (hero.Spouse == null ? "无" : FormatHeroFamilyIdentity(hero.Spouse))
			+ ",children=[" + string.Join(";", children) + "]";
	}

	private static string FormatHeroFamilyIdentity(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		return (hero.Name?.ToString() ?? "未知")
			+ "(id=" + (hero.StringId ?? "")
			+ "," + (hero.IsAlive ? "在世" : "已故") + ")";
	}

	private static string ResolveDirectHeroRelationship(Hero first, Hero second)
	{
		if (first == null || second == null)
		{
			return "unknown";
		}
		if (first == second)
		{
			return "same_person";
		}
		if (first.Spouse == second || second.Spouse == first)
		{
			return "spouses";
		}
		if (first.Father == second || first.Mother == second)
		{
			return "target_is_author_parent";
		}
		if (second.Father == first || second.Mother == first)
		{
			return "target_is_author_child";
		}
		bool shareFather = first.Father != null && first.Father == second.Father;
		bool shareMother = first.Mother != null && first.Mother == second.Mother;
		return shareFather || shareMother ? "siblings" : "none_listed";
	}

	private string BuildRecentBilateralBattleContext(Kingdom author, Kingdom target)
	{
		if (author == null || target == null)
		{
			return "双方身份无效；不得陈述具体战斗。";
		}
		int cutoff = CurrentDay() - RecentBattleRetentionDays;
		List<WorldDiplomacyBattleFact> battles = (_storage.RecentBattles ?? new List<WorldDiplomacyBattleFact>())
			.Where(x => IsBilateralBattleFact(x, author.StringId, target.StringId) && x.Day >= cutoff)
			.OrderByDescending(x => x.Day)
			.Take(MaxPromptRecentBattles)
			.ToList();
		if (battles.Count == 0)
		{
			return "最近" + RecentBattleRetentionDays.ToString(CultureInfo.InvariantCulture)
				+ "个游戏日内没有记录到双方之间已经结束的战斗。双方可能仍处于战争状态，但不得声称发生过任何具体战役或给出战果数字。";
		}
		return string.Join("\n", battles.Select(FormatBattleFactForPrompt));
	}

	private static bool IsBilateralBattleFact(WorldDiplomacyBattleFact fact, string firstKingdomId, string secondKingdomId)
	{
		if (fact == null)
		{
			return false;
		}
		bool firstAttacker = fact.AttackerKingdomIds?.Contains(firstKingdomId, StringComparer.OrdinalIgnoreCase) == true;
		bool firstDefender = fact.DefenderKingdomIds?.Contains(firstKingdomId, StringComparer.OrdinalIgnoreCase) == true;
		bool secondAttacker = fact.AttackerKingdomIds?.Contains(secondKingdomId, StringComparer.OrdinalIgnoreCase) == true;
		bool secondDefender = fact.DefenderKingdomIds?.Contains(secondKingdomId, StringComparer.OrdinalIgnoreCase) == true;
		return (firstAttacker && secondDefender) || (firstDefender && secondAttacker);
	}

	private static string FormatBattleFactForPrompt(WorldDiplomacyBattleFact fact)
	{
		string attackers = FormatBattleKingdomNames(fact?.AttackerKingdomIds);
		string defenders = FormatBattleKingdomNames(fact?.DefenderKingdomIds);
		string winner = string.Equals(fact?.WinnerSide, "attacker", StringComparison.OrdinalIgnoreCase) ? attackers : defenders;
		string attackerLeaders = string.Join("、", fact?.AttackerLeaderNames ?? new List<string>());
		string defenderLeaders = string.Join("、", fact?.DefenderLeaderNames ?? new List<string>());
		return "- " + FirstNonEmpty(fact?.GameDate, FormatCampaignDate(fact?.Day ?? 0))
			+ "，" + FirstNonEmpty(fact?.Location, "野外") + "的" + FirstNonEmpty(fact?.BattleType, "战斗")
			+ "：攻方=" + attackers + "，守方=" + defenders + "，胜方=" + winner
			+ (string.IsNullOrWhiteSpace(attackerLeaders) ? "" : "，攻方已记录领主=" + attackerLeaders)
			+ (string.IsNullOrWhiteSpace(defenderLeaders) ? "" : "，守方已记录领主=" + defenderLeaders)
			+ "。本记录没有提供可靠兵力、伤亡或俘虏信息，不得补写；列出的参战领主不代表其已被俘。";
	}

	private static string FormatBattleKingdomNames(IEnumerable<string> kingdomIds)
	{
		List<string> names = (kingdomIds ?? Enumerable.Empty<string>())
			.Select(id => KingdomName(ResolveKingdom(id)))
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		return names.Count == 0 ? "未知王国" : string.Join("、", names);
	}

	private static string BuildAnalysisSystemPrompt(string commonContract)
	{
		StringBuilder sb = CreateSystemPromptBuilder(commonContract);
		sb.AppendLine(DiplomacyAnalysisTaskMarker + "最后一条消息的 MODE=ANALYZE 决定本次任务和输出结构。");
		return sb.ToString().TrimEnd();
	}

	private static string BuildAnalysisModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("这份玩家宣言已经正式公开发布。只负责理解和提取语义，不得决定是否允许发布，也不得因没有游戏机制动作而退回公文；玩家文风偏好不参与语义裁判。");
		sb.AppendLine("warning表示谴责，不是劝告、关心或善意提醒；只有明确要求停止具体敌对或军事行为，并说明否则升级最后通牒或战争时才可使用。ultimatum表示战争最后通牒；已经开战用declare_war。");
		sb.AppendLine("优先提取会登记或执行机制状态的实际外交动作，不得替作者臆造动作。正文没有这类动作时仍返回status=success：普通立场用statement，一般谴责用condemn，明确正式道歉用apology，明确正式让步用concession；这些公开语义不等于宣战、提案、接受或拒绝。");
		sb.AppendLine("若材料列出当前待本国答复的正式提案，明确接受或拒绝时必须使用对应accept_*或reject_*并绑定原提出国和来源。和平原案只能原样接受或明确拒绝，不得改写条款或另提和平方案；其他提案只能使用材料列出的当前合法动作。");
		sb.AppendLine("只有正文明确、肯定且无条件地服从材料列出的未决谴责或最后通牒时，intent才可使用comply_ultimatum，commitment用binding，primary_target_kingdom_id填发出国，并把当前阶段来源公文ID填入responding_to_threat_document_id。对象国本篇就是一次性决定；含糊、沉默、附带条件、反条件、仅愿继续谈判或任何其他intent一律是不退让，该字段留空。");
		sb.AppendLine("同时生成title_summary：以发文国统治者的立场简洁概括公告核心，不使用书信标题，不超过20个汉字。");
		sb.AppendLine("addressed_kingdom_ids列出被直接点名、要求答复或承受正式主张的国家；mentioned_kingdom_ids只列被谈及但未被直接要求回应的国家。只允许使用用户消息给出的王国ID。");
		sb.AppendLine("propose_peace的peace_terms只提取正文明确条款；accept_peace由系统继承原案。领地必须来自允许清单，清单为空就留空。");
		sb.AppendLine("在完成语义提取后，对这篇已经公开的宣言做事后国际声誉评估；该评估绝不能改变或否定宣言。每篇宣言都必须产生非零评价，只能填写-10到-1或1到10，不得为0。履约、可执行妥协、有效调停、承担责任和可靠协作通常提高；违约、反复改条件、欺骗、拖延、滥用威胁和违反停战通常降低。单纯拒绝要求时，根据是否及时、明确、前后一致以及是否提供可继续谈判的说明判定最低幅度±1；重复没有新条件、新解释、新行动或谈判进展的空洞表态判-1。reason只写简短事实理由。");
		sb.AppendLine("只输出一个JSON对象，不要解释或代码围栏：");
		sb.AppendLine("{\"status\":\"success\",\"title_summary\":\"公告要点标题\",\"responding_to_offer_document_id\":\"提议来源公文ID或空字符串\",\"responding_to_threat_document_id\":\"退让对象的谴责或最后通牒来源公文ID或空字符串\",\"primary_target_kingdom_id\":\"王国ID或空字符串\",\"addressed_kingdom_ids\":[\"王国ID\"],\"mentioned_kingdom_ids\":[\"王国ID\"],\"intent\":\"statement|condemn|apology|concession|warning|ultimatum|comply_ultimatum|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"requires_response\":true,\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"international_reputation_delta\":1,\"international_reputation_reason\":\"事后评估理由\",\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID或空\",\"tribute_receiver_kingdom_id\":\"ID或空\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID或空\",\"cession_to_kingdom_id\":\"ID或空\",\"cession_settlement_id\":\"ID或空\"}}");
		return sb.ToString().TrimEnd();
	}

	private string BuildAnalysisPrompt(WorldDiplomacyDocument document)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("发文国：" + document.AuthorKingdomName + "（ID=" + document.AuthorKingdomId + "）");
		Kingdom documentAuthor = ResolveKingdom(document.AuthorKingdomId);
		WorldDiplomacyRound analysisRound = ResolveRound(document.RoundId);
		if (document.IsPlayerAuthored) PruneInvalidOffers(analysisRound);
		if (documentAuthor != null) AppendDiplomaticThreatAnalysisContext(sb, documentAuthor);
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		sb.AppendLine("候选对象国：");
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated && !string.Equals(x.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase))
		{
			sb.AppendLine("- " + kingdom.StringId + " = " + KingdomName(kingdom));
		}
		if (!string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
			sb.AppendLine("系统当前候选主要对象：" + document.TargetKingdomId + " = " + document.TargetKingdomName);
			Kingdom author = ResolveKingdom(document.AuthorKingdomId);
			Kingdom candidateTarget = ResolveKingdom(document.TargetKingdomId);
			if (author != null && candidateTarget != null && FactionManager.IsAtWarAgainstFaction(author, candidateTarget))
			{
				bool canProposePeace = BuildLegalDiplomaticActionIntents(analysisRound, author, candidateTarget)
					.Any(x => string.Equals(NormalizeIntent(x), "propose_peace", StringComparison.OrdinalIgnoreCase));
				sb.AppendLine(BuildWarDecisionContext(author, candidateTarget, canProposePeace));
			}
		}
		if (document.IsPlayerAuthored)
		{
			WorldDiplomacyRoundOffer requiredPlayerPeaceOffer = FindRequiredPeaceOfferResponse(
				analysisRound,
				documentAuthor,
				document.ResultSettlementSlotId,
				isExternalResponseOnly: false,
				sourceDocumentId: document.SourceDocumentId,
				requireAnyOpenPeaceOffer: true);
			List<WorldDiplomacyRoundOffer> openOffers = (analysisRound?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
				.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => requiredPlayerPeaceOffer != null
					&& string.Equals(x.SourceDocumentId, requiredPlayerPeaceOffer.SourceDocumentId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.SourceActionId ?? "", requiredPlayerPeaceOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(x => x.CreatedDay)
				.Take(4)
				.ToList();
			if (openOffers.Count > 0)
			{
				sb.AppendLine("当前待本国正式答复的提案：");
				foreach (WorldDiplomacyRoundOffer offer in openOffers)
				{
					WorldDiplomacyDocument source = ResolveDocument(offer.SourceDocumentId);
					bool isPeaceOffer = string.Equals(
						NormalizeIntent(offer.Intent),
						"propose_peace",
						StringComparison.OrdinalIgnoreCase);
					sb.AppendLine("- 来源=" + offer.SourceDocumentId + "|类型=" + offer.Intent
						+ "|提出国=" + offer.ProposerKingdomId + "=" + KingdomName(ResolveKingdom(offer.ProposerKingdomId))
						+ "|标题=" + Limit(source?.Title, 80) + "|要点=" + Limit(source?.Body, 240)
						+ (isPeaceOffer
							? "|原案条款=" + FormatPeaceTermsForPrompt(ResolveOfferedPeaceTerms(source, offer.SourceActionId))
								+ "|答复=原样接受或明确拒绝"
							: ""));
				}
				sb.AppendLine("接受或拒绝必须绑定对应来源；和平原案不得改写或另提方案，其他动作以当前合法状态为准。只有评论且没有实际动作时按其语义返回statement或condemn，公文仍然有效。");
			}
		}
		WorldDiplomacyDocument sourceDocument = ResolveDocument(document.SourceDocumentId);
		if (sourceDocument != null)
		{
			sb.AppendLine("该公文正在回应：");
			string sourceActionFact = BuildSourceActionFactForTarget(sourceDocument, document.AuthorKingdomId);
			string sourcePeaceTerms = BuildPeaceOfferTermsFact(sourceDocument, document.AuthorKingdomId);
			if (!string.IsNullOrWhiteSpace(sourceActionFact)) sb.AppendLine("与本国相关动作=" + sourceActionFact);
			if (!string.IsNullOrWhiteSpace(sourcePeaceTerms)) sb.AppendLine("和平原案条款=" + sourcePeaceTerms);
			sb.AppendLine(sourceDocument.AuthorKingdomName + "《" + sourceDocument.Title + "》：" + Limit(sourceDocument.Body, 1400));
		}
		sb.AppendLine("公文标题：" + document.Title);
		sb.AppendLine("公文正文：" + Limit(document.Body, 3000));
		sb.AppendLine("【MODE=ANALYZE】");
		sb.AppendLine(BuildAnalysisModeContract());
		return sb.ToString().TrimEnd();
	}

	private static string BuildTokenCompressionPrompt(string batchId, long throughSequence, long tokenCount, int summaryTargetTokens, long protectedTokens)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【本次压缩参数】");
		sb.AppendLine("压缩批次=" + (batchId ?? "") + "；覆盖截止seq=" + Math.Max(0L, throughSequence).ToString(CultureInfo.InvariantCulture)
			+ "；当前估算tokens=" + Math.Max(0L, tokenCount).ToString(CultureInfo.InvariantCulture)
			+ "；近期硬事实预算占用tokens=" + Math.Max(0L, protectedTokens).ToString(CultureInfo.InvariantCulture)
			+ "；summary目标上限tokens=" + Math.Max(1, summaryTargetTokens).ToString(CultureInfo.InvariantCulture) + "。");
		sb.AppendLine("【MODE=COMPACT】");
		sb.AppendLine("只激活第一条system消息中的MODE=COMPACT固定任务合同，并只输出该合同规定的JSON对象。");
		return sb.ToString().TrimEnd();
	}

	private string BuildFallbackAnalysisJson(WorldDiplomacyJob job)
	{
		WorldDiplomacyDocument document = ResolveDocument(job?.DocumentId);
		return new JObject
		{
			["status"] = "fallback",
			["title_summary"] = BuildFallbackDocumentTitle(document, "statement"),
			["responding_to_offer_document_id"] = "",
			["responding_to_threat_document_id"] = "",
			["primary_target_kingdom_id"] = FirstNonEmpty(document?.TargetKingdomId, job?.TargetKingdomId),
			["addressed_kingdom_ids"] = new JArray(),
			["mentioned_kingdom_ids"] = new JArray(),
			["intent"] = "statement",
			["commitment"] = "non_binding",
			["requires_response"] = false,
			["tone"] = "neutral",
			["confidence"] = 0.0,
			["international_reputation_delta"] = 0,
			["international_reputation_reason"] = "语义分析服务未完成评估，交由本地结构化规则给出非零评价。"
		}.ToString(Formatting.None);
	}

	private string BuildFallbackAnnualSummary(int year, List<string> ids)
	{
		List<WorldDiplomacyDocument> documents = _storage.Documents
			.Where(x => x != null && (ids ?? new List<string>()).Contains(x.DocumentId))
			.OrderBy(x => x.Day)
			.ToList();
		List<string> major = documents.Where(IsMajorDiplomaticDocument).Select(BuildCompactDocumentMemoryLine).Take(18).ToList();
		if (major.Count == 0)
		{
			major = documents.Select(BuildCompactDocumentMemoryLine).Take(10).ToList();
		}
		return major.Count == 0
			? "这一年没有留下值得长期记录的重大外交变化。"
			: string.Join("；", major) + "。";
	}

	private static string BuildExternalFactBody(string action, Kingdom initiator, Kingdom target, string reason)
	{
		string result = action switch
		{
			"declare_war" => KingdomName(initiator) + "的统治者在面对面交涉中向" + KingdomName(target) + "正式宣战。",
			"propose_peace" or "accept_peace" => KingdomName(initiator) + "与" + KingdomName(target) + "已经通过面对面交涉达成和平。",
			"propose_alliance" or "accept_alliance" => KingdomName(initiator) + "与" + KingdomName(target) + "已经通过面对面交涉缔结同盟。",
			"break_alliance" => KingdomName(initiator) + "在面对面交涉后终止了与" + KingdomName(target) + "的同盟。",
			"propose_trade" or "accept_trade" => KingdomName(initiator) + "与" + KingdomName(target) + "已经通过面对面交涉缔结贸易协定。",
			"cancel_trade" => KingdomName(initiator) + "在面对面交涉后终止了与" + KingdomName(target) + "的贸易协定。",
			_ => KingdomName(initiator) + "与" + KingdomName(target) + "完成了一次具有公开影响的面对面外交交涉。"
		};
		return result + (string.IsNullOrWhiteSpace(reason) ? "" : "\n\n缘由：" + reason.Trim());
	}

	private static string BuildNativeDecisionReason(Kingdom source, Kingdom target, KingdomDecision decision, string action)
	{
		List<string> parts = new List<string>();
		try
		{
			string title = decision.GetGeneralTitle()?.ToString();
			if (!string.IsNullOrWhiteSpace(title))
			{
				parts.Add(title);
			}
		}
		catch
		{
		}
		try
		{
			if (action == "declare_war")
			{
				TextObject reason;
				float score = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(source, target, source.RulingClan, out reason, true);
				parts.Add(score > 0f ? "王庭认为宣战有现实理由" : "王庭认为宣战理由不足");
				if (!string.IsNullOrWhiteSpace(reason?.ToString()))
				{
					parts.Add("原版理由=" + reason);
				}
			}
			else if (action == "propose_peace")
			{
				float score = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(source, target);
				parts.Add(score > 0f ? "王庭倾向寻找和平条件" : "王庭暂不倾向议和");
			}
		}
		catch
		{
		}
		int relation = GetRulerRelation(source, target);
		parts.Add("统治者私人关系=" + DescribeRulerRelation(relation));
		int claims = CountCulturalClaims(source, target);
		if (claims > 0)
		{
			parts.Add("对方占有本文化领地=" + claims.ToString(CultureInfo.InvariantCulture));
		}
		return string.Join("；", parts);
	}

	private static AfVassalageType NormalizeWorldDiplomacyVassalageType(AfVassalageType type)
	{
		if (type == AfVassalageType.Military)
		{
			return AfVassalageType.Garrison;
		}
		if (type == AfVassalageType.Protectorate)
		{
			return AfVassalageType.Tributary;
		}
		return type;
	}

	private static bool TryGetWorldDiplomacyVassalage(
		Kingdom kingdom,
		out VassalageAgreement agreement,
		out Kingdom suzerain,
		out AfVassalageType type)
	{
		agreement = null;
		suzerain = null;
		type = AfVassalageType.Tributary;
		if (kingdom == null || kingdom.IsEliminated || VassalageBehavior.Instance == null)
		{
			return false;
		}
		agreement = VassalageBehavior.Instance.GetAnyVassalageAgreementForBridge(kingdom);
		if (agreement == null)
		{
			return false;
		}
		suzerain = agreement.ResolveSuzerain();
		if (suzerain == null || suzerain.IsEliminated || suzerain == kingdom)
		{
			agreement = null;
			suzerain = null;
			return false;
		}
		type = NormalizeWorldDiplomacyVassalageType(agreement.Type);
		return true;
	}

	private static bool HasIndependentWorldDiplomacyAuthority(Kingdom kingdom)
	{
		return kingdom != null
			&& !kingdom.IsEliminated
			&& (!TryGetWorldDiplomacyVassalage(kingdom, out _, out _, out AfVassalageType type)
				|| type != AfVassalageType.Vassal);
	}

	private static Kingdom ResolveWorldDiplomacyRepresentative(Kingdom kingdom)
	{
		return TryGetWorldDiplomacyVassalage(kingdom, out _, out Kingdom suzerain, out AfVassalageType type)
			&& type == AfVassalageType.Vassal
			? suzerain
			: kingdom;
	}

	private static string GetWorldDiplomacyVassalageTypeName(AfVassalageType type)
	{
		return NormalizeWorldDiplomacyVassalageType(type) switch
		{
			AfVassalageType.Tributary => "朝贡国",
			AfVassalageType.Garrison => "卫戍国",
			_ => "附庸国"
		};
	}

	private static string BuildWorldDiplomacyVassalageSnapshot()
	{
		if (VassalageBehavior.Instance == null)
		{
			return "";
		}
		List<string> relations = new List<string>();
		HashSet<string> agreementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Kingdom subject in Kingdom.All
			.Where(x => x != null && !x.IsEliminated)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase))
		{
			if (!TryGetWorldDiplomacyVassalage(subject, out VassalageAgreement agreement, out Kingdom suzerain, out AfVassalageType type)
				|| !agreementIds.Add(agreement.AgreementId ?? subject.StringId))
			{
				continue;
			}
			string authority = type switch
			{
				AfVassalageType.Tributary => "保留自身外交与军事自主，向宗主纳贡换取庇护",
				AfVassalageType.Garrison => "接受宗主军事号令，但仍可按条约表达本国利益",
				_ => "外交与军事由宗主控制，不得作为独立外交回合发言者"
			};
			relations.Add("- " + subject.StringId + "=" + KingdomName(subject)
				+ "是" + suzerain.StringId + "=" + KingdomName(suzerain) + "的"
				+ GetWorldDiplomacyVassalageTypeName(type) + "；" + authority + "。");
		}
		if (relations.Count == 0)
		{
			return "";
		}
		return "【当前宗主—臣属关系硬事实】\n"
			+ string.Join("\n", relations)
			+ "\n臣属国在涉及宗主国时必须承认现存宗主关系并保持臣属礼制上的恭敬；这不等于每篇公文都要谄媚或放弃条约仍保留的利益表达。";
	}

	private List<string> BuildPotentialDiplomaticActionIntents(Kingdom first, Kingdom second)
	{
		List<string> actions = new List<string>();
		if (first == null || second == null || first == second) return actions;
		bool atWar = FactionManager.IsAtWarAgainstFaction(first, second);
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		bool allied = alliance != null && alliance.IsAllyWithKingdom(first, second);
		bool trading = trade != null && BannerlordApiCompat.HasTradeAgreement(trade, first, second);
		if (atWar)
		{
			actions.Add("propose_peace");
			return actions;
		}
		WorldDiplomacyThreat incoming = FindOpenDiplomaticThreat(second.StringId, first.StringId);
		if (incoming != null && string.Equals(incoming.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)) actions.Add("comply_ultimatum");
		WorldDiplomacyThreat outbound = FindOpenDiplomaticThreatIssuedBy(first.StringId);
		bool canIssueWarThreat = CanIssueWarThreat(first, second, out _);
		if (canIssueWarThreat && outbound == null)
		{
			actions.Add("warning");
			actions.Add("ultimatum");
		}
		else if (canIssueWarThreat && string.Equals(outbound?.TargetKingdomId, second.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(outbound.Stage, "warning", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(outbound.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
		{
			actions.Add("ultimatum");
		}
		bool enforcingRejectedUltimatum = IsEnforcingRejectedUltimatum(first, second);
		bool canDeclareWar = CanDeclareWar(first, second, out _, enforcingRejectedUltimatum);
		if (canDeclareWar) actions.Add("declare_war");
		if (alliance != null)
		{
			if (allied) actions.Add("break_alliance");
			else if (!IsTradeAllianceProposalCoolingDown(first, second, "propose_alliance")) actions.Add("propose_alliance");
		}
		if (trade != null)
		{
			if (trading) actions.Add("cancel_trade");
			else if (!IsTradeAllianceProposalCoolingDown(first, second, "propose_trade")) actions.Add("propose_trade");
		}
		return actions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static void AppendOpenOfferResponseIntents(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target,
		List<string> actions)
	{
		if (round == null || author == null || target == null || actions == null) return;
		IEnumerable<string> proposalIntents = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
			.Select(x => NormalizeIntent(x.Intent))
			.Where(IsProposalIntent)
			.Distinct(StringComparer.OrdinalIgnoreCase);
		foreach (string proposalIntent in proposalIntents)
		{
			if (!TryResolveUniqueOpenProposalForRound(round, author, target, proposalIntent, out _)) continue;
			string acceptIntent = ProposalIntentToResponseIntent(proposalIntent, accepted: true);
			string rejectIntent = ProposalIntentToResponseIntent(proposalIntent, accepted: false);
			if (!string.IsNullOrWhiteSpace(acceptIntent)) actions.Add(acceptIntent);
			if (!string.IsNullOrWhiteSpace(rejectIntent)) actions.Add(rejectIntent);
		}
	}

	private List<string> BuildLegalDiplomaticActionIntents(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target)
	{
		List<string> actions = BuildPotentialDiplomaticActionIntents(author, target);
		if (round != null && author != null && target != null
			&& TryResolveUniqueOpenProposalForRound(round, author, target, "propose_peace", out _))
		{
			actions.Clear();
			actions.Add("accept_peace");
			actions.Add("reject_peace");
			return actions;
		}
		if (IsImmediateWarResponsePeaceSuppressed(
			round,
			round?.ResultSettlementCurrentSlotId,
			author,
			target))
		{
			actions.RemoveAll(x => string.Equals(
				NormalizeIntent(x),
				"propose_peace",
				StringComparison.OrdinalIgnoreCase));
		}
		if (round?.ResultSettlementPending == true && author != null && target != null)
		{
			WorldDiplomacyResultSettlementSlot currentSlot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
			bool hasAnswerableOfferForTarget = currentSlot != null
				&& (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
					&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.ProposerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase));
			if (hasAnswerableOfferForTarget)
			{
				actions.Clear();
				AppendOpenOfferResponseIntents(round, author, target, actions);
				return actions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			}
		}
		if (round != null && author != null && target != null)
		{
			HashSet<string> ownOpenProposalIntents = new HashSet<string>((round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
				.Where(x => x != null
					&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.ProposerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase))
				.Select(x => NormalizeIntent(x.Intent)), StringComparer.OrdinalIgnoreCase);
			actions.RemoveAll(ownOpenProposalIntents.Contains);
		}
		AppendOpenOfferResponseIntents(round, author, target, actions);
		return actions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static bool IsExclusivePeaceOfferResponseSet(IEnumerable<string> intents)
	{
		if (intents == null) return false;
		bool hasIntent = false;
		foreach (string intent in intents)
		{
			hasIntent = true;
			string normalized = NormalizeIntent(intent);
			if (!string.Equals(normalized, "accept_peace", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(normalized, "reject_peace", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return hasIntent;
	}

	private static WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(
		WorldDiplomacyRound round,
		Kingdom author,
		string resultSettlementSlotId,
		bool isExternalResponseOnly,
		string sourceDocumentId,
		bool requireAnyOpenPeaceOffer = false)
	{
		if (round == null || author == null) return null;
		IEnumerable<WorldDiplomacyRoundOffer> openPeaceOffers = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase));
		if (isExternalResponseOnly && !string.IsNullOrWhiteSpace(sourceDocumentId))
		{
			WorldDiplomacyRoundOffer exactSource = openPeaceOffers.FirstOrDefault(x => string.Equals(
				x.SourceDocumentId,
				sourceDocumentId,
				StringComparison.OrdinalIgnoreCase));
			return exactSource;
		}
		if (round.ResultSettlementPending == true
			&& !string.IsNullOrWhiteSpace(resultSettlementSlotId)
			&& string.Equals(resultSettlementSlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase))
		{
			WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, resultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
					&& SettlementSlotHasKind(x, "offer_response"));
			if (slot != null)
			{
				HashSet<string> sourceIds = new HashSet<string>(
					slot.SourceDocumentIds ?? new List<string>(),
					StringComparer.OrdinalIgnoreCase);
				WorldDiplomacyRoundOffer slotOffer = openPeaceOffers
					.Where(x => sourceIds.Contains(x.SourceDocumentId ?? ""))
					.OrderBy(x => x.CreatedDay)
					.ThenBy(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.SourceActionId, StringComparer.OrdinalIgnoreCase)
					.FirstOrDefault();
				if (slotOffer != null) return slotOffer;
			}
		}
		if (!requireAnyOpenPeaceOffer) return null;
		return openPeaceOffers
			.OrderBy(x => x.CreatedDay)
			.ThenBy(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.SourceActionId, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault();
	}

	private static bool IsRequiredPeaceOfferResponse(
		string targetKingdomId,
		string intent,
		string respondingToOfferDocumentId,
		string respondingToOfferActionId,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		string normalized = NormalizeIntent(intent);
		return (string.Equals(normalized, "accept_peace", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(normalized, "reject_peace", StringComparison.OrdinalIgnoreCase))
			&& string.Equals(targetKingdomId, requiredOffer.ProposerKingdomId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(respondingToOfferDocumentId, requiredOffer.SourceDocumentId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(respondingToOfferActionId ?? "", requiredOffer.SourceActionId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static bool DocumentContainsRequiredPeaceOfferResponse(
		WorldDiplomacyDocument document,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		if (document?.Actions?.Count > 0)
		{
			return document.Actions.Any(x => x != null && IsRequiredPeaceOfferResponse(
				x.TargetKingdomId,
				x.Intent,
				x.RespondingToOfferDocumentId,
				x.RespondingToOfferActionId,
				requiredOffer));
		}
		return document != null && IsRequiredPeaceOfferResponse(
			document.TargetKingdomId,
			document.Intent,
			document.RespondingToOfferDocumentId,
			document.RespondingToOfferActionId,
			requiredOffer);
	}

	private static bool GeneratedActionsContainRequiredPeaceOfferResponse(
		JArray actions,
		WorldDiplomacyRoundOffer requiredOffer)
	{
		if (requiredOffer == null) return true;
		return actions?.OfType<JObject>().Any(x => IsRequiredPeaceOfferResponse(
			ReadString(x, "target_kingdom_id", "target"),
			ReadString(x, "intent", "author_intent.intent"),
			ReadString(x, "responding_to_offer_document_id"),
			ReadString(x, "responding_to_offer_action_id"),
			requiredOffer)) == true;
	}

	private bool GeneratedActionsHaveUnsafeMultiplePeaceAcceptances(JArray actions)
	{
		List<JObject> acceptances = actions?.OfType<JObject>()
			.Where(x => string.Equals(
				NormalizeIntent(ReadString(x, "intent", "author_intent.intent")),
				"accept_peace",
				StringComparison.OrdinalIgnoreCase))
			.ToList() ?? new List<JObject>();
		if (acceptances.Count <= 1) return false;
		foreach (JObject acceptance in acceptances)
		{
			WorldDiplomacyDocument source = ResolveDocument(ReadString(acceptance, "responding_to_offer_document_id"));
			WorldDiplomacyPeaceTerms terms = ResolveOfferedPeaceTerms(
				source,
				ReadString(acceptance, "responding_to_offer_action_id"));
			if (source == null || PeaceTermsContainCession(terms)) return true;
		}
		return false;
	}

	private bool DocumentHasUnsafeMultiplePeaceAcceptances(WorldDiplomacyDocument document)
	{
		List<WorldDiplomacyDocumentAction> acceptances = document?.Actions?
			.Where(x => x != null && string.Equals(
				NormalizeIntent(x.Intent),
				"accept_peace",
				StringComparison.OrdinalIgnoreCase))
			.ToList() ?? new List<WorldDiplomacyDocumentAction>();
		if (acceptances.Count <= 1) return false;
		foreach (WorldDiplomacyDocumentAction acceptance in acceptances)
		{
			WorldDiplomacyDocument source = ResolveDocument(acceptance.RespondingToOfferDocumentId);
			WorldDiplomacyPeaceTerms terms = ResolveOfferedPeaceTerms(source, acceptance.RespondingToOfferActionId);
			if (source == null || PeaceTermsContainCession(terms)) return true;
		}
		return false;
	}

	private static bool PeaceTermsContainCession(WorldDiplomacyPeaceTerms terms)
	{
		return terms != null && (!string.IsNullOrWhiteSpace(terms.CessionFromKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionSettlementId));
	}

	private bool HasCessionBoundMultiplePeaceAcceptanceOptions(
		WorldDiplomacyRound round,
		Kingdom author,
		IReadOnlyDictionary<string, List<string>> legalActionsByTarget)
	{
		if (round == null || author == null || legalActionsByTarget == null) return false;
		HashSet<string> acceptingTargets = new HashSet<string>(legalActionsByTarget
			.Where(x => x.Value?.Contains("accept_peace", StringComparer.OrdinalIgnoreCase) == true)
			.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
		if (acceptingTargets.Count <= 1) return false;
		Dictionary<string, WorldDiplomacyDocument> documentsById = BuildDocumentIndex(_storage.Documents);
		foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
		{
			if (offer == null
				|| !string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				|| !acceptingTargets.Contains(offer.ProposerKingdomId ?? "")
				|| !string.Equals(NormalizeIntent(offer.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
				|| !documentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source)) continue;
			if (PeaceTermsContainCession(ResolveOfferedPeaceTerms(source, offer.SourceActionId))) return true;
		}
		return false;
	}

	private List<string> BuildLegalDiplomaticDeclarationIntents(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target,
		bool isRelayTurn,
		string resultSettlementSlotId = null,
		bool isExternalResponseOnly = false,
		WorldDiplomacyDocument responseSource = null)
	{
		List<string> intents = BuildLegalDiplomaticActionIntents(round, author, target);
		bool mustAnswerPeaceOffer = IsExclusivePeaceOfferResponseSet(intents);
		if (!mustAnswerPeaceOffer && IsNonRootAiRelayNoActionAllowed(
			round,
			resultSettlementSlotId,
			author,
			target,
			isRelayTurn,
			isExternalResponseOnly,
			responseSource))
		{
			intents.Add("statement");
		}
		return intents.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private List<Kingdom> GetActionableDiplomaticTargets(Kingdom author, WorldDiplomacyRound round = null)
	{
		if (author == null) return new List<Kingdom>();
		return Kingdom.All
			.Where(x => x != null && x != author && !x.IsEliminated && HasIndependentWorldDiplomacyAuthority(x))
			.Where(x => BuildLegalDiplomaticActionIntents(round, author, x).Count > 0)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<Kingdom> GetRoundPlanActionableParticipants(Kingdom author, WorldDiplomacyRound round)
	{
		if (author == null) return new List<Kingdom>();
		return Kingdom.All
			.Where(x => x != null && x != author && !x.IsEliminated && HasIndependentWorldDiplomacyAuthority(x))
			.Where(x => BuildLegalDiplomaticActionIntents(round, author, x).Count > 0
				|| BuildLegalDiplomaticActionIntents(round, x, author).Any(intent =>
					string.Equals(intent, "comply_ultimatum", StringComparison.OrdinalIgnoreCase)
					|| !string.IsNullOrWhiteSpace(ResponseIntentToProposalIntent(intent))))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static string DescribePotentialDiplomaticActions(IEnumerable<string> intents)
	{
		List<string> labels = (intents ?? Enumerable.Empty<string>())
			.Select(NormalizeIntent)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();
		return labels.Count == 0 ? "技术状态下没有可发布的实际外交动作" : string.Join("、", labels);
	}

	private string BuildCurrentLegalDiplomaticOptions(
		WorldDiplomacyRound round,
		Kingdom author,
		IEnumerable<string> targetKingdomIds = null,
		bool isRelayTurn = false,
		string resultSettlementSlotId = null,
		bool isExternalResponseOnly = false,
		WorldDiplomacyDocument responseSource = null)
	{
		if (author == null) return "当前可选动作：无。";
		List<string> lines = new List<string>();
		foreach (string id in (targetKingdomIds ?? round?.RelayRouteKingdomIds ?? new List<string>())
			.Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			Kingdom target = ResolveKingdom(id);
			if (target == null) continue;
			List<string> actions = BuildLegalDiplomaticDeclarationIntents(
				round,
				author,
				target,
				isRelayTurn,
				resultSettlementSlotId,
				isExternalResponseOnly,
				responseSource);
			List<string> normalizedActions = actions
				.Select(NormalizeIntent)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (normalizedActions.Count == 0) continue;
			lines.Add(id + "=" + string.Join("/", normalizedActions));
		}
		return lines.Count == 0
			? "当前可选动作：无；不得生成填充宣言。"
			: "当前可选动作：" + string.Join("；", lines) + "。";
	}

	private Dictionary<string, List<string>> BuildLegalDiplomaticDeclarationIntentMap(
		WorldDiplomacyRound round,
		Kingdom author,
		IEnumerable<string> targetKingdomIds,
		bool isRelayTurn,
		string resultSettlementSlotId,
		bool isExternalResponseOnly,
		WorldDiplomacyDocument responseSource)
	{
		Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		if (author == null) return result;
		HashSet<string> requestedIds = new HashSet<string>((targetKingdomIds ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x)
				&& !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
		if (requestedIds.Count == 0) return result;
		Dictionary<string, Kingdom> kingdomsById = Kingdom.All
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.StringId) && requestedIds.Contains(x.StringId))
			.GroupBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		foreach (string id in requestedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			if (!kingdomsById.TryGetValue(id, out Kingdom target)) continue;
			List<string> actions = BuildLegalDiplomaticDeclarationIntents(
				round,
				author,
				target,
				isRelayTurn,
				resultSettlementSlotId,
				isExternalResponseOnly,
				responseSource)
				.Select(NormalizeIntent)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (actions.Count > 0) result[id] = actions;
		}
		return result;
	}

	private static string BuildCurrentLegalDiplomaticOptions(
		IReadOnlyDictionary<string, List<string>> actionsByTarget)
	{
		List<string> lines = (actionsByTarget ?? new Dictionary<string, List<string>>())
			.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value?.Count > 0)
			.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.Key + "=" + string.Join("/", x.Value))
			.ToList();
		return lines.Count == 0
			? "当前可选动作：无；不得生成填充宣言。"
			: "当前可选动作：" + string.Join("；", lines) + "。";
	}

	private List<Kingdom> GetEligibleAiKingdoms()
	{
		return Kingdom.All
			.Where(x => x != null
				&& !x.IsEliminated
				&& HasIndependentWorldDiplomacyAuthority(x)
				&& CanAiAuthorDiplomaticDocument(x, out _)
				&& x.RulingClan?.Leader != null
				&& x.RulingClan.Leader.IsAlive)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private WorldDiplomacyDocument CreateDocument(
		Kingdom author,
		Kingdom target,
		string title,
		string body,
		string origin,
		bool isPlayerAuthored,
		bool isResponse,
		string exchangeId)
	{
		return new WorldDiplomacyDocument
		{
			DocumentId = NewId("diplomacy_document"),
			ExchangeId = exchangeId ?? "",
			RoundId = exchangeId ?? "",
			AuthorKingdomId = author?.StringId ?? "",
			AuthorKingdomName = KingdomName(author),
			AuthorRulerId = author?.RulingClan?.Leader?.StringId ?? "",
			AuthorRulerName = RulerName(author),
			TargetKingdomId = target?.StringId ?? "",
			TargetKingdomName = target == null ? "" : KingdomName(target),
			Title = Limit(FirstNonEmpty(title, "外交宣言"), 100),
			Body = NormalizeBody(body),
			Origin = origin ?? "",
			Day = CurrentDay(),
			GameDate = FormatCampaignDate(CurrentDay()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			IsPlayerAuthored = isPlayerAuthored,
			IsResponse = isResponse,
			IsRead = isPlayerAuthored,
			AddressedKingdomIds = target == null ? new List<string>() : new List<string> { target.StringId }
		};
	}

	private void AddDocument(WorldDiplomacyDocument document)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
		{
			return;
		}
		_storage.Documents.RemoveAll(x => x != null && string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
		_storage.Documents.Add(document);
		_storage.Documents = _storage.Documents
			.Where(x => x != null)
			// Keep any publishable artifact whose canonical append is still pending
			// ahead of ordinary archive eviction.
			.OrderByDescending(NeedsCanonicalHistoryRetry)
			.ThenByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(MaxStoredDocuments)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ToList();
	}

	private void EnsureCanonicalHistoryInitialized()
	{
		if (_canonicalHistoryInitializedThisSession && _storage?.CanonicalHistory?.Snapshot != null && _storage.CanonicalHistory.DeltaEntries != null) return;
		_storage.CanonicalHistory ??= new WorldDiplomacyCanonicalHistoryState();
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		history.Snapshot ??= new WorldDiplomacyCanonicalHistorySnapshot();
		history.Snapshot.PreservedResultSourceIds ??= new List<string>();
		history.Snapshot.ProtectedFacts ??= new List<WorldDiplomacyCanonicalProtectedFact>();
		history.Snapshot.ProtectedFacts = history.Snapshot.ProtectedFacts
			.Select(CloneProtectedFact)
			.Where(x => x != null
				&& (x.Kind == "diplomatic_result" || x.Kind == "response_link")
				&& !string.IsNullOrWhiteSpace(x.SourceId)
				&& (x.Kind != "diplomatic_result" || !string.IsNullOrWhiteSpace(x.Text))
				&& (x.Kind != "response_link" || !string.IsNullOrWhiteSpace(x.RelatedSourceId)))
			.GroupBy(ProtectedFactStableKey, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.OrderBy(y => y.Sequence).First())
			.OrderBy(x => x.Sequence).ThenBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase)
			.ToList();
		history.Snapshot.PreservedResultSourceIds = history.Snapshot.PreservedResultSourceIds
			.Concat(history.Snapshot.ProtectedFacts.Where(x => x.Kind == "diplomatic_result").Select(x => x.SourceId))
			.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		history.DeltaEntries ??= new List<WorldDiplomacyCanonicalHistoryEntry>();
		history.WorldWeeklySourceHashes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		history.WorldWeeklySourceRevisions ??= new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
		history.PolicyRevisionSignatures ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		history.LastPolicyArtifactSequence = Math.Max(0L, history.LastPolicyArtifactSequence);
		history.LastPolicyArtifactLedgerId = (history.LastPolicyArtifactLedgerId ?? "").Trim();
		history.WorldWeeklySourceHashes = history.WorldWeeklySourceHashes.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Last().Value ?? "", StringComparer.OrdinalIgnoreCase);
		history.WorldWeeklySourceRevisions = history.WorldWeeklySourceRevisions.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => Math.Max(0L, x.Last().Value), StringComparer.OrdinalIgnoreCase);
		history.PolicyRevisionSignatures = history.PolicyRevisionSignatures.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Last().Value ?? "", StringComparer.OrdinalIgnoreCase);
		history.DeltaEntries.RemoveAll(x => x == null || x.Sequence <= history.Snapshot.CoveredThroughSequence || string.IsNullOrWhiteSpace(x.Kind));
		history.DeltaEntries = history.DeltaEntries
			.OrderBy(x => x.Sequence)
			.GroupBy(x => x.Sequence)
			.Select(x => x.First())
			.ToList();
		_canonicalHistorySourceKeys.Clear();
		foreach (string sourceKey in history.DeltaEntries.Select(x => x?.SourceKey).Where(x => !string.IsNullOrWhiteSpace(x))) _canonicalHistorySourceKeys.Add(sourceKey);
		long lastSequence = history.DeltaEntries.Count == 0
			? history.Snapshot.CoveredThroughSequence
			: Math.Max(history.Snapshot.CoveredThroughSequence, history.DeltaEntries[history.DeltaEntries.Count - 1].Sequence);
		history.NextSequence = Math.Max(Math.Max(1L, history.NextSequence), lastSequence + 1L);
		foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries)
		{
			entry.TargetKingdomIds ??= new List<string>();
			entry.TargetKingdomIds = entry.TargetKingdomIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			entry.RespondingToOfferDocumentId = (entry.RespondingToOfferDocumentId ?? "").Trim();
			entry.RespondingToThreatDocumentId = (entry.RespondingToThreatDocumentId ?? "").Trim();
			if (entry.ActionFacts != null)
			{
				entry.ActionFacts = entry.ActionFacts.Where(x => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			}
			if (entry.EstimatedTokens <= 0) entry.EstimatedTokens = EstimateHistoryTokens(RenderCanonicalHistoryEntry(entry));
		}
		string snapshotPayload = RenderCanonicalSnapshotPayload(history.Snapshot);
		history.Snapshot.EstimatedTokens = EstimateHistoryTokens(snapshotPayload);
		history.Snapshot.ContentHash = StablePromptHash(snapshotPayload);
		RecalculateCanonicalHistoryTokens();
		_canonicalHistoryInitializedThisSession = true;
	}

	private static long EstimateHistoryTokens(string text)
	{
		return Math.Max(0, Logger.EstimateTokens(text ?? ""));
	}

	private void RecalculateCanonicalHistoryTokens()
	{
		WorldDiplomacyCanonicalHistoryState history = _storage?.CanonicalHistory;
		if (history == null) return;
		long total = Math.Max(0L, history.Snapshot?.EstimatedTokens ?? 0L);
		foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
		{
			total += Math.Max(0L, entry?.EstimatedTokens ?? 0L);
		}
		history.EstimatedTokens = Math.Max(0L, total);
		_storage.DiplomacyTokensSinceCompression = history.EstimatedTokens;
		_storage.DiplomacyCompressionPending = history.EstimatedTokens >= GetHistoryCompressionTriggerTokens();
	}

	private bool AppendCanonicalHistoryEntry(
		string kind,
		string sourceKey,
		string sourceId,
		int day,
		string gameDate,
		string authorKingdomId,
		IEnumerable<string> targetKingdomIds,
		string intent,
		string commitment,
		string content,
		bool verified,
		string respondingToOfferDocumentId = null,
		string respondingToThreatDocumentId = null,
		IEnumerable<string> actionFacts = null)
	{
		EnsureCanonicalHistoryInitialized();
		string normalizedKind = (kind ?? "").Trim().ToLowerInvariant();
		string normalizedSourceKey = (sourceKey ?? "").Trim();
		string normalizedContent = NormalizeCanonicalHistoryText(content);
		if (string.IsNullOrWhiteSpace(normalizedKind) || string.IsNullOrWhiteSpace(normalizedSourceKey) || string.IsNullOrWhiteSpace(normalizedContent)) return false;
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		if (_canonicalHistorySourceKeys.Contains(normalizedSourceKey)) return false;
		WorldDiplomacyCanonicalHistoryEntry entry = new WorldDiplomacyCanonicalHistoryEntry
		{
			EntryId = NewId("diplomacy_history"),
			SourceKey = normalizedSourceKey,
			Sequence = history.NextSequence++,
			Day = Math.Max(0, day),
			GameDate = FirstNonEmpty(gameDate, FormatCampaignDate(Math.Max(0, day))),
			Kind = normalizedKind,
			SourceId = (sourceId ?? "").Trim(),
			RespondingToOfferDocumentId = (respondingToOfferDocumentId ?? "").Trim(),
			RespondingToThreatDocumentId = (respondingToThreatDocumentId ?? "").Trim(),
			AuthorKingdomId = (authorKingdomId ?? "").Trim(),
			TargetKingdomIds = (targetKingdomIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Intent = NormalizeIntent(intent),
			Commitment = NormalizeCommitment(commitment),
			ActionFacts = actionFacts?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Text = normalizedContent,
			Verified = verified
		};
		entry.EstimatedTokens = EstimateHistoryTokens(RenderCanonicalHistoryEntry(entry));
		history.DeltaEntries.Add(entry);
		_canonicalHistorySourceKeys.Add(normalizedSourceKey);
		history.Revision++;
		history.EstimatedTokens += entry.EstimatedTokens;
		_storage.DiplomacyTokensSinceCompression = history.EstimatedTokens;
		_storage.DiplomacyCompressionPending = history.EstimatedTokens >= GetHistoryCompressionTriggerTokens();
		InvalidateCanonicalHistoryRenderCache();
		return true;
	}

	private void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document)
	{
		if (document == null || !document.IsReadyForPublication || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		bool externalResolvedFact = string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase);
		if (externalResolvedFact) document.HistoryDeclarationRecorded = true;
		List<string> targets = (document.AddressedKingdomIds ?? new List<string>())
			.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })
			.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> actionFacts = document.Actions?.Where(x => x != null)
			.Select(x => (x.ActionId ?? "") + "@" + (x.TargetKingdomId ?? "") + "=" + NormalizeIntent(x.Intent))
			.ToList();
		string declarationIntent = document.Actions?.Count > 1 ? "multi_action" : document.Intent;
		string declarationCommitment = document.Actions?.Count > 1 ? "mixed" : document.Commitment;
		if (!document.HistoryDeclarationRecorded && !string.IsNullOrWhiteSpace(document.Body))
		{
			string declarationSourceKey = "document:" + document.DocumentId + ":declaration";
			bool appended = AppendCanonicalHistoryEntry("declaration", declarationSourceKey, document.DocumentId,
				document.Day, document.GameDate, document.AuthorKingdomId, targets, declarationIntent, declarationCommitment, document.Body,
				verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId,
				respondingToThreatDocumentId: document.RespondingToThreatDocumentId,
				actionFacts: actionFacts);
			if (appended || CanonicalDeltaContainsSourceKey(declarationSourceKey))
			{
				document.HistoryDeclarationRecorded = true;
			}
		}
		if (document.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null
				&& x.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(x.MechanicalResult)))
			{
				string resultSourceKey = "document:" + document.DocumentId + ":action:" + action.ActionId + ":result";
				bool appended = AppendCanonicalHistoryEntry("diplomatic_result", resultSourceKey, document.DocumentId,
					document.Day, document.GameDate, document.AuthorKingdomId, new[] { action.TargetKingdomId },
					action.Intent, action.Commitment, "经游戏机制确认：" + action.MechanicalResult,
					verified: true, respondingToOfferDocumentId: action.RespondingToOfferDocumentId,
					respondingToThreatDocumentId: action.RespondingToThreatDocumentId,
					actionFacts: new[] { (action.ActionId ?? "") + "@" + (action.TargetKingdomId ?? "") + "=" + NormalizeIntent(action.Intent) });
				if (appended || CanonicalDeltaContainsSourceKey(resultSourceKey)) action.HistoryResultRecorded = true;
			}
			document.HistoryResultRecorded = document.Actions.Where(x => x != null && x.ChangedDiplomaticState
				&& !string.IsNullOrWhiteSpace(x.MechanicalResult)).All(x => x.HistoryResultRecorded);
			return;
		}
		if (!document.HistoryResultRecorded && (document.ChangedDiplomaticState || externalResolvedFact) && !string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			string resultText = "经游戏机制确认：" + document.MechanicalResult;
			string resultSourceKey = "document:" + document.DocumentId + ":result";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", resultSourceKey, document.DocumentId,
				document.Day, document.GameDate, document.AuthorKingdomId, targets, document.Intent, document.Commitment, resultText,
				verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId,
				respondingToThreatDocumentId: document.RespondingToThreatDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(resultSourceKey)
				|| (_storage.CanonicalHistory.Snapshot.PreservedResultSourceIds ?? new List<string>()).Contains(document.DocumentId, StringComparer.OrdinalIgnoreCase))
			{
				document.HistoryResultRecorded = true;
			}
		}
	}

	private bool CanonicalDeltaContainsSourceKey(string sourceKey)
	{
		return !string.IsNullOrWhiteSpace(sourceKey) && _canonicalHistorySourceKeys.Contains(sourceKey);
	}

	private void SyncCanonicalHistorySources(bool force = false)
	{
		EnsureCanonicalHistoryInitialized();
		int currentHour = CurrentHour();
		if (!force && _lastCanonicalSourceSyncHour == currentHour) return;
		long weeklyRevision = MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
		if (_lastObservedWorldWeeklyHistoryRevision != weeklyRevision)
		{
			foreach (MyBehavior.WorldWeeklyReportHistoryEntry report in MyBehavior.GetPublishedWorldWeeklyReportHistoryForExternal())
			{
				AppendPublishedWorldWeeklyReportArtifact(report);
			}
			_lastObservedWorldWeeklyHistoryRevision = weeklyRevision;
		}
		SyncPublishedPolicyArtifacts(force ? PolicyHistoryForceSyncMaxBatches : 1);
		_lastCanonicalSourceSyncHour = currentHour;
	}

	private void AppendPublishedWorldWeeklyReportArtifact(MyBehavior.WorldWeeklyReportHistoryEntry report)
	{
		if (report == null || string.IsNullOrWhiteSpace(report.SourceId) || string.IsNullOrWhiteSpace(report.PublishedReportText)) return;
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		string publishedTitle = (report.PublishedTitle ?? "").Trim();
		string publishedBody = report.PublishedReportText.Trim();
		string hash = StablePromptHash(publishedTitle + "\n" + publishedBody);
		history.WorldWeeklySourceHashes.TryGetValue(report.SourceId, out string previousHash);
		if (string.Equals(previousHash, hash, StringComparison.Ordinal)) return;
		history.WorldWeeklySourceRevisions.TryGetValue(report.SourceId, out long previousRevision);
		long revision = Math.Max(0L, previousRevision) + 1L;
		string sourceKey = "weekly:" + report.SourceId + ":r" + revision.ToString(CultureInfo.InvariantCulture);
		while (CanonicalDeltaContainsSourceKey(sourceKey))
		{
			revision++;
			sourceKey = "weekly:" + report.SourceId + ":r" + revision.ToString(CultureInfo.InvariantCulture);
		}
		bool correction = !string.IsNullOrWhiteSpace(previousHash);
		string heading = correction ? "世界周报成品更正版" : "世界周报成品";
		string text = heading + (string.IsNullOrWhiteSpace(publishedTitle) ? "：\n" : "《" + publishedTitle + "》：\n") + publishedBody;
		if (AppendCanonicalHistoryEntry("world_weekly", sourceKey, report.SourceId,
			report.CreatedDay, report.CreatedDate, "", Enumerable.Empty<string>(), "", "", text, verified: true))
		{
			history.WorldWeeklySourceHashes[report.SourceId] = hash;
			history.WorldWeeklySourceRevisions[report.SourceId] = revision;
		}
	}

	private void SyncPublishedPolicyArtifacts(int maxBatches)
	{
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		string ledgerId = (WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryLedgerId() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(ledgerId)) return;
		if (string.IsNullOrWhiteSpace(history.LastPolicyArtifactLedgerId))
		{
			history.LastPolicyArtifactLedgerId = ledgerId;
			if (history.LastPolicyArtifactSequence > 0L)
			{
				RebuildPublishedPolicySignaturesThrough(history.LastPolicyArtifactSequence);
			}
		}
		else if (!string.Equals(history.LastPolicyArtifactLedgerId, ledgerId, StringComparison.Ordinal))
		{
			Log("published policy ledger epoch changed old=" + history.LastPolicyArtifactLedgerId
				+ " new=" + ledgerId + "; resynchronizing immutable artifacts");
			history.LastPolicyArtifactLedgerId = ledgerId;
			history.LastPolicyArtifactSequence = 0L;
		}
		long availableSequence = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryCurrentSequence();
		long cursor = Math.Max(0L, history.LastPolicyArtifactSequence);
		if (cursor > availableSequence)
		{
			Log("published policy cursor exceeds current ledger sequence cursor="
				+ cursor.ToString(CultureInfo.InvariantCulture)
				+ " available=" + availableSequence.ToString(CultureInfo.InvariantCulture)
				+ "; resynchronizing immutable artifacts");
			cursor = 0L;
			history.LastPolicyArtifactSequence = 0L;
		}
		int batchLimit = Math.Max(1, maxBatches);
		for (int batch = 0; batch < batchLimit && cursor < availableSequence; batch++)
		{
			IReadOnlyList<PublishedPolicyArtifactLedgerEntry> entries = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryArtifacts(cursor, PolicyHistorySyncBatchSize);
			if (entries == null || entries.Count == 0) break;
			long previousCursor = cursor;
			foreach (PublishedPolicyArtifactLedgerEntry policy in entries.OrderBy(x => x?.Sequence ?? long.MaxValue))
			{
				if (policy == null || policy.Sequence <= cursor) continue;
				if (!AppendPublishedPolicyArtifact(policy)) break;
				cursor = policy.Sequence;
				history.LastPolicyArtifactSequence = cursor;
			}
			if (cursor <= previousCursor) break;
			WorldDiplomacyPolicyContext.TryAcknowledgePublishedPolicyHistoryThrough(cursor);
		}
	}

	private void RebuildPublishedPolicySignaturesThrough(long throughSequence)
	{
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		long cutoff = Math.Max(0L, throughSequence);
		long cursor = 0L;
		while (cursor < cutoff)
		{
			IReadOnlyList<PublishedPolicyArtifactLedgerEntry> entries =
				WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryArtifacts(cursor, 1024);
			if (entries == null || entries.Count == 0) break;
			long previousCursor = cursor;
			foreach (PublishedPolicyArtifactLedgerEntry policy in entries.OrderBy(x => x?.Sequence ?? long.MaxValue))
			{
				if (policy == null || policy.Sequence <= cursor) continue;
				if (policy.Sequence > cutoff) return;
				if (TryBuildPublishedPolicySignature(policy, out string signatureKey, out string fingerprint))
				{
					history.PolicyRevisionSignatures[signatureKey] = fingerprint;
				}
				cursor = policy.Sequence;
			}
			if (cursor <= previousCursor) break;
		}
	}

	private static bool TryBuildPublishedPolicySignature(
		PublishedPolicyArtifactLedgerEntry policy,
		out string signatureKey,
		out string fingerprint)
	{
		signatureKey = "";
		fingerprint = "";
		string eventKind = (policy?.EventKind ?? "").Trim().ToLowerInvariant();
		if (policy == null || policy.Revision <= 0L || string.IsNullOrWhiteSpace(policy.PolicyId)
			|| (eventKind != "policy_published" && eventKind != "policy_snapshot")) return false;
		signatureKey = policy.PolicyId.Trim() + ":" + policy.Revision.ToString(CultureInfo.InvariantCulture) + ":" + eventKind;
		fingerprint = StablePromptHash(string.Join("\n", new[]
		{
			policy.PolicyName ?? "",
			policy.KingdomId ?? "",
			policy.KingdomName ?? "",
			policy.ScopeKind ?? "",
			policy.PublishedText ?? ""
		}));
		return true;
	}

	private bool AppendPublishedPolicyArtifact(PublishedPolicyArtifactLedgerEntry policy)
	{
		string eventKind = (policy?.EventKind ?? "").Trim().ToLowerInvariant();
		if (policy == null
			|| policy.Sequence <= 0L
			|| policy.Revision <= 0L
			|| string.IsNullOrWhiteSpace(policy.PolicyId)
			|| string.IsNullOrWhiteSpace(policy.PublishedText)
			|| (eventKind != "policy_published" && eventKind != "policy_snapshot")) return false;
		if (!TryBuildPublishedPolicySignature(policy, out string signatureKey, out string fingerprint)) return false;
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		if (history.PolicyRevisionSignatures.TryGetValue(signatureKey, out string previousFingerprint)
			&& string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal)) return true;
		string ledgerId = FirstNonEmpty(history.LastPolicyArtifactLedgerId, "legacy");
		string sourceKey = "policy:" + ledgerId + ":" + signatureKey;
		if (CanonicalDeltaContainsSourceKey(sourceKey))
		{
			history.PolicyRevisionSignatures[signatureKey] = fingerprint;
			return true;
		}
		StringBuilder text = new StringBuilder();
		text.Append("政策《").Append(FirstNonEmpty(policy.PolicyName, "未命名政策")).Append("》");
		if (!string.IsNullOrWhiteSpace(policy.KingdomName) || !string.IsNullOrWhiteSpace(policy.KingdomId))
		{
			text.Append("；发布国=").Append(FirstNonEmpty(policy.KingdomName, policy.KingdomId));
		}
		if (!string.IsNullOrWhiteSpace(policy.ScopeKind)) text.Append("；范围=").Append(policy.ScopeKind.Trim());
		text.AppendLine().Append(policy.PublishedText.Trim());
		bool appended = AppendCanonicalHistoryEntry(eventKind, sourceKey, policy.PolicyId,
			policy.OccurredDay, policy.GameDate, policy.KingdomId, Enumerable.Empty<string>(), "", "",
			text.ToString(), verified: true);
		if (appended) history.PolicyRevisionSignatures[signatureKey] = fingerprint;
		return appended;
	}

	private static string RenderCanonicalHistoryEntry(WorldDiplomacyCanonicalHistoryEntry entry)
	{
		if (entry == null) return "";
		StringBuilder sb = new StringBuilder();
		sb.Append("[seq=").Append(entry.Sequence.ToString(CultureInfo.InvariantCulture))
			.Append("|kind=").Append(entry.Kind ?? "")
			.Append("|date=").Append(entry.GameDate ?? "")
			.Append("|source=").Append(entry.SourceId ?? "");
		if (!string.IsNullOrWhiteSpace(entry.RespondingToOfferDocumentId))
		{
			sb.Append("|responding_to=").Append(entry.RespondingToOfferDocumentId);
		}
		if (!string.IsNullOrWhiteSpace(entry.RespondingToThreatDocumentId))
		{
			sb.Append("|responding_to_threat=").Append(entry.RespondingToThreatDocumentId);
		}
		if (entry.ActionFacts?.Count > 0)
		{
			sb.Append("|actions=").Append(string.Join(",", entry.ActionFacts));
		}
		sb.Append("|author=").Append(entry.AuthorKingdomId ?? "")
			.Append("|targets=").Append(string.Join(",", entry.TargetKingdomIds ?? new List<string>()))
			.Append("|intent=").Append(entry.Intent ?? "")
			.Append("|commitment=").Append(entry.Commitment ?? "")
			.Append("|verified=").Append(entry.Verified ? "true" : "false").AppendLine("]");
		sb.Append(entry.Text ?? "");
		return sb.ToString().TrimEnd();
	}

	private static string ProtectedFactStableKey(WorldDiplomacyCanonicalProtectedFact fact)
	{
		if (fact == null) return "";
		string kind = (fact.Kind ?? "").Trim().ToLowerInvariant();
		string sourceKey = (fact.SourceKey ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(sourceKey)) return kind + ":" + sourceKey;
		return kind + ":" + (fact.SourceId ?? "").Trim() + ":" + (fact.RelatedSourceId ?? "").Trim();
	}

	private static WorldDiplomacyCanonicalProtectedFact CloneProtectedFact(WorldDiplomacyCanonicalProtectedFact fact)
	{
		if (fact == null) return null;
		return new WorldDiplomacyCanonicalProtectedFact
		{
			Kind = (fact.Kind ?? "").Trim().ToLowerInvariant(),
			SourceKey = (fact.SourceKey ?? "").Trim(),
			SourceId = (fact.SourceId ?? "").Trim(),
			RelatedSourceId = (fact.RelatedSourceId ?? "").Trim(),
			Sequence = Math.Max(0L, fact.Sequence),
			Day = Math.Max(0, fact.Day),
			GameDate = (fact.GameDate ?? "").Trim(),
			AuthorKingdomId = (fact.AuthorKingdomId ?? "").Trim(),
			TargetKingdomIds = (fact.TargetKingdomIds ?? new List<string>())
				.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Intent = NormalizeIntent(fact.Intent),
			Commitment = NormalizeCommitment(fact.Commitment),
			Text = NormalizeCanonicalHistoryText(fact.Text)
		};
	}

	private List<WorldDiplomacyCanonicalProtectedFact> BuildCanonicalProtectedFactsThrough(long cutoff)
	{
		EnsureCanonicalHistoryInitialized();
		Dictionary<string, WorldDiplomacyCanonicalProtectedFact> facts = new Dictionary<string, WorldDiplomacyCanonicalProtectedFact>(StringComparer.OrdinalIgnoreCase);
		void Add(WorldDiplomacyCanonicalProtectedFact candidate)
		{
			WorldDiplomacyCanonicalProtectedFact clean = CloneProtectedFact(candidate);
			if (clean == null
				|| (clean.Kind != "diplomatic_result" && clean.Kind != "response_link")
				|| string.IsNullOrWhiteSpace(clean.SourceId)
				|| (clean.Kind == "diplomatic_result" && string.IsNullOrWhiteSpace(clean.Text))
				|| (clean.Kind == "response_link" && string.IsNullOrWhiteSpace(clean.RelatedSourceId))) return;
			string key = ProtectedFactStableKey(clean);
			if (!string.IsNullOrWhiteSpace(key) && !facts.ContainsKey(key)) facts.Add(key, clean);
		}
		foreach (WorldDiplomacyCanonicalProtectedFact fact in _storage.CanonicalHistory.Snapshot.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>()) Add(fact);
		foreach (WorldDiplomacyCanonicalHistoryEntry entry in _storage.CanonicalHistory.DeltaEntries
			.Where(x => x != null && x.Sequence <= cutoff).OrderBy(x => x.Sequence))
		{
			if (entry.Verified && string.Equals(entry.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
			{
				Add(new WorldDiplomacyCanonicalProtectedFact
				{
					Kind = "diplomatic_result",
					SourceKey = FirstNonEmpty(entry.SourceKey, "result:" + entry.SourceId),
					SourceId = entry.SourceId,
					RelatedSourceId = FirstNonEmpty(entry.RespondingToThreatDocumentId, entry.RespondingToOfferDocumentId),
					Sequence = entry.Sequence,
					Day = entry.Day,
					GameDate = entry.GameDate,
					AuthorKingdomId = entry.AuthorKingdomId,
					TargetKingdomIds = entry.TargetKingdomIds,
					Intent = entry.Intent,
					Commitment = entry.Commitment,
					Text = entry.Text
				});
			}
			if (!string.IsNullOrWhiteSpace(entry.SourceId) && !string.IsNullOrWhiteSpace(entry.RespondingToOfferDocumentId))
			{
				Add(new WorldDiplomacyCanonicalProtectedFact
				{
					Kind = "response_link",
					SourceKey = "response:" + entry.SourceId + "->" + entry.RespondingToOfferDocumentId,
					SourceId = entry.SourceId,
					RelatedSourceId = entry.RespondingToOfferDocumentId,
					Sequence = entry.Sequence,
					Day = entry.Day,
					GameDate = entry.GameDate,
					AuthorKingdomId = entry.AuthorKingdomId,
					TargetKingdomIds = entry.TargetKingdomIds,
					Intent = entry.Intent,
					Commitment = entry.Commitment
				});
			}
			if (!string.IsNullOrWhiteSpace(entry.SourceId) && !string.IsNullOrWhiteSpace(entry.RespondingToThreatDocumentId))
			{
				Add(new WorldDiplomacyCanonicalProtectedFact
				{
					Kind = "response_link",
					SourceKey = "threat-response:" + entry.SourceId + "->" + entry.RespondingToThreatDocumentId,
					SourceId = entry.SourceId,
					RelatedSourceId = entry.RespondingToThreatDocumentId,
					Sequence = entry.Sequence,
					Day = entry.Day,
					GameDate = entry.GameDate,
					AuthorKingdomId = entry.AuthorKingdomId,
					TargetKingdomIds = entry.TargetKingdomIds,
					Intent = entry.Intent,
					Commitment = entry.Commitment
				});
			}
		}
		return facts.Values.OrderBy(x => x.Sequence).ThenBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<WorldDiplomacyCanonicalProtectedFact> SelectCanonicalProtectedFactsWithinTokenBudget(
		IEnumerable<WorldDiplomacyCanonicalProtectedFact> source,
		long tokenBudget)
	{
		if (tokenBudget <= 0L) return new List<WorldDiplomacyCanonicalProtectedFact>();
		List<WorldDiplomacyCanonicalProtectedFact> selected = new List<WorldDiplomacyCanonicalProtectedFact>();
		long estimated = 0L;
		foreach (WorldDiplomacyCanonicalProtectedFact fact in (source ?? Enumerable.Empty<WorldDiplomacyCanonicalProtectedFact>())
			.Where(x => x != null)
			.OrderByDescending(x => x.Sequence)
			.ThenByDescending(x => x.Kind, StringComparer.Ordinal)
			.ThenByDescending(x => x.SourceKey, StringComparer.OrdinalIgnoreCase))
		{
			long factTokens = EstimateHistoryTokens(RenderCanonicalProtectedFacts(
				new[] { fact }, Enumerable.Empty<string>()));
			if (factTokens <= 0L || estimated + factTokens > tokenBudget) continue;
			selected.Add(fact);
			estimated += factTokens;
		}
		selected = selected.OrderBy(x => x.Sequence).ThenBy(x => x.Kind, StringComparer.Ordinal)
			.ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase).ToList();
		while (selected.Count > 0
			&& EstimateHistoryTokens(RenderCanonicalProtectedFacts(selected, Enumerable.Empty<string>())) > tokenBudget)
		{
			selected.RemoveAt(0);
		}
		return selected;
	}

	private static string RenderCanonicalProtectedFacts(
		IEnumerable<WorldDiplomacyCanonicalProtectedFact> protectedFacts,
		IEnumerable<string> preservedResultSourceIds)
	{
		List<WorldDiplomacyCanonicalProtectedFact> facts = (protectedFacts ?? Enumerable.Empty<WorldDiplomacyCanonicalProtectedFact>())
			.Where(x => x != null).OrderBy(x => x.Sequence).ThenBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase).ToList();
		HashSet<string> exactResultIds = new HashSet<string>(facts.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
			.Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		List<string> legacyResultIds = (preservedResultSourceIds ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x) && !exactResultIds.Contains(x)).Select(x => x.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		if (facts.Count == 0 && legacyResultIds.Count == 0) return "";
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【确定性保留的外交硬事实；压缩摘要不得覆盖】");
		foreach (WorldDiplomacyCanonicalProtectedFact fact in facts)
		{
			if (string.Equals(fact.Kind, "response_link", StringComparison.OrdinalIgnoreCase))
			{
				sb.Append("[kind=response_link|source=").Append(fact.SourceId)
					.Append("|responding_to=").Append(fact.RelatedSourceId)
					.Append("|date=").Append(fact.GameDate ?? "")
					.Append("|author=").Append(fact.AuthorKingdomId ?? "")
					.Append("|targets=").Append(string.Join(",", fact.TargetKingdomIds ?? new List<string>()))
					.Append("|intent=").Append(fact.Intent ?? "")
					.Append("|commitment=").Append(fact.Commitment ?? "").AppendLine("]");
				continue;
			}
			sb.Append("[kind=diplomatic_result|source=").Append(fact.SourceId)
				.Append("|date=").Append(fact.GameDate ?? "")
				.Append("|author=").Append(fact.AuthorKingdomId ?? "")
				.Append("|targets=").Append(string.Join(",", fact.TargetKingdomIds ?? new List<string>()))
				.Append("|intent=").Append(fact.Intent ?? "")
				.Append("|commitment=").Append(fact.Commitment ?? "")
				.AppendLine("|verified=true]");
			sb.AppendLine(fact.Text ?? "");
		}
		foreach (string sourceId in legacyResultIds)
		{
			sb.Append("[kind=diplomatic_result_manifest|source=").Append(sourceId)
				.AppendLine("|verified=true|detail_in_compressed_summary=true]");
		}
		return sb.ToString().TrimEnd();
	}

	private static string RenderCanonicalSnapshotPayload(WorldDiplomacyCanonicalHistorySnapshot snapshot)
	{
		if (snapshot == null) return "";
		StringBuilder sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(snapshot.Content)) sb.AppendLine(snapshot.Content.Trim());
		string protectedFacts = RenderCanonicalProtectedFacts(snapshot.ProtectedFacts, snapshot.PreservedResultSourceIds);
		if (!string.IsNullOrWhiteSpace(protectedFacts)) sb.AppendLine(protectedFacts);
		return sb.ToString().TrimEnd();
	}

	private string BuildCanonicalHistoryBlock(long throughSequence = long.MaxValue)
	{
		EnsureCanonicalHistoryInitialized();
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		long cutoff = throughSequence == long.MaxValue ? history.NextSequence - 1L : Math.Max(0L, throughSequence);
		string cacheKey = (history.Snapshot.ContentHash ?? "") + "|" + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture)
			+ "|" + cutoff.ToString(CultureInfo.InvariantCulture);
		if (string.Equals(_canonicalHistoryRenderCacheKey, cacheKey, StringComparison.Ordinal) && !string.IsNullOrEmpty(_canonicalHistoryRenderCache))
		{
			return _canonicalHistoryRenderCache;
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【全局长期外交历史】");
		sb.AppendLine("本档案对所有王国可见；动态当前状态与本档案冲突时，以动态当前状态为准。提议或宣言不等于已执行结果，只有 verified=true 的 diplomatic_result 表示游戏机制已确认改变现实状态。");
		string snapshotPayload = RenderCanonicalSnapshotPayload(history.Snapshot);
		if (!string.IsNullOrWhiteSpace(snapshotPayload) && history.Snapshot.CoveredThroughSequence <= cutoff)
		{
			sb.AppendLine("【已压缩历史；覆盖至seq=" + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture) + "】");
			sb.AppendLine(snapshotPayload);
		}
		foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries.Where(x => x != null && x.Sequence <= cutoff).OrderBy(x => x.Sequence))
		{
			sb.AppendLine(RenderCanonicalHistoryEntry(entry));
		}
		if (string.IsNullOrWhiteSpace(snapshotPayload) && !history.DeltaEntries.Any(x => x != null && x.Sequence <= cutoff)) sb.AppendLine("（暂无历史记录）");
		string rendered = sb.ToString().TrimEnd();
		_canonicalHistoryRenderCacheKey = cacheKey;
		_canonicalHistoryRenderCache = rendered;
		return rendered;
	}

	private void InvalidateCanonicalHistoryRenderCache()
	{
		_canonicalHistoryRenderCacheKey = "";
		_canonicalHistoryRenderCache = "";
	}

	private void CaptureCanonicalHistoryForJob(WorldDiplomacyJob job, bool syncSources)
	{
		if (job == null) return;
		if (syncSources)
		{
			RetryDeferredCanonicalHistoryEntries();
			SyncCanonicalHistorySources(force: true);
		}
		EnsureCanonicalHistoryInitialized();
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		job.HistoryThroughSequence = Math.Max(history.Snapshot.CoveredThroughSequence, history.NextSequence - 1L);
		job.HistoryRevision = history.Revision;
		job.HistoryEstimatedTokens = history.EstimatedTokens;
		job.HistorySnapshotThroughSequence = history.Snapshot.CoveredThroughSequence;
		job.HistorySnapshotHash = history.Snapshot.ContentHash ?? "";
		string historyBlock = BuildCanonicalHistoryBlock(job.HistoryThroughSequence);
		job.HistoryPrefixHash = StablePromptHashPair(job.SystemPrompt, historyBlock);
	}

	private static List<PublishedPolicyArtifactLedgerEntry> ReadAllPublishedPolicyArtifactsForMigration()
	{
		List<PublishedPolicyArtifactLedgerEntry> result = new List<PublishedPolicyArtifactLedgerEntry>();
		long cursor = 0L;
		long available = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryCurrentSequence();
		while (cursor < available)
		{
			IReadOnlyList<PublishedPolicyArtifactLedgerEntry> batch = WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryArtifacts(cursor, 1024);
			if (batch == null || batch.Count == 0) break;
			long previousCursor = cursor;
			foreach (PublishedPolicyArtifactLedgerEntry entry in batch.OrderBy(x => x?.Sequence ?? long.MaxValue))
			{
				if (entry == null || entry.Sequence <= cursor) continue;
				result.Add(entry);
				cursor = entry.Sequence;
			}
			if (cursor <= previousCursor) break;
		}
		return result;
	}

	private void MigrateCanonicalHistoryIfNeeded()
	{
		if (_storage == null || _storage.HistoryMemorySchemaVersion >= HistoryMemorySchemaVersion) return;
		if (Campaign.Current == null || !Kingdom.All.Any()) return;
		EnsureCanonicalHistoryInitialized();
		WorldDiplomacyCanonicalHistoryState history = _storage.CanonicalHistory;
		if (_storage.HistoryMemorySchemaVersion == 3)
		{
			// Schema v3 temporarily kept an unbounded, exact hard-fact appendix beside the
			// summary. Fold it once into the compressible snapshot so the configured
			// summary target and independent trigger apply to the complete history again.
			string legacyProtectedFacts = RenderCanonicalProtectedFacts(
				history.Snapshot.ProtectedFacts,
				history.Snapshot.PreservedResultSourceIds);
			if (!string.IsNullOrWhiteSpace(legacyProtectedFacts))
			{
				history.Snapshot.Content = NormalizeCanonicalHistoryText(
					string.Join("\n", new[] { history.Snapshot.Content, legacyProtectedFacts }
						.Where(x => !string.IsNullOrWhiteSpace(x))));
				history.Revision++;
			}
			history.Snapshot.ProtectedFacts.Clear();
			history.Snapshot.PreservedResultSourceIds.Clear();
			string upgradedSnapshotPayload = RenderCanonicalSnapshotPayload(history.Snapshot);
			history.Snapshot.ContentHash = StablePromptHash(upgradedSnapshotPayload);
			history.Snapshot.EstimatedTokens = EstimateHistoryTokens(upgradedSnapshotPayload);
		}
		if (_storage.HistoryMemorySchemaVersion >= 3)
		{
			history.LastPolicyArtifactLedgerId =
				(WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryLedgerId() ?? "").Trim();
			if (history.LastPolicyArtifactSequence > 0L)
			{
				RebuildPublishedPolicySignaturesThrough(history.LastPolicyArtifactSequence);
			}
			RecalculateCanonicalHistoryTokens();
			_storage.HistoryMemorySchemaVersion = HistoryMemorySchemaVersion;
			InvalidateCanonicalHistoryRenderCache();
			Log("canonical diplomacy history schema upgraded version="
				+ HistoryMemorySchemaVersion.ToString(CultureInfo.InvariantCulture)
				+ " entries=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
				+ " snapshot_tokens=" + history.Snapshot.EstimatedTokens.ToString(CultureInfo.InvariantCulture));
			return;
		}
		if (_storage.HistoryMemorySchemaVersion < 3)
		{
			// Early canonical-history schemas could contain pre-final policy material whose
			// provenance cannot be proven after it was compressed. Rebuild this cold migration
			// exclusively from published documents/results, final world reports, the immutable
			// policy artifact ledger and legacy summary products; never carry the old request body.
			history.Snapshot = new WorldDiplomacyCanonicalHistorySnapshot();
			history.DeltaEntries.Clear();
			history.NextSequence = 1L;
			history.EstimatedTokens = 0L;
			history.WorldWeeklySourceHashes.Clear();
			history.WorldWeeklySourceRevisions.Clear();
			history.PolicyRevisionSignatures.Clear();
			history.LastPolicyArtifactSequence = 0L;
			history.LastPolicyArtifactLedgerId = "";
			history.Revision++;
			_canonicalHistorySourceKeys.Clear();
			foreach (WorldDiplomacyDocument document in _storage.Documents ?? new List<WorldDiplomacyDocument>())
			{
				if (document == null) continue;
				document.HistoryDeclarationRecorded = false;
				document.HistoryResultRecorded = false;
			}
		}
		history.LastPolicyArtifactLedgerId =
			(WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryLedgerId() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(history.Snapshot.Content) && history.DeltaEntries.Count == 0)
		{
			List<string> legacy = new List<string>();
			foreach (WorldDiplomacyAnnualSummary summary in (_storage.AnnualSummaries ?? new List<WorldDiplomacyAnnualSummary>())
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.Year).ThenBy(x => x.CreatedDay))
			{
				legacy.Add("[旧年度档案 " + summary.Year.ToString(CultureInfo.InvariantCulture) + "]\n" + summary.Summary.Trim());
			}
			foreach (WorldDiplomacyCompressionSummary summary in (_storage.CompressionSummaries ?? new List<WorldDiplomacyCompressionSummary>())
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.CreatedDay).ThenBy(x => x.BatchId, StringComparer.OrdinalIgnoreCase))
			{
				legacy.Add("[旧压缩档案 " + (summary.BatchId ?? "") + "]\n" + summary.Summary.Trim());
			}
			foreach (WorldDiplomacyRoundSummary summary in (_storage.RoundSummaries ?? new List<WorldDiplomacyRoundSummary>())
				.Where(x => x != null && !x.IsTokenCompressed && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.CreatedDay).ThenBy(x => x.RoundId, StringComparer.OrdinalIgnoreCase))
			{
				legacy.Add("[旧回合档案 " + (summary.RoundId ?? "") + "]\n" + summary.Summary.Trim());
			}
			if (legacy.Count > 0)
			{
				history.Snapshot.Content = "【从旧存档恢复的外交摘要；仅作历史背景】\n" + string.Join("\n", legacy.Distinct(StringComparer.Ordinal));
				history.Snapshot.CoveredThroughSequence = 0L;
				history.Snapshot.CreatedDay = CurrentDay();
				history.Snapshot.ContentHash = StablePromptHash(history.Snapshot.Content);
				history.Snapshot.EstimatedTokens = EstimateHistoryTokens(history.Snapshot.Content);
			}
		}
		List<CanonicalHistoryMigrationWorkItem> migrationItems = new List<CanonicalHistoryMigrationWorkItem>();
		foreach (WorldDiplomacyDocument document in (_storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication
				&& (!string.IsNullOrWhiteSpace(x.Body)
					|| ((x.ChangedDiplomaticState || string.Equals(x.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase))
						&& !string.IsNullOrWhiteSpace(x.MechanicalResult)))))
		{
			migrationItems.Add(new CanonicalHistoryMigrationWorkItem
			{
				Day = Math.Max(0, document.Day),
				CreatedUtcTicks = Math.Max(0L, document.CreatedUtcTicks),
				StableKey = "document:" + (document.DocumentId ?? ""),
				Document = document
			});
		}
		foreach (MyBehavior.WorldWeeklyReportHistoryEntry report in MyBehavior.GetPublishedWorldWeeklyReportHistoryForExternal())
		{
			if (report == null || string.IsNullOrWhiteSpace(report.SourceId) || string.IsNullOrWhiteSpace(report.PublishedReportText)) continue;
			migrationItems.Add(new CanonicalHistoryMigrationWorkItem
			{
				Day = Math.Max(0, report.CreatedDay),
				StableKey = "weekly:" + report.SourceId,
				WorldWeeklyReport = report
			});
		}
		List<PublishedPolicyArtifactLedgerEntry> policyArtifacts = ReadAllPublishedPolicyArtifactsForMigration();
		foreach (PublishedPolicyArtifactLedgerEntry policy in policyArtifacts)
		{
			if (policy == null || string.IsNullOrWhiteSpace(policy.PolicyId) || string.IsNullOrWhiteSpace(policy.PublishedText)) continue;
			migrationItems.Add(new CanonicalHistoryMigrationWorkItem
			{
				Day = Math.Max(0, policy.OccurredDay),
				CreatedUtcTicks = Math.Max(0L, policy.CreatedUtcTicks),
				StableKey = "policy:" + policy.Sequence.ToString("D20", CultureInfo.InvariantCulture),
				Policy = policy
			});
		}
		foreach (CanonicalHistoryMigrationWorkItem item in migrationItems
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ThenBy(x => x.StableKey, StringComparer.OrdinalIgnoreCase))
		{
			if (item.Document != null) AppendCanonicalDocumentEvents(item.Document);
			else if (item.WorldWeeklyReport != null) AppendPublishedWorldWeeklyReportArtifact(item.WorldWeeklyReport);
			else if (item.Policy != null) AppendPublishedPolicyArtifact(item.Policy);
		}
		BackfillCanonicalResponseLinksV2();
		if (policyArtifacts.Count > 0)
		{
			history.LastPolicyArtifactSequence = Math.Max(history.LastPolicyArtifactSequence, policyArtifacts.Max(x => x.Sequence));
			WorldDiplomacyPolicyContext.TryAcknowledgePublishedPolicyHistoryThrough(history.LastPolicyArtifactSequence);
		}
		_lastObservedWorldWeeklyHistoryRevision = MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
		foreach (WorldDiplomacyRound round in (_storage.CompletedRounds ?? new List<WorldDiplomacyRound>())
			.Concat(_storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { _storage.ActiveRound }).Where(x => x != null))
		{
			round.LlmTranscript?.Clear();
			round.LlmProfiledKingdomIds?.Clear();
			round.LlmLastStateSignatureByKingdom?.Clear();
			round.CachePrefix = "";
			round.CommonContractSnapshot = "";
			round.CommonContractSnapshotInitialized = false;
			round.SchemaVersion = Math.Max(round.SchemaVersion, RelaySchemaVersion);
		}
		List<WorldDiplomacyJob> invalidJobs = new List<WorldDiplomacyJob>();
		foreach (WorldDiplomacyJob job in (_storage.Jobs ?? new List<WorldDiplomacyJob>()).Where(x => x != null))
		{
			job.IsRunning = false;
			job.LlmMessages?.Clear();
			if (string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase))
			{
				invalidJobs.Add(job);
				continue;
			}
			if (!TryRebuildPendingWorldDiplomacyJob(job)) invalidJobs.Add(job);
		}
		if (invalidJobs.Count > 0)
		{
			HashSet<string> invalidIds = new HashSet<string>(invalidJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
			_storage.Jobs.RemoveAll(x => x != null && invalidIds.Contains(x.JobId ?? ""));
			foreach (WorldDiplomacyJob invalidJob in invalidJobs)
			{
				if (ResolveExchange(invalidJob.ExchangeId) != null) CompleteExchange(invalidJob.ExchangeId, "canonical_history_migration_retired_invalid_job");
			}
			WorldDiplomacyRound activeRound = _storage.ActiveRound;
			if (activeRound != null)
			{
				activeRound.RelayWaiting = false;
				bool hasRoundJob = _storage.Jobs.Any(x => x != null && string.Equals(x.RoundId, activeRound.RoundId, StringComparison.OrdinalIgnoreCase));
				bool hasPublishedRoot = ResolveDocument(activeRound.RootDocumentId)?.IsReadyForPublication == true;
				if (!hasRoundJob && !hasPublishedRoot) CloseActiveRound("canonical_history_migration_missing_root");
			}
		}
		RecalculateCanonicalHistoryTokens();
		_storage.HistoryMemorySchemaVersion = HistoryMemorySchemaVersion;
		InvalidateCanonicalHistoryRenderCache();
		Log("canonical diplomacy history migration completed entries=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
			+ " snapshot_tokens=" + history.Snapshot.EstimatedTokens.ToString(CultureInfo.InvariantCulture)
			+ " retired_jobs=" + invalidJobs.Count.ToString(CultureInfo.InvariantCulture));
	}

	private void BackfillCanonicalResponseLinksV2()
	{
		if (_storage == null || _storage.HistoryMemorySchemaVersion >= 2) return;
		foreach (WorldDiplomacyDocument document in (_storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication
				&& !string.IsNullOrWhiteSpace(x.DocumentId)
				&& !string.IsNullOrWhiteSpace(x.RespondingToOfferDocumentId)
				&& !string.IsNullOrWhiteSpace(x.Body))
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks))
		{
			bool alreadyLinked = (_storage.CanonicalHistory.DeltaEntries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
				.Any(x => x != null
					&& string.Equals(x.SourceId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.RespondingToOfferDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase));
			if (alreadyLinked) continue;
			List<string> targets = (document.AddressedKingdomIds ?? new List<string>())
				.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			AppendCanonicalHistoryEntry("declaration",
				"document:" + document.DocumentId + ":response_link_v2",
				document.DocumentId,
				document.Day,
				document.GameDate,
				document.AuthorKingdomId,
				targets,
				document.Intent,
				document.Commitment,
				document.Body,
				verified: true,
				respondingToOfferDocumentId: document.RespondingToOfferDocumentId);
		}
	}

	private void MigrateDiplomacyPromptContractIfNeeded()
	{
		if (_storage == null || _storage.PromptContractVersion >= DiplomacyPromptContractVersion) return;
		if (Campaign.Current == null || !Kingdom.All.Any()) return;
		List<WorldDiplomacyJob> retiredJobs = new List<WorldDiplomacyJob>();
		bool retiredCompression = false;
		foreach (WorldDiplomacyJob job in (_storage.Jobs ?? new List<WorldDiplomacyJob>()).Where(x => x != null).ToList())
		{
			job.IsRunning = false;
			bool isAnalysis = string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase);
			if (!UsesCanonicalHistory(job) && !isAnalysis) continue;
			job.LlmMessages?.Clear();
			job.SemanticRepairAttempts = 0;
			job.HistoryPrefixHash = "";
			if (string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase))
			{
				retiredCompression = true;
				retiredJobs.Add(job);
				continue;
			}
			if (!TryRebuildPendingWorldDiplomacyJob(job)) retiredJobs.Add(job);
		}
		if (retiredJobs.Count > 0)
		{
			HashSet<string> retiredIds = new HashSet<string>(retiredJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
			_storage.Jobs.RemoveAll(x => x != null && retiredIds.Contains(x.JobId ?? ""));
			foreach (WorldDiplomacyJob retired in retiredJobs.Where(x => !string.Equals(x.Kind, "compress", StringComparison.OrdinalIgnoreCase)))
			{
				if (ResolveExchange(retired.ExchangeId) != null) CompleteExchange(retired.ExchangeId, "prompt_contract_migration_retired_invalid_job");
			}
		}
		if (retiredCompression)
		{
			_storage.DiplomacyCompressionPending = true;
			_storage.CompressionRetryAfterHour = 0;
			_storage.CompressionRetryAttempts = 0;
		}
		foreach (WorldDiplomacyRound round in (_storage.CompletedRounds ?? new List<WorldDiplomacyRound>())
			.Concat(_storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { _storage.ActiveRound })
			.Where(x => x != null))
		{
			round.LlmTranscript?.Clear();
			round.LlmProfiledKingdomIds?.Clear();
			round.LlmLastStateSignatureByKingdom?.Clear();
			round.CachePrefix = "";
			round.CommonContractSnapshot = "";
			round.CommonContractSnapshotInitialized = false;
		}
		_lastLlmCacheAffinityKey = "";
		_storage.PromptContractVersion = DiplomacyPromptContractVersion;
		Log("diplomacy prompt contract migration completed version=" + DiplomacyPromptContractVersion.ToString(CultureInfo.InvariantCulture)
			+ " rebuilt_jobs=" + ((_storage.Jobs ?? new List<WorldDiplomacyJob>()).Count(UsesCanonicalHistory)).ToString(CultureInfo.InvariantCulture)
			+ " retired_jobs=" + retiredJobs.Count.ToString(CultureInfo.InvariantCulture)
			+ " compression_requeued=" + retiredCompression.ToString());
	}

	private bool TryRebuildPendingWorldDiplomacyJob(WorldDiplomacyJob job)
	{
		if (job == null) return false;
		if (string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase))
		{
			Kingdom author = ResolveKingdom(job.AuthorKingdomId);
			Kingdom target = ResolveKingdom(job.TargetKingdomId);
			if (author == null || (target == null && !job.AllowUntargeted)) return false;
			job.PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(author.StringId);
			job.PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(author.StringId);
			WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
			if (job.IsRelayTurn && round?.ResultSettlementPending == true
				&& !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId))
			{
				job.CandidateKingdomIds = GetResultSettlementActionableTargets(round, author)
					.Select(x => x.StringId).ToList();
			}
			// Legacy persisted flag is never authoritative for rebuilt jobs. Opening
			// documents stay action-only; later relay eligibility is derived from live state.
			job.AllowAutonomousNoAction = false;
			string commonContract = GetCommonDiplomacyContract(round);
			job.SystemPrompt = job.IsRelayTurn ? BuildRelayGenerationSystemPrompt(commonContract) : BuildGenerationSystemPrompt(commonContract);
			List<string> candidates = job.CandidateKingdomIds ?? new List<string>();
			if (job.IsRelayTurn && round == null) return false;
			string dynamicPrompt = job.IsRelayTurn
				? BuildRelayConversationTurnPrompt(round, author, target,
					prioritySource: ResolveDocument(job.SourceDocumentId), priorityResponseOnly: job.IsExternalResponseOnly)
				: BuildGenerationPrompt(author, target, ResolveExchange(job.ExchangeId), job.IsResponse,
					ResolveDocument(job.SourceDocumentId), job.IsReminder, job.RoundId, job.AllowUntargeted,
					candidates, job.IsExternalResponseOnly);
			job.UserPrompt = BuildDeclareModePrompt(dynamicPrompt);
			job.CacheAffinityKey = CanonicalHistoryCacheAffinityKey;
			job.ProfiledKingdomId = "";
			job.StrategicProfileKingdomId = author.StringId;
			job.MaxTokens = GenerationMaxTokens;
			job.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(job);
			CaptureCanonicalHistoryForJob(job, syncSources: false);
			return !string.IsNullOrWhiteSpace(job.UserPrompt);
		}
		if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
		{
			WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
			if (document == null) return false;
			job.PresentedThreatDocumentIds = GetPresentedThreatDocumentIds(document.AuthorKingdomId);
			job.PresentedThreatFollowThroughDocumentIds = GetPresentedThreatFollowThroughDocumentIds(document.AuthorKingdomId);
			job.SystemPrompt = BuildAnalysisSystemPrompt(GetCommonDiplomacyContract(ResolveRound(FirstNonEmpty(document.RoundId, document.ExchangeId))));
			job.UserPrompt = BuildAnalysisPrompt(document);
			job.CacheAffinityKey = "analyze";
			job.MaxTokens = AnalysisMaxTokens;
			return true;
		}
		if (string.Equals(job.Kind, "round_plan", StringComparison.OrdinalIgnoreCase))
		{
			WorldDiplomacyRound round = ResolveRound(job.RoundId);
			WorldDiplomacyDocument root = ResolveDocument(job.DocumentId);
			if (round == null || root == null) return false;
			job.SystemPrompt = BuildRoundPlanSystemPrompt(round);
			job.UserPrompt = BuildRoundPlanPrompt(root, job.CandidateKingdomIds ?? new List<string>());
			job.MaxTokens = AnalysisMaxTokens;
			return true;
		}
		return false;
	}

	private List<WorldDiplomacyDocument> GetRecentDocuments(int maxCount)
	{
		return _storage.Documents
			.Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(Math.Max(1, Math.Min(MaxStoredDocuments, maxCount)))
			.Select(CloneDocument)
			.ToList();
	}

	private static WorldDiplomacyDocument CloneDocument(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return null;
		}
		return JsonConvert.DeserializeObject<WorldDiplomacyDocument>(JsonConvert.SerializeObject(document));
	}

	private void MigrateAutonomousDecisionArchitectureIfNeeded()
	{
		if (_storage == null || _storage.DecisionArchitectureVersion >= DecisionArchitectureVersion) return;
		if (_storage.ActiveRound != null && (Campaign.Current == null || !Kingdom.All.Any())) return;
		int day = CurrentDay();
		List<WorldDiplomacyJob> retiredJobs = (_storage.Jobs ?? new List<WorldDiplomacyJob>())
			.Where(x => x != null && (string.Equals(x.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Kind, "round_plan", StringComparison.OrdinalIgnoreCase)
				|| !string.IsNullOrWhiteSpace(x.ForcedIntent)))
			.ToList();
		Dictionary<string, int> retiredByRound = retiredJobs
			.Where(x => string.Equals(x.Kind, "generate", StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(x.RoundId))
			.GroupBy(x => x.RoundId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> retiredJobIds = new HashSet<string>(retiredJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
		_storage.Jobs.RemoveAll(x => x != null && retiredJobIds.Contains(x.JobId ?? ""));

		if (_storage.ActiveExchange?.IsForced == true || !string.IsNullOrWhiteSpace(_storage.ActiveExchange?.PendingAction))
		{
			_storage.ActiveExchange.State = "closed_architecture_migration";
			_storage.ActiveExchange.CompletedDay = day;
			_storage.ActiveExchange = null;
		}
		_storage.SuspendedExchanges.RemoveAll(x => x == null || x.IsForced || !string.IsNullOrWhiteSpace(x.PendingAction));
		_storage.RecentTopicUses.Clear();
		foreach (WarPressureEntry entry in _storage.WarPressure.Where(x => x != null))
		{
			entry.IsEscalationArmed = false;
			entry.ArmedDay = 0;
			entry.NeedsFreshEscalation = false;
		}
		_storage.ForcedWarToggleWasEnabled = false;

		WorldDiplomacyRound active = _storage.ActiveRound;
		if (active != null)
		{
			active.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
			active.LlmProfiledKingdomIds ??= new List<string>();
			active.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			active.LlmTranscript.Clear();
			active.LlmProfiledKingdomIds.Clear();
			active.LlmLastStateSignatureByKingdom.Clear();
			active.CachePrefix = "";
			active.RelayWaiting = false;
			active.RequiresSharedBorder = false;
			active.TopicSeedContext = "";
			active.TopicFingerprint = "";
			active.EventSourceType = "";
			active.EventMotif = "";
			active.EventLocation = "";
			active.AllowedFiction = "";
			active.ForbiddenFiction = "";
			active.PotentialActionIntents ??= new List<string>();
			active.PotentialActionIntents.Clear();
			foreach (WorldDiplomacyRoundParticipant participant in (active.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Where(x => x != null))
			{
				participant.Role = "";
				participant.Agenda = "";
				participant.PrimaryTargetKingdomId = "";
				participant.PreferredOutcome = "";
				participant.RedLine = "";
				participant.Leverage = "";
				participant.RequiredContribution = "";
			}
			if (retiredByRound.TryGetValue(active.RoundId ?? "", out int retiredCount))
			{
				int publishedAutomatic = _storage.Documents.Count(x => x != null && !x.IsPlayerAuthored
					&& string.Equals(x.RoundId, active.RoundId, StringComparison.OrdinalIgnoreCase));
				active.AutomaticDocumentsStarted = Math.Max(publishedAutomatic, active.AutomaticDocumentsStarted - retiredCount);
			}
			_storage.RelayArrivals.RemoveAll(x => x != null && string.Equals(x.RoundId, active.RoundId, StringComparison.OrdinalIgnoreCase));
			WorldDiplomacyDocument root = ResolveDocument(active.RootDocumentId);
			Kingdom rootAuthor = ResolveKingdom(root?.AuthorKingdomId);
			if (root?.IsReadyForPublication != true)
			{
				CloseActiveRound("technical_architecture_migration_unpublished_round");
				_storage.NextNormalRoundDay = day + 1;
			}
			else if (rootAuthor == null || rootAuthor.IsEliminated || !HasIndependentWorldDiplomacyAuthority(rootAuthor))
			{
				CloseActiveRound("technical_architecture_migration_invalid_root_author");
				_storage.NextNormalRoundDay = day + 1;
			}
			else
			{
				active.RoundTopic = FirstNonEmpty(root.PlannedRoundTopic, root.Title, "外交交涉");
				active.TopicCategory = NormalizeIntent(root.Intent) is "warning" or "ultimatum"
					? "war_escalation"
					: InferTopicCategory(active.RoundTopic, rootAuthor, ResolveKingdom(root.TargetKingdomId));
				active.SchemaVersion = RelaySchemaVersion;
				if (active.RelayPlanned)
				{
					List<string> previousRoute = active.RelayRouteKingdomIds ?? new List<string>();
					string cursorKingdomId = active.RelayCursor >= 0 && active.RelayCursor < previousRoute.Count
						? previousRoute[active.RelayCursor]
						: rootAuthor.StringId;
					active.RelayRouteKingdomIds = (active.RelayRouteKingdomIds ?? new List<string>())
						.Where(id => ResolveKingdom(id) is Kingdom kingdom && !kingdom.IsEliminated && HasIndependentWorldDiplomacyAuthority(kingdom))
						.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
					if (!active.RelayRouteKingdomIds.Contains(rootAuthor.StringId, StringComparer.OrdinalIgnoreCase))
					{
						active.RelayRouteKingdomIds.Insert(0, rootAuthor.StringId);
					}
					active.RelayDirection = active.RelayDirection < 0 ? -1 : 1;
					active.RelayCursor = active.RelayRouteKingdomIds.FindIndex(id => string.Equals(id, cursorKingdomId, StringComparison.OrdinalIgnoreCase));
					if (active.RelayCursor < 0) active.RelayCursor = active.RelayRouteKingdomIds.FindIndex(id => string.Equals(id, rootAuthor.StringId, StringComparison.OrdinalIgnoreCase));
					if (active.RelayCursor < 0) active.RelayCursor = 0;
					HashSet<string> migratedRouteIds = new HashSet<string>(active.RelayRouteKingdomIds, StringComparer.OrdinalIgnoreCase);
					foreach (WorldDiplomacyRoundParticipant participant in (active.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Where(x => x != null))
					{
						participant.SelectedForRelay = migratedRouteIds.Contains(participant.KingdomId ?? "");
					}
					if (active.RelayRouteKingdomIds.Count < 2)
					{
						active.RelayPlanned = false;
						active.RelayCursor = 0;
						active.RelayDirection = 1;
					}
				}
				active.CachePrefix = "";
			}
		}
		_storage.DecisionArchitectureVersion = DecisionArchitectureVersion;
		Log("autonomous diplomacy architecture migration completed retiredJobs=" + retiredJobs.Count.ToString(CultureInfo.InvariantCulture)
			+ " activeRound=" + (_storage.ActiveRound?.RoundId ?? "none"));
	}

	private void MigrateDiplomaticThreatsToNextDeclarationRules()
	{
		List<WorldDiplomacyDocument> orderedDocuments = (_storage?.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication && !string.IsNullOrWhiteSpace(x.DocumentId))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		Dictionary<string, int> documentIndex = orderedDocuments
			.Select((document, index) => new { document.DocumentId, Index = index })
			.GroupBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);
		int currentDay = CurrentDay();
		foreach (WorldDiplomacyThreat threat in (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Where(IsOpenDiplomaticThreat).ToList())
		{
			threat.Stage = NormalizeToken(threat.Stage) == "ultimatum" || !string.IsNullOrWhiteSpace(threat.UltimatumDocumentId)
				? "ultimatum"
				: "warning";
			threat.StageDocumentId = FirstNonEmpty(threat.StageDocumentId,
				threat.Stage == "ultimatum" ? threat.UltimatumDocumentId : threat.WarningDocumentId);
			threat.ObligationRoundId = "";
			threat.ObligationClaimedDay = 0;
			for (int stagePass = 0; stagePass < 2 && IsOpenDiplomaticThreat(threat); stagePass++)
			{
				WorldDiplomacyDocument source = ResolveDocument(threat.StageDocumentId);
				bool sourceMatchesThreat = source?.IsReadyForPublication == true
					&& string.Equals(NormalizeIntent(source.Intent), threat.Stage, StringComparison.Ordinal)
					&& string.Equals(source.AuthorKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(source.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
					&& documentIndex.ContainsKey(threat.StageDocumentId);
				if (!sourceMatchesThreat)
				{
					InvalidateDiplomaticThreatForNormalization(threat,
						"source_document_structure_mismatch", currentDay);
					Log("legacy open diplomatic threat invalidated without prestige penalty threat=" + threat.ThreatId
						+ " source=" + threat.StageDocumentId + " reason=source_document_structure_mismatch");
					break;
				}
				threat.TargetDecision = "pending";
				threat.TargetDecisionDocumentId = "";
				threat.TargetDecisionRoundId = "";
				threat.TargetDecisionDay = 0;
				threat.NonComplianceHistoryRecorded = false;
				if (!documentIndex.TryGetValue(threat.StageDocumentId, out int sourceIndex)) break;
				WorldDiplomacyDocument firstTargetDeclaration = orderedDocuments.Skip(sourceIndex + 1)
					.FirstOrDefault(x => string.Equals(x.AuthorKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
				if (firstTargetDeclaration == null) break;
				bool wasValidCompliance = NormalizeIntent(firstTargetDeclaration.Intent) == "comply_ultimatum"
					&& string.Equals(firstTargetDeclaration.TargetKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(firstTargetDeclaration.RespondingToThreatDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase);
				if (wasValidCompliance)
				{
					threat.Status = "complied";
					threat.TargetDecision = "complied";
					threat.TargetDecisionDocumentId = firstTargetDeclaration.DocumentId ?? "";
					threat.TargetDecisionRoundId = firstTargetDeclaration.RoundId ?? "";
					threat.TargetDecisionDay = Math.Max(0, firstTargetDeclaration.Day);
					threat.ComplianceDocumentId = firstTargetDeclaration.DocumentId ?? "";
					threat.ResolutionDocumentId = firstTargetDeclaration.DocumentId ?? "";
					threat.ResolutionRoundId = firstTargetDeclaration.RoundId ?? "";
					threat.ResolutionReason = "migrated_valid_compliance_first_declaration";
					threat.IssuerResolutionNoticePending = true;
					threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.TargetDecisionDay);
					break;
				}
				threat.TargetDecision = "noncomplied";
				threat.TargetDecisionDocumentId = firstTargetDeclaration.DocumentId ?? "";
				threat.TargetDecisionRoundId = firstTargetDeclaration.RoundId ?? "";
				threat.TargetDecisionDay = Math.Max(0, firstTargetDeclaration.Day);
				threat.ResolutionReason = "migrated_target_noncompliance_first_declaration";
				threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.TargetDecisionDay);
				CaptureDiplomaticThreatNonComplianceEvent(threat);

				if (!documentIndex.TryGetValue(firstTargetDeclaration.DocumentId, out int targetDecisionIndex)) break;
				WorldDiplomacyDocument firstIssuerFollowThrough = orderedDocuments.Skip(targetDecisionIndex + 1)
					.FirstOrDefault(x => string.Equals(x.AuthorKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase));
				if (firstIssuerFollowThrough == null) break;
				string followThroughIntent = NormalizeIntent(firstIssuerFollowThrough.Intent);
				bool targetsThreatTarget = string.Equals(firstIssuerFollowThrough.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
				if (string.Equals(threat.Stage, "warning", StringComparison.OrdinalIgnoreCase)
					&& followThroughIntent == "ultimatum" && targetsThreatTarget)
				{
					threat.Stage = "ultimatum";
					threat.UltimatumDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
					threat.StageDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
					threat.StageRoundId = firstIssuerFollowThrough.RoundId ?? "";
					threat.StageIssuedDay = Math.Max(0, firstIssuerFollowThrough.Day);
					threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.StageIssuedDay);
					continue;
				}
				if (string.Equals(threat.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
					&& followThroughIntent == "declare_war" && targetsThreatTarget
					&& firstIssuerFollowThrough.ChangedDiplomaticState)
				{
					threat.Status = "enforced";
					threat.ResolutionDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
					threat.ResolutionRoundId = firstIssuerFollowThrough.RoundId ?? "";
					threat.ResolutionReason = "migrated_issuer_declared_war_in_next_declaration";
					threat.HistoryResultRecorded = true;
					threat.UpdatedDay = Math.Max(threat.UpdatedDay, Math.Max(0, firstIssuerFollowThrough.Day));
					break;
				}

				InvalidateDiplomaticThreatForNormalization(threat,
					"legacy_next_declaration_already_consumed_without_retroactive_penalty", currentDay);
				Log("legacy threat follow-through closed without retroactive prestige penalty threat=" + threat.ThreatId
					+ " declaration=" + firstIssuerFollowThrough.DocumentId + " intent=" + followThroughIntent);
				break;
			}
		}
	}

	private void MigrateDiplomaticThreatComplianceConsequencesV3()
	{
		foreach (WorldDiplomacyThreat threat in _storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
		{
			if (threat == null) continue;
			threat.PolicyConditionSignalKey = "";
			threat.PolicyConditionPolicyId = "";
			threat.PolicyConditionPolicyName = "";
			threat.PolicyConditionOwnerKingdomId = "";
			threat.PolicyConditionAffectedKingdomId = "";
			threat.PolicyConditionCancellationCompleted = true;
			threat.PolicyConditionCancellationStatus = "legacy_not_bound";
			if (string.Equals((threat.Status ?? "").Trim(), "open", StringComparison.OrdinalIgnoreCase)) continue;
			// Existing terminal threats predate the issuer reward. Never award them retroactively.
			threat.IssuerRewardAmount = 0;
			threat.IssuerRewardSnapshotCaptured = true;
			threat.IssuerRewardCompleted = true;
			threat.IssuerRewardHistoryRecorded = true;
		}
	}

	private void NormalizeDiplomaticThreats(bool allowWorldValidation)
	{
		_storage.DiplomaticThreats ??= new List<WorldDiplomacyThreat>();
		_storage.NationalPrestigeByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.InternationalReputationNaturalChangeLastDayByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.NationalPrestigeRelationModifiers ??= new List<WorldDiplomacyPrestigeRelationModifier>();
		if (_storage.DiplomaticThreatStateSchemaVersion < DiplomaticThreatStateSchemaVersion)
		{
			int previousThreatSchemaVersion = _storage.DiplomaticThreatStateSchemaVersion;
			if (_storage.DiplomaticThreatStateSchemaVersion <= 0)
			{
				// Older releases did not persist enforceable threat obligations. Never infer them
				// from old prose, because that would create a retroactive prestige penalty.
				_storage.DiplomaticThreats.Clear();
				_storage.NationalPrestigeByKingdom.Clear();
			}
			else
			{
				if (_storage.DiplomaticThreatStateSchemaVersion < 2)
				{
					MigrateDiplomaticThreatsToNextDeclarationRules();
				}
				if (_storage.DiplomaticThreatStateSchemaVersion < 3)
				{
					MigrateDiplomaticThreatComplianceConsequencesV3();
				}
			}
			_storage.DiplomaticThreatStateSchemaVersion = DiplomaticThreatStateSchemaVersion;
			Log("diplomatic threat state migrated previous=" + previousThreatSchemaVersion.ToString(CultureInfo.InvariantCulture)
				+ " current=" + DiplomaticThreatStateSchemaVersion.ToString(CultureInfo.InvariantCulture));
		}

		_storage.NationalPrestigeByKingdom = _storage.NationalPrestigeByKingdom
			.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				x => x.Key,
				x => Math.Max(0, Math.Min(DefaultNationalPrestige, x.Last().Value)),
				StringComparer.OrdinalIgnoreCase);
		_storage.InternationalReputationByKingdom = _storage.InternationalReputationByKingdom
			.Where(x => !string.IsNullOrWhiteSpace(x.Key))
			.GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				x => x.Key,
				x => Math.Max(0, Math.Min(100, x.Last().Value)),
				StringComparer.OrdinalIgnoreCase);
		_storage.InternationalReputationNaturalChangeLastDayByKingdom =
			_storage.InternationalReputationNaturalChangeLastDayByKingdom
				.Where(x => !string.IsNullOrWhiteSpace(x.Key))
				.GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					x => x.Key,
					x => Math.Max(0, x.Last().Value),
					StringComparer.OrdinalIgnoreCase);
		_storage.NationalPrestigeRelationModifiers = _storage.NationalPrestigeRelationModifiers
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId)
				&& !string.IsNullOrWhiteSpace(x.RulerHeroId) && !string.IsNullOrWhiteSpace(x.VassalLeaderHeroId))
			.GroupBy(x => x.KingdomId.Trim() + "|" + x.RulerHeroId.Trim() + "|" + x.VassalLeaderHeroId.Trim(), StringComparer.OrdinalIgnoreCase)
			.Select(x => x.Last())
			.ToList();

		_storage.DiplomaticThreats = _storage.DiplomaticThreats
			.Where(x => x != null)
			.ToList();
		foreach (WorldDiplomacyThreat threat in _storage.DiplomaticThreats)
		{
			NormalizeDiplomaticThreatRecord(threat);
		}
		_storage.DiplomaticThreats.RemoveAll(x => string.IsNullOrWhiteSpace(x.IssuerKingdomId)
			|| string.IsNullOrWhiteSpace(x.TargetKingdomId)
			|| string.Equals(x.IssuerKingdomId, x.TargetKingdomId, StringComparison.OrdinalIgnoreCase));

		int currentDay = CurrentDay();
		foreach (IGrouping<string, WorldDiplomacyThreat> issuerGroup in _storage.DiplomaticThreats
			.Where(IsOpenDiplomaticThreat)
			.GroupBy(x => x.IssuerKingdomId, StringComparer.OrdinalIgnoreCase))
		{
			WorldDiplomacyThreat retained = issuerGroup
				.OrderByDescending(x => x.UpdatedDay)
				.ThenByDescending(x => x.StageIssuedDay)
				.ThenByDescending(x => string.Equals(x.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
				.ThenByDescending(x => x.CreatedDay)
				.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
				.First();
			foreach (WorldDiplomacyThreat duplicate in issuerGroup.Where(x => !ReferenceEquals(x, retained)))
			{
				InvalidateDiplomaticThreatForNormalization(duplicate, "duplicate_open_threat_for_issuer", currentDay);
			}
		}

		if (allowWorldValidation && Campaign.Current != null)
		{
			IAllianceCampaignBehavior alliance = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			foreach (WorldDiplomacyThreat threat in _storage.DiplomaticThreats.Where(IsOpenDiplomaticThreat))
			{
				Kingdom issuer = ResolveKingdom(threat.IssuerKingdomId);
				Kingdom target = ResolveKingdom(threat.TargetKingdomId);
				if (issuer == null || target == null || issuer == target
					|| issuer.IsEliminated || target.IsEliminated
					|| !HasIndependentWorldDiplomacyAuthority(issuer)
					|| !HasIndependentWorldDiplomacyAuthority(target))
				{
					InvalidateDiplomaticThreatForNormalization(threat, "threat_party_no_longer_eligible", currentDay);
					continue;
				}
				if (FactionManager.IsAtWarAgainstFaction(issuer, target))
				{
					threat.Status = "invalidated";
					threat.ResolutionReason = "war_already_started_outside_pending_declaration";
					threat.UpdatedDay = Math.Max(threat.UpdatedDay, currentDay);
					threat.ObligationRoundId = "";
					threat.ObligationClaimedDay = 0;
					// The war itself is already recorded by the game event/history path.
					threat.HistoryResultRecorded = true;
					continue;
				}
				if (alliance?.IsAllyWithKingdom(issuer, target) == true)
				{
					InvalidateDiplomaticThreatForNormalization(threat, "threat_parties_became_allies", currentDay);
				}
			}
		}

		int cutoffDay = currentDay - DiplomaticThreatRetentionDays;
		List<WorldDiplomacyThreat> active = _storage.DiplomaticThreats
			.Where(IsRetainedActiveDiplomaticThreat)
			.ToList();
		HashSet<WorldDiplomacyThreat> activeSet = new HashSet<WorldDiplomacyThreat>(active);
		List<WorldDiplomacyThreat> protectedTerminal = _storage.DiplomaticThreats
			.Where(x => !activeSet.Contains(x) && NeedsDiplomaticThreatSettlementRetention(x))
			.OrderByDescending(x => x.UpdatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		HashSet<WorldDiplomacyThreat> protectedSet = new HashSet<WorldDiplomacyThreat>(protectedTerminal);
		int ordinarySlots = Math.Max(0, MaxStoredDiplomaticThreats - protectedTerminal.Count);
		List<WorldDiplomacyThreat> ordinaryTerminal = _storage.DiplomaticThreats
			.Where(x => !activeSet.Contains(x) && !protectedSet.Contains(x) && x.UpdatedDay >= cutoffDay)
			.OrderByDescending(x => x.UpdatedDay)
			.ThenByDescending(x => x.CreatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.Take(ordinarySlots)
			.ToList();
		_storage.DiplomaticThreats = active
			.Concat(protectedTerminal)
			.Concat(ordinaryTerminal)
			.OrderBy(x => x.CreatedDay)
			.ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static void NormalizeDiplomaticThreatRecord(WorldDiplomacyThreat threat)
	{
		if (threat == null) return;
		threat.ThreatId = string.IsNullOrWhiteSpace(threat.ThreatId) ? NewId("diplomacy_threat") : threat.ThreatId.Trim();
		threat.IssuerKingdomId = (threat.IssuerKingdomId ?? "").Trim();
		threat.TargetKingdomId = (threat.TargetKingdomId ?? "").Trim();
		string normalizedStage = NormalizeToken(threat.Stage);
		threat.Stage = normalizedStage == "ultimatum" || !string.IsNullOrWhiteSpace(threat.UltimatumDocumentId)
			? "ultimatum"
			: "warning";
		threat.Status = NormalizeToken(threat.Status);
		if (string.IsNullOrWhiteSpace(threat.Status)) threat.Status = "open";
		if (string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
		{
			threat.Status = "complied";
		}
		threat.WarningDocumentId = (threat.WarningDocumentId ?? "").Trim();
		threat.WarningActionId = (threat.WarningActionId ?? "").Trim();
		threat.UltimatumDocumentId = (threat.UltimatumDocumentId ?? "").Trim();
		threat.UltimatumActionId = (threat.UltimatumActionId ?? "").Trim();
		threat.StageDocumentId = (threat.StageDocumentId ?? "").Trim();
		threat.StageActionId = (threat.StageActionId ?? "").Trim();
		threat.StageRoundId = (threat.StageRoundId ?? "").Trim();
		threat.TargetDecision = NormalizeToken(threat.TargetDecision);
		if (string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)) threat.TargetDecision = "complied";
		else if (threat.TargetDecision != "noncomplied") threat.TargetDecision = "pending";
		threat.TargetDecisionDocumentId = (threat.TargetDecisionDocumentId ?? "").Trim();
		threat.TargetDecisionActionId = (threat.TargetDecisionActionId ?? "").Trim();
		threat.TargetDecisionRoundId = (threat.TargetDecisionRoundId ?? "").Trim();
		threat.TargetDecisionDay = Math.Max(0, threat.TargetDecisionDay);
		threat.NonComplianceEvents = (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
			.Where(x => x != null)
			.Select(x =>
			{
				x.Stage = NormalizeToken(x.Stage) == "ultimatum" ? "ultimatum" : "warning";
				x.StageDocumentId = (x.StageDocumentId ?? "").Trim();
				x.StageActionId = (x.StageActionId ?? "").Trim();
				x.DecisionDocumentId = (x.DecisionDocumentId ?? "").Trim();
				x.DecisionActionId = (x.DecisionActionId ?? "").Trim();
				x.DecisionRoundId = (x.DecisionRoundId ?? "").Trim();
				x.DecisionDay = Math.Max(0, x.DecisionDay);
				return x;
			})
			.Where(x => x.StageDocumentId.Length > 0 && x.DecisionDocumentId.Length > 0)
			.GroupBy(x => x.StageDocumentId + "\n" + x.StageActionId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(x => x.HistoryRecorded).ThenByDescending(x => x.DecisionDay).First())
			.OrderBy(x => x.DecisionDay)
			.ThenBy(x => x.StageDocumentId, StringComparer.OrdinalIgnoreCase)
			.Take(2)
			.ToList();
		CaptureDiplomaticThreatNonComplianceEvent(threat);
		WorldDiplomacyThreatNonComplianceEvent currentNonCompliance = threat.NonComplianceEvents.FirstOrDefault(x => x != null
			&& string.Equals(x.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase));
		if (currentNonCompliance != null) threat.NonComplianceHistoryRecorded = currentNonCompliance.HistoryRecorded;
		threat.ObligationRoundId = "";
		threat.ComplianceDocumentId = (threat.ComplianceDocumentId ?? "").Trim();
		threat.ComplianceActionId = (threat.ComplianceActionId ?? "").Trim();
		threat.ResolutionRoundId = (threat.ResolutionRoundId ?? "").Trim();
		threat.ResolutionDocumentId = (threat.ResolutionDocumentId ?? "").Trim();
		threat.ResolutionActionId = (threat.ResolutionActionId ?? "").Trim();
		threat.ResolutionReason = Limit((threat.ResolutionReason ?? "").Trim(), 180);
		threat.DomesticPenaltyRulingClanId = (threat.DomesticPenaltyRulingClanId ?? "").Trim();
		threat.CreatedDay = Math.Max(0, threat.CreatedDay);
		threat.StageIssuedDay = Math.Max(threat.CreatedDay, threat.StageIssuedDay);
		threat.UpdatedDay = Math.Max(threat.StageIssuedDay, threat.UpdatedDay);
		threat.ObligationClaimedDay = 0;
		threat.ReputationPenaltyAmount = Math.Max(0, threat.ReputationPenaltyAmount);
		if (string.IsNullOrWhiteSpace(threat.StageDocumentId))
		{
			threat.StageDocumentId = threat.Stage == "ultimatum" ? threat.UltimatumDocumentId : threat.WarningDocumentId;
		}
		if (threat.Stage == "ultimatum" && string.IsNullOrWhiteSpace(threat.UltimatumDocumentId))
		{
			threat.UltimatumDocumentId = threat.StageDocumentId;
		}
		else if (threat.Stage == "warning" && string.IsNullOrWhiteSpace(threat.WarningDocumentId))
		{
			threat.WarningDocumentId = threat.StageDocumentId;
		}
		if (!IsOpenDiplomaticThreat(threat))
		{
			threat.ObligationRoundId = "";
			threat.ObligationClaimedDay = 0;
		}
		threat.DomesticPenaltyEligibleClanIds = NormalizeDiplomaticThreatIdList(threat.DomesticPenaltyEligibleClanIds);
		threat.DomesticPenaltyAppliedClanIds = NormalizeDiplomaticThreatIdList(threat.DomesticPenaltyAppliedClanIds);
		threat.DomesticPenaltySkippedClanIds = NormalizeDiplomaticThreatIdList(threat.DomesticPenaltySkippedClanIds);
		foreach (string settledClanId in threat.DomesticPenaltyAppliedClanIds.Concat(threat.DomesticPenaltySkippedClanIds))
		{
			if (!threat.DomesticPenaltyEligibleClanIds.Contains(settledClanId, StringComparer.OrdinalIgnoreCase))
			{
				threat.DomesticPenaltyEligibleClanIds.Add(settledClanId);
			}
		}
		if (threat.DomesticPenaltyEligibleClanIds.Count > 0 || threat.DomesticPenaltyAppliedClanIds.Count > 0
			|| threat.DomesticPenaltySkippedClanIds.Count > 0 || threat.DomesticPenaltyCompleted)
		{
			threat.DomesticPenaltySnapshotCaptured = true;
		}
		if (threat.DomesticPenaltySnapshotCaptured
			&& threat.DomesticPenaltyEligibleClanIds.All(id => threat.DomesticPenaltyAppliedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)
				|| threat.DomesticPenaltySkippedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
		{
			threat.DomesticPenaltyCompleted = true;
		}

		threat.PolicyConditionSignalKey = (threat.PolicyConditionSignalKey ?? "").Trim();
		threat.PolicyConditionPolicyId = (threat.PolicyConditionPolicyId ?? "").Trim();
		threat.PolicyConditionPolicyName = Limit((threat.PolicyConditionPolicyName ?? "").Trim(), 80);
		threat.PolicyConditionOwnerKingdomId = (threat.PolicyConditionOwnerKingdomId ?? "").Trim();
		threat.PolicyConditionAffectedKingdomId = (threat.PolicyConditionAffectedKingdomId ?? "").Trim();
		threat.PolicyConditionBoundDay = Math.Max(0, threat.PolicyConditionBoundDay);
		threat.PolicyConditionCancellationStatus = NormalizeToken(threat.PolicyConditionCancellationStatus);
		threat.PolicyConditionCancellationDay = Math.Max(0, threat.PolicyConditionCancellationDay);
		if (threat.PolicyConditionPolicyId.Length == 0 || threat.PolicyConditionOwnerKingdomId.Length == 0)
		{
			threat.PolicyConditionCancellationCompleted = true;
			threat.PolicyConditionCancellationStatus = "not_bound";
		}
		else if (threat.PolicyConditionCancellationStatus is "cancelled" or "already_inactive")
		{
			threat.PolicyConditionCancellationCompleted = true;
		}
		else if (!threat.PolicyConditionCancellationCompleted)
		{
			threat.PolicyConditionCancellationStatus = "pending";
		}

		threat.IssuerRewardRulingClanId = (threat.IssuerRewardRulingClanId ?? "").Trim();
		threat.IssuerRewardAmount = Math.Max(0, Math.Min(
			DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax,
			threat.IssuerRewardAmount));
		threat.IssuerRewardEligibleClanIds = NormalizeDiplomaticThreatIdList(threat.IssuerRewardEligibleClanIds);
		threat.IssuerRewardAppliedClanIds = NormalizeDiplomaticThreatIdList(threat.IssuerRewardAppliedClanIds);
		threat.IssuerRewardSkippedClanIds = NormalizeDiplomaticThreatIdList(threat.IssuerRewardSkippedClanIds);
		foreach (string settledClanId in threat.IssuerRewardAppliedClanIds.Concat(threat.IssuerRewardSkippedClanIds))
		{
			if (!threat.IssuerRewardEligibleClanIds.Contains(settledClanId, StringComparer.OrdinalIgnoreCase))
			{
				threat.IssuerRewardEligibleClanIds.Add(settledClanId);
			}
		}
		if (threat.IssuerRewardEligibleClanIds.Count > 0 || threat.IssuerRewardAppliedClanIds.Count > 0
			|| threat.IssuerRewardSkippedClanIds.Count > 0 || threat.IssuerRewardCompleted)
		{
			threat.IssuerRewardSnapshotCaptured = true;
		}
		if (threat.IssuerRewardSnapshotCaptured
			&& threat.IssuerRewardEligibleClanIds.All(id => threat.IssuerRewardAppliedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)
				|| threat.IssuerRewardSkippedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
		{
			threat.IssuerRewardCompleted = true;
		}
		if (threat.IssuerRewardCompleted && threat.IssuerRewardAmount <= 0)
		{
			threat.IssuerRewardHistoryRecorded = true;
		}
	}

	private static List<string> NormalizeDiplomaticThreatIdList(IEnumerable<string> ids)
	{
		return (ids ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static bool IsOpenDiplomaticThreat(WorldDiplomacyThreat threat)
	{
		return threat != null && string.Equals(threat.Status, "open", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsRetainedActiveDiplomaticThreat(WorldDiplomacyThreat threat)
	{
		if (threat == null) return false;
		return IsOpenDiplomaticThreat(threat)
			|| string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase);
	}

	private static bool NeedsDiplomaticThreatSettlementRetention(WorldDiplomacyThreat threat)
	{
		if (threat == null) return false;
		bool domesticPenaltyPending = (string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase))
			&& !threat.DomesticPenaltyCompleted;
		bool domesticHistoryPending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
			&& threat.DomesticPenaltyCompleted && !threat.DomesticPenaltyHistoryRecorded;
		bool complianceConsequencePending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
			&& (!threat.PolicyConditionCancellationCompleted || !threat.IssuerRewardCompleted);
		bool issuerRewardHistoryPending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
			&& threat.IssuerRewardCompleted && !threat.IssuerRewardHistoryRecorded;
		bool nonComplianceHistoryPending = (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
			.Any(x => x != null && !x.HistoryRecorded)
			|| (string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
				&& !threat.NonComplianceHistoryRecorded);
		return domesticPenaltyPending || domesticHistoryPending || complianceConsequencePending
			|| issuerRewardHistoryPending || nonComplianceHistoryPending || !threat.HistoryResultRecorded;
	}

	private static void InvalidateDiplomaticThreatForNormalization(WorldDiplomacyThreat threat, string reason, int currentDay)
	{
		if (threat == null) return;
		threat.Status = "invalidated";
		threat.ResolutionReason = Limit(reason, 180);
		threat.UpdatedDay = Math.Max(threat.UpdatedDay, Math.Max(0, currentDay));
		threat.ObligationRoundId = "";
		threat.ObligationClaimedDay = 0;
		// This is storage repair rather than a gameplay result and must not create a history event.
		threat.HistoryResultRecorded = true;
	}

	private WorldDiplomacyThreat FindOpenDiplomaticThreat(string issuerKingdomId, string targetKingdomId)
	{
		string issuerId = (issuerKingdomId ?? "").Trim();
		string targetId = (targetKingdomId ?? "").Trim();
		if (issuerId.Length == 0 || targetId.Length == 0) return null;
		return (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => IsOpenDiplomaticThreat(x)
			&& string.Equals(x.IssuerKingdomId, issuerId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
	}

	private WorldDiplomacyThreat FindOpenDiplomaticThreatIssuedBy(string issuerKingdomId)
	{
		string issuerId = (issuerKingdomId ?? "").Trim();
		if (issuerId.Length == 0) return null;
		return (_storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => IsOpenDiplomaticThreat(x)
			&& string.Equals(x.IssuerKingdomId, issuerId, StringComparison.OrdinalIgnoreCase));
	}

	private int GetNationalPrestige(string kingdomId)
	{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		return _storage?.NationalPrestigeByKingdom != null
			&& _storage.NationalPrestigeByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(DefaultNationalPrestige, value))
			: DefaultNationalPrestige;
	}

	private int GetInternationalReputation(string kingdomId)
	{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		return _storage?.InternationalReputationByKingdom != null
			&& _storage.InternationalReputationByKingdom.TryGetValue(normalizedId, out int value)
			? Math.Max(0, Math.Min(100, value))
			: DefaultInternationalReputation;
	}

	private int ApplyNationalPrestigeDelta(
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason)
	{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultNationalPrestige;
		_storage ??= new WorldDiplomacyStorage();
		_storage.NationalPrestigeByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		int before = GetNationalPrestige(normalizedId);
		int updated = (int)Math.Max(0L, Math.Min(DefaultNationalPrestige, (long)before + delta));
		_storage.NationalPrestigeByKingdom[normalizedId] = updated;
		RecordDiplomaticStandingChange(sourceDocument, "national_prestige", normalizedId, before, updated, reason);
		ReconcileNationalPrestigeVassalRelations(ResolveKingdomIncludingEliminated(normalizedId));
		return updated;
	}

	private int ApplyInternationalReputationDelta(
		string kingdomId,
		int delta,
		WorldDiplomacyDocument sourceDocument,
		string reason)
	{
		string normalizedId = (kingdomId ?? "").Trim();
		if (normalizedId.Length == 0) return DefaultInternationalReputation;
		_storage ??= new WorldDiplomacyStorage();
		_storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		int before = GetInternationalReputation(normalizedId);
		int boundedDelta = Math.Max(-MaximumInternationalReputationChangePerDocument,
			Math.Min(MaximumInternationalReputationChangePerDocument, delta));
		int updated = (int)Math.Max(0L, Math.Min(100L, (long)before + boundedDelta));
		_storage.InternationalReputationByKingdom[normalizedId] = updated;
		RecordDiplomaticStandingChange(sourceDocument, "international_reputation", normalizedId, before, updated, reason);
		return updated;
	}

	private static void ApplyInternationalReputationEvaluation(WorldDiplomacyDocument document, JObject json)
	{
		if (document == null) return;
		string modelReason = Limit(
			SanitizePublicDiplomacyText(ReadString(json, "international_reputation_reason")), 240);
		if (TryReadInteger(json, "international_reputation_delta", out int modelDelta)
			&& modelDelta != 0
			&& !string.IsNullOrWhiteSpace(modelReason))
		{
			document.InternationalReputationEvaluationDelta = Math.Max(-MaximumInternationalReputationChangePerDocument,
				Math.Min(MaximumInternationalReputationChangePerDocument, modelDelta));
			document.InternationalReputationEvaluationReason = modelReason;
			document.InternationalReputationEvaluationSource = "llm";
			return;
		}

		int fallbackDelta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
		document.InternationalReputationEvaluationDelta = fallbackDelta;
		document.InternationalReputationEvaluationReason = Limit(fallbackReason, 240);
		document.InternationalReputationEvaluationSource = "local_structured_fallback";
	}

	private static int CalculateStructuredInternationalReputationFallback(
		WorldDiplomacyDocument document,
		out string reason)
	{
		List<string> intents = document.Actions?.Where(x => x != null)
			.Select(x => NormalizeIntent(x.Intent))
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList() ?? new List<string>();
		if (intents.Count == 0) intents.Add(NormalizeIntent(document.Intent));
		int score = 0;
		bool hasPositiveAction = false;
		bool hasNegativeAction = false;
		foreach (string intent in intents)
		{
			int actionScore = intent switch
			{
				"accept_peace" or "accept_alliance" or "accept_trade" or "apology" or "concession" => 2,
				"propose_peace" or "propose_alliance" or "propose_trade" or "comply_ultimatum" => 1,
				"break_alliance" or "cancel_trade" or "declare_war" => -2,
				"condemn" or "warning" or "ultimatum" => -1,
				_ => 0
			};
			score += actionScore;
			hasPositiveAction |= actionScore > 0;
			hasNegativeAction |= actionScore < 0;
		}
		string basis = score > 0
			? "建设性承诺、合作或承担责任"
			: score < 0
				? "公开施压、毁约或冲突升级"
				: "";
		string move = NormalizeNegotiationMove(document.NegotiationMove);
		if (score == 0 && document.MadeDiplomaticProgress)
		{
			score = 1;
			basis = "本篇提供了新的可执行条件或谈判进展";
		}
		else if (score == 0 && move is "question" or "clarification" or "acknowledge_concern"
			or "counterproposal" or "conditional_acceptance" or "partial_concession"
			or "request_concession" or "revise_terms")
		{
			score = 1;
			basis = "本篇提出了能够继续谈判的新问题、回应或条件";
		}
		else if (score == 0 && move is "set_deadline" or "final_offer" or "withdraw_offer"
			or "end_negotiation" or "declare_deadlock")
		{
			score = -1;
			basis = "本篇收紧、撤回或终止了谈判空间";
		}
		else if (score == 0 && string.Equals(document.Tone, "hostile", StringComparison.OrdinalIgnoreCase))
		{
			score = -1;
			basis = "本篇以敌对措辞加剧了国际疑虑";
		}
		else if (score == 0 && intents.Any(x => x.StartsWith("reject_", StringComparison.Ordinal)))
		{
			score = 1;
			basis = "本篇及时、明确地答复了正式提案";
		}
		else if (score == 0 && string.Equals(document.Tone, "conciliatory", StringComparison.OrdinalIgnoreCase))
		{
			score = 1;
			basis = "本篇以克制且可沟通的方式公开立场";
		}
		else if (score == 0 && hasPositiveAction && hasNegativeAction)
		{
			score = -1;
			basis = "同篇正负行为相互抵消，系统按冲突与毁约风险作保守判定";
		}
		else if (score == 0)
		{
			score = -1;
			basis = "本篇没有提供新的条件、解释、行动或谈判进展";
		}
		score = Math.Max(-4, Math.Min(4, score));
		if (score == 0) score = -1;
		reason = "系统依据本篇已解析的外交动作作出非零保守判定：" + basis + "。";
		return score;
	}

	private void SettleInternationalReputationForDocument(WorldDiplomacyDocument document)
	{
		if (document == null || document.InternationalReputationSettled
			|| string.IsNullOrWhiteSpace(document.AuthorKingdomId)) return;
		int delta = Math.Max(-MaximumInternationalReputationChangePerDocument,
			Math.Min(MaximumInternationalReputationChangePerDocument, document.InternationalReputationEvaluationDelta));
		if (delta == 0)
		{
			delta = CalculateStructuredInternationalReputationFallback(document, out string fallbackReason);
			document.InternationalReputationEvaluationReason = Limit(fallbackReason, 240);
			document.InternationalReputationEvaluationSource = "local_nonzero_settlement_guard";
		}
		string reason = Limit(FirstNonEmpty(
			document.InternationalReputationEvaluationReason,
			"本篇宣言的公开表现改变了国际观感。"), 240);
		document.InternationalReputationEvaluationDelta = delta;
		document.InternationalReputationEvaluationReason = reason;
		if (string.IsNullOrWhiteSpace(document.InternationalReputationEvaluationSource))
		{
			document.InternationalReputationEvaluationSource = "legacy_or_unspecified";
		}
		int before = GetInternationalReputation(document.AuthorKingdomId);
		int after = ApplyInternationalReputationDelta(document.AuthorKingdomId, delta, document, reason);
		document.InternationalReputationSettled = true;
		Log("international-reputation.settled document=" + document.DocumentId
			+ " author=" + document.AuthorKingdomId
			+ " delta=" + delta.ToString(CultureInfo.InvariantCulture)
			+ " before=" + before.ToString(CultureInfo.InvariantCulture)
			+ " after=" + after.ToString(CultureInfo.InvariantCulture)
			+ " source=" + document.InternationalReputationEvaluationSource);
	}

	private void RecoverUnsettledAiInternationalReputation()
	{
		if (_storage?.Documents == null) return;
		int recovered = 0;
		foreach (WorldDiplomacyDocument document in _storage.Documents
			.Where(x => x != null && !x.IsPlayerAuthored && x.IsReadyForPublication
				&& !x.InternationalReputationSettled
				&& (x.InternationalReputationEvaluationDelta != 0
					|| !string.IsNullOrWhiteSpace(x.InternationalReputationEvaluationReason)))
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks))
		{
			SettleInternationalReputationForDocument(document);
			recovered++;
		}
		if (recovered > 0)
		{
			Log("international-reputation.recovered documents="
				+ recovered.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void RecordDiplomaticStandingChange(
		WorldDiplomacyDocument document,
		string kind,
		string kingdomId,
		int before,
		int after,
		string reason)
	{
		if (document == null || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(kingdomId)) return;
		document.DiplomaticStandingChanges ??= new List<WorldDiplomacyStandingChange>();
		string normalizedReason = Limit((reason ?? "").Trim(), 240);
		if (document.DiplomaticStandingChanges.Any(x => x != null
			&& string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.Reason, normalizedReason, StringComparison.Ordinal))) return;
		document.DiplomaticStandingChanges.Add(new WorldDiplomacyStandingChange
		{
			Kind = kind,
			KingdomId = kingdomId,
			KingdomName = KingdomName(ResolveKingdomIncludingEliminated(kingdomId)),
			Before = before,
			After = after,
			Delta = after - before,
			Reason = normalizedReason
		});
	}

	private static int GetNationalPrestigeRelationTarget(int prestige)
	{
		if (prestige >= 80) return 0;
		if (prestige >= 60) return -2;
		if (prestige >= 40) return -5;
		if (prestige >= 20) return -10;
		if (prestige >= 1) return -15;
		return -20;
	}

	private void ReconcileAllNationalPrestigeVassalRelations()
	{
		if (Campaign.Current == null) return;
		foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null))
		{
			ReconcileNationalPrestigeVassalRelations(kingdom);
		}
	}

	private void ReconcileNationalPrestigeVassalRelations(Kingdom kingdom)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(kingdom.StringId)) return;
		_storage.NationalPrestigeRelationModifiers ??= new List<WorldDiplomacyPrestigeRelationModifier>();
		Hero ruler = kingdom.RulingClan?.Leader;
		int desired = kingdom.IsEliminated || ruler == null ? 0 : GetNationalPrestigeRelationTarget(GetNationalPrestige(kingdom.StringId));
		HashSet<string> activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (ruler != null && kingdom.Clans != null)
		{
			for (int index = 0; index < kingdom.Clans.Count; index++)
			{
				Clan clan = kingdom.Clans[index];
				Hero vassalLeader = clan?.Leader;
				if (clan == null || clan == kingdom.RulingClan || clan.Kingdom != kingdom || clan.IsEliminated
					|| clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || vassalLeader == null || vassalLeader == ruler) continue;
				string key = kingdom.StringId + "|" + ruler.StringId + "|" + vassalLeader.StringId;
				activeKeys.Add(key);
				WorldDiplomacyPrestigeRelationModifier modifier = _storage.NationalPrestigeRelationModifiers.FirstOrDefault(x => x != null
					&& string.Equals(x.KingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.RulerHeroId, ruler.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.VassalLeaderHeroId, vassalLeader.StringId, StringComparison.OrdinalIgnoreCase));
				if (modifier == null)
				{
					modifier = new WorldDiplomacyPrestigeRelationModifier
					{
						KingdomId = kingdom.StringId,
						RulerHeroId = ruler.StringId,
						VassalLeaderHeroId = vassalLeader.StringId
					};
					_storage.NationalPrestigeRelationModifiers.Add(modifier);
				}
				ApplyNationalPrestigeRelationDifference(modifier, vassalLeader, ruler, desired);
			}
		}

		foreach (WorldDiplomacyPrestigeRelationModifier stale in _storage.NationalPrestigeRelationModifiers
			.Where(x => x != null && string.Equals(x.KingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase)
				&& !activeKeys.Contains(x.KingdomId + "|" + x.RulerHeroId + "|" + x.VassalLeaderHeroId)).ToList())
		{
			Hero oldRuler = ResolveHeroById(stale.RulerHeroId);
			Hero oldVassal = ResolveHeroById(stale.VassalLeaderHeroId);
			if (oldRuler != null && oldVassal != null) ApplyNationalPrestigeRelationDifference(stale, oldVassal, oldRuler, 0);
			if (stale.AppliedAmount == 0 || oldRuler == null || oldVassal == null)
			{
				_storage.NationalPrestigeRelationModifiers.Remove(stale);
			}
		}
	}

	private static void ApplyNationalPrestigeRelationDifference(
		WorldDiplomacyPrestigeRelationModifier modifier,
		Hero vassalLeader,
		Hero ruler,
		int desired)
	{
		if (modifier == null || vassalLeader == null || ruler == null) return;
		int difference = desired - modifier.AppliedAmount;
		if (difference == 0) return;
		try
		{
			int before = CharacterRelationManager.GetHeroRelation(vassalLeader, ruler);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(vassalLeader, ruler, difference, showQuickNotification: false);
			int after = CharacterRelationManager.GetHeroRelation(vassalLeader, ruler);
			modifier.AppliedAmount += after - before;
		}
		catch
		{
		}
	}

	private static Hero ResolveHeroById(string heroId)
	{
		string normalized = (heroId ?? "").Trim();
		if (normalized.Length == 0) return null;
		try
		{
			Hero hero = Game.Current?.ObjectManager?.GetObject<Hero>(normalized);
			if (hero != null) return hero;
		}
		catch
		{
		}
		return (Hero.AllAliveHeroes ?? new List<Hero>()).FirstOrDefault(x => x != null
			&& string.Equals(x.StringId, normalized, StringComparison.OrdinalIgnoreCase));
	}

	private void ApplyZeroPrestigeBreachRelationPenalty(Kingdom kingdom, int amount)
	{
		if (kingdom?.RulingClan?.Leader == null || amount >= 0 || kingdom.Clans == null) return;
		Hero ruler = kingdom.RulingClan.Leader;
		for (int index = 0; index < kingdom.Clans.Count; index++)
		{
			Clan clan = kingdom.Clans[index];
			if (clan == null || clan == kingdom.RulingClan || clan.Kingdom != kingdom || clan.IsEliminated
				|| clan.IsUnderMercenaryService || clan.IsClanTypeMercenary || clan.Leader == null || clan.Leader == ruler) continue;
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, ruler, amount, showQuickNotification: false);
			}
			catch
			{
			}
		}
	}

	private static string OfferCooldownDomainToken(WorldDiplomacyOfferDomain domain)
	{
		return domain switch
		{
			WorldDiplomacyOfferDomain.Trade => "trade",
			WorldDiplomacyOfferDomain.Alliance => "alliance",
			_ => ""
		};
	}

	private static bool TryParseOfferCooldownDomain(string value, out WorldDiplomacyOfferDomain domain)
	{
		string normalized = (value ?? "").Trim();
		if (string.Equals(normalized, "trade", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "propose_trade", StringComparison.OrdinalIgnoreCase))
		{
			domain = WorldDiplomacyOfferDomain.Trade;
			return true;
		}
		if (string.Equals(normalized, "alliance", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "propose_alliance", StringComparison.OrdinalIgnoreCase))
		{
			domain = WorldDiplomacyOfferDomain.Alliance;
			return true;
		}
		domain = WorldDiplomacyOfferDomain.None;
		return false;
	}

	private void RebuildOfferCooldownIndex()
	{
		_offerCooldownByKey.Clear();
		foreach (WorldDiplomacyOfferCooldown cooldown in _storage?.OfferCooldowns ?? new List<WorldDiplomacyOfferCooldown>())
		{
			if (cooldown == null || cooldown.LastFailedRoundDay < 0
				|| !TryParseOfferCooldownDomain(cooldown.Domain, out WorldDiplomacyOfferDomain domain)) continue;
			WorldDiplomacyOfferCooldownKey key = new WorldDiplomacyOfferCooldownKey(
				cooldown.ProposerKingdomId,
				cooldown.TargetKingdomId,
				domain);
			if (!key.IsValid) continue;
			if (!_offerCooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown existing)
				|| existing.LastFailedRoundDay <= cooldown.LastFailedRoundDay)
			{
				_offerCooldownByKey[key] = cooldown;
			}
		}
	}

	private void NormalizeOfferCooldownStorage()
	{
		_storage ??= new WorldDiplomacyStorage();
		_storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();
		Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> normalized =
			new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
		foreach (WorldDiplomacyOfferCooldown cooldown in _storage.OfferCooldowns)
		{
			if (cooldown == null || cooldown.LastFailedRoundDay < 0
				|| !TryParseOfferCooldownDomain(cooldown.Domain, out WorldDiplomacyOfferDomain domain)) continue;
			WorldDiplomacyOfferCooldownKey key = new WorldDiplomacyOfferCooldownKey(
				cooldown.ProposerKingdomId,
				cooldown.TargetKingdomId,
				domain);
			if (!key.IsValid) continue;
			if (normalized.TryGetValue(key, out WorldDiplomacyOfferCooldown existing)
				&& existing.LastFailedRoundDay > cooldown.LastFailedRoundDay) continue;
			normalized[key] = new WorldDiplomacyOfferCooldown
			{
				ProposerKingdomId = key.ProposerKingdomId,
				TargetKingdomId = key.TargetKingdomId,
				Domain = OfferCooldownDomainToken(domain),
				LastFailedRoundDay = cooldown.LastFailedRoundDay,
				SourceRoundId = (cooldown.SourceRoundId ?? "").Trim()
			};
		}
		_storage.OfferCooldowns = normalized.Values
			.OrderByDescending(x => x.LastFailedRoundDay)
			.ThenBy(x => x.ProposerKingdomId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.TargetKingdomId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.Domain, StringComparer.OrdinalIgnoreCase)
			.Take(MaxStoredOfferCooldowns)
			.ToList();
		_storage.OfferCooldownStateSchemaVersion = OfferCooldownStateSchemaVersion;
		RebuildOfferCooldownIndex();
	}

	private bool IsTradeAllianceProposalCoolingDown(Kingdom proposer, Kingdom target, string proposalIntent)
	{
		int cooldownDays = GetTradeAllianceFailedProposalCooldownDays();
		if (cooldownDays <= 0 || proposer == null || target == null
			|| !WorldDiplomacyOfferCooldownRules.TryGetProposalDomain(proposalIntent, out WorldDiplomacyOfferDomain domain)) return false;
		WorldDiplomacyOfferCooldownKey key = new WorldDiplomacyOfferCooldownKey(proposer.StringId, target.StringId, domain);
		return key.IsValid
			&& _offerCooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown cooldown)
			&& WorldDiplomacyOfferCooldownRules.IsCoolingDown(cooldown.LastFailedRoundDay, CurrentDay(), cooldownDays);
	}

	private void RemoveOfferCooldown(WorldDiplomacyOfferCooldownKey key)
	{
		if (!key.IsValid) return;
		if (_offerCooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown existing))
		{
			_storage.OfferCooldowns.Remove(existing);
		}
		_offerCooldownByKey.Remove(key);
	}

	private void ClearBilateralOfferCooldowns(Kingdom first, Kingdom second, WorldDiplomacyOfferDomain domain)
	{
		if (first == null || second == null || first == second || domain == WorldDiplomacyOfferDomain.None) return;
		RemoveOfferCooldown(new WorldDiplomacyOfferCooldownKey(first.StringId, second.StringId, domain));
		RemoveOfferCooldown(new WorldDiplomacyOfferCooldownKey(second.StringId, first.StringId, domain));
	}

	private static void MarkOpenBilateralOffersAccepted(
		WorldDiplomacyRound round,
		Kingdom first,
		Kingdom second,
		WorldDiplomacyOfferDomain domain)
	{
		if (round == null || first == null || second == null || first == second
			|| domain == WorldDiplomacyOfferDomain.None) return;
		foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
		{
			if (offer == null || !string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase)
				|| !WorldDiplomacyOfferCooldownRules.TryGetProposalDomain(offer.Intent, out WorldDiplomacyOfferDomain offerDomain)
				|| offerDomain != domain) continue;
			bool samePair = (string.Equals(offer.ProposerKingdomId, first.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(offer.TargetKingdomId, second.StringId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(offer.ProposerKingdomId, second.StringId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(offer.TargetKingdomId, first.StringId, StringComparison.OrdinalIgnoreCase));
			if (samePair) offer.Status = "accepted";
		}
	}

	private void UpsertOfferCooldown(WorldDiplomacyOfferCooldownKey key, int failedRoundDay, string sourceRoundId)
	{
		if (!key.IsValid || failedRoundDay < 0) return;
		if (!_offerCooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown cooldown))
		{
			cooldown = new WorldDiplomacyOfferCooldown();
			_storage.OfferCooldowns.Add(cooldown);
		}
		cooldown.ProposerKingdomId = key.ProposerKingdomId;
		cooldown.TargetKingdomId = key.TargetKingdomId;
		cooldown.Domain = OfferCooldownDomainToken(key.Domain);
		cooldown.LastFailedRoundDay = failedRoundDay;
		cooldown.SourceRoundId = sourceRoundId ?? "";
		_offerCooldownByKey[key] = cooldown;
		if (_storage.OfferCooldowns.Count > MaxStoredOfferCooldowns) NormalizeOfferCooldownStorage();
	}

	private static bool IsTechnicalOfferCooldownCloseReason(string reason)
	{
		string normalized = (reason ?? "").Trim();
		return normalized.StartsWith("technical_", StringComparison.OrdinalIgnoreCase)
			|| normalized.StartsWith("round_plan_", StringComparison.OrdinalIgnoreCase)
			|| normalized.StartsWith("canonical_history_migration_", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "relay_has_no_participants", StringComparison.OrdinalIgnoreCase);
	}

	private void SettleTradeAllianceOfferCooldownsForClosedRound(WorldDiplomacyRound round)
	{
		if (round == null) return;
		List<WorldDiplomacyOfferRoundObservation> observations = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null)
			.Select(x => new WorldDiplomacyOfferRoundObservation(
				x.ProposerKingdomId,
				x.TargetKingdomId,
				x.Intent,
				x.Status))
			.ToList();
		List<WorldDiplomacyOfferCooldownDecision> decisions = WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(observations);
		bool recordFailures = !IsTechnicalOfferCooldownCloseReason(round.CloseReason);
		int started = 0;
		int cleared = 0;
		foreach (WorldDiplomacyOfferCooldownDecision decision in decisions)
		{
			if (decision.Action == WorldDiplomacyOfferCooldownAction.ClearCooldown)
			{
				bool existed = _offerCooldownByKey.ContainsKey(decision.Key);
				RemoveOfferCooldown(decision.Key);
				if (existed) cleared++;
			}
			else if (recordFailures)
			{
				UpsertOfferCooldown(decision.Key, round.CompletedDay, round.RoundId);
				started++;
			}
		}
		if (started > 0 || cleared > 0)
		{
			Log("trade/alliance proposal cooldowns settled round=" + round.RoundId
				+ " started=" + started.ToString(CultureInfo.InvariantCulture)
				+ " cleared=" + cleared.ToString(CultureInfo.InvariantCulture)
				+ " recordFailures=" + recordFailures);
		}
	}

	private void MigrateResultSettlementStateIfNeeded()
	{
		if (_storage == null
			|| _storage.ResultSettlementStateSchemaVersion >= ResultSettlementStateSchemaVersion) return;
		WorldDiplomacyRound round = _storage.ActiveRound;
		if (round != null && string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			&& !round.ResultSettlementPending)
		{
			List<WorldDiplomacyDocument> published = (_storage.Documents ?? new List<WorldDiplomacyDocument>())
				.Where(x => x != null && x.IsReadyForPublication
					&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Day)
				.ThenBy(x => x.CreatedUtcTicks)
				.ToList();
			foreach (WorldDiplomacyDocument document in published)
			{
				if (TryGetConfirmedRoundResult(document, round, out string closeReason, out string roundStatus))
				{
					BeginOrExtendRoundResultSettlement(round, document, closeReason, roundStatus);
				}
			}
			RemoveAnsweredMigratedWarResponseSlots(round, published);
		}
		_storage.ResultSettlementStateSchemaVersion = ResultSettlementStateSchemaVersion;
		Log("round result-settlement state migrated version="
			+ ResultSettlementStateSchemaVersion.ToString(CultureInfo.InvariantCulture)
			+ " active=" + (round?.ResultSettlementPending == true).ToString());
	}

	private static void RemoveAnsweredMigratedWarResponseSlots(
		WorldDiplomacyRound round,
		IReadOnlyCollection<WorldDiplomacyDocument> published)
	{
		if (round == null || published == null || published.Count == 0) return;
		foreach (WorldDiplomacyResultSettlementSlot slot in (round.ResultSettlementSlots
			?? new List<WorldDiplomacyResultSettlementSlot>()).ToList())
		{
			if (!SettlementSlotHasKind(slot, "war_response")) continue;
			List<WorldDiplomacyDocument> wars = published.Where(x => x != null
				&& (slot.SourceDocumentIds ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase)
				&& DocumentHasSuccessfulWarAgainst(x, slot.KingdomId)).ToList();
			if (wars.Count == 0 || wars.Any(war => !published.Any(response => response != null
				&& response.IsReadyForPublication
				&& string.Equals(response.AuthorKingdomId, slot.KingdomId, StringComparison.OrdinalIgnoreCase)
				&& (response.Day > war.Day
					|| (response.Day == war.Day && response.CreatedUtcTicks > war.CreatedUtcTicks))))) continue;
			List<string> remainingKinds = (slot.Kind ?? "").Split('+')
				.Where(x => !string.IsNullOrWhiteSpace(x)
					&& !string.Equals(x, "war_response", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (remainingKinds.Count == 0) round.ResultSettlementSlots.Remove(slot);
			else slot.Kind = string.Join("+", remainingKinds);
		}
	}

	private static bool DocumentHasSuccessfulWarAgainst(WorldDiplomacyDocument document, string targetKingdomId)
	{
		if (document == null || string.IsNullOrWhiteSpace(targetKingdomId)) return false;
		if (document.Actions?.Count > 0)
		{
			return document.Actions.Any(x => x != null && x.ChangedDiplomaticState
				&& NormalizeIntent(x.Intent) == "declare_war"
				&& string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase));
		}
		return document.ChangedDiplomaticState && NormalizeIntent(document.Intent) == "declare_war"
			&& string.Equals(document.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase);
	}

	private void NormalizeStorage(bool allowWorldValidation = false)
	{
		_storage ??= new WorldDiplomacyStorage();
		_storage.CanonicalHistory ??= new WorldDiplomacyCanonicalHistoryState();
		_storage.CompletedRounds ??= new List<WorldDiplomacyRound>();
		_storage.PropagationArrivals ??= new List<WorldDiplomacyPropagationArrival>();
		_storage.SettlementKnowledge ??= new List<WorldDiplomacySettlementKnowledge>();
		_storage.KingdomKnowledge ??= new List<WorldDiplomacyKingdomKnowledge>();
		_storage.NobleKnowledge ??= new List<WorldDiplomacyKingdomKnowledge>();
		_storage.PendingParticipationEvaluations ??= new List<WorldDiplomacyParticipationRequest>();
		_storage.PendingSpeeches ??= new List<WorldDiplomacyPendingSpeech>();
		_storage.RelayArrivals ??= new List<WorldDiplomacyRelayArrival>();
		_storage.PlayerOpportunities ??= new List<WorldDiplomacyPlayerOpportunity>();
		_storage.RoundSummaries ??= new List<WorldDiplomacyRoundSummary>();
		_storage.PendingPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		_storage.ProcessedPolicySignalKeys ??= new List<string>();
		_storage.Documents ??= new List<WorldDiplomacyDocument>();
		if (_storage.DiplomacyNotificationStateSchemaVersion < DiplomacyNotificationStateSchemaVersion)
		{
			foreach (WorldDiplomacyDocument document in _storage.Documents.Where(x => x != null))
			{
				if (document.IsPlayerAuthored)
				{
					document.RumorNotified = true;
					document.FormalNoticeShown = true;
					continue;
				}
				if (document.IsReadyForPublication) document.RumorNotified = true;
				if (document.IsNotified || document.IsRead || document.IsCompressed) document.FormalNoticeShown = true;
			}
			_storage.DiplomacyNotificationStateSchemaVersion = DiplomacyNotificationStateSchemaVersion;
		}
		_storage.AnnualSummaries ??= new List<WorldDiplomacyAnnualSummary>();
		_storage.CompressionSummaries ??= new List<WorldDiplomacyCompressionSummary>();
		_storage.WarPressure ??= new List<WarPressureEntry>();
		_storage.ActiveWarLedgers ??= new List<WorldDiplomacyWarLedger>();
		_storage.RecentBattles ??= new List<WorldDiplomacyBattleFact>();
		_storage.NativeSignals ??= new List<NativeDiplomacySignal>();
		_storage.RecentTopicUses ??= new List<WorldDiplomacyTopicUse>();
		_storage.Jobs ??= new List<WorldDiplomacyJob>();
		_storage.SuspendedExchanges ??= new List<WorldDiplomacyExchange>();
		_storage.LastOffensiveWarDayByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.LastPeaceDayByPair ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.OfferCooldowns ??= new List<WorldDiplomacyOfferCooldown>();
		NormalizeOfferCooldownStorage();
		_storage.CompressionRetryAfterHour = Math.Max(0, _storage.CompressionRetryAfterHour);
		_storage.CompressionRetryAttempts = Math.Max(0, Math.Min(31, _storage.CompressionRetryAttempts));
		NormalizeDiplomaticThreats(allowWorldValidation);
		if (allowWorldValidation)
		{
			try
			{
				MigrateAutonomousDecisionArchitectureIfNeeded();
			}
			catch (Exception ex)
			{
				// Leave the version unstamped so OnSessionLaunched or the next daily tick can retry.
				Log("autonomous diplomacy architecture migration deferred after error=" + ex.Message);
			}
		}
		_storage.PendingPolicySignals.RemoveAll(x => x == null
			|| string.IsNullOrWhiteSpace(x.SignalKey)
			|| string.IsNullOrWhiteSpace(x.IssuerKingdomId)
			|| string.IsNullOrWhiteSpace(x.TargetKingdomId));
		_storage.PendingPolicySignals = _storage.PendingPolicySignals
			.GroupBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.OrderByDescending(y => y.PublishedDay).First())
			.OrderByDescending(x => x.PublishedDay)
			.Take(MaxPendingPolicySignals)
			.ToList();
		_storage.ProcessedPolicySignalKeys = _storage.ProcessedPolicySignalKeys
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		// 旧选题历史仅为反序列化兼容保留，不再参与自主决策。
		_storage.RecentTopicUses.Clear();
		if (_storage.ProcessedPolicySignalKeys.Count > MaxProcessedPolicySignalKeys)
		{
			_storage.ProcessedPolicySignalKeys.RemoveRange(0, _storage.ProcessedPolicySignalKeys.Count - MaxProcessedPolicySignalKeys);
		}
		foreach (WorldDiplomacyPropagationArrival arrival in _storage.PropagationArrivals.Where(x => x != null))
		{
			if (string.IsNullOrWhiteSpace(arrival.Scope)) arrival.Scope = "civilian";
		}
		_storage.PropagationArrivals = _storage.PropagationArrivals
			.Where(x => x != null
				&& !string.IsNullOrWhiteSpace(x.DocumentId)
				&& (!string.IsNullOrWhiteSpace(x.SettlementId) || (IsCourtArrival(x) && !string.IsNullOrWhiteSpace(x.KingdomId))))
			.OrderBy(x => x.DueDay)
			.ThenBy(x => IsCourtArrival(x) ? 0 : 1)
			.ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		_storage.ActiveWarLedgers.RemoveAll(x => x == null
			|| string.IsNullOrWhiteSpace(x.FirstKingdomId)
			|| string.IsNullOrWhiteSpace(x.SecondKingdomId));
		foreach (WorldDiplomacyWarLedger ledger in _storage.ActiveWarLedgers)
		{
			ledger.SettlementChanges ??= new List<WorldDiplomacySettlementChange>();
			ledger.SettlementChanges.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.SettlementId));
		}
		_storage.Documents.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.DocumentId));
		_storage.RecentBattles.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.BattleId));
		foreach (WorldDiplomacyBattleFact battle in _storage.RecentBattles)
		{
			battle.AttackerKingdomIds ??= new List<string>();
			battle.DefenderKingdomIds ??= new List<string>();
			battle.AttackerLeaderNames ??= new List<string>();
			battle.DefenderLeaderNames ??= new List<string>();
			if (string.IsNullOrWhiteSpace(battle.GameDate))
			{
				battle.GameDate = FormatCampaignDate(battle.Day);
			}
		}
		bool migrateLegacyPropagationState = allowWorldValidation && _storage.PropagationReliabilityVersion < 1;
		int legacyPropagationRecoveryWindow = Math.Max(GetCivilianSpreadDays(), GetCourtMaxDeliveryDays()) + 7;
		foreach (WorldDiplomacyDocument document in _storage.Documents)
		{
			if (document.RoundProgressHandled) document.RoundAccountingHandled = true;
			document.NegotiationMove = NormalizeNegotiationMove(document.NegotiationMove);
			document.InternationalReputationEvaluationDelta = Math.Max(-MaximumInternationalReputationChangePerDocument,
				Math.Min(MaximumInternationalReputationChangePerDocument, document.InternationalReputationEvaluationDelta));
			document.InternationalReputationEvaluationReason = Limit((document.InternationalReputationEvaluationReason ?? "").Trim(), 240);
			document.InternationalReputationEvaluationSource = Limit(
				NormalizeToken(document.InternationalReputationEvaluationSource), 40);
			document.DiplomaticStandingChanges ??= new List<WorldDiplomacyStandingChange>();
			document.DiplomaticStandingChanges = document.DiplomaticStandingChanges
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Kind) && !string.IsNullOrWhiteSpace(x.KingdomId))
				.Take(16)
				.ToList();
			document.Title = Limit(SanitizePublicDiplomacyText(document.Title), 100);
			document.Body = NormalizeBody(SanitizePublicDiplomacyText(document.Body));
			document.AddressedKingdomIds ??= new List<string>();
			document.MentionedKingdomIds ??= new List<string>();
			document.PlannedKingdomIds ??= new List<string>();
			document.PresentedThreatDocumentIds = NormalizeDiplomaticThreatIdList(document.PresentedThreatDocumentIds);
			document.PresentedThreatFollowThroughDocumentIds = NormalizeDiplomaticThreatIdList(document.PresentedThreatFollowThroughDocumentIds);
			document.RespondingToOfferActionId = (document.RespondingToOfferActionId ?? "").Trim();
			document.RespondingToThreatDocumentId ??= "";
			document.RespondingToThreatActionId = (document.RespondingToThreatActionId ?? "").Trim();
			document.ResultSettlementSlotId ??= "";
			if (document.Actions != null)
			{
				List<WorldDiplomacyDocumentAction> normalizedActions = new List<WorldDiplomacyDocumentAction>(MaxDiplomaticActionsPerDocument);
				HashSet<string> normalizedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (WorldDiplomacyDocumentAction action in document.Actions)
				{
					string targetId = (action?.TargetKingdomId ?? "").Trim();
					if (action == null || string.IsNullOrWhiteSpace(targetId)
						|| string.IsNullOrWhiteSpace(action.Intent) || !normalizedTargets.Add(targetId)) continue;
					action.TargetKingdomId = targetId;
					normalizedActions.Add(action);
					if (normalizedActions.Count >= MaxDiplomaticActionsPerDocument) break;
				}
				document.Actions = normalizedActions;
				HashSet<string> actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				for (int actionIndex = 0; actionIndex < document.Actions.Count; actionIndex++)
				{
					WorldDiplomacyDocumentAction action = document.Actions[actionIndex];
					action.ActionId = (action.ActionId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(action.ActionId) || !actionIds.Add(action.ActionId))
					{
						int suffix = actionIndex + 1;
						do
						{
							action.ActionId = "action_" + suffix.ToString(CultureInfo.InvariantCulture);
							suffix++;
						}
						while (!actionIds.Add(action.ActionId));
					}
					action.TargetKingdomId = (action.TargetKingdomId ?? "").Trim();
					Kingdom actionTarget = ResolveKingdom(action.TargetKingdomId);
					action.TargetKingdomName = FirstNonEmpty(actionTarget == null ? "" : KingdomName(actionTarget), action.TargetKingdomName);
					action.Intent = NormalizeIntent(action.Intent);
					action.NegotiationMove = NormalizeNegotiationMove(action.NegotiationMove);
					action.Commitment = DefaultCommitmentForIntent(action.Intent);
					action.RespondingToOfferDocumentId = (action.RespondingToOfferDocumentId ?? "").Trim();
					action.RespondingToOfferActionId = (action.RespondingToOfferActionId ?? "").Trim();
					action.RespondingToThreatDocumentId = (action.RespondingToThreatDocumentId ?? "").Trim();
					action.RespondingToThreatActionId = (action.RespondingToThreatActionId ?? "").Trim();
				}
				if (document.Actions.Count == 0) document.Actions = null;
				else
				{
					document.AddressedKingdomIds = NormalizeKingdomIdList(document.Actions.Select(x => x.TargetKingdomId), document.AuthorKingdomId);
					MirrorPrimaryActionToDocument(document, document.Actions[0]);
					document.ChangedDiplomaticState = document.Actions.Any(x => x.ChangedDiplomaticState);
					document.MechanicalResult = BuildMultiActionMechanicalResult(document.Actions);
					document.RequiresResponse = document.Actions.Any(x => x.RequiresResponse);
				}
			}
			if (string.IsNullOrWhiteSpace(document.RoundId) && !string.IsNullOrWhiteSpace(document.ExchangeId)) document.RoundId = document.ExchangeId;
			if (document.AddressedKingdomIds.Count == 0 && !string.IsNullOrWhiteSpace(document.TargetKingdomId)) document.AddressedKingdomIds.Add(document.TargetKingdomId);
			if (string.IsNullOrWhiteSpace(document.GameDate))
			{
				document.GameDate = FormatCampaignDate(document.Day);
			}
			if (string.Equals(document.TargetKingdomName, "未知王国", StringComparison.Ordinal))
			{
				document.TargetKingdomName = "";
			}
			if (!document.IsReadyForPublication
				&& (!string.IsNullOrWhiteSpace(document.AnalysisStatus) || document.IsCompressed))
			{
				document.IsReadyForPublication = true;
			}
			if (migrateLegacyPropagationState && document.IsReadyForPublication)
			{
				bool belongsToActiveRound = _storage.ActiveRound != null
					&& string.Equals(_storage.ActiveRound.RoundId, document.RoundId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(_storage.ActiveRound.State, "active", StringComparison.OrdinalIgnoreCase);
				bool stillRelevant = belongsToActiveRound || document.Day >= CurrentDay() - legacyPropagationRecoveryWindow;
				if (!document.PropagationStarted && string.IsNullOrWhiteSpace(document.OriginSettlementId))
				{
					// Pre-propagation-format declarations were globally visible already.
					document.PropagationCompleted = true;
				}
				else
				{
					document.PropagationCompleted = !stillRelevant || HasCompleteLegacyPropagationCoverage(document);
				}
			}
			if (document.IsReadyForPublication && !document.PropagationStarted && string.IsNullOrWhiteSpace(document.OriginSettlementId))
			{
				// Documents from the pre-propagation save format were globally visible already.
				document.HasReachedPlayerCourt = document.HasReachedPlayerCourt || !document.IsPlayerAuthored;
			}
		}
		if (allowWorldValidation)
		{
			try
			{
				MigrateCanonicalHistoryIfNeeded();
			}
			catch (Exception ex)
			{
				Log("canonical diplomacy history migration deferred after error=" + ex.Message);
			}
		}
		foreach (WorldDiplomacyJob legacyRoundCompression in _storage.Jobs
			.Where(x => x != null && string.Equals(x.Kind, "round_compress", StringComparison.OrdinalIgnoreCase)).ToList())
		{
			WorldDiplomacyRound round = ResolveRound(legacyRoundCompression.RoundId);
			List<WorldDiplomacyDocument> documents = _storage.Documents.Where(x => x != null
				&& (string.Equals(x.RoundId, legacyRoundCompression.RoundId, StringComparison.OrdinalIgnoreCase)
					|| (legacyRoundCompression.CompressionDocumentIds ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase))).ToList();
			if (round != null && documents.Count > 0) CommitLocalRoundSummary(round, documents);
		}
		_storage.Jobs.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.JobId)
			|| string.Equals(x.Kind, "participate", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(x.Kind, "round_compress", StringComparison.OrdinalIgnoreCase)
			|| (x.IsRelayTurn && x.Priority == 98 && !string.IsNullOrWhiteSpace(x.ForcedIntent))
			|| (string.Equals(x.Kind, "compress", StringComparison.OrdinalIgnoreCase) && x.CompressionTargetTokens <= 0));
		foreach (WorldDiplomacyJob job in _storage.Jobs)
		{
			job.CandidateKingdomIds ??= new List<string>();
			job.TriggerDocumentIds ??= new List<string>();
			job.PresentedThreatDocumentIds = NormalizeDiplomaticThreatIdList(job.PresentedThreatDocumentIds);
			job.PresentedThreatFollowThroughDocumentIds = NormalizeDiplomaticThreatIdList(job.PresentedThreatFollowThroughDocumentIds);
			job.PresentedLegalActionSignature ??= "";
			job.ResultSettlementSlotId ??= "";
			job.LlmMessages ??= new List<WorldDiplomacyLlmMessage>();
			job.CompressionRoundIds ??= new List<string>();
			job.ForcedIntent = "";
		}
		Dictionary<string, WorldDiplomacyDocument> normalizedDocumentsById = BuildDocumentIndex(_storage.Documents);
		foreach (WorldDiplomacyRound round in _storage.CompletedRounds.Concat(_storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { _storage.ActiveRound }).Where(x => x != null))
		{
			round.ActionAttemptCountAtPassStart = Math.Max(0, Math.Min(round.DiplomaticActionAttemptCount, round.ActionAttemptCountAtPassStart));
			round.ConsecutiveNoActionPasses = Math.Max(0, Math.Min(3, round.ConsecutiveNoActionPasses));
			round.LastAccountedRelayPassNumber = Math.Max(0, round.LastAccountedRelayPassNumber);
			round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
			round.RelayRouteKingdomIds ??= new List<string>();
			round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
			round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
			round.ResultSettlementWarDocumentIds ??= new List<string>();
			round.ResultSettlementTriggerDocumentId ??= "";
			round.ResultSettlementCloseReason ??= "";
			round.ResultSettlementRoundStatus = string.Equals(round.ResultSettlementRoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
				? "deadlocked" : "resolved";
			round.ResultSettlementCurrentSlotId ??= "";
			round.ResultSettlementWarDocumentIds = round.ResultSettlementWarDocumentIds
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<WorldDiplomacyResultSettlementSlot> normalizedSettlementSlots = new List<WorldDiplomacyResultSettlementSlot>();
			string originalCurrentSettlementSlotId = round.ResultSettlementCurrentSlotId;
			string normalizedCurrentSettlementSlotId = "";
			foreach (IGrouping<string, WorldDiplomacyResultSettlementSlot> group in round.ResultSettlementSlots
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
				.GroupBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase))
			{
				List<WorldDiplomacyResultSettlementSlot> pending = group
					.Where(x => string.IsNullOrWhiteSpace(x.Status)
						|| string.Equals(x.Status, "pending", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.Status, "inflight", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.Status, "scheduled", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.Status, "waiting_player", StringComparison.OrdinalIgnoreCase))
					.ToList();
				if (pending.Count == 0) continue;
				WorldDiplomacyResultSettlementSlot slot = (!string.IsNullOrWhiteSpace(originalCurrentSettlementSlotId)
					? pending.FirstOrDefault(x => string.Equals(x.SlotId, originalCurrentSettlementSlotId, StringComparison.OrdinalIgnoreCase))
					: null)
					?? pending.FirstOrDefault(x => string.Equals(x.Status, "waiting_player", StringComparison.OrdinalIgnoreCase))
					?? pending.FirstOrDefault(x => string.Equals(x.Status, "inflight", StringComparison.OrdinalIgnoreCase))
					?? pending.FirstOrDefault(x => string.Equals(x.Status, "scheduled", StringComparison.OrdinalIgnoreCase))
					?? pending[0];
				slot.SlotId = string.IsNullOrWhiteSpace(slot.SlotId) ? "diplomacy_result_slot:" + group.Key : slot.SlotId;
				slot.Kind = string.Join("+", pending.SelectMany(x => (x.Kind ?? "").Split('+'))
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase));
				if (string.IsNullOrWhiteSpace(slot.Kind)) slot.Kind = "route";
				slot.Status = string.Equals(slot.Status, "waiting_player", StringComparison.OrdinalIgnoreCase)
					? "waiting_player"
					: string.Equals(slot.Status, "inflight", StringComparison.OrdinalIgnoreCase)
						? "inflight"
						: string.Equals(slot.Status, "scheduled", StringComparison.OrdinalIgnoreCase)
							? "scheduled" : "pending";
				slot.SourceDocumentIds = pending.SelectMany(x => x.SourceDocumentIds ?? new List<string>())
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				slot.RelatedKingdomIds = pending.SelectMany(x => x.RelatedKingdomIds ?? new List<string>())
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				if (!string.IsNullOrWhiteSpace(originalCurrentSettlementSlotId)
					&& pending.Any(x => string.Equals(x.SlotId, originalCurrentSettlementSlotId, StringComparison.OrdinalIgnoreCase)))
				{
					normalizedCurrentSettlementSlotId = slot.SlotId;
				}
				normalizedSettlementSlots.Add(slot);
			}
			round.ResultSettlementSlots = normalizedSettlementSlots;
			round.ResultSettlementCurrentSlotId = normalizedCurrentSettlementSlotId;
			if (string.IsNullOrWhiteSpace(normalizedCurrentSettlementSlotId)) round.ResultSettlementPlayerWaitingSinceDay = 0;
			round.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
			round.LlmTranscript.Clear();
			round.LlmProfiledKingdomIds ??= new List<string>();
			round.ExternalSignalKeys ??= new List<string>();
			round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
			round.PotentialActionIntents ??= new List<string>();
			round.CommonContractSnapshot = "";
			round.CommonContractSnapshotInitialized = false;
			round.CachePrefix = "";
			round.PotentialActionIntents = round.PotentialActionIntents
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(NormalizeIntent)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			round.ExternalSignalKeys = round.ExternalSignalKeys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			round.AttachedPolicySignals = round.AttachedPolicySignals
				.Where(x => x != null
					&& !string.IsNullOrWhiteSpace(x.SignalKey)
					&& !string.IsNullOrWhiteSpace(x.PolicyId)
					&& !string.IsNullOrWhiteSpace(x.IssuerKingdomId)
					&& !string.IsNullOrWhiteSpace(x.TargetKingdomId))
				.GroupBy(x => x.SignalKey.Trim(), StringComparer.OrdinalIgnoreCase)
				.Select(group => ClonePolicySignal(group.OrderByDescending(x => x.PublishedDay).First()))
				.OrderByDescending(x => x.PublishedDay)
				.ThenBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase)
				.Take(MaxPendingPolicySignals)
				.ToList();
			round.ExternalOpeningContext ??= "";
			round.EventSourceType ??= "";
			round.EventMotif ??= "";
			round.EventLocation ??= "";
			round.AllowedFiction ??= "";
			round.ForbiddenFiction ??= "";
			round.LlmProfiledKingdomIds.Clear();
			round.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			round.LlmLastStateSignatureByKingdom.Clear();
			round.PendingOffers.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.SourceDocumentId));
			int normalizedOfferSourceBindings = 0;
			foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers)
			{
				offer.SourceDocumentId = (offer.SourceDocumentId ?? "").Trim();
				offer.SourceActionId = (offer.SourceActionId ?? "").Trim();
				if (NormalizePendingOfferSourceActionBinding(offer, normalizedDocumentsById))
				{
					normalizedOfferSourceBindings++;
				}
			}
			if (normalizedOfferSourceBindings > 0)
			{
				Log("diplomacy offer source-action bindings normalized round=" + round.RoundId
					+ " count=" + normalizedOfferSourceBindings.ToString(CultureInfo.InvariantCulture));
			}
			if (allowWorldValidation
				&& ReferenceEquals(round, _storage.ActiveRound)
				&& _storage.DecisionArchitectureVersion >= DecisionArchitectureVersion) PruneInvalidOffers(round);
			if (round.DiplomaticActionAttemptCount <= 0)
			{
				round.DiplomaticActionAttemptCount = round.PendingOffers.Count(x => x != null
					&& !string.Equals(x.Status, "expired", StringComparison.OrdinalIgnoreCase));
				if (round.ExecutedActionCount > round.DiplomaticActionAttemptCount)
				{
					round.DiplomaticActionAttemptCount = round.ExecutedActionCount;
				}
			}
			int storedTargetDurationDays = round.SoftEndDay > round.StartedDay
				? round.SoftEndDay - round.StartedDay
				: RelayTargetDurationDays;
			if (round.SoftEndDay <= round.StartedDay) round.SoftEndDay = round.StartedDay + storedTargetDurationDays;
			if (round.RelayPassDurationDays <= 0) round.RelayPassDurationDays = GetCourtMaxDeliveryDays();
			if (round.HardEndDay <= 0) round.HardEndDay = Math.Max(round.SoftEndDay, round.StartedDay + GetRoundHardDurationDays(storedTargetDurationDays));
			if (string.Equals(round.State, "closed", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(round.FinalDocumentId))
			{
				round.FinalDocumentId = _storage.Documents.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
					.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).LastOrDefault()?.DocumentId ?? "";
			}
			foreach (WorldDiplomacyRoundParticipant participant in round.Participants.Where(x => x != null))
			{
				bool isWaitingSettlementPlayer = round.ResultSettlementPending
					&& (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>()).Any(x => x != null
						&& string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.KingdomId, participant.KingdomId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.Status, "waiting_player", StringComparison.OrdinalIgnoreCase));
				if (!isWaitingSettlementPlayer) participant.MandatoryReplyPending = false;
			}
			if (round.AutomaticDocumentsStarted <= 0)
			{
				round.AutomaticDocumentsStarted = _storage.Documents.Count(x => x != null && !x.IsPlayerAuthored && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
			}
			if (allowWorldValidation
				&& ReferenceEquals(round, _storage.ActiveRound)
				&& _storage.DecisionArchitectureVersion >= DecisionArchitectureVersion
				&& round.SchemaVersion < RelaySchemaVersion)
			{
				List<WorldDiplomacyJob> retiredRoundJobs = _storage.Jobs.Where(x => x != null
					&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
					&& (string.Equals(x.Kind, "generate", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.Kind, "round_plan", StringComparison.OrdinalIgnoreCase))).ToList();
				int retiredGenerateCount = retiredRoundJobs.Count(x => string.Equals(x.Kind, "generate", StringComparison.OrdinalIgnoreCase));
				HashSet<string> retiredRoundJobIds = new HashSet<string>(retiredRoundJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
				_storage.Jobs.RemoveAll(x => x != null && retiredRoundJobIds.Contains(x.JobId ?? ""));
				int publishedAutomatic = _storage.Documents.Count(x => x != null && !x.IsPlayerAuthored
					&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
				round.AutomaticDocumentsStarted = Math.Max(publishedAutomatic, round.AutomaticDocumentsStarted - retiredGenerateCount);
				_storage.RelayArrivals.RemoveAll(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase));
				round.CachePrefix = "";
				round.LlmTranscript.Clear();
				round.LlmProfiledKingdomIds.Clear();
				round.LlmLastStateSignatureByKingdom.Clear();
				round.RelayWaiting = false;
				round.SchemaVersion = RelaySchemaVersion;
			}
		}
		if (allowWorldValidation)
		{
			try
			{
				MigrateResultSettlementStateIfNeeded();
			}
			catch (Exception ex)
			{
				Log("round result-settlement migration deferred after error=" + ex.Message);
			}
			try
			{
				MigrateDiplomacyPromptContractIfNeeded();
			}
			catch (Exception ex)
			{
				Log("diplomacy prompt contract migration deferred after error=" + ex.Message);
			}
		}
		if (migrateLegacyPropagationState) _storage.PropagationReliabilityVersion = 1;
		foreach (WorldDiplomacySettlementKnowledge knowledge in _storage.SettlementKnowledge.Where(x => x != null)) knowledge.DocumentIds ??= new List<string>();
		foreach (WorldDiplomacyKingdomKnowledge knowledge in _storage.KingdomKnowledge.Where(x => x != null)) knowledge.DocumentIds ??= new List<string>();
		foreach (WorldDiplomacyKingdomKnowledge knowledge in _storage.NobleKnowledge.Where(x => x != null)) knowledge.DocumentIds ??= new List<string>();
		if (!_storage.CourtKnowledgeMigratedToNobles)
		{
			foreach (WorldDiplomacyKingdomKnowledge courtKnowledge in _storage.KingdomKnowledge.Where(x => x != null))
			{
				foreach (string documentId in courtKnowledge.DocumentIds ?? new List<string>()) RecordNobleKnowledge(courtKnowledge.KingdomId, documentId, courtKnowledge.LastUpdatedDay);
			}
			_storage.CourtKnowledgeMigratedToNobles = true;
		}
		foreach (WorldDiplomacyParticipationRequest request in _storage.PendingParticipationEvaluations.Where(x => x != null)) request.TriggerDocumentIds ??= new List<string>();
		_storage.PendingParticipationEvaluations.Clear();
		_storage.PendingSpeeches.Clear();
		_storage.RelayArrivals = _storage.RelayArrivals
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.RoundId) && !string.IsNullOrWhiteSpace(x.ToKingdomId))
			.OrderBy(x => x.DueDay).ThenBy(x => x.Sequence).ToList();
		foreach (WorldDiplomacyPlayerOpportunity opportunity in _storage.PlayerOpportunities.Where(x => x != null)) opportunity.KnownDocumentIds ??= new List<string>();
		_storage.PlayerOpportunities = _storage.PlayerOpportunities
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.RoundId))
			.OrderByDescending(x => x.ArrivedDay).Take(16).ToList();
		foreach (WorldDiplomacyRoundSummary summary in _storage.RoundSummaries.Where(x => x != null))
		{
			UpgradeRoundSummaryToStructuredArchive(summary);
			summary.SourceDocumentIds ??= new List<string>();
			summary.Facts ??= new List<WorldDiplomacyRoundFact>();
			summary.KingdomIds ??= new List<string>();
			foreach (WorldDiplomacyRoundFact fact in summary.Facts.Where(x => x != null))
			{
				fact.Kind = string.IsNullOrWhiteSpace(fact.Kind) ? "declaration" : fact.Kind;
				fact.SourceDocumentIds ??= new List<string>();
				fact.KingdomIds ??= new List<string>();
			}
			if (summary.KingdomIds.Count == 0)
			{
				summary.KingdomIds = _storage.Documents.Where(x => x != null && (summary.SourceDocumentIds ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase))
					.SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }.Concat(x.AddressedKingdomIds ?? new List<string>()))
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			}
		}
		foreach (WorldDiplomacyCompressionSummary summary in _storage.CompressionSummaries.Where(x => x != null))
		{
			summary.SourceRoundIds ??= new List<string>();
			summary.KingdomIds ??= new List<string>();
			summary.ConfirmedResults ??= new List<string>();
		}
		_storage.CompletedRounds = _storage.CompletedRounds.Where(x => x != null).OrderByDescending(x => x.CompletedDay).Take(64).ToList();
		_storage.RoundSummaries = _storage.RoundSummaries.Where(x => x != null).OrderByDescending(x => x.CreatedDay).Take(MaxStoredRoundSummaries).ToList();
		_storage.AnnualSummaries = _storage.AnnualSummaries
			.Where(x => x != null)
			.OrderByDescending(x => x.Year)
			.Take(MaxStoredAnnualSummaries)
			.ToList();
		_storage.CompressionSummaries = _storage.CompressionSummaries.Where(x => x != null && !string.IsNullOrWhiteSpace(x.BatchId))
			.OrderByDescending(x => x.CreatedDay).Take(MaxStoredCompressionSummaries).ToList();
		EnsureCanonicalHistoryInitialized();
		RecalculateCanonicalHistoryTokens();
		TrimNativeSignals();
		TrimRecentBattleFacts();
	}

	private void TrimRecentBattleFacts()
	{
		int cutoff = CurrentDay() - RecentBattleRetentionDays;
		_storage.RecentBattles ??= new List<WorldDiplomacyBattleFact>();
		_storage.RecentBattles = _storage.RecentBattles
			.Where(x => x != null && x.Day >= cutoff && !string.IsNullOrWhiteSpace(x.BattleId))
			.OrderByDescending(x => x.Day)
			.Take(MaxStoredRecentBattles)
			.ToList();
	}

	private void TrimNativeSignals()
	{
		int cutoff = CurrentDay() - DaysPerYear * 2;
		_storage.NativeSignals = _storage.NativeSignals
			.Where(x => x != null && x.Day >= cutoff)
			.OrderByDescending(x => x.Day)
			.Take(180)
			.ToList();
	}

	private void RemoveJob(string jobId)
	{
		_storage.Jobs.RemoveAll(x => x != null && string.Equals(x.JobId, jobId, StringComparison.OrdinalIgnoreCase));
	}

	private static Dictionary<string, WorldDiplomacyDocument> BuildDocumentIndex(
		IEnumerable<WorldDiplomacyDocument> documents)
	{
		Dictionary<string, WorldDiplomacyDocument> result = new Dictionary<string, WorldDiplomacyDocument>(StringComparer.OrdinalIgnoreCase);
		foreach (WorldDiplomacyDocument document in documents ?? Enumerable.Empty<WorldDiplomacyDocument>())
		{
			if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) continue;
			result[document.DocumentId.Trim()] = document;
		}
		return result;
	}

	private static bool NormalizePendingOfferSourceActionBinding(
		WorldDiplomacyRoundOffer offer,
		Dictionary<string, WorldDiplomacyDocument> documentsById)
	{
		if (offer == null || documentsById == null) return false;
		bool isOpen = string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase);
		if (!documentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source)
			|| source == null
			|| !source.IsReadyForPublication
			|| !string.Equals(source.AuthorKingdomId, offer.ProposerKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			if (!isOpen) return false;
			offer.Status = "invalidated";
			return true;
		}
		if (source.Actions == null || source.Actions.Count == 0)
		{
			if (string.IsNullOrWhiteSpace(offer.SourceActionId)) return false;
			offer.SourceActionId = "";
			return true;
		}
		List<WorldDiplomacyDocumentAction> matches = source.Actions
			.Where(x => x != null
				&& string.Equals(x.TargetKingdomId, offer.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), NormalizeIntent(offer.Intent), StringComparison.OrdinalIgnoreCase))
			.Take(2)
			.ToList();
		if (matches.Count == 1 && !string.IsNullOrWhiteSpace(matches[0].ActionId))
		{
			string normalizedActionId = matches[0].ActionId.Trim();
			if (string.Equals(offer.SourceActionId, normalizedActionId, StringComparison.OrdinalIgnoreCase)) return false;
			offer.SourceActionId = normalizedActionId;
			return true;
		}
		if (!isOpen) return false;
		offer.Status = "invalidated";
		return true;
	}

	private WorldDiplomacyDocument ResolveDocument(string documentId)
	{
		if (string.IsNullOrWhiteSpace(documentId))
		{
			return null;
		}
		return _storage.Documents.FirstOrDefault(x => x != null && string.Equals(x.DocumentId, documentId, StringComparison.OrdinalIgnoreCase));
	}

	private static WorldDiplomacyDocumentAction ResolveDocumentAction(
		WorldDiplomacyDocument document,
		string actionId)
	{
		if (document?.Actions == null || document.Actions.Count == 0 || string.IsNullOrWhiteSpace(actionId)) return null;
		return document.Actions.FirstOrDefault(x => x != null
			&& string.Equals(x.ActionId, actionId, StringComparison.OrdinalIgnoreCase));
	}

	private static WorldDiplomacyPeaceTerms ResolveOfferedPeaceTerms(
		WorldDiplomacyDocument source,
		string sourceActionId)
	{
		if (source == null) return null;
		if (source.Actions?.Count > 0)
		{
			return ResolveDocumentAction(source, sourceActionId)?.PeaceTerms;
		}
		return source.PeaceTerms;
	}

	private static bool AreOfferedPeaceTermsCurrentlyExecutable(
		WorldDiplomacyRoundOffer offer,
		WorldDiplomacyDocument source,
		Kingdom proposer,
		Kingdom target)
	{
		if (offer == null || source == null || proposer == null || target == null
			|| proposer == target || !source.IsReadyForPublication
			|| !string.Equals(source.AuthorKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyPeaceTerms terms = ResolveOfferedPeaceTerms(source, offer.SourceActionId);
		if (source.Actions?.Count > 0 && ResolveDocumentAction(source, offer.SourceActionId) == null) return false;
		if (terms == null) return true;

		int promisedTribute = Math.Max(0, terms.DailyTribute);
		int promisedDuration = Math.Max(0, terms.DurationDays);
		if (promisedTribute > 0)
		{
			Kingdom payer = ResolveKingdom(terms.TributePayerKingdomId);
			Kingdom receiver = ResolveKingdom(terms.TributeReceiverKingdomId);
			if (payer == null || receiver == null || payer == receiver
				|| (payer != proposer && payer != target)
				|| (receiver != proposer && receiver != target)
				|| DiplomacyPeaceTermsService.ClampTributeAmount(payer, promisedTribute) != promisedTribute
				|| DiplomacyPeaceTermsService.ResolveDurationDays(
					promisedDuration.ToString(CultureInfo.InvariantCulture),
					hasTribute: true) != promisedDuration) return false;
		}
		else if (promisedDuration != 0)
		{
			return false;
		}

		bool hasAnyCession = !string.IsNullOrWhiteSpace(terms.CessionFromKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionSettlementId);
		if (!hasAnyCession) return true;
		Kingdom from = ResolveKingdom(terms.CessionFromKingdomId);
		Kingdom to = ResolveKingdom(terms.CessionToKingdomId);
		Settlement settlement = ResolveSettlementById(terms.CessionSettlementId);
		return from != null && to != null && settlement != null && from != to
			&& (from == proposer || from == target)
			&& (to == proposer || to == target)
			&& settlement.OwnerClan?.Kingdom == from
			&& to.RulingClan?.Leader != null;
	}

	private WorldDiplomacyExchange ResolveExchange(string exchangeId)
	{
		if (string.IsNullOrWhiteSpace(exchangeId))
		{
			return null;
		}
		if (_storage.ActiveExchange != null && string.Equals(_storage.ActiveExchange.ExchangeId, exchangeId, StringComparison.OrdinalIgnoreCase))
		{
			return _storage.ActiveExchange;
		}
		return _storage.SuspendedExchanges.FirstOrDefault(x => x != null && string.Equals(x.ExchangeId, exchangeId, StringComparison.OrdinalIgnoreCase));
	}

	private int GetWarPressure(string sourceId, string targetId)
	{
		return _storage.WarPressure.FirstOrDefault(x => x != null
			&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))?.Value ?? 0;
	}

	private string BuildRecentNativeSignalContext(string sourceId, string targetId)
	{
		return string.Join("\n", _storage.NativeSignals
			.Where(x => x != null
				&& string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.Day)
			.Take(4)
			.Select(x => "- 第" + x.Day.ToString(CultureInfo.InvariantCulture) + "天：" + x.Reason));
	}

	private string BuildRecentBilateralDocumentContext(string sourceId, string targetId, int maxCount)
	{
		string activeRoundId = _storage.ActiveRound?.RoundId ?? "";
		if (string.IsNullOrWhiteSpace(activeRoundId)) return "";
		WorldDiplomacyKingdomKnowledge knowledge = _storage.KingdomKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, sourceId, StringComparison.OrdinalIgnoreCase));
		HashSet<string> knownIds = new HashSet<string>(knowledge?.DocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		return string.Join("\n", _storage.Documents
			.Where(x => x != null
				&& !x.IsCompressed
				&& string.Equals(x.RoundId, activeRoundId, StringComparison.OrdinalIgnoreCase)
				&& knownIds.Contains(x.DocumentId ?? "")
				&& ((string.Equals(x.AuthorKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase))
					|| (string.Equals(x.AuthorKingdomId, targetId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(x.TargetKingdomId, sourceId, StringComparison.OrdinalIgnoreCase))))
			.OrderByDescending(x => x.Day)
			.Take(maxCount)
			.Select(x => "- " + BuildCompactDocumentMemoryLine(x)));
	}

	private string BuildRelevantCompletedRoundContext(string sourceId, string targetId)
	{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId)) return "";
		WorldDiplomacyRoundSummary summary = (_storage.RoundSummaries ?? new List<WorldDiplomacyRoundSummary>())
			.Where(x => x != null && !x.IsTokenCompressed
				&& (x.KingdomIds ?? new List<string>()).Contains(sourceId, StringComparer.OrdinalIgnoreCase)
				&& (x.KingdomIds ?? new List<string>()).Contains(targetId, StringComparer.OrdinalIgnoreCase)
				&& !string.Equals(x.RoundId, _storage.ActiveRound?.RoundId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.CreatedDay)
			.FirstOrDefault();
		return summary == null ? "" : "- [已结束] " + Limit(summary.Summary, 700);
	}

	private string BuildRelevantCompressedDiplomacyContext(string sourceId, string targetId, int maxCount)
	{
		if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || maxCount <= 0) return "";
		return string.Join("\n", (_storage.RoundSummaries ?? new List<WorldDiplomacyRoundSummary>())
			.Where(x => x != null && x.IsTokenCompressed
				&& (x.KingdomIds ?? new List<string>()).Contains(sourceId, StringComparer.OrdinalIgnoreCase)
				&& (x.KingdomIds ?? new List<string>()).Contains(targetId, StringComparer.OrdinalIgnoreCase))
			.OrderByDescending(x => x.CreatedDay).Take(maxCount)
			.Select(x => "- [已整理回合；宣言经过与游戏结果分列] " + Limit(x.Summary, 900)));
	}

	private string BuildKnownRoundContext(string kingdomId, string roundId, int maxCount)
	{
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(roundId)) return "";
		WorldDiplomacyKingdomKnowledge knowledge = _storage.KingdomKnowledge.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		HashSet<string> known = new HashSet<string>(knowledge?.DocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> recentKnown = _storage.Documents
			.Where(x => x != null && known.Contains(x.DocumentId ?? "") && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks)
			.Take(Math.Max(1, maxCount))
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks)
			.ToList();
		return string.Join("\n", recentKnown
			.Select(x => "- " + BuildCompactDocumentMemoryLine(x) + (string.IsNullOrWhiteSpace(x.Body) ? "" : "：" + Limit(x.Body, 420))));
	}

	private static string BuildCompactDocumentMemoryLine(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return "";
		}
		string target = document.Actions?.Count > 1
			? "对" + string.Join("、", document.Actions.Where(x => x != null)
				.Select(x => FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId) + "=" + IntentLabel(x.Intent)))
			: (string.IsNullOrWhiteSpace(document.TargetKingdomName) ? "" : "致" + document.TargetKingdomName);
		string result = string.IsNullOrWhiteSpace(document.MechanicalResult) ? "" : "；结果：" + document.MechanicalResult;
		string date = string.IsNullOrWhiteSpace(document.GameDate) ? FormatCampaignDate(document.Day) : document.GameDate;
		return date
			+ "，"
			+ document.AuthorKingdomName
			+ target
			+ "发布《"
			+ document.Title
			+ "》（"
			+ (document.Actions?.Count > 1 ? "复合外交动作" : IntentLabel(document.Intent))
			+ "）"
			+ result;
	}

	private static string FormatRoundFactForPrompt(WorldDiplomacyRoundFact fact)
	{
		if (fact == null || string.IsNullOrWhiteSpace(fact.Text)) return "";
		string text = fact.Text.Trim();
		if (text.StartsWith("[", StringComparison.Ordinal)) return text;
		return string.Equals(fact.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase)
			? "[游戏已执行] " + text
			: "[宣言记录，不代表执行] " + text;
	}

	private static bool IsMajorDiplomaticDocument(WorldDiplomacyDocument document)
	{
		string intent = NormalizeIntent(document?.Intent);
		return intent == "declare_war"
			|| intent == "accept_peace"
			|| intent == "propose_peace"
			|| intent == "accept_alliance"
			|| intent == "propose_alliance"
			|| intent == "break_alliance"
			|| intent == "accept_trade"
			|| intent == "propose_trade"
			|| intent == "cancel_trade"
			|| intent == "ultimatum"
			|| !string.IsNullOrWhiteSpace(document?.MechanicalResult);
	}

	private void EnsureActiveWarLedgersAndRemoveEndedWars()
	{
		_storage.ActiveWarLedgers.RemoveAll(x => x == null
			|| !AreKingdomsAtWar(x.FirstKingdomId, x.SecondKingdomId));
		List<Kingdom> kingdoms = Kingdom.All
			.Where(x => x != null && !x.IsEliminated)
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		for (int i = 0; i < kingdoms.Count; i++)
		{
			for (int j = i + 1; j < kingdoms.Count; j++)
			{
				if (FactionManager.IsAtWarAgainstFaction(kingdoms[i], kingdoms[j]))
				{
					EnsureWarLedger(kingdoms[i], kingdoms[j]);
				}
			}
		}
	}

	private WorldDiplomacyWarLedger EnsureWarLedger(Kingdom first, Kingdom second)
	{
		if (first == null || second == null || first == second)
		{
			return null;
		}
		WorldDiplomacyWarLedger existing = ResolveWarLedger(first.StringId, second.StringId);
		if (existing != null)
		{
			return existing;
		}
		string firstId = string.Compare(first.StringId, second.StringId, StringComparison.OrdinalIgnoreCase) <= 0 ? first.StringId : second.StringId;
		string secondId = string.Equals(firstId, first.StringId, StringComparison.OrdinalIgnoreCase) ? second.StringId : first.StringId;
		WorldDiplomacyWarLedger ledger = new WorldDiplomacyWarLedger
		{
			PairKey = PairKey(firstId, secondId),
			FirstKingdomId = firstId,
			SecondKingdomId = secondId,
			StartedDay = CurrentDay()
		};
		_storage.ActiveWarLedgers.Add(ledger);
		return ledger;
	}

	private WorldDiplomacyWarLedger ResolveWarLedger(string firstId, string secondId)
	{
		string key = PairKey(firstId, secondId);
		return _storage.ActiveWarLedgers.FirstOrDefault(x => x != null
			&& string.Equals(x.PairKey, key, StringComparison.OrdinalIgnoreCase));
	}

	private void RemoveWarLedger(string firstId, string secondId)
	{
		string key = PairKey(firstId, secondId);
		_storage.ActiveWarLedgers.RemoveAll(x => x != null
			&& string.Equals(x.PairKey, key, StringComparison.OrdinalIgnoreCase));
	}

	private static bool AreKingdomsAtWar(string firstId, string secondId)
	{
		Kingdom first = ResolveKingdom(firstId);
		Kingdom second = ResolveKingdom(secondId);
		return first != null && second != null && FactionManager.IsAtWarAgainstFaction(first, second);
	}

	private void InvalidateWarSituation(Kingdom first, Kingdom second)
	{
		if (first == null || second == null)
		{
			return;
		}
		string prefix1 = first.StringId + ">" + second.StringId + ":";
		string prefix2 = second.StringId + ">" + first.StringId + ":";
		foreach (string key in _warSituationCache.Keys.Where(x => x.StartsWith(prefix1, StringComparison.OrdinalIgnoreCase)
			|| x.StartsWith(prefix2, StringComparison.OrdinalIgnoreCase)).ToList())
		{
			_warSituationCache.Remove(key);
		}
	}

	private WarSituationSnapshot GetWarSituation(Kingdom author, Kingdom target)
	{
		WarSituationSnapshot empty = new WarSituationSnapshot();
		if (author == null || target == null)
		{
			return empty;
		}
		int day = CurrentDay();
		string key = author.StringId + ">" + target.StringId + ":" + day.ToString(CultureInfo.InvariantCulture);
		if (_warSituationCache.TryGetValue(key, out WarSituationSnapshot cached))
		{
			return cached;
		}
		WarSituationSnapshot snapshot = BuildWarSituation(author, target, day);
		_warSituationCache[key] = snapshot;
		return snapshot;
	}

	private WarSituationSnapshot BuildWarSituation(Kingdom author, Kingdom target, int day)
	{
		WarSituationSnapshot snapshot = new WarSituationSnapshot
		{
			Day = day,
			IsAtWar = FactionManager.IsAtWarAgainstFaction(author, target),
			AuthorStrength = Math.Max(1f, author.CurrentTotalStrength),
			TargetStrength = Math.Max(1f, target.CurrentTotalStrength)
		};
		if (!snapshot.IsAtWar)
		{
			return snapshot;
		}
		try
		{
			StanceLink stance = author.GetStanceWith(target);
			var model = Campaign.Current?.Models?.DiplomacyModel;
			snapshot.WarDays = Math.Max(0, (int)stance.WarStartDate.ElapsedDaysUntilNow);
			snapshot.AuthorProgress = model?.GetWarProgressScore(author, target).ResultNumber ?? 0f;
			snapshot.TargetProgress = model?.GetWarProgressScore(target, author).ResultNumber ?? 0f;
			snapshot.AuthorInflictedCasualties = Math.Max(0, stance.GetCasualties(target));
			snapshot.AuthorSufferedCasualties = Math.Max(0, stance.GetCasualties(author));
			snapshot.AuthorSuccessfulSieges = Math.Max(0, stance.GetSuccessfulSieges(author));
			snapshot.TargetSuccessfulSieges = Math.Max(0, stance.GetSuccessfulSieges(target));
			snapshot.AuthorOtherWars = CountOtherWars(author, target);
			snapshot.TargetOtherWars = CountOtherWars(target, author);
			snapshot.AuthorPeacePressure = CalculatePeacePressure(snapshot, author, target, authorPerspective: true);
			snapshot.TargetPeacePressure = CalculatePeacePressure(snapshot, author, target, authorPerspective: false);
			snapshot.AuthorCessionScore = CalculateCessionScore(author, target, snapshot, authorPerspective: true);
			snapshot.TargetCessionScore = CalculateCessionScore(target, author, snapshot, authorPerspective: false);
			if (DiplomacyBehavior.TryBuildTributePowerContext(author, target, out AfTributePowerContext authorPays))
			{
				snapshot.AuthorSuggestedTribute = Math.Max(0, authorPays.CalculatedTribute);
			}
			if (DiplomacyBehavior.TryBuildTributePowerContext(target, author, out AfTributePowerContext targetPays))
			{
				snapshot.TargetSuggestedTribute = Math.Max(0, targetPays.CalculatedTribute);
			}
		}
		catch (Exception ex)
		{
			Log("war snapshot failed pair=" + author.StringId + "/" + target.StringId + " error=" + ex.Message);
		}
		return snapshot;
	}

	private static int CountOtherWars(Kingdom kingdom, Kingdom excluded)
	{
		return Kingdom.All.Count(x => x != null
			&& !x.IsEliminated
			&& x != kingdom
			&& x != excluded
			&& FactionManager.IsAtWarAgainstFaction(kingdom, x));
	}

	private float CalculatePeacePressure(WarSituationSnapshot snapshot, Kingdom author, Kingdom target, bool authorPerspective)
	{
		float ownProgress = authorPerspective ? snapshot.AuthorProgress : snapshot.TargetProgress;
		float enemyProgress = authorPerspective ? snapshot.TargetProgress : snapshot.AuthorProgress;
		float ownStrength = authorPerspective ? snapshot.AuthorStrength : snapshot.TargetStrength;
		float enemyStrength = authorPerspective ? snapshot.TargetStrength : snapshot.AuthorStrength;
		int suffered = authorPerspective ? snapshot.AuthorSufferedCasualties : snapshot.AuthorInflictedCasualties;
		int inflicted = authorPerspective ? snapshot.AuthorInflictedCasualties : snapshot.AuthorSufferedCasualties;
		int otherWars = authorPerspective ? snapshot.AuthorOtherWars : snapshot.TargetOtherWars;
		Kingdom ownKingdom = authorPerspective ? author : target;
		Kingdom enemyKingdom = authorPerspective ? target : author;
		int lostFiefs = GetUnrecoveredLostSettlements(ownKingdom, enemyKingdom).Count;
		float duration = Clamp01((snapshot.WarDays - 7f) / 112f) * 70f;
		float setback = Clamp01((enemyProgress - ownProgress) / 500f) * 70f;
		float strength = Clamp01((enemyStrength / Math.Max(1f, ownStrength) - 1f) / 1.5f) * 40f;
		float casualtyBurden = Clamp01(suffered / Math.Max(500f, ownStrength * 1.5f)) * 40f;
		float casualtyImbalance = Clamp01((suffered - inflicted) / Math.Max(500f, ownStrength)) * 20f;
		float multiWar = Clamp01(otherWars / 2f) * 30f;
		float territory = Clamp01(lostFiefs / 2f) * 30f;
		return Math.Max(0f, Math.Min(300f, duration + setback + strength + casualtyBurden + casualtyImbalance + multiWar + territory));
	}

	private float CalculateCessionScore(Kingdom loser, Kingdom winner, WarSituationSnapshot snapshot, bool authorPerspective)
	{
		float ownProgress = authorPerspective ? snapshot.AuthorProgress : snapshot.TargetProgress;
		float enemyProgress = authorPerspective ? snapshot.TargetProgress : snapshot.AuthorProgress;
		float ownStrength = authorPerspective ? snapshot.AuthorStrength : snapshot.TargetStrength;
		float enemyStrength = authorPerspective ? snapshot.TargetStrength : snapshot.AuthorStrength;
		int suffered = authorPerspective ? snapshot.AuthorSufferedCasualties : snapshot.AuthorInflictedCasualties;
		int inflicted = authorPerspective ? snapshot.AuthorInflictedCasualties : snapshot.AuthorSufferedCasualties;
		int otherWars = authorPerspective ? snapshot.AuthorOtherWars : snapshot.TargetOtherWars;
		int lostFiefs = GetUnrecoveredLostSettlements(loser, winner).Count;
		float progress = Clamp01((enemyProgress - ownProgress) / 500f) * 40f;
		float strength = Clamp01((enemyStrength / Math.Max(1f, ownStrength) - 1f) / 2f) * 20f;
		float territory = Clamp01(lostFiefs / 2f) * 20f;
		float casualties = Clamp01((suffered - inflicted) / Math.Max(500f, ownStrength)) * 10f;
		float multiWar = Clamp01(otherWars / 2f) * 10f;
		return Math.Max(0f, Math.Min(100f, progress + strength + territory + casualties + multiWar));
	}

	private static float Clamp01(float value)
	{
		return Math.Max(0f, Math.Min(1f, value));
	}

	private List<Settlement> GetUnrecoveredLostSettlements(Kingdom originalOwner, Kingdom currentOwner)
	{
		WorldDiplomacyWarLedger ledger = ResolveWarLedger(originalOwner?.StringId, currentOwner?.StringId);
		if (ledger == null)
		{
			return new List<Settlement>();
		}
		return ledger.SettlementChanges
			.Where(x => x != null
				&& string.Equals(x.OriginalKingdomId, originalOwner.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.CurrentKingdomId, currentOwner.StringId, StringComparison.OrdinalIgnoreCase))
			.Select(x => ResolveSettlementById(x.SettlementId))
			.Where(x => x != null && x.OwnerClan?.Kingdom == currentOwner)
			.Distinct()
			.ToList();
	}

	private List<Settlement> BuildCessionCandidates(Kingdom cedingKingdom, Kingdom receivingKingdom, float cessionScore)
	{
		if (cedingKingdom == null || receivingKingdom == null || cessionScore < CessionCastleUnlockThreshold)
		{
			return new List<Settlement>();
		}
		List<Settlement> priority = GetUnrecoveredLostSettlements(receivingKingdom, cedingKingdom);
		IEnumerable<Settlement> owned = cedingKingdom.Fiefs
			.Select(x => x?.Settlement)
			.Where(x => x != null && (x.IsCastle || x.IsTown));
		return priority
			.Concat(owned.Where(x => x.Culture == receivingKingdom.Culture))
			.Concat(owned)
			.Where(x => x != null
				&& x.OwnerClan?.Kingdom == cedingKingdom
				&& !x.IsUnderSiege
				&& (!x.IsTown || cessionScore >= CessionTownUnlockThreshold)
				&& cedingKingdom.Fiefs.Count() > 1)
			.Distinct()
			.Take(MaxPeaceCessionCandidates)
			.ToList();
	}

	private static Settlement ResolveSettlementById(string settlementId)
	{
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return null;
		}
		return Settlement.All.FirstOrDefault(x => x != null
			&& string.Equals(x.StringId, settlementId, StringComparison.OrdinalIgnoreCase));
	}

	private static Settlement ResolveMentionedSettlement(string tokenOrText, IEnumerable<Settlement> allowed)
	{
		string text = (tokenOrText ?? "").Trim();
		List<Settlement> candidates = (allowed ?? Enumerable.Empty<Settlement>()).Where(x => x != null).Distinct().ToList();
		if (string.IsNullOrWhiteSpace(text) || candidates.Count == 0)
		{
			return null;
		}
		Settlement exact = candidates.FirstOrDefault(x => string.Equals(x.StringId, text, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(x.Name?.ToString(), text, StringComparison.OrdinalIgnoreCase));
		if (exact != null)
		{
			return exact;
		}
		exact = candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name?.ToString())
			&& text.IndexOf(x.Name.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);
		if (exact != null)
		{
			return exact;
		}
		return candidates
			.Select(x => new
			{
				Settlement = x,
				Score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(text, new[] { x.Name?.ToString() ?? "", x.StringId ?? "" })
			})
			.Where(x => x.Score >= 0.72f)
			.OrderByDescending(x => x.Score)
			.Select(x => x.Settlement)
			.FirstOrDefault();
	}

	private static string BuildBilateralState(Kingdom author, Kingdom target)
	{
		if (author == null || target == null)
		{
			return "未知";
		}
		if (FactionManager.IsAtWarAgainstFaction(author, target))
		{
			return "双方正在交战";
		}
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliance != null && alliance.IsAllyWithKingdom(author, target))
		{
			return "双方处于同盟关系";
		}
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		if (trade != null && BannerlordApiCompat.HasTradeAgreement(trade, author, target))
		{
			return "双方和平并有贸易协定";
		}
		return "双方处于和平状态";
	}

	private static int GetRulerRelation(Kingdom source, Kingdom target)
	{
		try
		{
			Hero sourceRuler = source?.RulingClan?.Leader;
			Hero targetRuler = target?.RulingClan?.Leader;
			return sourceRuler == null || targetRuler == null ? 0 : sourceRuler.GetRelation(targetRuler);
		}
		catch
		{
			return 0;
		}
	}

	private static int CountCulturalClaims(Kingdom source, Kingdom target)
	{
		try
		{
			if (source?.Culture == null || target == null)
			{
				return 0;
			}
			return target.Fiefs.Count(x => x != null && x.Culture == source.Culture);
		}
		catch
		{
			return 0;
		}
	}

	private static void GetDiplomaticDeclarationCharacterRange(out int minimumCharacters, out int maximumCharacters)
	{
		minimumCharacters = DuelSettings.DefaultWorldDiplomacyDeclarationMinCharacters;
		int configuredMaximumCharacters = DuelSettings.DefaultWorldDiplomacyDeclarationMaxCharacters;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			minimumCharacters = Math.Max(
				DuelSettings.WorldDiplomacyDeclarationCharactersMin,
				Math.Min(
					DuelSettings.WorldDiplomacyDeclarationCharactersMax,
					settings?.WorldDiplomacyDeclarationMinCharacters
					?? DuelSettings.DefaultWorldDiplomacyDeclarationMinCharacters));
			configuredMaximumCharacters = Math.Max(
				DuelSettings.WorldDiplomacyDeclarationCharactersMin,
				Math.Min(
					DuelSettings.WorldDiplomacyDeclarationCharactersMax,
					settings?.WorldDiplomacyDeclarationMaxCharacters
					?? DuelSettings.DefaultWorldDiplomacyDeclarationMaxCharacters));
		}
		catch
		{
			// Keep the declared defaults when MCM settings are temporarily unavailable.
		}
		maximumCharacters = Math.Max(minimumCharacters, configuredMaximumCharacters);
	}

	private static bool IsWorldDiplomacyEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableWorldDiplomacy ?? false;
		}
		catch
		{
			return true;
		}
	}

	private static bool AreMapNotificationsEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableWorldDiplomacyMapNotifications ?? true;
		}
		catch
		{
			return true;
		}
	}

	private static int GetRoundIntervalDays()
	{
		try
		{
			return Math.Max(1, Math.Min(14, DuelSettings.GetSettings()?.WorldDiplomacyRoundIntervalDays ?? 3));
		}
		catch
		{
			return 3;
		}
	}

	private static int GetActivityLevel()
	{
		try
		{
			int index = DuelSettings.GetSettings()?.WorldDiplomacyActivityDropdown?.SelectedIndex ?? 1;
			return Math.Max(0, Math.Min(2, index));
		}
		catch
		{
			return 1;
		}
	}

	private static int GetRoundParticipantLimit()
	{
		return Math.Min(MaxRelayParticipants, GetActivityLevel() switch
		{
			0 => 2,
			2 => 5,
			_ => 3
		});
	}

	private static int GetCourtMaxDeliveryDays()
	{
		try
		{
			return Math.Max(3, Math.Min(14, DuelSettings.GetSettings()?.WorldDiplomacyCourtMaxDeliveryDays ?? 7));
		}
		catch
		{
			return 7;
		}
	}

	private static int GetCivilianSpreadDays()
	{
		try
		{
			return Math.Max(7, Math.Min(42, DuelSettings.GetSettings()?.WorldDiplomacyContinentSpreadDays ?? 21));
		}
		catch
		{
			return 21;
		}
	}

	private static int GetRoundLengthDays()
	{
		try
		{
			int index = DuelSettings.GetSettings()?.WorldDiplomacyRoundLengthDropdown?.SelectedIndex ?? 1;
			return index <= 0 ? 15 : index >= 2 ? 28 : 21;
		}
		catch
		{
			return RelayTargetDurationDays;
		}
	}

	private static int GetRoundHardDurationDays(int targetDurationDays)
	{
		if (targetDurationDays <= 15) return 18;
		if (targetDurationDays >= 28) return 32;
		return RelayHardDurationDays;
	}

	private static int GetOffensiveWarCooldownDays()
	{
		try
		{
			return Math.Max(7, Math.Min(120, DuelSettings.GetSettings()?.WorldDiplomacyOffensiveWarCooldownDays ?? 42));
		}
		catch
		{
			return 42;
		}
	}

	private static int GetPeaceProtectionDays()
	{
		try
		{
			return Math.Max(0, Math.Min(60, DuelSettings.GetSettings()?.WorldDiplomacyPeaceProtectionDays ?? 21));
		}
		catch
		{
			return 21;
		}
	}

	private static int GetTradeAllianceFailedProposalCooldownDays()
	{
		try
		{
			return Math.Max(0, Math.Min(DaysPerYear * 8,
				DuelSettings.GetSettings()?.WorldDiplomacyTradeAllianceFailedProposalCooldownDays ?? DaysPerYear * 2));
		}
		catch
		{
			return DaysPerYear * 2;
		}
	}

	private void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat)
	{
		if (threat == null || threat.IssuerRewardHistoryRecorded || !threat.IssuerRewardCompleted
			|| !string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyDocument compliance = ResolveDocument(threat.ComplianceDocumentId);
		if (compliance?.HistoryDeclarationRecorded != true) return;
		int rewardAmount = Math.Max(0, threat.IssuerRewardAmount);
		int appliedCount = (threat.IssuerRewardAppliedClanIds ?? new List<string>()).Count(x => !string.IsNullOrWhiteSpace(x));
		if (rewardAmount <= 0 || appliedCount <= 0)
		{
			threat.IssuerRewardHistoryRecorded = true;
			return;
		}
		try
		{
			int skippedCount = (threat.IssuerRewardSkippedClanIds ?? new List<string>()).Count(x => !string.IsNullOrWhiteSpace(x));
			Kingdom issuer = ResolveKingdomIncludingEliminated(threat.IssuerKingdomId);
			string sourceKey = "threat:" + threat.ThreatId + ":issuer_relation_reward";
			string result = "经游戏机制确认：" + KingdomName(issuer) + "迫使对方明确退让后，已按每个正式封臣家族与退让时王族关系增加"
				+ rewardAmount.ToString(CultureInfo.InvariantCulture) + "点（最高为100）的规则完成国内关系奖励；已结算"
				+ appliedCount.ToString(CultureInfo.InvariantCulture) + "个家族"
				+ (skippedCount > 0 ? "，另有" + skippedCount.ToString(CultureInfo.InvariantCulture) + "个已无有效关系对象的家族未执行" : "") + "。";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", sourceKey,
				FirstNonEmpty(threat.ComplianceDocumentId, threat.ThreatId), threat.UpdatedDay,
				FormatCampaignDate(threat.UpdatedDay), threat.IssuerKingdomId, new[] { threat.TargetKingdomId },
				"comply_ultimatum", "binding", result, verified: true,
				respondingToThreatDocumentId: threat.StageDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(sourceKey)
				|| (_storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
					.Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
			{
				threat.IssuerRewardHistoryRecorded = true;
			}
		}
		catch (Exception ex)
		{
			Log("issuer relation reward history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
		}
	}

	private static int GetThreatComplianceIssuerRelationReward()
	{
		try
		{
			return Math.Max(
				DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMin,
				Math.Min(
					DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax,
					DuelSettings.GetSettings()?.WorldDiplomacyThreatComplianceIssuerRelationReward
						?? DuelSettings.DefaultWorldDiplomacyThreatComplianceIssuerRelationReward));
		}
		catch
		{
			return DuelSettings.DefaultWorldDiplomacyThreatComplianceIssuerRelationReward;
		}
	}

	private static int GetHistoryCompressionTargetTokens()
	{
		try
		{
			int thousands = Math.Max(DuelSettings.WorldDiplomacyHistoryCompressionTargetThousandsMin,
				Math.Min(DuelSettings.WorldDiplomacyHistoryCompressionTargetThousandsMax,
					DuelSettings.GetSettings()?.WorldDiplomacyHistoryCompressionTargetThousands
					?? DuelSettings.DefaultWorldDiplomacyHistoryCompressionTargetThousands));
			return thousands * 1000;
		}
		catch
		{
			return DuelSettings.DefaultWorldDiplomacyHistoryCompressionTargetThousands * 1000;
		}
	}

	private static long GetHistoryCompressionTriggerTokens()
	{
		try
		{
			int thousands = Math.Max(DuelSettings.WorldDiplomacyHistoryCompressionTriggerThousandsMin,
				Math.Min(DuelSettings.WorldDiplomacyHistoryCompressionTriggerThousandsMax,
					DuelSettings.GetSettings()?.WorldDiplomacyHistoryCompressionTriggerThousands
					?? DuelSettings.DefaultWorldDiplomacyHistoryCompressionTriggerThousands));
			return thousands * 1000L;
		}
		catch
		{
			return DuelSettings.DefaultWorldDiplomacyHistoryCompressionTriggerThousands * 1000L;
		}
	}

	private static WorldDiplomacyBehavior ResolveInstance()
	{
		return Instance ?? Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
	}

	private static Kingdom ResolveKingdom(string id)
	{
		if (string.IsNullOrWhiteSpace(id) || Campaign.Current == null)
		{
			return null;
		}
		return Kingdom.All.FirstOrDefault(x => x != null
			&& !x.IsEliminated
			&& (string.Equals(x.StringId, id.Trim(), StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Name?.ToString(), id.Trim(), StringComparison.OrdinalIgnoreCase)));
	}

	private static Kingdom ResolveKingdomIncludingEliminated(string id)
	{
		if (string.IsNullOrWhiteSpace(id) || Campaign.Current == null) return null;
		string normalizedId = id.Trim();
		return Kingdom.All.FirstOrDefault(x => x != null
			&& (string.Equals(x.StringId, normalizedId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Name?.ToString(), normalizedId, StringComparison.OrdinalIgnoreCase)));
	}

	private static bool IsPlayerKingdom(Kingdom kingdom)
	{
		return kingdom != null && kingdom == Clan.PlayerClan?.Kingdom && kingdom.RulingClan?.Leader == Hero.MainHero;
	}

	private static bool IsPlayerAffiliatedKingdom(Kingdom kingdom)
	{
		return kingdom != null && kingdom == Clan.PlayerClan?.Kingdom;
	}

	private static bool CanAiAuthorDiplomaticDocument(Kingdom kingdom, out string reason)
	{
		reason = "";
		if (kingdom == null || kingdom.IsEliminated)
		{
			reason = "author_kingdom_missing";
			return false;
		}
		if (IsPlayerKingdom(kingdom))
		{
			reason = "player_controlled_realm_requires_player_authorization";
			return false;
		}
		Hero ruler = kingdom.RulingClan?.Leader;
		if (ruler == null || !ruler.IsAlive)
		{
			reason = "ruler_unavailable";
			return false;
		}
		if (ruler.IsPrisoner)
		{
			reason = "ruler_is_prisoner";
			return false;
		}
		return true;
	}

	private static int CurrentDay()
	{
		try
		{
			return Math.Max(0, (int)CampaignTime.Now.ToDays);
		}
		catch
		{
			return 0;
		}
	}

	private static int CurrentHour()
	{
		try
		{
			return Math.Max(0, (int)CampaignTime.Now.ToHours);
		}
		catch
		{
			return CurrentDay() * 24;
		}
	}

	private static string FormatCampaignDate(int day)
	{
		try
		{
			int safeDay = Math.Max(0, day);
			int daysInSeason = CampaignTime.DaysInSeason > 0 ? CampaignTime.DaysInSeason : 21;
			int daysInYear = CampaignTime.DaysInYear > 0 ? CampaignTime.DaysInYear : daysInSeason * 4;
			int year = safeDay / Math.Max(1, daysInYear);
			int dayOfYear = safeDay % Math.Max(1, daysInYear);
			int season = dayOfYear / Math.Max(1, daysInSeason);
			int dayOfSeason = dayOfYear % Math.Max(1, daysInSeason) + 1;
			int normalizedSeason = (season % 4 + 4) % 4;
			string seasonText = normalizedSeason switch
			{
				0 => "春",
				1 => "夏",
				2 => "秋",
				_ => "冬"
			};
			return year.ToString(CultureInfo.InvariantCulture)
				+ "年"
				+ seasonText
				+ "季"
				+ dayOfSeason.ToString(CultureInfo.InvariantCulture)
				+ "日";
		}
		catch
		{
			return "第" + Math.Max(0, day).ToString(CultureInfo.InvariantCulture) + "天";
		}
	}

	private static string PairKey(string first, string second)
	{
		string a = first ?? "";
		string b = second ?? "";
		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? a + "|" + b : b + "|" + a;
	}

	private static string NewId(string prefix)
	{
		return (prefix ?? "world_diplomacy") + ":" + Guid.NewGuid().ToString("N");
	}

	private static string KingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
	}

	private static string RulerName(Kingdom kingdom)
	{
		return kingdom?.RulingClan?.Leader?.Name?.ToString() ?? "未知统治者";
	}

	private static string SanitizePublicDiplomacyText(string value)
	{
		string text = value ?? "";
		return text
			.Replace("预先核验的结果路线", "可行的交涉方向")
			.Replace("预核验结果路线", "可行的交涉方向")
			.Replace("预核验", "事先审议")
			.Replace("既定外交动作", "正式外交决定")
			.Replace("候选路线", "可行方向")
			.Replace("结果路线", "交涉方向")
			.Replace("程序执行", "正式施行")
			.Replace("游戏外交状态", "外交关系")
			.Replace("世界外交状态", "外交局势")
			.Replace("游戏外交动作", "正式外交行动")
			.Replace("世界状态", "当前局势")
			.Replace("硬目标", "首要目标")
			.Replace("外交回合", "外交交涉")
			.Replace("本回合", "本次交涉")
			.Replace("该回合", "此次交涉")
			.Replace("此回合", "此次交涉")
			.Replace("回合开始", "交涉开始")
			.Replace("回合结束", "交涉告一段落")
			.Replace("接力顺序", "公文往来次序")
			.Replace("接力轮次", "公文往来阶段")
			.Replace("最后行动机会", "最后决定")
			.Replace("程序核验", "正式确认");
	}

	private static string NormalizeBody(string value)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return Limit(text, 6000);
	}

	private static string NormalizeCanonicalHistoryText(string value)
	{
		// Canonical artifacts and configured-size snapshots must never inherit the per-document
		// display cap. Compression, rather than silent truncation, owns their size control.
		return (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
	}

	private static string FormatDiplomaticBodyForDisplay(string value)
	{
		string text = NormalizeBody(value);
		if (string.IsNullOrWhiteSpace(text)) return "";
		List<string> paragraphs = new List<string>();
		foreach (string line in text.Split('\n'))
		{
			string paragraph = (line ?? "").Trim().TrimStart('　');
			if (string.IsNullOrWhiteSpace(paragraph)) continue;
			AppendDiplomaticDisplayParagraphs(paragraphs, paragraph);
		}
		return string.Join("\n\n", paragraphs.Select(x => "　　" + x));
	}

	private static void AppendDiplomaticDisplayParagraphs(List<string> target, string paragraph)
	{
		if (target == null || string.IsNullOrWhiteSpace(paragraph)) return;
		if (paragraph.Length <= 220)
		{
			target.Add(paragraph.Trim());
			return;
		}
		StringBuilder current = new StringBuilder();
		foreach (char ch in paragraph)
		{
			current.Append(ch);
			bool sentenceEnd = ch == '。' || ch == '！' || ch == '？' || ch == '!' || ch == '?' || ch == '；' || ch == ';';
			bool fallbackBreak = (current.Length >= 220 && (ch == '，' || ch == ',' || ch == '、')) || current.Length >= 260;
			if ((current.Length >= 120 && sentenceEnd) || fallbackBreak)
			{
				target.Add(current.ToString().Trim());
				current.Clear();
			}
		}
		string tail = current.ToString().Trim();
		if (string.IsNullOrWhiteSpace(tail)) return;
		if (target.Count > 0 && tail.Length < 45) target[target.Count - 1] += tail;
		else target.Add(tail);
	}

	private static string DeriveTitle(string body, string fallback)
	{
		string firstLine = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').FirstOrDefault()?.Trim() ?? "";
		firstLine = firstLine.Trim('《', '》', '"', '\'', '“', '”');
		return Limit(FirstNonEmpty(firstLine, fallback), 36);
	}

	private static JObject ParseJsonObject(string raw)
	{
		return TryParseJsonObject(raw, out JObject parsed) ? parsed : new JObject();
	}

	private static JObject BuildPeaceTermsJson(WorldDiplomacyPeaceTerms terms)
	{
		return new JObject
		{
			["tribute_payer_kingdom_id"] = terms?.TributePayerKingdomId ?? "",
			["tribute_receiver_kingdom_id"] = terms?.TributeReceiverKingdomId ?? "",
			["daily_tribute"] = Math.Max(0, terms?.DailyTribute ?? 0),
			["duration_days"] = Math.Max(0, terms?.DurationDays ?? 0),
			["cession_from_kingdom_id"] = terms?.CessionFromKingdomId ?? "",
			["cession_to_kingdom_id"] = terms?.CessionToKingdomId ?? "",
			["cession_settlement_id"] = terms?.CessionSettlementId ?? ""
		};
	}

	private static void NormalizeGeneratedDiplomaticEnvelopeShape(WorldDiplomacyJob job, JObject json)
	{
		if (json == null) return;
		if (json["actions"] == null)
		{
			string legacyIntent = ReadString(json, "author_intent.intent", "intent", "author_intent");
			string legacyTargetId = ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
			if (string.IsNullOrWhiteSpace(legacyTargetId)) legacyTargetId = job?.TargetKingdomId ?? "";
			JObject legacyAction = new JObject
			{
				["target_kingdom_id"] = legacyTargetId,
				["intent"] = legacyIntent,
				["peace_terms"] = json["peace_terms"] is JObject legacyTerms
					? legacyTerms.DeepClone()
					: BuildPeaceTermsJson(null)
			};
			string legacyOfferSource = ReadString(json, "responding_to_offer_document_id");
			string legacyThreatSource = ReadString(json, "responding_to_threat_document_id");
			if (!string.IsNullOrWhiteSpace(legacyOfferSource)) legacyAction["responding_to_offer_document_id"] = legacyOfferSource;
			if (!string.IsNullOrWhiteSpace(legacyThreatSource)) legacyAction["responding_to_threat_document_id"] = legacyThreatSource;
			json["actions"] = new JArray(legacyAction);
		}
		if (json["actions"] is JArray actions)
		{
			foreach (JObject action in actions.OfType<JObject>())
			{
				if (action["peace_terms"] is not JObject) action["peace_terms"] = BuildPeaceTermsJson(null);
			}
			MirrorFirstGeneratedActionEnvelope(json, actions);
			json["addressed_kingdom_ids"] = new JArray(actions.OfType<JObject>()
				.Select(x => ReadString(x, "target_kingdom_id", "target"))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase));
		}
		if (json["mentioned_kingdom_ids"] is not JArray)
		{
			json["mentioned_kingdom_ids"] = new JArray();
		}

		if (json["round_plan"] is not JObject roundPlan)
		{
			roundPlan = new JObject();
			json["round_plan"] = roundPlan;
		}
		bool autonomousOpening = IsAutonomousOpeningJob(job);
		if (roundPlan["selected_kingdom_ids"] is not JArray)
		{
			roundPlan["selected_kingdom_ids"] = autonomousOpening && json["actions"] is JArray generatedActions
				? new JArray(generatedActions.OfType<JObject>()
					.Select(x => ReadString(x, "target_kingdom_id", "target"))
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase))
				: new JArray();
		}
		if (roundPlan["topic"] == null || roundPlan["topic"].Type == JTokenType.Null)
		{
			roundPlan["topic"] = autonomousOpening ? FirstNonEmpty(ReadString(json, "title"), "外交交涉") : "";
		}
		if (json["peace_terms"] is not JObject)
		{
			json["peace_terms"] = BuildPeaceTermsJson(null);
		}
		if (json["requires_response"] == null || json["requires_response"].Type == JTokenType.Null) json["requires_response"] = false;
		if (json["tone"] == null || json["tone"].Type == JTokenType.Null) json["tone"] = "neutral";
		if (json["confidence"] == null || json["confidence"].Type == JTokenType.Null) json["confidence"] = 0.5;
		if (json["round_participation"] == null || json["round_participation"].Type == JTokenType.Null) json["round_participation"] = "continue";
		if (json["round_status"] == null || json["round_status"].Type == JTokenType.Null) json["round_status"] = "continue";
		if (json["made_progress"] == null || json["made_progress"].Type == JTokenType.Null) json["made_progress"] = true;
	}

	private static void MirrorFirstGeneratedActionEnvelope(JObject json, JArray actions)
	{
		if (json == null || actions == null || actions.Count == 0 || actions[0] is not JObject first) return;
		string intent = ReadString(first, "intent", "author_intent.intent");
		string targetId = ReadString(first, "target_kingdom_id", "target");
		json["author_intent"] = new JObject
		{
			["intent"] = intent,
			["commitment"] = ReadString(first, "commitment")
		};
		json["primary_target_kingdom_id"] = targetId;
		json["peace_terms"] = first["peace_terms"] is JObject terms ? terms.DeepClone() : BuildPeaceTermsJson(null);
		json["responding_to_offer_document_id"] = ReadString(first, "responding_to_offer_document_id");
		json["responding_to_offer_action_id"] = ReadString(first, "responding_to_offer_action_id");
		json["responding_to_threat_document_id"] = ReadString(first, "responding_to_threat_document_id");
		json["responding_to_threat_action_id"] = ReadString(first, "responding_to_threat_action_id");
	}

	private static JObject BuildGeneratedSingleActionEnvelope(JObject source, JObject action)
	{
		JObject single = source == null ? new JObject() : (JObject)source.DeepClone();
		single.Remove("actions");
		string targetId = ReadString(action, "target_kingdom_id", "target");
		single["author_intent"] = new JObject
		{
			["intent"] = ReadString(action, "intent", "author_intent.intent"),
			["commitment"] = ReadString(action, "commitment")
		};
		single["primary_target_kingdom_id"] = targetId;
		single["negotiation_move"] = ReadString(action, "negotiation_move");
		single["addressed_kingdom_ids"] = string.IsNullOrWhiteSpace(targetId) ? new JArray() : new JArray(targetId);
		single["peace_terms"] = action?["peace_terms"] is JObject terms ? terms.DeepClone() : BuildPeaceTermsJson(null);
		single["responding_to_offer_document_id"] = ReadString(action, "responding_to_offer_document_id");
		single["responding_to_offer_action_id"] = ReadString(action, "responding_to_offer_action_id");
		single["responding_to_threat_document_id"] = ReadString(action, "responding_to_threat_document_id");
		single["responding_to_threat_action_id"] = ReadString(action, "responding_to_threat_action_id");
		return single;
	}

	private static void CopyDerivedGeneratedActionEnvelope(JObject single, JObject action)
	{
		if (single == null || action == null) return;
		action["intent"] = ReadString(single, "author_intent.intent", "intent");
		action["commitment"] = ReadString(single, "author_intent.commitment", "commitment");
		action["negotiation_move"] = ReadString(single, "negotiation_move");
		action["responding_to_offer_document_id"] = ReadString(single, "responding_to_offer_document_id");
		action["responding_to_offer_action_id"] = ReadString(single, "responding_to_offer_action_id");
		action["responding_to_threat_document_id"] = ReadString(single, "responding_to_threat_document_id");
		action["responding_to_threat_action_id"] = ReadString(single, "responding_to_threat_action_id");
		if (single["peace_terms"] is JObject terms) action["peace_terms"] = terms.DeepClone();
	}

	private static bool TryParseJsonObject(string raw, out JObject parsed)
	{
		parsed = null;
		string text = (raw ?? "").Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int firstNewLine = text.IndexOf('\n');
			int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
			if (firstNewLine >= 0 && lastFence > firstNewLine)
			{
				text = text.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
			}
		}
		try
		{
			parsed = JObject.Parse(text);
			return true;
		}
		catch
		{
			int start = text.IndexOf('{');
			int end = text.LastIndexOf('}');
			if (start >= 0 && end > start)
			{
				try
				{
					parsed = JObject.Parse(text.Substring(start, end - start + 1));
					return true;
				}
				catch
				{
				}
			}
			parsed = new JObject();
			return false;
		}
	}

	private static string ReadString(JObject json, params string[] paths)
	{
		foreach (string path in paths ?? Array.Empty<string>())
		{
			try
			{
				string value = json?.SelectToken(path)?.ToString()?.Trim();
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}
			catch
			{
			}
		}
		return "";
	}

	private static bool TryNormalizeInlineResponseBinding(JObject json, out string bindingKind)
	{
		bindingKind = "";
		if (json?["author_intent"] is not JObject authorIntent) return false;
		string rawCommitment = authorIntent["commitment"]?.ToString()?.Trim() ?? "";
		if (rawCommitment.Length == 0) return false;
		const string OfferMarker = ":offer=";
		const string ThreatMarker = ":threat=";
		int offerIndex = rawCommitment.IndexOf(OfferMarker, StringComparison.OrdinalIgnoreCase);
		int threatIndex = rawCommitment.IndexOf(ThreatMarker, StringComparison.OrdinalIgnoreCase);
		if ((offerIndex < 0) == (threatIndex < 0)) return false;
		bool isOffer = offerIndex >= 0;
		int markerIndex = isOffer ? offerIndex : threatIndex;
		string marker = isOffer ? OfferMarker : ThreatMarker;
		if (markerIndex <= 0) return false;
		string normalizedCommitment = NormalizeCommitment(rawCommitment.Substring(0, markerIndex));
		if (!IsSupportedCommitment(normalizedCommitment)) return false;
		string sourceDocumentId = rawCommitment.Substring(markerIndex + marker.Length).Trim();
		const string DocumentIdPrefix = "diplomacy_document:";
		if (!sourceDocumentId.StartsWith(DocumentIdPrefix, StringComparison.OrdinalIgnoreCase)
			|| sourceDocumentId.Length <= DocumentIdPrefix.Length)
		{
			return false;
		}
		string fieldName = isOffer ? "responding_to_offer_document_id" : "responding_to_threat_document_id";
		string existingSource = ReadString(json, fieldName);
		if (!string.IsNullOrWhiteSpace(existingSource)
			&& !string.Equals(existingSource, sourceDocumentId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		authorIntent["commitment"] = normalizedCommitment;
		json[fieldName] = sourceDocumentId;
		bindingKind = isOffer ? "offer" : "threat";
		return true;
	}

	private static bool IsJsonStringArray(JToken token)
	{
		if (token is not JArray array) return false;
		foreach (JToken item in array)
		{
			if (item == null || item.Type != JTokenType.String) return false;
		}
		return true;
	}

	private static List<string> ReadStringList(JObject json, params string[] paths)
	{
		foreach (string path in paths ?? Array.Empty<string>())
		{
			try
			{
				JToken token = json?.SelectToken(path);
				if (token is JArray array) return array.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				string value = token?.ToString()?.Trim();
				if (!string.IsNullOrWhiteSpace(value)) return value.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch
			{
			}
		}
		return new List<string>();
	}

	private static List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(ResolveKingdom).Where(x => x != null && !string.Equals(x.StringId, excludedId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static float ReadFloat(JObject json, string path)
	{
		try
		{
			return float.TryParse(json?.SelectToken(path)?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
				? Math.Max(0f, Math.Min(1f, value))
				: 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private static int ReadInteger(JObject json, string path)
	{
		try
		{
			return int.TryParse(json?.SelectToken(path)?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
				? value
				: 0;
		}
		catch
		{
			return 0;
		}
	}

	private static bool TryReadInteger(JObject json, string path, out int value)
	{
		value = 0;
		try
		{
			JToken token = json?.SelectToken(path);
			return token != null
				&& token.Type != JTokenType.Null
				&& int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}
		catch
		{
			return false;
		}
	}

	private static bool ReadBool(JObject json, string path)
	{
		try
		{
			string value = json?.SelectToken(path)?.ToString()?.Trim();
			return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static string NormalizeIntent(string value)
	{
		string token = NormalizeToken(value);
		return token switch
		{
			"make_peace" or "peace" or "peace_proposal" => "propose_peace",
			"form_alliance" or "alliance" or "alliance_proposal" => "propose_alliance",
			"make_trade" or "trade" or "trade_proposal" => "propose_trade",
			"terminate_alliance" => "break_alliance",
			"end_trade" or "terminate_trade" => "cancel_trade",
			"war" or "declarewar" => "declare_war",
			"complyultimatum" or "comply_with_ultimatum" => "comply_ultimatum",
			"threat" => "warning",
			"denounce" => "condemn",
			"accept" => "accept",
			"reject" => "reject",
			_ => token
		};
	}

	private static string NormalizeCommitment(string value)
	{
		string token = NormalizeToken(value);
		return token switch
		{
			"formal" or "explicit" or "committed" => "binding",
			"offer" => "proposal",
			"accepted" => "acceptance",
			"rejected" => "rejection",
			"none" or "nonbinding" => "non_binding",
			_ => token
		};
	}

	private static string DefaultCommitmentForIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		if (IsProposalIntent(normalized)) return "proposal";
		if (normalized.StartsWith("accept_", StringComparison.Ordinal)) return "acceptance";
		if (normalized.StartsWith("reject_", StringComparison.Ordinal)) return "rejection";
		return normalized is "ultimatum" or "comply_ultimatum" or "apology" or "concession"
			or "declare_war" or "break_alliance" or "cancel_trade"
			? "binding"
			: "non_binding";
	}

	private static string NormalizeTone(string value)
	{
		string token = NormalizeToken(value);
		return token is "conciliatory" or "neutral" or "firm" or "hostile" ? token : "neutral";
	}

	private static string NormalizeToken(string value)
	{
		return (value ?? "").Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
	}

	private static bool ContainsAny(string text, params string[] needles)
	{
		return (needles ?? Array.Empty<string>()).Any(x => !string.IsNullOrWhiteSpace(x) && (text ?? "").IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool IsImmediateIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "declare_war" || normalized == "break_alliance" || normalized == "cancel_trade"
			|| normalized == "comply_ultimatum";
	}

	private static bool IsProposalIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "propose_peace" || normalized == "propose_alliance" || normalized == "propose_trade";
	}

	private static string ResponseIntentToProposalIntent(string intent)
	{
		return NormalizeIntent(intent) switch
		{
			"accept_peace" or "reject_peace" => "propose_peace",
			"accept_alliance" or "reject_alliance" => "propose_alliance",
			"accept_trade" or "reject_trade" => "propose_trade",
			_ => ""
		};
	}

	private static string ProposalIntentToResponseIntent(string proposalIntent, bool accepted)
	{
		return NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => accepted ? "accept_peace" : "reject_peace",
			"propose_alliance" => accepted ? "accept_alliance" : "reject_alliance",
			"propose_trade" => accepted ? "accept_trade" : "reject_trade",
			_ => ""
		};
	}

	private static bool IsPeaceIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "propose_peace" || normalized == "accept_peace" || normalized == "reject_peace";
	}

	private static bool IsSupportedDiplomacyIntent(string intent)
	{
		return NormalizeIntent(intent) is "statement" or "condemn" or "warning" or "ultimatum" or "comply_ultimatum" or "apology" or "concession"
			or "propose_peace" or "accept_peace" or "reject_peace"
			or "propose_alliance" or "accept_alliance" or "reject_alliance" or "break_alliance"
			or "propose_trade" or "accept_trade" or "reject_trade" or "cancel_trade" or "declare_war";
	}

	private static string NormalizeNegotiationMove(string value)
	{
		return NormalizeToken(value);
	}

	private static bool IsSupportedNegotiationMove(string value)
	{
		return NormalizeNegotiationMove(value) is "question" or "clarification" or "state_position" or "justify_demand"
			or "acknowledge_concern" or "dispute_claim" or "counterproposal" or "conditional_acceptance"
			or "partial_concession" or "request_concession" or "revise_terms" or "request_delay"
			or "consult_court" or "set_deadline" or "final_offer" or "withdraw_offer"
			or "end_negotiation" or "declare_deadlock";
	}

	private static bool IsTerminalNegotiationMove(string value)
	{
		return NormalizeNegotiationMove(value) is "end_negotiation" or "declare_deadlock";
	}

	private static bool IsActionableDiplomacyIntent(string intent)
	{
		return NormalizeIntent(intent) is "warning" or "ultimatum" or "comply_ultimatum"
			or "propose_peace" or "accept_peace" or "reject_peace"
			or "propose_alliance" or "accept_alliance" or "reject_alliance" or "break_alliance"
			or "propose_trade" or "accept_trade" or "reject_trade" or "cancel_trade" or "declare_war";
	}

	private static bool IsExternallyResolvedDiplomaticIntent(string intent)
	{
		return NormalizeIntent(intent) is "declare_war" or "accept_peace"
			or "accept_alliance" or "break_alliance"
			or "accept_trade" or "cancel_trade";
	}

	private static bool IsSupportedCommitment(string commitment)
	{
		return NormalizeCommitment(commitment) is "non_binding" or "proposal" or "acceptance" or "rejection" or "binding";
	}

	private static bool IsRoundDiplomaticBehaviorIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return IsActionableDiplomacyIntent(normalized) || normalized is "apology" or "concession";
	}

	private static bool IsTerminalResponseIntent(string intent)
	{
		return NormalizeIntent(intent) is "accept_peace" or "reject_peace"
			or "accept_alliance" or "reject_alliance"
			or "accept_trade" or "reject_trade"
			or "comply_ultimatum" or "apology" or "concession" or "break_alliance" or "cancel_trade" or "declare_war";
	}

	private static bool ResolveValidatedResponseObligation(WorldDiplomacyDocument document, string intent, bool modelRequestedResponse)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
			return false;
		}
		if (document.AutomaticReplyDepth >= MaxAutomaticReplyDepth || IsTerminalResponseIntent(intent))
		{
			return false;
		}
		return IsProposalIntent(intent)
			|| string.Equals(NormalizeIntent(intent), "warning", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(NormalizeIntent(intent), "ultimatum", StringComparison.OrdinalIgnoreCase)
			|| modelRequestedResponse;
	}

	private static bool IsAcceptanceIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "accept"
			|| normalized == "accept_peace"
			|| normalized == "accept_alliance"
			|| normalized == "accept_trade"
			|| normalized == "comply_ultimatum";
	}

	private static string IntentLabel(string intent)
	{
		return NormalizeIntent(intent) switch
		{
			"declare_war" => "正式宣战",
			"propose_peace" => "和平提议",
			"accept_peace" => "接受和平",
			"reject_peace" => "拒绝和平",
			"propose_alliance" => "结盟提议",
			"accept_alliance" => "接受结盟",
			"reject_alliance" => "拒绝结盟",
			"break_alliance" => "解除同盟",
			"propose_trade" => "贸易提议",
			"accept_trade" => "接受贸易",
			"reject_trade" => "拒绝贸易",
			"cancel_trade" => "终止贸易",
			"comply_ultimatum" => "服从最后通牒",
			"ultimatum" => "最后通牒",
			"warning" => "谴责",
			"condemn" => "公开谴责",
			"apology" => "公开致歉",
			"concession" => "外交让步",
			_ => "外交声明"
		};
	}

	private static string BuildFallbackDocumentTitle(WorldDiplomacyDocument document, string intent)
	{
		string target = string.IsNullOrWhiteSpace(document?.TargetKingdomName) || document.TargetKingdomName == "未知王国"
			? ""
			: document.TargetKingdomName;
		string subject = NormalizeIntent(intent) switch
		{
			"declare_war" => "正式宣战",
			"propose_peace" => "提出和平方案",
			"accept_peace" => "宣布接受和平",
			"reject_peace" => "拒绝和平条件",
			"propose_alliance" => "提出结盟",
			"accept_alliance" => "宣布缔结同盟",
			"reject_alliance" => "拒绝结盟",
			"break_alliance" => "宣布解除同盟",
			"propose_trade" => "提出贸易协定",
			"accept_trade" => "宣布达成贸易协定",
			"reject_trade" => "拒绝贸易协定",
			"cancel_trade" => "宣布终止贸易协定",
			"comply_ultimatum" => "宣布服从最后通牒",
			"ultimatum" => "发出最后通牒",
			"warning" => "发布谴责",
			"condemn" => "公开谴责",
			"apology" => "公开致歉",
			"concession" => "公布外交让步",
			_ => document?.IsResponse == true ? "回应外交主张" : "阐明王国立场"
		};
		return Limit(string.IsNullOrWhiteSpace(target) ? subject : "对" + target + subject, 36);
	}

	private static string DocumentTypeLabel(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return "外交公告";
		}
		if (document.IsReminder)
		{
			return "谈判催促";
		}
		if (document.Actions?.Count > 1) return "复合外交宣言";
		return NormalizeIntent(document.Intent) switch
		{
			"declare_war" => "宣战告知",
			"propose_peace" => "和平申请",
			"accept_peace" => "和平回应",
			"reject_peace" => "和平拒绝",
			"propose_alliance" => "同盟申请",
			"accept_alliance" => "同盟回应",
			"reject_alliance" => "同盟拒绝",
			"break_alliance" => "解盟告知",
			"propose_trade" => "贸易申请",
			"accept_trade" => "贸易回应",
			"reject_trade" => "贸易拒绝",
			"cancel_trade" => "贸易终止",
			"comply_ultimatum" => "退让",
			"ultimatum" => "最后通牒",
			"warning" => "谴责",
			"condemn" => "公开谴责",
			"apology" => "外交致歉",
			"concession" => "外交让步",
			_ => document.IsResponse ? "谈判回应" : (document.RequiresResponse ? "谈判等待" : "外交公告")
		};
	}

	private static string BuildNotificationDescription(WorldDiplomacyDocument document)
	{
		if (document == null)
		{
			return "点击查看外交宣言。";
		}
		string targetNames = document.Actions?.Count > 1
			? string.Join("、", document.Actions.Where(x => x != null)
				.Select(x => FirstNonEmpty(x.TargetKingdomName, x.TargetKingdomId))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.CurrentCulture))
			: document.TargetKingdomName;
		string target = string.IsNullOrWhiteSpace(targetNames) ? "" : " · " + targetNames;
		return FirstNonEmpty(document.GameDate, FormatCampaignDate(document.Day))
			+ " · "
			+ DocumentTypeLabel(document)
			+ " · "
			+ document.AuthorKingdomName
			+ target
			+ "。点击查看全文。";
	}

	private string BuildDisplayedDocumentTitle(WorldDiplomacyDocument document)
	{
		return SanitizePublicDiplomacyText(FirstNonEmpty(
			document?.Title,
			document?.AuthorKingdomName + "发布外交宣言",
			"外交宣言"));
	}

	private static string BuildArchiveIndexDocumentTitle(WorldDiplomacyDocument document)
	{
		string title = SanitizePublicDiplomacyText(FirstNonEmpty(document?.Title, document?.AuthorKingdomName + "发布外交宣言", "外交宣言"));
		string cleaned = Regex.Replace(
			title,
			@"^\s*(?:致|复|回复|回应|答复|答|回)\s*[^：:\r\n]{1,48}[：:]\s*",
			"",
			RegexOptions.CultureInvariant);
		string targetName = (document?.TargetKingdomName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(targetName))
		{
			cleaned = Regex.Replace(
				cleaned,
				@"^\s*(?:致|复|回复|回应|答复|答|回)\s*" + Regex.Escape(targetName) + @"(?:王国|帝国|王庭)?(?:的)?\s*[：:—\-·]*\s*",
				"",
				RegexOptions.CultureInvariant);
		}
		return FirstNonEmpty(cleaned, title, "外交宣言");
	}

	private string BuildDocumentEventMeta(WorldDiplomacyDocument document)
	{
		WorldDiplomacyRound round = ResolveRound(document?.RoundId);
		string topic = SanitizePublicDiplomacyText(FirstNonEmpty(round?.RoundTopic, ResolveDocument(round?.RootDocumentId)?.Title));
		return string.IsNullOrWhiteSpace(topic) ? "" : "  ·  外交事件：" + Limit(topic, 48);
	}

	private string BuildRoyalAnnouncementSubtitle()
	{
		WorldDiplomacyRound round = _storage.ActiveRound;
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			|| string.IsNullOrWhiteSpace(round.RootDocumentId))
		{
			return "统一查看自定义政策、政策衍生事件与各国公开发布的外交宣言。";
		}
		string topic = SanitizePublicDiplomacyText(FirstNonEmpty(round.RoundTopic, ResolveDocument(round.RootDocumentId)?.Title, "外交交涉"));
		List<string> participantNames = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
			.Where(x => x != null && string.Equals(x.State, "active", StringComparison.OrdinalIgnoreCase))
			.Select(x => ResolveKingdom(x.KingdomId))
			.Select(ResolveWorldDiplomacyRepresentative)
			.Where(HasIndependentWorldDiplomacyAuthority)
			.Select(KingdomName)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.CurrentCulture)
			.ToList();
		if (participantNames.Count == 0)
		{
			Kingdom initiator = ResolveKingdom(round.InitiatorKingdomId);
			if (initiator != null) participantNames.Add(KingdomName(initiator));
		}
		return "当前外交事件：" + Limit(topic, 60)
			+ "  ·  进行中"
			+ (participantNames.Count == 0 ? "" : "  ·  参与国：" + string.Join("、", participantNames));
	}

	private static int ParseDayForArchive(string value)
	{
		string text = value ?? "";
		int yearMarker = text.IndexOf('年');
		int dayMarker = text.LastIndexOf('日');
		if (yearMarker > 0 && dayMarker > yearMarker)
		{
			string yearDigits = new string(text.Substring(0, yearMarker).Where(char.IsDigit).ToArray());
			string dayDigits = new string(text.Substring(yearMarker + 1, dayMarker - yearMarker - 1).Where(char.IsDigit).ToArray());
			if (int.TryParse(yearDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
			{
				int season = text.IndexOf('夏') >= 0 ? 1 : text.IndexOf('秋') >= 0 ? 2 : text.IndexOf('冬') >= 0 ? 3 : 0;
				int.TryParse(dayDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dayOfSeason);
				return year * 1000 + season * 100 + dayOfSeason;
			}
		}
		string digits = new string(text.Where(char.IsDigit).ToArray());
		return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) ? day : 0;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
	}

	private static string Limit(string value, int maxChars)
	{
		string text = value ?? "";
		return text.Length <= maxChars ? text : text.Substring(0, Math.Max(0, maxChars));
	}

	private static bool CanPublishMapNotification()
	{
		try
		{
			return Mission.Current == null
				&& Game.Current?.GameStateManager?.ActiveState is MapState
				&& MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

	private static void ProcessComposePopup()
	{
		try
		{
			WorldDiplomacyComposePopup.ProcessDeferredCloseIfNeeded();
		}
		catch
		{
		}
	}

	private static void Log(string message)
	{
		Logger.Log(Source, "[AF-WORLD-DIPLOMACY] " + message);
	}

	private sealed class WarSituationSnapshot
	{
		public int Day;
		public bool IsAtWar;
		public int WarDays;
		public float AuthorStrength;
		public float TargetStrength;
		public float AuthorProgress;
		public float TargetProgress;
		public int AuthorInflictedCasualties;
		public int AuthorSufferedCasualties;
		public int AuthorSuccessfulSieges;
		public int TargetSuccessfulSieges;
		public int AuthorOtherWars;
		public int TargetOtherWars;
		public float AuthorPeacePressure;
		public float TargetPeacePressure;
		public float AuthorCessionScore;
		public float TargetCessionScore;
		public int AuthorSuggestedTribute;
		public int TargetSuggestedTribute;
	}

	private sealed class WorldDiplomacyBorderRelation
	{
		public bool SharesBorder;
		public string FirstSettlementId = "";
		public string FirstSettlementName = "";
		public string SecondSettlementId = "";
		public string SecondSettlementName = "";
		public float Distance = float.MaxValue;
	}

	private sealed class CanonicalHistoryMigrationWorkItem
	{
		public int Day;
		public long CreatedUtcTicks;
		public string StableKey = "";
		public WorldDiplomacyDocument Document;
		public MyBehavior.WorldWeeklyReportHistoryEntry WorldWeeklyReport;
		public PublishedPolicyArtifactLedgerEntry Policy;
	}

	private sealed class LlmJobResult
	{
		public string JobId = "";
		public long RuntimeGeneration;
		public bool Success;
		public string Content = "";
		public string Error = "";
		public bool IsServiceFailure;
		public bool IsOutputTruncated;
		public int? PromptTokens;
		public int? CompletionTokens;
		public int? PromptCacheHitTokens;
		public int? PromptCacheMissTokens;
		public int? PromptCacheCreationTokens;
		public int? PromptUncachedTokens;
	}
}

public sealed class WorldDiplomacyRound
{
	[JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }
	[JsonProperty("roundId")] public string RoundId { get; set; } = "";
	[JsonProperty("initiatorKingdomId")] public string InitiatorKingdomId { get; set; } = "";
	[JsonProperty("rootDocumentId")] public string RootDocumentId { get; set; } = "";
	[JsonProperty("finalDocumentId")] public string FinalDocumentId { get; set; } = "";
	[JsonProperty("state")] public string State { get; set; } = "active";
	[JsonProperty("startedDay")] public int StartedDay { get; set; }
	[JsonProperty("lastActivityDay")] public int LastActivityDay { get; set; }
	[JsonProperty("softEndDay")] public int SoftEndDay { get; set; }
	[JsonProperty("completedDay")] public int CompletedDay { get; set; }
	[JsonProperty("closeReason")] public string CloseReason { get; set; } = "";
	[JsonProperty("isPlayerInsertion")] public bool IsPlayerInsertion { get; set; }
	[JsonProperty("automaticDocumentsStarted")] public int AutomaticDocumentsStarted { get; set; }
	[JsonProperty("automaticCircuitBreakerTripped")] public bool AutomaticCircuitBreakerTripped { get; set; }
	[JsonProperty("relayPlanned")] public bool RelayPlanned { get; set; }
	[JsonProperty("relayRouteKingdomIds")] public List<string> RelayRouteKingdomIds { get; set; } = new List<string>();
	[JsonProperty("relayCursor")] public int RelayCursor { get; set; }
	[JsonProperty("relayDirection")] public int RelayDirection { get; set; } = 1;
	[JsonProperty("relayPassNumber")] public int RelayPassNumber { get; set; }
	[JsonProperty("relayPassStartedDay")] public int RelayPassStartedDay { get; set; }
	[JsonProperty("relayPassDurationDays")] public int RelayPassDurationDays { get; set; }
	[JsonProperty("relaySequence")] public int RelaySequence { get; set; }
	[JsonProperty("relayWaiting")] public bool RelayWaiting { get; set; }
	[JsonProperty("hardEndDay")] public int HardEndDay { get; set; }
	[JsonProperty("roundTopic")] public string RoundTopic { get; set; } = "";
	[JsonProperty("topicCategory")] public string TopicCategory { get; set; } = "";
	[JsonProperty("topicFingerprint")] public string TopicFingerprint { get; set; } = "";
	[JsonProperty("topicSeedContext")] public string TopicSeedContext { get; set; } = "";
	[JsonProperty("eventSourceType")] public string EventSourceType { get; set; } = "";
	[JsonProperty("eventMotif")] public string EventMotif { get; set; } = "";
	[JsonProperty("eventLocation")] public string EventLocation { get; set; } = "";
	[JsonProperty("allowedFiction")] public string AllowedFiction { get; set; } = "";
	[JsonProperty("forbiddenFiction")] public string ForbiddenFiction { get; set; } = "";
	[JsonProperty("requiresSharedBorder")] public bool RequiresSharedBorder { get; set; }
	[JsonProperty("potentialActionIntents")] public List<string> PotentialActionIntents { get; set; } = new List<string>();
	[JsonProperty("commonContractSnapshot")] public string CommonContractSnapshot { get; set; } = "";
	[JsonProperty("commonContractSnapshotInitialized")] public bool CommonContractSnapshotInitialized { get; set; }
	[JsonProperty("cachePrefix")] public string CachePrefix { get; set; } = "";
	[JsonProperty("externalSignalKeys")] public List<string> ExternalSignalKeys { get; set; } = new List<string>();
	[JsonProperty("attachedPolicySignals")] public List<WorldDiplomacyPolicySignal> AttachedPolicySignals { get; set; } = new List<WorldDiplomacyPolicySignal>();
	[JsonProperty("externalOpeningContext")] public string ExternalOpeningContext { get; set; } = "";
	[JsonProperty("llmTranscript")] public List<WorldDiplomacyLlmMessage> LlmTranscript { get; set; } = new List<WorldDiplomacyLlmMessage>();
	[JsonProperty("llmProfiledKingdomIds")] public List<string> LlmProfiledKingdomIds { get; set; } = new List<string>();
	[JsonProperty("llmLastStateSignatureByKingdom")] public Dictionary<string, string> LlmLastStateSignatureByKingdom { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	[JsonProperty("roundStatus")] public string RoundStatus { get; set; } = "active";
	[JsonProperty("executedActionCount")] public int ExecutedActionCount { get; set; }
	[JsonProperty("substantiveProgressCount")] public int SubstantiveProgressCount { get; set; }
	[JsonProperty("diplomaticActionAttemptCount")] public int DiplomaticActionAttemptCount { get; set; }
	[JsonProperty("actionAttemptCountAtPassStart")] public int ActionAttemptCountAtPassStart { get; set; }
	[JsonProperty("consecutiveNoActionPasses")] public int ConsecutiveNoActionPasses { get; set; }
	[JsonProperty("lastAccountedRelayPassNumber")] public int LastAccountedRelayPassNumber { get; set; }
	[JsonProperty("lastSubstantiveProgressDay")] public int LastSubstantiveProgressDay { get; set; }
	[JsonProperty("finalActionOpportunityIssued")] public bool FinalActionOpportunityIssued { get; set; }
	[JsonProperty("pendingOffers")] public List<WorldDiplomacyRoundOffer> PendingOffers { get; set; } = new List<WorldDiplomacyRoundOffer>();
	[JsonProperty("participants")] public List<WorldDiplomacyRoundParticipant> Participants { get; set; } = new List<WorldDiplomacyRoundParticipant>();
	[JsonProperty("resultSettlementPending")] public bool ResultSettlementPending { get; set; }
	[JsonProperty("resultSettlementTriggerDocumentId")] public string ResultSettlementTriggerDocumentId { get; set; } = "";
	[JsonProperty("resultSettlementCloseReason")] public string ResultSettlementCloseReason { get; set; } = "";
	[JsonProperty("resultSettlementRoundStatus")] public string ResultSettlementRoundStatus { get; set; } = "resolved";
	[JsonProperty("resultSettlementRouteInitialized")] public bool ResultSettlementRouteInitialized { get; set; }
	[JsonProperty("resultSettlementCurrentSlotId")] public string ResultSettlementCurrentSlotId { get; set; } = "";
	[JsonProperty("resultSettlementPlayerWaitingSinceDay")] public int ResultSettlementPlayerWaitingSinceDay { get; set; }
	[JsonProperty("resultSettlementSlots")] public List<WorldDiplomacyResultSettlementSlot> ResultSettlementSlots { get; set; } = new List<WorldDiplomacyResultSettlementSlot>();
	[JsonProperty("resultSettlementWarDocumentIds")] public List<string> ResultSettlementWarDocumentIds { get; set; } = new List<string>();
}

public sealed class WorldDiplomacyLlmMessage
{
	[JsonProperty("role")] public string Role { get; set; } = "";
	[JsonProperty("content")] public string Content { get; set; } = "";
	[JsonProperty("strategicProfileKingdomId")] public string StrategicProfileKingdomId { get; set; } = "";
}

public sealed class WorldDiplomacyRoundOffer
{
	[JsonProperty("sourceDocumentId")] public string SourceDocumentId { get; set; } = "";
	[JsonProperty("sourceActionId")] public string SourceActionId { get; set; } = "";
	[JsonProperty("proposerKingdomId")] public string ProposerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("intent")] public string Intent { get; set; } = "";
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
}

public sealed class WorldDiplomacyOfferCooldown
{
	[JsonProperty("proposerKingdomId")] public string ProposerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("domain")] public string Domain { get; set; } = "";
	[JsonProperty("lastFailedRoundDay")] public int LastFailedRoundDay { get; set; } = -1;
	[JsonProperty("sourceRoundId")] public string SourceRoundId { get; set; } = "";
}

public sealed class WorldDiplomacyThreatNonComplianceEvent
{
	[JsonProperty("stage")] public string Stage { get; set; } = "warning";
	[JsonProperty("stageDocumentId")] public string StageDocumentId { get; set; } = "";
	[JsonProperty("stageActionId")] public string StageActionId { get; set; } = "";
	[JsonProperty("decisionDocumentId")] public string DecisionDocumentId { get; set; } = "";
	[JsonProperty("decisionActionId")] public string DecisionActionId { get; set; } = "";
	[JsonProperty("decisionRoundId")] public string DecisionRoundId { get; set; } = "";
	[JsonProperty("decisionDay")] public int DecisionDay { get; set; }
	[JsonProperty("historyRecorded")] public bool HistoryRecorded { get; set; }
}

public sealed class WorldDiplomacyThreat
{
	[JsonProperty("threatId")] public string ThreatId { get; set; } = "";
	[JsonProperty("issuerKingdomId")] public string IssuerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("stage")] public string Stage { get; set; } = "warning";
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("warningDocumentId")] public string WarningDocumentId { get; set; } = "";
	[JsonProperty("warningActionId")] public string WarningActionId { get; set; } = "";
	[JsonProperty("ultimatumDocumentId")] public string UltimatumDocumentId { get; set; } = "";
	[JsonProperty("ultimatumActionId")] public string UltimatumActionId { get; set; } = "";
	[JsonProperty("stageDocumentId")] public string StageDocumentId { get; set; } = "";
	[JsonProperty("stageActionId")] public string StageActionId { get; set; } = "";
	[JsonProperty("stageRoundId")] public string StageRoundId { get; set; } = "";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
	[JsonProperty("stageIssuedDay")] public int StageIssuedDay { get; set; }
	[JsonProperty("updatedDay")] public int UpdatedDay { get; set; }
	[JsonProperty("targetDecision")] public string TargetDecision { get; set; } = "pending";
	[JsonProperty("targetDecisionDocumentId")] public string TargetDecisionDocumentId { get; set; } = "";
	[JsonProperty("targetDecisionActionId")] public string TargetDecisionActionId { get; set; } = "";
	[JsonProperty("targetDecisionRoundId")] public string TargetDecisionRoundId { get; set; } = "";
	[JsonProperty("targetDecisionDay")] public int TargetDecisionDay { get; set; }
	[JsonProperty("nonComplianceHistoryRecorded")] public bool NonComplianceHistoryRecorded { get; set; }
	[JsonProperty("nonComplianceEvents")] public List<WorldDiplomacyThreatNonComplianceEvent> NonComplianceEvents { get; set; } = new List<WorldDiplomacyThreatNonComplianceEvent>();
	[JsonProperty("obligationRoundId")] public string ObligationRoundId { get; set; } = "";
	[JsonProperty("obligationClaimedDay")] public int ObligationClaimedDay { get; set; }
	[JsonProperty("complianceDocumentId")] public string ComplianceDocumentId { get; set; } = "";
	[JsonProperty("complianceActionId")] public string ComplianceActionId { get; set; } = "";
	[JsonProperty("resolutionRoundId")] public string ResolutionRoundId { get; set; } = "";
	[JsonProperty("resolutionDocumentId")] public string ResolutionDocumentId { get; set; } = "";
	[JsonProperty("resolutionActionId")] public string ResolutionActionId { get; set; } = "";
	[JsonProperty("resolutionReason")] public string ResolutionReason { get; set; } = "";
	[JsonProperty("reputationPenaltyApplied")] public bool ReputationPenaltyApplied { get; set; }
	[JsonProperty("reputationPenaltyAmount")] public int ReputationPenaltyAmount { get; set; }
	[JsonProperty("issuerResolutionNoticePending")] public bool IssuerResolutionNoticePending { get; set; }
	[JsonProperty("historyResultRecorded")] public bool HistoryResultRecorded { get; set; }
	[JsonProperty("domesticPenaltyRulingClanId")] public string DomesticPenaltyRulingClanId { get; set; } = "";
	[JsonProperty("domesticPenaltyEligibleClanIds")] public List<string> DomesticPenaltyEligibleClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltyAppliedClanIds")] public List<string> DomesticPenaltyAppliedClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltySkippedClanIds")] public List<string> DomesticPenaltySkippedClanIds { get; set; } = new List<string>();
	[JsonProperty("domesticPenaltySnapshotCaptured")] public bool DomesticPenaltySnapshotCaptured { get; set; }
	[JsonProperty("domesticPenaltyCompleted")] public bool DomesticPenaltyCompleted { get; set; }
	[JsonProperty("domesticPenaltyHistoryRecorded")] public bool DomesticPenaltyHistoryRecorded { get; set; }
	[JsonProperty("policyConditionSignalKey")] public string PolicyConditionSignalKey { get; set; } = "";
	[JsonProperty("policyConditionPolicyId")] public string PolicyConditionPolicyId { get; set; } = "";
	[JsonProperty("policyConditionPolicyName")] public string PolicyConditionPolicyName { get; set; } = "";
	[JsonProperty("policyConditionOwnerKingdomId")] public string PolicyConditionOwnerKingdomId { get; set; } = "";
	[JsonProperty("policyConditionAffectedKingdomId")] public string PolicyConditionAffectedKingdomId { get; set; } = "";
	[JsonProperty("policyConditionBoundDay")] public int PolicyConditionBoundDay { get; set; }
	[JsonProperty("policyConditionCancellationCompleted")] public bool PolicyConditionCancellationCompleted { get; set; }
	[JsonProperty("policyConditionCancellationStatus")] public string PolicyConditionCancellationStatus { get; set; } = "";
	[JsonProperty("policyConditionCancellationDay")] public int PolicyConditionCancellationDay { get; set; }
	[JsonProperty("issuerRewardRulingClanId")] public string IssuerRewardRulingClanId { get; set; } = "";
	[JsonProperty("issuerRewardEligibleClanIds")] public List<string> IssuerRewardEligibleClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardAppliedClanIds")] public List<string> IssuerRewardAppliedClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardSkippedClanIds")] public List<string> IssuerRewardSkippedClanIds { get; set; } = new List<string>();
	[JsonProperty("issuerRewardSnapshotCaptured")] public bool IssuerRewardSnapshotCaptured { get; set; }
	[JsonProperty("issuerRewardCompleted")] public bool IssuerRewardCompleted { get; set; }
	[JsonProperty("issuerRewardAmount")] public int IssuerRewardAmount { get; set; }
	[JsonProperty("issuerRewardHistoryRecorded")] public bool IssuerRewardHistoryRecorded { get; set; }
}

public sealed class WorldDiplomacyRoundParticipant
{
	[JsonProperty("kingdomId")] public string KingdomId { get; set; } = "";
	[JsonProperty("state")] public string State { get; set; } = "observer";
	[JsonProperty("mandatoryReplyPending")] public bool MandatoryReplyPending { get; set; }
	[JsonProperty("lastSpokeDay")] public int LastSpokeDay { get; set; }
	[JsonProperty("lastEvaluationDay")] public int LastEvaluationDay { get; set; }
	[JsonProperty("lastEvaluationMaterialDay")] public int LastEvaluationMaterialDay { get; set; }
	[JsonProperty("lastTriggeredDocumentId")] public string LastTriggeredDocumentId { get; set; } = "";
	[JsonProperty("mandatorySinceDay")] public int MandatorySinceDay { get; set; }
	[JsonProperty("reminderSent")] public bool ReminderSent { get; set; }
	[JsonProperty("selectedForRelay")] public bool SelectedForRelay { get; set; }
	[JsonProperty("isPlayerAsync")] public bool IsPlayerAsync { get; set; }
	[JsonProperty("turnCount")] public int TurnCount { get; set; }
	[JsonProperty("role")] public string Role { get; set; } = "";
	[JsonProperty("agenda")] public string Agenda { get; set; } = "";
	[JsonProperty("primaryTargetKingdomId")] public string PrimaryTargetKingdomId { get; set; } = "";
	[JsonProperty("preferredOutcome")] public string PreferredOutcome { get; set; } = "";
	[JsonProperty("redLine")] public string RedLine { get; set; } = "";
	[JsonProperty("leverage")] public string Leverage { get; set; } = "";
	[JsonProperty("requiredContribution")] public string RequiredContribution { get; set; } = "";
	[JsonProperty("contributionMade")] public bool ContributionMade { get; set; }
}

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

public sealed class WorldDiplomacyStorage
{
	[JsonProperty("diplomacyNotificationStateSchemaVersion")]
	public int DiplomacyNotificationStateSchemaVersion { get; set; }

	[JsonProperty("resultSettlementStateSchemaVersion")]
	public int ResultSettlementStateSchemaVersion { get; set; }

	[JsonProperty("offerCooldownStateSchemaVersion")]
	public int OfferCooldownStateSchemaVersion { get; set; }

	[JsonProperty("offerCooldowns")]
	public List<WorldDiplomacyOfferCooldown> OfferCooldowns { get; set; } = new List<WorldDiplomacyOfferCooldown>();

	[JsonProperty("diplomaticThreatStateSchemaVersion")]
	public int DiplomaticThreatStateSchemaVersion { get; set; }

	[JsonProperty("diplomaticThreats")]
	public List<WorldDiplomacyThreat> DiplomaticThreats { get; set; } = new List<WorldDiplomacyThreat>();

	[JsonProperty("diplomaticReputationByKingdom")]
	public Dictionary<string, int> NationalPrestigeByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("internationalReputationByKingdom")]
	public Dictionary<string, int> InternationalReputationByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("internationalReputationNaturalChangeLastDayByKingdom")]
	public Dictionary<string, int> InternationalReputationNaturalChangeLastDayByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("nationalPrestigeRelationModifiers")]
	public List<WorldDiplomacyPrestigeRelationModifier> NationalPrestigeRelationModifiers { get; set; } = new List<WorldDiplomacyPrestigeRelationModifier>();

	[JsonProperty("historyMemorySchemaVersion")]
	public int HistoryMemorySchemaVersion { get; set; }

	[JsonProperty("promptContractVersion")]
	public int PromptContractVersion { get; set; }

	[JsonProperty("canonicalHistory")]
	public WorldDiplomacyCanonicalHistoryState CanonicalHistory { get; set; } = new WorldDiplomacyCanonicalHistoryState();

	[JsonProperty("decisionArchitectureVersion")]
	public int DecisionArchitectureVersion { get; set; }

	[JsonProperty("propagationReliabilityVersion")]
	public int PropagationReliabilityVersion { get; set; }

	[JsonProperty("initialPeacePending")]
	public bool InitialPeacePending { get; set; }

	[JsonProperty("initialPeaceApplied")]
	public bool InitialPeaceApplied { get; set; }

	[JsonProperty("activeRound")]
	public WorldDiplomacyRound ActiveRound { get; set; }

	[JsonProperty("completedRounds")]
	public List<WorldDiplomacyRound> CompletedRounds { get; set; } = new List<WorldDiplomacyRound>();

	[JsonProperty("propagationArrivals")]
	public List<WorldDiplomacyPropagationArrival> PropagationArrivals { get; set; } = new List<WorldDiplomacyPropagationArrival>();

	[JsonProperty("settlementKnowledge")]
	public List<WorldDiplomacySettlementKnowledge> SettlementKnowledge { get; set; } = new List<WorldDiplomacySettlementKnowledge>();

	[JsonProperty("kingdomKnowledge")]
	public List<WorldDiplomacyKingdomKnowledge> KingdomKnowledge { get; set; } = new List<WorldDiplomacyKingdomKnowledge>();

	[JsonProperty("nobleKnowledge")]
	public List<WorldDiplomacyKingdomKnowledge> NobleKnowledge { get; set; } = new List<WorldDiplomacyKingdomKnowledge>();

	[JsonProperty("courtKnowledgeMigratedToNobles")]
	public bool CourtKnowledgeMigratedToNobles { get; set; }

	[JsonProperty("pendingParticipationEvaluations")]
	public List<WorldDiplomacyParticipationRequest> PendingParticipationEvaluations { get; set; } = new List<WorldDiplomacyParticipationRequest>();

	[JsonProperty("pendingSpeeches")]
	public List<WorldDiplomacyPendingSpeech> PendingSpeeches { get; set; } = new List<WorldDiplomacyPendingSpeech>();

	[JsonProperty("relayArrivals")]
	public List<WorldDiplomacyRelayArrival> RelayArrivals { get; set; } = new List<WorldDiplomacyRelayArrival>();

	[JsonProperty("playerOpportunities")]
	public List<WorldDiplomacyPlayerOpportunity> PlayerOpportunities { get; set; } = new List<WorldDiplomacyPlayerOpportunity>();

	[JsonProperty("roundSummaries")]
	public List<WorldDiplomacyRoundSummary> RoundSummaries { get; set; } = new List<WorldDiplomacyRoundSummary>();

	[JsonProperty("pendingPolicySignals")]
	public List<WorldDiplomacyPolicySignal> PendingPolicySignals { get; set; } = new List<WorldDiplomacyPolicySignal>();

	[JsonProperty("processedPolicySignalKeys")]
	public List<string> ProcessedPolicySignalKeys { get; set; } = new List<string>();

	[JsonProperty("recentTopicUses")]
	public List<WorldDiplomacyTopicUse> RecentTopicUses { get; set; } = new List<WorldDiplomacyTopicUse>();

	[JsonProperty("forcedWarToggleWasEnabled")]
	public bool ForcedWarToggleWasEnabled { get; set; } = true;

	[JsonProperty("lastAppliedContinentSpreadDays")]
	public int LastAppliedContinentSpreadDays { get; set; }

	[JsonProperty("lastAppliedCourtDeliveryDays")]
	public int LastAppliedCourtDeliveryDays { get; set; }

	[JsonProperty("lastAppliedCivilianSpreadDays")]
	public int LastAppliedCivilianSpreadDays { get; set; }

	[JsonProperty("documents")]
	public List<WorldDiplomacyDocument> Documents { get; set; } = new List<WorldDiplomacyDocument>();

	[JsonProperty("annualSummaries")]
	public List<WorldDiplomacyAnnualSummary> AnnualSummaries { get; set; } = new List<WorldDiplomacyAnnualSummary>();

	[JsonProperty("compressionSummaries")]
	public List<WorldDiplomacyCompressionSummary> CompressionSummaries { get; set; } = new List<WorldDiplomacyCompressionSummary>();

	[JsonProperty("warPressure")]
	public List<WarPressureEntry> WarPressure { get; set; } = new List<WarPressureEntry>();

	[JsonProperty("activeWarLedgers")]
	public List<WorldDiplomacyWarLedger> ActiveWarLedgers { get; set; } = new List<WorldDiplomacyWarLedger>();

	[JsonProperty("recentBattles")]
	public List<WorldDiplomacyBattleFact> RecentBattles { get; set; } = new List<WorldDiplomacyBattleFact>();

	[JsonProperty("nativeSignals")]
	public List<NativeDiplomacySignal> NativeSignals { get; set; } = new List<NativeDiplomacySignal>();

	[JsonProperty("jobs")]
	public List<WorldDiplomacyJob> Jobs { get; set; } = new List<WorldDiplomacyJob>();

	[JsonProperty("activeExchange")]
	public WorldDiplomacyExchange ActiveExchange { get; set; }

	[JsonProperty("suspendedExchanges")]
	public List<WorldDiplomacyExchange> SuspendedExchanges { get; set; } = new List<WorldDiplomacyExchange>();

	[JsonProperty("lastOffensiveWarDayByKingdom")]
	public Dictionary<string, int> LastOffensiveWarDayByKingdom { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("lastPeaceDayByPair")]
	public Dictionary<string, int> LastPeaceDayByPair { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty("nextNormalRoundDay")]
	public int NextNormalRoundDay { get; set; }

	[JsonProperty("lastAppliedRoundIntervalDays")]
	public int LastAppliedRoundIntervalDays { get; set; }

	[JsonProperty("rotationIndex")]
	public int RotationIndex { get; set; }

	[JsonProperty("lastCompressedYear")]
	public int LastCompressedYear { get; set; } = -1;

	[JsonProperty("diplomacyTokensSinceCompression")]
	public long DiplomacyTokensSinceCompression { get; set; }

	[JsonProperty("diplomacyCompressionPending")]
	public bool DiplomacyCompressionPending { get; set; }

	[JsonProperty("lastDiplomacyCompressionDay")]
	public int LastDiplomacyCompressionDay { get; set; } = -1;

	[JsonProperty("compressionSequence")]
	public int CompressionSequence { get; set; }

	[JsonProperty("compressionRetryAfterHour")]
	public int CompressionRetryAfterHour { get; set; }

	[JsonProperty("compressionRetryAttempts")]
	public int CompressionRetryAttempts { get; set; }

	[JsonProperty("serviceCooldownUntilHour")]
	public int ServiceCooldownUntilHour { get; set; }

	[JsonProperty("consecutiveServiceFailures")]
	public int ConsecutiveServiceFailures { get; set; }
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

public sealed class WorldDiplomacyDocumentAction
{
	[JsonProperty("actionId")] public string ActionId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomName")] public string TargetKingdomName { get; set; } = "";
	[JsonProperty("intent")] public string Intent { get; set; } = "";
	[JsonProperty("negotiationMove")] public string NegotiationMove { get; set; } = "";
	[JsonProperty("commitment")] public string Commitment { get; set; } = "";
	[JsonProperty("requiresResponse")] public bool RequiresResponse { get; set; }
	[JsonProperty("respondingToOfferDocumentId")] public string RespondingToOfferDocumentId { get; set; } = "";
	[JsonProperty("respondingToOfferActionId")] public string RespondingToOfferActionId { get; set; } = "";
	[JsonProperty("respondingToThreatDocumentId")] public string RespondingToThreatDocumentId { get; set; } = "";
	[JsonProperty("respondingToThreatActionId")] public string RespondingToThreatActionId { get; set; } = "";
	[JsonProperty("peaceTerms")] public WorldDiplomacyPeaceTerms PeaceTerms { get; set; }
	[JsonProperty("mechanicalResult")] public string MechanicalResult { get; set; } = "";
	[JsonProperty("changedDiplomaticState")] public bool ChangedDiplomaticState { get; set; }
	[JsonProperty("historyResultRecorded")] public bool HistoryResultRecorded { get; set; }
}

public sealed class WorldDiplomacyDocument
{
	[JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore)]
	public List<WorldDiplomacyDocumentAction> Actions { get; set; }

	[JsonIgnore]
	public string ProcessingActionId { get; set; } = "";

	[JsonProperty("historyDeclarationRecorded")]
	public bool HistoryDeclarationRecorded { get; set; }

	[JsonProperty("historyResultRecorded")]
	public bool HistoryResultRecorded { get; set; }

	[JsonProperty("roundId")]
	public string RoundId { get; set; } = "";

	[JsonProperty("originSettlementId")]
	public string OriginSettlementId { get; set; } = "";

	[JsonProperty("addressedKingdomIds")]
	public List<string> AddressedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("mentionedKingdomIds")]
	public List<string> MentionedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("propagationStarted")]
	public bool PropagationStarted { get; set; }

	[JsonProperty("propagationCompleted")]
	public bool PropagationCompleted { get; set; }

	[JsonProperty("hasReachedPlayerCourt")]
	public bool HasReachedPlayerCourt { get; set; }

	[JsonProperty("documentId")]
	public string DocumentId { get; set; } = "";

	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("respondingToOfferDocumentId")]
	public string RespondingToOfferDocumentId { get; set; } = "";

	[JsonProperty("respondingToOfferActionId")]
	public string RespondingToOfferActionId { get; set; } = "";

	[JsonProperty("respondingToThreatDocumentId")]
	public string RespondingToThreatDocumentId { get; set; } = "";

	[JsonProperty("respondingToThreatActionId")]
	public string RespondingToThreatActionId { get; set; } = "";

	[JsonProperty("presentedThreatDocumentIds")]
	public List<string> PresentedThreatDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatFollowThroughDocumentIds")]
	public List<string> PresentedThreatFollowThroughDocumentIds { get; set; } = new List<string>();

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("authorKingdomName")]
	public string AuthorKingdomName { get; set; } = "";

	[JsonProperty("authorRulerId")]
	public string AuthorRulerId { get; set; } = "";

	[JsonProperty("authorRulerName")]
	public string AuthorRulerName { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomName")]
	public string TargetKingdomName { get; set; } = "";

	[JsonProperty("title")]
	public string Title { get; set; } = "";

	[JsonProperty("body")]
	public string Body { get; set; } = "";

	[JsonProperty("origin")]
	public string Origin { get; set; } = "";

	[JsonProperty("intent")]
	public string Intent { get; set; } = "";

	[JsonProperty("negotiationMove")]
	public string NegotiationMove { get; set; } = "";

	[JsonProperty("commitment")]
	public string Commitment { get; set; } = "";

	[JsonProperty("tone")]
	public string Tone { get; set; } = "";

	[JsonProperty("confidence")]
	public float Confidence { get; set; }

	[JsonProperty("analysisStatus")]
	public string AnalysisStatus { get; set; } = "";

	[JsonProperty("hiddenIntent")]
	public string HiddenIntent { get; set; } = "";

	[JsonProperty("hiddenCommitment")]
	public string HiddenCommitment { get; set; } = "";

	[JsonProperty("mechanicalResult")]
	public string MechanicalResult { get; set; } = "";

	[JsonProperty("changedDiplomaticState")]
	public bool ChangedDiplomaticState { get; set; }

	[JsonProperty("peaceTerms")]
	public WorldDiplomacyPeaceTerms PeaceTerms { get; set; }

	[JsonProperty("day")]
	public int Day { get; set; }

	[JsonProperty("gameDate")]
	public string GameDate { get; set; } = "";

	[JsonProperty("createdUtcTicks")]
	public long CreatedUtcTicks { get; set; }

	[JsonProperty("isPlayerAuthored")]
	public bool IsPlayerAuthored { get; set; }

	[JsonProperty("isResponse")]
	public bool IsResponse { get; set; }

	[JsonProperty("requiresResponse")]
	public bool RequiresResponse { get; set; }

	[JsonProperty("isExternalResponseOnly")]
	public bool IsExternalResponseOnly { get; set; }

	[JsonProperty("isReminder")]
	public bool IsReminder { get; set; }

	[JsonProperty("isRelayTurn")]
	public bool IsRelayTurn { get; set; }

	[JsonProperty("automaticReplyDepth")]
	public int AutomaticReplyDepth { get; set; }

	[JsonProperty("isRead")]
	public bool IsRead { get; set; }

	[JsonProperty("isNotified")]
	public bool IsNotified { get; set; }

	[JsonProperty("rumorNotified")]
	public bool RumorNotified { get; set; }

	[JsonProperty("formalNoticeShown")]
	public bool FormalNoticeShown { get; set; }

	[JsonProperty("isCompressed")]
	public bool IsCompressed { get; set; }

	[JsonProperty("isReadyForPublication")]
	public bool IsReadyForPublication { get; set; }

	[JsonProperty("roundParticipation")]
	public string RoundParticipation { get; set; } = "continue";

	[JsonProperty("roundStatus")]
	public string RoundStatus { get; set; } = "continue";

	[JsonProperty("madeDiplomaticProgress")]
	public bool MadeDiplomaticProgress { get; set; }

	[JsonProperty("internationalReputationEvaluationDelta")]
	public int InternationalReputationEvaluationDelta { get; set; }

	[JsonProperty("internationalReputationEvaluationReason")]
	public string InternationalReputationEvaluationReason { get; set; } = "";

	[JsonProperty("internationalReputationEvaluationSource")]
	public string InternationalReputationEvaluationSource { get; set; } = "";

	[JsonProperty("internationalReputationSettled")]
	public bool InternationalReputationSettled { get; set; }

	[JsonProperty("diplomaticStandingChanges")]
	public List<WorldDiplomacyStandingChange> DiplomaticStandingChanges { get; set; } = new List<WorldDiplomacyStandingChange>();

	[JsonProperty("roundProgressHandled")]
	public bool RoundProgressHandled { get; set; }

	[JsonProperty("roundAccountingHandled")]
	public bool RoundAccountingHandled { get; set; }

	[JsonProperty("hasEmbeddedRoundPlan")]
	public bool HasEmbeddedRoundPlan { get; set; }

	[JsonProperty("plannedRoundTopic")]
	public string PlannedRoundTopic { get; set; } = "";

	[JsonProperty("plannedKingdomIds")]
	public List<string> PlannedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("resultSettlementSlotId")]
	public string ResultSettlementSlotId { get; set; } = "";

	[JsonProperty("isAutonomousNoActionDeclaration")]
	public bool IsAutonomousNoActionDeclaration { get; set; }

	[JsonProperty("isRoundResponseNoActionDeclaration")]
	public bool IsRoundResponseNoActionDeclaration { get; set; }

	[JsonProperty("isWarResponseNoActionDeclaration")]
	public bool IsWarResponseNoActionDeclaration { get; set; }
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
}

public sealed class WorldDiplomacyExchange
{
	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("initiatorKingdomId")]
	public string InitiatorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("responseDocumentId")]
	public string ResponseDocumentId { get; set; } = "";

	[JsonProperty("pendingAction")]
	public string PendingAction { get; set; } = "";

	[JsonProperty("pendingPeaceTerms")]
	public WorldDiplomacyPeaceTerms PendingPeaceTerms { get; set; }

	[JsonProperty("negotiationRevision")]
	public int NegotiationRevision { get; set; }

	[JsonProperty("state")]
	public string State { get; set; } = "";

	[JsonProperty("stateBeforeSuspension")]
	public string StateBeforeSuspension { get; set; } = "";

	[JsonProperty("startedDay")]
	public int StartedDay { get; set; }

	[JsonProperty("responseDueDay")]
	public int ResponseDueDay { get; set; }

	[JsonProperty("closeDueDay")]
	public int CloseDueDay { get; set; }

	[JsonProperty("suspendedDay")]
	public int SuspendedDay { get; set; }

	[JsonProperty("completedDay")]
	public int CompletedDay { get; set; }

	[JsonProperty("closeReason")]
	public string CloseReason { get; set; } = "";

	[JsonProperty("isForced")]
	public bool IsForced { get; set; }

	[JsonProperty("isPlayerInsertion")]
	public bool IsPlayerInsertion { get; set; }

	[JsonProperty("reminderSent")]
	public bool ReminderSent { get; set; }
}

public sealed class WorldDiplomacyJob
{
	[JsonProperty("historyThroughSequence")]
	public long HistoryThroughSequence { get; set; }

	[JsonProperty("historyRevision")]
	public long HistoryRevision { get; set; }

	[JsonProperty("historyPrefixHash")]
	public string HistoryPrefixHash { get; set; } = "";

	[JsonProperty("historyEstimatedTokens")]
	public long HistoryEstimatedTokens { get; set; }

	[JsonProperty("historySnapshotThroughSequence")]
	public long HistorySnapshotThroughSequence { get; set; }

	[JsonProperty("historySnapshotHash")]
	public string HistorySnapshotHash { get; set; } = "";

	[JsonProperty("roundId")]
	public string RoundId { get; set; } = "";

	[JsonProperty("candidateKingdomIds")]
	public List<string> CandidateKingdomIds { get; set; } = new List<string>();

	[JsonProperty("triggerDocumentIds")]
	public List<string> TriggerDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatDocumentIds")]
	public List<string> PresentedThreatDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedThreatFollowThroughDocumentIds")]
	public List<string> PresentedThreatFollowThroughDocumentIds { get; set; } = new List<string>();

	[JsonProperty("presentedLegalActionSignature")]
	public string PresentedLegalActionSignature { get; set; } = "";

	[JsonProperty("resultSettlementSlotId")]
	public string ResultSettlementSlotId { get; set; } = "";

	[JsonProperty("allowAutonomousNoAction")]
	public bool AllowAutonomousNoAction { get; set; }

	[JsonProperty("jobId")]
	public string JobId { get; set; } = "";

	[JsonProperty("kind")]
	public string Kind { get; set; } = "";

	[JsonProperty("priority")]
	public int Priority { get; set; }

	[JsonProperty("createdDay")]
	public int CreatedDay { get; set; }

	[JsonProperty("exchangeId")]
	public string ExchangeId { get; set; } = "";

	[JsonProperty("documentId")]
	public string DocumentId { get; set; } = "";

	[JsonProperty("sourceDocumentId")]
	public string SourceDocumentId { get; set; } = "";

	[JsonProperty("authorKingdomId")]
	public string AuthorKingdomId { get; set; } = "";

	[JsonProperty("targetKingdomId")]
	public string TargetKingdomId { get; set; } = "";

	[JsonProperty("forcedIntent")]
	public string ForcedIntent { get; set; } = "";

	[JsonProperty("isResponse")]
	public bool IsResponse { get; set; }

	[JsonProperty("isExternalResponseOnly")]
	public bool IsExternalResponseOnly { get; set; }

	[JsonProperty("isReminder")]
	public bool IsReminder { get; set; }

	[JsonProperty("isRelayTurn")]
	public bool IsRelayTurn { get; set; }

	[JsonProperty("allowUntargeted")]
	public bool AllowUntargeted { get; set; }

	[JsonProperty("previousKingdomId")]
	public string PreviousKingdomId { get; set; } = "";

	[JsonProperty("wasAtWarWhenQueued")]
	public bool WasAtWarWhenQueued { get; set; }

	[JsonProperty("semanticRepairAttempts")]
	public int SemanticRepairAttempts { get; set; }

	[JsonProperty("isRunning")]
	public bool IsRunning { get; set; }

	[JsonProperty("systemPrompt")]
	public string SystemPrompt { get; set; } = "";

	[JsonProperty("userPrompt")]
	public string UserPrompt { get; set; } = "";

	[JsonProperty("llmMessages")]
	public List<WorldDiplomacyLlmMessage> LlmMessages { get; set; } = new List<WorldDiplomacyLlmMessage>();

	[JsonProperty("profiledKingdomId")]
	public string ProfiledKingdomId { get; set; } = "";

	[JsonProperty("strategicProfileKingdomId")]
	public string StrategicProfileKingdomId { get; set; } = "";

	[JsonProperty("cacheAffinityKey")]
	public string CacheAffinityKey { get; set; } = "";

	[JsonProperty("maxTokens")]
	public int MaxTokens { get; set; }

	[JsonProperty("compressionYear")]
	public int CompressionYear { get; set; }

	[JsonProperty("compressionDocumentIds")]
	public List<string> CompressionDocumentIds { get; set; } = new List<string>();

	[JsonProperty("compressionBatchId")]
	public string CompressionBatchId { get; set; } = "";

	[JsonProperty("compressionRoundIds")]
	public List<string> CompressionRoundIds { get; set; } = new List<string>();

	[JsonProperty("compressionTokenCount")]
	public long CompressionTokenCount { get; set; }

	[JsonProperty("compressionThroughSequence")]
	public long CompressionThroughSequence { get; set; }

	[JsonProperty("compressionTargetTokens")]
	public int CompressionTargetTokens { get; set; }

	[JsonProperty("compressionOverallTargetTokens")]
	public int CompressionOverallTargetTokens { get; set; }
}

public sealed class WorldDiplomacyCanonicalHistoryState
{
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

internal sealed class WorldDiplomacyMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public string DocumentId { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public WorldDiplomacyMapNotification(string documentId, string title, string description)
		: base(new TextObject(string.IsNullOrWhiteSpace(description) ? "点击查看外交宣言。" : description))
	{
		DocumentId = (documentId ?? "").Trim();
		_titleText = new TextObject(string.IsNullOrWhiteSpace(title) ? "新的外交宣言" : title);
	}

	public override bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(DocumentId);
	}
}

internal sealed class WorldDiplomacyMapNotificationItemVM : MapNotificationItemBaseVM
{
	public WorldDiplomacyMapNotificationItemVM(WorldDiplomacyMapNotification data)
		: base(data)
	{
		WorldDiplomacyUiSprites.EnsureInstalledForNotificationUi();
		NotificationIdentifier = WorldDiplomacyUiSprites.NotificationIdentifier;
		_onInspect = delegate
		{
			if (WorldDiplomacyBehavior.Instance?.OpenDocumentFromNotification(data.DocumentId) == true)
			{
				ExecuteRemove();
			}
		};
	}
}

internal static class WorldDiplomacyUiSprites
{
	public const string NotificationIdentifier = "af_world_diplomacy_notice";
	private const string Source = "WorldDiplomacyUiSprites";
	private const string Category = "af_world_diplomacy";
	private const string FileName = "af_world_diplomacy_notice_v2.png";
	private const string BrushName = "Map.Notification.Type.Circle.Image";
	private static readonly string SpriteName = Category + "\\" + NotificationIdentifier;
	private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static BannerlordUiSprite _runtimeSprite;
	private static bool _patched;
	private static bool _brushApplied;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		Harmony patcher = harmony ?? new Harmony("AnimusForge.world.diplomacy.ui.sprites");
		TryPatch(patcher, "RefreshSpriteData", nameof(RefreshSpriteDataPostfix));
		TryPatch(patcher, "RefreshBrushFactory", nameof(RefreshBrushFactoryPostfix));
		EnsureInstalledForNotificationUi();
	}

	public static void EnsureInstalledForNotificationUi()
	{
		TryInstallRuntimeSprite();
		TryApplyBrushLayerSprite();
	}

	public static void RefreshSpriteDataPostfix()
	{
		TryInstallRuntimeSprite();
	}

	public static void RefreshBrushFactoryPostfix()
	{
		TryInstallRuntimeSprite();
		TryApplyBrushLayerSprite();
	}

	private static void TryPatch(Harmony harmony, string targetName, string postfixName)
	{
		try
		{
			MethodInfo target = AccessTools.Method(typeof(UIResourceManager), targetName);
			if (target != null)
			{
				harmony.Patch(target, postfix: new HarmonyMethod(typeof(WorldDiplomacyUiSprites), postfixName));
			}
		}
		catch (Exception ex)
		{
			LogOnce("patch-" + targetName, ex.Message);
		}
	}

	private static void TryInstallRuntimeSprite()
	{
		try
		{
			if (UIResourceManager.SpriteData == null)
			{
				return;
			}
			if (UIResourceManager.SpriteData.Sprites.TryGetValue(SpriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
			{
				_runtimeSprite = existing;
				return;
			}
			string filePath = Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "GUI", "SpriteParts", Category, FileName);
			if (!File.Exists(filePath))
			{
				LogOnce("file-missing", "file missing: " + filePath);
				return;
			}
			BannerlordEngineTexture engineTexture = null;
			try
			{
				engineTexture = BannerlordEngineTexture.CreateFromMemory(File.ReadAllBytes(filePath));
			}
			catch
			{
			}
			engineTexture ??= BannerlordEngineTexture.LoadTextureFromPath(Path.GetFileName(filePath), Path.GetDirectoryName(filePath));
			if (engineTexture == null)
			{
				LogOnce("texture-null", "native texture loader returned null");
				return;
			}
			try
			{
				engineTexture.Name = SpriteName;
				engineTexture.SetTextureAsAlwaysValid();
				engineTexture.PreloadTexture(true);
			}
			catch
			{
			}
			int width = engineTexture.Width > 0 ? engineTexture.Width : 2048;
			int height = engineTexture.Height > 0 ? engineTexture.Height : 2048;
			BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
			_runtimeSprite = new RuntimeTextureSprite(SpriteName, uiTexture, width, height);
			UIResourceManager.SpriteData.Sprites[SpriteName] = _runtimeSprite;
		}
		catch (Exception ex)
		{
			LogOnce("install", ex.Message);
		}
	}

	private static void TryApplyBrushLayerSprite()
	{
		try
		{
			Brush brush = UIResourceManager.BrushFactory?.GetBrush(BrushName);
			if (brush == null || _runtimeSprite == null)
			{
				return;
			}
			if (AnimusForgeRuntimeBrushSpriteGuard.TryApplyLayerStyle(brush, NotificationIdentifier, _runtimeSprite, out string failureReason))
			{
				Style style = brush.GetStyle(NotificationIdentifier);
				StyleLayer styleLayer = style?.GetLayer(NotificationIdentifier);
				if (styleLayer != null)
				{
					styleLayer.Sprite = _runtimeSprite;
					styleLayer.Color = TaleWorlds.Library.Color.White;
					styleLayer.ColorFactor = 1f;
					styleLayer.AlphaFactor = 1f;
					styleLayer.HueFactor = 0f;
					styleLayer.SaturationFactor = 0f;
					styleLayer.ValueFactor = 0f;
					styleLayer.ImageFitType = ImageFit.ImageFitTypes.Cover;
					styleLayer.ImageFitHorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center;
					styleLayer.ImageFitVerticalAlignment = ImageFit.ImageVerticalAlignments.Center;
				}
				_brushApplied = true;
			}
			else if (!_brushApplied)
			{
				LogOnce("brush", failureReason);
			}
		}
		catch (Exception ex)
		{
			LogOnce("brush-exception", ex.Message);
		}
	}

	private static void LogOnce(string key, string message)
	{
		if (LoggedFailures.Add(key))
		{
			Logger.Log(Source, "[AF-WORLD-DIPLOMACY-UI] " + message);
		}
	}

	private sealed class RuntimeTextureSprite : BannerlordUiSprite
	{
		private readonly BannerlordUiTexture _texture;

		public RuntimeTextureSprite(string name, BannerlordUiTexture texture, int width, int height)
			: base(name, width, height, TaleWorlds.TwoDimension.SpriteNinePatchParameters.Empty)
		{
			_texture = texture;
		}

		public override BannerlordUiTexture Texture => _texture;

		public override Vec2 GetMinUvs()
		{
			return Vec2.Zero;
		}

		public override Vec2 GetMaxUvs()
		{
			return Vec2.One;
		}
	}
}

public sealed class WorldDiplomacyComposePopup
{
	private enum PendingCloseAction
	{
		None,
		Submit,
		Cancel
	}

	private static WorldDiplomacyComposePopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly WorldDiplomacyComposePopupVM _dataSource;
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private PendingCloseAction _pendingAction;
	private string _pendingBody = "";
	private bool _closed;

	public static bool IsOpen => _activePopup != null && !_activePopup._closed;

	private WorldDiplomacyComposePopup(ScreenBase screen, string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_dataSource = new WorldDiplomacyComposePopupVM(title, subtitle, hint, HandleSubmit, HandleCancel);
		_layer = new GauntletLayer("WorldDiplomacyComposePopup", 4050, false);
	}

	public static bool Show(string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		ScreenBase screen = ScreenManager.TopScreen;
		if (screen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			WorldDiplomacyComposePopup popup = new WorldDiplomacyComposePopup(screen, title, subtitle, hint, onSubmit, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("WorldDiplomacyComposePopup", "[ERROR] " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		WorldDiplomacyComposePopup popup = _activePopup;
		if (popup == null || popup._closed)
		{
			return;
		}
		try
		{
			if (popup._layer?.Input != null && (popup._layer.Input.IsHotKeyReleased("Exit") || popup._layer.Input.IsKeyReleased(InputKey.Escape)))
			{
				popup.HandleCancel();
			}
		}
		catch
		{
		}
		popup.ProcessPendingAction();
	}

	private void Open()
	{
		_layer.LoadMovie("WorldDiplomacyComposePopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
	}

	private void HandleSubmit(string body)
	{
		if (_pendingAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingBody = body ?? "";
		_pendingAction = PendingCloseAction.Submit;
	}

	private void HandleCancel()
	{
		if (_pendingAction == PendingCloseAction.None)
		{
			_pendingAction = PendingCloseAction.Cancel;
		}
	}

	private void ProcessPendingAction()
	{
		if (_pendingAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingAction;
		string body = _pendingBody;
		_pendingAction = PendingCloseAction.None;
		_pendingBody = "";
		Close(silent: true);
		if (action == PendingCloseAction.Submit)
		{
			_onSubmit?.Invoke(body);
		}
		else
		{
			_onCancel?.Invoke();
		}
	}

	private void Close(bool silent)
	{
		if (_closed)
		{
			return;
		}
		_closed = true;
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("WorldDiplomacyComposePopup", "[WARN] " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}

public sealed class WorldDiplomacyComposePopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private string _titleText;
	private string _subtitleText;
	private string _hintText;
	private string _bodyText;
	private bool _canPublish;

	public WorldDiplomacyComposePopupVM(string title, string subtitle, string hint, Action<string> onSubmit, Action onCancel)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		TitleText = string.IsNullOrWhiteSpace(title) ? "撰写外交宣言" : title;
		SubtitleText = subtitle ?? "";
		HintText = hint ?? "";
		BodyText = "";
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string HintText
	{
		get => _hintText;
		set
		{
			if (value != _hintText)
			{
				_hintText = value;
				OnPropertyChangedWithValue(value, nameof(HintText));
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get => _bodyText;
		set
		{
			string clean = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, 6000);
			if (clean != _bodyText)
			{
				_bodyText = clean;
				OnPropertyChangedWithValue(clean, nameof(BodyText));
				CanPublish = !string.IsNullOrWhiteSpace(clean);
			}
		}
	}

	[DataSourceProperty]
	public bool CanPublish
	{
		get => _canPublish;
		private set
		{
			if (value != _canPublish)
			{
				_canPublish = value;
				OnPropertyChangedWithValue(value, nameof(CanPublish));
			}
		}
	}

	public void ExecutePublish()
	{
		if (CanPublish)
		{
			_onSubmit?.Invoke(BodyText);
		}
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}
}
