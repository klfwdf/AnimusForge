using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000615 RID: 1557
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum ParameterAttributes
	{
		// Token: 0x04001DE2 RID: 7650
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001DE3 RID: 7651
		[__DynamicallyInvokable]
		In = 1,
		// Token: 0x04001DE4 RID: 7652
		[__DynamicallyInvokable]
		Out = 2,
		// Token: 0x04001DE5 RID: 7653
		[__DynamicallyInvokable]
		Lcid = 4,
		// Token: 0x04001DE6 RID: 7654
		[__DynamicallyInvokable]
		Retval = 8,
		// Token: 0x04001DE7 RID: 7655
		[__DynamicallyInvokable]
		Optional = 16,
		// Token: 0x04001DE8 RID: 7656
		ReservedMask = 61440,
		// Token: 0x04001DE9 RID: 7657
		[__DynamicallyInvokable]
		HasDefault = 4096,
		// Token: 0x04001DEA RID: 7658
		[__DynamicallyInvokable]
		HasFieldMarshal = 8192,
		// Token: 0x04001DEB RID: 7659
		Reserved3 = 16384,
		// Token: 0x04001DEC RID: 7660
		Reserved4 = 32768
	}
}
