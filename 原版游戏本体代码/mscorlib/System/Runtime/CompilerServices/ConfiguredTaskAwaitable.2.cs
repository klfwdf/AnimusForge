using System;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F9 RID: 2297
	[__DynamicallyInvokable]
	public struct ConfiguredTaskAwaitable<TResult>
	{
		// Token: 0x06005E4D RID: 24141 RVA: 0x0014B579 File Offset: 0x00149779
		internal ConfiguredTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
		{
			this.m_configuredTaskAwaiter = new ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
		}

		// Token: 0x06005E4E RID: 24142 RVA: 0x0014B588 File Offset: 0x00149788
		[__DynamicallyInvokable]
		public ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter()
		{
			return this.m_configuredTaskAwaiter;
		}

		// Token: 0x04002A59 RID: 10841
		private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter m_configuredTaskAwaiter;

		// Token: 0x02000C95 RID: 3221
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
		public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
		{
			// Token: 0x06007104 RID: 28932 RVA: 0x0018524E File Offset: 0x0018344E
			internal ConfiguredTaskAwaiter(Task<TResult> task, bool continueOnCapturedContext)
			{
				this.m_task = task;
				this.m_continueOnCapturedContext = continueOnCapturedContext;
			}

			// Token: 0x17001361 RID: 4961
			// (get) Token: 0x06007105 RID: 28933 RVA: 0x0018525E File Offset: 0x0018345E
			[__DynamicallyInvokable]
			public bool IsCompleted
			{
				[__DynamicallyInvokable]
				get
				{
					return this.m_task.IsCompleted;
				}
			}

			// Token: 0x06007106 RID: 28934 RVA: 0x0018526B File Offset: 0x0018346B
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			public void OnCompleted(Action continuation)
			{
				TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext, true);
			}

			// Token: 0x06007107 RID: 28935 RVA: 0x00185280 File Offset: 0x00183480
			[SecurityCritical]
			[__DynamicallyInvokable]
			public void UnsafeOnCompleted(Action continuation)
			{
				TaskAwaiter.OnCompletedInternal(this.m_task, continuation, this.m_continueOnCapturedContext, false);
			}

			// Token: 0x06007108 RID: 28936 RVA: 0x00185295 File Offset: 0x00183495
			[__DynamicallyInvokable]
			public TResult GetResult()
			{
				TaskAwaiter.ValidateEnd(this.m_task);
				return this.m_task.ResultOnSuccess;
			}

			// Token: 0x04003851 RID: 14417
			private readonly Task<TResult> m_task;

			// Token: 0x04003852 RID: 14418
			private readonly bool m_continueOnCapturedContext;
		}
	}
}
