using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AnimusForge;

internal static class PerfProbe
{

	public readonly struct ScopeToken : IDisposable
	{
		private readonly string _name;
		private readonly long _startTimestamp;

		internal ScopeToken(string name, long startTimestamp)
		{
			_name = name;
			_startTimestamp = startTimestamp;
		}

		public void Dispose()
		{
			try
			{
				if (_startTimestamp <= 0L || string.IsNullOrWhiteSpace(_name))
				{
					return;
				}
				RecordElapsed(_name, _startTimestamp);
			}
			catch
			{
			}
		}
	}

	private static readonly long EnabledStateRefreshIntervalTicks = TimeSpan.FromMilliseconds(250.0).Ticks;

	private static readonly PerformanceWindow Window = new PerformanceWindow();
	private static readonly Func<string> RuntimeContextProvider = BuildRuntimeContext;
	private static int _enabled;
	private static int _detailedScopesEnabled;
	private static long _nextEnabledStateRefreshUtcTicks;

	public static ScopeToken Scope(string name)
	{
		try
		{
			if (!AreDetailedScopesEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return default;
			}
			return new ScopeToken(name, Stopwatch.GetTimestamp());
		}
		catch
		{
			return default;
		}
	}

	public static long BeginFrame(float dt)
	{
		try
		{
			RefreshEnabledStateIfDue(DateTime.UtcNow.Ticks);
			if (!IsEnabled())
			{
				return 0L;
			}
			RecordFrameDt(dt);
			return Stopwatch.GetTimestamp();
		}
		catch
		{
			return 0L;
		}
	}

	public static void EndFrame(long startTimestamp, string name)
	{
		try
		{
			if (startTimestamp <= 0L)
			{
				return;
			}
			RecordElapsed(string.IsNullOrWhiteSpace(name) ? "frame.total" : name, startTimestamp);
			FlushIfDue();
		}
		catch
		{
		}
	}

	public static void MarkEvent(string name)
	{
		try
		{
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name)) return;
			Window.MarkEvent(name);
			FlushIfDue();
		}
		catch
		{
		}
	}

	private static bool IsEnabled()
	{
		return Volatile.Read(ref _enabled) != 0;
	}

	internal static bool IsDetailedScopeRecordingActive()
	{
		return AreDetailedScopesEnabled();
	}

	private static bool AreDetailedScopesEnabled()
	{
		return Volatile.Read(ref _detailedScopesEnabled) != 0;
	}

	private static void RefreshEnabledStateIfDue(long nowUtcTicks)
	{
		try
		{
			long nextAllowedTicks = Interlocked.Read(ref _nextEnabledStateRefreshUtcTicks);
			if (nowUtcTicks < nextAllowedTicks)
			{
				return;
			}
			Interlocked.Exchange(ref _nextEnabledStateRefreshUtcTicks, nowUtcTicks + EnabledStateRefreshIntervalTicks);
			bool enabled = Logger.IsModLogicEnabled && (DuelSettings.GetSettings()?.EnablePerformanceMonitor ?? false);
			bool detailedScopesEnabled = enabled && Logger.IsVerboseModLogicEnabled;
			int next = enabled ? 1 : 0;
			Interlocked.Exchange(ref _detailedScopesEnabled, detailedScopesEnabled ? 1 : 0);
			int previous = Interlocked.Exchange(ref _enabled, next);
			if (previous == next)
			{
				return;
			}
			Window.ResetWindow(nowUtcTicks);
			if (enabled)
			{
				Logger.Log("PerfProbe", "monitor_state enabled=true intervalSec=" + PerformanceWindow.FlushIntervalSeconds + " topBuckets=" + PerformanceWindow.TopBucketCount);
			}
			else
			{
				Logger.Log("PerfProbe", "monitor_state enabled=false");
			}
		}
		catch
		{
		}
	}

	private static void RecordFrameDt(float dt) => Window.RecordFrameDt(dt);

	private static void RecordElapsed(string name, long startTimestamp)
	{
		Window.RecordElapsed(name, startTimestamp);
		FlushIfDue();
	}

	private static void FlushIfDue()
	{
		long nowTicks = DateTime.UtcNow.Ticks;
		if (!Window.IsFlushDue(nowTicks)) return;
		List<string> lines = Window.BuildFlushLines(nowTicks, RuntimeContextProvider);
		if (lines.Count == 0) return;
		foreach (string line in lines) Logger.Log("PerfProbe", line);
	}

	internal static string BuildCurrentSnapshotForFreeze()
	{
		try
		{
			return IsEnabled() ? Window.BuildCurrentSnapshotForFreeze() : "disabled_by_mcm";
		}
		catch
		{
			return "unavailable";
		}
	}

	private static string BuildRuntimeContext()
	{
		return FreezeWatchdog.GetCachedRuntimeContextForDiagnostics();
	}
}
