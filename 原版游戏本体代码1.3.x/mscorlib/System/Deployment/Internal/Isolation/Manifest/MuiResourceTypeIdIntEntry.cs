using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006CE RID: 1742
	[StructLayout(LayoutKind.Sequential)]
	internal class MuiResourceTypeIdIntEntry : IDisposable
	{
		// Token: 0x06005060 RID: 20576 RVA: 0x0011DAF4 File Offset: 0x0011BCF4
		~MuiResourceTypeIdIntEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x06005061 RID: 20577 RVA: 0x0011DB24 File Offset: 0x0011BD24
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06005062 RID: 20578 RVA: 0x0011DB30 File Offset: 0x0011BD30
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

		// Token: 0x040022E8 RID: 8936
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr StringIds;

		// Token: 0x040022E9 RID: 8937
		public uint StringIdsSize;

		// Token: 0x040022EA RID: 8938
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr IntegerIds;

		// Token: 0x040022EB RID: 8939
		public uint IntegerIdsSize;
	}
}
