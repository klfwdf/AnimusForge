using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed partial class TtsEngine
{
    private sealed class AudioSegment
    {
        internal byte[] Pcm;
        internal int Rate;
        internal long? Samples;
    }

    private void ProcessStreamingJob(TtsJob job)
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(job.Request.CancellationToken);
        // The transport enforces 32 MiB total; even 8 kHz / 2s segments fit below this item cap.
        using var segments = new BlockingCollection<AudioSegment>(2048);
        ProviderSnapshot config = job.Settings;
        LogTtsReport("Streaming.Request", job.AgentIndex, "provider=OpenAI-compatible;stream=true;response_format=wav");
        Task<TtsSynthesisResult> producer = Task.Run(async () =>
        {
            try
            {
                void Publish(byte[] pcm, int rate, long? samples)
                {
                    cancel.Token.ThrowIfCancellationRequested();
                    segments.Add(new AudioSegment { Pcm = pcm, Rate = rate, Samples = samples }, cancel.Token);
                }
                return await new OpenAiCompatibleTtsGateway(_openAiHttpClient).StreamAsync(
                    new OpenAiTtsSynthesisRequest(config.Url, config.Model, config.Voice, job.Text, job.Speed, config.TimeoutSeconds), config.Credential, Publish, cancel.Token).ConfigureAwait(false);
            }
            finally { segments.CompleteAdding(); }
        }, cancel.Token);
        bool started = false;
        NativeSegmentOutput output = null;
        try
        {
            job.Request.StreamingPlayback = true;
            job.Request.StreamingDurationSeconds = Math.Max(1f, job.Text.Length * 0.15f);
            foreach (AudioSegment segment in segments.GetConsumingEnumerable(job.Request.CancellationToken))
            {
                if (!IsJobCurrent(job)) return;
                if (producer.IsCompleted)
                {
                    TtsSynthesisResult completed = producer.GetAwaiter().GetResult();
                    if (!completed.Success) { NotifyPlaybackFailed(job, completed.ErrorCode); return; }
                }
                if (segment.Samples.HasValue) job.Request.StreamingDurationSeconds = (float)segment.Samples.Value / segment.Rate;
                float duration = (float)segment.Pcm.Length / (segment.Rate * 2);
                bool scene = job.AgentIndex >= 0;
                bool map = !scene && (OnRequestAudioFileReady != null || OnAudioFileReady != null) && ShoutBehavior.ShouldUseMapConversationTableauPlaybackForNativeTtsExternal();
                string wav = "", xml = "";
                if ((scene || map) && (OnRequestAudioFileReady != null || OnAudioFileReady != null))
                {
                    string stem = Path.Combine(GetTempAudioDir(), "tts_stream_" + job.Request.RequestId + "_" + Guid.NewGuid().ToString("N"));
                    wav = stem + ".wav"; xml = stem + ".xml";
                    // Scene native audio remains the existing muted lip carrier when winmm is selected.
                    float gain = scene && config.SceneWinmm ? 0f : scene ? DuelSettings.GetSettings()?.TtsLipSyncSoundEventVolume ?? 0f : 1f;
                    SavePcmAsWav(ScalePcm16Mono(segment.Pcm, gain), segment.Rate, wav);
                    GenerateRhubarbXml(xml, duration);
                }
                try
                {
                    bool accepted = PublishJobEvent(job,
                        () => InvokePlaybackSubscribers(OnRequestAudioFileReady, h => h(job.Request, wav, xml, duration), job.Request),
                        () => { if (!started) InvokePlaybackSubscribers(OnAudioFileReady, h => h(job.AgentIndex, wav, xml, job.Request.StreamingDurationSeconds), job.Request); });
                    if (!accepted) { TryDeleteUnpublishedAudioFile(wav); TryDeleteUnpublishedAudioFile(xml); return; }
                }
                catch { TryDeleteUnpublishedAudioFile(wav); TryDeleteUnpublishedAudioFile(xml); throw; }
                if (!started)
                {
                    started = true;
                    PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestPlaybackStarted, h => h(job.Request), job.Request),
                        () => InvokePlaybackSubscribers(OnPlaybackStarted, h => h(job.AgentIndex), job.Request));
                    LogTtsReport("Streaming.FirstPlayback", job.AgentIndex, "rate=" + segment.Rate);
                }
                if ((scene && !config.SceneWinmm) || map)
                {
                    // These paths are audible through the original SoundEvent / tableau consumer.
                    int elapsed = 0, milliseconds = (int)Math.Ceiling(duration * 1000);
                    while (elapsed < milliseconds)
                    {
                        job.Request.CancellationToken.ThrowIfCancellationRequested();
                        int wait = Math.Min(50, milliseconds - elapsed);
                        if (!_pauseRequested && (!scene || IsGameWindowFocused())) elapsed += wait;
                        job.Request.CancellationToken.WaitHandle.WaitOne(wait);
                    }
                }
                else
                {
                    output ??= new NativeSegmentOutput(this, job, segment.Rate, config.Volume);
                    output.Write(segment.Pcm, scene);
                }
            }
            TtsSynthesisResult result = producer.GetAwaiter().GetResult();
            if (!IsJobCurrent(job)) return;
            if (result.Success && started) NotifyPlaybackFinished(job);
            else NotifyPlaybackFailed(job, result.Success ? "tts_audio_empty" : result.ErrorCode);
        }
        catch (OperationCanceledException) when (job.Request.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            Logger.Log("TtsEngine", "Streaming playback failed: " + exception.GetType().Name);
            NotifyPlaybackFailed(job, exception is TtsStreamException streamError ? streamError.Code : "tts_stream_playback_failed");
        }
        finally
        {
            cancel.Cancel();
            output?.Dispose();
            try { producer.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
            catch (Exception exception) { Logger.Log("TtsEngine", "Streaming producer closed: " + exception.GetType().Name); }
        }
    }

    // Persistent device, stable pinned PCM/native headers, event-driven completion.
    // No speech-duration guess and no former extra 300ms delay between segments.
    private sealed class NativeSegmentOutput : IDisposable
    {
        private readonly TtsEngine _owner;
        private readonly TtsJob _job;
        private readonly AutoResetEvent _done = new AutoResetEvent(false);
        private IntPtr _handle;
        private static readonly int HeaderSize = Marshal.SizeOf(typeof(WAVEHDR));
        private static readonly int FlagsOffset = Marshal.OffsetOf(typeof(WAVEHDR), nameof(WAVEHDR.dwFlags)).ToInt32();
        internal NativeSegmentOutput(TtsEngine owner, TtsJob job, int rate, float volume)
        {
            _owner = owner; _job = job;
            WAVEFORMATEX format = new WAVEFORMATEX { wFormatTag = 1, nChannels = 1, nSamplesPerSec = (uint)rate,
                nAvgBytesPerSec = (uint)rate * 2, nBlockAlign = 2, wBitsPerSample = 16 };
            int code = waveOutOpen(out _handle, WAVE_MAPPER, ref format, _done.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 0x50000);
            if (code != 0) { _done.Dispose(); throw new IOException("waveOutOpen:" + code); }
            lock (owner._playbackLock) { owner._currentWaveOut = _handle; owner._currentWaveOutRequest = job.Request; }
            waveOutSetVolume(_handle, ToWaveOutVolume(volume));
        }
        internal void Write(byte[] pcm, bool focusPause)
        {
            GCHandle pin = GCHandle.Alloc(pcm, GCHandleType.Pinned);
            IntPtr header = Marshal.AllocHGlobal(HeaderSize);
            bool prepared = false;
            try
            {
                Marshal.StructureToPtr(new WAVEHDR { lpData = pin.AddrOfPinnedObject(), dwBufferLength = (uint)pcm.Length }, header, false);
                int code = PrepareStreamingHeader(_handle, header, HeaderSize);
                if (code != 0) throw new IOException("waveOutPrepareHeader:" + code);
                prepared = true;
                lock (_owner._playbackLock)
                {
                    _job.Request.CancellationToken.ThrowIfCancellationRequested();
                    code = WriteStreamingHeader(_handle, header, HeaderSize);
                }
                if (code != 0) throw new IOException("waveOutWrite:" + code);
                bool paused = false;
                WaitHandle[] waits = { _done, _job.Request.CancellationToken.WaitHandle };
                while ((Marshal.ReadInt32(header, FlagsOffset) & 1) == 0)
                {
                    _job.Request.CancellationToken.ThrowIfCancellationRequested();
                    bool pause = _owner._pauseRequested || (focusPause && !IsGameWindowFocused());
                    if (pause != paused) { if (pause) waveOutPause(_handle); else waveOutRestart(_handle); paused = pause; }
                    WaitHandle.WaitAny(waits, 50);
                }
            }
            finally
            {
                if (prepared) { if ((Marshal.ReadInt32(header, FlagsOffset) & 1) == 0) waveOutReset(_handle); UnprepareStreamingHeader(_handle, header, HeaderSize); }
                Marshal.FreeHGlobal(header); pin.Free();
            }
        }
        public void Dispose()
        {
            lock (_owner._playbackLock)
            {
                if (_owner._currentWaveOut == _handle) { _owner._currentWaveOut = IntPtr.Zero; _owner._currentWaveOutRequest = null; }
                if (_handle != IntPtr.Zero) { waveOutReset(_handle); waveOutClose(_handle); _handle = IntPtr.Zero; }
            }
            _done.Dispose();
        }
    }
    [DllImport("winmm.dll", EntryPoint = "waveOutPrepareHeader")]
    private static extern int PrepareStreamingHeader(IntPtr handle, IntPtr header, int size);
    [DllImport("winmm.dll", EntryPoint = "waveOutWrite")]
    private static extern int WriteStreamingHeader(IntPtr handle, IntPtr header, int size);
    [DllImport("winmm.dll", EntryPoint = "waveOutUnprepareHeader")]
    private static extern int UnprepareStreamingHeader(IntPtr handle, IntPtr header, int size);
}
