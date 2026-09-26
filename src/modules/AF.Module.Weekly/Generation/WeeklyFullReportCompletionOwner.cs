using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge;

// Owns completed on-demand requests; only Process may invoke game/UI mutations.
internal sealed class WeeklyFullReportCompletionOwner
{
	private sealed class PendingCompletion
	{
		internal readonly long RuntimeGeneration;
		internal readonly Func<bool> Apply;
		internal readonly TaskCompletionSource<bool> Result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		internal PendingCompletion(long runtimeGeneration, Func<bool> apply)
		{
			RuntimeGeneration = runtimeGeneration;
			Apply = apply;
		}
	}

	private readonly object _sync = new object();
	private readonly Queue<PendingCompletion> _pending = new Queue<PendingCompletion>();
	private readonly Func<bool> _isCurrentOwner;
	private readonly Func<long, string, bool> _isStale;

	internal WeeklyFullReportCompletionOwner(Func<bool> isCurrentOwner, Func<long, string, bool> isStale)
	{
		_isCurrentOwner = isCurrentOwner ?? throw new ArgumentNullException(nameof(isCurrentOwner));
		_isStale = isStale ?? throw new ArgumentNullException(nameof(isStale));
	}

	internal Task<bool> Enqueue(long runtimeGeneration, Func<bool> apply)
	{
		lock (_sync)
		{
			if (!_isCurrentOwner() || _isStale(runtimeGeneration, "weekly_full_report_enqueue"))
			{
				return Task.FromResult(false);
			}
			PendingCompletion pending = new PendingCompletion(runtimeGeneration, apply);
			_pending.Enqueue(pending);
			return pending.Result.Task;
		}
	}

	internal void Process(int maxCompletions = 2)
	{
		for (int processed = 0; processed < maxCompletions; processed++)
		{
			PendingCompletion pending;
			lock (_sync)
			{
				if (_pending.Count == 0) return;
				pending = _pending.Dequeue();
			}
			try
			{
				bool accepted = _isCurrentOwner() && !_isStale(pending.RuntimeGeneration, "weekly_full_report_commit");
				pending.Result.TrySetResult(accepted && (pending.Apply?.Invoke() ?? false));
			}
			catch (Exception ex)
			{
				pending.Result.TrySetException(ex);
			}
		}
	}

	internal void Cancel()
	{
		lock (_sync)
		{
			while (_pending.Count > 0)
			{
				_pending.Dequeue().Result.TrySetResult(false);
			}
		}
	}
}
