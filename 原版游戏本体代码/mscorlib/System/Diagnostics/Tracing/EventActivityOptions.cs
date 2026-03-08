using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200041C RID: 1052
	[Flags]
	[__DynamicallyInvokable]
	public enum EventActivityOptions
	{
		// Token: 0x0400172E RID: 5934
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x0400172F RID: 5935
		[__DynamicallyInvokable]
		Disable = 2,
		// Token: 0x04001730 RID: 5936
		[__DynamicallyInvokable]
		Recursive = 4,
		// Token: 0x04001731 RID: 5937
		[__DynamicallyInvokable]
		Detachable = 8
	}
}
