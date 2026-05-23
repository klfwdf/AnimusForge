using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099C RID: 2460
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.VARDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct VARDESC
	{
		// Token: 0x04002C54 RID: 11348
		public int memid;

		// Token: 0x04002C55 RID: 11349
		public string lpstrSchema;

		// Token: 0x04002C56 RID: 11350
		public ELEMDESC elemdescVar;

		// Token: 0x04002C57 RID: 11351
		public short wVarFlags;

		// Token: 0x04002C58 RID: 11352
		public VarEnum varkind;

		// Token: 0x02000C9B RID: 3227
		[ComVisible(false)]
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct DESCUNION
		{
			// Token: 0x0400385C RID: 14428
			[FieldOffset(0)]
			public int oInst;

			// Token: 0x0400385D RID: 14429
			[FieldOffset(0)]
			public IntPtr lpvarValue;
		}
	}
}
