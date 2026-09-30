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
	private const int DiplomacyPromptContractVersion = 28;
	private const int RelaySchemaVersion = 23;
	private const int RelayPassDurationDays = 7;
	private const int RelayTargetDurationDays = 21;
	private const int RelayHardDurationDays = 24;
	private const int MaxRelayParticipants = 12;
	private const int BorderForeignNeighborCount = 2;
	private const int RecentNegativeReputationFactRetentionDays = DaysPerYear;
	private const float BorderDistanceMedianMultiplier = 3.5f;
	private const float MinimumBorderDistance = 24f;
	private const float MaximumBorderDistance = 72f;

	private static bool _patchesApplied;
	private static int _internalDiplomaticActionDepth;
	private static readonly BannerlordWorldDiplomacyPersistenceAdapter PersistenceAdapter =
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
	private readonly WorldDiplomacyRequestLeaseCoordinator _llmRequestLease = new WorldDiplomacyRequestLeaseCoordinator();
	private int _kingdomBorderCacheDay = -1;
	private float _kingdomBorderDistanceThreshold = MinimumBorderDistance;
	private long _realmInstitutionalVoiceRuleVersion = -1L;

	private WorldDiplomacyStorage _storage = new WorldDiplomacyStorage();
	private MapNotificationView _registeredMapNotificationView;
	private long _runtimeGeneration;
	// Runtime-only revision lets the world-message timeline detect a published document without cloning the archive every tick.
	private long _worldMessageTimelineRevision = 1L;

	private readonly WorldDiplomacyLlmBudget _llmBudget = new WorldDiplomacyLlmBudget();
	private long _cacheHitTokensThisSession;
	private long _cacheMissTokensThisSession;
	private long _relayCacheHitTokensThisSession;
	private long _relayCacheMissTokensThisSession;
	private readonly WorldDiplomacyRuntimeState _runtime = new WorldDiplomacyRuntimeState();
	private readonly WorldDiplomacyOrchestration _orchestration;

	public static WorldDiplomacyBehavior Instance { get; private set; }
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
		if (dataStore.IsSaving)
		{
			_orchestration.NormalizeStorage(allowWorldValidation: false);
			PersistenceAdapter.Save(dataStore, _storage);
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		_storage = PersistenceAdapter.Load(dataStore, out string loadError);
		if (!string.IsNullOrWhiteSpace(loadError))
		{
			Log("load failed: " + loadError);
		}
		ResetTransientRuntime("load");
		_orchestration.NormalizeStorage(allowWorldValidation: false);
	}

	public void OnEngineTick()
	{
		var source = new TickSource(this);
		WorldDiplomacyTickApplication.Run(ref source, _orchestration);
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
	{
		_storage = new WorldDiplomacyStorage();
		_storage.HistoryMemorySchemaVersion = HistoryMemorySchemaVersion;
		_storage.PromptContractVersion = DiplomacyPromptContractVersion;
		_storage.DiplomaticThreatStateSchemaVersion =
			WorldDiplomacyThreatStorageMigration.DiplomaticThreatStateSchemaVersion;
		_storage.OfferCooldownStateSchemaVersion =
			WorldDiplomacyOfferCooldownStorageNormalizer.CurrentSchemaVersion;
		_storage.ResultSettlementStateSchemaVersion = ResultSettlementStateSchemaVersion;
		_storage.DiplomacyNotificationStateSchemaVersion =
			WorldDiplomacyNotificationStateMigration.CurrentSchemaVersion;
		_storage.CanonicalHistory = new WorldDiplomacyCanonicalHistoryState();
		_storage.DecisionArchitectureVersion = DecisionArchitectureVersion;
		_storage.PropagationReliabilityVersion = 1;
		_storage.InitialPeacePending = IsWorldDiplomacyEnabled() && ShouldStartNewGameAtPeace();
		InitializeSchedule();
		ResetTransientRuntime("new-game");
	}
	private void OnGameLoaded(CampaignGameStarter starter)
	{
		_orchestration.NormalizeStorage(allowWorldValidation: true);
		_orchestration.RecoverUnsettledAiInternationalReputation();
		_orchestration.RecoverPlayerCourtReceiptsFromKnowledge();
		InitializeSchedule();
		ResetTransientRuntime("game-loaded");
		_orchestration.ReconcileActiveDiplomacyAfterLoad();
	}
	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		_orchestration.NormalizeStorage(allowWorldValidation: true);
		_orchestration.RecoverUnsettledAiInternationalReputation();
		_orchestration.RecoverPlayerCourtReceiptsFromKnowledge();
		InitializeSchedule();
		ResetTransientRuntime("session-launched");
		_orchestration.ReconcileActiveDiplomacyAfterLoad();
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
		Kingdom first = faction1 as Kingdom;
		Kingdom second = faction2 as Kingdom;
		if (first == null || second == null || first == second)
		{
			return;
		}
		EnsureWarLedger(first, second);
		WorldDiplomacyRoundLifecycleRules.ResolveDiplomaticThreatsAfterWarStarted(
			_storage?.DiplomaticThreats, first?.StringId, second?.StringId, CurrentDay(), _internalDiplomaticActionDepth > 0);
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
		WorldDiplomacyWarPressureRules.RemoveWarLedger(_storage?.ActiveWarLedgers, first.StringId, second.StringId);
		WorldDiplomacyWarPressureRules.ClearWarPressure(_storage?.WarPressure, first.StringId, second.StringId, CurrentDay());
		WorldDiplomacyWarPressureRules.ClearWarPressure(_storage?.WarPressure, second.StringId, first.StringId, CurrentDay());
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
		WorldDiplomacyWarLedger ledger = WorldDiplomacyWarPressureRules.ResolveWarLedger(_storage?.ActiveWarLedgers, oldKingdom.StringId, newKingdom.StringId);
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
				Location = mapEvent.MapEventSettlement?.Name?.ToString() ?? "閲庡",
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
		return WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((side?.Parties ?? Enumerable.Empty<MapEventParty>())
			.Select(x => x?.Party?.LeaderHero?.Name?.ToString()))
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
			_storage.NextNormalRoundDay = WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay(day, intervalDays);
		}
		if (_storage.LastAppliedRoundIntervalDays <= 0) _storage.LastAppliedRoundIntervalDays = intervalDays;
		if (_storage.LastCompressedYear < 0) _storage.LastCompressedYear = WorldDiplomacyRoundLifecycleRules.ComputeInitialCompressedYear(day, DaysPerYear);
	}



































	private void ResetTransientRuntime(string reason)
	{
		_runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		_llmRequestLease.Reset();
		_runtime.DisabledStateApplied = false;
		while (_completedJobs.TryDequeue(out _))
		{
		}
		_notifications.ResetView();
		_registeredMapNotificationView = null;
		_warSituationCache.Clear();
		_realmInstitutionalVoiceCache.Clear();
		_realmRelationProfileCache.Clear();
		_kingdomBorderCache.Clear();
		WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(_storage?.OfferCooldowns, _runtime.OfferCooldownByKey);
		_kingdomBorderCacheDay = -1;
		_realmInstitutionalVoiceRuleVersion = -1L;
		DiplomacyModuleServices.Policy.Clear();
		_runtime.LastLlmCacheAffinityKey = "";
		_runtime.NativeQueueSanitized = false;
		_runtime.LastSchedulerDay = -1;
		_runtime.AiDocumentsStartedDay = -1;
		_runtime.AiDocumentsStartedToday = 0;
		_llmBudget.Reset();
		_cacheHitTokensThisSession = 0;
		_cacheMissTokensThisSession = 0;
		_relayCacheHitTokensThisSession = 0;
		_relayCacheMissTokensThisSession = 0;
		_notifications.Reset();
		_runtime.InitialPeaceApplicationAttempted = false;
		_runtime.CanonicalHistorySourceKeys.Clear();
		_runtime.DeferredCanonicalHistoryDocumentIds.Clear();
		_runtime.DeferredCanonicalHistoryDocumentIdSet.Clear();
		_runtime.DeferredCanonicalHistoryRetryAttempts.Clear();
		_runtime.DeferredCanonicalHistoryRetryAfterHour.Clear();
		foreach (WorldDiplomacyDocument document in _storage.Documents ?? new List<WorldDiplomacyDocument>())
		{
			if (WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document)) WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(_runtime.DeferredCanonicalHistoryDocumentIdSet, _runtime.DeferredCanonicalHistoryDocumentIds, document.DocumentId);
		}
		_runtime.CanonicalHistoryRenderCacheKey = "";
		_runtime.CanonicalHistoryRenderCache = "";
		_runtime.LastCanonicalSourceSyncHour = int.MinValue;
		_runtime.LastObservedWorldWeeklyHistoryRevision = -1L;
		_runtime.CanonicalHistoryInitializedThisSession = false;
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














































	private void AttachPolicySignalToRound(WorldDiplomacyRound round, WorldDiplomacyPolicySignal signal, Kingdom issuer, Kingdom affected)
	{
		if (round == null || signal == null || issuer == null || affected == null)
		{
			return;
		}
		WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(round, signal);
		foreach (Kingdom kingdom in new[] { ResolveWorldDiplomacyRepresentative(issuer), ResolveWorldDiplomacyRepresentative(affected) }
			.Where(x => x != null).Distinct())
		{
			WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, kingdom.StringId, "observer", mandatoryReply: false);
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
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
		sb.AppendLine("档案版本=" + WorldDiplomacyPromptContractRules.StablePromptHash((nationalPersonality ?? "") + "\n" + (longTermStrategy ?? "")));
		sb.AppendLine("国家性格=" + (nationalPersonality ?? ""));
		sb.AppendLine("长期战略=" + (longTermStrategy ?? ""));
		sb.Append(WorldDiplomacyPromptContractRules.KingdomStrategicIntentRule);
		prompt = sb.ToString();
		return true;
	}



























	private void LogPromptCacheShape(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = WorldDiplomacyPromptContractRules.BuildLlmMessagesForJob(job, _orchestration.BuildCanonicalHistoryBlock);
		string system = messages.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string user = messages.LastOrDefault(x => x != null && string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string frozenContract = ResolveCommonContractForCacheDiagnostics(job, out string contractSource);
		int userPrefix1024Chars = Math.Min(1024, user.Length);
		int userPrefixChars = Math.Min(2048, user.Length);
		int totalChars = messages.Sum(x => x?.Content?.Length ?? 0);
		int expectedCachedMessageCount = WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job) && messages.Count >= 2 ? 2 : 0;
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

	private void CommitFailedJob(WorldDiplomacyJob job, string error)
	{
		_orchestration.CommitFailedJob(job, error);
	}
	private void CommitGeneratedDocument(WorldDiplomacyJob job, string raw)
	{
		_orchestration.CommitGeneratedDocument(job, raw);
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





























































































































































































































































	private static bool IsThreatConsequenceClanEligible(Clan clan, Kingdom kingdom, Clan rulingClan)
	{
		return clan != null
			&& clan != rulingClan
			&& clan.Kingdom == kingdom
			&& !clan.IsEliminated
			&& !clan.IsUnderMercenaryService
			&& !clan.IsClanTypeMercenary
			&& !string.IsNullOrWhiteSpace(clan.StringId);
	}























































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
				string key = WorldDiplomacyRoundLifecycleRules.PairKey(kingdom.StringId, otherKingdom.StringId);
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


































































	private bool TryConsumeDiplomacyLlmRequestBudget(bool consume = true)
	{
		return _llmBudget.TryConsume(CurrentDay(), MaxDiplomacyLlmRequestsPerDay, consume, Log);
	}














	private bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document)
	{
		if (document == null || !document.PropagationStarted) return false;
		HashSet<string> pendingSettlements = new HashSet<string>((_storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
			.Where(x => x != null && !WorldDiplomacyStructureRules.IsCourtArrival(x)
				&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId))
			.Select(x => x.SettlementId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		HashSet<string> pendingKingdoms = new HashSet<string>((_storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
			.Where(x => x != null && WorldDiplomacyStructureRules.IsCourtArrival(x)
				&& WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId))
			.Select(x => x.KingdomId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		HashSet<string> knownSettlementIds = WorldDiplomacyDocumentFactRules.GetKnownSettlementIdsForDocument(_storage.SettlementKnowledge, document.DocumentId);
		HashSet<string> knownKingdomIds = WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(_storage.KingdomKnowledge, document.DocumentId);
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































































































































	private void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
	{
		WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(_storage, round, documents,
			CurrentDay, FormatCampaignDate, Log);
	}
	private void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary)
	{
		WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(_storage, summary,
			ResolveRound, FormatCampaignDate);
	}






	private WorldDiplomacyRound ResolveRound(string roundId)
	{
		return WorldDiplomacyRoundLifecycleRules.ResolveRound(_storage?.ActiveRound, _storage?.CompletedRounds, roundId);
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
			return "已将" + from.Name + "割让" + settlement.Name + "给" + to.Name;
		}
		catch (Exception ex)
		{
			Log("peace cession failed settlement=" + settlement.StringId + " error=" + ex.Message);
			return "；领地交割失败";
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
		var port = new WarAdmissionPort(this, initiator, target);
		return WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref port, out reason);
	}
	private bool CanDeclareWar(Kingdom initiator, Kingdom target, out string reason, bool enforceRejectedUltimatum = false)
	{
		var port = new WarAdmissionPort(this, initiator, target);
		return WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref port, out reason, enforceRejectedUltimatum);
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
		string kingdomId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		Settlement currentSettlement = hero?.CurrentSettlement ?? hero?.PartyBelongedTo?.CurrentSettlement;
		bool isKingdomNoble = hero?.IsLord == true && !string.IsNullOrWhiteSpace(hero.Clan?.Kingdom?.StringId);
		bool includeCourtKnowledge = (hero?.Clan != null && hero.Clan == hero.Clan.Kingdom?.RulingClan)
			|| string.Equals(hero?.StringId, ResolveKingdom(kingdomId)?.RulingClan?.Leader?.StringId, StringComparison.OrdinalIgnoreCase);
		return WorldDiplomacyRoundLifecycleRules.CollectKnownDocumentIds(
			_storage?.SettlementKnowledge,
			_storage?.NobleKnowledge,
			_storage?.KingdomKnowledge,
			currentSettlement?.StringId,
			kingdomId,
			isKingdomNoble,
			includeCourtKnowledge);
	}

	private string BuildDiplomacyMemoryContext(Hero hero, string kingdomIdOverride, string input = "")
	{
		if (_storage.Documents.Count == 0)
		{
			return "";
		}
		string kingdomId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(hero?.Clan?.Kingdom?.StringId, kingdomIdOverride);
		HashSet<string> knownIds = GetKnownDocumentIdsForHero(hero, kingdomIdOverride);
		if (knownIds.Count == 0) return "";
		List<WorldDiplomacyDocument> queryMatches = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(_storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
					&& WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input) > 0)
				.OrderByDescending(x => WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input)))
			.Take(2)
			.ToList();
		HashSet<string> selectedIds = new HashSet<string>(queryMatches.Select(x => x.DocumentId), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyDocument> direct = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency(_storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "")
					&& !selectedIds.Contains(x.DocumentId ?? "")
					&& (!string.IsNullOrWhiteSpace(kingdomId) && (string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
						|| (x.AddressedKingdomIds ?? new List<string>()).Contains(kingdomId, StringComparer.OrdinalIgnoreCase))))
				.OrderByDescending(x => WorldDiplomacyDocumentFactRules.DiplomacyDocumentQueryRelevance(x, input)))
			.Take(3)
			.ToList();
		foreach (WorldDiplomacyDocument document in direct) selectedIds.Add(document.DocumentId ?? "");
		List<WorldDiplomacyDocument> headlines = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(_storage.Documents
				.Where(x => x != null && !x.IsCompressed && knownIds.Contains(x.DocumentId ?? "") && !selectedIds.Contains(x.DocumentId ?? "") && WorldDiplomacyDocumentFactRules.IsMajorDiplomaticDocument(x)))
			.Take(2)
			.ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【当前人物已获知的王国公告】");
		sb.AppendLine("以下仅是公文传播到此人所在地点后，或传到其所属王庭后由贵族通信网获得的事实；不代表全世界同步知晓，也不是当前对话的新承诺。");
		foreach (WorldDiplomacyDocument document in queryMatches)
		{
			sb.AppendLine("- [当前问题命中] " + WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(document, FormatCampaignDate));
		}
		foreach (WorldDiplomacyDocument document in direct)
		{
			sb.AppendLine("- [直接相关] " + WorldDiplomacyTextRules.BuildDetailedDocumentMemoryLine(document, FormatCampaignDate));
		}
		foreach (WorldDiplomacyDocument document in headlines)
		{
			sb.AppendLine("- [世界要闻] " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(document, FormatCampaignDate));
		}
		foreach (WorldDiplomacyRoundSummary summary in _storage.RoundSummaries
			.Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains))
			.OrderByDescending(x => x.CreatedDay).Take(1))
		{
			List<string> visibleFacts = (summary.Facts ?? new List<WorldDiplomacyRoundFact>()).Where(x => x != null && (x.SourceDocumentIds ?? new List<string>()).Any(knownIds.Contains)).Select(WorldDiplomacyDocumentFactRules.FormatRoundFactForPrompt).Where(x => !string.IsNullOrWhiteSpace(x)).Take(6).ToList();
			sb.AppendLine("- [往期外交事件] " + WorldDiplomacyTextRules.Limit(visibleFacts.Count > 0 ? string.Join("、", visibleFacts) : summary.Summary, 650));
		}
		return sb.ToString().TrimEnd();
	}

	private void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent = "")
	{
		WorldDiplomacyWarPressureRules.AddWarPressure(_storage?.WarPressure, sourceId, targetId, delta, reason, CurrentDay(), intent);
	}

	private WarPressureEntry FindWarPressure(string sourceId, string targetId)
	{
		return _storage.WarPressure.FirstOrDefault(x => x != null && string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
	}

















	private void TryPublishPendingNotifications()
	{
		_notifications.Poll(_storage, DateTime.UtcNow, NotificationSink);
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
		return DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? "";
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
	private bool HasStaleDiplomaticThreatPresentation(WorldDiplomacyJob job)
	{
		if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
		List<string> currentPresented = WorldDiplomacyRoundLifecycleRules.SelectPresentedThreatStageDocumentIds(_storage?.DiplomaticThreats, job.AuthorKingdomId);
		List<string> currentFollowThrough = WorldDiplomacyRoundLifecycleRules.SelectNoncompliedThreatStageDocumentIds(_storage?.DiplomaticThreats, job.AuthorKingdomId);
		return WorldDiplomacyRoundLifecycleRules.HasThreatPresentationDrift(
			job.PresentedThreatDocumentIds, currentPresented,
			job.PresentedThreatFollowThroughDocumentIds, currentFollowThrough);
	}





























	private void AppendDiplomaticThreatDynamicContext(StringBuilder sb, Kingdom author, string roundId)
	{
		if (sb == null || author == null) return;
		List<WorldDiplomacyThreat> threats = _storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>();
		int prestige = WorldDiplomacyReputationRules.GetNationalPrestige(_storage?.NationalPrestigeByKingdom, author.StringId);
		int reputation = WorldDiplomacyReputationRules.GetInternationalReputation(_storage?.InternationalReputationByKingdom, author.StringId);
		sb.AppendLine("【本国国家威望、国际声誉趋势与未结威慑；内部动态事实，不得在公文中公开数值】");
		sb.AppendLine("本国当前国家威望=" + prestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(prestige) + "）");
		sb.AppendLine("国家威望衡量本国威慑与承诺是否兑现：威望低会削弱威胁可信度，并按档位动态降低正式封臣家族领袖对国王的关系；恢复威望会撤回这部分动态关系惩罚。");
		sb.AppendLine("本国当前国际声誉=" + reputation.ToString(CultureInfo.InvariantCulture)
			+ "/100（外国公开评价档位=" + WorldDiplomacyReputationRules.DescribeInternationalReputation(reputation)
			+ "）；当前自然趋势=" + WorldDiplomacyReputationRules.DescribeInternationalReputationNaturalTrend(reputation)
			+ "。精确值、档位与趋势用于规划如何维护、修复或为核心利益消耗这项战略资本；现实局势允许时可主动发表有实际内容的宣言维护声誉，但不得为了声誉而沉默、回避合法立场、机械改选动作或发布空话。");
		List<string> recentReputationReasons = WorldDiplomacyTextRules.GetRecentOwnInternationalReputationReasons(
				_storage?.Documents, author.StringId, CurrentDay(), RecentNegativeReputationFactRetentionDays, FormatCampaignDate);
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
		List<WorldDiplomacyStandingChange> recentChanges = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency((_storage?.Documents ?? new List<WorldDiplomacyDocument>())
				.Where(x => x?.DiplomaticStandingChanges != null && x.IsReadyForPublication))
			.SelectMany(x => x.DiplomaticStandingChanges.AsEnumerable().Reverse())
			.Where(x => x != null
				&& string.Equals(x.KingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Kind, "national_prestige", StringComparison.OrdinalIgnoreCase))
			.Take(4)
			.ToList();
		foreach (WorldDiplomacyStandingChange change in recentChanges)
		{
			sb.AppendLine("近期国家威望结算=" + WorldDiplomacyReputationRules.FormatSignedDelta(change.Delta) + "；原因=" + change.Reason + "。");
		}

		WorldDiplomacyThreat outbound = threats.FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x?.Status)
			&& string.Equals(x.IssuerKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
		if (WorldDiplomacyRoundLifecycleRules.IsThreatDecisionNoncomplied(outbound))
		{
			Kingdom target = ResolveKingdom(outbound.TargetKingdomId);
			WorldDiplomacyDocument source = ResolveDocument(outbound.StageDocumentId);
			if (WorldDiplomacyRoundLifecycleRules.IsThreatAtStage(outbound, "warning"))
			{
				sb.AppendLine("强制后果提示：" + KingdomName(target) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国谴责作出不退让决定。本篇就是本国谴责后的下一份宣言，最好对该国升级为战争最后通牒（intent=ultimatum），否则本篇发布后立即扣除20点国家威望。最后通牒必须延续同一军事争端与核心要求，不得更换事项。原谴责标题="
					+ WorldDiplomacyTextRules.Limit(source?.Title, 80) + "；原谴责要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260) + "。");
			}
			else
			{
				sb.AppendLine("强制后果提示：" + KingdomName(target) + "（ID=" + outbound.TargetKingdomId
					+ "）已对本国最后通牒作出不退让决定。本篇就是本国通牒后的下一份宣言，最好对该国宣战（intent=declare_war），否则本篇发布后立即扣除15点国家威望，但也要考虑战争的后果。");
			}
		}
		else if (outbound != null)
		{
			sb.AppendLine("本国已有等待对象国一次性决定的"
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal(outbound.Stage)
				+ "：对象=" + outbound.TargetKingdomId + "=" + KingdomName(ResolveKingdom(outbound.TargetKingdomId))
				+ "，来源=" + outbound.StageDocumentId + "。对象国尚未发布决定；在其决定前不得重复或提前升级该威慑。");
		}

		foreach (WorldDiplomacyThreat incoming in WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			threats, author.StringId))
		{
			WorldDiplomacyDocument source = ResolveDocument(incoming.StageDocumentId);
			sb.AppendLine("本国收到的未决威慑："
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(incoming.Stage))
				+ "：发出国=" + incoming.IssuerKingdomId + "=" + KingdomName(ResolveKingdom(incoming.IssuerKingdomId))
				+ "；来源=" + incoming.StageDocumentId
				+ "；标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80)
				+ "；要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260)
				+ "。选择intent=comply_ultimatum即为无条件退让；任何其他intent即不退让，后续不能反悔。退让会降低本国国家威望，并使本国每个正式封臣家族与当前王族关系下降20点，最后可能导致内战发生，请根据形势、战事、国家性格与长期战略权衡利弊。");
			if (!string.IsNullOrWhiteSpace(incoming.PolicyConditionPolicyId))
			{
				sb.AppendLine("附带政策条件：若本国选择comply_ultimatum，"
					+ WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(incoming.PolicyConditionPolicyName, incoming.PolicyConditionPolicyId)
					+ "》将由机制取消。");
			}
		}

		foreach (WorldDiplomacyThreat notice in WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
			threats, author.StringId, null, 4))
		{
			sb.AppendLine("已确认：" + KingdomName(ResolveKingdom(notice.TargetKingdomId)) + "已明确服从本国此前的"
				+ WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
					WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(notice.Stage))
				+ "，后续宣言无需为该威慑宣战或继续升级，也不会因此扣除国家威望。"
				+ (WorldDiplomacyRoundLifecycleRules.IsThreatCancellationStatusCancelled(notice.PolicyConditionCancellationStatus)
					? "附带政策《" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(notice.PolicyConditionPolicyName, notice.PolicyConditionPolicyId) + "》已经取消。"
					: ""));
		}
	}
	private void AppendDiplomaticThreatAnalysisContext(StringBuilder sb, Kingdom author)
	{
		if (sb == null || author == null) return;
		List<WorldDiplomacyThreat> incoming = WorldDiplomacyRoundLifecycleRules.SelectPendingIncomingThreats(
			_storage?.DiplomaticThreats, author.StringId);
		if (incoming.Count == 0) return;
		sb.AppendLine("当前可供语义裁定绑定的未决威慑：");
		foreach (WorldDiplomacyThreat threat in incoming)
		{
			WorldDiplomacyDocument source = ResolveDocument(threat.StageDocumentId);
			sb.AppendLine("- 来源=" + threat.StageDocumentId + "|类型=" + threat.Stage
				+ "|发出国=" + threat.IssuerKingdomId + "=" + KingdomName(ResolveKingdom(threat.IssuerKingdomId))
				+ "|标题=" + WorldDiplomacyTextRules.Limit(source?.Title, 80) + "|要点=" + WorldDiplomacyTextRules.Limit(source?.Body, 260));
		}
		sb.AppendLine("只有玩家正文以本国为主语，明确、完整、无条件服从其中一项威慑时才裁定comply_ultimatum并绑定该来源；这是一次性决定，部分接受、原则接受、附带要求、反条件、沉默、第三国叙述或任何其他意图都立即算不退让。");
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
		sb.AppendLine("本国当前交战=" + (currentWars.Count == 0 ? "[]" : "[" + string.Join(",", currentWars) + "]") + "。此项只陈述战争状态，不授予名单外外交动作。");
		AppendRulerCaptivityDecisionContext(sb, author, null);
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
		string policySnapshot = DiplomacyModuleServices.Policy.BuildSnapshot(authorId);
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
		string recentBattles = WorldDiplomacyTextRules.BuildRecentBilateralBattleContext(
				_storage?.RecentBattles, author?.StringId, target?.StringId, CurrentDay(), RecentBattleRetentionDays,
				id => KingdomName(ResolveKingdom(id)), FormatCampaignDate);
		string nativeReasons = WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext(_storage.NativeSignals, authorId, targetId);
		string targetPolicy = DiplomacyModuleServices.Policy.BuildSnapshot(targetId);
		int relation = GetRulerRelation(author, target);
		int culturalFiefs = CountCulturalClaims(author, target);
		int pressure = WorldDiplomacyWarPressureRules.GetWarPressure(_storage?.WarPressure, authorId, targetId);
		bool peaceTermsVisible = includePeaceNegotiationTerms
			&& !WorldDiplomacyRoundLifecycleRules.IsImmediateWarResponsePeaceSuppressed(round, round?.ResultSettlementCurrentSlotId,
				author?.StringId, target?.StringId, ResolveDocument);

		sb.AppendLine("【对象决策硬事实】" + targetId + "】");
		sb.AppendLine("对象国=" + KingdomName(target) + "（ID=" + targetId + "），统治者=" + RulerName(target));
		int targetPrestige = WorldDiplomacyReputationRules.GetNationalPrestige(_storage?.NationalPrestigeByKingdom, targetId);
		int targetReputation = WorldDiplomacyReputationRules.GetInternationalReputation(_storage?.InternationalReputationByKingdom, targetId);
		sb.AppendLine("对象国国家威望=" + targetPrestige.ToString(CultureInfo.InvariantCulture)
			+ "/100：" + WorldDiplomacyReputationRules.DescribeNationalPrestige(targetPrestige) + "）；外国对该国的公开国际声誉档位="
			+ WorldDiplomacyReputationRules.DescribeInternationalReputation(targetReputation)
			+ "。国家威望低意味着其威胁较不可信，但也可能迫使其为避免进一步失威而采取更冒险的兑现行动；国际声誉只用于判断其承诺可信度、合作条件与外交风险，不代表友好、和平倾向或不可宣战。");
		string reputationConflictOpportunity = target == null ? "" : WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				WorldDiplomacyReputationRules.GetInternationalReputation(_storage?.InternationalReputationByKingdom, target.StringId), legalActions, _storage?.Documents, target.StringId,
				CurrentDay(), RecentNegativeReputationFactRetentionDays, FormatCampaignDate);
		if (!string.IsNullOrWhiteSpace(reputationConflictOpportunity))
		{
			sb.AppendLine(reputationConflictOpportunity);
		}
		if (!string.IsNullOrWhiteSpace(bilateralFamily)) sb.AppendLine(bilateralFamily);
		sb.AppendLine("当前关系=" + BuildBilateralState(author, target)
			+ "；两国贵族整体关系=" + WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile)
			+ "；统治者私人关系=" + WorldDiplomacyTextRules.DescribeRulerRelation(relation)
			+ "；地理关系=" + (border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border) : "不接壤")
			+ "；总体军力=" + WorldDiplomacyTextRules.DescribeStrengthBalance(situation.AuthorStrength, situation.TargetStrength) + "。");
		sb.AppendLine("对象国占有的发文国文化城镇城堡数量=" + culturalFiefs.ToString(CultureInfo.InvariantCulture)
			+ "；边境与政治压力=" + WorldDiplomacyTextRules.DescribeWarPressure(pressure) + "。这些只供王庭判断，不得写成分数或门槛。");
		if (!string.IsNullOrWhiteSpace(targetPolicy))
		{
			sb.AppendLine("对象国政策=" + WorldDiplomacyTextRules.Limit(targetPolicy, 700));
		}
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine("近期原版外交动机素材】");
			sb.AppendLine(WorldDiplomacyTextRules.Limit(nativeReasons, 800));
		}
		sb.AppendLine("近期双边战斗事实。");
		sb.AppendLine(WorldDiplomacyTextRules.Limit(recentBattles, 1500));
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
		if (author == null) return;
		WorldDiplomacyRoundLifecycleRules.AppendRelayResponseSourceContext(
			sb, round, author.StringId, responseSource, requiredSourceDocumentId,
			_storage?.Documents);
	}














	private string BuildCompactRoundPlanCandidateLine(
		Kingdom initiator,
		Kingdom candidate,
		WorldDiplomacyRound round,
		IReadOnlyList<string> legalActions)
	{
		List<string> actions = legalActions as List<string> ?? legalActions?.ToList() ?? new List<string>();
		string line = BuildCompactDiplomaticRelationshipLine(initiator, candidate)
			+ "；可选动作=" + WorldDiplomacyPromptContractRules.DescribePotentialDiplomaticActions(actions);
		string captivityHint = BuildRulerCaptivityTargetHint(initiator, candidate);
		if (!string.IsNullOrWhiteSpace(captivityHint)) line += "\n  " + captivityHint;
		string reputationConflictOpportunity = candidate == null ? "" : WorldDiplomacyTextRules.BuildLowReputationConflictOpportunityContext(
				WorldDiplomacyReputationRules.GetInternationalReputation(_storage?.InternationalReputationByKingdom, candidate.StringId), actions, _storage?.Documents, candidate.StringId,
				CurrentDay(), RecentNegativeReputationFactRetentionDays, FormatCampaignDate);
		return string.IsNullOrWhiteSpace(reputationConflictOpportunity)
			? line
			: line + "\n  " + reputationConflictOpportunity;
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
				sb.AppendLine("  战争态势=" + WorldDiplomacyTextRules.CompactPromptFact(BuildWarDecisionContext(author, other, false), 650));
			}
		}
	}
	private string BuildCompactDiplomaticRelationshipLine(Kingdom initiator, Kingdom candidate)
	{
		if (initiator == null || candidate == null) return "";
		string policy = WorldDiplomacyTextRules.CompactPromptFact(DiplomacyModuleServices.Policy.BuildSnapshot(candidate.StringId), 180);
		StringBuilder sb = new StringBuilder();
		WorldDiplomacyRealmRelationProfile relationProfile = GetRealmRelationProfile(initiator, candidate);
		WorldDiplomacyBorderRelation border = GetKingdomBorderRelation(initiator, candidate);
		WarSituationSnapshot strengthSituation = GetWarSituation(initiator, candidate);
		int candidateReputation = WorldDiplomacyReputationRules.GetInternationalReputation(_storage?.InternationalReputationByKingdom, candidate.StringId);
		sb.Append("- ").Append(candidate.StringId).Append('=').Append(KingdomName(candidate))
			.Append("；与本国=").Append(BuildBilateralState(initiator, candidate))
			.Append("；两国贵族整体关系=").Append(WorldDiplomacyTextRules.DescribeRealmRelationProfile(relationProfile))
			.Append("；统治者私人关系=").Append(WorldDiplomacyTextRules.DescribeRulerRelation(GetRulerRelation(initiator, candidate)))
			.Append("；地理关系=").Append(border.SharesBorder ? WorldDiplomacyTextRules.DescribeBorderRelation(border) : "不接壤")
			.Append("；总体军力=").Append(WorldDiplomacyTextRules.DescribeStrengthBalance(strengthSituation.AuthorStrength, strengthSituation.TargetStrength))
			.Append("；国家威望=").Append(WorldDiplomacyReputationRules.GetNationalPrestige(_storage?.NationalPrestigeByKingdom, candidate.StringId).ToString(CultureInfo.InvariantCulture))
			.Append("；外国对其公开国际声誉档位=").Append(WorldDiplomacyReputationRules.DescribeInternationalReputation(candidateReputation));
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
		sb.AppendLine("【仅供统治者判断的战争态势】战争已" + WorldDiplomacyTextRules.DescribeWarDuration(snapshot.WarDays, DaysPerYear) + "。");
		sb.AppendLine("双方总体军力=" + WorldDiplomacyTextRules.DescribeStrengthBalance(snapshot.AuthorStrength, snapshot.TargetStrength)
			+ "；近期战局=" + WorldDiplomacyTextRules.DescribeWarProgress(snapshot.AuthorProgress, snapshot.TargetProgress)
			+ "；发文国=" + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.AuthorOtherWars)
			+ "；对象国=" + WorldDiplomacyTextRules.DescribeOtherWarBurden(snapshot.TargetOtherWars) + "。这些是综合判断，只能转写成世界内措辞，不得公开任何评分、分差、开放度或战力数值。");
		if (includePeaceNegotiationTerms)
		{
			List<Settlement> targetCanCede = BuildCessionCandidates(target, author, snapshot.TargetCessionScore);
			List<Settlement> authorCanCede = BuildCessionCandidates(author, target, snapshot.AuthorCessionScore);
			sb.AppendLine("【仅在本篇可选和平动作时使用的议和条件】发文国所受议和压力=" + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.AuthorPeacePressure)
				+ "；对象国所受议和压力=" + WorldDiplomacyTextRules.DescribePeacePressure(snapshot.TargetPeacePressure) + "。");
		sb.AppendLine("贡金可与割地并存。参考每日贡金：若发文国付款约" + snapshot.AuthorSuggestedTribute + "，若对象国付款约" + snapshot.TargetSuggestedTribute + "；可以谈判但不得超出任务给出的合法上限。");
		sb.AppendLine("对象国当前可合法提出割让给发文国的领地=" + WorldDiplomacyTextRules.FormatCessionCandidates(ProjectCessionCandidates(targetCanCede)) + "；发文国当前可合法提出割让给对象国的领地=" + WorldDiplomacyTextRules.FormatCessionCandidates(ProjectCessionCandidates(authorCanCede)) + "。清单为空时不得提出或同意割地，也不得编造城名；优先考虑战争中尚未收复的失地。城镇只有在战局严重不利时才会进入清单。");
		}
		return sb.ToString().TrimEnd();
	}

	private static IEnumerable<string> ProjectCessionCandidates(IEnumerable<Settlement> settlements)
	{
		return (settlements ?? Enumerable.Empty<Settlement>())
			.Where(x => x != null)
			.Select(x => (x.StringId ?? "") + "=" + (x.Name?.ToString() ?? "未知"));
	}

	private static string BuildRulerVoiceContext(Kingdom kingdom)
	{
		Hero ruler = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		if (ruler == null)
		{
			return "RulerPersona{name=未知统治者,culture=" + (kingdom?.Culture?.Name?.ToString() ?? "未知") + ",note=没有可用人物档案，不得编造个人经历}";
		}
		MyBehavior.GetNpcPersonaForExternal(ruler, out string personality, out string background);
		string compactPersonality = WorldDiplomacyTextRules.CompactPromptFact(personality, 280);
		string compactBackground = WorldDiplomacyTextRules.CompactPromptFact(background, 420);
		string title = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
			kingdom?.EncyclopediaRulerTitle?.ToString(),
			ruler.Clan?.Name?.ToString(),
			"未知");

		return "RulerPersona{name=" + (ruler.Name?.ToString() ?? "未知")
			+ ",kingdom=" + KingdomName(kingdom)
			+ ",culture=" + (kingdom?.Culture?.Name?.ToString() ?? ruler.Culture?.Name?.ToString() ?? "未知")
			+ ",title=" + WorldDiplomacyTextRules.CompactPromptFact(title, 80)
			+ ",traits=" + BuildRulerVoiceTraitSummary(ruler)
			+ ",personality=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(compactPersonality, "未提供专属个性档案")
			+ ",background=" + WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(compactBackground, "未提供专属背景档案，不得自行补写经历")
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
				+ " 统治者";

				lore = library.BuildLoreContextWithoutPlayerContext(query, ruler, "world_diplomacy_realm_voice", entities);
				string result = WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore);
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
		return WorldDiplomacyPromptContractRules.BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore);
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
		return WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(kingdom?.EncyclopediaRulerTitle?.ToString(), "未知");

	}
	private static string BuildCanonicalRealmGovernmentHardFact(Kingdom kingdom, string rulerTitle)
	{
		string title = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(rulerTitle, "未知统治者");

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
	private static string BuildRulerVoiceTraitSummary(Hero ruler)
	{
		if (ruler == null)
		{
			return "未知";
		}
		try
		{
			List<string> traits = new List<string>();
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Mercy), "仁慈", "冷酷");
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Valor), "勇敢", "谨慎避险");
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Honor), "重视荣誉与承诺", "善用权谋");
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Generosity), "慷慨", "看重积蓄与代价");
			WorldDiplomacyPromptContractRules.AppendVoiceTrait(traits, ruler.GetTraitLevel(DefaultTraits.Calculating), "精于算计", "直率果断");
			return traits.Count == 0 ? "无明显倾向" : string.Join("、", traits);
		}
		catch
		{
			return "读取失败";
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
		parts.Add("统治者私人关系=" + WorldDiplomacyTextRules.DescribeRulerRelation(relation));
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
			_ => "独立"

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
				_ => "完全独立行事"

			};
			relations.Add("- " + subject.StringId + "=" + KingdomName(subject)
				+ "宗主=" + suzerain.StringId + "=" + KingdomName(suzerain) + "；类型="
				+ GetWorldDiplomacyVassalageTypeName(type) + "；权限=" + authority + "。");
		}
		if (relations.Count == 0)
		{
			return "";
		}
		return "【当前宗主—附属关系硬事实】\n"
			+ string.Join("\n", relations)
			+ "\n臣属国在涉及宗主国时必须承认现存宗主关系并保持臣属礼制上的恭敬；这不等于每篇公文都要谄媚或放弃条约仍保留的利益表达。";
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






	private static string BuildRulerCaptivityTargetHint(Kingdom author, Kingdom target)
	{
		Hero ruler = author?.RulingClan?.Leader ?? author?.Leader;
		if (ruler == null || !ruler.IsPrisoner) return "";
		TaleWorlds.CampaignSystem.Party.PartyBase holder = null;
		try { holder = ruler.PartyBelongedToAsPrisoner; } catch { }
		Kingdom holderKingdom = null;
		try { holderKingdom = holder?.MapFaction as Kingdom; } catch { }
		if (holderKingdom != null && !holderKingdom.IsEliminated && holderKingdom == target)
			return "君主被当前对象国关押或控制：本国应更重视停战、和平和可执行的让步，但仍不得绕过当前合法动作。";
		if (holderKingdom != null && !holderKingdom.IsEliminated)
			return "君主被其他王国关押或控制：本国整体处境恶化，应更重视稳定与谈判，不得把当前对象误认作关押方。";
		return "君主被俘但关押或控制方未知：本国处境恶化，应更重视稳定与谈判，不得猜测关押方。";
	}

	private static void AppendRulerCaptivityDecisionContext(StringBuilder sb, Kingdom author, Kingdom currentTarget)
	{
		if (sb == null || author == null) return;
		Hero ruler = author.RulingClan?.Leader ?? author.Leader;
		if (ruler == null || !ruler.IsPrisoner) return;
		TaleWorlds.CampaignSystem.Party.PartyBase holder = null;
		try { holder = ruler.PartyBelongedToAsPrisoner; } catch { }
		Kingdom holderKingdom = null;
		try { holderKingdom = holder?.MapFaction as Kingdom; } catch { }
		bool holderKnown = holderKingdom != null && !holderKingdom.IsEliminated;
		bool currentTargetIsHolder = holderKnown && currentTarget != null && holderKingdom == currentTarget;
		string pressure = currentTarget == null
			? "需结合具体外交对象判断"
			: currentTargetIsHolder ? "是" : holderKnown ? "否" : "未知";
		sb.AppendLine("【本国君主当前处境】");
		sb.AppendLine("本国统治者被俘：是；当前关押/控制方="
			+ (holderKnown ? KingdomName(holderKingdom) + "（ID=" + holderKingdom.StringId + "）" : "未知")
			+ "；针对本篇外交对象的被俘压力=" + pressure + "。");
		if (currentTarget == null)
		{
			sb.AppendLine("君主被俘会提高本国对稳定、停战与谈判的重视程度；若本篇对象正是当前关押或控制方，则进一步提高对让步和妥协的重视。不得猜测未知关押方，也不得绕过当前合法动作。");
		}
		else if (currentTargetIsHolder)
		{
			sb.AppendLine("君主被当前外交对象关押或控制。本国处于明显不利处境，应更重视停战、和平、让步和避免战争扩大；可以接受比平时更不利但仍可执行的条件。不得因此无条件接受不存在的提议、非法条款或绕过当前合法动作。");
		}
		else if (holderKnown)
		{
			sb.AppendLine("本国君主被其他王国关押或控制。本国整体处境恶化，应减少无意义的外交升级，更重视稳定与谈判；不得因此自动接受当前对象的条件，也不得把当前对象自动认定为关押方。");
		}
		else
		{
			sb.AppendLine("本国君主被俘但当前关押/控制方无法可靠确认。本国处境恶化，应更重视稳定与谈判；不得猜测关押方，也不得把当前对象自动认定为关押方。");
		}
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
		if (issuer == null || target == null || issuer == target
			|| issuer.IsEliminated || target.IsEliminated
			|| !HasIndependentWorldDiplomacyAuthority(issuer)
			|| !HasIndependentWorldDiplomacyAuthority(target))
		{
			return "threat_party_no_longer_eligible";
		}
		if (FactionManager.IsAtWarAgainstFaction(issuer, target))
		{
			return "war_already_started_outside_pending_declaration";
		}
		if (alliance?.IsAllyWithKingdom(issuer, target) == true)
		{
			return "threat_parties_became_allies";
		}
		return null;
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































	private void TrimRecentBattleFacts()
	{
		_storage.RecentBattles = WorldDiplomacyRoundLifecycleRules.TrimRecentBattleFacts(
			_storage?.RecentBattles, CurrentDay(), RecentBattleRetentionDays);
	}

	private void TrimNativeSignals()
	{
		_storage.NativeSignals = WorldDiplomacyRoundLifecycleRules.TrimNativeSignals(
			_storage?.NativeSignals, CurrentDay() - DaysPerYear * 2);
	}

	private void RemoveJob(string jobId)
	{
		_storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobId(x, jobId));
	}
	private WorldDiplomacyDocument ResolveDocument(string documentId)
	{
		return WorldDiplomacyRoundLifecycleRules.ResolveDocument(_storage?.Documents, documentId);
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
		return WorldDiplomacyWarPressureRules.EnsureWarLedger(_storage?.ActiveWarLedgers, first.StringId, second.StringId, CurrentDay());
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
		float duration = WorldDiplomacyRoundLifecycleRules.Clamp01((snapshot.WarDays - 7f) / 112f) * 70f;
		float setback = WorldDiplomacyRoundLifecycleRules.Clamp01((enemyProgress - ownProgress) / 500f) * 70f;
		float strength = WorldDiplomacyRoundLifecycleRules.Clamp01((enemyStrength / Math.Max(1f, ownStrength) - 1f) / 1.5f) * 40f;
		float casualtyBurden = WorldDiplomacyRoundLifecycleRules.Clamp01(suffered / Math.Max(500f, ownStrength * 1.5f)) * 40f;
		float casualtyImbalance = WorldDiplomacyRoundLifecycleRules.Clamp01((suffered - inflicted) / Math.Max(500f, ownStrength)) * 20f;
		float multiWar = WorldDiplomacyRoundLifecycleRules.Clamp01(otherWars / 2f) * 30f;
		float territory = WorldDiplomacyRoundLifecycleRules.Clamp01(lostFiefs / 2f) * 30f;
		return Math.Max(0f, Math.Min(300f, duration + setback + strength + casualtyBurden + casualtyImbalance + multiWar + territory));
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
		return Settlement.All.FirstOrDefault(x => x != null
			&& string.Equals(x.StringId, settlementId, StringComparison.OrdinalIgnoreCase));
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
		return "双方处于和平状态。";
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
		return true;
	}
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
				+ "天";

		}
		catch
		{
			return "第" + Math.Max(0, day).ToString(CultureInfo.InvariantCulture) + "天";
		}
	}
	private static string NewId(string prefix)
	{
		return (prefix ?? "world_diplomacy") + ":" + Guid.NewGuid().ToString("N");
	}
	private static string KingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
	}
	private static string ResolveKingdomNameOrEmpty(string kingdomId)
	{
		Kingdom kingdom = ResolveKingdom(kingdomId);
		return kingdom == null ? "" : KingdomName(kingdom);
	}
	private static string RulerName(Kingdom kingdom)
	{
		return kingdom?.RulingClan?.Leader?.Name?.ToString() ?? "未知统治者";
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
