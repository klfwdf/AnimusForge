using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security.Permissions;

namespace System.Threading.Tasks
{
	// Token: 0x0200057B RID: 1403
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class TaskCompletionSource<TResult>
	{
		// Token: 0x0600422E RID: 16942 RVA: 0x000F66F6 File Offset: 0x000F48F6
		[__DynamicallyInvokable]
		public TaskCompletionSource()
		{
			this.m_task = new Task<TResult>();
		}

		// Token: 0x0600422F RID: 16943 RVA: 0x000F6709 File Offset: 0x000F4909
		[__DynamicallyInvokable]
		public TaskCompletionSource(TaskCreationOptions creationOptions)
			: this(null, creationOptions)
		{
		}

		// Token: 0x06004230 RID: 16944 RVA: 0x000F6713 File Offset: 0x000F4913
		[__DynamicallyInvokable]
		public TaskCompletionSource(object state)
			: this(state, TaskCreationOptions.None)
		{
		}

		// Token: 0x06004231 RID: 16945 RVA: 0x000F671D File Offset: 0x000F491D
		[__DynamicallyInvokable]
		public TaskCompletionSource(object state, TaskCreationOptions creationOptions)
		{
			this.m_task = new Task<TResult>(state, creationOptions);
		}

		// Token: 0x170009D3 RID: 2515
		// (get) Token: 0x06004232 RID: 16946 RVA: 0x000F6732 File Offset: 0x000F4932
		[__DynamicallyInvokable]
		public Task<TResult> Task
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_task;
			}
		}

		// Token: 0x06004233 RID: 16947 RVA: 0x000F673C File Offset: 0x000F493C
		private void SpinUntilCompleted()
		{
			SpinWait spinWait = default(SpinWait);
			while (!this.m_task.IsCompleted)
			{
				spinWait.SpinOnce();
			}
		}

		// Token: 0x06004234 RID: 16948 RVA: 0x000F6768 File Offset: 0x000F4968
		[__DynamicallyInvokable]
		public bool TrySetException(Exception exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}
			bool flag = this.m_task.TrySetException(exception);
			if (!flag && !this.m_task.IsCompleted)
			{
				this.SpinUntilCompleted();
			}
			return flag;
		}

		// Token: 0x06004235 RID: 16949 RVA: 0x000F67A8 File Offset: 0x000F49A8
		[__DynamicallyInvokable]
		public bool TrySetException(IEnumerable<Exception> exceptions)
		{
			if (exceptions == null)
			{
				throw new ArgumentNullException("exceptions");
			}
			List<Exception> list = new List<Exception>();
			foreach (Exception ex in exceptions)
			{
				if (ex == null)
				{
					throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NullException"), "exceptions");
				}
				list.Add(ex);
			}
			if (list.Count == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NoExceptions"), "exceptions");
			}
			bool flag = this.m_task.TrySetException(list);
			if (!flag && !this.m_task.IsCompleted)
			{
				this.SpinUntilCompleted();
			}
			return flag;
		}

		// Token: 0x06004236 RID: 16950 RVA: 0x000F6860 File Offset: 0x000F4A60
		internal bool TrySetException(IEnumerable<ExceptionDispatchInfo> exceptions)
		{
			bool flag = this.m_task.TrySetException(exceptions);
			if (!flag && !this.m_task.IsCompleted)
			{
				this.SpinUntilCompleted();
			}
			return flag;
		}

		// Token: 0x06004237 RID: 16951 RVA: 0x000F6891 File Offset: 0x000F4A91
		[__DynamicallyInvokable]
		public void SetException(Exception exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}
			if (!this.TrySetException(exception))
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
			}
		}

		// Token: 0x06004238 RID: 16952 RVA: 0x000F68BA File Offset: 0x000F4ABA
		[__DynamicallyInvokable]
		public void SetException(IEnumerable<Exception> exceptions)
		{
			if (!this.TrySetException(exceptions))
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
			}
		}

		// Token: 0x06004239 RID: 16953 RVA: 0x000F68D8 File Offset: 0x000F4AD8
		[__DynamicallyInvokable]
		public bool TrySetResult(TResult result)
		{
			bool flag = this.m_task.TrySetResult(result);
			if (!flag && !this.m_task.IsCompleted)
			{
				this.SpinUntilCompleted();
			}
			return flag;
		}

		// Token: 0x0600423A RID: 16954 RVA: 0x000F6909 File Offset: 0x000F4B09
		[__DynamicallyInvokable]
		public void SetResult(TResult result)
		{
			if (!this.TrySetResult(result))
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
			}
		}

		// Token: 0x0600423B RID: 16955 RVA: 0x000F6924 File Offset: 0x000F4B24
		[__DynamicallyInvokable]
		public bool TrySetCanceled()
		{
			return this.TrySetCanceled(default(CancellationToken));
		}

		// Token: 0x0600423C RID: 16956 RVA: 0x000F6940 File Offset: 0x000F4B40
		[__DynamicallyInvokable]
		public bool TrySetCanceled(CancellationToken cancellationToken)
		{
			bool flag = this.m_task.TrySetCanceled(cancellationToken);
			if (!flag && !this.m_task.IsCompleted)
			{
				this.SpinUntilCompleted();
			}
			return flag;
		}

		// Token: 0x0600423D RID: 16957 RVA: 0x000F6971 File Offset: 0x000F4B71
		[__DynamicallyInvokable]
		public void SetCanceled()
		{
			if (!this.TrySetCanceled())
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
			}
		}

		// Token: 0x04001B79 RID: 7033
		private readonly Task<TResult> m_task;
	}
}
