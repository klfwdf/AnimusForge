using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000421 RID: 1057
	[Flags]
	[__DynamicallyInvokable]
	public enum EventSourceSettings
	{
		// Token: 0x04001772 RID: 6002
		[__DynamicallyInvokable]
		Default = 0,
		// Token: 0x04001773 RID: 6003
		[__DynamicallyInvokable]
		ThrowOnEventWriteErrors = 1,
		// Token: 0x04001774 RID: 6004
		[__DynamicallyInvokable]
		EtwManifestEventFormat = 4,
		// Token: 0x04001775 RID: 6005
		[__DynamicallyInvokable]
		EtwSelfDescribingEventFormat = 8
	}
}
