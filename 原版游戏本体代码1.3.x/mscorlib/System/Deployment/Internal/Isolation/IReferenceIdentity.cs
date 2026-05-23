using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200068F RID: 1679
	[Guid("6eaf5ace-7917-4f3c-b129-e046a9704766")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IReferenceIdentity
	{
		// Token: 0x06004F81 RID: 20353
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string GetAttribute([MarshalAs(UnmanagedType.LPWStr)] [In] string Namespace, [MarshalAs(UnmanagedType.LPWStr)] [In] string Name);

		// Token: 0x06004F82 RID: 20354
		[SecurityCritical]
		void SetAttribute([MarshalAs(UnmanagedType.LPWStr)] [In] string Namespace, [MarshalAs(UnmanagedType.LPWStr)] [In] string Name, [MarshalAs(UnmanagedType.LPWStr)] [In] string Value);

		// Token: 0x06004F83 RID: 20355
		[SecurityCritical]
		IEnumIDENTITY_ATTRIBUTE EnumAttributes();

		// Token: 0x06004F84 RID: 20356
		[SecurityCritical]
		IReferenceIdentity Clone([In] IntPtr cDeltas, [MarshalAs(UnmanagedType.LPArray)] [In] IDENTITY_ATTRIBUTE[] Deltas);
	}
}
