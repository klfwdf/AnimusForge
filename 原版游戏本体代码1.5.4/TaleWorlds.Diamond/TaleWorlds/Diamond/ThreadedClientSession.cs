using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public class ThreadedClientSession : IClientSession
{
	private IClientSession _session;

	private ThreadedClientSessionTask _task;

	private volatile bool _taskBegunJob;

	private readonly int _threadSleepTime;

	private ConcurrentQueue<Action> _eventQueue = new ConcurrentQueue<Action>();

	private ConcurrentQueue<ThreadedClientSessionTask> _tasks = new ConcurrentQueue<ThreadedClientSessionTask>();

	public int AliveCheckInterval
	{
		set
		{
			_session.AliveCheckInterval = value;
		}
	}

	public int MaxConsecutiveFailuresBeforeDisconnect
	{
		set
		{
			_session.MaxConsecutiveFailuresBeforeDisconnect = value;
		}
	}

	public string Address
	{
		get
		{
			return _session.Address;
		}
		set
		{
			_session.Address = value;
		}
	}

	public event MessageHandledDelegate MessageReceived;

	public event ConnectedDelegate Connected;

	public event DisconnectedDelegate Disconnected;

	public event OnCantConnectDelegate ConnectionFailed;

	public ThreadedClientSession(IClientSession session, int threadSleepTime)
	{
		_session = session;
		_session.Connected += SessionConnected;
		_session.Disconnected += SessionDisconnected;
		_session.ConnectionFailed += SessionConnectionFailed;
		_session.MessageReceived += SessionMessageReceived;
		_task = null;
		_taskBegunJob = false;
		_threadSleepTime = threadSleepTime;
		RefreshTask(null);
	}

	private void SessionConnected()
	{
		_eventQueue.Enqueue(delegate
		{
			this.Connected?.Invoke();
		});
	}

	private void SessionDisconnected()
	{
		_eventQueue.Enqueue(delegate
		{
			this.Disconnected?.Invoke();
		});
	}

	private void SessionConnectionFailed()
	{
		_eventQueue.Enqueue(delegate
		{
			this.ConnectionFailed?.Invoke();
		});
	}

	private void SessionMessageReceived(Message message)
	{
		_eventQueue.Enqueue(delegate
		{
			this.MessageReceived?.Invoke(message);
		});
	}

	private void RefreshTask(Task previousTask)
	{
		if (previousTask == null || previousTask.IsCompleted)
		{
			Task.Run(async delegate
			{
				ThreadMain();
				await Task.Delay(_threadSleepTime);
			}).ContinueWith(delegate(Task t)
			{
				RefreshTask(t);
			}, TaskContinuationOptions.ExecuteSynchronously);
			return;
		}
		if (previousTask.IsFaulted)
		{
			throw new Exception("ThreadedClientSession.ThreadMain Task is faulted", previousTask.Exception);
		}
		throw new Exception("RefreshTask is called before task is completed");
	}

	private void ThreadMain()
	{
		_session.Tick();
		if (!_taskBegunJob && _tasks.TryDequeue(out _task))
		{
			_task.BeginJob();
			_taskBegunJob = true;
		}
	}

	void IClientSession.Connect()
	{
		ThreadedClientSessionConnectTask item = new ThreadedClientSessionConnectTask(_session);
		_tasks.Enqueue(item);
	}

	void IClientSession.Disconnect()
	{
		ThreadedClientSessionDisconnectTask item = new ThreadedClientSessionDisconnectTask(_session);
		_tasks.Enqueue(item);
	}

	void IClientSession.Tick()
	{
		if (_taskBegunJob)
		{
			_task.DoMainThreadJob();
			if (_task.Finished)
			{
				_task = null;
				_taskBegunJob = false;
			}
		}
		Action result;
		while (_eventQueue.TryDequeue(out result))
		{
			result();
		}
	}

	async Task<LoginResult> IClientSession.Login(LoginMessage message)
	{
		ThreadedClientSessionLoginTask task = new ThreadedClientSessionLoginTask(_session, message);
		_tasks.Enqueue(task);
		await task.Wait();
		return task.LoginResult;
	}

	void IClientSession.SendMessage(Message message)
	{
		ThreadedClientSessionMessageTask item = new ThreadedClientSessionMessageTask(_session, message);
		_tasks.Enqueue(item);
	}

	async Task<CallResult> IClientSession.CallFunction<TReturn>(Message message)
	{
		ThreadedClientSessionFunctionTask task = new ThreadedClientSessionFunctionTask(_session, message);
		_tasks.Enqueue(task);
		await task.Wait();
		return task.CallResult;
	}

	Task<bool> IClientSession.CheckConnection()
	{
		return _session.CheckConnection();
	}
}
