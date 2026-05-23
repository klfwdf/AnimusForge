using System;
using System.Collections.Generic;
using System.Threading;

namespace TaleWorlds.Library
{
	// Token: 0x0200008E RID: 142
	public sealed class SingleThreadedSynchronizationContext : SynchronizationContext
	{
		// Token: 0x06000509 RID: 1289 RVA: 0x0001239A File Offset: 0x0001059A
		public SingleThreadedSynchronizationContext()
		{
			this._worksLock = new object();
			this._futureWorks = new List<SingleThreadedSynchronizationContext.WorkRequest>(100);
			this._currentWorks = new List<SingleThreadedSynchronizationContext.WorkRequest>(100);
			this._mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x000123D8 File Offset: 0x000105D8
		public override void Send(SendOrPostCallback callback, object state)
		{
			if (this._mainThreadId == Thread.CurrentThread.ManagedThreadId)
			{
				callback.DynamicInvokeWithLog(new object[] { state });
				return;
			}
			using (ManualResetEvent manualResetEvent = new ManualResetEvent(false))
			{
				object worksLock = this._worksLock;
				lock (worksLock)
				{
					this._futureWorks.Add(new SingleThreadedSynchronizationContext.WorkRequest(callback, state, manualResetEvent));
				}
				manualResetEvent.WaitOne();
			}
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x00012470 File Offset: 0x00010670
		public override void Post(SendOrPostCallback callback, object state)
		{
			SingleThreadedSynchronizationContext.WorkRequest item = new SingleThreadedSynchronizationContext.WorkRequest(callback, state, null);
			object worksLock = this._worksLock;
			lock (worksLock)
			{
				this._futureWorks.Add(item);
			}
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x000124C0 File Offset: 0x000106C0
		public void Tick()
		{
			object worksLock = this._worksLock;
			lock (worksLock)
			{
				List<SingleThreadedSynchronizationContext.WorkRequest> currentWorks = this._currentWorks;
				this._currentWorks = this._futureWorks;
				this._futureWorks = currentWorks;
			}
			foreach (SingleThreadedSynchronizationContext.WorkRequest workRequest in this._currentWorks)
			{
				workRequest.Invoke();
			}
			this._currentWorks.Clear();
		}

		// Token: 0x04000191 RID: 401
		private List<SingleThreadedSynchronizationContext.WorkRequest> _futureWorks;

		// Token: 0x04000192 RID: 402
		private List<SingleThreadedSynchronizationContext.WorkRequest> _currentWorks;

		// Token: 0x04000193 RID: 403
		private readonly object _worksLock;

		// Token: 0x04000194 RID: 404
		private readonly int _mainThreadId;

		// Token: 0x020000E3 RID: 227
		private struct WorkRequest
		{
			// Token: 0x0600079E RID: 1950 RVA: 0x00019283 File Offset: 0x00017483
			public WorkRequest(SendOrPostCallback callback, object state, ManualResetEvent waitHandle = null)
			{
				this._callback = callback;
				this._state = state;
				this._waitHandle = waitHandle;
			}

			// Token: 0x0600079F RID: 1951 RVA: 0x0001929A File Offset: 0x0001749A
			public void Invoke()
			{
				this._callback.DynamicInvokeWithLog(new object[] { this._state });
				ManualResetEvent waitHandle = this._waitHandle;
				if (waitHandle == null)
				{
					return;
				}
				waitHandle.Set();
			}

			// Token: 0x040002EA RID: 746
			private readonly SendOrPostCallback _callback;

			// Token: 0x040002EB RID: 747
			private readonly object _state;

			// Token: 0x040002EC RID: 748
			private readonly ManualResetEvent _waitHandle;
		}
	}
}
