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

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class SceneSpeechOutputQueueController
{
    private readonly SceneSpeechOutputQueueControllerPorts _ports;
    internal SceneSpeechOutputQueueController(SceneSpeechOutputQueueControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal readonly object _ttsBubbleSyncLock = new object();

	internal readonly Dictionary<int, Queue<PendingNpcBubbleEntry>> _pendingNpcBubbleQueues = new Dictionary<int, Queue<PendingNpcBubbleEntry>>();

	internal readonly Dictionary<int, Queue<float>> _pendingAudioDurationQueues = new Dictionary<int, Queue<float>>();

	internal readonly HashSet<int> _ttsPlaybackStartedAgents = new HashSet<int>();

	internal readonly Dictionary<int, Queue<long>> _pendingSpeechCompletionTokenQueues = new Dictionary<int, Queue<long>>();

	internal readonly Dictionary<int, Queue<PendingSceneDialogueFeedEntry>> _pendingSceneDialogueFeedQueues = new Dictionary<int, Queue<PendingSceneDialogueFeedEntry>>();

	internal void EnqueuePendingNpcBubble(int agentIndex, Agent liveAgent, string uiContent, string npcName, float fallbackDurationSeconds = -1f)
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

	internal void EnqueuePendingAudioDuration(int agentIndex, float durationSeconds)
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

	internal bool TryDequeuePendingNpcBubble(int agentIndex, out PendingNpcBubbleEntry bubble, out float typingDurationSeconds)
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

	internal bool TryDispatchPendingNpcBubbleForTts(int agentIndex, bool allowFallbackDuration)
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

	internal void SchedulePendingNpcBubbleFallbackDispatch(TtsEngine.PlaybackRequest request, int delayMs = 180, int remainingRetries = 3)
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

	internal void ClearOrphanPendingAudioDuration(int agentIndex)
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

	internal void ClearPendingTtsBubbleSyncForAgent(int agentIndex, bool clearInteractionToken = false)
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

	internal void ClearPendingSceneDialogueFeedForAgent(int agentIndex)
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

	internal void EnqueuePendingSceneDialogueFeed(int agentIndex, string speakerLabel, string content, Color color, bool waitForPlaybackFinished, float executeAtMissionTime = -1f)
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

	internal bool TryDequeuePendingSceneDialogueFeed(int agentIndex, out PendingSceneDialogueFeedEntry entry)
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

	internal void FlushPendingSceneDialogueFeedAfterSpeech(int agentIndex)
	{
		if (!TryDequeuePendingSceneDialogueFeed(agentIndex, out var entry) || entry == null)
		{
			return;
		}
		RecordSceneDialogueToMessageFeed(entry.SpeakerLabel, entry.Content, entry.Color);
	}

	internal void ConvertPendingSceneDialogueFeedToTimedFlush(int agentIndex, float delaySeconds)
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

	internal void EnqueuePendingSpeechCompletionToken(int agentIndex, long interactionToken)
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

	internal bool TryDequeuePendingSpeechCompletionToken(int agentIndex, out long interactionToken)
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

	internal void ClearPendingTtsBubbleSyncQueues()
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
		_ports.ClearInteractionTimeoutArms();
	}

	internal void RecordSceneDialogueToMessageFeed(string speakerLabel, string content, Color color)
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

	internal void RecordPlayerSpeechToMessageFeed(string content)
	{
		RecordSceneDialogueToMessageFeed("你", content, new Color(0.3f, 1f, 0.3f));
	}

	internal void RecordNpcSpeechToMessageFeed(string npcDisplayName, string content)
	{
		RecordSceneDialogueToMessageFeed(npcDisplayName, content, new Color(1f, 0.8f, 0.2f));
	}

	internal void QueueSceneInfoMessage(string message, Color color, int requiredConversationEpoch = 0, string soundEventPath = "")
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

	internal void ScheduleNpcSpeechToMessageFeed(int agentIndex, string npcDisplayName, string content, SceneSpeechPlaybackInfo playbackInfo)
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

	internal void UpdatePendingSceneDialogueFeeds()
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

	internal static float EstimateBubbleTypingDurationSeconds(string content)
	{
		int num = Math.Max(0, (content ?? "").Length);
		if (num <= 0)
		{
			return 0f;
		}
		return Math.Max(1f, (float)num * 0.05f);
	}

 internal SceneAudioFailureOutput ResolveSceneAudioFailureOutput(int index)
 {
  bool hasToken = TryDequeuePendingSpeechCompletionToken(index, out long token);
  PendingNpcBubbleEntry bubble = null; float duration = -1f;
  bool hasBubble = index >= 0 && TryDequeuePendingNpcBubble(index, out bubble, out duration);
  return new SceneAudioFailureOutput { HasInteractionToken = hasToken, InteractionToken = token, HasBubble = hasBubble,
   Agent = bubble?.Agent, UiContent = bubble?.UiContent, TypingDuration = duration };
 }

 internal void ClearSceneAudioPendingForAgent(int index)
 {
  lock (_ttsBubbleSyncLock) { _pendingNpcBubbleQueues.Remove(index); _pendingAudioDurationQueues.Remove(index); _pendingSpeechCompletionTokenQueues.Remove(index); _pendingSceneDialogueFeedQueues.Remove(index); }
 }

    private ConcurrentQueue<Action> _mainThreadActions { get => _ports.Get_mainThreadActions(); set => _ports.Set_mainThreadActions(value); }
    private bool TryShowNpcBubble(Agent liveAgent, string content, float typingDurationSeconds = -1f) => _ports.TryShowNpcBubble_L2127(liveAgent, content, typingDurationSeconds);
    private void ResetSceneAudioRequestOwnership() => _ports.ResetSceneAudioRequestOwnership_L67();
    private bool IsTtsPlaybackRequestCurrent(TtsEngine.PlaybackRequest request, bool allowCancelled = false) => _ports.IsTtsPlaybackRequestCurrent_L73(request, allowCancelled);
    private void LogTtsReport(string stage, int agentIndex, string extra = null) => _ports.LogTtsReport_L208(stage, agentIndex, extra);
    private SceneMovementController _sceneMovement { get => _ports.Get_sceneMovement(); }
    private bool IsSceneConversationEpochCurrent(int epoch) => _ports.IsSceneConversationEpochCurrent_L223(epoch);
    internal object OutputSyncRoot => _ttsBubbleSyncLock;
    internal void UnmarkPlaybackStarted(int index) { lock (_ttsBubbleSyncLock) _ttsPlaybackStartedAgents.Remove(index); }
    internal void MarkPlaybackStarted(int index) { lock (_ttsBubbleSyncLock) _ttsPlaybackStartedAgents.Add(index); }
    internal bool HasPlaybackStarted(int index) { lock (_ttsBubbleSyncLock) return _ttsPlaybackStartedAgents.Contains(index); }
    internal void ClearPendingSpeechCompletionTokens(int index) { lock (_ttsBubbleSyncLock) _pendingSpeechCompletionTokenQueues.Remove(index); }
    internal NativeSpeechOutputDiagnosticSnapshot CaptureQueueDiagnostic(int index)
    {
        lock (_ttsBubbleSyncLock) return new NativeSpeechOutputDiagnosticSnapshot {
            PendingBubbleCount = _pendingNpcBubbleQueues.TryGetValue(index, out var bubbles) && bubbles != null ? bubbles.Count : 0,
            PendingDurationCount = _pendingAudioDurationQueues.TryGetValue(index, out var durations) && durations != null ? durations.Count : 0,
            PendingSpeechTokenCount = _pendingSpeechCompletionTokenQueues.TryGetValue(index, out var tokens) && tokens != null ? tokens.Count : 0 };
    }

}

internal sealed class SceneSpeechOutputQueueControllerPorts
{
    internal Func<ConcurrentQueue<Action>> Get_mainThreadActions;
    internal Action<ConcurrentQueue<Action>> Set_mainThreadActions;
    internal delegate bool TryShowNpcBubble_L2127Callback(Agent liveAgent, string content, float typingDurationSeconds);
    internal TryShowNpcBubble_L2127Callback TryShowNpcBubble_L2127;
    internal delegate void ResetSceneAudioRequestOwnership_L67Callback();
    internal ResetSceneAudioRequestOwnership_L67Callback ResetSceneAudioRequestOwnership_L67;
    internal delegate bool IsTtsPlaybackRequestCurrent_L73Callback(TtsEngine.PlaybackRequest request, bool allowCancelled);
    internal IsTtsPlaybackRequestCurrent_L73Callback IsTtsPlaybackRequestCurrent_L73;
    internal delegate void LogTtsReport_L208Callback(string stage, int agentIndex, string extra);
    internal LogTtsReport_L208Callback LogTtsReport_L208;
    internal Func<SceneMovementController> Get_sceneMovement;
    internal delegate bool IsSceneConversationEpochCurrent_L223Callback(int epoch);
    internal IsSceneConversationEpochCurrent_L223Callback IsSceneConversationEpochCurrent_L223;
    internal Action ClearInteractionTimeoutArms;
}
