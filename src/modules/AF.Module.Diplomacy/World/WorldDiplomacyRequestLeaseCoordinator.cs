using System;

namespace AnimusForge;

/// <summary>
/// Owns the process-local identity of the single world-diplomacy transport request.
/// The immutable snapshot is the only request metadata captured by the worker.
/// </summary>
internal sealed class WorldDiplomacyRequestLeaseCoordinator
{
	private readonly object _sync = new object();
	private string _activeJobId = "";
	private long _activeRuntimeGeneration;

	internal bool IsRunning
	{
		get
		{
			lock (_sync)
			{
				return !string.IsNullOrWhiteSpace(_activeJobId);
			}
		}
	}

	internal bool TryClaim(
		string jobId,
		long runtimeGeneration,
		int maxTokens,
		int timeoutMilliseconds,
		out WorldDiplomacyRequestSnapshot snapshot)
	{
		snapshot = null;
		string normalizedJobId = (jobId ?? "").Trim();
		if (normalizedJobId.Length == 0 || runtimeGeneration <= 0L)
		{
			return false;
		}

		lock (_sync)
		{
			if (!string.IsNullOrWhiteSpace(_activeJobId))
			{
				return false;
			}
			_activeJobId = normalizedJobId;
			_activeRuntimeGeneration = runtimeGeneration;
			snapshot = new WorldDiplomacyRequestSnapshot(
				normalizedJobId,
				runtimeGeneration,
				Math.Max(256, maxTokens),
				Math.Max(1, timeoutMilliseconds));
			return true;
		}
	}

	internal bool TryRelease(string jobId, long runtimeGeneration)
	{
		lock (_sync)
		{
			if (!string.Equals(_activeJobId, (jobId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
				|| _activeRuntimeGeneration != runtimeGeneration)
			{
				return false;
			}
			_activeJobId = "";
			_activeRuntimeGeneration = 0L;
			return true;
		}
	}

	internal void Reset()
	{
		lock (_sync)
		{
			_activeJobId = "";
			_activeRuntimeGeneration = 0L;
		}
	}
}

internal sealed class WorldDiplomacyRequestSnapshot
{
	internal WorldDiplomacyRequestSnapshot(string jobId, long runtimeGeneration, int maxTokens, int timeoutMilliseconds)
	{
		JobId = jobId;
		RuntimeGeneration = runtimeGeneration;
		MaxTokens = maxTokens;
		TimeoutMilliseconds = timeoutMilliseconds;
	}

	internal string JobId { get; }
	internal long RuntimeGeneration { get; }
	internal int MaxTokens { get; }
	internal int TimeoutMilliseconds { get; }
}
