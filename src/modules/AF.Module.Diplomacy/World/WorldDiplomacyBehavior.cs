using AnimusForge;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
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
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior : CampaignBehaviorBase
{
	private const string Source = "WorldDiplomacy";
	private const int DaysPerYear = 84;
	private const int DefaultApiTimeoutMilliseconds = 90000;
	private const int GenerationMaxTokens = 1800;
	private const int MaxConsecutiveTechnicalGenerationFailuresPerRound = 3;
	private const int MaxDiplomaticActionsPerDocument = 4;
	private const int CompressionOutputTokenReserve = 1024;
	private const int CompressionJobPriority = 1000;
	private const int MaxStoredDocuments = 420;
	private const int MaxStoredAnnualSummaries = 24;
	private const int MaxStoredCompressionSummaries = 24;
	private const int MaxStoredRoundSummaries = 96;
	private const int CompressionRetryInitialHours = 1;
	private const int CompressionRetryMaximumHours = 24;
	private const int MaxPendingJobs = 24;
	private const int FixedMaxConcurrentOffensiveWars = 2;
	private const int FailedServiceCooldownHours = 12;
	private const float CessionCastleUnlockThreshold = 90f;
	private const float CessionTownUnlockThreshold = 95f;
	private const int MaxPeaceCessionCandidates = 5;
	private const int RecentBattleRetentionDays = 21;
	private const int MaxPropagationArrivalsPerDay = 1200;
	private const int MaxAiDocumentsStartedPerDay = 8;
	private const int MaxDiplomacyLlmRequestsPerDay = 12;
	private const int MaxAutomaticDocumentsPerRound = 12;
	private const int MaxAutomaticReplyDepth = 2;
	private const int MaxPriorityPlayerResponsesPerDocument = 3;
	private const int RoundInactivityDays = 7;
	private const int MaxPendingPolicySignals = 24;
	private const int MaxProcessedPolicySignalKeys = 256;
	private const int PolicyHistorySyncBatchSize = 256;
	private const int PolicyHistoryForceSyncMaxBatches = 40;
	private const int PolicySignalRetentionDays = 21;
	private const int ResultSettlementStateSchemaVersion = 1;
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
	private const int DiplomacyPromptContractVersion = 29;
	private const int RelaySchemaVersion = 23;
	private const int RelayPassDurationDays = 7;
	private const int RelayTargetDurationDays = 21;
	private const int RelayHardDurationDays = 24;
	private const int MaxRelayParticipants = 12;
	private const int RecentNegativeReputationFactRetentionDays = DaysPerYear;

	private static bool _patchesApplied;
	private static int _internalDiplomaticActionDepth;
	private readonly BannerlordWorldDiplomacyPersistenceAdapter PersistenceAdapter =
		new BannerlordWorldDiplomacyPersistenceAdapter();

	private readonly ConcurrentQueue<LlmJobResult> _completedJobs = new ConcurrentQueue<LlmJobResult>();
	private readonly WorldDiplomacyNotificationApplication _notifications = new WorldDiplomacyNotificationApplication();
	private NotificationWorld _notificationSink;
	private NotificationWorld NotificationSink => _notificationSink ?? (_notificationSink = new NotificationWorld(this));
	private readonly Dictionary<string, WarSituationSnapshot> _warSituationCache = new Dictionary<string, WarSituationSnapshot>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _courtSettlementCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _realmInstitutionalVoiceCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, WorldDiplomacyRealmRelationProfile> _realmRelationProfileCache = new Dictionary<string, WorldDiplomacyRealmRelationProfile>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, WorldDiplomacyBorderRelation> _kingdomBorderCache = new Dictionary<string, WorldDiplomacyBorderRelation>(StringComparer.OrdinalIgnoreCase);
	private WorldDiplomacyRequestLeaseCoordinator _llmRequestLease => _orchestration.RequestLeases;
	private int _kingdomBorderCacheDay = -1;
	private float _kingdomBorderDistanceThreshold = WorldDiplomacyWorldProfileRules.MinimumBorderDistance;
	private long _realmInstitutionalVoiceRuleVersion = -1L;

	// Canonical state is owned by the application-side store inside the
	// orchestration; this alias only projects the current snapshot for leaf
	// reads. No site may assign fields on it or replace it outside the
	// orchestration persistence lanes.
	private WorldDiplomacyStorage _storage => _orchestration.CurrentStorage;
	private MapNotificationView _registeredMapNotificationView;
	private long _runtimeGeneration;
    private bool _persistenceWarningShown;
    internal static string PersistenceUnavailableMessage => DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_PersistenceUnavailable", "Diplomacy save data is inconsistent. Diplomacy is paused and the original data kept. Check the WorldDiplomacy log under Logs/Mod_Logic; the current diplomatic operation was refused.");
    internal string UnavailableReason => !ReferenceEquals(Instance, this) || !CampaignRuntimeGuard.IsLiveCampaign(_owningCampaign)
        ? DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_SessionEnded", "The session this diplomatic operation belongs to has ended. Please reopen it.")
        : PersistenceAdapter.HasCompleteRejectedEvidence ? PersistenceUnavailableMessage
        : DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_PersistenceUnreadable", "Diplomacy save data could not be read. Diplomacy is paused to protect the original save. Do not overwrite the original save; check the WorldDiplomacy log under Logs/Mod_Logic.");
	// Runtime-only revision lets the world-message timeline detect a published document without cloning the archive every tick.
	private long _worldMessageTimelineRevision = 1L;


	private long _cacheHitTokensThisSession;
	private long _cacheMissTokensThisSession;
	private long _relayCacheHitTokensThisSession;
	private long _relayCacheMissTokensThisSession;
	private readonly WorldDiplomacyRuntimeState _runtime = new WorldDiplomacyRuntimeState();
	private readonly WorldDiplomacyOrchestration _orchestration;

	public static WorldDiplomacyBehavior Instance { get; private set; }
	private readonly Campaign _owningCampaign = Campaign.Current;
	internal bool IsPersistenceAvailable => PersistenceAdapter.IsHealthy && _orchestration.IsPersistenceHealthy;
	internal bool IsLiveCampaign => IsPersistenceAvailable && ReferenceEquals(Instance, this) && CampaignRuntimeGuard.IsLiveCampaign(_owningCampaign);
	internal WorldDiplomacyOrchestration Orchestration => _orchestration;

	public WorldDiplomacyBehavior()
	{
		Instance = this;
		_orchestration = new WorldDiplomacyOrchestration(new OrchestrationHost(this), _runtime);
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
		string loadError = null;
		_orchestration.SyncData(dataStore.IsSaving, dataStore.IsLoading,
			() => PersistenceAdapter.Load(dataStore, out loadError),
			() => loadError,
			storage => PersistenceAdapter.Save(dataStore, storage),
			Log,
			() => ResetTransientRuntime("load"));
	}

	public void OnEngineTick()
	{
		if (!IsLiveCampaign) return;
		var source = new TickSource(this);
		WorldDiplomacyTickApplication.Run(ref source, _orchestration);
	}

	internal void RetireCampaignRuntime(string reason)
	{
		_runtimeGeneration = 0;
		_orchestration.RequestLeases.Reset();
		while (_completedJobs.TryDequeue(out _)) { }
		if (ReferenceEquals(Instance, this)) Instance = null;
		WorldDiplomacyComposePopup.CloseForCampaignEnd();
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
		return WorldDiplomacyPresentation.OpenComposeFromTerminal(onClose);
	}

	public static bool ShowRoyalAnnouncementArchive(Action onClose = null)
	{
		return WorldDiplomacyPresentation.ShowRoyalAnnouncementArchive(onClose);
	}

	public static void NotifyExternalDiplomacyResolved(string action, Kingdom initiator, Kingdom target, string reason = null)
	{
		try
		{
			FeatureBridgeDecision bridgeDecision = FeatureBridgeRuntime.Evaluate(
				FeatureBridgeIds.PolicyWorldDiplomacy,
				FeatureBridgeIds.ContractVersion);
			if (!bridgeDecision.IsAllowed)
			{
				Log("external diplomacy notification skipped: " + bridgeDecision.ReasonCode);
				return;
			}
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
			return TryGetRecentDocumentsForTimelineQuery(maxCount, out List<WorldDiplomacyDocument> documents)
				? documents
				: new List<WorldDiplomacyDocument>();
		}
		catch
		{
			return new List<WorldDiplomacyDocument>();
		}
	}

	internal static bool TryGetRecentDocumentsForTimelineQuery(
		int maxCount,
		out List<WorldDiplomacyDocument> documents)
	{
		documents = null;
		WorldDiplomacyBehavior behavior = ResolveInstance();
		if (behavior == null)
		{
			return false;
		}

		documents = WorldDiplomacyRoundLifecycleRules.SelectRecentDocumentsForTimelineQuery(
			behavior._storage.Documents, maxCount, MaxStoredDocuments);
		return true;
	}

    internal static bool TryGetTimelineState(out WorldDiplomacyStorage storage)
    {
        WorldDiplomacyBehavior owner = ResolveInstance();
        storage = owner?._storage;
        return owner != null;
    }

	public static long GetWorldMessageTimelineRevisionForExternal()
	{
		return WorldDiplomacyTimelineQueryHost.GetRevisionOrZero();
	}

	internal static bool TryGetTimelineRevisionSnapshot(out long revision)
	{
		WorldDiplomacyBehavior behavior = ResolveInstance();
		revision = behavior?._worldMessageTimelineRevision ?? 0L;
		return behavior != null;
	}

	public static string BuildKingdomDiplomaticStandingEncyclopediaTextForExternal(Kingdom kingdom)
	{
		return WorldDiplomacyPresentationHost.Standing(kingdom?.StringId);
	}

	public static string BuildDiplomaticStandingImpactTextForExternal(WorldDiplomacyDocument document)
	{
		return WorldDiplomacyPresentationQueries.BuildImpactText(document);
	}

	public static bool CanDiscussWorldDiplomacyForExternal(Hero hero) =>
		DiplomacyModuleServices.World.CanDiscuss(hero?.StringId);

	internal static bool TryCaptureDiscussionCandidate(
		Hero hero, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId)
	{
		candidate = default;
		kingdomId = "";
		if (ResolveInstance() == null) return false;
		Clan clan = hero?.Clan;
		Kingdom kingdom = clan?.Kingdom;
		kingdomId = kingdom?.StringId ?? "";
		candidate = new WorldDiplomacyDiscussionCandidate(
			heroExists: hero != null,
			kingdomExists: kingdom != null,
			kingdomIsEliminated: kingdom?.IsEliminated == true,
			isLord: hero?.IsLord == true,
			isRulingLeader: hero != null && hero == kingdom?.RulingClan?.Leader);
		return true;
	}

	internal static bool HasKnownDocumentForDiscussion(Hero hero, string kingdomId) =>
		ResolveInstance()?.GetKnownDocumentIdsForHero(hero, kingdomId).Count > 0;

	public static bool TryBuildProactiveDiscussionForExternal(Hero hero, out string stableKey, out string fact, out float urgency)
	{
		return DiplomacyModuleServices.World.TryBuildProactiveDiscussion(hero?.StringId,
			out stableKey, out fact, out urgency);
	}

	internal static bool TryCaptureProactiveSpeaker(Hero hero,
		out WorldDiplomacyProactiveSpeakerCandidate candidate, out string playerKingdomId)
	{
		candidate = default;
		playerKingdomId = "";
		if (ResolveInstance() == null) return false;
		Clan playerClan = Clan.PlayerClan;
		Kingdom playerKingdom = playerClan?.Kingdom;
		Clan clan = hero?.Clan;
		playerKingdomId = playerKingdom?.StringId ?? "";
		candidate = new WorldDiplomacyProactiveSpeakerCandidate(
			heroExists: hero != null,
			playerKingdomExists: playerKingdom != null,
			playerKingdomIsEliminated: playerKingdom?.IsEliminated == true,
			clanExists: clan != null,
			clanBelongsToPlayerKingdom: clan?.Kingdom == playerKingdom,
			isPlayerClan: clan == playerClan,
			isUnderMercenaryService: clan?.IsUnderMercenaryService == true,
			isClanTypeMercenary: clan?.IsClanTypeMercenary == true,
			isLord: hero?.IsLord == true);
		return true;
	}

	internal static bool TryCaptureProactiveDocuments(Hero hero, string playerKingdomId,
		out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay)
	{
		documents = null;
		knownIds = null;
		currentDay = 0;
		WorldDiplomacyBehavior owner = ResolveInstance();
		if (owner == null) return false;
		knownIds = owner.GetKnownDocumentIdsForHero(hero, playerKingdomId);
		currentDay = CurrentDay();
		documents = owner._storage?.Documents;
		return true;
	}

	internal static string GetPlayerKingdomNameForProactive() => KingdomName(Clan.PlayerClan?.Kingdom);
	internal static string FormatDateForProactive(int day) => FormatCampaignDate(day);

	public static bool MarkDocumentReadForExternal(string documentId)
	{
		return WorldDiplomacyTimelineQueryHost.MarkDocumentRead(documentId);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
        { PersistenceAdapter.ResetForNewGame(); _persistenceWarningShown = false; DiplomacyModuleServices.World.OnLifecycle(WorldDiplomacyLifecycleEvent.NewGame); }
	private void OnGameLoaded(CampaignGameStarter starter)
        { if (IsPersistenceAvailable) DiplomacyModuleServices.World.OnLifecycle(WorldDiplomacyLifecycleEvent.Loaded); }
	private void OnSessionLaunched(CampaignGameStarter starter)
    {
        if (IsPersistenceAvailable) { DiplomacyModuleServices.World.OnLifecycle(WorldDiplomacyLifecycleEvent.Session); return; }
        if (!_persistenceWarningShown)
        {
            _persistenceWarningShown = true;
            InformationManager.DisplayMessage(new InformationMessage(UnavailableReason));
        }
    }

	private void OnCampaignTick(float dt)
	{
		DiplomacyModuleServices.World.OnCampaignTick();
	}
	private void OnDailyTick()
	{
		DiplomacyModuleServices.World.OnDailyTick();
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
        if (!IsPersistenceAvailable) return;
		Kingdom first = faction1 as Kingdom;
		Kingdom second = faction2 as Kingdom;
		if (first == null || second == null || first == second)
		{
			return;
		}
		_orchestration.HandleWarDeclared(first.StringId, second.StringId);
		InvalidateWarSituation(first, second);
	}
	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
        if (!IsPersistenceAvailable) return;
		Kingdom first = faction1 as Kingdom;
		Kingdom second = faction2 as Kingdom;
		if (first == null || second == null)
		{
			return;
		}
		_orchestration.HandlePeaceMade(first.StringId, second.StringId);
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
        if (!IsPersistenceAvailable) return;
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
		_orchestration.HandleSettlementOwnerChanged(settlement.StringId, settlement.Name?.ToString(),
			oldKingdom.StringId, newKingdom.StringId);
		InvalidateWarSituation(oldKingdom, newKingdom);
	}
	private void OnMapEventEnded(MapEvent mapEvent)
	{
        if (!IsPersistenceAvailable) return;
		WorldDiplomacyBattleApplication.Record(new BattlePort(mapEvent), _orchestration);
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
		return WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((side?.Parties ?? Enumerable.Empty<MapEventParty>())
			.Select(x => x?.Party?.LeaderHero?.Name?.ToString()))
			.Take(6)
			.ToList();
	}
	private static string ResolveMapEventBattleType(MapEvent mapEvent)
	{
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			return DiplomacyCompatibilityTexts.Get("Battle_Siege");
		}
		if (mapEvent?.IsRaid == true)
		{
			return DiplomacyCompatibilityTexts.Get("Battle_Raid");
		}
		return DiplomacyCompatibilityTexts.Get("Battle_Field");
	}

	private void ResetTransientRuntime(string reason)
	{
		_runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		_llmRequestLease.Reset();
		while (_completedJobs.TryDequeue(out _))
		{
		}
		_notifications.ResetView();
		_registeredMapNotificationView = null;
		_warSituationCache.Clear();
		_realmInstitutionalVoiceCache.Clear();
		_realmRelationProfileCache.Clear();
		_kingdomBorderCache.Clear();
		_kingdomBorderCacheDay = -1;
		_realmInstitutionalVoiceRuleVersion = -1L;
		DiplomacyModuleServices.Policy.Clear();

		_cacheHitTokensThisSession = 0;
		_cacheMissTokensThisSession = 0;
		_relayCacheHitTokensThisSession = 0;
		_relayCacheMissTokensThisSession = 0;
		_notifications.Reset();
		_orchestration.ResetRuntimeState();
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

	private (int, int) GetDeclarationCharacterRange()
	{
		GetDiplomaticDeclarationCharacterRange(out int minimum, out int maximum);
		return (minimum, maximum);
	}

	private static void LogKingdomStrategicProfileInjection(WorldDiplomacyJob job, string profilePrompt)
	{
		Log("strategic profile injected job=" + (job?.JobId ?? "")
			+ " author=" + (job?.AuthorKingdomId ?? "")
			+ " relay=" + (job?.IsRelayTurn == true).ToString()
			+ " chars=" + (profilePrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
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
		sb.AppendLine(DiplomacyPromptTexts.Get("StrategicProfileVersion", "Profile version=") + WorldDiplomacyPromptContractRules.StablePromptHash((nationalPersonality ?? "") + "\n" + (longTermStrategy ?? "")));
		sb.AppendLine(DiplomacyPromptTexts.Get("StrategicProfilePersonality", "National personality=") + (nationalPersonality ?? ""));
		sb.AppendLine(DiplomacyPromptTexts.Get("StrategicProfileStrategy", "Long-term strategy=") + (longTermStrategy ?? ""));
		sb.Append(DiplomacyPromptTexts.Get("AF_DIP_Compatibility_008", "Find reasons for the long-term strategy, especially war, based on current circumstances; use real interests, disputes or security concerns as public reasons."));
		prompt = sb.ToString();
		return true;
	}

	private void LogPromptCacheShape(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(job, _orchestration.BuildCanonicalHistoryBlock);
		string system = messages.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string user = messages.LastOrDefault(x => x != null && string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string frozenContract = ResolveCommonContractForCacheDiagnostics(job, out string contractSource);
		int userPrefix1024Chars = Math.Min(1024, user.Length);
		int userPrefixChars = Math.Min(2048, user.Length);
		int totalChars = messages.Sum(x => x?.Content?.Length ?? 0);
		int expectedCachedMessageCount = WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job) && messages.Count >= 2
			? WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate") ? 1 : 2 : 0;
		int expectedCachedPrefixChars = messages.Take(expectedCachedMessageCount).Sum(x => x?.Content?.Length ?? 0);
		Log("cache-shape kind=" + (job?.Kind ?? "")
			+ " affinity=" + WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(job)
			+ " messages=" + messages.Count.ToString(CultureInfo.InvariantCulture)
			+ " totalChars=" + totalChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedMessages=" + expectedCachedMessageCount.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixChars=" + expectedCachedPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixHash=" + WorldDiplomacyPromptContractRules.StablePromptHashMessagePrefix(messages, expectedCachedMessageCount)
			+ " historyRevision=" + (job?.HistoryRevision ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " historyThroughSequence=" + (job?.HistoryThroughSequence ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " historyEstimatedTokens=" + (job?.HistoryEstimatedTokens ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " requestHistoryChars=" + (messages.Count > 2 ? messages[1].Content?.Length ?? 0 : 0).ToString(CultureInfo.InvariantCulture)
			+ " historyMode=" + (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate") ? "recent_window" : "archive_prefix")
			+ " inputLimit=" + WorldDiplomacyRequestHistoryApplication.InputLimit(job, GetHistoryCompressionTriggerTokens()).ToString(CultureInfo.InvariantCulture)
			+ " snapshotThroughSequence=" + (job?.HistorySnapshotThroughSequence ?? 0L).ToString(CultureInfo.InvariantCulture)
			+ " snapshotHash=" + (job?.HistorySnapshotHash ?? "")
			+ " stablePrefixHash=" + (job?.HistoryPrefixHash ?? "")
			+ " contractSource=" + contractSource
			+ " contractState=" + (frozenContract.Length == 0 ? "empty" : "present")
			+ " contractChars=" + frozenContract.Length.ToString(CultureInfo.InvariantCulture)
			+ " contractHash=" + WorldDiplomacyPromptContractRules.StablePromptHash(frozenContract)
			+ " contractAtTop=" + (frozenContract.Length == 0 ? "n/a_empty" : system.StartsWith(frozenContract, StringComparison.Ordinal).ToString())
			+ " systemChars=" + system.Length.ToString(CultureInfo.InvariantCulture)
			+ " systemHash=" + WorldDiplomacyPromptContractRules.StablePromptHash(system)
			+ " userChars=" + user.Length.ToString(CultureInfo.InvariantCulture)
			+ " userPrefix1024Hash=" + WorldDiplomacyPromptContractRules.StablePromptHash(userPrefix1024Chars <= 0 ? "" : user.Substring(0, userPrefix1024Chars))
			+ " userPrefixChars=" + userPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " userPrefixHash=" + WorldDiplomacyPromptContractRules.StablePromptHash(userPrefixChars <= 0 ? "" : user.Substring(0, userPrefixChars)));
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
			+ " affinity=" + WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(job)
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

	private (string kingdomId, string name) ResolveKingdomValidationIdentity(string kingdomId)
	{
		Kingdom kingdom = ResolveKingdom(kingdomId);
		return kingdom == null ? (null, null) : (kingdom.StringId, KingdomName(kingdom));
	}
	private static string ResolveSettlementValidationName(string settlementId)
	{
		return ResolveSettlementById(settlementId) is Settlement settlement ? settlement.Name?.ToString() ?? "" : null;
	}

	private static WorldDiplomacyClanSnapshot CaptureClanSnapshot(Clan clan, Kingdom kingdom, Clan ruler)
        => new WorldDiplomacyClanSnapshot(clan?.StringId, clan?.Leader?.StringId, clan != null,
            clan?.Kingdom == kingdom, clan == ruler, clan?.IsEliminated == true,
            clan?.IsUnderMercenaryService == true, clan?.IsClanTypeMercenary == true,
            clan?.Leader == ruler?.Leader, clan?.Tier ?? 0, clan?.Influence ?? 0f, clan?.Fiefs?.Count ?? 0);

	private void LogDiplomaticThreatFallbackAnalysisPublished(WorldDiplomacyJob job)
	{
		if (job == null) return;
		WorldDiplomacyDocument document = ResolveDocument(job.DocumentId);
		if (document?.IsReadyForPublication != true) return;
		Log("diplomatic threat declaration settled from fallback analysis after service failure author="
			+ (job.AuthorKingdomId ?? "") + " round=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId)
			+ " document=" + (job.DocumentId ?? ""));
	}

	private void RetryDeferredCanonicalHistoryEntries(int maxAttempts = 16)
	{
		_orchestration.RetryDeferredCanonicalHistoryEntries(maxAttempts);
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
		return _kingdomBorderCache.TryGetValue(WorldDiplomacyRoundLifecycleRules.PairKey(first.StringId, second.StringId), out WorldDiplomacyBorderRelation relation)
			? relation
			: new WorldDiplomacyBorderRelation();
	}
	private void EnsureKingdomBorderCache()
    {
        int day = CurrentDay();
        if (_kingdomBorderCacheDay == day) return;
        _kingdomBorderCacheDay = day;
        _kingdomBorderCache.Clear();
        var raw = new List<WorldDiplomacyFortSnapshot>();
        var live = new List<Settlement>();
        int kingdomIndex = 0;
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null) continue;
            foreach (var fief in kingdom.Fiefs)
            {
                Settlement fort = fief?.Settlement;
                if (fort == null) continue;
                live.Add(fort);
                raw.Add(new WorldDiplomacyFortSnapshot(fort.StringId, fort.Name?.ToString(), kingdom.StringId,
                    kingdomIndex, !kingdom.IsEliminated, fort.IsTown || fort.IsCastle,
                    fort.OwnerClan == kingdom.RulingClan, GetSettlementProsperity(fort)));
            }
            kingdomIndex++;
        }
        List<int> selected = WorldDiplomacyWorldProfileRules.SelectBorderFortIndices(raw);
        var distances = new float[selected.Count, selected.Count];
        // Native float distance is captured once per unordered pair on a daily
        // cache miss. No game object enters the pure neighbor calculation.
        for (int i = 0; i < selected.Count; i++)
            for (int j = i + 1; j < selected.Count; j++)
                distances[j, i] = distances[i, j] = live[selected[i]].GatePosition.Distance(live[selected[j]].GatePosition);
        foreach (var pair in WorldDiplomacyWorldProfileRules.BuildBorders(
            selected.Select(i => raw[i]).ToList(), distances, out _kingdomBorderDistanceThreshold))
            _kingdomBorderCache[pair.Key] = pair.Value;
        Log("kingdom border cache rebuilt day=" + day.ToString(CultureInfo.InvariantCulture)
            + " forts=" + selected.Count.ToString(CultureInfo.InvariantCulture)
            + " threshold=" + _kingdomBorderDistanceThreshold.ToString("0.0", CultureInfo.InvariantCulture)
            + " pairs=" + _kingdomBorderCache.Count.ToString(CultureInfo.InvariantCulture));
    }
	private WorldDiplomacyRealmRelationProfile GetRealmRelationProfile(Kingdom source, Kingdom target)
    {
        if (source == null || target == null) return new WorldDiplomacyRealmRelationProfile();
        string key = source.StringId + ">" + target.StringId + ":" + CurrentDay().ToString(CultureInfo.InvariantCulture);
        if (_realmRelationProfileCache.TryGetValue(key, out WorldDiplomacyRealmRelationProfile cached)) return cached;
        List<Clan> sourceClans = source.Clans.ToList(), targetClans = target.Clans.ToList();
        var sourceValues = sourceClans.Select(c => CaptureClanSnapshot(c, source, source.RulingClan)).ToList();
        var targetValues = targetClans.Select(c => CaptureClanSnapshot(c, target, target.RulingClan)).ToList();
        List<int> sourceIndices = WorldDiplomacyWorldProfileRules.SelectRealmClanIndices(sourceValues);
        List<int> targetIndices = WorldDiplomacyWorldProfileRules.SelectRealmClanIndices(targetValues);
        var relations = new int[sourceIndices.Count, targetIndices.Count];
        for (int i = 0; i < sourceIndices.Count; i++)
            for (int j = 0; j < targetIndices.Count; j++)
            {
                try { relations[i, j] = FactionManager.GetRelationBetweenClans(sourceClans[sourceIndices[i]], targetClans[targetIndices[j]]); }
                catch { relations[i, j] = 0; }
            }
        WorldDiplomacyRealmRelationProfile profile = WorldDiplomacyWorldProfileRules.BuildRealmProfile(
            sourceIndices.Select(i => sourceValues[i]).ToList(), targetIndices.Select(i => targetValues[i]).ToList(),
            relations, GetRulerRelation(source, target));
        _realmRelationProfileCache[key] = profile;
        return profile;
    }

	private bool TryConsumeDiplomacyLlmRequestBudget(bool consume = true)
	{
		return consume ? _storage.RequestBudget.TryAdmit(false) : _storage.RequestBudget.CanAdmit(false);
	}

	private Settlement ResolveCourtSettlement(Kingdom kingdom)
    {
        if (kingdom == null) return null;
        if (_courtSettlementCache.TryGetValue(kingdom.StringId ?? "", out string cachedId)) return ResolveSettlementById(cachedId);
        var forts = new List<WorldDiplomacyFortSnapshot>();
        var live = new List<Settlement>();
        foreach (var fief in kingdom.Fiefs)
        {
            Settlement fort = fief?.Settlement;
            if (fort == null) continue;
            live.Add(fort);
            forts.Add(new WorldDiplomacyFortSnapshot(fort.StringId, null, kingdom.StringId, 0,
                !kingdom.IsEliminated, fort.IsTown || fort.IsCastle, fort.OwnerClan == kingdom.RulingClan, GetSettlementProsperity(fort)));
        }
        int selected = WorldDiplomacyWorldProfileRules.SelectCourtIndex(forts);
        Settlement court = selected < 0 ? null : live[selected];
        _courtSettlementCache[kingdom.StringId ?? ""] = court?.StringId ?? "";
        return court;
    }
	private static float GetSettlementProsperity(Settlement settlement)
	{
		return settlement?.Town?.Prosperity ?? 0f;
	}

	private WorldDiplomacyRound ResolveRound(string roundId)
	{
		return Orchestration.ResolveRound(roundId);
	}

	private WorldDiplomacyCessionReceipt TryApplyValidatedCession(WorldDiplomacyPeaceTerms terms, Kingdom first, Kingdom second)
        => AnimusForge.Refactor.Adapters.BannerlordWorldDiplomacyCessionGameActionPort.Apply(terms,
            first, second, ResolveKingdom(terms?.CessionFromKingdomId), ResolveKingdom(terms?.CessionToKingdomId),
            ResolveSettlementById(terms?.CessionSettlementId), Log);

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
		var port = new WarAdmissionPort(this, initiator, target);
		return WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref port, out reason);
	}
	private bool CanDeclareWar(Kingdom initiator, Kingdom target, out string reason, bool enforceRejectedUltimatum = false, bool isPlayerAuthored = false)
	{
		var port = new WarAdmissionPort(this, initiator, target);
		return WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref port, out reason, enforceRejectedUltimatum, isPlayerAuthored);
	}

		private void NotifyExternalDiplomacyResolvedInternal(string action, Kingdom initiator, Kingdom target, string reason)
	{
		_orchestration.NotifyExternalDiplomacyResolved(action, initiator?.StringId, target?.StringId, reason);
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
                var current = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
                if (current?.IsPersistenceAvailable == false && IsNativeDiplomacyDecision(kingdomDecision)) return false;
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
			TextObject explanation = DiplomacyUiLocalization.Object("AF_DIP_UI_WorldDiplomacy_ManagedByAi", "This action is managed by AI diplomacy. Publish a diplomatic declaration from Kingdom Bulletins.");
			TextObject hint = DiplomacyUiLocalization.Object("AF_DIP_UI_WorldDiplomacy_ManagedByAi", "This action is managed by AI diplomacy. Publish a diplomatic declaration from Kingdom Bulletins.");
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
		InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_WorldDiplomacy_ManagedByAi", "This action is managed by AI diplomacy. Publish a diplomatic declaration from Kingdom Bulletins.")));
		return false;
	}
	private bool CaptureNativeDiplomacyDecision(Kingdom hostKingdom, KingdomDecision decision)
	{
		return WorldDiplomacyNativeDecisionApplication.Capture(new NativeDecisionPort(hostKingdom, decision), 0, _orchestration);
	}
	private void RemoveQueuedNativeDiplomacyDecisions()
	{
		WorldDiplomacyNativeDecisionApplication.Sanitize(new NativeDecisionPort(), _orchestration);
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
            Hero hero = targetHero ?? targetCharacter?.HeroObject;
            bool proactiveDiscussion = ProactiveNpcRequestBehavior.IsNeedTypeActiveForExternal("Diplomacy")
                && ProactiveNpcRequestBehavior.IsActiveRequestHero(hero);
            string block = DiplomacyConversationBridge.BuildDiplomacyMemory(hero, kingdomIdOverride, input,
                __result.PreprocessRuleIds, proactiveDiscussion);
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

    internal static bool TryCaptureMemory(string heroId, string kingdomOverride, out WorldDiplomacyMemorySnapshot snapshot)
    {
        var owner = ResolveInstance();
        if (owner == null) { snapshot = default; return false; }
        Hero hero = DiplomacyIdentityResolver.Hero(heroId);
        snapshot = new WorldDiplomacyMemorySnapshot(owner._storage,
            WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomOverride),
            owner.GetKnownDocumentIdsForHero(hero, kingdomOverride));
        return true;
    }

	private HashSet<string> GetKnownDocumentIdsForHero(Hero hero, string kingdomIdOverride)
	{
		string kingdomId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		Settlement currentSettlement = hero?.CurrentSettlement ?? hero?.PartyBelongedTo?.CurrentSettlement;
		bool isKingdomNoble = hero?.IsLord == true && !string.IsNullOrWhiteSpace(hero.Clan?.Kingdom?.StringId);
		bool includeCourtKnowledge = (hero?.Clan != null && hero.Clan == hero.Clan.Kingdom?.RulingClan)
			|| string.Equals(hero?.StringId, ResolveKingdom(kingdomId)?.RulingClan?.Leader?.StringId, StringComparison.OrdinalIgnoreCase);
		return WorldDiplomacyRoundLifecycleRules.CollectKnownDocumentIds(
			_storage?.SettlementKnowledge, _storage?.NobleKnowledge, _storage?.KingdomKnowledge,
			currentSettlement?.StringId, kingdomId, isKingdomNoble, includeCourtKnowledge);
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
				_notifications.ResetView();
			}
			return true;
		}
		catch (Exception ex)
		{
			Log("notification registration failed: " + ex.Message);
			return false;
		}
	}

	private static string BuildCommonDiplomacySystemPrefix()
	{
		return DuelSettings.LocalizeWorldDiplomacyContractForPrompt(DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? "");
	}
	private string GetCommonDiplomacyContract(WorldDiplomacyRound round)
	{
		return BuildCommonDiplomacySystemPrefix();
	}
	private string ResolveCommonContractForCacheDiagnostics(WorldDiplomacyJob job, out string source)
	{
		if (WorldDiplomacyPromptContractRules.TryExtractCommonContractFromJob(job, out string jobContract))
		{
			source = "job-system";
			return jobContract;
		}
		source = "current-config";
		return BuildCommonDiplomacySystemPrefix();
	}
	private static IEnumerable<string> ProjectCessionCandidates(IEnumerable<Settlement> settlements)
	{
		return (settlements ?? Enumerable.Empty<Settlement>())
			.Where(x => x != null)
			.Select(x => (x.StringId ?? "") + "=" + (x.Name?.ToString() ?? DiplomacyMiscPromptTexts.Get("WorldDiplomacy_Unknown", "Unknown")));
	}

	private static WorldDiplomacyRulerCaptivity ResolveAuthorRulerCaptivity(Kingdom author)
	{
		WorldDiplomacyRulerCaptivity captivity = new WorldDiplomacyRulerCaptivity();
		Hero ruler = author?.RulingClan?.Leader ?? author?.Leader;
		if (ruler == null || !ruler.IsPrisoner) return captivity;
		captivity.IsPrisoner = true;
		TaleWorlds.CampaignSystem.Party.PartyBase holder = null;
		try { holder = ruler.PartyBelongedToAsPrisoner; } catch { }
		Kingdom holderKingdom = null;
		try { holderKingdom = holder?.MapFaction as Kingdom; } catch { }
		if (holderKingdom != null && !holderKingdom.IsEliminated)
		{
			captivity.HolderKingdomId = holderKingdom.StringId ?? "";
			captivity.HolderKingdomName = KingdomName(holderKingdom);
		}
		return captivity;
	}

	private static string BuildRulerVoiceContext(Kingdom kingdom)
	{
		Hero ruler = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		if (ruler == null)
		{
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_000", "RulerPersona{name=Unknown ruler,culture=") + (kingdom?.Culture?.Name?.ToString() ?? DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown")) + DiplomacyPromptTexts.Get("RulerVoicePrompt_001", ",note=No usable character profile; do not invent personal experiences}");
		}
		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out string background);
		string compactPersonality = WorldDiplomacyTextRules.CompactPromptFact(personality, 280);
		string compactBackground = WorldDiplomacyTextRules.CompactPromptFact(background, 420);
		string title = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			kingdom?.EncyclopediaRulerTitle?.ToString(),
			ruler.Clan?.Name?.ToString(),
			DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown"));

		return "RulerPersona{name=" + (ruler.Name?.ToString() ?? DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown"))
			+ ",kingdom=" + KingdomName(kingdom)
			+ ",culture=" + (kingdom?.Culture?.Name?.ToString() ?? ruler.Culture?.Name?.ToString() ?? DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown"))
			+ ",title=" + WorldDiplomacyTextRules.CompactPromptFact(title, 80)
			+ ",traits=" + BuildRulerVoiceTraitSummary(ruler)
			+ ",personality=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(compactPersonality, DiplomacyPromptTexts.Get("RulerVoicePrompt_004", "No dedicated personality profile supplied"))
			+ ",background=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(compactBackground, DiplomacyPromptTexts.Get("RulerVoicePrompt_005", "No dedicated background profile supplied; do not invent experiences"))
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
		string cultureName = kingdom.Culture?.Name?.ToString() ?? ruler?.Culture?.Name?.ToString() ?? DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown");
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
				+ " " + DiplomacyMiscPromptTexts.Get("Prompt_RulerFallback", "Ruler");

				lore = library.BuildLoreContextWithoutPlayerContext(query, ruler, "world_diplomacy_realm_voice", entities);
				string result = WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore, DiplomacyPromptTexts.Get);
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
		return WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore, DiplomacyPromptTexts.Get);
	}
	private static string ResolveRealmRulerTitle(Kingdom kingdom, Hero ruler)
	{
		string kingdomId = (kingdom?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.Equals(kingdomId, "empire_n", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kingdomId, "empire_w", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kingdomId, "empire_s", StringComparison.OrdinalIgnoreCase))
		{
			return ruler?.IsFemale == true ? DiplomacyPromptTexts.Get("RulerVoicePrompt_006", "Empress") : DiplomacyPromptTexts.Get("RulerVoicePrompt_007", "Emperor");
		}
		return WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(kingdom?.EncyclopediaRulerTitle?.ToString(), DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown"));

	}
	private static string BuildCanonicalRealmGovernmentHardFact(Kingdom kingdom, string rulerTitle)
	{
		string title = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(rulerTitle, DiplomacyPromptTexts.Get("RulerVoicePrompt_003", "Unknown ruler"));

		switch ((kingdom?.StringId ?? "").Trim().ToLowerInvariant())
		{
			case "empire_n":
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_008", "The Northern Empire is an imperial regime based on the senate and senatorial political traditions; the supreme ruler’s personal title is ") + title + DiplomacyPromptTexts.Get("RulerVoicePrompt_009", ". The senate is a state institution, not the ruler’s personal identity; do not call the ruler a senator, legislator or consul.");
			case "empire_w":
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_010", "The Western Empire is an imperial regime whose legitimacy rests on military acclamation, military merit and military political traditions; the supreme ruler’s personal title is ") + title + DiplomacyPromptTexts.Get("RulerVoicePrompt_011", "; do not substitute king, general, senator or consul. The Western Empire is not a senatorial regime.");
			case "empire_s":
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_012", "The Southern Empire is an imperial monarchy whose legitimacy rests on dynastic succession and monarchical authority; the supreme ruler’s personal title is ") + title + DiplomacyPromptTexts.Get("RulerVoicePrompt_013", "; do not substitute king, queen, senator, legislator or consul. The Southern Empire is not a senatorial regime.");
			default:
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_014", "The supreme ruler’s personal title confirmed by the current game identity is ") + title + DiplomacyPromptTexts.Get("RulerVoicePrompt_015", "; this title is a hard fact. Institutional terminology or character background must not replace it.");
		}
	}
	private static string BuildRulerVoiceTraitSummary(Hero ruler)
	{
		if (ruler == null)
		{
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown");
		}
		try
		{
			List<string> traits = new List<string>();
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Mercy), DiplomacyPromptTexts.Get("RulerVoicePrompt_016", "merciful"), DiplomacyPromptTexts.Get("RulerVoicePrompt_017", "ruthless"));
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Valor), DiplomacyPromptTexts.Get("RulerVoicePrompt_018", "brave"), DiplomacyPromptTexts.Get("RulerVoicePrompt_019", "cautious and risk-averse"));
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Honor), DiplomacyPromptTexts.Get("RulerVoicePrompt_020", "values honor and commitments"), DiplomacyPromptTexts.Get("RulerVoicePrompt_021", "skilled in political intrigue"));
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Generosity), DiplomacyPromptTexts.Get("RulerVoicePrompt_022", "generous"), DiplomacyPromptTexts.Get("RulerVoicePrompt_023", "values savings and weighs costs"));
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Calculating), DiplomacyPromptTexts.Get("RulerVoicePrompt_024", "calculating"), DiplomacyPromptTexts.Get("RulerVoicePrompt_025", "direct and decisive"));
			return traits.Count == 0 ? DiplomacyPromptTexts.Get("RulerVoicePrompt_026", "No clear inclination") : string.Join(DiplomacyPromptTexts.Get("RulerVoicePrompt_028", ", "), traits);
		}
		catch
		{
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_027", "Read failed");
		}
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
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_029", "hero=Unknown,parents=[],spouse=None,children=[]");
		}
		List<string> parents = new List<string>();
		if (hero.Father != null)
		{
			parents.Add(DiplomacyPromptTexts.Get("RulerVoicePrompt_030", "father:") + FormatHeroFamilyIdentity(hero.Father));
		}
		if (hero.Mother != null)
		{
			parents.Add(DiplomacyPromptTexts.Get("RulerVoicePrompt_031", "mother:") + FormatHeroFamilyIdentity(hero.Mother));
		}
		List<string> children = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((hero.Children ?? Enumerable.Empty<Hero>())
			.Where(x => x != null)
			.Select(FormatHeroFamilyIdentity))
			.Take(16)
			.ToList();
		return "hero=" + FormatHeroFamilyIdentity(hero)
			+ ",parents=[" + string.Join(";", parents) + "]"
			+ ",spouse=" + (hero.Spouse == null ? "" : FormatHeroFamilyIdentity(hero.Spouse))

			+ ",children=[" + string.Join(";", children) + "]";
	}
	private static string FormatHeroFamilyIdentity(Hero hero)
	{
		if (hero == null)
		{
			return DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown");
		}
		return (hero.Name?.ToString() ?? DiplomacyPromptTexts.Get("RulerVoicePrompt_002", "Unknown"))
			+ "(id=" + (hero.StringId ?? "")
			+ "," + (hero.IsAlive ? DiplomacyPromptTexts.Get("RulerVoicePrompt_032", "alive") : DiplomacyPromptTexts.Get("RulerVoicePrompt_033", "deceased")) + ")";
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
				parts.Add(score > 0f ? DiplomacyCompatibilityTexts.Get("WarScore_WarReasonSufficient") : DiplomacyCompatibilityTexts.Get("WarScore_WarReasonInsufficient"));
				if (!string.IsNullOrWhiteSpace(reason?.ToString()))
				{
					parts.Add(DiplomacyCompatibilityTexts.Get("WarScore_NativeReasonPrefix") + reason);
				}
			}
			else if (action == "propose_peace")
			{
				float score = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(source, target);
				parts.Add(score > 0f ? DiplomacyCompatibilityTexts.Get("PeaceScore_PeaceInclined") : DiplomacyCompatibilityTexts.Get("PeaceScore_PeaceNotInclined"));
			}
		}
		catch
		{
		}
		int relation = GetRulerRelation(source, target);
		parts.Add(DiplomacyCompatibilityTexts.Get("RulerScore_PrivateRelationPrefix") + WorldDiplomacyTextRules.DescribeRulerRelation(relation));
		int claims = CountCulturalClaims(source, target);
		if (claims > 0)
		{
			parts.Add(DiplomacyCompatibilityTexts.Get("RulerScore_CultureClaimsPrefix") + claims.ToString(CultureInfo.InvariantCulture));
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
	private static WorldDiplomacyAuthoritySnapshot CaptureDiplomacyAuthority(Kingdom kingdom)
    {
        bool agreement = TryGetWorldDiplomacyVassalage(kingdom, out _, out Kingdom suzerain, out AfVassalageType type);
        return new WorldDiplomacyAuthoritySnapshot(kingdom?.StringId, kingdom != null, kingdom?.IsEliminated == true,
            agreement && type == AfVassalageType.Vassal, suzerain?.StringId,
            IsPlayerKingdom(kingdom), kingdom?.RulingClan?.Leader?.IsAlive == true);
    }
    private static bool HasIndependentWorldDiplomacyAuthority(Kingdom kingdom)
        => WorldDiplomacyAuthorityRules.HasIndependentAuthority(CaptureDiplomacyAuthority(kingdom));
	private static Kingdom ResolveWorldDiplomacyRepresentative(Kingdom kingdom)
    {
        WorldDiplomacyAuthoritySnapshot snapshot = CaptureDiplomacyAuthority(kingdom);
        string representative = WorldDiplomacyAuthorityRules.Representative(snapshot);
        return string.Equals(representative, kingdom?.StringId, StringComparison.OrdinalIgnoreCase)
            ? kingdom : ResolveKingdom(representative);
    }
	private static string GetWorldDiplomacyVassalageTypeName(AfVassalageType type)
	{
		return NormalizeWorldDiplomacyVassalageType(type) switch
		{
			AfVassalageType.Tributary => DiplomacyPromptTexts.Get("WorldVassalagePrompt_000", "tributary"),
			AfVassalageType.Garrison => DiplomacyPromptTexts.Get("WorldVassalagePrompt_001", "garrison kingdom"),
			_ => DiplomacyPromptTexts.Get("WorldVassalagePrompt_002", "independent")

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
				AfVassalageType.Tributary => DiplomacyPromptTexts.Get("WorldVassalagePrompt_003", "Retains diplomatic and military autonomy and pays tribute to the suzerain for protection"),
				AfVassalageType.Garrison => DiplomacyPromptTexts.Get("WorldVassalagePrompt_004", "Obeys the suzerain's military orders but may still express its own interests under the treaty"),
				_ => DiplomacyPromptTexts.Get("WorldVassalagePrompt_005", "Acts fully independently")

			};
			relations.Add("- " + subject.StringId + "=" + KingdomName(subject)
				+ DiplomacyPromptTexts.Get("WorldVassalagePrompt_006", "; suzerain=") + suzerain.StringId + "=" + KingdomName(suzerain) + DiplomacyPromptTexts.Get("WorldVassalagePrompt_007", "; type=")
				+ GetWorldDiplomacyVassalageTypeName(type) + DiplomacyPromptTexts.Get("WorldVassalagePrompt_008", "; authority=") + authority + DiplomacyPromptTexts.Get("WorldVassalagePrompt_009", "."));
		}
		if (relations.Count == 0)
		{
			return "";
		}
		return DiplomacyPromptTexts.Get("WorldVassalagePrompt_010", "[Current suzerain–subject relationship hard facts]\n")
			+ string.Join("\n", relations)
			+ DiplomacyPromptTexts.Get("WorldVassalagePrompt_011", "\nSubjects must acknowledge their existing suzerain relationship and maintain the formal respect due from subjects when dealing with the suzerain. This does not require flattery in every declaration or abandoning expressions of interests retained under the treaty.");
	}

	private List<string> BuildLegalDiplomaticActionIntents(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom target)
	{
        return new WorldDiplomacyActionSelectionApplication(new ActionSelectionPort(this, author, target)).BuildLegalDiplomaticActionIntents(round, author?.StringId, target?.StringId);
    }
	private static WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(
		WorldDiplomacyRound round,
		Kingdom author,
		string resultSettlementSlotId,
		bool isExternalResponseOnly,
		string sourceDocumentId,
		bool requireAnyOpenPeaceOffer = false)
	{
		return WorldDiplomacyRoundLifecycleRules.FindRequiredPeaceOfferResponse(
			round, author?.StringId, resultSettlementSlotId, isExternalResponseOnly,
			sourceDocumentId, requireAnyOpenPeaceOffer);
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
		return WorldDiplomacyDocumentApplication.Create(new WorldDiplomacyDocumentApplication.CreationSnapshot
		{
			DocumentId = NewId("diplomacy_document"),
			ExchangeId = exchangeId,
			AuthorKingdomId = author?.StringId,
			AuthorKingdomName = KingdomName(author),
			AuthorRulerId = author?.RulingClan?.Leader?.StringId,
			AuthorRulerName = RulerName(author),
			TargetKingdomId = target?.StringId,
			TargetKingdomName = target == null ? "" : KingdomName(target),
			HasTarget = target != null,
			Title = title,
			Body = body,
			Origin = origin,
			Day = CurrentDay(),
			GameDate = FormatCampaignDate(CurrentDay()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			IsPlayerAuthored = isPlayerAuthored,
			IsResponse = isResponse
		});
	}

	private void AdvanceWorldMessageTimelineRevision()
	{
		// Overflow is practically unreachable, but preserving a nonzero revision keeps the comparison valid in long-running sessions.
		_worldMessageTimelineRevision = _worldMessageTimelineRevision == long.MaxValue
			? 1L
			: _worldMessageTimelineRevision + 1L;
	}

	private static List<PublishedPolicyArtifactLedgerEntry> ReadAllPublishedPolicyArtifactsForMigration()
	{
		List<PublishedPolicyArtifactLedgerEntry> result = new List<PublishedPolicyArtifactLedgerEntry>();
		long cursor = 0L;
		long available = DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryCurrentSequence();
		while (cursor < available)
		{
			IReadOnlyList<PublishedPolicyArtifactLedgerEntry> batch = DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryArtifacts(cursor, 1024);
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

	private string ResolveEligibleDiplomacyKingdomId(string kingdomId)
	{
		Kingdom kingdom = ResolveKingdom(kingdomId);
		return kingdom != null && !kingdom.IsEliminated && HasIndependentWorldDiplomacyAuthority(kingdom)
			? kingdom.StringId
			: null;
	}
	private bool IsAtWarByKingdomIds(string issuerKingdomId, string targetKingdomId)
	{
		Kingdom issuer = ResolveKingdom(issuerKingdomId);
		Kingdom target = ResolveKingdom(targetKingdomId);
		return issuer != null && target != null && FactionManager.IsAtWarAgainstFaction(issuer, target);
	}
	private string ValidateOpenThreatWorldEligibility(WorldDiplomacyThreat threat, IAllianceCampaignBehavior alliance)
	{
		Kingdom issuer = ResolveKingdom(threat.IssuerKingdomId);
		Kingdom target = ResolveKingdom(threat.TargetKingdomId);
		return WorldDiplomacyRoundLifecycleRules.ValidateOpenThreatEligibility(
			issuer != null, issuer?.IsEliminated == true, issuer != null && HasIndependentWorldDiplomacyAuthority(issuer),
			target != null, target?.IsEliminated == true, target != null && HasIndependentWorldDiplomacyAuthority(target),
			issuer != null && issuer == target,
			issuer != null && target != null && FactionManager.IsAtWarAgainstFaction(issuer, target),
			issuer != null && target != null && alliance?.IsAllyWithKingdom(issuer, target) == true);
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

	private WorldDiplomacyDocument ResolveDocument(string documentId)
	{
		return WorldDiplomacyRoundLifecycleRules.ResolveDocument(_storage?.Documents, documentId);
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
			snapshot.AuthorCessionScore = WorldDiplomacyRoundLifecycleRules.CalculateCessionScore(snapshot, GetUnrecoveredLostSettlements(author, target).Count, authorPerspective: true);
			snapshot.TargetCessionScore = WorldDiplomacyRoundLifecycleRules.CalculateCessionScore(snapshot, GetUnrecoveredLostSettlements(target, author).Count, authorPerspective: false);
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
        float pressure = WorldDiplomacyWarPressureRules.CalculatePeacePressure(snapshot.WarDays,
            authorPerspective ? snapshot.AuthorProgress : snapshot.TargetProgress,
            authorPerspective ? snapshot.TargetProgress : snapshot.AuthorProgress,
            authorPerspective ? snapshot.AuthorStrength : snapshot.TargetStrength,
            authorPerspective ? snapshot.TargetStrength : snapshot.AuthorStrength,
            authorPerspective ? snapshot.AuthorSufferedCasualties : snapshot.AuthorInflictedCasualties,
            authorPerspective ? snapshot.AuthorInflictedCasualties : snapshot.AuthorSufferedCasualties,
            authorPerspective ? snapshot.AuthorOtherWars : snapshot.TargetOtherWars,
            GetUnrecoveredLostSettlements(authorPerspective ? author : target, authorPerspective ? target : author).Count);
        // This war's weariness (the terminal's bar for this side): a worn-out side is pushed toward peace.
        return WorldDiplomacyWarPressureRules.WithWeariness(pressure, MyBehavior.GetWarWearinessForExternal(authorPerspective ? author : target, authorPerspective ? target : author));
    }

	private List<Settlement> GetUnrecoveredLostSettlements(Kingdom originalOwner, Kingdom currentOwner)
	{
		WorldDiplomacyWarLedger ledger = WorldDiplomacyWarPressureRules.ResolveWarLedger(_storage?.ActiveWarLedgers, originalOwner?.StringId, currentOwner?.StringId);
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
        var port = new PeaceAdmissionPort(this);
        return port.ResolveSelected(WorldDiplomacyPeaceAdmissionApplication.BuildCessionCandidates(port, cedingKingdom?.StringId, receivingKingdom?.StringId, cessionScore));
    }
	private static Settlement ResolveSettlementById(string settlementId)
	{
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return null;
		}
		Settlement indexed = DiplomacyIdentityResolver.Settlement(settlementId.Trim());
		if (indexed != null) return indexed;
		return Settlement.All.FirstOrDefault(x => x != null
			&& string.Equals(x.StringId, settlementId, StringComparison.OrdinalIgnoreCase));
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
	internal static void GetDiplomaticDeclarationCharacterRange(out int minimumCharacters, out int maximumCharacters)
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
	private static int GetOrdinaryRoundLimit()
	{
		try
		{
			return Math.Max(1, Math.Min(DuelSettings.WorldDiplomacyMaxConcurrentOrdinaryRoundsMax,
                DuelSettings.GetSettings()?.WorldDiplomacyMaxConcurrentOrdinaryRounds
                ?? DuelSettings.DefaultWorldDiplomacyMaxConcurrentOrdinaryRounds));
		}
		catch
		{
			return DuelSettings.DefaultWorldDiplomacyMaxConcurrentOrdinaryRounds;
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

	private int GetOfferCooldownLastFailedRoundDay(WorldDiplomacyOfferCooldownKey key)
	{
		return _runtime.OfferCooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown cooldown)
			? cooldown.LastFailedRoundDay
			: -1;
	}
	private static int GetRoundParticipantLimit()
	{
		return WorldDiplomacyRoundLifecycleRules.GetRoundParticipantLimit(GetActivityLevel(), MaxRelayParticipants);
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
		return WorldDiplomacyEventRules.RoundHardDurationDays(targetDurationDays);
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
        var owner = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        return owner?.IsPersistenceAvailable == true ? owner : null;
	}
	private static Kingdom ResolveKingdom(string id)
	{
		if (string.IsNullOrWhiteSpace(id) || Campaign.Current == null)
		{
			return null;
		}
		string normalizedId = id.Trim();
		Kingdom indexed = DiplomacyIdentityResolver.Kingdom(normalizedId);
		if (indexed != null && !indexed.IsEliminated) return indexed;
		return Kingdom.All.FirstOrDefault(x => x != null
			&& !x.IsEliminated
			&& (string.Equals(x.StringId, normalizedId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Name?.ToString(), normalizedId, StringComparison.OrdinalIgnoreCase)));
	}
	private static Kingdom ResolveKingdomIncludingEliminated(string id)
	{
		if (string.IsNullOrWhiteSpace(id) || Campaign.Current == null) return null;
		string normalizedId = id.Trim();
		Kingdom indexed = DiplomacyIdentityResolver.Kingdom(normalizedId);
		if (indexed != null) return indexed;
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
        => WorldDiplomacyAuthorityRules.CanAiAuthor(CaptureDiplomacyAuthority(kingdom), out reason);
	private static int CurrentDay()
	{
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return 0;
		}
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
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return 0;
		}
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
				0 => DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSpring"),
				1 => DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSummer"),
				2 => DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonAutumn"),

				_ => DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonWinter")

			};
			return year.ToString(CultureInfo.InvariantCulture)
				+ DiplomacyCompatibilityTexts.Get("KingdomProfile_YearSuffix")
				+ seasonText
				+ DiplomacyCompatibilityTexts.Get("KingdomProfile_SeasonSuffix")
				+ dayOfSeason.ToString(CultureInfo.InvariantCulture)
				+ DiplomacyCompatibilityTexts.Get("KingdomProfile_DaySuffix");

		}
		catch
		{
			return DiplomacyCompatibilityTexts.Get("KingdomProfile_DayPrefix") + Math.Max(0, day).ToString(CultureInfo.InvariantCulture) + DiplomacyCompatibilityTexts.Get("KingdomProfile_DaySuffix");
		}
	}
	private static string NewId(string prefix)
	{
		return (prefix ?? "world_diplomacy") + ":" + Guid.NewGuid().ToString("N");
	}
	private static string KingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? DiplomacyMiscPromptTexts.Get("Prompt_UnknownKingdomFallback", "Unknown Kingdom");
	}
	private static string ResolveKingdomNameOrEmpty(string kingdomId)
	{
		Kingdom kingdom = ResolveKingdom(kingdomId);
		return kingdom == null ? "" : KingdomName(kingdom);
	}
	private static string RulerName(Kingdom kingdom)
	{
		return kingdom?.RulingClan?.Leader?.Name?.ToString() ?? DiplomacyMiscPromptTexts.Get("WorldDiplomacy_UnknownRuler", "Unknown ruler");
	}
	private static List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(ResolveKingdom).Where(x => x != null && !string.Equals(x.StringId, excludedId, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
	private bool IsDiplomaticRepresentativeForAddressedVassal(Kingdom receiver, WorldDiplomacyDocument document)
	{
		return receiver != null && WorldDiplomacyDocumentFactRules.IsDiplomaticRepresentativeForAddressedVassal(document, id =>
		{
			Kingdom addressed = ResolveKingdom(id);
			return addressed != null && addressed != receiver && ResolveWorldDiplomacyRepresentative(addressed) == receiver;
		});
	}
}
