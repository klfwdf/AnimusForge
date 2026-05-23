using System;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000029 RID: 41
	[SecurityCritical]
	internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06000187 RID: 391 RVA: 0x000049AB File Offset: 0x00002BAB
		private SafeLsaPolicyHandle()
			: base(true)
		{
		}

		// Token: 0x06000188 RID: 392 RVA: 0x000049B4 File Offset: 0x00002BB4
		internal SafeLsaPolicyHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000189 RID: 393 RVA: 0x000049C4 File Offset: 0x00002BC4
		internal static SafeLsaPolicyHandle InvalidHandle
		{
			get
			{
				return new SafeLsaPolicyHandle(IntPtr.Zero);
			}
		}

		// Token: 0x0600018A RID: 394 RVA: 0x000049D0 File Offset: 0x00002BD0
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.LsaClose(this.handle) == 0;
		}
	}
}
