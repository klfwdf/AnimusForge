using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x0200070F RID: 1807
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("4A33D662-2210-463A-BE9F-FBDF1AA554E3")]
	[ComImport]
	internal interface ICompatibleFrameworksMetadataEntry
	{
		// Token: 0x17000D3D RID: 3389
		// (get) Token: 0x060050F8 RID: 20728
		CompatibleFrameworksMetadataEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D3E RID: 3390
		// (get) Token: 0x060050F9 RID: 20729
		string SupportUrl
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
