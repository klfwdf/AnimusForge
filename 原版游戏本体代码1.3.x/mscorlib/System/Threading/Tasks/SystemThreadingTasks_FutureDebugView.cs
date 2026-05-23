using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200054D RID: 1357
	internal class SystemThreadingTasks_FutureDebugView<TResult>
	{
		// Token: 0x06003FCA RID: 16330 RVA: 0x000ED08F File Offset: 0x000EB28F
		public SystemThreadingTasks_FutureDebugView(Task<TResult> task)
		{
			this.m_task = task;
		}

		// Token: 0x17000970 RID: 2416
		// (get) Token: 0x06003FCB RID: 16331 RVA: 0x000ED0A0 File Offset: 0x000EB2A0
		public TResult Result
		{
			get
			{
				if (this.m_task.Status != TaskStatus.RanToCompletion)
				{
					return default(TResult);
				}
				return this.m_task.Result;
			}
		}

		// Token: 0x17000971 RID: 2417
		// (get) Token: 0x06003FCC RID: 16332 RVA: 0x000ED0D0 File Offset: 0x000EB2D0
		public object AsyncState
		{
			get
			{
				return this.m_task.AsyncState;
			}
		}

		// Token: 0x17000972 RID: 2418
		// (get) Token: 0x06003FCD RID: 16333 RVA: 0x000ED0DD File Offset: 0x000EB2DD
		public TaskCreationOptions CreationOptions
		{
			get
			{
				return this.m_task.CreationOptions;
			}
		}

		// Token: 0x17000973 RID: 2419
		// (get) Token: 0x06003FCE RID: 16334 RVA: 0x000ED0EA File Offset: 0x000EB2EA
		public Exception Exception
		{
			get
			{
				return this.m_task.Exception;
			}
		}

		// Token: 0x17000974 RID: 2420
		// (get) Token: 0x06003FCF RID: 16335 RVA: 0x000ED0F7 File Offset: 0x000EB2F7
		public int Id
		{
			get
			{
				return this.m_task.Id;
			}
		}

		// Token: 0x17000975 RID: 2421
		// (get) Token: 0x06003FD0 RID: 16336 RVA: 0x000ED104 File Offset: 0x000EB304
		public bool CancellationPending
		{
			get
			{
				return this.m_task.Status == TaskStatus.WaitingToRun && this.m_task.CancellationToken.IsCancellationRequested;
			}
		}

		// Token: 0x17000976 RID: 2422
		// (get) Token: 0x06003FD1 RID: 16337 RVA: 0x000ED134 File Offset: 0x000EB334
		public TaskStatus Status
		{
			get
			{
				return this.m_task.Status;
			}
		}

		// Token: 0x04001AC2 RID: 6850
		private Task<TResult> m_task;
	}
}
