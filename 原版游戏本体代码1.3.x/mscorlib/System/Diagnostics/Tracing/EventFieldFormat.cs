using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000444 RID: 1092
	[__DynamicallyInvokable]
	public enum EventFieldFormat
	{
		// Token: 0x04001829 RID: 6185
		[__DynamicallyInvokable]
		Default,
		// Token: 0x0400182A RID: 6186
		[__DynamicallyInvokable]
		String = 2,
		// Token: 0x0400182B RID: 6187
		[__DynamicallyInvokable]
		Boolean,
		// Token: 0x0400182C RID: 6188
		[__DynamicallyInvokable]
		Hexadecimal,
		// Token: 0x0400182D RID: 6189
		[__DynamicallyInvokable]
		Xml = 11,
		// Token: 0x0400182E RID: 6190
		[__DynamicallyInvokable]
		Json,
		// Token: 0x0400182F RID: 6191
		[__DynamicallyInvokable]
		HResult = 15
	}
}
