using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AnimusForge;

internal sealed class FreezeWatchState
{
	internal const int RecentEventLimit = 256;
	internal sealed class ScopeState
	{
		internal string Name;
		internal long StartTimestamp;
		internal int ThreadId;
		internal bool MainThreadScope;
		internal ScopeState Parent;
	}

	internal struct Snapshot
	{
		internal string ActiveScope;
		internal long ActiveStart;
		internal string LastCompleted;
		internal long LastCompletedUtc;
		internal long LastHeartbeatUtc;
		internal long FrameIndex;
		internal int MainThreadId;
		internal string RuntimeContext;
		internal long RuntimeContextUtc;
		internal long MonitorHeartbeatUtc;
	}

	private readonly object _sync = new object();
	private readonly string[] _recentEvents = new string[RecentEventLimit];
	private int _recentEventNext;
	private int _recentEventCount;
	private long _frameIndex;
	private int _mainThreadId;
	private long _lastMainHeartbeatTimestamp;
	private long _lastMainHeartbeatUtcTicks;
	private string _mainThreadActiveScope = "";
	private long _mainThreadActiveScopeStartTimestamp;
	private string _lastCompletedMainScope = "";
	private long _lastCompletedMainScopeUtcTicks;
	private long _monitorHeartbeatUtcTicks;
	private int _monitorStarted;
	private string _cachedRuntimeContext = "state=unknown mission=False conversation=False menu=";
	private long _cachedRuntimeContextUtcTicks;
	[ThreadStatic] private static ScopeState _currentScope;

	internal long FrameIndex => Interlocked.Read(ref _frameIndex);
	internal int MainThreadId => Interlocked.CompareExchange(ref _mainThreadId, 0, 0);
	internal long LastMainHeartbeatTimestamp => Interlocked.Read(ref _lastMainHeartbeatTimestamp);
	internal string CachedRuntimeContext => Volatile.Read(ref _cachedRuntimeContext) ?? "state=unknown";
	internal bool TryStartMonitor() => Interlocked.CompareExchange(ref _monitorStarted, 1, 0) == 0;
	internal void SetMonitorHeartbeat(long utcTicks) => Interlocked.Exchange(ref _monitorHeartbeatUtcTicks, utcTicks);
	internal void SetRuntimeContext(string value, long utcTicks)
	{
		Volatile.Write(ref _cachedRuntimeContext, value);
		Interlocked.Exchange(ref _cachedRuntimeContextUtcTicks, utcTicks);
	}

	internal ScopeState EnterScope(string name, int threadId, bool main, long startTimestamp)
	{
		ScopeState state = new ScopeState { Name = Sanitize(name, 180), StartTimestamp = startTimestamp,
			ThreadId = threadId, MainThreadScope = main, Parent = _currentScope };
		_currentScope = state;
		if (main)
		{
			TouchMainHeartbeat(startTimestamp);
			lock (_sync)
			{
				_mainThreadActiveScope = state.Name;
				_mainThreadActiveScopeStartTimestamp = startTimestamp;
			}
		}
		return state;
	}

	internal void EnsureMainThread(int threadId)
	{
		if (_mainThreadId == 0) Interlocked.CompareExchange(ref _mainThreadId, threadId, 0);
	}

	internal long BeginFrameHeartbeat(long timestamp, long utcTicks)
	{
		long previous = Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, timestamp);
		Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, utcTicks);
		return previous;
	}

	internal long AdvanceFrame() => Interlocked.Increment(ref _frameIndex);

	internal void EndFrame(long timestamp)
	{
		TouchMainHeartbeat(timestamp);
		lock (_sync)
		{
			if (_currentScope == null)
			{
				_mainThreadActiveScope = "";
				_mainThreadActiveScopeStartTimestamp = 0L;
			}
			_lastCompletedMainScope = "SubModule.OnApplicationTick.frame_end";
			_lastCompletedMainScopeUtcTicks = DateTime.UtcNow.Ticks;
		}
	}

	internal void CompleteScope(ScopeState state, long timestamp, double elapsedMs)
	{
		_currentScope = state.Parent;
		if (!state.MainThreadScope) return;
		TouchMainHeartbeat(timestamp);
		lock (_sync)
		{
			if (state.Parent != null && state.Parent.MainThreadScope)
			{
				_mainThreadActiveScope = state.Parent.Name;
				_mainThreadActiveScopeStartTimestamp = state.Parent.StartTimestamp;
			}
			else
			{
				_mainThreadActiveScope = "";
				_mainThreadActiveScopeStartTimestamp = 0L;
			}
			_lastCompletedMainScope = state.Name + " elapsedMs=" + elapsedMs.ToString("0.00");
			_lastCompletedMainScopeUtcTicks = DateTime.UtcNow.Ticks;
		}
	}

	internal void TouchMainHeartbeat(long timestamp)
	{
		Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, timestamp);
		Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
	}

	internal bool IsKnownMainThread(int threadId)
	{
		int known = Interlocked.CompareExchange(ref _mainThreadId, 0, 0);
		return known != 0 && known == threadId;
	}

	internal void RecordEvent(string kind, string name, string detail, int threadId)
	{
		string line = DateTime.Now.ToString("HH:mm:ss.fff")
			+ " frame=" + Interlocked.Read(ref _frameIndex) + " tid=" + threadId
			+ " " + Sanitize(kind, 32) + " " + Sanitize(name, 180)
			+ (string.IsNullOrWhiteSpace(detail) ? "" : " " + Sanitize(detail, 300));
		lock (_sync)
		{
			_recentEvents[_recentEventNext] = line;
			_recentEventNext = (_recentEventNext + 1) % RecentEventLimit;
			if (_recentEventCount < RecentEventLimit) _recentEventCount++;
		}
	}

	internal List<string> RecentEventsSnapshot()
	{
		List<string> result = new List<string>();
		lock (_sync)
		{
			int start = (_recentEventNext - _recentEventCount + RecentEventLimit) % RecentEventLimit;
			for (int i = 0; i < _recentEventCount; i++)
			{
				string line = _recentEvents[(start + i) % RecentEventLimit];
				if (!string.IsNullOrWhiteSpace(line)) result.Add(line);
			}
		}
		return result;
	}

	internal string RecentEventsOneLine(int maxCount)
	{
		List<string> snapshot = RecentEventsSnapshot();
		if (snapshot.Count == 0) return "";
		int start = Math.Max(0, snapshot.Count - Math.Max(1, maxCount));
		StringBuilder sb = new StringBuilder();
		for (int i = start; i < snapshot.Count; i++)
		{
			if (sb.Length > 0) sb.Append(" | ");
			sb.Append(Sanitize(snapshot[i], 180));
		}
		return sb.ToString();
	}

	internal Snapshot Capture()
	{
		Snapshot snapshot;
		lock (_sync)
		{
			snapshot = new Snapshot { ActiveScope = _mainThreadActiveScope ?? "", ActiveStart = _mainThreadActiveScopeStartTimestamp,
				LastCompleted = _lastCompletedMainScope ?? "", LastCompletedUtc = _lastCompletedMainScopeUtcTicks,
				LastHeartbeatUtc = _lastMainHeartbeatUtcTicks };
		}
		snapshot.FrameIndex = FrameIndex;
		snapshot.MainThreadId = MainThreadId;
		snapshot.RuntimeContext = CachedRuntimeContext;
		snapshot.RuntimeContextUtc = Interlocked.Read(ref _cachedRuntimeContextUtcTicks);
		snapshot.MonitorHeartbeatUtc = Interlocked.Read(ref _monitorHeartbeatUtcTicks);
		return snapshot;
	}

	internal static string Sanitize(string value, int maxLength)
	{
		try
		{
			string text = (value ?? "").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ").Trim();
			if (maxLength > 0 && text.Length > maxLength) text = text.Substring(0, maxLength) + "...";
			return text;
		}
		catch { return ""; }
	}
}
