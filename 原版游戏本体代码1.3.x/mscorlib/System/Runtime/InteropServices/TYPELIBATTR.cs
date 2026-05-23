using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009A7 RID: 2471
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPELIBATTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TYPELIBATTR
	{
		// Token: 0x04002CA0 RID: 11424
		public Guid guid;

		// Token: 0x04002CA1 RID: 11425
		public int lcid;

		// Token: 0x04002CA2 RID: 11426
		public SYSKIND syskind;

		// Token: 0x04002CA3 RID: 11427
		public short wMajorVerNum;

		// Token: 0x04002CA4 RID: 11428
		public short wMinorVerNum;

		// Token: 0x04002CA5 RID: 11429
		public LIBFLAGS wLibFlags;
	}
}
