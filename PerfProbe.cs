using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AnimusForge;

internal static class PerfProbe
{
	private sealed class Bucket
	{
		public long Count;
		public long SlowCount;
		public double SumMs;
		public double MaxMs;
	}

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

	private const double FlushIntervalSeconds = 30.0;
	private const double SlowScopeThresholdMs = 3.0;
	private const double SlowFrameThresholdMs = 50.0;
	private const double CriticalFrameThresholdMs = 100.0;
	private const int TopBucketCount = 8;
	private static readonly long EnabledStateRefreshIntervalTicks = TimeSpan.FromMilliseconds(250.0).Ticks;

	private static readonly object SyncRoot = new object();
	private static readonly Dictionary<string, Bucket> Buckets = new Dictionary<string, Bucket>(StringComparer.Ordinal);
	private static readonly Dictionary<string, long> Events = new Dictionary<string, long>(StringComparer.Ordinal);
	private static long _windowStartUtcTicks = DateTime.UtcNow.Ticks;
	private static long _nextFlushUtcTicks = DateTime.UtcNow.AddSeconds(FlushIntervalSeconds).Ticks;
	private static long _frameCount;
	private static long _slowFrameCount;
	private static long _criticalFrameCount;
	private static double _sumFrameDtMs;
	private static double _maxFrameDtMs;
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
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return;
			}
			lock (SyncRoot)
			{
				if (!Events.TryGetValue(name, out long count))
				{
					count = 0L;
				}
				Events[name] = count + 1L;
			}
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
			ResetWindow(nowUtcTicks);
			if (enabled)
			{
				Logger.Log("PerfProbe", "monitor_state enabled=true intervalSec=" + FlushIntervalSeconds + " topBuckets=" + TopBucketCount);
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

	private static void ResetWindow(long nowTicks)
	{
		lock (SyncRoot)
		{
			Buckets.Clear();
			Events.Clear();
			_frameCount = 0L;
			_slowFrameCount = 0L;
			_criticalFrameCount = 0L;
			_sumFrameDtMs = 0.0;
			_maxFrameDtMs = 0.0;
			_windowStartUtcTicks = nowTicks;
			_nextFlushUtcTicks = nowTicks + TimeSpan.FromSeconds(FlushIntervalSeconds).Ticks;
		}
	}

	private static void RecordFrameDt(float dt)
	{
		double ms = Math.Max(0.0, dt * 1000.0);
		lock (SyncRoot)
		{
			_frameCount++;
			_sumFrameDtMs += ms;
			if (ms > _maxFrameDtMs)
			{
				_maxFrameDtMs = ms;
			}
			if (ms >= SlowFrameThresholdMs)
			{
				_slowFrameCount++;
			}
			if (ms >= CriticalFrameThresholdMs)
			{
				_criticalFrameCount++;
			}
		}
	}

	private static void RecordElapsed(string name, long startTimestamp)
	{
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		if (elapsedMs < 0.0)
		{
			elapsedMs = 0.0;
		}
		lock (SyncRoot)
		{
			if (!Buckets.TryGetValue(name, out Bucket bucket) || bucket == null)
			{
				bucket = new Bucket();
				Buckets[name] = bucket;
			}
			bucket.Count++;
			bucket.SumMs += elapsedMs;
			if (elapsedMs > bucket.MaxMs)
			{
				bucket.MaxMs = elapsedMs;
			}
			if (elapsedMs >= SlowScopeThresholdMs)
			{
				bucket.SlowCount++;
			}
		}
		FlushIfDue();
	}

	private static void FlushIfDue()
	{
		long nowTicks = DateTime.UtcNow.Ticks;
		if (nowTicks < _nextFlushUtcTicks)
		{
			return;
		}
		List<string> lines = BuildFlushLines(nowTicks);
		if (lines.Count == 0)
		{
			return;
		}
		foreach (string line in lines)
		{
			Logger.Log("PerfProbe", line);
		}
	}

	private static List<string> BuildFlushLines(long nowTicks)
	{
		List<KeyValuePair<string, Bucket>> bucketSnapshot;
		List<KeyValuePair<string, long>> eventSnapshot;
		long frameCount;
		long slowFrameCount;
		long criticalFrameCount;
		double sumFrameDtMs;
		double maxFrameDtMs;
		long windowStartTicks;
		lock (SyncRoot)
		{
			if (nowTicks < _nextFlushUtcTicks)
			{
				return new List<string>();
			}
			windowStartTicks = _windowStartUtcTicks;
			bucketSnapshot = new List<KeyValuePair<string, Bucket>>(Buckets.Count);
			foreach (KeyValuePair<string, Bucket> pair in Buckets)
			{
				Bucket b = pair.Value;
				if (b == null)
				{
					continue;
				}
				bucketSnapshot.Add(new KeyValuePair<string, Bucket>(pair.Key, new Bucket
				{
					Count = b.Count,
					SlowCount = b.SlowCount,
					SumMs = b.SumMs,
					MaxMs = b.MaxMs
				}));
			}
			eventSnapshot = new List<KeyValuePair<string, long>>(Events);
			frameCount = _frameCount;
			slowFrameCount = _slowFrameCount;
			criticalFrameCount = _criticalFrameCount;
			sumFrameDtMs = _sumFrameDtMs;
			maxFrameDtMs = _maxFrameDtMs;
			Buckets.Clear();
			Events.Clear();
			_frameCount = 0L;
			_slowFrameCount = 0L;
			_criticalFrameCount = 0L;
			_sumFrameDtMs = 0.0;
			_maxFrameDtMs = 0.0;
			_windowStartUtcTicks = nowTicks;
			_nextFlushUtcTicks = nowTicks + TimeSpan.FromSeconds(FlushIntervalSeconds).Ticks;
		}
		double windowSec = Math.Max(0.001, TimeSpan.FromTicks(nowTicks - windowStartTicks).TotalSeconds);
		double avgFrameDt = frameCount > 0L ? sumFrameDtMs / frameCount : 0.0;
		double avgFps = avgFrameDt > 0.001 ? 1000.0 / avgFrameDt : 0.0;
		List<string> lines = new List<string>
		{
			$"window={windowSec:0.0}s frames={frameCount} avgFps={avgFps:0.0} avgFrameDtMs={avgFrameDt:0.00} maxFrameDtMs={maxFrameDtMs:0.00} slowFrames>={SlowFrameThresholdMs:0}ms={slowFrameCount} criticalFrames>={CriticalFrameThresholdMs:0}ms={criticalFrameCount} {BuildRuntimeContext()}"
		};
		bucketSnapshot.Sort(CompareBucketsByMaxThenSum);
		int bucketLimit = Math.Min(TopBucketCount, bucketSnapshot.Count);
		for (int i = 0; i < bucketLimit; i++)
		{
			KeyValuePair<string, Bucket> pair = bucketSnapshot[i];
			Bucket bucket = pair.Value;
			double avg = bucket.Count > 0L ? bucket.SumMs / bucket.Count : 0.0;
			lines.Add($"top[{i + 1}] name={pair.Key} count={bucket.Count} avgMs={avg:0.000} maxMs={bucket.MaxMs:0.000} slow>={SlowScopeThresholdMs:0.0}ms={bucket.SlowCount} sumMs={bucket.SumMs:0.000}");
		}
		eventSnapshot.Sort((a, b) => b.Value.CompareTo(a.Value));
		int eventLimit = Math.Min(4, eventSnapshot.Count);
		for (int i = 0; i < eventLimit; i++)
		{
			KeyValuePair<string, long> pair = eventSnapshot[i];
			lines.Add($"event[{i + 1}] name={pair.Key} count={pair.Value} ratePerSec={(pair.Value / windowSec):0.00}");
		}
		return lines;
	}

	internal static string BuildCurrentSnapshotForFreeze()
	{
		try
		{
			if (!IsEnabled())
			{
				return "disabled_by_mcm";
			}
			long nowTicks = DateTime.UtcNow.Ticks;
			List<KeyValuePair<string, Bucket>> bucketSnapshot = new List<KeyValuePair<string, Bucket>>();
			long frameCount;
			long slowFrameCount;
			long criticalFrameCount;
			double sumFrameDtMs;
			double maxFrameDtMs;
			long windowStartTicks;
			lock (SyncRoot)
			{
				windowStartTicks = _windowStartUtcTicks;
				frameCount = _frameCount;
				slowFrameCount = _slowFrameCount;
				criticalFrameCount = _criticalFrameCount;
				sumFrameDtMs = _sumFrameDtMs;
				maxFrameDtMs = _maxFrameDtMs;
				foreach (KeyValuePair<string, Bucket> pair in Buckets)
				{
					Bucket bucket = pair.Value;
					if (bucket == null)
					{
						continue;
					}
					bucketSnapshot.Add(new KeyValuePair<string, Bucket>(pair.Key, new Bucket
					{
						Count = bucket.Count,
						SlowCount = bucket.SlowCount,
						SumMs = bucket.SumMs,
						MaxMs = bucket.MaxMs
					}));
				}
			}
			double windowSec = Math.Max(0.001, TimeSpan.FromTicks(nowTicks - windowStartTicks).TotalSeconds);
			double avgFrameDt = frameCount > 0L ? sumFrameDtMs / frameCount : 0.0;
			double avgFps = avgFrameDt > 0.001 ? 1000.0 / avgFrameDt : 0.0;
			StringBuilder result = new StringBuilder();
			result.Append("window=").Append(windowSec.ToString("0.0"))
				.Append("s frames=").Append(frameCount)
				.Append(" avgFps=").Append(avgFps.ToString("0.0"))
				.Append(" avgFrameDtMs=").Append(avgFrameDt.ToString("0.00"))
				.Append(" maxFrameDtMs=").Append(maxFrameDtMs.ToString("0.00"))
				.Append(" slowFrames=").Append(slowFrameCount)
				.Append(" criticalFrames=").Append(criticalFrameCount);
			bucketSnapshot.Sort(CompareBucketsByMaxThenSum);
			int limit = Math.Min(TopBucketCount, bucketSnapshot.Count);
			for (int i = 0; i < limit; i++)
			{
				KeyValuePair<string, Bucket> pair = bucketSnapshot[i];
				Bucket bucket = pair.Value;
				double avg = bucket.Count > 0L ? bucket.SumMs / bucket.Count : 0.0;
				result.AppendLine();
				result.Append("top[").Append(i + 1).Append("] name=").Append(pair.Key)
					.Append(" count=").Append(bucket.Count)
					.Append(" avgMs=").Append(avg.ToString("0.000"))
					.Append(" maxMs=").Append(bucket.MaxMs.ToString("0.000"))
					.Append(" slow=").Append(bucket.SlowCount);
			}
			return result.ToString();
		}
		catch
		{
			return "unavailable";
		}
	}

	private static int CompareBucketsByMaxThenSum(KeyValuePair<string, Bucket> left, KeyValuePair<string, Bucket> right)
	{
		int maxCompare = right.Value.MaxMs.CompareTo(left.Value.MaxMs);
		if (maxCompare != 0)
		{
			return maxCompare;
		}
		return right.Value.SumMs.CompareTo(left.Value.SumMs);
	}

	private static string BuildRuntimeContext()
	{
		return FreezeWatchdog.GetCachedRuntimeContextForDiagnostics();
	}
}
