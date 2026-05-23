using System;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000571 RID: 1393
	internal sealed class TaskSchedulerAwaitTaskContinuation : AwaitTaskContinuation
	{
		// Token: 0x0600417B RID: 16763 RVA: 0x000F48B9 File Offset: 0x000F2AB9
		[SecurityCritical]
		internal TaskSchedulerAwaitTaskContinuation(TaskScheduler scheduler, Action action, bool flowExecutionContext, ref StackCrawlMark stackMark)
			: base(action, flowExecutionContext, ref stackMark)
		{
			this.m_scheduler = scheduler;
		}

		// Token: 0x0600417C RID: 16764 RVA: 0x000F48CC File Offset: 0x000F2ACC
		internal sealed override void Run(Task ignored, bool canInlineContinuationTask)
		{
			if (this.m_scheduler == TaskScheduler.Default)
			{
				base.Run(ignored, canInlineContinuationTask);
				return;
			}
			bool flag = canInlineContinuationTask && (TaskScheduler.InternalCurrent == this.m_scheduler || Thread.CurrentThread.IsThreadPoolThread);
			Task task = base.CreateTask(delegate(object state)
			{
				try
				{
					((Action)state)();
				}
				catch (Exception exc)
				{
					AwaitTaskContinuation.ThrowAsyncIfNecessary(exc);
				}
			}, this.m_action, this.m_scheduler);
			if (flag)
			{
				TaskContinuation.InlineIfPossibleOrElseQueue(task, false);
				return;
			}
			try
			{
				task.ScheduleAndStart(false);
			}
			catch (TaskSchedulerException)
			{
			}
		}

		// Token: 0x04001B5D RID: 7005
		private readonly TaskScheduler m_scheduler;
	}
}
