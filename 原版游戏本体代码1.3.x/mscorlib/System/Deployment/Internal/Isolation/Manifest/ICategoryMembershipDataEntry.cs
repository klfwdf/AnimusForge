using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006DF RID: 1759
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("DA0C3B27-6B6B-4b80-A8F8-6CE14F4BC0A4")]
	[ComImport]
	internal interface ICategoryMembershipDataEntry
	{
		// Token: 0x17000CEC RID: 3308
		// (get) Token: 0x06005094 RID: 20628
		CategoryMembershipDataEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CED RID: 3309
		// (get) Token: 0x06005095 RID: 20629
		uint index
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CEE RID: 3310
		// (get) Token: 0x06005096 RID: 20630
		string Xml
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CEF RID: 3311
		// (get) Token: 0x06005097 RID: 20631
		string Description
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
