using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009A8 RID: 2472
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.ITypeLib instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("00020402-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface UCOMITypeLib
	{
		// Token: 0x060062F1 RID: 25329
		[PreserveSig]
		int GetTypeInfoCount();

		// Token: 0x060062F2 RID: 25330
		void GetTypeInfo(int index, out UCOMITypeInfo ppTI);

		// Token: 0x060062F3 RID: 25331
		void GetTypeInfoType(int index, out TYPEKIND pTKind);

		// Token: 0x060062F4 RID: 25332
		void GetTypeInfoOfGuid(ref Guid guid, out UCOMITypeInfo ppTInfo);

		// Token: 0x060062F5 RID: 25333
		void GetLibAttr(out IntPtr ppTLibAttr);

		// Token: 0x060062F6 RID: 25334
		void GetTypeComp(out UCOMITypeComp ppTComp);

		// Token: 0x060062F7 RID: 25335
		void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);

		// Token: 0x060062F8 RID: 25336
		[return: MarshalAs(UnmanagedType.Bool)]
		bool IsName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal);

		// Token: 0x060062F9 RID: 25337
		void FindName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray)] [Out] UCOMITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray)] [Out] int[] rgMemId, ref short pcFound);

		// Token: 0x060062FA RID: 25338
		[PreserveSig]
		void ReleaseTLibAttr(IntPtr pTLibAttr);
	}
}
