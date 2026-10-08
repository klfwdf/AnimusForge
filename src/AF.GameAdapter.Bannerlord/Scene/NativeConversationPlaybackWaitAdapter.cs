using System;
using System.Threading.Tasks;
namespace AnimusForge;

// Sole Native wait state; the composition root retains one instance across host replacement.
internal sealed class NativeConversationPlaybackWaitAdapter
{
    private readonly NativeConversationPlaybackWaitPorts _ports;
    internal NativeConversationPlaybackWaitAdapter(NativeConversationPlaybackWaitPorts ports)
    { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }
	private readonly object _nativeConversationTtsPlaybackWaitLock = new object();

	private TaskCompletionSource<bool> _nativeConversationTtsPlaybackWaitTcs;

	private int _nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;

	private int _nativeConversationTtsPlaybackWaitTimeoutMs = 0;

	private long _nativeConversationTtsPlaybackWaitToken = 0L;
	private TtsEngine.PlaybackRequest _nativeConversationTtsPlaybackRequest;

	private const int NativeConversationTtsPlaybackWaitMinTimeoutMs = 30000;

	private const int NativeConversationTtsPlaybackWaitMaxTimeoutMs = 300000;

	internal long RegisterNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, float estimatedDurationSeconds, int textLength)
	{
		if (request == null || request.IsCancellationRequested) { return 0L; }
		int agentIndex = request.AgentIndex;
		TaskCompletionSource<bool> oldTcs = null;
		int timeoutMs = ResolveNativeConversationTtsPlaybackWaitTimeoutMs(estimatedDurationSeconds, textLength);
		TaskCompletionSource<bool> newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (request.IsCancellationRequested) { return 0L; }
			oldTcs = _nativeConversationTtsPlaybackWaitTcs;
			_nativeConversationTtsPlaybackRequest = request;
			_nativeConversationTtsPlaybackWaitTcs = newTcs;
			_nativeConversationTtsPlaybackWaitAgentIndex = agentIndex;
			_nativeConversationTtsPlaybackWaitTimeoutMs = timeoutMs;
			_nativeConversationTtsPlaybackWaitToken++;
			token = _nativeConversationTtsPlaybackWaitToken;
		}
		try
		{
			oldTcs?.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] registered native playback wait. agentIndex=" + agentIndex + ", timeoutMs=" + timeoutMs + ", token=" + token);
		return token;
	}

	internal int ResolveNativeConversationTtsPlaybackWaitTimeoutMs(float estimatedDurationSeconds, int textLength)
	{
		double seconds = 0.0;
		if (estimatedDurationSeconds > 0f && !float.IsNaN(estimatedDurationSeconds) && !float.IsInfinity(estimatedDurationSeconds))
		{
			seconds = estimatedDurationSeconds;
		}
		else
		{
			seconds = Math.Max(1.0, Math.Max(0, textLength) * 0.05);
		}
		seconds += 30.0;
		int timeoutMs = (int)Math.Round(seconds * 1000.0);
		return Math.Max(NativeConversationTtsPlaybackWaitMinTimeoutMs, Math.Min(NativeConversationTtsPlaybackWaitMaxTimeoutMs, timeoutMs));
	}

	internal void ScheduleNativeConversationTypewriterPlaybackFallback(long waitToken, int agentIndex, float estimatedDurationSeconds, int textLength)
	{
		Func<bool> isOwnerCurrent = _ports.CaptureOwnerCurrent();
		TtsEngine.PlaybackRequest request;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (isOwnerCurrent == null || _nativeConversationTtsPlaybackWaitToken != waitToken
				|| _nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackRequest == null)
			{
				return;
			}
			request = _nativeConversationTtsPlaybackRequest;
		}
		int delayMs = ResolveNativeConversationTypewriterFallbackDelayMs(estimatedDurationSeconds, textLength);
		Task.Run(async delegate
		{
			try
			{
				await _ports.Delay(delayMs).ConfigureAwait(false);
				_ports.PostMainThread(delegate
				{
					try
					{
					if (!isOwnerCurrent()) { return; }
					bool started;
					// Keep the owner -> native-wait lock order. Registration of B cannot
					// replace A between its last identity check and the typewriter release.
					lock (_ports.OutputSyncRoot())
					{
						if (!_ports.IsPlaybackRequestCurrent(request)) { return; }
						lock (_nativeConversationTtsPlaybackWaitLock)
						{
							if (!ReferenceEquals(_nativeConversationTtsPlaybackRequest, request)
								|| !IsNativeConversationTtsPlaybackWaitToken(waitToken, agentIndex)
								|| !_ports.IsTypewriterWaiting())
							{
								return;
							}
							started = _ports.StartTypewriter(estimatedDurationSeconds);
						}
					}
					if (started)
					{
						Logger.Log("NativeConversation", "[TTS] typewriter playback fallback released waiting text. agentIndex=" + agentIndex + ", token=" + waitToken + ", delayMs=" + delayMs);
					}
					}
					catch (Exception ex)
					{
						Logger.Log("NativeConversation", "[TTS] typewriter playback fallback failed: " + ex.Message);
					}
				});
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversation", "[TTS] typewriter playback fallback failed: " + ex.Message);
			}
		});
	}

	internal int ResolveNativeConversationTypewriterFallbackDelayMs(float estimatedDurationSeconds, int textLength)
	{
		double seconds = 12.0;
		if (estimatedDurationSeconds > 0f && !float.IsNaN(estimatedDurationSeconds) && !float.IsInfinity(estimatedDurationSeconds))
		{
			seconds = Math.Max(seconds, Math.Min(45.0, estimatedDurationSeconds + 4.0));
		}
		else if (textLength > 0)
		{
			seconds = Math.Max(seconds, Math.Min(45.0, textLength * 0.05 + 4.0));
		}
		return (int)Math.Round(seconds * 1000.0);
	}

	internal bool IsNativeConversationTtsPlaybackWaitToken(long token, int agentIndex)
	{
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackWaitToken != token || _nativeConversationTtsPlaybackRequest == null || _nativeConversationTtsPlaybackRequest.IsCancellationRequested)
			{
				return false;
			}
			int expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			return expectedAgentIndex == agentIndex || (expectedAgentIndex < 0 && agentIndex < 0);
		}
	}

	internal bool CompleteNativeConversationTtsPlaybackWait(TtsEngine.PlaybackRequest request, string reason, bool force = false)
	{
		int agentIndex = request?.AgentIndex ?? int.MinValue;
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		long token = 0L;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			bool matches = force || (request != null && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request));
			if (!matches)
			{
				return false;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			token = _nativeConversationTtsPlaybackWaitToken;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait. reason=" + (reason ?? "") + ", agentIndex=" + agentIndex + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
		return true;
	}

	internal void CompleteNativeConversationTtsPlaybackWaitByToken(long token, string reason)
	{
		TaskCompletionSource<bool> tcs = null;
		int expectedAgentIndex = int.MinValue;
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null || _nativeConversationTtsPlaybackWaitToken != token)
			{
				return;
			}
			tcs = _nativeConversationTtsPlaybackWaitTcs;
			expectedAgentIndex = _nativeConversationTtsPlaybackWaitAgentIndex;
			_nativeConversationTtsPlaybackWaitTcs = null;
			_nativeConversationTtsPlaybackRequest = null;
			_nativeConversationTtsPlaybackWaitAgentIndex = int.MinValue;
			_nativeConversationTtsPlaybackWaitTimeoutMs = 0;
		}
		try
		{
			tcs.TrySetResult(true);
		}
		catch
		{
		}
		Logger.Log("NativeConversation", "[TTS] completed native playback wait by token. reason=" + (reason ?? "") + ", expectedAgentIndex=" + expectedAgentIndex + ", token=" + token);
	}

	internal bool IsNativeConversationTtsPlaybackWaitRequest(TtsEngine.PlaybackRequest request)
	{
		lock (_nativeConversationTtsPlaybackWaitLock)
		{
			if (_nativeConversationTtsPlaybackWaitTcs == null)
			{
				return false;
			}
			return request != null && !request.IsCancellationRequested && ReferenceEquals(_nativeConversationTtsPlaybackRequest, request);
		}
	}

	internal async Task WaitForNativeConversationTtsPlaybackFinishedForExternalAsync()
	{
		Task waitTask = null;
		int timeoutMs = 0;
		long token = 0L;
		try
		{
			lock (_nativeConversationTtsPlaybackWaitLock)
			{
				waitTask = _nativeConversationTtsPlaybackWaitTcs?.Task;
				timeoutMs = _nativeConversationTtsPlaybackWaitTimeoutMs;
				token = _nativeConversationTtsPlaybackWaitToken;
			}
			if (waitTask == null || waitTask.IsCompleted)
			{
				return;
			}
			if (timeoutMs <= 0)
			{
				timeoutMs = NativeConversationTtsPlaybackWaitMinTimeoutMs;
			}
			Logger.Log("NativeConversation", "[TTS] waiting for native conversation playback before enabling reply. timeoutMs=" + timeoutMs);
			Task completed = await Task.WhenAny(waitTask, _ports.Delay(timeoutMs)).ConfigureAwait(false);
			if (completed != waitTask)
			{
				CompleteNativeConversationTtsPlaybackWaitByToken(token, "timeout");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[TTS] wait for native playback failed: " + ex.Message);
		}
	}

	internal void HandleTtsPlaybackCancelled(TtsEngine.PlaybackRequest request)
	{
		if (request == null) { return; }
		long completedWaitRevision;
		lock (_nativeConversationTtsPlaybackWaitLock) { completedWaitRevision = _nativeConversationTtsPlaybackWaitToken; }
		bool releaseNativeTypewriter = CompleteNativeConversationTtsPlaybackWait(request, "playback_cancelled");
		_ports.PostMainThread(delegate
		{
			try
			{
				if (!_ports.IsPlaybackRequestCurrentAllowCancelled(request)) { return; }
				if (releaseNativeTypewriter)
				{
					lock (_nativeConversationTtsPlaybackWaitLock)
					{
						if (_nativeConversationTtsPlaybackWaitToken == completedWaitRevision && _nativeConversationTtsPlaybackWaitTcs == null)
						{
							_ports.StartTypewriter(-1f);
						}
					}
				}
				if (request.AgentIndex >= 0 && _ports.IsActivePlaybackRequestAllowCancelled(request))
				{
					_ports.ClearPendingBubble(request.AgentIndex);
					_ports.ClearPendingFeed(request.AgentIndex);
					_ports.CleanupLipSync(request.AgentIndex);
				}
			}
			finally { _ports.RetirePlaybackRequest(request); }
		});
	}
}

internal sealed class NativeConversationPlaybackWaitPorts
{
    internal Func<object> OutputSyncRoot;
    internal Func<Func<bool>> CaptureOwnerCurrent;
    internal Action<Action> PostMainThread;
    internal Func<TtsEngine.PlaybackRequest, bool> IsPlaybackRequestCurrent;
    internal Func<TtsEngine.PlaybackRequest, bool> IsPlaybackRequestCurrentAllowCancelled;
    internal Func<TtsEngine.PlaybackRequest, bool> IsActivePlaybackRequestAllowCancelled;
    internal Action<TtsEngine.PlaybackRequest> RetirePlaybackRequest;
    internal Action<int> ClearPendingBubble, ClearPendingFeed, CleanupLipSync;
    internal Func<bool> IsTypewriterWaiting;
    internal Func<float, bool> StartTypewriter;
    internal Func<int, Task> Delay = Task.Delay;
}
