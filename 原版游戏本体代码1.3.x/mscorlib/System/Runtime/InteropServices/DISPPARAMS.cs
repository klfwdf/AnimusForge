using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099D RID: 2461
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.DISPPARAMS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct DISPPARAMS
	{
		// Token: 0x04002C59 RID: 11353
		public IntPtr rgvarg;

		// Token: 0x04002C5A RID: 11354
		public IntPtr rgdispidNamedArgs;

		// Token: 0x04002C5B RID: 11355
		public int cArgs;

		// Token: 0x04002C5C RID: 11356
		public int cNamedArgs;
	}
}
