using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000701 RID: 1793
	[StructLayout(LayoutKind.Sequential)]
	internal class AssemblyRequestEntry
	{
		// Token: 0x0400238F RID: 9103
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		// Token: 0x04002390 RID: 9104
		[MarshalAs(UnmanagedType.LPWStr)]
		public string permissionSetID;
	}
}
