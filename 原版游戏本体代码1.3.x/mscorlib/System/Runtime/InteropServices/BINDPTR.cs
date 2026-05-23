using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200098F RID: 2447
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.BINDPTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct BINDPTR
	{
		// Token: 0x04002BFC RID: 11260
		[FieldOffset(0)]
		public IntPtr lpfuncdesc;

		// Token: 0x04002BFD RID: 11261
		[FieldOffset(0)]
		public IntPtr lpvardesc;

		// Token: 0x04002BFE RID: 11262
		[FieldOffset(0)]
		public IntPtr lptcomp;
	}
}
