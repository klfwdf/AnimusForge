using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006FE RID: 1790
	[StructLayout(LayoutKind.Sequential)]
	internal class PermissionSetEntry
	{
		// Token: 0x0400238B RID: 9099
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Id;

		// Token: 0x0400238C RID: 9100
		[MarshalAs(UnmanagedType.LPWStr)]
		public string XmlSegment;
	}
}
