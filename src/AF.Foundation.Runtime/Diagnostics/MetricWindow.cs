using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class MetricWindow
{
	internal sealed class Bucket
	{
		internal long Count;
		internal long Ok;
		internal long Err;
		internal double SumMs;
		internal double MaxMs;
	}

	private readonly object _sync = new object();
	private readonly Dictionary<string, Bucket> _metrics = new Dictionary<string, Bucket>(StringComparer.Ordinal);
	private DateTime _windowStartUtc = DateTime.UtcNow;
	private DateTime _nextFlushUtc = DateTime.UtcNow.AddSeconds(180.0);

	internal void Record(string metric, bool ok, double latencyMs)
	{
		lock (_sync)
		{
			if (!_metrics.TryGetValue(metric, out Bucket value) || value == null)
			{
				value = new Bucket();
				_metrics[metric] = value;
			}
			value.Count++;
			if (ok) value.Ok++; else value.Err++;
			if (latencyMs >= 0.0)
			{
				value.SumMs += latencyMs;
				if (latencyMs > value.MaxMs) value.MaxMs = latencyMs;
			}
		}
	}

	internal bool TryDrain(DateTime utcNow, out double windowSeconds, out List<KeyValuePair<string, Bucket>> snapshot)
	{
		windowSeconds = 0.0;
		snapshot = null;
		if (utcNow < _nextFlushUtc) return false;
		lock (_sync)
		{
			if (utcNow < _nextFlushUtc) return false;
			windowSeconds = Math.Max(1.0, (utcNow - _windowStartUtc).TotalSeconds);
			snapshot = new List<KeyValuePair<string, Bucket>>(_metrics);
			_metrics.Clear();
			_windowStartUtc = utcNow;
			_nextFlushUtc = utcNow.AddSeconds(180.0);
		}
		return true;
	}
}
