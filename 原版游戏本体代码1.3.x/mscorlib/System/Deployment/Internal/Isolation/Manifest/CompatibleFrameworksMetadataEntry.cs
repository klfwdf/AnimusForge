using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x0200070D RID: 1805
	[StructLayout(LayoutKind.Sequential)]
	internal class CompatibleFrameworksMetadataEntry
	{
		// Token: 0x040023BA RID: 9146
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SupportUrl;
	}
}
