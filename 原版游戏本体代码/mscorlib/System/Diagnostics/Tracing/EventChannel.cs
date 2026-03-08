using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000438 RID: 1080
	[FriendAccessAllowed]
	[__DynamicallyInvokable]
	public enum EventChannel : byte
	{
		// Token: 0x040017FE RID: 6142
		[__DynamicallyInvokable]
		None,
		// Token: 0x040017FF RID: 6143
		[__DynamicallyInvokable]
		Admin = 16,
		// Token: 0x04001800 RID: 6144
		[__DynamicallyInvokable]
		Operational,
		// Token: 0x04001801 RID: 6145
		[__DynamicallyInvokable]
		Analytic,
		// Token: 0x04001802 RID: 6146
		[__DynamicallyInvokable]
		Debug
	}
}
