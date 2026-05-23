using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000980 RID: 2432
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumMoniker instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("00000102-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIEnumMoniker
	{
		// Token: 0x0600628C RID: 25228
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] UCOMIMoniker[] rgelt, out int pceltFetched);

		// Token: 0x0600628D RID: 25229
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x0600628E RID: 25230
		[PreserveSig]
		int Reset();

		// Token: 0x0600628F RID: 25231
		void Clone(out UCOMIEnumMoniker ppenum);
	}
}
