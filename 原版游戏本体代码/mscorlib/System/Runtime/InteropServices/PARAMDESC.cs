using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000999 RID: 2457
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.PARAMDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct PARAMDESC
	{
		// Token: 0x04002C4E RID: 11342
		public IntPtr lpVarValue;

		// Token: 0x04002C4F RID: 11343
		public PARAMFLAG wParamFlags;
	}
}
