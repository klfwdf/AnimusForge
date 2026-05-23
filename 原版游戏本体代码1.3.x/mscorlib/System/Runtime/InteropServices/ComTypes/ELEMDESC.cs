using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A44 RID: 2628
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct ELEMDESC
	{
		// Token: 0x04002DB6 RID: 11702
		[__DynamicallyInvokable]
		public TYPEDESC tdesc;

		// Token: 0x04002DB7 RID: 11703
		[__DynamicallyInvokable]
		public ELEMDESC.DESCUNION desc;

		// Token: 0x02000CAA RID: 3242
		[__DynamicallyInvokable]
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct DESCUNION
		{
			// Token: 0x0400388D RID: 14477
			[__DynamicallyInvokable]
			[FieldOffset(0)]
			public IDLDESC idldesc;

			// Token: 0x0400388E RID: 14478
			[__DynamicallyInvokable]
			[FieldOffset(0)]
			public PARAMDESC paramdesc;
		}
	}
}
