using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D1 RID: 1745
	[StructLayout(LayoutKind.Sequential)]
	internal class MuiResourceMapEntry : IDisposable
	{
		// Token: 0x06005067 RID: 20583 RVA: 0x0011DBA0 File Offset: 0x0011BDA0
		~MuiResourceMapEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x06005068 RID: 20584 RVA: 0x0011DBD0 File Offset: 0x0011BDD0
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06005069 RID: 20585 RVA: 0x0011DBDC File Offset: 0x0011BDDC
		[SecuritySafeCritical]
		public void Dispose(bool fDisposing)
		{
			if (this.ResourceTypeIdInt != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.ResourceTypeIdInt);
				this.ResourceTypeIdInt = IntPtr.Zero;
			}
			if (this.ResourceTypeIdString != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.ResourceTypeIdString);
				this.ResourceTypeIdString = IntPtr.Zero;
			}
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x040022F1 RID: 8945
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr ResourceTypeIdInt;

		// Token: 0x040022F2 RID: 8946
		public uint ResourceTypeIdIntSize;

		// Token: 0x040022F3 RID: 8947
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr ResourceTypeIdString;

		// Token: 0x040022F4 RID: 8948
		public uint ResourceTypeIdStringSize;
	}
}
