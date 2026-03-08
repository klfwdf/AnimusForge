using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200051C RID: 1308
	internal sealed class ThreadPoolWorkQueueThreadLocals
	{
		// Token: 0x06003DCA RID: 15818 RVA: 0x000E7272 File Offset: 0x000E5472
		public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
		{
			this.workQueue = tpq;
			this.workStealingQueue = new ThreadPoolWorkQueue.WorkStealingQueue();
			ThreadPoolWorkQueue.allThreadQueues.Add(this.workStealingQueue);
		}

		// Token: 0x06003DCB RID: 15819 RVA: 0x000E72B4 File Offset: 0x000E54B4
		[SecurityCritical]
		private void CleanUp()
		{
			if (this.workStealingQueue != null)
			{
				if (this.workQueue != null)
				{
					bool flag = false;
					while (!flag)
					{
						try
						{
						}
						finally
						{
							IThreadPoolWorkItem callback = null;
							if (this.workStealingQueue.LocalPop(out callback))
							{
								this.workQueue.Enqueue(callback, true);
							}
							else
							{
								flag = true;
							}
						}
					}
				}
				ThreadPoolWorkQueue.allThreadQueues.Remove(this.workStealingQueue);
			}
		}

		// Token: 0x06003DCC RID: 15820 RVA: 0x000E7320 File Offset: 0x000E5520
		[SecuritySafeCritical]
		~ThreadPoolWorkQueueThreadLocals()
		{
			if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
			{
				this.CleanUp();
			}
		}

		// Token: 0x04001A0C RID: 6668
		[ThreadStatic]
		[SecurityCritical]
		public static ThreadPoolWorkQueueThreadLocals threadLocals;

		// Token: 0x04001A0D RID: 6669
		public readonly ThreadPoolWorkQueue workQueue;

		// Token: 0x04001A0E RID: 6670
		public readonly ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue;

		// Token: 0x04001A0F RID: 6671
		public readonly Random random = new Random(Thread.CurrentThread.ManagedThreadId);
	}
}
