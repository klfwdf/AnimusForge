using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000685 RID: 1669
	[Guid("a5c637bf-6eaa-4e5f-b535-55299657e33e")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_ASSEMBLY
	{
		// Token: 0x06004F4A RID: 20298
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] STORE_ASSEMBLY[] rgelt);

		// Token: 0x06004F4B RID: 20299
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F4C RID: 20300
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F4D RID: 20301
		[SecurityCritical]
		IEnumSTORE_ASSEMBLY Clone();
	}
}
