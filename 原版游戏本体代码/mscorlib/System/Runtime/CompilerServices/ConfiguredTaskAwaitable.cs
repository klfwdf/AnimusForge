using System;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F8 RID: 2296
	[__DynamicallyInvokable]
	public struct ConfiguredTaskAwaitable
	{
		// Token: 0x06005E4B RID: 24139 RVA: 0x0014B562 File Offset: 0x00149762
		internal ConfiguredTaskAwaitable(Task task, bool continueOnCapturedContext)
		{
			this.m_configuredTaskAwaiter = new ConfiguredTaskAwaitable.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
		}

		// Token: 0x06005E4C RID: 24140 RVA: 0x0014B571 File Offset: 0x00149771
		[__DynamicallyInvokable]
		public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
		{
			return this.m_configuredTaskAwaiter;
		}

		// Token: 0x04002A58 RID: 10840
		private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

		// Token: 0x02000C94 RID: 3220
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
		public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
		{
			// Token: 0x060070FF RID: 28927 RVA: 0x001851FA File Offset: 0x001833FA
			internal ConfiguredTaskAwaiter(Task task, bool continueOnCapturedContext)
			{
				this.m_task = task;
				this.m_continueOnCapturedContext = continueOnCapturedContext;
			}

			// Token: 0x17001360 RID: 4960
			// (get) Token: 0x06007100 RID: 28928 RVA: 0x0018520A File Offset: 0x0018340A
			[__DynamicallyInvokable]
			public bool IsCompleted
			{
				[__DynamicallyInvokable]
				get
				{
					return this.m_task.IsCompleted;
				}
			}

			// Token: 0x06007101 RID: 28929 RVA: 0x00185217 File Offset: 0x00183417
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			public void OnCompleted(Action continuation)
			{
				TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext, true);
			}

			// Token: 0x06007102 RID: 28930 RVA: 0x0018522C File Offset: 0x0018342C
			[SecurityCritical]
			[__DynamicallyInvokable]
			public void UnsafeOnCompleted(Action continuation)
			{
				TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext, false);
			}

			// Token: 0x06007103 RID: 28931 RVA: 0x00185241 File Offset: 0x00183441
			[__DynamicallyInvokable]
			public void GetResult()
			{
				TaskAwaiter.ValidateEnd(this.m_task);
			}

			// Token: 0x0400384F RID: 14415
			private readonly Task m_task;

			// Token: 0x04003850 RID: 14416
			private readonly bool m_continueOnCapturedContext;
		}
	}
}
