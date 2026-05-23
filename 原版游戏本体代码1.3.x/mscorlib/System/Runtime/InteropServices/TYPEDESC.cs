using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099A RID: 2458
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPEDESC
	{
		// Token: 0x04002C50 RID: 11344
		public IntPtr lpValue;

		// Token: 0x04002C51 RID: 11345
		public short vt;
	}
}
