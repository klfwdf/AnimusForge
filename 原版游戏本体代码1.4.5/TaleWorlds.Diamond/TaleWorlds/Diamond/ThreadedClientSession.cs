using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public class ThreadedClientSession : IClientSession
{
	private IClientSession _session;

	private ThreadedClient _threadedClient;

	private Queue<ThreadedClientSessionTask> _tasks;

	private ThreadedClientSessionTask _task;

	private volatile bool _tasBegunJob;

	private readonly int _threadSleepTime;

	public ThreadedClientSession(ThreadedClient threadedClient, IClientSession session, int threadSleepTime)
	{
		_session = session;
		_threadedClient = threadedClient;
		_tasks = new Queue<ThreadedClientSessionTask>();
		_task = null;
		_tasBegunJob = false;
		_threadSleepTime = threadSleepTime;
		RefreshTask(null);
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
		if (_tasBegunJob)
		{
			return;
		}
		lock (_tasks)
		{
			if (_tasks.Count > 0)
			{
				_task = _tasks.Dequeue();
			}
		}
		if (_task != null)
		{
			_task.BeginJob();
			_tasBegunJob = true;
		}
	}

	void IClientSession.Connect()
	{
		ThreadedClientSessionConnectTask item = new ThreadedClientSessionConnectTask(_session);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	void IClientSession.Disconnect()
	{
		ThreadedClientSessionDisconnectTask item = new ThreadedClientSessionDisconnectTask(_session);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	void IClientSession.Tick()
	{
		_threadedClient.Tick();
		if (_tasBegunJob)
		{
			_task.DoMainThreadJob();
			if (_task.Finished)
			{
				_task = null;
				_tasBegunJob = false;
			}
		}
	}

	async Task<LoginResult> IClientSession.Login(LoginMessage message)
	{
		ThreadedClientSessionLoginTask task = new ThreadedClientSessionLoginTask(_session, message);
		lock (_tasks)
		{
			_tasks.Enqueue(task);
		}
		await task.Wait();
		return task.LoginResult;
	}

	void IClientSession.SendMessage(Message message)
	{
		ThreadedClientSessionMessageTask item = new ThreadedClientSessionMessageTask(_session, message);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	async Task<TReturn> IClientSession.CallFunction<TReturn>(Message message)
	{
		ThreadedClientSessionFunctionTask task = new ThreadedClientSessionFunctionTask(_session, message);
		lock (_tasks)
		{
			_tasks.Enqueue(task);
		}
		await task.Wait();
		return (TReturn)task.FunctionResult;
	}

	Task<bool> IClientSession.CheckConnection()
	{
		return _session.CheckConnection();
	}
}
