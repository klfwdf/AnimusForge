using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002B RID: 43
	public class ThreadedClientSession : IClientSession
	{
		// Token: 0x060000E2 RID: 226 RVA: 0x0000372B File Offset: 0x0000192B
		public ThreadedClientSession(ThreadedClient threadedClient, IClientSession session, int threadSleepTime)
		{
			this._session = session;
			this._threadedClient = threadedClient;
			this._tasks = new Queue<ThreadedClientSessionTask>();
			this._task = null;
			this._tasBegunJob = false;
			this._threadSleepTime = threadSleepTime;
			this.RefreshTask(null);
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000376C File Offset: 0x0000196C
		private void RefreshTask(Task previousTask)
		{
			if (previousTask == null || previousTask.IsCompleted)
			{
				Task.Run(async delegate()
				{
					this.ThreadMain();
					await Task.Delay(this._threadSleepTime);
				}).ContinueWith(delegate(Task t)
				{
					this.RefreshTask(t);
				}, TaskContinuationOptions.ExecuteSynchronously);
				return;
			}
			if (previousTask.IsFaulted)
			{
				throw new Exception("ThreadedClientSession.ThreadMain Task is faulted", previousTask.Exception);
			}
			throw new Exception("RefreshTask is called before task is completed");
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x000037D0 File Offset: 0x000019D0
		private void ThreadMain()
		{
			this._session.Tick();
			if (!this._tasBegunJob)
			{
				Queue<ThreadedClientSessionTask> tasks = this._tasks;
				lock (tasks)
				{
					if (this._tasks.Count > 0)
					{
						this._task = this._tasks.Dequeue();
					}
				}
				if (this._task != null)
				{
					this._task.BeginJob();
					this._tasBegunJob = true;
				}
			}
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000385C File Offset: 0x00001A5C
		void IClientSession.Connect()
		{
			ThreadedClientSessionConnectTask item = new ThreadedClientSessionConnectTask(this._session);
			Queue<ThreadedClientSessionTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000038B0 File Offset: 0x00001AB0
		void IClientSession.Disconnect()
		{
			ThreadedClientSessionDisconnectTask item = new ThreadedClientSessionDisconnectTask(this._session);
			Queue<ThreadedClientSessionTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00003904 File Offset: 0x00001B04
		void IClientSession.Tick()
		{
			this._threadedClient.Tick();
			if (this._tasBegunJob)
			{
				this._task.DoMainThreadJob();
				if (this._task.Finished)
				{
					this._task = null;
					this._tasBegunJob = false;
				}
			}
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00003944 File Offset: 0x00001B44
		async Task<LoginResult> IClientSession.Login(LoginMessage message)
		{
			ThreadedClientSessionLoginTask task = new ThreadedClientSessionLoginTask(this._session, message);
			Queue<ThreadedClientSessionTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(task);
			}
			await task.Wait();
			return task.LoginResult;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00003994 File Offset: 0x00001B94
		void IClientSession.SendMessage(Message message)
		{
			ThreadedClientSessionMessageTask item = new ThreadedClientSessionMessageTask(this._session, message);
			Queue<ThreadedClientSessionTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000EA RID: 234 RVA: 0x000039E8 File Offset: 0x00001BE8
		async Task<TReturn> IClientSession.CallFunction<TReturn>(Message message)
		{
			ThreadedClientSessionFunctionTask task = new ThreadedClientSessionFunctionTask(this._session, message);
			Queue<ThreadedClientSessionTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(task);
			}
			await task.Wait();
			return (TReturn)((object)task.FunctionResult);
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00003A35 File Offset: 0x00001C35
		Task<bool> IClientSession.CheckConnection()
		{
			return this._session.CheckConnection();
		}

		// Token: 0x04000045 RID: 69
		private IClientSession _session;

		// Token: 0x04000046 RID: 70
		private ThreadedClient _threadedClient;

		// Token: 0x04000047 RID: 71
		private Queue<ThreadedClientSessionTask> _tasks;

		// Token: 0x04000048 RID: 72
		private ThreadedClientSessionTask _task;

		// Token: 0x04000049 RID: 73
		private volatile bool _tasBegunJob;

		// Token: 0x0400004A RID: 74
		private readonly int _threadSleepTime;
	}
}
