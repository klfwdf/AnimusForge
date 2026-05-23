using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	// Token: 0x02000007 RID: 7
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
	[ComImport]
	internal interface IAssemblyEnum
	{
		// Token: 0x06000007 RID: 7
		[PreserveSig]
		int GetNextAssembly(out IApplicationContext ppAppCtx, out IAssemblyName ppName, uint dwFlags);

		// Token: 0x06000008 RID: 8
		[PreserveSig]
		int Reset();

		// Token: 0x06000009 RID: 9
		[PreserveSig]
		int Clone(out IAssemblyEnum ppEnum);
	}
}
