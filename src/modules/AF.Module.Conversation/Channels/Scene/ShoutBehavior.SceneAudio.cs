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
 private SceneSpeechOutputPort _sceneSpeechOutput;
 private SceneSpeechOutputPort SceneSpeechOutput => _sceneSpeechOutput ??= new SceneSpeechOutputPort
 {
  SanitizeUiText = SanitizeSceneSpeechText, SanitizeTtsText = SanitizeSceneSpeechTextForTts,
  BuildPatienceBadge = BuildPatienceBadgeForNpc, NpcDisplayName = GetSceneNpcHistoryNameForPrompt,
  IsHostile = IsAgentHostileToMainAgent, IsTtsEnabled = IsTtsPlaybackEnabledForShout, CanLipSync = CanAgentUseSceneLipSync,
  ResolveHero = ResolveHeroFromAgentIndex, ExternalHeroVoice = MyBehavior.GetNpcVoiceIdForExternal,
  EstimateTypingDuration = EstimateBubbleTypingDurationSeconds, Audio = () => SceneAudio,
  RemoveHostileInteraction = RemoveHostileSpeechInteraction, CaptureInteractionToken = CaptureSpeechInteractionToken,
  Report = LogTtsReport, ClearPendingBubble = ClearPendingTtsBubbleSyncForAgent,
  ClearPendingFeed = ClearPendingSceneDialogueFeedForAgent, EnqueueCompletionToken = EnqueuePendingSpeechCompletionToken,
  EnqueueBubble = EnqueuePendingNpcBubble, ScheduleFeed = ScheduleNpcSpeechToMessageFeed,
  ShowBubble = TryShowNpcBubble, ArmInteractionTimeout = ScheduleInteractionTimeoutArm,
 };
private void RemoveHostileSpeechInteraction(int agentIndex) => _j17SceneInteractionLifecycleController.RemoveHostileSpeechInteraction(agentIndex);
private long CaptureSpeechInteractionToken(int agentIndex) => _j17SceneInteractionLifecycleController.CaptureSpeechInteractionToken(agentIndex);
 private SceneAudioLipSyncController _sceneAudio;
 private SceneAudioLipSyncController SceneAudio => _sceneAudio ??= new SceneAudioLipSyncController(new SceneAudioLipSyncPort
 {
  SceneSessionId = () => Volatile.Read(ref SceneConversationHistoryOwner.SessionId), ConversationEpoch = () => _sceneConversationEpoch,
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
private SceneAudioFailureOutput ResolveSceneAudioFailureOutput(int index) => _j17SceneSpeechOutputQueueController.ResolveSceneAudioFailureOutput(index);
 private void ResetSceneAudioRequestOwnership() => SceneAudio.ResetRequestOwnership();
private void ClearSceneAudioPendingForAgent(int index) => _j17SceneSpeechOutputQueueController.ClearSceneAudioPendingForAgent(index);
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
	private void HandleTtsPlaybackCancelled(TtsEngine.PlaybackRequest request) => _j17NativeConversationPlaybackWaitAdapter.HandleTtsPlaybackCancelled(request);
private void PrepareInteractionCompletion(int agentIndex) => _j17SceneInteractionLifecycleController.PrepareInteractionCompletion(agentIndex);

	private void HandleSceneTtsPlaybackFinishedOnMainThread(int agentIndex) => _sceneSpeechCompletion.Complete(agentIndex);
private void RunSceneTtsPlaybackFinishedStep(string step, int agentIndex, Action action) => _j17SceneSpeechFollowupController.RunSceneTtsPlaybackFinishedStep(step, agentIndex, action);
	private void HandleTtsPlaybackFailed(TtsEngine.PlaybackRequest request, string errorMessage) => SceneAudio.HandleTtsPlaybackFailed(request, errorMessage);
	private void LogTtsReport(string stage, int agentIndex, string extra = null) => _j17NativeConversationSpeechAdapter.LogTtsReport(stage, agentIndex, extra);








 private bool _wasGameWindowFocused { get => SceneAudio._wasGameWindowFocused; set => SceneAudio._wasGameWindowFocused = value; }

 private bool _ttsPausedByShoutUi { get => SceneAudio._ttsPausedByShoutUi; set => SceneAudio._ttsPausedByShoutUi = value; }





}
