using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006FD RID: 1789
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("1583EFE9-832F-4d08-B041-CAC5ACEDB948")]
	[ComImport]
	internal interface IEntryPointEntry
	{
		// Token: 0x17000D1C RID: 3356
		// (get) Token: 0x060050D1 RID: 20689
		EntryPointEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D1D RID: 3357
		// (get) Token: 0x060050D2 RID: 20690
		string Name
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D1E RID: 3358
		// (get) Token: 0x060050D3 RID: 20691
		string CommandLine_File
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D1F RID: 3359
		// (get) Token: 0x060050D4 RID: 20692
		string CommandLine_Parameters
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000D20 RID: 3360
		// (get) Token: 0x060050D5 RID: 20693
		IReferenceIdentity Identity
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D21 RID: 3361
		// (get) Token: 0x060050D6 RID: 20694
		uint Flags
		{
			[SecurityCritical]
			get;
		}
	}
}
