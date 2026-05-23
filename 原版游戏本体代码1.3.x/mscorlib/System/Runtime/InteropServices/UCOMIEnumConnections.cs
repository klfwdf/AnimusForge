using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000982 RID: 2434
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumConnections instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("B196B287-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIEnumConnections
	{
		// Token: 0x06006290 RID: 25232
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] CONNECTDATA[] rgelt, out int pceltFetched);

		// Token: 0x06006291 RID: 25233
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006292 RID: 25234
		[PreserveSig]
		void Reset();

		// Token: 0x06006293 RID: 25235
		void Clone(out UCOMIEnumConnections ppenum);
	}
}
