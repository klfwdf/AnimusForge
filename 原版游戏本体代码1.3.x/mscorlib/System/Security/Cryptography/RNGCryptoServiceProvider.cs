using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000247 RID: 583
	[ComVisible(true)]
	public sealed class RNGCryptoServiceProvider : RandomNumberGenerator
	{
		// Token: 0x060020C5 RID: 8389 RVA: 0x00072AA8 File Offset: 0x00070CA8
		public RNGCryptoServiceProvider()
			: this(null)
		{
		}

		// Token: 0x060020C6 RID: 8390 RVA: 0x00072AB1 File Offset: 0x00070CB1
		public RNGCryptoServiceProvider(string str)
			: this(null)
		{
		}

		// Token: 0x060020C7 RID: 8391 RVA: 0x00072ABA File Offset: 0x00070CBA
		public RNGCryptoServiceProvider(byte[] rgb)
			: this(null)
		{
		}

		// Token: 0x060020C8 RID: 8392 RVA: 0x00072AC3 File Offset: 0x00070CC3
		[SecuritySafeCritical]
		public RNGCryptoServiceProvider(CspParameters cspParams)
		{
			if (cspParams != null)
			{
				this.m_safeProvHandle = Utils.AcquireProvHandle(cspParams);
				this.m_ownsHandle = true;
				return;
			}
			this.m_safeProvHandle = Utils.StaticProvHandle;
			this.m_ownsHandle = false;
		}

		// Token: 0x060020C9 RID: 8393 RVA: 0x00072AF4 File Offset: 0x00070CF4
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing && this.m_ownsHandle)
			{
				this.m_safeProvHandle.Dispose();
			}
		}

		// Token: 0x060020CA RID: 8394 RVA: 0x00072B13 File Offset: 0x00070D13
		[SecuritySafeCritical]
		public override void GetBytes(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			RNGCryptoServiceProvider.GetBytes(this.m_safeProvHandle, data, data.Length);
		}

		// Token: 0x060020CB RID: 8395 RVA: 0x00072B32 File Offset: 0x00070D32
		[SecuritySafeCritical]
		public override void GetNonZeroBytes(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			RNGCryptoServiceProvider.GetNonZeroBytes(this.m_safeProvHandle, data, data.Length);
		}

		// Token: 0x060020CC RID: 8396
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetBytes(SafeProvHandle hProv, byte[] randomBytes, int count);

		// Token: 0x060020CD RID: 8397
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetNonZeroBytes(SafeProvHandle hProv, byte[] randomBytes, int count);

		// Token: 0x04000BE5 RID: 3045
		[SecurityCritical]
		private SafeProvHandle m_safeProvHandle;

		// Token: 0x04000BE6 RID: 3046
		private bool m_ownsHandle;
	}
}
