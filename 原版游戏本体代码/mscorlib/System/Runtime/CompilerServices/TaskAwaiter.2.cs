using System;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F7 RID: 2295
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
	{
		// Token: 0x06005E46 RID: 24134 RVA: 0x0014B514 File Offset: 0x00149714
		internal TaskAwaiter(Task<TResult> task)
		{
			this.m_task = task;
		}

		// Token: 0x1700102F RID: 4143
		// (get) Token: 0x06005E47 RID: 24135 RVA: 0x0014B51D File Offset: 0x0014971D
		[__DynamicallyInvokable]
		public bool IsCompleted
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_task.IsCompleted;
			}
		}

		// Token: 0x06005E48 RID: 24136 RVA: 0x0014B52A File Offset: 0x0014972A
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public void OnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true, true);
		}

		// Token: 0x06005E49 RID: 24137 RVA: 0x0014B53A File Offset: 0x0014973A
		[SecurityCritical]
		[__DynamicallyInvokable]
		public void UnsafeOnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(this.m_task, continuation, true, false);
		}

		// Token: 0x06005E4A RID: 24138 RVA: 0x0014B54A File Offset: 0x0014974A
		[__DynamicallyInvokable]
		public TResult GetResult()
		{
			TaskAwaiter.ValidateEnd(this.m_task);
			return this.m_task.ResultOnSuccess;
		}

		// Token: 0x04002A57 RID: 10839
		private readonly Task<TResult> m_task;
	}
}
