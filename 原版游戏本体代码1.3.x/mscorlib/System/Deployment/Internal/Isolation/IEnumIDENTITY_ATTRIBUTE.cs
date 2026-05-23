using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000691 RID: 1681
	[Guid("9cdaae75-246e-4b00-a26d-b9aec137a3eb")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IEnumIDENTITY_ATTRIBUTE
	{
		// Token: 0x06004F89 RID: 20361
		[SecurityCritical]
		uint Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray)] [Out] IDENTITY_ATTRIBUTE[] rgAttributes);

		// Token: 0x06004F8A RID: 20362
		[SecurityCritical]
		IntPtr CurrentIntoBuffer([In] IntPtr Available, [MarshalAs(UnmanagedType.LPArray)] [Out] byte[] Data);

		// Token: 0x06004F8B RID: 20363
		[SecurityCritical]
		void Skip([In] uint celt);

		// Token: 0x06004F8C RID: 20364
		[SecurityCritical]
		void Reset();

		// Token: 0x06004F8D RID: 20365
		[SecurityCritical]
		IEnumIDENTITY_ATTRIBUTE Clone();
	}
}
