using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000680 RID: 1664
	[Guid("d8b1aacb-5142-4abb-bcc1-e9dc9052a89e")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE
	{
		// Token: 0x06004F30 RID: 20272
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] StoreApplicationReference[] rgelt);

		// Token: 0x06004F31 RID: 20273
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F32 RID: 20274
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F33 RID: 20275
		[SecurityCritical]
		IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE Clone();
	}
}
