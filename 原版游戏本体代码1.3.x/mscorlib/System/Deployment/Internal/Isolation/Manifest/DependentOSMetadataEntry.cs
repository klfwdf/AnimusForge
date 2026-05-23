using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x0200070A RID: 1802
	[StructLayout(LayoutKind.Sequential)]
	internal class DependentOSMetadataEntry
	{
		// Token: 0x040023AB RID: 9131
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SupportUrl;

		// Token: 0x040023AC RID: 9132
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Description;

		// Token: 0x040023AD RID: 9133
		public ushort MajorVersion;

		// Token: 0x040023AE RID: 9134
		public ushort MinorVersion;

		// Token: 0x040023AF RID: 9135
		public ushort BuildNumber;

		// Token: 0x040023B0 RID: 9136
		public byte ServicePackMajor;

		// Token: 0x040023B1 RID: 9137
		public byte ServicePackMinor;
	}
}
