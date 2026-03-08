using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000563 RID: 1379
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum TaskCreationOptions
	{
		// Token: 0x04001B28 RID: 6952
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001B29 RID: 6953
		[__DynamicallyInvokable]
		PreferFairness = 1,
		// Token: 0x04001B2A RID: 6954
		[__DynamicallyInvokable]
		LongRunning = 2,
		// Token: 0x04001B2B RID: 6955
		[__DynamicallyInvokable]
		AttachedToParent = 4,
		// Token: 0x04001B2C RID: 6956
		[__DynamicallyInvokable]
		DenyChildAttach = 8,
		// Token: 0x04001B2D RID: 6957
		[__DynamicallyInvokable]
		HideScheduler = 16,
		// Token: 0x04001B2E RID: 6958
		[__DynamicallyInvokable]
		RunContinuationsAsynchronously = 64
	}
}
