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
	private const int DecisionArchitectureVersion = 1;
	private const int HistoryMemorySchemaVersion = 4;
	private const int DiplomacyPromptContractVersion = 3;
	private const int RelaySchemaVersion = 20;
	private const string CanonicalHistoryCacheAffinityKey = "diplomacy-history:v4";
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
	private const float BorderDistanceMedianMultiplier = 3.5f;
	private const float MinimumBorderDistance = 24f;
	private const float MaximumBorderDistance = 72f;
	private static readonly string[] PeaceDomainPhrases = { "和平", "议和", "和谈", "停战", "休战" };
	private static readonly string[] AllianceDomainPhrases = { "同盟", "结盟", "盟约", "盟友" };
	private static readonly string[] TradeDomainPhrases = { "贸易", "通商", "商路", "商贸", "互市" };
	private static readonly string[] DiplomacyDomainPhrases = PeaceDomainPhrases.Concat(AllianceDomainPhrases).Concat(TradeDomainPhrases).ToArray();
	private static readonly string[] ProposalActionPhrases = { "提议", "建议", "倡议", "邀请", "请求" };
	private static readonly string[] AcceptanceActionPhrases = { "接受", "同意", "应允", "批准", "确认接受", "愿按" };
	private static readonly string[] RejectionActionPhrases = { "拒绝", "不接受", "不可接受", "无法接受", "不同意", "不能同意", "驳回" };
	private static readonly string[] WarActionPhrases = { "宣战", "宣布战争", "进入战争状态", "决定开战", "和平就此结束", "和平已经结束", "和平正式终结" };
	private static readonly string[] BreakAllianceActionPhrases = { "解除同盟", "终止同盟", "结束同盟", "退出同盟", "废除盟约", "终止盟约", "解除盟约" };
	private static readonly string[] CancelTradeActionPhrases = { "终止贸易协定", "取消贸易协定", "结束贸易协定", "废除贸易协定", "中止贸易协定", "断绝贸易" };
	private static readonly Regex InternalMetricWithNumberRegex = new Regex(
		@"(?:战争进展|战争进度|战局进度|议和开放度|和平开放度|劣势评分|优势评分|战争压力(?:值|分数)?|统治者关系(?:值|点数)?|关系点数|好感度|战力值|总战力)[^。\r\n]{0,12}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+分)|(?:领先|落后|高出|低于)[^。\r\n]{0,8}(?:[-+]?\d+(?:\.\d+)?|[零〇一二三四五六七八九十百千万]+)分",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex InternalMetricTermRegex = new Regex(
		@"(?:议和|和平)开放度|(?:优势|劣势)评分|数值阈值|(?:系统|模型|AI|程序)(?:判定|评分|数据|数值|字段)|游戏(?:机制|数据|数值)",
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
		InitializeSchedule();
		ResetTransientRuntime("game-loaded");
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		NormalizeStorage(allowWorldValidation: true);
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
			if (!_disabledStateApplied) HandleDisabledState();
			return;
		}
		_disabledStateApplied = false;
		RemoveQueuedNativeDiplomacyDecisions();
		_nativeDiplomacyDecisionQueueSanitized = true;
		DecayWarPressure();
		RefreshPolicyDiplomacySignals();
		RetryDeferredDocumentPropagation();
		RetryDeferredCanonicalHistoryEntries();
		ProcessPropagationArrivals();
		ProcessRelayArrivals();
		RetryDeferredRoundProgress();
		ProcessRoundLifecycle();
		TryScheduleTokenCompression();
		TrySchedulePolicyTriggeredRound();
		TryScheduleNormalRound();
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
			"待解析的外交宣言",
			cleanBody,
			"player",
			isPlayerAuthored: true,
			isResponse: false,
			exchangeId: round?.RoundId ?? "");
		document.RoundId = round?.RoundId ?? "";
		AddDocument(document);
		if (round != null)
		{
			round.RootDocumentId = FirstNonEmpty(round.RootDocumentId, document.DocumentId);
			round.LastActivityDay = CurrentDay();
			EnsureRoundParticipant(round, playerKingdom.StringId, "active", mandatoryReply: false);
		}
		EnqueueAnalysisJob(document, priority: 100);
		InformationManager.DisplayMessage(new InformationMessage("外交宣言已经公开发布，将从王庭向各地传播；系统会在后台解析其外交含义。"));
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
		if (_storage.Jobs.Count > 0 || _llmRequestRunning || !TryConsumeAiDocumentBudget())
		{
			return;
		}

		Kingdom author = IsPlayerKingdom(affectedRepresentative) ? issuerRepresentative : affectedRepresentative;
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
		if (!round.ExternalSignalKeys.Contains(signal.SignalKey, StringComparer.OrdinalIgnoreCase))
		{
			round.ExternalSignalKeys.Add(signal.SignalKey);
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
		int index = Math.Abs(_storage.RotationIndex) % initiators.Count;
		Kingdom initiator = initiators[index];
		_storage.RotationIndex = (index + 1) % initiators.Count;
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
		int scheduledDay = -1)
	{
		if (author == null || (target == null && !allowUntargeted))
		{
			CompleteExchange(exchange?.ExchangeId, "invalid_generation_parties");
			return;
		}
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
		if (!HasIndependentWorldDiplomacyAuthority(author))
		{
			Log("generation skipped for diplomatically controlled vassal author=" + (author.StringId ?? "")
				+ " round=" + (owningRound?.RoundId ?? ""));
			CompleteExchange(exchange?.ExchangeId, "controlled_vassal_has_no_diplomatic_authority");
			if (isRelayTurn && owningRound != null)
			{
				owningRound.RelayWaiting = false;
				AdvanceRelay(owningRound);
			}
			return;
		}
		bool playerPriorityResponse = externalResponseOnly && sourceDocument?.IsPlayerAuthored == true;
		if (owningRound != null)
		{
			if (!playerPriorityResponse
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
			roundPlanCandidates = Kingdom.All
				.Where(x => x != null && !x.IsEliminated && x != author && HasIndependentWorldDiplomacyAuthority(x))
				.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.StringId)
				.ToList();
		}
		string dynamicPrompt = isRelayTurn
			? BuildRelayConversationTurnPrompt(owningRound, author, target,
				prioritySource: sourceDocument, priorityResponseOnly: externalResponseOnly)
			: BuildGenerationPrompt(author, target, exchange, isResponse, sourceDocument, isReminder, roundId, allowUntargeted, roundPlanCandidates);
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
			CandidateKingdomIds = roundPlanCandidates,
			WasAtWarWhenQueued = target != null && FactionManager.IsAtWarAgainstFaction(author, target),
			SystemPrompt = systemPrompt,
			UserPrompt = userPrompt,
			CacheAffinityKey = CanonicalHistoryCacheAffinityKey,
			ProfiledKingdomId = "",
			MaxTokens = GenerationMaxTokens
		};
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

		string normalizedAuthorId = (authorId ?? "").Trim();
		if (normalizedAuthorId.Length == 0) return -1;
		string participantAnchor = "-- " + normalizedAuthorId + "=";
		int participantIndex = prompt.IndexOf(participantAnchor, StringComparison.OrdinalIgnoreCase);
		if (participantIndex < 0) return -1;
		int participantEnd = prompt.IndexOf("\n-- ", participantIndex + participantAnchor.Length, StringComparison.Ordinal);
		if (participantEnd < 0) participantEnd = prompt.Length;
		int participantInstitutionIndex = prompt.IndexOf("\n国家制度与礼制声音=", participantIndex, StringComparison.Ordinal);
		if (participantInstitutionIndex < 0 || participantInstitutionIndex >= participantEnd) return -1;
		int participantFamilyIndex = prompt.IndexOf("\n王室与亲属=", participantInstitutionIndex, StringComparison.Ordinal);
		return participantFamilyIndex >= 0 && participantFamilyIndex < participantEnd
			? participantFamilyIndex + 1
			: participantEnd;
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
		if (job == null || job.SemanticRepairAttempts <= 0 || messages == null || messages.Count < 5
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
		string modePrompt = job.SemanticRepairAttempts > 0 && job.LlmMessages?.Count >= 3
			? job.LlmMessages[2]?.Content ?? ""
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
			if (!result.Success)
			{
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
		Kingdom author = ResolveKingdom(job.AuthorKingdomId);
		Kingdom fallbackTarget = ResolveKingdom(job.TargetKingdomId);
		if (author == null)
		{
			CompleteExchange(job.ExchangeId, "generated_party_missing");
			return;
		}
		PruneInvalidOffers(ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId)));
		JObject json = ParseJsonObject(raw);
		if (TryGetGeneratedIntentLegalityViolation(job, json, author, fallbackTarget, out Kingdom generatedTarget, out string legalityReason))
		{
			Log("generated declaration rejected before publication job=" + job.JobId
				+ " intent=" + NormalizeIntent(ReadString(json, "author_intent.intent", "intent"))
				+ " author=" + author.StringId
				+ " target=" + (generatedTarget?.StringId ?? fallbackTarget?.StringId ?? "")
				+ " reason=" + legalityReason);
			if (job.SemanticRepairAttempts < 1)
			{
				EnqueueGeneratedDeclarationRepair(job, raw, author, generatedTarget ?? fallbackTarget, legalityReason);
				return;
			}
			AbandonRejectedGeneration(job, author, generatedTarget ?? fallbackTarget, legalityReason);
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
			AbandonRejectedGeneration(job, author, target, "empty_public_document");
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
		document.IsExternalResponseOnly = job.IsExternalResponseOnly;
		document.IsReminder = job.IsReminder;
		document.IsRelayTurn = job.IsRelayTurn;
		document.RoundParticipation = NormalizeToken(ReadString(json, "round_participation"));
		if (document.RoundParticipation != "withdraw") document.RoundParticipation = "continue";
		document.RoundStatus = NormalizeToken(ReadString(json, "round_status"));
		if (document.RoundStatus != "resolved" && document.RoundStatus != "deadlocked") document.RoundStatus = "continue";
		document.MadeDiplomaticProgress = ReadBool(json, "made_progress");
		document.HasEmbeddedRoundPlan = IsAutonomousOpeningJob(job);
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
		if (!TryApplyGeneratedSemanticEnvelope(document, json, author, target, job.AllowUntargeted, job.IsRelayTurn))
		{
			AbandonRejectedGeneration(job, author, target, "generated_semantic_envelope_incomplete");
			return;
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
		if (job == null || json == null || author == null)
		{
			reason = "semantic_envelope_incomplete";
			return true;
		}
		if (!(json["author_intent"] is JObject)
			|| !(json["addressed_kingdom_ids"] is JArray)
			|| !(json["mentioned_kingdom_ids"] is JArray)
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
		string commitment = NormalizeCommitment(ReadString(json, "author_intent.commitment", "commitment"));
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
				|| !(roundPlan["selected_kingdom_ids"] is JArray)
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
		WorldDiplomacyRound owningRound = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (job.IsRelayTurn
			&& ((generatedTarget != null && !RoundRouteContainsKingdom(owningRound, generatedTarget.StringId))
				|| addressedIds.Any(id => !RoundRouteContainsKingdom(owningRound, id))))
		{
			reason = "kingdom_not_in_relay_route";
			return true;
		}
		bool targetRequired = IsImmediateIntent(intent) || IsProposalIntent(intent)
			|| !string.IsNullOrWhiteSpace(ResponseIntentToProposalIntent(intent))
			|| intent is "ultimatum" or "apology" or "concession";
		if (targetRequired && (generatedTarget == null || string.IsNullOrWhiteSpace(targetId)))
		{
			reason = "diplomatic_action_has_no_target";
			return true;
		}
		if (!CommitmentMatchesIntent(intent, commitment))
		{
			reason = "intent_commitment_mismatch";
			return true;
		}
		if (TryGetDiplomaticStateViolation(intent, author, generatedTarget, out reason)) return true;

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
				WorldDiplomacyPeaceTerms offeredTerms = ResolveDocument(openOfferDocumentId)?.PeaceTerms;
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

	private bool TryGetPlayerVisibleIntentViolation(string intent, string visibleText, Kingdom author, Kingdom target, out string reason)
	{
		reason = "";
		string normalized = NormalizeIntent(intent);
		string text = visibleText ?? "";
		bool proposesPeace = HasAffirmativeDiplomacyDomainAction(text, PeaceDomainPhrases, ProposalActionPhrases);
		bool rejectsPeace = HasAffirmativeDiplomacyDomainAction(text, PeaceDomainPhrases, RejectionActionPhrases);
		bool acceptsPeace = HasAffirmativeDiplomacyDomainAction(text, PeaceDomainPhrases, AcceptanceActionPhrases)
			&& !rejectsPeace && !LooksLikeExplicitCounterProposal(text);
		bool proposesAlliance = HasAffirmativeDiplomacyDomainAction(text, AllianceDomainPhrases, ProposalActionPhrases);
		bool rejectsAlliance = HasAffirmativeDiplomacyDomainAction(text, AllianceDomainPhrases, RejectionActionPhrases);
		bool acceptsAlliance = HasAffirmativeDiplomacyDomainAction(text, AllianceDomainPhrases, AcceptanceActionPhrases)
			&& !rejectsAlliance && !LooksLikeExplicitCounterProposal(text);
		bool proposesTrade = HasAffirmativeDiplomacyDomainAction(text, TradeDomainPhrases, ProposalActionPhrases);
		bool rejectsTrade = HasAffirmativeDiplomacyDomainAction(text, TradeDomainPhrases, RejectionActionPhrases);
		bool acceptsTrade = HasAffirmativeDiplomacyDomainAction(text, TradeDomainPhrases, AcceptanceActionPhrases)
			&& !rejectsTrade && !LooksLikeExplicitCounterProposal(text);
		bool hasPlainWarDeclaration = HasAffirmativeDiplomacyPhrase(text, WarActionPhrases)
			&& !HasConditionalDiplomacyPhrase(text, WarActionPhrases);
		bool breaksAlliance = HasAffirmativeDiplomacyPhrase(text, BreakAllianceActionPhrases)
			&& !HasConditionalDiplomacyPhrase(text, BreakAllianceActionPhrases);
		bool cancelsTrade = HasAffirmativeDiplomacyPhrase(text, CancelTradeActionPhrases)
			&& !HasConditionalDiplomacyPhrase(text, CancelTradeActionPhrases);
		bool visibleUltimatum = HasAffirmativeDiplomacyPhrase(text, "最后通牒", "最后期限", "否则将", "若不");
		bool visibleApology = HasAffirmativeDiplomacyPhrase(text, "道歉", "致歉", "歉意", "赔罪");
		bool visibleConcession = HasAffirmativeDiplomacyPhrase(text, "让步", "退让", "撤回", "放弃要求", "接受贵国条件");
		bool visible = normalized switch
		{
			"declare_war" => hasPlainWarDeclaration,
			"break_alliance" => breaksAlliance,
			"cancel_trade" => cancelsTrade,
			"propose_peace" => proposesPeace,
			"accept_peace" => acceptsPeace,
			"reject_peace" => rejectsPeace && !acceptsPeace,
			"propose_alliance" => proposesAlliance,
			"accept_alliance" => acceptsAlliance,
			"reject_alliance" => rejectsAlliance && !acceptsAlliance,
			"propose_trade" => proposesTrade,
			"accept_trade" => acceptsTrade,
			"reject_trade" => rejectsTrade && !acceptsTrade,
			"ultimatum" => visibleUltimatum,
			"apology" => visibleApology,
			"concession" => visibleConcession,
			_ => true
		};
		if (!visible)
		{
			reason = "visible_intent_mismatch:" + normalized;
			return true;
		}
		if (RequiresPublicActionTarget(normalized) && target != null
			&& !HasVisibleIntentDirectedAtTarget(normalized, text, author, target))
		{
			reason = "visible_action_target_mismatch:" + normalized + ":" + (target.StringId ?? "");
			return true;
		}
		List<string> visibleActionIntents = new List<string>();
		if (hasPlainWarDeclaration) visibleActionIntents.Add("declare_war");
		if (breaksAlliance) visibleActionIntents.Add("break_alliance");
		if (cancelsTrade) visibleActionIntents.Add("cancel_trade");
		if (proposesPeace) visibleActionIntents.Add("propose_peace");
		if (acceptsPeace) visibleActionIntents.Add("accept_peace");
		if (rejectsPeace && !acceptsPeace) visibleActionIntents.Add("reject_peace");
		if (proposesAlliance) visibleActionIntents.Add("propose_alliance");
		if (acceptsAlliance) visibleActionIntents.Add("accept_alliance");
		if (rejectsAlliance && !acceptsAlliance) visibleActionIntents.Add("reject_alliance");
		if (proposesTrade) visibleActionIntents.Add("propose_trade");
		if (acceptsTrade) visibleActionIntents.Add("accept_trade");
		if (rejectsTrade && !acceptsTrade) visibleActionIntents.Add("reject_trade");
		string conflictingVisibleIntent = visibleActionIntents
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault(x => !string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(conflictingVisibleIntent))
		{
			reason = "visible_action_conflicts_with_intent:" + conflictingVisibleIntent + ":" + normalized;
			return true;
		}

		return false;
	}

	private bool TryGetPublicPeaceTermsDisclosureViolation(string intent, string visibleText, JObject json, Kingdom author, Kingdom target, out string reason)
	{
		reason = "";
		string normalized = NormalizeIntent(intent);
		string text = visibleText ?? "";
		if (normalized != "propose_peace" || json?.SelectToken("peace_terms") is not JToken terms) return false;
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

	private static bool HasAffirmativeDiplomacyPhrase(string text, params string[] phrases)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;
		foreach (string clause in Regex.Split(text, @"[\r\n。！？；]+"))
		{
			if (string.IsNullOrWhiteSpace(clause)) continue;
			foreach (string phrase in phrases ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(phrase)) continue;
				int searchFrom = 0;
				while (searchFrom < clause.Length)
				{
					int index = clause.IndexOf(phrase, searchFrom, StringComparison.OrdinalIgnoreCase);
					if (index < 0) break;
					if (IsAffirmativeDiplomacyPhraseOccurrence(clause, index, phrase)) return true;
					searchFrom = index + phrase.Length;
				}
			}
		}
		return false;
	}

	private static bool HasAffirmativeDiplomacyDomainAction(string text, string[] domainPhrases, params string[] actionPhrases)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;
		foreach (string clause in Regex.Split(text, @"[\r\n。！？；]+"))
		{
			if (!ContainsAny(clause, domainPhrases)) continue;
			foreach (string actionPhrase in actionPhrases ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(actionPhrase)) continue;
				int searchFrom = 0;
				while (searchFrom < clause.Length)
				{
					int actionIndex = clause.IndexOf(actionPhrase, searchFrom, StringComparison.OrdinalIgnoreCase);
					if (actionIndex < 0) break;
					if (IsAffirmativeDiplomacyPhraseOccurrence(clause, actionIndex, actionPhrase))
					{
						int ownDomainDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, domainPhrases);
						int anyDomainDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, DiplomacyDomainPhrases);
						if (ownDomainDistance <= 18 && ownDomainDistance == anyDomainDistance) return true;
					}
					searchFrom = actionIndex + actionPhrase.Length;
				}
			}
		}
		return false;
	}

	private static bool HasConditionalDiplomacyPhrase(string text, params string[] phrases)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;
		foreach (string clause in Regex.Split(text, @"[\r\n。！？；]+"))
		{
			foreach (string phrase in phrases ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(phrase)) continue;
				int searchFrom = 0;
				while (searchFrom < clause.Length)
				{
					int index = clause.IndexOf(phrase, searchFrom, StringComparison.OrdinalIgnoreCase);
					if (index < 0) break;
					int beforeStart = Math.Max(0, index - 64);
					string before = clause.Substring(beforeStart, index - beforeStart);
					int afterStart = index + phrase.Length;
					string after = clause.Substring(afterStart, Math.Min(48, clause.Length - afterStart));
					if (Regex.IsMatch(before, @"(?:若|如果|倘若|除非|否则|一旦|只有|唯有|只要|前提是|条件是|条件为|须待|必须等到)[^。！？；\n]{0,56}$", RegexOptions.CultureInvariant)
						|| Regex.IsMatch(before, @"(?:(?:将(?!军|领|士|帅|官|校|门|才|相|近|来))|(?:(?:我国|本国|我方|本朝|本王国|本廷|我们|我王|本王|朕|寡人|王廷|政府|国家|王国|帝国|汗国|公国|共和国|联盟|联邦|部落|政权|朝廷)(?:最终|仍然|仍|也|必然|必定|可能|或许|恐怕|当然|大概|很可能)?会)|(?:(?:^|[，,：:])(?:届时|到时|此后|随后|未来|今后|之后)?会)|准备|打算|可能|或将|意欲|考虑|曾|此前|过去)[^。！？；\n]{0,56}$", RegexOptions.CultureInvariant)
						|| Regex.IsMatch(after, @"^[^。！？；\n]{0,40}(?:若|如果|倘若|除非|只有|唯有|只要|前提是|条件是|条件为|才会|方会|取决于|视.+而定)", RegexOptions.CultureInvariant)) return true;
					searchFrom = index + phrase.Length;
				}
			}
		}
		return false;
	}

	private static bool RequiresPublicActionTarget(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return IsImmediateIntent(normalized) || IsProposalIntent(normalized)
			|| !string.IsNullOrWhiteSpace(ResponseIntentToProposalIntent(normalized))
			|| normalized is "ultimatum" or "apology" or "concession";
	}

	private static bool HasVisibleIntentDirectedAtTarget(string intent, string text, Kingdom author, Kingdom target)
	{
		if (target == null || string.IsNullOrWhiteSpace(text)) return false;
		string normalized = NormalizeIntent(intent);
		string[] actionPhrases = normalized switch
		{
			"declare_war" => WarActionPhrases,
			"break_alliance" => BreakAllianceActionPhrases,
			"cancel_trade" => CancelTradeActionPhrases,
			"propose_peace" or "propose_alliance" or "propose_trade" => ProposalActionPhrases,
			"accept_peace" or "accept_alliance" or "accept_trade" => AcceptanceActionPhrases,
			"reject_peace" or "reject_alliance" or "reject_trade" => RejectionActionPhrases,
			"ultimatum" => new[] { "最后通牒", "最后期限", "否则将", "若不" },
			"apology" => new[] { "道歉", "致歉", "歉意", "赔罪" },
			"concession" => new[] { "让步", "退让", "撤回", "放弃要求", "接受贵国条件" },
			_ => Array.Empty<string>()
		};
		string[] domainPhrases = normalized.EndsWith("_peace", StringComparison.Ordinal) ? PeaceDomainPhrases
			: normalized.EndsWith("_alliance", StringComparison.Ordinal) ? AllianceDomainPhrases
			: normalized.EndsWith("_trade", StringComparison.Ordinal) ? TradeDomainPhrases
			: null;
		string[] targetReferences = new[]
		{
			target.StringId, KingdomName(target), RulerName(target), "贵国", "贵方", "你国", "你方", "对方", "双方", "两国", "彼此"
		}.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		string[] otherKingdomReferences = Kingdom.All.Where(x => x != null && x != author && x != target)
			.SelectMany(x => new[] { x.StringId, KingdomName(x), RulerName(x) })
			.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		foreach (string clause in Regex.Split(text, @"[\r\n。！？；]+"))
		{
			if (string.IsNullOrWhiteSpace(clause)) continue;
			foreach (string actionPhrase in actionPhrases)
			{
				int searchFrom = 0;
				while (searchFrom < clause.Length)
				{
					int actionIndex = clause.IndexOf(actionPhrase, searchFrom, StringComparison.OrdinalIgnoreCase);
					if (actionIndex < 0) break;
					if (IsAffirmativeDiplomacyPhraseOccurrence(clause, actionIndex, actionPhrase))
					{
						if (IsActionAttributedToTarget(clause, actionIndex, targetReferences))
						{
							searchFrom = actionIndex + actionPhrase.Length;
							continue;
						}
						if (IsActionAttributedToOtherKingdom(clause, actionIndex, author, target, targetReferences))
						{
							searchFrom = actionIndex + actionPhrase.Length;
							continue;
						}
						bool domainMatches = domainPhrases == null;
						if (domainPhrases != null)
						{
							int ownDomainDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, domainPhrases);
							int anyDomainDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, DiplomacyDomainPhrases);
							domainMatches = ownDomainDistance <= 18 && ownDomainDistance == anyDomainDistance;
						}
						if (domainMatches)
						{
							int targetDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, targetReferences);
							int otherDistance = DistanceToNearestPhrase(clause, actionIndex, actionPhrase.Length, otherKingdomReferences);
							if (targetDistance <= 48 && (otherDistance == int.MaxValue || targetDistance < otherDistance)) return true;
						}
					}
					searchFrom = actionIndex + actionPhrase.Length;
				}
			}
		}
		return false;
	}

	private static bool IsActionAttributedToTarget(string clause, int actionIndex, IEnumerable<string> targetReferences)
	{
		if (string.IsNullOrWhiteSpace(clause) || actionIndex <= 0) return false;
		string[] references = (targetReferences ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.OrderByDescending(x => x.Length)
			.Select(Regex.Escape).ToArray();
		if (references.Length == 0) return false;
		string targetPattern = "(?:" + string.Join("|", references) + ")";
		string before = clause.Substring(0, actionIndex);
		return Regex.IsMatch(before,
			@"(?:^|[^向对与])" + targetPattern + @"(?:已经|曾经|正式|公然|宣布|决定|要求)?[^，,：:]{0,2}$",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
			|| Regex.IsMatch(before,
				targetPattern + @"[^，,：:]{0,14}(?:向|对)(?:我国|本国|我方|本朝|本王国)[^，,：:]{0,6}$",
				RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	}

	private static bool IsActionAttributedToOtherKingdom(
		string clause,
		int actionIndex,
		Kingdom author,
		Kingdom target,
		IEnumerable<string> targetReferences)
	{
		if (string.IsNullOrWhiteSpace(clause) || actionIndex <= 0) return false;
		string before = clause.Substring(0, actionIndex);
		string[] authorReferences = new[]
		{
			author?.StringId, KingdomName(author), RulerName(author), "我国", "本国", "我方", "本朝", "本王国", "本廷"
		}.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		int lastAuthorIndex = authorReferences.Select(x => before.LastIndexOf(x, StringComparison.OrdinalIgnoreCase)).DefaultIfEmpty(-1).Max();
		string[] escapedTargets = (targetReferences ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x)).OrderByDescending(x => x.Length).Select(Regex.Escape).ToArray();
		string targetPattern = escapedTargets.Length == 0 ? "(?!)" : "(?:" + string.Join("|", escapedTargets) + ")";
		foreach (Kingdom other in Kingdom.All.Where(x => x != null && x != author && x != target))
		{
			foreach (string reference in new[] { other.StringId, KingdomName(other), RulerName(other) }
				.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				int otherIndex = before.LastIndexOf(reference, StringComparison.OrdinalIgnoreCase);
				if (otherIndex < 0 || otherIndex < lastAuthorIndex) continue;
				if (otherIndex > 0 && "向对与同".IndexOf(before[otherIndex - 1]) >= 0) continue;
				string suffix = before.Substring(otherIndex + reference.Length);
				if (ContainsAny(suffix, "而", "因此", "故而", "故向", "遂由")) continue;
				bool directSubject = Regex.IsMatch(suffix,
					@"^(?:已经|曾经|正式|公然|宣布|决定|要求|将|会|正|现)?[^，,：:]{0,4}$",
					RegexOptions.CultureInvariant);
				bool directedSubject = Regex.IsMatch(suffix,
					@"^(?:已经|曾经|正式|公然|宣布|决定|要求|将|会|正|现)?[^，,：:]{0,10}(?:向|对)" + targetPattern + @"[^，,：:]{0,8}$",
					RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				bool continuedSubjectAcrossPunctuation = Regex.IsMatch(suffix,
					@"^(?:方面|王廷|政府|国王|女王|统治者|使团|代表)?[^。！？；\n]{0,12}(?:宣布|决定|决意|下令|确认|作出决定)[^。！？；\n]{0,8}[，,：:](?:并)?(?:现|现已|正式|已经|立即|随后|同时|就此)?(?:(?:向|对)" + targetPattern + @")?[^，,：:]{0,6}$",
					RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				if (directSubject || directedSubject || continuedSubjectAcrossPunctuation) return true;
			}
		}
		return false;
	}

	private static bool IsAffirmativeDiplomacyPhraseOccurrence(string clause, int index, string phrase)
	{
		int phraseLength = phrase?.Length ?? 0;
		if (string.IsNullOrWhiteSpace(clause) || index < 0 || phraseLength <= 0 || index + phraseLength > clause.Length) return false;
		int beforeStart = Math.Max(0, index - 24);
		string before = clause.Substring(beforeStart, index - beforeStart);
		int afterStart = index + phraseLength;
		string after = clause.Substring(afterStart, Math.Min(28, clause.Length - afterStart));
		bool negatedBefore = Regex.IsMatch(before,
			@"(?:不|未|没有|不会|无意|拒绝|否认|并非|绝不|不能|无法)[^，,：:]{0,12}$",
			RegexOptions.CultureInvariant);
		bool reportedInsteadOfPerformed = Regex.IsMatch(before,
			@"(?:声称|宣称|诬称|谎称|传言|谣传|所谓|捏造|指控|假称)[^，,：:]{0,18}$",
			RegexOptions.CultureInvariant);
		bool reportedAcrossPunctuation = Regex.IsMatch(before,
			@"(?:声称|宣称|诬称|谎称|传言|谣传|所谓|捏造|指控|假称)[^。！？；\n]{0,16}[，,：:][^。！？；\n]{0,18}$",
			RegexOptions.CultureInvariant);
		bool attributedToForeignSpeaker = Regex.IsMatch(before,
			@"(?<![向对与])(?:贵国|你国|贵方|你方|对方|敌国|该国|他国|来使)(?:已经|曾经|正式|公然)?$|(?:贵国|你国|贵方|你方|对方|敌国|该国|他国|来使)(?:的|所)[^，,：:]{0,12}$",
			RegexOptions.CultureInvariant);
		bool nominalProposalReference = ProposalActionPhrases.Contains(phrase, StringComparer.OrdinalIgnoreCase)
			&& Regex.IsMatch(before, @"(?:和平|议和|和谈|停战|休战|同盟|结盟|盟约|盟友|贸易|通商|商路|商贸|互市)(?:的|之)?$", RegexOptions.CultureInvariant);
		bool negatedAfter = Regex.IsMatch(after,
			@"^[^，,。！？；]{0,18}(?:并非|不是|不成立|不存在|未获|未经|非我方|非我国|非本国|不代表|不构成|绝非|纯属|绝?不会|不可能|不能(?:发生|成立|生效)?|无效|作废)",
			RegexOptions.CultureInvariant);
		return !negatedBefore && !reportedInsteadOfPerformed && !reportedAcrossPunctuation
			&& !attributedToForeignSpeaker && !nominalProposalReference && !negatedAfter;
	}

	private static int DistanceToNearestPhrase(string text, int actionIndex, int actionLength, IEnumerable<string> phrases)
	{
		int nearest = int.MaxValue;
		foreach (string phrase in phrases ?? Enumerable.Empty<string>())
		{
			if (string.IsNullOrWhiteSpace(phrase)) continue;
			int searchFrom = 0;
			while (searchFrom < text.Length)
			{
				int phraseIndex = text.IndexOf(phrase, searchFrom, StringComparison.OrdinalIgnoreCase);
				if (phraseIndex < 0) break;
				int actionEnd = actionIndex + actionLength;
				int phraseEnd = phraseIndex + phrase.Length;
				int distance = phraseEnd <= actionIndex
					? actionIndex - phraseEnd
					: (actionEnd <= phraseIndex ? phraseIndex - actionEnd : 0);
				if (distance < nearest) nearest = distance;
				searchFrom = phraseIndex + phrase.Length;
			}
		}
		return nearest;
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
		if (normalizedIntent is "ultimatum" or "apology" or "concession") return normalizedCommitment == "binding";
		if (normalizedIntent is "statement" or "condemn" or "warning") return normalizedCommitment == "non_binding";
		return normalizedCommitment == "non_binding";
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
				if (!CanDeclareWar(author, target, out string blockReason))
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
			case "accept_alliance":
				if (alliance == null) { reason = "alliance_system_unavailable"; return true; }
				if (atWar || allied) { reason = "alliance_intent_conflicts_with_current_state"; return true; }
				break;
			case "propose_trade":
			case "accept_trade":
				if (trade == null) { reason = "trade_system_unavailable"; return true; }
				if (atWar || trading) { reason = "trade_intent_conflicts_with_current_state"; return true; }
				break;
		}
		return false;
	}

	private bool TryResolveOpenProposalFor(WorldDiplomacyJob job, Kingdom responder, Kingdom proposer, string proposalIntent, out string sourceDocumentId)
	{
		sourceDocumentId = "";
		if (job == null || responder == null || proposer == null || !IsProposalIntent(proposalIntent)) return false;
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		WorldDiplomacyRoundOffer offer = round?.PendingOffers?.Where(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.CreatedDay).FirstOrDefault();
		if (offer == null) return false;
		sourceDocumentId = offer.SourceDocumentId ?? "";
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
			&& (response.IsPlayerAuthored || string.Equals(response.RespondingToOfferDocumentId, x.SourceDocumentId, StringComparison.OrdinalIgnoreCase))) == true;
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
				invalid = intent switch
				{
					"propose_peace" => !atWar,
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

	private void EnqueueGeneratedDeclarationRepair(WorldDiplomacyJob source, string rejectedRaw, Kingdom author, Kingdom target, string reason)
	{
		if (source == null || author == null) return;
		StringBuilder correctionBuilder = new StringBuilder();
		correctionBuilder.AppendLine("【未发布草稿的硬事实纠正】");
		correctionBuilder.AppendLine("上一份assistant内容只是未发布草稿，不属于外交历史，不得引用、延续或假定其中事件已经发生。");
		correctionBuilder.AppendLine("草稿未通过公开文书与事实校验，请按下列说明重新起草。");
		correctionBuilder.AppendLine("当前发文国=" + author.StringId + "=" + KingdomName(author) + "。"
			+ (target == null ? "本次仍可从原候选清单自主选择合法对象，或在非行动性声明中不设主要对象。"
				: "对象国=" + target.StringId + "=" + KingdomName(target) + "；实时关系=" + BuildBilateralState(author, target) + "。"));
		if (string.Equals(reason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("双方当前没有战争，因此不得提出、接受或拒绝和平，不得写停战、议和、退出战争、归还战争失地或战争补偿。请改用符合现状的声明、警告、通牒、合作、贸易、结盟或其他外交主张。");
		}
		else if (string.Equals(reason, "non_response_claims_offer_source", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("只有明确接受或拒绝一项提议时，才填写responding_to_offer_document_id。若本国没有接受或拒绝，而是在原提议基础上提出反方案，应使用对应的propose_*意图，并把该字段留空；正文仍可自然说明反方案针对哪项来文。不得把字段格式错误解释成发文国没有答复资格。");
			WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(source.RoundId, source.ExchangeId));
			foreach (WorldDiplomacyRoundOffer offer in (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)).Take(8))
			{
				bool canAnswer = string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase);
				correctionBuilder.AppendLine("开放提议：来源=" + offer.SourceDocumentId + "；类型=" + offer.Intent + "；提出国=" + offer.ProposerKingdomId
					+ "；唯一答复国=" + offer.TargetKingdomId + "；本国答复资格=" + (canAnswer ? "有" : "无") + "。");
			}
		}
		else if (reason.StartsWith("offer_response_", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "new_proposal_claims_third_party_offer", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("接受或拒绝提议只能由提议中写明的对象国作出，答复对象必须是原提出国。当前发文国不是别国提议的对象时，只能评论此事，或者明确另行提出一份新提议；不得用‘你的提议我可以接受’之类措辞把别国收到的提议说成发给自己。");
			WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(source.RoundId, source.ExchangeId));
			foreach (WorldDiplomacyRoundOffer offer in (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)).Take(8))
			{
				correctionBuilder.AppendLine("开放提议：来源=" + offer.SourceDocumentId + "；类型=" + offer.Intent + "；提出国=" + offer.ProposerKingdomId + "；唯一答复国=" + offer.TargetKingdomId + "。");
			}
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
		else if (string.Equals(reason, "semantic_envelope_incomplete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "unsupported_intent_or_commitment", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "intent_commitment_mismatch", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("必须完整输出契约中的全部JSON字段。宣战、解盟、断贸使用binding；提出和平、结盟或贸易使用proposal；接受使用acceptance；拒绝使用rejection。公开正文不能为空。");
		}
		else if (string.Equals(reason, "target_kingdom_not_found", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "target_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "referenced_kingdom_not_eligible", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "kingdom_not_in_autonomous_candidate_set", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "kingdom_not_in_relay_route", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "diplomatic_action_has_no_target", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("primary_target_kingdom_id、addressed_kingdom_ids和mentioned_kingdom_ids只能使用原任务列出的合法王国ID。宣战、解盟、断贸、提议、接受或拒绝都必须填写实际对象；只有非行动性声明可以不设主要对象。");
		}
		else if (string.Equals(reason, "autonomous_round_plan_incomplete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_has_invalid_participant", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_exceeds_participant_limit", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "autonomous_round_plan_omits_direct_target", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("自主开场必须同时填写round_plan.topic和selected_kingdom_ids。参与国只能来自候选清单，总数不得超过任务上限；primary_target_kingdom_id和所有addressed_kingdom_ids必须同时列入selected_kingdom_ids。");
		}
		else if (string.Equals(reason, "accept_peace_changes_offer_terms", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("接受议和只能原样接受来源提案的条款。若要改变贡金、期限或割地，必须改为propose_peace反提案，并把responding_to_offer_document_id留空。");
		}
		else if ((reason ?? "").StartsWith("visible_intent_mismatch:", StringComparison.OrdinalIgnoreCase)
			|| (reason ?? "").StartsWith("peace_terms_not_visible:", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("JSON意图与公开正文必须一致。正式动作要在标题或正文中明确写出；议和提案中的贡金、期限和割地必须逐项公开，不能只藏在JSON字段里。若只是威胁、评论或试探，应改用相应的非执行意图。");
		}
		else if ((reason ?? "").StartsWith("declare_war_not_legal:", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "break_alliance_without_alliance", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "cancel_trade_without_trade_agreement", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "alliance_system_unavailable", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "trade_system_unavailable", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "alliance_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "trade_intent_conflicts_with_current_state", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("所选外交行动与当前真实关系不相容。保持国家自主判断，但改选当前状态下可以成立的对象与行动；不得把尚未生效的关系写成既成事实。");
		}
		correctionBuilder.Append("重新输出完整JSON。接受或拒绝时填写正确的responding_to_offer_document_id，其他情况留空。不要提到草稿、纠正、系统或上述错误。");
		string correction = correctionBuilder.ToString();
		List<WorldDiplomacyLlmMessage> messages = CloneLlmMessages(BuildLlmMessagesForJob(source));
		messages.Add(new WorldDiplomacyLlmMessage { Role = "assistant", Content = rejectedRaw ?? "" });
		messages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = correction });
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
			CandidateKingdomIds = new List<string>(source.CandidateKingdomIds ?? new List<string>()),
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
		EnqueueJob(repair);
		Log("generated declaration repair queued sourceJob=" + source.JobId + " repairJob=" + repair.JobId + " reason=" + reason);
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
			AdvanceRelay(round);
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
		Log("invalid generated document suppressed before propagation document=" + document.DocumentId
			+ " author=" + (document.AuthorKingdomId ?? "") + " target=" + (document.TargetKingdomId ?? "")
			+ " reason=" + (reason ?? ""));
		_storage.Documents.RemoveAll(x => x != null && string.Equals(x.DocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		round?.LlmProfiledKingdomIds?.RemoveAll(x => string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		if (document.IsRelayTurn && round != null && string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase))
		{
			round.RelayWaiting = false;
			AdvanceRelay(round);
			return;
		}
		CompleteExchange(document.ExchangeId, "technical_invalid_document_suppressed");
		if (round != null
			&& ReferenceEquals(_storage.ActiveRound, round)
			&& string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
			&& string.IsNullOrWhiteSpace(round.RootDocumentId))
		{
			CloseActiveRound("technical_invalid_document_suppressed");
		}
	}

	private bool TryApplyGeneratedSemanticEnvelope(WorldDiplomacyDocument document, JObject json, Kingdom author, Kingdom fallbackTarget, bool allowUntargeted, bool relayTurn)
	{
		if (document == null || json == null
			|| !(json["author_intent"] is JObject)
			|| !(json["addressed_kingdom_ids"] is JArray)
			|| !(json["mentioned_kingdom_ids"] is JArray)
			|| json["requires_response"] == null
			|| json["tone"] == null
			|| json["confidence"] == null
			|| json["primary_target_kingdom_id"] == null)
		{
			return false;
		}
		string intent = NormalizeIntent(ReadString(json, "author_intent.intent", "intent"));
		string commitment = NormalizeCommitment(ReadString(json, "author_intent.commitment", "commitment"));
		if (!IsSupportedDiplomacyIntent(intent) || !IsSupportedCommitment(commitment))
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
		if (relayTurn && target != null && !RoundRouteContainsKingdom(ResolveRound(document.RoundId), target.StringId)) return false;
		document.TargetKingdomId = target?.StringId ?? "";
		document.TargetKingdomName = target == null ? "" : KingdomName(target);
		List<string> addressed = ReadStringList(json, "addressed_kingdom_ids", "addressed");
		List<string> mentioned = ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
		if (addressed.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null)
			|| mentioned.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null))
		{
			return false;
		}
		if (relayTurn && addressed.Any(x => !RoundRouteContainsKingdom(ResolveRound(document.RoundId), x))) return false;
		document.AddressedKingdomIds = NormalizeKingdomIdList(addressed.Concat(target == null ? Enumerable.Empty<string>() : new[] { target.StringId }), author.StringId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(mentioned, author.StringId);
		document.Intent = intent;
		document.Commitment = commitment;
		document.Tone = NormalizeTone(ReadString(json, "tone"));
		document.Confidence = Math.Max(0f, Math.Min(1f, ReadFloat(json, "confidence")));
		document.RequiresResponse = ResolveValidatedResponseObligation(document, intent, ReadBool(json, "requires_response"));
		document.PeaceTerms = target == null ? document.PeaceTerms : (ParseAndValidatePeaceTerms(json, author, target) ?? document.PeaceTerms);
		document.AnalysisStatus = "generation_envelope";
		return true;
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
		if (string.IsNullOrWhiteSpace(intent))
		{
			intent = NormalizeIntent(document.HiddenIntent);
		}
		if (string.IsNullOrWhiteSpace(commitment))
		{
			commitment = NormalizeCommitment(document.HiddenCommitment);
		}
		if (string.IsNullOrWhiteSpace(intent))
		{
			intent = InferIntentFromExplicitPhrases(document.Body);
		}
		if (string.IsNullOrWhiteSpace(intent))
		{
			intent = "statement";
		}
		if (document.IsPlayerAuthored)
		{
			ReconcilePlayerDeclarationWithOpenOffer(document, ref intent, ref commitment, ref targetId, ref respondingToOfferDocumentId);
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
		document.PeaceTerms = ParseAndValidatePeaceTerms(json, ResolveKingdom(document.AuthorKingdomId), target) ?? document.PeaceTerms;
		IEnumerable<string> directTargets = addressedIds.Concat(new[] { document.TargetKingdomId });
		document.AddressedKingdomIds = NormalizeKingdomIdList(directTargets, document.AuthorKingdomId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(mentionedIds, document.AuthorKingdomId);
		document.AnalysisStatus = status == "failed" ? "fallback" : "success";
		document.Title = !string.IsNullOrWhiteSpace(titleSummary)
			? Limit(SanitizePublicDiplomacyText(titleSummary), 36)
			: (document.IsPlayerAuthored ? BuildFallbackDocumentTitle(document, intent) : document.Title);
		document.Intent = intent;
		document.Commitment = commitment;
		document.RespondingToOfferDocumentId = respondingToOfferDocumentId ?? "";
		if (!string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
		{
			document.SourceDocumentId = document.RespondingToOfferDocumentId;
			document.IsResponse = true;
		}
		document.Tone = tone;
		document.Confidence = confidence;
		document.RequiresResponse = ResolveValidatedResponseObligation(document, intent, requiresResponse);
		ProcessAnalyzedDocument(document, intent, commitment, document.RequiresResponse, tone, confidence);
	}

	private void ReconcilePlayerDeclarationWithOpenOffer(
		WorldDiplomacyDocument document,
		ref string intent,
		ref string commitment,
		ref string targetId,
		ref string respondingToOfferDocumentId)
	{
		string claimedOfferDocumentId = respondingToOfferDocumentId ?? "";
		respondingToOfferDocumentId = "";
		WorldDiplomacyRound round = ResolveRound(document?.RoundId);
		if (document == null || round == null) return;
		List<WorldDiplomacyRoundOffer> openOffers = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(x => x.CreatedDay)
			.ToList();
		if (openOffers.Count == 0) return;

		string text = FirstNonEmpty(document.Title, "") + "\n" + FirstNonEmpty(document.Body, "");
		bool explicitRejection = LooksLikeExplicitOfferRejection(text);
		bool explicitAcceptance = LooksLikeExplicitOfferAcceptance(text);
		bool counterProposal = LooksLikeExplicitCounterProposal(text);
		string responseProposalIntent = ResponseIntentToProposalIntent(intent);
		string textProposalIntent = InferProposalIntentFromOfferResponseText(text);
		if (counterProposal)
		{
			// A counter-proposal is always a new proposal, never an acceptance or rejection of
			// the incoming offer. The analyzer may still label conditional wording as accept_*.
			string counterIntent = FirstNonEmpty(textProposalIntent, responseProposalIntent);
			if (IsProposalIntent(counterIntent))
			{
				intent = counterIntent;
				commitment = "proposal";
			}
			else
			{
				intent = "statement";
				commitment = "non_binding";
			}
			string sourceHint = FirstNonEmpty(claimedOfferDocumentId, document.SourceDocumentId);
			WorldDiplomacyRoundOffer counteredOffer = openOffers.FirstOrDefault(x => !string.IsNullOrWhiteSpace(sourceHint)
				&& string.Equals(x.SourceDocumentId, sourceHint, StringComparison.OrdinalIgnoreCase));
			if (counteredOffer == null && openOffers.Count == 1) counteredOffer = openOffers[0];
			if (counteredOffer != null) targetId = counteredOffer.ProposerKingdomId;
			Log("player counter-proposal kept independent document=" + document.DocumentId
				+ " intent=" + intent + " claimedOffer=" + claimedOfferDocumentId);
			return;
		}
		IEnumerable<WorldDiplomacyRoundOffer> candidates = openOffers;
		if (!string.IsNullOrWhiteSpace(claimedOfferDocumentId))
		{
			candidates = candidates.Where(x => string.Equals(x.SourceDocumentId, claimedOfferDocumentId, StringComparison.OrdinalIgnoreCase));
		}
		string requestedTargetId = targetId ?? "";
		if (!string.IsNullOrWhiteSpace(requestedTargetId))
		{
			candidates = candidates.Where(x => string.Equals(x.ProposerKingdomId, requestedTargetId, StringComparison.OrdinalIgnoreCase));
		}
		string expectedProposalIntent = FirstNonEmpty(responseProposalIntent, textProposalIntent);
		if (!string.IsNullOrWhiteSpace(expectedProposalIntent))
		{
			candidates = candidates.Where(x => string.Equals(NormalizeIntent(x.Intent), expectedProposalIntent, StringComparison.OrdinalIgnoreCase));
		}
		List<WorldDiplomacyRoundOffer> matches = candidates.Take(2).ToList();
		if (matches.Count != 1)
		{
			// “接受/拒绝此约”只有在当前确实只有一份待本国答复的提案时才允许自动绑定。
			WorldDiplomacyRoundOffer soleOffer = openOffers.Count == 1 ? openOffers[0] : null;
			Kingdom requestedTarget = ResolveKingdom(requestedTargetId);
			bool explicitlyNamesConflictingTarget = soleOffer != null && requestedTarget != null
				&& !string.Equals(requestedTarget.StringId, soleOffer.ProposerKingdomId, StringComparison.OrdinalIgnoreCase)
				&& ((!string.IsNullOrWhiteSpace(KingdomName(requestedTarget))
						&& text.IndexOf(KingdomName(requestedTarget), StringComparison.OrdinalIgnoreCase) >= 0)
					|| (!string.IsNullOrWhiteSpace(RulerName(requestedTarget))
						&& text.IndexOf(RulerName(requestedTarget), StringComparison.OrdinalIgnoreCase) >= 0));
			if ((explicitAcceptance || explicitRejection) && soleOffer != null && !explicitlyNamesConflictingTarget)
			{
				matches = new List<WorldDiplomacyRoundOffer> { soleOffer };
			}
			else
			{
				return;
			}
		}

		WorldDiplomacyRoundOffer offer = matches[0];
		string offerIntent = NormalizeIntent(offer.Intent);
		if (!counterProposal && explicitRejection)
		{
			intent = ProposalIntentToResponseIntent(offerIntent, accepted: false);
			commitment = "rejection";
		}
		else if (!counterProposal && explicitAcceptance)
		{
			intent = ProposalIntentToResponseIntent(offerIntent, accepted: true);
			commitment = "acceptance";
		}
		else if (!string.Equals(ResponseIntentToProposalIntent(intent), offerIntent, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(intent)) return;

		targetId = offer.ProposerKingdomId;
		respondingToOfferDocumentId = offer.SourceDocumentId;
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
		string normalizedIntent = NormalizeIntent(intent);
		if (document.IsPlayerAuthored
			&& TryGetPlayerWorldStateIntentViolation(document, normalizedIntent, commitment, author, target, out string playerActionBlockReason))
		{
			Log("player diplomatic action blocked before execution document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + (target?.StringId ?? "")
				+ " intent=" + normalizedIntent + " reason=" + playerActionBlockReason);
			document.Intent = "statement";
			document.Commitment = "non_binding";
			document.RespondingToOfferDocumentId = "";
			document.Title = BuildFallbackDocumentTitle(document, "statement");
			document.MechanicalResult = "外交动作未执行：正文或当前外交状态不足以支持该动作";
			normalizedIntent = "statement";
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
		// Make the validated declaration minimally publishable before any irreversible game
		// action. Full geographic propagation is filled in below.
		document.IsReadyForPublication = true;
		try
		{
			ApplyDocumentPressure(document);
			if (target != null && target != author && IsImmediateIntent(normalizedIntent))
			{
				ExecuteImmediateIntent(author, target, normalizedIntent, document);
			}
			TrySettleRelayOffer(document);
			ApplyDiplomaticPressureEffect(document);
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
		try
		{
			AppendCanonicalDocumentEvents(document);
		}
		catch (Exception ex)
		{
			ScheduleDeferredCanonicalHistoryRetry(document.DocumentId);
			Log("canonical history append deferred document=" + document.DocumentId + " error=" + ex.Message);
		}
		try
		{
			StartDocumentPropagation(document, author);
		}
		catch (Exception ex)
		{
			Log("valid declaration propagation failed document=" + document.DocumentId + " error=" + ex.Message);
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
		bool isQualitativeCommitment = normalizedIntent is "ultimatum" or "apology" or "concession";
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
		bool isAcceptance = normalizedIntent.StartsWith("accept_", StringComparison.OrdinalIgnoreCase);
		if (isAcceptance && !HasExplicitPlayerAcceptance(document.Body, proposalIntent))
		{
			reason = "player_action_acceptance_not_explicit_or_is_counter_proposal";
			return true;
		}
		bool isRejection = normalizedIntent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase);
		if (isRejection && !HasExplicitPlayerRejection(document.Body, proposalIntent))
		{
			reason = "player_action_rejection_not_explicit_or_is_counter_proposal";
			return true;
		}
		if ((isNewProposal || isQualitativeCommitment)
			&& TryGetPlayerVisibleIntentViolation(normalizedIntent, document.Body, author, target, out string visibleActionReason))
		{
			reason = "player_action_" + visibleActionReason;
			return true;
		}
		if (IsImmediateIntent(normalizedIntent)
			&& !HasExplicitPlayerImmediateAction(document, normalizedIntent, author, target, out reason))
		{
			return true;
		}
		if (TryGetDiplomaticStateViolation(normalizedIntent, author, target, out reason))
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
		return false;
	}

	private bool HasExplicitPlayerImmediateAction(
		WorldDiplomacyDocument document,
		string intent,
		Kingdom author,
		Kingdom target,
		out string reason)
	{
		reason = "player_action_not_explicitly_addressed_to_target";
		if (document == null || target == null || string.IsNullOrWhiteSpace(document.Body)) return false;
		WorldDiplomacyDocument source = ResolveDocument(document.SourceDocumentId);
		bool targetIsReplyContext = document.IsResponse
			&& string.Equals(source?.AuthorKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase);
		foreach (string rawClause in Regex.Split(document.Body, @"[\r\n。！？；]+"))
		{
			string clause = (rawClause ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clause)) continue;
			bool namesTarget = ContainsAny(clause, target.StringId, KingdomName(target), RulerName(target));
			bool usesReplyReference = targetIsReplyContext
				&& ContainsAny(clause, "贵国", "你国", "贵方", "你方", "贵朝", "贵邦", "双方", "两国");
			if (!namesTarget && !usesReplyReference) continue;
			if (!TryGetPlayerVisibleIntentViolation(intent, clause, author, target, out string visibleReason))
			{
				reason = "";
				return true;
			}
			reason = "player_action_" + visibleReason;
		}
		return false;
	}

	private static bool HasExplicitPlayerAcceptance(string body, string proposalIntent)
	{
		if (string.IsNullOrWhiteSpace(body)
			|| LooksLikeExplicitCounterProposal(body)
			|| LooksLikeExplicitOfferRejection(body)
			|| !HasAffirmativeDiplomacyPhrase(body, "接受", "同意", "应允", "批准", "确认接受", "愿按")) return false;
		return MentionsProposalSubject(body, proposalIntent);
	}

	private static bool HasExplicitPlayerRejection(string body, string proposalIntent)
	{
		if (string.IsNullOrWhiteSpace(body)
			|| LooksLikeExplicitCounterProposal(body)
			|| !HasAffirmativeDiplomacyPhrase(body, "拒绝", "不接受", "不可接受", "无法接受", "不同意", "不能同意", "驳回")) return false;
		return MentionsProposalSubject(body, proposalIntent);
	}

	private static bool MentionsProposalSubject(string body, string proposalIntent)
	{
		return NormalizeIntent(proposalIntent) switch
		{
			"propose_peace" => ContainsAny(body, "和平", "议和", "和谈", "停战", "休战", "和约"),
			"propose_alliance" => ContainsAny(body, "同盟", "结盟", "盟约", "联盟"),
			"propose_trade" => ContainsAny(body, "贸易", "通商", "商路", "商贸", "互市"),
			_ => false
		};
	}

	private void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument document)
	{
		if (document == null || !string.IsNullOrWhiteSpace(document.MechanicalResult)) return;
		string intent = NormalizeIntent(document.Intent);
		if (intent != "apology" && intent != "concession" && intent != "ultimatum") return;
		Kingdom author = ResolveKingdom(document.AuthorKingdomId);
		Kingdom target = ResolveKingdom(document.TargetKingdomId);
		if (author == null || target == null || author == target) return;
		if (intent == "ultimatum")
		{
			AddWarPressure(author.StringId, target.StringId, 18, "正式最后通牒：" + document.Title, intent);
		}
		else
		{
			int reduction = intent == "concession" ? -22 : -16;
			AddWarPressure(author.StringId, target.StringId, reduction, "正式" + (intent == "concession" ? "让步" : "道歉") + "：" + document.Title, intent);
			AddWarPressure(target.StringId, author.StringId, reduction / 2, "对方作出正式" + (intent == "concession" ? "让步" : "道歉"), intent);
		}
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

	private void HandleRoundDocumentProcessed(WorldDiplomacyDocument document)
	{
		if (document == null || document.RoundProgressHandled) return;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		bool successfulMechanicalAction = document.ChangedDiplomaticState;
		bool substantiveProgress = document.MadeDiplomaticProgress;
		bool isRootDocument = string.IsNullOrWhiteSpace(round.RootDocumentId)
			|| string.Equals(round.RootDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase);
		WorldDiplomacyRoundParticipant participant = null;
		if (!document.RoundAccountingHandled)
		{
			substantiveProgress = IsValidatedSubstantiveProgress(document, round, successfulMechanicalAction);
			bool diplomaticActionAttempt = IsValidatedDiplomaticActionAttempt(document, round, successfulMechanicalAction);
			int accountingDay = CurrentDay();
			if (!isRootDocument && !document.IsPlayerAuthored && document.IsRelayTurn)
			{
				participant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
			}
			round.LastActivityDay = accountingDay;
			if (successfulMechanicalAction) round.ExecutedActionCount++;
			document.MadeDiplomaticProgress = substantiveProgress;
			if (substantiveProgress)
			{
				round.SubstantiveProgressCount++;
				round.LastSubstantiveProgressDay = accountingDay;
			}
			if (diplomaticActionAttempt)
			{
				round.DiplomaticActionAttemptCount++;
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
		bool hasValidatedResolution = round.ExecutedActionCount > 0
			|| (substantiveProgress && !HasOpenRoundOffers(round));
		if (string.Equals(document.RoundStatus, "resolved", StringComparison.OrdinalIgnoreCase) && hasValidatedResolution)
		{
			round.RoundStatus = "resolved";
			CloseActiveRound("relay_resolved");
			document.RoundProgressHandled = true;
			return;
		}
		if (string.Equals(document.RoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
			&& round.SubstantiveProgressCount > 0
			&& (round.RelayPassNumber >= 2 || string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase)))
		{
			round.RoundStatus = "deadlocked";
			CloseActiveRound("relay_deadlocked");
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

	private static bool IsValidatedSubstantiveProgress(WorldDiplomacyDocument document, WorldDiplomacyRound round, bool successfulMechanicalAction)
	{
		if (document == null || round == null) return false;
		if (successfulMechanicalAction) return true;
		string intent = NormalizeIntent(document.Intent);
		if (intent == "ultimatum" || intent == "apology" || intent == "concession")
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

	private static bool IsValidatedDiplomaticActionAttempt(WorldDiplomacyDocument document, WorldDiplomacyRound round, bool successfulMechanicalAction)
	{
		if (document == null || round == null) return false;
		if (successfulMechanicalAction) return true;
		string intent = NormalizeIntent(document.Intent);
		if (!IsProposalIntent(intent) && string.IsNullOrWhiteSpace(ResponseIntentToProposalIntent(intent))) return false;
		return IsValidatedSubstantiveProgress(document, round, successfulMechanicalAction: false);
	}

	private void CommitEmbeddedRoundPlan(WorldDiplomacyRound round, WorldDiplomacyDocument root)
	{
		if (round == null || root == null || round.RelayPlanned
			|| !ReferenceEquals(_storage.ActiveRound, round)
			|| !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		List<string> candidates = Kingdom.All.Where(x => x != null && !x.IsEliminated
			&& !string.Equals(x.StringId, root.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
			&& HasIndependentWorldDiplomacyAuthority(x))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
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
		List<string> candidates = Kingdom.All
			.Where(x => x != null && !x.IsEliminated
				&& !string.Equals(x.StringId, root.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& HasIndependentWorldDiplomacyAuthority(x))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.StringId).ToList();
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
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(ResolveKingdom(root.AuthorKingdomId), kingdom));
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
		if (string.IsNullOrWhiteSpace(round.TopicCategory)) round.TopicCategory = InferTopicCategory(round.RoundTopic, initiator, ResolveKingdom(root.TargetKingdomId));
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
		ScheduleNextRelayHop(round);
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

	private string BuildCurrentGeographicRelations(WorldDiplomacyRound round, Kingdom author)
	{
		if (round == null || author == null) return "【当前地理关系】无可核实对象。";
		List<string> lines = new List<string>();
		foreach (string id in (round.RelayRouteKingdomIds ?? new List<string>())
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
		if (value.IndexOf("贸易", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("商路", StringComparison.OrdinalIgnoreCase) >= 0) return "trade_order";
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
		sb.AppendLine("【本次外交公文动态状态】");
		sb.AppendLine("长期档案中的宣言是已颁布公文，不是君主即时聊天。为当前王国另行起草一份可独立传阅的正式公文；不要承接聊天语气，绝不可以重述历史发言，严禁反复扯皮，必须在四个轮次内产生行为结果。");
		sb.AppendLine("议题=" + (round.RoundTopic ?? ""));
		sb.AppendLine("公文送达与发布顺序=" + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
		sb.AppendLine("本篇发布国=" + author.StringId + "=" + KingdomName(author) + "，授权统治者=" + RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			sb.AppendLine("【本篇优先任务：回应玩家王国宣言】");
			sb.AppendLine("玩家王国的下列宣言已经送达本国王庭并直接指向本国，本篇必须正面回应它，而不是沿原定公文次序改谈其他国家：来源="
				+ prioritySource.DocumentId + "|发文国=" + prioritySource.AuthorKingdomId + "|标题=" + prioritySource.Title
				+ "|已裁定意图=" + NormalizeIntent(prioritySource.Intent));
			sb.AppendLine("若玩家是在接受或拒绝本国此前提案，应承认该答复已经发生，并对结果作一次正式反应；不得继续声称玩家尚未答复。若玩家提出新提案、反提案或最后通牒，应接受、拒绝、提出明确反方案或说明仍待解决的具体条件。该优先回应完成后，原定公文传递会继续，不得要求玩家立即再次发言。");
		}
		{
			sb.AppendLine("【本发布国当前决策档案】");
			string voice = BuildRulerVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(voice)) sb.AppendLine("统治者声音=" + Limit(voice, 700));
			string realmVoice = BuildRealmInstitutionalVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(realmVoice)) sb.AppendLine("国家制度与礼制声音=" + Limit(realmVoice, 1100));
			string family = BuildAuthorRulerFamilyContext(author);
			if (!string.IsNullOrWhiteSpace(family)) sb.AppendLine("王室与亲属=" + Limit(family, 400));
			string policy = WorldDiplomacyPolicyContext.BuildSnapshot(author.StringId);
			if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("政策=" + Limit(policy, 550));
		}
		sb.AppendLine("最近送抵本国王庭的公文来源=" + (previous?.StringId ?? "") + "=" + KingdomName(previous));
		sb.AppendLine("送件国只是最近来文来源，不是程序指定对象；本国自行选择参与国作为对象，非行动性宣言也可不设主要对象。");
		sb.AppendLine("允许动作对象=" + string.Join(",", (round.RelayRouteKingdomIds ?? new List<string>()).Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))));
		sb.AppendLine(BuildCurrentLegalDiplomaticOptions(round, author));
		sb.AppendLine(BuildCurrentGeographicRelations(round, author));
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase);
			sb.AppendLine("待回应提议=" + offer.Intent + "|提出国=" + offer.ProposerKingdomId + "|对象国=" + offer.TargetKingdomId + "|来源=" + offer.SourceDocumentId
				+ (canAnswer ? "|答复资格=本国可以接受或拒绝" : "|答复资格=本国不是对象国，不得接受或拒绝；只能评论或明确另提新案"));
		}
		int age = Math.Max(0, CurrentDay() - round.StartedDay);
		int targetDays = Math.Max(1, round.SoftEndDay - round.StartedDay);
		int remainingDays = Math.Max(0, targetDays - age);
		sb.AppendLine("本次交涉已经进行=" + age.ToString(CultureInfo.InvariantCulture) + "天；预计时长=" + targetDays.ToString(CultureInfo.InvariantCulture)
			+ "天；距离预计收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前公文往来阶段=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
		AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays);
		AppendOpenOfferAnswerRequirement(sb, round, author, age, targetDays);
		if (age * 100 >= targetDays * 85) sb.AppendLine("当前已进入最后阶段：必须给出最终条件、接受、拒绝、退出或合法行动，不得继续空泛争论。");
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
		List<string> legalPeaceTargetIds = new List<string>();
		StringBuilder stateSnapshot = new StringBuilder();
		foreach (string id in round.RelayRouteKingdomIds ?? new List<string>())
		{
			Kingdom other = ResolveKingdom(id);
			if (other == null || other == author) continue;
			stateSnapshot.AppendLine("即时状态：" + id + "=" + BuildBilateralState(author, other) + "，私人关系=" + DescribeRulerRelation(GetRulerRelation(author, other)));
			if (FactionManager.IsAtWarAgainstFaction(author, other))
			{
				legalPeaceTargetIds.Add(id);
				stateSnapshot.AppendLine(Limit(BuildWarNegotiationContext(author, other), 1200));
			}
		}
		stateSnapshot.AppendLine(legalPeaceTargetIds.Count == 0
			? "当前没有可合法议和的参与国：不得使用propose_peace、accept_peace或reject_peace，也不得把和平关系写成停战谈判。"
			: "当前可合法使用和平类意图的对象=" + string.Join(",", legalPeaceTargetIds) + "。和平类意图不得指向名单外王国。");
		sb.AppendLine(stateSnapshot.ToString().TrimEnd());
		return sb.ToString().TrimEnd();
	}

	private static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays)
	{
		if (sb == null || round == null) return;
		sb.AppendLine("公开事件只提供已经发生的交涉背景，不预定本国必须推进哪种结果。本国依据国家卡与当前利益，自主决定提出条件、回应来文、施压、合作、退出或维持现状；生成一个合法选项不等于它已经生效。");
		sb.AppendLine("已经形成的明确外交尝试=" + Math.Max(0, round.SubstantiveProgressCount).ToString(CultureInfo.InvariantCulture)
			+ "次；其中指向关系变更的尝试=" + Math.Max(0, round.DiplomaticActionAttemptCount).ToString(CultureInfo.InvariantCulture)
			+ "次；已经正式生效的外交行动=" + Math.Max(0, round.ExecutedActionCount).ToString(CultureInfo.InvariantCulture) + "次。");
		if (round.SubstantiveProgressCount > 0) return;
		if (round.FinalActionOpportunityIssued || age * 100 >= targetDays * 85)
		{
			sb.AppendLine("交涉已临近预计期限。本篇应给出本国的清楚最终选择：提出可执行条件、回答有权答复的提议、明确施压或合作方向，或说明为何退出并维持现状；不得为了收束而虚构已经生效的结果。");
		}
		else if (age * 100 >= targetDays * 40)
		{
			sb.AppendLine("交涉已经进入中段。本篇应推进本国真正关心的事项：给出具体条件、反条件、承诺、威慑或明确保留意见；不要只改写旧立场。");
		}
		else
		{
			sb.AppendLine("本次交涉仍在早期。可以先说明立场，但应让本国的真实利益和下一步方向清楚可辨。");
		}
	}

	private static void AppendOpenOfferAnswerRequirement(StringBuilder sb, WorldDiplomacyRound round, Kingdom author, int age, int targetDays)
	{
		if (sb == null || round == null || author == null) return;
		List<WorldDiplomacyRoundOffer> answerable = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (answerable.Count == 0) return;
		if (round.FinalActionOpportunityIssued || age * 100 >= targetDays * 70)
		{
			sb.AppendLine("本国是尚未答复的正式提议对象，交涉已经进入收束阶段。本篇必须接受、拒绝或提出明确反方案；不得绕开提议另谈无关事项，也不得以评论代替答复。");
		}
		else
		{
			sb.AppendLine("本国面前有一项有资格回答的正式提议，应当正面接受、拒绝或提出反方案；若暂不作答，正文必须说明仍待解决的具体条件。");
		}
	}

	private void ScheduleNextRelayHop(WorldDiplomacyRound round)
	{
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
		int nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection);
		if (nextIndex < 0)
		{
			round.RelayDirection = round.RelayDirection >= 0 ? -1 : 1;
			round.RelayPassNumber++;
			round.RelayPassStartedDay += passDurationDays;
			if (CurrentDay() >= round.SoftEndDay && round.RelayPassNumber > 3)
			{
				if (round.SubstantiveProgressCount > 0 && !HasOpenRoundOffers(round))
				{
					CloseActiveRound("relay_soft_end");
					return;
				}
				if (!round.FinalActionOpportunityIssued)
				{
					round.FinalActionOpportunityIssued = true;
					Log("relay final substantive attempt opportunity opened round=" + round.RoundId);
				}
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
			foreach (WorldDiplomacyDocument document in _storage.Documents.Where(x => x != null && x.IsReadyForPublication && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)))
			{
				RecordKingdomKnowledge(receiver.StringId, document.DocumentId, CurrentDay());
				RecordNobleKnowledge(receiver.StringId, document.DocumentId, CurrentDay());
			}
			if (IsPlayerKingdom(receiver))
			{
				RecordPlayerOpportunity(round, receiver);
				round.RelayWaiting = false;
				AdvanceRelay(round);
				continue;
			}
			WorldDiplomacyDocument source = _storage.Documents.Where(x => x != null && x.IsReadyForPublication && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).FirstOrDefault();
			EnqueueGenerationJob(receiver, previous ?? ResolveKingdom(round.InitiatorKingdomId), null, isResponse: true,
				sourceDocument: source, priority: 75, roundId: round.RoundId, allowUntargeted: true,
				isRelayTurn: true, previousKingdomId: arrival.FromKingdomId, scheduledDay: arrival.DueDay);
		}
	}

	private void AdvanceRelay(WorldDiplomacyRound round)
	{
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
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
			playerParticipant.SelectedForRelay = AddParticipantToRelayRouteIfNeeded(round, document.AuthorKingdomId);
		}
		WorldDiplomacyPlayerOpportunity opportunity = _storage.PlayerOpportunities.FirstOrDefault(x => x != null
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase));
		if (opportunity != null) opportunity.Status = "answered";
		foreach (string id in (document.AddressedKingdomIds ?? new List<string>())
			.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
			if (kingdom == null) continue;
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdom.StringId, "active", mandatoryReply: false);
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
			participant.SelectedForRelay = AddParticipantToRelayRouteIfNeeded(round, kingdom.StringId);
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
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null)
		{
			round = EnsureActiveRound(author, ResolveKingdom(document.TargetKingdomId), document.IsPlayerAuthored);
			document.RoundId = round?.RoundId ?? "";
			document.ExchangeId = document.RoundId;
		}
		Settlement origin = ResolveCourtSettlement(author);
		document.OriginSettlementId = origin?.StringId ?? "";
		document.PropagationStarted = true;
		document.IsReadyForPublication = true;
		if (!document.IsPlayerAuthored && !AreMapNotificationsEnabled())
		{
			// A declaration published while notifications are disabled must not be replayed
			// as a backlog when the player enables them later.
			document.IsNotified = true;
		}
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
			if (knownKingdomIds.Contains(destination.Item1.StringId)) continue;
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
					if (RecordKingdomKnowledge(receiver.StringId, document.DocumentId, day)) ProcessCourtArrival(receiver, document);
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
				if (string.Equals(court.Item1.StringId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
					|| HasKingdomKnowledge(court.Item1.StringId, document.DocumentId)
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
		if (IsPlayerKingdom(receiver))
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
			if (directlyAddressed && (isPrimaryTarget || document.RequiresResponse))
			{
				WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, receiver.StringId, "active", mandatoryReply: true);
				TryScheduleMandatoryCourtResponse(round, participant, receiver, document);
			}
		}
		Log("court received document=" + document.DocumentId + " receiver=" + receiver.StringId + " direct=" + directlyAddressed + " day=" + CurrentDay().ToString(CultureInfo.InvariantCulture));
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
			|| !trigger.IsPlayerAuthored || (!isPrimaryTarget && !IsDiplomaticRepresentativeForAddressedVassal(receiver, trigger) && !trigger.RequiresResponse))
		{
			if (participant != null) participant.MandatoryReplyPending = false;
			return;
		}
		if (HasKingdomRespondedToDocument(receiver.StringId, trigger.DocumentId))
		{
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
		EnqueueGenerationJob(receiver, target, null, isResponse: true, sourceDocument: trigger,
			priority: 95, externalResponseOnly: true, roundId: round.RoundId, isRelayTurn: reuseRelayTranscript,
			previousKingdomId: trigger.AuthorKingdomId, scheduledDay: CurrentDay());
		participant.LastTriggeredDocumentId = trigger.DocumentId;
		Log("mandatory response queued round=" + round.RoundId + " author=" + receiver.StringId + " target=" + (target?.StringId ?? "") + " source=" + trigger.DocumentId);
	}

	private bool HasKingdomRespondedToDocument(string kingdomId, string documentId)
	{
		return _storage.Documents.Any(x => x != null && string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.SourceDocumentId, documentId, StringComparison.OrdinalIgnoreCase));
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
				CloseActiveRound("automatic_request_circuit_breaker");
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
			CloseActiveRound("relay_hard_end");
			return;
		}
		if (!round.RelayPlanned)
		{
			WorldDiplomacyDocument root = ResolveDocument(round.RootDocumentId);
			if (root != null && root.IsReadyForPublication) EnqueueRoundPlanJob(round, root);
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
		List<WorldDiplomacyDocument> documents = _storage.Documents.Where(x => x != null && x.IsReadyForPublication
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
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
			summary.Facts.Add(new WorldDiplomacyRoundFact
			{
				Kind = "declaration",
				Text = "[宣言记录] " + BuildCompactDocumentMemoryLine(document),
				SourceDocumentIds = new List<string> { document.DocumentId },
				KingdomIds = new[] { document.AuthorKingdomId, document.TargetKingdomId }
					.Concat(document.AddressedKingdomIds ?? new List<string>())
					.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			});
			if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult))
			{
				summary.Facts.Add(new WorldDiplomacyRoundFact
				{
					Kind = "confirmed_result",
					Text = "[游戏已执行] " + document.MechanicalResult,
					SourceDocumentIds = new List<string> { document.DocumentId },
					KingdomIds = new[] { document.AuthorKingdomId, document.TargetKingdomId }
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
				KingdomIds = new[] { document.AuthorKingdomId, document.TargetKingdomId }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			});
			if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult)) summary.Facts.Add(new WorldDiplomacyRoundFact
			{
				Kind = "confirmed_result", Text = "[游戏已执行] " + document.MechanicalResult,
				SourceDocumentIds = new List<string> { document.DocumentId },
				KingdomIds = new[] { document.AuthorKingdomId, document.TargetKingdomId }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
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
			round.PendingOffers.RemoveAll(x => x != null && string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
			round.PendingOffers.Add(new WorldDiplomacyRoundOffer
			{
				SourceDocumentId = document.DocumentId,
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
		WorldDiplomacyRoundOffer offer = round.PendingOffers
			.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Intent, proposalIntent, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(document.TargetKingdomId) || string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
				&& (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId) || string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(x => x.CreatedDay).FirstOrDefault();
		if (offer == null)
		{
			document.MechanicalResult = "答复未执行：来源提议已关闭或失效";
			return;
		}
		if (intent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase))
		{
			offer.Status = "rejected";
			return;
		}
		WorldDiplomacyDocument source = ResolveDocument(offer.SourceDocumentId);
		Kingdom proposer = ResolveKingdom(offer.ProposerKingdomId);
		Kingdom target = ResolveKingdom(offer.TargetKingdomId);
		if (source == null || proposer == null || target == null)
		{
			offer.Status = "invalidated";
			document.MechanicalResult = "接受未执行：原提议或当事国已失效";
			return;
		}
		try
		{
			if (proposalIntent == "propose_peace")
			{
				// Acceptance ratifies the source offer exactly. Different terms are a counter-proposal.
				document.PeaceTerms = source.PeaceTerms;
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
				offer.Status = "accepted";
			}
			else
			{
				document.MechanicalResult = "接受未执行：" + Limit(ex.Message, 180);
				offer.Status = "execution_failed";
			}
			Log("offer acceptance execution failed document=" + document.DocumentId + " offer=" + offer.SourceDocumentId + " error=" + ex.Message);
			return;
		}
		offer.Status = document.ChangedDiplomaticState
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
		if (intent == "declare_war")
		{
			if (!CanDeclareWar(author, target, out string blockReason))
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
				RunDiplomaticAction("world_diplomacy_break_alliance", () => alliance.EndAlliance(author, target));
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
		JToken token = json.SelectToken("peace_terms");
		if (token == null) return null;
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

	private bool CanDeclareWar(Kingdom initiator, Kingdom target, out string reason)
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
		WorldDiplomacyRound round = EnsureActiveRound(initiator, target, isPlayerInsertion: IsPlayerKingdom(initiator));
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
		int scaledValue = (int)Math.Round(baseValue * GetNativeIntentMultiplier());
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
			if (!discussionHit && !proactiveDiscussion)
			{
				return;
			}
			string block = ResolveInstance()?.BuildDiplomacyMemoryContext(hero, kingdomIdOverride);
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

	private HashSet<string> GetKnownDocumentIdsForHero(Hero hero, string kingdomIdOverride)
	{
		string kingdomId = FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		HashSet<string> knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Settlement currentSettlement = hero?.CurrentSettlement ?? hero?.PartyBelongedTo?.CurrentSettlement ?? Settlement.CurrentSettlement;
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

	private string BuildDiplomacyMemoryContext(Hero hero, string kingdomIdOverride)
	{
		if (_storage.Documents.Count == 0)
		{
			return "";
		}
		string kingdomId = FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		HashSet<string> knownIds = GetKnownDocumentIdsForHero(hero, kingdomIdOverride);
		if (knownIds.Count == 0) return "";
		List<WorldDiplomacyDocument> direct = _storage.Documents
			.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
				&& (!string.IsNullOrWhiteSpace(kingdomId) && (string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase) || (x.AddressedKingdomIds ?? new List<string>()).Contains(kingdomId, StringComparer.OrdinalIgnoreCase))))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(3)
			.ToList();
		HashSet<string> directIds = new HashSet<string>(direct.Select(x => x.DocumentId), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> headlines = _storage.Documents
			.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "") && !directIds.Contains(x.DocumentId ?? "") && IsMajorDiplomaticDocument(x))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(2)
			.ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【当前人物已获知的王国公告】");
		sb.AppendLine("以下仅是公文传播到此人所在地点后，或传到其所属王庭后由贵族通信网获得的事实；不代表全世界同步知晓，也不是当前对话的新承诺。");
		foreach (WorldDiplomacyDocument document in direct)
		{
			sb.AppendLine("- [直接相关] " + BuildCompactDocumentMemoryLine(document));
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
			int scaledDelta = (int)Math.Round(delta * GetDocumentInfluenceMultiplier() * repetitionFactor);
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
		bool enabled = AreMapNotificationsEnabled();
		if (!enabled)
		{
			if (_lastMapNotificationsEnabled != false)
			{
				foreach (WorldDiplomacyDocument document in _storage.Documents.Where(x => x != null
					&& !x.IsPlayerAuthored && x.IsReadyForPublication && !x.IsNotified))
				{
					document.IsNotified = true;
				}
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
				&& !x.IsRead
				&& !x.IsNotified
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
			}
			catch (Exception ex)
			{
				_notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
				Log("notification publish failed: " + ex.Message);
				break;
			}
		}
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
			"关闭");
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
					"待解析的外交回应",
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
				EnqueueAnalysisJob(response, priority: 100);
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
			group.Records.Add(new WorldEventRecordData
			{
				EventId = document.DocumentId,
				KindLabel = typeLabel,
				HeaderRightText = FirstNonEmpty(document.TargetKingdomName, "世界公告"),
				DateText = date,
				TitleText = BuildDisplayedDocumentTitle(document),
				IndexTitleText = FirstNonEmpty(document.Title, document.AuthorKingdomName + "发布外交宣言", "外交宣言"),
				MetaText = date + "  ·  " + typeLabel + "  ·  " + document.AuthorKingdomName + (string.IsNullOrWhiteSpace(document.TargetKingdomName) ? "" : " → " + document.TargetKingdomName) + eventMeta,
				PolicyNameText = "",
				BodyText = string.IsNullOrWhiteSpace(document.Body) ? "该旧公文正文已经压缩，可查看对应年度外交摘要。" : FormatDiplomaticBodyForDisplay(document.Body),
				BodySectionTitleText = "公告正文",
				ImpactSectionTitleText = string.IsNullOrWhiteSpace(document.MechanicalResult) ? "" : "外交结果",
				ImpactText = document.MechanicalResult ?? "",
				IndexMetaText = date + "  ·  " + typeLabel + eventMeta,
				UnreadMarkerText = document.IsRead ? "" : "新",
				IsUnread = !document.IsRead,
				HasPolicyName = false,
				HasImpact = !string.IsNullOrWhiteSpace(document.MechanicalResult)
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
		sb.AppendLine(DiplomaticDeclarationWritingContractMarker);
		sb.AppendLine("本节仅在MODE=DECLARE时生效；MODE=COMPACT时忽略本节，并严格执行system中的MODE=COMPACT固定任务合同与尾部动态参数。");
		sb.AppendLine("标题和正文只能使用卡拉迪亚世界内的政治与外交语言，不得讨论幕后调度、生成规则、候选方案、数据判定或技术流程；内部字段只出现在JSON结构中，绝不能变成公文内容。");
		sb.AppendLine("正文是一份面向诸国、本国贵族与臣民公开颁布的外交文书，必须脱离上下文也能成立。此前的宣言只是已经送抵并归档的别国公文，不是正在发言的聊天对象。普通宣言不得用“你”“你的”“你们”持续向另一位君主说话；使用对方国名、“贵国”或其明确制度称谓。禁止“让我说说”“你应该谢我”“你自己选”“那我就……”“等你答复”等私人回嘴句式。");
		sb.AppendLine("国家而非君主私交是叙述中心。正文至少自然出现一次能够代表政治共同体的称谓，例如王国名、帝国、王庭、诸侯与贵族、臣民、军队、商旅或边地；由统治者的个人性格决定语气和取舍，但不能把国家决定写成几位君主私下讨价还价。不得虚构贵族已经集会、投票、宣誓或一致同意。");
		sb.AppendLine("知识库提供的制度、合法性来源、历史身份和礼制称谓可以自然进入正文：若明确记载元老院、汗庭、部族、议政传统或其他机构，发文国可用它们说明权威与责任；未明确提供时只能使用中性称谓，不得根据文化刻板印象自创制度。机构身份与统治者个人头衔必须严格区分：元老院制不等于皇帝本人是元老，军功制不等于皇帝是将军，君主制也不能被改写成元老院制。发文档案给出的ruler_title_hard_fact与government_hard_fact优先于人物背景和检索材料，绝不可改称。世界观材料用于决定称谓和立场，不得整段照抄编年史；每篇最多使用一处有辨识度的文化意象，不能堆砌口号。");
		sb.AppendLine("文风应正式、克制并有国家分量，但不是僵硬的八股模板。开头直接说明事件、判断、决定或条件，不逐一点名所有君主和头衔，不反复自报身份；避免机械套用“回顾—原则—要求—后果”的固定段式，也不要每篇都先致意、再遗憾、最后威胁。需要正式确认的条约可以分项，普通宣言优先用连贯段落表达。");
		sb.AppendLine("不同国家不得共享一套换名模板。必须依据发文国制度、合法性来源、政治共同体与礼制声音选择公文主体、措辞和节奏：帝国制度、王庭贵族、部族与汗庭、氏族传统或商业城邦只有在档案明确支持时才能使用。文化特色应融入国家如何主张权威、承诺和责任，不能只在通用正文上粘贴一句口号。");
		sb.AppendLine("可以坚定、务实、冷峻、和缓或骄傲，但讥讽也必须是一个国家对另一个国家的公开评价，不能写成两个人斗嘴。整体要像中世纪国家文书的清楚现代中文译文：不写文言文、半文半白、现代新闻稿、现代法律或国际组织话术，也不堆砌官样套话。直接称呼别国统治者只限于个人誓约或最后通牒的一两句核心文字，其余部分仍由国家作为主语。");
		sb.AppendLine("按内容自然分段，不强求固定段数；正文超过约180字时至少分成两段，不能挤成一个大段落。贡金、停战期限、开放商路、割地或盟约条件应写成完整而清晰的国家主张；没有新条件时宁可简短明确，也不要用空话扩写。标题应抓住国家决定或外交事件本身，避免万能标题和收信人格式。");
		sb.AppendLine("不要把供决策的后台态势照抄进正文。不能说战争进展领先多少分、议和开放度或劣势评分达到多少、关系点和战力值是多少；应改成由战报和现实结果支撑的自然判断。精确贡金、停战期限及其他正式条款不受此限制。");
		sb.AppendLine("地理称谓必须服从用户消息中的当前地理关系：只有明确标为接壤的两国才可互称邻国、边境国家或声称拥有共同边界；标为不接壤时，即使关系密切、同属一种文化、曾经统治相邻领土或正在参与同一场交涉，也不得写成邻国或边界争端。不得把供判断的距离档位和地图距离写进正文。");
	}

	private static string BuildDiplomaticDeclarationModeContract()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【统一任务：公开外交宣言】根据用户消息提供的档案，为当前发布国起草一篇由其统治者授权或署名、面向诸国与本国贵族臣民颁布的外交宣言。连续公文与开场公文使用相同输出结构；开场时程序没有预选对象、议题或动作，由发布国在同次输出中自主决定。");
		sb.AppendLine("公开事件和既有来文只是已发生背景，不是预定结果。依据发布国国家卡和真实利益，选择最符合本国战略的回应、条件、合作、施压、冲突、退出或维持现状；不要为凑出关系变化而行动，也不要只重复旧措辞。");
		sb.AppendLine("开场宣言可以明确指向一个或多个王国，也可以面向诸国而没有主要对象；没有主要对象时primary_target_kingdom_id必须为空字符串。连续公文只能把公开档案列出的传递参与国作为外交动作对象。");
		sb.AppendLine("不要使用现代国际组织、现代法律或现代媒体措辞。不得替玩家王国发言，不得编造输入中没有支持的领土、制度、亲属关系、战斗和硬事件。未具名商队、使节、地方官、巡逻队、边民或传闻只能写成报告、指控、争议或待核实事项，绝不能冒充已经改变游戏状态的事实。优先使用王国名、“我国”“王庭”或档案明确给出的政治共同体称谓。第一人称单数“我”原则上不超过两次，只能用于统治者承担个人誓言或责任；不能用它串起整篇文章。“本王”整篇最多一次。");
		sb.AppendLine("和平类意图受战争状态硬约束：propose_peace、accept_peace、reject_peace只能指向当前确实正在交战的王国。双方处于和平状态时，不得把政治分歧、敌对关系、历史内战或统一诉求写成正在交战、停战、议和、退出战争或战争补偿；可以改为声明、警告、通牒、贸易、结盟或其他符合现状的外交主张。");
		sb.AppendLine("commitment必须与意图一致：statement、condemn、warning用non_binding；ultimatum、apology、concession、declare_war、break_alliance、cancel_trade用binding；propose_*用proposal；accept_*用acceptance；reject_*用rejection。除普通声明、谴责或警告外，正式动作必须填写primary_target_kingdom_id。");
		sb.AppendLine("标题应简洁概括事件或决定，通常不超过20个汉字。requires_response只表示正文提出了尚待回答的新提案、反提案、最后通牒或明确问题；接受、拒绝、道歉、普通声明、普通谴责和结束性立场必须为false。");
		sb.AppendLine("连续公文模式必须填写round_participation、round_status和made_progress；开场或普通模式固定使用continue、continue和true，这些字段只供内部整理外交事件，不能写入玩家可见正文。");
		sb.AppendLine("用户消息含“同次确定本次外交事件参与国”时，必须填写round_plan：topic概括本次事件真实议题，selected_kingdom_ids从候选简表中选择，并包含宣言明确指向的王国。没有该段时round_plan使用空标题和空数组。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏：");
		sb.AppendLine("{\"title\":\"简短外交宣言标题\",\"body\":\"自然分段的完整外交宣言正文\",\"author_intent\":{\"intent\":\"statement|condemn|warning|ultimatum|apology|concession|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\"},\"responding_to_offer_document_id\":\"接受或拒绝时填写来源公文ID，否则为空\",\"primary_target_kingdom_id\":\"主要对象ID或空\",\"addressed_kingdom_ids\":[\"ID\"],\"mentioned_kingdom_ids\":[\"ID\"],\"requires_response\":false,\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"round_participation\":\"continue|withdraw\",\"round_status\":\"continue|resolved|deadlocked\",\"made_progress\":true,\"round_plan\":{\"topic\":\"简短议题或空\",\"selected_kingdom_ids\":[\"ID\"]},\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID或空\",\"tribute_receiver_kingdom_id\":\"ID或空\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID或空\",\"cession_to_kingdom_id\":\"ID或空\",\"cession_settlement_id\":\"允许清单中的ID或空\"}}");
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

	private string BuildAutonomousOpeningPrompt(Kingdom author, string roundId, List<string> candidateIds)
	{
		if (author == null) return "";
		string authorId = author.StringId;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【发文者稳定档案】");
		sb.AppendLine("发文国：" + KingdomName(author) + "（ID=" + authorId + "），统治者：" + RulerName(author));
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		sb.AppendLine("【发文者人格与声音】");
		sb.AppendLine(BuildRulerVoiceContext(author));
		sb.AppendLine("你是在为上述王国起草由这位具体统治者授权或署名的国家公文，不是在扮演通用的国王模板。人格应体现在他重视什么、相信谁、如何评价对手、愿意付出什么代价，以及威胁或让步时的分寸；国家立场仍须以王国、王庭、贵族与臣民的共同利益来表达，不要只靠称号、古词和口号表现身份。");
		sb.AppendLine("【发文国制度、合法性与礼制声音】");
		sb.AppendLine(BuildRealmInstitutionalVoiceContext(author));
		sb.AppendLine("这部分规定国家如何称呼自身、凭什么宣称权威及惯用何种政治语言。其中ruler_title_hard_fact和government_hard_fact来自当前游戏身份与王国硬事实，优先级高于imported_lore；imported_lore只能补充语气和历史背景，不得改写政体或统治者头衔。未命中时保持笼统，不得自行发明元老院、议会、部族大会、宗教机关或贵族表决。");
		sb.AppendLine("【权威人物与亲属关系】");
		sb.AppendLine(BuildAuthorRulerFamilyContext(author));
		sb.AppendLine("这一段来自当前游戏对象，优先级高于人物背景、世界观常识、近期宣言和模型记忆。只有被明确列出的父母、配偶、子女或双方直接关系才可当作亲属事实；不得把同文化、同阵营或其他王室成员自动认作家人。亲属信息只有在王朝继承、联姻、人质、王室安全或明确牵涉该亲属的外交事件中才能写入正文；其余情况必须忽略，不得用‘我的女儿’等家事补充国家立场。");
		string policySnapshot = WorldDiplomacyPolicyContext.BuildSnapshot(authorId);
		if (!string.IsNullOrWhiteSpace(policySnapshot))
		{
			sb.AppendLine("【发文国政策快照】");
			sb.AppendLine(policySnapshot);
			sb.AppendLine("政策用于判断当前政治目标、利益和压力，不代表政策已经取得任何未明确提供的外交或军事结果。");
		}
		WorldDiplomacyRound round = ResolveRound(roundId);
		if (!string.IsNullOrWhiteSpace(round?.ExternalOpeningContext))
		{
			sb.AppendLine("【已经发生的外部外交事件】");
			sb.AppendLine(Limit(round.ExternalOpeningContext, 1800));
			sb.AppendLine("这是可供本国利用或回应的真实事件，但不预定本国的对象、立场或行动。");
		}
		sb.AppendLine("【同次确定本次外交事件参与国】");
		sb.AppendLine("依据发文国国家卡和当前真实局势，自主决定主要对象、公开议题、外交意图与参与国；涉及战争升级时必须考虑军力对比。可以先制造公开理由、提出条件或寻求合作。主要对象只能使用候选ID。");
		sb.AppendLine("在同一个JSON中填写round_plan。本次参与国总数上限（包括发起国）=" + GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。直接指向的国家必须列入selected_kingdom_ids；只选择确实需要进入本次连续公文的国家，不要凑满。");
		sb.AppendLine("【可选择的外交对象与即时硬事实】");
		foreach (string id in candidateIds ?? new List<string>())
		{
			Kingdom candidate = ResolveKingdom(id);
			if (candidate == null || candidate == author || candidate.IsEliminated || !HasIndependentWorldDiplomacyAuthority(candidate)) continue;
			sb.AppendLine(BuildCompactRoundPlanCandidateLine(author, candidate));
			if (FactionManager.IsAtWarAgainstFaction(author, candidate))
			{
				sb.AppendLine("  战争判断=" + CompactPromptFact(BuildWarNegotiationContext(author, candidate), 900));
			}
		}
		int activity = GetActivityLevel();
		sb.AppendLine(activity switch
		{
			0 => "外交活跃程度为低：可以克制或只公布立场，但仍应服从国家自身战略。",
			2 => "外交活跃程度为高：更积极寻找推进国家目标的外交机会，但不得无理由发动战争。",
			_ => "外交活跃程度为标准：根据国家目标和局势，自主选择合作、施压、冲突或暂不改变关系。"
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
		List<string> roundPlanCandidateIds)
	{
		if (author == null) return "";
		if (target == null && !isResponse)
		{
			return BuildAutonomousOpeningPrompt(author, roundId, roundPlanCandidateIds);
		}
		if (target == null) return "";
		string authorId = author.StringId;
		string targetId = target.StringId;
		int pressure = GetWarPressure(authorId, targetId);
		int relation = GetRulerRelation(author, target);
		int culturalFiefs = CountCulturalClaims(author, target);
		string currentState = BuildBilateralState(author, target);
		string rulerVoiceContext = BuildRulerVoiceContext(author);
		string realmInstitutionalVoiceContext = BuildRealmInstitutionalVoiceContext(author);
		string authorFamilyContext = BuildAuthorRulerFamilyContext(author);
		string bilateralFamilyContext = BuildBilateralRulerFamilyContext(author, target);
		string recentBattleContext = BuildRecentBilateralBattleContext(author, target);
		string nativeReasons = BuildRecentNativeSignalContext(authorId, targetId);
		string policySnapshot = WorldDiplomacyPolicyContext.BuildSnapshot(authorId);
		string resolvedRoundId = FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId);
		WorldDiplomacyRound activeRound = ResolveRound(resolvedRoundId);
		List<string> relevantKingdomIds = new List<string> { authorId, targetId };
		if (activeRound?.RelayRouteKingdomIds != null) relevantKingdomIds.AddRange(activeRound.RelayRouteKingdomIds);
		string gatheringSnapshot = NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(relevantKingdomIds, 3);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【发文者稳定档案】");
		sb.AppendLine("发文国：" + KingdomName(author) + "（ID=" + authorId + "），统治者：" + RulerName(author));
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		sb.AppendLine("【发文者人格与声音】");
		sb.AppendLine(rulerVoiceContext);
		sb.AppendLine("你是在为上述王国起草由这位具体统治者授权或署名的国家公文，不是在扮演通用的国王模板。人格应体现在他重视什么、相信谁、如何评价对手、愿意付出什么代价，以及威胁或让步时的分寸；国家立场仍须以王国、王庭、贵族与臣民的共同利益来表达，不要只靠称号、古词和口号表现身份。");
		sb.AppendLine("【发文国制度、合法性与礼制声音】");
		sb.AppendLine(realmInstitutionalVoiceContext);
		sb.AppendLine("这部分规定国家如何称呼自身、凭什么宣称权威及惯用何种政治语言。其中ruler_title_hard_fact和government_hard_fact来自当前游戏身份与王国硬事实，优先级高于imported_lore；imported_lore只能补充语气和历史背景，不得改写政体或统治者头衔。未命中时保持笼统，不得自行发明元老院、议会、部族大会、宗教机关或贵族表决。");
		sb.AppendLine("【权威人物与亲属关系】");
		sb.AppendLine(authorFamilyContext);
		sb.AppendLine("这一段来自当前游戏对象，优先级高于人物背景、世界观常识、近期宣言和模型记忆。只有被明确列出的父母、配偶、子女或双方直接关系才可当作亲属事实；不得把同文化、同阵营或其他王室成员自动认作家人。亲属信息只有在王朝继承、联姻、人质、王室安全或明确牵涉该亲属的外交事件中才能写入正文；其余情况必须忽略，不得用‘我的女儿’等家事补充国家立场。");
		if (!string.IsNullOrWhiteSpace(policySnapshot))
		{
			sb.AppendLine("【发文国政策快照】");
			sb.AppendLine(policySnapshot);
			sb.AppendLine("政策用于判断当前政治目标、利益和压力，不代表政策已经取得任何未明确提供的外交或军事结果。");
		}
		sb.AppendLine("【本次双边对象与即时事实】");
		sb.AppendLine("主要对象国：" + KingdomName(target) + "（ID=" + targetId + "），统治者：" + RulerName(target));
		if (allowUntargeted)
		{
			sb.AppendLine("该对象国只是帮助构造开场局势的参考，不要求宣言必须指向它。若统治者更适合面向诸国提出外交议题，请把primary_target_kingdom_id留空，并让addressed_kingdom_ids为空。");
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
		sb.AppendLine(bilateralFamilyContext);
		sb.AppendLine("【近期双边战斗硬事实】");
		sb.AppendLine(recentBattleContext);
		sb.AppendLine("只能引用本段明确列出的具体战斗。不得自行补写战斗地点、参战领主、胜负、投入兵力、伤亡数字、俘虏或连续战果；参战领主名单不表示他们已经被俘。战争状态、战争压力和敌对态度本身不证明某场具体战斗已经发生。");
		sb.AppendLine("当前关系：" + currentState);
		if (FactionManager.IsAtWarAgainstFaction(author, target))
		{
			sb.AppendLine("硬性状态：双方已经处于战争中，本次不得再次宣布开战，也不得把既有战争写成刚刚开始。可以评论战局、提出或拒绝议和、要求贡金/归还失地，或发表其他战时立场。");
			sb.AppendLine(BuildWarNegotiationContext(author, target));
		}
		else
		{
			sb.AppendLine("硬性状态：双方当前没有战争。不得使用propose_peace、accept_peace、reject_peace，不得声称双方需要停战、议和、退出战争或支付战争补偿。历史敌意、帝国分裂和统一诉求不等于当前战争。");
			WarSituationSnapshot peacetimeSituation = GetWarSituation(author, target);
			sb.AppendLine("双方总体军力=" + DescribeStrengthBalance(peacetimeSituation.AuthorStrength, peacetimeSituation.TargetStrength)
				+ "。这是仅供统治者决策的综合军情，不得公开具体战力数值。");
		}
		sb.AppendLine("统治者私人关系：" + DescribeRulerRelation(relation));
		sb.AppendLine("对象国占有的发文国文化城镇/城堡数量：" + culturalFiefs.ToString(CultureInfo.InvariantCulture));
		sb.AppendLine("边境与政治压力：" + DescribeWarPressure(pressure) + "。这只是王庭的综合判断，不得在公开正文中写成分数、进度或门槛。");
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine("近期原版外交动机素材：");
			sb.AppendLine(nativeReasons);
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
			sb.AppendLine("【同次确定本次外交事件参与国】");
			sb.AppendLine("在起草开场宣言的同时填写round_plan。本次参与国总数上限（包括发起国）=" + GetRoundParticipantLimit().ToString(CultureInfo.InvariantCulture) + "。宣言明确指向的王国必须优先入选；其余只选确有战争、同盟、贸易、安全或政治利益且能够采取外交行为者，不要为了热闹选满。候选简表：");
			foreach (string candidateId in roundPlanCandidateIds)
			{
				Kingdom candidate = ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(BuildCompactRoundPlanCandidateLine(author, candidate));
			}
		}
		if (activeRound != null)
		{
			int age = Math.Max(0, CurrentDay() - activeRound.StartedDay);
			sb.AppendLine("当前外交事件已经持续" + age.ToString(CultureInfo.InvariantCulture) + "天，软时间尺度为" + Math.Max(1, activeRound.SoftEndDay - activeRound.StartedDay).ToString(CultureInfo.InvariantCulture) + "天。接近或超过软尺度时，统治者应更重视收束重复争论、给出最终立场或停止无意义往返；但尚未解决且直接关系本国利益的正式问题不能被假装遗忘。");
		}
		if (isResponse && sourceDocument != null)
		{
			sb.AppendLine("你必须回应下列公开外交宣言，可以接受、拒绝、反驳、缓和、追问或提出反条件：");
			sb.AppendLine("来源公文ID：" + sourceDocument.DocumentId + "；提出国=" + sourceDocument.AuthorKingdomId + "；对象国=" + sourceDocument.TargetKingdomId + "。只有对象国可以接受或拒绝这项提议。");
			sb.AppendLine("标题：" + sourceDocument.Title);
			sb.AppendLine("正文：" + Limit(sourceDocument.Body, 2200));
		}
		if (isReminder)
		{
			sb.AppendLine("这是因为对象国统治者迟迟没有回应而发布的第二篇宣言。正文必须明确催促答复，或对沉默表示不满、质疑、指责；不得假定对方已经接受，也不要替对方作答。");
		}
		int activity = GetActivityLevel();
		sb.AppendLine(activity switch
		{
			0 => "外交活跃程度为低：优先克制、审慎和现实利益，但严重矛盾仍可升级。",
			2 => "外交活跃程度为高：应更积极提出可回应的主张、合作或冲突方案，但不得无理由发动战争。",
			_ => "外交活跃程度为标准：在讨论、合作、冲突和正式行动之间按局势自然选择。"
		});
		return sb.ToString();
	}

	private string BuildCompactRoundPlanCandidateLine(Kingdom initiator, Kingdom candidate)
	{
		string policy = CompactPromptFact(WorldDiplomacyPolicyContext.BuildSnapshot(candidate.StringId), 180);
		StringBuilder sb = new StringBuilder();
		WorldDiplomacyRealmRelationProfile relationProfile = GetRealmRelationProfile(initiator, candidate);
		WorldDiplomacyBorderRelation border = GetKingdomBorderRelation(initiator, candidate);
		WarSituationSnapshot strengthSituation = GetWarSituation(initiator, candidate);
		sb.Append("- ").Append(candidate.StringId).Append('=').Append(KingdomName(candidate))
			.Append("；与发起国=").Append(BuildBilateralState(initiator, candidate))
			.Append("；两国贵族整体关系=").Append(DescribeRealmRelationProfile(relationProfile))
			.Append("；统治者私人关系=").Append(DescribeRulerRelation(GetRulerRelation(initiator, candidate)))
			.Append("；地理关系=").Append(border.SharesBorder ? DescribeBorderRelation(border) : "不接壤")
			.Append("；总体军力=").Append(DescribeStrengthBalance(strengthSituation.AuthorStrength, strengthSituation.TargetStrength))
			.Append("；当前可改变关系的方向=").Append(DescribePotentialDiplomaticActions(BuildPotentialDiplomaticActionIntents(initiator, candidate)));
		if (!string.IsNullOrWhiteSpace(policy)) sb.Append("；政策倾向=").Append(policy);
		return sb.ToString();
	}

	private string BuildWarNegotiationContext(Kingdom author, Kingdom target)
	{
		WarSituationSnapshot snapshot = GetWarSituation(author, target);
		if (snapshot?.IsAtWar != true) return "";
		List<Settlement> targetCanCede = BuildCessionCandidates(target, author, snapshot.TargetCessionScore);
		List<Settlement> authorCanCede = BuildCessionCandidates(author, target, snapshot.AuthorCessionScore);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【仅供统治者判断的战争与议和态势】战争已经" + DescribeWarDuration(snapshot.WarDays)
			+ "；发文国所受议和压力=" + DescribePeacePressure(snapshot.AuthorPeacePressure)
			+ "；对象国所受议和压力=" + DescribePeacePressure(snapshot.TargetPeacePressure) + "。");
		sb.AppendLine("双方总体军力=" + DescribeStrengthBalance(snapshot.AuthorStrength, snapshot.TargetStrength)
			+ "；近期战局=" + DescribeWarProgress(snapshot.AuthorProgress, snapshot.TargetProgress)
			+ "；发文国=" + DescribeOtherWarBurden(snapshot.AuthorOtherWars)
			+ "；对象国=" + DescribeOtherWarBurden(snapshot.TargetOtherWars) + "。这些是综合判断，只能转写成世界内措辞，不得公开任何评分、分差、开放度或战力数值。");
		sb.AppendLine("贡金可与割地并存。参考每日贡金：若发文国付款约" + snapshot.AuthorSuggestedTribute + "，若对象国付款约" + snapshot.TargetSuggestedTribute + "；可以谈判但不得超出任务给出的合法上限。");
		sb.AppendLine("对象国当前可合法提出割让给发文国的领地=" + FormatCessionCandidates(targetCanCede) + "；发文国当前可合法提出割让给对象国的领地=" + FormatCessionCandidates(authorCanCede) + "。清单为空时不得提出或同意割地，也不得编造城名；优先考虑战争中尚未收复的失地。城镇只有在战局严重不利时才会进入清单。");
		return sb.ToString().TrimEnd();
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
		sb.AppendLine("读懂已经发布的宣言表达了什么，不替作者决定世界局势；玩家文风偏好不参与语义裁判。");
		sb.AppendLine("区分正式宣战与威胁：明确终结和平、宣布战争状态、命令军队越境或用刀剑取代外交，可判为declare_war；“不要逼迫我们开战”“我们不惧战争”只是warning或ultimatum。");
		sb.AppendLine("公文可以只是讨论或公开表态。没有正式行为时必须返回statement、condemn、warning等，而不是判定失败。");
		sb.AppendLine("若材料列出当前待本国答复的正式提案，宣言明确接受或拒绝其中一项时，intent必须使用对应的accept_*或reject_*，primary_target_kingdom_id必须是原提出国，并把来源公文ID填入responding_to_offer_document_id。提出不同条件属于新反提案，应使用propose_*并把该字段留空。");
		sb.AppendLine("同时生成title_summary：以发文国统治者的立场简洁概括公告核心，不使用书信标题，不超过20个汉字。");
		sb.AppendLine("addressed_kingdom_ids列出被直接点名、要求答复或承受正式主张的国家；mentioned_kingdom_ids只列被谈及但未被直接要求回应的国家。只允许使用用户消息给出的王国ID。");
		sb.AppendLine("议和公文可同时含贡金与割地。peace_terms只提取正文明确写出的条件；领地必须来自允许清单，清单为空就留空，不能自由同意不合法割地。");
		sb.AppendLine("只输出一个JSON对象，不要解释或代码围栏：");
		sb.AppendLine("{\"status\":\"success\",\"title_summary\":\"公告要点标题\",\"responding_to_offer_document_id\":\"来源公文ID或空字符串\",\"primary_target_kingdom_id\":\"王国ID或空字符串\",\"addressed_kingdom_ids\":[\"王国ID\"],\"mentioned_kingdom_ids\":[\"王国ID\"],\"intent\":\"statement|condemn|warning|ultimatum|apology|concession|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\",\"requires_response\":true,\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID或空\",\"tribute_receiver_kingdom_id\":\"ID或空\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID或空\",\"cession_to_kingdom_id\":\"ID或空\",\"cession_settlement_id\":\"ID或空\"}}");
		return sb.ToString().TrimEnd();
	}

	private string BuildAnalysisPrompt(WorldDiplomacyDocument document)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("发文国：" + document.AuthorKingdomName + "（ID=" + document.AuthorKingdomId + "）");
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
			if (author != null && candidateTarget != null && FactionManager.IsAtWarAgainstFaction(author, candidateTarget)) sb.AppendLine(BuildWarNegotiationContext(author, candidateTarget));
		}
		if (document.IsPlayerAuthored)
		{
			WorldDiplomacyRound round = ResolveRound(document.RoundId);
			List<WorldDiplomacyRoundOffer> openOffers = (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
				.Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => x.CreatedDay)
				.Take(4)
				.ToList();
			if (openOffers.Count > 0)
			{
				sb.AppendLine("当前待本国正式答复的提案：");
				foreach (WorldDiplomacyRoundOffer offer in openOffers)
				{
					WorldDiplomacyDocument source = ResolveDocument(offer.SourceDocumentId);
					sb.AppendLine("- 来源=" + offer.SourceDocumentId + "|类型=" + offer.Intent
						+ "|提出国=" + offer.ProposerKingdomId + "=" + KingdomName(ResolveKingdom(offer.ProposerKingdomId))
						+ "|标题=" + Limit(source?.Title, 80) + "|要点=" + Limit(source?.Body, 240));
				}
				sb.AppendLine("若正文是在接受或拒绝上列提案，必须绑定对应来源；若只是评论或另提条件，不得伪装成接受或拒绝。");
			}
		}
		WorldDiplomacyDocument sourceDocument = ResolveDocument(document.SourceDocumentId);
		if (sourceDocument != null)
		{
			sb.AppendLine("该公文正在回应：");
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
		WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
		string intent = InferIntentFromExplicitPhrases(document?.Body);
		if (string.IsNullOrWhiteSpace(intent))
		{
			intent = FirstNonEmpty(document?.HiddenIntent, "statement");
		}
		return new JObject
		{
			["status"] = "fallback",
			["title_summary"] = BuildFallbackDocumentTitle(document, intent),
			["primary_target_kingdom_id"] = FirstNonEmpty(document?.TargetKingdomId, job.TargetKingdomId),
			["intent"] = intent,
			["commitment"] = intent == "declare_war" ? "binding" : FirstNonEmpty(document?.HiddenCommitment, "non_binding"),
			["requires_response"] = IsProposalIntent(intent) || intent == "warning" || intent == "ultimatum",
			["tone"] = intent == "declare_war" || intent == "warning" || intent == "ultimatum" ? "hostile" : "neutral",
			["confidence"] = 0.55
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
		if (CanDeclareWar(first, second, out _)) actions.Add("declare_war");
		if (alliance != null) actions.Add(allied ? "break_alliance" : "propose_alliance");
		if (trade != null) actions.Add(trading ? "cancel_trade" : "propose_trade");
		return actions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string DescribePotentialDiplomaticActions(IEnumerable<string> intents)
	{
		List<string> labels = (intents ?? Enumerable.Empty<string>())
			.Select(NormalizeIntent)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(IntentLabel)
			.ToList();
		return labels.Count == 0 ? "当前没有可直接执行的关系变更，但仍可提出条件、道歉、让步或退出" : string.Join("、", labels);
	}

	private string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round, Kingdom author)
	{
		if (round == null || author == null) return "当前合法外交出口：无。";
		List<string> lines = new List<string>();
		foreach (string id in (round.RelayRouteKingdomIds ?? new List<string>())
			.Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom target = ResolveKingdom(id);
			if (target == null) continue;
			List<string> actions = BuildPotentialDiplomaticActionIntents(author, target);
			if (actions.Count == 0) continue;
			lines.Add(id + "=" + KingdomName(target) + "：" + string.Join("、", actions.Select(x => x + "(" + IntentLabel(x) + ")")));
		}
		return lines.Count == 0
			? "当前合法外交出口：没有可立即改变关系的行为；仍可提出条件、道歉、让步、拒绝或退出。"
			: "【当前可合法提出或执行的关系变更；不是命令，也不代表已经生效】\n" + string.Join("\n", lines);
	}

	private List<Kingdom> GetEligibleAiKingdoms()
	{
		Kingdom player = Clan.PlayerClan?.Kingdom;
		return Kingdom.All
			.Where(x => x != null
				&& !x.IsEliminated
				&& HasIndependentWorldDiplomacyAuthority(x)
				&& x.RulingClan?.Leader != null
				&& x.RulingClan.Leader.IsAlive
				&& x != player)
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
		string respondingToOfferDocumentId = null)
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
			AuthorKingdomId = (authorKingdomId ?? "").Trim(),
			TargetKingdomIds = (targetKingdomIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Intent = NormalizeIntent(intent),
			Commitment = NormalizeCommitment(commitment),
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
		if (!document.HistoryDeclarationRecorded && !string.IsNullOrWhiteSpace(document.Body))
		{
			string declarationSourceKey = "document:" + document.DocumentId + ":declaration";
			bool appended = AppendCanonicalHistoryEntry("declaration", declarationSourceKey, document.DocumentId,
				document.Day, document.GameDate, document.AuthorKingdomId, targets, document.Intent, document.Commitment, document.Body,
				verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId);
			if (appended || CanonicalDeltaContainsSourceKey(declarationSourceKey))
			{
				document.HistoryDeclarationRecorded = true;
			}
		}
		if (!document.HistoryResultRecorded && (document.ChangedDiplomaticState || externalResolvedFact) && !string.IsNullOrWhiteSpace(document.MechanicalResult))
		{
			string resultText = "经游戏机制确认：" + document.MechanicalResult;
			string resultSourceKey = "document:" + document.DocumentId + ":result";
			bool appended = AppendCanonicalHistoryEntry("diplomatic_result", resultSourceKey, document.DocumentId,
				document.Day, document.GameDate, document.AuthorKingdomId, targets, document.Intent, document.Commitment, resultText,
				verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId);
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
					RelatedSourceId = entry.RespondingToOfferDocumentId,
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
			if (!UsesCanonicalHistory(job)) continue;
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
			WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
			string commonContract = GetCommonDiplomacyContract(round);
			job.SystemPrompt = job.IsRelayTurn ? BuildRelayGenerationSystemPrompt(commonContract) : BuildGenerationSystemPrompt(commonContract);
			List<string> candidates = job.CandidateKingdomIds ?? new List<string>();
			if (job.IsRelayTurn && round == null) return false;
			string dynamicPrompt = job.IsRelayTurn
				? BuildRelayConversationTurnPrompt(round, author, target,
					prioritySource: ResolveDocument(job.SourceDocumentId), priorityResponseOnly: job.IsExternalResponseOnly)
				: BuildGenerationPrompt(author, target, ResolveExchange(job.ExchangeId), job.IsResponse, ResolveDocument(job.SourceDocumentId), job.IsReminder, job.RoundId, job.AllowUntargeted, candidates);
			job.UserPrompt = BuildDeclareModePrompt(dynamicPrompt);
			job.CacheAffinityKey = CanonicalHistoryCacheAffinityKey;
			job.ProfiledKingdomId = "";
			job.StrategicProfileKingdomId = author.StringId;
			job.MaxTokens = GenerationMaxTokens;
			CaptureCanonicalHistoryForJob(job, syncSources: false);
			return !string.IsNullOrWhiteSpace(job.UserPrompt);
		}
		if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
		{
			WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
			if (document == null) return false;
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
			.Take(Math.Max(1, Math.Min(200, maxCount)))
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
				active.TopicCategory = InferTopicCategory(active.RoundTopic, rootAuthor, ResolveKingdom(root.TargetKingdomId));
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
		_storage.CompressionRetryAfterHour = Math.Max(0, _storage.CompressionRetryAfterHour);
		_storage.CompressionRetryAttempts = Math.Max(0, Math.Min(31, _storage.CompressionRetryAttempts));
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
			document.Title = Limit(SanitizePublicDiplomacyText(document.Title), 100);
			document.Body = NormalizeBody(SanitizePublicDiplomacyText(document.Body));
			document.AddressedKingdomIds ??= new List<string>();
			document.MentionedKingdomIds ??= new List<string>();
			document.PlannedKingdomIds ??= new List<string>();
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
			job.LlmMessages ??= new List<WorldDiplomacyLlmMessage>();
			job.CompressionRoundIds ??= new List<string>();
			job.ForcedIntent = "";
		}
		foreach (WorldDiplomacyRound round in _storage.CompletedRounds.Concat(_storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { _storage.ActiveRound }).Where(x => x != null))
		{
			round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
			round.RelayRouteKingdomIds ??= new List<string>();
			round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
			round.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
			round.LlmTranscript.Clear();
			round.LlmProfiledKingdomIds ??= new List<string>();
			round.ExternalSignalKeys ??= new List<string>();
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
			foreach (WorldDiplomacyRoundParticipant participant in round.Participants.Where(x => x != null)) participant.MandatoryReplyPending = false;
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

	private WorldDiplomacyDocument ResolveDocument(string documentId)
	{
		if (string.IsNullOrWhiteSpace(documentId))
		{
			return null;
		}
		return _storage.Documents.FirstOrDefault(x => x != null && string.Equals(x.DocumentId, documentId, StringComparison.OrdinalIgnoreCase));
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
		string target = string.IsNullOrWhiteSpace(document.TargetKingdomName) ? "" : "致" + document.TargetKingdomName;
		string result = string.IsNullOrWhiteSpace(document.MechanicalResult) ? "" : "；结果：" + document.MechanicalResult;
		string date = string.IsNullOrWhiteSpace(document.GameDate) ? FormatCampaignDate(document.Day) : document.GameDate;
		return date
			+ "，"
			+ document.AuthorKingdomName
			+ target
			+ "发布《"
			+ document.Title
			+ "》（"
			+ IntentLabel(document.Intent)
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

	private static float GetNativeIntentMultiplier()
	{
		try
		{
			return Math.Max(0.5f, Math.Min(2f, (DuelSettings.GetSettings()?.WorldDiplomacyNativeIntentInfluencePercent ?? 100) / 100f));
		}
		catch
		{
			return 1f;
		}
	}

	private static float GetDocumentInfluenceMultiplier()
	{
		try
		{
			return Math.Max(0f, Math.Min(2f, (DuelSettings.GetSettings()?.WorldDiplomacyDocumentInfluencePercent ?? 100) / 100f));
		}
		catch
		{
			return 1f;
		}
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

	private static bool IsPlayerKingdom(Kingdom kingdom)
	{
		return kingdom != null && kingdom == Clan.PlayerClan?.Kingdom && kingdom.RulingClan?.Leader == Hero.MainHero;
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
			return JObject.Parse(text);
		}
		catch
		{
			int start = text.IndexOf('{');
			int end = text.LastIndexOf('}');
			if (start >= 0 && end > start)
			{
				try
				{
					return JObject.Parse(text.Substring(start, end - start + 1));
				}
				catch
				{
				}
			}
			return new JObject();
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

	private static string NormalizeTone(string value)
	{
		string token = NormalizeToken(value);
		return token is "conciliatory" or "neutral" or "firm" or "hostile" ? token : "neutral";
	}

	private static string NormalizeToken(string value)
	{
		return (value ?? "").Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
	}

	private static string InferIntentFromExplicitPhrases(string body)
	{
		string text = body ?? "";
		if (ContainsAny(text, "正式宣战", "进入战争状态", "和平至此终结", "和平自今日起终结", "两国再无和平可言", "军队已奉命越过边境", "刀剑将代替使者", "刀剑将代替使节"))
		{
			return "declare_war";
		}
		if (ContainsAny(text, "接受和平", "接受议和", "同意停战", "同意和平"))
		{
			return "accept_peace";
		}
		if (ContainsAny(text, "提议和平", "请求议和", "愿意停战", "缔结和平"))
		{
			return "propose_peace";
		}
		if (ContainsAny(text, "接受同盟", "同意结盟", "接受结盟"))
		{
			return "accept_alliance";
		}
		if (ContainsAny(text, "提议结盟", "缔结同盟", "建立同盟"))
		{
			return "propose_alliance";
		}
		if (ContainsAny(text, "解除同盟", "终止同盟", "废除盟约"))
		{
			return "break_alliance";
		}
		if (ContainsAny(text, "接受贸易", "同意贸易协定", "缔结贸易协定"))
		{
			return "accept_trade";
		}
		if (ContainsAny(text, "提议贸易", "建立贸易协定", "商定贸易"))
		{
			return "propose_trade";
		}
		if (ContainsAny(text, "终止贸易协定", "取消贸易协定", "废除贸易协定"))
		{
			return "cancel_trade";
		}
		if (ContainsAny(text, "最后通牒", "限期", "否则后果自负"))
		{
			return "ultimatum";
		}
		if (ContainsAny(text, "警告", "不要迫使我们", "不会容忍"))
		{
			return "warning";
		}
		return "";
	}

	private static bool ContainsAny(string text, params string[] needles)
	{
		return (needles ?? Array.Empty<string>()).Any(x => !string.IsNullOrWhiteSpace(x) && (text ?? "").IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool IsImmediateIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "declare_war" || normalized == "break_alliance" || normalized == "cancel_trade";
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

	private static bool LooksLikeExplicitOfferRejection(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text,
			@"(?:拒绝|驳回|否决|不接受|不同意|绝不接受|不能接受|不予接受).{0,24}(?:提议|建议|条件|条约|和约|停战|议和|联盟|盟约|贸易|商路|协议|要求)|(?:提议|建议|条件|条约|和约|停战|议和|联盟|盟约|贸易|商路|协议|要求).{0,18}(?:不可接受|不能接受|予以拒绝|予以驳回)",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	}

	private static bool LooksLikeExplicitOfferAcceptance(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text,
			@"(?:接受|同意|批准|认可|答应|准予).{0,24}(?:提议|建议|条件|条约|和约|停战|议和|联盟|盟约|贸易|商路|协议|要求)|(?:提议|建议|条件|条约|和约|停战|议和|联盟|盟约|贸易|商路|协议|要求).{0,18}(?:可以接受|予以接受|予以批准|正式同意)",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	}

	private static bool LooksLikeExplicitCounterProposal(string text)
	{
		if (ContainsAny(text, "反提案", "反建议", "另提", "重新提出", "新的条件", "条件改为", "条件是", "条件为", "条件如下",
			"改为", "改成", "调整为", "变更为", "换成", "除非", "前提是", "作为前提", "作为交换",
			"可以，但", "愿意，但", "接受，但", "同意，但", "可以，不过", "愿意，不过", "接受，不过", "同意，不过")) return true;
		return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text,
			@"(?:只有|唯有|若要|倘若|必须|须先|应先)[^。；\n]{0,48}(?:才|方可|方能|之后|以后)|(?:接受|同意|愿意|可以)[^。；\n]{0,32}(?:但|不过|然而|条件(?:是|为|如下)|前提)",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	}

	private static string InferProposalIntentFromOfferResponseText(string text)
	{
		if (ContainsAny(text, "停战", "停火", "议和", "和谈", "休战", "和平条件", "和约", "战争赔偿")) return "propose_peace";
		if (ContainsAny(text, "结盟", "联盟", "盟约", "共同防御", "军事同盟")) return "propose_alliance";
		if (ContainsAny(text, "贸易", "商路", "通商", "关税", "商贸协议")) return "propose_trade";
		return "";
	}

	private static bool IsPeaceIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "propose_peace" || normalized == "accept_peace" || normalized == "reject_peace";
	}

	private static bool IsSupportedDiplomacyIntent(string intent)
	{
		return NormalizeIntent(intent) is "statement" or "condemn" or "warning" or "ultimatum" or "apology" or "concession"
			or "propose_peace" or "accept_peace" or "reject_peace"
			or "propose_alliance" or "accept_alliance" or "reject_alliance" or "break_alliance"
			or "propose_trade" or "accept_trade" or "reject_trade" or "cancel_trade" or "declare_war";
	}

	private static bool IsSupportedCommitment(string commitment)
	{
		return NormalizeCommitment(commitment) is "non_binding" or "proposal" or "acceptance" or "rejection" or "binding";
	}

	private static bool IsTerminalResponseIntent(string intent)
	{
		return NormalizeIntent(intent) is "accept_peace" or "reject_peace"
			or "accept_alliance" or "reject_alliance"
			or "accept_trade" or "reject_trade"
			or "apology" or "concession" or "break_alliance" or "cancel_trade" or "declare_war";
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
			|| string.Equals(NormalizeIntent(intent), "ultimatum", StringComparison.OrdinalIgnoreCase)
			|| modelRequestedResponse;
	}

	private static bool IsAcceptanceIntent(string intent)
	{
		string normalized = NormalizeIntent(intent);
		return normalized == "accept"
			|| normalized == "accept_peace"
			|| normalized == "accept_alliance"
			|| normalized == "accept_trade";
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
			"ultimatum" => "最后通牒",
			"warning" => "外交警告",
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
			"ultimatum" => "发出最后通牒",
			"warning" => "发出外交警告",
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
			"ultimatum" => "最后通牒",
			"warning" => "外交警告",
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
		string target = string.IsNullOrWhiteSpace(document.TargetKingdomName)
			? ""
			: " · " + document.TargetKingdomName;
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
		string title = SanitizePublicDiplomacyText(FirstNonEmpty(document?.Title, document?.AuthorKingdomName + "发布外交宣言", "外交宣言"));
		if (document == null || string.IsNullOrWhiteSpace(document.RoundId)) return title;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null) return title;
		bool isStart = string.Equals(round.RootDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase);
		bool isEnd = string.Equals(round.State, "closed", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(round.FinalDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase);
		if (isStart && isEnd) return "外交事件始末：" + title;
		if (isStart) return "外交事件开始：" + title;
		if (isEnd) return "外交事件结束：" + title;
		return title;
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
	[JsonProperty("externalOpeningContext")] public string ExternalOpeningContext { get; set; } = "";
	[JsonProperty("llmTranscript")] public List<WorldDiplomacyLlmMessage> LlmTranscript { get; set; } = new List<WorldDiplomacyLlmMessage>();
	[JsonProperty("llmProfiledKingdomIds")] public List<string> LlmProfiledKingdomIds { get; set; } = new List<string>();
	[JsonProperty("llmLastStateSignatureByKingdom")] public Dictionary<string, string> LlmLastStateSignatureByKingdom { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	[JsonProperty("roundStatus")] public string RoundStatus { get; set; } = "active";
	[JsonProperty("executedActionCount")] public int ExecutedActionCount { get; set; }
	[JsonProperty("substantiveProgressCount")] public int SubstantiveProgressCount { get; set; }
	[JsonProperty("diplomaticActionAttemptCount")] public int DiplomaticActionAttemptCount { get; set; }
	[JsonProperty("lastSubstantiveProgressDay")] public int LastSubstantiveProgressDay { get; set; }
	[JsonProperty("finalActionOpportunityIssued")] public bool FinalActionOpportunityIssued { get; set; }
	[JsonProperty("pendingOffers")] public List<WorldDiplomacyRoundOffer> PendingOffers { get; set; } = new List<WorldDiplomacyRoundOffer>();
	[JsonProperty("participants")] public List<WorldDiplomacyRoundParticipant> Participants { get; set; } = new List<WorldDiplomacyRoundParticipant>();
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
	[JsonProperty("proposerKingdomId")] public string ProposerKingdomId { get; set; } = "";
	[JsonProperty("targetKingdomId")] public string TargetKingdomId { get; set; } = "";
	[JsonProperty("intent")] public string Intent { get; set; } = "";
	[JsonProperty("status")] public string Status { get; set; } = "open";
	[JsonProperty("createdDay")] public int CreatedDay { get; set; }
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

public sealed class WorldDiplomacyDocument
{
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
