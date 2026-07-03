using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class FreezeWatchdog
{
	internal sealed class ScopeState
	{
		public string Name;
		public long StartTimestamp;
		public int ThreadId;
		public bool MainThreadScope;
		public ScopeState Parent;
	}

	internal readonly struct ScopeToken : IDisposable
	{
		private readonly ScopeState _state;

		internal ScopeToken(ScopeState state)
		{
			_state = state;
		}

		public void Dispose()
		{
			CompleteScope(_state);
		}
	}

	private const string LogSource = "FreezeWatchdog";
	private const string SnapshotFileName = "FreezeWatchdog_LastCheckpoint.txt";
	private const int RecentEventLimit = 96;
	private const double MainThreadSlowScopeMs = 250.0;
	private const double BackgroundSlowScopeMs = 1000.0;
	private const double FrameGapReportMs = 1000.0;
	private const double MonitorHangReportMs = 2500.0;
	private const double MonitorRepeatMs = 5000.0;
	private const int MonitorIntervalMs = 1000;

	private static readonly object SyncRoot = new object();
	private static readonly string[] RecentEvents = new string[RecentEventLimit];
	private static int _recentEventNext;
	private static int _recentEventCount;
	private static long _frameIndex;
	private static int _mainThreadId;
	private static long _lastMainHeartbeatTimestamp;
	private static long _lastMainHeartbeatUtcTicks;
	private static string _mainThreadActiveScope = "";
	private static long _mainThreadActiveScopeStartTimestamp;
	private static string _lastCompletedMainScope = "";
	private static long _lastCompletedMainScopeUtcTicks;
	private static long _lastMonitorReportTimestamp;
	private static System.Threading.Timer _monitorTimer;
	private static int _monitorStarted;

	[ThreadStatic]
	private static ScopeState _currentScope;

	internal static ScopeToken Scope(string name)
	{
		try
		{
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return default;
			}
			EnsureMonitorStarted();
			int threadId = Thread.CurrentThread.ManagedThreadId;
			bool isMainThread = IsKnownMainThread(threadId);
			long startTimestamp = Stopwatch.GetTimestamp();
			ScopeState state = new ScopeState
			{
				Name = Sanitize(name, 180),
				StartTimestamp = startTimestamp,
				ThreadId = threadId,
				MainThreadScope = isMainThread,
				Parent = _currentScope
			};
			_currentScope = state;
			if (isMainThread)
			{
				TouchMainHeartbeat(startTimestamp);
				lock (SyncRoot)
				{
					_mainThreadActiveScope = state.Name;
					_mainThreadActiveScopeStartTimestamp = startTimestamp;
				}
			}
			RecordEvent("begin", state.Name, "", threadId);
			return new ScopeToken(state);
		}
		catch
		{
			return default;
		}
	}

	internal static void BeginFrame(float dt)
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			EnsureMonitorStarted();
			int threadId = Thread.CurrentThread.ManagedThreadId;
			if (_mainThreadId == 0)
			{
				Interlocked.CompareExchange(ref _mainThreadId, threadId, 0);
			}
			long now = Stopwatch.GetTimestamp();
			long previous = Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, now);
			Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
			long frame = Interlocked.Increment(ref _frameIndex);
			double dtMs = Math.Max(0.0, dt * 1000.0);
			RecordEvent("frame_begin", "SubModule.OnApplicationTick", "dtMs=" + dtMs.ToString("0.00") + " frame=" + frame, threadId);
			if (previous > 0L)
			{
				double gapMs = TimestampDeltaMs(previous, now);
				if (gapMs >= FrameGapReportMs)
				{
					WriteImmediate("[FREEZE_GAP] no_frame_ms=" + gapMs.ToString("0.00") + " dtMs=" + dtMs.ToString("0.00") + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
				}
			}
		}
		catch
		{
		}
	}

	internal static void EndFrame()
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			TouchMainHeartbeat(now);
			lock (SyncRoot)
			{
				if (_currentScope == null)
				{
					_mainThreadActiveScope = "";
					_mainThreadActiveScopeStartTimestamp = 0L;
				}
				_lastCompletedMainScope = "SubModule.OnApplicationTick.frame_end";
				_lastCompletedMainScopeUtcTicks = DateTime.UtcNow.Ticks;
			}
			RecordEvent("frame_end", "SubModule.OnApplicationTick", "frame=" + Interlocked.Read(ref _frameIndex), Thread.CurrentThread.ManagedThreadId);
		}
		catch
		{
		}
	}

	internal static void Mark(string name, string detail = null, bool immediate = false)
	{
		try
		{
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return;
			}
			EnsureMonitorStarted();
			string safeName = Sanitize(name, 180);
			string safeDetail = Sanitize(detail, 300);
			int threadId = Thread.CurrentThread.ManagedThreadId;
			RecordEvent("mark", safeName, safeDetail, threadId);
			if (immediate)
			{
				WriteImmediate("[MARK] name=" + safeName + (string.IsNullOrWhiteSpace(safeDetail) ? "" : " detail=" + safeDetail) + " " + BuildStateSummary(includeRecent: false), writeSnapshot: true);
			}
		}
		catch
		{
		}
	}

	private static void CompleteScope(ScopeState state)
	{
		try
		{
			if (state == null || state.StartTimestamp <= 0L)
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			double elapsedMs = TimestampDeltaMs(state.StartTimestamp, now);
			if (ReferenceEquals(_currentScope, state))
			{
				_currentScope = state.Parent;
			}
			else
			{
				_currentScope = state.Parent;
			}
			if (state.MainThreadScope)
			{
				TouchMainHeartbeat(now);
				lock (SyncRoot)
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
			RecordEvent("end", state.Name, "elapsedMs=" + elapsedMs.ToString("0.00"), state.ThreadId);
			double threshold = state.MainThreadScope ? MainThreadSlowScopeMs : BackgroundSlowScopeMs;
			if (elapsedMs >= threshold)
			{
				WriteImmediate("[FREEZE_SLOW_SCOPE] name=" + state.Name + " elapsedMs=" + elapsedMs.ToString("0.00") + " thread=" + state.ThreadId + " main=" + state.MainThreadScope + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
			}
		}
		catch
		{
		}
	}

	private static void EnsureMonitorStarted()
	{
		try
		{
			if (Interlocked.CompareExchange(ref _monitorStarted, 1, 0) != 0)
			{
				return;
			}
			_monitorTimer = new System.Threading.Timer(MonitorCallback, null, MonitorIntervalMs, MonitorIntervalMs);
		}
		catch
		{
		}
	}

	private static void MonitorCallback(object state)
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			long lastHeartbeat = Interlocked.Read(ref _lastMainHeartbeatTimestamp);
			if (lastHeartbeat <= 0L)
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			double noHeartbeatMs = TimestampDeltaMs(lastHeartbeat, now);
			if (noHeartbeatMs < MonitorHangReportMs)
			{
				return;
			}
			long lastReport = Interlocked.Read(ref _lastMonitorReportTimestamp);
			if (lastReport > 0L && TimestampDeltaMs(lastReport, now) < MonitorRepeatMs)
			{
				return;
			}
			Interlocked.Exchange(ref _lastMonitorReportTimestamp, now);
			WriteImmediate("[FREEZE_MONITOR] no_main_heartbeat_ms=" + noHeartbeatMs.ToString("0.00") + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
		}
		catch
		{
		}
	}

	private static void TouchMainHeartbeat(long timestamp)
	{
		try
		{
			Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, timestamp);
			Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
	}

	private static bool IsKnownMainThread(int threadId)
	{
		try
		{
			int known = Interlocked.CompareExchange(ref _mainThreadId, 0, 0);
			if (known == 0)
			{
				return false;
			}
			return known == threadId;
		}
		catch
		{
			return false;
		}
	}

	private static void RecordEvent(string kind, string name, string detail, int threadId)
	{
		try
		{
			string line = DateTime.Now.ToString("HH:mm:ss.fff")
				+ " frame=" + Interlocked.Read(ref _frameIndex)
				+ " tid=" + threadId
				+ " " + Sanitize(kind, 32)
				+ " " + Sanitize(name, 180)
				+ (string.IsNullOrWhiteSpace(detail) ? "" : " " + Sanitize(detail, 300));
			lock (SyncRoot)
			{
				RecentEvents[_recentEventNext] = line;
				_recentEventNext = (_recentEventNext + 1) % RecentEventLimit;
				if (_recentEventCount < RecentEventLimit)
				{
					_recentEventCount++;
				}
			}
		}
		catch
		{
		}
	}

	private static void WriteImmediate(string message, bool writeSnapshot)
	{
		try
		{
			Logger.LogImmediate(LogSource, message ?? "");
			if (writeSnapshot)
			{
				Logger.WriteLogSnapshotImmediate(SnapshotFileName, BuildSnapshot(message));
			}
		}
		catch
		{
		}
	}

	private static string BuildStateSummary(bool includeRecent)
	{
		string activeScope;
		long activeStart;
		string lastCompleted;
		long lastCompletedUtc;
		long lastHeartbeatUtc;
		lock (SyncRoot)
		{
			activeScope = _mainThreadActiveScope ?? "";
			activeStart = _mainThreadActiveScopeStartTimestamp;
			lastCompleted = _lastCompletedMainScope ?? "";
			lastCompletedUtc = _lastCompletedMainScopeUtcTicks;
			lastHeartbeatUtc = _lastMainHeartbeatUtcTicks;
		}
		double activeMs = activeStart > 0L ? TimestampDeltaMs(activeStart, Stopwatch.GetTimestamp()) : 0.0;
		string summary = "frame=" + Interlocked.Read(ref _frameIndex)
			+ " mainThread=" + Interlocked.CompareExchange(ref _mainThreadId, 0, 0)
			+ " active=" + (string.IsNullOrWhiteSpace(activeScope) ? "(none)" : activeScope)
			+ " activeMs=" + activeMs.ToString("0.00")
			+ " lastCompleted=" + (string.IsNullOrWhiteSpace(lastCompleted) ? "(none)" : Sanitize(lastCompleted, 180))
			+ " lastCompletedAgeMs=" + AgeMsFromUtcTicks(lastCompletedUtc).ToString("0.00")
			+ " heartbeatAgeMs=" + AgeMsFromUtcTicks(lastHeartbeatUtc).ToString("0.00")
			+ " context={" + BuildRuntimeContext() + "}";
		if (includeRecent)
		{
			summary += " recent={" + BuildRecentEventsOneLine(12) + "}";
		}
		return summary;
	}

	private static string BuildSnapshot(string trigger)
	{
		try
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("AnimusForge FreezeWatchdog last checkpoint");
			sb.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			sb.AppendLine("trigger=" + Sanitize(trigger, 500));
			sb.AppendLine("state=" + BuildStateSummary(includeRecent: false));
			sb.AppendLine("recent:");
			foreach (string line in BuildRecentEventsSnapshot())
			{
				sb.AppendLine(line);
			}
			return sb.ToString();
		}
		catch
		{
			return "AnimusForge FreezeWatchdog snapshot failed.";
		}
	}

	private static List<string> BuildRecentEventsSnapshot()
	{
		List<string> result = new List<string>();
		try
		{
			lock (SyncRoot)
			{
				int start = (_recentEventNext - _recentEventCount + RecentEventLimit) % RecentEventLimit;
				for (int i = 0; i < _recentEventCount; i++)
				{
					string line = RecentEvents[(start + i) % RecentEventLimit];
					if (!string.IsNullOrWhiteSpace(line))
					{
						result.Add(line);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static string BuildRecentEventsOneLine(int maxCount)
	{
		try
		{
			List<string> snapshot = BuildRecentEventsSnapshot();
			if (snapshot.Count == 0)
			{
				return "";
			}
			int start = Math.Max(0, snapshot.Count - Math.Max(1, maxCount));
			StringBuilder sb = new StringBuilder();
			for (int i = start; i < snapshot.Count; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(" | ");
				}
				sb.Append(Sanitize(snapshot[i], 180));
			}
			return sb.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildRuntimeContext()
	{
		try
		{
			string activeState = "";
			try
			{
				activeState = Game.Current?.GameStateManager?.ActiveState?.GetType()?.Name ?? "";
			}
			catch
			{
				activeState = "";
			}
			string menu = "";
			try
			{
				menu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";
			}
			catch
			{
				menu = "";
			}
			bool conversation = false;
			try
			{
				conversation = Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
			}
			catch
			{
				conversation = false;
			}
			bool mission = false;
			try
			{
				mission = Mission.Current != null;
			}
			catch
			{
				mission = false;
			}
			return "state=" + activeState + " mission=" + mission + " conversation=" + conversation + " menu=" + menu;
		}
		catch
		{
			return "state=unknown";
		}
	}

	private static bool IsEnabled()
	{
		try
		{
			return Logger.IsModLogicEnabled;
		}
		catch
		{
			return false;
		}
	}

	private static double TimestampDeltaMs(long startTimestamp, long endTimestamp)
	{
		try
		{
			return Math.Max(0.0, (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency);
		}
		catch
		{
			return 0.0;
		}
	}

	private static double AgeMsFromUtcTicks(long utcTicks)
	{
		try
		{
			if (utcTicks <= 0L)
			{
				return -1.0;
			}
			return Math.Max(0.0, TimeSpan.FromTicks(DateTime.UtcNow.Ticks - utcTicks).TotalMilliseconds);
		}
		catch
		{
			return -1.0;
		}
	}

	private static string Sanitize(string value, int maxLength)
	{
		try
		{
			string text = (value ?? "").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ").Trim();
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = text.Substring(0, maxLength) + "...";
			}
			return text;
		}
		catch
		{
			return "";
		}
	}
}
