using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000687 RID: 1671
	[Guid("a5c6aaa3-03e4-478d-b9f5-2e45908d5e4f")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_ASSEMBLY_FILE
	{
		// Token: 0x06004F55 RID: 20309
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] STORE_ASSEMBLY_FILE[] rgelt);

		// Token: 0x06004F56 RID: 20310
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F57 RID: 20311
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F58 RID: 20312
		[SecurityCritical]
		IEnumSTORE_ASSEMBLY_FILE Clone();
	}
}
