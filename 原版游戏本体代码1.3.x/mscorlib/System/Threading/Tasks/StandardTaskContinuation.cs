using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200056F RID: 1391
	internal class StandardTaskContinuation : TaskContinuation
	{
		// Token: 0x06004172 RID: 16754 RVA: 0x000F45F0 File Offset: 0x000F27F0
		internal StandardTaskContinuation(Task task, TaskContinuationOptions options, TaskScheduler scheduler)
		{
			this.m_task = task;
			this.m_options = options;
			this.m_taskScheduler = scheduler;
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.m_task.Id, "Task.ContinueWith: " + ((Delegate)task.m_action).Method.Name, 0UL);
			}
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.AddToActiveTasks(this.m_task);
			}
		}

		// Token: 0x06004173 RID: 16755 RVA: 0x000F4664 File Offset: 0x000F2864
		internal override void Run(Task completedTask, bool bCanInlineContinuationTask)
		{
			TaskContinuationOptions options = this.m_options;
			bool flag = (completedTask.IsRanToCompletion ? ((options & TaskContinuationOptions.NotOnRanToCompletion) == TaskContinuationOptions.None) : (completedTask.IsCanceled ? ((options & TaskContinuationOptions.NotOnCanceled) == TaskContinuationOptions.None) : ((options & TaskContinuationOptions.NotOnFaulted) == TaskContinuationOptions.None)));
			Task task = this.m_task;
			if (flag)
			{
				if (!task.IsCanceled && AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, task.Id, CausalityRelation.AssignDelegate);
				}
				task.m_taskScheduler = this.m_taskScheduler;
				if (bCanInlineContinuationTask && (options & TaskContinuationOptions.ExecuteSynchronously) != TaskContinuationOptions.None)
				{
					TaskContinuation.InlineIfPossibleOrElseQueue(task, true);
					return;
				}
				try
				{
					task.ScheduleAndStart(true);
					return;
				}
				catch (TaskSchedulerException)
				{
					return;
				}
			}
			task.InternalCancel(false);
		}

		// Token: 0x06004174 RID: 16756 RVA: 0x000F4718 File Offset: 0x000F2918
		internal override Delegate[] GetDelegateContinuationsForDebugger()
		{
			if (this.m_task.m_action == null)
			{
				return this.m_task.GetDelegateContinuationsForDebugger();
			}
			return new Delegate[] { this.m_task.m_action as Delegate };
		}

		// Token: 0x04001B57 RID: 6999
		internal readonly Task m_task;

		// Token: 0x04001B58 RID: 7000
		internal readonly TaskContinuationOptions m_options;

		// Token: 0x04001B59 RID: 7001
		private readonly TaskScheduler m_taskScheduler;
	}
}
