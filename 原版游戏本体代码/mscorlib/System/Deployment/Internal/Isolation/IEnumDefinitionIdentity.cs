using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000692 RID: 1682
	[Guid("f3549d9c-fc73-4793-9c00-1cd204254c0c")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumDefinitionIdentity
	{
		// Token: 0x06004F8E RID: 20366
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] IDefinitionIdentity[] DefinitionIdentity);

		// Token: 0x06004F8F RID: 20367
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F90 RID: 20368
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F91 RID: 20369
		[SecurityCritical]
		IEnumDefinitionIdentity Clone();
	}
}
