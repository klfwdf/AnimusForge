using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000570 RID: 1392
	internal sealed class SynchronizationContextAwaitTaskContinuation : AwaitTaskContinuation
	{
		// Token: 0x06004175 RID: 16757 RVA: 0x000F474C File Offset: 0x000F294C
		[SecurityCritical]
		internal SynchronizationContextAwaitTaskContinuation(SynchronizationContext context, Action action, bool flowExecutionContext, ref StackCrawlMark stackMark)
			: base(action, flowExecutionContext, ref stackMark)
		{
			this.m_syncContext = context;
		}

		// Token: 0x06004176 RID: 16758 RVA: 0x000F4760 File Offset: 0x000F2960
		[SecuritySafeCritical]
		internal sealed override void Run(Task task, bool canInlineContinuationTask)
		{
			if (canInlineContinuationTask && this.m_syncContext == SynchronizationContext.CurrentNoFlow)
			{
				base.RunCallback(AwaitTaskContinuation.GetInvokeActionCallback(), this.m_action, ref Task.t_currentTask);
				return;
			}
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.IsEnabled())
			{
				this.m_continuationId = Task.NewId();
				log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, this.m_continuationId);
			}
			base.RunCallback(SynchronizationContextAwaitTaskContinuation.GetPostActionCallback(), this, ref Task.t_currentTask);
		}

		// Token: 0x06004177 RID: 16759 RVA: 0x000F47E4 File Offset: 0x000F29E4
		[SecurityCritical]
		private static void PostAction(object state)
		{
			SynchronizationContextAwaitTaskContinuation synchronizationContextAwaitTaskContinuation = (SynchronizationContextAwaitTaskContinuation)state;
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.TasksSetActivityIds && synchronizationContextAwaitTaskContinuation.m_continuationId != 0)
			{
				synchronizationContextAwaitTaskContinuation.m_syncContext.Post(SynchronizationContextAwaitTaskContinuation.s_postCallback, SynchronizationContextAwaitTaskContinuation.GetActionLogDelegate(synchronizationContextAwaitTaskContinuation.m_continuationId, synchronizationContextAwaitTaskContinuation.m_action));
				return;
			}
			synchronizationContextAwaitTaskContinuation.m_syncContext.Post(SynchronizationContextAwaitTaskContinuation.s_postCallback, synchronizationContextAwaitTaskContinuation.m_action);
		}

		// Token: 0x06004178 RID: 16760 RVA: 0x000F4848 File Offset: 0x000F2A48
		private static Action GetActionLogDelegate(int continuationId, Action action)
		{
			return delegate()
			{
				Guid activityId = TplEtwProvider.CreateGuidForTaskID(continuationId);
				Guid currentThreadActivityId;
				EventSource.SetCurrentThreadActivityId(activityId, out currentThreadActivityId);
				try
				{
					action();
				}
				finally
				{
					EventSource.SetCurrentThreadActivityId(currentThreadActivityId);
				}
			};
		}

		// Token: 0x06004179 RID: 16761 RVA: 0x000F4878 File Offset: 0x000F2A78
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ContextCallback GetPostActionCallback()
		{
			ContextCallback contextCallback = SynchronizationContextAwaitTaskContinuation.s_postActionCallback;
			if (contextCallback == null)
			{
				contextCallback = (SynchronizationContextAwaitTaskContinuation.s_postActionCallback = new ContextCallback(SynchronizationContextAwaitTaskContinuation.PostAction));
			}
			return contextCallback;
		}

		// Token: 0x04001B5A RID: 7002
		private static readonly SendOrPostCallback s_postCallback = delegate(object state)
		{
			((Action)state)();
		};

		// Token: 0x04001B5B RID: 7003
		[SecurityCritical]
		private static ContextCallback s_postActionCallback;

		// Token: 0x04001B5C RID: 7004
		private readonly SynchronizationContext m_syncContext;
	}
}
