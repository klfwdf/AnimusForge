using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000675 RID: 1653
	internal struct IDENTITY_ATTRIBUTE
	{
		// Token: 0x040021E2 RID: 8674
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Namespace;

		// Token: 0x040021E3 RID: 8675
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		// Token: 0x040021E4 RID: 8676
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Value;
	}
}
