using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A35 RID: 2613
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STATSTG
	{
		// Token: 0x04002D4E RID: 11598
		[__DynamicallyInvokable]
		public string pwcsName;

		// Token: 0x04002D4F RID: 11599
		[__DynamicallyInvokable]
		public int type;

		// Token: 0x04002D50 RID: 11600
		[__DynamicallyInvokable]
		public long cbSize;

		// Token: 0x04002D51 RID: 11601
		[__DynamicallyInvokable]
		public FILETIME mtime;

		// Token: 0x04002D52 RID: 11602
		[__DynamicallyInvokable]
		public FILETIME ctime;

		// Token: 0x04002D53 RID: 11603
		[__DynamicallyInvokable]
		public FILETIME atime;

		// Token: 0x04002D54 RID: 11604
		[__DynamicallyInvokable]
		public int grfMode;

		// Token: 0x04002D55 RID: 11605
		[__DynamicallyInvokable]
		public int grfLocksSupported;

		// Token: 0x04002D56 RID: 11606
		[__DynamicallyInvokable]
		public Guid clsid;

		// Token: 0x04002D57 RID: 11607
		[__DynamicallyInvokable]
		public int grfStateBits;

		// Token: 0x04002D58 RID: 11608
		[__DynamicallyInvokable]
		public int reserved;
	}
}
