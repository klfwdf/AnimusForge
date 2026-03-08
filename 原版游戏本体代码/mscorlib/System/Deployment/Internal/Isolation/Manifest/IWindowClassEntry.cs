using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006F7 RID: 1783
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("8AD3FC86-AFD3-477a-8FD5-146C291195BA")]
	[ComImport]
	internal interface IWindowClassEntry
	{
		// Token: 0x17000D15 RID: 3349
		// (get) Token: 0x060050C8 RID: 20680
		WindowClassEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D16 RID: 3350
		// (get) Token: 0x060050C9 RID: 20681
		string ClassName
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D17 RID: 3351
		// (get) Token: 0x060050CA RID: 20682
		string HostDll
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D18 RID: 3352
		// (get) Token: 0x060050CB RID: 20683
		bool fVersioned
		{
			[SecurityCritical]
			get;
		}
	}
}
