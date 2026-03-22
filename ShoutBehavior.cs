using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
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
		Show
	}

	private class ShoutTradeResourceOption
	{
		public bool IsGold;

		public string ItemId;

		public string Name;

		public int AvailableAmount;

		public ItemObject Item;
	}

	private class ShoutPendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;
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
	}

	private sealed class PendingInteractionTimeoutArm
	{
		public int AgentIndex;

		public long InteractionToken;

		public float ArmAtMissionTime;
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

		public bool CommitHistory;

		public bool SuppressStare;

		public bool AllowPlayerDirectedActions = true;

		public int RequiredConversationEpoch;
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
				catch
				{
				}
			}
			try
			{
				_parent.ProcessDeferredCleanup();
			}
			catch
			{
			}
			try
			{
				_parent.TickLipSyncAnimations(dt);
			}
			catch
			{
			}
			if (Campaign.Current != null && Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				_parent._stareTimer = 0f;
				_parent._currentStareTarget = null;
				_parent.ResetStaringBehavior();
				return;
			}
			_parent.UpdateStaringBehavior();
			_parent.ProcessPendingInteractionTimeoutArms();
			_parent.UpdateActiveInteractionTimeouts();
			if (_parent._interactionGraceTimer > 0f)
			{
				_parent._interactionGraceTimer -= dt;
			}
			_parent._tickTimer += dt;
			if (_parent._tickTimer >= 0.2f)
			{
				_parent.UpdatePassiveStareLogic(_parent._tickTimer);
				_parent._tickTimer = 0f;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			InputKey key = InputKey.K;
			if (!string.IsNullOrEmpty(settings.ShoutKey) && Enum.TryParse<InputKey>(settings.ShoutKey.ToUpper(), out var result2))
			{
				key = result2;
			}
			bool flag = true;
			try
			{
				flag = IsGameWindowFocused();
			}
			catch
			{
				flag = true;
			}
			if (_parent._wasGameWindowFocused && !flag)
			{
				try
				{
					_parent.HandleEscapePressedForAudioSafety("FOCUS_LOST");
				}
				catch
				{
				}
			}
			_parent._wasGameWindowFocused = flag;
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

	private bool _shoutPendingTradeIsGive = false;

	private NpcDataPacket _shoutTradeTargetNpc = null;

	private readonly Dictionary<string, ScenePrepaidTransferRecord> _scenePrepaidTransfers = new Dictionary<string, ScenePrepaidTransferRecord>(StringComparer.OrdinalIgnoreCase);

	private List<Agent> _staringAgents = new List<Agent>();

	private readonly Dictionary<int, Vec3> _staringAgentAnchors = new Dictionary<int, Vec3>();

	private readonly HashSet<int> _staringUseConversationAgents = new HashSet<int>();

	private float _stopStaringTime = 0f;

	private Dictionary<int, List<ConversationMessage>> _npcConversationHistory = new Dictionary<int, List<ConversationMessage>>();

	private List<ConversationMessage> _publicConversationHistory = new List<ConversationMessage>();

	private static int _sceneHistorySessionId = 0;

	private int _sceneConversationEpoch = 0;

	private readonly object _autoGroupChatLock = new object();

	private AutoNpcGroupChatSession _autoGroupChatSession = null;

	private bool _autoGroupChatLoopRunning = false;

	private const int MAX_HISTORY_TURNS = 20;

	private const int AUTO_GROUP_CHAT_MAX_LINES = 8;

	private const int AUTO_GROUP_CHAT_IDLE_POLL_MS = 250;

	private const int AUTO_GROUP_CHAT_CONSECUTIVE_NO_CONTINUE_LIMIT = 2;

	private Agent _currentStareTarget = null;

	private float _stareTimer = 0f;

	private float _stareTargetLostGraceTimer = 0f;

	private float _interactionGraceTimer = 0f;

	private Dictionary<string, float> _passiveCooldowns = new Dictionary<string, float>(StringComparer.Ordinal);


    private const float STARE_TRIGGER_TIME = 6f;

	private const float STARE_TARGET_LOST_GRACE = 2f;

	private const float PASSIVE_STARE_COOLDOWN = 10f;

	private const float ACTIVE_CHAT_COOLDOWN = 120f;

	private const float PASSIVE_INTERACTION_GRACE = 0.75f;

	private const float ACTIVE_INTERACTION_IDLE_TIMEOUT = 20f;

	private const float ACTIVE_INTERACTION_IDLE_PLAYER_RANGE = 10f;

	private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

	private float _tickTimer = 0f;

	private readonly HashSet<int> _speakingAgentIndices = new HashSet<int>();

	private readonly object _speakingLock = new object();

	private readonly Dictionary<int, SoundEvent> _agentSoundEvents = new Dictionary<int, SoundEvent>();

	private readonly Dictionary<int, string> _agentWavPaths = new Dictionary<int, string>();

	private readonly Dictionary<int, string> _agentXmlPaths = new Dictionary<int, string>();

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

	private readonly Dictionary<int, Queue<long>> _pendingSpeechCompletionTokenQueues = new Dictionary<int, Queue<long>>();

	private readonly Dictionary<int, PendingInteractionTimeoutArm> _pendingInteractionTimeoutArms = new Dictionary<int, PendingInteractionTimeoutArm>();

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

	private void EnqueuePendingNpcBubble(int agentIndex, Agent liveAgent, string uiContent, string npcName)
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
				NpcName = npcName
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
				if (hasBubble && bubble != null)
				{
					if (!TryShowNpcBubble(bubble.Agent, bubble.UiContent, typingDuration))
					{
						InformationManager.DisplayMessage(new InformationMessage("[" + (bubble.NpcName ?? "NPC") + "] " + bubble.UiContent, new Color(1f, 0.8f, 0.2f)));
					}
					if (hasInteractionToken)
					{
						ScheduleInteractionTimeoutArm(agentIndex, interactionToken, (typingDuration > 0f) ? typingDuration : EstimateBubbleTypingDurationSeconds(bubble.UiContent));
					}
				}
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

	private void ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true)
	{
		if (!CanAgentParticipateInSceneSpeech(liveAgent))
		{
			return;
		}
		string text = content;
		try
		{
			string text2 = BuildPatienceBadgeForNpc(npc, liveAgent);
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(content))
			{
				text = "【" + text2 + "】" + content;
			}
		}
		catch
		{
		}
		int num = npc?.AgentIndex ?? (-1);
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
		int num2 = ((flag2 && attachTtsToSceneAgent && num >= 0 && CanAgentParticipateInSceneSpeech(liveAgent)) ? num : (-1));
		LogTtsReport("ShowNpcSpeechOutput.Enter", num, $"allowTts={allowTts};attachToSceneAgent={attachTtsToSceneAgent};effectiveAgentIndex={num2};contentLen={(content ?? string.Empty).Length};hostileSpeech={flagHostileSpeech}");
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
		else if (flag2 && num2 < 0)
		{
			try
			{
				Logger.Log("LipSync", "[SAFEGUARD] Use detached TTS for combat/immediate speech. agentIndex=" + num);
			}
			catch
			{
			}
		}
		if (flag2)
		{
			string text3 = "";
			try
			{
				if (npc != null && npc.IsHero)
				{
					Hero hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
					if (hero != null)
					{
						text3 = MyBehavior.GetNpcVoiceIdForExternal(hero);
						if (string.IsNullOrWhiteSpace(text3))
						{
							text3 = VoiceMapper.ResolveVoiceId(hero);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text3) && npc != null)
				{
					text3 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, npc.AgentIndex);
				}
				flag = TtsEngine.Instance.SpeakAsync(content, -1, -1f, num2, text3);
			}
			catch
			{
			}
			LogTtsReport("ShowNpcSpeechOutput.SpeakAttempt", num, $"effectiveAgentIndex={num2};speakAccepted={flag};voiceId={text3}");
		}
		if (flag && num2 >= 0 && CanAgentParticipateInSceneSpeech(liveAgent))
		{
			if (interactionToken != 0L)
			{
				EnqueuePendingSpeechCompletionToken(num, interactionToken);
			}
			EnqueuePendingNpcBubble(num, liveAgent, text, npc?.Name ?? "NPC");
			LogTtsReport("ShowNpcSpeechOutput.SceneBubbleQueued", num, $"interactionToken={interactionToken};uiLen={text.Length}");
			return;
		}
		float num3 = EstimateBubbleTypingDurationSeconds(text);
		if (!TryShowNpcBubble(liveAgent, text, num3))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + (npc?.Name ?? "NPC"));
			InformationManager.DisplayMessage(new InformationMessage("[" + (npc?.Name ?? "NPC") + "] " + text, new Color(1f, 0.8f, 0.2f)));
		}
		if (interactionToken != 0L)
		{
			ScheduleInteractionTimeoutArm(num, interactionToken, num3);
		}
		LogTtsReport("ShowNpcSpeechOutput.BubbleFallback", num, $"interactionToken={interactionToken};typingDuration={num3:F2};ttsAccepted={flag};ttsEnabled={flag2}");
		if (!(!flag && flag2))
		{
			return;
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
			TtsEngine.Instance.SpeakAsync(content, -1, -1f, num2, text4);
			LogTtsReport("ShowNpcSpeechOutput.DetachedSpeakFallback", num, $"effectiveAgentIndex={num2};voiceId={text4}");
		}
		catch
		{
		}
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

	private static string BuildSceneNpcRoleIntroForPrompt(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null)
	{
		if (npc == null)
		{
			return "";
		}
		string name = (npc.Name ?? "").Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "未命名NPC";
		}
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
				if (RewardSystemBehavior.Instance != null)
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
				if (RewardSystemBehavior.Instance != null && characterObject != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var _))
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
				.Append(name)
				.Append("，你的身份是")
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
		string playerIntroLine = BuildScenePlayerIntroForPrompt();
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

	private static string BuildSceneSystemTopPromptIntroForSingle(NpcDataPacket npc, Hero hero, IEnumerable<NpcDataPacket> presentNpcs = null)
	{
		return BuildSceneNpcRoleIntroForPrompt(npc, hero, presentNpcs);
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

	private static string BuildPlayerSceneIdentityClauseForPrompt(Hero playerHero)
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
					return kingdomName + "的雇佣兵";
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
				return factionName + "的统治者";
			}
			if (playerHero.IsLord && !playerHero.IsFactionLeader && kingdom != null && !string.IsNullOrWhiteSpace(factionName))
			{
				return factionName + "的封臣";
			}
		}
		catch
		{
		}
		return "";
	}

	private static string BuildScenePlayerIntroForPrompt()
	{
		Hero playerHero = Hero.MainHero;
		if (playerHero == null)
		{
			return "";
		}
		string culture = "未知文化";
		string age = "未知";
		string equipment = "未知";
		string identityClause = BuildPlayerSceneIdentityClauseForPrompt(playerHero);
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
		if (clanTier >= 2)
		{
			string reputation = MyBehavior.GetClanTierReputationLabelForExternal(clanTier) + $"（{Math.Max(0, clanTier)} level）";
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，你知道他的名声，他")
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
		if (!string.IsNullOrWhiteSpace(identityClause))
		{
			stringBuilder.Append("而且他还是").Append(identityClause).Append("。");
		}
		return stringBuilder.ToString().Trim();
	}

	private static string BuildNearbyPresentNpcLineForPrompt(NpcDataPacket selfNpc, IEnumerable<NpcDataPacket> presentNpcs)
	{
		if (selfNpc == null || presentNpcs == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		List<string> nonHeroOrder = new List<string>();
		Dictionary<string, int> nonHeroCounts = new Dictionary<string, int>(StringComparer.Ordinal);
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
			string text = (presentNpc.Name ?? "").Trim();
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
			if (!nonHeroCounts.ContainsKey(text))
			{
				nonHeroCounts[text] = 0;
				nonHeroOrder.Add(text);
			}
			nonHeroCounts[text]++;
		}
		foreach (string item in nonHeroOrder)
		{
			if (!nonHeroCounts.TryGetValue(item, out var value) || value <= 0)
			{
				continue;
			}
			if (value == 1)
			{
				list.Add(item);
			}
			else
			{
				list.Add("另外" + ConvertCountToChineseForPrompt(value) + "个" + item);
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
		if (text.StartsWith("【NPC当前可用财富与物品】", StringComparison.Ordinal))
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
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[ACTION:[^\\]]*\\]", "", RegexOptions.IgnoreCase).Trim();
	}

	private static bool ContainsAutoGroupNoContinueSignal(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && text.IndexOf("[NO_CONTINUE]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string StripAutoGroupStopSignal(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r", ""), "\\[NO_CONTINUE\\]", "", RegexOptions.IgnoreCase).Trim();
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
			NpcDataPacket speakerData = ShoutUtils.ExtractNpcData(speakerAgent);
			if (speakerData == null)
			{
				return;
			}
			string safeContent = content.Trim();
			_mainThreadActions.Enqueue(delegate
			{
				try
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerData.AgentIndex);
					string aiResponse = safeContent;
					bool meetingTauntEscalated = false;
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
								LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref aiResponse, out meetingTauntEscalated);
								SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
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
							if (agent != null && agent.Character is CharacterObject characterObject3)
							{
								SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
							}
						}
					}
					catch
					{
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
							AddAgentToStareList(agent, interruptCurrentUse: true);
						}
						RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);
						PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);
					}
					if (flag)
					{
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
						lock (_speakingLock)
						{
							_agentWavPaths[agentIndex] = wavPath;
							_agentXmlPaths[agentIndex] = xmlPath;
						}
						LogTtsReport("AudioFileReady", agentIndex, $"duration={durationSecs:F2};wav={System.IO.Path.GetFileName(wavPath)};xml={System.IO.Path.GetFileName(xmlPath)}");
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
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
									if (agent != null && agent.IsActive() && !(agent.AgentVisuals == null))
									{
										lock (_speakingLock)
										{
											if (_agentSoundEvents.Count > 0)
											{
												foreach (KeyValuePair<int, SoundEvent> agentSoundEvent in _agentSoundEvents)
												{
													StopAgentRhubarbRecordIfPossible(agentSoundEvent.Key, "OnAudioFileReady.ReplaceExisting");
													if (!IsInEscapeTransitionWindow())
													{
														SafeStopAndReleaseSoundEvent(agentSoundEvent.Value);
													}
												}
												_agentSoundEvents.Clear();
											}
										}
										SoundEvent soundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", wavPath, Mission.Current.Scene, is3d: false, isBlocking: false);
										if (soundEvent == null)
										{
											Logger.Log("LipSync", $"[WARN] SoundEvent.CreateEventFromExternalFile 返回 null, agentIndex={agentIndex}");
										}
										else
										{
											soundEvent.SetPosition(agent.Position);
											soundEvent.Play();
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
											int soundId = soundEvent.GetSoundId();
											if (soundId <= 0)
											{
												Logger.Log("LipSync", $"[WARN] SoundEvent.GetSoundId 非法({soundId})，跳过 StartRhubarbRecord, agentIndex={agentIndex}");
												SafeStopAndReleaseSoundEvent(soundEvent);
											}
											else
											{
												Logger.Log("LipSync", $"[Rhubarb] SoundEvent created, vol={num:F2}, agentIndex={agentIndex}, soundId={soundId}");
												lock (_speakingLock)
												{
													if (_agentSoundEvents.TryGetValue(agentIndex, out var value) && !IsInEscapeTransitionWindow())
													{
														StopAgentRhubarbRecordIfPossible(agentIndex, "OnAudioFileReady.ReplaceSameAgent");
														SafeStopAndReleaseSoundEvent(value);
													}
													_agentSoundEvents[agentIndex] = soundEvent;
												}
												agent.AgentVisuals.StartRhubarbRecord(xmlPath, soundId);
												Logger.Log("LipSync", $"[Rhubarb] StartRhubarbRecord 调用成功, agentIndex={agentIndex}, soundId={soundId}");
											}
										}
									}
								}
							}
							catch (Exception ex3)
							{
								Logger.Log("LipSync", "[ERROR] OnAudioFileReady 主线程处理失败: " + ex3.Message);
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
							}
							LogTtsReport("PlaybackStarted.InvalidAgent", agentIndex);
							return;
						}
						lock (_speakingLock)
						{
							_speakingAgentIndices.Add(agentIndex);
						}
						Logger.Log("LipSync", $"[OnPlaybackStarted] agentIndex={agentIndex} added to speaking set");
						LogTtsReport("PlaybackStarted", agentIndex);
						if (TryDequeuePendingNpcBubble(agentIndex, out var bubble, out var typingDuration))
						{
							_mainThreadActions.Enqueue(delegate
							{
								try
								{
									if (!TryShowNpcBubble(bubble.Agent, bubble.UiContent, typingDuration))
									{
										InformationManager.DisplayMessage(new InformationMessage("[" + (bubble.NpcName ?? "NPC") + "] " + bubble.UiContent, new Color(1f, 0.8f, 0.2f)));
									}
								}
								catch
								{
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
								LogTtsReport("PlaybackFinished.MainThreadCleanupStart", agentIndex);
								StopAgentRhubarbRecordIfPossible(agentIndex, "PlaybackFinished");
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
								}
								QueueDeferredCleanup(value, value2, value3, "PlaybackFinished", agentIndex);
								Logger.Log("LipSync", $"[Rhubarb] cleanup queued, agentIndex={agentIndex}, hasSe={(value != null)}, hasWav={(!string.IsNullOrWhiteSpace(value2))}, hasXml={(!string.IsNullOrWhiteSpace(value3))}");
								LogTtsReport("PlaybackFinished.CleanupQueued", agentIndex, $"hasSe={(value != null)};hasWav={(!string.IsNullOrWhiteSpace(value2))};hasXml={(!string.IsNullOrWhiteSpace(value3))}");
								LogTtsReport("PlaybackFinished.MainThreadCleanupEnd", agentIndex);
							}
							catch
							{
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
			catch
			{
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

	private static void SafeStopAndReleaseSoundEvent(SoundEvent se)
	{
		if (se == null)
		{
			return;
		}
		try
		{
			se.SetParameter("Volume", 0f);
		}
		catch
		{
		}
		try
		{
			se.Pause();
		}
		catch
		{
		}
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
		_ttsPausedByInterruption = false;
		_ttsPausedByShoutUi = false;
		List<int> list = null;
		lock (_speakingLock)
		{
			list = _agentSoundEvents.Keys.ToList();
			_speakingAgentIndices.Clear();
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
				if (agent != null && agent.IsActive())
				{
					continue;
				}
				LogTtsReport("AgentInvalidDuringLipSync", agentIndex);
				CancelAgentSpeechForRemoval(agentIndex, "agent_invalid_during_lipsync");
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
			if (MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated)
			{
				return false;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return false;
			}
			bool flag = DuelBehavior.IsFormalDuelActive;
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
			if (!flag && main.Team != null && targetAgent.Team != null)
			{
				flag = main.Team.IsEnemyOf(targetAgent.Team);
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
			if (DuelBehavior.IsArenaMissionActive || DuelBehavior.IsFormalDuelActive)
			{
				return true;
			}
			string text = (Mission.Current?.SceneName ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text) && text.Contains("arena"))
			{
				return true;
			}
			string text2 = (ShoutUtils.GetCurrentSceneDescription() ?? "").Trim();
			return !string.IsNullOrWhiteSpace(text2) && text2.IndexOf("竞技场", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
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

	private static string TryGetActiveWeaponDisplayNameForPassiveReaction(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return "";
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (primaryWieldedItemIndex != EquipmentIndex.None && IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[primaryWieldedItemIndex]))
			{
				string text = agent.Equipment[primaryWieldedItemIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text.Trim();
				}
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex != EquipmentIndex.None && IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[offhandWieldedItemIndex]))
			{
				string text2 = agent.Equipment[offhandWieldedItemIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2.Trim();
				}
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
			if (primaryWieldedItemIndex != EquipmentIndex.None && IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[primaryWieldedItemIndex]))
			{
				return true;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return offhandWieldedItemIndex != EquipmentIndex.None && IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[offhandWieldedItemIndex]);
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
		string text2 = string.Join("、", (allNpcData ?? new List<NpcDataPacket>()).Where((NpcDataPacket npc) => npc != null && npc.AgentIndex >= 0 && npc.Name != null && !string.Equals((npc.Name ?? "").Trim(), text, StringComparison.Ordinal))
			.Select((NpcDataPacket npc) => npc.Name.Trim())
			.Where((string name) => !string.IsNullOrWhiteSpace(name))
			.Distinct()
			.Take(5));
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "周围的人";
		}
		return text + "现在正在和" + text2 + "继续聊天，玩家暂时没有插话。请只以" + text + "的身份，自然接续【当前场景公共对话与互动】里最新的内容，对周围NPC说一句话。优先回应别人刚刚说的话，不要重复自己，不要替其他角色发言，不要输出角色名，不要动作描写、心理活动、旁白，不要输出任何[ACTION:*]标签。如果你觉得此时没有必要继续说话，请只输出 [NO_CONTINUE]。";
	}

	private async Task<string> GenerateAutoGroupChatLineAsync(NpcDataPacket speakerNpc, List<NpcDataPacket> allNpcData, Dictionary<int, Hero> resolvedHeroes)
	{
		try
		{
			if (speakerNpc == null || allNpcData == null || allNpcData.Count < 2)
			{
				return "";
			}
			await EnsurePersonaForCandidatesAsync(new List<NpcDataPacket> { speakerNpc }, resolvedHeroes ?? new Dictionary<int, Hero>());
			DuelSettings settings = DuelSettings.GetSettings();
			int maxTokens = Math.Max(28, settings.ShoutMaxTokens / 2);
			int minTokens = Math.Max(10, maxTokens / 2);
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
			MyBehavior.ShoutPromptContext shoutPromptContext = MyBehavior.BuildShoutPromptContextForExternal(hero, "请根据当前场景公共对话与互动继续和周围NPC闲聊。", null, speakerNpc.CultureId ?? "neutral", hasAnyHero: speakerNpc.IsHero, targetCharacter: characterObject, kingdomIdOverride: kingdomIdOverride, targetAgentIndex: speakerNpc.AgentIndex, suppressDynamicRuleAndLore: true);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【在场人物列表】：");
			foreach (NpcDataPacket npc in allNpcData)
			{
				if (npc != null)
				{
					stringBuilder.AppendLine("- 名字: " + npc.Name + " | 身份: " + npc.RoleDesc);
				}
			}
			stringBuilder.AppendLine("【群体续聊规则】");
			stringBuilder.AppendLine("1. 现在是NPC之间自己继续聊天，不是在回复玩家。");
			stringBuilder.AppendLine("2. 只输出你自己说的一句话，不要替其他NPC说话。");
			stringBuilder.AppendLine("3. 不要输出任何[ACTION:*]标签。");
			stringBuilder.AppendLine("4. 如果此时没必要继续说话，只输出 [NO_CONTINUE]。");
			string baseExtras = StripScenePersonaBlocks((shoutPromptContext?.Extras ?? "").Trim());
			string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
			string localExtras = InjectTrustBlockBelowTriState(stringBuilder.ToString().Trim(), trustBlock);
			string text = string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust);
			List<string> historyLines = null;
			lock (_historyLock)
			{
				if (_publicConversationHistory.Count > 0)
				{
					historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, speakerNpc.AgentIndex, speakerNpc.Name ?? "", useNpcNameAddress: false);
				}
			}
			string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
			string persistedHeroHistory = speakerNpc.IsHero ? BuildPersistedHeroHistoryContext(speakerNpc.AgentIndex, "", resolvedHeroes) : "";
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
			if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
			{
				layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
			}
			string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(speakerNpc, hero, allNpcData);
			if (!string.IsNullOrWhiteSpace(roleTopIntro))
			{
				layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
			}
			string userContent = BuildAutoGroupChatReplyInstruction(speakerNpc.Name ?? "NPC", allNpcData) + "\n" + $"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；如果没必要继续说话，只输出 [NO_CONTINUE])";
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
			Logger.Log("ShoutBehavior", "[AutoGroupChat] prompt failed: " + ex.Message);
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
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, inputActionText, fullExtra, cultureId, hasAnyHero: data.IsHero, targetCharacter: passiveCharacter, kingdomIdOverride: passiveKingdomIdOverride, targetAgentIndex: data.AgentIndex, usePrefetchedLoreContext: precalculatedLore != null, prefetchedLoreContext: loreContext);
				DuelSettings settings = DuelSettings.GetSettings();
				int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
				int minTokens = maxTokens / 2;
				if (minTokens < 5)
				{
					minTokens = 5;
				}
				StringBuilder sysPrompt = new StringBuilder();
				string npcName = (string.IsNullOrWhiteSpace(data.Name) ? "未命名NPC" : data.Name);
				sysPrompt.AppendLine("【在场人物列表】：");
				foreach (NpcDataPacket npc2 in allNpcData)
				{
					if (npc2 == null)
					{
						continue;
					}
					string line = "- 名字: " + npc2.Name + " | 身份: " + npc2.RoleDesc;
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
					bool hasPatience = ((hero == null) ? MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(data.UnnamedKey, data.Name, out patienceLine, out canSpeak) : MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out patienceLine, out canSpeak));
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
					sysPrompt.AppendLine("【群体对话规则】");
					sysPrompt.AppendLine("1. 如果【在场人物列表】中只有一个NPC，那他必须回应" + playerNameForPrompt + "。");
					sysPrompt.AppendLine("2. 【在场人物列表】中最上方的NPC是主要对话对象，必须回复" + playerNameForPrompt + "，其他NPC可以酌情选择是否回复");
					sysPrompt.AppendLine("3. 你只需以自己的身份输出一行回复，不需要包含角色名开头。");
					sysPrompt.AppendLine("4. 禁止动作描写、心理活动、括号备注；只保留角色说出口的话，不要加引号，但是其他规则要求你加入标签你必须加入标签");
					sysPrompt.AppendLine("5. 若多人在场，请注意你的身份和性格，给出与其他NPC不同的独特见解，避免重复相似的内容。");
					sysPrompt.AppendLine("6. kingdom_service 标签去重：仅在你明确同意“加入势力/成为封臣/退出当前效力”时才可输出 [ACTION:KINGDOM_SERVICE:*:*]；若【当前场景公共对话与互动】中已出现同事项的 kingdom_service 标签，本轮你只能口头补充，不得重复输出。");
					sysPrompt.AppendLine("7. marriage 标签去重：同一轮同一事项最多出现一个结婚标签；若已有角色输出 [ACTION:MARRIAGE_FORMAL:*] 或 [ACTION:MARRIAGE_ELOPE:*]，其他角色只能口头补充。");
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
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(data, hero, allNpcData);
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
			new InquiryElement("show", "向其展示物品并交流", null, isEnabled: true, "")
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

	private void BeginShoutTradeFlow(NpcDataPacket targetNpc, bool isGive)
	{
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
			list.Add(new InquiryElement(i, $"{shoutTradeResourceOption.Name} (×{shoutTradeResourceOption.AvailableAmount})", null, isEnabled: true, $"可用数量: {shoutTradeResourceOption.AvailableAmount}"));
		}
		string text = targetNpc?.Name ?? "附近的人";
		string titleText = (isGive ? ("给予其物品并交流 - " + text) : ("向其展示物品并交流 - " + text));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, "当前目标：" + text + "\n选择要使用的物品或第纳尔（可多选）：", list, isExitShown: true, 1, list.Count, "确定", "取消", OnShoutTradeResourcesSelected, delegate
		{
			ResetShoutTradeState();
			ResumeGame();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private List<ShoutTradeResourceOption> BuildShoutTradeOptions()
	{
		List<ShoutTradeResourceOption> list = new List<ShoutTradeResourceOption>();
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return list;
		}
		Hero hero = null;
		string text = "";
		if (!_shoutPendingTradeIsGive)
		{
			text = ResolveShownTradeTargetKey(_shoutTradeTargetNpc, out hero);
		}
		int num = Hero.MainHero?.Gold ?? 0;
		if (!_shoutPendingTradeIsGive)
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
				}
				else
				{
					dictionary[text2] = new ShoutTradeResourceOption
					{
						IsGold = false,
						ItemId = text2,
						Name = item.Name.ToString(),
						AvailableAmount = amount,
						Item = item
					};
				}
			}
			foreach (ShoutTradeResourceOption value2 in dictionary.Values)
			{
				int availableAmount = value2.AvailableAmount;
				if (!_shoutPendingTradeIsGive)
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
		string titleText = (_shoutPendingTradeIsGive ? "给予数量" : "展示数量");
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
			BeginShoutTradeFlow(_shoutTradeTargetNpc, _shoutPendingTradeIsGive);
		}));
	}

	private void ShowShoutTradeChatInput()
	{
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
		if (_shoutPendingTradeIsGive)
		{
			ApplyShoutGiveTransfer();
			text = BuildShoutTradeFactText(isGive: true);
		}
		else
		{
			RecordShoutShownResources();
			text = BuildShoutTradeFactText(isGive: false);
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
			if (shoutPendingTradeItem.IsGold)
			{
				int num = Math.Min(shoutPendingTradeItem.Amount, Hero.MainHero.Gold);
				if (num > 0)
				{
					if (hero != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, num);
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, num, null, 0);
						}
						catch
						{
						}
					}
					else if (flag && currentSettlement != null)
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num, disableNotification: true);
						currentSettlement.SettlementComponent?.ChangeGold(num);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, num, null, 0);
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num} 第纳尔。", characterObject?.Name?.ToString());
					}
					else
					{
						GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num, disableNotification: true);
						RecordScenePrepaidTransfer(text, num);
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
				int num2 = MyBehavior.RemoveItemsFromRosterByStringId(itemRoster, text2, shoutPendingTradeItem.Amount, out itemObject);
				if (num2 > 0)
				{
					if (hero?.PartyBelongedTo != null && itemObject != null)
					{
						hero.PartyBelongedTo.ItemRoster.AddToCounts(itemObject, num2);
					}
					else if (flag && currentSettlement?.ItemRoster != null && itemObject != null)
					{
						currentSettlement.ItemRoster.AddToCounts(itemObject, num2);
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransferForMerchant(currentSettlement, settlementMerchantKind, 0, text2, num2);
						string text3 = RewardSystemBehavior.Instance?.BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, num2) ?? "";
						RewardSystemBehavior.Instance?.AppendSettlementMerchantNpcFact(currentSettlement, settlementMerchantKind, $"你已经收下了玩家交来的 {num2} 个 {itemObject.Name?.ToString() ?? text2}{text3}。", characterObject?.Name?.ToString());
					}
					if (hero != null)
					{
						try
						{
							RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, 0, text2, num2);
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
		string text = GetPlayerDisplayNameForShout();
		string text2 = GetShoutTradeTargetDisplayName();
		List<string> list = new List<string>();
		for (int i = 0; i < _shoutPendingTradeItems.Count; i++)
		{
			ShoutPendingTradeItem shoutPendingTradeItem = _shoutPendingTradeItems[i];
			if (shoutPendingTradeItem.Amount > 0)
			{
				if (shoutPendingTradeItem.IsGold)
				{
					list.Add($"{shoutPendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					string text3 = shoutPendingTradeItem.ItemId ?? shoutPendingTradeItem.Item?.StringId ?? "";
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
		return text + "给 " + text2 + " 看了看 " + string.Join("、", list) + "，证明自己有这些东西。";
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
		ResetStaringForActiveInteraction(nearbyAgents, primaryTarget);
		if (!string.IsNullOrWhiteSpace(extraFact))
		{
			RecordExtraFactToSceneHistory(extraFact, allNpcData);
			_mainThreadActions.Enqueue(delegate
			{
				PersistExtraFactToNamedHeroes(extraFact, allNpcData);
			});
		}
		InformationManager.DisplayMessage(new InformationMessage("[你] " + shoutText, new Color(0.3f, 1f, 0.3f)));
		if (_floatingTextView != null && Agent.Main != null)
		{
			_floatingTextView.AddOrUpdateText(Agent.Main, shoutText);
		}
		List<NpcDataPacket> capturedNpcData = allNpcData;
		RecordPlayerMessage(shoutText, capturedNpcData, primaryDataPacket?.AgentIndex ?? (-1), primaryDataPacket?.Name ?? "");
		TrackPlayerInteraction(primaryDataPacket);

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
						HasLoreContext = true,
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
							hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, out statusLine, out canSpeak);
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
				sysPrompt.AppendLine("【在场人物列表】：");
				foreach (NpcDataPacket npc2 in speakingCandidates)
				{
					if (npc2 == null)
					{
						continue;
					}
					string line = "- 名字: " + npc2.Name + " | 身份: " + npc2.RoleDesc;
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
				sysPrompt.AppendLine("1. 如果【在场人物列表】中只有一个NPC，那他必须回应" + playerNameForPrompt + "。");
				sysPrompt.AppendLine("2. 【在场人物列表】中最上方的NPC是主要对话对象，必须回复" + playerNameForPrompt + "，其他NPC可以酌情选择是否回复");
				sysPrompt.AppendLine("3. 格式必须为：[角色名]: [纯对话内容]（每名说话者单独一行，禁止同一行出现两个角色）。");
				sysPrompt.AppendLine("4. 禁止动作描写、心理活动、括号备注；只保留角色说出口的话，不要加引号，但是其他规则要求你加入标签你必须加入标签");
				sysPrompt.AppendLine("5. 若多人在场，请注意你的身份和性格，给出与其他NPC不同的独特见解，避免重复相似的内容。");
				sysPrompt.AppendLine("5. [角色名] 必须来自【在场人物列表】，即便有同名角色，也禁止自创姓名或错名，比如某某甲，某某乙，某某1，某某2");
				sysPrompt.AppendLine("6. 若多人在场，回复之间应彼此连贯。");
				sysPrompt.AppendLine("7. kingdom_service 标签去重：同一轮同一事项最多出现一个 [ACTION:KINGDOM_SERVICE:*:*]；若某角色已给出同事项标签，其他角色只能口头补充，禁止重复输出 kingdom_service 标签。");
				sysPrompt.AppendLine("8. marriage 标签去重：同一轮同一事项最多出现一个结婚标签；若某角色已给出 [ACTION:MARRIAGE_FORMAL:*] 或 [ACTION:MARRIAGE_ELOPE:*]，其他角色只能口头补充。");
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
							historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, historySourceIndex, primaryNpc?.Name ?? "", useNpcNameAddress: true);
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
				bool hasAnyQueuedLine = false;
				ftm.OnPartialLineUpdated = null;
				ftm.OnNewLineReady = delegate(NpcDataPacket npcDataPacket, string content)
				{
					if (npcDataPacket != null && !string.IsNullOrWhiteSpace(content))
					{
						hasAnyQueuedLine = true;
						EnqueueSpeechLine(npcDataPacket, content.Trim(), capturedAllNpcData);
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
					hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(npc.UnnamedKey, npc.Name, out statusLine, out canSpeak);
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
			DuelSettings settings = DuelSettings.GetSettings();
			int maxTokens = Math.Max(40, settings.ShoutMaxTokens);
			int minTokens = Math.Max(5, maxTokens / 2);
			int lastSpeakerAgentIndex = -1;
			StringBuilder commonCandidatesList = new StringBuilder();
			commonCandidatesList.AppendLine("【在场人物列表】：");
			foreach (NpcDataPacket npc2 in speakableCandidates)
			{
				if (npc2 == null)
				{
					continue;
				}
				string line = "- 名字: " + npc2.Name + " | 身份: " + npc2.RoleDesc;
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
			foreach (NpcDataPacket npc2 in speakableCandidates)
			{
				if (!IsSceneConversationEpochCurrent(conversationEpoch))
				{
					return;
				}
				if (npc2 == null)
				{
					continue;
				}
				Hero contextHero = null;
				if (npc2.IsHero && resolvedHeroes != null) resolvedHeroes.TryGetValue(npc2.AgentIndex, out contextHero);

				string cultureId = npc2.CultureId ?? "neutral";
				PrecomputedShoutRagContext precomputed = null;
				bool hasPrecomputed = precomputedContexts != null && precomputedContexts.TryGetValue(npc2.AgentIndex, out precomputed);
				string loreContext = (hasPrecomputed && precomputed != null) ? (precomputed.LoreContext ?? "") : "";
				string fullExtra = (string.IsNullOrWhiteSpace(loreContext) ? null : loreContext);
				if (!string.IsNullOrWhiteSpace(extraFact)) fullExtra = (fullExtra == null ? extraFact : (fullExtra + "\n" + extraFact));
				Agent npcAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc2.AgentIndex);
				CharacterObject npcCharacter = npcAgent?.Character as CharacterObject;
				string npcKingdomIdOverride = TryGetKingdomIdOverrideFromAgent(npcAgent);
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, fullExtra, cultureId, hasAnyHero: npc2.IsHero, targetCharacter: npcCharacter, kingdomIdOverride: npcKingdomIdOverride, targetAgentIndex: npc2.AgentIndex, usePrefetchedLoreContext: hasPrecomputed && precomputed != null && precomputed.HasLoreContext, prefetchedLoreContext: precomputed?.LoreContext);
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
					if (string.IsNullOrWhiteSpace(playerNameForPrompt))
					{
						playerNameForPrompt = "玩家";
					}
					local.AppendLine("【群体对话规则】");
					local.AppendLine("1. 如果【在场人物列表】中只有一个NPC，那他必须回应" + playerNameForPrompt + "。");
					local.AppendLine("2. 【在场人物列表】中最上方的NPC是主要对话对象，必须回复" + playerNameForPrompt + "，其他NPC可以酌情选择是否回复");
					local.AppendLine("3. 你只需以自己的身份输出一行回复，不需要包含角色名开头。");
					local.AppendLine("4. 禁止动作描写、心理活动、括号备注；只保留角色说出口的话，不要加引号，但是其他规则要求你加入标签你必须加入标签");
					local.AppendLine("5. 若多人在场，请注意你的身份和性格，给出与其他NPC不同的独特见解，避免重复相似的内容。");
					local.AppendLine("6. 你说的话要考虑【当前场景公共对话与互动】中别人的发言，而不是各说各的，毕竟现在是群聊");
					local.AppendLine("7. kingdom_service 标签去重：仅在你明确同意“加入势力/成为封臣/退出当前效力”时才可输出 [ACTION:KINGDOM_SERVICE:*:*]；若【当前场景公共对话与互动】已出现同事项的 kingdom_service 标签，本轮你只能口头补充，不得重复输出。");
					local.AppendLine("8. marriage 标签去重：同一轮同一事项最多出现一个结婚标签；若已有角色输出 [ACTION:MARRIAGE_FORMAL:*] 或 [ACTION:MARRIAGE_ELOPE:*]，你只能口头补充。");
				}
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string trustBlock = ExtractTrustPromptBlock(baseExtras, out var baseExtrasWithoutTrust);
				string localExtras = InjectTrustBlockBelowTriState(local.ToString().Trim(), trustBlock);
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtrasWithoutTrust) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtrasWithoutTrust + "\n" + localExtras) : baseExtrasWithoutTrust));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				List<string> historyLines = null;
				lock (_historyLock)
				{
					if (_publicConversationHistory.Count > 0)
					{
						historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, npc2.AgentIndex, npc2.Name ?? "", speakableCandidates.Count > 1);
					}
				}
				string scenePublicHistorySection = BuildScenePublicHistorySection(historyLines);
				string persistedHeroHistory = npc2.IsHero ? GetOrBuildPrecomputedPersistedHistoryContext(npc2.AgentIndex, playerText, resolvedHeroes, precomputedContexts) : "";
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
				if (!string.IsNullOrWhiteSpace(scenePublicHistorySection))
				{
					layeredPrompt = layeredPrompt + "\n" + scenePublicHistorySection;
				}
				string roleTopIntro = BuildSceneSystemTopPromptIntroForSingle(npc2, contextHero, speakableCandidates);
				if (!string.IsNullOrWhiteSpace(roleTopIntro))
				{
					layeredPrompt = roleTopIntro + "\n" + layeredPrompt;
				}
				string singleReplyPlayerName = GetPlayerDisplayNameForShout();
				if (string.IsNullOrWhiteSpace(singleReplyPlayerName))
				{
					singleReplyPlayerName = "玩家";
				}
				string singleReplyUserContent = BuildSingleNpcSceneReplyInstruction(npc2.Name ?? "NPC", multiNpcScene) + "\n" + $"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非{singleReplyPlayerName}明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。长度限制不含 ACTION 标签)" + (string.IsNullOrWhiteSpace(scenePatienceInstruction) ? "" : ("\n" + scenePatienceInstruction));
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
				if (!string.IsNullOrWhiteSpace(cleaned))
				{
					lastSpeakerAgentIndex = npc2.AgentIndex;
					EnqueueSpeechLineWithOptions(npc2, cleaned, allNpcData, commitHistory: true, suppressStare: false, allowPlayerDirectedActions: true, conversationEpoch);
				}
			}
			if (IsSceneConversationEpochCurrent(conversationEpoch) && speakableCandidates.Count > 1)
			{
				StartAutoGroupChatSession(speakableCandidates, primaryNpc, conversationEpoch, lastSpeakerAgentIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] HandleGroupResponsePerHeroIndependent: " + ex.Message);
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
			Age = npc.Age
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
		if (allNpcData == null || allNpcData.Count <= 1)
		{
			return;
		}
		foreach (IGrouping<string, NpcDataPacket> item in allNpcData.Where(delegate(NpcDataPacket npc)
		{
			if (npc == null || npc.IsHero)
			{
				return false;
			}
			return !string.IsNullOrWhiteSpace(npc.Name);
		}).GroupBy((NpcDataPacket npc) => npc.Name.Trim(), StringComparer.Ordinal))
		{
			List<NpcDataPacket> list = item.OrderBy((NpcDataPacket npc) => npc.AgentIndex).ToList();
			if (list.Count <= 1)
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Name = item.Key + "#" + (i + 1);
			}
		}
	}

	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false)
	{
		EnqueueSpeechLineWithOptions(npc, content, allNpcData, !skipHistory, suppressStare, allowPlayerDirectedActions: true, requiredConversationEpoch: 0);
	}

	private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch)
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
		}
		lock (_speechQueueLock)
		{
			_speechQueue.Enqueue(new SceneSpeechQueueItem
			{
				Npc = value,
				Content = content,
				ContextSnapshot = value2,
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
									LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref content, out flag);
									SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
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
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject3)
								{
									SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
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
						if (!string.IsNullOrWhiteSpace(content))
						{
							bool flag2 = allowPlayerDirectedActions && !flag && !flagSceneTaunt && ShoutUtils.TryTriggerDuelAction(matchedNpc, ref content);
							bool flag3 = allowPlayerDirectedActions && !flagSceneTaunt && TryTriggerOpenLordsHallAction(matchedNpc, agent, ref content);
							if (!string.IsNullOrWhiteSpace(content))
							{
								if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
								{
									return;
								}
								string historyText = StripActionTagsForSceneSpeech(content);
								bool flag4 = IsAgentHostileToMainAgent(agent);
								ShowNpcSpeechOutput(matchedNpc, agent, content, allowTts: true, attachTtsToSceneAgent: true);
								if (flag4)
								{
									RefreshHostileCombatAgentAutonomy(agent);
								}
								if (CanAgentParticipateInSceneSpeech(agent) && !suppressStare && !flag4)
								{
									AddAgentToStareList(agent, interruptCurrentUse: true);
								}
								if (commitHistory && !string.IsNullOrWhiteSpace(historyText))
								{
									RecordResponseForAllNearbySafe(allNpcData, matchedNpc.AgentIndex, matchedNpc.Name, historyText);
									PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, historyText, allNpcData);
								}
							}
							if (flag2)
							{
								DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
								ShoutUtils.ExecuteDuel(agent);
							}
							if (flag3)
							{
								return;
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
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "user",
				Content = text,
				SpeakerName = "你",
				SpeakerAgentIndex = -1,
				TargetAgentIndex = primaryTargetAgentIndex,
				TargetName = (primaryTargetName ?? "").Trim(),
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
					TargetName = (primaryTargetName ?? "").Trim(),
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
					Content = "[" + speakerName + "]: " + response,
					SpeakerName = speakerName,
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
				SpeakerName = speakerName,
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

	private void TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false)
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
				TrackPlayerInteraction(npcDataPacket, postSpeechLeaveSeconds);
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
			return;
		}
		try
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			if (agent == null || agent.AgentVisuals == null)
			{
				return;
			}
			agent.AgentVisuals.StartRhubarbRecord("", -1);
			Logger.Log("LipSync", $"[Rhubarb] StopRhubarbRecord called, agentIndex={agentIndex}, reason={reason}");
			LogTtsReport("StopRhubarbRecord", agentIndex, "reason=" + reason);
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] StopRhubarbRecord failed, agentIndex={agentIndex}, reason={reason}, error={ex.Message}");
			LogTtsReport("StopRhubarbRecord.Failed", agentIndex, $"reason={reason};error={ex.Message}");
		}
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
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
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
			return;
		}
		LogTtsReport("InterruptAgentSpeechForCombat", agentIndex, $"reason={reason};speaking={speaking};interrupted={interrupted};hadSe={(soundEvent != null)};hadWav={(!string.IsNullOrWhiteSpace(wavPath))};hadXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Interrupt:" + reason);
		QueueDeferredCleanup(soundEvent, wavPath, xmlPath, "Interrupt:" + reason, agentIndex);
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
		}
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Remove(agentIndex);
			_pendingAudioDurationQueues.Remove(agentIndex);
			_pendingSpeechCompletionTokenQueues.Remove(agentIndex);
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
			return agent != null && !agent.IsMainAgent && agent.IsActive() && main != null && main.IsActive() && agent.Team != null && main.Team != null && agent.Team.IsEnemyOf(main.Team);
		}
		catch
		{
			return false;
		}
	}

	private void StartAutoGroupChatSession(List<NpcDataPacket> participants, NpcDataPacket primaryNpc, int conversationEpoch, int lastSpeakerAgentIndex)
	{
		if (!IsSceneConversationEpochCurrent(conversationEpoch) || participants == null)
		{
			return;
		}
		List<int> list = participants.Where((NpcDataPacket npc) => npc != null)
			.Select((NpcDataPacket npc) => npc.AgentIndex)
			.Where((int agentIndex) => agentIndex >= 0)
			.Distinct()
			.ToList();
		if (list.Count < 2)
		{
			return;
		}
		lock (_autoGroupChatLock)
		{
			_autoGroupChatSession = new AutoNpcGroupChatSession
			{
				ConversationEpoch = conversationEpoch,
				PrimaryAgentIndex = ((primaryNpc != null && primaryNpc.AgentIndex >= 0) ? primaryNpc.AgentIndex : list[0]),
				ParticipantAgentIndices = list,
				LastSpeakerAgentIndex = lastSpeakerAgentIndex,
				MaxAutoLines = AUTO_GROUP_CHAT_MAX_LINES,
				IsActive = true
			};
			int num = (lastSpeakerAgentIndex >= 0) ? list.IndexOf(lastSpeakerAgentIndex) : (-1);
			_autoGroupChatSession.NextSpeakerCursor = ((num >= 0) ? ((num + 1) % list.Count) : 0);
		}
		EnsureAutoGroupChatLoopRunning();
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
		_ = Task.Run(async delegate
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
						return;
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
					await Task.Delay(AUTO_GROUP_CHAT_IDLE_POLL_MS);
					continue;
				}
				if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
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
					await Task.Delay(AUTO_GROUP_CHAT_IDLE_POLL_MS);
					continue;
				}
				if (!TryBuildAutoGroupChatSnapshot(autoNpcGroupChatSession, out var participants, out var resolvedHeroes))
				{
					StopAutoGroupChatSession("participants_lost");
					continue;
				}
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
				}
				string text = "";
				try
				{
					text = await GenerateAutoGroupChatLineAsync(nextSpeaker, participants, resolvedHeroes);
				}
				catch (Exception ex)
				{
					Logger.Log("ShoutBehavior", "[AutoGroupChat] generate failed: " + ex.Message);
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
				string text2 = StripAutoGroupStopSignal(text);
				if (flag || string.IsNullOrWhiteSpace(text2))
				{
					bool flag2 = false;
					lock (_autoGroupChatLock)
					{
						if (_autoGroupChatSession != null && _autoGroupChatSession.ConversationEpoch == autoNpcGroupChatSession.ConversationEpoch)
						{
							_autoGroupChatSession.LastSpeakerAgentIndex = nextSpeaker.AgentIndex;
							_autoGroupChatSession.ConsecutiveNoContinueCount++;
							flag2 = _autoGroupChatSession.ConsecutiveNoContinueCount >= AUTO_GROUP_CHAT_CONSECUTIVE_NO_CONTINUE_LIMIT;
						}
					}
					if (flag2)
					{
						StopAutoGroupChatSession("no_continue");
						continue;
					}
					await Task.Delay(AUTO_GROUP_CHAT_IDLE_POLL_MS);
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
					TrackPlayerInteraction(npcDataPacket);
				}
				EnqueueSpeechLineWithOptions(nextSpeaker, text2, participants, commitHistory: true, suppressStare: false, allowPlayerDirectedActions: false, autoNpcGroupChatSession.ConversationEpoch);
				await Task.Delay(AUTO_GROUP_CHAT_IDLE_POLL_MS);
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
		participants = new List<NpcDataPacket>();
		resolvedHeroes = new Dictionary<int, Hero>();
		if (session == null || session.ParticipantAgentIndices == null || session.ParticipantAgentIndices.Count == 0 || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		Vec3 position = Agent.Main.Position;
		foreach (int participantAgentIndex in session.ParticipantAgentIndices)
		{
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == participantAgentIndex);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				continue;
			}
			try
			{
				if (agent.Position.Distance(position) > 8f)
				{
					continue;
				}
			}
			catch
			{
			}
			NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
			if (npcDataPacket == null)
			{
				continue;
			}
			participants.Add(npcDataPacket);
			if (npcDataPacket.IsHero && agent.Character is CharacterObject { HeroObject: not null } characterObject)
			{
				resolvedHeroes[npcDataPacket.AgentIndex] = characterObject.HeroObject;
			}
		}
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
			if (participant == null)
			{
				continue;
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
				hasStatus = MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(participant.UnnamedKey, participant.Name, out statusLine, out canSpeak);
			}
			if (!hasStatus || canSpeak)
			{
				list.Add(participant);
			}
		}
		if (list.Count < 2)
		{
			return null;
		}
		List<int> list2 = participants.Select((NpcDataPacket npc) => npc.AgentIndex).ToList();
		int num = session.NextSpeakerCursor;
		NpcDataPacket npcDataPacket = null;
		for (int i = 0; i < list2.Count; i++)
		{
			int num2 = (num + i) % list2.Count;
			int num3 = list2[num2];
			NpcDataPacket npcDataPacket2 = list.FirstOrDefault((NpcDataPacket npc) => npc.AgentIndex == num3);
			if (npcDataPacket2 == null)
			{
				continue;
			}
			if (npcDataPacket2.AgentIndex == session.LastSpeakerAgentIndex && list.Count > 1)
			{
				continue;
			}
			npcDataPacket = npcDataPacket2;
			num = (num2 + 1) % list2.Count;
			break;
		}
		if (npcDataPacket == null)
		{
			npcDataPacket = list.FirstOrDefault();
			int num4 = list2.IndexOf(npcDataPacket?.AgentIndex ?? (-1));
			num = ((num4 >= 0 && list2.Count > 0) ? ((num4 + 1) % list2.Count) : 0);
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

	private int BeginNewPlayerDrivenSceneConversationEpoch()
	{
		int num = Interlocked.Increment(ref _sceneConversationEpoch);
		StopAutoGroupChatSession("player_turn");
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
		lock (_autoGroupChatLock)
		{
			if (_autoGroupChatSession != null)
			{
				_autoGroupChatSession.IsActive = false;
				_autoGroupChatSession = null;
			}
		}
		if (!string.IsNullOrWhiteSpace(reason))
		{
			Logger.Log("ShoutBehavior", "[AutoGroupChat] stopped: " + reason);
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

	private void TrackPlayerInteraction(NpcDataPacket primaryTarget, float timeoutSeconds = -1f)
	{
		if (primaryTarget == null || primaryTarget.AgentIndex < 0 || Mission.Current == null)
		{
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
			TimeoutSeconds = ((timeoutSeconds > 0f) ? timeoutSeconds : ACTIVE_INTERACTION_IDLE_TIMEOUT),
			InteractionToken = DateTime.UtcNow.Ticks
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
			Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == activeInteractionSession.Key && a.IsActive());
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
		foreach (int item in list)
		{
			if (_activeInteractionSessions.TryGetValue(item, out var value2))
			{
				ExpireActiveInteraction(value2);
			}
			if (_activeInteractionSessions.TryGetValue(item, out var value3) && (value3 == null || ReferenceEquals(value3, value2) || value3.InteractionToken == value2?.InteractionToken))
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
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == session.TargetAgentIndex && a.IsActive());
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		if (IsAgentHostileToMainAgent(agent))
		{
			RestoreAgentAutonomy(agent);
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
		string factText = text + ": "+ text2 +"长时间没有发言，你决定走开去干自己的事了。";
		string prefixedFactText = "[AFEF NPC行为补充] " + factText;
		if (session.TimeoutSeconds > 3.5f)
		{
			TriggerImmediateSceneBehaviorReaction(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f);
			return;
		}
		AppendTargetedSceneNpcFact(prefixedFactText, session.TargetAgentIndex, persistHeroPrivateHistory: true);
		_staringAgents.RemoveAll((Agent a) => a == null || a.Index == session.TargetAgentIndex);
		_staringAgentAnchors.Remove(session.TargetAgentIndex);
		if (_staringAgents.Count == 0)
		{
			_stopStaringTime = 0f;
		}
		RestoreAgentAutonomy(agent);
	}

	private void RestoreAgentAutonomy(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		bool flag = IsAgentHostileToMainAgent(agent);
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
				stringBuilder.AppendLine("- 名字: " + item.Name + " | 身份: " + item.RoleDesc);
			}
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
				historyLines = BuildVisibleSceneHistoryLines(_publicConversationHistory, targetNpc.AgentIndex, targetNpc.Name ?? "", useNpcNameAddress: false);
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
		text3 = Regex.Replace(text3, "\\[ACTION:[^\\]]*\\]", "", RegexOptions.IgnoreCase).Trim();
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
				value.HasPersistedHistoryContext = true;
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
				string aiText = "[场景喊话] " + speakerName + ": " + response;
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
					staringAgent.DisableScriptedMovement();
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
		if (agent != null)
		{
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
		ResumeGame();
	}
}
