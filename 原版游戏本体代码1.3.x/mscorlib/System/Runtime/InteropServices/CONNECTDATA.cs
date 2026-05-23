using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000981 RID: 2433
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.CONNECTDATA instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct CONNECTDATA
	{
		// Token: 0x04002BE6 RID: 11238
		[MarshalAs(UnmanagedType.Interface)]
		public object pUnk;

		// Token: 0x04002BE7 RID: 11239
		public int dwCookie;
	}
}
