using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000565 RID: 1381
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum TaskContinuationOptions
	{
		// Token: 0x04001B3A RID: 6970
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001B3B RID: 6971
		[__DynamicallyInvokable]
		PreferFairness = 1,
		// Token: 0x04001B3C RID: 6972
		[__DynamicallyInvokable]
		LongRunning = 2,
		// Token: 0x04001B3D RID: 6973
		[__DynamicallyInvokable]
		AttachedToParent = 4,
		// Token: 0x04001B3E RID: 6974
		[__DynamicallyInvokable]
		DenyChildAttach = 8,
		// Token: 0x04001B3F RID: 6975
		[__DynamicallyInvokable]
		HideScheduler = 16,
		// Token: 0x04001B40 RID: 6976
		[__DynamicallyInvokable]
		LazyCancellation = 32,
		// Token: 0x04001B41 RID: 6977
		[__DynamicallyInvokable]
		RunContinuationsAsynchronously = 64,
		// Token: 0x04001B42 RID: 6978
		[__DynamicallyInvokable]
		NotOnRanToCompletion = 65536,
		// Token: 0x04001B43 RID: 6979
		[__DynamicallyInvokable]
		NotOnFaulted = 131072,
		// Token: 0x04001B44 RID: 6980
		[__DynamicallyInvokable]
		NotOnCanceled = 262144,
		// Token: 0x04001B45 RID: 6981
		[__DynamicallyInvokable]
		OnlyOnRanToCompletion = 393216,
		// Token: 0x04001B46 RID: 6982
		[__DynamicallyInvokable]
		OnlyOnFaulted = 327680,
		// Token: 0x04001B47 RID: 6983
		[__DynamicallyInvokable]
		OnlyOnCanceled = 196608,
		// Token: 0x04001B48 RID: 6984
		[__DynamicallyInvokable]
		ExecuteSynchronously = 524288
	}
}
