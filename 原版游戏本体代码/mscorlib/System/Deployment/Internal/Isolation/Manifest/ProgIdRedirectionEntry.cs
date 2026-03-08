using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E9 RID: 1769
	[StructLayout(LayoutKind.Sequential)]
	internal class ProgIdRedirectionEntry
	{
		// Token: 0x04002350 RID: 9040
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ProgId;

		// Token: 0x04002351 RID: 9041
		public Guid RedirectedGuid;
	}
}
