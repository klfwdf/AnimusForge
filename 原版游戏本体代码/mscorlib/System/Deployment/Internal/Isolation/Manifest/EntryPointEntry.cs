using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006FB RID: 1787
	[StructLayout(LayoutKind.Sequential)]
	internal class EntryPointEntry
	{
		// Token: 0x04002381 RID: 9089
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		// Token: 0x04002382 RID: 9090
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CommandLine_File;

		// Token: 0x04002383 RID: 9091
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CommandLine_Parameters;

		// Token: 0x04002384 RID: 9092
		public IReferenceIdentity Identity;

		// Token: 0x04002385 RID: 9093
		public uint Flags;
	}
}
