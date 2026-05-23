using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000703 RID: 1795
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("2474ECB4-8EFD-4410-9F31-B3E7C4A07731")]
	[ComImport]
	internal interface IAssemblyRequestEntry
	{
		// Token: 0x17000D25 RID: 3365
		// (get) Token: 0x060050DC RID: 20700
		AssemblyRequestEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D26 RID: 3366
		// (get) Token: 0x060050DD RID: 20701
		string Name
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D27 RID: 3367
		// (get) Token: 0x060050DE RID: 20702
		string permissionSetID
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
