using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed class TtsEngine : IDisposable
{
	internal struct WAVEFORMATEX
	{
		public ushort wFormatTag;

		public ushort nChannels;

		public uint nSamplesPerSec;

		public uint nAvgBytesPerSec;

		public ushort nBlockAlign;

		public ushort wBitsPerSample;

		public ushort cbSize;
	}

	internal struct WAVEHDR
	{
		public IntPtr lpData;

		public uint dwBufferLength;

		public uint dwBytesRecorded;

		public IntPtr dwUser;

		public uint dwFlags;

		public uint dwLoops;

		public IntPtr lpNext;

		public IntPtr reserved;
	}

	internal sealed class PlaybackRequest
	{
		private readonly CancellationToken _generationToken;
		public long RequestId { get; }
		public int AgentIndex { get; }
		public CancellationToken CancellationToken { get; }
		public bool IsCancellationRequested => CancellationToken.IsCancellationRequested || _generationToken.IsCancellationRequested;

		internal PlaybackRequest(long requestId, int agentIndex, CancellationToken cancellationToken, CancellationToken generationToken)
		{
			RequestId = requestId;
			AgentIndex = agentIndex;
			CancellationToken = cancellationToken;
			_generationToken = generationToken;
		}
	}

	private class TtsJob
	{
		public PlaybackRequest Request;
		public CancellationTokenSource Cancellation;
		public bool BypassEnabledCheck;
		public bool Cancelled;
		public bool TerminalPublished;
		public bool HoldsQueueSlot;
		public bool ReleaseRequested;
		public bool CancellationDispatched;
		public bool CancellationDisposed;

		public string Text;

		public int SpeakerId;

		public float Speed;

		public int AgentIndex = -1;

		public string VoiceIdOverride = "";
	}

	private static readonly object _instanceLock = new object();

	private static TtsEngine _instance;

	private bool _initialized;

	private bool _disposed;

	private readonly object _initLock = new object();

	private static readonly HttpClient _httpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	private readonly BlockingCollection<TtsJob> _jobQueue = new BlockingCollection<TtsJob>(32);

	private Thread _workerThread;

	private volatile bool _stopWorker;

	private IntPtr _currentWaveOut = IntPtr.Zero;
	private PlaybackRequest _currentWaveOutRequest;

	private readonly object _playbackLock = new object();

	private readonly object _requestLock = new object();
	private readonly Dictionary<long, TtsJob> _pendingJobs = new Dictionary<long, TtsJob>();
	private CancellationTokenSource _playbackGeneration = new CancellationTokenSource();
	private static long _nextRequestId;
	private int _reservedQueueSlots;
	private TtsJob _currentJob;

	private volatile bool _pauseRequested;


	private static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	internal const uint WAVE_MAPPER = uint.MaxValue;

	internal const int MMSYSERR_NOERROR = 0;

	internal const uint CALLBACK_NULL = 0u;

	private static string _tempAudioDir;

	public static TtsEngine Instance
	{
		get
		{
			if (_instance == null)
			{
				lock (_instanceLock)
				{
					if (_instance == null)
					{
						_instance = new TtsEngine();
					}
				}
			}
			return _instance;
		}
	}

	public bool IsReady => _initialized && !_disposed && !_stopWorker;

	public event Action<int> OnPlaybackStarted;

	public event Action<int> OnPlaybackFinished;

	public event Action<int, string, string, float> OnAudioFileReady;

	public event Action<int, string> OnPlaybackFailed;

	// Existing events remain source-compatible; in-project consumers use the request-scoped events.
	public event Action<PlaybackRequest> OnRequestPlaybackStarted;
	public event Action<PlaybackRequest> OnRequestPlaybackFinished;
	public event Action<PlaybackRequest, string, string, float> OnRequestAudioFileReady;
	public event Action<PlaybackRequest, string> OnRequestPlaybackFailed;
	public event Action<PlaybackRequest> OnRequestPlaybackCancelled;

	[DllImport("winmm.dll")]
	internal static extern int waveOutOpen(out IntPtr phwo, uint uDeviceID, ref WAVEFORMATEX pwfx, IntPtr dwCallback, IntPtr dwInstance, uint fdwOpen);

	[DllImport("winmm.dll")]
	internal static extern int waveOutClose(IntPtr hwo);

	[DllImport("winmm.dll")]
	internal static extern int waveOutPrepareHeader(IntPtr hwo, ref WAVEHDR pwh, int cbwh);

	[DllImport("winmm.dll")]
	internal static extern int waveOutUnprepareHeader(IntPtr hwo, ref WAVEHDR pwh, int cbwh);

	[DllImport("winmm.dll")]
	internal static extern int waveOutWrite(IntPtr hwo, ref WAVEHDR pwh, int cbwh);

	[DllImport("winmm.dll")]
	internal static extern int waveOutReset(IntPtr hwo);

	[DllImport("winmm.dll")]
	internal static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

	[DllImport("winmm.dll")]
	internal static extern int waveOutPause(IntPtr hwo);

	[DllImport("winmm.dll")]
	internal static extern int waveOutRestart(IntPtr hwo);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	public void Initialize()
	{
		if (_initialized || _disposed)
		{
			return;
		}
		lock (_initLock)
		{
			if (_initialized || _disposed)
			{
				return;
			}
			try
			{
				_stopWorker = false;
				_workerThread = new Thread(WorkerLoop)
				{
					Name = "TtsEngine_Worker",
					IsBackground = true
				};
				_workerThread.Start();
				_initialized = true;
				Logger.Log("TtsEngine", "在线 TTS 引擎初始化成功");
			}
			catch (Exception ex)
			{
				Logger.Log("TtsEngine", "[ERROR] 初始化失败: " + ex.Message);
			}
		}
	}

	public bool SpeakAsync(string text, int speakerId = -1, float speed = -1f, int agentIndex = -1, string voiceIdOverride = null)
	{
		return SpeakAsync(text, speakerId, speed, agentIndex, voiceIdOverride, null);
	}

	public bool SpeakAsync(string text, int speakerId, float speed, int agentIndex, string voiceIdOverride, Action<PlaybackRequest> onAccepted)
	{
		if (!IsReady)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				if (!settings.EnableTtsSpeech)
				{
					return false;
				}
				if (!settings.TtsVolcDedicatedEnabled)
				{
					return false;
				}
			}
			if (speed <= 0f)
			{
				try
				{
					speed = settings?.TtsVolcDedicatedSpeed ?? 1f;
				}
				catch
				{
					speed = 1f;
				}
			}
			TtsJob item = new TtsJob
			{
				Text = text.Trim(),
				SpeakerId = speakerId,
				Speed = speed,
				AgentIndex = agentIndex,
				VoiceIdOverride = (voiceIdOverride ?? "").Trim()
			};
			if (!TryEnqueueJob(item, onAccepted))
			{
				Logger.Log("TtsEngine", "[WARN] TTS 请求未入队（已取消、已关闭或队列已满）: " + text.Substring(0, Math.Min(30, text.Length)));
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TtsEngine", "[ERROR] SpeakAsync: " + ex.Message);
			return false;
		}
	}

	public void SpeakTestAsync(string text, float speed)
	{
		if (IsReady && !string.IsNullOrWhiteSpace(text))
		{
			TtsJob item = new TtsJob
			{
				Text = text.Trim(),
				SpeakerId = 0,
				Speed = ((speed > 0f) ? speed : 1f),
				AgentIndex = -1,
				BypassEnabledCheck = true
			};
			if (!TryEnqueueJob(item, null))
			{
				Logger.Log("TtsEngine", "[WARN] TTS 队列已满，测试播放丢弃");
			}
		}
	}

	private bool TryEnqueueJob(TtsJob job, Action<PlaybackRequest> onAccepted)
	{
		lock (_requestLock)
		{
			if (!IsReady || _reservedQueueSlots >= _jobQueue.BoundedCapacity)
			{
				return false;
			}
			CancellationToken generationToken = _playbackGeneration.Token;
			job.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(generationToken);
			job.Request = new PlaybackRequest(Interlocked.Increment(ref _nextRequestId), job.AgentIndex, job.Cancellation.Token, generationToken);
			job.HoldsQueueSlot = true;
			_reservedQueueSlots++;
			_pendingJobs.Add(job.Request.RequestId, job);
		}
		bool queued = false;
		try
		{
			// Register waits before publication, without holding lifecycle locks across caller code.
			onAccepted?.Invoke(job.Request);
			lock (_requestLock)
			{
				if (IsJobCurrentLocked(job))
				{
					queued = _jobQueue.TryAdd(job, 0);
				}
			}
			return queued;
		}
		finally
		{
			if (!queued)
			{
				CancelJob(job);
				ReleaseJob(job);
			}
		}
	}

	private bool IsJobCurrentLocked(TtsJob job)
	{
		return job != null && !job.Cancelled && !job.Request.IsCancellationRequested && !_stopWorker && !_disposed
			&& _pendingJobs.ContainsKey(job.Request.RequestId);
	}

	private bool IsJobCurrent(TtsJob job)
	{
		lock (_requestLock) { return IsJobCurrentLocked(job); }
	}

	private void ReleaseJob(TtsJob job)
	{
		CancellationTokenSource dispose = null;
		lock (_requestLock)
		{
			_pendingJobs.Remove(job.Request.RequestId);
			if (job.HoldsQueueSlot)
			{
				job.HoldsQueueSlot = false;
				_reservedQueueSlots--;
			}
			if (ReferenceEquals(_currentJob, job))
			{
				_currentJob = null;
			}
			job.ReleaseRequested = true;
			if (!job.CancellationDisposed && (!job.Cancelled || job.CancellationDispatched))
			{
				job.CancellationDisposed = true;
				dispose = job.Cancellation;
			}
		}
		dispose?.Dispose();
	}

	private void CancelJob(TtsJob job)
	{
		lock (_requestLock)
		{
			if (job == null || job.Cancelled || job.ReleaseRequested) { return; }
			job.Cancelled = true;
		}
		DispatchJobCancellation(job);
	}

	private void DispatchJobCancellation(TtsJob job)
	{
		try
		{
			job.Cancellation.Cancel();
		}
		catch (Exception ex)
		{
			BannerlordExceptionSentinel.ReportObservedException("TtsEngine.CancelRequest", ex);
		}
		try
		{
			InvokePlaybackSubscribers(OnRequestPlaybackCancelled, handler => handler(job.Request), job.Request, allowCancelled: true);
		}
		catch (Exception ex)
		{
			BannerlordExceptionSentinel.ReportObservedException("TtsEngine.OnRequestPlaybackCancelled", ex);
		}
		finally
		{
			CancellationTokenSource dispose = null;
			lock (_requestLock)
			{
				job.CancellationDispatched = true;
				if (job.ReleaseRequested && !job.CancellationDisposed)
				{
					job.CancellationDisposed = true;
					dispose = job.Cancellation;
				}
			}
			dispose?.Dispose();
		}
	}

	public void StopPlayback()
	{
		CancellationTokenSource previousGeneration;
		List<TtsJob> jobs;
		List<TtsJob> drained = new List<TtsJob>();
		lock (_requestLock)
		{
			previousGeneration = _playbackGeneration;
			if (previousGeneration == null) { return; }
			_playbackGeneration = _disposed ? null : new CancellationTokenSource();
			jobs = new List<TtsJob>();
			foreach (TtsJob job in _pendingJobs.Values)
			{
				if (!job.Cancelled && !job.ReleaseRequested)
				{
					// Claim cancellation before a dequeued worker can release its CTS or skip notification.
					job.Cancelled = true;
					jobs.Add(job);
				}
			}
			while (_jobQueue.TryTake(out TtsJob queued)) { drained.Add(queued); }
			_pauseRequested = false;
		}
		// All accepted jobs, including a job between dequeue and activation, retain this generation.
		try { previousGeneration.Cancel(); }
		catch (Exception ex) { BannerlordExceptionSentinel.ReportObservedException("TtsEngine.StopPlayback", ex); }
		finally { previousGeneration.Dispose(); }
		foreach (TtsJob job in jobs) { DispatchJobCancellation(job); }
		foreach (TtsJob job in drained) { ReleaseJob(job); }
		ResetCurrentWaveOut();
	}

	private void ResetCurrentWaveOut()
	{
		lock (_playbackLock)
		{
			// A cancellation subscriber can enqueue a new job before StopPlayback finishes.
			// Never reset an audio handle now owned by that newer request.
			if (_currentWaveOut != IntPtr.Zero && _currentWaveOutRequest != null && _currentWaveOutRequest.IsCancellationRequested)
			{
				try { waveOutReset(_currentWaveOut); }
				catch { }
			}
		}
	}


	public void PausePlayback()
	{
		_pauseRequested = true;
		try
		{
			lock (_playbackLock)
			{
				if (_currentWaveOut != IntPtr.Zero)
				{
					try
					{
						waveOutPause(_currentWaveOut);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	public void ResumePlayback()
	{
		_pauseRequested = false;
		try
		{
			lock (_playbackLock)
			{
				if (_currentWaveOut != IntPtr.Zero)
				{
					try
					{
						waveOutRestart(_currentWaveOut);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

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

	private void WorkerLoop()
	{
		Logger.Log("TtsEngine", "工作线程已启动");
		try
		{
			while (!_stopWorker)
			{
				TtsJob item;
				try
				{
					if (!_jobQueue.TryTake(out item, 500)) { continue; }
				}
				catch (InvalidOperationException) { break; }
				try
				{
					lock (_requestLock)
					{
						if (!IsJobCurrentLocked(item)) { continue; }
						_currentJob = item;
						if (item.HoldsQueueSlot)
						{
							item.HoldsQueueSlot = false;
							_reservedQueueSlots--;
						}
					}
					if (IsJobCurrent(item)) { ProcessJob(item); }
				}
				catch (OperationCanceledException) when (item.Request.IsCancellationRequested) { }
				catch (Exception ex)
				{
					Logger.Log("TtsEngine", "[ERROR] ProcessJob: " + ex.Message);
					BannerlordExceptionSentinel.ReportObservedException("TtsEngine.ProcessJob", ex, "agentIndex=" + item.AgentIndex);
					NotifyPlaybackFailed(item, "TTS processing exception: " + ex.Message);
				}
				finally { ReleaseJob(item); }
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TtsEngine", "[ERROR] WorkerLoop 异常退出: " + ex.Message);
			BannerlordExceptionSentinel.ReportObservedException("TtsEngine.WorkerLoop", ex);
		}
		Logger.Log("TtsEngine", "工作线程已退出");
	}


	private static void InvokePlaybackSubscribers<TDelegate>(TDelegate subscribers, Action<TDelegate> invoke, PlaybackRequest request, bool allowCancelled = false) where TDelegate : Delegate
	{
		if (subscribers == null) { return; }
		foreach (TDelegate handler in subscribers.GetInvocationList())
		{
			if (!allowCancelled && request.IsCancellationRequested) { break; }
			try { invoke(handler); }
			catch (Exception ex) { BannerlordExceptionSentinel.ReportObservedException("TtsEngine.PlaybackSubscriber", ex); }
		}
	}

	private bool PublishJobEvent(TtsJob job, Action requestEvent, Action legacyEvent, bool terminal = false)
	{
		lock (_requestLock)
		{
			if (!IsJobCurrentLocked(job) || job.TerminalPublished) { return false; }
			if (terminal) { job.TerminalPublished = true; }
		}
		try { if (!job.Request.IsCancellationRequested) { requestEvent?.Invoke(); } }
		catch (Exception ex) { BannerlordExceptionSentinel.ReportObservedException("TtsEngine.RequestEvent", ex); }
		// Subscribers can reenter StopPlayback. Never follow that with an unscoped late event.
		try { if (!job.Request.IsCancellationRequested) { legacyEvent?.Invoke(); } }
		catch (Exception ex) { BannerlordExceptionSentinel.ReportObservedException("TtsEngine.LegacyEvent", ex); }
		return !job.Request.IsCancellationRequested;
	}

	private void NotifyPlaybackFailed(TtsJob job, string reason)
	{
		string text = string.IsNullOrWhiteSpace(reason) ? "TTS failed." : reason.Trim();
		LogTtsReport("NotifyPlaybackFailed", job?.AgentIndex ?? -1, "reason=" + text);
		PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestPlaybackFailed, handler => handler(job.Request, text), job.Request), () => InvokePlaybackSubscribers(OnPlaybackFailed, handler => handler(job.AgentIndex, text), job.Request), terminal: true);
	}

	private void NotifyPlaybackFinished(TtsJob job)
	{
		PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestPlaybackFinished, handler => handler(job.Request), job.Request), () => InvokePlaybackSubscribers(OnPlaybackFinished, handler => handler(job.AgentIndex), job.Request), terminal: true);
	}


	private void ProcessJob(TtsJob job)
	{
		if (!IsJobCurrent(job) || string.IsNullOrWhiteSpace(job.Text))
		{
			return;
		}
		LogTtsReport("ProcessJob.Start", job.AgentIndex, $"speakerId={job.SpeakerId};speed={job.Speed:F2};voiceOverride={job.VoiceIdOverride};textLen={(job.Text ?? string.Empty).Length}");
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		string text6 = "wav";
		string extraParamJson = "{}";
		int num = 24000;
		float speed = job.Speed;
		float loudnessRatio = 1f;
		float num2 = 1f;
		bool flag = true;
		bool flag2 = false;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			text = settings?.TtsVolcDedicatedApiUrl ?? "";
			text2 = settings?.TtsVolcDedicatedApiKey ?? "";
			text3 = settings?.TtsVolcDedicatedAppKey ?? "";
			text4 = settings?.TtsVolcDedicatedResourceId ?? "";
			text5 = settings?.TtsVolcDedicatedSpeaker ?? "";
			text6 = settings?.TtsVolcDedicatedAudioFormat ?? "wav";
			extraParamJson = settings?.TtsVolcDedicatedAdditionsJson ?? "{}";
			num = settings?.TtsVolcDedicatedSampleRate ?? 24000;
			num2 = settings?.TtsVolcDedicatedVolume ?? 1f;
			flag = settings?.TtsSceneUseWinmmAudible ?? true;
			flag2 = (settings == null || settings.EnableTtsSpeech) && (settings?.TtsVolcDedicatedEnabled ?? false);
		}
		catch
		{
		}
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		if (num2 > 1f)
		{
			num2 = 1f;
		}
		if (!string.IsNullOrWhiteSpace(job.VoiceIdOverride))
		{
			text5 = job.VoiceIdOverride;
		}
		if (!flag2 && !job.BypassEnabledCheck)
		{
			NotifyPlaybackFailed(job, "TTS disabled during playback; fallback to text bubble.");
			Logger.Log("TtsEngine", "[WARN] 火山专用模式未开启，跳过合成");
			return;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			NotifyPlaybackFailed(job, "TTS API URL is missing.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 API 地址未配置");
			return;
		}
		if ((text ?? "").IndexOf("/api/v3/", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			NotifyPlaybackFailed(job, "Unsupported URL for current engine. Please use V1 endpoint: https://openspeech.bytedance.com/api/v1/tts");
			Logger.Log("TtsEngine", "[WARN] 当前引擎仅支持 V1 非流式接口，请将 API 地址改为 https://openspeech.bytedance.com/api/v1/tts");
			return;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			NotifyPlaybackFailed(job, "TTS token is missing.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 Token 未配置（Authorization: Bearer;token）");
			return;
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			NotifyPlaybackFailed(job, "TTS app id is missing.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 AppID 未配置");
			return;
		}
		if (string.IsNullOrWhiteSpace(text4))
		{
			NotifyPlaybackFailed(job, "TTS resource id is missing.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 Resource ID 未配置（X-Api-Resource-Id）");
			return;
		}
		if (string.IsNullOrWhiteSpace(text5))
		{
			NotifyPlaybackFailed(job, "TTS voice type is missing.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 voice_type 未配置");
			return;
		}
		string audioEncoding = (text6 ?? "wav").Trim().ToLowerInvariant();
		if (audioEncoding != "wav" && audioEncoding != "pcm")
		{
			NotifyPlaybackFailed(job, "Unsupported TTS audio format. Only wav/pcm is supported.");
			Logger.Log("TtsEngine", "[ERROR] 当前播放器仅支持 wav/pcm，请将【火山专用音频格式】改为 wav 或 pcm");
			return;
		}
		Logger.Log("TtsEngine", $"在线合成开始: text={job.Text.Substring(0, Math.Min(50, job.Text.Length))}..., voice={text5}, override={!string.IsNullOrWhiteSpace(job.VoiceIdOverride)}");
		LogTtsReport("ProcessJob.SynthesisBegin", job.AgentIndex, $"voice={text5};encoding={audioEncoding};sampleRate={num};audible={flag}");
		if (job.AgentIndex >= 0 && !ShoutBehavior.CanAgentParticipateInSceneSpeechExternal(job.AgentIndex))
		{
			LogTtsReport("ProcessJob.AbortInvalidAgent.BeforeSynthesis", job.AgentIndex, "speakerId=" + job.SpeakerId);
			NotifyPlaybackFailed(job, "Scene speech target became unavailable before synthesis.");
			return;
		}
		byte[] array = CallVolcV1Api(text, text2, text3, text4, text5, job.Text, audioEncoding, num, speed, loudnessRatio, extraParamJson, job.Request.CancellationToken);
		if (!IsJobCurrent(job)) { return; }
		if (array == null || array.Length == 0)
		{
			NotifyPlaybackFailed(job, "TTS synthesis returned empty audio.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 API 返回空音频数据");
		}
		else
		{
			if (job.Request.IsCancellationRequested || _stopWorker)
			{
				return;
			}
			Logger.Log("TtsEngine", $"在线合成完成: {array.Length} bytes");
			LogTtsReport("ProcessJob.SynthesisReady", job.AgentIndex, $"bytes={array.Length}");
			ParseAudioData(array, audioEncoding, out var pcmData, out var sampleRate);
			if (audioEncoding == "pcm")
			{
				sampleRate = num;
			}
			if (sampleRate <= 0)
			{
				sampleRate = num;
			}
			if (pcmData == null || pcmData.Length == 0)
			{
				NotifyPlaybackFailed(job, "TTS audio parse failed.");
				Logger.Log("TtsEngine", "[WARN] 音频数据解析失败");
			}
			else
			{
				if (job.Request.IsCancellationRequested || _stopWorker)
				{
					return;
				}
				if (job.AgentIndex >= 0 && !ShoutBehavior.CanAgentParticipateInSceneSpeechExternal(job.AgentIndex))
				{
					LogTtsReport("ProcessJob.AbortInvalidAgent.BeforePlayback", job.AgentIndex, $"bytes={array.Length}");
					NotifyPlaybackFailed(job, "Scene speech target became unavailable before playback.");
					return;
				}
				float num3 = (float)pcmData.Length / ((float)sampleRate * 2f);
				bool flag3 = job.AgentIndex >= 0;
				bool nativeMapConversationTableauPlayback = !flag3 && (this.OnAudioFileReady != null || this.OnRequestAudioFileReady != null) && ShoutBehavior.ShouldUseMapConversationTableauPlaybackForNativeTtsExternal();
				bool flag4 = flag3 ? flag : !nativeMapConversationTableauPlayback;
				LogTtsReport("ProcessJob.PlaybackPrepared", job.AgentIndex, $"duration={num3:F2};sampleRate={sampleRate};sceneAgent={flag3};playAudible={flag4};mapTableau={nativeMapConversationTableauPlayback}");
				try
				{
					if (this.OnAudioFileReady != null || this.OnRequestAudioFileReady != null)
					{
						if (job.AgentIndex < 0 && !nativeMapConversationTableauPlayback)
						{
							try
							{
								PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestAudioFileReady, handler => handler(job.Request, "", "", num3), job.Request), () => InvokePlaybackSubscribers(OnAudioFileReady, handler => handler(job.AgentIndex, "", "", num3), job.Request));
								LogTtsReport("ProcessJob.OnAudioDurationReadyDispatched", job.AgentIndex, $"duration={num3:F2}");
							}
							catch (Exception ex)
							{
								BannerlordExceptionSentinel.ReportObservedException("TtsEngine.OnAudioDurationReady", ex, "agentIndex=" + job.AgentIndex);
							}
						}
						else
						{
							string tempAudioDir = GetTempAudioDir();
							string tempFileStem = $"tts_{(job.AgentIndex >= 0 ? job.AgentIndex.ToString() : "map")}_{Stopwatch.GetTimestamp()}";
							string text8 = Path.Combine(tempAudioDir, tempFileStem + ".wav");
							string text9 = Path.Combine(tempAudioDir, tempFileStem + ".xml");
							float num4 = 1f;
							if (flag3)
							{
								if (flag4)
								{
									num4 = 0f;
								}
								else
								{
									try
									{
										num4 = DuelSettings.GetSettings()?.TtsLipSyncSoundEventVolume ?? 0f;
									}
									catch
									{
										num4 = 0f;
									}
								}
							}
							if (num4 < 0f)
							{
								num4 = 0f;
							}
							if (num4 > 1f)
							{
								num4 = 1f;
							}
							byte[] pcmData2 = ScalePcm16Mono(pcmData, num4);
							SavePcmAsWav(pcmData2, sampleRate, text8);
							GenerateRhubarbXml(text9, num3);
							try
							{
								if (!PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestAudioFileReady, handler => handler(job.Request, text8, text9, num3), job.Request), () => InvokePlaybackSubscribers(OnAudioFileReady, handler => handler(job.AgentIndex, text8, text9, num3), job.Request)))
								{
									TryDeleteUnpublishedAudioFile(text8);
									TryDeleteUnpublishedAudioFile(text9);
								}
								LogTtsReport("ProcessJob.OnAudioFileReadyDispatched", job.AgentIndex, $"wav={Path.GetFileName(text8)};xml={Path.GetFileName(text9)};duration={num3:F2}");
							}
							catch (Exception ex)
							{
								BannerlordExceptionSentinel.ReportObservedException("TtsEngine.OnAudioFileReady", ex, "agentIndex=" + job.AgentIndex);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log("TtsEngine", "[WARN] Rhubarb 准备失败: " + ex.Message);
					LogTtsReport("ProcessJob.OnAudioFileReadyFailed", job.AgentIndex, "error=" + ex.Message);
				}
				try
				{
					LogTtsReport("ProcessJob.OnPlaybackStartedDispatching", job.AgentIndex);
					PublishJobEvent(job, () => InvokePlaybackSubscribers(OnRequestPlaybackStarted, handler => handler(job.Request), job.Request), () => InvokePlaybackSubscribers(OnPlaybackStarted, handler => handler(job.AgentIndex), job.Request));
					LogTtsReport("ProcessJob.OnPlaybackStartedDispatched", job.AgentIndex);
				}
				catch (Exception ex2)
				{
					LogTtsReport("ProcessJob.OnPlaybackStartedFailed", job.AgentIndex, "error=" + ex2.Message);
					BannerlordExceptionSentinel.ReportObservedException("TtsEngine.OnPlaybackStarted", ex2, "agentIndex=" + job.AgentIndex);
				}
				try
				{
					if (flag3)
					{
						if (flag4)
						{
							PlayPcmData(job, pcmData, sampleRate, num2, true);
							return;
						}
						int num5 = Math.Max(100, (int)(num3 * 1000f) + 100);
						int num6 = 0;
						bool flag5 = false;
						while (num6 < num5)
						{
							if (job.Request.IsCancellationRequested || _stopWorker)
							{
								break;
							}
							bool flag6 = !IsGameWindowFocused();
							if (_pauseRequested || flag6)
							{
								if (flag6 && !flag5)
								{
									flag5 = true;
									Logger.Log("TtsEngine", "[SCENE] focus lost, pause timing loop");
								}
								Thread.Sleep(50);
								continue;
							}
							if (flag5)
							{
								flag5 = false;
								Logger.Log("TtsEngine", "[SCENE] focus restored, resume timing loop");
							}
							Thread.Sleep(50);
							num6 += 50;
						}
					}
					else if (!flag4)
					{
						int num7 = Math.Max(100, (int)(num3 * 1000f) + 100);
						int num8 = 0;
						while (num8 < num7)
						{
							if (job.Request.IsCancellationRequested || _stopWorker)
							{
								break;
							}
							if (_pauseRequested)
							{
								Thread.Sleep(50);
								continue;
							}
							Thread.Sleep(50);
							num8 += 50;
						}
					}
					else
					{
						PlayPcmData(job, pcmData, sampleRate, num2, false);
					}
				}
				finally
				{
					try
					{
						LogTtsReport("ProcessJob.OnPlaybackFinishedDispatching", job.AgentIndex, $"cancelCurrent={job.Request.IsCancellationRequested};stopWorker={_stopWorker}");
						NotifyPlaybackFinished(job);
						LogTtsReport("ProcessJob.OnPlaybackFinishedDispatched", job.AgentIndex, $"cancelCurrent={job.Request.IsCancellationRequested};stopWorker={_stopWorker}");
					}
					catch (Exception ex3)
					{
						LogTtsReport("ProcessJob.OnPlaybackFinishedFailed", job.AgentIndex, "error=" + ex3.Message);
						BannerlordExceptionSentinel.ReportObservedException("TtsEngine.OnPlaybackFinished", ex3, "agentIndex=" + job.AgentIndex);
					}
				}
			}
		}
	}

	public bool InterruptCurrentPlaybackForAgent(int agentIndex, string reason = "")
	{
		TtsJob current;
		lock (_requestLock)
		{
			current = _currentJob;
			if (agentIndex < 0 || current == null || current.AgentIndex != agentIndex || !IsJobCurrentLocked(current)) { return false; }
			_pauseRequested = false;
		}
		CancelJob(current);
		ResetCurrentWaveOut();
		LogTtsReport("InterruptCurrentPlaybackForAgent", agentIndex, "reason=" + reason);
		return true;
	}


	private void LogTtsReport(string stage, int agentIndex, string extra = null)
	{
		try
		{
			int queueCount = 0;
			try
			{
				queueCount = _jobQueue?.Count ?? 0;
			}
			catch
			{
				queueCount = -1;
			}
			string extraSuffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;
			Logger.Log("TTSReport", $"[Engine.{stage}] agentIndex={agentIndex}, queueCount={queueCount}, ready={_initialized}, disposed={_disposed}, cancelCurrent={_currentJob?.Request.IsCancellationRequested ?? false}, pauseRequested={_pauseRequested}, stopWorker={_stopWorker}, waveOutActive={(_currentWaveOut != IntPtr.Zero)}{extraSuffix}");
		}
		catch (Exception ex)
		{
			Logger.Log("TTSReport", $"[Engine.{stage}] report_failed agentIndex={agentIndex}, error={ex.Message}");
		}
	}

	private byte[] CallVolcV1Api(string apiUrl, string token, string appId, string resourceId, string voiceType, string text, string encoding, int sampleRate, float speedRatio, float loudnessRatio, string extraParamJson, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			TtsSynthesisRequest request = new TtsSynthesisRequest(
				apiUrl,
				appId,
				resourceId,
				voiceType,
				text,
				encoding,
				sampleRate,
				speedRatio,
				loudnessRatio,
				extraParamJson);
			TtsSynthesisResult result = new LegacyVolcTtsGateway(_httpClient)
				.SynthesizeAsync(request, token, cancellationToken)
				.GetAwaiter()
				.GetResult();
			if (!result.Success)
			{
				Logger.Log("TtsEngine", "[ERROR] 火山 V1 Gateway failure: " + (result.ErrorCode ?? "unknown"));
				return null;
			}
			cancellationToken.ThrowIfCancellationRequested();
			return result.AudioBytes;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			Logger.Log("TtsEngine", "[ERROR] 火山 V1 Gateway invocation failed: " + exception.GetType().Name + ": " + exception.Message);
			return null;
		}
	}

	private static void TryDeleteUnpublishedAudioFile(string path)
	{
		try { if (!string.IsNullOrEmpty(path)) { File.Delete(path); } }
		catch (Exception ex) { BannerlordExceptionSentinel.ReportObservedException("TtsEngine.UnpublishedAudioCleanup", ex); }
	}


	private static void ParseAudioData(byte[] data, string format, out byte[] pcmData, out int sampleRate)
	{
		pcmData = null;
		sampleRate = 24000;
		if (data == null || data.Length < 16)
		{
			pcmData = data;
			return;
		}
		if (data.Length < 12 || data[0] != 82 || data[1] != 73 || data[2] != 70 || data[3] != 70)
		{
			pcmData = data;
			return;
		}
		try
		{
			ushort num = 1;
			ushort val = 1;
			ushort val2 = 16;
			int num2 = -1;
			int num3 = 0;
			int num4 = 12;
			while (num4 <= data.Length - 8)
			{
				string text = Encoding.ASCII.GetString(data, num4, 4);
				int num5 = BitConverter.ToInt32(data, num4 + 4);
				if (num5 < 0)
				{
					break;
				}
				int num6 = num4 + 8;
				if (num6 > data.Length)
				{
					break;
				}
				if (text == "fmt " && num5 >= 16 && num6 + 16 <= data.Length)
				{
					num = BitConverter.ToUInt16(data, num6);
					val = BitConverter.ToUInt16(data, num6 + 2);
					sampleRate = BitConverter.ToInt32(data, num6 + 4);
					val2 = BitConverter.ToUInt16(data, num6 + 14);
				}
				else if (text == "data")
				{
					num2 = num6;
					num3 = Math.Min(num5, data.Length - num6);
					break;
				}
				num4 = num6 + num5;
				if ((num5 & 1) != 0)
				{
					num4++;
				}
			}
			if (num2 < 0 || num3 <= 0)
			{
				if (data.Length > 44)
				{
					pcmData = new byte[data.Length - 44];
					Buffer.BlockCopy(data, 44, pcmData, 0, pcmData.Length);
				}
				else
				{
					pcmData = data;
				}
				return;
			}
			int num7 = Math.Max(1, (int)val);
			int num8 = Math.Max(8, (int)val2);
			int num9 = Math.Max(1, num8 / 8);
			int num10 = num7 * num9;
			if (num10 <= 0)
			{
				num10 = 2;
			}
			int num11 = num3 / num10;
			if (num11 <= 0)
			{
				pcmData = null;
				return;
			}
			pcmData = new byte[num11 * 2];
			if (num == 1 && num8 == 16)
			{
				for (int i = 0; i < num11; i++)
				{
					int num12 = num2 + i * num10;
					int num13 = 0;
					for (int j = 0; j < num7; j++)
					{
						int startIndex = num12 + j * 2;
						short num14 = BitConverter.ToInt16(data, startIndex);
						num13 += num14;
					}
					short num15 = (short)(num13 / num7);
					pcmData[i * 2] = (byte)(num15 & 0xFF);
					pcmData[i * 2 + 1] = (byte)((num15 >> 8) & 0xFF);
				}
			}
			else
			{
				Logger.Log("TtsEngine", $"[WARN] 未识别 WAV 编码(formatTag={num}, bits={num8}, ch={num7})，按原始 data 区回退");
				pcmData = new byte[num3];
				Buffer.BlockCopy(data, num2, pcmData, 0, num3);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TtsEngine", "[WARN] WAV 解析异常，回退原始数据: " + ex.Message);
			if (data.Length > 44)
			{
				pcmData = new byte[data.Length - 44];
				Buffer.BlockCopy(data, 44, pcmData, 0, pcmData.Length);
			}
			else
			{
				pcmData = data;
			}
		}
	}

	private void PlayPcmData(TtsJob job, byte[] pcmData, int sampleRate, float volume, bool autoPauseOnFocusLoss)
	{
		if (job.Request.IsCancellationRequested || pcmData == null || pcmData.Length == 0)
		{
			return;
		}
		WAVEFORMATEX pwfx = default(WAVEFORMATEX);
		pwfx.wFormatTag = 1;
		pwfx.nChannels = 1;
		pwfx.nSamplesPerSec = (uint)sampleRate;
		pwfx.wBitsPerSample = 16;
		pwfx.nBlockAlign = (ushort)(pwfx.nChannels * pwfx.wBitsPerSample / 8);
		pwfx.nAvgBytesPerSec = pwfx.nSamplesPerSec * pwfx.nBlockAlign;
		pwfx.cbSize = 0;
		int num = waveOutOpen(out var phwo, uint.MaxValue, ref pwfx, IntPtr.Zero, IntPtr.Zero, 0u);
		if (num != 0)
		{
			Logger.Log("TtsEngine", $"[ERROR] waveOutOpen 失败, result={num}");
			return;
		}
		lock (_playbackLock)
		{
			_currentWaveOut = phwo;
			_currentWaveOutRequest = job.Request;
		}
		try
		{
			waveOutSetVolume(phwo, ToWaveOutVolume(volume));
		}
		catch
		{
		}
		GCHandle gCHandle = GCHandle.Alloc(pcmData, GCHandleType.Pinned);
		try
		{
			WAVEHDR pwh = new WAVEHDR
			{
				lpData = gCHandle.AddrOfPinnedObject(),
				dwBufferLength = (uint)pcmData.Length,
				dwFlags = 0u,
				dwLoops = 0u
			};
			int cbwh = Marshal.SizeOf(typeof(WAVEHDR));
			num = waveOutPrepareHeader(phwo, ref pwh, cbwh);
			if (num != 0)
			{
				Logger.Log("TtsEngine", $"[ERROR] waveOutPrepareHeader 失败, result={num}");
				return;
			}
			lock (_playbackLock)
			{
				if (job.Request.IsCancellationRequested || _stopWorker)
				{
					waveOutUnprepareHeader(phwo, ref pwh, cbwh);
					return;
				}
				num = waveOutWrite(phwo, ref pwh, cbwh);
			}
			if (num != 0)
			{
				Logger.Log("TtsEngine", $"[ERROR] waveOutWrite 失败, result={num}");
				waveOutUnprepareHeader(phwo, ref pwh, cbwh);
				return;
			}
			int num2 = (int)((double)pcmData.Length / (double)pwfx.nAvgBytesPerSec * 1000.0) + 500;
			int num3 = 0;
			bool flag = false;
			bool flag2 = false;
			while (num3 < num2)
			{
				if (job.Request.IsCancellationRequested || _stopWorker)
				{
					break;
				}
				bool flag3 = autoPauseOnFocusLoss && !IsGameWindowFocused();
				bool flag4 = _pauseRequested || flag3;
				if (flag4)
				{
					if (!flag)
					{
						try
						{
							waveOutPause(phwo);
						}
						catch
						{
						}
						flag = true;
					}
					if (flag3 && !flag2)
					{
						flag2 = true;
						Logger.Log("TtsEngine", "[SCENE] focus lost, waveOut paused");
					}
					Thread.Sleep(50);
					continue;
				}
				if (flag)
				{
					try
					{
						waveOutRestart(phwo);
					}
					catch
					{
					}
					flag = false;
					if (flag2)
					{
						flag2 = false;
						Logger.Log("TtsEngine", "[SCENE] focus restored, waveOut resumed");
					}
				}
				Thread.Sleep(50);
				num3 += 50;
				if (num3 >= num2 - 200)
				{
					break;
				}
			}
			if (job.Request.IsCancellationRequested || _stopWorker)
			{
				waveOutReset(phwo);
			}
			waveOutUnprepareHeader(phwo, ref pwh, cbwh);
		}
		finally
		{
			gCHandle.Free();
			lock (_playbackLock)
			{
				_currentWaveOut = IntPtr.Zero;
				_currentWaveOutRequest = null;
			}
			waveOutClose(phwo);
		}
	}

	private static uint ToWaveOutVolume(float volume)
	{
		float num = volume;
		if (float.IsNaN(num) || float.IsInfinity(num))
		{
			num = 1f;
		}
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		ushort num2 = (ushort)Math.Round(num * 65535f);
		return (uint)(num2 | (num2 << 16));
	}

	private static string GetTempAudioDir()
	{
		if (_tempAudioDir == null)
		{
			_tempAudioDir = Path.Combine(Path.GetTempPath(), "AnimusForge_TtsAudio");
		}
		if (!Directory.Exists(_tempAudioDir))
		{
			Directory.CreateDirectory(_tempAudioDir);
		}
		return _tempAudioDir;
	}

	private static byte[] ScalePcm16Mono(byte[] pcmData, float gain)
	{
		if (pcmData == null || pcmData.Length == 0)
		{
			return pcmData;
		}
		if (gain >= 0.999f && gain <= 1.001f)
		{
			return pcmData;
		}
		if (gain <= 0f)
		{
			return new byte[pcmData.Length];
		}
		byte[] array = new byte[pcmData.Length];
		int num = pcmData.Length - pcmData.Length % 2;
		for (int i = 0; i < num; i += 2)
		{
			short num2 = (short)(pcmData[i] | (pcmData[i + 1] << 8));
			int num3 = (int)Math.Round((float)num2 * gain);
			if (num3 > 32767)
			{
				num3 = 32767;
			}
			if (num3 < -32768)
			{
				num3 = -32768;
			}
			short num4 = (short)num3;
			array[i] = (byte)(num4 & 0xFF);
			array[i + 1] = (byte)((num4 >> 8) & 0xFF);
		}
		if ((pcmData.Length & 1) != 0)
		{
			array[pcmData.Length - 1] = pcmData[pcmData.Length - 1];
		}
		return array;
	}

	private static void SavePcmAsWav(byte[] pcmData, int sampleRate, string filePath)
	{
		using FileStream output = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		using BinaryWriter binaryWriter = new BinaryWriter(output);
		int num = pcmData.Length;
		binaryWriter.Write(new char[4] { 'R', 'I', 'F', 'F' });
		binaryWriter.Write(36 + num);
		binaryWriter.Write(new char[4] { 'W', 'A', 'V', 'E' });
		binaryWriter.Write(new char[4] { 'f', 'm', 't', ' ' });
		binaryWriter.Write(16);
		binaryWriter.Write((short)1);
		binaryWriter.Write((short)1);
		binaryWriter.Write(sampleRate);
		binaryWriter.Write(sampleRate * 2);
		binaryWriter.Write((short)2);
		binaryWriter.Write((short)16);
		binaryWriter.Write(new char[4] { 'd', 'a', 't', 'a' });
		binaryWriter.Write(num);
		binaryWriter.Write(pcmData);
	}

	private static void GenerateRhubarbXml(string xmlPath, float durationSecs)
	{
		char[] array = new char[10] { 'B', 'C', 'D', 'C', 'B', 'E', 'D', 'F', 'C', 'B' };
		float num = 0.12f;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
		stringBuilder.AppendLine("<rhubarbResult>");
		stringBuilder.AppendLine("  <metadata>");
		stringBuilder.AppendLine($"    <duration>{durationSecs:F4}</duration>");
		stringBuilder.AppendLine("  </metadata>");
		stringBuilder.AppendLine("  <mouthCues>");
		float num2 = 0f;
		int num3 = 0;
		while (num2 < durationSecs)
		{
			char c = array[num3 % array.Length];
			float num4 = Math.Min(num2 + num, durationSecs);
			stringBuilder.AppendLine($"    <mouthCue start=\"{num2:F4}\" end=\"{num4:F4}\">{c}</mouthCue>");
			num2 = num4;
			num3++;
		}
		if (num2 < durationSecs + 0.01f)
		{
			stringBuilder.AppendLine($"    <mouthCue start=\"{durationSecs:F4}\" end=\"{durationSecs:F4}\">A</mouthCue>");
		}
		stringBuilder.AppendLine("  </mouthCues>");
		stringBuilder.AppendLine("</rhubarbResult>");
		File.WriteAllText(xmlPath, stringBuilder.ToString(), Encoding.UTF8);
	}

	public void Dispose()
	{
		lock (_initLock)
		{
			lock (_requestLock)
			{
				if (_disposed) { return; }
				_disposed = true;
				_stopWorker = true;
				_initialized = false;
			}
		}
		// Cancellation events are caller code and may reenter Initialize/Dispose from any thread.
		StopPlayback();
		try { _jobQueue.CompleteAdding(); }
		catch (InvalidOperationException) { }
		if (_workerThread != null && _workerThread != Thread.CurrentThread && _workerThread.IsAlive)
		{
			_workerThread.Join(3000);
		}
		Logger.Log("TtsEngine", "TTS 引擎已释放");
	}
}
