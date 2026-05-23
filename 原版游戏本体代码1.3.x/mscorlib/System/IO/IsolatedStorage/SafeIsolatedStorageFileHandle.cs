using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B7 RID: 439
	[SecurityCritical]
	internal sealed class SafeIsolatedStorageFileHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06001BC2 RID: 7106
		[SuppressUnmanagedCodeSecurity]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void Close(IntPtr file);

		// Token: 0x06001BC3 RID: 7107 RVA: 0x0005F6D8 File Offset: 0x0005D8D8
		private SafeIsolatedStorageFileHandle()
			: base(true)
		{
			base.SetHandle(IntPtr.Zero);
		}

		// Token: 0x06001BC4 RID: 7108 RVA: 0x0005F6EC File Offset: 0x0005D8EC
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeIsolatedStorageFileHandle.Close(this.handle);
			return true;
		}
	}
}
