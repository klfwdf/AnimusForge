using SceneSpeechPlaybackInfo = AnimusForge.ShoutBehavior.SceneSpeechPlaybackInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Owns only the persistent scene panel: participants, lifetime, round readiness and UI trade staging.
// Game references are read on the mission thread; held participants are checked at 10 Hz (no agent scan).
internal sealed class ScenePresentationController
{
 private readonly Func<Agent, bool> _canParticipate;
 private readonly Func<long> _sequence;
 private readonly Func<Mission, string> _combatReason;
 private readonly Func<float> _maxRange;
 private readonly Func<long> _historyFingerprint;
 private readonly Action<int> _activateMovement;
 private readonly Action _deactivateMovement;
 private readonly Action _onEnd;
 private readonly Func<bool> _autoExcludeUnframedParticipants;
 private bool _lastAutoExcludeUnframedParticipants;
 internal ScenePresentationController(Func<Agent, bool> canParticipate, Func<long> sequence,
  Func<Mission, string> combatReason, Func<float> maxRange, Func<long> historyFingerprint,
  Action<int> activateMovement, Action deactivateMovement, Action onEnd, Func<bool> autoExcludeUnframedParticipants = null)
 {
  _canParticipate = canParticipate; _sequence = sequence; _combatReason = combatReason;
  _maxRange = maxRange; _historyFingerprint = historyFingerprint;
  _activateMovement = activateMovement; _deactivateMovement = deactivateMovement; _onEnd = onEnd;
  _autoExcludeUnframedParticipants = autoExcludeUnframedParticipants;
 }
	internal SceneSpeechPlaybackInfo ShowNpcSpeechOutput(SceneSpeechOutputPort port, NpcDataPacket npc, Agent liveAgent, string content, bool allowTts = true, bool attachTtsToSceneAgent = true, bool suppressInteractionTimeoutArm = false)
	{
		SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = new SceneSpeechPlaybackInfo();
		if (!_canParticipate(liveAgent))
		{
			return sceneSpeechPlaybackInfo;
		}
		string text = port.SanitizeUiText(content);
		if (string.IsNullOrWhiteSpace(text))
		{
			return sceneSpeechPlaybackInfo;
		}
		try
		{
			string text2 = port.BuildPatienceBadge(npc, liveAgent);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = "【" + text2 + "】" + text;
			}
		}
		catch
		{
		}
		int packetAgentIndex = npc?.AgentIndex ?? (-1);
		int num = (liveAgent != null) ? liveAgent.Index : packetAgentIndex;
		if (packetAgentIndex >= 0 && num >= 0 && packetAgentIndex != num)
		{
			port.Report("ShowNpcSpeechOutput.AgentIndexMismatch", num, $"packetAgentIndex={packetAgentIndex};liveAgentIndex={num}");
		}
		string npcDisplayName = port.NpcDisplayName(npc);
		if (string.IsNullOrWhiteSpace(npcDisplayName))
		{
			npcDisplayName = "NPC";
		}
		bool flagHostileSpeech = port.IsHostile(liveAgent);
		if (flagHostileSpeech && num >= 0)
		{
			port.RemoveHostileInteraction(num);
		}
		long interactionToken = 0L;
		if (!flagHostileSpeech && !suppressInteractionTimeoutArm && num >= 0)
		{
			interactionToken = port.CaptureInteractionToken(num);
		}
		bool flag = false;
		TtsEngine.PlaybackRequest acceptedRequest = null;
		bool flag2 = allowTts && port.IsTtsEnabled();
		sceneSpeechPlaybackInfo.TtsEnabled = flag2;
		string text3 = "scene_lipsync_not_requested";
		bool flag3 = flag2 && attachTtsToSceneAgent && num >= 0 && _canParticipate(liveAgent) && port.CanLipSync(liveAgent, out text3);
		int num2 = (flag3 ? num : (-1));
		port.Report("ShowNpcSpeechOutput.Enter", num, $"allowTts={allowTts};attachToSceneAgent={attachTtsToSceneAgent};suppressTimeoutArm={suppressInteractionTimeoutArm};effectiveAgentIndex={num2};contentLen={(text ?? string.Empty).Length};hostileSpeech={flagHostileSpeech};lipSyncSafe={flag3};lipSyncReason={text3}");
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
			string text5 = port.SanitizeTtsText(text);
			try
			{
				if (npc != null && npc.IsHero)
				{
					Hero hero = port.ResolveHero(num);
					if (hero != null)
					{
						text4 = port.ExternalHeroVoice(hero);
						if (string.IsNullOrWhiteSpace(text4))
						{
							text4 = VoiceMapper.ResolveVoiceId(hero);
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text4) && npc != null)
				{
					text4 = VoiceMapper.ResolveVoiceIdForNonHero(npc.IsFemale, npc.Age, num);
				}
				if (!string.IsNullOrWhiteSpace(text5))
				{
					flag = TtsEngine.Instance.SpeakAsync(text5, -1, -1f, num2, text4, request =>
					{
						acceptedRequest = request;
						sceneSpeechPlaybackInfo.TtsAccepted = true;
						sceneSpeechPlaybackInfo.WaitForPlaybackFinished = num2 >= 0;
						sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, port.EstimateTypingDuration(text));
						port.Audio().TrackTtsPlaybackRequest(request, delegate
						{
							if (num2 < 0) { return; }
						sceneSpeechPlaybackInfo.VisualDurationSeconds = Math.Max(0.75f, port.EstimateTypingDuration(text));
						port.ClearPendingBubble(num, true);
						port.ClearPendingFeed(num);
						if (interactionToken != 0L)
						{
							port.EnqueueCompletionToken(num, interactionToken);
						}
						port.EnqueueBubble(num, liveAgent, text, npcDisplayName, sceneSpeechPlaybackInfo.VisualDurationSeconds);
						port.ScheduleFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
						});
					});
				}
			}
			catch
			{
			}
			if (!flag && acceptedRequest != null) { port.Audio().RetireTtsPlaybackRequest(acceptedRequest); }
			sceneSpeechPlaybackInfo.TtsAccepted = flag;
			sceneSpeechPlaybackInfo.WaitForPlaybackFinished = flag && num2 >= 0;
			port.Report("ShowNpcSpeechOutput.SpeakAttempt", num, $"effectiveAgentIndex={num2};speakAccepted={flag};voiceId={text4};lipSyncSafe={flag3};lipSyncReason={text3};ttsLen={(text5 ?? string.Empty).Length};uiLen={(text ?? string.Empty).Length}");
		}
		if (flag && num2 >= 0 && _canParticipate(liveAgent))
		{
			MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
			return sceneSpeechPlaybackInfo;
		}
		float num3 = port.EstimateTypingDuration(text);
		sceneSpeechPlaybackInfo.VisualDurationSeconds = num3;
		if (!port.ShowBubble(liveAgent, text, num3))
		{
			Logger.Log("FloatingText", "[Fallback] bubble unavailable, use message: npc=" + npcDisplayName);
		}
		if (interactionToken != 0L)
		{
			port.ArmInteractionTimeout(num, interactionToken, num3);
		}
		port.ScheduleFeed(num, npcDisplayName, text, sceneSpeechPlaybackInfo);
		MeetingBattleLockMissionBehavior.ReapplyMeetingLockForAgentIfNeeded(liveAgent, recaptureAnchor: false, preserveFacing: true);
		port.Report("ShowNpcSpeechOutput.BubbleFallback", num, $"interactionToken={interactionToken};typingDuration={num3:F2};ttsAccepted={flag};ttsEnabled={flag2}");
		return sceneSpeechPlaybackInfo;
	}


 internal static void BumpPresentation() { _presentationVersion = unchecked(_presentationVersion + 1); }
 internal string TradeRequestMode;
 internal bool TradeOwnsState;
 internal bool TradeStaged;
 internal string TradeSummary = "";
internal sealed class Member
	{
		internal Agent Agent;
		internal int AgentIndex;
		internal string Name;
		internal string Role;
		internal CharacterObject Character;
		internal ScenePresentationParticipantState State;
		internal bool ExplicitlyIncluded;
		internal bool InRange = true;
	}
	internal const float PresentationTickSeconds = 0.1f;
	internal const float PresentationTapSeconds = 0.25f;
	internal const int PresentationContextLines = 4;

	// Read every frame by the UI; plain static ints so polling costs nothing.
	internal static volatile int _presentationVersion;
	internal static volatile bool _presentationActive;
	internal static volatile bool _presentationCollapsed;

	internal readonly List<Member> _presentationMembers = new List<Member>();
	internal Mission _presentationMission;
	// Lets the static UI getter reject a stale flag (save reload, mission torn down without reset)
	// without an instance lookup and without pinning the old Mission in memory.
	internal static readonly WeakReference<Mission> _presentationMissionRef = new WeakReference<Mission>(null);
	internal int _presentationAddresseeIndex = -1;
	internal long _presentationStartSequence;
	internal long _presentationHistoryFingerprint;
	internal float _presentationTickElapsed;
	internal float _presentationLastPlayerHealth = -1f;



internal bool IsPresentationSessionLive()
	{
		return _presentationActive && _presentationMission != null && ReferenceEquals(_presentationMission, Mission.Current);
	}

internal bool IsUsablePresentationAgent(Agent agent, Mission mission)
	{
		try
		{
			return _canParticipate(agent) && ReferenceEquals(agent.Mission, mission) && agent != Agent.Main;
		}
		catch
		{
			return false;
		}
	}

internal Member FindPresentationMember(int agentIndex)
	{
		for (int i = 0; i < _presentationMembers.Count; i++)
		{
			if (_presentationMembers[i].AgentIndex == agentIndex)
			{
				return _presentationMembers[i];
			}
		}
		return null;
	}

internal bool IsPresentationAudience(Member member)
		=> IsPresentationAudience(member, _autoExcludeUnframedParticipants?.Invoke() == true);

private ScenePresentationParticipantState GetParticipantState(Member member, bool autoExclude)
	{
		// Automatic exclusion is derived; manual states survive toggling the setting in either direction.
		return autoExclude && !member.ExplicitlyIncluded && member.AgentIndex != _presentationAddresseeIndex
			&& member.State == ScenePresentationParticipantState.Participating
			? ScenePresentationParticipantState.Excluded : member.State;
	}

private bool IsPresentationAudience(Member member, bool autoExclude)
	{
		return member != null && ScenePresentationPolicy.IsAudience(GetParticipantState(member, autoExclude), IsUsablePresentationAgent(member.Agent, _presentationMission), member.InRange);
	}

internal int ChoosePresentationAddressee()
	{
		int count = _presentationMembers.Count;
		int[] indices = new int[count];
		ScenePresentationParticipantState[] states = new ScenePresentationParticipantState[count];
		bool[] audience = new bool[count];
		bool autoExclude = _autoExcludeUnframedParticipants?.Invoke() == true;
		for (int i = 0; i < count; i++)
		{
			indices[i] = _presentationMembers[i].AgentIndex;
			states[i] = GetParticipantState(_presentationMembers[i], autoExclude);
			audience[i] = IsPresentationAudience(_presentationMembers[i], autoExclude);
		}
		return ScenePresentationPolicy.ChooseAddressee(indices, states, audience);
	}

internal static string BuildPresentationRole(Agent agent, CharacterObject character)
	{
		try
		{
			Hero hero = character?.HeroObject;
			if (hero != null)
			{
				string clan = hero.Clan?.Name?.ToString();
				string title = hero.IsLord ? "领主" : hero.IsNotable ? "显要" : hero.IsWanderer ? "流浪者" : "";
				return string.IsNullOrWhiteSpace(clan) ? title : (string.IsNullOrWhiteSpace(title) ? clan : clan + " · " + title);
			}
			return character?.Name?.ToString() ?? agent?.Character?.Name?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}

internal bool AddPresentationMembers(IEnumerable<Agent> agents, bool explicitlyIncluded = true)
	{
		bool changed = false;
		foreach (Agent agent in agents ?? Enumerable.Empty<Agent>())
		{
			if (!IsUsablePresentationAgent(agent, _presentationMission))
			{
				continue;
			}
			Member existing = FindPresentationMember(agent.Index);
			if (existing != null)
			{
				if (explicitlyIncluded && !existing.ExplicitlyIncluded)
				{
					existing.ExplicitlyIncluded = true;
					changed = true;
				}
				continue;
			}
			CharacterObject character = agent.Character as CharacterObject;
			_presentationMembers.Add(new Member
			{
				Agent = agent,
				AgentIndex = agent.Index,
				Name = agent.Name?.ToString() ?? character?.Name?.ToString() ?? "NPC",
				Role = BuildPresentationRole(agent, character),
				Character = character,
				State = ScenePresentationParticipantState.Participating,
				ExplicitlyIncluded = explicitlyIncluded
			});
			changed = true;
		}
		return changed;
	}

internal bool EnsurePresentationSession(IReadOnlyList<Agent> framedAgents, int primaryAgentIndex)
	{
		Mission mission = Mission.Current;
		if (mission == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		if (!IsPresentationSessionLive())
		{
			EndPresentationSession("restart");
			_presentationMission = mission;
			_presentationMissionRef.SetTarget(mission);
			_presentationActive = true;
			_presentationCollapsed = false;
			_presentationStartSequence = _sequence() + 1;
			_presentationLastPlayerHealth = Agent.Main.Health;
			_presentationTickElapsed = 0f;
			Logger.Log("ScenePresentation", "session begin mission=" + (mission.SceneName ?? ""));
		}
		AddPresentationMembers(framedAgents);
		Member primary = FindPresentationMember(primaryAgentIndex);
		if (primary != null && primary.State == ScenePresentationParticipantState.Excluded)
		{
			primary.State = ScenePresentationParticipantState.Participating;
		}
		if (primary != null)
		{
			primary.InRange = true;
		}
		_presentationAddresseeIndex = IsPresentationAudience(primary) ? primaryAgentIndex : ChoosePresentationAddressee();
		if (_presentationAddresseeIndex < 0)
		{
			EndPresentationSession("no_participants");
			return false;
		}
		_presentationCollapsed = false;
		_activateMovement(_presentationAddresseeIndex);
		BumpPresentation();
		return true;
	}

internal void EndPresentationSession(string reason)
	{
		bool wasActive = _presentationActive;
		_presentationActive = false;
		_presentationCollapsed = false;
		_presentationMembers.Clear();
		_presentationMission = null;
		_presentationMissionRef.SetTarget(null);
		_presentationAddresseeIndex = -1;
		_presentationHistoryFingerprint = 0;
		_lastAutoExcludeUnframedParticipants = false;
		MergesHotkeyCharge=false;
		_onEnd();
		TradeRequestMode = null;
		ClearRound();
		if (wasActive)
		{
			_deactivateMovement();
			Logger.Log("ScenePresentation", "session end reason=" + (reason ?? ""));
			BumpPresentation();
		}
	}
internal void TickPresentationSession(float dt)
	{
		if (!_presentationActive)
		{
			return;
		}
		_presentationTickElapsed += dt;
		if (_presentationTickElapsed < PresentationTickSeconds)
		{
			return;
		}
		_presentationTickElapsed = 0f;
		Mission mission = Mission.Current;
		if (!ReferenceEquals(mission, _presentationMission) || mission == null || mission.MissionEnded)
		{
			EndPresentationSession("mission_changed");
			return;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			EndPresentationSession("native_conversation");
			return;
		}
		Agent player = Agent.Main;
		if (player == null || !player.IsActive() || player.Health <= 0f)
		{
			EndPresentationSession("player_down");
			return;
		}
		string combatReason = _combatReason(mission);
		if (combatReason != null)
		{
			EndPresentationSession(combatReason);
			return;
		}
		bool autoExclude = _autoExcludeUnframedParticipants?.Invoke() == true;
		bool changed = autoExclude != _lastAutoExcludeUnframedParticipants;
		_lastAutoExcludeUnframedParticipants = autoExclude;
		// Typing does not pause the mission; any damage hands control straight back to the player.
		if (!_presentationCollapsed && _presentationLastPlayerHealth >= 0f && player.Health < _presentationLastPlayerHealth - 0.01f)
		{
			_presentationCollapsed = true;
			changed = true;
			InformationManager.DisplayMessage(new InformationMessage("[场景会话] 你受到了攻击，会话面板已收起（按 T 展开）。", new Color(1f, 0.55f, 0.3f)));
		}
		_presentationLastPlayerHealth = player.Health;
		float maxRange = _maxRange();
		float maxRangeSquared = maxRange * maxRange;
		Vec3 playerPosition = player.Position;
		for (int i = _presentationMembers.Count - 1; i >= 0; i--)
		{
			Member member = _presentationMembers[i];
			if (!IsUsablePresentationAgent(member.Agent, mission))
			{
				_presentationMembers.RemoveAt(i);
				changed = true;
				continue;
			}
			bool inRange = ScenePresentationPolicy.IsInRange(member.State, member.Agent.Position.DistanceSquared(playerPosition), maxRangeSquared);
			if (inRange != member.InRange)
			{
				member.InRange = inRange;
				changed = true;
			}
		}
		if (!IsPresentationAudience(FindPresentationMember(_presentationAddresseeIndex)))
		{
			int next = ChoosePresentationAddressee();
			if (next < 0)
			{
				EndPresentationSession("no_participants");
				InformationManager.DisplayMessage(new InformationMessage("[场景会话] 周围已经没有可交谈的人，会话结束。", new Color(0.8f, 0.75f, 0.6f)));
				return;
			}
			_presentationAddresseeIndex = next;
			_activateMovement(next);
			changed = true;
		}
		long fingerprint = _historyFingerprint();
		if (fingerprint != _presentationHistoryFingerprint)
		{
			_presentationHistoryFingerprint = fingerprint;
			changed = true;
		}
		if (changed)
		{
			BumpPresentation();
		}
	}
	internal const float PresentationRoundSpeechGraceSeconds = 45f;

	// Between the send and the group start (gate wait, main-thread hop) there is no task yet. If another
	// channel claims the line (XihaiAction battle speech) no group starts, so the pending mark expires.
	internal const float PresentationRoundStartTimeoutSeconds = 8f;

	internal Task _presentationRoundTask;
	internal int _presentationRoundEpoch = -1;
	internal float _presentationRoundFinishedAt = -1f;
	internal float _presentationRoundPendingSince = -1f;


internal bool IsRoundActive(float now, int epoch, bool postprocessComplete, Func<bool> speechBusy)
	{
		Task task = _presentationRoundTask;
		if (task == null)
		{
			if (_presentationRoundPendingSince < 0f)
			{
				return false;
			}
			if (now - _presentationRoundPendingSince < PresentationRoundStartTimeoutSeconds)
			{
				return true;
			}
			_presentationRoundPendingSince = -1f;
			return false;
		}
		// An interrupt or a newer player line retired this round.
		if (_presentationRoundEpoch != epoch && task.IsCompleted)
		{
			ClearRound();
			return false;
		}
		if (!task.IsCompleted || !postprocessComplete)
		{
			return true;
		}
		if (_presentationRoundFinishedAt < 0f)
		{
			_presentationRoundFinishedAt = now;
		}
		if (now - _presentationRoundFinishedAt < PresentationRoundSpeechGraceSeconds && speechBusy())
		{
			return true;
		}
		ClearRound();
		return false;
	}
internal void ClearRound()
	{
		_presentationRoundTask = null;
		_presentationRoundEpoch = -1;
		_presentationRoundFinishedAt = -1f;
		_presentationRoundPendingSince = -1f;
	}
internal void NoteRoundGroup(Task groupTask, int epoch)
	{
		if (groupTask == null || _presentationRoundPendingSince < 0f)
		{
			return;
		}
		_presentationRoundTask = groupTask;
		_presentationRoundEpoch = epoch;
		_presentationRoundFinishedAt = -1f;
		_presentationRoundPendingSince = -1f;
	}
 internal void BeginRound(float now) { ClearRound(); _presentationRoundPendingSince = now; }
internal List<ScenePresentationParticipantInfo> GetParticipants()
	{
		List<ScenePresentationParticipantInfo> result = new List<ScenePresentationParticipantInfo>();
		if (!IsPresentationSessionLive())
		{
			return result;
		}
		bool autoExclude = _autoExcludeUnframedParticipants?.Invoke() == true;
		foreach (Member member in _presentationMembers)
		{
			result.Add(new ScenePresentationParticipantInfo
			{
				AgentIndex = member.AgentIndex,
				Name = member.Name ?? "",
				Role = member.Role ?? "",
				State = GetParticipantState(member, autoExclude),
				IsAddressee = member.AgentIndex == _presentationAddresseeIndex,
				IsInRange = member.InRange,
				Character = member.Character
			});
		}
		return result;
	}
internal bool SetAddressee(int agentIndex)
	{
		Member member = IsPresentationSessionLive() ? FindPresentationMember(agentIndex) : null;
		if (member == null)
		{
			return false;
		}
		if (member.State == ScenePresentationParticipantState.Excluded)
		{
			member.State = ScenePresentationParticipantState.Participating;
		}
		member.ExplicitlyIncluded = true;
		if (!IsPresentationAudience(member))
		{
			return false;
		}
		_presentationAddresseeIndex = agentIndex;
		_activateMovement(agentIndex);
		BumpPresentation();
		return true;
	}
internal void CycleParticipant(int agentIndex)
	{
		Member member = IsPresentationSessionLive() ? FindPresentationMember(agentIndex) : null;
		if (member == null)
		{
			return;
		}
		member.State = ScenePresentationPolicy.NextState(GetParticipantState(member, _autoExcludeUnframedParticipants?.Invoke() == true), agentIndex == _presentationAddresseeIndex);
		member.ExplicitlyIncluded = true;
		BumpPresentation();
	}
internal void SetAllParticipants(bool include)
	{
		if (!IsPresentationSessionLive())
		{
			return;
		}
		foreach (Member member in _presentationMembers)
		{
			if (include)
			{
				if (member.State == ScenePresentationParticipantState.Excluded)
				{
					member.State = ScenePresentationParticipantState.Participating;
				}
				member.ExplicitlyIncluded = true;
			}
			else if (!include && member.State == ScenePresentationParticipantState.Participating && member.AgentIndex != _presentationAddresseeIndex)
			{
				member.State = ScenePresentationParticipantState.Excluded;
			}
		}
		BumpPresentation();
	}

 internal void ReleaseTrade(Action resetTransfer)
 {
  if (TradeOwnsState) resetTransfer();
  TradeOwnsState = false; TradeStaged = false; TradeSummary = "";
 }
 // UI selection validation only. The game transfer port still owns inventory and the actual commit.
 internal bool StageTrade(IReadOnlyList<ScenePresentationTradeOption> options, IReadOnlyList<int> indices,
  IReadOnlyList<int> amounts, string verb, string targetName, Action clearItems, Action<int,int> stageItem, Action finishItems, out string status)
 {
  status = "";
  if (!TradeOwnsState || options == null || options.Count == 0) { status = "给予列表已失效，请重新打开。"; return false; }
  if (indices == null || amounts == null || indices.Count == 0 || indices.Count != amounts.Count) { status = "请先选择要给予的资源。"; return false; }
  clearItems();
  HashSet<int> seen = new HashSet<int>(); List<string> labels = new List<string>();
  for (int i=0; i<indices.Count; i++)
  {
   int index=indices[i]; if (index < 0 || index >= options.Count || !seen.Add(index)) continue;
   ScenePresentationTradeOption option=options[index]; int amount=option.IsSettlement ? 1 : amounts[i];
   if (amount < 1 || amount > option.Available) { clearItems(); status="数量超出可用范围："+(option.ValidationName ?? option.Name ?? "资源"); return false; }
   stageItem(index,amount); labels.Add(option.Name+" ×"+amount);
  }
  if (labels.Count==0) { status="请先选择要给予的资源。"; return false; }
  finishItems(); TradeStaged=true; TradeSummary=verb+" "+(targetName ?? "对方")+"："+string.Join("、",labels);
  BumpPresentation(); return true;
 }
 internal bool ValidateStagedTrade(bool hasTargetAndItems, bool targetInAudience, Action resetTransfer, out string status)
 {
  status="";
  if (TradeOwnsState && hasTargetAndItems && targetInAudience) return true;
  ReleaseTrade(resetTransfer); BumpPresentation(); status="给予对象已离开或给予已失效，本次没有交付，话也没有发出。"; return false;
 }
 internal void ConsumeStagedTrade() { TradeOwnsState=false; TradeStaged=false; TradeSummary=""; }
internal HashSet<int> GetPresentationExcludedAgentIndices()
	{
		if (!IsPresentationSessionLive())
		{
			return null;
		}
		HashSet<int> excluded = null;
		bool autoExclude = _autoExcludeUnframedParticipants?.Invoke() == true;
		foreach (Member member in _presentationMembers)
		{
			if (GetParticipantState(member, autoExclude) == ScenePresentationParticipantState.Excluded)
			{
				(excluded ??= new HashSet<int>()).Add(member.AgentIndex);
			}
		}
		return excluded;
	}
internal List<ScenePresentationHistoryLine> BuildHistory(int count, Func<int, ScenePresentationHistoryRecord> read, ScenePresentationFactNormalizer normalizeFact, int maxLines)
	{
		List<ScenePresentationHistoryLine> lines = new List<ScenePresentationHistoryLine>();
		int context = 0;
		{
			for (int i = count - 1; i >= 0 && lines.Count < maxLines; i--)
			{
				ScenePresentationHistoryRecord message = read(i);
				if (!message.Exists)
				{
					continue;
				}
				if (message.EventSequence < _presentationStartSequence && ++context > PresentationContextLines)
				{
					break;
				}
				ScenePresentationHistoryLine line = BuildHistoryLine(message, normalizeFact);
				if (line != null)
				{
					lines.Add(line);
				}
			}
		}
		lines.Reverse();
		return lines;
	}
private static ScenePresentationHistoryLine BuildHistoryLine(ScenePresentationHistoryRecord message, ScenePresentationFactNormalizer normalizeFact)
	{
		string role = (message.Role ?? "").Trim();
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return null;
		}
		if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
		{
			string target = (message.TargetName ?? "").Trim();
			return new ScenePresentationHistoryLine { Speaker = string.IsNullOrWhiteSpace(target) ? "你" : "你 → " + target, Text = content, Kind = "player" };
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			return new ScenePresentationHistoryLine { Speaker = (message.SpeakerName ?? "NPC").Trim(), Text = content, Kind = "npc" };
		}
		// Only confirmed AFEF facts are shown; scene descriptions and prompt scaffolding stay internal.
		return normalizeFact(content, out var fact)
			? new ScenePresentationHistoryLine { Speaker = "记录", Text = fact, Kind = "fact" }
			: null;
	}
internal bool UpdateHotkey(ScenePresentationHotkeyPort input, InputKey shoutKey, InputKey specialMenuKey)
	{
		if (input.IsCharging() && MergesHotkeyCharge)
		{
			if (!IsPresentationSessionLive())
			{
				input.Cancel("session_ended");
				return true;
			}
			float held = input.HeldSeconds();
			if (input.IsReleased())
			{
				Action merge = held >= PresentationTapSeconds ? input.CaptureMerge() : null;
				input.Cancel("released");
				if (merge != null)
				{
					merge();
				}
				else if (_presentationCollapsed)
				{
					_presentationCollapsed = false;
					BumpPresentation();
				}
				return true;
			}
			if (!input.IsDown())
			{
				input.Cancel("key_state_lost");
				return true;
			}
			if (held >= PresentationTapSeconds)
			{
				input.DrawPreview();
			}
			return true;
		}
		if (!IsPresentationSessionLive() || input.IsCharging())
		{
			return false;
		}
		if (!input.BeginIfPressed(shoutKey, specialMenuKey)) return false;
  MergesHotkeyCharge = true;
  return true;
 }
 internal bool MergesHotkeyCharge;

 internal bool PrepareTextSubmission(string text, Func<bool> isMainThread, Func<bool> blocked, Func<bool> roundActive,
  out string content, out string status)
 {
  status=""; content=(text ?? "").Replace("\r", "").Trim();
  if (!IsPresentationSessionLive()) { status="场景会话已结束。"; return false; }
  if (string.IsNullOrWhiteSpace(content) || !isMainThread()) return false;
  if (blocked()) { status=roundActive() ? "上一轮回应还没结束；想插话请点「打断」。" : "上一句还在处理中，请稍候。"; return false; }
  if (!IsPresentationAudience(FindPresentationMember(_presentationAddresseeIndex)))
  {
   _presentationAddresseeIndex=ChoosePresentationAddressee();
   if (_presentationAddresseeIndex<0) { EndPresentationSession("no_participants"); status="周围已经没有可交谈的人。"; return false; }
  }
  return true;
 }
 internal void VisitAudience(Action<int,Agent> visit)
 {
  bool autoExclude = _autoExcludeUnframedParticipants?.Invoke() == true;
  foreach (Member member in _presentationMembers) if (IsPresentationAudience(member, autoExclude)) visit(member.AgentIndex,member.Agent);
 }
 internal void AbsorbAudience(IReadOnlyList<Agent> agents)
 {
  if (IsPresentationSessionLive() && AddPresentationMembers(agents, explicitlyIncluded: false)) BumpPresentation();
 }
}

internal delegate bool ScenePresentationFactNormalizer(string content, out string fact);
internal struct ScenePresentationHistoryRecord
{
 internal bool Exists;
 internal long EventSequence;
 internal string Role, Content, TargetName, SpeakerName;
}

internal sealed class ScenePresentationHotkeyPort
{
 internal Func<bool> IsCharging, IsReleased, IsDown;
 internal Func<float> HeldSeconds;
 internal Func<InputKey,InputKey,bool> BeginIfPressed;
 internal Func<Action> CaptureMerge;
 internal Action<string> Cancel;
 internal Action DrawPreview;
}

// Stateless named leaves; shared interaction/bubble state remains in its original authoritative adapter.
internal sealed class SceneSpeechOutputPort
{
 internal Func<string,string> SanitizeUiText, SanitizeTtsText;
 internal Func<NpcDataPacket,Agent,string> BuildPatienceBadge;
 internal Func<NpcDataPacket,string> NpcDisplayName;
 internal Func<Agent,bool> IsHostile;
 internal Func<bool> IsTtsEnabled;
 internal SceneLipSyncAgentCheck CanLipSync;
 internal Func<int,TaleWorlds.CampaignSystem.Hero> ResolveHero;
 internal Func<TaleWorlds.CampaignSystem.Hero,string> ExternalHeroVoice;
 internal Func<string,float> EstimateTypingDuration;
 internal Func<SceneAudioLipSyncController> Audio;
 internal Action<int> RemoveHostileInteraction, ClearPendingFeed;
 internal Func<int,long> CaptureInteractionToken;
 internal Action<string,int,string> Report;
 internal Action<int,bool> ClearPendingBubble;
 internal Action<int,long> EnqueueCompletionToken;
 internal Action<int,Agent,string,string,float> EnqueueBubble;
 internal Action<int,string,string,SceneSpeechPlaybackInfo> ScheduleFeed;
 internal Func<Agent,string,float,bool> ShowBubble;
 internal Action<int,long,float> ArmInteractionTimeout;
}
