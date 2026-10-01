using System.Collections.Generic;
using System.Threading;
using System;

namespace AnimusForge;

/// <summary>
/// Owns queue and worker-lease state for the Scene speech pipeline. Payload execution,
/// Bannerlord access, timing, audio, history, and action side effects remain with the host.
/// </summary>
internal sealed class SceneSpeechQueueOwner<T> where T : class
{
	private readonly object _gate = new object();

	private readonly Queue<T> _queue = new Queue<T>();

	private bool _workerRunning;

	private long _generation;

	private readonly HashSet<Action> _pendingDispatches = new HashSet<Action>();

	/// <summary>
	/// Enqueues one item and returns true only to the caller that must start the worker.
	/// </summary>
	internal bool EnqueueAndTryStartWorker(T item) => EnqueueAndTryStartWorker(item, out _);

	internal bool EnqueueAndTryStartWorker(T item, out long generation)
	{
		generation = 0;
		if (item == null)
		{
			return false;
		}

		lock (_gate)
		{
			generation = _generation;
			_queue.Enqueue(item);
			if (_workerRunning)
			{
				return false;
			}

			_workerRunning = true;
			return true;
		}
	}

	/// <summary>
	/// Dequeues the next item, or atomically retires the worker when the queue is empty.
	/// </summary>
	internal bool TryDequeueOrStopWorker(out T item)
	{
		lock (_gate) return TryDequeueOrStopWorker(_generation, out item);
	}

	internal bool TryDequeueOrStopWorker(long generation, out T item)
	{
		lock (_gate)
		{
			if (generation != _generation)
			{
				item = default;
				return false;
			}
			if (_queue.Count == 0)
			{
				_workerRunning = false;
				item = default;
				return false;
			}

			item = _queue.Dequeue();
			return true;
		}
	}

	internal void StopWorker()
	{
		lock (_gate) StopWorker(_generation);
	}

	internal void StopWorker(long generation)
	{
		lock (_gate)
		{
			if (generation == _generation) _workerRunning = false;
		}
	}

	internal bool IsCurrentGeneration(long generation)
	{
		lock (_gate) return generation == _generation;
	}

	internal bool TryRegisterDispatch(long generation, Action retire)
	{
		lock (_gate)
		{
			if (generation != _generation) return false;
			_pendingDispatches.Add(retire);
			return true;
		}
	}

	internal bool TryClaimDispatch(long generation, Action retire)
	{
		lock (_gate)
			return generation == _generation && _pendingDispatches.Remove(retire);
	}

	/// <summary>
	/// Drops queued lines while preserving the running worker lease, matching the existing
	/// conversation-epoch cancellation behavior.
	/// </summary>
	internal T[] ClearQueued()
	{
		lock (_gate)
		{
			T[] dropped = _queue.ToArray();
			_queue.Clear();
			return dropped;
		}
	}

	internal T[] Reset()
	{
		T[] dropped;
		Action[] retired;
		lock (_gate)
		{
			_generation++;
			dropped = _queue.ToArray();
			_queue.Clear();
			_workerRunning = false;
			retired = new Action[_pendingDispatches.Count];
			_pendingDispatches.CopyTo(retired);
			_pendingDispatches.Clear();
		}
		// Completion callbacks never run while holding the admission/queue gate.
		foreach (Action retire in retired) retire();
		return dropped;
	}

	internal bool HasQueuedOrWorker()
	{
		lock (_gate)
		{
			return _queue.Count > 0 || _workerRunning;
		}
	}

	internal bool TryGetSnapshot(out int queueCount, out bool workerRunning)
	{
		queueCount = 0;
		workerRunning = false;
		if (!Monitor.TryEnter(_gate))
		{
			return false;
		}

		try
		{
			queueCount = _queue.Count;
			workerRunning = _workerRunning;
			return true;
		}
		finally
		{
			Monitor.Exit(_gate);
		}
	}
}
