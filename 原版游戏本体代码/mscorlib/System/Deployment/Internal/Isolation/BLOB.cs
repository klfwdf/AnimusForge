using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000674 RID: 1652
	internal struct BLOB : IDisposable
	{
		// Token: 0x06004F2F RID: 20271 RVA: 0x0011C41F File Offset: 0x0011A61F
		[SecuritySafeCritical]
		public void Dispose()
		{
			if (this.BlobData != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.BlobData);
				this.BlobData = IntPtr.Zero;
			}
		}

		// Token: 0x040021E0 RID: 8672
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x040021E1 RID: 8673
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr BlobData;
	}
}
