using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006CB RID: 1739
	[StructLayout(LayoutKind.Sequential)]
	internal class MuiResourceTypeIdStringEntry : IDisposable
	{
		// Token: 0x06005059 RID: 20569 RVA: 0x0011DA48 File Offset: 0x0011BC48
		~MuiResourceTypeIdStringEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x0600505A RID: 20570 RVA: 0x0011DA78 File Offset: 0x0011BC78
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x0600505B RID: 20571 RVA: 0x0011DA84 File Offset: 0x0011BC84
		[SecuritySafeCritical]
		public void Dispose(bool fDisposing)
		{
			if (this.StringIds != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.StringIds);
				this.StringIds = IntPtr.Zero;
			}
			if (this.IntegerIds != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.IntegerIds);
				this.IntegerIds = IntPtr.Zero;
			}
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x040022DF RID: 8927
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr StringIds;

		// Token: 0x040022E0 RID: 8928
		public uint StringIdsSize;

		// Token: 0x040022E1 RID: 8929
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr IntegerIds;

		// Token: 0x040022E2 RID: 8930
		public uint IntegerIdsSize;
	}
}
