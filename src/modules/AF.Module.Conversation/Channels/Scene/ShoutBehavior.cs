using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;
using SceneGroupReceipt = AnimusForge.SceneConversationSessionRuntime.SceneGroupReceipt;

namespace AnimusForge;

public partial class ShoutBehavior : CampaignBehaviorBase
{
    private ConversationActionBoundaryBannerlordAdapter _actionBoundary;
    private ConversationActionBoundaryBannerlordAdapter ActionBoundary => _actionBoundary ??= new ConversationActionBoundaryBannerlordAdapter(_nativeGameEffects,TryTriggerNativeConversationOpenLordsHallAction,_mainThreadActions.Enqueue,()=>_mainThreadActions.Count,MainThreadActionDrain,ScheduleNativeSceneTauntFightAfterDelay,QueuePendingCurrentAfefFactForAgent,QueuePendingCurrentNativeAfefFactForKey);
    private ConversationActionExecutorComposition _actionExecutors;
    private ConversationActionExecutorComposition ActionExecutors => _actionExecutors ??= new ConversationActionExecutorComposition(()=>ReferenceEquals(CurrentInstance,this),_sceneMovement,_nativeGameEffects,ActionBoundary,_conversationGameThreadDispatcher);
    private ConversationDeveloperActionTestController _developerActions;
    private ConversationDeveloperActionTestController DeveloperActions => _developerActions ??= new ConversationDeveloperActionTestController(()=>ReferenceEquals(CurrentInstance,this),CanSubmitNativeConversationForExternal,_sceneMovement,_nativeGameEffects,TryQueueNativeSceneMechanismActionAfterConversationExit,()=>_activeShoutTargetingContext,GetAgentsForShoutTargetingContext,OnShoutCancelled,ResumeGame,(npc,content,npcs,summon,guide)=>EnqueueSpeechLineWithOptions(npc,content,npcs,commitHistory:true,suppressStare:false,allowPlayerDirectedActions:true,requiredConversationEpoch:0,summon,guide,null));
    private ConversationDiagnosticsCaptureAdapter _diagnosticCapture;
    private ConversationDiagnosticsCaptureAdapter DiagnosticCapture => _diagnosticCapture ??= new ConversationDiagnosticsCaptureAdapter(()=>Monitor.TryEnter(_historyLock),()=>Monitor.Exit(_historyLock),SceneHistoryOwner.AppendDiagnostics,()=>_mainThreadActions.Count,_sceneSpeechQueueOwner.TryGetSnapshot);
	private readonly ConversationMainThreadActionDrain MainThreadActionDrain;
	internal enum ShoutChatMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners,
		GiveSettlements
	}

	internal class ShoutTradeResourceOption
	{
		public bool IsGold;

		public string ItemId;

		public string Name;

		public int AvailableAmount;

		public ItemObject Item;

		public long InventoryTotalValue;

		public int InventoryUnitValue;

		public MyBehavior.PartyTransferPromptEntry PartyEntry;

		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}

	internal class ShoutPendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;

		public int InventoryUnitValue;

		public MyBehavior.PartyTransferPromptEntry PartyEntry;

		// Discarded with the existing trade state; not a second outcome ledger.
		public string PartyTransferPartialFact;

		public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
	}

	internal struct ShoutPreviewLineSegment
	{
		public Vec3 Start;

		public Vec3 End;
	}

	internal enum ShoutPreviewAgentHighlightRole
	{
		Candidate,
		Primary
	}

	internal struct ShoutPreviewAgentHighlightState
	{
		public Agent Agent;

		public ShoutPreviewAgentHighlightRole Role;
	}

	internal sealed class SceneRelayEligibilitySnapshot
	{
		public List<NpcDataPacket> Candidates = new List<NpcDataPacket>();

		public List<string> PatienceStatusLines = new List<string>();
	}

	internal sealed class ScenePrepaidTransferRecord
	{
		public int Gold;

		public int NegotiatedGold;

		public int Day;

		public string SettlementId;
	}

	internal sealed class PendingNpcBubbleEntry
	{
		public Agent Agent;

		public string UiContent;

		public string NpcName;

		public float FallbackDurationSeconds;
	}


	internal sealed class SceneInteractionSession
	{
		public int TargetAgentIndex;

		public string TargetName;

		public float LastActivityTime;

		public bool TimeoutArmed;

		public float TimeoutSeconds;

		public long InteractionToken;

		public bool ReturnSceneSummonOnTimeout;

		public float InitialPlayerDistanceMeters;

		public float PlayerReleaseRangeMeters;
	}

	internal sealed class PendingInteractionTimeoutArm
	{
		public int AgentIndex;

		public long InteractionToken;

		public float ArmAtMissionTime;
	}



	internal sealed class PendingMeetingReleaseAfterSpeech
	{
		public int AgentIndex;

		public Hero TargetHero;

		public PartyBase EncounterParty;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class PendingWorldMapMissionExitAfterSpeech
	{
		public int AgentIndex;
		public bool WaitForPlaybackFinished;
		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class PendingLordsHallMissionEntryAfterSpeech
	{
		public Mission Mission;
		public int AgentIndex;
		public string SettlementId;
		public string Reason;
		public bool WaitForConversationEnd;
		public bool WaitForPlaybackFinished;
		public float ExecuteAtMissionTime = -1f;
	}



	internal sealed class PendingSceneAutonomyRestoreAfterSpeech
	{
		public int AgentIndex;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class PrecomputedShoutRagContext
	{
		public bool HasLoreContext = false;

		public string LoreContext = "";

		public bool HasPersistedHistoryContext;

		public string PersistedHistoryContext;

		public Task<string> PersistedHistoryContextTask;
	}


	// This request is created and consumed only on the Bannerlord main thread.
	// Its Messages payload is copied into locals before the background HTTP task starts,
	// so the worker never needs to dereference Mission, Agent, Hero, or UI state.
	internal sealed class SceneSummonPromptTarget
	{
		public int PromptId;

		public string DisplayName;

		public string LocationCode;

		public LocationCharacter LocationCharacter;

		public Location SourceLocation;
	}

	internal sealed class SceneGuidePromptTarget
	{
		public int PromptId;

		public string DisplayName;

		public string LocationCode;

		public LocationCharacter LocationCharacter;

		public Location SourceLocation;
	}




	internal sealed class SceneSpeechPlaybackInfo
	{
		public bool TtsEnabled;

		public bool TtsAccepted;

		public bool WaitForPlaybackFinished;

		public float VisualDurationSeconds;
	}

	internal sealed class PendingSceneDialogueFeedEntry
	{
        public Mission SourceMission;
        public long RuntimeGeneration;
        public int ConversationEpoch = -1;
		public string SpeakerLabel;

		public string Content;

		public Color Color;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}








	private class ShoutMissionBehavior : MissionBehavior
	{
		private ShoutBehavior _parent;

		private float _nextHotkeySettingsRefreshApplicationTime;

		private InputKey _shoutKey = InputKey.T;

		private InputKey _specialMenuKey = InputKey.Y;

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public ShoutMissionBehavior(ShoutBehavior parent)
		{
			_parent = parent;
		}

		public override void OnMissionTick(float dt)
		{
			using PerfProbe.ScopeToken perfScope = PerfProbe.Scope("Mission.ShoutMissionBehavior.OnMissionTick.total");
			using FreezeWatchdog.ScopeToken missionTickScope = FreezeWatchdog.Scope("ShoutMissionBehavior.OnMissionTick.total");
			using (FreezeWatchdog.Scope("ShoutMissionBehavior.OnMissionTick"))
			{
				using (FreezeWatchdog.Scope("ShoutMissionBehavior.DrainMainThreadActionsForMissionTick"))
				{
					_parent.DrainMainThreadActionsForMissionTick();
				}
				_parent.TickPendingNativeSceneMechanismActions();
				// Before the conversation early-return below so a native conversation ends the session.
				_parent.TickPresentationSession(dt);
				using (FreezeWatchdog.Scope("ShoutMissionBehavior.TryTriggerPendingProactiveSceneOpening"))
				{
					_parent.TryTriggerPendingProactiveSceneOpening();
				}
				try
				{
					using (FreezeWatchdog.Scope("ShoutMissionBehavior.ProcessDeferredCleanup"))
					{
						_parent.ProcessDeferredCleanup();
					}
				}
				catch (Exception ex2)
				{
					BannerlordExceptionSentinel.ReportObservedException("LipSync.ProcessDeferredCleanup", ex2, "behavior=ShoutMissionBehavior");
				}
				try
				{
					using (FreezeWatchdog.Scope("ShoutMissionBehavior.TickLipSyncAnimations"))
					{
						_parent.TickLipSyncAnimations(dt);
					}
				}
				catch (Exception ex3)
				{
					BannerlordExceptionSentinel.ReportObservedException("LipSync.TickLipSyncAnimations", ex3, "behavior=ShoutMissionBehavior");
				}
			}
			// A live presentation session has already passed its own (narrower) combat check this tick. The broad
			// check also counts any team-hostile bystander in town, which would close the panel and keep
			// releasing the addressee's movement hold, so it does not apply while a session is live.
			bool flag = !_parent.IsPresentationSessionLive() && ShoutBehavior.ShouldSuppressSceneConversationControlForMeeting();
			if (flag)
			{
				_parent.ClearMeetingSceneConversationControlState();
			}
			else
			{
				_parent.UpdateMultiSceneMovementSuppression(dt);
			}
			_parent.UpdatePendingNativeSceneTauntFightDelay();
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				_parent.CancelShoutHotkeyCharge("conversation");
				_parent._stareTimer = 0f;
				_parent._currentStareTarget = null;
				if (!_parent._ttsPausedByShoutUi)
				{
					_parent.DeactivateMultiSceneMovementSuppression();
				}
				_parent.ResetStaringBehavior();
				return;
			}
			if (!flag)
			{
				_parent.UpdateStaringBehavior();
				_parent.FlushPendingSceneConversationAttentionRelease();
			}
			_parent.ProcessPendingInteractionTimeoutArms();
			_parent.UpdateActiveInteractionTimeouts();
			_parent.UpdatePendingSceneDialogueFeeds();
			_parent._sceneMovement.TickScheduledReturns();
			_parent.UpdatePendingSceneAutonomyRestoresAfterSpeech();
			_parent.UpdatePendingLordsHallMissionEntryAfterSpeech();
			_parent.UpdatePendingMeetingReleasesAfterSpeech();
			_parent.UpdatePendingWorldMapMissionExitsAfterSpeech();
			_parent._sceneMovement.TickActiveCommands();
			if (_parent._interactionGraceTimer > 0f)
			{
				_parent._interactionGraceTimer -= dt;
			}
			_parent._tickTimer += dt;
			if (!flag && _parent._tickTimer >= 0.2f)
			{
				// Do not let the same key press that opens scene shout win the passive-stare race.
				if (Input.IsKeyPressed(_shoutKey) || Input.IsKeyPressed(_specialMenuKey))
				{
					_parent.ResetPassiveStareTracking();
				}
				else
				{
					_parent.UpdatePassiveStareLogic(_parent._tickTimer);
				}
				_parent._tickTimer = 0f;
			}
			else if (flag && _parent._tickTimer >= 0.2f)
			{
				_parent._tickTimer = 0f;
			}
			RefreshHotkeySettingsIfDue();
			bool flag2 = true;
			try
			{
				flag2 = IsGameWindowFocused();
			}
			catch
			{
				flag2 = true;
			}
			bool wasGameWindowFocused = _parent._wasGameWindowFocused;
			if (wasGameWindowFocused && !flag2)
			{
				_parent.ArmShoutHotkeyFocusDebounce("focus_lost");
				_parent.CancelShoutHotkeyCharge("focus_lost");
				ShoutTextInputPopup.HandleGameWindowFocusChanged(focusGained: false);
				try
				{
					_parent.HandleEscapePressedForAudioSafety("FOCUS_LOST");
				}
				catch
				{
				}
			}
			else if (!wasGameWindowFocused && flag2)
			{
				_parent.ArmShoutHotkeyFocusDebounce("focus_gained");
			}
			_parent._wasGameWindowFocused = flag2;
			if (Input.IsKeyPressed(InputKey.Escape))
			{
				_parent.CancelShoutHotkeyCharge("escape");
				ShoutTextInputPopup.CancelActiveForEscapeMenu();
				try
				{
					_parent.HandleEscapePressedForAudioSafety();
				}
				catch
				{
				}
			}
			try
			{
				_parent.TryResumeInterruptionPauseIfPossible();
			}
			catch
			{
			}
			ShoutTextInputPopup.KeepMissionPausedIfOpen();
			_parent.ClearStaleShoutProcessingIfNeeded();
			if (HotkeyInputGuard.IsTextInputFocused())
			{
				_parent.CancelShoutHotkeyCharge("text_input");
			}
			else
			{
				_parent.UpdateShoutHotkeyCharge(_shoutKey, _specialMenuKey);
			}
		}

		private void RefreshHotkeySettingsIfDue()
		{
			float applicationTime = GetApplicationTimeSafe();
			if (applicationTime < _nextHotkeySettingsRefreshApplicationTime)
			{
				return;
			}
			_nextHotkeySettingsRefreshApplicationTime = applicationTime + 1f;
			DuelSettings settings = DuelSettings.GetSettings();
			_shoutKey = InputKey.T;
			_specialMenuKey = InputKey.Y;
			if (!string.IsNullOrWhiteSpace(settings?.ShoutKey) && Enum.TryParse(settings.ShoutKey.Trim(), ignoreCase: true, out InputKey parsedShoutKey))
			{
				_shoutKey = parsedShoutKey;
			}
			if (!string.IsNullOrWhiteSpace(settings?.ShoutSpecialMenuKey) && Enum.TryParse(settings.ShoutSpecialMenuKey.Trim(), ignoreCase: true, out InputKey parsedSpecialMenuKey))
			{
				_specialMenuKey = parsedSpecialMenuKey;
			}
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			_parent.ResetSceneShoutRuntimeOnMissionEnd("remove_behavior");
			_parent.CancelShoutHotkeyCharge("remove_behavior");
			try
			{
				_parent.StopAllLipSyncPlaybackAndCleanup();
			}
			catch
			{
			}
			try
			{
				_parent.UnsubscribeTtsPlaybackEvents();
			}
			catch
			{
			}
		}
	}

private bool _isProcessingShout { get => _j17SceneShoutInputController._isProcessingShout; set => _j17SceneShoutInputController._isProcessingShout = value; }

private float _shoutProcessingStartedAt { get => _j17SceneShoutInputController._shoutProcessingStartedAt; set => _j17SceneShoutInputController._shoutProcessingStartedAt = value; }

	private const float ShoutProcessingFailsafeSeconds = 180f;

	internal const int ScenePostprocessGateWaitTimeoutMilliseconds = 180000;

	internal const float ImmediateSceneReactionCooldownSeconds = 20f;

	private const float ProactiveSceneOpeningProbeIntervalSeconds = 0.5f;

	private const int ShoutHotkeyFocusDebounceMilliseconds = 1200;

	private const int ShoutProcessingBusyMessageCooldownMilliseconds = 1500;

	private const float ShoutChargeSecondsToMax = 4f;

	private const float ShoutHardMaxRangeMeters = 150f;

	private const float ShoutMinRangeMeters = 1f;

	private const float ShoutInitialTotalAngleRadians = 1.5707964f;

	private const float ShoutMaxTotalAngleRadians = 5.2359877f;

	private const float ShoutPreviewArcSegmentLengthMeters = 1.4f;

	private const float ShoutPreviewRadialSegmentLengthMeters = 1.6f;

	private const float ShoutPreviewLineLengthScaleMultiplier = 2.4f;

	private const float ShoutPreviewLineLengthOverlapScale = 1.2f;

	private const float ShoutPreviewLineWidthScale = 1.6f;

	private const float ShoutPreviewLineHeightScale = 0.38f;

	private const int ShoutPreviewMinArcSegments = 36;

	private const int ShoutPreviewMaxArcSegments = 520;

	private const int ShoutPreviewMinRadialSegments = 6;

	private const int ShoutPreviewMaxRadialSegments = 96;

	private const int ShoutPreviewMaxMarkerCount = 760;

	private const int ShoutPreviewMaxMarkerCreatesPerTick = 220;

	private const int ShoutPreviewMarkerRemoveReason = 95;

	private const float ShoutPreviewMarkerGroundOffset = 0.9f;

private static uint ShoutPreviewMarkerTintColor { get => SceneShoutInputController.ShoutPreviewMarkerTintColor; }

private static uint ShoutPreviewAgentHighlightColor { get => SceneShoutInputController.ShoutPreviewAgentHighlightColor; }

private static uint ShoutPreviewPrimaryAgentHighlightColor { get => SceneShoutInputController.ShoutPreviewPrimaryAgentHighlightColor; }

	private const string ShoutPreviewMarkerItemId = "animusforge_denar_ingot_item";

	private const string ShoutPreviewSecondaryMarkerItemId = "animusforge_denar_coin_item";

	private const string ShoutPreviewFallbackMarkerItemId = "sling_leadammo";

private long _suppressShoutHotkeyUntilUtcTicks { get => _j17SceneShoutInputController._suppressShoutHotkeyUntilUtcTicks; set => _j17SceneShoutInputController._suppressShoutHotkeyUntilUtcTicks = value; }

private long _lastShoutProcessingBusyMessageUtcTicks { get => _j17SceneShoutInputController._lastShoutProcessingBusyMessageUtcTicks; set => _j17SceneShoutInputController._lastShoutProcessingBusyMessageUtcTicks = value; }

private bool _shoutHotkeyChargeActive { get => _j17SceneShoutInputController._shoutHotkeyChargeActive; set => _j17SceneShoutInputController._shoutHotkeyChargeActive = value; }

private bool _shoutHotkeyChargeOpenModeMenu { get => _j17SceneShoutInputController._shoutHotkeyChargeOpenModeMenu; set => _j17SceneShoutInputController._shoutHotkeyChargeOpenModeMenu = value; }

private InputKey _shoutHotkeyChargeKey { get => _j17SceneShoutInputController._shoutHotkeyChargeKey; set => _j17SceneShoutInputController._shoutHotkeyChargeKey = value; }

private float _shoutHotkeyChargeStartedAt { get => _j17SceneShoutInputController._shoutHotkeyChargeStartedAt; set => _j17SceneShoutInputController._shoutHotkeyChargeStartedAt = value; }

private ShoutTargetingContext _activeShoutTargetingContext { get => _j17SceneShoutInputController._activeShoutTargetingContext; set => _j17SceneShoutInputController._activeShoutTargetingContext = value; }

private ShoutTargetingContext _lastRenderedShoutTargetingContext { get => _j17SceneShoutInputController._lastRenderedShoutTargetingContext; set => _j17SceneShoutInputController._lastRenderedShoutTargetingContext = value; }

private List<GameEntity> _shoutPreviewMarkerEntities { get => _j17SceneShoutInputController._shoutPreviewMarkerEntities; }

private Dictionary<int, ShoutPreviewAgentHighlightState> _shoutPreviewAgentHighlightStates { get => _j17SceneShoutInputController._shoutPreviewAgentHighlightStates; }

private HashSet<int> _shoutPreviewDesiredAgentIndicesScratch { get => _j17SceneShoutInputController._shoutPreviewDesiredAgentIndicesScratch; }

private List<int> _shoutPreviewStaleAgentIndicesScratch { get => _j17SceneShoutInputController._shoutPreviewStaleAgentIndicesScratch; }

private ItemObject _shoutPreviewMarkerItemObject { get => _j17SceneShoutInputController._shoutPreviewMarkerItemObject; set => _j17SceneShoutInputController._shoutPreviewMarkerItemObject = value; }

private bool _shoutPreviewMarkerCreationFailed { get => _j17SceneShoutInputController._shoutPreviewMarkerCreationFailed; set => _j17SceneShoutInputController._shoutPreviewMarkerCreationFailed = value; }

internal static float GetApplicationTimeSafe() => SceneShoutInputController.GetApplicationTimeSafe();

private void UpdateShoutHotkeyCharge(InputKey shoutKey, InputKey specialMenuKey) => _j17SceneShoutInputController.UpdateShoutHotkeyCharge(shoutKey, specialMenuKey);

private void TryBeginShoutHotkeyCharge(InputKey key, bool openModeMenu) => _j17SceneShoutInputController.TryBeginShoutHotkeyCharge(key, openModeMenu);

private void CancelShoutHotkeyCharge(string reason) => _j17SceneShoutInputController.CancelShoutHotkeyCharge(reason);

private ShoutTargetingContext BuildCurrentShoutTargetingContext() => _j17SceneShoutInputController.BuildCurrentShoutTargetingContext();

internal static void GetConfiguredShoutRange(out float initialRange, out float maxRange) => SceneShoutInputController.GetConfiguredShoutRange(out initialRange, out maxRange);

private ShoutTargetingContext BuildShoutTargetingContext(float elapsedSeconds) => _j17SceneShoutInputController.BuildShoutTargetingContext(elapsedSeconds);

internal static bool TryGetPlayerPlanarDistanceMeters(Agent targetAgent, out float distanceMeters) => SceneShoutInputController.TryGetPlayerPlanarDistanceMeters(targetAgent, out distanceMeters);

private void DrawShoutRangePreview(ShoutTargetingContext targetingContext) => _j17SceneShoutInputController.DrawShoutRangePreview(targetingContext);

private void UpdateShoutPreviewAgentHighlights(ShoutTargetingContext targetingContext) => _j17SceneShoutInputController.UpdateShoutPreviewAgentHighlights(targetingContext);

private void ClearShoutPreviewAgentHighlights() => _j17SceneShoutInputController.ClearShoutPreviewAgentHighlights();

private static bool TrySetShoutPreviewAgentHighlight(Agent agent, uint? color) => SceneShoutInputController.TrySetShoutPreviewAgentHighlight(agent, color);

private static List<ShoutPreviewLineSegment> BuildShoutPreviewLineSegments(Vec3 center, float range, float startAngle, float endAngle) => SceneShoutInputController.BuildShoutPreviewLineSegments(center, range, startAngle, endAngle);

private static int ClampShoutPreviewSegmentCount(int value, int minValue, int maxValue) => SceneShoutInputController.ClampShoutPreviewSegmentCount(value, minValue, maxValue);

private static void AddShoutPreviewArcSegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float startAngle, float endAngle, int arcSegments) => SceneShoutInputController.AddShoutPreviewArcSegments(segments, center, range, startAngle, endAngle, arcSegments);

private static void AddShoutPreviewBoundarySegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float angle, int radialSegments) => SceneShoutInputController.AddShoutPreviewBoundarySegments(segments, center, range, angle, radialSegments);

private static Vec3 GetShoutPreviewArcPoint(Vec3 center, float range, float angle) => SceneShoutInputController.GetShoutPreviewArcPoint(center, range, angle);

private static Vec3 GetShoutPreviewGroundPoint(Vec3 point) => SceneShoutInputController.GetShoutPreviewGroundPoint(point);

private void UpdateShoutPreviewLineEntities(List<ShoutPreviewLineSegment> segments) => _j17SceneShoutInputController.UpdateShoutPreviewLineEntities(segments);

private void EnsureShoutPreviewMarkerCount(int desiredCount, Vec3 spawnPosition) => _j17SceneShoutInputController.EnsureShoutPreviewMarkerCount(desiredCount, spawnPosition);

private static bool TryBuildShoutPreviewLineFrame(ShoutPreviewLineSegment segment, out MatrixFrame frame) => SceneShoutInputController.TryBuildShoutPreviewLineFrame(segment, out frame);

private static Vec3 CrossVec3(Vec3 a, Vec3 b) => SceneShoutInputController.CrossVec3(a, b);

private static Vec3 ScaleVec3(Vec3 value, float scale) => SceneShoutInputController.ScaleVec3(value, scale);

private static bool TryNormalizeVec3(ref Vec3 value) => SceneShoutInputController.TryNormalizeVec3(ref value);

private void HideExtraShoutPreviewMarkerEntities(int visibleCount) => _j17SceneShoutInputController.HideExtraShoutPreviewMarkerEntities(visibleCount);

private GameEntity TryCreateShoutPreviewMarkerEntity(Vec3 spawnPosition) => _j17SceneShoutInputController.TryCreateShoutPreviewMarkerEntity(spawnPosition);

private ItemObject TryGetShoutPreviewMarkerItemObject() => _j17SceneShoutInputController.TryGetShoutPreviewMarkerItemObject();

private static void TryTintShoutPreviewMarker(GameEntity entity) => SceneShoutInputController.TryTintShoutPreviewMarker(entity);

private void ClearShoutRangePreviewEntities() => _j17SceneShoutInputController.ClearShoutRangePreviewEntities();

private List<Agent> GetAgentsForShoutTargetingContext(ShoutTargetingContext targetingContext) => _j17SceneShoutInputController.GetAgentsForShoutTargetingContext(targetingContext);

internal static Agent ResolvePrimaryAgentForShoutTargetingContext(ShoutTargetingContext targetingContext, List<Agent> agents) => SceneShoutInputController.ResolvePrimaryAgentForShoutTargetingContext(targetingContext, agents);

	// excludedAgentIndices: persistent-session 屏蔽 list; never contains the primary. Null outside a session.
internal static bool TryBuildSceneShoutConversationScope(List<Agent> framedAgents, Agent primaryAgent, int conversationEpoch, out SceneShoutConversationScope scope, out List<Agent> audienceAgents, HashSet<int> excludedAgentIndices = null, bool excludeUnframedAgents = false) => SceneShoutInputController.TryBuildSceneShoutConversationScope(framedAgents, primaryAgent, conversationEpoch, out scope, out audienceAgents, excludedAgentIndices, excludeUnframedAgents);

private void TryStartShoutFromHotkey(bool openModeMenu, ShoutTargetingContext targetingContext = null) => _j17SceneShoutInputController.TryStartShoutFromHotkey(openModeMenu, targetingContext);

private static void LogShoutTargetingContextSnapshot(bool openModeMenu, ShoutTargetingContext targetingContext) => SceneShoutInputController.LogShoutTargetingContextSnapshot(openModeMenu, targetingContext);

private void DrainMainThreadActionsForMissionTick()
	{
		MainThreadActionDrain.DrainMainThreadActionsForMissionTick();
	}

private void ExecuteMainThreadAction(Action action)
	{
		MainThreadActionDrain.ExecuteMainThreadAction(action);
	}

private void LogMainThreadActionBudget13(int pendingAtStart, int processed, int remaining, double elapsedMs)
	{
		MainThreadActionDrain.LogMainThreadActionBudget13(pendingAtStart, processed, remaining, elapsedMs);
	}

	public static void OnApplicationTickForMainThreadActionsExternal()
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				return;
			}
			instance.DrainMainThreadActionsForMissionTick();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] main-thread action application tick drain failed: " + ex.Message);
		}
	}

private void BeginShoutProcessing(string reason) => _j17SceneShoutInputController.BeginShoutProcessing(reason);

private void EndShoutProcessing(string reason) => _j17SceneShoutInputController.EndShoutProcessing(reason);

private float GetShoutProcessingElapsedSeconds() => _j17SceneShoutInputController.GetShoutProcessingElapsedSeconds();

private void ArmShoutHotkeyFocusDebounce(string reason) => _j17SceneShoutInputController.ArmShoutHotkeyFocusDebounce(reason);

private bool ShouldSuppressShoutHotkeyAfterFocusChange() => _j17SceneShoutInputController.ShouldSuppressShoutHotkeyAfterFocusChange();

private void TryShowShoutProcessingBusyMessage() => _j17SceneShoutInputController.TryShowShoutProcessingBusyMessage();

private bool IsMissionPausedByShoutUi() => _j17SceneShoutInputController.IsMissionPausedByShoutUi();

private void ClearStaleShoutProcessingIfNeeded() => _j17SceneShoutInputController.ClearStaleShoutProcessingIfNeeded();















	private readonly object _historyLock = new object();


	private bool _lastShoutDuelLiteralHit = false;

private List<ShoutTradeResourceOption> _shoutTradeOptions { get => _j17SceneTradeController._shoutTradeOptions; set => _j17SceneTradeController._shoutTradeOptions = value; }

private List<ShoutPendingTradeItem> _shoutPendingTradeItems { get => _j17SceneTradeController._shoutPendingTradeItems; set => _j17SceneTradeController._shoutPendingTradeItems = value; }

private int _shoutPendingTradeItemIndex { get => _j17SceneTradeController._shoutPendingTradeItemIndex; set => _j17SceneTradeController._shoutPendingTradeItemIndex = value; }

private ShoutChatMode _shoutTradeMode { get => _j17SceneTradeController._shoutTradeMode; set => _j17SceneTradeController._shoutTradeMode = value; }

private NpcDataPacket _shoutTradeTargetNpc { get => _j17SceneTradeController._shoutTradeTargetNpc; set => _j17SceneTradeController._shoutTradeTargetNpc = value; }

	// The live Agent captured when the short-lived trade UI opens is the stable identity for non-Hero native conversation targets.
private ConversationManager _shoutTradeNativeManager { get => _j17SceneTradeController._shoutTradeNativeManager; set => _j17SceneTradeController._shoutTradeNativeManager = value; }
private Mission _shoutTradeNativeMission { get => _j17SceneTradeController._shoutTradeNativeMission; set => _j17SceneTradeController._shoutTradeNativeMission = value; }
private Agent _shoutTradeNativeAgent { get => _j17SceneTradeController._shoutTradeNativeAgent; set => _j17SceneTradeController._shoutTradeNativeAgent = value; }
private Agent _shoutTradeTargetAgentSnapshot { get => _j17SceneTradeController._shoutTradeTargetAgentSnapshot; set => _j17SceneTradeController._shoutTradeTargetAgentSnapshot = value; }

private bool _shoutTradeActionOnly { get => _j17SceneTradeController._shoutTradeActionOnly; set => _j17SceneTradeController._shoutTradeActionOnly = value; }

private Hero _shoutTradeTargetHeroOverride { get => _j17SceneTradeController._shoutTradeTargetHeroOverride; set => _j17SceneTradeController._shoutTradeTargetHeroOverride = value; }

private CharacterObject _shoutTradeTargetCharacterOverride { get => _j17SceneTradeController._shoutTradeTargetCharacterOverride; set => _j17SceneTradeController._shoutTradeTargetCharacterOverride = value; }

private Action _shoutTradeActionOnlyFinished { get => _j17SceneTradeController._shoutTradeActionOnlyFinished; set => _j17SceneTradeController._shoutTradeActionOnlyFinished = value; }

private int _shoutTradeActionFactSequence { get => _j17SceneTradeController._shoutTradeActionFactSequence; set => _j17SceneTradeController._shoutTradeActionFactSequence = value; }

	private readonly ScenePrepaidTransferRecordOwner _prepaidTransferRecords;
internal readonly SceneAttentionController _j17SceneAttentionController;
internal readonly SceneInteractionLifecycleController _j17SceneInteractionLifecycleController;
internal readonly SceneNativeMechanismController _j17SceneNativeMechanismController;
internal readonly ScenePassiveInteractionController _j17ScenePassiveInteractionController;
internal readonly ScenePresentationBannerlordAdapter _j17ScenePresentationBannerlordAdapter;
internal readonly SceneRelayTargetAdapter _j17SceneRelayTargetAdapter;
internal readonly SceneShoutInputController _j17SceneShoutInputController;
internal readonly SceneSpeechEnqueueAdapter _j17SceneSpeechEnqueueAdapter;
internal readonly SceneSpeechFollowupController _j17SceneSpeechFollowupController;
internal readonly SceneSpeechOutputQueueController _j17SceneSpeechOutputQueueController;
internal readonly SceneTradeBannerlordAdapter _j17SceneTradeBannerlordAdapter;
internal readonly SceneTradeController _j17SceneTradeController;
private static readonly NativeConversationSpeechPorts _j17NativeConversationSpeechQualificationPorts = new NativeConversationSpeechPorts
{
    CanParticipate = agent => CanAgentParticipateInSceneSpeech(agent),
    IsHostile = agent => IsAgentHostileToMainAgent(agent),
    IsValidTargetAgent = (agent, hero, character) => IsValidNativeConversationTargetAgent(agent, hero, character),
    ResolveTarget = (out Hero hero, out CharacterObject character, out string name) => TryResolveNativeConversationTarget(out hero, out character, out name),
    IsInputOpen = () => _nativeConversationInputOpen
};
private static readonly NativeConversationPlaybackWaitAdapter _j17NativeConversationPlaybackWaitAdapter =
    new NativeConversationPlaybackWaitAdapter(new NativeConversationPlaybackWaitPorts
    {
        OutputSyncRoot = () => CurrentInstance?._j17SceneSpeechOutputQueueController.OutputSyncRoot,
        CaptureOwnerCurrent = () => { ShoutBehavior captured = CurrentInstance; if (captured == null) return null; return () => ReferenceEquals(CurrentInstance, captured); },
        PostMainThread = action => CurrentInstance?._mainThreadActions.Enqueue(action),
        IsPlaybackRequestCurrent = request => CurrentInstance?.SceneAudio.IsTtsPlaybackRequestCurrent(request) == true,
        IsPlaybackRequestCurrentAllowCancelled = request => CurrentInstance?.SceneAudio.IsTtsPlaybackRequestCurrent(request, true) == true,
        IsActivePlaybackRequestAllowCancelled = request => CurrentInstance?.SceneAudio.IsActiveTtsPlaybackRequest(request, true) == true,
        RetirePlaybackRequest = request => CurrentInstance?.SceneAudio.RetireTtsPlaybackRequest(request),
        ClearPendingBubble = index => CurrentInstance?._j17SceneSpeechOutputQueueController.ClearPendingTtsBubbleSyncForAgent(index, true),
        ClearPendingFeed = index => CurrentInstance?._j17SceneSpeechOutputQueueController.ClearPendingSceneDialogueFeedForAgent(index),
        CleanupLipSync = index => CurrentInstance?.SceneAudio.CleanupSceneLipSyncAfterPlaybackFinished(index),
        IsTypewriterWaiting = () => ConversationHelper.IsTypewriterWaitingForPlayback,
        StartTypewriter = duration => ConversationHelper.StartTypewriterPlaybackIfWaiting(duration)
    });
private readonly NativeConversationSpeechAdapter _j17NativeConversationSpeechAdapter;
public ShoutBehavior()
	{
		_sceneRevisitState = new SceneRevisitStateOwner(_historyLock);
		_sceneRevisitPersistence = new CampaignSceneRevisitPersistenceAdapter(_sceneRevisitState);
		_sceneRevisitRecords = new SceneRevisitRecordGameAdapter(_sceneRevisitState, ResolveHeroFromAgentIndex, GetPlayerDisplayNameForShout, GetSceneNpcHistoryNameForPrompt, RecordExtraFactToSceneHistory, ParseCurrentScenePlaceAndSpotForPrompt);
        NativeAdmissions = new NativeAdmissionApplicationAdapter(_nativeAdmissionOwner,IsBannerlordMainThreadForNativeActions,()=>ReferenceEquals(CurrentInstance,this),CanSubmitNativeConversationForExternal,_mainThreadActions.Enqueue,NativeConversationMainThreadPreprocessTimeoutMs,TryResolveNativeConversationTarget,TryResolveNativeConversationAgentIndex,IsNativeConversationResponseTargetAvailableForActionDispatch,SubmitNativeConversationTextInternalAsync);
_j17NativeConversationSpeechAdapter = new NativeConversationSpeechAdapter(new NativeConversationSpeechPorts
{
    CurrentOwner = () => CurrentInstance?._j17NativeConversationSpeechAdapter,
    Audio = () => SceneAudio,
    Wait = _j17NativeConversationPlaybackWaitAdapter,
    SetTypingPaused = paused => _floatingTextView?.SetTypingPaused(paused),
    SanitizeUiText = value => SanitizeSceneSpeechText(value),
    SanitizeTtsText = value => SanitizeSceneSpeechTextForTts(value),
    PreferredHeroVoice = hero => MyBehavior.GetNpcVoiceIdForExternal(hero),
    MapHeroVoice = hero => VoiceMapper.ResolveVoiceId(hero),
    MapNonHeroVoice = (key, female, age, index) => VoiceMapper.ResolveVoiceIdForNonHeroKey(key, female, age, index),
    NonHeroAge = character => ResolveNativeConversationNonHeroAge(character),
    NonHeroVoiceKey = (npc, hero, character, index) => BuildNativeConversationNonHeroVoiceKey(npc, hero, character, index),
    CanParticipate = agent => CanAgentParticipateInSceneSpeech(agent),
    IsHostile = agent => IsAgentHostileToMainAgent(agent),
    IsValidTargetAgent = (agent, hero, character) => IsValidNativeConversationTargetAgent(agent, hero, character),
    ResolveTarget = (out Hero hero, out CharacterObject character, out string name) => TryResolveNativeConversationTarget(out hero, out character, out name),
    IsInputOpen = () => _nativeConversationInputOpen,
    TypingDuration = value => EstimateBubbleTypingDurationSeconds(value),
    QueueDiagnostic = index => _j17SceneSpeechOutputQueueController.CaptureQueueDiagnostic(index),
    InteractionDiagnostic = index => _j17SceneInteractionLifecycleController.CaptureInteractionDiagnostic(index)
});
		_deferredHistoryFacts = new SceneDeferredHistoryFactOwner(SceneRosterPromptCaptureAdapter.CloneNpcDataSnapshot, (fact, index) => SceneHistoryPromptCapture.PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(fact, index), (fact, targets, index, receipt) => SceneHistoryPromptCapture.PersistExtraFactToNamedHeroes(fact, targets, index, receipt));
_j17SceneAttentionController = new SceneAttentionController(new SceneAttentionControllerPorts
{
    Get_currentStareTarget = () => _currentStareTarget,
    Set_currentStareTarget = value => _currentStareTarget = value,
    Get_stareTimer = () => _stareTimer,
    Set_stareTimer = value => _stareTimer = value,
    Get_stareTargetLostGraceTimer = () => _stareTargetLostGraceTimer,
    Set_stareTargetLostGraceTimer = value => _stareTargetLostGraceTimer = value,
    ApplyInteractionGraceAndGroupCooldown_L8009 = (float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget, IEnumerable<NpcDataPacket> participantsData) => _j17ScenePassiveInteractionController.ApplyInteractionGraceAndGroupCooldown(graceSeconds, cooldownSeconds, participants, extraTarget, participantsData),
    GetPassiveCooldownGroupAgents_L8070 = (Agent targetAgent) => _j17ScenePassiveInteractionController.GetPassiveCooldownGroupAgents(targetAgent),
    ClearQueuedSceneSpeech_L19216 = () => ClearQueuedSceneSpeech(),
    IsSpeechPipelineBusy_L19229 = () => IsSpeechPipelineBusy(),
    Get_sceneMovement = () => _sceneMovement,
    TryGetInteractionSession = (int index, out SceneInteractionSession session) => _j17SceneInteractionLifecycleController.TryGetInteractionSession(index, out session),
    RemoveInteractionSession = (index) => _j17SceneInteractionLifecycleController.RemoveInteractionSession(index),
    CancelInteractionTimeoutArm = (index) => _j17SceneInteractionLifecycleController.CancelInteractionTimeoutArm(index),
});

_j17SceneInteractionLifecycleController = new SceneInteractionLifecycleController(new SceneInteractionLifecycleControllerPorts
{
    TryDequeuePendingSpeechCompletionToken_L2832 = (int agentIndex, out long interactionToken) => _j17SceneSpeechOutputQueueController.TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken),
    AppendTargetedSceneNpcFact_L18284 = (string factText, int targetAgentIndex, bool persistHeroPrivateHistory) => AppendTargetedSceneNpcFact(factText, targetAgentIndex, persistHeroPrivateHistory),
    RemoveSceneMovementSuppressionAgents_L18679 = (IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.RemoveSceneMovementSuppressionAgents(participantAgentIndices),
    ReleaseAgentFromSceneConversationLocks_L19099 = (Agent agent) => _j17SceneAttentionController.ReleaseAgentFromSceneConversationLocks(agent),
    RestoreAgentAutonomy_L20524 = (Agent agent) => _j17SceneAttentionController.RestoreAgentAutonomy(agent),
    GetSceneAudio = () => SceneAudio,
    RunSceneTtsPlaybackFinishedStep_L176 = (string step, int agentIndex, Action action) => _j17SceneSpeechFollowupController.RunSceneTtsPlaybackFinishedStep(step, agentIndex, action),
    LogTtsReport_L208 = (string stage, int agentIndex, string extra) => LogTtsReport(stage, agentIndex, extra),
    Get_sceneMovement = () => _sceneMovement,
    TriggerImmediateSceneBehaviorReaction_L147 = (string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds, bool skipSceneFactRecord, bool returnSceneSummonOnTimeout, Action onNoSpeech, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, Action<bool> onCompleted) => TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted),
    ReleaseStareAgent = (index) => _j17SceneAttentionController.ReleaseStareAgentForInteraction(index),
    CaptureMovementSuppressionAgents = (indices) => _j17SceneAttentionController.CaptureMovementSuppressionAgents(indices),
    CancelAutonomyRestore = (index) => _j17SceneSpeechFollowupController.CancelAutonomyRestore(index),
    PrepareAutonomyRestore = (index) => _j17SceneSpeechFollowupController.PrepareAutonomyRestore(index),
    ClearPendingSpeechCompletionTokens = (index) => _j17SceneSpeechOutputQueueController.ClearPendingSpeechCompletionTokens(index),
    UnmarkPlaybackStarted = (index) => _j17SceneSpeechOutputQueueController.UnmarkPlaybackStarted(index),
});

_j17SceneNativeMechanismController = new SceneNativeMechanismController(new SceneNativeMechanismControllerPorts
{
    Get_mainThreadActions = () => _mainThreadActions,
    Set_mainThreadActions = value => _mainThreadActions = value,
    TryProcessSetsOwnedSettlementMassacreActionTags_L12771 = (int targetAgentIndex, ref string content) => TryProcessSetsOwnedSettlementMassacreActionTags(targetAgentIndex, ref content),
    TryDrainNativeConversationQueuedActions_L13050 = (string reason) => TryDrainNativeConversationQueuedActions(reason),
    TryTriggerOpenLordsHallAction_L15221 = (NpcDataPacket npc, Agent agent, ref string content) => _j17SceneSpeechFollowupController.TryTriggerOpenLordsHallAction(npc, agent, ref content),
    ScheduleLordsHallMissionEntryAfterSpeech_L19653 = (int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd) => _j17SceneSpeechFollowupController.ScheduleLordsHallMissionEntryAfterSpeech(agentIndex, playbackInfo, reason, waitForConversationEnd),
    Get_sceneMovement = () => _sceneMovement,
    EnqueueSpeechLineWithOptions_L60 = (NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string afterSpeechInfoMessage, TaskCompletionSource<bool> completionSource, float interactionTimeoutSeconds, int interactionParticipantCount, Func<bool> canStillPublish, string playerDirectedActionText, string playerDirectedNpcReplyText) => _j17SceneSpeechEnqueueAdapter.EnqueueSpeechLineWithOptions(npc, content, allNpcData, commitHistory, suppressStare, allowPlayerDirectedActions, requiredConversationEpoch, sceneSummonTargets, sceneGuideTargets, afterSpeechInfoMessage, completionSource, interactionTimeoutSeconds, interactionParticipantCount, canStillPublish, playerDirectedActionText, playerDirectedNpcReplyText),
});

_j17ScenePassiveInteractionController = new ScenePassiveInteractionController(new ScenePassiveInteractionControllerPorts
{
    Get_isProcessingShout = () => _isProcessingShout,
    Set_isProcessingShout = value => _isProcessingShout = value,
    Get_shoutHotkeyChargeActive = () => _shoutHotkeyChargeActive,
    Set_shoutHotkeyChargeActive = value => _shoutHotkeyChargeActive = value,
    IsMultiNpcSceneConversationActive_L7716 = () => _j17SceneInteractionLifecycleController.IsMultiNpcSceneConversationActive(),
    ResolveHeroFromAgentIndex_L17635 = (int agentIndex) => ResolveHeroFromAgentIndex(agentIndex),
    TriggerImmediateSceneBehaviorReaction_L147 = (string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds, bool skipSceneFactRecord, bool returnSceneSummonOnTimeout, Action onNoSpeech, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, Action<bool> onCompleted) => TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted),
});

_j17ScenePresentationBannerlordAdapter = new ScenePresentationBannerlordAdapter(new ScenePresentationBannerlordAdapterPorts
{
    BeginNewPlayerDrivenSceneConversationEpoch = () => BeginNewPlayerDrivenSceneConversationEpoch(),
    Get_isProcessingShout = () => _isProcessingShout,
    Set_isProcessingShout = value => _isProcessingShout = value,
    Get_activeShoutTargetingContext = () => _activeShoutTargetingContext,
    Set_activeShoutTargetingContext = value => _activeShoutTargetingContext = value,
    GetAgentsForShoutTargetingContext_L1285 = (ShoutTargetingContext targetingContext) => _j17SceneShoutInputController.GetAgentsForShoutTargetingContext(targetingContext),
    Get_historyLock = () => _historyLock,
    Get_floatingTextView = () => _floatingTextView,
    OnShoutConfirmedWithContext_L17001 = (string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _j17SceneShoutInputController.OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex),
    ResumeGame_L21311 = () => _j17SceneShoutInputController.ResumeGame(),
    GetSceneHistoryOwner = () => SceneHistoryOwner,
    IsPresentationRoundActive_L26 = () => IsPresentationRoundActive(),
    GetPresentation = () => Presentation,
    EnsurePresentationSession_L55 = (IReadOnlyList<Agent> agents, int primary) => EnsurePresentationSession(agents, primary),
    Get_isWaitingForScenePostprocessGate = () => _isWaitingForScenePostprocessGate,
    Set_isWaitingForScenePostprocessGate = value => _isWaitingForScenePostprocessGate = value,
});

_j17SceneRelayTargetAdapter = new SceneRelayTargetAdapter(new SceneRelayTargetAdapterPorts
{

});

_j17SceneShoutInputController = new SceneShoutInputController(new SceneShoutInputControllerPorts
{
    OpenBattleShoutInput = imageOnly => OpenBattleShoutInput(imageOnly),
    ResetPassiveStareTracking_L7764 = () => _j17ScenePassiveInteractionController.ResetPassiveStareTracking(),
    OnShoutTagTestConfirmed_L13792 = (string input, int? forcedPrimaryAgentIndex) => OnShoutTagTestConfirmed(input, forcedPrimaryAgentIndex),
    BeginShoutTradeFlow_L14081 = (NpcDataPacket targetNpc, ShoutChatMode mode) => _j17SceneTradeController.BeginShoutTradeFlow(targetNpc, mode),
    ResolveHeroFromAgentIndex_L17635 = (int agentIndex) => ResolveHeroFromAgentIndex(agentIndex),
    ActivateMultiSceneMovementSuppression_L18651 = (IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.ActivateMultiSceneMovementSuppression(participantAgentIndices),
    DeactivateMultiSceneMovementSuppression_L18702 = () => _j17SceneAttentionController.DeactivateMultiSceneMovementSuppression(),
    PauseTtsForShoutUi_L87 = () => PauseTtsForShoutUi(),
    ResumeTtsAfterShoutUi_L88 = () => ResumeTtsAfterShoutUi(),
    UpdatePresentationHotkey_L76 = (InputKey shoutKey, InputKey specialMenuKey) => UpdatePresentationHotkey(shoutKey, specialMenuKey),
    TryOpenPresentationSessionFromWheel_L108 = () => _j17ScenePresentationBannerlordAdapter.TryOpenPresentationSessionFromWheel(),
    GetPresentation = () => Presentation,
    Get_shoutHotkeyChargeMergesIntoSession = () => _shoutHotkeyChargeMergesIntoSession,
    Set_shoutHotkeyChargeMergesIntoSession = value => _shoutHotkeyChargeMergesIntoSession = value,
    TryOpenPresentationTradeFromWheel_L82 = (string mode) => _j17SceneTradeController.TryOpenPresentationTradeFromWheel(mode),
    Get_sceneConversationEpoch = () => _sceneConversationEpoch,
    Set_sceneConversationEpoch = value => _sceneConversationEpoch = value,
    Get_isWaitingForScenePostprocessGate = () => _isWaitingForScenePostprocessGate,
    Set_isWaitingForScenePostprocessGate = value => _isWaitingForScenePostprocessGate = value,
    Get_scenePlayerShoutRequestOwner = () => _scenePlayerShoutRequestOwner,
    ProcessCapturedScenePlayerShoutAsync_L191 = (string shoutText, string extraFact, int? forcedPrimaryAgentIndex, ScenePlayerShoutRequest request, Action<Action> runWithObservationScope, SceneGroupReceipt receipt) => ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, runWithObservationScope, receipt),
    CaptureScenePlayerShoutRequest_L195 = (IEnumerable<Agent> framedTargets, int primaryAgentIndex) => CaptureScenePlayerShoutRequest(framedTargets, primaryAgentIndex),
    IsScenePlayerShoutRequestCurrent_L196 = (ScenePlayerShoutRequest request) => IsScenePlayerShoutRequestCurrent(request),
});

_j17SceneSpeechEnqueueAdapter = new SceneSpeechEnqueueAdapter(new SceneSpeechEnqueueAdapterPorts
{
    Get_sceneSpeechQueueOwner = () => _sceneSpeechQueueOwner,
});

_j17SceneSpeechFollowupController = new SceneSpeechFollowupController(new SceneSpeechFollowupControllerPorts
{
    ShowNpcSpeechOutput_L2959 = (NpcDataPacket npc, Agent liveAgent, string content, bool allowTts, bool attachTtsToSceneAgent, bool suppressInteractionTimeoutArm) => ShowNpcSpeechOutput(npc, liveAgent, content, allowTts, attachTtsToSceneAgent, suppressInteractionTimeoutArm),
    RecordNegotiatedNonHeroBribe_L15018 = (string targetKey, int goldAmount) => RecordNegotiatedNonHeroBribe(targetKey, goldAmount),
    RecordResponseForAllNearbySafe_L17560 = (List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt) => RecordResponseForAllNearbySafe(nearbyData, speakerAgentIndex, speakerName, response, requireMemoryReceipt),
    RemoveSceneMovementSuppressionAgents_L18679 = (IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.RemoveSceneMovementSuppressionAgents(participantAgentIndices),
    RestoreAgentAutonomy_L20524 = (Agent agent) => _j17SceneAttentionController.RestoreAgentAutonomy(agent),
    PersistNpcSpeechToNamedHeroes_L21118 = (int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt) => PersistNpcSpeechToNamedHeroes(speakerAgentIndex, speakerName, response, nearbyData, requireMemoryReceipt),
    ReleaseStareAgent = (index) => _j17SceneAttentionController.ReleaseStareAgentForInteraction(index),
});

_j17SceneSpeechOutputQueueController = new SceneSpeechOutputQueueController(new SceneSpeechOutputQueueControllerPorts
{
    Get_mainThreadActions = () => _mainThreadActions,
    Set_mainThreadActions = value => _mainThreadActions = value,
    TryShowNpcBubble_L2127 = (Agent liveAgent, string content, float typingDurationSeconds) => _j17ScenePresentationBannerlordAdapter.TryShowNpcBubble(liveAgent, content, typingDurationSeconds),
    ResetSceneAudioRequestOwnership_L67 = () => ResetSceneAudioRequestOwnership(),
    IsTtsPlaybackRequestCurrent_L73 = (TtsEngine.PlaybackRequest request, bool allowCancelled) => IsTtsPlaybackRequestCurrent(request, allowCancelled),
    LogTtsReport_L208 = (string stage, int agentIndex, string extra) => LogTtsReport(stage, agentIndex, extra),
    Get_sceneMovement = () => _sceneMovement,
    IsSceneConversationEpochCurrent_L223 = (int epoch) => IsSceneConversationEpochCurrent(epoch),
    CaptureConversationEpoch = () => _sceneConversationEpoch,
    ClearInteractionTimeoutArms = () => _j17SceneInteractionLifecycleController.ClearInteractionTimeoutArms(),
});

_j17SceneTradeBannerlordAdapter = new SceneTradeBannerlordAdapter(new SceneTradeBannerlordAdapterPorts
{
    Get_shoutTradeNativeAgent = () => _shoutTradeNativeAgent,
    Get_shoutTradeNativeMission = () => _shoutTradeNativeMission,
    Get_shoutTradeNativeManager = () => _shoutTradeNativeManager,
    Get_activeShoutTargetingContext = () => _activeShoutTargetingContext,
    Set_activeShoutTargetingContext = value => _activeShoutTargetingContext = value,
    GetAgentsForShoutTargetingContext_L1285 = (ShoutTargetingContext targetingContext) => _j17SceneShoutInputController.GetAgentsForShoutTargetingContext(targetingContext),
    Get_shoutTradeOptions = () => _shoutTradeOptions,
    Set_shoutTradeOptions = value => _shoutTradeOptions = value,
    Get_shoutPendingTradeItems = () => _shoutPendingTradeItems,
    Set_shoutPendingTradeItems = value => _shoutPendingTradeItems = value,
    Get_shoutPendingTradeItemIndex = () => _shoutPendingTradeItemIndex,
    Set_shoutPendingTradeItemIndex = value => _shoutPendingTradeItemIndex = value,
    Get_shoutTradeMode = () => _shoutTradeMode,
    Set_shoutTradeMode = value => _shoutTradeMode = value,
    Get_shoutTradeTargetNpc = () => _shoutTradeTargetNpc,
    Set_shoutTradeTargetNpc = value => _shoutTradeTargetNpc = value,
    Get_shoutTradeTargetAgentSnapshot = () => _shoutTradeTargetAgentSnapshot,
    Set_shoutTradeTargetAgentSnapshot = value => _shoutTradeTargetAgentSnapshot = value,
    Get_shoutTradeActionOnly = () => _shoutTradeActionOnly,
    Set_shoutTradeActionOnly = value => _shoutTradeActionOnly = value,
    Get_shoutTradeTargetHeroOverride = () => _shoutTradeTargetHeroOverride,
    Set_shoutTradeTargetHeroOverride = value => _shoutTradeTargetHeroOverride = value,
    Get_shoutTradeTargetCharacterOverride = () => _shoutTradeTargetCharacterOverride,
    Set_shoutTradeTargetCharacterOverride = value => _shoutTradeTargetCharacterOverride = value,
    GetShoutTradeTargetAgentIndex_L14076 = () => _j17SceneTradeController.GetShoutTradeTargetAgentIndex(),
    AppendShoutTradeActionFactSequence_L14587 = (string fact) => _j17SceneTradeController.AppendShoutTradeActionFactSequence(fact),
    RecordScenePrepaidTransfer_L14987 = (string targetKey, int goldAmount) => RecordScenePrepaidTransfer(targetKey, goldAmount),
    QueuePendingCurrentAfefFactForAgent_L17336 = (int targetAgentIndex, string fact) => QueuePendingCurrentAfefFactForAgent(targetAgentIndex, fact),
    QueuePendingCurrentNativeAfefFactForKey_L17363 = (string key, string fact) => QueuePendingCurrentNativeAfefFactForKey(key, fact),
    AppendActionAfefFactToSceneHistoryInOrder_L17395 = (int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory) => AppendActionAfefFactToSceneHistoryInOrder(targetAgentIndex, fact, mirrorToNativeSharedHistory),
    PromotePersonalizedExtraFactInScenePrivateHistory_L17524 = (string personalizedExtraFact, int personalizedAgentIndex) => PromotePersonalizedExtraFactInScenePrivateHistory(personalizedExtraFact, personalizedAgentIndex),
    ResolveHeroFromAgentIndex_L17635 = (int agentIndex) => ResolveHeroFromAgentIndex(agentIndex),
    GetPresentation = () => Presentation,
});

_j17SceneTradeController = new SceneTradeController(new SceneTradeControllerPorts
{
    Get_activeShoutTargetingContext = () => _activeShoutTargetingContext,
    Set_activeShoutTargetingContext = value => _activeShoutTargetingContext = value,
    BeginShoutProcessing_L1570 = (string reason) => _j17SceneShoutInputController.BeginShoutProcessing(reason),
    BuildShoutTargetEncyclopediaAction_L13863 = (NpcDataPacket targetNpc) => _j17SceneShoutInputController.BuildShoutTargetEncyclopediaAction(targetNpc),
    EnsureShoutTradePrimaryTargetValidForCommit_L14056 = (bool requireCurrentShoutFrame) => _j17SceneTradeBannerlordAdapter.EnsureShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame),
    BuildShoutTradeOptions_L14160 = () => _j17SceneTradeBannerlordAdapter.BuildShoutTradeOptions(),
    RecordNativeConversationTradeActionFact_L14519 = (string fact) => _j17SceneTradeBannerlordAdapter.RecordNativeConversationTradeActionFact(fact),
    ApplyShoutGiveTransfer_L14619 = () => _j17SceneTradeBannerlordAdapter.ApplyShoutGiveTransfer(),
    BuildShoutTradeFactText_L15241 = (bool isGive) => _j17SceneTradeBannerlordAdapter.BuildShoutTradeFactText(isGive),
    EstimateShoutPendingShowTotalValue_L15395 = () => _j17SceneTradeBannerlordAdapter.EstimateShoutPendingShowTotalValue(),
    ShowShoutPendingDisplayValueMessage_L15421 = (long totalValue) => _j17SceneTradeBannerlordAdapter.ShowShoutPendingDisplayValueMessage(totalValue),
    RecordShoutShownResources_L16847 = () => _j17SceneTradeBannerlordAdapter.RecordShoutShownResources(),
    OnShoutConfirmedWithContext_L17001 = (string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _j17SceneShoutInputController.OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex),
    ActivateMultiSceneMovementSuppression_L18651 = (IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.ActivateMultiSceneMovementSuppression(participantAgentIndices),
    PauseGame_L21301 = () => _j17SceneShoutInputController.PauseGame(),
    ResumeGame_L21311 = () => _j17SceneShoutInputController.ResumeGame(),
    OnShoutCancelled_L21322 = () => _j17SceneShoutInputController.OnShoutCancelled(),
    BuildPresentationTargetingContext_L51 = () => _j17ScenePresentationBannerlordAdapter.BuildPresentationTargetingContext(),
    EnsurePresentationSessionForWheelAction_L119 = () => _j17ScenePresentationBannerlordAdapter.EnsurePresentationSessionForWheelAction(),
    GetPresentation = () => Presentation,
    GetShoutTradeTargetIneligibility_L33 = (ShoutChatMode mode) => _j17SceneTradeBannerlordAdapter.GetShoutTradeTargetIneligibility(mode),
});
		MainThreadActionDrain = new ConversationMainThreadActionDrain(_mainThreadActions);
        NativeDetachedPostprocesses = new NativeDetachedPostprocessApplicationAdapter(() => ReferenceEquals(CurrentInstance, this), TryResolveNativeConversationTarget, TryResolveNativeConversationAgentIndex, IsNativeConversationResponseTargetAvailableForActionDispatch, TryGetNativeConversationPersistentHistoryTargetForExternal, _conversationGameThreadDispatcher, NativeSharedPromptCapture.CaptureNativeDetachedPostprocessWork, (work, raw) => CompleteSceneUnifiedActionPostprocess(work, true, raw, null));
		_prepaidTransferRecords = new ScenePrepaidTransferRecordOwner(_historyLock, GetCurrentCampaignDaySafe, GetCurrentSettlementIdSafe);
	}
	private ScenePrepaidTransferRecordOwner PrepaidTransferRecords => _prepaidTransferRecords;
	private Dictionary<string, ScenePrepaidTransferRecord> _scenePrepaidTransfers => _prepaidTransferRecords.Records;

	private readonly SceneRevisitStateOwner _sceneRevisitState;
	private readonly CampaignSceneRevisitPersistenceAdapter _sceneRevisitPersistence;
	private readonly SceneRevisitRecordGameAdapter _sceneRevisitRecords;
	private Dictionary<string, int> _sceneHeroRevisitDays { get => _sceneRevisitState.Days; set => _sceneRevisitState.Days = value; }

	private Dictionary<string, string> _sceneHeroRevisitDayStorage { get => _sceneRevisitPersistence.Storage; set => _sceneRevisitPersistence.Storage = value; }

	private HashSet<string> _sceneHeroRevisitHandledThisSession => _sceneRevisitState.HandledThisSession;

	private HashSet<string> _sceneHeroFirstMeetingShownThisSession => _sceneRevisitState.FirstMeetingShownThisSession;

	private const string PlayerCraftedAfefInspectionSuffix = ScenePromptMessageProjectionComposer.PlayerCraftedAfefInspectionSuffix;

	private readonly SceneDeferredHistoryFactOwner _deferredHistoryFacts;
	private string _pendingHeroHistoryExtraFactAfterSceneReply { get => _deferredHistoryFacts.Fact; set => _deferredHistoryFacts.Fact = value; }

	private List<NpcDataPacket> _pendingHeroHistoryExtraFactTargetsAfterSceneReply { get => _deferredHistoryFacts.Targets; set => _deferredHistoryFacts.Targets = value; }

	private int _pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply { get => _deferredHistoryFacts.PersonalizedAgentIndex; set => _deferredHistoryFacts.PersonalizedAgentIndex = value; }

private List<Agent> _staringAgents { get => _j17SceneAttentionController._staringAgents; set => _j17SceneAttentionController._staringAgents = value; }

private Dictionary<int, Vec3> _staringAgentAnchors { get => _j17SceneAttentionController._staringAgentAnchors; }

private HashSet<int> _staringUseConversationAgents { get => _j17SceneAttentionController._staringUseConversationAgents; }

private object _pendingSceneConversationAttentionReleaseLock { get => _j17SceneAttentionController._pendingSceneConversationAttentionReleaseLock; }

private HashSet<int> _pendingSceneConversationAttentionReleaseAgentIndices { get => _j17SceneAttentionController._pendingSceneConversationAttentionReleaseAgentIndices; }

private float _stopStaringTime { get => _j17SceneAttentionController._stopStaringTime; set => _j17SceneAttentionController._stopStaringTime = value; }



	private readonly ScenePendingAfefFactsOwner _scenePendingAfefFactsOwner = new ScenePendingAfefFactsOwner();





private object _pendingNativeSceneMechanismActionLock { get => _j17SceneNativeMechanismController._pendingNativeSceneMechanismActionLock; }

private Queue<PendingNativeSceneMechanismAction> _pendingNativeSceneMechanismActions { get => _j17SceneNativeMechanismController._pendingNativeSceneMechanismActions; }


	private const float NativeSceneTauntFightDelaySeconds = 10f;

private object _pendingNativeSceneTauntFightLock { get => _j17SceneNativeMechanismController._pendingNativeSceneTauntFightLock; }

private PendingNativeSceneTauntFight _pendingNativeSceneTauntFight { get => _j17SceneNativeMechanismController._pendingNativeSceneTauntFight; set => _j17SceneNativeMechanismController._pendingNativeSceneTauntFight = value; }

	private static int _sceneHistorySessionId { get => SceneConversationHistoryOwner.SessionId; set => SceneConversationHistoryOwner.SessionId = value; }

	private static long _currentConversationEventSequence { get => SceneConversationHistoryOwner.CurrentEventSequence; set => SceneConversationHistoryOwner.CurrentEventSequence = value; }



	internal const int AUTO_GROUP_CHAT_MAX_LINES = 8;

private Agent _currentStareTarget { get => _j17ScenePassiveInteractionController._currentStareTarget; set => _j17ScenePassiveInteractionController._currentStareTarget = value; }

private float _stareTimer { get => _j17ScenePassiveInteractionController._stareTimer; set => _j17ScenePassiveInteractionController._stareTimer = value; }

private float _stareTargetLostGraceTimer { get => _j17ScenePassiveInteractionController._stareTargetLostGraceTimer; set => _j17ScenePassiveInteractionController._stareTargetLostGraceTimer = value; }

private float _interactionGraceTimer { get => _j17ScenePassiveInteractionController._interactionGraceTimer; set => _j17ScenePassiveInteractionController._interactionGraceTimer = value; }

private Dictionary<string, float> _passiveCooldowns { get => _j17ScenePassiveInteractionController._passiveCooldowns; set => _j17ScenePassiveInteractionController._passiveCooldowns = value; }


	private const float DEFAULT_STARE_TRIGGER_TIME = 15f;

	private const float STARE_TARGET_LOST_GRACE = 2f;

	private const float PASSIVE_STARE_COOLDOWN = 10f;

	private const float ACTIVE_CHAT_COOLDOWN = 300f;

	private const float PASSIVE_INTERACTION_GRACE = 0.75f;

	internal const float ACTIVE_INTERACTION_IDLE_TIMEOUT = 45f;

	private const float ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT = 300f;

	private const float ACTIVE_INTERACTION_DYNAMIC_SINGLE_TIMEOUT_CAP = 240f;

	private const float ACTIVE_INTERACTION_DYNAMIC_GROUP_TIMEOUT_CAP = 600f;

	private const float ACTIVE_INTERACTION_GROUP_SPEAKER_BONUS_SECONDS = 20f;

	private const float ACTIVE_INTERACTION_IDLE_PLAYER_RANGE = 10f;

	private const float ACTIVE_INTERACTION_DISTANT_RELEASE_MIN_EXTRA_RANGE = 15f;

	private const float ACTIVE_INTERACTION_DISTANT_RELEASE_EXTRA_RATIO = 0.5f;







	// 已实测成功：带路到达后必须等到达台词结束，再短暂停顿后返回。
	// 严禁改回 ACTIVE_INTERACTION_IDLE_TIMEOUT，严禁复用普通交互超时；会破坏带路到达/说话/返回节奏。


	private const float LORDS_HALL_ENTRY_TTS_FALLBACK_SECONDS = 15f;


	private const float PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS = 60f;

	private const float MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL = 0.01f;

	private const float MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS = 0.25f;

	private const float LIP_SYNC_SAFE_MAX_DISTANCE = NativeConversationSpeechAdapter.LIP_SYNC_SAFE_MAX_DISTANCE;

	private static readonly Regex MeetingSceneShoutTauntTagRegex = ConversationActionBoundaryBannerlordAdapter.MeetingSceneShoutTauntTagRegex;

	internal const string NpcSurrenderActionTag = ConversationActionBoundaryBannerlordAdapter.NpcSurrenderActionTag;

	private const string SiegeSurrenderActionTag = ConversationActionBoundaryBannerlordAdapter.SiegeSurrenderActionTag;

	public const string PersistentAdpDebtPostprocessRuleId = PromptPreprocessRuleIdAssembler.PersistentAdpDebtRuleId;

	internal const string CustomPolicyAgendaPostprocessRuleId = "kingdom_agenda";

	private const string CustomPolicyAgendaActionTag = ConversationActionBoundaryBannerlordAdapter.CustomPolicyAgendaActionTag;

	private const string AutoGroupRelayRuleId = "scene_auto_group_relay";

	private const string AutoGroupRelayTagTemplate = "[RELAY:接力编号]";

	internal const string AutoGroupRelayPositiveSoundEvent = "event:/ui/notification/relation";

	internal const string AutoGroupRelayNegativeSoundEvent = "event:/ui/notification/coins_negative";





	private static readonly Regex SetsOwnedSettlementMassacreRequestActionTagRegex = ConversationActionBoundaryBannerlordAdapter.SetsOwnedSettlementMassacreRequestActionTagRegex;

	private static readonly Regex SetsOwnedSettlementMassacreStartActionTagRegex = ConversationActionBoundaryBannerlordAdapter.SetsOwnedSettlementMassacreStartActionTagRegex;

	private static readonly Regex SetsOwnedSettlementMassacreStopActionTagRegex = ConversationActionBoundaryBannerlordAdapter.SetsOwnedSettlementMassacreStopActionTagRegex;

	private static readonly Regex SetsOwnedSettlementMassacreCancelRequestActionTagRegex = ConversationActionBoundaryBannerlordAdapter.SetsOwnedSettlementMassacreCancelRequestActionTagRegex;

	private static readonly Regex TroopInspectionPrisonerSlaughterActionTagRegex = ConversationActionBoundaryBannerlordAdapter.TroopInspectionPrisonerSlaughterActionTagRegex;


	private static readonly Regex CustomPolicyAgendaActionTagRegex = ConversationActionBoundaryBannerlordAdapter.CustomPolicyAgendaActionTagRegex;


























	private const int DeferredPostprocessTargetUnavailableResult = ConversationActionBoundaryBannerlordAdapter.DeferredPostprocessTargetUnavailableResult;

	internal const int NativeConversationMainThreadPreprocessTimeoutMs = 30000;


	private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
	private static int _nativeDetachedPromptParityLoggingEnabled;
	internal static bool IsNativeDetachedPromptParityLoggingEnabledForPostprocess => Volatile.Read(ref _nativeDetachedPromptParityLoggingEnabled) != 0;
	internal static List<PostprocessSummonTarget> CapturePostprocessSummonTargets(IEnumerable<SceneSummonPromptTarget> targets)
	{
		return SharedPromptCaptureBannerlordAdapter.CapturePostprocessSummonTargets(targets);
	}
	internal static List<PostprocessGuideTarget> CapturePostprocessGuideTargets(IEnumerable<SceneGuidePromptTarget> targets)
	{
		return SharedPromptCaptureBannerlordAdapter.CapturePostprocessGuideTargets(targets);
	}







private float _tickTimer { get => _j17ScenePassiveInteractionController._tickTimer; set => _j17ScenePassiveInteractionController._tickTimer = value; }

private float _nextProactiveSceneOpeningProbeMissionTime { get => _j17ScenePassiveInteractionController._nextProactiveSceneOpeningProbeMissionTime; set => _j17ScenePassiveInteractionController._nextProactiveSceneOpeningProbeMissionTime = value; }

private object _multiSceneMovementSuppressionLock { get => _j17SceneAttentionController._multiSceneMovementSuppressionLock; }

private HashSet<int> _multiSceneMovementSuppressionAgentIndices { get => _j17SceneAttentionController._multiSceneMovementSuppressionAgentIndices; }

private bool _multiSceneMovementSuppressionActive { get => _j17SceneAttentionController._multiSceneMovementSuppressionActive; set => _j17SceneAttentionController._multiSceneMovementSuppressionActive = value; }

private float _multiSceneMovementSuppressionTimer { get => _j17SceneAttentionController._multiSceneMovementSuppressionTimer; set => _j17SceneAttentionController._multiSceneMovementSuppressionTimer = value; }













private Dictionary<int, SceneInteractionSession> _activeInteractionSessions { get => _j17SceneInteractionLifecycleController._activeInteractionSessions; }















private object _ttsBubbleSyncLock { get => _j17SceneSpeechOutputQueueController._ttsBubbleSyncLock; }

private Dictionary<int, Queue<PendingNpcBubbleEntry>> _pendingNpcBubbleQueues { get => _j17SceneSpeechOutputQueueController._pendingNpcBubbleQueues; }

private Dictionary<int, Queue<float>> _pendingAudioDurationQueues { get => _j17SceneSpeechOutputQueueController._pendingAudioDurationQueues; }

private HashSet<int> _ttsPlaybackStartedAgents { get => _j17SceneSpeechOutputQueueController._ttsPlaybackStartedAgents; }

private Dictionary<int, Queue<long>> _pendingSpeechCompletionTokenQueues { get => _j17SceneSpeechOutputQueueController._pendingSpeechCompletionTokenQueues; }



private Dictionary<int, Queue<PendingSceneDialogueFeedEntry>> _pendingSceneDialogueFeedQueues { get => _j17SceneSpeechOutputQueueController._pendingSceneDialogueFeedQueues; }

private Dictionary<int, PendingInteractionTimeoutArm> _pendingInteractionTimeoutArms { get => _j17SceneInteractionLifecycleController._pendingInteractionTimeoutArms; }



private Dictionary<int, PendingMeetingReleaseAfterSpeech> _pendingMeetingReleasesAfterSpeech { get => _j17SceneSpeechFollowupController._pendingMeetingReleasesAfterSpeech; }

private Dictionary<int, PendingWorldMapMissionExitAfterSpeech> _pendingWorldMapMissionExitsAfterSpeech { get => _j17SceneSpeechFollowupController._pendingWorldMapMissionExitsAfterSpeech; }

private PendingLordsHallMissionEntryAfterSpeech _pendingLordsHallMissionEntryAfterSpeech { get => _j17SceneSpeechFollowupController._pendingLordsHallMissionEntryAfterSpeech; set => _j17SceneSpeechFollowupController._pendingLordsHallMissionEntryAfterSpeech = value; }

private bool _pendingLordsHallMissionEntryConversationEndHookRegistered { get => _j17SceneSpeechFollowupController._pendingLordsHallMissionEntryConversationEndHookRegistered; set => _j17SceneSpeechFollowupController._pendingLordsHallMissionEntryConversationEndHookRegistered = value; }



private Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech> _pendingSceneAutonomyRestoresAfterSpeech { get => _j17SceneSpeechFollowupController._pendingSceneAutonomyRestoresAfterSpeech; }















	private FloatingTextMissionView _floatingTextView => FloatingTextManager.Instance.MissionView;

	internal static ShoutBehavior CurrentInstance
	{
		get
		{
			try
			{
				return Campaign.Current?.GetCampaignBehavior<ShoutBehavior>();
			}
			catch
			{
				return null;
			}
		}
	}

	public static void ResetTransientRuntimeForLoadedSaveExternal(string reason)
	{
		try
		{
			CloseNativeConversationInput(clearSessionHistory: true);
			_nativeSessionOwner.ClearAll();
			Interlocked.Increment(ref SceneConversationHistoryOwner.SessionId);
			Interlocked.Exchange(ref SceneConversationHistoryOwner.CurrentEventSequence, 0L);
			CurrentInstance?.ResetInstanceTransientRuntimeForLoadedSave(reason);
			Logger.Log("SaveRuntimeGuard", "shout_transient_cleared reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("SaveRuntimeGuard", "[WARN] shout transient clear failed: " + ex.Message);
		}
	}

	private void ResetInstanceTransientRuntimeForLoadedSave(string reason)
	{
		try
		{
            NativeAdmissions.EndConversation();
			lock (_historyLock)
			{
				SceneHistoryOwner.Reset();
				_sceneHeroRevisitHandledThisSession.Clear();
				_sceneHeroFirstMeetingShownThisSession.Clear();
				_pendingHeroHistoryExtraFactAfterSceneReply = "";
				_pendingHeroHistoryExtraFactTargetsAfterSceneReply.Clear();
				_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1;
			}
			ClearPendingCurrentAfefFacts();
			ClearPendingNativeSceneMechanismActions("loaded_save:" + (reason ?? ""));
			_staringAgents.Clear();
			_staringAgentAnchors.Clear();
			_staringUseConversationAgents.Clear();
			_currentStareTarget = null;
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_interactionGraceTimer = 0f;
			_passiveCooldowns.Clear();
			_activeInteractionSessions.Clear();
			_pendingInteractionTimeoutArms.Clear();
			_pendingWorldMapMissionExitsAfterSpeech.Clear();
			_sceneMovement.ClearTransientFollowers();
			_sceneMovement.InvalidateSceneCommandFollowerCache();
			_sceneConversation.ResetImmediateReactions();
			RetireModuleSceneGroup("scene.stale_context");
			RetireQueuedSceneSpeech(_sceneSpeechQueueOwner.Reset());
			ResetPendingMainThreadFunctions();
			_sceneConversationEpoch = 0;
			_isProcessingShout = false;
			_shoutProcessingStartedAt = -1f;
			_isWaitingForScenePostprocessGate = false;
			ResetShoutTradeState();
		}
		catch (Exception ex)
		{
			Logger.Log("SaveRuntimeGuard", "[WARN] shout instance transient clear failed: " + ex.Message);
		}
	}

	internal static void NotifyUiInterruption(string reason = "UI_SCREEN")
	{
		try
		{
			CurrentInstance?.HandleEscapePressedForAudioSafety(string.IsNullOrWhiteSpace(reason) ? "UI_SCREEN" : reason);
		}
		catch
		{
		}
	}

	internal static void NotifyCriticalUiTransition(string reason = "UI_TRANSITION")
	{
		try
		{
			CurrentInstance?.HandleCriticalUiTransitionForLipSyncSafety(string.IsNullOrWhiteSpace(reason) ? "UI_TRANSITION" : reason);
		}
		catch
		{
		}
	}

internal static void NotifyGameWindowFocusChanged(bool focusGained) => SceneShoutInputController.NotifyGameWindowFocusChanged(focusGained);

internal static bool IsSceneShoutInputActiveForExternal() => SceneShoutInputController.IsSceneShoutInputActiveForExternal();

private bool TryShowNpcBubble(Agent liveAgent, string content, float typingDurationSeconds = -1f) => _j17ScenePresentationBannerlordAdapter.TryShowNpcBubble(liveAgent, content, typingDurationSeconds);

	/// <summary>
	/// Shared visual-only entry point for data-driven town chatter. Ambient lines use
	/// the same participation guards and floating-text layer as scene speech, without
	/// making an AI request.
	/// </summary>
internal static bool TryShowPassiveNpcBubbleForExternal(Agent liveAgent, string content, float typingDurationSeconds = -1f) => ScenePresentationBannerlordAdapter.TryShowPassiveNpcBubbleForExternal(liveAgent, content, typingDurationSeconds);

private void TryTriggerPendingProactiveSceneOpening() => _j17ScenePassiveInteractionController.TryTriggerPendingProactiveSceneOpening();

private static Agent FindProactiveSceneOpeningAgent(Mission mission, Hero targetHero) => ScenePassiveInteractionController.FindProactiveSceneOpeningAgent(mission, targetHero);

	internal static string BuildProactiveSceneOpeningFactText(string extraFact, string promptText)
	{
		return AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer.BuildProactiveSceneOpeningFactText(extraFact, promptText);
	}

	private static string BuildProactiveSceneOpeningPromptSection(string extraFact, string promptText)
	{
		return BuildProactiveSceneOpeningFactText(extraFact, promptText);
	}

	internal static string BuildNpcInitiatedOpeningUserText(string extraFact, string promptText)
	{
		return AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer.BuildNpcInitiatedOpeningUserText(extraFact, promptText);
	}

	internal static string BuildNpcInitiatedOpeningPersistentFactText(string extraFact)
	{
		return ScenePromptMessageProjectionComposer.BuildNpcInitiatedOpeningPersistentFactText(extraFact);
	}

	private static string StripAfefPrefixForPromptSection(string text)
	{
        return ScenePromptMessageProjectionComposer.StripAfefPrefixForPromptSection(text);
    }

	private static string StripAfefPromptScopeLabel(string text)
	{
		return ConversationSpeechTextRules.StripAfefPromptScopeLabel(text);
	}




	private static string BuildScopedAfefFactLinesForPrompt(string text, bool isCurrent)
	{
        return ScenePromptMessageProjectionComposer.BuildScopedAfefFactLinesForPrompt(text, isCurrent);
    }

	internal static string BuildCurrentAfefFactPromptBlock(string extraFact)
	{
        return ScenePromptMessageProjectionComposer.BuildCurrentAfefFactPromptBlock(extraFact);
    }

internal static bool CanAgentParticipateInSceneSpeech(Agent agent) => SceneShoutInputController.CanAgentParticipateInSceneSpeech(agent);

	private bool IsTtsPlaybackEnabledForShout() => NativeConversationSpeechAdapter.IsTtsPlaybackEnabledForShout();

	private bool IsTtsPlaybackEnabledForNativeConversation(out string disabledReason) => NativeConversationSpeechAdapter.IsTtsPlaybackEnabledForNativeConversation(out disabledReason);

	private void ResumeTtsForNativeConversationReply() => _j17NativeConversationSpeechAdapter.ResumeTtsForNativeConversationReply();

	private void EnsureTtsPlaybackEventsSubscribedForNativeConversation() => _j17NativeConversationSpeechAdapter.EnsureTtsPlaybackEventsSubscribedForNativeConversation();

private void EnqueuePendingNpcBubble(int agentIndex, Agent liveAgent, string uiContent, string npcName, float fallbackDurationSeconds = -1f) => _j17SceneSpeechOutputQueueController.EnqueuePendingNpcBubble(agentIndex, liveAgent, uiContent, npcName, fallbackDurationSeconds);

private void EnqueuePendingAudioDuration(int agentIndex, float durationSeconds) => _j17SceneSpeechOutputQueueController.EnqueuePendingAudioDuration(agentIndex, durationSeconds);

private bool TryDequeuePendingNpcBubble(int agentIndex, out PendingNpcBubbleEntry bubble, out float typingDurationSeconds) => _j17SceneSpeechOutputQueueController.TryDequeuePendingNpcBubble(agentIndex, out bubble, out typingDurationSeconds);

private bool TryDispatchPendingNpcBubbleForTts(int agentIndex, bool allowFallbackDuration) => _j17SceneSpeechOutputQueueController.TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration);

private void SchedulePendingNpcBubbleFallbackDispatch(TtsEngine.PlaybackRequest request, int delayMs = 180, int remainingRetries = 3) => _j17SceneSpeechOutputQueueController.SchedulePendingNpcBubbleFallbackDispatch(request, delayMs, remainingRetries);

private void ClearOrphanPendingAudioDuration(int agentIndex) => _j17SceneSpeechOutputQueueController.ClearOrphanPendingAudioDuration(agentIndex);

private void ClearPendingTtsBubbleSyncForAgent(int agentIndex, bool clearInteractionToken = false) => _j17SceneSpeechOutputQueueController.ClearPendingTtsBubbleSyncForAgent(agentIndex, clearInteractionToken);

private void ClearPendingSceneDialogueFeedForAgent(int agentIndex) => _j17SceneSpeechOutputQueueController.ClearPendingSceneDialogueFeedForAgent(agentIndex);

private void EnqueuePendingSceneDialogueFeed(int agentIndex, string speakerLabel, string content, Color color, bool waitForPlaybackFinished, float executeAtMissionTime = -1f) => _j17SceneSpeechOutputQueueController.EnqueuePendingSceneDialogueFeed(agentIndex, speakerLabel, content, color, waitForPlaybackFinished, executeAtMissionTime);

private bool TryDequeuePendingSceneDialogueFeed(int agentIndex, out PendingSceneDialogueFeedEntry entry) => _j17SceneSpeechOutputQueueController.TryDequeuePendingSceneDialogueFeed(agentIndex, out entry);

private void FlushPendingSceneDialogueFeedAfterSpeech(int agentIndex) => _j17SceneSpeechOutputQueueController.FlushPendingSceneDialogueFeedAfterSpeech(agentIndex);

private void ConvertPendingSceneDialogueFeedToTimedFlush(int agentIndex, float delaySeconds) => _j17SceneSpeechOutputQueueController.ConvertPendingSceneDialogueFeedToTimedFlush(agentIndex, delaySeconds);

private void EnqueuePendingSpeechCompletionToken(int agentIndex, long interactionToken) => _j17SceneSpeechOutputQueueController.EnqueuePendingSpeechCompletionToken(agentIndex, interactionToken);

private bool TryDequeuePendingSpeechCompletionToken(int agentIndex, out long interactionToken) => _j17SceneSpeechOutputQueueController.TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);

private void ClearPendingTtsBubbleSyncQueues() => _j17SceneSpeechOutputQueueController.ClearPendingTtsBubbleSyncQueues();


private void RecordSceneDialogueToMessageFeed(string speakerLabel, string content, Color color) => _j17SceneSpeechOutputQueueController.RecordSceneDialogueToMessageFeed(speakerLabel, content, color);

private void RecordPlayerSpeechToMessageFeed(string content) => _j17SceneSpeechOutputQueueController.RecordPlayerSpeechToMessageFeed(content);

private void RecordNpcSpeechToMessageFeed(string npcDisplayName, string content) => _j17SceneSpeechOutputQueueController.RecordNpcSpeechToMessageFeed(npcDisplayName, content);

private void QueueSceneInfoMessage(string message, Color color, int requiredConversationEpoch = 0, string soundEventPath = "") => _j17SceneSpeechOutputQueueController.QueueSceneInfoMessage(message, color, requiredConversationEpoch, soundEventPath);

private void ScheduleNpcSpeechToMessageFeed(int agentIndex, string npcDisplayName, string content, SceneSpeechPlaybackInfo playbackInfo) => _j17SceneSpeechOutputQueueController.ScheduleNpcSpeechToMessageFeed(agentIndex, npcDisplayName, content, playbackInfo);

	// BattleSpeech publishes its body immediately in the AF message feed. The
	// normal scene-shout path waits for TTS completion, which is appropriate for
	// ordinary replies but makes a long battle speech appear to be missing from
	// the lower-left feed while its world bubble is already visible.
internal void PublishBattleSpeechMessageFeed(NpcDataPacket npc, Agent liveAgent, string content) => _j17SceneSpeechOutputQueueController.PublishBattleSpeechMessageFeed(npc, liveAgent, content);

	private SceneSpeechPlaybackInfo ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true, bool suppressInteractionTimeoutArm = false)
		=> Presentation.ShowNpcSpeechOutput(SceneSpeechOutput, npc, liveAgent, content, allowTts, attachTtsToSceneAgent, suppressInteractionTimeoutArm);
	private void TrySpeakNativeConversationReplyWithTts(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex, string visibleText) => _j17NativeConversationSpeechAdapter.TrySpeakNativeConversationReplyWithTts(targetHero, targetCharacter, npc, targetAgentIndex, visibleText);

public static bool CanAgentParticipateInSceneSpeechExternal(int agentIndex) => SceneShoutInputController.CanAgentParticipateInSceneSpeechExternal(agentIndex);

	private static bool IsEscortedPrisonerAgentIdentityMatch(Agent liveAgent, int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter)
	{
        return ConversationActionContextBannerlordAdapter.IsEscortedPrisonerAgentIdentityMatch(liveAgent,targetAgentIndex,expectedHero,expectedCharacter);
    }

	// This runs only at asynchronous-response handoff points on the Bannerlord main thread.
	// A queued LLM result must never act on an Agent that was knocked out, killed, or removed
	// while the request was in flight.
	private static bool IsSceneResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
        return ConversationActionContextBannerlordAdapter.IsSceneResponseTargetAvailableForActionDispatch(targetAgentIndex,expectedHero,expectedCharacter,out unavailableReason);
    }

	internal static bool IsNativeConversationResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
        return ConversationActionContextBannerlordAdapter.IsNativeConversationResponseTargetAvailableForActionDispatch(targetAgentIndex,expectedHero,expectedCharacter,out unavailableReason);
    }

	internal static string StripScenePersonaBlocks(string text)
	{
        return ScenePromptMessageProjectionComposer.StripScenePersonaBlocks(text);
    }

	internal static string ExtractTrustPromptBlock(string text, out string remaining)
	{
        return ScenePromptMessageProjectionComposer.ExtractTrustPromptBlock(text, out remaining);
    }

	private static string InjectTrustBlockBeforeGroupRules(string localExtras, string trustBlock)
	{
        return ScenePromptMessageProjectionComposer.InjectTrustBlockBeforeGroupRules(localExtras, trustBlock);
    }

	internal static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
	}

	internal static string GetSceneNpcIdentityNameForPrompt(NpcDataPacket npc)
	{
		return ConversationActionPostprocessOwner.GetSceneNpcIdentityNameForPrompt(npc);
	}

	internal static string GetSceneNpcGivenNameForPrompt(NpcDataPacket npc)
	{
		return ConversationActionPostprocessOwner.GetSceneNpcGivenNameForPrompt(npc);
	}

	internal static string GetSceneNpcHistoryNameForPrompt(NpcDataPacket npc)
	{
		return SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcHistoryNameForPrompt(npc);
	}

	internal static string GetSceneNpcPatienceNameForPrompt(NpcDataPacket npc)
{
    return SceneAgentIdentityPromptCaptureAdapter.GetSceneNpcPatienceNameForPrompt(npc);
}

	private static string BuildNpcInventorySummaryHeader(string npcName, bool isHeroNpcLord = false) => PersonaIntroTextRules.BuildNpcInventorySummaryHeader(npcName, isHeroNpcLord);

	private static string BuildNpcInventorySummaryHeader(Hero hero)
	{
		return BuildNpcInventorySummaryHeader(hero?.Name?.ToString(), hero != null && hero != Hero.MainHero && hero.IsLord);
	}

	private static bool IsNpcInventorySummaryHeader(string text)
	{
		return ConversationSpeechTextRules.IsNpcInventorySummaryHeader(text);
	}

	private static string BuildNpcCurrentMountLineForPrompt(NpcDataPacket npc)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildNpcCurrentMountLineForPrompt(npc);
	}

	private static string BuildPlayerCurrentMountLineForPrompt()
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCurrentMountLineForPrompt();
	}

	private static Agent ResolveSceneAgentForMountPrompt(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ResolveSceneAgentForMountPrompt(agentIndex);
	}

	private static bool TryGetLiveRiddenMountNameForPrompt(Agent riderAgent, out string mountName)
	{
		return SceneAgentIdentityPromptCaptureAdapter.TryGetLiveRiddenMountNameForPrompt(riderAgent, out mountName);
	}

	private static bool TryGetMountEquipmentNameForPrompt(Agent agent, out string mountName)
	{
		return SceneAgentIdentityPromptCaptureAdapter.TryGetMountEquipmentNameForPrompt(agent, out mountName);
	}

	private static string NormalizeInlinePromptText(string value)
	{
		return SceneAgentIdentityPromptCaptureAdapter.NormalizeInlinePromptText(value);
	}

	private static string GetSceneNpcListIdentityForPrompt(NpcDataPacket npc)
	{
		return ConversationActionPostprocessOwner.GetSceneNpcListIdentityForPrompt(npc);
	}

	private static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildSceneNpcListLineForPrompt(npc);
	}

	private static string BuildPlayerRelationIdentitySuffixForNpcListLine(Hero npcHero, string playerDisplayName)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerRelationIdentitySuffixForNpcListLine(npcHero, playerDisplayName);
	}

	private string BuildPlayerDisplayNameForSceneNpcListIdentity(NpcDataPacket currentNpc, Dictionary<int, Hero> resolvedHeroes)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerDisplayNameForSceneNpcListIdentity(currentNpc, resolvedHeroes);
	}

	private static string BuildDistanceToCurrentNpcForNpcListLine(NpcDataPacket npc, NpcDataPacket currentNpc)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildDistanceToCurrentNpcForNpcListLine(npc, currentNpc);
	}

	private static string NormalizeSceneNpcMatchValue(string value)
    { return SceneAgentIdentityPromptCaptureAdapter.NormalizeSceneNpcMatchValue(value); }

	private static bool IsSameSceneNpcForPrompt(NpcDataPacket candidate, NpcDataPacket selfNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.IsSameSceneNpcForPrompt(candidate, selfNpc); }

	private static List<NpcDataPacket> FilterScenePresentNpcsForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc)
	{
		return SceneAgentIdentityPromptCaptureAdapter.FilterScenePresentNpcsForPrompt(presentNpcs, selfNpc);
	}

	private string BuildSceneNpcListLineWithRuntimeFactsForPrompt(NpcDataPacket npc, NpcDataPacket currentNpc, string playerDisplayName, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildSceneNpcListLineWithRuntimeFactsForPrompt(npc, currentNpc, playerDisplayName, resolvedHeroes, useAllegianceLabel);
	}

	private string BuildScenePresentNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildScenePresentNpcListBlockForPrompt(presentNpcs, selfNpc, resolvedHeroes, useAllegianceLabel);
	}

	private static string SanitizeSceneRelayPostprocessField(string value)
	{
		return ConversationActionPostprocessOwner.SanitizeSceneRelayPostprocessField(value);
	}

	private static string BuildSceneRelayNpcListLineForPostprocess(NpcDataPacket npc, bool isPrimaryTarget = false, bool isCurrentSpeaker = false)
	{
		return ConversationActionPostprocessOwner.BuildSceneRelayNpcListLineForPostprocess(npc, isPrimaryTarget, isCurrentSpeaker);
	}

	private static string BuildSceneRelayTargetListForPostprocess(IEnumerable<NpcDataPacket> presentNpcs, int currentSpeakerAgentIndex, int primaryTargetAgentIndex = -1)
	{
		return ConversationActionPostprocessOwner.BuildSceneRelayTargetListForPostprocess(presentNpcs, currentSpeakerAgentIndex, primaryTargetAgentIndex);
	}

	private static string BuildAutoGroupRelayInfoDisplayName(NpcDataPacket npc)
	{
        return ScenePromptMessageProjectionComposer.BuildAutoGroupRelayInfoDisplayName(npc);
    }

	internal static string BuildAutoGroupRelayThinkingInfoMessage(NpcDataPacket npc)
	{
		return BuildAutoGroupRelayInfoDisplayName(npc) + "正在思考该说什么。";
	}

	private static string ResolveSceneHistorySpeakerNameForPrompt(int speakerAgentIndex, string fallbackSpeakerName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
        return SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, fallbackSpeakerName, nearbyData);
    }

	private static string ResolveSceneTargetNameForPrompt(int targetAgentIndex, string fallbackTargetName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
        return SceneAgentIdentityPromptCaptureAdapter.ResolveSceneTargetNameForPrompt(targetAgentIndex, fallbackTargetName, nearbyData);
    }

	private static string BuildSceneNonHeroNamingNoteForPrompt(IEnumerable<NpcDataPacket> npcs)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildSceneNonHeroNamingNoteForPrompt(npcs);
	}



	internal static string GetSceneLocationDisplayName(Location location)
	{
        return SceneLocationPromptCaptureAdapter.GetSceneLocationDisplayName(location);
    }






	internal static bool IsSceneLocation(Location location, string locationId)
	{
		return string.Equals((location?.StringId ?? "").Trim(), (locationId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}







	private static void AppendSceneUnifiedTargetPromptSection(StringBuilder prompt, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
    {
        SceneMechanismPromptCaptureAdapter.AppendSceneUnifiedTargetPromptSection(prompt, summonTargets, guideTargets);
    }

	internal static string BuildSceneMechanismPromptSection(List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string sceneSummonClosureInstruction = null, string sceneFollowControlInstruction = null, NpcDataPacket publicActionTarget = null)
    {
        return SceneMechanismPromptCaptureAdapter.BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction, publicActionTarget);
    }

	internal static bool CanUseSceneMechanismPostprocessForSpeaker(int speakerAgentIndex)
	{
		return ConversationActionPostprocessOwner.CanUseSceneMechanismPostprocessForSpeaker(speakerAgentIndex);
	}

	private static string InjectSceneMechanismPromptSection(string prompt, string mechanismSection, bool allowAppendWithoutMarker = false)
{
    return ScenePromptMessageProjectionComposer.InjectSceneMechanismPromptSection(prompt, mechanismSection, allowAppendWithoutMarker);
}














	internal static bool HasPartyTransferRuleContext(string extras)
	{
		return ScenePromptMessageProjectionComposer.HasPartyTransferRuleContext(extras);
	}

	private static string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null) => CreateScenePersonaEquipmentPromptCaptureAdapter().BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);

	private static string BuildHeroPregnancySelfKnowledgeForPrompt(Hero hero)
	{ return SceneAgentIdentityPromptCaptureAdapter.BuildHeroPregnancySelfKnowledgeForPrompt(hero); }

	private static bool IsHeroInPlayerMainPartyForPrompt(Hero hero)
	{ return SceneAgentIdentityPromptCaptureAdapter.IsHeroInPlayerMainPartyForPrompt(hero); }

	private static string BuildPlayerCommandRelationshipLineForPrompt(NpcDataPacket npc, Hero hero)
	{ return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCommandRelationshipLineForPrompt(npc, hero); }

	private static bool ShouldIncludePlayerPartyRosterForScenePrompt(Hero observerHero, bool partyTransferTopicSelected)
	{ return SceneAgentIdentityPromptCaptureAdapter.ShouldIncludePlayerPartyRosterForScenePrompt(observerHero, partyTransferTopicSelected); }

	private static bool ShouldUseCompactPlayerPartyRosterForScenePrompt(bool partyTransferTopicSelected)
	{ return SceneAgentIdentityPromptCaptureAdapter.ShouldUseCompactPlayerPartyRosterForScenePrompt(partyTransferTopicSelected); }

	private static bool IsSettlementSceneForPlayerPartyRosterSuppression()
	{ return SceneAgentIdentityPromptCaptureAdapter.IsSettlementSceneForPlayerPartyRosterSuppression(); }

	private static string BuildPlayerTownPartyStayHintForPrompt(Hero playerHero)
	{ return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerTownPartyStayHintForPrompt(playerHero); }

	private static string BuildPrisonerContextLineForPrompt(NpcDataPacket npc, Hero hero)
	{ return SceneAgentIdentityPromptCaptureAdapter.BuildPrisonerContextLineForPrompt(npc, hero); }

	private static bool IsInspectionPrisonerNpcForPrompt(NpcDataPacket npc)
	{ return SceneAgentIdentityPromptCaptureAdapter.IsInspectionPrisonerNpcForPrompt(npc); }

	private static bool TryResolvePlayerCommandRelationshipForPrompt(NpcDataPacket npc, Hero hero, out string relationship)
	{ return SceneAgentIdentityPromptCaptureAdapter.TryResolvePlayerCommandRelationshipForPrompt(npc, hero, out relationship); }

	private static bool TryResolveMilitaryExerciseCommandRelationshipForPrompt(PartyBase party, out string relationship)
	{ return SceneAgentIdentityPromptCaptureAdapter.TryResolveMilitaryExerciseCommandRelationshipForPrompt(party, out relationship); }

	private static bool IsInspectionPrisonerAgentForPrompt(Agent agent)
	{ return SceneAgentIdentityPromptCaptureAdapter.IsInspectionPrisonerAgentForPrompt(agent); }

	internal static bool ShouldSuppressHeroJoinPartyPostprocessForScene(Hero hero)
	{
		return IsHeroInPlayerMainPartyForPrompt(hero);
	}

	private static string JoinPromptSections(params string[] sections)
	{ return SceneAgentIdentityPromptCaptureAdapter.JoinPromptSections(sections); }

	private static string BuildPlayerCustomPromptRuleBlock()
	{ return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCustomPromptRuleBlock(); }

	internal static string AppendPlayerCustomPromptRuleToSystemPrompt(string systemPrompt)
	{
		return JoinPromptSections(BuildPlayerCustomPromptRuleBlock(), systemPrompt);
	}

	private static bool ShouldHideSceneReputationForPrompt(NpcDataPacket npc, Hero hero) => PersonaEquipmentPromptCaptureAdapter.CaptureSceneReputationVisibility(npc, hero);

	private static bool IsWeaponEquipmentIndexForScenePrompt(EquipmentIndex index) => EquipmentPromptCaptureAdapter.IsWeaponEquipmentIndexForPrompt(index);

	private static void AddSceneEquipmentSummaryItem(Dictionary<string, int> counts, Dictionary<string, string> names, EquipmentIndex index, ItemObject item) => EquipmentPromptCaptureAdapter.AddEquipmentSummaryItemForPrompt(counts, names, index, item);

	private static List<string> BuildSceneEquipmentSummaryItemLines(Dictionary<string, int> counts, Dictionary<string, string> names, int maxEntries) => PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(counts, names, maxEntries);

	private static string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8) => EquipmentPromptCaptureAdapter.BuildNonHeroEquipmentSummaryForPrompt(npc, maxEntries);

	private SceneAgentIdentityPromptCaptureAdapter.ActionReadPorts _sceneActionReadPorts;
    private static SceneAgentIdentityPromptCaptureAdapter.ActionReadPorts CaptureSceneActionReadPorts()
    {
        ShoutBehavior owner = CurrentInstance;
        return owner == null ? null : (owner._sceneActionReadPorts ??= new SceneAgentIdentityPromptCaptureAdapter.ActionReadPorts(owner._sceneMovement, owner.SceneAudio, owner._j17SceneInteractionLifecycleController, owner._j17SceneAttentionController));
    }
    private static string BuildSceneAgentSelfActionFactForPrompt(NpcDataPacket npc, Hero hero = null)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildSceneAgentSelfActionFactForPrompt(CaptureSceneActionReadPorts, npc, hero);
	}

	private static List<string> CollectSceneAgentSelfActionLabels(Agent agent)
	{
		return SceneAgentIdentityPromptCaptureAdapter.CollectSceneAgentSelfActionLabels(CaptureSceneActionReadPorts, agent);
	}

	private static bool IsSceneAgentPromptActive(Agent agent)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentPromptActive(agent);
	}

	private static bool IsSceneAgentSittingForPrompt(Agent agent)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentSittingForPrompt(agent);
	}

	private static bool IsSceneAgentConversingForPrompt(Agent agent, string actionSearchText)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentConversingForPrompt(CaptureSceneActionReadPorts, agent, actionSearchText);
	}

	private static bool IsNativeConversationTargetForActionPrompt(NpcDataPacket npc, Hero hero)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsNativeConversationTargetForActionPrompt(npc, hero);
	}

	private static bool IsSceneAgentUsingObjectForPrompt(Agent agent)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentUsingObjectForPrompt(agent);
	}

	private static bool IsSceneAgentPlayingMusicForPrompt(Agent agent, string actionSearchText)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentPlayingMusicForPrompt(agent, actionSearchText);
	}

	private static bool IsSceneAgentMovingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentMovingForPrompt(agent, actionSearchText, lowerBodyAction);
	}

	private static bool IsSceneAgentWaitingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentWaitingForPrompt(agent, actionSearchText, lowerBodyAction, upperBodyAction);
	}

	private static bool IsSceneAgentDownOrHitForPrompt(Agent agent, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentDownOrHitForPrompt(agent, lowerBodyAction, upperBodyAction);
	}

	private static bool IsSceneAgentAttackAction(Agent.ActionCodeType actionType)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentAttackAction(actionType);
	}

	private static bool IsSceneAgentDefenseAction(Agent.ActionCodeType actionType)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentDefenseAction(actionType);
	}

	private static bool IsSceneAgentStrikeAction(Agent.ActionCodeType actionType)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsSceneAgentStrikeAction(actionType);
	}

	private static Agent.ActionCodeType GetSceneAgentCurrentActionType(Agent agent, int channelNo)
	{
		return SceneAgentIdentityPromptCaptureAdapter.GetSceneAgentCurrentActionType(agent, channelNo);
	}

	private static string BuildSceneAgentActionSearchText(Agent agent)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildSceneAgentActionSearchText(agent);
	}

	private static void AppendSceneAgentCurrentAnimationName(StringBuilder stringBuilder, Agent agent, int channelNo)
	{
		SceneAgentIdentityPromptCaptureAdapter.AppendSceneAgentCurrentAnimationName(stringBuilder, agent, channelNo);
	}

	private static void AppendSceneAgentUsableObjectSearchText(StringBuilder stringBuilder, Agent agent)
	{
		SceneAgentIdentityPromptCaptureAdapter.AppendSceneAgentUsableObjectSearchText(stringBuilder, agent);
	}

	private static void AppendSceneAgentNavigatorSearchText(StringBuilder stringBuilder, Agent agent)
	{
		SceneAgentIdentityPromptCaptureAdapter.AppendSceneAgentNavigatorSearchText(stringBuilder, agent);
	}

	private static void AppendSceneAgentActionSearchPart(StringBuilder stringBuilder, string value)
	{
		SceneAgentIdentityPromptCaptureAdapter.AppendSceneAgentActionSearchPart(stringBuilder, value);
	}

	private static bool ContainsSceneActionKeyword(string text, params string[] keywords)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ContainsSceneActionKeyword(text, keywords);
	}

	private static bool ContainsSceneEatingOrDrinkingKeyword(string text)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ContainsSceneEatingOrDrinkingKeyword(text);
	}

	private static bool ContainsSceneActionTokenKeyword(string text, params string[] keywords)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ContainsSceneActionTokenKeyword(text, keywords);
	}

	private static bool ContainsSceneActionTokenKeyword(string text, string keyword)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ContainsSceneActionTokenKeyword(text, keyword);
	}

	private static void AddSceneAgentActionLabel(List<string> labels, HashSet<string> seen, string label)
	{
		SceneAgentIdentityPromptCaptureAdapter.AddSceneAgentActionLabel(labels, seen, label);
	}

private static string BuildCeremonyRoleFactForPrompt(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildCeremonyRoleFactForPrompt(agentIndex);
	}

	internal static string BuildSceneSystemTopPromptIntroForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildSceneSystemTopPromptIntroForSingle(SceneRoleIntroCapture, npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
}

public static string BuildHeroStableRoleContextForExternal(Hero hero)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildHeroStableRoleContextForExternal(SceneRoleIntroCapture, hero);
}

private static string BuildSceneSystemTopPromptIntroForGroup(IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool partyTransferTopicSelected = false)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildSceneSystemTopPromptIntroForGroup(SceneRoleIntroCapture, npcs, resolvedHeroes, partyTransferTopicSelected);
}

internal static string BuildSceneUserRuntimeContextForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildSceneUserRuntimeContextForSingle(SceneRoleIntroCapture, npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
}

internal static string BuildCompactSceneUserRuntimeContextForShortReply(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildCompactSceneUserRuntimeContextForShortReply(SceneRoleIntroCapture, npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
}

private static string BuildSceneUserRuntimeContextForGroup(IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildSceneUserRuntimeContextForGroup(SceneRoleIntroCapture, npcs, resolvedHeroes, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
}

private static void SplitSceneNpcRoleIntroSections(string fullIntro, bool isHeroNpc, out string stableIntro, out string runtimeIntro)
{
    SceneAgentIdentityPromptCaptureAdapter.SplitSceneNpcRoleIntroSections(fullIntro, isHeroNpc, out stableIntro, out runtimeIntro);
}

	// This sentence is appended to the scene-facing player intro. The no-faction branch is intentional:
	// without it, NPCs tend to over-associate the player with their culture's kingdom.
	private static string BuildPlayerSceneIdentitySentenceForPrompt(Hero playerHero)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerSceneIdentitySentenceForPrompt(playerHero);
}

	private static bool ShouldForceDetailedPlayerIntroForObserver(Hero observerHero)
{
    return SceneAgentIdentityPromptCaptureAdapter.ShouldForceDetailedPlayerIntroForObserver(observerHero);
}

	private static bool DoesSceneObserverKnowPlayerIdentityForPrompt(Hero observerHero, NpcDataPacket observerNpc)
{
    return SceneAgentIdentityPromptCaptureAdapter.DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero, observerNpc);
}

	private static string BuildSceneObserverInlineStateForPrompt(Hero observerHero, NpcDataPacket observerNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.BuildSceneObserverInlineStateForPrompt(observerHero, observerNpc); }

	private static string BuildScenePlayerIntroForPrompt(bool includeTradePricing = false, bool includePlayerPartyRoster = false) => BuildScenePlayerIntroForPrompt(null, null, includeTradePricing, includePlayerPartyRoster);

	private static string BuildScenePlayerIntroForPrompt(Hero observerHero, bool includeTradePricing = false, bool includePlayerPartyRoster = false) => BuildScenePlayerIntroForPrompt(observerHero, null, includeTradePricing, includePlayerPartyRoster);

	private static string BuildScenePlayerIntroForPrompt(Hero observerHero, NpcDataPacket observerNpc, bool includeTradePricing = false, bool includePlayerPartyRoster = false, bool useCompactPlayerPartyRoster = false) => CreateScenePersonaEquipmentPromptCaptureAdapter().BuildScenePlayerIntroForPrompt(observerHero, observerNpc, includeTradePricing, includePlayerPartyRoster, useCompactPlayerPartyRoster);

	private static IFaction ResolveNpcPerspectiveFactionForPlayerCrimePrompt(Hero observerHero, NpcDataPacket observerNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc); }

	private static string BuildPlayerFactionWarLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerFactionWarLineForPrompt(observerHero, observerNpc); }

	private static string BuildPlayerVassalageRelationLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerVassalageRelationLineForPrompt(observerHero, observerNpc); }

	private static Kingdom ResolveNpcPerspectiveKingdomForPrompt(Hero observerHero, NpcDataPacket observerNpc)
    { return SceneAgentIdentityPromptCaptureAdapter.ResolveNpcPerspectiveKingdomForPrompt(observerHero, observerNpc); }

	private static string BuildFactionNameForPrompt(IFaction faction)
    { return SceneAgentIdentityPromptCaptureAdapter.BuildFactionNameForPrompt(faction); }

	private static string BuildPlayerCompanionPartyRoleLabelForPrompt(Hero companionHero)
    { return SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCompanionPartyRoleLabelForPrompt(companionHero); }

	private static PartyBase ResolveLedPartyBaseForPrompt(Hero hero)
	{
		return SceneRosterPromptCaptureAdapter.ResolveLedPartyBaseForPrompt(hero);
	}

	private static void AggregateRosterForPrompt(TroopRoster roster, bool excludeHeroes,
		out int total, out int infantry, out int cavalry, out int archer, out int horseArcher,
		out List<KeyValuePair<string, int>> tieredTopN, int topN = 10)
	{
		SceneRosterPromptCaptureAdapter.AggregateRosterForPrompt(roster, excludeHeroes, out total, out infantry, out cavalry, out archer, out horseArcher, out tieredTopN, topN);
	}

	private static List<string> ExtractHeroPrisonerNamesForPrompt(TroopRoster roster)
	{
		return SceneRosterPromptCaptureAdapter.ExtractHeroPrisonerNamesForPrompt(roster);
	}

	private static List<string> ExtractHeroMemberNamesForPrompt(TroopRoster roster, Hero leaderHero, string leaderDisplayNameOverride, int maxCount, out int totalHeroCount)
	{
		return SceneRosterPromptCaptureAdapter.ExtractHeroMemberNamesForPrompt(roster, leaderHero, leaderDisplayNameOverride, maxCount, out totalHeroCount);
	}

	private static string BuildPartyTroopsLineForPrompt(PartyBase partyBase, string leadingText, string noTroopsText, bool includeDetails = true, string leaderDisplayNameOverride = null)
	{
		return SceneRosterPromptCaptureAdapter.BuildPartyTroopsLineForPrompt(partyBase, leadingText, noTroopsText, includeDetails, leaderDisplayNameOverride);
	}

	private static string BuildPartyShipPromptSuffixForPrompt(PartyBase partyBase)
	{
		return SceneRosterPromptCaptureAdapter.BuildPartyShipPromptSuffixForPrompt(partyBase);
	}

	private static string BuildHeroPartyTroopsLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true, string leaderDisplayNameOverride = null)
	{
		return SceneRosterPromptCaptureAdapter.BuildHeroPartyTroopsLineForPrompt(hero, secondPerson, includeDetails, leaderDisplayNameOverride);
	}

	private static string BuildPartyPrisonersLineForPrompt(PartyBase partyBase, string subject, string noPrisonersText, bool includeDetails = true)
	{
		return SceneRosterPromptCaptureAdapter.BuildPartyPrisonersLineForPrompt(partyBase, subject, noPrisonersText, includeDetails);
	}

	private static string BuildHeroPartyPrisonersLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true)
	{
		return SceneRosterPromptCaptureAdapter.BuildHeroPartyPrisonersLineForPrompt(hero, secondPerson, includeDetails);
	}

	public static string BuildPlayerSceneIntroForExternal(Hero observerHero = null, bool includeTradePricing = false)
	{
		try
		{
			return BuildScenePlayerIntroForPrompt(observerHero, includeTradePricing);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildNearbyPresentNpcLineForPrompt(NpcDataPacket selfNpc, IEnumerable<NpcDataPacket> presentNpcs)
    { return ScenePromptMessageProjectionComposer.BuildNearbyPresentNpcLineForPrompt(selfNpc, presentNpcs); }

	private static string ConvertCountToChineseForPrompt(int count)
	{
		return SceneLocationPromptCaptureAdapter.ConvertCountToChineseForPrompt(count);
	}

	private static string NormalizeFactionRelationLabelForPrompt(IFaction faction, IFaction referenceFaction)
	{
		return SceneLocationPromptCaptureAdapter.NormalizeFactionRelationLabelForPrompt(faction, referenceFaction);
	}

	private static string ConvertNpcSideRelationLabelForPrompt(string relation)
	{
		return SceneLocationPromptCaptureAdapter.ConvertNpcSideRelationLabelForPrompt(relation);
	}

	private static void ParseCurrentScenePlaceAndSpotForPrompt(out string placeName, out string spotName)
	{
		SceneLocationPromptCaptureAdapter.ParseCurrentScenePlaceAndSpotForPrompt(out placeName, out spotName);
	}

	private static bool IsUnknownSceneDescriptionForPrompt(string sceneDescription)
	{
		return SceneLocationPromptCaptureAdapter.IsUnknownSceneDescriptionForPrompt(sceneDescription);
	}

	private static string NormalizeSettlementNameForPromptLookup(string name)
	{
		return SceneLocationPromptCaptureAdapter.NormalizeSettlementNameForPromptLookup(name);
	}

	private static Settlement FindNearestSettlementForPrompt(MobileParty referenceParty = null)
	{
		return SceneLocationPromptCaptureAdapter.FindNearestSettlementForPrompt(referenceParty);
	}

	private static MobileParty ResolveMapLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		return SceneLocationPromptCaptureAdapter.ResolveMapLocationReferencePartyForPrompt(perspectiveHero);
	}

	private static MobileParty ResolveSeaLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		return SceneLocationPromptCaptureAdapter.ResolveSeaLocationReferencePartyForPrompt(perspectiveHero);
	}

	private static Settlement ResolveSceneSettlementForPrompt(string placeName, Hero perspectiveHero = null)
	{
		return SceneLocationPromptCaptureAdapter.ResolveSceneSettlementForPrompt(placeName, perspectiveHero);
	}

	private static string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero)
	{
		return SceneLocationPromptCaptureAdapter.BuildSceneLocationAndSettlementLineForPrompt(perspectiveHero);
	}

	private static bool IsWildernessSpotForPrompt(string spotName)
	{
		return SceneLocationPromptCaptureAdapter.IsWildernessSpotForPrompt(spotName);
	}

	private static bool IsSeaSpotForPrompt(string spotName)
	{
		return SceneLocationPromptCaptureAdapter.IsSeaSpotForPrompt(spotName);
	}

	private static bool IsCurrentSettlementContextForPrompt()
	{
		return SceneLocationPromptCaptureAdapter.IsCurrentSettlementContextForPrompt();
	}

	private static string BuildSettlementFlavorLineForPrompt(Hero perspectiveHero)
	{
		return SceneLocationPromptCaptureAdapter.BuildSettlementFlavorLineForPrompt(perspectiveHero);
	}

	private static string BuildSettlementRulerPresenceLineForPrompt()
	{
		return SceneLocationPromptCaptureAdapter.BuildSettlementRulerPresenceLineForPrompt();
	}

	internal static bool ContainsLiteralKeywordHit(string input, List<string> keywords)
	{
        return ScenePromptMessageProjectionComposer.ContainsLiteralKeywordHit(input, keywords);
    }


	private static bool TryRenderSceneHistoryLine(ConversationMessage msg, HashSet<string> allowedSpeakers, out string rendered, int viewerAgentIndex = -1, string fallbackTargetNpcName = "", bool useNpcNameAddress = false, bool useSceneDistanceSpeechLabels = true) => HistorySectionProjectionOwner.TryRenderSceneHistoryLine(msg, allowedSpeakers, out rendered, viewerAgentIndex, fallbackTargetNpcName, useNpcNameAddress, useSceneDistanceSpeechLabels, GetPlayerDisplayNameForShout());

	private static bool IsLeakedPromptLineForShout(string line)
	{
		return ConversationSpeechTextRules.IsLeakedPromptLineForShout(line);
	}

	private static string StripLeakedPromptFragmentsForShout(string text)
	{
		return ConversationSpeechTextRules.StripLeakedPromptFragmentsForShout(text);
	}

	internal static string StripLeakedPromptContentForShout(string text)
	{
		return ConversationSpeechTextRules.StripLeakedPromptContentForShout(text);
	}

	internal static string StripStageDirectionsForPassiveShout(string text)
	{
		return ConversationSpeechTextRules.StripStageDirectionsForPassiveShout(text, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
	}

	internal static string StripActionTagsForSceneSpeech(string text)
	{
		return ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(text);
	}

	internal static string ExtractDeferredSceneActionTags(string text)
	{
		return ConversationActionPostprocessOwner.ExtractDeferredSceneActionTags(text);
	}

	internal static bool HasNonMoodDeferredSceneActionTag(string text)
	{
		return ConversationActionPostprocessOwner.HasNonMoodDeferredSceneActionTag(text);
	}

	internal static string StripDeferredSceneMoodTags(string text)
	{
		return ConversationActionPostprocessOwner.StripDeferredSceneMoodTags(text);
	}

	private static bool HasDeferredDirectGameActionTag(string text)
	{
		return ConversationActionPostprocessOwner.HasDeferredDirectGameActionTag(text);
	}

	private bool TryApplyDeferredScenePostprocessActionTagsDirectly(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		ref string tags,
		string playerText,
		string npcReplyText,
		string chainName,
		bool replyIsDirectPlayerResponse,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
        return ActionBoundary.TryApplyDeferredScenePostprocessActionTagsDirectly(targetHero,targetCharacter,targetAgentIndex,ref tags,playerText,npcReplyText,chainName,replyIsDirectPlayerResponse,duelDispatchContext);
    }

	internal static string StripNpcNamePrefixSafely(string text, int maxPrefixLength = 30)
	{
		return ShoutUtils.StripNamePrefixedLineSafely(text, maxPrefixLength);
	}

	internal static string SanitizeSceneSpeechText(string text)
	{
		return ConversationSpeechTextRules.SanitizeSceneSpeechText(text, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
	}

	private static string SanitizeSceneSpeechTextForTts(string text)
{
    return ConversationSpeechTextRules.SanitizeSceneSpeechTextForTts(text);
}

	private static string ExtractContentSectionForTts(string text)
{
    return ConversationSpeechTextRules.ExtractContentSectionForTts(text);
}

	private static string StripTtsNarrationLines(string text)
{
    return ConversationSpeechTextRules.StripTtsNarrationLines(text);
}

	internal static string PrepareSceneHistorySpeechText(string text)
	{
		return ConversationSpeechTextRules.PrepareSceneHistorySpeechText(text);
	}

	internal static bool IsDetailedSceneSpeechPromptEnabled()
	{
        return SceneHistoryPromptCaptureAdapter.IsDetailedSceneSpeechPromptEnabled();
    }

	internal static bool ShouldPreserveSceneAsteriskActions()
	{
        return SceneHistoryPromptCaptureAdapter.ShouldPreserveSceneAsteriskActions();
    }

	internal static bool ContainsAutoGroupEndSignal(string text)
	{
        return ScenePromptMessageProjectionComposer.ContainsAutoGroupEndSignal(text);
    }

	internal static bool TryParseAutoGroupRelayTargetAgentIndex(string text, out int relayTargetAgentIndex)
	{
        return ScenePromptMessageProjectionComposer.TryParseAutoGroupRelayTargetAgentIndex(text, out relayTargetAgentIndex);
    }

	private static string StripAutoGroupStopSignal(string text)
	{
		return ConversationSpeechTextRules.StripAutoGroupStopSignal(text);
	}

	internal static string StripAutoGroupRelaySignal(string text)
	{
		return ConversationSpeechTextRules.StripAutoGroupRelaySignal(text);
	}

	private static bool IsPrivateRecentWindowHeader(string line) => HistorySectionProjectionOwner.IsPrivateRecentWindowHeader(line);

	internal static string BuildScenePublicHistorySection(List<string> sceneHistoryLines)
	{
        return SceneHistoryPromptCaptureAdapter.BuildScenePublicHistorySection(sceneHistoryLines);
    }


private static bool IsSceneWeeklyFullReportHeader(string line)
{
    return ScenePromptMessageProjectionComposer.IsSceneWeeklyFullReportHeader(line);
}

private static string FormatSceneRuleSection(string text)
{
    return ScenePromptMessageProjectionComposer.FormatSceneRuleSection(text);
}

private static string FormatSceneKnowledgeSection(string text)
{
    return ScenePromptMessageProjectionComposer.FormatSceneKnowledgeSection(text);
}

internal static void SplitSceneExtraSections(string text, out string miscSection, out string ruleSection, out string knowledgeSection)
{
    ScenePromptMessageProjectionComposer.SplitSceneExtraSections(text, out miscSection, out ruleSection, out knowledgeSection);
}

internal static string BuildSceneSystemRuleBlock(string ruleSection, string sceneMechanismPromptSection)
{
    return ScenePromptMessageProjectionComposer.BuildSceneSystemRuleBlock(ruleSection, sceneMechanismPromptSection);
}

internal static bool HasGcczImmediatePromptExtras(string baseExtras)
{
    return SceneAgentIdentityPromptCaptureAdapter.HasGcczImmediatePromptExtras(baseExtras);
}

internal static string BuildGcczImmediateIdentityOverrideBlock(Hero contextHero, CharacterObject npcCharacter, int targetAgentIndex, string baseExtras)
{
    return SceneAgentIdentityPromptCaptureAdapter.BuildGcczImmediateIdentityOverrideBlock(contextHero, npcCharacter, targetAgentIndex, baseExtras);
}

private static string FormatSceneHistorySection(string sectionText) => HistorySectionProjectionOwner.FormatSceneHistorySection(sectionText);

private static string BuildSceneHistoryUserBlock(string scenePublicHistorySection, string privateRecentWindowSection, string persistedWithoutRecentWindow)
{
    return ScenePromptMessageProjectionComposer.BuildSceneHistoryUserBlock(scenePublicHistorySection, privateRecentWindowSection, persistedWithoutRecentWindow);
}

internal static string FilterHistorySectionAgainstScenePublicHistory(string historySection, string scenePublicHistorySection) => HistorySectionProjectionOwner.FilterHistorySectionAgainstScenePublicHistory(historySection, scenePublicHistorySection, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));

private static HashSet<string> BuildHistoryLineSemanticKeySet(string section) => HistorySectionProjectionOwner.BuildHistoryLineSemanticKeySet(section, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));

private static bool IsHistorySectionHeaderLine(string line) => HistorySectionProjectionOwner.IsHistorySectionHeaderLine(line);

private static bool IsHistorySectionDateHeaderLine(string line) => HistorySectionProjectionOwner.IsHistorySectionDateHeaderLine(line);

private static string BuildHistoryLineSemanticKey(string line) => HistorySectionProjectionOwner.BuildHistoryLineSemanticKey(line, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));

private static string StripHistoryLineSpeakerPrefixForDedupe(string text) => HistorySectionProjectionOwner.StripHistoryLineSpeakerPrefixForDedupe(text);

private static string PruneEmptyHistoryDateBlocks(List<string> lines) => HistorySectionProjectionOwner.PruneEmptyHistoryDateBlocks(lines);

internal static string BuildSceneCompositeUserBlock(string sceneHistoryUserBlock, params string[] extraSections)
{
		return MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(sceneHistoryUserBlock, extraSections);
	}

internal static string BuildSceneSingleNpcTaskSystemBlock(string npcName, bool hasMultiplePresentNpcs, int minTokens, int maxTokens, string playerNameForLength)
{
    bool disabled = DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled();
    if (disabled) return "";
    string playerName = PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal();
    var options = SceneAgentIdentityPromptCaptureAdapter.CaptureSceneReplyTaskSettings();
    return ScenePromptMessageProjectionComposer.BuildSceneSingleNpcTaskSystemBlock(npcName, hasMultiplePresentNpcs, minTokens, maxTokens, playerNameForLength, disabled, options.InnerThoughtDisabled, options.ThoughtMinTokens, playerName);
}

private static string BuildReplyLengthInstruction(int minTokens, int maxTokens)
{
    bool disabled = DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled();
    if (disabled) return "";
    var options = SceneAgentIdentityPromptCaptureAdapter.CaptureSceneReplyTaskSettings();
    return ScenePromptMessageProjectionComposer.BuildReplyLengthInstruction(minTokens, maxTokens, disabled, options.InnerThoughtDisabled, options.ThoughtMinTokens);
}

internal static string BuildSimpleDialogueReplyLengthInstruction(int minTokens, int maxTokens)
{
    return ScenePromptMessageProjectionComposer.BuildSimpleDialogueReplyLengthInstruction(minTokens, maxTokens);
}

private static int GetShoutThoughtMinTokens()
{
    return SceneAgentIdentityPromptCaptureAdapter.GetShoutThoughtMinTokens();
}

private static bool IsShoutInnerThoughtPromptDisabled()
{
    return SceneAgentIdentityPromptCaptureAdapter.IsShoutInnerThoughtPromptDisabled();
}

private static bool TryExtractReplyFormatInstruction(ref string prompt, out string instruction)
{
    return ScenePromptMessageProjectionComposer.TryExtractReplyFormatInstruction(ref prompt, out instruction);
}

internal static void GetSceneReplyLengthLimits(DuelSettings settings, out int minTokens, out int maxTokens)
{
    SceneAgentIdentityPromptCaptureAdapter.GetSceneReplyLengthLimits(settings, out minTokens, out maxTokens);
}

private static string NormalizeScenePlayerHistoryLine(string text, string targetNpcName = "", bool useNpcNameAddress = false, float playerDistanceMeters = -1f) => HistorySectionProjectionOwner.NormalizeScenePlayerHistoryLine(text, targetNpcName, useNpcNameAddress, playerDistanceMeters, GetPlayerDisplayNameForShout());

	internal static void SplitPersistedHeroHistorySections(string persistedHeroHistory, out string privateRecentWindowSection, out string persistedWithoutRecentWindow) => HistorySectionProjectionOwner.SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);

	internal static string TrimPrivateRecentWindowForActionPostprocess(string privateRecentWindowSection, int maxTurns = 5)
{
    return ScenePromptMessageProjectionComposer.TrimPrivateRecentWindowForActionPostprocess(privateRecentWindowSection, maxTurns);
}

	private static bool ContainsAfefFactMarkerForActionPostprocess(string line)
{
    return ScenePromptMessageProjectionComposer.ContainsAfefFactMarkerForActionPostprocess(line);
}

	private static bool IsActionPostprocessHistoryHeaderLine(string line)
{
    return ScenePromptMessageProjectionComposer.IsActionPostprocessHistoryHeaderLine(line);
}

	private static string StripAfefFactLinesForActionPostprocess(string historySection)
{
    return ScenePromptMessageProjectionComposer.StripAfefFactLinesForActionPostprocess(historySection);
}

	private static bool IsActionPostprocessPlayerTurnLine(string line)
{
    return ScenePromptMessageProjectionComposer.IsActionPostprocessPlayerTurnLine(line);
}

	private static string BuildSceneFirstMeetingNpcFactSection(Hero hero)
{
    return SceneHistoryPromptCaptureAdapter.BuildSceneFirstMeetingNpcFactSection(hero, CaptureFirstMeetingPromptText);
}

	private static string BuildSceneFirstMeetingNpcFactSection(int agentIndex, Dictionary<int, Hero> resolvedHeroes)
{
    return SceneHistoryPromptCaptureAdapter.BuildSceneFirstMeetingNpcFactSection(agentIndex, resolvedHeroes, CaptureFirstMeetingPromptText);
}

	private string BuildPatienceBadgeForNpc(NpcDataPacket npc, Agent liveAgent)
	{
    return SceneAgentIdentityPromptCaptureAdapter.BuildPatienceBadgeForNpc(npc, liveAgent);
}

	public static void TrySystemNpcShout(Agent speakerAgent, string content)
	{
		try
		{
			if (speakerAgent != null && !string.IsNullOrWhiteSpace(content))
			{
				(Campaign.Current?.GetCampaignBehavior<ShoutBehavior>())?.EnqueueSystemNpcShout(speakerAgent, content);
			}
		}
		catch
		{
		}
	}

	private void EnqueueSystemNpcShout(Agent speakerAgent, string content) => _ = _systemNpcShout.Enqueue(speakerAgent, content);

	public override void RegisterEvents()
	{
		Volatile.Write(ref _freezeWatchdogDiagnosticInstance, this);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnNativeConversationEnded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnNativeIllustrationSessionLaunched);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
	}

	private void OnCampaignTick(float dt)
	{
		try
		{
			if (Mission.Current == null)
			{
				DrainMainThreadActionsForMissionTick();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] Campaign tick main-thread action drain failed: " + ex.Message);
		}
	}

	public static void OnApplicationTickForNativeConversationTtsExternal()
	{
		try
		{
			ShoutBehavior instance;
			using (FreezeWatchdog.Scope("NativeConversationTts.ResolveInstance"))
			{
				instance = CurrentInstance;
			}
			bool hasMission;
			using (FreezeWatchdog.Scope("NativeConversationTts.CheckMission"))
			{
				hasMission = Mission.Current != null;
			}
			if (instance == null || hasMission)
			{
				return;
			}
			bool conversationInProgress;
			using (FreezeWatchdog.Scope("NativeConversationTts.CheckConversation"))
			{
				conversationInProgress = Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
			}
			if (!conversationInProgress)
			{
				return;
			}
			using (FreezeWatchdog.Scope("NativeConversationTts.DrainMainThreadActions"))
			{
				instance.DrainMainThreadActionsForMissionTick();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] Application tick native conversation TTS drain failed: " + ex.Message);
		}
	}

	public override void SyncData(IDataStore dataStore) => _sceneRevisitPersistence.Sync(dataStore);

private static void OpenNativeConversationInput(bool showMessage = true) => SceneShoutInputController.OpenNativeConversationInput(showMessage);

internal static void CloseNativeConversationInput(bool clearSessionHistory = false) => SceneShoutInputController.CloseNativeConversationInput(clearSessionHistory);

	private void OnNativeIllustrationSessionLaunched(CampaignGameStarter starter)
	{
		var manager = Campaign.Current?.ConversationManager;
		if (manager == null) return;
		manager.ConversationBegin -= CaptureNativeIllustrationHistoryBoundary;
		manager.ConversationBegin += CaptureNativeIllustrationHistoryBoundary;
	}

private static void CaptureNativeIllustrationHistoryBoundary()
	{
		CurrentInstance?._j17SceneNativeMechanismController.OnNativeConversationStarted();
		SceneConversationHistoryOwner.CaptureNativeIllustrationHistoryBoundary();
	}

	private void OnNativeConversationEnded(IEnumerable<CharacterObject> characters)
	{
		_shoutTradeNativeManager = null;
		_shoutTradeNativeMission = null;
		_shoutTradeNativeAgent = null;
		SceneConversationHistoryOwner.CaptureNativeIllustrationHistoryBoundary();
		CloseNativeConversationInput(clearSessionHistory: false);
		ExecutePendingNativeSceneMechanismActionsAfterConversationExit("native_conversation_ended");
		// EndConversation still owns its agent list and handler until this event returns.
	}

	private void OnMissionStarted(IMission mission)
	{
		Mission currentMission = Mission.Current;
		if (mission == null || currentMission == null)
		{
			return;
		}
		Logger.LogImmediate("TownAmbient", "shout_mission_started scene=" + (currentMission.SceneName ?? "") + " agents=" + (currentMission.Agents?.Count ?? 0));
		lock (_historyLock)
		{
			SceneHistoryOwner.Reset();
			_sceneHeroRevisitHandledThisSession.Clear();
			_sceneHeroFirstMeetingShownThisSession.Clear();
			_pendingHeroHistoryExtraFactAfterSceneReply = "";
			_pendingHeroHistoryExtraFactTargetsAfterSceneReply.Clear();
			_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1;
		}
		ClearPendingCurrentAfefFacts();
		ClearPendingNativeSceneMechanismActions("mission_started");
		ClearPendingNativeSceneTauntFight("mission_started");
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
		_currentStareTarget = null;
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_interactionGraceTimer = 0f;
		_passiveCooldowns.Clear();
		_activeInteractionSessions.Clear();
		_pendingInteractionTimeoutArms.Clear();
		_sceneMovement.ClearTransientFollowers();
		_sceneMovement.InvalidateSceneCommandFollowerCache();
		_sceneConversation.ResetImmediateReactions();
		RetireModuleSceneGroup("scene.stale_context");
		RetireQueuedSceneSpeech(_sceneSpeechQueueOwner.Reset());
		_sceneConversationEpoch = 0;
		_nextProactiveSceneOpeningProbeMissionTime = 0f;
		ResetPendingMainThreadFunctions();
		Interlocked.Increment(ref SceneConversationHistoryOwner.SessionId);
		PromptSemanticWarmupSeedBatch semanticWarmupSeeds = AIConfigHandler.CaptureGuardrailSemanticWarmupSeeds();
		RagWarmupCoordinator.TryStartBackgroundWarmup("mission_start", semanticWarmupSeeds);
		AIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start", semanticWarmupSeeds);
		currentMission.AddMissionBehavior(new ShoutMissionBehavior(this));
		currentMission.AddMissionBehavior(new FloatingTextMissionView());
		currentMission.AddMissionBehavior(new TownAmbientDialogueMissionBehavior());
		Logger.LogImmediate("TownAmbient", "mission_behaviors_attached scene=" + (currentMission.SceneName ?? ""));
		try
		{
			Settlement ambientSettlement = Settlement.CurrentSettlement;
			Location ambientLocation = CampaignMission.Current?.Location;
			if (DuelSettings.GetTownAmbientDialogueDensity() > 0
				&& ambientSettlement?.IsTown == true
				&& ambientLocation != null
				&& currentMission.GetMissionBehavior<InterventionNativeTownCivilianPopulationMissionBehavior>() == null)
			{
				currentMission.AddMissionBehavior(new InterventionNativeTownCivilianPopulationMissionBehavior(ambientSettlement.StringId, regularTownMultiplierMode: true));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TownAmbient", "regular town population behavior was not attached: " + ex.Message);
		}
		SubscribeTtsPlaybackEvents();
		try
		{
			if (!ShoutUtils.IsInValidScene())
			{
				return;
			}
			string text = "T";
			string text2 = "Y";
			try
			{
				DuelSettings settings = DuelSettings.GetSettings();
				if (settings != null && !string.IsNullOrWhiteSpace(settings.ShoutKey))
				{
					text = settings.ShoutKey.Trim().ToUpperInvariant();
				}
				if (settings != null && !string.IsNullOrWhiteSpace(settings.ShoutSpecialMenuKey))
				{
					text2 = settings.ShoutSpecialMenuKey.Trim().ToUpperInvariant();
				}
			}
			catch
			{
				text = "T";
				text2 = "Y";
			}
			AnimusForgeQuickInfo.Show("按住" + text + "键预览喊话范围，松开后说话；按住" + text2 + "键打开复杂交流");
		}
		catch
		{
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		try
		{
			lock (_historyLock)
			{
				SceneHistoryOwner.Reset();
				_pendingHeroHistoryExtraFactAfterSceneReply = "";
				_pendingHeroHistoryExtraFactTargetsAfterSceneReply.Clear();
				_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1;
			}
			ClearPendingCurrentAfefFacts();
			ClearPendingNativeSceneMechanismActions("mission_ended");
			ClearPendingNativeSceneTauntFight("mission_ended");
			_pendingLordsHallMissionEntryAfterSpeech = null;
			UnregisterPendingLordsHallMissionEntryConversationEndHook();
			DeactivateMultiSceneMovementSuppression();
			ClearPendingSceneConversationAttentionRelease();
			_staringAgents.Clear();
			_staringAgentAnchors.Clear();
			_staringUseConversationAgents.Clear();
			_currentStareTarget = null;
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_interactionGraceTimer = 0f;
			_passiveCooldowns.Clear();
			_activeInteractionSessions.Clear();
			_pendingInteractionTimeoutArms.Clear();
			_sceneMovement.ClearTransientFollowers();
			_sceneMovement.InvalidateSceneCommandFollowerCache();
			_sceneConversation.ResetImmediateReactions();
			RetireModuleSceneGroup("scene.stale_context");
			RetireQueuedSceneSpeech(_sceneSpeechQueueOwner.Reset());
			_sceneConversationEpoch = 0;
			_nextProactiveSceneOpeningProbeMissionTime = 0f;
			ResetPendingMainThreadFunctions();
			Interlocked.Increment(ref SceneConversationHistoryOwner.SessionId);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] OnMissionEnded cleanup failed: " + ex.Message);
		}
	}




































private bool IsMultiNpcSceneConversationActive() => _j17SceneInteractionLifecycleController.IsMultiNpcSceneConversationActive();

private void ResetPassiveStareTracking() => _j17ScenePassiveInteractionController.ResetPassiveStareTracking();

public void UpdatePassiveStareLogic(float dt) => _j17ScenePassiveInteractionController.UpdatePassiveStareLogic(dt);

private static float GetPassiveStareTriggerTime() => ScenePassiveInteractionController.GetPassiveStareTriggerTime();

private void UpdateCooldowns(float dt) => _j17ScenePassiveInteractionController.UpdateCooldowns(dt);

	internal static string TryGetKingdomIdOverrideFromAgent(Agent agent)
    {
        return SceneAgentIdentityPromptCaptureAdapter.TryGetKingdomIdOverrideFromAgent(agent);
    }

	private static void LogShoutLorePrequery(string phase, Agent agent, CharacterObject character, string kingdomIdOverride, string inputText, string secondaryInput = null)
	{
    SceneAgentIdentityPromptCaptureAdapter.LogShoutLorePrequery(phase, agent, character, kingdomIdOverride, inputText, secondaryInput);
}

private string GetCooldownIdentityKey(NpcDataPacket data, int fallbackAgentIndex = -1) => _j17ScenePassiveInteractionController.GetCooldownIdentityKey(data, fallbackAgentIndex);

private string GetCooldownIdentityKey(Agent agent) => _j17ScenePassiveInteractionController.GetCooldownIdentityKey(agent);

private void ApplyInteractionGraceAndGroupCooldown(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget = null, IEnumerable<NpcDataPacket> participantsData = null) => _j17ScenePassiveInteractionController.ApplyInteractionGraceAndGroupCooldown(graceSeconds, cooldownSeconds, participants, extraTarget, participantsData);

private bool IsCooldownReady(Agent agent) => _j17ScenePassiveInteractionController.IsCooldownReady(agent);

private List<Agent> GetPassiveCooldownGroupAgents(Agent targetAgent) => _j17ScenePassiveInteractionController.GetPassiveCooldownGroupAgents(targetAgent);

private void TriggerPassiveReaction(Agent targetAgent) => _j17ScenePassiveInteractionController.TriggerPassiveReaction(targetAgent);

private static string BuildPassiveReactionFactText(Agent targetAgent) => ScenePassiveInteractionController.BuildPassiveReactionFactText(targetAgent);

private static bool ShouldUseCombatPassiveReactionText(Agent targetAgent, out bool armed) => ScenePassiveInteractionController.ShouldUseCombatPassiveReactionText(targetAgent, out armed);

private static bool IsPassiveReactionOpponentContext() => ScenePassiveInteractionController.IsPassiveReactionOpponentContext();

private static bool IsArenaLikePassiveReactionContext() => ScenePassiveInteractionController.IsArenaLikePassiveReactionContext();

private static bool HasMissionBehaviorTypeNameForPassiveReaction(params string[] typeNames) => ScenePassiveInteractionController.HasMissionBehaviorTypeNameForPassiveReaction(typeNames);

private static string BuildCombatPassiveBrawlFactText(bool opponentContext) => ScenePassiveInteractionController.BuildCombatPassiveBrawlFactText(opponentContext);

private static string BuildCombatPassiveArmedFactText(Agent targetAgent, bool opponentContext) => ScenePassiveInteractionController.BuildCombatPassiveArmedFactText(targetAgent, opponentContext);

internal static string BuildFallbackSceneTauntSpeech(bool escalatedToFight) => ScenePassiveInteractionController.BuildFallbackSceneTauntSpeech(escalatedToFight);

internal static string BuildCombatActiveShoutExtraFact(Agent targetAgent) => ScenePassiveInteractionController.BuildCombatActiveShoutExtraFact(targetAgent);

internal static bool IsActiveSceneConversationDuelCombat() => ScenePassiveInteractionController.IsActiveSceneConversationDuelCombat();

internal static bool IsMeetingPseudoCombatContext() => ScenePassiveInteractionController.IsMeetingPseudoCombatContext();

internal static bool IsSceneConversationMissionEnding() => ScenePassiveInteractionController.IsSceneConversationMissionEnding();

private static string TryGetActiveWeaponDisplayNameForPassiveReaction(Agent agent) => ScenePassiveInteractionController.TryGetActiveWeaponDisplayNameForPassiveReaction(agent);

private static bool IsAgentUsingRealWeaponForPassiveReaction(Agent agent) => ScenePassiveInteractionController.IsAgentUsingRealWeaponForPassiveReaction(agent);

	private static bool CanAgentUseSceneLipSync(Agent agent, out string reason) => NativeConversationSpeechAdapter.CanAgentUseSceneLipSync(agent, out reason, _j17NativeConversationSpeechQualificationPorts);

	private static long RegisterNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, float estimatedDurationSeconds, int textLength) => _j17NativeConversationPlaybackWaitAdapter.RegisterNativeConversationTtsPlaybackWait(request, estimatedDurationSeconds, textLength);

	private static int ResolveNativeConversationTtsPlaybackWaitTimeoutMs(float estimatedDurationSeconds, int textLength) => _j17NativeConversationPlaybackWaitAdapter.ResolveNativeConversationTtsPlaybackWaitTimeoutMs(estimatedDurationSeconds, textLength);

	private static void ScheduleNativeConversationTypewriterPlaybackFallback(long waitToken, int agentIndex, float estimatedDurationSeconds, int textLength) => _j17NativeConversationPlaybackWaitAdapter.ScheduleNativeConversationTypewriterPlaybackFallback(waitToken, agentIndex, estimatedDurationSeconds, textLength);

	private static int ResolveNativeConversationTypewriterFallbackDelayMs(float estimatedDurationSeconds, int textLength) => _j17NativeConversationPlaybackWaitAdapter.ResolveNativeConversationTypewriterFallbackDelayMs(estimatedDurationSeconds, textLength);

	private static bool IsNativeConversationTtsPlaybackWaitToken(long token, int agentIndex) => _j17NativeConversationPlaybackWaitAdapter.IsNativeConversationTtsPlaybackWaitToken(token, agentIndex);

	internal static bool CompleteNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, string reason, bool force = false) => _j17NativeConversationPlaybackWaitAdapter.CompleteNativeConversationTtsPlaybackWait(request, reason, force);

	private static void CompleteNativeConversationTtsPlaybackWaitByToken(long token, string reason) => _j17NativeConversationPlaybackWaitAdapter.CompleteNativeConversationTtsPlaybackWaitByToken(token, reason);

	private static bool IsNativeConversationTtsPlaybackWaitRequest(TtsEngine.PlaybackRequest request) => _j17NativeConversationPlaybackWaitAdapter.IsNativeConversationTtsPlaybackWaitRequest(request);

	private static bool IsNativeConversationLipSyncAgentIndex(int agentIndex) => NativeConversationSpeechAdapter.IsNativeConversationLipSyncAgentIndex(agentIndex, _j17NativeConversationSpeechQualificationPorts);

	private static bool IsNativeConversationLipSyncAgent(Agent agent) => NativeConversationSpeechAdapter.IsNativeConversationLipSyncAgent(agent, _j17NativeConversationSpeechQualificationPorts);

	private static bool CanAgentUseSceneLipSyncExternal(int agentIndex, out string reason) => NativeConversationSpeechAdapter.CanAgentUseSceneLipSyncExternal(agentIndex, out reason, _j17NativeConversationSpeechQualificationPorts);

private static bool TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex, out string weaponName) => ScenePassiveInteractionController.TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(agent, equipmentIndex, out weaponName);

private static bool IsRealWeaponWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex) => ScenePassiveInteractionController.IsRealWeaponWieldedSlotForPassiveReaction(agent, equipmentIndex);

private static bool IsRealWeaponMissionWeaponForPassiveReaction(MissionWeapon missionWeapon) => ScenePassiveInteractionController.IsRealWeaponMissionWeaponForPassiveReaction(missionWeapon);

private static bool IsRealWeaponEquipmentElementForPassiveReaction(EquipmentElement equipmentElement) => ScenePassiveInteractionController.IsRealWeaponEquipmentElementForPassiveReaction(equipmentElement);

	private static string BuildAutoGroupChatReplyInstruction(string npcName, List<NpcDataPacket> allNpcData)
	{
        return ScenePromptMessageProjectionComposer.BuildAutoGroupChatReplyInstruction(npcName, allNpcData);
    }

	private static string NormalizeSceneHistoryLineForLoreQuery(string line)
	{
        return ScenePromptMessageProjectionComposer.NormalizeSceneHistoryLineForLoreQuery(line);
    }

	private static string BuildRecentSceneLoreQueryText(List<string> historyLines, int maxLines = 3, int maxChars = 280)
	{
    return ScenePromptMessageProjectionComposer.BuildRecentSceneLoreQueryText(historyLines, maxLines, maxChars);
}



	private string BuildAutoGroupLoreContextForSpeaker(NpcDataPacket speakerNpc, Agent speakerAgent, CharacterObject speakerCharacter, Hero speakerHero, string kingdomIdOverride, List<string> visibleHistoryLines)
	{
    return SceneHistoryPromptCapture.BuildAutoGroupLoreContextForSpeaker(speakerNpc, speakerAgent, speakerCharacter, speakerHero, kingdomIdOverride, visibleHistoryLines);
}

private static bool _nativeConversationInputOpen { get => SceneShoutInputController._nativeConversationInputOpen; set => SceneShoutInputController._nativeConversationInputOpen = value; }

private static string _nativeConversationInputTargetKey { get => SceneShoutInputController._nativeConversationInputTargetKey; set => SceneShoutInputController._nativeConversationInputTargetKey = value; }
















	private static readonly NativeConversationSessionOwner _nativeSessionOwner = new NativeConversationSessionOwner(NextConversationEventSequence, DuelSettings.DailyConversationHistoryLineLimitMax);
	internal static void CloseNativeSessionHistoryForInput(bool clearSessionHistory) => _nativeSessionOwner.CloseInput(clearSessionHistory);





	private static ShoutBehavior _freezeWatchdogDiagnosticInstance;

internal static string GetFreezeWatchdogDiagnosticSnapshot()
	{
        return ConversationDiagnosticsCaptureAdapter.Snapshot(_nativeSessionOwner.AppendDiagnostics, ()=>Volatile.Read(ref _freezeWatchdogDiagnosticInstance)?.DiagnosticCapture);
    }

private void AppendFreezeWatchdogDiagnosticSnapshot(StringBuilder sb)
	{
        DiagnosticCapture.Append(sb);
    }

public static bool IsNativeConversationInputOpenForExternal() => SceneShoutInputController.IsNativeConversationInputOpenForExternal();

public static bool CanSubmitNativeConversationForExternal() => SceneShoutInputController.CanSubmitNativeConversationForExternal();

	// Legacy public availability query retained for binary/source compatibility.
	// The current overlay uses a request-bound presentation scope instead. This
	// still fails closed for a scene Agent, while map/tableau conversations keep
	// their existing behavior.
	public static bool IsNativeConversationResponseTargetAvailableForExternal()
	{
		try
		{
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				return false;
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			return IsNativeConversationResponseTargetAvailableForActionDispatch(targetAgentIndex, targetHero, targetCharacter, out var _);
		}
		catch
		{
			return false;
		}
	}

public static bool ShouldSuppressNativeConversationVisibleStreamingForTtsExternal() => CurrentInstance?._j17NativeConversationSpeechAdapter.ShouldSuppressNativeConversationVisibleStreamingForTtsExternal() ?? false;

	public static bool ShouldUseMapConversationTableauPlaybackForNativeTtsExternal() => CurrentInstance?._j17NativeConversationSpeechAdapter.ShouldUseMapConversationTableauPlaybackForNativeTtsExternal() ?? false;

	private static bool IsMapConversationMission(ICampaignMission campaignMission) => NativeConversationSpeechAdapter.IsMapConversationMission(campaignMission);

	private static bool TryQueueNativeMapConversationTableauTtsPlayback(TtsEngine.PlaybackRequest request, string wavPath, string xmlPath, float durationSecs) => CurrentInstance?._j17NativeConversationSpeechAdapter.TryQueueNativeMapConversationTableauTtsPlayback(request, wavPath, xmlPath, durationSecs) ?? false;

	private static void ScheduleNativeMapConversationTtsFileCleanup(string wavPath, string xmlPath, float durationSecs) => NativeConversationSpeechAdapter.ScheduleNativeMapConversationTtsFileCleanup(wavPath, xmlPath, durationSecs);

	public static Task WaitForNativeConversationTtsPlaybackFinishedForExternalAsync() => _j17NativeConversationPlaybackWaitAdapter.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();

	// Builds a UI-only RichText copy from a completed main/final reply; callers must never pass incomplete stream fragments.
	public static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText)
	{
    return SceneAgentIdentityPromptCaptureAdapter.FormatNativeConversationDisplayTextForExternal(rawVisibleText);
}

	// The completed-reply callback carries its original target so a later action cannot redirect NPC/玩家 links before the UI formats them on the main thread.
	public static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText, Hero targetHero, CharacterObject targetCharacter)
	{
    return SceneAgentIdentityPromptCaptureAdapter.FormatNativeConversationDisplayTextForExternal(rawVisibleText, targetHero, targetCharacter);
}

public static Hero GetNativeConversationTargetHeroForExternal() => SceneShoutInputController.GetNativeConversationTargetHeroForExternal();

	public static bool CanEditNativeConversationPersonaForExternal() => PersonaEditorController.CanOpenNativePersonaEditor(GetNativeConversationTargetHeroForExternal, MyBehavior.IsDevDataManagementEnabledForExternal, hero => hero == Hero.MainHero, hero => hero.CharacterObject?.IsHero == true);

	public static bool CanEditNativeConversationNpcForExternal()
	{
		return CanEditNativeConversationPersonaForExternal();
	}

	public static bool CanOpenNativeConversationTagTestForExternal()
	{
        var owner=CurrentInstance; if(owner==null) { return false; } return owner.DeveloperActions.CanOpenNativeConversationTagTestForExternal();
    }

	public static bool OpenNativeConversationTagTestForExternal(Action onFinished = null)
	{
        var owner=CurrentInstance; if(owner==null) { return false; } return owner.DeveloperActions.OpenNativeConversationTagTestForExternal(onFinished);
    }

	public static bool TrySubmitNativeConversationTagTestForExternal(string rawText, out string statusText)
	{
        var owner=CurrentInstance; if(owner==null) { statusText = ""; return false; } return owner.DeveloperActions.TrySubmitNativeConversationTagTestForExternal(rawText,out statusText);
    }

	private static void ShowTagTestStatus(string statusText, bool success)
	{
        ConversationDeveloperActionTestController.ShowTagTestStatus(statusText,success);
    }

	private static int CountDeveloperTagTestTags(string text)
	{
        return ConversationDeveloperActionTestController.CountDeveloperTagTestTags(text);
    }

	private void CommitNativeConversationTagTestVisibleLine(Hero targetHero, CharacterObject targetCharacter, string npcName, string visible, int targetAgentIndex)
	{
        DeveloperActions.CommitNativeConversationTagTestVisibleLine(targetHero,targetCharacter,npcName,visible,targetAgentIndex);
    }

	public static bool OpenNativeConversationNpcEditorForExternal(Action onFinished = null) => PersonaEditorController.OpenNativeEditor(CanEditNativeConversationNpcForExternal, GetNativeConversationTargetHeroForExternal, MyBehavior.OpenHeroNpcEditorForExternal, onFinished, ex => Logger.Log("NativeConversation", "[WARN] Failed to open native conversation NPC editor: " + ex.Message));

	public static bool OpenNativeConversationPersonaEditorForExternal(Action onFinished = null) => PersonaEditorController.OpenNativeEditor(CanEditNativeConversationPersonaForExternal, GetNativeConversationTargetHeroForExternal, MyBehavior.OpenHeroPersonaEditorForExternal, onFinished, ex => Logger.Log("NativeConversation", "[WARN] Failed to open native conversation persona editor: " + ex.Message));

public static void OpenNativeConversationInputForExternal() => SceneShoutInputController.OpenNativeConversationInputForExternal();

public static void OpenNativeConversationInputSilentlyForExternal() => SceneShoutInputController.OpenNativeConversationInputSilentlyForExternal();

public static void CloseNativeConversationInputForExternal() => SceneShoutInputController.CloseNativeConversationInputForExternal();

public static bool OpenNativeConversationGiveShowForExternal(Action onFinished = null) => SceneTradeController.OpenNativeConversationGiveShowForExternal(onFinished);

internal static int TryResolveNativeConversationAgentIndex(Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);

private static bool IsUsableNativeConversationFallbackAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.IsUsableNativeConversationFallbackAgent(agent, targetHero, targetCharacter);

private static bool IsValidNativeConversationTargetAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter) => SceneShoutInputController.IsValidNativeConversationTargetAgent(agent, targetHero, targetCharacter);

	internal static bool TryResolveWildernessNonHeroRewardParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
        return SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out party);
    }

	private static bool TryResolveNativeConversationMeetingTauntParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
        return ConversationActionBoundaryBannerlordAdapter.TryResolveNativeConversationMeetingTauntParty(targetHero,targetCharacter,targetAgentIndex,out party);
    }

private void OpenNativeConversationGiveShowMenu(NpcDataPacket targetNpc, Hero targetHero, CharacterObject targetCharacter, Action onFinished) => _j17SceneTradeController.OpenNativeConversationGiveShowMenu(targetNpc, targetHero, targetCharacter, onFinished);

	public static bool TryGetNativeConversationHistoryTargetForExternal(out Hero targetHero, out string targetName)
	{
    return SceneAgentIdentityPromptCaptureAdapter.TryGetNativeConversationHistoryTargetForExternal(out targetHero, out targetName);
}

	// History retains the concrete non-hero target for identity, but the formatter links NPC only when that target resolves to a Hero.
	public static bool TryGetNativeConversationLinkTargetForExternal(out Hero targetHero, out CharacterObject targetCharacter)
	{
    return SceneAgentIdentityPromptCaptureAdapter.TryGetNativeConversationLinkTargetForExternal(out targetHero, out targetCharacter);
}

	public static bool TryGetNativeConversationPersistentHistoryTargetForExternal(out Hero targetHero, out string targetName, out string memoryId)
	{
    return NativePromptIdentityCapture.TryGetNativeConversationPersistentHistoryTargetForExternal(out targetHero, out targetName, out memoryId);
}

	// Illustrator reads only the current encounter; the existing history-log API retains its history.
	public static List<AnimusForgeDialogueHistoryEntry> GetCurrentNativeConversationHistoryEntriesForExternal(int maxLines = 260)
	{
		long boundary = SceneConversationHistoryOwner.ReadNativeIllustrationHistoryBoundary();
		return GetNativeConversationSessionHistoryEntriesForExternal(maxLines)
			.Where(entry => entry != null && entry.EventSequence > boundary).ToList();
	}

	public static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistoryEntriesForExternal(int maxLines = 260)
	{
        return NativePendingHistoryApplicationAdapter.GetNativeConversationSessionHistoryEntriesForExternal(_nativeSessionOwner, maxLines);
    }

	public static void ClearNativeConversationSessionHistoryForExternal(Hero targetHero, CharacterObject targetCharacter = null, string npcName = null, int dayIndex = -1)
	{
        NativePendingHistoryApplicationAdapter.ClearNativeConversationSessionHistoryForExternal(_nativeSessionOwner, targetHero, targetCharacter, npcName, dayIndex);
    }

	public static void SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(
		Hero targetHero,
		CharacterObject targetCharacter,
		string npcName,
		int dayIndex,
		IEnumerable<AnimusForgeDialogueHistoryEntry> previousEntries,
		IEnumerable<AnimusForgeDialogueHistoryEntry> currentEntries,
		string reason)
	{
        SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(targetHero, targetCharacter, npcName, dayIndex, previousEntries, currentEntries, reason, completeDaySnapshot: true);
    }

public static void SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(
		Hero targetHero,
		CharacterObject targetCharacter,
		string npcName,
		int dayIndex,
		IEnumerable<AnimusForgeDialogueHistoryEntry> previousEntries,
		IEnumerable<AnimusForgeDialogueHistoryEntry> currentEntries,
		string reason, bool completeDaySnapshot)
	{
        NativePendingHistoryApplicationAdapter.SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(_nativeSessionOwner, targetHero, targetCharacter, npcName, dayIndex, previousEntries, currentEntries, reason, completeDaySnapshot);
    }















	public static void RecordNativeConversationNpcLineForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string text, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
        NativePendingHistoryApplicationAdapter.RecordNativeConversationNpcLineForExternal(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, targetHero, targetCharacter, npcName, text, targetAgentIndex, npc);
    }



	internal static string BuildNativeConversationHistoryKey(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null) => CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);

	internal static void AppendNativeConversationSessionHistory(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, bool bridgeToSceneHistory = true, int targetAgentIndex = -1, NpcDataPacket npc = null, int playerTargetAgentIndex = -1, string playerTargetName = null, string capturedHistoryKey = null) => AppendNativeConversationSessionHistoryCaptured(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, bridgeToSceneHistory, targetAgentIndex, npc, playerTargetAgentIndex, playerTargetName, capturedHistoryKey);

	// A native request records the player line before its LLM calls so the current
	// turn is available to the main and postprocess prompts. If its live scene
	// target disappears, remove only that request's globally unique event rather
	// than leaving an orphaned player line in either shared history store.
	private static void RollbackNativeConversationPendingPlayerHistory(ShoutBehavior owner,
        NativeConversationAdmission admission, string historyKey, long eventSequence, string reason)
    {
        if (owner == null || eventSequence <= 0 || string.IsNullOrWhiteSpace(historyKey)) return;
        owner.NativePendingHistoryApplication.RollbackNativeConversationPendingPlayerHistory(admission, historyKey, eventSequence, reason);
    }



	internal static void MarkNativeConversationCurrentDialogRecorded(Hero targetHero, CharacterObject targetCharacter, string npcName, string currentDialogText, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
        NativePendingHistoryApplicationAdapter.MarkNativeConversationCurrentDialogRecorded(_nativeSessionOwner, targetHero, targetCharacter, npcName, currentDialogText, targetAgentIndex, npc);
    }

	internal static string NormalizeNativeConversationFactLineForPrompt(string text, string speaker)
	{
		return ConversationSpeechTextRules.NormalizeNativeConversationFactLineForPrompt(text, speaker);
	}

	private static string NormalizeNativeConversationVisibleTextKey(string text)
	{
		return ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(text, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
	}

	private static string NormalizeNativeConversationHistoryTextForPostprocess(string text)
	{
		return ConversationSpeechTextRules.NormalizeNativeConversationHistoryTextForPostprocess(text);
	}

	private static int ResolveDailyConversationHistoryLineLimit(int requestedMaxLines = 0)
	{
		return NativeConversationSessionOwner.ResolveHistoryLineLimit(requestedMaxLines, DuelSettings.DailyConversationHistoryLineLimitMax, requestedMaxLines > 0 ? 0 : DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
	}

	private static void TryRecordNativeConversationCurrentDialogLine(Hero targetHero, CharacterObject targetCharacter, string npcName, string npcDisplayName, string currentNativeDialogText)
	{
        SceneHistoryPromptCaptureAdapter.TryRecordNativeConversationCurrentDialogLine(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, targetHero, targetCharacter, npcName, npcDisplayName, currentNativeDialogText);
    }

	public static string GetLatestNativeConversationNpcUtteranceForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
        return SceneHistoryPromptCaptureAdapter.GetLatestNativeConversationNpcUtteranceForExternal(_nativeSessionOwner, targetHero, targetCharacter, targetAgentIndex);
    }

	private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
        return SceneHistoryPromptCaptureAdapter.GetNativeConversationSessionHistorySnapshot(_nativeSessionOwner, targetHero, targetCharacter, npcName, targetAgentIndex, maxLines, npc, capturedHistoryKey);
    }

	private static bool HasNativeConversationSessionHistory(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
    {
        return SceneHistoryPromptCaptureAdapter.HasNativeConversationSessionHistory(_nativeSessionOwner, targetHero, targetCharacter, npcName, targetAgentIndex, npc);
    }

	private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
        return SceneHistoryPromptCaptureAdapter.BuildNativeConversationSessionHistoryMessages(_nativeSessionOwner, targetHero, targetCharacter, npcName, targetAgentIndex, maxLines, npc, capturedHistoryKey);
    }

	private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessagesForAgent(int targetAgentIndex, int maxLines = 0)
	{
        return SceneHistoryPromptCaptureAdapter.BuildNativeConversationSessionHistoryMessagesForAgent(_nativeSessionOwner, targetAgentIndex, maxLines);
    }

	internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, int targetAgentIndex)
	{
		return SceneHistoryPromptCaptureAdapter.BuildUncompressedMemoryRoleMessagesForPrompt(hero, targetAgentIndex);
	}

	internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex)
	{
		return SceneHistoryPromptCaptureAdapter.BuildUncompressedMemoryRoleMessagesForPrompt(hero, targetCharacter, npc, targetAgentIndex);
	}

	// An NPC-initiated opening begins a new native conversation.  Its short-lived
	// native cache can contain an older conversation with the same NPC, but it
	// does not contain the shared courier/scene history.  When the canonical
	// daily memory is injected for that opening, retain only native entries that
	// have not yet reached persistent memory (for example, an action fact).
	internal static List<ConversationMessage> RemoveNativeMessagesAlreadyInPersistentMemory(List<ConversationMessage> nativeMessages, List<ConversationMessage> persistentMessages) => HistorySectionProjectionOwner.RemoveNativeMessagesAlreadyInPersistentMemory(nativeMessages, persistentMessages);

	private static string BuildConversationMemoryDedupKey(ConversationMessage message) => HistorySectionProjectionOwner.BuildConversationMemoryDedupKey(message);

	internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(int targetAgentIndex, Dictionary<int, Hero> resolvedHeroes)
	{
		return SceneHistoryPromptCaptureAdapter.BuildUncompressedMemoryRoleMessagesForPrompt(targetAgentIndex, resolvedHeroes);
	}

	private static List<string> BuildNativeConversationSceneHistoryLinesForPrompt(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
        return SceneHistoryPromptCaptureAdapter.BuildNativeConversationSceneHistoryLinesForPrompt(_nativeSessionOwner, targetHero, targetCharacter, npcName, targetAgentIndex, maxLines);
    }

	internal static List<string> BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
        return SceneHistoryPromptCaptureAdapter.BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, targetHero, targetCharacter, npcName, targetAgentIndex, maxLines);
    }

	private static bool HasNativeConversationSceneDialogueHistoryForPrompt(int targetAgentIndex)
	{
        return SceneHistoryPromptCaptureAdapter.HasNativeConversationSceneDialogueHistoryForPrompt(CurrentSceneHistoryPromptCapture, targetAgentIndex);
    }

	private static bool ShouldIncludeCurrentSceneSessionInNativePersistedHistory(int targetAgentIndex)
	{
        return SceneHistoryPromptCaptureAdapter.ShouldIncludeCurrentSceneSessionInNativePersistedHistory(CurrentSceneHistoryPromptCapture, targetAgentIndex);
    }

	public static List<string> GetNativeConversationAuxiliaryHistoryLinesForExternal(int maxLines = 6)
	{
        return SceneHistoryPromptCaptureAdapter.GetNativeConversationAuxiliaryHistoryLinesForExternal(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, maxLines);
    }

	internal static Func<string> CaptureNativeConversationPersistedHistoryWork(Hero targetHero, CharacterObject targetCharacter, string playerText, string currentNativeDialogText, bool includeCurrentActiveSceneSession, long generation)
    {
        return SceneHistoryPromptCaptureAdapter.CaptureNativeConversationPersistedHistoryWork(_nativeSessionOwner, NativePromptIdentityCapture, targetHero, targetCharacter, playerText, currentNativeDialogText, includeCurrentActiveSceneSession, generation);
    }

	private static bool IsWildernessNonHeroMemoryScope(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.IsWildernessNonHeroMemoryScope(agentIndex);
	}

	private static string BuildWildernessNonHeroPartyRepresentativePrompt(int agentIndex)
	{
        return SceneAgentIdentityPromptCaptureAdapter.BuildWildernessNonHeroPartyRepresentativePrompt(agentIndex);
    }

	private static PartyBase ResolveWildernessNonHeroPartyBaseForPrompt(int agentIndex)
	{
        return SceneAgentIdentityPromptCaptureAdapter.ResolveWildernessNonHeroPartyBaseForPrompt(agentIndex);
    }

	internal static MobileParty TryResolveWildernessNonHeroMobileParty(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMobileParty(agentIndex);
	}

	internal static string NormalizeWildernessNonHeroMemoryKeyPart(string value) => NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(value);

	private static void LogNonHeroMemoryTrace(string message)
	{
		SceneAgentIdentityPromptCaptureAdapter.LogNonHeroMemoryTrace(message);
	}

	private static string BuildWildernessNonHeroPartyTrace(MobileParty party)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildWildernessNonHeroPartyTrace(party);
	}

	private static string BuildWildernessNonHeroPartyMemoryKey(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildWildernessNonHeroPartyMemoryKey(agentIndex);
	}

	private static string BuildWildernessNonHeroCharacterMemoryKey(CharacterObject character, MobileParty party)
	{
		return SceneAgentIdentityPromptCaptureAdapter.BuildWildernessNonHeroCharacterMemoryKey(character, party);
	}

	internal static bool TryResolveWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, out string memoryId, out string memoryName)
	{
		return SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out memoryId, out memoryName);
	}

	public static bool TryResolveWildernessNonHeroMemoryForExternal(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, out string memoryId, out string memoryName)
	{
		return TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out memoryId, out memoryName);
	}

	internal static RewardSystemBehavior.RpItemIntroductionContext CreateRpItemIntroductionContextForReward(
		Hero giverHero,
		NpcDataPacket giverNpc,
		CharacterObject giverCharacter,
		int giverAgentIndex,
		string giverName,
		string currentPlayerText,
		string currentNpcText,
		bool includeNativeConversationSessionHistory = false)
	{
        return ConversationActionBoundaryBannerlordAdapter.CreateRpItemIntroductionContextForReward(giverHero,giverNpc,giverCharacter,giverAgentIndex,giverName,currentPlayerText,currentNpcText,includeNativeConversationSessionHistory);
    }

	internal static bool MayContainGeneratedRpItemReward(string responseText)
	{
		return ConversationActionPostprocessOwner.MayContainGeneratedRpItemReward(responseText);
	}

	private static string BuildNativeConversationNonHeroVoiceKey(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex)
	{
        return SceneAgentIdentityPromptCaptureAdapter.BuildNativeConversationNonHeroVoiceKey(npc, targetHero, targetCharacter, agentIndex);
    }

	private static string BuildNativeConversationNonHeroUnnamedKey(CharacterObject targetCharacter, string npcName, int agentIndex) => CaptureNativeConversationNonHeroUnnamedKey(targetCharacter, npcName, agentIndex);

	private static float ResolveNativeConversationNonHeroAge(CharacterObject targetCharacter)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ResolveNativeConversationNonHeroAge(targetCharacter);
	}

	internal static string BuildWildernessNonHeroHistoryContextForPrompt(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession = false)
	{
        return SceneHistoryPromptCaptureAdapter.BuildWildernessNonHeroHistoryContextForPrompt(npc, targetHero, targetCharacter, agentIndex, currentInput, secondaryInput, includeCurrentActiveSceneSession);
    }

	internal static void AppendWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string playerText, string aiText, string extraFact, int sceneSessionId = -1)
	{
        SceneHistoryPromptCaptureAdapter.AppendWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, playerText, aiText, extraFact, sceneSessionId);
    }

	internal static string BuildNativeConversationNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc = null)
	{
        return SceneAgentIdentityPromptCaptureAdapter.BuildNativeConversationNpcListBlockForPrompt(presentNpcs, selfNpc);
    }

	internal static string ResolveCurrentNativeConversationTargetKey()
	{
		if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
		{
			return "";
		}
		return targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "";
	}

	internal static string ResolveNativeConversationPostprocessChainName()
	{
		return ConversationActionPostprocessOwner.ResolveNativeConversationPostprocessChainName();
	}

	internal static bool ContainsKingdomAnnexActionTagForLog(string text)
	{
		return ConversationActionPostprocessOwner.ContainsKingdomAnnexActionTagForLog(text);
	}

	internal static bool ContainsVassalageActionTagForLog(string text)
	{
		return ConversationActionPostprocessOwner.ContainsVassalageActionTagForLog(text);
	}

	internal static string ResolveScenePostprocessChainName()
	{
		return ConversationActionPostprocessOwner.ResolveScenePostprocessChainName();
	}

	public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText)
	{
		return SubmitNativeConversationTextForExternalAsync(playerText, null, null, null);
	}

	/// <summary>
	/// Creates the opt-in Native Conversation refactor facade. This is a
	/// wiring seam only: the existing SubmitNativeConversation* entry points
	/// remain the default path until the real legacy rule/prompt/action ports
	/// have been audited. The caller must create and use the facade on the
	/// game main thread when it captures or commits live conversation state.
	/// </summary>
	public static LegacyNativeConversationFacade CreateNativeConversationRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway)
	{
		return LegacyInteractionSnapshotAdapters.CreateNativeConversationFacade(ports, gateway);
	}

	/// <summary>
	/// Opt-in Native facade overload for Prompt sections assembled by the
	/// existing channel owner at the interaction boundary.
	/// </summary>
	public static LegacyNativeConversationFacade CreateNativeConversationRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway,
		Func<string, DetachedPromptSections> promptSectionsProvider)
	{
		return LegacyInteractionSnapshotAdapters.CreateNativeConversationFacade(ports, gateway, promptSectionsProvider);
	}

	/// <summary>
	/// Enables an opt-in, content-hashed comparison between the final Native
	/// legacy prompt and its detached sections. It is disabled by default and
	/// has no effect on request routing, action execution, or persistence.
	/// </summary>
	public static bool NativeConversationDetachedPromptParityLoggingEnabled
	{
		get { return Volatile.Read(ref _nativeDetachedPromptParityLoggingEnabled) != 0; }
	}

	public static void SetNativeConversationDetachedPromptParityLoggingForExternal(bool enabled)
	{
		Volatile.Write(ref _nativeDetachedPromptParityLoggingEnabled, enabled ? 1 : 0);
	}

	/// <summary>
	/// Creates the explicit Native opt-in runner. The caller still owns the
	/// main-thread capture and commit callback; the default Native entry is not
	/// routed through this runner.
	/// </summary>
	public static LegacyNativeConversationOptInRunner CreateNativeConversationOptInRunnerForExternal(
		LegacyNativeConversationFacade facade)
	{
		return new LegacyNativeConversationOptInRunner(facade);
	}

	private static IEconomyRewardDebtMainThreadPort CreateEconomyReplayPortForExternal(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		string displayName,
		string expectedInteractionSubjectId)
	{
        return ConversationActionContextBannerlordAdapter.CreateEconomyReplayPortForExternal(targetHero,targetCharacter,targetAgentIndex,displayName,expectedInteractionSubjectId);
    }

	internal static string ResolveDetachedInteractionSubjectId(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		NpcDataPacket npc)
	{
        return ConversationActionContextBannerlordAdapter.ResolveDetachedInteractionSubjectId(targetHero,targetCharacter,targetAgentIndex,npc);
    }

	/// <summary>
	/// Creates the real Native ActionPlan executor for an opt-in turn. The
	/// current target and its interaction-boundary prompt targets are captured
	/// here on the game thread; the returned executor must only be used by the
	/// host's main-thread commit callback. It reuses the same core as the legacy
	/// Native path, so all existing domain validators, AFEF writes, notifications
	/// and action ordering remain authoritative.
	/// </summary>
	public static LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal()
	{
        var owner=CurrentInstance; if(owner==null) { return null; } return owner.ActionExecutors.CreateNativeConversationActionPlanExecutorForExternal();
    }

	/// <summary>
	/// Executes one explicitly opted-in Native turn through the detached
	/// facade. Capture must be called from the game interaction boundary; the
	/// generated envelope is the only value sent to the worker. The commit
	/// callback is synchronously marshalled back through the existing Native
	/// main-thread queue, where the real ActionPlan executor and memory facade
	/// run. The unchanged Native entry is used only when the detached
	/// infrastructure fails; this method never changes the default entry.
	/// </summary>
	public static Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInForExternalAsync(
		LegacyNativeConversationFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		Func<Task<string>> fallbackToLegacyNative,
		CancellationToken cancellationToken)
	{
		ShoutBehavior instance = CurrentInstance;
		if (instance == null)
		{
			return CompleteNativeConversationOptInFallbackAsync("host_not_ready", fallbackToLegacyNative);
		}
		return instance.SubmitNativeConversationRefactorOptInCoreAsync(
			facade,
			configuration,
			moduleId,
			providerId,
			playerText,
			fallbackToLegacyNative,
			cancellationToken);
	}

	private Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInCoreAsync(
		LegacyNativeConversationFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		Func<Task<string>> fallbackToLegacyNative,
		CancellationToken cancellationToken)
	{
        return ActionExecutors.SubmitNativeConversationRefactorOptInCoreAsync(facade,configuration,moduleId,providerId,playerText,fallbackToLegacyNative,cancellationToken);
    }

	private static IInteractionMemory CreateNativeConversationMemoryFacadeForExternal()
	{
        return ConversationActionExecutorComposition.CreateNativeConversationMemoryFacadeForExternal();
    }

	private Task<InteractionCommitResult> DispatchNativeConversationOptInCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
        return ActionExecutors.DispatchNativeConversationOptInCommitAsync(commit,targetLog,targetAgentIndex);
    }

	private static Task<LegacyNativeConversationOptInResult> CompleteNativeConversationOptInFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacyNative)
	{
        return ConversationActionExecutorComposition.CompleteNativeConversationOptInFallbackAsync(errorCode,fallbackToLegacyNative);
    }

	/// <summary>
	/// Opt-in Native facade overload accepting one atomic main/postprocess
	/// Prompt sections bundle for the same interaction turn.
	/// </summary>
	public static LegacyNativeConversationFacade CreateNativeConversationRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway,
		Func<string, DetachedInteractionPromptSections> promptSectionsProvider)
	{
		return LegacyInteractionSnapshotAdapters.CreateNativeConversationFacade(ports, gateway, promptSectionsProvider);
	}

	/// <summary>
	/// Creates the explicit SceneShout detached facade. The existing scene
	/// conversation remains the default path; this factory is only for an
	/// opt-in host which supplies the channel's ActionPlan executor and main
	/// thread commit callback.
	/// </summary>
	public static LegacyChannelInteractionFacade CreateSceneShoutRefactorFacadeForExternal(
		LegacyInteractionPipelinePorts ports,
		ILlmGateway gateway,
		int targetAgentIndex)
	{
		return LegacyInteractionSnapshotAdapters.CreateSceneShoutInteractionFacade(
			ports,
			gateway,
			playerText => CaptureSceneShoutRefactorEnvelopeForExternal(playerText, targetAgentIndex));
	}

	/// <summary>
	/// Builds the explicit SceneShout ports from the already captured legacy
	/// prompt sections. The baseline rule keeps ordinary scene conversation
	/// eligible when no optional gameplay rule is selected; it grants no action
	/// capability by itself.
	/// </summary>
	public static LegacyInteractionPipelinePorts CreateSceneShoutDetachedPortsForExternal(
		IEnumerable<string> allowedTagFamilies,
		int maxActions = 64)
	{
        return ConversationActionExecutorComposition.CreateSceneShoutDetachedPortsForExternal(allowedTagFamilies,maxActions);
    }







	internal static string PrepareSceneMainReplySpeechText(string text, bool stopFollowing, bool endSummon)
	{
        return ScenePromptMessageProjectionComposer.PrepareSceneMainReplySpeechText(text, stopFollowing, endSummon);
    }




	/// <summary>
	/// Creates the explicit SceneShout ActionPlan executor. The executor keeps
	/// only the captured Agent index; the live Agent/Character/Hero is resolved
	/// again at the main-thread commit boundary and must still match the
	/// captured candidate. This is opt-in and does not alter the legacy shout
	/// path.
	/// </summary>
	public static LegacyNativeActionPlanExecutor CreateSceneShoutActionPlanExecutorForExternal(
		int targetAgentIndex,
		int maxActions = 64)
	{
        var owner=CurrentInstance; if(owner==null) { return null; } return owner.ActionExecutors.CreateSceneShoutActionPlanExecutorForExternal(targetAgentIndex,maxActions);
    }

	/// <summary>
	/// Executes one explicitly opted-in SceneShout turn through the detached
	/// host. The caller must create the facade and invoke this entry from the
	/// interaction boundary; the legacy SceneShout path remains unchanged.
	/// </summary>
	public static Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInForExternalAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		int targetAgentIndex,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		ShoutBehavior instance = CurrentInstance;
		if (instance == null)
		{
			return Task.FromResult(new DetachedInteractionHostResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				"host_not_ready",
				null,
				null));
		}
		return instance.SubmitSceneShoutRefactorOptInCoreAsync(
			facade,
			configuration,
			moduleId,
			providerId,
			playerText,
			targetAgentIndex,
			fallbackToLegacy,
			cancellationToken);
	}

	private Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInCoreAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		int targetAgentIndex,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
        return ActionExecutors.SubmitSceneShoutRefactorOptInCoreAsync(facade,configuration,moduleId,providerId,playerText,targetAgentIndex,fallbackToLegacy,cancellationToken);
    }

	private static IInteractionMemory CreateSceneShoutMemoryFacadeForExternal(InteractionEnvelope envelope)
	{
        return ConversationActionExecutorComposition.CreateSceneShoutMemoryFacadeForExternal(envelope);
    }

	private Task<InteractionCommitResult> DispatchSceneShoutRefactorCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
        return ActionExecutors.DispatchSceneShoutRefactorCommitAsync(commit,targetLog,targetAgentIndex);
    }

	internal static Task<DetachedInteractionHostResult> RunDetachedRefactorFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacy)
	{
        return ConversationActionExecutorComposition.RunDetachedRefactorFallbackAsync(errorCode,fallbackToLegacy);
    }

	/// <summary>
	/// Captures the single-target SceneShout prompt boundary using the same
	/// helpers as the current per-NPC scene turn. This method is deliberately
	/// synchronous and must run on the game thread; only its copied strings and
	/// memory messages cross into the detached pipeline.
	/// </summary>
	public static InteractionEnvelope CaptureSceneShoutRefactorEnvelopeForExternal(
		string playerText,
		int targetAgentIndex)
	{
		return CaptureSceneShoutRefactorEnvelopeForExternal(playerText, targetAgentIndex, null);
	}

	/// <summary>
	/// Same as the basic capture overload, with an atomic main/postprocess
	/// sections provider. The provider is evaluated once at the interaction
	/// boundary, preventing a config reload from pairing two turns.
	/// </summary>
	public static InteractionEnvelope CaptureSceneShoutRefactorEnvelopeForExternal(
		string playerText,
		int targetAgentIndex,
		Func<string, DetachedInteractionPromptSections> promptSectionsProvider)
	{
		Func<string, DetachedInteractionPromptSections> provider = promptSectionsProvider
			?? (text => BuildSceneShoutDetachedPromptSectionsForExternal(text, targetAgentIndex));
		DetachedInteractionPromptSections sections = provider(playerText) ?? DetachedInteractionPromptSections.Empty;
		return LegacyInteractionSnapshotAdapters.CaptureSceneShout(
			playerText,
			targetAgentIndex,
			sections.Main,
			sections.Postprocess);
	}

	/// <summary>
	/// Builds the real single-NPC SceneShout main/postprocess sections from the
	/// current prompt helpers. It does not call an LLM or execute an action.
	/// The postprocess user section contains the configured legacy template and
	/// current captured history; the detached composer appends the final visible
	/// reply after generation.
	/// </summary>
	public static DetachedInteractionPromptSections BuildSceneShoutDetachedPromptSectionsForExternal(
        string playerText, int targetAgentIndex)
    {
        ShoutBehavior instance = CurrentInstance;
        return instance == null || targetAgentIndex < 0
            ? DetachedInteractionPromptSections.Empty
            : instance.CreateSceneExternalPromptCaptureAdapter().Build(playerText, targetAgentIndex);
    }

	/// <summary>
	/// Captures a Native Conversation into an immutable, detached envelope for
	/// the opt-in refactor path. No LLM request is started by this method.
	/// </summary>
	public static InteractionEnvelope CaptureNativeConversationRefactorEnvelopeForExternal(string playerText)
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversation(playerText);
	}

	/// <summary>
	/// Opt-in overload for the shared detached composer. The supplied blocks
	/// must be assembled from the existing scene/native prompt helpers while the
	/// caller is on the game thread; this overload does not invent or alter
	/// Persona, history/AFEF, knowledge/RAG, rule, or action semantics.
	/// </summary>
	public static InteractionEnvelope CaptureNativeConversationRefactorEnvelopeForExternal(
		string playerText,
		DetachedPromptSections promptSections)
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversation(playerText, promptSections);
	}

	/// <summary>
	/// Creates the shared string-only composer for an opt-in Native/Scene/Courier
	/// composition. It never resolves a game object and never runs on a tick.
	/// </summary>
	public static LegacyDetachedPromptComposer CreateDetachedPromptComposerForExternal(
		int maxTokens = 4096,
		string model = "legacy-detached")
	{
		return new LegacyDetachedPromptComposer(maxTokens, model);
	}

	/// <summary>
	/// Captures a non-secret runtime configuration snapshot for the opt-in
	/// Native refactor path. The legacy gateway still owns the real API
	/// endpoint and credential lookup.
	/// </summary>
	public static RuntimeConfigSnapshot CaptureNativeConversationRefactorConfigurationForExternal()
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversationRuntimeConfiguration();
	}

	public static RuntimeConfigSnapshot CaptureSceneShoutRefactorConfigurationForExternal()
	{
		return LegacyInteractionSnapshotAdapters.CaptureNativeConversationRuntimeConfiguration();
	}

	/// <summary>
	/// Creates the detached rule selector for an opt-in refactor composition.
	/// The lookup receives only strings and rule exclusions; it must not resolve
	/// or retain Bannerlord game objects.
	/// </summary>
	public static LegacyDetachedRuleSelector CreateNativeConversationDetachedRuleSelectorForExternal(int topN = 12)
	{
    return PromptRuleCaptureBannerlordAdapter.CreateNativeConversationDetachedRuleSelectorForExternal(topN);
}

	/// <summary>
	/// Builds the explicit Native detached ports from the existing rule lookup,
	/// shared composers and allowlisted action parser. The allowlist is supplied
	/// by the channel owner; an empty allowlist intentionally produces no
	/// executable actions. No default Native call is routed here.
	/// </summary>
	public static LegacyInteractionPipelinePorts CreateNativeConversationDetachedPortsForExternal(
		IEnumerable<string> allowedTagFamilies,
		int topN = 12,
		int maxActions = 64)
	{
		return CreateNativeConversationDetachedPortsCore(allowedTagFamilies, topN, maxActions);
	}

	public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText)
	{
		return SubmitNativeConversationTextForExternalAsync(playerText, onStreamText, null, null);
	}

	public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride)
	{
		return SubmitNativeConversationTextForExternalAsync(playerText, onStreamText, currentDialogTextOverride, null);
	}

	public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted)
	{
		return SubmitNativeConversationTextForExternalAsync(playerText, onStreamText, currentDialogTextOverride, onPostprocessStarted, null);
	}

	// The main-reply callback is deliberately separate from postprocess start so UI can present a completed reply while actions still resolve.
	public static Task<string> SubmitNativeConversationTextForExternalAsync(string playerText, Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)
	{
		ShoutBehavior currentInstance = CurrentInstance;
		if (currentInstance == null)
		{
			return Task.FromResult("AnimusForge ShoutBehavior is not ready.");
		}
		return currentInstance.SubmitNativeConversationAdmittedAsync(playerText, onStreamText,
			currentDialogTextOverride, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening: false);
	}

	public static Task<string> SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted)
	{
		return SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(onStreamText, currentDialogTextOverride, onPostprocessStarted, null);
	}

	// Keep the existing three-argument entry point intact while exposing the same early completed-reply signal for NPC openings.
	public static Task<string> SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(Action<string> onStreamText, string currentDialogTextOverride, Action<string> onPostprocessStarted, Action<string, Hero, CharacterObject> onMainReplyReady)
	{
		ShoutBehavior currentInstance = CurrentInstance;
		if (currentInstance == null)
		{
			return Task.FromResult("AnimusForge ShoutBehavior is not ready.");
		}
		return currentInstance.SubmitNativeConversationAdmittedAsync("", onStreamText,
			currentDialogTextOverride, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening: true);
	}

	internal static bool TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
        return SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationTarget(out targetHero, out targetCharacter, out npcName);
    }

	internal static bool IsNativeConversationSelfTarget(Hero targetHero, CharacterObject targetCharacter)
	{
        return SceneAgentIdentityPromptCaptureAdapter.IsNativeConversationSelfTarget(targetHero, targetCharacter);
    }

	private static bool TryResolveNativeConversationTargetFromAgent(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
        return SceneAgentIdentityPromptCaptureAdapter.TryResolveNativeConversationTargetFromAgent(out targetHero, out targetCharacter, out npcName);
    }

	internal static NpcDataPacket BuildNativeConversationNpcData(Hero targetHero, CharacterObject targetCharacter)
	{
		return NativePromptIdentityCapture.BuildNativeConversationNpcData(targetHero, targetCharacter);
	}

	internal sealed class PendingNativeSceneMechanismAction
	{
		public Mission OriginMission;

		public Campaign OriginCampaign;

		public ConversationManager OriginManager;

		public Agent SpeakerAgent;

		public long ConversationGeneration;

		public NpcDataPacket Speaker;

		public List<SceneSummonPromptTarget> SummonTargets;

		public List<SceneGuidePromptTarget> GuideTargets;

		public string Tags;
	}

	internal sealed class PendingNativeSceneTauntFight
	{
		public Hero TargetHero;

		public CharacterObject TargetCharacter;

		public int TargetAgentIndex;

		public string TargetKey;

		public string TargetName;

		public float ExecuteAtMissionTime;

		public long CreatedUtcTicks;
	}

private bool TryQueueNativeSceneMechanismActionAfterConversationExit(NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, ref string content) => _j17SceneNativeMechanismController.TryQueueNativeSceneMechanismActionAfterConversationExit(npc, allNpcData, sceneSummonTargets, sceneGuideTargets, ref content);

private static void ShowNativeSceneMechanismPendingExitPrompt(int pendingCount) => SceneNativeMechanismController.ShowNativeSceneMechanismPendingExitPrompt(pendingCount);

private void ExecutePendingNativeSceneMechanismActionsAfterConversationExit(string reason) => _j17SceneNativeMechanismController.ExecutePendingNativeSceneMechanismActionsAfterConversationExit(reason);

private void TickPendingNativeSceneMechanismActions() => _j17SceneNativeMechanismController.TickPendingNativeSceneMechanismActions();

private void ExecutePendingNativeSceneMechanismAction(PendingNativeSceneMechanismAction pending, string reason) => _j17SceneNativeMechanismController.ExecutePendingNativeSceneMechanismAction(pending, reason);

private void ClearPendingNativeSceneMechanismActions(string reason) => _j17SceneNativeMechanismController.ClearPendingNativeSceneMechanismActions(reason);

private bool ScheduleNativeSceneTauntFightAfterDelay(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, string reason) => _j17SceneNativeMechanismController.ScheduleNativeSceneTauntFightAfterDelay(targetHero, targetCharacter, targetAgentIndex, targetKey, reason);

private void UpdatePendingNativeSceneTauntFightDelay() => _j17SceneNativeMechanismController.UpdatePendingNativeSceneTauntFightDelay();

private void ExecuteDelayedNativeSceneTauntFight(PendingNativeSceneTauntFight pending) => _j17SceneNativeMechanismController.ExecuteDelayedNativeSceneTauntFight(pending);

private void ClearPendingNativeSceneTauntFight(string reason) => _j17SceneNativeMechanismController.ClearPendingNativeSceneTauntFight(reason);

private bool TryExecuteNativeSceneMechanismActionTagsDirectly(NpcDataPacket npc, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string tags) => _j17SceneNativeMechanismController.TryExecuteNativeSceneMechanismActionTagsDirectly(npc, sceneSummonTargets, sceneGuideTargets, tags);

internal static void CloseNativeConversationForSceneMechanism(string reason) => SceneNativeMechanismController.CloseNativeConversationForSceneMechanism(reason);

private bool TryTriggerNativeConversationOpenLordsHallAction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content) => _j17SceneNativeMechanismController.TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, targetAgentIndex, ref content);

	private bool TryProcessSetsOwnedSettlementMassacreActionTags(int targetAgentIndex, ref string content)
	{
        return ActionBoundary.TryProcessSetsOwnedSettlementMassacreActionTags(targetAgentIndex,ref content);
    }

	private static string StripSetsOwnedSettlementMassacreActionTags(string content)
	{
		return ConversationActionPostprocessOwner.StripSetsOwnedSettlementMassacreActionTags(content);
	}

	internal static bool TryProcessCustomPolicyAgendaActionTag(Hero targetHero, string chainName, string playerProposalText, ref string content, string npcReplyTextOverride = null)
	{
        return ConversationActionBoundaryBannerlordAdapter.TryProcessCustomPolicyAgendaActionTag(targetHero,chainName,playerProposalText,ref content,npcReplyTextOverride);
    }


	internal static void LogNativeActionStep(string step, Hero targetHero, CharacterObject targetCharacter, string content)
	{
		try
		{
			Logger.Log("Logic", "[NativeActionStep] step=" + (step ?? "") + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " contentLen=" + ((content ?? "").Length));
		}
		catch
		{
		}
	}

	internal static void PrepareDuelFromActionTag(
		Hero target,
		float delaySeconds,
		DetachedDuelDispatchContext duelDispatchContext)
	{
		if (duelDispatchContext == null)
		{
			DuelBehavior.PrepareDuel(target, delaySeconds);
		}
		else
		{
			DuelBehavior.PrepareDuelForDetachedRequest(
				target,
				delaySeconds,
				duelDispatchContext);
		}
	}

	internal static void PrepareDuelFromActionTag(
		Agent target,
		float delaySeconds,
		DetachedDuelDispatchContext duelDispatchContext)
	{
		if (duelDispatchContext == null)
		{
			DuelBehavior.PrepareDuel(target, delaySeconds);
		}
		else
		{
			DuelBehavior.PrepareDuelForDetachedRequest(
				target,
				delaySeconds,
				duelDispatchContext);
		}
	}

	internal static void PrepareDuelFromActionTag(
		CharacterObject target,
		float delaySeconds,
		DetachedDuelDispatchContext duelDispatchContext)
	{
		if (duelDispatchContext == null)
		{
			DuelBehavior.PrepareDuel(target, delaySeconds);
		}
		else
		{
			DuelBehavior.PrepareDuelForDetachedRequest(
				target,
				delaySeconds,
				duelDispatchContext);
		}
	}

	internal static MyBehavior.ShoutPromptContext CreateEmptyNativeConversationPromptContext()
	{
        return ScenePromptMessageProjectionComposer.CreateEmptyNativeConversationPromptContext();
    }

	internal static string BuildNativeConversationPreprocessUnavailableText()
	{
		return "（API请求失败: 原生对话前处理超时或上一轮仍在运行，请稍后重试）";
	}

	public static bool IsNativeConversationPreprocessUnavailableTextForExternal(string text)
	{
        return ScenePromptMessageProjectionComposer.IsNativeConversationPreprocessUnavailableTextForExternal(text);
    }





	private Task<T> RunNativeConversationMainThreadFuncAsync<T>(string operationName, string targetLog, int targetAgentIndex, Func<T> func, T fallback)
        => _conversationGameThreadDispatcher.RunAsync(operationName, targetLog, targetAgentIndex, func, fallback);



	internal sealed class NativeConversationGameActionResult
	{
		public string Content;
		public WorldMapPartyCommandBehavior.WorldMapOrderApplyResult WorldMapResult;
		public bool ResponseDiscarded;
		public string FinalVisible;
	}



	internal static bool IsBannerlordMainThreadForNativeActions()
	{
		try
		{
			return TWParallel.IsMainThread();
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryConsumeNativeConversationSiegeSurrenderTag(Hero targetHero, CharacterObject targetCharacter, ref string content, out int targetAgentIndex)
	{
        return ConversationActionBoundaryBannerlordAdapter.TryConsumeNativeConversationSiegeSurrenderTag(targetHero,targetCharacter,ref content,out targetAgentIndex);
    }

	internal static bool TryConsumeNativeConversationNpcSurrenderTag(Hero targetHero, CharacterObject targetCharacter, ref string content, out int targetAgentIndex)
	{
        return ConversationActionBoundaryBannerlordAdapter.TryConsumeNativeConversationNpcSurrenderTag(targetHero,targetCharacter,ref content,out targetAgentIndex);
    }

	private void QueueNativeConversationNpcSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
        ActionBoundary.QueueNativeConversationNpcSurrender(targetHero,targetCharacter,targetAgentIndex,reason);
    }

	private void TryDrainNativeConversationQueuedActions(string reason)
	{
        ActionBoundary.TryDrainNativeConversationQueuedActions(reason);
    }

	private void RecordGeneratedNpcAfefFactsForNativeConversation(Hero targetHero, CharacterObject targetCharacter, IEnumerable<string> factLines)
	{
        ActionBoundary.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero,targetCharacter,factLines);
    }

	private static bool IsNativeConversationWorldMapContext()
	{
        return ConversationActionBoundaryBannerlordAdapter.IsNativeConversationWorldMapContext();
    }

	internal static bool TryProcessNativeConversationRawMeetingTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToBattle)
	{
        return ConversationActionBoundaryBannerlordAdapter.TryProcessNativeConversationRawMeetingTauntTags(targetHero,targetCharacter,targetAgentIndex,ref content,out escalatedToBattle);
    }

	internal static bool TryProcessNativeConversationSceneTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToFight)
	{
		return AnimusForge.ConversationActionBoundaryBannerlordAdapter.TryProcessNativeConversationSceneTauntTags(targetHero, targetCharacter, targetAgentIndex, ref content, out escalatedToFight, (hero, character, index, key, reason) => CurrentInstance?.ScheduleNativeSceneTauntFightAfterDelay(hero, character, index, key, reason) == true);
	}

	private static void StripBlockedTownTauntTags(ref string content)
	{
        ConversationActionBoundaryBannerlordAdapter.StripBlockedTownTauntTags(ref content);
    }

	private List<string> BuildPreprocessExcludedRuleIdsForCurrentInteraction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null, string currentPlayerText = null)
    {
        return SceneMechanismPromptCapture.BuildPreprocessExcludedRuleIdsForCurrentInteraction(targetHero, targetCharacter, targetAgentIndex, hasAnyHero, sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates, currentPlayerText);
    }

	private bool CanInjectRuleTopicIntoPreprocessForCurrentInteraction(string ruleId, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null)
    {
        return SceneMechanismPromptCapture.CanInjectRuleTopicIntoPreprocessForCurrentInteraction(ruleId, targetHero, targetCharacter, targetAgentIndex, hasAnyHero, sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates);
    }

	private bool CanInjectSceneMechanismTopicIntoPreprocess(List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, NpcDataPacket sceneSpeakerNpc, List<NpcDataPacket> sceneCandidates)
    {
        return SceneMechanismPromptCapture.CanInjectSceneMechanismTopicIntoPreprocess(sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates);
    }

	private static bool ShouldExcludeWorldMapCommandTopicForPreprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
    {
        return SceneMechanismPromptCaptureAdapter.ShouldExcludeWorldMapCommandTopicForPreprocess(targetHero, targetCharacter, targetAgentIndex);
    }

	private Task<string> SubmitNativeConversationTextInternalAsync(NativeConversationAdmission admission, string playerText, Action<string> onStreamText = null, string currentDialogTextOverride = null, Action<string> onPostprocessStarted = null, Action<string, Hero, CharacterObject> onMainReplyReady = null, bool npcInitiatedOpening = false)
	{
		return NativeConversationTurnCoordinator.RunAsync(new NativeConversationTurnRuntime(CreateNativeConversationTurnPorts(), admission,
			playerText, onStreamText, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening));
	}

	internal static void SubmitNativeConversationSceneActionObservation(string replyText, int agentIndex)
	{
        ConversationActionBoundaryBannerlordAdapter.SubmitNativeConversationSceneActionObservation(replyText,agentIndex);
    }

	internal static bool IsNativeConversationNoSpeechPlaceholder(string text)
	{
		return ConversationActionPostprocessOwner.IsNativeConversationNoSpeechPlaceholder(text);
	}

	private const int NativeConversationPersonaGenerationWaitTimeoutMs = 180000;

	private const int NativeConversationMainReplyTimeoutMs = 180000;

	internal static Task<string> CallNativeConversationApiAsync(List<object> messages, Action<string> onStreamText, CancellationToken cancellationToken = default(CancellationToken))
	{
		return NativeConversationLlmApplicationAdapter.CallNativeConversationApiAsync(messages, onStreamText, LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions(), cancellationToken);
	}

	private static string BuildNativeConversationStreamingVisibleText(string text)
	{
		return ScenePromptMessageProjectionComposer.BuildNativeConversationStreamingVisibleText(text, LlmRequestConfigurationCaptureAdapter.CaptureSceneSpeechTextOptions());
	}

public void TriggerShout() => _j17SceneShoutInputController.TriggerShout();

private void OpenBattleSpeechFromShoutMenu(
		bool npcSpeech,
		Agent primaryTarget,
		IReadOnlyList<Agent> framedTargets,
		int conversationEpoch) => _j17SceneShoutInputController.OpenBattleSpeechFromShoutMenu(npcSpeech, primaryTarget, framedTargets, conversationEpoch);

private static bool IsPlayerSideBattleSpeechTarget(
		Mission mission,
		Agent player,
		Agent primaryTarget) => SceneShoutInputController.IsPlayerSideBattleSpeechTarget(mission, player, primaryTarget);

private void TriggerShoutDirectInput() => _j17SceneShoutInputController.TriggerShoutDirectInput();

private bool TryPrepareShoutTarget(out NpcDataPacket primaryDataPacket) => _j17SceneShoutInputController.TryPrepareShoutTarget(out primaryDataPacket);

private void OpenShoutTextInput(NpcDataPacket primaryDataPacket, string preface, string extraFact) => _j17SceneShoutInputController.OpenShoutTextInput(primaryDataPacket, preface, extraFact);

private void OpenShoutTagTestInput(NpcDataPacket primaryDataPacket) => _j17SceneShoutInputController.OpenShoutTagTestInput(primaryDataPacket);

	private void OnShoutTagTestConfirmed(string input, int? forcedPrimaryAgentIndex)
	{
        DeveloperActions.OnShoutTagTestConfirmed(input,forcedPrimaryAgentIndex);
    }

private Action BuildShoutTargetEncyclopediaAction(NpcDataPacket targetNpc) => _j17SceneShoutInputController.BuildShoutTargetEncyclopediaAction(targetNpc);

internal static void OpenHeroEncyclopediaFromShoutInput(Hero hero) => SceneShoutInputController.OpenHeroEncyclopediaFromShoutInput(hero);

internal static bool IsShoutTradeShowMode(ShoutChatMode mode) => SceneTradeController.IsShoutTradeShowMode(mode);

internal static bool IsShoutPartyTransferMode(ShoutChatMode mode) => SceneTradeController.IsShoutPartyTransferMode(mode);

internal static bool IsShoutSettlementTransferMode(ShoutChatMode mode) => SceneTradeController.IsShoutSettlementTransferMode(mode);

internal static bool IsShoutTroopTransferMode(ShoutChatMode mode) => SceneTradeController.IsShoutTroopTransferMode(mode);

private static bool IsShoutPrisonerTransferMode(ShoutChatMode mode) => SceneTradeController.IsShoutPrisonerTransferMode(mode);

private static bool IsShoutTradeGiveMode(ShoutChatMode mode) => SceneTradeController.IsShoutTradeGiveMode(mode);

private void ResolveShoutTradeRuntimeTarget(out Hero hero, out CharacterObject characterObject, out Agent agent) => _j17SceneTradeBannerlordAdapter.ResolveShoutTradeRuntimeTarget(out hero, out characterObject, out agent);

private bool IsShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false) => _j17SceneTradeBannerlordAdapter.IsShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame);

private bool EnsureShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false) => _j17SceneTradeBannerlordAdapter.EnsureShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame);

private int GetShoutTradeTargetAgentIndex() => _j17SceneTradeController.GetShoutTradeTargetAgentIndex();

private void BeginShoutTradeFlow(NpcDataPacket targetNpc, ShoutChatMode mode) => _j17SceneTradeController.BeginShoutTradeFlow(targetNpc, mode);

private List<ShoutTradeResourceOption> BuildShoutTradeOptions() => _j17SceneTradeBannerlordAdapter.BuildShoutTradeOptions();

private void OnShoutTradeResourcesSelected(List<InquiryElement> selectedElements) => _j17SceneTradeController.OnShoutTradeResourcesSelected(selectedElements);

private void ShowShoutTradeAmountInquiry() => _j17SceneTradeController.ShowShoutTradeAmountInquiry();

private void ShowShoutTradeChatInput() => _j17SceneTradeController.ShowShoutTradeChatInput();

private void OnShoutTradeChatConfirmed(string input) => _j17SceneTradeController.OnShoutTradeChatConfirmed(input);

private void CommitShoutTradeActionOnly() => _j17SceneTradeController.CommitShoutTradeActionOnly();

private void RecordNativeConversationTradeActionFact(string fact) => _j17SceneTradeBannerlordAdapter.RecordNativeConversationTradeActionFact(fact);

private string AppendShoutTradeActionFactSequence(string fact) => _j17SceneTradeController.AppendShoutTradeActionFactSequence(fact);

private void FinishShoutTradeActionOnlyIfNeeded() => _j17SceneTradeController.FinishShoutTradeActionOnlyIfNeeded();

private void ApplyShoutGiveTransfer() => _j17SceneTradeBannerlordAdapter.ApplyShoutGiveTransfer();

	private static int GetCurrentCampaignDaySafe() => SceneRevisitRecordGameAdapter.GetCurrentCampaignDaySafe();

	private static string GetCurrentSettlementIdSafe() => SceneRevisitRecordGameAdapter.GetCurrentSettlementIdSafe();

	private static string NormalizeSceneRevisitKeyToken(string text) => SceneRevisitRecordGameAdapter.NormalizeSceneRevisitKeyToken(text);

	private static string BuildCurrentSceneRevisitKeySafe() => SceneRevisitRecordGameAdapter.BuildCurrentSceneRevisitKeySafe(ParseCurrentScenePlaceAndSpotForPrompt);

	private static string BuildSceneHeroRevisitRecordKey(Hero hero, string sceneKey) => SceneRevisitRecordGameAdapter.BuildSceneHeroRevisitRecordKey(hero, sceneKey);

	// Same-day revisits should not read as "0天前"; use a dedicated sentence so the prompt stays natural.
	private static string BuildSceneRevisitFactBody(string playerDisplayName, int elapsedDays) => SceneRevisitRecordGameAdapter.BuildSceneRevisitFactBody(playerDisplayName, elapsedDays);

	private void TryInjectSceneFirstMeetingFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData) => _sceneRevisitRecords.TryInjectSceneFirstMeetingFactsBeforePlayerMessage(nearbyData);

	// Inject the scene revisit AFEF before the player's new line is written, otherwise Token_Stats will place it below the player utterance.
	private void TryInjectSceneRevisitFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData) => _sceneRevisitRecords.TryInjectSceneRevisitFactsBeforePlayerMessage(nearbyData);

	private void RecordScenePrepaidTransfer(string targetKey, int goldAmount) => PrepaidTransferRecords.RecordScenePrepaidTransfer(targetKey, goldAmount);

	private void RecordNegotiatedNonHeroBribe(string targetKey, int goldAmount) => PrepaidTransferRecords.RecordNegotiatedNonHeroBribe(targetKey, goldAmount);

	internal int GetRecentNonHeroGoldForRuleTarget(string targetKey) => PrepaidTransferRecords.GetRecentNonHeroGoldForRuleTarget(targetKey);

	internal int GetNegotiatedNonHeroBribeForRuleTarget(string targetKey) => PrepaidTransferRecords.GetNegotiatedNonHeroBribeForRuleTarget(targetKey);

	internal void ConsumeRecentNonHeroGoldForRuleTarget(string targetKey, int goldAmount) => PrepaidTransferRecords.ConsumeRecentNonHeroGoldForRuleTarget(targetKey, goldAmount);

private bool TryCaptureNegotiatedLordsHallBribe(NpcDataPacket npc, Agent agent, ref string content) => _j17SceneSpeechFollowupController.TryCaptureNegotiatedLordsHallBribe(npc, agent, ref content);

private static bool IsLordsHallGuardAgent(Agent agent) => SceneSpeechFollowupController.IsLordsHallGuardAgent(agent);

private bool TryUnlockLordsHallForNpc(NpcDataPacket npc, Agent agent) => _j17SceneSpeechFollowupController.TryUnlockLordsHallForNpc(npc, agent);

private bool TryTriggerOpenLordsHallAction(NpcDataPacket npc, Agent agent, ref string content) => _j17SceneSpeechFollowupController.TryTriggerOpenLordsHallAction(npc, agent, ref content);

private string BuildShoutTradeFactText(bool isGive) => _j17SceneTradeBannerlordAdapter.BuildShoutTradeFactText(isGive);

private string BuildShoutPartyTransferFactText() => _j17SceneTradeBannerlordAdapter.BuildShoutPartyTransferFactText();

private long EstimateShoutPendingShowTotalValue() => _j17SceneTradeBannerlordAdapter.EstimateShoutPendingShowTotalValue();

private void ShowShoutPendingDisplayValueMessage(long totalValue) => _j17SceneTradeBannerlordAdapter.ShowShoutPendingDisplayValueMessage(totalValue);

internal static string GetPlayerDisplayNameForShout() => SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout();

private string GetShoutTradeTargetDisplayName() => _j17SceneTradeBannerlordAdapter.GetShoutTradeTargetDisplayName();

	private static string BuildSingleNpcSceneReplyInstruction(string npcName, bool hasMultiplePresentNpcs)
{
    return ScenePromptMessageProjectionComposer.BuildSingleNpcSceneReplyInstruction(npcName, hasMultiplePresentNpcs, PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal());
}

	internal static bool HasInjectedRuleBlockForPostprocess(string instructions, string ruleId)
	{
        return ScenePromptMessageProjectionComposer.HasInjectedRuleBlockForPostprocess(instructions, ruleId);
    }

	public static bool HasInjectedRuleBlockForExternal(string instructions, string ruleId)
	{
		return HasInjectedRuleBlockForPostprocess(instructions, ruleId);
	}

	internal static void MarkWeeklyMemoryMaterialTriggerForScene(Hero targetHero, CharacterObject targetCharacter, string npcName, string normalizedTags, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions, List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions, bool forceLooseSession = false, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null) => MemoryHistoryCommitBannerlordAdapter.MarkWeeklyMemoryMaterialTriggerForScene(targetHero, targetCharacter, npcName, normalizedTags, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, forceLooseSession, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);

	internal static bool HasPreprocessRuleHit(IEnumerable<string> ruleIds, string ruleId)
	{
		return ConversationActionPostprocessOwner.HasPreprocessRuleHit(ruleIds, ruleId);
	}

	internal static bool HasPreprocessRuleHitForExternal(IEnumerable<string> ruleIds, string ruleId)
	{
		return HasPreprocessRuleHit(ruleIds, ruleId);
	}

	internal static string ResolveCourierRuntimeTargetKingdomId(Hero targetHero, CharacterObject targetCharacter)
	{
		return ConversationActionPostprocessOwner.ResolveCourierRuntimeTargetKingdomId(targetHero, targetCharacter);
	}

	internal sealed class CourierActionPostprocessWorkItem
    {
        private readonly ConversationCourierPostprocessWorkItem _prepared;
        internal CourierActionPostprocessWorkItem(ConversationCourierPostprocessWorkItem prepared) { _prepared=prepared; }
        internal string SystemPrompt => _prepared.SystemPrompt;
        internal string UserPrompt => _prepared.UserPrompt;
        internal string FallbackText => _prepared.FallbackText;
        internal string CompleteOnMainThread(string content) => _prepared.CompleteOnMainThread(content);
    }

	public static string RunCourierActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected = false, bool diplomacyRuleInjected = false, bool worldMapPartyCommandRuleInjected = false, List<string> preprocessRuleHits = null, string entityPostprocessContext = null, int targetAgentIndex = -1, bool latestReplyHasPlayerInput = true, bool forceLooseWeeklyMemoryMaterialSession = false, bool kingdomVassalageRuleInjected = false, bool kingdomAnnexationRuleInjected = false, string chainName = null)
	{
        return ConversationActionExecutorComposition.RunCourierActionPostprocessForExternal(targetHero,targetCharacter,npcName,playerText,historyText,replyText,duelRuleInjected,rewardRuleInjected,loanRuleInjected,kingdomServiceRuleInjected,lordsHallRuleInjected,meetingReleaseRuleInjected,vanillaIssueRuleInjected,heroJoinPartyRuleInjected,sceneMechanismRuleInjected,partyTransferRuleInjected,voteDealRuleInjected,diplomacyRuleInjected,worldMapPartyCommandRuleInjected,preprocessRuleHits,entityPostprocessContext,targetAgentIndex,latestReplyHasPlayerInput,forceLooseWeeklyMemoryMaterialSession,kingdomVassalageRuleInjected,kingdomAnnexationRuleInjected,chainName);
    }

	internal static bool TryPrepareCourierActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, out CourierActionPostprocessWorkItem workItem, out string immediateResult, bool voteDealRuleInjected = false, bool diplomacyRuleInjected = false, bool worldMapPartyCommandRuleInjected = false, List<string> preprocessRuleHits = null, string entityPostprocessContext = null, int targetAgentIndex = -1, bool latestReplyHasPlayerInput = true, bool forceLooseWeeklyMemoryMaterialSession = false, bool kingdomVassalageRuleInjected = false, bool kingdomAnnexationRuleInjected = false, string chainName = null)
	{
		bool ready = ConversationActionPostprocessOwner.TryPrepareCourierActionPostprocessForExternal(targetHero, targetCharacter, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, out var prepared, out immediateResult, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, preprocessRuleHits, entityPostprocessContext, targetAgentIndex, latestReplyHasPlayerInput, forceLooseWeeklyMemoryMaterialSession, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, chainName);
		workItem = ready ? new CourierActionPostprocessWorkItem(prepared) : null;
		return ready;
	}

	private static List<PostprocessRuleEntry> BuildPersistentAdpDebtPostprocessRules()
	{
		return ConversationActionPostprocessOwner.BuildPersistentAdpDebtPostprocessRules();
	}

	private static string KeepOnlyPersistentAdpDebtTags(string tags)
	{
		return ConversationActionPostprocessOwner.KeepOnlyPersistentAdpDebtTags(tags);
	}

	internal static bool CanInjectDuelPostprocessRule(MyBehavior.ShoutPromptContext ctx, Hero targetHero, int targetAgentIndex, string playerText, out string reason)
	{
        return SceneMechanismPromptCaptureAdapter.CanInjectDuelPostprocessRule(ctx, targetHero, targetAgentIndex, playerText, out reason);
    }

	internal static string BuildPostprocessRuleTextForScene(IEnumerable<PostprocessRuleEntry> entries)
	{
		return ConversationActionPostprocessOwner.BuildPostprocessRuleTextForScene(entries);
	}

	internal static bool IsNpcSurrenderPostprocessContext()
	{
        return SceneMechanismPromptCaptureAdapter.IsNpcSurrenderPostprocessContext();
    }

	private static bool IsNativeConversationPostprocessChain(string chainName)
	{
		return ConversationActionPostprocessOwner.IsNativeConversationPostprocessChain(chainName);
	}

	private static bool IsActiveSiegeSettlementForSurrender(Settlement settlement)
	{
        return SceneMechanismPromptCaptureAdapter.IsActiveSiegeSettlementForSurrender(settlement);
    }

	private static Settlement ResolveSiegeEventSettlementForSurrender(SiegeEvent siegeEvent)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveSiegeEventSettlementForSurrender(siegeEvent);
    }

	private static void AddSiegeSettlementCandidate(List<Settlement> settlements, Settlement settlement)
	{
        SceneMechanismPromptCaptureAdapter.AddSiegeSettlementCandidate(settlements, settlement);
    }

	private static void AddSiegePartyCandidate(List<PartyBase> parties, PartyBase party)
	{
        SceneMechanismPromptCaptureAdapter.AddSiegePartyCandidate(parties, party);
    }

	private static PartyBase ResolveNativeConversationAgentPartyForSiegeSurrender(int targetAgentIndex)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveNativeConversationAgentPartyForSiegeSurrender(targetAgentIndex);
    }

	private static List<PartyBase> ResolveNativeConversationPartiesForSiegeSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveNativeConversationPartiesForSiegeSurrender(targetHero, targetCharacter, targetAgentIndex);
    }

	private static Settlement ResolveActiveSiegeSettlementForNativeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveActiveSiegeSettlementForNativeConversation(targetHero, targetCharacter, targetAgentIndex);
    }

	private static IFaction ResolveSiegeSurrenderTargetFaction(Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveSiegeSurrenderTargetFaction(targetHero, targetCharacter, parties);
    }

	private static IFaction ResolveSiegeAttackerFaction(Settlement settlement)
	{
        return SceneMechanismPromptCaptureAdapter.ResolveSiegeAttackerFaction(settlement);
    }

	private static bool IsSiegeAttackerPartyForSurrender(Settlement settlement, PartyBase party)
	{
        return SceneMechanismPromptCaptureAdapter.IsSiegeAttackerPartyForSurrender(settlement, party);
    }

	private static bool IsSiegeDefenderPartyForSurrender(Settlement settlement, PartyBase party)
	{
        return SceneMechanismPromptCaptureAdapter.IsSiegeDefenderPartyForSurrender(settlement, party);
    }

	private static bool TryResolveSiegeSurrenderSide(Settlement settlement, Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties, out BattleSideEnum side)
	{
        return SceneMechanismPromptCaptureAdapter.TryResolveSiegeSurrenderSide(settlement, targetHero, targetCharacter, parties, out side);
    }

	internal static bool TryResolveNativeConversationSiegeSurrenderContext(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out Settlement settlement, out BattleSideEnum targetSide, out string sideLabel)
	{
        return SceneMechanismPromptCaptureAdapter.TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out settlement, out targetSide, out sideLabel);
    }

	private static List<PostprocessRuleEntry> BuildSiegeSurrenderPostprocessRulesForNativeConversation(bool enabled, Settlement settlement, BattleSideEnum targetSide)
	{
		return ConversationActionPostprocessOwner.BuildSiegeSurrenderPostprocessRulesForNativeConversation(enabled, settlement, targetSide);
	}

	private static string BuildSiegeSurrenderPostprocessContextForNativeConversation(Settlement settlement, string sideLabel)
	{
		return ConversationActionPostprocessOwner.BuildSiegeSurrenderPostprocessContextForNativeConversation(settlement, sideLabel);
	}

	private static List<PostprocessRuleEntry> BuildNpcSurrenderPostprocessRulesForScene(bool enabled)
	{
		return ConversationActionPostprocessOwner.BuildNpcSurrenderPostprocessRulesForScene(enabled);
	}

	private static List<PostprocessRuleEntry> BuildAutoGroupRelayPostprocessRulesForScene(bool enabled, bool useSingleFramedNpcDescription)
	{
		return ConversationActionPostprocessOwner.BuildAutoGroupRelayPostprocessRulesForScene(enabled, useSingleFramedNpcDescription);
	}

	private static bool HasNpcSurrenderPostprocessRule(List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.HasNpcSurrenderPostprocessRule(rules);
	}

	private static bool HasSiegeSurrenderPostprocessRule(List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.HasSiegeSurrenderPostprocessRule(rules);
	}

	private static string BuildDuelPostprocessItemListForScene(List<RewardSystemBehavior.DuelStakeOption> options)
	{
		return ConversationActionPostprocessOwner.BuildDuelPostprocessItemListForScene(options);
	}

	private static RewardSystemBehavior.DuelStakeOption FindDuelStakeOptionByTokenForScene(List<RewardSystemBehavior.DuelStakeOption> options, string token)
	{
		return ConversationActionPostprocessOwner.FindDuelStakeOptionByTokenForScene(options, token);
	}

	private static string TranslateDuelStakeItemIndexesForScene(string text, List<RewardSystemBehavior.DuelStakeOption> options)
	{
		return ConversationActionPostprocessOwner.TranslateDuelStakeItemIndexesForScene(text, options);
	}

	private static string NormalizeDuelPostprocessTagsForScene(string raw, List<RewardSystemBehavior.DuelStakeOption> options, Hero targetHero = null)
	{
		return ConversationActionPostprocessOwner.NormalizeDuelPostprocessTagsForScene(raw, options, targetHero);
	}

	private static string BuildRewardPostprocessItemListForScene(List<RewardSystemBehavior.RewardItemInfo> options, int gold, List<RewardSystemBehavior.RewardItemInfo> allOptions = null)
	{
		return ConversationActionPostprocessOwner.BuildRewardPostprocessItemListForScene(options, gold, allOptions);
	}

	private static bool RequestsAllOrdinaryAssetsForPostprocess(string playerText, string historyText)
	{
		return ConversationActionPostprocessOwner.RequestsAllOrdinaryAssetsForPostprocess(playerText, historyText);
	}

	private static bool RequestsAllFixedAssetsForPostprocess(string playerText, string historyText)
	{
		return ConversationActionPostprocessOwner.RequestsAllFixedAssetsForPostprocess(playerText, historyText);
	}

	private static RewardSystemBehavior.RewardItemInfo FindRewardItemByTokenForScene(List<RewardSystemBehavior.RewardItemInfo> options, string token)
	{
		return ConversationActionPostprocessOwner.FindRewardItemByTokenForScene(options, token);
	}

	private static string TranslateRewardItemIndexesForScene(string text, List<RewardSystemBehavior.RewardItemInfo> options)
	{
		return ConversationActionPostprocessOwner.TranslateRewardItemIndexesForScene(text, options);
	}

	private static string NormalizeRewardPostprocessTagsForScene(string raw, List<RewardSystemBehavior.RewardItemInfo> options, List<RewardSystemBehavior.RewardItemInfo> allOptions, List<MyBehavior.SettlementTransferPromptEntry> fixedAssetOptions, bool allowAssetTransfer, int availableGold)
	{
		return ConversationActionPostprocessOwner.NormalizeRewardPostprocessTagsForScene(raw, options, allOptions, fixedAssetOptions, allowAssetTransfer, availableGold);
	}

	internal static int ResolvePartyTransferRecruitMaxTierForScene(Hero targetHero, CharacterObject targetCharacter)
	{
        return SceneMechanismPromptCaptureAdapter.ResolvePartyTransferRecruitMaxTierForScene(targetHero, targetCharacter);
    }

	private static int GetScenePartyTransferTroopTier(MyBehavior.PartyTransferPromptEntry entry)
	{
		return ConversationActionPostprocessOwner.GetScenePartyTransferTroopTier(entry);
	}

	private static List<MyBehavior.PartyTransferPromptEntry> BuildDisplayIndexedPartyTransferEntriesForScene(IEnumerable<MyBehavior.PartyTransferPromptEntry> entries)
	{
		return ConversationActionPostprocessOwner.BuildDisplayIndexedPartyTransferEntriesForScene(entries);
	}

	private static string BuildCompactPartyTransferEntryTextForScene(MyBehavior.PartyTransferPromptEntry entry, bool isPrisoner)
	{
		return ConversationActionPostprocessOwner.BuildCompactPartyTransferEntryTextForScene(entry, isPrisoner);
	}

	private static void AppendCompactPartyTransferPostprocessSectionForScene(StringBuilder sb, string header, IEnumerable<MyBehavior.PartyTransferPromptEntry> entries, bool isPrisoner)
	{
		ConversationActionPostprocessOwner.AppendCompactPartyTransferPostprocessSectionForScene(sb, header, entries, isPrisoner);
	}

	private static string BuildPartyTransferNpcPostprocessListForScene(List<MyBehavior.PartyTransferPromptEntry> troopOptions, List<MyBehavior.PartyTransferPromptEntry> prisonerOptions, List<MyBehavior.PartyTransferPromptEntry> allTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> allPrisonerOptions = null)
	{
		return ConversationActionPostprocessOwner.BuildPartyTransferNpcPostprocessListForScene(troopOptions, prisonerOptions, allTroopOptions, allPrisonerOptions);
	}

	private static List<MyBehavior.SettlementTransferPromptEntry> BuildDisplayIndexedSettlementTransferEntriesForScene(IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries)
	{
		return ConversationActionPostprocessOwner.BuildDisplayIndexedSettlementTransferEntriesForScene(entries);
	}

	private static void AppendCompactSettlementTransferPostprocessSectionForScene(StringBuilder sb, string header, IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries)
	{
		ConversationActionPostprocessOwner.AppendCompactSettlementTransferPostprocessSectionForScene(sb, header, entries);
	}

	private static string BuildSettlementTransferPostprocessListForScene(List<MyBehavior.SettlementTransferPromptEntry> npcOptions, List<MyBehavior.SettlementTransferPromptEntry> allNpcOptions = null)
	{
		return ConversationActionPostprocessOwner.BuildSettlementTransferPostprocessListForScene(npcOptions, allNpcOptions);
	}

	internal static string AppendPostprocessContextBlockForScene(string current, string block)
	{
		return ConversationActionPostprocessOwner.AppendPostprocessContextBlockForScene(current, block);
	}

	private static MyBehavior.SettlementTransferPromptEntry FindSettlementTransferEntryByTokenForScene(List<MyBehavior.SettlementTransferPromptEntry> options, string token)
	{
		return ConversationActionPostprocessOwner.FindSettlementTransferEntryByTokenForScene(options, token);
	}

	private static string NormalizePartyTransferPostprocessTagsForScene(string raw, List<MyBehavior.PartyTransferPromptEntry> troopOptions, List<MyBehavior.PartyTransferPromptEntry> prisonerOptions, List<MyBehavior.PartyTransferPromptEntry> allTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> allPrisonerOptions = null)
	{
		return ConversationActionPostprocessOwner.NormalizePartyTransferPostprocessTagsForScene(raw, troopOptions, prisonerOptions, allTroopOptions, allPrisonerOptions);
	}

	private static string NormalizeCustomPolicyAgendaPostprocessTag(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeCustomPolicyAgendaPostprocessTag(raw, rules);
	}

	private static string NormalizeVoteDealPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeVoteDealPostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeDiplomacyPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeDiplomacyPostprocessTagsForScene(raw, rules);
	}

	private static string ExtractDiplomacyPostprocessActionName(string tag)
	{
		return ConversationActionPostprocessOwner.ExtractDiplomacyPostprocessActionName(tag);
	}

	private static string NormalizeWorldMapPartyCommandPostprocessTagsForScene(string raw)
	{
		return ConversationActionPostprocessOwner.NormalizeWorldMapPartyCommandPostprocessTagsForScene(raw);
	}

	internal static string NormalizeAutoGroupRelayPostprocessTagsForScene(string raw, List<NpcDataPacket> relayCandidates, int currentSpeakerAgentIndex)
	{
		return ConversationActionPostprocessOwner.NormalizeAutoGroupRelayPostprocessTagsForScene(raw, relayCandidates, currentSpeakerAgentIndex);
	}

	private static bool IsValidVoteDealPostprocessTag(string tag, HashSet<string> validTags)
	{
		return ConversationActionPostprocessOwner.IsValidVoteDealPostprocessTag(tag, validTags);
	}

	private static MyBehavior.PartyTransferPromptEntry FindPartyTransferEntryByTokenForScene(List<MyBehavior.PartyTransferPromptEntry> options, string token)
	{
		return ConversationActionPostprocessOwner.FindPartyTransferEntryByTokenForScene(options, token);
	}

	private static List<PostprocessRuleEntry> MergePostprocessRulesForScene(params List<PostprocessRuleEntry>[] ruleSets)
	{
		return ConversationActionPostprocessOwner.MergePostprocessRulesForScene(ruleSets);
	}

	private static PostprocessRuleEntry MergeSharedDeferredDebtPostprocessRule(PostprocessRuleEntry existing, PostprocessRuleEntry incoming)
	{
		return ConversationActionPostprocessOwner.MergeSharedDeferredDebtPostprocessRule(existing, incoming);
	}

	private static string MergePostprocessRuleDescription(string existingDescription, string incomingDescription)
	{
		return ConversationActionPostprocessOwner.MergePostprocessRuleDescription(existingDescription, incomingDescription);
	}

	private static List<PostprocessRuleEntry> BuildRuntimeDiplomacyPostprocessRulesForScene(Hero targetHero, CharacterObject targetCharacter)
	{
		return ConversationActionPostprocessOwner.BuildRuntimeDiplomacyPostprocessRulesForScene(targetHero, targetCharacter);
	}

	internal static string StripLordsHallAccessActionTagsForScene(string text)
	{
		return ConversationActionPostprocessOwner.StripLordsHallAccessActionTagsForScene(text);
	}

	internal static bool ContainsOpenLordsHallActionTag(string text)
	{
		return ConversationActionPostprocessOwner.ContainsOpenLordsHallActionTag(text);
	}

	internal static string StripSceneMechanismActionTagsForScene(string text)
	{
		return ConversationActionPostprocessOwner.StripSceneMechanismActionTagsForScene(text);
	}

	internal static string ExtractSceneMechanismActionTagsForScene(string text)
	{
		return ConversationActionPostprocessOwner.ExtractSceneMechanismActionTagsForScene(text);
	}

	private static string BuildSceneMechanismTargetListForPostprocess(List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
	{
		return ConversationActionPostprocessOwner.BuildSceneMechanismTargetListForPostprocess(CapturePostprocessSummonTargets(summonTargets), CapturePostprocessGuideTargets(guideTargets));
	}

	private List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForScene(NpcDataPacket speaker, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, bool includeGenericRules = true)
	{
		return SceneMechanismPromptCapture.BuildRuntimeSceneMechanismPostprocessRulesForScene(speaker, summonTargets, guideTargets, includeGenericRules);
	}

	private static void AddSceneMechanismPostprocessRule(List<PostprocessRuleEntry> target, HashSet<string> seenTags, string tag, string description)
	{
		SceneMechanismPromptCaptureAdapter.AddSceneMechanismPostprocessRule(target, seenTags, tag, description);
	}

	private static List<string> SelectPrimarySceneMechanismPostprocessActions(string raw, List<string> actionTags)
	{
		return ConversationActionPostprocessOwner.SelectPrimarySceneMechanismPostprocessActions(raw, actionTags);
	}

	private static string NormalizeSceneMechanismPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
	{
		return ConversationActionPostprocessOwner.NormalizeSceneMechanismPostprocessTagsForScene(raw, rules, CapturePostprocessSummonTargets(summonTargets), CapturePostprocessGuideTargets(guideTargets));
	}

	private static string NormalizeKingdomServicePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeKingdomServicePostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeVassalagePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeVassalagePostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeKingdomAnnexationPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return NormalizeVassalagePostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeLordsHallAccessPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeLordsHallAccessPostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeEncounterReleasePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeEncounterReleasePostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeNpcSurrenderPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, bool enabled)
	{
		return ConversationActionPostprocessOwner.NormalizeNpcSurrenderPostprocessTagsForScene(raw, rules, enabled);
	}

	private static string NormalizeSiegeSurrenderPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, bool enabled)
	{
		return ConversationActionPostprocessOwner.NormalizeSiegeSurrenderPostprocessTagsForScene(raw, rules, enabled);
	}

	private static string NormalizeVanillaIssuePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeVanillaIssuePostprocessTagsForScene(raw, rules);
	}

	private static string NormalizeHeroJoinPartyPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		return ConversationActionPostprocessOwner.NormalizeHeroJoinPartyPostprocessTagsForScene(raw, rules);
	}

	private static string MergeNormalizedPostprocessBlocksForScene(params string[] blocks)
	{
		return ConversationActionPostprocessOwner.MergeNormalizedPostprocessBlocksForScene(blocks);
	}



	internal static string BuildCompactTownAmbientRuleText(
		IEnumerable<PostprocessRuleEntry> entries,
		TownPromptTextCatalog text)
	{
        return SceneMechanismPromptCaptureAdapter.BuildCompactTownAmbientRuleText(entries, text);
    }

	internal static string EnsureScenePostprocessFallbackMood(string dialogue)
	{
        return SceneMechanismPromptCaptureAdapter.EnsureScenePostprocessFallbackMood(dialogue);
    }



	private static string NormalizePlayerNameForScenePostprocess(string text, string npcName = null)
	{
		return ConversationActionPostprocessOwner.NormalizePlayerNameForScenePostprocess(text, npcName);
	}

	private static string BuildSceneActionPostprocessUserPrompt(string userPromptTemplate, string tagRules, string npcName, string historyText, string latestReplyBlock, string sharedItemList = null, string playerItemList = null, string debtHint = null, string marriagePlayerCandidates = null, string marriageTargetCandidates = null, string runtimeContext = null)
	{
		return ConversationActionPostprocessOwner.BuildSceneActionPostprocessUserPrompt(userPromptTemplate, tagRules, npcName, historyText, latestReplyBlock, sharedItemList, playerItemList, debtHint, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
	}



	private static string InsertTaggedLatestReplyAfterScenePublicSection(string historyText, string taggedLatestReplyBlock)
	{
        return SceneMechanismPromptCaptureAdapter.InsertTaggedLatestReplyAfterScenePublicSection(historyText, taggedLatestReplyBlock);
    }

private long _sceneShoutProcessingSequence { get => _j17SceneShoutInputController._sceneShoutProcessingSequence; set => _j17SceneShoutInputController._sceneShoutProcessingSequence = value; }





	private void ResetSceneShoutRuntimeOnMissionEnd(string reason)
	{
        _sceneMovementController?.Reset();
		PublicExecutionOrderRuntime.Reset();
		ResetSceneAudioRequestOwnership();
		try
		{
			_sceneConversation.AdvanceConversationEpoch();
			Interlocked.Increment(ref SceneConversationHistoryOwner.SessionId);
			EndPresentationSession("mission_end:" + (reason ?? ""));
			RetireModuleSceneGroup("scene.stale_context");
			ForceClearScenePostprocessGate("mission_end:" + (reason ?? ""));
			_isWaitingForScenePostprocessGate = false;
			EndShoutProcessing("mission_end:" + (reason ?? ""));
			ClearQueuedSceneSpeech();
			DeactivateMultiSceneMovementSuppression();
			ClearPendingSceneConversationAttentionRelease();
			ResetStaringBehavior();
			_activeInteractionSessions.Clear();
			_pendingInteractionTimeoutArms.Clear();
			ClearPendingNativeSceneMechanismActions("reset_runtime:" + (reason ?? ""));
			ClearPendingNativeSceneTauntFight("reset_runtime:" + (reason ?? ""));
			_pendingLordsHallMissionEntryAfterSpeech = null;
			UnregisterPendingLordsHallMissionEntryConversationEndHook();
			ResetPendingMainThreadFunctions();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] ResetSceneShoutRuntimeOnMissionEnd failed: " + ex.Message);
		}
	}

	private bool TryApplyDeferredSceneMoodTag(NpcDataPacket speaker, string tags)
	{
        return ActionBoundary.TryApplyDeferredSceneMoodTag(speaker,tags);
    }

	private bool TryConsumeSceneNpcSurrenderTag(NpcDataPacket speaker, ref string content, out Hero targetHero, out CharacterObject targetCharacter, out int targetAgentIndex)
	{
        return ActionBoundary.TryConsumeSceneNpcSurrenderTag(speaker,ref content,out targetHero,out targetCharacter,out targetAgentIndex);
    }




private void RecordShoutShownResources() => _j17SceneTradeBannerlordAdapter.RecordShoutShownResources();

private string ResolveShownTradeTargetKey(NpcDataPacket targetNpc, out Hero hero) => _j17SceneTradeBannerlordAdapter.ResolveShownTradeTargetKey(targetNpc, out hero);

private static string AppendSceneAgentIdentityToShownTargetKey(string targetKey, int agentIndex) => SceneTradeBannerlordAdapter.AppendSceneAgentIdentityToShownTargetKey(targetKey, agentIndex);

private static string ResolveShownTradeNonHeroScope(int agentIndex) => SceneTradeBannerlordAdapter.ResolveShownTradeNonHeroScope(agentIndex);

private void ResetShoutTradeState() => _j17SceneTradeController.ResetShoutTradeState();

private void OnShoutConfirmed(string shoutText) => _j17SceneShoutInputController.OnShoutConfirmed(shoutText);

private void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _j17SceneShoutInputController.OnShoutConfirmedWithContext(shoutText, extraFact, forcedPrimaryAgentIndex);

	// The opaque request is transient, never persisted, and never installed as the UI's mutable context.
internal object CaptureScenePlayerShoutRequestForReplay(Agent[] framedTargets, Agent primaryTarget) => _j17SceneShoutInputController.CaptureScenePlayerShoutRequestForReplay(framedTargets, primaryTarget);

internal bool IsCapturedScenePlayerShoutRequestCurrent(object capturedRequest) => _j17SceneShoutInputController.IsCapturedScenePlayerShoutRequestCurrent(capturedRequest);





internal bool TryReplayCapturedScenePlayerShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		object capturedRequest, Action<Action> runWithObservationScope) => _j17SceneShoutInputController.TryReplayCapturedScenePlayerShout(shoutText, extraFact, forcedPrimaryAgentIndex, capturedRequest, runWithObservationScope);

private Task ProcessShoutConfirmedInternal(string shoutText, string extraFact, int? forcedPrimaryAgentIndex) => _j17SceneShoutInputController.ProcessShoutConfirmedInternal(shoutText, extraFact, forcedPrimaryAgentIndex);



	internal static List<NpcDataPacket> BuildGroupSpeakingCandidates(List<NpcDataPacket> allNpcData, NpcDataPacket primaryNpc)
	{
		return SceneRosterPromptCaptureAdapter.BuildGroupSpeakingCandidates(allNpcData, primaryNpc);
	}

private void HoldSceneConversationParticipants(List<NpcDataPacket> participants) => _j17SceneAttentionController.HoldSceneConversationParticipants(participants);

private void HoldSceneConversationAgents(List<Agent> participants) => _j17SceneAttentionController.HoldSceneConversationAgents(participants);

	internal static NpcDataPacket CloneNpcDataPacket(NpcDataPacket npc)
	{
		return SceneRosterPromptCaptureAdapter.CloneNpcDataPacket(npc);
	}

	internal static List<NpcDataPacket> CloneNpcDataSnapshot(List<NpcDataPacket> allNpcData)
	{
		return SceneRosterPromptCaptureAdapter.CloneNpcDataSnapshot(allNpcData);
	}

	internal static void ApplySceneLocalDisambiguatedNames(List<NpcDataPacket> allNpcData)
	{
		SceneRosterPromptCaptureAdapter.ApplySceneLocalDisambiguatedNames(allNpcData);
	}




	private static string NormalizeSceneExtraFactForHistory(string extraFact)
	{
		return ScenePromptMessageProjectionComposer.NormalizeSceneExtraFactForHistory(extraFact);
	}

	private void RecordExtraFactToSceneHistory(string extraFact, List<NpcDataPacket> nearbyData) => RecordExtraFactToSceneHistoryCaptured(extraFact, nearbyData);

	private void AppendNativeConversationSessionLineToSceneHistory(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, int targetAgentIndexOverride = -1, NpcDataPacket targetNpc = null, int playerTargetAgentIndexOverride = -1, string playerTargetNameOverride = null) => AppendNativeConversationSessionLineToSceneHistoryCaptured(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, targetAgentIndexOverride, targetNpc, playerTargetAgentIndexOverride, playerTargetNameOverride);

	private void RemoveNativeConversationSessionHistoryEventFromSceneHistory(long eventSequence) => RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(eventSequence);

	private void ClearPendingCurrentAfefFacts()
	{
		_scenePendingAfefFactsOwner.Clear();
		_nativeSessionOwner.ClearPendingFacts();
	}

	private NpcDataPacket ResolveSceneNpcDataForSharedHistory(int agentIndex, string fallbackName = "")
    {
        return SceneHistoryPromptCapture.ResolveSceneNpcDataForSharedHistory(agentIndex, fallbackName);
    }

	private void AppendSceneEventToNativeSharedHistory(NpcDataPacket targetNpc, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Agent targetAgentOverride = null)
    {
        SceneHistoryPromptCapture.AppendSceneEventToNativeSharedHistory(targetNpc, speaker, text, kind, eventSequence, playerTargetAgentIndex, playerTargetName, targetAgentOverride);
    }

	private void AppendSceneEventToNativeSharedHistoryForTargets(IEnumerable<NpcDataPacket> targets, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Dictionary<int, Agent> audienceAgentsByIndex = null)
    {
        SceneHistoryPromptCapture.AppendSceneEventToNativeSharedHistoryForTargets(targets, speaker, text, kind, eventSequence, playerTargetAgentIndex, playerTargetName, audienceAgentsByIndex);
    }

	private void QueuePendingCurrentNativeAfefFactForSceneTarget(NpcDataPacket targetNpc, string fact)
    {
        SceneHistoryPromptCapture.QueuePendingCurrentNativeAfefFactForSceneTarget(targetNpc, fact);
    }

	private void QueuePendingCurrentAfefFactForAgent(int targetAgentIndex, string fact)
	{
        SceneHistoryPromptCapture.QueuePendingCurrentAfefFactForAgent(targetAgentIndex, fact);
    }

	private List<ConversationMessage> ConsumePendingCurrentAfefFactMessagesForPrompt(int targetAgentIndex)
	{
		return _scenePendingAfefFactsOwner.Consume(targetAgentIndex);
	}

	private void QueuePendingCurrentNativeAfefFactForKey(string key, string fact)
	{
        SceneHistoryPromptCapture.QueuePendingCurrentNativeAfefFactForKey(key, fact);
    }

	private List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(string key)
	{
        return SceneHistoryPromptCapture.ConsumePendingCurrentNativeAfefFactMessagesForPrompt(key);
    }

	private void AppendActionAfefFactToSceneHistoryInOrder(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true) => AppendActionAfefFactToSceneHistoryInOrderCaptured(targetAgentIndex, fact, mirrorToNativeSharedHistory);

	private bool PersistExtraFactToNamedHeroes(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1,
		bool requireMemoryReceipt = false)
	{
        return SceneHistoryPromptCapture.PersistExtraFactToNamedHeroes(extraFact, nearbyData, personalizedAgentIndex, requireMemoryReceipt);
    }

	private bool PersistExtraFactToNamedObserver(
		NpcDataPacket observer,
		string extraFact,
		HashSet<string> persistedNonHeroIds,
		bool requireMemoryReceipt)
	{
        return SceneHistoryPromptCapture.PersistExtraFactToNamedObserver(observer, extraFact, persistedNonHeroIds, requireMemoryReceipt);
    }

	private bool RecordPlayerMessage(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false) => RecordPlayerMessageCaptured(text, nearbyData, primaryTargetAgentIndex, primaryTargetName, audienceAgentsByIndex, requireMemoryReceipt);

	internal static bool ShouldDeferExtraFactPersistenceUntilAfterSceneReply(string extraFact)
	{
        return SceneHistoryPromptCaptureAdapter.ShouldDeferExtraFactPersistenceUntilAfterSceneReply(extraFact);
    }

	internal static bool ContainsPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return ScenePromptMessageProjectionComposer.ContainsPlayerCraftedAfefInspectionSuffix(extraFact);
	}

	internal static string StripPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return ScenePromptMessageProjectionComposer.StripPlayerCraftedAfefInspectionSuffix(extraFact);
	}

	private void SetPendingHeroHistoryExtraFactAfterSceneReply(string extraFact, List<NpcDataPacket> nearbyData, int personalizedAgentIndex = -1) => _deferredHistoryFacts.Set(extraFact, nearbyData, personalizedAgentIndex);

	private void PromotePersonalizedExtraFactInScenePrivateHistory(
		string personalizedExtraFact,
		int personalizedAgentIndex) => PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(personalizedExtraFact, personalizedAgentIndex);

	private bool FlushPendingHeroHistoryExtraFactAfterSceneReply(bool requireMemoryReceipt = false) => _deferredHistoryFacts.Flush(requireMemoryReceipt);

	private static bool IsSingleUseSceneNpcFactText(string text) => SceneHistoryProjectionOwner.IsSingleUseSceneNpcFactText(text);

	private static bool IsSceneConversationTurn(ConversationMessage msg) => SceneHistoryProjectionOwner.IsSceneConversationTurn(msg);

	private static void RemoveExpiredSingleUseSceneNpcFacts(List<ConversationMessage> history) => SceneHistoryProjectionOwner.RemoveExpiredSingleUseSceneNpcFacts(history);

	private bool RecordResponseForAllNearbySafe(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false) => RecordResponseForAllNearbySafeCaptured(nearbyData, speakerAgentIndex, speakerName, response, requireMemoryReceipt);

	private void RecordSystemFactForNearbySafe(List<NpcDataPacket> nearbyData, string factText) => RecordSystemFactForNearbySafeCaptured(nearbyData, factText);

	internal static void BuildHeroPersonaFallback(Hero hero, out string personality, out string background)
	{
        AnimusForge.Refactor.Adapters.ScenePersonaPreparationAdapter.BuildHeroPersonaFallback(hero, out personality, out background);
    }

	private Hero ResolveHeroFromAgentIndex(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(agentIndex);
	}












	private NpcDataPacket BuildSceneNpcDataFromLocationCharacter(LocationCharacter locationCharacter)
	{
    return SceneRosterPromptCaptureAdapter.BuildSceneNpcDataFromLocationCharacter(locationCharacter);
}










































	internal static string NormalizeSceneMechanismTargetToken(string text)
	{
		return ConversationActionPostprocessOwner.NormalizeSceneMechanismTargetToken(text);
	}

	private static string NormalizeSceneMechanismTargetKey(string text)
	{
		return ConversationActionPostprocessOwner.NormalizeSceneMechanismTargetKey(text);
	}

	internal static List<string> ParseSceneMechanismTargetTokens(string rawValue)
	{
		return ConversationActionPostprocessOwner.ParseSceneMechanismTargetTokens(rawValue);
	}

	internal static T ResolveSceneMechanismTargetByToken<T>(IEnumerable<T> targets, string token, Func<T, int> getId, Func<T, string> getName) where T : class
	{
		return ConversationActionPostprocessOwner.ResolveSceneMechanismTargetByToken<T>(targets, token, getId, getName);
	}









































	public static bool TryForceSceneFollowPlayerForExternal(int targetAgentIndex, bool transient = true, string reason = null)
	{
		try
		{
			return CurrentInstance?._sceneMovement.TryForceSceneFollowPlayerInternal(targetAgentIndex, transient, reason) == true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneFollow", "external_force_start_failed agent=" + targetAgentIndex + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static bool TryForceStopSceneFollowForExternal(int targetAgentIndex, string reason = null)
	{
		try
		{
			return CurrentInstance?._sceneMovement.TryForceStopSceneFollowInternal(targetAgentIndex, reason) == true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneFollow", "external_force_stop_failed agent=" + targetAgentIndex + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static bool IsSceneFollowingPlayerForExternal(int targetAgentIndex)
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
			return agent != null && CurrentInstance?._sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent) == true;
		}
		catch
		{
			return false;
		}
	}





































	private static List<int> BuildVisibleAgentSnapshot(List<NpcDataPacket> nearbyData)
	{
        return SceneHistoryPromptCaptureAdapter.BuildVisibleAgentSnapshot(nearbyData);
    }

	private static string NormalizeSceneHeroId(string heroId) => SceneHistoryProjectionOwner.NormalizeSceneHeroId(heroId);

	private static string ResolveSceneHeroIdFromAgentIndex(int agentIndex)
	{
		return SceneAgentIdentityPromptCaptureAdapter.ResolveSceneHeroIdFromAgentIndex(agentIndex);
	}

	private static bool IsSameSceneHeroId(string left, string right) => SceneHistoryProjectionOwner.IsSameSceneHeroId(left, right);

	private static bool ContainsSceneHeroId(IEnumerable<string> heroIds, string heroId) => SceneHistoryProjectionOwner.ContainsSceneHeroId(heroIds, heroId);

	private static bool IsSceneHistoryVisibleToAgent(ConversationMessage msg, int viewerAgentIndex)
	{
		return IsSceneHistoryVisibleToAgentOrHero(msg, viewerAgentIndex, ResolveSceneHeroIdFromAgentIndex(viewerAgentIndex));
	}

	private static bool IsSceneHistoryVisibleToAgentOrHero(ConversationMessage msg, int viewerAgentIndex, string viewerHeroId) => SceneHistoryProjectionOwner.IsSceneHistoryVisibleToAgentOrHero(msg, viewerAgentIndex, viewerHeroId);

	internal static List<string> BuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName = "", bool useNpcNameAddress = false) => CaptureAndBuildVisibleSceneHistoryLines(history, viewerAgentIndex, targetNpcName, useNpcNameAddress);

	internal static object CreateChatMessage(string role, string content)
	{
		return MainPromptMessageAssemblyOwner.CreateCourierChatMessage(role, content);
	}

	private static string BuildStrictSceneMessagesSystemPrompt(string systemPrompt, bool suppressReplyFormatInstruction = false)
	{
        return SceneHistoryPromptCaptureAdapter.BuildStrictSceneMessagesSystemPrompt(systemPrompt, suppressReplyFormatInstruction);
    }
	private List<ConversationMessage> GetNpcConversationHistorySnapshot(int npcAgentIndex) => CaptureNpcConversationHistory(npcAgentIndex);

	private static bool IsSceneHistoryMessageRelatedToHero(ConversationMessage msg, string heroId) => SceneHistoryProjectionOwner.IsSceneHistoryMessageRelatedToHero(msg, heroId);

	private static bool DoesSceneHistoryBucketRelateToHero(IEnumerable<ConversationMessage> messages, string heroId) => SceneHistoryProjectionOwner.DoesSceneHistoryBucketRelateToHero(messages, heroId);

	private static string BuildSceneHistoryMergeKey(ConversationMessage msg) => SceneHistoryProjectionOwner.BuildSceneHistoryMergeKey(msg, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));


	private static void AppendStrictSceneUserSections(List<object> messages, IEnumerable<string> sections)
	{
		MainPromptMessageAssemblyOwner.AppendStrictSceneUserSections(messages, sections);
	}

	private static string GetStrictScenePlayerDisplayName()
	{
        return SceneHistoryPromptCaptureAdapter.GetStrictScenePlayerDisplayName();
    }


	private static float GetPlayerDistanceToAgentForScenePrompt(int agentIndex)
	{
        return SceneAgentIdentityPromptCaptureAdapter.GetPlayerDistanceToAgentForScenePrompt(agentIndex);
    }

	private static int ClampMemoryPromptHour(int hour)
	{
        return SceneHistoryPromptCaptureAdapter.ClampMemoryPromptHour(hour);
    }

	private static long NextConversationEventSequence()
	{
		return Interlocked.Increment(ref SceneConversationHistoryOwner.CurrentEventSequence);
	}

	private static void FillSceneMessageHeroIdentity(ConversationMessage message)
	{
		SceneAgentIdentityPromptCaptureAdapter.FillSceneMessageHeroIdentity(message);
	}

	private static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage message)
	{
        return SceneHistoryPromptCaptureAdapter.StampConversationMessageWithCurrentMemoryContext(message);
    }



	private static bool TryConvertAfefFactToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, bool isCurrent, out object chatMessage)
	{
        return SceneHistoryPromptCaptureAdapter.TryConvertAfefFactToStrictChatMessage(msg, npcAgentIndex, isCurrent, out chatMessage);
    }









	public static List<string> GetAuxiliarySceneDialogueHistoryLinesForExternal(int targetAgentIndex, int maxLines = 6)
	{
    return SceneHistoryPromptCaptureAdapter.GetAuxiliarySceneDialogueHistoryLinesForExternal(_nativeSessionOwner, CurrentSceneHistoryPromptCapture, targetAgentIndex, maxLines);
}

	private List<string> GetAuxiliarySceneDialogueHistoryLines(int targetAgentIndex, int maxLines) => CaptureAuxiliarySceneDialogueHistoryLines(targetAgentIndex, maxLines);

	private static bool IsAuxiliaryPureDialogueSceneMessage(ConversationMessage msg)
	{
        return ScenePromptMessageProjectionComposer.IsAuxiliaryPureDialogueSceneMessage(msg);
    }

	public static string GetLatestSceneNpcUtteranceForExternal(int targetAgentIndex)
	{
		try
		{
			return CurrentInstance?.GetLatestSceneNpcUtterance(targetAgentIndex) ?? "";
		}
		catch
		{
			return "";
		}
	}

	private string GetLatestSceneNpcUtterance(int targetAgentIndex) => targetAgentIndex < 0 ? "" : SceneHistoryOwner.LatestNpcUtterance(targetAgentIndex);

	private static string GetLatestSceneNpcUtteranceFromHistory(List<ConversationMessage> history, int targetAgentIndex) => SceneHistoryProjectionOwner.GetLatestSceneNpcUtteranceFromHistory(history, targetAgentIndex);

	public static void AppendExternalTargetedScenePlayerFactForExternal(string factText, int targetAgentIndex, bool triggerImmediateReaction = true, float postSpeechLeaveSeconds = 3f)
	{
		try
		{
			CurrentInstance?.AppendTargetedScenePlayerFact(factText, targetAgentIndex, triggerImmediateReaction, postSpeechLeaveSeconds);
		}
		catch
		{
		}
	}

	public static bool TriggerImmediateSceneBehaviorReactionForExternal(string factText, int targetAgentIndex, bool persistHeroPrivateHistory = true, bool suppressStare = false, float postSpeechLeaveSeconds = -1f, bool runSiegeReactionPostprocess = false, Func<bool> canStillPublish = null, Action<bool> onCompleted = null)
	{
		try
		{
			return CurrentInstance?.TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, runSiegeReactionPostprocess: runSiegeReactionPostprocess, canStillPublish: canStillPublish, onCompleted: onCompleted) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static int GetPendingImmediateSceneReactionCountForExternal()
	{
		ShoutBehavior instance = CurrentInstance;
		if (instance == null)
		{
			return 0;
		}
		return instance._sceneConversation.PendingImmediateCount;
	}

	public static void AppendExternalTargetedSceneNpcFactForExternal(string factText, int targetAgentIndex, bool persistHeroPrivateHistory = true)
	{
		try
		{
			CurrentInstance?.AppendTargetedSceneNpcFact(factText, targetAgentIndex, persistHeroPrivateHistory);
		}
		catch
		{
		}
	}

	private void AppendTargetedScenePlayerFact(string factText, int targetAgentIndex, bool triggerImmediateReaction, float postSpeechLeaveSeconds)
	{
    SceneHistoryPromptCapture.AppendTargetedScenePlayerFact(factText, targetAgentIndex, triggerImmediateReaction, postSpeechLeaveSeconds, TargetedFactImmediateReaction);
}

	private void AppendTargetedSceneNpcFact(string factText, int targetAgentIndex, bool persistHeroPrivateHistory)
	{
    SceneHistoryPromptCapture.AppendTargetedSceneNpcFact(factText, targetAgentIndex, persistHeroPrivateHistory);
}

	public static void InterruptAgentSpeechForCombatExternal(int agentIndex, string reason = "combat_hit")
	{
		try
		{
			CurrentInstance?.InterruptAgentSpeechForCombat(agentIndex, reason);
		}
		catch
		{
		}
	}

public static void ReleaseSceneConversationAgentsForCombatExternal(
		IEnumerable<int> agentIndices,
		string reason = "combat_start") => SceneAttentionController.ReleaseSceneConversationAgentsForCombatExternal(agentIndices, reason);

	public static void CancelAgentSpeechForRemovalExternal(int agentIndex, string reason = "agent_removed")
	{
		try
		{
			CurrentInstance?.CancelAgentSpeechForRemoval(agentIndex, reason);
		}
		catch
		{
		}
	}






internal static bool IsAgentHostileToMainAgent(Agent agent) => SceneAttentionController.IsAgentHostileToMainAgent(agent);

internal static bool IsUsableTeam(Team team) => SceneAttentionController.IsUsableTeam(team);

private static bool AreTeamsHostileSafely(Team firstTeam, Team secondTeam) => SceneAttentionController.AreTeamsHostileSafely(firstTeam, secondTeam);

internal static bool AreAgentsHostileForSceneConversation(Agent a, Agent b) => SceneAttentionController.AreAgentsHostileForSceneConversation(a, b);

private static bool CanNpcParticipateInAutoGroupRelay(NpcDataPacket participant, Dictionary<int, Hero> resolvedHeroes) => SceneRelayTargetAdapter.CanNpcParticipateInAutoGroupRelay(participant, resolvedHeroes);

private static bool IsSceneRelayAudienceEntrySpatiallyEligible(SceneShoutConversationScope scope, SceneShoutAudienceEntry entry, Agent liveAgent) => SceneRelayTargetAdapter.IsSceneRelayAudienceEntrySpatiallyEligible(scope, entry, liveAgent);

internal static SceneRelayEligibilitySnapshot BuildSceneRelayEligibilitySnapshot(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch) => SceneRelayTargetAdapter.BuildSceneRelayEligibilitySnapshot(scope, audienceByAgentIndex, resolvedHeroes, conversationEpoch);

internal static NpcDataPacket ResolveLiveSceneRelayTarget(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, int relayTargetAgentIndex) => SceneRelayTargetAdapter.ResolveLiveSceneRelayTarget(scope, audienceByAgentIndex, resolvedHeroes, conversationEpoch, relayTargetAgentIndex);

private int ResolveAutoGroupRelayTargetAgentIndex(int relayTargetAgentIndex, int currentSpeakerAgentIndex, List<NpcDataPacket> participants, Dictionary<int, Hero> resolvedHeroes) => _j17SceneRelayTargetAdapter.ResolveAutoGroupRelayTargetAgentIndex(relayTargetAgentIndex, currentSpeakerAgentIndex, participants, resolvedHeroes);

private void PrepareAutoGroupParticipantsForIdleTimeout(List<NpcDataPacket> participants, string trailingSpeechText = null, string playerText = null, List<string> npcVisibleTexts = null, int distinctNpcSpeakerCount = 0) => _j17SceneInteractionLifecycleController.PrepareAutoGroupParticipantsForIdleTimeout(participants, trailingSpeechText, playerText, npcVisibleTexts, distinctNpcSpeakerCount);


private void RefreshSceneConversationParticipantInteractions(List<NpcDataPacket> participants, float timeoutSeconds = -1f, ShoutTargetingContext shoutTargetingContext = null) => _j17SceneInteractionLifecycleController.RefreshSceneConversationParticipantInteractions(participants, timeoutSeconds, shoutTargetingContext);



private void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.ActivateMultiSceneMovementSuppression(participantAgentIndices);

private void RemoveSceneMovementSuppressionAgents(IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.RemoveSceneMovementSuppressionAgents(participantAgentIndices);

private void DeactivateMultiSceneMovementSuppression() => _j17SceneAttentionController.DeactivateMultiSceneMovementSuppression();

private void RequestSceneConversationAttentionRelease(IEnumerable<int> participantAgentIndices) => _j17SceneAttentionController.RequestSceneConversationAttentionRelease(participantAgentIndices);

private void ClearPendingSceneConversationAttentionRelease() => _j17SceneAttentionController.ClearPendingSceneConversationAttentionRelease();

private void FlushPendingSceneConversationAttentionRelease() => _j17SceneAttentionController.FlushPendingSceneConversationAttentionRelease();

private void UpdateMultiSceneMovementSuppression(float dt) => _j17SceneAttentionController.UpdateMultiSceneMovementSuppression(dt);

private void ApplyMultiSceneMovementSuppression(Agent agent) => _j17SceneAttentionController.ApplyMultiSceneMovementSuppression(agent);

internal static bool ShouldSuppressSceneConversationControlForMeeting() => SceneAttentionController.ShouldSuppressSceneConversationControlForMeeting();

private static bool IsSceneConversationCombatContext() => SceneAttentionController.IsSceneConversationCombatContext();

private static bool ShouldReleaseSceneConversationControlForCombat() => SceneAttentionController.ShouldReleaseSceneConversationControlForCombat();

	internal static void StripMeetingTauntTagsForSceneConversation(ref string content)
	{
        ConversationActionBoundaryBannerlordAdapter.StripMeetingTauntTagsForSceneConversation(ref content);
    }

private void ClearMeetingSceneConversationControlState() => _j17SceneAttentionController.ClearMeetingSceneConversationControlState();

private void ReleaseAllSceneConversationControlForCombat() => _j17SceneAttentionController.ReleaseAllSceneConversationControlForCombat();

internal static bool IsMeetingSceneConversationReleaseSensitive() => SceneAttentionController.IsMeetingSceneConversationReleaseSensitive();

internal static bool ShouldPreserveMeetingSceneAutonomy() => SceneAttentionController.ShouldPreserveMeetingSceneAutonomy();

private void ClearAgentSceneConversationFocus(Agent agent) => _j17SceneAttentionController.ClearAgentSceneConversationFocus(agent);

private void ReleaseAgentFromSceneConversationLocks(Agent agent) => _j17SceneAttentionController.ReleaseAgentFromSceneConversationLocks(agent);

private void ReleaseSceneConversationAttention(IEnumerable<int> participantAgentIndices, bool fullyRestoreAutonomy = true) => _j17SceneAttentionController.ReleaseSceneConversationAttention(participantAgentIndices, fullyRestoreAutonomy);

private void ReleaseSceneConversationConstraints(List<NpcDataPacket> participants, int fallbackAgentIndex = -1, bool stopAutoGroupSession = true, bool clearQueuedSpeech = true, bool forceFullAutonomyRelease = false) => _j17SceneAttentionController.ReleaseSceneConversationConstraints(participants, fallbackAgentIndex, stopAutoGroupSession, clearQueuedSpeech, forceFullAutonomyRelease);



	private void ClearQueuedSceneSpeech()
	{
		RetireQueuedSceneSpeech(_sceneSpeechQueueOwner.ClearQueued());
	}

	private static void RetireQueuedSceneSpeech(SceneSpeechQueueItem[] dropped)
	{
		foreach (SceneSpeechQueueItem item in dropped)
		{
			item.CompletionSource?.TrySetResult(false);
		}
	}

	private bool IsSpeechPipelineBusy()
	{
		if (_sceneSpeechQueueOwner.HasQueuedOrWorker())
		{
			return true;
		}
		return SceneAudio.HasSpeakingAgents;
	}

internal static void RefreshHostileCombatAgentAutonomy(Agent agent) => SceneAttentionController.RefreshHostileCombatAgentAutonomy(agent);

private static float GetSceneConversationTimeoutSecondsPerVisibleCharacter() => SceneInteractionLifecycleController.GetSceneConversationTimeoutSecondsPerVisibleCharacter();

private static string NormalizeSceneTimeoutVisibleText(string text) => SceneInteractionLifecycleController.NormalizeSceneTimeoutVisibleText(text);

private static int CountSceneTimeoutVisibleCharacters(string text) => SceneInteractionLifecycleController.CountSceneTimeoutVisibleCharacters(text);

private static int CountSceneTimeoutVisibleCharacters(IEnumerable<string> texts) => SceneInteractionLifecycleController.CountSceneTimeoutVisibleCharacters(texts);

internal static float ResolveDynamicSceneConversationTimeoutSeconds(string playerText, IEnumerable<string> npcVisibleTexts, int distinctNpcSpeakerCount, int participantCount) => SceneInteractionLifecycleController.ResolveDynamicSceneConversationTimeoutSeconds(playerText, npcVisibleTexts, distinctNpcSpeakerCount, participantCount);

private static float ResolveActiveInteractionTimeoutSeconds(int participantCount, float timeoutSeconds) => SceneInteractionLifecycleController.ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds);

private void RefreshActiveInteractionTimeout(NpcDataPacket primaryTarget, int participantCount, float timeoutSeconds) => _j17SceneInteractionLifecycleController.RefreshActiveInteractionTimeout(primaryTarget, participantCount, timeoutSeconds);

private void TrackPlayerInteraction(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false, ShoutTargetingContext shoutTargetingContext = null) => _j17SceneInteractionLifecycleController.TrackPlayerInteraction(primaryTarget, participantCount, timeoutSeconds, returnSceneSummonOnTimeout, shoutTargetingContext);

private void ScheduleInteractionTimeoutArm(int agentIndex, long interactionToken, float speechDurationSeconds) => _j17SceneInteractionLifecycleController.ScheduleInteractionTimeoutArm(agentIndex, interactionToken, speechDurationSeconds);

private void ArmActiveInteractionTimeoutNow(int agentIndex, long interactionToken) => _j17SceneInteractionLifecycleController.ArmActiveInteractionTimeoutNow(agentIndex, interactionToken);

private void UpdatePendingSceneDialogueFeeds() => _j17SceneSpeechOutputQueueController.UpdatePendingSceneDialogueFeeds();

private void ProcessPendingInteractionTimeoutArms() => _j17SceneInteractionLifecycleController.ProcessPendingInteractionTimeoutArms();





private void ScheduleMeetingReleaseAfterSpeech(int agentIndex, Hero targetHero, SceneSpeechPlaybackInfo playbackInfo) => _j17SceneSpeechFollowupController.ScheduleMeetingReleaseAfterSpeech(agentIndex, targetHero, playbackInfo);

private void FlushMeetingReleaseAfterSpeech(int agentIndex) => _j17SceneSpeechFollowupController.FlushMeetingReleaseAfterSpeech(agentIndex);

private void ShowOpenLordsHallResponseAndScheduleEntry(NpcDataPacket npc, Agent agent, List<NpcDataPacket> allNpcData, string content, bool commitHistory, string afterSpeechInfoMessage) => _j17SceneSpeechFollowupController.ShowOpenLordsHallResponseAndScheduleEntry(npc, agent, allNpcData, content, commitHistory, afterSpeechInfoMessage);

private void ScheduleLordsHallMissionEntryAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd = false) => _j17SceneSpeechFollowupController.ScheduleLordsHallMissionEntryAfterSpeech(agentIndex, playbackInfo, reason, waitForConversationEnd);

internal static bool IsNativeConversationActiveForLordsHallEntry() => SceneSpeechFollowupController.IsNativeConversationActiveForLordsHallEntry();

private void RegisterPendingLordsHallMissionEntryConversationEndHook() => _j17SceneSpeechFollowupController.RegisterPendingLordsHallMissionEntryConversationEndHook();

private void UnregisterPendingLordsHallMissionEntryConversationEndHook() => _j17SceneSpeechFollowupController.UnregisterPendingLordsHallMissionEntryConversationEndHook();

private void OnPendingLordsHallMissionEntryConversationEnded() => _j17SceneSpeechFollowupController.OnPendingLordsHallMissionEntryConversationEnded();

private bool FlushLordsHallMissionEntryAfterSpeech(int agentIndex) => _j17SceneSpeechFollowupController.FlushLordsHallMissionEntryAfterSpeech(agentIndex);

private void ExecutePendingLordsHallMissionEntry(PendingLordsHallMissionEntryAfterSpeech pending) => _j17SceneSpeechFollowupController.ExecutePendingLordsHallMissionEntry(pending);

private void ScheduleWorldMapMissionExitAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo) => _j17SceneSpeechFollowupController.ScheduleWorldMapMissionExitAfterSpeech(agentIndex, playbackInfo);

private void FlushWorldMapMissionExitAfterSpeech(int agentIndex) => _j17SceneSpeechFollowupController.FlushWorldMapMissionExitAfterSpeech(agentIndex);



private void ScheduleSceneAutonomyRestoreAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo) => _j17SceneSpeechFollowupController.ScheduleSceneAutonomyRestoreAfterSpeech(agentIndex, playbackInfo);

private void FlushSceneAutonomyRestoreAfterSpeech(int agentIndex) => _j17SceneSpeechFollowupController.FlushSceneAutonomyRestoreAfterSpeech(agentIndex);



private void UpdatePendingSceneAutonomyRestoresAfterSpeech() => _j17SceneSpeechFollowupController.UpdatePendingSceneAutonomyRestoresAfterSpeech();


private void UpdatePendingMeetingReleasesAfterSpeech() => _j17SceneSpeechFollowupController.UpdatePendingMeetingReleasesAfterSpeech();

private void UpdatePendingLordsHallMissionEntryAfterSpeech() => _j17SceneSpeechFollowupController.UpdatePendingLordsHallMissionEntryAfterSpeech();

private void UpdatePendingWorldMapMissionExitsAfterSpeech() => _j17SceneSpeechFollowupController.UpdatePendingWorldMapMissionExitsAfterSpeech();



	public static bool HasAnyImmediateSceneReactionInFlightForExternal()
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				return false;
			}
			return instance._sceneConversation.HasActiveImmediateReactions;
		}
		catch
		{
			return false;
		}
	}

internal static float EstimateBubbleTypingDurationSeconds(string content) => SceneSpeechOutputQueueController.EstimateBubbleTypingDurationSeconds(content);

internal static float ResolveActiveInteractionPlayerRangeMeters(SceneInteractionSession session) => SceneInteractionLifecycleController.ResolveActiveInteractionPlayerRangeMeters(session);

private static float CalculateDistantActiveInteractionReleaseRange(float initialPlayerDistanceMeters) => SceneInteractionLifecycleController.CalculateDistantActiveInteractionReleaseRange(initialPlayerDistanceMeters);

private static bool TryGetSnapshotPlayerDistanceMeters(ShoutTargetingContext targetingContext, int agentIndex, Agent agent, out float distanceMeters) => SceneInteractionLifecycleController.TryGetSnapshotPlayerDistanceMeters(targetingContext, agentIndex, agent, out distanceMeters);

private static bool IsPlayerWithinActiveInteractionRange(Agent targetAgent, SceneInteractionSession session = null) => SceneInteractionLifecycleController.IsPlayerWithinActiveInteractionRange(targetAgent, session);

private void UpdateActiveInteractionTimeouts() => _j17SceneInteractionLifecycleController.UpdateActiveInteractionTimeouts();

private List<SceneInteractionSession> BuildGroupedExpiredInteractionSessions(SceneInteractionSession seedSession, List<int> expiredAgentIndices) => _j17SceneInteractionLifecycleController.BuildGroupedExpiredInteractionSessions(seedSession, expiredAgentIndices);

private void ExpireGroupedActiveInteractions(List<SceneInteractionSession> sessions) => _j17SceneInteractionLifecycleController.ExpireGroupedActiveInteractions(sessions);

private SceneInteractionSession ChooseGroupedTimeoutRepresentative(List<SceneInteractionSession> sessions) => _j17SceneInteractionLifecycleController.ChooseGroupedTimeoutRepresentative(sessions);

private bool TriggerGroupedTimeoutDisperseSpeech(SceneInteractionSession representativeSession, List<SceneInteractionSession> sessions, SceneSummonConversationSession representativeSummonSession) => _j17SceneInteractionLifecycleController.TriggerGroupedTimeoutDisperseSpeech(representativeSession, sessions, representativeSummonSession);

private void ExpireActiveInteractionSilently(SceneInteractionSession session, bool deferSceneSummonReturn) => _j17SceneInteractionLifecycleController.ExpireActiveInteractionSilently(session, deferSceneSummonReturn);

private void ExpireActiveInteraction(SceneInteractionSession session) => _j17SceneInteractionLifecycleController.ExpireActiveInteraction(session);

private void RestoreAgentAutonomy(Agent agent) => _j17SceneAttentionController.RestoreAgentAutonomy(agent);

private static bool ShouldPreserveAmbientUseDuringStare(Agent agent) => SceneAttentionController.ShouldPreserveAmbientUseDuringStare(agent);

private void TryInterruptAgentSceneUseForStare(Agent agent) => _j17SceneAttentionController.TryInterruptAgentSceneUseForStare(agent);

private static string GetAgentDebugLabel(Agent agent) => SceneAttentionController.GetAgentDebugLabel(agent);

private void TraceStareDebug(string message) => _j17SceneAttentionController.TraceStareDebug(message);

private void FreezeAgentForStare(Agent agent, bool trace = false) => _j17SceneAttentionController.FreezeAgentForStare(agent, trace);

private void ForceAgentFacePlayer(Agent agent) => _j17SceneAttentionController.ForceAgentFacePlayer(agent);

private static bool IsAgentNearlyStationary(Agent agent) => SceneAttentionController.IsAgentNearlyStationary(agent);

private void ResetStaringForActiveInteraction(List<Agent> nearbyAgents, Agent primaryTarget) => _j17SceneAttentionController.ResetStaringForActiveInteraction(nearbyAgents, primaryTarget);

private void ExtendStaringHoldForPlayerDrivenSceneRound(int participantCount) => _j17SceneAttentionController.ExtendStaringHoldForPlayerDrivenSceneRound(participantCount);



	internal static string ApplyCompactPromptTemplate(string template, string key, string value)
	{
		return (template ?? string.Empty).Replace("{" + key + "}", value ?? string.Empty);
	}

	private static string BuildPlayerMarriageFactForNpcListLine(Hero npcHero)
	{
		return SceneHistoryPromptCaptureAdapter.BuildPlayerMarriageFactForNpcListLine(npcHero);
	}

	private string BuildPersistedHeroHistoryContext(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes)
    { return SceneHistoryPromptCapture.BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes); }

	private string GetOrBuildPrecomputedPersistedHistoryContext(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts)
    { return SceneHistoryPromptCapture.GetOrBuildPrecomputedPersistedHistoryContext(agentIndex, currentInput, resolvedHeroes, precomputedContexts); }

	private Task<string> StartPrecomputedPersistedHistoryContextTask(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string reason)
    { return SceneHistoryPromptCapture.StartPrecomputedPersistedHistoryContextTask(agentIndex, currentInput, resolvedHeroes, precomputedContexts, reason); }

	private Task<string> AwaitPrecomputedPersistedHistoryContextAsync(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Task<string> startedTask, string reason)
    { return SceneHistoryPromptCapture.AwaitPrecomputedPersistedHistoryContextAsync(agentIndex, currentInput, resolvedHeroes, precomputedContexts, startedTask, reason); }

	private bool PersistPlayerMessageToNamedHeroes(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex, string primaryTargetName, Dictionary<int, Agent> audienceAgentsByIndex, bool requireMemoryReceipt = false)
    {
        return SceneHistoryPromptCapture.PersistPlayerMessageToNamedHeroes(text, nearbyData, primaryTargetAgentIndex, primaryTargetName, audienceAgentsByIndex, requireMemoryReceipt);
    }

	private bool PersistNpcSpeechToNamedHeroes(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt = false)
    { return SceneHistoryPromptCapture.PersistNpcSpeechToNamedHeroes(speakerAgentIndex, speakerName, response, nearbyData, requireMemoryReceipt); }

	public static int GetCurrentSceneHistorySessionIdForExternal()
	{
		return _sceneHistorySessionId;
	}

	internal static int TryGetCurrentSceneHistorySessionIdForHistoryPersistence()
	{
		try
		{
			if (Mission.Current?.Scene == null)
			{
				return -1;
			}
			return Volatile.Read(ref SceneConversationHistoryOwner.SessionId);
		}
		catch
		{
			return -1;
		}
	}

public void UpdateStaringBehavior() => _j17SceneAttentionController.UpdateStaringBehavior();

private void ResetStaringBehavior() => _j17SceneAttentionController.ResetStaringBehavior();

private void AddAgentToStareList(Agent agent, bool interruptCurrentUse = false) => _j17SceneAttentionController.AddAgentToStareList(agent, interruptCurrentUse);

private void PauseGame() => _j17SceneShoutInputController.PauseGame();

private void ResumeGame() => _j17SceneShoutInputController.ResumeGame();

private void OnShoutCancelled() => _j17SceneShoutInputController.OnShoutCancelled();
}
