using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private static ExecutionLineAudio _executionLineAudio;
    private static readonly WeakReference<Mission> ExecutionAudioMission = new(null);
    private static Guid _executionAudioSession;
    private static int _executionAudioSequence;

    // Called only after Vengeance successfully showed its own bubble. No AF
    // bubble, feed, history, interaction token or postprocess is published here.
    internal static ExecutionSpeechAudioReceipt TryPlayExecutionSpeechForExternal(
        ExecutionRequest execution, int sequence, SpeechCue cue, Agent speaker)
    {
        if (!IsBannerlordMainThreadForNativeActions()) return null;
        var owner = CurrentInstance;
        var mission = Mission.Current;
        var controller = mission?.GetMissionBehavior<TownExecutionMissionBehavior>();
        if (owner == null || execution == null || cue == null || sequence <= 0 ||
            controller?.Request?.SessionId != execution.SessionId ||
            !ExecutionLineAudio.IsSpeakerCurrent(mission, controller, cue.Speaker, speaker)) return null;

        // Director sequences are monotonic across phases. One high-water mark,
        // scoped to Mission + SessionId, rejects repeat (sequence, Agent) keys
        // without retaining a set of Agents or scanning the mission every tick.
        ExecutionAudioMission.TryGetTarget(out var previousMission);
        if (ReferenceEquals(previousMission, mission) && _executionAudioSession == execution.SessionId)
        {
            if (sequence <= _executionAudioSequence) return null;
        }
        Interlocked.Exchange(ref _executionLineAudio, null)?.Cancel();
        ExecutionAudioMission.SetTarget(mission);
        _executionAudioSession = execution.SessionId;
        _executionAudioSequence = sequence;
        if (!owner.IsTtsPlaybackEnabledForShout()) return null;

        ExecutionLineAudio audio = null;
        try
        {
            string text = SanitizeSceneSpeechTextForTts(cue.Text);
            if (string.IsNullOrWhiteSpace(text)) return null;
            var character = speaker.Character as CharacterObject;
            var hero = character?.HeroObject;
            string voice = hero == null ? null : MyBehavior.GetNpcVoiceIdForExternal(hero);
            if (hero != null && string.IsNullOrWhiteSpace(voice)) voice = VoiceMapper.ResolveVoiceId(hero);
            if (string.IsNullOrWhiteSpace(voice))
                voice = VoiceMapper.ResolveVoiceIdForNonHero(speaker.IsFemale, character?.Age ?? 30f, speaker.Index);

            owner.EnsureTtsPlaybackEventsSubscribedForNativeConversation();
            audio = new ExecutionLineAudio(owner, mission, controller, execution.SessionId, sequence, cue.Speaker, speaker);
            Interlocked.Exchange(ref _executionLineAudio, audio);
            if (audio.TrySpeak(text, voice)) return audio;
            audio.Cancel();
        }
        catch (Exception exception)
        {
            audio?.Cancel();
            Logger.Log("Vengeance", "Execution audio kept its text fallback: " + exception.GetType().Name);
        }
        return null;
    }

    internal static void CancelExecutionSpeechAudioForExternal(Guid sessionId)
    {
        var audio = Volatile.Read(ref _executionLineAudio);
        if (audio == null || audio.SessionId != sessionId) return;
        Interlocked.CompareExchange(ref _executionLineAudio, null, audio);
        audio.Cancel();
    }

    // Main-thread lifecycle entry for host unregister/shutdown, and mission end
    // after the caller has validated its ending Mission identity. Call before
    // resetting SceneAudio ownership or clearing queued main-thread work.
    internal static void ResetExecutionSpeechAudioForExternal()
    {
        Interlocked.Exchange(ref _executionLineAudio, null)?.Cancel();
        ExecutionAudioMission.SetTarget(null);
        _executionAudioSession = Guid.Empty;
        _executionAudioSequence = 0;
    }

    // At most one receipt is live. Director polls only that receipt while its
    // cue runs; no new mission behavior, timer, agent scan or unbounded queue.
    private sealed class ExecutionLineAudio : ExecutionSpeechAudioReceipt
    {
        private const int MainThreadAcceptanceTimeoutMs = 2000;
        private readonly ShoutBehavior _owner;
        private readonly SceneAudioLipSyncController _audio;
        private readonly TtsEngine _tts;
        private readonly Mission _mission;
        private readonly TownExecutionMissionBehavior _controller;
        private readonly Agent _speaker;
        private readonly SpeechSpeaker _role;
        private readonly int _sequence;
        private readonly int _speakerIndex;
        private readonly long _generation;
        private readonly HashSet<long> _requestIds = new();
        private TtsEngine.PlaybackRequest _request;
        private int _closed;
        internal Guid SessionId { get; }

        internal ExecutionLineAudio(ShoutBehavior owner, Mission mission, TownExecutionMissionBehavior controller,
            Guid sessionId, int sequence, SpeechSpeaker role, Agent speaker)
        {
            _owner = owner;
            _audio = owner.SceneAudio;
            _tts = TtsEngine.Instance;
            _mission = mission;
            _controller = controller;
            SessionId = sessionId;
            _sequence = sequence;
            _role = role;
            _speaker = speaker;
            _speakerIndex = speaker.Index;
            _generation = SaveRuntimeGuard.CaptureGeneration();
        }

        internal bool TrySpeak(string text, string voice)
        {
            // The existing detached fallback is intentional: passing a scene
            // AgentIndex makes TtsEngine inspect live Agents on its worker (also
            // in streaming synthesis). It also couples receipts to unrelated
            // per-Agent shout queues. Keep audible local/cloud/streaming TTS,
            // with SceneAudio request ownership, but do not start native lipsync.
            return _tts.SpeakAsync(text, -1, -1f, -1, voice, request =>
            {
                _request = request;
                _requestIds.Add(request.RequestId);
                _audio.TrackTtsPlaybackRequest(request);
                // Subscribe before publication to the TTS worker queue.
                _tts.OnRequestAudioFileReady += OnAudioReady;
                _tts.OnRequestPlaybackStarted += BeforePlayback;
                _tts.OnRequestPlaybackFinished += OnTerminal;
                _tts.OnRequestPlaybackFailed += OnFailure;
                _tts.OnRequestPlaybackCancelled += OnTerminal;
                if (!IsCurrentOnMainThread()) Cancel();
            });
        }

        internal override bool IsPending
        {
            get
            {
                if (Volatile.Read(ref _closed) != 0) return false;
                if (IsCurrentOnMainThread()) return true;
                Cancel();
                return false;
            }
        }

        private bool IsCurrentOnMainThread()
        {
            try
            {
                return Volatile.Read(ref _closed) == 0 && _request != null && !_request.IsCancellationRequested &&
                    ReferenceEquals(Volatile.Read(ref _executionLineAudio), this) && ReferenceEquals(CurrentInstance, _owner) &&
                    SaveRuntimeGuard.IsCurrentGeneration(_generation) && ReferenceEquals(Mission.Current, _mission) &&
                    _executionAudioSession == SessionId && _executionAudioSequence == _sequence &&
                    ReferenceEquals(_mission.GetMissionBehavior<TownExecutionMissionBehavior>(), _controller) &&
                    _controller.Request?.SessionId == SessionId && _speaker.Index == _speakerIndex &&
                    IsSpeakerCurrent(_mission, _controller, _role, _speaker) &&
                    _audio.IsTtsPlaybackRequestCurrent(_request);
            }
            catch (Exception exception)
            {
                // A disposed native speaker degrades this audio request only;
                // it must not abort the already-visible text address.
                Logger.Log("Vengeance", "Execution audio owner became unavailable: " + exception.GetType().Name);
                return false;
            }
        }

        internal static bool IsSpeakerCurrent(Mission mission, TownExecutionMissionBehavior controller,
            SpeechSpeaker role, Agent speaker)
        {
            return mission != null && !mission.MissionEnded && controller != null &&
                controller.State != ExecutionSessionState.Cancelled && speaker != null &&
                ReferenceEquals(speaker.Mission, mission) && speaker.IsActive() && speaker.Health > 0f &&
                (role != SpeechSpeaker.Victim || !controller.HasReachedLethalFrame);
        }

        private void OnAudioReady(TtsEngine.PlaybackRequest request, string wav, string xml, float duration) => BeforePlayback(request);

        private void BeforePlayback(TtsEngine.PlaybackRequest request)
        {
            if (!ReferenceEquals(request, _request) || Volatile.Read(ref _closed) != 0 || request.IsCancellationRequested) return;
            // TTS events run on the worker. No Mission/Agent/Hero is read here.
            // Await one bounded main-thread acceptance before the engine can
            // continue into waveOut; every streamed chunk uses this same gate.
            var accepted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _owner._mainThreadActions.Enqueue(() =>
            {
                bool current = false;
                try { current = IsCurrentOnMainThread(); }
                finally
                {
                    if (!current) Cancel();
                    accepted.TrySetResult(current);
                }
            });
            try
            {
                if (accepted.Task.Wait(MainThreadAcceptanceTimeoutMs, request.CancellationToken) && accepted.Task.Result) return;
            }
            catch (OperationCanceledException) { }
            // A blocked main thread must not cause late speech after timeout.
            // This is request-scoped engine cancellation, never StopPlayback.
            Cancel();
        }

        private void OnTerminal(TtsEngine.PlaybackRequest request)
        {
            if (ReferenceEquals(request, _request)) Close(cancelRequest: false);
        }

        private void OnFailure(TtsEngine.PlaybackRequest request, string reason) => OnTerminal(request);

        internal override void Cancel() => Close(cancelRequest: true);

        private void Close(bool cancelRequest)
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0) return;
            // Terminal events may run on the worker, including after mission end.
            // Release the static root immediately, never through a queued callback
            // that mission teardown can discard. A late A cannot clear a new B.
            Interlocked.CompareExchange(ref _executionLineAudio, null, this);
            _tts.OnRequestAudioFileReady -= OnAudioReady;
            _tts.OnRequestPlaybackStarted -= BeforePlayback;
            _tts.OnRequestPlaybackFinished -= OnTerminal;
            _tts.OnRequestPlaybackFailed -= OnFailure;
            _tts.OnRequestPlaybackCancelled -= OnTerminal;
            try
            {
                // Natural terminal events must not cancel the engine again or
                // suppress unrelated terminal subscribers/legacy notifications.
                if (cancelRequest) _tts.CancelRequests(_requestIds, keepCurrent: false);
            }
            finally
            {
                // This method only removes entries under SceneAudio._requestLock;
                // it reads no Mission/Agent and needs no main-thread dispatch.
                // Detached requests have no per-Agent native resources to stop.
                _audio.RetireTtsPlaybackRequest(_request);
            }
        }
    }
}
