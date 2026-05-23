using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000670 RID: 1648
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("285a8876-c84a-11d7-850f-005cd062464f")]
	[ComImport]
	internal interface ISectionWithReferenceIdentityKey
	{
		// Token: 0x06004F23 RID: 20259
		void Lookup(IReferenceIdentity ReferenceIdentityKey, [MarshalAs(UnmanagedType.Interface)] out object ppUnknown);
	}
}
