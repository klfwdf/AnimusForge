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
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace Voxforge;

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
			if (Input.IsKeyPressed(key))
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

	private readonly Queue<(NpcDataPacket npc, string content)> _speechQueue = new Queue<(NpcDataPacket, string)>();

	private bool _speechWorkerRunning = false;

	private bool _lastShoutDuelLiteralHit = false;

	private List<ShoutTradeResourceOption> _shoutTradeOptions = new List<ShoutTradeResourceOption>();

	private List<ShoutPendingTradeItem> _shoutPendingTradeItems = new List<ShoutPendingTradeItem>();

	private int _shoutPendingTradeItemIndex = 0;

	private bool _shoutPendingTradeIsGive = false;

	private NpcDataPacket _shoutTradeTargetNpc = null;

	private List<Agent> _staringAgents = new List<Agent>();

	private float _stopStaringTime = 0f;

	private Dictionary<int, List<ConversationMessage>> _npcConversationHistory = new Dictionary<int, List<ConversationMessage>>();

	private List<ConversationMessage> _publicConversationHistory = new List<ConversationMessage>();

	private const int MAX_HISTORY_TURNS = 20;

	private Agent _currentStareTarget = null;

	private float _stareTimer = 0f;

	private float _interactionGraceTimer = 0f;

	private Dictionary<int, float> _passiveCooldowns = new Dictionary<int, float>();

	private const float STARE_TRIGGER_TIME = 5f;

	private const float PASSIVE_COOLDOWN = 60f;

	private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

	private float _tickTimer = 0f;

	private readonly HashSet<int> _speakingAgentIndices = new HashSet<int>();

	private readonly object _speakingLock = new object();

	private readonly Dictionary<int, SoundEvent> _agentSoundEvents = new Dictionary<int, SoundEvent>();

	private readonly Dictionary<int, string> _agentWavPaths = new Dictionary<int, string>();

	private readonly Dictionary<int, string> _agentXmlPaths = new Dictionary<int, string>();

	private Action<int, string, string, float> _ttsOnAudioFileReadyHandler;

	private Action<int> _ttsOnPlaybackStartedHandler;

	private Action<int> _ttsOnPlaybackFinishedHandler;

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

	private bool TryShowNpcBubble(Agent liveAgent, string content, float typingDurationSeconds = -1f)
	{
		try
		{
			if (liveAgent == null || !liveAgent.IsActive())
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

	private void ClearPendingTtsBubbleSyncQueues()
	{
		lock (_ttsBubbleSyncLock)
		{
			_pendingNpcBubbleQueues.Clear();
			_pendingAudioDurationQueues.Clear();
		}
	}

	private void ShowNpcSpeechOutput(NpcDataPacket npc, Agent liveAgent, string content)
	{
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
		bool flag = false;
		bool flag2 = IsTtsPlaybackEnabledForShout();
		if (flag2)
		{
			try
			{
				string text3 = "";
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
				flag = TtsEngine.Instance.SpeakAsync(content, -1, -1f, num, text3);
			}
			catch
			{
			}
		}
		if (flag && num >= 0 && liveAgent != null && liveAgent.IsActive())
		{
			EnqueuePendingNpcBubble(num, liveAgent, text, npc?.Name ?? "NPC");
			return;
		}
		if (!TryShowNpcBubble(liveAgent, text))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + (npc?.Name ?? "NPC"));
			InformationManager.DisplayMessage(new InformationMessage("[" + (npc?.Name ?? "NPC") + "] " + text, new Color(1f, 0.8f, 0.2f)));
		}
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
			TtsEngine.Instance.SpeakAsync(content, -1, -1f, num, text4);
		}
		catch
		{
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

	private static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
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

	private static bool TryRenderSceneHistoryLine(ConversationMessage msg, HashSet<string> allowedSpeakers, out string rendered)
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
			rendered = text2;
			return true;
		}
		case "user":
			if (text2.StartsWith("玩家:", StringComparison.Ordinal) || text2.StartsWith("玩家：", StringComparison.Ordinal))
			{
				rendered = text2;
			}
			else
			{
				rendered = "玩家: " + text2;
			}
			return true;
		case "system":
			rendered = "[系统事实] " + text2;
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
		if (text.StartsWith("Gold:", StringComparison.OrdinalIgnoreCase))
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
					try
					{
						if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
						{
							MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref aiResponse);
							DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref aiResponse);
							DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref aiResponse);
							if (RewardSystemBehavior.Instance != null)
							{
								RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref aiResponse);
							}
						}
						else
						{
							MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(speakerData.UnnamedKey, speakerData.Name, ref aiResponse);
						}
					}
					catch
					{
					}
					bool flag = false;
					try
					{
						flag = ShoutUtils.TryTriggerDuelAction(speakerData, ref aiResponse);
					}
					catch
					{
					}
					ShowNpcSpeechOutput(speakerData, agent, aiResponse);
					if (agent != null && agent.IsActive())
					{
						AddAgentToStareList(agent);
					}
					RecordResponseForAllNearbySafe(allNpcData, speakerData.Name, aiResponse);
					PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);
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
		_currentStareTarget = null;
		_stareTimer = 0f;
		_interactionGraceTimer = 0f;
		_passiveCooldowns.Clear();
		lock (_speechQueueLock)
		{
			_speechQueue.Clear();
			_speechWorkerRunning = false;
		}
		Action result;
		while (_mainThreadActions.TryDequeue(out result))
		{
		}
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
						lock (_speakingLock)
						{
							_speakingAgentIndices.Add(agentIndex);
						}
						Logger.Log("LipSync", $"[OnPlaybackStarted] agentIndex={agentIndex} added to speaking set");
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
						lock (_speakingLock)
						{
							_speakingAgentIndices.Remove(agentIndex);
						}
						Logger.Log("LipSync", $"[OnPlaybackFinished] agentIndex={agentIndex} removed from speaking set");
						_mainThreadActions.Enqueue(delegate
						{
							try
							{
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
							}
							catch
							{
							}
						});
					}
				};
				instance.OnAudioFileReady += _ttsOnAudioFileReadyHandler;
				instance.OnPlaybackStarted += _ttsOnPlaybackStartedHandler;
				instance.OnPlaybackFinished += _ttsOnPlaybackFinishedHandler;
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
		lock (_speakingLock)
		{
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
				lock (_speakingLock)
				{
					_speakingAgentIndices.Remove(agentIndex);
				}
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
				QueueDeferredCleanup(value, value2, value3, "AgentInvalid", agentIndex);
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
			_currentStareTarget = null;
			return;
		}
		Agent closestFacingAgent = ShoutUtils.GetClosestFacingAgent(5f);
		if (closestFacingAgent != null)
		{
			if (closestFacingAgent == _currentStareTarget)
			{
				_stareTimer += dt;
				if (_stareTimer >= 5f && IsCooldownReady(closestFacingAgent))
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
		else
		{
			_currentStareTarget = null;
			_stareTimer = 0f;
		}
		UpdateCooldowns(dt);
	}

	private void UpdateCooldowns(float dt)
	{
		List<int> list = new List<int>(_passiveCooldowns.Keys);
		foreach (int item in list)
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

	private bool IsCooldownReady(Agent agent)
	{
		if (agent == null)
		{
			return false;
		}
		return !_passiveCooldowns.ContainsKey(agent.Index);
	}

	private void TriggerPassiveReaction(Agent targetAgent)
	{
		if (targetAgent == null || _isProcessingShout)
		{
			return;
		}
		_isProcessingShout = true;
		_interactionGraceTimer = 10f;
		_passiveCooldowns[targetAgent.Index] = 60f;
		NpcDataPacket npcData = ShoutUtils.ExtractNpcData(targetAgent);
		if (npcData == null)
		{
			_isProcessingShout = false;
			return;
		}
		string sceneDesc = ShoutUtils.GetCurrentSceneDescription();
		List<Agent> source = ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
		List<NpcDataPacket> allNpcData = (from a in source
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		if (!allNpcData.Any((NpcDataPacket d) => d != null && d.AgentIndex == npcData.AgentIndex))
		{
			allNpcData.Add(npcData);
		}
		InformationManager.DisplayMessage(new InformationMessage("你盯着 " + npcData.Name + " 看了很久...", new Color(0.7f, 0.7f, 0.7f)));
		_ = Task.Run(async delegate
		{
			try
			{
				string virtualInput = "(沉默地长时间注视)";
				RecordPlayerMessage(virtualInput, allNpcData);
				string response = await GetPassiveNpcResponse(npcData, sceneDesc, virtualInput);
				if (!string.IsNullOrWhiteSpace(response))
				{
					List<(NpcDataPacket, string)> singleResponseList = new List<(NpcDataPacket, string)> { (npcData, response) };
					await PlaybackSpeechSequence(singleResponseList, allNpcData);
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Logger.Log("ShoutBehavior", "[ERROR] TriggerPassiveReaction async failed: " + ex2.Message);
			}
			finally
			{
				_isProcessingShout = false;
			}
		});
	}

	private async Task<string> GetPassiveNpcResponse(NpcDataPacket data, string sceneDesc, string inputActionText)
	{
		try
		{
			if (data == null)
			{
				return "（没说话）";
			}
			Hero hero = null;
			try
			{
				if (data.IsHero)
				{
					hero = ResolveHeroFromAgentIndex(data.AgentIndex);
				}
			}
			catch
			{
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
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(hero, inputActionText, null, data.CultureId);
				DuelSettings settings = DuelSettings.GetSettings();
				int maxTokens = settings.ShoutMaxTokens;
				int minTokens = maxTokens / 2;
				if (minTokens < 5)
				{
					minTokens = 5;
				}
				StringBuilder sysPrompt = new StringBuilder();
				bool factsHeaderAdded = false;
				Action ensureFactsHeader = delegate
				{
					if (!factsHeaderAdded)
					{
						sysPrompt.AppendLine("【3.当前事实】");
						factsHeaderAdded = true;
					}
				};
				string npcName = (string.IsNullOrWhiteSpace(data.Name) ? "未命名NPC" : data.Name);
				sysPrompt.AppendLine("【2.动作约束】");
				sysPrompt.AppendLine("你是当前被动回应者：" + npcName + "。");
				sysPrompt.AppendLine("你只能以该角色身份回答，禁止替其他角色发言。");
				if (!string.IsNullOrWhiteSpace(data.RoleDesc))
				{
					sysPrompt.AppendLine("身份：" + data.RoleDesc);
				}
				if (!string.IsNullOrWhiteSpace(data.PersonalityDesc))
				{
					sysPrompt.AppendLine("个性：" + data.PersonalityDesc);
				}
				if (!string.IsNullOrWhiteSpace(data.BackgroundDesc))
				{
					sysPrompt.AppendLine("背景：" + data.BackgroundDesc);
				}
				ensureFactsHeader();
				if (!string.IsNullOrWhiteSpace(inputActionText))
				{
					sysPrompt.AppendLine("[玩家动作] " + inputActionText);
				}
				try
				{
					string patienceLine;
					bool canSpeak;
					bool hasPatience = ((hero == null) ? MyBehavior.TryGetSceneUnnamedPatienceStatusForExternal(data.UnnamedKey, data.Name, out patienceLine, out canSpeak) : MyBehavior.TryGetSceneHeroPatienceStatusForExternal(hero, out patienceLine, out canSpeak));
					if (hasPatience && !string.IsNullOrWhiteSpace(patienceLine))
					{
						sysPrompt.AppendLine("【4.三值状态】");
						sysPrompt.AppendLine(patienceLine);
						sysPrompt.AppendLine(MyBehavior.GetScenePatienceInstructionForExternal());
					}
				}
				catch
				{
				}
				if (!string.IsNullOrWhiteSpace(sceneDesc))
				{
					ensureFactsHeader();
					sysPrompt.AppendLine("*场景：" + sceneDesc + "*");
				}
				try
				{
					if (!LordEncounterBehavior.IsEncounterMeetingMissionActive)
					{
						string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt() ?? "").Replace("\r", "").Trim();
						if (!string.IsNullOrWhiteSpace(nativeInfo))
						{
							if (nativeInfo.Length > 900)
							{
								nativeInfo = nativeInfo.Substring(0, 900) + "…";
							}
							ensureFactsHeader();
							sysPrompt.AppendLine("【当前定居点（原版到达介绍）】：");
							sysPrompt.AppendLine(nativeInfo);
						}
					}
				}
				catch
				{
				}
				sysPrompt.AppendLine($"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非玩家明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。)");
				Stopwatch swPrompt = Stopwatch.StartNew();
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string localExtras = sysPrompt.ToString().Trim();
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtras) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtras + "\n" + localExtras) : baseExtras));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				string taskPreamble = "你是【在场人物列表】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复玩家的对话。";
				layeredPrompt = taskPreamble + "\n" + layeredPrompt;
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
					if (_npcConversationHistory.ContainsKey(data.AgentIndex))
					{
						List<ConversationMessage> history = _npcConversationHistory[data.AgentIndex];
						int start = Math.Max(0, history.Count - 20);
						if (start < history.Count)
						{
							historyDump = new StringBuilder();
							historyDump.AppendLine($">>> History(agentIndex={data.AgentIndex}) count={history.Count - start}");
							historyLines = new List<string>();
						}
						for (int i = start; i < history.Count; i++)
						{
							if (TryRenderSceneHistoryLine(history[i], null, out var line))
							{
								historyDump?.AppendLine(line);
								historyLines?.Add(line);
								line = null;
							}
						}
					}
				}
				if (historyDump != null)
				{
					Logger.Log("ShoutBehavior(被动)", historyDump.ToString());
				}
				string persistedHeroHistory = BuildPersistedHeroHistoryContext(data.AgentIndex, inputActionText);
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(被动)", $"[HistoryBridge] heroAgentIndex={data.AgentIndex} chars={persistedHeroHistory.Length}");
				}
				layeredPrompt = ((historyLines == null || historyLines.Count <= 0) ? (layeredPrompt + "\n【场景近期对话】\n无") : (layeredPrompt + "\n【场景近期对话】\n" + string.Join("\n", historyLines)));
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					layeredPrompt = layeredPrompt + "\n" + persistedHeroHistory;
				}
				messages[0] = new
				{
					role = "system",
					content = layeredPrompt
				};
				string passiveUserMessage = "[玩家动作] " + inputActionText + "\n现在请你以" + npcName + "的身份回复。直接输出对话内容，禁止生成任何【】章节标题或格式说明。";
				messages.Add(new
				{
					role = "user",
					content = passiveUserMessage
				});
				Stopwatch swApi = Stopwatch.StartNew();
				string output = await ShoutNetwork.CallApiWithMessages(messages, 2048);
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
				return output;
			}
		}
		catch
		{
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
		_interactionGraceTimer = 30f;
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
		int num = Hero.MainHero?.Gold ?? 0;
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
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemObject itemAtIndex = itemRoster.GetItemAtIndex(i);
				if (itemAtIndex != null)
				{
					int itemNumber = itemRoster.GetItemNumber(itemAtIndex);
					if (itemNumber > 0)
					{
						list.Add(new ShoutTradeResourceOption
						{
							IsGold = false,
							ItemId = itemAtIndex.StringId,
							Name = itemAtIndex.Name.ToString(),
							AvailableAmount = itemNumber,
							Item = itemAtIndex
						});
					}
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
		try
		{
			if (shoutTradeTargetNpc != null && shoutTradeTargetNpc.IsHero)
			{
				hero = ResolveHeroFromAgentIndex(shoutTradeTargetNpc.AgentIndex);
			}
		}
		catch
		{
			hero = null;
		}
		if (hero == null)
		{
			return;
		}
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
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, num);
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, num, null, 0);
					}
					catch
					{
					}
				}
			}
			else
			{
				if (mobileParty == null || shoutPendingTradeItem.Item == null)
				{
					continue;
				}
				ItemRoster itemRoster = mobileParty.ItemRoster;
				if (itemRoster == null)
				{
					continue;
				}
				int itemNumber = itemRoster.GetItemNumber(shoutPendingTradeItem.Item);
				int num2 = Math.Min(shoutPendingTradeItem.Amount, itemNumber);
				if (num2 > 0)
				{
					itemRoster.AddToCounts(shoutPendingTradeItem.Item, -num2);
					if (hero.PartyBelongedTo != null)
					{
						hero.PartyBelongedTo.ItemRoster.AddToCounts(shoutPendingTradeItem.Item, num2);
					}
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(hero, 0, shoutPendingTradeItem.ItemId, num2);
					}
					catch
					{
					}
				}
			}
		}
	}

	private string BuildShoutTradeFactText(bool isGive)
	{
		if (_shoutPendingTradeItems == null || _shoutPendingTradeItems.Count == 0)
		{
			return "";
		}
		string text = Hero.MainHero?.Name?.ToString() ?? "玩家";
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
					list.Add($"{shoutPendingTradeItem.Amount} 个 {shoutPendingTradeItem.ItemName}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		if (isGive)
		{
			return text + "已经将 " + string.Join("、", list) + " 交给你。";
		}
		return text + "给你看了看 " + string.Join("、", list) + "，但是没有将这些东西交给你。";
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
		_interactionGraceTimer = 30f;
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
		ResetStaringBehavior();
		_stopStaringTime = Mission.Current.CurrentTime + 20f;
		foreach (Agent agent2 in nearbyAgents)
		{
			AddAgentToStareList(agent2);
		}
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
		RecordPlayerMessage(shoutText, capturedNpcData);
		_ = Task.Run(async delegate
		{
			await HandleGroupResponse(shoutText, capturedNpcData, sceneDesc, primaryDataPacket, extraFact);
		});
		ResumeGame();
	}

	private async Task HandleGroupResponse(string playerText, List<NpcDataPacket> allNpcData, string sceneDesc, NpcDataPacket primaryNpc, string extraFact)
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
							Hero hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
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
				await EnsurePersonaForCandidatesAsync(speakingCandidates);
				StringBuilder sysPrompt = new StringBuilder();
				Hero contextHero = null;
				try
				{
					if (primaryNpc != null && primaryNpc.IsHero)
					{
						contextHero = ResolveHeroFromAgentIndex(primaryNpc.AgentIndex);
					}
					if (contextHero == null)
					{
						NpcDataPacket heroNpc = speakingCandidates.FirstOrDefault((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
						if (heroNpc != null)
						{
							contextHero = ResolveHeroFromAgentIndex(heroNpc.AgentIndex);
						}
					}
				}
				catch
				{
				}
				string cultureId = ((primaryNpc != null) ? primaryNpc.CultureId : "neutral");
				bool hasAnyHero = speakingCandidates.Any((NpcDataPacket npcDataPacket) => npcDataPacket?.IsHero ?? false);
				MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(contextHero, playerText, extraFact, cultureId, hasAnyHero);
				bool factsHeaderAdded = false;
				Action ensureFactsHeader = delegate
				{
					if (!factsHeaderAdded)
					{
						sysPrompt.AppendLine("【3.当前事实】");
						factsHeaderAdded = true;
					}
				};
				if (!string.IsNullOrWhiteSpace(sceneDesc))
				{
					ensureFactsHeader();
					sysPrompt.AppendLine("*场景：" + sceneDesc + "*");
				}
				try
				{
					if (!LordEncounterBehavior.IsEncounterMeetingMissionActive)
					{
						string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt() ?? "").Replace("\r", "").Trim();
						if (!string.IsNullOrWhiteSpace(nativeInfo))
						{
							if (nativeInfo.Length > 900)
							{
								nativeInfo = nativeInfo.Substring(0, 900) + "…";
							}
							ensureFactsHeader();
							sysPrompt.AppendLine(" ");
							sysPrompt.AppendLine("【当前定居点（原版到达介绍）】：");
							sysPrompt.AppendLine(nativeInfo);
						}
					}
				}
				catch
				{
				}
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
							Hero hero2 = ResolveHeroFromAgentIndex(npc2.AgentIndex);
							if (hero2?.IsPrisoner ?? false)
							{
								string captor = hero2.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
								line += ((!string.IsNullOrEmpty(captor)) ? (" | 状态: 囚犯（被" + captor + "关押）") : " | 状态: 囚犯");
							}
						}
						catch
						{
						}
					}
					if (!string.IsNullOrWhiteSpace(npc2.PersonalityDesc))
					{
						line = line + " | 个性: " + npc2.PersonalityDesc;
					}
					if (!string.IsNullOrWhiteSpace(npc2.BackgroundDesc))
					{
						line = line + " | 背景: " + npc2.BackgroundDesc;
					}
					sysPrompt.AppendLine(line);
				}
				if (patienceStatusLines.Count > 0)
				{
					sysPrompt.AppendLine(" ");
					sysPrompt.AppendLine("【4.三值状态】");
					sysPrompt.AppendLine("【NPC耐心状态】：");
					foreach (string line2 in patienceStatusLines)
					{
						sysPrompt.AppendLine(line2);
					}
					sysPrompt.AppendLine(MyBehavior.GetScenePatienceInstructionForExternal());
				}
				int maxTokens = settings.ShoutMaxTokens;
				int minTokens = maxTokens / 2;
				if (minTokens < 5)
				{
					minTokens = 5;
				}
				sysPrompt.AppendLine("【群体对话规则】");
				sysPrompt.AppendLine("1. 如果【在场人物列表】中只有一个NPC，那他必须回应玩家。");
				sysPrompt.AppendLine("2. 【在场人物列表】中最上方的NPC是主要对话对象，必须回复玩家，其他NPC可以酌情选择是否回复");
				sysPrompt.AppendLine("3. 格式必须为：[角色名]: [纯对话内容]（每名说话者单独一行，禁止同一行出现两个角色）。");
				sysPrompt.AppendLine("4. 禁止动作描写、心理活动、括号备注；只保留角色说出口的话，不要加引号。");
				sysPrompt.AppendLine("5. [角色名] 必须来自【在场人物列表】，即便有同名角色，也禁止自创姓名或错名，比如某某甲，某某乙，某某1，某某2");
				sysPrompt.AppendLine("6. 若多人在场，回复之间应彼此连贯。");
				sysPrompt.AppendLine($"(回复长度要求：请将本轮回复控制在 {minTokens}-{maxTokens} 字之间；除非玩家明确要求简短，否则尽量贴近上限，不要少于 {minTokens} 字。)");
				Stopwatch swPrompt = Stopwatch.StartNew();
				string fixedLayerText = "";
				string baseExtras = StripScenePersonaBlocks((ctx?.Extras ?? "").Trim());
				string localExtras = sysPrompt.ToString().Trim();
				string deltaLayerText = (string.IsNullOrWhiteSpace(baseExtras) ? localExtras : ((!string.IsNullOrWhiteSpace(localExtras)) ? (baseExtras + "\n" + localExtras) : baseExtras));
				string layeredPrompt = (string.IsNullOrWhiteSpace(fixedLayerText) ? deltaLayerText : ((!string.IsNullOrWhiteSpace(deltaLayerText)) ? (fixedLayerText + "\n" + deltaLayerText) : fixedLayerText));
				string taskPreamble = "你是【在场人物列表】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复玩家的对话。";
				layeredPrompt = taskPreamble + "\n" + layeredPrompt;
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
						if (_npcConversationHistory.ContainsKey(historySourceIndex))
						{
							List<ConversationMessage> history = _npcConversationHistory[historySourceIndex];
							HashSet<string> allowedSpeakers = new HashSet<string>(from npcDataPacket in speakingCandidates
								where npcDataPacket != null
								select (npcDataPacket.Name ?? "").Trim() into value
								where !string.IsNullOrWhiteSpace(value)
								select value);
							int start = Math.Max(0, history.Count - 20);
							if (start < history.Count)
							{
								historyDump = new StringBuilder();
								historyDump.AppendLine($">>> History(sourceAgentIndex={historySourceIndex}) count={history.Count - start}");
								historyLines = new List<string>();
							}
							for (int i = start; i < history.Count; i++)
							{
								ConversationMessage msg = history[i];
								if (TryRenderSceneHistoryLine(msg, allowedSpeakers, out var line3))
								{
									historyDump?.AppendLine(line3);
									historyLines?.Add(line3);
									line3 = null;
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
				string persistedHeroHistory = ((persistedHistorySourceIndex == -1) ? "" : BuildPersistedHeroHistoryContext(persistedHistorySourceIndex, playerText));
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					Logger.Log("ShoutBehavior(主动)", $"[HistoryBridge] heroAgentIndex={persistedHistorySourceIndex} chars={persistedHeroHistory.Length}");
				}
				layeredPrompt = ((historyLines == null || historyLines.Count <= 0) ? (layeredPrompt + "\n【场景近期对话】\n无") : (layeredPrompt + "\n【场景近期对话】\n" + string.Join("\n", historyLines)));
				if (!string.IsNullOrWhiteSpace(persistedHeroHistory))
				{
					layeredPrompt = layeredPrompt + "\n" + persistedHeroHistory;
				}
				messages[0] = new
				{
					role = "system",
					content = layeredPrompt
				};
				string userMessage = "玩家说：\"" + playerText + "\"\n现在请你直接以【在场人物列表】中角色的身份回复玩家。直接输出对话内容，格式为'角色名: 对话内容'，每人一行。\n禁止生成任何新的【】章节标题、编号规则列表、格式说明或回复要求。禁止替玩家说话或编造玩家的台词。\n立即开始输出角色对话：";
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
				await ShoutNetwork.CallApiWithMessagesStream(messages, 2048, delegate(string delta)
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
				RecordResponseForAllNearbySafe(capturedAllNpcData, fallbackNpc.Name, c);
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

	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData)
	{
		if (npc == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		lock (_speechQueueLock)
		{
			_speechQueue.Enqueue((npc, content));
			if (_speechWorkerRunning)
			{
				return;
			}
			_speechWorkerRunning = true;
		}
		_ = Task.Run(async delegate
		{
			await RunSpeechQueueWorker(allNpcData);
		});
	}

	private async Task RunSpeechQueueWorker(List<NpcDataPacket> allNpcData)
	{
		try
		{
			while (true)
			{
				(NpcDataPacket npc, string content) item;
				lock (_speechQueueLock)
				{
					if (_speechQueue.Count == 0)
					{
						_speechWorkerRunning = false;
						break;
					}
					item = _speechQueue.Dequeue();
				}
				var (matchedNpc, content) = item;
				_mainThreadActions.Enqueue(delegate
				{
					try
					{
						Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == matchedNpc.AgentIndex);
						try
						{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref content);
								DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref content);
								DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref content);
								if (RewardSystemBehavior.Instance != null)
								{
									RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref content);
								}
							}
							else
							{
								MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(matchedNpc.UnnamedKey, matchedNpc.Name, ref content);
							}
						}
						catch
						{
						}
						content = StripLeakedPromptContentForShout(content);
						content = StripStageDirectionsForPassiveShout(content);
						if (!string.IsNullOrWhiteSpace(content))
						{
							bool flag = ShoutUtils.TryTriggerDuelAction(matchedNpc, ref content);
							ShowNpcSpeechOutput(matchedNpc, agent, content);
							if (agent != null && agent.IsActive())
							{
								AddAgentToStareList(agent);
							}
							RecordResponseForAllNearbySafe(allNpcData, matchedNpc.Name, content);
							PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, content, allNpcData);
							if (flag)
							{
								DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
								ShoutUtils.ExecuteDuel(agent);
							}
						}
					}
					catch (Exception ex)
					{
						Logger.Log("ShoutBehavior", "[ERROR] RunSpeechQueueWorker mainThread: " + ex.Message);
					}
				});
				await Task.Delay(2000);
				item = default((NpcDataPacket, string));
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

	private async Task PlaybackSpeechSequence(List<(NpcDataPacket npc, string content)> responses, List<NpcDataPacket> allNpcData)
	{
		foreach (var item in responses)
		{
			var (matchedNpc, content) = item;
			try
			{
				_mainThreadActions.Enqueue(delegate
				{
					try
					{
						Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == matchedNpc.AgentIndex);
						try
						{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref content);
								DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref content);
								DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref content);
								if (RewardSystemBehavior.Instance != null)
								{
									RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref content);
								}
							}
							else
							{
								MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(matchedNpc.UnnamedKey, matchedNpc.Name, ref content);
							}
						}
						catch
						{
						}
						content = StripLeakedPromptContentForShout(content);
						if (!string.IsNullOrWhiteSpace(content))
						{
							bool flag = ShoutUtils.TryTriggerDuelAction(matchedNpc, ref content);
							ShowNpcSpeechOutput(matchedNpc, agent, content);
							if (agent != null && agent.IsActive())
							{
								AddAgentToStareList(agent);
							}
							RecordResponseForAllNearbySafe(allNpcData, matchedNpc.Name, content);
							PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, content, allNpcData);
							if (flag)
							{
								DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
								ShoutUtils.ExecuteDuel(agent);
							}
						}
					}
					catch (Exception ex)
					{
						Logger.Log("ShoutBehavior", "[ERROR] PlaybackSpeechSequence mainThread: " + ex.Message);
					}
				});
				await Task.Delay(2000);
			}
			catch
			{
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
					Role = "system",
					Content = text,
					SpeakerName = "系统"
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

	private void RecordLoreToNpcHistory(int agentIndex, string lore)
	{
		try
		{
			lore = (lore ?? "").Trim();
			if (string.IsNullOrWhiteSpace(lore))
			{
				return;
			}
			lock (_historyLock)
			{
				if (!_npcConversationHistory.ContainsKey(agentIndex))
				{
					_npcConversationHistory[agentIndex] = new List<ConversationMessage>();
				}
				_npcConversationHistory[agentIndex].Add(new ConversationMessage
				{
					Role = "system",
					Content = lore,
					SpeakerName = "系统"
				});
				while (_npcConversationHistory[agentIndex].Count > 40)
				{
					_npcConversationHistory[agentIndex].RemoveAt(0);
				}
			}
			try
			{
				string text = lore.Replace("\r", "").Replace("\n", "\\n");
				if (text.Length > 200)
				{
					text = text.Substring(0, 200);
				}
				Logger.Log("LoreHistory", $"shout_history agentIndex={agentIndex} preview={text}");
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void RecordPlayerMessage(string text, List<NpcDataPacket> nearbyData)
	{
		lock (_historyLock)
		{
			_publicConversationHistory.Add(new ConversationMessage
			{
				Role = "user",
				Content = text,
				SpeakerName = "你"
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
					SpeakerName = "你"
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

	private void RecordResponseForAllNearbySafe(List<NpcDataPacket> nearbyData, string speakerName, string response)
	{
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
					SpeakerName = speakerName
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
				SpeakerName = speakerName
			});
		}
	}

	private async Task EnsurePersonaForCandidatesAsync(List<NpcDataPacket> candidates)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return;
		}
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
					Hero hero = ResolveHeroFromAgentIndex(npc.AgentIndex);
					if (hero == null)
					{
						continue;
					}
					MyBehavior.GetNpcPersonaForExternal(hero, out var p, out var b);
					if (string.IsNullOrWhiteSpace(p) && string.IsNullOrWhiteSpace(b))
					{
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
				bool loaded = false;
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
						loaded = true;
					}
					up = null;
					ub = null;
				}
				if (loaded)
				{
					continue;
				}
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
				if (agent == null)
				{
					continue;
				}
				if (ShoutUtils.TryGetUnnamedNpcPersona(agent, out var up2, out var ub2))
				{
					if (!string.IsNullOrWhiteSpace(up2))
					{
						npc.PersonalityDesc = up2.Trim();
					}
					if (!string.IsNullOrWhiteSpace(ub2))
					{
						npc.BackgroundDesc = ub2.Trim();
					}
				}
				up2 = null;
				ub2 = null;
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

	private string BuildPersistedHeroHistoryContext(int agentIndex, string currentInput)
	{
		try
		{
			Hero hero = ResolveHeroFromAgentIndex(agentIndex);
			if (hero == null)
			{
				return "";
			}
			string text = MyBehavior.BuildHistoryContextForExternal(hero, 0, currentInput);
			return (text ?? "").Trim();
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
				Hero hero = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
				if (hero != null)
				{
					MyBehavior.AppendExternalDialogueHistory(hero, text, null, null);
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
			Hero hero = ResolveHeroFromAgentIndex(nearbyDatum.AgentIndex);
			if (hero != null)
			{
				if (nearbyDatum.AgentIndex == speakerAgentIndex)
				{
					MyBehavior.AppendExternalDialogueHistory(hero, null, response, null);
					continue;
				}
				string aiText = "[场景喊话] " + speakerName + ": " + response;
				MyBehavior.AppendExternalDialogueHistory(hero, null, aiText, null);
			}
		}
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
	}

	private void AddAgentToStareList(Agent agent)
	{
		if (agent != null)
		{
			try
			{
				agent.SetLookAgent(Agent.Main);
				agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
				WorldPosition position = agent.GetWorldPosition();
				agent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
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

