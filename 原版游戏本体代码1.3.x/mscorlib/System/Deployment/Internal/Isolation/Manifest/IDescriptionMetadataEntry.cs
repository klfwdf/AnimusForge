using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000706 RID: 1798
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("CB73147E-5FC2-4c31-B4E6-58D13DBE1A08")]
	[ComImport]
	internal interface IDescriptionMetadataEntry
	{
		// Token: 0x17000D28 RID: 3368
		// (get) Token: 0x060050E0 RID: 20704
		DescriptionMetadataEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D29 RID: 3369
		// (get) Token: 0x060050E1 RID: 20705
		string Publisher
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D2A RID: 3370
		// (get) Token: 0x060050E2 RID: 20706
		string Product
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D2B RID: 3371
		// (get) Token: 0x060050E3 RID: 20707
		string SupportUrl
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D2C RID: 3372
		// (get) Token: 0x060050E4 RID: 20708
		string IconFile
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D2D RID: 3373
		// (get) Token: 0x060050E5 RID: 20709
		string ErrorReportUrl
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D2E RID: 3374
		// (get) Token: 0x060050E6 RID: 20710
		string SuiteName
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
