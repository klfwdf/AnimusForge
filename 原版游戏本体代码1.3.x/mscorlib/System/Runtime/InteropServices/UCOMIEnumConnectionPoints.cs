using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000983 RID: 2435
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMIEnumConnectionPoints
	{
		// Token: 0x06006294 RID: 25236
		[PreserveSig]
		int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] UCOMIConnectionPoint[] rgelt, out int pceltFetched);

		// Token: 0x06006295 RID: 25237
		[PreserveSig]
		int Skip(int celt);

		// Token: 0x06006296 RID: 25238
		[PreserveSig]
		int Reset();

		// Token: 0x06006297 RID: 25239
		void Clone(out UCOMIEnumConnectionPoints ppenum);
	}
}
