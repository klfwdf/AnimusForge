using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099B RID: 2459
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.ELEMDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct ELEMDESC
	{
		// Token: 0x04002C52 RID: 11346
		public TYPEDESC tdesc;

		// Token: 0x04002C53 RID: 11347
		public ELEMDESC.DESCUNION desc;

		// Token: 0x02000C9A RID: 3226
		[ComVisible(false)]
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct DESCUNION
		{
			// Token: 0x0400385A RID: 14426
			[FieldOffset(0)]
			public IDLDESC idldesc;

			// Token: 0x0400385B RID: 14427
			[FieldOffset(0)]
			public PARAMDESC paramdesc;
		}
	}
}
