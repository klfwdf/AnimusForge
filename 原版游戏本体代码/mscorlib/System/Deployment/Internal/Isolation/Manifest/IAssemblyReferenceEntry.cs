using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006F4 RID: 1780
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("FD47B733-AFBC-45e4-B7C2-BBEB1D9F766C")]
	[ComImport]
	internal interface IAssemblyReferenceEntry
	{
		// Token: 0x17000D11 RID: 3345
		// (get) Token: 0x060050C3 RID: 20675
		AssemblyReferenceEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D12 RID: 3346
		// (get) Token: 0x060050C4 RID: 20676
		IReferenceIdentity ReferenceIdentity
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D13 RID: 3347
		// (get) Token: 0x060050C5 RID: 20677
		uint Flags
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000D14 RID: 3348
		// (get) Token: 0x060050C6 RID: 20678
		IAssemblyReferenceDependentAssemblyEntry DependentAssembly
		{
			[SecurityCritical]
			get;
		}
	}
}
