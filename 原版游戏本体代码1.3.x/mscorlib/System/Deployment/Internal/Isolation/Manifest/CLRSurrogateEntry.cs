using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006EC RID: 1772
	[StructLayout(LayoutKind.Sequential)]
	internal class CLRSurrogateEntry
	{
		// Token: 0x04002354 RID: 9044
		public Guid Clsid;

		// Token: 0x04002355 RID: 9045
		[MarshalAs(UnmanagedType.LPWStr)]
		public string RuntimeVersion;

		// Token: 0x04002356 RID: 9046
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ClassName;
	}
}
