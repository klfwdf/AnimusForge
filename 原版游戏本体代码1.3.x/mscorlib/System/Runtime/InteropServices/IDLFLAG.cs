using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000996 RID: 2454
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IDLFLAG instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Flags]
	[Serializable]
	public enum IDLFLAG : short
	{
		// Token: 0x04002C3E RID: 11326
		IDLFLAG_NONE = 0,
		// Token: 0x04002C3F RID: 11327
		IDLFLAG_FIN = 1,
		// Token: 0x04002C40 RID: 11328
		IDLFLAG_FOUT = 2,
		// Token: 0x04002C41 RID: 11329
		IDLFLAG_FLCID = 4,
		// Token: 0x04002C42 RID: 11330
		IDLFLAG_FRETVAL = 8
	}
}
