using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000681 RID: 1665
	[Guid("f9fd4090-93db-45c0-af87-624940f19cff")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_DEPLOYMENT_METADATA
	{
		// Token: 0x06004F34 RID: 20276
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] IDefinitionAppId[] AppIds);

		// Token: 0x06004F35 RID: 20277
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F36 RID: 20278
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F37 RID: 20279
		[SecurityCritical]
		IEnumSTORE_DEPLOYMENT_METADATA Clone();
	}
}
