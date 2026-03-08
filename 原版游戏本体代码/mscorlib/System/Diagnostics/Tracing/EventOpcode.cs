using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000437 RID: 1079
	[FriendAccessAllowed]
	[__DynamicallyInvokable]
	public enum EventOpcode
	{
		// Token: 0x040017F2 RID: 6130
		[__DynamicallyInvokable]
		Info,
		// Token: 0x040017F3 RID: 6131
		[__DynamicallyInvokable]
		Start,
		// Token: 0x040017F4 RID: 6132
		[__DynamicallyInvokable]
		Stop,
		// Token: 0x040017F5 RID: 6133
		[__DynamicallyInvokable]
		DataCollectionStart,
		// Token: 0x040017F6 RID: 6134
		[__DynamicallyInvokable]
		DataCollectionStop,
		// Token: 0x040017F7 RID: 6135
		[__DynamicallyInvokable]
		Extension,
		// Token: 0x040017F8 RID: 6136
		[__DynamicallyInvokable]
		Reply,
		// Token: 0x040017F9 RID: 6137
		[__DynamicallyInvokable]
		Resume,
		// Token: 0x040017FA RID: 6138
		[__DynamicallyInvokable]
		Suspend,
		// Token: 0x040017FB RID: 6139
		[__DynamicallyInvokable]
		Send,
		// Token: 0x040017FC RID: 6140
		[__DynamicallyInvokable]
		Receive = 240
	}
}
