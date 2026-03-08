using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C0 RID: 704
	[SecurityCritical]
	[SuppressUnmanagedCodeSecurity]
	internal sealed class SafeBCryptAlgorithmHandle : SafeHandle
	{
		// Token: 0x06002510 RID: 9488
		[SecurityCritical]
		[DllImport("bcrypt.dll")]
		private static extern int BCryptCloseAlgorithmProvider([In] IntPtr hAlgorithm, [In] uint dwFlags);

		// Token: 0x06002511 RID: 9489 RVA: 0x00086F16 File Offset: 0x00085116
		[SecurityCritical]
		public SafeBCryptAlgorithmHandle()
			: base(IntPtr.Zero, true)
		{
		}

		// Token: 0x1700049C RID: 1180
		// (get) Token: 0x06002512 RID: 9490 RVA: 0x00086F24 File Offset: 0x00085124
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == IntPtr.Zero;
			}
		}

		// Token: 0x06002513 RID: 9491 RVA: 0x00086F38 File Offset: 0x00085138
		[SecurityCritical]
		protected sealed override bool ReleaseHandle()
		{
			int num = SafeBCryptAlgorithmHandle.BCryptCloseAlgorithmProvider(this.handle, 0U);
			return num == 0;
		}
	}
}
