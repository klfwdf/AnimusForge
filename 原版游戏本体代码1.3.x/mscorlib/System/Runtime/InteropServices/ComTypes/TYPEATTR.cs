using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A3D RID: 2621
	[__DynamicallyInvokable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPEATTR
	{
		// Token: 0x04002D82 RID: 11650
		[__DynamicallyInvokable]
		public const int MEMBER_ID_NIL = -1;

		// Token: 0x04002D83 RID: 11651
		[__DynamicallyInvokable]
		public Guid guid;

		// Token: 0x04002D84 RID: 11652
		[__DynamicallyInvokable]
		public int lcid;

		// Token: 0x04002D85 RID: 11653
		[__DynamicallyInvokable]
		public int dwReserved;

		// Token: 0x04002D86 RID: 11654
		[__DynamicallyInvokable]
		public int memidConstructor;

		// Token: 0x04002D87 RID: 11655
		[__DynamicallyInvokable]
		public int memidDestructor;

		// Token: 0x04002D88 RID: 11656
		public IntPtr lpstrSchema;

		// Token: 0x04002D89 RID: 11657
		[__DynamicallyInvokable]
		public int cbSizeInstance;

		// Token: 0x04002D8A RID: 11658
		[__DynamicallyInvokable]
		public TYPEKIND typekind;

		// Token: 0x04002D8B RID: 11659
		[__DynamicallyInvokable]
		public short cFuncs;

		// Token: 0x04002D8C RID: 11660
		[__DynamicallyInvokable]
		public short cVars;

		// Token: 0x04002D8D RID: 11661
		[__DynamicallyInvokable]
		public short cImplTypes;

		// Token: 0x04002D8E RID: 11662
		[__DynamicallyInvokable]
		public short cbSizeVft;

		// Token: 0x04002D8F RID: 11663
		[__DynamicallyInvokable]
		public short cbAlignment;

		// Token: 0x04002D90 RID: 11664
		[__DynamicallyInvokable]
		public TYPEFLAGS wTypeFlags;

		// Token: 0x04002D91 RID: 11665
		[__DynamicallyInvokable]
		public short wMajorVerNum;

		// Token: 0x04002D92 RID: 11666
		[__DynamicallyInvokable]
		public short wMinorVerNum;

		// Token: 0x04002D93 RID: 11667
		[__DynamicallyInvokable]
		public TYPEDESC tdescAlias;

		// Token: 0x04002D94 RID: 11668
		[__DynamicallyInvokable]
		public IDLDESC idldescType;
	}
}
