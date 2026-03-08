using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A50 RID: 2640
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum LIBFLAGS : short
	{
		// Token: 0x04002E08 RID: 11784
		[__DynamicallyInvokable]
		LIBFLAG_FRESTRICTED = 1,
		// Token: 0x04002E09 RID: 11785
		[__DynamicallyInvokable]
		LIBFLAG_FCONTROL = 2,
		// Token: 0x04002E0A RID: 11786
		[__DynamicallyInvokable]
		LIBFLAG_FHIDDEN = 4,
		// Token: 0x04002E0B RID: 11787
		[__DynamicallyInvokable]
		LIBFLAG_FHASDISKIMAGE = 8
	}
}
