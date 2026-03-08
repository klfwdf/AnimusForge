using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097A RID: 2426
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.BIND_OPTS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	public struct BIND_OPTS
	{
		// Token: 0x04002BE2 RID: 11234
		public int cbStruct;

		// Token: 0x04002BE3 RID: 11235
		public int grfFlags;

		// Token: 0x04002BE4 RID: 11236
		public int grfMode;

		// Token: 0x04002BE5 RID: 11237
		public int dwTickCountDeadline;
	}
}
