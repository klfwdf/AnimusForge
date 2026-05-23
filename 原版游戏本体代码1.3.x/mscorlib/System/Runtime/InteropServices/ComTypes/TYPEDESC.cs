using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A43 RID: 2627
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPEDESC
	{
		// Token: 0x04002DB4 RID: 11700
		public IntPtr lpValue;

		// Token: 0x04002DB5 RID: 11701
		[__DynamicallyInvokable]
		public short vt;
	}
}
