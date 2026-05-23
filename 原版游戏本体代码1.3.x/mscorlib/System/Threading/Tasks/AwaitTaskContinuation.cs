using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000572 RID: 1394
	internal class AwaitTaskContinuation : TaskContinuation, IThreadPoolWorkItem
	{
		// Token: 0x0600417D RID: 16765 RVA: 0x000F496C File Offset: 0x000F2B6C
		[SecurityCritical]
		internal AwaitTaskContinuation(Action action, bool flowExecutionContext, ref StackCrawlMark stackMark)
		{
			this.m_action = action;
			if (flowExecutionContext)
			{
				this.m_capturedContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
			}
		}

		// Token: 0x0600417E RID: 16766 RVA: 0x000F498B File Offset: 0x000F2B8B
		[SecurityCritical]
		internal AwaitTaskContinuation(Action action, bool flowExecutionContext)
		{
			this.m_action = action;
			if (flowExecutionContext)
			{
				this.m_capturedContext = ExecutionContext.FastCapture();
			}
		}

		// Token: 0x0600417F RID: 16767 RVA: 0x000F49A8 File Offset: 0x000F2BA8
		protected Task CreateTask(Action<object> action, object state, TaskScheduler scheduler)
		{
			return new Task(action, state, null, default(CancellationToken), TaskCreationOptions.None, InternalTaskOptions.QueuedByRuntime, scheduler)
			{
				CapturedContext = this.m_capturedContext
			};
		}

		// Token: 0x06004180 RID: 16768 RVA: 0x000F49DC File Offset: 0x000F2BDC
		[SecuritySafeCritical]
		internal override void Run(Task task, bool canInlineContinuationTask)
		{
			if (canInlineContinuationTask && AwaitTaskContinuation.IsValidLocationForInlining)
			{
				this.RunCallback(AwaitTaskContinuation.GetInvokeActionCallback(), this.m_action, ref Task.t_currentTask);
				return;
			}
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.IsEnabled())
			{
				this.m_continuationId = Task.NewId();
				log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, this.m_continuationId);
			}
			ThreadPool.UnsafeQueueCustomWorkItem(this, false);
		}

		// Token: 0x170009C1 RID: 2497
		// (get) Token: 0x06004181 RID: 16769 RVA: 0x000F4A50 File Offset: 0x000F2C50
		internal static bool IsValidLocationForInlining
		{
			get
			{
				SynchronizationContext currentNoFlow = SynchronizationContext.CurrentNoFlow;
				if (currentNoFlow != null && currentNoFlow.GetType() != typeof(SynchronizationContext))
				{
					return false;
				}
				TaskScheduler internalCurrent = TaskScheduler.InternalCurrent;
				return internalCurrent == null || internalCurrent == TaskScheduler.Default;
			}
		}

		// Token: 0x06004182 RID: 16770 RVA: 0x000F4A94 File Offset: 0x000F2C94
		[SecurityCritical]
		private void ExecuteWorkItemHelper()
		{
			TplEtwProvider log = TplEtwProvider.Log;
			Guid empty = Guid.Empty;
			if (log.TasksSetActivityIds && this.m_continuationId != 0)
			{
				Guid activityId = TplEtwProvider.CreateGuidForTaskID(this.m_continuationId);
				EventSource.SetCurrentThreadActivityId(activityId, out empty);
			}
			try
			{
				if (this.m_capturedContext == null)
				{
					this.m_action();
				}
				else
				{
					try
					{
						ExecutionContext.Run(this.m_capturedContext, AwaitTaskContinuation.GetInvokeActionCallback(), this.m_action, true);
					}
					finally
					{
						this.m_capturedContext.Dispose();
					}
				}
			}
			finally
			{
				if (log.TasksSetActivityIds && this.m_continuationId != 0)
				{
					EventSource.SetCurrentThreadActivityId(empty);
				}
			}
		}

		// Token: 0x06004183 RID: 16771 RVA: 0x000F4B40 File Offset: 0x000F2D40
		[SecurityCritical]
		void IThreadPoolWorkItem.ExecuteWorkItem()
		{
			if (this.m_capturedContext == null && !TplEtwProvider.Log.IsEnabled())
			{
				this.m_action();
				return;
			}
			this.ExecuteWorkItemHelper();
		}

		// Token: 0x06004184 RID: 16772 RVA: 0x000F4B68 File Offset: 0x000F2D68
		[SecurityCritical]
		void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
		{
		}

		// Token: 0x06004185 RID: 16773 RVA: 0x000F4B6A File Offset: 0x000F2D6A
		[SecurityCritical]
		private static void InvokeAction(object state)
		{
			((Action)state)();
		}

		// Token: 0x06004186 RID: 16774 RVA: 0x000F4B78 File Offset: 0x000F2D78
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ContextCallback GetInvokeActionCallback()
		{
			ContextCallback contextCallback = AwaitTaskContinuation.s_invokeActionCallback;
			if (contextCallback == null)
			{
				contextCallback = (AwaitTaskContinuation.s_invokeActionCallback = new ContextCallback(AwaitTaskContinuation.InvokeAction));
			}
			return contextCallback;
		}

		// Token: 0x06004187 RID: 16775 RVA: 0x000F4BA4 File Offset: 0x000F2DA4
		[SecurityCritical]
		protected void RunCallback(ContextCallback callback, object state, ref Task currentTask)
		{
			Task task = currentTask;
			try
			{
				if (task != null)
				{
					currentTask = null;
				}
				if (this.m_capturedContext == null)
				{
					callback(state);
				}
				else
				{
					ExecutionContext.Run(this.m_capturedContext, callback, state, true);
				}
			}
			catch (Exception exc)
			{
				AwaitTaskContinuation.ThrowAsyncIfNecessary(exc);
			}
			finally
			{
				if (task != null)
				{
					currentTask = task;
				}
				if (this.m_capturedContext != null)
				{
					this.m_capturedContext.Dispose();
				}
			}
		}

		// Token: 0x06004188 RID: 16776 RVA: 0x000F4C1C File Offset: 0x000F2E1C
		[SecurityCritical]
		internal static void RunOrScheduleAction(Action action, bool allowInlining, ref Task currentTask)
		{
			if (!allowInlining || !AwaitTaskContinuation.IsValidLocationForInlining)
			{
				AwaitTaskContinuation.UnsafeScheduleAction(action, currentTask);
				return;
			}
			Task task = currentTask;
			try
			{
				if (task != null)
				{
					currentTask = null;
				}
				action();
			}
			catch (Exception exc)
			{
				AwaitTaskContinuation.ThrowAsyncIfNecessary(exc);
			}
			finally
			{
				if (task != null)
				{
					currentTask = task;
				}
			}
		}

		// Token: 0x06004189 RID: 16777 RVA: 0x000F4C7C File Offset: 0x000F2E7C
		[SecurityCritical]
		internal static void UnsafeScheduleAction(Action action, Task task)
		{
			AwaitTaskContinuation awaitTaskContinuation = new AwaitTaskContinuation(action, false);
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.IsEnabled() && task != null)
			{
				awaitTaskContinuation.m_continuationId = Task.NewId();
				log.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, awaitTaskContinuation.m_continuationId);
			}
			ThreadPool.UnsafeQueueCustomWorkItem(awaitTaskContinuation, false);
		}

		// Token: 0x0600418A RID: 16778 RVA: 0x000F4CDC File Offset: 0x000F2EDC
		protected static void ThrowAsyncIfNecessary(Exception exc)
		{
			if (!(exc is ThreadAbortException) && !(exc is AppDomainUnloadedException) && !WindowsRuntimeMarshal.ReportUnhandledError(exc))
			{
				ExceptionDispatchInfo state = ExceptionDispatchInfo.Capture(exc);
				ThreadPool.QueueUserWorkItem(delegate(object s)
				{
					((ExceptionDispatchInfo)s).Throw();
				}, state);
			}
		}

		// Token: 0x0600418B RID: 16779 RVA: 0x000F4D2E File Offset: 0x000F2F2E
		internal override Delegate[] GetDelegateContinuationsForDebugger()
		{
			return new Delegate[] { AsyncMethodBuilderCore.TryGetStateMachineForDebugger(this.m_action) };
		}

		// Token: 0x04001B5E RID: 7006
		private readonly ExecutionContext m_capturedContext;

		// Token: 0x04001B5F RID: 7007
		protected readonly Action m_action;

		// Token: 0x04001B60 RID: 7008
		protected int m_continuationId;

		// Token: 0x04001B61 RID: 7009
		[SecurityCritical]
		private static ContextCallback s_invokeActionCallback;
	}
}
