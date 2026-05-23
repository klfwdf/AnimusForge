using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D0 RID: 1744
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("55b2dec1-d0f6-4bf4-91b1-30f73ad8e4df")]
	[ComImport]
	internal interface IMuiResourceTypeIdIntEntry
	{
		// Token: 0x17000CCA RID: 3274
		// (get) Token: 0x06005064 RID: 20580
		MuiResourceTypeIdIntEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CCB RID: 3275
		// (get) Token: 0x06005065 RID: 20581
		object StringIds
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		// Token: 0x17000CCC RID: 3276
		// (get) Token: 0x06005066 RID: 20582
		object IntegerIds
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
	}
}
