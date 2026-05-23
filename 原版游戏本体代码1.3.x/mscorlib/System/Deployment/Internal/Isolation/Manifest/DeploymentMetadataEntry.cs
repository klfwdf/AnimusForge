using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000707 RID: 1799
	[StructLayout(LayoutKind.Sequential)]
	internal class DeploymentMetadataEntry
	{
		// Token: 0x040023A0 RID: 9120
		[MarshalAs(UnmanagedType.LPWStr)]
		public string DeploymentProviderCodebase;

		// Token: 0x040023A1 RID: 9121
		[MarshalAs(UnmanagedType.LPWStr)]
		public string MinimumRequiredVersion;

		// Token: 0x040023A2 RID: 9122
		public ushort MaximumAge;

		// Token: 0x040023A3 RID: 9123
		public byte MaximumAge_Unit;

		// Token: 0x040023A4 RID: 9124
		public uint DeploymentFlags;
	}
}
