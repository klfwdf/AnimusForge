using System.Collections.Generic;
using System.Threading;

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

	/// <summary>
	/// Enqueues one item and returns true only to the caller that must start the worker.
	/// </summary>
	internal bool EnqueueAndTryStartWorker(T item)
	{
		if (item == null)
		{
			return false;
		}

		lock (_gate)
		{
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
		lock (_gate)
		{
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
		lock (_gate)
		{
			_workerRunning = false;
		}
	}

	/// <summary>
	/// Drops queued lines while preserving the running worker lease, matching the existing
	/// conversation-epoch cancellation behavior.
	/// </summary>
	internal void ClearQueued()
	{
		lock (_gate)
		{
			_queue.Clear();
		}
	}

	internal void Reset()
	{
		lock (_gate)
		{
			_queue.Clear();
			_workerRunning = false;
		}
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
