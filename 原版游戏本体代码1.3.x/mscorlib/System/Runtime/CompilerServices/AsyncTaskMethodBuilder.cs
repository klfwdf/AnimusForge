using System;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008EF RID: 2287
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct AsyncTaskMethodBuilder
	{
		// Token: 0x06005E15 RID: 24085 RVA: 0x0014A98C File Offset: 0x00148B8C
		[__DynamicallyInvokable]
		public static AsyncTaskMethodBuilder Create()
		{
			return default(AsyncTaskMethodBuilder);
		}

		// Token: 0x06005E16 RID: 24086 RVA: 0x0014A9A4 File Offset: 0x00148BA4
		[SecuritySafeCritical]
		[DebuggerStepThrough]
		[__DynamicallyInvokable]
		public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
		{
			if (stateMachine == null)
			{
				throw new ArgumentNullException("stateMachine");
			}
			ExecutionContextSwitcher executionContextSwitcher = default(ExecutionContextSwitcher);
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				ExecutionContext.EstablishCopyOnWriteScope(ref executionContextSwitcher);
				stateMachine.MoveNext();
			}
			finally
			{
				executionContextSwitcher.Undo();
			}
		}

		// Token: 0x06005E17 RID: 24087 RVA: 0x0014AA04 File Offset: 0x00148C04
		[__DynamicallyInvokable]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			this.m_builder.SetStateMachine(stateMachine);
		}

		// Token: 0x06005E18 RID: 24088 RVA: 0x0014AA12 File Offset: 0x00148C12
		[__DynamicallyInvokable]
		public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
		{
			this.m_builder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
		}

		// Token: 0x06005E19 RID: 24089 RVA: 0x0014AA21 File Offset: 0x00148C21
		[__DynamicallyInvokable]
		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
		{
			this.m_builder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
		}

		// Token: 0x1700102A RID: 4138
		// (get) Token: 0x06005E1A RID: 24090 RVA: 0x0014AA30 File Offset: 0x00148C30
		[__DynamicallyInvokable]
		public Task Task
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_builder.Task;
			}
		}

		// Token: 0x06005E1B RID: 24091 RVA: 0x0014AA3D File Offset: 0x00148C3D
		[__DynamicallyInvokable]
		public void SetResult()
		{
			this.m_builder.SetResult(AsyncTaskMethodBuilder.s_cachedCompleted);
		}

		// Token: 0x06005E1C RID: 24092 RVA: 0x0014AA4F File Offset: 0x00148C4F
		[__DynamicallyInvokable]
		public void SetException(Exception exception)
		{
			this.m_builder.SetException(exception);
		}

		// Token: 0x06005E1D RID: 24093 RVA: 0x0014AA5D File Offset: 0x00148C5D
		internal void SetNotificationForWaitCompletion(bool enabled)
		{
			this.m_builder.SetNotificationForWaitCompletion(enabled);
		}

		// Token: 0x1700102B RID: 4139
		// (get) Token: 0x06005E1E RID: 24094 RVA: 0x0014AA6B File Offset: 0x00148C6B
		private object ObjectIdForDebugger
		{
			get
			{
				return this.Task;
			}
		}

		// Token: 0x04002A4A RID: 10826
		private static readonly Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

		// Token: 0x04002A4B RID: 10827
		private AsyncTaskMethodBuilder<VoidTaskResult> m_builder;
	}
}
