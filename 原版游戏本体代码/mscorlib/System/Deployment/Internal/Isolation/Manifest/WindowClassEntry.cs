using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006F5 RID: 1781
	[StructLayout(LayoutKind.Sequential)]
	internal class WindowClassEntry
	{
		// Token: 0x04002377 RID: 9079
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ClassName;

		// Token: 0x04002378 RID: 9080
		[MarshalAs(UnmanagedType.LPWStr)]
		public string HostDll;

		// Token: 0x04002379 RID: 9081
		public bool fVersioned;
	}
}
