using System;

namespace System.Runtime
{
	// Token: 0x02000716 RID: 1814
	[__DynamicallyInvokable]
	[Serializable]
	public enum GCLatencyMode
	{
		// Token: 0x040023F7 RID: 9207
		[__DynamicallyInvokable]
		Batch,
		// Token: 0x040023F8 RID: 9208
		[__DynamicallyInvokable]
		Interactive,
		// Token: 0x040023F9 RID: 9209
		[__DynamicallyInvokable]
		LowLatency,
		// Token: 0x040023FA RID: 9210
		[__DynamicallyInvokable]
		SustainedLowLatency,
		// Token: 0x040023FB RID: 9211
		NoGCRegion
	}
}
