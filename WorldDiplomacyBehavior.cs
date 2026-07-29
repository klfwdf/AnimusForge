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
	private const int CompressionMaxTokens = 1800;
	private const int MaxStoredDocuments = 420;
	private const int MaxStoredAnnualSummaries = 24;
	private const int MaxStoredCompressionSummaries = 24;
	private const int MaxStoredRoundSummaries = 96;
	private const int CompressionRetryCooldownHours = 12;
	private const int MaxPendingJobs = 24;
	private const int NativeWarSignalBase = 24;
	private const int NativeOtherSignalBase = 42;
	private const int FixedMaxConcurrentOffensiveWars = 2;
	private const int FailedServiceCooldownHours = 12;
	private const int ForcedPeaceProposalCooldownDays = 14;
	private const float DiplomaticAdvanceReleaseRatio = 0.7f;
	private const float CessionCastleUnlockThreshold = 90f;
	private const float CessionTownUnlockThreshold = 95f;
	private const int MaxPeaceCessionCandidates = 5;
	private const int RecentBattleRetentionDays = 21;
	private const int MaxStoredRecentBattles = 96;
	private const int MaxPromptRecentBattles = 5;
	private const int MaxPropagationArrivalsPerDay = 1200;
	private const int MaxParticipationCandidatesPerJob = 8;
	private const int MaxAiDocumentsStartedPerDay = 8;
	private const int MaxDiplomacyLlmRequestsPerDay = 12;
	private const int MaxAutomaticDocumentsPerRound = 12;
	private const int MaxAutomaticReplyDepth = 2;
	private const int MaxPriorityPlayerResponsesPerDocument = 3;
	private const int ParticipationObserverCooldownDays = 7;
	private const int MaxPendingSpeeches = 24;
	private const int RoundInactivityDays = 7;
	private const int MaxKnownDocumentsPerLocation = 64;
	private const int MaxPendingPolicySignals = 24;
	private const int MaxProcessedPolicySignalKeys = 256;
	private const int PolicySignalRetentionDays = 21;
	private const int RelaySchemaVersion = 12;
	private const string RelayCacheAffinityKey = "diplomacy-relay:v12";
	private const string DiplomacySystemContractMarker = "【AnimusForge 王国外交共同契约 v11】";
	private const int RelayPassDurationDays = 7;
	private const int RelayTargetDurationDays = 21;
	private const int RelayHardDurationDays = 24;
	private const int RepeatedPairCooldownDays = 21;
	private const int MaxRelayParticipants = 12;
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
	private readonly Dictionary<string, WeeklyDiplomacySnapshotCacheEntry> _weeklyDiplomacySnapshotCache = new Dictionary<string, WeeklyDiplomacySnapshotCacheEntry>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _realmInstitutionalVoiceCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private long _realmInstitutionalVoiceRuleVersion = -1L;

	private WorldDiplomacyStorage _storage = new WorldDiplomacyStorage();
	private bool _llmRequestRunning;
	private string _activeJobId = "";
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
		InitializeSchedule();
		ResetTransientRuntime("new-game");
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		NormalizeStorage();
		InitializeSchedule();
		ResetTransientRuntime("game-loaded");
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		NormalizeStorage();
		InitializeSchedule();
		ResetTransientRuntime("session-launched");
	}

	private void OnCampaignTick(float dt)
	{
		if (!IsWorldDiplomacyEnabled())
		{
			HandleDisabledState();
			return;
		}
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
			TryScheduleArmedEscalationOpportunity();
			TrySchedulePeacePressureOpportunity();
			TryScheduleNormalRound();
		}
	}

	private void OnDailyTick()
	{
		NormalizeStorage();
		RefreshRoundIntervalScheduleIfNeeded();
		_warSituationCache.Clear();
		_courtSettlementCache.Clear();
		ResetDailyGenerationBudget();
		RecalculatePendingPropagationIfNeeded();
		_lastSchedulerDay = CurrentDay();
		EnsureActiveWarLedgersAndRemoveEndedWars();
		TrimRecentBattleFacts();
		if (!IsWorldDiplomacyEnabled())
		{
			return;
		}
		RemoveQueuedNativeDiplomacyDecisions();
		_nativeDiplomacyDecisionQueueSanitized = true;
		DecayWarPressure();
		RefreshPolicyDiplomacySignals();
		ProcessPropagationArrivals();
		ProcessRelayArrivals();
		ProcessRoundLifecycle();
		TryScheduleTokenCompression();
		TrySchedulePolicyTriggeredRound();
		TryScheduleArmedEscalationOpportunity();
		TrySchedulePeacePressureOpportunity();
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
		while (_completedJobs.TryDequeue(out _))
		{
		}
		_notifiedDocumentIdsThisSession.Clear();
		_registeredMapNotificationView = null;
		_warSituationCache.Clear();
		_weeklyDiplomacySnapshotCache.Clear();
		_realmInstitutionalVoiceCache.Clear();
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
		foreach (WorldDiplomacyJob job in _storage.Jobs)
		{
			if (job != null)
			{
				job.IsRunning = false;
			}
		}
		Log("runtime reset reason=" + reason);
	}

	private void HandleDisabledState()
	{
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
		_llmRequestRunning = false;
		_activeJobId = "";
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
		Kingdom target = author == affectedRepresentative ? issuerRepresentative : affectedRepresentative;
		WorldDiplomacyRound round = EnsureActiveRound(author, target, isPlayerInsertion: false);
		AttachPolicySignalToRound(round, signal, issuer, affected);
		ScheduleNextNormalRoundAfter(CurrentDay());
		EnqueueGenerationJob(author, target, null, isResponse: false, forcedIntent: "", sourceDocument: null,
			priority: 70, roundId: round?.RoundId, allowUntargeted: false);
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
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdom.StringId, "active", mandatoryReply: !IsPlayerKingdom(kingdom));
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
			participant.SelectedForRelay = true;
			AddParticipantToRelayRouteIfNeeded(round, kingdom.StringId);
		}
	}

	private static string BuildPolicySignalContext(WorldDiplomacyPolicySignal signal)
	{
		return "【本回合的公开政策事件】\n"
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
		Kingdom target = SelectTargetKingdom(initiator);
		if (target == null)
		{
			ScheduleNextNormalRoundAfter(day);
			return;
		}
		if (!TryConsumeAiDocumentBudget())
		{
			return;
		}
		WorldDiplomacyRound round = EnsureActiveRound(initiator, target, isPlayerInsertion: false);
		EnqueueGenerationJob(initiator, target, null, isResponse: false, forcedIntent: ResolveArmedIntent(initiator, target), sourceDocument: null, priority: 20, roundId: round?.RoundId, allowUntargeted: true);
	}

	private void TryScheduleArmedEscalationOpportunity()
	{
		if (!IsDiplomaticSituationAutoAdvanceEnabled() || _storage.Jobs.Count > 0 || _llmRequestRunning)
		{
			return;
		}
		int releaseThreshold = GetDiplomaticAdvanceReleaseThreshold();
		List<WarPressureEntry> candidates = _storage.WarPressure
			.Where(x => x != null && x.IsEscalationArmed && x.Value >= releaseThreshold)
			.OrderByDescending(x => x.Value)
			.ThenBy(x => x.LastUpdatedDay)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}
		WorldDiplomacyRound round = _storage.ActiveRound;
		if (round == null && CurrentDay() < _storage.NextNormalRoundDay)
		{
			return;
		}
		WarPressureEntry candidate = null;
		Kingdom initiator = null;
		Kingdom target = null;
		foreach (WarPressureEntry item in candidates)
		{
			Kingdom candidateInitiator = ResolveKingdom(item.SourceKingdomId);
			Kingdom candidateTarget = ResolveKingdom(item.TargetKingdomId);
			if (round != null && !RoundContainsKingdom(round, candidateInitiator?.StringId) && !RoundContainsKingdom(round, candidateTarget?.StringId)) continue;
			if (!CanDeclareWar(candidateInitiator, candidateTarget, forcedByThreshold: true, out string blockReason))
			{
				item.LastBlockReason = blockReason;
				continue;
			}
			candidate = item;
			initiator = candidateInitiator;
			target = candidateTarget;
			break;
		}
		if (candidate == null)
		{
			return;
		}
		if (!TryConsumeAiDocumentBudget())
		{
			return;
		}
		round ??= EnsureActiveRound(initiator, target, isPlayerInsertion: false);
		EnsureRoundParticipant(round, initiator.StringId, "active", mandatoryReply: false);
		EnsureRoundParticipant(round, target.StringId, "active", mandatoryReply: true);
		EnqueueGenerationJob(initiator, target, null, isResponse: false, forcedIntent: "declare_war", sourceDocument: null, priority: 90, roundId: round?.RoundId);
	}

	private void TrySchedulePeacePressureOpportunity()
	{
		if (!IsDiplomaticSituationAutoAdvanceEnabled() || _storage.Jobs.Count > 0 || _llmRequestRunning)
		{
			return;
		}
		WorldDiplomacyRound activeRound = _storage.ActiveRound;
		if (activeRound != null)
		{
			// The active relay prompt already carries the live peace-pressure requirement.
			// Do not start a parallel full-prompt generation that would fragment the cache chain.
			return;
		}
		if (CurrentDay() < _storage.NextNormalRoundDay)
		{
			return;
		}
		int threshold = GetDiplomaticAdvanceThreshold();
		int day = CurrentDay();
		WorldDiplomacyWarLedger selectedLedger = null;
		Kingdom selectedAuthor = null;
		Kingdom selectedTarget = null;
		float selectedPressure = float.MinValue;
		foreach (WorldDiplomacyWarLedger ledger in _storage.ActiveWarLedgers.Where(x => x != null))
		{
			Kingdom first = ResolveKingdom(ledger.FirstKingdomId);
			Kingdom second = ResolveKingdom(ledger.SecondKingdomId);
			if (first == null || second == null || first == second || !FactionManager.IsAtWarAgainstFaction(first, second))
			{
				continue;
			}
			foreach ((Kingdom Author, Kingdom Target) direction in new[] { (first, second), (second, first) })
			{
				if (direction.Author == null || direction.Target == null || direction.Author.IsEliminated
					|| IsPlayerKingdom(direction.Author)
					|| !HasIndependentWorldDiplomacyAuthority(direction.Author)
					|| !HasIndependentWorldDiplomacyAuthority(direction.Target))
				{
					continue;
				}
				int lastProposalDay = GetLastForcedPeaceProposalDay(ledger, direction.Author.StringId);
				if (lastProposalDay > 0 && day - lastProposalDay < ForcedPeaceProposalCooldownDays)
				{
					continue;
				}
				if (HasOpenPeaceOffer(direction.Author.StringId, direction.Target.StringId))
				{
					continue;
				}
				WarSituationSnapshot snapshot = GetWarSituation(direction.Author, direction.Target);
				float pressure = snapshot?.AuthorPeacePressure ?? 0f;
				if (pressure < threshold || pressure <= selectedPressure)
				{
					continue;
				}
				selectedLedger = ledger;
				selectedAuthor = direction.Author;
				selectedTarget = direction.Target;
				selectedPressure = pressure;
			}
		}
		if (selectedLedger == null || selectedAuthor == null || selectedTarget == null || !TryConsumeAiDocumentBudget())
		{
			return;
		}
		WorldDiplomacyRound round = EnsureActiveRound(selectedAuthor, selectedTarget, isPlayerInsertion: false);
		EnsureRoundParticipant(round, selectedAuthor.StringId, "active", mandatoryReply: false);
		EnsureRoundParticipant(round, selectedTarget.StringId, IsPlayerKingdom(selectedTarget) ? "observer" : "active", mandatoryReply: false);
		SetLastForcedPeaceProposalDay(selectedLedger, selectedAuthor.StringId, day);
		EnqueueGenerationJob(selectedAuthor, selectedTarget, null, isResponse: false, forcedIntent: "propose_peace",
			sourceDocument: null, priority: 88, roundId: round?.RoundId);
		Log("peace pressure proposal queued author=" + selectedAuthor.StringId
			+ " target=" + selectedTarget.StringId
			+ " pressure=" + Math.Round(selectedPressure).ToString(CultureInfo.InvariantCulture)
			+ " threshold=" + threshold.ToString(CultureInfo.InvariantCulture));
	}

	private void EnqueueGenerationJob(
		Kingdom author,
		Kingdom target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		string forcedIntent,
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
			if (!playerPriorityResponse) owningRound.AutomaticDocumentsStarted++;
		}
		string systemPrompt = isRelayTurn ? BuildRelayGenerationSystemPrompt() : BuildGenerationSystemPrompt();
		if (isRelayTurn && owningRound?.LlmTranscript?.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase)) is WorldDiplomacyLlmMessage frozenSystem
			&& !string.IsNullOrWhiteSpace(frozenSystem.Content))
		{
			if (frozenSystem.Content.StartsWith(DiplomacySystemContractMarker, StringComparison.Ordinal))
			{
				systemPrompt = frozenSystem.Content;
			}
			else
			{
				frozenSystem.Content = systemPrompt;
				Log("relay system contract upgraded round=" + (owningRound.RoundId ?? "") + " schema=" + RelaySchemaVersion.ToString(CultureInfo.InvariantCulture));
			}
		}
		bool actorProfileAlreadyInTranscript = owningRound?.LlmProfiledKingdomIds?.Any(x => string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase)) == true;
		bool includeEmbeddedRoundPlan = !isRelayTurn && !isResponse && owningRound != null && string.IsNullOrWhiteSpace(owningRound.RootDocumentId);
		List<string> roundPlanCandidates = includeEmbeddedRoundPlan
			? Kingdom.All.Where(x => x != null && !x.IsEliminated && x != author && HasIndependentWorldDiplomacyAuthority(x)).OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase).Select(x => x.StringId).ToList()
			: new List<string>();
		string userPrompt = isRelayTurn
			? BuildRelayConversationTurnPrompt(owningRound, author, target, includeActorProfile: !actorProfileAlreadyInTranscript,
				prioritySource: sourceDocument, priorityResponseOnly: externalResponseOnly)
			: BuildGenerationPrompt(author, target, exchange, isResponse, forcedIntent, sourceDocument, isReminder, roundId, allowUntargeted, roundPlanCandidates);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_generate"),
			Kind = "generate",
			Priority = priority,
			CreatedDay = scheduledDay >= 0 ? scheduledDay : CurrentDay(),
			ExchangeId = exchange?.ExchangeId ?? roundId ?? "",
			RoundId = FirstNonEmpty(roundId, exchange?.ExchangeId),
			AuthorKingdomId = author.StringId,
			TargetKingdomId = target.StringId,
			SourceDocumentId = sourceDocument?.DocumentId ?? "",
			IsResponse = isResponse,
			ForcedIntent = NormalizeIntent(forcedIntent),
			IsExternalResponseOnly = externalResponseOnly,
			IsReminder = isReminder,
			IsRelayTurn = isRelayTurn,
			AllowUntargeted = allowUntargeted,
			PreviousKingdomId = previousKingdomId ?? "",
			CandidateKingdomIds = roundPlanCandidates,
			WasAtWarWhenQueued = target != null && FactionManager.IsAtWarAgainstFaction(author, target),
			SystemPrompt = systemPrompt,
			UserPrompt = userPrompt,
			CacheAffinityKey = isRelayTurn ? RelayCacheAffinityKey : "generate:" + (author.StringId ?? ""),
			ProfiledKingdomId = isRelayTurn && !actorProfileAlreadyInTranscript ? author.StringId ?? "" : "",
			MaxTokens = GenerationMaxTokens
		};
		if (isRelayTurn && owningRound?.LlmTranscript?.Count > 0)
		{
			job.LlmMessages = CloneLlmMessages(owningRound.LlmTranscript);
			job.LlmMessages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = userPrompt });
		}
		EnqueueJob(job);
	}

	private void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority)
	{
		if (document == null)
		{
			return;
		}
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
			SystemPrompt = BuildAnalysisSystemPrompt(),
			UserPrompt = BuildAnalysisPrompt(document),
			CacheAffinityKey = "analyze",
			MaxTokens = AnalysisMaxTokens
		};
		EnqueueJob(job);
	}

	private void EnqueueCompressionJob(List<WorldDiplomacyRoundSummary> summaries, long tokenCount)
	{
		if (summaries == null || summaries.Count == 0) return;
		int batchSequence = Math.Max(0, _storage.CompressionSequence) + 1;
		string batchId = "diplomacy_compaction_" + batchSequence.ToString(CultureInfo.InvariantCulture);
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_compress"),
			Kind = "compress",
			Priority = 1,
			CreatedDay = CurrentDay(),
			CompressionBatchId = batchId,
			CompressionRoundIds = summaries.Select(x => x.RoundId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
			CompressionTokenCount = Math.Max(0L, tokenCount),
			SystemPrompt = BuildTokenCompressionSystemPrompt(),
			UserPrompt = BuildTokenCompressionPrompt(batchId, summaries, tokenCount),
			CacheAffinityKey = "compress:token-cycle:v1",
			MaxTokens = CompressionMaxTokens
		};
		EnqueueJob(job);
		Log("token compression queued batch=" + batchId
			+ " rounds=" + summaries.Count.ToString(CultureInfo.InvariantCulture)
			+ " accumulated=" + tokenCount.ToString(CultureInfo.InvariantCulture));
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
		_storage.Jobs = _storage.Jobs
			.Where(x => x != null)
			.OrderByDescending(x => x.Priority)
			.ThenBy(x => x.CreatedDay)
			.ThenBy(x => x.JobId, StringComparer.OrdinalIgnoreCase)
			.Take(MaxPendingJobs)
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

	private static void LogPromptCacheShape(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> messages = job?.LlmMessages?.Count > 0
			? job.LlmMessages
			: new List<WorldDiplomacyLlmMessage>
			{
				new WorldDiplomacyLlmMessage { Role = "system", Content = job?.SystemPrompt ?? "" },
				new WorldDiplomacyLlmMessage { Role = "user", Content = job?.UserPrompt ?? "" }
			};
		string system = messages.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string user = messages.LastOrDefault(x => x != null && string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content ?? "";
		string foundation = BuildCommonDiplomacySystemPrefix();
		int userPrefix1024Chars = Math.Min(1024, user.Length);
		int userPrefixChars = Math.Min(2048, user.Length);
		int totalChars = messages.Sum(x => x?.Content?.Length ?? 0);
		int expectedCachedMessageCount = 0;
		for (int index = messages.Count - 1; index >= 0; index--)
		{
			if (!string.Equals(messages[index]?.Role, "assistant", StringComparison.OrdinalIgnoreCase)) continue;
			expectedCachedMessageCount = index + 1;
			break;
		}
		int expectedCachedPrefixChars = messages.Take(expectedCachedMessageCount).Sum(x => x?.Content?.Length ?? 0);
		string expectedCachedPrefixShape = string.Join("\n", messages.Take(expectedCachedMessageCount).Select(x => (x?.Role ?? "") + ":" + (x?.Content ?? "")));
		Log("cache-shape kind=" + (job?.Kind ?? "")
			+ " affinity=" + ResolveCacheAffinityKey(job)
			+ " messages=" + messages.Count.ToString(CultureInfo.InvariantCulture)
			+ " totalChars=" + totalChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedMessages=" + expectedCachedMessageCount.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixChars=" + expectedCachedPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " expectedCachedPrefixHash=" + StablePromptHash(expectedCachedPrefixShape)
			+ " foundationChars=" + foundation.Length.ToString(CultureInfo.InvariantCulture)
			+ " foundationHash=" + StablePromptHash(foundation)
			+ " foundationAtTop=" + system.StartsWith(foundation, StringComparison.Ordinal).ToString()
			+ " systemChars=" + system.Length.ToString(CultureInfo.InvariantCulture)
			+ " systemHash=" + StablePromptHash(system)
			+ " userChars=" + user.Length.ToString(CultureInfo.InvariantCulture)
			+ " userPrefix1024Hash=" + StablePromptHash(userPrefix1024Chars <= 0 ? "" : user.Substring(0, userPrefix1024Chars))
			+ " userPrefixChars=" + userPrefixChars.ToString(CultureInfo.InvariantCulture)
			+ " userPrefixHash=" + StablePromptHash(userPrefixChars <= 0 ? "" : user.Substring(0, userPrefixChars)));
	}

	private void LogPromptCacheUsage(WorldDiplomacyJob job, LlmJobResult result)
	{
		int hit = Math.Max(0, result?.PromptCacheHitTokens ?? 0);
		int miss = Math.Max(0, result?.PromptCacheMissTokens ?? 0);
		int denominator = hit + miss;
		string rate = denominator <= 0 ? "n/a" : (100d * hit / denominator).ToString("F1", CultureInfo.InvariantCulture) + "%";
		Log("cache-usage kind=" + (job?.Kind ?? "")
			+ " affinity=" + ResolveCacheAffinityKey(job)
			+ " prompt_tokens=" + (result?.PromptTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " completion_tokens=" + (result?.CompletionTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_cache_hit_tokens=" + (result?.PromptCacheHitTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " prompt_cache_miss_tokens=" + (result?.PromptCacheMissTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " hit_rate=" + rate);
		_cacheHitTokensThisSession += hit;
		_cacheMissTokensThisSession += miss;
		if (string.Equals(ResolveCacheAffinityKey(job), RelayCacheAffinityKey, StringComparison.OrdinalIgnoreCase))
		{
			_relayCacheHitTokensThisSession += hit;
			_relayCacheMissTokensThisSession += miss;
		}
		long overall = _cacheHitTokensThisSession + _cacheMissTokensThisSession;
		long relay = _relayCacheHitTokensThisSession + _relayCacheMissTokensThisSession;
		Log("cache-session overall_hit_rate=" + (overall <= 0 ? "n/a" : (100d * _cacheHitTokensThisSession / overall).ToString("F1", CultureInfo.InvariantCulture) + "%")
			+ " relay_hit_rate=" + (relay <= 0 ? "n/a" : (100d * _relayCacheHitTokensThisSession / relay).ToString("F1", CultureInfo.InvariantCulture) + "%")
			+ " target=70.0%");
	}

	private void RecordDiplomacyTokenUsage(WorldDiplomacyJob job, LlmJobResult result)
	{
		if (job == null || result == null || string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase)) return;
		long prompt = result.PromptTokens ?? 0;
		if (prompt <= 0)
		{
			prompt = Math.Max(0, result.PromptCacheHitTokens ?? 0) + Math.Max(0, result.PromptCacheMissTokens ?? 0);
		}
		if (prompt <= 0)
		{
			IEnumerable<object> messages = BuildLlmMessageArray(job).Children().Cast<object>();
			prompt = Logger.EstimateTokensFromMessages(messages);
		}
		long completion = result.CompletionTokens ?? 0;
		if (completion <= 0 && !string.IsNullOrWhiteSpace(result.Content)) completion = Logger.EstimateTokens(result.Content);
		long added = Math.Max(0, prompt) + Math.Max(0, completion);
		if (added <= 0) return;
		_storage.DiplomacyTokensSinceCompression = Math.Max(0L, _storage.DiplomacyTokensSinceCompression) + added;
		long threshold = GetCompressionThresholdTokens();
		if (_storage.DiplomacyTokensSinceCompression >= threshold) _storage.DiplomacyCompressionPending = true;
		Log("compression-meter added=" + added.ToString(CultureInfo.InvariantCulture)
			+ " accumulated=" + _storage.DiplomacyTokensSinceCompression.ToString(CultureInfo.InvariantCulture)
			+ " threshold=" + threshold.ToString(CultureInfo.InvariantCulture)
			+ " pending=" + _storage.DiplomacyCompressionPending.ToString());
	}

	private static string StablePromptHash(string text)
	{
		unchecked
		{
			ulong hash = 1469598103934665603UL;
			foreach (char ch in text ?? "")
			{
				hash ^= ch;
				hash *= 1099511628211UL;
			}
			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	private static List<WorldDiplomacyLlmMessage> CloneLlmMessages(IEnumerable<WorldDiplomacyLlmMessage> messages)
	{
		return (messages ?? Enumerable.Empty<WorldDiplomacyLlmMessage>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Role))
			.Select(x => new WorldDiplomacyLlmMessage { Role = x.Role, Content = x.Content ?? "" })
			.ToList();
	}

	private static JArray BuildLlmMessageArray(WorldDiplomacyJob job)
	{
		List<WorldDiplomacyLlmMessage> source = job?.LlmMessages?.Count > 0
			? job.LlmMessages
			: new List<WorldDiplomacyLlmMessage>
			{
				new WorldDiplomacyLlmMessage { Role = "system", Content = job?.SystemPrompt ?? "" },
				new WorldDiplomacyLlmMessage { Role = "user", Content = job?.UserPrompt ?? "" }
			};
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
		if (_llmRequestRunning || _storage.Jobs.Count == 0)
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
		job.IsRunning = true;
		job.CacheAffinityKey = ResolveCacheAffinityKey(job);
		_lastLlmCacheAffinityKey = job.CacheAffinityKey;
		LogPromptCacheShape(job);
		_llmRequestRunning = true;
		_activeJobId = job.JobId;
		long generation = _runtimeGeneration;
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
					BuildLlmMessageArray(job),
					Math.Max(256, job.MaxTokens),
					DefaultApiTimeoutMilliseconds,
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
			}
			catch (Exception ex)
			{
				result.Error = ex.ToString();
				result.IsServiceFailure = true;
			}
			_completedJobs.Enqueue(result);
		});
	}

	private void ProcessCompletedJobs()
	{
		while (_completedJobs.TryDequeue(out LlmJobResult result))
		{
			_llmRequestRunning = false;
			_activeJobId = "";
			WorldDiplomacyJob job = _storage.Jobs.FirstOrDefault(x => x != null && string.Equals(x.JobId, result.JobId, StringComparison.OrdinalIgnoreCase));
			if (job == null)
			{
				continue;
			}
			job.IsRunning = false;
			LogPromptCacheUsage(job, result);
			RecordDiplomacyTokenUsage(job, result);
			if (result.RuntimeGeneration != _runtimeGeneration || SaveRuntimeGuard.IsStale(result.RuntimeGeneration, "world_diplomacy_commit"))
			{
				RemoveJob(job.JobId);
				continue;
			}
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
				else if (string.Equals(job.Kind, "participate", StringComparison.OrdinalIgnoreCase))
				{
					CommitParticipation(job, result.Content);
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
			CommitGeneratedDocument(job, BuildFallbackGenerationJson(job));
		}
		else if (string.Equals(job.Kind, "analyze", StringComparison.OrdinalIgnoreCase))
		{
			CommitAnalysis(job, BuildFallbackAnalysisJson(job));
		}
		else if (string.Equals(job.Kind, "compress", StringComparison.OrdinalIgnoreCase))
		{
			_storage.DiplomacyCompressionPending = true;
			_storage.CompressionRetryAfterHour = CurrentHour() + CompressionRetryCooldownHours;
			Log("token compression retained for retry batch=" + (job.CompressionBatchId ?? "")
				+ " retry_after_hour=" + _storage.CompressionRetryAfterHour.ToString(CultureInfo.InvariantCulture));
		}
		else if (string.Equals(job.Kind, "participate", StringComparison.OrdinalIgnoreCase))
		{
			CommitParticipation(job, "{\"decisions\":[]}");
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

	private void CommitGeneratedDocument(WorldDiplomacyJob job, string raw)
	{
		Kingdom author = ResolveKingdom(job.AuthorKingdomId);
		Kingdom target = ResolveKingdom(job.TargetKingdomId);
		if (author == null || target == null)
		{
			CompleteExchange(job.ExchangeId, "generated_party_missing");
			return;
		}
		JObject json = ParseJsonObject(raw);
		if (TryGetGeneratedIntentLegalityViolation(job, json, author, target, out Kingdom generatedTarget, out string legalityReason))
		{
			Log("generated declaration rejected before publication job=" + job.JobId
				+ " intent=" + NormalizeIntent(ReadString(json, "author_intent.intent", "intent"))
				+ " author=" + author.StringId
				+ " target=" + (generatedTarget?.StringId ?? target.StringId)
				+ " reason=" + legalityReason);
			if (job.SemanticRepairAttempts < 1)
			{
				EnqueueGeneratedDeclarationRepair(job, raw, author, generatedTarget ?? target, legalityReason);
				return;
			}
			if (string.IsNullOrWhiteSpace(job.ForcedIntent)
				|| string.Equals(legalityReason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(legalityReason, "peace_intent_has_no_valid_target", StringComparison.OrdinalIgnoreCase))
			{
				AbandonRejectedGeneration(job, author, generatedTarget ?? target, legalityReason);
				return;
			}
			raw = BuildRejectedDeclarationFallbackGenerationJson(job, author, generatedTarget ?? target, legalityReason);
			json = ParseJsonObject(raw);
		}
		WorldDiplomacyDocument sourceDocument = ResolveDocument(job.SourceDocumentId);
		string title = FirstNonEmpty(
			ReadString(json, "title"),
			job.IsResponse ? "外交回应" : "王国外交宣言");
		string body = NormalizeBody(ReadString(json, "body", "public_document", "document"));
		if (string.IsNullOrWhiteSpace(body))
		{
			body = BuildFallbackDocumentBody(author, target, job.ForcedIntent, job.IsResponse, ResolveDocument(job.SourceDocumentId));
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
		document.PeaceTerms = ParseAndValidatePeaceTerms(json, author, target);
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
		document.HasEmbeddedRoundPlan = (job.CandidateKingdomIds?.Count ?? 0) > 0;
		if (document.HasEmbeddedRoundPlan)
		{
			document.PlannedRoundTopic = Limit(FirstNonEmpty(ReadString(json, "round_plan.topic"), title), 120);
			document.PlannedKingdomIds = ReadStringList(json, "round_plan.selected_kingdom_ids")
				.Where(x => job.CandidateKingdomIds.Contains(x, StringComparer.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		document.AutomaticReplyDepth = job.IsResponse ? Math.Max(1, (sourceDocument?.AutomaticReplyDepth ?? 0) + 1) : 0;
		if (!string.IsNullOrWhiteSpace(job.ForcedIntent))
		{
			document.Intent = NormalizeIntent(job.ForcedIntent);
			document.Commitment = "binding";
			document.AnalysisStatus = "forced";
			document.AddressedKingdomIds = NormalizeKingdomIdList(new[] { document.TargetKingdomId }, document.AuthorKingdomId);
			document.RequiresResponse = ResolveValidatedResponseObligation(document, document.Intent, modelRequestedResponse: true);
		}
		AddDocument(document);
		RecordGeneratedLlmTranscript(job, raw);
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
				exchange.State = string.IsNullOrWhiteSpace(job.ForcedIntent) ? "analyzing_source" : "forced_action_published";
			}
		}
		if (!string.IsNullOrWhiteSpace(job.ForcedIntent))
		{
			ProcessAnalyzedDocument(document, job.ForcedIntent, "binding", document.RequiresResponse, "hostile", 1f, forced: true);
			return;
		}
		if (TryProcessGeneratedSemanticEnvelope(document, json, author, target, job.AllowUntargeted, job.IsRelayTurn))
		{
			return;
		}
		Log("generated semantic envelope incomplete; fallback analysis queued document=" + document.DocumentId);
		EnqueueAnalysisJob(document, job.IsResponse ? 60 : 50);
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
		if (job == null || json == null || author == null) return false;
		string intent = NormalizeIntent(ReadString(json, "author_intent.intent", "intent", "author_intent"));
		string title = ReadString(json, "title");
		string body = ReadString(json, "body", "public_document", "document");
		string visibleText = title + "\n" + body;
		string targetId = ReadString(json, "primary_target_kingdom_id", "target_kingdom_id", "target");
		generatedTarget = ResolveKingdom(targetId) ?? fallbackTarget;

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

		if (LooksLikeMisaddressedThirdPartyOfferResponse(job, author, generatedTarget, intent, visibleText))
		{
			reason = "new_proposal_claims_third_party_offer";
			return true;
		}
		if (TryGetImmersionViolation(visibleText, out reason))
		{
			return true;
		}
		if (TryGetRealmIdentityViolation(author, visibleText, out reason))
		{
			return true;
		}
		if (TryGetRequiredPeaceMoveViolation(job, author, generatedTarget, intent, out reason))
		{
			return true;
		}

		bool hasExplicitPeaceNegotiation = LooksLikeExplicitPeaceNegotiationWithTarget(author, generatedTarget, title + "\n" + body);
		if (!IsPeaceIntent(intent) && !hasExplicitPeaceNegotiation) return false;
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

	private bool TryGetRequiredPeaceMoveViolation(
		WorldDiplomacyJob job,
		Kingdom author,
		Kingdom generatedTarget,
		string generatedIntent,
		out string reason)
	{
		reason = "";
		if (job == null || author == null || !IsDiplomaticSituationAutoAdvanceEnabled() || IsPlayerKingdom(author)) return false;
		if (string.Equals(NormalizeIntent(job.ForcedIntent), "propose_peace", StringComparison.OrdinalIgnoreCase))
		{
			Kingdom forcedTarget = ResolveKingdom(job.TargetKingdomId);
			if (forcedTarget != null
				&& FactionManager.IsAtWarAgainstFaction(author, forcedTarget)
				&& (!string.Equals(NormalizeIntent(generatedIntent), "propose_peace", StringComparison.OrdinalIgnoreCase)
					|| generatedTarget != forcedTarget))
			{
				reason = "high_peace_pressure_requires_peace_proposal";
				return true;
			}
			return false;
		}
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (round == null) return false;
		IEnumerable<string> candidateIds = (round.RelayRouteKingdomIds?.Count ?? 0) > 0
			? round.RelayRouteKingdomIds
			: (round.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Where(x => x != null).Select(x => x.KingdomId);
		Kingdom requiredTarget = null;
		bool hasIncomingOffer = false;
		float highestPressure = float.MinValue;
		foreach (string id in candidateIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom other = ResolveKingdom(id);
			if (other == null || other == author || !FactionManager.IsAtWarAgainstFaction(author, other)) continue;
			WarSituationSnapshot snapshot = GetWarSituation(author, other);
			if (snapshot.AuthorPeacePressure < GetDiplomaticAdvanceThreshold()) continue;
			bool incoming = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
				&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(NormalizeIntent(x.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.ProposerKingdomId, other.StringId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
			if (incoming)
			{
				requiredTarget = other;
				hasIncomingOffer = true;
				break;
			}
			WorldDiplomacyWarLedger ledger = ResolveWarLedger(author.StringId, other.StringId);
			int lastProposalDay = GetLastForcedPeaceProposalDay(ledger, author.StringId);
			if (HasOpenPeaceOffer(author.StringId, other.StringId)
				|| (lastProposalDay > 0 && CurrentDay() - lastProposalDay < ForcedPeaceProposalCooldownDays)
				|| snapshot.AuthorPeacePressure <= highestPressure)
			{
				continue;
			}
			requiredTarget = other;
			highestPressure = snapshot.AuthorPeacePressure;
		}
		if (requiredTarget == null) return false;
		string intent = NormalizeIntent(generatedIntent);
		bool validMove = hasIncomingOffer
			? intent == "accept_peace" || intent == "propose_peace"
			: intent == "propose_peace";
		if (validMove && generatedTarget == requiredTarget) return false;
		reason = hasIncomingOffer
			? "high_peace_pressure_requires_acceptance_or_counteroffer"
			: "high_peace_pressure_requires_peace_proposal";
		return true;
	}

	private bool TryResolveOpenProposalFor(WorldDiplomacyJob job, Kingdom responder, Kingdom proposer, string proposalIntent, out string sourceDocumentId)
	{
		sourceDocumentId = "";
		if (job == null || responder == null || proposer == null || !IsProposalIntent(proposalIntent)) return false;
		WorldDiplomacyDocument source = ResolveDocument(job.SourceDocumentId);
		if (source != null
			&& string.Equals(NormalizeIntent(FirstNonEmpty(source.Intent, source.HiddenIntent)), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(source.AuthorKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(source.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase))
		{
			sourceDocumentId = source.DocumentId ?? "";
			return true;
		}
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
		WorldDiplomacyDocument source = ResolveDocument(response.SourceDocumentId);
		if (source != null
			&& string.Equals(NormalizeIntent(FirstNonEmpty(source.Intent, source.HiddenIntent)), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(source.AuthorKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(source.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase)
			&& (response.IsPlayerAuthored || string.Equals(response.RespondingToOfferDocumentId, source.DocumentId, StringComparison.OrdinalIgnoreCase))) return true;
		WorldDiplomacyRound round = ResolveRound(response.RoundId);
		return round?.PendingOffers?.Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, proposer.StringId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, responder.StringId, StringComparison.OrdinalIgnoreCase)
			&& (response.IsPlayerAuthored || string.Equals(response.RespondingToOfferDocumentId, x.SourceDocumentId, StringComparison.OrdinalIgnoreCase))) == true;
	}

	private bool LooksLikeMisaddressedThirdPartyOfferResponse(WorldDiplomacyJob job, Kingdom author, Kingdom target, string intent, string visibleText)
	{
		if (job == null || author == null || target == null || string.IsNullOrWhiteSpace(visibleText)) return false;
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (round?.PendingOffers == null) return false;
		List<WorldDiplomacyRoundOffer> offersFromTargetToOthers = round.PendingOffers.Where(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, target.StringId, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(x.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase)).ToList();
		if (offersFromTargetToOthers.Count == 0) return false;
		string normalizedIntent = NormalizeIntent(intent);
		if (IsProposalIntent(normalizedIntent)
			&& !offersFromTargetToOthers.Any(x => string.Equals(NormalizeIntent(x.Intent), normalizedIntent, StringComparison.OrdinalIgnoreCase))) return false;
		if (ContainsAny(visibleText, "另行提议", "另提一案", "另有一项提议", "由我国重新提出", "不是对先前提议的答复")) return false;
		bool claimsDirectOffer = ContainsAny(visibleText, "你的提议", "你的建议", "你所提出", "你的使者", "你派来的使者", "你想结盟", "你要求结盟", "你提出结盟", "你希望通商", "你提出议和");
		bool answersOffer = ContainsAny(visibleText, "接受", "同意", "答应", "拒绝", "可以，但", "可以，不过", "我有条件", "条件是");
		return claimsDirectOffer && answersOffer;
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

	private void PruneInvalidPeaceOffers(WorldDiplomacyRound round)
	{
		if (round?.PendingOffers == null || round.PendingOffers.Count == 0) return;
		int removed = round.PendingOffers.RemoveAll(x =>
		{
			if (x == null || !string.Equals(NormalizeIntent(x.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)) return false;
			Kingdom proposer = ResolveKingdom(x.ProposerKingdomId);
			Kingdom target = ResolveKingdom(x.TargetKingdomId);
			return proposer == null || target == null || !FactionManager.IsAtWarAgainstFaction(proposer, target);
		});
		if (removed > 0)
		{
			Log("invalid pending peace offers pruned round=" + round.RoundId + " count=" + removed.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void EnqueueGeneratedDeclarationRepair(WorldDiplomacyJob source, string rejectedRaw, Kingdom author, Kingdom target, string reason)
	{
		if (source == null || author == null || target == null) return;
		StringBuilder correctionBuilder = new StringBuilder();
		correctionBuilder.AppendLine("【未发布草稿的硬事实纠正】");
		correctionBuilder.AppendLine("上一份assistant内容只是未发布草稿，不属于外交历史，不得引用、延续或假定其中事件已经发生。");
		correctionBuilder.AppendLine("草稿违反原因=" + reason + "。");
		correctionBuilder.AppendLine("当前发文国=" + author.StringId + "=" + KingdomName(author) + "；对象国=" + target.StringId + "=" + KingdomName(target) + "；实时关系=" + BuildBilateralState(author, target) + "。");
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
		else if (string.Equals(reason, "high_peace_pressure_requires_acceptance_or_counteroffer", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "high_peace_pressure_requires_peace_proposal", StringComparison.OrdinalIgnoreCase))
		{
			correctionBuilder.AppendLine("发文国当前承受的战争压力已经达到外交局势自动推进门槛，不能继续只发表战况评论、谴责或泛泛警告。若对方已经提出和平方案，本国必须接受，或明确另提一份能够继续成交的和平反方案；若尚无方案，本国必须向实际交战对象提出合法和平条件。不得直接在后台结束战争，所有条件仍须写入公开宣言。");
		}
		correctionBuilder.Append("重新输出完整JSON。接受或拒绝时填写正确的responding_to_offer_document_id，其他情况留空。不要提到草稿、纠正、系统或上述错误。");
		string correction = correctionBuilder.ToString();
		List<WorldDiplomacyLlmMessage> messages = source.LlmMessages?.Count > 0
			? CloneLlmMessages(source.LlmMessages)
			: new List<WorldDiplomacyLlmMessage>
			{
				new WorldDiplomacyLlmMessage { Role = "system", Content = source.SystemPrompt ?? "" },
				new WorldDiplomacyLlmMessage { Role = "user", Content = source.UserPrompt ?? "" }
			};
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
			ForcedIntent = source.ForcedIntent ?? "",
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
			CacheAffinityKey = source.CacheAffinityKey ?? "",
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
		if (round?.LlmTranscript != null)
		{
			for (int index = round.LlmTranscript.Count - 1; index >= 0; index--)
			{
				WorldDiplomacyLlmMessage message = round.LlmTranscript[index];
				if (!string.Equals(message?.Role, "assistant", StringComparison.OrdinalIgnoreCase)
					|| string.IsNullOrWhiteSpace(document.Title)
					|| (message.Content ?? "").IndexOf(document.Title, StringComparison.OrdinalIgnoreCase) < 0) continue;
				round.LlmTranscript.RemoveAt(index);
				if (index > 0 && string.Equals(round.LlmTranscript[index - 1]?.Role, "user", StringComparison.OrdinalIgnoreCase))
				{
					round.LlmTranscript.RemoveAt(index - 1);
				}
				break;
			}
			round.LlmProfiledKingdomIds?.RemoveAll(x => string.Equals(x, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase));
		}
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

	private static string BuildIllegalPeaceFallbackGenerationJson(WorldDiplomacyJob job, Kingdom author, Kingdom target)
	{
		string targetId = target?.StringId ?? "";
		string body = KingdomName(author) + "将依据已经送达的公文与当前局势审视同" + KingdomName(target)
			+ "的关系。任何新的条件或承诺，都将由王庭另行发布明确文书。";
		return new JObject
		{
			["title"] = "关于当前局势",
			["body"] = body,
			["author_intent"] = new JObject { ["intent"] = "statement", ["commitment"] = "non_binding" },
			["responding_to_offer_document_id"] = "",
			["primary_target_kingdom_id"] = targetId,
			["addressed_kingdom_ids"] = string.IsNullOrWhiteSpace(targetId) ? new JArray() : new JArray(targetId),
			["mentioned_kingdom_ids"] = new JArray(),
			["requires_response"] = false,
			["tone"] = "neutral",
			["confidence"] = 1.0,
			["round_participation"] = "continue",
			["round_status"] = "continue",
			["made_progress"] = true,
			["peace_terms"] = new JObject
			{
				["tribute_payer_kingdom_id"] = "",
				["tribute_receiver_kingdom_id"] = "",
				["daily_tribute"] = 0,
				["duration_days"] = 0,
				["cession_from_kingdom_id"] = "",
				["cession_to_kingdom_id"] = "",
				["cession_settlement_id"] = ""
			}
		}.ToString(Formatting.None);
	}

	private string BuildRejectedDeclarationFallbackGenerationJson(WorldDiplomacyJob job, Kingdom author, Kingdom target, string reason)
	{
		if (string.Equals(reason, "peace_intent_between_kingdoms_not_at_war", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(reason, "peace_intent_has_no_valid_target", StringComparison.OrdinalIgnoreCase))
		{
			return BuildIllegalPeaceFallbackGenerationJson(job, author, target);
		}
		string forcedIntent = NormalizeIntent(job?.ForcedIntent);
		bool preserveForcedIntent = !string.IsNullOrWhiteSpace(forcedIntent);
		string intent = preserveForcedIntent ? forcedIntent : "statement";
		string targetId = target?.StringId ?? "";
		string title;
		string body;
		if (preserveForcedIntent)
		{
			title = IntentLabel(intent);
			body = BuildFallbackDocumentBody(author, target, intent, job.IsResponse, ResolveDocument(job.SourceDocumentId));
		}
		else if ((reason ?? "").IndexOf("offer", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			title = "关于当前交涉";
			body = BuildFallbackDocumentBody(author, target, "", job?.IsResponse == true, ResolveDocument(job?.SourceDocumentId));
		}
		else
		{
			title = job?.IsResponse == true ? "对当前交涉的回应" : "关于当前局势";
			body = BuildFallbackDocumentBody(author, target, "", job?.IsResponse == true, ResolveDocument(job?.SourceDocumentId));
		}
		return new JObject
		{
			["title"] = title,
			["body"] = body,
			["author_intent"] = new JObject { ["intent"] = intent, ["commitment"] = preserveForcedIntent ? "binding" : "non_binding" },
			["responding_to_offer_document_id"] = "",
			["primary_target_kingdom_id"] = targetId,
			["addressed_kingdom_ids"] = string.IsNullOrWhiteSpace(targetId) ? new JArray() : new JArray(targetId),
			["mentioned_kingdom_ids"] = new JArray(),
			["requires_response"] = false,
			["tone"] = "neutral",
			["confidence"] = 1.0,
			["round_participation"] = "continue",
			["round_status"] = "continue",
			["made_progress"] = true,
			["round_plan"] = new JObject { ["topic"] = "", ["selected_kingdom_ids"] = new JArray() },
			["peace_terms"] = new JObject
			{
				["tribute_payer_kingdom_id"] = "", ["tribute_receiver_kingdom_id"] = "", ["daily_tribute"] = 0, ["duration_days"] = 0,
				["cession_from_kingdom_id"] = "", ["cession_to_kingdom_id"] = "", ["cession_settlement_id"] = ""
			}
		}.ToString(Formatting.None);
	}

	private void RecordGeneratedLlmTranscript(WorldDiplomacyJob job, string rawAssistantContent)
	{
		if (job == null || !string.Equals(job.Kind, "generate", StringComparison.OrdinalIgnoreCase)) return;
		WorldDiplomacyRound round = ResolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
		if (round == null) return;
		round.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
		round.LlmProfiledKingdomIds ??= new List<string>();
		if (!job.IsRelayTurn && round.LlmTranscript.Count > 0) return;

		List<WorldDiplomacyLlmMessage> transcript = job.LlmMessages?.Count > 0
			? CloneLlmMessages(job.LlmMessages)
			: new List<WorldDiplomacyLlmMessage>
			{
				new WorldDiplomacyLlmMessage { Role = "system", Content = job.SystemPrompt ?? "" },
				new WorldDiplomacyLlmMessage { Role = "user", Content = job.UserPrompt ?? "" }
			};
		if (job.SemanticRepairAttempts > 0 && transcript.Count >= 2)
		{
			int correctionIndex = transcript.Count - 1;
			WorldDiplomacyLlmMessage correction = transcript[correctionIndex];
			WorldDiplomacyLlmMessage rejectedDraft = transcript[correctionIndex - 1];
			if (string.Equals(correction?.Role, "user", StringComparison.OrdinalIgnoreCase)
				&& (correction.Content ?? "").StartsWith("【未发布草稿的硬事实纠正】", StringComparison.Ordinal)
				&& string.Equals(rejectedDraft?.Role, "assistant", StringComparison.OrdinalIgnoreCase))
			{
				transcript.RemoveRange(correctionIndex - 1, 2);
			}
		}
		List<WorldDiplomacyLlmMessage> interleavedExternalMessages = new List<WorldDiplomacyLlmMessage>();
		if (job.IsRelayTurn && job.LlmMessages?.Count > 0)
		{
			int priorTranscriptCount = Math.Max(0, job.LlmMessages.Count - 1);
			if (round.LlmTranscript.Count > priorTranscriptCount
				&& LlmMessagePrefixMatches(round.LlmTranscript, job.LlmMessages, priorTranscriptCount))
			{
				interleavedExternalMessages = CloneLlmMessages(round.LlmTranscript.Skip(priorTranscriptCount));
			}
		}
		transcript.Add(new WorldDiplomacyLlmMessage { Role = "assistant", Content = rawAssistantContent ?? "" });
		transcript.AddRange(interleavedExternalMessages);
		round.LlmTranscript = transcript;
		string profiledId = FirstNonEmpty(job.ProfiledKingdomId, !job.IsRelayTurn ? job.AuthorKingdomId : "");
		if (!string.IsNullOrWhiteSpace(profiledId)
			&& !round.LlmProfiledKingdomIds.Any(x => string.Equals(x, profiledId, StringComparison.OrdinalIgnoreCase)))
		{
			round.LlmProfiledKingdomIds.Add(profiledId);
		}
		int transcriptChars = round.LlmTranscript.Sum(x => x?.Content?.Length ?? 0);
		Log("relay transcript committed round=" + round.RoundId
			+ " messages=" + round.LlmTranscript.Count.ToString(CultureInfo.InvariantCulture)
			+ " chars=" + transcriptChars.ToString(CultureInfo.InvariantCulture)
			+ " hash=" + StablePromptHash(string.Join("\n", round.LlmTranscript.Select(x => (x?.Role ?? "") + ":" + (x?.Content ?? "")))));
	}

	private static bool LlmMessagePrefixMatches(IReadOnlyList<WorldDiplomacyLlmMessage> first, IReadOnlyList<WorldDiplomacyLlmMessage> second, int count)
	{
		if (first == null || second == null || count < 0 || first.Count < count || second.Count < count) return false;
		for (int index = 0; index < count; index++)
		{
			WorldDiplomacyLlmMessage a = first[index];
			WorldDiplomacyLlmMessage b = second[index];
			if (!string.Equals(a?.Role ?? "", b?.Role ?? "", StringComparison.Ordinal)
				|| !string.Equals(a?.Content ?? "", b?.Content ?? "", StringComparison.Ordinal)) return false;
		}
		return true;
	}

	private bool TryProcessGeneratedSemanticEnvelope(WorldDiplomacyDocument document, JObject json, Kingdom author, Kingdom fallbackTarget, bool allowUntargeted, bool relayTurn)
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
		if (relayTurn && target != null && !RoundContainsKingdom(ResolveRound(document.RoundId), target.StringId)) return false;
		document.TargetKingdomId = target?.StringId ?? "";
		document.TargetKingdomName = target == null ? "" : KingdomName(target);
		List<string> addressed = ReadStringList(json, "addressed_kingdom_ids", "addressed");
		List<string> mentioned = ReadStringList(json, "mentioned_kingdom_ids", "mentioned");
		if (addressed.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null)
			|| mentioned.Any(x => string.IsNullOrWhiteSpace(x) || ResolveKingdom(x) == null))
		{
			return false;
		}
		if (relayTurn && addressed.Any(x => !RoundContainsKingdom(ResolveRound(document.RoundId), x))) return false;
		document.AddressedKingdomIds = NormalizeKingdomIdList(addressed.Concat(target == null ? Enumerable.Empty<string>() : new[] { target.StringId }), author.StringId);
		document.MentionedKingdomIds = NormalizeKingdomIdList(mentioned, author.StringId);
		document.Intent = intent;
		document.Commitment = commitment;
		document.Tone = NormalizeTone(ReadString(json, "tone"));
		document.Confidence = Math.Max(0f, Math.Min(1f, ReadFloat(json, "confidence")));
		document.RequiresResponse = ResolveValidatedResponseObligation(document, intent, ReadBool(json, "requires_response"));
		document.PeaceTerms = target == null ? document.PeaceTerms : (ParseAndValidatePeaceTerms(json, author, target) ?? document.PeaceTerms);
		document.AnalysisStatus = "generation_envelope";
		ProcessAnalyzedDocument(document, intent, commitment, document.RequiresResponse, document.Tone, document.Confidence, forced: false);
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
			? Limit(titleSummary, 36)
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
		ProcessAnalyzedDocument(document, intent, commitment, document.RequiresResponse, tone, confidence, forced: false);
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
		float confidence,
		bool forced)
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
			&& (IsPeaceIntent(normalizedIntent) || LooksLikeExplicitPeaceNegotiationWithTarget(author, target, document.Title + "\n" + document.Body))
			&& !FactionManager.IsAtWarAgainstFaction(author, target))
		{
			Log("illegal AI peace intent blocked before propagation document=" + document.DocumentId
				+ " author=" + author.StringId + " target=" + target.StringId + " intent=" + normalizedIntent);
			SuppressInvalidDocumentBeforePropagation(document, "peace_legality_guard");
			return;
		}
		ApplyDocumentPressure(document);
		if (target != null && target != author && IsImmediateIntent(normalizedIntent))
		{
			ExecuteImmediateIntent(author, target, normalizedIntent, forced, document);
		}
		TrySettleRoundAction(document);
		TrySettleRelayOffer(document);
		StartDocumentPropagation(document, author);
		HandleRoundDocumentProcessed(document);
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
		document.RoundProgressHandled = true;
		WorldDiplomacyRound round = ResolveRound(document.RoundId);
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		round.LastActivityDay = CurrentDay();
		bool successfulMechanicalAction = IsSuccessfulMechanicalResult(document.MechanicalResult);
		if (successfulMechanicalAction) round.ExecutedActionCount++;
		bool substantiveProgress = IsValidatedSubstantiveProgress(document, round, successfulMechanicalAction);
		document.MadeDiplomaticProgress = substantiveProgress;
		if (substantiveProgress)
		{
			round.SubstantiveProgressCount++;
			round.LastSubstantiveProgressDay = CurrentDay();
			Log("substantive diplomacy progress accepted round=" + round.RoundId
				+ " document=" + document.DocumentId
				+ " intent=" + NormalizeIntent(document.Intent)
				+ " count=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture));
		}
		if (string.IsNullOrWhiteSpace(round.RootDocumentId))
		{
			round.RootDocumentId = document.DocumentId;
			round.InitiatorKingdomId = document.AuthorKingdomId;
			if (document.HasEmbeddedRoundPlan)
			{
				CommitEmbeddedRoundPlan(round, document);
				if (round.RelayPlanned) return;
			}
			EnqueueRoundPlanJob(round, document);
			return;
		}
		if (!round.RelayPlanned)
		{
			EnqueueRoundPlanJob(round, ResolveDocument(round.RootDocumentId) ?? document);
			return;
		}
		if (document.IsPlayerAuthored)
		{
			IntegratePlayerDeclaration(round, document);
			return;
		}
		if (!document.IsRelayTurn) return;
		WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
		if (participant != null)
		{
			participant.TurnCount++;
			participant.LastSpokeDay = CurrentDay();
			if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase)
				&& round.SubstantiveProgressCount > 0)
			{
				participant.State = "withdrawn";
			}
			else if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase))
			{
				document.RoundParticipation = "continue";
				Log("premature relay withdrawal ignored until substantive diplomacy occurs round=" + round.RoundId + " kingdom=" + document.AuthorKingdomId);
			}
		}
		if (document.IsExternalResponseOnly)
		{
			if (participant != null) participant.MandatoryReplyPending = false;
			Log("priority player declaration response completed without moving relay cursor round=" + round.RoundId
				+ " document=" + document.DocumentId + " author=" + document.AuthorKingdomId);
			return;
		}
		round.RelayWaiting = false;
		bool hasValidatedResolution = round.ExecutedActionCount > 0;
		if (string.Equals(document.RoundStatus, "resolved", StringComparison.OrdinalIgnoreCase) && hasValidatedResolution)
		{
			round.RoundStatus = "resolved";
			CloseActiveRound("relay_resolved");
			return;
		}
		if (string.Equals(document.RoundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
			&& round.SubstantiveProgressCount > 0
			&& (round.RelayPassNumber >= 2 || string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase)))
		{
			round.RoundStatus = "deadlocked";
			CloseActiveRound("relay_deadlocked");
			return;
		}
		AdvanceRelay(round);
	}

	private static bool IsSuccessfulMechanicalResult(string mechanicalResult)
	{
		string value = (mechanicalResult ?? "").Trim();
		return !string.IsNullOrWhiteSpace(value)
			&& value.IndexOf("未执行", StringComparison.OrdinalIgnoreCase) < 0;
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
				&& !string.Equals(x.Status, "expired", StringComparison.OrdinalIgnoreCase));
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

	private void CommitEmbeddedRoundPlan(WorldDiplomacyRound round, WorldDiplomacyDocument root)
	{
		if (round == null || root == null || round.RelayPlanned) return;
		List<string> candidates = Kingdom.All.Where(x => x != null && !x.IsEliminated
			&& !string.Equals(x.StringId, root.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
			&& HasIndependentWorldDiplomacyAuthority(x))
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase).Select(x => x.StringId).ToList();
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
			SystemPrompt = BuildRoundPlanSystemPrompt(),
			UserPrompt = BuildRoundPlanPrompt(root, candidates),
			CacheAffinityKey = "diplomacy-round-plan:v4",
			MaxTokens = AnalysisMaxTokens
		};
		EnqueueJob(job);
	}

	private static string BuildRoundPlanSystemPrompt()
	{
		StringBuilder sb = new StringBuilder(BuildCommonDiplomacySystemPrefix());
		AppendWorldDiplomacyCustomPrompt(sb);
		sb.AppendLine("【当前任务：一次性规划外交回合参与国】根据开场外交宣言和候选国现实利益，一次选定本回合参与者；后续不会反复评估观察国。");
		sb.AppendLine("若宣言明确指向某国，该国必须参与。其余国家只选择确有战争、同盟、贸易、安全或政治利益关系者。不要为了热闹选满名单。");
		sb.AppendLine("标准活跃度通常选择2至4个非发起国；低活跃度1至2个，高活跃度3至5个。只可使用候选ID。");
		sb.AppendLine("回合目标是在当前设定的时间内通过提议、反提议、接受、拒绝、退出或合法外交动作形成进展，不是长期争吵。");
		sb.AppendLine("只输出JSON：{\"topic\":\"简短外交议题\",\"selected_kingdom_ids\":[\"ID\"],\"reason\":\"简短理由\"}");
		return sb.ToString().TrimEnd();
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
		sb.AppendLine("外交活跃度=" + GetActivityLevel().ToString(CultureInfo.InvariantCulture));
		sb.AppendLine("候选国：");
		foreach (string id in candidateIds ?? new List<string>())
		{
			Kingdom kingdom = ResolveKingdom(id);
			if (kingdom == null) continue;
			sb.AppendLine(id + "=" + KingdomName(kingdom) + "，统治者=" + RulerName(kingdom)
				+ "，与发起国关系=" + GetRulerRelation(ResolveKingdom(root.AuthorKingdomId), kingdom).ToString(CultureInfo.InvariantCulture));
			string policy = WorldDiplomacyPolicyContext.BuildSnapshot(id);
			if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("  政策=" + Limit(policy, 500));
			string weekly = BuildWeeklyDiplomacySnapshot(id);
			if (!string.IsNullOrWhiteSpace(weekly)) sb.AppendLine("  周报=" + Limit(weekly, 260));
		}
		return sb.ToString().TrimEnd();
	}

	private void CommitRoundPlan(WorldDiplomacyJob job, string raw)
	{
		WorldDiplomacyRound round = ResolveRound(job?.RoundId);
		WorldDiplomacyDocument root = ResolveDocument(job?.DocumentId);
		Kingdom initiator = ResolveKingdom(root?.AuthorKingdomId ?? round?.InitiatorKingdomId);
		if (round == null || root == null || initiator == null || round.RelayPlanned) return;
		JObject json = ParseJsonObject(raw);
		round.RoundTopic = Limit(FirstNonEmpty(ReadString(json, "topic"), root.Title, "外交交涉"), 120);
		HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in ReadStringList(json, "selected_kingdom_ids"))
		{
			Kingdom selectedKingdom = ResolveKingdom(id);
			if ((job.CandidateKingdomIds ?? new List<string>()).Contains(id, StringComparer.OrdinalIgnoreCase)
				&& HasIndependentWorldDiplomacyAuthority(selectedKingdom)) selected.Add(id);
		}
		HashSet<string> mandatoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string id in _storage.Documents
			.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase))
			.SelectMany(x => x.AddressedKingdomIds ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Kingdom mandatory = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
			if (mandatory != null && mandatory != initiator)
			{
				mandatoryIds.Add(mandatory.StringId);
				selected.Add(mandatory.StringId);
			}
		}
		int desired = GetActivityLevel() switch { 0 => 2, 2 => 5, _ => 3 };
		if (selected.Count == 0)
		{
			foreach (Kingdom fallback in (job.CandidateKingdomIds ?? new List<string>()).Select(ResolveKingdom)
				.Where(x => x != null && !selected.Contains(x.StringId))
				.OrderBy(x => CourtDistance(initiator, x)).ThenBy(x => x.StringId, StringComparer.OrdinalIgnoreCase))
			{
				if (selected.Count >= desired) break;
				selected.Add(fallback.StringId);
			}
		}
		List<Kingdom> mandatoryRoute = mandatoryIds.Select(ResolveKingdom).Where(x => x != null && x != initiator)
			.Distinct().OrderBy(x => CourtDistance(initiator, x)).ToList();
		int optionalSlots = Math.Max(0, MaxRelayParticipants - 1 - mandatoryRoute.Count);
		List<Kingdom> optionalRoute = selected.Where(x => !mandatoryIds.Contains(x)).Select(ResolveKingdom)
			.Where(x => x != null && x != initiator).Distinct()
			.OrderBy(x => CourtDistance(initiator, x)).Take(optionalSlots).ToList();
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
			CloseActiveRound("round_plan_no_participants");
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
		round.CachePrefix = BuildRoundCachePrefix(round, root);
		int cachePrefixProbeChars = Math.Min(2048, round.CachePrefix?.Length ?? 0);
		Log("relay round planned round=" + round.RoundId + " route=" + string.Join(">", route)
			+ " passDays=" + round.RelayPassDurationDays.ToString(CultureInfo.InvariantCulture)
			+ " targetDays=" + Math.Max(1, round.SoftEndDay - round.StartedDay).ToString(CultureInfo.InvariantCulture)
			+ " frozenPrefixChars=" + (round.CachePrefix?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " frozenPrefix2048Hash=" + StablePromptHash(cachePrefixProbeChars <= 0 ? "" : round.CachePrefix.Substring(0, cachePrefixProbeChars)));
		ScheduleNextRelayHop(round);
	}

	private float CourtDistance(Kingdom first, Kingdom second)
	{
		Settlement a = ResolveCourtSettlement(first);
		Settlement b = ResolveCourtSettlement(second);
		return a == null || b == null ? float.MaxValue : a.GatePosition.Distance(b.GatePosition);
	}

	private string BuildRoundCachePrefix(WorldDiplomacyRound round, WorldDiplomacyDocument root)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【本回合冻结档案 v8】");
		sb.AppendLine("议题=" + round.RoundTopic);
		sb.AppendLine("开场宣言=" + root.AuthorKingdomId + "|" + root.Title + "|" + Limit(root.Body, 2200));
		sb.AppendLine("基础接力顺序=" + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
		sb.AppendLine("计划回合时长=" + Math.Max(1, round.SoftEndDay - round.StartedDay).ToString(CultureInfo.InvariantCulture) + "天；接力单程=" + Math.Max(1, round.RelayPassDurationDays).ToString(CultureInfo.InvariantCulture) + "天");
		sb.AppendLine("回合硬目标=正常结束前至少出现一次可由程序核验的实质外交尝试。正式提议、反提议、对真实提议的接受或拒绝、最后通牒、明确道歉或让步，以及成功执行的外交机制可以计入；普通声明、谴责、泛泛警告、评论和重复旧立场不计入。");
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		sb.AppendLine("参与国回合初始背景（仅用于保持各统治者自身决策一致；不得声称这些内部信息已被其他王国获知）：");
		sb.AppendLine("档案中的政策、周报和态势材料只供判断。公开宣言必须改写成卡拉迪亚统治者能从使者、战报、账簿与领地得失中理解的说法，不得复述后台评分、关系点或其他游戏指标。");
		foreach (string id in (round.RelayRouteKingdomIds ?? new List<string>()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveKingdom(id);
			if (kingdom == null) continue;
			sb.AppendLine("-- " + id + "=" + KingdomName(kingdom) + "，统治者=" + RulerName(kingdom));
			string voice = BuildRulerVoiceContext(kingdom);
			if (!string.IsNullOrWhiteSpace(voice)) sb.AppendLine("统治者声音=" + Limit(voice, 900));
			string realmVoice = BuildRealmInstitutionalVoiceContext(kingdom);
			if (!string.IsNullOrWhiteSpace(realmVoice)) sb.AppendLine("国家制度与礼制声音=" + Limit(realmVoice, 1400));
			string family = BuildAuthorRulerFamilyContext(kingdom);
			if (!string.IsNullOrWhiteSpace(family)) sb.AppendLine("王室与亲属=" + Limit(family, 650));
			string policy = WorldDiplomacyPolicyContext.BuildSnapshot(id);
			if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("政策=" + Limit(policy, 900));
			string weekly = BuildWeeklyDiplomacySnapshot(id);
			if (!string.IsNullOrWhiteSpace(weekly)) sb.AppendLine("周报=" + Limit(weekly, 500));
		}
		if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext))
		{
			sb.AppendLine("【本回合既定外部事件】");
			sb.AppendLine(Limit(round.ExternalOpeningContext, 1800));
		}
		return sb.ToString().TrimEnd();
	}

	private static string BuildRelayGenerationSystemPrompt()
	{
		return BuildDiplomaticDeclarationSystemPrompt();
	}

	private string BuildRelayGenerationPrompt(WorldDiplomacyRound round, Kingdom author, Kingdom previous)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(round?.CachePrefix ?? "");
		if (!string.IsNullOrWhiteSpace(round?.ExternalOpeningContext)
			&& (round.CachePrefix ?? "").IndexOf((round.ExternalSignalKeys ?? new List<string>()).FirstOrDefault() ?? "", StringComparison.OrdinalIgnoreCase) < 0)
		{
			sb.AppendLine("【本回合新增外部事件】");
			sb.AppendLine(Limit(round.ExternalOpeningContext, 1800));
		}
		sb.AppendLine("【本回合已颁布外交公文档案】");
		foreach (WorldDiplomacyDocument document in _storage.Documents
			.Where(x => x != null && string.Equals(x.RoundId, round?.RoundId, StringComparison.OrdinalIgnoreCase) && x.IsReadyForPublication
				&& !string.Equals(x.DocumentId, round?.RootDocumentId, StringComparison.OrdinalIgnoreCase))
			.OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).Take(30))
		{
			sb.AppendLine("DOC " + document.DocumentId + "|" + document.AuthorKingdomId + "|" + document.Intent + "|" + document.Title);
			sb.AppendLine(Limit(document.Body, 1800));
		}
		sb.AppendLine("【本次动态尾部】");
		sb.AppendLine("本篇发布国=" + author.StringId + "=" + KingdomName(author) + "，授权统治者=" + RulerName(author));
		if ((round?.CachePrefix ?? "").IndexOf("-- " + author.StringId + "=", StringComparison.OrdinalIgnoreCase) < 0)
		{
			string addedVoice = BuildRulerVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(addedVoice)) sb.AppendLine("玩家介入后新增参与国声音=" + Limit(addedVoice, 900));
			string addedRealmVoice = BuildRealmInstitutionalVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(addedRealmVoice)) sb.AppendLine("玩家介入后新增参与国制度与礼制声音=" + Limit(addedRealmVoice, 1400));
			string addedFamily = BuildAuthorRulerFamilyContext(author);
			if (!string.IsNullOrWhiteSpace(addedFamily)) sb.AppendLine("玩家介入后新增参与国王室与亲属=" + Limit(addedFamily, 650));
			string addedPolicy = WorldDiplomacyPolicyContext.BuildSnapshot(author.StringId);
			if (!string.IsNullOrWhiteSpace(addedPolicy)) sb.AppendLine("玩家介入后新增参与国政策=" + Limit(addedPolicy, 900));
			string addedWeekly = BuildWeeklyDiplomacySnapshot(author.StringId);
			if (!string.IsNullOrWhiteSpace(addedWeekly)) sb.AppendLine("玩家介入后新增参与国周报=" + Limit(addedWeekly, 500));
		}
		sb.AppendLine("最近送抵本国王庭的公文来源=" + (previous?.StringId ?? "") + "=" + KingdomName(previous));
		sb.AppendLine("允许动作对象=" + string.Join(",", (round?.RelayRouteKingdomIds ?? new List<string>()).Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))));
		foreach (WorldDiplomacyRoundOffer offer in (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase);
			sb.AppendLine("待回应提议=" + offer.Intent + "|提出国=" + offer.ProposerKingdomId + "|对象国=" + offer.TargetKingdomId + "|来源=" + offer.SourceDocumentId
				+ (canAnswer ? "|答复资格=本国可以接受或拒绝" : "|答复资格=本国不是对象国，不得接受或拒绝；只能评论或明确另提新案"));
		}
		int age = Math.Max(0, CurrentDay() - (round?.StartedDay ?? CurrentDay()));
		int targetDays = Math.Max(1, (round?.SoftEndDay ?? CurrentDay() + RelayTargetDurationDays) - (round?.StartedDay ?? CurrentDay()));
		int remainingDays = Math.Max(0, targetDays - age);
		sb.AppendLine("回合已经进行=" + age.ToString(CultureInfo.InvariantCulture) + "天；计划时长=" + targetDays.ToString(CultureInfo.InvariantCulture)
			+ "天；距离计划收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前接力轮次=" + (round?.RelayPassNumber ?? 0).ToString(CultureInfo.InvariantCulture));
		AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays);
		AppendOpenOfferAnswerRequirement(sb, round, author, age, targetDays);
		if (age * 100 >= targetDays * 85) sb.AppendLine("当前已进入最后阶段：必须给出最终条件、接受、拒绝、退出或合法行动，不得继续空泛争论。");
		else if (age * 100 >= targetDays * 70) sb.AppendLine("当前已进入回合后段：优先收束分歧并形成明确结果。");
		string gatheringContext = NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(round?.RelayRouteKingdomIds, 3);
		if (!string.IsNullOrWhiteSpace(gatheringContext))
		{
			sb.AppendLine("【近期相关宴会】");
			sb.AppendLine(Limit(gatheringContext, 900));
			sb.AppendLine("宴会只是当前可利用或评论的公开动向，不预设赞扬、嘲讽或敌意，也不自动产生外交结果。");
		}
		foreach (string id in round?.RelayRouteKingdomIds ?? new List<string>())
		{
			Kingdom other = ResolveKingdom(id);
			if (other == null || other == author) continue;
			sb.AppendLine("即时状态：" + id + "=" + BuildBilateralState(author, other) + "，私人关系=" + DescribeRulerRelation(GetRulerRelation(author, other)));
			if (FactionManager.IsAtWarAgainstFaction(author, other)) sb.AppendLine(Limit(BuildWarNegotiationContext(author, other), 1200));
		}
		return sb.ToString().TrimEnd();
	}

	private string BuildRelayConversationTurnPrompt(
		WorldDiplomacyRound round,
		Kingdom author,
		Kingdom previous,
		bool includeActorProfile,
		WorldDiplomacyDocument prioritySource = null,
		bool priorityResponseOnly = false)
	{
		PruneInvalidPeaceOffers(round);
		if (round?.LlmTranscript == null || round.LlmTranscript.Count == 0)
		{
			// 旧存档或玩家发起的回合没有可逐字复用的 API 历史；首个 AI 回应沿用完整档案，
			// 成功后会把这次真实请求和原始输出保存为后续接力的缓存链起点。
			return BuildRelayGenerationPrompt(round, author, previous);
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("【本次新增外交公文任务 v10】");
		sb.AppendLine("此前的assistant JSON是按日期归档的已颁布外交公文，不是君主们正在进行的即时聊天。读取其中已经送达的主张与未决提议，为当前王国另行起草一份能够独立传阅的正式公文；不要承接上一句的聊天语气，不要重述全部历史，也不要把内部档案写成公开事实。");
		sb.AppendLine("议题=" + (round.RoundTopic ?? ""));
		sb.AppendLine("公文送达与发布顺序=" + string.Join(">", round.RelayRouteKingdomIds ?? new List<string>()));
		sb.AppendLine("本篇发布国=" + author.StringId + "=" + KingdomName(author) + "，授权统治者=" + RulerName(author));
		if (priorityResponseOnly && prioritySource != null)
		{
			sb.AppendLine("【本篇优先任务：回应玩家王国宣言】");
			sb.AppendLine("玩家王国的下列宣言已经送达本国王庭并直接指向本国，本篇必须正面回应它，而不是沿原接力顺序改谈其他国家：来源="
				+ prioritySource.DocumentId + "|发文国=" + prioritySource.AuthorKingdomId + "|标题=" + prioritySource.Title
				+ "|已裁定意图=" + NormalizeIntent(prioritySource.Intent));
			sb.AppendLine("若玩家是在接受或拒绝本国此前提案，应承认该答复已经发生，并对结果作一次正式反应；不得继续声称玩家尚未答复。若玩家提出新提案、反提案或最后通牒，应接受、拒绝、提出明确反方案或说明仍待解决的具体条件。该优先回应完成后，原接力会继续，不得要求玩家立即再次发言。");
		}
		if (includeActorProfile)
		{
			sb.AppendLine("【本发布国首次进入公文链的稳定决策档案】");
			string voice = BuildRulerVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(voice)) sb.AppendLine("统治者声音=" + Limit(voice, 700));
			string realmVoice = BuildRealmInstitutionalVoiceContext(author);
			if (!string.IsNullOrWhiteSpace(realmVoice)) sb.AppendLine("国家制度与礼制声音=" + Limit(realmVoice, 1100));
			string family = BuildAuthorRulerFamilyContext(author);
			if (!string.IsNullOrWhiteSpace(family)) sb.AppendLine("王室与亲属=" + Limit(family, 400));
			string policy = WorldDiplomacyPolicyContext.BuildSnapshot(author.StringId);
			if (!string.IsNullOrWhiteSpace(policy)) sb.AppendLine("政策=" + Limit(policy, 550));
			string weekly = BuildWeeklyDiplomacySnapshot(author.StringId);
			if (!string.IsNullOrWhiteSpace(weekly)) sb.AppendLine("周报=" + Limit(weekly, 300));
		}
		else
		{
			sb.AppendLine("该国的稳定决策档案已在此前消息中给出，继续沿用，不要重新发明人物立场。");
		}
		sb.AppendLine("最近送抵本国王庭的公文来源=" + (previous?.StringId ?? "") + "=" + KingdomName(previous));
		sb.AppendLine("允许动作对象=" + string.Join(",", (round.RelayRouteKingdomIds ?? new List<string>()).Where(x => !string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase))));
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)))
		{
			bool canAnswer = string.Equals(offer.TargetKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase);
			sb.AppendLine("待回应提议=" + offer.Intent + "|提出国=" + offer.ProposerKingdomId + "|对象国=" + offer.TargetKingdomId + "|来源=" + offer.SourceDocumentId
				+ (canAnswer ? "|答复资格=本国可以接受或拒绝" : "|答复资格=本国不是对象国，不得接受或拒绝；只能评论或明确另提新案"));
		}
		int age = Math.Max(0, CurrentDay() - round.StartedDay);
		int targetDays = Math.Max(1, round.SoftEndDay - round.StartedDay);
		int remainingDays = Math.Max(0, targetDays - age);
		sb.AppendLine("回合已经进行=" + age.ToString(CultureInfo.InvariantCulture) + "天；计划时长=" + targetDays.ToString(CultureInfo.InvariantCulture)
			+ "天；距离计划收束=" + remainingDays.ToString(CultureInfo.InvariantCulture) + "天；当前接力轮次=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture));
		AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays);
		AppendOpenOfferAnswerRequirement(sb, round, author, age, targetDays);
		if (age * 100 >= targetDays * 85) sb.AppendLine("当前已进入最后阶段：必须给出最终条件、接受、拒绝、退出或合法行动，不得继续空泛争论。");
		else if (age * 100 >= targetDays * 70) sb.AppendLine("当前已进入回合后段：优先收束分歧并形成明确结果。");
		if (!string.IsNullOrWhiteSpace(round.ExternalOpeningContext))
		{
			sb.AppendLine("【本回合已知外部事件】");
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
		string stateText = stateSnapshot.ToString().TrimEnd();
		string stateSignature = StablePromptHash(stateText);
		round.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (round.LlmLastStateSignatureByKingdom.TryGetValue(author.StringId, out string previousStateSignature)
			&& string.Equals(previousStateSignature, stateSignature, StringComparison.Ordinal))
		{
			sb.AppendLine("当前即时状态与该国上次发言时相同，继续沿用此前已经给出的战争、和平、关系与合法动作边界。");
		}
		else
		{
			sb.AppendLine(stateText);
			round.LlmLastStateSignatureByKingdom[author.StringId] = stateSignature;
		}
		return sb.ToString().TrimEnd();
	}

	private static void AppendRoundSubstantiveProgressRequirement(StringBuilder sb, WorldDiplomacyRound round, int age, int targetDays)
	{
		if (sb == null || round == null) return;
		sb.AppendLine("程序核验的实质外交进展=" + Math.Max(0, round.SubstantiveProgressCount).ToString(CultureInfo.InvariantCulture)
			+ "次；已经执行的游戏外交动作=" + Math.Max(0, round.ExecutedActionCount).ToString(CultureInfo.InvariantCulture) + "次。");
		if (round.SubstantiveProgressCount > 0) return;
		if (round.FinalActionOpportunityIssued || age * 100 >= targetDays * 85)
		{
			sb.AppendLine("硬性要求：本回合至今没有任何可核验的实质外交尝试，本篇是最后行动机会。必须提出一项合法而明确的方案、反方案或最后通牒，或者对本国有资格回答的真实提议作出接受或拒绝；不得只声明、谴责、评论、泛泛警告或退出。");
		}
		else if (age * 100 >= targetDays * 40)
		{
			sb.AppendLine("推进要求：本回合已经进入中段但仍无实质外交尝试。本篇必须开始形成明确条件、提议、反提议、最后通牒、道歉或让步；不得继续只改写旧立场。");
		}
		else
		{
			sb.AppendLine("本回合尚无实质外交尝试。可以先说明立场，但必须为随后形成明确提议、条件、让步或其他外交结果留下可执行方向。");
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
			sb.AppendLine("本国是尚未答复的正式提议对象，回合已经进入收束阶段。本篇必须接受、拒绝或提出明确反方案；不得绕开提议另谈无关事项，也不得以评论代替答复。");
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
					Log("relay final substantive action opportunity opened round=" + round.RoundId);
				}
			}
			nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection);
		}
		if (nextIndex < 0)
		{
			CloseActiveRound(round.SubstantiveProgressCount > 0
				? "relay_all_participants_withdrew"
				: "technical_no_substantive_participants");
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
				forcedIntent: ResolveArmedIntent(receiver, previous), sourceDocument: source, priority: 75, roundId: round.RoundId,
				isRelayTurn: true, previousKingdomId: arrival.FromKingdomId, scheduledDay: arrival.DueDay);
		}
	}

	private void AdvanceRelay(WorldDiplomacyRound round)
	{
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		if (CurrentDay() >= round.HardEndDay)
		{
			CloseActiveRound(round.SubstantiveProgressCount > 0
				? "relay_hard_end"
				: "technical_no_substantive_progress");
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
		AppendPlayerDeclarationToLlmTranscript(round, document);
		WorldDiplomacyRoundParticipant playerParticipant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
		if (playerParticipant != null)
		{
			playerParticipant.SelectedForRelay = true;
			playerParticipant.IsPlayerAsync = true;
			playerParticipant.LastSpokeDay = CurrentDay();
			AddParticipantToRelayRouteIfNeeded(round, document.AuthorKingdomId);
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
			participant.SelectedForRelay = true;
			participant.IsPlayerAsync = IsPlayerKingdom(kingdom);
			AddParticipantToRelayRouteIfNeeded(round, kingdom.StringId);
		}
		Log("player declaration appended to relay round=" + round.RoundId + " document=" + document.DocumentId);
	}

	private void AppendPlayerDeclarationToLlmTranscript(WorldDiplomacyRound round, WorldDiplomacyDocument document)
	{
		if (round?.LlmTranscript == null || round.LlmTranscript.Count == 0 || document == null) return;
		List<string> directTargets = (document.AddressedKingdomIds ?? new List<string>())
			.Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		string content = "【玩家王国在两次 AI 接力之间公开发布的外交宣言】\n"
			+ "发文国=" + document.AuthorKingdomId + "=" + document.AuthorKingdomName + "\n"
			+ "标题=" + document.Title + "\n"
			+ "正文=" + document.Body + "\n"
			+ "指向=" + string.Join(",", directTargets) + "\n"
			+ "语义=" + NormalizeIntent(document.Intent)
			+ (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId) ? "" : "|回应来源=" + document.RespondingToOfferDocumentId) + "\n"
			+ "这是玩家亲自写出的公开材料，不得替玩家补写承诺；后续 AI 王国可以据此作出自己的回应。";
		round.LlmTranscript.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = content });
		Log("relay transcript appended player declaration round=" + round.RoundId + " messages=" + round.LlmTranscript.Count.ToString(CultureInfo.InvariantCulture));
	}

	private static bool RoundContainsKingdom(WorldDiplomacyRound round, string kingdomId)
	{
		return round?.Participants?.Any(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)) == true;
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

	private static void AddParticipantToRelayRouteIfNeeded(WorldDiplomacyRound round, string kingdomId)
	{
		if (round == null || !round.RelayPlanned || string.IsNullOrWhiteSpace(kingdomId)) return;
		round.RelayRouteKingdomIds ??= new List<string>();
		if (round.RelayRouteKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase)
			|| round.RelayRouteKingdomIds.Count >= MaxRelayParticipants) return;
		round.RelayRouteKingdomIds.Add(kingdomId);
	}

	private string ResolveArmedIntent(Kingdom author, Kingdom target)
	{
		if (author == null || target == null || author == target || !IsDiplomaticSituationAutoAdvanceEnabled())
		{
			return "";
		}
		if (FactionManager.IsAtWarAgainstFaction(author, target) && !IsPlayerKingdom(author))
		{
			WorldDiplomacyWarLedger ledger = ResolveWarLedger(author.StringId, target.StringId);
			int day = CurrentDay();
			int lastProposalDay = GetLastForcedPeaceProposalDay(ledger, author.StringId);
			WarSituationSnapshot snapshot = GetWarSituation(author, target);
			if (ledger != null
				&& snapshot.AuthorPeacePressure >= GetDiplomaticAdvanceThreshold()
				&& (lastProposalDay <= 0 || day - lastProposalDay >= ForcedPeaceProposalCooldownDays)
				&& !HasOpenPeaceOffer(author.StringId, target.StringId))
			{
				SetLastForcedPeaceProposalDay(ledger, author.StringId, day);
				return "propose_peace";
			}
			return "";
		}
		WarPressureEntry entry = FindWarPressure(author?.StringId, target?.StringId);
		return entry?.IsEscalationArmed == true
			&& entry.Value >= GetDiplomaticAdvanceReleaseThreshold()
			? "declare_war"
			: "";
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
		if (document == null || document.PropagationStarted || author == null)
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
		foreach (Settlement settlement in settlements)
		{
			if (origin != null && settlement == origin)
			{
				continue;
			}
			float distance = origin == null ? maxCivilianDistance : origin.GatePosition.Distance(settlement.GatePosition);
			int travelDays = maxCivilianDistance <= 0.01f
				? 1
				: CalculatePropagationDays(distance, maxCivilianDistance, civilianSpreadDays);
			latestCivilianDueDay = Math.Max(latestCivilianDueDay, CurrentDay() + travelDays);
			_storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
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
			float distance = origin == null || destination.Item2 == null
				? maxCourtDistance
				: origin.GatePosition.Distance(destination.Item2.GatePosition);
			int travelDays = maxCourtDistance <= 0.01f
				? courtDeliveryDays
				: CalculatePropagationDays(distance, maxCourtDistance, courtDeliveryDays);
			latestCourtDueDay = Math.Max(latestCourtDueDay, CurrentDay() + travelDays);
			_storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
			{
				DocumentId = document.DocumentId,
				RoundId = document.RoundId,
				SettlementId = destination.Item2?.StringId ?? "",
				KingdomId = destination.Item1.StringId,
				Scope = "court",
				DueDay = CurrentDay() + travelDays
			});
		}
		_storage.PropagationArrivals = _storage.PropagationArrivals
			.Where(x => x != null)
			.OrderBy(x => x.DueDay)
			.ThenBy(x => IsCourtArrival(x) ? 0 : 1)
			.ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
			.ToList();
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
			if (!string.IsNullOrWhiteSpace(item.MechanicalResult))
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
			InformationManager.DisplayMessage(new InformationMessage("你的宣言已传播至" + KingdomName(receiver) + "。"));
			WorldDiplomacyRound round = ResolveRound(document.RoundId);
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
		bool reuseRelayTranscript = round.LlmTranscript?.Count > 0;
		EnqueueGenerationJob(receiver, target, null, isResponse: true, forcedIntent: "", sourceDocument: trigger,
			priority: 95, externalResponseOnly: true, roundId: round.RoundId, isRelayTurn: reuseRelayTranscript,
			previousKingdomId: trigger.AuthorKingdomId, scheduledDay: CurrentDay());
		participant.LastTriggeredDocumentId = trigger.DocumentId;
		Log("mandatory response queued round=" + round.RoundId + " author=" + receiver.StringId + " target=" + (target?.StringId ?? "") + " source=" + trigger.DocumentId);
	}

	private bool HasKingdomRespondedToDocument(string kingdomId, string documentId)
	{
		return _storage.Documents.Any(x => x != null && string.Equals(x.AuthorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.SourceDocumentId, documentId, StringComparison.OrdinalIgnoreCase));
	}

	private void QueueParticipationEvaluation(WorldDiplomacyRound round, Kingdom kingdom, WorldDiplomacyDocument trigger)
	{
		kingdom = ResolveWorldDiplomacyRepresentative(kingdom);
		if (round == null || kingdom == null || trigger == null || !HasIndependentWorldDiplomacyAuthority(kingdom)) return;
		WorldDiplomacyRoundParticipant participant = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
			.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase));
		participant ??= EnsureRoundParticipant(round, kingdom.StringId, "observer", mandatoryReply: false);
		if (!ShouldReevaluateParticipation(participant, kingdom, trigger)) return;
		WorldDiplomacyParticipationRequest existing = _storage.PendingParticipationEvaluations.FirstOrDefault(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.KingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase));
		if (existing == null)
		{
			// Give nearby replies a short geographic collection window, then judge all newly arrived material in one request.
			existing = new WorldDiplomacyParticipationRequest { RoundId = round.RoundId, KingdomId = kingdom.StringId, DueDay = CurrentDay() + 2 };
			_storage.PendingParticipationEvaluations.Add(existing);
		}
		if (!existing.TriggerDocumentIds.Contains(trigger.DocumentId, StringComparer.OrdinalIgnoreCase)) existing.TriggerDocumentIds.Add(trigger.DocumentId);
	}

	private bool ShouldReevaluateParticipation(WorldDiplomacyRoundParticipant participant, Kingdom kingdom, WorldDiplomacyDocument trigger)
	{
		if (participant == null || participant.LastEvaluationDay <= 0)
		{
			return true;
		}
		bool explicitlyMentioned = (trigger?.MentionedKingdomIds ?? new List<string>()).Contains(kingdom?.StringId, StringComparer.OrdinalIgnoreCase);
		bool major = IsMajorDiplomaticDocument(trigger);
		int cooldown = explicitlyMentioned || major ? 2 : ParticipationObserverCooldownDays;
		return CurrentDay() - participant.LastEvaluationDay >= cooldown;
	}

	private void EnqueueParticipationBatchIfNeeded()
	{
		if (_storage.Jobs.Any(x => x != null && string.Equals(x.Kind, "participate", StringComparison.OrdinalIgnoreCase))) return;
		List<WorldDiplomacyParticipationRequest> requests = _storage.PendingParticipationEvaluations
			.Where(x => x != null && x.DueDay <= CurrentDay())
			.Take(MaxParticipationCandidatesPerJob)
			.ToList();
		if (requests.Count == 0) return;
		WorldDiplomacyJob job = new WorldDiplomacyJob
		{
			JobId = NewId("diplomacy_participate"), Kind = "participate", Priority = 35, CreatedDay = CurrentDay(),
			RoundId = requests[0].RoundId,
			CandidateKingdomIds = requests.Select(x => x.KingdomId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			TriggerDocumentIds = requests.SelectMany(x => x.TriggerDocumentIds).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			MaxTokens = AnalysisMaxTokens,
			SystemPrompt = BuildParticipationSystemPrompt(),
			CacheAffinityKey = "participate"
		};
		job.UserPrompt = BuildParticipationPrompt(job, requests);
		foreach (WorldDiplomacyParticipationRequest request in requests) _storage.PendingParticipationEvaluations.Remove(request);
		EnqueueJob(job);
	}

	private static string BuildParticipationSystemPrompt()
	{
		StringBuilder sb = new StringBuilder(BuildCommonDiplomacySystemPrefix());
		sb.AppendLine("【任务：外交参与前处理】一次性判断候选王国在收到新公文后，是继续旁观、加入并发言，还是认为无关而退出当前外交事件。");
		string customPrompt = DuelSettings.GetWorldDiplomacyPromptForExternal();
		if (!string.IsNullOrWhiteSpace(customPrompt))
		{
			sb.AppendLine("【玩家自定义判断与文风偏好】");
			sb.AppendLine(customPrompt.Trim());
			sb.AppendLine("该偏好不得覆盖已知事实、参与状态和输出契约。");
		}
		sb.AppendLine("评论、支持、威胁、调停、搅局、提出合作或转向事件内其他国家都允许；不要把第三国限制为评论员。候选国与事件国家交战、结盟、存在直接政策压力，或重大提案会改变其安全与利益时，应积极考虑speak_now=true；确实无关时才保持旁观。");
		sb.AppendLine("只能使用候选王国ID和当前仍存在的王国ID。state取observer|active|withdrawn。target_kingdom_id为空表示不发言；若发言，选择最主要的公开对象。退出后除非未来被直接点名或利益受到确定性影响，不会再次调用模型判断。");
		sb.AppendLine("只输出JSON：{\"decisions\":[{\"kingdom_id\":\"ID\",\"state\":\"observer|active|withdrawn\",\"speak_now\":false,\"target_kingdom_id\":\"ID或空\",\"reason\":\"简短理由\"}]}");
		return sb.ToString().TrimEnd();
	}

	private string BuildParticipationPrompt(WorldDiplomacyJob job, List<WorldDiplomacyParticipationRequest> requests)
	{
		WorldDiplomacyRound round = ResolveRound(job.RoundId);
		StringBuilder sb = new StringBuilder();
		string vassalageSnapshot = BuildWorldDiplomacyVassalageSnapshot();
		if (!string.IsNullOrWhiteSpace(vassalageSnapshot)) sb.AppendLine(vassalageSnapshot);
		foreach (WorldDiplomacyParticipationRequest request in requests.OrderBy(x => x?.KingdomId ?? "", StringComparer.OrdinalIgnoreCase))
		{
			Kingdom kingdom = ResolveKingdom(request.KingdomId);
			if (kingdom == null) continue;
			sb.AppendLine("候选：" + kingdom.StringId + "=" + KingdomName(kingdom) + "，统治者=" + RulerName(kingdom));
			string interest = BuildParticipationInterestContext(kingdom, job.TriggerDocumentIds);
			if (!string.IsNullOrWhiteSpace(interest)) sb.AppendLine("  与事件的战略关系：" + interest);
			string policySnapshot = WorldDiplomacyPolicyContext.BuildSnapshot(kingdom.StringId);
			if (!string.IsNullOrWhiteSpace(policySnapshot)) sb.AppendLine("  该王国政策：\n" + policySnapshot);
			string weeklySnapshot = BuildWeeklyDiplomacySnapshot(kingdom.StringId);
			if (!string.IsNullOrWhiteSpace(weeklySnapshot)) sb.AppendLine("  该王国最新周报：" + weeklySnapshot);
			string knownContext = BuildKnownRoundContext(kingdom.StringId, round?.RoundId, 5);
			if (!string.IsNullOrWhiteSpace(knownContext)) sb.AppendLine("  该王庭当前已知：\n" + knownContext);
		}
		sb.AppendLine("活动程度=" + GetActivityLevel().ToString(CultureInfo.InvariantCulture) + "；回合已进行" + Math.Max(0, CurrentDay() - (round?.StartedDay ?? CurrentDay())).ToString(CultureInfo.InvariantCulture) + "天。");
		return sb.ToString();
	}

	private string BuildParticipationInterestContext(Kingdom candidate, IEnumerable<string> triggerDocumentIds)
	{
		if (candidate == null) return "";
		List<Kingdom> involved = (triggerDocumentIds ?? Enumerable.Empty<string>())
			.Select(ResolveDocument)
			.Where(x => x != null)
			.SelectMany(x => new[] { ResolveKingdom(x.AuthorKingdomId), ResolveKingdom(x.TargetKingdomId) })
			.Where(x => x != null && x != candidate)
			.GroupBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
			.Take(4)
			.ToList();
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		List<string> facts = new List<string>();
		foreach (Kingdom other in involved)
		{
			string relation = FactionManager.IsAtWarAgainstFaction(candidate, other)
				? "交战"
				: alliance?.IsAllyWithKingdom(candidate, other) == true ? "同盟" : "中立";
			facts.Add(KingdomName(other) + "=" + relation + ",私人关系=" + DescribeRulerRelation(GetRulerRelation(candidate, other)));
		}
		return string.Join("；", facts);
	}

	private void CommitParticipation(WorldDiplomacyJob job, string raw)
	{
		WorldDiplomacyRound round = ResolveRound(job.RoundId);
		if (round == null || !string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)) return;
		JObject json = ParseJsonObject(raw);
		JArray decisions = json["decisions"] as JArray;
		if (decisions == null) return;
		foreach (JToken token in decisions.Take(MaxParticipationCandidatesPerJob))
		{
			string kingdomId = token?["kingdom_id"]?.ToString()?.Trim() ?? "";
			if (!(job.CandidateKingdomIds ?? new List<string>()).Contains(kingdomId, StringComparer.OrdinalIgnoreCase)) continue;
			Kingdom author = ResolveKingdom(kingdomId);
			if (author == null || IsPlayerKingdom(author) || !HasIndependentWorldDiplomacyAuthority(author)) continue;
			string state = NormalizeToken(token?["state"]?.ToString());
			if (state != "active" && state != "withdrawn") state = "observer";
			WorldDiplomacyRoundParticipant participant = EnsureRoundParticipant(round, kingdomId, state, mandatoryReply: false);
			participant.State = state;
			if (state == "active")
			{
				participant.SelectedForRelay = true;
				AddParticipantToRelayRouteIfNeeded(round, kingdomId);
			}
			else if (state == "withdrawn")
			{
				participant.MandatoryReplyPending = false;
			}
			participant.LastEvaluationDay = CurrentDay();
			participant.LastEvaluationMaterialDay = round.LastActivityDay;
			bool speak = ReadBooleanToken(token?["speak_now"]);
			if (!speak || state == "withdrawn") continue;
			Kingdom target = ResolveKingdom(token?["target_kingdom_id"]?.ToString());
			WorldDiplomacyDocument source = (job.TriggerDocumentIds ?? new List<string>())
				.Where(id => HasKingdomKnowledge(author.StringId, id))
				.Select(ResolveDocument)
				.Where(x => x != null)
				.OrderByDescending(x => x.Day)
				.ThenByDescending(x => x.CreatedUtcTicks)
				.FirstOrDefault();
			target ??= ResolveKingdom(source?.AuthorKingdomId);
			if (source == null || target == null || target == author)
			{
				participant.State = "observer";
				Log("participation speech skipped because no valid known source/target round=" + round.RoundId + " author=" + author.StringId);
				continue;
			}
			participant.State = "active";
			QueuePendingSpeech(round, author, target, source, priority: 45);
		}
		TryDispatchPendingSpeeches();
	}

	private void QueuePendingSpeech(WorldDiplomacyRound round, Kingdom author, Kingdom target, WorldDiplomacyDocument source, int priority)
	{
		if (round == null || author == null || target == null || source == null || author == target || round.AutomaticCircuitBreakerTripped) return;
		_storage.PendingSpeeches ??= new List<WorldDiplomacyPendingSpeech>();
		WorldDiplomacyPendingSpeech pending = _storage.PendingSpeeches.FirstOrDefault(x => x != null
			&& string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.AuthorKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase));
		if (pending == null)
		{
			pending = new WorldDiplomacyPendingSpeech
			{
				RoundId = round.RoundId,
				AuthorKingdomId = author.StringId,
				QueuedDay = CurrentDay()
			};
			_storage.PendingSpeeches.Add(pending);
		}
		pending.TargetKingdomId = target.StringId;
		pending.SourceDocumentId = source.DocumentId;
		pending.Priority = Math.Max(pending.Priority, priority);
		List<WorldDiplomacyPendingSpeech> ordered = _storage.PendingSpeeches
			.Where(x => x != null)
			.OrderByDescending(x => x.Priority)
			.ThenBy(x => x.QueuedDay)
			.ThenBy(x => x.AuthorKingdomId, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (ordered.Count > MaxPendingSpeeches)
		{
			foreach (WorldDiplomacyPendingSpeech evicted in ordered.Skip(MaxPendingSpeeches))
			{
				Log("pending speech evicted by safety cap round=" + (evicted.RoundId ?? "") + " author=" + (evicted.AuthorKingdomId ?? ""));
			}
		}
		_storage.PendingSpeeches = ordered.Take(MaxPendingSpeeches).ToList();
		Log("pending speech queued round=" + round.RoundId + " author=" + author.StringId + " target=" + target.StringId);
	}

	private void TryDispatchPendingSpeeches()
	{
		if (_storage.PendingSpeeches == null || _storage.PendingSpeeches.Count == 0) return;
		WorldDiplomacyRound activeRound = _storage.ActiveRound;
		_storage.PendingSpeeches.RemoveAll(x => x == null || activeRound == null
			|| !string.Equals(x.RoundId, activeRound.RoundId, StringComparison.OrdinalIgnoreCase)
			|| activeRound.AutomaticCircuitBreakerTripped);
		if (_storage.PendingSpeeches.Count == 0) return;
		if ((activeRound.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Any(x => x?.MandatoryReplyPending == true)) return;
		foreach (WorldDiplomacyPendingSpeech pending in _storage.PendingSpeeches
			.OrderByDescending(x => x.Priority)
			.ThenBy(x => x.QueuedDay)
			.ThenBy(x => x.AuthorKingdomId, StringComparer.OrdinalIgnoreCase)
			.ToList())
		{
			Kingdom author = ResolveKingdom(pending.AuthorKingdomId);
			Kingdom target = ResolveKingdom(pending.TargetKingdomId);
			WorldDiplomacyDocument source = ResolveDocument(pending.SourceDocumentId);
			if (author == null || target == null || source == null || author == target
				|| HasKingdomRespondedToDocument(author?.StringId, source?.DocumentId))
			{
				_storage.PendingSpeeches.Remove(pending);
				continue;
			}
			if (_storage.Jobs.Any(x => x != null && string.Equals(x.RoundId, pending.RoundId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.AuthorKingdomId, author.StringId, StringComparison.OrdinalIgnoreCase))) continue;
			if (!TryConsumeAiDocumentBudget()) break;
			_storage.PendingSpeeches.Remove(pending);
			EnqueueGenerationJob(author, target, null, isResponse: true, forcedIntent: ResolveArmedIntent(author, target), sourceDocument: source, priority: pending.Priority, roundId: pending.RoundId);
			Log("pending speech dispatched round=" + pending.RoundId + " author=" + author.StringId + " queuedDay=" + pending.QueuedDay.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static bool ReadBooleanToken(JToken token)
	{
		string value = token?.ToString()?.Trim() ?? "";
		return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
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
			CloseActiveRound(round.SubstantiveProgressCount > 0
				? "relay_hard_end"
				: "technical_no_substantive_progress");
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
			CloseActiveRound(round.SubstantiveProgressCount > 0
				? "relay_all_ai_withdrew"
				: "technical_no_substantive_participants");
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
				EnqueueGenerationJob(author, player, null, isResponse: true, forcedIntent: ResolveArmedIntent(author, player), sourceDocument: source, priority: 80, externalResponseOnly: true, isReminder: true, roundId: round.RoundId);
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
				: round.SubstantiveProgressCount > 0 ? "deadlocked" : "closed";
		}
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase))) offer.Status = "expired";
		List<WorldDiplomacyDocument> documents = _storage.Documents.Where(x => x != null && string.Equals(x.RoundId, round.RoundId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
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
		Log("round closed round=" + round.RoundId
			+ " reason=" + round.CloseReason
			+ " documents=" + documents.Count.ToString(CultureInfo.InvariantCulture)
			+ " substantiveProgress=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture)
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
			if (!string.IsNullOrWhiteSpace(document.MechanicalResult))
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
			if (!string.IsNullOrWhiteSpace(document.MechanicalResult)) summary.Facts.Add(new WorldDiplomacyRoundFact
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
		List<string> results = (documents ?? new List<WorldDiplomacyDocument>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.MechanicalResult))
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

	private WorldDiplomacyRound ResolveRound(string roundId)
	{
		if (string.IsNullOrWhiteSpace(roundId)) return null;
		if (_storage.ActiveRound != null && string.Equals(_storage.ActiveRound.RoundId, roundId, StringComparison.OrdinalIgnoreCase)) return _storage.ActiveRound;
		return _storage.CompletedRounds.FirstOrDefault(x => x != null && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase));
	}

	private void TrySettleRoundAction(WorldDiplomacyDocument response)
	{
		if (response == null || !response.IsResponse || !IsAcceptanceIntent(NormalizeIntent(response.Intent))) return;
		WorldDiplomacyDocument source = ResolveDocument(response.SourceDocumentId);
		if (source == null) return;
		Kingdom initiator = ResolveKingdom(source.AuthorKingdomId);
		Kingdom target = ResolveKingdom(response.AuthorKingdomId);
		if (initiator == null || target == null) return;
		string pending = NormalizeIntent(source.Intent);
		if (pending == "propose_peace")
		{
			response.PeaceTerms ??= source.PeaceTerms;
			ExecuteMakePeace(initiator, target, response);
		}
		else if (pending == "propose_alliance") ExecuteAlliance(initiator, target, response);
		else if (pending == "propose_trade") ExecuteTradeAgreement(initiator, target, response);
	}

	private void TrySettleRelayOffer(WorldDiplomacyDocument document)
	{
		WorldDiplomacyRound round = ResolveRound(document?.RoundId);
		if (round == null || document == null) return;
		round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
		string intent = NormalizeIntent(document.Intent);
		if (IsProposalIntent(intent) && !string.IsNullOrWhiteSpace(document.TargetKingdomId))
		{
			if (intent == "propose_peace")
			{
				Kingdom peaceProposer = ResolveKingdom(document.AuthorKingdomId);
				Kingdom peaceTarget = ResolveKingdom(document.TargetKingdomId);
				if (peaceProposer == null || peaceTarget == null || !FactionManager.IsAtWarAgainstFaction(peaceProposer, peaceTarget))
				{
					Log("illegal peace offer not registered document=" + document.DocumentId
						+ " author=" + document.AuthorKingdomId + " target=" + document.TargetKingdomId);
					return;
				}
				SetLastForcedPeaceProposalDay(ResolveWarLedger(peaceProposer.StringId, peaceTarget.StringId), peaceProposer.StringId, CurrentDay());
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
		if (offer == null) return;
		if (intent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase))
		{
			offer.Status = "rejected";
			return;
		}
		offer.Status = "accepted";
		if (!string.IsNullOrWhiteSpace(document.MechanicalResult)) return;
		WorldDiplomacyDocument source = ResolveDocument(offer.SourceDocumentId);
		Kingdom proposer = ResolveKingdom(offer.ProposerKingdomId);
		Kingdom target = ResolveKingdom(offer.TargetKingdomId);
		if (source == null || proposer == null || target == null) return;
		if (proposalIntent == "propose_peace")
		{
			document.PeaceTerms ??= source.PeaceTerms;
			ExecuteMakePeace(proposer, target, document);
		}
		else if (proposalIntent == "propose_alliance") ExecuteAlliance(proposer, target, document);
		else if (proposalIntent == "propose_trade") ExecuteTradeAgreement(proposer, target, document);
	}

	private void TrySettleBilateralAction(WorldDiplomacyExchange exchange, WorldDiplomacyDocument response)
	{
		if (exchange == null || response == null || string.IsNullOrWhiteSpace(exchange.PendingAction))
		{
			return;
		}
		string responseIntent = NormalizeIntent(response.Intent);
		if (!IsAcceptanceIntent(responseIntent))
		{
			return;
		}
		Kingdom initiator = ResolveKingdom(exchange.InitiatorKingdomId);
		Kingdom target = ResolveKingdom(exchange.TargetKingdomId);
		if (initiator == null || target == null)
		{
			return;
		}
		string pending = NormalizeIntent(exchange.PendingAction);
		if (pending == "propose_peace")
		{
			ExecuteMakePeace(initiator, target, response);
		}
		else if (pending == "propose_alliance")
		{
			ExecuteAlliance(initiator, target, response);
		}
		else if (pending == "propose_trade")
		{
			ExecuteTradeAgreement(initiator, target, response);
		}
	}

	private void ExecuteImmediateIntent(Kingdom author, Kingdom target, string intent, bool forced, WorldDiplomacyDocument document)
	{
		if (intent == "declare_war")
		{
			if (!CanDeclareWar(author, target, forced, out string blockReason))
			{
				document.MechanicalResult = "宣战未执行：" + blockReason;
				return;
			}
			RunDiplomaticAction("world_diplomacy_declare_war", delegate
			{
				IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
				if (alliance != null && alliance.IsAllyWithKingdom(author, target))
				{
					alliance.EndAlliance(author, target);
				}
				DeclareWarAction.ApplyByKingdomDecision(author, target);
			});
			document.MechanicalResult = "已宣战";
			ClearWarPressure(author.StringId, target.StringId);
			_storage.LastOffensiveWarDayByKingdom[author.StringId] = CurrentDay();
			return;
		}
		if (intent == "break_alliance")
		{
			IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (alliance != null && alliance.IsAllyWithKingdom(author, target))
			{
				RunDiplomaticAction("world_diplomacy_break_alliance", () => alliance.EndAlliance(author, target));
				document.MechanicalResult = "已解除同盟";
			}
			return;
		}
		if (intent == "cancel_trade")
		{
			ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (trade != null && BannerlordApiCompat.HasTradeAgreement(trade, author, target))
			{
				RunDiplomaticAction("world_diplomacy_cancel_trade", () => trade.EndTradeAgreement(author, target));
				document.MechanicalResult = "已终止贸易协定";
			}
		}
	}

	private void ExecuteMakePeace(Kingdom initiator, Kingdom target, WorldDiplomacyDocument document)
	{
		if (!FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
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
		string pairKey = PairKey(initiator.StringId, target.StringId);
		_storage.LastPeaceDayByPair[pairKey] = CurrentDay();
		ClearWarPressure(initiator.StringId, target.StringId);
		ClearWarPressure(target.StringId, initiator.StringId);
		string cessionResult = TryApplyValidatedCession(terms, initiator, target);
		document.MechanicalResult = "双方已达成和平"
			+ (appliedTribute > 0 ? "；" + KingdomName(payer) + "每日向" + KingdomName(receiver) + "支付" + appliedTribute.ToString(CultureInfo.InvariantCulture) + "第纳尔，共" + appliedDays.ToString(CultureInfo.InvariantCulture) + "天" : "")
			+ cessionResult;
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
			return;
		}
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliance == null || alliance.IsAllyWithKingdom(initiator, target))
		{
			return;
		}
		RunDiplomaticAction("world_diplomacy_alliance", () => alliance.StartAlliance(initiator, target));
		document.MechanicalResult = "双方已缔结同盟";
	}

	private void ExecuteTradeAgreement(Kingdom initiator, Kingdom target, WorldDiplomacyDocument document)
	{
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			return;
		}
		ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		if (trade == null || BannerlordApiCompat.HasTradeAgreement(trade, initiator, target))
		{
			return;
		}
		CampaignTime duration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(initiator, target);
		RunDiplomaticAction("world_diplomacy_trade", () => trade.MakeTradeAgreement(initiator, target, duration));
		document.MechanicalResult = "双方已缔结贸易协定";
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

	private bool CanDeclareWar(Kingdom initiator, Kingdom target, bool forcedByThreshold, out string reason)
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
				EnqueueGenerationJob(author, player, exchange, isResponse: true, forcedIntent: "", sourceDocument: source, priority: 80, externalResponseOnly: true, isReminder: true);
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
		StartDocumentPropagation(fact, initiator);
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
			if (IsDiplomaticSituationAutoAdvanceEnabled() && entry.Value >= GetDiplomaticAdvanceThreshold())
			{
				entry.IsEscalationArmed = true;
				entry.ArmedDay = CurrentDay();
			}
		}
		if (entry.Value < GetDiplomaticAdvanceReleaseThreshold())
		{
			entry.IsEscalationArmed = false;
			entry.ArmedDay = 0;
		}
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
		SyncDiplomaticAdvanceToggleState();
		int day = CurrentDay();
		foreach (WarPressureEntry entry in _storage.WarPressure)
		{
			if (entry == null || entry.Value <= 0 || day - entry.LastUpdatedDay < 7)
			{
				continue;
			}
			entry.Value = Math.Max(0, entry.Value - 4);
			if (entry.Value < GetDiplomaticAdvanceReleaseThreshold())
			{
				entry.IsEscalationArmed = false;
				entry.ArmedDay = 0;
			}
		}
	}

	private WarPressureEntry FindWarPressure(string sourceId, string targetId)
	{
		return _storage.WarPressure.FirstOrDefault(x => x != null && string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
	}

	private void SyncDiplomaticAdvanceToggleState()
	{
		bool enabled = IsDiplomaticSituationAutoAdvanceEnabled();
		if (!enabled)
		{
			foreach (WarPressureEntry entry in _storage.WarPressure.Where(x => x != null)) entry.IsEscalationArmed = false;
			_storage.ForcedWarToggleWasEnabled = false;
			return;
		}
		if (_storage.ForcedWarToggleWasEnabled) return;
		foreach (WarPressureEntry entry in _storage.WarPressure.Where(x => x != null))
		{
			entry.IsEscalationArmed = false;
			entry.NeedsFreshEscalation = true;
		}
		_storage.ForcedWarToggleWasEnabled = true;
	}

	private void TryScheduleTokenCompression()
	{
		long threshold = GetCompressionThresholdTokens();
		if (_storage.DiplomacyTokensSinceCompression >= threshold) _storage.DiplomacyCompressionPending = true;
		if (!_storage.DiplomacyCompressionPending || _storage.ActiveRound != null || CurrentHour() < _storage.CompressionRetryAfterHour) return;
		if (_llmRequestRunning || _storage.Jobs.Any(x => x != null)) return;
		List<WorldDiplomacyRoundSummary> summaries = _storage.RoundSummaries
			.Where(x => x != null && !x.IsTokenCompressed && !string.IsNullOrWhiteSpace(x.RoundId))
			.OrderBy(x => x.CreatedDay).ThenBy(x => x.RoundId, StringComparer.OrdinalIgnoreCase).Take(MaxStoredRoundSummaries).ToList();
		if (summaries.Count == 0)
		{
			// 没有可整理的历史时保留累计值，等下一场外交结束后再尝试。
			return;
		}
		EnqueueCompressionJob(summaries, _storage.DiplomacyTokensSinceCompression);
	}

	private void CommitCompression(WorldDiplomacyJob job, string raw)
	{
		JObject json = ParseJsonObject(raw);
		string summaryText = NormalizeBody(ReadString(json, "summary", "diplomatic_memory", "body"));
		if (string.IsNullOrWhiteSpace(summaryText))
		{
			summaryText = BuildFallbackTokenCompressionSummary(job.CompressionRoundIds);
		}
		HashSet<string> roundIds = new HashSet<string>(job.CompressionRoundIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		List<WorldDiplomacyRoundSummary> sourceSummaries = _storage.RoundSummaries.Where(x => x != null && roundIds.Contains(x.RoundId ?? "")).ToList();
		WorldDiplomacyCompressionSummary summary = new WorldDiplomacyCompressionSummary
		{
			BatchId = FirstNonEmpty(job.CompressionBatchId, "diplomacy_compaction_" + (_storage.CompressionSequence + 1).ToString(CultureInfo.InvariantCulture)),
			Summary = summaryText,
			CreatedDay = CurrentDay(),
			StartDay = sourceSummaries.Count == 0 ? CurrentDay() : sourceSummaries.Min(x => x.CreatedDay),
			EndDay = sourceSummaries.Count == 0 ? CurrentDay() : sourceSummaries.Max(x => x.CreatedDay),
			TokenCount = Math.Max(0L, job.CompressionTokenCount),
			SourceRoundIds = roundIds.ToList(),
			KingdomIds = sourceSummaries.SelectMany(x => x.KingdomIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			ConfirmedResults = sourceSummaries.SelectMany(x => x.Facts ?? new List<WorldDiplomacyRoundFact>())
				.Where(x => x != null && string.Equals(x.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase))
				.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(48).ToList()
		};
		_storage.CompressionSummaries.RemoveAll(x => x != null && string.Equals(x.BatchId, summary.BatchId, StringComparison.OrdinalIgnoreCase));
		_storage.CompressionSummaries.Add(summary);
		foreach (WorldDiplomacyRoundSummary roundSummary in sourceSummaries)
		{
			roundSummary.IsTokenCompressed = true;
			roundSummary.CompressionBatchId = summary.BatchId;
		}
		HashSet<string> compressedDocumentIds = new HashSet<string>(sourceSummaries
			.SelectMany(x => x.SourceDocumentIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		foreach (WorldDiplomacyDocument document in _storage.Documents.Where(x => x != null && compressedDocumentIds.Contains(x.DocumentId ?? "")))
		{
			document.IsCompressed = true;
		}
		foreach (WorldDiplomacyRound round in _storage.CompletedRounds.Where(x => x != null && roundIds.Contains(x.RoundId ?? "")))
		{
			round.LlmTranscript?.Clear();
			round.LlmProfiledKingdomIds?.Clear();
			round.LlmLastStateSignatureByKingdom?.Clear();
			round.CachePrefix = "";
		}
		_storage.CompressionSequence = Math.Max(_storage.CompressionSequence + 1, ParseCompressionSequence(summary.BatchId));
		_storage.LastDiplomacyCompressionDay = CurrentDay();
		_storage.CompressionRetryAfterHour = 0;
		_storage.DiplomacyTokensSinceCompression = Math.Max(0L, _storage.DiplomacyTokensSinceCompression - Math.Max(0L, job.CompressionTokenCount));
		_storage.DiplomacyCompressionPending = _storage.DiplomacyTokensSinceCompression >= GetCompressionThresholdTokens();
		Log("token compression committed batch=" + summary.BatchId
			+ " rounds=" + sourceSummaries.Count.ToString(CultureInfo.InvariantCulture)
			+ " remaining_tokens=" + _storage.DiplomacyTokensSinceCompression.ToString(CultureInfo.InvariantCulture));
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
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(DiplomacySystemContractMarker);
		sb.AppendLine("你正在处理卡拉迪亚诸王国之间的公开外交。无论当前任务是规划参与国、起草宣言、判断语义还是整理回合，都必须先遵守本共同契约；后面的任务说明只能补充它，不能推翻它。");
		sb.AppendLine("【事实与知识边界】");
		sb.AppendLine("游戏直接提供的王国状态、战争状态、统治者、亲属、领地、关系、合法动作与已发生事件是硬事实，优先级最高。王庭只能依据已经送达或明确放入当前档案的宣言作出反应；尚在传播途中的内容、别国秘密政策、模型记忆和常识推测都不能冒充已知事实。");
		sb.AppendLine("政策快照描述统治目标、利益取向与内部压力，只能影响决策倾向，不能证明政策已经产生未给出的军事或外交结果。周报是可能滞后的近期概括，只能提供背景；它与即时硬事实冲突时，以即时硬事实为准。不得从沉默推导接受，不得从敌意推导已经宣战，不得从同文化、同阵营或王室身份推导未列出的亲属关系。");
		sb.AppendLine("没有材料支持时，不得补写具体战斗地点、胜负、兵力、伤亡、密约、领土承诺、使节往来或人物动机。可以根据明确关系和利益作审慎判断，但要把判断保持为立场或可能性，不能写成已经发生的事实。");
		sb.AppendLine("【外交动作与回合原则】");
		sb.AppendLine("公开宣言应当服务于可理解的外交目的，例如提出或修改条件、争取支持、施加压力、接受、拒绝、让步、道歉、警告、发出最后通牒、退出交涉，或执行当前游戏允许的正式动作。谴责和反驳可以出现，但不能代替进展；同一回合不应只改写旧立场或无限争吵。");
		sb.AppendLine("正式动作必须同时满足语义和游戏合法性。威胁战争不等于宣战；已经交战的双方不能再次宣布开战。和平、联盟、贸易、解除关系、贡金和割地只能使用任务提供的合法对象与条件。明确指向某国表示它承担主张或需要回应；只是举例、评价或谈到某国时，只算提及。");
		sb.AppendLine("每项待回应提议都有唯一的提出国、对象国和来源公文。只有该对象国可以接受或拒绝，答复对象必须是原提出国；第三国可以评论、施压、调停或另行提出一份明确的新提议，但不得把别国收到的提议说成发给自己，也不得替真正的对象国作答。接受或拒绝时填写对应的responding_to_offer_document_id；另行提出新提议时该字段必须为空。");
		sb.AppendLine("任务提供的宗主—臣属关系是当前世界硬事实。臣属国不得否认现存条约或把宗主国写成普通平等国家；提及宗主国或代表本国对外表态时，应承认宗主地位并保持符合臣属礼制的恭敬，但不必在无关公文中反复颂扬。朝贡国和卫戍国仍可按任务给出的权限表达自身利益；完全没有外交自主权的附庸国不会作为外交回合发言者，其外交事务由宗主国出面。");
		sb.AppendLine("外交回合应在当前任务给出的时间内形成结果或明确停滞。参与国可以改变目标、回应第三国、提出反方案、退出或采取实质动作，但不得替玩家统治的王国自动发言。回合结束必须来自问题解决、合法动作已经触发、相关国家陆续退出或交涉确实破裂，而不是模型随意遗忘议题。");
		sb.AppendLine("每个正常运转的外交回合在结束前至少要有一次可由程序核验的实质外交尝试。正式提议、反提议、对真实提议的接受或拒绝、最后通牒、明确道歉或让步，以及成功执行的外交机制可以计入；普通声明、谴责、泛泛警告、评论和重复旧立场不计入。made_progress只是输出字段，最终是否计入由程序依据意图、提议归属和机械结果核验，模型不得自行宣布已经推进。");
		sb.AppendLine("【身份、表达与输出纪律】");
		sb.AppendLine("必须保持王国、统治者和ID一一对应，只使用当前任务给出的合法ID。不得泄露AI、模型、提示词、缓存、数值阈值、程序字段或系统内部机制。输出要求JSON时只输出可解析JSON，不加代码围栏、解释、前言或尾注；布尔值、数字、数组和空字符串必须保持约定类型。");
		sb.AppendLine("统治者把卡拉迪亚视为自己生活的真实世界，只能以使者来报、公开宣言、战报、俘虏、领地得失、王庭账簿和可见军情来理解局势；不得自称处于游戏或某个历史时代，也不知道战争进展分、议和开放度、劣势评分、关系点、战争压力阈值或总战力数值。后台态势只能转化成势均力敌、略占上风、处境不利、愿听条件等世界内判断，不能复述指标名称或数值。第纳尔金额、条约期限、领地名称、实际战斗场数等可核实事实可以按需准确表达。");
		sb.AppendLine("当任务要求撰写玩家可见的外交宣言时，它必须是一份能够独立颁布、传阅和归档的国家公文，而不是君主之间的即时聊天。王国是政治主体，统治者负责授权、定调或署名；人物差异通过利益判断、承诺、威胁与让步的分寸体现，国家差异则通过档案明确提供的制度、合法性来源、政治共同体和礼制称谓体现。");
		return sb.ToString();
	}

	private static void AppendWorldDiplomacyCustomPrompt(StringBuilder sb)
	{
		if (sb == null) return;
		string customPrompt = DuelSettings.GetWorldDiplomacyPromptForExternal();
		if (string.IsNullOrWhiteSpace(customPrompt)) return;
		sb.AppendLine("【玩家自定义AI外交偏好】");
		sb.AppendLine(customPrompt.Trim());
		sb.AppendLine("自定义偏好只影响利益判断、行动取向与文风，不得覆盖共同契约、事实边界、合法动作条件和输出格式。");
	}

	private static void AppendDiplomaticDeclarationWritingContract(StringBuilder sb)
	{
		if (sb == null) return;
		sb.AppendLine("【国家外交公文文体契约】");
		sb.AppendLine("正文是一份面向诸国、本国贵族与臣民公开颁布的外交文书，必须脱离上下文也能成立。此前的宣言只是已经送抵并归档的别国公文，不是正在发言的聊天对象。普通宣言不得用“你”“你的”“你们”持续向另一位君主说话；使用对方国名、“贵国”或其明确制度称谓。禁止“让我说说”“你应该谢我”“你自己选”“那我就……”“等你答复”等私人回嘴句式。");
		sb.AppendLine("国家而非君主私交是叙述中心。正文至少自然出现一次能够代表政治共同体的称谓，例如王国名、帝国、王庭、诸侯与贵族、臣民、军队、商旅或边地；由统治者的个人性格决定语气和取舍，但不能把国家决定写成几位君主私下讨价还价。不得虚构贵族已经集会、投票、宣誓或一致同意。");
		sb.AppendLine("知识库提供的制度、合法性来源、历史身份和礼制称谓可以自然进入正文：若明确记载元老院、汗庭、部族、议政传统或其他机构，发文国可用它们说明权威与责任；未明确提供时只能使用中性称谓，不得根据文化刻板印象自创制度。机构身份与统治者个人头衔必须严格区分：元老院制不等于皇帝本人是元老，军功制不等于皇帝是将军，君主制也不能被改写成元老院制。发文档案给出的ruler_title_hard_fact与government_hard_fact优先于人物背景和检索材料，绝不可改称。世界观材料用于决定称谓和立场，不得整段照抄编年史；每篇最多使用一处有辨识度的文化意象，不能堆砌口号。");
		sb.AppendLine("文风应正式、克制并有国家分量，但不是僵硬的八股模板。开头直接说明事件、判断、决定或条件，不逐一点名所有君主和头衔，不反复自报身份；避免机械套用“回顾—原则—要求—后果”的固定段式，也不要每篇都先致意、再遗憾、最后威胁。需要正式确认的条约可以分项，普通宣言优先用连贯段落表达。");
		sb.AppendLine("可以坚定、务实、冷峻、和缓或骄傲，但讥讽也必须是一个国家对另一个国家的公开评价，不能写成两个人斗嘴。整体要像中世纪国家文书的清楚现代中文译文：不写文言文、半文半白、现代新闻稿、现代法律或国际组织话术，也不堆砌官样套话。直接称呼别国统治者只限于个人誓约或最后通牒的一两句核心文字，其余部分仍由国家作为主语。");
		sb.AppendLine("按内容自然分段，不强求固定段数；正文超过约180字时至少分成两段，不能挤成一个大段落。贡金、停战期限、开放商路、割地或盟约条件应写成完整而清晰的国家主张；没有新条件时宁可简短明确，也不要用空话扩写。标题应抓住国家决定或外交事件本身，避免万能标题和收信人格式。");
		sb.AppendLine("不要把供决策的后台态势照抄进正文。不能说战争进展领先多少分、议和开放度或劣势评分达到多少、关系点和战力值是多少；应改成由战报和现实结果支撑的自然判断。精确贡金、停战期限及其他正式条款不受此限制。");
	}

	private static string BuildDiplomaticDeclarationSystemPrompt()
	{
		StringBuilder sb = new StringBuilder(BuildCommonDiplomacySystemPrefix());
		AppendWorldDiplomacyCustomPrompt(sb);
		AppendDiplomaticDeclarationWritingContract(sb);
		sb.AppendLine("【统一任务：公开外交宣言】根据用户消息提供的档案，为指定王国起草一篇由其统治者授权或署名、面向诸国与本国贵族臣民颁布的外交宣言。用户消息含“本回合冻结档案”时属于接力发文，否则属于开场或普通发文；两种模式使用完全相同的输出结构。");
		sb.AppendLine("所有宣言都必须推动外交：提出或修改方案、寻求支持、施加压力、接受、拒绝、让步、道歉、发出最后通牒、退出，或执行合法外交动作。可以谴责和反驳，但不得只重复情绪与旧立场。");
		sb.AppendLine("开场宣言可以明确指向一个或多个王国，也可以面向诸国而没有主要对象；没有主要对象时primary_target_kingdom_id必须为空字符串，addressed_kingdom_ids可以为空。接力发言只能把冻结档案列出的参与国作为外交动作对象，并应根据动态尾部给出的进度及时收束争议。");
		sb.AppendLine("不要使用现代国际组织、现代法律或现代媒体措辞。不得替玩家王国发言，不得编造输入中没有支持的领土、制度、亲属关系、战斗和事件。优先使用王国名、“我国”“王庭”或档案明确给出的政治共同体称谓。第一人称单数“我”原则上不超过两次，只能用于统治者承担个人誓言或责任；不能用它串起整篇文章。“本王”整篇最多一次。");
		sb.AppendLine("和平类意图受战争状态硬约束：propose_peace、accept_peace、reject_peace只能指向当前确实正在交战的王国。双方处于和平状态时，不得把政治分歧、敌对关系、历史内战或统一诉求写成正在交战、停战、议和、退出战争或战争补偿；可以改为声明、警告、通牒、贸易、结盟或其他符合现状的外交主张。");
		sb.AppendLine("标题应简洁概括事件或决定，通常不超过20个汉字。requires_response只表示正文提出了尚待回答的新提案、反提案、最后通牒或明确问题；接受、拒绝、道歉、普通声明、普通谴责和结束性立场必须为false。");
		sb.AppendLine("接力模式必须填写round_participation、round_status和made_progress；开场或普通模式固定使用continue、continue和true，这些字段只供系统推进回合，不能写入玩家可见正文。");
		sb.AppendLine("用户消息含“同次确定本回合参与国”时，必须填写round_plan：topic概括本回合真实议题，selected_kingdom_ids从候选简表中选择，并包含宣言明确指向的王国。没有该段时round_plan使用空标题和空数组。");
		sb.AppendLine("只输出一个JSON对象，不要代码围栏：");
		sb.AppendLine("{\"title\":\"简短外交宣言标题\",\"body\":\"自然分段的完整外交宣言正文\",\"author_intent\":{\"intent\":\"statement|condemn|warning|ultimatum|apology|concession|propose_peace|accept_peace|reject_peace|propose_alliance|accept_alliance|reject_alliance|break_alliance|propose_trade|accept_trade|reject_trade|cancel_trade|declare_war\",\"commitment\":\"non_binding|proposal|acceptance|rejection|binding\"},\"responding_to_offer_document_id\":\"接受或拒绝时填写来源公文ID，否则为空\",\"primary_target_kingdom_id\":\"主要对象ID或空\",\"addressed_kingdom_ids\":[\"ID\"],\"mentioned_kingdom_ids\":[\"ID\"],\"requires_response\":false,\"tone\":\"conciliatory|neutral|firm|hostile\",\"confidence\":0.0,\"round_participation\":\"continue|withdraw\",\"round_status\":\"continue|resolved|deadlocked\",\"made_progress\":true,\"round_plan\":{\"topic\":\"简短议题或空\",\"selected_kingdom_ids\":[\"ID\"]},\"peace_terms\":{\"tribute_payer_kingdom_id\":\"ID或空\",\"tribute_receiver_kingdom_id\":\"ID或空\",\"daily_tribute\":0,\"duration_days\":0,\"cession_from_kingdom_id\":\"ID或空\",\"cession_to_kingdom_id\":\"ID或空\",\"cession_settlement_id\":\"允许清单中的ID或空\"}}");
		return sb.ToString().TrimEnd();
	}

	private static string BuildGenerationSystemPrompt()
	{
		return BuildDiplomaticDeclarationSystemPrompt();
	}

	private string BuildWeeklyDiplomacySnapshot(string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return "";
		}
		int weekIndex = Math.Max(0, CurrentDay() / 7);
		if (_weeklyDiplomacySnapshotCache.TryGetValue(id, out WeeklyDiplomacySnapshotCacheEntry cached)
			&& cached.WeekIndex == weekIndex)
		{
			return cached.Text ?? "";
		}
		string summary = Limit(MyBehavior.GetLatestKingdomWeeklyShortSummaryForExternal(id), 500).Trim();
		_weeklyDiplomacySnapshotCache[id] = new WeeklyDiplomacySnapshotCacheEntry
		{
			WeekIndex = weekIndex,
			Text = summary
		};
		return summary;
	}

	private string BuildGenerationPrompt(
		Kingdom author,
		Kingdom target,
		WorldDiplomacyExchange exchange,
		bool isResponse,
		string forcedIntent,
		WorldDiplomacyDocument sourceDocument,
		bool isReminder,
		string roundId,
		bool allowUntargeted,
		List<string> roundPlanCandidateIds)
	{
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
		string recentDocuments = BuildRecentBilateralDocumentContext(authorId, targetId, 5);
		string completedRoundHistory = BuildRelevantCompletedRoundContext(authorId, targetId);
		string compressedHistory = BuildRelevantCompressedDiplomacyContext(authorId, targetId, 2);
		string policySnapshot = WorldDiplomacyPolicyContext.BuildSnapshot(authorId);
		string weeklySnapshot = BuildWeeklyDiplomacySnapshot(authorId);
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
		if (!string.IsNullOrWhiteSpace(weeklySnapshot))
		{
			sb.AppendLine("【发文国最新周报摘要】");
			sb.AppendLine(weeklySnapshot);
			sb.AppendLine("周报是截至发布时的概括，可能滞后于下方即时硬事实；发生冲突时以下方即时状态为准。");
		}
		sb.AppendLine("【本次双边对象与即时事实】");
		sb.AppendLine("主要对象国：" + KingdomName(target) + "（ID=" + targetId + "），统治者：" + RulerName(target));
		if (allowUntargeted)
		{
			sb.AppendLine("该对象国只是帮助构造开场局势的参考，不要求宣言必须指向它。若统治者更适合面向诸国提出外交议题，请把primary_target_kingdom_id留空，并让addressed_kingdom_ids为空。");
		}
		if (!string.IsNullOrWhiteSpace(activeRound?.ExternalOpeningContext))
		{
			sb.AppendLine("【本回合的外部起因】");
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
		}
		sb.AppendLine("统治者私人关系：" + DescribeRulerRelation(relation));
		sb.AppendLine("对象国占有的发文国文化城镇/城堡数量：" + culturalFiefs.ToString(CultureInfo.InvariantCulture));
		sb.AppendLine("边境与政治压力：" + DescribeWarPressure(pressure) + "。这只是王庭的综合判断，不得在公开正文中写成分数、进度或门槛。");
		if (!string.IsNullOrWhiteSpace(nativeReasons))
		{
			sb.AppendLine("近期原版外交动机素材：");
			sb.AppendLine(nativeReasons);
		}
		if (!string.IsNullOrWhiteSpace(recentDocuments))
		{
			sb.AppendLine("当前外交事件内的相关宣言：");
			sb.AppendLine(recentDocuments);
		}
		if (!string.IsNullOrWhiteSpace(completedRoundHistory))
		{
			sb.AppendLine("【最近一次已结束的相关外交事件】");
			sb.AppendLine(completedRoundHistory);
			sb.AppendLine("该事件已经结束，只能作为背景；其中的提议、拒绝和争端都不是本回合等待答复的当前公文。除非出现新的事实，不得原样重启同一场争论。");
		}
		if (!string.IsNullOrWhiteSpace(compressedHistory))
		{
			sb.AppendLine("【相关往期外交记忆】");
			sb.AppendLine(compressedHistory);
			sb.AppendLine("其中的宣言、提案和威胁只表示有关王国曾公开表达这些立场；只有明确标注为游戏已执行的内容才是已经发生的外交结果。");
		}
		if (roundPlanCandidateIds != null && roundPlanCandidateIds.Count > 0)
		{
			sb.AppendLine("【同次确定本回合参与国】");
			sb.AppendLine("在起草开场宣言的同时填写round_plan。宣言明确指向的王国必须入选；其余只选确有战争、同盟、贸易、安全或政治利益者，不要为了热闹选满。候选简表：");
			foreach (string candidateId in roundPlanCandidateIds)
			{
				Kingdom candidate = ResolveKingdom(candidateId);
				if (candidate == null) continue;
				sb.AppendLine(BuildCompactRoundPlanCandidateLine(author, candidate));
			}
		}
		string roundContext = BuildKnownRoundContext(author.StringId, resolvedRoundId, 10);
		if (activeRound != null)
		{
			int age = Math.Max(0, CurrentDay() - activeRound.StartedDay);
			sb.AppendLine("当前外交事件已经持续" + age.ToString(CultureInfo.InvariantCulture) + "天，软时间尺度为" + Math.Max(1, activeRound.SoftEndDay - activeRound.StartedDay).ToString(CultureInfo.InvariantCulture) + "天。接近或超过软尺度时，统治者应更重视收束重复争论、给出最终立场或停止无意义往返；但尚未解决且直接关系本国利益的正式问题不能被假装遗忘。");
		}
		if (!string.IsNullOrWhiteSpace(roundContext))
		{
			sb.AppendLine("当前外交事件中，发文国王庭已经收到的外交宣言（按实际传播结果）：");
			sb.AppendLine(roundContext);
			sb.AppendLine("只能依据这里已经抵达的宣言作出回应，不得知晓仍在路上的宣言。");
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
		string normalizedForced = NormalizeIntent(forcedIntent);
		if (!string.IsNullOrWhiteSpace(normalizedForced))
		{
			sb.AppendLine("底层战略已经确定本次正式行为：" + normalizedForced + "。你不能把它改成讨论、威胁或其他行为；正文必须用清楚、无歧义的正式措辞公开这一行为。");
		}
		else
		{
			int activity = GetActivityLevel();
			sb.AppendLine(activity switch
			{
				0 => "外交活跃程度为低：优先克制、审慎和现实利益，但严重矛盾仍可升级。",
				2 => "外交活跃程度为高：应更积极提出可回应的主张、合作或冲突方案，但不得无理由发动战争。",
				_ => "外交活跃程度为标准：在讨论、合作、冲突和正式行动之间按局势自然选择。"
			});
		}
		return sb.ToString();
	}

	private string BuildCompactRoundPlanCandidateLine(Kingdom initiator, Kingdom candidate)
	{
		string policy = CompactPromptFact(WorldDiplomacyPolicyContext.BuildSnapshot(candidate.StringId), 180);
		string weekly = CompactPromptFact(BuildWeeklyDiplomacySnapshot(candidate.StringId), 120);
		StringBuilder sb = new StringBuilder();
		sb.Append("- ").Append(candidate.StringId).Append('=').Append(KingdomName(candidate))
			.Append("；与发起国=").Append(BuildBilateralState(initiator, candidate))
			.Append("；私人关系=").Append(DescribeRulerRelation(GetRulerRelation(initiator, candidate)));
		if (!string.IsNullOrWhiteSpace(policy)) sb.Append("；政策倾向=").Append(policy);
		if (!string.IsNullOrWhiteSpace(weekly)) sb.Append("；近期背景=").Append(weekly);
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
		int advanceThreshold = GetDiplomaticAdvanceThreshold();
		if (IsDiplomaticSituationAutoAdvanceEnabled() && snapshot.AuthorPeacePressure >= advanceThreshold)
		{
			sb.AppendLine("硬性推进要求：发文国承受的战争压力已经达到自动推进门槛。本国本次不能只谈战况或继续谴责，必须提出一份合法和平方案；若正在回应对方的和平提议，则必须接受或提出能够继续成交的明确反方案。");
		}
		if (IsDiplomaticSituationAutoAdvanceEnabled() && snapshot.TargetPeacePressure >= advanceThreshold)
		{
			sb.AppendLine("对象国也已承受足以认真议和的战争压力。若本国提出条件，应给出对方能够接受或反提的现实方案，不得把议和写成纯粹羞辱或无法履行的空话。");
		}
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
		float threshold = Math.Max(1f, GetDiplomaticAdvanceThreshold());
		if (pressure < threshold * 0.35f) return "几乎无意谈和";
		if (pressure < threshold) return "暂不急于谈和，但会衡量条件";
		if (pressure < threshold * 1.5f) return "愿意主动提出或认真回应和平条件";
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
		int threshold = Math.Max(1, GetDiplomaticAdvanceThreshold());
		if (pressure * 3 < threshold) return "压力较低";
		if (pressure < threshold) return "压力正在积累";
		if (pressure < threshold * 2) return "压力很高";
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

	private static string BuildAnalysisSystemPrompt()
	{
		StringBuilder sb = new StringBuilder(BuildCommonDiplomacySystemPrefix());
		sb.AppendLine("【任务：外交宣言语义裁判】读懂已经发布的宣言表达了什么，不是替作者决定世界局势。玩家文风偏好不参与语义裁判。");
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
		return sb.ToString();
	}

	private static string BuildTokenCompressionSystemPrompt()
	{
		return "你是卡拉迪亚外交编年史官。把若干已经结束的外交事件整理成长期外交记忆。"
			+ "材料严格区分[宣言记录]与[游戏已执行]：宣言记录只证明某国公开提出、接受、拒绝、谴责或威胁过某事，不能证明提案已经执行；"
			+ "只有[游戏已执行]才能写成已经发生的战争、和平、联盟、贸易、贡金或领地变化。保留重要立场、未果提案与确认结果，合并重复内容，不逐篇复述，不补充材料外事实。"
			+ "只输出JSON：{\"summary\":\"长期外交记忆正文\"}";
	}

	private string BuildTokenCompressionPrompt(string batchId, List<WorldDiplomacyRoundSummary> summaries, long tokenCount)
	{
		StringBuilder material = new StringBuilder();
		material.AppendLine("压缩批次=" + (batchId ?? ""));
		material.AppendLine("本周期累计外交Tokens=" + Math.Max(0L, tokenCount).ToString(CultureInfo.InvariantCulture));
		foreach (WorldDiplomacyRoundSummary roundSummary in (summaries ?? new List<WorldDiplomacyRoundSummary>()).Where(x => x != null).OrderBy(x => x.CreatedDay).Take(MaxStoredRoundSummaries))
		{
			material.AppendLine("[已结束回合 " + roundSummary.RoundId + "] " + Limit(roundSummary.Summary, 1200));
			foreach (WorldDiplomacyRoundFact fact in (roundSummary.Facts ?? new List<WorldDiplomacyRoundFact>())
				.Where(x => x != null && string.Equals(x.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase)).Take(8))
			{
				material.AppendLine(Limit(FormatRoundFactForPrompt(fact), 500));
			}
		}
		return material.ToString().TrimEnd();
	}

	private string BuildFallbackGenerationJson(WorldDiplomacyJob job)
	{
		Kingdom author = ResolveKingdom(job.AuthorKingdomId);
		Kingdom target = ResolveKingdom(job.TargetKingdomId);
		string body = BuildFallbackDocumentBody(author, target, job.ForcedIntent, job.IsResponse, ResolveDocument(job.SourceDocumentId));
		return new JObject
		{
			["title"] = string.IsNullOrWhiteSpace(job.ForcedIntent) ? (job.IsResponse ? "外交回应" : "王国外交声明") : IntentLabel(job.ForcedIntent),
			["body"] = body,
			["author_intent"] = new JObject
			{
				["intent"] = string.IsNullOrWhiteSpace(job.ForcedIntent) ? "statement" : job.ForcedIntent,
				["commitment"] = string.IsNullOrWhiteSpace(job.ForcedIntent) ? "non_binding" : "binding"
			}
		}.ToString(Formatting.None);
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

	private string BuildFallbackCompressionJson(WorldDiplomacyJob job)
	{
		return new JObject
		{
			["summary"] = BuildFallbackTokenCompressionSummary(job.CompressionRoundIds)
		}.ToString(Formatting.None);
	}

	private string BuildFallbackTokenCompressionSummary(List<string> roundIds)
	{
		HashSet<string> ids = new HashSet<string>(roundIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
		List<string> summaries = _storage.RoundSummaries.Where(x => x != null && ids.Contains(x.RoundId ?? ""))
			.OrderBy(x => x.CreatedDay).Select(x => Limit(x.Summary, 900)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		return summaries.Count == 0 ? "本周期没有留下可整理的外交事件。" : string.Join("\n", summaries);
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

	private string BuildFallbackDocumentBody(Kingdom author, Kingdom target, string intent, bool isResponse, WorldDiplomacyDocument sourceDocument)
	{
		string authorName = KingdomName(author);
		string targetName = KingdomName(target);
		string normalizedIntent = NormalizeIntent(intent);
		if (normalizedIntent == "declare_war")
		{
			string reason = BuildRecentNativeSignalContext(author?.StringId, target?.StringId);
			return authorName + "在此向" + targetName + "及世人宣告：两国之间的和平自今日起正式终结。"
				+ (string.IsNullOrWhiteSpace(reason) ? "" : "长久以来的争端已经使一切劝告失去意义。");
		}
		if (normalizedIntent == "propose_peace")
		{
			return authorName + "正式向" + targetName + "提出结束当前战争。我国愿遣使商定停战、贡金及其他现实条件；在双方作出明确答复以前，战事仍然存在，这份公文不应被误解为和平已经成立。";
		}
		if (isResponse)
		{
			return authorName + "已经知悉" + targetName + "所发布的公文。我国将依照自身荣誉、利益与疆土安危审视其中主张，并保留作出进一步回应的权利。";
		}
		return authorName + "向" + targetName + "及诸国声明：当前局势值得各方审慎对待。我国愿以公开言辞说明自身立场，也将依据对方今后的行动决定两国关系的走向。";
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

	private Kingdom SelectTargetKingdom(Kingdom initiator)
	{
		if (initiator == null)
		{
			return null;
		}
		List<Kingdom> candidates = Kingdom.All.Where(x => x != null && !x.IsEliminated && x != initiator
			&& HasIndependentWorldDiplomacyAuthority(x)).ToList();
		if (candidates.Count == 0)
		{
			return null;
		}
		return candidates
			.Select(target => new
			{
				Target = target,
				Score = ScoreDiplomaticTarget(initiator, target)
			})
			.OrderByDescending(x => x.Score)
			.ThenBy(x => x.Target.StringId, StringComparer.OrdinalIgnoreCase)
			.First().Target;
	}

	private float ScoreDiplomaticTarget(Kingdom initiator, Kingdom target)
	{
		float score = MBRandom.RandomFloat * 15f;
		int pressure = GetWarPressure(initiator.StringId, target.StringId);
		score += pressure;
		int relation = GetRulerRelation(initiator, target);
		score += Math.Abs(relation) * 0.35f;
		score += CountCulturalClaims(initiator, target) * 8f;
		if (FactionManager.IsAtWarAgainstFaction(initiator, target))
		{
			score += 55f;
		}
		IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliance != null && alliance.IsAllyWithKingdom(initiator, target))
		{
			score += 20f;
		}
		score -= GetRepeatedPairCooldownPenalty(initiator.StringId, target.StringId);
		return score;
	}

	private float GetRepeatedPairCooldownPenalty(string firstId, string secondId)
	{
		int day = CurrentDay();
		WorldDiplomacyRound latest = (_storage.CompletedRounds ?? new List<WorldDiplomacyRound>())
			.Where(x => x != null && x.CompletedDay > 0
				&& (x.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Any(p => p != null && string.Equals(p.KingdomId, firstId, StringComparison.OrdinalIgnoreCase))
				&& (x.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Any(p => p != null && string.Equals(p.KingdomId, secondId, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(x => x.CompletedDay)
			.FirstOrDefault();
		if (latest == null || day - latest.CompletedDay >= RepeatedPairCooldownDays) return 0f;
		bool hasNewHardSignal = (_storage.NativeSignals ?? new List<NativeDiplomacySignal>()).Any(x => x != null && x.Day > latest.CompletedDay
			&& ((string.Equals(x.SourceKingdomId, firstId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.TargetKingdomId, secondId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(x.SourceKingdomId, secondId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.TargetKingdomId, firstId, StringComparison.OrdinalIgnoreCase))));
		if (hasNewHardSignal) return 0f;
		int remaining = RepeatedPairCooldownDays - Math.Max(0, day - latest.CompletedDay);
		return 120f * remaining / RepeatedPairCooldownDays;
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
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(MaxStoredDocuments)
			.OrderBy(x => x.Day)
			.ThenBy(x => x.CreatedUtcTicks)
			.ToList();
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

	private void NormalizeStorage()
	{
		_storage ??= new WorldDiplomacyStorage();
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
		_storage.Jobs ??= new List<WorldDiplomacyJob>();
		_storage.SuspendedExchanges ??= new List<WorldDiplomacyExchange>();
		_storage.LastOffensiveWarDayByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_storage.LastPeaceDayByPair ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
		foreach (WorldDiplomacyDocument document in _storage.Documents)
		{
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
			if (document.IsReadyForPublication && !document.PropagationStarted && string.IsNullOrWhiteSpace(document.OriginSettlementId))
			{
				// Documents from the pre-propagation save format were globally visible already.
				document.HasReachedPlayerCourt = document.HasReachedPlayerCourt || !document.IsPlayerAuthored;
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
			|| (string.Equals(x.Kind, "compress", StringComparison.OrdinalIgnoreCase) && (x.CompressionRoundIds == null || x.CompressionRoundIds.Count == 0)));
		foreach (WorldDiplomacyJob job in _storage.Jobs)
		{
			job.CandidateKingdomIds ??= new List<string>();
			job.TriggerDocumentIds ??= new List<string>();
			job.LlmMessages ??= new List<WorldDiplomacyLlmMessage>();
			job.CompressionRoundIds ??= new List<string>();
		}
		foreach (WorldDiplomacyRound round in _storage.CompletedRounds.Concat(_storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { _storage.ActiveRound }).Where(x => x != null))
		{
			round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
			round.RelayRouteKingdomIds ??= new List<string>();
			round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
			round.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
			round.LlmTranscript.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Role));
			round.LlmProfiledKingdomIds ??= new List<string>();
			round.ExternalSignalKeys ??= new List<string>();
			round.ExternalSignalKeys = round.ExternalSignalKeys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			round.ExternalOpeningContext ??= "";
			round.LlmProfiledKingdomIds = round.LlmProfiledKingdomIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			round.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			round.PendingOffers.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.SourceDocumentId));
			PruneInvalidPeaceOffers(round);
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
			if (ReferenceEquals(round, _storage.ActiveRound) && round.RelayPlanned && round.SchemaVersion < RelaySchemaVersion)
			{
				WorldDiplomacyDocument root = ResolveDocument(round.RootDocumentId);
				if (root != null) round.CachePrefix = BuildRoundCachePrefix(round, root);
				WorldDiplomacyLlmMessage frozenSystem = round.LlmTranscript.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase));
				if (frozenSystem != null) frozenSystem.Content = BuildRelayGenerationSystemPrompt();
				round.SchemaVersion = RelaySchemaVersion;
			}
		}
		foreach (WorldDiplomacyJob job in _storage.Jobs.Where(x => x != null))
		{
			if (job.IsRelayTurn && !string.Equals(job.CacheAffinityKey, RelayCacheAffinityKey, StringComparison.OrdinalIgnoreCase))
			{
				WorldDiplomacyRound round = ResolveRound(job.RoundId);
				Kingdom author = ResolveKingdom(job.AuthorKingdomId);
				if (round != null && author != null)
				{
					WorldDiplomacyLlmMessage frozenSystem = round.LlmTranscript.FirstOrDefault(x => x != null && string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase));
					job.SystemPrompt = !string.IsNullOrWhiteSpace(frozenSystem?.Content) ? frozenSystem.Content : BuildRelayGenerationSystemPrompt();
					bool profiled = round.LlmProfiledKingdomIds.Any(x => string.Equals(x, author.StringId, StringComparison.OrdinalIgnoreCase));
					job.UserPrompt = BuildRelayConversationTurnPrompt(round, author, ResolveKingdom(job.PreviousKingdomId), !profiled,
						ResolveDocument(job.SourceDocumentId), job.IsExternalResponseOnly);
					job.ProfiledKingdomId = profiled ? "" : author.StringId;
					job.LlmMessages = CloneLlmMessages(round.LlmTranscript);
					if (job.LlmMessages.Count > 0) job.LlmMessages.Add(new WorldDiplomacyLlmMessage { Role = "user", Content = job.UserPrompt });
					job.CacheAffinityKey = RelayCacheAffinityKey;
				}
			}
			else if (string.Equals(job.Kind, "round_plan", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(job.CacheAffinityKey, "diplomacy-round-plan:v4", StringComparison.OrdinalIgnoreCase))
			{
				job.SystemPrompt = BuildRoundPlanSystemPrompt();
				job.CacheAffinityKey = "diplomacy-round-plan:v4";
			}
		}
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
		_storage.PendingSpeeches.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.RoundId) || string.IsNullOrWhiteSpace(x.AuthorKingdomId));
		_storage.PendingSpeeches = _storage.PendingSpeeches
			.OrderByDescending(x => x.Priority)
			.ThenBy(x => x.QueuedDay)
			.ThenBy(x => x.AuthorKingdomId, StringComparer.OrdinalIgnoreCase)
			.Take(MaxPendingSpeeches)
			.ToList();
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
		_storage.DiplomacyTokensSinceCompression = Math.Max(0L, _storage.DiplomacyTokensSinceCompression);
		if (_storage.DiplomacyTokensSinceCompression >= GetCompressionThresholdTokens()) _storage.DiplomacyCompressionPending = true;
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

	private static int GetLastForcedPeaceProposalDay(WorldDiplomacyWarLedger ledger, string authorKingdomId)
	{
		if (ledger == null || string.IsNullOrWhiteSpace(authorKingdomId)) return 0;
		return string.Equals(ledger.FirstKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase)
			? ledger.FirstLastForcedPeaceProposalDay
			: string.Equals(ledger.SecondKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase)
				? ledger.SecondLastForcedPeaceProposalDay
				: 0;
	}

	private static void SetLastForcedPeaceProposalDay(WorldDiplomacyWarLedger ledger, string authorKingdomId, int day)
	{
		if (ledger == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
		if (string.Equals(ledger.FirstKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			ledger.FirstLastForcedPeaceProposalDay = day;
		}
		else if (string.Equals(ledger.SecondKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			ledger.SecondLastForcedPeaceProposalDay = day;
		}
	}

	private bool HasOpenPeaceOffer(string proposerKingdomId, string targetKingdomId)
	{
		WorldDiplomacyRound round = _storage.ActiveRound;
		return round?.PendingOffers?.Any(x => x != null
			&& string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeIntent(x.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.ProposerKingdomId, proposerKingdomId, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)) == true;
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
			return DuelSettings.GetSettings()?.EnableWorldDiplomacy ?? true;
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

	private static bool IsDiplomaticSituationAutoAdvanceEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableWorldDiplomacyForcedWar ?? true;
		}
		catch
		{
			return true;
		}
	}

	private static int GetDiplomaticAdvanceThreshold()
	{
		try
		{
			return Math.Max(50, Math.Min(200, DuelSettings.GetSettings()?.WorldDiplomacyWarPressureThreshold ?? 100));
		}
		catch
		{
			return 100;
		}
	}

	private static int GetDiplomaticAdvanceReleaseThreshold()
	{
		return Math.Max(1, (int)Math.Floor(GetDiplomaticAdvanceThreshold() * DiplomaticAdvanceReleaseRatio));
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

	private static long GetCompressionThresholdTokens()
	{
		try
		{
			int tenThousands = Math.Max(10, Math.Min(200, DuelSettings.GetSettings()?.WorldDiplomacyCompressionThresholdTenThousands ?? 50));
			return tenThousands * 10000L;
		}
		catch
		{
			return 500000L;
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

	private static string NormalizeBody(string value)
	{
		string text = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		return Limit(text, 6000);
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
		return ContainsAny(text, "反提案", "反建议", "另提", "重新提出", "新的条件", "条件改为", "改为", "除非", "前提是", "可以，但", "愿意，但", "接受，但", "同意，但");
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

	private static bool LooksLikeExplicitPeaceNegotiation(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;
		if (ContainsAny(text, "并未交战", "没有战争", "不存在停战", "不存在议和")) return false;
		return ContainsAny(text, "停战", "停火", "议和", "和谈", "休战", "结束战争", "结束这场战争", "退出战争", "战争补偿");
	}

	private static bool LooksLikeExplicitPeaceNegotiationWithTarget(Kingdom author, Kingdom target, string text)
	{
		if (author == null || target == null || author == target || !LooksLikeExplicitPeaceNegotiation(text)) return false;
		string targetName = KingdomName(target);
		string targetRulerName = RulerName(target);
		foreach (string rawClause in Regex.Split(text ?? "", @"[\r\n。！？；]+"))
		{
			string clause = (rawClause ?? "").Trim();
			if (!LooksLikeExplicitPeaceNegotiation(clause) || IsPeaceDiscussionDisclaimer(clause)) continue;
			bool namesTarget = (!string.IsNullOrWhiteSpace(targetName) && clause.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (!string.IsNullOrWhiteSpace(targetRulerName) && clause.IndexOf(targetRulerName, StringComparison.OrdinalIgnoreCase) >= 0);
			bool namesOtherKingdom = Kingdom.All.Any(x => x != null && x != author && x != target
				&& ((!string.IsNullOrWhiteSpace(KingdomName(x)) && clause.IndexOf(KingdomName(x), StringComparison.OrdinalIgnoreCase) >= 0)
					|| (!string.IsNullOrWhiteSpace(RulerName(x)) && clause.IndexOf(RulerName(x), StringComparison.OrdinalIgnoreCase) >= 0)));
			if (namesTarget) return true;
			if (namesOtherKingdom) continue;
			bool refersToBilateralParties = ContainsAny(clause, "贵国", "双方", "两国", "彼此", "本国与", "我国与");
			bool statesPeaceMove = ContainsAny(clause, "提议", "提出", "接受", "同意", "拒绝", "要求", "愿意", "愿与", "请求", "条件", "必须", "应当", "准备", "另行商议", "另行讨论");
			if (refersToBilateralParties || statesPeaceMove) return true;
		}
		return false;
	}

	private static bool IsPeaceDiscussionDisclaimer(string clause)
	{
		if (string.IsNullOrWhiteSpace(clause)) return false;
		if (ContainsAny(clause, "不是一份和平条约", "不构成和平条约", "不改变战争状态", "不改变任何两国之间的战争状态", "不等于停战", "不等于议和", "不把和平与", "不把停战与")) return true;
		return Regex.IsMatch(clause,
			@"(?:无意|不打算|并非要|不会在此).{0,24}(?:讨论|商议|提出|缔结).{0,16}(?:停战|停火|议和|和谈|休战)|(?:不应|不得|不要).{0,20}(?:变成|视为|混同为).{0,12}(?:停战|停火|议和|和谈|休战)",
			RegexOptions.CultureInvariant);
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
		string title = FirstNonEmpty(document?.Title, document?.AuthorKingdomName + "发布外交宣言", "外交宣言");
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
		string topic = FirstNonEmpty(round?.RoundTopic, ResolveDocument(round?.RootDocumentId)?.Title);
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
		string topic = FirstNonEmpty(round.RoundTopic, ResolveDocument(round.RootDocumentId)?.Title, "外交交涉");
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

	private sealed class WeeklyDiplomacySnapshotCacheEntry
	{
		public int WeekIndex;
		public string Text = "";
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
	[JsonProperty("cachePrefix")] public string CachePrefix { get; set; } = "";
	[JsonProperty("externalSignalKeys")] public List<string> ExternalSignalKeys { get; set; } = new List<string>();
	[JsonProperty("externalOpeningContext")] public string ExternalOpeningContext { get; set; } = "";
	[JsonProperty("llmTranscript")] public List<WorldDiplomacyLlmMessage> LlmTranscript { get; set; } = new List<WorldDiplomacyLlmMessage>();
	[JsonProperty("llmProfiledKingdomIds")] public List<string> LlmProfiledKingdomIds { get; set; } = new List<string>();
	[JsonProperty("llmLastStateSignatureByKingdom")] public Dictionary<string, string> LlmLastStateSignatureByKingdom { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	[JsonProperty("roundStatus")] public string RoundStatus { get; set; } = "active";
	[JsonProperty("executedActionCount")] public int ExecutedActionCount { get; set; }
	[JsonProperty("substantiveProgressCount")] public int SubstantiveProgressCount { get; set; }
	[JsonProperty("lastSubstantiveProgressDay")] public int LastSubstantiveProgressDay { get; set; }
	[JsonProperty("finalActionOpportunityIssued")] public bool FinalActionOpportunityIssued { get; set; }
	[JsonProperty("pendingOffers")] public List<WorldDiplomacyRoundOffer> PendingOffers { get; set; } = new List<WorldDiplomacyRoundOffer>();
	[JsonProperty("participants")] public List<WorldDiplomacyRoundParticipant> Participants { get; set; } = new List<WorldDiplomacyRoundParticipant>();
}

public sealed class WorldDiplomacyLlmMessage
{
	[JsonProperty("role")] public string Role { get; set; } = "";
	[JsonProperty("content")] public string Content { get; set; } = "";
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
