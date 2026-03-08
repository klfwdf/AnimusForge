using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A38 RID: 2616
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct BINDPTR
	{
		// Token: 0x04002D60 RID: 11616
		[FieldOffset(0)]
		public IntPtr lpfuncdesc;

		// Token: 0x04002D61 RID: 11617
		[FieldOffset(0)]
		public IntPtr lpvardesc;

		// Token: 0x04002D62 RID: 11618
		[FieldOffset(0)]
		public IntPtr lptcomp;
	}
}
