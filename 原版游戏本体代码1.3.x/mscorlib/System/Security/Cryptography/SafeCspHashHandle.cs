using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
	// Token: 0x0200023E RID: 574
	[SecurityCritical]
	internal sealed class SafeCspHashHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x060020A1 RID: 8353 RVA: 0x0007287B File Offset: 0x00070A7B
		private SafeCspHashHandle()
			: base(true)
		{
		}

		// Token: 0x060020A2 RID: 8354
		[SuppressUnmanagedCodeSecurity]
		[DllImport("advapi32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptDestroyHash(IntPtr hKey);

		// Token: 0x060020A3 RID: 8355 RVA: 0x00072884 File Offset: 0x00070A84
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return SafeCspHashHandle.CryptDestroyHash(this.handle);
		}
	}
}
