using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200055D RID: 1373
	[__DynamicallyInvokable]
	public enum TaskStatus
	{
		// Token: 0x04001AF0 RID: 6896
		[__DynamicallyInvokable]
		Created,
		// Token: 0x04001AF1 RID: 6897
		[__DynamicallyInvokable]
		WaitingForActivation,
		// Token: 0x04001AF2 RID: 6898
		[__DynamicallyInvokable]
		WaitingToRun,
		// Token: 0x04001AF3 RID: 6899
		[__DynamicallyInvokable]
		Running,
		// Token: 0x04001AF4 RID: 6900
		[__DynamicallyInvokable]
		WaitingForChildrenToComplete,
		// Token: 0x04001AF5 RID: 6901
		[__DynamicallyInvokable]
		RanToCompletion,
		// Token: 0x04001AF6 RID: 6902
		[__DynamicallyInvokable]
		Canceled,
		// Token: 0x04001AF7 RID: 6903
		[__DynamicallyInvokable]
		Faulted
	}
}
