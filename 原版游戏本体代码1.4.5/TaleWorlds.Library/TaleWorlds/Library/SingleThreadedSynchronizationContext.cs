using System.Collections.Generic;
using System.Threading;

namespace TaleWorlds.Library;

public sealed class SingleThreadedSynchronizationContext : SynchronizationContext
{
	private struct WorkRequest
	{
		private readonly SendOrPostCallback _callback;

		private readonly object _state;

		private readonly ManualResetEvent _waitHandle;

		public WorkRequest(SendOrPostCallback callback, object state, ManualResetEvent waitHandle = null)
		{
			_callback = callback;
			_state = state;
			_waitHandle = waitHandle;
		}

		public void Invoke()
		{
			_callback.DynamicInvokeWithLog(_state);
			_waitHandle?.Set();
		}
	}

	private List<WorkRequest> _futureWorks;

	private List<WorkRequest> _currentWorks;

	private readonly object _worksLock;

	private readonly int _mainThreadId;

	public SingleThreadedSynchronizationContext()
	{
		_worksLock = new object();
		_futureWorks = new List<WorkRequest>(100);
		_currentWorks = new List<WorkRequest>(100);
		_mainThreadId = Thread.CurrentThread.ManagedThreadId;
	}

	public override void Send(SendOrPostCallback callback, object state)
	{
		if (_mainThreadId == Thread.CurrentThread.ManagedThreadId)
		{
			callback.DynamicInvokeWithLog(state);
			return;
		}
		using ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
		lock (_worksLock)
		{
			_futureWorks.Add(new WorkRequest(callback, state, manualResetEvent));
		}
		manualResetEvent.WaitOne();
	}

	public override void Post(SendOrPostCallback callback, object state)
	{
		WorkRequest item = new WorkRequest(callback, state);
		lock (_worksLock)
		{
			_futureWorks.Add(item);
		}
	}

	public void Tick()
	{
		lock (_worksLock)
		{
			List<WorkRequest> currentWorks = _currentWorks;
			_currentWorks = _futureWorks;
			_futureWorks = currentWorks;
		}
		foreach (WorkRequest currentWork in _currentWorks)
		{
			currentWork.Invoke();
		}
		_currentWorks.Clear();
	}
}
