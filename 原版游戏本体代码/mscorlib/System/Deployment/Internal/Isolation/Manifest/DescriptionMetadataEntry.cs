using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000704 RID: 1796
	[StructLayout(LayoutKind.Sequential)]
	internal class DescriptionMetadataEntry
	{
		// Token: 0x04002393 RID: 9107
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Publisher;

		// Token: 0x04002394 RID: 9108
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Product;

		// Token: 0x04002395 RID: 9109
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SupportUrl;

		// Token: 0x04002396 RID: 9110
		[MarshalAs(UnmanagedType.LPWStr)]
		public string IconFile;

		// Token: 0x04002397 RID: 9111
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ErrorReportUrl;

		// Token: 0x04002398 RID: 9112
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SuiteName;
	}
}
