using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A46 RID: 2630
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct VARDESC
	{
		// Token: 0x04002DBD RID: 11709
		[__DynamicallyInvokable]
		public int memid;

		// Token: 0x04002DBE RID: 11710
		[__DynamicallyInvokable]
		public string lpstrSchema;

		// Token: 0x04002DBF RID: 11711
		[__DynamicallyInvokable]
		public VARDESC.DESCUNION desc;

		// Token: 0x04002DC0 RID: 11712
		[__DynamicallyInvokable]
		public ELEMDESC elemdescVar;

		// Token: 0x04002DC1 RID: 11713
		[__DynamicallyInvokable]
		public short wVarFlags;

		// Token: 0x04002DC2 RID: 11714
		[__DynamicallyInvokable]
		public VARKIND varkind;

		// Token: 0x02000CAB RID: 3243
		[__DynamicallyInvokable]
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct DESCUNION
		{
			// Token: 0x0400388F RID: 14479
			[__DynamicallyInvokable]
			[FieldOffset(0)]
			public int oInst;

			// Token: 0x04003890 RID: 14480
			[FieldOffset(0)]
			public IntPtr lpvarValue;
		}
	}
}
