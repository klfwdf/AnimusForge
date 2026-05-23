using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A42 RID: 2626
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct PARAMDESC
	{
		// Token: 0x04002DB2 RID: 11698
		public IntPtr lpVarValue;

		// Token: 0x04002DB3 RID: 11699
		[__DynamicallyInvokable]
		public PARAMFLAG wParamFlags;
	}
}
