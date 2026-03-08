using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000683 RID: 1667
	[Guid("5fa4f590-a416-4b22-ac79-7c3f0d31f303")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY
	{
		// Token: 0x06004F3F RID: 20287
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] StoreOperationMetadataProperty[] AppIds);

		// Token: 0x06004F40 RID: 20288
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F41 RID: 20289
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F42 RID: 20290
		[SecurityCritical]
		IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY Clone();
	}
}
