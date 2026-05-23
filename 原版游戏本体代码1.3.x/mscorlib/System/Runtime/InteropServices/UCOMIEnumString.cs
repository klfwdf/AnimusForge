using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000984 RID: 2436
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumString instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("00000101-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIEnumString
	{
		// Token: 0x06006298 RID: 25240
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] [Out] string[] rgelt, out int pceltFetched);

		// Token: 0x06006299 RID: 25241
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x0600629A RID: 25242
		[PreserveSig]
		int Reset();

		// Token: 0x0600629B RID: 25243
		void Clone(out UCOMIEnumString ppenum);
	}
}
