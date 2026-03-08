using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006FA RID: 1786
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("70A4ECEE-B195-4c59-85BF-44B6ACA83F07")]
	[ComImport]
	internal interface IResourceTableMappingEntry
	{
		// Token: 0x17000D19 RID: 3353
		// (get) Token: 0x060050CD RID: 20685
		ResourceTableMappingEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D1A RID: 3354
		// (get) Token: 0x060050CE RID: 20686
		string id
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D1B RID: 3355
		// (get) Token: 0x060050CF RID: 20687
		string FinalStringMapped
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
