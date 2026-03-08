using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x0200001D RID: 29
	[SecurityCritical]
	internal sealed class SafeLocalAllocHandle : SafeBuffer
	{
		// Token: 0x06000161 RID: 353 RVA: 0x00004761 File Offset: 0x00002961
		private SafeLocalAllocHandle()
			: base(true)
		{
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000476A File Offset: 0x0000296A
		internal SafeLocalAllocHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000163 RID: 355 RVA: 0x0000477A File Offset: 0x0000297A
		internal static SafeLocalAllocHandle InvalidHandle
		{
			get
			{
				return new SafeLocalAllocHandle(IntPtr.Zero);
			}
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00004786 File Offset: 0x00002986
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.LocalFree(this.handle) == IntPtr.Zero;
		}
	}
}
