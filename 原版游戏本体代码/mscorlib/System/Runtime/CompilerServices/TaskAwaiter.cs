using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F6 RID: 2294
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
	{
		// Token: 0x06005E3C RID: 24124 RVA: 0x0014B323 File Offset: 0x00149523
		internal TaskAwaiter(Task task)
		{
			this.m_task = task;
		}

		// Token: 0x1700102E RID: 4142
		// (get) Token: 0x06005E3D RID: 24125 RVA: 0x0014B32C File Offset: 0x0014952C
		[__DynamicallyInvokable]
		public bool IsCompleted
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_task.IsCompleted;
			}
		}

		// Token: 0x06005E3E RID: 24126 RVA: 0x0014B339 File Offset: 0x00149539
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public void OnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true, true);
		}

		// Token: 0x06005E3F RID: 24127 RVA: 0x0014B349 File Offset: 0x00149549
		[SecurityCritical]
		[__DynamicallyInvokable]
		public void UnsafeOnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true, false);
		}

		// Token: 0x06005E40 RID: 24128 RVA: 0x0014B359 File Offset: 0x00149559
		[__DynamicallyInvokable]
		public void GetResult()
		{
			TaskAwaiter.ValidateEnd(this.m_task);
		}

		// Token: 0x06005E41 RID: 24129 RVA: 0x0014B366 File Offset: 0x00149566
		internal static void ValidateEnd(Task task)
		{
			if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
			{
				TaskAwaiter.HandleNonSuccessAndDebuggerNotification(task);
			}
		}

		// Token: 0x06005E42 RID: 24130 RVA: 0x0014B378 File Offset: 0x00149578
		private static void HandleNonSuccessAndDebuggerNotification(Task task)
		{
			if (!task.IsCompleted)
			{
				bool flag = task.InternalWait(-1, default(CancellationToken));
			}
			task.NotifyDebuggerOfWaitCompletionIfNecessary();
			if (!task.IsRanToCompletion)
			{
				TaskAwaiter.ThrowForNonSuccess(task);
			}
		}

		// Token: 0x06005E43 RID: 24131 RVA: 0x0014B3B4 File Offset: 0x001495B4
		private static void ThrowForNonSuccess(Task task)
		{
			TaskStatus status = task.Status;
			if (status == TaskStatus.Canceled)
			{
				ExceptionDispatchInfo cancellationExceptionDispatchInfo = task.GetCancellationExceptionDispatchInfo();
				if (cancellationExceptionDispatchInfo != null)
				{
					cancellationExceptionDispatchInfo.Throw();
				}
				throw new TaskCanceledException(task);
			}
			if (status != TaskStatus.Faulted)
			{
				return;
			}
			ReadOnlyCollection<ExceptionDispatchInfo> exceptionDispatchInfos = task.GetExceptionDispatchInfos();
			if (exceptionDispatchInfos.Count > 0)
			{
				exceptionDispatchInfos[0].Throw();
				return;
			}
			throw task.Exception;
		}

		// Token: 0x06005E44 RID: 24132 RVA: 0x0014B40C File Offset: 0x0014960C
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
		{
			if (continuation == null)
			{
				throw new ArgumentNullException("continuation");
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (TplEtwProvider.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
			{
				continuation = TaskAwaiter.OutputWaitEtwEvents(task, continuation);
			}
			task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext, ref stackCrawlMark);
		}

		// Token: 0x06005E45 RID: 24133 RVA: 0x0014B450 File Offset: 0x00149650
		private static Action OutputWaitEtwEvents(Task task, Action continuation)
		{
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.AddToActiveTasks(task);
			}
			TplEtwProvider etwLog = TplEtwProvider.Log;
			if (etwLog.IsEnabled())
			{
				Task internalCurrent = Task.InternalCurrent;
				Task task2 = AsyncMethodBuilderCore.TryGetContinuationTask(continuation);
				etwLog.TaskWaitBegin((internalCurrent != null) ? internalCurrent.m_taskScheduler.Id : TaskScheduler.Default.Id, (internalCurrent != null) ? internalCurrent.Id : 0, task.Id, TplEtwProvider.TaskWaitBehavior.Asynchronous, (task2 != null) ? task2.Id : 0, Thread.GetDomainID());
			}
			return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, delegate
			{
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.RemoveFromActiveTasks(task.Id);
				}
				Guid currentThreadActivityId = default(Guid);
				bool flag = etwLog.IsEnabled();
				if (flag)
				{
					Task internalCurrent2 = Task.InternalCurrent;
					etwLog.TaskWaitEnd((internalCurrent2 != null) ? internalCurrent2.m_taskScheduler.Id : TaskScheduler.Default.Id, (internalCurrent2 != null) ? internalCurrent2.Id : 0, task.Id);
					if (etwLog.TasksSetActivityIds && (task.Options & (TaskCreationOptions)1024) != TaskCreationOptions.None)
					{
						EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(task.Id), out currentThreadActivityId);
					}
				}
				continuation();
				if (flag)
				{
					etwLog.TaskWaitContinuationComplete(task.Id);
					if (etwLog.TasksSetActivityIds && (task.Options & (TaskCreationOptions)1024) != TaskCreationOptions.None)
					{
						EventSource.SetCurrentThreadActivityId(currentThreadActivityId);
					}
				}
			}, null);
		}

		// Token: 0x04002A56 RID: 10838
		private readonly Task m_task;
	}
}
