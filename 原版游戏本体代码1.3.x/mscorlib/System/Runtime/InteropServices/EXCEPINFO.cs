using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099E RID: 2462
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.EXCEPINFO instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct EXCEPINFO
	{
		// Token: 0x04002C5D RID: 11357
		public short wCode;

		// Token: 0x04002C5E RID: 11358
		public short wReserved;

		// Token: 0x04002C5F RID: 11359
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrSource;

		// Token: 0x04002C60 RID: 11360
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrDescription;

		// Token: 0x04002C61 RID: 11361
		[MarshalAs(UnmanagedType.BStr)]
		public string bstrHelpFile;

		// Token: 0x04002C62 RID: 11362
		public int dwHelpContext;

		// Token: 0x04002C63 RID: 11363
		public IntPtr pvReserved;

		// Token: 0x04002C64 RID: 11364
		public IntPtr pfnDeferredFillIn;
	}
}
