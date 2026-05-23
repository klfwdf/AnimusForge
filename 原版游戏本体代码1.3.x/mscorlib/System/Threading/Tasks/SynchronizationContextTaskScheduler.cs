using System;
using System.Collections.Generic;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000578 RID: 1400
	internal sealed class SynchronizationContextTaskScheduler : TaskScheduler
	{
		// Token: 0x06004219 RID: 16921 RVA: 0x000F6538 File Offset: 0x000F4738
		internal SynchronizationContextTaskScheduler()
		{
			SynchronizationContext synchronizationContext = SynchronizationContext.Current;
			if (synchronizationContext == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_FromCurrentSynchronizationContext_NoCurrent"));
			}
			this.m_synchronizationContext = synchronizationContext;
		}

		// Token: 0x0600421A RID: 16922 RVA: 0x000F656B File Offset: 0x000F476B
		[SecurityCritical]
		protected internal override void QueueTask(Task task)
		{
			this.m_synchronizationContext.Post(SynchronizationContextTaskScheduler.s_postCallback, task);
		}

		// Token: 0x0600421B RID: 16923 RVA: 0x000F657E File Offset: 0x000F477E
		[SecurityCritical]
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return SynchronizationContext.Current == this.m_synchronizationContext && base.TryExecuteTask(task);
		}

		// Token: 0x0600421C RID: 16924 RVA: 0x000F6596 File Offset: 0x000F4796
		[SecurityCritical]
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return null;
		}

		// Token: 0x170009CF RID: 2511
		// (get) Token: 0x0600421D RID: 16925 RVA: 0x000F6599 File Offset: 0x000F4799
		public override int MaximumConcurrencyLevel
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x0600421E RID: 16926 RVA: 0x000F659C File Offset: 0x000F479C
		private static void PostCallback(object obj)
		{
			Task task = (Task)obj;
			task.ExecuteEntry(true);
		}

		// Token: 0x04001B74 RID: 7028
		private SynchronizationContext m_synchronizationContext;

		// Token: 0x04001B75 RID: 7029
		private static SendOrPostCallback s_postCallback = new SendOrPostCallback(SynchronizationContextTaskScheduler.PostCallback);
	}
}
