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

namespace AnimusForge;

public partial class ShoutBehavior : CampaignBehaviorBase
{
	private enum ShoutChatMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners,
		GiveSettlements
	}

	private class ShoutTradeResourceOption
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

	private class ShoutPendingTradeItem
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

	private struct ShoutPreviewLineSegment
	{
		public Vec3 Start;

		public Vec3 End;
	}

	private enum ShoutPreviewAgentHighlightRole
	{
		Candidate,
		Primary
	}

	private struct ShoutPreviewAgentHighlightState
	{
		public Agent Agent;

		public ShoutPreviewAgentHighlightRole Role;
	}

	internal sealed class SceneRelayEligibilitySnapshot
	{
		public List<NpcDataPacket> Candidates = new List<NpcDataPacket>();

		public List<string> PatienceStatusLines = new List<string>();
	}

	private sealed class ScenePrepaidTransferRecord
	{
		public int Gold;

		public int NegotiatedGold;

		public int Day;

		public string SettlementId;
	}

	private sealed class PendingNpcBubbleEntry
	{
		public Agent Agent;

		public string UiContent;

		public string NpcName;

		public float FallbackDurationSeconds;
	}


	private sealed class SceneInteractionSession
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

	private sealed class PendingInteractionTimeoutArm
	{
		public int AgentIndex;

		public long InteractionToken;

		public float ArmAtMissionTime;
	}



	private sealed class PendingMeetingReleaseAfterSpeech
	{
		public int AgentIndex;

		public Hero TargetHero;

		public PartyBase EncounterParty;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PendingWorldMapMissionExitAfterSpeech
	{
		public int AgentIndex;
		public bool WaitForPlaybackFinished;
		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PendingLordsHallMissionEntryAfterSpeech
	{
		public Mission Mission;
		public int AgentIndex;
		public string SettlementId;
		public string Reason;
		public bool WaitForConversationEnd;
		public bool WaitForPlaybackFinished;
		public float ExecuteAtMissionTime = -1f;
	}



	private sealed class PendingSceneAutonomyRestoreAfterSpeech
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

	private sealed class PendingSceneDialogueFeedEntry
	{
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

	private bool _isProcessingShout = false;

	private float _shoutProcessingStartedAt = -1f;

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

	private static readonly uint ShoutPreviewMarkerTintColor = new Color(0.08f, 1f, 1f, 1f).ToUnsignedInteger();

	private static readonly uint ShoutPreviewAgentHighlightColor = new Color(1f, 0.84f, 0.2f, 1f).ToUnsignedInteger();

	private static readonly uint ShoutPreviewPrimaryAgentHighlightColor = new Color(1f, 0.22f, 0.72f, 1f).ToUnsignedInteger();

	private const string ShoutPreviewMarkerItemId = "animusforge_denar_ingot_item";

	private const string ShoutPreviewSecondaryMarkerItemId = "animusforge_denar_coin_item";

	private const string ShoutPreviewFallbackMarkerItemId = "sling_leadammo";

	private long _suppressShoutHotkeyUntilUtcTicks = 0L;

	private long _lastShoutProcessingBusyMessageUtcTicks = 0L;

	private bool _shoutHotkeyChargeActive = false;

	private bool _shoutHotkeyChargeOpenModeMenu = false;

	private InputKey _shoutHotkeyChargeKey = InputKey.Invalid;

	private float _shoutHotkeyChargeStartedAt = -1f;

	private ShoutTargetingContext _activeShoutTargetingContext = null;

	private ShoutTargetingContext _lastRenderedShoutTargetingContext = null;

	private readonly List<GameEntity> _shoutPreviewMarkerEntities = new List<GameEntity>();

	private readonly Dictionary<int, ShoutPreviewAgentHighlightState> _shoutPreviewAgentHighlightStates = new Dictionary<int, ShoutPreviewAgentHighlightState>();

	private readonly HashSet<int> _shoutPreviewDesiredAgentIndicesScratch = new HashSet<int>();

	private readonly List<int> _shoutPreviewStaleAgentIndicesScratch = new List<int>();

	private ItemObject _shoutPreviewMarkerItemObject = null;

	private bool _shoutPreviewMarkerCreationFailed = false;

	internal static float GetApplicationTimeSafe()
	{
		try
		{
			return Time.ApplicationTime;
		}
		catch
		{
			return (float)Environment.TickCount / 1000f;
		}
	}

	private void UpdateShoutHotkeyCharge(InputKey shoutKey, InputKey specialMenuKey)
	{
		if (IsSceneIllustrationBattleForExternal)
		{
			if (_shoutHotkeyChargeActive) CancelShoutHotkeyCharge("battle_direct_input");
			if (!ShoutTextInputPopup.IsOpen && (Input.IsKeyPressed(shoutKey) || Input.IsKeyPressed(specialMenuKey)))
				TryStartShoutFromHotkey(false, BuildCurrentShoutTargetingContext());
			return;
		}
		if (UpdatePresentationHotkey(shoutKey, specialMenuKey))
		{
			return;
		}
		if (_shoutHotkeyChargeActive)
		{
			if (_isProcessingShout || _isWaitingForScenePostprocessGate)
			{
				CancelShoutHotkeyCharge("processing");
				return;
			}
			if (!ShoutUtils.IsInValidScene())
			{
				CancelShoutHotkeyCharge("invalid_scene");
				return;
			}
			if (Input.IsKeyReleased(_shoutHotkeyChargeKey))
			{
				ShoutTargetingContext targetingContext = _lastRenderedShoutTargetingContext ?? BuildCurrentShoutTargetingContext();
				bool openModeMenu = _shoutHotkeyChargeOpenModeMenu;
				CancelShoutHotkeyCharge("released");
				TryStartShoutFromHotkey(openModeMenu, targetingContext);
				return;
			}
			if (!Input.IsKeyDown(_shoutHotkeyChargeKey))
			{
				CancelShoutHotkeyCharge("key_state_lost");
				return;
			}
			DrawShoutRangePreview(BuildCurrentShoutTargetingContext());
			return;
		}
		if (Input.IsKeyPressed(specialMenuKey))
		{
			TryBeginShoutHotkeyCharge(specialMenuKey, openModeMenu: true);
		}
		else if (Input.IsKeyPressed(shoutKey))
		{
			TryBeginShoutHotkeyCharge(shoutKey, openModeMenu: false);
		}
	}

	private void TryBeginShoutHotkeyCharge(InputKey key, bool openModeMenu)
	{
		if (ShouldSuppressShoutHotkeyAfterFocusChange())
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_ignored", () => "[Hotkey] ignored during focus debounce", 1.0);
			return;
		}
		if (_isProcessingShout || _isWaitingForScenePostprocessGate)
		{
			TryShowShoutProcessingBusyMessage();
			return;
		}
		if (!ShoutUtils.IsInValidScene())
		{
			return;
		}
		_shoutHotkeyChargeActive = true;
		_shoutHotkeyChargeOpenModeMenu = openModeMenu;
		_shoutHotkeyChargeKey = key;
		_shoutHotkeyChargeStartedAt = GetApplicationTimeSafe();
		ResetPassiveStareTracking();
		_lastRenderedShoutTargetingContext = null;
		DrawShoutRangePreview(BuildCurrentShoutTargetingContext());
	}

	private void CancelShoutHotkeyCharge(string reason)
	{
		if (_shoutHotkeyChargeActive)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_cancel:" + (reason ?? ""), () => "[Hotkey] charge cancelled reason=" + (reason ?? ""), 1.0);
		}
		_shoutHotkeyChargeActive = false;
		_shoutHotkeyChargeOpenModeMenu = false;
		_shoutHotkeyChargeMergesIntoSession = false;
		_shoutHotkeyChargeKey = InputKey.Invalid;
		_shoutHotkeyChargeStartedAt = -1f;
		ClearShoutRangePreviewEntities();
		ClearShoutPreviewAgentHighlights();
		_lastRenderedShoutTargetingContext = null;
	}

	private ShoutTargetingContext BuildCurrentShoutTargetingContext()
	{
		float elapsedSeconds = 0f;
		if (_shoutHotkeyChargeStartedAt >= 0f)
		{
			elapsedSeconds = Math.Max(0f, GetApplicationTimeSafe() - _shoutHotkeyChargeStartedAt);
		}
		return BuildShoutTargetingContext(elapsedSeconds);
	}

	private static void GetConfiguredShoutRange(out float initialRange, out float maxRange)
	{
		initialRange = 4f;
		maxRange = ShoutHardMaxRangeMeters;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				initialRange = settings.ShoutInitialRangeMeters;
				maxRange = settings.ShoutMaxRangeMeters;
			}
		}
		catch
		{
			initialRange = 4f;
			maxRange = ShoutHardMaxRangeMeters;
		}
		if (float.IsNaN(initialRange) || float.IsInfinity(initialRange))
		{
			initialRange = 4f;
		}
		if (float.IsNaN(maxRange) || float.IsInfinity(maxRange))
		{
			maxRange = ShoutHardMaxRangeMeters;
		}
		initialRange = Math.Max(ShoutMinRangeMeters, Math.Min(ShoutHardMaxRangeMeters, initialRange));
		maxRange = Math.Max(initialRange, Math.Min(ShoutHardMaxRangeMeters, maxRange));
	}

	private ShoutTargetingContext BuildShoutTargetingContext(float elapsedSeconds)
	{
		GetConfiguredShoutRange(out var initialRange, out var maxRange);
		float progress = Math.Max(0f, Math.Min(1f, elapsedSeconds / ShoutChargeSecondsToMax));
		progress *= progress;
		float range = initialRange + (maxRange - initialRange) * progress;
		float totalAngle = ShoutInitialTotalAngleRadians + (ShoutMaxTotalAngleRadians - ShoutInitialTotalAngleRadians) * progress;
		float halfAngle = totalAngle * 0.5f;
		List<Agent> agents = ShoutUtils.GetNearbyNPCAgents(range, halfAngle) ?? new List<Agent>();
		Agent primary = ShoutUtils.GetMostCenteredAgent(agents) ?? agents.FirstOrDefault();
		Dictionary<int, float> candidateDistances = new Dictionary<int, float>();
		foreach (Agent agent in agents)
		{
			if (agent == null)
			{
				continue;
			}
			if (TryGetPlayerPlanarDistanceMeters(agent, out var distanceMeters))
			{
				candidateDistances[agent.Index] = distanceMeters;
			}
		}
		return new ShoutTargetingContext
		{
			RangeMeters = range,
			HalfAngleRadians = halfAngle,
			PrimaryAgentIndex = primary?.Index ?? (-1),
			CandidateAgentIndices = agents.Where((Agent agent) => agent != null).Select((Agent agent) => agent.Index).Distinct().ToList(),
			CandidatePlayerDistancesMeters = candidateDistances,
			PreviewCandidateAgents = agents
		};
	}

	private static bool TryGetPlayerPlanarDistanceMeters(Agent targetAgent, out float distanceMeters)
	{
		distanceMeters = 0f;
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float distanceSquared = targetAgent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2);
			if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || distanceSquared < 0f)
			{
				return false;
			}
			distanceMeters = (float)Math.Sqrt(distanceSquared);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void DrawShoutRangePreview(ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null || Mission.Current?.Scene == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			_lastRenderedShoutTargetingContext = null;
			ClearShoutPreviewAgentHighlights();
			return;
		}
		try
		{
			Vec2 forward = Agent.Main.LookDirection.AsVec2;
			if (forward.LengthSquared <= 1E-05f)
			{
				_lastRenderedShoutTargetingContext = null;
				ClearShoutPreviewAgentHighlights();
				return;
			}
			forward.Normalize();
			float forwardAngle = (float)Math.Atan2(forward.y, forward.x);
			float startAngle = forwardAngle - targetingContext.HalfAngleRadians;
			float endAngle = forwardAngle + targetingContext.HalfAngleRadians;
			Vec3 center = GetShoutPreviewGroundPoint(Agent.Main.Position);
			List<ShoutPreviewLineSegment> previewSegments = BuildShoutPreviewLineSegments(center, targetingContext.RangeMeters, startAngle, endAngle);
			UpdateShoutPreviewLineEntities(previewSegments);
			UpdateShoutPreviewAgentHighlights(targetingContext);
			_lastRenderedShoutTargetingContext = targetingContext;
		}
		catch (Exception ex)
		{
			_lastRenderedShoutTargetingContext = null;
			ClearShoutPreviewAgentHighlights();
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_failed", () => "[Hotkey] range preview failed: " + ex.Message, 2.0);
		}
	}

	private void UpdateShoutPreviewAgentHighlights(ShoutTargetingContext targetingContext)
	{
		_shoutPreviewDesiredAgentIndicesScratch.Clear();
		_shoutPreviewStaleAgentIndicesScratch.Clear();
		List<Agent> previewCandidateAgents = targetingContext?.PreviewCandidateAgents;
		int primaryAgentIndex = targetingContext?.PrimaryAgentIndex ?? (-1);
		if (previewCandidateAgents != null)
		{
			for (int i = 0; i < previewCandidateAgents.Count; i++)
			{
				Agent agent = previewCandidateAgents[i];
				if (agent == null || !agent.IsActive() || agent.Index < 0 || !_shoutPreviewDesiredAgentIndicesScratch.Add(agent.Index))
				{
					continue;
				}
				ShoutPreviewAgentHighlightRole role = ((agent.Index == primaryAgentIndex) ? ShoutPreviewAgentHighlightRole.Primary : ShoutPreviewAgentHighlightRole.Candidate);
				uint color = ((role == ShoutPreviewAgentHighlightRole.Primary) ? ShoutPreviewPrimaryAgentHighlightColor : ShoutPreviewAgentHighlightColor);
				if (_shoutPreviewAgentHighlightStates.TryGetValue(agent.Index, out var state))
				{
					if (ReferenceEquals(state.Agent, agent) && state.Role == role)
					{
						continue;
					}
					if (!ReferenceEquals(state.Agent, agent))
					{
						TrySetShoutPreviewAgentHighlight(state.Agent, null);
					}
					if (TrySetShoutPreviewAgentHighlight(agent, color))
					{
						_shoutPreviewAgentHighlightStates[agent.Index] = new ShoutPreviewAgentHighlightState
						{
							Agent = agent,
							Role = role
						};
					}
					continue;
				}
				if (TrySetShoutPreviewAgentHighlight(agent, color))
				{
					_shoutPreviewAgentHighlightStates.Add(agent.Index, new ShoutPreviewAgentHighlightState
					{
						Agent = agent,
						Role = role
					});
				}
			}
		}
		foreach (KeyValuePair<int, ShoutPreviewAgentHighlightState> highlightState in _shoutPreviewAgentHighlightStates)
		{
			if (!_shoutPreviewDesiredAgentIndicesScratch.Contains(highlightState.Key))
			{
				_shoutPreviewStaleAgentIndicesScratch.Add(highlightState.Key);
			}
		}
		for (int j = 0; j < _shoutPreviewStaleAgentIndicesScratch.Count; j++)
		{
			int agentIndex = _shoutPreviewStaleAgentIndicesScratch[j];
			if (_shoutPreviewAgentHighlightStates.TryGetValue(agentIndex, out var staleState))
			{
				TrySetShoutPreviewAgentHighlight(staleState.Agent, null);
				_shoutPreviewAgentHighlightStates.Remove(agentIndex);
			}
		}
	}

	private void ClearShoutPreviewAgentHighlights()
	{
		if (_shoutPreviewAgentHighlightStates.Count == 0)
		{
			_shoutPreviewDesiredAgentIndicesScratch.Clear();
			_shoutPreviewStaleAgentIndicesScratch.Clear();
			return;
		}
		foreach (ShoutPreviewAgentHighlightState state in _shoutPreviewAgentHighlightStates.Values)
		{
			TrySetShoutPreviewAgentHighlight(state.Agent, null);
		}
		_shoutPreviewAgentHighlightStates.Clear();
		_shoutPreviewDesiredAgentIndicesScratch.Clear();
		_shoutPreviewStaleAgentIndicesScratch.Clear();
	}

	private static bool TrySetShoutPreviewAgentHighlight(Agent agent, uint? color)
	{
		if (agent == null)
		{
			return false;
		}
		try
		{
			if (color.HasValue && !agent.IsActive())
			{
				return false;
			}
			if (agent.AgentVisuals == null)
			{
				return false;
			}
			agent.AgentVisuals.SetContourColor(color);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static List<ShoutPreviewLineSegment> BuildShoutPreviewLineSegments(Vec3 center, float range, float startAngle, float endAngle)
	{
		float totalAngle = Math.Max(0.1f, Math.Abs(endAngle - startAngle));
		int arcSegments = ClampShoutPreviewSegmentCount((int)Math.Ceiling(range * totalAngle / ShoutPreviewArcSegmentLengthMeters), ShoutPreviewMinArcSegments, ShoutPreviewMaxArcSegments);
		int radialSegments = ClampShoutPreviewSegmentCount((int)Math.Ceiling(range / ShoutPreviewRadialSegmentLengthMeters), ShoutPreviewMinRadialSegments, ShoutPreviewMaxRadialSegments);
		List<ShoutPreviewLineSegment> segments = new List<ShoutPreviewLineSegment>(Math.Min(ShoutPreviewMaxMarkerCount, arcSegments + radialSegments * 2));
		AddShoutPreviewArcSegments(segments, center, range, startAngle, endAngle, arcSegments);
		AddShoutPreviewBoundarySegments(segments, center, range, startAngle, radialSegments);
		AddShoutPreviewBoundarySegments(segments, center, range, endAngle, radialSegments);
		return segments;
	}

	private static int ClampShoutPreviewSegmentCount(int value, int minValue, int maxValue)
	{
		return Math.Max(minValue, Math.Min(maxValue, value));
	}

	private static void AddShoutPreviewArcSegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float startAngle, float endAngle, int arcSegments)
	{
		if (segments == null || arcSegments <= 0 || range <= 0f)
		{
			return;
		}
		for (int i = 0; i < arcSegments; i++)
		{
			if (segments.Count >= ShoutPreviewMaxMarkerCount)
			{
				return;
			}
			float t = (float)i / arcSegments;
			float t2 = (float)(i + 1) / arcSegments;
			segments.Add(new ShoutPreviewLineSegment
			{
				Start = GetShoutPreviewArcPoint(center, range, startAngle + (endAngle - startAngle) * t),
				End = GetShoutPreviewArcPoint(center, range, startAngle + (endAngle - startAngle) * t2)
			});
		}
	}

	private static void AddShoutPreviewBoundarySegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float angle, int radialSegments)
	{
		if (segments == null || radialSegments <= 0 || range <= 0f)
		{
			return;
		}
		for (int i = 0; i < radialSegments; i++)
		{
			if (segments.Count >= ShoutPreviewMaxMarkerCount)
			{
				return;
			}
			float startDistance = range * i / radialSegments;
			float endDistance = range * (i + 1) / radialSegments;
			if (endDistance <= 0.05f)
			{
				continue;
			}
			segments.Add(new ShoutPreviewLineSegment
			{
				Start = GetShoutPreviewArcPoint(center, startDistance, angle),
				End = GetShoutPreviewArcPoint(center, endDistance, angle)
			});
		}
	}

	private static Vec3 GetShoutPreviewArcPoint(Vec3 center, float range, float angle)
	{
		Vec3 point = new Vec3(center.x + (float)Math.Cos(angle) * range, center.y + (float)Math.Sin(angle) * range, center.z, -1f);
		return GetShoutPreviewGroundPoint(point);
	}

	private static Vec3 GetShoutPreviewGroundPoint(Vec3 point)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Scene != null)
			{
				point.z = mission.Scene.GetGroundHeightAtPosition(point, BodyFlags.CommonCollisionExcludeFlags) + ShoutPreviewMarkerGroundOffset;
			}
			else
			{
				point.z += ShoutPreviewMarkerGroundOffset;
			}
		}
		catch
		{
			point.z += ShoutPreviewMarkerGroundOffset;
		}
		return point;
	}

	private void UpdateShoutPreviewLineEntities(List<ShoutPreviewLineSegment> segments)
	{
		if (segments == null || segments.Count == 0)
		{
			HideExtraShoutPreviewMarkerEntities(0);
			return;
		}
		int desiredCount = Math.Min(segments.Count, ShoutPreviewMaxMarkerCount);
		EnsureShoutPreviewMarkerCount(desiredCount, segments[0].Start);
		int visibleCount = Math.Min(desiredCount, _shoutPreviewMarkerEntities.Count);
		for (int i = 0; i < visibleCount; i++)
		{
			GameEntity entity = _shoutPreviewMarkerEntities[i];
			if (entity == null)
			{
				continue;
			}
			try
			{
				if (TryBuildShoutPreviewLineFrame(segments[i], out var frame))
				{
					entity.SetFrame(ref frame);
					entity.SetVisibilityExcludeParents(true);
				}
				else
				{
					entity.SetVisibilityExcludeParents(false);
				}
			}
			catch
			{
			}
		}
		HideExtraShoutPreviewMarkerEntities(visibleCount);
	}

	private void EnsureShoutPreviewMarkerCount(int desiredCount, Vec3 spawnPosition)
	{
		desiredCount = Math.Min(Math.Max(0, desiredCount), ShoutPreviewMaxMarkerCount);
		int createdThisTick = 0;
		while (!_shoutPreviewMarkerCreationFailed && _shoutPreviewMarkerEntities.Count < desiredCount && createdThisTick < ShoutPreviewMaxMarkerCreatesPerTick)
		{
			GameEntity entity = TryCreateShoutPreviewMarkerEntity(spawnPosition);
			if (entity == null)
			{
				_shoutPreviewMarkerCreationFailed = true;
				Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_create_failed", () => "[Hotkey] range preview marker creation failed", 2.0);
				break;
			}
			_shoutPreviewMarkerEntities.Add(entity);
			createdThisTick++;
		}
	}

	private static bool TryBuildShoutPreviewLineFrame(ShoutPreviewLineSegment segment, out MatrixFrame frame)
	{
		frame = MatrixFrame.Identity;
		Vec3 delta = new Vec3(segment.End.x - segment.Start.x, segment.End.y - segment.Start.y, segment.End.z - segment.Start.z, -1f);
		float length = (float)Math.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
		if (float.IsNaN(length) || float.IsInfinity(length) || length < 0.05f)
		{
			return false;
		}
		Vec3 line = new Vec3(delta.x / length, delta.y / length, delta.z / length, -1f);
		Vec3 up = new Vec3(0f, 0f, 1f, -1f);
		Vec3 widthAxis = CrossVec3(up, line);
		if (!TryNormalizeVec3(ref widthAxis))
		{
			widthAxis = new Vec3(-line.y, line.x, 0f, -1f);
			if (!TryNormalizeVec3(ref widthAxis))
			{
				widthAxis = new Vec3(0f, 1f, 0f, -1f);
			}
		}
		Vec3 adjustedUp = CrossVec3(line, widthAxis);
		if (!TryNormalizeVec3(ref adjustedUp))
		{
			adjustedUp = up;
		}
		frame.origin = new Vec3((segment.Start.x + segment.End.x) * 0.5f, (segment.Start.y + segment.End.y) * 0.5f, (segment.Start.z + segment.End.z) * 0.5f, -1f);
		frame.rotation.s = ScaleVec3(line, Math.Max(0.25f, length * ShoutPreviewLineLengthScaleMultiplier + ShoutPreviewLineLengthOverlapScale));
		frame.rotation.f = ScaleVec3(widthAxis, ShoutPreviewLineWidthScale);
		frame.rotation.u = ScaleVec3(adjustedUp, ShoutPreviewLineHeightScale);
		return true;
	}

	private static Vec3 CrossVec3(Vec3 a, Vec3 b)
	{
		return new Vec3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x, -1f);
	}

	private static Vec3 ScaleVec3(Vec3 value, float scale)
	{
		return new Vec3(value.x * scale, value.y * scale, value.z * scale, -1f);
	}

	private static bool TryNormalizeVec3(ref Vec3 value)
	{
		float lengthSquared = value.x * value.x + value.y * value.y + value.z * value.z;
		if (float.IsNaN(lengthSquared) || float.IsInfinity(lengthSquared) || lengthSquared <= 1E-06f)
		{
			return false;
		}
		float invLength = 1f / (float)Math.Sqrt(lengthSquared);
		value = new Vec3(value.x * invLength, value.y * invLength, value.z * invLength, -1f);
		return true;
	}

	private void HideExtraShoutPreviewMarkerEntities(int visibleCount)
	{
		for (int i = visibleCount; i < _shoutPreviewMarkerEntities.Count; i++)
		{
			try
			{
				_shoutPreviewMarkerEntities[i]?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
		}
	}

	private GameEntity TryCreateShoutPreviewMarkerEntity(Vec3 spawnPosition)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Scene == null)
			{
				return null;
			}
			ItemObject markerItem = TryGetShoutPreviewMarkerItemObject();
			if (markerItem == null)
			{
				return null;
			}
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = spawnPosition;
			frame.rotation.ApplyScaleLocal(ShoutPreviewLineWidthScale);
			MissionWeapon missionWeapon = new MissionWeapon(markerItem, null, null, 1);
			GameEntity entity = mission.SpawnWeaponWithNewEntity(ref missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics | Mission.WeaponSpawnFlags.CannotBePickedUp, frame);
			if (entity == null)
			{
				return null;
			}
			entity.Name = "animusforge_shout_range_marker";
			entity.EntityFlags |= EntityFlags.DontSaveToScene | EntityFlags.PhysicsDisabled;
			entity.SetMobility(GameEntity.Mobility.Stationary);
			try
			{
				entity.SetPhysicsState(false, setChildren: true);
			}
			catch
			{
			}
			TryTintShoutPreviewMarker(entity);
			entity.SetFrame(ref frame);
			entity.SetVisibilityExcludeParents(true);
			return entity;
		}
		catch (Exception ex)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_create_exception", () => "[Hotkey] range preview marker creation exception: " + ex.Message, 2.0);
			return null;
		}
	}

	private ItemObject TryGetShoutPreviewMarkerItemObject()
	{
		if (_shoutPreviewMarkerItemObject != null)
		{
			return _shoutPreviewMarkerItemObject;
		}
		try
		{
			var objectManager = Game.Current?.ObjectManager;
			_shoutPreviewMarkerItemObject = objectManager?.GetObject<ItemObject>(ShoutPreviewMarkerItemId) ?? objectManager?.GetObject<ItemObject>(ShoutPreviewSecondaryMarkerItemId) ?? objectManager?.GetObject<ItemObject>(ShoutPreviewFallbackMarkerItemId);
			if (_shoutPreviewMarkerItemObject != null)
			{
				Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_item:" + _shoutPreviewMarkerItemObject.StringId, () => "[Hotkey] range preview marker item=" + _shoutPreviewMarkerItemObject.StringId, 1.0);
			}
			return _shoutPreviewMarkerItemObject;
		}
		catch (Exception ex)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_item_lookup_failed", () => "[Hotkey] range preview marker item lookup failed: " + ex.Message, 2.0);
			return null;
		}
	}

	private static void TryTintShoutPreviewMarker(GameEntity entity)
	{
		if (entity == null)
		{
			return;
		}
		try
		{
			entity.SetContourColor(ShoutPreviewMarkerTintColor);
			entity.SetVectorArgument(0.08f, 1f, 1f, 1f);
			entity.SetColor(ShoutPreviewMarkerTintColor, ShoutPreviewMarkerTintColor, "");
			for (int i = 0; i < 8; i++)
			{
				MetaMesh metaMesh = entity.GetMetaMesh(i);
				if (metaMesh == null)
				{
					continue;
				}
				metaMesh.SetFactor1(ShoutPreviewMarkerTintColor);
				metaMesh.SetFactor2(ShoutPreviewMarkerTintColor);
				metaMesh.SetGlossMultiplier(6f);
			}
		}
		catch
		{
		}
	}

	private void ClearShoutRangePreviewEntities()
	{
		if (_shoutPreviewMarkerEntities.Count == 0)
		{
			return;
		}
		foreach (GameEntity entity in _shoutPreviewMarkerEntities)
		{
			try
			{
				entity?.Remove(ShoutPreviewMarkerRemoveReason);
			}
			catch
			{
			}
		}
		_shoutPreviewMarkerEntities.Clear();
		_shoutPreviewMarkerCreationFailed = false;
	}

	private List<Agent> GetAgentsForShoutTargetingContext(ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null)
		{
			return ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
		}
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (targetingContext.CandidateAgentIndices == null || targetingContext.CandidateAgentIndices.Count == 0 || agents == null)
		{
			return new List<Agent>();
		}
		HashSet<int> wanted = new HashSet<int>(targetingContext.CandidateAgentIndices);
		Dictionary<int, Agent> snapshotAgents = new Dictionary<int, Agent>();
		if (targetingContext.PreviewCandidateAgents != null)
		{
			for (int i = 0; i < targetingContext.PreviewCandidateAgents.Count; i++)
			{
				Agent snapshotAgent = targetingContext.PreviewCandidateAgents[i];
				if (snapshotAgent != null && wanted.Contains(snapshotAgent.Index) && !snapshotAgents.ContainsKey(snapshotAgent.Index))
				{
					snapshotAgents.Add(snapshotAgent.Index, snapshotAgent);
				}
			}
		}
		Dictionary<int, Agent> liveAgents = new Dictionary<int, Agent>();
		foreach (Agent agent in agents)
		{
			if (agent != null && wanted.Contains(agent.Index) && agent != Agent.Main && agent.IsActive() && agent.IsHuman && !RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent) && (!snapshotAgents.TryGetValue(agent.Index, out var snapshotAgent2) || ReferenceEquals(snapshotAgent2, agent)))
			{
				liveAgents[agent.Index] = agent;
			}
		}
		List<Agent> result = new List<Agent>();
		foreach (int agentIndex in targetingContext.CandidateAgentIndices)
		{
			if (liveAgents.TryGetValue(agentIndex, out var agent))
			{
				result.Add(agent);
			}
		}
		return result;
	}

	internal static Agent ResolvePrimaryAgentForShoutTargetingContext(ShoutTargetingContext targetingContext, List<Agent> agents)
	{
		if (targetingContext == null || targetingContext.PrimaryAgentIndex < 0 || agents == null)
		{
			return null;
		}
		return agents.FirstOrDefault((Agent a) => a != null && a.Index == targetingContext.PrimaryAgentIndex);
	}

	// excludedAgentIndices: persistent-session 屏蔽 list; never contains the primary. Null outside a session.
	internal static bool TryBuildSceneShoutConversationScope(List<Agent> framedAgents, Agent primaryAgent, int conversationEpoch, out SceneShoutConversationScope scope, out List<Agent> audienceAgents, HashSet<int> excludedAgentIndices = null, bool excludeUnframedAgents = false)
	{
		if (excludedAgentIndices != null && primaryAgent != null)
		{
			excludedAgentIndices.Remove(primaryAgent.Index);
			framedAgents = framedAgents?.Where(agent => agent != null && !excludedAgentIndices.Contains(agent.Index)).ToList();
		}
		scope = null;
		audienceAgents = new List<Agent>();
		Mission mission = Mission.Current;
		Agent playerAgent = Agent.Main;
		var missionAgents = mission?.Agents;
		if (mission == null || missionAgents == null || playerAgent == null || primaryAgent == null || !playerAgent.IsActive() || !primaryAgent.IsActive())
		{
			return false;
		}
		Vec3 primaryAnchorPosition = primaryAgent.Position;
		Vec3 playerAnchorPosition = playerAgent.Position;
		List<Agent> primaryAnchorAgents = new List<Agent>();
		List<Agent> playerAnchorAgents = new List<Agent>();
		// In restricted mode, retain anchor/LOS metadata for invited agents without scanning bystanders.
		IEnumerable<Agent> anchorCandidates = excludeUnframedAgents
			? ((IEnumerable<Agent>)framedAgents ?? Enumerable.Empty<Agent>())
			: missionAgents;
		foreach (Agent agent in anchorCandidates)
		{
			if (agent == null || agent == playerAgent || !agent.IsActive() || !agent.IsHuman
				|| RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent)
				|| (excludedAgentIndices != null && excludedAgentIndices.Contains(agent.Index)))
			{
				continue;
			}
			try
			{
				if (agent.State != AgentState.Active || agent.Health <= 0f)
				{
					continue;
				}
				float primaryDistanceSquared = agent.Position.DistanceSquared(primaryAnchorPosition);
				if (!float.IsNaN(primaryDistanceSquared) && !float.IsInfinity(primaryDistanceSquared) && primaryDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightBetweenAgents(primaryAgent, agent, failClosed: true))
				{
					primaryAnchorAgents.Add(agent);
				}
				float playerDistanceSquared = agent.Position.DistanceSquared(playerAnchorPosition);
				if (!float.IsNaN(playerDistanceSquared) && !float.IsInfinity(playerDistanceSquared) && playerDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightBetweenAgents(playerAgent, agent, failClosed: true))
				{
					playerAnchorAgents.Add(agent);
				}
			}
			catch
			{
			}
		}
		if (!SceneShoutConversationScope.TryCreate(mission, conversationEpoch, primaryAgent, playerAgent, primaryAnchorPosition, playerAnchorPosition, framedAgents ?? new List<Agent>(), primaryAnchorAgents, playerAnchorAgents, out scope) || scope == null)
		{
			return false;
		}
		audienceAgents = new List<Agent>(scope.Count);
		for (int i = 0; i < scope.Entries.Count; i++)
		{
			Agent agent2 = scope.Entries[i].AgentReference;
			if (agent2 != null)
			{
				audienceAgents.Add(agent2);
			}
		}
		Logger.Log("ShoutBehavior", "[SceneAudience] captured epoch=" + conversationEpoch + " primary=" + scope.PrimaryAgentIndex + " framed=" + scope.FramedCount + " primary10m=" + scope.PrimaryAnchorCount + " player10m=" + scope.PlayerAnchorCount + " total=" + scope.Count);
		return audienceAgents.Count > 0;
	}

	private void TryStartShoutFromHotkey(bool openModeMenu, ShoutTargetingContext targetingContext = null)
	{
		if (ShouldSuppressShoutHotkeyAfterFocusChange())
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_ignored", () => "[Hotkey] ignored during focus debounce", 1.0);
			return;
		}
		if (_isProcessingShout || _isWaitingForScenePostprocessGate)
		{
			if (IsSceneIllustrationBattleForExternal && IsSceneIllustrationAvailableForExternal && !ShoutTextInputPopup.IsOpen)
			{
				OpenBattleShoutInput(imageOnly: true);
				return;
			}
			TryShowShoutProcessingBusyMessage();
			return;
		}
		if (!ShoutUtils.IsInValidScene())
		{
			return;
		}
		// Presentation session styles merge T and Y: both open the action wheel.
		openModeMenu = openModeMenu || IsScenePresentationSessionEnabled();
		_activeShoutTargetingContext = targetingContext;
		LogShoutTargetingContextSnapshot(openModeMenu, targetingContext);
		BeginShoutProcessing(openModeMenu ? "hotkey_special_menu" : "hotkey_shout_input");
        try
        {
            if (IsSceneIllustrationBattleForExternal)
            {
                OpenBattleShoutInput();
                return;
            }
            if (openModeMenu)
			{
				TriggerShout();
			}
			else
			{
				TriggerShoutDirectInput();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] TriggerShout failed: " + ex.Message);
			ResumeGame();
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 打开喊话界面失败，已重置状态。", new Color(1f, 0.3f, 0.3f)));
		}
	}

	private static void LogShoutTargetingContextSnapshot(bool openModeMenu, ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_targeting_snapshot_null", () => "[Hotkey] targeting snapshot is null mode=" + (openModeMenu ? "special" : "direct"), 1.0);
			return;
		}
		int count = targetingContext.CandidateAgentIndices?.Count ?? 0;
		float totalAngleDegrees = targetingContext.HalfAngleRadians * 2f * 57.29578f;
		Logger.LogVerbose("ShoutBehavior", "hotkey_targeting_snapshot", () => "[Hotkey] targeting snapshot mode=" + (openModeMenu ? "special" : "direct") + " range=" + targetingContext.RangeMeters.ToString("0.##") + " angle=" + totalAngleDegrees.ToString("0.#") + " candidates=" + count + " primary=" + targetingContext.PrimaryAgentIndex, 0.5);
	}

	private void DrainMainThreadActionsForMissionTick()
	{
		LlmRetryPrompt.CaptureMainThreadContext();
#if BANNERLORD_1_4_OR_GREATER
		Action action;
		while (_mainThreadActions.TryDequeue(out action))
		{
			ExecuteMainThreadAction(action);
		}
#else
		int pendingAtStart = _mainThreadActions.Count;
		if (pendingAtStart <= 0)
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		int processed = 0;
		int maxActions = Math.Min(SceneMainThreadActionMaxPerTick13, pendingAtStart);
		while (processed < maxActions && _mainThreadActions.TryDequeue(out var action))
		{
			processed++;
			ExecuteMainThreadAction(action);
			if (stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionBudgetMs13)
			{
				break;
			}
		}
		stopwatch.Stop();
		int remaining = _mainThreadActions.Count;
		if (remaining > 0 || stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionSlowMs)
		{
			LogMainThreadActionBudget13(pendingAtStart, processed, remaining, stopwatch.Elapsed.TotalMilliseconds);
		}
#endif
	}

	private void ExecuteMainThreadAction(Action action)
	{
		if (action == null)
		{
			return;
		}
		string actionScopeName = "ShoutBehavior.ExecuteMainThreadAction";
		try
		{
			string methodName = action.Method?.Name;
			if (!string.IsNullOrWhiteSpace(methodName))
			{
				actionScopeName += "." + methodName;
			}
		}
		catch
		{
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			using (FreezeWatchdog.Scope(actionScopeName))
			{
				FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.begin", "method=" + actionScopeName + " remaining=" + _mainThreadActions.Count);
				action();
				FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.end", "method=" + actionScopeName + " remaining=" + _mainThreadActions.Count);
			}
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			BannerlordExceptionSentinel.ReportObservedException("LipSync.MainThreadAction", ex, "behavior=ShoutMissionBehavior");
		}
		finally
		{
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionSlowMs)
			{
				Logger.Log("ShoutBehavior", "[MainThreadActions] slow_action elapsedMs=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) + " remaining=" + _mainThreadActions.Count);
			}
		}
	}

	private void LogMainThreadActionBudget13(int pendingAtStart, int processed, int remaining, double elapsedMs)
	{
		try
		{
			long now = DateTime.UtcNow.Ticks;
			if (remaining > 0 && now < _nextSceneMainThreadActionBudgetLogTicks && elapsedMs < SceneMainThreadActionSlowMs)
			{
				return;
			}
			_nextSceneMainThreadActionBudgetLogTicks = now + TimeSpan.FromSeconds(1.0).Ticks;
			Logger.Log("ShoutBehavior", "[MainThreadActions][1.3_budget] pendingAtStart=" + pendingAtStart + " processed=" + processed + " remaining=" + remaining + " elapsedMs=" + Math.Round(elapsedMs, 2));
		}
		catch
		{
		}
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

	private void BeginShoutProcessing(string reason)
	{
		_scenePlayerShoutRequestOwner.InvalidateCurrent();
		Interlocked.Increment(ref _sceneShoutProcessingSequence);
		_isProcessingShout = true;
		_shoutProcessingStartedAt = GetApplicationTimeSafe();
		Logger.Log("ShoutBehavior", "[Processing] begin reason=" + (reason ?? ""));
	}

	private void EndShoutProcessing(string reason)
	{
		_scenePlayerShoutRequestOwner.InvalidateModuleTickets();
		Interlocked.Increment(ref _sceneShoutProcessingSequence);
		if (_isProcessingShout || _shoutProcessingStartedAt >= 0f)
		{
			Logger.Log("ShoutBehavior", "[Processing] end reason=" + (reason ?? "") + " elapsed=" + GetShoutProcessingElapsedSeconds().ToString("0.###"));
		}
		CancelShoutHotkeyCharge("processing_end");
		_activeShoutTargetingContext = null;
		_isProcessingShout = false;
		_shoutProcessingStartedAt = -1f;
	}

	private float GetShoutProcessingElapsedSeconds()
	{
		if (_shoutProcessingStartedAt < 0f)
		{
			return 0f;
		}
		return Math.Max(0f, GetApplicationTimeSafe() - _shoutProcessingStartedAt);
	}

	private void ArmShoutHotkeyFocusDebounce(string reason)
	{
		try
		{
			long ticks = DateTime.UtcNow.AddMilliseconds(ShoutHotkeyFocusDebounceMilliseconds).Ticks;
			Interlocked.Exchange(ref _suppressShoutHotkeyUntilUtcTicks, ticks);
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_armed:" + (reason ?? ""), () => "[Hotkey] focus debounce armed reason=" + (reason ?? "") + " ms=" + ShoutHotkeyFocusDebounceMilliseconds, 1.0);
		}
		catch
		{
		}
	}

	private bool ShouldSuppressShoutHotkeyAfterFocusChange()
	{
		try
		{
			long ticks = Interlocked.Read(ref _suppressShoutHotkeyUntilUtcTicks);
			return ticks > 0L && DateTime.UtcNow.Ticks < ticks;
		}
		catch
		{
			return false;
		}
	}

	private void TryShowShoutProcessingBusyMessage()
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			long num = Interlocked.Read(ref _lastShoutProcessingBusyMessageUtcTicks);
			if (num > 0L && ticks - num < TimeSpan.FromMilliseconds(ShoutProcessingBusyMessageCooldownMilliseconds).Ticks)
			{
				return;
			}
			Interlocked.Exchange(ref _lastShoutProcessingBusyMessageUtcTicks, ticks);
		}
		catch
		{
		}
		InformationManager.DisplayMessage(new InformationMessage("正在处理中...", new Color(1f, 1f, 0f)));
	}

	private bool IsMissionPausedByShoutUi()
	{
		try
		{
			var scene = Mission.Current?.Scene;
			return scene != null && scene.TimeSpeed <= 0.001f;
		}
		catch
		{
			return false;
		}
	}

	private void ClearStaleShoutProcessingIfNeeded()
	{
		if (!_isProcessingShout || _shoutProcessingStartedAt < 0f || ShoutTextInputPopup.IsOpen || IsMissionPausedByShoutUi())
		{
			return;
		}
		float elapsed = GetShoutProcessingElapsedSeconds();
		if (elapsed < ShoutProcessingFailsafeSeconds)
		{
			return;
		}
		Logger.Log("ShoutBehavior", "[WARN] stale shout processing flag cleared elapsed=" + elapsed.ToString("0.###"));
		EndShoutProcessing("failsafe_timeout");
	}















	private readonly object _historyLock = new object();


	private bool _lastShoutDuelLiteralHit = false;

	private List<ShoutTradeResourceOption> _shoutTradeOptions = new List<ShoutTradeResourceOption>();

	private List<ShoutPendingTradeItem> _shoutPendingTradeItems = new List<ShoutPendingTradeItem>();

	private int _shoutPendingTradeItemIndex = 0;

	private ShoutChatMode _shoutTradeMode = ShoutChatMode.Normal;

	private NpcDataPacket _shoutTradeTargetNpc = null;

	// The live Agent captured when the short-lived trade UI opens is the stable identity for non-Hero native conversation targets.
	private Agent _shoutTradeTargetAgentSnapshot = null;
	private ConversationManager _shoutTradeNativeManager;
	private Mission _shoutTradeNativeMission;
	private Agent _shoutTradeNativeAgent;

	private bool _shoutTradeActionOnly = false;

	private Hero _shoutTradeTargetHeroOverride = null;

	private CharacterObject _shoutTradeTargetCharacterOverride = null;

	private Action _shoutTradeActionOnlyFinished = null;

	private int _shoutTradeActionFactSequence = 0;

	private readonly Dictionary<string, ScenePrepaidTransferRecord> _scenePrepaidTransfers = new Dictionary<string, ScenePrepaidTransferRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _sceneHeroRevisitDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _sceneHeroRevisitDayStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _sceneHeroRevisitHandledThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _sceneHeroFirstMeetingShownThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private const string PlayerCraftedAfefInspectionSuffix = "*你的智识分辨出来了该物品由玩家亲手制造*";

	private string _pendingHeroHistoryExtraFactAfterSceneReply = "";

	private List<NpcDataPacket> _pendingHeroHistoryExtraFactTargetsAfterSceneReply = new List<NpcDataPacket>();

	private int _pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1;

	private List<Agent> _staringAgents = new List<Agent>();

	private readonly Dictionary<int, Vec3> _staringAgentAnchors = new Dictionary<int, Vec3>();

	private readonly HashSet<int> _staringUseConversationAgents = new HashSet<int>();

	private readonly object _pendingSceneConversationAttentionReleaseLock = new object();

	private readonly HashSet<int> _pendingSceneConversationAttentionReleaseAgentIndices = new HashSet<int>();

	private float _stopStaringTime = 0f;



	private readonly ScenePendingAfefFactsOwner _scenePendingAfefFactsOwner = new ScenePendingAfefFactsOwner();





	private readonly object _pendingNativeSceneMechanismActionLock = new object();

	private readonly Queue<PendingNativeSceneMechanismAction> _pendingNativeSceneMechanismActions = new Queue<PendingNativeSceneMechanismAction>();

	private const float NativeSceneTauntFightDelaySeconds = 10f;

	private readonly object _pendingNativeSceneTauntFightLock = new object();

	private PendingNativeSceneTauntFight _pendingNativeSceneTauntFight;

	private static int _sceneHistorySessionId = 0;

	private static long _currentConversationEventSequence = 0L;
	private static long _nativeIllustrationHistoryBoundary = 0L;



	internal const int AUTO_GROUP_CHAT_MAX_LINES = 8;

	private Agent _currentStareTarget = null;

	private float _stareTimer = 0f;

	private float _stareTargetLostGraceTimer = 0f;

	private float _interactionGraceTimer = 0f;

	private Dictionary<string, float> _passiveCooldowns = new Dictionary<string, float>(StringComparer.Ordinal);


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

	private const float LIP_SYNC_SAFE_MAX_DISTANCE = 6.5f;

	private static readonly Regex MeetingSceneShoutTauntTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_(?:WARN|BATTLE)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	internal const string NpcSurrenderActionTag = "[ACTION:NPC_SURRENDER]";

	private const string SiegeSurrenderActionTag = NpcSurrenderActionTag;

	public const string PersistentAdpDebtPostprocessRuleId = PromptPreprocessRuleIdAssembler.PersistentAdpDebtRuleId;

	internal const string CustomPolicyAgendaPostprocessRuleId = "kingdom_agenda";

	private const string CustomPolicyAgendaActionTag = KingdomAgendaCustomPolicyBehavior.ActionTag;

	private const string AutoGroupRelayRuleId = "scene_auto_group_relay";

	private const string AutoGroupRelayTagTemplate = "[RELAY:接力编号]";

	internal const string AutoGroupRelayPositiveSoundEvent = "event:/ui/notification/relation";

	internal const string AutoGroupRelayNegativeSoundEvent = "event:/ui/notification/coins_negative";





	private static readonly Regex SetsOwnedSettlementMassacreRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.RequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SetsOwnedSettlementMassacreStartActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StartActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SetsOwnedSettlementMassacreStopActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StopActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SetsOwnedSettlementMassacreCancelRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TroopInspectionPrisonerSlaughterActionTagRegex = new Regex(Regex.Escape(TroopInspectionPrisonerSlaughterProfile.ActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);


	private static readonly Regex CustomPolicyAgendaActionTagRegex = new Regex(Regex.Escape(CustomPolicyAgendaActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);




















	private const int SceneMainThreadActionMaxPerTick13 = 2;

	private const double SceneMainThreadActionBudgetMs13 = 6.0;

	private const double SceneMainThreadActionSlowMs = 40.0;

	private const int DeferredPostprocessTargetUnavailableResult = -2;

	internal const int NativeConversationMainThreadPreprocessTimeoutMs = 30000;


	private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
	private static int _nativeDetachedPromptParityLoggingEnabled;
	internal static bool IsNativeDetachedPromptParityLoggingEnabledForPostprocess => Volatile.Read(ref _nativeDetachedPromptParityLoggingEnabled) != 0;
	internal static List<PostprocessSummonTarget> CapturePostprocessSummonTargets(IEnumerable<SceneSummonPromptTarget> targets)
	{
		ConversationActionPostprocessOwner.RequireMainThread();
		return targets?.Where(x=>x!=null).Select(x=>new PostprocessSummonTarget { PromptId=x.PromptId,DisplayName=x.DisplayName,LocationCode=x.LocationCode,HasLocationCharacter=x.LocationCharacter!=null }).ToList();
	}
	internal static List<PostprocessGuideTarget> CapturePostprocessGuideTargets(IEnumerable<SceneGuidePromptTarget> targets)
	{
		ConversationActionPostprocessOwner.RequireMainThread();
		return targets?.Where(x=>x!=null).Select(x=>new PostprocessGuideTarget { PromptId=x.PromptId,DisplayName=x.DisplayName,LocationCode=x.LocationCode,HasLocationCharacter=x.LocationCharacter!=null }).ToList();
	}





	private long _nextSceneMainThreadActionBudgetLogTicks;

	private float _tickTimer = 0f;

	private float _nextProactiveSceneOpeningProbeMissionTime = 0f;

	private readonly object _multiSceneMovementSuppressionLock = new object();

	private readonly HashSet<int> _multiSceneMovementSuppressionAgentIndices = new HashSet<int>();

	private bool _multiSceneMovementSuppressionActive = false;

	private float _multiSceneMovementSuppressionTimer = 0f;













	private readonly Dictionary<int, SceneInteractionSession> _activeInteractionSessions = new Dictionary<int, SceneInteractionSession>();















	private readonly object _ttsBubbleSyncLock = new object();

	private readonly Dictionary<int, Queue<PendingNpcBubbleEntry>> _pendingNpcBubbleQueues = new Dictionary<int, Queue<PendingNpcBubbleEntry>>();

	private readonly Dictionary<int, Queue<float>> _pendingAudioDurationQueues = new Dictionary<int, Queue<float>>();

	private readonly HashSet<int> _ttsPlaybackStartedAgents = new HashSet<int>();

	private readonly Dictionary<int, Queue<long>> _pendingSpeechCompletionTokenQueues = new Dictionary<int, Queue<long>>();



	private readonly Dictionary<int, Queue<PendingSceneDialogueFeedEntry>> _pendingSceneDialogueFeedQueues = new Dictionary<int, Queue<PendingSceneDialogueFeedEntry>>();

	private readonly Dictionary<int, PendingInteractionTimeoutArm> _pendingInteractionTimeoutArms = new Dictionary<int, PendingInteractionTimeoutArm>();



	private readonly Dictionary<int, PendingMeetingReleaseAfterSpeech> _pendingMeetingReleasesAfterSpeech = new Dictionary<int, PendingMeetingReleaseAfterSpeech>();

	private readonly Dictionary<int, PendingWorldMapMissionExitAfterSpeech> _pendingWorldMapMissionExitsAfterSpeech = new Dictionary<int, PendingWorldMapMissionExitAfterSpeech>();

	private PendingLordsHallMissionEntryAfterSpeech _pendingLordsHallMissionEntryAfterSpeech;

	private bool _pendingLordsHallMissionEntryConversationEndHookRegistered;



	private readonly Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech> _pendingSceneAutonomyRestoresAfterSpeech = new Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech>();















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
			Interlocked.Increment(ref _sceneHistorySessionId);
			Interlocked.Exchange(ref _currentConversationEventSequence, 0L);
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

	internal static void NotifyGameWindowFocusChanged(bool focusGained)
	{
		try
		{
			CurrentInstance?.ArmShoutHotkeyFocusDebounce(focusGained ? "focus_gained_event" : "focus_lost_event");
		}
		catch
		{
		}
	}

	internal static bool IsSceneShoutInputActiveForExternal()
	{
		try
		{
			ShoutBehavior currentInstance = CurrentInstance;
			return currentInstance != null && (currentInstance._isProcessingShout
				|| currentInstance._isWaitingForScenePostprocessGate
				|| currentInstance._shoutHotkeyChargeActive
				|| ShoutTextInputPopup.IsOpen);
		}
		catch
		{
			return false;
		}
	}

	private bool TryShowNpcBubble(Agent liveAgent, string content, float typingDurationSeconds = -1f)
	{
		try
		{
			if (!CanAgentParticipateInSceneSpeech(liveAgent))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			FloatingTextMissionView floatingTextView = _floatingTextView;
			if (floatingTextView == null || !floatingTextView.IsBubbleReady())
			{
				return false;
			}
			floatingTextView.AddOrUpdateText(liveAgent, content, isAppend: false, typingDurationSeconds);
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Shared visual-only entry point for data-driven town chatter. Ambient lines use
	/// the same participation guards and floating-text layer as scene speech, without
	/// making an AI request.
	/// </summary>
	internal static bool TryShowPassiveNpcBubbleForExternal(Agent liveAgent, string content, float typingDurationSeconds = -1f)
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=current_instance_null");
				return false;
			}
			FloatingTextMissionView floatingTextView = instance._floatingTextView;
			if (floatingTextView == null || !floatingTextView.IsBubbleReady())
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=floating_text_not_ready view=" + (floatingTextView != null));
				return false;
			}
			bool shown = instance.TryShowNpcBubble(liveAgent, content, typingDurationSeconds);
			if (!shown)
			{
				Logger.LogImmediate("TownAmbient", "bubble_failed reason=agent_guard agent=" + (liveAgent?.Index.ToString() ?? "null"));
			}
			return shown;
		}
		catch
		{
			return false;
		}
	}

	private void TryTriggerPendingProactiveSceneOpening()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || mission.Scene == null || Agent.Main == null || !Agent.Main.IsActive() || Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				return;
			}
			float missionTime = mission.CurrentTime;
			if (missionTime < _nextProactiveSceneOpeningProbeMissionTime)
			{
				return;
			}
			_nextProactiveSceneOpeningProbeMissionTime = missionTime + ProactiveSceneOpeningProbeIntervalSeconds;
			if (!ProactiveNpcRequestBehavior.TryPeekPendingSceneOpening(out var targetHero, out var extraFact, out var promptText))
			{
				return;
			}
			Agent targetAgent = FindProactiveSceneOpeningAgent(mission, targetHero);
			if (!CanAgentParticipateInSceneSpeech(targetAgent))
			{
				return;
			}
			string factText = BuildProactiveSceneOpeningFactText(extraFact, promptText);
			if (TriggerImmediateSceneBehaviorReaction(factText, targetAgent.Index, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false))
			{
				ProactiveNpcRequestBehavior.TryConsumePendingSceneOpeningForHero(targetHero, out var _, out var _);
				Logger.Log("ProactiveNpcRequest", "scene opening triggered hero=" + (targetHero?.StringId ?? "") + " agentIndex=" + targetAgent.Index);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "scene opening trigger failed: " + ex.Message);
		}
	}

	private static Agent FindProactiveSceneOpeningAgent(Mission mission, Hero targetHero)
	{
		if (mission?.Agents == null || targetHero == null)
		{
			return null;
		}
		string targetHeroId = (targetHero.StringId ?? "").Trim();
		foreach (Agent agent in mission.Agents)
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			Hero agentHero = (agent.Character as CharacterObject)?.HeroObject;
			if (agentHero == null)
			{
				continue;
			}
			if (ReferenceEquals(agentHero, targetHero) || string.Equals((agentHero.StringId ?? "").Trim(), targetHeroId, StringComparison.OrdinalIgnoreCase))
			{
				return agent;
			}
		}
		CharacterObject targetCharacter = targetHero.CharacterObject;
		if (targetCharacter == null)
		{
			return null;
		}
		return mission.Agents.FirstOrDefault(agent => agent != null && agent.IsActive() && agent.Character == targetCharacter);
	}

	private static string BuildProactiveSceneOpeningFactText(string extraFact, string promptText)
	{
		List<string> parts = new List<string>();
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		if (!string.IsNullOrWhiteSpace(factBody))
		{
			parts.Add(factBody);
		}
		string promptBody = (promptText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(promptBody))
		{
			parts.Add(promptBody + " 这是NPC主动发起本轮对话的事实补充，不是玩家说出的台词。");
		}
		return parts.Count == 0 ? "" : ("[AFEF NPC行为补充] " + string.Join(" ", parts).Trim());
	}

	private static string BuildProactiveSceneOpeningPromptSection(string extraFact, string promptText)
	{
		return BuildProactiveSceneOpeningFactText(extraFact, promptText);
	}

	internal static string BuildNpcInitiatedOpeningUserText(string extraFact, string promptText)
	{
		List<string> sections = new List<string>();
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		if (!string.IsNullOrWhiteSpace(factBody))
		{
			sections.Add("【当下事实】\n[AFEF NPC行为补充] " + factBody);
		}
		string promptBody = (promptText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(promptBody))
		{
			sections.Add("【本轮行为请求】\n" + promptBody);
		}
		if (sections.Count == 0)
		{
			return "";
		}
		return "【NPC主动发起本轮对话】\n" + string.Join("\n\n", sections)
			+ "\n\n以上 role=user 内容是行为与事实指令，不是玩家说出的台词。";
	}

	internal static string BuildNpcInitiatedOpeningPersistentFactText(string extraFact)
	{
		string factBody = StripAfefPrefixForPromptSection(extraFact);
		return string.IsNullOrWhiteSpace(factBody) ? "" : ("[AFEF NPC行为补充] " + factBody);
	}

	private static string StripAfefPrefixForPromptSection(string text)
	{
		string value = StripAfefPromptScopeLabel((text ?? "").Replace("\r", " ").Replace("\n", " ").Trim());
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string[] prefixes = new string[4] { "[AFEF NPC行为补充]", "[AFEF玩家行为补充]", "【AFEF NPC行为补充】", "【AFEF玩家行为补充】" };
		foreach (string prefix in prefixes)
		{
			if (value.StartsWith(prefix, StringComparison.Ordinal))
			{
				return value.Substring(prefix.Length).Trim();
			}
		}
		return value;
	}

	private static string StripAfefPromptScopeLabel(string text)
	{
		return ConversationSpeechTextRules.StripAfefPromptScopeLabel(text);
	}




	private static string BuildScopedAfefFactLinesForPrompt(string text, bool isCurrent)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		List<string> lines = new List<string>();
		string[] parts = value.Split('\n');
		for (int i = 0; i < parts.Length; i++)
		{
			string line = BuildScopedAfefFactLineForPrompt(parts[i], isCurrent);
			if (!string.IsNullOrWhiteSpace(line))
			{
				lines.Add(line);
			}
		}
		return string.Join("\n", lines);
	}

	internal static string BuildCurrentAfefFactPromptBlock(string extraFact)
	{
		string lines = BuildScopedAfefFactLinesForPrompt(extraFact, isCurrent: true);
		if (string.IsNullOrWhiteSpace(lines))
		{
			return "";
		}
		return "【当前真实行为事实】\n" + lines + "\n【事实边界】只有上面标为【当下行为】的 AFEF 事实代表本轮刚刚真实发生的交付、展示或动作；玩家本轮口头声称给了相同财物，如果没有对应【当下行为】AFEF事实，不算新的交付。";
	}

	internal static bool CanAgentParticipateInSceneSpeech(Agent agent)
	{
		try
		{
			return agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& agent.State == AgentState.Active
				&& agent.Health > 0f;
		}
		catch
		{
			return false;
		}
	}

	private bool IsTtsPlaybackEnabledForShout()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				return false;
			}
			if (!settings.EnableTtsSpeech)
			{
				return false;
			}
			if (!settings.TtsVolcDedicatedEnabled)
			{
				return false;
			}
			return TtsEngine.Instance?.IsReady ?? false;
		}
		catch
		{
			return false;
		}
	}

	private bool IsTtsPlaybackEnabledForNativeConversation(out string disabledReason)
	{
		disabledReason = "";
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				disabledReason = "settings_null";
				return false;
			}
			if (!settings.EnableTtsSpeech)
			{
				disabledReason = "EnableTtsSpeech=false";
				return false;
			}
			if (!settings.TtsVolcDedicatedEnabled)
			{
				disabledReason = "TtsVolcDedicatedEnabled=false";
				return false;
			}
			TtsEngine tts = TtsEngine.Instance;
			if (tts == null)
			{
				disabledReason = "engine_null";
				return false;
			}
			if (!tts.IsReady)
			{
				try
				{
					tts.Initialize();
				}
				catch (Exception ex)
				{
					Logger.Log("NativeConversation", "[TTS] initialize failed before reply playback: " + ex.Message);
				}
			}
			if (!tts.IsReady)
			{
				disabledReason = "engine_not_ready";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			disabledReason = "exception:" + ex.GetType().Name;
			return false;
		}
	}

	private void ResumeTtsForNativeConversationReply()
	{
		try
		{
			bool wasPaused = SceneAudio.ClearPauseForNativeReply();
			try
			{
				_floatingTextView?.SetTypingPaused(false);
			}
			catch
			{
			}
			TtsEngine.Instance?.ResumePlayback();
			if (wasPaused)
			{
				Logger.Log("NativeConversation", "[TTS] resumed paused TTS state before native conversation reply playback");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] resume before reply playback failed: " + ex.Message);
		}
	}

	private void EnsureTtsPlaybackEventsSubscribedForNativeConversation()
	{
		try
		{
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			if (SceneAudio.IsSubscribed) return;
			SubscribeTtsPlaybackEvents();
			Logger.Log("NativeConversation", "[TTS] ensured playback event subscription for native conversation");
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] ensure playback event subscription failed: " + ex.Message);
		}
	}

	private void EnqueuePendingNpcBubble(int agentIndex, Agent liveAgent, string uiContent, string npcName, float fallbackDurationSeconds = -1f)
	{
		if (agentIndex < 0 || liveAgent == null || string.IsNullOrWhiteSpace(uiContent))
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<PendingNpcBubbleEntry>();
				_pendingNpcBubbleQueues[agentIndex] = value;
			}
			value.Enqueue(new PendingNpcBubbleEntry
			{
				Agent = liveAgent,
				UiContent = uiContent,
				NpcName = npcName,
				FallbackDurationSeconds = fallbackDurationSeconds
			});
		}
	}

	private void EnqueuePendingAudioDuration(int agentIndex, float durationSeconds)
	{
		if (agentIndex < 0 || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0f)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingAudioDurationQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<float>();
				_pendingAudioDurationQueues[agentIndex] = value;
			}
			value.Enqueue(durationSeconds);
		}
	}

	private bool TryDequeuePendingNpcBubble(int agentIndex, out PendingNpcBubbleEntry bubble, out float typingDurationSeconds)
	{
		bubble = null;
		typingDurationSeconds = -1f;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				bubble = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingNpcBubbleQueues.Remove(agentIndex);
				}
			}
			if (_pendingAudioDurationQueues.TryGetValue(agentIndex, out var value2) && value2.Count > 0)
			{
				typingDurationSeconds = value2.Dequeue();
				if (value2.Count == 0)
				{
					_pendingAudioDurationQueues.Remove(agentIndex);
				}
			}
		}
		return bubble != null;
	}

	private bool TryDispatchPendingNpcBubbleForTts(int agentIndex, bool allowFallbackDuration)
	{
		PendingNpcBubbleEntry bubble = null;
		float typingDurationSeconds = -1f;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value) || value == null || value.Count == 0)
			{
				return false;
			}
			bool flag = _pendingAudioDurationQueues.TryGetValue(agentIndex, out var value2) && value2 != null && value2.Count > 0;
			if (!flag && !allowFallbackDuration)
			{
				return false;
			}
			bubble = value.Dequeue();
			if (value.Count == 0)
			{
				_pendingNpcBubbleQueues.Remove(agentIndex);
			}
			if (flag)
			{
				typingDurationSeconds = value2.Dequeue();
				if (value2.Count == 0)
				{
					_pendingAudioDurationQueues.Remove(agentIndex);
				}
			}
			else
			{
				typingDurationSeconds = bubble?.FallbackDurationSeconds ?? (-1f);
			}
		}
		if (bubble == null)
		{
			return false;
		}
		if (typingDurationSeconds <= 0f || float.IsNaN(typingDurationSeconds) || float.IsInfinity(typingDurationSeconds))
		{
			typingDurationSeconds = EstimateBubbleTypingDurationSeconds(bubble.UiContent);
		}
		LogTtsReport("PlaybackStarted.BubbleDispatchStart", agentIndex, $"bubbleAgent={(bubble.Agent?.Index ?? -1)};typingDuration={typingDurationSeconds:F2};fallback={(typingDurationSeconds == bubble.FallbackDurationSeconds)}");
		TryShowNpcBubble(bubble.Agent, bubble.UiContent, typingDurationSeconds);
		LogTtsReport("PlaybackStarted.BubbleDispatchEnd", agentIndex, $"typingDuration={typingDurationSeconds:F2}");
		return true;
	}

	private void SchedulePendingNpcBubbleFallbackDispatch(TtsEngine.PlaybackRequest request, int delayMs = 180, int remainingRetries = 3)
	{
		int agentIndex = request?.AgentIndex ?? -1;
		if (agentIndex < 0 || !IsTtsPlaybackRequestCurrent(request))
		{
			return;
		}
		_ = Task.Run(async delegate
		{
			try
			{
				await Task.Delay(Math.Max(50, delayMs));
				_mainThreadActions.Enqueue(delegate
				{
					if (!IsTtsPlaybackRequestCurrent(request)) { return; }
					try
					{
						bool flag;
						lock (_ttsBubbleSyncLock)
						{
							flag = _ttsPlaybackStartedAgents.Contains(agentIndex);
						}
						if (flag)
						{
							bool flag2;
							lock (_ttsBubbleSyncLock)
							{
								flag2 = _pendingAudioDurationQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
							}
							if (flag2)
							{
								TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration: false);
							}
							else if (remainingRetries > 0)
							{
								SchedulePendingNpcBubbleFallbackDispatch(request, delayMs, remainingRetries - 1);
							}
							else
							{
								TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration: true);
							}
						}
					}
					catch
					{
					}
				});
			}
			catch
			{
			}
		});
	}

	private void ClearOrphanPendingAudioDuration(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			bool flag = _pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
			if (!flag)
			{
				_pendingAudioDurationQueues.Remove(agentIndex);
			}
		}
	}

	private void ClearPendingTtsBubbleSyncForAgent(int agentIndex, bool clearInteractionToken = false)
	{
		if (agentIndex < 0)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_ttsPlaybackStartedAgents.Remove(agentIndex);
			if (clearInteractionToken)
			{
				_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
			}
		}
	}

	private void ClearPendingSceneDialogueFeedForAgent(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingSceneDialogueFeedQueues.Remove(agentIndex);
		}
	}

	private void EnqueuePendingSceneDialogueFeed(int agentIndex, string speakerLabel, string content, Color color, bool waitForPlaybackFinished, float executeAtMissionTime = -1f)
	{
		if (agentIndex < 0)
		{
			return;
		}
		string text = (content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = (speakerLabel ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "NPC";
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingSceneDialogueFeedQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<PendingSceneDialogueFeedEntry>();
				_pendingSceneDialogueFeedQueues[agentIndex] = value;
			}
			value.Enqueue(new PendingSceneDialogueFeedEntry
			{
				SpeakerLabel = text2,
				Content = text,
				Color = color,
				WaitForPlaybackFinished = waitForPlaybackFinished,
				ExecuteAtMissionTime = executeAtMissionTime
			});
		}
	}

	private bool TryDequeuePendingSceneDialogueFeed(int agentIndex, out PendingSceneDialogueFeedEntry entry)
	{
		entry = null;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (_pendingSceneDialogueFeedQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0)
			{
				entry = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSceneDialogueFeedQueues.Remove(agentIndex);
				}
			}
		}
		return entry != null;
	}

	private void FlushPendingSceneDialogueFeedAfterSpeech(int agentIndex)
	{
		if (!TryDequeuePendingSceneDialogueFeed(agentIndex, out var entry) || entry == null)
		{
			return;
		}
		RecordSceneDialogueToMessageFeed(entry.SpeakerLabel, entry.Content, entry.Color);
	}

	private void ConvertPendingSceneDialogueFeedToTimedFlush(int agentIndex, float delaySeconds)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingSceneDialogueFeedQueues.TryGetValue(agentIndex, out var value) || value == null || value.Count == 0)
			{
				return;
			}
			PendingSceneDialogueFeedEntry[] array = value.ToArray();
			value.Clear();
			bool flag = false;
			float num = mission.CurrentTime + Math.Max(0f, delaySeconds);
			for (int i = 0; i < array.Length; i++)
			{
				PendingSceneDialogueFeedEntry pendingSceneDialogueFeedEntry = array[i];
				if (!flag && pendingSceneDialogueFeedEntry != null && pendingSceneDialogueFeedEntry.WaitForPlaybackFinished)
				{
					pendingSceneDialogueFeedEntry.WaitForPlaybackFinished = false;
					pendingSceneDialogueFeedEntry.ExecuteAtMissionTime = num;
					flag = true;
				}
				value.Enqueue(pendingSceneDialogueFeedEntry);
			}
			if (value.Count == 0)
			{
				_pendingSceneDialogueFeedQueues.Remove(agentIndex);
			}
		}
	}

	private void EnqueuePendingSpeechCompletionToken(int agentIndex, long interactionToken)
	{
		if (agentIndex < 0 || interactionToken == 0L)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingSpeechCompletionTokenQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<long>();
				_pendingSpeechCompletionTokenQueues[agentIndex] = value;
			}
			value.Enqueue(interactionToken);
		}
	}

	private bool TryDequeuePendingSpeechCompletionToken(int agentIndex, out long interactionToken)
	{
		interactionToken = 0L;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (_pendingSpeechCompletionTokenQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				interactionToken = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
				}
				return interactionToken != 0L;
			}
		}
		return false;
	}

	private void ClearPendingTtsBubbleSyncQueues()
	{
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Clear();
			_pendingAudioDurationQueues.Clear();
			_pendingSpeechCompletionTokenQueues.Clear();
			_sceneMovement.ClearPendingSummonLaunches();
			_pendingSceneDialogueFeedQueues.Clear();
			_ttsPlaybackStartedAgents.Clear();
		}
		ResetSceneAudioRequestOwnership();
		_pendingInteractionTimeoutArms.Clear();
	}


	private void RecordSceneDialogueToMessageFeed(string speakerLabel, string content, Color color)
	{
		string text = (content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = (speakerLabel ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "NPC";
		}
		InformationManager.DisplayMessage(new InformationMessage("[" + text2 + "] " + text, color));
	}

	private void RecordPlayerSpeechToMessageFeed(string content)
	{
		RecordSceneDialogueToMessageFeed("你", content, new Color(0.3f, 1f, 0.3f));
	}

	private void RecordNpcSpeechToMessageFeed(string npcDisplayName, string content)
	{
		RecordSceneDialogueToMessageFeed(npcDisplayName, content, new Color(1f, 0.8f, 0.2f));
	}

	private void QueueSceneInfoMessage(string message, Color color, int requiredConversationEpoch = 0, string soundEventPath = "")
	{
		string text = (message ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string soundPath = (soundEventPath ?? "").Trim();
		_mainThreadActions.Enqueue(delegate
		{
			try
			{
				if (requiredConversationEpoch > 0 && !IsSceneConversationEpochCurrent(requiredConversationEpoch))
				{
					return;
				}
				InformationManager.DisplayMessage(new InformationMessage(text, color));
				if (!string.IsNullOrWhiteSpace(soundPath))
				{
					SoundEvent.PlaySound2D(soundPath);
				}
			}
			catch
			{
			}
		});
	}

	private void ScheduleNpcSpeechToMessageFeed(int agentIndex, string npcDisplayName, string content, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || string.IsNullOrWhiteSpace(content) || mission == null)
		{
			return;
		}
		float num = Math.Max(0f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool flag = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		float executeAtMissionTime = flag ? (-1f) : (mission.CurrentTime + num);
		EnqueuePendingSceneDialogueFeed(agentIndex, npcDisplayName, content, new Color(1f, 0.8f, 0.2f), flag, executeAtMissionTime);
	}

	// BattleSpeech publishes its body immediately in the AF message feed. The
	// normal scene-shout path waits for TTS completion, which is appropriate for
	// ordinary replies but makes a long battle speech appear to be missing from
	// the lower-left feed while its world bubble is already visible.
	internal void PublishBattleSpeechMessageFeed(NpcDataPacket npc, Agent liveAgent, string content)
	{
		if (!CanAgentParticipateInSceneSpeech(liveAgent) || liveAgent.Index < 0)
		{
			return;
		}
		string text = SanitizeSceneSpeechText(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string speakerLabel = GetSceneNpcHistoryNameForPrompt(npc);
		ClearPendingSceneDialogueFeedForAgent(liveAgent.Index);
		RecordSceneDialogueToMessageFeed(
			speakerLabel,
			text,
			new Color(1f, 0.8f, 0.2f));
	}

	private SceneSpeechPlaybackInfo ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true, bool suppressInteractionTimeoutArm = false)
		=> Presentation.ShowNpcSpeechOutput(SceneSpeechOutput, npc, liveAgent, content, allowTts, attachTtsToSceneAgent, suppressInteractionTimeoutArm);
	private void TrySpeakNativeConversationReplyWithTts(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex, string visibleText)
	{
		try
		{
			string uiText = SanitizeSceneSpeechText(visibleText);
			// TTS retains the plain reply, while the typewriter receives a safe display copy so model markup cannot become live RichText mid-word.
			string typewriterText = EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(uiText);
			string ttsText = SanitizeSceneSpeechTextForTts(uiText);
			if (string.IsNullOrWhiteSpace(ttsText))
			{
				return;
			}
			if (!IsTtsPlaybackEnabledForNativeConversation(out var disabledReason))
			{
				LogTtsReport("NativeConversationTts.SkipDisabled", targetAgentIndex, $"reason={disabledReason};uiLen={(uiText ?? string.Empty).Length};ttsLen={ttsText.Length}");
				Logger.Log("NativeConversation", "[TTS] skipped native conversation reply playback: " + disabledReason);
				return;
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			string voiceId = "";
			string voiceSource = "";
			string voiceKey = "";
			try
			{
				if (targetHero != null)
				{
					voiceId = MyBehavior.GetNpcVoiceIdForExternal(targetHero);
					if (!string.IsNullOrWhiteSpace(voiceId))
					{
						voiceSource = "hero_preferred";
					}
					if (string.IsNullOrWhiteSpace(voiceId))
					{
						voiceId = VoiceMapper.ResolveVoiceId(targetHero);
						voiceSource = "hero_mapper";
					}
				}
				if (string.IsNullOrWhiteSpace(voiceId))
				{
					bool isFemale = npc?.IsFemale ?? targetHero?.IsFemale ?? targetCharacter?.IsFemale ?? false;
					float age = npc?.Age ?? 0f;
					if (age < 18f || age > 80f)
					{
						age = targetHero?.Age ?? ResolveNativeConversationNonHeroAge(targetCharacter);
					}
					if (targetHero == null && targetAgentIndex < 0)
					{
						voiceKey = BuildNativeConversationNonHeroVoiceKey(npc, targetHero, targetCharacter, targetAgentIndex);
					}
					voiceId = VoiceMapper.ResolveVoiceIdForNonHeroKey(voiceKey, isFemale, age, targetAgentIndex);
					voiceSource = string.IsNullOrWhiteSpace(voiceKey) ? "nonhero_agent_or_random" : "nonhero_key";
				}
			}
			catch
			{
				voiceId = "";
			}
			string lipSyncReason = "native_agent_unavailable";
			bool lipSyncSafe = false;
			Agent liveAgent = null;
			try
			{
				Mission mission = Mission.Current;
				var agents = mission?.Agents;
				if (targetAgentIndex >= 0 && agents != null)
				{
					liveAgent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				}
				lipSyncSafe = CanAgentParticipateInSceneSpeech(liveAgent) && CanAgentUseSceneLipSync(liveAgent, out lipSyncReason);
			}
			catch (Exception ex)
			{
				lipSyncReason = "exception:" + ex.GetType().Name;
				lipSyncSafe = false;
			}
			int effectiveAgentIndex = lipSyncSafe ? targetAgentIndex : -1;
			bool accepted = false;
			long acceptedWaitToken = 0L;
			try
			{
				EnsureTtsPlaybackEventsSubscribedForNativeConversation();
				ResumeTtsForNativeConversationReply();
				accepted = TtsEngine.Instance.SpeakAsync(ttsText, -1, -1f, effectiveAgentIndex, voiceId, request =>
				{
					TrackTtsPlaybackRequest(request);
					float estimatedDuration = Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(typewriterText));
					acceptedWaitToken = RegisterNativeConversationTtsPlaybackWait(request, estimatedDuration, typewriterText.Length);
					if (acceptedWaitToken == 0L) { return; }
					ConversationHelper.StartTypewriterText(typewriterText, estimatedDuration, waitForPlayback: true);
					ScheduleNativeConversationTypewriterPlaybackFallback(acceptedWaitToken, effectiveAgentIndex, estimatedDuration, typewriterText.Length);
				});
			}
			catch (Exception ex2)
			{
				if (acceptedWaitToken != 0L && IsNativeConversationTtsPlaybackWaitToken(acceptedWaitToken, effectiveAgentIndex))
				{
					CompleteNativeConversationTtsPlaybackWaitByToken(acceptedWaitToken, "enqueue_rejected");
					ConversationHelper.StartTypewriterPlaybackIfWaiting();
				}
				LogTtsReport("NativeConversationTts.SpeakFailed", targetAgentIndex, $"effectiveAgentIndex={effectiveAgentIndex};lipSyncSafe={lipSyncSafe};reason={lipSyncReason};error={ex2.Message}");
				Logger.Log("NativeConversation", "[TTS] SpeakAsync threw for native conversation reply: " + ex2.Message);
				return;
			}
			if (!accepted)
			{
				if (acceptedWaitToken != 0L && IsNativeConversationTtsPlaybackWaitToken(acceptedWaitToken, effectiveAgentIndex))
				{
					CompleteNativeConversationTtsPlaybackWaitByToken(acceptedWaitToken, "enqueue_rejected");
					ConversationHelper.StartTypewriterPlaybackIfWaiting();
				}
				Logger.Log("NativeConversation", "[TTS] SpeakAsync rejected native conversation reply. effectiveAgentIndex=" + effectiveAgentIndex + ", lipSyncSafe=" + lipSyncSafe + ", reason=" + lipSyncReason);
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 自由对话语音未入队，请检查TTS设置或队列。", new Color(1f, 0.8f, 0.25f)));
				}
				catch
				{
				}
			}
			string logVoiceKey = (voiceKey ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			LogTtsReport("NativeConversationTts.SpeakAttempt", targetAgentIndex, $"effectiveAgentIndex={effectiveAgentIndex};speakAccepted={accepted};voiceId={voiceId};voiceSource={voiceSource};voiceKey={logVoiceKey};lipSyncSafe={lipSyncSafe};reason={lipSyncReason};ttsLen={ttsText.Length};uiLen={(uiText ?? string.Empty).Length}");
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] TTS lipsync dispatch failed: " + ex.Message);
		}
	}

	public static bool CanAgentParticipateInSceneSpeechExternal(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return false;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			return CanAgentParticipateInSceneSpeech(agent);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEscortedPrisonerAgentIdentityMatch(Agent liveAgent, int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter)
	{
		// Custom prisoner agents can use a mission representation whose Character
		// identity is not the same object exposed by the native conversation adapter.
		// The escort registry is the authoritative, bounded identity bridge for them.
		if (liveAgent == null
			|| !NoblePrisonerEscortBehavior.TryGetEscortedHero(targetAgentIndex, out Hero escortedHero, out Agent escortedAgent)
			|| !ReferenceEquals(escortedAgent, liveAgent))
		{
			return false;
		}
		Hero expectedTargetHero = expectedHero ?? expectedCharacter?.HeroObject;
		string expectedHeroId = (expectedTargetHero?.StringId ?? "").Trim();
		string escortedHeroId = (escortedHero?.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(expectedHeroId)
			&& string.Equals(escortedHeroId, expectedHeroId, StringComparison.OrdinalIgnoreCase);
	}

	// This runs only at asynchronous-response handoff points on the Bannerlord main thread.
	// A queued LLM result must never act on an Agent that was knocked out, killed, or removed
	// while the request was in flight.
	private static bool IsSceneResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
		unavailableReason = "";
		if (targetAgentIndex < 0)
		{
			// Map/tableau conversations and non-scene channels have no live Agent to validate here.
			return true;
		}
		var agents = Mission.Current?.Agents;
		if (agents == null)
		{
			unavailableReason = "mission_or_agents_missing";
			return false;
		}
		Agent liveAgent;
		try
		{
			liveAgent = agents.FirstOrDefault((Agent agent) => agent != null && agent.Index == targetAgentIndex);
		}
		catch
		{
			unavailableReason = "agent_lookup_failed";
			return false;
		}
		if (!CanAgentParticipateInSceneSpeech(liveAgent))
		{
			unavailableReason = "agent_unavailable";
			return false;
		}
		try
		{
			if (IsEscortedPrisonerAgentIdentityMatch(liveAgent, targetAgentIndex, expectedHero, expectedCharacter))
			{
				return true;
			}
			CharacterObject liveCharacter = liveAgent.Character as CharacterObject;
			if (expectedHero != null)
			{
				Hero liveHero = liveCharacter?.HeroObject;
				if (liveHero == null || !string.Equals(liveHero.StringId, expectedHero.StringId, StringComparison.OrdinalIgnoreCase))
				{
					unavailableReason = "agent_identity_changed";
					return false;
				}
			}
			else if (expectedCharacter != null && (liveCharacter == null || !string.Equals(liveCharacter.StringId, expectedCharacter.StringId, StringComparison.OrdinalIgnoreCase)))
			{
				unavailableReason = "agent_identity_changed";
				return false;
			}
		}
		catch
		{
			unavailableReason = "agent_identity_check_failed";
			return false;
		}
		return true;
	}

	internal static bool IsNativeConversationResponseTargetAvailableForActionDispatch(int targetAgentIndex, Hero expectedHero, CharacterObject expectedCharacter, out string unavailableReason)
	{
		// A negative index means this conversation adapter did not capture a concrete
		// scene Agent; it does not prove that a target that was once present despawned.
		// The shared guard still fails closed for every captured Agent index, covering
		// the async knockout/removal race without suppressing valid prisoner adapters.
		return IsSceneResponseTargetAvailableForActionDispatch(targetAgentIndex, expectedHero, expectedCharacter, out unavailableReason);
	}

	internal static string StripScenePersonaBlocks(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string[] array = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? string.Empty).Trim();
			if (!text2.StartsWith("【角色个性】", StringComparison.Ordinal) && !text2.StartsWith("【角色背景】", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine(text2);
			}
		}
		return stringBuilder.ToString().Trim();
	}

	internal static string ExtractTrustPromptBlock(string text, out string remaining)
	{
		remaining = "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Replace("\r", "").Split('\n');
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				if (text2.StartsWith("本级语义：", StringComparison.Ordinal) || text2.StartsWith("本级信用规则：", StringComparison.Ordinal) || text2.StartsWith("价值口径：", StringComparison.Ordinal))
				{
					list.Add(text2);
				}
				else
				{
					list2.Add(text2);
				}
			}
		}
		remaining = string.Join("\n", list2).Trim();
		return string.Join("\n", list).Trim();
	}

	private static string InjectTrustBlockBeforeGroupRules(string localExtras, string trustBlock)
	{
		string text = (localExtras ?? "").Trim();
		string text2 = (trustBlock ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		int num = text.IndexOf("【群体对话规则】", StringComparison.Ordinal);
		if (num >= 0)
		{
			string text3 = text.Substring(0, num).TrimEnd();
			string text4 = text.Substring(num).TrimStart();
			return text3 + "\n" + text2 + "\n" + text4;
		}
		return text + "\n" + text2;
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
		string text = (ShoutUtils.GetPromptHistoryName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

	private static string GetSceneNpcPatienceNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptPatienceName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcHistoryNameForPrompt(npc) : text;
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
		try
		{
			Agent agent = ResolveSceneAgentForMountPrompt(npc?.AgentIndex ?? -1);
			return TryGetLiveRiddenMountNameForPrompt(agent, out var mountName) ? ("【当前坐骑】你骑着：" + mountName + "。") : "";
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPlayerCurrentMountLineForPrompt()
	{
		try
		{
			return TryGetLiveRiddenMountNameForPrompt(Agent.Main, out var mountName) ? ("【当前坐骑】面前此人骑着：" + mountName + "。") : "";
		}
		catch
		{
			return "";
		}
	}

	private static Agent ResolveSceneAgentForMountPrompt(int agentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (agentIndex < 0 || agents == null)
			{
				return null;
			}
			return agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
		}
		catch
		{
			return null;
		}
	}

	private static bool TryGetLiveRiddenMountNameForPrompt(Agent riderAgent, out string mountName)
	{
		mountName = "";
		try
		{
			if (riderAgent == null || !riderAgent.IsActive())
			{
				return false;
			}
			Agent mountAgent = riderAgent.MountAgent;
			if (mountAgent == null || !mountAgent.IsActive())
			{
				return false;
			}
			if (!TryGetMountEquipmentNameForPrompt(mountAgent, out mountName))
			{
				TryGetMountEquipmentNameForPrompt(riderAgent, out mountName);
			}
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountAgent.Name?.ToString());
			}
			return !string.IsNullOrWhiteSpace(mountName);
		}
		catch
		{
			mountName = "";
			return false;
		}
	}

	private static bool TryGetMountEquipmentNameForPrompt(Agent agent, out string mountName)
	{
		mountName = "";
		try
		{
			Equipment equipment = agent?.SpawnEquipment;
			if (equipment == null)
			{
				return false;
			}
			EquipmentElement mountElement = equipment[EquipmentIndex.ArmorItemEndSlot];
			if (mountElement.IsEmpty || mountElement.Item == null || mountElement.Item.HorseComponent == null)
			{
				return false;
			}
			mountName = NormalizeInlinePromptText(mountElement.GetModifiedItemName()?.ToString());
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountElement.Item.Name?.ToString());
			}
			if (string.IsNullOrWhiteSpace(mountName))
			{
				mountName = NormalizeInlinePromptText(mountElement.Item.StringId);
			}
			return !string.IsNullOrWhiteSpace(mountName);
		}
		catch
		{
			mountName = "";
			return false;
		}
	}

	private static string NormalizeInlinePromptText(string value)
	{
		return Regex.Replace((value ?? "").Replace("\r", " ").Replace("\n", " "), "[ \\t]{2,}", " ").Trim();
	}

	private static string GetSceneNpcListIdentityForPrompt(NpcDataPacket npc)
	{
		return ConversationActionPostprocessOwner.GetSceneNpcListIdentityForPrompt(npc);
	}

	private static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "- 名字: 未知 | 性别: 未知 | 身份: 未知身份";
		}
		string text = npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc);
		return "- 名字: " + text + " | 性别: " + (npc.IsFemale ? "女" : "男") + " | 身份: " + GetSceneNpcListIdentityForPrompt(npc);
	}

	private static string BuildPlayerRelationIdentitySuffixForNpcListLine(Hero npcHero, string playerDisplayName)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (npcHero == null || mainHero == null || npcHero == mainHero)
			{
				return "";
			}
			Clan playerClan = Clan.PlayerClan ?? mainHero.Clan;
			string playerName = (playerDisplayName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (npcHero.IsPlayerCompanion || (playerClan != null && npcHero.CompanionOf == playerClan))
			{
				return "、" + playerName + "的同伴";
			}
			if (playerClan != null && npcHero.Clan == playerClan)
			{
				return "、" + playerName + "的家族成员";
			}
		}
		catch
		{
		}
		return "";
	}

	private string BuildPlayerDisplayNameForSceneNpcListIdentity(NpcDataPacket currentNpc, Dictionary<int, Hero> resolvedHeroes)
	{
		Hero observerHero = null;
		try
		{
			if (currentNpc?.IsHero == true)
			{
				resolvedHeroes?.TryGetValue(currentNpc.AgentIndex, out observerHero);
				if (observerHero == null && currentNpc.AgentIndex >= 0)
				{
					observerHero = ResolveHeroFromAgentIndex(currentNpc.AgentIndex);
				}
			}
			if (ShouldForceDetailedPlayerIntroForObserver(observerHero) || DoesSceneObserverKnowPlayerIdentityForPrompt(observerHero, currentNpc))
			{
				string knownName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(knownName))
				{
					return knownName;
				}
			}
			string publicName = (observerHero != null
				? MyBehavior.BuildPlayerPublicDisplayNameForExternal(observerHero)
				: MyBehavior.BuildPlayerPublicDisplayNameForExternal()) ?? "";
			if (!string.IsNullOrWhiteSpace(publicName))
			{
				return publicName.Trim();
			}
		}
		catch
		{
		}
		return "玩家";
	}

	private static string BuildDistanceToCurrentNpcForNpcListLine(NpcDataPacket npc, NpcDataPacket currentNpc)
	{
		if (npc == null || currentNpc == null || !npc.HasScenePosition || !currentNpc.HasScenePosition)
		{
			return " | 距离你: 未知";
		}
		float deltaX = npc.ScenePositionX - currentNpc.ScenePositionX;
		float deltaY = npc.ScenePositionY - currentNpc.ScenePositionY;
		float deltaZ = npc.ScenePositionZ - currentNpc.ScenePositionZ;
		float distanceMeters = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
		if (float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters))
		{
			return " | 距离你: 未知";
		}
		return " | 距离你: " + distanceMeters.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "米";
	}

	private static string NormalizeSceneNpcMatchValue(string value)
	{
		return (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static bool IsSameSceneNpcForPrompt(NpcDataPacket candidate, NpcDataPacket selfNpc)
	{
		if (candidate == null || selfNpc == null)
		{
			return false;
		}
		if (object.ReferenceEquals(candidate, selfNpc))
		{
			return true;
		}
		if (candidate.AgentIndex >= 0 && selfNpc.AgentIndex >= 0 && candidate.AgentIndex == selfNpc.AgentIndex)
		{
			return true;
		}
		string candidateUnnamedKey = NormalizeSceneNpcMatchValue(candidate.UnnamedKey);
		string selfUnnamedKey = NormalizeSceneNpcMatchValue(selfNpc.UnnamedKey);
		if (!candidate.IsHero && !selfNpc.IsHero && !string.IsNullOrWhiteSpace(candidateUnnamedKey) && string.Equals(candidateUnnamedKey, selfUnnamedKey, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string candidateTroopId = NormalizeSceneNpcMatchValue(candidate.TroopId);
		string selfTroopId = NormalizeSceneNpcMatchValue(selfNpc.TroopId);
		if (candidate.IsHero && selfNpc.IsHero && !string.IsNullOrWhiteSpace(candidateTroopId) && string.Equals(candidateTroopId, selfTroopId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (candidate.AgentIndex < 0 && selfNpc.AgentIndex < 0 && candidate.IsHero == selfNpc.IsHero)
		{
			string candidateName = NormalizeSceneNpcMatchValue(candidate.Name);
			string selfName = NormalizeSceneNpcMatchValue(selfNpc.Name);
			if (!string.IsNullOrWhiteSpace(candidateName) && string.Equals(candidateName, selfName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static List<NpcDataPacket> FilterScenePresentNpcsForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc)
	{
		List<NpcDataPacket> result = new List<NpcDataPacket>();
		foreach (NpcDataPacket npc in presentNpcs ?? Enumerable.Empty<NpcDataPacket>())
		{
			if (npc != null && !IsSameSceneNpcForPrompt(npc, selfNpc))
			{
				result.Add(npc);
			}
		}
		return result;
	}

	private string BuildSceneNpcListLineWithRuntimeFactsForPrompt(NpcDataPacket npc, NpcDataPacket currentNpc, string playerDisplayName, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		string line = BuildSceneNpcListLineForPrompt(npc);
		if (npc == null)
		{
			return line;
		}
		try
		{
			if (!npc.IsHero && !IsInspectionPrisonerNpcForPrompt(npc))
			{
				string key = (npc.UnnamedKey ?? "").Trim().ToLower();
				string kingdomId = "";
				string lordId = "";
				int kIdx = key.IndexOf(":kingdom:", StringComparison.OrdinalIgnoreCase);
				if (kIdx >= 0)
				{
					kingdomId = key.Substring(kIdx + ":kingdom:".Length).Trim().ToLower();
					int cut = kingdomId.IndexOf(':');
					if (cut >= 0)
					{
						kingdomId = kingdomId.Substring(0, cut);
					}
				}
				int lIdx = key.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
				if (lIdx >= 0)
				{
					lordId = key.Substring(lIdx + ":lord:".Length).Trim().ToLower();
					int cut2 = lordId.IndexOf(':');
					if (cut2 >= 0)
					{
						lordId = lordId.Substring(0, cut2);
					}
				}
				if (string.IsNullOrWhiteSpace(kingdomId))
				{
					kingdomId = (npc.CultureId ?? "").Trim().ToLower();
				}
				string kingdomName = kingdomId;
				string rulerName = "";
				try
				{
					Kingdom kObj = Kingdom.All?.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim().ToLower(), kingdomId, StringComparison.OrdinalIgnoreCase));
					if (kObj != null)
					{
						kingdomName = (kObj.Name?.ToString() ?? kingdomName).Trim();
						rulerName = (kObj.Leader?.Name?.ToString() ?? "").Trim();
					}
				}
				catch
				{
				}
				if (!string.IsNullOrWhiteSpace(kingdomName))
				{
					line = line + " | 势力: " + kingdomName;
				}
				if (!string.IsNullOrWhiteSpace(rulerName))
				{
					line = line + " | " + (useAllegianceLabel ? "效忠于" : "统治者") + ": " + rulerName;
				}
				if (string.IsNullOrWhiteSpace(lordId))
				{
					try
					{
						lordId = (Settlement.CurrentSettlement?.OwnerClan?.Leader?.StringId ?? "").Trim().ToLower();
					}
					catch
					{
						lordId = "";
					}
				}
				if (!string.IsNullOrWhiteSpace(lordId))
				{
					string lordName;
					try
					{
						lordName = (Hero.Find(lordId)?.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						lordName = "";
					}
					if (!string.IsNullOrWhiteSpace(lordName))
					{
						line = line + " | 隶属领主: " + lordName;
					}
				}
			}
		}
		catch
		{
		}
		if (npc.IsHero)
		{
			try
			{
				Hero hero = null;
				if (resolvedHeroes != null)
				{
					resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
				}
				if (hero == null && npc.AgentIndex >= 0)
				{
					hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
				}
				line += BuildPlayerRelationIdentitySuffixForNpcListLine(hero, playerDisplayName);
				if (hero?.IsPrisoner ?? false)
				{
					string captor = hero.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
					line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
				}
				line += BuildPlayerMarriageFactForNpcListLine(hero);
			}
			catch
			{
			}
		}
		line += BuildDistanceToCurrentNpcForNpcListLine(npc, currentNpc);
		return line;
	}

	private string BuildScenePresentNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc, Dictionary<int, Hero> resolvedHeroes = null, bool useAllegianceLabel = false)
	{
		try
		{
			List<NpcDataPacket> promptNpcs = FilterScenePresentNpcsForPrompt(presentNpcs, selfNpc);
			if (promptNpcs.Count == 0)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【站在你旁边的人】：");
			string playerDisplayName = BuildPlayerDisplayNameForSceneNpcListIdentity(selfNpc, resolvedHeroes);
			foreach (NpcDataPacket npc in promptNpcs)
			{
				sb.AppendLine(BuildSceneNpcListLineWithRuntimeFactsForPrompt(npc, selfNpc, playerDisplayName, resolvedHeroes, useAllegianceLabel));
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(promptNpcs);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				sb.AppendLine(sceneNamingNote);
			}
			return sb.ToString().Trim();
		}
		catch
		{
			return "";
		}
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
		if (npc == null)
		{
			return "有人";
		}
		string name = (npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc)).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = (npc.PromptDisplayName ?? npc.PromptGivenName ?? npc.Name ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "未知NPC";
		}
		string identity = (GetSceneNpcListIdentityForPrompt(npc) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(identity) || identity.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || string.Equals(identity, name, StringComparison.OrdinalIgnoreCase))
		{
			return identity.Length > 0 ? identity : name;
		}
		return identity + name;
	}

	internal static string BuildAutoGroupRelayThinkingInfoMessage(NpcDataPacket npc)
	{
		return BuildAutoGroupRelayInfoDisplayName(npc) + "正在思考该说什么。";
	}

	private static string ResolveSceneHistorySpeakerNameForPrompt(int speakerAgentIndex, string fallbackSpeakerName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == speakerAgentIndex);
		string text = (npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : ((fallbackSpeakerName ?? "").Trim());
		return string.IsNullOrWhiteSpace(text) ? "某NPC" : text;
	}

	private static string ResolveSceneTargetNameForPrompt(int targetAgentIndex, string fallbackTargetName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == targetAgentIndex);
		string text = (npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : ((fallbackTargetName ?? "").Trim());
		return string.IsNullOrWhiteSpace(text) ? (fallbackTargetName ?? "").Trim() : text;
	}

	private static string BuildSceneNonHeroNamingNoteForPrompt(IEnumerable<NpcDataPacket> npcs)
	{
		if (npcs == null || !npcs.Any((NpcDataPacket npc) => npc != null && !npc.IsHero))
		{
			return "";
		}
		return "【非HeroNPC命名说明】：【站在你旁边的人】里的“名字”是非HeroNPC的个人名字；在【当前场景公共对话与互动】等历史里，会使用“身份+名字”的写法，例如“帝国女镇民利娅”，两者指向同一人。";
	}



	internal static string GetSceneLocationDisplayName(Location location)
	{
		try
		{
			string text = (location?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		string text2 = (location?.StringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text2) ? "当前位置" : text2;
	}






	internal static bool IsSceneLocation(Location location, string locationId)
	{
		return string.Equals((location?.StringId ?? "").Trim(), (locationId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}







	private static void AppendSceneUnifiedTargetPromptSection(StringBuilder prompt, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
	{
		if (prompt == null)
		{
			return;
		}
		List<string> list = new List<string>();
		HashSet<int> hashSet = new HashSet<int>();
		foreach (SceneSummonPromptTarget item in summonTargets ?? new List<SceneSummonPromptTarget>())
		{
			if (item != null && item.PromptId > 0 && !string.IsNullOrWhiteSpace(item.DisplayName) && hashSet.Add(item.PromptId))
			{
				list.Add(item.PromptId + " " + item.DisplayName.Trim() + " " + (item.LocationCode ?? "处"));
			}
		}
		foreach (SceneGuidePromptTarget item2 in guideTargets ?? new List<SceneGuidePromptTarget>())
		{
			if (item2 != null && item2.PromptId > 0 && !string.IsNullOrWhiteSpace(item2.DisplayName) && hashSet.Add(item2.PromptId))
			{
				list.Add(item2.PromptId + " " + item2.DisplayName.Trim() + " " + (item2.LocationCode ?? "处"));
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【带路与传唤NPC清单】：");
		foreach (string item3 in list)
		{
			prompt.AppendLine(item3);
		}
	}

	internal static string BuildSceneMechanismPromptSection(List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string sceneSummonClosureInstruction = null, string sceneFollowControlInstruction = null, NpcDataPacket publicActionTarget = null)
	{
		string executionInstruction = publicActionTarget == null
			? string.Empty
			: NoblePrisonerEscortBehavior.BuildSceneExecutionPromptInstruction(publicActionTarget.AgentIndex);
		string inspectionSlaughterInstruction = publicActionTarget == null
			? string.Empty
			: TroopInspectionBehavior.BuildPrisonerSlaughterPromptInstructionForExternal(publicActionTarget.AgentIndex);
		if (AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			return string.Join(
				"\n",
				new[]
				{
					executionInstruction,
					inspectionSlaughterInstruction
				}.Where(text => !string.IsNullOrWhiteSpace(text))).Trim();
		}
		if (SceneMovementController.IsPrisonBreakRescueMissionActive())
		{
			string prisonBreakInstruction = string.IsNullOrWhiteSpace(sceneFollowControlInstruction) ? "" : sceneFollowControlInstruction.Trim();
			return string.IsNullOrWhiteSpace(executionInstruction)
				? prisonBreakInstruction
				: (prisonBreakInstruction + "\n" + executionInstruction).Trim();
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(Mission.Current))
		{
			SetsSettlementSceneKind sceneKind = SettlementEntryTroopSelectionBehavior.GetSetsSettlementSceneKindForExternal(Mission.Current);
			bool massacreActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(Mission.Current);
			bool requestPending = SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(Mission.Current);
			stringBuilder.AppendLine(SetsOwnedSettlementMassacreProfile.BuildRuntimeInstruction(sceneKind, massacreActive, requestPending));
		}
		AppendSceneUnifiedTargetPromptSection(stringBuilder, sceneSummonTargets, sceneGuideTargets);
		if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
		{
			stringBuilder.AppendLine(sceneSummonClosureInstruction);
		}
		if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
		{
			stringBuilder.AppendLine(sceneFollowControlInstruction);
		}
		if (!string.IsNullOrWhiteSpace(executionInstruction))
		{
			stringBuilder.AppendLine(executionInstruction);
		}
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterInstruction))
		{
			stringBuilder.AppendLine(inspectionSlaughterInstruction);
		}
		return stringBuilder.ToString().Trim();
	}

	internal static bool CanUseSceneMechanismPostprocessForSpeaker(int speakerAgentIndex)
	{
		return ConversationActionPostprocessOwner.CanUseSceneMechanismPostprocessForSpeaker(speakerAgentIndex);
	}

	private static string InjectSceneMechanismPromptSection(string prompt, string mechanismSection, bool allowAppendWithoutMarker = false)
	{
		string text = (prompt ?? "").Trim();
		string text2 = (mechanismSection ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		const string marker = "【附加规则:scene_mechanism_actions】";
		int num = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			if (!allowAppendWithoutMarker)
			{
				return text;
			}
			return string.IsNullOrWhiteSpace(text) ? text2 : (text + "\n" + text2);
		}
		int num2 = text.IndexOf("【附加规则:", num + marker.Length, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return text.TrimEnd() + "\n" + text2;
		}
		return text.Substring(0, num2).TrimEnd() + "\n" + text2 + "\n" + text.Substring(num2).TrimStart();
	}














	internal static bool HasPartyTransferRuleContext(string extras)
	{
		return !string.IsNullOrWhiteSpace(extras) && extras.IndexOf("【附加规则:party_transfer】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null) => CreateScenePersonaEquipmentPromptCaptureAdapter().BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);

	private static string BuildHeroPregnancySelfKnowledgeForPrompt(Hero hero)
	{
		try
		{
			return hero != null && hero.IsPregnant
				? "你当前正在怀孕，并且清楚知道自己已经怀孕；这是实时事实，不能否认或遗忘"
				: "";
		}
		catch
		{
			return "";
		}
	}

	private static bool IsHeroInPlayerMainPartyForPrompt(Hero hero)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			return hero != null && hero != Hero.MainHero && mainParty != null && hero.PartyBelongedTo == mainParty;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildPlayerCommandRelationshipLineForPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (!TryResolvePlayerCommandRelationshipForPrompt(npc, hero, out var relationship))
			{
				return "";
			}
			string playerName = (GetPlayerDisplayNameForShout() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (string.Equals(relationship, "exercise_opponent", StringComparison.OrdinalIgnoreCase))
			{
				return "【上下级关系】你认得" + playerName + "：你原本来自他的队伍。当前只是军事演习中被临时编入对抗队，你仍知道他是平日里的指挥官和这次演习的组织者；回应时不要把他当陌生人、外人或真正敌军。";
			}
			if (string.Equals(relationship, "player_side", StringComparison.OrdinalIgnoreCase))
			{
				return "【上下级关系】你认得" + playerName + "：当前战斗中你和他同属己方阵营，你知道他是己方指挥者，不应把他当陌生人、外人或敌人。";
			}
			return "【上下级关系】你认得" + playerName + "：他是你当前部队的指挥官和上级。你应以士兵面对长官的口吻回应，不要把他当陌生平民或外人。";
		}
		catch
		{
			return "";
		}
	}

	private static bool ShouldIncludePlayerPartyRosterForScenePrompt(Hero observerHero, bool partyTransferTopicSelected)
	{
		if (partyTransferTopicSelected)
		{
			return true;
		}
		if (AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(observerHero))
		{
			return true;
		}
		return !IsSettlementSceneForPlayerPartyRosterSuppression();
	}

	private static bool ShouldUseCompactPlayerPartyRosterForScenePrompt(bool partyTransferTopicSelected)
	{
		if (partyTransferTopicSelected)
		{
			return false;
		}
		return IsSettlementSceneForPlayerPartyRosterSuppression();
	}

	private static bool IsSettlementSceneForPlayerPartyRosterSuppression()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement currentSettlement = MobileParty.MainParty?.CurrentSettlement;
			if (currentSettlement != null && !currentSettlement.IsHideout)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildPlayerTownPartyStayHintForPrompt(Hero playerHero)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			if (settlement == null || !settlement.IsTown)
			{
				return "";
			}
			MobileParty party = MobileParty.MainParty;
			if (party?.MemberRoster == null)
			{
				return "";
			}
			if (party.CurrentSettlement != null && party.CurrentSettlement != settlement)
			{
				return "";
			}
			int totalMen = Math.Max(0, party.MemberRoster.TotalManCount);
			if (totalMen <= 0)
			{
				return "";
			}
			string playerName = (playerHero?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = (GetPlayerDisplayNameForShout() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = "此人";
			}
			return playerName + "拥有一支" + totalMen + "人的部队暂时驻扎在城外，你不清楚他的军队的具体明细";
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPrisonerContextLineForPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			Agent agent = null;
			try
			{
				agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && npc != null && a.Index == npc.AgentIndex);
			}
			catch
			{
				agent = null;
			}
			if (CastleAftermathRuntimeBridge.IsPrisonerAgent(agent))
			{
				string castleSituation = SiegeAiInterventionBehavior.BuildCastleNpcSituationPromptForAgent(
					hero,
					agent?.Character as CharacterObject,
					npc?.AgentIndex ?? -1);
				if (!string.IsNullOrWhiteSpace(castleSituation))
				{
					return castleSituation;
				}
			}
			string heroPrisonerStatusLine = MyBehavior.BuildHeroPrisonerStatusPromptLineForExternal(hero);
			if (!string.IsNullOrWhiteSpace(heroPrisonerStatusLine))
			{
				return heroPrisonerStatusLine;
			}
			if (!IsInspectionPrisonerAgentForPrompt(agent))
			{
				return "";
			}
			string playerName = (GetPlayerDisplayNameForShout() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName) || string.Equals(playerName, "玩家", StringComparison.Ordinal))
			{
				playerName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			string captor = playerName;
			if (hero?.PartyBelongedToAsPrisoner?.LeaderHero != null)
			{
				string captorName = (hero.PartyBelongedToAsPrisoner.LeaderHero.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(captorName))
				{
					captor = captorName;
				}
			}
			return "【俘虏处境】你现在是" + captor + "的俘虏，被押在当前阅兵/检阅场景中；你不属于" + playerName + "的正常部队，也不是他的下属或友方士兵。你应清楚自己处在被控制、被看押、行动受限的状态，并按俘虏身份回应。";
		}
		catch
		{
			return "";
		}
	}

	private static bool IsInspectionPrisonerNpcForPrompt(NpcDataPacket npc)
	{
		try
		{
			if (npc == null || npc.AgentIndex < 0)
			{
				return false;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
			return IsInspectionPrisonerAgentForPrompt(agent);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryResolvePlayerCommandRelationshipForPrompt(NpcDataPacket npc, Hero hero, out string relationship)
	{
		relationship = "";
		try
		{
			if (hero != null && IsHeroInPlayerMainPartyForPrompt(hero))
			{
				relationship = "direct_command";
				return true;
			}
			Agent agent = null;
			try
			{
				agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && npc != null && a.Index == npc.AgentIndex);
			}
			catch
			{
				agent = null;
			}
			if (agent == null || agent.IsMainAgent)
			{
				return false;
			}
			CharacterObject character = agent.Character as CharacterObject;
			bool isSoldierLike = character?.IsSoldier == true || string.Equals((npc?.UnnamedRank ?? "").Trim(), "soldier", StringComparison.OrdinalIgnoreCase) || string.Equals((npc?.RoleDesc ?? "").Trim(), "士兵", StringComparison.Ordinal);
			if (!isSoldierLike && hero?.IsPlayerCompanion != true)
			{
				return false;
			}
			if (IsInspectionPrisonerAgentForPrompt(agent))
			{
				return false;
			}
			try
			{
				if (agent.Origin?.IsUnderPlayersCommand == true)
				{
					relationship = "direct_command";
					return true;
				}
			}
			catch
			{
			}
			PartyBase party = null;
			try
			{
				party = agent.Origin?.BattleCombatant as PartyBase;
			}
			catch
			{
				party = null;
			}
			if (party != null)
			{
				if (ReferenceEquals(party, PartyBase.MainParty) || ReferenceEquals(party.MobileParty, MobileParty.MainParty))
				{
					relationship = "direct_command";
					return true;
				}
				if (TryResolveMilitaryExerciseCommandRelationshipForPrompt(party, out relationship))
				{
					return true;
				}
				return false;
			}
			Mission mission = Mission.Current;
			if (mission?.PlayerTeam != null && agent.Team == mission.PlayerTeam)
			{
				relationship = "player_side";
				return true;
			}
		}
		catch
		{
			relationship = "";
		}
		return false;
	}

	private static bool TryResolveMilitaryExerciseCommandRelationshipForPrompt(PartyBase party, out string relationship)
	{
		relationship = "";
		try
		{
			if (party == null || !MilitaryExerciseBehavior.IsCurrentExerciseRuntime())
			{
				return false;
			}
			MilitaryExerciseBehavior.MilitaryExerciseRuntime runtime = MilitaryExerciseBehavior.GetCurrentRuntime();
			if (runtime == null)
			{
				return false;
			}
			if (ReferenceEquals(party, runtime.OpponentDummyParty?.Party) || ReferenceEquals(party.MobileParty, runtime.OpponentDummyParty))
			{
				relationship = "exercise_opponent";
				return true;
			}
			if (ReferenceEquals(party, runtime.HoldingDummyParty?.Party) || ReferenceEquals(party.MobileParty, runtime.HoldingDummyParty))
			{
				relationship = "direct_command";
				return true;
			}
		}
		catch
		{
			relationship = "";
		}
		return false;
	}

	private static bool IsInspectionPrisonerAgentForPrompt(Agent agent)
	{
		try
		{
			bool isLord;
			if (TroopInspectionMissionLogic.TryResolveInspectionPrisoner(agent, out isLord))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (agent?.Origin is PrisonerAgentOrigin)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool ShouldSuppressHeroJoinPartyPostprocessForScene(Hero hero)
	{
		return IsHeroInPlayerMainPartyForPrompt(hero);
	}

	private static string JoinPromptSections(params string[] sections)
	{
		if (sections == null || sections.Length == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (string section in sections)
		{
			string text = (section ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return string.Join("\n", list);
	}

	private static string BuildPlayerCustomPromptRuleBlock()
	{
		string text = (DuelSettings.GetSettings()?.PlayerCustomPromptRule ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "请遵循以下规则参与互动：\n" + text;
	}

	internal static string AppendPlayerCustomPromptRuleToSystemPrompt(string systemPrompt)
	{
		return JoinPromptSections(BuildPlayerCustomPromptRuleBlock(), systemPrompt);
	}

	private static bool ShouldHideSceneReputationForPrompt(NpcDataPacket npc, Hero hero) => PersonaEquipmentPromptCaptureAdapter.CaptureSceneReputationVisibility(npc, hero);

	private static bool IsWeaponEquipmentIndexForScenePrompt(EquipmentIndex index) => EquipmentPromptCaptureAdapter.IsWeaponEquipmentIndexForPrompt(index);

	private static void AddSceneEquipmentSummaryItem(Dictionary<string, int> counts, Dictionary<string, string> names, EquipmentIndex index, ItemObject item) => EquipmentPromptCaptureAdapter.AddEquipmentSummaryItemForPrompt(counts, names, index, item);

	private static List<string> BuildSceneEquipmentSummaryItemLines(Dictionary<string, int> counts, Dictionary<string, string> names, int maxEntries) => PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(counts, names, maxEntries);

	private static string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8) => EquipmentPromptCaptureAdapter.BuildNonHeroEquipmentSummaryForPrompt(npc, maxEntries);

	private static string BuildSceneAgentSelfActionFactForPrompt(NpcDataPacket npc, Hero hero = null)
	{
		try
		{
			if (npc == null)
			{
				return "";
			}
			var agents = Mission.Current?.Agents;
			Agent agent = (npc.AgentIndex >= 0 && agents != null)
				? agents.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex)
				: null;
			List<string> labels = agent != null ? CollectSceneAgentSelfActionLabels(agent) : new List<string>();
			if (labels.Count == 0 && IsNativeConversationTargetForActionPrompt(npc, hero))
			{
				AddSceneAgentActionLabel(labels, new HashSet<string>(labels), "正在对话");
			}
			return labels.Count > 0 ? "【当前动作】你当前状态：" + string.Join("、", labels) + "。" : "";
		}
		catch
		{
			return "";
		}
	}

	private static List<string> CollectSceneAgentSelfActionLabels(Agent agent)
	{
		List<string> labels = new List<string>();
		HashSet<string> seen = new HashSet<string>();
		if (agent == null)
		{
			return labels;
		}
		if (!IsSceneAgentPromptActive(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "倒地/受击/死亡");
			return labels;
		}
		string actionSearchText = BuildSceneAgentActionSearchText(agent);
		Agent.ActionCodeType lowerBodyAction = GetSceneAgentCurrentActionType(agent, 0);
		Agent.ActionCodeType upperBodyAction = GetSceneAgentCurrentActionType(agent, 1);
		if (IsSceneAgentSittingForPrompt(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "坐着");
		}
		if (IsSceneAgentConversingForPrompt(agent, actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在对话");
		}
		if (IsSceneAgentUsingObjectForPrompt(agent))
		{
			AddSceneAgentActionLabel(labels, seen, "正在使用物体");
		}
		if (ContainsSceneActionKeyword(actionSearchText, "dance", "dancing", "perform", "performance", "performer", "bard", "minstrel", "entertain", "sing", "song", "stage", "跳舞", "表演", "歌唱"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在跳舞/表演");
		}
		if (ContainsSceneEatingOrDrinkingKeyword(actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在喝酒/吃东西");
		}
		if (IsSceneAgentPlayingMusicForPrompt(agent, actionSearchText))
		{
			AddSceneAgentActionLabel(labels, seen, "正在演奏");
		}
		if (IsSceneAgentMovingForPrompt(agent, actionSearchText, lowerBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "正在行走/移动");
		}
		if (IsSceneAgentAttackAction(lowerBodyAction) || IsSceneAgentAttackAction(upperBodyAction) || ContainsSceneActionKeyword(actionSearchText, "fightbehavior", "combat", "attack"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在战斗/攻击");
		}
		if (IsSceneAgentDefenseAction(lowerBodyAction) || IsSceneAgentDefenseAction(upperBodyAction) || ContainsSceneActionKeyword(actionSearchText, "defend", "block", "parry"))
		{
			AddSceneAgentActionLabel(labels, seen, "正在防御/格挡");
		}
		if (IsSceneAgentDownOrHitForPrompt(agent, lowerBodyAction, upperBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "倒地/受击/死亡");
		}
		ShoutBehavior instance = CurrentInstance;
		if (instance != null)
		{
			if (instance._sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent) || ContainsSceneActionKeyword(actionSearchText, "followagentbehavior"))
			{
				AddSceneAgentActionLabel(labels, seen, "正在跟随");
			}
			if (instance._sceneMovement.IsAgentBusyWithSceneGuideErrand(agent.Index) || SceneMovementController.GetSceneGuideEscortBehavior(agent) != null)
			{
				AddSceneAgentActionLabel(labels, seen, "正在带路");
			}
			if (instance._sceneMovement.HasGuideArrivalHold(agent.Index))
			{
				AddSceneAgentActionLabel(labels, seen, "正在等待");
			}
		}
		if (labels.Count == 0 && IsSceneAgentWaitingForPrompt(agent, actionSearchText, lowerBodyAction, upperBodyAction))
		{
			AddSceneAgentActionLabel(labels, seen, "正在等待");
		}
		return labels;
	}

	private static bool IsSceneAgentPromptActive(Agent agent)
	{
		try
		{
			return agent != null && agent.IsActive() && agent.State == AgentState.Active && agent.Health > 0f
				&& !RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent);
		}
		catch
		{
			try
			{
				return agent != null && agent.IsActive();
			}
			catch
			{
				return false;
			}
		}
	}

	private static bool IsSceneAgentSittingForPrompt(Agent agent)
	{
		try
		{
			return agent != null && agent.IsSitting();
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSceneAgentConversingForPrompt(Agent agent, string actionSearchText)
	{
		if (agent == null)
		{
			return false;
		}
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance != null)
			{
				if (instance.SceneAudio.IsSpeaking(agent.Index) || instance._activeInteractionSessions.ContainsKey(agent.Index) || instance._staringAgents.Any((Agent a) => a != null && a.Index == agent.Index))
				{
					return true;
				}
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true && TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
				if (targetAgentIndex == agent.Index)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return ContainsSceneActionKeyword(actionSearchText, "conversation", "talkbehavior", "talk");
	}

	private static bool IsNativeConversationTargetForActionPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (npc == null || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var targetName))
			{
				return false;
			}
			if (hero != null && (targetHero == hero || targetCharacter?.HeroObject == hero))
			{
				return true;
			}
			if (npc.AgentIndex >= 0 && TryResolveNativeConversationAgentIndex(targetHero, targetCharacter) == npc.AgentIndex)
			{
				return true;
			}
			string npcName = GetSceneNpcIdentityNameForPrompt(npc);
			string resolvedName = (targetName ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			return !string.IsNullOrWhiteSpace(npcName) && !string.IsNullOrWhiteSpace(resolvedName) && string.Equals(npcName.Trim(), resolvedName, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSceneAgentUsingObjectForPrompt(Agent agent)
	{
		try
		{
			return agent != null && (agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSceneAgentPlayingMusicForPrompt(Agent agent, string actionSearchText)
	{
		try
		{
			if (agent?.CurrentlyUsedGameObject is PlayMusicPoint)
			{
				return true;
			}
		}
		catch
		{
		}
		return ContainsSceneActionKeyword(actionSearchText, "playmusic", "play_music", "music", "musician", "instrument", "lute", "lyre", "drum", "flute", "horn", "演奏", "乐器", "音乐");
	}

	private static bool IsSceneAgentMovingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction)
	{
		try
		{
			if (agent != null && agent.GetCurrentVelocity().LengthSquared > 0.01f)
			{
				return true;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Dash
			|| lowerBodyAction == Agent.ActionCodeType.JumpStart
			|| lowerBodyAction == Agent.ActionCodeType.Jump
			|| lowerBodyAction == Agent.ActionCodeType.JumpEnd
			|| lowerBodyAction == Agent.ActionCodeType.Mount
			|| lowerBodyAction == Agent.ActionCodeType.Dismount
			|| ContainsSceneActionKeyword(actionSearchText, "walkingbehavior", "patrol", "escortagentbehavior", "followagentbehavior", "goto", "move");
	}

	private static bool IsSceneAgentWaitingForPrompt(Agent agent, string actionSearchText, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		try
		{
			if (agent == null || agent.GetCurrentVelocity().LengthSquared > 0.01f)
			{
				return false;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Idle
			|| lowerBodyAction == Agent.ActionCodeType.Guard
			|| upperBodyAction == Agent.ActionCodeType.Idle
			|| ContainsSceneActionKeyword(actionSearchText, "idleagentbehavior", "wait");
	}

	private static bool IsSceneAgentDownOrHitForPrompt(Agent agent, Agent.ActionCodeType lowerBodyAction, Agent.ActionCodeType upperBodyAction)
	{
		try
		{
			if (agent != null && agent.IsInBeingStruckAction)
			{
				return true;
			}
		}
		catch
		{
		}
		return lowerBodyAction == Agent.ActionCodeType.Fall
			|| upperBodyAction == Agent.ActionCodeType.Fall
			|| IsSceneAgentStrikeAction(lowerBodyAction)
			|| IsSceneAgentStrikeAction(upperBodyAction);
	}

	private static bool IsSceneAgentAttackAction(Agent.ActionCodeType actionType)
	{
		return actionType == Agent.ActionCodeType.ReadyRanged
			|| actionType == Agent.ActionCodeType.ReleaseRanged
			|| actionType == Agent.ActionCodeType.ReleaseThrowing
			|| actionType == Agent.ActionCodeType.ReadyMelee
			|| actionType == Agent.ActionCodeType.ReleaseMelee
			|| actionType == Agent.ActionCodeType.Kick
			|| actionType == Agent.ActionCodeType.KickContinue
			|| actionType == Agent.ActionCodeType.KickHit
			|| actionType == Agent.ActionCodeType.WeaponBash
			|| actionType == Agent.ActionCodeType.HitObject;
	}

	private static bool IsSceneAgentDefenseAction(Agent.ActionCodeType actionType)
	{
		int value = (int)actionType;
		return (value >= (int)Agent.ActionCodeType.DefendFist && value <= (int)Agent.ActionCodeType.DefendLeftStaff)
			|| actionType == Agent.ActionCodeType.ParriedMelee
			|| actionType == Agent.ActionCodeType.BlockedMelee
			|| actionType == Agent.ActionCodeType.Guard;
	}

	private static bool IsSceneAgentStrikeAction(Agent.ActionCodeType actionType)
	{
		return actionType == Agent.ActionCodeType.StrikeLight
			|| actionType == Agent.ActionCodeType.StrikeMedium
			|| actionType == Agent.ActionCodeType.StrikeHeavy
			|| actionType == Agent.ActionCodeType.StrikeKnockBack
			|| actionType == Agent.ActionCodeType.MountStrike;
	}

	private static Agent.ActionCodeType GetSceneAgentCurrentActionType(Agent agent, int channelNo)
	{
		try
		{
			return agent != null ? agent.GetCurrentActionType(channelNo) : Agent.ActionCodeType.Other;
		}
		catch
		{
			return Agent.ActionCodeType.Other;
		}
	}

	private static string BuildSceneAgentActionSearchText(Agent agent)
	{
		if (agent == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		AppendSceneAgentActionSearchPart(stringBuilder, GetSceneAgentCurrentActionType(agent, 0).ToString());
		AppendSceneAgentActionSearchPart(stringBuilder, GetSceneAgentCurrentActionType(agent, 1).ToString());
		AppendSceneAgentCurrentAnimationName(stringBuilder, agent, 0);
		AppendSceneAgentCurrentAnimationName(stringBuilder, agent, 1);
		AppendSceneAgentUsableObjectSearchText(stringBuilder, agent);
		AppendSceneAgentNavigatorSearchText(stringBuilder, agent);
		return stringBuilder.ToString().ToLowerInvariant();
	}

	private static void AppendSceneAgentCurrentAnimationName(StringBuilder stringBuilder, Agent agent, int channelNo)
	{
		try
		{
			ActionIndexCache action = agent.GetCurrentAction(channelNo);
			if (action == ActionIndexCache.act_none)
			{
				return;
			}
			AppendSceneAgentActionSearchPart(stringBuilder, agent.ActionSet.GetAnimationName(action));
		}
		catch
		{
		}
	}

	private static void AppendSceneAgentUsableObjectSearchText(StringBuilder stringBuilder, Agent agent)
	{
		object usedObject = null;
		try
		{
			usedObject = agent?.CurrentlyUsedGameObject;
		}
		catch
		{
		}
		if (usedObject == null)
		{
			return;
		}
		AppendSceneAgentActionSearchPart(stringBuilder, usedObject.GetType().Name);
		try
		{
			WeakGameEntity gameEntity = agent.CurrentlyUsedGameObject.GameEntity;
			if (gameEntity.IsValid)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, gameEntity.Name);
			}
		}
		catch
		{
		}
		try
		{
			if (usedObject is AnimationPoint animationPoint)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.ArriveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.PairLoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LeaveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.LeftHandItem);
				AppendSceneAgentActionSearchPart(stringBuilder, animationPoint.RightHandItem);
			}
			if (usedObject is DynamicObjectAnimationPoint dynamicObjectAnimationPoint)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.ArriveAction);
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.LoopStartAction);
				AppendSceneAgentActionSearchPart(stringBuilder, dynamicObjectAnimationPoint.LeaveAction);
			}
		}
		catch
		{
		}
	}

	private static void AppendSceneAgentNavigatorSearchText(StringBuilder stringBuilder, Agent agent)
	{
		try
		{
			AgentNavigator agentNavigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			AgentBehavior activeBehavior = agentNavigator?.GetActiveBehavior();
			if (activeBehavior != null)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, activeBehavior.GetType().Name);
			}
			UsableMachine targetMachine = agentNavigator?.TargetUsableMachine;
			if (targetMachine != null)
			{
				AppendSceneAgentActionSearchPart(stringBuilder, targetMachine.GetType().Name);
				WeakGameEntity gameEntity = targetMachine.GameEntity;
				if (gameEntity.IsValid)
				{
					AppendSceneAgentActionSearchPart(stringBuilder, gameEntity.Name);
				}
			}
		}
		catch
		{
		}
	}

	private static void AppendSceneAgentActionSearchPart(StringBuilder stringBuilder, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.Append(' ').Append(value.Trim());
	}

	private static bool ContainsSceneActionKeyword(string text, params string[] keywords)
	{
		if (string.IsNullOrWhiteSpace(text) || keywords == null)
		{
			return false;
		}
		foreach (string keyword in keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword) && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsSceneEatingOrDrinkingKeyword(string text)
	{
		return ContainsSceneActionTokenKeyword(text, "drink", "drinking", "eat", "eating", "food", "beer", "ale", "wine", "mug", "cup", "bowl", "meal", "feast", "bread", "tavern_drink")
			|| ContainsSceneActionKeyword(text, "喝酒", "饮酒", "进食", "吃饭", "吃东西", "啤酒", "麦酒", "酒杯", "酒桶", "饭碗", "食物");
	}

	private static bool ContainsSceneActionTokenKeyword(string text, params string[] keywords)
	{
		if (string.IsNullOrWhiteSpace(text) || keywords == null)
		{
			return false;
		}
		foreach (string keyword in keywords)
		{
			if (ContainsSceneActionTokenKeyword(text, keyword))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsSceneActionTokenKeyword(string text, string keyword)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
		{
			return false;
		}
		int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
		while (index >= 0)
		{
			int before = index - 1;
			int after = index + keyword.Length;
			bool beforeOk = before < 0 || !char.IsLetterOrDigit(text[before]);
			bool afterOk = after >= text.Length || !char.IsLetterOrDigit(text[after]);
			if (beforeOk && afterOk)
			{
				return true;
			}
			index = text.IndexOf(keyword, index + 1, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static void AddSceneAgentActionLabel(List<string> labels, HashSet<string> seen, string label)
	{
		if (labels == null || seen == null || string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		label = label.Trim();
		if (seen.Add(label))
		{
			labels.Add(label);
		}
	}

private static string BuildCeremonyRoleFactForPrompt(int agentIndex)
	{
		try
		{
			TownExecutionMissionBehavior ceremony = Mission.Current?.GetMissionBehavior<TownExecutionMissionBehavior>();
			if (ceremony?.Request == null || ceremony.State == ExecutionSessionState.Cancelled)
			{
				return string.Empty;
			}
			Agent speaker = Mission.Current.Agents?.FirstOrDefault(agent => agent != null && agent.Index == agentIndex);
			if (speaker == null)
			{
				return string.Empty;
			}
			ExecutionRequest request = ceremony.Request;
			string charge = (request.Charge?.GetName()?.ToString() ?? string.Empty).Trim();
			string method = (request.Method?.GetName()?.ToString() ?? string.Empty).Trim();
			string victim = (request.Victim?.Name?.ToString() ?? string.Empty).Trim();
			string venue = (request.Venue?.Name?.ToString() ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(charge)) charge = "未说明的罪名";
			if (string.IsNullOrWhiteSpace(method)) method = "公开处决";
			if (string.IsNullOrWhiteSpace(victim)) victim = "死刑犯";
			if (string.IsNullOrWhiteSpace(venue)) venue = "这座城镇";
			string fact;
			if (ceremony.IsCeremonyExecutioner(speaker))
			{
				fact = "你是这场公开处决的刽子手，正在" + venue + "对" + victim + "执行" + charge + "的判决，处刑方式是" + method;
			}
			else if (ceremony.IsCeremonyVictim(speaker))
			{
				fact = "你是这场公开处决的死刑犯，因" + charge + "在" + venue + "等候" + method;
			}
			else if (ceremony.IsCeremonyGuard(speaker))
			{
				fact = "你是这场公开处决的卫兵，正在" + venue + "看押因" + charge + "等候" + method + "的" + victim;
			}
			else if (ceremony.IsCeremonyCrowd(speaker))
			{
				fact = "你是" + venue + "围观这场公开处决的平民，被处决的是因" + charge + "等候" + method + "的" + victim;
			}
			else
			{
				return string.Empty;
			}
			bool waiting = ceremony.State == ExecutionSessionState.WaitingForPlayer ||
				ceremony.State == ExecutionSessionState.Preparing;
			return waiting
				? fact + "。判决已经宣布，但处刑还没有开始。"
				: fact + "。处刑已经开始。";
		}
		catch
		{
			return string.Empty;
		}
	}

	internal static string BuildSceneSystemTopPromptIntroForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var stableIntro, out var _);
	string ordinaryVoiceContext = AfGcczShoutBridge.BuildOrdinarySpeakerVoiceContext(hero, npc);
	if (!string.IsNullOrWhiteSpace(ordinaryVoiceContext))
	{
		stableIntro = BuildSceneCompositeUserBlock("", stableIntro, ordinaryVoiceContext);
	}
	string selfActionFact = BuildSceneAgentSelfActionFactForPrompt(npc, hero);
	if (!string.IsNullOrWhiteSpace(selfActionFact))
	{
		stableIntro = (stableIntro ?? "").TrimEnd();
		stableIntro = string.IsNullOrWhiteSpace(stableIntro) ? selfActionFact : stableIntro + Environment.NewLine + selfActionFact;
	}
	return stableIntro;
}

public static string BuildHeroStableRoleContextForExternal(Hero hero)
{
	if (hero == null)
	{
		return "";
	}
	try
	{
		NpcDataPacket npc = BuildNativeConversationNpcData(hero, hero.CharacterObject);
		if (npc == null)
		{
			return "";
		}
		npc.AgentIndex = -1;
		if (string.IsNullOrWhiteSpace(npc.PersonalityDesc) || string.IsNullOrWhiteSpace(npc.BackgroundDesc))
		{
			BuildHeroPersonaFallback(hero, out var fallbackPersonality, out var fallbackBackground);
			if (string.IsNullOrWhiteSpace(npc.PersonalityDesc))
			{
				npc.PersonalityDesc = fallbackPersonality ?? "";
			}
			if (string.IsNullOrWhiteSpace(npc.BackgroundDesc))
			{
				npc.BackgroundDesc = fallbackBackground ?? "";
			}
		}
		return BuildSceneSystemTopPromptIntroForSingle(npc, hero, new List<NpcDataPacket> { npc });
	}
	catch (Exception ex)
	{
		Logger.Log("ShoutBehavior", "[CourierContext][WARN] build stable hero role context failed hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
		return "";
	}
}

private static string BuildSceneSystemTopPromptIntroForGroup(IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool partyTransferTopicSelected = false)
{
		if (npcs == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (NpcDataPacket npc in npcs)
		{
			if (npc == null)
			{
				continue;
			}
			Hero hero = null;
			if (npc.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
			}
			string fullIntro = BuildSceneNpcRoleIntroForPrompt(npc, hero, npcs, includeInventorySummary: false, includeTradePricing: false, partyTransferTopicSelected: partyTransferTopicSelected);
			SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var intro, out var _);
			string ordinaryVoiceContext = AfGcczShoutBridge.BuildOrdinarySpeakerVoiceContext(hero, npc);
			if (!string.IsNullOrWhiteSpace(ordinaryVoiceContext))
			{
				intro = BuildSceneCompositeUserBlock("", intro, ordinaryVoiceContext);
			}
			if (string.IsNullOrWhiteSpace(intro))
			{
				continue;
			}
			string selfActionFact = BuildSceneAgentSelfActionFactForPrompt(npc, hero);
			if (!string.IsNullOrWhiteSpace(selfActionFact))
			{
				intro = intro.TrimEnd() + Environment.NewLine + selfActionFact;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine(intro);
	}
	return stringBuilder.ToString().Trim();
}

internal static string BuildSceneUserRuntimeContextForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var _, out var runtimeIntro);
	return runtimeIntro;
}

internal static string BuildCompactSceneUserRuntimeContextForShortReply(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	string fullIntro = BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
	SplitSceneNpcRoleIntroSections(fullIntro, hero != null, out var _, out var runtimeIntro);
	if (string.IsNullOrWhiteSpace(runtimeIntro))
	{
		return "";
	}
	string[] lines = runtimeIntro.Replace("\r", "").Split('\n');
	List<string> keptLines = new List<string>();
	for (int i = 0; i < lines.Length; i++)
	{
		string line = (lines[i] ?? "").Trim();
		if (string.IsNullOrWhiteSpace(line))
		{
			continue;
		}
		if (line.StartsWith("你身上穿着", StringComparison.Ordinal)
			|| line.StartsWith("现在的时间是", StringComparison.Ordinal)
			|| line.StartsWith("你面前站着一个", StringComparison.Ordinal)
			|| line.StartsWith("【当前臣属关系】", StringComparison.Ordinal))
		{
			keptLines.Add(line);
		}
	}
	return string.Join("\n", keptLines).Trim();
}

private static string BuildSceneUserRuntimeContextForGroup(IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes, bool includeInventorySummary = false, bool includeTradePricing = false, bool partyTransferTopicSelected = false, MentionedWorldEntities promptMentions = null)
{
	if (npcs == null)
	{
		return "";
	}
	StringBuilder stringBuilder = new StringBuilder();
	foreach (NpcDataPacket npc in npcs)
	{
		if (npc == null)
		{
			continue;
		}
		Hero hero = null;
		if (npc.IsHero && resolvedHeroes != null)
		{
			resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
		}
		string runtimeIntro = BuildSceneUserRuntimeContextForSingle(npc, hero, npcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, promptMentions);
		if (string.IsNullOrWhiteSpace(runtimeIntro))
		{
			continue;
		}
		if (stringBuilder.Length > 0)
		{
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine(runtimeIntro);
	}
	return stringBuilder.ToString().Trim();
}

private static void SplitSceneNpcRoleIntroSections(string fullIntro, bool isHeroNpc, out string stableIntro, out string runtimeIntro)
{
	stableIntro = "";
	runtimeIntro = "";
	string[] lines = (fullIntro ?? "").Replace("\r", "").Split('\n');
	List<string> stableLines = new List<string>();
	List<string> runtimeLines = new List<string>();
	List<string> normalizedLines = lines.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).ToList();
	if (normalizedLines.Count <= 0)
	{
		return;
	}
	if (normalizedLines.Count > 0)
	{
		stableLines.Add(normalizedLines[0]);
	}
	if (normalizedLines.Count > 1)
	{
		runtimeLines.Add(normalizedLines[1]);
	}
	if (isHeroNpc)
	{
		if (normalizedLines.Count > 2)
		{
			stableLines.Add(normalizedLines[2]);
		}
		if (normalizedLines.Count > 3)
		{
			stableLines.Add(normalizedLines[3]);
		}
		if (normalizedLines.Count > 4)
		{
			stableLines.Add(normalizedLines[4]);
		}
		for (int i = 5; i < normalizedLines.Count; i++)
		{
			runtimeLines.Add(normalizedLines[i]);
		}
	}
	else
	{
		if (normalizedLines.Count > 2)
		{
			stableLines.Add(normalizedLines[2]);
		}
		for (int i = 3; i < normalizedLines.Count; i++)
		{
			runtimeLines.Add(normalizedLines[i]);
		}
	}
	stableIntro = string.Join("\n", stableLines.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	runtimeIntro = string.Join("\n", runtimeLines.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

	// This sentence is appended to the scene-facing player intro. The no-faction branch is intentional:
	// without it, NPCs tend to over-associate the player with their culture's kingdom.
	private static string BuildPlayerSceneIdentitySentenceForPrompt(Hero playerHero)
	{
		if (playerHero == null)
		{
			return "";
		}
		try
		{
			Clan clan = playerHero.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan != null && clan.IsUnderMercenaryService && kingdom != null)
			{
				string kingdomName = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(kingdomName))
				{
					return "而且他还是" + kingdomName + "的雇佣兵。";
				}
			}
		}
		catch
		{
		}
		try
		{
			Kingdom kingdom = playerHero.Clan?.Kingdom;
			string factionName = (kingdom?.Name?.ToString() ?? "").Trim();
			if (playerHero.IsFactionLeader && kingdom != null && kingdom.Leader == playerHero && !string.IsNullOrWhiteSpace(factionName))
			{
				return "而且他还是" + factionName + "的统治者。";
			}
			if (playerHero.IsLord && !playerHero.IsFactionLeader && kingdom != null && !string.IsNullOrWhiteSpace(factionName))
			{
				return "而且他还是" + factionName + "的封臣。";
			}
		}
		catch
		{
		}
		string text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "玩家", StringComparison.Ordinal))
		{
			text = (playerHero.Name?.ToString() ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		return text + "没有效忠于任何人。";
	}

	private static bool ShouldForceDetailedPlayerIntroForObserver(Hero observerHero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (observerHero == null || mainHero == null)
			{
				return false;
			}
			return observerHero.Clan != null && observerHero.Clan == mainHero.Clan;
		}
		catch
		{
			return false;
		}
	}

	private static bool DoesSceneObserverKnowPlayerIdentityForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			if (observerHero != null)
			{
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerHero);
			}
			CharacterObject observerCharacter = null;
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && observerNpc != null && a.Index == observerNpc.AgentIndex);
				observerCharacter = agent?.Character as CharacterObject;
			}
			catch
			{
			}
			if (observerCharacter?.HeroObject != null)
			{
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerCharacter.HeroObject);
			}
			string observerKey = MyBehavior.BuildRuleTargetKeyForExternal(null, observerCharacter, observerNpc?.AgentIndex ?? -1);
			string cultureId = (observerCharacter?.Culture?.StringId ?? observerNpc?.CultureId ?? "").Trim();
			return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerKey, cultureId);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildSceneObserverInlineStateForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			bool canSpeak;
			string stateText;
			if (observerHero != null)
			{
				return MyBehavior.TryGetSceneHeroInlineStateForExternal(observerHero, out stateText, out canSpeak) ? (stateText ?? "").Trim() : "";
			}
			if (observerNpc != null)
			{
				return MyBehavior.TryGetSceneUnnamedInlineStateForExternal(observerNpc.UnnamedKey, observerNpc.Name, GetSceneNpcPatienceNameForPrompt(observerNpc), out stateText, out canSpeak) ? (stateText ?? "").Trim() : "";
			}
		}
		catch
		{
		}
		return "";
	}

	private static string BuildScenePlayerIntroForPrompt(bool includeTradePricing = false, bool includePlayerPartyRoster = false) => BuildScenePlayerIntroForPrompt(null, null, includeTradePricing, includePlayerPartyRoster);

	private static string BuildScenePlayerIntroForPrompt(Hero observerHero, bool includeTradePricing = false, bool includePlayerPartyRoster = false) => BuildScenePlayerIntroForPrompt(observerHero, null, includeTradePricing, includePlayerPartyRoster);

	private static string BuildScenePlayerIntroForPrompt(Hero observerHero, NpcDataPacket observerNpc, bool includeTradePricing = false, bool includePlayerPartyRoster = false, bool useCompactPlayerPartyRoster = false) => CreateScenePersonaEquipmentPromptCaptureAdapter().BuildScenePlayerIntroForPrompt(observerHero, observerNpc, includeTradePricing, includePlayerPartyRoster, useCompactPlayerPartyRoster);

	private static IFaction ResolveNpcPerspectiveFactionForPlayerCrimePrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			if (observerHero != null)
			{
				return observerHero.Clan?.Kingdom ?? observerHero.MapFaction;
			}
			if (observerNpc == null)
			{
				return null;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == observerNpc.AgentIndex);
			PartyBase party = agent?.Origin?.BattleCombatant as PartyBase;
			return party?.MapFaction;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildPlayerFactionWarLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			IFaction npcFaction = ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc);
			Hero playerHero = Hero.MainHero;
			IFaction playerFaction = playerHero?.Clan?.Kingdom ?? playerHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction;
			if (npcFaction == null || playerFaction == null || !string.Equals(NormalizeFactionRelationLabelForPrompt(npcFaction, playerFaction), "敌对", StringComparison.Ordinal))
			{
				return "";
			}
			string npcFactionName = BuildFactionNameForPrompt(npcFaction);
			string playerFactionName = BuildFactionNameForPrompt(playerFaction);
			if (string.IsNullOrWhiteSpace(npcFactionName) || string.IsNullOrWhiteSpace(playerFactionName))
			{
				return "";
			}
			return "【当前外交状态】你所属阵营（" + npcFactionName + "）正在与面前此人所属阵营（" + playerFactionName + "）交战；政治立场上他属于敌对阵营，不要把这次会面说成双方没有冲突。";
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPlayerVassalageRelationLineForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			int targetAgentIndex = observerNpc?.AgentIndex ?? -1;
			Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			CharacterObject observerCharacter = agent?.Character as CharacterObject;
			string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
			return MyBehavior.BuildPlayerVassalageRelationPromptLineForExternal(observerHero, observerCharacter, kingdomIdOverride, "面前此人所在王国", targetAgentIndex);
		}
		catch
		{
			return "";
		}
	}

	private static Kingdom ResolveNpcPerspectiveKingdomForPrompt(Hero observerHero, NpcDataPacket observerNpc)
	{
		try
		{
			IFaction faction = ResolveNpcPerspectiveFactionForPlayerCrimePrompt(observerHero, observerNpc);
			if (faction is Kingdom kingdom)
			{
				return kingdom;
			}
			if (faction is Clan clan)
			{
				return clan.Kingdom;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string BuildFactionNameForPrompt(IFaction faction)
	{
		try
		{
			string text = (faction?.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return (faction?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPlayerCompanionPartyRoleLabelForPrompt(Hero companionHero)
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (companionHero == null || mainParty == null)
			{
				return "下属";
			}
			PartyRole role = PartyRole.None;
			if (mainParty.GetRoleHolder(PartyRole.Scout) == companionHero)
			{
				role = PartyRole.Scout;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Engineer) == companionHero)
			{
				role = PartyRole.Engineer;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Surgeon) == companionHero)
			{
				role = PartyRole.Surgeon;
			}
			else if (mainParty.GetRoleHolder(PartyRole.Quartermaster) == companionHero)
			{
				role = PartyRole.Quartermaster;
			}
			switch (role)
			{
			case PartyRole.Scout:
				return "斥候";
			case PartyRole.Engineer:
				return "工程师";
			case PartyRole.Surgeon:
				return "医师";
			case PartyRole.Quartermaster:
				return "军需官";
			default:
				return "下属";
			}
		}
		catch
		{
			return "下属";
		}
	}

	private static PartyBase ResolveLedPartyBaseForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		try
		{
			MobileParty mobileParty = hero.PartyBelongedTo;
			if (hero == Hero.MainHero)
			{
				if (mobileParty?.LeaderHero == hero && mobileParty?.Party != null)
				{
					return mobileParty.Party;
				}
				return MobileParty.MainParty?.Party;
			}
			if (mobileParty != null && mobileParty.LeaderHero == hero)
			{
				return mobileParty.Party;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void AggregateRosterForPrompt(TroopRoster roster, bool excludeHeroes,
		out int total, out int infantry, out int cavalry, out int archer, out int horseArcher,
		out List<KeyValuePair<string, int>> tieredTopN, int topN = 10)
	{
		total = 0;
		infantry = 0;
		cavalry = 0;
		archer = 0;
		horseArcher = 0;
		tieredTopN = new List<KeyValuePair<string, int>>();
		if (roster == null)
		{
			return;
		}
		Dictionary<string, int> countByKey = new Dictionary<string, int>(StringComparer.Ordinal);
		Dictionary<string, string> nameByKey = new Dictionary<string, string>(StringComparer.Ordinal);
		Dictionary<string, int> tierByKey = new Dictionary<string, int>(StringComparer.Ordinal);
		try
		{
			int rosterCount = roster.Count;
			for (int i = 0; i < rosterCount; i++)
			{
				TroopRosterElement element;
				try
				{
					element = roster.GetElementCopyAtIndex(i);
				}
				catch
				{
					continue;
				}
				CharacterObject character = element.Character;
				if (character == null || element.Number <= 0)
				{
					continue;
				}
				if (excludeHeroes && character.IsHero)
				{
					continue;
				}
				int count = Math.Max(0, element.Number);
				if (count <= 0)
				{
					continue;
				}
				total += count;
				try
				{
					string label = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(character);
					switch (label)
					{
						case "骑兵":
							cavalry += count;
							break;
						case "弓手":
							archer += count;
							break;
						case "骑射手":
							horseArcher += count;
							break;
						default:
							infantry += count;
							break;
					}
				}
				catch
				{
					infantry += count;
				}
				string key = (character.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(key))
				{
					try
					{
						key = character.Name?.ToString() ?? "";
					}
					catch
					{
						key = "";
					}
				}
				if (string.IsNullOrWhiteSpace(key))
				{
					key = "未知兵";
				}
				string displayName;
				try
				{
					displayName = (character.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					displayName = "";
				}
				if (string.IsNullOrWhiteSpace(displayName))
				{
					displayName = "未知兵";
				}
				int tier = 0;
				try
				{
					tier = Math.Max(0, character.Tier);
				}
				catch
				{
					tier = 0;
				}
				if (countByKey.ContainsKey(key))
				{
					countByKey[key] += count;
				}
				else
				{
					countByKey[key] = count;
					nameByKey[key] = displayName;
					tierByKey[key] = tier;
				}
			}
		}
		catch
		{
		}
		try
		{
			tieredTopN = countByKey
				.Select(kv => new
				{
					Key = kv.Key,
					Name = nameByKey.TryGetValue(kv.Key, out var n) ? n : kv.Key,
					Count = kv.Value,
					Tier = tierByKey.TryGetValue(kv.Key, out var t) ? t : 0
				})
				.OrderByDescending(x => x.Tier)
				.ThenByDescending(x => x.Count)
				.ThenBy(x => x.Name, StringComparer.Ordinal)
				.Take(Math.Max(1, topN))
				.Select(x => new KeyValuePair<string, int>(x.Name, x.Count))
				.ToList();
		}
		catch
		{
			tieredTopN = new List<KeyValuePair<string, int>>();
		}
	}

	private static List<string> ExtractHeroPrisonerNamesForPrompt(TroopRoster roster)
	{
		List<string> result = new List<string>();
		if (roster == null)
		{
			return result;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		try
		{
			int rosterCount = roster.Count;
			for (int i = 0; i < rosterCount; i++)
			{
				TroopRosterElement element;
				try
				{
					element = roster.GetElementCopyAtIndex(i);
				}
				catch
				{
					continue;
				}
				CharacterObject character = element.Character;
				if (character == null || element.Number <= 0 || !character.IsHero)
				{
					continue;
				}
				string name = "";
				try
				{
					name = (character.HeroObject?.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					name = "";
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					try
					{
						name = (character.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						name = "";
					}
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					name = "无名贵族";
				}
				if (seen.Add(name))
				{
					result.Add(name);
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<string> ExtractHeroMemberNamesForPrompt(TroopRoster roster, Hero leaderHero, string leaderDisplayNameOverride, int maxCount, out int totalHeroCount)
	{
		totalHeroCount = 0;
		List<string> result = new List<string>();
		if (roster == null)
		{
			return result;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		try
		{
			int rosterCount = roster.Count;
			for (int i = 0; i < rosterCount; i++)
			{
				TroopRosterElement element;
				try
				{
					element = roster.GetElementCopyAtIndex(i);
				}
				catch
				{
					continue;
				}
				CharacterObject character = element.Character;
				if (character == null || element.Number <= 0 || !character.IsHero)
				{
					continue;
				}
				Hero hero = character.HeroObject;
				if (hero == Hero.MainHero)
				{
					continue;
				}
				string key = (hero?.StringId ?? character.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(key))
				{
					key = character.Name?.ToString() ?? "";
				}
				if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}
				totalHeroCount++;
				string name = "";
				if (hero != null && leaderHero != null && hero == leaderHero && !string.IsNullOrWhiteSpace(leaderDisplayNameOverride))
				{
					name = leaderDisplayNameOverride.Trim();
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					try
					{
						name = (hero?.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						name = "";
					}
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					try
					{
						name = (character.Name?.ToString() ?? "").Trim();
					}
					catch
					{
						name = "";
					}
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					name = "无名英雄";
				}
				if (hero != null && leaderHero != null && hero == leaderHero)
				{
					name += "（领队）";
				}
				if (result.Count < Math.Max(1, maxCount))
				{
					result.Add(name);
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static string BuildPartyTroopsLineForPrompt(PartyBase partyBase, string leadingText, string noTroopsText, bool includeDetails = true, string leaderDisplayNameOverride = null)
	{
		leadingText = (leadingText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(leadingText))
		{
			leadingText = "该队伍共有";
		}
		noTroopsText = string.IsNullOrWhiteSpace(noTroopsText) ? "该队伍无可战兵力" : noTroopsText.Trim();
		try
		{
			if (partyBase == null || partyBase.MemberRoster == null)
			{
				return noTroopsText;
			}
			string shipPromptText = BuildPartyShipPromptSuffixForPrompt(partyBase);
			int rosterTotal = 0;
			try
			{
				rosterTotal = Math.Max(0, partyBase.MemberRoster.TotalManCount);
			}
			catch
			{
				rosterTotal = 0;
			}
			AggregateRosterForPrompt(partyBase.MemberRoster, excludeHeroes: true,
				out int total, out int inf, out int cav, out int arc, out int hArc,
				out List<KeyValuePair<string, int>> top, 10);
			Hero leaderHero = null;
			try
			{
				leaderHero = partyBase.LeaderHero ?? partyBase.MobileParty?.LeaderHero;
			}
			catch
			{
				leaderHero = null;
			}
			List<string> heroNames = ExtractHeroMemberNamesForPrompt(partyBase.MemberRoster, leaderHero, leaderDisplayNameOverride, 12, out int heroCount);
			if (rosterTotal <= 0)
			{
				return noTroopsText + shipPromptText;
			}
			List<string> classParts = new List<string>(4);
			if (inf > 0) classParts.Add("步兵" + inf);
			if (cav > 0) classParts.Add("骑兵" + cav);
			if (arc > 0) classParts.Add("弓兵" + arc);
			if (hArc > 0) classParts.Add("骑射" + hArc);
			StringBuilder sb = new StringBuilder();
			sb.Append(leadingText).Append(rosterTotal).Append("人部队");
			if (!includeDetails)
			{
				if (!string.IsNullOrWhiteSpace(shipPromptText))
				{
					sb.Append(shipPromptText);
				}
				return sb.ToString().Trim();
			}
			if (heroNames != null && heroNames.Count > 0)
			{
				sb.Append("；随队英雄：").Append(string.Join("、", heroNames));
				if (heroCount > heroNames.Count)
				{
					sb.Append("等").Append(heroCount).Append("人");
				}
			}
			if (total > 0 && classParts.Count > 0)
			{
				if (heroNames != null && heroNames.Count > 0)
				{
					sb.Append("；普通兵力").Append(total).Append("人（").Append(string.Join("、", classParts)).Append("）");
				}
				else
				{
					sb.Append("（").Append(string.Join("、", classParts)).Append("）");
				}
			}
			if (total > 0 && top != null && top.Count > 0)
			{
				List<string> troopParts = new List<string>(top.Count);
				foreach (KeyValuePair<string, int> kv in top)
				{
					troopParts.Add(kv.Key + "x" + kv.Value);
				}
				sb.Append("；兵种：").Append(string.Join("、", troopParts));
			}
			if (!string.IsNullOrWhiteSpace(shipPromptText))
			{
				sb.Append(shipPromptText);
			}
			return sb.ToString().Replace("\r", " ").Replace("\n", " ").Trim();
		}
		catch
		{
			return noTroopsText;
		}
	}

	private static string BuildPartyShipPromptSuffixForPrompt(PartyBase partyBase)
	{
		try
		{
			string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(partyBase?.MobileParty);
			return string.IsNullOrWhiteSpace(shipText) ? "" : ("；舰船：" + shipText);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildHeroPartyTroopsLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true, string leaderDisplayNameOverride = null)
	{
		string subject = secondPerson ? "你" : "他";
		return BuildPartyTroopsLineForPrompt(ResolveLedPartyBaseForPrompt(hero), subject + "率领", subject + "未率领部队", includeDetails, leaderDisplayNameOverride);
	}

	private static string BuildPartyPrisonersLineForPrompt(PartyBase partyBase, string subject, string noPrisonersText, bool includeDetails = true)
	{
		subject = string.IsNullOrWhiteSpace(subject) ? "该队伍" : subject.Trim();
		noPrisonersText = string.IsNullOrWhiteSpace(noPrisonersText) ? (subject + "无俘虏") : noPrisonersText.Trim();
		try
		{
			if (partyBase == null || partyBase.PrisonRoster == null)
			{
				return noPrisonersText;
			}
			List<string> heroNames = ExtractHeroPrisonerNamesForPrompt(partyBase.PrisonRoster);
			AggregateRosterForPrompt(partyBase.PrisonRoster, excludeHeroes: true,
				out int total, out int _, out int _, out int _, out int _,
				out List<KeyValuePair<string, int>> top, 10);
			if ((heroNames == null || heroNames.Count == 0) && total <= 0)
			{
				return noPrisonersText;
			}
			if (!includeDetails)
			{
				int prisonerTotal = 0;
				try
				{
					prisonerTotal = Math.Max(0, partyBase.PrisonRoster?.TotalManCount ?? 0);
				}
				catch
				{
					prisonerTotal = Math.Max(0, total + (heroNames?.Count ?? 0));
				}
				return prisonerTotal <= 0 ? noPrisonersText : (subject + "押着" + prisonerTotal + "名俘虏");
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(subject);
			if (heroNames != null && heroNames.Count > 0)
			{
				sb.Append("押着").Append(heroNames.Count).Append("名贵族俘虏：")
					.Append(string.Join("、", heroNames));
				if (top != null && top.Count > 0)
				{
					List<string> troopParts = new List<string>(top.Count);
					foreach (KeyValuePair<string, int> kv in top)
					{
						troopParts.Add(kv.Key + "x" + kv.Value);
					}
					sb.Append("；其余俘虏：").Append(string.Join("、", troopParts));
				}
			}
			else
			{
				sb.Append("押着").Append(total).Append("名俘虏");
				if (top != null && top.Count > 0)
				{
					List<string> troopParts = new List<string>(top.Count);
					foreach (KeyValuePair<string, int> kv in top)
					{
						troopParts.Add(kv.Key + "x" + kv.Value);
					}
					sb.Append("：").Append(string.Join("、", troopParts));
				}
			}
			return sb.ToString().Replace("\r", " ").Replace("\n", " ").Trim();
		}
		catch
		{
			return noPrisonersText;
		}
	}

	private static string BuildHeroPartyPrisonersLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true)
	{
		string subject = secondPerson ? "你" : "他";
		return BuildPartyPrisonersLineForPrompt(ResolveLedPartyBaseForPrompt(hero), subject, subject + "无俘虏", includeDetails);
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
	{
		if (selfNpc == null || presentNpcs == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (NpcDataPacket presentNpc in presentNpcs)
		{
			if (presentNpc == null)
			{
				continue;
			}
			if (IsSameSceneNpcForPrompt(presentNpc, selfNpc))
			{
				continue;
			}
			string text = GetSceneNpcIdentityNameForPrompt(presentNpc);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (presentNpc.IsHero)
			{
				string text2 = (presentNpc.RoleDesc ?? "").Trim();
				list.Add(string.IsNullOrWhiteSpace(text2) ? text : (text2 + text));
				continue;
			}
			list.Add(GetSceneNpcHistoryNameForPrompt(presentNpc));
		}
		if (list.Count == 0)
		{
			return "";
		}
		return "并且你旁边还站着" + string.Join("，", list) + "。";
	}

	private static string ConvertCountToChineseForPrompt(int count)
	{
		return count switch
		{
			2 => "两个",
			3 => "三个",
			4 => "四个",
			5 => "五个",
			6 => "六个",
			7 => "七个",
			8 => "八个",
			9 => "九个",
			10 => "十个",
			_ => count.ToString() + "个",
		};
	}

	private static string NormalizeFactionRelationLabelForPrompt(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return "中立";
			}
			string factionId = (faction.StringId ?? "").Trim();
			string referenceId = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(factionId) && string.Equals(factionId, referenceId, StringComparison.OrdinalIgnoreCase))
			{
				return "友方";
			}
			if (ReferenceEquals(faction, referenceFaction))
			{
				return "友方";
			}
			if (referenceFaction.IsAtWarWith(faction) || faction.IsAtWarWith(referenceFaction))
			{
				return "敌对";
			}
			return "中立";
		}
		catch
		{
			return "中立";
		}
	}

	private static string ConvertNpcSideRelationLabelForPrompt(string relation)
	{
		switch ((relation ?? "").Trim())
		{
		case "敌对":
			return "敌人";
		case "友方":
			return "友方";
		default:
			return "中立";
		}
	}

	private static void ParseCurrentScenePlaceAndSpotForPrompt(out string placeName, out string spotName)
	{
		placeName = "";
		spotName = "";
		try
		{
			string sceneDescription = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", "").Trim();
			if (string.IsNullOrWhiteSpace(sceneDescription))
			{
				return;
			}
			if (IsUnknownSceneDescriptionForPrompt(sceneDescription))
			{
				return;
			}
			int extraIndex = sceneDescription.IndexOf('|');
			if (extraIndex >= 0)
			{
				sceneDescription = sceneDescription.Substring(0, extraIndex).Trim();
			}
			if (IsUnknownSceneDescriptionForPrompt(sceneDescription))
			{
				return;
			}
			if (sceneDescription.StartsWith("你正位于", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("你正位于".Length).Trim();
				body = body.TrimEnd('。', '.', ' ');
				const string seaSuffix = "附近的海上";
				if (body.EndsWith(seaSuffix, StringComparison.Ordinal))
				{
					placeName = body.Substring(0, body.Length - seaSuffix.Length).Trim();
					spotName = "海上";
					return;
				}
				if (string.Equals(body, "海上", StringComparison.Ordinal))
				{
					spotName = "海上";
					return;
				}
			}
			if (sceneDescription.StartsWith("位于 ", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("位于 ".Length).Trim();
				int splitIndex = body.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + " 的 ".Length).Trim();
					return;
				}
			}
			if (sceneDescription.StartsWith("靠近 ", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("靠近 ".Length).Trim();
				int splitIndex = body.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + " 的 ".Length).Trim();
					return;
				}
			}
			spotName = sceneDescription;
		}
		catch
		{
			placeName = "";
			spotName = "";
		}
	}

	private static bool IsUnknownSceneDescriptionForPrompt(string sceneDescription)
	{
		string text = (sceneDescription ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		return string.Equals(text, "未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "大地图或未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "某个地方", StringComparison.Ordinal);
	}

	private static string NormalizeSettlementNameForPromptLookup(string name)
	{
		string text = (name ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf('（');
		if (num >= 0)
		{
			text = text.Substring(0, num).Trim();
		}
		int num2 = text.IndexOf('(');
		if (num2 >= 0)
		{
			text = text.Substring(0, num2).Trim();
		}
		return text;
	}

	private static Settlement FindNearestSettlementForPrompt(MobileParty referenceParty = null)
	{
		return MapSeaContextGuard.FindNearestSettlementForPrompt(referenceParty ?? MobileParty.MainParty);
	}

	private static MobileParty ResolveMapLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		try
		{
			MobileParty party = perspectiveHero?.PartyBelongedTo;
			if (party != null && party.IsActive && party.CurrentSettlement == null && party.Position.IsValid())
			{
				return party;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty != null && mainParty.CurrentSettlement == null && mainParty.Position.IsValid())
			{
				return mainParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static MobileParty ResolveSeaLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		try
		{
			MobileParty party = perspectiveHero?.PartyBelongedTo;
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				return party;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				return mainParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement ResolveSceneSettlementForPrompt(string placeName, Hero perspectiveHero = null)
	{
		Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		if (settlement != null)
		{
			return settlement;
		}
		string text = NormalizeSettlementNameForPromptLookup(placeName);
		if (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				settlement = Settlement.All?.FirstOrDefault((Settlement x) => x != null && string.Equals(NormalizeSettlementNameForPromptLookup(x.Name?.ToString()), text, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				settlement = null;
			}
		}
		return settlement ?? FindNearestSettlementForPrompt(ResolveMapLocationReferencePartyForPrompt(perspectiveHero));
	}

	private static string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			ParseCurrentScenePlaceAndSpotForPrompt(out var placeName, out var spotName);
			bool parsedPlaceName = !string.IsNullOrWhiteSpace(placeName);
			MobileParty mapReferenceParty = ResolveMapLocationReferencePartyForPrompt(perspectiveHero);
			bool seaContext = IsSeaSpotForPrompt(spotName) || ResolveSeaLocationReferencePartyForPrompt(perspectiveHero) != null;
			bool wildernessContext = !seaContext && (IsWildernessSpotForPrompt(spotName) || (!parsedPlaceName && !IsCurrentSettlementContextForPrompt()));
			if (seaContext)
			{
				spotName = "海上";
			}
			else
			{
				string terrainSpotName = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(mapReferenceParty);
				if (!string.IsNullOrWhiteSpace(terrainSpotName) && (wildernessContext || IsWildernessSpotForPrompt(spotName)))
				{
					spotName = terrainSpotName;
					wildernessContext = true;
				}
			}
			Settlement settlement = ResolveSceneSettlementForPrompt(placeName, perspectiveHero);
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = (settlement?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = "当前区域";
			}
			if (string.IsNullOrWhiteSpace(spotName))
			{
				spotName = wildernessContext ? "野外" : "某处";
			}
			if (wildernessContext && settlement == null && string.Equals(placeName, "当前区域", StringComparison.Ordinal))
			{
				return "你现在身处" + spotName + "。";
			}
			string cultureName = (settlement?.Culture?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(cultureName))
			{
				cultureName = "未知";
			}
			string rulerName = (settlement?.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(rulerName))
			{
				rulerName = "未知人物";
			}
			string clanName = (settlement?.OwnerClan?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clanName))
			{
				clanName = "未知";
			}
			if (!clanName.EndsWith("家族", StringComparison.Ordinal))
			{
				clanName += "家族";
			}
			string factionName = (settlement?.MapFaction?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(factionName))
			{
				factionName = "未知势力";
			}
			bool isRuledByPerspectiveHero = perspectiveHero != null && settlement?.OwnerClan?.Leader == perspectiveHero;
			IFaction settlementFaction = settlement?.MapFaction;
			IFaction npcReferenceFaction = perspectiveHero?.Clan?.Kingdom ?? perspectiveHero?.MapFaction ?? settlementFaction;
			IFaction playerReferenceFaction = Hero.MainHero?.Clan?.Kingdom ?? Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction;
			string npcRelation = NormalizeFactionRelationLabelForPrompt(settlementFaction, npcReferenceFaction);
			string playerRelation = NormalizeFactionRelationLabelForPrompt(settlementFaction, playerReferenceFaction);
			string playerName = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (seaContext)
			{
				if (settlement == null)
				{
					if (string.Equals(placeName, "当前区域", StringComparison.Ordinal))
					{
						return "你正位于海上。";
					}
					return "你正位于" + placeName + "附近的海上。";
				}
				if (isRuledByPerspectiveHero)
				{
					return "你正位于" + placeName + "附近的海上；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
				}
				return "你正位于" + placeName + "附近的海上；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
			}
			if (isRuledByPerspectiveHero)
			{
				if (wildernessContext)
				{
					return "你现在位于" + placeName + "附近的" + spotName + "；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
				}
				return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
			}
			if (wildernessContext)
			{
				return "你现在位于" + placeName + "附近的" + spotName + "；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
			}
			return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
		}
		catch
		{
			return "";
		}
	}

	private static bool IsWildernessSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, "野外", StringComparison.Ordinal)
			|| string.Equals(text, "平原", StringComparison.Ordinal)
			|| string.Equals(text, "森林", StringComparison.Ordinal)
			|| string.Equals(text, "丘陵山地", StringComparison.Ordinal)
			|| string.Equals(text, "雪原", StringComparison.Ordinal)
			|| string.Equals(text, "沙漠", StringComparison.Ordinal)
			|| string.Equals(text, "草原", StringComparison.Ordinal)
			|| string.Equals(text, "沼泽", StringComparison.Ordinal)
			|| string.Equals(text, "峡谷", StringComparison.Ordinal)
			|| string.Equals(text, "沙丘", StringComparison.Ordinal)
			|| string.Equals(text, "乡野", StringComparison.Ordinal)
			|| string.Equals(text, "海滩", StringComparison.Ordinal)
			|| string.Equals(text, "峭壁", StringComparison.Ordinal)
			|| string.Equals(text, "浅滩", StringComparison.Ordinal)
			|| string.Equals(text, "桥梁", StringComparison.Ordinal)
			|| text.IndexOf("野外", StringComparison.Ordinal) >= 0;
	}

	private static bool IsSeaSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, "海上", StringComparison.Ordinal)
			|| text.IndexOf("海上", StringComparison.Ordinal) >= 0;
	}

	private static bool IsCurrentSettlementContextForPrompt()
	{
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return MobileParty.MainParty?.CurrentSettlement != null;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildSettlementFlavorLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			if (settlement == null)
			{
				return "";
			}
			string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt(settlement) ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(nativeInfo))
			{
				return "";
			}
			string settlementName = (settlement.Name?.ToString() ?? "").Trim();
			string rulerName = (settlement.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			string clanName = (settlement.OwnerClan?.Name?.ToString() ?? "").Trim();
			string factionName = (settlement.MapFaction?.Name?.ToString() ?? "").Trim();
			string officialTitle = "";
			try
			{
				IFaction mapFaction = settlement.MapFaction;
				string cultureId = ((mapFaction?.Culture)?.StringId ?? "").Trim();
				if (settlement.OwnerClan?.Leader?.IsFemale ?? false)
				{
					cultureId += "_f";
				}
				officialTitle = ((mapFaction == null || !mapFaction.IsKingdomFaction || settlement.OwnerClan?.Leader == null || mapFaction.Leader != settlement.OwnerClan.Leader) ? GameTexts.FindText("str_faction_official", cultureId)?.ToString() : GameTexts.FindText("str_faction_ruler", cultureId)?.ToString());
				officialTitle = (officialTitle ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			catch
			{
				officialTitle = "";
			}
			List<string> removablePrefixes = new List<string>();
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(factionName) && !string.IsNullOrWhiteSpace(officialTitle) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + "被" + factionName + "的" + officialTitle + "，" + rulerName + "统治着。");
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + "由" + rulerName + "统治。");
				removablePrefixes.Add(settlementName + "由" + rulerName + "控制。");
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && settlement.OwnerClan == Clan.PlayerClan)
			{
				removablePrefixes.Add(settlementName + "是你的封地。");
			}
			foreach (string prefix in removablePrefixes)
			{
				if (!string.IsNullOrWhiteSpace(prefix) && nativeInfo.StartsWith(prefix, StringComparison.Ordinal))
				{
					nativeInfo = nativeInfo.Substring(prefix.Length).Trim();
					break;
				}
			}
			if (!string.IsNullOrWhiteSpace(clanName))
			{
				string familyPrefix = "所属家族：" + clanName + "。";
				if (nativeInfo.StartsWith(familyPrefix, StringComparison.Ordinal))
				{
					nativeInfo = nativeInfo.Substring(familyPrefix.Length).Trim();
				}
			}
			return nativeInfo.Trim('。', ' ') + "。";
		}
		catch
		{
			return "";
		}
	}

	private static string BuildSettlementRulerPresenceLineForPrompt()
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			Hero ruler = settlement?.OwnerClan?.Leader;
			if (settlement == null || ruler == null)
			{
				return "";
			}
			bool isPresent = false;
			try
			{
				isPresent = ruler.CurrentSettlement == settlement || ruler.PartyBelongedTo?.CurrentSettlement == settlement;
			}
			catch
			{
				isPresent = false;
			}
			return isPresent ? "当前这座城镇的统治者在该处。" : "当前这座城镇的统治者不在该处。";
		}
		catch
		{
			return "";
		}
	}

	internal static bool ContainsLiteralKeywordHit(string input, List<string> keywords)
	{
		string text = (input ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text) || keywords == null || keywords.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < keywords.Count; i++)
		{
			string text2 = (keywords[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && text.Contains(text2.ToLowerInvariant()))
			{
				return true;
			}
		}
		return false;
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
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (GiveAssetTag giveAssetTag in GiveAssetTagCodec.Extract(text2))
		{
			if (!string.IsNullOrWhiteSpace(giveAssetTag.RawTag) && hashSet.Add(giveAssetTag.RawTag))
			{
				list.Add(giveAssetTag.RawTag);
			}
		}
		text2 = GiveAssetTagCodec.StripTags(text2);
		foreach (Match item in Regex.Matches(text2, "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join(" ", list).Trim();
	}

	internal static bool HasNonMoodDeferredSceneActionTag(string text)
	{
		if (GiveAssetTagCodec.Contains(text))
		{
			return true;
		}
		foreach (Match item in Regex.Matches(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && !text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static string StripDeferredSceneMoodTags(string text)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (GiveAssetTag giveAssetTag in GiveAssetTagCodec.Extract(text))
		{
			if (!string.IsNullOrWhiteSpace(giveAssetTag.RawTag) && hashSet.Add(giveAssetTag.RawTag))
			{
				list.Add(giveAssetTag.RawTag);
			}
		}
		foreach (Match item in Regex.Matches(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && !text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase) && hashSet.Add(text2))
			{
				list.Add(text2);
			}
		}
		return string.Join(" ", list).Trim();
	}

	private static bool HasDeferredDirectGameActionTag(string text)
	{
		if (CustomPolicyAgendaActionTagRegex.IsMatch(text ?? "") || GiveAssetTagCodec.Contains(text))
		{
			return true;
		}
		return Regex.IsMatch(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:(?:PUBLIC_EXECUTION_START|GIVE_ASSET|KINGDOM_SERVICE|JOIN_MERCENARY|JOIN_VASSAL|TRADE_TRUST|KING_ABDICATE_TO_PLAYER|VASSALAGE|KINGDOM_ANNEX|AGENDA|WORLDMAP_ORDER|DUEL|ISSUE_|QUEST_TURN_IN|NOBLE_GATHERING|NOBLE_PRISONER_EXECUTE|NOBLE_EXECUTE_ESCORT|NOBLE_EXECUTE_PARTY_PRISONER|TROOP_INSPECTION_SLAUGHTER_PRISONERS|INTIMACY_INTERNAL|MEETING_TAUNT_BATTLE|LET_PLAYER_GO|ENCOUNTER_RELEASE_PLAYER|NPC_SURRENDER|SIEGE_|6|召集)[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*)\\]", RegexOptions.IgnoreCase);
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
		if (TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, targetAgentIndex, ref tags))
		{
			return true;
		}
		if (!HasDeferredDirectGameActionTag(tags))
		{
			return false;
		}
		string before = tags ?? "";
		Logger.Log("ShoutBehavior", "[DeferredPostprocess] direct_action_apply start target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agent=" + targetAgentIndex + " tags=" + before.Replace("\r", "\\r").Replace("\n", "\\n"));
		NoblePrisonerExecutionOrderBehavior.TryProcessAcceptedTag(
			targetHero ?? targetCharacter?.HeroObject,
			targetAgentIndex,
			replyIsDirectPlayerResponse,
			ref tags,
			out _);
		PublicExecutionOrderRuntime.Consume(targetAgentIndex, ref tags);
		NoblePrisonerEscortBehavior.TryProcessSceneExecutionTag(targetAgentIndex, replyIsDirectPlayerResponse, ref tags);
		if (string.IsNullOrWhiteSpace(tags))
		{
			return !string.Equals(before, tags ?? "", StringComparison.Ordinal);
		}
		_nativeGameEffects.ApplyNativeConversationActionTags(
			targetHero,
			targetCharacter,
			ref tags,
			targetAgentIndex,
			playerText,
			actionChainName: chainName,
			npcReplyTextOverride: npcReplyText,
			duelDispatchContext: duelDispatchContext);
		bool changed = !string.Equals(before, tags ?? "", StringComparison.Ordinal);
		Logger.Log("ShoutBehavior", "[DeferredPostprocess] direct_action_apply done target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agent=" + targetAgentIndex + " changed=" + changed + " remaining=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
		return changed;
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
		string text2 = ExtractContentSectionForTts((text ?? "").Replace("\r", ""));
		text2 = StripLeakedPromptContentForShout(text2);
		text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
		text2 = StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "<think\\b[^>]*>.*?</think>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\[REASONING\\].*?(?=\\[CONTENT\\]|$)", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\（.*?\\）", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\(.*?\\)", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\*\\*.*?\\*\\*", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "\\*.*?\\*", "", RegexOptions.Singleline);
		text2 = Regex.Replace(text2, "(^|\\n)\\s*【[^】\\r\\n]{1,40}】", "$1");
		text2 = StripTtsNarrationLines(text2);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		text2 = Regex.Replace(text2, "\\n{3,}", "\n\n");
		text2 = text2.Trim(' ', '\t', '\r', '\n', '[', ']', ':', '：', '，', '。', ',', ';', '；');
		return text2.Trim();
	}

	private static string ExtractContentSectionForTts(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		int num = text2.LastIndexOf("[CONTENT]", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			text2 = text2.Substring(num + "[CONTENT]".Length);
		}
		return text2.Trim();
	}

	private static string StripTtsNarrationLines(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Replace("\r", "").Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (Regex.IsMatch(text2, "^(?:OUTPUT|INPUT|REQUEST_BODY|raw_response|reasoning_content|\\[?REASONING\\]?|\\[?CONTENT\\]?)[：:]", RegexOptions.IgnoreCase))
			{
				continue;
			}
			if (Regex.IsMatch(text2, "^(?:内心|心声|心想|思考|动作|神态|表情|旁白|叙述|舞台指示|场景描写|心理活动)\\s*[：:]"))
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(text2);
		}
		return stringBuilder.ToString().Trim();
	}

	internal static string PrepareSceneHistorySpeechText(string text)
	{
		return ConversationSpeechTextRules.PrepareSceneHistorySpeechText(text);
	}

	internal static bool IsDetailedSceneSpeechPromptEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.UseDetailedSceneSpeechPrompt == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldPreserveSceneAsteriskActions()
	{
		try
		{
			return DuelSettings.GetSettings()?.PreserveSceneAsteriskActions == true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ContainsAutoGroupEndSignal(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.IndexOf("[END]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[STP]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static bool TryParseAutoGroupRelayTargetAgentIndex(string text, out int relayTargetAgentIndex)
	{
		relayTargetAgentIndex = -1;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[RELAY\\s*:\\s*(\\d+)\\]", RegexOptions.IgnoreCase);
		return match.Success && int.TryParse(match.Groups[1].Value, out relayTargetAgentIndex) && relayTargetAgentIndex >= 0;
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
		List<string> lines = new List<string>();
		if (sceneHistoryLines != null)
		{
			foreach (string sceneHistoryLine in sceneHistoryLines)
			{
				string text = (sceneHistoryLine ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !IsLeakedPromptLineForShout(text))
				{
					lines.Add(text);
				}
			}
		}
		lines = KeepAfefFactsAndRecentHistoryLines(lines, DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
		return (lines.Count == 0) ? "【当前场景公共对话与互动】\n无" : ("【当前场景公共对话与互动】\n" + string.Join("\n", lines));
	}


private static bool IsSceneWeeklyFullReportHeader(string line)
{
	string text = (line ?? "").Trim();
	return text.StartsWith("【NPC所属王国完整周报】", StringComparison.Ordinal) || text.StartsWith("【世界完整周报】", StringComparison.Ordinal) || text.StartsWith("【周边相关王国完整周报】", StringComparison.Ordinal);
}

private static string FormatSceneRuleSection(string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		return "";
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> blocks = new List<string>();
	StringBuilder current = new StringBuilder();
	for (int i = 0; i < lines.Length; i++)
	{
		string line = (lines[i] ?? "").TrimEnd();
		string trimmed = line.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			continue;
		}
		bool startsNewBlock = trimmed.StartsWith("【附加规则:", StringComparison.Ordinal) || string.Equals(trimmed, "【说明】你不必提到附加规则内的内容，除非有人问起。", StringComparison.Ordinal);
		if (startsNewBlock && current.Length > 0)
		{
			blocks.Add(current.ToString().Trim());
			current.Clear();
		}
		if (current.Length > 0)
		{
			current.AppendLine();
		}
		current.Append(trimmed);
	}
	if (current.Length > 0)
	{
		blocks.Add(current.ToString().Trim());
	}
	return string.Join("\n\n", blocks.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

private static string FormatSceneKnowledgeSection(string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		return "";
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> output = new List<string>();
	StringBuilder currentBlock = new StringBuilder();
	void FlushCurrentBlock()
	{
		if (currentBlock.Length > 0)
		{
			string value = currentBlock.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				output.Add(value);
			}
			currentBlock.Clear();
		}
	}
	for (int i = 0; i < lines.Length; i++)
	{
		string trimmed = (lines[i] ?? "").Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			continue;
		}
		if (string.Equals(trimmed, "参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
			output.Add(trimmed);
			continue;
		}
		if (trimmed.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
		}
		if (trimmed.StartsWith("【玩家外貌信息（常驻）】", StringComparison.Ordinal))
		{
			FlushCurrentBlock();
		}
		if (currentBlock.Length > 0)
		{
			currentBlock.AppendLine();
		}
		currentBlock.Append(trimmed);
	}
	FlushCurrentBlock();
	return string.Join("\n\n", output.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

internal static void SplitSceneExtraSections(string text, out string miscSection, out string ruleSection, out string knowledgeSection)
{
	miscSection = "";
	ruleSection = "";
	knowledgeSection = "";
	if (string.IsNullOrWhiteSpace(text))
	{
		return;
	}
	string[] lines = (text ?? "").Replace("\r", "").Split('\n');
	List<string> miscLines = new List<string>();
	List<string> ruleLines = new List<string>();
	List<string> knowledgeLines = new List<string>();
	bool inRuleSection = false;
	bool inKnowledgeSection = false;
	for (int i = 0; i < lines.Length; i++)
	{
		string rawLine = lines[i] ?? "";
		string trimmed = rawLine.Trim();
		if (!inKnowledgeSection && (string.Equals(trimmed, "参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) || trimmed.StartsWith("【以下是关于（", StringComparison.Ordinal) || trimmed.StartsWith("【玩家外貌信息（常驻）】", StringComparison.Ordinal)))
		{
			inRuleSection = false;
			inKnowledgeSection = true;
		}
		else if (!inKnowledgeSection && (trimmed.StartsWith("【附加规则:", StringComparison.Ordinal) || string.Equals(trimmed, "【说明】你不必提到附加规则内的内容，除非有人问起。", StringComparison.Ordinal)))
		{
			inRuleSection = true;
		}
		else if (inRuleSection && IsSceneWeeklyFullReportHeader(trimmed))
		{
			inRuleSection = false;
		}
		if (inKnowledgeSection)
		{
			knowledgeLines.Add(rawLine);
		}
		else if (inRuleSection)
		{
			ruleLines.Add(rawLine);
		}
		else
		{
			miscLines.Add(rawLine);
		}
	}
	miscSection = string.Join("\n", miscLines).Trim();
	ruleSection = FormatSceneRuleSection(string.Join("\n", ruleLines));
	knowledgeSection = FormatSceneKnowledgeSection(string.Join("\n", knowledgeLines));
}

internal static string BuildSceneSystemRuleBlock(string ruleSection, string sceneMechanismPromptSection)
{
	string text = FormatSceneRuleSection(ruleSection);
	string mechanism = FormatSceneRuleSection(sceneMechanismPromptSection);
	bool hasSceneMechanismRuleMarker = !string.IsNullOrWhiteSpace(text) && text.IndexOf("【附加规则:scene_mechanism_actions】", StringComparison.OrdinalIgnoreCase) >= 0;
	if (!string.IsNullOrWhiteSpace(mechanism) && hasSceneMechanismRuleMarker)
	{
		bool allowAppendWithoutMarker = false;
		text = string.IsNullOrWhiteSpace(text) ? mechanism : InjectSceneMechanismPromptSection(text, mechanism, allowAppendWithoutMarker);
		text = FormatSceneRuleSection(text);
	}
	else if (!string.IsNullOrWhiteSpace(mechanism)
		&& mechanism.IndexOf(TroopInspectionPrisonerSlaughterProfile.ActionTag, StringComparison.OrdinalIgnoreCase) >= 0
		&& (string.IsNullOrWhiteSpace(text)
			|| text.IndexOf(TroopInspectionPrisonerSlaughterProfile.ActionTag, StringComparison.OrdinalIgnoreCase) < 0))
	{
		string inspectionSlaughterInstruction = string.Join(
			"\n",
			mechanism
				.Replace("\r", "")
				.Split('\n')
				.Where(line => (line ?? "").IndexOf(
					TroopInspectionPrisonerSlaughterProfile.ActionTag,
					StringComparison.OrdinalIgnoreCase) >= 0))
			.Trim();
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterInstruction))
		{
			string inspectionSlaughterRule = "【附加规则:scene_mechanism_actions】\n"
				+ inspectionSlaughterInstruction;
			text = string.IsNullOrWhiteSpace(text)
				? inspectionSlaughterRule
				: (text + "\n" + inspectionSlaughterRule);
			text = FormatSceneRuleSection(text);
		}
	}
	else if (!string.IsNullOrWhiteSpace(mechanism)
		&& mechanism.IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0
		&& (string.IsNullOrWhiteSpace(text)
			|| text.IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) < 0))
	{
		string executionInstruction = string.Join(
			"\n",
			mechanism
				.Replace("\r", "")
				.Split('\n')
				.Where(line => (line ?? "").IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0))
			.Trim();
		if (!string.IsNullOrWhiteSpace(executionInstruction))
		{
			string executionRule = "【附加规则:noble_prisoner_execution】\n" + executionInstruction;
			text = string.IsNullOrWhiteSpace(text) ? executionRule : (text + "\n" + executionRule);
			text = FormatSceneRuleSection(text);
		}
	}
	return text;
}

internal static bool HasGcczImmediatePromptExtras(string baseExtras)
{
	try
	{
		return !string.IsNullOrWhiteSpace(baseExtras) && AfGcczShoutBridge.IsActive() && AfGcczShoutBridge.HasInjectedRuleBlock(baseExtras);
	}
	catch
	{
		return false;
	}
}

internal static string BuildGcczImmediateIdentityOverrideBlock(Hero contextHero, CharacterObject npcCharacter, int targetAgentIndex, string baseExtras)
{
	if (!HasGcczImmediatePromptExtras(baseExtras))
	{
		return "";
	}
	try
	{
		return (AfGcczShoutBridge.BuildImmediateReactionIdentityOverride(contextHero, npcCharacter, targetAgentIndex) ?? "").Trim();
	}
	catch
	{
		return "";
	}
}

private static string FormatSceneHistorySection(string sectionText) => HistorySectionProjectionOwner.FormatSceneHistorySection(sectionText);

private static string BuildSceneHistoryUserBlock(string scenePublicHistorySection, string privateRecentWindowSection, string persistedWithoutRecentWindow)
{
	List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
		{
			list.Add(FormatSceneHistorySection(scenePublicHistorySection));
		}
		if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
		{
			list.Add(FormatSceneHistorySection(privateRecentWindowSection));
		}
		if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
		{
			list.Add(FormatSceneHistorySection(persistedWithoutRecentWindow));
	}
	return string.Join("\n\n", list.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
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
	if (DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled())
	{
		return "";
	}
	StringBuilder stringBuilder = new StringBuilder();
	stringBuilder.AppendLine(BuildSingleNpcSceneReplyInstruction(npcName, hasMultiplePresentNpcs));
	stringBuilder.Append(BuildReplyLengthInstruction(minTokens, maxTokens));
	return stringBuilder.ToString().Trim();
}

private static string BuildReplyLengthInstruction(int minTokens, int maxTokens)
{
	if (DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled())
	{
		return "";
	}
	int num = Math.Max(1, minTokens);
	int num2 = Math.Max(num, maxTokens);
	int num3 = GetShoutThoughtMinTokens();
	string text = (num == num2) ? $"你实际要说的话，此处约{num2}字。" : $"你实际要说的话，此处不得少于{num}字，最多{num2}字。";
	if (IsShoutInnerThoughtPromptDisabled())
	{
		return "你的回复格式必须严格按照以下执行，动作要用**括起来：\n*你的动作内容*(注意不要分段）\n" + text;
	}
	return $"你的回复格式必须严格按照以下执行，其中内心思考要用()括起来，动作要用**括起来：\n（你的内心思考内容，不少于{num3}字，重点是思考#4 role=user及往后的对话历史）\n*你的动作内容*\n" + text;
}

internal static string BuildSimpleDialogueReplyLengthInstruction(int minTokens, int maxTokens)
{
	int num = Math.Max(1, minTokens);
	int num2 = Math.Max(num, maxTokens);
	return (num == num2) ? $"你实际要说的话，此处约{num2}字。" : $"你实际要说的话，此处不得少于{num}字，最多{num2}字。";
}

private static int GetShoutThoughtMinTokens()
{
	try
	{
		return Math.Max(40, DuelSettings.GetSettings()?.ShoutThoughtMinTokens ?? 200);
	}
	catch
	{
		return 200;
	}
}

private static bool IsShoutInnerThoughtPromptDisabled()
{
	try
	{
		return DuelSettings.GetSettings()?.DisableShoutInnerThoughtPrompt == true;
	}
	catch
	{
		return false;
	}
}

private static bool TryExtractReplyFormatInstruction(ref string prompt, out string instruction)
{
	instruction = "";
	string text = (prompt ?? "").Replace("\r", "");
	if (string.IsNullOrWhiteSpace(text))
	{
		prompt = "";
		return false;
	}
	string[] array = text.Split('\n');
	for (int i = 0; i <= array.Length - 4; i++)
	{
		if (!string.Equals(array[i].Trim(), "你的回复格式必须严格按照以下执行，其中内心思考要用()括起来，动作要用**括起来：", StringComparison.Ordinal))
		{
			continue;
		}
		if (!array[i + 1].Trim().StartsWith("（你的内心思考内容", StringComparison.Ordinal) || !string.Equals(array[i + 2].Trim(), "*你的动作内容*", StringComparison.Ordinal) || !array[i + 3].Trim().StartsWith("你实际要说的话，此处", StringComparison.Ordinal))
		{
			continue;
		}
		int instructionLineCount = (i + 4 < array.Length && array[i + 4].Trim().StartsWith("[RELAY:接力编号]", StringComparison.Ordinal)) ? 5 : 4;
		List<string> list = array.ToList();
		instruction = string.Join("\n", list.GetRange(i, instructionLineCount)).Trim();
		list.RemoveRange(i, instructionLineCount);
		prompt = string.Join("\n", list).Trim();
		return true;
	}
	for (int i = 0; i <= array.Length - 3; i++)
	{
		if (!string.Equals(array[i].Trim(), "你的回复格式必须严格按照以下执行，动作要用**括起来：", StringComparison.Ordinal))
		{
			continue;
		}
		if (!string.Equals(array[i + 1].Trim(), "*你的动作内容*", StringComparison.Ordinal) || !array[i + 2].Trim().StartsWith("你实际要说的话，此处", StringComparison.Ordinal))
		{
			continue;
		}
		int instructionLineCount = (i + 3 < array.Length && array[i + 3].Trim().StartsWith("[RELAY:接力编号]", StringComparison.Ordinal)) ? 4 : 3;
		List<string> list = array.ToList();
		instruction = string.Join("\n", list.GetRange(i, instructionLineCount)).Trim();
		list.RemoveRange(i, instructionLineCount);
		prompt = string.Join("\n", list).Trim();
		return true;
	}
	prompt = text.Trim();
	return false;
}

internal static void GetSceneReplyLengthLimits(DuelSettings settings, out int minTokens, out int maxTokens)
{
	minTokens = Math.Max(1, settings?.ShoutMinTokens ?? DuelSettings.DefaultShoutMinTokens);
	maxTokens = Math.Max(1, settings?.ShoutMaxTokens ?? DuelSettings.DefaultShoutMaxTokens);
	if (maxTokens < minTokens)
	{
		maxTokens = minTokens;
	}
}

private static string NormalizeScenePlayerHistoryLine(string text, string targetNpcName = "", bool useNpcNameAddress = false, float playerDistanceMeters = -1f) => HistorySectionProjectionOwner.NormalizeScenePlayerHistoryLine(text, targetNpcName, useNpcNameAddress, playerDistanceMeters, GetPlayerDisplayNameForShout());

	internal static void SplitPersistedHeroHistorySections(string persistedHeroHistory, out string privateRecentWindowSection, out string persistedWithoutRecentWindow) => HistorySectionProjectionOwner.SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);

	internal static string TrimPrivateRecentWindowForActionPostprocess(string privateRecentWindowSection, int maxTurns = 5)
	{
		string text = (privateRecentWindowSection ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Split('\n');
		int num = Math.Max(1, maxTurns);
		int num2 = 0;
		int num3 = 0;
		for (int i = array.Length - 1; i >= 0; i--)
		{
			string text2 = (array[i] ?? "").Trim();
			if (IsActionPostprocessPlayerTurnLine(text2))
			{
				num2++;
				if (num2 >= num)
				{
					num3 = i;
					break;
				}
			}
		}
		if (num2 < num)
		{
			num3 = 0;
		}
		List<string> list = new List<string>();
		string text3 = array.Length > 0 ? (array[0] ?? "").Trim() : "";
		if (IsPrivateRecentWindowHeader(text3) && num3 > 0)
		{
			list.Add(text3);
		}
		for (int j = num3; j < array.Length; j++)
		{
			string text4 = (array[j] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text4) && (list.Count == 0 || !string.Equals(list[list.Count - 1], text4, StringComparison.Ordinal)))
			{
				list.Add(text4);
			}
		}
		return string.Join("\n", list).Trim();
	}

	private static bool ContainsAfefFactMarkerForActionPostprocess(string line)
	{
		string text = StripAfefPromptScopeLabel((line ?? "").Trim());
		return text.IndexOf("[AFEF玩家行为补充]", StringComparison.Ordinal) >= 0
			|| text.IndexOf("[AFEF NPC行为补充]", StringComparison.Ordinal) >= 0
			|| text.IndexOf("【AFEF玩家行为补充】", StringComparison.Ordinal) >= 0
			|| text.IndexOf("【AFEF NPC行为补充】", StringComparison.Ordinal) >= 0;
	}

	private static bool IsActionPostprocessHistoryHeaderLine(string line)
	{
		string text = (line ?? "").Trim();
		return text.StartsWith("【", StringComparison.Ordinal) && text.EndsWith("】", StringComparison.Ordinal);
	}

	private static string StripAfefFactLinesForActionPostprocess(string historySection)
	{
		string text = (historySection ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		List<string> kept = new List<string>();
		bool hasContent = false;
		string[] lines = text.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = (lines[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(line) || ContainsAfefFactMarkerForActionPostprocess(line))
			{
				continue;
			}
			kept.Add(line);
			if (!IsActionPostprocessHistoryHeaderLine(line) && !string.Equals(line, "无", StringComparison.Ordinal))
			{
				hasContent = true;
			}
		}
		return hasContent ? string.Join("\n", kept).Trim() : "";
	}

	private static bool IsActionPostprocessPlayerTurnLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.StartsWith("【", StringComparison.Ordinal) || text.StartsWith("——", StringComparison.Ordinal))
		{
			return false;
		}
		int num = text.IndexOfAny(new char[2] { ':', '：' });
		string text2 = (num >= 0) ? text.Substring(0, num).Trim() : text;
		return text2.Equals("玩家", StringComparison.OrdinalIgnoreCase) || text2.Equals("你", StringComparison.OrdinalIgnoreCase) || text2.EndsWith("对你说", StringComparison.Ordinal) || text2.EndsWith("对NPC说", StringComparison.Ordinal);
	}

	private static string BuildSceneFirstMeetingNpcFactSection(Hero hero)
	{
		string text = MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero, persistToHistory: false);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "【AFEF NPC行为补充】\n" + text.Trim();
	}

	private static string BuildSceneFirstMeetingNpcFactSection(int agentIndex, Dictionary<int, Hero> resolvedHeroes)
	{
		if (agentIndex < 0 || resolvedHeroes == null || !resolvedHeroes.TryGetValue(agentIndex, out var value))
		{
			return "";
		}
		return BuildSceneFirstMeetingNpcFactSection(value);
	}

	private string BuildPatienceBadgeForNpc(NpcDataPacket npc, Agent liveAgent)
	{
		if (npc == null)
		{
			return "";
		}
		if (npc.IsHero)
		{
			Hero hero = null;
			try
			{
				if (liveAgent != null && liveAgent.Character is CharacterObject { HeroObject: not null } characterObject)
				{
					hero = characterObject.HeroObject;
				}
			}
			catch
			{
			}
			if (hero == null)
			{
				try
				{
					hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
				}
				catch
				{
				}
			}
			return MyBehavior.BuildScenePatienceBadgeForHeroExternal(hero);
		}
		return MyBehavior.BuildScenePatienceBadgeForUnnamedExternal(npc.UnnamedKey, npc.Name);
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

	public override void SyncData(IDataStore dataStore)
	{
		try
		{
			if (dataStore == null)
			{
				return;
			}
			// Keep revisit markers across saves so "距离上次见面 X 天" survives save/load and scene re-entry.
			Dictionary<string, string> dictionary = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage, "_sceneHeroRevisitDays_v1", "SceneHeroRevisit") : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (dataStore.IsSaving)
			{
				_sceneHeroRevisitDayStorage.Clear();
				lock (_historyLock)
				{
					foreach (KeyValuePair<string, int> sceneHeroRevisitDay in _sceneHeroRevisitDays)
					{
						if (!string.IsNullOrWhiteSpace(sceneHeroRevisitDay.Key) && sceneHeroRevisitDay.Value >= 0)
						{
							_sceneHeroRevisitDayStorage[sceneHeroRevisitDay.Key] = sceneHeroRevisitDay.Value.ToString();
						}
					}
				}
				dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage, "_sceneHeroRevisitDays_v1", "SceneHeroRevisit");
				dataStore.SyncData("_sceneHeroRevisitDays_v1", ref dictionary);
				return;
			}
			dataStore.SyncData("_sceneHeroRevisitDays_v1", ref dictionary);
			_sceneHeroRevisitDayStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "SceneHeroRevisit");
			lock (_historyLock)
			{
				_sceneHeroRevisitDays.Clear();
				if (_sceneHeroRevisitDayStorage == null)
				{
					return;
				}
				foreach (KeyValuePair<string, string> item in _sceneHeroRevisitDayStorage)
				{
					if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value) && int.TryParse(item.Value.Trim(), out var result) && result >= 0)
					{
						_sceneHeroRevisitDays[item.Key] = result;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] SyncData failed: " + ex.Message);
		}
	}

	private static void OpenNativeConversationInput(bool showMessage = true)
	{
		try
		{
			_nativeConversationInputOpen = true;
			_nativeConversationInputTargetKey = ResolveCurrentNativeConversationTargetKey();
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage("\u5df2\u6253\u5f00 AnimusForge \u81ea\u7531\u5bf9\u8bdd\u8f93\u5165\u6846\u3002", new Color(0.4f, 1f, 0.4f)));
			}
		}
		catch
		{
			_nativeConversationInputOpen = true;
			_nativeConversationInputTargetKey = "";
		}
	}

	private static void CloseNativeConversationInput(bool clearSessionHistory = false)
	{
		CompleteNativeConversationTtsPlaybackWait(null, "native_conversation_closed", force: true);
		_nativeConversationInputOpen = false;
		_nativeConversationInputTargetKey = "";
		_nativeSessionOwner.CloseInput(clearSessionHistory);
		if (clearSessionHistory) Interlocked.Exchange(ref _nativeIllustrationHistoryBoundary, 0L);
	}

	private void OnNativeIllustrationSessionLaunched(CampaignGameStarter starter)
	{
		var manager = Campaign.Current?.ConversationManager;
		if (manager == null) return;
		manager.ConversationBegin -= CaptureNativeIllustrationHistoryBoundary;
		manager.ConversationBegin += CaptureNativeIllustrationHistoryBoundary;
	}

	private static void CaptureNativeIllustrationHistoryBoundary()
	{
		Interlocked.Exchange(ref _nativeIllustrationHistoryBoundary, Interlocked.Read(ref _currentConversationEventSequence));
	}

	private void OnNativeConversationEnded(IEnumerable<CharacterObject> characters)
	{
		_shoutTradeNativeManager = null;
		_shoutTradeNativeMission = null;
		_shoutTradeNativeAgent = null;
		Interlocked.Exchange(ref _nativeIllustrationHistoryBoundary, Interlocked.Read(ref _currentConversationEventSequence));
		CloseNativeConversationInput(clearSessionHistory: false);
		ExecutePendingNativeSceneMechanismActionsAfterConversationExit("native_conversation_ended");
		TryDrainNativeConversationQueuedActions("native_conversation_ended");
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
		Interlocked.Increment(ref _sceneHistorySessionId);
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
			Interlocked.Increment(ref _sceneHistorySessionId);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] OnMissionEnded cleanup failed: " + ex.Message);
		}
	}




































	private bool IsMultiNpcSceneConversationActive()
	{
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return false;
		}
		HashSet<int> agentIndices = new HashSet<int>();
		lock (_multiSceneMovementSuppressionLock)
		{
			if (_multiSceneMovementSuppressionActive)
			{
				foreach (int agentIndex in _multiSceneMovementSuppressionAgentIndices)
				{
					if (agentIndex >= 0)
					{
						agentIndices.Add(agentIndex);
					}
				}
			}
		}
		foreach (KeyValuePair<int, SceneInteractionSession> activeInteractionSession in _activeInteractionSessions)
		{
			if (activeInteractionSession.Key >= 0 && activeInteractionSession.Value != null)
			{
				agentIndices.Add(activeInteractionSession.Key);
			}
		}
		if (agentIndices.Count < 2)
		{
			return false;
		}
		int liveParticipantCount = 0;
		foreach (int agentIndex in agentIndices)
		{
			Agent agent = mission.Agents?.FirstOrDefault(a => a != null && a.Index == agentIndex && a.IsActive());
			if (CanAgentParticipateInSceneSpeech(agent))
			{
				liveParticipantCount++;
				if (liveParticipantCount >= 2)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ResetPassiveStareTracking()
	{
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
	}

	public void UpdatePassiveStareLogic(float dt)
	{
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || _isProcessingShout)
		{
			return;
		}
        if (_shoutHotkeyChargeActive || ShoutTextInputPopup.IsOpen ||
            IsScenePresentationActiveForExternal || AnimusForgeNativeConversationOverlay.IsOpen ||
            Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			ResetPassiveStareTracking();
			return;
		}
		if (IsMultiNpcSceneConversationActive())
		{
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_currentStareTarget = null;
			UpdateCooldowns(dt);
			return;
		}
		if (_interactionGraceTimer > 0f)
		{
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_currentStareTarget = null;
			return;
		}
		Agent closestFacingAgent = ShoutUtils.GetClosestFacingAgent(6.5f);
		if (closestFacingAgent != null)
		{
			_stareTargetLostGraceTimer = 0f;
			if (closestFacingAgent == _currentStareTarget)
			{
				_stareTimer += dt;
				if (_stareTimer >= GetPassiveStareTriggerTime() && IsCooldownReady(closestFacingAgent))
				{
					TriggerPassiveReaction(closestFacingAgent);
					_stareTimer = 0f;
				}
			}
			else
			{
				_currentStareTarget = closestFacingAgent;
				_stareTimer = 0f;
			}
		}
		else if (_currentStareTarget != null)
		{
			_stareTargetLostGraceTimer += dt;
			if (_stareTargetLostGraceTimer >= STARE_TARGET_LOST_GRACE)
			{
				_currentStareTarget = null;
				_stareTimer = 0f;
				_stareTargetLostGraceTimer = 0f;
			}
		}
		else
		{
			_stareTargetLostGraceTimer = 0f;
			_stareTimer = 0f;
		}
		UpdateCooldowns(dt);
	}

	private static float GetPassiveStareTriggerTime()
	{
		try
		{
			int seconds = DuelSettings.GetSettings()?.PassiveStareTriggerSeconds ?? (int)DEFAULT_STARE_TRIGGER_TIME;
			return Math.Max(1f, Math.Min(120f, seconds));
		}
		catch
		{
			return DEFAULT_STARE_TRIGGER_TIME;
		}
	}

	private void UpdateCooldowns(float dt)
	{
		List<string> list = new List<string>(_passiveCooldowns.Keys);
		foreach (string item in list)
		{
			_passiveCooldowns[item] -= dt;
			if (_passiveCooldowns[item] <= 0f)
			{
				_passiveCooldowns.Remove(item);
			}
		}
	}

	internal static string TryGetKingdomIdOverrideFromAgent(Agent agent)
	{
		string text = null;
		try
		{
			text = (((!(agent?.Origin?.BattleCombatant is PartyBase partyBase)) ? null : partyBase.MapFaction?.StringId) ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				text = (currentSettlement?.OwnerClan?.Kingdom?.StringId ?? currentSettlement?.MapFaction?.StringId ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					Logger.Log("LoreMatch", "kingdomIdOverride_from_settlement settlement=" + currentSettlement?.StringId + " mapFaction=" + currentSettlement?.MapFaction?.StringId + " -> " + text);
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				Logger.Log("LoreMatch", "kingdomIdOverride_missing");
				text = null;
			}
		}
		catch
		{
			text = null;
		}
		return text;
	}

	private static void LogShoutLorePrequery(string phase, Agent agent, CharacterObject character, string kingdomIdOverride, string inputText, string secondaryInput = null)
	{
		try
		{
			string source = ((character != null) ? "character" : "invalid");
			string agentIdx = ((agent != null) ? agent.Index.ToString() : "-1");
			string charId = (character?.StringId ?? "").Trim();
			string cultureId = (character?.Culture?.StringId ?? "neutral").Trim().ToLowerInvariant();
			string role = "commoner";
			try
			{
				if (character != null)
				{
					role = (character.IsSoldier ? "soldier" : character.Occupation.ToString().Trim().ToLowerInvariant());
				}
			}
			catch
			{
				role = "commoner";
			}
			string kingdom = (kingdomIdOverride ?? "").Trim().ToLowerInvariant();
			string traceId = DateTime.UtcNow.Ticks.ToString() + "_" + agentIdx;
			string text = (secondaryInput ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (text.Length > 72)
			{
				text = text.Substring(0, 72);
			}
			Logger.Log("LoreMatch", $"shout_lore_prequery phase={phase} traceId={traceId} source={source} agentIndex={agentIdx} charId={charId} culture={cultureId} kingdomOverride={kingdom} role={role} inputLen={(inputText ?? "").Length} npcRecall={(string.IsNullOrWhiteSpace(text) ? "off" : "on")} secondaryLen={text.Length}");
		}
		catch
		{
		}
	}

	private string GetCooldownIdentityKey(NpcDataPacket data, int fallbackAgentIndex = -1)
	{
		if (data != null)
		{
			if (data.IsHero)
			{
				Hero hero = ResolveHeroFromAgentIndex(data.AgentIndex);
				string heroId = (hero?.StringId ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					return "hero:" + heroId;
				}
			}
			string unnamedKey = (data.UnnamedKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(unnamedKey))
			{
				int idx = data.AgentIndex;
				if (idx < 0)
				{
					idx = fallbackAgentIndex;
				}
				if (idx >= 0)
				{
					return "unnamed:" + unnamedKey + "|agent:" + idx;
				}
				return "unnamed:" + unnamedKey;
			}
			string troopId = (data.TroopId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(troopId))
			{
				int idx2 = data.AgentIndex;
				if (idx2 < 0)
				{
					idx2 = fallbackAgentIndex;
				}
				if (idx2 >= 0)
				{
					return "troop:" + troopId + "|agent:" + idx2;
				}
				return "troop:" + troopId;
			}
			if (data.AgentIndex >= 0)
			{
				return "agent:" + data.AgentIndex;
			}
		}
		if (fallbackAgentIndex >= 0)
		{
			return "agent:" + fallbackAgentIndex;
		}
		return "";
	}

	private string GetCooldownIdentityKey(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !agent.IsHuman)
		{
			return "";
		}
		try
		{
			if (agent.Character is CharacterObject { HeroObject: not null } characterObject)
			{
				string heroId = (characterObject.HeroObject?.StringId ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					return "hero:" + heroId;
				}
			}
		}
		catch
		{
		}
		NpcDataPacket data = null;
		try
		{
			data = ShoutUtils.ExtractNpcData(agent);
		}
		catch
		{
			data = null;
		}
		return GetCooldownIdentityKey(data, agent.Index);
	}

	private void ApplyInteractionGraceAndGroupCooldown(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget = null, IEnumerable<NpcDataPacket> participantsData = null)
	{
		_interactionGraceTimer = Math.Max(_interactionGraceTimer, graceSeconds);
		HashSet<string> affectedIdentityKeys = new HashSet<string>(StringComparer.Ordinal);
		if (participantsData != null)
		{
			foreach (NpcDataPacket npc in participantsData)
			{
				string identityKey = GetCooldownIdentityKey(npc, npc?.AgentIndex ?? (-1));
				if (!string.IsNullOrWhiteSpace(identityKey))
				{
					affectedIdentityKeys.Add(identityKey);
				}
			}
		}
		if (participants != null)
		{
			foreach (Agent participant in participants)
			{
				string identityKey2 = GetCooldownIdentityKey(participant);
				if (!string.IsNullOrWhiteSpace(identityKey2))
				{
					affectedIdentityKeys.Add(identityKey2);
				}
			}
		}
		if (extraTarget != null)
		{
			string identityKey3 = GetCooldownIdentityKey(extraTarget);
			if (!string.IsNullOrWhiteSpace(identityKey3))
			{
				affectedIdentityKeys.Add(identityKey3);
			}
		}
		foreach (string affectedIdentityKey in affectedIdentityKeys)
		{
			if (_passiveCooldowns.TryGetValue(affectedIdentityKey, out var currentCooldown))
			{
				_passiveCooldowns[affectedIdentityKey] = Math.Max(currentCooldown, cooldownSeconds);
			}
			else
			{
				_passiveCooldowns[affectedIdentityKey] = cooldownSeconds;
			}
		}
	}

	private bool IsCooldownReady(Agent agent)
	{
		if (agent == null)
		{
			return false;
		}
		string identityKey = GetCooldownIdentityKey(agent);
		if (string.IsNullOrWhiteSpace(identityKey))
		{
			return false;
		}
		return !_passiveCooldowns.ContainsKey(identityKey);
	}

	private List<Agent> GetPassiveCooldownGroupAgents(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (targetAgent == null || !targetAgent.IsActive() || agents == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return list;
		}
		Vec3 playerPos = Agent.Main.Position;
		Vec3 playerLook = Agent.Main.LookDirection;
		foreach (Agent agent in agents)
		{
			if (agent == null || agent == Agent.Main || !agent.IsActive() || !agent.IsHuman)
			{
				continue;
			}
			float num = agent.Position.Distance(targetAgent.Position);
			if (num > 7f)
			{
				continue;
			}
			if (agent == targetAgent || num <= 3f)
			{
				list.Add(agent);
				continue;
			}
			Vec3 v = agent.Position - playerPos;
			v.Normalize();
			if (Vec3.DotProduct(playerLook, v) > 0.866f)
			{
				list.Add(agent);
			}
		}
		if (!list.Any((Agent a) => a != null && a.Index == targetAgent.Index))
		{
			list.Add(targetAgent);
		}
		return list;
	}

	private void TriggerPassiveReaction(Agent targetAgent)
	{
		if (targetAgent == null || _isProcessingShout || IsMultiNpcSceneConversationActive())
		{
			return;
		}
		_isProcessingShout = true;
		NpcDataPacket npcData = ShoutUtils.ExtractNpcData(targetAgent);
		if (npcData == null)
		{
			_isProcessingShout = false;
			return;
		}
		string sceneDesc = ShoutUtils.GetCurrentSceneDescription();
		List<Agent> source = GetPassiveCooldownGroupAgents(targetAgent);
		List<NpcDataPacket> allNpcData = (from a in source
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		if (!allNpcData.Any((NpcDataPacket d) => d != null && d.AgentIndex == npcData.AgentIndex))
		{
			allNpcData.Add(npcData);
		}
		ApplyInteractionGraceAndGroupCooldown(PASSIVE_INTERACTION_GRACE, PASSIVE_STARE_COOLDOWN, source, targetAgent, allNpcData);
		InformationManager.DisplayMessage(new InformationMessage("你盯着 " + npcData.Name + " 看了很久...", new Color(0.7f, 0.7f, 0.7f)));
		string factText = BuildPassiveReactionFactText(targetAgent);
		try
		{
			TriggerImmediateSceneBehaviorReactionForExternal(factText, npcData.AgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] TriggerPassiveReaction immediate failed: " + ex.Message);
		}
		finally
		{
			_isProcessingShout = false;
		}
	}

	private static string BuildPassiveReactionFactText(Agent targetAgent)
	{
		if (ShouldUseCombatPassiveReactionText(targetAgent, out var armed))
		{
			bool flag = IsPassiveReactionOpponentContext();
			return armed ? BuildCombatPassiveArmedFactText(targetAgent, flag) : BuildCombatPassiveBrawlFactText(flag);
		}
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		return playerName + "看着你";
	}

	private static bool ShouldUseCombatPassiveReactionText(Agent targetAgent, out bool armed)
	{
		armed = false;
		try
		{
			if (IsSceneConversationMissionEnding())
			{
				return false;
			}
			if (IsMeetingPseudoCombatContext())
			{
				return false;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return false;
			}
			bool flag = IsActiveSceneConversationDuelCombat();
			if (!flag)
			{
				try
				{
					MissionFightHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionFightHandler>();
					flag = missionBehavior != null && missionBehavior.IsThereActiveFight();
				}
				catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				try
				{
					if (AreAgentsHostileForSceneConversation(main, targetAgent))
					{
						flag = true;
					}
				}
				catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				return false;
			}
			armed = IsAgentUsingRealWeaponForPassiveReaction(main) || IsAgentUsingRealWeaponForPassiveReaction(targetAgent);
			return true;
		}
		catch
		{
			armed = false;
			return false;
		}
	}

	private static bool IsPassiveReactionOpponentContext()
	{
		try
		{
			if (IsActiveSceneConversationDuelCombat())
			{
				return true;
			}
			if (IsArenaLikePassiveReactionContext())
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsArenaLikePassiveReactionContext()
	{
		try
		{
			if (DuelBehavior.IsArenaMissionActive)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text = (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
			if (text == "arena")
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (Mission.Current?.SceneName ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2) && text2.Contains("arena"))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text3 = (ShoutUtils.GetCurrentSceneDescription() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && text3.IndexOf("竞技场", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return HasMissionBehaviorTypeNameForPassiveReaction("ArenaPracticeFightMissionController", "TournamentFightMissionController", "ArenaDuelMissionController", "TournamentJoustingMissionController", "TournamentArcheryMissionController");
	}

	private static bool HasMissionBehaviorTypeNameForPassiveReaction(params string[] typeNames)
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null || typeNames == null || typeNames.Length == 0)
			{
				return false;
			}
			foreach (MissionBehavior missionBehavior in current.MissionBehaviors)
			{
				string text = missionBehavior?.GetType()?.Name;
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				for (int i = 0; i < typeNames.Length; i++)
				{
					if (string.Equals(text, typeNames[i], StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildCombatPassiveBrawlFactText(bool opponentContext)
	{
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你和" + playerName + "只是对手，正在赤手较量") : ("[AFEF NPC行为补充] 你和" + playerName + "互为敌人，正在互相殴打");
	}

	private static string BuildCombatPassiveArmedFactText(Agent targetAgent, bool opponentContext)
	{
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		string text = TryGetActiveWeaponDisplayNameForPassiveReaction(targetAgent);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "武器";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你现在拿着" + text + "与" + playerName + "作为对手激烈交锋，但还未决出胜负") : ("[AFEF NPC行为补充] 你现在正拿着" + text + "与" + playerName + "作为敌人拼杀，还未决出胜负");
	}

	internal static string BuildFallbackSceneTauntSpeech(bool escalatedToFight)
	{
		return escalatedToFight ? "少废话，既然你想找打，那就来吧。" : "嘴巴放干净点，别逼我动手。";
	}

	internal static string BuildCombatActiveShoutExtraFact(Agent targetAgent)
	{
		try
		{
			if (!ShouldUseCombatPassiveReactionText(targetAgent, out var armed))
			{
				return "";
			}
			bool opponentContext = IsPassiveReactionOpponentContext();
			return armed ? BuildCombatPassiveArmedFactText(targetAgent, opponentContext) : BuildCombatPassiveBrawlFactText(opponentContext);
		}
		catch
		{
			return "";
		}
	}

	private static bool IsActiveSceneConversationDuelCombat()
	{
		try
		{
			if (DuelBehavior.IsDuelEnded)
			{
				return false;
			}
			if (DuelBehavior.IsArenaMissionActive)
			{
				return true;
			}
			if (!DuelBehavior.IsFormalDuelActive)
			{
				return false;
			}
			return !DuelBehavior.IsFormalDuelPreFightActive;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMeetingPseudoCombatContext()
	{
		try
		{
			return MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated && !IsActiveSceneConversationDuelCombat();
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSceneConversationMissionEnding()
	{
		try
		{
			Mission current = Mission.Current;
			return current != null && (current.IsMissionEnding || current.MissionEnded);
		}
		catch
		{
			return false;
		}
	}

	private static string TryGetActiveWeaponDisplayNameForPassiveReaction(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return "";
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(agent, primaryWieldedItemIndex, out var weaponName))
			{
				return weaponName;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(agent, offhandWieldedItemIndex, out weaponName))
			{
				return weaponName;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[equipmentIndex]))
				{
					continue;
				}
				string text3 = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					return text3.Trim();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsAgentUsingRealWeaponForPassiveReaction(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (IsRealWeaponWieldedSlotForPassiveReaction(agent, primaryWieldedItemIndex))
			{
				return true;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return IsRealWeaponWieldedSlotForPassiveReaction(agent, offhandWieldedItemIndex);
		}
		catch
		{
			return false;
		}
	}

	private static bool CanAgentUseSceneLipSync(Agent agent, out string reason)
	{
		reason = "unknown";
		try
		{
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				reason = "agent_not_participating";
				return false;
			}
			if (Mission.Current?.Scene == null)
			{
				reason = "mission_scene_unavailable";
				return false;
			}
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				reason = "main_agent_unavailable";
				return false;
			}
			if (agent.AgentVisuals == null)
			{
				reason = "agent_visuals_missing";
				return false;
			}
			if (IsNativeConversationLipSyncAgent(agent))
			{
				reason = "native_conversation_ok";
				return true;
			}
			float distanceSquared = agent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2);
			if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared))
			{
				reason = "distance_invalid";
				return false;
			}
			if (distanceSquared > LIP_SYNC_SAFE_MAX_DISTANCE * LIP_SYNC_SAFE_MAX_DISTANCE)
			{
				reason = $"distance={Math.Sqrt(distanceSquared):0.00}>{LIP_SYNC_SAFE_MAX_DISTANCE:0.0}";
				return false;
			}
			reason = "ok";
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static long RegisterNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, float estimatedDurationSeconds, int textLength)
	{
		if (request == null || request.IsCancellationRequested) { return 0L; }
		int agentIndex = request.AgentIndex;
		TaskCompletionSource<bool> oldTcs = null;
		int timeoutMs = ResolveNativeConversationTtsPlaybackWaitTimeoutMs(estimatedDurationSeconds, textLength);
		TaskCompletionSource<bool> newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (request.IsCancellationRequested) { return 0L; }
			oldTcs = _nativeConversationTtsPlaybackWaitTcs;
			_nativeConversationTtsPlaybackRequest = request;
			_nativeConversationTtsPlaybackWaitTcs = newTcs;
			_nativeConversationTtsPlaybackWaitAgentIndex = agentIndex;
			_nativeConversationTtsPlaybackWaitTimeoutMs = timeoutMs;
			_nativeConversationTtsPlaybackWaitToken++;
			token = _nativeConversationTtsPlaybackWaitToken;
		}
		try
		{
			oldTcs?.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] registered native playback wait. agentIndex=" + agentIndex + ", timeoutMs=" + timeoutMs + ", token=" + token);
		return token;
	}

	private static int ResolveNativeConversationTtsPlaybackWaitTimeoutMs(float estimatedDurationSeconds, int textLength)
	{
		double seconds = 0.0;
		if (estimatedDurationSeconds > 0f && !float.IsNaN(estimatedDurationSeconds) && !float.IsInfinity(estimatedDurationSeconds))
		{
			seconds = estimatedDurationSeconds;
		}
		else
		{
			seconds = Math.Max(1.0, Math.Max(0, textLength) * 0.05);
		}
		seconds += 30.0;
		int timeoutMs = (int)Math.Round(seconds * 1000.0);
		return Math.Max(NativeConversationTtsPlaybackWaitMinTimeoutMs, Math.Min(NativeConversationTtsPlaybackWaitMaxTimeoutMs, timeoutMs));
	}

	private static void ScheduleNativeConversationTypewriterPlaybackFallback(long waitToken, int agentIndex, float estimatedDurationSeconds, int textLength)
	{
		ShoutBehavior owner = CurrentInstance;
		TtsEngine.PlaybackRequest request;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (owner == null || _nativeConversationTtsPlaybackWaitToken != waitToken
				|| _nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackRequest == null)
			{
				return;
			}
			request = _nativeConversationTtsPlaybackRequest;
		}
		int delayMs = ResolveNativeConversationTypewriterFallbackDelayMs(estimatedDurationSeconds, textLength);
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(delayMs).ConfigureAwait(false);
				owner._mainThreadActions.Enqueue(delegate
				{
					try
					{
					if (!ReferenceEquals(CurrentInstance, owner)) { return; }
					bool started;
					// Keep the owner -> native-wait lock order. Registration of B cannot
					// replace A between its last identity check and the typewriter release.
					lock (owner._ttsBubbleSyncLock)
					{
						if (!owner.IsTtsPlaybackRequestCurrent(request)) { return; }
						lock (_nativeConversationTtsPlaybackWaitLock)
						{
							if (!ReferenceEquals(_nativeConversationTtsPlaybackRequest, request)
								|| !IsNativeConversationTtsPlaybackWaitToken(waitToken, agentIndex)
								|| !ConversationHelper.IsTypewriterWaitingForPlayback)
							{
								return;
							}
							started = ConversationHelper.StartTypewriterPlaybackIfWaiting(estimatedDurationSeconds);
						}
					}
					if (started)
					{
						Logger.Log("NativeConversation", "[TTS] typewriter playback fallback released waiting text. agentIndex=" + agentIndex + ", token=" + waitToken + ", delayMs=" + delayMs);
					}
					}
					catch (Exception ex)
					{
						Logger.Log("NativeConversation", "[TTS] typewriter playback fallback failed: " + ex.Message);
					}
				});
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversation", "[TTS] typewriter playback fallback failed: " + ex.Message);
			}
		});
	}

	private static int ResolveNativeConversationTypewriterFallbackDelayMs(float estimatedDurationSeconds, int textLength)
	{
		double seconds = 12.0;
		if (estimatedDurationSeconds > 0f && !float.IsNaN(estimatedDurationSeconds) && !float.IsInfinity(estimatedDurationSeconds))
		{
			seconds = Math.Max(seconds, Math.Min(45.0, estimatedDurationSeconds + 4.0));
		}
		else if (textLength > 0)
		{
			seconds = Math.Max(seconds, Math.Min(45.0, textLength * 0.05 + 4.0));
		}
		return (int)Math.Round(seconds * 1000.0);
	}

	private static bool IsNativeConversationTtsPlaybackWaitToken(long token, int agentIndex)
	{
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackWaitToken != token || _nativeConversationTtsPlaybackRequest == null || _nativeConversationTtsPlaybackRequest.IsCancellationRequested)
			{
				return false;
			}
			int expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			return expectedAgentIndex == agentIndex || (expectedAgentIndex < 0 && agentIndex < 0);
		}
	}

	private static bool CompleteNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, string reason, bool force = false)
	{
		int agentIndex = request?.AgentIndex ?? int.MinValue;
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			bool matches = force || (request != null && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request));
			if (!matches)
			{
				return false;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			token = _nativeConversationTtsPlaybackWaitToken;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait. reason=" + (reason ?? "") + ", agentIndex=" + agentIndex + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
		return true;
	}

	private static void CompleteNativeConversationTtsPlaybackWaitByToken(long token, string reason)
	{
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackWaitToken != token)
			{
				return;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait by token. reason=" + (reason ?? "") + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
	}

	private static bool IsNativeConversationTtsPlaybackWaitRequest(TtsEngine.PlaybackRequest request)
	{
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			return request != null && !request.IsCancellationRequested && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request);
		}
	}

	private static bool IsNativeConversationLipSyncAgentIndex(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return false;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			return IsNativeConversationLipSyncAgent(agent);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNativeConversationLipSyncAgent(Agent agent)
	{
		if (agent == null || !_nativeConversationInputOpen)
		{
			return false;
		}
		try
		{
			var conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			Agent conversationAgent = conversationManager.OneToOneConversationAgent as Agent;
			if (conversationAgent != null && conversationAgent.Index == agent.Index)
			{
				return true;
			}
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				return false;
			}
			return IsValidNativeConversationTargetAgent(agent, targetHero, targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	private static bool CanAgentUseSceneLipSyncExternal(int agentIndex, out string reason)
	{
		reason = "agent_index_invalid";
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return false;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			if (agent == null)
			{
				reason = "agent_missing";
				return false;
			}
			return CanAgentUseSceneLipSync(agent, out reason);
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex, out string weaponName)
	{
		weaponName = "";
		try
		{
			if (!IsRealWeaponWieldedSlotForPassiveReaction(agent, equipmentIndex))
			{
				return false;
			}
			string text = null;
			try
			{
				text = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
			}
			catch
			{
				text = null;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				try
				{
					text = agent.SpawnEquipment[equipmentIndex].Item?.Name?.ToString();
				}
				catch
				{
					text = null;
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			weaponName = text.Trim();
			return weaponName.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex)
	{
		try
		{
			if (agent == null || equipmentIndex == EquipmentIndex.None || equipmentIndex < EquipmentIndex.WeaponItemBeginSlot || equipmentIndex >= EquipmentIndex.NumAllWeaponSlots)
			{
				return false;
			}
			if (IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[equipmentIndex]))
			{
				return true;
			}
			try
			{
				return IsRealWeaponEquipmentElementForPassiveReaction(agent.SpawnEquipment[equipmentIndex]);
			}
			catch
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponMissionWeaponForPassiveReaction(MissionWeapon missionWeapon)
	{
		try
		{
			WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
			return currentUsageItem != null && !currentUsageItem.IsShield;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponEquipmentElementForPassiveReaction(EquipmentElement equipmentElement)
	{
		try
		{
			ItemObject item = equipmentElement.Item;
			if (item == null)
			{
				return false;
			}
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return primaryWeapon != null && !primaryWeapon.IsShield && item.Type != ItemObject.ItemTypeEnum.Shield;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildAutoGroupChatReplyInstruction(string npcName, List<NpcDataPacket> allNpcData)
	{
		string text = (npcName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		string text2 = string.Join("、", (allNpcData ?? new List<NpcDataPacket>()).Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.Select(GetSceneNpcHistoryNameForPrompt)
			.Where((string name) => !string.IsNullOrWhiteSpace(name) && !string.Equals(name.Trim(), text, StringComparison.Ordinal))
			.Where((string name) => !string.IsNullOrWhiteSpace(name))
			.Distinct()
			.Take(5));
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "周围的人";
		}
		return text + "现在正在和" + text2 + "继续聊天，玩家暂时没有插话。请只以" + text + "的身份，自然接续【当前场景公共对话与互动】里最新的内容，尽量不要附和别人，要像一个独立的人";
	}

	private static string NormalizeSceneHistoryLineForLoreQuery(string line)
	{
		string text = (line ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text) || IsLeakedPromptLineForShout(text))
		{
			return "";
		}
		if (text.StartsWith("[系统事实] ", StringComparison.Ordinal))
		{
			text = text.Substring("[系统事实] ".Length).Trim();
		}
		return text;
	}

	private static string BuildRecentSceneLoreQueryText(List<string> historyLines, int maxLines = 3, int maxChars = 280)
	{
		if (historyLines == null || historyLines.Count == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		for (int num = historyLines.Count - 1; num >= 0 && list.Count < Math.Max(1, maxLines); num--)
		{
			string text = NormalizeSceneHistoryLineForLoreQuery(historyLines[num]);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		list.Reverse();
		string text2 = string.Join("\n", list).Trim();
		if (text2.Length > maxChars)
		{
			text2 = text2.Substring(text2.Length - maxChars).Trim();
		}
		return text2;
	}

	private static string BuildAutoGroupPatienceInstruction()
	{
		return "【续聊耐心规则】：请结合上面的耐心/状态信息决定是否继续聊。若你已经明显无聊、冷淡、恼火，或觉得话题正在变干，请优先缩短回复。续聊阶段不要输出任何内部动作标签。";
	}

	private string BuildAutoGroupLoreContextForSpeaker(NpcDataPacket speakerNpc, Agent speakerAgent, CharacterObject speakerCharacter, Hero speakerHero, string kingdomIdOverride, List<string> visibleHistoryLines)
	{
		try
		{
			string inputText = BuildRecentSceneLoreQueryText(visibleHistoryLines);
			string secondaryInput = GetLatestSceneNpcUtterance((speakerNpc != null) ? speakerNpc.AgentIndex : (-1));
			if (string.IsNullOrWhiteSpace(inputText))
			{
				inputText = secondaryInput;
				secondaryInput = null;
			}
			if (string.IsNullOrWhiteSpace(inputText))
			{
				return "";
			}
			LogShoutLorePrequery("auto_group_round", speakerAgent, speakerCharacter, kingdomIdOverride, inputText, secondaryInput);
			if (speakerHero != null)
			{
				return AIConfigHandler.GetLoreContext(inputText, speakerHero, secondaryInput);
			}
			if (speakerCharacter != null)
			{
				return AIConfigHandler.GetLoreContext(inputText, speakerCharacter, kingdomIdOverride, secondaryInput);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[AutoGroupChat] lore query failed: " + ex.Message);
		}
		return "";
	}

	private static bool _nativeConversationInputOpen;

	private static string _nativeConversationInputTargetKey = "";

	private static readonly object _nativeConversationTtsPlaybackWaitLock = new object();

	private static TaskCompletionSource<bool> _nativeConversationTtsPlaybackWaitTcs;

	private static int _nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;

	private static int _nativeConversationTtsPlaybackWaitTimeoutMs = 0;

	private static long _nativeConversationTtsPlaybackWaitToken = 0L;
	private static TtsEngine.PlaybackRequest _nativeConversationTtsPlaybackRequest;

	private const int NativeConversationTtsPlaybackWaitMinTimeoutMs = 30000;

	private const int NativeConversationTtsPlaybackWaitMaxTimeoutMs = 300000;

	private static readonly NativeConversationSessionOwner _nativeSessionOwner = new NativeConversationSessionOwner(NextConversationEventSequence, DuelSettings.DailyConversationHistoryLineLimitMax);





	private static ShoutBehavior _freezeWatchdogDiagnosticInstance;

	internal static string GetFreezeWatchdogDiagnosticSnapshot()
	{
		StringBuilder sb = new StringBuilder();
		try { _nativeSessionOwner.AppendDiagnostics(sb); }
		catch { sb.Append("nativeHistory=unavailable"); }
		try
		{
			ShoutBehavior instance = Volatile.Read(ref _freezeWatchdogDiagnosticInstance);
			if (instance != null)
			{
				instance.AppendFreezeWatchdogDiagnosticSnapshot(sb);
			}
			else
			{
				sb.Append(" sceneHistory=instance_unavailable");
			}
		}
		catch
		{
			sb.Append(" sceneHistory=unavailable");
		}
		return sb.ToString();
	}

	private void AppendFreezeWatchdogDiagnosticSnapshot(StringBuilder sb)
	{
		if (sb == null)
		{
			return;
		}
		bool historyLockTaken = false;
		try
		{
			historyLockTaken = Monitor.TryEnter(_historyLock);
			if (historyLockTaken)
			{
				SceneHistoryOwner.AppendDiagnostics(sb);
			}
			else
			{
				sb.Append(" sceneHistory=busy");
			}
		}
		finally
		{
			if (historyLockTaken)
			{
				Monitor.Exit(_historyLock);
			}
		}
		try
		{
			sb.Append(" mainThreadActions=").Append(_mainThreadActions.Count);
		}
		catch
		{
			sb.Append(" mainThreadActions=unavailable");
		}
		if (_sceneSpeechQueueOwner.TryGetSnapshot(out int speechQueueCount, out bool speechWorkerRunning))
		{
			sb.Append(" sceneSpeechQueue=").Append(speechQueueCount)
				.Append(" sceneSpeechWorker=").Append(speechWorkerRunning ? 1 : 0);
		}
		else
		{
			sb.Append(" sceneSpeechQueue=busy");
		}
	}

	public static bool IsNativeConversationInputOpenForExternal()
	{
		try
		{
			if (!_nativeConversationInputOpen || CurrentInstance == null || Campaign.Current?.ConversationManager?.IsConversationInProgress != true || PlayerEncounterCompat.IsInPostBattleResultFlow())
			{
				return false;
			}
			string currentKey = ResolveCurrentNativeConversationTargetKey();
			return string.IsNullOrWhiteSpace(_nativeConversationInputTargetKey) || string.IsNullOrWhiteSpace(currentKey) || string.Equals(_nativeConversationInputTargetKey, currentKey, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public static bool CanSubmitNativeConversationForExternal()
	{
		try
		{
			return CurrentInstance != null && Campaign.Current?.ConversationManager?.IsConversationInProgress == true && !PlayerEncounterCompat.IsInPostBattleResultFlow() && TryResolveNativeConversationTarget(out var _, out var _, out var _);
		}
		catch
		{
			return false;
		}
	}

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

	public static bool ShouldSuppressNativeConversationVisibleStreamingForTtsExternal()
	{
		try
		{
			if (CurrentInstance == null || !_nativeConversationInputOpen || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			return settings != null && settings.EnableTtsSpeech && settings.TtsVolcDedicatedEnabled;
		}
		catch
		{
			return false;
		}
	}

	public static bool ShouldUseMapConversationTableauPlaybackForNativeTtsExternal()
	{
		try
		{
			if (CurrentInstance == null || !_nativeConversationInputOpen || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			if (Mission.Current != null)
			{
				return false;
			}
			return IsMapConversationMission(CampaignMission.Current);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMapConversationMission(ICampaignMission campaignMission)
	{
		try
		{
			string typeName = campaignMission?.GetType()?.FullName ?? "";
			return typeName.IndexOf("MapConversation", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryQueueNativeMapConversationTableauTtsPlayback(TtsEngine.PlaybackRequest request, string wavPath, string xmlPath, float durationSecs)
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null || !instance.IsTtsPlaybackRequestCurrent(request) || !ShouldUseMapConversationTableauPlaybackForNativeTtsExternal())
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
			{
				return false;
			}
			RunTtsMainThreadEventStep(delegate
			{
				try
				{
					if (!instance.IsTtsPlaybackRequestCurrent(request) || !ShouldUseMapConversationTableauPlaybackForNativeTtsExternal())
					{
						return;
					}
					ICampaignMission currentMission = CampaignMission.Current;
					if (!IsMapConversationMission(currentMission))
					{
						return;
					}
					if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
					{
						return;
					}
					currentMission.OnConversationPlay("", "", "", "", wavPath);
					ConversationHelper.StartTypewriterPlaybackIfWaiting(durationSecs);
					ScheduleNativeMapConversationTtsFileCleanup(wavPath, xmlPath, durationSecs);
					Logger.Log("NativeConversation", "[TTS] queued map conversation tableau playback. wav=" + System.IO.Path.GetFileName(wavPath) + ", duration=" + durationSecs.ToString("F2"));
				}
				catch (Exception ex)
				{
					if (!instance.IsTtsPlaybackRequestCurrent(request)) { return; }
					Logger.Log("NativeConversation", "[TTS] map conversation tableau playback failed: " + ex.Message);
					ConversationHelper.StartTypewriterPlaybackIfWaiting(durationSecs);
				}
			});
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] queue map conversation tableau playback failed: " + ex.Message);
			return false;
		}
	}

	private static void ScheduleNativeMapConversationTtsFileCleanup(string wavPath, string xmlPath, float durationSecs)
	{
		int delayMs = 15000;
		if (durationSecs > 0f && !float.IsNaN(durationSecs) && !float.IsInfinity(durationSecs))
		{
			delayMs = Math.Max(15000, Math.Min(300000, (int)Math.Round((durationSecs + 10f) * 1000f)));
		}
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(delayMs).ConfigureAwait(false);
				if (!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath))
				{
					File.Delete(wavPath);
				}
				if (!string.IsNullOrWhiteSpace(xmlPath) && File.Exists(xmlPath))
				{
					File.Delete(xmlPath);
				}
			}
			catch
			{
			}
		});
	}

	public static async Task WaitForNativeConversationTtsPlaybackFinishedForExternalAsync()
	{
		Task waitTask = null;
		int timeoutMs = 0;
		long token = 0L;
		try
		{
			lock (_nativeConversationTtsPlaybackWaitLock)
			{
				waitTask = _nativeConversationTtsPlaybackWaitTcs?.Task;
				timeoutMs = _nativeConversationTtsPlaybackWaitTimeoutMs;
				token = _nativeConversationTtsPlaybackWaitToken;
			}
			if (waitTask == null || waitTask.IsCompleted)
			{
				return;
			}
			if (timeoutMs <= 0)
			{
				timeoutMs = NativeConversationTtsPlaybackWaitMinTimeoutMs;
			}
			Logger.Log("NativeConversation", "[TTS] waiting for native conversation playback before enabling reply. timeoutMs=" + timeoutMs);
			Task completed = await Task.WhenAny(waitTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
			if (completed != waitTask)
			{
				CompleteNativeConversationTtsPlaybackWaitByToken(token, "timeout");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] wait for native playback failed: " + ex.Message);
		}
	}

	// Builds a UI-only RichText copy from a completed main/final reply; callers must never pass incomplete stream fragments.
	public static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText)
	{
		try
		{
			TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _);
			return FormatNativeConversationDisplayTextForExternal(rawVisibleText, targetHero, targetCharacter);
		}
		catch (Exception ex)
		{
			// A display-only failure must fall back to safe plain text and never invalidate a completed dialogue turn.
			Logger.LogTrace("NativeConversation", "[WARN] Could not format encyclopedia links for a visible reply: " + ex.Message);
			return EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawVisibleText);
		}
	}

	// The completed-reply callback carries its original target so a later action cannot redirect NPC/玩家 links before the UI formats them on the main thread.
	public static string FormatNativeConversationDisplayTextForExternal(string rawVisibleText, Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			return EncyclopediaEntityLinkFormatter.FormatNativeConversationText(rawVisibleText, targetHero, targetCharacter);
		}
		catch (Exception ex)
		{
			// A display-only failure must fall back to safe plain text and never invalidate a completed dialogue turn.
			Logger.LogTrace("NativeConversation", "[WARN] Could not format encyclopedia links for a visible reply: " + ex.Message);
			return EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(rawVisibleText);
		}
	}

	public static Hero GetNativeConversationTargetHeroForExternal()
	{
		try
		{
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				return null;
			}
			return targetHero ?? targetCharacter?.HeroObject;
		}
		catch
		{
			return null;
		}
	}

	public static bool CanEditNativeConversationPersonaForExternal()
	{
		try
		{
			Hero hero = GetNativeConversationTargetHeroForExternal();
			return MyBehavior.IsDevDataManagementEnabledForExternal() && hero != null && hero != Hero.MainHero && hero.CharacterObject?.IsHero == true;
		}
		catch
		{
			return false;
		}
	}

	public static bool CanEditNativeConversationNpcForExternal()
	{
		return CanEditNativeConversationPersonaForExternal();
	}

	public static bool CanOpenNativeConversationTagTestForExternal()
	{
		try
		{
			return MyBehavior.IsDevDataManagementEnabledForExternal() && CanSubmitNativeConversationForExternal();
		}
		catch
		{
			return false;
		}
	}

	public static bool OpenNativeConversationTagTestForExternal(Action onFinished = null)
	{
		try
		{
			if (!CanOpenNativeConversationTagTestForExternal() || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return false;
			}
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			Action finish = delegate
			{
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
			};
			Action<string> submit = delegate(string input)
			{
				try
				{
					if (TrySubmitNativeConversationTagTestForExternal(input, out var statusText))
					{
						ShowTagTestStatus(statusText, success: true);
					}
					else
					{
						ShowTagTestStatus(string.IsNullOrWhiteSpace(statusText) ? "标签测试未执行。" : statusText, success: false);
					}
				}
				finally
				{
					finish();
				}
			};
			string title = "标签输入 - " + displayName;
			string subtitle = "当前目标：" + displayName + "\n输入 NPC 可见正文和后处理标签。不会调用 API，标签会按当前 NPC 立即执行。";
			if (ShoutTextInputPopup.Show(title, subtitle, "输入正文和标签：", "", submit, finish))
			{
				return true;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData(title, subtitle + "\n\n输入正文和标签：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "执行", "取消", submit, finish), pauseGameActiveState: true);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[WARN] open failed: " + ex.Message);
			return false;
		}
	}

	public static bool TrySubmitNativeConversationTagTestForExternal(string rawText, out string statusText)
	{
		statusText = "";
		try
		{
			if (!CanOpenNativeConversationTagTestForExternal())
			{
				statusText = "数据管理未开启，或当前没有可测试的对话目标。";
				return false;
			}
			ShoutBehavior instance = CurrentInstance;
			if (instance == null || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				statusText = "当前没有可接入的对话对象。";
				return false;
			}
			string content = (rawText ?? "").Replace("\r", "").Trim();
			if (string.IsNullOrWhiteSpace(content))
			{
				statusText = "输入为空。";
				return false;
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			int tagCount = CountDeveloperTagTestTags(content);
			Logger.Log("NativeConversationTagTest", "submit target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? displayName) + " agentIndex=" + targetAgentIndex + " tagCount=" + tagCount + " raw=" + content.Replace("\n", "\\n"));
			NpcDataPacket nativeTagTestNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			nativeTagTestNpc.AgentIndex = targetAgentIndex;
			List<NpcDataPacket> presentNpcs = new List<NpcDataPacket> { nativeTagTestNpc };
			Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
			if (targetAgentIndex >= 0 && targetHero != null)
			{
				resolvedHeroes[targetAgentIndex] = targetHero;
			}
			List<SceneSummonPromptTarget> sceneSummonTargets = (targetAgentIndex >= 0) ? instance._sceneMovement.BuildSceneSummonPromptTargets(presentNpcs, resolvedHeroes) : null;
			int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
			Agent targetAgent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			List<SceneGuidePromptTarget> sceneGuideTargets = (targetAgentIndex >= 0) ? instance._sceneMovement.BuildSceneGuidePromptTargets(targetAgent, sceneGuideFirstPromptId) : null;
			if (targetHero != null)
			{
				MyBehavior.ApplyPostprocessMoodFromSceneHeroResponseExternal(targetHero, ref content);
			}
			else
			{
				try
				{
					Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()) : null;
					NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
					if (npc != null)
					{
						MyBehavior.ApplyPostprocessMoodFromSceneUnnamedResponseExternal(npc.UnnamedKey, npc.Name, ref content);
					}
				}
				catch
				{
				}
			}
			bool sceneMechanismHandled = instance.TryQueueNativeSceneMechanismActionAfterConversationExit(nativeTagTestNpc, presentNpcs, sceneSummonTargets, sceneGuideTargets, ref content);
			instance._nativeGameEffects.ApplyNativeConversationActionTags(targetHero, targetCharacter, ref content, targetAgentIndex);
			string visible = SanitizeSceneSpeechText(content);
			if (!string.IsNullOrWhiteSpace(visible))
			{
				try
				{
					ConversationHelper.UpdateDialogText(visible);
				}
				catch
				{
				}
				instance.CommitNativeConversationTagTestVisibleLine(targetHero, targetCharacter, npcName, visible, targetAgentIndex);
			}
			statusText = "标签测试已执行。目标：" + displayName + "，标签数：" + tagCount + (sceneMechanismHandled ? "，SCENE_MOVE将在退出对话框后执行。" : "") + (string.IsNullOrWhiteSpace(visible) ? "，无可见正文。" : "，已显示正文。");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[ERROR] submit failed: " + ex);
			statusText = "标签测试失败：" + ex.Message;
			return false;
		}
	}

	private static void ShowTagTestStatus(string statusText, bool success)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(statusText ?? "", success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.45f, 0.25f)));
		}
		catch
		{
		}
	}

	private static int CountDeveloperTagTestTags(string text)
	{
		try
		{
			string text2 = text ?? "";
			return GiveAssetTagCodec.Extract(text2).Count + Regex.Matches(GiveAssetTagCodec.StripTags(text2), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END|RELAY:[^\\]]*|AFEF[^\\]]*|AF_SCENE_SESSION:[^\\]]*|CONTENT)\\]", RegexOptions.IgnoreCase).Count;
		}
		catch
		{
			return 0;
		}
	}

	private void CommitNativeConversationTagTestVisibleLine(Hero targetHero, CharacterObject targetCharacter, string npcName, string visible, int targetAgentIndex)
	{
		visible = (visible ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(visible))
		{
			return;
		}
		try
		{
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			npc.AgentIndex = targetAgentIndex;
			string displayName = (GetSceneNpcHistoryNameForPrompt(npc) ?? npcName ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			if (targetHero != null)
			{
				int sceneSessionId = TryGetCurrentSceneHistorySessionIdForHistoryPersistence();
				if (sceneSessionId >= 0)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(targetHero, null, visible, null, sceneSessionId);
				}
				else
				{
					MyBehavior.AppendExternalDialogueHistory(targetHero, null, visible, null);
				}
			}
			else
			{
				AppendWildernessNonHeroMemory(npc, targetHero, targetCharacter, targetAgentIndex, null, visible, null, TryGetCurrentSceneHistorySessionIdForHistoryPersistence());
			}
			RecordNativeConversationNpcLineForExternal(targetHero, targetCharacter, displayName, visible, targetAgentIndex, npc);
			MarkNativeConversationCurrentDialogRecorded(targetHero, targetCharacter, npcName, visible, targetAgentIndex, npc);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[WARN] commit visible line failed: " + ex.Message);
		}
	}

	public static bool OpenNativeConversationNpcEditorForExternal(Action onFinished = null)
	{
		try
		{
			if (!CanEditNativeConversationNpcForExternal())
			{
				return false;
			}
			Hero hero = GetNativeConversationTargetHeroForExternal();
			if (hero == null)
			{
				return false;
			}
			MyBehavior.OpenHeroNpcEditorForExternal(hero, onFinished);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] Failed to open native conversation NPC editor: " + ex.Message);
			return false;
		}
	}

	public static bool OpenNativeConversationPersonaEditorForExternal(Action onFinished = null)
	{
		try
		{
			if (!CanEditNativeConversationPersonaForExternal())
			{
				return false;
			}
			Hero hero = GetNativeConversationTargetHeroForExternal();
			if (hero == null)
			{
				return false;
			}
			MyBehavior.OpenHeroPersonaEditorForExternal(hero, onFinished);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] Failed to open native conversation persona editor: " + ex.Message);
			return false;
		}
	}

	public static void OpenNativeConversationInputForExternal()
	{
		OpenNativeConversationInput();
	}

	public static void OpenNativeConversationInputSilentlyForExternal()
	{
		OpenNativeConversationInput(showMessage: false);
	}

	public static void CloseNativeConversationInputForExternal()
	{
		CloseNativeConversationInput();
	}

	public static bool OpenNativeConversationGiveShowForExternal(Action onFinished = null)
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return false;
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			targetNpc.AgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			if (string.IsNullOrWhiteSpace(targetNpc.Name))
			{
				targetNpc.Name = npcName;
			}
			instance.OpenNativeConversationGiveShowMenu(targetNpc, targetHero, targetCharacter, onFinished);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTrade", "[WARN] Failed to open give/show menu: " + ex.Message);
			return false;
		}
	}

	internal static int TryResolveNativeConversationAgentIndex(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			Agent conversationAgent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (IsValidNativeConversationTargetAgent(conversationAgent, targetHero, targetCharacter))
			{
				return conversationAgent.Index;
			}
			if (IsUsableNativeConversationFallbackAgent(conversationAgent, targetHero, targetCharacter))
			{
				Logger.Log("NativeConversation", "[AgentResolve] using fallback OneToOneConversationAgent index=" + conversationAgent.Index + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "nonhero"));
				return conversationAgent.Index;
			}
			CharacterObject character = targetCharacter ?? targetHero?.CharacterObject;
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if ((character == null && targetHero == null) || agents == null)
			{
				return -1;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => IsValidNativeConversationTargetAgent(a, targetHero, targetCharacter));
			if (agent != null)
			{
				return agent.Index;
			}
			if (targetHero == null)
			{
				Agent fallbackAgent = agents.FirstOrDefault((Agent a) => IsUsableNativeConversationFallbackAgent(a, targetHero, targetCharacter));
				if (fallbackAgent != null)
				{
					Logger.Log("NativeConversation", "[AgentResolve] using fallback mission agent index=" + fallbackAgent.Index + " targetCharacter=" + (targetCharacter?.StringId ?? "nonhero"));
					return fallbackAgent.Index;
				}
			}
			return -1;
		}
		catch
		{
			return -1;
		}
	}

	private static bool IsUsableNativeConversationFallbackAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter)
	{
		if (agent == null || agent.IsMainAgent || agent.Character == null || targetHero != null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive())
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		CharacterObject agentCharacter = agent.Character as CharacterObject;
		if (agentCharacter == null || agentCharacter.HeroObject != null)
		{
			return false;
		}
		if (targetCharacter != null && ReferenceEquals(agentCharacter, targetCharacter))
		{
			return true;
		}
		try
		{
			Agent conversationAgent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (conversationAgent != null && conversationAgent.Index == agent.Index)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			PartyBase agentParty = agent.Origin?.BattleCombatant as PartyBase;
			if (encounteredParty != null && agentParty != null && ReferenceEquals(encounteredParty, agentParty))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsValidNativeConversationTargetAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter)
	{
		if (agent == null || agent.IsMainAgent || agent.Character == null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive())
			{
				return false;
			}
		}
		catch
		{
		}
		CharacterObject agentCharacter = agent.Character as CharacterObject;
		if (agentCharacter == null)
		{
			return false;
		}
		Hero agentHero = agentCharacter.HeroObject;
		if (targetHero != null)
		{
			if (ReferenceEquals(agentHero, targetHero))
			{
				return true;
			}
			string targetHeroId = (targetHero.StringId ?? "").Trim();
			string agentHeroId = (agentHero?.StringId ?? "").Trim();
			return !string.IsNullOrWhiteSpace(targetHeroId) && string.Equals(agentHeroId, targetHeroId, StringComparison.OrdinalIgnoreCase);
		}
		CharacterObject character = targetCharacter;
		if (character == null)
		{
			return false;
		}
		if (ReferenceEquals(agentCharacter, character))
		{
			return true;
		}
		string targetCharacterId = (character.StringId ?? "").Trim();
		string agentCharacterId = (agentCharacter.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(targetCharacterId) && string.Equals(agentCharacterId, targetCharacterId, StringComparison.OrdinalIgnoreCase);
	}

	internal static bool TryResolveWildernessNonHeroRewardParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
		party = null;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null || targetCharacter == null)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
			PartyBase partyBase = null;
			if (targetAgentIndex >= 0)
			{
				partyBase = MyBehavior.ResolvePartyTransferCounterpartyForExternal(null, targetCharacter, targetAgentIndex);
			}
			if (partyBase == null && targetAgentIndex < 0 && IsNativeConversationWorldMapContext())
			{
				MobileParty mobileParty = TryResolveWildernessNonHeroMobileParty(targetAgentIndex);
				partyBase = mobileParty?.Party;
			}
			if (partyBase == null || partyBase == PartyBase.MainParty || partyBase.MobileParty == null || partyBase.MobileParty == MobileParty.MainParty || partyBase.ItemRoster == null)
			{
				return false;
			}
			party = partyBase;
			return true;
		}
		catch
		{
			party = null;
			return false;
		}
	}

	private static bool TryResolveNativeConversationMeetingTauntParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
		party = null;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return false;
			}
			return TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out party);
		}
		catch
		{
			party = null;
			return false;
		}
	}

	private void OpenNativeConversationGiveShowMenu(NpcDataPacket targetNpc, Hero targetHero, CharacterObject targetCharacter, Action onFinished)
	{
		_shoutTradeNativeManager = Campaign.Current?.ConversationManager;
		_shoutTradeNativeMission = Mission.Current;
		_shoutTradeNativeAgent = _shoutTradeNativeManager?.OneToOneConversationAgent as Agent;
		_shoutTradeActionOnly = true;
		_shoutTradeTargetHeroOverride = targetHero;
		_shoutTradeTargetCharacterOverride = targetCharacter ?? targetHero?.CharacterObject;
		_shoutTradeActionOnlyFinished = onFinished;
		_shoutTradeTargetNpc = targetNpc;
		PauseGame();
		string targetName = (targetNpc?.Name ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "对方").Trim();
		List<InquiryElement> inquiryElements = new List<InquiryElement>
		{
			new InquiryElement("give", "给予物品", null, isEnabled: true, ""),
			new InquiryElement("show", "展示物品", null, isEnabled: true, ""),
			new InquiryElement("give_troops", "给予部队", null, isEnabled: true, ""),
			new InquiryElement("give_prisoners", "给予俘虏", null, isEnabled: true, ""),
			new InquiryElement("give_settlements", "转移固定资产", null, isEnabled: true, "")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("给予/展示 - " + targetName, "当前目标：" + targetName + "\n只执行给予或展示动作，不触发 AI 回复。动作会写入 AnimusForge 对话历史，供后续 AI 交流读取。", inquiryElements, isExitShown: true, 1, 1, "确定", "取消", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				ResetShoutTradeState();
				ResumeGame();
				FinishShoutTradeActionOnlyIfNeeded();
				return;
			}
			string choice = (selected[0]?.Identifier ?? "").ToString();
			if (choice == "give")
			{
				BeginShoutTradeFlow(targetNpc, ShoutChatMode.Give);
			}
			else if (choice == "show")
			{
				BeginShoutTradeFlow(targetNpc, ShoutChatMode.Show);
			}
			else if (choice == "give_troops")
			{
				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GiveTroops);
			}
			else if (choice == "give_prisoners")
			{
				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GivePrisoners);
			}
			else if (choice == "give_settlements")
			{
				BeginShoutTradeFlow(targetNpc, ShoutChatMode.GiveSettlements);
			}
			else
			{
				ResetShoutTradeState();
				ResumeGame();
				FinishShoutTradeActionOnlyIfNeeded();
			}
		}, delegate
		{
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	public static bool TryGetNativeConversationHistoryTargetForExternal(out Hero targetHero, out string targetName)
	{
		targetHero = null;
		targetName = "";
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out var npcName))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetName = (targetHero?.Name?.ToString() ?? character?.Name?.ToString() ?? npcName ?? "").Trim();
			return targetHero != null || !string.IsNullOrWhiteSpace(targetName);
		}
		catch
		{
			targetHero = null;
			targetName = "";
			return false;
		}
	}

	// History retains the concrete non-hero target for identity, but the formatter links NPC only when that target resolves to a Hero.
	public static bool TryGetNativeConversationLinkTargetForExternal(out Hero targetHero, out CharacterObject targetCharacter)
	{
		targetHero = null;
		targetCharacter = null;
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out _))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetCharacter = character;
			return targetHero != null || targetCharacter != null;
		}
		catch
		{
			targetHero = null;
			targetCharacter = null;
			return false;
		}
	}

	public static bool TryGetNativeConversationPersistentHistoryTargetForExternal(out Hero targetHero, out string targetName, out string memoryId)
	{
		targetHero = null;
		targetName = "";
		memoryId = "";
		try
		{
			if (!TryResolveNativeConversationTarget(out var hero, out var character, out var npcName))
			{
				return false;
			}
			targetHero = hero ?? character?.HeroObject;
			targetName = (targetHero?.Name?.ToString() ?? character?.Name?.ToString() ?? npcName ?? "").Trim();
			if (targetHero != null)
			{
				memoryId = (targetHero.StringId ?? "").Trim();
				return !string.IsNullOrWhiteSpace(memoryId) || !string.IsNullOrWhiteSpace(targetName);
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, character);
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, character);
			if (npc != null && targetAgentIndex >= 0)
			{
				npc.AgentIndex = targetAgentIndex;
			}
			// 读档后原生自由对话的短期 session 表会清空；右上角历史必须回到同一个 af_nonhero 持久记忆 ID 读取。
			if (TryResolveWildernessNonHeroMemory(npc, targetHero, character, targetAgentIndex, out var nonHeroMemoryId, out var nonHeroMemoryName))
			{
				memoryId = nonHeroMemoryId;
				if (!string.IsNullOrWhiteSpace(nonHeroMemoryName))
				{
					targetName = nonHeroMemoryName.Trim();
				}
				Logger.Log("NativeConversationHistory", "persistent_target kind=nonhero memoryId=" + memoryId + " targetName=" + targetName + " agent=" + targetAgentIndex);
				return !string.IsNullOrWhiteSpace(memoryId);
			}
			return !string.IsNullOrWhiteSpace(targetName);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationHistory", "[WARN] persistent target resolve failed: " + ex.Message);
			targetHero = null;
			targetName = "";
			memoryId = "";
			return false;
		}
	}

	// Illustrator reads only the current encounter; the existing history-log API retains its history.
	public static List<AnimusForgeDialogueHistoryEntry> GetCurrentNativeConversationHistoryEntriesForExternal(int maxLines = 260)
	{
		long boundary = Interlocked.Read(ref _nativeIllustrationHistoryBoundary);
		return GetNativeConversationSessionHistoryEntriesForExternal(maxLines)
			.Where(entry => entry != null && entry.EventSequence > boundary).ToList();
	}

	public static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistoryEntriesForExternal(int maxLines = 260)
	{
		try
		{
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			int limit = Math.Max(1, Math.Min(260, maxLines <= 0 ? 260 : maxLines));
			return _nativeSessionOwner.GetTail(key, limit);
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}

	public static void ClearNativeConversationSessionHistoryForExternal(Hero targetHero, CharacterObject targetCharacter = null, string npcName = null, int dayIndex = -1)
	{
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			_nativeSessionOwner.Clear(key, dayIndex);
		}
		catch
		{
		}
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
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key) || dayIndex < 0)
			{
				return;
			}

			string result = _nativeSessionOwner.SyncDay(key, dayIndex, previousEntries, currentEntries, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
			if (!string.IsNullOrEmpty(result)) Logger.Log("NativeConversationHistory", "manual_daily_memory_sync key=" + key + " day=" + dayIndex + " " + result + " reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationHistory", "[WARN] manual daily memory sync failed: " + ex.Message);
		}
	}















	public static void RecordNativeConversationNpcLineForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string text, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			if (targetHero == null)
			{
				targetHero = targetCharacter?.HeroObject;
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
			string line = NormalizeNativeConversationHistoryTextForPostprocess(text);
			if (string.IsNullOrWhiteSpace(line))
			{
				return;
			}
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, displayName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			if (_nativeSessionOwner.IsLastNpcLine(key, line)) return;
			AppendNativeConversationSessionHistory(targetHero, targetCharacter, displayName, displayName, line, "npc", targetAgentIndex: targetAgentIndex, npc: npc);
			Logger.Log("NativeConversation", "[ExternalNpcLine] recorded target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? displayName));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] external npc line record failed: " + ex.Message);
		}
	}



	private static string BuildNativeConversationHistoryKey(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null) => CaptureNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);

	private static void AppendNativeConversationSessionHistory(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, bool bridgeToSceneHistory = true, int targetAgentIndex = -1, NpcDataPacket npc = null, int playerTargetAgentIndex = -1, string playerTargetName = null, string capturedHistoryKey = null) => AppendNativeConversationSessionHistoryCaptured(targetHero, targetCharacter, npcName, speaker, text, kind, eventSequence, bridgeToSceneHistory, targetAgentIndex, npc, playerTargetAgentIndex, playerTargetName, capturedHistoryKey);

	// A native request records the player line before its LLM calls so the current
	// turn is available to the main and postprocess prompts. If its live scene
	// target disappears, remove only that request's globally unique event rather
	// than leaving an orphaned player line in either shared history store.
	private static void RollbackNativeConversationPendingPlayerHistory(ShoutBehavior owner,
        NativeConversationAdmission admission, string historyKey, long eventSequence, string reason)
    {
        // Event sequences reset on load. Never recompute the key or use a new CurrentInstance.
        if (owner == null || eventSequence <= 0 || string.IsNullOrWhiteSpace(historyKey)
            || !owner.IsNativeConversationContextStampCurrent(admission)
            || !owner._nativeAdmissionOwner.IsPresentationCurrent(admission.PresentationRevision))
            return;
        _nativeSessionOwner.RollbackPlayerEvent(historyKey, eventSequence);
        owner.RemoveNativeConversationSessionHistoryEventFromSceneHistory(eventSequence);
        ObserveNativePendingHistory("rollback_applied_" + (reason ?? "unknown"));
    }



	internal static void MarkNativeConversationCurrentDialogRecorded(Hero targetHero, CharacterObject targetCharacter, string npcName, string currentDialogText, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			string normalizedLine = NormalizeNativeConversationHistoryTextForPostprocess(currentDialogText);
			if (string.IsNullOrWhiteSpace(normalizedLine))
			{
				return;
			}
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			_nativeSessionOwner.MarkDialog(key, normalizedLine);
		}
		catch
		{
		}
	}

	private static string NormalizeNativeConversationFactLineForPrompt(string text, string speaker)
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
		try
		{
			string normalizedLine = NormalizeNativeConversationVisibleTextKey(currentNativeDialogText);
			if (string.IsNullOrWhiteSpace(normalizedLine))
			{
				return;
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			bool shouldAppend = false;
			shouldAppend = _nativeSessionOwner.TryMarkDialog(key, normalizedLine);
			if (shouldAppend)
			{
				AppendNativeConversationSessionHistory(targetHero, targetCharacter, npcName, string.IsNullOrWhiteSpace(npcDisplayName) ? npcName : npcDisplayName, normalizedLine, "npc", targetAgentIndex: targetAgentIndex);
			}
		}
		catch
		{
		}
	}

	public static string GetLatestNativeConversationNpcUtteranceForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			string npcName = targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "";
			if (targetAgentIndex < 0)
			{
				targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			}
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(key))
			{
				return "";
			}
			return _nativeSessionOwner.LatestNpcUtterance(key, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
		}
		catch
		{
		}
		return "";
	}

	private static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
		try
		{
			string key = capturedHistoryKey ?? BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			return _nativeSessionOwner.Snapshot(key, ResolveDailyConversationHistoryLineLimit(maxLines));
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}

	private static bool HasNativeConversationSessionHistory(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex = -1, NpcDataPacket npc = null)
	{
		try
		{
			string key = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, npc);
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			return _nativeSessionOwner.HasHistory(key);
		}
		catch
		{
			return false;
		}
	}

	private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0, NpcDataPacket npc = null, string capturedHistoryKey = null)
	{
		try
		{
			var entries = GetNativeConversationSessionHistorySnapshot(targetHero, targetCharacter, npcName, targetAgentIndex, maxLines, npc, capturedHistoryKey);
			var distances = new Dictionary<int, float>();
			foreach (var entry in entries)
			{
				if (entry == null || string.IsNullOrWhiteSpace(entry.Text) || string.Equals((entry.Kind ?? "").Trim(), "fact", StringComparison.OrdinalIgnoreCase) || string.Equals((entry.Kind ?? "").Trim(), "npc", StringComparison.OrdinalIgnoreCase)) continue;
				int index = entry.TargetAgentIndex >= 0 ? entry.TargetAgentIndex : targetAgentIndex;
				if (!distances.ContainsKey(index)) distances[index] = GetPlayerDistanceToAgentForScenePrompt(index);
			}
			return NativeConversationSessionOwner.ProjectHistoryMessages(entries, npcName, targetAgentIndex, distances);
		}
		catch { return new List<ConversationMessage>(); }
	}

	private static List<ConversationMessage> BuildNativeConversationSessionHistoryMessagesForAgent(int targetAgentIndex, int maxLines = 0)
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (targetAgentIndex < 0 || agents == null)
			{
				return new List<ConversationMessage>();
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (agent == null)
			{
				return new List<ConversationMessage>();
			}
			CharacterObject character = agent.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			string npcName = (npc != null) ? GetSceneNpcHistoryNameForPrompt(npc) : (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? "");
			return BuildNativeConversationSessionHistoryMessages(hero, character, npcName, targetAgentIndex, maxLines, npc);
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}

	internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, int targetAgentIndex)
	{
		return BuildUncompressedMemoryRoleMessagesForPrompt(hero, null, null, targetAgentIndex);
	}

	internal static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForPrompt(Hero hero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex)
	{
		try
		{
			if (!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero, targetCharacter, targetAgentIndex))
			{
				return new List<ConversationMessage>();
			}
			if (hero != null)
			{
				return MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(hero, targetAgentIndex, includeCurrentActiveSceneSession: false) ?? new List<ConversationMessage>();
			}
			NpcDataPacket resolvedNpc = npc;
			CharacterObject resolvedCharacter = targetCharacter;
			var agents = Mission.Current?.Agents;
			if ((resolvedNpc == null || resolvedCharacter == null) && targetAgentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
				resolvedCharacter ??= agent?.Character as CharacterObject;
				resolvedNpc ??= ShoutUtils.ExtractNpcData(agent);
			}
			if (resolvedNpc != null && resolvedNpc.AgentIndex < 0 && targetAgentIndex >= 0)
			{
				resolvedNpc.AgentIndex = targetAgentIndex;
			}
			if (TryResolveWildernessNonHeroMemory(resolvedNpc, null, resolvedCharacter, targetAgentIndex, out var memoryId, out var memoryName))
			{
				List<ConversationMessage> messages = MyBehavior.BuildNonHeroUncompressedMemoryRoleMessagesForExternal(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession: false) ?? new List<ConversationMessage>();
				// 读档后原生短期会话历史会清空，非 hero 必须从同一个 af_nonhero 记忆 ID 注入未压缩长期记忆。
				LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " messages=" + messages.Count);
				return messages;
			}
			LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId= reason=resolve_failed messages=0 character=" + (resolvedCharacter?.StringId ?? "") + " npc=" + (resolvedNpc?.TroopId ?? resolvedNpc?.UnnamedKey ?? ""));
			return new List<ConversationMessage>();
		}
		catch (Exception ex)
		{
			LogNonHeroMemoryTrace("stage=uncompressed_inject agent=" + targetAgentIndex + " memoryId= reason=exception messages=0 error=" + ex.Message);
			return new List<ConversationMessage>();
		}
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
		try
		{
			Hero hero = null;
			if (resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(targetAgentIndex, out hero);
			}
			var agents = Mission.Current?.Agents;
			if (hero == null && targetAgentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
				hero = (agent?.Character as CharacterObject)?.HeroObject;
				if (hero == null)
				{
					CharacterObject character = agent?.Character as CharacterObject;
					NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
					return BuildUncompressedMemoryRoleMessagesForPrompt(null, character, npc, targetAgentIndex);
				}
			}
			return BuildUncompressedMemoryRoleMessagesForPrompt(hero, targetAgentIndex);
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}

	private static List<string> BuildNativeConversationSceneHistoryLinesForPrompt(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
		List<string> lines = new List<string>();
		try
		{
			string targetName = (npcName ?? "").Trim();
			int historyLineLimit = ResolveDailyConversationHistoryLineLimit(maxLines);
			List<ConversationMessage> messages = BuildNativeConversationSessionHistoryMessages(targetHero, targetCharacter, targetName, targetAgentIndex, historyLineLimit);
			for (int i = 0; i < messages.Count; i++)
			{
				if (TryRenderSceneHistoryLine(messages[i], null, out var rendered, targetAgentIndex, targetName, useNpcNameAddress: false, useSceneDistanceSpeechLabels: false))
				{
					string text = (rendered ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						lines.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return lines;
	}

	internal static List<string> BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(Hero targetHero, CharacterObject targetCharacter, string npcName, int targetAgentIndex, int maxLines = 0)
	{
		List<string> lines = new List<string>();
		try
		{
			int historyLineLimit = ResolveDailyConversationHistoryLineLimit(maxLines);
			List<ConversationMessage> messages = new List<ConversationMessage>();
			if (targetAgentIndex >= 0 && CurrentInstance != null)
			{
				AppendConversationMessages(messages, CurrentInstance.GetNpcConversationHistorySnapshot(targetAgentIndex));
			}
			AppendConversationMessages(messages, BuildNativeConversationSessionHistoryMessages(targetHero, targetCharacter, npcName, targetAgentIndex, historyLineLimit));
			messages = SortConversationMessagesByEventSequence(messages);
			string targetName = (npcName ?? "").Trim();
			HashSet<long> renderedEventSequences = new HashSet<long>();
			for (int i = 0; i < messages.Count; i++)
			{
				ConversationMessage message = messages[i];
				if (message.EventSequence > 0L && !renderedEventSequences.Add(message.EventSequence))
				{
					continue;
				}
				if (TryRenderSceneHistoryLine(message, null, out var rendered, targetAgentIndex, targetName, useNpcNameAddress: false, useSceneDistanceSpeechLabels: false))
				{
					string text = (rendered ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						lines.Add(text);
					}
				}
			}
			lines = KeepAfefFactsAndRecentHistoryLines(lines, historyLineLimit);
		}
		catch
		{
		}
		return lines;
	}

	private static bool HasNativeConversationSceneDialogueHistoryForPrompt(int targetAgentIndex)
	{
		try
		{
			if (CurrentInstance == null)
			{
				return false;
			}
			List<ConversationMessage> messages = CurrentInstance.GetNpcConversationHistorySnapshot(targetAgentIndex);
			return messages != null && messages.Any(IsSceneConversationTurn);
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldIncludeCurrentSceneSessionInNativePersistedHistory(int targetAgentIndex)
	{
		try
		{
			if (Mission.Current?.Scene == null || !ShoutUtils.IsInValidScene())
			{
				return false;
			}
			return !HasNativeConversationSceneDialogueHistoryForPrompt(targetAgentIndex);
		}
		catch
		{
			return false;
		}
	}

	public static List<string> GetNativeConversationAuxiliaryHistoryLinesForExternal(int maxLines = 6)
	{
		try
		{
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return new List<string>();
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			List<string> lines = BuildNativeConversationUnifiedSceneHistoryLinesForPrompt(targetHero, targetCharacter, npcName, targetAgentIndex, Math.Max(1, maxLines));
			if (lines.Count <= maxLines)
			{
				return lines;
			}
			return lines.Skip(Math.Max(0, lines.Count - maxLines)).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	internal static Func<string> CaptureNativeConversationPersistedHistoryWork(Hero targetHero, CharacterObject targetCharacter, string playerText, string currentNativeDialogText, bool includeCurrentActiveSceneSession, long generation)
    {
        if (!IsBannerlordMainThreadForNativeActions()) return null;
        try
        {
            Hero hero = targetHero ?? targetCharacter?.HeroObject;
            Func<string> work;
            if (hero == null)
            {
                int agentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
                NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
                if (npc != null) npc.AgentIndex = agentIndex;
                string secondaryInput = NormalizeNativeConversationVisibleTextKey(currentNativeDialogText);
                if (string.IsNullOrWhiteSpace(secondaryInput))
                    secondaryInput = GetLatestNativeConversationNpcUtteranceForExternal(targetHero, targetCharacter, agentIndex);
                if (!TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
                {
                    LogNonHeroMemoryTrace("stage=history_context_request ok=0 reason=resolve_failed agent=" + agentIndex);
                    return () => "";
                }
                Func<string> captured = MyBehavior.CaptureHistoryContextWorkById(memoryId, memoryName, playerText, secondaryInput, includeCurrentActiveSceneSession, generation);
                if (captured == null) return null;
                work = () =>
                {
                    string context = (captured() ?? "").Trim();
                    LogNonHeroMemoryTrace("stage=history_context_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " chars=" + context.Length + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentInputLen=" + ((playerText ?? "").Length) + " secondaryInputLen=" + ((secondaryInput ?? "").Length));
                    return context;
                };
            }
            else
            {
                string secondaryInput = NormalizeNativeConversationVisibleTextKey(currentNativeDialogText);
                if (string.IsNullOrWhiteSpace(secondaryInput))
                    secondaryInput = GetLatestNativeConversationNpcUtteranceForExternal(targetHero, targetCharacter, TryResolveNativeConversationAgentIndex(targetHero, targetCharacter));
                work = MyBehavior.CaptureHistoryContextWorkForHero(hero, playerText, secondaryInput, includeCurrentActiveSceneSession, generation);
            }
            if (work == null) return null;
            // Preserve the old history-only failure fallback; the work itself contains no live identity lookup.
            return () => { try { return (work() ?? "").Trim(); } catch { return ""; } };
        }
        catch { return () => ""; }
    }

	private static bool IsWildernessNonHeroMemoryScope(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return TryResolveWildernessNonHeroMobileParty(agentIndex) != null;
	}

	private static string BuildWildernessNonHeroPartyRepresentativePrompt(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return "";
			}
		}
		catch
		{
		}
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			if (party == null || party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
			{
				return "";
			}
			string partyName = (party.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(partyName))
			{
				partyName = (party.MapFaction?.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			if (string.IsNullOrWhiteSpace(partyName))
			{
				return "";
			}
			return "你正作为" + partyName + "的代表进行交涉。";
		}
		catch
		{
			return "";
		}
	}

	private static PartyBase ResolveWildernessNonHeroPartyBaseForPrompt(int agentIndex)
	{
		try
		{
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return null;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			if (party == null || party == MobileParty.MainParty || party.Party == PartyBase.MainParty)
			{
				return null;
			}
			return party.Party;
		}
		catch
		{
			return null;
		}
	}

	private static MobileParty TryResolveWildernessNonHeroMobileParty(int agentIndex)
	{
		try
		{
			Agent agent = (agentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive()) : null;
			PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
			if (partyBase != null && partyBase.IsMobile && partyBase.MobileParty != null)
			{
				return partyBase.MobileParty;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (encounteredParty != null && encounteredParty.IsMobile && encounteredParty.MobileParty != null)
			{
				return encounteredParty.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string NormalizeWildernessNonHeroMemoryKeyPart(string value) => NativeHistoryIdentityProjectionOwner.NormalizeWildernessNonHeroMemoryKeyPart(value);

	private static void LogNonHeroMemoryTrace(string message)
	{
		try
		{
			if (Logger.IsVerboseModLogicEnabled)
			{
				Logger.Log("Logic", "[NonHeroMemoryTrace] " + (message ?? ""));
			}
		}
		catch
		{
		}
	}

	private static string BuildWildernessNonHeroPartyTrace(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return "party=null";
			}
			PartyBase partyBase = party.Party;
			return "partyStringId=" + (party.StringId ?? "")
				+ " partyName=" + ((party.Name?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim())
				+ " partyIndex=" + (partyBase?.Index ?? -1)
				+ " mapFaction=" + (party.MapFaction?.StringId ?? "")
				+ " leader=" + (party.LeaderHero?.StringId ?? "");
		}
		catch
		{
			return "party=trace_failed";
		}
	}

	private static string BuildWildernessNonHeroPartyMemoryKey(int agentIndex)
	{
		try
		{
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			string partyStringId = NormalizeWildernessNonHeroMemoryKeyPart(party?.StringId);
			if (!string.IsNullOrWhiteSpace(partyStringId))
			{
				// MobileParty.StringId 是 Bannerlord 为每支 MobileParty 分配并随存档保存的唯一对象 ID。
				// 只用 StringId，不用显示名称；同名劫匪/商队可以同名，但 StringId 不同。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_string_id agent=" + agentIndex + " key=party_string_id:" + partyStringId + " " + BuildWildernessNonHeroPartyTrace(party));
				return "party_string_id:" + partyStringId;
			}
			string savedPartyKey = MyBehavior.GetOrCreateWildernessNonHeroPartyMemoryKeyForExternal(party);
			if (!string.IsNullOrWhiteSpace(savedPartyKey))
			{
				// 备用路径：如果某些非原版部队没有 StringId，再使用本模组存档内 GUID。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_guid agent=" + agentIndex + " key=" + savedPartyKey + " " + BuildWildernessNonHeroPartyTrace(party));
				return savedPartyKey;
			}
			PartyBase partyBase = party?.Party;
			if (partyBase != null && partyBase.Index >= 0)
			{
				// 仅作为极端 fallback。party_index 不能作为持久记忆主键，读档后可能查不到旧记忆。
				LogNonHeroMemoryTrace("stage=party_key ok=1 mode=party_index agent=" + agentIndex + " key=party_index:" + partyBase.Index + " " + BuildWildernessNonHeroPartyTrace(party));
				return "party_index:" + partyBase.Index;
			}
			// 不要退回显示名称：刷新的劫匪、商队经常同名，会再次共享记忆。
			LogNonHeroMemoryTrace("stage=party_key ok=0 reason=no_stable_party_key agent=" + agentIndex + " " + BuildWildernessNonHeroPartyTrace(party));
			return "";
		}
		catch (Exception ex)
		{
			LogNonHeroMemoryTrace("stage=party_key ok=0 reason=exception agent=" + agentIndex + " error=" + ex.Message);
			return "";
		}
	}

	private static string BuildWildernessNonHeroCharacterMemoryKey(CharacterObject character, MobileParty party)
	{
		string troopKey = NormalizeWildernessNonHeroMemoryKeyPart(character?.StringId);
		if (string.IsNullOrWhiteSpace(troopKey))
		{
			return "";
		}
		string key = "troop:" + troopKey;
		string factionKey = NormalizeWildernessNonHeroMemoryKeyPart(party?.MapFaction?.StringId);
		if (string.IsNullOrWhiteSpace(factionKey))
		{
			factionKey = NormalizeWildernessNonHeroMemoryKeyPart(character?.Culture?.StringId);
		}
		if (!string.IsNullOrWhiteSpace(factionKey))
		{
			key += ":kingdom:" + factionKey;
		}
		string leaderKey = NormalizeWildernessNonHeroMemoryKeyPart(party?.LeaderHero?.StringId);
		if (!string.IsNullOrWhiteSpace(leaderKey))
		{
			key += ":lord:" + leaderKey;
		}
		return key;
	}

	internal static bool TryResolveWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, out string memoryId, out string memoryName)
	{
		memoryId = "";
		memoryName = "";
		try
		{
			if (targetHero != null || targetCharacter?.HeroObject != null || npc?.IsHero == true)
			{
				return false;
			}
			if (!IsWildernessNonHeroMemoryScope(agentIndex))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=not_wilderness_scope agent=" + agentIndex);
				return false;
			}
			MobileParty party = TryResolveWildernessNonHeroMobileParty(agentIndex);
			NpcDataPacket data = npc;
			CharacterObject character = targetCharacter;
			if ((data == null || string.IsNullOrWhiteSpace(data.UnnamedKey)) && agentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
				data = ShoutUtils.ExtractNpcData(agent) ?? data;
				character ??= agent?.Character as CharacterObject;
			}
			string key = (data?.UnnamedKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key))
			{
				key = BuildWildernessNonHeroCharacterMemoryKey(character, party);
			}
			if (string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(data?.TroopId))
			{
				key = "troop:" + NormalizeWildernessNonHeroMemoryKeyPart(data.TroopId);
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_base_key agent=" + agentIndex + " troop=" + (character?.StringId ?? data?.TroopId ?? "") + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			string partyKey = BuildWildernessNonHeroPartyMemoryKey(agentIndex);
			if (string.IsNullOrWhiteSpace(partyKey))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_party_key agent=" + agentIndex + " baseKey=" + NormalizeWildernessNonHeroMemoryKeyPart(key) + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			key = key + "|party:" + partyKey;
			memoryId = MyBehavior.BuildNonHeroMemoryIdForExternal(key);
			if (string.IsNullOrWhiteSpace(memoryId))
			{
				LogNonHeroMemoryTrace("stage=resolve ok=0 reason=no_memory_id agent=" + agentIndex + " baseKey=" + NormalizeWildernessNonHeroMemoryKeyPart(key) + " partyKey=" + partyKey + " " + BuildWildernessNonHeroPartyTrace(party));
				return false;
			}
			// 读档稳定性以 party_string_id 为准；这里仅把同一支部队的旧 GUID/未标注 StringId 记录迁到新 key。
			// 不迁移显示名称或兵种共享 key，避免同名劫匪、商队再次合并记忆。
			MyBehavior.MigrateNonHeroPartyScopedMemoryForExternal(memoryId, partyKey);
			string oldGuidPartyKey = MyBehavior.GetExistingWildernessNonHeroPartyMemoryKeyForExternal(party);
			if (!string.IsNullOrWhiteSpace(oldGuidPartyKey) && !string.Equals(oldGuidPartyKey, partyKey, StringComparison.OrdinalIgnoreCase))
			{
				MyBehavior.MigrateNonHeroPartyScopedMemoryForExternal(memoryId, oldGuidPartyKey);
			}
			memoryName = (data != null ? GetSceneNpcHistoryNameForPrompt(data) : "").Trim();
			if (string.IsNullOrWhiteSpace(memoryName))
			{
				memoryName = (targetCharacter?.Name?.ToString() ?? data?.Name ?? "NPC").Trim();
			}
			if (string.IsNullOrWhiteSpace(memoryName))
			{
				memoryName = "NPC";
			}
			LogNonHeroMemoryTrace("stage=resolve ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " baseKey=" + NormalizeWildernessNonHeroMemoryKeyPart(key) + " partyKey=" + partyKey + " troop=" + (character?.StringId ?? data?.TroopId ?? "") + " oldGuidKey=" + (oldGuidPartyKey ?? "") + " " + BuildWildernessNonHeroPartyTrace(party));
			return true;
		}
		catch (Exception ex)
		{
			LogNonHeroMemoryTrace("stage=resolve ok=0 reason=exception agent=" + agentIndex + " error=" + ex.Message);
			memoryId = "";
			memoryName = "";
			return false;
		}
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
		try
		{
			string nonHeroMemoryId = "";
			string resolvedGiverName = (giverName ?? "").Trim();
			if (giverHero == null
				&& TryResolveWildernessNonHeroMemoryForExternal(
					giverNpc,
					null,
					giverCharacter,
					giverAgentIndex,
					out string memoryId,
					out string memoryName))
			{
				nonHeroMemoryId = memoryId;
				if (string.IsNullOrWhiteSpace(resolvedGiverName))
				{
					resolvedGiverName = memoryName;
				}
			}
			if (string.IsNullOrWhiteSpace(resolvedGiverName))
			{
				resolvedGiverName = giverCharacter?.Name?.ToString() ?? "";
			}
			return RewardSystemBehavior.CreateRpItemIntroductionContextForExternal(
				giverHero,
				nonHeroMemoryId,
				resolvedGiverName,
				currentPlayerText,
				StripActionTagsForSceneSpeech(currentNpcText),
				includeNativeConversationSessionHistory);
		}
		catch
		{
			// Context enrichment is optional; preserve existing reward processing on failure.
			return null;
		}
	}

	internal static bool MayContainGeneratedRpItemReward(string responseText)
	{
		return !string.IsNullOrEmpty(responseText)
			&& responseText.IndexOf(GiveAssetTagCodec.Prefix, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string BuildNativeConversationNonHeroVoiceKey(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex)
	{
		try
		{
			if (targetHero != null || targetCharacter?.HeroObject != null || npc?.IsHero == true)
			{
				return "";
			}
			if (TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var _))
			{
				return memoryId;
			}
			string key = (npc?.UnnamedKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key))
			{
				key = BuildNativeConversationNonHeroUnnamedKey(targetCharacter, npc?.Name, agentIndex);
			}
			if (string.IsNullOrWhiteSpace(key))
			{
				key = BuildNativeConversationHistoryKey(null, targetCharacter, npc?.Name, agentIndex, npc);
			}
			string partyKey = BuildWildernessNonHeroPartyMemoryKey(agentIndex);
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(partyKey) && key.IndexOf("|party:", StringComparison.OrdinalIgnoreCase) < 0)
			{
				key = key + "|party:" + partyKey;
			}
			return (key ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildNativeConversationNonHeroUnnamedKey(CharacterObject targetCharacter, string npcName, int agentIndex) => CaptureNativeConversationNonHeroUnnamedKey(targetCharacter, npcName, agentIndex);

	private static float ResolveNativeConversationNonHeroAge(CharacterObject targetCharacter)
	{
		float age = 0f;
		try
		{
			age = targetCharacter?.Age ?? 0f;
		}
		catch
		{
			age = 0f;
		}
		if (age >= 18f && age <= 55f)
		{
			return age;
		}
		float fallbackAge = 30f;
		try
		{
			if (targetCharacter != null && !targetCharacter.IsSoldier)
			{
				switch (targetCharacter.Occupation)
				{
				case Occupation.Weaponsmith:
				case Occupation.Blacksmith:
				case Occupation.Armorer:
				case Occupation.GoodsTrader:
				case Occupation.HorseTrader:
				case Occupation.Artisan:
				case Occupation.Merchant:
					fallbackAge = 38f;
					break;
				case Occupation.Headman:
				case Occupation.Preacher:
				case Occupation.GangLeader:
				case Occupation.RuralNotable:
					fallbackAge = 46f;
					break;
				default:
					fallbackAge = 30f;
					break;
				}
			}
		}
		catch
		{
			fallbackAge = 30f;
		}
		return Math.Max(18f, Math.Min(55f, fallbackAge));
	}

	internal static string BuildWildernessNonHeroHistoryContextForPrompt(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string currentInput, string secondaryInput, bool includeCurrentActiveSceneSession = false)
	{
		if (!TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
		{
			LogNonHeroMemoryTrace("stage=history_context_request ok=0 reason=resolve_failed agent=" + agentIndex);
			return "";
		}
		string context = (MyBehavior.BuildNonHeroHistoryContextForExternal(memoryId, memoryName, 0, currentInput, secondaryInput, includeCurrentActiveSceneSession) ?? "").Trim();
		LogNonHeroMemoryTrace("stage=history_context_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " chars=" + context.Length + " includeCurrentSession=" + includeCurrentActiveSceneSession + " currentInputLen=" + ((currentInput ?? "").Length) + " secondaryInputLen=" + ((secondaryInput ?? "").Length));
		return context;
	}

	internal static void AppendWildernessNonHeroMemory(NpcDataPacket npc, Hero targetHero, CharacterObject targetCharacter, int agentIndex, string playerText, string aiText, string extraFact, int sceneSessionId = -1)
	{
		if (!TryResolveWildernessNonHeroMemory(npc, targetHero, targetCharacter, agentIndex, out var memoryId, out var memoryName))
		{
			LogNonHeroMemoryTrace("stage=append_request ok=0 reason=resolve_failed agent=" + agentIndex + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length) + " sceneSession=" + sceneSessionId);
			return;
		}
		LogNonHeroMemoryTrace("stage=append_request ok=1 agent=" + agentIndex + " memoryId=" + memoryId + " memoryName=" + memoryName + " playerLen=" + ((playerText ?? "").Length) + " aiLen=" + ((aiText ?? "").Length) + " factLen=" + ((extraFact ?? "").Length) + " sceneSession=" + sceneSessionId);
		if (sceneSessionId >= 0)
		{
			MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, playerText, aiText, extraFact, sceneSessionId);
		}
		else
		{
			MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, memoryName, playerText, aiText, extraFact);
		}
	}

	internal static string BuildNativeConversationNpcListBlockForPrompt(IEnumerable<NpcDataPacket> presentNpcs, NpcDataPacket selfNpc = null)
	{
		try
		{
			List<NpcDataPacket> promptNpcs = FilterScenePresentNpcsForPrompt(presentNpcs, selfNpc);
			if (promptNpcs.Count == 0)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【站在你旁边的人】：");
			foreach (NpcDataPacket npc in promptNpcs)
			{
				if (npc != null)
				{
					sb.AppendLine(BuildSceneNpcListLineForPrompt(npc));
				}
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(promptNpcs);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				sb.AppendLine(sceneNamingNote);
			}
			return sb.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveCurrentNativeConversationTargetKey()
	{
		if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
		{
			return "";
		}
		return targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "";
	}

	internal static string ResolveNativeConversationPostprocessChainName()
	{
		try
		{
			return LordEncounterBehavior.IsEncounterMeetingMissionActive ? "meeting" : "native_conversation";
		}
		catch
		{
			return "native_conversation";
		}
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
		try
		{
			return LordEncounterBehavior.IsEncounterMeetingMissionActive ? "meeting" : "scene";
		}
		catch
		{
			return "scene";
		}
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
		Hero resolvedHero = targetHero ?? targetCharacter?.HeroObject;
		string expectedSubjectId = !string.IsNullOrWhiteSpace(expectedInteractionSubjectId)
			? expectedInteractionSubjectId.Trim()
			: resolvedHero?.StringId
			?? targetCharacter?.StringId
			?? (targetAgentIndex >= 0 ? "agent:" + targetAgentIndex : string.Empty);
		if (resolvedHero != null)
		{
			return RewardSystemBehavior.CreateEconomyRewardDebtMainThreadPortForExternal();
		}
		if (TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out PartyBase party))
		{
			return RewardSystemBehavior.CreatePartyEconomyRewardDebtMainThreadPortForExternal(
				party,
				targetCharacter,
				expectedSubjectId,
				displayName);
		}
		Settlement settlement = Settlement.CurrentSettlement;
		if (targetCharacter != null && settlement != null)
		{
			return RewardSystemBehavior.CreateMerchantEconomyRewardDebtMainThreadPortForExternal(
				targetCharacter,
				settlement,
				expectedSubjectId,
				displayName);
		}
		return null;
	}

	internal static string ResolveDetachedInteractionSubjectId(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		NpcDataPacket npc)
	{
		if (!string.IsNullOrWhiteSpace(targetHero?.StringId))
		{
			return targetHero.StringId;
		}
		if (TryResolveWildernessNonHeroMemoryForExternal(
			npc,
			null,
			targetCharacter,
			targetAgentIndex,
			out string nonHeroMemoryId,
			out _)
			&& !string.IsNullOrWhiteSpace(nonHeroMemoryId))
		{
			return nonHeroMemoryId.Trim();
		}
		return targetCharacter?.StringId
			?? (targetAgentIndex >= 0 ? "agent:" + targetAgentIndex : string.Empty);
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
		ShoutBehavior instance = CurrentInstance;
		if (instance == null)
		{
			return null;
		}
		if (!TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string npcName))
		{
			return null;
		}

		int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		string targetUnavailableReason = "";
		if (!IsNativeConversationResponseTargetAvailableForActionDispatch(
			targetAgentIndex,
			targetHero,
			targetCharacter,
			out targetUnavailableReason))
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] detached action executor unavailable target="
				+ (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "unknown")
				+ " agentIndex=" + targetAgentIndex
				+ " reason=" + (targetUnavailableReason ?? "validation_failed"));
			return null;
		}

		NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
		targetNpc.AgentIndex = targetAgentIndex;
		List<NpcDataPacket> allNpcData = new List<NpcDataPacket> { targetNpc };
		Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
		if (targetAgentIndex >= 0 && targetHero != null)
		{
			resolvedHeroes[targetAgentIndex] = targetHero;
		}
		List<SceneSummonPromptTarget> sceneSummonTargets = targetAgentIndex >= 0
			? instance._sceneMovement.BuildSceneSummonPromptTargets(allNpcData, resolvedHeroes)
			: null;
		int firstGuidePromptId = (sceneSummonTargets != null && sceneSummonTargets.Count > 0
			? sceneSummonTargets.Max(item => item?.PromptId ?? 0)
			: 0) + 1;
		Agent targetAgent = targetAgentIndex >= 0
			? Mission.Current?.Agents?.FirstOrDefault(agent => agent != null && agent.Index == targetAgentIndex)
			: null;
		List<SceneGuidePromptTarget> sceneGuideTargets = targetAgentIndex >= 0
			? instance._sceneMovement.BuildSceneGuidePromptTargets(targetAgent, firstGuidePromptId)
			: null;
		ConversationManager expectedConversationManager = Campaign.Current?.ConversationManager;
		int expectedConversationToken = expectedConversationManager?.ActiveToken ?? int.MinValue;
		string expectedSubjectId = ResolveDetachedInteractionSubjectId(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			targetNpc);
		try
		{
			TryGetNativeConversationPersistentHistoryTargetForExternal(
				out Hero persistentHero,
				out _,
				out string persistentMemoryId);
			expectedSubjectId = !string.IsNullOrWhiteSpace(persistentMemoryId)
				? persistentMemoryId.Trim()
				: persistentHero?.StringId ?? expectedSubjectId;
		}
		catch
		{
		}
		Func<GameInteractionSnapshot, bool> isCurrentNativeContext = snapshot =>
		{
			try
			{
				if (!IsBannerlordMainThreadForNativeActions()
					|| snapshot?.Identity == null
					|| snapshot.Identity.Channel != InteractionChannel.NativeConversation
					|| !string.Equals(snapshot.Identity.SubjectId, expectedSubjectId, StringComparison.Ordinal)
					|| !ReferenceEquals(Campaign.Current?.ConversationManager, expectedConversationManager)
					|| expectedConversationManager == null
					|| expectedConversationManager.ActiveToken != expectedConversationToken
					|| !snapshot.DetachedFacts.TryGetValue("native_conversation_token", out string capturedToken)
					|| !int.TryParse(capturedToken, out int parsedToken)
					|| parsedToken != expectedConversationToken)
				{
					return false;
				}
				return IsNativeConversationResponseTargetAvailableForActionDispatch(
					targetAgentIndex,
					targetHero,
					targetCharacter,
					out _);
			}
			catch
			{
				return false;
			}
		};
		IEconomyRewardDebtMainThreadPort economyPort = CreateEconomyReplayPortForExternal(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			npcName,
			expectedSubjectId);

		return LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
			(actionPlan, snapshot, duelDispatchContext) =>
		{
			if (!isCurrentNativeContext(snapshot))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] detached action rejected because session or target is stale");
				return InteractionStatus.RejectedByValidation;
			}
			string content = actionPlan?.RawPostprocessId ?? "";
			using var diplomacySource = DiplomacyDialogueSourceScope.Begin(content, "native", snapshot.Identity.SessionId,
				snapshot.PlayerText, content);
			NativeConversationGameActionResult actionResult = instance._nativeGameEffects.ApplyNativeConversationGameActionsLegacyCore(
				targetHero,
				targetCharacter,
				targetNpc,
				allNpcData,
				sceneSummonTargets,
				sceneGuideTargets,
				content,
				snapshot?.PlayerText ?? "",
				expectedConversationManager,
				expectedConversationToken,
				duelDispatchContext);
			return actionResult != null && !actionResult.ResponseDiscarded
				? InteractionStatus.Executed
				: InteractionStatus.RejectedByValidation;
		},
			DuelBehavior.CreateDetachedDuelDispatchOwnerForExternal(),
			allowedTagFamilies: LegacyActionTagCatalog.DefaultAllowedTagFamilies,
			economyPlanner: economyPort == null ? null : new LegacyEconomyRewardDebtAdapter(),
			economyPort: economyPort,
			economyCapabilities: economyPort == null ? null : LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
			economyExecutionGate: (actionPlan, snapshot, isEconomyOnly) =>
				isCurrentNativeContext(snapshot)
					? InteractionStatus.Executed
					: InteractionStatus.RejectedByValidation);
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

	private async Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInCoreAsync(
		LegacyNativeConversationFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		Func<Task<string>> fallbackToLegacyNative,
		CancellationToken cancellationToken)
	{
		if (facade == null)
		{
			return await CompleteNativeConversationOptInFallbackAsync("missing_facade", fallbackToLegacyNative).ConfigureAwait(false);
		}

		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		DetachedInteractionHostResult hostResult = await host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => CreateNativeConversationActionPlanExecutorForExternal(),
			envelope => CreateNativeConversationMemoryFacadeForExternal(),
			(envelope, commit) => DispatchNativeConversationOptInCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				envelope?.Snapshot?.Candidates?.FirstOrDefault()?.AgentIndex ?? (-1)),
			fallbackToLegacyNative,
			cancellationToken).ConfigureAwait(false);
		return new LegacyNativeConversationOptInResult(
			hostResult?.VisibleReply ?? "",
			hostResult?.UsedLegacyFallback ?? false,
			hostResult?.Status ?? InteractionStatus.NonRetryableFailure,
			hostResult?.ErrorCode ?? "missing_host_result",
			hostResult?.DetachedResult,
			hostResult?.Commit);
	}

	private static IInteractionMemory CreateNativeConversationMemoryFacadeForExternal()
	{
		if (!TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string targetName))
		{
			return null;
		}
		return targetHero != null
			? new MyBehaviorMemoryFacade(targetHero)
			: new MyBehaviorMemoryFacade(
				targetCharacter?.StringId ?? "native:unknown",
				string.IsNullOrWhiteSpace(targetName) ? "NPC" : targetName);
	}

	private Task<InteractionCommitResult> DispatchNativeConversationOptInCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
		return RunNativeConversationMainThreadFuncAsync(
			"detached_opt_in_commit",
			targetLog,
			targetAgentIndex,
			commit,
			new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"main_thread_dispatch_failed"));
	}

	private static async Task<LegacyNativeConversationOptInResult> CompleteNativeConversationOptInFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacyNative)
	{
		if (fallbackToLegacyNative == null)
		{
			return new LegacyNativeConversationOptInResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode,
				null,
				null);
		}
		try
		{
			return new LegacyNativeConversationOptInResult(
				await fallbackToLegacyNative().ConfigureAwait(false),
				true,
				InteractionStatus.Succeeded,
				errorCode,
				null,
				null);
		}
		catch (Exception exception)
		{
			return new LegacyNativeConversationOptInResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode + ";legacy_" + exception.GetType().Name,
				null,
				null);
		}
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
		List<string> tagFamilies = (allowedTagFamilies ?? Enumerable.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		LegacyDetachedPromptComposer mainComposer = new LegacyDetachedPromptComposer(model: "legacy-scene-shout");
		LegacyDetachedPostprocessPromptComposer postprocessComposer = new LegacyDetachedPostprocessPromptComposer(model: "legacy-scene-shout-postprocess");
		LegacyActionTagParser actionParser = new LegacyActionTagParser(maxActions);
		CapabilitySet capabilities = new CapabilitySet(new[]
		{
			"llm.generate",
			"prompt.compose",
			"postprocess.compose",
			"action.parse"
		});
		return new LegacyInteractionPipelinePorts(
			snapshot => new RuleSelection(new[] { "scene_shout" }, Array.Empty<string>()),
			(envelope, selection, availableCapabilities) => mainComposer.Compose(envelope, selection, availableCapabilities),
			(snapshot, selection, availableCapabilities) => new PostprocessContext(selection?.RuleIds, tagFamilies, availableCapabilities),
			(rawText, context) => actionParser.Parse(rawText, context),
			(rawText, internalTagFamilies) => LlmVisibleReplyNormalizer.NormalizeComplete(rawText),
			capabilities,
			(envelope, selection, visibleReply, rawReply, context) => postprocessComposer.Compose(envelope, selection, visibleReply, rawReply, context));
	}







	internal static string PrepareSceneMainReplySpeechText(string text, bool stopFollowing, bool endSummon)
	{
		// Main text cannot authorize domain actions. Only the already-validated Scene
		// end-of-conversation controls may be restored for the existing speech owner.
		string speechText = StripActionTagsForSceneSpeech(StripAutoGroupStopSignal(StripAutoGroupRelaySignal(text)));
		if (stopFollowing) speechText = (speechText + " [STP]").Trim();
		if (endSummon) speechText = (speechText + " [END]").Trim();
		return speechText;
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
		ShoutBehavior instance = CurrentInstance;
		if (instance == null || targetAgentIndex < 0)
		{
			return null;
		}
		IReadOnlyList<string> allowedTagFamilies = LegacyActionTagCatalog.DefaultAllowedTagFamilies;
		Agent capturedAgent = Mission.Current?.Agents?.FirstOrDefault(candidate => candidate != null && candidate.Index == targetAgentIndex);
		CharacterObject capturedCharacter = capturedAgent?.Character as CharacterObject;
		Hero capturedHero = capturedCharacter?.HeroObject;
		NpcDataPacket capturedNpc = capturedAgent == null ? null : ShoutUtils.ExtractNpcData(capturedAgent);
		string expectedSceneSubjectId = ResolveDetachedInteractionSubjectId(
			capturedHero,
			capturedCharacter,
			targetAgentIndex,
			capturedNpc);
		Func<GameInteractionSnapshot, bool> isCurrentSceneContext = snapshot =>
		{
			try
			{
				if (!IsBannerlordMainThreadForNativeActions()
					|| snapshot?.Identity == null
					|| snapshot.Identity.Channel != InteractionChannel.SceneShout
					|| !string.Equals(snapshot.Identity.SubjectId, expectedSceneSubjectId, StringComparison.Ordinal)
					|| !snapshot.DetachedFacts.TryGetValue("scene_session_id", out string sceneSessionToken)
					|| !int.TryParse(sceneSessionToken, out int capturedSceneSessionId)
					|| capturedSceneSessionId != GetCurrentSceneHistorySessionIdForExternal())
				{
					return false;
				}
				InteractionCandidate candidate = snapshot.Candidates?.FirstOrDefault(
					item => item != null && item.AgentIndex == targetAgentIndex);
				Agent liveAgent = Mission.Current?.Agents?.FirstOrDefault(
					item => item != null && item.Index == targetAgentIndex);
				if (candidate == null
					|| !candidate.IsAlive
					|| liveAgent == null
					|| !liveAgent.IsActive()
					|| !CanAgentParticipateInSceneSpeech(liveAgent))
				{
					return false;
				}
				CharacterObject liveCharacter = liveAgent.Character as CharacterObject;
				string liveStableId = liveCharacter?.HeroObject?.StringId
					?? liveCharacter?.StringId
					?? "agent:" + targetAgentIndex;
				return string.Equals(candidate.StableId, liveStableId, StringComparison.Ordinal);
			}
			catch
			{
				return false;
			}
		};
		IEconomyRewardDebtMainThreadPort economyPort = CreateEconomyReplayPortForExternal(
			capturedHero,
			capturedCharacter,
			targetAgentIndex,
			capturedCharacter?.Name?.ToString(),
			expectedSceneSubjectId);
		return LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
			(actionPlan, snapshot, duelDispatchContext) =>
		{
			if (!isCurrentSceneContext(snapshot))
			{
				return InteractionStatus.RejectedByValidation;
			}
			if (duelDispatchContext != null
				&& (!snapshot.DetachedFacts.TryGetValue("scene_session_id", out string sceneSessionToken)
					|| !int.TryParse(sceneSessionToken, out int capturedSceneSessionId)
					|| capturedSceneSessionId != GetCurrentSceneHistorySessionIdForExternal()))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] exact Duel rejected because scene session is stale");
				return InteractionStatus.RejectedByValidation;
			}
			InteractionCandidate capturedCandidate = snapshot.Candidates?.FirstOrDefault(
				candidate => candidate != null && candidate.AgentIndex == targetAgentIndex);
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(
				candidate => candidate != null && candidate.Index == targetAgentIndex);
			if (capturedCandidate == null
				|| !capturedCandidate.IsAlive
				|| agent == null
				|| !agent.IsActive()
				|| !CanAgentParticipateInSceneSpeech(agent))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] scene target unavailable agent=" + targetAgentIndex);
				return InteractionStatus.RejectedByValidation;
			}
			CharacterObject targetCharacter = agent.Character as CharacterObject;
			Hero targetHero = targetCharacter?.HeroObject;
			string currentStableId = targetHero?.StringId ?? targetCharacter?.StringId ?? "agent:" + targetAgentIndex;
			if (!string.Equals(capturedCandidate.StableId, currentStableId, StringComparison.Ordinal))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] scene target identity changed agent=" + targetAgentIndex);
				return InteractionStatus.RejectedByValidation;
			}
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc == null)
			{
				return InteractionStatus.RejectedByValidation;
			}
			string content = actionPlan.RawPostprocessId ?? string.Empty;
			using var diplomacySource = DiplomacyDialogueSourceScope.Begin(content, "scene", snapshot.Identity.SessionId,
				snapshot.PlayerText, content);
			bool consumed = instance.TryApplyDeferredSceneMoodTag(npc, content);
			content = StripDeferredSceneMoodTags(content);
			if (instance.TryApplyDeferredScenePostprocessActionTagsDirectly(
				targetHero,
				targetCharacter,
				targetAgentIndex,
				ref content,
				snapshot.PlayerText ?? string.Empty,
				string.Empty,
				"scene-refactor",
				replyIsDirectPlayerResponse: true,
				duelDispatchContext: duelDispatchContext))
			{
				consumed = true;
				content = ExtractDeferredSceneActionTags(content);
			}
			if (instance._sceneMovement.TryExecuteDeferredSceneFollowTagsDirectly(npc, content))
			{
				consumed = true;
			}
			return consumed ? InteractionStatus.Executed : InteractionStatus.RejectedByValidation;
		},
			DuelBehavior.CreateDetachedDuelDispatchOwnerForExternal(),
			maxActions,
		 allowedTagFamilies,
		 economyPort == null ? null : new LegacyEconomyRewardDebtAdapter(),
		 economyPort,
			 economyPort == null ? null : LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
			economyExecutionGate: (actionPlan, snapshot, isEconomyOnly) =>
				isCurrentSceneContext(snapshot)
					? InteractionStatus.Executed
					: InteractionStatus.RejectedByValidation);
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

	private async Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInCoreAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		int targetAgentIndex,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (facade == null || targetAgentIndex < 0)
		{
			return await RunDetachedRefactorFallbackAsync("missing_scene_facade", fallbackToLegacy).ConfigureAwait(false);
		}
		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		DetachedInteractionHostResult result = await host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => CreateSceneShoutActionPlanExecutorForExternal(targetAgentIndex),
			CreateSceneShoutMemoryFacadeForExternal,
			(envelope, commit) => DispatchSceneShoutRefactorCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				targetAgentIndex),
			fallbackToLegacy,
			cancellationToken).ConfigureAwait(false);
		return result;
	}

	private static IInteractionMemory CreateSceneShoutMemoryFacadeForExternal(InteractionEnvelope envelope)
	{
		GameInteractionSnapshot snapshot = envelope?.Snapshot;
		if (snapshot?.Identity == null)
		{
			return null;
		}
		string memoryKind = snapshot.DetachedFacts.TryGetValue("memory_kind", out string kind)
			? kind
			: string.Empty;
		if (string.Equals(memoryKind, "hero", StringComparison.OrdinalIgnoreCase))
		{
			Hero hero = Hero.Find(snapshot.Identity.SubjectId);
			return hero == null ? null : new MyBehaviorMemoryFacade(hero);
		}
		string memoryId = snapshot.DetachedFacts.TryGetValue("memory_id", out string detachedMemoryId)
			? detachedMemoryId
			: snapshot.Identity.SubjectId;
		if (string.IsNullOrWhiteSpace(memoryId)
			|| !snapshot.DetachedFacts.TryGetValue("memory_kind", out string resolvedKind)
			|| string.Equals(resolvedKind, "unresolved", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string name = snapshot.Candidates?.FirstOrDefault()?.DisplayName;
		return new MyBehaviorMemoryFacade(memoryId, name);
	}

	private Task<InteractionCommitResult> DispatchSceneShoutRefactorCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
		return RunNativeConversationMainThreadFuncAsync(
			"detached_scene_commit",
			targetLog,
			targetAgentIndex,
			commit,
			new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"main_thread_dispatch_failed"));
	}

	internal static async Task<DetachedInteractionHostResult> RunDetachedRefactorFallbackAsync(
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
		return new LegacyDetachedRuleSelector(
			(userText, secondaryText, runtimeContext, requestedTopN, excludedRuleIds) =>
			{
				if (AIConfigHandler.TryCallAuxiliaryRuleCodesForExternal(
					userText,
					secondaryText,
					runtimeContext,
					requestedTopN,
					out List<string> ruleIds,
					out string error,
					excludedRuleIds))
				{
					return new DetachedRuleLookupResult(ruleIds, null);
				}
				return new DetachedRuleLookupResult(Array.Empty<string>(), string.IsNullOrWhiteSpace(error) ? "failed" : error);
			},
			 topN);
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

	private static bool TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
		targetHero = null;
		targetCharacter = null;
		npcName = "";
		try
		{
			targetHero = Hero.OneToOneConversationHero;
		}
		catch
		{
			targetHero = null;
		}
		try
		{
			targetCharacter = CharacterObject.OneToOneConversationCharacter;
		}
		catch
		{
			targetCharacter = null;
		}
		if (targetHero == null)
		{
			targetHero = targetCharacter?.HeroObject;
		}
		if ((targetHero == null && targetCharacter == null) || IsNativeConversationSelfTarget(targetHero, targetCharacter))
		{
			if (TryResolveNativeConversationTargetFromAgent(out var agentHero, out var agentCharacter, out var agentName))
			{
				targetHero = agentHero;
				targetCharacter = agentCharacter;
				npcName = agentName;
			}
		}
		if (targetHero == null)
		{
			try
			{
				targetHero = PlayerEncounter.EncounteredParty?.LeaderHero;
			}
			catch
			{
				targetHero = null;
			}
		}
		if (targetCharacter == null)
		{
			targetCharacter = targetHero?.CharacterObject;
		}
		if (targetCharacter == null)
		{
			try
			{
				targetCharacter = TaleWorlds.CampaignSystem.Conversation.ConversationHelper.GetConversationCharacterPartyLeader(PlayerEncounter.EncounteredParty);
			}
			catch
			{
				targetCharacter = null;
			}
		}
		if (targetHero == null)
		{
			targetHero = targetCharacter?.HeroObject;
		}
		if (targetHero == null && TryResolveNativeConversationTargetFromAgent(out var directAgentHero, out var directAgentCharacter, out var directAgentName) && !IsNativeConversationSelfTarget(directAgentHero, directAgentCharacter))
		{
			targetHero = directAgentHero;
			targetCharacter = directAgentCharacter;
			npcName = directAgentName;
		}
		if (IsNativeConversationSelfTarget(targetHero, targetCharacter))
		{
			if (TryResolveNativeConversationTargetFromAgent(out var agentHero, out var agentCharacter, out var agentName) && !IsNativeConversationSelfTarget(agentHero, agentCharacter))
			{
				targetHero = agentHero;
				targetCharacter = agentCharacter;
				npcName = agentName;
			}
			else
			{
				targetHero = null;
				targetCharacter = null;
				npcName = "";
				return false;
			}
		}
		npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
		return targetHero != null || targetCharacter != null;
	}

	private static bool IsNativeConversationSelfTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			if (targetHero != null && targetHero == Hero.MainHero)
			{
				return true;
			}
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter ?? Hero.MainHero?.CharacterObject;
			return targetCharacter != null && playerCharacter != null && targetCharacter == playerCharacter;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryResolveNativeConversationTargetFromAgent(out Hero targetHero, out CharacterObject targetCharacter, out string npcName)
	{
		targetHero = null;
		targetCharacter = null;
		npcName = "";
		try
		{
			Agent agent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (agent == null || agent.IsMainAgent || agent.Character == null)
			{
				return false;
			}
			targetCharacter = agent.Character as CharacterObject;
			targetHero = targetCharacter?.HeroObject;
			if (IsNativeConversationSelfTarget(targetHero, targetCharacter))
			{
				targetHero = null;
				targetCharacter = null;
				return false;
			}
			npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? agent.Name ?? "").Trim();
			return targetHero != null || targetCharacter != null;
		}
		catch
		{
			targetHero = null;
			targetCharacter = null;
			npcName = "";
			return false;
		}
	}

	internal static NpcDataPacket BuildNativeConversationNpcData(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			int agentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			var agents = Mission.Current?.Agents;
			if (agentIndex >= 0 && agents != null)
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
				NpcDataPacket sceneNpc = ShoutUtils.ExtractNpcData(agent);
				if (sceneNpc != null)
				{
					sceneNpc.AgentIndex = agentIndex;
					return sceneNpc;
				}
			}
		}
		catch
		{
		}
		string npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
		string role = "";
		string personality = "";
		string background = "";
		try
		{
			role = targetHero != null ? MyBehavior.BuildHeroIdentityTitleForExternal(targetHero) : (targetCharacter?.Occupation.ToString() ?? "");
		}
		catch
		{
			role = "";
		}
		if (targetHero != null)
		{
			try
			{
				MyBehavior.GetNpcPersonaForExternal(targetHero, out personality, out background);
			}
			catch
			{
				personality = "";
				background = "";
			}
		}
		Hero npcHero = targetHero ?? targetCharacter?.HeroObject;
		bool isNonHero = npcHero == null;
		string troopId = targetCharacter?.StringId ?? targetHero?.CharacterObject?.StringId ?? "";
		int unnamedAgentIndex = isNonHero ? TryResolveNativeConversationAgentIndex(targetHero, targetCharacter) : -1;
		string unnamedKey = isNonHero ? BuildNativeConversationNonHeroUnnamedKey(targetCharacter, npcName, unnamedAgentIndex) : "";
		string unnamedRank = "";
		if (isNonHero)
		{
			try
			{
				unnamedRank = targetCharacter != null && targetCharacter.IsSoldier ? "soldier" : "commoner";
			}
			catch
			{
				unnamedRank = "";
			}
		}
		float age = npcHero?.Age ?? (isNonHero ? ResolveNativeConversationNonHeroAge(targetCharacter) : 0f);
		return new NpcDataPacket
		{
			Name = npcName,
			RoleDesc = string.IsNullOrWhiteSpace(role) ? "原版对话对象" : role,
			PersonalityDesc = (personality ?? "").Trim(),
			BackgroundDesc = (background ?? "").Trim(),
			AgentIndex = -1,
			IsHero = !isNonHero,
			CultureId = targetHero?.Culture?.StringId ?? targetCharacter?.Culture?.StringId ?? "neutral",
			UnnamedKey = unnamedKey,
			TroopId = troopId,
			UnnamedRank = unnamedRank,
			IsFemale = targetHero?.IsFemale ?? targetCharacter?.IsFemale ?? false,
			Age = age,
			PromptGivenName = npcName,
			PromptDisplayName = npcName
		};
	}

	private sealed class PendingNativeSceneMechanismAction
	{
		public NpcDataPacket Speaker;

		public List<NpcDataPacket> Context;

		public List<SceneSummonPromptTarget> SummonTargets;

		public List<SceneGuidePromptTarget> GuideTargets;

		public string Tags;

		public long CreatedUtcTicks;
	}

	private sealed class PendingNativeSceneTauntFight
	{
		public Hero TargetHero;

		public CharacterObject TargetCharacter;

		public int TargetAgentIndex;

		public string TargetKey;

		public string TargetName;

		public float ExecuteAtMissionTime;

		public long CreatedUtcTicks;
	}

	private bool TryQueueNativeSceneMechanismActionAfterConversationExit(NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, ref string content)
	{
		if (ContainsOpenLordsHallActionTag(content))
		{
			// OPEN_LORDS_HALL is an immediate scene transition, never an escort follow-up.
			return false;
		}
		string text = ExtractSceneMechanismActionTagsForScene(content);
		if (npc == null || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		content = StripSceneMechanismActionTagsForScene(content);
		PendingNativeSceneMechanismAction pending = new PendingNativeSceneMechanismAction
		{
			Speaker = CloneNpcDataPacket(npc),
			Context = CloneNpcDataSnapshot(allNpcData),
			SummonTargets = SceneMovementController.CloneSceneSummonPromptTargets(sceneSummonTargets),
			GuideTargets = SceneMovementController.CloneSceneGuidePromptTargets(sceneGuideTargets),
			Tags = text,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		int pendingCount;
		lock (_pendingNativeSceneMechanismActionLock)
		{
			_pendingNativeSceneMechanismActions.Enqueue(pending);
			pendingCount = _pendingNativeSceneMechanismActions.Count;
		}
		ShowNativeSceneMechanismPendingExitPrompt(pendingCount);
		Logger.Log("ShoutBehavior", "[NativeConversation] deferred scene mechanism action until manual conversation exit agent=" + pending.Speaker.AgentIndex + " pending=" + pendingCount + " tags=" + text.Replace("\r", "\\r").Replace("\n", "\\n"));
		return true;
	}

	private static void ShowNativeSceneMechanismPendingExitPrompt(int pendingCount)
	{
		try
		{
			string suffix = pendingCount > 1 ? (" 当前待执行 " + pendingCount + " 个。") : "";
			InformationManager.DisplayMessage(new InformationMessage("SCENE_MOVE已准备。请手动退出对话框，退出后将执行NPC场景动作。" + suffix, new Color(1f, 0.95f, 0.25f)));
		}
		catch
		{
		}
	}

	private void ExecutePendingNativeSceneMechanismActionsAfterConversationExit(string reason)
	{
		List<PendingNativeSceneMechanismAction> pendingActions = new List<PendingNativeSceneMechanismAction>();
		lock (_pendingNativeSceneMechanismActionLock)
		{
			while (_pendingNativeSceneMechanismActions.Count > 0)
			{
				pendingActions.Add(_pendingNativeSceneMechanismActions.Dequeue());
			}
		}
		if (pendingActions.Count == 0)
		{
			return;
		}
		foreach (PendingNativeSceneMechanismAction pending in pendingActions)
		{
			_mainThreadActions.Enqueue(delegate
			{
				ExecutePendingNativeSceneMechanismAction(pending, reason);
			});
		}
		Logger.Log("ShoutBehavior", "[NativeConversation] queued deferred scene mechanism actions after manual exit count=" + pendingActions.Count + " reason=" + (reason ?? ""));
		TryDrainNativeConversationQueuedActions("scene_mechanism_after_conversation_exit");
	}

	private void ExecutePendingNativeSceneMechanismAction(PendingNativeSceneMechanismAction pending, string reason)
	{
		if (pending == null || pending.Speaker == null || string.IsNullOrWhiteSpace(pending.Tags))
		{
			return;
		}
		try
		{
			if (TryExecuteNativeSceneMechanismActionTagsDirectly(pending.Speaker, pending.SummonTargets, pending.GuideTargets, pending.Tags))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] executed deferred scene mechanism action agent=" + pending.Speaker.AgentIndex + " reason=" + (reason ?? "") + " tags=" + pending.Tags.Replace("\r", "\\r").Replace("\n", "\\n"));
				return;
			}
			EnqueueSpeechLineWithOptions(pending.Speaker, pending.Tags, pending.Context, commitHistory: false, suppressStare: true, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, pending.SummonTargets, pending.GuideTargets, null);
			Logger.Log("ShoutBehavior", "[NativeConversation] queued deferred scene mechanism action fallback agent=" + pending.Speaker.AgentIndex + " reason=" + (reason ?? "") + " tags=" + pending.Tags.Replace("\r", "\\r").Replace("\n", "\\n"));
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] execute deferred scene mechanism action failed: " + ex.Message);
		}
	}

	private void ClearPendingNativeSceneMechanismActions(string reason)
	{
		try
		{
			int count;
			lock (_pendingNativeSceneMechanismActionLock)
			{
				count = _pendingNativeSceneMechanismActions.Count;
				_pendingNativeSceneMechanismActions.Clear();
			}
			if (count > 0)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] cleared pending scene mechanism actions count=" + count + " reason=" + (reason ?? ""));
			}
		}
		catch
		{
		}
	}

	private bool ScheduleNativeSceneTauntFightAfterDelay(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, string reason)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return false;
			}
			string targetName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(targetName))
			{
				targetName = "NPC";
			}
			PendingNativeSceneTauntFight pending = new PendingNativeSceneTauntFight
			{
				TargetHero = targetHero,
				TargetCharacter = targetCharacter,
				TargetAgentIndex = targetAgentIndex,
				TargetKey = targetKey,
				TargetName = targetName,
				ExecuteAtMissionTime = mission.CurrentTime + NativeSceneTauntFightDelaySeconds,
				CreatedUtcTicks = DateTime.UtcNow.Ticks
			};
			lock (_pendingNativeSceneTauntFightLock)
			{
				_pendingNativeSceneTauntFight = pending;
			}
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("SCENE_TAUNT_FIGHT已触发：10秒后将自动退出对话并爆发冲突。", new Color(1f, 0.55f, 0.25f)));
			}
			catch
			{
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] delayed scene taunt fight scheduled target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? targetName) + " agentIndex=" + targetAgentIndex + " executeAt=" + pending.ExecuteAtMissionTime.ToString("F2") + " reason=" + (reason ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] schedule delayed scene taunt fight failed: " + ex.Message);
			return false;
		}
	}

	private void UpdatePendingNativeSceneTauntFightDelay()
	{
		PendingNativeSceneTauntFight pending = null;
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return;
			}
			lock (_pendingNativeSceneTauntFightLock)
			{
				if (_pendingNativeSceneTauntFight == null)
				{
					return;
				}
				double elapsedRealSeconds = new TimeSpan(Math.Max(0L, DateTime.UtcNow.Ticks - _pendingNativeSceneTauntFight.CreatedUtcTicks)).TotalSeconds;
				bool missionTimeElapsed = mission.CurrentTime >= _pendingNativeSceneTauntFight.ExecuteAtMissionTime;
				bool realTimeElapsed = elapsedRealSeconds >= NativeSceneTauntFightDelaySeconds;
				if (!missionTimeElapsed && !realTimeElapsed)
				{
					return;
				}
				pending = _pendingNativeSceneTauntFight;
				_pendingNativeSceneTauntFight = null;
			}
			ExecuteDelayedNativeSceneTauntFight(pending);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] update delayed scene taunt fight failed: " + ex.Message);
			if (pending != null)
			{
				ClearPendingNativeSceneTauntFight("update_failed");
			}
		}
	}

	private void ExecuteDelayedNativeSceneTauntFight(PendingNativeSceneTauntFight pending)
	{
		if (pending == null)
		{
			return;
		}
		try
		{
			CloseNativeConversationForSceneMechanism("native_scene_taunt_fight_delay_elapsed");
			bool started = SceneTauntBehavior.TryStartSceneTauntFightForExternal(pending.TargetHero, pending.TargetCharacter, pending.TargetAgentIndex, pending.TargetKey, "native_scene_taunt_fight_delay_elapsed");
			try
			{
				InformationManager.DisplayMessage(new InformationMessage(started ? "冲突爆发！" : "冲突未能爆发：当前场景目标不可用。", started ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.75f, 0.25f)));
			}
			catch
			{
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] delayed scene taunt fight executed started=" + started + " target=" + (pending.TargetHero?.StringId ?? pending.TargetCharacter?.StringId ?? pending.TargetName ?? "unknown") + " agentIndex=" + pending.TargetAgentIndex);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] execute delayed scene taunt fight failed: " + ex.Message);
		}
	}

	private void ClearPendingNativeSceneTauntFight(string reason)
	{
		try
		{
			bool hadPending;
			lock (_pendingNativeSceneTauntFightLock)
			{
				hadPending = _pendingNativeSceneTauntFight != null;
				_pendingNativeSceneTauntFight = null;
			}
			if (hadPending)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] cleared delayed scene taunt fight reason=" + (reason ?? ""));
			}
		}
		catch
		{
		}
	}

	private bool TryExecuteNativeSceneMechanismActionTagsDirectly(NpcDataPacket npc, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string tags)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (npc == null || string.IsNullOrWhiteSpace(tags) || agents == null)
		{
			return false;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		string content = tags;
		bool openLordsHall = TryTriggerOpenLordsHallAction(npc, agent, ref content);
		if (openLordsHall)
		{
			bool waitForConversationEnd = IsNativeConversationActiveForLordsHallEntry();
			ScheduleLordsHallMissionEntryAfterSpeech(npc.AgentIndex, null, "native_scene_direct_tag", waitForConversationEnd);
			if (waitForConversationEnd)
			{
				CloseNativeConversationForSceneMechanism("native_scene_open_lords_hall_tag");
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct result agent=" + npc.AgentIndex + " openLordsHall=True");
			return true;
		}
		bool setsOwnedMassacre = TryProcessSetsOwnedSettlementMassacreActionTags(npc.AgentIndex, ref content);
		if (setsOwnedMassacre && string.IsNullOrWhiteSpace(content))
		{
			return true;
		}
		if (SceneMovementController.IsPrisonBreakRescueMissionActive() && SceneMovementController.HasSceneFollowCommandTag(tags))
		{
			Agent commandAgent = SceneMovementController.ResolvePrisonBreakSceneFollowCommandAgent(agent);
			if (commandAgent != null && commandAgent != agent)
			{
				Logger.Log("SceneFollow", "prison_break_native_follow_redirect requested=" + (agent?.Index ?? npc.AgentIndex) + " prisoner=" + commandAgent.Index);
				agent = commandAgent;
				npc = ShoutUtils.ExtractNpcData(commandAgent) ?? npc;
			}
		}
		if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct skip agent=" + (npc?.AgentIndex ?? -1) + " reason=agent_unavailable tags=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
			return false;
		}
		SceneSummonConversationSession sceneSummonConversationSession = null;
		ActiveSceneSummonRequest activeSceneSummonRequest = null;
		ActiveSceneGuideRequest activeSceneGuideRequest = null;
		bool stopFollow = _sceneMovement.TryConsumeSceneFollowStopTag(npc, agent, ref content);
		bool startFollow = _sceneMovement.TryConsumeSceneFollowStartTag(npc, agent, ref content);
		bool endChat = _sceneMovement.TryConsumeSceneEndChatActionTag(npc, agent, ref content, out sceneSummonConversationSession);
		bool summon = !openLordsHall && !string.IsNullOrWhiteSpace(content) && _sceneMovement.TryTriggerSceneSummonAction(npc, agent, sceneSummonTargets, sceneGuideTargets, ref content, out activeSceneSummonRequest);
		bool guide = !openLordsHall && !string.IsNullOrWhiteSpace(content) && _sceneMovement.TryTriggerSceneGuideAction(npc, agent, sceneGuideTargets, sceneSummonTargets, ref content, out activeSceneGuideRequest);
		bool handled = setsOwnedMassacre || openLordsHall || stopFollow || startFollow || endChat || summon || guide;
		if (!handled)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct no-op agent=" + npc.AgentIndex + " tags=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
			return false;
		}
		if (stopFollow)
		{
			_sceneMovement.StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			_sceneMovement.ReturnAgentAfterStoppingSceneFollow(agent);
		}
		else if (startFollow)
		{
			_sceneMovement.RememberSceneFollowReturnState(agent, overwriteExisting: true);
			_sceneMovement.RemoveAgentFromSceneSummonConversationForFollow(agent);
			_sceneMovement.StartSceneSummonFollowPlayer(agent);
		}
		if (endChat && sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
		if (summon && activeSceneSummonRequest != null)
		{
			_sceneMovement.SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, null, "");
		}
		if (guide && activeSceneGuideRequest != null)
		{
			_sceneMovement.SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, null, "");
		}
		Logger.Log("ShoutBehavior", "[NativeConversation] scene mechanism direct result agent=" + npc.AgentIndex + " openLordsHall=" + openLordsHall + " setsMassacre=" + setsOwnedMassacre + " stop=" + stopFollow + " start=" + startFollow + " end=" + endChat + " summon=" + summon + " guide=" + guide + " remaining=" + ((content ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
		return true;
	}

	internal static void CloseNativeConversationForSceneMechanism(string reason)
	{
		try
		{
			CloseNativeConversationInput(clearSessionHistory: false);
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager != null && (conversationManager.IsConversationInProgress || conversationManager.IsConversationFlowActive))
			{
				conversationManager.EndConversation();
				Logger.Log("ShoutBehavior", "[NativeConversation] closed native conversation for scene mechanism. reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] close native conversation for scene mechanism failed: " + ex.Message);
		}
	}

	private bool TryTriggerNativeConversationOpenLordsHallAction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content)
	{
		if (!ContainsOpenLordsHallActionTag(content))
		{
			return false;
		}
		try
		{
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			npc.AgentIndex = targetAgentIndex;
			Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			bool result = TryTriggerOpenLordsHallAction(npc, agent, ref content);
			if (result)
			{
				content = StripActionTagsForSceneSpeech(content);
				bool waitForConversationEnd = IsNativeConversationActiveForLordsHallEntry();
				ScheduleLordsHallMissionEntryAfterSpeech(targetAgentIndex, null, "native_conversation_tag", waitForConversationEnd);
				if (waitForConversationEnd)
				{
					CloseNativeConversationForSceneMechanism("native_conversation_open_lords_hall_tag");
				}
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] open lords hall tag handled=" + result + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex);
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] open lords hall action failed: " + ex.Message);
			return false;
		}
	}

	private bool TryProcessSetsOwnedSettlementMassacreActionTags(int targetAgentIndex, ref string content)
	{
		try
		{
			bool requestPermission = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreRequestActionTagRegex.IsMatch(content);
			bool startRequested = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreStartActionTagRegex.IsMatch(content);
			bool stopRequested = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreStopActionTagRegex.IsMatch(content);
			bool cancelRequest = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreCancelRequestActionTagRegex.IsMatch(content);
			if (!requestPermission && !startRequested && !stopRequested && !cancelRequest)
			{
				return false;
			}

			content = StripSetsOwnedSettlementMassacreActionTags(content);
			Mission mission = Mission.Current;
			if (!SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(mission))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] ignored SETS massacre tag outside owned/attached settlement scene. agent=" + targetAgentIndex);
				return false;
			}

			bool massacreActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(mission);
			bool requestPendingForSpeaker = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreRequestPendingForExternal(mission, targetAgentIndex);
			bool anyRequestPending = SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(mission);
			bool handled = false;
			string action = "none";
			if (stopRequested && massacreActive)
			{
				action = "stop";
				handled = SettlementEntryTroopSelectionBehavior.TryStopOwnedOrAttachedSettlementMassacreForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.StopSource);
			}
			else if (cancelRequest && !massacreActive && requestPendingForSpeaker)
			{
				action = "cancel_request";
				handled = SettlementEntryTroopSelectionBehavior.TryCancelOwnedOrAttachedSettlementMassacreRequestForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.CancelRequestSource);
			}
			else if (startRequested && !massacreActive)
			{
				action = "start";
				handled = SettlementEntryTroopSelectionBehavior.TryStartOwnedOrAttachedSettlementMassacreForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.StartSource);
			}
			else if (requestPermission && !massacreActive && !anyRequestPending)
			{
				action = "request_permission";
				handled = SettlementEntryTroopSelectionBehavior.TryRecordOwnedOrAttachedSettlementMassacreRequestForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.RequestSource);
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] SETS owned/attached massacre tag action=" + action
				+ " handled=" + handled
				+ " activeBefore=" + massacreActive
				+ " pendingForSpeaker=" + requestPendingForSpeaker
				+ " anyPending=" + anyRequestPending
				+ " agent=" + targetAgentIndex);
			return handled;
		}
		catch (Exception ex)
		{
			content = StripSetsOwnedSettlementMassacreActionTags(content);
			Logger.Log("ShoutBehavior", "[NativeConversation] SETS owned/attached massacre action failed: " + ex.Message);
			return false;
		}
	}

	private static string StripSetsOwnedSettlementMassacreActionTags(string content)
	{
		return ConversationActionPostprocessOwner.StripSetsOwnedSettlementMassacreActionTags(content);
	}

	internal static bool TryProcessCustomPolicyAgendaActionTag(Hero targetHero, string chainName, string playerProposalText, ref string content, string npcReplyTextOverride = null)
	{
		if (string.IsNullOrWhiteSpace(content) || !CustomPolicyAgendaActionTagRegex.IsMatch(content))
		{
			return false;
		}
		string resolvedChainName = string.IsNullOrWhiteSpace(chainName) ? "native" : chainName.Trim();
		string npcReplyText = StripActionTagsForSceneSpeech(string.IsNullOrWhiteSpace(npcReplyTextOverride) ? content : npcReplyTextOverride);
		string failureReason = "";
		bool started = false;
		try
		{
			started = TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(targetHero, resolvedChainName, playerProposalText, npcReplyText, ref content, out failureReason);
			Logger.Log("ShoutBehavior", "[CustomPolicyAgenda] dispatch chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null") + " started=" + started + " reason=" + (failureReason ?? ""));
		}
		catch (Exception ex)
		{
			failureReason = ex.GetType().Name + ": " + ex.Message;
			Logger.Log("ShoutBehavior", "[CustomPolicyAgenda] dispatch exception chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null") + " error=" + failureReason);
			PolicySystemLog.Failure("CustomPolicyAgenda", "dispatch-failure", "Policy proposal action dispatch failed. chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null"), failureReason);
		}
		finally
		{
			// Internal action tags must never leak into scene/native/courier-visible text,
			// including rejected or exceptional paths.
			content = CustomPolicyAgendaActionTagRegex.Replace(content ?? "", "").Trim();
		}
		return started;
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
		return new MyBehavior.ShoutPromptContext
		{
			Extras = "",
			EntityPostprocessContext = "",
			PreprocessRuleIds = new List<string>(),
			UseDuelContext = false,
			UseRewardContext = false,
			IsLoanContext = false,
			IsQualified = true
		};
	}

	internal static string BuildNativeConversationPreprocessUnavailableText()
	{
		return "（API请求失败: 原生对话前处理超时或上一轮仍在运行，请稍后重试）";
	}

	public static bool IsNativeConversationPreprocessUnavailableTextForExternal(string text)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		return value.StartsWith("（API请求失败", StringComparison.Ordinal)
			&& value.IndexOf("原生对话前处理", StringComparison.Ordinal) >= 0;
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
		targetAgentIndex = -1;
		string pattern = Regex.Escape(SiegeSurrenderActionTag);
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
		{
			return false;
		}
		targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		bool validContext = TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out var settlement, out var side, out var sideLabel);
		if (!validContext)
		{
			return false;
		}
		content = Regex.Replace(content, pattern, "", RegexOptions.IgnoreCase).Trim();
		Logger.Log("SiegeSurrender", "Accepted native/direct conversation siege surrender tag. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " settlement=" + (settlement?.StringId ?? "") + " side=" + (sideLabel ?? side.ToString()) + " agentIndex=" + targetAgentIndex);
		return true;
	}

	internal static bool TryConsumeNativeConversationNpcSurrenderTag(Hero targetHero, CharacterObject targetCharacter, ref string content, out int targetAgentIndex)
	{
		targetAgentIndex = -1;
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, "\\[ACTION:NPC_SURRENDER\\]", RegexOptions.IgnoreCase))
		{
			return false;
		}
		targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		if (TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out var settlement, out var side, out var sideLabel))
		{
			content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
			Logger.Log("NpcSurrender", "Hidden native/direct conversation NPC surrender tag in siege context. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " settlement=" + (settlement?.StringId ?? "") + " side=" + (sideLabel ?? side.ToString()) + " agentIndex=" + targetAgentIndex);
			return false;
		}
		content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
		return true;
	}

	private void QueueNativeConversationNpcSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		string targetId = targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown";
		try
		{
			_mainThreadActions.Enqueue(delegate
			{
				LordEncounterBehavior.TryExecuteNpcSurrenderFromNativeConversation(targetHero, targetCharacter, targetAgentIndex, reason ?? "native_conversation_npc_surrender_tag");
			});
			Logger.Log("NpcSurrender", "Queued native/direct conversation NPC surrender. Target=" + targetId + " agentIndex=" + targetAgentIndex + " reason=" + (reason ?? "N/A"));
			TryDrainNativeConversationQueuedActions("npc_surrender_queued");
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Queue native/direct conversation NPC surrender failed. Target=" + targetId + " error=" + ex.Message);
		}
	}

	private void TryDrainNativeConversationQueuedActions(string reason)
	{
		try
		{
			int pending = _mainThreadActions.Count;
			if (pending <= 0)
			{
				return;
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] draining queued actions. Pending=" + pending + " missionActive=" + (Mission.Current != null) + " reason=" + (reason ?? "N/A"));
			if (!IsBannerlordMainThreadForNativeActions())
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] skip immediate drain from background thread. Pending=" + pending + " reason=" + (reason ?? "N/A") + " thread=" + Thread.CurrentThread.ManagedThreadId);
				return;
			}
			DrainMainThreadActionsForMissionTick();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] drain queued actions failed. Reason=" + (reason ?? "N/A") + " error=" + ex.Message);
		}
	}

	private void RecordGeneratedNpcAfefFactsForNativeConversation(Hero targetHero, CharacterObject targetCharacter, IEnumerable<string> factLines)
	{
		if (factLines == null)
		{
			return;
		}
		targetHero ??= targetCharacter?.HeroObject;
		targetCharacter ??= targetHero?.CharacterObject;
		string npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = "NPC";
		}
		int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
		targetNpc.AgentIndex = targetAgentIndex;
		string nativeKey = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, targetNpc);
		foreach (string rawFact in factLines)
		{
			string fact = NormalizeNativeConversationFactLineForPrompt(rawFact, "NPC");
			if (string.IsNullOrWhiteSpace(fact))
			{
				continue;
			}
			try
			{
				AppendNativeConversationSessionHistory(targetHero, targetCharacter, npcName, "系统", fact, "fact", targetAgentIndex: targetAgentIndex, npc: targetNpc);
				if (targetAgentIndex >= 0)
				{
					QueuePendingCurrentAfefFactForAgent(targetAgentIndex, fact);
				}
				if (!string.IsNullOrWhiteSpace(nativeKey))
				{
					QueuePendingCurrentNativeAfefFactForKey(nativeKey, fact);
				}
				Logger.Log("NativeConversationTrade", "recorded generated NPC AFEF fact target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName) + " fact=" + fact.Replace("\r", "\\r").Replace("\n", "\\n"));
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversationTrade", "[WARN] Failed to record generated NPC AFEF fact: " + ex.Message);
			}
		}
	}

	private static bool IsNativeConversationWorldMapContext()
	{
		try
		{
			if (Mission.Current != null)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	internal static bool TryProcessNativeConversationRawMeetingTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToBattle)
	{
		escalatedToBattle = false;
		if (!AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId, targetAgentIndex))
		{
			StripBlockedTownTauntTags(ref content);
			return false;
		}
		if (string.IsNullOrWhiteSpace(content) || !IsNativeConversationWorldMapContext())
		{
			return false;
		}
		try
		{
			if (!MeetingSceneShoutTauntTagRegex.IsMatch(content))
			{
				return false;
			}
			PartyBase nonHeroParty = null;
			if (targetHero == null)
			{
				TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, targetAgentIndex, out nonHeroParty);
			}
			if (targetHero == null && nonHeroParty == null)
			{
				return false;
			}
			bool result = LordEncounterBehavior.TryProcessMeetingTauntAction(targetHero, nonHeroParty, ref content, out escalatedToBattle);
			if (result)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] raw meeting taunt tag consumed before postprocess. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? nonHeroParty?.Name?.ToString() ?? "") + " escalated=" + escalatedToBattle);
			}
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] raw meeting taunt handling failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryProcessNativeConversationSceneTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToFight)
	{
		escalatedToFight = false;
		if (!AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId, targetAgentIndex))
		{
			StripBlockedTownTauntTags(ref content);
			return false;
		}
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		try
		{
			if (SceneTauntBehavior.HasSceneTauntFightTagForExternal(content))
			{
				if (SceneTauntBehavior.TryConsumeSceneTauntTagsForDelayedFightExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out var hadWarnTag, out var hadFightTag, out var targetKey))
				{
					bool scheduled = hadFightTag && CurrentInstance?.ScheduleNativeSceneTauntFightAfterDelay(targetHero, targetCharacter, targetAgentIndex, targetKey, "native_conversation_scene_taunt_fight_tag") == true;
					escalatedToFight = scheduled;
					Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt tag consumed for delayed fight. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex + " warn=" + hadWarnTag + " fight=" + hadFightTag + " scheduled=" + scheduled);
					return true;
				}
			}
			bool result = SceneTauntBehavior.TryProcessSceneTauntAction(targetHero, targetCharacter, targetAgentIndex, ref content, out escalatedToFight);
			if (result)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt tag consumed. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex + " escalated=" + escalatedToFight);
				if (escalatedToFight)
				{
					CloseNativeConversationForSceneMechanism("native_scene_taunt_fight");
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt handling failed: " + ex.Message);
			return false;
		}
	}

	private static void StripBlockedTownTauntTags(ref string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return;
		}

		content = MeetingSceneShoutTauntTagRegex.Replace(content, "");
		content = Regex.Replace(content, "\\[ACTION:SCENE_TAUNT_(?:WARN|FIGHT)\\]", "", RegexOptions.IgnoreCase).Trim();
	}

	private List<string> BuildPreprocessExcludedRuleIdsForCurrentInteraction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null, string currentPlayerText = null)
	{
		List<string> allRuleIds = AIConfigHandler.GetConfiguredEnabledGuardrailRuleIdsForExternal();
		if (AfGcczShoutBridge.ShouldUseExclusivePreprocessRuleRouting(targetAgentIndex))
		{
			return AfGcczShoutBridge.BuildRuntimePreprocessRuleExclusions(allRuleIds, targetAgentIndex);
		}
		if (allRuleIds == null || allRuleIds.Count == 0)
		{
			return new List<string>();
		}
		HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
		string targetKingdomId = TryGetKingdomIdOverrideFromAgent(agent);
		string targetHeroId = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
		string targetCharacterId = (targetCharacter?.StringId ?? "").Trim();
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		try
		{
			PromptRuntimeTargetBinding runtimeTargetBinding = MyBehavior.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);
			AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));
			foreach (string ruleId in allRuleIds)
			{
				string id = (ruleId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}
				if (!CanInjectRuleTopicIntoPreprocessForCurrentInteraction(id, targetHero, targetCharacter, targetAgentIndex, hasAnyHero, sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates))
				{
					excluded.Add(id);
				}
			}
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
		return excluded.ToList();
	}

	private bool CanInjectRuleTopicIntoPreprocessForCurrentInteraction(string ruleId, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null)
	{
		string id = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (!AIConfigHandler.IsGuardrailRuleAvailableToPreprocessForExternal(id, hasAnyHero))
		{
			return false;
		}
		if (NoblePrisonerEscortBehavior.IsEscortedAgent(targetAgentIndex))
		{
			return true;
		}
		if (AIConfigHandler.IsPlayerPartyTradeLimitedTarget(hero) && (id == "loan" || id == "kingdom_agenda" || id == "diplomacy" || id == "party_transfer"))
		{
			return false;
		}
		if (id == AutoGroupRelayRuleId)
		{
			return false;
		}
		switch (id)
		{
		case "scene_mechanism_actions":
			return CanInjectSceneMechanismTopicIntoPreprocess(sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates);
		case "worldmap_party_command":
			return !ShouldExcludeWorldMapCommandTopicForPreprocess(hero, targetCharacter, targetAgentIndex);
		case "party_transfer":
			return (MyBehavior.BuildPartyTransferPromptEntriesForExternal(hero, targetCharacter, targetAgentIndex)?.Count ?? 0) > 0;
		case "encounter_release_player":
			return !string.IsNullOrWhiteSpace(LordEncounterBehavior.BuildMeetingPlayerReleaseRuntimeInstructionForExternal(hero));
		case "meeting_taunt":
			PartyBase meetingTauntParty = null;
			if (hero == null)
			{
				TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, targetAgentIndex, out meetingTauntParty);
			}
			return !string.IsNullOrWhiteSpace(LordEncounterBehavior.BuildMeetingTauntRuntimeInstructionForExternal(hero, targetCharacter, meetingTauntParty))
				|| !string.IsNullOrWhiteSpace(SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(hero, targetCharacter, targetAgentIndex));
		default:
			return true;
		}
	}

	private bool CanInjectSceneMechanismTopicIntoPreprocess(List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, NpcDataPacket sceneSpeakerNpc, List<NpcDataPacket> sceneCandidates)
	{
		try
		{
			if (Mission.Current == null)
			{
				return false;
			}
			string sceneSummonClosureInstruction = "";
			if (sceneCandidates != null && sceneCandidates.Count > 0)
			{
				sceneSummonClosureInstruction = _sceneMovement.BuildSceneSummonClosurePromptInstruction(sceneCandidates);
			}
			else if (sceneSpeakerNpc != null)
			{
				sceneSummonClosureInstruction = _sceneMovement.BuildSceneSummonClosurePromptInstruction(new List<NpcDataPacket> { sceneSpeakerNpc });
			}
			string sceneFollowControlInstruction = _sceneMovement.BuildSceneFollowControlPromptInstruction(sceneSpeakerNpc);
			string section = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction, sceneSpeakerNpc);
			return !string.IsNullOrWhiteSpace(section);
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldExcludeWorldMapCommandTopicForPreprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return hero.IsNotable
					|| hero.Occupation == Occupation.Headman
					|| hero.Occupation == Occupation.RuralNotable
					|| hero.Occupation == Occupation.GangLeader
					|| hero.Occupation == Occupation.Merchant
					|| hero.Occupation == Occupation.Artisan
					|| hero.Occupation == Occupation.Preacher;
			}
			if (targetCharacter != null && !targetCharacter.IsHero)
			{
				return !WorldMapPartyCommandBehavior.CanUseNonHeroPartyFallbackForExternal(targetCharacter, targetAgentIndex);
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private Task<string> SubmitNativeConversationTextInternalAsync(NativeConversationAdmission admission, string playerText, Action<string> onStreamText = null, string currentDialogTextOverride = null, Action<string> onPostprocessStarted = null, Action<string, Hero, CharacterObject> onMainReplyReady = null, bool npcInitiatedOpening = false)
	{
		return NativeConversationTurnCoordinator.RunAsync(new NativeConversationTurnRuntime(CreateNativeConversationTurnPorts(), admission,
			playerText, onStreamText, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening));
	}

	internal static void SubmitNativeConversationSceneActionObservation(string replyText, int agentIndex)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(replyText) || agentIndex < 0 ||
				!SceneActionsRuntimeHost.IsInitialized ||
				SceneActionsRuntimeHost.Settings?.NpcSceneShoutReplyEnabled != true ||
				Mission.Current == null)
			{
				return;
			}
			Mission mission = Mission.Current;
			Agent speaker = mission.Agents?.FirstOrDefault(agent => agent != null && agent.Index == agentIndex);
			if (speaker == null)
			{
				return;
			}
			bool submitted = SceneActionsRuntimeHost.SubmitNpcReply(
				Guid.NewGuid(),
				mission,
				speaker,
				replyText,
				mission.CurrentTime);
			if (submitted)
			{
				SceneActionsLog.Info(
					"NATIVE_CONVERSATION_ACTION",
					"Submitted native AI conversation reply to the natural-action parser. Agent=" + agentIndex);
			}
		}
		catch (Exception ex)
		{
			// A parser failure must not discard an otherwise valid native conversation.
			SceneActionsLog.Warning(
				"NATIVE_CONVERSATION_ACTION",
				"Natural-action observation failed open: " + ex.Message);
		}
	}

	internal static bool IsNativeConversationNoSpeechPlaceholder(string text)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		return string.Equals(value, "（没说话）", StringComparison.Ordinal)
			|| string.Equals(value, "(没说话)", StringComparison.Ordinal)
			|| string.Equals(value, "无", StringComparison.Ordinal)
			|| string.Equals(value, "无回信", StringComparison.Ordinal);
	}

	private const int NativeConversationPersonaGenerationWaitTimeoutMs = 180000;

	private const int NativeConversationMainReplyTimeoutMs = 180000;

	internal static async Task<string> CallNativeConversationApiAsync(List<object> messages, Action<string> onStreamText, CancellationToken cancellationToken = default(CancellationToken))
	{
		Stopwatch nativeApiWatchSw = Stopwatch.StartNew();
		using CancellationTokenSource requestTimeout = LlmNonStreamingTransport.CreateTimeout(NativeConversationMainReplyTimeoutMs, cancellationToken);
		if (onStreamText == null)
		{
			FreezeWatchdog.Mark("NativeConversation.api_non_stream_start", "messages=" + (messages?.Count ?? 0) + " timeoutMs=" + NativeConversationMainReplyTimeoutMs, immediate: true);
			string result;
			try
			{
				result = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: false, cancellationToken: requestTimeout.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (requestTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
			}
			cancellationToken.ThrowIfCancellationRequested();
			if (requestTimeout.IsCancellationRequested)
				return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
			FreezeWatchdog.Mark("NativeConversation.api_non_stream_done", "resultLen=" + ((result ?? "").Length) + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return result;
		}
		FreezeWatchdog.Mark("NativeConversation.api_stream_start", "messages=" + (messages?.Count ?? 0) + " timeoutMs=" + NativeConversationMainReplyTimeoutMs, immediate: true);
		StringBuilder streamed = new StringBuilder();
		LlmVisibleReplyNormalizer.StreamFilter visibleReplyFilter = new LlmVisibleReplyNormalizer.StreamFilter();
		string completed = "";
		string error = "";
		CancellationTokenSource timeoutCts = requestTimeout;
		await LegacyShoutNetworkGateway.SendLegacyMessagesStreamAsync(messages, 5000, delegate(string delta)
		{
			if (timeoutCts.IsCancellationRequested || string.IsNullOrEmpty(delta))
			{
				return;
			}
			string visibleDelta = visibleReplyFilter.Push(delta);
			if (string.IsNullOrEmpty(visibleDelta))
			{
				return;
			}
			streamed.Append(visibleDelta);
			string visible = BuildNativeConversationStreamingVisibleText(streamed.ToString());
			if (!string.IsNullOrWhiteSpace(visible))
			{
				onStreamText(visible);
			}
		}, delegate(string full)
		{
			if (timeoutCts.IsCancellationRequested) return;
			string finalDelta = visibleReplyFilter.Complete(full ?? "");
			if (!string.IsNullOrEmpty(finalDelta))
			{
				streamed.Append(finalDelta);
			}
			completed = (visibleReplyFilter.NormalizedText ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(completed))
			{
				onStreamText(BuildNativeConversationStreamingVisibleText(completed));
			}
		}, delegate(string err)
		{
			error = (err ?? "").Trim();
		}, timeoutCts.Token, promptRetryOnError: false).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		if (timeoutCts.IsCancellationRequested && string.IsNullOrWhiteSpace(completed) && streamed.Length == 0 && string.IsNullOrWhiteSpace(error))
		{
			Logger.Log("NativeConversation", "[WARN] main reply timed out before first stream chunk. timeoutMs=" + NativeConversationMainReplyTimeoutMs);
			FreezeWatchdog.Mark("NativeConversation.api_stream_timeout", "elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return "（API请求失败: 原生对话正文生成超时 " + NativeConversationMainReplyTimeoutMs + "ms）";
		}
		if (!string.IsNullOrWhiteSpace(completed))
		{
			FreezeWatchdog.Mark("NativeConversation.api_stream_done", "completedLen=" + completed.Length + " streamedLen=" + streamed.Length + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return completed;
		}
		string fallback = streamed.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(fallback))
		{
			FreezeWatchdog.Mark("NativeConversation.api_stream_fallback", "streamedLen=" + fallback.Length + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return fallback;
		}
		FreezeWatchdog.Mark("NativeConversation.api_stream_error", "errorLen=" + ((error ?? "").Length) + " elapsedMs=" + Math.Round(nativeApiWatchSw.Elapsed.TotalMilliseconds, 2), immediate: true);
		return error ?? "";
	}

	private static string BuildNativeConversationStreamingVisibleText(string text)
	{
		string visible = LlmVisibleReplyNormalizer.NormalizeStreamingPreview(text);
		visible = (visible ?? "").Replace("\r", "").TrimStart();
		if (string.IsNullOrWhiteSpace(visible))
		{
			return "...";
		}
		visible = StripNpcNamePrefixSafely(visible, 30);
		visible = StripLeakedPromptContentForShout(visible);
		visible = StripStageDirectionsForPassiveShout(visible);
		visible = SanitizeSceneSpeechText(visible);
		return string.IsNullOrWhiteSpace(visible) ? "..." : visible.Trim();
	}

	public void TriggerShout()
	{
		if (!TryPrepareShoutTarget(out var primaryDataPacket))
		{
			return;
		}
		string text = primaryDataPacket?.Name ?? "附近的人";
		bool battleSpeechMenu = BattleSpeechRuntimeHost.CanOpenSpeechMenu(Mission.Current);
		List<Agent> battleSpeechFramedTargets = null;
		Agent battleSpeechPrimaryTarget = null;
		if (battleSpeechMenu)
		{
			battleSpeechFramedTargets = GetAgentsForShoutTargetingContext(_activeShoutTargetingContext) ?? new List<Agent>();
			battleSpeechPrimaryTarget = ResolvePrimaryAgentForShoutTargetingContext(
				_activeShoutTargetingContext,
				battleSpeechFramedTargets);
		}
		bool npcSpeechTargetAllowed = battleSpeechMenu &&
			IsPlayerSideBattleSpeechTarget(
				Mission.Current,
				Agent.Main,
				battleSpeechPrimaryTarget);
		List<InquiryElement> inquiryElements = new List<InquiryElement>
		{
			new InquiryElement("normal", "交流", null, isEnabled: true, ""),
			new InquiryElement("give", "给予其物品并交流", null, isEnabled: true, ""),
			new InquiryElement("show", "向其展示物品并交流", null, isEnabled: true, ""),
			new InquiryElement("give_troops", "给予部队并交流", null, isEnabled: true, ""),
			new InquiryElement("give_prisoners", "给予俘虏并交流", null, isEnabled: true, ""),
			new InquiryElement("give_settlements", "转移固定资产并交流", null, isEnabled: true, "")
		};
		if (MyBehavior.IsDevDataManagementEnabledForExternal())
		{
			inquiryElements.Add(new InquiryElement("tag_test", "标签测试", null, isEnabled: true, "输入 NPC 可见正文和后处理标签，不调用 API，按当前目标立即执行。"));
		}
		if (battleSpeechMenu)
		{
			inquiryElements.Add(new InquiryElement(
				"battle_speech_player",
				"演讲",
				null,
				isEnabled: Agent.Main != null && Agent.Main.IsActive(),
				"你亲自面向己方士兵发表演讲。"));
			if (npcSpeechTargetAllowed)
			{
				inquiryElements.Add(new InquiryElement(
					"battle_speech_npc",
					"他人演讲",
					null,
					isEnabled: true,
					"让当前框选的己方主目标发表演讲。"));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(text, "当前目标：" + text + "\n此菜单用于边交流边给予或展示物品，也可转移部队、俘虏或固定资产。\n请选择交流方式：", inquiryElements, isExitShown: true, 1, 1, "确定", "取消", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				ResumeGame();
			}
			else
			{
				string text2 = (selected[0]?.Identifier ?? "").ToString();
				if (text2 == "battle_speech_player" || text2 == "battle_speech_npc")
				{
					OpenBattleSpeechFromShoutMenu(
						text2 == "battle_speech_npc",
						battleSpeechPrimaryTarget,
						battleSpeechFramedTargets,
						_sceneConversationEpoch);
				}
				else if (text2 == "normal" && TryOpenPresentationSessionFromWheel())
				{
					// The persistent session panel owns input; the one-shot popup is not opened.
				}
				// With a session style the give/show choices open the session's own give panel;
				// the old popup chain (resource list → amounts → one-shot input) is only the fallback.
				else if (text2 == "give")
				{
					if (!TryOpenPresentationTradeFromWheel("give")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Give);
				}
				else if (text2 == "show")
				{
					if (!TryOpenPresentationTradeFromWheel("show")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Show);
				}
				else if (text2 == "give_troops")
				{
					if (!TryOpenPresentationTradeFromWheel("give_troops")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GiveTroops);
				}
				else if (text2 == "give_prisoners")
				{
					if (!TryOpenPresentationTradeFromWheel("give_prisoners")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GivePrisoners);
				}
				else if (text2 == "give_settlements")
				{
					if (!TryOpenPresentationTradeFromWheel("give_settlements")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GiveSettlements);
				}
				else if (text2 == "tag_test")
				{
					OpenShoutTagTestInput(primaryDataPacket);
				}
				else
				{
					OpenShoutTextInput(primaryDataPacket, null, null);
				}
			}
		}, delegate
		{
			OnShoutCancelled();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenBattleSpeechFromShoutMenu(
		bool npcSpeech,
		Agent primaryTarget,
		IReadOnlyList<Agent> framedTargets,
		int conversationEpoch)
	{
		// TryPrepareShoutTarget paused AF and acquired ordinary scene-shout
		// movement control. The dedicated speech channel owns the following UI,
		// so release only that control before handing the frozen agent snapshot
		// to BattleSpeechRuntimeHost.
		DeactivateMultiSceneMovementSuppression();
		EndShoutProcessing("battle_speech_menu_takeover");
		Agent player = Agent.Main;
		AfCompatV130.BeginDedicatedSpeechMenuInput();
		bool opened = BattleSpeechRuntimeHost.TryOpenSpeechInputFromShoutMenu(
			Mission.Current,
			npcSpeech,
			player,
			primaryTarget,
			framedTargets ?? Array.Empty<Agent>(),
			conversationEpoch);
		if (!opened)
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"演讲输入框无法打开，已返回普通场景状态。",
				new Color(1f, 0.5f, 0.3f)));
			AfCompatV130.CompleteDedicatedSpeechMenuInput();
		}
	}

	private static bool IsPlayerSideBattleSpeechTarget(
		Mission mission,
		Agent player,
		Agent primaryTarget)
	{
		if (mission?.PlayerTeam == null ||
			player == null ||
			primaryTarget == null ||
			!primaryTarget.IsActive() ||
			!primaryTarget.IsHuman ||
			ReferenceEquals(primaryTarget, player) ||
			!ReferenceEquals(primaryTarget.Mission, mission) ||
			primaryTarget.Team == null ||
			!primaryTarget.Team.IsValid ||
			primaryTarget.Team.Side != mission.PlayerTeam.Side)
		{
			return false;
		}
		return true;
	}

	private void TriggerShoutDirectInput()
	{
		if (!TryPrepareShoutTarget(out var primaryDataPacket))
		{
			return;
		}
		OpenShoutTextInput(primaryDataPacket, null, null);
	}

	private bool TryPrepareShoutTarget(out NpcDataPacket primaryDataPacket)
	{
		primaryDataPacket = null;
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			EndShoutProcessing("setup_not_ready");
			return false;
		}
		// The presentation wheel and session run unpaused; the one-shot flows keep pausing.
		if (!IsScenePresentationSessionEnabled())
		{
			PauseGame();
		}
		ShoutTargetingContext targetingContext = _activeShoutTargetingContext;
		List<Agent> nearbyNPCAgents = GetAgentsForShoutTargetingContext(targetingContext);
		if (nearbyNPCAgents == null || nearbyNPCAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f)));
			ResumeGame();
			return false;
		}
		List<NpcDataPacket> source = (from a in nearbyNPCAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		Agent primaryTarget = ResolvePrimaryAgentForShoutTargetingContext(targetingContext, nearbyNPCAgents);
		if (primaryTarget == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 异色主对象已经离场，请重新框选。", new Color(1f, 0.5f, 0.3f)));
			ResumeGame();
			return false;
		}
		primaryDataPacket = source.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index);
		if (primaryDataPacket == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 没有找到可交流的目标。", new Color(1f, 0.5f, 0.3f)));
			ResumeGame();
			return false;
		}
		ActivateMultiSceneMovementSuppression(new int[1] { primaryDataPacket.AgentIndex });
		return true;
	}

	private void OpenShoutTextInput(NpcDataPacket primaryDataPacket, string preface, string extraFact)
	{
		string text = primaryDataPacket?.Name ?? "附近的人";
		string titleText = text;
		string text2 = string.IsNullOrWhiteSpace(preface) ? "" : preface;
		PauseGame();
		if (!ShoutTextInputPopup.Show(titleText, text2, "请输入你想说的话：", "", delegate(string input)
		{
			OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
        }, OnShoutCancelled, BuildShoutTargetEncyclopediaAction(primaryDataPacket), enableIllustration: true))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, (string.IsNullOrWhiteSpace(text2) ? "" : (text2 + "\n")) + "请输入你想说的话：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "发送", "取消", delegate(string input)
			{
				OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
			}, OnShoutCancelled), pauseGameActiveState: true);
		}
	}

	private void OpenShoutTagTestInput(NpcDataPacket primaryDataPacket)
	{
		if (!MyBehavior.IsDevDataManagementEnabledForExternal())
		{
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 请先在 MCM 开启数据管理。", new Color(1f, 0.45f, 0.25f)));
			OnShoutCancelled();
			return;
		}
		string text = primaryDataPacket?.Name ?? "附近的人";
		string titleText = "标签输入 - " + text;
		string subtitle = "当前目标：" + text + "\n输入 NPC 可见正文和后处理标签。不会调用 API，标签会按当前目标立即执行。";
		PauseGame();
		if (!ShoutTextInputPopup.Show(titleText, subtitle, "输入正文和标签：", "", delegate(string input)
		{
			OnShoutTagTestConfirmed(input, primaryDataPacket?.AgentIndex);
		}, OnShoutCancelled, BuildShoutTargetEncyclopediaAction(primaryDataPacket)))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, subtitle + "\n\n输入正文和标签：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "执行", "取消", delegate(string input)
			{
				OnShoutTagTestConfirmed(input, primaryDataPacket?.AgentIndex);
			}, OnShoutCancelled), pauseGameActiveState: true);
		}
	}

	private void OnShoutTagTestConfirmed(string input, int? forcedPrimaryAgentIndex)
	{
		string content = (input ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			OnShoutCancelled();
			return;
		}
		try
		{
			if (!MyBehavior.IsDevDataManagementEnabledForExternal())
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 请先在 MCM 开启数据管理。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			ShoutTargetingContext targetingContext = _activeShoutTargetingContext;
			List<Agent> nearbyAgents = GetAgentsForShoutTargetingContext(targetingContext) ?? new List<Agent>();
			if (forcedPrimaryAgentIndex.HasValue && !nearbyAgents.Any((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value))
			{
				Agent forcedAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value && a.IsActive());
				if (forcedAgent != null)
				{
					nearbyAgents.Insert(0, forcedAgent);
				}
			}
			nearbyAgents = nearbyAgents.Where((Agent a) => a != null && a.IsActive()).ToList();
			if (nearbyAgents.Count == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 没有找到当前场景目标。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			List<NpcDataPacket> allNpcData = nearbyAgents.Select((Agent a) => ShoutUtils.ExtractNpcData(a)).Where((NpcDataPacket d) => d != null).ToList();
			ApplySceneLocalDisambiguatedNames(allNpcData);
			Agent primaryTarget = null;
			if (forcedPrimaryAgentIndex.HasValue)
			{
				primaryTarget = nearbyAgents.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value);
			}
			primaryTarget ??= ResolvePrimaryAgentForShoutTargetingContext(targetingContext, nearbyAgents) ?? nearbyAgents.FirstOrDefault();
			NpcDataPacket primaryNpc = (primaryTarget != null) ? allNpcData.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index) : allNpcData.FirstOrDefault();
			if (primaryNpc == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 没有找到可执行标签的 NPC。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
			foreach (Agent agent in nearbyAgents)
			{
				if (agent?.Character is CharacterObject { HeroObject: not null } character)
				{
					resolvedHeroes[agent.Index] = character.HeroObject;
				}
			}
			List<SceneSummonPromptTarget> sceneSummonTargets = _sceneMovement.BuildSceneSummonPromptTargets(allNpcData, resolvedHeroes);
			int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
			List<SceneGuidePromptTarget> sceneGuideTargets = _sceneMovement.BuildSceneGuidePromptTargets(primaryTarget, sceneGuideFirstPromptId);
			Logger.Log("SceneTagTest", "submit target=" + (primaryNpc?.Name ?? "") + " agentIndex=" + primaryNpc.AgentIndex + " tagCount=" + CountDeveloperTagTestTags(content) + " raw=" + content.Replace("\n", "\\n"));
			EnqueueSpeechLineWithOptions(primaryNpc, content, allNpcData, commitHistory: true, suppressStare: false, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, sceneSummonTargets, sceneGuideTargets, null);
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 已提交给 " + (primaryNpc.Name ?? "NPC") + "，标签数：" + CountDeveloperTagTestTags(content), new Color(0.4f, 1f, 0.4f)));
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTagTest", "[ERROR] submit failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 执行失败：" + ex.Message, new Color(1f, 0.35f, 0.25f)));
		}
		finally
		{
			ResumeGame();
		}
	}

	private Action BuildShoutTargetEncyclopediaAction(NpcDataPacket targetNpc)
	{
		Hero hero = null;
		try
		{
			if (targetNpc != null && targetNpc.IsHero)
			{
				hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			}
		}
		catch
		{
			hero = null;
		}
		if (hero == null)
		{
			return null;
		}
		return delegate
		{
			OpenHeroEncyclopediaFromShoutInput(hero);
		};
	}

	private static void OpenHeroEncyclopediaFromShoutInput(Hero hero)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			string link = hero.EncyclopediaLink;
			if (!string.IsNullOrWhiteSpace(link))
			{
				Campaign.Current?.EncyclopediaManager?.GoToLink(link);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] Failed to open shout target encyclopedia: " + ex.Message);
		}
	}

	private static bool IsShoutTradeShowMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.Show;
	}

	private static bool IsShoutPartyTransferMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners;
	}

	private static bool IsShoutSettlementTransferMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveSettlements;
	}

	private static bool IsShoutTroopTransferMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveTroops;
	}

	private static bool IsShoutPrisonerTransferMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GivePrisoners;
	}

	private static bool IsShoutTradeGiveMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.Give || mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners || mode == ShoutChatMode.GiveSettlements;
	}

	private void ResolveShoutTradeRuntimeTarget(out Hero hero, out CharacterObject characterObject, out Agent agent)
	{
		hero = null;
		characterObject = null;
		agent = null;
		try
		{
			agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1)));
			characterObject = agent?.Character as CharacterObject;
		}
		catch
		{
			agent = null;
			characterObject = null;
		}
		if (_shoutTradeTargetHeroOverride != null)
		{
			hero = _shoutTradeTargetHeroOverride;
		}
		else if (_shoutTradeTargetNpc != null && _shoutTradeTargetNpc.IsHero)
		{
			hero = ResolveHeroFromAgentIndex(_shoutTradeTargetNpc.AgentIndex) ?? characterObject?.HeroObject;
		}
		// Prefer the current scene Agent character; native non-Hero conversations can expose a proxy CharacterObject instead.
		if (_shoutTradeTargetCharacterOverride != null && characterObject == null)
		{
			characterObject = _shoutTradeTargetCharacterOverride;
		}
		if (hero == null)
		{
			hero = characterObject?.HeroObject;
		}
		if (characterObject == null)
		{
			characterObject = hero?.CharacterObject;
		}
	}

	private bool IsNativeTradeTargetValidForCommit()
	{
		try
		{
			ConversationManager manager = Campaign.Current?.ConversationManager;
			if (_shoutTradeNativeManager == null || !ReferenceEquals(manager, _shoutTradeNativeManager)
				|| !manager.IsConversationInProgress || !ReferenceEquals(Mission.Current, _shoutTradeNativeMission))
				return false;
			// Use the actual conversation participant, never the encountered army leader as fallback.
			CharacterObject character = manager.OneToOneConversationCharacter;
			Hero hero = character?.HeroObject;
			if (_shoutTradeTargetHeroOverride != null)
			{
				if (!ReferenceEquals(hero, _shoutTradeTargetHeroOverride) || !hero.IsAlive) return false;
			}
			else if (character == null || !ReferenceEquals(character, _shoutTradeTargetCharacterOverride))
				return false;

			Agent agent = manager.OneToOneConversationAgent as Agent;
			if (_shoutTradeNativeAgent != null)
			{
				// Native conversation proxies need not satisfy scene-shout speech/health rules.
				return ReferenceEquals(agent, _shoutTradeNativeAgent) && agent.IsActive() && !agent.IsMainAgent
					&& (_shoutTradeTargetHeroOverride == null
						|| (agent.Character as CharacterObject)?.HeroObject == _shoutTradeTargetHeroOverride);
			}
			// A genuinely agentless map conversation is valid; a missing scene participant is not.
			return agent == null && _shoutTradeNativeMission == null && IsNativeConversationWorldMapContext();
		}
		catch { return false; }
	}

	private bool IsShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false)
	{
		if (_shoutTradeActionOnly && !requireCurrentShoutFrame)
			return IsNativeTradeTargetValidForCommit();
		NpcDataPacket expectedTarget = _shoutTradeTargetNpc;
		if (expectedTarget == null || expectedTarget.AgentIndex < 0)
		{
			return false;
		}
		try
		{
			Agent liveAgent = Mission.Current?.Agents?.FirstOrDefault(
				agent => agent != null && agent.Index == expectedTarget.AgentIndex);
			if (!CanAgentParticipateInSceneSpeech(liveAgent)
				|| liveAgent.Character is not CharacterObject liveCharacter)
			{
				return false;
			}
			// Agent indexes are scene-local; reject a recycled index when this UI flow already captured the original Agent.
			bool hasCapturedLiveAgent = _shoutTradeTargetAgentSnapshot != null;
			if (hasCapturedLiveAgent && !ReferenceEquals(liveAgent, _shoutTradeTargetAgentSnapshot))
			{
				return false;
			}
			if (requireCurrentShoutFrame
				&& !GetAgentsForShoutTargetingContext(_activeShoutTargetingContext)
					.Any(agent => agent != null && agent.Index == expectedTarget.AgentIndex))
			{
				return false;
			}
			if (_shoutTradeTargetHeroOverride != null
				&& liveCharacter.HeroObject != _shoutTradeTargetHeroOverride)
			{
				return false;
			}
			// A captured Agent is authoritative for non-Hero native dialogue; its proxy CharacterObject may legitimately differ.
			if (!hasCapturedLiveAgent
				&& _shoutTradeTargetCharacterOverride != null
				&& !ReferenceEquals(liveCharacter, _shoutTradeTargetCharacterOverride)
				&& !string.Equals(
					liveCharacter.StringId ?? "",
					_shoutTradeTargetCharacterOverride.StringId ?? "",
					StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (expectedTarget.IsHero)
			{
				return liveCharacter.HeroObject != null;
			}
			if (liveCharacter.HeroObject != null)
			{
				return false;
			}
			// ExtractNpcData appends the location SpecialTargetTag ("troop_id tag"); only the first token is the StringId.
			if (!hasCapturedLiveAgent
				&& !string.IsNullOrWhiteSpace(expectedTarget.TroopId)
				&& !string.Equals(
					expectedTarget.TroopId.Trim().Split(' ')[0],
					liveCharacter.StringId ?? "",
					StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (!hasCapturedLiveAgent && !string.IsNullOrWhiteSpace(expectedTarget.UnnamedKey))
			{
				NpcDataPacket liveTarget = ShoutUtils.ExtractNpcData(liveAgent);
				if (!string.Equals(
					expectedTarget.UnnamedKey,
					liveTarget?.UnnamedKey ?? "",
					StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool EnsureShoutTradePrimaryTargetValidForCommit(bool requireCurrentShoutFrame = false)
	{
		if (IsShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame))
		{
			return true;
		}
		try
		{
			Logger.Log("ShoutBehavior", "[ShoutTrade] commit cancelled because primary target is no longer valid agent="
				+ (_shoutTradeTargetNpc?.AgentIndex ?? (-1))
				+ " native=" + _shoutTradeActionOnly
				+ " sameManager=" + ReferenceEquals(_shoutTradeNativeManager, Campaign.Current?.ConversationManager)
				+ " sameMission=" + ReferenceEquals(_shoutTradeNativeMission, Mission.Current)
				+ " inConversation=" + (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
				+ " sameAgent=" + ReferenceEquals(_shoutTradeNativeAgent, Campaign.Current?.ConversationManager?.OneToOneConversationAgent)
				+ " expected=" + (_shoutTradeTargetHeroOverride?.StringId ?? _shoutTradeTargetCharacterOverride?.StringId ?? "")
				+ " current=" + (Campaign.Current?.ConversationManager?.OneToOneConversationCharacter?.StringId ?? ""));
			InformationManager.DisplayMessage(new InformationMessage(
				"交易目标已经离场或失效，本次给予/展示没有执行。",
				new Color(1f, 0.45f, 0.25f)));
		}
		catch
		{
		}
		return false;
	}

	private int GetShoutTradeTargetAgentIndex()
	{
		return _shoutTradeTargetNpc?.AgentIndex ?? (-1);
	}

	private void BeginShoutTradeFlow(NpcDataPacket targetNpc, ShoutChatMode mode)
	{
		_shoutTradeTargetNpc = targetNpc;
		_shoutTradeMode = mode;
		try
		{
			// Capture once per UI flow so later validation does not mistake a non-Hero conversation proxy for another NPC.
			_shoutTradeTargetAgentSnapshot = Mission.Current?.Agents?.FirstOrDefault(
				agent => agent != null && agent.Index == (targetNpc?.AgentIndex ?? (-1)));
		}
		catch
		{
			_shoutTradeTargetAgentSnapshot = null;
		}
		// Shared with the scene-session give panel (ShoutBehavior.ScenePresentationTrade.cs).
		string ineligible = GetShoutTradeTargetIneligibility(mode);
		if (ineligible != null)
		{
			InformationManager.DisplayMessage(new InformationMessage(ineligible));
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
			return;
		}
		_shoutTradeOptions = BuildShoutTradeOptions();
		_shoutPendingTradeItems.Clear();
		_shoutPendingTradeItemIndex = 0;
		if (_shoutTradeOptions == null || _shoutTradeOptions.Count == 0)
		{
			string information = GetNoShoutTradeOptionsMessage(mode);
			InformationManager.DisplayMessage(new InformationMessage(information));
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < _shoutTradeOptions.Count; i++)
		{
			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];
			string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutTradeResourceOption.Name);
			string text2 = $"{transferDisplayName} (×{shoutTradeResourceOption.AvailableAmount})";
			string text3 = $"可用数量: {shoutTradeResourceOption.AvailableAmount}";
			if (shoutTradeResourceOption.PartyEntry != null)
			{
				if (IsShoutTroopTransferMode(_shoutTradeMode))
				{
					text3 = $"可用数量: {shoutTradeResourceOption.AvailableAmount} | 日薪: {shoutTradeResourceOption.PartyEntry.WageDenarsPerDay}第纳尔/天 | 雇佣价: {shoutTradeResourceOption.PartyEntry.HirePriceDenarsPerUnit}第纳尔/人";
				}
				else if (IsShoutPrisonerTransferMode(_shoutTradeMode))
				{
					text3 = $"可用数量: {shoutTradeResourceOption.AvailableAmount} | 购买价: {shoutTradeResourceOption.PartyEntry.BuyPriceDenarsPerUnit}第纳尔/人";
					string sourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(shoutTradeResourceOption.PartyEntry);
					if (!string.IsNullOrWhiteSpace(sourceLabel))
					{
						text2 += "（来源：" + sourceLabel + "）";
						text3 += " | 来源: " + sourceLabel;
					}
				}
			}
			else if (shoutTradeResourceOption.SettlementEntry != null)
			{
				text3 = $"每日收益: {Math.Max(0, shoutTradeResourceOption.SettlementEntry.DailyIncomeDenars)} 第纳尔 | 一次结清指导价: {Math.Max(0, shoutTradeResourceOption.SettlementEntry.GuidePriceDenars)} 第纳尔 | 类型: {(string.IsNullOrWhiteSpace(shoutTradeResourceOption.SettlementEntry.TypeLabel) ? (shoutTradeResourceOption.SettlementEntry.Settlement?.IsTown ?? false ? "城市" : "城堡") : shoutTradeResourceOption.SettlementEntry.TypeLabel)}";
			}
			list.Add(new InquiryElement(i, text2, null, isEnabled: true, text3));
		}
		string text = targetNpc?.Name ?? "附近的人";
		string suffix = _shoutTradeActionOnly ? "" : "并交流";
		string titleText = ((mode == ShoutChatMode.Give) ? ("给予其物品" + suffix + " - " + text) : ((mode == ShoutChatMode.Show) ? ("向其展示物品" + suffix + " - " + text) : ((mode == ShoutChatMode.GiveTroops) ? ("给予部队" + suffix + " - " + text) : ((mode == ShoutChatMode.GivePrisoners) ? ("给予俘虏" + suffix + " - " + text) : ("转移固定资产" + suffix + " - " + text)))));
		string actionOnlyNote = _shoutTradeActionOnly ? "\n该动作只记录到对话历史，不触发 AI 回复。" : "";
		string descriptionText = (IsShoutPartyTransferMode(mode) ? ((mode == ShoutChatMode.GiveTroops) ? ("当前目标：" + text + "\n选择要转入对方麾下的部队（可多选）：" + actionOnlyNote) : ("当前目标：" + text + "\n选择要交给对方的俘虏（可多选）：" + actionOnlyNote)) : (IsShoutSettlementTransferMode(mode) ? ("当前目标：" + text + "\n选择要转给对方的固定资产（可多选）：" + actionOnlyNote) : ("当前目标：" + text + "\n选择要使用的物品或第纳尔（可多选）：" + actionOnlyNote)));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 1, list.Count, "确定", "取消", OnShoutTradeResourcesSelected, delegate
		{
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private List<ShoutTradeResourceOption> BuildShoutTradeOptions()
	{
		List<ShoutTradeResourceOption> list = new List<ShoutTradeResourceOption>();
		string text = "";
		ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
		if (IsShoutPartyTransferMode(_shoutTradeMode))
		{
			List<MyBehavior.PartyTransferPromptEntry> list2 = MyBehavior.BuildPartyTransferPromptEntriesForExternal(hero, characterObject, GetShoutTradeTargetAgentIndex());
			IEnumerable<MyBehavior.PartyTransferPromptEntry> enumerable = list2.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == (IsShoutTroopTransferMode(_shoutTradeMode) ? MyBehavior.PartyTransferEntrySection.PlayerTroops : MyBehavior.PartyTransferEntrySection.PlayerPrisoners));
			foreach (MyBehavior.PartyTransferPromptEntry item in enumerable)
			{
				list.Add(new ShoutTradeResourceOption
				{
					Name = item.DisplayName,
					AvailableAmount = item.Count,
					PartyEntry = item
				});
			}
			return list;
		}
		if (IsShoutSettlementTransferMode(_shoutTradeMode))
		{
			List<MyBehavior.SettlementTransferPromptEntry> list3 = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(hero, characterObject).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.PlayerFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)).ToList();
			foreach (MyBehavior.SettlementTransferPromptEntry item in list3)
			{
				list.Add(new ShoutTradeResourceOption
				{
					Name = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(item),
					AvailableAmount = 1,
					SettlementEntry = item
				});
			}
			return list;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return list;
		}
		if (IsShoutTradeShowMode(_shoutTradeMode))
		{
			text = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		}
		int num = Hero.MainHero?.Gold ?? 0;
		if (IsShoutTradeShowMode(_shoutTradeMode))
		{
			num = MyBehavior.GetRemainingShowableGoldForExternal(hero, text, num);
		}
		if (num > 0)
		{
			list.Add(new ShoutTradeResourceOption
			{
				IsGold = true,
				ItemId = null,
				Name = "第纳尔",
				AvailableAmount = num,
				Item = null
			});
		}
		ItemRoster itemRoster = mobileParty.ItemRoster;
		if (itemRoster != null)
		{
			Dictionary<string, ShoutTradeResourceOption> dictionary = new Dictionary<string, ShoutTradeResourceOption>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				int amount = elementCopyAtIndex.Amount;
				if (item == null || amount <= 0)
				{
					continue;
				}
				string text2 = (item.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (dictionary.TryGetValue(text2, out var value))
				{
					value.AvailableAmount += amount;
					value.InventoryTotalValue += (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1);
				}
				else
				{
					dictionary[text2] = new ShoutTradeResourceOption
					{
						IsGold = false,
						ItemId = text2,
						Name = item.Name.ToString(),
						AvailableAmount = amount,
						Item = item,
						InventoryTotalValue = (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1)
					};
				}
			}
			foreach (ShoutTradeResourceOption value2 in dictionary.Values)
			{
				int availableAmount = value2.AvailableAmount;
				value2.InventoryUnitValue = (availableAmount > 0) ? Math.Max(1, (int)Math.Round((double)value2.InventoryTotalValue / (double)availableAmount, MidpointRounding.AwayFromZero)) : 1;
				if (IsShoutTradeShowMode(_shoutTradeMode))
				{
					availableAmount = MyBehavior.GetRemainingShowableItemCountForExternal(hero, text, value2.ItemId, availableAmount);
				}
				if (availableAmount > 0)
				{
					value2.AvailableAmount = availableAmount;
					list.Add(value2);
				}
			}
		}
		return list;
	}

	private void OnShoutTradeResourcesSelected(List<InquiryElement> selectedElements)
	{
		if (selectedElements == null || selectedElements.Count == 0 || _shoutTradeOptions == null || _shoutTradeOptions.Count == 0)
		{
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
			return;
		}
		_shoutPendingTradeItems.Clear();
		foreach (InquiryElement selectedElement in selectedElements)
		{
			int num = (int)selectedElement.Identifier;
			if (num >= 0 && num < _shoutTradeOptions.Count)
			{
				ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[num];
				_shoutPendingTradeItems.Add(new ShoutPendingTradeItem
				{
					IsGold = shoutTradeResourceOption.IsGold,
					ItemId = shoutTradeResourceOption.ItemId,
					ItemName = shoutTradeResourceOption.Name,
					Item = shoutTradeResourceOption.Item,
					InventoryUnitValue = shoutTradeResourceOption.InventoryUnitValue,
					PartyEntry = shoutTradeResourceOption.PartyEntry,
					SettlementEntry = shoutTradeResourceOption.SettlementEntry,
					Amount = (shoutTradeResourceOption.SettlementEntry != null) ? 1 : 0
				});
			}
		}
		if (_shoutPendingTradeItems.Count == 0)
		{
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
		}
		else
		{
			_shoutPendingTradeItemIndex = 0;
			if (IsShoutSettlementTransferMode(_shoutTradeMode))
			{
				if (_shoutTradeActionOnly)
				{
					CommitShoutTradeActionOnly();
				}
				else
				{
					ShowShoutTradeChatInput();
				}
			}
			else
			{
				ShowShoutTradeAmountInquiry();
			}
		}
	}

	private void ShowShoutTradeAmountInquiry()
	{
		if (_shoutPendingTradeItemIndex >= _shoutPendingTradeItems.Count)
		{
			if (_shoutTradeActionOnly)
			{
				CommitShoutTradeActionOnly();
			}
			else
			{
				ShowShoutTradeChatInput();
			}
			return;
		}
		ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[_shoutPendingTradeItemIndex];
		int availableAmount = 0;
		for (int i = 0; i < _shoutTradeOptions.Count; i++)
		{
			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];
			if (shoutPendingTradeItem.PartyEntry != null)
			{
				if (shoutTradeResourceOption.PartyEntry != null && shoutTradeResourceOption.PartyEntry.PromptIndex == shoutPendingTradeItem.PartyEntry.PromptIndex)
				{
					availableAmount = shoutTradeResourceOption.AvailableAmount;
					break;
				}
			}
			else if (shoutTradeResourceOption.IsGold == shoutPendingTradeItem.IsGold && shoutTradeResourceOption.ItemId == shoutPendingTradeItem.ItemId)
			{
				availableAmount = shoutTradeResourceOption.AvailableAmount;
				break;
			}
		}
		if (availableAmount <= 0)
		{
			_shoutPendingTradeItemIndex++;
			ShowShoutTradeAmountInquiry();
			return;
		}
		string titleText = (IsShoutTradeShowMode(_shoutTradeMode) ? "展示数量" : (IsShoutPartyTransferMode(_shoutTradeMode) ? "转移数量" : "给予数量"));
		string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutPendingTradeItem.ItemName);
		string text = $"[{_shoutPendingTradeItemIndex + 1}/{_shoutPendingTradeItems.Count}] {transferDisplayName} 最多可填 {availableAmount}。\n请输入 1 到 {availableAmount} 的整数：";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
			if (!int.TryParse(input, out var result) || result <= 0 || result > availableAmount)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的数量。"));
				ShowShoutTradeAmountInquiry();
			}
			else
			{
				_shoutPendingTradeItems[_shoutPendingTradeItemIndex].Amount = result;
				_shoutPendingTradeItemIndex++;
				ShowShoutTradeAmountInquiry();
			}
		}, delegate
		{
			BeginShoutTradeFlow(_shoutTradeTargetNpc, _shoutTradeMode);
		}), pauseGameActiveState: true);
	}

	private void ShowShoutTradeChatInput()
	{
		string text = _shoutTradeTargetNpc?.Name ?? "附近的人";
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount > 0)
			{
				if (shoutPendingTradeItem.PartyEntry != null)
				{
					if (IsShoutTroopTransferMode(_shoutTradeMode))
					{
						stringBuilder.AppendLine($"  · 转入 {shoutPendingTradeItem.Amount} 名 {shoutPendingTradeItem.ItemName}");
					}
					else
					{
						stringBuilder.AppendLine(shoutPendingTradeItem.PartyEntry.IsHero ? $"  · 交付俘虏 {shoutPendingTradeItem.ItemName}" : $"  · 交付 {shoutPendingTradeItem.Amount} 名 {shoutPendingTradeItem.ItemName} 俘虏");
					}
				}
				else if (shoutPendingTradeItem.SettlementEntry != null)
				{
					stringBuilder.AppendLine($"  · 转移 {(shoutPendingTradeItem.SettlementEntry.TypeLabel ?? "固定资产")} {shoutPendingTradeItem.ItemName}");
				}
				else if (shoutPendingTradeItem.IsGold)
				{
					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? $"  · 给予 {shoutPendingTradeItem.Amount} 第纳尔" : $"  · 展示 {shoutPendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					string transferDisplayName = CourierDeliveryBehavior.GetCourierLetterTransferDisplayTitleForExternal(shoutPendingTradeItem.ItemName);
					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? $"  · 给予 {shoutPendingTradeItem.Amount} 个 {transferDisplayName}" : $"  · 展示 {shoutPendingTradeItem.Amount} 个 {transferDisplayName}");
				}
			}
		}
		string text2 = (IsShoutTroopTransferMode(_shoutTradeMode) ? ("你准备将以下部队转入对方麾下：\n" + stringBuilder.ToString()) : (IsShoutPrisonerTransferMode(_shoutTradeMode) ? ("你准备将以下俘虏交给对方：\n" + stringBuilder.ToString()) : (IsShoutSettlementTransferMode(_shoutTradeMode) ? ("你准备将以下固定资产转给对方：\n" + stringBuilder.ToString()) : ((IsShoutTradeGiveMode(_shoutTradeMode) ? "你准备给予对方以下物品：\n" : "你准备向对方展示以下物品：\n") + stringBuilder.ToString()))));
		string titleText = text;
		PauseGame();
		if (!ShoutTextInputPopup.Show(titleText, text2, "请输入你想说的话：", "", OnShoutTradeChatConfirmed, delegate
		{
			ResetShoutTradeState();
			OnShoutCancelled();
		}, BuildShoutTargetEncyclopediaAction(_shoutTradeTargetNpc)))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2 + "\n请输入你想说的话：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "发送", "取消", OnShoutTradeChatConfirmed, delegate
			{
				ResetShoutTradeState();
				OnShoutCancelled();
			}), pauseGameActiveState: true);
		}
	}

	private void OnShoutTradeChatConfirmed(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			ResetShoutTradeState();
			ResumeGame();
			return;
		}
		if (!EnsureShoutTradePrimaryTargetValidForCommit(requireCurrentShoutFrame: true))
		{
			ResetShoutTradeState();
			ResumeGame();
			return;
		}
		string text = "";
		if (IsShoutTradeGiveMode(_shoutTradeMode))
		{
			ApplyShoutGiveTransfer();
			text = BuildShoutTradeFactText(isGive: true);
		}
		else
		{
			RecordShoutShownResources();
			text = BuildShoutTradeFactText(isGive: false);
			ShowShoutPendingDisplayValueMessage(EstimateShoutPendingShowTotalValue());
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			text = AppendShoutTradeActionFactSequence(text);
		}
		int? forcedPrimaryAgentIndex = ((_shoutTradeTargetNpc != null) ? new int?(_shoutTradeTargetNpc.AgentIndex) : ((int?)null));
		ResetShoutTradeState();
		OnShoutConfirmedWithContext(input, text, forcedPrimaryAgentIndex);
	}

	private void CommitShoutTradeActionOnly()
	{
		string fact = "";
		bool isGive = IsShoutTradeGiveMode(_shoutTradeMode);
		try
		{
			if (!EnsureShoutTradePrimaryTargetValidForCommit())
			{
				return;
			}
			if (isGive)
			{
				ApplyShoutGiveTransfer();
				fact = BuildShoutTradeFactText(isGive: true);
			}
			else
			{
				RecordShoutShownResources();
				fact = BuildShoutTradeFactText(isGive: false);
				ShowShoutPendingDisplayValueMessage(EstimateShoutPendingShowTotalValue());
			}
			RecordNativeConversationTradeActionFact(fact);
			if (!string.IsNullOrWhiteSpace(fact))
			{
				InformationManager.DisplayMessage(new InformationMessage("已记录给予/展示动作，NPC 会在后续 AI 交流中知道这件事。", new Color(0.4f, 1f, 0.4f)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversationTrade] action-only commit failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("给予/展示失败：" + ex.Message, new Color(1f, 0.35f, 0.25f)));
		}
		finally
		{
			ResetShoutTradeState();
			ResumeGame();
			FinishShoutTradeActionOnlyIfNeeded();
		}
	}

	private void RecordNativeConversationTradeActionFact(string fact)
	{
		fact = (fact ?? "").Trim();
		if (string.IsNullOrWhiteSpace(fact))
		{
			return;
		}
		fact = AppendShoutTradeActionFactSequence(fact);
		fact = NormalizeNativeConversationFactLineForPrompt(fact, "玩家动作");
		Hero targetHero = _shoutTradeTargetHeroOverride;
		CharacterObject targetCharacter = _shoutTradeTargetCharacterOverride;
		string npcName = (_shoutTradeTargetNpc?.Name ?? "").Trim();
		try
		{
			if ((targetHero == null && targetCharacter == null) && TryResolveNativeConversationTarget(out var hero, out var character, out var resolvedName))
			{
				targetHero = hero ?? character?.HeroObject;
				targetCharacter = character ?? targetHero?.CharacterObject;
				if (string.IsNullOrWhiteSpace(npcName))
				{
					npcName = resolvedName;
				}
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			}
			int targetAgentIndex = (_shoutTradeTargetNpc != null) ? _shoutTradeTargetNpc.AgentIndex : TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			if (targetAgentIndex >= 0)
			{
				string sharedFact = ContainsPlayerCraftedAfefInspectionSuffix(fact)
					? StripPlayerCraftedAfefInspectionSuffix(fact)
					: fact;
				AppendActionAfefFactToSceneHistoryInOrder(targetAgentIndex, sharedFact, mirrorToNativeSharedHistory: false);
				if (!string.Equals(sharedFact, fact, StringComparison.Ordinal))
				{
					PromotePersonalizedExtraFactInScenePrivateHistory(fact, targetAgentIndex);
				}
				QueuePendingCurrentAfefFactForAgent(targetAgentIndex, fact);
			}
			else
			{
				QueuePendingCurrentNativeAfefFactForKey(BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, _shoutTradeTargetNpc), fact);
			}
			if (targetHero != null)
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
			}
			AppendNativeConversationSessionHistory(
				targetHero,
				targetCharacter,
				npcName,
				"玩家动作",
				fact,
				"fact",
				targetAgentIndex: targetAgentIndex,
				npc: _shoutTradeTargetNpc,
				bridgeToSceneHistory: false);
			Logger.Log("NativeConversationTrade", "recorded action-only fact target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName) + " fact=" + fact.Replace("\r", "\\r").Replace("\n", "\\n"));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTrade", "[WARN] Failed to record action fact: " + ex.Message);
		}
	}

	private string AppendShoutTradeActionFactSequence(string fact)
	{
		fact = (fact ?? "").Trim();
		if (string.IsNullOrWhiteSpace(fact))
		{
			return "";
		}
		int sequence = Interlocked.Increment(ref _shoutTradeActionFactSequence);
		return fact + "（本次交易动作记录#" + sequence + "）";
	}

	private void FinishShoutTradeActionOnlyIfNeeded()
	{
		Action callback = _shoutTradeActionOnlyFinished;
		bool shouldCallback = _shoutTradeActionOnly || callback != null;
		_shoutTradeActionOnly = false;
		_shoutTradeNativeManager = null;
		_shoutTradeNativeMission = null;
		_shoutTradeNativeAgent = null;
		_shoutTradeTargetHeroOverride = null;
		_shoutTradeTargetCharacterOverride = null;
		_shoutTradeActionOnlyFinished = null;
		if (!shouldCallback)
		{
			return;
		}
		try
		{
			callback?.Invoke();
		}
		catch
		{
		}
	}

	private void ApplyShoutGiveTransfer()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return;
		}
		NpcDataPacket shoutTradeTargetNpc = _shoutTradeTargetNpc;
		ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
		RewardSystemBehavior.SettlementMerchantKind settlementMerchantKind = RewardSystemBehavior.SettlementMerchantKind.None;
		bool flag = hero == null && characterObject != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out settlementMerchantKind);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		int shoutTradeTargetAgentIndex = GetShoutTradeTargetAgentIndex();
		bool hasWildernessNonHeroParty = TryResolveWildernessNonHeroRewardParty(hero, characterObject, shoutTradeTargetAgentIndex, out var wildernessNonHeroParty);
		bool captureGcczSharedRelief = AfGcczShoutBridge.ShouldCaptureSharedReliefTransfer(shoutTradeTargetAgentIndex);
		string text = MyBehavior.BuildRuleTargetKeyForExternal(hero, characterObject, shoutTradeTargetAgentIndex);
		string playerCraftObserverKey = ResolveShownTradeTargetKey(shoutTradeTargetNpc, out var _);
		if (string.IsNullOrWhiteSpace(playerCraftObserverKey))
		{
			playerCraftObserverKey = text;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.SettlementEntry != null)
			{
				string statusText = "";
				bool flag2 = RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryApplyPlayerSettlementTransferForExternal(hero, shoutPendingTradeItem.SettlementEntry, out statusText);
				if (flag2)
				{
					InformationManager.DisplayMessage(new InformationMessage("已将 " + shoutPendingTradeItem.ItemName + " 转交给 " + GetShoutTradeTargetDisplayName() + "。", new Color(0.4f, 1f, 0.4f)));
				}
				else if (!string.IsNullOrWhiteSpace(statusText))
				{
					InformationManager.DisplayMessage(new InformationMessage(statusText, new Color(1f, 0.5f, 0.5f)));
				}
				shoutPendingTradeItem.Amount = flag2 ? 1 : 0;
			}
			else if (shoutPendingTradeItem.PartyEntry != null)
			{
                var partyEffect = MyBehavior.TransferPlayerPartyEntryWithObservedEffects(hero, characterObject, GetShoutTradeTargetAgentIndex(), shoutPendingTradeItem.PartyEntry, shoutPendingTradeItem.Amount);
                int num = partyEffect.Delivered;
                shoutPendingTradeItem.Amount = num;
                shoutPendingTradeItem.PartyTransferPartialFact = PartyTransferExecutionOwner.BuildPartialEffectFact(shoutPendingTradeItem.PartyEntry, partyEffect);
				if (num > 0)
				{
					string information = (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops) ? ("已将 " + num + " 名" + shoutPendingTradeItem.ItemName + "转入" + GetShoutTradeTargetDisplayName() + "的麾下") : (shoutPendingTradeItem.PartyEntry.IsHero ? ("已将俘虏" + shoutPendingTradeItem.ItemName + "交给" + GetShoutTradeTargetDisplayName()) : ("已将 " + num + " 名" + shoutPendingTradeItem.ItemName + "俘虏交给" + GetShoutTradeTargetDisplayName()));
					InformationManager.DisplayMessage(new InformationMessage(information, new Color(0.4f, 1f, 0.4f)));
				}
			}
			else if (shoutPendingTradeItem.IsGold)
			{
				int num2 = Math.Min(shoutPendingTradeItem.Amount, Hero.MainHero.Gold);
				shoutPendingTradeItem.Amount = num2;
				if (num2 > 0)
				{
					if (captureGcczSharedRelief && AfGcczShoutBridge.CaptureSharedReliefGoldTransfer(shoutTradeTargetAgentIndex, num2))
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
					}
					else if (hero != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, num2);
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, num2, null, 0);
						}
						catch
						{
						}
					}
					else if (flag && currentSettlement != null)
					{
						if (RewardSystemBehavior.Instance != null)
						{
							RewardSystemBehavior.Instance.TransferGoldToSettlement(currentSettlement, Hero.MainHero, num2);
						}
						else
						{
							GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
						}
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, num2, null, 0);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num2} 第纳尔。", characterObject?.Name?.ToString());
					}
					else
					{
						if (hasWildernessNonHeroParty && RewardSystemBehavior.Instance != null)
						{
							num2 = RewardSystemBehavior.Instance.TransferGoldToParty(wildernessNonHeroParty, Hero.MainHero, num2);
							shoutPendingTradeItem.Amount = num2;
							if (num2 > 0)
							{
								InformationManager.DisplayMessage(new InformationMessage("已将 " + num2 + " 第纳尔交给 " + GetShoutTradeTargetDisplayName() + " 所在部队。", new Color(0.4f, 1f, 0.4f)));
								RecordScenePrepaidTransfer(text, num2);
							}
						}
						else
						{
							GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
							RecordScenePrepaidTransfer(text, num2);
						}
					}
				}
			}
			else
			{
				if (mobileParty == null)
				{
					continue;
				}
				ItemRoster itemRoster = mobileParty.ItemRoster;
				if (itemRoster == null)
				{
					continue;
				}
				string text2 = (shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (!captureGcczSharedRelief && hasWildernessNonHeroParty && RewardSystemBehavior.Instance != null)
				{
					string itemName;
					int num3 = RewardSystemBehavior.Instance.TransferItemToParty(wildernessNonHeroParty, Hero.MainHero, text2, shoutPendingTradeItem.Amount, out itemName);
					shoutPendingTradeItem.Amount = num3;
					if (num3 > 0 && !string.IsNullOrWhiteSpace(itemName))
					{
						shoutPendingTradeItem.ItemName = itemName;
					}
					continue;
				}
				ItemObject itemObject;
				int num4 = MyBehavior.RemoveItemsFromRosterByStringId(itemRoster, text2, shoutPendingTradeItem.Amount, out itemObject);
				shoutPendingTradeItem.Amount = num4;
				if (num4 > 0)
				{
					if (captureGcczSharedRelief && AfGcczShoutBridge.CaptureSharedReliefItemTransfer(shoutTradeTargetAgentIndex, text2, num4, itemObject ?? shoutPendingTradeItem.Item, shoutPendingTradeItem.InventoryUnitValue))
					{
						continue;
					}
					if (hero?.PartyBelongedTo != null && itemObject != null)
					{
						hero.PartyBelongedTo.ItemRoster.AddToCounts(itemObject, num4);
					}
					else if (flag && currentSettlement?.ItemRoster != null && itemObject != null)
					{
						currentSettlement.ItemRoster.AddToCounts(itemObject, num4);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, 0, text2, num4);
						string text3 = RewardSystemBehavior.Instance?.BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, num4) ?? "";
						string merchantFactItemName = CourierDeliveryBehavior.GetCourierLetterTransferFactDescriptionForExternal(
							text2,
							itemObject.Id.InternalValue,
							itemObject.Name?.ToString() ?? text2);
						merchantFactItemName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
							text2,
							itemObject.Id.InternalValue,
							merchantFactItemName,
							null,
							characterObject,
							playerCraftObserverKey,
							"give",
							commit: true);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num4} 个 {merchantFactItemName}{text3}。", characterObject?.Name?.ToString());
					}
					if (hero != null)
					{
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, 0, text2, num4);
						}
						catch
						{
						}
					}
				}
			}
		}
	}

	private static int GetCurrentCampaignDaySafe()
	{
		try
		{
			return (int)CampaignTime.Now.ToDays;
		}
		catch
		{
			return -1;
		}
	}

	private static string GetCurrentSettlementIdSafe()
	{
		try
		{
			return (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeSceneRevisitKeyToken(string text)
	{
		string text2 = (text ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\s+", " ");
		return text2.Trim();
	}

	private static string BuildCurrentSceneRevisitKeySafe()
	{
		try
		{
			ParseCurrentScenePlaceAndSpotForPrompt(out var placeName, out var spotName);
			string[] value = new string[4]
			{
				NormalizeSceneRevisitKeyToken(GetCurrentSettlementIdSafe()),
				NormalizeSceneRevisitKeyToken(Mission.Current?.SceneName),
				NormalizeSceneRevisitKeyToken(placeName),
				NormalizeSceneRevisitKeyToken(spotName)
			};
			string text = string.Join("|", value.Where(x => !string.IsNullOrWhiteSpace(x)));
			return text.Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildSceneHeroRevisitRecordKey(Hero hero, string sceneKey)
	{
		string text = (hero?.StringId ?? "").Trim();
		string text2 = (sceneKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text + "@" + text2;
	}

	// Same-day revisits should not read as "0天前"; use a dedicated sentence so the prompt stays natural.
	private static string BuildSceneRevisitFactBody(string playerDisplayName, int elapsedDays)
	{
		string text = string.IsNullOrWhiteSpace(playerDisplayName) ? "玩家" : playerDisplayName.Trim();
		if (elapsedDays <= 0)
		{
			return "今天稍早时候刚与" + text + "见过面。";
		}
		return "距离你上次与" + text + "见面，已有" + elapsedDays + "天了。";
	}

	private void TryInjectSceneFirstMeetingFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData)
	{
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		List<(NpcDataPacket Npc, string Fact)> list = new List<(NpcDataPacket, string)>();
		lock (_historyLock)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (!(nearbyDatum?.IsHero ?? false))
				{
					continue;
				}
				Hero hero = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				string text = (hero?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text) || _sceneHeroFirstMeetingShownThisSession.Contains(text))
				{
					continue;
				}
				string firstMeetingNpcFactTextForPromptIfNeeded = MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero, persistToHistory: false);
				if (string.IsNullOrWhiteSpace(firstMeetingNpcFactTextForPromptIfNeeded))
				{
					continue;
				}
				list.Add((nearbyDatum, firstMeetingNpcFactTextForPromptIfNeeded.Trim()));
				_sceneHeroFirstMeetingShownThisSession.Add(text);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			// This fact is written in second person for one observer. Broadcasting it to every
			// nearby NPC can mix different notoriety results into the same prompt.
			RecordExtraFactToSceneHistory(list[i].Fact, new List<NpcDataPacket>(1) { list[i].Npc });
		}
	}

	// Inject the scene revisit AFEF before the player's new line is written, otherwise Token_Stats will place it below the player utterance.
	private void TryInjectSceneRevisitFactsBeforePlayerMessage(List<NpcDataPacket> nearbyData)
	{
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
		if (currentCampaignDaySafe < 0)
		{
			return;
		}
		string text = BuildCurrentSceneRevisitKeySafe();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string playerDisplayNameForShout = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerDisplayNameForShout))
		{
			playerDisplayNameForShout = "玩家";
		}
		List<(Hero Hero, string SceneHistoryName, string RecordKey, int ElapsedDays)> list = new List<(Hero, string, string, int)>();
		lock (_historyLock)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (!(nearbyDatum?.IsHero ?? false))
				{
					continue;
				}
				Hero hero = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				if (hero == null)
				{
					continue;
				}
				string text2 = BuildSceneHeroRevisitRecordKey(hero, text);
				if (string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
				{
					continue;
				}
				if (_sceneHeroRevisitHandledThisSession.Contains(text2))
				{
					continue;
				}
				if (_sceneHeroRevisitDays.TryGetValue(text2, out var value))
				{
					list.Add((hero, GetSceneNpcHistoryNameForPrompt(nearbyDatum), text2, Math.Max(0, currentCampaignDaySafe - value)));
				}
				_sceneHeroRevisitDays[text2] = currentCampaignDaySafe;
				_sceneHeroRevisitHandledThisSession.Add(text2);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (var item in list)
		{
			string text3 = string.IsNullOrWhiteSpace(item.SceneHistoryName) ? (item.Hero.Name?.ToString() ?? "对方") : item.SceneHistoryName;
			string text4 = BuildSceneRevisitFactBody(playerDisplayNameForShout, item.ElapsedDays);
			string extraFact = "[AFEF NPC行为补充] " + text3 + text4;
			RecordExtraFactToSceneHistory(extraFact, nearbyData);
			MyBehavior.AppendExternalDialogueHistory(item.Hero, null, null, "[AFEF NPC行为补充] " + text4);
		}
	}

	private void RecordScenePrepaidTransfer(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
			string currentSettlementIdSafe = GetCurrentSettlementIdSafe();
			lock (_historyLock)
			{
				if (!_scenePrepaidTransfers.TryGetValue(text, out var value) || value == null || value.Day != currentCampaignDaySafe || !string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					value = new ScenePrepaidTransferRecord
					{
						Gold = 0,
						Day = currentCampaignDaySafe,
						SettlementId = currentSettlementIdSafe
					};
					_scenePrepaidTransfers[text] = value;
				}
				value.Gold += goldAmount;
			}
		}
		catch
		{
		}
	}

	private void RecordNegotiatedNonHeroBribe(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
			string currentSettlementIdSafe = GetCurrentSettlementIdSafe();
			lock (_historyLock)
			{
				if (!_scenePrepaidTransfers.TryGetValue(text, out var value) || value == null || value.Day != currentCampaignDaySafe || !string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					value = new ScenePrepaidTransferRecord
					{
						Gold = 0,
						NegotiatedGold = 0,
						Day = currentCampaignDaySafe,
						SettlementId = currentSettlementIdSafe
					};
					_scenePrepaidTransfers[text] = value;
				}
				value.NegotiatedGold = goldAmount;
			}
		}
		catch
		{
		}
	}

	internal int GetRecentNonHeroGoldForRuleTarget(string targetKey)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
			string currentSettlementIdSafe = GetCurrentSettlementIdSafe();
			lock (_historyLock)
			{
				if (_scenePrepaidTransfers.TryGetValue(text, out var value) && value != null && value.Day == currentCampaignDaySafe && string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					return Math.Max(0, value.Gold);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	internal int GetNegotiatedNonHeroBribeForRuleTarget(string targetKey)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			int currentCampaignDaySafe = GetCurrentCampaignDaySafe();
			string currentSettlementIdSafe = GetCurrentSettlementIdSafe();
			lock (_historyLock)
			{
				if (_scenePrepaidTransfers.TryGetValue(text, out var value) && value != null && value.Day == currentCampaignDaySafe && string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					return Math.Max(0, value.NegotiatedGold);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	internal void ConsumeRecentNonHeroGoldForRuleTarget(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			lock (_historyLock)
			{
				if (_scenePrepaidTransfers.TryGetValue(text, out var value) && value != null)
				{
					value.Gold = Math.Max(0, value.Gold - goldAmount);
				}
			}
		}
		catch
		{
		}
	}

	private bool TryCaptureNegotiatedLordsHallBribe(NpcDataPacket npc, Agent agent, ref string content)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(content) || npc == null)
			{
				return false;
			}
			Match match = Regex.Match(content, "\\[ACTION:LORDS_HALL_BRIBE_PRICE:(\\d+)\\]", RegexOptions.IgnoreCase);
			if (!match.Success || !int.TryParse(match.Groups[1].Value, out var result) || result <= 0)
			{
				return false;
			}
			Agent agent2 = agent ?? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
			CharacterObject characterObject = agent2?.Character as CharacterObject;
			string text = MyBehavior.BuildRuleTargetKeyForExternal(null, characterObject, npc.AgentIndex);
			if (!string.IsNullOrWhiteSpace(text))
			{
				RecordNegotiatedNonHeroBribe(text, result);
			}
			content = Regex.Replace(content, "\\[ACTION:LORDS_HALL_BRIBE_PRICE:(\\d+)\\]", "", RegexOptions.IgnoreCase).Trim();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsLordsHallGuardAgent(Agent agent)
	{
		try
		{
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
					MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
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

	private bool TryUnlockLordsHallForNpc(NpcDataPacket npc, Agent agent)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement;
			Mission mission = Mission.Current;
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (settlement == null || !settlement.IsTown || mission == null || locationComplex == null || currentLocation == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL outside an active town-center scene. speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
				return false;
			}
			if (!string.Equals(currentLocation.StringId, "center", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL outside town center. location=" + (currentLocation.StringId ?? "") + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
				return false;
			}
			Location lordsHall = locationComplex.GetLocationWithId("lordshall");
			Location center = locationComplex.GetLocationWithId("center");
			if (lordsHall == null || center == null || Campaign.Current?.GameMenuManager == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] ignored OPEN_LORDS_HALL because the location transition is unavailable. settlement=" + (settlement.StringId ?? "") + " hasHall=" + (lordsHall != null) + " hasCenter=" + (center != null));
				return false;
			}
			Logger.Log("ShoutBehavior", "[LordsHallAccess] accepted OPEN_LORDS_HALL. settlement=" + (settlement.StringId ?? "") + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL validation failed: " + ex.Message);
		}
		return false;
	}

	private bool TryTriggerOpenLordsHallAction(NpcDataPacket npc, Agent agent, ref string content)
	{
		try
		{
			if (!ContainsOpenLordsHallActionTag(content))
			{
				return false;
			}
			content = StripLordsHallAccessActionTagsForScene(content);
			bool result = TryUnlockLordsHallForNpc(npc, agent);
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL parsed handled=" + result + " speaker=" + (npc?.Name ?? agent?.Name?.ToString() ?? "unknown"));
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] OPEN_LORDS_HALL parse failed: " + ex.Message);
			return false;
		}
	}

	private string BuildShoutTradeFactText(bool isGive)
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return "";
		}
		if (isGive && IsShoutSettlementTransferMode(_shoutTradeMode))
		{
			string playerName = GetPlayerDisplayNameForShout();
			string targetName = GetShoutTradeTargetDisplayName();
			List<string> facts = new List<string>();
			for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
			{
				ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
				if (shoutPendingTradeItem?.SettlementEntry != null && shoutPendingTradeItem.Amount > 0)
				{
					facts.Add(playerName + "已经将" + shoutPendingTradeItem.ItemName + "转交给" + targetName + "的家族。");
				}
			}
			return string.Join("\n", facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
		}
		if (isGive && IsShoutPartyTransferMode(_shoutTradeMode))
		{
			return BuildShoutPartyTransferFactText();
		}
		string text = GetPlayerDisplayNameForShout();
		string text2 = GetShoutTradeTargetDisplayName();
		ResolveShoutTradeRuntimeTarget(out var observerHero, out var observerCharacter, out var _);
		int observerAgentIndex = GetShoutTradeTargetAgentIndex();
		string observerKey = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out var _);
		if (string.IsNullOrWhiteSpace(observerKey))
		{
			observerKey = MyBehavior.BuildRuleTargetKeyForExternal(observerHero, observerCharacter, observerAgentIndex);
		}
		List<string> list = new List<string>();
		long num = 0L;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount > 0)
			{
				if (shoutPendingTradeItem.IsGold)
				{
					list.Add($"{shoutPendingTradeItem.Amount} 第纳尔");
					num += shoutPendingTradeItem.Amount;
				}
				else
				{
					string itemFactName = CourierDeliveryBehavior.GetCourierLetterTransferFactDescriptionForExternal(
						shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId,
						shoutPendingTradeItem.Item?.Id.InternalValue ?? 0u,
						shoutPendingTradeItem.ItemName);
					string playerCraftedItemId = (shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(playerCraftedItemId))
					{
						itemFactName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
							playerCraftedItemId,
							shoutPendingTradeItem.Item?.Id.InternalValue ?? 0u,
							itemFactName,
							observerHero,
							observerCharacter ?? observerHero?.CharacterObject,
							observerKey,
							isGive ? "give" : "show",
							commit: true);
					}
					string text3;
					if (isGive)
					{
						text3 = shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "";
						string text4 = "";
						try
						{
							ResolveShoutTradeRuntimeTarget(out var hero, out var characterObject, out var _);
							if (characterObject != null && hero == null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
							{
								text4 = RewardSystemBehavior.Instance.BuildSettlementItemValueFactSuffixForExternal(Settlement.CurrentSettlement, text3, shoutPendingTradeItem.Amount);
								num += RewardSystemBehavior.Instance.EstimateSettlementItemValueForExternal(Settlement.CurrentSettlement, text3, shoutPendingTradeItem.Amount);
							}
							else
							{
								text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(hero ?? Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
								num += RewardSystemBehavior.Instance?.EstimateItemValueForExternal(hero ?? Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? 0L;
							}
						}
						catch
						{
							text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
							num += RewardSystemBehavior.Instance?.EstimateItemValueForExternal(Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? 0L;
						}
						list.Add($"{shoutPendingTradeItem.Amount} 个 {itemFactName}{text4}");
					}
					else
					{
						string text5 = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? "";
						num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0L;
						list.Add($"{shoutPendingTradeItem.Amount} 个 {itemFactName}{text5}");
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		if (isGive)
		{
			string text6 = (num > 0L) ? ("（合计总值约 " + num + " 第纳尔）") : "";
			return text + "已经将 " + string.Join("、", list) + text6 + " 交给 " + text2 + "。如果你正在和"+text+"交易，并核对好了数量和价值，那么你现在可以将商量好的物品，第纳尔或人交给"+text+"";
		}
		return text + "给 " + text2 + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，但暂未交付给你，不过证明了他有这些东西，如果你正在和他交易，请问他索要，尽量不要先把东西交给他，如果不是交易，就无需索要";
	}

	private string BuildShoutPartyTransferFactText()
	{
		string playerName = GetPlayerDisplayNameForShout();
		string targetName = GetShoutTradeTargetDisplayName();
		List<string> facts = new List<string>();
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
            if (!string.IsNullOrWhiteSpace(shoutPendingTradeItem?.PartyTransferPartialFact))
                facts.Add(shoutPendingTradeItem.PartyTransferPartialFact);
			if (shoutPendingTradeItem?.PartyEntry == null || shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			string itemName = (shoutPendingTradeItem.ItemName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(itemName))
			{
				itemName = (shoutPendingTradeItem.PartyEntry.DisplayName ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(itemName))
			{
				itemName = "目标";
			}
			if (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops)
			{
				facts.Add(playerName + "已经将 " + shoutPendingTradeItem.Amount + " 名" + itemName + "转入" + targetName + "的麾下。");
			}
			else if (shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerPrisoners)
			{
				if (shoutPendingTradeItem.PartyEntry.IsHero)
				{
					facts.Add(playerName + "已经将俘虏" + itemName + "交给" + targetName + "。");
				}
				else
				{
					facts.Add(playerName + "已经将 " + shoutPendingTradeItem.Amount + " 名" + itemName + "俘虏交给" + targetName + "。");
				}
			}
		}
		return string.Join("\n", facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

	private long EstimateShoutPendingShowTotalValue()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return 0L;
		}
		long num = 0L;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem == null || shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				num += shoutPendingTradeItem.Amount;
			}
			else
			{
				num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0L;
			}
		}
		return num;
	}

	private void ShowShoutPendingDisplayValueMessage(long totalValue)
	{
		if (totalValue <= 0)
		{
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage("【展示估值】你向 " + GetShoutTradeTargetDisplayName() + " 展示了总值为 " + totalValue + " 第纳尔的财物。", new Color(0.95f, 0.85f, 0.25f)));
	}

	internal static string GetPlayerDisplayNameForShout()
	{
		return MyBehavior.BuildPlayerPublicDisplayNameForExternal();
	}

	private string GetShoutTradeTargetDisplayName()
	{
		string text = (_shoutTradeTargetNpc?.Name ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "对方" : text;
	}

	private static string BuildSingleNpcSceneReplyInstruction(string npcName, bool hasMultiplePresentNpcs)
	{
		string text = (npcName ?? "").Trim();
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		if (hasMultiplePresentNpcs)
		{
			return "请只以" + text + "的身份回复，并结合role=user中其他人刚才说过的话来回应，不要各说各的。绝对不可以代替他人说话";
		}
		return text + "现在正在与" + text2 + "单独交谈。请只以" + text + "的身份回应" + text2 + "。绝对不可以代替他人说话";
	}

	internal static bool HasInjectedRuleBlockForPostprocess(string instructions, string ruleId)
	{
		string text = (instructions ?? "").Trim();
		string text2 = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf("【附加规则:" + text2 + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool HasInjectedRuleBlockForExternal(string instructions, string ruleId)
	{
		return HasInjectedRuleBlockForPostprocess(instructions, ruleId);
	}

	internal static void MarkWeeklyMemoryMaterialTriggerForScene(Hero targetHero, CharacterObject targetCharacter, string npcName, string normalizedTags, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions, List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions, List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions, bool forceLooseSession = false, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(normalizedTags))
			{
				return;
			}
			Hero memoryHero = targetHero ?? targetCharacter?.HeroObject;
			string nonHeroMemoryId = "";
			string memoryName = (npcName ?? "").Trim();
			if (memoryHero == null)
			{
				if (TryResolveWildernessNonHeroMemory(null, targetHero, targetCharacter, targetAgentIndex, out var resolvedMemoryId, out var resolvedMemoryName))
				{
					nonHeroMemoryId = resolvedMemoryId;
					if (string.IsNullOrWhiteSpace(memoryName))
					{
						memoryName = resolvedMemoryName;
					}
				}
			}
			if (memoryHero == null && string.IsNullOrWhiteSpace(nonHeroMemoryId))
			{
				return;
			}
			int sceneSessionId = forceLooseSession ? -1 : TryGetCurrentSceneHistorySessionIdForHistoryPersistence();
			MyBehavior.MarkWeeklyMemoryMaterialTriggerWithAllSnapshotsForExternal(memoryHero, nonHeroMemoryId, string.IsNullOrWhiteSpace(memoryName) ? "NPC" : memoryName, normalizedTags, sceneSessionId, -1, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, suppressImplicitDialogueSession: forceLooseSession, partyTransferAllTroopOptions: partyTransferAllTroopOptions, partyTransferAllPrisonerOptions: partyTransferAllPrisonerOptions);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WeeklyMemoryMaterial] mark failed: " + ex.Message);
		}
	}

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
		if (!TryPrepareCourierActionPostprocessForExternal(targetHero, targetCharacter, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, out CourierActionPostprocessWorkItem workItem, out string immediateResult, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, preprocessRuleHits, entityPostprocessContext, targetAgentIndex, latestReplyHasPlayerInput, forceLooseWeeklyMemoryMaterialSession, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, chainName))
		{
			return immediateResult;
		}
		if (!AIConfigHandler.TryCallAuxiliaryActionPostprocess(workItem.SystemPrompt, workItem.UserPrompt, 5000, 0f, out string content, out string error))
		{
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] 调用失败: " + error);
			return workItem.FallbackText;
		}
		return workItem.CompleteOnMainThread(content);
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
		reason = "";
		try
		{
			Agent agent = null;
			var agents = Mission.Current?.Agents;
			if (agents != null)
			{
				if (targetAgentIndex >= 0)
				{
					agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				}
				if (agent == null)
				{
					agent = agents.FirstOrDefault((Agent a) => a != null && targetHero != null && a.Character == targetHero.CharacterObject);
				}
			}
			if (targetHero == null)
			{
				if (agent == null)
				{
					return true;
				}
				try
				{
					if (!agent.IsActive() || agent.IsMainAgent || agent == Agent.Main || !(agent.Character is CharacterObject))
					{
						reason = "target_agent_invalid";
						return false;
					}
				}
				catch
				{
					reason = "target_agent_invalid";
					return false;
				}
			}
		}
		catch
		{
		}
		return true;
	}

	internal static string BuildPostprocessRuleTextForScene(IEnumerable<PostprocessRuleEntry> entries)
	{
		return ConversationActionPostprocessOwner.BuildPostprocessRuleTextForScene(entries);
	}

	internal static bool IsNpcSurrenderPostprocessContext()
	{
		bool flag = false;
		try
		{
			flag = Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter;
		}
		catch
		{
		}
		try
		{
			flag = flag || LordEncounterBehavior.IsEncounterMeetingMissionActive;
		}
		catch
		{
		}
		try
		{
			flag = flag || MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
		}
		if (!flag)
		{
			return false;
		}
		try
		{
			if (MeetingBattleRuntime.IsCombatEscalated)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static bool IsNativeConversationPostprocessChain(string chainName)
	{
		return ConversationActionPostprocessOwner.IsNativeConversationPostprocessChain(chainName);
	}

	private static bool IsActiveSiegeSettlementForSurrender(Settlement settlement)
	{
		try
		{
			if (settlement == null || !settlement.IsFortification)
			{
				return false;
			}
			if (settlement.IsUnderSiege || settlement.SiegeEvent != null)
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			return PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}

	private static Settlement ResolveSiegeEventSettlementForSurrender(SiegeEvent siegeEvent)
	{
		if (siegeEvent == null)
		{
			return null;
		}
		try
		{
			Settlement settlement = siegeEvent.BesiegedSettlement;
			if (IsActiveSiegeSettlementForSurrender(settlement))
			{
				return settlement;
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.All?.FirstOrDefault((Settlement x) => x != null && x.SiegeEvent == siegeEvent && IsActiveSiegeSettlementForSurrender(x));
		}
		catch
		{
			return null;
		}
	}

	private static void AddSiegeSettlementCandidate(List<Settlement> settlements, Settlement settlement)
	{
		if (settlements == null || !IsActiveSiegeSettlementForSurrender(settlement))
		{
			return;
		}
		if (!settlements.Any((Settlement x) => x == settlement))
		{
			settlements.Add(settlement);
		}
	}

	private static void AddSiegePartyCandidate(List<PartyBase> parties, PartyBase party)
	{
		if (parties == null || party == null)
		{
			return;
		}
		if (!parties.Any((PartyBase x) => x == party))
		{
			parties.Add(party);
		}
	}

	private static PartyBase ResolveNativeConversationAgentPartyForSiegeSurrender(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0)
			{
				return null;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
			return agent?.Origin?.BattleCombatant as PartyBase;
		}
		catch
		{
			return null;
		}
	}

	private static List<PartyBase> ResolveNativeConversationPartiesForSiegeSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		List<PartyBase> list = new List<PartyBase>();
		AddSiegePartyCandidate(list, ResolveNativeConversationAgentPartyForSiegeSurrender(targetAgentIndex));
		try
		{
			AddSiegePartyCandidate(list, targetHero?.PartyBelongedTo?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, targetCharacter?.HeroObject?.PartyBelongedTo?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, MobileParty.ConversationParty?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, PlayerEncounter.EncounteredParty);
		}
		catch
		{
		}
		return list;
	}

	private static Settlement ResolveActiveSiegeSettlementForNativeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		List<Settlement> list = new List<Settlement>();
		try
		{
			AddSiegeSettlementCandidate(list, PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, PlayerEncounter.EncounterSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, Settlement.CurrentSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, MobileParty.MainParty?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, MobileParty.MainParty?.BesiegedSettlement);
			AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(MobileParty.MainParty?.SiegeEvent));
		}
		catch
		{
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		try
		{
			AddSiegeSettlementCandidate(list, targetHero?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, targetHero?.PartyBelongedTo?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, targetHero?.PartyBelongedTo?.BesiegedSettlement);
			AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(targetHero?.PartyBelongedTo?.SiegeEvent));
		}
		catch
		{
		}
		foreach (PartyBase party in ResolveNativeConversationPartiesForSiegeSurrender(targetHero, targetCharacter, targetAgentIndex))
		{
			try
			{
				AddSiegeSettlementCandidate(list, party.Settlement);
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, party.MobileParty?.CurrentSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.BesiegedSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.TargetSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.ShortTermTargetSettlement);
				AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(party.MobileParty?.SiegeEvent));
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, party.MapEvent?.MapEventSettlement);
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(party.SiegeEvent));
			}
			catch
			{
			}
		}
		return list.FirstOrDefault();
	}

	private static IFaction ResolveSiegeSurrenderTargetFaction(Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties)
	{
		try
		{
			if (targetHero?.MapFaction != null)
			{
				return targetHero.MapFaction;
			}
		}
		catch
		{
		}
		try
		{
			if (targetCharacter?.HeroObject?.MapFaction != null)
			{
				return targetCharacter.HeroObject.MapFaction;
			}
		}
		catch
		{
		}
		foreach (PartyBase party in parties ?? new List<PartyBase>())
		{
			try
			{
				if (party?.MapFaction != null)
				{
					return party.MapFaction;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static IFaction ResolveSiegeAttackerFaction(Settlement settlement)
	{
		try
		{
			if (settlement?.SiegeEvent?.BesiegerCamp?.MapFaction != null)
			{
				return settlement.SiegeEvent.BesiegerCamp.MapFaction;
			}
		}
		catch
		{
		}
		try
		{
			return settlement?.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsSiegeAttackerPartyForSurrender(Settlement settlement, PartyBase party)
	{
		if (!IsActiveSiegeSettlementForSurrender(settlement) || party == null)
		{
			return false;
		}
		try
		{
			if (settlement.SiegeEvent?.BesiegerCamp?.HasInvolvedPartyForEventType(party) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			if (mobileParty != null && settlement.SiegeEvent?.BesiegerCamp?.IsBesiegerSideParty(mobileParty) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mobileParty2 = party.MobileParty;
			if (mobileParty2 != null && (mobileParty2.BesiegedSettlement == settlement || mobileParty2.SiegeEvent == settlement.SiegeEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsSiegeDefenderPartyForSurrender(Settlement settlement, PartyBase party)
	{
		if (!IsActiveSiegeSettlementForSurrender(settlement) || party == null)
		{
			return false;
		}
		try
		{
			if (party.IsSettlement && party.Settlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (settlement.HasInvolvedPartyForEventType(party))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.MobileParty?.CurrentSettlement == settlement && !IsSiegeAttackerPartyForSurrender(settlement, party))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryResolveSiegeSurrenderSide(Settlement settlement, Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties, out BattleSideEnum side)
	{
		side = BattleSideEnum.None;
		if (!IsActiveSiegeSettlementForSurrender(settlement))
		{
			return false;
		}
		foreach (PartyBase party in parties ?? new List<PartyBase>())
		{
			if (IsSiegeAttackerPartyForSurrender(settlement, party))
			{
				side = BattleSideEnum.Attacker;
				return true;
			}
			if (IsSiegeDefenderPartyForSurrender(settlement, party))
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		try
		{
			if (targetHero?.CurrentSettlement == settlement)
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction faction = ResolveSiegeSurrenderTargetFaction(targetHero, targetCharacter, parties);
			IFaction defenderFaction = settlement.MapFaction;
			IFaction attackerFaction = ResolveSiegeAttackerFaction(settlement);
			if (faction != null && attackerFaction != null && faction == attackerFaction)
			{
				side = BattleSideEnum.Attacker;
				return true;
			}
			if (faction != null && defenderFaction != null && faction == defenderFaction)
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement == settlement || MobileParty.MainParty?.CurrentSettlement == settlement)
			{
				side = BattleSideEnum.Defender;
				return targetHero != null || targetCharacter != null;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool TryResolveNativeConversationSiegeSurrenderContext(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out Settlement settlement, out BattleSideEnum targetSide, out string sideLabel)
	{
		settlement = null;
		targetSide = BattleSideEnum.None;
		sideLabel = "";
		try
		{
			targetHero = targetHero ?? targetCharacter?.HeroObject;
			settlement = ResolveActiveSiegeSettlementForNativeConversation(targetHero, targetCharacter, targetAgentIndex);
			if (!IsActiveSiegeSettlementForSurrender(settlement))
			{
				return false;
			}
			List<PartyBase> parties = ResolveNativeConversationPartiesForSiegeSurrender(targetHero, targetCharacter, targetAgentIndex);
			if (!TryResolveSiegeSurrenderSide(settlement, targetHero, targetCharacter, parties, out targetSide))
			{
				return false;
			}
			sideLabel = targetSide == BattleSideEnum.Attacker ? "攻城方" : "守城方";
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[SiegeSurrender] context resolve failed: " + ex.Message);
			settlement = null;
			targetSide = BattleSideEnum.None;
			sideLabel = "";
			return false;
		}
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
		int num = 6;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null && RewardSystemBehavior.Instance != null)
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance.GetEffectiveTrust(hero));
			}
		}
		catch
		{
		}
		if (num <= 4)
		{
			return 0;
		}
		if (num == 5)
		{
			return 1;
		}
		if (num == 6)
		{
			return 2;
		}
		if (num == 7)
		{
			return 3;
		}
		if (num == 8)
		{
			return 4;
		}
		return int.MaxValue;
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

	private static string StripLordsHallAccessActionTagsForScene(string text)
	{
		string text2 = text ?? "";
		text2 = Regex.Replace(text2, "\\[ACTION:OPEN_LORDS_HALL\\]", "", RegexOptions.IgnoreCase);
		return text2.Trim();
	}

	internal static bool ContainsOpenLordsHallActionTag(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, "\\[ACTION:OPEN_LORDS_HALL\\]", RegexOptions.IgnoreCase);
	}

	private static string StripSceneMechanismActionTagsForScene(string text)
	{
		return ConversationActionPostprocessOwner.StripSceneMechanismActionTagsForScene(text);
	}

	private static string ExtractSceneMechanismActionTagsForScene(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in Regex.Matches(text2, "\\[(?:(?:ASS|ACTION:SCENE_SUMMON):[^\\]\\r\\n]+|(?:GUI|ACTION:SCENE_GUIDE):[^\\]\\r\\n]+|ACTION:(?:SETS_REQUEST_MASSACRE|SETS_START_MASSACRE|SETS_STOP_MASSACRE|SETS_CANCEL_MASSACRE_REQUEST)|FOL|ACTION:SCENE_FOLLOW_PLAYER|STP|ACTION:SCENE_STOP_FOLLOW|END)\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join(" ", list).Trim();
	}

	private static string BuildSceneMechanismTargetListForPostprocess(List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
	{
		return ConversationActionPostprocessOwner.BuildSceneMechanismTargetListForPostprocess(CapturePostprocessSummonTargets(summonTargets), CapturePostprocessGuideTargets(guideTargets));
	}

	private List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForScene(NpcDataPacket speaker, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, bool includeGenericRules = true)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		int num = speaker?.AgentIndex ?? (-1);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string inspectionSlaughterRule = TroopInspectionBehavior.BuildPrisonerSlaughterPostprocessRuleForExternal(num);
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterRule))
		{
			AddSceneMechanismPostprocessRule(
				list,
				hashSet,
				TroopInspectionPrisonerSlaughterProfile.ActionTag,
				inspectionSlaughterRule);
		}
		string noblePrisonerExecutionRule = NoblePrisonerEscortBehavior.BuildSceneExecutionPostprocessRule(num);
		if (!string.IsNullOrWhiteSpace(noblePrisonerExecutionRule))
		{
			AddSceneMechanismPostprocessRule(
				list,
				hashSet,
				NoblePrisonerEscortBehavior.ExecuteActionTag,
				noblePrisonerExecutionRule);
		}
		if (!includeGenericRules || AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			return list;
		}
		Agent agent = (num >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == num) : null;
		bool setsSceneActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(Mission.Current);
		SetsSettlementSceneKind sceneKind = SettlementEntryTroopSelectionBehavior.GetSetsSettlementSceneKindForExternal(Mission.Current);
		bool setsMassacreActive = setsSceneActive && SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(Mission.Current);
		bool speakerIsSelectedFollower = SettlementEntryTroopSelectionBehavior.IsSetsSelectedFollowerAgentForExternal(agent);
		bool anyMassacreRequestPending = setsSceneActive && SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(Mission.Current);
		bool massacreRequestPendingForSpeaker = anyMassacreRequestPending && SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreRequestPendingForExternal(Mission.Current, num);
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferStopRule(setsSceneActive, setsMassacreActive, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.StopActionTag, SetsOwnedSettlementMassacreProfile.BuildStopRuleDescription());
		}
		if (setsMassacreActive)
		{
			return list;
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferRequestRule(setsSceneActive, setsMassacreActive, anyMassacreRequestPending, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.RequestActionTag, SetsOwnedSettlementMassacreProfile.BuildRequestRuleDescription(sceneKind));
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferStartRule(setsSceneActive, setsMassacreActive, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.StartActionTag, SetsOwnedSettlementMassacreProfile.BuildStartRuleDescription(sceneKind, massacreRequestPendingForSpeaker));
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferCancelRequestRule(setsSceneActive, setsMassacreActive, massacreRequestPendingForSpeaker, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.CancelRequestActionTag, SetsOwnedSettlementMassacreProfile.BuildCancelRequestRuleDescription());
		}
		List<PostprocessRuleEntry> guardrailRulePostprocessRules = AIConfigHandler.GetGuardrailRulePostprocessRules("scene_mechanism_actions") ?? new List<PostprocessRuleEntry>();
		if (guardrailRulePostprocessRules.Count == 0)
		{
			return list;
		}
		SceneSummonConversationSession sceneSummonConversationSession = (num >= 0) ? _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(num) : null;
		bool flag = sceneSummonConversationSession != null;
		bool flag2 = agent != null && _sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent);
		if (SceneMovementController.IsPrisonBreakRescueMissionActive())
		{
			if (!SceneMovementController.IsPrisonBreakRescuePrisonerAgent(agent))
			{
				return list;
			}
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in guardrailRulePostprocessRules)
			{
				string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				string text2 = guardrailRulePostprocessRule?.Description ?? "";
				if (string.Equals(text, "[ACTION:SCENE_FOLLOW_PLAYER]", StringComparison.OrdinalIgnoreCase))
				{
					if (!flag2)
					{
						AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
					}
					continue;
				}
				if (string.Equals(text, "[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase) && flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
			}
			return list;
		}
		foreach (PostprocessRuleEntry guardrailRulePostprocessRule in guardrailRulePostprocessRules)
		{
			string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
			string text2 = guardrailRulePostprocessRule?.Description ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_FOLLOW_PLAYER]", StringComparison.OrdinalIgnoreCase))
			{
				if (!flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase))
			{
				if (flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (string.Equals(text, "[END]", StringComparison.OrdinalIgnoreCase))
			{
				if (flag)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (text.StartsWith("[ACTION:SCENE_SUMMON:", StringComparison.OrdinalIgnoreCase))
			{
				if ((summonTargets?.Count).GetValueOrDefault() > 0)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (!text.StartsWith("[ACTION:SCENE_GUIDE:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if ((guideTargets?.Count).GetValueOrDefault() > 0 || (summonTargets?.Count).GetValueOrDefault() > 0)
			{
				AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
			}
		}
		return list;
	}

	private static void AddSceneMechanismPostprocessRule(List<PostprocessRuleEntry> target, HashSet<string> seenTags, string tag, string description)
	{
		if (target == null || seenTags == null)
		{
			return;
		}
		string text = (tag ?? "").Trim();
		string text2 = description ?? "";
		if (!string.IsNullOrWhiteSpace(text) && seenTags.Add(text))
		{
			target.Add(new PostprocessRuleEntry
			{
				Tag = text,
				Description = text2
			});
		}
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
		StringBuilder result = new StringBuilder();
		foreach (PostprocessRuleEntry entry in entries ?? Enumerable.Empty<PostprocessRuleEntry>())
		{
			string tag = (entry?.Tag ?? string.Empty).Trim();
			if (tag.Length == 0)
			{
				continue;
			}

			result.Append(tag);
			IReadOnlyList<TownAmbientReactionActionKind> kinds = TownAmbientReactionTagCatalog.ExtractKinds(tag);
			if (kinds.Count == 1
				&& TownAmbientReactionTagCatalog.TryGetSuggestedAction(
					kinds[0],
					out SiegeInterventionActionKind suggestedAction))
			{
				result.Append(": ").Append(text.GetSuggestionActionLabel(suggestedAction));
			}
			else if (!string.IsNullOrWhiteSpace(entry.Description))
			{
				result.Append(": ").Append(entry.Description.Trim());
			}
			result.AppendLine();
		}
		return result.ToString().TrimEnd();
	}

	internal static string EnsureScenePostprocessFallbackMood(string dialogue)
	{
		string normalizedDialogue = (dialogue ?? string.Empty).Trim();
		if (Regex.Matches(normalizedDialogue, "\\[ACTION:MOOD:[^\\]]+\\]", RegexOptions.IgnoreCase).Count > 0
			|| string.IsNullOrWhiteSpace(AIConfigHandler.ActionPostprocessFallbackMoodTag))
		{
			return normalizedDialogue;
		}
		return (normalizedDialogue + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
	}



	private static string NormalizePlayerNameForScenePostprocess(string text, string npcName = null)
	{
		return ConversationActionPostprocessOwner.NormalizePlayerNameForScenePostprocess(text, npcName);
	}

	private static string BuildSceneActionPostprocessUserPrompt(string userPromptTemplate, string tagRules, string npcName, string historyText, string latestReplyBlock, string sharedItemList = null, string playerItemList = null, string debtHint = null, string marriagePlayerCandidates = null, string marriageTargetCandidates = null, string runtimeContext = null)
	{
		return ConversationActionPostprocessOwner.BuildSceneActionPostprocessUserPrompt(userPromptTemplate, tagRules, npcName, historyText, latestReplyBlock, sharedItemList, playerItemList, debtHint, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
	}

	private static string RemoveStandaloneLatestReplySection(string userPromptTemplate)
	{
		string text = userPromptTemplate ?? "";
		text = Regex.Replace(text, "(?:\\r?\\n)*<latest_reply>\\s*\\{reply\\}\\s*</latest_reply>(?:\\r?\\n)*", Environment.NewLine, RegexOptions.IgnoreCase);
		return Regex.Replace(text, "(\\r?\\n){3,}", Environment.NewLine + Environment.NewLine);
	}

	private static string InsertTaggedLatestReplyAfterScenePublicSection(string historyText, string taggedLatestReplyBlock)
	{
		string text = (historyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		string text2 = (taggedLatestReplyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return string.IsNullOrWhiteSpace(text) ? "（无）" : text;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (text.IndexOf("<latest_reply>", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		string[] array = text.Split('\n');
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = (array[i] ?? "").Trim();
			if (text3.StartsWith("【当前场景公共对话与互动", StringComparison.Ordinal) && text3.EndsWith("】", StringComparison.Ordinal))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return (text + "\n" + text2).Trim();
		}
		int num2 = array.Length;
		for (int j = num + 1; j < array.Length; j++)
		{
			string text4 = (array[j] ?? "").Trim();
			if (text4.StartsWith("【", StringComparison.Ordinal) && text4.EndsWith("】", StringComparison.Ordinal))
			{
				num2 = j;
				break;
			}
		}
		List<string> list = new List<string>(array.Length + 1);
		for (int k = 0; k < num2; k++)
		{
			list.Add(array[k]);
		}
		list.Add(text2);
		for (int l = num2; l < array.Length; l++)
		{
			list.Add(array[l]);
		}
		return string.Join("\n", list).Trim();
	}

	private long _sceneShoutProcessingSequence;





	private void ResetSceneShoutRuntimeOnMissionEnd(string reason)
	{
        _sceneMovementController?.Reset();
		PublicExecutionOrderRuntime.Reset();
		ResetSceneAudioRequestOwnership();
		try
		{
			_sceneConversation.AdvanceConversationEpoch();
			Interlocked.Increment(ref _sceneHistorySessionId);
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
		try
		{
			MatchCollection matchCollection = Regex.Matches(tags ?? "", "\\[ACTION:MOOD:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase);
			if (matchCollection == null || matchCollection.Count <= 0)
			{
				return false;
			}
			string text = (matchCollection[matchCollection.Count - 1]?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			string text2 = text;
			Hero hero = ResolveHeroFromAgentIndex(speaker?.AgentIndex ?? (-1));
			if (hero != null)
			{
				MyBehavior.ApplyPostprocessMoodFromSceneHeroResponseExternal(hero, ref text2);
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] mood_applied hero=" + (hero.StringId ?? "") + " mood=" + text);
			}
			else
			{
				MyBehavior.ApplyPostprocessMoodFromSceneUnnamedResponseExternal(speaker?.UnnamedKey, speaker?.Name, ref text2);
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] mood_applied unnamed=" + (speaker?.UnnamedKey ?? speaker?.Name ?? "") + " mood=" + text);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] DeferredPostprocess mood apply failed: " + ex.Message);
			return false;
		}
	}

	private bool TryConsumeSceneNpcSurrenderTag(NpcDataPacket speaker, ref string content, out Hero targetHero, out CharacterObject targetCharacter, out int targetAgentIndex)
	{
		targetHero = null;
		targetCharacter = null;
		targetAgentIndex = speaker?.AgentIndex ?? (-1);
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, "\\[ACTION:NPC_SURRENDER\\]", RegexOptions.IgnoreCase))
		{
			return false;
		}
		content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
		try
		{
			int resolvedAgentIndex = targetAgentIndex;
			Agent agent = (resolvedAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == resolvedAgentIndex) : null;
			targetCharacter = agent?.Character as CharacterObject;
			targetHero = targetCharacter?.HeroObject;
			if (targetHero == null && targetAgentIndex >= 0)
			{
				targetHero = ResolveHeroFromAgentIndex(targetAgentIndex);
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
		}
		catch
		{
		}
		Logger.Log("NpcSurrender", "Consumed scene dialog NPC surrender tag. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? speaker?.Name ?? "unknown") + " agentIndex=" + targetAgentIndex);
		return true;
	}




	private void RecordShoutShownResources()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return;
		}
		Hero hero = null;
		string targetKey = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		if (hero == null && string.IsNullOrWhiteSpace(targetKey))
		{
			return;
		}
		int num = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				num += shoutPendingTradeItem.Amount;
				continue;
			}
			string text = (shoutPendingTradeItem.ItemId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!dictionary.ContainsKey(text))
				{
					dictionary[text] = 0;
				}
				dictionary[text] += shoutPendingTradeItem.Amount;
			}
		}
		MyBehavior.RecordShownResourcesForExternal(hero, targetKey, num, dictionary);
	}

	private string ResolveShownTradeTargetKey(NpcDataPacket targetNpc, out Hero hero)
	{
		hero = null;
		if (targetNpc == null)
		{
			return "";
		}
		if (_shoutTradeTargetHeroOverride != null)
		{
			hero = _shoutTradeTargetHeroOverride;
			if (!string.IsNullOrWhiteSpace(hero.StringId))
			{
				return hero.StringId;
			}
		}
		if (targetNpc.IsHero)
		{
			try
			{
				hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			}
			catch
			{
				hero = null;
			}
			if (!string.IsNullOrWhiteSpace(hero?.StringId))
			{
				return hero.StringId;
			}
		}
		string text = (targetNpc.UnnamedKey ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return AppendSceneAgentIdentityToShownTargetKey(text, targetNpc.AgentIndex);
		}
		string text2 = (targetNpc.TroopId ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return AppendSceneAgentIdentityToShownTargetKey("troop:" + text2, targetNpc.AgentIndex);
		}
		if (!string.IsNullOrWhiteSpace(targetNpc.Name))
		{
			return AppendSceneAgentIdentityToShownTargetKey(
				("name:" + targetNpc.Name).Trim().ToLowerInvariant(),
				targetNpc.AgentIndex);
		}
		return "";
	}

	private static string AppendSceneAgentIdentityToShownTargetKey(string targetKey, int agentIndex)
	{
		string key = (targetKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(key)
			|| key.IndexOf("|agent:", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return key;
		}
		string scope = ResolveShownTradeNonHeroScope(agentIndex);
		if (!string.IsNullOrWhiteSpace(scope)
			&& key.IndexOf("|scope:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			key += "|scope:" + scope;
		}
		if (agentIndex < 0)
		{
			return key;
		}
		return key + "|agent:" + agentIndex;
	}

	private static string ResolveShownTradeNonHeroScope(int agentIndex)
	{
		try
		{
			string settlementId = NormalizeWildernessNonHeroMemoryKeyPart(
				Settlement.CurrentSettlement?.StringId);
			if (!string.IsNullOrWhiteSpace(settlementId))
			{
				return "settlement:" + settlementId;
			}
			string partyId = NormalizeWildernessNonHeroMemoryKeyPart(
				TryResolveWildernessNonHeroMobileParty(agentIndex)?.StringId);
			if (!string.IsNullOrWhiteSpace(partyId))
			{
				return "party:" + partyId;
			}
			string sceneName = NormalizeWildernessNonHeroMemoryKeyPart(
				Mission.Current?.SceneName);
			if (!string.IsNullOrWhiteSpace(sceneName))
			{
				return "scene:" + sceneName;
			}
		}
		catch
		{
		}
		return "";
	}

	private void ResetShoutTradeState()
	{
		_shoutTradeOptions.Clear();
		_shoutPendingTradeItems.Clear();
		_shoutPendingTradeItemIndex = 0;
		_shoutTradeMode = ShoutChatMode.Normal;
		_shoutTradeTargetNpc = null;
		// Release the scene-bound identity when the UI flow ends; Agents must not outlive a completed trade flow.
		_shoutTradeTargetAgentSnapshot = null;
	}

	private async void OnShoutConfirmed(string shoutText)
	{
		await ProcessShoutConfirmedInternal(shoutText, null, null);
	}

	private async void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		await ProcessShoutConfirmedInternal(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	// The opaque request is transient, never persisted, and never installed as the UI's mutable context.
	internal object CaptureScenePlayerShoutRequestForReplay(Agent[] framedTargets, Agent primaryTarget)
	{
		if (!IsBannerlordMainThreadForNativeActions() || framedTargets == null || primaryTarget == null
			|| !framedTargets.Any(agent => ReferenceEquals(agent, primaryTarget)))
		{
			return null;
		}
		return CaptureScenePlayerShoutRequest(framedTargets, primaryTarget.Index);
	}

	internal bool IsCapturedScenePlayerShoutRequestCurrent(object capturedRequest)
	{
		ScenePlayerShoutRequest request = capturedRequest as ScenePlayerShoutRequest;
		return IsBannerlordMainThreadForNativeActions()
			&& IsScenePlayerShoutRequestCurrent(request)
			&& _scenePlayerShoutRequestOwner.IsUnclaimed(request);
	}





	internal bool TryReplayCapturedScenePlayerShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		object capturedRequest, Action<Action> runWithObservationScope)
	{
		ScenePlayerShoutRequest request = capturedRequest as ScenePlayerShoutRequest;
		if (!IsBannerlordMainThreadForNativeActions() || !IsScenePlayerShoutRequestCurrent(request)
			|| string.IsNullOrWhiteSpace(shoutText) || runWithObservationScope == null
			|| !_scenePlayerShoutRequestOwner.TryClaim(request))
		{
			return false;
		}
		_ = ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, runWithObservationScope);
		return true;
	}

	private async Task ProcessShoutConfirmedInternal(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		if (!IsBannerlordMainThreadForNativeActions())
		{
			Logger.Log("ShoutBehavior", "[SceneInput] rejected off-main-thread submission");
			return;
		}
		if (string.IsNullOrWhiteSpace(shoutText))
		{
			ResumeGame();
			return;
		}
		ShoutTargetingContext targetingContext = _activeShoutTargetingContext;
		List<Agent> framedTargets = GetAgentsForShoutTargetingContext(targetingContext);
		int primaryAgentIndex = forcedPrimaryAgentIndex ?? targetingContext?.PrimaryAgentIndex ?? -1;
		ScenePlayerShoutRequest request = CaptureScenePlayerShoutRequest(framedTargets, primaryAgentIndex);
		if (request == null)
		{
			return;
		}
		_scenePlayerShoutRequestOwner.MarkStarted(request);
		await ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, null);
	}



	internal static List<NpcDataPacket> BuildGroupSpeakingCandidates(List<NpcDataPacket> allNpcData, NpcDataPacket primaryNpc)
	{
		List<NpcDataPacket> speakingCandidates = new List<NpcDataPacket>();
		HashSet<int> addedAgentIndices = new HashSet<int>();
		if (primaryNpc != null && addedAgentIndices.Add(primaryNpc.AgentIndex))
		{
			speakingCandidates.Add(primaryNpc);
		}
		if (allNpcData != null)
		{
			foreach (NpcDataPacket n in allNpcData)
			{
				if (n == null || !addedAgentIndices.Add(n.AgentIndex))
				{
					continue;
				}
				speakingCandidates.Add(n);
			}
		}
		return speakingCandidates;
	}

	private void HoldSceneConversationParticipants(List<NpcDataPacket> participants)
	{
		var agents = Mission.Current?.Agents;
		if (ShouldSuppressSceneConversationControlForMeeting() || participants == null || participants.Count <= 1 || agents == null)
		{
			return;
		}
		ExtendStaringHoldForPlayerDrivenSceneRound(participants.Count);
		for (int i = 0; i < participants.Count; i++)
		{
			NpcDataPacket npcDataPacket = participants[i];
			if (npcDataPacket == null)
			{
				continue;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == npcDataPacket.AgentIndex);
			if (agent != null && agent.IsActive() && agent.IsHuman && agent != Agent.Main)
			{
				AddAgentToStareList(agent, interruptCurrentUse: false);
			}
		}
	}

	private void HoldSceneConversationAgents(List<Agent> participants)
	{
		if (ShouldSuppressSceneConversationControlForMeeting() || participants == null || participants.Count <= 1 || Mission.Current == null)
		{
			return;
		}
		ExtendStaringHoldForPlayerDrivenSceneRound(participants.Count);
		for (int i = 0; i < participants.Count; i++)
		{
			Agent agent = participants[i];
			if (agent != null && agent.IsActive() && agent.IsHuman && agent != Agent.Main)
			{
				AddAgentToStareList(agent, interruptCurrentUse: false);
			}
		}
	}

	internal static NpcDataPacket CloneNpcDataPacket(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return null;
		}
		return new NpcDataPacket
		{
			Name = npc.Name,
			RoleDesc = npc.RoleDesc,
			PersonalityDesc = npc.PersonalityDesc,
			BackgroundDesc = npc.BackgroundDesc,
			AgentIndex = npc.AgentIndex,
			IsHero = npc.IsHero,
			CultureId = npc.CultureId,
			UnnamedKey = npc.UnnamedKey,
			TroopId = npc.TroopId,
			UnnamedRank = npc.UnnamedRank,
			IsFemale = npc.IsFemale,
			Age = npc.Age,
			PromptGivenName = npc.PromptGivenName,
			PromptDisplayName = npc.PromptDisplayName,
			HasScenePosition = npc.HasScenePosition,
			ScenePositionX = npc.ScenePositionX,
			ScenePositionY = npc.ScenePositionY,
			ScenePositionZ = npc.ScenePositionZ
		};
	}

	internal static List<NpcDataPacket> CloneNpcDataSnapshot(List<NpcDataPacket> allNpcData)
	{
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		if (allNpcData == null || allNpcData.Count == 0)
		{
			return list;
		}
		foreach (NpcDataPacket npcDataPacket in allNpcData)
		{
			NpcDataPacket item = CloneNpcDataPacket(npcDataPacket);
			if (item != null)
			{
				list.Add(item);
			}
		}
		return list;
	}

	internal static void ApplySceneLocalDisambiguatedNames(List<NpcDataPacket> allNpcData)
	{
		if (allNpcData == null || allNpcData.Count == 0)
		{
			return;
		}
		ShoutUtils.EnsureScenePromptNames(allNpcData);
	}




	private static string NormalizeSceneExtraFactForHistory(string extraFact)
	{
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (!text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal)
			&& !text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			text = "[AFEF玩家行为补充] " + text;
		}
		return text;
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
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc != null)
			{
				return npc;
			}
		}
		catch
		{
		}
		if (agentIndex < 0 && string.IsNullOrWhiteSpace(fallbackName))
		{
			return null;
		}
		return new NpcDataPacket
		{
			AgentIndex = agentIndex,
			Name = string.IsNullOrWhiteSpace(fallbackName) ? "NPC" : fallbackName.Trim(),
			IsHero = false,
			CultureId = "neutral"
		};
	}

	private void AppendSceneEventToNativeSharedHistory(NpcDataPacket targetNpc, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Agent targetAgentOverride = null)
	{
		text = (text ?? "").Replace("\r", "").Trim();
		if (targetNpc == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			Agent targetAgent = targetAgentOverride != null && targetAgentOverride.Index == targetNpc.AgentIndex ? targetAgentOverride : null;
			CharacterObject character = targetAgent?.Character as CharacterObject;
			Hero hero = character?.HeroObject;
			if (hero == null && targetAgent == null)
			{
				hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
				character = hero?.CharacterObject;
			}
			if (character == null && targetNpc.AgentIndex >= 0)
			{
				targetAgent ??= Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex && a.IsActive());
				character = targetAgent?.Character as CharacterObject;
				hero ??= character?.HeroObject;
			}
			string npcName = GetSceneNpcHistoryNameForPrompt(targetNpc);
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? targetNpc.Name ?? "NPC").Trim();
			}
			AppendNativeConversationSessionHistory(hero, character, npcName, speaker, text, kind, eventSequence, bridgeToSceneHistory: false, targetAgentIndex: targetNpc.AgentIndex, npc: targetNpc, playerTargetAgentIndex: playerTargetAgentIndex, playerTargetName: playerTargetName);
		}
		catch (Exception ex)
		{
			Logger.Log("SharedConversationHistory", "[WARN] scene->native append failed: " + ex.Message);
		}
	}

	private void AppendSceneEventToNativeSharedHistoryForTargets(IEnumerable<NpcDataPacket> targets, string speaker, string text, string kind, long eventSequence, int playerTargetAgentIndex = -1, string playerTargetName = null, Dictionary<int, Agent> audienceAgentsByIndex = null)
	{
		if (targets == null)
		{
			return;
		}
		HashSet<string> writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcDataPacket target in targets)
		{
			if (target == null)
			{
				continue;
			}
			string key = target.AgentIndex.ToString();
			if (!writtenKeys.Add(key))
			{
				continue;
			}
			Agent targetAgent = null;
			audienceAgentsByIndex?.TryGetValue(target.AgentIndex, out targetAgent);
			AppendSceneEventToNativeSharedHistory(target, speaker, text, kind, eventSequence, playerTargetAgentIndex, playerTargetName, targetAgent);
		}
	}

	private void QueuePendingCurrentNativeAfefFactForSceneTarget(NpcDataPacket targetNpc, string fact)
	{
		if (targetNpc == null || string.IsNullOrWhiteSpace(fact))
		{
			return;
		}
		try
		{
			Hero hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			CharacterObject character = hero?.CharacterObject;
			if (character == null && targetNpc.AgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex && a.IsActive());
				character = agent?.Character as CharacterObject;
				hero ??= character?.HeroObject;
			}
			string npcName = GetSceneNpcHistoryNameForPrompt(targetNpc);
			if (string.IsNullOrWhiteSpace(npcName))
			{
				npcName = (hero?.Name?.ToString() ?? character?.Name?.ToString() ?? targetNpc.Name ?? "NPC").Trim();
			}
			string key = BuildNativeConversationHistoryKey(hero, character, npcName, targetNpc.AgentIndex, targetNpc);
			if (!string.IsNullOrWhiteSpace(key))
			{
				QueuePendingCurrentNativeAfefFactForKey(key, fact);
			}
		}
		catch
		{
		}
	}

	private void QueuePendingCurrentAfefFactForAgent(int targetAgentIndex, string fact)
	{
		if (targetAgentIndex < 0)
		{
			return;
		}
		string factLine = NormalizeAfefFactLineForPrompt(fact);
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		_scenePendingAfefFactsOwner.Queue(targetAgentIndex, message);
	}

	private List<ConversationMessage> ConsumePendingCurrentAfefFactMessagesForPrompt(int targetAgentIndex)
	{
		return _scenePendingAfefFactsOwner.Consume(targetAgentIndex);
	}

	private void QueuePendingCurrentNativeAfefFactForKey(string key, string fact)
	{
		key = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		string factLine = NormalizeAfefFactLineForPrompt(fact);
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1
		});
		_nativeSessionOwner.QueueFact(key, message);
	}

	private List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(string key)
	{
		key = (key ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key))
		{
			return new List<ConversationMessage>();
		}
		return _nativeSessionOwner.ConsumeFacts(key);
	}

	private void AppendActionAfefFactToSceneHistoryInOrder(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true) => AppendActionAfefFactToSceneHistoryInOrderCaptured(targetAgentIndex, fact, mirrorToNativeSharedHistory);

	private bool PersistExtraFactToNamedHeroes(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1,
		bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return true;
		}
		string sharedExtraFact = personalizedAgentIndex >= 0
			? StripPlayerCraftedAfefInspectionSuffix(extraFact)
			: extraFact;
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		if (personalizedAgentIndex >= 0)
		{
			NpcDataPacket personalizedTarget = nearbyData.FirstOrDefault(
				npc => npc != null && npc.AgentIndex == personalizedAgentIndex);
			accepted &= PersistExtraFactToNamedObserver(
				personalizedTarget,
				extraFact,
				persistedNonHeroIds,
				requireMemoryReceipt);
		}
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum == null || nearbyDatum.AgentIndex == personalizedAgentIndex)
			{
				continue;
			}
			accepted &= PersistExtraFactToNamedObserver(
				nearbyDatum,
				sharedExtraFact,
				persistedNonHeroIds,
				requireMemoryReceipt);
		}
		return accepted;
	}

	private bool PersistExtraFactToNamedObserver(
		NpcDataPacket observer,
		string extraFact,
		HashSet<string> persistedNonHeroIds,
		bool requireMemoryReceipt)
	{
		if (observer == null || string.IsNullOrWhiteSpace(extraFact))
		{
			return true;
		}
		if (observer.IsHero)
		{
			Hero hero = ResolveHeroFromAgentIndex(observer.AgentIndex);
			if (hero != null)
			{
				if (requireMemoryReceipt)
					return MyBehavior.CommitDialogueHistoryWithScene(hero.StringId, false, hero.Name?.ToString(), null, null, extraFact, _sceneHistorySessionId)?.HistoryWritten == true;
				MyBehavior.AppendExternalSceneDialogueHistory(
					hero,
					null,
					null,
					extraFact,
					_sceneHistorySessionId);
			}
			return true;
		}
		if (TryResolveWildernessNonHeroMemory(
			observer,
			null,
			null,
			observer.AgentIndex,
			out var memoryId,
			out var memoryName)
			&& (persistedNonHeroIds == null || persistedNonHeroIds.Add(memoryId)))
		{
			if (requireMemoryReceipt)
				return MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, null, null, extraFact, _sceneHistorySessionId)?.HistoryWritten == true;
			MyBehavior.AppendExternalNonHeroSceneDialogueHistory(
				memoryId,
				memoryName,
				null,
				null,
				extraFact,
				_sceneHistorySessionId);
		}
		return true;
	}

	private bool RecordPlayerMessage(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false) => RecordPlayerMessageCaptured(text, nearbyData, primaryTargetAgentIndex, primaryTargetName, audienceAgentsByIndex, requireMemoryReceipt);

	internal static bool ShouldDeferExtraFactPersistenceUntilAfterSceneReply(string extraFact)
	{
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.Contains("看了看 总值为")
			&& text.Contains("暂未交付给你")
			&& text.Contains("证明了他有这些东西");
	}

	internal static bool ContainsPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return !string.IsNullOrWhiteSpace(extraFact)
			&& extraFact.IndexOf(
				PlayerCraftedAfefInspectionSuffix,
				StringComparison.Ordinal) >= 0;
	}

	internal static string StripPlayerCraftedAfefInspectionSuffix(string extraFact)
	{
		return (extraFact ?? "").Replace(
			PlayerCraftedAfefInspectionSuffix,
			"");
	}

	private void SetPendingHeroHistoryExtraFactAfterSceneReply(
		string extraFact,
		List<NpcDataPacket> nearbyData,
		int personalizedAgentIndex = -1)
	{
		_pendingHeroHistoryExtraFactAfterSceneReply = (extraFact ?? "").Trim();
		_pendingHeroHistoryExtraFactTargetsAfterSceneReply = CloneNpcDataSnapshot(nearbyData) ?? new List<NpcDataPacket>();
		_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = personalizedAgentIndex;
	}

	private void PromotePersonalizedExtraFactInScenePrivateHistory(
		string personalizedExtraFact,
		int personalizedAgentIndex) => PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(personalizedExtraFact, personalizedAgentIndex);

	private bool FlushPendingHeroHistoryExtraFactAfterSceneReply(bool requireMemoryReceipt = false)
	{
		string text = (_pendingHeroHistoryExtraFactAfterSceneReply ?? "").Trim();
		List<NpcDataPacket> list = _pendingHeroHistoryExtraFactTargetsAfterSceneReply ?? new List<NpcDataPacket>();
		int personalizedAgentIndex =
			_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply;
		_pendingHeroHistoryExtraFactAfterSceneReply = "";
		_pendingHeroHistoryExtraFactTargetsAfterSceneReply = new List<NpcDataPacket>();
		_pendingHeroHistoryExtraFactPersonalizedAgentIndexAfterSceneReply = -1;
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		if (personalizedAgentIndex >= 0)
		{
			PromotePersonalizedExtraFactInScenePrivateHistory(
				text,
				personalizedAgentIndex);
		}
		return PersistExtraFactToNamedHeroes(
			text,
			list,
			personalizedAgentIndex,
			requireMemoryReceipt);
	}

	private static bool IsSingleUseSceneNpcFactText(string text) => SceneHistoryProjectionOwner.IsSingleUseSceneNpcFactText(text);

	private static bool IsSceneConversationTurn(ConversationMessage msg) => SceneHistoryProjectionOwner.IsSceneConversationTurn(msg);

	private static void RemoveExpiredSingleUseSceneNpcFacts(List<ConversationMessage> history) => SceneHistoryProjectionOwner.RemoveExpiredSingleUseSceneNpcFacts(history);

	private bool RecordResponseForAllNearbySafe(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false) => RecordResponseForAllNearbySafeCaptured(nearbyData, speakerAgentIndex, speakerName, response, requireMemoryReceipt);

	private void RecordSystemFactForNearbySafe(List<NpcDataPacket> nearbyData, string factText) => RecordSystemFactForNearbySafeCaptured(nearbyData, factText);

	internal static void BuildHeroPersonaFallback(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (hero == null)
		{
			return;
		}
		string text = "";
		string text2 = "";
		string value = "";
		try
		{
			text = hero.Culture?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		try
		{
			text2 = hero.Clan?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		try
		{
			value = hero.MapFaction?.Name?.ToString() ?? hero.Clan?.Kingdom?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		string arg = "英雄";
		try
		{
			if (MyBehavior.IsHeroActiveKingdomRulerForExternal(hero))
			{
				arg = "统治者";
			}
			else if (hero.Clan?.Leader == hero)
			{
				arg = "家族族长";
			}
			else if (hero.IsLord)
			{
				arg = "领主";
			}
			else if (hero.IsWanderer)
			{
				arg = "流浪者";
			}
			else if (hero.IsNotable)
			{
				arg = "要人";
			}
		}
		catch
		{
		}
		string arg2 = (string.IsNullOrWhiteSpace(text) ? "" : (text + "的"));
		personality = $"{hero.Name}是一位{arg2}{arg}，处事谨慎务实，重视秩序与利益平衡。";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(hero.Name).Append("出身于");
		stringBuilder.Append(string.IsNullOrWhiteSpace(text2) ? "本地家族" : text2);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append("，当前活跃于").Append(value).Append("的政治与军事事务中");
		}
		background = stringBuilder.ToString().TrimEnd('。', '.', ' ') + "。";
	}

	private Hero ResolveHeroFromAgentIndex(int agentIndex)
	{
		try
		{
			BasicCharacterObject basicCharacterObject = (Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex))?.Character;
			if (basicCharacterObject == null || !basicCharacterObject.IsHero)
			{
				return null;
			}
			return (basicCharacterObject is CharacterObject characterObject) ? characterObject.HeroObject : null;
		}
		catch
		{
			return null;
		}
	}












	private NpcDataPacket BuildSceneNpcDataFromLocationCharacter(LocationCharacter locationCharacter)
	{
		if (locationCharacter?.Character == null)
		{
			return null;
		}
		Agent agent = SceneMovementController.ResolveAgentForLocationCharacter(locationCharacter);
		if (agent != null)
		{
			return ShoutUtils.ExtractNpcData(agent);
		}
		CharacterObject character = locationCharacter.Character;
		NpcDataPacket npcDataPacket = new NpcDataPacket
		{
			Name = character.Name?.ToString() ?? "NPC",
			AgentIndex = -1,
			IsHero = character.IsHero,
			CultureId = character.Culture?.StringId?.ToLowerInvariant() ?? "neutral",
			IsFemale = character.IsFemale,
			Age = character.IsHero ? (character.HeroObject?.Age ?? 30f) : 30f,
			UnnamedKey = "",
			TroopId = "",
			UnnamedRank = "",
			RoleDesc = character.IsHero ? "英雄" : "平民",
			PersonalityDesc = "",
			BackgroundDesc = ""
		};
		if (character.IsHero && character.HeroObject != null)
		{
			Hero hero = character.HeroObject;
			if (hero.IsLord)
			{
				npcDataPacket.RoleDesc = "领主";
			}
			else if (hero.IsWanderer)
			{
				npcDataPacket.RoleDesc = "流浪者";
			}
			else if (hero.IsNotable)
			{
				npcDataPacket.RoleDesc = "要人";
			}
			MyBehavior.GetNpcPersonaForExternal(hero, out var personality, out var background);
			npcDataPacket.PersonalityDesc = (personality ?? "").Trim();
			npcDataPacket.BackgroundDesc = (background ?? "").Trim();
		}
		else
		{
			npcDataPacket.TroopId = (character.StringId ?? "").Trim().ToLowerInvariant();
			if (character.Occupation == Occupation.Villager)
			{
				npcDataPacket.RoleDesc = "村民";
			}
			else if (character.Occupation == Occupation.Guard)
			{
				npcDataPacket.RoleDesc = "守卫";
			}
			else if (character.Occupation == Occupation.Mercenary)
			{
				npcDataPacket.RoleDesc = "士兵";
			}
			else
			{
				npcDataPacket.RoleDesc = "平民";
			}
		}
		ShoutUtils.EnsurePromptNameFields(npcDataPacket);
		return npcDataPacket;
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
		List<int> list = new List<int>();
		if (nearbyData == null || nearbyData.Count == 0)
		{
			return list;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < nearbyData.Count; i++)
		{
			NpcDataPacket npcDataPacket = nearbyData[i];
			if (npcDataPacket != null && npcDataPacket.AgentIndex >= 0 && hashSet.Add(npcDataPacket.AgentIndex))
			{
				list.Add(npcDataPacket.AgentIndex);
			}
		}
		return list;
	}

	private static string NormalizeSceneHeroId(string heroId) => SceneHistoryProjectionOwner.NormalizeSceneHeroId(heroId);

	private static string ResolveSceneHeroIdFromAgentIndex(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return "";
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			CharacterObject characterObject = agent?.Character as CharacterObject;
			return NormalizeSceneHeroId(characterObject?.HeroObject?.StringId);
		}
		catch
		{
			return "";
		}
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
		string text = (systemPrompt ?? "").Trim();
		string value = "【messages说明】在下面的对话消息里，assistant 只代表你自己过去说过的话；role=user 里的系统事实和规则必须严格遵守。如果 AFEF 事实前有【当下行为】，表示本轮刚刚真实发生；如果前有【过往行为】，只表示历史上已经发生过，不代表玩家本轮又交付了一次。玩家口头声称给了相同财物，必须以本轮是否存在对应【当下行为】AFEF事实为准。如果附加规则和开头的规则冲突，优先遵循开头的规则";
		bool disableBuiltInReplyFormatPrompt = DuelSettings.IsBuiltInSceneReplyFormatPromptDisabled();
		string instruction = "";
		if (!disableBuiltInReplyFormatPrompt)
		{
			TryExtractReplyFormatInstruction(ref text, out instruction);
		}
		if (suppressReplyFormatInstruction)
		{
			instruction = "";
		}
		string text2 = string.IsNullOrWhiteSpace(instruction) ? value : (value + "\n" + instruction);
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		string text3 = BuildPlayerCustomPromptRuleBlock();
		if (!string.IsNullOrWhiteSpace(text3) && text.StartsWith(text3, StringComparison.Ordinal))
		{
			return JoinPromptSections(text3, text2, text.Substring(text3.Length));
		}
		return text2 + "\n" + text;
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
		string text = (GetPlayerDisplayNameForShout() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "玩家" : text;
	}


	private static float GetPlayerDistanceToAgentForScenePrompt(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return -1f;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			return TryGetPlayerPlanarDistanceMeters(agent, out var distanceMeters) ? distanceMeters : (-1f);
		}
		catch
		{
			return -1f;
		}
	}

	private static int ClampMemoryPromptHour(int hour)
	{
		if (hour < 0)
		{
			return MyBehavior.GetCurrentMemoryGameHourForExternal();
		}
		if (hour > 23)
		{
			return 23;
		}
		return hour;
	}

	private static long NextConversationEventSequence()
	{
		return Interlocked.Increment(ref _currentConversationEventSequence);
	}

	private static void FillSceneMessageHeroIdentity(ConversationMessage message)
	{
		if (message == null)
		{
			return;
		}
		message.SpeakerHeroId = NormalizeSceneHeroId(message.SpeakerHeroId);
		if (string.IsNullOrWhiteSpace(message.SpeakerHeroId) && message.SpeakerAgentIndex >= 0)
		{
			message.SpeakerHeroId = ResolveSceneHeroIdFromAgentIndex(message.SpeakerAgentIndex);
		}
		message.TargetHeroId = NormalizeSceneHeroId(message.TargetHeroId);
		if (string.IsNullOrWhiteSpace(message.TargetHeroId) && message.TargetAgentIndex >= 0)
		{
			message.TargetHeroId = ResolveSceneHeroIdFromAgentIndex(message.TargetAgentIndex);
		}
		if (message.VisibleAgentIndices == null)
		{
			message.VisibleAgentIndices = new List<int>();
		}
		List<string> visibleHeroIds = new List<string>();
		HashSet<string> seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (message.VisibleHeroIds != null)
		{
			foreach (string heroId in message.VisibleHeroIds)
			{
				string normalizedHeroId = NormalizeSceneHeroId(heroId);
				if (!string.IsNullOrWhiteSpace(normalizedHeroId) && seenHeroIds.Add(normalizedHeroId))
				{
					visibleHeroIds.Add(normalizedHeroId);
				}
			}
		}
		foreach (int visibleAgentIndex in message.VisibleAgentIndices)
		{
			string visibleHeroId = ResolveSceneHeroIdFromAgentIndex(visibleAgentIndex);
			if (!string.IsNullOrWhiteSpace(visibleHeroId) && seenHeroIds.Add(visibleHeroId))
			{
				visibleHeroIds.Add(visibleHeroId);
			}
		}
		message.VisibleHeroIds = visibleHeroIds;
	}

	private static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage message)
	{
		if (message == null)
		{
			return null;
		}
		if (message.EventSequence <= 0L)
		{
			message.EventSequence = NextConversationEventSequence();
		}
		if (message.GameDayIndex < 0)
		{
			try
			{
				message.GameDayIndex = (int)CampaignTime.Now.ToDays;
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(message.GameDate))
		{
			try
			{
				message.GameDate = CampaignTime.Now.ToString();
			}
			catch
			{
			}
		}
		if (message.GameHour < 0)
		{
			message.GameHour = MyBehavior.GetCurrentMemoryGameHourForExternal();
		}
		if (string.IsNullOrWhiteSpace(message.Scene))
		{
			message.Scene = MyBehavior.ResolveCurrentMemorySceneLabelForExternal();
		}
		FillSceneMessageHeroIdentity(message);
		return message;
	}



	private static bool TryConvertAfefFactToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, bool isCurrent, out object chatMessage)
	{
		chatMessage = null;
		if (msg == null)
		{
			return false;
		}
		string text = NormalizeSceneHistoryPromptLineContent(msg.Content);
		if (string.IsNullOrWhiteSpace(text) || !TryNormalizeAfefFactLineForPrompt(text, out var afefFactLine))
		{
			return false;
		}
		chatMessage = CreateChatMessage("user", PrefixConversationMessageForPrompt(msg, "AFEF", BuildScopedAfefFactLineForPrompt(afefFactLine, isCurrent)));
		return true;
	}









	public static List<string> GetAuxiliarySceneDialogueHistoryLinesForExternal(int targetAgentIndex, int maxLines = 6)
	{
		try
		{
			if (IsNativeConversationInputOpenForExternal())
			{
				List<string> nativeLines = GetNativeConversationAuxiliaryHistoryLinesForExternal(maxLines);
				if (nativeLines != null && nativeLines.Count > 0)
				{
					return nativeLines;
				}
			}
			return CurrentInstance?.GetAuxiliarySceneDialogueHistoryLines(targetAgentIndex, maxLines) ?? new List<string>();
		}
		catch
		{
			return new List<string>();
		}
	}

	private List<string> GetAuxiliarySceneDialogueHistoryLines(int targetAgentIndex, int maxLines) => CaptureAuxiliarySceneDialogueHistoryLines(targetAgentIndex, maxLines);

	private static bool IsAuxiliaryPureDialogueSceneMessage(ConversationMessage msg)
	{
		string text = (msg?.Role ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.Equals("assistant", StringComparison.OrdinalIgnoreCase) || text.Equals("user", StringComparison.OrdinalIgnoreCase);
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
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0)
		{
			return;
		}
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				return;
			}
			RecordExtraFactToSceneHistory(factText, new List<NpcDataPacket> { npcDataPacket });
			if (triggerImmediateReaction)
			{
				TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds, skipSceneFactRecord: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] AppendTargetedScenePlayerFact failed: " + ex.Message);
		}
	}

	private void AppendTargetedSceneNpcFact(string factText, int targetAgentIndex, bool persistHeroPrivateHistory)
	{
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0)
		{
			return;
		}
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				return;
			}
			string text = factText.Replace("\r", " ").Replace("\n", " ").Trim();
			if (!text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal) && !text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
			{
				text = "[AFEF NPC行为补充] " + text;
			}
			RecordExtraFactToSceneHistory(text, new List<NpcDataPacket> { npcDataPacket });
			if (persistHeroPrivateHistory
				&& agent.Character is CharacterObject { HeroObject: not null } characterObject
				&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(characterObject.HeroObject, characterObject, targetAgentIndex))
			{
				MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, text);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] AppendTargetedSceneNpcFact failed: " + ex.Message);
		}
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
		string reason = "combat_start")
	{
		try
		{
			ShoutBehavior instance = CurrentInstance;
			if (instance == null)
			{
				return;
			}
			List<NpcDataPacket> participants = (agentIndices ?? Enumerable.Empty<int>())
				.Where(agentIndex => agentIndex >= 0)
				.Distinct()
				.Select(agentIndex => new NpcDataPacket
				{
					AgentIndex = agentIndex
				})
				.ToList();
			if (participants.Count == 0)
			{
				return;
			}
			instance.ReleaseSceneConversationConstraints(
				participants,
				stopAutoGroupSession: true,
				clearQueuedSpeech: false,
				forceFullAutonomyRelease: true);
			Logger.Log(
				"ShoutBehavior",
				"[SceneCombat] released conversation constraints agents="
				+ string.Join(",", participants.Select(participant => participant.AgentIndex))
				+ " reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log(
				"ShoutBehavior",
				"[SceneCombat] release conversation constraints failed reason="
				+ (reason ?? "")
				+ " error=" + ex.Message);
		}
	}

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






	internal static bool IsAgentHostileToMainAgent(Agent agent)
	{
		try
		{
			Agent main = Agent.Main;
			return agent != null && !agent.IsMainAgent && agent.IsActive() && main != null && main.IsActive() && AreAgentsHostileForSceneConversation(agent, main);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsUsableTeam(Team team)
	{
		try
		{
			return team != null && team != Team.Invalid && team.IsValid;
		}
		catch
		{
			return false;
		}
	}

	private static bool AreTeamsHostileSafely(Team firstTeam, Team secondTeam)
	{
		try
		{
			return IsUsableTeam(firstTeam) && IsUsableTeam(secondTeam) && firstTeam != secondTeam && (firstTeam.IsEnemyOf(secondTeam) || secondTeam.IsEnemyOf(firstTeam));
		}
		catch
		{
			return false;
		}
	}

	private static bool AreAgentsHostileForSceneConversation(Agent a, Agent b)
	{
		if (a == null || b == null || !a.IsActive() || !b.IsActive())
		{
			return false;
		}
		try
		{
			if (a.IsEnemyOf(b) || b.IsEnemyOf(a))
			{
				return true;
			}
		}
		catch
		{
		}
		return AreTeamsHostileSafely(a.Team, b.Team);
	}

	private static bool CanNpcParticipateInAutoGroupRelay(NpcDataPacket participant, Dictionary<int, Hero> resolvedHeroes)
	{
		if (participant == null || participant.AgentIndex < 0)
		{
			return false;
		}
		bool canSpeak = true;
		string statusLine = "";
		bool hasStatus;
		if (participant.IsHero)
		{
			Hero hero = null;
			if (resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(participant.AgentIndex, out hero);
			}
			hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
		}
		else
		{
			hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(participant.UnnamedKey, participant.Name, GetSceneNpcPatienceNameForPrompt(participant), out statusLine, out canSpeak);
		}
		return !hasStatus || canSpeak;
	}

	private static bool IsSceneRelayAudienceEntrySpatiallyEligible(SceneShoutConversationScope scope, SceneShoutAudienceEntry entry, Agent liveAgent)
	{
		if (scope == null || liveAgent == null)
		{
			return false;
		}
		if (entry.IsFramed)
		{
			return true;
		}
		try
		{
			Vec3 position = liveAgent.Position;
			if (entry.IsFromPrimaryAnchor)
			{
				float primaryDistanceSquared = position.DistanceSquared(scope.PrimaryAnchorPosition);
				if (!float.IsNaN(primaryDistanceSquared) && !float.IsInfinity(primaryDistanceSquared) && primaryDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightFromFixedAnchor(scope.PrimaryAnchorPosition, liveAgent))
				{
					return true;
				}
			}
			if (entry.IsFromPlayerAnchor)
			{
				float playerDistanceSquared = position.DistanceSquared(scope.PlayerAnchorPosition);
				if (!float.IsNaN(playerDistanceSquared) && !float.IsInfinity(playerDistanceSquared) && playerDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightFromFixedAnchor(scope.PlayerAnchorPosition, liveAgent))
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

	internal static SceneRelayEligibilitySnapshot BuildSceneRelayEligibilitySnapshot(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch)
	{
		SceneRelayEligibilitySnapshot snapshot = new SceneRelayEligibilitySnapshot();
		if (scope == null || audienceByAgentIndex == null || !scope.IsCurrent(Mission.Current, conversationEpoch))
		{
			return snapshot;
		}
		HashSet<string> patienceLines = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < scope.Entries.Count; i++)
		{
			SceneShoutAudienceEntry entry = scope.Entries[i];
			if (!audienceByAgentIndex.TryGetValue(entry.AgentIndex, out var npc) || npc == null)
			{
				continue;
			}
			Agent liveAgent = entry.AgentReference;
			SceneShoutLiveValidationResult validation = scope.ValidateLiveAgent(Mission.Current, conversationEpoch, entry.AgentIndex, liveAgent, requireActiveSpeaker: true, out var validatedEntry);
			if (validation != SceneShoutLiveValidationResult.Valid || !IsSceneRelayAudienceEntrySpatiallyEligible(scope, validatedEntry, liveAgent))
			{
				continue;
			}
			bool canSpeak = true;
			string statusLine = "";
			bool hasStatus;
			if (npc.IsHero)
			{
				Hero hero = null;
				resolvedHeroes?.TryGetValue(npc.AgentIndex, out hero);
				hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
			}
			else
			{
				hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, GetSceneNpcPatienceNameForPrompt(npc), out statusLine, out canSpeak);
			}
			if (hasStatus && !string.IsNullOrWhiteSpace(statusLine) && patienceLines.Add(statusLine.Trim()))
			{
				snapshot.PatienceStatusLines.Add(statusLine.Trim());
			}
			if (!hasStatus || canSpeak)
			{
				snapshot.Candidates.Add(npc);
			}
		}
		return snapshot;
	}

	internal static NpcDataPacket ResolveLiveSceneRelayTarget(SceneShoutConversationScope scope, Dictionary<int, NpcDataPacket> audienceByAgentIndex, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch, int relayTargetAgentIndex)
	{
		if (relayTargetAgentIndex < 0 || scope == null || audienceByAgentIndex == null || !audienceByAgentIndex.TryGetValue(relayTargetAgentIndex, out var npc) || npc == null)
		{
			return null;
		}
		if (!scope.TryGetEntry(relayTargetAgentIndex, out var entry))
		{
			return null;
		}
		Agent liveAgent = entry.AgentReference;
		if (scope.ValidateLiveAgent(Mission.Current, conversationEpoch, relayTargetAgentIndex, liveAgent, requireActiveSpeaker: true, out var validatedEntry) != SceneShoutLiveValidationResult.Valid || !IsSceneRelayAudienceEntrySpatiallyEligible(scope, validatedEntry, liveAgent))
		{
			return null;
		}
		return CanNpcParticipateInAutoGroupRelay(npc, resolvedHeroes) ? npc : null;
	}

	private int ResolveAutoGroupRelayTargetAgentIndex(int relayTargetAgentIndex, int currentSpeakerAgentIndex, List<NpcDataPacket> participants, Dictionary<int, Hero> resolvedHeroes)
	{
		if (relayTargetAgentIndex < 0 || participants == null || participants.Count == 0)
		{
			return -1;
		}
		NpcDataPacket npcDataPacket = participants.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == relayTargetAgentIndex);
		if (npcDataPacket == null || !CanNpcParticipateInAutoGroupRelay(npcDataPacket, resolvedHeroes))
		{
			return -1;
		}
		return npcDataPacket.AgentIndex;
	}

	private void PrepareAutoGroupParticipantsForIdleTimeout(List<NpcDataPacket> participants, string trailingSpeechText = null, string playerText = null, List<string> npcVisibleTexts = null, int distinctNpcSpeakerCount = 0)
	{
		if (participants == null || participants.Count == 0 || Mission.Current == null)
		{
			return;
		}
		List<NpcDataPacket> list = participants.Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.GroupBy((NpcDataPacket npc) => npc.AgentIndex)
			.Select((IGrouping<int, NpcDataPacket> group) => group.First())
			.ToList();
		if (list.Count == 0)
		{
			return;
		}
		float num = 0f;
		string text = SanitizeSceneSpeechText(trailingSpeechText ?? "");
		if (!string.IsNullOrWhiteSpace(text))
		{
			num = Math.Max(0.25f, EstimateBubbleTypingDurationSeconds(text));
		}
		int num2 = Math.Max(1, list.Count);
		float timeoutSeconds = ResolveDynamicSceneConversationTimeoutSeconds(playerText, npcVisibleTexts, distinctNpcSpeakerCount, num2);
		foreach (NpcDataPacket item in list)
		{
			RefreshActiveInteractionTimeout(item, num2, timeoutSeconds);
			if (!_activeInteractionSessions.TryGetValue(item.AgentIndex, out var value) || value == null)
			{
				continue;
			}
			if (num > 0f)
			{
				ScheduleInteractionTimeoutArm(item.AgentIndex, value.InteractionToken, num);
			}
			else
			{
				ArmActiveInteractionTimeoutNow(item.AgentIndex, value.InteractionToken);
			}
		}
	}


	private void RefreshSceneConversationParticipantInteractions(List<NpcDataPacket> participants, float timeoutSeconds = -1f, ShoutTargetingContext shoutTargetingContext = null)
	{
		if (participants == null || participants.Count == 0 || Mission.Current == null)
		{
			return;
		}
		List<NpcDataPacket> list = participants.Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0)
			.GroupBy((NpcDataPacket npc) => npc.AgentIndex)
			.Select((IGrouping<int, NpcDataPacket> group) => group.First())
			.ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = Math.Max(1, list.Count);
		foreach (NpcDataPacket item in list)
		{
			TrackPlayerInteraction(item, num, timeoutSeconds, false, shoutTargetingContext);
		}
	}



	private void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			DeactivateMultiSceneMovementSuppression();
			return;
		}
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_multiSceneMovementSuppressionAgentIndices.Add(participantAgentIndex);
				}
			}
			_multiSceneMovementSuppressionActive = _multiSceneMovementSuppressionAgentIndices.Count >= 1;
			if (_multiSceneMovementSuppressionActive)
			{
				_multiSceneMovementSuppressionTimer = MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL;
			}
		}
	}

	private void RemoveSceneMovementSuppressionAgents(IEnumerable<int> participantAgentIndices)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_multiSceneMovementSuppressionAgentIndices.Remove(participantAgentIndex);
				}
			}
			_multiSceneMovementSuppressionActive = _multiSceneMovementSuppressionAgentIndices.Count >= 1;
			if (!_multiSceneMovementSuppressionActive)
			{
				_multiSceneMovementSuppressionTimer = 0f;
			}
		}
	}

	private void DeactivateMultiSceneMovementSuppression()
	{
		lock (_multiSceneMovementSuppressionLock)
		{
			_multiSceneMovementSuppressionActive = false;
			_multiSceneMovementSuppressionAgentIndices.Clear();
			_multiSceneMovementSuppressionTimer = 0f;
		}
	}

	private void RequestSceneConversationAttentionRelease(IEnumerable<int> participantAgentIndices)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			foreach (int participantAgentIndex in participantAgentIndices)
			{
				if (participantAgentIndex >= 0)
				{
					_pendingSceneConversationAttentionReleaseAgentIndices.Add(participantAgentIndex);
				}
			}
		}
	}

	private void ClearPendingSceneConversationAttentionRelease()
	{
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			_pendingSceneConversationAttentionReleaseAgentIndices.Clear();
		}
	}

	private void FlushPendingSceneConversationAttentionRelease()
	{
		if (Mission.Current == null || IsSpeechPipelineBusy())
		{
			return;
		}
		List<int> list = null;
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			if (_pendingSceneConversationAttentionReleaseAgentIndices.Count == 0)
			{
				return;
			}
			list = _pendingSceneConversationAttentionReleaseAgentIndices.ToList();
			_pendingSceneConversationAttentionReleaseAgentIndices.Clear();
		}
		ReleaseSceneConversationAttention(list);
	}

	private void UpdateMultiSceneMovementSuppression(float dt)
	{
		List<int> list;
		lock (_multiSceneMovementSuppressionLock)
		{
			if (!_multiSceneMovementSuppressionActive)
			{
				return;
			}
			_multiSceneMovementSuppressionTimer += Math.Max(0f, dt);
			if (_multiSceneMovementSuppressionTimer < MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL)
			{
				return;
			}
			_multiSceneMovementSuppressionTimer = 0f;
			list = _multiSceneMovementSuppressionAgentIndices.ToList();
		}
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		Agent main = Agent.Main;
		if (agents == null || main == null || !main.IsActive())
		{
			DeactivateMultiSceneMovementSuppression();
			return;
		}
		List<int> list2 = null;
		int num = 0;
		foreach (int item in list)
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == item);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				if (list2 == null)
				{
					list2 = new List<int>();
				}
				list2.Add(item);
				continue;
			}
			try
			{
				_activeInteractionSessions.TryGetValue(item, out var interactionSession);
				float playerRange = ResolveActiveInteractionPlayerRangeMeters(interactionSession);
				float num2 = playerRange * playerRange;
				if (agent.Position.AsVec2.DistanceSquared(main.Position.AsVec2) > num2)
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			num++;
			ApplyMultiSceneMovementSuppression(agent);
		}
		if (list2 != null && list2.Count > 0)
		{
			lock (_multiSceneMovementSuppressionLock)
			{
				foreach (int item2 in list2)
				{
					_multiSceneMovementSuppressionAgentIndices.Remove(item2);
				}
				if (_multiSceneMovementSuppressionAgentIndices.Count < 1)
				{
					_multiSceneMovementSuppressionActive = false;
					_multiSceneMovementSuppressionTimer = 0f;
				}
			}
		}
		if (num >= 1)
		{
			_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS);
		}
	}

	private void ApplyMultiSceneMovementSuppression(Agent agent)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(agent))
		{
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		try
		{
			agent.SetLookAgent(Agent.Main);
			agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
			WorldPosition worldPosition = agent.GetWorldPosition();
			agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: false);
		}
		catch
		{
		}
		if (!_staringAgents.Contains(agent))
		{
			_staringAgents.Add(agent);
		}
	}

	internal static bool ShouldSuppressSceneConversationControlForMeeting()
	{
		try
		{
			if (IsMeetingPseudoCombatContext())
			{
				return true;
			}
		}
		catch
		{
		}
		return IsSceneConversationCombatContext();
	}

	private static bool IsSceneConversationCombatContext()
	{
		try
		{
			if (IsSceneConversationMissionEnding())
			{
				return false;
			}
			if (IsMeetingPseudoCombatContext())
			{
				return false;
			}
			if (IsActiveSceneConversationDuelCombat())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MissionFightHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionFightHandler>();
			if (missionBehavior != null && missionBehavior.IsThereActiveFight())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Mission current = Mission.Current;
			Agent main = Agent.Main;
			if (current != null && main != null && main.IsActive() && current.Agents != null)
			{
				MissionMode mode = current.Mode;
				if (mode == MissionMode.Battle || mode == MissionMode.Duel)
				{
					foreach (Agent agent in current.Agents)
					{
						if (agent != null && agent.IsActive() && agent != main && AreAgentsHostileForSceneConversation(agent, main))
						{
							return true;
						}
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			Agent main = Agent.Main;
			if (main != null && main.IsActive() && agents != null)
			{
				foreach (Agent agent in agents)
				{
					if (agent != null && agent.IsActive() && agent != main && AreAgentsHostileForSceneConversation(agent, main))
					{
						return true;
					}
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool ShouldReleaseSceneConversationControlForCombat()
	{
		return IsSceneConversationCombatContext();
	}

	internal static void StripMeetingTauntTagsForSceneConversation(ref string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		try
		{
			content = MeetingSceneShoutTauntTagRegex.Replace(content, "").Trim();
		}
		catch
		{
		}
	}

	private void ClearMeetingSceneConversationControlState()
	{
		if (!ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (ShouldReleaseSceneConversationControlForCombat())
		{
			ReleaseAllSceneConversationControlForCombat();
			return;
		}
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
		_stopStaringTime = 0f;
	}

	private void ReleaseAllSceneConversationControlForCombat()
	{
		HashSet<int> hashSet = new HashSet<int>();
		lock (_multiSceneMovementSuppressionLock)
		{
			foreach (int multiSceneMovementSuppressionAgentIndex in _multiSceneMovementSuppressionAgentIndices)
			{
				if (multiSceneMovementSuppressionAgentIndex >= 0)
				{
					hashSet.Add(multiSceneMovementSuppressionAgentIndex);
				}
			}
		}
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			foreach (int pendingSceneConversationAttentionReleaseAgentIndex in _pendingSceneConversationAttentionReleaseAgentIndices)
			{
				if (pendingSceneConversationAttentionReleaseAgentIndex >= 0)
				{
					hashSet.Add(pendingSceneConversationAttentionReleaseAgentIndex);
				}
			}
		}
		for (int i = 0; i < _staringAgents.Count; i++)
		{
			Agent agent = _staringAgents[i];
			if (agent != null)
			{
				hashSet.Add(agent.Index);
			}
		}
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		if (hashSet.Count > 0)
		{
			ReleaseSceneConversationAttention(hashSet, fullyRestoreAutonomy: true);
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
		_stopStaringTime = 0f;
	}

	internal static bool IsMeetingSceneConversationReleaseSensitive()
	{
		try
		{
			return IsMeetingPseudoCombatContext();
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldPreserveMeetingSceneAutonomy()
	{
		try
		{
			return IsMeetingPseudoCombatContext();
		}
		catch
		{
			return false;
		}
	}

	private void ClearAgentSceneConversationFocus(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			if (_staringUseConversationAgents.Remove(agent.Index) && agent.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationEnd();
			}
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent(null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
	}

	private void ReleaseAgentFromSceneConversationLocks(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ClearAgentSceneConversationFocus(agent);
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	private void ReleaseSceneConversationAttention(IEnumerable<int> participantAgentIndices, bool fullyRestoreAutonomy = true)
	{
		if (participantAgentIndices == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>(participantAgentIndices.Where((int agentIndex) => agentIndex >= 0));
		if (hashSet.Count == 0)
		{
			return;
		}
		List<Agent> list = null;
		for (int num = _staringAgents.Count - 1; num >= 0; num--)
		{
			Agent agent = _staringAgents[num];
			if (agent == null)
			{
				_staringAgents.RemoveAt(num);
				continue;
			}
			if (!hashSet.Contains(agent.Index))
			{
				continue;
			}
			if (list == null)
			{
				list = new List<Agent>();
			}
			list.Add(agent);
			_staringAgents.RemoveAt(num);
			_staringAgentAnchors.Remove(agent.Index);
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (Agent item in list)
		{
			if (fullyRestoreAutonomy)
			{
				RestoreAgentAutonomy(item);
			}
			else
			{
				ReleaseAgentFromSceneConversationLocks(item);
			}
		}
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
	}

	private void ReleaseSceneConversationConstraints(List<NpcDataPacket> participants, int fallbackAgentIndex = -1, bool stopAutoGroupSession = true, bool clearQueuedSpeech = true, bool forceFullAutonomyRelease = false)
	{
		List<int> list = new List<int>();
		if (participants != null)
		{
			foreach (NpcDataPacket participant in participants)
			{
				if (participant != null && participant.AgentIndex >= 0 && !list.Contains(participant.AgentIndex))
				{
					list.Add(participant.AgentIndex);
				}
			}
		}
		if (fallbackAgentIndex >= 0 && !list.Contains(fallbackAgentIndex))
		{
			list.Add(fallbackAgentIndex);
		}
		if (list.Count == 0)
		{
			return;
		}
		if (clearQueuedSpeech)
		{
			ClearQueuedSceneSpeech();
		}
		ClearPendingSceneConversationAttentionRelease();
		RemoveSceneMovementSuppressionAgents(list);
		ReleaseSceneConversationAttention(list, forceFullAutonomyRelease || !IsMeetingSceneConversationReleaseSensitive());
		foreach (int item in list)
		{
			_pendingInteractionTimeoutArms.Remove(item);
			_activeInteractionSessions.Remove(item);
		}
	}



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

	internal static void RefreshHostileCombatAgentAutonomy(Agent agent)
	{
		if (!IsAgentHostileToMainAgent(agent) || !agent.IsAIControlled)
		{
			return;
		}
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent(null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		try
		{
			agent.ResetEnemyCaches();
			agent.InvalidateTargetAgent();
			agent.InvalidateAIWeaponSelections();
		}
		catch
		{
		}
		try
		{
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		catch
		{
		}
		try
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
	}

	private static float GetSceneConversationTimeoutSecondsPerVisibleCharacter()
	{
		float value = 1f;
		try
		{
			value = DuelSettings.GetSettings()?.SceneConversationTimeoutSecondsPerVisibleCharacter ?? 1f;
		}
		catch
		{
			value = 1f;
		}
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = 1f;
		}
		return Math.Max(0.5f, Math.Min(3f, value));
	}

	private static string NormalizeSceneTimeoutVisibleText(string text)
	{
		string text2 = StripNpcNamePrefixSafely(SanitizeSceneSpeechText(text ?? ""), 30);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\[[^\\]\\r\\n]*AFEF[^\\]\\r\\n]*\\][^\\r\\n]*", "", RegexOptions.IgnoreCase);
		string[] array = text2.Replace("\r", "").Split('\n');
		List<string> list = new List<string>();
		foreach (string item in array)
		{
			string text3 = (item ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[AFEF", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("【AFEF", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			list.Add(text3);
		}
		return string.Join("\n", list).Trim();
	}

	private static int CountSceneTimeoutVisibleCharacters(string text)
	{
		string text2 = NormalizeSceneTimeoutVisibleText(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return 0;
		}
		int num = 0;
		foreach (char c in text2)
		{
			if (!char.IsWhiteSpace(c))
			{
				num++;
			}
		}
		return num;
	}

	private static int CountSceneTimeoutVisibleCharacters(IEnumerable<string> texts)
	{
		if (texts == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string text in texts)
		{
			num += CountSceneTimeoutVisibleCharacters(text);
		}
		return num;
	}

	internal static float ResolveDynamicSceneConversationTimeoutSeconds(string playerText, IEnumerable<string> npcVisibleTexts, int distinctNpcSpeakerCount, int participantCount)
	{
		int visibleCharCount = CountSceneTimeoutVisibleCharacters(playerText) + CountSceneTimeoutVisibleCharacters(npcVisibleTexts);
		float secondsPerChar = GetSceneConversationTimeoutSecondsPerVisibleCharacter();
		int speakerCount = Math.Max(0, distinctNpcSpeakerCount);
		bool groupLike = participantCount > 1 || speakerCount > 1;
		float cap = groupLike ? ACTIVE_INTERACTION_DYNAMIC_GROUP_TIMEOUT_CAP : ACTIVE_INTERACTION_DYNAMIC_SINGLE_TIMEOUT_CAP;
		float value = ACTIVE_INTERACTION_IDLE_TIMEOUT + visibleCharCount * secondsPerChar + Math.Max(0, speakerCount - 1) * ACTIVE_INTERACTION_GROUP_SPEAKER_BONUS_SECONDS;
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = ACTIVE_INTERACTION_IDLE_TIMEOUT;
		}
		return Math.Max(ACTIVE_INTERACTION_IDLE_TIMEOUT, Math.Min(cap, value));
	}

	private static float ResolveActiveInteractionTimeoutSeconds(int participantCount, float timeoutSeconds)
	{
		if (timeoutSeconds > 0f)
		{
			return timeoutSeconds;
		}
		return (participantCount > 1) ? ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT : ACTIVE_INTERACTION_IDLE_TIMEOUT;
	}

	private void RefreshActiveInteractionTimeout(NpcDataPacket primaryTarget, int participantCount, float timeoutSeconds)
	{
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || Mission.Current == null || timeoutSeconds <= 0f)
		{
			return;
		}
		if (_activeInteractionSessions.TryGetValue(primaryTarget.AgentIndex, out var value) && value != null)
		{
			value.TimeoutSeconds = ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds);
			return;
		}
		TrackPlayerInteraction(primaryTarget, participantCount, timeoutSeconds);
	}

	private void TrackPlayerInteraction(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false, ShoutTargetingContext shoutTargetingContext = null)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || mission == null)
		{
			return;
		}
		Agent agent = agents?.FirstOrDefault((Agent a) => a != null && a.Index == primaryTarget.AgentIndex && a.IsActive());
		bool flag = _sceneMovement.IsAgentBusyWithSceneGuideErrand(primaryTarget.AgentIndex);
		bool flag2 = flag && _sceneMovement.HasGuideArrivalHold(primaryTarget.AgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(primaryTarget.AgentIndex) || (flag && !flag2))
		{
			_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
			_activeInteractionSessions.Remove(primaryTarget.AgentIndex);
			return;
		}
		if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
			_activeInteractionSessions.Remove(primaryTarget.AgentIndex);
			return;
		}
		string text = (primaryTarget.Name ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		float initialPlayerDistanceMeters = 0f;
		float playerReleaseRangeMeters = ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		if (TryGetSnapshotPlayerDistanceMeters(shoutTargetingContext, primaryTarget.AgentIndex, agent, out initialPlayerDistanceMeters))
		{
			playerReleaseRangeMeters = CalculateDistantActiveInteractionReleaseRange(initialPlayerDistanceMeters);
		}
		_activeInteractionSessions[primaryTarget.AgentIndex] = new SceneInteractionSession
		{
			TargetAgentIndex = primaryTarget.AgentIndex,
			TargetName = text,
			LastActivityTime = mission.CurrentTime,
			TimeoutArmed = false,
			TimeoutSeconds = ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds),
			InteractionToken = DateTime.UtcNow.Ticks,
			ReturnSceneSummonOnTimeout = returnSceneSummonOnTimeout,
			InitialPlayerDistanceMeters = initialPlayerDistanceMeters,
			PlayerReleaseRangeMeters = playerReleaseRangeMeters
		};
		_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
	}

	private void ScheduleInteractionTimeoutArm(int agentIndex, long interactionToken, float speechDurationSeconds)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || interactionToken == 0L || mission == null)
		{
			return;
		}
		float num = mission.CurrentTime + Math.Max(0f, speechDurationSeconds);
		_pendingInteractionTimeoutArms[agentIndex] = new PendingInteractionTimeoutArm
		{
			AgentIndex = agentIndex,
			InteractionToken = interactionToken,
			ArmAtMissionTime = num
		};
	}

	private void ArmActiveInteractionTimeoutNow(int agentIndex, long interactionToken)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		if (!_activeInteractionSessions.TryGetValue(agentIndex, out var value) || value == null || value.InteractionToken != interactionToken)
		{
			return;
		}
		value.LastActivityTime = mission.CurrentTime;
		value.TimeoutArmed = true;
		_pendingInteractionTimeoutArms.Remove(agentIndex);
	}

	private void UpdatePendingSceneDialogueFeeds()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneDialogueFeedQueues.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		lock (_ttsBubbleSyncLock)
		{
			foreach (KeyValuePair<int, Queue<PendingSceneDialogueFeedEntry>> pendingSceneDialogueFeedQueue in _pendingSceneDialogueFeedQueues)
			{
				Queue<PendingSceneDialogueFeedEntry> value = pendingSceneDialogueFeedQueue.Value;
				if (value == null || value.Count == 0)
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(pendingSceneDialogueFeedQueue.Key);
					continue;
				}
				PendingSceneDialogueFeedEntry pendingSceneDialogueFeedEntry = value.Peek();
				if (pendingSceneDialogueFeedEntry != null && !pendingSceneDialogueFeedEntry.WaitForPlaybackFinished && pendingSceneDialogueFeedEntry.ExecuteAtMissionTime >= 0f && currentTime >= pendingSceneDialogueFeedEntry.ExecuteAtMissionTime)
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(pendingSceneDialogueFeedQueue.Key);
				}
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushPendingSceneDialogueFeedAfterSpeech(item);
		}
	}

	private void ProcessPendingInteractionTimeoutArms()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingInteractionTimeoutArms.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingInteractionTimeoutArm> pendingInteractionTimeoutArm in _pendingInteractionTimeoutArms)
		{
			PendingInteractionTimeoutArm value = pendingInteractionTimeoutArm.Value;
			if (value == null || currentTime >= value.ArmAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingInteractionTimeoutArm.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			if (_pendingInteractionTimeoutArms.TryGetValue(item, out var value2))
			{
				if (value2 != null)
				{
					ArmActiveInteractionTimeoutNow(value2.AgentIndex, value2.InteractionToken);
				}
				else
				{
					_pendingInteractionTimeoutArms.Remove(item);
				}
			}
		}
	}





	private void ScheduleMeetingReleaseAfterSpeech(int agentIndex, Hero targetHero, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		_pendingMeetingReleasesAfterSpeech[agentIndex] = new PendingMeetingReleaseAfterSpeech
		{
			AgentIndex = agentIndex,
			TargetHero = targetHero,
			EncounterParty = LordEncounterBehavior.CaptureMeetingReleaseEncounterPartyForExternal(),
			WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished,
			ExecuteAtMissionTime = ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? (-1f) : (mission.CurrentTime + num))
		};
	}

	private void FlushMeetingReleaseAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingMeetingReleasesAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingMeetingReleasesAfterSpeech.Remove(agentIndex);
		LordEncounterBehavior.TryExecuteMeetingPlayerRelease(value.TargetHero, value.EncounterParty, "meeting_release_player_after_speech");
	}

	private void ShowOpenLordsHallResponseAndScheduleEntry(NpcDataPacket npc, Agent agent, List<NpcDataPacket> allNpcData, string content, bool commitHistory, string afterSpeechInfoMessage)
	{
		int agentIndex = npc?.AgentIndex ?? agent?.Index ?? -1;
		string visibleContent = StripActionTagsForSceneSpeech(content);
		string historyText = SanitizeSceneSpeechText(visibleContent);
		string fullHistoryText = PrepareSceneHistorySpeechText(visibleContent);
		SceneSpeechPlaybackInfo playbackInfo = null;
		if (npc != null && CanAgentParticipateInSceneSpeech(agent) && !string.IsNullOrWhiteSpace(historyText))
		{
			playbackInfo = ShowNpcSpeechOutput(npc, agent, historyText, allowTts: true, attachTtsToSceneAgent: true, suppressInteractionTimeoutArm: true);
			if (commitHistory && !string.IsNullOrWhiteSpace(fullHistoryText))
			{
				RecordResponseForAllNearbySafe(allNpcData, npc.AgentIndex, npc.Name, fullHistoryText);
				PersistNpcSpeechToNamedHeroes(npc.AgentIndex, npc.Name, fullHistoryText, allNpcData);
			}
		}
		if (!string.IsNullOrWhiteSpace(afterSpeechInfoMessage))
		{
			try
			{
				InformationManager.DisplayMessage(new InformationMessage(afterSpeechInfoMessage, new Color(1f, 0.95f, 0.25f)));
			}
			catch
			{
			}
		}
		ScheduleLordsHallMissionEntryAfterSpeech(agentIndex, playbackInfo, "scene_tag_priority");
	}

	private void ScheduleLordsHallMissionEntryAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo, string reason, bool waitForConversationEnd = false)
	{
		Mission mission = Mission.Current;
		Settlement settlement = Settlement.CurrentSettlement;
		if (mission == null || settlement == null || !settlement.IsTown)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entry schedule skipped because the town mission is no longer active. reason=" + (reason ?? ""));
			return;
		}
		float delay = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool waitForNativeConversation = waitForConversationEnd && IsNativeConversationActiveForLordsHallEntry();
		bool waitForPlayback = !waitForNativeConversation && agentIndex >= 0 && playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		float fallbackDelay = waitForPlayback ? Math.Max(LORDS_HALL_ENTRY_TTS_FALLBACK_SECONDS, delay + 3f) : delay;
		UnregisterPendingLordsHallMissionEntryConversationEndHook();
		_pendingLordsHallMissionEntryAfterSpeech = new PendingLordsHallMissionEntryAfterSpeech
		{
			Mission = mission,
			AgentIndex = agentIndex,
			SettlementId = settlement.StringId ?? "",
			Reason = reason ?? "",
			WaitForConversationEnd = waitForNativeConversation,
			WaitForPlaybackFinished = waitForPlayback,
			ExecuteAtMissionTime = mission.CurrentTime + fallbackDelay
		};
		if (waitForNativeConversation)
		{
			RegisterPendingLordsHallMissionEntryConversationEndHook();
		}
		Logger.Log("ShoutBehavior", "[LordsHallAccess] entry scheduled after speech. agent=" + agentIndex + " settlement=" + (settlement.StringId ?? "") + " waitForConversationEnd=" + waitForNativeConversation + " waitForPlayback=" + waitForPlayback + " delay=" + delay.ToString("F2") + " fallback=" + fallbackDelay.ToString("F2") + " reason=" + (reason ?? ""));
	}

	private static bool IsNativeConversationActiveForLordsHallEntry()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			return conversationManager != null && (conversationManager.IsConversationInProgress || conversationManager.IsConversationFlowActive);
		}
		catch
		{
			return false;
		}
	}

	private void RegisterPendingLordsHallMissionEntryConversationEndHook()
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager == null)
			{
				return;
			}
			conversationManager.ConversationEndOneShot -= OnPendingLordsHallMissionEntryConversationEnded;
			conversationManager.ConversationEndOneShot += OnPendingLordsHallMissionEntryConversationEnded;
			_pendingLordsHallMissionEntryConversationEndHookRegistered = true;
			Logger.Log("ShoutBehavior", "[LordsHallAccess] registered native conversation-end transition hook.");
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] failed to register native conversation-end transition hook: " + ex.Message);
		}
	}

	private void UnregisterPendingLordsHallMissionEntryConversationEndHook()
	{
		if (!_pendingLordsHallMissionEntryConversationEndHookRegistered)
		{
			return;
		}
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (conversationManager != null)
			{
				conversationManager.ConversationEndOneShot -= OnPendingLordsHallMissionEntryConversationEnded;
			}
		}
		catch
		{
		}
		_pendingLordsHallMissionEntryConversationEndHookRegistered = false;
	}

	private void OnPendingLordsHallMissionEntryConversationEnded()
	{
		_pendingLordsHallMissionEntryConversationEndHookRegistered = false;
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null || !pending.WaitForConversationEnd)
		{
			return;
		}
		Logger.Log("ShoutBehavior", "[LordsHallAccess] native conversation ended; entering lordshall.");
		ExecutePendingLordsHallMissionEntry(pending);
	}

	private bool FlushLordsHallMissionEntryAfterSpeech(int agentIndex)
	{
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null || pending.WaitForConversationEnd || !pending.WaitForPlaybackFinished || pending.AgentIndex != agentIndex)
		{
			return false;
		}
		ExecutePendingLordsHallMissionEntry(pending);
		return true;
	}

	private void ExecutePendingLordsHallMissionEntry(PendingLordsHallMissionEntryAfterSpeech pending)
	{
		if (pending == null || !ReferenceEquals(_pendingLordsHallMissionEntryAfterSpeech, pending))
		{
			return;
		}
		_pendingLordsHallMissionEntryAfterSpeech = null;
		UnregisterPendingLordsHallMissionEntryConversationEndHook();
		try
		{
			Mission mission = Mission.Current;
			Settlement settlement = Settlement.CurrentSettlement;
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (mission == null || !ReferenceEquals(mission, pending.Mission) || settlement == null || !settlement.IsTown || locationComplex == null || currentLocation == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the scene changed. reason=" + (pending.Reason ?? ""));
				return;
			}
			if (!string.Equals(settlement.StringId ?? "", pending.SettlementId ?? "", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(currentLocation.StringId, "center", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the settlement or source location changed. expectedSettlement=" + (pending.SettlementId ?? "") + " actualSettlement=" + (settlement.StringId ?? "") + " location=" + (currentLocation.StringId ?? ""));
				return;
			}
			Location lordsHall = locationComplex.GetLocationWithId("lordshall");
			Location center = locationComplex.GetLocationWithId("center");
			if (lordsHall == null || center == null || Campaign.Current?.GameMenuManager == null)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] entry cancelled because the lordshall transition is unavailable. settlement=" + (settlement.StringId ?? ""));
				return;
			}
			Campaign.Current.GameMenuManager.NextLocation = lordsHall;
			Campaign.Current.GameMenuManager.PreviousLocation = center;
			mission.EndMission();
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entered lordshall from OPEN_LORDS_HALL. settlement=" + (settlement.StringId ?? "") + " agent=" + pending.AgentIndex + " reason=" + (pending.Reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[LordsHallAccess] entry transition failed: " + ex.Message);
		}
	}

	private void ScheduleWorldMapMissionExitAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		float delay = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool waitForPlayback = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		_pendingWorldMapMissionExitsAfterSpeech[agentIndex] = new PendingWorldMapMissionExitAfterSpeech
		{
			AgentIndex = agentIndex,
			WaitForPlaybackFinished = waitForPlayback,
			ExecuteAtMissionTime = waitForPlayback ? -1f : mission.CurrentTime + delay
		};
		Logger.Log("ShoutBehavior", "[WorldMapCommand] scheduled mission exit after speech agent=" + agentIndex + " waitForPlayback=" + waitForPlayback + " delay=" + delay.ToString("F2"));
	}

	private void FlushWorldMapMissionExitAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingWorldMapMissionExitsAfterSpeech.Remove(agentIndex))
		{
			return;
		}
		try
		{
			Mission mission = Mission.Current;
			if (mission != null)
			{
				mission.NextCheckTimeEndMission = 0f;
				mission.EndMission();
				Logger.Log("ShoutBehavior", "[WorldMapCommand] ended mission for implicit party creation agent=" + agentIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WorldMapCommand] mission exit failed agent=" + agentIndex + " error=" + ex.Message);
		}
	}



	private void ScheduleSceneAutonomyRestoreAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null || !_pendingSceneAutonomyRestoresAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (mission.CurrentTime + num);
	}

	private void FlushSceneAutonomyRestoreAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneAutonomyRestoresAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneAutonomyRestoresAfterSpeech.Remove(agentIndex);
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == agentIndex);
		_staringAgentAnchors.Remove(agentIndex);
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
		RemoveSceneMovementSuppressionAgents(new int[1] { agentIndex });
		RestoreAgentAutonomy(agent);
	}



	private void UpdatePendingSceneAutonomyRestoresAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneAutonomyRestoresAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneAutonomyRestoreAfterSpeech> pendingSceneAutonomyRestoresAfterSpeech in _pendingSceneAutonomyRestoresAfterSpeech)
		{
			PendingSceneAutonomyRestoreAfterSpeech value = pendingSceneAutonomyRestoresAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneAutonomyRestoresAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneAutonomyRestoreAfterSpeech(item);
		}
	}


	private void UpdatePendingMeetingReleasesAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingMeetingReleasesAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingMeetingReleaseAfterSpeech> pendingMeetingReleaseAfterSpeech in _pendingMeetingReleasesAfterSpeech)
		{
			PendingMeetingReleaseAfterSpeech value = pendingMeetingReleaseAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingMeetingReleaseAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushMeetingReleaseAfterSpeech(item);
		}
	}

	private void UpdatePendingLordsHallMissionEntryAfterSpeech()
	{
		PendingLordsHallMissionEntryAfterSpeech pending = _pendingLordsHallMissionEntryAfterSpeech;
		if (pending == null)
		{
			return;
		}
		Mission mission = Mission.Current;
		if (mission == null || !ReferenceEquals(mission, pending.Mission))
		{
			_pendingLordsHallMissionEntryAfterSpeech = null;
			UnregisterPendingLordsHallMissionEntryConversationEndHook();
			Logger.Log("ShoutBehavior", "[LordsHallAccess] cleared stale pending entry because the mission changed.");
			return;
		}
		if (pending.WaitForConversationEnd)
		{
			if (IsNativeConversationActiveForLordsHallEntry())
			{
				return;
			}
			Logger.Log("ShoutBehavior", "[LordsHallAccess] native conversation-end hook fallback entering lordshall.");
			ExecutePendingLordsHallMissionEntry(pending);
			return;
		}
		if (pending.ExecuteAtMissionTime >= 0f && mission.CurrentTime >= pending.ExecuteAtMissionTime)
		{
			if (pending.WaitForPlaybackFinished)
			{
				Logger.Log("ShoutBehavior", "[LordsHallAccess] TTS completion fallback entering lordshall. agent=" + pending.AgentIndex);
			}
			ExecutePendingLordsHallMissionEntry(pending);
		}
	}

	private void UpdatePendingWorldMapMissionExitsAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingWorldMapMissionExitsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> ready = _pendingWorldMapMissionExitsAfterSpeech
			.Where(pair => pair.Value != null && !pair.Value.WaitForPlaybackFinished && pair.Value.ExecuteAtMissionTime >= 0f && currentTime >= pair.Value.ExecuteAtMissionTime)
			.Select(pair => pair.Key)
			.ToList();
		foreach (int agentIndex in ready)
		{
			FlushWorldMapMissionExitAfterSpeech(agentIndex);
		}
	}



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

	internal static float EstimateBubbleTypingDurationSeconds(string content)
	{
		int num = Math.Max(0, (content ?? "").Length);
		if (num <= 0)
		{
			return 0f;
		}
		return Math.Max(1f, (float)num * 0.05f);
	}

	private static float ResolveActiveInteractionPlayerRangeMeters(SceneInteractionSession session)
	{
		float range = session?.PlayerReleaseRangeMeters ?? 0f;
		if (float.IsNaN(range) || float.IsInfinity(range) || range < ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		return range;
	}

	private static float CalculateDistantActiveInteractionReleaseRange(float initialPlayerDistanceMeters)
	{
		if (float.IsNaN(initialPlayerDistanceMeters) || float.IsInfinity(initialPlayerDistanceMeters) || initialPlayerDistanceMeters <= ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		float extraRange = Math.Max(ACTIVE_INTERACTION_DISTANT_RELEASE_MIN_EXTRA_RANGE, initialPlayerDistanceMeters * ACTIVE_INTERACTION_DISTANT_RELEASE_EXTRA_RATIO);
		float releaseRange = initialPlayerDistanceMeters + extraRange;
		if (float.IsNaN(releaseRange) || float.IsInfinity(releaseRange) || releaseRange < ACTIVE_INTERACTION_IDLE_PLAYER_RANGE)
		{
			return ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		}
		return releaseRange;
	}

	private static bool TryGetSnapshotPlayerDistanceMeters(ShoutTargetingContext targetingContext, int agentIndex, Agent agent, out float distanceMeters)
	{
		distanceMeters = 0f;
		if (targetingContext == null || agentIndex < 0)
		{
			return false;
		}
		if (targetingContext.CandidateAgentIndices == null || !targetingContext.CandidateAgentIndices.Contains(agentIndex))
		{
			return false;
		}
		if (targetingContext.CandidatePlayerDistancesMeters != null && targetingContext.CandidatePlayerDistancesMeters.TryGetValue(agentIndex, out distanceMeters))
		{
			return !(float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters) || distanceMeters < 0f);
		}
		return TryGetPlayerPlanarDistanceMeters(agent, out distanceMeters);
	}

	private static bool IsPlayerWithinActiveInteractionRange(Agent targetAgent, SceneInteractionSession session = null)
	{
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float playerRange = ResolveActiveInteractionPlayerRangeMeters(session);
			float num = playerRange * playerRange;
			return targetAgent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2) <= num;
		}
		catch
		{
			return false;
		}
	}

	private void UpdateActiveInteractionTimeouts()
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (mission == null || agents == null || _activeInteractionSessions.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		HashSet<int> outOfRangeReleaseAgentIndices = null;
		foreach (KeyValuePair<int, SceneInteractionSession> activeInteractionSession in _activeInteractionSessions)
		{
			SceneInteractionSession value = activeInteractionSession.Value;
			float num = ((value != null && value.TimeoutSeconds > 0f) ? value.TimeoutSeconds : ACTIVE_INTERACTION_IDLE_TIMEOUT);
			if (value == null)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				continue;
			}
			if (!value.TimeoutArmed)
			{
				continue;
			}
			if (_sceneMovement.HasPendingGuideReturn(activeInteractionSession.Key))
			{
				continue;
			}
			if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(activeInteractionSession.Key) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(activeInteractionSession.Key))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			Agent agent = agents.FirstOrDefault(a => a != null && a.Index == activeInteractionSession.Key && a.IsActive());
			if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			if (!IsPlayerWithinActiveInteractionRange(agent, value))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				if (outOfRangeReleaseAgentIndices == null)
				{
					outOfRangeReleaseAgentIndices = new HashSet<int>();
				}
				outOfRangeReleaseAgentIndices.Add(activeInteractionSession.Key);
				continue;
			}
			if (currentTime - value.LastActivityTime >= num)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
			}
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		List<int> groupedTimeoutCandidateIndices = list;
		if (outOfRangeReleaseAgentIndices != null && outOfRangeReleaseAgentIndices.Count > 0)
		{
			groupedTimeoutCandidateIndices = list.Where(agentIndex => !outOfRangeReleaseAgentIndices.Contains(agentIndex)).ToList();
		}
		foreach (int item in list)
		{
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value2))
			{
				if (outOfRangeReleaseAgentIndices != null && outOfRangeReleaseAgentIndices.Contains(item))
				{
					ExpireActiveInteractionSilently(value2, deferSceneSummonReturn: false);
				}
				else
				{
					List<SceneInteractionSession> groupedTimeoutSessions = BuildGroupedExpiredInteractionSessions(value2, groupedTimeoutCandidateIndices);
					if (groupedTimeoutSessions != null && groupedTimeoutSessions.Count > 1)
					{
						foreach (SceneInteractionSession groupedTimeoutSession in groupedTimeoutSessions)
						{
							if (groupedTimeoutSession != null && groupedTimeoutSession.TargetAgentIndex >= 0)
							{
								hashSet.Add(groupedTimeoutSession.TargetAgentIndex);
							}
						}
						ExpireGroupedActiveInteractions(groupedTimeoutSessions);
					}
					else
					{
						ExpireActiveInteraction(value2);
					}
				}
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value3) && (value3 == null || ReferenceEquals(value3, value2) || value3.InteractionToken == value2?.InteractionToken))
			{
				_activeInteractionSessions.Remove(item);
			}
		}
	}

	private List<SceneInteractionSession> BuildGroupedExpiredInteractionSessions(SceneInteractionSession seedSession, List<int> expiredAgentIndices)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (seedSession == null || seedSession.TargetAgentIndex < 0 || expiredAgentIndices == null || expiredAgentIndices.Count < 2 || seedSession.TimeoutSeconds <= 3.5f || agents == null)
		{
			return null;
		}
		Agent agent = agents.FirstOrDefault(a => a != null && a.Index == seedSession.TargetAgentIndex && a.IsActive());
		if (agent == null)
		{
			return null;
		}
		List<SceneInteractionSession> list = new List<SceneInteractionSession> { seedSession };
		for (int i = 0; i < expiredAgentIndices.Count; i++)
		{
			int num = expiredAgentIndices[i];
			if (num < 0 || num == seedSession.TargetAgentIndex || !_activeInteractionSessions.TryGetValue(num, out var value) || value == null || value.TimeoutSeconds <= 3.5f)
			{
				continue;
			}
			if (Math.Abs(value.TimeoutSeconds - seedSession.TimeoutSeconds) > 0.5f || Math.Abs(value.LastActivityTime - seedSession.LastActivityTime) > 1.5f)
			{
				continue;
			}
			Agent agent2 = agents.FirstOrDefault(a => a != null && a.Index == num && a.IsActive());
			if (agent2 == null || agent.Position.DistanceSquared(agent2.Position) > 36f)
			{
				continue;
			}
			list.Add(value);
		}
		return (list.Count > 1) ? list : null;
	}

	private void ExpireGroupedActiveInteractions(List<SceneInteractionSession> sessions)
	{
		Mission mission = Mission.Current;
		if (sessions == null || sessions.Count == 0 || mission == null)
		{
			return;
		}
		List<SceneInteractionSession> list = sessions.Where(s => s != null && s.TargetAgentIndex >= 0).GroupBy(s => s.TargetAgentIndex).Select(group => group.First()).ToList();
		if (list.Count == 0)
		{
			return;
		}
		SceneInteractionSession representativeSession = ChooseGroupedTimeoutRepresentative(list);
		if (representativeSession == null)
		{
			foreach (SceneInteractionSession item in list)
			{
				ExpireActiveInteractionSilently(item, deferSceneSummonReturn: false);
			}
			return;
		}
		SceneSummonConversationSession representativeSummonSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(representativeSession.TargetAgentIndex);
		foreach (SceneInteractionSession item2 in list)
		{
			if (item2 == null || item2.TargetAgentIndex < 0 || item2.TargetAgentIndex == representativeSession.TargetAgentIndex)
			{
				continue;
			}
			bool deferSceneSummonReturn = representativeSummonSession != null && _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(item2.TargetAgentIndex) == representativeSummonSession;
			ExpireActiveInteractionSilently(item2, deferSceneSummonReturn);
			_activeInteractionSessions.Remove(item2.TargetAgentIndex);
			_pendingInteractionTimeoutArms.Remove(item2.TargetAgentIndex);
		}
		if (!TriggerGroupedTimeoutDisperseSpeech(representativeSession, list, representativeSummonSession))
		{
			ExpireActiveInteractionSilently(representativeSession, deferSceneSummonReturn: false);
		}
		_activeInteractionSessions.Remove(representativeSession.TargetAgentIndex);
		_pendingInteractionTimeoutArms.Remove(representativeSession.TargetAgentIndex);
	}

	private SceneInteractionSession ChooseGroupedTimeoutRepresentative(List<SceneInteractionSession> sessions)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (sessions == null || sessions.Count == 0 || agents == null)
		{
			return null;
		}
		Agent main = Agent.Main;
		SceneInteractionSession sceneInteractionSession = null;
		float num = float.MaxValue;
		for (int i = 0; i < sessions.Count; i++)
		{
			SceneInteractionSession sceneInteractionSession2 = sessions[i];
			if (sceneInteractionSession2 == null || sceneInteractionSession2.TargetAgentIndex < 0)
			{
				continue;
			}
			Agent agent = agents.FirstOrDefault(a => a != null && a.Index == sceneInteractionSession2.TargetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(agent) || IsAgentHostileToMainAgent(agent))
			{
				continue;
			}
			float num2 = ((main != null && main.IsActive()) ? main.Position.DistanceSquared(agent.Position) : 0f);
			if (sceneInteractionSession == null || num2 < num)
			{
				sceneInteractionSession = sceneInteractionSession2;
				num = num2;
			}
		}
		return sceneInteractionSession ?? sessions.FirstOrDefault();
	}

	private bool TriggerGroupedTimeoutDisperseSpeech(SceneInteractionSession representativeSession, List<SceneInteractionSession> sessions, SceneSummonConversationSession representativeSummonSession)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (representativeSession == null || representativeSession.TargetAgentIndex < 0 || agents == null)
		{
			return false;
		}
		Agent agent = agents.FirstOrDefault(a => a != null && a.Index == representativeSession.TargetAgentIndex && a.IsActive());
		if (!CanAgentParticipateInSceneSpeech(agent) || IsAgentHostileToMainAgent(agent))
		{
			return false;
		}
		NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
		if (npcDataPacket == null)
		{
			return false;
		}
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		if (sessions != null)
		{
			foreach (SceneInteractionSession session in sessions)
			{
				if (session == null || session.TargetAgentIndex < 0)
				{
					continue;
				}
				Agent agent2 = agents.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
				NpcDataPacket npcDataPacket2 = ShoutUtils.ExtractNpcData(agent2);
				if (npcDataPacket2 != null && !list.Any(x => x != null && x.AgentIndex == npcDataPacket2.AgentIndex))
				{
					list.Add(npcDataPacket2);
				}
			}
		}
		if (!list.Any(x => x != null && x.AgentIndex == npcDataPacket.AgentIndex))
		{
			list.Insert(0, npcDataPacket);
		}
		Action onNoSpeech = delegate
		{
			_sceneMovement.CancelPendingSummonReturn(npcDataPacket.AgentIndex);
			_pendingSceneAutonomyRestoresAfterSpeech.Remove(npcDataPacket.AgentIndex);
			ExpireActiveInteractionSilently(representativeSession, deferSceneSummonReturn: false);
		};
		if (representativeSummonSession != null)
		{
			_sceneMovement.ClearSceneSummonConversationInteractionTimers(representativeSummonSession);
			_sceneMovement.SetPendingSummonReturn(npcDataPacket.AgentIndex, representativeSummonSession, _sceneMovement.ShouldReturnOnlySceneSummonSpeaker(representativeSummonSession, agent));
		}
		else
		{
			_pendingSceneAutonomyRestoresAfterSpeech[npcDataPacket.AgentIndex] = new PendingSceneAutonomyRestoreAfterSpeech
			{
				AgentIndex = npcDataPacket.AgentIndex
			};
		}
		string factText = "[AFEF NPC行为补充] 玩家长时间未继续发言，会话准备自然结束。";
		TriggerImmediateSceneBehaviorReaction(factText, npcDataPacket.AgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false, onNoSpeech);
		return true;
	}

	private void ExpireActiveInteractionSilently(SceneInteractionSession session, bool deferSceneSummonReturn)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
		SceneSummonConversationSession sceneSummonConversationSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (!deferSceneSummonReturn && sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
			_staringAgentAnchors.Remove(session.TargetAgentIndex);
			if (_staringAgents.Count == 0)
			{
				_stopStaringTime = 0f;
			}
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			ReleaseAgentFromSceneConversationLocks(agent);
			return;
		}
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
		_staringAgentAnchors.Remove(session.TargetAgentIndex);
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
		RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
		RestoreAgentAutonomy(agent);
		if (!deferSceneSummonReturn && sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

	private void ExpireActiveInteraction(SceneInteractionSession session)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
		SceneSummonConversationSession sceneSummonConversationSession = _sceneMovement.TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (_sceneMovement.IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || _sceneMovement.IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (_sceneMovement.IsAgentFollowingPlayerBySceneCommand(agent))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (IsAgentHostileToMainAgent(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			RestoreAgentAutonomy(agent);
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
			_staringAgentAnchors.Remove(session.TargetAgentIndex);
			if (_staringAgents.Count == 0)
			{
				_stopStaringTime = 0f;
			}
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			ReleaseAgentFromSceneConversationLocks(agent);
			return;
		}
		string text = (session.TargetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (agent.Name?.ToString() ?? "NPC").Trim();
		}
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		string factText =  text2 +"长时间没有发言，"+text+"决定去干自己的事了。";
		string prefixedFactText = "[AFEF NPC行为补充] " + factText;
		if (session.TimeoutSeconds > 3.5f)
		{
			Action onNoSpeech = delegate
			{
				_sceneMovement.CancelPendingSummonReturn(session.TargetAgentIndex);
				_pendingSceneAutonomyRestoresAfterSpeech.Remove(session.TargetAgentIndex);
				ExpireActiveInteractionSilently(session, deferSceneSummonReturn: false);
			};
			if (sceneSummonConversationSession != null)
			{
				_sceneMovement.ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
				_sceneMovement.SetPendingSummonReturn(session.TargetAgentIndex, sceneSummonConversationSession, _sceneMovement.ShouldReturnOnlySceneSummonSpeaker(sceneSummonConversationSession, agent));
			}
			TriggerImmediateSceneBehaviorReaction(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false, onNoSpeech);
			return;
		}
		AppendTargetedSceneNpcFact(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true);
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
		_staringAgentAnchors.Remove(session.TargetAgentIndex);
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
		RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
		RestoreAgentAutonomy(agent);
		if (sceneSummonConversationSession != null)
		{
			_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

	private void RestoreAgentAutonomy(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_sceneMovement.ClearSceneSummonScriptedBehavior(agent);
		bool flag = IsAgentHostileToMainAgent(agent);
		ClearAgentSceneConversationFocus(agent);
		if (ShouldPreserveMeetingSceneAutonomy())
		{
			return;
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		if (flag)
		{
			RefreshHostileCombatAgentAutonomy(agent);
			return;
		}
		try
		{
			agent.SetWatchState(Agent.WatchState.Patrolling);
		}
		catch
		{
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			(component?.AgentNavigator ?? component?.CreateAgentNavigator())?.ClearTarget();
		}
		catch
		{
		}
	}

	private static bool ShouldPreserveAmbientUseDuringStare(Agent agent)
	{
		object obj = agent?.CurrentlyUsedGameObject;
		if (obj == null)
		{
			return false;
		}
		if (obj is PlayMusicPoint)
		{
			return true;
		}
		try
		{
			string text = obj.GetType().Name ?? "";
			if (agent.CurrentlyUsedGameObject != null)
			{
				WeakGameEntity gameEntity = agent.CurrentlyUsedGameObject.GameEntity;
				if (gameEntity.IsValid)
				{
					text = text + " " + gameEntity.Name;
				}
			}
			text = text.ToLowerInvariant();
			return text.Contains("dance") || text.Contains("dancing") || text.Contains("music") || text.Contains("musician") || text.Contains("instrument");
		}
		catch
		{
			return false;
		}
	}

	private void TryInterruptAgentSceneUseForStare(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			if (!agent.IsUsingGameObject && agent.CurrentlyUsedGameObject == null)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		if (ShouldPreserveAmbientUseDuringStare(agent))
		{
			return;
		}
		try
		{
			if (agent.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationStart();
				_staringUseConversationAgents.Add(agent.Index);
			}
		}
		catch
		{
		}
	}

	private static string GetAgentDebugLabel(Agent agent)
	{
		if (agent == null)
		{
			return "null";
		}
		string text = (agent.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		return text + "#" + agent.Index;
	}

	private void TraceStareDebug(string message)
	{
		try
		{
			Logger.Log("ShoutBehavior", "[STARE] " + message);
		}
		catch
		{
		}
	}

	private void FreezeAgentForStare(Agent agent, bool trace = false)
	{
		if (agent == null || !agent.IsActive())
		{
			if (trace)
			{
				TraceStareDebug("Freeze skipped: invalid agent");
			}
			return;
		}
		try
		{
			agent.SetLookAgent(Agent.Main);
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetLookAgent failed for " + GetAgentDebugLabel(agent));
			}
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetIsAIPaused(false) failed for " + GetAgentDebugLabel(agent));
			}
		}
		float? rotation = null;
		try
		{
			if (Agent.Main != null && Agent.Main.IsActive())
			{
				Vec3 vec = Agent.Main.Position - agent.Position;
				if (vec.AsVec2.LengthSquared > 0.0001f)
				{
					rotation = vec.AsVec2.RotationInRadians;
				}
			}
		}
		catch
		{
			rotation = null;
		}
		try
		{
			var scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			Vec3 anchorPosition = agent.Position;
			if (_staringAgentAnchors.TryGetValue(agent.Index, out var storedAnchor))
			{
				anchorPosition = storedAnchor;
			}
			if (agentNavigator != null && rotation.HasValue)
			{
				WorldPosition worldPosition = new WorldPosition(scene, anchorPosition);
				agentNavigator.SetTargetFrame(worldPosition, rotation.Value, 0.05f, 0.8f, Agent.AIScriptedFrameFlags.DoNotRun | Agent.AIScriptedFrameFlags.NoAttack, false);
			}
			else
			{
				WorldPosition worldPosition2 = new WorldPosition(scene, anchorPosition);
				agent.SetScriptedPosition(ref worldPosition2, addHumanLikeDelay: false);
			}
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetTargetFrame/SetScriptedPosition failed for " + GetAgentDebugLabel(agent));
			}
		}
		if (trace)
		{
			TraceStareDebug("Freeze applied to " + GetAgentDebugLabel(agent) + " vel2=" + agent.Velocity.AsVec2.LengthSquared + " rotation=" + (rotation.HasValue ? rotation.Value.ToString("F3") : "null"));
		}
	}

	private void ForceAgentFacePlayer(Agent agent)
	{
		var scene = Mission.Current?.Scene;
		if (agent == null || !agent.IsActive() || scene == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		try
		{
			Vec3 position = agent.Position;
			Vec3 position2 = Agent.Main.Position;
			try
			{
				if (agent.AgentVisuals != null)
				{
					position.z = agent.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			try
			{
				if (Agent.Main.AgentVisuals != null)
				{
					position2.z = Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			Vec3 vec = position2 - position;
			if (vec.LengthSquared < 0.0001f)
			{
				return;
			}
			try
			{
				if (Agent.Main.AgentVisuals != null)
				{
					agent.SetLookToPointOfInterest(Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true));
				}
			}
			catch
			{
			}
			try
			{
				agent.AgentVisuals?.GetSkeleton()?.ForceUpdateBoneFrames();
			}
			catch
			{
			}
			agent.LookDirection = vec.NormalizedCopy();
			if (IsAgentNearlyStationary(agent))
			{
				Vec3 vec2 = agent.Position;
				if (_staringAgentAnchors.TryGetValue(agent.Index, out var value))
				{
					vec2 = value;
				}
				WorldPosition scriptedPosition = new WorldPosition(scene, vec2);
				agent.SetScriptedPositionAndDirection(ref scriptedPosition, vec.AsVec2.RotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
			}
		}
		catch
		{
		}
	}

	private static bool IsAgentNearlyStationary(Agent agent)
	{
		try
		{
			return agent != null && agent.Velocity.AsVec2.LengthSquared <= 0.01f;
		}
		catch
		{
			return false;
		}
	}

	private void ResetStaringForActiveInteraction(List<Agent> nearbyAgents, Agent primaryTarget)
	{
		List<Agent> list = nearbyAgents ?? new List<Agent>();
		List<Agent> passiveCooldownGroupAgents = ((primaryTarget != null) ? GetPassiveCooldownGroupAgents(primaryTarget) : list);
		List<NpcDataPacket> list2 = (from a in passiveCooldownGroupAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		ApplyInteractionGraceAndGroupCooldown(PASSIVE_INTERACTION_GRACE, ACTIVE_CHAT_COOLDOWN, passiveCooldownGroupAgents, primaryTarget, list2);
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			ClearMeetingSceneConversationControlState();
			return;
		}
		ResetStaringBehavior();
		Mission mission = Mission.Current;
		if (mission == null)
		{
			TraceStareDebug("ResetStaringForActiveInteraction aborted: Mission.Current null");
			return;
		}
		TraceStareDebug("ResetStaringForActiveInteraction primary=" + GetAgentDebugLabel(primaryTarget) + " nearby=" + list.Count);
		_stopStaringTime = mission.CurrentTime + 20f;
		foreach (Agent item in list)
		{
			AddAgentToStareList(item, primaryTarget != null && item.Index == primaryTarget.Index);
		}
	}

	private void ExtendStaringHoldForPlayerDrivenSceneRound(int participantCount)
	{
		Mission mission = Mission.Current;
		if (participantCount <= 1 || mission == null)
		{
			return;
		}
		_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS);
	}



	internal static string ApplyCompactPromptTemplate(string template, string key, string value)
	{
		return (template ?? string.Empty).Replace("{" + key + "}", value ?? string.Empty);
	}

	private static string BuildPlayerMarriageFactForNpcListLine(Hero npcHero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (npcHero == null || mainHero == null || npcHero == mainHero)
			{
				return "";
			}
			if (npcHero.Spouse == mainHero || mainHero.Spouse == npcHero)
			{
				string text = (npcHero.IsFemale ? "丈夫" : "妻子");
				string text2 = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "玩家";
				}
				return " | 与" + text2 + "的关系: 配偶（" + text2 + "是其" + text + "）";
			}
		}
		catch
		{
		}
		return "";
	}

	private string BuildPersistedHeroHistoryContext(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			Hero hero = null;
			if (resolvedHeroes != null) resolvedHeroes.TryGetValue(agentIndex, out hero);
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			CharacterObject character = agent?.Character as CharacterObject;
			if (!AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(hero, character, agentIndex))
			{
				return "";
			}
			if (hero == null)
			{
				NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
				string nonHeroSecondaryInput = GetLatestSceneNpcUtterance(agentIndex);
				return BuildWildernessNonHeroHistoryContextForPrompt(npc, null, character, agentIndex, currentInput, nonHeroSecondaryInput);
			}
			string secondaryInput = GetLatestSceneNpcUtterance(agentIndex);
			string text = MyBehavior.BuildHistoryContextForExternal(hero, 0, currentInput, secondaryInput);
			return (text ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private string GetOrBuildPrecomputedPersistedHistoryContext(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts)
	{
		try
		{
			Stopwatch sw = Stopwatch.StartNew();
			if (agentIndex < 0)
			{
				return "";
			}
			if (precomputedContexts != null)
			{
				if (!precomputedContexts.TryGetValue(agentIndex, out var value) || value == null)
				{
					value = new PrecomputedShoutRagContext();
					precomputedContexts[agentIndex] = value;
				}
				if (value.HasPersistedHistoryContext)
				{
					string cached = (value.PersistedHistoryContext ?? "").Trim();
					sw.Stop();
					Logger.Log("Logic", "[MemoryPerf] persisted_history_cache_hit agent=" + agentIndex + " chars=" + cached.Length + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
					return cached;
				}
				value.PersistedHistoryContext = BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes);
				value.HasPersistedHistoryContext = !string.IsNullOrWhiteSpace(value.PersistedHistoryContext);
				string built = (value.PersistedHistoryContext ?? "").Trim();
				sw.Stop();
				Logger.Log("Logic", "[MemoryPerf] persisted_history_cache_miss agent=" + agentIndex + " chars=" + built.Length + " hasValue=" + value.HasPersistedHistoryContext + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
				return built;
			}
			string direct = BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes);
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] persisted_history_direct agent=" + agentIndex + " chars=" + ((direct ?? "").Trim().Length) + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return direct;
		}
		catch
		{
			return "";
		}
	}

	private Task<string> StartPrecomputedPersistedHistoryContextTask(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string reason)
	{
		try
		{
			if (agentIndex < 0)
			{
				return Task.FromResult("");
			}
			if (precomputedContexts != null)
			{
				PrecomputedShoutRagContext value = null;
				lock (precomputedContexts)
				{
					if (!precomputedContexts.TryGetValue(agentIndex, out value) || value == null)
					{
						value = new PrecomputedShoutRagContext();
						precomputedContexts[agentIndex] = value;
					}
					if (value.HasPersistedHistoryContext)
					{
						string cached = (value.PersistedHistoryContext ?? "").Trim();
						Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=cached chars=" + cached.Length);
						return Task.FromResult(cached);
					}
					if (value.PersistedHistoryContextTask != null)
					{
						Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=reuse_task");
						return value.PersistedHistoryContextTask;
					}
					value.PersistedHistoryContextTask = Task.Run(() => (BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes) ?? "").Trim());
					Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=task");
					return value.PersistedHistoryContextTask;
				}
			}
			Logger.Log("Logic", "[MemoryPerf] parallel_history_start reason=" + (reason ?? "") + " agent=" + agentIndex + " mode=direct_task");
			return Task.Run(() => (BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes) ?? "").Trim());
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[MemoryPerf] parallel_history_start_failed reason=" + (reason ?? "") + " agent=" + agentIndex + " error=" + ex.Message);
			return Task.FromResult("");
		}
	}

	private async Task<string> AwaitPrecomputedPersistedHistoryContextAsync(int agentIndex, string currentInput, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Task<string> startedTask, string reason)
	{
		if (startedTask == null)
		{
			return GetOrBuildPrecomputedPersistedHistoryContext(agentIndex, currentInput, resolvedHeroes, precomputedContexts);
		}
		Stopwatch sw = Stopwatch.StartNew();
		try
		{
			string built = ((await startedTask) ?? "").Trim();
			if (precomputedContexts != null && agentIndex >= 0)
			{
				lock (precomputedContexts)
				{
					PrecomputedShoutRagContext value = null;
					if (!precomputedContexts.TryGetValue(agentIndex, out value) || value == null)
					{
						value = new PrecomputedShoutRagContext();
						precomputedContexts[agentIndex] = value;
					}
					value.PersistedHistoryContext = built;
					value.HasPersistedHistoryContext = !string.IsNullOrWhiteSpace(built);
					if (object.ReferenceEquals(value.PersistedHistoryContextTask, startedTask))
					{
						value.PersistedHistoryContextTask = null;
					}
				}
			}
			sw.Stop();
			Logger.Log("Logic", "[MemoryPerf] parallel_history_join reason=" + (reason ?? "") + " agent=" + agentIndex + " chars=" + built.Length + " hasValue=" + !string.IsNullOrWhiteSpace(built) + " waitMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2));
			return built;
		}
		catch (Exception ex)
		{
			sw.Stop();
			if (precomputedContexts != null && agentIndex >= 0)
			{
				try
				{
					lock (precomputedContexts)
					{
						if (precomputedContexts.TryGetValue(agentIndex, out var value) && value != null && object.ReferenceEquals(value.PersistedHistoryContextTask, startedTask))
						{
							value.PersistedHistoryContextTask = null;
						}
					}
				}
				catch
				{
				}
			}
			Logger.Log("Logic", "[MemoryPerf] parallel_history_join_failed reason=" + (reason ?? "") + " agent=" + agentIndex + " waitMs=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " error=" + ex.Message);
			return "";
		}
	}

	private bool PersistPlayerMessageToNamedHeroes(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex, string primaryTargetName, Dictionary<int, Agent> audienceAgentsByIndex, bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(text) || nearbyData == null || nearbyData.Count == 0)
		{
			return true;
		}
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum != null && nearbyDatum.IsHero)
			{
				Agent agent = null;
				if (audienceAgentsByIndex == null || !audienceAgentsByIndex.TryGetValue(nearbyDatum.AgentIndex, out agent))
				{
					agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
				}
				if (agent != null
					&& agent.Character is CharacterObject co
					&& co.HeroObject != null
					&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(co.HeroObject, co, nearbyDatum.AgentIndex))
				{
					if (requireMemoryReceipt)
						accepted &= MyBehavior.CommitDialogueHistoryWithScene(co.HeroObject.StringId, false, co.HeroObject.Name?.ToString(), text, null, null, _sceneHistorySessionId, primaryTargetAgentIndex, primaryTargetName)?.HistoryWritten == true;
					else
						MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, text, null, null, _sceneHistorySessionId, primaryTargetAgentIndex, primaryTargetName);
				}
			}
			else if (nearbyDatum != null && TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
			{
				if (requireMemoryReceipt)
					accepted &= MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, text, null, null, _sceneHistorySessionId, primaryTargetAgentIndex, primaryTargetName)?.HistoryWritten == true;
				else
					MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, text, null, null, _sceneHistorySessionId, primaryTargetAgentIndex, primaryTargetName);
			}
		}
		return accepted;
	}

	private bool PersistNpcSpeechToNamedHeroes(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData, bool requireMemoryReceipt = false)
	{
		if (string.IsNullOrWhiteSpace(response) || nearbyData == null || nearbyData.Count == 0)
		{
			return true;
		}
		string text = ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool accepted = true;
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum == null || !nearbyDatum.IsHero)
			{
				if (nearbyDatum != null && TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
				{
					string aiText = nearbyDatum.AgentIndex == speakerAgentIndex ? response : "[场景喊话] " + text + ": " + response;
					if (requireMemoryReceipt)
						accepted &= MyBehavior.CommitDialogueHistoryWithScene(memoryId, true, memoryName, null, aiText, null, _sceneHistorySessionId)?.HistoryWritten == true;
					else
						MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, null, aiText, null, _sceneHistorySessionId);
				}
				continue;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
			if (agent != null
				&& agent.Character is CharacterObject co
				&& co.HeroObject != null
				&& AfGcczShoutBridge.ShouldUsePersistentPersonalMemory(co.HeroObject, co, nearbyDatum.AgentIndex))
			{
				string aiText = nearbyDatum.AgentIndex == speakerAgentIndex ? response : "[场景喊话] " + text + ": " + response;
				if (requireMemoryReceipt)
					accepted &= MyBehavior.CommitDialogueHistoryWithScene(co.HeroObject.StringId, false, co.HeroObject.Name?.ToString(), null, aiText, null, _sceneHistorySessionId)?.HistoryWritten == true;
				else
					MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, null, aiText, null, _sceneHistorySessionId);
			}
		}
		return accepted;
	}

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
			return Volatile.Read(ref _sceneHistorySessionId);
		}
		catch
		{
			return -1;
		}
	}

	public void UpdateStaringBehavior()
	{
		Mission mission = Mission.Current;
		if (_staringAgents.Count <= 0 || mission == null)
		{
			return;
		}
		for (int i = _staringAgents.Count - 1; i >= 0; i--)
		{
			Agent controlledSpeaker = _staringAgents[i];
			if (!BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(controlledSpeaker))
			{
				continue;
			}
			_staringAgents.RemoveAt(i);
			_staringAgentAnchors.Remove(controlledSpeaker.Index);
			_staringUseConversationAgents.Remove(controlledSpeaker.Index);
			try
			{
				controlledSpeaker.SetLookAgent(null);
				controlledSpeaker.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			}
			catch
			{
			}
		}
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
			return;
		}
		float currentTime = mission.CurrentTime;
		if (currentTime < _stopStaringTime)
		{
			foreach (Agent staringAgent in _staringAgents)
			{
				if (staringAgent != null && staringAgent.IsActive())
				{
					try
					{
						staringAgent.SetLookAgent(Agent.Main);
						staringAgent.SetMaximumSpeedLimit(0f, isMultiplier: false);
					}
					catch
					{
					}
				}
			}
			return;
		}
		ResetStaringBehavior();
	}

	private void ResetStaringBehavior()
	{
		foreach (Agent staringAgent in _staringAgents)
		{
			if (staringAgent != null && staringAgent.IsActive())
			{
				try
				{
					staringAgent.SetLookAgent(null);
					staringAgent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
					if (!ShouldPreserveMeetingSceneAutonomy())
					{
						staringAgent.DisableScriptedMovement();
					}
				}
				catch
				{
				}
			}
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
	}

	private void AddAgentToStareList(Agent agent, bool interruptCurrentUse = false)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			return;
		}
		if (BattleSpeechRuntimeHost.IsClaimedSpeechSpeaker(agent))
		{
			try
			{
				agent?.SetLookAgent(null);
			}
			catch
			{
			}
			return;
		}
		if (agent != null)
		{
			Mission mission = Mission.Current;
			if (mission != null)
			{
				_stopStaringTime = Math.Max(_stopStaringTime, mission.CurrentTime + 20f);
			}
			if (interruptCurrentUse)
			{
				TryInterruptAgentSceneUseForStare(agent);
			}
			try
			{
				agent.SetLookAgent(Agent.Main);
				agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: false);
			}
			catch
			{
			}
			if (!_staringAgents.Contains(agent))
			{
				_staringAgents.Add(agent);
			}
		}
	}

	private void PauseGame()
	{
		Mission mission = Mission.Current;
		if (mission?.Scene != null)
		{
			mission.Scene.TimeSpeed = 0.0001f;
		}
		PauseTtsForShoutUi();
	}

	private void ResumeGame()
	{
		Mission mission = Mission.Current;
		if (mission?.Scene != null)
		{
			mission.Scene.TimeSpeed = 1f;
		}
		ResumeTtsAfterShoutUi();
		EndShoutProcessing("resume_game");
	}

	private void OnShoutCancelled()
	{
		DeactivateMultiSceneMovementSuppression();
		ResumeGame();
	}
}
