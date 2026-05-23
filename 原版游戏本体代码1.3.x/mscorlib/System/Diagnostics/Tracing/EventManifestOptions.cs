using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000430 RID: 1072
	[Flags]
	[__DynamicallyInvokable]
	public enum EventManifestOptions
	{
		// Token: 0x040017C8 RID: 6088
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x040017C9 RID: 6089
		[__DynamicallyInvokable]
		Strict = 1,
		// Token: 0x040017CA RID: 6090
		[__DynamicallyInvokable]
		AllCultures = 2,
		// Token: 0x040017CB RID: 6091
		[__DynamicallyInvokable]
		OnlyIfNeededForRegistration = 4,
		// Token: 0x040017CC RID: 6092
		[__DynamicallyInvokable]
		AllowEventSourceOverride = 8
	}
}
