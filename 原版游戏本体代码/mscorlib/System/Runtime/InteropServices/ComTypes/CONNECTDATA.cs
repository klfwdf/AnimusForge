using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2A RID: 2602
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct CONNECTDATA
	{
		// Token: 0x04002D4A RID: 11594
		[__DynamicallyInvokable]
		[MarshalAs(UnmanagedType.Interface)]
		public object pUnk;

		// Token: 0x04002D4B RID: 11595
		[__DynamicallyInvokable]
		public int dwCookie;
	}
}
