using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B0 RID: 1712
	internal struct IStore_BindingResult
	{
		// Token: 0x0400226D RID: 8813
		[MarshalAs(UnmanagedType.U4)]
		public uint Flags;

		// Token: 0x0400226E RID: 8814
		[MarshalAs(UnmanagedType.U4)]
		public uint Disposition;

		// Token: 0x0400226F RID: 8815
		public IStore_BindingResult_BoundVersion Component;

		// Token: 0x04002270 RID: 8816
		public Guid CacheCoherencyGuid;

		// Token: 0x04002271 RID: 8817
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr Reserved;
	}
}
