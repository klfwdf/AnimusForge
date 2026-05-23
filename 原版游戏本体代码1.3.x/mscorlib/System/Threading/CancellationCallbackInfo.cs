using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000547 RID: 1351
	internal class CancellationCallbackInfo
	{
		// Token: 0x06003F6A RID: 16234 RVA: 0x000EC16C File Offset: 0x000EA36C
		internal CancellationCallbackInfo(Action<object> callback, object stateForCallback, SynchronizationContext targetSyncContext, ExecutionContext targetExecutionContext, CancellationTokenSource cancellationTokenSource)
		{
			this.Callback = callback;
			this.StateForCallback = stateForCallback;
			this.TargetSyncContext = targetSyncContext;
			this.TargetExecutionContext = targetExecutionContext;
			this.CancellationTokenSource = cancellationTokenSource;
		}

		// Token: 0x06003F6B RID: 16235 RVA: 0x000EC19C File Offset: 0x000EA39C
		[SecuritySafeCritical]
		internal void ExecuteCallback()
		{
			if (this.TargetExecutionContext != null)
			{
				ContextCallback contextCallback = CancellationCallbackInfo.s_executionContextCallback;
				if (contextCallback == null)
				{
					contextCallback = (CancellationCallbackInfo.s_executionContextCallback = new ContextCallback(CancellationCallbackInfo.ExecutionContextCallback));
				}
				ExecutionContext.Run(this.TargetExecutionContext, contextCallback, this);
				return;
			}
			CancellationCallbackInfo.ExecutionContextCallback(this);
		}

		// Token: 0x06003F6C RID: 16236 RVA: 0x000EC1E4 File Offset: 0x000EA3E4
		[SecurityCritical]
		private static void ExecutionContextCallback(object obj)
		{
			CancellationCallbackInfo cancellationCallbackInfo = obj as CancellationCallbackInfo;
			cancellationCallbackInfo.Callback(cancellationCallbackInfo.StateForCallback);
		}

		// Token: 0x04001AAF RID: 6831
		internal readonly Action<object> Callback;

		// Token: 0x04001AB0 RID: 6832
		internal readonly object StateForCallback;

		// Token: 0x04001AB1 RID: 6833
		internal readonly SynchronizationContext TargetSyncContext;

		// Token: 0x04001AB2 RID: 6834
		internal readonly ExecutionContext TargetExecutionContext;

		// Token: 0x04001AB3 RID: 6835
		internal readonly CancellationTokenSource CancellationTokenSource;

		// Token: 0x04001AB4 RID: 6836
		[SecurityCritical]
		private static ContextCallback s_executionContextCallback;
	}
}
