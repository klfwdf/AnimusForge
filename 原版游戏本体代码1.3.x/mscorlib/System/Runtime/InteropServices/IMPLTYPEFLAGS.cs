using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000993 RID: 2451
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Flags]
	[Serializable]
	public enum IMPLTYPEFLAGS
	{
		// Token: 0x04002C1A RID: 11290
		IMPLTYPEFLAG_FDEFAULT = 1,
		// Token: 0x04002C1B RID: 11291
		IMPLTYPEFLAG_FSOURCE = 2,
		// Token: 0x04002C1C RID: 11292
		IMPLTYPEFLAG_FRESTRICTED = 4,
		// Token: 0x04002C1D RID: 11293
		IMPLTYPEFLAG_FDEFAULTVTABLE = 8
	}
}
