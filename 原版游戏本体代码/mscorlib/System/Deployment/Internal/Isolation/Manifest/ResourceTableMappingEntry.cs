using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006F8 RID: 1784
	[StructLayout(LayoutKind.Sequential)]
	internal class ResourceTableMappingEntry
	{
		// Token: 0x0400237D RID: 9085
		[MarshalAs(UnmanagedType.LPWStr)]
		public string id;

		// Token: 0x0400237E RID: 9086
		[MarshalAs(UnmanagedType.LPWStr)]
		public string FinalStringMapped;
	}
}
