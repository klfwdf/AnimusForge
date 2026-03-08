using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200042B RID: 1067
	[__DynamicallyInvokable]
	public enum EventCommand
	{
		// Token: 0x040017AD RID: 6061
		[__DynamicallyInvokable]
		Update,
		// Token: 0x040017AE RID: 6062
		[__DynamicallyInvokable]
		SendManifest = -1,
		// Token: 0x040017AF RID: 6063
		[__DynamicallyInvokable]
		Enable = -2,
		// Token: 0x040017B0 RID: 6064
		[__DynamicallyInvokable]
		Disable = -3
	}
}
