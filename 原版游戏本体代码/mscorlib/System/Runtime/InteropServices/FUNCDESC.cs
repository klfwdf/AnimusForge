using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000995 RID: 2453
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.FUNCDESC instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	public struct FUNCDESC
	{
		// Token: 0x04002C31 RID: 11313
		public int memid;

		// Token: 0x04002C32 RID: 11314
		public IntPtr lprgscode;

		// Token: 0x04002C33 RID: 11315
		public IntPtr lprgelemdescParam;

		// Token: 0x04002C34 RID: 11316
		public FUNCKIND funckind;

		// Token: 0x04002C35 RID: 11317
		public INVOKEKIND invkind;

		// Token: 0x04002C36 RID: 11318
		public CALLCONV callconv;

		// Token: 0x04002C37 RID: 11319
		public short cParams;

		// Token: 0x04002C38 RID: 11320
		public short cParamsOpt;

		// Token: 0x04002C39 RID: 11321
		public short oVft;

		// Token: 0x04002C3A RID: 11322
		public short cScodes;

		// Token: 0x04002C3B RID: 11323
		public ELEMDESC elemdescFunc;

		// Token: 0x04002C3C RID: 11324
		public short wFuncFlags;
	}
}
