using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000564 RID: 1380
	[Flags]
	[Serializable]
	internal enum InternalTaskOptions
	{
		// Token: 0x04001B30 RID: 6960
		None = 0,
		// Token: 0x04001B31 RID: 6961
		InternalOptionsMask = 65280,
		// Token: 0x04001B32 RID: 6962
		ChildReplica = 256,
		// Token: 0x04001B33 RID: 6963
		ContinuationTask = 512,
		// Token: 0x04001B34 RID: 6964
		PromiseTask = 1024,
		// Token: 0x04001B35 RID: 6965
		SelfReplicating = 2048,
		// Token: 0x04001B36 RID: 6966
		LazyCancellation = 4096,
		// Token: 0x04001B37 RID: 6967
		QueuedByRuntime = 8192,
		// Token: 0x04001B38 RID: 6968
		DoNotDispose = 16384
	}
}
