using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000272 RID: 626
	[ComVisible(true)]
	public sealed class MD5CryptoServiceProvider : MD5
	{
		// Token: 0x0600222A RID: 8746 RVA: 0x00078D0D File Offset: 0x00076F0D
		[SecuritySafeCritical]
		public MD5CryptoServiceProvider()
		{
			if (CryptoConfig.AllowOnlyFipsAlgorithms && AppContextSwitches.UseLegacyFipsThrow)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
			}
			this._safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, 32771);
		}

		// Token: 0x0600222B RID: 8747 RVA: 0x00078D48 File Offset: 0x00076F48
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			if (this._safeHashHandle != null && !this._safeHashHandle.IsClosed)
			{
				this._safeHashHandle.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x0600222C RID: 8748 RVA: 0x00078D71 File Offset: 0x00076F71
		[SecuritySafeCritical]
		public override void Initialize()
		{
			if (this._safeHashHandle != null && !this._safeHashHandle.IsClosed)
			{
				this._safeHashHandle.Dispose();
			}
			this._safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, 32771);
		}

		// Token: 0x0600222D RID: 8749 RVA: 0x00078DA8 File Offset: 0x00076FA8
		[SecuritySafeCritical]
		protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
		{
			Utils.HashData(this._safeHashHandle, rgb, ibStart, cbSize);
		}

		// Token: 0x0600222E RID: 8750 RVA: 0x00078DB8 File Offset: 0x00076FB8
		[SecuritySafeCritical]
		protected override byte[] HashFinal()
		{
			return Utils.EndHash(this._safeHashHandle);
		}

		// Token: 0x04000C6A RID: 3178
		[SecurityCritical]
		private SafeHashHandle _safeHashHandle;
	}
}
