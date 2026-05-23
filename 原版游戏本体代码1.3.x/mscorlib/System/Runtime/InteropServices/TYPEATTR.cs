using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000994 RID: 2452
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEATTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPEATTR
	{
		// Token: 0x04002C1E RID: 11294
		public const int MEMBER_ID_NIL = -1;

		// Token: 0x04002C1F RID: 11295
		public Guid guid;

		// Token: 0x04002C20 RID: 11296
		public int lcid;

		// Token: 0x04002C21 RID: 11297
		public int dwReserved;

		// Token: 0x04002C22 RID: 11298
		public int memidConstructor;

		// Token: 0x04002C23 RID: 11299
		public int memidDestructor;

		// Token: 0x04002C24 RID: 11300
		public IntPtr lpstrSchema;

		// Token: 0x04002C25 RID: 11301
		public int cbSizeInstance;

		// Token: 0x04002C26 RID: 11302
		public TYPEKIND typekind;

		// Token: 0x04002C27 RID: 11303
		public short cFuncs;

		// Token: 0x04002C28 RID: 11304
		public short cVars;

		// Token: 0x04002C29 RID: 11305
		public short cImplTypes;

		// Token: 0x04002C2A RID: 11306
		public short cbSizeVft;

		// Token: 0x04002C2B RID: 11307
		public short cbAlignment;

		// Token: 0x04002C2C RID: 11308
		public TYPEFLAGS wTypeFlags;

		// Token: 0x04002C2D RID: 11309
		public short wMajorVerNum;

		// Token: 0x04002C2E RID: 11310
		public short wMinorVerNum;

		// Token: 0x04002C2F RID: 11311
		public TYPEDESC tdescAlias;

		// Token: 0x04002C30 RID: 11312
		public IDLDESC idldescType;
	}
}
