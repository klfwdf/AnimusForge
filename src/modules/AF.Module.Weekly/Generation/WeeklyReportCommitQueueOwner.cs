using System;
using System.Collections.Generic;
using System.Threading;

namespace AnimusForge;

internal sealed class WeeklyReportCommitQueueOwner<TContext, TResult> where TContext : class
{
	private readonly object _sync = new object();
	private readonly Queue<TContext> _pending = new Queue<TContext>();
	private readonly Action<TContext, TResult> _complete;
	private readonly Func<TResult> _newCanceledResult;
	private int _hasPending;

	internal WeeklyReportCommitQueueOwner(Action<TContext, TResult> complete, Func<TResult> newCanceledResult)
	{
		_complete = complete ?? throw new ArgumentNullException(nameof(complete));
		_newCanceledResult = newCanceledResult ?? throw new ArgumentNullException(nameof(newCanceledResult));
	}

	internal bool HasPending => Volatile.Read(ref _hasPending) != 0;

	internal void Enqueue(TContext context)
	{
		EnqueueIfCurrent(context, null);
	}

	// Admission and reset use the same lock. A worker arriving after load cleanup
	// must be settled here, because a retired host may never receive another tick.
	// The predicate only reads owner/generation; completion runs outside the lock.
	internal bool EnqueueIfCurrent(TContext context, Func<bool> isCurrent)
	{
		lock (_sync)
		{
			if (isCurrent == null || isCurrent())
			{
				_pending.Enqueue(context);
				Volatile.Write(ref _hasPending, 1);
				return true;
			}
		}
		_complete(context, _newCanceledResult());
		return false;
	}

	internal TContext Peek()
	{
		lock (_sync)
		{
			if (_pending.Count > 0)
			{
				return _pending.Peek();
			}
			Volatile.Write(ref _hasPending, 0);
			return null;
		}
	}

	internal void CompleteProcessed(TContext context)
	{
		lock (_sync)
		{
			if (_pending.Count > 0 && ReferenceEquals(_pending.Peek(), context))
			{
				_pending.Dequeue();
			}
			if (_pending.Count == 0)
			{
				Volatile.Write(ref _hasPending, 0);
			}
		}
	}

	internal void CancelAll()
	{
		List<TContext> abandoned;
		lock (_sync)
		{
			abandoned = new List<TContext>(_pending);
			_pending.Clear();
			Volatile.Write(ref _hasPending, 0);
		}
		foreach (TContext context in abandoned)
		{
			_complete(context, _newCanceledResult());
		}
	}

	internal void Complete(TContext context, TResult result)
	{
		_complete(context, result);
	}
}
