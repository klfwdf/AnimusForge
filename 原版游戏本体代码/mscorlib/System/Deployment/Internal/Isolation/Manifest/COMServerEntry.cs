using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E6 RID: 1766
	[StructLayout(LayoutKind.Sequential)]
	internal class COMServerEntry
	{
		// Token: 0x04002340 RID: 9024
		public Guid Clsid;

		// Token: 0x04002341 RID: 9025
		public uint Flags;

		// Token: 0x04002342 RID: 9026
		public Guid ConfiguredGuid;

		// Token: 0x04002343 RID: 9027
		public Guid ImplementedClsid;

		// Token: 0x04002344 RID: 9028
		public Guid TypeLibrary;

		// Token: 0x04002345 RID: 9029
		public uint ThreadingModel;

		// Token: 0x04002346 RID: 9030
		[MarshalAs(UnmanagedType.LPWStr)]
		public string RuntimeVersion;

		// Token: 0x04002347 RID: 9031
		[MarshalAs(UnmanagedType.LPWStr)]
		public string HostFile;
	}
}
