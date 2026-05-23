using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A51 RID: 2641
	[__DynamicallyInvokable]
	[Serializable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPELIBATTR
	{
		// Token: 0x04002E0C RID: 11788
		[__DynamicallyInvokable]
		public Guid guid;

		// Token: 0x04002E0D RID: 11789
		[__DynamicallyInvokable]
		public int lcid;

		// Token: 0x04002E0E RID: 11790
		[__DynamicallyInvokable]
		public SYSKIND syskind;

		// Token: 0x04002E0F RID: 11791
		[__DynamicallyInvokable]
		public short wMajorVerNum;

		// Token: 0x04002E10 RID: 11792
		[__DynamicallyInvokable]
		public short wMinorVerNum;

		// Token: 0x04002E11 RID: 11793
		[__DynamicallyInvokable]
		public LIBFLAGS wLibFlags;
	}
}
