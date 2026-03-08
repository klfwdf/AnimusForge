using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006DD RID: 1757
	[StructLayout(LayoutKind.Sequential)]
	internal class CategoryMembershipDataEntry
	{
		// Token: 0x04002332 RID: 9010
		public uint index;

		// Token: 0x04002333 RID: 9011
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Xml;

		// Token: 0x04002334 RID: 9012
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Description;
	}
}
