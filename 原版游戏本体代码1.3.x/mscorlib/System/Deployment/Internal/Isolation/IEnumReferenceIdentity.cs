using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000693 RID: 1683
	[Guid("b30352cf-23da-4577-9b3f-b4e6573be53b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumReferenceIdentity
	{
		// Token: 0x06004F92 RID: 20370
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] IReferenceIdentity[] ReferenceIdentity);

		// Token: 0x06004F93 RID: 20371
		[SecurityCritical]
		void Skip(uint celt);

		// Token: 0x06004F94 RID: 20372
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F95 RID: 20373
		[SecurityCritical]
		IEnumReferenceIdentity Clone();
	}
}
