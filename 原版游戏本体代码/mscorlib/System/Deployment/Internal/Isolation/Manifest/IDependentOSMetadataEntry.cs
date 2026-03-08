using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x0200070C RID: 1804
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("CF168CF4-4E8F-4d92-9D2A-60E5CA21CF85")]
	[ComImport]
	internal interface IDependentOSMetadataEntry
	{
		// Token: 0x17000D35 RID: 3381
		// (get) Token: 0x060050EF RID: 20719
		DependentOSMetadataEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D36 RID: 3382
		// (get) Token: 0x060050F0 RID: 20720
		string SupportUrl
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D37 RID: 3383
		// (get) Token: 0x060050F1 RID: 20721
		string Description
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D38 RID: 3384
		// (get) Token: 0x060050F2 RID: 20722
		ushort MajorVersion
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D39 RID: 3385
		// (get) Token: 0x060050F3 RID: 20723
		ushort MinorVersion
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D3A RID: 3386
		// (get) Token: 0x060050F4 RID: 20724
		ushort BuildNumber
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D3B RID: 3387
		// (get) Token: 0x060050F5 RID: 20725
		byte ServicePackMajor
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D3C RID: 3388
		// (get) Token: 0x060050F6 RID: 20726
		byte ServicePackMinor
		{
			[SecurityCritical]
			get;
		}
	}
}
