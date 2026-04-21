using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class ShoutBehavior : CampaignBehaviorBase
{
	private enum ShoutChatMode
	{
		Normal,
		Give,
		Show
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
	}

	private class ShoutPendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;

		public int InventoryUnitValue;
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

	private sealed class AutoNpcGroupChatSession
	{
		public int ConversationEpoch;

		public int PrimaryAgentIndex = -1;

		public List<int> ParticipantAgentIndices = new List<int>();

		public int NextSpeakerCursor;

		public int LastSpeakerAgentIndex = -1;

		public int AutoLinesGenerated;

		public int ConsecutiveNoContinueCount;

		public int MaxAutoLines = 8;

		public bool IsActive = true;

		public bool GenerationInFlight;
	}

	private class ShoutMissionBehavior : MissionBehavior
	{
		private ShoutBehavior _parent;

		public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

		public ShoutMissionBehavior(ShoutBehavior parent)
		{
			_parent = parent;
		}

		public override void OnMissionTick(float dt)
		{
			//IL_0379: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c3: Expected O, but got Unknown
			//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
			Action result;
			while (_parent._mainThreadActions.TryDequeue(out result))
			{
				try
				{
					result();
				}
				catch (Exception exception)
				{
					BannerlordExceptionSentinel.ReportObservedException("LipSync.MainThreadAction", exception, "behavior=ShoutMissionBehavior");
				}
			}
			try
			{
				_parent.ProcessDeferredCleanup();
			}
			catch (Exception exception2)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.ProcessDeferredCleanup", exception2, "behavior=ShoutMissionBehavior");
			}
			try
			{
				_parent.TickLipSyncAnimations(dt);
			}
			catch (Exception exception3)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.TickLipSyncAnimations", exception3, "behavior=ShoutMissionBehavior");
			}
			bool flag = ShouldSuppressSceneConversationControlForMeeting();
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
			InputKey val = (InputKey)37;
			if (!string.IsNullOrEmpty(settings.ShoutKey) && Enum.TryParse<InputKey>(settings.ShoutKey.ToUpper(), out InputKey result2))
			{
				val = result2;
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
			if (Input.IsKeyPressed((InputKey)1))
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
			if (!HotkeyInputGuard.IsTextInputFocused() && Input.IsKeyPressed(val))
			{
				if (_parent._isProcessingShout)
				{
					InformationManager.DisplayMessage(new InformationMessage("正在处理中...", new Color(1f, 1f, 0f, 1f)));
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
			((MissionBehavior)this).OnRemoveBehavior();
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

	private bool _shoutPendingTradeIsGive = false;

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

	private readonly object _autoGroupChatLock = new object();

	private AutoNpcGroupChatSession _autoGroupChatSession = null;

	private bool _autoGroupChatLoopRunning = false;

	private readonly List<int> _pendingPlayerTurnCarryAgentIndices = new List<int>();

	private const int MAX_HISTORY_TURNS = 20;

	private const int AUTO_GROUP_CHAT_MAX_LINES = 8;

	private const int AUTO_GROUP_CHAT_IDLE_POLL_MS = 250;

	private const int AUTO_GROUP_CHAT_CONSECUTIVE_NO_CONTINUE_LIMIT = 2;

	private const string AUTO_GROUP_CHAT_END_TAG = "[END]";

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

	private readonly Dictionary<int, SceneFollowReturnState> _sceneFollowReturnStates = new Dictionary<int, SceneFollowReturnState>();

	private int _nextSceneSummonBatchId = 1;

	private static readonly object _ttsEventSubLock = new object();

	private static ShoutBehavior _ttsEventSubscribedOwner = null;

	private static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	private readonly bool _enableRhubarbSoundEventPlayback = true;

	private FloatingTextMissionView _floatingTextView => FloatingTextManager.Instance.MissionView;

	internal static ShoutBehavior CurrentInstance
	{
		get
		{
			try
			{
				Campaign current = Campaign.Current;
				return (current != null) ? current.GetCampaignBehavior<ShoutBehavior>() : null;
			}
			catch
			{
				return null;
			}
		}
	}

	private static void LogLipSyncNativeProbe(string stage, int agentIndex, string extra = null)
	{
		try
		{
			int num = -1;
			try
			{
				num = Thread.CurrentThread.ManagedThreadId;
			}
			catch
			{
			}
			string text = "";
			try
			{
				Mission current = Mission.Current;
				text = ((current != null) ? current.SceneName : null) ?? "";
			}
			catch
			{
			}
			string text2 = (string.IsNullOrWhiteSpace(extra) ? string.Empty : (", " + extra));
			Logger.Log("LipSyncProbe", $"stage={stage}, agentIndex={agentIndex}, thread={num}, scene={text}{text2}");
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
			int agentIndex = list[i];
			StopAgentRhubarbRecordIfPossible(agentIndex, reason);
			if (!IsInEscapeTransitionWindow())
			{
				SafeStopAndReleaseSoundEvent(list2[i]);
			}
			QueueDeferredCleanup(null, list3[i], list4[i], reason, agentIndex);
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
			string text = "";
			try
			{
				text = (((from a in AppDomain.CurrentDomain.GetAssemblies()
					select a?.GetType("TaleWorlds.CampaignSystem.CharacterHelper", throwOnError: false) ?? a?.GetType("TaleWorlds.CampaignSystem.Helpers.CharacterHelper", throwOnError: false)).FirstOrDefault((Type t) => t != null)?.GetMethod("GetDefaultFaceIdle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(CharacterObject) }, null))?.Invoke(null, new object[1] { (object)/*isinst with value type is only supported in some contexts*/ }) as string) ?? "";
			}
			catch
			{
				text = "";
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				agent.SetAgentFacialAnimation((FacialAnimChannel)1, text, true);
			}
			agent.SetAgentFacialAnimation((FacialAnimChannel)0, "", false);
			LogLipSyncNativeProbe("PrepareTrueLipSyncFaceIdle", agent.Index, "idle=" + text);
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] PrepareAgentForTrueLipSyncIfPossible failed, agentIndex={agent.Index}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.PrepareTrueLipSyncFaceIdle", ex, "agentIndex=" + agent.Index);
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
			return CurrentInstance?._isProcessingShout ?? false;
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
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		try
		{
			return agent != null && agent.IsHuman && agent.IsActive() && (int)agent.State == 1 && agent.Health > 0f;
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
		PendingNpcBubbleEntry pendingNpcBubbleEntry = null;
		float num = -1f;
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
			Queue<float> value2;
			bool flag = _pendingAudioDurationQueues.TryGetValue(agentIndex, out value2) && value2 != null && value2.Count > 0;
			if (!flag && !allowFallbackDuration)
			{
				return false;
			}
			pendingNpcBubbleEntry = value.Dequeue();
			if (value.Count == 0)
			{
				_pendingNpcBubbleQueues.Remove(agentIndex);
			}
			if (flag)
			{
				num = value2.Dequeue();
				if (value2.Count == 0)
				{
					_pendingAudioDurationQueues.Remove(agentIndex);
				}
			}
			else
			{
				num = pendingNpcBubbleEntry?.FallbackDurationSeconds ?? (-1f);
			}
		}
		if (pendingNpcBubbleEntry == null)
		{
			return false;
		}
		if (num <= 0f || float.IsNaN(num) || float.IsInfinity(num))
		{
			num = EstimateBubbleTypingDurationSeconds(pendingNpcBubbleEntry.UiContent);
		}
		Agent agent = pendingNpcBubbleEntry.Agent;
		LogTtsReport("PlaybackStarted.BubbleDispatchStart", agentIndex, $"bubbleAgent={((agent != null) ? agent.Index : (-1))};typingDuration={num:F2};fallback={num == pendingNpcBubbleEntry.FallbackDurationSeconds}");
		TryShowNpcBubble(pendingNpcBubbleEntry.Agent, pendingNpcBubbleEntry.UiContent, num);
		LogTtsReport("PlaybackStarted.BubbleDispatchEnd", agentIndex, $"typingDuration={num:F2}");
		return true;
	}

	private void SchedulePendingNpcBubbleFallbackDispatch(int agentIndex, int delayMs = 180, int remainingRetries = 3)
	{
		if (agentIndex < 0)
		{
			return;
		}
		Task.Run(async delegate
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
			if (!_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value) || value == null || value.Count <= 0)
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
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (TryDequeuePendingSceneDialogueFeed(agentIndex, out var entry) && entry != null)
		{
			RecordSceneDialogueToMessageFeed(entry.SpeakerLabel, entry.Content, entry.Color);
		}
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
			float executeAtMissionTime = Mission.Current.CurrentTime + Math.Max(0f, delaySeconds);
			foreach (PendingSceneDialogueFeedEntry pendingSceneDialogueFeedEntry in array)
			{
				if (!flag && pendingSceneDialogueFeedEntry != null && pendingSceneDialogueFeedEntry.WaitForPlaybackFinished)
				{
					pendingSceneDialogueFeedEntry.WaitForPlaybackFinished = false;
					pendingSceneDialogueFeedEntry.ExecuteAtMissionTime = executeAtMissionTime;
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
		if (agentIndex < 0 || interactionToken == 0)
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
				return interactionToken != 0;
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
		string failText = (string.IsNullOrWhiteSpace(errorMessage) ? "语音合成失败，已切换为文字气泡。" : ("语音合成失败：" + errorMessage));
		Logger.Log("LipSync", $"[OnPlaybackFailed] agentIndex={agentIndex}, error={errorMessage}");
		LogTtsReport("PlaybackFailed", agentIndex, $"error={errorMessage};hasInteractionToken={hasInteractionToken};hasBubble={hasBubble};typingDuration={typingDuration:F2}");
		_mainThreadActions.Enqueue(delegate
		{
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0172: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Expected O, but got Unknown
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
				InformationManager.DisplayMessage(new InformationMessage("[TTS错误] " + failText, new Color(1f, 0.35f, 0.35f, 1f)));
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
					LogTtsReport("PlaybackFailed.CleanupQueued", agentIndex, $"hasSe={value != null};hasWav={!string.IsNullOrWhiteSpace(value2)};hasXml={!string.IsNullOrWhiteSpace(value3)}");
				}
			}
			catch
			{
			}
		});
	}

	private void RecordSceneDialogueToMessageFeed(string speakerLabel, string content, Color color)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		string text = (content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = (speakerLabel ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "NPC";
			}
			InformationManager.DisplayMessage(new InformationMessage("[" + text2 + "] " + text, color));
		}
	}

	private void RecordPlayerSpeechToMessageFeed(string content)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		RecordSceneDialogueToMessageFeed("你", content, new Color(0.3f, 1f, 0.3f, 1f));
	}

	private void RecordNpcSpeechToMessageFeed(string npcDisplayName, string content)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		RecordSceneDialogueToMessageFeed(npcDisplayName, content, new Color(1f, 0.8f, 0.2f, 1f));
	}

	private void ScheduleNpcSpeechToMessageFeed(int agentIndex, string npcDisplayName, string content, SceneSpeechPlaybackInfo playbackInfo)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (agentIndex >= 0 && !string.IsNullOrWhiteSpace(content) && Mission.Current != null)
		{
			float num = Math.Max(0f, playbackInfo?.VisualDurationSeconds ?? 0f);
			bool flag = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
			float executeAtMissionTime = (flag ? (-1f) : (Mission.Current.CurrentTime + num));
			EnqueuePendingSceneDialogueFeed(agentIndex, npcDisplayName, content, new Color(1f, 0.8f, 0.2f, 1f), flag, executeAtMissionTime);
		}
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
		string text3 = GetSceneNpcHistoryNameForPrompt(npc);
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "NPC";
		}
		bool flag = IsAgentHostileToMainAgent(liveAgent);
		if (flag && num >= 0)
		{
			_activeInteractionSessions.Remove(num);
			_pendingInteractionTimeoutArms.Remove(num);
			lock (_ttsBubbleSyncLock)
			{
				_pendingSpeechCompletionTokenQueues.Remove(num);
			}
		}
		long num2 = 0L;
		if (!flag && num >= 0 && _activeInteractionSessions.TryGetValue(num, out var value) && value != null)
		{
			num2 = value.InteractionToken;
		}
		bool flag2 = false;
		bool flag3 = (sceneSpeechPlaybackInfo.TtsEnabled = allowTts && IsTtsPlaybackEnabledForShout());
		string reason = "scene_lipsync_not_requested";
		bool flag4 = flag3 && attachTtsToSceneAgent && num >= 0 && CanAgentParticipateInSceneSpeech(liveAgent) && CanAgentUseSceneLipSync(liveAgent, out reason);
		int num3 = (flag4 ? num : (-1));
		LogTtsReport("ShowNpcSpeechOutput.Enter", num, $"allowTts={allowTts};attachToSceneAgent={attachTtsToSceneAgent};effectiveAgentIndex={num3};contentLen={(text ?? string.Empty).Length};hostileSpeech={flag};lipSyncSafe={flag4};lipSyncReason={reason}");
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
		else if (flag3 && attachTtsToSceneAgent && num >= 0 && num3 < 0)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Use detached TTS without scene lipsync. agentIndex=" + num + ", reason=" + reason);
			}
			catch
			{
			}
		}
		if (flag3)
		{
			string text4 = "";
			try
			{
				if (npc != null && npc.IsHero)
				{
					Hero val = ResolveHeroFromAgentIndex(npc.AgentIndex);
					if (val != null)
					{
						text4 = MyBehavior.GetNpcVoiceIdForExternal(val);
						if (string.IsNullOrWhiteSpace(text4))
						{
							text4 = VoiceMapper.ResolveVoiceId(val);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text4) && npc != null)
				{
					text4 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, npc.AgentIndex);
				}
				flag2 = TtsEngine.Instance.SpeakAsync(text, -1, -1f, num3, text4);
			}
			catch
			{
			}
			sceneSpeechPlaybackInfo.TtsAccepted = flag2;
			sceneSpeechPlaybackInfo.WaitForPlaybackFinished = flag2 && num3 >= 0;
			LogTtsReport("ShowNpcSpeechOutput.SpeakAttempt", num, $"effectiveAgentIndex={num3};speakAccepted={flag2};voiceId={text4};lipSyncSafe={flag4};lipSyncReason={reason}");
		}
		if (flag2 && num3 >= 0 && CanAgentParticipateInSceneSpeech(liveAgent))
		{
			sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, EstimateBubbleTypingDurationSeconds(text));
			ClearPendingTtsBubbleSyncForAgent(num);
			if (num2 != 0)
			{
				EnqueuePendingSpeechCompletionToken(num, num2);
			}
			EnqueuePendingNpcBubble(num, liveAgent, text, text3, sceneSpeechPlaybackInfo.VisualDurationSeconds);
			ScheduleNpcSpeechToMessageFeed(num, text3, text, sceneSpeechPlaybackInfo);
			MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent);
			LogTtsReport("ShowNpcSpeechOutput.SceneBubbleQueued", num, $"interactionToken={num2};uiLen={text.Length}");
			return sceneSpeechPlaybackInfo;
		}
		float num4 = (sceneSpeechPlaybackInfo.VisualDurationSeconds = EstimateBubbleTypingDurationSeconds(text));
		if (!TryShowNpcBubble(liveAgent, text, num4))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + text3);
		}
		if (num2 != 0)
		{
			ScheduleInteractionTimeoutArm(num, num2, num4);
		}
		ScheduleNpcSpeechToMessageFeed(num, text3, text, sceneSpeechPlaybackInfo);
		MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent);
		LogTtsReport("ShowNpcSpeechOutput.BubbleFallback", num, $"interactionToken={num2};typingDuration={num4:F2};ttsAccepted={flag2};ttsEnabled={flag3}");
		if (!(!flag2 && flag3))
		{
			return sceneSpeechPlaybackInfo;
		}
		try
		{
			string text5 = "";
			if (npc != null && npc.IsHero)
			{
				Hero val2 = ResolveHeroFromAgentIndex(npc.AgentIndex);
				if (val2 != null)
				{
					text5 = MyBehavior.GetNpcVoiceIdForExternal(val2);
					if (string.IsNullOrWhiteSpace(text5))
					{
						text5 = VoiceMapper.ResolveVoiceId(val2);
					}
				}
			}
			if (string.IsNullOrWhiteSpace(text5) && npc != null)
			{
				text5 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, npc.AgentIndex);
			}
			TtsEngine.Instance.SpeakAsync(text, -1, -1f, num3, text5);
			LogTtsReport("ShowNpcSpeechOutput.DetachedSpeakFallback", num, $"effectiveAgentIndex={num3};voiceId={text5}");
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
			Agent agent = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
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

	private static string BuildSceneNpcListLineForPrompt(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "- 名字: 未知 | 身份: 未知身份";
		}
		string text = (npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc));
		return "- 名字: " + text + " | 身份: " + GetSceneNpcListIdentityForPrompt(npc);
	}

	private static string ResolveSceneHistorySpeakerNameForPrompt(int speakerAgentIndex, string fallbackSpeakerName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == speakerAgentIndex);
		string text = ((npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : (fallbackSpeakerName ?? "").Trim());
		return string.IsNullOrWhiteSpace(text) ? "某NPC" : text;
	}

	private static string ResolveSceneTargetNameForPrompt(int targetAgentIndex, string fallbackTargetName, IEnumerable<NpcDataPacket> nearbyData = null)
	{
		NpcDataPacket npcDataPacket = nearbyData?.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == targetAgentIndex);
		string text = ((npcDataPacket != null) ? GetSceneNpcHistoryNameForPrompt(npcDataPacket) : (fallbackTargetName ?? "").Trim());
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
		string text = (((location != null) ? location.StringId : null) ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
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
		if (1 == 0)
		{
		}
		return result;
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
			Location val = queue.Dequeue();
			IEnumerable<Location> locationsOfPassages = val.LocationsOfPassages;
			IEnumerable<Location> enumerable = locationsOfPassages ?? Enumerable.Empty<Location>();
			foreach (Location item in enumerable)
			{
				if (item == null || !hashSet.Add(item))
				{
					continue;
				}
				dictionary[item] = val;
				if (item == toLocation)
				{
					List<Location> list = new List<Location> { toLocation };
					Location key = toLocation;
					Location value;
					while (dictionary.TryGetValue(key, out value))
					{
						list.Add(value);
						if (value == fromLocation)
						{
							break;
						}
						key = value;
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
		ICampaignMission current = CampaignMission.Current;
		Location location = ((current != null) ? current.Location : null);
		if (locationComplex == null || location == null)
		{
			return list;
		}
		HashSet<LocationCharacter> hashSet = new HashSet<LocationCharacter>();
		if (presentNpcs != null)
		{
			foreach (NpcDataPacket presentNpc in presentNpcs)
			{
				if (presentNpc == null)
				{
					continue;
				}
				LocationCharacter locationCharacter = null;
				if (presentNpc.IsHero)
				{
					Hero value = null;
					resolvedHeroes?.TryGetValue(presentNpc.AgentIndex, out value);
					if (value == null)
					{
						value = ResolveHeroFromAgentIndex(presentNpc.AgentIndex);
					}
					if (value != null)
					{
						locationCharacter = locationComplex.GetLocationCharacterOfHero(value);
					}
				}
				else
				{
					Mission current2 = Mission.Current;
					Agent val = ((current2 == null) ? null : ((IEnumerable<Agent>)current2.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == presentNpc.AgentIndex));
					if (val != null)
					{
						locationCharacter = locationComplex.FindCharacter((IAgent)(object)val);
					}
				}
				tryAdd(locationCharacter, GetSceneSummonTargetDisplayName(presentNpc));
				if (list.Count < 12)
				{
					continue;
				}
				break;
			}
		}
		if (list.Count < 12)
		{
			IEnumerable<IGrouping<Location, LocationCharacter>> enumerable = (from val2 in locationComplex.GetListOfCharacters()
				where val2 != null && val2.Character != null && ((BasicCharacterObject)val2.Character).IsHero && val2.Character.HeroObject != null && val2.Character.HeroObject != Hero.MainHero
				group val2 by locationComplex.GetLocationOfCharacter(val2) into grouped
				orderby FindSceneLocationPath(location, grouped.Key)?.Count ?? int.MaxValue
				select grouped).ThenBy(delegate(IGrouping<Location, LocationCharacter> grouped)
			{
				Location key = grouped.Key;
				return ((key != null) ? key.StringId : null) ?? "";
			}).ThenBy(delegate(IGrouping<Location, LocationCharacter> grouped)
			{
				LocationCharacter obj = grouped.FirstOrDefault();
				object obj2;
				if (obj == null)
				{
					obj2 = null;
				}
				else
				{
					CharacterObject character2 = obj.Character;
					obj2 = ((character2 == null) ? null : ((object)((BasicCharacterObject)character2).Name)?.ToString());
				}
				if (obj2 == null)
				{
					obj2 = "";
				}
				return (string)obj2;
			});
			foreach (IGrouping<Location, LocationCharacter> item in enumerable)
			{
				foreach (LocationCharacter item2 in item.OrderBy(delegate(LocationCharacter x)
				{
					CharacterObject character2 = x.Character;
					return (((character2 == null) ? null : ((object)((BasicCharacterObject)character2).Name)?.ToString()) ?? "").Trim();
				}, StringComparer.CurrentCulture))
				{
					CharacterObject character = item2.Character;
					object displayName;
					if (character == null)
					{
						displayName = null;
					}
					else
					{
						Hero heroObject = character.HeroObject;
						displayName = ((heroObject == null) ? null : ((object)heroObject.Name)?.ToString());
					}
					tryAdd(item2, (string)displayName);
					if (list.Count >= 12)
					{
						break;
					}
				}
				if (list.Count >= 12)
				{
					break;
				}
			}
		}
		for (int num = 0; num < list.Count; num++)
		{
			list[num].PromptId = num + 1;
		}
		return list;
		void tryAdd(LocationCharacter val2, string text2)
		{
			if (val2 != null && !hashSet.Contains(val2) && val2.Character != null && val2.Character != CharacterObject.PlayerCharacter)
			{
				Location locationOfCharacter = locationComplex.GetLocationOfCharacter(val2);
				if (locationOfCharacter != null)
				{
					List<Location> list2 = FindSceneLocationPath(location, locationOfCharacter);
					if (list2 != null && list2.Count != 0)
					{
						string text = (text2 ?? "").Trim();
						if (string.IsNullOrWhiteSpace(text))
						{
							text = (((object)((BasicCharacterObject)val2.Character).Name)?.ToString() ?? "").Trim();
						}
						if (!string.IsNullOrWhiteSpace(text))
						{
							hashSet.Add(val2);
							list.Add(new SceneSummonPromptTarget
							{
								DisplayName = text,
								LocationCode = GetSceneSummonLocationCode(locationOfCharacter),
								LocationCharacter = val2,
								SourceLocation = locationOfCharacter
							});
						}
					}
				}
			}
		}
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
			if (item != null)
			{
				list.Add(new SceneSummonPromptTarget
				{
					PromptId = item.PromptId,
					DisplayName = item.DisplayName,
					LocationCode = item.LocationCode,
					LocationCharacter = item.LocationCharacter,
					SourceLocation = item.SourceLocation
				});
			}
		}
		return list;
	}

	private static string GetSceneGuideCategoryDisplayName(Occupation occupation)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result;
		if ((int)occupation <= 12)
		{
			if ((int)occupation != 4)
			{
				switch (occupation - 10)
				{
				case 1:
					break;
				case 0:
					goto IL_003d;
				case 2:
					goto IL_004d;
				default:
					goto IL_0065;
				}
				result = "盔甲匠";
			}
			else
			{
				result = "商贩";
			}
		}
		else if ((int)occupation != 18)
		{
			if ((int)occupation != 28)
			{
				goto IL_0065;
			}
			result = "武器匠";
		}
		else
		{
			result = "商贩";
		}
		goto IL_006d;
		IL_006d:
		if (1 == 0)
		{
		}
		return result;
		IL_003d:
		result = "武器匠";
		goto IL_006d;
		IL_0065:
		result = "";
		goto IL_006d;
		IL_004d:
		result = "马匹贩子";
		goto IL_006d;
	}

	private static bool TryGetSceneGuideTargetLabel(LocationCharacter locationCharacter, out string displayName, out bool preferNearest)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		displayName = "";
		preferNearest = false;
		Occupation val = (Occupation)0;
		try
		{
			CharacterObject val2 = ((locationCharacter != null) ? locationCharacter.Character : null);
			if (val2 != null)
			{
				val = val2.Occupation;
			}
		}
		catch
		{
			val = (Occupation)0;
		}
		displayName = GetSceneGuideCategoryDisplayName(val);
		preferNearest = (int)val == 18 || (int)val == 4;
		return !string.IsNullOrWhiteSpace(displayName);
	}

	private List<SceneGuidePromptTarget> BuildSceneGuidePromptTargets(Agent guideAgent = null)
	{
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		List<SceneGuidePromptTarget> list = new List<SceneGuidePromptTarget>();
		LocationComplex current = LocationComplex.Current;
		ICampaignMission current2 = CampaignMission.Current;
		Location val = ((current2 != null) ? current2.Location : null);
		if (current == null || val == null)
		{
			return list;
		}
		Dictionary<string, List<LocationCharacter>> dictionary = new Dictionary<string, List<LocationCharacter>>(StringComparer.OrdinalIgnoreCase);
		foreach (LocationCharacter listOfCharacter in current.GetListOfCharacters())
		{
			if (!TryGetSceneGuideTargetLabel(listOfCharacter, out var displayName, out var _))
			{
				continue;
			}
			if (string.Equals(displayName, "商贩", StringComparison.OrdinalIgnoreCase))
			{
				CharacterObject character = listOfCharacter.Character;
				if (character != null && ((BasicCharacterObject)character).IsHero)
				{
					continue;
				}
			}
			Location locationOfCharacter = current.GetLocationOfCharacter(listOfCharacter);
			if (locationOfCharacter == null)
			{
				continue;
			}
			List<Location> list2 = FindSceneLocationPath(val, locationOfCharacter);
			if (list2 != null && list2.Count != 0)
			{
				if (!dictionary.TryGetValue(displayName, out var value))
				{
					value = (dictionary[displayName] = new List<LocationCharacter>());
				}
				value.Add(listOfCharacter);
			}
		}
		_003F val2;
		if (guideAgent == null)
		{
			Agent main = Agent.Main;
			val2 = ((main != null) ? main.Position : Vec3.Zero);
		}
		else
		{
			val2 = guideAgent.Position;
		}
		Vec3 vec = (Vec3)val2;
		string[] array = new string[4] { "盔甲匠", "武器匠", "马匹贩子", "商贩" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!dictionary.TryGetValue(text, out var value2) || value2 == null || value2.Count == 0)
			{
				continue;
			}
			LocationCharacter val3 = value2[0];
			val3 = ((!string.Equals(text, "商贩", StringComparison.OrdinalIgnoreCase)) ? value2.OrderBy(delegate(LocationCharacter x)
			{
				CharacterObject character2 = x.Character;
				return (((character2 == null) ? null : ((object)((BasicCharacterObject)character2).Name)?.ToString()) ?? "").Trim();
			}, StringComparer.CurrentCulture).FirstOrDefault() : value2.OrderBy(delegate(LocationCharacter x)
			{
				//IL_001a: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				Agent val4 = ResolveAgentForLocationCharacter(x);
				if (val4 != null && val4.IsActive())
				{
					Vec3 position = val4.Position;
					return ((Vec3)(ref position)).DistanceSquared(vec);
				}
				return float.MaxValue;
			}).ThenBy(delegate(LocationCharacter x)
			{
				CharacterObject character2 = x.Character;
				return (((character2 == null) ? null : ((object)((BasicCharacterObject)character2).Name)?.ToString()) ?? "").Trim();
			}, StringComparer.CurrentCulture).FirstOrDefault());
			if (val3 == null)
			{
				continue;
			}
			Location locationOfCharacter2 = current.GetLocationOfCharacter(val3);
			if (locationOfCharacter2 != null)
			{
				list.Add(new SceneGuidePromptTarget
				{
					PromptId = list.Count + 1,
					DisplayName = text,
					LocationCode = GetSceneSummonLocationCode(locationOfCharacter2),
					LocationCharacter = val3,
					SourceLocation = locationOfCharacter2
				});
				if (list.Count >= 6)
				{
					break;
				}
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
			if (item != null)
			{
				list.Add(new SceneGuidePromptTarget
				{
					PromptId = item.PromptId,
					DisplayName = item.DisplayName,
					LocationCode = item.LocationCode,
					LocationCharacter = item.LocationCharacter,
					SourceLocation = item.SourceLocation
				});
			}
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
		if (agentIndex >= 0 && Mission.Current != null)
		{
			ActiveSceneSummonRequest request;
			while (TryDequeuePendingSceneSummonLaunch(agentIndex, out request))
			{
				request.NextStageMissionTime = Mission.Current.CurrentTime + Math.Max(0f, additionalDelaySeconds);
				LogSceneSummonState("pending_launch_tts_finished", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "extraDelay=" + additionalDelaySeconds.ToString("F2"), force: true);
			}
		}
	}

	private void FlushPendingSceneGuideLaunches(int agentIndex, float additionalDelaySeconds = 0f)
	{
		if (agentIndex >= 0 && Mission.Current != null)
		{
			ActiveSceneGuideRequest request;
			while (TryDequeuePendingSceneGuideLaunch(agentIndex, out request))
			{
				request.NextStageMissionTime = Mission.Current.CurrentTime + Math.Max(0f, additionalDelaySeconds);
			}
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
			foreach (int item2 in list)
			{
				if (_pendingSceneSummonLaunchQueues.TryGetValue(item2, out var value) && value != null && value.Count != 0)
				{
					Queue<ActiveSceneSummonRequest> queue = new Queue<ActiveSceneSummonRequest>(value.Where((ActiveSceneSummonRequest x) => x != null && x.BatchId != batchId));
					if (queue.Count == 0)
					{
						_pendingSceneSummonLaunchQueues.Remove(item2);
					}
					else
					{
						_pendingSceneSummonLaunchQueues[item2] = queue;
					}
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
		if (_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) && value != null && value.PendingTargets.Count != 0)
		{
			if (TryStartNextSceneSummonBatchRequest(value, speakerAgent, isInitialRequest: false, out var preparedRequest))
			{
				LogSceneSummonState("batch_followup_queued", preparedRequest, speakerAgent, ResolveAgentForLocationCharacter(preparedRequest.TargetLocationCharacter), "remaining=" + value.PendingTargets.Count, force: true);
			}
			else
			{
				CancelSceneSummonBatch(request.BatchId);
			}
		}
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
				List<string> list = (from x in sceneSummonConversationSession.Participants
					where x != null && !string.IsNullOrWhiteSpace(x.DisplayName)
					select x.DisplayName.Trim()).Distinct().ToList();
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
		if (request == null || request.BatchId < 0 || request.SpeakerAgentIndex < 0 || !_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) || value == null || value.CompletionFactRecorded || value.PendingTargets.Count > 0)
		{
			return;
		}
		int num = _activeSceneSummonRequests.Count((ActiveSceneSummonRequest x) => x != null && x.BatchId == request.BatchId);
		if (num > 1)
		{
			return;
		}
		string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
		if (!string.IsNullOrWhiteSpace(text))
		{
			string text2 = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "此人";
			}
			AppendTargetedSceneNpcFact("你已将" + text2 + "要求召集的" + text + "都带到了" + text2 + "身边。", request.SpeakerAgentIndex, persistHeroPrivateHistory: true);
			value.CompletionFactRecorded = true;
		}
	}

	private string BuildSceneSummonClosurePromptInstruction(IEnumerable<NpcDataPacket> participants)
	{
		if (participants == null)
		{
			return "";
		}
		foreach (NpcDataPacket participant in participants)
		{
			if (participant == null || participant.AgentIndex < 0 || TryGetSceneSummonConversationSessionForAgentIndex(participant.AgentIndex) == null)
			{
				continue;
			}
			return "【当前是传唤后的会面】若你同意之后跟随此人，可在句末输出[FOL]；若想结束这次会面，可输出[END]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]；若此人改让你带路去找【可带路目标】，也可输出[GUI:id]。";
		}
		return "";
	}

	private string BuildSceneFollowControlPromptInstruction(NpcDataPacket speaker)
	{
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && speaker != null && a.Index == speaker.AgentIndex));
		if (!CanAgentParticipateInSceneSpeech(val))
		{
			return "";
		}
		if (TryGetSceneSummonConversationSessionForAgentIndex(val.Index) != null)
		{
			return "";
		}
		return IsAgentFollowingPlayerBySceneCommand(val) ? "【当前正跟随玩家】若此人明确让你停止跟随且你同意，可在句末输出[STP]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]，多人可写[ASS:id1,id2]；若此人改让你带路去找【可带路目标】，也可输出[GUI:id]。" : "";
	}

	private static string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null, bool includeInventorySummary = false, bool includeTradePricing = false)
	{
		if (npc == null)
		{
			return "";
		}
		string sceneNpcIdentityNameForPrompt = GetSceneNpcIdentityNameForPrompt(npc);
		string text = ((hero == null) ? GetSceneNpcGivenNameForPrompt(npc) : "");
		string value = (npc.RoleDesc ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			value = "未知身份";
		}
		string value2 = (string.IsNullOrWhiteSpace(npc.PersonalityDesc) ? "暂无记录" : npc.PersonalityDesc.Trim());
		string value3 = (string.IsNullOrWhiteSpace(npc.BackgroundDesc) ? "暂无记录" : npc.BackgroundDesc.Trim());
		string value4 = "无（独立）";
		string text2 = "无家族";
		string text3 = "成员";
		string value5 = MyBehavior.GetClanTierReputationLabelForExternal(0) + "（0 level）";
		string text4 = (npc.CultureId ?? "").Trim();
		string value6 = MyBehavior.BuildAgeBracketLabelForExternal(npc.Age);
		string value7 = "未知";
		string value8 = "";
		string text5 = "";
		if (hero != null)
		{
			try
			{
				Clan clan = hero.Clan;
				string text6 = (((clan == null) ? null : ((object)clan.Name)?.ToString()) ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text6))
				{
					text2 = text6;
				}
				if (!string.IsNullOrWhiteSpace(text2) && !text2.EndsWith("家族", StringComparison.Ordinal))
				{
					text2 += "家族";
				}
				Clan clan2 = hero.Clan;
				int num = ((clan2 != null) ? clan2.Tier : 0);
				value5 = MyBehavior.GetClanTierReputationLabelForExternal(num) + $"（{Math.Max(0, num)} level）";
				string text7 = MyBehavior.BuildHeroIdentityTitleForExternal(hero);
				if (!string.IsNullOrWhiteSpace(text7))
				{
					value = text7.Trim();
				}
				Clan clan3 = hero.Clan;
				object obj;
				if (clan3 == null)
				{
					obj = null;
				}
				else
				{
					Kingdom kingdom = clan3.Kingdom;
					obj = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString());
				}
				if (obj == null)
				{
					obj = "";
				}
				string text8 = ((string)obj).Trim();
				if (string.IsNullOrWhiteSpace(text8))
				{
					IFaction mapFaction = hero.MapFaction;
					text8 = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
				}
				if (string.IsNullOrWhiteSpace(text8))
				{
					text8 = text2;
				}
				if (!string.IsNullOrWhiteSpace(text8))
				{
					value4 = text8;
				}
				Clan clan4 = hero.Clan;
				if (((clan4 != null) ? clan4.Leader : null) == hero)
				{
					text3 = "族长";
				}
				CultureObject culture = hero.Culture;
				string text9 = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text9))
				{
					text4 = text9;
				}
				value6 = MyBehavior.BuildAgeBracketLabelForExternal(hero.Age);
				value7 = MyBehavior.BuildHeroEquipmentSummaryForExternal(hero);
				if (includeInventorySummary && RewardSystemBehavior.Instance != null)
				{
					value8 = (RewardSystemBehavior.Instance.BuildInventorySummaryForAI(hero) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		else
		{
			string text10 = (npc.UnnamedRank ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text10))
			{
				text2 = text10;
			}
			if (!string.IsNullOrWhiteSpace(text2) && text2 != "无家族" && !text2.EndsWith("家族", StringComparison.Ordinal))
			{
				text2 += "家族";
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				value4 = text4;
			}
			value7 = BuildNonHeroEquipmentSummaryForPrompt(npc);
			try
			{
				Mission current = Mission.Current;
				Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex));
				BasicCharacterObject obj3 = ((val != null) ? val.Character : null);
				CharacterObject val2 = (CharacterObject)(object)((obj3 is CharacterObject) ? obj3 : null);
				if (includeInventorySummary && RewardSystemBehavior.Instance != null && val2 != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(val2, out var _))
				{
					string text11 = RewardSystemBehavior.Instance.BuildSettlementMerchantInventorySummaryForAI(val2);
					if (!string.IsNullOrWhiteSpace(text11))
					{
						value8 = text11.Trim();
					}
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = "未知文化";
		}
		if (!text4.EndsWith("人", StringComparison.Ordinal))
		{
			text4 += "人";
		}
		if (string.IsNullOrWhiteSpace(value6))
		{
			value6 = "未知";
		}
		if (string.IsNullOrWhiteSpace(value7))
		{
			value7 = "未知";
		}
		try
		{
			text5 = (MyBehavior.BuildCurrentDateFactForExternal() ?? "").Replace("\r", "").Trim();
			if (text5.StartsWith("当前游戏日期：", StringComparison.Ordinal))
			{
				text5 = text5.Substring("当前游戏日期：".Length).Trim();
			}
			int num2 = text5.IndexOf('（');
			if (num2 >= 0)
			{
				text5 = text5.Substring(0, num2).Trim();
			}
		}
		catch
		{
			text5 = "";
		}
		bool flag = ShouldHideSceneReputationForPrompt(npc, hero);
		StringBuilder stringBuilder = new StringBuilder();
		if (hero != null)
		{
			string value9 = ((text3 == "族长") ? "统治者" : (hero.IsFemale ? "女性成员" : "男性成员"));
			stringBuilder.Append("你名叫").Append(sceneNpcIdentityNameForPrompt).Append("，是")
				.Append(value4)
				.Append("的")
				.Append(text2)
				.Append("的")
				.Append(value9)
				.Append("，你的身份是")
				.Append(value);
			if (!flag && !string.IsNullOrWhiteSpace(value5))
			{
				stringBuilder.Append("，你").Append(value5);
			}
			stringBuilder.Append("。");
		}
		else
		{
			stringBuilder.Append("你是一个").Append(sceneNpcIdentityNameForPrompt);
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, sceneNpcIdentityNameForPrompt, StringComparison.Ordinal))
			{
				stringBuilder.Append("，名叫").Append(text);
			}
			stringBuilder.Append("，你的身份是").Append(value);
			if (!flag && !string.IsNullOrWhiteSpace(value5))
			{
				stringBuilder.Append("，你").Append(value5);
			}
			stringBuilder.Append("。");
		}
		stringBuilder.AppendLine().Append("你身上穿着").Append(value7)
			.Append("。");
		if (hero != null)
		{
			stringBuilder.AppendLine().Append("你的个性为：").Append(value2)
				.Append("。")
				.AppendLine()
				.Append("你的背景是：")
				.Append(value3)
				.Append("。");
		}
		stringBuilder.AppendLine().Append("你的年纪是").Append(value6)
			.Append("，你是")
			.Append(text4)
			.Append("。");
		if (!string.IsNullOrWhiteSpace(text5))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("现在的时间是").Append(text5).Append("。");
		}
		string value10 = BuildSceneLocationAndSettlementLineForPrompt(hero);
		if (!string.IsNullOrWhiteSpace(value10))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value10);
		}
		string text12 = (ShoutUtils.BuildCurrentSettlementHeroNpcLineForPrompt() ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(text12))
		{
			if (text12.StartsWith("当前定居点HeroNPC：", StringComparison.Ordinal))
			{
				text12 = text12.Substring("当前定居点HeroNPC：".Length).Trim();
			}
			if (!string.IsNullOrWhiteSpace(text12))
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("这个定居点有这些人物：").Append(text12);
			}
		}
		string value11 = BuildSettlementFlavorLineForPrompt(hero);
		if (!string.IsNullOrWhiteSpace(value11))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value11);
		}
		string value12 = BuildSettlementRulerPresenceLineForPrompt();
		if (!string.IsNullOrWhiteSpace(value12))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value12);
		}
		string value13 = BuildScenePlayerIntroForPrompt(hero, includeTradePricing);
		if (!string.IsNullOrWhiteSpace(value13))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value13);
		}
		string value14 = BuildNearbyPresentNpcLineForPrompt(npc, presentNpcs);
		if (!string.IsNullOrWhiteSpace(value14))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value14);
		}
		if (includeInventorySummary && !string.IsNullOrWhiteSpace(value8))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append((hero != null) ? "【NPC当前可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说" : "【当前商铺可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说）");
			stringBuilder.AppendLine();
			stringBuilder.Append(value8);
		}
		string value15 = (DuelSettings.GetSettings()?.PlayerCustomPromptRule ?? "").Replace("\r", "").Trim();
		stringBuilder.AppendLine();
		stringBuilder.Append("请遵循以下规则参与互动：");
		if (!string.IsNullOrWhiteSpace(value15))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(value15);
		}
		return stringBuilder.ToString().Trim();
	}

	private static bool ShouldHideSceneReputationForPrompt(NpcDataPacket npc, Hero hero)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		try
		{
			if (hero != null)
			{
				if (hero.IsWanderer)
				{
					return true;
				}
				if ((int)hero.Occupation == 20)
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
			string a = (npc?.RoleDesc ?? "").Trim();
			if (string.Equals(a, "士兵", StringComparison.Ordinal))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private unsafe static string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8)
	{
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		if (npc == null)
		{
			return "未知";
		}
		Agent val = null;
		try
		{
			Mission current = Mission.Current;
			val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex));
		}
		catch
		{
			val = null;
		}
		if (val == null || !val.IsActive())
		{
			return "未知";
		}
		bool flag = false;
		try
		{
			Mission current2 = Mission.Current;
			if (current2 != null)
			{
				flag = current2.DoesMissionRequireCivilianEquipment;
			}
			else
			{
				object obj2 = Settlement.CurrentSettlement;
				if (obj2 == null)
				{
					MobileParty mainParty = MobileParty.MainParty;
					obj2 = ((mainParty != null) ? mainParty.CurrentSettlement : null);
				}
				Settlement val2 = (Settlement)obj2;
				flag = val2 != null && !val2.IsVillage;
			}
		}
		catch
		{
			flag = false;
		}
		string text = (flag ? "常服" : "战斗装");
		try
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
			EquipmentIndex[] array3 = array2;
			for (int num = 0; num < array3.Length; num++)
			{
				EquipmentIndex val3 = array3[num];
				ItemObject val4 = null;
				try
				{
					EquipmentElement val5 = val.SpawnEquipment[val3];
					val4 = ((EquipmentElement)(ref val5)).Item;
				}
				catch
				{
					val4 = null;
				}
				if (val4 == null)
				{
					try
					{
						MissionWeapon val6 = val.Equipment[val3];
						val4 = ((MissionWeapon)(ref val6)).Item;
					}
					catch
					{
						val4 = null;
					}
				}
				if (val4 != null)
				{
					string text2 = (((MBObjectBase)val4).StringId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = ((object)(*(EquipmentIndex*)(&val3))/*cast due to .constrained prefix*/).ToString();
					}
					string value = (((object)val4.Name)?.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(value))
					{
						value = text2;
					}
					if (!dictionary.ContainsKey(text2))
					{
						dictionary[text2] = 0;
						dictionary2[text2] = value;
					}
					dictionary[text2]++;
				}
			}
			if (dictionary.Count == 0)
			{
				return text + "：未读取到可识别装备";
			}
			List<string> values = (from x in (from x in dictionary.Select(delegate(KeyValuePair<string, int> kv)
					{
						string value2;
						string name = (dictionary2.TryGetValue(kv.Key, out value2) ? value2 : kv.Key);
						return new
						{
							Name = name,
							Count = kv.Value
						};
					})
					orderby x.Count descending
					select x).ThenBy(x => x.Name, StringComparer.Ordinal).Take(Math.Max(1, maxEntries))
				select x.Name + "x" + x.Count).ToList();
			return text + "：" + string.Join("、", values);
		}
		catch
		{
			return text + "：读取装备失败";
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
			Hero value = null;
			if (npc.IsHero)
			{
				resolvedHeroes?.TryGetValue(npc.AgentIndex, out value);
			}
			string value2 = BuildSceneNpcRoleIntroForPrompt(npc, value, npcs);
			if (!string.IsNullOrWhiteSpace(value2))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(value2);
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private static string BuildPlayerSceneIdentitySentenceForPrompt(Hero playerHero)
	{
		if (playerHero == null)
		{
			return "";
		}
		try
		{
			Clan clan = playerHero.Clan;
			Kingdom val = ((clan != null) ? clan.Kingdom : null);
			if (clan != null && clan.IsUnderMercenaryService && val != null)
			{
				string text = (((object)val.Name)?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return "而且他还是" + text + "的雇佣兵。";
				}
			}
		}
		catch
		{
		}
		try
		{
			Clan clan2 = playerHero.Clan;
			Kingdom val2 = ((clan2 != null) ? clan2.Kingdom : null);
			string text2 = (((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? "").Trim();
			if (playerHero.IsFactionLeader && val2 != null && val2.Leader == playerHero && !string.IsNullOrWhiteSpace(text2))
			{
				return "而且他还是" + text2 + "的统治者。";
			}
			if (playerHero.IsLord && !playerHero.IsFactionLeader && val2 != null && !string.IsNullOrWhiteSpace(text2))
			{
				return "而且他还是" + text2 + "的封臣。";
			}
		}
		catch
		{
		}
		string text3 = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text3) || string.Equals(text3, "玩家", StringComparison.Ordinal))
		{
			text3 = (((object)playerHero.Name)?.ToString() ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "此人";
		}
		return text3 + "没有效忠于任何人。";
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
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return "";
		}
		string text = "未知文化";
		string text2 = "未知";
		string text3 = "未知";
		string value = "";
		string value2 = BuildPlayerSceneIdentitySentenceForPrompt(mainHero);
		int num = 0;
		string text4 = "无家族";
		bool flag = false;
		try
		{
			CultureObject culture = mainHero.Culture;
			string text5 = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				text = text5;
			}
		}
		catch
		{
		}
		if (!text.EndsWith("人", StringComparison.Ordinal))
		{
			text += "人";
		}
		try
		{
			text2 = MyBehavior.BuildAgeBracketLabelForExternal(mainHero.Age);
		}
		catch
		{
			text2 = "未知";
		}
		try
		{
			text3 = MyBehavior.BuildHeroEquipmentSummaryForExternal(mainHero);
		}
		catch
		{
			text3 = "未知";
		}
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					value = (RewardSystemBehavior.Instance.BuildVisibleEquipmentActualValueInlineFactForAI(mainHero) ?? "").Trim();
				}
			}
			catch
			{
			}
		}
		try
		{
			Clan clan = mainHero.Clan;
			num = ((clan != null) ? clan.Tier : 0);
			Clan clan2 = mainHero.Clan;
			string text6 = (((clan2 == null) ? null : ((object)clan2.Name)?.ToString()) ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text6))
			{
				text4 = text6;
			}
			if (!string.IsNullOrWhiteSpace(text4) && text4 != "无家族" && !text4.EndsWith("家族", StringComparison.Ordinal))
			{
				text4 += "家族";
			}
			Clan clan3 = mainHero.Clan;
			flag = ((clan3 != null) ? clan3.Leader : null) == mainHero;
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag2 = ShouldForceDetailedPlayerIntroForObserver(observerHero);
		if (num >= 2 || flag2)
		{
			string text7 = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text7) || string.Equals(text7, "玩家", StringComparison.Ordinal))
			{
				text7 = (((object)mainHero.Name)?.ToString() ?? "").Trim();
			}
			string value3 = MyBehavior.GetClanTierReputationLabelForExternal(num) + $"（{Math.Max(0, num)} level）";
			stringBuilder.Append("你面前站着一个").Append(text).Append("，你知道他叫")
				.Append(string.IsNullOrWhiteSpace(text7) ? "这人" : text7)
				.Append("，他")
				.Append(value3)
				.Append("，是")
				.Append(text4)
				.Append("的")
				.Append(flag ? "族长" : "成员")
				.Append("。从面貌上来看，是一个")
				.Append(text2)
				.Append("，穿着")
				.Append(text3)
				.Append("。");
		}
		else
		{
			stringBuilder.Append("你面前站着一个").Append(text).Append("，他看起来是个普通人，总之不是贵族，从他的面貌上来看，是一个")
				.Append(text2)
				.Append("，穿着")
				.Append(text3)
				.Append("。");
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append(value).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.Append(value2);
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
			if (presentNpc == null || presentNpc.AgentIndex == selfNpc.AgentIndex)
			{
				continue;
			}
			string sceneNpcIdentityNameForPrompt = GetSceneNpcIdentityNameForPrompt(presentNpc);
			if (!string.IsNullOrWhiteSpace(sceneNpcIdentityNameForPrompt))
			{
				if (presentNpc.IsHero)
				{
					string text = (presentNpc.RoleDesc ?? "").Trim();
					list.Add(string.IsNullOrWhiteSpace(text) ? sceneNpcIdentityNameForPrompt : (text + sceneNpcIdentityNameForPrompt));
				}
				else
				{
					list.Add(GetSceneNpcHistoryNameForPrompt(presentNpc));
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return "并且你旁边还站着" + string.Join("，", list) + "。";
	}

	private static string ConvertCountToChineseForPrompt(int count)
	{
		if (1 == 0)
		{
		}
		string result = count switch
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
			_ => count + "个", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string NormalizeFactionRelationLabelForPrompt(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return "中立";
			}
			string text = (faction.StringId ?? "").Trim();
			string b = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
			{
				return "友方";
			}
			if (faction == referenceFaction)
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
		string text = (relation ?? "").Trim();
		string text2 = text;
		if (!(text2 == "敌对"))
		{
			if (text2 == "友方")
			{
				return "友方";
			}
			return "中立";
		}
		return "敌人";
	}

	private static void ParseCurrentScenePlaceAndSpotForPrompt(out string placeName, out string spotName)
	{
		placeName = "";
		spotName = "";
		try
		{
			string text = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			int num = text.IndexOf('|');
			if (num >= 0)
			{
				text = text.Substring(0, num).Trim();
			}
			if (text.StartsWith("位于 ", StringComparison.Ordinal))
			{
				string text2 = text.Substring("位于 ".Length).Trim();
				int num2 = text2.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (num2 > 0)
				{
					placeName = text2.Substring(0, num2).Trim();
					spotName = text2.Substring(num2 + " 的 ".Length).Trim();
					return;
				}
			}
			if (text.StartsWith("靠近 ", StringComparison.Ordinal))
			{
				string text3 = text.Substring("靠近 ".Length).Trim();
				int num3 = text3.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (num3 > 0)
				{
					placeName = text3.Substring(0, num3).Trim();
					spotName = text3.Substring(num3 + " 的 ".Length).Trim();
					return;
				}
			}
			spotName = text;
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
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			CampaignVec2? val = ((mainParty != null) ? new CampaignVec2?(mainParty.Position) : ((CampaignVec2?)null));
			if (val.HasValue)
			{
				CampaignVec2 val2 = val.Value;
				if (((CampaignVec2)(ref val2)).IsValid())
				{
					val2 = val.Value;
					Vec2 val3 = ((CampaignVec2)(ref val2)).ToVec2();
					Settlement result = null;
					float num = float.MaxValue;
					foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
					{
						if (item != null && !item.IsHideout)
						{
							val2 = item.GatePosition;
							Vec2 val4 = ((CampaignVec2)(ref val2)).ToVec2();
							float num2 = val4.x - val3.x;
							float num3 = val4.y - val3.y;
							float num4 = num2 * num2 + num3 * num3;
							if (num4 < num)
							{
								num = num4;
								result = item;
							}
						}
					}
					return result;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSceneSettlementForPrompt(string placeName)
	{
		object obj = Settlement.CurrentSettlement;
		if (obj == null)
		{
			MobileParty mainParty = MobileParty.MainParty;
			obj = ((mainParty != null) ? mainParty.CurrentSettlement : null);
		}
		Settlement val = (Settlement)obj;
		if (val != null)
		{
			return val;
		}
		string text = NormalizeSettlementNameForPromptLookup(placeName);
		if (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				val = ((IEnumerable<Settlement>)Settlement.All)?.FirstOrDefault((Settlement x) => x != null && string.Equals(NormalizeSettlementNameForPromptLookup(((object)x.Name)?.ToString()), text, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				val = null;
			}
		}
		return val ?? FindNearestSettlementForPrompt();
	}

	private static string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			ParseCurrentScenePlaceAndSpotForPrompt(out var placeName, out var spotName);
			Settlement val = ResolveSceneSettlementForPrompt(placeName);
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = "当前区域";
			}
			if (string.IsNullOrWhiteSpace(spotName))
			{
				spotName = "某处";
			}
			object obj;
			if (val == null)
			{
				obj = null;
			}
			else
			{
				CultureObject culture = val.Culture;
				obj = ((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
			string text = ((string)obj).Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "未知";
			}
			object obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				Clan ownerClan = val.OwnerClan;
				if (ownerClan == null)
				{
					obj2 = null;
				}
				else
				{
					Hero leader = ownerClan.Leader;
					obj2 = ((leader == null) ? null : ((object)leader.Name)?.ToString());
				}
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			string text2 = ((string)obj2).Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "未知人物";
			}
			object obj3;
			if (val == null)
			{
				obj3 = null;
			}
			else
			{
				Clan ownerClan2 = val.OwnerClan;
				obj3 = ((ownerClan2 == null) ? null : ((object)ownerClan2.Name)?.ToString());
			}
			if (obj3 == null)
			{
				obj3 = "";
			}
			string text3 = ((string)obj3).Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "未知";
			}
			if (!text3.EndsWith("家族", StringComparison.Ordinal))
			{
				text3 += "家族";
			}
			object obj4;
			if (val == null)
			{
				obj4 = null;
			}
			else
			{
				IFaction mapFaction = val.MapFaction;
				obj4 = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
			}
			if (obj4 == null)
			{
				obj4 = "";
			}
			string text4 = ((string)obj4).Trim();
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = "未知势力";
			}
			int num;
			if (perspectiveHero != null)
			{
				object obj5;
				if (val == null)
				{
					obj5 = null;
				}
				else
				{
					Clan ownerClan3 = val.OwnerClan;
					obj5 = ((ownerClan3 != null) ? ownerClan3.Leader : null);
				}
				num = ((obj5 == perspectiveHero) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			IFaction val2 = ((val != null) ? val.MapFaction : null);
			object obj6;
			if (perspectiveHero == null)
			{
				obj6 = null;
			}
			else
			{
				Clan clan = perspectiveHero.Clan;
				obj6 = ((clan != null) ? clan.Kingdom : null);
			}
			IFaction val3 = (IFaction)obj6;
			IFaction referenceFaction = val3 ?? ((perspectiveHero != null) ? perspectiveHero.MapFaction : null) ?? val2;
			Hero mainHero = Hero.MainHero;
			object obj7;
			if (mainHero == null)
			{
				obj7 = null;
			}
			else
			{
				Clan clan2 = mainHero.Clan;
				obj7 = ((clan2 != null) ? clan2.Kingdom : null);
			}
			val3 = (IFaction)obj7;
			object obj8 = val3;
			if (obj8 == null)
			{
				Hero mainHero2 = Hero.MainHero;
				obj8 = ((mainHero2 != null) ? mainHero2.MapFaction : null);
				if (obj8 == null)
				{
					Clan playerClan = Clan.PlayerClan;
					val3 = (IFaction)(object)((playerClan != null) ? playerClan.Kingdom : null);
					obj8 = val3;
					if (obj8 == null)
					{
						Clan playerClan2 = Clan.PlayerClan;
						obj8 = ((playerClan2 != null) ? playerClan2.MapFaction : null);
					}
				}
			}
			IFaction referenceFaction2 = (IFaction)obj8;
			string relation = NormalizeFactionRelationLabelForPrompt(val2, referenceFaction);
			string text5 = NormalizeFactionRelationLabelForPrompt(val2, referenceFaction2);
			string text6 = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "玩家";
			}
			if (flag)
			{
				return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + text + "文化，由你统治，隶属于" + text4 + "，与" + text6 + "保持" + text5 + "。";
			}
			return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + text + "文化，由" + text3 + "的" + text2 + "统治，隶属于" + text4 + "，是你的" + ConvertNpcSideRelationLabelForPrompt(relation) + "，但与" + text6 + "保持" + text5 + "。";
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
			object obj = Settlement.CurrentSettlement;
			if (obj == null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				obj = ((mainParty != null) ? mainParty.CurrentSettlement : null);
			}
			Settlement val = (Settlement)obj;
			if (val == null)
			{
				return "";
			}
			string text = (ShoutUtils.GetNativeSettlementInfoForPrompt(val) ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			string text2 = (((object)val.Name)?.ToString() ?? "").Trim();
			Clan ownerClan = val.OwnerClan;
			object obj2;
			if (ownerClan == null)
			{
				obj2 = null;
			}
			else
			{
				Hero leader = ownerClan.Leader;
				obj2 = ((leader == null) ? null : ((object)leader.Name)?.ToString());
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			string text3 = ((string)obj2).Trim();
			Clan ownerClan2 = val.OwnerClan;
			string text4 = (((ownerClan2 == null) ? null : ((object)ownerClan2.Name)?.ToString()) ?? "").Trim();
			IFaction mapFaction = val.MapFaction;
			string text5 = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
			string text6 = "";
			try
			{
				IFaction mapFaction2 = val.MapFaction;
				CultureObject obj3 = ((mapFaction2 != null) ? mapFaction2.Culture : null);
				string text7 = (((obj3 != null) ? ((MBObjectBase)obj3).StringId : null) ?? "").Trim();
				Clan ownerClan3 = val.OwnerClan;
				bool? obj4;
				if (ownerClan3 == null)
				{
					obj4 = null;
				}
				else
				{
					Hero leader2 = ownerClan3.Leader;
					obj4 = ((leader2 != null) ? new bool?(leader2.IsFemale) : ((bool?)null));
				}
				bool? flag = obj4;
				if (flag == true)
				{
					text7 += "_f";
				}
				object obj5;
				if (mapFaction2 != null && mapFaction2.IsKingdomFaction)
				{
					Clan ownerClan4 = val.OwnerClan;
					if (((ownerClan4 != null) ? ownerClan4.Leader : null) != null && mapFaction2.Leader == val.OwnerClan.Leader)
					{
						obj5 = ((object)GameTexts.FindText("str_faction_ruler", text7))?.ToString();
						goto IL_022b;
					}
				}
				obj5 = ((object)GameTexts.FindText("str_faction_official", text7))?.ToString();
				goto IL_022b;
				IL_022b:
				text6 = (string)obj5;
				text6 = (text6 ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			catch
			{
				text6 = "";
			}
			List<string> list = new List<string>();
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text6) && !string.IsNullOrWhiteSpace(text3))
			{
				list.Add(text2 + "被" + text5 + "的" + text6 + "，" + text3 + "统治着。");
			}
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
			{
				list.Add(text2 + "由" + text3 + "统治。");
				list.Add(text2 + "由" + text3 + "控制。");
			}
			if (!string.IsNullOrWhiteSpace(text2) && val.OwnerClan == Clan.PlayerClan)
			{
				list.Add(text2 + "是你的封地。");
			}
			foreach (string item in list)
			{
				if (!string.IsNullOrWhiteSpace(item) && text.StartsWith(item, StringComparison.Ordinal))
				{
					text = text.Substring(item.Length).Trim();
					break;
				}
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				string text8 = "所属家族：" + text4 + "。";
				if (text.StartsWith(text8, StringComparison.Ordinal))
				{
					text = text.Substring(text8.Length).Trim();
				}
			}
			return text.Trim('。', ' ') + "。";
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
			object obj = Settlement.CurrentSettlement;
			if (obj == null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				obj = ((mainParty != null) ? mainParty.CurrentSettlement : null);
			}
			Settlement val = (Settlement)obj;
			object obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				Clan ownerClan = val.OwnerClan;
				obj2 = ((ownerClan != null) ? ownerClan.Leader : null);
			}
			Hero val2 = (Hero)obj2;
			if (val == null || val2 == null)
			{
				return "";
			}
			bool flag = false;
			try
			{
				int num;
				if (val2.CurrentSettlement != val)
				{
					MobileParty partyBelongedTo = val2.PartyBelongedTo;
					num = ((((partyBelongedTo != null) ? partyBelongedTo.CurrentSettlement : null) == val) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				flag = (byte)num != 0;
			}
			catch
			{
				flag = false;
			}
			return flag ? "当前这座城镇的统治者在该处。" : "当前这座城镇的统治者不在该处。";
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
			string targetNpcName = (msg.TargetName ?? "").Trim();
			bool useNpcNameAddress2 = false;
			if (msg.TargetAgentIndex >= 0)
			{
				useNpcNameAddress2 = msg.TargetAgentIndex != viewerAgentIndex;
			}
			else if (useNpcNameAddress && !string.IsNullOrWhiteSpace(fallbackTargetNpcName))
			{
				useNpcNameAddress2 = true;
				targetNpcName = fallbackTargetNpcName;
			}
			rendered = NormalizeScenePlayerHistoryLine(text2, targetNpcName, useNpcNameAddress2);
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
		return false;
	}

	private static string StripLeakedPromptContentForShout(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
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
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[(?:ACTION:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
	}

	private static string SanitizeSceneSpeechText(string text)
	{
		string text2 = StripLeakedPromptContentForShout(text);
		text2 = StripStageDirectionsForPassiveShout(text2);
		text2 = StripActionTagsForSceneSpeech(text2);
		text2 = Regex.Replace(text2, "\\[(?:ACTION:)?MOOD:[^\\]\\r\\n]*\\]?", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "(?:^|\\s)(?:ACTION:)?MOOD:[A-Z_]+\\]?(?=$|\\s)", " ", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:NO_CONTINUE|END)\\]", "", RegexOptions.IgnoreCase);
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

	private static bool ContainsAutoGroupNoContinueSignal(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && text.IndexOf("[NO_CONTINUE]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string StripAutoGroupStopSignal(string text)
	{
		string input = (text ?? "").Replace("\r", "");
		input = Regex.Replace(input, "\\[NO_CONTINUE\\]", "", RegexOptions.IgnoreCase);
		input = Regex.Replace(input, "\\[END\\]", "", RegexOptions.IgnoreCase);
		input = Regex.Replace(input, "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", "", RegexOptions.IgnoreCase);
		return input.Trim();
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
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (sceneHistoryLines != null)
		{
			foreach (string sceneHistoryLine in sceneHistoryLines)
			{
				string text = (sceneHistoryLine ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !IsLeakedPromptLineForShout(text) && hashSet.Add(text))
				{
					list.Add(text);
				}
			}
		}
		return (list.Count == 0) ? "【当前场景公共对话与互动】\n无" : ("【当前场景公共对话与互动】\n" + string.Join("\n", list));
	}

	private static string NormalizeScenePlayerHistoryLine(string text, string targetNpcName = "", bool useNpcNameAddress = false)
	{
		string text2 = (text ?? "").Trim();
		string text3 = GetPlayerDisplayNameForShout() + ((useNpcNameAddress && !string.IsNullOrWhiteSpace(targetNpcName)) ? ("对" + targetNpcName + "说") : "对你说");
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
			string text4 = ((num >= 0 && num + 1 < text2.Length) ? text2.Substring(num + 1).Trim() : "");
			return text3 + ": " + text4;
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
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder(persistedHeroHistory.Length);
		StringBuilder stringBuilder2 = new StringBuilder(persistedHeroHistory.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i] ?? "";
			string text2 = text.Trim();
			bool flag2 = text2.StartsWith("【", StringComparison.Ordinal) && text2.EndsWith("】", StringComparison.Ordinal);
			if (IsPrivateRecentWindowHeader(text2))
			{
				flag = true;
				stringBuilder.AppendLine(text2);
				continue;
			}
			if (flag && flag2)
			{
				flag = false;
			}
			if (flag)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					stringBuilder.AppendLine(text2);
				}
			}
			else if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder2.AppendLine(text);
			}
		}
		privateRecentWindowSection = stringBuilder.ToString().Trim();
		persistedWithoutRecentWindow = stringBuilder2.ToString().Trim();
	}

	private static string BuildSceneFirstMeetingNpcFactSection(Hero hero)
	{
		string firstMeetingNpcFactTextForPromptIfNeeded = MyBehavior.GetFirstMeetingNpcFactTextForPromptIfNeeded(hero);
		if (string.IsNullOrWhiteSpace(firstMeetingNpcFactTextForPromptIfNeeded))
		{
			return "";
		}
		return "【AFEF NPC行为补充】\n" + firstMeetingNpcFactTextForPromptIfNeeded.Trim();
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
			Hero val = null;
			try
			{
				if (liveAgent != null)
				{
					BasicCharacterObject character = liveAgent.Character;
					CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
					if (val2 != null && val2.HeroObject != null)
					{
						val = val2.HeroObject;
					}
				}
			}
			catch
			{
			}
			if (val == null)
			{
				try
				{
					val = ResolveHeroFromAgentIndex(npc.AgentIndex);
				}
				catch
				{
				}
			}
			return MyBehavior.BuildScenePatienceBadgeForHeroExternal(val);
		}
		return MyBehavior.BuildScenePatienceBadgeForUnnamedExternal(npc.UnnamedKey, npc.Name);
	}

	public static void TrySystemNpcShout(Agent speakerAgent, string content)
	{
		try
		{
			if (speakerAgent != null && !string.IsNullOrWhiteSpace(content))
			{
				Campaign current = Campaign.Current;
				((current != null) ? current.GetCampaignBehavior<ShoutBehavior>() : null)?.EnqueueSystemNpcShout(speakerAgent, content);
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
					Mission current = Mission.Current;
					Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == speakerData.AgentIndex));
					string aiResponse = safeContent;
					bool escalatedToBattle = false;
					bool flag = false;
					bool escalatedToFight = false;
					try
					{
						if (val == null)
						{
							goto IL_01ab;
						}
						BasicCharacterObject character = val.Character;
						CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
						if (val2 == null || val2.HeroObject == null)
						{
							goto IL_01ab;
						}
						MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(val2.HeroObject, ref aiResponse);
						DuelBehavior.TryCacheDuelAfterLinesFromText(val2.HeroObject, ref aiResponse);
						DuelBehavior.TryCacheDuelStakeFromText(val2.HeroObject, ref aiResponse);
						VanillaIssueOfferBridge.ApplyIssueOfferTags(val2.HeroObject, ref aiResponse);
						if (RewardSystemBehavior.Instance != null)
						{
							RewardSystemBehavior.Instance.ApplyRewardTags(val2.HeroObject, Hero.MainHero, ref aiResponse);
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
							RomanceSystemBehavior.Instance.ApplyMarriageTags(val2.HeroObject, Hero.MainHero, ref aiResponse);
						}
						if (!ShouldSuppressSceneConversationControlForMeeting())
						{
							LordEncounterBehavior.TryProcessMeetingTauntAction(val2.HeroObject, ref aiResponse, out escalatedToBattle);
						}
						else
						{
							StripMeetingTauntTagsForSceneConversation(ref aiResponse);
						}
						flag = SceneTauntBehavior.TryProcessSceneTauntAction(val2.HeroObject, val2, speakerData.AgentIndex, ref aiResponse, out escalatedToFight);
						goto end_IL_003a;
						IL_01ab:
						MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(speakerData.UnnamedKey, speakerData.Name, ref aiResponse);
						if (val != null)
						{
							BasicCharacterObject character2 = val.Character;
							CharacterObject val3 = (CharacterObject)(object)((character2 is CharacterObject) ? character2 : null);
							if (val3 != null && RewardSystemBehavior.Instance != null)
							{
								RewardSystemBehavior.Instance.ApplyMerchantRewardTags(val3, Hero.MainHero, ref aiResponse);
								List<string> list3 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
								if (list3 != null)
								{
									foreach (string item2 in list3)
									{
										RecordSystemFactForNearbySafe(allNpcData, item2);
									}
								}
							}
						}
						if (val != null)
						{
							BasicCharacterObject character3 = val.Character;
							CharacterObject val4 = (CharacterObject)(object)((character3 is CharacterObject) ? character3 : null);
							if (val4 != null)
							{
								flag = SceneTauntBehavior.TryProcessSceneTauntAction(val4.HeroObject, val4, speakerData.AgentIndex, ref aiResponse, out escalatedToFight);
							}
						}
						end_IL_003a:;
					}
					catch
					{
					}
					if (flag && string.IsNullOrWhiteSpace(aiResponse))
					{
						aiResponse = BuildFallbackSceneTauntSpeech(escalatedToFight);
					}
					bool flag2 = false;
					try
					{
						flag2 = !escalatedToBattle && !escalatedToFight && ShoutUtils.TryTriggerDuelAction(speakerData, ref aiResponse);
					}
					catch
					{
					}
					if (!string.IsNullOrWhiteSpace(aiResponse))
					{
						ShowNpcSpeechOutput(speakerData, val, aiResponse);
						if (val != null && val.IsActive())
						{
							AddAgentToStareList(val);
						}
						RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);
						PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);
					}
					if (flag2)
					{
						ReleaseSceneConversationConstraints(allNpcData, speakerData.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
						DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
						ShoutUtils.ExecuteDuel(val);
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
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionStarted);
	}

	public override void SyncData(IDataStore dataStore)
	{
		try
		{
			if (dataStore == null)
			{
				return;
			}
			Dictionary<string, string> stored = (dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
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
				stored = CampaignSaveChunkHelper.FlattenStringDictionary(_sceneHeroRevisitDayStorage);
				dataStore.SyncData<Dictionary<string, string>>("_sceneHeroRevisitDays_v1", ref stored);
				return;
			}
			dataStore.SyncData<Dictionary<string, string>>("_sceneHeroRevisitDays_v1", ref stored);
			_sceneHeroRevisitDayStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored, "SceneHeroRevisit");
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
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Expected O, but got Unknown
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
		lock (_autoGroupChatLock)
		{
			_autoGroupChatSession = null;
			_autoGroupChatLoopRunning = false;
		}
		_sceneConversationEpoch = 0;
		Action result;
		while (_mainThreadActions.TryDequeue(out result))
		{
		}
		Interlocked.Increment(ref _sceneHistorySessionId);
		RagWarmupCoordinator.TryStartBackgroundWarmup("mission_start");
		AIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start");
		Mission.Current.AddMissionBehavior((MissionBehavior)(object)new ShoutMissionBehavior(this));
		Mission.Current.AddMissionBehavior((MissionBehavior)(object)new FloatingTextMissionView());
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
			InformationManager.DisplayMessage(new InformationMessage("按（" + text + "）与NPC交流", new Color(0.95f, 0.85f, 0.2f, 1f)));
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
						LogTtsReport("AudioFileReady", agentIndex, $"duration={durationSecs:F2};wav={Path.GetFileName(wavPath)};xml={Path.GetFileName(xmlPath)}");
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
							//IL_026c: Unknown result type (might be due to invalid IL or missing references)
							try
							{
								LogTtsReport("AudioFileReady.MainThreadStart", agentIndex, $"wavExists={!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath)};xmlExists={!string.IsNullOrWhiteSpace(xmlPath) && File.Exists(xmlPath)}");
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
								if (_enableRhubarbSoundEventPlayback && Mission.Current != null && !((NativeObject)(object)Mission.Current.Scene == (NativeObject)null))
								{
									Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
									if (val != null && val.IsActive())
									{
										CleanupPreviousLipSyncPlaybackForReplacement("OnAudioFileReady.ReplaceExisting");
										LogLipSyncNativeProbe("CreateEventFromExternalFile.Before", agentIndex, "wav=" + Path.GetFileName(wavPath));
										SoundEvent val2 = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", wavPath, Mission.Current.Scene, false, false);
										if (val2 == null)
										{
											Logger.Log("LipSync", $"[WARN] SoundEvent.CreateEventFromExternalFile 返回 null, agentIndex={agentIndex}");
											LogTtsReport("AudioFileReady.CreateSoundEventNull", agentIndex, "wav=" + Path.GetFileName(wavPath));
											QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.CreateSoundEventNull", agentIndex);
										}
										else
										{
											LogTtsReport("AudioFileReady.SoundEventCreated", agentIndex, "wav=" + Path.GetFileName(wavPath));
											LogLipSyncNativeProbe("SoundEvent.SetPosition.Before", agentIndex);
											val2.SetPosition(val.Position);
											LogLipSyncNativeProbe("SoundEvent.SetPosition.After", agentIndex);
											LogLipSyncNativeProbe("SoundEvent.Play.Before", agentIndex);
											val2.Play();
											LogLipSyncNativeProbe("SoundEvent.Play.After", agentIndex);
											float num = 0f;
											bool flag2 = true;
											try
											{
												flag2 = DuelSettings.GetSettings()?.TtsSceneUseWinmmAudible ?? true;
												num = DuelSettings.GetSettings()?.TtsLipSyncSoundEventVolume ?? 0f;
											}
											catch
											{
												flag2 = true;
												num = 0f;
											}
											if (flag2)
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
												val2.SetParameter("Volume", num);
											}
											catch (Exception ex2)
											{
												Logger.Log("LipSync", "[WARN] 设置 SoundEvent 音量失败(Volume): " + ex2.Message);
											}
											LogLipSyncNativeProbe("SoundEvent.GetSoundId.Before", agentIndex);
											int soundId = val2.GetSoundId();
											LogLipSyncNativeProbe("SoundEvent.GetSoundId.After", agentIndex, "soundId=" + soundId);
											if (soundId <= 0)
											{
												Logger.Log("LipSync", $"[WARN] SoundEvent.GetSoundId 非法({soundId})，跳过 StartRhubarbRecord, agentIndex={agentIndex}");
												SafeStopAndReleaseSoundEvent(val2);
												QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.InvalidSoundId", agentIndex);
											}
											else
											{
												string reason = "";
												bool flag3 = CanAgentUseSceneLipSync(val, out reason);
												Logger.Log("LipSync", $"[Rhubarb] SoundEvent created, vol={num:F2}, agentIndex={agentIndex}, soundId={soundId}, lipSyncSafe={flag3}, reason={reason}");
												lock (_speakingLock)
												{
													if (flag3)
													{
														_agentLipSyncDetachedForSafety.Remove(agentIndex);
													}
													else
													{
														_agentLipSyncDetachedForSafety.Add(agentIndex);
													}
													_agentSoundEvents[agentIndex] = val2;
													_agentWavPaths[agentIndex] = wavPath;
													_agentXmlPaths[agentIndex] = xmlPath;
												}
												if (flag3)
												{
													PrepareAgentForTrueLipSyncIfPossible(val);
													LogLipSyncNativeProbe("StartRhubarbRecord.Before", agentIndex, $"soundId={soundId};xml={Path.GetFileName(xmlPath)}");
													val.AgentVisuals.StartRhubarbRecord(xmlPath, soundId);
													LogLipSyncNativeProbe("StartRhubarbRecord.After", agentIndex, "soundId=" + soundId);
													Logger.Log("LipSync", $"[Rhubarb] StartRhubarbRecord 调用成功, agentIndex={agentIndex}, soundId={soundId}");
												}
												else
												{
													Logger.Log("LipSync", $"[SAFEGUARD] Skip StartRhubarbRecord for unsafe scene agent. agentIndex={agentIndex}, reason={reason}");
													LogTtsReport("AudioFileReady.LipSyncDetached", agentIndex, "reason=" + reason);
												}
											}
										}
									}
									else
									{
										LogTtsReport("AudioFileReady.AgentUnavailable", agentIndex, $"agentMissing={val == null};active={val != null && val.IsActive()}");
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
						}
						else
						{
							string reason = "";
							bool flag = CanAgentUseSceneLipSyncExternal(agentIndex, out reason);
							lock (_speakingLock)
							{
								if (flag)
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
							Logger.Log("LipSync", $"[OnPlaybackStarted] agentIndex={agentIndex}, lipSyncSafe={flag}, reason={reason}");
							LogTtsReport("PlaybackStarted", agentIndex, $"lipSyncSafe={flag};reason={reason}");
							lock (_ttsBubbleSyncLock)
							{
								_ttsPlaybackStartedAgents.Add(agentIndex);
							}
							_mainThreadActions.Enqueue(delegate
							{
								try
								{
									if (!TryDispatchPendingNpcBubbleForTts(agentIndex, allowFallbackDuration: false))
									{
										SchedulePendingNpcBubbleFallbackDispatch(agentIndex);
									}
								}
								catch (Exception ex2)
								{
									Logger.Log("LipSync", $"[ERROR] PlaybackStarted bubble dispatch failed, agentIndex={agentIndex}, error={ex2.Message}");
									LogTtsReport("PlaybackStarted.BubbleDispatchFailed", agentIndex, "error=" + ex2.Message);
									BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackStarted.BubbleDispatch", ex2, "agentIndex=" + agentIndex);
								}
							});
						}
					}
				};
				_ttsOnPlaybackFinishedHandler = delegate(int agentIndex)
				{
					if (agentIndex >= 0)
					{
						long interactionToken = 0L;
						bool flag = TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);
						Mission current = Mission.Current;
						Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex));
						bool flag2 = IsAgentHostileToMainAgent(agent);
						if (flag2)
						{
							flag = false;
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
						LogTtsReport("PlaybackFinished", agentIndex, $"hasInteractionToken={flag};interactionToken={interactionToken};hostileFinishedSpeech={flag2}");
						if (flag)
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
								bool flag3 = false;
								bool flag4 = false;
								bool flag5 = false;
								bool flag6 = false;
								SoundEvent value = null;
								string value2 = null;
								string value3 = null;
								lock (_speakingLock)
								{
									flag3 = _agentSoundEvents.ContainsKey(agentIndex);
									flag4 = _agentWavPaths.ContainsKey(agentIndex);
									flag5 = _agentXmlPaths.ContainsKey(agentIndex);
									flag6 = _agentLipSyncDetachedForSafety.Remove(agentIndex);
									if (flag6)
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
								if (flag6)
								{
									QueueDeferredCleanup(value, value2, value3, "PlaybackFinished.DetachedSafety", agentIndex);
									Logger.Log("LipSync", $"[SAFEGUARD] queued detached lipsync cleanup after playback finished, agentIndex={agentIndex}, hasSe={value != null}, hasWav={!string.IsNullOrWhiteSpace(value2)}, hasXml={!string.IsNullOrWhiteSpace(value3)}");
									LogTtsReport("PlaybackFinished.DetachedCleanupQueued", agentIndex, $"hasSe={value != null};hasWav={!string.IsNullOrWhiteSpace(value2)};hasXml={!string.IsNullOrWhiteSpace(value3)}");
								}
								else
								{
									Logger.Log("LipSync", $"[Rhubarb] keep native lipsync state after playback finished, agentIndex={agentIndex}, hasSe={flag3}, hasWav={flag4}, hasXml={flag5}");
									LogTtsReport("PlaybackFinished.NoImmediateCleanup", agentIndex, $"hasSe={flag3};hasWav={flag4};hasXml={flag5}");
									LogLipSyncNativeProbe("PlaybackFinished.NoImmediateCleanup", agentIndex, $"hasSe={flag3};hasWav={flag4};hasXml={flag5}");
								}
								LogTtsReport("PlaybackFinished.MainThreadCleanupEnd", agentIndex);
							}
							catch (Exception ex2)
							{
								Logger.Log("LipSync", $"[ERROR] PlaybackFinished main-thread cleanup failed, agentIndex={agentIndex}, error={ex2.Message}");
								LogTtsReport("PlaybackFinished.MainThreadCleanupFailed", agentIndex, "error=" + ex2.Message);
								BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackFinished.MainThreadCleanup", ex2, "agentIndex=" + agentIndex);
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
				Logger.Log("LipSync", $"[INFO] TTS 播放事件订阅完成 owner={((object)this).GetHashCode()}");
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
			return Mission.Current != null && (NativeObject)(object)Mission.Current.Scene != (NativeObject)null;
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
						SoundEvent value = agentSoundEvent.Value;
						if (value != null)
						{
							value.Pause();
						}
					}
					catch
					{
					}
				}
				return;
			}
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
					SoundEvent value2 = agentSoundEvent2.Value;
					if (value2 != null)
					{
						value2.Resume();
					}
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
		if (!_ttsPausedByInterruption || !HasInterruptionDebounceElapsed() || !IsGameWindowFocused())
		{
			return;
		}
		float num = 1f;
		try
		{
			Mission current = Mission.Current;
			float? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				Scene scene = current.Scene;
				obj = ((scene != null) ? new float?(scene.TimeSpeed) : ((float?)null));
			}
			num = obj ?? 1f;
		}
		catch
		{
			num = 1f;
		}
		if (!(num <= 0.001f))
		{
			_ttsPausedByInterruption = false;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[INTERRUPT] TTS resumed after interruption");
		}
	}

	private void QueueDeferredCleanup(SoundEvent se, string wavPath, string xmlPath, string source = "Unknown", int agentIndex = -1)
	{
		if (se != null || !string.IsNullOrWhiteSpace(wavPath) || !string.IsNullOrWhiteSpace(xmlPath))
		{
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
			int num = 0;
			lock (_speakingLock)
			{
				_deferredCleanupQueue.Add(deferredCleanupItem);
				num = _deferredCleanupQueue.Count;
			}
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
			Logger.Log("LipSync", $"[DeferredCleanup][ENQUEUE] id={deferredCleanupItem.ItemId}, src={deferredCleanupItem.Source}, agent={deferredCleanupItem.AgentIndex}, hasSe={deferredCleanupItem.SoundEvent != null}, hasWav={!string.IsNullOrWhiteSpace(deferredCleanupItem.WavPath)}, hasXml={!string.IsNullOrWhiteSpace(deferredCleanupItem.XmlPath)}, queue={num}");
		}
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
			Mission current = Mission.Current;
			float? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				Scene scene = current.Scene;
				obj = ((scene != null) ? new float?(scene.TimeSpeed) : ((float?)null));
			}
			num = obj ?? 1f;
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
				for (int num2 = _deferredCleanupQueue.Count - 1; num2 >= 0; num2--)
				{
					DeferredCleanupItem deferredCleanupItem = _deferredCleanupQueue[num2];
					if (deferredCleanupItem == null)
					{
						_deferredCleanupQueue.RemoveAt(num2);
					}
					else if (new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem.EnqueuedUtcTicks)).TotalSeconds > 20.0)
					{
						list.Add(deferredCleanupItem);
						_deferredCleanupQueue.RemoveAt(num2);
					}
				}
				num = _deferredCleanupQueue.Count;
			}
		}
		if (list.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				DeferredCleanupItem deferredCleanupItem2 = list[i];
				TimeSpan timeSpan = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem2.EnqueuedUtcTicks));
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
				Logger.Log("LipSync", $"[DeferredCleanup][TIMEOUT_DROP] id={deferredCleanupItem2.ItemId}, src={deferredCleanupItem2.Source}, agent={deferredCleanupItem2.AgentIndex}, ageMs={(int)timeSpan.TotalMilliseconds}");
			}
		}
		if (num <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
				return;
			}
			catch
			{
				return;
			}
		}
		if (!IsDeferredCleanupWindowStable())
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
				return;
			}
			catch
			{
				return;
			}
		}
		long num3 = 0L;
		try
		{
			num3 = Interlocked.Read(ref _deferredCleanupStableSinceUtcTicks);
		}
		catch
		{
			num3 = 0L;
		}
		if (num3 <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, ticks);
				return;
			}
			catch
			{
				return;
			}
		}
		if (new TimeSpan(ticks - num3).TotalSeconds < 1.5)
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
			for (int j = 0; j < _deferredCleanupQueue.Count; j++)
			{
				DeferredCleanupItem deferredCleanupItem3 = _deferredCleanupQueue[j];
				if (deferredCleanupItem3 != null && !(new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem3.EnqueuedUtcTicks)).TotalSeconds < 0.5))
				{
					list2.Add(deferredCleanupItem3);
					if (list2.Count >= 8)
					{
						break;
					}
				}
			}
			if (list2.Count > 0)
			{
				for (int k = 0; k < list2.Count; k++)
				{
					_deferredCleanupQueue.Remove(list2[k]);
				}
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		for (int l = 0; l < list2.Count; l++)
		{
			DeferredCleanupItem deferredCleanupItem4 = list2[l];
			TimeSpan timeSpan2 = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem4.EnqueuedUtcTicks));
			try
			{
				SafeStopAndReleaseSoundEvent(deferredCleanupItem4.SoundEvent);
			}
			catch (Exception exception)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.DeferredCleanup.SoundEvent", exception, $"agentIndex={deferredCleanupItem4.AgentIndex};itemId={deferredCleanupItem4.ItemId};source={deferredCleanupItem4.Source}");
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
			Logger.Log("LipSync", $"[DeferredCleanup][PROCESS] id={deferredCleanupItem4.ItemId}, src={deferredCleanupItem4.Source}, agent={deferredCleanupItem4.AgentIndex}, ageMs={(int)timeSpan2.TotalMilliseconds}");
		}
		int num4 = 0;
		lock (_speakingLock)
		{
			num4 = _deferredCleanupQueue.Count;
		}
		Logger.Log("LipSync", $"[DeferredCleanup][SUMMARY] processed={list2.Count}, timeoutDropped={list.Count}, remaining={num4}");
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
			Logger.Log("LipSync", "[" + reason + "] TTS paused by interruption");
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
		if (HasLipSyncOrSceneSpeechWork())
		{
			ClearQueuedSceneSpeech();
			Logger.Log("LipSync", "[" + reason + "] critical UI transition detected, forcing native lipsync teardown");
			LogLipSyncNativeProbe("CriticalUiTransition.StopAll.Before", -1, reason);
			StopAllLipSyncPlaybackAndCleanup();
			LogLipSyncNativeProbe("CriticalUiTransition.StopAll.After", -1, reason);
		}
	}

	private static void SafeStopAndReleaseSoundEvent(SoundEvent se)
	{
		if (se == null)
		{
			Logger.Log("LipSync", "[SoundEvent] SafeStopAndRelease skipped: null");
			return;
		}
		int num = -1;
		try
		{
			num = se.GetSoundId();
		}
		catch
		{
			num = -1;
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease start, soundId={num}");
		LogLipSyncNativeProbe("SafeStopAndRelease.Enter", -1, "soundId=" + num);
		bool flag = false;
		try
		{
			flag = se.IsValid;
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[SoundEvent] IsValid probe failed, soundId={num}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.IsValid", ex, "soundId=" + num);
			return;
		}
		if (!flag)
		{
			Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease skipped: invalid soundId={num}");
			LogLipSyncNativeProbe("SafeStopAndRelease.SkipInvalid", -1, "soundId=" + num);
			return;
		}
		try
		{
			LogLipSyncNativeProbe("SoundEvent.Stop.Before", -1, "soundId=" + num);
			se.Stop();
			LogLipSyncNativeProbe("SoundEvent.Stop.After", -1, "soundId=" + num);
		}
		catch (Exception ex2)
		{
			Logger.Log("LipSync", $"[SoundEvent] Stop failed, soundId={num}, error={ex2.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.Stop", ex2, "soundId=" + num);
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease end, soundId={num}");
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
			_floatingTextView?.SetTypingPaused(paused: false);
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
			foreach (DeferredCleanupItem item in _deferredCleanupQueue)
			{
				try
				{
					SafeStopAndReleaseSoundEvent(item?.SoundEvent);
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(item?.WavPath) && File.Exists(item.WavPath))
					{
						File.Delete(item.WavPath);
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(item?.XmlPath) && File.Exists(item.XmlPath))
					{
						File.Delete(item.XmlPath);
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
				Agent val = ((IEnumerable<Agent>)agents).FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
				if (val == null || !val.IsActive())
				{
					LogTtsReport("AgentInvalidDuringLipSync", agentIndex);
					CancelAgentSpeechForRemoval(agentIndex, "agent_invalid_during_lipsync");
					continue;
				}
				string reason = "";
				if (!CanAgentUseSceneLipSync(val, out reason))
				{
					DetachAgentLipSyncForSafety(agentIndex, reason);
				}
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
				if (_stareTimer >= 25f && IsCooldownReady(closestFacingAgent))
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
			if (_stareTargetLostGraceTimer >= 2f)
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
			object obj;
			if (agent == null)
			{
				obj = null;
			}
			else
			{
				IAgentOriginBase origin = agent.Origin;
				obj = ((origin != null) ? origin.BattleCombatant : null);
			}
			PartyBase val = (PartyBase)((obj is PartyBase) ? obj : null);
			object obj2;
			if (val != null)
			{
				IFaction mapFaction = val.MapFaction;
				obj2 = ((mapFaction != null) ? mapFaction.StringId : null);
			}
			else
			{
				obj2 = null;
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			text = ((string)obj2).Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				object obj3;
				if (currentSettlement == null)
				{
					obj3 = null;
				}
				else
				{
					Clan ownerClan = currentSettlement.OwnerClan;
					if (ownerClan == null)
					{
						obj3 = null;
					}
					else
					{
						Kingdom kingdom = ownerClan.Kingdom;
						obj3 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
				}
				if (obj3 == null)
				{
					if (currentSettlement == null)
					{
						obj3 = null;
					}
					else
					{
						IFaction mapFaction2 = currentSettlement.MapFaction;
						obj3 = ((mapFaction2 != null) ? mapFaction2.StringId : null);
					}
					if (obj3 == null)
					{
						obj3 = "";
					}
				}
				text = ((string)obj3).Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					string[] obj4 = new string[6]
					{
						"kingdomIdOverride_from_settlement settlement=",
						(currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null,
						" mapFaction=",
						null,
						null,
						null
					};
					object obj5;
					if (currentSettlement == null)
					{
						obj5 = null;
					}
					else
					{
						IFaction mapFaction3 = currentSettlement.MapFaction;
						obj5 = ((mapFaction3 != null) ? mapFaction3.StringId : null);
					}
					obj4[3] = (string)obj5;
					obj4[4] = " -> ";
					obj4[5] = text;
					Logger.Log("LoreMatch", string.Concat(obj4));
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
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = ((character != null) ? "character" : "invalid");
			string text2 = ((agent != null) ? agent.Index.ToString() : "-1");
			string text3 = (((character != null) ? ((MBObjectBase)character).StringId : null) ?? "").Trim();
			object obj;
			if (character == null)
			{
				obj = null;
			}
			else
			{
				CultureObject culture = character.Culture;
				obj = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
			}
			if (obj == null)
			{
				obj = "neutral";
			}
			string text4 = ((string)obj).Trim().ToLowerInvariant();
			string text5 = "commoner";
			try
			{
				if (character != null)
				{
					text5 = (((BasicCharacterObject)character).IsSoldier ? "soldier" : ((object)character.Occupation/*cast due to .constrained prefix*/).ToString().Trim().ToLowerInvariant());
				}
			}
			catch
			{
				text5 = "commoner";
			}
			string text6 = (kingdomIdOverride ?? "").Trim().ToLowerInvariant();
			string text7 = DateTime.UtcNow.Ticks + "_" + text2;
			string text8 = (secondaryInput ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (text8.Length > 72)
			{
				text8 = text8.Substring(0, 72);
			}
			Logger.Log("LoreMatch", string.Format("shout_lore_prequery phase={0} traceId={1} source={2} agentIndex={3} charId={4} culture={5} kingdomOverride={6} role={7} inputLen={8} npcRecall={9} secondaryLen={10}", phase, text7, text, text2, text3, text4, text6, text5, (inputText ?? "").Length, string.IsNullOrWhiteSpace(text8) ? "off" : "on", text8.Length));
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
				Hero val = ResolveHeroFromAgentIndex(data.AgentIndex);
				string text = (((val != null) ? ((MBObjectBase)val).StringId : null) ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return "hero:" + text;
				}
			}
			string text2 = (data.UnnamedKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				int num = data.AgentIndex;
				if (num < 0)
				{
					num = fallbackAgentIndex;
				}
				if (num >= 0)
				{
					return "unnamed:" + text2 + "|agent:" + num;
				}
				return "unnamed:" + text2;
			}
			string text3 = (data.TroopId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				int num2 = data.AgentIndex;
				if (num2 < 0)
				{
					num2 = fallbackAgentIndex;
				}
				if (num2 >= 0)
				{
					return "troop:" + text3 + "|agent:" + num2;
				}
				return "troop:" + text3;
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
			BasicCharacterObject character = agent.Character;
			CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val != null && val.HeroObject != null)
			{
				Hero heroObject = val.HeroObject;
				string text = (((heroObject != null) ? ((MBObjectBase)heroObject).StringId : null) ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return "hero:" + text;
				}
			}
		}
		catch
		{
		}
		NpcDataPacket npcDataPacket = null;
		try
		{
			npcDataPacket = ShoutUtils.ExtractNpcData(agent);
		}
		catch
		{
			npcDataPacket = null;
		}
		return GetCooldownIdentityKey(npcDataPacket, agent.Index);
	}

	private void ApplyInteractionGraceAndGroupCooldown(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget = null, IEnumerable<NpcDataPacket> participantsData = null)
	{
		_interactionGraceTimer = Math.Max(_interactionGraceTimer, graceSeconds);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (participantsData != null)
		{
			foreach (NpcDataPacket participantsDatum in participantsData)
			{
				string cooldownIdentityKey = GetCooldownIdentityKey(participantsDatum, participantsDatum?.AgentIndex ?? (-1));
				if (!string.IsNullOrWhiteSpace(cooldownIdentityKey))
				{
					hashSet.Add(cooldownIdentityKey);
				}
			}
		}
		if (participants != null)
		{
			foreach (Agent participant in participants)
			{
				string cooldownIdentityKey2 = GetCooldownIdentityKey(participant);
				if (!string.IsNullOrWhiteSpace(cooldownIdentityKey2))
				{
					hashSet.Add(cooldownIdentityKey2);
				}
			}
		}
		if (extraTarget != null)
		{
			string cooldownIdentityKey3 = GetCooldownIdentityKey(extraTarget);
			if (!string.IsNullOrWhiteSpace(cooldownIdentityKey3))
			{
				hashSet.Add(cooldownIdentityKey3);
			}
		}
		foreach (string item in hashSet)
		{
			if (_passiveCooldowns.TryGetValue(item, out var value))
			{
				_passiveCooldowns[item] = Math.Max(value, cooldownSeconds);
			}
			else
			{
				_passiveCooldowns[item] = cooldownSeconds;
			}
		}
	}

	private bool IsCooldownReady(Agent agent)
	{
		if (agent == null)
		{
			return false;
		}
		string cooldownIdentityKey = GetCooldownIdentityKey(agent);
		if (string.IsNullOrWhiteSpace(cooldownIdentityKey))
		{
			return false;
		}
		return !_passiveCooldowns.ContainsKey(cooldownIdentityKey);
	}

	private List<Agent> GetPassiveCooldownGroupAgents(Agent targetAgent)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		List<Agent> list = new List<Agent>();
		if (targetAgent == null || !targetAgent.IsActive() || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return list;
		}
		Vec3 position = Agent.Main.Position;
		Vec3 lookDirection = Agent.Main.LookDirection;
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (item == null || item == Agent.Main || !item.IsActive() || !item.IsHuman)
			{
				continue;
			}
			Vec3 position2 = item.Position;
			float num = ((Vec3)(ref position2)).Distance(targetAgent.Position);
			if (num > 7f)
			{
				continue;
			}
			if (item == targetAgent || num <= 3f)
			{
				list.Add(item);
				continue;
			}
			Vec3 val = item.Position - position;
			((Vec3)(ref val)).Normalize();
			if (Vec3.DotProduct(lookDirection, val) > 0.866f)
			{
				list.Add(item);
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
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
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
		string currentSceneDescription = ShoutUtils.GetCurrentSceneDescription();
		List<Agent> passiveCooldownGroupAgents = GetPassiveCooldownGroupAgents(targetAgent);
		List<NpcDataPacket> list = (from a in passiveCooldownGroupAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		if (!list.Any((NpcDataPacket d) => d != null && d.AgentIndex == npcData.AgentIndex))
		{
			list.Add(npcData);
		}
		ApplyInteractionGraceAndGroupCooldown(0.75f, 10f, passiveCooldownGroupAgents, targetAgent, list);
		InformationManager.DisplayMessage(new InformationMessage("你盯着 " + npcData.Name + " 看了很久...", new Color(0.7f, 0.7f, 0.7f, 1f)));
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
			bool opponentContext = IsPassiveReactionOpponentContext();
			return armed ? BuildCombatPassiveArmedFactText(targetAgent, opponentContext) : BuildCombatPassiveBrawlFactText(opponentContext);
		}
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return text + "看着你";
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
					Mission current = Mission.Current;
					MissionFightHandler val = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
					flag = val != null && val.IsThereActiveFight();
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
			ICampaignMission current = CampaignMission.Current;
			object obj2;
			if (current == null)
			{
				obj2 = null;
			}
			else
			{
				Location location = current.Location;
				obj2 = ((location != null) ? location.StringId : null);
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			string text = ((string)obj2).Trim().ToLowerInvariant();
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
			Mission current2 = Mission.Current;
			string text2 = (((current2 != null) ? current2.SceneName : null) ?? "").Trim().ToLowerInvariant();
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
				string text = ((object)missionBehavior)?.GetType()?.Name;
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
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你和" + text + "只是对手，正在赤手较量") : ("[AFEF NPC行为补充] 你和" + text + "互为敌人，正在互相殴打");
	}

	private static string BuildCombatPassiveArmedFactText(Agent targetAgent, bool opponentContext)
	{
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string text2 = TryGetActiveWeaponDisplayNameForPassiveReaction(targetAgent);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "武器";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你现在拿着" + text2 + "与" + text + "作为对手激烈交锋，但还未决出胜负") : ("[AFEF NPC行为补充] 你现在正拿着" + text2 + "与" + text + "作为敌人拼杀，还未决出胜负");
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
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
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
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				if (IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[val]))
				{
					MissionWeapon val2 = agent.Equipment[val];
					ItemObject item = ((MissionWeapon)(ref val2)).Item;
					string text = ((item == null) ? null : ((object)item.Name)?.ToString());
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text.Trim();
					}
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
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		reason = "unknown";
		try
		{
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				reason = "agent_not_participating";
				return false;
			}
			Mission current = Mission.Current;
			if ((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null)
			{
				reason = "mission_scene_unavailable";
				return false;
			}
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				reason = "main_agent_unavailable";
				return false;
			}
			if ((NativeObject)(object)agent.AgentVisuals == (NativeObject)null)
			{
				reason = "agent_visuals_missing";
				return false;
			}
			Vec3 position = agent.Position;
			Vec2 asVec = ((Vec3)(ref position)).AsVec2;
			position = Agent.Main.Position;
			float num = ((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2);
			if (float.IsNaN(num) || float.IsInfinity(num))
			{
				reason = "distance_invalid";
				return false;
			}
			if (num > 42.25f)
			{
				reason = $"distance={Math.Sqrt(num):0.00}>{6.5f:0.0}";
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
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			if (val == null)
			{
				reason = "agent_missing";
				return false;
			}
			return CanAgentUseSceneLipSync(val, out reason);
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex, out string weaponName)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
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
				MissionWeapon val = agent.Equipment[equipmentIndex];
				ItemObject item = ((MissionWeapon)(ref val)).Item;
				text = ((item == null) ? null : ((object)item.Name)?.ToString());
			}
			catch
			{
				text = null;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				try
				{
					EquipmentElement val2 = agent.SpawnEquipment[equipmentIndex];
					ItemObject item2 = ((EquipmentElement)(ref val2)).Item;
					text = ((item2 == null) ? null : ((object)item2.Name)?.ToString());
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
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent == null || (int)equipmentIndex == -1 || (int)equipmentIndex < 0 || (int)equipmentIndex >= 5)
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
			WeaponComponentData currentUsageItem = ((MissionWeapon)(ref missionWeapon)).CurrentUsageItem;
			return currentUsageItem != null && !currentUsageItem.IsShield;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponEquipmentElementForPassiveReaction(EquipmentElement equipmentElement)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		try
		{
			ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
			if (item == null)
			{
				return false;
			}
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return primaryWeapon != null && !primaryWeapon.IsShield && (int)item.Type != 8;
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
		string text2 = string.Join("、", (from name in (allNpcData ?? new List<NpcDataPacket>()).Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0).Select(GetSceneNpcHistoryNameForPrompt)
			where !string.IsNullOrWhiteSpace(name) && !string.Equals(name.Trim(), text, StringComparison.Ordinal)
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct().Take(5));
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "周围的人";
		}
		return text + "现在正在和" + text2 + "继续聊天，玩家暂时没有插话。请只以" + text + "的身份，自然接续【当前场景公共对话与互动】里最新的内容，对周围NPC说一句话。优先回应别人刚刚说的话，不要重复自己，不要替其他角色发言，不要输出角色名，不要动作描写、心理活动、旁白，不要输出任何[ACTION:*]标签。如果你认为这场续聊可以结束了，可以在你这句台词末尾加上 [END]；如果你一句都不想再说，也可以只输出 [END]。";
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
		int num = historyLines.Count - 1;
		while (num >= 0 && list.Count < Math.Max(1, maxLines))
		{
			string text = NormalizeSceneHistoryLineForLoreQuery(historyLines[num]);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
			num--;
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
		return "【续聊耐心规则】：请结合上面的耐心/状态信息决定是否继续聊。若你已经明显无聊、冷淡、恼火，或觉得话题正在变干，请优先缩短回复，并可在本句末尾加 [END] 结束续聊；若已没有继续聊的兴趣，也可以只输出 [END]。续聊阶段不要输出任何 [ACTION:*] 标签。";
	}

	private string BuildAutoGroupLoreContextForSpeaker(NpcDataPacket speakerNpc, Agent speakerAgent, CharacterObject speakerCharacter, Hero speakerHero, string kingdomIdOverride, List<string> visibleHistoryLines)
	{
		try
		{
			string text = BuildRecentSceneLoreQueryText(visibleHistoryLines);
			string text2 = GetLatestSceneNpcUtterance(speakerNpc?.AgentIndex ?? (-1));
			if (string.IsNullOrWhiteSpace(text))
			{
				text = text2;
				text2 = null;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			LogShoutLorePrequery("auto_group_round", speakerAgent, speakerCharacter, kingdomIdOverride, text, text2);
			if (speakerHero != null)
			{
				return AIConfigHandler.GetLoreContext(text, speakerHero, text2);
			}
			if (speakerCharacter != null)
			{
				return AIConfigHandler.GetLoreContext(text, speakerCharacter, kingdomIdOverride, text2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[AutoGroupChat] lore query failed: " + ex.Message);
		}
		return "";
	}

	private async Task<string> GenerateAutoGroupChatLineAsync(NpcDataPacket speakerNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			if (speakerNpc == null || allNpcData == null || allNpcData.Count < 2)
			{
				return "";
			}
			ApplySceneLocalDisambiguatedNames(allNpcData);
			await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { speakerNpc }, resolvedHeroes ?? new Dictionary<int, Hero>());
			DuelSettings settings = DuelSettings.GetSettings();
			int maxTokens = Math.Max(28, settings.ShoutMaxTokens / 2);
			int minTokens = Math.Max(10, maxTokens / 2);
			Mission current = Mission.Current;
			Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == speakerNpc.AgentIndex));
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return "";
			}
			BasicCharacterObject character = agent.Character;
			CharacterObject characterObject = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			Hero hero = null;
			if (speakerNpc.IsHero && resolvedHeroes != null)
			{
				resolvedHeroes.TryGetValue(speakerNpc.AgentIndex, out hero);
			}
			string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent);
			MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(hero, "请根据当前场景公共对话与互动继续和周围NPC闲聊。", null, speakerNpc.CultureId ?? "neutral", speakerNpc.IsHero, characterObject, kingdomIdOverride, speakerNpc.AgentIndex, suppressDynamicRuleAndLore: true);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【在场人物列表】：");
			foreach (NpcDataPacket npc in allNpcData)
			{
				if (npc != null)
				{
					stringBuilder.AppendLine(BuildSceneNpcListLineForPrompt(npc));
				}
			}
			string sceneNamingNote = BuildSceneNonHeroNamingNoteForPrompt(allNpcData);
			if (!string.IsNullOrWhiteSpace(sceneNamingNote))
			{
				stringBuilder.AppendLine(sceneNamingNote);
			}
			List<string> patienceStatusLines = new List<string>();
			foreach (NpcDataPacket npc2 in allNpcData)
			{
				if (npc2 == null)
				{
					continue;
				}
				try
				{
					if (((npc2.IsHero && resolvedHeroes != null && resolvedHeroes.TryGetValue(npc2.AgentIndex, out var hero2)) ? MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero2, out var statusLine, out var canSpeak) : MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc2.UnnamedKey, npc2.Name, GetSceneNpcPatienceNameForPrompt(npc2), out statusLine, out canSpeak)) && !string.IsNullOrWhiteSpace(statusLine))
					{
						patienceStatusLines.Add(statusLine);
					}
					statusLine = null;
					hero2 = null;
				}
				catch
				{
				}
			}
			if (patienceStatusLines.Count > 0)
			{
				stringBuilder.AppendLine("【4.三值状态】");
				stringBuilder.AppendLine("【NPC耐心状态】：");
				foreach (string item in patienceStatusLines)
				{
					stringBuilder.AppendLine(item);
				}
				stringBuilder.AppendLine(BuildAutoGroupPatienceInstruction());
			}
			stringBuilder.AppendLine("【群体续聊规则】");
			stringBuilder.AppendLine("1. 现在是NPC之间自己继续聊天，不是在回复玩家。");
			stringBuilder.AppendLine("2. 只输出你自己说的一句话，不要替其他NPC说话。");
			stringBuilder.AppendLine("3. 不要输出任何[ACTION:*]标签。");
			stringBuilder.AppendLine("4. 如果你认为这场续聊可以结束了，可以在你这句台词最后加上 [END]。");
			stringBuilder.AppendLine("5. 如果你一句都不想再说，也可以只输出 [END]。");
			string baseExtras = StripScenePersonaBlocks((shoutPromptContext?.Extras ?? "").Trim());
			string baseExtrasWithoutTrust;
			string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: stringBuilder.ToString().Trim());
			string text = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
			List<string> historyLines = null;
			lock (_historyLock)
			{
				if (_publicConversationHistory.Count > 0)
				{
					historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, speakerNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(speakerNpc));
				}
			}
			string loreContext = BuildAutoGroupLoreContextForSpeaker(speakerNpc, agent, characterObject, hero, kingdomIdOverride, historyLines);
			if (!string.IsNullOrWhiteSpace(loreContext))
			{
				text = (string.IsNullOrWhiteSpace(text) ? loreContext : (text + "\n" + loreContext));
			}
			string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
			string persistedHeroHistory = (speakerNpc.IsHero ? BuildPersistedHeroHistoryContext(speakerNpc.AgentIndex, "", resolvedHeroes) : "");
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
			string firstMeetingNpcFactSection = BuildSceneFirstMeetingNpcFactSection(hero);
			if (!string.IsNullOrWhiteSpace(firstMeetingNpcFactSection))
			{
				layeredPrompt = layeredPrompt + "\n" + firstMeetingNpcFactSection;
			}
			if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
			{
				layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
			}
			string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(speakerNpc, hero, allNpcData);
			if (!string.IsNullOrWhiteSpace(roleTopIntro))
			{
				layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
			}
			string userContent = BuildAutoGroupChatReplyInstruction(GetSceneNpcHistoryNameForPrompt(speakerNpc), allNpcData) + "\n" + string.Format("(回复长度要求：请将本轮回复控制在 {0}-{1} 字之间；若你想说完这句就结束续聊，请把 {2} 放在句尾；若一句都不想再说，只输出 {3},如果场景中有人表达类似有事要走了，再见之类的结束用于，你也可以在回复末尾输出{4})", minTokens, maxTokens, "[END]", "[END]", "[END]");
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
			string text2 = await ShoutNetwork.CallApiWithMessages(messages, 5000);
			if (string.IsNullOrWhiteSpace(text2) || text2.StartsWith("（错误") || text2.StartsWith("（程序错误") || text2.StartsWith("（API请求失败"))
			{
				return "";
			}
			string text3 = (text2 ?? "").Replace("\r", "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				int num = text3.IndexOf(':');
				if (num == -1)
				{
					num = text3.IndexOf('：');
				}
				if (num > 0 && num < 30)
				{
					string text4 = text3.Substring(num + 1).Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						text3 = text4;
					}
				}
			}
			text3 = StripLeakedPromptContentForShout(text3);
			text3 = StripStageDirectionsForPassiveShout(text3);
			text3 = StripActionTagsForSceneSpeech(text3);
			return text3.Trim();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("ShoutBehavior", "[AutoGroupChat] prompt failed: " + ex2.Message);
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
			Hero obj = hero;
			using (Logger.BeginTrace("shout_passive", (obj != null) ? ((MBObjectBase)obj).StringId : null, data.Name))
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
				Mission current = Mission.Current;
				Agent passiveAgent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == data.AgentIndex));
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(targetCharacter: (CharacterObject)/*isinst with value type is only supported in some contexts*/, kingdomIdOverride: TryGetKingdomIdOverrideFromAgent(passiveAgent), targetHero: hero, input: inputActionText, extraFact: fullExtra, cultureIdOverride: cultureId, hasAnyHero: data.IsHero, targetAgentIndex: data.AgentIndex, suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: !string.IsNullOrWhiteSpace(loreContext), prefetchedLoreContext: loreContext);
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
								Kingdom kObj = ((IEnumerable<Kingdom>)Kingdom.All)?.FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim().ToLower(), kingdomId, StringComparison.OrdinalIgnoreCase));
								if (kObj != null)
								{
									kingdomName = (((object)kObj.Name)?.ToString() ?? kingdomName).Trim();
									Hero leader = kObj.Leader;
									rulerName = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
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
									Settlement currentSettlement = Settlement.CurrentSettlement;
									object obj3;
									if (currentSettlement == null)
									{
										obj3 = null;
									}
									else
									{
										Clan ownerClan = currentSettlement.OwnerClan;
										if (ownerClan == null)
										{
											obj3 = null;
										}
										else
										{
											Hero leader2 = ownerClan.Leader;
											obj3 = ((leader2 != null) ? ((MBObjectBase)leader2).StringId : null);
										}
									}
									if (obj3 == null)
									{
										obj3 = "";
									}
									lordId = ((string)obj3).Trim().ToLower();
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
									Hero obj5 = Hero.Find(lordId);
									lordName = (((obj5 == null) ? null : ((object)obj5.Name)?.ToString()) ?? "").Trim();
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
							resolvedHeroes?.TryGetValue(npc2.AgentIndex, out hero2);
							Hero obj8 = hero2;
							if (obj8 != null && obj8.IsPrisoner)
							{
								PartyBase partyBelongedToAsPrisoner = hero2.PartyBelongedToAsPrisoner;
								object obj9;
								if (partyBelongedToAsPrisoner == null)
								{
									obj9 = null;
								}
								else
								{
									Hero leaderHero = partyBelongedToAsPrisoner.LeaderHero;
									obj9 = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
								}
								string captor = (string)obj9;
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
				bool passiveMultiNpcScene = allNpcData != null && allNpcData.Count((NpcDataPacket npcDataPacket) => npcDataPacket != null) > 1;
				if (!string.IsNullOrWhiteSpace(inputActionText))
				{
					sysPrompt.AppendLine("[玩家动作] " + inputActionText);
				}
				string scenePatienceInstruction = "";
				try
				{
					if (((hero == null) ? MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(data.UnnamedKey, data.Name, GetSceneNpcPatienceNameForPrompt(data), out var patienceLine, out var canSpeak) : MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out patienceLine, out canSpeak)) && !string.IsNullOrWhiteSpace(patienceLine))
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
					if (!string.IsNullOrWhiteSpace(playerNameForPrompt))
					{
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
				string baseExtrasWithoutTrust;
				string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: sysPrompt.ToString().Trim());
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(includeTradePricing: ctx != null && (ctx.UseRewardContext || ctx.IsLoanContext || ctx.UseDuelContext), npc: data, hero: hero, presentNpcs: allNpcData);
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
						//IL_0025: Unknown result type (might be due to invalid IL or missing references)
						//IL_002a: Unknown result type (might be due to invalid IL or missing references)
						//IL_0034: Expected O, but got Unknown
						InformationManager.DisplayMessage(new InformationMessage("[被动回应失败] " + output, new Color(1f, 0.3f, 0.3f, 1f)));
					});
					return "（没说话）";
				}
				return output;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("ShoutBehavior", "[ERROR] GetPassiveNpcResponse 异常: " + ex2.Message);
			Logger.Metric("api.shout_passive", ok: false);
			return "（没说话）";
		}
	}

	public void TriggerShout()
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Expected O, but got Unknown
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			return;
		}
		PauseGame();
		List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
		CapturePendingPlayerTurnCarryAgents(nearbyNPCAgents);
		if (nearbyNPCAgents != null && nearbyNPCAgents.Count > 0)
		{
			ActivateMultiSceneMovementSuppression(nearbyNPCAgents.Select((Agent agent) => (agent != null) ? agent.Index : (-1)));
		}
		if (nearbyNPCAgents == null || nearbyNPCAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f, 1f)));
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
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement((object)"normal", "交流", (ImageIdentifier)null, true, ""),
			new InquiryElement((object)"give", "给予其物品并交流", (ImageIdentifier)null, true, ""),
			new InquiryElement((object)"show", "向其展示物品并交流", (ImageIdentifier)null, true, "")
		};
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("与 " + text + " 交流", "当前目标：" + text + "\n请选择交流方式：", list, true, 1, 1, "确定", "取消", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
					BeginShoutTradeFlow(primaryDataPacket, isGive: true);
				}
				else if (text2 == "show")
				{
					BeginShoutTradeFlow(primaryDataPacket, isGive: false);
				}
				else
				{
					OpenShoutTextInput(primaryDataPacket, null, null);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OnShoutCancelled();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private void OpenShoutTextInput(NpcDataPacket primaryDataPacket, string preface, string extraFact)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		string text = primaryDataPacket?.Name ?? "附近的人";
		string text2 = "与 " + text + " 交流";
		string text3 = (string.IsNullOrWhiteSpace(preface) ? "输入你想说的话：" : (preface + "\n请输入你想说的话："));
		InformationManager.ShowTextInquiry(new TextInquiryData(text2, text3, true, true, "发送", "取消", (Action<string>)delegate(string input)
		{
			OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
		}, (Action)OnShoutCancelled, false, (Func<string, Tuple<bool, string>>)null, "", ""), true, false);
	}

	private void BeginShoutTradeFlow(NpcDataPacket targetNpc, bool isGive)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		_shoutTradeTargetNpc = targetNpc;
		_shoutPendingTradeIsGive = isGive;
		_shoutTradeOptions = BuildShoutTradeOptions();
		_shoutPendingTradeItems.Clear();
		_shoutPendingTradeItemIndex = 0;
		if (_shoutTradeOptions == null || _shoutTradeOptions.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你没有可用的物品或第纳尔。"));
			ResumeGame();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < _shoutTradeOptions.Count; i++)
		{
			ShoutTradeResourceOption shoutTradeResourceOption = _shoutTradeOptions[i];
			list.Add(new InquiryElement((object)i, $"{shoutTradeResourceOption.Name} (×{shoutTradeResourceOption.AvailableAmount})", (ImageIdentifier)null, true, $"可用数量: {shoutTradeResourceOption.AvailableAmount}"));
		}
		string text = targetNpc?.Name ?? "附近的人";
		string text2 = (isGive ? ("给予其物品并交流 - " + text) : ("向其展示物品并交流 - " + text));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(text2, "当前目标：" + text + "\n选择要使用的物品或第纳尔（可多选）：", list, true, 1, list.Count, "确定", "取消", (Action<List<InquiryElement>>)OnShoutTradeResourcesSelected, (Action<List<InquiryElement>>)delegate
		{
			ResetShoutTradeState();
			ResumeGame();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
	}

	private List<ShoutTradeResourceOption> BuildShoutTradeOptions()
	{
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		List<ShoutTradeResourceOption> list = new List<ShoutTradeResourceOption>();
		Hero mainHero = Hero.MainHero;
		MobileParty val = ((mainHero != null) ? mainHero.PartyBelongedTo : null);
		if (val == null)
		{
			return list;
		}
		Hero hero = null;
		string targetKey = "";
		if (!_shoutPendingTradeIsGive)
		{
			targetKey = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		}
		Hero mainHero2 = Hero.MainHero;
		int num = ((mainHero2 != null) ? mainHero2.Gold : 0);
		if (!_shoutPendingTradeIsGive)
		{
			num = MyBehavior.GetRemainingShowableGoldForExternal(hero, targetKey, num);
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
		ItemRoster itemRoster = val.ItemRoster;
		if (itemRoster != null)
		{
			Dictionary<string, ShoutTradeResourceOption> dictionary = new Dictionary<string, ShoutTradeResourceOption>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				int amount = ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
				if (item == null || amount <= 0)
				{
					continue;
				}
				string text = (((MBObjectBase)item).StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (dictionary.TryGetValue(text, out var value))
					{
						value.AvailableAmount += amount;
						value.InventoryTotalValue += (long)amount * (long)(RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement) ?? 1);
						continue;
					}
					dictionary[text] = new ShoutTradeResourceOption
					{
						IsGold = false,
						ItemId = text,
						Name = ((object)item.Name).ToString(),
						AvailableAmount = amount,
						Item = item,
						InventoryTotalValue = (long)amount * (long)(RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement) ?? 1)
					};
				}
			}
			foreach (ShoutTradeResourceOption value2 in dictionary.Values)
			{
				int num2 = value2.AvailableAmount;
				value2.InventoryUnitValue = ((num2 <= 0) ? 1 : Math.Max(1, (int)Math.Round((double)value2.InventoryTotalValue / (double)num2, MidpointRounding.AwayFromZero)));
				if (!_shoutPendingTradeIsGive)
				{
					num2 = MyBehavior.GetRemainingShowableItemCountForExternal(hero, targetKey, value2.ItemId, num2);
				}
				if (num2 > 0)
				{
					value2.AvailableAmount = num2;
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
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected O, but got Unknown
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
			if (shoutTradeResourceOption.IsGold == shoutPendingTradeItem.IsGold && shoutTradeResourceOption.ItemId == shoutPendingTradeItem.ItemId)
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
		string text = (_shoutPendingTradeIsGive ? "给予数量" : "展示数量");
		string text2 = $"[{_shoutPendingTradeItemIndex + 1}/{_shoutPendingTradeItems.Count}] {shoutPendingTradeItem.ItemName} 最多可填 {availableAmount}。\n请输入 1 到 {availableAmount} 的整数：";
		InformationManager.ShowTextInquiry(new TextInquiryData(text, text2, true, true, "确定", "返回", (Action<string>)delegate(string input)
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
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
		}, (Action)delegate
		{
			BeginShoutTradeFlow(_shoutTradeTargetNpc, _shoutPendingTradeIsGive);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void ShowShoutTradeChatInput()
	{
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		string text = _shoutTradeTargetNpc?.Name ?? "附近的人";
		bool shoutPendingTradeIsGive = _shoutPendingTradeIsGive;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount > 0)
			{
				if (shoutPendingTradeItem.IsGold)
				{
					stringBuilder.AppendLine(shoutPendingTradeIsGive ? $"  · 给予 {shoutPendingTradeItem.Amount} 第纳尔" : $"  · 展示 {shoutPendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					stringBuilder.AppendLine(shoutPendingTradeIsGive ? $"  · 给予 {shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}" : $"  · 展示 {shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}");
				}
			}
		}
		string text2 = (shoutPendingTradeIsGive ? "你准备给予对方以下物品：\n" : "你准备向对方展示以下物品：\n") + stringBuilder.ToString();
		string text3 = "与 " + text + " 交流";
		InformationManager.ShowTextInquiry(new TextInquiryData(text3, text2 + "\n请输入你想说的话：", true, true, "发送", "取消", (Action<string>)OnShoutTradeChatConfirmed, (Action)delegate
		{
			ResetShoutTradeState();
			OnShoutCancelled();
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), true, false);
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
		if (_shoutPendingTradeIsGive)
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
		Hero val = null;
		Agent val2 = null;
		CharacterObject val3 = null;
		try
		{
			if (shoutTradeTargetNpc != null)
			{
				Mission current = Mission.Current;
				val2 = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == shoutTradeTargetNpc.AgentIndex));
				BasicCharacterObject obj = ((val2 != null) ? val2.Character : null);
				val3 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
			}
			if (shoutTradeTargetNpc != null && shoutTradeTargetNpc.IsHero)
			{
				val = ResolveHeroFromAgentIndex(shoutTradeTargetNpc.AgentIndex);
			}
		}
		catch
		{
			val = null;
		}
		RewardSystemBehavior.SettlementMerchantKind kind = RewardSystemBehavior.SettlementMerchantKind.None;
		bool flag = val == null && val3 != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(val3, out kind);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		string targetKey = MyBehavior.BuildRuleTargetKeyForExternal(val, val3, shoutTradeTargetNpc?.AgentIndex ?? (-1));
		Hero mainHero = Hero.MainHero;
		MobileParty val4 = ((mainHero != null) ? mainHero.PartyBelongedTo : null);
		for (int num = 0; num < _shoutPendingTradeItems.Count; num++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[num];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				int num2 = Math.Min(shoutPendingTradeItem.Amount, Hero.MainHero.Gold);
				if (num2 <= 0)
				{
					continue;
				}
				if (val != null)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, val, num2, false);
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(val, num2, null, 0);
					}
					catch
					{
					}
				}
				else if (flag && currentSettlement != null)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, (Hero)null, num2, true);
					SettlementComponent settlementComponent = currentSettlement.SettlementComponent;
					if (settlementComponent != null)
					{
						settlementComponent.ChangeGold(num2);
					}
					RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, kind, num2, null, 0);
					RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, kind, $"你已经收下了玩家交来的 {num2} 第纳尔。", (val3 == null) ? null : ((object)((BasicCharacterObject)val3).Name)?.ToString());
				}
				else
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, (Hero)null, num2, true);
					RecordScenePrepaidTransfer(targetKey, num2);
				}
			}
			else
			{
				if (val4 == null)
				{
					continue;
				}
				ItemRoster itemRoster = val4.ItemRoster;
				if (itemRoster == null)
				{
					continue;
				}
				object obj4 = shoutPendingTradeItem.ItemId;
				if (obj4 == null)
				{
					ItemObject item = shoutPendingTradeItem.Item;
					obj4 = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
				}
				string text = ((string)obj4).Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				ItemObject removedItem;
				int num3 = MyBehavior.RemoveItemsFromRosterByStringId(itemRoster, text, shoutPendingTradeItem.Amount, out removedItem);
				if (num3 <= 0)
				{
					continue;
				}
				if (((val != null) ? val.PartyBelongedTo : null) != null && removedItem != null)
				{
					val.PartyBelongedTo.ItemRoster.AddToCounts(removedItem, num3);
				}
				else if (flag && ((currentSettlement != null) ? currentSettlement.ItemRoster : null) != null && removedItem != null)
				{
					currentSettlement.ItemRoster.AddToCounts(removedItem, num3);
					RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, kind, 0, text, num3);
					string arg = RewardSystemBehavior.Instance?.BuildSettlementItemValueFactSuffixForExternal(currentSettlement, removedItem, num3) ?? "";
					RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, kind, $"你已经收下了玩家交来的 {num3} 个 {((object)removedItem.Name)?.ToString() ?? text}{arg}。", (val3 == null) ? null : ((object)((BasicCharacterObject)val3).Name)?.ToString());
				}
				if (val != null)
				{
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(val, 0, text, num3);
					}
					catch
					{
					}
				}
			}
		}
	}

	private static int GetCurrentCampaignDaySafe()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return (int)((CampaignTime)(ref now)).ToDays;
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
			Settlement currentSettlement = Settlement.CurrentSettlement;
			return (((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null) ?? "").Trim().ToLowerInvariant();
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
			string[] obj = new string[4]
			{
				NormalizeSceneRevisitKeyToken(GetCurrentSettlementIdSafe()),
				null,
				null,
				null
			};
			Mission current = Mission.Current;
			obj[1] = NormalizeSceneRevisitKeyToken((current != null) ? current.SceneName : null);
			obj[2] = NormalizeSceneRevisitKeyToken(placeName);
			obj[3] = NormalizeSceneRevisitKeyToken(spotName);
			string[] source = obj;
			string text = string.Join("|", source.Where((string x) => !string.IsNullOrWhiteSpace(x)));
			return text.Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildSceneHeroRevisitRecordKey(Hero hero, string sceneKey)
	{
		string text = (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim();
		string text2 = (sceneKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text + "@" + text2;
	}

	private static string BuildSceneRevisitFactBody(string playerDisplayName, int elapsedDays)
	{
		string text = (string.IsNullOrWhiteSpace(playerDisplayName) ? "玩家" : playerDisplayName.Trim());
		if (elapsedDays <= 0)
		{
			return "你今天稍早时候刚与" + text + "见过面。";
		}
		return "距离你上次与" + text + "见面，已有" + elapsedDays + "天了。";
	}

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
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		List<(Hero, string, string, int)> list = new List<(Hero, string, string, int)>();
		lock (_historyLock)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (nearbyDatum == null || !nearbyDatum.IsHero)
				{
					continue;
				}
				Hero val = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				if (val == null)
				{
					continue;
				}
				string text3 = BuildSceneHeroRevisitRecordKey(val, text);
				if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3) && !_sceneHeroRevisitHandledThisSession.Contains(text3))
				{
					if (_sceneHeroRevisitDays.TryGetValue(text3, out var value))
					{
						list.Add((val, GetSceneNpcHistoryNameForPrompt(nearbyDatum), text3, Math.Max(0, currentCampaignDaySafe - value)));
					}
					_sceneHeroRevisitDays[text3] = currentCampaignDaySafe;
					_sceneHeroRevisitHandledThisSession.Add(text3);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (var item in list)
		{
			string text4 = (string.IsNullOrWhiteSpace(item.Item2) ? (((object)item.Item1.Name)?.ToString() ?? "对方") : item.Item2);
			string text5 = BuildSceneRevisitFactBody(text2, item.Item4);
			string extraFact = "[AFEF NPC行为补充] 对" + text4 + "：" + text5;
			RecordExtraFactToSceneHistory(extraFact, nearbyData);
			MyBehavior.AppendExternalDialogueHistory(item.Item1, null, null, "[AFEF NPC行为补充] " + text5);
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
			object obj = agent;
			if (obj == null)
			{
				Mission current = Mission.Current;
				obj = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex));
			}
			Agent val = (Agent)obj;
			BasicCharacterObject obj2 = ((val != null) ? val.Character : null);
			CharacterObject targetCharacter = (CharacterObject)(object)((obj2 is CharacterObject) ? obj2 : null);
			string text = MyBehavior.BuildRuleTargetKeyForExternal(null, targetCharacter, npc.AgentIndex);
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
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agent != null)
			{
				BasicCharacterObject character = agent.Character;
				CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val != null && ((BasicCharacterObject)val).IsSoldier)
				{
					CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
					AgentNavigator val2 = ((component != null) ? component.AgentNavigator : null);
					bool flag = false;
					if (val2 != null)
					{
						WeakGameEntity gameEntity;
						int num;
						if (val2.TargetUsableMachine != null && agent.IsUsingGameObject)
						{
							gameEntity = ((ScriptComponentBehavior)val2.TargetUsableMachine).GameEntity;
							num = (((WeakGameEntity)(ref gameEntity)).HasTag("sp_guard_castle") ? 1 : 0);
						}
						else
						{
							num = 0;
						}
						flag = (byte)num != 0;
						if (!flag && (val2.SpecialTargetTag == "sp_guard_castle" || val2.SpecialTargetTag == "sp_guard"))
						{
							LocationComplex current = LocationComplex.Current;
							Location lordsHallLocation = ((current != null) ? current.GetLocationWithId("lordshall") : null);
							Mission current2 = Mission.Current;
							MissionAgentHandler val3 = ((current2 != null) ? current2.GetMissionBehavior<MissionAgentHandler>() : null);
							if (lordsHallLocation != null && ((val3 != null) ? val3.TownPassageProps : null) != null)
							{
								UsableMachine val4 = val3.TownPassageProps.FirstOrDefault(delegate(UsableMachine x)
								{
									Passage val5 = (Passage)(object)((x is Passage) ? x : null);
									return val5 != null && val5.ToLocation == lordsHallLocation;
								});
								if (val4 != null)
								{
									gameEntity = ((ScriptComponentBehavior)val4).GameEntity;
									Vec3 globalPosition = ((WeakGameEntity)(ref gameEntity)).GlobalPosition;
									if (((Vec3)(ref globalPosition)).DistanceSquared(agent.Position) < 100f)
									{
										flag = true;
									}
								}
							}
						}
					}
					return flag;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private bool TryUnlockLordsHallForNpc(NpcDataPacket npc, Agent agent)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Invalid comparison between Unknown and I4
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Invalid comparison between Unknown and I4
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (npc == null || currentSettlement == null || !currentSettlement.IsTown)
			{
				return false;
			}
			object obj = agent;
			if (obj == null)
			{
				Mission current = Mission.Current;
				obj = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex));
			}
			Agent val = (Agent)obj;
			BasicCharacterObject obj2 = ((val != null) ? val.Character : null);
			CharacterObject val2 = (CharacterObject)(object)((obj2 is CharacterObject) ? obj2 : null);
			if (npc.IsHero || val2 == null || !((BasicCharacterObject)val2).IsSoldier || !IsLordsHallGuardAgent(val))
			{
				return false;
			}
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				GameModels models = current2.Models;
				obj3 = ((models != null) ? models.SettlementAccessModel : null);
			}
			SettlementAccessModel val3 = (SettlementAccessModel)obj3;
			if (val3 == null)
			{
				return false;
			}
			bool flag = false;
			TextObject val4 = null;
			if (val3.CanMainHeroAccessLocation(currentSettlement, "lordshall", ref flag, ref val4))
			{
				return true;
			}
			AccessDetails val5 = default(AccessDetails);
			val3.CanMainHeroEnterLordsHall(currentSettlement, ref val5);
			Campaign current3 = Campaign.Current;
			int? obj4;
			if (current3 == null)
			{
				obj4 = null;
			}
			else
			{
				GameModels models2 = current3.Models;
				if (models2 == null)
				{
					obj4 = null;
				}
				else
				{
					BribeCalculationModel bribeCalculationModel = models2.BribeCalculationModel;
					obj4 = ((bribeCalculationModel != null) ? new int?(bribeCalculationModel.GetBribeToEnterLordsHall(currentSettlement)) : ((int?)null));
				}
			}
			int? num = obj4;
			int valueOrDefault = num.GetValueOrDefault();
			string targetKey = MyBehavior.BuildRuleTargetKeyForExternal(null, val2, npc.AgentIndex);
			int recentNonHeroGoldForRuleTarget = GetRecentNonHeroGoldForRuleTarget(targetKey);
			if ((int)val5.AccessLevel == 1 && (int)val5.LimitedAccessSolution == 1 && valueOrDefault > 0 && recentNonHeroGoldForRuleTarget > 0)
			{
				currentSettlement.BribePaid += valueOrDefault;
				ConsumeRecentNonHeroGoldForRuleTarget(targetKey, recentNonHeroGoldForRuleTarget);
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
		string playerDisplayNameForShout = GetPlayerDisplayNameForShout();
		string shoutTradeTargetDisplayName = GetShoutTradeTargetDisplayName();
		List<string> list = new List<string>();
		long num = 0L;
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (shoutPendingTradeItem.IsGold)
			{
				list.Add($"{shoutPendingTradeItem.Amount} 第纳尔");
				if (!isGive)
				{
					num += shoutPendingTradeItem.Amount;
				}
			}
			else if (isGive)
			{
				object obj = shoutPendingTradeItem.ItemId;
				if (obj == null)
				{
					ItemObject item = shoutPendingTradeItem.Item;
					obj = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
				}
				string itemId = (string)obj;
				string text = "";
				try
				{
					Mission current = Mission.Current;
					Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == (_shoutTradeTargetNpc?.AgentIndex ?? (-1))));
					BasicCharacterObject obj2 = ((val != null) ? val.Character : null);
					CharacterObject val2 = (CharacterObject)(object)((obj2 is CharacterObject) ? obj2 : null);
					Hero val3 = ((val2 != null) ? val2.HeroObject : null);
					text = ((val2 == null || val3 != null || RewardSystemBehavior.Instance == null || !RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(val2, out var _)) ? (RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(val3 ?? Hero.MainHero, itemId, shoutPendingTradeItem.Amount) ?? "") : RewardSystemBehavior.Instance.BuildSettlementItemValueFactSuffixForExternal(Settlement.CurrentSettlement, itemId, shoutPendingTradeItem.Amount));
				}
				catch
				{
					text = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(Hero.MainHero, itemId, shoutPendingTradeItem.Amount) ?? "";
				}
				list.Add($"{shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}{text}");
			}
			else
			{
				string arg = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? "";
				num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0;
				list.Add($"{shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}{arg}");
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		if (isGive)
		{
			return playerDisplayNameForShout + "已经将 " + string.Join("、", list) + " 交给 " + shoutTradeTargetDisplayName + "。";
		}
		return playerDisplayNameForShout + "给 " + shoutTradeTargetDisplayName + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，证明自己有这些东西。";
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
			if (shoutPendingTradeItem != null && shoutPendingTradeItem.Amount > 0)
			{
				num = ((!shoutPendingTradeItem.IsGold) ? (num + (RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(shoutPendingTradeItem.Item, shoutPendingTradeItem.Amount, shoutPendingTradeItem.InventoryUnitValue) ?? 0)) : (num + shoutPendingTradeItem.Amount));
			}
		}
		return num;
	}

	private void ShowShoutPendingDisplayValueMessage(long totalValue)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		if (totalValue > 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("【展示估值】你向 " + GetShoutTradeTargetDisplayName() + " 展示了总值为 " + totalValue + " 第纳尔的财物。", new Color(0.95f, 0.85f, 0.25f, 1f)));
		}
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
			return text + "现在正在参与一场多人对话。请只以" + text + "的身份自然发言，并结合【当前场景公共对话与互动】中其他人刚才说过的话来回应，不要各说各的。只输出你要说的话，不要输出动作描写、心理活动或旁白。如果需要写入标签，请把需要的标签都完整写在回复末尾，不要遗漏。";
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
		string text = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		if (hero == null && string.IsNullOrWhiteSpace(text))
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
			string text2 = (shoutPendingTradeItem.ItemId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				if (!dictionary.ContainsKey(text2))
				{
					dictionary[text2] = 0;
				}
				dictionary[text2] += shoutPendingTradeItem.Amount;
			}
		}
		MyBehavior.RecordShownResourcesForExternal(hero, text, num, dictionary);
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
			Hero obj2 = hero;
			if (!string.IsNullOrWhiteSpace((obj2 != null) ? ((MBObjectBase)obj2).StringId : null))
			{
				return ((MBObjectBase)hero).StringId;
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
		_shoutPendingTradeIsGive = false;
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
		MergeAgentLists(nearbyAgents, ConsumePendingPlayerTurnCarryAgents());
		if (nearbyAgents.Count > 0)
		{
			ActivateMultiSceneMovementSuppression(nearbyAgents.Select((Agent val) => (val != null) ? val.Index : (-1)));
		}
		if (nearbyAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f, 1f)));
			ResumeGame();
			return;
		}
		if (nearbyAgents.Count == 1)
		{
			string targetName = nearbyAgents[0].Name.ToString();
			InformationManager.DisplayMessage(new InformationMessage(targetName + " 正在思考...", new Color(0.7f, 0.7f, 0.7f, 1f)));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("人群正在思考...", new Color(0.7f, 0.7f, 0.7f, 1f)));
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
			extraFact = (string.IsNullOrWhiteSpace(extraFact) ? sceneTauntExtraFact : (extraFact + "\n" + sceneTauntExtraFact));
		}
		string combatShoutExtraFact = BuildCombatActiveShoutExtraFact(primaryTarget);
		if (!string.IsNullOrWhiteSpace(combatShoutExtraFact))
		{
			extraFact = (string.IsNullOrWhiteSpace(extraFact) ? combatShoutExtraFact : (extraFact + "\n" + combatShoutExtraFact));
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
		TrackPlayerInteraction(primaryDataPacket, capturedNpcData?.Count ?? 0);
		ResumeGame();
		Task.Run(async delegate
		{
			try
			{
				Dictionary<int, PrecomputedShoutRagContext> precomputedContexts = new Dictionary<int, PrecomputedShoutRagContext>();
				Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
				foreach (Agent agent2 in nearbyAgents)
				{
					if (agent2 != null)
					{
						HoldSceneConversationAgents(nearbyAgents);
						BasicCharacterObject character = agent2.Character;
						CharacterObject co = (CharacterObject)(object)((character is CharacterObject) ? character : null);
						if (co != null && co.HeroObject != null)
						{
							resolvedHeroes[agent2.Index] = co.HeroObject;
						}
						string kingdomIdOverride = TryGetKingdomIdOverrideFromAgent(agent2);
						string secondaryInput = GetLatestSceneNpcUtterance(agent2.Index);
						LogShoutLorePrequery("group_precalc", agent2, co, kingdomIdOverride, shoutText, secondaryInput);
						string lore = AIConfigHandler.GetLoreContext(shoutText, co, kingdomIdOverride, secondaryInput);
						precomputedContexts[agent2.Index] = new PrecomputedShoutRagContext
						{
							HasLoreContext = !string.IsNullOrWhiteSpace(lore),
							LoreContext = (lore ?? ""),
							HasPersistedHistoryContext = false,
							PersistedHistoryContext = ""
						};
					}
				}
				foreach (NpcDataPacket npc in capturedNpcData)
				{
					Agent liveAgent = nearbyAgents.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
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
						up = null;
						ub = null;
					}
				}
				await HandleGroupResponse(shoutText, capturedNpcData, sceneDesc, primaryDataPacket, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Logger.Log("ShoutBehavior", "[ERROR] ProcessShoutConfirmedInternal background failed: " + ex2.Message);
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
				if (true)
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
							resolvedHeroes?.TryGetValue(npc.AgentIndex, out hero);
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
						//IL_001a: Unknown result type (might be due to invalid IL or missing references)
						//IL_001f: Unknown result type (might be due to invalid IL or missing references)
						//IL_0029: Expected O, but got Unknown
						InformationManager.DisplayMessage(new InformationMessage("周围的人明显不想继续聊下去。", new Color(1f, 0.8f, 0.3f, 1f)));
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
						NpcDataPacket heroNpc = speakingCandidates.FirstOrDefault((NpcDataPacket npcDataPacket3) => npcDataPacket3?.IsHero ?? false);
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
				bool hasAnyHero = speakingCandidates.Any((NpcDataPacket npcDataPacket3) => npcDataPacket3?.IsHero ?? false);
				int contextAgentIndex = ((primaryNpc != null) ? primaryNpc.AgentIndex : ((speakingCandidates.Count > 0) ? speakingCandidates[0].AgentIndex : (-1)));
				object obj2;
				if (contextAgentIndex < 0)
				{
					obj2 = null;
				}
				else
				{
					Mission current = Mission.Current;
					obj2 = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == contextAgentIndex));
				}
				Agent contextAgent = (Agent)obj2;
				BasicCharacterObject obj3 = ((contextAgent != null) ? contextAgent.Character : null);
				CharacterObject contextCharacter = (CharacterObject)(object)((obj3 is CharacterObject) ? obj3 : null);
				string contextKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(contextAgent);
				PrecomputedShoutRagContext contextPrecomputed = null;
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(usePrefetchedLoreContext: (precomputedContexts?.TryGetValue(contextAgentIndex, out contextPrecomputed) ?? false) && contextPrecomputed != null && contextPrecomputed.HasLoreContext, targetHero: contextHero, input: playerText, extraFact: extraFact, cultureIdOverride: cultureId, hasAnyHero: hasAnyHero, targetCharacter: contextCharacter, kingdomIdOverride: contextKingdomIdOverride, targetAgentIndex: contextAgentIndex, suppressDynamicRuleAndLore: false, prefetchedLoreContext: contextPrecomputed?.LoreContext);
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
								Kingdom kObj = ((IEnumerable<Kingdom>)Kingdom.All)?.FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim().ToLower(), kingdomId, StringComparison.OrdinalIgnoreCase));
								if (kObj != null)
								{
									kingdomName = (((object)kObj.Name)?.ToString() ?? kingdomName).Trim();
									Hero leader = kObj.Leader;
									rulerName = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
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
									Settlement currentSettlement = Settlement.CurrentSettlement;
									object obj5;
									if (currentSettlement == null)
									{
										obj5 = null;
									}
									else
									{
										Clan ownerClan = currentSettlement.OwnerClan;
										if (ownerClan == null)
										{
											obj5 = null;
										}
										else
										{
											Hero leader2 = ownerClan.Leader;
											obj5 = ((leader2 != null) ? ((MBObjectBase)leader2).StringId : null);
										}
									}
									if (obj5 == null)
									{
										obj5 = "";
									}
									lordId = ((string)obj5).Trim().ToLower();
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
									Hero obj7 = Hero.Find(lordId);
									lordName = (((obj7 == null) ? null : ((object)obj7.Name)?.ToString()) ?? "").Trim();
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
							resolvedHeroes?.TryGetValue(npc2.AgentIndex, out hero2);
							Hero obj10 = hero2;
							if (obj10 != null && obj10.IsPrisoner)
							{
								PartyBase partyBelongedToAsPrisoner = hero2.PartyBelongedToAsPrisoner;
								object obj11;
								if (partyBelongedToAsPrisoner == null)
								{
									obj11 = null;
								}
								else
								{
									Hero leaderHero = partyBelongedToAsPrisoner.LeaderHero;
									obj11 = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
								}
								string captor = (string)obj11;
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
				AppendSceneSummonPromptSection(sysPrompt, sceneSummonTargets);
				AppendSceneGuidePromptSection(sysPrompt, sceneGuideTargets);
				string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(speakingCandidates);
				if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
				{
					sysPrompt.AppendLine(sceneSummonClosureInstruction);
				}
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(primaryNpc);
				if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
				{
					sysPrompt.AppendLine(sceneFollowControlInstruction);
				}
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
				string baseExtrasWithoutTrust;
				string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: sysPrompt.ToString().Trim());
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
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
				if (primaryNpc != null && speakingCandidates.Any((NpcDataPacket npcDataPacket3) => npcDataPacket3 != null && npcDataPacket3.AgentIndex == primaryNpc.AgentIndex))
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
					NpcDataPacket npcDataPacket2 = speakingCandidates.FirstOrDefault((NpcDataPacket x) => x?.IsHero ?? false);
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
				ftm.OnNewLineReady = delegate(NpcDataPacket npcDataPacket3, string content)
				{
					if (npcDataPacket3 != null && !string.IsNullOrWhiteSpace(content))
					{
						hasAnyQueuedLine = true;
						EnqueueSpeechLine(npcDataPacket3, content.Trim(), capturedAllNpcData, skipHistory: false, suppressStare: false, capturedSceneSummonTargets, capturedSceneGuideTargets);
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
						//IL_0025: Unknown result type (might be due to invalid IL or missing references)
						//IL_002a: Unknown result type (might be due to invalid IL or missing references)
						//IL_0034: Expected O, but got Unknown
						InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + streamError, new Color(1f, 0.3f, 0.3f, 1f)));
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
				CharacterObject co = null;
				NpcDataPacket fallbackNpc;
				Agent liveAgent;
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
						c = (fullText ?? "").Trim();
						int ci = c.IndexOf(':');
						if (ci == -1)
						{
							ci = c.IndexOf('：');
						}
						if (ci > 0 && ci < 20)
						{
							string rest = c.Substring(ci + 1).Trim();
							if (!string.IsNullOrWhiteSpace(rest))
							{
								c = rest;
							}
						}
						c = Regex.Replace(c, "\\（.*?\\）", "");
						c = Regex.Replace(c, "\\(.*?\\)", "");
						c = Regex.Replace(c, "\\*.*?\\*", "");
						c = (c ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(c))
						{
							Mission current2 = Mission.Current;
							liveAgent = ((current2 == null) ? null : ((IEnumerable<Agent>)current2.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == fallbackNpc.AgentIndex));
							if (liveAgent != null)
							{
								BasicCharacterObject character = liveAgent.Character;
								co = (CharacterObject)(object)((character is CharacterObject) ? character : null);
								if (co != null)
								{
									num = ((co.HeroObject != null) ? 1 : 0);
									goto IL_20b3;
								}
							}
							num = 0;
							goto IL_20b3;
						}
					}
				}
				goto IL_2044;
				IL_2044:
				Logger.Obs("ShoutGroup", "done", new Dictionary<string, object>
				{
					["streamFailed"] = streamFailed,
					["hasAnyQueuedLine"] = hasAnyQueuedLine,
					["speakingCandidates"] = speakingCandidates.Count
				});
				goto end_IL_00a0;
				IL_20b3:
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
				goto IL_2044;
				end_IL_00a0:;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Logger.Log("ShoutBehavior", "[ERROR] " + ex3.Message);
			Logger.Obs("ShoutGroup", "error", new Dictionary<string, object>
			{
				["message"] = ex3.Message,
				["type"] = ex3.GetType().Name
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
			bool autoGroupEndRequested = false;
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
						resolvedHeroes?.TryGetValue(npc.AgentIndex, out hero);
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
					if (!hasStatus || canSpeak)
					{
						speakableCandidates.Add(npc);
					}
				}
			}
			if (speakableCandidates.Count == 0)
			{
				_mainThreadActions.Enqueue(delegate
				{
					//IL_001a: Unknown result type (might be due to invalid IL or missing references)
					//IL_001f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0029: Expected O, but got Unknown
					InformationManager.DisplayMessage(new InformationMessage("周围的人明显不想继续聊下去。", new Color(1f, 0.8f, 0.3f, 1f)));
				});
				return;
			}
			await EnsurePersonaForCandidatesAsync(speakableCandidates, resolvedHeroes);
			HoldSceneConversationParticipants(speakableCandidates);
			DuelSettings settings = DuelSettings.GetSettings();
			int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
			int minTokens = Math.Max(5, maxTokens / 2);
			int lastSpeakerAgentIndex = -1;
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
							Kingdom kObj = ((IEnumerable<Kingdom>)Kingdom.All)?.FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim().ToLower(), kingdomId, StringComparison.OrdinalIgnoreCase));
							if (kObj != null)
							{
								kingdomName = (((object)kObj.Name)?.ToString() ?? kingdomName).Trim();
								Hero leader = kObj.Leader;
								rulerName = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
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
								Settlement currentSettlement = Settlement.CurrentSettlement;
								object obj2;
								if (currentSettlement == null)
								{
									obj2 = null;
								}
								else
								{
									Clan ownerClan = currentSettlement.OwnerClan;
									if (ownerClan == null)
									{
										obj2 = null;
									}
									else
									{
										Hero leader2 = ownerClan.Leader;
										obj2 = ((leader2 != null) ? ((MBObjectBase)leader2).StringId : null);
									}
								}
								if (obj2 == null)
								{
									obj2 = "";
								}
								lordId = ((string)obj2).Trim().ToLower();
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
								Hero obj4 = Hero.Find(lordId);
								lordName = (((obj4 == null) ? null : ((object)obj4.Name)?.ToString()) ?? "").Trim();
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
						if (hero2 != null && hero2.IsPrisoner)
						{
							PartyBase partyBelongedToAsPrisoner = hero2.PartyBelongedToAsPrisoner;
							object obj7;
							if (partyBelongedToAsPrisoner == null)
							{
								obj7 = null;
							}
							else
							{
								Hero leaderHero = partyBelongedToAsPrisoner.LeaderHero;
								obj7 = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
							}
							string captor = (string)obj7;
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
			AppendSceneSummonPromptSection(commonCandidatesList, sceneSummonTargets);
			AppendSceneGuidePromptSection(commonCandidatesList, sceneGuideTargets);
			string sceneSummonClosureInstruction = BuildSceneSummonClosurePromptInstruction(speakableCandidates);
			if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
			{
				commonCandidatesList.AppendLine(sceneSummonClosureInstruction);
			}
			foreach (NpcDataPacket npc3 in speakableCandidates)
			{
				if (!IsSceneConversationEpochCurrent(conversationEpoch))
				{
					return;
				}
				if (npc3 == null)
				{
					continue;
				}
				HoldSceneConversationParticipants(speakableCandidates);
				Hero contextHero = null;
				if (npc3.IsHero && resolvedHeroes != null)
				{
					resolvedHeroes.TryGetValue(npc3.AgentIndex, out contextHero);
				}
				string cultureId = npc3.CultureId ?? "neutral";
				PrecomputedShoutRagContext precomputed = null;
				bool hasPrecomputed = precomputedContexts?.TryGetValue(npc3.AgentIndex, out precomputed) ?? false;
				string loreContext = ((hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "");
				string fullExtra = (string.IsNullOrWhiteSpace(loreContext) ? null : loreContext);
				if (!string.IsNullOrWhiteSpace(extraFact))
				{
					fullExtra = ((fullExtra == null) ? extraFact : (fullExtra + "\n" + extraFact));
				}
				Mission current = Mission.Current;
				Agent npcAgent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc3.AgentIndex));
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(targetCharacter: (CharacterObject)/*isinst with value type is only supported in some contexts*/, kingdomIdOverride: TryGetKingdomIdOverrideFromAgent(npcAgent), targetHero: contextHero, input: playerText, extraFact: fullExtra, cultureIdOverride: cultureId, hasAnyHero: npc3.IsHero, targetAgentIndex: npc3.AgentIndex, suppressDynamicRuleAndLore: false, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext);
				StringBuilder local = new StringBuilder();
				local.Append(commonCandidatesList);
				bool multiNpcScene = speakableCandidates.Count > 1;
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
				if (multiNpcScene)
				{
					string playerNameForPrompt = GetPlayerDisplayNameForShout();
					if (!string.IsNullOrWhiteSpace(playerNameForPrompt))
					{
					}
				}
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string baseExtrasWithoutTrust;
				string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: local.ToString().Trim());
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				string sceneFollowControlInstruction = BuildSceneFollowControlPromptInstruction(npc3);
				if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
				{
					layeredPrompt = layeredPrompt + "\n" + sceneFollowControlInstruction;
				}
				List<string> historyLines = null;
				lock (_historyLock)
				{
					if (_publicConversationHistory.Count > 0)
					{
						historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, npc3.AgentIndex, GetSceneNpcHistoryNameForPrompt(npc3), speakableCandidates.Count > 1);
					}
				}
				string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
				string persistedHeroHistory = (npc3.IsHero ? GetOrBuildPrecomputedPersistedHistoryContext(npc3.AgentIndex, playerText, resolvedHeroes, precomputedContexts) : "");
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
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(npc3, contextHero, speakableCandidates, includeInventorySummary, includeTradePricing);
				if (!string.IsNullOrWhiteSpace(roleTopIntro))
				{
					layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
				}
				string singleReplyPlayerName = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
				{
					singleReplyPlayerName = "玩家";
				}
				string singleReplyUserContent = BuildSingleNpcSceneReplyInstruction(GetSceneNpcHistoryNameForPrompt(npc3), multiNpcScene) + "\n" + $"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{singleReplyPlayerName}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)" + (string.IsNullOrWhiteSpace(scenePatienceInstruction) ? "" : ("\n" + scenePatienceInstruction));
				if (multiNpcScene)
				{
					singleReplyUserContent += "\n(若你认为玩家这次插话是在结束这场群聊，或你判断本轮回复后不必再进入下一轮NPC自治续聊，可以在这句末尾追加 [END]；若你一句都不想再说，也可以只输出 [END])";
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
						//IL_0025: Unknown result type (might be due to invalid IL or missing references)
						//IL_002a: Unknown result type (might be due to invalid IL or missing references)
						//IL_0034: Expected O, but got Unknown
						InformationManager.DisplayMessage(new InformationMessage("[场景喊话] " + output, new Color(1f, 0.3f, 0.3f, 1f)));
					});
					continue;
				}
				string cleaned = (output ?? "").Replace("\r", "").Trim();
				if (!string.IsNullOrWhiteSpace(cleaned))
				{
					int ci = cleaned.IndexOf(':');
					if (ci == -1)
					{
						ci = cleaned.IndexOf('：');
					}
					if (ci > 0 && ci < 30)
					{
						string rest = cleaned.Substring(ci + 1).Trim();
						if (!string.IsNullOrWhiteSpace(rest))
						{
							cleaned = rest;
						}
					}
				}
				cleaned = StripLeakedPromptContentForShout(cleaned);
				cleaned = StripStageDirectionsForPassiveShout(cleaned);
				bool endRequested = multiNpcScene && ContainsAutoGroupEndSignal(cleaned);
				if (endRequested)
				{
					autoGroupEndRequested = true;
				}
				int num;
				if (endRequested)
				{
					ShoutBehavior shoutBehavior = this;
					Mission current2 = Mission.Current;
					num = (shoutBehavior.IsAgentFollowingPlayerBySceneCommand((current2 == null) ? null : ((IEnumerable<Agent>)current2.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc3.AgentIndex && a.IsActive())) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				bool flag9 = (byte)num != 0;
				bool flag10 = endRequested && TryGetSceneSummonConversationSessionForAgentIndex(npc3.AgentIndex) != null;
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
					lastSpeakerAgentIndex = npc3.AgentIndex;
					EnqueueSpeechLineWithOptions(npc3, cleaned, allNpcData, commitHistory: true, suppressStare: false, allowPlayerDirectedActions: true, conversationEpoch, sceneSummonTargets, sceneGuideTargets);
				}
				baseExtrasWithoutTrust = null;
			}
			if (IsSceneConversationEpochCurrent(conversationEpoch) && speakableCandidates.Count > 1 && !autoGroupEndRequested)
			{
				StartAutoGroupChatSession(speakableCandidates, primaryNpc, conversationEpoch, lastSpeakerAgentIndex);
			}
			else if (speakableCandidates.Count > 1 && autoGroupEndRequested)
			{
				DeactivateMultiSceneMovementSuppression();
				RequestSceneConversationAttentionRelease(from npcDataPacket in speakableCandidates
					where npcDataPacket != null
					select npcDataPacket.AgentIndex);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("ShoutBehavior", "[ERROR] HandleGroupResponsePerHeroIndependent: " + ex2.Message);
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
			if (npcDataPacket != null)
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npcDataPacket.AgentIndex);
				if (val != null && val.IsActive() && val.IsHuman && val != Agent.Main)
				{
					AddAgentToStareList(val);
				}
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
			Agent val = participants[i];
			if (val != null && val.IsActive() && val.IsHuman && val != Agent.Main)
			{
				AddAgentToStareList(val);
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
		foreach (NpcDataPacket allNpcDatum in allNpcData)
		{
			NpcDataPacket npcDataPacket = CloneNpcDataPacket(allNpcDatum);
			if (npcDataPacket != null)
			{
				list.Add(npcDataPacket);
			}
		}
		return list;
	}

	private static void ApplySceneLocalDisambiguatedNames(List<NpcDataPacket> allNpcData)
	{
		if (allNpcData != null && allNpcData.Count != 0)
		{
			ShoutUtils.EnsureScenePromptNames(allNpcData);
		}
	}

	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null)
	{
		EnqueueSpeechLineWithOptions(npc, content, allNpcData, !skipHistory, suppressStare, allowPlayerDirectedActions: true, 0, sceneSummonTargets, sceneGuideTargets);
	}

	private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null)
	{
		if (npc == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		Mission current = Mission.Current;
		Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex));
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		NpcDataPacket value = CloneNpcDataPacket(npc);
		List<NpcDataPacket> list = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(list);
		NpcDataPacket npcDataPacket = list.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == value.AgentIndex);
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
				ContextSnapshot = list,
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
		Task.Run(async delegate
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
						if (IsSceneConversationEpochCurrent(requiredConversationEpoch))
						{
							Mission current = Mission.Current;
							Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == matchedNpc.AgentIndex));
							if (CanAgentParticipateInSceneSpeech(val))
							{
								bool escalatedToBattle = false;
								bool escalatedToFight = false;
								bool flag = false;
								try
								{
									if (val == null)
									{
										goto IL_0208;
									}
									BasicCharacterObject character = val.Character;
									CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
									if (val2 == null || val2.HeroObject == null)
									{
										goto IL_0208;
									}
									MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(val2.HeroObject, ref content);
									if (allowPlayerDirectedActions)
									{
										DuelBehavior.TryCacheDuelAfterLinesFromText(val2.HeroObject, ref content);
										DuelBehavior.TryCacheDuelStakeFromText(val2.HeroObject, ref content);
										VanillaIssueOfferBridge.ApplyIssueOfferTags(val2.HeroObject, ref content);
										if (RewardSystemBehavior.Instance != null)
										{
											RewardSystemBehavior.Instance.ApplyRewardTags(val2.HeroObject, Hero.MainHero, ref content);
											List<string> list = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
											if (list != null)
											{
												foreach (string item2 in list)
												{
													RecordSystemFactForNearbySafe(allNpcData, item2);
												}
											}
										}
										if (RomanceSystemBehavior.Instance != null)
										{
											RomanceSystemBehavior.Instance.ApplyMarriageTags(val2.HeroObject, Hero.MainHero, ref content);
										}
										if (!ShouldSuppressSceneConversationControlForMeeting())
										{
											LordEncounterBehavior.TryProcessMeetingTauntAction(val2.HeroObject, ref content, out escalatedToBattle);
										}
										else
										{
											StripMeetingTauntTagsForSceneConversation(ref content);
										}
										flag = SceneTauntBehavior.TryProcessSceneTauntAction(val2.HeroObject, val2, matchedNpc.AgentIndex, ref content, out escalatedToFight);
									}
									goto end_IL_0067;
									IL_0208:
									MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(matchedNpc.UnnamedKey, matchedNpc.Name, ref content);
									if (allowPlayerDirectedActions && val != null)
									{
										BasicCharacterObject character2 = val.Character;
										CharacterObject val3 = (CharacterObject)(object)((character2 is CharacterObject) ? character2 : null);
										if (val3 != null && RewardSystemBehavior.Instance != null)
										{
											RewardSystemBehavior.Instance.ApplyMerchantRewardTags(val3, Hero.MainHero, ref content);
											List<string> list2 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
											if (list2 != null)
											{
												foreach (string item3 in list2)
												{
													RecordSystemFactForNearbySafe(allNpcData, item3);
												}
											}
										}
									}
									if (allowPlayerDirectedActions && val != null)
									{
										BasicCharacterObject character3 = val.Character;
										CharacterObject val4 = (CharacterObject)(object)((character3 is CharacterObject) ? character3 : null);
										if (val4 != null)
										{
											flag = SceneTauntBehavior.TryProcessSceneTauntAction(val4.HeroObject, val4, matchedNpc.AgentIndex, ref content, out escalatedToFight);
										}
									}
									end_IL_0067:;
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
								SceneSummonConversationSession session = null;
								ActiveSceneSummonRequest preparedRequest = null;
								ActiveSceneGuideRequest preparedRequest2 = null;
								bool flag2 = allowPlayerDirectedActions && !escalatedToFight && TryConsumeSceneFollowStopTag(matchedNpc, val, ref content);
								bool flag3 = allowPlayerDirectedActions && !escalatedToFight && TryConsumeSceneFollowStartTag(matchedNpc, val, ref content);
								bool flag4 = allowPlayerDirectedActions && !escalatedToFight && TryConsumeSceneEndChatActionTag(matchedNpc, val, ref content, out session);
								if (flag && string.IsNullOrWhiteSpace(content))
								{
									content = BuildFallbackSceneTauntSpeech(escalatedToFight);
								}
								if (flag && !IsMeetingSceneConversationReleaseSensitive() && string.IsNullOrWhiteSpace(content))
								{
									ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex);
									if (escalatedToFight && matchedNpc.AgentIndex >= 0)
									{
										InterruptAgentSpeechForCombat(matchedNpc.AgentIndex, "scene_taunt_action");
									}
								}
								else
								{
									if (!string.IsNullOrWhiteSpace(content))
									{
										bool flag5 = allowPlayerDirectedActions && !escalatedToBattle && !escalatedToFight && ShoutUtils.TryTriggerDuelAction(matchedNpc, ref content);
										bool flag6 = allowPlayerDirectedActions && !escalatedToFight && TryTriggerOpenLordsHallAction(matchedNpc, val, ref content);
										bool flag7 = allowPlayerDirectedActions && !escalatedToFight && TryTriggerSceneSummonAction(matchedNpc, val, sceneSummonTargets, ref content, out preparedRequest);
										bool flag8 = allowPlayerDirectedActions && !escalatedToFight && TryTriggerSceneGuideAction(matchedNpc, val, sceneGuideTargets, ref content, out preparedRequest2);
										if (!string.IsNullOrWhiteSpace(content))
										{
											if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
											{
												return;
											}
											string text = SanitizeSceneSpeechText(content);
											RefreshSceneSummonConversationForSpeaker((val != null) ? val.Index : matchedNpc.AgentIndex);
											bool flag9 = IsAgentHostileToMainAgent(val);
											SceneSpeechPlaybackInfo playbackInfo = ShowNpcSpeechOutput(matchedNpc, val, text);
											if (flag2)
											{
												ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, playbackInfo);
											}
											else if (flag3)
											{
												ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, playbackInfo);
											}
											if (flag4 && session != null)
											{
												_pendingSceneSummonReturnsAfterSpeech[matchedNpc.AgentIndex] = new PendingSceneSummonReturnAfterSpeech
												{
													AgentIndex = matchedNpc.AgentIndex,
													Session = session
												};
											}
											ScheduleSceneSummonReturnAfterSpeech(matchedNpc.AgentIndex, playbackInfo);
											ScheduleSceneGuideReturnAfterSpeech(matchedNpc.AgentIndex, playbackInfo);
											if (flag7 && preparedRequest != null)
											{
												SchedulePreparedSceneSummonLaunch(preparedRequest, playbackInfo, text);
											}
											if (flag8 && preparedRequest2 != null)
											{
												SchedulePreparedSceneGuideLaunch(preparedRequest2, playbackInfo, text);
											}
											if (flag9)
											{
												RefreshHostileCombatAgentAutonomy(val);
											}
											if (CanAgentParticipateInSceneSpeech(val) && !suppressStare && !flag9 && !flag7 && !flag8)
											{
												HoldSceneConversationParticipants(allNpcData);
												AddAgentToStareList(val);
											}
											if (commitHistory && !string.IsNullOrWhiteSpace(text))
											{
												RecordResponseForAllNearbySafe(allNpcData, matchedNpc.AgentIndex, matchedNpc.Name, text);
												PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, text, allNpcData);
											}
											if (flag && !IsMeetingSceneConversationReleaseSensitive())
											{
												ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex);
											}
										}
										else if (flag2)
										{
											ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, null);
										}
										else if (flag3)
										{
											ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, null);
										}
										if (flag5)
										{
											ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
											DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
											ShoutUtils.ExecuteDuel(val);
										}
										if (flag6)
										{
											return;
										}
										if (flag7 && preparedRequest != null && string.IsNullOrWhiteSpace(content))
										{
											SchedulePreparedSceneSummonLaunch(preparedRequest, null, "");
										}
										else if (flag8 && preparedRequest2 != null && string.IsNullOrWhiteSpace(content))
										{
											SchedulePreparedSceneGuideLaunch(preparedRequest2, null, "");
										}
									}
									if (flag4 && session != null && string.IsNullOrWhiteSpace(content))
									{
										BeginSceneSummonConversationReturn(session);
									}
								}
							}
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
				Hero val = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				if (val != null)
				{
					MyBehavior.AppendExternalDialogueHistory(val, null, null, extraFact);
				}
			}
		}
	}

	private void RecordPlayerMessage(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "")
	{
		TryInjectSceneRevisitFactsBeforePlayerMessage(nearbyData);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string targetName = ResolveSceneTargetNameForPrompt(primaryTargetAgentIndex, primaryTargetName, nearbyData);
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "user",
				Content = text,
				SpeakerName = "你",
				SpeakerAgentIndex = -1,
				TargetAgentIndex = primaryTargetAgentIndex,
				TargetName = targetName,
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
					TargetName = targetName,
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
				if (nearbyDatum != null)
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
					resolvedHeroes?.TryGetValue(npc.AgentIndex, out hero);
					if (hero == null)
					{
						continue;
					}
					MyBehavior.GetNpcPersonaForExternal(hero, out var p, out var b);
					if (string.IsNullOrWhiteSpace(p) && string.IsNullOrWhiteSpace(b))
					{
						string text = (((MBObjectBase)hero).StringId ?? "").Trim();
						if ((string.IsNullOrWhiteSpace(text) || hashSet.Add(text)) && (!MyBehavior.TryGetNpcPersonaGenerationStatusForExternal(hero, out var needsGeneration, out var _) || needsGeneration))
						{
							InformationManager.DisplayMessage(new InformationMessage(MyBehavior.BuildNpcPersonaGenerationHintForExternal(hero), new Color(1f, 0.85f, 0.3f, 1f)));
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
				if (string.IsNullOrEmpty(key))
				{
					continue;
				}
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
				up = null;
				ub = null;
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
			CultureObject culture = hero.Culture;
			text = ((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "";
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			text2 = ((clan == null) ? null : ((object)clan.Name)?.ToString()) ?? "";
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = hero.MapFaction;
			object obj3 = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
			if (obj3 == null)
			{
				Clan clan2 = hero.Clan;
				if (clan2 == null)
				{
					obj3 = null;
				}
				else
				{
					Kingdom kingdom = clan2.Kingdom;
					obj3 = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString());
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
			}
			value = (string)obj3;
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
			Mission current = Mission.Current;
			Agent obj = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex));
			BasicCharacterObject val = ((obj != null) ? obj.Character : null);
			if (val == null || !val.IsHero)
			{
				return null;
			}
			CharacterObject val2 = (CharacterObject)(object)((val is CharacterObject) ? val : null);
			return (val2 != null) ? val2.HeroObject : null;
		}
		catch
		{
			return null;
		}
	}

	private static Agent ResolveAgentForLocationCharacter(LocationCharacter locationCharacter)
	{
		if (locationCharacter != null)
		{
			Mission current = Mission.Current;
			if (((current != null) ? current.Agents : null) != null)
			{
				IAgentOriginBase agentOrigin = locationCharacter.AgentOrigin;
				if (agentOrigin != null)
				{
					Agent val = ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && a.Origin == agentOrigin);
					if (val != null)
					{
						return val;
					}
				}
				CharacterObject character = locationCharacter.Character;
				if (character == null)
				{
					return null;
				}
				return ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && (object)a.Character == character);
			}
		}
		return null;
	}

	private static Passage FindCurrentScenePassageToLocation(Location targetLocation)
	{
		if (targetLocation == null)
		{
			return null;
		}
		Mission current = Mission.Current;
		MissionAgentHandler val = ((current != null) ? current.GetMissionBehavior<MissionAgentHandler>() : null);
		IEnumerable<UsableMachine> enumerable = Enumerable.Empty<UsableMachine>();
		if (((val != null) ? val.TownPassageProps : null) != null)
		{
			enumerable = enumerable.Concat(val.TownPassageProps);
		}
		if (((val != null) ? val.DisabledPassages : null) != null)
		{
			enumerable = enumerable.Concat(val.DisabledPassages);
		}
		return enumerable.OfType<Passage>().FirstOrDefault((Passage x) => x != null && x.ToLocation == targetLocation);
	}

	private Agent ResolveSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		if (request != null && request.DoorProxyAgentIndex >= 0)
		{
			Mission current = Mission.Current;
			if (((current != null) ? current.Agents : null) != null)
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && a.Index == request.DoorProxyAgentIndex);
				if (val == null || !val.IsActive())
				{
					request.DoorProxyAgentIndex = -1;
					return null;
				}
				return val;
			}
		}
		return null;
	}

	private Agent EnsureSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request, Passage passage, Agent speakerAgent)
	{
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		Agent val = ResolveSceneSummonDoorProxyAgent(request);
		if (val != null)
		{
			return val;
		}
		if (request != null && passage != null)
		{
			Mission current = Mission.Current;
			if (!((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null))
			{
				BasicCharacterObject obj = ((speakerAgent != null) ? speakerAgent.Character : null);
				CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
				if (val2 == null)
				{
					LocationCharacter speakerLocationCharacter = request.SpeakerLocationCharacter;
					val2 = ((speakerLocationCharacter != null) ? speakerLocationCharacter.Character : null);
				}
				if (val2 == null)
				{
					return null;
				}
				Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
				passageWaitingPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(passageWaitingPosition, (BodyFlags)544321929);
				Vec3 val3 = passageWaitingPosition;
				val3.z -= 25f;
				Vec2 val4 = Vec2.Forward;
				try
				{
					Vec2 val5;
					Vec3 val6;
					if (Agent.Main == null || !Agent.Main.IsActive())
					{
						val5 = Vec2.Zero;
					}
					else
					{
						val6 = Agent.Main.Position - passageWaitingPosition;
						val5 = ((Vec3)(ref val6)).AsVec2;
					}
					Vec2 val7 = val5;
					if (((Vec2)(ref val7)).LengthSquared > 0.001f)
					{
						val4 = ((Vec2)(ref val7)).Normalized();
					}
					else if (speakerAgent != null)
					{
						val6 = speakerAgent.LookDirection;
						val4 = ((Vec3)(ref val6)).AsVec2;
					}
				}
				catch
				{
				}
				Equipment val8 = null;
				try
				{
					object obj3;
					if (speakerAgent == null)
					{
						obj3 = null;
					}
					else
					{
						Equipment spawnEquipment = speakerAgent.SpawnEquipment;
						obj3 = ((spawnEquipment != null) ? spawnEquipment.Clone(false) : null);
					}
					val8 = (Equipment)obj3;
				}
				catch
				{
				}
				if (val8 == null)
				{
					try
					{
						Equipment obj5 = (Mission.Current.DoesMissionRequireCivilianEquipment ? ((BasicCharacterObject)val2).FirstCivilianEquipment : ((BasicCharacterObject)val2).FirstBattleEquipment);
						val8 = ((obj5 != null) ? obj5.Clone(false) : null);
					}
					catch
					{
					}
				}
				Team val9 = null;
				if (IsUsableTeam((speakerAgent != null) ? speakerAgent.Team : null))
				{
					val9 = speakerAgent.Team;
				}
				else if (IsUsableTeam(Mission.Current.PlayerTeam))
				{
					val9 = Mission.Current.PlayerTeam;
				}
				else if (IsUsableTeam(Mission.Current.DefenderTeam))
				{
					val9 = Mission.Current.DefenderTeam;
				}
				else if (IsUsableTeam(Mission.Current.AttackerTeam))
				{
					val9 = Mission.Current.AttackerTeam;
				}
				if (!IsUsableTeam(val9))
				{
					return null;
				}
				try
				{
					AgentBuildData val10 = new AgentBuildData((BasicCharacterObject)(object)val2).Team(val9).InitialPosition(ref passageWaitingPosition).InitialDirection(ref val4)
						.NoHorses(true)
						.Controller((AgentControllerType)1);
					if (val8 != null)
					{
						val10 = val10.Equipment(val8);
					}
					Agent val11 = Mission.Current.SpawnAgent(val10, false);
					if (val11 == null)
					{
						return null;
					}
					request.DoorProxyAgentIndex = val11.Index;
					try
					{
						MBAgentVisuals agentVisuals = val11.AgentVisuals;
						if (agentVisuals != null)
						{
							agentVisuals.SetVisible(false);
						}
					}
					catch
					{
					}
					try
					{
						MBAgentVisuals agentVisuals2 = val11.AgentVisuals;
						if (agentVisuals2 != null)
						{
							GameEntity entity = agentVisuals2.GetEntity();
							if (entity != null)
							{
								entity.SetVisibilityExcludeParents(false);
							}
						}
					}
					catch
					{
					}
					try
					{
						val11.TeleportToPosition(val3);
					}
					catch
					{
					}
					try
					{
						val11.SetMaximumSpeedLimit(0f, false);
					}
					catch
					{
					}
					try
					{
						val11.SetIsAIPaused(true);
					}
					catch
					{
					}
					try
					{
						val11.SetMortalityState((MortalityState)1);
					}
					catch
					{
					}
					LogSceneSummonState("cross_scene_proxy_spawned", request, speakerAgent, val11, "waitPos=" + FormatSceneSummonPosition(passageWaitingPosition) + " hiddenPos=" + FormatSceneSummonPosition(val3), force: true);
					return val11;
				}
				catch
				{
					request.DoorProxyAgentIndex = -1;
					return null;
				}
			}
		}
		return null;
	}

	private void CleanupSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		if (request == null)
		{
			return;
		}
		Agent val = ResolveSceneSummonDoorProxyAgent(request);
		request.DoorProxyAgentIndex = -1;
		if (val == null)
		{
			return;
		}
		try
		{
			val.SetIsAIPaused(false);
		}
		catch
		{
		}
		try
		{
			val.SetMaximumSpeedLimit(-1f, false);
		}
		catch
		{
		}
		try
		{
			val.FadeOut(false, true);
		}
		catch
		{
		}
	}

	private Agent ResolveSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		if (job != null && job.DoorProxyAgentIndex >= 0)
		{
			Mission current = Mission.Current;
			if (((current != null) ? current.Agents : null) != null)
			{
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents).FirstOrDefault((Agent a) => a != null && a.Index == job.DoorProxyAgentIndex);
				if (val == null || !val.IsActive())
				{
					job.DoorProxyAgentIndex = -1;
					return null;
				}
				return val;
			}
		}
		return null;
	}

	private Agent EnsureSceneReturnDoorProxyAgent(SceneReturnJob job, Passage passage, Agent movingAgent)
	{
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		Agent val = ResolveSceneReturnDoorProxyAgent(job);
		if (val != null)
		{
			return val;
		}
		if (job != null && passage != null)
		{
			Mission current = Mission.Current;
			if (!((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null))
			{
				BasicCharacterObject obj = ((movingAgent != null) ? movingAgent.Character : null);
				CharacterObject val2 = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
				if (val2 == null)
				{
					LocationCharacter locationCharacter = job.LocationCharacter;
					val2 = ((locationCharacter != null) ? locationCharacter.Character : null);
				}
				if (val2 == null)
				{
					return null;
				}
				Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
				passageWaitingPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(passageWaitingPosition, (BodyFlags)544321929);
				Vec3 val3 = passageWaitingPosition;
				val3.z -= 25f;
				Vec2 val4 = Vec2.Forward;
				try
				{
					Vec2 val5;
					Vec3 val6;
					if (Agent.Main == null || !Agent.Main.IsActive())
					{
						val5 = Vec2.Zero;
					}
					else
					{
						val6 = Agent.Main.Position - passageWaitingPosition;
						val5 = ((Vec3)(ref val6)).AsVec2;
					}
					Vec2 val7 = val5;
					if (((Vec2)(ref val7)).LengthSquared > 0.001f)
					{
						val4 = ((Vec2)(ref val7)).Normalized();
					}
					else if (movingAgent != null)
					{
						val6 = movingAgent.LookDirection;
						val4 = ((Vec3)(ref val6)).AsVec2;
					}
				}
				catch
				{
				}
				Equipment val8 = null;
				try
				{
					object obj3;
					if (movingAgent == null)
					{
						obj3 = null;
					}
					else
					{
						Equipment spawnEquipment = movingAgent.SpawnEquipment;
						obj3 = ((spawnEquipment != null) ? spawnEquipment.Clone(false) : null);
					}
					val8 = (Equipment)obj3;
				}
				catch
				{
				}
				if (val8 == null)
				{
					try
					{
						Equipment obj5 = (Mission.Current.DoesMissionRequireCivilianEquipment ? ((BasicCharacterObject)val2).FirstCivilianEquipment : ((BasicCharacterObject)val2).FirstBattleEquipment);
						val8 = ((obj5 != null) ? obj5.Clone(false) : null);
					}
					catch
					{
					}
				}
				Team val9 = null;
				if (IsUsableTeam((movingAgent != null) ? movingAgent.Team : null))
				{
					val9 = movingAgent.Team;
				}
				else if (IsUsableTeam(Mission.Current.PlayerTeam))
				{
					val9 = Mission.Current.PlayerTeam;
				}
				else if (IsUsableTeam(Mission.Current.DefenderTeam))
				{
					val9 = Mission.Current.DefenderTeam;
				}
				else if (IsUsableTeam(Mission.Current.AttackerTeam))
				{
					val9 = Mission.Current.AttackerTeam;
				}
				if (!IsUsableTeam(val9))
				{
					return null;
				}
				try
				{
					AgentBuildData val10 = new AgentBuildData((BasicCharacterObject)(object)val2).Team(val9).InitialPosition(ref passageWaitingPosition).InitialDirection(ref val4)
						.NoHorses(true)
						.Controller((AgentControllerType)1);
					if (val8 != null)
					{
						val10 = val10.Equipment(val8);
					}
					Agent val11 = Mission.Current.SpawnAgent(val10, false);
					if (val11 == null)
					{
						return null;
					}
					job.DoorProxyAgentIndex = val11.Index;
					try
					{
						MBAgentVisuals agentVisuals = val11.AgentVisuals;
						if (agentVisuals != null)
						{
							agentVisuals.SetVisible(false);
						}
					}
					catch
					{
					}
					try
					{
						MBAgentVisuals agentVisuals2 = val11.AgentVisuals;
						if (agentVisuals2 != null)
						{
							GameEntity entity = agentVisuals2.GetEntity();
							if (entity != null)
							{
								entity.SetVisibilityExcludeParents(false);
							}
						}
					}
					catch
					{
					}
					try
					{
						val11.TeleportToPosition(val3);
					}
					catch
					{
					}
					try
					{
						val11.SetMaximumSpeedLimit(0f, false);
					}
					catch
					{
					}
					try
					{
						val11.SetIsAIPaused(true);
					}
					catch
					{
					}
					try
					{
						val11.SetMortalityState((MortalityState)1);
					}
					catch
					{
					}
					return val11;
				}
				catch
				{
					job.DoorProxyAgentIndex = -1;
					return null;
				}
			}
		}
		return null;
	}

	private void CleanupSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		if (job == null)
		{
			return;
		}
		Agent val = ResolveSceneReturnDoorProxyAgent(job);
		job.DoorProxyAgentIndex = -1;
		if (val == null)
		{
			return;
		}
		try
		{
			val.SetIsAIPaused(false);
		}
		catch
		{
		}
		try
		{
			val.SetMaximumSpeedLimit(-1f, false);
		}
		catch
		{
		}
		try
		{
			val.FadeOut(false, true);
		}
		catch
		{
		}
	}

	private NpcDataPacket BuildSceneNpcDataFromLocationCharacter(LocationCharacter locationCharacter)
	{
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Invalid comparison between Unknown and I4
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Invalid comparison between Unknown and I4
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Invalid comparison between Unknown and I4
		if (((locationCharacter != null) ? locationCharacter.Character : null) == null)
		{
			return null;
		}
		Agent val = ResolveAgentForLocationCharacter(locationCharacter);
		if (val != null)
		{
			return ShoutUtils.ExtractNpcData(val);
		}
		CharacterObject character = locationCharacter.Character;
		NpcDataPacket obj = new NpcDataPacket
		{
			Name = (((object)((BasicCharacterObject)character).Name)?.ToString() ?? "NPC"),
			AgentIndex = -1,
			IsHero = ((BasicCharacterObject)character).IsHero
		};
		CultureObject culture = character.Culture;
		obj.CultureId = ((culture == null) ? null : ((MBObjectBase)culture).StringId?.ToLowerInvariant()) ?? "neutral";
		obj.IsFemale = ((BasicCharacterObject)character).IsFemale;
		float age;
		if (!((BasicCharacterObject)character).IsHero)
		{
			age = 30f;
		}
		else
		{
			Hero heroObject = character.HeroObject;
			age = ((heroObject != null) ? heroObject.Age : 30f);
		}
		obj.Age = age;
		obj.UnnamedKey = "";
		obj.TroopId = "";
		obj.UnnamedRank = "";
		obj.RoleDesc = (((BasicCharacterObject)character).IsHero ? "英雄" : "平民");
		obj.PersonalityDesc = "";
		obj.BackgroundDesc = "";
		NpcDataPacket npcDataPacket = obj;
		if (((BasicCharacterObject)character).IsHero && character.HeroObject != null)
		{
			Hero heroObject2 = character.HeroObject;
			if (heroObject2.IsLord)
			{
				npcDataPacket.RoleDesc = "领主";
			}
			else if (heroObject2.IsWanderer)
			{
				npcDataPacket.RoleDesc = "流浪者";
			}
			else if (heroObject2.IsNotable)
			{
				npcDataPacket.RoleDesc = "要人";
			}
			MyBehavior.GetNpcPersonaForExternal(heroObject2, out var personality, out var background);
			npcDataPacket.PersonalityDesc = (personality ?? "").Trim();
			npcDataPacket.BackgroundDesc = (background ?? "").Trim();
		}
		else
		{
			npcDataPacket.TroopId = (((MBObjectBase)character).StringId ?? "").Trim().ToLowerInvariant();
			if ((int)character.Occupation == 6)
			{
				npcDataPacket.RoleDesc = "村民";
			}
			else if ((int)character.Occupation == 24)
			{
				npcDataPacket.RoleDesc = "守卫";
			}
			else if ((int)character.Occupation == 2)
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
		List<NpcDataPacket> list = (from a in ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>()
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		if (speakerAgent != null && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == speakerAgent.Index))
		{
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(speakerAgent);
			if (npcDataPacket != null)
			{
				list.Add(npcDataPacket);
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
		await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { targetNpc }, (contextHero != null) ? new Dictionary<int, Hero> { [targetNpc.AgentIndex] = contextHero } : new Dictionary<int, Hero>());
		object obj;
		if (targetNpc.AgentIndex < 0)
		{
			obj = null;
		}
		else
		{
			Mission current = Mission.Current;
			obj = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex));
		}
		Agent npcAgent = (Agent)obj;
		BasicCharacterObject obj2 = ((npcAgent != null) ? npcAgent.Character : null);
		object obj3 = ((obj2 is CharacterObject) ? obj2 : null);
		if (obj3 == null)
		{
			obj3 = ((contextHero != null) ? contextHero.CharacterObject : null);
		}
		MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(targetCharacter: (CharacterObject)obj3, kingdomIdOverride: TryGetKingdomIdOverrideFromAgent(npcAgent), targetHero: contextHero, input: "请直接根据刚刚发生的公开互动做出即时反应。", extraFact: null, cultureIdOverride: targetNpc.CultureId ?? "neutral", hasAnyHero: targetNpc.IsHero, targetAgentIndex: targetNpc.AgentIndex, suppressDynamicRuleAndLore: true);
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
		string baseExtrasWithoutTrust;
		string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: stringBuilder.ToString().Trim());
		string text = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
		List<string> historyLines = null;
		lock (_historyLock)
		{
			if (_publicConversationHistory.Count > 0)
			{
				historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, targetNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(targetNpc));
			}
		}
		string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
		string persistedHeroHistory = ((contextHero != null) ? MyBehavior.BuildHistoryContextForExternal(contextHero, 0, "", GetLatestSceneNpcUtterance(targetNpc.AgentIndex)) : "");
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
		int num = text3.IndexOf(':');
		if (num < 0)
		{
			num = text3.IndexOf('：');
		}
		if (num > 0 && num < 30)
		{
			string text4 = text3.Substring(num + 1).Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				text3 = text4;
			}
		}
		text3 = StripLeakedPromptContentForShout(text3);
		text3 = StripStageDirectionsForPassiveShout(text3);
		return text3.Trim();
	}

	private void PrimeSceneSummonArrivalSpeechAsync(ActiveSceneSummonRequest request)
	{
		if (request == null)
		{
			return;
		}
		LocationCharacter targetLocationCharacter = request.TargetLocationCharacter;
		if (((targetLocationCharacter != null) ? targetLocationCharacter.Character : null) == null)
		{
			return;
		}
		Mission current = Mission.Current;
		Agent speakerAgent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex));
		List<NpcDataPacket> sceneSummonArrivalContextSnapshot = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, speakerAgent);
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
		Task.Run(async delegate
		{
			try
			{
				string text2 = await GenerateCompactSceneReactionLineAsync(sceneNpcDataFromLocationCharacter, hero, sceneSummonArrivalContextSnapshot, extraFactLine, singleReplyUserContent);
				request.PreGeneratedArrivalSpeech = text2;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Logger.Log("ShoutBehavior", "[ERROR] PrimeSceneSummonArrivalSpeechAsync failed: " + ex2.Message);
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
		if (sceneNpcDataFromLocationCharacter != null)
		{
			List<NpcDataPacket> list = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, speakerAgent);
			if (!list.Any((NpcDataPacket x) => x != null && x.AgentIndex >= 0 && x.AgentIndex == sceneNpcDataFromLocationCharacter.AgentIndex))
			{
				list.Add(sceneNpcDataFromLocationCharacter);
				ApplySceneLocalDisambiguatedNames(list);
			}
			EnqueueSpeechLine(sceneNpcDataFromLocationCharacter, text, list);
		}
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
			Agent val = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
			if (val != null && val.Index == agentIndex)
			{
				return sceneSummonConversationSession;
			}
			for (int j = 0; j < sceneSummonConversationSession.Participants.Count; j++)
			{
				SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants[j];
				if (sceneSummonConversationParticipant != null)
				{
					Agent val2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
					if (val2 != null && val2.Index == agentIndex)
					{
						return sceneSummonConversationSession;
					}
				}
			}
		}
		return null;
	}

	private void RegisterSceneSummonConversationSession(ActiveSceneSummonRequest request, Agent speakerAgent, Agent targetAgent)
	{
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
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
				return x.SpeakerLocationCharacter == request.SpeakerLocationCharacter || x.Participants.Any((SceneSummonConversationParticipant p) => p != null && p.LocationCharacter == request.TargetLocationCharacter);
			});
			sceneSummonConversationSession = new SceneSummonConversationSession
			{
				BatchId = request.BatchId,
				SpeakerAgentIndex = request.SpeakerAgentIndex,
				SpeakerName = request.SpeakerName,
				SpeakerLocationCharacter = request.SpeakerLocationCharacter,
				OriginalSpeakerLocation = ((request.TargetSourceLocation == request.CurrentLocation) ? request.CurrentLocation : (request.OriginalSpeakerLocation ?? request.CurrentLocation)),
				OriginalSpeakerPosition = (request.OriginalSpeakerPosition ?? ((speakerAgent != null && speakerAgent.IsActive()) ? new Vec3?(speakerAgent.Position) : ((Vec3?)null))),
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
				OriginalLocation = (request.OriginalTargetLocation ?? request.TargetSourceLocation ?? request.CurrentLocation),
				OriginalPosition = (request.OriginalTargetPosition ?? ((targetAgent != null && targetAgent.IsActive()) ? new Vec3?(targetAgent.Position) : ((Vec3?)null)))
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
		NpcDataPacket npcDataPacket = BuildSceneNpcDataFromLocationCharacter(session.SpeakerLocationCharacter);
		if (npcDataPacket != null && npcDataPacket.AgentIndex >= 0)
		{
			list.Add(npcDataPacket);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			NpcDataPacket sceneNpcDataFromLocationCharacter2 = BuildSceneNpcDataFromLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (sceneNpcDataFromLocationCharacter2 != null && sceneNpcDataFromLocationCharacter2.AgentIndex >= 0 && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == sceneNpcDataFromLocationCharacter2.AgentIndex))
			{
				list.Add(sceneNpcDataFromLocationCharacter2);
			}
		}
		int participantCount = Math.Max(1, list.Count);
		for (int num = 0; num < list.Count; num++)
		{
			TrackPlayerInteraction(list[num], participantCount, 15f);
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
		Agent val = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
		if (sceneSummonConversationSession.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(sceneSummonConversationSession);
			if (CanAgentParticipateInSceneSpeech(val) && val != agent)
			{
				StopSceneSummonFollowPlayer(val);
				_sceneFollowReturnStates.Remove(val.Index);
				QueueSceneReturnJob(sceneSummonConversationSession.SpeakerName, sceneSummonConversationSession.SpeakerLocationCharacter, sceneSummonConversationSession.OriginalSpeakerLocation, sceneSummonConversationSession.OriginalSpeakerPosition);
			}
			if (val != null)
			{
				_pendingInteractionTimeoutArms.Remove(val.Index);
				_activeInteractionSessions.Remove(val.Index);
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
		Agent val = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		if (val != null)
		{
			hashSet.Add(val.Index);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			Agent val2 = ResolveAgentForLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (val2 != null)
			{
				hashSet.Add(val2.Index);
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
		if (session == null)
		{
			return;
		}
		ICampaignMission current = CampaignMission.Current;
		if (((current != null) ? current.Location : null) == null)
		{
			return;
		}
		string text = string.Join("、", (from x in session.Participants
			where x != null && !string.IsNullOrWhiteSpace(x.DisplayName)
			select x.DisplayName).Distinct());
		_activeSceneSummonConversationSessions.Remove(session);
		CancelSceneSummonBatch(session.BatchId);
		Agent val = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		StopSceneSummonFollowPlayer(val);
		if (val != null)
		{
			_sceneFollowReturnStates.Remove(val.Index);
		}
		QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
		for (int num = 0; num < session.Participants.Count; num++)
		{
			SceneSummonConversationParticipant sceneSummonConversationParticipant = session.Participants[num];
			if (sceneSummonConversationParticipant != null)
			{
				Agent val2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
				StopSceneSummonFollowPlayer(val2);
				if (val2 != null)
				{
					_sceneFollowReturnStates.Remove(val2.Index);
				}
				QueueSceneReturnJob(sceneSummonConversationParticipant.DisplayName, sceneSummonConversationParticipant.LocationCharacter, sceneSummonConversationParticipant.OriginalLocation, sceneSummonConversationParticipant.OriginalPosition);
			}
		}
		List<int> list = new List<int>();
		if (val != null)
		{
			list.Add(val.Index);
		}
		for (int num2 = 0; num2 < session.Participants.Count; num2++)
		{
			Agent val3 = ResolveAgentForLocationCharacter(session.Participants[num2]?.LocationCharacter);
			if (val3 != null && !list.Contains(val3.Index))
			{
				list.Add(val3.Index);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		ClearPendingSceneConversationAttentionRelease();
		RemoveSceneMovementSuppressionAgents(list);
		ReleaseSceneConversationAttention(list);
		foreach (int item in list)
		{
			_pendingSceneFollowCommands.Remove(item);
			_pendingSceneSummonReturnsAfterSpeech.Remove(item);
			_pendingInteractionTimeoutArms.Remove(item);
			_activeInteractionSessions.Remove(item);
		}
	}

	private void QueueSceneReturnJob(string displayName, LocationCharacter locationCharacter, Location originalLocation, Vec3? originalPosition)
	{
		if (locationCharacter == null)
		{
			return;
		}
		ICampaignMission current = CampaignMission.Current;
		Location val = ((current != null) ? current.Location : null);
		if (val == null)
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
			CurrentLocation = val,
			OriginalLocation = (originalLocation ?? val),
			OriginalPosition = originalPosition,
			ExitPassage = null
		});
	}

	private void PrepareAgentForSceneSummonMovement(Agent agent)
	{
		if (agent != null && agent.IsActive())
		{
			_staringAgents.RemoveAll((Agent a) => a == null || a.Index == agent.Index);
			_staringAgentAnchors.Remove(agent.Index);
			RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
			ReleaseAgentFromSceneConversationLocks(agent);
			RestoreAgentAutonomy(agent);
		}
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
			object obj;
			if (component == null)
			{
				obj = null;
			}
			else
			{
				AgentNavigator agentNavigator = component.AgentNavigator;
				obj = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>() : null);
			}
			DailyBehaviorGroup val = (DailyBehaviorGroup)obj;
			if (((val != null) ? ((AgentBehaviorGroup)val).ScriptedBehavior : null) is ScriptBehavior)
			{
				((AgentBehaviorGroup)val).DisableScriptedBehavior();
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
			agent.SetLookAgent((Agent)null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, false);
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
			agent.SetIsAIPaused(false);
		}
		catch
		{
		}
	}

	private bool NavigateAgentToScenePassage(Agent agent, Passage passage)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive() || passage == null)
		{
			return false;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		return NavigateAgentToWorldPosition(agent, passageWaitingPosition, 0.9f);
	}

	private Vec3 GetPassageWaitingPosition(Passage passage)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		Mission current = Mission.Current;
		if ((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null || passage == null)
		{
			return Vec3.Zero;
		}
		WeakGameEntity gameEntity;
		try
		{
			if (((UsableMachine)passage).PilotStandingPoint != null && ((ScriptComponentBehavior)((UsableMachine)passage).PilotStandingPoint).GameEntity != (GameEntity)null)
			{
				gameEntity = ((ScriptComponentBehavior)((UsableMachine)passage).PilotStandingPoint).GameEntity;
				MatrixFrame globalFrame = ((WeakGameEntity)(ref gameEntity)).GetGlobalFrame();
				Vec3 val = globalFrame.origin;
				Vec3 f = globalFrame.rotation.f;
				if (((Vec3)(ref f)).LengthSquared > 0.0001f)
				{
					((Vec3)(ref f)).Normalize();
					val -= f * 0.35f;
				}
				val.z = Mission.Current.Scene.GetGroundHeightAtPosition(val, (BodyFlags)544321929);
				return val;
			}
		}
		catch
		{
		}
		try
		{
			gameEntity = ((ScriptComponentBehavior)passage).GameEntity;
			Vec3 val = ((WeakGameEntity)(ref gameEntity)).GlobalPosition;
			val.z = Mission.Current.Scene.GetGroundHeightAtPosition(val, (BodyFlags)544321929);
			return val;
		}
		catch
		{
			return Vec3.Zero;
		}
	}

	private bool NavigateAgentToWorldPosition(Agent agent, Vec3 targetPosition, float rangeThreshold = 0.8f, bool doNotRun = false)
	{
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		if (agent != null && agent.IsActive())
		{
			Mission current = Mission.Current;
			if (!((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null))
			{
				try
				{
					PrepareAgentForSceneSummonMovement(agent);
					Vec3 val = targetPosition - agent.Position;
					Vec2 asVec = ((Vec3)(ref val)).AsVec2;
					val = agent.LookDirection;
					Vec2 asVec2 = ((Vec3)(ref val)).AsVec2;
					float rotationInRadians = ((Vec2)(ref asVec2)).RotationInRadians;
					if (((Vec2)(ref asVec)).LengthSquared > 0.0001f)
					{
						rotationInRadians = ((Vec2)(ref asVec)).RotationInRadians;
					}
					targetPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(targetPosition, (BodyFlags)544321929);
					CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
					AgentNavigator val2 = ((component != null) ? component.AgentNavigator : null) ?? ((component != null) ? component.CreateAgentNavigator() : null);
					if (val2 == null)
					{
						return false;
					}
					WorldPosition val3 = default(WorldPosition);
					((WorldPosition)(ref val3))._002Ector(Mission.Current.Scene, targetPosition);
					WorldFrame val4 = default(WorldFrame);
					((WorldFrame)(ref val4))._002Ector(Mat3.CreateMat3WithForward(ref Vec3.Forward), val3);
					try
					{
						Vec2 val5 = default(Vec2);
						((Vec2)(ref val5))._002Ector((float)Math.Cos(rotationInRadians), (float)Math.Sin(rotationInRadians));
						val = ((Vec2)(ref val5)).ToVec3(0f);
						val4 = new WorldFrame(Mat3.CreateMat3WithForward(ref val), val3);
					}
					catch
					{
					}
					ScriptBehavior.AddWorldFrameTarget(agent, val4);
					TrackSceneSummonScriptedAgent(agent);
					try
					{
						WorldPosition origin = val4.Origin;
						agent.SetScriptedPosition(ref origin, false, (AIScriptedFrameFlags)(doNotRun ? 16 : 8));
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
		}
		return false;
	}

	private static void BuildSceneSummonStandPositions(Agent main, out Vec3 primaryPosition, out Vec3 secondaryPosition)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		Vec3 position = main.Position;
		Vec3 lookDirection = main.LookDirection;
		Vec2 asVec = ((Vec3)(ref lookDirection)).AsVec2;
		if (((Vec2)(ref asVec)).LengthSquared < 0.0001f)
		{
			((Vec2)(ref asVec))._002Ector(0f, 1f);
		}
		else
		{
			((Vec2)(ref asVec)).Normalize();
		}
		Vec2 val = default(Vec2);
		((Vec2)(ref val))._002Ector(0f - asVec.y, asVec.x);
		primaryPosition = position + ((Vec2)(ref asVec)).ToVec3(0f) * 1.85f - ((Vec2)(ref val)).ToVec3(0f) * 0.85f;
		secondaryPosition = position + ((Vec2)(ref asVec)).ToVec3(0f) * 1.55f + ((Vec2)(ref val)).ToVec3(0f) * 0.85f;
	}

	private static Vec3 BuildSceneSummonEscortPosition(Agent main, int slotIndex, int slotCount)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Vec3 position = main.Position;
		Vec3 lookDirection = main.LookDirection;
		Vec2 asVec = ((Vec3)(ref lookDirection)).AsVec2;
		if (((Vec2)(ref asVec)).LengthSquared < 0.0001f)
		{
			((Vec2)(ref asVec))._002Ector(0f, 1f);
		}
		else
		{
			((Vec2)(ref asVec)).Normalize();
		}
		Vec2 val = default(Vec2);
		((Vec2)(ref val))._002Ector(0f - asVec.y, asVec.x);
		int num = Math.Max(1, slotCount);
		float num2 = ((num == 1) ? 0f : ((float)slotIndex - (float)(num - 1) * 0.5f));
		float num3 = 1.7f + 0.25f * (float)Math.Min(slotIndex, 3);
		float num4 = 0.85f * num2;
		return position + ((Vec2)(ref asVec)).ToVec3(0f) * num3 + ((Vec2)(ref val)).ToVec3(0f) * num4;
	}

	private static string GetSceneSummonLocationDebugName(Location location)
	{
		string text = (((location != null) ? location.StringId : null) ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (((location == null) ? null : ((object)location.Name)?.ToString()) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "null" : text;
	}

	private static string GetSceneSummonPassageDebugName(Passage passage)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (passage == null)
		{
			return "null";
		}
		string text = "";
		try
		{
			WeakGameEntity gameEntity = ((ScriptComponentBehavior)passage).GameEntity;
			text = (((WeakGameEntity)(ref gameEntity)).Name ?? "").Trim();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ((object)passage).GetType().Name;
		}
		return text + "->" + GetSceneSummonLocationDebugName(passage.ToLocation);
	}

	private static string FormatSceneSummonPosition(Vec3? position)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (!position.HasValue)
		{
			return "null";
		}
		Vec3 value = position.Value;
		return value.x.ToString("F2") + "," + value.y.ToString("F2") + "," + value.z.ToString("F2");
	}

	private string GetSceneSummonAgentDebugState(Agent agent)
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null)
		{
			return "null";
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			object obj;
			if (component == null)
			{
				obj = null;
			}
			else
			{
				AgentNavigator agentNavigator = component.AgentNavigator;
				obj = ((agentNavigator != null) ? agentNavigator.TargetUsableMachine : null);
			}
			UsableMachine val = (UsableMachine)obj;
			string text = (((object)val)?.GetType().Name ?? "none").Trim();
			if (val != null)
			{
				try
				{
					WeakGameEntity gameEntity = ((ScriptComponentBehavior)val).GameEntity;
					string text2 = (((WeakGameEntity)(ref gameEntity)).Name ?? "").Trim();
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
			return "idx=" + agent.Index + " pos=" + FormatSceneSummonPosition(agent.Position) + " using=" + agent.IsUsingGameObject + " curUse=" + (((object)agent.CurrentlyUsedGameObject)?.GetType().Name ?? "none") + " nav=" + text;
		}
		catch (Exception ex)
		{
			return "idx=" + agent.Index + " debug_err=" + ex.GetType().Name;
		}
	}

	private void LogSceneSummonState(string phase, ActiveSceneSummonRequest request, Agent speakerAgent = null, Agent targetAgent = null, string extra = null, bool force = false)
	{
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
		string[] array2 = array;
		foreach (string text in array2)
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
		Match match = SceneSummonActionTagRegex.Match(content);
		if (!match.Success)
		{
			return false;
		}
		content = SceneSummonActionTagRegex.Replace(content, "").Trim();
		List<int> list = ParseSceneSummonPromptIds(match.Groups[1].Value);
		if (list == null || list.Count == 0 || summonTargets == null || summonTargets.Count == 0)
		{
			return false;
		}
		List<SceneSummonPromptTarget> list2 = new List<SceneSummonPromptTarget>();
		HashSet<LocationCharacter> hashSet = new HashSet<LocationCharacter>();
		foreach (int item in list)
		{
			SceneSummonPromptTarget sceneSummonPromptTarget = summonTargets.FirstOrDefault((SceneSummonPromptTarget x) => x != null && x.PromptId == item);
			if (sceneSummonPromptTarget != null && sceneSummonPromptTarget.LocationCharacter != null && hashSet.Add(sceneSummonPromptTarget.LocationCharacter))
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
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		preparedRequest = null;
		LocationComplex current = LocationComplex.Current;
		ICampaignMission current2 = CampaignMission.Current;
		Location val = ((current2 != null) ? current2.Location : null);
		LocationCharacter val2 = ((current != null) ? current.FindCharacter((IAgent)(object)agent) : null);
		if (npc == null || agent == null || !agent.IsActive() || current == null || val == null || val2 == null || targets == null || targets.Count == 0)
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
		List<int> list = (from x in _activeSceneSummonBatches.Values
			where x != null && x.SpeakerAgentIndex == npc.AgentIndex
			select x.BatchId).ToList();
		foreach (int item2 in list)
		{
			CancelSceneSummonBatch(item2);
		}
		SceneSummonBatchState sceneSummonBatchState = new SceneSummonBatchState
		{
			BatchId = _nextSceneSummonBatchId++,
			SpeakerAgentIndex = npc.AgentIndex,
			SpeakerName = ((!string.IsNullOrWhiteSpace(npc.Name)) ? npc.Name : (agent.Name?.ToString() ?? "有人")),
			SpeakerLocationCharacter = val2,
			OriginalSpeakerLocation = val,
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

	private bool TryTriggerSceneGuideAction(NpcDataPacket npc, Agent agent, List<SceneGuidePromptTarget> guideTargets, ref string content, out ActiveSceneGuideRequest preparedRequest)
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
			return false;
		}
		SceneGuidePromptTarget sceneGuidePromptTarget = guideTargets.FirstOrDefault((SceneGuidePromptTarget x) => x != null && x.PromptId == result);
		if (sceneGuidePromptTarget == null)
		{
			return false;
		}
		return StartSceneGuideAction(npc, agent, sceneGuidePromptTarget, out preparedRequest);
	}

	private bool StartSceneGuideAction(NpcDataPacket npc, Agent agent, SceneGuidePromptTarget target, out ActiveSceneGuideRequest preparedRequest)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		preparedRequest = null;
		LocationComplex current = LocationComplex.Current;
		ICampaignMission current2 = CampaignMission.Current;
		Location val = ((current2 != null) ? current2.Location : null);
		LocationCharacter val2 = ((current != null) ? current.FindCharacter((IAgent)(object)agent) : null);
		if (npc == null || agent == null || !agent.IsActive() || target == null || target.LocationCharacter == null || current == null || val == null || val2 == null)
		{
			return false;
		}
		Location val3 = val;
		Vec3? val4 = agent.Position;
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null)
		{
			val3 = value.OriginalLocation ?? val3;
			val4 = value.OriginalPosition ?? val4;
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		}
		CancelSceneGuideActionForAgent(npc.AgentIndex);
		ActiveSceneGuideRequest obj = new ActiveSceneGuideRequest
		{
			GuideAgentIndex = npc.AgentIndex,
			GuideName = ((!string.IsNullOrWhiteSpace(npc.Name)) ? npc.Name : (agent.Name?.ToString() ?? "有人")),
			TargetName = target.DisplayName,
			TargetPromptId = target.PromptId,
			GuideLocationCharacter = val2,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = val,
			OriginalGuideLocation = val3,
			OriginalGuidePosition = val4,
			TargetSourceLocation = target.SourceLocation
		};
		Mission current3 = Mission.Current;
		obj.NextStageMissionTime = ((current3 != null) ? current3.CurrentTime : 0f);
		preparedRequest = obj;
		_activeSceneGuideRequests.Add(preparedRequest);
		return true;
	}

	private void CancelSceneGuideActionForAgent(int guideAgentIndex)
	{
		if (guideAgentIndex >= 0)
		{
			_activeSceneGuideRequests.RemoveAll((ActiveSceneGuideRequest x) => x == null || x.GuideAgentIndex == guideAgentIndex);
			lock (_ttsBubbleSyncLock)
			{
				_pendingSceneGuideLaunchQueues.Remove(guideAgentIndex);
			}
			_pendingSceneGuideReturnsAfterSpeech.Remove(guideAgentIndex);
		}
	}

	private bool TryStartNextSceneSummonBatchRequest(SceneSummonBatchState batch, Agent fallbackSpeakerAgent, bool isInitialRequest, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		if (batch == null || batch.PendingTargets.Count == 0)
		{
			return false;
		}
		Agent val = ResolveAgentForLocationCharacter(batch.SpeakerLocationCharacter) ?? fallbackSpeakerAgent;
		if (!CanAgentParticipateInSceneSpeech(val))
		{
			return false;
		}
		while (batch.PendingTargets.Count > 0)
		{
			SceneSummonPromptTarget target = batch.PendingTargets.Dequeue();
			bool keepMessengerWithTarget = batch.PendingTargets.Count == 0;
			preparedRequest = BuildSceneSummonRequest(batch, val, target, keepMessengerWithTarget, isInitialRequest);
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
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		LocationComplex current = LocationComplex.Current;
		ICampaignMission current2 = CampaignMission.Current;
		Location val = ((current2 != null) ? current2.Location : null);
		if (batch == null || speakerAgent == null || !speakerAgent.IsActive() || target == null || target.LocationCharacter == null || current == null || val == null)
		{
			return null;
		}
		Location val2 = current.GetLocationOfCharacter(target.LocationCharacter) ?? target.SourceLocation;
		if (val2 == null || target.LocationCharacter.Character == null || target.LocationCharacter.Character == CharacterObject.PlayerCharacter || (object)target.LocationCharacter.Character == speakerAgent.Character)
		{
			return null;
		}
		List<Location> list = FindSceneLocationPath(val, val2);
		if (list == null || list.Count == 0)
		{
			return null;
		}
		string targetName = ((!string.IsNullOrWhiteSpace(target.DisplayName)) ? target.DisplayName : (((object)((BasicCharacterObject)target.LocationCharacter.Character).Name)?.ToString() ?? "那个人"));
		string text = BuildSceneSummonBatchTargetSummary(batch, target);
		LocationCharacter val3 = batch.SpeakerLocationCharacter ?? current.FindCharacter((IAgent)(object)speakerAgent);
		if (val3 == null)
		{
			return null;
		}
		if (val2 == val)
		{
			Agent val4 = ResolveAgentForLocationCharacter(target.LocationCharacter);
			if (!CanAgentParticipateInSceneSpeech(val4))
			{
				return null;
			}
			ActiveSceneSummonRequest activeSceneSummonRequest = new ActiveSceneSummonRequest
			{
				BatchId = batch.BatchId,
				SpeakerAgentIndex = batch.SpeakerAgentIndex,
				SpeakerName = batch.SpeakerName,
				TargetPromptId = target.PromptId,
				TargetName = targetName,
				SpeakerLocationCharacter = val3,
				TargetLocationCharacter = target.LocationCharacter,
				CurrentLocation = val,
				OriginalSpeakerLocation = (batch.OriginalSpeakerLocation ?? val),
				OriginalTargetLocation = val2,
				TargetSourceLocation = val2,
				PassageHopLocation = val,
				OriginalSpeakerPosition = (Vec3)(((_003F?)batch.OriginalSpeakerPosition) ?? speakerAgent.Position),
				OriginalTargetPosition = val4.Position,
				NextStageMissionTime = Mission.Current.CurrentTime,
				ArrivalSpeechDeadlineMissionTime = Mission.Current.CurrentTime + 6f,
				Stage = SceneSummonStage.PendingLaunch,
				PendingLaunchStage = SceneSummonStage.MessengerToTarget,
				LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去叫" + text + "了。") : null),
				KeepMessengerWithTarget = keepMessengerWithTarget
			};
			LogSceneSummonState(isInitialRequest ? "start_same_scene" : "start_same_scene_followup", activeSceneSummonRequest, speakerAgent, val4, "pathLen=1 keepMessenger=" + keepMessengerWithTarget, force: true);
			if (!isInitialRequest)
			{
				activeSceneSummonRequest.NextStageMissionTime = Mission.Current.CurrentTime + 0.25f;
			}
			return activeSceneSummonRequest;
		}
		Location val5 = list[1];
		Passage val6 = FindCurrentScenePassageToLocation(val5);
		if (val6 == null)
		{
			return null;
		}
		ActiveSceneSummonRequest activeSceneSummonRequest2 = new ActiveSceneSummonRequest
		{
			BatchId = batch.BatchId,
			SpeakerAgentIndex = batch.SpeakerAgentIndex,
			SpeakerName = batch.SpeakerName,
			TargetPromptId = target.PromptId,
			TargetName = targetName,
			SpeakerLocationCharacter = val3,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = val,
			OriginalSpeakerLocation = (batch.OriginalSpeakerLocation ?? val),
			OriginalTargetLocation = val2,
			TargetSourceLocation = val2,
			PassageHopLocation = val5,
			OriginalSpeakerPosition = (Vec3)(((_003F?)batch.OriginalSpeakerPosition) ?? speakerAgent.Position),
			OriginalTargetPosition = null,
			MessengerDoorPassage = val6,
			NextStageMissionTime = Mission.Current.CurrentTime + (isInitialRequest ? 2.25f : 0.25f),
			ArrivalSpeechDeadlineMissionTime = Mission.Current.CurrentTime + 8f,
			Stage = SceneSummonStage.PendingLaunch,
			PendingLaunchStage = SceneSummonStage.MessengerToDoor,
			LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去帮你叫" + text + "了。") : null),
			KeepMessengerWithTarget = keepMessengerWithTarget
		};
		LogSceneSummonState(isInitialRequest ? "start_cross_scene" : "start_cross_scene_followup", activeSceneSummonRequest2, speakerAgent, null, "pathLen=" + list.Count + " keepMessenger=" + keepMessengerWithTarget, force: true);
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
			}
			else
			{
				SceneSummonStage stage = activeSceneSummonRequest.Stage;
				if (1 == 0)
				{
				}
				bool flag = stage switch
				{
					SceneSummonStage.PendingLaunch => TickSceneSummonPendingLaunchStage(activeSceneSummonRequest, currentTime), 
					SceneSummonStage.MessengerToTarget => TickSceneSummonMessengerToTargetStage(activeSceneSummonRequest, currentTime), 
					SceneSummonStage.MessengerToDoor => TickSceneSummonMessengerStage(activeSceneSummonRequest, currentTime), 
					SceneSummonStage.WaitingForTarget => TickSceneSummonWaitStage(activeSceneSummonRequest, currentTime), 
					SceneSummonStage.TargetToPlayer => TickSceneSummonTargetStage(activeSceneSummonRequest, currentTime), 
					_ => true, 
				};
				if (1 == 0)
				{
				}
				if (flag)
				{
					CleanupSceneSummonDoorProxyAgent(activeSceneSummonRequest);
					_activeSceneSummonRequests.RemoveAt(num);
				}
			}
		}
	}

	private static bool IsSceneSummonRequestStillValid(ActiveSceneSummonRequest request)
	{
		int result;
		if (request != null && request.TargetLocationCharacter != null)
		{
			ICampaignMission current = CampaignMission.Current;
			result = ((((current != null) ? current.Location : null) == request.CurrentLocation) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
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
		LogSceneSummonState("pending_launch_scheduled", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "delay=" + num2.ToString("F2") + " ttsAccepted=" + ((playbackInfo != null) ? playbackInfo.TtsAccepted.ToString() : "False") + " waitForFinish=" + ((playbackInfo != null) ? playbackInfo.WaitForPlaybackFinished.ToString() : "False") + " speechLen=" + (spokenText ?? "").Length, force: true);
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
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		if (request == null || currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex));
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(val) || main == null || !main.IsActive())
		{
			return true;
		}
		Vec3 position = val.Position;
		if (((Vec3)(ref position)).DistanceSquared(main.Position) > 100f)
		{
			if (request.PlayerOutOfRangeSinceMissionTime < 0f)
			{
				request.PlayerOutOfRangeSinceMissionTime = currentTime;
			}
			else if (currentTime - request.PlayerOutOfRangeSinceMissionTime >= 15f)
			{
				TriggerSceneGuideTimeoutAndReturn(request);
				return true;
			}
			return false;
		}
		request.PlayerOutOfRangeSinceMissionTime = -1f;
		ICampaignMission current2 = CampaignMission.Current;
		Location val2 = ((current2 != null) ? current2.Location : null);
		if (val2 == null || request.TargetSourceLocation == null)
		{
			return true;
		}
		if (request.TargetSourceLocation == val2)
		{
			Agent val3 = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
			if (!CanAgentParticipateInSceneSpeech(val3))
			{
				return true;
			}
			NavigateAgentToWorldPosition(val, val3.Position, 1f);
			position = val.Position;
			if (((Vec3)(ref position)).DistanceSquared(val3.Position) <= 36f)
			{
				TriggerSceneGuideArrivalAndReturn(request, reachedDoor: false);
				return true;
			}
			return false;
		}
		List<Location> list = FindSceneLocationPath(val2, request.TargetSourceLocation);
		if (list == null || list.Count < 2)
		{
			return true;
		}
		Location targetLocation = list[1];
		Passage val4 = request.TargetDoorPassage ?? FindCurrentScenePassageToLocation(targetLocation);
		if (val4 == null)
		{
			return true;
		}
		request.TargetDoorPassage = val4;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(val4);
		NavigateAgentToWorldPosition(val, passageWaitingPosition, 0.9f);
		Vec2 asVec = ((Vec3)(ref passageWaitingPosition)).AsVec2;
		position = val.Position;
		if (((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2) <= 36f)
		{
			TriggerSceneGuideArrivalAndReturn(request, reachedDoor: true);
			return true;
		}
		return false;
	}

	private void TriggerSceneGuideArrivalAndReturn(ActiveSceneGuideRequest request, bool reachedDoor)
	{
		if (request != null && request.GuideAgentIndex >= 0)
		{
			string text = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "此人";
			}
			string factText = (reachedDoor ? ("[AFEF NPC行为补充] 你已把" + text + "带到了前往" + request.TargetName + "所在之处的门口。") : ("[AFEF NPC行为补充] 你已把" + text + "带到了" + request.TargetName + "身边。"));
			ScheduleSceneGuideReturnAfterNextSpeech(request);
			TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, 0.5f);
		}
	}

	private void TriggerSceneGuideTimeoutAndReturn(ActiveSceneGuideRequest request)
	{
		if (request != null && request.GuideAgentIndex >= 0)
		{
			string text = GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "此人";
			}
			string factText = "[AFEF NPC行为补充] " + text + "长时间没有跟上你，你决定不再继续带路，先回去忙自己的事。";
			ScheduleSceneGuideReturnAfterNextSpeech(request);
			TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, 0.5f);
		}
	}

	private void ScheduleSceneGuideReturnAfterNextSpeech(ActiveSceneGuideRequest request)
	{
		if (request != null && request.GuideAgentIndex >= 0 && request.GuideLocationCharacter != null)
		{
			_pendingSceneGuideReturnsAfterSpeech[request.GuideAgentIndex] = new PendingSceneGuideReturnAfterSpeech
			{
				AgentIndex = request.GuideAgentIndex,
				DisplayName = request.GuideName,
				LocationCharacter = request.GuideLocationCharacter,
				OriginalLocation = request.OriginalGuideLocation,
				OriginalPosition = request.OriginalGuidePosition
			};
		}
	}

	private bool TickSceneSummonPendingLaunchStage(ActiveSceneSummonRequest request, float currentTime)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
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
			InformationManager.DisplayMessage(new InformationMessage(request.LaunchAnnouncement, new Color(1f, 0.85f, 0.35f, 1f)));
		}
		LogSceneSummonState("pending_launch_begin", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "launchStage=" + request.PendingLaunchStage, force: true);
		return false;
	}

	private bool TickSceneSummonMessengerToTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex));
		Agent val2 = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		if (!CanAgentParticipateInSceneSpeech(val) || !CanAgentParticipateInSceneSpeech(val2))
		{
			LogSceneSummonState("same_scene_abort", request, val, val2, "speakerOrTargetInactive", force: true);
			return true;
		}
		Vec3 position = val.Position;
		if (((Vec3)(ref position)).DistanceSquared(val2.Position) <= 100f)
		{
			try
			{
				val.SetLookAgent(val2);
			}
			catch
			{
			}
			request.Stage = SceneSummonStage.TargetToPlayer;
			request.NextStageMissionTime = currentTime + 0.25f;
			TryAdvanceSceneSummonBatch(request, val);
			LogSceneSummonState("same_scene_reached_target", request, val, val2, null, force: true);
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(val);
			ScriptBehavior.AddAgentTarget(val, val2);
			TrackSceneSummonScriptedAgent(val);
		}
		catch
		{
		}
		ActiveSceneSummonRequest request2 = request;
		position = val.Position;
		LogSceneSummonState("same_scene_moving_to_target", request2, val, val2, "targetDistanceSq=" + ((Vec3)(ref position)).DistanceSquared(val2.Position).ToString("F2"));
		return false;
	}

	private bool TickSceneSummonMessengerStage(ActiveSceneSummonRequest request, float currentTime)
	{
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex));
		if (val == null || !val.IsActive())
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + 2.25f;
			LogSceneSummonState("cross_scene_speaker_missing", request, val, null, "fallback_to_wait", force: true);
			return false;
		}
		Passage val2 = request.MessengerDoorPassage ?? FindCurrentScenePassageToLocation(request.PassageHopLocation);
		if (val2 == null)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			LogSceneSummonState("cross_scene_no_passage", request, val, null, null, force: true);
			return true;
		}
		request.MessengerDoorPassage = val2;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(val2);
		Agent val3 = EnsureSceneSummonDoorProxyAgent(request, val2, val);
		Vec2 asVec = ((Vec3)(ref passageWaitingPosition)).AsVec2;
		Vec3 position = val.Position;
		float num = ((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2);
		if (num <= 9f)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + 2.25f;
			TryAdvanceSceneSummonBatch(request, val);
			LogSceneSummonState("cross_scene_reached_door", request, val, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			return false;
		}
		if (val3 != null && val3.IsActive())
		{
			try
			{
				PrepareAgentForSceneSummonMovement(val);
				ScriptBehavior.AddAgentTarget(val, val3);
				TrackSceneSummonScriptedAgent(val);
			}
			catch
			{
			}
			LogSceneSummonState("cross_scene_reissue_proxy_target", request, val, val3, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
		}
		else
		{
			LogSceneSummonState("cross_scene_reissue_passage_target", request, val, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			NavigateAgentToScenePassage(val, val2);
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
		LocationComplex current = LocationComplex.Current;
		if (current == null || request.PassageHopLocation == null)
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
		LocationComplex current = LocationComplex.Current;
		Mission current2 = Mission.Current;
		MissionAgentHandler val = ((current2 != null) ? current2.GetMissionBehavior<MissionAgentHandler>() : null);
		if (locationCharacter == null || current == null || currentLocation == null || visibleFromLocation == null || val == null)
		{
			return;
		}
		Agent val2 = ResolveAgentForLocationCharacter(locationCharacter);
		Location val3 = current.GetLocationOfCharacter(locationCharacter);
		if (val3 != visibleFromLocation)
		{
			current.ChangeLocation(locationCharacter, val3, visibleFromLocation);
			val3 = visibleFromLocation;
		}
		if (val3 != currentLocation)
		{
			current.ChangeLocation(locationCharacter, val3, currentLocation);
		}
		if (val2 != null && val2.IsActive())
		{
			return;
		}
		try
		{
			val.SpawnEnteringLocationCharacter(locationCharacter, visibleFromLocation);
		}
		catch
		{
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
		if (agentIndex >= 0)
		{
			SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agentIndex);
			if (sceneSummonConversationSession != null)
			{
				RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
			}
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
			if (activeSceneGuideRequest != null)
			{
				ICampaignMission current = CampaignMission.Current;
				if (((current != null) ? current.Location : null) == activeSceneGuideRequest.CurrentLocation)
				{
					if (TickSceneGuideRequest(activeSceneGuideRequest, currentTime))
					{
						_activeSceneGuideRequests.RemoveAt(num);
					}
					continue;
				}
			}
			_activeSceneGuideRequests.RemoveAt(num);
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
			Queue<ActiveSceneSummonRequest> value;
			return _pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out value) && value != null && value.Count > 0;
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
			Queue<ActiveSceneGuideRequest> value;
			return _pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out value) && value != null && value.Count > 0;
		}
	}

	private void UpdateSceneSummonConversationEscortMovement()
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		Agent main = Agent.Main;
		if (main == null || !main.IsActive() || _activeSceneSummonConversationSessions.Count == 0)
		{
			return;
		}
		foreach (SceneSummonConversationSession activeSceneSummonConversationSession in _activeSceneSummonConversationSessions)
		{
			if (activeSceneSummonConversationSession == null)
			{
				continue;
			}
			List<Agent> list = new List<Agent>();
			for (int i = 0; i < activeSceneSummonConversationSession.Participants.Count; i++)
			{
				Agent val = ResolveAgentForLocationCharacter(activeSceneSummonConversationSession.Participants[i]?.LocationCharacter);
				if (CanAgentParticipateInSceneSpeech(val) && val != main)
				{
					list.Add(val);
				}
			}
			if (activeSceneSummonConversationSession.KeepSpeakerNearby)
			{
				Agent val2 = ResolveAgentForLocationCharacter(activeSceneSummonConversationSession.SpeakerLocationCharacter);
				if (CanAgentParticipateInSceneSpeech(val2) && val2 != main && !list.Contains(val2))
				{
					list.Add(val2);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				Vec3 position = list[j].Position;
				if (!(((Vec3)(ref position)).DistanceSquared(main.Position) <= 25f))
				{
					Vec3 targetPosition = BuildSceneSummonEscortPosition(main, j, list.Count);
					TryGuideSummonParticipantToPosition(list[j], targetPosition);
				}
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
		if (_activeSceneReturnJobs.Count == 0)
		{
			return;
		}
		ICampaignMission current = CampaignMission.Current;
		if (((current != null) ? current.Location : null) == null)
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
			}
			else if (TickSceneReturnJob(sceneReturnJob))
			{
				CleanupSceneReturnDoorProxyAgent(sceneReturnJob);
				_activeSceneReturnJobs.RemoveAt(num);
			}
		}
	}

	private bool TickSceneReturnJob(SceneReturnJob job)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		LocationComplex current = LocationComplex.Current;
		ICampaignMission current2 = CampaignMission.Current;
		Location val = ((current2 != null) ? current2.Location : null);
		if (job == null || current == null || val == null)
		{
			return true;
		}
		Location val2 = job.OriginalLocation ?? val;
		Agent val3 = ResolveAgentForLocationCharacter(job.LocationCharacter);
		Vec3 position;
		if (val2 == val)
		{
			if (!job.OriginalPosition.HasValue || !CanAgentParticipateInSceneSpeech(val3))
			{
				if (val3 != null)
				{
					RestoreAgentAutonomy(val3);
				}
				return true;
			}
			ApplySceneReturnWalkPacing(val3);
			NavigateAgentToWorldPosition(val3, job.OriginalPosition.Value, 0.6f, doNotRun: true);
			position = val3.Position;
			if (((Vec3)(ref position)).DistanceSquared(job.OriginalPosition.Value) > 9f)
			{
				return false;
			}
			RestoreAgentAutonomy(val3);
			return true;
		}
		if (!CanAgentParticipateInSceneSpeech(val3))
		{
			return true;
		}
		List<Location> list = FindSceneLocationPath(val, val2);
		if (list == null || list.Count < 2)
		{
			return true;
		}
		Location val4 = list[1];
		Passage val5 = job.ExitPassage ?? FindCurrentScenePassageToLocation(val4);
		if (val5 == null)
		{
			return true;
		}
		job.ExitPassage = val5;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(val5);
		Agent val6 = EnsureSceneReturnDoorProxyAgent(job, val5, val3);
		Vec2 asVec = ((Vec3)(ref passageWaitingPosition)).AsVec2;
		position = val3.Position;
		if (((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2) > 2.25f)
		{
			ApplySceneReturnWalkPacing(val3);
			NavigateAgentToWorldPosition(val3, passageWaitingPosition, 0.9f, doNotRun: true);
			return false;
		}
		CleanupSceneReturnDoorProxyAgent(job);
		current.ChangeLocation(job.LocationCharacter, val, val4);
		if (val2 != val4)
		{
			current.ChangeLocation(job.LocationCharacter, val4, val2);
		}
		try
		{
			val3.FadeOut(false, true);
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
			agent.SetMaximumSpeedLimit(1.55f, false);
		}
		catch
		{
		}
	}

	private bool TryGuideSummonParticipantToPosition(Agent agent, Vec3 targetPosition)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (CanAgentParticipateInSceneSpeech(agent))
		{
			Mission current = Mission.Current;
			if (!((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null))
			{
				targetPosition.z = Mission.Current.Scene.GetGroundHeightAtPosition(targetPosition, (BodyFlags)544321929);
				NavigateAgentToWorldPosition(agent, targetPosition, 0.75f);
				Vec3 position = agent.Position;
				return ((Vec3)(ref position)).DistanceSquared(targetPosition) <= 9f;
			}
		}
		return false;
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
		if (CanAgentParticipateInSceneSpeech(agent) && (overwriteExisting || !_sceneFollowReturnStates.ContainsKey(agent.Index)))
		{
			SceneFollowReturnState sceneFollowReturnState = BuildSceneFollowReturnState(agent);
			if (sceneFollowReturnState != null)
			{
				_sceneFollowReturnStates[agent.Index] = sceneFollowReturnState;
			}
		}
	}

	private SceneFollowReturnState BuildSceneFollowReturnState(Agent agent)
	{
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			LocationComplex current = LocationComplex.Current;
			ICampaignMission current2 = CampaignMission.Current;
			Location val = ((current2 != null) ? current2.Location : null);
			if (!CanAgentParticipateInSceneSpeech(agent) || current == null || val == null)
			{
				return null;
			}
			LocationCharacter locationCharacter = current.FindCharacter((IAgent)(object)agent);
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
						OriginalLocation = (sceneSummonConversationParticipant.OriginalLocation ?? val),
						OriginalPosition = sceneSummonConversationParticipant.OriginalPosition
					};
				}
				if (sceneSummonConversationSession.SpeakerLocationCharacter == locationCharacter)
				{
					return new SceneFollowReturnState
					{
						DisplayName = sceneSummonConversationSession.SpeakerName,
						LocationCharacter = locationCharacter,
						OriginalLocation = (sceneSummonConversationSession.OriginalSpeakerLocation ?? val),
						OriginalPosition = sceneSummonConversationSession.OriginalSpeakerPosition
					};
				}
			}
			return new SceneFollowReturnState
			{
				DisplayName = (agent.Name?.ToString() ?? "NPC"),
				LocationCharacter = locationCharacter,
				OriginalLocation = val,
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
				return;
			}
			catch
			{
				return;
			}
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
			AgentNavigator val = ((component != null) ? component.AgentNavigator : null);
			DailyBehaviorGroup val2 = ((val != null) ? val.GetBehaviorGroup<DailyBehaviorGroup>() : null);
			if (val2 == null)
			{
				return false;
			}
			FollowAgentBehavior val3 = ((AgentBehaviorGroup)val2).GetBehavior<FollowAgentBehavior>() ?? ((AgentBehaviorGroup)val2).AddBehavior<FollowAgentBehavior>();
			((AgentBehaviorGroup)val2).SetScriptedBehavior<FollowAgentBehavior>();
			((AgentBehavior)val3).IsActive = true;
			val3.SetTargetAgent(target);
			ScriptBehavior behavior = ((AgentBehaviorGroup)val2).GetBehavior<ScriptBehavior>();
			if (behavior != null)
			{
				((AgentBehavior)behavior).IsActive = false;
			}
			WalkingBehavior behavior2 = ((AgentBehaviorGroup)val2).GetBehavior<WalkingBehavior>();
			if (behavior2 != null)
			{
				((AgentBehavior)behavior2).IsActive = false;
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
			CampaignAgentComponent val = ((agent != null) ? agent.GetComponent<CampaignAgentComponent>() : null);
			AgentNavigator val2 = ((val != null) ? val.AgentNavigator : null);
			DailyBehaviorGroup val3 = ((val2 != null) ? val2.GetBehaviorGroup<DailyBehaviorGroup>() : null);
			if (val3 == null)
			{
				return;
			}
			((AgentBehaviorGroup)val3).RemoveBehavior<FollowAgentBehavior>();
			if (restoreDailyBehaviors)
			{
				ScriptBehavior behavior = ((AgentBehaviorGroup)val3).GetBehavior<ScriptBehavior>();
				if (behavior != null)
				{
					((AgentBehavior)behavior).IsActive = true;
				}
				WalkingBehavior val4 = ((AgentBehaviorGroup)val3).GetBehavior<WalkingBehavior>() ?? ((AgentBehaviorGroup)val3).AddBehavior<WalkingBehavior>();
				((AgentBehavior)val4).IsActive = true;
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
			LocationComplex current = LocationComplex.Current;
			if (agent == null || locationEncounter == null || current == null)
			{
				return false;
			}
			LocationCharacter val = current.FindCharacter((IAgent)(object)agent);
			if (val == null)
			{
				return false;
			}
			if (isFollowing)
			{
				AccompanyingCharacter accompanyingCharacter = locationEncounter.GetAccompanyingCharacter(val);
				if (accompanyingCharacter == null || !accompanyingCharacter.IsFollowingPlayerAtMissionStart)
				{
					locationEncounter.RemoveAccompanyingCharacter(val);
					locationEncounter.AddAccompanyingCharacter(val, true);
				}
			}
			else
			{
				locationEncounter.RemoveAccompanyingCharacter(val);
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
			LocationComplex current = LocationComplex.Current;
			if (locationEncounter == null || current == null)
			{
				return false;
			}
			LocationCharacter val = current.FindCharacter((IAgent)(object)agent);
			AccompanyingCharacter val2 = ((val != null) ? locationEncounter.GetAccompanyingCharacter(val) : null);
			return val2 != null && val2.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}

	private bool TickSceneSummonTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Expected O, but got Unknown
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Expected O, but got Unknown
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Agent val = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		Agent val2 = ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter);
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(val) || main == null || !main.IsActive())
		{
			LogSceneSummonState("target_to_player_wait_abort", request, val2, val, "targetOrPlayerUnavailable", currentTime >= request.NextStageMissionTime + 6f);
			return currentTime >= request.NextStageMissionTime + 6f;
		}
		BuildSceneSummonStandPositions(main, out var primaryPosition, out var secondaryPosition);
		bool flag = TryGuideSummonParticipantToPosition(val, primaryPosition);
		bool flag2 = true;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(val2) && val2 != val)
		{
			flag2 = TryGuideSummonParticipantToPosition(val2, secondaryPosition);
		}
		if (!flag)
		{
			LogSceneSummonState("target_to_player_moving", request, val2, val, "targetArrived=" + flag + " messengerArrived=" + flag2 + " targetStand=" + FormatSceneSummonPosition(primaryPosition) + " messengerStand=" + FormatSceneSummonPosition(secondaryPosition));
			return false;
		}
		if (!request.ArrivalSpeechConsumed && string.IsNullOrWhiteSpace(request.PreGeneratedArrivalSpeech) && currentTime < request.ArrivalSpeechDeadlineMissionTime)
		{
			LogSceneSummonState("target_to_player_waiting_speech", request, val2, val, "deadline=" + request.ArrivalSpeechDeadlineMissionTime.ToString("F2"));
			return false;
		}
		ForceAgentFacePlayer(val);
		bool flag3 = request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(val2) && val2 != val && flag2;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(val2) && val2 != val && !flag2)
		{
			RestoreAgentAutonomy(val2);
		}
		if (flag3)
		{
			ForceAgentFacePlayer(val2);
			PlaySceneSummonArrivalSpeechIfReady(request, val2);
			RegisterSceneSummonConversationSession(request, val2, val);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_pair", request, val2, val, null, force: true);
			string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			InformationManager.DisplayMessage(new InformationMessage(request.SpeakerName + " 带着" + text + "过来了。", new Color(0.75f, 1f, 0.75f, 1f)));
		}
		else
		{
			PlaySceneSummonArrivalSpeechIfReady(request, val2 ?? val);
			RegisterSceneSummonConversationSession(request, val2, val);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_single", request, val2, val, null, force: true);
			string text2 = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			InformationManager.DisplayMessage(new InformationMessage(text2 + " 被叫过来了。", new Color(0.75f, 1f, 0.75f, 1f)));
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
		int num = history.Count - 1;
		while (num >= 0 && list.Count < 20)
		{
			ConversationMessage msg = history[num];
			if (IsSceneHistoryVisibleToAgent(msg, viewerAgentIndex) && TryRenderSceneHistoryLine(msg, null, out var rendered, viewerAgentIndex, targetNpcName, useNpcNameAddress))
			{
				list.Add(rendered);
			}
			num--;
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
			bool flag = false;
			for (int num = _publicConversationHistory.Count - 1; num >= 0; num--)
			{
				ConversationMessage conversationMessage = _publicConversationHistory[num];
				if (conversationMessage != null)
				{
					string text = (conversationMessage.Role ?? "").Trim();
					if (text.Equals("user", StringComparison.OrdinalIgnoreCase))
					{
						if (flag)
						{
							break;
						}
						flag = true;
					}
					else if (flag && text.Equals("assistant", StringComparison.OrdinalIgnoreCase) && conversationMessage.SpeakerAgentIndex == targetAgentIndex)
					{
						string text2 = (conversationMessage.Content ?? "").Replace("\r", "").Trim();
						if (!string.IsNullOrWhiteSpace(text2) && !IsLeakedPromptLineForShout(text2))
						{
							return text2;
						}
					}
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
			Mission current = Mission.Current;
			Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()));
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket != null)
			{
				RecordExtraFactToSceneHistory(factText, new List<NpcDataPacket> { npcDataPacket });
				if (triggerImmediateReaction)
				{
					TriggerImmediateSceneBehaviorReaction(factText, targetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds, skipSceneFactRecord: true);
				}
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
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()));
			if (!CanAgentParticipateInSceneSpeech(val))
			{
				return;
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(val);
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
			if (persistHeroPrivateHistory)
			{
				BasicCharacterObject character = val.Character;
				CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val2 != null && val2.HeroObject != null)
				{
					MyBehavior.AppendExternalDialogueHistory(val2.HeroObject, null, null, text);
				}
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
			Agent val = nearbyNPCAgents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()) ?? ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (!CanAgentParticipateInSceneSpeech(val))
			{
				return;
			}
			if (!suppressStare)
			{
				ResetStaringForActiveInteraction(nearbyNPCAgents, val);
			}
			List<NpcDataPacket> list = (from a in ShoutUtils.GetNearbyNPCAgents()
				select ShoutUtils.ExtractNpcData(a) into d
				where d != null
				select d).ToList();
			NpcDataPacket npcDataPacket = list.FirstOrDefault((NpcDataPacket d) => d != null && d.AgentIndex == targetAgentIndex) ?? ShoutUtils.ExtractNpcData(val);
			if (npcDataPacket == null)
			{
				return;
			}
			if (IsAgentHostileToMainAgent(val))
			{
				_activeInteractionSessions.Remove(targetAgentIndex);
				_pendingInteractionTimeoutArms.Remove(targetAgentIndex);
				RefreshHostileCombatAgentAutonomy(val);
			}
			else
			{
				TrackPlayerInteraction(npcDataPacket, list?.Count ?? 0, postSpeechLeaveSeconds, returnSceneSummonOnTimeout);
			}
			if (!list.Any((NpcDataPacket d) => d != null && d.AgentIndex == targetAgentIndex))
			{
				list.Insert(0, npcDataPacket);
			}
			if (!skipSceneFactRecord)
			{
				RecordExtraFactToSceneHistory(factText, list);
			}
			if (persistHeroPrivateHistory)
			{
				BasicCharacterObject character = val.Character;
				CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val2 != null && val2.HeroObject != null)
				{
					MyBehavior.AppendExternalDialogueHistory(val2.HeroObject, null, null, factText);
				}
			}
			Dictionary<int, Hero> dictionary = new Dictionary<int, Hero>();
			foreach (NpcDataPacket item in list)
			{
				if (item != null && item.IsHero)
				{
					Hero val3 = ResolveHeroFromAgentIndex(item.AgentIndex);
					if (val3 != null)
					{
						dictionary[item.AgentIndex] = val3;
					}
				}
			}
			Task.Run(async delegate
			{
				try
				{
					await GenerateImmediateSceneBehaviorReactionAsync(npcDataPacket, list, dictionary, suppressStare);
				}
				catch (Exception ex2)
				{
					Exception ex3 = ex2;
					Logger.Log("ShoutBehavior", "[ERROR] TriggerImmediateSceneBehaviorReaction background failed: " + ex3.Message);
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
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			if (val == null || (NativeObject)(object)val.AgentVisuals == (NativeObject)null)
			{
				LogTtsReport("StopRhubarbRecord.Skip", agentIndex, $"reason={reason};agentMissing={val == null};visualsMissing={val != null && (NativeObject)(object)val.AgentVisuals == (NativeObject)null}");
				return;
			}
			LogLipSyncNativeProbe("StopRhubarbRecord.Before", agentIndex, "reason=" + reason);
			val.AgentVisuals.StartRhubarbRecord("", -1);
			LogLipSyncNativeProbe("StopRhubarbRecord.After", agentIndex, "reason=" + reason);
			Logger.Log("LipSync", $"[Rhubarb] StopRhubarbRecord called, agentIndex={agentIndex}, reason={reason}");
			LogTtsReport("StopRhubarbRecord", agentIndex, "reason=" + reason);
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] StopRhubarbRecord failed, agentIndex={agentIndex}, reason={reason}, error={ex.Message}");
			LogTtsReport("StopRhubarbRecord.Failed", agentIndex, "reason=" + reason + ";error=" + ex.Message);
			BannerlordExceptionSentinel.ReportObservedException("LipSync.StopRhubarbRecord", ex, $"agentIndex={agentIndex};reason={reason}");
		}
	}

	private void DetachAgentLipSyncForSafety(int agentIndex, string reason)
	{
		if (agentIndex >= 0)
		{
			bool flag = false;
			bool flag2 = false;
			lock (_speakingLock)
			{
				flag = _speakingAgentIndices.Remove(agentIndex);
				flag2 = !_agentLipSyncDetachedForSafety.Add(agentIndex);
			}
			StopAgentRhubarbRecordIfPossible(agentIndex, "SafetyDetach:" + reason);
			Logger.Log("LipSync", $"[SAFEGUARD] Detached scene lipsync for agentIndex={agentIndex}, reason={reason}, hadSpeaking={flag}, alreadyDetached={flag2}");
			LogTtsReport("LipSyncSafeguard.Detached", agentIndex, $"reason={reason};hadSpeaking={flag};alreadyDetached={flag2}");
		}
	}

	private void InterruptAgentSpeechForCombat(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool flag = false;
		SoundEvent value = null;
		string value2 = null;
		string value3 = null;
		lock (_speakingLock)
		{
			flag = _speakingAgentIndices.Remove(agentIndex);
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
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
			_pendingSceneDialogueFeedQueues.Remove(agentIndex);
		}
		_pendingInteractionTimeoutArms.Remove(agentIndex);
		_activeInteractionSessions.Remove(agentIndex);
		bool flag2 = false;
		try
		{
			flag2 = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			flag2 = false;
		}
		if (!flag && value == null && string.IsNullOrWhiteSpace(value2) && string.IsNullOrWhiteSpace(value3) && !flag2)
		{
			if (ShouldSuppressSceneConversationControlForMeeting())
			{
				StopAutoGroupChatSession("meeting_combat_interrupt");
				ClearQueuedSceneSpeech();
				ClearMeetingSceneConversationControlState();
			}
			return;
		}
		LogTtsReport("InterruptAgentSpeechForCombat", agentIndex, $"reason={reason};speaking={flag};interrupted={flag2};hadSe={value != null};hadWav={!string.IsNullOrWhiteSpace(value2)};hadXml={!string.IsNullOrWhiteSpace(value3)}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Interrupt:" + reason);
		QueueDeferredCleanup(value, value2, value3, "Interrupt:" + reason, agentIndex);
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			StopAutoGroupChatSession("meeting_combat_interrupt");
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
		bool flag = false;
		SoundEvent value = null;
		string value2 = null;
		string value3 = null;
		lock (_speakingLock)
		{
			flag = _speakingAgentIndices.Remove(agentIndex);
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
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
			_pendingSceneDialogueFeedQueues.Remove(agentIndex);
		}
		_pendingInteractionTimeoutArms.Remove(agentIndex);
		_activeInteractionSessions.Remove(agentIndex);
		bool flag2 = false;
		try
		{
			flag2 = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			flag2 = false;
		}
		if (!flag && value == null && string.IsNullOrWhiteSpace(value2) && string.IsNullOrWhiteSpace(value3) && !flag2)
		{
			LogTtsReport("CancelAgentSpeechForRemoval.Noop", agentIndex, "reason=" + reason);
			return;
		}
		LogTtsReport("CancelAgentSpeechForRemoval", agentIndex, $"reason={reason};speaking={flag};interrupted={flag2};hadSe={value != null};hadWav={!string.IsNullOrWhiteSpace(value2)};hadXml={!string.IsNullOrWhiteSpace(value3)}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Removed:" + reason);
		QueueDeferredCleanup(value, value2, value3, "Removed:" + reason, agentIndex);
	}

	private void LogTtsReport(string stage, int agentIndex, string extra = null)
	{
		try
		{
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex));
			bool flag = val != null && val.IsActive();
			bool flag2 = IsAgentHostileToMainAgent(val);
			string text = (((val == null) ? null : val.Name?.ToString()) ?? "").Trim();
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			lock (_speakingLock)
			{
				flag3 = _speakingAgentIndices.Contains(agentIndex);
				flag4 = _agentSoundEvents.ContainsKey(agentIndex);
				flag5 = _agentWavPaths.ContainsKey(agentIndex);
				flag6 = _agentXmlPaths.ContainsKey(agentIndex);
			}
			bool flag7 = false;
			int num = 0;
			bool flag8 = false;
			int num2 = 0;
			bool flag9 = false;
			int num3 = 0;
			lock (_ttsBubbleSyncLock)
			{
				if (_pendingNpcBubbleQueues.TryGetValue(agentIndex, out var value) && value != null)
				{
					flag7 = value.Count > 0;
					num = value.Count;
				}
				if (_pendingAudioDurationQueues.TryGetValue(agentIndex, out var value2) && value2 != null)
				{
					flag8 = value2.Count > 0;
					num2 = value2.Count;
				}
				if (_pendingSpeechCompletionTokenQueues.TryGetValue(agentIndex, out var value3) && value3 != null)
				{
					flag9 = value3.Count > 0;
					num3 = value3.Count;
				}
			}
			SceneInteractionSession value4;
			bool flag10 = _activeInteractionSessions.TryGetValue(agentIndex, out value4) && value4 != null;
			long num4 = (flag10 ? value4.InteractionToken : 0);
			bool flag11 = flag10 && value4.TimeoutArmed;
			PendingInteractionTimeoutArm value5;
			bool flag12 = _pendingInteractionTimeoutArms.TryGetValue(agentIndex, out value5) && value5 != null;
			Mission current2 = Mission.Current;
			float num5 = ((current2 != null) ? current2.CurrentTime : (-1f));
			Mission current3 = Mission.Current;
			string text2 = (((current3 != null) ? current3.SceneName : null) ?? "").Trim();
			string text3 = (flag12 ? value5.ArmAtMissionTime.ToString("F2") : "-");
			string text4 = (string.IsNullOrWhiteSpace(extra) ? string.Empty : (", " + extra));
			Logger.Log("TTSReport", $"[{stage}] agentIndex={agentIndex}, name={text}, active={flag}, hostile={flag2}, speaking={flag3}, hasSe={flag4}, hasWav={flag5}, hasXml={flag6}, hasInteraction={flag10}, interactionToken={num4}, timeoutArmed={flag11}, hasPendingArm={flag12}, pendingArmAt={text3}, pendingBubble={flag7}, pendingBubbleCount={num}, pendingDuration={flag8}, pendingDurationCount={num2}, pendingSpeechToken={flag9}, pendingSpeechTokenCount={num3}, missionTime={num5:F2}, scene={text2}{text4}");
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

	private void StartAutoGroupChatSession(List<NpcDataPacket> participants, NpcDataPacket primaryNpc, int conversationEpoch, int lastSpeakerAgentIndex)
	{
		if (!IsSceneConversationEpochCurrent(conversationEpoch) || participants == null)
		{
			return;
		}
		List<int> list = (from npc in participants
			where npc != null
			select npc.AgentIndex into agentIndex
			where agentIndex >= 0
			select agentIndex).Distinct().ToList();
		if (list.Count >= 2)
		{
			lock (_autoGroupChatLock)
			{
				_autoGroupChatSession = new AutoNpcGroupChatSession
				{
					ConversationEpoch = conversationEpoch,
					PrimaryAgentIndex = ((primaryNpc != null && primaryNpc.AgentIndex >= 0) ? primaryNpc.AgentIndex : list[0]),
					ParticipantAgentIndices = list,
					LastSpeakerAgentIndex = lastSpeakerAgentIndex,
					MaxAutoLines = 8,
					IsActive = true
				};
				int num = ((lastSpeakerAgentIndex >= 0) ? list.IndexOf(lastSpeakerAgentIndex) : (-1));
				_autoGroupChatSession.NextSpeakerCursor = ((num >= 0) ? ((num + 1) % list.Count) : 0);
			}
			EnsureAutoGroupChatLoopRunning();
		}
	}

	private void EnsureAutoGroupChatLoopRunning()
	{
		lock (_autoGroupChatLock)
		{
			if (_autoGroupChatLoopRunning)
			{
				return;
			}
			_autoGroupChatLoopRunning = true;
		}
		Task.Run(async delegate
		{
			await RunAutoGroupChatLoopAsync();
		});
	}

	private async Task RunAutoGroupChatLoopAsync()
	{
		try
		{
			while (true)
			{
				AutoNpcGroupChatSession autoNpcGroupChatSession;
				lock (_autoGroupChatLock)
				{
					if (_autoGroupChatSession == null || !_autoGroupChatSession.IsActive)
					{
						_autoGroupChatLoopRunning = false;
						break;
					}
					autoNpcGroupChatSession = new AutoNpcGroupChatSession
					{
						ConversationEpoch = _autoGroupChatSession.ConversationEpoch,
						PrimaryAgentIndex = _autoGroupChatSession.PrimaryAgentIndex,
						ParticipantAgentIndices = new List<int>(_autoGroupChatSession.ParticipantAgentIndices),
						NextSpeakerCursor = _autoGroupChatSession.NextSpeakerCursor,
						LastSpeakerAgentIndex = _autoGroupChatSession.LastSpeakerAgentIndex,
						AutoLinesGenerated = _autoGroupChatSession.AutoLinesGenerated,
						ConsecutiveNoContinueCount = _autoGroupChatSession.ConsecutiveNoContinueCount,
						MaxAutoLines = _autoGroupChatSession.MaxAutoLines,
						IsActive = _autoGroupChatSession.IsActive,
						GenerationInFlight = _autoGroupChatSession.GenerationInFlight
					};
				}
				if (!IsSceneConversationEpochCurrent(autoNpcGroupChatSession.ConversationEpoch))
				{
					StopAutoGroupChatSession("stale_epoch");
					continue;
				}
				if (_isProcessingShout || _ttsPausedByShoutUi)
				{
					await Task.Delay(250);
					continue;
				}
				int num;
				if (Mission.Current != null && Agent.Main != null && Agent.Main.IsActive())
				{
					Campaign current = Campaign.Current;
					if (current == null)
					{
						num = 0;
					}
					else
					{
						ConversationManager conversationManager = current.ConversationManager;
						num = ((((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null)) == true) ? 1 : 0);
					}
				}
				else
				{
					num = 1;
				}
				if (num != 0)
				{
					StopAutoGroupChatSession("scene_invalid");
					continue;
				}
				if (autoNpcGroupChatSession.AutoLinesGenerated >= autoNpcGroupChatSession.MaxAutoLines)
				{
					StopAutoGroupChatSession("line_limit");
					continue;
				}
				if (autoNpcGroupChatSession.GenerationInFlight || IsSpeechPipelineBusy())
				{
					await Task.Delay(250);
					continue;
				}
				if (!TryBuildAutoGroupChatSnapshot(autoNpcGroupChatSession, out var participants, out var resolvedHeroes))
				{
					StopAutoGroupChatSession("participants_lost");
					continue;
				}
				HoldSceneConversationParticipants(participants);
				NpcDataPacket nextSpeaker = SelectNextAutoGroupChatSpeaker(autoNpcGroupChatSession, participants, resolvedHeroes);
				if (nextSpeaker == null)
				{
					StopAutoGroupChatSession("no_speaker");
					continue;
				}
				lock (_autoGroupChatLock)
				{
					if (_autoGroupChatSession == null || !_autoGroupChatSession.IsActive || _autoGroupChatSession.ConversationEpoch != autoNpcGroupChatSession.ConversationEpoch)
					{
						continue;
					}
					_autoGroupChatSession.GenerationInFlight = true;
					goto IL_0542;
				}
				IL_0542:
				string text = "";
				try
				{
					text = await GenerateAutoGroupChatLineAsync(nextSpeaker, participants, resolvedHeroes);
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Logger.Log("ShoutBehavior", "[AutoGroupChat] generate failed: " + ex2.Message);
				}
				finally
				{
					lock (_autoGroupChatLock)
					{
						if (_autoGroupChatSession != null && _autoGroupChatSession.ConversationEpoch == autoNpcGroupChatSession.ConversationEpoch)
						{
							_autoGroupChatSession.GenerationInFlight = false;
						}
					}
				}
				if (!IsSceneConversationEpochCurrent(autoNpcGroupChatSession.ConversationEpoch))
				{
					continue;
				}
				bool flag = ContainsAutoGroupNoContinueSignal(text);
				bool flag2 = ContainsAutoGroupEndSignal(text);
				string text2 = StripAutoGroupStopSignal(text);
				if (flag || (!flag2 && string.IsNullOrWhiteSpace(text2)))
				{
					bool flag3 = false;
					lock (_autoGroupChatLock)
					{
						if (_autoGroupChatSession != null && _autoGroupChatSession.ConversationEpoch == autoNpcGroupChatSession.ConversationEpoch)
						{
							_autoGroupChatSession.LastSpeakerAgentIndex = nextSpeaker.AgentIndex;
							_autoGroupChatSession.ConsecutiveNoContinueCount++;
							flag3 = _autoGroupChatSession.ConsecutiveNoContinueCount >= 2;
						}
					}
					if (flag3)
					{
						StopAutoGroupChatSession("no_continue");
					}
					else
					{
						await Task.Delay(250);
					}
					continue;
				}
				if (flag2 && string.IsNullOrWhiteSpace(text2))
				{
					int speakerAgentIndex = nextSpeaker.AgentIndex;
					_mainThreadActions.Enqueue(delegate
					{
						try
						{
							Mission current3 = Mission.Current;
							Agent agent3 = ((current3 == null) ? null : ((IEnumerable<Agent>)current3.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == speakerAgentIndex && a.IsActive()));
							if (IsAgentFollowingPlayerBySceneCommand(agent3))
							{
								StopSceneSummonFollowPlayer(agent3, restoreDailyBehaviors: false);
								ReturnAgentAfterStoppingSceneFollow(agent3);
							}
							SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(speakerAgentIndex);
							if (sceneSummonConversationSession != null)
							{
								BeginSceneSummonConversationReturn(sceneSummonConversationSession);
							}
						}
						catch
						{
						}
					});
					StopAutoGroupChatSession("end_tag");
					continue;
				}
				NpcDataPacket npcDataPacket = participants.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == autoNpcGroupChatSession.PrimaryAgentIndex) ?? participants.FirstOrDefault();
				lock (_autoGroupChatLock)
				{
					if (_autoGroupChatSession != null && _autoGroupChatSession.ConversationEpoch == autoNpcGroupChatSession.ConversationEpoch)
					{
						_autoGroupChatSession.LastSpeakerAgentIndex = nextSpeaker.AgentIndex;
						_autoGroupChatSession.AutoLinesGenerated++;
						_autoGroupChatSession.ConsecutiveNoContinueCount = 0;
					}
				}
				if (npcDataPacket != null)
				{
					TrackPlayerInteraction(npcDataPacket, participants?.Count ?? 0);
				}
				if (!string.IsNullOrWhiteSpace(text2))
				{
					Mission current2 = Mission.Current;
					Agent agent2 = ((current2 == null) ? null : ((IEnumerable<Agent>)current2.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == nextSpeaker.AgentIndex && a.IsActive()));
					bool flag4 = flag2 && IsAgentFollowingPlayerBySceneCommand(agent2);
					bool flag5 = flag2 && TryGetSceneSummonConversationSessionForAgentIndex(nextSpeaker.AgentIndex) != null;
					string text3 = text2;
					if (flag4)
					{
						text3 = (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + " ")) + "[STP]";
					}
					if (flag5)
					{
						text3 = (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + " ")) + "[END]";
					}
					EnqueueSpeechLineWithOptions(nextSpeaker, text3, participants, commitHistory: true, suppressStare: false, flag4 || flag5, autoNpcGroupChatSession.ConversationEpoch);
				}
				if (flag2)
				{
					StopAutoGroupChatSession("end_tag");
					continue;
				}
				await Task.Delay(250);
				participants = null;
				resolvedHeroes = null;
			}
		}
		finally
		{
			lock (_autoGroupChatLock)
			{
				if (_autoGroupChatSession == null || !_autoGroupChatSession.IsActive)
				{
					_autoGroupChatLoopRunning = false;
				}
			}
		}
	}

	private bool TryBuildAutoGroupChatSnapshot(AutoNpcGroupChatSession session, out List<NpcDataPacket> participants, out Dictionary<int, Hero> resolvedHeroes)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		participants = new List<NpcDataPacket>();
		resolvedHeroes = new Dictionary<int, Hero>();
		if (session == null || session.ParticipantAgentIndices == null || session.ParticipantAgentIndices.Count == 0 || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		Vec3 position = Agent.Main.Position;
		foreach (int participantAgentIndex in session.ParticipantAgentIndices)
		{
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == participantAgentIndex);
			if (!CanAgentParticipateInSceneSpeech(val))
			{
				continue;
			}
			try
			{
				Vec3 position2 = val.Position;
				if (((Vec3)(ref position2)).Distance(position) > 8f)
				{
					continue;
				}
			}
			catch
			{
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(val);
			if (npcDataPacket == null)
			{
				continue;
			}
			participants.Add(npcDataPacket);
			if (npcDataPacket.IsHero)
			{
				BasicCharacterObject character = val.Character;
				CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val2 != null && val2.HeroObject != null)
				{
					resolvedHeroes[npcDataPacket.AgentIndex] = val2.HeroObject;
				}
			}
		}
		ApplySceneLocalDisambiguatedNames(participants);
		return participants.Count >= 2;
	}

	private NpcDataPacket SelectNextAutoGroupChatSpeaker(AutoNpcGroupChatSession session, List<NpcDataPacket> participants, Dictionary<int, Hero> resolvedHeroes)
	{
		if (session == null || participants == null || participants.Count < 2)
		{
			return null;
		}
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		foreach (NpcDataPacket participant in participants)
		{
			if (participant != null)
			{
				bool canSpeak = true;
				string statusLine = "";
				bool flag;
				if (participant.IsHero)
				{
					Hero value = null;
					resolvedHeroes?.TryGetValue(participant.AgentIndex, out value);
					flag = MyBehavior.TryGetSceneHeroPatienceStatusForExternal(value, out statusLine, out canSpeak);
				}
				else
				{
					flag = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(participant.UnnamedKey, participant.Name, GetSceneNpcPatienceNameForPrompt(participant), out statusLine, out canSpeak);
				}
				if (!flag || canSpeak)
				{
					list.Add(participant);
				}
			}
		}
		if (list.Count < 2)
		{
			return null;
		}
		List<int> list2 = participants.Select((NpcDataPacket npc) => npc.AgentIndex).ToList();
		int num = session.NextSpeakerCursor;
		NpcDataPacket npcDataPacket = null;
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			int num3 = (num + num2) % list2.Count;
			int num4 = list2[num3];
			NpcDataPacket npcDataPacket2 = list.FirstOrDefault((NpcDataPacket npc) => npc.AgentIndex == num4);
			if (npcDataPacket2 != null && (npcDataPacket2.AgentIndex != session.LastSpeakerAgentIndex || list.Count <= 1))
			{
				npcDataPacket = npcDataPacket2;
				num = (num3 + 1) % list2.Count;
				break;
			}
		}
		if (npcDataPacket == null)
		{
			npcDataPacket = list.FirstOrDefault();
			int num5 = list2.IndexOf(npcDataPacket?.AgentIndex ?? (-1));
			num = ((num5 >= 0 && list2.Count > 0) ? ((num5 + 1) % list2.Count) : 0);
		}
		lock (_autoGroupChatLock)
		{
			if (_autoGroupChatSession != null && _autoGroupChatSession.ConversationEpoch == session.ConversationEpoch)
			{
				_autoGroupChatSession.NextSpeakerCursor = num;
			}
		}
		return npcDataPacket;
	}

	private void CapturePendingPlayerTurnCarryAgents(List<Agent> currentNearbyAgents)
	{
		List<int> participantAgentIndices = null;
		lock (_autoGroupChatLock)
		{
			_pendingPlayerTurnCarryAgentIndices.Clear();
			if (_autoGroupChatSession == null || !_autoGroupChatSession.IsActive || _autoGroupChatSession.ParticipantAgentIndices == null || _autoGroupChatSession.ParticipantAgentIndices.Count == 0)
			{
				return;
			}
			participantAgentIndices = new List<int>(_autoGroupChatSession.ParticipantAgentIndices);
			List<Agent> list = currentNearbyAgents ?? new List<Agent>();
			for (int i = 0; i < list.Count; i++)
			{
				Agent val = list[i];
				if (val != null && _autoGroupChatSession.ParticipantAgentIndices.Contains(val.Index) && !_pendingPlayerTurnCarryAgentIndices.Contains(val.Index))
				{
					_pendingPlayerTurnCarryAgentIndices.Add(val.Index);
					AddAgentToStareList(val);
				}
			}
		}
		ActivateMultiSceneMovementSuppression(participantAgentIndices);
		HoldInterruptedAutoGroupParticipants(participantAgentIndices);
	}

	private void HoldInterruptedAutoGroupParticipants(List<int> participantAgentIndices)
	{
		if (participantAgentIndices == null || participantAgentIndices.Count == 0 || Mission.Current == null)
		{
			return;
		}
		ExtendStaringHoldForPlayerDrivenSceneRound(participantAgentIndices.Count);
		for (int i = 0; i < participantAgentIndices.Count; i++)
		{
			int participantAgentIndex = participantAgentIndices[i];
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == participantAgentIndex);
			if (val != null && val.IsActive() && val.IsHuman && val != Agent.Main)
			{
				AddAgentToStareList(val);
			}
		}
	}

	private List<Agent> ConsumePendingPlayerTurnCarryAgents()
	{
		List<int> list;
		lock (_autoGroupChatLock)
		{
			list = new List<int>(_pendingPlayerTurnCarryAgentIndices);
			_pendingPlayerTurnCarryAgentIndices.Clear();
		}
		List<Agent> list2 = new List<Agent>();
		if (Mission.Current == null || list.Count == 0)
		{
			return list2;
		}
		for (int i = 0; i < list.Count; i++)
		{
			int num = list[i];
			Agent agent = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == num);
			if (agent != null && agent.IsActive() && agent.IsHuman && agent != Agent.Main && !list2.Any((Agent existing) => existing != null && existing.Index == agent.Index))
			{
				list2.Add(agent);
			}
		}
		return list2;
	}

	private void ClearPendingPlayerTurnCarryAgents()
	{
		lock (_autoGroupChatLock)
		{
			_pendingPlayerTurnCarryAgentIndices.Clear();
		}
	}

	private static void MergeAgentLists(List<Agent> target, List<Agent> extras)
	{
		if (target == null || extras == null || extras.Count == 0)
		{
			return;
		}
		for (int i = 0; i < extras.Count; i++)
		{
			Agent agent = extras[i];
			if (agent != null && !target.Any((Agent existing) => existing != null && existing.Index == agent.Index))
			{
				target.Add(agent);
			}
		}
	}

	private int BeginNewPlayerDrivenSceneConversationEpoch()
	{
		int result = Interlocked.Increment(ref _sceneConversationEpoch);
		StopAutoGroupChatSession("player_turn");
		ClearPendingSceneConversationAttentionRelease();
		ClearQueuedSceneSpeech();
		try
		{
			StopAllLipSyncPlaybackAndCleanup();
		}
		catch
		{
		}
		return result;
	}

	private void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices)
	{
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
			DeactivateMultiSceneMovementSuppression();
		}
		else
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
						_multiSceneMovementSuppressionAgentIndices.Add(participantAgentIndex);
					}
				}
				_multiSceneMovementSuppressionActive = _multiSceneMovementSuppressionAgentIndices.Count >= 1;
				if (_multiSceneMovementSuppressionActive)
				{
					_multiSceneMovementSuppressionTimer = 0.01f;
				}
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
		List<int> participantAgentIndices = null;
		lock (_pendingSceneConversationAttentionReleaseLock)
		{
			if (_pendingSceneConversationAttentionReleaseAgentIndices.Count == 0)
			{
				return;
			}
			participantAgentIndices = _pendingSceneConversationAttentionReleaseAgentIndices.ToList();
			_pendingSceneConversationAttentionReleaseAgentIndices.Clear();
		}
		ReleaseSceneConversationAttention(participantAgentIndices);
	}

	private void UpdateMultiSceneMovementSuppression(float dt)
	{
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		List<int> list;
		lock (_multiSceneMovementSuppressionLock)
		{
			if (!_multiSceneMovementSuppressionActive)
			{
				return;
			}
			_multiSceneMovementSuppressionTimer += Math.Max(0f, dt);
			if (_multiSceneMovementSuppressionTimer < 0.01f)
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
		float num2 = 100f;
		foreach (int item in list)
		{
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == item);
			if (!CanAgentParticipateInSceneSpeech(val))
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
				Vec3 position = val.Position;
				Vec2 asVec = ((Vec3)(ref position)).AsVec2;
				position = Agent.Main.Position;
				if (((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2) > num2)
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			num++;
			ApplyMultiSceneMovementSuppression(val);
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
			_stopStaringTime = Math.Max(_stopStaringTime, Mission.Current.CurrentTime + 0.25f);
		}
	}

	private void ApplyMultiSceneMovementSuppression(Agent agent)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldSuppressSceneConversationControlForMeeting() && CanAgentParticipateInSceneSpeech(agent))
		{
			try
			{
				agent.SetLookAgent(Agent.Main);
				agent.SetMaximumSpeedLimit(0f, false);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, false, (AIScriptedFrameFlags)0);
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
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Invalid comparison between Unknown and I4
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
			Mission current = Mission.Current;
			MissionFightHandler val = ((current != null) ? current.GetMissionBehavior<MissionFightHandler>() : null);
			if (val != null && val.IsThereActiveFight())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Mission current2 = Mission.Current;
			Agent main = Agent.Main;
			if (current2 != null && main != null && main.IsActive() && current2.Agents != null)
			{
				MissionMode mode = current2.Mode;
				if ((int)mode == 2 || (int)mode == 3)
				{
					foreach (Agent item in (List<Agent>)(object)current2.Agents)
					{
						if (item != null && item.IsActive() && item != main && AreAgentsHostileForSceneConversation(item, main))
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
			Agent main2 = Agent.Main;
			if (main2 != null && main2.IsActive())
			{
				Mission current4 = Mission.Current;
				if (((current4 != null) ? current4.Agents : null) != null)
				{
					foreach (Agent item2 in (List<Agent>)(object)Mission.Current.Agents)
					{
						if (item2 != null && item2.IsActive() && item2 != main2 && AreAgentsHostileForSceneConversation(item2, main2))
						{
							return true;
						}
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
		if (ShouldSuppressSceneConversationControlForMeeting())
		{
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
			Agent val = _staringAgents[i];
			if (val != null)
			{
				hashSet.Add(val.Index);
			}
		}
		DeactivateMultiSceneMovementSuppression();
		ClearPendingSceneConversationAttentionRelease();
		if (hashSet.Count > 0)
		{
			ReleaseSceneConversationAttention(hashSet);
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
			agent.SetLookAgent((Agent)null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, false);
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
			agent.SetIsAIPaused(false);
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
			Agent val = _staringAgents[num];
			if (val == null)
			{
				_staringAgents.RemoveAt(num);
			}
			else if (hashSet.Contains(val.Index))
			{
				if (list == null)
				{
					list = new List<Agent>();
				}
				list.Add(val);
				_staringAgents.RemoveAt(num);
				_staringAgentAnchors.Remove(val.Index);
			}
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
		if (stopAutoGroupSession)
		{
			StopAutoGroupChatSession("taunt_action");
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

	private void StopAutoGroupChatSession(string reason)
	{
		List<int> participantAgentIndices = null;
		lock (_autoGroupChatLock)
		{
			if (_autoGroupChatSession != null)
			{
				participantAgentIndices = ((_autoGroupChatSession.ParticipantAgentIndices != null) ? new List<int>(_autoGroupChatSession.ParticipantAgentIndices) : null);
				_autoGroupChatSession.IsActive = false;
				_autoGroupChatSession = null;
			}
		}
		if (!string.Equals(reason, "player_turn", StringComparison.Ordinal))
		{
			DeactivateMultiSceneMovementSuppression();
			RequestSceneConversationAttentionRelease(participantAgentIndices);
		}
		if (!string.IsNullOrWhiteSpace(reason))
		{
			Logger.Log("ShoutBehavior", "[AutoGroupChat] stopped: " + reason);
		}
	}

	private static void RefreshHostileCombatAgentAutonomy(Agent agent)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!IsAgentHostileToMainAgent(agent) || !agent.IsAIControlled)
		{
			return;
		}
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			agent.SetAgentFlags((AgentFlag)(agentFlags | 0x10000));
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent((Agent)null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, false);
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
			agent.SetIsAIPaused(false);
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
			agent.SetAlarmState((AIStateFlag)3);
		}
		catch
		{
		}
		try
		{
			agent.SetWatchState((WatchState)2);
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
		return (participantCount > 1) ? 300f : 15f;
	}

	private void TrackPlayerInteraction(NpcDataPacket primaryTarget, int participantCount = 1, float timeoutSeconds = -1f, bool returnSceneSummonOnTimeout = false)
	{
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || Mission.Current == null)
		{
			return;
		}
		Agent agent = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == primaryTarget.AgentIndex && a.IsActive());
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
		if (agentIndex >= 0 && interactionToken != 0L && Mission.Current != null)
		{
			float armAtMissionTime = Mission.Current.CurrentTime + Math.Max(0f, speechDurationSeconds);
			_pendingInteractionTimeoutArms[agentIndex] = new PendingInteractionTimeoutArm
			{
				AgentIndex = agentIndex,
				InteractionToken = interactionToken,
				ArmAtMissionTime = armAtMissionTime
			};
		}
	}

	private void ArmActiveInteractionTimeoutNow(int agentIndex, long interactionToken)
	{
		if (agentIndex >= 0 && Mission.Current != null && _activeInteractionSessions.TryGetValue(agentIndex, out var value) && value != null && value.InteractionToken == interactionToken)
		{
			value.LastActivityTime = Mission.Current.CurrentTime;
			value.TimeoutArmed = true;
			_pendingInteractionTimeoutArms.Remove(agentIndex);
		}
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
		if (agentIndex >= 0 && Mission.Current != null && _pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) && value != null)
		{
			float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
			value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
			value.ExecuteAtMissionTime = (value.WaitForPlaybackFinished ? (-1f) : (Mission.Current.CurrentTime + num));
		}
	}

	private void FlushSceneSummonReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex >= 0 && _pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) && value != null)
		{
			_pendingSceneSummonReturnsAfterSpeech.Remove(agentIndex);
			if (value.Session != null && _activeSceneSummonConversationSessions.Contains(value.Session))
			{
				BeginSceneSummonConversationReturn(value.Session);
			}
		}
	}

	private void ScheduleSceneFollowCommandAfterSpeech(int agentIndex, bool startFollow, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex >= 0 && Mission.Current != null)
		{
			float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
			_pendingSceneFollowCommands[agentIndex] = new PendingSceneFollowCommand
			{
				AgentIndex = agentIndex,
				StartFollow = startFollow,
				WaitForPlaybackFinished = (playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished),
				ExecuteAtMissionTime = ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? (-1f) : (Mission.Current.CurrentTime + num))
			};
		}
	}

	private void FlushSceneFollowCommandAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneFollowCommands.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneFollowCommands.Remove(agentIndex);
		Mission current = Mission.Current;
		Agent agent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex));
		if (CanAgentParticipateInSceneSpeech(agent))
		{
			if (value.StartFollow)
			{
				RememberSceneFollowReturnState(agent);
				RemoveAgentFromSceneSummonConversationForFollow(agent);
				StartSceneSummonFollowPlayer(agent);
			}
			else
			{
				StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
				ReturnAgentAfterStoppingSceneFollow(agent);
			}
		}
	}

	private void ScheduleSceneGuideReturnAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		if (agentIndex >= 0 && Mission.Current != null && _pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) && value != null)
		{
			float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
			value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
			value.ExecuteAtMissionTime = (value.WaitForPlaybackFinished ? (-1f) : (Mission.Current.CurrentTime + num));
		}
	}

	private void FlushSceneGuideReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex >= 0 && _pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) && value != null)
		{
			_pendingSceneGuideReturnsAfterSpeech.Remove(agentIndex);
			if (value.LocationCharacter != null)
			{
				QueueSceneReturnJob(value.DisplayName, value.LocationCharacter, value.OriginalLocation, value.OriginalPosition);
			}
		}
	}

	private void UpdatePendingSceneSummonReturnsAfterSpeech()
	{
		if (Mission.Current == null || _pendingSceneSummonReturnsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneSummonReturnAfterSpeech> item in _pendingSceneSummonReturnsAfterSpeech)
		{
			PendingSceneSummonReturnAfterSpeech value = item.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(item.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item2 in list)
		{
			FlushSceneSummonReturnAfterSpeech(item2);
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
		foreach (KeyValuePair<int, PendingSceneGuideReturnAfterSpeech> item in _pendingSceneGuideReturnsAfterSpeech)
		{
			PendingSceneGuideReturnAfterSpeech value = item.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(item.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item2 in list)
		{
			FlushSceneGuideReturnAfterSpeech(item2);
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
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (!IsAgentFollowingPlayerBySceneCommand(item))
			{
				continue;
			}
			try
			{
				CampaignAgentComponent component = item.GetComponent<CampaignAgentComponent>();
				object obj;
				if (component == null)
				{
					obj = null;
				}
				else
				{
					AgentNavigator agentNavigator = component.AgentNavigator;
					if (agentNavigator == null)
					{
						obj = null;
					}
					else
					{
						DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
						obj = ((behaviorGroup != null) ? ((AgentBehaviorGroup)behaviorGroup).GetBehavior<FollowAgentBehavior>() : null);
					}
				}
				FollowAgentBehavior val = (FollowAgentBehavior)obj;
				if (val != null)
				{
					FollowAgentBehaviorIdleDistanceField.SetValue(val, 0f);
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
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (IsAgentFollowingPlayerBySceneCommand(item) && IsAgentHostileToMainAgent(item))
			{
				try
				{
					_pendingSceneFollowCommands.Remove(item.Index);
					_pendingInteractionTimeoutArms.Remove(item.Index);
					_activeInteractionSessions.Remove(item.Index);
					_sceneFollowReturnStates.Remove(item.Index);
					StopSceneSummonFollowPlayer(item);
					RefreshHostileCombatAgentAutonomy(item);
				}
				catch
				{
				}
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
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float num = 100f;
			Vec3 position = targetAgent.Position;
			Vec2 asVec = ((Vec3)(ref position)).AsVec2;
			position = Agent.Main.Position;
			return ((Vec2)(ref asVec)).DistanceSquared(((Vec3)(ref position)).AsVec2) <= num;
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
			float num = ((value != null && value.TimeoutSeconds > 0f) ? value.TimeoutSeconds : 15f);
			if (value == null)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(activeInteractionSession.Key);
			}
			else
			{
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
				Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == activeInteractionSession.Key && a.IsActive());
				if (IsAgentFollowingPlayerBySceneCommand(val))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(activeInteractionSession.Key);
					_pendingInteractionTimeoutArms.Remove(activeInteractionSession.Key);
				}
				else if (!IsPlayerWithinActiveInteractionRange(val))
				{
					value.LastActivityTime = currentTime;
				}
				else if (currentTime - value.LastActivityTime >= num)
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(activeInteractionSession.Key);
				}
			}
		}
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (int item in list)
		{
			if (_activeInteractionSessions.TryGetValue(item, out var value2))
			{
				ExpireActiveInteraction(value2);
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value3) && (value3 == null || value3 == value2 || value3.InteractionToken == value2?.InteractionToken))
			{
				_activeInteractionSessions.Remove(item);
			}
		}
	}

	private void ExpireActiveInteraction(SceneInteractionSession session)
	{
		if (session == null || session.TargetAgentIndex < 0)
		{
			return;
		}
		Mission current = Mission.Current;
		Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == session.TargetAgentIndex && a.IsActive()));
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(session.TargetAgentIndex);
		if (IsAgentBusyWithSceneSummonErrand(session.TargetAgentIndex) || IsAgentBusyWithSceneGuideErrand(session.TargetAgentIndex))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (IsAgentFollowingPlayerBySceneCommand(val))
		{
			_pendingInteractionTimeoutArms.Remove(session.TargetAgentIndex);
			_activeInteractionSessions.Remove(session.TargetAgentIndex);
			return;
		}
		if (!CanAgentParticipateInSceneSpeech(val))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			if (sceneSummonConversationSession != null)
			{
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
			return;
		}
		if (IsAgentHostileToMainAgent(val))
		{
			RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
			RestoreAgentAutonomy(val);
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
			ReleaseAgentFromSceneConversationLocks(val);
			return;
		}
		string text = (session.TargetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (val.Name?.ToString() ?? "NPC").Trim();
		}
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		string text3 = text + ": " + text2 + "长时间没有发言，你决定去干自己的事了。";
		string factText = "[AFEF NPC行为补充] " + text3;
		if (session.TimeoutSeconds > 3.5f)
		{
			if (sceneSummonConversationSession != null)
			{
				ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
				_pendingSceneSummonReturnsAfterSpeech[session.TargetAgentIndex] = new PendingSceneSummonReturnAfterSpeech
				{
					AgentIndex = session.TargetAgentIndex,
					Session = sceneSummonConversationSession
				};
			}
			TriggerImmediateSceneBehaviorReaction(factText, session.TargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, 3f);
			return;
		}
		AppendTargetedSceneNpcFact(factText, session.TargetAgentIndex, persistHeroPrivateHistory: true);
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
		_staringAgentAnchors.Remove(session.TargetAgentIndex);
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
		RemoveSceneMovementSuppressionAgents(new int[1] { session.TargetAgentIndex });
		RestoreAgentAutonomy(val);
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
			agent.SetIsAIPaused(false);
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
			agent.SetWatchState((WatchState)0);
		}
		catch
		{
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator obj5 = ((component != null) ? component.AgentNavigator : null) ?? ((component != null) ? component.CreateAgentNavigator() : null);
			if (obj5 != null)
			{
				obj5.ClearTarget();
			}
		}
		catch
		{
		}
	}

	private static bool ShouldPreserveAmbientUseDuringStare(Agent agent)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		object obj = ((agent != null) ? agent.CurrentlyUsedGameObject : null);
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
				WeakGameEntity gameEntity = ((ScriptComponentBehavior)agent.CurrentlyUsedGameObject).GameEntity;
				if (((WeakGameEntity)(ref gameEntity)).IsValid)
				{
					text = text + " " + ((WeakGameEntity)(ref gameEntity)).Name;
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
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
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
			agent.SetIsAIPaused(false);
		}
		catch
		{
			if (trace)
			{
				TraceStareDebug("SetIsAIPaused(false) failed for " + GetAgentDebugLabel(agent));
			}
		}
		float? num = null;
		Vec2 asVec;
		try
		{
			if (Agent.Main != null && Agent.Main.IsActive())
			{
				Vec3 val = Agent.Main.Position - agent.Position;
				asVec = ((Vec3)(ref val)).AsVec2;
				if (((Vec2)(ref asVec)).LengthSquared > 0.0001f)
				{
					asVec = ((Vec3)(ref val)).AsVec2;
					num = ((Vec2)(ref asVec)).RotationInRadians;
				}
			}
		}
		catch
		{
			num = null;
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator val2 = ((component != null) ? component.AgentNavigator : null) ?? ((component != null) ? component.CreateAgentNavigator() : null);
			Vec3 val3 = agent.Position;
			if (_staringAgentAnchors.TryGetValue(agent.Index, out var value))
			{
				val3 = value;
			}
			if (val2 != null && num.HasValue)
			{
				WorldPosition val4 = default(WorldPosition);
				((WorldPosition)(ref val4))._002Ector(Mission.Current.Scene, val3);
				val2.SetTargetFrame(val4, num.Value, 0.05f, 0.8f, (AIScriptedFrameFlags)18, false);
			}
			else
			{
				WorldPosition val5 = default(WorldPosition);
				((WorldPosition)(ref val5))._002Ector(Mission.Current.Scene, val3);
				agent.SetScriptedPosition(ref val5, false, (AIScriptedFrameFlags)0);
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
			string[] obj5 = new string[6]
			{
				"Freeze applied to ",
				GetAgentDebugLabel(agent),
				" vel2=",
				null,
				null,
				null
			};
			Vec3 velocity = agent.Velocity;
			asVec = ((Vec3)(ref velocity)).AsVec2;
			obj5[3] = ((Vec2)(ref asVec)).LengthSquared.ToString();
			obj5[4] = " rotation=";
			obj5[5] = (num.HasValue ? num.Value.ToString("F3") : "null");
			TraceStareDebug(string.Concat(obj5));
		}
	}

	private void ForceAgentFacePlayer(Agent agent)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		Mission current = Mission.Current;
		if ((NativeObject)(object)((current != null) ? current.Scene : null) == (NativeObject)null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		try
		{
			Vec3 position = agent.Position;
			Vec3 position2 = Agent.Main.Position;
			try
			{
				if ((NativeObject)(object)agent.AgentVisuals != (NativeObject)null)
				{
					position.z = agent.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			try
			{
				if ((NativeObject)(object)Agent.Main.AgentVisuals != (NativeObject)null)
				{
					position2.z = Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true).z;
				}
			}
			catch
			{
			}
			Vec3 val = position2 - position;
			if (((Vec3)(ref val)).LengthSquared < 0.0001f)
			{
				return;
			}
			try
			{
				if ((NativeObject)(object)Agent.Main.AgentVisuals != (NativeObject)null)
				{
					agent.SetLookToPointOfInterest(Agent.Main.AgentVisuals.GetGlobalStableEyePoint(true));
				}
			}
			catch
			{
			}
			try
			{
				MBAgentVisuals agentVisuals = agent.AgentVisuals;
				if (agentVisuals != null)
				{
					Skeleton skeleton = agentVisuals.GetSkeleton();
					if (skeleton != null)
					{
						skeleton.ForceUpdateBoneFrames();
					}
				}
			}
			catch
			{
			}
			agent.LookDirection = ((Vec3)(ref val)).NormalizedCopy();
			if (IsAgentNearlyStationary(agent))
			{
				Vec3 val2 = agent.Position;
				if (_staringAgentAnchors.TryGetValue(agent.Index, out var value))
				{
					val2 = value;
				}
				WorldPosition val3 = default(WorldPosition);
				((WorldPosition)(ref val3))._002Ector(Mission.Current.Scene, val2);
				Vec2 asVec = ((Vec3)(ref val)).AsVec2;
				agent.SetScriptedPositionAndDirection(ref val3, ((Vec2)(ref asVec)).RotationInRadians, false, (AIScriptedFrameFlags)18);
			}
		}
		catch
		{
		}
	}

	private static bool IsAgentNearlyStationary(Agent agent)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			int result;
			if (agent != null)
			{
				Vec3 velocity = agent.Velocity;
				Vec2 asVec = ((Vec3)(ref velocity)).AsVec2;
				result = ((((Vec2)(ref asVec)).LengthSquared <= 0.01f) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
		catch
		{
			return false;
		}
	}

	private void ResetStaringForActiveInteraction(List<Agent> nearbyAgents, Agent primaryTarget)
	{
		List<Agent> list = nearbyAgents ?? new List<Agent>();
		List<Agent> list2 = ((primaryTarget != null) ? GetPassiveCooldownGroupAgents(primaryTarget) : list);
		List<NpcDataPacket> participantsData = (from a in list2
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		ApplyInteractionGraceAndGroupCooldown(0.75f, 300f, list2, primaryTarget, participantsData);
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
		if (participantCount > 1 && Mission.Current != null)
		{
			_stopStaringTime = Math.Max(_stopStaringTime, Mission.Current.CurrentTime + 60f);
		}
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
		Math.Max(10, maxTokens / 2);
		Mission current = Mission.Current;
		Agent npcAgent = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetNpc.AgentIndex));
		if (!CanAgentParticipateInSceneSpeech(npcAgent))
		{
			return;
		}
		BasicCharacterObject character = npcAgent.Character;
		CharacterObject npcCharacter = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		Hero contextHero = null;
		if (targetNpc.IsHero && resolvedHeroes != null)
		{
			resolvedHeroes.TryGetValue(targetNpc.AgentIndex, out contextHero);
		}
		MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(kingdomIdOverride: TryGetKingdomIdOverrideFromAgent(npcAgent), targetHero: contextHero, input: "请直接根据刚刚发生的公开互动做出即时反应。", extraFact: null, cultureIdOverride: targetNpc.CultureId ?? "neutral", hasAnyHero: targetNpc.IsHero, targetCharacter: npcCharacter, targetAgentIndex: targetNpc.AgentIndex, suppressDynamicRuleAndLore: true);
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
		string baseExtrasWithoutTrust;
		string localExtras = InjectTrustBlockBelowTriState(trustBlock: ExtractTrustPromptBlock(baseExtras, out baseExtrasWithoutTrust), localExtras: stringBuilder.ToString().Trim());
		string text = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
		List<string> historyLines = null;
		lock (_historyLock)
		{
			if (_publicConversationHistory.Count > 0)
			{
				historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, targetNpc.AgentIndex, GetSceneNpcHistoryNameForPrompt(targetNpc));
			}
		}
		string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
		string persistedHeroHistory = (targetNpc.IsHero ? BuildPersistedHeroHistoryContext(targetNpc.AgentIndex, "", resolvedHeroes) : "");
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
		int num = text3.IndexOf(':');
		if (num < 0)
		{
			num = text3.IndexOf('：');
		}
		if (num > 0 && num < 30)
		{
			string text4 = text3.Substring(num + 1).Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				text3 = text4;
			}
		}
		text3 = StripLeakedPromptContentForShout(text3);
		text3 = StripStageDirectionsForPassiveShout(text3);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			RecordResponseForAllNearbySafe(allNpcData, targetNpc.AgentIndex, targetNpc.Name, text3);
			PersistNpcSpeechToNamedHeroes(targetNpc.AgentIndex, targetNpc.Name, text3, allNpcData);
			EnqueueSpeechLine(targetNpc, text3, allNpcData, skipHistory: true, suppressStare);
		}
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
			Hero value = null;
			resolvedHeroes?.TryGetValue(agentIndex, out value);
			if (value == null)
			{
				return "";
			}
			string latestSceneNpcUtterance = GetLatestSceneNpcUtterance(agentIndex);
			string text = MyBehavior.BuildHistoryContextForExternal(value, 0, currentInput, latestSceneNpcUtterance);
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
					value = (precomputedContexts[agentIndex] = new PrecomputedShoutRagContext());
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
			if (nearbyDatum == null || !nearbyDatum.IsHero)
			{
				continue;
			}
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == nearbyDatum.AgentIndex));
			if (val != null)
			{
				BasicCharacterObject character = val.Character;
				CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val2 != null && val2.HeroObject != null)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(val2.HeroObject, text, null, null, _sceneHistorySessionId);
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
			Mission current = Mission.Current;
			Agent val = ((current == null) ? null : ((IEnumerable<Agent>)current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == nearbyDatum.AgentIndex));
			if (val == null)
			{
				continue;
			}
			BasicCharacterObject character = val.Character;
			CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
			if (val2 != null && val2.HeroObject != null)
			{
				if (nearbyDatum.AgentIndex == speakerAgentIndex)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(val2.HeroObject, null, response, null, _sceneHistorySessionId);
					continue;
				}
				string aiText = "[场景喊话] " + text + ": " + response;
				MyBehavior.AppendExternalSceneDialogueHistory(val2.HeroObject, null, aiText, null, _sceneHistorySessionId);
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
						staringAgent.SetMaximumSpeedLimit(0f, false);
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
			if (staringAgent == null || !staringAgent.IsActive())
			{
				continue;
			}
			try
			{
				staringAgent.SetLookAgent((Agent)null);
				staringAgent.SetMaximumSpeedLimit(-1f, false);
				if (!ShouldPreserveMeetingSceneAutonomy())
				{
					staringAgent.DisableScriptedMovement();
				}
			}
			catch
			{
			}
		}
		_staringAgents.Clear();
		_staringAgentAnchors.Clear();
		_staringUseConversationAgents.Clear();
	}

	private void AddAgentToStareList(Agent agent, bool interruptCurrentUse = false)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (!ShouldSuppressSceneConversationControlForMeeting() && agent != null)
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
				agent.SetMaximumSpeedLimit(0f, false);
				WorldPosition worldPosition = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref worldPosition, false, (AIScriptedFrameFlags)0);
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
		ClearPendingPlayerTurnCarryAgents();
		DeactivateMultiSceneMovementSuppression();
		ResumeGame();
	}
}
