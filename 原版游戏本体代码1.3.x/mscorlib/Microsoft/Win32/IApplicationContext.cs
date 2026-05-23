using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	// Token: 0x02000008 RID: 8
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("7c23ff90-33af-11d3-95da-00a024a85b51")]
	[ComImport]
	internal interface IApplicationContext
	{
		// Token: 0x0600000A RID: 10
		void SetContextNameObject(IAssemblyName pName);

		// Token: 0x0600000B RID: 11
		void GetContextNameObject(out IAssemblyName ppName);

		// Token: 0x0600000C RID: 12
		void Set([MarshalAs(UnmanagedType.LPWStr)] string szName, int pvValue, uint cbValue, uint dwFlags);

		// Token: 0x0600000D RID: 13
		void Get([MarshalAs(UnmanagedType.LPWStr)] string szName, out int pvValue, ref uint pcbValue, uint dwFlags);

		// Token: 0x0600000E RID: 14
		void GetDynamicDirectory(out int wzDynamicDir, ref uint pdwSize);
	}
}
