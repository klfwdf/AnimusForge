using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000987 RID: 2439
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.FILETIME instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	public struct FILETIME
	{
		// Token: 0x04002BE8 RID: 11240
		public int dwLowDateTime;

		// Token: 0x04002BE9 RID: 11241
		public int dwHighDateTime;
	}
}
