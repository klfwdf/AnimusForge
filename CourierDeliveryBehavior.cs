using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior : CampaignBehaviorBase
{
	private const string LogSource = "CourierDelivery";
	private const string SessionStorageKey = "_af_courier_sessions_v1";
	private const string NpcDiplomacyLetterStorageKey = "_af_courier_npc_diplomacy_letters_v1";
	private const string CourierLetterInventoryStorageKey = "_af_courier_letter_inventory_v1";
	private const int CourierLetterVelvetClanTier = 5;
	private const string CourierLetterLinenTemplateItemId = "linen";
	private const string CourierLetterVelvetTemplateItemId = "velvet";
	private const float MobilePartyArrivalDistance = 3.5f;
	private const float SenderArrivalDistanceSquared = 9f;
	private const float SettlementArrivalDistanceSquared = 1.44f;
	private const int MainReplyMaxTokens = 5000;
	private const double RouteRefreshSeconds = 2.0;
	private const double CampaignTickThrottleSeconds = 0.75;
	private const float CourierThreatAvoidanceMinimumScore = 1f;
	private const float CourierThreatAvoidanceMinimumMoveDistanceSquared = 0.25f;
	private const string CourierPartyPrefix = "af_courier_";
	private const string TemporaryCourierShipName = "AnimusForge Courier Boat";
	private const double NavalStuckRefreshHours = 12.0;
	private const float NavalStuckDistanceSquared = 0.0625f;
	private const int NavalStuckSafeRouteThreshold = 3;
	private const string CourierDirectionOutbound = "Outbound";
	private const string CourierDirectionInboundToPlayer = "InboundToPlayer";
	private const string InboundLetterKindDiplomacy = "Diplomacy";
	private const string InboundLetterKindPersonal = "Personal";
	private const string InboundLetterKindNeedRequest = "NeedRequest";
	private const string InboundLetterKindExternal = "External";
	private const string LetterMotiveGreeting = "Greeting";
	private const string LetterMotiveStatus = "Status";
	private const string LetterMotiveEvent = "Event";
	private const string LetterMotiveEmotion = "Emotion";
	private const string LetterMotiveNeed = "Need";
	private const string LetterMotiveDiplomacy = "Diplomacy";
	private const float NpcDiplomacyLetterScanIntervalHours = 12f;
	private const float NpcDiplomacyLetterGlobalCooldownDays = 5f;
	private const float NpcDiplomacyLetterSenderCooldownDays = 21f;
	private const float NpcDiplomacyLetterSendChance = 0.25f;
	private const int NpcInitiatedLetterScanTargetTicks = 45;
	private const int NpcInitiatedLetterScanMaxHeroesPerTick = 16;
	private const double NpcInitiatedLetterScanFrameBudgetMilliseconds = 1.0;
	private static readonly string[] CourierExcludedRuleIds = new[] { "duel", "meeting_taunt", "lords_hall_access", "scene_mechanism_actions", "encounter_release_player", "noble_deference" };

	private enum CourierStage
	{
		Outbound,
		WaitingRecipient,
		GeneratingReply,
		Returning,
		WaitingSender,
		Completed,
		Destroyed
	}

	private enum CourierPayloadMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners,
		GiveSettlements
	}

	private sealed class CourierSession
	{
		public string Id;
		public string Direction = CourierDirectionOutbound;
		public string SenderHeroId;
		public string SenderName;
		public string RecipientHeroId;
		public string RecipientName;
		public string CourierPartyId;
		public string Stage = CourierStage.Outbound.ToString();
		public string PayloadMode = CourierPayloadMode.Normal.ToString();
		public string LetterText;
		public string DeliveryFactText;
		public string ReplyText;
		public string ReplyPostprocessedText;
		public string InboundLetterKind;
		public string InboundMotiveType;
		public string InboundNeedType;
		public string InboundIntentFact;
		public string InboundIntentText;
		public string InboundFallbackLetter;
		public string InboundEventKey;
		public bool IsNpcInitiated;
		public int NpcInitiatedBondScore;
		public string LastRouteKey;
		public string TemporaryShipHullId;
		public bool TemporaryShipCreated;
		public string LastProgressRouteKey;
		public float LastProgressX;
		public float LastProgressY;
		public double LastProgressCampaignHours;
		public int NavalStuckRefreshCount;
		public string SafeSettlementId;
		public string RecipientWaitReason;
		public bool DeliveryApplied;
		public bool ReplyGenerated;
		public bool ReplyGenerationStarted;
		public bool ReplyPopupShown;
		public bool PostprocessConsumed;
		public bool ReplyWaitPopupShown;
		public string InboundCompletionReceipt;
		public int EscrowGold;
		public List<CourierCargoEntry> Entries = new List<CourierCargoEntry>();
		public List<CourierCargoEntry> CrewEntries = new List<CourierCargoEntry>();
	}

	private sealed class CourierCargoEntry
	{
		public string Kind;
		public string Id;
		public string Name;
		public int Amount;
		public int GuidePriceDenars;
		public bool IsHero;
		public string SourceSettlementId;
		public bool Delivered;
	}

	private sealed class NpcDiplomacyLetterStorage
	{
		public Dictionary<string, float> SenderCooldownUntilDays { get; set; }
		public Dictionary<string, float> MotiveFatigueUntilDays { get; set; }
		public Dictionary<string, string> LastDeliveredEventKeyBySender { get; set; }
		public float GlobalCooldownUntilDays { get; set; }
		public float NextScanHour { get; set; }
	}

	private sealed class NpcInitiatedLetterCandidate
	{
		public Hero Sender;
		public int BondScore;
		public int PrivateLove;
		public int PersonalTrust;
		public int PublicTrust;
		public int LetterTrust;
		public int DaysSinceInteraction;
		public float SelectionWeight;
		public List<NpcInitiatedLetterMotive> Motives = new List<NpcInitiatedLetterMotive>();
	}

	// Runtime-only: spreads the full Hero roster scan across campaign ticks.
	private sealed class NpcInitiatedLetterScanState
	{
		public DuelSettings Settings;
		public List<Hero> Heroes = new List<Hero>();
		public List<NpcInitiatedLetterCandidate> Candidates = new List<NpcInitiatedLetterCandidate>();
		public float NowDays;
		public int MinimumBond;
		public int QuietDays;
		public int PublicTrustCap;
		public int BatchSize;
		public int NextIndex;
		public long StartedAtUtcTicks;
		public HashSet<string> ActiveInboundSenderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class NpcInitiatedLetterMotive
	{
		public string MotiveType;
		public string LetterKind;
		public string NeedType;
		public string EventKey;
		public string IntentText;
		public string FactText;
		public string FallbackLetter;
		public float Weight = 1f;
	}

	private sealed class CourierLetterInventoryRecord
	{
		public string Key;
		public string ItemStringId;
		public string DisplayName;
		public string LetterBody;
		public string TemplateStringId;
		public uint ObjectId;
		public string SenderHeroId;
		public string SenderName;
		public bool IsReply;
		public int Amount;
		public int LastSeenDay;
	}

	private sealed class CourierRoutePlan
	{
		public MobileParty.NavigationType NavigationType;
		public bool RequiresNaval;
		public bool UsePort;
		public string Reason;

		public string KeySuffix
		{
			get
			{
				return ":nav=" + NavigationType + ":port=" + (UsePort ? "1" : "0") + ":sea=" + (RequiresNaval ? "1" : "0") + ":reason=" + (Reason ?? "");
			}
		}
	}

	private sealed class PendingCourierFlow
	{
		public Hero Recipient;
		public TroopRoster CrewRoster;
		public CourierPayloadMode Mode;
		public List<CourierCargoEntry> CrewEntries = new List<CourierCargoEntry>();
		public List<CourierTradeOption> TradeOptions = new List<CourierTradeOption>();
		public List<CourierCargoEntry> SelectedEntries = new List<CourierCargoEntry>();
		public int PendingAmountIndex;
	}

	private sealed class CourierTradeOption
	{
		public string Kind;
		public string Id;
		public string Name;
		public int AvailableAmount;
		public int GuidePriceDenars;
		public ItemObject Item;
		public MyBehavior.PartyTransferPromptEntry PartyEntry;
		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}

	private sealed class CourierReplyGenerationRequest
	{
		public string SessionId;
		public long RuntimeGeneration;
		public string RecipientHeroId;
		public string RecipientName;
		public string LetterText;
		public string ExtraFact;
		public string HistoryText;
		public string Extras;
		public string EntityPostprocessContext;
		public List<string> SelectedRuleHits = new List<string>();
		public List<object> Messages = new List<object>();
	}

	private sealed class InboundLetterGenerationRequest
	{
		public string SessionId;
		public long RuntimeGeneration;
		public string SenderHeroId;
		public string Seed;
		public string FallbackLetter;
		public List<string> SelectedRuleHits = new List<string>();
		public List<object> Messages = new List<object>();
	}

	private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
	private static bool _letterInputOpen;
	private static Harmony _courierHarmony;
	private static bool _partyNameplatePatchApplied;
	private static bool _partyNameplatePatchFailed;
	private static bool _mapTrackerProviderPatchApplied;
	private static bool _mapTrackerProviderPatchFailed;
	private static MapNotificationView _courierReplyRegisteredMapNotificationView;
	private static readonly Dictionary<string, long> LastTrackerEventPulseTicks = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, long> LastCourierLogicPulseTicks = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
	private static int _hasPotentialActiveCourierPartiesForAi;
	private sealed class CourierMapEventMarker { }
	private static readonly ConditionalWeakTable<MapEvent, CourierMapEventMarker> CourierMapEventMarkers = new ConditionalWeakTable<MapEvent, CourierMapEventMarker>();
	private static readonly FieldInfo MapEventFinishCalledField = AccessTools.Field(typeof(MapEvent), "_isFinishCalled");
	private static readonly PropertyInfo MapEventSideLeaderPartyProperty = AccessTools.Property(typeof(MapEventSide), nameof(MapEventSide.LeaderParty));
	private static readonly FieldInfo MapEventSideMapFactionField = AccessTools.Field(typeof(MapEventSide), "_mapFaction");

	private readonly Dictionary<string, CourierSession> _sessions = new Dictionary<string, CourierSession>(StringComparer.OrdinalIgnoreCase);
	private readonly object _sessionLock = new object();
	private volatile HashSet<string> _activeCourierPartyIdsSnapshot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private volatile bool _courierRuntimeIndexesReady = true;
	private readonly Dictionary<string, MobileParty> _courierPartyCache = new Dictionary<string, MobileParty>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _npcDiplomacyLetterSenderCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, float> _npcLetterMotiveFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, string> _npcLetterLastDeliveredEventKeyBySender = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, CourierLetterInventoryRecord> _courierLetterInventoryRecords = new Dictionary<string, CourierLetterInventoryRecord>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, string> _courierLetterInventoryStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private PendingCourierFlow _pendingFlow;
	private long _lastCampaignTickUtcTicks;
	private string _courierInboundCompletionScanCursor = string.Empty;
	private long _nextCourierLetterInventoryRestoreRetryUtcTicks;
	private float _npcDiplomacyLetterGlobalCooldownUntilDays;
	private float _nextNpcDiplomacyLetterScanHour;
	private NpcInitiatedLetterScanState _npcInitiatedLetterScan;
	private bool _courierReplyWaitTimeLocked;
	private CampaignTimeControlMode _courierReplyWaitPreviousMode = CampaignTimeControlMode.Stop;
	private bool _courierReplyWaitPreviousLock;
	private int _courierLetterInventoryRestoreRetryRemaining;

	public static CourierDeliveryBehavior Instance { get; private set; }

	private static bool IsCourierBridgeEnabled()
	{
		return FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationCourier);
	}

	/// <summary>
	/// Explicit opt-in detached facade for an outbound Courier reply. The
	/// existing Courier session state machine remains authoritative for delivery
	/// and return timing; this only exposes the already-built reply prompt at the
	/// interaction boundary.
	/// </summary>
	public static LegacyChannelInteractionFacade CreateCourierReplyRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway,
		string sessionId)
	{
		if (!IsCourierBridgeEnabled())
		{
			return null;
		}
		return LegacyInteractionSnapshotAdapters.CreateCourierInteractionFacade(
			ports,
			gateway,
			playerText => CaptureCourierReplyRefactorEnvelopeForExternal(sessionId, playerText));
	}

	/// <summary>
	/// Creates the explicit Courier reply ActionPlan executor. Only the stable
	/// session ID crosses the detached boundary; the current recipient and
	/// Courier session are resolved and validated on the game main thread.
	/// Existing delivery/return timing remains authoritative.
	/// </summary>
	public static LegacyNativeActionPlanExecutor CreateCourierReplyActionPlanExecutorForExternal(
		string sessionId,
		int maxActions = 64)
	{
		if (!IsCourierBridgeEnabled())
		{
			return null;
		}
		CourierDeliveryBehavior instance = Instance;
		if (instance == null || string.IsNullOrWhiteSpace(sessionId))
		{
			return null;
		}
		IReadOnlyList<string> allowedTagFamilies = LegacyActionTagCatalog.DefaultAllowedTagFamilies;
		CourierSession session = instance.GetSessionById(sessionId);
		Hero recipient = session == null ? null : instance.ResolveRecipient(session);
		IEconomyRewardDebtMainThreadPort economyPort = recipient == null
			? null
			: RewardSystemBehavior.CreateEconomyRewardDebtMainThreadPortForExternal();
		return LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
			(actionPlan, snapshot, duelDispatchContext) => instance.ExecuteCourierActionPlanForExternal(
				actionPlan,
				snapshot,
				sessionId,
				duelDispatchContext),
			DuelBehavior.CreateDetachedDuelDispatchOwnerForExternal(),
			maxActions,
			allowedTagFamilies,
			economyPort == null ? null : new LegacyEconomyRewardDebtAdapter(),
			economyPort,
			economyPort == null ? null : LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
			economyPort == null ? null : (actionPlan, snapshot, isEconomyOnly) =>
				instance.GateCourierEconomyActionPlanForExternal(actionPlan, snapshot, sessionId, isEconomyOnly));
	}

	public static RuntimeConfigSnapshot CaptureCourierReplyRefactorConfigurationForExternal()
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversationRuntimeConfiguration();
	}

	public static RuntimeConfigSnapshot CaptureCourierInboundRefactorConfigurationForExternal()
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversationRuntimeConfiguration();
	}

	/// <summary>
	/// Executes one explicitly opted-in Courier reply through the detached host.
	/// Capture must happen at the Courier interaction boundary; delivery and
	/// return timing remain owned by the existing Courier session state machine.
	/// </summary>
	public static Task<DetachedInteractionHostResult> SubmitCourierReplyRefactorOptInForExternalAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string sessionId,
		string playerText,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (!IsCourierBridgeEnabled())
		{
			return RunCourierDetachedRefactorFallbackAsync("bridge.conversation_courier_disabled", fallbackToLegacy);
		}
		CourierDeliveryBehavior instance = Instance;
		if (instance == null)
		{
			return RunCourierDetachedRefactorFallbackAsync("host_not_ready", fallbackToLegacy);
		}
		return instance.SubmitCourierReplyRefactorOptInCoreAsync(
			facade,
			configuration,
			moduleId,
			providerId,
			sessionId,
			playerText,
			fallbackToLegacy,
			cancellationToken);
	}

	/// <summary>
	/// Executes one explicitly opted-in NPC-initiated inbound letter through the
	/// detached host. The inbound seed is a generation input, not a player
	/// utterance, so the shared commit deliberately omits a user-history write.
	/// </summary>
	public static Task<DetachedInteractionHostResult> SubmitCourierInboundRefactorOptInForExternalAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string sessionId,
		string playerText,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (!IsCourierBridgeEnabled())
		{
			return RunCourierDetachedRefactorFallbackAsync("bridge.conversation_courier_disabled", fallbackToLegacy);
		}
		CourierDeliveryBehavior instance = Instance;
		if (instance == null)
		{
			return RunCourierDetachedRefactorFallbackAsync("host_not_ready", fallbackToLegacy);
		}
		return instance.SubmitCourierInboundRefactorOptInCoreAsync(
			facade,
			configuration,
			moduleId,
			providerId,
			sessionId,
			playerText,
			fallbackToLegacy,
			cancellationToken);
	}

	private async Task<DetachedInteractionHostResult> SubmitCourierInboundRefactorOptInCoreAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string sessionId,
		string playerText,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (facade == null || string.IsNullOrWhiteSpace(sessionId))
		{
			return await RunCourierDetachedRefactorFallbackAsync("missing_courier_inbound_facade", fallbackToLegacy).ConfigureAwait(false);
		}
		Task<DetachedInteractionHostResult> execution = await RunCourierOwnerPhaseAsync(
			SaveRuntimeGuard.CaptureGeneration(), "inbound_host_start", () =>
		{
		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		return host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => null,
			envelope => CreateCourierInboundMemoryFacadeForExternal(envelope, sessionId),
			(envelope, commit) => DispatchCourierRefactorCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				sessionId,
				abortInboundWithoutReceipt: true),
			fallbackToLegacy,
			cancellationToken,
			CompleteCourierInboundDetachedCommit,
			appendPlayerInput: false);
		}, cancellationToken).ConfigureAwait(false);
		return await execution.ConfigureAwait(false);
	}

	private async Task<DetachedInteractionHostResult> SubmitCourierReplyRefactorOptInCoreAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string sessionId,
		string playerText,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (facade == null || string.IsNullOrWhiteSpace(sessionId))
		{
			return await RunCourierDetachedRefactorFallbackAsync("missing_courier_facade", fallbackToLegacy).ConfigureAwait(false);
		}
		Task<DetachedInteractionHostResult> execution = await RunCourierOwnerPhaseAsync(
			SaveRuntimeGuard.CaptureGeneration(), "reply_host_start", () =>
		{
		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		return host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => CreateCourierReplyActionPlanExecutorForExternal(sessionId),
			CreateCourierMemoryFacadeForExternal,
			(envelope, commit) => DispatchCourierRefactorCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				sessionId),
			fallbackToLegacy,
			cancellationToken);
		}, cancellationToken).ConfigureAwait(false);
		return await execution.ConfigureAwait(false);
	}

	private static IInteractionMemory CreateCourierMemoryFacadeForExternal(InteractionEnvelope envelope)
	{
		string subjectId = envelope?.Snapshot?.Identity?.SubjectId;
		if (string.IsNullOrWhiteSpace(subjectId))
		{
			return null;
		}
		Hero recipient = ResolveHeroByIdForCourier(subjectId);
		return recipient == null ? null : new MyBehaviorMemoryFacade(recipient);
	}

	private static async Task<DetachedInteractionHostResult> RunCourierDetachedRefactorFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacy)
	{
		if (fallbackToLegacy == null)
		{
			return new DetachedInteractionHostResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode,
				null,
				null);
		}
		try
		{
			return new DetachedInteractionHostResult(
				await fallbackToLegacy().ConfigureAwait(false),
				true,
				InteractionStatus.Succeeded,
				errorCode,
				null,
				null);
		}
		catch (Exception exception)
		{
			return new DetachedInteractionHostResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode + ";legacy_" + exception.GetType().Name,
				null,
				null);
		}
	}

	/// <summary>
	/// Explicit opt-in detached facade for an NPC-initiated inbound letter.
	/// Inbound letter generation has no action postprocess stage; its existing
	/// letter delivery state machine is not replaced.
	/// </summary>
	public static LegacyChannelInteractionFacade CreateCourierInboundRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway,
		string sessionId)
	{
		if (!IsCourierBridgeEnabled())
		{
			return null;
		}
		return LegacyInteractionSnapshotAdapters.CreateCourierInteractionFacade(
			ports,
			gateway,
			playerText => CaptureCourierInboundRefactorEnvelopeForExternal(sessionId, playerText));
	}

	/// <summary>
	/// Captures the exact legacy outbound Courier message order into immutable
	/// PromptPackage/history sections. Must run on the game main thread while
	/// the session and recipient are valid.
	/// </summary>
	public static InteractionEnvelope CaptureCourierReplyRefactorEnvelopeForExternal(
		string sessionId,
		string playerText = null)
	{
		CourierDeliveryBehavior instance = Instance;
		CourierSession session = instance?.GetSessionById(sessionId);
		Hero recipient = session == null ? null : instance.ResolveRecipient(session);
		if (session == null || recipient == null || recipient.IsDead)
		{
			return LegacyInteractionSnapshotAdapters.CaptureCourier(recipient, playerText ?? string.Empty, sessionId, string.Empty);
		}
		CourierReplyGenerationRequest request = instance.BuildCourierReplyGenerationRequestOnMainThread(
			session,
			recipient,
			SaveRuntimeGuard.CaptureGeneration(),
			CaptureCourierHistoryForLegacyEnvelope(session, recipient, false));
		PromptPackage prompt = LegacyPromptPackageAdapter.FromLegacyMessages(
			request.Messages,
			MainReplyMaxTokens,
			"legacy-courier-reply");
		return instance.CapturePreparedCourierReplyEnvelope(request, recipient, prompt);
	}

	private InteractionEnvelope CapturePreparedCourierReplyEnvelope(CourierReplyGenerationRequest request, Hero recipient, PromptPackage prompt)
	{
		Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["courier_direction"] = "outbound_reply",
			["courier_selected_rule_ids"] = string.Join(",", request.SelectedRuleHits ?? new List<string>()),
			["rule_runtime_context"] = "courier",
			["excluded_rule_ids"] = string.Join(",", CourierExcludedRuleIds),
			["courier_request_extras"] = request.Extras ?? string.Empty,
			["courier_request_history"] = request.HistoryText ?? string.Empty,
			["courier_request_entities"] = request.EntityPostprocessContext ?? string.Empty
		};
		return LegacyInteractionSnapshotAdapters.CaptureCourierFromPromptPackage(
			recipient,
			request.LetterText,
			request.SessionId,
			request.ExtraFact,
			prompt,
			null,
			facts);
	}

	/// <summary>
	/// Captures the exact legacy inbound-letter message order into immutable
	/// PromptPackage/history sections. Must run on the game main thread.
	/// </summary>
	public static InteractionEnvelope CaptureCourierInboundRefactorEnvelopeForExternal(
		string sessionId,
		string playerText = null)
	{
		CourierDeliveryBehavior instance = Instance;
		CourierSession session = instance?.GetSessionById(sessionId);
		Hero sender = session == null ? null : instance.ResolveSender(session);
		if (session == null || sender == null || sender.IsDead)
		{
			return LegacyInteractionSnapshotAdapters.CaptureCourier(sender, playerText ?? string.Empty, sessionId, string.Empty);
		}
		string fallbackLetter = session.InboundFallbackLetter ?? session.LetterText ?? string.Empty;
		InboundLetterGenerationRequest request = instance.BuildInboundLetterGenerationRequestOnMainThread(
			session,
			sender,
			fallbackLetter,
			SaveRuntimeGuard.CaptureGeneration(),
			CaptureCourierHistoryForLegacyEnvelope(session, sender, true));
		PromptPackage prompt = LegacyPromptPackageAdapter.FromLegacyMessages(
			request.Messages,
			MainReplyMaxTokens,
			"legacy-courier-inbound");
		Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["courier_direction"] = "inbound_letter",
			["courier_selected_rule_ids"] = string.Join(",", request.SelectedRuleHits ?? new List<string>()),
			["rule_runtime_context"] = "courier",
			["excluded_rule_ids"] = string.Join(",", CourierExcludedRuleIds)
		};
		return LegacyInteractionSnapshotAdapters.CaptureCourierFromPromptPackage(
			sender,
			request.Seed,
			request.SessionId,
			BuildInboundDeliveryFactText(session, delivered: false, sender),
			prompt,
			null,
			facts);
	}

	/// <summary>
	/// Creates the Courier detached ports without a second rule lookup: the
	/// selected rule IDs are captured from the legacy request into the immutable
	/// envelope. Action execution remains a channel-owned main-thread concern.
	/// </summary>
	public static LegacyInteractionPipelinePorts CreateCourierDetachedPortsForExternal(
		IEnumerable<string> allowedTagFamilies,
		bool includePostprocess = true,
		int maxActions = 64)
	{
		return CreateCourierDetachedPorts(allowedTagFamilies, includePostprocess, maxActions, null);
	}

	private static LegacyInteractionPipelinePorts CreateCourierDetachedPorts(
		IEnumerable<string> allowedTagFamilies, bool includePostprocess, int maxActions, PromptPackage preparedMainPrompt)
	{
		List<string> tagFamilies = (allowedTagFamilies ?? Enumerable.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		LegacyDetachedPromptComposer mainComposer = new LegacyDetachedPromptComposer(MainReplyMaxTokens, "legacy-courier");
		LegacyActionTagParser actionParser = new LegacyActionTagParser(maxActions);
		var postprocessOwners = new ConditionalWeakTable<PostprocessContext, CourierDetachedPostprocessOwner>();
		CapabilitySet capabilities = new CapabilitySet(new[]
		{
			"llm.generate",
			"prompt.compose",
			"postprocess.compose",
			"action.parse"
		});
		return new LegacyInteractionPipelinePorts(
			snapshot => new RuleSelection(
				(new[]
				{
					snapshot != null
						&& snapshot.DetachedFacts.TryGetValue("courier_direction", out string direction)
						&& string.Equals(direction, "inbound_letter", StringComparison.OrdinalIgnoreCase)
						? "courier_inbound"
						: "courier_reply"
				})
					.Concat(ReadCourierCsvFact(snapshot, "courier_selected_rule_ids"))
					.Distinct(StringComparer.OrdinalIgnoreCase),
				Array.Empty<string>()),
			(envelope, selection, availableCapabilities) => preparedMainPrompt ?? mainComposer.Compose(envelope, selection, availableCapabilities),
			(snapshot, selection, availableCapabilities) => new PostprocessContext(selection?.RuleIds, tagFamilies, availableCapabilities),
			// Unbound/synchronous parsing has no live rule authority. Only the async owner may parse.
			(rawText, context) => new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
			(rawText, internalTagFamilies) => NormalizeCourierDetachedVisibleReply(rawText),
			capabilities,
			null,
			includePostprocess
				? ((envelope, selection, visibleReply, rawReply, context, token) => PrepareCourierDetachedPostprocessAsync(envelope, rawReply, context, postprocessOwners, token))
				: null,
			includePostprocess
				? ((rawText, context, token) => CompleteCourierDetachedPostprocessAsync(rawText, context, postprocessOwners, actionParser, token))
				: null);
	}

	private static IReadOnlyList<string> ReadCourierCsvFact(GameInteractionSnapshot snapshot, string key)
	{
		if (snapshot == null || !snapshot.DetachedFacts.TryGetValue(key, out string value))
		{
			return Array.Empty<string>();
		}
		return (value ?? string.Empty)
			.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(item => item.Trim())
			.Where(item => item.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private ShoutBehavior.CourierActionPostprocessWorkItem PrepareCourierDetachedPostprocessWorkItem(
		Hero recipient,
		CourierReplyGenerationRequest request,
		string playerText,
		string historyText,
		string replyText)
	{
		if (recipient == null || request == null)
		{
			return null;
		}
		string extras = request.Extras ?? string.Empty;
		List<string> selected = request.SelectedRuleHits ?? new List<string>();
		bool duel = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "duel") || HasPreprocessRuleHit(selected, "duel");
		bool reward = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "reward") || HasPreprocessRuleHit(selected, "reward");
		bool loan = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "loan") || HasPreprocessRuleHit(selected, "loan");
		bool kingdomService = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_service") || HasPreprocessRuleHit(selected, "kingdom_service");
		bool lordsHall = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "lords_hall_access") || HasPreprocessRuleHit(selected, "lords_hall_access");
		bool meetingRelease = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "encounter_release_player") || HasPreprocessRuleHit(selected, "encounter_release_player");
		bool vanillaIssue = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "vanilla_issue") || HasPreprocessRuleHit(selected, "vanilla_issue");
		bool partyTransfer = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "party_transfer") || HasPreprocessRuleHit(selected, "party_transfer");
		bool voteDeal = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_agenda") || HasPreprocessRuleHit(selected, "kingdom_agenda");
		bool diplomacy = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "diplomacy") || HasPreprocessRuleHit(selected, "diplomacy");
		bool worldMap = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "worldmap_party_command") || HasPreprocessRuleHit(selected, "worldmap_party_command");
		bool vassalage = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_vassalage") || HasPreprocessRuleHit(selected, "kingdom_vassalage");
		if (!ShoutBehavior.TryPrepareCourierActionPostprocessForExternal(
			recipient,
			recipient.CharacterObject,
			recipient.Name?.ToString() ?? request.RecipientName ?? "NPC",
			playerText,
			historyText,
			replyText,
			duel,
			reward,
			loan,
			kingdomService,
			lordsHall,
			meetingRelease,
			vanillaIssue,
			kingdomService,
			false,
			partyTransfer,
			out ShoutBehavior.CourierActionPostprocessWorkItem workItem,
			out _,
			voteDeal,
			diplomacy,
			worldMap,
			selected,
			request.EntityPostprocessContext,
			-1,
			latestReplyHasPlayerInput: true,
			forceLooseWeeklyMemoryMaterialSession: true,
			kingdomVassalageRuleInjected: vassalage,
			kingdomAnnexationRuleInjected: false,
			chainName: "courier-detached"))
		{
			return null;
		}
		return workItem;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		UpdateAiCourierPresenceFlag(_activeCourierPartyIdsSnapshot, _courierRuntimeIndexesReady);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dataStore != null && dataStore.IsLoading)
		{
			ResetTransientRuntimeForLoadedSaveExternal("courier_sync_load");
		}
		if (dataStore.IsSaving)
		{
			lock (_sessionLock)
			{
				foreach (KeyValuePair<string, CourierSession> pair in _sessions)
				{
					if (pair.Value == null)
					{
						continue;
					}
					storage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
				}
			}
			storage = CampaignSaveChunkHelper.FlattenStringDictionary(storage, SessionStorageKey, "CourierDelivery");
		}
		dataStore.SyncData(SessionStorageKey, ref storage);
		if (dataStore.IsLoading)
		{
			storage = CampaignSaveChunkHelper.RestoreStringDictionary(storage, "CourierDelivery");
			lock (_sessionLock)
			{
				_sessions.Clear();
				foreach (KeyValuePair<string, string> pair in storage ?? new Dictionary<string, string>())
				{
					try
					{
						CourierSession session = JsonConvert.DeserializeObject<CourierSession>(pair.Value ?? "");
						if (session == null || string.IsNullOrWhiteSpace(session.Id))
						{
							continue;
						}
						NormalizeSession(session);
						ResetReplyGenerationAfterLoad(session, "sync_load");
						_sessions[session.Id] = session;
					}
					catch (Exception ex)
					{
						Log("load session failed key=" + pair.Key + " error=" + ex.Message);
					}
				}
			}
			RebuildCourierRuntimeIndexes();
		}

		string npcLetterStorageJson = null;
		if (dataStore.IsSaving)
		{
			npcLetterStorageJson = JsonConvert.SerializeObject(new NpcDiplomacyLetterStorage
			{
				SenderCooldownUntilDays = _npcDiplomacyLetterSenderCooldownUntilDays,
				MotiveFatigueUntilDays = _npcLetterMotiveFatigueUntilDays,
				LastDeliveredEventKeyBySender = _npcLetterLastDeliveredEventKeyBySender,
				GlobalCooldownUntilDays = _npcDiplomacyLetterGlobalCooldownUntilDays,
				// The in-progress roster scan is runtime-only; a loaded save retries it cleanly.
				NextScanHour = _npcInitiatedLetterScan == null ? _nextNpcDiplomacyLetterScanHour : 0f
			});
		}
		dataStore.SyncData(NpcDiplomacyLetterStorageKey, ref npcLetterStorageJson);
		if (dataStore.IsLoading)
		{
			try
			{
				NpcDiplomacyLetterStorage npcStorage = string.IsNullOrWhiteSpace(npcLetterStorageJson) ? null : JsonConvert.DeserializeObject<NpcDiplomacyLetterStorage>(npcLetterStorageJson);
				_npcDiplomacyLetterSenderCooldownUntilDays = NormalizeFloatDictionary(npcStorage?.SenderCooldownUntilDays);
				_npcLetterMotiveFatigueUntilDays = NormalizeFloatDictionary(npcStorage?.MotiveFatigueUntilDays);
				_npcLetterLastDeliveredEventKeyBySender = NormalizeStringDictionary(npcStorage?.LastDeliveredEventKeyBySender);
				_npcDiplomacyLetterGlobalCooldownUntilDays = npcStorage?.GlobalCooldownUntilDays ?? 0f;
				_nextNpcDiplomacyLetterScanHour = npcStorage?.NextScanHour ?? 0f;
			}
			catch (Exception ex)
			{
				Log("load npc diplomacy letter storage failed: " + ex.Message);
				_npcDiplomacyLetterSenderCooldownUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
				_npcLetterMotiveFatigueUntilDays = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
				_npcLetterLastDeliveredEventKeyBySender = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_npcDiplomacyLetterGlobalCooldownUntilDays = 0f;
				_nextNpcDiplomacyLetterScanHour = 0f;
			}
		}

		SyncCourierLetterInventoryData(dataStore);
	}

	private void SyncCourierLetterInventoryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		try
		{
			EnsureCourierLetterInventoryData();
			if (dataStore.IsLoading)
			{
				_courierLetterInventoryRecords.Clear();
				_courierLetterInventoryStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_courierLetterInventoryRestoreRetryRemaining = 0;
				_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
				Log("courier letter inventory save scope cleared reason=sync_load_begin");
			}
			if (dataStore.IsSaving)
			{
				DiscoverCourierLetterInventoryRecordsFromPlayerRoster("sync_save");
				RefreshCourierLetterInventoryRecordsFromPlayerRoster("sync_save");
				_courierLetterInventoryStorage.Clear();
				foreach (KeyValuePair<string, CourierLetterInventoryRecord> pair in _courierLetterInventoryRecords.ToList())
				{
					CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(pair.Key, pair.Value);
					if (record == null || record.Amount <= 0)
					{
						continue;
					}
					_courierLetterInventoryStorage[record.Key] = JsonConvert.SerializeObject(record);
				}
				Log("courier letter inventory save records=" + _courierLetterInventoryStorage.Count + " tracked=" + _courierLetterInventoryRecords.Count);
				_courierLetterInventoryStorage = CampaignSaveChunkHelper.FlattenStringDictionary(_courierLetterInventoryStorage, CourierLetterInventoryStorageKey, "CourierLetterInventory");
			}
			Dictionary<string, string> inventoryStorage = dataStore.IsSaving ? _courierLetterInventoryStorage : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			dataStore.SyncData(CourierLetterInventoryStorageKey, ref inventoryStorage);
			if (dataStore.IsLoading)
			{
				_courierLetterInventoryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(inventoryStorage, "CourierLetterInventory") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_courierLetterInventoryRecords.Clear();
				foreach (KeyValuePair<string, string> pair in _courierLetterInventoryStorage)
				{
					if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
					{
						continue;
					}
					try
					{
						CourierLetterInventoryRecord record = NormalizeCourierLetterInventoryRecord(pair.Key, JsonConvert.DeserializeObject<CourierLetterInventoryRecord>(pair.Value));
						if (record != null && record.Amount > 0)
						{
							_courierLetterInventoryRecords[record.Key] = record;
						}
					}
					catch (Exception ex)
					{
						Log("load courier letter inventory record failed key=" + pair.Key + " error=" + ex.Message);
					}
				}
				DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest("sync_load_manifest_merge");
				PrimeCourierLetterInventoryRecords("sync_load");
				ScheduleCourierLetterInventoryRestoreRetries("sync_load", 8);
			}
		}
		catch (Exception ex)
		{
			Log("courier letter inventory sync failed: " + ex.Message);
			_courierLetterInventoryRecords = new Dictionary<string, CourierLetterInventoryRecord>(StringComparer.OrdinalIgnoreCase);
			_courierLetterInventoryStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	public static bool IsCourierInputOpen => _letterInputOpen;

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		try
		{
			Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.courier.delivery");
			_courierHarmony = activeHarmony;
			MethodInfoAccess.PatchDefaultEncounterModel(activeHarmony);
			MethodInfoAccess.PatchCustomPartyComponentBanner(activeHarmony);
			MethodInfoAccess.PatchCourierMapEventSafety(activeHarmony);
			AnimusForgeCourierUiSprites.EnsurePatched(activeHarmony);
			Log("harmony patches registered");
		}
		catch (Exception ex)
		{
			Log("harmony register failed: " + ex);
		}
	}

	public static bool IsBanditOrOutlawParty(MobileParty party)
	{
		try
		{
			return party != null && (party.IsBandit || party.MapFaction?.IsBanditFaction == true || party.ActualClan?.IsBanditFaction == true);
		}
		catch
		{
			return false;
		}
	}

	private void CommitGeneratedReplyActionsAtRecipientCore(CourierSession session, Hero recipient, bool persistHistory = true)
	{
		if (session == null || session.PostprocessConsumed)
		{
			return;
		}
		if (!session.DeliveryApplied)
		{
			return;
		}
		string text = session.ReplyPostprocessedText ?? session.ReplyText ?? "";
		if (recipient == null || recipient.IsDead)
		{
			session.PostprocessConsumed = true;
			session.ReplyPostprocessedText = StripCourierActionTags(text);
			Log("postprocess skipped recipient invalid session=" + session.Id);
			return;
		}
		try
		{
			TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure);
			if (!string.IsNullOrWhiteSpace(proposalFailure))
			{
				Log("kingdom agenda custom policy not queued session=" + session.Id + " reason=" + proposalFailure);
			}
		}
		catch (Exception ex)
		{
			Log("apply kingdom agenda custom policy tag failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			VoteDealBehavior.ProcessAgendaTagsDispatch(recipient, ref text);
			DiplomacyBehavior.ProcessDiplomacyTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vote deal tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply world map tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal(recipient, ref text, out var nobleFacts, out var nobleNotifications))
			{
				foreach (string fact in nobleFacts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in nobleNotifications ?? new List<string>())
				{
					if (!string.IsNullOrWhiteSpace(note))
					{
						InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply noble gathering tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			if (MyBehavior.TryApplyPartyTransferTagsForExternal(recipient, recipient.CharacterObject, -1, ref text, out var facts, out var notifications))
			{
				foreach (string fact in facts ?? new List<string>())
				{
					MyBehavior.AppendExternalDialogueHistory(recipient, null, null, fact);
				}
				foreach (string note in notifications ?? new List<string>())
				{
					InformationManager.DisplayMessage(new InformationMessage(note, Colors.Green));
				}
			}
		}
		catch (Exception ex)
		{
			Log("apply party transfer tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			bool rewardBeforeHasVassalage = ContainsVassalageActionTag(text);
			bool rewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTag(text);
			Log("ApplyRewardTags start chain=courier session=" + session.Id + " containsVASSALAGE=" + rewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex);
			RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(text)
				? CreateCourierRpItemIntroductionContext(session, recipient, text)
				: null;
			RewardSystemBehavior.Instance?.ApplyRewardTags(recipient, Hero.MainHero, ref text, rpItemIntroductionContext);
			Log("ApplyRewardTags done chain=courier session=" + session.Id + " beforeVASSALAGE=" + rewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTag(text) + " beforeKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTag(text));
		}
		catch (Exception ex)
		{
			Log("apply reward tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			VanillaIssueOfferBridge.ApplyIssueOfferTags(recipient, ref text);
		}
		catch (Exception ex)
		{
			Log("apply vanilla issue tags failed session=" + session.Id + " error=" + ex.Message);
		}
		try
		{
			RomanceSystemBehavior.Instance?.ApplyMarriageTags(recipient, Hero.MainHero, ref text, runPostprocessIfMissing: false);
		}
		catch (Exception ex)
		{
			Log("apply marriage tags failed session=" + session.Id + " error=" + ex.Message);
		}
		session.ReplyPostprocessedText = text;
		session.PostprocessConsumed = true;
		if (persistHistory)
		{
			PersistCourierReplyToHistories(session, recipient, text);
		}
		Log("postprocess committed at recipient session=" + session.Id + " remainingLen=" + (text ?? "").Length);
	}

	/// <summary>
	/// Main-thread bridge for the detached Courier reply path. Domain handlers
	/// remain in <see cref="CommitGeneratedReplyActionsAtRecipientCore"/>; the optional
	/// history write is disabled here because InteractionResultCommitter owns
	/// the single shared user/assistant memory commit.
	/// </summary>
	private InteractionStatus ExecuteCourierActionPlanForExternal(
		ActionPlan actionPlan,
		GameInteractionSnapshot snapshot,
		string sessionId,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
		try
		{
			if (!TWParallel.IsMainThread()
				|| actionPlan == null
				|| snapshot?.Identity == null
				|| snapshot.Identity.Channel != InteractionChannel.Courier
				|| !string.Equals(snapshot.Identity.SessionId, sessionId, StringComparison.Ordinal)
				|| string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId))
			{
				return InteractionStatus.RejectedByValidation;
			}
			if (duelDispatchContext != null)
			{
				DuelBehavior.RejectDetachedDuelDispatchForExternal(
					duelDispatchContext,
					"unsupported_channel");
				Log("detached courier Duel rejected session=" + sessionId
					+ " reason=unsupported_channel");
				return InteractionStatus.RejectedByValidation;
			}
			CourierSession session = GetSessionById(sessionId);
			Hero recipient = session == null ? null : ResolveRecipient(session);
			if (recipient == null
				|| recipient.IsDead
				|| !IsCourierActionSessionEligible(session, snapshot, sessionId, SafeHeroId(recipient)))
			{
				Log("detached courier action rejected session=" + sessionId + " reason=target_or_delivery_invalid");
				return InteractionStatus.RejectedByValidation;
			}
			session.ReplyPostprocessedText = actionPlan.RawPostprocessId;
			CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory: false);
			return session.PostprocessConsumed
				? InteractionStatus.Executed
				: InteractionStatus.RejectedByValidation;
		}
		catch (Exception ex)
		{
			Log("detached courier action failed session=" + (sessionId ?? "") + " error=" + ex.Message);
			return InteractionStatus.RejectedByValidation;
		}
	}

	/// <summary>
	/// Validates the Courier owner before any Economy port mutation. Economy-only
	/// plans also reserve the already-persisted PostprocessConsumed flag first,
	/// so a new trace or loaded save cannot replay the same session action.
	/// Mixed plans retain their later legacy commit and are only prevalidated here.
	/// </summary>
	private InteractionStatus GateCourierEconomyActionPlanForExternal(
		ActionPlan actionPlan,
		GameInteractionSnapshot snapshot,
		string sessionId,
		bool isEconomyOnly)
	{
		try
		{
			if (!TWParallel.IsMainThread()
				|| actionPlan == null
				|| string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId)
				|| actionPlan.Actions.Count == 0)
			{
				return InteractionStatus.RejectedByValidation;
			}
			bool allEconomy = actionPlan.Actions.All(LegacyEconomyRewardDebtAdapter.IsEconomyAction);
			if (!actionPlan.Actions.Any(LegacyEconomyRewardDebtAdapter.IsEconomyAction)
				|| allEconomy != isEconomyOnly)
			{
				return InteractionStatus.RejectedByValidation;
			}
			CourierSession session = GetSessionById(sessionId);
			Hero recipient = session == null ? null : ResolveRecipient(session);
			if (recipient == null
				|| recipient.IsDead
				|| !IsCourierActionSessionEligible(session, snapshot, sessionId, SafeHeroId(recipient)))
			{
				Log("detached courier economy gate rejected session=" + (sessionId ?? "") + " reason=target_or_delivery_invalid");
				return InteractionStatus.RejectedByValidation;
			}
			if (isEconomyOnly && !TryReserveCourierEconomyOnly(session))
			{
				return InteractionStatus.RejectedByValidation;
			}
			Log("detached courier economy gate accepted session=" + session.Id + " economyOnly=" + isEconomyOnly);
			return InteractionStatus.Executed;
		}
		catch (Exception ex)
		{
			Log("detached courier economy gate failed session=" + (sessionId ?? "") + " error=" + ex.Message);
			return InteractionStatus.RejectedByValidation;
		}
	}

	private static bool IsCourierActionSessionEligible(
		CourierSession session,
		GameInteractionSnapshot snapshot,
		string expectedSessionId,
		string recipientHeroId)
	{
		return session != null
			&& snapshot?.Identity != null
			&& snapshot.Identity.Channel == InteractionChannel.Courier
			&& !string.IsNullOrWhiteSpace(expectedSessionId)
			&& string.Equals(session.Id, expectedSessionId, StringComparison.Ordinal)
			&& string.Equals(snapshot.Identity.SessionId, expectedSessionId, StringComparison.Ordinal)
			&& !IsInboundToPlayer(session)
			&& !IsTerminalStage(session)
			&& session.DeliveryApplied
			&& !session.PostprocessConsumed
			&& !string.IsNullOrWhiteSpace(recipientHeroId)
			&& string.Equals(snapshot.Identity.SubjectId, recipientHeroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryReserveCourierEconomyOnly(CourierSession session)
	{
		if (session == null || session.PostprocessConsumed)
		{
			return false;
		}
		session.PostprocessConsumed = true;
		return true;
	}

	private void PersistCourierReplyToHistories(CourierSession session, Hero recipient, string processedReplyText)
	{
		try
		{
			if (session == null || recipient == null)
			{
				return;
			}
			// Keep role-play action prose in the shared dialogue history for later
			// postprocessing. The player-facing letter is still sanitized at display.
			string reply = StripCourierActionTags(processedReplyText);
			if (string.IsNullOrWhiteSpace(reply))
			{
				reply = StripCourierActionTags(session.ReplyText);
			}
			reply = (reply ?? "").Trim();
			if (string.IsNullOrWhiteSpace(reply))
			{
				return;
			}
			string historyLine = "【回信】" + reply;
			string npcName = (recipient.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = "NPC";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, null, historyLine, "[AFEF NPC行为补充] " + npcName + "已通过信使写下回信，信使正在把回信带给玩家。");
			ShoutBehavior.RecordNativeConversationNpcLineForExternal(recipient, recipient.CharacterObject, npcName, historyLine);
			PlayerNotorietyBehavior.NoteCourierReplyForExternal(recipient);
			Log("reply history persisted session=" + session.Id + " recipient=" + SafeHeroId(recipient));
		}
		catch (Exception ex)
		{
			Log("persist reply history failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}

	private void ShowCourierReplyWaitPopupAndPause(CourierSession session, Hero recipient)
	{
		if (session == null || session.ReplyGenerated)
		{
			return;
		}
		BeginCourierReplyWaitPause(session, recipient);
		if (session.ReplyWaitPopupShown)
		{
			return;
		}
		session.ReplyWaitPopupShown = true;
		try
		{
			if (IsInboundToPlayer(session))
			{
				string senderName = recipient?.Name?.ToString() ?? session.SenderName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使来信生成", "信使已经抵达你的队伍，正在等待" + senderName + "写完信件正文。\n\n游戏时间已暂停，信件生成完成后会自动送达。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
			else
			{
				string name = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使回信生成", "信使已经抵达 " + name + " 的位置，正在等待对方读信并写下回信。\n\n游戏时间已暂停，回信生成完成后会自动继续并执行后处理标签。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
		}
		catch (Exception ex)
		{
			Log("show reply wait inquiry failed session=" + session.Id + " error=" + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage(IsInboundToPlayer(session) ? "信使已抵达，正在等待来信生成。游戏时间已暂停。" : "信使已抵达，正在等待回信生成。游戏时间已暂停。", Colors.Yellow));
		}
	}

	private void BeginCourierReplyWaitPause(CourierSession session, Hero recipient)
	{
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return;
			}
			if (!_courierReplyWaitTimeLocked)
			{
				_courierReplyWaitPreviousMode = campaign.TimeControlMode;
				_courierReplyWaitPreviousLock = campaign.TimeControlModeLock;
				campaign.TimeControlMode = CampaignTimeControlMode.Stop;
				campaign.SetTimeControlModeLock(true);
				_courierReplyWaitTimeLocked = true;
				Log("reply wait time locked session=" + (session?.Id ?? "") + " recipient=" + SafeHeroId(recipient));
			}
			else
			{
				campaign.SetTimeSpeed(0);
			}
		}
		catch (Exception ex)
		{
			Log("reply wait pause failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}

	private void EndCourierReplyWaitPause(CourierSession completedSession, string reason)
	{
		if (completedSession != null)
		{
			completedSession.ReplyWaitPopupShown = false;
		}
		if (HasActiveCourierReplyWait())
		{
			return;
		}
		try
		{
			InformationManager.HideInquiry();
		}
		catch
		{
		}
		if (!_courierReplyWaitTimeLocked)
		{
			return;
		}
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign != null)
			{
				campaign.SetTimeControlModeLock(_courierReplyWaitPreviousLock);
				if (!_courierReplyWaitPreviousLock)
				{
					campaign.TimeControlMode = _courierReplyWaitPreviousMode;
				}
			}
			Log("reply wait time released reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Log("reply wait release failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
		_courierReplyWaitTimeLocked = false;
	}

	private bool HasActiveCourierReplyWait()
	{
		try
		{
			lock (_sessionLock)
			{
				return _sessions.Values.Any(x => x != null && !IsTerminalStage(x) && !x.ReplyGenerated && x.ReplyWaitPopupShown);
			}
		}
		catch
		{
			return false;
		}
	}

	private static List<object> BuildCourierReplyMessages(Hero recipient, CourierSession session, string extras, string deliveryFactForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null, string npcRoleContext = null, string preprocessExcludedRuleBlock = null)
	{
		string npcName = recipient?.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string deliveryFact = string.IsNullOrWhiteSpace(deliveryFactForPrompt) ? (session?.DeliveryFactText ?? "") : deliveryFactForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(recipient, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), session.LetterText, deliveryFact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(recipient, 6);
		string senderIdentity = MyBehavior.BuildPlayerCourierSenderIdentityForExternal(recipient);
		string senderRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(recipient);
		string currentLocationLine = BuildCourierCurrentLocationLine(recipient);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你刚刚通过信使收到" + playerName + "写给你的一封信。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含来信、玩家发言、事实、旁听内容与规则。\n"
			+ "你必须根据来信者的公开身份选择合适称呼；如果来信者是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在回信中的正文，不要写旁白、动作描写、系统说明或标签解释。\n"
			+ "如果你认为没有必要回信，可以完全空回复。\n"
			+ "如果你在回信中明确同意给玩家物品、部队、俘虏或固定资产，仍然按已注入的后处理规则在正文语义中表达，标签由后处理阶段生成。";
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(senderIdentity))
		{
			context.AppendLine(senderIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(senderRelationship))
		{
			AppendCourierUserSection(context, "【来信者与你的关系】", senderRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(currentDateFact))
		{
			AppendCourierRawUserSection(context, currentDateFact);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【信件内容】");
		current.AppendLine(session.LetterText ?? "");
		if (!string.IsNullOrWhiteSpace(deliveryFact))
		{
			current.AppendLine();
			current.AppendLine("【随信送达事实】");
			current.AppendLine("【当下行为】" + deliveryFact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份决定是否回信；如果回信，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	private static List<object> BuildInboundNpcLetterMessages(Hero sender, CourierSession session, string seed, string extras, string factForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null, string npcRoleContext = null, string preprocessExcludedRuleBlock = null)
	{
		string npcName = sender?.Name?.ToString() ?? session?.SenderName ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string fact = string.IsNullOrWhiteSpace(factForPrompt) ? (session?.DeliveryFactText ?? "") : factForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(sender, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), null, fact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(sender, 6);
		string playerIdentity = MyBehavior.BuildPlayerCourierRecipientIdentityForExternal(sender);
		string playerRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(sender);
		string currentLocationLine = BuildCourierCurrentLocationLine(sender);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
		int targetChars = ClampInt(DuelSettings.GetSettings()?.NpcInitiatedLetterTargetChars ?? 220, 80, 1000);
		string letterKind = GetInboundLetterKindDisplayText(session);
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你决定主动通过信使给" + playerName + "写一封" + letterKind + "。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含历史、事实、旁听内容、规则与本次写信意图。\n"
			+ "你必须根据收信人的公开身份选择合适称呼；如果收信人是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在信中的正文，不要写旁白、动作描写、系统说明、标签解释或方括号动作标签。\n"
			+ "正文目标约" + targetChars + "字，只保留一个主要动机或一项具体请求。\n"
			+ "只能使用已提供的事实；不能宣布" + playerName + "已经同意，也不能把尚未发生的交易、承诺、外交或其他机制结果写成事实。";
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(playerIdentity))
		{
			context.AppendLine(playerIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(playerRelationship))
		{
			AppendCourierUserSection(context, "【收信人与你的关系】", playerRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(currentDateFact))
		{
			AppendCourierRawUserSection(context, currentDateFact);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【本次 NPC 主动写信意图】");
		current.AppendLine(seed ?? "");
		if (!string.IsNullOrWhiteSpace(fact))
		{
			current.AppendLine();
			current.AppendLine("【送信事实】");
			current.AppendLine("【当下行为】" + fact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份写给" + playerName + "一封会由信使送达的" + letterKind + "，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	private static string BuildCourierCurrentLocationLine(Hero recipient)
	{
		try
		{
			MobileParty party = ResolveCourierPromptParty(recipient);
			Settlement currentSettlement = recipient?.CurrentSettlement ?? party?.CurrentSettlement;
			if (currentSettlement != null)
			{
				return "你当前位于" + FormatCourierSettlementNameWithType(currentSettlement) + "。";
			}
			if (party == null)
			{
				return "";
			}
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				Settlement nearest = FindNearestSettlementForCourierPrompt(party);
				string nearestName = FormatCourierSettlementNameWithType(nearest);
				string locationLine = string.IsNullOrWhiteSpace(nearestName)
					? "你正位于海上。"
					: "你正位于" + nearestName + "附近的海上。";
				string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(party);
				if (!string.IsNullOrWhiteSpace(shipText))
				{
					locationLine += "舰船：" + shipText + "。";
				}
				return locationLine;
			}
			string terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
			if (string.IsNullOrWhiteSpace(terrainLabel))
			{
				terrainLabel = "野外";
			}
			Settlement landNearest = FindNearestSettlementForCourierPrompt(party);
			string landNearestName = FormatCourierSettlementNameWithType(landNearest);
			return string.IsNullOrWhiteSpace(landNearestName)
				? "你当前位于" + terrainLabel + "。"
				: "你当前位于" + landNearestName + "附近的" + terrainLabel + "。";
		}
		catch
		{
			return "";
		}
	}

	private static MobileParty ResolveCourierPromptParty(Hero hero)
	{
		try
		{
			if (hero?.PartyBelongedTo != null && hero.PartyBelongedTo.IsActive)
			{
				return hero.PartyBelongedTo;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase prisonerParty = hero?.PartyBelongedToAsPrisoner;
			if (prisonerParty?.MobileParty != null && prisonerParty.MobileParty.IsActive)
			{
				return prisonerParty.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement FindNearestSettlementForCourierPrompt(MobileParty party)
	{
		return MapSeaContextGuard.FindNearestSettlementForPrompt(party);
	}

	private static string FormatCourierSettlementNameWithType(Settlement settlement)
	{
		return MapSeaContextGuard.FormatSettlementNameWithTypeForPrompt(settlement);
	}

	private static object CreateCourierChatMessage(string role, string content)
	{
		return new
		{
			role = role ?? "",
			content = content ?? ""
		};
	}

	private static void AppendCourierRawUserSection(StringBuilder builder, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		builder.AppendLine(content.Trim());
	}

	private static void AppendCourierUserSection(StringBuilder builder, string title, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		if (!string.IsNullOrWhiteSpace(title))
		{
			builder.AppendLine(title.Trim());
		}
		builder.AppendLine(content.Trim());
	}

	private static void AppendCourierPersistentMemoryRoleMessages(List<object> messages, IEnumerable<ConversationMessage> historyMessages, string npcName, string playerName)
	{
		if (messages == null || historyMessages == null)
		{
			return;
		}
		foreach (ConversationMessage message in historyMessages.Where((ConversationMessage x) => x != null && !string.IsNullOrWhiteSpace(x.Content)))
		{
			if (TryConvertCourierMemoryMessageToChatMessage(message, npcName, playerName, out var chatMessage))
			{
				messages.Add(chatMessage);
			}
		}
	}

	private static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker);
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && IsCourierMemorySpeakerRecipient(speaker, npcName))
		{
			chatMessage = CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName));
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往行为】" + StripCourierPromptScopeLabel(content));
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? "某NPC" : speaker;
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往听闻】" + otherSpeaker + "说：" + StripCourierSpeakerPrefix(content, otherSpeaker));
			return true;
		}
		string player = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
		chatMessage = CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, player));
		return true;
	}

	private static bool IsCourierMemorySpeakerRecipient(string speaker, string npcName)
	{
		string left = (speaker ?? "").Trim();
		string right = (npcName ?? "").Trim();
		return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
	{
		string date = string.IsNullOrWhiteSpace(message?.GameDate) ? ("第" + Math.Max(0, message?.GameDayIndex ?? 0) + "日") : message.GameDate.Trim();
		int hour = Math.Max(0, Math.Min(23, message?.GameHour ?? 0));
		string scene = (message?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string speaker = string.IsNullOrWhiteSpace(fallbackSpeaker) ? "记录" : fallbackSpeaker.Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			return "[" + date + " " + hour + "时｜" + speaker + "] ";
		}
		return "[" + date + " " + hour + "时｜" + scene + "｜" + speaker + "] ";
	}

	private static string StripCourierPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		bool changed;
		do
		{
			changed = false;
			string[] prefixes = new string[4] { "【当下行为】", "【过往行为】", "[当下行为]", "[过往行为]" };
			foreach (string prefix in prefixes)
			{
				if (value.StartsWith(prefix, StringComparison.Ordinal))
				{
					value = value.Substring(prefix.Length).Trim();
					changed = true;
				}
			}
		}
		while (changed);
		return value;
	}

	private static string StripCourierSpeakerPrefix(string content, string speaker)
	{
		string text = (content ?? "").Trim();
		string name = (speaker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(name))
		{
			return text;
		}
		string[] prefixes = new string[2] { name + ": ", name + "：" };
		foreach (string prefix in prefixes)
		{
			if (text.StartsWith(prefix, StringComparison.Ordinal))
			{
				return text.Substring(prefix.Length).Trim();
			}
		}
		return text;
	}

	private static string BuildDeliveryFactText(CourierSession session, bool delivered, Hero recipient = null, bool commitPlayerCraftInspection = false)
	{
		if (session == null)
		{
			return "";
		}
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		StringBuilder sb = new StringBuilder();
		if (delivered)
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("通过信使向你寄来一封信。");
		}
		else
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("已安排信使携带一封信出发。");
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			sb.Append("\n[AFEF玩家行为补充] ").Append(playerName)
				.Append(delivered ? "派来送达这封信的信使队成员为：" : "安排的送信队伍成员为：")
				.Append(crewSummary)
				.Append("。这些成员只负责送信与护送回信，不属于随信赠与或转移给你的物资或部队。");
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			string verb = delivered ? "通过信使" : "准备通过信使";
			string factEntryName = (entry.Kind == "item" || entry.Kind == "show_item")
				? GetCourierLetterTransferFactDescriptionForExternal(entry.Id, 0u, entry.Name)
				: entry.Name;
			if (delivered
				&& entry.Delivered
				&& (entry.Kind == "item" || entry.Kind == "show_item")
				&& !string.IsNullOrWhiteSpace(entry.Id))
			{
				string observerKey = (recipient?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(observerKey))
				{
					observerKey = MyBehavior.BuildRuleTargetKeyForExternal(recipient, recipient?.CharacterObject, -1);
				}
				factEntryName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
					entry.Id,
					0u,
					factEntryName,
					recipient,
					recipient?.CharacterObject,
					observerKey,
					entry.Kind == "show_item" ? "show" : "give",
					commitPlayerCraftInspection);
			}
			if (entry.Kind == "gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 第纳尔").Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 个 ").Append(factEntryName).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 第纳尔，但没有转移所有权。");
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 个 ").Append(factEntryName).Append("，但没有转移所有权。");
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append(entry.IsHero ? "转移了俘虏 " : "转移了俘虏 ").Append(entry.IsHero ? entry.Name : (entry.Amount + " 名 " + entry.Name)).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了固定资产 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
		}
		return sb.ToString().Trim();
	}

	private static string BuildInboundDeliveryFactText(CourierSession session, bool delivered, Hero sender = null)
	{
		if (session == null)
		{
			return "";
		}
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string letterKind = GetInboundLetterKindDisplayText(session);
		string fact = delivered
			? "[AFEF NPC行为补充] " + senderName + "通过信使给" + playerName + "送来一封" + letterKind + "。"
			: "[AFEF NPC行为补充] " + senderName + "已经派出信使，准备把一封" + letterKind + "送给" + playerName + "。";
		if (!string.IsNullOrWhiteSpace(session.InboundIntentFact))
		{
			fact += "\n" + session.InboundIntentFact.Trim();
		}
		try
		{
			IFaction senderFaction = sender?.MapFaction;
			IFaction playerFaction = Hero.MainHero?.MapFaction;
			if (senderFaction != null && playerFaction != null && senderFaction.IsAtWarWith(playerFaction))
			{
				fact += "\n[AFEF NPC行为补充] " + senderName + "所属势力与" + playerName + "所属势力当前仍处于战争状态；这封信属于私人或秘密通信，不代表停战、议和已经成立或敌对关系已经解除。";
			}
		}
		catch
		{
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			fact += "\n[AFEF NPC行为补充] " + senderName + "派出的信使队成员为：" + crewSummary + "。这些成员只负责送信，不属于随信赠与或转移给" + playerName + "的物资或部队。";
		}
		return fact;
	}

	private static string GetInboundLetterKindDisplayText(CourierSession session)
	{
		string kind = (session?.InboundLetterKind ?? "").Trim();
		if (string.Equals(kind, InboundLetterKindNeedRequest, StringComparison.OrdinalIgnoreCase)) return "请求信";
		if (string.Equals(kind, InboundLetterKindDiplomacy, StringComparison.OrdinalIgnoreCase)) return "外交信";
		if (string.Equals(kind, InboundLetterKindPersonal, StringComparison.OrdinalIgnoreCase)) return "私人来信";
		return "来信";
	}

	private static string BuildCourierCrewSummaryForPrompt(CourierSession session)
	{
		List<string> parts = new List<string>();
		foreach (CourierCargoEntry entry in session?.CrewEntries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			if (!string.Equals(entry.Kind ?? "", "crew", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string name = string.IsNullOrWhiteSpace(entry.Name) ? (entry.Id ?? "").Trim() : entry.Name.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}
			string role = entry.IsHero ? "角色" : "士兵";
			parts.Add(name + "×" + Math.Max(1, entry.Amount) + "（" + role + "）");
		}
		return parts.Count == 0 ? "" : string.Join("，", parts);
	}

	private static string BuildCourierCargoValueSuffix(CourierCargoEntry entry)
	{
		long value = EstimateCourierCargoEntryTotalValue(entry);
		return value > 0L ? ("（估值约 " + value + " 第纳尔）") : "";
	}

	private static long EstimateCourierCargoEntryTotalValue(CourierCargoEntry entry)
	{
		if (entry == null || entry.Amount <= 0)
		{
			return 0L;
		}
		string kind = (entry.Kind ?? "").Trim();
		if (string.Equals(kind, "gold", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Max(0, entry.Amount);
		}
		int unitValue = Math.Max(0, entry.GuidePriceDenars);
		if (unitValue <= 0 && string.Equals(kind, "item", StringComparison.OrdinalIgnoreCase))
		{
			unitValue = EstimateCourierItemUnitValue(ResolveItem(entry.Id));
		}
		if (unitValue <= 0)
		{
			return 0L;
		}
		int amount = string.Equals(kind, "settlement", StringComparison.OrdinalIgnoreCase) || entry.IsHero ? 1 : Math.Max(1, entry.Amount);
		return (long)amount * unitValue;
	}

	private static string BuildPendingPayloadSummary(PendingCourierFlow flow)
	{
		if (flow == null)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("信使成员：");
		sb.AppendLine("  " + RosterSummary(flow.CrewRoster));
		if (flow.Mode == CourierPayloadMode.Normal || flow.SelectedEntries.Count == 0)
		{
			sb.AppendLine("随信内容：仅信件。");
			return sb.ToString().Trim();
		}
		sb.AppendLine("随信内容：");
		foreach (CourierCargoEntry entry in flow.SelectedEntries)
		{
			if (entry == null)
			{
				continue;
			}
			sb.Append("  · ");
			if (entry.Kind == "gold")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("转移 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name);
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append(entry.IsHero ? ("转移俘虏 " + entry.Name) : ("转移 " + entry.Amount + " 名 " + entry.Name + " 俘虏"));
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("转移固定资产 ").Append(entry.Name);
			}
			sb.AppendLine();
		}
		return sb.ToString().Trim();
	}

	private List<CourierTradeOption> BuildCourierTradeOptions(PendingCourierFlow flow, CourierPayloadMode mode)
	{
		List<CourierTradeOption> list = new List<CourierTradeOption>();
		if (flow?.Recipient == null)
		{
			return list;
		}
		if (mode == CourierPayloadMode.GiveTroops || mode == CourierPayloadMode.GivePrisoners)
		{
			List<MyBehavior.PartyTransferPromptEntry> entries = MyBehavior.BuildPartyTransferPromptEntriesForExternal(flow.Recipient, flow.Recipient.CharacterObject, -1);
			MyBehavior.PartyTransferEntrySection section = mode == CourierPayloadMode.GiveTroops ? MyBehavior.PartyTransferEntrySection.PlayerTroops : MyBehavior.PartyTransferEntrySection.PlayerPrisoners;
			foreach (MyBehavior.PartyTransferPromptEntry entry in entries.Where(x => x != null && x.Section == section))
			{
				int available = Math.Max(0, entry.Count);
				if (mode == CourierPayloadMode.GiveTroops)
				{
					available -= CountRosterCharacter(flow.CrewRoster, entry.Character);
				}
				if (available <= 0)
				{
					continue;
				}
				list.Add(new CourierTradeOption
				{
					Kind = mode == CourierPayloadMode.GiveTroops ? "troop" : "prisoner",
					Id = entry.Character?.StringId ?? "",
					Name = entry.DisplayName,
					AvailableAmount = available,
					GuidePriceDenars = mode == CourierPayloadMode.GiveTroops ? Math.Max(1, entry.HirePriceDenarsPerUnit) : Math.Max(1, entry.BuyPriceDenarsPerUnit),
					PartyEntry = entry
				});
			}
			return list;
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			foreach (MyBehavior.SettlementTransferPromptEntry entry in MyBehavior.BuildSettlementTransferPromptEntriesForExternal(flow.Recipient, flow.Recipient.CharacterObject).Where(x => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)))
			{
				list.Add(new CourierTradeOption
				{
					Kind = "settlement",
					Id = MyBehavior.GetSettlementTransferAssetIdForExternal(entry),
					Name = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(entry),
					AvailableAmount = 1,
					GuidePriceDenars = Math.Max(0, entry.GuidePriceDenars),
					SettlementEntry = entry
				});
			}
			return list;
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			return list;
		}
		string shownKey = BuildCourierShownTargetKey(flow.Recipient);
		int gold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		if (mode == CourierPayloadMode.Show)
		{
			gold = MyBehavior.GetRemainingShowableGoldForExternal(flow.Recipient, shownKey, gold);
		}
		if (gold > 0)
		{
			list.Add(new CourierTradeOption
			{
				Kind = mode == CourierPayloadMode.Show ? "show_gold" : "gold",
				Id = "gold",
				Name = "第纳尔",
				AvailableAmount = gold,
				GuidePriceDenars = 1
			});
		}
		ItemRoster itemRoster = mainParty.ItemRoster;
		if (itemRoster == null)
		{
			return list;
		}
		Dictionary<string, CourierTradeOption> byItem = new Dictionary<string, CourierTradeOption>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement element = itemRoster.GetElementCopyAtIndex(i);
			ItemObject item = element.EquipmentElement.Item;
			string id = (item?.StringId ?? "").Trim();
			if (item == null || string.IsNullOrWhiteSpace(id) || element.Amount <= 0)
			{
				continue;
			}
			if (!byItem.TryGetValue(id, out var option))
			{
				option = new CourierTradeOption
				{
					Kind = mode == CourierPayloadMode.Show ? "show_item" : "item",
					Id = id,
					Name = item.Name?.ToString() ?? id,
					AvailableAmount = 0,
					GuidePriceDenars = EstimateCourierItemUnitValue(item),
					Item = item
				};
				byItem[id] = option;
			}
			option.AvailableAmount += element.Amount;
		}
		foreach (CourierTradeOption option in byItem.Values)
		{
			int available = option.AvailableAmount;
			if (mode == CourierPayloadMode.Show)
			{
				available = MyBehavior.GetRemainingShowableItemCountForExternal(flow.Recipient, shownKey, option.Id, available);
			}
			if (available > 0)
			{
				option.AvailableAmount = available;
				list.Add(option);
			}
		}
		return list;
	}

	private static int EstimateCourierItemUnitValue(ItemObject item)
	{
		if (item == null)
		{
			return 0;
		}
		try
		{
			long value = RewardSystemBehavior.Instance?.EstimateItemValueForExternal(Hero.MainHero, item, 1) ?? 0L;
			if (value > 0L)
			{
				return (int)Math.Min(int.MaxValue, value);
			}
		}
		catch
		{
		}
		return Math.Max(1, item.Value);
	}

	private static int EstimateCourierPartyTransferUnitValue(Hero recipient, CourierCargoEntry entry, bool isPrisoner)
	{
		try
		{
			MyBehavior.PartyTransferEntrySection section = isPrisoner ? MyBehavior.PartyTransferEntrySection.PlayerPrisoners : MyBehavior.PartyTransferEntrySection.PlayerTroops;
			string id = (entry?.Id ?? "").Trim();
			string name = (entry?.Name ?? "").Trim();
			MyBehavior.PartyTransferPromptEntry match = MyBehavior.BuildPartyTransferPromptEntriesForExternal(recipient, recipient?.CharacterObject, -1)
				.FirstOrDefault(x => x != null && x.Section == section && (string.Equals((x.Character?.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase)));
			if (match == null)
			{
				return 0;
			}
			return isPrisoner ? Math.Max(1, match.BuyPriceDenarsPerUnit) : Math.Max(1, match.HirePriceDenarsPerUnit);
		}
		catch
		{
			return 0;
		}
	}

	private static int EstimateCourierSettlementTransferValue(Hero recipient, CourierCargoEntry entry)
	{
		try
		{
			string id = (entry?.Id ?? "").Trim();
			string name = (entry?.Name ?? "").Trim();
			MyBehavior.SettlementTransferPromptEntry match = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(recipient, recipient?.CharacterObject)
				.FirstOrDefault(x => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x) && (string.Equals(MyBehavior.GetSettlementTransferAssetIdForExternal(x), id, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase) || string.Equals(MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(x), name, StringComparison.OrdinalIgnoreCase)));
			return Math.Max(0, match?.GuidePriceDenars ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string BuildEmptyPayloadMessage(CourierPayloadMode mode)
	{
		if (mode == CourierPayloadMode.GiveTroops)
		{
			return "你当前没有可转移给对方的部队。";
		}
		if (mode == CourierPayloadMode.GivePrisoners)
		{
			return "你当前没有可转移给对方的俘虏。";
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			return "你当前没有可转移给对方的固定资产。";
		}
		return "你没有可用的物品或第纳尔。";
	}

	private static string BuildPayloadTitle(CourierPayloadMode mode, string targetName)
	{
		string prefix = mode == CourierPayloadMode.Give ? "发送物品并写信" : mode == CourierPayloadMode.Show ? "展示物品并写信" : mode == CourierPayloadMode.GiveTroops ? "转移部队并写信" : mode == CourierPayloadMode.GivePrisoners ? "转移俘虏并写信" : "转移固定资产并写信";
		return prefix + " - " + targetName;
	}

	private static string BuildPayloadDescription(CourierPayloadMode mode, string targetName)
	{
		if (mode == CourierPayloadMode.GiveTroops)
		{
			return "当前收件人：" + targetName + "\n选择要随信转入对方麾下的部队（可多选）：";
		}
		if (mode == CourierPayloadMode.GivePrisoners)
		{
			return "当前收件人：" + targetName + "\n选择要随信交给对方的俘虏（可多选）：";
		}
		if (mode == CourierPayloadMode.GiveSettlements)
		{
			return "当前收件人：" + targetName + "\n选择要随信转给对方的固定资产（可多选）：";
		}
		return "当前收件人：" + targetName + "\n选择要" + (mode == CourierPayloadMode.Show ? "展示" : "发送") + "的物品或第纳尔（可多选）：";
	}

	private static TroopRoster BuildSelectionRosterFromUi(TroopRoster source)
	{
		TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return roster;
		}
		foreach (TroopRosterElement item in SnapshotRoster(source))
		{
			if (item.Character == null || item.Number <= 0 || item.Character.IsPlayerCharacter)
			{
				continue;
			}
			roster.AddToCounts(item.Character, item.Number, false, item.WoundedNumber, item.Xp, true, -1);
		}
		return roster;
	}

	private static List<TroopRosterElement> SnapshotRoster(TroopRoster roster)
	{
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		if (roster == null)
		{
			return list;
		}
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			list.Add(item);
		}
		return list;
	}

	private static List<CourierCargoEntry> BuildCargoEntriesFromRoster(TroopRoster roster, string kind)
	{
		List<CourierCargoEntry> list = new List<CourierCargoEntry>();
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			list.Add(new CourierCargoEntry
			{
				Kind = kind,
				Id = character.StringId ?? "",
				Name = character.Name?.ToString() ?? character.StringId ?? "",
				Amount = item.Number,
				IsHero = character.IsHero
			});
		}
		return list;
	}

	private static void MoveRosterFromMainParty(TroopRoster selectedRoster, MobileParty targetParty, string label)
	{
		foreach (TroopRosterElement item in SnapshotRoster(selectedRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || character.IsPlayerCharacter || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				AddHeroToPartyAction.Apply(character.HeroObject, targetParty, false);
				continue;
			}
			MoveRegularMember(MobileParty.MainParty?.Party, targetParty?.Party, character, item.Number, item.WoundedNumber, item.Xp);
		}
	}

	private static void MoveCharacterFromMainMembersToParty(string characterId, int amount, MobileParty targetParty, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || targetParty == null || amount <= 0)
		{
			return;
		}
		if (isHero || character.IsHero)
		{
			AddHeroToPartyAction.Apply(character.HeroObject, targetParty, false);
			return;
		}
		MoveRegularMember(MobileParty.MainParty?.Party, targetParty.Party, character, amount, -1, -1);
	}

	private static bool IsCourierDungeonPrisonerSource(PartyBase sourceParty, Settlement sourceSettlement)
	{
		if (sourceParty == null || sourceSettlement == null || sourceSettlement.OwnerClan != Clan.PlayerClan || !sourceSettlement.IsFortification)
		{
			return false;
		}
		if (sourceParty == sourceSettlement.Party)
		{
			return true;
		}
		try
		{
			return sourceSettlement.Parties.Any((MobileParty x) => x != null && x.IsGarrison && x.Party == sourceParty);
		}
		catch
		{
			return false;
		}
	}

	private static int MoveCharacterFromPlayerPrisonerSourceToParty(string characterId, int amount, MobileParty targetParty, bool isHero, string sourceSettlementId)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || targetParty == null || amount <= 0)
		{
			return 0;
		}
		PartyBase sourceParty = MobileParty.MainParty?.Party;
		string settlementId = (sourceSettlementId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(settlementId))
		{
			if (!isHero && !character.IsHero)
			{
				return 0;
			}
			Settlement sourceSettlement = Settlement.Find(settlementId);
			sourceParty = character.HeroObject?.PartyBelongedToAsPrisoner;
			if (!IsCourierDungeonPrisonerSource(sourceParty, sourceSettlement))
			{
				return 0;
			}
		}
		return MoveCharacterBetweenPrisonRosters(sourceParty, targetParty.Party, characterId, amount, isHero);
	}

	private static int MoveCharacterBetweenMemberRosters(PartyBase source, PartyBase target, string characterId, int amount, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || source == null || target == null || amount <= 0)
		{
			return 0;
		}
		if (isHero || character.IsHero)
		{
			try
			{
				AddHeroToPartyAction.Apply(character.HeroObject, target.MobileParty, false);
				return 1;
			}
			catch
			{
				return 0;
			}
		}
		return MoveRegularMember(source, target, character, amount, -1, -1);
	}

	private static int MoveCharacterBetweenPrisonRosters(PartyBase source, PartyBase target, string characterId, int amount, bool isHero)
	{
		CharacterObject character = ResolveCharacter(characterId);
		if (character == null || source == null || target == null || amount <= 0)
		{
			return 0;
		}
		if (isHero || character.IsHero)
		{
			Hero heroObject = character.HeroObject;
			if (heroObject == null || !heroObject.IsPrisoner || heroObject.PartyBelongedToAsPrisoner != source || (source.PrisonRoster?.FindIndexOfTroop(character) ?? (-1)) < 0 || (target.PrisonRoster?.FindIndexOfTroop(character) ?? (-1)) >= 0)
			{
				return 0;
			}
			try
			{
				TransferPrisonerAction.Apply(character, source, target);
				return heroObject.PartyBelongedToAsPrisoner == target && (target.PrisonRoster?.FindIndexOfTroop(character) ?? (-1)) >= 0 ? 1 : 0;
			}
			catch
			{
				return 0;
			}
		}
		return MoveRegularPrisoner(source, target, character, amount);
	}

	private static int MoveRegularMember(PartyBase source, PartyBase target, CharacterObject character, int amount, int woundedOverride, int xpOverride)
	{
		TroopRoster sourceRoster = source?.MemberRoster;
		TroopRoster targetRoster = target?.MemberRoster;
		if (sourceRoster == null || targetRoster == null || character == null || amount <= 0)
		{
			return 0;
		}
		int index = sourceRoster.FindIndexOfTroop(character);
		if (index < 0)
		{
			return 0;
		}
		TroopRosterElement sourceElement = sourceRoster.GetElementCopyAtIndex(index);
		int moved = Math.Min(amount, Math.Max(0, sourceElement.Number));
		if (moved <= 0)
		{
			return 0;
		}
		int wounded = woundedOverride >= 0 ? Math.Min(woundedOverride, moved) : CalculateProportional(sourceElement.WoundedNumber, sourceElement.Number, moved);
		int xp = xpOverride >= 0 ? Math.Min(xpOverride, sourceElement.Xp) : CalculateProportional(sourceElement.Xp, sourceElement.Number, moved);
		sourceRoster.AddToCounts(character, -moved, false, -wounded, -xp, true, -1);
		targetRoster.AddToCounts(character, moved, false, wounded, xp, true, -1);
		return moved;
	}

	private static int MoveRegularPrisoner(PartyBase source, PartyBase target, CharacterObject character, int amount)
	{
		TroopRoster sourceRoster = source?.PrisonRoster;
		if (sourceRoster == null || target == null || character == null || amount <= 0)
		{
			return 0;
		}
		int index = sourceRoster.FindIndexOfTroop(character);
		if (index < 0)
		{
			return 0;
		}
		TroopRosterElement sourceElement = sourceRoster.GetElementCopyAtIndex(index);
		int moved = Math.Min(amount, Math.Max(0, sourceElement.Number));
		if (moved <= 0)
		{
			return 0;
		}
		int xp = CalculateProportional(sourceElement.Xp, sourceElement.Number, moved);
		sourceRoster.AddToCounts(character, -moved, false, 0, -xp, true, -1);
		target.AddPrisoner(character, moved);
		if (xp > 0)
		{
			target.PrisonRoster?.AddXpToTroop(character, xp);
		}
		return moved;
	}

	private static void MoveWholeMemberRoster(PartyBase source, PartyBase target)
	{
		foreach (TroopRosterElement item in SnapshotRoster(source?.MemberRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					AddHeroToPartyAction.Apply(character.HeroObject, target.MobileParty, false);
				}
				catch
				{
				}
			}
			else
			{
				MoveRegularMember(source, target, character, item.Number, item.WoundedNumber, item.Xp);
			}
		}
	}

	private static void MoveWholePrisonRoster(PartyBase source, PartyBase target)
	{
		foreach (TroopRosterElement item in SnapshotRoster(source?.PrisonRoster))
		{
			CharacterObject character = item.Character;
			if (character == null || item.Number <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					TransferPrisonerAction.Apply(character, source, target);
				}
				catch
				{
				}
			}
			else
			{
				MoveRegularPrisoner(source, target, character, item.Number);
			}
		}
	}

	private static void MoveWholeItemRoster(ItemRoster source, ItemRoster target)
	{
		if (source == null || target == null)
		{
			return;
		}
		for (int i = source.Count - 1; i >= 0; i--)
		{
			ItemRosterElement element = source.GetElementCopyAtIndex(i);
			if (element.Amount <= 0 || element.EquipmentElement.Item == null)
			{
				continue;
			}
			source.AddToCounts(element.EquipmentElement, -element.Amount);
			target.AddToCounts(element.EquipmentElement, element.Amount);
		}
	}

	private static int CalculateProportional(int totalValue, int totalCount, int movedCount)
	{
		if (totalValue <= 0 || totalCount <= 0 || movedCount <= 0)
		{
			return 0;
		}
		if (movedCount >= totalCount)
		{
			return totalValue;
		}
		return Math.Max(0, Math.Min(totalValue, (int)Math.Round((double)totalValue * movedCount / totalCount, MidpointRounding.AwayFromZero)));
	}

	private static CharacterObject ResolveCharacter(string characterId)
	{
		string id = (characterId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return MBObjectManager.Instance?.GetObject<CharacterObject>(id) ?? Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>()?.FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static ItemObject ResolveItem(string itemId)
	{
		string id = (itemId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(id);
		}
		catch
		{
			return null;
		}
	}

	private static PartyBase ResolveRecipientPartyBase(Hero recipient)
	{
		if (recipient?.PartyBelongedTo?.Party != null)
		{
			return recipient.PartyBelongedTo.Party;
		}
		if (recipient?.PartyBelongedToAsPrisoner != null)
		{
			return recipient.PartyBelongedToAsPrisoner;
		}
		if (recipient?.Clan?.Leader?.PartyBelongedTo?.Party != null)
		{
			return recipient.Clan.Leader.PartyBelongedTo.Party;
		}
		if (recipient?.CurrentSettlement?.Town?.GarrisonParty?.Party != null)
		{
			return recipient.CurrentSettlement.Town.GarrisonParty.Party;
		}
		return recipient?.CurrentSettlement?.Party;
	}

	private static ItemRoster ResolveRecipientItemRoster(Hero recipient)
	{
		if (recipient?.PartyBelongedTo?.ItemRoster != null)
		{
			return recipient.PartyBelongedTo.ItemRoster;
		}
		if (recipient?.CurrentSettlement?.ItemRoster != null)
		{
			return recipient.CurrentSettlement.ItemRoster;
		}
		return recipient?.Clan?.Leader?.PartyBelongedTo?.ItemRoster;
	}

	private static int CountRosterCharacter(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return 0;
		}
		int index = roster.FindIndexOfTroop(character);
		return index < 0 ? 0 : Math.Max(0, roster.GetElementCopyAtIndex(index).Number);
	}

	private static void ApplyCourierAiOverrides(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			EnsureCourierCampaignIdentity(courier, TryFindCourierSessionByPartyId(courier.StringId), reason);
			EnsureCourierNonSettlementCombatState(courier, reason);
			if (courier.Ai != null)
			{
				if (!courier.Ai.DoNotMakeNewDecisions)
				{
					courier.Ai.SetDoNotMakeNewDecisions(true);
					LogVerbose("ai_lock:" + (courier.StringId ?? ""), "ai decisions locked party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""), 10.0);
					LogCourierStatusVerbose("ai_lock:" + (courier.StringId ?? ""), "ai_lock sessionParty=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier), 10.0);
				}
			}
			ApplyCourierFoodOverrides(courier, reason);
			ApplyCourierMapBannerVisual(courier, reason);
		}
		catch (Exception ex)
		{
			Log("ai override failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void EnsureCourierNonSettlementCombatState(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			bool forbiddenBehavior = IsCourierForbiddenSettlementCombatBehavior(courier.DefaultBehavior)
				|| IsCourierForbiddenSettlementCombatBehavior(courier.ShortTermBehavior);
			bool inSiege = courier.BesiegerCamp != null || courier.SiegeEvent != null || courier.BesiegedSettlement != null;
			if (!forbiddenBehavior && !inSiege)
			{
				return;
			}
			string before = DescribeMobileParty(courier);
			if (courier.BesiegerCamp != null)
			{
				try
				{
					courier.BesiegerCamp = null;
				}
				catch (Exception ex)
				{
					Log("courier siege detach failed party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
				}
			}
			if (forbiddenBehavior || courier.BesiegerCamp != null || courier.SiegeEvent != null || courier.BesiegedSettlement != null)
			{
				courier.SetMoveModeHold();
			}
			LogCourierStatusVerbose("settlement_combat_suppressed:" + (courier.StringId ?? ""), "settlement_combat_suppressed party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " before=" + before + " after=" + DescribeMobileParty(courier), 5.0);
		}
		catch (Exception ex)
		{
			Log("courier settlement combat guard failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsCourierForbiddenSettlementCombatBehavior(AiBehavior behavior)
	{
		return behavior == AiBehavior.BesiegeSettlement
			|| behavior == AiBehavior.AssaultSettlement
			|| behavior == AiBehavior.RaidSettlement
			|| behavior == AiBehavior.DefendSettlement;
	}

	private static void ApplyCourierMapBannerVisual(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			CourierSession session = TryFindCourierSessionByPartyId(courier.StringId);
			Clan visualClan = ResolveCourierIdentityClan(courier, SafePartyOwner(courier.Party), session);
			if (visualClan != null)
			{
				courier.ActualClan = visualClan;
			}
			courier.IsVisible = true;
			courier.IsInspected = true;
			EnsureCourierVisualTracked(courier, reason);
			if (!courier.IsCurrentlyUsedByAQuest)
			{
				courier.SetPartyUsedByQuest(true);
				LogVerbose("tracker_quest_flag:" + (courier.StringId ?? ""), "map tracker quest flag applied party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""), 10.0);
			}
			else
			{
				PulseCourierTrackerQuestEvent(courier, reason);
			}
			courier.Party?.SetVisualAsDirty();
			if (string.Equals(reason ?? "", "create", StringComparison.OrdinalIgnoreCase) || string.Equals(reason ?? "", "load_restore", StringComparison.OrdinalIgnoreCase))
			{
				Log("banner visual applied party=" + (courier.StringId ?? "") + " clan=" + (courier.ActualClan?.StringId ?? "null") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("banner visual failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void EnsureCourierVisualTracked(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || !IsCourierParty(courier) || Campaign.Current?.VisualTrackerManager == null)
			{
				return;
			}
			bool beforeTracked = Campaign.Current.VisualTrackerManager.CheckTracked(courier);
			LogCourierTrackerSnapshot(courier, reason, "before_register", beforeTracked);
			if (!beforeTracked)
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(courier);
				Log("map tracker registered party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""));
			}
			LogCourierTrackerSnapshot(courier, reason, "after_register", Campaign.Current.VisualTrackerManager.CheckTracked(courier));
		}
		catch (Exception ex)
		{
			Log("map tracker register failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void PulseCourierTrackerQuestEvent(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || !IsCourierParty(courier) || Campaign.Current?.VisualTrackerManager == null || !Campaign.Current.VisualTrackerManager.CheckTracked(courier))
			{
				return;
			}
			string id = courier.StringId ?? "";
			long now = DateTime.UtcNow.Ticks;
			if (LastTrackerEventPulseTicks.TryGetValue(id, out long last) && now - last < TimeSpan.FromSeconds(8).Ticks)
			{
				return;
			}
			LastTrackerEventPulseTicks[id] = now;
			courier.SetPartyUsedByQuest(false);
			courier.SetPartyUsedByQuest(true);
			LogVerbose("tracker_pulse:" + id, "map tracker quest event pulsed party=" + id + " reason=" + (reason ?? ""), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker quest event pulse failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void LogCourierTrackerSnapshot(MobileParty courier, string reason, string phase, bool tracked)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			LogVerbose("tracker_snapshot:" + (courier.StringId ?? "") + ":" + (phase ?? ""), "map tracker snapshot phase=" + (phase ?? "") +
				" party=" + (courier.StringId ?? "") +
				" reason=" + (reason ?? "") +
				" tracked=" + tracked +
				" questUsed=" + courier.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (courier.LeaderHero == null) +
				" active=" + courier.IsActive +
				" visible=" + courier.IsVisible +
				" inspected=" + courier.IsInspected +
				" usedByQuest=" + courier.IsCurrentlyUsedByAQuest +
				" actualClan=" + (courier.ActualClan?.StringId ?? "null") +
				" mapFaction=" + (courier.MapFaction?.StringId ?? "null") +
				" component=" + (courier.PartyComponent?.GetType().FullName ?? "null"), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker snapshot failed party=" + (courier?.StringId ?? "") + " phase=" + (phase ?? "") + " error=" + ex.Message);
		}
	}

	private static void UntrackCourierMapVisual(MobileParty courier, string reason)
	{
		try
		{
			if (courier == null || Campaign.Current?.VisualTrackerManager == null)
			{
				return;
			}
			if (Campaign.Current.VisualTrackerManager.CheckTracked(courier))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(courier, true);
				Log("map tracker unregistered party=" + (courier.StringId ?? "") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Log("map tracker unregister failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void TryPatchPartyNameplateForCourierBanner()
	{
		if (_partyNameplatePatchApplied || _partyNameplatePatchFailed)
		{
			return;
		}
		try
		{
			Harmony harmony = _courierHarmony ?? new Harmony("AnimusForge.courier.delivery");
			Type nameplateType = AccessTools.TypeByName("SandBox.ViewModelCollection.Nameplate.PartyNameplateVM");
			MethodInfo method = nameplateType == null ? null : AccessTools.Method(nameplateType, "RefreshBinding");
			MethodInfo postfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(PartyNameplateRefreshBindingCourierPostfix));
			if (method == null || postfix == null)
			{
				_partyNameplatePatchFailed = true;
				Log("party nameplate delayed patch skipped method_missing type=" + (nameplateType == null ? "null" : nameplateType.FullName));
				return;
			}
			harmony.Patch(method, postfix: new HarmonyMethod(postfix));
			_partyNameplatePatchApplied = true;
			Log("party nameplate delayed patch applied");
		}
		catch (Exception ex)
		{
			_partyNameplatePatchFailed = true;
			Log("party nameplate delayed patch failed: " + ex);
		}
	}

	public static void PartyNameplateRefreshBindingCourierPostfix(object __instance)
	{
		try
		{
			if (__instance == null)
			{
				return;
			}
			Type type = __instance.GetType();
			PropertyInfo partyProperty = type.GetProperty("Party", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MobileParty party = partyProperty?.GetValue(__instance, null) as MobileParty;
			if (!IsCourierParty(party))
			{
				return;
			}
			type.GetProperty("IsArmy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.GetProperty("ShouldShowFullName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.BaseType?.GetProperty("IsVisibleOnMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, true, null);
			type.GetProperty("PartyBanner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(__instance, CreateCourierBannerImageIdentifier(party), null);
		}
		catch
		{
		}
	}

	private static object CreateCourierBannerImageIdentifier(MobileParty party)
	{
		try
		{
			Banner banner = party?.Banner ?? Clan.PlayerClan?.Banner ?? Hero.MainHero?.Clan?.Banner;
			if (banner == null)
			{
				return null;
			}
			Type bannerVmType = AccessTools.TypeByName("TaleWorlds.Core.ViewModelCollection.ImageIdentifiers.BannerImageIdentifierVM");
			ConstructorInfo ctor = bannerVmType?.GetConstructor(new[] { typeof(Banner), typeof(bool) });
			return ctor?.Invoke(new object[] { banner, true });
		}
		catch
		{
			return null;
		}
	}

	private static void TryPatchMapTrackerProviderForCourierDiagnostics()
	{
		if (_mapTrackerProviderPatchApplied || _mapTrackerProviderPatchFailed)
		{
			return;
		}
		try
		{
			Harmony harmony = _courierHarmony ?? new Harmony("AnimusForge.courier.delivery");
			Type providerType = AccessTools.TypeByName("SandBox.ViewModelCollection.Map.Tracker.MapTrackerProvider");
			MethodInfo canAdd = providerType == null ? null : AccessTools.Method(providerType, "CanAddMobileParty", new[] { typeof(MobileParty) });
			MethodInfo addIfEligible = providerType == null ? null : AccessTools.Method(providerType, "AddIfEligible", new[] { typeof(MobileParty) });
			MethodInfo canAddPostfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(MapTrackerProviderCanAddMobilePartyCourierPostfix));
			MethodInfo addPostfix = AccessTools.Method(typeof(CourierDeliveryBehavior), nameof(MapTrackerProviderAddIfEligibleCourierPostfix));
			if (providerType == null || canAdd == null || addIfEligible == null || canAddPostfix == null || addPostfix == null)
			{
				_mapTrackerProviderPatchFailed = true;
				Log("map tracker provider diagnostics patch skipped type=" + (providerType?.FullName ?? "null") + " canAdd=" + (canAdd != null) + " addIfEligible=" + (addIfEligible != null));
				return;
			}
			harmony.Patch(canAdd, postfix: new HarmonyMethod(canAddPostfix));
			harmony.Patch(addIfEligible, postfix: new HarmonyMethod(addPostfix));
			_mapTrackerProviderPatchApplied = true;
			Log("map tracker provider diagnostics patch applied");
		}
		catch (Exception ex)
		{
			_mapTrackerProviderPatchFailed = true;
			Log("map tracker provider diagnostics patch failed: " + ex);
		}
	}

	public static void MapTrackerProviderCanAddMobilePartyCourierPostfix(MobileParty party, ref bool __result)
	{
		try
		{
			if (!IsCourierParty(party))
			{
				return;
			}
			bool tracked = Campaign.Current?.VisualTrackerManager != null && Campaign.Current.VisualTrackerManager.CheckTracked(party);
			if (tracked && party.IsActive)
			{
				__result = true;
			}
			LogVerbose("tracker_provider_can_add:" + (party.StringId ?? ""), "map tracker provider CanAddMobileParty courier party=" + (party.StringId ?? "") +
				" result=" + __result +
				" tracked=" + tracked +
				" questUsed=" + party.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (party.LeaderHero == null) +
				" active=" + party.IsActive +
				" forced=" + (tracked && party.IsActive) +
				" isQuestCondition=" + (party.LeaderHero == null && party.IsCurrentlyUsedByAQuest && tracked), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker provider CanAddMobileParty log failed: " + ex.Message);
		}
	}

	public static void MapTrackerProviderAddIfEligibleCourierPostfix(object __instance, MobileParty party)
	{
		try
		{
			if (!IsCourierParty(party))
			{
				return;
			}
			bool hasTracker = false;
			object container = __instance?.GetType().GetField("_trackerContainer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
			MethodInfo hasTrackerFor = container?.GetType().GetMethod("HasTrackerFor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (container != null && hasTrackerFor != null)
			{
				hasTracker = (bool)hasTrackerFor.Invoke(container, new object[] { party });
			}
			bool tracked = Campaign.Current?.VisualTrackerManager != null && Campaign.Current.VisualTrackerManager.CheckTracked(party);
			LogVerbose("tracker_provider_add:" + (party.StringId ?? ""), "map tracker provider AddIfEligible courier party=" + (party.StringId ?? "") +
				" hasTracker=" + hasTracker +
				" tracked=" + tracked +
				" questUsed=" + party.IsCurrentlyUsedByAQuest +
				" leaderNull=" + (party.LeaderHero == null), 10.0);
		}
		catch (Exception ex)
		{
			Log("map tracker provider AddIfEligible log failed: " + ex.Message);
		}
	}

	private static void ApplyCourierFoodOverrides(MobileParty courier, string reason)
	{
		try
		{
			PartyBase party = courier?.Party;
			if (party == null || party.RemainingFoodPercentage >= 0)
			{
				return;
			}
			int oldValue = party.RemainingFoodPercentage;
			party.RemainingFoodPercentage = 100;
			party.OnConsumedFood();
			LogVerbose("food_override:" + (courier.StringId ?? ""), "food override applied party=" + (courier.StringId ?? "") + " reason=" + (reason ?? "") + " oldRemaining=" + oldValue + " newRemaining=100", 30.0);
		}
		catch (Exception ex)
		{
			Log("food override failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void AssignCourierLeader(MobileParty courier)
	{
		try
		{
			Hero leader = SnapshotRoster(courier?.MemberRoster).Select(x => x.Character?.HeroObject).FirstOrDefault(x => x != null && !x.IsHumanPlayerCharacter && !x.IsDead);
			if (leader != null)
			{
				courier.PartyComponent?.ChangePartyLeader(leader);
				courier.Party.SetCustomOwner(leader);
			}
		}
		catch (Exception ex)
		{
			Log("assign leader failed: " + ex.Message);
		}
	}

	private static string BuildCourierShownTargetKey(Hero hero)
	{
		return "courier:" + (hero?.StringId ?? "").Trim().ToLowerInvariant();
	}

	private static Hero SafePartyOwner(PartyBase party)
	{
		try
		{
			return party?.Owner;
		}
		catch
		{
			return null;
		}
	}

	private static IFaction SafeMapFaction(PartyBase party)
	{
		try
		{
			return party?.MapFaction;
		}
		catch
		{
			return null;
		}
	}

	private static IFaction SafeMapFaction(MobileParty party)
	{
		try
		{
			return party?.MapFaction;
		}
		catch
		{
			return null;
		}
	}

	private static IFaction SafeClanMapFaction(Clan clan)
	{
		try
		{
			return clan?.MapFaction;
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveCourierIdentityOwner(CourierSession session, MobileParty courier)
	{
		Hero owner = SafePartyOwner(courier?.Party);
		if (owner != null && !owner.IsDead)
		{
			return owner;
		}
		if (IsInboundToPlayer(session))
		{
			Hero sender = ResolveHeroByIdForCourier(session?.SenderHeroId);
			if (sender != null && !sender.IsDead)
			{
				return sender;
			}
		}
		if (Hero.MainHero != null && !Hero.MainHero.IsDead)
		{
			return Hero.MainHero;
		}
		Hero recipient = ResolveHeroByIdForCourier(session?.RecipientHeroId);
		if (recipient != null && !recipient.IsDead)
		{
			return recipient;
		}
		Hero senderFallback = ResolveHeroByIdForCourier(session?.SenderHeroId);
		if (senderFallback != null && !senderFallback.IsDead)
		{
			return senderFallback;
		}
		Hero playerClanLeader = Clan.PlayerClan?.Leader;
		return playerClanLeader != null && !playerClanLeader.IsDead ? playerClanLeader : null;
	}

	private static Clan ResolveCourierIdentityClan(MobileParty courier, Hero owner, CourierSession session)
	{
		if (IsInboundToPlayer(session))
		{
			Hero sender = ResolveHeroByIdForCourier(session?.SenderHeroId);
			if (SafeClanMapFaction(sender?.Clan) != null)
			{
				return sender.Clan;
			}
			if (SafeClanMapFaction(owner?.Clan) != null)
			{
				return owner.Clan;
			}
			if (SafeClanMapFaction(courier?.ActualClan) != null)
			{
				return courier.ActualClan;
			}
		}
		if (SafeClanMapFaction(Clan.PlayerClan) != null)
		{
			return Clan.PlayerClan;
		}
		if (SafeClanMapFaction(owner?.Clan) != null)
		{
			return owner.Clan;
		}
		if (SafeClanMapFaction(courier?.ActualClan) != null)
		{
			return courier.ActualClan;
		}
		if (SafeClanMapFaction(Hero.MainHero?.Clan) != null)
		{
			return Hero.MainHero.Clan;
		}
		return courier?.ActualClan ?? owner?.Clan ?? Clan.PlayerClan ?? Hero.MainHero?.Clan;
	}

	private static void EnsureCourierCampaignIdentity(MobileParty courier, CourierSession session, string reason)
	{
		try
		{
			if (courier == null)
			{
				return;
			}
			MarkCourierCurrentMapEvent(courier);
			Hero owner = ResolveCourierIdentityOwner(session, courier);
			Hero currentOwner = SafePartyOwner(courier.Party);
			if ((currentOwner == null || currentOwner.IsDead) && owner != null && courier.Party != null)
			{
				courier.Party.SetCustomOwner(owner);
				currentOwner = owner;
			}
			Clan desiredClan = ResolveCourierIdentityClan(courier, currentOwner ?? owner, session);
			bool restoreNpcSenderClan = IsInboundToPlayer(session) && desiredClan != null && courier.ActualClan != desiredClan;
			if ((restoreNpcSenderClan || courier.ActualClan == null || SafeMapFaction(courier) == null) && desiredClan != null)
			{
				courier.ActualClan = desiredClan;
			}
			if (SafeMapFaction(courier) == null && Clan.PlayerClan != null)
			{
				courier.ActualClan = Clan.PlayerClan;
			}
			LogCourierStatusVerbose("identity:" + (courier.StringId ?? ""), "identity_checked party=" + (courier.StringId ?? "") +
				" reason=" + (reason ?? "") +
				" owner=" + (SafePartyOwner(courier.Party)?.StringId ?? "null") +
				" actualClan=" + (courier.ActualClan?.StringId ?? "null") +
				" mapFaction=" + (SafeMapFaction(courier)?.StringId ?? "null"), 10.0);
		}
		catch (Exception ex)
		{
			Log("courier identity repair failed party=" + (courier?.StringId ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static Dictionary<string, float> NormalizeFloatDictionary(Dictionary<string, float> source)
	{
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		if (source == null)
		{
			return result;
		}
		foreach (KeyValuePair<string, float> pair in source)
		{
			string key = (pair.Key ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key))
			{
				result[key] = pair.Value;
			}
		}
		return result;
	}

	private static Dictionary<string, string> NormalizeStringDictionary(Dictionary<string, string> source)
	{
		Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> pair in source ?? new Dictionary<string, string>())
		{
			string key = (pair.Key ?? "").Trim();
			string value = (pair.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				result[key] = value;
			}
		}
		return result;
	}

	private static int ClampInt(int value, int min, int max)
	{
		if (value < min) return min;
		return value > max ? max : value;
	}

	private static float ClampFloat(float value, float min, float max)
	{
		if (value < min) return min;
		return value > max ? max : value;
	}

	private static float NowHours()
	{
		try
		{
			return (float)CampaignTime.Now.ToHours;
		}
		catch
		{
			return 0f;
		}
	}

	private static float NowDays()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private void ResetPendingFlow(string reason)
	{
		Log("reset pending reason=" + reason);
		_pendingFlow = null;
		_letterInputOpen = false;
	}

	private static string RosterSummary(TroopRoster roster)
	{
		if (roster == null || roster.TotalManCount <= 0)
		{
			return "无";
		}
		List<string> parts = new List<string>();
		foreach (TroopRosterElement item in SnapshotRoster(roster))
		{
			if (item.Character == null || item.Number <= 0)
			{
				continue;
			}
			parts.Add((item.Character.Name?.ToString() ?? item.Character.StringId ?? "未知") + "×" + item.Number);
		}
		return parts.Count == 0 ? "无" : string.Join("，", parts);
	}

	private static string PrepareNpcReplyForActionPostprocess(string text)
	{
		string value = LlmVisibleReplyNormalizer.NormalizeComplete(text).Trim();
		value = Regex.Replace(value, "<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
		value = Regex.Replace(value, "^(NPC|回复|回信)[:：]\\s*", "", RegexOptions.IgnoreCase).Trim();
		if (value == "（没说话）" || value == "无" || value == "无回信")
		{
			return "";
		}
		return value;
	}

	private static string CleanNpcReply(string text)
	{
		string value = PrepareNpcReplyForActionPostprocess(text);
		if (!LooksLikeApiError(value))
		{
			value = CourierVisibleLetterSanitizer.Clean(value);
		}
		return value;
	}

	private static string NormalizeInboundLetterText(string text, CourierSession session, Hero sender)
	{
		string value = LlmVisibleReplyNormalizer.NormalizeComplete(text).Trim();
		value = Regex.Replace(value, "<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
		value = Regex.Replace(value, "^(NPC|来信|信件|外交信|私人来信|请求信)[:：]\\s*", "", RegexOptions.IgnoreCase).Trim();
		value = StripCourierActionTags(value).Trim();
		if (!LooksLikeApiError(value))
		{
			value = CourierVisibleLetterSanitizer.Clean(value);
		}
		if (!string.IsNullOrWhiteSpace(value) && value != "（没说话）" && value != "无" && value != "无信件")
		{
			return value;
		}
		string fallback = string.IsNullOrWhiteSpace(session?.InboundFallbackLetter) ? (session?.LetterText ?? "").Trim() : session.InboundFallbackLetter.Trim();
		if (!string.IsNullOrWhiteSpace(fallback))
		{
			fallback = CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(fallback));
			if (!string.IsNullOrWhiteSpace(fallback))
			{
				return fallback;
			}
		}
		string senderName = string.IsNullOrWhiteSpace(session?.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		if (string.Equals(session?.InboundLetterKind, InboundLetterKindNeedRequest, StringComparison.OrdinalIgnoreCase))
		{
			return senderName + "致" + playerName + "：\n\n我有一项确切的困难，希望与你商量。若你愿意，请回信。";
		}
		if (string.Equals(session?.InboundLetterKind, InboundLetterKindPersonal, StringComparison.OrdinalIgnoreCase))
		{
			return senderName + "致" + playerName + "：\n\n愿你近来安好。得空时，请回信告诉我你的近况。";
		}
		return senderName + "致" + playerName + "：\n\n我希望以正式信件与你讨论当前事务。若你愿意，请回信说明你的看法。";
	}

	private static bool LooksLikeApiError(string text)
	{
		string value = (text ?? "").Trim();
		return value.StartsWith("（错误", StringComparison.Ordinal) || value.StartsWith("（程序错误", StringComparison.Ordinal) || value.StartsWith("（API请求失败", StringComparison.Ordinal) || value.StartsWith("（API响应格式错误", StringComparison.Ordinal);
	}

	private static bool HasPreprocessRuleHit(List<string> hits, string ruleId)
	{
		string value = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value) || hits == null || hits.Count == 0)
		{
			return false;
		}
		return hits.Any(x => string.Equals((x ?? "").Trim(), value, StringComparison.OrdinalIgnoreCase));
	}

	private static void LogCourierContextAlignment(string chain, string sessionId, Hero hero, string npcRoleContext, string extras, string entityPostprocessContext, string historyText, IEnumerable<ConversationMessage> persistentMemoryRoleMessages)
	{
		string role = npcRoleContext ?? "";
		string extra = extras ?? "";
		bool hasPersonality = role.IndexOf("你的个性为：", StringComparison.Ordinal) >= 0;
		bool hasBackground = role.IndexOf("你的背景是：", StringComparison.Ordinal) >= 0;
		bool hasKnowledge = extra.IndexOf("参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0
			|| extra.IndexOf("【以下是关于（", StringComparison.Ordinal) >= 0;
		int persistentMemoryCount = persistentMemoryRoleMessages?.Count() ?? 0;
		Log("[ContextAlignment] chain=courier_" + (chain ?? "unknown")
			+ " session=" + (sessionId ?? "")
			+ " hero=" + SafeHeroId(hero)
			+ " personality=" + hasPersonality
			+ " background=" + hasBackground
			+ " knowledge=" + hasKnowledge
			+ " entityPostprocess=" + !string.IsNullOrWhiteSpace(entityPostprocessContext)
			+ " history=" + !string.IsNullOrWhiteSpace(historyText)
			+ " persistentMemoryCount=" + persistentMemoryCount
			+ " roleLen=" + role.Length
			+ " extrasLen=" + extra.Length);
	}

	private static string AppendCourierPlayerRecentActions(string extras, Hero recipient)
	{
		string playerRecent = PlayerNotorietyBehavior.BuildPlayerRecentRuntimeInstructionForExternal(recipient, courier: true);
		if (string.IsNullOrWhiteSpace(playerRecent))
		{
			return extras ?? "";
		}
		string normalizedExtras = extras ?? "";
		string normalizedPlayerRecent = playerRecent.Trim();
		if (normalizedExtras.IndexOf(normalizedPlayerRecent, StringComparison.Ordinal) >= 0)
		{
			return normalizedExtras;
		}
		return string.IsNullOrWhiteSpace(normalizedExtras) ? normalizedPlayerRecent : (normalizedExtras.TrimEnd() + Environment.NewLine + normalizedPlayerRecent);
	}

	private static bool ContainsKingdomAnnexActionTag(string text)
	{
		return (text ?? "").IndexOf("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool ContainsVassalageActionTag(string text)
	{
		return (text ?? "").IndexOf("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static List<string> MergeCourierSelectedRuleIds(params IEnumerable<string>[] sources)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (IEnumerable<string> source in sources ?? new IEnumerable<string>[0])
			{
				foreach (string raw in source ?? Enumerable.Empty<string>())
				{
					string value = (raw ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
					{
						result.Add(value);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<string> ExcludeCourierSelectedRuleIds(List<string> source, IEnumerable<string> excludedRuleIds)
	{
		if (source == null || source.Count == 0)
		{
			return new List<string>();
		}
		HashSet<string> excluded = new HashSet<string>((excludedRuleIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
		if (excluded.Count == 0)
		{
			return source;
		}
		return source.Where(x => !string.IsNullOrWhiteSpace(x) && !excluded.Contains(x.Trim())).ToList();
	}

	private static string RemoveInjectedRuleBlock(string text, string ruleId)
	{
		string value = text ?? "";
		string id = Regex.Escape((ruleId ?? "").Trim());
		if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		string pattern = "(?ms)^【附加规则:" + id + "】.*?(?=^【附加规则:|\\z)";
		return Regex.Replace(value, pattern, "").Trim();
	}

	private static string StripCourierActionTags(string text)
	{
		string value = text ?? "";
		value = Regex.Replace(value, "\\[ACTION:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[AD:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ADP:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ATT:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[ATP:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:H_J_P_P_(?:C&L|[CL])\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:C_J_P_K\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:C_J_K:[^\\]]+\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:P_J_K_[MV]\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[A:P_L_K\\]", "", RegexOptions.IgnoreCase);
		value = Regex.Replace(value, "\\[(?:FOL|STP|END)\\]", "", RegexOptions.IgnoreCase);
		return value.Trim();
	}

	private static RewardSystemBehavior.RpItemIntroductionContext CreateCourierRpItemIntroductionContext(
		CourierSession session,
		Hero recipient,
		string fallbackReplyText)
	{
		try
		{
			string replyText = session?.ReplyText;
			if (string.IsNullOrWhiteSpace(replyText))
			{
				replyText = fallbackReplyText;
			}
			return RewardSystemBehavior.CreateRpItemIntroductionContextForExternal(
				recipient,
				null,
				recipient?.Name?.ToString(),
				session?.LetterText,
				StripCourierActionTags(replyText));
		}
		catch
		{
			// Context enrichment must never stop the existing reward-tag processing.
			return null;
		}
	}

	private static bool MayContainGeneratedRpItemReward(string responseText)
	{
		return !string.IsNullOrEmpty(responseText)
			&& responseText.IndexOf(GiveAssetTagCodec.Prefix, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string BuildCourierStatusSnapshot(CourierSession session, MobileParty courier, Hero recipient, string phase)
	{
		try
		{
			return "phase=" + (phase ?? "") +
				" session=" + (session?.Id ?? "") +
				" stage=" + (session?.Stage ?? "") +
				" deliveryApplied=" + (session?.DeliveryApplied == true) +
				" replyStarted=" + (session?.ReplyGenerationStarted == true) +
				" replyGenerated=" + (session?.ReplyGenerated == true) +
				" waitReason=" + (session?.RecipientWaitReason ?? "") +
				" safeSettlement=" + (session?.SafeSettlementId ?? "") +
				" lastRoute=" + (session?.LastRouteKey ?? "") +
				" progressRoute=" + (session?.LastProgressRouteKey ?? "") +
				" stuckCount=" + (session?.NavalStuckRefreshCount ?? 0) +
				" tempShipCreated=" + (session?.TemporaryShipCreated == true) +
				" tempShipHull=" + (session?.TemporaryShipHullId ?? "") +
				" " + DescribeHero(recipient) +
				" courier=" + DescribeMobileParty(courier);
		}
		catch (Exception ex)
		{
			return "phase=" + (phase ?? "") + " session=" + (session?.Id ?? "") + " snapshot_error=" + ex.Message;
		}
	}

	private static string DescribeRecipientTarget(Hero recipient, MobileParty targetParty, Settlement targetSettlement)
	{
		return "source=" + GetRecipientTargetSource(recipient, targetParty, targetSettlement) +
			" " + DescribeHero(recipient) +
			" targetParty=" + DescribeMobileParty(targetParty) +
			" targetSettlement=" + DescribeSettlement(targetSettlement);
	}

	private static string GetRecipientTargetSource(Hero recipient, MobileParty targetParty, Settlement targetSettlement)
	{
		try
		{
			if (recipient == null)
			{
				return "none";
			}
			if (targetParty != null && recipient.PartyBelongedTo == targetParty)
			{
				return "party_belonged_to";
			}
			PartyBase prisonerParty = recipient.PartyBelongedToAsPrisoner;
			if (prisonerParty != null)
			{
				if (targetParty != null && prisonerParty.MobileParty == targetParty)
				{
					return "prisoner_mobile_party";
				}
				if (targetSettlement != null && prisonerParty.Settlement == targetSettlement)
				{
					return "prisoner_settlement";
				}
			}
			if (targetSettlement != null && recipient.CurrentSettlement == targetSettlement)
			{
				return "current_settlement";
			}
			if (targetSettlement != null && recipient.StayingInSettlement == targetSettlement)
			{
				return "staying_settlement";
			}
		}
		catch
		{
		}
		return "resolved_fallback";
	}

	private static string DescribeRoutePlan(CourierRoutePlan plan)
	{
		if (plan == null)
		{
			return "plan=null";
		}
		return "planNav=" + plan.NavigationType +
			" requiresNaval=" + plan.RequiresNaval +
			" usePort=" + plan.UsePort +
			" reason=" + (plan.Reason ?? "");
	}

	private static string DescribeHero(Hero hero)
	{
		if (hero == null)
		{
			return "recipient=null";
		}
		try
		{
			return "recipient=" + SafeHeroId(hero) +
				" name=" + SafeLogText(hero.Name?.ToString()) +
				" dead=" + hero.IsDead +
				" fugitive=" + hero.IsFugitive +
				" prisoner=" + hero.IsPrisoner +
				" party=" + PartyIdOnly(hero.PartyBelongedTo) +
				" prisonerParty=" + DescribePartyBase(hero.PartyBelongedToAsPrisoner) +
				" currentSettlement=" + SettlementIdOnly(hero.CurrentSettlement) +
				" stayingSettlement=" + SettlementIdOnly(hero.StayingInSettlement);
		}
		catch (Exception ex)
		{
			return "recipient=" + SafeHeroId(hero) + " describe_error=" + ex.Message;
		}
	}

	private static string DescribeMobileParty(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return (party.StringId ?? "") +
				"(name=" + SafeLogText(party.Name?.ToString()) +
				",active=" + party.IsActive +
				",pos=" + FormatCampaignVec2(party.Position) +
				",posLand=" + SafeIsOnLand(party.Position) +
				",sea=" + party.IsCurrentlyAtSea +
				",current=" + SettlementIdOnly(party.CurrentSettlement) +
				",default=" + party.DefaultBehavior +
				",short=" + party.ShortTermBehavior +
				",nav=" + party.NavigationCapability +
				",desiredNav=" + party.DesiredAiNavigationType +
				",isTargetingPort=" + party.IsTargetingPort +
				",targetSettlement=" + SettlementIdOnly(party.TargetSettlement) +
				",targetParty=" + PartyIdOnly(party.TargetParty) +
				",shortTargetSettlement=" + SettlementIdOnly(party.ShortTermTargetSettlement) +
				",shortTargetParty=" + PartyIdOnly(party.ShortTermTargetParty) +
				",targetPos=" + FormatCampaignVec2(party.TargetPosition) +
				",moveTarget=" + FormatCampaignVec2(party.MoveTargetPoint) +
				",ships=" + SafeShipCount(party) +
				",landCap=" + SafeHasLandNavigation(party) +
				",navalCap=" + SafeHasNavalNavigation(party) +
				",transition=" + party.IsTransitionInProgress +
				",quest=" + party.IsCurrentlyUsedByAQuest +
				")";
		}
		catch (Exception ex)
		{
			return (party.StringId ?? "") + "(describe_error=" + ex.Message + ")";
		}
	}

	private static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		try
		{
			return (settlement.StringId ?? "") +
				"(name=" + SafeLogText(settlement.Name?.ToString()) +
				",town=" + settlement.IsTown +
				",castle=" + settlement.IsCastle +
				",village=" + settlement.IsVillage +
				",fort=" + settlement.IsFortification +
				",hasPort=" + settlement.HasPort +
				",underSiege=" + settlement.IsUnderSiege +
				",gate=" + FormatCampaignVec2(settlement.GatePosition) +
				",port=" + (settlement.HasPort ? FormatCampaignVec2(settlement.PortPosition) : "none") +
				")";
		}
		catch (Exception ex)
		{
			return (settlement.StringId ?? "") + "(describe_error=" + ex.Message + ")";
		}
	}

	private static string DescribePartyBase(PartyBase party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return "(name=" + SafeLogText(party.Name?.ToString()) +
				",mobile=" + PartyIdOnly(party.MobileParty) +
				",settlement=" + SettlementIdOnly(party.Settlement) +
				",isMobile=" + party.IsMobile +
				",isSettlement=" + party.IsSettlement +
				")";
		}
		catch (Exception ex)
		{
			return "(describe_error=" + ex.Message + ")";
		}
	}

	private static string PartyIdOnly(MobileParty party)
	{
		return party == null ? "null" : (party.StringId ?? "");
	}

	private static string SettlementIdOnly(Settlement settlement)
	{
		return settlement == null ? "null" : (settlement.StringId ?? "");
	}

	private static string FormatCampaignVec2(CampaignVec2 position)
	{
		try
		{
			if (!position.IsValid())
			{
				return "invalid";
			}
		}
		catch
		{
			return "invalid";
		}
		return position.X.ToString("0.##") + "," + position.Y.ToString("0.##");
	}

	private static string SafeIsOnLand(CampaignVec2 position)
	{
		try
		{
			return position.IsOnLand.ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static int SafeShipCount(MobileParty party)
	{
		try
		{
			return party?.Ships?.Count ?? 0;
		}
		catch
		{
			return -1;
		}
	}

	private static string SafeHasLandNavigation(MobileParty party)
	{
		try
		{
			return (party?.HasLandNavigationCapability == true).ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeHasNavalNavigation(MobileParty party)
	{
		try
		{
			return (party?.HasNavalNavigationCapability == true).ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeLogText(string text)
	{
		string value = (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
		if (value.Length > 80)
		{
			value = value.Substring(0, 80);
		}
		return value;
	}

	private static void LogCourierStatus(string message)
	{
		try
		{
			Logger.Log("Logic", "[CourierStatus] " + (message ?? ""));
		}
		catch
		{
		}
	}

	private static void LogCourierStatusVerbose(string key, string message, double minIntervalSeconds = 0.0)
	{
		try
		{
			if (!Logger.IsModLogicEnabled)
			{
				return;
			}
			long now = DateTime.UtcNow.Ticks;
			string throttleKey = (key ?? "").Trim();
			long minTicks = TimeSpan.FromSeconds(Math.Max(0.0, minIntervalSeconds)).Ticks;
			lock (LastCourierLogicPulseTicks)
			{
				if (!string.IsNullOrWhiteSpace(throttleKey) && minTicks > 0L && LastCourierLogicPulseTicks.TryGetValue(throttleKey, out long last) && now - last < minTicks)
				{
					return;
				}
				if (LastCourierLogicPulseTicks.Count > 512)
				{
					LastCourierLogicPulseTicks.Clear();
				}
				if (!string.IsNullOrWhiteSpace(throttleKey))
				{
					LastCourierLogicPulseTicks[throttleKey] = now;
				}
			}
			LogCourierStatus(message);
		}
		catch
		{
		}
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log(LogSource, message ?? "");
		}
		catch
		{
		}
	}

	private static void LogVerbose(string key, string message, double minIntervalSeconds = 0.0)
	{
		try
		{
			Logger.LogVerbose(LogSource, key, () => message ?? "", minIntervalSeconds);
		}
		catch
		{
		}
	}

	private static int GetRuntimeHash(object value)
	{
		return value == null ? 0 : RuntimeHelpers.GetHashCode(value);
	}

	private static void MarkCourierMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return;
		}
		try
		{
			CourierMapEventMarkers.GetValue(mapEvent, _ => new CourierMapEventMarker());
		}
		catch
		{
		}
	}

	private static bool IsKnownCourierMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			return CourierMapEventMarkers.TryGetValue(mapEvent, out _);
		}
		catch
		{
			return false;
		}
	}

	private static void MarkCourierCurrentMapEvent(MobileParty courier)
	{
		try
		{
			MapEvent mapEvent = courier?.MapEvent;
			if (mapEvent != null)
			{
				MarkCourierMapEvent(mapEvent);
			}
		}
		catch
		{
		}
	}

	private static bool IsCourierPartyOrId(MobileParty party)
	{
		try
		{
			return party != null && (IsCourierParty(party) || IsCourierPartyId(party.StringId));
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetCourierFromPartyBase(PartyBase party, out MobileParty courier)
	{
		courier = null;
		try
		{
			MobileParty mobileParty = party?.MobileParty;
			if (mobileParty == null || !IsCourierPartyOrId(mobileParty))
			{
				return false;
			}
			courier = mobileParty;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetCourierFromMapEventSide(MapEventSide side, out MobileParty courier)
	{
		courier = null;
		try
		{
			if (TryGetCourierFromPartyBase(side?.LeaderParty, out courier))
			{
				return true;
			}
			if (side?.Parties == null)
			{
				return false;
			}
			foreach (MapEventParty eventParty in side.Parties)
			{
				if (TryGetCourierFromPartyBase(eventParty?.Party, out courier))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryFindCourierInMapEvent(MapEvent mapEvent, out MobileParty courier, out CourierSession session)
	{
		courier = null;
		session = null;
		try
		{
			if (mapEvent == null)
			{
				return false;
			}
			if (TryGetCourierFromMapEventSide(mapEvent.AttackerSide, out courier) || TryGetCourierFromMapEventSide(mapEvent.DefenderSide, out courier))
			{
				session = TryFindCourierSessionByPartyId(courier?.StringId, includeTerminal: true);
				MarkCourierMapEvent(mapEvent);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static IEnumerable<MapEventSide> GetSafeMapEventSides(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			yield break;
		}
		MapEventSide attacker = null;
		MapEventSide defender = null;
		try
		{
			attacker = mapEvent.AttackerSide;
			defender = mapEvent.DefenderSide;
		}
		catch
		{
		}
		if (attacker != null)
		{
			yield return attacker;
		}
		if (defender != null && !ReferenceEquals(defender, attacker))
		{
			yield return defender;
		}
	}

	private static IEnumerable<MobileParty> CollectCourierParties(MapEvent mapEvent)
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (MapEventSide side in GetSafeMapEventSides(mapEvent))
		{
			if (TryGetCourierFromPartyBase(side?.LeaderParty, out MobileParty leaderCourier))
			{
				string id = (leaderCourier.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(id) || seen.Add(id))
				{
					yield return leaderCourier;
				}
			}
			if (side?.Parties == null)
			{
				continue;
			}
			foreach (MapEventParty eventParty in side.Parties)
			{
				if (!TryGetCourierFromPartyBase(eventParty?.Party, out MobileParty partyCourier))
				{
					continue;
				}
				string id = (partyCourier.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(id) || seen.Add(id))
				{
					yield return partyCourier;
				}
			}
		}
	}

	private static PartyBase SafeLeaderParty(MapEventSide side)
	{
		try
		{
			return side?.LeaderParty;
		}
		catch
		{
			return null;
		}
	}

	private static string SafePartyBaseLogId(PartyBase party)
	{
		try
		{
			if (party?.MobileParty != null)
			{
				return party.MobileParty.StringId ?? party.MobileParty.Name?.ToString() ?? "mobile_party";
			}
			if (party?.Settlement != null)
			{
				return party.Settlement.StringId ?? party.Settlement.Name?.ToString() ?? "settlement";
			}
			return party?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "unknown";
		}
	}

	private static PartyBase FindSidePartyWithFaction(MapEventSide side)
	{
		try
		{
			PartyBase leader = SafeLeaderParty(side);
			if (SafeMapFaction(leader) != null)
			{
				return leader;
			}
			if (side?.Parties == null)
			{
				return null;
			}
			foreach (MapEventParty eventParty in side.Parties)
			{
				PartyBase party = eventParty?.Party;
				if (party != null && SafeMapFaction(party) != null)
				{
					return party;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool TrySetMapEventSideLeaderParty(MapEventSide side, PartyBase party, string reason)
	{
		try
		{
			if (side == null || party == null || MapEventSideLeaderPartyProperty == null)
			{
				return false;
			}
			MapEventSideLeaderPartyProperty.SetValue(side, party, null);
			IFaction faction = SafeMapFaction(party);
			if (faction != null && MapEventSideMapFactionField != null)
			{
				MapEventSideMapFactionField.SetValue(side, faction);
			}
			LogCourierStatusVerbose("map_event_side_leader_repair:" + GetRuntimeHash(side), "map_event_side_leader_repair reason=" + (reason ?? "") +
				" leader=" + SafePartyBaseLogId(party) +
				" faction=" + (faction?.StringId ?? "null"), 10.0);
			return SafeLeaderParty(side) == party;
		}
		catch (Exception ex)
		{
			Log("map event side leader repair failed reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static void RepairMapEventSideLeaderFaction(MapEventSide side, string reason)
	{
		try
		{
			PartyBase leader = SafeLeaderParty(side);
			if (leader != null && SafeMapFaction(leader) != null)
			{
				return;
			}
			PartyBase replacement = FindSidePartyWithFaction(side);
			if (replacement == null)
			{
				return;
			}
			if (!ReferenceEquals(leader, replacement))
			{
				TrySetMapEventSideLeaderParty(side, replacement, reason);
				return;
			}
			if (MapEventSideMapFactionField != null)
			{
				MapEventSideMapFactionField.SetValue(side, SafeMapFaction(replacement));
			}
		}
		catch (Exception ex)
		{
			Log("map event side faction repair failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void PrepareCourierMapEventForNativeUpdate(MapEvent mapEvent, MobileParty knownCourier, CourierSession session, string reason)
	{
		try
		{
			if (mapEvent == null)
			{
				return;
			}
			MarkCourierMapEvent(mapEvent);
			if (knownCourier != null)
			{
				EnsureCourierCampaignIdentity(knownCourier, session ?? TryFindCourierSessionByPartyId(knownCourier.StringId, includeTerminal: true), reason);
			}
			foreach (MobileParty courier in CollectCourierParties(mapEvent))
			{
				EnsureCourierCampaignIdentity(courier, TryFindCourierSessionByPartyId(courier.StringId, includeTerminal: true), reason);
			}
			foreach (MapEventSide side in GetSafeMapEventSides(mapEvent))
			{
				RepairMapEventSideLeaderFaction(side, reason);
			}
		}
		catch (Exception ex)
		{
			Log("courier map event prepare failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool TryValidateMapEventLeaderFactions(MapEvent mapEvent, out string reason)
	{
		reason = "";
		try
		{
			PartyBase attacker = SafeLeaderParty(mapEvent?.AttackerSide);
			PartyBase defender = SafeLeaderParty(mapEvent?.DefenderSide);
			if (attacker == null || defender == null)
			{
				reason = "leader_party_null attacker=" + (attacker == null) + " defender=" + (defender == null);
				return false;
			}
			IFaction attackerFaction = SafeMapFaction(attacker);
			IFaction defenderFaction = SafeMapFaction(defender);
			if (attackerFaction == null || defenderFaction == null)
			{
				reason = "leader_faction_null attacker=" + (attackerFaction == null) + " defender=" + (defenderFaction == null);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "validate_exception:" + ex.Message;
			return false;
		}
	}

	private static void SetCourierMapEventFinishCalled(MapEvent mapEvent)
	{
		try
		{
			MapEventFinishCalledField?.SetValue(mapEvent, true);
		}
		catch
		{
		}
	}

	private static bool TryFinalizeCourierMapEvent(MapEvent mapEvent, MobileParty courier, CourierSession session, string reason, Exception sourceException)
	{
		try
		{
			if (mapEvent == null)
			{
				return false;
			}
			MarkCourierMapEvent(mapEvent);
			PrepareCourierMapEventForNativeUpdate(mapEvent, courier, session, reason);
			SetCourierMapEventFinishCalled(mapEvent);
			mapEvent.DiplomaticallyFinished = true;
			if (!mapEvent.IsFinalized)
			{
				mapEvent.FinalizeEvent();
			}
			Log("courier map event finalized reason=" + (reason ?? "") +
				" courier=" + (courier?.StringId ?? "null") +
				" exception=" + (sourceException == null ? "none" : sourceException.GetType().Name + ":" + sourceException.Message));
			return true;
		}
		catch (Exception ex)
		{
			Log("courier map event finalize failed reason=" + (reason ?? "") + " error=" + ex);
			try
			{
				if (mapEvent != null && !mapEvent.IsFinalized)
				{
					mapEvent.ResetBattleState();
					SetCourierMapEventFinishCalled(mapEvent);
					mapEvent.DiplomaticallyFinished = true;
					mapEvent.FinalizeEvent();
					Log("courier map event finalized after reset reason=" + (reason ?? ""));
					return true;
				}
			}
			catch (Exception retryEx)
			{
				Log("courier map event reset finalize failed reason=" + (reason ?? "") + " error=" + retryEx);
			}
			return false;
		}
	}

	public static bool CourierMapEventUpdatePrefix(MapEvent __instance)
	{
		try
		{
			if (__instance == null)
			{
				return true;
			}
			bool hasCourier = TryFindCourierInMapEvent(__instance, out MobileParty courier, out CourierSession session);
			bool knownCourierMapEvent = hasCourier || IsKnownCourierMapEvent(__instance);
			if (!knownCourierMapEvent)
			{
				return true;
			}
			if (__instance.IsFinalized)
			{
				SetCourierMapEventFinishCalled(__instance);
				return false;
			}
			if (hasCourier && session != null && IsInboundToPlayer(session))
			{
				return !TryFinalizeCourierMapEvent(__instance, courier, session, "npc_courier_neutrality_guard", null);
			}
			PrepareCourierMapEventForNativeUpdate(__instance, courier, session, "update_prefix");
			if (!TryValidateMapEventLeaderFactions(__instance, out string invalidReason))
			{
				return !TryFinalizeCourierMapEvent(__instance, courier, session, "update_prefix_invalid:" + invalidReason, null);
			}
		}
		catch (Exception ex)
		{
			Log("courier map event prefix failed: " + ex);
		}
		return true;
	}

	public static Exception CourierMapEventUpdateFinalizer(MapEvent __instance, Exception __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (!(__exception is NullReferenceException))
			{
				return __exception;
			}
			bool hasCourier = TryFindCourierInMapEvent(__instance, out MobileParty courier, out CourierSession session);
			if (!hasCourier && !IsKnownCourierMapEvent(__instance))
			{
				return __exception;
			}
			if (TryFinalizeCourierMapEvent(__instance, courier, session, "update_finalizer_null_reference", __exception))
			{
				return null;
			}
		}
		catch (Exception ex)
		{
			Log("courier map event finalizer failed: " + ex);
		}
		return __exception;
	}

	private static class MethodInfoAccess
	{
		public static void PatchDefaultEncounterModel(Harmony harmony)
		{
			var method = AccessTools.Method(typeof(DefaultEncounterModel), nameof(DefaultEncounterModel.IsEncounterExemptFromHostileActions));
			if (method != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(DefaultEncounterModelIsEncounterExemptPrefix)));
			}
		}

		public static void PatchCustomPartyComponentBanner(Harmony harmony)
		{
			var method = AccessTools.Method(typeof(CustomPartyComponent), nameof(CustomPartyComponent.GetDefaultComponentBanner));
			if (method != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(CustomPartyComponentGetDefaultComponentBannerPrefix)));
			}
		}

		public static void PatchCourierMapEventSafety(Harmony harmony)
		{
			var method = AccessTools.Method(typeof(MapEvent), "Update");
			if (method != null)
			{
				harmony.Patch(
					method,
					prefix: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(CourierMapEventUpdatePrefix)),
					finalizer: new HarmonyMethod(typeof(CourierDeliveryBehavior), nameof(CourierMapEventUpdateFinalizer)));
			}
		}
	}

	public static bool CustomPartyComponentGetDefaultComponentBannerPrefix(CustomPartyComponent __instance, ref Banner __result)
	{
		try
		{
			MobileParty party = __instance?.MobileParty;
			if (!IsCourierParty(party))
			{
				return true;
			}
			CourierSession session = TryFindCourierSessionByPartyId(party?.StringId);
			Hero owner = SafePartyOwner(party?.Party);
			Clan clan = ResolveCourierIdentityClan(party, owner, session);
			__result = clan?.Banner ?? party?.ActualClan?.Banner ?? Clan.PlayerClan?.Banner ?? Hero.MainHero?.Clan?.Banner;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool DefaultEncounterModelIsEncounterExemptPrefix(PartyBase side1, PartyBase side2, ref bool __result)
	{
		try
		{
			MobileParty courier = side1?.MobileParty != null && IsCourierParty(side1.MobileParty) ? side1.MobileParty : (side2?.MobileParty != null && IsCourierParty(side2.MobileParty) ? side2.MobileParty : null);
			if (courier == null)
			{
				return true;
			}
			if (IsNpcIssuedCourierParty(courier))
			{
				__result = true;
				return false;
			}
			PartyBase other = courier == side1?.MobileParty ? side2 : side1;
			IFaction otherFaction = other?.MapFaction;
			bool otherIsBandit = IsBanditOrOutlawParty(other?.MobileParty) || otherFaction?.IsBanditFaction == true;
			if (otherIsBandit)
			{
				__result = false;
				return false;
			}
			__result = true;
			return false;
		}
		catch
		{
			return true;
		}
	}
}
