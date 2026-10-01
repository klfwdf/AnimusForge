using static AnimusForge.SceneMovementController;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
namespace AnimusForge;
public partial class ShoutBehavior
{
 private SceneAudioLipSyncController _sceneAudio;
 private SceneAudioLipSyncController SceneAudio => _sceneAudio ??= new SceneAudioLipSyncController(new SceneAudioLipSyncPort
 {
  SceneSessionId = () => Volatile.Read(ref _sceneHistorySessionId), ConversationEpoch = () => Volatile.Read(ref _sceneConversationEpoch),
  Dispatch = action => _mainThreadActions.Enqueue(action), Cancelled = HandleTtsPlaybackCancelled,
  Finished = HandleSceneTtsPlaybackFinishedOnMainThread,
  CompleteNativeWait = (request, reason) => CompleteNativeConversationTtsPlaybackWait(request, reason),
  IsNativeWait = IsNativeConversationTtsPlaybackWaitRequest, QueueMapPlayback = TryQueueNativeMapConversationTableauTtsPlayback,
  UseMapPlayback = ShouldUseMapConversationTableauPlaybackForNativeTtsExternal, CanParticipate = CanAgentParticipateInSceneSpeechExternal,
  CanLipSync = CanAgentUseSceneLipSync, CanLipSyncIndex = CanAgentUseSceneLipSyncExternal,
  AudioDuration = EnqueuePendingAudioDuration, DispatchBubble = TryDispatchPendingNpcBubbleForTts,
  ClearOrphanDuration = ClearOrphanPendingAudioDuration, ScheduleBubbleFallback = request => SchedulePendingNpcBubbleFallbackDispatch(request),
  HasPlaybackStarted = index => { lock (_ttsBubbleSyncLock) return _ttsPlaybackStartedAgents.Contains(index); },
  MarkPlaybackStarted = index => { lock (_ttsBubbleSyncLock) _ttsPlaybackStartedAgents.Add(index); },
  ClearAgentPending = ClearSceneAudioPendingForAgent, ClearAgentInteraction = index => { _pendingInteractionTimeoutArms.Remove(index); _activeInteractionSessions.Remove(index); },
  PauseTyping = paused => _floatingTextView?.SetTypingPaused(paused), StopTyping = () => _floatingTextView?.StopTypingForAll(fadeSoon: true),
  ClearPending = ClearPendingTtsBubbleSyncQueues, SpeechBusy = IsSpeechPipelineBusy, ClearSpeechQueue = ClearQueuedSceneSpeech,
  FailureOutput = ResolveSceneAudioFailureOutput, UnmarkPlaybackStarted = index => { lock (_ttsBubbleSyncLock) _ttsPlaybackStartedAgents.Remove(index); },
  ConvertDialogueFeed = ConvertPendingSceneDialogueFeedToTimedFlush, EstimateTypingDuration = EstimateBubbleTypingDurationSeconds,
  ShowBubble = (agent, text, duration) => TryShowNpcBubble(agent, text, duration), ScheduleInteractionTimeout = ScheduleInteractionTimeoutArm,
  FlushFailedSpeechEffects = (index, duration) => { _sceneMovement.FlushPendingSceneSummonLaunches(index, duration); _sceneMovement.FlushPendingSceneGuideLaunches(index, duration); FlushLordsHallMissionEntryAfterSpeech(index); },
  Report = LogTtsReport, ClearMeetingControl = ClearMeetingSceneConversationControlState, SuppressMeetingControl = ShouldSuppressSceneConversationControlForMeeting
 });
 private SceneAudioFailureOutput ResolveSceneAudioFailureOutput(int index)
 {
  bool hasToken = TryDequeuePendingSpeechCompletionToken(index, out long token);
  PendingNpcBubbleEntry bubble = null; float duration = -1f;
  bool hasBubble = index >= 0 && TryDequeuePendingNpcBubble(index, out bubble, out duration);
  return new SceneAudioFailureOutput { HasInteractionToken = hasToken, InteractionToken = token, HasBubble = hasBubble,
   Agent = bubble?.Agent, UiContent = bubble?.UiContent, TypingDuration = duration };
 }
 private void ResetSceneAudioRequestOwnership() => SceneAudio.ResetRequestOwnership();
 private void ClearSceneAudioPendingForAgent(int index)
 {
  lock (_ttsBubbleSyncLock) { _pendingNpcBubbleQueues.Remove(index); _pendingAudioDurationQueues.Remove(index); _pendingSpeechCompletionTokenQueues.Remove(index); _pendingSceneDialogueFeedQueues.Remove(index); }
 }
	private void TrackTtsPlaybackRequest(TtsEngine.PlaybackRequest request, Action prepareSceneOutput = null) => SceneAudio.TrackTtsPlaybackRequest(request, prepareSceneOutput);
	private bool IsTtsPlaybackRequestCurrent(TtsEngine.PlaybackRequest request, bool allowCancelled = false) => SceneAudio.IsTtsPlaybackRequestCurrent(request, allowCancelled);
	private bool PrepareTtsPlaybackRequest(TtsEngine.PlaybackRequest request) => SceneAudio.PrepareTtsPlaybackRequest(request);
	private bool IsActiveTtsPlaybackRequest(TtsEngine.PlaybackRequest request, bool allowCancelled = false) => SceneAudio.IsActiveTtsPlaybackRequest(request, allowCancelled);
	private void RetireTtsPlaybackRequest(TtsEngine.PlaybackRequest request) => SceneAudio.RetireTtsPlaybackRequest(request);
	private static void RunTtsMainThreadEventStep(Action action) => SceneAudioLipSyncController.RunTtsMainThreadEventStep(action);
	private void SubscribeTtsPlaybackEvents() => SceneAudio.SubscribeTtsPlaybackEvents();
	private void UnsubscribeTtsPlaybackEventsInternal(TtsEngine tts, bool clearOwnerIfMatch) => SceneAudio.UnsubscribeTtsPlaybackEventsInternal(tts, clearOwnerIfMatch);
	private void UnsubscribeTtsPlaybackEvents() => SceneAudio.UnsubscribeTtsPlaybackEvents();
	private void CleanupSceneLipSyncAfterPlaybackFinished(int agentIndex) => SceneAudio.CleanupSceneLipSyncAfterPlaybackFinished(agentIndex);
	private static bool IsMissionSceneReadyForSoundOps() => SceneAudioLipSyncController.IsMissionSceneReadyForSoundOps();
	private static bool IsGameWindowFocused() => SceneAudioLipSyncController.IsGameWindowFocused();
	private bool IsInEscapeTransitionWindow() => SceneAudio.IsInEscapeTransitionWindow();
	private bool HasInterruptionDebounceElapsed() => SceneAudio.HasInterruptionDebounceElapsed();
	private void ApplyTtsPauseState() => SceneAudio.ApplyTtsPauseState();
	private void PauseTtsForShoutUi() => SceneAudio.PauseTtsForShoutUi();
	private void ResumeTtsAfterShoutUi() => SceneAudio.ResumeTtsAfterShoutUi();
	private void TryResumeInterruptionPauseIfPossible() => SceneAudio.TryResumeInterruptionPauseIfPossible();
	private void QueueDeferredCleanup(SoundEvent se, string wavPath, string xmlPath, string source = "Unknown", int agentIndex = -1) => SceneAudio.QueueDeferredCleanup(se, wavPath, xmlPath, source, agentIndex);
	private bool IsDeferredCleanupWindowStable() => SceneAudio.IsDeferredCleanupWindowStable();
	private void ProcessDeferredCleanup() => SceneAudio.ProcessDeferredCleanup();
	private void HandleEscapePressedForAudioSafety(string reason = "ESC") => SceneAudio.HandleEscapePressedForAudioSafety(reason);
	private bool HasLipSyncOrSceneSpeechWork() => SceneAudio.HasLipSyncOrSceneSpeechWork();
	private void HandleCriticalUiTransitionForLipSyncSafety(string reason = "UI_TRANSITION") => SceneAudio.HandleCriticalUiTransitionForLipSyncSafety(reason);
	private static void SafeStopAndReleaseSoundEvent(SoundEvent se) => SceneAudioLipSyncController.SafeStopAndReleaseSoundEvent(se);
	private void StopAllLipSyncPlaybackAndCleanup() => SceneAudio.StopAllLipSyncPlaybackAndCleanup();
	private void TickLipSyncAnimations(float dt) => SceneAudio.TickLipSyncAnimations(dt);
	internal HashSet<int> GetSpeakingAgentIndicesSnapshot() => SceneAudio.GetSpeakingAgentIndicesSnapshot();
	private void CleanupPreviousLipSyncPlaybackForReplacement(string reason) => SceneAudio.CleanupPreviousLipSyncPlaybackForReplacement(reason);
	private static void PrepareAgentForTrueLipSyncIfPossible(Agent agent) => SceneAudioLipSyncController.PrepareAgentForTrueLipSyncIfPossible(agent);
	private static void LogLipSyncNativeProbe(string stage, int agentIndex, string extra = null) => SceneAudioLipSyncController.LogLipSyncNativeProbe(stage, agentIndex, extra);
	private void StopAgentRhubarbRecordIfPossible(int agentIndex, string reason = "Unknown") => SceneAudio.StopAgentRhubarbRecordIfPossible(agentIndex, reason);
	private void DetachAgentLipSyncForSafety(int agentIndex, string reason) => SceneAudio.DetachAgentLipSyncForSafety(agentIndex, reason);
	private void InterruptAgentSpeechForCombat(int agentIndex, string reason) => SceneAudio.InterruptAgentSpeechForCombat(agentIndex, reason);
	private void CancelAgentSpeechForRemoval(int agentIndex, string reason) => SceneAudio.CancelAgentSpeechForRemoval(agentIndex, reason);
	private void HandleTtsPlaybackCancelled(TtsEngine.PlaybackRequest request)
	{
		if (request == null) { return; }
		long completedWaitRevision;
		lock (_nativeConversationTtsPlaybackWaitLock) { completedWaitRevision = _nativeConversationTtsPlaybackWaitToken; }
		bool releaseNativeTypewriter = CompleteNativeConversationTtsPlaybackWait(request, "playback_cancelled");
		_mainThreadActions.Enqueue(delegate
		{
			try
			{
				if (!IsTtsPlaybackRequestCurrent(request, allowCancelled: true)) { return; }
				if (releaseNativeTypewriter)
				{
					lock (_nativeConversationTtsPlaybackWaitLock)
					{
						if (_nativeConversationTtsPlaybackWaitToken == completedWaitRevision && _nativeConversationTtsPlaybackWaitTcs == null)
						{
							ConversationHelper.StartTypewriterPlaybackIfWaiting();
						}
					}
				}
				if (request.AgentIndex >= 0 && IsActiveTtsPlaybackRequest(request, allowCancelled: true))
				{
					ClearPendingTtsBubbleSyncForAgent(request.AgentIndex, clearInteractionToken: true);
					ClearPendingSceneDialogueFeedForAgent(request.AgentIndex);
					CleanupSceneLipSyncAfterPlaybackFinished(request.AgentIndex);
				}
			}
			finally { RetireTtsPlaybackRequest(request); }
		});
	}
	private void HandleSceneTtsPlaybackFinishedOnMainThread(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		FreezeWatchdog.Mark("SceneTts.playback_finished.main_begin", "agent=" + agentIndex + " thread=" + Thread.CurrentThread.ManagedThreadId, immediate: true);
		long interactionToken = 0L;
		bool hasInteractionToken = false;
		bool hostileFinishedSpeech = false;
		RunSceneTtsPlaybackFinishedStep("resolve_state", agentIndex, delegate
		{
			hasInteractionToken = TryDequeuePendingSpeechCompletionToken(agentIndex, out interactionToken);
			Agent finishedAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			hostileFinishedSpeech = IsAgentHostileToMainAgent(finishedAgent);
			if (hostileFinishedSpeech)
			{
				hasInteractionToken = false;
				interactionToken = 0L;
				_pendingInteractionTimeoutArms.Remove(agentIndex);
				_activeInteractionSessions.Remove(agentIndex);
			}
		});
		RunSceneTtsPlaybackFinishedStep("clear_speaking", agentIndex, delegate
		{
			SceneAudio.MarkPlaybackFinished(agentIndex);
			lock (_ttsBubbleSyncLock)
			{
				_ttsPlaybackStartedAgents.Remove(agentIndex);
			}
			Logger.Log("LipSync", $"[OnPlaybackFinished] agentIndex={agentIndex} finalized on main thread");
			LogTtsReport("PlaybackFinished", agentIndex, $"hasInteractionToken={hasInteractionToken};interactionToken={interactionToken};hostileFinishedSpeech={hostileFinishedSpeech};mainThread=True");
		});
		if (hasInteractionToken)
		{
			RunSceneTtsPlaybackFinishedStep("arm_interaction_timeout", agentIndex, delegate
			{
				ArmActiveInteractionTimeoutNow(agentIndex, interactionToken);
			});
		}
		RunSceneTtsPlaybackFinishedStep("dialogue_feed", agentIndex, delegate
		{
			FlushPendingSceneDialogueFeedAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("follow_command", agentIndex, delegate
		{
			_sceneMovement.FlushSceneFollowCommandAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("meeting_release", agentIndex, delegate
		{
			if (FlushLordsHallMissionEntryAfterSpeech(agentIndex))
			{
				return;
			}
			FlushMeetingReleaseAfterSpeech(agentIndex);
			FlushWorldMapMissionExitAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("summon_return", agentIndex, delegate
		{
			_sceneMovement.FlushSceneSummonReturnAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("guide_return", agentIndex, delegate
		{
			_sceneMovement.FlushSceneGuideReturnAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("autonomy_restore", agentIndex, delegate
		{
			FlushSceneAutonomyRestoreAfterSpeech(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("pending_launches", agentIndex, delegate
		{
			_sceneMovement.FlushPendingSceneSummonLaunches(agentIndex);
			_sceneMovement.FlushPendingSceneGuideLaunches(agentIndex);
		});
		RunSceneTtsPlaybackFinishedStep("lipsync_cleanup", agentIndex, delegate
		{
			CleanupSceneLipSyncAfterPlaybackFinished(agentIndex);
		});
		FreezeWatchdog.Mark("SceneTts.playback_finished.main_end", "agent=" + agentIndex + " remaining=" + _mainThreadActions.Count, immediate: true);
	}
	private void RunSceneTtsPlaybackFinishedStep(string step, int agentIndex, Action action)
	{
		if (action == null)
		{
			return;
		}
		string safeStep = string.IsNullOrWhiteSpace(step) ? "step" : step.Trim();
		string markName = "SceneTts.playback_finished." + safeStep;
		Stopwatch stopwatch = Stopwatch.StartNew();
		FreezeWatchdog.Mark(markName + ".begin", "agent=" + agentIndex, immediate: true);
		try
		{
			action();
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark(markName + ".exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			Logger.Log("LipSync", "[ERROR] playback_finished step failed step=" + safeStep + " agent=" + agentIndex + " error=" + ex.Message);
			BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackFinished." + safeStep, ex, "agentIndex=" + agentIndex);
		}
		finally
		{
			stopwatch.Stop();
			double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
			FreezeWatchdog.Mark(markName + ".end", "agent=" + agentIndex + " elapsedMs=" + Math.Round(elapsedMs, 2), immediate: true);
			if (elapsedMs >= SceneMainThreadActionSlowMs)
			{
				Logger.Log("LipSync", "[WARN] playback_finished slow step step=" + safeStep + " agent=" + agentIndex + " elapsedMs=" + Math.Round(elapsedMs, 2));
			}
		}
	}
	private void HandleTtsPlaybackFailed(TtsEngine.PlaybackRequest request, string errorMessage) => SceneAudio.HandleTtsPlaybackFailed(request, errorMessage);
	private void LogTtsReport(string stage, int agentIndex, string extra = null)
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			bool active = agent != null && agent.IsActive();
			bool hostile = IsAgentHostileToMainAgent(agent);
			string agentName = (agent?.Name?.ToString() ?? "").Trim();
   SceneAudioResourceSnapshot resource=SceneAudio.GetResourceSnapshot(agentIndex);
   bool speaking=resource.Speaking, hasSe=resource.HasSound, hasWav=resource.HasWav, hasXml=resource.HasXml;
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
			Logger.LogVerbose("TTSReport", "tts_report:" + (stage ?? "") + ":" + agentIndex, () => $"[{stage}] agentIndex={agentIndex}, name={agentName}, active={active}, hostile={hostile}, speaking={speaking}, hasSe={hasSe}, hasWav={hasWav}, hasXml={hasXml}, hasInteraction={hasInteraction}, interactionToken={interactionToken}, timeoutArmed={timeoutArmed}, hasPendingArm={hasPendingArm}, pendingArmAt={pendingArmAt}, pendingBubble={hasPendingBubble}, pendingBubbleCount={pendingBubbleCount}, pendingDuration={hasPendingDuration}, pendingDurationCount={pendingDurationCount}, pendingSpeechToken={hasPendingSpeechToken}, pendingSpeechTokenCount={pendingSpeechTokenCount}, missionTime={missionTime:F2}, scene={sceneName}{extraSuffix}", 2.0);
		}
		catch (Exception ex)
		{
			Logger.Log("TTSReport", $"[{stage}] report_failed agentIndex={agentIndex}, error={ex.Message}");
		}
	}








 private bool _wasGameWindowFocused { get => SceneAudio._wasGameWindowFocused; set => SceneAudio._wasGameWindowFocused = value; }

 private bool _ttsPausedByShoutUi { get => SceneAudio._ttsPausedByShoutUi; set => SceneAudio._ttsPausedByShoutUi = value; }





}
