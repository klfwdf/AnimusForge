using System;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x0200002B RID: 43
	[SecurityCritical]
	internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x0600018F RID: 399 RVA: 0x00004A18 File Offset: 0x00002C18
		private SafeProcessHandle()
			: base(true)
		{
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00004A21 File Offset: 0x00002C21
		internal SafeProcessHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000191 RID: 401 RVA: 0x00004A31 File Offset: 0x00002C31
		internal static SafeProcessHandle InvalidHandle
		{
			get
			{
				return new SafeProcessHandle(IntPtr.Zero);
			}
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00004A3D File Offset: 0x00002C3D
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.CloseHandle(this.handle);
		}
	}
}
