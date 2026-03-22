using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

	private class TtsJob
	{
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

	private readonly object _playbackLock = new object();

	private volatile bool _cancelCurrent;

	private volatile bool _bypassEnabledCheck;

	private volatile bool _pauseRequested;

	private volatile int _currentAgentIndex = -1;

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

	public bool IsReady => _initialized;

	public event Action<int> OnPlaybackStarted;

	public event Action<int> OnPlaybackFinished;

	public event Action<int, string, string, float> OnAudioFileReady;

	public event Action<int, string> OnPlaybackFailed;

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
		if (_initialized)
		{
			return;
		}
		lock (_initLock)
		{
			if (_initialized)
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
			if (!_jobQueue.TryAdd(item, 0))
			{
				Logger.Log("TtsEngine", "[WARN] TTS 队列已满，丢弃: " + text.Substring(0, Math.Min(30, text.Length)));
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
				AgentIndex = -1
			};
			_bypassEnabledCheck = true;
			if (!_jobQueue.TryAdd(item, 0))
			{
				_bypassEnabledCheck = false;
				Logger.Log("TtsEngine", "[WARN] TTS 队列已满，测试播放丢弃");
			}
		}
	}

	public void StopPlayback()
	{
		try
		{
			TtsJob item;
			while (_jobQueue.TryTake(out item))
			{
			}
			_cancelCurrent = true;
			_pauseRequested = false;
			lock (_playbackLock)
			{
				if (_currentWaveOut != IntPtr.Zero)
				{
					try
					{
						waveOutReset(_currentWaveOut);
						return;
					}
					catch
					{
						return;
					}
				}
			}
		}
		catch
		{
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
					if (!_jobQueue.TryTake(out item, 500))
					{
						continue;
					}
				}
				catch (InvalidOperationException)
				{
					break;
				}
				if (_stopWorker)
				{
					break;
				}
				if (_bypassEnabledCheck)
				{
					_bypassEnabledCheck = false;
				}
				else
				{
					try
					{
						DuelSettings settings = DuelSettings.GetSettings();
						if (settings != null && (!settings.EnableTtsSpeech || !settings.TtsVolcDedicatedEnabled))
						{
							continue;
						}
					}
					catch
					{
					}
				}
				_cancelCurrent = false;
				try
				{
					ProcessJob(item);
				}
				catch (Exception ex2)
				{
					Logger.Log("TtsEngine", "[ERROR] ProcessJob: " + ex2.Message);
					NotifyPlaybackFailed(item, "TTS processing exception: " + ex2.Message);
				}
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("TtsEngine", "[ERROR] WorkerLoop 异常退出: " + ex3.Message);
		}
		Logger.Log("TtsEngine", "工作线程已退出");
	}

	private void NotifyPlaybackFailed(TtsJob job, string reason)
	{
		string text = string.IsNullOrWhiteSpace(reason) ? "TTS failed." : reason.Trim();
		LogTtsReport("NotifyPlaybackFailed", job?.AgentIndex ?? (-1), $"reason={text}");
		try
		{
			this.OnPlaybackFailed?.Invoke(job?.AgentIndex ?? -1, text);
		}
		catch
		{
		}
	}

	private void ProcessJob(TtsJob job)
	{
		if (string.IsNullOrWhiteSpace(job.Text))
		{
			return;
		}
		_currentAgentIndex = job.AgentIndex;
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
		if (!flag2)
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
			_currentAgentIndex = -1;
			return;
		}
		byte[] array = CallVolcV1Api(text, text2, text3, text4, text5, job.Text, audioEncoding, num, speed, loudnessRatio, extraParamJson);
		if (array == null || array.Length == 0)
		{
			NotifyPlaybackFailed(job, "TTS synthesis returned empty audio.");
			Logger.Log("TtsEngine", "[WARN] 火山 V1 API 返回空音频数据");
		}
		else
		{
			if (_cancelCurrent || _stopWorker)
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
				if (_cancelCurrent || _stopWorker)
				{
					return;
				}
				if (job.AgentIndex >= 0 && !ShoutBehavior.CanAgentParticipateInSceneSpeechExternal(job.AgentIndex))
				{
					LogTtsReport("ProcessJob.AbortInvalidAgent.BeforePlayback", job.AgentIndex, $"bytes={array.Length}");
					_currentAgentIndex = -1;
					return;
				}
				float num3 = (float)pcmData.Length / ((float)sampleRate * 2f);
				bool flag3 = job.AgentIndex >= 0;
				bool flag4 = !flag3 || flag;
				LogTtsReport("ProcessJob.PlaybackPrepared", job.AgentIndex, $"duration={num3:F2};sampleRate={sampleRate};sceneAgent={flag3};playAudible={flag4}");
				try
				{
					if (job.AgentIndex >= 0 && this.OnAudioFileReady != null)
					{
						string tempAudioDir = GetTempAudioDir();
							string tempFileStem = $"tts_{job.AgentIndex}_{Stopwatch.GetTimestamp()}";
							string text8 = Path.Combine(tempAudioDir, tempFileStem + ".wav");
							string text9 = Path.Combine(tempAudioDir, tempFileStem + ".xml");
						float num4 = 1f;
						if (flag3 && flag4)
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
							this.OnAudioFileReady(job.AgentIndex, text8, text9, num3);
							LogTtsReport("ProcessJob.OnAudioFileReadyDispatched", job.AgentIndex, $"wav={Path.GetFileName(text8)};xml={Path.GetFileName(text9)};duration={num3:F2}");
						}
						catch
						{
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log("TtsEngine", "[WARN] Rhubarb 准备失败: " + ex.Message);
				}
				try
				{
					this.OnPlaybackStarted?.Invoke(job.AgentIndex);
					LogTtsReport("ProcessJob.OnPlaybackStartedDispatched", job.AgentIndex);
				}
				catch
				{
				}
				try
				{
					if (flag3)
					{
						if (flag4)
						{
							PlayPcmData(pcmData, sampleRate, num2, true);
							return;
						}
						int num5 = Math.Max(100, (int)(num3 * 1000f) + 100);
						int num6 = 0;
						bool flag5 = false;
						while (num6 < num5)
						{
							if (_cancelCurrent)
							{
								break;
							}
							if (_stopWorker)
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
					else
					{
						PlayPcmData(pcmData, sampleRate, num2, false);
					}
				}
				finally
				{
					try
					{
						this.OnPlaybackFinished?.Invoke(job.AgentIndex);
						LogTtsReport("ProcessJob.OnPlaybackFinishedDispatched", job.AgentIndex, $"cancelCurrent={_cancelCurrent};stopWorker={_stopWorker}");
					}
					catch
					{
					}
					finally
					{
						_currentAgentIndex = -1;
					}
				}
			}
		}
	}

	public bool InterruptCurrentPlaybackForAgent(int agentIndex, string reason = "")
	{
		if (agentIndex < 0)
		{
			return false;
		}
		if (_currentAgentIndex != agentIndex)
		{
			LogTtsReport("InterruptCurrentPlaybackForAgent.Skip", agentIndex, "reason=" + reason + ";currentAgentIndex=" + _currentAgentIndex);
			return false;
		}
		try
		{
			_cancelCurrent = true;
			_pauseRequested = false;
			lock (_playbackLock)
			{
				if (_currentWaveOut != IntPtr.Zero)
				{
					try
					{
						waveOutReset(_currentWaveOut);
					}
					catch
					{
					}
				}
			}
			LogTtsReport("InterruptCurrentPlaybackForAgent", agentIndex, "reason=" + reason);
			return true;
		}
		catch (Exception ex)
		{
			LogTtsReport("InterruptCurrentPlaybackForAgent.Failed", agentIndex, "reason=" + reason + ";error=" + ex.Message);
			return false;
		}
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
			Logger.Log("TTSReport", $"[Engine.{stage}] agentIndex={agentIndex}, queueCount={queueCount}, ready={_initialized}, disposed={_disposed}, cancelCurrent={_cancelCurrent}, pauseRequested={_pauseRequested}, stopWorker={_stopWorker}, waveOutActive={(_currentWaveOut != IntPtr.Zero)}{extraSuffix}");
		}
		catch (Exception ex)
		{
			Logger.Log("TTSReport", $"[Engine.{stage}] report_failed agentIndex={agentIndex}, error={ex.Message}");
		}
	}

	private byte[] CallVolcV1Api(string apiUrl, string token, string appId, string resourceId, string voiceType, string text, string encoding, int sampleRate, float speedRatio, float loudnessRatio, string extraParamJson)
	{
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Expected O, but got Unknown
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		string requestId = "";
		try
		{
			string text2 = NormalizeExtraParam(extraParamJson);
			if (text2 == null)
			{
				Logger.Log("TtsEngine", "[ERROR] extra_param JSON 无效");
				return null;
			}
			requestId = Guid.NewGuid().ToString();
				JObject jObject = new JObject
				{
					["app"] = new JObject
					{
						["appid"] = appId,
						["token"] = "token",
					["cluster"] = "volcano_tts"
				},
				["user"] = new JObject { ["uid"] = "animusforge" },
				["audio"] = new JObject
				{
					["voice_type"] = voiceType,
					["encoding"] = encoding,
					["speed_ratio"] = Math.Round(speedRatio, 2),
					["rate"] = sampleRate,
					["loudness_ratio"] = Math.Round(loudnessRatio, 2)
				},
					["request"] = new JObject
					{
						["reqid"] = requestId,
						["text"] = text ?? "",
						["operation"] = "query",
						["extra_param"] = text2
					}
				};
			string text3 = jObject.ToString(Formatting.None);
			HttpRequestMessage val = new HttpRequestMessage(HttpMethod.Post, apiUrl);
				try
				{
					val.Content = (HttpContent)new StringContent(text3, Encoding.UTF8, "application/json");
					((HttpHeaders)val.Headers).TryAddWithoutValidation("Authorization", "Bearer;" + token.Trim());
					((HttpHeaders)val.Headers).TryAddWithoutValidation("X-Api-App-Id", (appId ?? "").Trim());
					((HttpHeaders)val.Headers).TryAddWithoutValidation("X-Api-App-Key", (appId ?? "").Trim());
					((HttpHeaders)val.Headers).TryAddWithoutValidation("X-Api-Access-Key", (token ?? "").Trim());
					((HttpHeaders)val.Headers).TryAddWithoutValidation("X-Api-Resource-Id", (resourceId ?? "").Trim());
					((HttpHeaders)val.Headers).TryAddWithoutValidation("X-Api-Request-Id", requestId);
					HttpResponseMessage result = _httpClient.SendAsync(val).GetAwaiter().GetResult();
					string text4 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
				if (!result.IsSuccessStatusCode)
				{
					Logger.Log("TtsEngine", $"[ERROR] 火山 V1 HTTP {(int)result.StatusCode}: {text4.Substring(0, Math.Min(200, text4.Length))}");
					return null;
				}
				JObject jObject2;
				try
				{
					jObject2 = JObject.Parse(text4);
				}
				catch (Exception ex)
				{
					Logger.Log("TtsEngine", "[ERROR] 火山 V1 返回解析失败: " + ex.Message);
					return null;
				}
				int result2 = -1;
				try
				{
					result2 = ((jObject2["code"] != null) ? jObject2["code"].Value<int>() : (-1));
				}
				catch
				{
					int.TryParse((jObject2["code"] != null) ? jObject2["code"].ToString() : "", out result2);
				}
				string arg = ((jObject2["message"] != null) ? jObject2["message"].ToString() : "");
				if (result2 != 3000)
				{
					Logger.Log("TtsEngine", $"[ERROR] 火山 V1 返回异常: code={result2}, message={arg}");
					return null;
				}
				string text5 = ((jObject2["data"] != null) ? jObject2["data"].ToString() : "");
				if (string.IsNullOrWhiteSpace(text5))
				{
					Logger.Log("TtsEngine", "[WARN] 火山 V1 成功但 data 为空");
					return null;
				}
				try
				{
					return Convert.FromBase64String(text5.Trim());
				}
				catch (Exception ex2)
				{
					Logger.Log("TtsEngine", "[ERROR] 火山 V1 data(base64) 解码失败: " + ex2.Message);
					return null;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex3)
		{
			Exception ex4 = ((ex3 is AggregateException aggregateException) ? (aggregateException.Flatten().InnerException ?? ex3) : ex3);
			Logger.Log("TtsEngine", $"[ERROR] 火山 V1 API 调用失败: reqid={requestId}, {ex4.GetType().Name}: {ex4.Message}");
			return null;
		}
	}

	private static string NormalizeExtraParam(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return "{}";
		}
		try
		{
			JToken jToken = JToken.Parse(json);
			return jToken.ToString(Formatting.None);
		}
		catch
		{
			return null;
		}
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

	private void PlayPcmData(byte[] pcmData, int sampleRate, float volume, bool autoPauseOnFocusLoss)
	{
		if (pcmData == null || pcmData.Length == 0)
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
			num = waveOutWrite(phwo, ref pwh, cbwh);
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
				if (_cancelCurrent || _stopWorker)
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
			if (_cancelCurrent || _stopWorker)
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
		if (!_disposed)
		{
			_disposed = true;
			_stopWorker = true;
			StopPlayback();
			try
			{
				_jobQueue.CompleteAdding();
			}
			catch
			{
			}
			if (_workerThread != null && _workerThread.IsAlive)
			{
				_workerThread.Join(3000);
			}
			_initialized = false;
			Logger.Log("TtsEngine", "TTS 引擎已释放");
		}
	}
}
