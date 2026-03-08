using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009A6 RID: 2470
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.LIBFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Flags]
	[Serializable]
	public enum LIBFLAGS : short
	{
		// Token: 0x04002C9C RID: 11420
		LIBFLAG_FRESTRICTED = 1,
		// Token: 0x04002C9D RID: 11421
		LIBFLAG_FCONTROL = 2,
		// Token: 0x04002C9E RID: 11422
		LIBFLAG_FHIDDEN = 4,
		// Token: 0x04002C9F RID: 11423
		LIBFLAG_FHASDISKIMAGE = 8
	}
}
