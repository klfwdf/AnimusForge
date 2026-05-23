using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000560 RID: 1376
	internal class SystemThreadingTasks_TaskDebugView
	{
		// Token: 0x06004149 RID: 16713 RVA: 0x000F3DF0 File Offset: 0x000F1FF0
		public SystemThreadingTasks_TaskDebugView(Task task)
		{
			this.m_task = task;
		}

		// Token: 0x170009B8 RID: 2488
		// (get) Token: 0x0600414A RID: 16714 RVA: 0x000F3DFF File Offset: 0x000F1FFF
		public object AsyncState
		{
			get
			{
				return this.m_task.AsyncState;
			}
		}

		// Token: 0x170009B9 RID: 2489
		// (get) Token: 0x0600414B RID: 16715 RVA: 0x000F3E0C File Offset: 0x000F200C
		public TaskCreationOptions CreationOptions
		{
			get
			{
				return this.m_task.CreationOptions;
			}
		}

		// Token: 0x170009BA RID: 2490
		// (get) Token: 0x0600414C RID: 16716 RVA: 0x000F3E19 File Offset: 0x000F2019
		public Exception Exception
		{
			get
			{
				return this.m_task.Exception;
			}
		}

		// Token: 0x170009BB RID: 2491
		// (get) Token: 0x0600414D RID: 16717 RVA: 0x000F3E26 File Offset: 0x000F2026
		public int Id
		{
			get
			{
				return this.m_task.Id;
			}
		}

		// Token: 0x170009BC RID: 2492
		// (get) Token: 0x0600414E RID: 16718 RVA: 0x000F3E34 File Offset: 0x000F2034
		public bool CancellationPending
		{
			get
			{
				return this.m_task.Status == TaskStatus.WaitingToRun && this.m_task.CancellationToken.IsCancellationRequested;
			}
		}

		// Token: 0x170009BD RID: 2493
		// (get) Token: 0x0600414F RID: 16719 RVA: 0x000F3E64 File Offset: 0x000F2064
		public TaskStatus Status
		{
			get
			{
				return this.m_task.Status;
			}
		}

		// Token: 0x04001B22 RID: 6946
		private Task m_task;
	}
}
