using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006DA RID: 1754
	[StructLayout(LayoutKind.Sequential)]
	internal class FileAssociationEntry
	{
		// Token: 0x04002328 RID: 9000
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Extension;

		// Token: 0x04002329 RID: 9001
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Description;

		// Token: 0x0400232A RID: 9002
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ProgID;

		// Token: 0x0400232B RID: 9003
		[MarshalAs(UnmanagedType.LPWStr)]
		public string DefaultIcon;

		// Token: 0x0400232C RID: 9004
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Parameter;
	}
}
