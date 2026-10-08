using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SandBox;
namespace AnimusForge;

internal sealed class NativeConversationSpeechAdapter
{
    private readonly NativeConversationSpeechPorts _ports;
    internal NativeConversationSpeechAdapter(NativeConversationSpeechPorts ports)
    { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }
	internal const float LIP_SYNC_SAFE_MAX_DISTANCE = 6.5f;

	internal static bool IsTtsPlaybackEnabledForShout()
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

	internal static bool IsTtsPlaybackEnabledForNativeConversation(out string disabledReason)
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

	internal void ResumeTtsForNativeConversationReply()
	{
		try
		{
			bool wasPaused = _ports.Audio().ClearPauseForNativeReply();
			try
			{
				_ports.SetTypingPaused(false);
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

	internal void EnsureTtsPlaybackEventsSubscribedForNativeConversation()
	{
		try
		{
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			if (_ports.Audio().IsSubscribed) return;
			_ports.Audio().SubscribeTtsPlaybackEvents();
			Logger.Log("NativeConversation", "[TTS] ensured playback event subscription for native conversation");
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] ensure playback event subscription failed: " + ex.Message);
		}
	}

	internal void TrySpeakNativeConversationReplyWithTts(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, int targetAgentIndex, string visibleText)
	{
		try
		{
			string uiText = _ports.SanitizeUiText(visibleText);
			// TTS retains the plain reply, while the typewriter receives a safe display copy so model markup cannot become live RichText mid-word.
			string typewriterText = EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(uiText);
			string ttsText = _ports.SanitizeTtsText(uiText);
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
					voiceId = _ports.PreferredHeroVoice(targetHero);
					if (!string.IsNullOrWhiteSpace(voiceId))
					{
						voiceSource = "hero_preferred";
					}
					if (string.IsNullOrWhiteSpace(voiceId))
					{
						voiceId = _ports.MapHeroVoice(targetHero);
						voiceSource = "hero_mapper";
					}
				}
				if (string.IsNullOrWhiteSpace(voiceId))
				{
					bool isFemale = npc?.IsFemale ?? targetHero?.IsFemale ?? targetCharacter?.IsFemale ?? false;
					float age = npc?.Age ?? 0f;
					if (age < 18f || age > 80f)
					{
						age = targetHero?.Age ?? _ports.NonHeroAge(targetCharacter);
					}
					if (targetHero == null && targetAgentIndex < 0)
					{
						voiceKey = _ports.NonHeroVoiceKey(npc, targetHero, targetCharacter, targetAgentIndex);
					}
					voiceId = _ports.MapNonHeroVoice(voiceKey, isFemale, age, targetAgentIndex);
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
				lipSyncSafe = _ports.CanParticipate(liveAgent) && CanAgentUseSceneLipSync(liveAgent, out lipSyncReason);
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
					_ports.Audio().TrackTtsPlaybackRequest(request);
					float estimatedDuration = Math.Max(0.75f, _ports.TypingDuration(typewriterText));
					acceptedWaitToken = _ports.Wait.RegisterNativeConversationTtsPlaybackWait(request, estimatedDuration, typewriterText.Length);
					if (acceptedWaitToken == 0L) { return; }
					ConversationHelper.StartTypewriterText(typewriterText, estimatedDuration, waitForPlayback: true);
					_ports.Wait.ScheduleNativeConversationTypewriterPlaybackFallback(acceptedWaitToken, effectiveAgentIndex, estimatedDuration, typewriterText.Length);
				});
			}
			catch (Exception ex2)
			{
				if (acceptedWaitToken != 0L && _ports.Wait.IsNativeConversationTtsPlaybackWaitToken(acceptedWaitToken, effectiveAgentIndex))
				{
					_ports.Wait.CompleteNativeConversationTtsPlaybackWaitByToken(acceptedWaitToken, "enqueue_rejected");
					ConversationHelper.StartTypewriterPlaybackIfWaiting();
				}
				LogTtsReport("NativeConversationTts.SpeakFailed", targetAgentIndex, $"effectiveAgentIndex={effectiveAgentIndex};lipSyncSafe={lipSyncSafe};reason={lipSyncReason};error={ex2.Message}");
				Logger.Log("NativeConversation", "[TTS] SpeakAsync threw for native conversation reply: " + ex2.Message);
				return;
			}
			if (!accepted)
			{
				if (acceptedWaitToken != 0L && _ports.Wait.IsNativeConversationTtsPlaybackWaitToken(acceptedWaitToken, effectiveAgentIndex))
				{
					_ports.Wait.CompleteNativeConversationTtsPlaybackWaitByToken(acceptedWaitToken, "enqueue_rejected");
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

	internal bool CanAgentUseSceneLipSync(Agent agent, out string reason) => CanAgentUseSceneLipSync(agent, out reason, _ports);
	internal static bool CanAgentUseSceneLipSync(Agent agent, out string reason, NativeConversationSpeechPorts ports)
	{
		reason = "unknown";
		try
		{
			if (!ports.CanParticipate(agent))
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
			if (IsNativeConversationLipSyncAgent(agent, ports))
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

	internal bool IsNativeConversationLipSyncAgentIndex(int agentIndex) => IsNativeConversationLipSyncAgentIndex(agentIndex, _ports);
	internal static bool IsNativeConversationLipSyncAgentIndex(int agentIndex, NativeConversationSpeechPorts ports)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return false;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			return IsNativeConversationLipSyncAgent(agent, ports);
		}
		catch
		{
			return false;
		}
	}

	internal bool IsNativeConversationLipSyncAgent(Agent agent) => IsNativeConversationLipSyncAgent(agent, _ports);
	internal static bool IsNativeConversationLipSyncAgent(Agent agent, NativeConversationSpeechPorts ports)
	{
		if (agent == null || !ports.IsInputOpen())
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
			if (!ports.ResolveTarget(out var targetHero, out var targetCharacter, out var _))
			{
				return false;
			}
			return ports.IsValidTargetAgent(agent, targetHero, targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	internal bool CanAgentUseSceneLipSyncExternal(int agentIndex, out string reason) => CanAgentUseSceneLipSyncExternal(agentIndex, out reason, _ports);
	internal static bool CanAgentUseSceneLipSyncExternal(int agentIndex, out string reason, NativeConversationSpeechPorts ports)
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
			return CanAgentUseSceneLipSync(agent, out reason, ports);
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.GetType().Name;
			return false;
		}
	}

	internal bool ShouldSuppressNativeConversationVisibleStreamingForTtsExternal()
	{
		try
		{
			if (_ports.CurrentOwner() == null || !_ports.IsInputOpen() || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
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

	internal bool ShouldUseMapConversationTableauPlaybackForNativeTtsExternal()
	{
		try
		{
			if (_ports.CurrentOwner() == null || !_ports.IsInputOpen() || Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
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

	internal static bool IsMapConversationMission(ICampaignMission campaignMission)
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

	internal bool TryQueueNativeMapConversationTableauTtsPlayback(TtsEngine.PlaybackRequest request, string wavPath, string xmlPath, float durationSecs)
	{
		try
		{
			NativeConversationSpeechAdapter instance = _ports.CurrentOwner();
			if (instance == null || !instance._ports.Audio().IsTtsPlaybackRequestCurrent(request) || !ShouldUseMapConversationTableauPlaybackForNativeTtsExternal())
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
			{
				return false;
			}
			SceneAudioLipSyncController.RunTtsMainThreadEventStep(delegate
			{
				try
				{
					if (!instance._ports.Audio().IsTtsPlaybackRequestCurrent(request) || !ShouldUseMapConversationTableauPlaybackForNativeTtsExternal())
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
					if (!instance._ports.Audio().IsTtsPlaybackRequestCurrent(request)) { return; }
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

	internal static void ScheduleNativeMapConversationTtsFileCleanup(string wavPath, string xmlPath, float durationSecs)
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

	internal void LogTtsReport(string stage, int agentIndex, string extra = null)
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			bool active = agent != null && agent.IsActive();
			bool hostile = _ports.IsHostile(agent);
			string agentName = (agent?.Name?.ToString() ?? "").Trim();
   SceneAudioResourceSnapshot resource=_ports.Audio().GetResourceSnapshot(agentIndex);
   bool speaking=resource.Speaking, hasSe=resource.HasSound, hasWav=resource.HasWav, hasXml=resource.HasXml;
            NativeSpeechOutputDiagnosticSnapshot queue = _ports.QueueDiagnostic(agentIndex);
            bool hasPendingBubble = queue.PendingBubbleCount > 0;
            int pendingBubbleCount = queue.PendingBubbleCount;
            bool hasPendingDuration = queue.PendingDurationCount > 0;
            int pendingDurationCount = queue.PendingDurationCount;
            bool hasPendingSpeechToken = queue.PendingSpeechTokenCount > 0;
            int pendingSpeechTokenCount = queue.PendingSpeechTokenCount;
            NativeSpeechInteractionDiagnosticSnapshot interaction = _ports.InteractionDiagnostic(agentIndex);
            bool hasInteraction = interaction.HasInteraction;
            long interactionToken = interaction.InteractionToken;
            bool timeoutArmed = interaction.TimeoutArmed;
            bool hasPendingArm = interaction.HasPendingArm;
			float missionTime = Mission.Current?.CurrentTime ?? (-1f);
			string sceneName = (Mission.Current?.SceneName ?? "").Trim();
			string pendingArmAt = hasPendingArm ? interaction.ArmAtMissionTime.ToString("F2") : "-";
			string extraSuffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;
			Logger.LogVerbose("TTSReport", "tts_report:" + (stage ?? "") + ":" + agentIndex, () => $"[{stage}] agentIndex={agentIndex}, name={agentName}, active={active}, hostile={hostile}, speaking={speaking}, hasSe={hasSe}, hasWav={hasWav}, hasXml={hasXml}, hasInteraction={hasInteraction}, interactionToken={interactionToken}, timeoutArmed={timeoutArmed}, hasPendingArm={hasPendingArm}, pendingArmAt={pendingArmAt}, pendingBubble={hasPendingBubble}, pendingBubbleCount={pendingBubbleCount}, pendingDuration={hasPendingDuration}, pendingDurationCount={pendingDurationCount}, pendingSpeechToken={hasPendingSpeechToken}, pendingSpeechTokenCount={pendingSpeechTokenCount}, missionTime={missionTime:F2}, scene={sceneName}{extraSuffix}", 2.0);
		}
		catch (Exception ex)
		{
			Logger.Log("TTSReport", $"[{stage}] report_failed agentIndex={agentIndex}, error={ex.Message}");
		}
	}
}
internal sealed class NativeConversationSpeechPorts
{
    internal delegate bool ResolveTargetCallback(out Hero hero, out CharacterObject character, out string name);
    internal Func<NativeConversationSpeechAdapter> CurrentOwner;
    internal Func<SceneAudioLipSyncController> Audio;
    internal NativeConversationPlaybackWaitAdapter Wait;
    internal Action<bool> SetTypingPaused;
    internal Func<string, string> SanitizeUiText, SanitizeTtsText;
    internal Func<Hero, string> PreferredHeroVoice, MapHeroVoice;
    internal Func<string, bool, float, int, string> MapNonHeroVoice;
    internal Func<CharacterObject, float> NonHeroAge;
    internal Func<NpcDataPacket, Hero, CharacterObject, int, string> NonHeroVoiceKey;
    internal Func<Agent, bool> CanParticipate, IsHostile;
    internal Func<Agent, Hero, CharacterObject, bool> IsValidTargetAgent;
    internal ResolveTargetCallback ResolveTarget;
    internal Func<bool> IsInputOpen;
    internal Func<string, float> TypingDuration;
    internal Func<int, NativeSpeechOutputDiagnosticSnapshot> QueueDiagnostic;
    internal Func<int, NativeSpeechInteractionDiagnosticSnapshot> InteractionDiagnostic;
}
internal struct NativeSpeechOutputDiagnosticSnapshot
{
    internal int PendingBubbleCount, PendingDurationCount, PendingSpeechTokenCount;
}
internal struct NativeSpeechInteractionDiagnosticSnapshot
{
    internal bool HasInteraction, TimeoutArmed, HasPendingArm;
    internal long InteractionToken;
    internal float ArmAtMissionTime;
}
