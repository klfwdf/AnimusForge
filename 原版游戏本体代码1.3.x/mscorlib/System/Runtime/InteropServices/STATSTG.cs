using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200098C RID: 2444
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.STATSTG instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STATSTG
	{
		// Token: 0x04002BEA RID: 11242
		public string pwcsName;

		// Token: 0x04002BEB RID: 11243
		public int type;

		// Token: 0x04002BEC RID: 11244
		public long cbSize;

		// Token: 0x04002BED RID: 11245
		public FILETIME mtime;

		// Token: 0x04002BEE RID: 11246
		public FILETIME ctime;

		// Token: 0x04002BEF RID: 11247
		public FILETIME atime;

		// Token: 0x04002BF0 RID: 11248
		public int grfMode;

		// Token: 0x04002BF1 RID: 11249
		public int grfLocksSupported;

		// Token: 0x04002BF2 RID: 11250
		public Guid clsid;

		// Token: 0x04002BF3 RID: 11251
		public int grfStateBits;

		// Token: 0x04002BF4 RID: 11252
		public int reserved;
	}
}
