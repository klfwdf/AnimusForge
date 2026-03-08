using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000026 RID: 38
	[SecurityCritical]
	public sealed class SafeAccessTokenHandle : SafeHandle
	{
		// Token: 0x0600017A RID: 378 RVA: 0x000048DB File Offset: 0x00002ADB
		private SafeAccessTokenHandle()
			: base(IntPtr.Zero, true)
		{
		}

		// Token: 0x0600017B RID: 379 RVA: 0x000048E9 File Offset: 0x00002AE9
		public SafeAccessTokenHandle(IntPtr handle)
			: base(IntPtr.Zero, true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600017C RID: 380 RVA: 0x000048FE File Offset: 0x00002AFE
		public static SafeAccessTokenHandle InvalidHandle
		{
			[SecurityCritical]
			get
			{
				return new SafeAccessTokenHandle(IntPtr.Zero);
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600017D RID: 381 RVA: 0x0000490A File Offset: 0x00002B0A
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == IntPtr.Zero || this.handle == new IntPtr(-1);
			}
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00004931 File Offset: 0x00002B31
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.CloseHandle(this.handle);
		}
	}
}
