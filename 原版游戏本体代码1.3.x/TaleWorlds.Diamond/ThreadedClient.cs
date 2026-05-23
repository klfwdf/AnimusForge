using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000025 RID: 37
	public class ThreadedClient : IClient
	{
		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000CA RID: 202 RVA: 0x00003497 File Offset: 0x00001697
		public ILoginAccessProvider AccessProvider
		{
			get
			{
				return this._client.AccessProvider;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000CB RID: 203 RVA: 0x000034A4 File Offset: 0x000016A4
		public bool IsInCriticalState
		{
			get
			{
				return this._client.IsInCriticalState;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000CC RID: 204 RVA: 0x000034B1 File Offset: 0x000016B1
		public long AliveCheckTimeInMiliSeconds
		{
			get
			{
				return this._client.AliveCheckTimeInMiliSeconds;
			}
		}

		// Token: 0x060000CD RID: 205 RVA: 0x000034BE File Offset: 0x000016BE
		public ThreadedClient(IClient client)
		{
			this._client = client;
			this._tasks = new Queue<ThreadedClientTask>();
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000034D8 File Offset: 0x000016D8
		public void Tick()
		{
			ThreadedClientTask threadedClientTask = null;
			Queue<ThreadedClientTask> tasks = this._tasks;
			lock (tasks)
			{
				if (this._tasks.Count > 0)
				{
					threadedClientTask = this._tasks.Dequeue();
				}
			}
			if (threadedClientTask != null)
			{
				threadedClientTask.DoJob();
			}
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00003538 File Offset: 0x00001738
		void IClient.HandleMessage(Message message)
		{
			ThreadedClientHandleMessageTask item = new ThreadedClientHandleMessageTask(this._client, message);
			Queue<ThreadedClientTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000358C File Offset: 0x0000178C
		void IClient.OnConnected()
		{
			ThreadedClientConnectedTask item = new ThreadedClientConnectedTask(this._client);
			Queue<ThreadedClientTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x000035E0 File Offset: 0x000017E0
		void IClient.OnDisconnected()
		{
			ThreadedClientDisconnectedTask item = new ThreadedClientDisconnectedTask(this._client);
			Queue<ThreadedClientTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00003634 File Offset: 0x00001834
		void IClient.OnCantConnect()
		{
			ThreadedClientCantConnectTask item = new ThreadedClientCantConnectTask(this._client);
			Queue<ThreadedClientTask> tasks = this._tasks;
			lock (tasks)
			{
				this._tasks.Enqueue(item);
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00003688 File Offset: 0x00001888
		public Task<bool> CheckConnection()
		{
			return this._client.CheckConnection();
		}

		// Token: 0x04000041 RID: 65
		private IClient _client;

		// Token: 0x04000042 RID: 66
		private Queue<ThreadedClientTask> _tasks;
	}
}
