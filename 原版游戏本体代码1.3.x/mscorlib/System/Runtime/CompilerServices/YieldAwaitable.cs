using System;
using System.Diagnostics.Tracing;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008FA RID: 2298
	[__DynamicallyInvokable]
	public struct YieldAwaitable
	{
		// Token: 0x06005E4F RID: 24143 RVA: 0x0014B590 File Offset: 0x00149790
		[__DynamicallyInvokable]
		public YieldAwaitable.YieldAwaiter GetAwaiter()
		{
			return default(YieldAwaitable.YieldAwaiter);
		}

		// Token: 0x02000C96 RID: 3222
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
		public struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
		{
			// Token: 0x17001362 RID: 4962
			// (get) Token: 0x06007109 RID: 28937 RVA: 0x001852AD File Offset: 0x001834AD
			[__DynamicallyInvokable]
			public bool IsCompleted
			{
				[__DynamicallyInvokable]
				get
				{
					return false;
				}
			}

			// Token: 0x0600710A RID: 28938 RVA: 0x001852B0 File Offset: 0x001834B0
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			public void OnCompleted(Action continuation)
			{
				YieldAwaitable.YieldAwaiter.QueueContinuation(continuation, true);
			}

			// Token: 0x0600710B RID: 28939 RVA: 0x001852B9 File Offset: 0x001834B9
			[SecurityCritical]
			[__DynamicallyInvokable]
			public void UnsafeOnCompleted(Action continuation)
			{
				YieldAwaitable.YieldAwaiter.QueueContinuation(continuation, false);
			}

			// Token: 0x0600710C RID: 28940 RVA: 0x001852C4 File Offset: 0x001834C4
			[SecurityCritical]
			private static void QueueContinuation(Action continuation, bool flowContext)
			{
				if (continuation == null)
				{
					throw new ArgumentNullException("continuation");
				}
				if (TplEtwProvider.Log.IsEnabled())
				{
					continuation = YieldAwaitable.YieldAwaiter.OutputCorrelationEtwEvent(continuation);
				}
				SynchronizationContext currentNoFlow = SynchronizationContext.CurrentNoFlow;
				if (currentNoFlow != null && currentNoFlow.GetType() != typeof(SynchronizationContext))
				{
					currentNoFlow.Post(YieldAwaitable.YieldAwaiter.s_sendOrPostCallbackRunAction, continuation);
					return;
				}
				TaskScheduler taskScheduler = TaskScheduler.Current;
				if (taskScheduler != TaskScheduler.Default)
				{
					Task.Factory.StartNew(continuation, default(CancellationToken), TaskCreationOptions.PreferFairness, taskScheduler);
					return;
				}
				if (flowContext)
				{
					ThreadPool.QueueUserWorkItem(YieldAwaitable.YieldAwaiter.s_waitCallbackRunAction, continuation);
					return;
				}
				ThreadPool.UnsafeQueueUserWorkItem(YieldAwaitable.YieldAwaiter.s_waitCallbackRunAction, continuation);
			}

			// Token: 0x0600710D RID: 28941 RVA: 0x00185364 File Offset: 0x00183564
			private static Action OutputCorrelationEtwEvent(Action continuation)
			{
				int continuationId = Task.NewId();
				Task internalCurrent = Task.InternalCurrent;
				TplEtwProvider.Log.AwaitTaskContinuationScheduled(TaskScheduler.Current.Id, (internalCurrent != null) ? internalCurrent.Id : 0, continuationId);
				return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, delegate
				{
					TplEtwProvider log = TplEtwProvider.Log;
					log.TaskWaitContinuationStarted(continuationId);
					Guid currentThreadActivityId = default(Guid);
					if (log.TasksSetActivityIds)
					{
						EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(continuationId), out currentThreadActivityId);
					}
					continuation();
					if (log.TasksSetActivityIds)
					{
						EventSource.SetCurrentThreadActivityId(currentThreadActivityId);
					}
					log.TaskWaitContinuationComplete(continuationId);
				}, null);
			}

			// Token: 0x0600710E RID: 28942 RVA: 0x001853CD File Offset: 0x001835CD
			private static void RunAction(object state)
			{
				((Action)state)();
			}

			// Token: 0x0600710F RID: 28943 RVA: 0x001853DA File Offset: 0x001835DA
			[__DynamicallyInvokable]
			public void GetResult()
			{
			}

			// Token: 0x04003853 RID: 14419
			private static readonly WaitCallback s_waitCallbackRunAction = new WaitCallback(YieldAwaitable.YieldAwaiter.RunAction);

			// Token: 0x04003854 RID: 14420
			private static readonly SendOrPostCallback s_sendOrPostCallbackRunAction = new SendOrPostCallback(YieldAwaitable.YieldAwaiter.RunAction);
		}
	}
}
