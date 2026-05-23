using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000689 RID: 1673
	[Guid("b840a2f5-a497-4a6d-9038-cd3ec2fbd222")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_CATEGORY
	{
		// Token: 0x06004F60 RID: 20320
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] STORE_CATEGORY[] rgElements);

		// Token: 0x06004F61 RID: 20321
		[SecurityCritical]
		void Skip([In] uint ulElements);

		// Token: 0x06004F62 RID: 20322
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F63 RID: 20323
		[SecurityCritical]
		IEnumSTORE_CATEGORY Clone();
	}
}
