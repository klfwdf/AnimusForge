using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A40 RID: 2624
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct IDLDESC
	{
		// Token: 0x04002DA7 RID: 11687
		public IntPtr dwReserved;

		// Token: 0x04002DA8 RID: 11688
		[__DynamicallyInvokable]
		public IDLFLAG wIDLFlags;
	}
}
