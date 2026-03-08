using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	// Token: 0x02000009 RID: 9
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
	[ComImport]
	internal interface IAssemblyName
	{
		// Token: 0x0600000F RID: 15
		[PreserveSig]
		int SetProperty(uint PropertyId, IntPtr pvProperty, uint cbProperty);

		// Token: 0x06000010 RID: 16
		[PreserveSig]
		int GetProperty(uint PropertyId, IntPtr pvProperty, ref uint pcbProperty);

		// Token: 0x06000011 RID: 17
		[PreserveSig]
		int Finalize();

		// Token: 0x06000012 RID: 18
		[PreserveSig]
		int GetDisplayName(IntPtr szDisplayName, ref uint pccDisplayName, uint dwDisplayFlags);

		// Token: 0x06000013 RID: 19
		[PreserveSig]
		int BindToObject(object refIID, object pAsmBindSink, IApplicationContext pApplicationContext, [MarshalAs(UnmanagedType.LPWStr)] string szCodeBase, long llFlags, int pvReserved, uint cbReserved, out int ppv);

		// Token: 0x06000014 RID: 20
		[PreserveSig]
		int GetName(out uint lpcwBuffer, out int pwzName);

		// Token: 0x06000015 RID: 21
		[PreserveSig]
		int GetVersion(out uint pdwVersionHi, out uint pdwVersionLow);

		// Token: 0x06000016 RID: 22
		[PreserveSig]
		int IsEqual(IAssemblyName pName, uint dwCmpFlags);

		// Token: 0x06000017 RID: 23
		[PreserveSig]
		int Clone(out IAssemblyName pName);
	}
}
