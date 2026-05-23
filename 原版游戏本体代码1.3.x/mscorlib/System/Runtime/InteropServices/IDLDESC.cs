using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000997 RID: 2455
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IDLDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct IDLDESC
	{
		// Token: 0x04002C43 RID: 11331
		public int dwReserved;

		// Token: 0x04002C44 RID: 11332
		public IDLFLAG wIDLFlags;
	}
}
