using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D3 RID: 1747
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("397927f5-10f2-4ecb-bfe1-3c264212a193")]
	[ComImport]
	internal interface IMuiResourceMapEntry
	{
		// Token: 0x17000CCD RID: 3277
		// (get) Token: 0x0600506B RID: 20587
		MuiResourceMapEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CCE RID: 3278
		// (get) Token: 0x0600506C RID: 20588
		object ResourceTypeIdInt
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		// Token: 0x17000CCF RID: 3279
		// (get) Token: 0x0600506D RID: 20589
		object ResourceTypeIdString
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
	}
}
