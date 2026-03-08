using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A47 RID: 2631
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct DISPPARAMS
	{
		// Token: 0x04002DC3 RID: 11715
		[__DynamicallyInvokable]
		public IntPtr rgvarg;

		// Token: 0x04002DC4 RID: 11716
		[__DynamicallyInvokable]
		public IntPtr rgdispidNamedArgs;

		// Token: 0x04002DC5 RID: 11717
		[__DynamicallyInvokable]
		public int cArgs;

		// Token: 0x04002DC6 RID: 11718
		[__DynamicallyInvokable]
		public int cNamedArgs;
	}
}
