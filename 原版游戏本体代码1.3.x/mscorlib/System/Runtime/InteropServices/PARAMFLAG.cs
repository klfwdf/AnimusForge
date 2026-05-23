using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000998 RID: 2456
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.PARAMFLAG instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Flags]
	[Serializable]
	public enum PARAMFLAG : short
	{
		// Token: 0x04002C46 RID: 11334
		PARAMFLAG_NONE = 0,
		// Token: 0x04002C47 RID: 11335
		PARAMFLAG_FIN = 1,
		// Token: 0x04002C48 RID: 11336
		PARAMFLAG_FOUT = 2,
		// Token: 0x04002C49 RID: 11337
		PARAMFLAG_FLCID = 4,
		// Token: 0x04002C4A RID: 11338
		PARAMFLAG_FRETVAL = 8,
		// Token: 0x04002C4B RID: 11339
		PARAMFLAG_FOPT = 16,
		// Token: 0x04002C4C RID: 11340
		PARAMFLAG_FHASDEFAULT = 32,
		// Token: 0x04002C4D RID: 11341
		PARAMFLAG_FHASCUSTDATA = 64
	}
}
