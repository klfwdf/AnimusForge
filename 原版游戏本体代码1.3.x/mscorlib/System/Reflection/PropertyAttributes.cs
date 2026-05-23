using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x0200061A RID: 1562
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum PropertyAttributes
	{
		// Token: 0x04001E04 RID: 7684
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001E05 RID: 7685
		[__DynamicallyInvokable]
		SpecialName = 512,
		// Token: 0x04001E06 RID: 7686
		ReservedMask = 62464,
		// Token: 0x04001E07 RID: 7687
		[__DynamicallyInvokable]
		RTSpecialName = 1024,
		// Token: 0x04001E08 RID: 7688
		[__DynamicallyInvokable]
		HasDefault = 4096,
		// Token: 0x04001E09 RID: 7689
		Reserved2 = 8192,
		// Token: 0x04001E0A RID: 7690
		Reserved3 = 16384,
		// Token: 0x04001E0B RID: 7691
		Reserved4 = 32768
	}
}
