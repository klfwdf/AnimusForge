using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000709 RID: 1801
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("CFA3F59F-334D-46bf-A5A5-5D11BB2D7EBC")]
	[ComImport]
	internal interface IDeploymentMetadataEntry
	{
		// Token: 0x17000D2F RID: 3375
		// (get) Token: 0x060050E8 RID: 20712
		DeploymentMetadataEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D30 RID: 3376
		// (get) Token: 0x060050E9 RID: 20713
		string DeploymentProviderCodebase
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D31 RID: 3377
		// (get) Token: 0x060050EA RID: 20714
		string MinimumRequiredVersion
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D32 RID: 3378
		// (get) Token: 0x060050EB RID: 20715
		ushort MaximumAge
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D33 RID: 3379
		// (get) Token: 0x060050EC RID: 20716
		byte MaximumAge_Unit
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D34 RID: 3380
		// (get) Token: 0x060050ED RID: 20717
		uint DeploymentFlags
		{
			[SecurityCritical]
			get;
		}
	}
}
