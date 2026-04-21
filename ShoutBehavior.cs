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
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace AnimusForge;

public class ShoutBehavior : CampaignBehaviorBase
{
	private enum ShoutChatMode
	{
		Normal,
		Give,
		Show,
		GiveTroops,
		GivePrisoners
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

	private sealed class DeferredCleanupItem
	{
		public long ItemId;

		public SoundEvent SoundEvent;

		public string WavPath;

		public string XmlPath;

		public long EnqueuedUtcTicks;

		public string Source;

		public int AgentIndex;
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
	}

	private sealed class PendingInteractionTimeoutArm
	{
		public int AgentIndex;

		public long InteractionToken;

		public float ArmAtMissionTime;
	}

	private sealed class PendingSceneSummonReturnAfterSpeech
	{
		public int AgentIndex;

		public SceneSummonConversationSession Session;

		public bool ReturnOnlySpeaker;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PendingSceneFollowCommand
	{
		public int AgentIndex;

		public bool StartFollow;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PendingSceneGuideReturnAfterSpeech
	{
		public int AgentIndex;

		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PendingSceneAutonomyRestoreAfterSpeech
	{
		public int AgentIndex;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	private sealed class PrecomputedShoutRagContext
	{
		public bool HasLoreContext;

		public string LoreContext;

		public bool HasPersistedHistoryContext;

		public string PersistedHistoryContext;
	}

	private sealed class SceneSpeechQueueItem
	{
		public NpcDataPacket Npc;

		public string Content;

		public List<NpcDataPacket> ContextSnapshot;

		public List<SceneSummonPromptTarget> SceneSummonTargets;

		public List<SceneGuidePromptTarget> SceneGuideTargets;

		public bool CommitHistory;

		public bool SuppressStare;

		public bool AllowPlayerDirectedActions = true;

		public int RequiredConversationEpoch;
	}

	private sealed class SceneSummonPromptTarget
	{
		public int PromptId;

		public string DisplayName;

		public string LocationCode;

		public LocationCharacter LocationCharacter;

		public Location SourceLocation;
	}

	private sealed class SceneGuidePromptTarget
	{
		public int PromptId;

		public string DisplayName;

		public string LocationCode;

		public LocationCharacter LocationCharacter;

		public Location SourceLocation;
	}

	private enum SceneGuideStage
	{
		Guiding
	}

	private sealed class ActiveSceneGuideRequest
	{
		public int GuideAgentIndex = -1;

		public string GuideName;

		public string TargetName;

		public int TargetPromptId = -1;

		public LocationCharacter GuideLocationCharacter;

		public LocationCharacter TargetLocationCharacter;

		public Location CurrentLocation;

		public Location OriginalGuideLocation;

		public Vec3? OriginalGuidePosition;

		public Location TargetSourceLocation;

		public Passage TargetDoorPassage;

		public SceneGuideStage Stage = SceneGuideStage.Guiding;

		public float NextStageMissionTime;

		public float PlayerOutOfRangeSinceMissionTime = -1f;

		public bool EscortStarted;
	}

	private sealed class ActiveSceneSummonRequest
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public int TargetPromptId;

		public string TargetName;

		public LocationCharacter SpeakerLocationCharacter;

		public LocationCharacter TargetLocationCharacter;

		public Location CurrentLocation;

		public Location OriginalSpeakerLocation;

		public Location OriginalTargetLocation;

		public Location TargetSourceLocation;

		public Location PassageHopLocation;

		public Vec3? OriginalSpeakerPosition;

		public Vec3? OriginalTargetPosition;

		public Passage MessengerDoorPassage;

		public int DoorProxyAgentIndex = -1;

		public float NextStageMissionTime;

		public float ArrivalSpeechDeadlineMissionTime;

		public string PreGeneratedArrivalSpeech;

		public bool ArrivalSpeechConsumed;

		public SceneSummonStage Stage;

		public SceneSummonStage PendingLaunchStage;

		public string LaunchAnnouncement;

		public bool KeepMessengerWithTarget = true;

		public bool BatchContinuationStarted;
	}

	private sealed class SceneSpeechPlaybackInfo
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

	private sealed class SceneSummonConversationParticipant
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;
	}

	private sealed class SceneSummonConversationSession
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public LocationCharacter SpeakerLocationCharacter;

		public Location OriginalSpeakerLocation;

		public Vec3? OriginalSpeakerPosition;

		public bool KeepSpeakerNearby = true;

		public List<SceneSummonConversationParticipant> Participants = new List<SceneSummonConversationParticipant>();
	}

	private sealed class SceneSummonBatchState
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public LocationCharacter SpeakerLocationCharacter;

		public Location OriginalSpeakerLocation;

		public Vec3? OriginalSpeakerPosition;

		public Queue<SceneSummonPromptTarget> PendingTargets = new Queue<SceneSummonPromptTarget>();

		public bool CompletionFactRecorded;
	}

	private sealed class SceneReturnJob
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location CurrentLocation;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;

		public Passage ExitPassage;

		public int DoorProxyAgentIndex = -1;
	}

	private sealed class SceneFollowReturnState
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;
	}

	private enum SceneSummonStage
	{
		PendingLaunch,
		MessengerToTarget,
		MessengerToDoor,
		WaitingForTarget,
		TargetToPlayer
	}

	private class ShoutMissionBehavior : MissionBehavior
	{
		private ShoutBehavior _parent;

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public ShoutMissionBehavior(ShoutBehavior parent)
		{
			_parent = parent;
		}

		public override void OnMissionTick(float dt)
		{
			Action result;
			while (_parent._mainThreadActions.TryDequeue(out result))
			{
				try
				{
					result();
				}
				catch (Exception ex)
				{
					BannerlordExceptionSentinel.ReportObservedException("LipSync.MainThreadAction", ex, "behavior=ShoutMissionBehavior");
				}
			}
			try
			{
				_parent.ProcessDeferredCleanup();
			}
			catch (Exception ex2)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.ProcessDeferredCleanup", ex2, "behavior=ShoutMissionBehavior");
			}
			try
			{
				_parent.TickLipSyncAnimations(dt);
			}
			catch (Exception ex3)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.TickLipSyncAnimations", ex3, "behavior=ShoutMissionBehavior");
			}
			bool flag = ShoutBehavior.ShouldSuppressSceneConversationControlForMeeting();
			if (flag)
			{
				_parent.ClearMeetingSceneConversationControlState();
			}
			else
			{
				_parent.UpdateMultiSceneMovementSuppression(dt);
			}
			if (Campaign.Current != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
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
			_parent.UpdatePendingSceneSummonReturnsAfterSpeech();
			_parent.UpdatePendingSceneGuideReturnsAfterSpeech();
			_parent.UpdatePendingSceneAutonomyRestoresAfterSpeech();
			_parent.UpdatePendingSceneFollowCommands();
			_parent.UpdateSceneSummonConversationEscortMovement();
			_parent.UpdateSceneFollowHostilityState();
			_parent.UpdateSceneFollowSpacing();
			_parent.UpdateActiveSceneSummonRequests();
			_parent.UpdateActiveSceneGuideRequests();
			_parent.UpdateActiveSceneReturnJobs();
			if (_parent._interactionGraceTimer > 0f)
			{
				_parent._interactionGraceTimer -= dt;
			}
			_parent._tickTimer += dt;
			if (!flag && _parent._tickTimer >= 0.2f)
			{
				_parent.UpdatePassiveStareLogic(_parent._tickTimer);
				_parent._tickTimer = 0f;
			}
			else if (flag && _parent._tickTimer >= 0.2f)
			{
				_parent._tickTimer = 0f;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			InputKey key = InputKey.K;
			if (!string.IsNullOrEmpty(settings.ShoutKey) && Enum.TryParse<InputKey>(settings.ShoutKey.ToUpper(), out var result2))
			{
				key = result2;
			}
			bool flag2 = true;
			try
			{
				flag2 = IsGameWindowFocused();
			}
			catch
			{
				flag2 = true;
			}
			if (_parent._wasGameWindowFocused && !flag2)
			{
				try
				{
					_parent.HandleEscapePressedForAudioSafety("FOCUS_LOST");
				}
				catch
				{
				}
			}
			_parent._wasGameWindowFocused = flag2;
			if (Input.IsKeyPressed(InputKey.Escape))
			{
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
			if (!HotkeyInputGuard.IsTextInputFocused() && Input.IsKeyPressed(key))
			{
				if (_parent._isProcessingShout)
				{
					InformationManager.DisplayMessage(new InformationMessage("正在处理中...", new Color(1f, 1f, 0f)));
				}
				else if (ShoutUtils.IsInValidScene())
				{
					_parent._isProcessingShout = true;
					_parent.TriggerShout();
				}
			}
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
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

	private readonly object _historyLock = new object();

	private readonly object _speechQueueLock = new object();

	private readonly Queue<SceneSpeechQueueItem> _speechQueue = new Queue<SceneSpeechQueueItem>();

	private bool _speechWorkerRunning = false;

	private bool _lastShoutDuelLiteralHit = false;

	private List<ShoutTradeResourceOption> _shoutTradeOptions = new List<ShoutTradeResourceOption>();

	private List<ShoutPendingTradeItem> _shoutPendingTradeItems = new List<ShoutPendingTradeItem>();

	private int _shoutPendingTradeItemIndex = 0;

	private ShoutChatMode _shoutTradeMode = ShoutChatMode.Normal;

	private NpcDataPacket _shoutTradeTargetNpc = null;

	private readonly Dictionary<string, ScenePrepaidTransferRecord> _scenePrepaidTransfers = new Dictionary<string, ScenePrepaidTransferRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _sceneHeroRevisitDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _sceneHeroRevisitDayStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _sceneHeroRevisitHandledThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private List<Agent> _staringAgents = new List<Agent>();

	private readonly Dictionary<int, Vec3> _staringAgentAnchors = new Dictionary<int, Vec3>();

	private readonly HashSet<int> _staringUseConversationAgents = new HashSet<int>();

	private readonly object _pendingSceneConversationAttentionReleaseLock = new object();

	private readonly HashSet<int> _pendingSceneConversationAttentionReleaseAgentIndices = new HashSet<int>();

	private float _stopStaringTime = 0f;

	private Dictionary<int, List<ConversationMessage>> _npcConversationHistory = new Dictionary<int, List<ConversationMessage>>();

	private List<ConversationMessage> _publicConversationHistory = new List<ConversationMessage>();

	private static int _sceneHistorySessionId = 0;

	private int _sceneConversationEpoch = 0;

	private const int MAX_HISTORY_TURNS = 20;

	private const int AUTO_GROUP_CHAT_MAX_LINES = 8;

	private Agent _currentStareTarget = null;

	private float _stareTimer = 0f;

	private float _stareTargetLostGraceTimer = 0f;

	private float _interactionGraceTimer = 0f;

	private Dictionary<string, float> _passiveCooldowns = new Dictionary<string, float>(StringComparer.Ordinal);


    private const float STARE_TRIGGER_TIME = 25f;

	private const float STARE_TARGET_LOST_GRACE = 2f;

	private const float PASSIVE_STARE_COOLDOWN = 10f;

	private const float ACTIVE_CHAT_COOLDOWN = 300f;

	private const float PASSIVE_INTERACTION_GRACE = 0.75f;

	private const float ACTIVE_INTERACTION_IDLE_TIMEOUT = 15f;

	private const float ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT = 300f;

	private const float ACTIVE_INTERACTION_IDLE_PLAYER_RANGE = 10f;

	private const float SCENE_FOLLOW_MAX_IDLE_DISTANCE = 0f;

	private const float SCENE_SUMMON_ESCORT_REPOSITION_DISTANCE_SQ = 25f;

	private const float SCENE_GUIDE_PLAYER_REQUIRED_DISTANCE_SQ = 100f;

	private const float SCENE_GUIDE_TARGET_REACHED_DISTANCE_SQ = 36f;

	private const float SCENE_GUIDE_PLAYER_LOST_TIMEOUT = 15f;

	private static readonly FieldInfo FollowAgentBehaviorIdleDistanceField = typeof(FollowAgentBehavior).GetField("_idleDistance", BindingFlags.Instance | BindingFlags.NonPublic);

	private const float PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS = 60f;

	private const float MULTI_SCENE_MOVEMENT_SUPPRESSION_INTERVAL = 0.01f;

	private const float MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS = 0.25f;

	private const float LIP_SYNC_SAFE_MAX_DISTANCE = 6.5f;

	private static readonly Regex MeetingSceneShoutTauntTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_(?:WARN|BATTLE)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneSummonActionTagRegex = new Regex("\\[(?:ASS|ACTION:SCENE_SUMMON):([0-9\\s,]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneGuideActionTagRegex = new Regex("\\[(?:GUI|ACTION:SCENE_GUIDE):([0-9]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneFollowStartTagRegex = new Regex("\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneFollowStopTagRegex = new Regex("\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneEndChatActionTagRegex = new Regex("\\[END\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private const int SCENE_SUMMON_PROMPT_TARGET_LIMIT = 12;

	private const int SCENE_GUIDE_PROMPT_TARGET_LIMIT = 6;

	private const float SCENE_SUMMON_MESSENGER_DOOR_DISTANCE_SQ = 2.25f;

	private const float SCENE_SUMMON_MESSENGER_TARGET_DISTANCE_SQ = 100f;

	private const float SCENE_SUMMON_RELAY_DISTANCE_SQ = 9f;

	private const float SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ = 9f;

	private const float SCENE_SUMMON_DELAY_SECONDS = 2.25f;

	private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

	private float _tickTimer = 0f;

	private readonly object _multiSceneMovementSuppressionLock = new object();

	private readonly HashSet<int> _multiSceneMovementSuppressionAgentIndices = new HashSet<int>();

	private bool _multiSceneMovementSuppressionActive = false;

	private float _multiSceneMovementSuppressionTimer = 0f;

	private readonly HashSet<int> _speakingAgentIndices = new HashSet<int>();

	private readonly object _speakingLock = new object();

	private readonly Dictionary<int, SoundEvent> _agentSoundEvents = new Dictionary<int, SoundEvent>();

	private readonly Dictionary<int, string> _agentWavPaths = new Dictionary<int, string>();

	private readonly List<ActiveSceneSummonRequest> _activeSceneSummonRequests = new List<ActiveSceneSummonRequest>();

	private readonly List<ActiveSceneGuideRequest> _activeSceneGuideRequests = new List<ActiveSceneGuideRequest>();

	private readonly List<SceneSummonConversationSession> _activeSceneSummonConversationSessions = new List<SceneSummonConversationSession>();

	private readonly Dictionary<int, SceneSummonBatchState> _activeSceneSummonBatches = new Dictionary<int, SceneSummonBatchState>();

	private readonly List<SceneReturnJob> _activeSceneReturnJobs = new List<SceneReturnJob>();

	private readonly HashSet<int> _sceneSummonScriptedAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, string> _agentXmlPaths = new Dictionary<int, string>();

	private readonly HashSet<int> _agentLipSyncDetachedForSafety = new HashSet<int>();

	private readonly Dictionary<int, SceneInteractionSession> _activeInteractionSessions = new Dictionary<int, SceneInteractionSession>();

	private Action<int, string, string, float> _ttsOnAudioFileReadyHandler;

	private Action<int> _ttsOnPlaybackStartedHandler;

	private Action<int> _ttsOnPlaybackFinishedHandler;

	private Action<int, string> _ttsOnPlaybackFailedHandler;

	private long _lastEscapePressedUtcTicks = 0L;

	private bool _wasGameWindowFocused = true;

	private bool _ttsPausedByShoutUi = false;

	private bool _ttsPausedByInterruption = false;

	private readonly List<DeferredCleanupItem> _deferredCleanupQueue = new List<DeferredCleanupItem>();

	private long _deferredCleanupStableSinceUtcTicks = 0L;

	private const double DeferredCleanupStableWindowSeconds = 1.5;

	private const double DeferredCleanupMinAgeSeconds = 0.5;

	private const double DeferredCleanupMaxAgeSeconds = 20.0;

	private const int DeferredCleanupBatchSize = 8;

	private readonly object _ttsBubbleSyncLock = new object();

	private readonly Dictionary<int, Queue<PendingNpcBubbleEntry>> _pendingNpcBubbleQueues = new Dictionary<int, Queue<PendingNpcBubbleEntry>>();

	private readonly Dictionary<int, Queue<float>> _pendingAudioDurationQueues = new Dictionary<int, Queue<float>>();

	private readonly HashSet<int> _ttsPlaybackStartedAgents = new HashSet<int>();

	private readonly Dictionary<int, Queue<long>> _pendingSpeechCompletionTokenQueues = new Dictionary<int, Queue<long>>();

	private readonly Dictionary<int, Queue<ActiveSceneSummonRequest>> _pendingSceneSummonLaunchQueues = new Dictionary<int, Queue<ActiveSceneSummonRequest>>();

	private readonly Dictionary<int, Queue<ActiveSceneGuideRequest>> _pendingSceneGuideLaunchQueues = new Dictionary<int, Queue<ActiveSceneGuideRequest>>();

	private readonly Dictionary<int, Queue<PendingSceneDialogueFeedEntry>> _pendingSceneDialogueFeedQueues = new Dictionary<int, Queue<PendingSceneDialogueFeedEntry>>();

	private readonly Dictionary<int, PendingInteractionTimeoutArm> _pendingInteractionTimeoutArms = new Dictionary<int, PendingInteractionTimeoutArm>();

	private readonly Dictionary<int, PendingSceneSummonReturnAfterSpeech> _pendingSceneSummonReturnsAfterSpeech = new Dictionary<int, PendingSceneSummonReturnAfterSpeech>();

	private readonly Dictionary<int, PendingSceneFollowCommand> _pendingSceneFollowCommands = new Dictionary<int, PendingSceneFollowCommand>();

	private readonly Dictionary<int, PendingSceneGuideReturnAfterSpeech> _pendingSceneGuideReturnsAfterSpeech = new Dictionary<int, PendingSceneGuideReturnAfterSpeech>();

	private readonly Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech> _pendingSceneAutonomyRestoresAfterSpeech = new Dictionary<int, PendingSceneAutonomyRestoreAfterSpeech>();

	private readonly Dictionary<int, SceneFollowReturnState> _sceneFollowReturnStates = new Dictionary<int, SceneFollowReturnState>();

	private int _nextSceneSummonBatchId = 1;

	private static readonly object _ttsEventSubLock = new object();

	private static ShoutBehavior _ttsEventSubscribedOwner = null;

	private static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	private readonly bool _enableRhubarbSoundEventPlayback = true;

	private static void LogLipSyncNativeProbe(string stage, int agentIndex, string extra = null)
	{
		try
		{
			int managedThreadId = -1;
			try
			{
				managedThreadId = Thread.CurrentThread.ManagedThreadId;
			}
			catch
			{
			}
			string sceneName = "";
			try
			{
				sceneName = Mission.Current?.SceneName ?? "";
			}
			catch
			{
			}
			string suffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;
			Logger.Log("LipSyncProbe", $"stage={stage}, agentIndex={agentIndex}, thread={managedThreadId}, scene={sceneName}{suffix}");
		}
		catch
		{
		}
	}

	private void CleanupPreviousLipSyncPlaybackForReplacement(string reason)
	{
		List<int> list = new List<int>();
		List<SoundEvent> list2 = new List<SoundEvent>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		lock (_speakingLock)
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (int key in _agentSoundEvents.Keys)
			{
				hashSet.Add(key);
			}
			foreach (int key2 in _agentWavPaths.Keys)
			{
				hashSet.Add(key2);
			}
			foreach (int key3 in _agentXmlPaths.Keys)
			{
				hashSet.Add(key3);
			}
			foreach (int item in hashSet)
			{
				SoundEvent value = null;
				string value2 = null;
				string value3 = null;
				if (_agentSoundEvents.TryGetValue(item, out value))
				{
					_agentSoundEvents.Remove(item);
				}
				if (_agentWavPaths.TryGetValue(item, out value2))
				{
					_agentWavPaths.Remove(item);
				}
				if (_agentXmlPaths.TryGetValue(item, out value3))
				{
					_agentXmlPaths.Remove(item);
				}
				_agentLipSyncDetachedForSafety.Remove(item);
				if (value != null || !string.IsNullOrWhiteSpace(value2) || !string.IsNullOrWhiteSpace(value3))
				{
					list.Add(item);
					list2.Add(value);
					list3.Add(value2);
					list4.Add(value3);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			int num = list[i];
			StopAgentRhubarbRecordIfPossible(num, reason);
			if (!IsInEscapeTransitionWindow())
			{
				SafeStopAndReleaseSoundEvent(list2[i]);
			}
			QueueDeferredCleanup(null, list3[i], list4[i], reason, num);
		}
	}

	private static void PrepareAgentForTrueLipSyncIfPossible(Agent agent)
	{
		if (agent == null || !agent.IsHuman)
		{
			return;
		}
		try
		{
			string defaultFaceIdle = "";
			try
			{
				Type type = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a?.GetType("TaleWorlds.CampaignSystem.CharacterHelper", throwOnError: false) ?? a?.GetType("TaleWorlds.CampaignSystem.Helpers.CharacterHelper", throwOnError: false)).FirstOrDefault((Type t) => t != null);
				MethodInfo methodInfo = type?.GetMethod("GetDefaultFaceIdle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(CharacterObject) }, null);
				defaultFaceIdle = (methodInfo?.Invoke(null, new object[1] { agent.Character as CharacterObject }) as string) ?? "";
			}
			catch
			{
				defaultFaceIdle = "";
			}
			if (!string.IsNullOrWhiteSpace(defaultFaceIdle))
			{
				agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, true);
			}
			agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", false);
			LogLipSyncNativeProbe("PrepareTrueLipSyncFaceIdle", agent.Index, "idle=" + (defaultFaceIdle ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] PrepareAgentForTrueLipSyncIfPossible failed, agentIndex={agent.Index}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.PrepareTrueLipSyncFaceIdle", ex, "agentIndex=" + agent.Index);
		}
	}

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

	internal static bool IsSceneShoutInputActiveForExternal()
	{
		try
		{
			ShoutBehavior currentInstance = CurrentInstance;
			return currentInstance != null && currentInstance._isProcessingShout;
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

	private static bool CanAgentParticipateInSceneSpeech(Agent agent)
	{
		try
		{
			return agent != null && agent.IsHuman && agent.IsActive() && agent.State == AgentState.Active && agent.Health > 0f;
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

	private void SchedulePendingNpcBubbleFallbackDispatch(int agentIndex, int delayMs = 180, int remainingRetries = 3)
	{
		if (agentIndex < 0)
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
								SchedulePendingNpcBubbleFallbackDispatch(agentIndex, delayMs, remainingRetries - 1);
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
		if (agentIndex < 0 || Mission.Current == null)
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
			float num = Mission.Current.CurrentTime + Math.Max(0f, delaySeconds);
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
			_pendingSceneSummonLaunchQueues.Clear();
			_pendingSceneDialogueFeedQueues.Clear();
			_ttsPlaybackStartedAgents.Clear();
		}
		_pendingInteractionTimeoutArms.Clear();
	}

	private void HandleTtsPlaybackFailed(int agentIndex, string errorMessage)
	{
		bool hasAgent = agentIndex >= 0;
		long interactionToken = 0L;
		bool hasInteractionToken = TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);
		if (hasAgent)
		{
			lock (_speakingLock)
			{
				_speakingAgentIndices.Remove(agentIndex);
				_agentLipSyncDetachedForSafety.Remove(agentIndex);
			}
			lock (_ttsBubbleSyncLock)
			{
				_ttsPlaybackStartedAgents.Remove(agentIndex);
			}
		}
		PendingNpcBubbleEntry bubble = null;
		float typingDuration = -1f;
		bool hasBubble = hasAgent && TryDequeuePendingNpcBubble(agentIndex, out bubble, out typingDuration);
		string failText = string.IsNullOrWhiteSpace(errorMessage) ? "语音合成失败，已切换为文字气泡。" : ("语音合成失败：" + errorMessage);
		Logger.Log("LipSync", $"[OnPlaybackFailed] agentIndex={agentIndex}, error={errorMessage}");
		LogTtsReport("PlaybackFailed", agentIndex, $"error={errorMessage};hasInteractionToken={hasInteractionToken};hasBubble={hasBubble};typingDuration={typingDuration:F2}");
		_mainThreadActions.Enqueue(delegate
		{
			try
			{
				if (hasAgent)
				{
					ConvertPendingSceneDialogueFeedToTimedFlush(agentIndex, (typingDuration > 0f) ? typingDuration : ((hasBubble && bubble != null) ? EstimateBubbleTypingDurationSeconds(bubble.UiContent) : 0f));
				}
				if (hasBubble && bubble != null)
				{
					TryShowNpcBubble(bubble.Agent, bubble.UiContent, typingDuration);
					if (hasInteractionToken)
					{
						ScheduleInteractionTimeoutArm(agentIndex, interactionToken, (typingDuration > 0f) ? typingDuration : EstimateBubbleTypingDurationSeconds(bubble.UiContent));
					}
				}
				FlushPendingSceneSummonLaunches(agentIndex, (typingDuration > 0f) ? typingDuration : 0f);
				FlushPendingSceneGuideLaunches(agentIndex, (typingDuration > 0f) ? typingDuration : 0f);
			}
			catch
			{
			}
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("[TTS错误] " + failText, new Color(1f, 0.35f, 0.35f)));
			}
			catch
			{
			}
			try
			{
				if (hasAgent)
				{
					StopAgentRhubarbRecordIfPossible(agentIndex, "PlaybackFailed");
					SoundEvent value = null;
					string value2 = null;
					string value3 = null;
					lock (_speakingLock)
					{
						if (_agentSoundEvents.TryGetValue(agentIndex, out value))
						{
							_agentSoundEvents.Remove(agentIndex);
						}
						if (_agentWavPaths.TryGetValue(agentIndex, out value2))
						{
							_agentWavPaths.Remove(agentIndex);
						}
						if (_agentXmlPaths.TryGetValue(agentIndex, out value3))
						{
							_agentXmlPaths.Remove(agentIndex);
						}
						_agentLipSyncDetachedForSafety.Remove(agentIndex);
					}
					QueueDeferredCleanup(value, value2, value3, "PlaybackFailed", agentIndex);
					LogTtsReport("PlaybackFailed.CleanupQueued", agentIndex, $"hasSe={(value != null)};hasWav={(!string.IsNullOrWhiteSpace(value2))};hasXml={(!string.IsNullOrWhiteSpace(value3))}");
				}
			}
			catch
			{
			}
		});
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

	private void ScheduleNpcSpeechToMessageFeed(int agentIndex, string npcDisplayName, string content, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex < 0 || string.IsNullOrWhiteSpace(content) || Mission.Current == null)
		{
			return;
		}
		float num = Math.Max(0f, playbackInfo?.VisualDurationSeconds ?? 0f);
		bool flag = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		float executeAtMissionTime = flag ? (-1f) : (Mission.Current.CurrentTime + num);
		EnqueuePendingSceneDialogueFeed(agentIndex, npcDisplayName, content, new Color(1f, 0.8f, 0.2f), flag, executeAtMissionTime);
	}

	private SceneSpeechPlaybackInfo ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true)
	{
		SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = new SceneSpeechPlaybackInfo();
		if (!CanAgentParticipateInSceneSpeech(liveAgent))
		{
			return sceneSpeechPlaybackInfo;
		}
		string text = SanitizeSceneSpeechText(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return sceneSpeechPlaybackInfo;
		}
		try
		{
			string text2 = BuildPatienceBadgeForNpc(npc, liveAgent);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = "【" + text2 + "】" + text;
			}
		}
		catch
		{
		}
		int num = npc?.AgentIndex ?? (-1);
		string npcDisplayName = GetSceneNpcHistoryNameForPrompt(npc);
		if (string.IsNullOrWhiteSpace(npcDisplayName))
		{
			npcDisplayName = "NPC";
		}
		bool flagHostileSpeech = IsAgentHostileToMainAgent(liveAgent);
		if (flagHostileSpeech && num >= 0)
		{
			_activeInteractionSessions.Remove(num);
			_pendingInteractionTimeoutArms.Remove(num);
			lock (_ttsBubbleSyncLock)
			{
				_pendingSpeechCompletionTokenQueues.Remove(num);
			}
		}
		long interactionToken = 0L;
		if (!flagHostileSpeech && num >= 0 && _activeInteractionSessions.TryGetValue(num, out var value) && value != null)
		{
			interactionToken = value.InteractionToken;
		}
		bool flag = false;
		bool flag2 = allowTts && IsTtsPlaybackEnabledForShout();
		sceneSpeechPlaybackInfo.TtsEnabled = flag2;
		string text3 = "scene_lipsync_not_requested";
		bool flag3 = flag2 && attachTtsToSceneAgent && num >= 0 && CanAgentParticipateInSceneSpeech(liveAgent) && CanAgentUseSceneLipSync(liveAgent, out text3);
		int num2 = (flag3 ? num : (-1));
		LogTtsReport("ShowNpcSpeechOutput.Enter", num, $"allowTts={allowTts};attachToSceneAgent={attachTtsToSceneAgent};effectiveAgentIndex={num2};contentLen={(text ?? string.Empty).Length};hostileSpeech={flagHostileSpeech};lipSyncSafe={flag3};lipSyncReason={text3}");
		if (!allowTts)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Skip TTS for current speech. agentIndex=" + num);
			}
			catch
			{
			}
		}
		else if (flag2 && attachTtsToSceneAgent && num >= 0 && num2 < 0)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Use detached TTS without scene lipsync. agentIndex=" + num + ", reason=" + text3);
			}
			catch
			{
			}
		}
		if (flag2)
		{
			string text4 = "";
			try
			{
				if (npc != null && npc.IsHero)
				{
					Hero hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
					if (hero != null)
					{
						text4 = MyBehavior.GetNpcVoiceIdForExternal(hero);
						if (string.IsNullOrWhiteSpace(text4))
						{
							text4 = VoiceMapper.ResolveVoiceId(hero);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text4) && npc != null)
				{
					text4 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, npc.AgentIndex);
				}
				flag = TtsEngine.Instance.SpeakAsync(text, -1, -1f, num2, text4);
			}
			catch
			{
			}
			sceneSpeechPlaybackInfo.TtsAccepted = flag;
			sceneSpeechPlaybackInfo.WaitForPlaybackFinished = flag && num2 >= 0;
			LogTtsReport("ShowNpcSpeechOutput.SpeakAttempt", num, $"effectiveAgentIndex={num2};speakAccepted={flag};voiceId={text4};lipSyncSafe={flag3};lipSyncReason={text3}");
		}
		if (flag && num2 >= 0 && CanAgentParticipateInSceneSpeech(liveAgent))
		{
			sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(text));
			ClearPendingTtsBubbleSyncForAgent(num);
			if (interactionToken != 0L)
			{
				EnqueuePendingSpeechCompletionToken(num, interactionToken);
			}
			EnqueuePendingNpcBubble(num, liveAgent, text, npcDisplayName, sceneSpeechPlaybackInfo.VisualDurationSeconds);
			ScheduleNpcSpeechToMessageFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
			MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
			LogTtsReport("ShowNpcSpeechOutput.SceneBubbleQueued", num, $"interactionToken={interactionToken};uiLen={text.Length}");
			return sceneSpeechPlaybackInfo;
		}
		float num3 = EstimateBubbleTypingDurationSeconds(text);
		sceneSpeechPlaybackInfo.VisualDurationSeconds = num3;
		if (!TryShowNpcBubble(liveAgent, text, num3))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + npcDisplayName);
		}
		if (interactionToken != 0L)
		{
			ScheduleInteractionTimeoutArm(num, interactionToken, num3);
		}
		ScheduleNpcSpeechToMessageFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
		MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
		LogTtsReport("ShowNpcSpeechOutput.BubbleFallback", num, $"interactionToken={interactionToken};typingDuration={num3:F2};ttsAccepted={flag};ttsEnabled={flag2}");
		if (!(!flag && flag2))
		{
			return sceneSpeechPlaybackInfo;
		}
		try
		{
			string text4 = "";
			if (npc != null && npc.IsHero)
			{
				Hero hero2 = ResolveHeroFromAgentIndex(npc.AgentIndex);
				if (hero2 != null)
				{
					text4 = MyBehavior.GetNpcVoiceIdForExternal(hero2);
					if (string.IsNullOrWhiteSpace(text4))
					{
						text4 = VoiceMapper.ResolveVoiceId(hero2);
					}
				}
			}
			if (string.IsNullOrWhiteSpace(text4) && npc != null)
			{
				text4 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, npc.AgentIndex);
			}
			TtsEngine.Instance.SpeakAsync(text, -1, -1f, num2, text4);
			LogTtsReport("ShowNpcSpeechOutput.DetachedSpeakFallback", num, $"effectiveAgentIndex={num2};voiceId={text4}");
		}
		catch
		{
		}
		return sceneSpeechPlaybackInfo;
	}

	public static bool CanAgentParticipateInSceneSpeechExternal(int agentIndex)
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			return false;
		}
		try
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			return CanAgentParticipateInSceneSpeech(agent);
		}
		catch
		{
			return false;
		}
	}

	private static string StripScenePersonaBlocks(string text)
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

	private static string ExtractTrustPromptBlock(string text, out string remaining)
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

	private static string InjectTrustBlockBelowTriState(string localExtras, string trustBlock)
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
		int num = text.IndexOf("【4.三值状态】", StringComparison.Ordinal);
		if (num < 0)
		{
			return text + "\n" + text2;
		}
		int num2 = text.IndexOf("【群体对话规则】", num, StringComparison.Ordinal);
		if (num2 >= 0)
		{
			string text3 = text.Substring(0, num2).TrimEnd();
			string text4 = text.Substring(num2).TrimStart();
			return text3 + "\n" + text2 + "\n" + text4;
		}
		return text + "\n" + text2;
	}

	private static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
	}

	private static string GetSceneNpcIdentityNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptIdentityName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "未命名NPC" : text;
	}

	private static string GetSceneNpcGivenNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptListName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

	private static string GetSceneNpcHistoryNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptHistoryName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

	private static string GetSceneNpcPatienceNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptPatienceName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcHistoryNameForPrompt(npc) : text;
	}

	private static string GetSceneNpcListIdentityForPrompt(NpcDataPacket npc)
	{
		if (npc != null && !npc.IsHero)
		{
			return GetSceneNpcIdentityNameForPrompt(npc);
		}
		string text = (npc?.RoleDesc ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

	private static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc, bool includeRelayId = false)
	{
		if (npc == null)
		{
			return "- 名字: 未知 | 身份: 未知身份";
		}
		string text = npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc);
		string text2 = "- 名字: " + text + " | 身份: " + GetSceneNpcListIdentityForPrompt(npc);
		if (includeRelayId && npc.AgentIndex >= 0)
		{
			text2 = text2 + " | 接力编号: " + npc.AgentIndex;
		}
		return text2;
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
		return "【非HeroNPC命名说明】：【在场人物列表】里的“名字”是非HeroNPC的个人名字；在【当前场景公共对话与互动】等历史里，会使用“身份+名字”的写法，例如“帝国女镇民利娅”，两者指向同一人。";
	}

	private static string GetSceneSummonTargetDisplayName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		return (npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc)).Trim();
	}

	private static string GetSceneSummonLocationCode(Location location)
	{
		string text = (location?.StringId ?? "").Trim().ToLowerInvariant();
		return text switch
		{
			"center" => "中",
			"village_center" => "村",
			"tavern" => "酒",
			"lordshall" => "厅",
			"prison" => "狱",
			"arena" => "场",
			"alley" => "巷",
			"port" => "港",
			_ => "处",
		};
	}

	private static List<Location> FindSceneLocationPath(Location fromLocation, Location toLocation)
	{
		if (fromLocation == null || toLocation == null)
		{
			return null;
		}
		if (fromLocation == toLocation)
		{
			return new List<Location> { fromLocation };
		}
		Queue<Location> queue = new Queue<Location>();
		Dictionary<Location, Location> dictionary = new Dictionary<Location, Location>();
		HashSet<Location> hashSet = new HashSet<Location>();
		hashSet.Add(fromLocation);
		queue.Enqueue(fromLocation);
		while (queue.Count > 0)
		{
			Location location = queue.Dequeue();
			IEnumerable<Location> enumerable = location.LocationsOfPassages ?? Enumerable.Empty<Location>();
			foreach (Location item in enumerable)
			{
				if (item == null || !hashSet.Add(item))
				{
					continue;
				}
				dictionary[item] = location;
				if (item == toLocation)
				{
					List<Location> list = new List<Location> { toLocation };
					Location location2 = toLocation;
					while (dictionary.TryGetValue(location2, out var value))
					{
						list.Add(value);
						if (value == fromLocation)
						{
							break;
						}
						location2 = value;
					}
					list.Reverse();
					return list;
				}
				queue.Enqueue(item);
			}
		}
		return null;
	}

	private List<SceneSummonPromptTarget> BuildSceneSummonPromptTargets(IEnumerable<NpcDataPacket> presentNpcs, Dictionary<int, Hero> resolvedHeroes = null)
	{
		List<SceneSummonPromptTarget> list = new List<SceneSummonPromptTarget>();
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (locationComplex == null || location == null)
		{
			return list;
		}
		HashSet<LocationCharacter> hashSet = new HashSet<LocationCharacter>();
		void tryAdd(LocationCharacter locationCharacter, string displayName)
		{
			if (locationCharacter == null || hashSet.Contains(locationCharacter) || locationCharacter.Character == null || locationCharacter.Character == CharacterObject.PlayerCharacter)
			{
				return;
			}
			Location locationOfCharacter = locationComplex.GetLocationOfCharacter(locationCharacter);
			if (locationOfCharacter == null)
			{
				return;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
			if (sceneLocationPath == null || sceneLocationPath.Count == 0)
			{
				return;
			}
			string text = (displayName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = (locationCharacter.Character.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			hashSet.Add(locationCharacter);
			list.Add(new SceneSummonPromptTarget
			{
				DisplayName = text,
				LocationCode = GetSceneSummonLocationCode(locationOfCharacter),
				LocationCharacter = locationCharacter,
				SourceLocation = locationOfCharacter
			});
		}
		if (presentNpcs != null)
		{
			foreach (NpcDataPacket presentNpc in presentNpcs)
			{
				if (presentNpc == null)
				{
					continue;
				}
				LocationCharacter locationCharacter2 = null;
				if (presentNpc.IsHero)
				{
					Hero hero = null;
					if (resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(presentNpc.AgentIndex, out hero);
					}
					hero ??= ResolveHeroFromAgentIndex(presentNpc.AgentIndex);
					if (hero != null)
					{
						locationCharacter2 = locationComplex.GetLocationCharacterOfHero(hero);
					}
				}
				else
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == presentNpc.AgentIndex);
					if (agent != null)
					{
						locationCharacter2 = locationComplex.FindCharacter(agent);
					}
				}
				tryAdd(locationCharacter2, GetSceneSummonTargetDisplayName(presentNpc));
				if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
				{
					break;
				}
			}
		}
		if (list.Count < SCENE_SUMMON_PROMPT_TARGET_LIMIT)
		{
			IEnumerable<IGrouping<Location, LocationCharacter>> enumerable2 = from locationCharacter3 in locationComplex.GetListOfCharacters()
				where locationCharacter3 != null && locationCharacter3.Character != null && locationCharacter3.Character.IsHero && locationCharacter3.Character.HeroObject != null && locationCharacter3.Character.HeroObject != Hero.MainHero
				group locationCharacter3 by locationComplex.GetLocationOfCharacter(locationCharacter3) into grouped
				orderby (FindSceneLocationPath(location, grouped.Key)?.Count ?? int.MaxValue), (grouped.Key?.StringId ?? ""), (grouped.FirstOrDefault()?.Character?.Name?.ToString() ?? "")
				select grouped;
			foreach (IGrouping<Location, LocationCharacter> item in enumerable2)
			{
				foreach (LocationCharacter item2 in item.OrderBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture))
				{
					tryAdd(item2, item2.Character?.HeroObject?.Name?.ToString());
					if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
					{
						break;
					}
				}
				if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
				{
					break;
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].PromptId = i + 1;
		}
		return list;
	}

	private static List<SceneSummonPromptTarget> CloneSceneSummonPromptTargets(List<SceneSummonPromptTarget> source)
	{
		if (source == null || source.Count == 0)
		{
			return null;
		}
		List<SceneSummonPromptTarget> list = new List<SceneSummonPromptTarget>(source.Count);
		foreach (SceneSummonPromptTarget item in source)
		{
			if (item == null)
			{
				continue;
			}
			list.Add(new SceneSummonPromptTarget
			{
				PromptId = item.PromptId,
				DisplayName = item.DisplayName,
				LocationCode = item.LocationCode,
				LocationCharacter = item.LocationCharacter,
				SourceLocation = item.SourceLocation
			});
		}
		return list;
	}

	private static string GetSceneGuideCategoryDisplayName(Occupation occupation)
	{
		return occupation switch
		{
			Occupation.Armorer => "盔甲匠",
			Occupation.Weaponsmith => "武器匠",
			Occupation.Blacksmith => "武器匠",
			Occupation.HorseTrader => "马匹贩子",
			Occupation.Merchant => "商贩",
			Occupation.GoodsTrader => "商贩",
			_ => ""
		};
	}

	private static bool TryGetSceneGuideTargetLabel(LocationCharacter locationCharacter, out string displayName, out bool preferNearest)
	{
		displayName = "";
		preferNearest = false;
		Occupation occupation = Occupation.NotAssigned;
		try
		{
			if (locationCharacter?.Character is CharacterObject characterObject)
			{
				occupation = characterObject.Occupation;
			}
		}
		catch
		{
			occupation = Occupation.NotAssigned;
		}
		displayName = GetSceneGuideCategoryDisplayName(occupation);
		preferNearest = occupation == Occupation.Merchant || occupation == Occupation.GoodsTrader;
		return !string.IsNullOrWhiteSpace(displayName);
	}

	private List<SceneGuidePromptTarget> BuildSceneGuidePromptTargets(Agent guideAgent = null)
	{
		List<SceneGuidePromptTarget> list = new List<SceneGuidePromptTarget>();
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (locationComplex == null || location == null)
		{
			return list;
		}
		Dictionary<string, List<LocationCharacter>> dictionary = new Dictionary<string, List<LocationCharacter>>(StringComparer.OrdinalIgnoreCase);
		foreach (LocationCharacter item in locationComplex.GetListOfCharacters())
		{
			if (!TryGetSceneGuideTargetLabel(item, out var displayName, out var _))
			{
				continue;
			}
			if (string.Equals(displayName, "商贩", StringComparison.OrdinalIgnoreCase) && (item.Character?.IsHero ?? false))
			{
				continue;
			}
			Location locationOfCharacter = locationComplex.GetLocationOfCharacter(item);
			if (locationOfCharacter == null)
			{
				continue;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
			if (sceneLocationPath == null || sceneLocationPath.Count == 0)
			{
				continue;
			}
			if (!dictionary.TryGetValue(displayName, out var value))
			{
				value = new List<LocationCharacter>();
				dictionary[displayName] = value;
			}
			value.Add(item);
		}
		Vec3 vec = guideAgent?.Position ?? Agent.Main?.Position ?? Vec3.Zero;
		string[] array = new string[4] { "盔甲匠", "武器匠", "马匹贩子", "商贩" };
		foreach (string text in array)
		{
			if (!dictionary.TryGetValue(text, out var value2) || value2 == null || value2.Count == 0)
			{
				continue;
			}
			LocationCharacter locationCharacter = value2[0];
			if (string.Equals(text, "商贩", StringComparison.OrdinalIgnoreCase))
			{
				locationCharacter = value2.OrderBy(delegate(LocationCharacter x)
				{
					Agent agent = ResolveAgentForLocationCharacter(x);
					if (agent != null && agent.IsActive())
					{
						return agent.Position.DistanceSquared(vec);
					}
					return float.MaxValue;
				}).ThenBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture).FirstOrDefault();
			}
			else
			{
				locationCharacter = value2.OrderBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture).FirstOrDefault();
			}
			if (locationCharacter == null)
			{
				continue;
			}
			Location locationOfCharacter2 = locationComplex.GetLocationOfCharacter(locationCharacter);
			if (locationOfCharacter2 == null)
			{
				continue;
			}
			list.Add(new SceneGuidePromptTarget
			{
				PromptId = list.Count + 1,
				DisplayName = text,
				LocationCode = GetSceneSummonLocationCode(locationOfCharacter2),
				LocationCharacter = locationCharacter,
				SourceLocation = locationOfCharacter2
			});
			if (list.Count >= SCENE_GUIDE_PROMPT_TARGET_LIMIT)
			{
				break;
			}
		}
		return list;
	}

	private static List<SceneGuidePromptTarget> CloneSceneGuidePromptTargets(List<SceneGuidePromptTarget> source)
	{
		if (source == null || source.Count == 0)
		{
			return null;
		}
		List<SceneGuidePromptTarget> list = new List<SceneGuidePromptTarget>(source.Count);
		foreach (SceneGuidePromptTarget item in source)
		{
			if (item == null)
			{
				continue;
			}
			list.Add(new SceneGuidePromptTarget
			{
				PromptId = item.PromptId,
				DisplayName = item.DisplayName,
				LocationCode = item.LocationCode,
				LocationCharacter = item.LocationCharacter,
				SourceLocation = item.SourceLocation
			});
		}
		return list;
	}

	private static void AppendSceneSummonPromptSection(StringBuilder prompt, List<SceneSummonPromptTarget> targets)
	{
		if (prompt == null || targets == null || targets.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【可召目标】：");
		foreach (SceneSummonPromptTarget target in targets)
		{
			if (target != null && target.PromptId > 0 && !string.IsNullOrWhiteSpace(target.DisplayName))
			{
				prompt.AppendLine(target.PromptId + " " + target.DisplayName.Trim() + " " + (target.LocationCode ?? "处"));
			}
		}
	}

	private static string BuildSceneMechanismPromptSection(List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string sceneSummonClosureInstruction = null, string sceneFollowControlInstruction = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendSceneSummonPromptSection(stringBuilder, sceneSummonTargets);
		AppendSceneGuidePromptSection(stringBuilder, sceneGuideTargets);
		if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
		{
			stringBuilder.AppendLine(sceneSummonClosureInstruction);
		}
		if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
		{
			stringBuilder.AppendLine(sceneFollowControlInstruction);
		}
		return stringBuilder.ToString().Trim();
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

	private void EnqueuePendingSceneSummonLaunch(int agentIndex, ActiveSceneSummonRequest request)
	{
		if (agentIndex < 0 || request == null)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<ActiveSceneSummonRequest>();
				_pendingSceneSummonLaunchQueues[agentIndex] = value;
			}
			value.Enqueue(request);
		}
	}

	private void EnqueuePendingSceneGuideLaunch(int agentIndex, ActiveSceneGuideRequest request)
	{
		if (agentIndex < 0 || request == null)
		{
			return;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (!_pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<ActiveSceneGuideRequest>();
				_pendingSceneGuideLaunchQueues[agentIndex] = value;
			}
			value.Enqueue(request);
		}
	}

	private bool TryDequeuePendingSceneSummonLaunch(int agentIndex, out ActiveSceneSummonRequest request)
	{
		request = null;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (_pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				request = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSceneSummonLaunchQueues.Remove(agentIndex);
				}
				return request != null;
			}
		}
		return false;
	}

	private bool TryDequeuePendingSceneGuideLaunch(int agentIndex, out ActiveSceneGuideRequest request)
	{
		request = null;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_ttsBubbleSyncLock)
		{
			if (_pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				request = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSceneGuideLaunchQueues.Remove(agentIndex);
				}
				return request != null;
			}
		}
		return false;
	}

	private void FlushPendingSceneSummonLaunches(int agentIndex, float additionalDelaySeconds = 0f)
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		while (TryDequeuePendingSceneSummonLaunch(agentIndex, out var request))
		{
			request.NextStageMissionTime = Mission.Current.CurrentTime + Math.Max(0f, additionalDelaySeconds);
			LogSceneSummonState("pending_launch_tts_finished", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "extraDelay=" + additionalDelaySeconds.ToString("F2"), force: true);
		}
	}

	private void FlushPendingSceneGuideLaunches(int agentIndex, float additionalDelaySeconds = 0f)
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		while (TryDequeuePendingSceneGuideLaunch(agentIndex, out var request))
		{
			request.NextStageMissionTime = Mission.Current.CurrentTime + Math.Max(0f, additionalDelaySeconds);
		}
	}

	private void CancelSceneSummonBatch(int batchId)
	{
		if (batchId < 0)
		{
			return;
		}
		_activeSceneSummonBatches.Remove(batchId);
		foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x != null && x.BatchId == batchId).ToList())
		{
			CleanupSceneSummonDoorProxyAgent(item);
		}
		_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x != null && x.BatchId == batchId);
		lock (_ttsBubbleSyncLock)
		{
			List<int> list = _pendingSceneSummonLaunchQueues.Keys.ToList();
			foreach (int item in list)
			{
				if (!_pendingSceneSummonLaunchQueues.TryGetValue(item, out var value) || value == null || value.Count == 0)
				{
					continue;
				}
				Queue<ActiveSceneSummonRequest> queue = new Queue<ActiveSceneSummonRequest>(value.Where((ActiveSceneSummonRequest x) => x != null && x.BatchId != batchId));
				if (queue.Count == 0)
				{
					_pendingSceneSummonLaunchQueues.Remove(item);
				}
				else
				{
					_pendingSceneSummonLaunchQueues[item] = queue;
				}
			}
		}
	}

	private void TryAdvanceSceneSummonBatch(ActiveSceneSummonRequest request, Agent speakerAgent)
	{
		if (request == null || request.BatchContinuationStarted || request.BatchId < 0 || request.KeepMessengerWithTarget)
		{
			return;
		}
		request.BatchContinuationStarted = true;
		if (!_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) || value == null || value.PendingTargets.Count == 0)
		{
			return;
		}
		if (TryStartNextSceneSummonBatchRequest(value, speakerAgent, isInitialRequest: false, out var preparedRequest))
		{
			LogSceneSummonState("batch_followup_queued", preparedRequest, speakerAgent, ResolveAgentForLocationCharacter(preparedRequest.TargetLocationCharacter), "remaining=" + value.PendingTargets.Count, force: true);
			return;
		}
		CancelSceneSummonBatch(request.BatchId);
	}

	private static string BuildSceneSummonBatchTargetSummary(SceneSummonBatchState batch, SceneSummonPromptTarget currentTarget)
	{
		List<string> list = new List<string>();
		if (currentTarget != null && !string.IsNullOrWhiteSpace(currentTarget.DisplayName))
		{
			list.Add(currentTarget.DisplayName.Trim());
		}
		if (batch != null)
		{
			foreach (SceneSummonPromptTarget pendingTarget in batch.PendingTargets)
			{
				string text = pendingTarget?.DisplayName;
				if (!string.IsNullOrWhiteSpace(text))
				{
					text = text.Trim();
					if (!list.Contains(text))
					{
						list.Add(text);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return "那些人";
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		return string.Join("、", list);
	}

	private string BuildSceneSummonArrivedDisplayNames(int batchId, string fallbackTargetName)
	{
		if (batchId >= 0)
		{
			SceneSummonConversationSession sceneSummonConversationSession = _activeSceneSummonConversationSessions.FirstOrDefault((SceneSummonConversationSession x) => x != null && x.BatchId == batchId);
			if (sceneSummonConversationSession != null)
			{
				List<string> list = sceneSummonConversationSession.Participants.Where((SceneSummonConversationParticipant x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName)).Select((SceneSummonConversationParticipant x) => x.DisplayName.Trim()).Distinct().ToList();
				if (list.Count == 1)
				{
					return list[0];
				}
				if (list.Count > 1)
				{
					return string.Join("、", list);
				}
			}
		}
		return fallbackTargetName ?? "那个人";
	}

	private void TryRecordSceneSummonBatchCompletionFact(ActiveSceneSummonRequest request)
	{
		if (request == null || request.BatchId < 0 || request.SpeakerAgentIndex < 0)
		{
			return;
		}
		if (!_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) || value == null || value.CompletionFactRecorded)
		{
			return;
		}
		if (value.PendingTargets.Count > 0)
		{
			return;
		}
		int num = _activeSceneSummonRequests.Count((ActiveSceneSummonRequest x) => x != null && x.BatchId == request.BatchId);
		if (num > 1)
		{
			return;
		}
		string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "此人";
		}
		AppendTargetedSceneNpcFact("你已将" + text2 + "要求召集的" + text + "都带到了" + text2 + "身边。", request.SpeakerAgentIndex, persistHeroPrivateHistory: true);
		value.CompletionFactRecorded = true;
	}

	private string BuildSceneSummonClosurePromptInstruction(IEnumerable<NpcDataPacket> participants)
	{
		if (participants == null)
		{
			return "";
		}
		foreach (NpcDataPacket participant in participants)
		{
			if (participant == null || participant.AgentIndex < 0)
			{
				continue;
			}
			if (TryGetSceneSummonConversationSessionForAgentIndex(participant.AgentIndex) != null)
			{
				return "【当前是传唤后的会面】若你同意之后跟随此人，可在句末输出[FOL]；若想结束这次会面，可输出[END]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]；若此人改让你带路去找【可带路目标】或去见【可召目标】中的人，也可输出[GUI:id]。";
			}
		}
		return "";
	}

	private string BuildSceneFollowControlPromptInstruction(NpcDataPacket speaker)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && speaker != null && a.Index == speaker.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return "";
		}
		if (TryGetSceneSummonConversationSessionForAgentIndex(agent.Index) != null)
		{
			return "";
		}
		return IsAgentFollowingPlayerBySceneCommand(agent) ? "【当前正跟随玩家】若此人明确让你停止跟随且你同意，可在句末输出[STP]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]，多人可写[ASS:id1,id2]；若此人改让你带路去找【可带路目标】或去见【可召目标】中的人，也可输出[GUI:id]。" : "";
	}

	private static string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false)
	{
		if (npc == null)
		{
			return "";
		}
		string name = GetSceneNpcIdentityNameForPrompt(npc);
		string givenName = hero == null ? GetSceneNpcGivenNameForPrompt(npc) : "";
		string identity = (npc.RoleDesc ?? "").Trim();
		if (string.IsNullOrWhiteSpace(identity))
		{
			identity = "未知身份";
		}
		string personality = string.IsNullOrWhiteSpace(npc.PersonalityDesc) ? "暂无记录" : npc.PersonalityDesc.Trim();
		string background = string.IsNullOrWhiteSpace(npc.BackgroundDesc) ? "暂无记录" : npc.BackgroundDesc.Trim();
		string faction = "无（独立）";
		string clan = "无家族";
		string clanRole = "成员";
		string reputation = MyBehavior.GetClanTierReputationLabelForExternal(0) + "（0 level）";
		string culture = (npc.CultureId ?? "").Trim();
		string age = MyBehavior.BuildAgeBracketLabelForExternal(npc.Age);
		string equipment = "未知";
		string inventorySummary = "";
		string currentDateText = "";
		if (hero != null)
		{
			try
			{
				string clanName = (hero.Clan?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(clanName))
				{
					clan = clanName;
				}
				if (!string.IsNullOrWhiteSpace(clan) && !clan.EndsWith("家族", StringComparison.Ordinal))
				{
					clan += "家族";
				}
				int tier = hero.Clan?.Tier ?? 0;
				reputation = MyBehavior.GetClanTierReputationLabelForExternal(tier) + $"（{Math.Max(0, tier)} level）";
				string heroIdentity = MyBehavior.BuildHeroIdentityTitleForExternal(hero);
				if (!string.IsNullOrWhiteSpace(heroIdentity))
				{
					identity = heroIdentity.Trim();
				}
				string factionName = (hero.Clan?.Kingdom?.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(factionName))
				{
					factionName = (hero.MapFaction?.Name?.ToString() ?? "").Trim();
				}
				if (string.IsNullOrWhiteSpace(factionName))
				{
					factionName = clan;
				}
				if (!string.IsNullOrWhiteSpace(factionName))
				{
					faction = factionName;
				}
				if (hero.Clan?.Leader == hero)
				{
					clanRole = "族长";
				}
				string cultureName = (hero.Culture?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(cultureName))
				{
					culture = cultureName;
				}
				age = MyBehavior.BuildAgeBracketLabelForExternal(hero.Age);
				equipment = MyBehavior.BuildHeroEquipmentSummaryForExternal(hero);
				if (includeInventorySummary && RewardSystemBehavior.Instance != null)
				{
					inventorySummary = (RewardSystemBehavior.Instance.BuildInventorySummaryForAI(hero) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		else
		{
			string unnamedRank = (npc.UnnamedRank ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(unnamedRank))
			{
				clan = unnamedRank;
			}
			if (!string.IsNullOrWhiteSpace(clan) && clan != "无家族" && !clan.EndsWith("家族", StringComparison.Ordinal))
			{
				clan += "家族";
			}
			if (!string.IsNullOrWhiteSpace(culture))
			{
				faction = culture;
			}
			equipment = BuildNonHeroEquipmentSummaryForPrompt(npc);
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
				CharacterObject characterObject = agent?.Character as CharacterObject;
				if (includeInventorySummary && RewardSystemBehavior.Instance != null && characterObject != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
				{
					string text = RewardSystemBehavior.Instance.BuildSettlementMerchantInventorySummaryForAI(characterObject);
					if (!string.IsNullOrWhiteSpace(text))
					{
						inventorySummary = text.Trim();
					}
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(culture))
		{
			culture = "未知文化";
		}
		if (!culture.EndsWith("人", StringComparison.Ordinal))
		{
			culture += "人";
		}
		if (string.IsNullOrWhiteSpace(age))
		{
			age = "未知";
		}
		if (string.IsNullOrWhiteSpace(equipment))
		{
			equipment = "未知";
		}
		try
		{
			currentDateText = (MyBehavior.BuildCurrentDateFactForExternal() ?? "").Replace("\r", "").Trim();
			if (currentDateText.StartsWith("当前游戏日期：", StringComparison.Ordinal))
			{
				currentDateText = currentDateText.Substring("当前游戏日期：".Length).Trim();
			}
			int parenIndex = currentDateText.IndexOf('（');
			if (parenIndex >= 0)
			{
				currentDateText = currentDateText.Substring(0, parenIndex).Trim();
			}
		}
		catch
		{
			currentDateText = "";
		}
		bool hideReputation = ShouldHideSceneReputationForPrompt(npc, hero);
		StringBuilder stringBuilder = new StringBuilder();
		if (hero != null)
		{
			string clanRoleText = (clanRole == "族长") ? "统治者" : (hero.IsFemale ? "女性成员" : "男性成员");
			stringBuilder.Append("你名叫")
				.Append(name)
				.Append("，是")
				.Append(faction)
				.Append("的")
				.Append(clan)
				.Append("的")
				.Append(clanRoleText)
				.Append("，你的身份是")
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append("，你").Append(reputation);
			}
			stringBuilder.Append("。");
		}
		else
		{
			stringBuilder.Append("你是一个")
				.Append(name);
			if (!string.IsNullOrWhiteSpace(givenName) && !string.Equals(givenName, name, StringComparison.Ordinal))
			{
				stringBuilder.Append("，名叫").Append(givenName);
			}
			stringBuilder.Append("，你的身份是")
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append("，你").Append(reputation);
			}
			stringBuilder.Append("。");
		}
		stringBuilder.AppendLine()
			.Append("你身上穿着")
			.Append(equipment)
			.Append("。");
		if (hero != null)
		{
			stringBuilder.AppendLine()
				.Append("你的个性为：")
				.Append(personality)
				.Append("。")
				.AppendLine()
				.Append("你的背景是：")
				.Append(background)
				.Append("。");
		}
		stringBuilder.AppendLine()
			.Append("你的年纪是")
			.Append(age)
			.Append("，你是")
			.Append(culture)
			.Append("。");
		if (!string.IsNullOrWhiteSpace(currentDateText))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("现在的时间是").Append(currentDateText).Append("。");
		}
		string sceneLocationLine = BuildSceneLocationAndSettlementLineForPrompt(hero);
		if (!string.IsNullOrWhiteSpace(sceneLocationLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(sceneLocationLine);
		}
		string settlementHeroNpcLine = (ShoutUtils.BuildCurrentSettlementHeroNpcLineForPrompt() ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
		{
			const string prefix = "当前定居点HeroNPC：";
			if (settlementHeroNpcLine.StartsWith(prefix, StringComparison.Ordinal))
			{
				settlementHeroNpcLine = settlementHeroNpcLine.Substring(prefix.Length).Trim();
			}
			if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("这个定居点有这些人物：").Append(settlementHeroNpcLine);
			}
		}
		string settlementFlavorLine = BuildSettlementFlavorLineForPrompt(hero);
		if (!string.IsNullOrWhiteSpace(settlementFlavorLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementFlavorLine);
		}
		string settlementRulerPresenceLine = BuildSettlementRulerPresenceLineForPrompt();
		if (!string.IsNullOrWhiteSpace(settlementRulerPresenceLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementRulerPresenceLine);
		}
		string playerIntroLine = BuildScenePlayerIntroForPrompt(hero, includeTradePricing);
		if (!string.IsNullOrWhiteSpace(playerIntroLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerIntroLine);
		}
		string nearbyPresentNpcLine = BuildNearbyPresentNpcLineForPrompt(npc, presentNpcs);
		if (!string.IsNullOrWhiteSpace(nearbyPresentNpcLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(nearbyPresentNpcLine);
		}
		if (includeInventorySummary && !string.IsNullOrWhiteSpace(inventorySummary))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(hero != null ? "【NPC当前可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说" : "【当前商铺可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说）");
			stringBuilder.AppendLine();
			stringBuilder.Append(inventorySummary);
		}
		string customPromptRule = (DuelSettings.GetSettings()?.PlayerCustomPromptRule ?? "").Replace("\r", "").Trim();
		stringBuilder.AppendLine();
		stringBuilder.Append("请遵循以下规则参与互动：");
		if (!string.IsNullOrWhiteSpace(customPromptRule))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(customPromptRule);
		}
		return stringBuilder.ToString().Trim();
	}

	private static bool ShouldHideSceneReputationForPrompt(NpcDataPacket npc, Hero hero)
	{
		try
		{
			if (hero != null)
			{
				if (hero.IsWanderer)
				{
					return true;
				}
				if (hero.Occupation == Occupation.Headman)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			string role = (npc?.RoleDesc ?? "").Trim();
			if (string.Equals(role, "士兵", StringComparison.Ordinal))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8)
	{
		if (npc == null)
		{
			return "未知";
		}
		Agent agent = null;
		try
		{
			agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		}
		catch
		{
			agent = null;
		}
		if (agent == null || !agent.IsActive())
		{
			return "未知";
		}
		bool useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
			}
			else
			{
				Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
				useCivilianEquipment = settlement != null && !settlement.IsVillage;
			}
		}
		catch
		{
			useCivilianEquipment = false;
		}
		string contextLabel = useCivilianEquipment ? "常服" : "战斗装";
		try
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9]
			{
				EquipmentIndex.NumAllWeaponSlots,
				EquipmentIndex.Body,
				EquipmentIndex.Leg,
				EquipmentIndex.Gloves,
				EquipmentIndex.Cape,
				EquipmentIndex.WeaponItemBeginSlot,
				EquipmentIndex.Weapon1,
				EquipmentIndex.Weapon2,
				EquipmentIndex.Weapon3
			};
			EquipmentIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EquipmentIndex equipmentIndex = array2[i];
				ItemObject itemObject = null;
				try
				{
					itemObject = agent.SpawnEquipment[equipmentIndex].Item;
				}
				catch
				{
					itemObject = null;
				}
				if (itemObject == null)
				{
					try
					{
						itemObject = agent.Equipment[equipmentIndex].Item;
					}
					catch
					{
						itemObject = null;
					}
				}
				if (itemObject == null)
				{
					continue;
				}
				string text = (itemObject.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = equipmentIndex.ToString();
				}
				string text2 = (itemObject.Name?.ToString() ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = text;
				}
				if (!dictionary.ContainsKey(text))
				{
					dictionary[text] = 0;
					dictionary2[text] = text2;
				}
				dictionary[text]++;
			}
			if (dictionary.Count == 0)
			{
				return contextLabel + "：未读取到可识别装备";
			}
			List<string> list = (from x in (from x in dictionary.Select(delegate(KeyValuePair<string, int> kv)
					{
						string value;
						string name = (dictionary2.TryGetValue(kv.Key, out value) ? value : kv.Key);
						return new
						{
							Name = name,
							Count = kv.Value
						};
					})
					orderby x.Count descending
					select x).ThenBy(x => x.Name, StringComparer.Ordinal).Take(Math.Max(1, maxEntries))
				select x.Name + "x" + x.Count).ToList();
			return contextLabel + "：" + string.Join("、", list);
		}
		catch
		{
			return contextLabel + "：读取装备失败";
		}
	}

	private static string BuildSceneSystemTopPromptIntroForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false)
	{
		return BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs, includeInventorySummary, includeTradePricing);
	}

	private static string BuildSceneSystemTopPromptIntroForGroup(IEnumerable<NpcDataPacket> npcs, Dictionary<int, Hero> resolvedHeroes)
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
			string intro = BuildSceneNpcRoleIntroForPrompt(npc, hero, npcs);
			if (string.IsNullOrWhiteSpace(intro))
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine(intro);
		}
		return stringBuilder.ToString().Trim();
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

	private static string BuildScenePlayerIntroForPrompt(bool includeTradePricing = false)
	{
		return BuildScenePlayerIntroForPrompt(null, includeTradePricing);
	}

	private static string BuildScenePlayerIntroForPrompt(Hero observerHero, bool includeTradePricing = false)
	{
		Hero playerHero = Hero.MainHero;
		if (playerHero == null)
		{
			return "";
		}
		string culture = "未知文化";
		string age = "未知";
		string equipment = "未知";
		string equipmentValueInline = "";
		string identitySentence = BuildPlayerSceneIdentitySentenceForPrompt(playerHero);
		int clanTier = 0;
		string clanName = "无家族";
		bool isClanLeader = false;
		try
		{
			string cultureName = (playerHero.Culture?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(cultureName))
			{
				culture = cultureName;
			}
		}
		catch
		{
		}
		if (!culture.EndsWith("人", StringComparison.Ordinal))
		{
			culture += "人";
		}
		try
		{
			age = MyBehavior.BuildAgeBracketLabelForExternal(playerHero.Age);
		}
		catch
		{
			age = "未知";
		}
		try
		{
			equipment = MyBehavior.BuildHeroEquipmentSummaryForExternal(playerHero);
		}
		catch
		{
			equipment = "未知";
		}
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					equipmentValueInline = (RewardSystemBehavior.Instance.BuildVisibleEquipmentActualValueInlineFactForAI(playerHero) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		try
		{
			clanTier = playerHero.Clan?.Tier ?? 0;
			string rawClanName = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(rawClanName))
			{
				clanName = rawClanName;
			}
			if (!string.IsNullOrWhiteSpace(clanName) && clanName != "无家族" && !clanName.EndsWith("家族", StringComparison.Ordinal))
			{
				clanName += "家族";
			}
			isClanLeader = playerHero.Clan?.Leader == playerHero;
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool forceDetailed = ShouldForceDetailedPlayerIntroForObserver(observerHero);
		if (clanTier >= 2 || forceDetailed)
		{
			string playerPublicName = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerPublicName) || string.Equals(playerPublicName, "玩家", StringComparison.Ordinal))
			{
				playerPublicName = (playerHero.Name?.ToString() ?? "").Trim();
			}
			string reputation = MyBehavior.GetClanTierReputationLabelForExternal(clanTier) + $"（{Math.Max(0, clanTier)} level）";
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，你知道他叫")
				.Append(string.IsNullOrWhiteSpace(playerPublicName) ? "这人" : playerPublicName)
				.Append("，他")
				.Append(reputation)
				.Append("，是")
				.Append(clanName)
				.Append("的")
				.Append(isClanLeader ? "族长" : "成员")
				.Append("。从面貌上来看，是一个")
				.Append(age)
				.Append("，穿着")
				.Append(equipment)
				.Append("。");
		}
		else
		{
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，他看起来是个普通人，总之不是贵族，从他的面貌上来看，是一个")
				.Append(age)
				.Append("，穿着")
				.Append(equipment)
				.Append("。");
		}
		if (!string.IsNullOrWhiteSpace(equipmentValueInline))
		{
			stringBuilder.Append(equipmentValueInline).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(identitySentence))
		{
			stringBuilder.Append(identitySentence);
		}
		return stringBuilder.ToString().Trim();
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
			if (presentNpc.AgentIndex == selfNpc.AgentIndex)
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
			int extraIndex = sceneDescription.IndexOf('|');
			if (extraIndex >= 0)
			{
				sceneDescription = sceneDescription.Substring(0, extraIndex).Trim();
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

	private static Settlement FindNearestSettlementForPrompt()
	{
		try
		{
			CampaignVec2? campaignVec = MobileParty.MainParty?.Position;
			if (!campaignVec.HasValue || !campaignVec.Value.IsValid())
			{
				return null;
			}
			Vec2 vec = campaignVec.Value.ToVec2();
			Settlement settlement = null;
			float num = float.MaxValue;
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				Vec2 vec2 = item.GatePosition.ToVec2();
				float num2 = vec2.x - vec.x;
				float num3 = vec2.y - vec.y;
				float num4 = num2 * num2 + num3 * num3;
				if (num4 < num)
				{
					num = num4;
					settlement = item;
				}
			}
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSceneSettlementForPrompt(string placeName)
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
		return settlement ?? FindNearestSettlementForPrompt();
	}

	private static string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			ParseCurrentScenePlaceAndSpotForPrompt(out var placeName, out var spotName);
			Settlement settlement = ResolveSceneSettlementForPrompt(placeName);
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
				spotName = "某处";
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
			if (isRuledByPerspectiveHero)
			{
				return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
			}
			return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
		}
		catch
		{
			return "";
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

	private static bool ContainsLiteralKeywordHit(string input, List<string> keywords)
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

	private static bool TryRenderSceneHistoryLine(ConversationMessage msg, HashSet<string> allowedSpeakers, out string rendered, int viewerAgentIndex = -1, string fallbackTargetNpcName = "", bool useNpcNameAddress = false)
	{
		rendered = "";
		if (msg == null)
		{
			return false;
		}
		string text = (msg.Role ?? "").Trim().ToLowerInvariant();
		string text2 = (msg.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		if (IsLeakedPromptLineForShout(text2))
		{
			return false;
		}
		if (text2.Length > 300)
		{
			text2 = text2.Substring(0, 300) + "…";
		}
		switch (text)
		{
		case "assistant":
		{
			string text3 = (msg.SpeakerName ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && allowedSpeakers != null && allowedSpeakers.Count > 0 && !allowedSpeakers.Contains(text3))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "某NPC";
			}
			if (text2.StartsWith(text3 + ":", StringComparison.Ordinal) || text2.StartsWith(text3 + "：", StringComparison.Ordinal))
			{
				rendered = text2;
			}
			else
			{
				rendered = text3 + ": " + text2;
			}
			return true;
		}
		case "user":
		{
			string text4 = (msg.TargetName ?? "").Trim();
			bool flag = false;
			if (msg.TargetAgentIndex >= 0)
			{
				flag = msg.TargetAgentIndex != viewerAgentIndex;
			}
			else if (useNpcNameAddress && !string.IsNullOrWhiteSpace(fallbackTargetNpcName))
			{
				flag = true;
				text4 = fallbackTargetNpcName;
			}
			rendered = NormalizeScenePlayerHistoryLine(text2, text4, flag);
			return true;
		}
		case "system":
			if (text2.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text2.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
			{
				rendered = text2;
			}
			else
			{
				rendered = "[系统事实] " + text2;
			}
			return true;
		default:
			rendered = text2;
			return true;
		}
	}

	private static bool IsLeakedPromptLineForShout(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("必须遵守【", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("必须严格遵守", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【对话历史】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【历史对话记录】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【近期对话窗口】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【玩家与", StringComparison.Ordinal) && text.EndsWith("（NPC名称的对话与互动）的近期对话】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【当前场景公共对话与互动】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【长期记忆摘要】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【当前对话】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【回复要求】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【输出要求】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【指令】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【请严格按照格式输出】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【常驻发言格式】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【群体对话规则】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【附加规则", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("现在，请根据以上所有信息", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("请根据以上所有信息", StringComparison.Ordinal))
		{
			return true;
		}
		if (Regex.IsMatch(text, "^\\d+\\.\\s*必须", RegexOptions.CultureInvariant))
		{
			return true;
		}
		if (text.StartsWith("【NPC当前可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("Gold:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("第纳尔:", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("InventoryItems:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("BattleEquipment:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.IndexOf("|guidePrice=", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.IndexOf("|lineValue=", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.StartsWith("【玩家触发了（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【触发相关话题/背景】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("这是一场多人聊天", StringComparison.Ordinal) || text.StartsWith("其他人也要说话", StringComparison.Ordinal) || text.StartsWith("如果你觉得下一位还必须接话", StringComparison.Ordinal) || text.StartsWith("若你觉得在你这句之后", StringComparison.Ordinal) || text.StartsWith("如果你觉得这句之后还值得继续聊下去", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	private static string StripLeakedPromptFragmentsForShout(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		string[] array = new string[11]
		{
			"这是一场多人聊天",
			"其他人也要说话",
			"如果你觉得下一位还必须接话",
			"若你觉得在你这句之后",
			"如果你觉得这句之后还值得继续聊下去",
			"接力编号必须从【在场人物列表】",
			"id 必须从【在场人物列表】",
			"你不能选你自己",
			"才能让其他人发言",
			"如果不必继续，就不要输出任何续聊标签",
			"若不必继续，就不要输出任何续聊标签"
		};
		string[] array2 = text2.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		for (int i = 0; i < array2.Length; i++)
		{
			string text3 = array2[i] ?? "";
			int num = -1;
			for (int j = 0; j < array.Length; j++)
			{
				int num2 = text3.IndexOf(array[j], StringComparison.Ordinal);
				if (num2 >= 0 && (num < 0 || num2 < num))
				{
					num = num2;
				}
			}
			if (num >= 0)
			{
				text3 = text3.Substring(0, num).TrimEnd();
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(text3);
		}
		return stringBuilder.ToString().Trim();
	}

	private static string StripLeakedPromptContentForShout(string text)
	{
		string text2 = StripLeakedPromptFragmentsForShout((text ?? "").Replace("\r", ""));
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		string[] array = text2.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = (array[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && !IsLeakedPromptLineForShout(text3))
			{
				stringBuilder.AppendLine(text3);
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private static string StripStageDirectionsForPassiveShout(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = Regex.Replace(text2, "\\（[^）\\n]{0,120}\\）", "");
		text2 = Regex.Replace(text2, "\\([^\\)\\n]{0,120}\\)", "");
		text2 = Regex.Replace(text2, "\\*[^\\*\\n]{0,120}\\*", "");
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		text2 = text2.Trim();
		text2 = text2.TrimStart('，', '。', '、', '；', '：', ',', ';', ':');
		return text2.Trim();
	}

	private static string StripActionTagsForSceneSpeech(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
	}

	private static string StripNpcNamePrefixSafely(string text, int maxPrefixLength = 30)
	{
		return ShoutUtils.StripNamePrefixedLineSafely(text, maxPrefixLength);
	}

	private static string SanitizeSceneSpeechText(string text)
	{
		string text2 = StripLeakedPromptContentForShout(text);
		text2 = StripStageDirectionsForPassiveShout(text2);
		text2 = StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "[ \\t]{2,}", " ");
		return text2.Trim(' ', '\t', '\r', '\n', '[', ']', ':');
	}

	private static bool ContainsAutoGroupEndSignal(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.IndexOf("[END]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[STP]", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool TryParseAutoGroupRelayTargetAgentIndex(string text, out int relayTargetAgentIndex)
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
		string text2 = (text ?? "").Replace("\r", "");
		text2 = Regex.Replace(text2, "\\[NO_CONTINUE\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[END\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", "", RegexOptions.IgnoreCase);
		return text2.Trim();
	}

	private static string StripAutoGroupRelaySignal(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[RELAY\\s*:[^\\]\\r\\n]+\\]", "", RegexOptions.IgnoreCase).Trim();
	}

	private static bool IsPrivateRecentWindowHeader(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("【近期对话窗口】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【玩家与", StringComparison.Ordinal) && text.EndsWith("（NPC名称的对话与互动）的近期对话】", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	private static string BuildScenePublicHistorySection(List<string> sceneHistoryLines)
	{
		List<string> lines = new List<string>();
		HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);
		if (sceneHistoryLines != null)
		{
			foreach (string sceneHistoryLine in sceneHistoryLines)
			{
				string text = (sceneHistoryLine ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !IsLeakedPromptLineForShout(text) && dedupe.Add(text))
				{
					lines.Add(text);
				}
			}
		}
		return (lines.Count == 0) ? "【当前场景公共对话与互动】\n无" : ("【当前场景公共对话与互动】\n" + string.Join("\n", lines));
	}

private static string NormalizeScenePlayerHistoryLine(string text, string targetNpcName = "", bool useNpcNameAddress = false)
	{
		string text2 = (text ?? "").Trim();
		string text3 = GetPlayerDisplayNameForShout() + (useNpcNameAddress && !string.IsNullOrWhiteSpace(targetNpcName) ? ("对" + targetNpcName + "说") : "对你说");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text3 + ":";
		}
		if (text2.StartsWith(text3 + ":", StringComparison.Ordinal) || text2.StartsWith(text3 + "：", StringComparison.Ordinal))
		{
			return text2;
		}
		if (text2.StartsWith("玩家:", StringComparison.Ordinal) || text2.StartsWith("玩家：", StringComparison.Ordinal) || text2.StartsWith("你:", StringComparison.Ordinal) || text2.StartsWith("你：", StringComparison.Ordinal))
		{
			int num = text2.IndexOfAny(new char[2] { ':', '：' });
			string value = ((num >= 0 && num + 1 < text2.Length) ? text2.Substring(num + 1).Trim() : "");
			return text3 + ": " + value;
		}
		return text3 + ": " + text2;
	}

	private static void SplitPersistedHeroHistorySections(string persistedHeroHistory, out string privateRecentWindowSection, out string persistedWithoutRecentWindow)
	{
		privateRecentWindowSection = "";
		persistedWithoutRecentWindow = "";
		if (string.IsNullOrWhiteSpace(persistedHeroHistory))
		{
			return;
		}
		string[] array = persistedHeroHistory.Replace("\r", "").Split('\n');
		bool capturePrivate = false;
		StringBuilder privateSb = new StringBuilder(persistedHeroHistory.Length);
		StringBuilder othersSb = new StringBuilder(persistedHeroHistory.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string raw = array[i] ?? "";
			string line = raw.Trim();
			bool isHeader = line.StartsWith("【", StringComparison.Ordinal) && line.EndsWith("】", StringComparison.Ordinal);
			if (IsPrivateRecentWindowHeader(line))
			{
				capturePrivate = true;
				privateSb.AppendLine(line);
				continue;
			}
			if (capturePrivate && isHeader)
			{
				capturePrivate = false;
			}
			if (capturePrivate)
			{
				if (!string.IsNullOrWhiteSpace(line))
				{
					privateSb.AppendLine(line);
				}
			}
			else if (!string.IsNullOrWhiteSpace(line))
			{
				othersSb.AppendLine(raw);
			}
		}
		privateRecentWindowSection = privateSb.ToString().Trim();
		persistedWithoutRecentWindow = othersSb.ToString().Trim();
	}

	private static string BuildSceneFirstMeetingNpcFactSection(Hero hero)
	{
		string text = MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero);
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

	private void EnqueueSystemNpcShout(Agent speakerAgent, string content)
	{
		try
		{
			if (speakerAgent == null || !speakerAgent.IsActive() || string.IsNullOrWhiteSpace(content) || Mission.Current == null)
			{
				return;
			}
			List<Agent> list = ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
			if (!list.Any((Agent a) => a != null && a.Index == speakerAgent.Index))
			{
				list.Add(speakerAgent);
			}
			List<NpcDataPacket> allNpcData = (from a in list
				where a != null
				select ShoutUtils.ExtractNpcData(a) into d
				where d != null
				select d).ToList();
			ApplySceneLocalDisambiguatedNames(allNpcData);
			NpcDataPacket speakerData = ShoutUtils.ExtractNpcData(speakerAgent);
			if (speakerData == null)
			{
				return;
			}
			NpcDataPacket npcDataPacket = allNpcData.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == speakerData.AgentIndex);
			if (npcDataPacket != null)
			{
				speakerData.PromptGivenName = npcDataPacket.PromptGivenName;
				speakerData.PromptDisplayName = npcDataPacket.PromptDisplayName;
			}
			string safeContent = content.Trim();
			_mainThreadActions.Enqueue(delegate
			{
				try
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerData.AgentIndex);
					string aiResponse = safeContent;
					bool meetingTauntEscalated = false;
					bool sceneTauntActionHandled = false;
					bool sceneTauntEscalated = false;
					try
					{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref aiResponse);
								DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref aiResponse);
								DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref aiResponse);
								VanillaIssueOfferBridge.ApplyIssueOfferTags(characterObject.HeroObject, ref aiResponse);
								if (RewardSystemBehavior.Instance != null)
								{
									RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref aiResponse);
									List<string> list2 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
									if (list2 != null)
									{
										foreach (string item in list2)
										{
											RecordSystemFactForNearbySafe(allNpcData, item);
										}
									}
								}
								if (RomanceSystemBehavior.Instance != null)
								{
									RomanceSystemBehavior.Instance.ApplyMarriageTags(characterObject.HeroObject, Hero.MainHero, ref aiResponse);
								}
								if (MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject.HeroObject, characterObject, speakerData.AgentIndex, ref aiResponse, out var generatedFacts, out var notifications))
								{
									if (generatedFacts != null)
									{
										foreach (string generatedFact in generatedFacts)
										{
											RecordSystemFactForNearbySafe(allNpcData, generatedFact);
											MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, generatedFact);
										}
									}
									if (notifications != null)
									{
										foreach (string notification in notifications)
										{
											if (!string.IsNullOrWhiteSpace(notification))
											{
												InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
											}
										}
									}
								}
								if (!ShouldSuppressSceneConversationControlForMeeting())
								{
									LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref aiResponse, out meetingTauntEscalated);
								}
								else
								{
									StripMeetingTauntTagsForSceneConversation(ref aiResponse);
								}
								sceneTauntActionHandled = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
							}
						else
						{
							MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(speakerData.UnnamedKey, speakerData.Name, ref aiResponse);
							if (agent != null && agent.Character is CharacterObject characterObject2 && RewardSystemBehavior.Instance != null)
							{
								RewardSystemBehavior.Instance.ApplyMerchantRewardTags(characterObject2, Hero.MainHero, ref aiResponse);
								List<string> list = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
								if (list != null)
								{
									foreach (string item2 in list)
									{
										RecordSystemFactForNearbySafe(allNpcData, item2);
									}
								}
							}
							if (agent != null && agent.Character is CharacterObject characterObject4 && MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject4.HeroObject, characterObject4, speakerData.AgentIndex, ref aiResponse, out var generatedFacts2, out var notifications2))
							{
								if (generatedFacts2 != null)
								{
									foreach (string generatedFact2 in generatedFacts2)
									{
										RecordSystemFactForNearbySafe(allNpcData, generatedFact2);
										if (characterObject4.HeroObject != null)
										{
											MyBehavior.AppendExternalDialogueHistory(characterObject4.HeroObject, null, null, generatedFact2);
										}
									}
								}
								if (notifications2 != null)
								{
									foreach (string notification2 in notifications2)
									{
										if (!string.IsNullOrWhiteSpace(notification2))
										{
											InformationManager.DisplayMessage(new InformationMessage(notification2, new Color(0.4f, 1f, 0.4f)));
										}
									}
								}
							}
							if (agent != null && agent.Character is CharacterObject characterObject3)
							{
								sceneTauntActionHandled = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
							}
						}
					}
					catch
					{
					}
					if (sceneTauntActionHandled && string.IsNullOrWhiteSpace(aiResponse))
					{
						aiResponse = BuildFallbackSceneTauntSpeech(sceneTauntEscalated);
					}
					bool flag = false;
					try
					{
						flag = !meetingTauntEscalated && !sceneTauntEscalated && ShoutUtils.TryTriggerDuelAction(speakerData, ref aiResponse);
					}
					catch
					{
					}
					if (!string.IsNullOrWhiteSpace(aiResponse))
					{
						ShowNpcSpeechOutput(speakerData, agent, aiResponse);
						if (agent != null && agent.IsActive())
						{
							AddAgentToStareList(agent, interruptCurrentUse: false);
						}
						RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);
						PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);
					}
					if (flag)
					{
						ReleaseSceneConversationConstraints(allNpcData, speakerData.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
						DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
						ShoutUtils.ExecuteDuel(agent);
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
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
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
			Dictionary<string, string> dictionary = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
				dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage);
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

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
	}

	private void OnMissionStarted(IMission mission)
	{
		if (mission == null || Mission.Current == null)
		{
			return;
		}
		lock (_historyLock)
		{
			_npcConversationHistory.Clear();
			_publicConversationHistory.Clear();
			_sceneHeroRevisitHandledThisSession.Clear();
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_currentStareTarget = null;
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_interactionGraceTimer = 0f;
		_passiveCooldowns.Clear();
		_activeInteractionSessions.Clear();
		_pendingInteractionTimeoutArms.Clear();
		lock (_speechQueueLock)
		{
			_speechQueue.Clear();
		}
		_speechWorkerRunning = false;
		_sceneConversationEpoch = 0;
		Action result;
		while (_mainThreadActions.TryDequeue(out result))
		{
		}
		Interlocked.Increment(ref _sceneHistorySessionId);
		RagWarmupCoordinator.TryStartBackgroundWarmup("mission_start");
		AIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start");
		Mission.Current.AddMissionBehavior(new ShoutMissionBehavior(this));
		Mission.Current.AddMissionBehavior(new FloatingTextMissionView());
		SubscribeTtsPlaybackEvents();
		try
		{
			if (!ShoutUtils.IsInValidScene())
			{
				return;
			}
			string text = "K";
			try
			{
				DuelSettings settings = DuelSettings.GetSettings();
				if (settings != null && !string.IsNullOrWhiteSpace(settings.ShoutKey))
				{
					text = settings.ShoutKey.Trim().ToUpperInvariant();
				}
			}
			catch
			{
				text = "K";
			}
			InformationManager.DisplayMessage(new InformationMessage("按（" + text + "）与NPC交流", new Color(0.95f, 0.85f, 0.2f)));
		}
		catch
		{
		}
	}

	private void SubscribeTtsPlaybackEvents()
	{
		try
		{
			lock (_speakingLock)
			{
				_speakingAgentIndices.Clear();
				_agentLipSyncDetachedForSafety.Clear();
			}
			_agentSoundEvents.Clear();
			_agentWavPaths.Clear();
			_agentXmlPaths.Clear();
			ClearPendingTtsBubbleSyncQueues();
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			lock (_ttsEventSubLock)
			{
				if (_ttsEventSubscribedOwner != null && _ttsEventSubscribedOwner != this)
				{
					_ttsEventSubscribedOwner.UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: false);
					Logger.Log("LipSync", "[WARN] 检测到旧 ShoutBehavior 残留订阅，已自动解绑旧实例");
				}
				UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: false);
				_ttsOnAudioFileReadyHandler = delegate(int agentIndex, string wavPath, string xmlPath, float durationSecs)
				{
					if (agentIndex >= 0)
					{
						Logger.Log("LipSync", $"[OnAudioFileReady] agentIndex={agentIndex}, wav={wavPath}, xml={xmlPath}, dur={durationSecs:F2}s");
						EnqueuePendingAudioDuration(agentIndex, durationSecs);
						LogTtsReport("AudioFileReady", agentIndex, $"duration={durationSecs:F2};wav={System.IO.Path.GetFileName(wavPath)};xml={System.IO.Path.GetFileName(xmlPath)}");
						bool flag = false;
						lock (_ttsBubbleSyncLock)
						{
							flag = _ttsPlaybackStartedAgents.Contains(agentIndex);
						}
						if (flag)
						{
							_mainThreadActions.Enqueue(delegate
							{
								try
								{
									if (!TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration: false))
									{
										ClearOrphanPendingAudioDuration(agentIndex);
									}
								}
								catch
								{
								}
							});
						}
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								LogTtsReport("AudioFileReady.MainThreadStart", agentIndex, $"wavExists={(!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath))};xmlExists={(!string.IsNullOrWhiteSpace(xmlPath) && File.Exists(xmlPath))}");
								if (IsInEscapeTransitionWindow())
								{
									try
									{
										if (!string.IsNullOrEmpty(wavPath) && File.Exists(wavPath))
										{
											File.Delete(wavPath);
										}
									}
									catch
									{
									}
									try
									{
										if (!string.IsNullOrEmpty(xmlPath) && File.Exists(xmlPath))
										{
											File.Delete(xmlPath);
										}
										return;
									}
									catch
									{
										return;
									}
								}
								if (_enableRhubarbSoundEventPlayback && Mission.Current != null && !(Mission.Current.Scene == null))
								{
									Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
									if (agent != null && agent.IsActive())
									{
										CleanupPreviousLipSyncPlaybackForReplacement("OnAudioFileReady.ReplaceExisting");
								LogLipSyncNativeProbe("CreateEventFromExternalFile.Before", agentIndex, "wav=" + System.IO.Path.GetFileName(wavPath));
								SoundEvent soundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", wavPath, Mission.Current.Scene, is3d: false, isBlocking: false);
								if (soundEvent == null)
								{
									Logger.Log("LipSync", $"[WARN] SoundEvent.CreateEventFromExternalFile 返回 null, agentIndex={agentIndex}");
									LogTtsReport("AudioFileReady.CreateSoundEventNull", agentIndex, $"wav={System.IO.Path.GetFileName(wavPath)}");
									QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.CreateSoundEventNull", agentIndex);
										}
								else
								{
									LogTtsReport("AudioFileReady.SoundEventCreated", agentIndex, $"wav={System.IO.Path.GetFileName(wavPath)}");
									LogLipSyncNativeProbe("SoundEvent.SetPosition.Before", agentIndex);
									soundEvent.SetPosition(agent.Position);
									LogLipSyncNativeProbe("SoundEvent.SetPosition.After", agentIndex);
									LogLipSyncNativeProbe("SoundEvent.Play.Before", agentIndex);
									soundEvent.Play();
									LogLipSyncNativeProbe("SoundEvent.Play.After", agentIndex);
									float num = 0f;
									bool flag = true;
									try
									{
										flag = DuelSettings.GetSettings()?.TtsSceneUseWinmmAudible ?? true;
												num = DuelSettings.GetSettings()?.TtsLipSyncSoundEventVolume ?? 0f;
											}
											catch
											{
												flag = true;
												num = 0f;
											}
											if (flag)
											{
												num = 0f;
											}
											if (num < 0f)
											{
												num = 0f;
											}
											if (num > 1f)
											{
												num = 1f;
											}
											try
											{
												soundEvent.SetParameter("Volume", num);
											}
											catch (Exception ex2)
									{
										Logger.Log("LipSync", "[WARN] 设置 SoundEvent 音量失败(Volume): " + ex2.Message);
									}
									LogLipSyncNativeProbe("SoundEvent.GetSoundId.Before", agentIndex);
									int soundId = soundEvent.GetSoundId();
									LogLipSyncNativeProbe("SoundEvent.GetSoundId.After", agentIndex, "soundId=" + soundId);
									if (soundId <= 0)
									{
										Logger.Log("LipSync", $"[WARN] SoundEvent.GetSoundId 非法({soundId})，跳过 StartRhubarbRecord, agentIndex={agentIndex}");
										SafeStopAndReleaseSoundEvent(soundEvent);
										QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.InvalidSoundId", agentIndex);
									}
											else
											{
												string text = "";
												bool flag2 = CanAgentUseSceneLipSync(agent, out text);
												Logger.Log("LipSync", $"[Rhubarb] SoundEvent created, vol={num:F2}, agentIndex={agentIndex}, soundId={soundId}, lipSyncSafe={flag2}, reason={text}");
												lock (_speakingLock)
												{
													if (flag2)
													{
														_agentLipSyncDetachedForSafety.Remove(agentIndex);
													}
													else
													{
														_agentLipSyncDetachedForSafety.Add(agentIndex);
													}
													_agentSoundEvents[agentIndex] = soundEvent;
													_agentWavPaths[agentIndex] = wavPath;
													_agentXmlPaths[agentIndex] = xmlPath;
												}
												if (flag2)
												{
													PrepareAgentForTrueLipSyncIfPossible(agent);
													LogLipSyncNativeProbe("StartRhubarbRecord.Before", agentIndex, $"soundId={soundId};xml={System.IO.Path.GetFileName(xmlPath)}");
													agent.AgentVisuals.StartRhubarbRecord(xmlPath, soundId);
													LogLipSyncNativeProbe("StartRhubarbRecord.After", agentIndex, "soundId=" + soundId);
													Logger.Log("LipSync", $"[Rhubarb] StartRhubarbRecord 调用成功, agentIndex={agentIndex}, soundId={soundId}");
												}
												else
												{
													Logger.Log("LipSync", $"[SAFEGUARD] Skip StartRhubarbRecord for unsafe scene agent. agentIndex={agentIndex}, reason={text}");
													LogTtsReport("AudioFileReady.LipSyncDetached", agentIndex, "reason=" + text);
												}
										}
									}
									}
									else
									{
										LogTtsReport("AudioFileReady.AgentUnavailable", agentIndex, $"agentMissing={(agent == null)};active={(agent != null && agent.IsActive())}");
										QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.AgentUnavailable", agentIndex);
									}
								}
								LogTtsReport("AudioFileReady.MainThreadEnd", agentIndex);
							}
							catch (Exception ex3)
							{
								QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.MainThreadFailed", agentIndex);
								Logger.Log("LipSync", "[ERROR] OnAudioFileReady 主线程处理失败: " + ex3.Message);
								LogTtsReport("AudioFileReady.MainThreadFailed", agentIndex, "error=" + ex3.Message);
								BannerlordExceptionSentinel.ReportObservedException("LipSync.OnAudioFileReady.MainThread", ex3, "agentIndex=" + agentIndex);
							}
						});
					}
				};
				_ttsOnPlaybackStartedHandler = delegate(int agentIndex)
				{
					if (agentIndex >= 0)
					{
						if (!CanAgentParticipateInSceneSpeechExternal(agentIndex))
						{
							lock (_ttsBubbleSyncLock)
							{
								_pendingNpcBubbleQueues.Remove(agentIndex);
								_pendingAudioDurationQueues.Remove(agentIndex);
								_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
								_pendingSceneDialogueFeedQueues.Remove(agentIndex);
							}
							LogTtsReport("PlaybackStarted.InvalidAgent", agentIndex);
							return;
						}
						string text = "";
						bool flag2 = CanAgentUseSceneLipSyncExternal(agentIndex, out text);
						lock (_speakingLock)
						{
							if (flag2)
							{
								_agentLipSyncDetachedForSafety.Remove(agentIndex);
								_speakingAgentIndices.Add(agentIndex);
							}
							else
							{
								_speakingAgentIndices.Remove(agentIndex);
								_agentLipSyncDetachedForSafety.Add(agentIndex);
							}
						}
						Logger.Log("LipSync", $"[OnPlaybackStarted] agentIndex={agentIndex}, lipSyncSafe={flag2}, reason={text}");
						LogTtsReport("PlaybackStarted", agentIndex, $"lipSyncSafe={flag2};reason={text}");
						lock (_ttsBubbleSyncLock)
						{
							_ttsPlaybackStartedAgents.Add(agentIndex);
						}
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								bool flag3 = TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration: false);
								if (!flag3)
								{
									SchedulePendingNpcBubbleFallbackDispatch(agentIndex);
								}
							}
							catch (Exception ex4)
							{
								Logger.Log("LipSync", $"[ERROR] PlaybackStarted bubble dispatch failed, agentIndex={agentIndex}, error={ex4.Message}");
								LogTtsReport("PlaybackStarted.BubbleDispatchFailed", agentIndex, "error=" + ex4.Message);
								BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackStarted.BubbleDispatch", ex4, "agentIndex=" + agentIndex);
							}
						});
					}
				};
				_ttsOnPlaybackFinishedHandler = delegate(int agentIndex)
				{
					if (agentIndex >= 0)
					{
						long interactionToken = 0L;
						bool hasInteractionToken = TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);
						Agent finishedAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
						bool hostileFinishedSpeech = IsAgentHostileToMainAgent(finishedAgent);
						if (hostileFinishedSpeech)
						{
							hasInteractionToken = false;
							interactionToken = 0L;
							_pendingInteractionTimeoutArms.Remove(agentIndex);
							_activeInteractionSessions.Remove(agentIndex);
						}
						lock (_speakingLock)
						{
							_speakingAgentIndices.Remove(agentIndex);
						}
						lock (_ttsBubbleSyncLock)
						{
							_ttsPlaybackStartedAgents.Remove(agentIndex);
						}
						Logger.Log("LipSync", $"[OnPlaybackFinished] agentIndex={agentIndex} removed from speaking set");
						LogTtsReport("PlaybackFinished", agentIndex, $"hasInteractionToken={hasInteractionToken};interactionToken={interactionToken};hostileFinishedSpeech={hostileFinishedSpeech}");
						if (hasInteractionToken)
						{
							_mainThreadActions.Enqueue(delegate
							{
								try
								{
									ArmActiveInteractionTimeoutNow(agentIndex, interactionToken);
								}
								catch
								{
								}
							});
						}
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushPendingSceneDialogueFeedAfterSpeech(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushSceneFollowCommandAfterSpeech(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushSceneSummonReturnAfterSpeech(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushSceneGuideReturnAfterSpeech(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushSceneAutonomyRestoreAfterSpeech(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								FlushPendingSceneSummonLaunches(agentIndex);
								FlushPendingSceneGuideLaunches(agentIndex);
							}
							catch
							{
							}
						});
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
								LogTtsReport("PlaybackFinished.MainThreadCleanupStart", agentIndex);
								LogLipSyncNativeProbe("PlaybackFinished.CleanupEnter", agentIndex);
								bool flag2 = false;
								bool flag3 = false;
								bool flag4 = false;
								bool flag5 = false;
								SoundEvent value = null;
								string value2 = null;
								string value3 = null;
								lock (_speakingLock)
								{
									flag2 = _agentSoundEvents.ContainsKey(agentIndex);
									flag3 = _agentWavPaths.ContainsKey(agentIndex);
									flag4 = _agentXmlPaths.ContainsKey(agentIndex);
									flag5 = _agentLipSyncDetachedForSafety.Remove(agentIndex);
									if (flag5)
									{
										if (_agentSoundEvents.TryGetValue(agentIndex, out value))
										{
											_agentSoundEvents.Remove(agentIndex);
										}
										if (_agentWavPaths.TryGetValue(agentIndex, out value2))
										{
											_agentWavPaths.Remove(agentIndex);
										}
										if (_agentXmlPaths.TryGetValue(agentIndex, out value3))
										{
											_agentXmlPaths.Remove(agentIndex);
										}
									}
								}
								if (flag5)
								{
									QueueDeferredCleanup(value, value2, value3, "PlaybackFinished.DetachedSafety", agentIndex);
									Logger.Log("LipSync", $"[SAFEGUARD] queued detached lipsync cleanup after playback finished, agentIndex={agentIndex}, hasSe={(value != null)}, hasWav={(!string.IsNullOrWhiteSpace(value2))}, hasXml={(!string.IsNullOrWhiteSpace(value3))}");
									LogTtsReport("PlaybackFinished.DetachedCleanupQueued", agentIndex, $"hasSe={(value != null)};hasWav={(!string.IsNullOrWhiteSpace(value2))};hasXml={(!string.IsNullOrWhiteSpace(value3))}");
								}
								else
								{
									Logger.Log("LipSync", $"[Rhubarb] keep native lipsync state after playback finished, agentIndex={agentIndex}, hasSe={flag2}, hasWav={flag3}, hasXml={flag4}");
									LogTtsReport("PlaybackFinished.NoImmediateCleanup", agentIndex, $"hasSe={flag2};hasWav={flag3};hasXml={flag4}");
									LogLipSyncNativeProbe("PlaybackFinished.NoImmediateCleanup", agentIndex, $"hasSe={flag2};hasWav={flag3};hasXml={flag4}");
								}
								LogTtsReport("PlaybackFinished.MainThreadCleanupEnd", agentIndex);
							}
							catch (Exception ex5)
							{
								Logger.Log("LipSync", $"[ERROR] PlaybackFinished main-thread cleanup failed, agentIndex={agentIndex}, error={ex5.Message}");
								LogTtsReport("PlaybackFinished.MainThreadCleanupFailed", agentIndex, "error=" + ex5.Message);
								BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackFinished.MainThreadCleanup", ex5, "agentIndex=" + agentIndex);
							}
						});
					}
				};
				_ttsOnPlaybackFailedHandler = delegate(int agentIndex, string errorMessage)
				{
					HandleTtsPlaybackFailed(agentIndex, errorMessage);
				};
				instance.OnAudioFileReady += _ttsOnAudioFileReadyHandler;
				instance.OnPlaybackStarted += _ttsOnPlaybackStartedHandler;
				instance.OnPlaybackFinished += _ttsOnPlaybackFinishedHandler;
				instance.OnPlaybackFailed += _ttsOnPlaybackFailedHandler;
				_ttsEventSubscribedOwner = this;
				Logger.Log("LipSync", $"[INFO] TTS 播放事件订阅完成 owner={GetHashCode()}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", "[ERROR] SubscribeTtsPlaybackEvents failed: " + ex.Message);
		}
	}

	private void UnsubscribeTtsPlaybackEventsInternal(TtsEngine tts, bool clearOwnerIfMatch)
	{
		if (tts == null)
		{
			return;
		}
		try
		{
			if (_ttsOnAudioFileReadyHandler != null)
			{
				tts.OnAudioFileReady -= _ttsOnAudioFileReadyHandler;
			}
			if (_ttsOnPlaybackStartedHandler != null)
			{
				tts.OnPlaybackStarted -= _ttsOnPlaybackStartedHandler;
			}
			if (_ttsOnPlaybackFinishedHandler != null)
			{
				tts.OnPlaybackFinished -= _ttsOnPlaybackFinishedHandler;
			}
			if (_ttsOnPlaybackFailedHandler != null)
			{
				tts.OnPlaybackFailed -= _ttsOnPlaybackFailedHandler;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", "[WARN] 取消订阅 TTS 事件失败: " + ex.Message);
		}
		if (clearOwnerIfMatch && _ttsEventSubscribedOwner == this)
		{
			_ttsEventSubscribedOwner = null;
		}
	}

	private void UnsubscribeTtsPlaybackEvents()
	{
		try
		{
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			lock (_ttsEventSubLock)
			{
				UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: true);
			}
		}
		catch
		{
		}
	}

	private static bool IsMissionSceneReadyForSoundOps()
	{
		try
		{
			return Mission.Current != null && Mission.Current.Scene != null;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	private static bool IsGameWindowFocused()
	{
		try
		{
			IntPtr foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return true;
			}
			GetWindowThreadProcessId(foregroundWindow, out var processId);
			if (processId == 0)
			{
				return true;
			}
			return processId == _currentProcessId;
		}
		catch
		{
			return true;
		}
	}

	private bool IsInEscapeTransitionWindow()
	{
		try
		{
			long num = Interlocked.Read(ref _lastEscapePressedUtcTicks);
			if (num <= 0)
			{
				return false;
			}
			TimeSpan timeSpan = DateTime.UtcNow - new DateTime(num, DateTimeKind.Utc);
			return timeSpan.TotalMilliseconds >= 0.0 && timeSpan.TotalSeconds <= 6.0;
		}
		catch
		{
			return false;
		}
	}

	private bool HasInterruptionDebounceElapsed()
	{
		try
		{
			long num = Interlocked.Read(ref _lastEscapePressedUtcTicks);
			if (num <= 0)
			{
				return true;
			}
			return (DateTime.UtcNow - new DateTime(num, DateTimeKind.Utc)).TotalMilliseconds >= 250.0;
		}
		catch
		{
			return true;
		}
	}

	private void ApplyTtsPauseState()
	{
		bool flag = _ttsPausedByShoutUi || _ttsPausedByInterruption;
		try
		{
			_floatingTextView?.SetTypingPaused(flag);
		}
		catch
		{
		}
		if (flag)
		{
			try
			{
				TtsEngine.Instance?.PausePlayback();
			}
			catch
			{
			}
			lock (_speakingLock)
			{
				foreach (KeyValuePair<int, SoundEvent> agentSoundEvent in _agentSoundEvents)
				{
					try
					{
						agentSoundEvent.Value?.Pause();
					}
					catch
					{
					}
				}
			}
			return;
		}
		try
		{
			TtsEngine.Instance?.ResumePlayback();
		}
		catch
		{
		}
		lock (_speakingLock)
		{
			foreach (KeyValuePair<int, SoundEvent> agentSoundEvent2 in _agentSoundEvents)
			{
				try
				{
					agentSoundEvent2.Value?.Resume();
				}
				catch
				{
				}
			}
		}
	}

	private void PauseTtsForShoutUi()
	{
		if (!_ttsPausedByShoutUi)
		{
			_ttsPausedByShoutUi = true;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[ShoutUI] TTS paused");
		}
	}

	private void ResumeTtsAfterShoutUi()
	{
		if (_ttsPausedByShoutUi)
		{
			_ttsPausedByShoutUi = false;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[ShoutUI] TTS resumed");
		}
	}

	private void TryResumeInterruptionPauseIfPossible()
	{
		if (_ttsPausedByInterruption)
		{
			if (!HasInterruptionDebounceElapsed())
			{
				return;
			}
			if (!IsGameWindowFocused())
			{
				return;
			}
			float num = 1f;
			try
			{
				num = (Mission.Current?.Scene?.TimeSpeed ?? 1f);
			}
			catch
			{
				num = 1f;
			}
			if (num <= 0.001f)
			{
				return;
			}
			_ttsPausedByInterruption = false;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[INTERRUPT] TTS resumed after interruption");
		}
	}

	private void QueueDeferredCleanup(SoundEvent se, string wavPath, string xmlPath, string source = "Unknown", int agentIndex = -1)
	{
		if (se == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath))
		{
			return;
		}
		long itemId;
		try
		{
			itemId = DateTime.UtcNow.Ticks;
		}
		catch
		{
			itemId = Stopwatch.GetTimestamp();
		}
		DeferredCleanupItem deferredCleanupItem = new DeferredCleanupItem
		{
			ItemId = itemId,
			SoundEvent = se,
			WavPath = (string.IsNullOrWhiteSpace(wavPath) ? null : wavPath),
			XmlPath = (string.IsNullOrWhiteSpace(xmlPath) ? null : xmlPath),
			EnqueuedUtcTicks = DateTime.UtcNow.Ticks,
			Source = (string.IsNullOrWhiteSpace(source) ? "Unknown" : source),
			AgentIndex = agentIndex
		};
		int num2 = 0;
		lock (_speakingLock)
		{
			_deferredCleanupQueue.Add(deferredCleanupItem);
			num2 = _deferredCleanupQueue.Count;
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		Logger.Log("LipSync", $"[DeferredCleanup][ENQUEUE] id={deferredCleanupItem.ItemId}, src={deferredCleanupItem.Source}, agent={deferredCleanupItem.AgentIndex}, hasSe={(deferredCleanupItem.SoundEvent != null)}, hasWav={(!string.IsNullOrWhiteSpace(deferredCleanupItem.WavPath))}, hasXml={(!string.IsNullOrWhiteSpace(deferredCleanupItem.XmlPath))}, queue={num2}");
	}

	private bool IsDeferredCleanupWindowStable()
	{
		if (!IsMissionSceneReadyForSoundOps())
		{
			return false;
		}
		if (IsInEscapeTransitionWindow())
		{
			return false;
		}
		if (!IsGameWindowFocused())
		{
			return false;
		}
		if (_ttsPausedByInterruption || _ttsPausedByShoutUi)
		{
			return false;
		}
		float num = 1f;
		try
		{
			num = (Mission.Current?.Scene?.TimeSpeed ?? 1f);
		}
		catch
		{
			num = 1f;
		}
		return num > 0.001f;
	}

	private void ProcessDeferredCleanup()
	{
		long ticks = DateTime.UtcNow.Ticks;
		int num = 0;
		List<DeferredCleanupItem> list = new List<DeferredCleanupItem>();
		lock (_speakingLock)
		{
			num = _deferredCleanupQueue.Count;
			if (num > 0)
			{
				for (int i = _deferredCleanupQueue.Count - 1; i >= 0; i--)
				{
					DeferredCleanupItem deferredCleanupItem = _deferredCleanupQueue[i];
					if (deferredCleanupItem == null)
					{
						_deferredCleanupQueue.RemoveAt(i);
						continue;
					}
					TimeSpan timeSpan = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem.EnqueuedUtcTicks));
					if (timeSpan.TotalSeconds > DeferredCleanupMaxAgeSeconds)
					{
						list.Add(deferredCleanupItem);
						_deferredCleanupQueue.RemoveAt(i);
					}
				}
				num = _deferredCleanupQueue.Count;
			}
		}
		if (list.Count > 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				DeferredCleanupItem deferredCleanupItem2 = list[j];
				TimeSpan timeSpan2 = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem2.EnqueuedUtcTicks));
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem2.WavPath) && File.Exists(deferredCleanupItem2.WavPath))
					{
						File.Delete(deferredCleanupItem2.WavPath);
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem2.XmlPath) && File.Exists(deferredCleanupItem2.XmlPath))
					{
						File.Delete(deferredCleanupItem2.XmlPath);
					}
				}
				catch
				{
				}
				Logger.Log("LipSync", $"[DeferredCleanup][TIMEOUT_DROP] id={deferredCleanupItem2.ItemId}, src={deferredCleanupItem2.Source}, agent={deferredCleanupItem2.AgentIndex}, ageMs={(int)timeSpan2.TotalMilliseconds}");
			}
		}
		if (num <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
			return;
		}
		if (!IsDeferredCleanupWindowStable())
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
			return;
		}
		long num2 = 0L;
		try
		{
			num2 = Interlocked.Read(ref _deferredCleanupStableSinceUtcTicks);
		}
		catch
		{
			num2 = 0L;
		}
		if (num2 <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, ticks);
			}
			catch
			{
			}
			return;
		}
		if ((new TimeSpan(ticks - num2)).TotalSeconds < DeferredCleanupStableWindowSeconds)
		{
			return;
		}
		List<DeferredCleanupItem> list2 = new List<DeferredCleanupItem>();
		lock (_speakingLock)
		{
			if (_deferredCleanupQueue.Count <= 0)
			{
				return;
			}
			for (int k = 0; k < _deferredCleanupQueue.Count; k++)
			{
				DeferredCleanupItem deferredCleanupItem3 = _deferredCleanupQueue[k];
				if (deferredCleanupItem3 == null)
				{
					continue;
				}
				if ((new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem3.EnqueuedUtcTicks))).TotalSeconds < DeferredCleanupMinAgeSeconds)
				{
					continue;
				}
				list2.Add(deferredCleanupItem3);
				if (list2.Count >= DeferredCleanupBatchSize)
				{
					break;
				}
			}
			if (list2.Count > 0)
			{
				for (int l = 0; l < list2.Count; l++)
				{
					_deferredCleanupQueue.Remove(list2[l]);
				}
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		for (int m = 0; m < list2.Count; m++)
		{
			DeferredCleanupItem deferredCleanupItem4 = list2[m];
			TimeSpan timeSpan3 = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem4.EnqueuedUtcTicks));
			try
			{
				SafeStopAndReleaseSoundEvent(deferredCleanupItem4.SoundEvent);
			}
			catch (Exception ex)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.DeferredCleanup.SoundEvent", ex, $"agentIndex={deferredCleanupItem4.AgentIndex};itemId={deferredCleanupItem4.ItemId};source={deferredCleanupItem4.Source}");
			}
			try
			{
				if (!string.IsNullOrEmpty(deferredCleanupItem4.WavPath) && File.Exists(deferredCleanupItem4.WavPath))
				{
					File.Delete(deferredCleanupItem4.WavPath);
				}
			}
			catch
			{
			}
			try
			{
				if (!string.IsNullOrEmpty(deferredCleanupItem4.XmlPath) && File.Exists(deferredCleanupItem4.XmlPath))
				{
					File.Delete(deferredCleanupItem4.XmlPath);
				}
			}
			catch
			{
			}
			Logger.Log("LipSync", $"[DeferredCleanup][PROCESS] id={deferredCleanupItem4.ItemId}, src={deferredCleanupItem4.Source}, agent={deferredCleanupItem4.AgentIndex}, ageMs={(int)timeSpan3.TotalMilliseconds}");
		}
		int num3 = 0;
		lock (_speakingLock)
		{
			num3 = _deferredCleanupQueue.Count;
		}
		Logger.Log("LipSync", $"[DeferredCleanup][SUMMARY] processed={list2.Count}, timeoutDropped={list.Count}, remaining={num3}");
	}

	private void HandleEscapePressedForAudioSafety(string reason = "ESC")
	{
		try
		{
			Interlocked.Exchange(ref _lastEscapePressedUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		if (!_ttsPausedByInterruption)
		{
			_ttsPausedByInterruption = true;
			ApplyTtsPauseState();
			Logger.Log("LipSync", $"[{reason}] TTS paused by interruption");
		}
	}

	private bool HasLipSyncOrSceneSpeechWork()
	{
		if (IsSpeechPipelineBusy())
		{
			return true;
		}
		lock (_speakingLock)
		{
			if (_speakingAgentIndices.Count > 0 || _agentSoundEvents.Count > 0 || _agentWavPaths.Count > 0 || _agentXmlPaths.Count > 0 || _deferredCleanupQueue.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	private void HandleCriticalUiTransitionForLipSyncSafety(string reason = "UI_TRANSITION")
	{
		try
		{
			Interlocked.Exchange(ref _lastEscapePressedUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		if (!HasLipSyncOrSceneSpeechWork())
		{
			return;
		}
		ClearQueuedSceneSpeech();
		Logger.Log("LipSync", $"[{reason}] critical UI transition detected, forcing native lipsync teardown");
		LogLipSyncNativeProbe("CriticalUiTransition.StopAll.Before", -1, reason);
		StopAllLipSyncPlaybackAndCleanup();
		LogLipSyncNativeProbe("CriticalUiTransition.StopAll.After", -1, reason);
	}

	private static void SafeStopAndReleaseSoundEvent(SoundEvent se)
	{
		if (se == null)
		{
			Logger.Log("LipSync", "[SoundEvent] SafeStopAndRelease skipped: null");
			return;
		}
		int soundId = -1;
		try
		{
			soundId = se.GetSoundId();
		}
		catch
		{
			soundId = -1;
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease start, soundId={soundId}");
		LogLipSyncNativeProbe("SafeStopAndRelease.Enter", -1, "soundId=" + soundId);
		bool isValid = false;
		try
		{
			isValid = se.IsValid;
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[SoundEvent] IsValid probe failed, soundId={soundId}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.IsValid", ex, "soundId=" + soundId);
			return;
		}
		if (!isValid)
		{
			Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease skipped: invalid soundId={soundId}");
			LogLipSyncNativeProbe("SafeStopAndRelease.SkipInvalid", -1, "soundId=" + soundId);
			return;
		}
		try
		{
			LogLipSyncNativeProbe("SoundEvent.Stop.Before", -1, "soundId=" + soundId);
			se.Stop();
			LogLipSyncNativeProbe("SoundEvent.Stop.After", -1, "soundId=" + soundId);
		}
		catch (Exception ex2)
		{
			Logger.Log("LipSync", $"[SoundEvent] Stop failed, soundId={soundId}, error={ex2.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.Stop", ex2, "soundId=" + soundId);
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease end, soundId={soundId}");
	}

	private void StopAllLipSyncPlaybackAndCleanup()
	{
		try
		{
			TtsEngine.Instance?.StopPlayback();
		}
		catch
		{
		}
		ClearPendingTtsBubbleSyncQueues();
		try
		{
			_floatingTextView?.SetTypingPaused(false);
			_floatingTextView?.StopTypingForAll(fadeSoon: true);
		}
		catch
		{
		}
		_ttsPausedByInterruption = false;
		_ttsPausedByShoutUi = false;
		List<int> list = null;
		lock (_speakingLock)
		{
			list = _agentSoundEvents.Keys.ToList();
			_speakingAgentIndices.Clear();
			_agentLipSyncDetachedForSafety.Clear();
			foreach (KeyValuePair<int, SoundEvent> agentSoundEvent in _agentSoundEvents)
			{
				_ = agentSoundEvent.Value;
			}
			_agentSoundEvents.Clear();
			foreach (KeyValuePair<int, string> agentWavPath in _agentWavPaths)
			{
				try
				{
					if (!string.IsNullOrEmpty(agentWavPath.Value) && File.Exists(agentWavPath.Value))
					{
						File.Delete(agentWavPath.Value);
					}
				}
				catch
				{
				}
			}
			foreach (KeyValuePair<int, string> agentXmlPath in _agentXmlPaths)
			{
				try
				{
					if (!string.IsNullOrEmpty(agentXmlPath.Value) && File.Exists(agentXmlPath.Value))
					{
						File.Delete(agentXmlPath.Value);
					}
				}
				catch
				{
				}
			}
			_agentWavPaths.Clear();
			_agentXmlPaths.Clear();
			foreach (DeferredCleanupItem deferredCleanupItem in _deferredCleanupQueue)
			{
				try
				{
					SafeStopAndReleaseSoundEvent(deferredCleanupItem?.SoundEvent);
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem?.WavPath) && File.Exists(deferredCleanupItem.WavPath))
					{
						File.Delete(deferredCleanupItem.WavPath);
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem?.XmlPath) && File.Exists(deferredCleanupItem.XmlPath))
					{
						File.Delete(deferredCleanupItem.XmlPath);
					}
				}
				catch
				{
				}
			}
			_deferredCleanupQueue.Clear();
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				StopAgentRhubarbRecordIfPossible(list[i], "StopAllLipSyncPlaybackAndCleanup");
			}
		}
	}

	private void TickLipSyncAnimations(float dt)
	{
		List<int> list;
		lock (_speakingLock)
		{
			if (_speakingAgentIndices.Count == 0)
			{
				return;
			}
			list = new List<int>(_speakingAgentIndices);
		}
		if (Mission.Current == null)
		{
			return;
		}
		AgentReadOnlyList agents = Mission.Current.Agents;
		if (agents == null)
		{
			return;
		}
		foreach (int agentIndex in list)
		{
			try
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
				if (agent == null || !agent.IsActive())
				{
					LogTtsReport("AgentInvalidDuringLipSync", agentIndex);
					CancelAgentSpeechForRemoval(agentIndex, "agent_invalid_during_lipsync");
					continue;
				}
				string reason = "";
				if (CanAgentUseSceneLipSync(agent, out reason))
				{
					continue;
				}
				DetachAgentLipSyncForSafety(agentIndex, reason);
			}
			catch
			{
			}
		}
	}

	internal HashSet<int> GetSpeakingAgentIndicesSnapshot()
	{
		lock (_speakingLock)
		{
			return new HashSet<int>(_speakingAgentIndices);
		}
	}

	public void UpdatePassiveStareLogic(float dt)
	{
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || _isProcessingShout)
		{
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
				if (_stareTimer >= STARE_TRIGGER_TIME && IsCooldownReady(closestFacingAgent))
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

	private static string TryGetKingdomIdOverrideFromAgent(Agent agent)
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
		if (targetAgent == null || !targetAgent.IsActive() || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return list;
		}
		Vec3 playerPos = Agent.Main.Position;
		Vec3 playerLook = Agent.Main.LookDirection;
		foreach (Agent agent in Mission.Current.Agents)
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
		if (targetAgent == null || _isProcessingShout)
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

	private static string BuildFallbackSceneTauntSpeech(bool escalatedToFight)
	{
		return escalatedToFight ? "少废话，既然你想找打，那就来吧。" : "嘴巴放干净点，别逼我动手。";
	}

	private static string BuildCombatActiveShoutExtraFact(Agent targetAgent)
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

	private static bool CanAgentUseSceneLipSyncExternal(int agentIndex, out string reason)
	{
		reason = "agent_index_invalid";
		if (agentIndex < 0 || Mission.Current == null)
		{
			return false;
		}
		try
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
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
		return "【续聊耐心规则】：请结合上面的耐心/状态信息决定是否继续聊。若你已经明显无聊、冷淡、恼火，或觉得话题正在变干，请优先缩短回复，并且不要输出任何续聊标签；只有当你真心觉得下一位还有必要接话时，才在句末追加 [RELAY:id]。续聊阶段不要输出任何 [ACTION:*] 标签。";
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

	private async Task<string> GenerateGroupConversationTurnLineAsync(NpcDataPacket speakerNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, string playerText, string extraFact, string commonCandidatesPrompt, string sceneMechanismPromptSectionBase, List<string> patienceStatusLines, bool multiNpcScene, int minTokens, int maxTokens)
	{
		try
		{
			if (speakerNpc == null || allNpcData == null || allNpcData.Count < 2)
			{
				return "";
			}
			ApplySceneLocalDisambiguatedNames(allNpcData);
			await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { speakerNpc }, resolvedHeroes ?? new Dictionary<int, Hero>());
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerNpc.AgentIndex);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return "";
			}
			CharacterObject characterObject = agent.Character as CharacterObject;
			Hero hero = null;
			if (speakerNpc.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(speakerNpc.AgentIndex, out hero);
			}
			string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
			PrecomputedShoutRagContext precomputed = null;
			bool hasPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(speakerNpc.AgentIndex, out precomputed);
			string loreContext = (hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "";
			string fullExtra = string.IsNullOrWhiteSpace(loreContext) ? null : loreContext;
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				fullExtra = ((fullExtra == null) ? extraFact : (fullExtra + "\n" + extraFact));
			}
			MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, playerText, fullExtra, speakerNpc.CultureId ?? "neutral", hasAnyHero: speakerNpc.IsHero, targetCharacter: characterObject, kingdomIdOverride: kingdomIdOverride, targetAgentIndex: speakerNpc.AgentIndex, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext);
			StringBuilder local = new StringBuilder();
			local.Append(commonCandidatesPrompt ?? "");
			string scenePatienceInstruction = "";
			if (patienceStatusLines != null && patienceStatusLines.Count > 0)
			{
				local.AppendLine("【4.三值状态】");
				local.AppendLine("【NPC耐心状态】：");
				foreach (string item in patienceStatusLines)
				{
					local.AppendLine(item);
				}
				scenePatienceInstruction = MyBehavior.GetScenePatienceInstructionForExternal();
			}
			string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
			string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
			string localExtras = InjectTrustBlockBelowTriState(local.ToString().Trim(), trustBlock);
			string fixedLayerText = "";
			string deltaLayerText = string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust);
			string layeredPrompt = string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText);
			string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(speakerNpc);
			string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(null, null, null, sceneFollowControlInstruction);
			if (!string.IsNullOrWhiteSpace(sceneMechanismPromptSectionBase))
			{
				sceneMechanismPromptSection = string.IsNullOrWhiteSpace(sceneMechanismPromptSection) ? sceneMechanismPromptSectionBase.Trim() : (sceneMechanismPromptSectionBase.Trim() + "\n" + sceneMechanismPromptSection);
			}
			bool allowAppendSceneMechanismWithoutMarker = !string.IsNullOrWhiteSpace(sceneFollowControlInstruction);
			layeredPrompt = InjectSceneMechanismPromptSection(layeredPrompt, sceneMechanismPromptSection, allowAppendSceneMechanismWithoutMarker);
			List<string> historyLines = null;
			lock (_historyLock)
			{
				if (_publicConversationHistory.Count > 0)
				{
					historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, speakerNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(speakerNpc), multiNpcScene);
				}
			}
			string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
			string persistedHeroHistory = speakerNpc.IsHero ? GetOrBuildPrecomputedPersistedHistoryContext(speakerNpc.AgentIndex, playerText, resolvedHeroes, precomputedContexts) : "";
			string privateRecentWindowSection = "";
			string persistedWithoutRecentWindow = "";
			SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
			if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
			{
				layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
			}
			if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
			{
				layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
			}
			string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(hero);
			if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
			{
				layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
			}
			if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
			{
				layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
			}
			bool includeInventorySummary = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext || ctx.UseDuelContext);
			bool includeTradePricing = includeInventorySummary;
			string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(speakerNpc, hero, allNpcData, includeInventorySummary, includeTradePricing);
			if (!string.IsNullOrWhiteSpace(roleTopIntro))
			{
				layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
			}
			string singleReplyPlayerName = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
			{
				singleReplyPlayerName = "玩家";
			}
			string userContent = BuildSingleNpcSceneReplyInstruction(GetSceneNpcHistoryNameForPrompt(speakerNpc), multiNpcScene) + "\n" + $"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{singleReplyPlayerName}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)" + (string.IsNullOrWhiteSpace(scenePatienceInstruction) ? "" : ("\n" + scenePatienceInstruction));
			if (multiNpcScene)
			{
				userContent += "\n(这是一场多人聊天，你可以在回复末尾输出 [RELAY:接力编号]，指定下一个发言的人，尽量让没在【当前场景公共对话与互动】说过话的人发言)";
			}
			List<object> messages = new List<object>
			{
				new
				{
					role = "system",
					content = layeredPrompt
				},
				new
				{
					role = "user",
					content = userContent
				}
			};
			string text = await ShoutNetwork.CallApiWithMessages(messages, 5000);
			if (string.IsNullOrWhiteSpace(text) || text.StartsWith("（错误") || text.StartsWith("（程序错误") || text.StartsWith("（API请求失败"))
			{
				return "";
			}
			string text2 = StripNpcNamePrefixSafely((text ?? "").Replace("\r", "").Trim(), 30);
			text2 = StripLeakedPromptContentForShout(text2);
			text2 = StripStageDirectionsForPassiveShout(text2);
			text2 = StripActionTagsForSceneSpeech(text2);
			return text2.Trim();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[GroupConversationTurn] prompt failed: " + ex.Message);
			return "";
		}
	}

	private async Task<string> GetPassiveNpcResponse(NpcDataPacket data, string sceneDesc, string inputActionText, string precalculatedLore, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			if (data == null)
			{
				return "（没说话）";
			}
			ApplySceneLocalDisambiguatedNames(allNpcData);
			Hero hero = null;
			if (data.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(data.AgentIndex, out hero);
			}
			using (Logger.BeginTrace("shout_passive", hero?.StringId, data.Name))
			{
				Logger.Obs("ShoutPassive", "start", new Dictionary<string, object>
				{
					["npc"] = data.Name ?? "",
					["agentIndex"] = data.AgentIndex,
					["inputLen"] = (inputActionText ?? "").Length,
					["isHero"] = data.IsHero
				});
				string cultureId = data.CultureId ?? "neutral";
				string loreContext = precalculatedLore ?? "";
				string fullExtra = (string.IsNullOrWhiteSpace(loreContext) ? null : loreContext);
				Agent passiveAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == data.AgentIndex);
				CharacterObject passiveCharacter = passiveAgent?.Character as CharacterObject;
				string passiveKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(passiveAgent);
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, inputActionText, fullExtra, cultureId, hasAnyHero: data.IsHero, targetCharacter: passiveCharacter, kingdomIdOverride: passiveKingdomIdOverride, targetAgentIndex: data.AgentIndex, usePrefetchedLoreContext: !string.IsNullOrWhiteSpace(loreContext), prefetchedLoreContext: loreContext);
				DuelSettings settings = DuelSettings.GetSettings();
				int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
				int minTokens = maxTokens / 2;
				if (minTokens < 5)
				{
					minTokens = 5;
				}
				StringBuilder sysPrompt = new StringBuilder();
				string npcName = GetSceneNpcHistoryNameForPrompt(data);
				sysPrompt.AppendLine("【在场人物列表】：");
				foreach (NpcDataPacket npc2 in allNpcData)
				{
					if (npc2 == null)
					{
						continue;
					}
					string line = BuildSceneNpcListLineForPrompt(npc2);
					try
					{
						if (!npc2.IsHero)
						{
							string key = (npc2.UnnamedKey ?? "").Trim().ToLower();
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
								kingdomId = (npc2.CultureId ?? "").Trim().ToLower();
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
								line = line + " | 效忠于: " + rulerName;
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
					if (npc2.IsHero)
					{
						try
						{
							Hero hero2 = null;
							if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc2.AgentIndex, out hero2);
							if (hero2?.IsPrisoner ?? false)
							{
								string captor = hero2.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
								line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
							}
							line += BuildPlayerMarriageFactForNpcListLine(hero2);
						}
						catch
						{
						}
					}
					sysPrompt.AppendLine(line);
				}
				string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(allNpcData);
				if (!string.IsNullOrWhiteSpace(sceneNamingNote))
				{
					sysPrompt.AppendLine(sceneNamingNote);
				}
				string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(allNpcData);
				if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
				{
					sysPrompt.AppendLine(sceneSummonClosureInstruction);
				}
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(data);
				if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
				{
					sysPrompt.AppendLine(sceneFollowControlInstruction);
				}
				bool passiveMultiNpcScene = allNpcData != null && allNpcData.Count((NpcDataPacket npc) => npc != null) > 1;
				if (!string.IsNullOrWhiteSpace(inputActionText))
				{
					sysPrompt.AppendLine("[玩家动作] " + inputActionText);
				}
				string scenePatienceInstruction = "";
				try
				{
					string patienceLine;
					bool canSpeak;
					bool hasPatience = ((hero == null) ? MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(data.UnnamedKey, data.Name, GetSceneNpcPatienceNameForPrompt(data), out patienceLine, out canSpeak) : MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out patienceLine, out canSpeak));
					if (hasPatience && !string.IsNullOrWhiteSpace(patienceLine))
					{
						sysPrompt.AppendLine("【4.三值状态】");
						sysPrompt.AppendLine(patienceLine);
						scenePatienceInstruction = MyBehavior.GetScenePatienceInstructionForExternal();
					}
				}
				catch
				{
				}
				if (passiveMultiNpcScene)
				{
					string playerNameForPrompt = GetPlayerDisplayNameForShout();
					if (string.IsNullOrWhiteSpace(playerNameForPrompt))
					{
						playerNameForPrompt = "玩家";
					}

				}
				string playerNameForLength = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForLength))
				{
					playerNameForLength = "玩家";
				}
				sysPrompt.AppendLine($"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{playerNameForLength}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)");
				if (!string.IsNullOrWhiteSpace(scenePatienceInstruction))
				{
					sysPrompt.AppendLine(scenePatienceInstruction);
				}
				Stopwatch swPrompt = Stopwatch.StartNew();
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
				string localExtras = InjectTrustBlockBelowTriState(sysPrompt.ToString().Trim(), trustBlock);
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				bool includeTradePricing = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext || ctx.UseDuelContext);
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(data, hero, allNpcData, includeTradePricing: includeTradePricing);
				string playerNameForTask = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForTask))
				{
					playerNameForTask = "玩家";
				}
				string taskPreamble = "你是【在场人物列表】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复" + playerNameForTask + "的对话。";
				layeredPrompt = (string.IsNullOrWhiteSpace(roleTopIntro) ? taskPreamble : (roleTopIntro + "\n" + taskPreamble)) + "\n" + layeredPrompt;
				swPrompt.Stop();
				int fixedChars = CountPromptChars(fixedLayerText);
				int deltaChars = CountPromptChars(deltaLayerText);
				int totalChars = CountPromptChars(layeredPrompt);
				Logger.Log("ShoutBehavior(被动)", $"[PromptLayer] fixedChars={fixedChars} deltaChars={deltaChars} totalChars={totalChars}");
				Logger.Obs("Prompt", "compose", new Dictionary<string, object>
				{
					["mode"] = "shout_passive",
					["fixedChars"] = fixedChars,
					["deltaChars"] = deltaChars,
					["totalChars"] = totalChars,
					["buildMs"] = Math.Round(swPrompt.Elapsed.TotalMilliseconds, 2)
				});
				Logger.Metric("prompt.compose.shout_passive", ok: true, swPrompt.Elapsed.TotalMilliseconds);
				List<object> messages = new List<object>
				{
					new
					{
						role = "system",
						content = layeredPrompt
					}
				};
				StringBuilder historyDump = null;
				List<string> historyLines = null;
				lock (_historyLock)
				{
					if (_publicConversationHistory.Count > 0)
					{
						historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, data.AgentIndex, npcName, passiveMultiNpcScene);
						if (historyLines != null && historyLines.Count > 0)
						{
							historyDump = new StringBuilder();
							historyDump.AppendLine($">>> History(Public viewerAgentIndex={data.AgentIndex}) count={historyLines.Count}");
							for (int i = 0; i < historyLines.Count; i++)
							{
								historyDump.AppendLine(historyLines[i]);
							}
						}
					}
				}
				if (historyDump != null)
				{
					Logger.Log("ShoutBehavior(被动)", historyDump.ToString());
				}
				string persistedHeroHistory = BuildPersistedHeroHistoryContext(data.AgentIndex, inputActionText, resolvedHeroes);
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(被动)", $"[HistoryBridge] heroAgentIndex={data.AgentIndex} chars={persistedHeroHistory.Length}");
				}
				string privateRecentWindowSection = "";
				string persistedWithoutRecentWindow = "";
				SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
				string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
				if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
				{
					layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
				}
				if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
				{
					layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
				}
				string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(hero);
				if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
				{
					layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
				}
				if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
				{
					layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
				}
				messages[0] = new
				{
					role = "system",
					content = layeredPrompt
				};
				string passiveUserMessage = BuildSingleNpcSceneReplyInstruction(npcName, passiveMultiNpcScene) + "\n禁止生成任何【】章节标题或格式说明。";
				messages.Add(new
				{
					role = "user",
					content = passiveUserMessage
				});
				Stopwatch swApi = Stopwatch.StartNew();
				string output = await ShoutNetwork.CallApiWithMessages(messages, 5000);
				swApi.Stop();
				bool ok = !string.IsNullOrWhiteSpace(output) && !output.StartsWith("（错误") && !output.StartsWith("（程序错误") && !output.StartsWith("（API请求失败");
				Logger.Obs("API", "complete", new Dictionary<string, object>
				{
					["mode"] = "shout_passive",
					["ok"] = ok,
					["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
					["resultLen"] = (output ?? "").Length
				});
				Logger.Metric("api.shout_passive", ok, swApi.Elapsed.TotalMilliseconds);
				
				if (!ok)
				{
					_mainThreadActions.Enqueue(delegate
					{
						InformationManager.DisplayMessage(new InformationMessage("[被动回应失败] " + output, new Color(1f, 0.3f, 0.3f)));
					});
					return "（没说话）";
				}
				
				return output;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] GetPassiveNpcResponse 异常: " + ex.Message);
			Logger.Metric("api.shout_passive", ok: false);
			return "（没说话）";
		}
	}

	public void TriggerShout()
	{
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			return;
		}
		PauseGame();
		List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
		if (nearbyNPCAgents != null && nearbyNPCAgents.Count > 0)
		{
			ActivateMultiSceneMovementSuppression(nearbyNPCAgents.Select((Agent agent) => agent?.Index ?? (-1)));
		}
		if (nearbyNPCAgents == null || nearbyNPCAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f)));
			ResumeGame();
			return;
		}
		List<NpcDataPacket> source = (from a in nearbyNPCAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		Agent primaryTarget = ShoutUtils.GetFacingAgent(nearbyNPCAgents) ?? nearbyNPCAgents[0];
		NpcDataPacket primaryDataPacket = source.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index) ?? source.FirstOrDefault();
		string text = primaryDataPacket?.Name ?? "附近的人";
		List<InquiryElement> inquiryElements = new List<InquiryElement>
		{
			new InquiryElement("normal", "交流", null, isEnabled: true, ""),
			new InquiryElement("give", "给予其物品并交流", null, isEnabled: true, ""),
			new InquiryElement("show", "向其展示物品并交流", null, isEnabled: true, ""),
			new InquiryElement("give_troops", "给予部队并交流", null, isEnabled: true, ""),
			new InquiryElement("give_prisoners", "给予俘虏并交流", null, isEnabled: true, "")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("与 " + text + " 交流", "当前目标：" + text + "\n请选择交流方式：", inquiryElements, isExitShown: true, 1, 1, "确定", "取消", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				ResumeGame();
			}
			else
			{
				string text2 = (selected[0]?.Identifier ?? "").ToString();
				if (text2 == "give")
				{
					BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Give);
				}
				else if (text2 == "show")
				{
					BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Show);
				}
				else if (text2 == "give_troops")
				{
					BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GiveTroops);
				}
				else if (text2 == "give_prisoners")
				{
					BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GivePrisoners);
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

	private void OpenShoutTextInput(NpcDataPacket primaryDataPacket, string preface, string extraFact)
	{
		string text = primaryDataPacket?.Name ?? "附近的人";
		string titleText = "与 " + text + " 交流";
		string text2 = (string.IsNullOrWhiteSpace(preface) ? "输入你想说的话：" : (preface + "\n请输入你想说的话："));
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "发送", "取消", delegate(string input)
		{
			OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
		}, OnShoutCancelled), pauseGameActiveState: true);
	}

	private static bool IsShoutTradeShowMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.Show;
	}

	private static bool IsShoutPartyTransferMode(ShoutChatMode mode)
	{
		return mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners;
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
		return mode == ShoutChatMode.Give || mode == ShoutChatMode.GiveTroops || mode == ShoutChatMode.GivePrisoners;
	}

	private void BeginShoutTradeFlow(NpcDataPacket targetNpc, ShoutChatMode mode)
	{
		_shoutTradeTargetNpc = targetNpc;
		_shoutTradeMode = mode;
		_shoutTradeOptions = BuildShoutTradeOptions();
		_shoutPendingTradeItems.Clear();
		_shoutPendingTradeItemIndex = 0;
		if (_shoutTradeOptions == null || _shoutTradeOptions.Count == 0)
		{
			string information = (mode == ShoutChatMode.GiveTroops) ? "你当前没有可转移给对方的部队。" : ((mode == ShoutChatMode.GivePrisoners) ? "你当前没有可转移给对方的俘虏。" : "你没有可用的物品或第纳尔。");
			InformationManager.DisplayMessage(new InformationMessage(information));
			ResumeGame();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < _shoutTradeOptions.Count; i++)
		{
			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];
			string text2 = $"{shoutTradeResourceOption.Name} (×{shoutTradeResourceOption.AvailableAmount})";
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
				}
			}
			list.Add(new InquiryElement(i, text2, null, isEnabled: true, text3));
		}
		string text = targetNpc?.Name ?? "附近的人";
		string titleText = ((mode == ShoutChatMode.Give) ? ("给予其物品并交流 - " + text) : ((mode == ShoutChatMode.Show) ? ("向其展示物品并交流 - " + text) : ((mode == ShoutChatMode.GiveTroops) ? ("给予部队并交流 - " + text) : ("给予俘虏并交流 - " + text))));
		string descriptionText = (IsShoutPartyTransferMode(mode) ? ((mode == ShoutChatMode.GiveTroops) ? ("当前目标：" + text + "\n选择要转入对方麾下的部队（可多选）：") : ("当前目标：" + text + "\n选择要交给对方的俘虏（可多选）：")) : ("当前目标：" + text + "\n选择要使用的物品或第纳尔（可多选）："));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 1, list.Count, "确定", "取消", OnShoutTradeResourcesSelected, delegate
		{
			ResetShoutTradeState();
			ResumeGame();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private List<ShoutTradeResourceOption> BuildShoutTradeOptions()
	{
		List<ShoutTradeResourceOption> list = new List<ShoutTradeResourceOption>();
		Hero hero = null;
		string text = "";
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1)));
		CharacterObject characterObject = agent?.Character as CharacterObject;
		if (_shoutTradeTargetNpc != null && _shoutTradeTargetNpc.IsHero)
		{
			hero = ResolveHeroFromAgentIndex(_shoutTradeTargetNpc.AgentIndex) ?? characterObject?.HeroObject;
		}
		if (IsShoutPartyTransferMode(_shoutTradeMode))
		{
			List<MyBehavior.PartyTransferPromptEntry> list2 = MyBehavior.BuildPartyTransferPromptEntriesForExternal(hero, characterObject, _shoutTradeTargetNpc?.AgentIndex ?? (-1));
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
					Amount = 0
				});
			}
		}
		if (_shoutPendingTradeItems.Count == 0)
		{
			ResetShoutTradeState();
			ResumeGame();
		}
		else
		{
			_shoutPendingTradeItemIndex = 0;
			ShowShoutTradeAmountInquiry();
		}
	}

	private void ShowShoutTradeAmountInquiry()
	{
		if (_shoutPendingTradeItemIndex >= _shoutPendingTradeItems.Count)
		{
			ShowShoutTradeChatInput();
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
		string text = $"[{_shoutPendingTradeItemIndex + 1}/{_shoutPendingTradeItems.Count}] {shoutPendingTradeItem.ItemName} 最多可填 {availableAmount}。\n请输入 1 到 {availableAmount} 的整数：";
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
		}));
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
				else if (shoutPendingTradeItem.IsGold)
				{
					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? $"  · 给予 {shoutPendingTradeItem.Amount} 第纳尔" : $"  · 展示 {shoutPendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					stringBuilder.AppendLine(IsShoutTradeGiveMode(_shoutTradeMode) ? $"  · 给予 {shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}" : $"  · 展示 {shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}");
				}
			}
		}
		string text2 = (IsShoutTroopTransferMode(_shoutTradeMode) ? ("你准备将以下部队转入对方麾下：\n" + stringBuilder.ToString()) : (IsShoutPrisonerTransferMode(_shoutTradeMode) ? ("你准备将以下俘虏交给对方：\n" + stringBuilder.ToString()) : ((IsShoutTradeGiveMode(_shoutTradeMode) ? "你准备给予对方以下物品：\n" : "你准备向对方展示以下物品：\n") + stringBuilder.ToString())));
		string titleText = "与 " + text + " 交流";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2 + "\n请输入你想说的话：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "发送", "取消", OnShoutTradeChatConfirmed, delegate
		{
			ResetShoutTradeState();
			OnShoutCancelled();
		}), pauseGameActiveState: true);
	}

	private void OnShoutTradeChatConfirmed(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
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
		int? forcedPrimaryAgentIndex = ((_shoutTradeTargetNpc != null) ? new int?(_shoutTradeTargetNpc.AgentIndex) : ((int?)null));
		ResetShoutTradeState();
		OnShoutConfirmedWithContext(input, text, forcedPrimaryAgentIndex);
	}

	private void ApplyShoutGiveTransfer()
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return;
		}
		NpcDataPacket shoutTradeTargetNpc = _shoutTradeTargetNpc;
		Hero hero = null;
		Agent agent = null;
		CharacterObject characterObject = null;
		try
		{
			if (shoutTradeTargetNpc != null)
			{
				agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == shoutTradeTargetNpc.AgentIndex);
				characterObject = agent?.Character as CharacterObject;
			}
			if (shoutTradeTargetNpc != null && shoutTradeTargetNpc.IsHero)
			{
				hero = ResolveHeroFromAgentIndex(shoutTradeTargetNpc.AgentIndex);
			}
		}
		catch
		{
			hero = null;
		}
		RewardSystemBehavior.SettlementMerchantKind settlementMerchantKind = RewardSystemBehavior.SettlementMerchantKind.None;
		bool flag = hero == null && characterObject != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out settlementMerchantKind);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		string text = MyBehavior.BuildRuleTargetKeyForExternal(hero, characterObject, shoutTradeTargetNpc?.AgentIndex ?? (-1));
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.PartyEntry != null)
			{
				int num = MyBehavior.TransferPlayerPartyEntryToCounterpartyForExternal(hero, characterObject, shoutTradeTargetNpc?.AgentIndex ?? (-1), shoutPendingTradeItem.PartyEntry, shoutPendingTradeItem.Amount);
				shoutPendingTradeItem.Amount = num;
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
					if (hero != null)
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
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
						currentSettlement.SettlementComponent?.ChangeGold(num2);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, num2, null, 0);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num2} 第纳尔。", characterObject?.Name?.ToString());
					}
					else
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num2, disableNotification: true);
						RecordScenePrepaidTransfer(text, num2);
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
				ItemObject itemObject;
				int num3 = MyBehavior.RemoveItemsFromRosterByStringId(itemRoster, text2, shoutPendingTradeItem.Amount, out itemObject);
				shoutPendingTradeItem.Amount = num3;
				if (num3 > 0)
				{
					if (hero?.PartyBelongedTo != null && itemObject != null)
					{
						hero.PartyBelongedTo.ItemRoster.AddToCounts(itemObject, num3);
					}
					else if (flag && currentSettlement?.ItemRoster != null && itemObject != null)
					{
						currentSettlement.ItemRoster.AddToCounts(itemObject, num3);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, 0, text2, num3);
						string text3 = RewardSystemBehavior.Instance?.BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, num3) ?? "";
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num3} 个 {itemObject.Name?.ToString() ?? text2}{text3}。", characterObject?.Name?.ToString());
					}
					if (hero != null)
					{
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, 0, text2, num3);
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
			return "你今天稍早时候刚与" + text + "见过面。";
		}
		return "距离你上次与" + text + "见面，已有" + elapsedDays + "天了。";
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
			string extraFact = "[AFEF NPC行为补充] 对" + text3 + "：" + text4;
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
			if (npc == null || settlement == null || !settlement.IsTown)
			{
				return false;
			}
			Agent agent2 = agent ?? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
			CharacterObject characterObject = agent2?.Character as CharacterObject;
			if (npc.IsHero || characterObject == null || !characterObject.IsSoldier || !IsLordsHallGuardAgent(agent2))
			{
				return false;
			}
			var settlementAccessModel = Campaign.Current?.Models?.SettlementAccessModel;
			if (settlementAccessModel == null)
			{
				return false;
			}
			bool disableOption = false;
			TaleWorlds.Localization.TextObject disabledText = null;
			if (settlementAccessModel.CanMainHeroAccessLocation(settlement, "lordshall", out disableOption, out disabledText))
			{
				return true;
			}
			SettlementAccessModel.AccessDetails accessDetails = default(SettlementAccessModel.AccessDetails);
			settlementAccessModel.CanMainHeroEnterLordsHall(settlement, out accessDetails);
			int bribeToEnterLordsHall = Campaign.Current?.Models?.BribeCalculationModel?.GetBribeToEnterLordsHall(settlement) ?? 0;
			string text = MyBehavior.BuildRuleTargetKeyForExternal(null, characterObject, npc.AgentIndex);
			int recentNonHeroGoldForRuleTarget = GetRecentNonHeroGoldForRuleTarget(text);
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe && bribeToEnterLordsHall > 0 && recentNonHeroGoldForRuleTarget > 0)
			{
				settlement.BribePaid += bribeToEnterLordsHall;
				ConsumeRecentNonHeroGoldForRuleTarget(text, recentNonHeroGoldForRuleTarget);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool TryTriggerOpenLordsHallAction(NpcDataPacket npc, Agent agent, ref string content)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(content) || content.IndexOf("[ACTION:OPEN_LORDS_HALL]", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
			content = content.Replace("[ACTION:OPEN_LORDS_HALL]", "").Trim();
			return TryUnlockLordsHallForNpc(npc, agent);
		}
		catch
		{
			return false;
		}
	}

	private string BuildShoutTradeFactText(bool isGive)
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return "";
		}
		if (isGive && IsShoutPartyTransferMode(_shoutTradeMode))
		{
			Hero hero = null;
			CharacterObject characterObject = null;
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1)));
				characterObject = agent?.Character as CharacterObject;
				if (_shoutTradeTargetNpc != null && _shoutTradeTargetNpc.IsHero)
				{
					hero = ResolveHeroFromAgentIndex(_shoutTradeTargetNpc.AgentIndex) ?? characterObject?.HeroObject;
				}
			}
			catch
			{
				hero = null;
			}
			List<string> facts = new List<string>();
			for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
			{
				ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
				if (shoutPendingTradeItem?.PartyEntry == null || shoutPendingTradeItem.Amount <= 0)
				{
					continue;
				}
				string item = ((shoutPendingTradeItem.PartyEntry.Section == MyBehavior.PartyTransferEntrySection.PlayerTroops) ? MyBehavior.BuildPlayerToNpcTroopTransferFactForExternal(hero, characterObject, _shoutTradeTargetNpc?.AgentIndex ?? (-1), shoutPendingTradeItem.PartyEntry, shoutPendingTradeItem.Amount) : MyBehavior.BuildPlayerToNpcPrisonerTransferFactForExternal(hero, characterObject, _shoutTradeTargetNpc?.AgentIndex ?? (-1), shoutPendingTradeItem.PartyEntry, shoutPendingTradeItem.Amount));
				if (!string.IsNullOrWhiteSpace(item))
				{
					facts.Add(item);
				}
			}
			return string.Join("\n", facts.Where((string x) => !string.IsNullOrWhiteSpace(x)));
		}
		string text = GetPlayerDisplayNameForShout();
		string text2 = GetShoutTradeTargetDisplayName();
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
					if (!isGive)
					{
						num += shoutPendingTradeItem.Amount;
					}
				}
				else
				{
					string text3;
					if (isGive)
					{
						text3 = shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "";
						string text4 = "";
						try
						{
							Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1)));
							CharacterObject characterObject = agent?.Character as CharacterObject;
							Hero hero = characterObject?.HeroObject;
							if (characterObject != null && hero == null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
							{
								text4 = RewardSystemBehavior.Instance.BuildSettlementItemValueFactSuffixForExternal(Settlement.CurrentSettlement, text3, shoutPendingTradeItem.Amount);
							}
							else
							{
								text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(hero ?? Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
							}
						}
						catch
						{
							text4 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(Hero.MainHero, text3, shoutPendingTradeItem.Amount) ?? "";
						}
						list.Add($"{shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}{text4}");
					}
					else
					{
						string text5 = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? "";
						num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0L;
						list.Add($"{shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}{text5}");
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
			return text + "已经将 " + string.Join("、", list) + " 交给 " + text2 + "。";
		}
		return text + "给 " + text2 + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，证明自己有这些东西。";
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

	private static string GetPlayerDisplayNameForShout()
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
			return text + "" + text + "的身份自然发言，并结合【当前场景公共对话与互动】中其他人刚才说过的话来回应，不要各说各的。只输出你要说的话，不要输出动作描写、心理活动或旁白，尽量以自己的观点为基础，不要附和别人。如果需要写入标签，请把需要的标签都完整写在回复末尾，不要遗漏。";
		}
		return text + "现在正在与" + text2 + "单独交谈。请只以" + text + "的身份自然回应" + text2 + "。只输出你要说的话，不要输出动作描写、心理活动或旁白。如果需要写入标签，请把需要的标签都完整写在回复末尾，不要遗漏。";
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
			return text;
		}
		string text2 = (targetNpc.TroopId ?? "").Trim().ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return "troop:" + text2;
		}
		if (!string.IsNullOrWhiteSpace(targetNpc.Name))
		{
			return ("name:" + targetNpc.Name).Trim().ToLowerInvariant();
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
	}

	private async void OnShoutConfirmed(string shoutText)
	{
		await ProcessShoutConfirmedInternal(shoutText, null, null);
	}

	private async void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		await ProcessShoutConfirmedInternal(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	private async Task ProcessShoutConfirmedInternal(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		_stareTimer = 0f;
		_currentStareTarget = null;
		if (string.IsNullOrWhiteSpace(shoutText))
		{
			ResumeGame();
			return;
		}
		try
		{
			_lastShoutDuelLiteralHit = ContainsLiteralKeywordHit(shoutText, AIConfigHandler.DuelTriggerKeywords);
		}
		catch
		{
			_lastShoutDuelLiteralHit = false;
		}
		int conversationEpoch = BeginNewPlayerDrivenSceneConversationEpoch();
		List<Agent> nearbyAgents = ShoutUtils.GetNearbyNPCAgents();
		if (nearbyAgents.Count > 0)
		{
			ActivateMultiSceneMovementSuppression(nearbyAgents.Select((Agent agent) => agent?.Index ?? (-1)));
		}
		if (nearbyAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f)));
			ResumeGame();
			return;
		}
		if (nearbyAgents.Count == 1)
		{
			string targetName = nearbyAgents[0].Name.ToString();
			InformationManager.DisplayMessage(new InformationMessage(targetName + " 正在思考...", new Color(0.7f, 0.7f, 0.7f)));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("人群正在思考...", new Color(0.7f, 0.7f, 0.7f)));
		}
		string sceneDesc = ShoutUtils.GetCurrentSceneDescription();
		List<NpcDataPacket> allNpcData = nearbyAgents.Select((Agent a) => ShoutUtils.ExtractNpcData(a)).ToList();
		ApplySceneLocalDisambiguatedNames(allNpcData);
		Agent primaryTarget = null;
		if (forcedPrimaryAgentIndex.HasValue)
		{
			primaryTarget = nearbyAgents.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value);
		}
		if (primaryTarget == null)
		{
			List<Agent> namedAgents = new List<Agent>();
			foreach (Agent agent in nearbyAgents)
			{
				if (!string.IsNullOrEmpty(agent.Name) && shoutText.IndexOf(agent.Name, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					namedAgents.Add(agent);
				}
			}
			if (namedAgents.Count > 0)
			{
				primaryTarget = ShoutUtils.GetFacingAgent(namedAgents);
			}
			else
			{
				primaryTarget = ShoutUtils.GetFacingAgent(nearbyAgents);
			}
		}
		NpcDataPacket primaryDataPacket = null;
		if (primaryTarget != null)
		{
			primaryDataPacket = allNpcData.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index);
		}
		string sceneTauntExtraFact = SceneTauntBehavior.BuildFrightenedCivilianShoutExtraFactExternal(primaryTarget);
		if (!string.IsNullOrWhiteSpace(sceneTauntExtraFact))
		{
			extraFact = string.IsNullOrWhiteSpace(extraFact) ? sceneTauntExtraFact : (extraFact + "\n" + sceneTauntExtraFact);
		}
		string combatShoutExtraFact = BuildCombatActiveShoutExtraFact(primaryTarget);
		if (!string.IsNullOrWhiteSpace(combatShoutExtraFact))
		{
			extraFact = string.IsNullOrWhiteSpace(extraFact) ? combatShoutExtraFact : (extraFact + "\n" + combatShoutExtraFact);
		}
		ResetStaringForActiveInteraction(nearbyAgents, primaryTarget);
		ExtendStaringHoldForPlayerDrivenSceneRound(nearbyAgents.Count);
		if (!string.IsNullOrWhiteSpace(extraFact))
		{
			RecordExtraFactToSceneHistory(extraFact, allNpcData);
			_mainThreadActions.Enqueue(delegate
			{
				PersistExtraFactToNamedHeroes(extraFact, allNpcData);
			});
		}
		RecordPlayerSpeechToMessageFeed(shoutText);
		if (_floatingTextView != null && Agent.Main != null)
		{
			_floatingTextView.AddOrUpdateText(Agent.Main, shoutText);
		}
		List<NpcDataPacket> capturedNpcData = allNpcData;
		RecordPlayerMessage(shoutText, capturedNpcData, primaryDataPacket?.AgentIndex ?? (-1), primaryDataPacket?.Name ?? "");
		if ((capturedNpcData?.Count ?? 0) > 1)
		{
			RefreshSceneConversationParticipantInteractions(capturedNpcData, ACTIVE_INTERACTION_IDLE_TIMEOUT);
		}
		else
		{
			TrackPlayerInteraction(primaryDataPacket, capturedNpcData?.Count ?? 0);
		}

		ResumeGame();

		_ = Task.Run(async delegate
		{
			try
			{
				Dictionary<int, PrecomputedShoutRagContext> precomputedContexts = new Dictionary<int, PrecomputedShoutRagContext>();
				Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();

				foreach (Agent agent in nearbyAgents)
				{
					if (agent == null)
					{
						continue;
					}
					HoldSceneConversationAgents(nearbyAgents);
					CharacterObject co = agent.Character as CharacterObject;
					if (co != null && co.HeroObject != null)
					{
						resolvedHeroes[agent.Index] = co.HeroObject;
					}
					string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
					string secondaryInput = GetLatestSceneNpcUtterance(agent.Index);
					LogShoutLorePrequery("group_precalc", agent, co, kingdomIdOverride, shoutText, secondaryInput);
					string lore = AIConfigHandler.GetLoreContext(shoutText, co, kingdomIdOverride, secondaryInput);
					precomputedContexts[agent.Index] = new PrecomputedShoutRagContext
					{
						HasLoreContext = !string.IsNullOrWhiteSpace(lore),
						LoreContext = lore ?? "",
						HasPersistedHistoryContext = false,
						PersistedHistoryContext = ""
					};
				}

				foreach (NpcDataPacket npc in capturedNpcData)
				{
					Agent liveAgent = nearbyAgents.FirstOrDefault(a => a != null && a.Index == npc.AgentIndex);
					if (liveAgent != null)
					{
						if (ShoutUtils.TryGetUnnamedNpcPersona(liveAgent, out var up, out var ub))
						{
							if (string.IsNullOrWhiteSpace(npc.PersonalityDesc) && !string.IsNullOrWhiteSpace(up))
							{
								npc.PersonalityDesc = up.Trim();
							}
							if (string.IsNullOrWhiteSpace(npc.BackgroundDesc) && !string.IsNullOrWhiteSpace(ub))
							{
								npc.BackgroundDesc = ub.Trim();
							}
						}
					}
				}

				await HandleGroupResponse(shoutText, capturedNpcData, sceneDesc, primaryDataPacket, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch);
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[ERROR] ProcessShoutConfirmedInternal background failed: " + ex.Message);
			}
		});
	}

	private async Task HandleGroupResponse(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch)
	{
		try
		{
			ApplySceneLocalDisambiguatedNames(allNpcData);
			using (Logger.BeginTrace("shout_group", null, primaryNpc?.Name))
			{
				Logger.Obs("ShoutGroup", "start", new Dictionary<string, object>
				{
					["primaryNpc"] = primaryNpc?.Name ?? "",
					["inputLen"] = (playerText ?? "").Length,
					["nearbyNpcCount"] = allNpcData?.Count ?? 0
				});
				bool usePerHeroIndependentRequests = true;
				if (usePerHeroIndependentRequests)
				{
					await HandleGroupResponsePerHeroIndependent(playerText, allNpcData, sceneDesc, primaryNpc, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch);
					return;
				}
				DuelSettings settings = DuelSettings.GetSettings();
				int maxCount = 10;
				List<NpcDataPacket> speakingCandidates = new List<NpcDataPacket>();
				if (primaryNpc != null)
				{
					speakingCandidates.Add(primaryNpc);
				}
				if (allNpcData != null)
				{
					foreach (NpcDataPacket n in allNpcData)
					{
						if (n != null && (primaryNpc == null || n.AgentIndex != primaryNpc.AgentIndex))
						{
							speakingCandidates.Add(n);
							if (speakingCandidates.Count >= maxCount)
							{
								break;
							}
						}
					}
				}
				List<string> patienceStatusLines = new List<string>();
				List<NpcDataPacket> speakableCandidates = new List<NpcDataPacket>();
				foreach (NpcDataPacket npc in speakingCandidates)
				{
					if (npc != null)
					{
						bool canSpeak = true;
						string statusLine = "";
						bool hasStatus;
						if (npc.IsHero)
						{
							Hero hero = null;
							if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
							hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
						}
						else
						{
							hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, GetSceneNpcPatienceNameForPrompt(npc), out statusLine, out canSpeak);
						}
						if (hasStatus && !string.IsNullOrWhiteSpace(statusLine))
						{
							patienceStatusLines.Add(statusLine);
						}
						if (canSpeak)
						{
							speakableCandidates.Add(npc);
						}
					}
				}
				speakingCandidates = speakableCandidates;
				if (speakingCandidates.Count == 0)
				{
					_mainThreadActions.Enqueue(delegate
					{
						InformationManager.DisplayMessage(new InformationMessage("周围的人明显不想继续聊下去。", new Color(1f, 0.8f, 0.3f)));
					});
					return;
				}
				await EnsurePersonaForCandidatesAsync(speakingCandidates, resolvedHeroes);
				StringBuilder sysPrompt = new StringBuilder();
				Hero contextHero = null;
				try
				{
					if (primaryNpc != null && primaryNpc.IsHero && resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(primaryNpc.AgentIndex, out contextHero);
					}
					if (contextHero == null)
					{
						NpcDataPacket heroNpc = speakingCandidates.FirstOrDefault((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
						if (heroNpc != null && resolvedHeroes != null)
						{
							resolvedHeroes.TryGetValue(heroNpc.AgentIndex, out contextHero);
						}
					}
				}
				catch
				{
				}
				string cultureId = ((primaryNpc != null) ? primaryNpc.CultureId : "neutral");
				bool hasAnyHero = speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
				int contextAgentIndex = ((primaryNpc != null) ? primaryNpc.AgentIndex : ((speakingCandidates.Count > 0) ? speakingCandidates[0].AgentIndex : (-1)));
				Agent contextAgent = ((contextAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == contextAgentIndex) : null);
				CharacterObject contextCharacter = contextAgent?.Character as CharacterObject;
				string contextKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(contextAgent);
				PrecomputedShoutRagContext contextPrecomputed = null;
				bool hasContextPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(contextAgentIndex, out contextPrecomputed);
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, extraFact, cultureId, hasAnyHero, targetCharacter: contextCharacter, kingdomIdOverride: contextKingdomIdOverride, targetAgentIndex: contextAgentIndex, usePrefetchedLoreContext: hasContextPrecomputed && contextPrecomputed != null && contextPrecomputed.HasLoreContext, prefetchedLoreContext: contextPrecomputed?.LoreContext);
				List<SceneSummonPromptTarget> sceneSummonTargets = BuildSceneSummonPromptTargets(speakingCandidates, resolvedHeroes);
				List<SceneGuidePromptTarget> sceneGuideTargets = BuildSceneGuidePromptTargets();
				sysPrompt.AppendLine("【在场人物列表】：");
				foreach (NpcDataPacket npc2 in speakingCandidates)
				{
					if (npc2 == null)
					{
						continue;
					}
					string line = BuildSceneNpcListLineForPrompt(npc2);
					try
					{
						if (!npc2.IsHero)
						{
							string key = (npc2.UnnamedKey ?? "").Trim().ToLower();
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
								kingdomId = (npc2.CultureId ?? "").Trim().ToLower();
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
								line = line + " | 统治者: " + rulerName;
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
					if (npc2.IsHero)
					{
						try
						{
							Hero hero2 = null;
							if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc2.AgentIndex, out hero2);
							if (hero2?.IsPrisoner ?? false)
							{
								string captor = hero2.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
								line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
							}
							line += BuildPlayerMarriageFactForNpcListLine(hero2);
						}
						catch
						{
						}
					}
					sysPrompt.AppendLine(line);
				}
				string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(speakingCandidates);
				if (!string.IsNullOrWhiteSpace(sceneNamingNote))
				{
					sysPrompt.AppendLine(sceneNamingNote);
				}
				string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(speakingCandidates);
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(primaryNpc);
				string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction);
				string scenePatienceInstruction = "";
				if (patienceStatusLines.Count > 0)
				{
					sysPrompt.AppendLine("【4.三值状态】");
					sysPrompt.AppendLine("【NPC耐心状态】：");
					foreach (string line2 in patienceStatusLines)
					{
						sysPrompt.AppendLine(line2);
					}
					scenePatienceInstruction = MyBehavior.GetScenePatienceInstructionForExternal();
				}
				int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
				int minTokens = maxTokens / 2;
				if (minTokens < 5)
				{
					minTokens = 5;
				}
				sysPrompt.AppendLine("【群体对话规则】");
				string playerNameForPrompt = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForPrompt))
				{
					playerNameForPrompt = "玩家";
				}
				string playerNameForLength = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForLength))
				{
					playerNameForLength = "玩家";
				}
				sysPrompt.AppendLine($"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{playerNameForLength}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)");
				if (!string.IsNullOrWhiteSpace(scenePatienceInstruction))
				{
					sysPrompt.AppendLine(scenePatienceInstruction);
				}
				Stopwatch swPrompt = Stopwatch.StartNew();
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
				string localExtras = InjectTrustBlockBelowTriState(sysPrompt.ToString().Trim(), trustBlock);
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				bool allowAppendSceneMechanismWithoutMarker2 = !string.IsNullOrWhiteSpace(sceneSummonClosureInstruction) || !string.IsNullOrWhiteSpace(sceneFollowControlInstruction);
				layeredPrompt = InjectSceneMechanismPromptSection(layeredPrompt, sceneMechanismPromptSection, allowAppendSceneMechanismWithoutMarker2);
				string roleTopIntro = BuildSceneSystemTopPromptIntroForGroup(speakingCandidates, resolvedHeroes);
				string playerNameForTask = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForTask))
				{
					playerNameForTask = "玩家";
				}
				string taskPreamble = "你是【在场人物列表】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复" + playerNameForTask + "的对话。";
				layeredPrompt = (string.IsNullOrWhiteSpace(roleTopIntro) ? taskPreamble : (roleTopIntro + "\n" + taskPreamble)) + "\n" + layeredPrompt;
				swPrompt.Stop();
				int fixedChars = CountPromptChars(fixedLayerText);
				int deltaChars = CountPromptChars(deltaLayerText);
				int totalChars = CountPromptChars(layeredPrompt);
				Logger.Log("ShoutBehavior(主动)", $"[PromptLayer] fixedChars={fixedChars} deltaChars={deltaChars} totalChars={totalChars}");
				Logger.Obs("Prompt", "compose", new Dictionary<string, object>
				{
					["mode"] = "shout_group",
					["fixedChars"] = fixedChars,
					["deltaChars"] = deltaChars,
					["totalChars"] = totalChars,
					["buildMs"] = Math.Round(swPrompt.Elapsed.TotalMilliseconds, 2),
					["speakingCandidates"] = speakingCandidates.Count
				});
				Logger.Metric("prompt.compose.shout_group", ok: true, swPrompt.Elapsed.TotalMilliseconds);
				List<object> messages = new List<object>
				{
					new
					{
						role = "system",
						content = layeredPrompt
					}
				};
				int historySourceIndex = -1;
				if (primaryNpc != null && speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket != null && npcDataPacket.AgentIndex == primaryNpc.AgentIndex))
				{
					historySourceIndex = primaryNpc.AgentIndex;
				}
				else if (speakingCandidates.Count > 0)
				{
					historySourceIndex = speakingCandidates[0].AgentIndex;
				}
				StringBuilder historyDump = null;
				List<string> historyLines = null;
				if (historySourceIndex != -1)
				{
					lock (_historyLock)
					{
						if (_publicConversationHistory.Count > 0)
						{
							historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, historySourceIndex, (primaryNpc != null) ? GetSceneNpcHistoryNameForPrompt(primaryNpc) : "", useNpcNameAddress: true);
							if (historyLines != null && historyLines.Count > 0)
							{
								historyDump = new StringBuilder();
								historyDump.AppendLine($">>> History(viewerAgentIndex={historySourceIndex}) count={historyLines.Count}");
								for (int i = 0; i < historyLines.Count; i++)
								{
									historyDump.AppendLine(historyLines[i]);
								}
							}
						}
					}
				}
				if (historyDump != null)
				{
					Logger.Log("ShoutBehavior(主动)", historyDump.ToString());
				}
				int persistedHistorySourceIndex = historySourceIndex;
				if (persistedHistorySourceIndex == -1)
				{
					NpcDataPacket npcDataPacket2 = speakingCandidates.FirstOrDefault((NpcDataPacket x) => x != null && x.IsHero);
					if (npcDataPacket2 != null)
					{
						persistedHistorySourceIndex = npcDataPacket2.AgentIndex;
					}
				}
				string persistedHeroHistory = ((persistedHistorySourceIndex == -1) ? "" : GetOrBuildPrecomputedPersistedHistoryContext(persistedHistorySourceIndex, playerText, resolvedHeroes, precomputedContexts));
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(主动)", $"[HistoryBridge] heroAgentIndex={persistedHistorySourceIndex} chars={persistedHeroHistory.Length}");
				}
				string privateRecentWindowSection = "";
				string persistedWithoutRecentWindow = "";
				SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
				string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
				if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
				{
					layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
				}
				if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
				{
					layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
				}
				string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(persistedHistorySourceIndex, resolvedHeroes);
				if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
				{
					layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
				}
				if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
				{
					layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
				}
				messages[0] = new
				{
					role = "system",
					content = layeredPrompt
				};
				string playerNameForUser = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(playerNameForUser))
				{
					playerNameForUser = "玩家";
				}
				string userMessage = "现在请你直接以【在场人物列表】中角色的身份回复" + playerNameForUser + "。直接输出对话内容，格式为'角色名: 对话内容'，每人一行。\n禁止生成任何新的【】章节标题、编号规则列表、格式说明或回复要求。禁止替" + playerNameForUser + "说话或编造" + playerNameForUser + "的台词。\n立即开始输出角色对话：";
				messages.Add(new
				{
					role = "user",
					content = userMessage
				});
				Logger.Log("ShoutBehavior(主动)", ">>> Prompt:\n" + layeredPrompt);
				Logger.Log("ShoutBehavior(主动)", ">>> 玩家: " + playerText);
				FloatingTextManager ftm = FloatingTextManager.Instance;
				ftm.BeginStream(speakingCandidates);
				List<NpcDataPacket> capturedAllNpcData = allNpcData;
				List<SceneSummonPromptTarget> capturedSceneSummonTargets = CloneSceneSummonPromptTargets(sceneSummonTargets);
				List<SceneGuidePromptTarget> capturedSceneGuideTargets = CloneSceneGuidePromptTargets(sceneGuideTargets);
				bool hasAnyQueuedLine = false;
				ftm.OnPartialLineUpdated = null;
				ftm.OnNewLineReady = delegate(NpcDataPacket npcDataPacket, string content)
				{
					if (npcDataPacket != null && !string.IsNullOrWhiteSpace(content))
					{
						hasAnyQueuedLine = true;
						EnqueueSpeechLine(npcDataPacket, content.Trim(), capturedAllNpcData, skipHistory: false, suppressStare: false, capturedSceneSummonTargets, capturedSceneGuideTargets);
					}
				};
				bool streamCompleted = false;
				bool streamFailed = false;
				string streamError = null;
				string streamFinalText = null;
				bool firstChunkSeen = false;
				Stopwatch swApi = Stopwatch.StartNew();
				double firstChunkMs = -1.0;
				await ShoutNetwork.CallApiWithMessagesStream(messages, 5000, delegate(string delta)
				{
					if (!firstChunkSeen)
					{
						firstChunkSeen = true;
						firstChunkMs = swApi.Elapsed.TotalMilliseconds;
						Logger.Obs("API", "first_chunk", new Dictionary<string, object>
						{
							["mode"] = "shout_group_stream",
							["firstChunkMs"] = Math.Round(firstChunkMs, 2),
							["deltaLen"] = (delta ?? "").Length
						});
					}
					try
					{
						ftm.AppendChunk(delta);
					}
					catch
					{
					}
				}, delegate(string full)
				{
					streamFinalText = full ?? "";
					streamCompleted = true;
				}, delegate(string err)
				{
					streamError = err ?? "（未知流式错误）";
					streamFailed = true;
				});
				swApi.Stop();
				if (streamFailed)
				{
					Logger.Log("ShoutBehavior(主动)", "<<< 流式错误: " + streamError);
					_mainThreadActions.Enqueue(delegate
					{
						InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + streamError, new Color(1f, 0.3f, 0.3f)));
					});
				}
				ftm.EndStream();
				ftm.OnPartialLineUpdated = null;
				ftm.OnNewLineReady = null;
				if (streamCompleted)
				{
					Logger.Log("ShoutBehavior(主动)", "<<< AI回复完成(流式):\n" + streamFinalText);
				}
				Logger.Obs("API", "complete", new Dictionary<string, object>
				{
					["mode"] = "shout_group_stream",
					["ok"] = !streamFailed,
					["error"] = streamError ?? "",
					["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
					["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
					["resultLen"] = (streamFinalText ?? "").Length,
					["hasAnyQueuedLine"] = hasAnyQueuedLine
				});
				Logger.Metric("api.shout_group.stream", !streamFailed, swApi.Elapsed.TotalMilliseconds);
				NpcDataPacket fallbackNpc;
				Agent liveAgent;
				CharacterObject co = default(CharacterObject);
				int num;
				string c;
				if (!hasAnyQueuedLine)
				{
					string fullText = streamFinalText ?? "";
					fallbackNpc = null;
					if (primaryNpc != null)
					{
						fallbackNpc = primaryNpc;
					}
					else if (speakingCandidates != null && speakingCandidates.Count > 0)
					{
						fallbackNpc = speakingCandidates[0];
					}
					if (fallbackNpc != null)
					{
						c = StripNpcNamePrefixSafely((fullText ?? "").Trim(), 20);
						c = Regex.Replace(c, "\\（.*?\\）", "");
						c = Regex.Replace(c, "\\(.*?\\)", "");
						c = Regex.Replace(c, "\\*.*?\\*", "");
						c = (c ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(c))
						{
							liveAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == fallbackNpc.AgentIndex);
							if (liveAgent != null)
							{
								BasicCharacterObject character = liveAgent.Character;
								co = character as CharacterObject;
								if (co != null)
								{
									num = ((co.HeroObject != null) ? 1 : 0);
									goto IL_1cba;
								}
							}
							num = 0;
							goto IL_1cba;
						}
					}
				}
				goto IL_1de3;
				IL_1de3:
				Logger.Obs("ShoutGroup", "done", new Dictionary<string, object>
				{
					["streamFailed"] = streamFailed,
					["hasAnyQueuedLine"] = hasAnyQueuedLine,
					["speakingCandidates"] = speakingCandidates.Count
				});
				goto end_IL_0086;
				IL_1cba:
				if (num != 0)
				{
					MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(co.HeroObject, ref c);
				}
				else
				{
					MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(fallbackNpc.UnnamedKey, fallbackNpc.Name, ref c);
				}
				c = StripLeakedPromptContentForShout(c);
				if (string.IsNullOrWhiteSpace(c))
				{
					return;
				}
				ShowNpcSpeechOutput(fallbackNpc, liveAgent, c);
				RecordResponseForAllNearbySafe(capturedAllNpcData, fallbackNpc.AgentIndex, fallbackNpc.Name, c);
				PersistNpcSpeechToNamedHeroes(fallbackNpc.AgentIndex, fallbackNpc.Name, c, capturedAllNpcData);
				goto IL_1de3;
				end_IL_0086:;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("ShoutBehavior", "[ERROR] " + ex2.Message);
			Logger.Obs("ShoutGroup", "error", new Dictionary<string, object>
			{
				["message"] = ex2.Message,
				["type"] = ex2.GetType().Name
			});
			Logger.Metric("api.shout_group.stream", ok: false);
		}
	}

	private async Task HandleGroupResponsePerHeroIndependent(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact, Dictionary<int, PrecomputedShoutRagContext> precomputedContexts, Dictionary<int, Hero> resolvedHeroes, int conversationEpoch)
	{
		try
		{
			ApplySceneLocalDisambiguatedNames(allNpcData);
			if (!IsSceneConversationEpochCurrent(conversationEpoch))
			{
				return;
			}
			if (allNpcData == null)
			{
				allNpcData = new List<NpcDataPacket>();
			}
			int maxCount = 10;
			List<NpcDataPacket> speakingCandidates = new List<NpcDataPacket>();
			if (primaryNpc != null)
			{
				speakingCandidates.Add(primaryNpc);
			}
			foreach (NpcDataPacket n in allNpcData)
			{
				if (n != null && (primaryNpc == null || n.AgentIndex != primaryNpc.AgentIndex))
				{
					speakingCandidates.Add(n);
					if (speakingCandidates.Count >= maxCount)
					{
						break;
					}
				}
			}
			List<NpcDataPacket> speakableCandidates = new List<NpcDataPacket>();
			List<string> patienceStatusLines = new List<string>();
			foreach (NpcDataPacket npc in speakingCandidates)
			{
				if (npc == null)
				{
					continue;
				}
				bool canSpeak = true;
				string statusLine = "";
				bool hasStatus;
				if (npc.IsHero)
				{
					Hero hero = null;
					if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
					hasStatus = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out statusLine, out canSpeak);
				}
				else
				{
					hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, GetSceneNpcPatienceNameForPrompt(npc), out statusLine, out canSpeak);
				}
				if (hasStatus && !string.IsNullOrWhiteSpace(statusLine))
				{
					patienceStatusLines.Add(statusLine);
				}
				if (hasStatus && !canSpeak)
				{
					continue;
				}
				speakableCandidates.Add(npc);
			}
			if (speakableCandidates.Count == 0)
			{
				_mainThreadActions.Enqueue(delegate
				{
					InformationManager.DisplayMessage(new InformationMessage("周围的人明显不想继续聊下去。", new Color(1f, 0.8f, 0.3f)));
				});
				return;
			}
			await EnsurePersonaForCandidatesAsync(speakableCandidates, resolvedHeroes);
			HoldSceneConversationParticipants(speakableCandidates);
			DuelSettings settings = DuelSettings.GetSettings();
			int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
			int minTokens = Math.Max(5, maxTokens / 2);
			string lastSpeakerOutputText = "";
			List<SceneSummonPromptTarget> sceneSummonTargets = BuildSceneSummonPromptTargets(speakableCandidates, resolvedHeroes);
			List<SceneGuidePromptTarget> sceneGuideTargets = BuildSceneGuidePromptTargets();
			StringBuilder commonCandidatesList = new StringBuilder();
			commonCandidatesList.AppendLine("【在场人物列表】：");
			foreach (NpcDataPacket npc2 in speakableCandidates)
			{
				if (npc2 == null)
				{
					continue;
				}
				string line = BuildSceneNpcListLineForPrompt(npc2, includeRelayId: true);
				try
				{
					if (!npc2.IsHero)
					{
						string key = (npc2.UnnamedKey ?? "").Trim().ToLower();
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
							kingdomId = (npc2.CultureId ?? "").Trim().ToLower();
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
							line = line + " | 统治者: " + rulerName;
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
				if (npc2.IsHero)
				{
					try
					{
						Hero hero2 = ResolveHeroFromAgentIndex(npc2.AgentIndex);
						if (hero2?.IsPrisoner ?? false)
						{
							string captor = hero2.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
							line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
						}
						line += BuildPlayerMarriageFactForNpcListLine(hero2);
					}
					catch
					{
					}
				}
				commonCandidatesList.AppendLine(line);
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(speakableCandidates);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				commonCandidatesList.AppendLine(sceneNamingNote);
			}
			string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(speakableCandidates);
			string sceneMechanismPromptSectionBase = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, null);
			bool multiNpcScene = speakableCandidates.Count > 1;
			NpcDataPacket currentSpeaker = ((primaryNpc != null) ? speakableCandidates.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == primaryNpc.AgentIndex) : null) ?? speakableCandidates.FirstOrDefault();
			bool firstTurn = true;
			int remainingTurns = multiNpcScene ? AUTO_GROUP_CHAT_MAX_LINES : 1;
			while (currentSpeaker != null && remainingTurns-- > 0)
			{
				if (!IsSceneConversationEpochCurrent(conversationEpoch))
				{
					return;
				}
				HoldSceneConversationParticipants(speakableCandidates);
				string cleaned = "";
				bool endRequested = false;
				int resolvedRelayTargetAgentIndex = -1;
				if (firstTurn)
				{
					Hero contextHero = null;
					if (currentSpeaker.IsHero && resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(currentSpeaker.AgentIndex, out contextHero);
					}
					string cultureId = currentSpeaker.CultureId ?? "neutral";
					PrecomputedShoutRagContext precomputed = null;
					bool hasPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(currentSpeaker.AgentIndex, out precomputed);
					string loreContext = (hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "";
					string fullExtra = string.IsNullOrWhiteSpace(loreContext) ? null : loreContext;
					if (!string.IsNullOrWhiteSpace(extraFact))
					{
						fullExtra = ((fullExtra == null) ? extraFact : (fullExtra + "\n" + extraFact));
					}
					Agent npcAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == currentSpeaker.AgentIndex);
					CharacterObject npcCharacter = npcAgent?.Character as CharacterObject;
					string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
					MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, fullExtra, cultureId, hasAnyHero: currentSpeaker.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: currentSpeaker.AgentIndex, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext);
					StringBuilder local = new StringBuilder();
					local.Append(commonCandidatesList);
					string scenePatienceInstruction = "";
					if (patienceStatusLines.Count > 0)
					{
						local.AppendLine("【4.三值状态】");
						local.AppendLine("【NPC耐心状态】：");
						foreach (string line2 in patienceStatusLines)
						{
							local.AppendLine(line2);
						}
						scenePatienceInstruction = MyBehavior.GetScenePatienceInstructionForExternal();
					}
					string fixedLayerText = "";
					string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
					string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
					string localExtras = InjectTrustBlockBelowTriState(local.ToString().Trim(), trustBlock);
					string deltaLayerText = string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust);
					string layeredPrompt = string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText);
					string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(currentSpeaker);
					string sceneMechanismPromptSection = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction);
					bool allowAppendSceneMechanismWithoutMarker3 = !string.IsNullOrWhiteSpace(sceneSummonClosureInstruction) || !string.IsNullOrWhiteSpace(sceneFollowControlInstruction);
					layeredPrompt = InjectSceneMechanismPromptSection(layeredPrompt, sceneMechanismPromptSection, allowAppendSceneMechanismWithoutMarker3);
					List<string> historyLines = null;
					lock (_historyLock)
					{
						if (_publicConversationHistory.Count > 0)
						{
							historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, currentSpeaker.AgentIndex, GetSceneNpcHistoryNameForPrompt(currentSpeaker), multiNpcScene);
						}
					}
					string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
					string persistedHeroHistory = currentSpeaker.IsHero ? GetOrBuildPrecomputedPersistedHistoryContext(currentSpeaker.AgentIndex, playerText, resolvedHeroes, precomputedContexts) : "";
					string privateRecentWindowSection = "";
					string persistedWithoutRecentWindow = "";
					SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
					if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
					{
						layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
					}
					if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
					{
						layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
					}
					string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(contextHero);
					if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
					{
						layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
					}
					if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
					{
						layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
					}
					bool includeInventorySummary = ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext || ctx.UseDuelContext);
					bool includeTradePricing = includeInventorySummary;
					string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(currentSpeaker, contextHero, speakableCandidates, includeInventorySummary, includeTradePricing);
					if (!string.IsNullOrWhiteSpace(roleTopIntro))
					{
						layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
					}
					string singleReplyPlayerName = GetPlayerDisplayNameForShout();
					if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
					{
						singleReplyPlayerName = "玩家";
					}
					string singleReplyUserContent = BuildSingleNpcSceneReplyInstruction(GetSceneNpcHistoryNameForPrompt(currentSpeaker), multiNpcScene) + "\n" + $"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{singleReplyPlayerName}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)" + (string.IsNullOrWhiteSpace(scenePatienceInstruction) ? "" : ("\n" + scenePatienceInstruction));
					if (multiNpcScene)
					{
						singleReplyUserContent += "\n(这是一场多人聊天，你可以在回复末尾输出 [RELAY:接力编号]，指定下一个发言的人，尽量让没在【当前场景公共对话与互动】说过话的人发言)";
					}
					List<object> messages = new List<object>
					{
						new
						{
							role = "system",
							content = layeredPrompt
						},
						new
						{
							role = "user",
							content = singleReplyUserContent
						}
					};
					string output = await ShoutNetwork.CallApiWithMessages(messages, 5000);
					if (!IsSceneConversationEpochCurrent(conversationEpoch))
					{
						return;
					}
					if (!string.IsNullOrWhiteSpace(output) && (output.StartsWith("（错误") || output.StartsWith("（程序错误") || output.StartsWith("（API请求失败")))
					{
						Logger.Log("ShoutBehavior(主动)", "<<< API错误: " + output);
						_mainThreadActions.Enqueue(delegate
						{
							InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + output, new Color(1f, 0.3f, 0.3f)));
						});
						break;
					}
					cleaned = StripNpcNamePrefixSafely((output ?? "").Replace("\r", "").Trim(), 30);
				}
				else
				{
					cleaned = await GenerateGroupConversationTurnLineAsync(currentSpeaker, speakableCandidates, resolvedHeroes, precomputedContexts, playerText, extraFact, commonCandidatesList.ToString(), sceneMechanismPromptSectionBase, patienceStatusLines, multiNpcScene, minTokens, maxTokens);
					if (!IsSceneConversationEpochCurrent(conversationEpoch))
					{
						return;
					}
				}
				cleaned = StripLeakedPromptContentForShout(cleaned);
				cleaned = StripStageDirectionsForPassiveShout(cleaned);
				endRequested = ContainsAutoGroupEndSignal(cleaned);
				int relayTargetAgentIndex = -1;
				bool relayRequested = multiNpcScene && !string.IsNullOrWhiteSpace(cleaned) && TryParseAutoGroupRelayTargetAgentIndex(cleaned, out relayTargetAgentIndex);
				bool flag9 = endRequested && IsAgentFollowingPlayerBySceneCommand(Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == currentSpeaker.AgentIndex && a.IsActive()));
				bool flag10 = endRequested && TryGetSceneSummonConversationSessionForAgentIndex(currentSpeaker.AgentIndex) != null;
				cleaned = StripAutoGroupRelaySignal(cleaned);
				cleaned = StripAutoGroupStopSignal(cleaned);
				if (endRequested)
				{
					if (flag9)
					{
						cleaned = (string.IsNullOrWhiteSpace(cleaned) ? "" : (cleaned + " ")) + "[STP]";
					}
					if (flag10)
					{
						cleaned = (string.IsNullOrWhiteSpace(cleaned) ? "" : (cleaned + " ")) + "[END]";
					}
				}
				if (!string.IsNullOrWhiteSpace(cleaned))
				{
					lastSpeakerOutputText = cleaned;
					string historyText = SanitizeSceneSpeechText(cleaned);
					if (!string.IsNullOrWhiteSpace(historyText))
					{
						RecordResponseForAllNearbySafe(allNpcData, currentSpeaker.AgentIndex, currentSpeaker.Name, historyText);
						PersistNpcSpeechToNamedHeroes(currentSpeaker.AgentIndex, currentSpeaker.Name, historyText, allNpcData);
					}
					EnqueueSpeechLineWithOptions(currentSpeaker, cleaned, allNpcData, commitHistory: false, suppressStare: false, allowPlayerDirectedActions: true, conversationEpoch, sceneSummonTargets, sceneGuideTargets);
				}
				else
				{
					lastSpeakerOutputText = "";
				}
				resolvedRelayTargetAgentIndex = (!string.IsNullOrWhiteSpace(cleaned) && relayRequested) ? ResolveAutoGroupRelayTargetAgentIndex(relayTargetAgentIndex, currentSpeaker.AgentIndex, speakableCandidates, resolvedHeroes) : (-1);
				if (!multiNpcScene || endRequested || resolvedRelayTargetAgentIndex < 0)
				{
					break;
				}
				currentSpeaker = speakableCandidates.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == resolvedRelayTargetAgentIndex);
				firstTurn = false;
			}
			if (multiNpcScene)
			{
				PrepareAutoGroupParticipantsForIdleTimeout(speakableCandidates, lastSpeakerOutputText);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] HandleGroupResponsePerHeroIndependent: " + ex.Message);
		}
	}

	private void HoldSceneConversationParticipants(List<NpcDataPacket> participants)
	{
		if (ShouldSuppressSceneConversationControlForMeeting() || participants == null || participants.Count <= 1 || Mission.Current == null)
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
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npcDataPacket.AgentIndex);
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

	private static NpcDataPacket CloneNpcDataPacket(NpcDataPacket npc)
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
			PromptDisplayName = npc.PromptDisplayName
		};
	}

	private static List<NpcDataPacket> CloneNpcDataSnapshot(List<NpcDataPacket> allNpcData)
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

	private static void ApplySceneLocalDisambiguatedNames(List<NpcDataPacket> allNpcData)
	{
		if (allNpcData == null || allNpcData.Count == 0)
		{
			return;
		}
		ShoutUtils.EnsureScenePromptNames(allNpcData);
	}

	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null)
	{
		EnqueueSpeechLineWithOptions(npc, content, allNpcData, !skipHistory, suppressStare, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, sceneSummonTargets, sceneGuideTargets);
	}

	private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null)
	{
		if (npc == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		NpcDataPacket value = CloneNpcDataPacket(npc);
		List<NpcDataPacket> value2 = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(value2);
		NpcDataPacket npcDataPacket = value2.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == value.AgentIndex);
		if (npcDataPacket != null)
		{
			value.Name = npcDataPacket.Name;
			value.PromptGivenName = npcDataPacket.PromptGivenName;
			value.PromptDisplayName = npcDataPacket.PromptDisplayName;
		}
		lock (_speechQueueLock)
		{
			_speechQueue.Enqueue(new SceneSpeechQueueItem
			{
				Npc = value,
				Content = content,
				ContextSnapshot = value2,
				SceneSummonTargets = CloneSceneSummonPromptTargets(sceneSummonTargets),
				SceneGuideTargets = CloneSceneGuidePromptTargets(sceneGuideTargets),
				CommitHistory = commitHistory,
				SuppressStare = suppressStare,
				AllowPlayerDirectedActions = allowPlayerDirectedActions,
				RequiredConversationEpoch = requiredConversationEpoch
			});
			if (_speechWorkerRunning)
			{
				return;
			}
			_speechWorkerRunning = true;
		}
		_ = Task.Run(async delegate
		{
			await RunSpeechQueueWorker();
		});
	}

	private async Task RunSpeechQueueWorker()
	{
		try
		{
			while (true)
			{
				if (_isProcessingShout)
				{
					await Task.Delay(100);
					continue;
				}
				SceneSpeechQueueItem item;
				lock (_speechQueueLock)
				{
					if (_speechQueue.Count == 0)
					{
						_speechWorkerRunning = false;
						break;
					}
					item = _speechQueue.Dequeue();
				}
				NpcDataPacket matchedNpc = item.Npc;
				string content = item.Content;
				List<NpcDataPacket> allNpcData = item.ContextSnapshot ?? new List<NpcDataPacket>();
				List<SceneSummonPromptTarget> sceneSummonTargets = item.SceneSummonTargets;
				List<SceneGuidePromptTarget> sceneGuideTargets = item.SceneGuideTargets;
				bool commitHistory = item.CommitHistory;
				bool suppressStare = item.SuppressStare;
				bool allowPlayerDirectedActions = item.AllowPlayerDirectedActions;
				int requiredConversationEpoch = item.RequiredConversationEpoch;
				_mainThreadActions.Enqueue(delegate
				{
					try
					{
						if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
						{
							return;
						}
						Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == matchedNpc.AgentIndex);
						if (!CanAgentParticipateInSceneSpeech(agent))
						{
							return;
						}
						bool flag = false;
						bool flagSceneTaunt = false;
						bool flagSceneTaunt2 = false;
						try
						{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref content);
								if (allowPlayerDirectedActions)
								{
									DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref content);
									DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref content);
									VanillaIssueOfferBridge.ApplyIssueOfferTags(characterObject.HeroObject, ref content);
									if (RewardSystemBehavior.Instance != null)
									{
										RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref content);
										List<string> list2 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
										if (list2 != null)
										{
											foreach (string item2 in list2)
											{
												RecordSystemFactForNearbySafe(allNpcData, item2);
											}
										}
									}
									if (RomanceSystemBehavior.Instance != null)
									{
										RomanceSystemBehavior.Instance.ApplyMarriageTags(characterObject.HeroObject, Hero.MainHero, ref content);
									}
									if (MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject.HeroObject, characterObject, matchedNpc.AgentIndex, ref content, out var generatedFacts, out var notifications))
									{
										if (generatedFacts != null)
										{
											foreach (string generatedFact in generatedFacts)
											{
												RecordSystemFactForNearbySafe(allNpcData, generatedFact);
												MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, generatedFact);
											}
										}
										if (notifications != null)
										{
											foreach (string notification in notifications)
											{
												if (!string.IsNullOrWhiteSpace(notification))
												{
													InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
												}
											}
										}
									}
									if (!ShouldSuppressSceneConversationControlForMeeting())
									{
										LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref content, out flag);
									}
									else
									{
										StripMeetingTauntTagsForSceneConversation(ref content);
									}
									flagSceneTaunt2 = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
								}
							}
							else
							{
								MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(matchedNpc.UnnamedKey, matchedNpc.Name, ref content);
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject2 && RewardSystemBehavior.Instance != null)
								{
									RewardSystemBehavior.Instance.ApplyMerchantRewardTags(characterObject2, Hero.MainHero, ref content);
									List<string> list = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
									if (list != null)
									{
										foreach (string item2 in list)
										{
											RecordSystemFactForNearbySafe(allNpcData, item2);
										}
									}
								}
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject4 && MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject4.HeroObject, characterObject4, matchedNpc.AgentIndex, ref content, out var generatedFacts2, out var notifications2))
								{
									if (generatedFacts2 != null)
									{
										foreach (string generatedFact2 in generatedFacts2)
										{
											RecordSystemFactForNearbySafe(allNpcData, generatedFact2);
											if (characterObject4.HeroObject != null)
											{
												MyBehavior.AppendExternalDialogueHistory(characterObject4.HeroObject, null, null, generatedFact2);
											}
										}
									}
									if (notifications2 != null)
									{
										foreach (string notification2 in notifications2)
										{
											if (!string.IsNullOrWhiteSpace(notification2))
											{
												InformationManager.DisplayMessage(new InformationMessage(notification2, new Color(0.4f, 1f, 0.4f)));
											}
										}
									}
								}
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject3)
								{
									flagSceneTaunt2 = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
								}
							}
						}
						catch
						{
						}
						content = StripLeakedPromptContentForShout(content);
						content = StripStageDirectionsForPassiveShout(content);
						if (!allowPlayerDirectedActions)
						{
							content = StripActionTagsForSceneSpeech(content);
						}
						SceneSummonConversationSession sceneSummonConversationSession = null;
						ActiveSceneSummonRequest activeSceneSummonRequest = null;
						ActiveSceneGuideRequest activeSceneGuideRequest = null;
						bool flag7 = allowPlayerDirectedActions && !flagSceneTaunt && TryConsumeSceneFollowStopTag(matchedNpc, agent, ref content);
						bool flag8 = allowPlayerDirectedActions && !flagSceneTaunt && TryConsumeSceneFollowStartTag(matchedNpc, agent, ref content);
						bool flag5 = allowPlayerDirectedActions && !flagSceneTaunt && TryConsumeSceneEndChatActionTag(matchedNpc, agent, ref content, out sceneSummonConversationSession);
						if (flagSceneTaunt2 && string.IsNullOrWhiteSpace(content))
						{
							content = BuildFallbackSceneTauntSpeech(flagSceneTaunt);
						}
						if (flagSceneTaunt2 && !IsMeetingSceneConversationReleaseSensitive() && string.IsNullOrWhiteSpace(content))
						{
							ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true);
							if (flagSceneTaunt && matchedNpc.AgentIndex >= 0)
							{
								InterruptAgentSpeechForCombat(matchedNpc.AgentIndex, "scene_taunt_action");
							}
							return;
						}
						if (!string.IsNullOrWhiteSpace(content))
						{
							bool flag2 = allowPlayerDirectedActions && !flag && !flagSceneTaunt && ShoutUtils.TryTriggerDuelAction(matchedNpc, ref content);
							bool flag3 = allowPlayerDirectedActions && !flagSceneTaunt && TryTriggerOpenLordsHallAction(matchedNpc, agent, ref content);
							bool flag6 = allowPlayerDirectedActions && !flagSceneTaunt && TryTriggerSceneSummonAction(matchedNpc, agent, sceneSummonTargets, ref content, out activeSceneSummonRequest);
							bool flag10 = allowPlayerDirectedActions && !flagSceneTaunt && TryTriggerSceneGuideAction(matchedNpc, agent, sceneGuideTargets, sceneSummonTargets, ref content, out activeSceneGuideRequest);
							if (!string.IsNullOrWhiteSpace(content))
							{
								if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
								{
									return;
								}
								string historyText = SanitizeSceneSpeechText(content);
								RefreshSceneSummonConversationForSpeaker((agent != null) ? agent.Index : matchedNpc.AgentIndex);
								bool flag4 = IsAgentHostileToMainAgent(agent);
								SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = ShowNpcSpeechOutput(matchedNpc, agent, historyText, allowTts: true, attachTtsToSceneAgent: true);
								if (flag7)
								{
									ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, sceneSpeechPlaybackInfo);
								}
								else if (flag8)
								{
									ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, sceneSpeechPlaybackInfo);
								}
								if (flag5 && sceneSummonConversationSession != null)
								{
									_pendingSceneSummonReturnsAfterSpeech[matchedNpc.AgentIndex] = new PendingSceneSummonReturnAfterSpeech
									{
										AgentIndex = matchedNpc.AgentIndex,
										Session = sceneSummonConversationSession,
										ReturnOnlySpeaker = ShouldReturnOnlySceneSummonSpeaker(sceneSummonConversationSession, agent)
									};
								}
								ScheduleSceneSummonReturnAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								ScheduleSceneGuideReturnAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								ScheduleSceneAutonomyRestoreAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								if (flag6 && activeSceneSummonRequest != null)
								{
									SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, sceneSpeechPlaybackInfo, historyText);
								}
								if (flag10 && activeSceneGuideRequest != null)
								{
									SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, sceneSpeechPlaybackInfo, historyText);
								}
								if (flag4)
								{
									RefreshHostileCombatAgentAutonomy(agent);
								}
								if (CanAgentParticipateInSceneSpeech(agent) && !suppressStare && !flag4 && !flag6 && !flag10)
								{
									HoldSceneConversationParticipants(allNpcData);
									AddAgentToStareList(agent, interruptCurrentUse: false);
								}
								if (commitHistory && !string.IsNullOrWhiteSpace(historyText))
								{
									RecordResponseForAllNearbySafe(allNpcData, matchedNpc.AgentIndex, matchedNpc.Name, historyText);
									PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, historyText, allNpcData);
								}
								if (flagSceneTaunt2 && !IsMeetingSceneConversationReleaseSensitive())
								{
									ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true);
								}
							}
							else if (flag7)
							{
								ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, null);
							}
							else if (flag8)
							{
								ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, null);
							}
							if (flag2)
							{
								ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
								DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
								ShoutUtils.ExecuteDuel(agent);
							}
							if (flag3)
							{
								return;
							}
							else if (flag6 && activeSceneSummonRequest != null && string.IsNullOrWhiteSpace(content))
							{
								SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, null, "");
							}
							else if (flag10 && activeSceneGuideRequest != null && string.IsNullOrWhiteSpace(content))
							{
								SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, null, "");
							}
						}
						if (flag5 && sceneSummonConversationSession != null && string.IsNullOrWhiteSpace(content))
						{
							BeginSceneSummonConversationReturn(sceneSummonConversationSession);
						}
					}
					catch (Exception ex)
					{
						Logger.Log("ShoutBehavior", "[ERROR] RunSpeechQueueWorker mainThread: " + ex.Message);
					}
				});
				await Task.Delay(2000);
				item = null;
			}
		}
		catch
		{
			lock (_speechQueueLock)
			{
				_speechWorkerRunning = false;
			}
		}
	}

	private void RecordExtraFactToSceneHistory(string extraFact, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return;
		}
		string text = extraFact.Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) && !text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			text = "[AFEF玩家行为补充] " + text;
		}
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "system",
				Content = text,
				SpeakerName = "系统",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			});
			if (_publicConversationHistory.Count > 40)
			{
				_publicConversationHistory.RemoveAt(0);
			}
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				if (!_npcConversationHistory.ContainsKey(agentIndex))
				{
					_npcConversationHistory[agentIndex] = new List<ConversationMessage>();
				}
				_npcConversationHistory[agentIndex].Add(new ConversationMessage
				{
					Role = "system",
					Content = text,
					SpeakerName = "系统",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				});
				while (_npcConversationHistory[agentIndex].Count > 40)
				{
					_npcConversationHistory[agentIndex].RemoveAt(0);
				}
			}
		}
	}

	private void PersistExtraFactToNamedHeroes(string extraFact, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return;
		}
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum != null && nearbyDatum.IsHero)
			{
				Hero hero = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				if (hero != null)
				{
					MyBehavior.AppendExternalDialogueHistory(hero, null, null, extraFact);
				}
			}
		}
	}

	private void RecordPlayerMessage(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "")
	{
		// Must happen first so the dynamic AFEF fact sits directly above the player's current scene utterance.
		TryInjectSceneRevisitFactsBeforePlayerMessage(nearbyData);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text2 = ResolveSceneTargetNameForPrompt(primaryTargetAgentIndex, primaryTargetName, nearbyData);
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "user",
				Content = text,
				SpeakerName = "你",
				SpeakerAgentIndex = -1,
				TargetAgentIndex = primaryTargetAgentIndex,
				TargetName = text2,
				VisibleAgentIndices = visibleAgentIndices
			});
			if (_publicConversationHistory.Count > 40)
			{
				_publicConversationHistory.RemoveAt(0);
			}
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				if (!_npcConversationHistory.ContainsKey(agentIndex))
				{
					_npcConversationHistory[agentIndex] = new List<ConversationMessage>();
				}
				_npcConversationHistory[agentIndex].Add(new ConversationMessage
				{
					Role = "user",
					Content = text,
					SpeakerName = "你",
					SpeakerAgentIndex = -1,
					TargetAgentIndex = primaryTargetAgentIndex,
					TargetName = text2,
					VisibleAgentIndices = visibleAgentIndices
				});
			}
		}
		try
		{
			_mainThreadActions.Enqueue(delegate
			{
				PersistPlayerMessageToNamedHeroes(text, nearbyData);
			});
		}
		catch
		{
		}
	}

	private void RecordResponseForAllNearbySafe(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response)
	{
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text = ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		lock (_historyLock)
		{
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				if (!_npcConversationHistory.ContainsKey(agentIndex))
				{
					_npcConversationHistory[agentIndex] = new List<ConversationMessage>();
				}
				_npcConversationHistory[agentIndex].Add(new ConversationMessage
				{
					Role = "assistant",
					Content = "[" + text + "]: " + response,
					SpeakerName = text,
					SpeakerAgentIndex = speakerAgentIndex,
					VisibleAgentIndices = visibleAgentIndices
				});
				if (_npcConversationHistory[agentIndex].Count > 40)
				{
					_npcConversationHistory[agentIndex].RemoveRange(0, 2);
				}
			}
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "assistant",
				Content = response,
				SpeakerName = text,
				SpeakerAgentIndex = speakerAgentIndex,
				VisibleAgentIndices = visibleAgentIndices
			});
			if (_publicConversationHistory.Count > 40)
			{
				_publicConversationHistory.RemoveAt(0);
			}
		}
	}

	private void RecordSystemFactForNearbySafe(List<NpcDataPacket> nearbyData, string factText)
	{
		string text = (factText ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "system",
				Content = text,
				SpeakerName = "system",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			});
			if (_publicConversationHistory.Count > 40)
			{
				_publicConversationHistory.RemoveAt(0);
			}
			if (nearbyData == null)
			{
				return;
			}
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (nearbyDatum == null)
				{
					continue;
				}
				int agentIndex = nearbyDatum.AgentIndex;
				if (!_npcConversationHistory.ContainsKey(agentIndex))
				{
					_npcConversationHistory[agentIndex] = new List<ConversationMessage>();
				}
				_npcConversationHistory[agentIndex].Add(new ConversationMessage
				{
					Role = "system",
					Content = text,
					SpeakerName = "system",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				});
				if (_npcConversationHistory[agentIndex].Count > 40)
				{
					_npcConversationHistory[agentIndex].RemoveRange(0, Math.Min(2, _npcConversationHistory[agentIndex].Count));
				}
			}
		}
	}

	private async Task EnsurePersonaForCandidatesAsync(List<NpcDataPacket> candidates, Dictionary<int, Hero> resolvedHeroes)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcDataPacket npc in candidates)
		{
			if (npc == null)
			{
				continue;
			}
			try
			{
				if (npc.IsHero)
				{
					Hero hero = null;
					if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
					if (hero == null)
					{
						continue;
					}
					MyBehavior.GetNpcPersonaForExternal(hero, out var p, out var b);
					if (string.IsNullOrWhiteSpace(p) && string.IsNullOrWhiteSpace(b))
					{
						string text = (hero.StringId ?? "").Trim();
						bool flag = string.IsNullOrWhiteSpace(text) || hashSet.Add(text);
						if (flag)
						{
							bool flag2 = !MyBehavior.TryGetNpcPersonaGenerationStatusForExternal(hero, out var needsGeneration, out _) || needsGeneration;
							if (flag2)
							{
								InformationManager.DisplayMessage(new InformationMessage(MyBehavior.BuildNpcPersonaGenerationHintForExternal(hero), new Color(1f, 0.85f, 0.3f)));
							}
						}
						try
						{
							await MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync(hero);
						}
						catch
						{
						}
						MyBehavior.GetNpcPersonaForExternal(hero, out p, out b);
					}
					if (string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(b))
					{
						BuildHeroPersonaFallback(hero, out var fp, out var fb);
						if (string.IsNullOrWhiteSpace(p))
						{
							p = fp;
						}
						if (string.IsNullOrWhiteSpace(b))
						{
							b = fb;
						}
						fp = null;
						fb = null;
					}
					if (!string.IsNullOrWhiteSpace(p))
					{
						npc.PersonalityDesc = p.Trim();
					}
					if (!string.IsNullOrWhiteSpace(b))
					{
						npc.BackgroundDesc = b.Trim();
					}
					p = null;
					b = null;
					continue;
				}
				string key = (npc.UnnamedKey ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(key))
				{
					if (ShoutUtils.TryGetUnnamedPersonaByKey(key, out var up, out var ub))
					{
						if (!string.IsNullOrWhiteSpace(up))
						{
							npc.PersonalityDesc = up.Trim();
						}
						if (!string.IsNullOrWhiteSpace(ub))
						{
							npc.BackgroundDesc = ub.Trim();
						}
					}
				}
			}
			catch
			{
			}
		}
	}

	private static void BuildHeroPersonaFallback(Hero hero, out string personality, out string background)
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
			if (hero.IsFactionLeader)
			{
				arg = "统治者";
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

	private static Agent ResolveAgentForLocationCharacter(LocationCharacter locationCharacter)
	{
		if (locationCharacter == null || Mission.Current?.Agents == null)
		{
			return null;
		}
		IAgentOriginBase agentOrigin = locationCharacter.AgentOrigin;
		if (agentOrigin != null)
		{
			Agent agent = Mission.Current.Agents.FirstOrDefault((Agent a) => a != null && a.Origin == agentOrigin);
			if (agent != null)
			{
				return agent;
			}
		}
		CharacterObject character = locationCharacter.Character;
		if (character == null)
		{
			return null;
		}
		return Mission.Current.Agents.FirstOrDefault((Agent a) => a != null && a.Character == character);
	}

	private static Passage FindCurrentScenePassageToLocation(Location targetLocation)
	{
		if (targetLocation == null)
		{
			return null;
		}
		MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
		IEnumerable<UsableMachine> enumerable = Enumerable.Empty<UsableMachine>();
		if (missionBehavior?.TownPassageProps != null)
		{
			enumerable = enumerable.Concat(missionBehavior.TownPassageProps);
		}
		if (missionBehavior?.DisabledPassages != null)
		{
			enumerable = enumerable.Concat(missionBehavior.DisabledPassages);
		}
		return enumerable.OfType<Passage>().FirstOrDefault((Passage x) => x != null && x.ToLocation == targetLocation);
	}

	private Agent ResolveSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		if (request == null || request.DoorProxyAgentIndex < 0 || Mission.Current?.Agents == null)
		{
			return null;
		}
		Agent agent = Mission.Current.Agents.FirstOrDefault((Agent a) => a != null && a.Index == request.DoorProxyAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
		return agent;
	}

	private Agent EnsureSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request, Passage passage, Agent speakerAgent)
	{
		Agent agent = ResolveSceneSummonDoorProxyAgent(request);
		if (agent != null)
		{
			return agent;
		}
		if (request == null || passage == null || Mission.Current?.Scene == null)
		{
			return null;
		}
		CharacterObject characterObject = speakerAgent?.Character as CharacterObject;
		if (characterObject == null)
		{
			characterObject = request.SpeakerLocationCharacter?.Character as CharacterObject;
		}
		if (characterObject == null)
		{
			return null;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		passageWaitingPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(passageWaitingPosition, BodyFlags.CommonCollisionExcludeFlags);
		Vec3 vec3 = passageWaitingPosition;
		vec3.z -= 25f;
		Vec2 vec = Vec2.Forward;
		try
		{
			Vec2 vec2 = ((Agent.Main != null && Agent.Main.IsActive()) ? (Agent.Main.Position - passageWaitingPosition).AsVec2 : Vec2.Zero);
			if (vec2.LengthSquared > 0.001f)
			{
				vec = vec2.Normalized();
			}
			else if (speakerAgent != null)
			{
				vec = speakerAgent.LookDirection.AsVec2;
			}
		}
		catch
		{
		}
		Equipment equipment = null;
		try
		{
			equipment = speakerAgent?.SpawnEquipment?.Clone(false);
		}
		catch
		{
		}
		if (equipment == null)
		{
			try
			{
				equipment = (Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment)?.Clone(false);
			}
			catch
			{
			}
		}
		Team team = null;
		if (IsUsableTeam(speakerAgent?.Team))
		{
			team = speakerAgent.Team;
		}
		else if (IsUsableTeam(Mission.Current.PlayerTeam))
		{
			team = Mission.Current.PlayerTeam;
		}
		else if (IsUsableTeam(Mission.Current.DefenderTeam))
		{
			team = Mission.Current.DefenderTeam;
		}
		else if (IsUsableTeam(Mission.Current.AttackerTeam))
		{
			team = Mission.Current.AttackerTeam;
		}
		if (!IsUsableTeam(team))
		{
			return null;
		}
		try
		{
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(in passageWaitingPosition)
				.InitialDirection(in vec)
				.NoHorses(true)
				.Controller(AgentControllerType.AI);
			if (equipment != null)
			{
				agentBuildData = agentBuildData.Equipment(equipment);
			}
			Agent agent2 = Mission.Current.SpawnAgent(agentBuildData, false);
			if (agent2 == null)
			{
				return null;
			}
			request.DoorProxyAgentIndex = agent2.Index;
			try
			{
				agent2.AgentVisuals?.SetVisible(false);
			}
			catch
			{
			}
			try
			{
				agent2.AgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
			try
			{
				agent2.TeleportToPosition(vec3);
			}
			catch
			{
			}
			try
			{
				agent2.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			try
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			LogSceneSummonState("cross_scene_proxy_spawned", request, speakerAgent, agent2, "waitPos=" + FormatSceneSummonPosition(passageWaitingPosition) + " hiddenPos=" + FormatSceneSummonPosition(vec3), force: true);
			return agent2;
		}
		catch
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
	}

	private void CleanupSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		if (request == null)
		{
			return;
		}
		Agent agent = ResolveSceneSummonDoorProxyAgent(request);
		request.DoorProxyAgentIndex = -1;
		if (agent == null)
		{
			return;
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
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
	}

	private Agent ResolveSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		if (job == null || job.DoorProxyAgentIndex < 0 || Mission.Current?.Agents == null)
		{
			return null;
		}
		Agent agent = Mission.Current.Agents.FirstOrDefault((Agent a) => a != null && a.Index == job.DoorProxyAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			job.DoorProxyAgentIndex = -1;
			return null;
		}
		return agent;
	}

	private Agent EnsureSceneReturnDoorProxyAgent(SceneReturnJob job, Passage passage, Agent movingAgent)
	{
		Agent agent = ResolveSceneReturnDoorProxyAgent(job);
		if (agent != null)
		{
			return agent;
		}
		if (job == null || passage == null || Mission.Current?.Scene == null)
		{
			return null;
		}
		CharacterObject characterObject = movingAgent?.Character as CharacterObject;
		if (characterObject == null)
		{
			characterObject = job.LocationCharacter?.Character as CharacterObject;
		}
		if (characterObject == null)
		{
			return null;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		passageWaitingPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(passageWaitingPosition, BodyFlags.CommonCollisionExcludeFlags);
		Vec3 vec3 = passageWaitingPosition;
		vec3.z -= 25f;
		Vec2 vec = Vec2.Forward;
		try
		{
			Vec2 vec2 = ((Agent.Main != null && Agent.Main.IsActive()) ? (Agent.Main.Position - passageWaitingPosition).AsVec2 : Vec2.Zero);
			if (vec2.LengthSquared > 0.001f)
			{
				vec = vec2.Normalized();
			}
			else if (movingAgent != null)
			{
				vec = movingAgent.LookDirection.AsVec2;
			}
		}
		catch
		{
		}
		Equipment equipment = null;
		try
		{
			equipment = movingAgent?.SpawnEquipment?.Clone(false);
		}
		catch
		{
		}
		if (equipment == null)
		{
			try
			{
				equipment = (Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment)?.Clone(false);
			}
			catch
			{
			}
		}
		Team team = null;
		if (IsUsableTeam(movingAgent?.Team))
		{
			team = movingAgent.Team;
		}
		else if (IsUsableTeam(Mission.Current.PlayerTeam))
		{
			team = Mission.Current.PlayerTeam;
		}
		else if (IsUsableTeam(Mission.Current.DefenderTeam))
		{
			team = Mission.Current.DefenderTeam;
		}
		else if (IsUsableTeam(Mission.Current.AttackerTeam))
		{
			team = Mission.Current.AttackerTeam;
		}
		if (!IsUsableTeam(team))
		{
			return null;
		}
		try
		{
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(in passageWaitingPosition)
				.InitialDirection(in vec)
				.NoHorses(true)
				.Controller(AgentControllerType.AI);
			if (equipment != null)
			{
				agentBuildData = agentBuildData.Equipment(equipment);
			}
			Agent agent2 = Mission.Current.SpawnAgent(agentBuildData, false);
			if (agent2 == null)
			{
				return null;
			}
			job.DoorProxyAgentIndex = agent2.Index;
			try
			{
				agent2.AgentVisuals?.SetVisible(false);
			}
			catch
			{
			}
			try
			{
				agent2.AgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
			try
			{
				agent2.TeleportToPosition(vec3);
			}
			catch
			{
			}
			try
			{
				agent2.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			try
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			return agent2;
		}
		catch
		{
			job.DoorProxyAgentIndex = -1;
			return null;
		}
	}

	private void CleanupSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		if (job == null)
		{
			return;
		}
		Agent agent = ResolveSceneReturnDoorProxyAgent(job);
		job.DoorProxyAgentIndex = -1;
		if (agent == null)
		{
			return;
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
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
	}

	private NpcDataPacket BuildSceneNpcDataFromLocationCharacter(LocationCharacter locationCharacter)
	{
		if (locationCharacter?.Character == null)
		{
			return null;
		}
		Agent agent = ResolveAgentForLocationCharacter(locationCharacter);
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

	private List<NpcDataPacket> BuildSceneSummonArrivalContextSnapshot(LocationCharacter targetLocationCharacter, Agent speakerAgent)
	{
		List<NpcDataPacket> list = (ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>()).Select((Agent a) => ShoutUtils.ExtractNpcData(a)).Where((NpcDataPacket d) => d != null).ToList();
		if (speakerAgent != null && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == speakerAgent.Index))
		{
			NpcDataPacket item = ShoutUtils.ExtractNpcData(speakerAgent);
			if (item != null)
			{
				list.Add(item);
			}
		}
		NpcDataPacket sceneNpcDataFromLocationCharacter = BuildSceneNpcDataFromLocationCharacter(targetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter != null)
		{
			if (sceneNpcDataFromLocationCharacter.AgentIndex >= 0)
			{
				if (!list.Any((NpcDataPacket x) => x != null && x.AgentIndex == sceneNpcDataFromLocationCharacter.AgentIndex))
				{
					list.Add(sceneNpcDataFromLocationCharacter);
				}
			}
			else if (!list.Any((NpcDataPacket x) => x != null && string.Equals((x.Name ?? "").Trim(), (sceneNpcDataFromLocationCharacter.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && x.IsHero == sceneNpcDataFromLocationCharacter.IsHero))
			{
				list.Add(sceneNpcDataFromLocationCharacter);
			}
		}
		ApplySceneLocalDisambiguatedNames(list);
		return list;
	}

	private async Task<string> GenerateCompactSceneReactionLineAsync(NpcDataPacket targetNpc, Hero contextHero, List<NpcDataPacket> allNpcData, string extraFactLine, string singleReplyUserContent)
	{
		if (targetNpc == null || allNpcData == null || allNpcData.Count == 0)
		{
			return "";
		}
		allNpcData = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(allNpcData);
		targetNpc = allNpcData.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex >= 0 && x.AgentIndex == targetNpc.AgentIndex) ?? CloneNpcDataPacket(targetNpc);
		if (targetNpc == null)
		{
			return "";
		}
		await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { targetNpc }, contextHero != null ? new Dictionary<int, Hero> { [targetNpc.AgentIndex] = contextHero } : new Dictionary<int, Hero>());
		Agent npcAgent = (targetNpc.AgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex) : null;
		CharacterObject npcCharacter = (npcAgent?.Character as CharacterObject) ?? contextHero?.CharacterObject;
		string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
		MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(contextHero, "请直接根据刚刚发生的公开互动做出即时反应。", null, targetNpc.CultureId ?? "neutral", hasAnyHero: targetNpc.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: targetNpc.AgentIndex, suppressDynamicRuleAndLore: true);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【在场人物列表】：");
		foreach (NpcDataPacket allNpcDatum in allNpcData)
		{
			if (allNpcDatum != null)
			{
				stringBuilder.AppendLine(BuildSceneNpcListLineForPrompt(allNpcDatum));
			}
		}
		string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(allNpcData);
		if (!string.IsNullOrWhiteSpace(sceneNamingNote))
		{
			stringBuilder.AppendLine(sceneNamingNote);
		}
		string baseExtras = StripScenePersonaBlocks((shoutPromptContext?.Extras ?? "").Trim());
		string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
		string localExtras = InjectTrustBlockBelowTriState(stringBuilder.ToString().Trim(), trustBlock);
		string text = string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust);
		List<string> historyLines = null;
		lock (_historyLock)
		{
			if (_publicConversationHistory.Count > 0)
			{
				historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, targetNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(targetNpc), useNpcNameAddress: false);
			}
		}
		string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
		string persistedHeroHistory = (contextHero != null) ? MyBehavior.BuildHistoryContextForExternal(contextHero, 0, "", GetLatestSceneNpcUtterance(targetNpc.AgentIndex)) : "";
		string privateRecentWindowSection = "";
		string persistedWithoutRecentWindow = "";
		SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
		string layeredPrompt = text;
		if (!string.IsNullOrWhiteSpace(extraFactLine))
		{
			layeredPrompt = layeredPrompt + "\n【AFEF玩家行为补充】\n" + extraFactLine.Trim();
		}
		if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
		{
			layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
		}
		if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
		{
			layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
		}
		string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(contextHero);
		if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
		{
			layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
		}
		if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
		{
			layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
		}
		string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(targetNpc, contextHero, new List<NpcDataPacket> { targetNpc });
		if (!string.IsNullOrWhiteSpace(roleTopIntro))
		{
			layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
		}
		List<object> messages = new List<object>
		{
			new
			{
				role = "system",
				content = layeredPrompt
			},
			new
			{
				role = "user",
				content = singleReplyUserContent
			}
		};
		string text2 = await ShoutNetwork.CallApiWithMessages(messages, 5000);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		string text3 = (text2 ?? "").Replace("\r", "").Trim();
		text3 = Regex.Replace(text3, "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
		text3 = StripNpcNamePrefixSafely(text3, 30);
		text3 = StripLeakedPromptContentForShout(text3);
		text3 = StripStageDirectionsForPassiveShout(text3);
		return text3.Trim();
	}

	private void PrimeSceneSummonArrivalSpeechAsync(ActiveSceneSummonRequest request)
	{
		if (request == null || request.TargetLocationCharacter?.Character == null)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		List<NpcDataPacket> sceneSummonArrivalContextSnapshot = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, agent);
		NpcDataPacket sceneNpcDataFromLocationCharacter = BuildSceneNpcDataFromLocationCharacter(request.TargetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter == null)
		{
			return;
		}
		Hero hero = request.TargetLocationCharacter.Character.HeroObject;
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string extraFactLine = request.SpeakerName + "将你带到了" + text + "面前，" + text + "找你有些事。";
		string singleReplyUserContent = "请只根据【当前场景公共对话与互动】和上面的【AFEF玩家行为补充】，以你的身份主动开口问" + text + "找你有什么事。控制在 18-40 字之间，只输出你说出口的话。";
		_ = Task.Run(async delegate
		{
			try
			{
				string text2 = await GenerateCompactSceneReactionLineAsync(sceneNpcDataFromLocationCharacter, hero, sceneSummonArrivalContextSnapshot, extraFactLine, singleReplyUserContent);
				request.PreGeneratedArrivalSpeech = text2;
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[ERROR] PrimeSceneSummonArrivalSpeechAsync failed: " + ex.Message);
				request.PreGeneratedArrivalSpeech = "";
			}
		});
	}

	private void PlaySceneSummonArrivalSpeechIfReady(ActiveSceneSummonRequest request, Agent speakerAgent)
	{
		if (request == null || request.ArrivalSpeechConsumed)
		{
			return;
		}
		request.ArrivalSpeechConsumed = true;
		string text = (request.PreGeneratedArrivalSpeech ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		NpcDataPacket sceneNpcDataFromLocationCharacter = BuildSceneNpcDataFromLocationCharacter(request.TargetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter == null)
		{
			return;
		}
		List<NpcDataPacket> sceneSummonArrivalContextSnapshot = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, speakerAgent);
		if (!sceneSummonArrivalContextSnapshot.Any((NpcDataPacket x) => x != null && x.AgentIndex >= 0 && x.AgentIndex == sceneNpcDataFromLocationCharacter.AgentIndex))
		{
			sceneSummonArrivalContextSnapshot.Add(sceneNpcDataFromLocationCharacter);
			ApplySceneLocalDisambiguatedNames(sceneSummonArrivalContextSnapshot);
		}
		EnqueueSpeechLine(sceneNpcDataFromLocationCharacter, text, sceneSummonArrivalContextSnapshot, skipHistory: false, suppressStare: false);
	}

	private SceneSummonConversationSession TryGetSceneSummonConversationSessionForAgentIndex(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return null;
		}
		for (int i = 0; i < _activeSceneSummonConversationSessions.Count; i++)
		{
			SceneSummonConversationSession sceneSummonConversationSession = _activeSceneSummonConversationSessions[i];
			if (sceneSummonConversationSession == null)
			{
				continue;
			}
			Agent agent = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
			if (agent != null && agent.Index == agentIndex)
			{
				return sceneSummonConversationSession;
			}
			for (int j = 0; j < sceneSummonConversationSession.Participants.Count; j++)
			{
				SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants[j];
				if (sceneSummonConversationParticipant == null)
				{
					continue;
				}
				Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
				if (agent2 != null && agent2.Index == agentIndex)
				{
					return sceneSummonConversationSession;
				}
			}
		}
		return null;
	}

	private void RegisterSceneSummonConversationSession(ActiveSceneSummonRequest request, Agent speakerAgent, Agent targetAgent)
	{
		if (request == null || request.SpeakerLocationCharacter == null || request.TargetLocationCharacter == null)
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = null;
		if (request.BatchId >= 0)
		{
			sceneSummonConversationSession = _activeSceneSummonConversationSessions.FirstOrDefault((SceneSummonConversationSession x) => x != null && x.BatchId == request.BatchId);
		}
		if (sceneSummonConversationSession == null)
		{
			_activeSceneSummonConversationSessions.RemoveAll(delegate(SceneSummonConversationSession x)
			{
				if (x == null)
				{
					return true;
				}
				if (x.SpeakerLocationCharacter == request.SpeakerLocationCharacter)
				{
					return true;
				}
				return x.Participants.Any((SceneSummonConversationParticipant p) => p != null && p.LocationCharacter == request.TargetLocationCharacter);
			});
			sceneSummonConversationSession = new SceneSummonConversationSession
			{
				BatchId = request.BatchId,
				SpeakerAgentIndex = request.SpeakerAgentIndex,
				SpeakerName = request.SpeakerName,
				SpeakerLocationCharacter = request.SpeakerLocationCharacter,
				OriginalSpeakerLocation = request.TargetSourceLocation == request.CurrentLocation ? request.CurrentLocation : request.OriginalSpeakerLocation ?? request.CurrentLocation,
				OriginalSpeakerPosition = request.OriginalSpeakerPosition ?? ((speakerAgent != null && speakerAgent.IsActive()) ? new Vec3?(speakerAgent.Position) : null),
				KeepSpeakerNearby = request.KeepMessengerWithTarget
			};
			_activeSceneSummonConversationSessions.Add(sceneSummonConversationSession);
		}
		if (!sceneSummonConversationSession.Participants.Any((SceneSummonConversationParticipant x) => x != null && x.LocationCharacter == request.TargetLocationCharacter))
		{
			sceneSummonConversationSession.Participants.Add(new SceneSummonConversationParticipant
			{
				DisplayName = request.TargetName,
				LocationCharacter = request.TargetLocationCharacter,
				OriginalLocation = request.OriginalTargetLocation ?? request.TargetSourceLocation ?? request.CurrentLocation,
				OriginalPosition = request.OriginalTargetPosition ?? ((targetAgent != null && targetAgent.IsActive()) ? new Vec3?(targetAgent.Position) : null)
			});
		}
		RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		LogSceneSummonState("conversation_session_registered", request, speakerAgent, targetAgent, null, force: true);
	}

	private void RefreshSceneSummonConversationInteractions(SceneSummonConversationSession session)
	{
		if (session == null)
		{
			return;
		}
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		NpcDataPacket sceneNpcDataFromLocationCharacter = BuildSceneNpcDataFromLocationCharacter(session.SpeakerLocationCharacter);
		if (sceneNpcDataFromLocationCharacter != null && sceneNpcDataFromLocationCharacter.AgentIndex >= 0)
		{
			list.Add(sceneNpcDataFromLocationCharacter);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			NpcDataPacket sceneNpcDataFromLocationCharacter2 = BuildSceneNpcDataFromLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (sceneNpcDataFromLocationCharacter2 != null && sceneNpcDataFromLocationCharacter2.AgentIndex >= 0 && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == sceneNpcDataFromLocationCharacter2.AgentIndex))
			{
				list.Add(sceneNpcDataFromLocationCharacter2);
			}
		}
		int num = Math.Max(1, list.Count);
		for (int j = 0; j < list.Count; j++)
		{
			TrackPlayerInteraction(list[j], num, ACTIVE_INTERACTION_IDLE_TIMEOUT);
		}
	}

	private void RemoveAgentFromSceneSummonConversationForFollow(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agent.Index);
		if (sceneSummonConversationSession == null)
		{
			return;
		}
		ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
		bool flag = false;
		for (int num = sceneSummonConversationSession.Participants.Count - 1; num >= 0; num--)
		{
			SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants[num];
			if (sceneSummonConversationParticipant != null && sceneSummonConversationParticipant.LocationCharacter != null && ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter) == agent)
			{
				sceneSummonConversationSession.Participants.RemoveAt(num);
				flag = true;
			}
		}
		if (!flag)
		{
			return;
		}
		Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
		if (sceneSummonConversationSession.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(sceneSummonConversationSession);
			if (CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent)
			{
				StopSceneSummonFollowPlayer(agent2);
				_sceneFollowReturnStates.Remove(agent2.Index);
				QueueSceneReturnJob(sceneSummonConversationSession.SpeakerName, sceneSummonConversationSession.SpeakerLocationCharacter, sceneSummonConversationSession.OriginalSpeakerLocation, sceneSummonConversationSession.OriginalSpeakerPosition);
			}
			if (agent2 != null)
			{
				_pendingInteractionTimeoutArms.Remove(agent2.Index);
				_activeInteractionSessions.Remove(agent2.Index);
			}
		}
		else
		{
			RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		}
		_pendingInteractionTimeoutArms.Remove(agent.Index);
		_activeInteractionSessions.Remove(agent.Index);
	}

	private void ClearSceneSummonConversationInteractionTimers(SceneSummonConversationSession session)
	{
		if (session == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		if (agent != null)
		{
			hashSet.Add(agent.Index);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			Agent agent2 = ResolveAgentForLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (agent2 != null)
			{
				hashSet.Add(agent2.Index);
			}
		}
		foreach (int item in hashSet)
		{
			_pendingInteractionTimeoutArms.Remove(item);
			_activeInteractionSessions.Remove(item);
		}
	}

	private void BeginSceneSummonConversationReturn(SceneSummonConversationSession session)
	{
		if (session == null || CampaignMission.Current?.Location == null)
		{
			return;
		}
		string text = string.Join("、", session.Participants.Where((SceneSummonConversationParticipant x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName)).Select((SceneSummonConversationParticipant x) => x.DisplayName).Distinct());
		_activeSceneSummonConversationSessions.Remove(session);
		CancelSceneSummonBatch(session.BatchId);
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		StopSceneSummonFollowPlayer(agent);
		if (agent != null)
		{
			_sceneFollowReturnStates.Remove(agent.Index);
		}
		QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
		for (int i = 0; i < session.Participants.Count; i++)
		{
			SceneSummonConversationParticipant sceneSummonConversationParticipant = session.Participants[i];
			if (sceneSummonConversationParticipant != null)
			{
				Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
				StopSceneSummonFollowPlayer(agent2);
				if (agent2 != null)
				{
					_sceneFollowReturnStates.Remove(agent2.Index);
				}
				QueueSceneReturnJob(sceneSummonConversationParticipant.DisplayName, sceneSummonConversationParticipant.LocationCharacter, sceneSummonConversationParticipant.OriginalLocation, sceneSummonConversationParticipant.OriginalPosition);
			}
		}
		List<int> list = new List<int>();
		if (agent != null)
		{
			list.Add(agent.Index);
		}
		for (int j = 0; j < session.Participants.Count; j++)
		{
			Agent agent2 = ResolveAgentForLocationCharacter(session.Participants[j]?.LocationCharacter);
			if (agent2 != null && !list.Contains(agent2.Index))
			{
				list.Add(agent2.Index);
			}
		}
		if (list.Count > 0)
		{
			ClearPendingSceneConversationAttentionRelease();
			RemoveSceneMovementSuppressionAgents(list);
			ReleaseSceneConversationAttention(list, fullyRestoreAutonomy: true);
			foreach (int item in list)
			{
				_pendingSceneFollowCommands.Remove(item);
				_pendingSceneSummonReturnsAfterSpeech.Remove(item);
				_pendingInteractionTimeoutArms.Remove(item);
				_activeInteractionSessions.Remove(item);
			}
		}
	}

	private bool IsSceneSummonConversationSpeaker(SceneSummonConversationSession session, Agent agent)
	{
		if (session == null || agent == null || !agent.IsActive())
		{
			return false;
		}
		Agent agent2 = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		return agent2 != null && agent2.Index == agent.Index;
	}

	private void BeginSceneSummonSpeakerReturn(SceneSummonConversationSession session)
	{
		if (session == null || CampaignMission.Current?.Location == null)
		{
			return;
		}
		CancelSceneSummonBatch(session.BatchId);
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		if (agent != null)
		{
			_pendingSceneFollowCommands.Remove(agent.Index);
			_pendingSceneSummonReturnsAfterSpeech.Remove(agent.Index);
			_pendingInteractionTimeoutArms.Remove(agent.Index);
			_activeInteractionSessions.Remove(agent.Index);
			StopSceneSummonFollowPlayer(agent);
			_sceneFollowReturnStates.Remove(agent.Index);
			RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
			ReleaseSceneConversationAttention(new List<int> { agent.Index }, fullyRestoreAutonomy: true);
		}
		QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
		session.SpeakerAgentIndex = -1;
		session.SpeakerLocationCharacter = null;
		session.KeepSpeakerNearby = false;
		if (session.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(session);
			return;
		}
		ClearSceneSummonConversationInteractionTimers(session);
		RefreshSceneSummonConversationInteractions(session);
	}

	private void QueueSceneReturnJob(string displayName, LocationCharacter locationCharacter, Location originalLocation, Vec3? originalPosition)
	{
		if (locationCharacter == null)
		{
			return;
		}
		Location currentLocation = CampaignMission.Current?.Location;
		if (currentLocation == null)
		{
			return;
		}
		foreach (SceneReturnJob item in _activeSceneReturnJobs.Where((SceneReturnJob x) => x != null && x.LocationCharacter == locationCharacter).ToList())
		{
			CleanupSceneReturnDoorProxyAgent(item);
		}
		_activeSceneReturnJobs.RemoveAll((SceneReturnJob x) => x == null || x.LocationCharacter == locationCharacter);
		_activeSceneReturnJobs.Add(new SceneReturnJob
		{
			DisplayName = displayName,
			LocationCharacter = locationCharacter,
			CurrentLocation = currentLocation,
			OriginalLocation = originalLocation ?? currentLocation,
			OriginalPosition = originalPosition,
			ExitPassage = null
		});
	}

	private void PrepareAgentForSceneSummonMovement(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == agent.Index);
		_staringAgentAnchors.Remove(agent.Index);
		RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
		ReleaseAgentFromSceneConversationLocks(agent);
		RestoreAgentAutonomy(agent);
	}

	private void TrackSceneSummonScriptedAgent(Agent agent)
	{
		if (agent != null && agent.IsActive() && agent.Index >= 0)
		{
			_sceneSummonScriptedAgentIndices.Add(agent.Index);
		}
	}

	private void ClearSceneSummonScriptedBehavior(Agent agent)
	{
		if (agent == null || agent.Index < 0 || !_sceneSummonScriptedAgentIndices.Contains(agent.Index))
		{
			return;
		}
		_sceneSummonScriptedAgentIndices.Remove(agent.Index);
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			DailyBehaviorGroup behaviorGroup = component?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup?.ScriptedBehavior is ScriptBehavior)
			{
				behaviorGroup.DisableScriptedBehavior();
			}
		}
		catch
		{
		}
	}

	private void ReleaseAgentForSceneSummonAction(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == agent.Index);
		_staringAgentAnchors.Remove(agent.Index);
		RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			_pendingSceneConversationAttentionReleaseAgentIndices.Remove(agent.Index);
		}
		_pendingInteractionTimeoutArms.Remove(agent.Index);
		_activeInteractionSessions.Remove(agent.Index);
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
		if (!ShouldPreserveMeetingSceneAutonomy())
		{
			try
			{
				agent.DisableScriptedMovement();
			}
			catch
			{
			}
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	private bool NavigateAgentToScenePassage(Agent agent, Passage passage)
	{
		if (agent == null || !agent.IsActive() || passage == null)
		{
			return false;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		return NavigateAgentToWorldPosition(agent, passageWaitingPosition, 0.9f, doNotRun: false);
	}

	private Vec3 GetPassageWaitingPosition(Passage passage)
	{
		Vec3 result;
		if (Mission.Current?.Scene == null || passage == null)
		{
			return Vec3.Zero;
		}
		try
		{
			if (passage.PilotStandingPoint != null && passage.PilotStandingPoint.GameEntity != null)
			{
				MatrixFrame globalFrame = passage.PilotStandingPoint.GameEntity.GetGlobalFrame();
				result = globalFrame.origin;
				Vec3 vec = globalFrame.rotation.f;
				if (vec.LengthSquared > 0.0001f)
				{
					vec.Normalize();
					result -= vec * 0.35f;
				}
				result.z = Mission.Current.Scene.GetGroundHeightAtPosition(result, BodyFlags.CommonCollisionExcludeFlags);
				return result;
			}
		}
		catch
		{
		}
		try
		{
			result = passage.GameEntity.GlobalPosition;
			result.z = Mission.Current.Scene.GetGroundHeightAtPosition(result, BodyFlags.CommonCollisionExcludeFlags);
			return result;
		}
		catch
		{
			return Vec3.Zero;
		}
	}

	private bool NavigateAgentToWorldPosition(Agent agent, Vec3 targetPosition, float rangeThreshold = 0.8f, bool doNotRun = false)
	{
		if (agent == null || !agent.IsActive() || Mission.Current?.Scene == null)
		{
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(agent);
			Vec2 vec = (targetPosition - agent.Position).AsVec2;
			float rotationInRadians = agent.LookDirection.AsVec2.RotationInRadians;
			if (vec.LengthSquared > 0.0001f)
			{
				rotationInRadians = vec.RotationInRadians;
			}
			targetPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(targetPosition, BodyFlags.CommonCollisionExcludeFlags);
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			if (agentNavigator == null)
			{
				return false;
			}
			WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, targetPosition);
			WorldFrame targetWorldFrame = new WorldFrame(Mat3.CreateMat3WithForward(Vec3.Forward), worldPosition);
			try
			{
				Vec2 vec2 = new Vec2((float)Math.Cos(rotationInRadians), (float)Math.Sin(rotationInRadians));
				targetWorldFrame = new WorldFrame(Mat3.CreateMat3WithForward(vec2.ToVec3()), worldPosition);
			}
			catch
			{
			}
			ScriptBehavior.AddWorldFrameTarget(agent, targetWorldFrame);
			TrackSceneSummonScriptedAgent(agent);
			try
			{
				WorldPosition origin = targetWorldFrame.Origin;
				agent.SetScriptedPosition(ref origin, addHumanLikeDelay: false, doNotRun ? Agent.AIScriptedFrameFlags.DoNotRun : Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void BuildSceneSummonStandPositions(Agent main, out Vec3 primaryPosition, out Vec3 secondaryPosition)
	{
		Vec3 position = main.Position;
		Vec2 vec = main.LookDirection.AsVec2;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		else
		{
			vec.Normalize();
		}
		Vec2 vec2 = new Vec2(-vec.y, vec.x);
		primaryPosition = position + (vec.ToVec3() * 1.85f) - (vec2.ToVec3() * 0.85f);
		secondaryPosition = position + (vec.ToVec3() * 1.55f) + (vec2.ToVec3() * 0.85f);
	}

	private static Vec3 BuildSceneSummonEscortPosition(Agent main, int slotIndex, int slotCount)
	{
		Vec3 position = main.Position;
		Vec2 vec = main.LookDirection.AsVec2;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		else
		{
			vec.Normalize();
		}
		Vec2 vec2 = new Vec2(-vec.y, vec.x);
		int num = Math.Max(1, slotCount);
		float num2 = (num == 1) ? 0f : ((float)slotIndex - (float)(num - 1) * 0.5f);
		float num3 = 1.7f + 0.25f * Math.Min(slotIndex, 3);
		float num4 = 0.85f * num2;
		return position + (vec.ToVec3() * num3) + (vec2.ToVec3() * num4);
	}

	private static string GetSceneSummonLocationDebugName(Location location)
	{
		string text = (location?.StringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (location?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "null" : text;
	}

	private static string GetSceneSummonPassageDebugName(Passage passage)
	{
		if (passage == null)
		{
			return "null";
		}
		string text = "";
		try
		{
			text = (passage.GameEntity.Name ?? "").Trim();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = passage.GetType().Name;
		}
		return text + "->" + GetSceneSummonLocationDebugName(passage.ToLocation);
	}

	private static string FormatSceneSummonPosition(Vec3? position)
	{
		if (!position.HasValue)
		{
			return "null";
		}
		Vec3 value = position.Value;
		return value.x.ToString("F2") + "," + value.y.ToString("F2") + "," + value.z.ToString("F2");
	}

	private string GetSceneSummonAgentDebugState(Agent agent)
	{
		if (agent == null)
		{
			return "null";
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			UsableMachine targetUsableMachine = component?.AgentNavigator?.TargetUsableMachine;
			string text = (targetUsableMachine?.GetType().Name ?? "none").Trim();
			if (targetUsableMachine != null)
			{
				try
				{
					string text2 = (targetUsableMachine.GameEntity.Name ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						text = text2;
					}
				}
				catch
				{
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "none";
			}
			return "idx=" + agent.Index + " pos=" + FormatSceneSummonPosition(agent.Position) + " using=" + agent.IsUsingGameObject + " curUse=" + (agent.CurrentlyUsedGameObject?.GetType().Name ?? "none") + " nav=" + text;
		}
		catch (Exception ex)
		{
			return "idx=" + agent.Index + " debug_err=" + ex.GetType().Name;
		}
	}

	private void LogSceneSummonState(string phase, ActiveSceneSummonRequest request, Agent speakerAgent = null, Agent targetAgent = null, string extra = null, bool force = false)
	{
		return;
	}

	private static List<int> ParseSceneSummonPromptIds(string rawIds)
	{
		if (string.IsNullOrWhiteSpace(rawIds))
		{
			return null;
		}
		List<int> list = new List<int>();
		HashSet<int> hashSet = new HashSet<int>();
		string[] array = rawIds.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			if (int.TryParse((text ?? "").Trim(), out var result) && result > 0 && hashSet.Add(result))
			{
				list.Add(result);
			}
		}
		return list;
	}

	private bool TryTriggerSceneSummonAction(NpcDataPacket npc, Agent agent, List<SceneSummonPromptTarget> summonTargets, ref string content, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		MatchCollection matchCollection = SceneSummonActionTagRegex.Matches(content);
		if (matchCollection == null || matchCollection.Count == 0)
		{
			return false;
		}
		content = SceneSummonActionTagRegex.Replace(content, "").Trim();
		HashSet<int> hashSet = new HashSet<int>();
		List<int> list = new List<int>();
		foreach (Match item2 in matchCollection)
		{
			if (item2 == null || !item2.Success)
			{
				continue;
			}
			List<int> list3 = ParseSceneSummonPromptIds(item2.Groups[1].Value);
			if (list3 == null || list3.Count == 0)
			{
				continue;
			}
			foreach (int item in list3)
			{
				if (hashSet.Add(item))
				{
					list.Add(item);
				}
			}
		}
		if (list == null || list.Count == 0 || summonTargets == null || summonTargets.Count == 0)
		{
			return false;
		}
		List<SceneSummonPromptTarget> list2 = new List<SceneSummonPromptTarget>();
		HashSet<LocationCharacter> hashSet2 = new HashSet<LocationCharacter>();
		foreach (int item in list)
		{
			SceneSummonPromptTarget sceneSummonPromptTarget = summonTargets.FirstOrDefault((SceneSummonPromptTarget x) => x != null && x.PromptId == item);
			if (sceneSummonPromptTarget != null && sceneSummonPromptTarget.LocationCharacter != null && hashSet2.Add(sceneSummonPromptTarget.LocationCharacter))
			{
				list2.Add(sceneSummonPromptTarget);
			}
		}
		if (list2.Count == 0)
		{
			return false;
		}
		return StartSceneSummonBatchAction(npc, agent, list2, out preparedRequest);
	}

	private bool StartSceneSummonBatchAction(NpcDataPacket npc, Agent agent, List<SceneSummonPromptTarget> targets, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		LocationCharacter locationCharacter = locationComplex?.FindCharacter(agent);
		if (npc == null || agent == null || !agent.IsActive() || locationComplex == null || location == null || locationCharacter == null || targets == null || targets.Count == 0)
		{
			return false;
		}
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(npc.AgentIndex);
		if (sceneSummonConversationSession != null)
		{
			BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
		foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x == null || x.SpeakerAgentIndex == npc.AgentIndex).ToList())
		{
			CleanupSceneSummonDoorProxyAgent(item);
		}
		_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x == null || x.SpeakerAgentIndex == npc.AgentIndex);
		List<int> list = _activeSceneSummonBatches.Values.Where((SceneSummonBatchState x) => x != null && x.SpeakerAgentIndex == npc.AgentIndex).Select((SceneSummonBatchState x) => x.BatchId).ToList();
		foreach (int item in list)
		{
			CancelSceneSummonBatch(item);
		}
		SceneSummonBatchState sceneSummonBatchState = new SceneSummonBatchState
		{
			BatchId = _nextSceneSummonBatchId++,
			SpeakerAgentIndex = npc.AgentIndex,
			SpeakerName = (string.IsNullOrWhiteSpace(npc.Name) ? (agent.Name?.ToString() ?? "有人") : npc.Name),
			SpeakerLocationCharacter = locationCharacter,
			OriginalSpeakerLocation = location,
			OriginalSpeakerPosition = agent.Position
		};
		foreach (SceneSummonPromptTarget target in targets)
		{
			if (target != null)
			{
				sceneSummonBatchState.PendingTargets.Enqueue(target);
			}
		}
		if (sceneSummonBatchState.PendingTargets.Count == 0)
		{
			return false;
		}
		_activeSceneSummonBatches[sceneSummonBatchState.BatchId] = sceneSummonBatchState;
		if (!TryStartNextSceneSummonBatchRequest(sceneSummonBatchState, agent, isInitialRequest: true, out preparedRequest))
		{
			_activeSceneSummonBatches.Remove(sceneSummonBatchState.BatchId);
			return false;
		}
		return true;
	}

	private bool TryTriggerSceneGuideAction(NpcDataPacket npc, Agent agent, List<SceneGuidePromptTarget> guideTargets, List<SceneSummonPromptTarget> summonTargets, ref string content, out ActiveSceneGuideRequest preparedRequest)
	{
		preparedRequest = null;
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		Match match = SceneGuideActionTagRegex.Match(content);
		if (!match.Success)
		{
			return false;
		}
		content = SceneGuideActionTagRegex.Replace(content, "").Trim();
		if (!int.TryParse(match.Groups[1].Value.Trim(), out var result) || guideTargets == null || guideTargets.Count == 0)
		{
			if (string.IsNullOrWhiteSpace(match.Groups[1].Value.Trim()) || !int.TryParse(match.Groups[1].Value.Trim(), out result))
			{
				return false;
			}
		}
		SceneGuidePromptTarget sceneGuidePromptTarget = guideTargets?.FirstOrDefault((SceneGuidePromptTarget x) => x != null && x.PromptId == result);
		if (sceneGuidePromptTarget != null)
		{
			return StartSceneGuideAction(npc, agent, sceneGuidePromptTarget, out preparedRequest);
		}
		SceneSummonPromptTarget sceneSummonPromptTarget = summonTargets?.FirstOrDefault((SceneSummonPromptTarget x) => x != null && x.PromptId == result);
		if (sceneSummonPromptTarget == null)
		{
			return false;
		}
		SceneGuidePromptTarget target = new SceneGuidePromptTarget
		{
			PromptId = sceneSummonPromptTarget.PromptId,
			DisplayName = sceneSummonPromptTarget.DisplayName,
			LocationCode = sceneSummonPromptTarget.LocationCode,
			LocationCharacter = sceneSummonPromptTarget.LocationCharacter,
			SourceLocation = sceneSummonPromptTarget.SourceLocation
		};
		return StartSceneGuideAction(npc, agent, target, out preparedRequest);
	}

	private bool StartSceneGuideAction(NpcDataPacket npc, Agent agent, SceneGuidePromptTarget target, out ActiveSceneGuideRequest preparedRequest)
	{
		preparedRequest = null;
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		LocationCharacter locationCharacter = locationComplex?.FindCharacter(agent);
		if (npc == null || agent == null || !agent.IsActive() || target == null || target.LocationCharacter == null || locationComplex == null || location == null || locationCharacter == null)
		{
			return false;
		}
		Location originalGuideLocation = location;
		Vec3? originalGuidePosition = agent.Position;
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null)
		{
			originalGuideLocation = value.OriginalLocation ?? originalGuideLocation;
			originalGuidePosition = value.OriginalPosition ?? originalGuidePosition;
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		}
		CancelSceneGuideActionForAgent(npc.AgentIndex);
		preparedRequest = new ActiveSceneGuideRequest
		{
			GuideAgentIndex = npc.AgentIndex,
			GuideName = (string.IsNullOrWhiteSpace(npc.Name) ? (agent.Name?.ToString() ?? "有人") : npc.Name),
			TargetName = target.DisplayName,
			TargetPromptId = target.PromptId,
			GuideLocationCharacter = locationCharacter,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = location,
			OriginalGuideLocation = originalGuideLocation,
			OriginalGuidePosition = originalGuidePosition,
			TargetSourceLocation = target.SourceLocation,
			NextStageMissionTime = Mission.Current?.CurrentTime ?? 0f
		};
		_activeSceneGuideRequests.Add(preparedRequest);
		return true;
	}

	private void CancelSceneGuideActionForAgent(int guideAgentIndex)
	{
		if (guideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == guideAgentIndex);
		if (agent != null)
		{
			StopSceneGuideEscort(agent);
		}
		_activeSceneGuideRequests.RemoveAll((ActiveSceneGuideRequest x) => x == null || x.GuideAgentIndex == guideAgentIndex);
		lock (_ttsBubbleSyncLock)
		{
			_pendingSceneGuideLaunchQueues.Remove(guideAgentIndex);
		}
		_pendingSceneGuideReturnsAfterSpeech.Remove(guideAgentIndex);
	}

	private static EscortAgentBehavior GetSceneGuideEscortBehavior(Agent agent)
	{
		try
		{
			AgentNavigator agentNavigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			return behaviorGroup?.GetBehavior<EscortAgentBehavior>();
		}
		catch
		{
			return null;
		}
	}

	private static bool TryStartEscortBehavior(Agent ownerAgent, UsableMachine targetMachine)
	{
		try
		{
			AgentNavigator agentNavigator = ownerAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (behaviorGroup == null || ownerAgent == null || Agent.Main == null || !Agent.Main.IsActive() || targetMachine == null)
			{
				return false;
			}
			EscortAgentBehavior escortAgentBehavior = behaviorGroup.GetBehavior<EscortAgentBehavior>() ?? behaviorGroup.AddBehavior<EscortAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			escortAgentBehavior.Initialize(Agent.Main, targetMachine, null);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryStartSceneGuideEscort(ActiveSceneGuideRequest request, Agent guideAgent)
	{
		if (request == null || !CanAgentParticipateInSceneSpeech(guideAgent) || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(guideAgent);
			Location currentLocation = CampaignMission.Current?.Location;
			if (currentLocation == null || request.TargetSourceLocation == null)
			{
				return false;
			}
			if (request.TargetSourceLocation == currentLocation)
			{
				Agent agent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
				if (!CanAgentParticipateInSceneSpeech(agent))
				{
					return false;
				}
				EscortAgentBehavior.AddEscortAgentBehavior(guideAgent, agent, null);
				return true;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, request.TargetSourceLocation);
			if (sceneLocationPath == null || sceneLocationPath.Count < 2)
			{
				return false;
			}
			Passage currentScenePassageToLocation = request.TargetDoorPassage ?? FindCurrentScenePassageToLocation(sceneLocationPath[1]);
			if (currentScenePassageToLocation == null)
			{
				return false;
			}
			request.TargetDoorPassage = currentScenePassageToLocation;
			return TryStartEscortBehavior(guideAgent, currentScenePassageToLocation);
		}
		catch
		{
			return false;
		}
	}

	private static void StopSceneGuideEscort(Agent agent)
	{
		try
		{
			if (agent != null && agent.IsActive())
			{
				EscortAgentBehavior.RemoveEscortBehaviorOfAgent(agent);
			}
		}
		catch
		{
		}
	}

	private bool TryStartNextSceneSummonBatchRequest(SceneSummonBatchState batch, Agent fallbackSpeakerAgent, bool isInitialRequest, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		if (batch == null || batch.PendingTargets.Count == 0)
		{
			return false;
		}
		Agent agent = ResolveAgentForLocationCharacter(batch.SpeakerLocationCharacter) ?? fallbackSpeakerAgent;
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return false;
		}
		while (batch.PendingTargets.Count > 0)
		{
			SceneSummonPromptTarget target = batch.PendingTargets.Dequeue();
			bool keepMessengerWithTarget = batch.PendingTargets.Count == 0;
			preparedRequest = BuildSceneSummonRequest(batch, agent, target, keepMessengerWithTarget, isInitialRequest);
			if (preparedRequest == null)
			{
				continue;
			}
			LocationCharacter preparedTargetLocationCharacter = preparedRequest.TargetLocationCharacter;
			foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x == null || (x.BatchId == batch.BatchId && x.TargetLocationCharacter == preparedTargetLocationCharacter)).ToList())
			{
				CleanupSceneSummonDoorProxyAgent(item);
			}
			_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x == null || (x.BatchId == batch.BatchId && x.TargetLocationCharacter == preparedTargetLocationCharacter));
			_activeSceneSummonRequests.Add(preparedRequest);
			PrimeSceneSummonArrivalSpeechAsync(preparedRequest);
			return true;
		}
		return false;
	}

	private ActiveSceneSummonRequest BuildSceneSummonRequest(SceneSummonBatchState batch, Agent speakerAgent, SceneSummonPromptTarget target, bool keepMessengerWithTarget, bool isInitialRequest)
	{
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (batch == null || speakerAgent == null || !speakerAgent.IsActive() || target == null || target.LocationCharacter == null || locationComplex == null || location == null)
		{
			return null;
		}
		Location locationOfCharacter = locationComplex.GetLocationOfCharacter(target.LocationCharacter) ?? target.SourceLocation;
		if (locationOfCharacter == null || target.LocationCharacter.Character == null || target.LocationCharacter.Character == CharacterObject.PlayerCharacter || target.LocationCharacter.Character == speakerAgent.Character)
		{
			return null;
		}
		List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
		if (sceneLocationPath == null || sceneLocationPath.Count == 0)
		{
			return null;
		}
		string text = string.IsNullOrWhiteSpace(target.DisplayName) ? (target.LocationCharacter.Character.Name?.ToString() ?? "那个人") : target.DisplayName;
		string text2 = BuildSceneSummonBatchTargetSummary(batch, target);
		LocationCharacter locationCharacter = batch.SpeakerLocationCharacter ?? locationComplex.FindCharacter(speakerAgent);
		if (locationCharacter == null)
		{
			return null;
		}
		if (locationOfCharacter == location)
		{
			Agent agent = ResolveAgentForLocationCharacter(target.LocationCharacter);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return null;
			}
			ActiveSceneSummonRequest activeSceneSummonRequest = new ActiveSceneSummonRequest
			{
				BatchId = batch.BatchId,
				SpeakerAgentIndex = batch.SpeakerAgentIndex,
				SpeakerName = batch.SpeakerName,
				TargetPromptId = target.PromptId,
				TargetName = text,
				SpeakerLocationCharacter = locationCharacter,
				TargetLocationCharacter = target.LocationCharacter,
				CurrentLocation = location,
				OriginalSpeakerLocation = batch.OriginalSpeakerLocation ?? location,
				OriginalTargetLocation = locationOfCharacter,
				TargetSourceLocation = locationOfCharacter,
				PassageHopLocation = location,
				OriginalSpeakerPosition = batch.OriginalSpeakerPosition ?? speakerAgent.Position,
				OriginalTargetPosition = agent.Position,
				NextStageMissionTime = Mission.Current.CurrentTime,
				ArrivalSpeechDeadlineMissionTime = Mission.Current.CurrentTime + 6f,
				Stage = SceneSummonStage.PendingLaunch,
				PendingLaunchStage = SceneSummonStage.MessengerToTarget,
				LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去叫" + text2 + "了。") : null),
				KeepMessengerWithTarget = keepMessengerWithTarget
			};
			LogSceneSummonState(isInitialRequest ? "start_same_scene" : "start_same_scene_followup", activeSceneSummonRequest, speakerAgent, agent, "pathLen=1 keepMessenger=" + keepMessengerWithTarget, force: true);
			if (!isInitialRequest)
			{
				activeSceneSummonRequest.NextStageMissionTime = Mission.Current.CurrentTime + 0.25f;
			}
			return activeSceneSummonRequest;
		}
		Location location2 = sceneLocationPath[1];
		Passage currentScenePassageToLocation = FindCurrentScenePassageToLocation(location2);
		if (currentScenePassageToLocation == null)
		{
			return null;
		}
		ActiveSceneSummonRequest activeSceneSummonRequest2 = new ActiveSceneSummonRequest
		{
			BatchId = batch.BatchId,
			SpeakerAgentIndex = batch.SpeakerAgentIndex,
			SpeakerName = batch.SpeakerName,
			TargetPromptId = target.PromptId,
			TargetName = text,
			SpeakerLocationCharacter = locationCharacter,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = location,
			OriginalSpeakerLocation = batch.OriginalSpeakerLocation ?? location,
			OriginalTargetLocation = locationOfCharacter,
			TargetSourceLocation = locationOfCharacter,
			PassageHopLocation = location2,
			OriginalSpeakerPosition = batch.OriginalSpeakerPosition ?? speakerAgent.Position,
			OriginalTargetPosition = null,
			MessengerDoorPassage = currentScenePassageToLocation,
			NextStageMissionTime = Mission.Current.CurrentTime + (isInitialRequest ? SCENE_SUMMON_DELAY_SECONDS : 0.25f),
			ArrivalSpeechDeadlineMissionTime = Mission.Current.CurrentTime + 8f,
			Stage = SceneSummonStage.PendingLaunch,
			PendingLaunchStage = SceneSummonStage.MessengerToDoor,
			LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去帮你叫" + text2 + "了。") : null),
			KeepMessengerWithTarget = keepMessengerWithTarget
		};
		LogSceneSummonState(isInitialRequest ? "start_cross_scene" : "start_cross_scene_followup", activeSceneSummonRequest2, speakerAgent, null, "pathLen=" + sceneLocationPath.Count + " keepMessenger=" + keepMessengerWithTarget, force: true);
		return activeSceneSummonRequest2;
	}

	private void UpdateActiveSceneSummonRequests()
	{
		if (_activeSceneSummonRequests.Count == 0 || Mission.Current == null)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		for (int num = _activeSceneSummonRequests.Count - 1; num >= 0; num--)
		{
			ActiveSceneSummonRequest activeSceneSummonRequest = _activeSceneSummonRequests[num];
			if (!IsSceneSummonRequestStillValid(activeSceneSummonRequest))
			{
				LogSceneSummonState("request_invalidated", activeSceneSummonRequest, ResolveAgentForLocationCharacter(activeSceneSummonRequest.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(activeSceneSummonRequest.TargetLocationCharacter), "campaignLocationChangedOrTargetMissing", force: true);
				CleanupSceneSummonDoorProxyAgent(activeSceneSummonRequest);
				if (activeSceneSummonRequest.BatchId >= 0)
				{
					CancelSceneSummonBatch(activeSceneSummonRequest.BatchId);
				}
				else
				{
					_activeSceneSummonRequests.RemoveAt(num);
				}
				continue;
			}
			bool flag = activeSceneSummonRequest.Stage switch
			{
				SceneSummonStage.PendingLaunch => TickSceneSummonPendingLaunchStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.MessengerToTarget => TickSceneSummonMessengerToTargetStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.MessengerToDoor => TickSceneSummonMessengerStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.WaitingForTarget => TickSceneSummonWaitStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.TargetToPlayer => TickSceneSummonTargetStage(activeSceneSummonRequest, currentTime),
				_ => true,
			};
			if (flag)
			{
				CleanupSceneSummonDoorProxyAgent(activeSceneSummonRequest);
				_activeSceneSummonRequests.RemoveAt(num);
			}
		}
	}

	private static bool IsSceneSummonRequestStillValid(ActiveSceneSummonRequest request)
	{
		return request != null && request.TargetLocationCharacter != null && CampaignMission.Current?.Location == request.CurrentLocation;
	}

	private void SchedulePreparedSceneSummonLaunch(ActiveSceneSummonRequest request, SceneSpeechPlaybackInfo playbackInfo, string spokenText)
	{
		if (request == null || Mission.Current == null)
		{
			return;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished && request.SpeakerAgentIndex >= 0)
		{
			request.NextStageMissionTime = float.MaxValue;
			EnqueuePendingSceneSummonLaunch(request.SpeakerAgentIndex, request);
			LogSceneSummonState("pending_launch_wait_tts_finish", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "speakerAgentIndex=" + request.SpeakerAgentIndex, force: true);
			return;
		}
		float num = EstimateBubbleTypingDurationSeconds(spokenText ?? "");
		if (playbackInfo != null && playbackInfo.VisualDurationSeconds > num)
		{
			num = playbackInfo.VisualDurationSeconds;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted)
		{
			num = Math.Max(num, EstimateBubbleTypingDurationSeconds(spokenText ?? ""));
		}
		float num2 = Math.Max(0.15f, num);
		request.NextStageMissionTime = Mission.Current.CurrentTime + num2;
		LogSceneSummonState("pending_launch_scheduled", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "delay=" + num2.ToString("F2") + " ttsAccepted=" + ((playbackInfo != null) ? playbackInfo.TtsAccepted.ToString() : "False") + " waitForFinish=" + ((playbackInfo != null) ? playbackInfo.WaitForPlaybackFinished.ToString() : "False") + " speechLen=" + ((spokenText ?? "").Length), force: true);
	}

	private void SchedulePreparedSceneGuideLaunch(ActiveSceneGuideRequest request, SceneSpeechPlaybackInfo playbackInfo, string spokenText)
	{
		if (request == null || Mission.Current == null)
		{
			return;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished && request.GuideAgentIndex >= 0)
		{
			request.NextStageMissionTime = float.MaxValue;
			EnqueuePendingSceneGuideLaunch(request.GuideAgentIndex, request);
			return;
		}
		float num = EstimateBubbleTypingDurationSeconds(spokenText ?? "");
		if (playbackInfo != null && playbackInfo.VisualDurationSeconds > num)
		{
			num = playbackInfo.VisualDurationSeconds;
		}
		request.NextStageMissionTime = Mission.Current.CurrentTime + Math.Max(0.15f, num);
	}

	private bool TickSceneGuideRequest(ActiveSceneGuideRequest request, float currentTime)
	{
		if (request == null || currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(agent) || main == null || !main.IsActive())
		{
			return true;
		}
		if (agent.Position.DistanceSquared(main.Position) > SCENE_GUIDE_PLAYER_REQUIRED_DISTANCE_SQ)
		{
			if (request.PlayerOutOfRangeSinceMissionTime < 0f)
			{
				request.PlayerOutOfRangeSinceMissionTime = currentTime;
			}
			else if (currentTime - request.PlayerOutOfRangeSinceMissionTime >= SCENE_GUIDE_PLAYER_LOST_TIMEOUT)
			{
				TriggerSceneGuideTimeoutAndReturn(request);
				return true;
			}
			return false;
		}
		request.PlayerOutOfRangeSinceMissionTime = -1f;
		EscortAgentBehavior sceneGuideEscortBehavior = GetSceneGuideEscortBehavior(agent);
		if (!request.EscortStarted || sceneGuideEscortBehavior == null)
		{
			if (!TryStartSceneGuideEscort(request, agent))
			{
				return true;
			}
			request.EscortStarted = true;
			sceneGuideEscortBehavior = GetSceneGuideEscortBehavior(agent);
		}
		Location currentLocation = CampaignMission.Current?.Location;
		if (currentLocation == null || request.TargetSourceLocation == null)
		{
			return true;
		}
		bool flag = request.TargetSourceLocation != currentLocation;
		if (sceneGuideEscortBehavior != null && sceneGuideEscortBehavior.IsEscortFinished())
		{
			TriggerSceneGuideArrivalAndReturn(request, reachedDoor: flag);
			return true;
		}
		return false;
	}

	private void PauseSceneGuideMovement(Agent agent)
	{
		if (agent == null || !agent.IsActive())
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
			ClearSceneSummonScriptedBehavior(agent);
		}
		catch
		{
		}
	}

	private void TriggerSceneGuideArrivalAndReturn(ActiveSceneGuideRequest request, bool reachedDoor)
	{
		if (request == null || request.GuideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		StopSceneGuideEscort(agent);
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		string factText = reachedDoor ? ("[AFEF NPC行为补充] 你已把" + text + "带到了前往" + request.TargetName + "所在之处的门口。") : ("[AFEF NPC行为补充] 你已把" + text + "带到了" + request.TargetName + "身边。");
		ScheduleSceneGuideReturnAfterNextSpeech(request);
		TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 0.5f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false);
	}

	private void TriggerSceneGuideTimeoutAndReturn(ActiveSceneGuideRequest request)
	{
		if (request == null || request.GuideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		StopSceneGuideEscort(agent);
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		string factText = "[AFEF NPC行为补充] " + text + "长时间没有跟上你，你决定不再继续带路，先回去忙自己的事。";
		ScheduleSceneGuideReturnAfterNextSpeech(request);
		TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 0.5f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false);
	}

	private void ScheduleSceneGuideReturnAfterNextSpeech(ActiveSceneGuideRequest request)
	{
		if (request == null || request.GuideAgentIndex < 0 || request.GuideLocationCharacter == null)
		{
			return;
		}
		_pendingSceneGuideReturnsAfterSpeech[request.GuideAgentIndex] = new PendingSceneGuideReturnAfterSpeech
		{
			AgentIndex = request.GuideAgentIndex,
			DisplayName = request.GuideName,
			LocationCharacter = request.GuideLocationCharacter,
			OriginalLocation = request.OriginalGuideLocation,
			OriginalPosition = request.OriginalGuidePosition
		};
	}

	private bool TickSceneSummonPendingLaunchStage(ActiveSceneSummonRequest request, float currentTime)
	{
		if (request == null)
		{
			return true;
		}
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		request.Stage = request.PendingLaunchStage;
		if (!string.IsNullOrWhiteSpace(request.LaunchAnnouncement))
		{
			InformationManager.DisplayMessage(new InformationMessage(request.LaunchAnnouncement, new Color(1f, 0.85f, 0.35f)));
		}
		LogSceneSummonState("pending_launch_begin", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "launchStage=" + request.PendingLaunchStage, force: true);
		return false;
	}

	private bool TickSceneSummonMessengerToTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		Agent agent2 = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		if (!CanAgentParticipateInSceneSpeech(agent) || !CanAgentParticipateInSceneSpeech(agent2))
		{
			LogSceneSummonState("same_scene_abort", request, agent, agent2, "speakerOrTargetInactive", force: true);
			return true;
		}
		if (agent.Position.DistanceSquared(agent2.Position) <= SCENE_SUMMON_MESSENGER_TARGET_DISTANCE_SQ)
		{
			try
			{
				agent.SetLookAgent(agent2);
			}
			catch
			{
			}
			request.Stage = SceneSummonStage.TargetToPlayer;
			request.NextStageMissionTime = currentTime + 0.25f;
			TryAdvanceSceneSummonBatch(request, agent);
			LogSceneSummonState("same_scene_reached_target", request, agent, agent2, null, force: true);
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(agent);
			ScriptBehavior.AddAgentTarget(agent, agent2);
			TrackSceneSummonScriptedAgent(agent);
		}
		catch
		{
		}
		LogSceneSummonState("same_scene_moving_to_target", request, agent, agent2, "targetDistanceSq=" + agent.Position.DistanceSquared(agent2.Position).ToString("F2"));
		return false;
	}

	private bool TickSceneSummonMessengerStage(ActiveSceneSummonRequest request, float currentTime)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + SCENE_SUMMON_DELAY_SECONDS;
			LogSceneSummonState("cross_scene_speaker_missing", request, agent, null, "fallback_to_wait", force: true);
			return false;
		}
		Passage messengerDoorPassage = request.MessengerDoorPassage ?? FindCurrentScenePassageToLocation(request.PassageHopLocation);
		if (messengerDoorPassage == null)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			LogSceneSummonState("cross_scene_no_passage", request, agent, null, null, force: true);
			return true;
		}
		request.MessengerDoorPassage = messengerDoorPassage;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(messengerDoorPassage);
		Agent agent2 = EnsureSceneSummonDoorProxyAgent(request, messengerDoorPassage, agent);
		float num = passageWaitingPosition.AsVec2.DistanceSquared(agent.Position.AsVec2);
		if (num <= SCENE_SUMMON_RELAY_DISTANCE_SQ)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + SCENE_SUMMON_DELAY_SECONDS;
			TryAdvanceSceneSummonBatch(request, agent);
			LogSceneSummonState("cross_scene_reached_door", request, agent, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			return false;
		}
		if (agent2 != null && agent2.IsActive())
		{
			try
			{
				PrepareAgentForSceneSummonMovement(agent);
				ScriptBehavior.AddAgentTarget(agent, agent2);
				TrackSceneSummonScriptedAgent(agent);
			}
			catch
			{
			}
			LogSceneSummonState("cross_scene_reissue_proxy_target", request, agent, agent2, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
		}
		else
		{
			LogSceneSummonState("cross_scene_reissue_passage_target", request, agent, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			NavigateAgentToScenePassage(agent, messengerDoorPassage);
		}
		return false;
	}

	private bool TickSceneSummonWaitStage(ActiveSceneSummonRequest request, float currentTime)
	{
		CleanupSceneSummonDoorProxyAgent(request);
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		LocationComplex locationComplex = LocationComplex.Current;
		if (locationComplex == null || request.PassageHopLocation == null)
		{
			LogSceneSummonState("wait_stage_abort", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "locationComplexOrHopMissing", force: true);
			return true;
		}
		BringLocationCharacterIntoCurrentScene(request.TargetLocationCharacter, request.PassageHopLocation, request.CurrentLocation);
		request.Stage = SceneSummonStage.TargetToPlayer;
		request.NextStageMissionTime = currentTime + 0.35f;
		LogSceneSummonState("wait_stage_spawn_back", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), null, force: true);
		return false;
	}

	private void BringLocationCharacterIntoCurrentScene(LocationCharacter locationCharacter, Location visibleFromLocation, Location currentLocation)
	{
		LocationComplex locationComplex = LocationComplex.Current;
		MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
		if (locationCharacter == null || locationComplex == null || currentLocation == null || visibleFromLocation == null || missionBehavior == null)
		{
			return;
		}
		Agent agent = ResolveAgentForLocationCharacter(locationCharacter);
		Location locationOfCharacter = locationComplex.GetLocationOfCharacter(locationCharacter);
		if (locationOfCharacter != visibleFromLocation)
		{
			locationComplex.ChangeLocation(locationCharacter, locationOfCharacter, visibleFromLocation);
			locationOfCharacter = visibleFromLocation;
		}
		if (locationOfCharacter != currentLocation)
		{
			locationComplex.ChangeLocation(locationCharacter, locationOfCharacter, currentLocation);
		}
		if (agent == null || !agent.IsActive())
		{
			try
			{
				missionBehavior.SpawnEnteringLocationCharacter(locationCharacter, visibleFromLocation);
			}
			catch
			{
			}
		}
	}

	private bool TryConsumeSceneEndChatActionTag(NpcDataPacket npc, Agent agent, ref string content, out SceneSummonConversationSession session)
	{
		session = null;
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		bool flag = SceneEndChatActionTagRegex.IsMatch(content);
		bool flag2 = SceneFollowStopTagRegex.IsMatch(content);
		if (!flag && !flag2)
		{
			return false;
		}
		if (flag)
		{
			content = SceneEndChatActionTagRegex.Replace(content, "").Trim();
		}
		if (flag2)
		{
			content = SceneFollowStopTagRegex.Replace(content, "").Trim();
		}
		session = TryGetSceneSummonConversationSessionForAgentIndex((agent != null) ? agent.Index : npc.AgentIndex);
		return session != null || IsAgentFollowingPlayerBySceneCommand(agent);
	}

	private bool ShouldReturnOnlySceneSummonSpeaker(SceneSummonConversationSession session, Agent agent)
	{
		return session != null && agent != null && IsSceneSummonConversationSpeaker(session, agent) && session.Participants.Count > 0;
	}

	private bool TryConsumeSceneFollowStartTag(NpcDataPacket npc, Agent agent, ref string content)
	{
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		if (!SceneFollowStartTagRegex.IsMatch(content))
		{
			return false;
		}
		content = SceneFollowStartTagRegex.Replace(content, "").Trim();
		return CanAgentParticipateInSceneSpeech(agent) && agent != Agent.Main && !IsAgentFollowingPlayerBySceneCommand(agent);
	}

	private bool TryConsumeSceneFollowStopTag(NpcDataPacket npc, Agent agent, ref string content)
	{
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		bool flag = SceneFollowStopTagRegex.IsMatch(content);
		bool flag2 = SceneEndChatActionTagRegex.IsMatch(content);
		if (!flag && !flag2)
		{
			return false;
		}
		if (flag)
		{
			content = SceneFollowStopTagRegex.Replace(content, "").Trim();
		}
		if (flag2)
		{
			content = SceneEndChatActionTagRegex.Replace(content, "").Trim();
		}
		return CanAgentParticipateInSceneSpeech(agent) && agent != Agent.Main && IsAgentFollowingPlayerBySceneCommand(agent);
	}

	private void RefreshSceneSummonConversationForSpeaker(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agentIndex);
		if (sceneSummonConversationSession != null)
		{
			RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		}
	}

	private void UpdateActiveSceneGuideRequests()
	{
		if (_activeSceneGuideRequests.Count == 0 || Mission.Current == null)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		for (int num = _activeSceneGuideRequests.Count - 1; num >= 0; num--)
		{
			ActiveSceneGuideRequest activeSceneGuideRequest = _activeSceneGuideRequests[num];
			if (activeSceneGuideRequest == null || CampaignMission.Current?.Location != activeSceneGuideRequest.CurrentLocation)
			{
				StopSceneGuideEscort(Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == activeSceneGuideRequest?.GuideAgentIndex));
				_activeSceneGuideRequests.RemoveAt(num);
				continue;
			}
			if (TickSceneGuideRequest(activeSceneGuideRequest, currentTime))
			{
				StopSceneGuideEscort(Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == activeSceneGuideRequest?.GuideAgentIndex));
				_activeSceneGuideRequests.RemoveAt(num);
			}
		}
	}

	private bool IsAgentBusyWithSceneSummonErrand(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return false;
		}
		if (_activeSceneSummonRequests.Any((ActiveSceneSummonRequest x) => x != null && x.SpeakerAgentIndex == agentIndex))
		{
			return true;
		}
		if (_activeSceneSummonBatches.Values.Any((SceneSummonBatchState x) => x != null && x.SpeakerAgentIndex == agentIndex && x.PendingTargets.Count > 0))
		{
			return true;
		}
		lock (_ttsBubbleSyncLock)
		{
			return _pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
		}
	}

	private bool IsAgentBusyWithSceneGuideErrand(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return false;
		}
		if (_activeSceneGuideRequests.Any((ActiveSceneGuideRequest x) => x != null && x.GuideAgentIndex == agentIndex))
		{
			return true;
		}
		lock (_ttsBubbleSyncLock)
		{
			return _pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
		}
	}

	private void UpdateSceneSummonConversationEscortMovement()
	{
		Agent main = Agent.Main;
		if (main == null || !main.IsActive() || _activeSceneSummonConversationSessions.Count == 0)
		{
			return;
		}
		foreach (SceneSummonConversationSession item in _activeSceneSummonConversationSessions)
		{
			if (item == null)
			{
				continue;
			}
			List<Agent> list = new List<Agent>();
			for (int i = 0; i < item.Participants.Count; i++)
			{
				Agent agent = ResolveAgentForLocationCharacter(item.Participants[i]?.LocationCharacter);
				if (CanAgentParticipateInSceneSpeech(agent) && agent != main)
				{
					list.Add(agent);
				}
			}
			if (item.KeepSpeakerNearby)
			{
				Agent agent2 = ResolveAgentForLocationCharacter(item.SpeakerLocationCharacter);
				if (CanAgentParticipateInSceneSpeech(agent2) && agent2 != main && !list.Contains(agent2))
				{
					list.Add(agent2);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].Position.DistanceSquared(main.Position) <= SCENE_SUMMON_ESCORT_REPOSITION_DISTANCE_SQ)
				{
					continue;
				}
				Vec3 targetPosition = BuildSceneSummonEscortPosition(main, j, list.Count);
				TryGuideSummonParticipantToPosition(list[j], targetPosition);
			}
		}
	}

	private static void AppendSceneGuidePromptSection(StringBuilder prompt, List<SceneGuidePromptTarget> targets)
	{
		if (prompt == null || targets == null || targets.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【可带路目标】：");
		foreach (SceneGuidePromptTarget target in targets)
		{
			if (target != null && target.PromptId > 0)
			{
				prompt.AppendLine(target.PromptId + " " + target.DisplayName.Trim() + " " + (target.LocationCode ?? "处"));
			}
		}
	}

	private void UpdateActiveSceneReturnJobs()
	{
		if (_activeSceneReturnJobs.Count == 0 || CampaignMission.Current?.Location == null)
		{
			return;
		}
		for (int num = _activeSceneReturnJobs.Count - 1; num >= 0; num--)
		{
			SceneReturnJob sceneReturnJob = _activeSceneReturnJobs[num];
			if (sceneReturnJob == null || sceneReturnJob.LocationCharacter == null || sceneReturnJob.CurrentLocation != CampaignMission.Current.Location)
			{
				CleanupSceneReturnDoorProxyAgent(sceneReturnJob);
				_activeSceneReturnJobs.RemoveAt(num);
				continue;
			}
			if (TickSceneReturnJob(sceneReturnJob))
			{
				CleanupSceneReturnDoorProxyAgent(sceneReturnJob);
				_activeSceneReturnJobs.RemoveAt(num);
			}
		}
	}

	private bool TickSceneReturnJob(SceneReturnJob job)
	{
		LocationComplex locationComplex = LocationComplex.Current;
		Location currentLocation = CampaignMission.Current?.Location;
		if (job == null || locationComplex == null || currentLocation == null)
		{
			return true;
		}
		Location originalLocation = job.OriginalLocation ?? currentLocation;
		Agent agent = ResolveAgentForLocationCharacter(job.LocationCharacter);
		if (originalLocation == currentLocation)
		{
			if (!job.OriginalPosition.HasValue || !CanAgentParticipateInSceneSpeech(agent))
			{
				if (agent != null)
				{
					RestoreAgentAutonomy(agent);
				}
				return true;
			}
			ApplySceneReturnWalkPacing(agent);
			NavigateAgentToWorldPosition(agent, job.OriginalPosition.Value, 0.6f, doNotRun: true);
			if (agent.Position.DistanceSquared(job.OriginalPosition.Value) > SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ)
			{
				return false;
			}
			RestoreAgentAutonomy(agent);
			return true;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return true;
		}
		List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, originalLocation);
		if (sceneLocationPath == null || sceneLocationPath.Count < 2)
		{
			return true;
		}
		Location location = sceneLocationPath[1];
		Passage passage = job.ExitPassage ?? FindCurrentScenePassageToLocation(location);
		if (passage == null)
		{
			return true;
		}
		job.ExitPassage = passage;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		Agent agent2 = EnsureSceneReturnDoorProxyAgent(job, passage, agent);
		if (passageWaitingPosition.AsVec2.DistanceSquared(agent.Position.AsVec2) > SCENE_SUMMON_MESSENGER_DOOR_DISTANCE_SQ)
		{
			ApplySceneReturnWalkPacing(agent);
			NavigateAgentToWorldPosition(agent, passageWaitingPosition, 0.9f, doNotRun: true);
			return false;
		}
		CleanupSceneReturnDoorProxyAgent(job);
		locationComplex.ChangeLocation(job.LocationCharacter, currentLocation, location);
		if (originalLocation != location)
		{
			locationComplex.ChangeLocation(job.LocationCharacter, location, originalLocation);
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
		return true;
	}

	private void ApplySceneReturnWalkPacing(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.SetMaximumSpeedLimit(1.55f, isMultiplier: false);
		}
		catch
		{
		}
	}

	private bool TryGuideSummonParticipantToPosition(Agent agent, Vec3 targetPosition)
	{
		if (!CanAgentParticipateInSceneSpeech(agent) || Mission.Current?.Scene == null)
		{
			return false;
		}
		targetPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(targetPosition, BodyFlags.CommonCollisionExcludeFlags);
		NavigateAgentToWorldPosition(agent, targetPosition, 0.75f, doNotRun: false);
		return agent.Position.DistanceSquared(targetPosition) <= SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ;
	}

	private void StartSceneSummonFollowPlayer(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent) || Agent.Main == null || !Agent.Main.IsActive() || agent == Agent.Main)
		{
			return;
		}
		try
		{
			ClearSceneSummonScriptedBehavior(agent);
			RememberSceneFollowReturnState(agent, overwriteExisting: false);
			TrySetSceneFollowPersistence(agent, isFollowing: true);
			_activeInteractionSessions.Remove(agent.Index);
			_pendingInteractionTimeoutArms.Remove(agent.Index);
			TryEnableVanillaSceneFollowBehavior(agent, Agent.Main);
		}
		catch
		{
		}
	}

	private void StopSceneSummonFollowPlayer(Agent agent, bool restoreDailyBehaviors = true)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			TrySetSceneFollowPersistence(agent, isFollowing: false);
			TryDisableVanillaSceneFollowBehavior(agent, restoreDailyBehaviors);
		}
		catch
		{
		}
	}

	private void RememberSceneFollowReturnState(Agent agent, bool overwriteExisting = true)
	{
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		if (!overwriteExisting && _sceneFollowReturnStates.ContainsKey(agent.Index))
		{
			return;
		}
		SceneFollowReturnState sceneFollowReturnState = BuildSceneFollowReturnState(agent);
		if (sceneFollowReturnState != null)
		{
			_sceneFollowReturnStates[agent.Index] = sceneFollowReturnState;
		}
	}

	private SceneFollowReturnState BuildSceneFollowReturnState(Agent agent)
	{
		try
		{
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (!CanAgentParticipateInSceneSpeech(agent) || locationComplex == null || currentLocation == null)
			{
				return null;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			if (locationCharacter == null)
			{
				return null;
			}
			SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agent.Index);
			if (sceneSummonConversationSession != null)
			{
				SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants.FirstOrDefault((SceneSummonConversationParticipant p) => p != null && p.LocationCharacter == locationCharacter);
				if (sceneSummonConversationParticipant != null)
				{
					return new SceneFollowReturnState
					{
						DisplayName = sceneSummonConversationParticipant.DisplayName,
						LocationCharacter = locationCharacter,
						OriginalLocation = sceneSummonConversationParticipant.OriginalLocation ?? currentLocation,
						OriginalPosition = sceneSummonConversationParticipant.OriginalPosition
					};
				}
				if (sceneSummonConversationSession.SpeakerLocationCharacter == locationCharacter)
				{
					return new SceneFollowReturnState
					{
						DisplayName = sceneSummonConversationSession.SpeakerName,
						LocationCharacter = locationCharacter,
						OriginalLocation = sceneSummonConversationSession.OriginalSpeakerLocation ?? currentLocation,
						OriginalPosition = sceneSummonConversationSession.OriginalSpeakerPosition
					};
				}
			}
			return new SceneFollowReturnState
			{
				DisplayName = agent.Name?.ToString() ?? "NPC",
				LocationCharacter = locationCharacter,
				OriginalLocation = currentLocation,
				OriginalPosition = agent.Position
			};
		}
		catch
		{
			return null;
		}
	}

	private void ReturnAgentAfterStoppingSceneFollow(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null && value.LocationCharacter != null)
		{
			_sceneFollowReturnStates.Remove(agent.Index);
			QueueSceneReturnJob(value.DisplayName, value.LocationCharacter, value.OriginalLocation, value.OriginalPosition);
			try
			{
				agent.ClearTargetFrame();
				ClearSceneSummonScriptedBehavior(agent);
			}
			catch
			{
			}
			return;
		}
		RestoreAgentAutonomy(agent);
	}

	private static bool TryEnableVanillaSceneFollowBehavior(Agent agent, Agent target)
	{
		try
		{
			if (agent == null || target == null || !agent.IsActive() || !target.IsActive())
			{
				return false;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator;
			DailyBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return false;
			}
			FollowAgentBehavior followAgentBehavior = behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			followAgentBehavior.IsActive = true;
			followAgentBehavior.SetTargetAgent(target);
			ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
			if (behavior != null)
			{
				behavior.IsActive = false;
			}
			WalkingBehavior behavior2 = behaviorGroup.GetBehavior<WalkingBehavior>();
			if (behavior2 != null)
			{
				behavior2.IsActive = false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void TryDisableVanillaSceneFollowBehavior(Agent agent, bool restoreDailyBehaviors = true)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator;
			DailyBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return;
			}
			behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
			if (restoreDailyBehaviors)
			{
				ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
				if (behavior != null)
				{
					behavior.IsActive = true;
				}
				WalkingBehavior behavior2 = behaviorGroup.GetBehavior<WalkingBehavior>() ?? behaviorGroup.AddBehavior<WalkingBehavior>();
				behavior2.IsActive = true;
			}
		}
		catch
		{
		}
	}

	private static bool TrySetSceneFollowPersistence(Agent agent, bool isFollowing)
	{
		try
		{
			LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
			LocationComplex locationComplex = LocationComplex.Current;
			if (agent == null || locationEncounter == null || locationComplex == null)
			{
				return false;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			if (locationCharacter == null)
			{
				return false;
			}
			if (isFollowing)
			{
				AccompanyingCharacter accompanyingCharacter = locationEncounter.GetAccompanyingCharacter(locationCharacter);
				if (accompanyingCharacter == null || !accompanyingCharacter.IsFollowingPlayerAtMissionStart)
				{
					locationEncounter.RemoveAccompanyingCharacter(locationCharacter);
					locationEncounter.AddAccompanyingCharacter(locationCharacter, isFollowing: true);
				}
			}
			else
			{
				locationEncounter.RemoveAccompanyingCharacter(locationCharacter);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool IsAgentFollowingPlayerBySceneCommand(Agent agent)
	{
		try
		{
			if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
			{
				return false;
			}
			LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
			LocationComplex locationComplex = LocationComplex.Current;
			if (locationEncounter == null || locationComplex == null)
			{
				return false;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = ((locationCharacter != null) ? locationEncounter.GetAccompanyingCharacter(locationCharacter) : null);
			return accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}

	private bool TickSceneSummonTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Agent agent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		Agent agent2 = ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter);
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(agent) || main == null || !main.IsActive())
		{
			LogSceneSummonState("target_to_player_wait_abort", request, agent2, agent, "targetOrPlayerUnavailable", force: currentTime >= request.NextStageMissionTime + 6f);
			return currentTime >= request.NextStageMissionTime + 6f;
		}
		BuildSceneSummonStandPositions(main, out var primaryPosition, out var secondaryPosition);
		bool flag = TryGuideSummonParticipantToPosition(agent, primaryPosition);
		bool flag2 = true;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent)
		{
			flag2 = TryGuideSummonParticipantToPosition(agent2, secondaryPosition);
		}
		if (!flag)
		{
			LogSceneSummonState("target_to_player_moving", request, agent2, agent, "targetArrived=" + flag + " messengerArrived=" + flag2 + " targetStand=" + FormatSceneSummonPosition(primaryPosition) + " messengerStand=" + FormatSceneSummonPosition(secondaryPosition));
			return false;
		}
		if (!request.ArrivalSpeechConsumed && string.IsNullOrWhiteSpace(request.PreGeneratedArrivalSpeech) && currentTime < request.ArrivalSpeechDeadlineMissionTime)
		{
			LogSceneSummonState("target_to_player_waiting_speech", request, agent2, agent, "deadline=" + request.ArrivalSpeechDeadlineMissionTime.ToString("F2"));
			return false;
		}
		ForceAgentFacePlayer(agent);
		bool flag3 = request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent && flag2;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent && !flag2)
		{
			RestoreAgentAutonomy(agent2);
		}
		if (flag3)
		{
			ForceAgentFacePlayer(agent2);
			PlaySceneSummonArrivalSpeechIfReady(request, agent2);
			RegisterSceneSummonConversationSession(request, agent2, agent);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_pair", request, agent2, agent, null, force: true);
			string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			InformationManager.DisplayMessage(new InformationMessage(request.SpeakerName + " 带着" + text + "过来了。", new Color(0.75f, 1f, 0.75f)));
		}
		else
		{
			PlaySceneSummonArrivalSpeechIfReady(request, agent2 ?? agent);
			RegisterSceneSummonConversationSession(request, agent2, agent);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_single", request, agent2, agent, null, force: true);
			string text2 = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			InformationManager.DisplayMessage(new InformationMessage(text2 + " 被叫过来了。", new Color(0.75f, 1f, 0.75f)));
		}
		return true;
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

	private static bool IsSceneHistoryVisibleToAgent(ConversationMessage msg, int viewerAgentIndex)
	{
		if (msg == null)
		{
			return false;
		}
		if (viewerAgentIndex < 0)
		{
			return true;
		}
		List<int> visibleAgentIndices = msg.VisibleAgentIndices;
		if (visibleAgentIndices == null || visibleAgentIndices.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < visibleAgentIndices.Count; i++)
		{
			if (visibleAgentIndices[i] == viewerAgentIndex)
			{
				return true;
			}
		}
		return false;
	}

	private static List<string> BuildVisibleSceneHistoryLines(List<ConversationMessage> history, int viewerAgentIndex, string targetNpcName = "", bool useNpcNameAddress = false)
	{
		if (history == null || history.Count == 0)
		{
			return null;
		}
		List<string> list = new List<string>();
		for (int num = history.Count - 1; num >= 0 && list.Count < MAX_HISTORY_TURNS; num--)
		{
			ConversationMessage msg = history[num];
			if (IsSceneHistoryVisibleToAgent(msg, viewerAgentIndex) && TryRenderSceneHistoryLine(msg, null, out var line, viewerAgentIndex, targetNpcName, useNpcNameAddress))
			{
				list.Add(line);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		list.Reverse();
		return list;
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

	private string GetLatestSceneNpcUtterance(int targetAgentIndex)
	{
		if (targetAgentIndex < 0)
		{
			return "";
		}
		lock (_historyLock)
		{
			if (_publicConversationHistory == null || _publicConversationHistory.Count == 0)
			{
				return "";
			}
			bool seenCurrentPlayerTurn = false;
			for (int i = _publicConversationHistory.Count - 1; i >= 0; i--)
			{
				ConversationMessage conversationMessage = _publicConversationHistory[i];
				if (conversationMessage == null)
				{
					continue;
				}
				string text = (conversationMessage.Role ?? "").Trim();
				if (text.Equals("user", StringComparison.OrdinalIgnoreCase))
				{
					if (!seenCurrentPlayerTurn)
					{
						seenCurrentPlayerTurn = true;
						continue;
					}
					break;
				}
				if (!seenCurrentPlayerTurn || !text.Equals("assistant", StringComparison.OrdinalIgnoreCase) || conversationMessage.SpeakerAgentIndex != targetAgentIndex)
				{
					continue;
				}
				string text2 = (conversationMessage.Content ?? "").Replace("\r", "").Trim();
				if (!string.IsNullOrWhiteSpace(text2) && !IsLeakedPromptLineForShout(text2))
				{
					return text2;
				}
			}
		}
		return "";
	}

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

	public static void TriggerImmediateSceneBehaviorReactionForExternal(string factText, int targetAgentIndex, bool persistHeroPrivateHistory = true, bool suppressStare = false, float postSpeechLeaveSeconds = -1f)
	{
		try
		{
			CurrentInstance?.TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds);
		}
		catch
		{
		}
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
			if (persistHeroPrivateHistory && agent.Character is CharacterObject { HeroObject: not null } characterObject)
			{
				MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, text);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] AppendTargetedSceneNpcFact failed: " + ex.Message);
		}
	}

	private void TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false, bool returnSceneSummonOnTimeout = false)
	{
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		try
		{
			List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
			Agent agent = nearbyNPCAgents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive()) ?? Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			if (!suppressStare)
			{
				ResetStaringForActiveInteraction(nearbyNPCAgents, agent);
			}
			List<NpcDataPacket> list = (from a in ShoutUtils.GetNearbyNPCAgents()
				select ShoutUtils.ExtractNpcData(a) into d
				where d != null
				select d).ToList();
			NpcDataPacket npcDataPacket = list.FirstOrDefault(d => d != null && d.AgentIndex == targetAgentIndex) ?? ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				return;
			}
			if (IsAgentHostileToMainAgent(agent))
			{
				_activeInteractionSessions.Remove(targetAgentIndex);
				_pendingInteractionTimeoutArms.Remove(targetAgentIndex);
				RefreshHostileCombatAgentAutonomy(agent);
			}
			else
			{
				TrackPlayerInteraction(npcDataPacket, list?.Count ?? 0, postSpeechLeaveSeconds, returnSceneSummonOnTimeout);
			}
			if (!list.Any(d => d != null && d.AgentIndex == targetAgentIndex))
			{
				list.Insert(0, npcDataPacket);
			}
			if (!skipSceneFactRecord)
			{
				RecordExtraFactToSceneHistory(factText, list);
			}
			if (persistHeroPrivateHistory && agent.Character is CharacterObject characterObject && characterObject.HeroObject != null)
			{
				MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, factText);
			}
			Dictionary<int, Hero> dictionary = new Dictionary<int, Hero>();
			foreach (NpcDataPacket item in list)
			{
				if (item != null && item.IsHero)
				{
					Hero hero = ResolveHeroFromAgentIndex(item.AgentIndex);
					if (hero != null)
					{
						dictionary[item.AgentIndex] = hero;
					}
				}
			}
			_ = Task.Run(async delegate
			{
				try
				{
					await GenerateImmediateSceneBehaviorReactionAsync(npcDataPacket, list, dictionary, suppressStare);
				}
				catch (Exception ex2)
				{
					Logger.Log("ShoutBehavior", "[ERROR] TriggerImmediateSceneBehaviorReaction background failed: " + ex2.Message);
				}
			});
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] TriggerImmediateSceneBehaviorReaction failed: " + ex.Message);
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

	private void StopAgentRhubarbRecordIfPossible(int agentIndex, string reason = "Unknown")
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			LogTtsReport("StopRhubarbRecord.Skip", agentIndex, "reason=" + reason + ";missionNull=" + (Mission.Current == null));
			return;
		}
		try
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			if (agent == null || agent.AgentVisuals == null)
			{
				LogTtsReport("StopRhubarbRecord.Skip", agentIndex, $"reason={reason};agentMissing={(agent == null)};visualsMissing={(agent != null && agent.AgentVisuals == null)}");
				return;
			}
			LogLipSyncNativeProbe("StopRhubarbRecord.Before", agentIndex, "reason=" + reason);
			agent.AgentVisuals.StartRhubarbRecord("", -1);
			LogLipSyncNativeProbe("StopRhubarbRecord.After", agentIndex, "reason=" + reason);
			Logger.Log("LipSync", $"[Rhubarb] StopRhubarbRecord called, agentIndex={agentIndex}, reason={reason}");
			LogTtsReport("StopRhubarbRecord", agentIndex, "reason=" + reason);
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] StopRhubarbRecord failed, agentIndex={agentIndex}, reason={reason}, error={ex.Message}");
			LogTtsReport("StopRhubarbRecord.Failed", agentIndex, $"reason={reason};error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.StopRhubarbRecord", ex, $"agentIndex={agentIndex};reason={reason}");
		}
	}

	private void DetachAgentLipSyncForSafety(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool hadSpeakingEntry = false;
		bool alreadyDetached = false;
		lock (_speakingLock)
		{
			hadSpeakingEntry = _speakingAgentIndices.Remove(agentIndex);
			alreadyDetached = !_agentLipSyncDetachedForSafety.Add(agentIndex);
		}
		StopAgentRhubarbRecordIfPossible(agentIndex, "SafetyDetach:" + reason);
		Logger.Log("LipSync", $"[SAFEGUARD] Detached scene lipsync for agentIndex={agentIndex}, reason={reason}, hadSpeaking={hadSpeakingEntry}, alreadyDetached={alreadyDetached}");
		LogTtsReport("LipSyncSafeguard.Detached", agentIndex, $"reason={reason};hadSpeaking={hadSpeakingEntry};alreadyDetached={alreadyDetached}");
	}

	private void InterruptAgentSpeechForCombat(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool speaking = false;
		SoundEvent soundEvent = null;
		string wavPath = null;
		string xmlPath = null;
		lock (_speakingLock)
		{
			speaking = _speakingAgentIndices.Remove(agentIndex);
			if (_agentSoundEvents.TryGetValue(agentIndex, out soundEvent))
			{
				_agentSoundEvents.Remove(agentIndex);
			}
			if (_agentWavPaths.TryGetValue(agentIndex, out wavPath))
			{
				_agentWavPaths.Remove(agentIndex);
			}
			if (_agentXmlPaths.TryGetValue(agentIndex, out xmlPath))
			{
				_agentXmlPaths.Remove(agentIndex);
			}
			_agentLipSyncDetachedForSafety.Remove(agentIndex);
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
			_pendingSceneDialogueFeedQueues.Remove(agentIndex);
		}
		_pendingInteractionTimeoutArms.Remove(agentIndex);
		_activeInteractionSessions.Remove(agentIndex);
		bool interrupted = false;
		try
		{
			interrupted = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			interrupted = false;
		}
		if (!speaking && soundEvent == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath) && !interrupted)
		{
			if (ShouldSuppressSceneConversationControlForMeeting())
			{
				ClearQueuedSceneSpeech();
				ClearMeetingSceneConversationControlState();
			}
			return;
		}
		LogTtsReport("InterruptAgentSpeechForCombat", agentIndex, $"reason={reason};speaking={speaking};interrupted={interrupted};hadSe={(soundEvent != null)};hadWav={(!string.IsNullOrWhiteSpace(wavPath))};hadXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Interrupt:" + reason);
		QueueDeferredCleanup(soundEvent, wavPath, xmlPath, "Interrupt:" + reason, agentIndex);
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			ClearQueuedSceneSpeech();
			ClearMeetingSceneConversationControlState();
		}
	}

	private void CancelAgentSpeechForRemoval(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool speaking = false;
		SoundEvent soundEvent = null;
		string wavPath = null;
		string xmlPath = null;
		lock (_speakingLock)
		{
			speaking = _speakingAgentIndices.Remove(agentIndex);
			if (_agentSoundEvents.TryGetValue(agentIndex, out soundEvent))
			{
				_agentSoundEvents.Remove(agentIndex);
			}
			if (_agentWavPaths.TryGetValue(agentIndex, out wavPath))
			{
				_agentWavPaths.Remove(agentIndex);
			}
			if (_agentXmlPaths.TryGetValue(agentIndex, out xmlPath))
			{
				_agentXmlPaths.Remove(agentIndex);
			}
			_agentLipSyncDetachedForSafety.Remove(agentIndex);
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
			_pendingSceneDialogueFeedQueues.Remove(agentIndex);
		}
		_pendingInteractionTimeoutArms.Remove(agentIndex);
		_activeInteractionSessions.Remove(agentIndex);
		bool interrupted = false;
		try
		{
			interrupted = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			interrupted = false;
		}
		if (!speaking && soundEvent == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath) && !interrupted)
		{
			LogTtsReport("CancelAgentSpeechForRemoval.Noop", agentIndex, "reason=" + reason);
			return;
		}
		LogTtsReport("CancelAgentSpeechForRemoval", agentIndex, $"reason={reason};speaking={speaking};interrupted={interrupted};hadSe={(soundEvent != null)};hadWav={(!string.IsNullOrWhiteSpace(wavPath))};hadXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Removed:" + reason);
		QueueDeferredCleanup(soundEvent, wavPath, xmlPath, "Removed:" + reason, agentIndex);
	}

	private void LogTtsReport(string stage, int agentIndex, string extra = null)
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			bool active = agent != null && agent.IsActive();
			bool hostile = IsAgentHostileToMainAgent(agent);
			string agentName = (agent?.Name?.ToString() ?? "").Trim();
			bool speaking = false;
			bool hasSe = false;
			bool hasWav = false;
			bool hasXml = false;
			lock (_speakingLock)
			{
				speaking = _speakingAgentIndices.Contains(agentIndex);
				hasSe = _agentSoundEvents.ContainsKey(agentIndex);
				hasWav = _agentWavPaths.ContainsKey(agentIndex);
				hasXml = _agentXmlPaths.ContainsKey(agentIndex);
			}
			bool hasPendingBubble = false;
			int pendingBubbleCount = 0;
			bool hasPendingDuration = false;
			int pendingDurationCount = 0;
			bool hasPendingSpeechToken = false;
			int pendingSpeechTokenCount = 0;
			lock (_ttsBubbleSyncLock)
			{
				if (_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var bubbleQueue) && bubbleQueue != null)
				{
					hasPendingBubble = bubbleQueue.Count > 0;
					pendingBubbleCount = bubbleQueue.Count;
				}
				if (_pendingAudioDurationQueues.TryGetValue(agentIndex, out var durationQueue) && durationQueue != null)
				{
					hasPendingDuration = durationQueue.Count > 0;
					pendingDurationCount = durationQueue.Count;
				}
				if (_pendingSpeechCompletionTokenQueues.TryGetValue(agentIndex, out var tokenQueue) && tokenQueue != null)
				{
					hasPendingSpeechToken = tokenQueue.Count > 0;
					pendingSpeechTokenCount = tokenQueue.Count;
				}
			}
			bool hasInteraction = _activeInteractionSessions.TryGetValue(agentIndex, out var session) && session != null;
			long interactionToken = hasInteraction ? session.InteractionToken : 0L;
			bool timeoutArmed = hasInteraction && session.TimeoutArmed;
			bool hasPendingArm = _pendingInteractionTimeoutArms.TryGetValue(agentIndex, out var pendingArm) && pendingArm != null;
			float missionTime = Mission.Current?.CurrentTime ?? (-1f);
			string sceneName = (Mission.Current?.SceneName ?? "").Trim();
			string pendingArmAt = hasPendingArm ? pendingArm.ArmAtMissionTime.ToString("F2") : "-";
			string extraSuffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;
			Logger.Log("TTSReport", $"[{stage}] agentIndex={agentIndex}, name={agentName}, active={active}, hostile={hostile}, speaking={speaking}, hasSe={hasSe}, hasWav={hasWav}, hasXml={hasXml}, hasInteraction={hasInteraction}, interactionToken={interactionToken}, timeoutArmed={timeoutArmed}, hasPendingArm={hasPendingArm}, pendingArmAt={pendingArmAt}, pendingBubble={hasPendingBubble}, pendingBubbleCount={pendingBubbleCount}, pendingDuration={hasPendingDuration}, pendingDurationCount={pendingDurationCount}, pendingSpeechToken={hasPendingSpeechToken}, pendingSpeechTokenCount={pendingSpeechTokenCount}, missionTime={missionTime:F2}, scene={sceneName}{extraSuffix}");
		}
		catch (Exception ex)
		{
			Logger.Log("TTSReport", $"[{stage}] report_failed agentIndex={agentIndex}, error={ex.Message}");
		}
	}

	private static bool IsAgentHostileToMainAgent(Agent agent)
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

	private static bool IsUsableTeam(Team team)
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

	private int ResolveAutoGroupRelayTargetAgentIndex(int relayTargetAgentIndex, int currentSpeakerAgentIndex, List<NpcDataPacket> participants, Dictionary<int, Hero> resolvedHeroes)
	{
		if (relayTargetAgentIndex < 0 || participants == null || participants.Count < 2)
		{
			return -1;
		}
		NpcDataPacket npcDataPacket = participants.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == relayTargetAgentIndex);
		if (npcDataPacket == null || npcDataPacket.AgentIndex == currentSpeakerAgentIndex || !CanNpcParticipateInAutoGroupRelay(npcDataPacket, resolvedHeroes))
		{
			return -1;
		}
		return npcDataPacket.AgentIndex;
	}

	private void PrepareAutoGroupParticipantsForIdleTimeout(List<NpcDataPacket> participants, string trailingSpeechText = null)
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
		foreach (NpcDataPacket item in list)
		{
			TrackPlayerInteraction(item, num2, ACTIVE_INTERACTION_IDLE_TIMEOUT);
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

	private void RefreshSceneConversationParticipantInteractions(List<NpcDataPacket> participants, float timeoutSeconds = -1f)
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
			TrackPlayerInteraction(item, num, timeoutSeconds);
		}
	}

	private int BeginNewPlayerDrivenSceneConversationEpoch()
	{
		int num = Interlocked.Increment(ref _sceneConversationEpoch);
		ClearPendingSceneConversationAttentionRelease();
		ClearQueuedSceneSpeech();
		try
		{
			StopAllLipSyncPlaybackAndCleanup();
		}
		catch
		{
		}
		return num;
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
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			DeactivateMultiSceneMovementSuppression();
			return;
		}
		List<int> list2 = null;
		int num = 0;
		float num2 = ACTIVE_INTERACTION_IDLE_PLAYER_RANGE * ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
		foreach (int item in list)
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == item);
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
				if (agent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2) > num2)
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
		if (num >= 1 && Mission.Current != null)
		{
			_stopStaringTime = Math.Max(_stopStaringTime, Mission.Current.CurrentTime + MULTI_SCENE_MOVEMENT_SUPPRESSION_HOLD_SECONDS);
		}
	}

	private void ApplyMultiSceneMovementSuppression(Agent agent)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
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

	private static bool ShouldSuppressSceneConversationControlForMeeting()
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
			Agent main = Agent.Main;
			if (main != null && main.IsActive() && Mission.Current?.Agents != null)
			{
				foreach (Agent agent in Mission.Current.Agents)
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

	private static void StripMeetingTauntTagsForSceneConversation(ref string content)
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

	private static bool IsMeetingSceneConversationReleaseSensitive()
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

	private static bool ShouldPreserveMeetingSceneAutonomy()
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

	private bool IsSceneConversationEpochCurrent(int epoch)
	{
		return epoch <= 0 || epoch == Volatile.Read(ref _sceneConversationEpoch);
	}

	private void ClearQueuedSceneSpeech()
	{
		lock (_speechQueueLock)
		{
			_speechQueue.Clear();
		}
	}

	private bool IsSpeechPipelineBusy()
	{
		lock (_speechQueueLock)
		{
			if (_speechQueue.Count > 0 || _speechWorkerRunning)
			{
				return true;
			}
		}
		lock (_speakingLock)
		{
			return _speakingAgentIndices.Count > 0;
		}
	}

	private static void RefreshHostileCombatAgentAutonomy(Agent agent)
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

	private static float ResolveActiveInteractionTimeoutSeconds(int participantCount, float timeoutSeconds)
	{
		if (timeoutSeconds > 0f)
		{
			return timeoutSeconds;
		}
		return (participantCount > 1) ? ACTIVE_INTERACTION_GROUP_IDLE_TIMEOUT : ACTIVE_INTERACTION_IDLE_TIMEOUT;
	}

	private void TrackPlayerInteraction(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false)
	{
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == primaryTarget.AgentIndex && a.IsActive());
		if (IsAgentBusyWithSceneSummonErrand(primaryTarget.AgentIndex) || IsAgentBusyWithSceneGuideErrand(primaryTarget.AgentIndex))
		{
			_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
			_activeInteractionSessions.Remove(primaryTarget.AgentIndex);
			return;
		}
		if (IsAgentFollowingPlayerBySceneCommand(agent))
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
		_activeInteractionSessions[primaryTarget.AgentIndex] = new SceneInteractionSession
		{
			TargetAgentIndex = primaryTarget.AgentIndex,
			TargetName = text,
			LastActivityTime = Mission.Current.CurrentTime,
			TimeoutArmed = false,
			TimeoutSeconds = ResolveActiveInteractionTimeoutSeconds(participantCount, timeoutSeconds),
			InteractionToken = DateTime.UtcNow.Ticks,
			ReturnSceneSummonOnTimeout = returnSceneSummonOnTimeout
		};
		_pendingInteractionTimeoutArms.Remove(primaryTarget.AgentIndex);
	}

	private void ScheduleInteractionTimeoutArm(int agentIndex, long interactionToken, float speechDurationSeconds)
	{
		if (agentIndex < 0 || interactionToken == 0L || Mission.Current == null)
		{
			return;
		}
		float num = Mission.Current.CurrentTime + Math.Max(0f, speechDurationSeconds);
		_pendingInteractionTimeoutArms[agentIndex] = new PendingInteractionTimeoutArm
		{
			AgentIndex = agentIndex,
			InteractionToken = interactionToken,
			ArmAtMissionTime = num
		};
	}

	private void ArmActiveInteractionTimeoutNow(int agentIndex, long interactionToken)
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		if (!_activeInteractionSessions.TryGetValue(agentIndex, out var value) || value == null || value.InteractionToken != interactionToken)
		{
			return;
		}
		value.LastActivityTime = Mission.Current.CurrentTime;
		value.TimeoutArmed = true;
		_pendingInteractionTimeoutArms.Remove(agentIndex);
	}

	private void UpdatePendingSceneDialogueFeeds()
	{
		if (Mission.Current == null || _pendingSceneDialogueFeedQueues.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
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
		if (Mission.Current == null || _pendingInteractionTimeoutArms.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
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

	private void ScheduleSceneSummonReturnAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex < 0 || Mission.Current == null || !_pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (Mission.Current.CurrentTime + num);
	}

	private void FlushSceneSummonReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneSummonReturnsAfterSpeech.Remove(agentIndex);
		if (value.Session != null && _activeSceneSummonConversationSessions.Contains(value.Session))
		{
			if (value.ReturnOnlySpeaker)
			{
				BeginSceneSummonSpeakerReturn(value.Session);
			}
			else
			{
				BeginSceneSummonConversationReturn(value.Session);
			}
		}
	}

	private void ScheduleSceneFollowCommandAfterSpeech(int agentIndex, bool startFollow, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		_pendingSceneFollowCommands[agentIndex] = new PendingSceneFollowCommand
		{
			AgentIndex = agentIndex,
			StartFollow = startFollow,
			WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished,
			ExecuteAtMissionTime = ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? (-1f) : (Mission.Current.CurrentTime + num))
		};
	}

	private void FlushSceneFollowCommandAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneFollowCommands.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneFollowCommands.Remove(agentIndex);
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		if (value.StartFollow)
		{
			RememberSceneFollowReturnState(agent, overwriteExisting: true);
			RemoveAgentFromSceneSummonConversationForFollow(agent);
			StartSceneSummonFollowPlayer(agent);
		}
		else
		{
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			ReturnAgentAfterStoppingSceneFollow(agent);
		}
	}

	private void ScheduleSceneGuideReturnAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex < 0 || Mission.Current == null || !_pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (Mission.Current.CurrentTime + num);
	}

	private void FlushSceneGuideReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneGuideReturnsAfterSpeech.Remove(agentIndex);
		if (value.LocationCharacter != null)
		{
			QueueSceneReturnJob(value.DisplayName, value.LocationCharacter, value.OriginalLocation, value.OriginalPosition);
		}
	}

	private void ScheduleSceneAutonomyRestoreAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex < 0 || Mission.Current == null || !_pendingSceneAutonomyRestoresAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (Mission.Current.CurrentTime + num);
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

	private void UpdatePendingSceneSummonReturnsAfterSpeech()
	{
		if (Mission.Current == null || _pendingSceneSummonReturnsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneSummonReturnAfterSpeech> pendingSceneSummonReturnsAfterSpeech in _pendingSceneSummonReturnsAfterSpeech)
		{
			PendingSceneSummonReturnAfterSpeech value = pendingSceneSummonReturnsAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneSummonReturnsAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneSummonReturnAfterSpeech(item);
		}
	}

	private void UpdatePendingSceneGuideReturnsAfterSpeech()
	{
		if (Mission.Current == null || _pendingSceneGuideReturnsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneGuideReturnAfterSpeech> pendingSceneGuideReturnsAfterSpeech in _pendingSceneGuideReturnsAfterSpeech)
		{
			PendingSceneGuideReturnAfterSpeech value = pendingSceneGuideReturnsAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneGuideReturnsAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneGuideReturnAfterSpeech(item);
		}
	}

	private void UpdatePendingSceneAutonomyRestoresAfterSpeech()
	{
		if (Mission.Current == null || _pendingSceneAutonomyRestoresAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
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

	private void UpdatePendingSceneFollowCommands()
	{
		if (Mission.Current == null || _pendingSceneFollowCommands.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneFollowCommand> pendingSceneFollowCommand in _pendingSceneFollowCommands)
		{
			PendingSceneFollowCommand value = pendingSceneFollowCommand.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneFollowCommand.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneFollowCommandAfterSpeech(item);
		}
	}

	private void UpdateSceneFollowSpacing()
	{
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || FollowAgentBehaviorIdleDistanceField == null)
		{
			return;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (!IsAgentFollowingPlayerBySceneCommand(agent))
			{
				continue;
			}
			try
			{
				FollowAgentBehavior followAgentBehavior = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.GetBehavior<FollowAgentBehavior>();
				if (followAgentBehavior != null)
				{
					FollowAgentBehaviorIdleDistanceField.SetValue(followAgentBehavior, SCENE_FOLLOW_MAX_IDLE_DISTANCE);
				}
			}
			catch
			{
			}
		}
	}

	private void UpdateSceneFollowHostilityState()
	{
		if (Mission.Current == null)
		{
			return;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (!IsAgentFollowingPlayerBySceneCommand(agent) || !IsAgentHostileToMainAgent(agent))
			{
				continue;
			}
			try
			{
				_pendingSceneFollowCommands.Remove(agent.Index);
				_pendingInteractionTimeoutArms.Remove(agent.Index);
				_activeInteractionSessions.Remove(agent.Index);
				_sceneFollowReturnStates.Remove(agent.Index);
				StopSceneSummonFollowPlayer(agent);
				RefreshHostileCombatAgentAutonomy(agent);
			}
			catch
			{
			}
		}
	}

	private static float EstimateBubbleTypingDurationSeconds(string content)
	{
		int num = Math.Max(0, (content ?? "").Length);
		if (num <= 0)
		{
			return 0f;
		}
		return Math.Max(1f, (float)num * 0.05f);
	}

	private static bool IsPlayerWithinActiveInteractionRange(Agent targetAgent)
	{
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float num = ACTIVE_INTERACTION_IDLE_PLAYER_RANGE * ACTIVE_INTERACTION_IDLE_PLAYER_RANGE;
			return targetAgent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2) <= num;
		}
		catch
		{
			return false;
		}
	}

	private void UpdateActiveInteractionTimeouts()
	{
		if (Mission.Current == null || _activeInteractionSessions.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		List<int> list = null;
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
			if (IsAgentBusyWithSceneSummonErrand(activeInteractionSession.Key) || IsAgentBusyWithSceneGuideErrand(activeInteractionSession.Key))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == activeInteractionSession.Key && a.IsActive());
			if (IsAgentFollowingPlayerBySceneCommand(agent))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
				_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				continue;
			}
			if (!IsPlayerWithinActiveInteractionRange(agent))
			{
				value.LastActivityTime = currentTime;
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
		foreach (int item in list)
		{
			if (!hashSet.Add(item))
			{
				continue;
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value2))
			{
				List<SceneInteractionSession> groupedTimeoutSessions = BuildGroupedExpiredInteractionSessions(value2, list);
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
			if (_activeInteractionSessions.TryGetValue(item, out var value3) && (value3 == null || ReferenceEquals(value3, value2) || value3.InteractionToken == value2?.InteractionToken))
			{
				_activeInteractionSessions.Remove(item);
			}
		}
	}

	private List<SceneInteractionSession> BuildGroupedExpiredInteractionSessions(SceneInteractionSession seedSession, List<int> expiredAgentIndices)
	{
		if (seedSession == null || seedSession.TargetAgentIndex < 0 || expiredAgentIndices == null || expiredAgentIndices.Count < 2 || seedSession.TimeoutSeconds <= 3.5f || Mission.Current == null)
		{
			return null;
		}
		Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == seedSession.TargetAgentIndex && a.IsActive());
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
			Agent agent2 = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == num && a.IsActive());
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
		if (sessions == null || sessions.Count == 0 || Mission.Current == null)
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
		SceneSummonConversationSession representativeSummonSession = TryGetSceneSummonConversationSessionForAgentIndex(representativeSession.TargetAgentIndex);
		foreach (SceneInteractionSession item2 in list)
		{
			if (item2 == null || item2.TargetAgentIndex < 0 || item2.TargetAgentIndex == representativeSession.TargetAgentIndex)
			{
				continue;
			}
			bool deferSceneSummonReturn = representativeSummonSession != null && TryGetSceneSummonConversationSessionForAgentIndex(item2.TargetAgentIndex) == representativeSummonSession;
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
		if (sessions == null || sessions.Count == 0 || Mission.Current == null)
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
			Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == sceneInteractionSession2.TargetAgentIndex && a.IsActive());
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
		if (representativeSession == null || representativeSession.TargetAgentIndex < 0 || Mission.Current == null)
		{
			return false;
		}
		Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == representativeSession.TargetAgentIndex && a.IsActive());
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
				Agent agent2 = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
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
		string text = "如果没事，大家就散了。";
		string historyText = SanitizeSceneSpeechText(text);
		if (!string.IsNullOrWhiteSpace(historyText))
		{
			RecordResponseForAllNearbySafe(list, npcDataPacket.AgentIndex, npcDataPacket.Name, historyText);
			PersistNpcSpeechToNamedHeroes(npcDataPacket.AgentIndex, npcDataPacket.Name, historyText, list);
		}
		if (representativeSummonSession != null)
		{
			ClearSceneSummonConversationInteractionTimers(representativeSummonSession);
			_pendingSceneSummonReturnsAfterSpeech[npcDataPacket.AgentIndex] = new PendingSceneSummonReturnAfterSpeech
			{
				AgentIndex = npcDataPacket.AgentIndex,
				Session = representativeSummonSession,
				ReturnOnlySpeaker = ShouldReturnOnlySceneSummonSpeaker(representativeSummonSession, agent)
			};
		}
		else
		{
			_pendingSceneAutonomyRestoresAfterSpeech[npcDataPacket.AgentIndex] = new PendingSceneAutonomyRestoreAfterSpeech
			{
				AgentIndex = npcDataPacket.AgentIndex
			};
		}
		EnqueueSpeechLineWithOptions(npcDataPacket, text, list, commitHistory: false, suppressStare: false, allowPlayerDirectedActions: false, requiredConversationEpoch: 0, null, null);
		return true;
	}

	private void ExpireActiveInteractionSilently(SceneInteractionSession session, bool deferSceneSummonReturn)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex) || IsAgentFollowingPlayerBySceneCommand(agent))
		{
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (!deferSceneSummonReturn && sceneSummonConversationSession != null)
			{
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
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
			BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

	private void ExpireActiveInteraction(SceneInteractionSession session)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (IsAgentFollowingPlayerBySceneCommand(agent))
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
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (IsAgentHostileToMainAgent(agent))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			RestoreAgentAutonomy(agent);
			if (sceneSummonConversationSession != null)
			{
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
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
		string factText = text + ": "+ text2 +"长时间没有发言，你决定去干自己的事了。";
		string prefixedFactText = "[AFEF NPC行为补充] " + factText;
		if (session.TimeoutSeconds > 3.5f)
		{
			if (sceneSummonConversationSession != null)
			{
				ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
				_pendingSceneSummonReturnsAfterSpeech[session.TargetAgentIndex] = new PendingSceneSummonReturnAfterSpeech
				{
					AgentIndex = session.TargetAgentIndex,
					Session = sceneSummonConversationSession,
					ReturnOnlySpeaker = ShouldReturnOnlySceneSummonSpeaker(sceneSummonConversationSession, agent)
				};
			}
			TriggerImmediateSceneBehaviorReaction(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false);
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
			BeginSceneSummonConversationReturn(sceneSummonConversationSession);
		}
	}

	private void RestoreAgentAutonomy(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		ClearSceneSummonScriptedBehavior(agent);
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
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			Vec3 anchorPosition = agent.Position;
			if (_staringAgentAnchors.TryGetValue(agent.Index, out var storedAnchor))
			{
				anchorPosition = storedAnchor;
			}
			if (agentNavigator != null && rotation.HasValue)
			{
				WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, anchorPosition);
				agentNavigator.SetTargetFrame(worldPosition, rotation.Value, 0.05f, 0.8f, Agent.AIScriptedFrameFlags.DoNotRun | Agent.AIScriptedFrameFlags.NoAttack, false);
			}
			else
			{
				WorldPosition worldPosition2 = new WorldPosition(Mission.Current.Scene, anchorPosition);
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
		if (agent == null || !agent.IsActive() || Mission.Current?.Scene == null || Agent.Main == null || !Agent.Main.IsActive())
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
				WorldPosition scriptedPosition = new WorldPosition(Mission.Current.Scene, vec2);
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
		if (Mission.Current == null)
		{
			TraceStareDebug("ResetStaringForActiveInteraction aborted: Mission.Current null");
			return;
		}
		TraceStareDebug("ResetStaringForActiveInteraction primary=" + GetAgentDebugLabel(primaryTarget) + " nearby=" + list.Count);
		_stopStaringTime = Mission.Current.CurrentTime + 20f;
		foreach (Agent item in list)
		{
			AddAgentToStareList(item, primaryTarget != null && item.Index == primaryTarget.Index);
		}
	}

	private void ExtendStaringHoldForPlayerDrivenSceneRound(int participantCount)
	{
		if (participantCount <= 1 || Mission.Current == null)
		{
			return;
		}
		_stopStaringTime = Math.Max(_stopStaringTime, Mission.Current.CurrentTime + PLAYER_DRIVEN_MULTI_SCENE_STARE_HOLD_SECONDS);
	}

	private async Task GenerateImmediateSceneBehaviorReactionAsync(NpcDataPacket targetNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes, bool suppressStare)
	{
		if (targetNpc == null || allNpcData == null || allNpcData.Count == 0)
		{
			return;
		}
		List<NpcDataPacket> list = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(list);
		NpcDataPacket npcDataPacket = list.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == targetNpc.AgentIndex) ?? CloneNpcDataPacket(targetNpc);
		if (npcDataPacket == null)
		{
			return;
		}
		targetNpc = npcDataPacket;
		allNpcData = list;
		await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { targetNpc }, resolvedHeroes ?? new Dictionary<int, Hero>());
		DuelSettings settings = DuelSettings.GetSettings();
		int maxTokens = Math.Max(28, settings.ShoutMaxTokens / 2);
		int minTokens = Math.Max(10, maxTokens / 2);
		Agent npcAgent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == targetNpc.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(npcAgent))
		{
			return;
		}
		CharacterObject npcCharacter = npcAgent.Character as CharacterObject;
		Hero contextHero = null;
		if (targetNpc.IsHero && resolvedHeroes != null)
		{
			resolvedHeroes.TryGetValue(targetNpc.AgentIndex, out contextHero);
		}
		string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
		MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(contextHero, "请直接根据刚刚发生的公开互动做出即时反应。", null, targetNpc.CultureId ?? "neutral", hasAnyHero: targetNpc.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: targetNpc.AgentIndex, suppressDynamicRuleAndLore: true);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【在场人物列表】：");
		foreach (NpcDataPacket item in allNpcData)
		{
			if (item != null)
			{
				stringBuilder.AppendLine(BuildSceneNpcListLineForPrompt(item));
			}
		}
		string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(allNpcData);
		if (!string.IsNullOrWhiteSpace(sceneNamingNote))
		{
			stringBuilder.AppendLine(sceneNamingNote);
		}
		string baseExtras = StripScenePersonaBlocks((shoutPromptContext?.Extras ?? "").Trim());
		string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
		string localExtras = InjectTrustBlockBelowTriState(stringBuilder.ToString().Trim(), trustBlock);
		string text = string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust);
		List<string> historyLines = null;
		lock (_historyLock)
		{
			if (_publicConversationHistory.Count > 0)
			{
				historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, targetNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(targetNpc), useNpcNameAddress: false);
			}
		}
		string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
		string persistedHeroHistory = targetNpc.IsHero ? BuildPersistedHeroHistoryContext(targetNpc.AgentIndex, "", resolvedHeroes) : "";
		string privateRecentWindowSection = "";
		string persistedWithoutRecentWindow = "";
		SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow);
		string layeredPrompt = text;
		if (!string.IsNullOrWhiteSpace(privateRecentWindowSection))
		{
			layeredPrompt = layeredPrompt + "\n" + privateRecentWindowSection;
		}
		if (!string.IsNullOrWhiteSpace(persistedWithoutRecentWindow))
		{
			layeredPrompt = layeredPrompt + "\n" + persistedWithoutRecentWindow;
		}
		string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(targetNpc.IsHero ? ResolveHeroFromAgentIndex(targetNpc.AgentIndex) : null);
		if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
		{
			layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
		}
		if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
		{
			layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
		}
		string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(targetNpc, contextHero, new List<NpcDataPacket> { targetNpc });
		if (!string.IsNullOrWhiteSpace(roleTopIntro))
		{
			layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
		}
		string singleReplyUserContent = "请只根据【当前场景公共对话与互动】、你自己的身份、处境和性格，回复一段发言,控制在 32-64 字之间,只输出你嘴里说出的话，不要描述你的行为和思考";
		List<object> messages = new List<object>
		{
			new
			{
				role = "system",
				content = layeredPrompt
			},
			new
			{
				role = "user",
				content = singleReplyUserContent
			}
		};
		string text2 = await ShoutNetwork.CallApiWithMessages(messages, 5000);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		string text3 = (text2 ?? "").Replace("\r", "").Trim();
		text3 = Regex.Replace(text3, "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
		text3 = StripNpcNamePrefixSafely(text3, 30);
		text3 = StripLeakedPromptContentForShout(text3);
		text3 = StripStageDirectionsForPassiveShout(text3);
		if (string.IsNullOrWhiteSpace(text3))
		{
			return;
		}
		RecordResponseForAllNearbySafe(allNpcData, targetNpc.AgentIndex, targetNpc.Name, text3);
		PersistNpcSpeechToNamedHeroes(targetNpc.AgentIndex, targetNpc.Name, text3, allNpcData);
		EnqueueSpeechLine(targetNpc, text3, allNpcData, skipHistory: true, suppressStare: suppressStare);
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
			if (hero == null)
			{
				return "";
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
					return (value.PersistedHistoryContext ?? "").Trim();
				}
				value.PersistedHistoryContext = BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes);
				value.HasPersistedHistoryContext = !string.IsNullOrWhiteSpace(value.PersistedHistoryContext);
				return (value.PersistedHistoryContext ?? "").Trim();
			}
			return BuildPersistedHeroHistoryContext(agentIndex, currentInput, resolvedHeroes);
		}
		catch
		{
			return "";
		}
	}

	private void PersistPlayerMessageToNamedHeroes(string text, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(text) || nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum != null && nearbyDatum.IsHero)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
				if (agent != null && agent.Character is CharacterObject co && co.HeroObject != null)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, text, null, null, _sceneHistorySessionId);
				}
			}
		}
	}

	private void PersistNpcSpeechToNamedHeroes(int speakerAgentIndex, string speakerName, string response, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(response) || nearbyData == null || nearbyData.Count == 0)
		{
			return;
		}
		string text = ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		foreach (NpcDataPacket nearbyDatum in nearbyData)
		{
			if (nearbyDatum == null || !nearbyDatum.IsHero)
			{
				continue;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == nearbyDatum.AgentIndex);
			if (agent != null && agent.Character is CharacterObject co && co.HeroObject != null)
			{
				if (nearbyDatum.AgentIndex == speakerAgentIndex)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, null, response, null, _sceneHistorySessionId);
					continue;
				}
				string aiText = "[场景喊话] " + text + ": " + response;
				MyBehavior.AppendExternalSceneDialogueHistory(co.HeroObject, null, aiText, null, _sceneHistorySessionId);
			}
		}
	}

	public static int GetCurrentSceneHistorySessionIdForExternal()
	{
		return _sceneHistorySessionId;
	}

	public void UpdateStaringBehavior()
	{
		if (_staringAgents.Count <= 0 || Mission.Current == null)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
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
		if (agent != null)
		{
			if (Mission.Current != null)
			{
				_stopStaringTime = Math.Max(_stopStaringTime, Mission.Current.CurrentTime + 20f);
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
		if (Mission.Current != null)
		{
			Mission.Current.Scene.TimeSpeed = 0.0001f;
		}
		PauseTtsForShoutUi();
	}

	private void ResumeGame()
	{
		if (Mission.Current != null)
		{
			Mission.Current.Scene.TimeSpeed = 1f;
		}
		ResumeTtsAfterShoutUi();
		_isProcessingShout = false;
	}

	private void OnShoutCancelled()
	{
		DeactivateMultiSceneMovementSuppression();
		ResumeGame();
	}
}
