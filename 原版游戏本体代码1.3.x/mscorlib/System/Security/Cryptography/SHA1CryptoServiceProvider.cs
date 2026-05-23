using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000292 RID: 658
	[ComVisible(true)]
	public sealed class SHA1CryptoServiceProvider : SHA1
	{
		// Token: 0x06002354 RID: 9044 RVA: 0x0008037D File Offset: 0x0007E57D
		[SecuritySafeCritical]
		public SHA1CryptoServiceProvider()
		{
			this._safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, 32772);
		}

		// Token: 0x06002355 RID: 9045 RVA: 0x0008039A File Offset: 0x0007E59A
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			if (this._safeHashHandle != null && !this._safeHashHandle.IsClosed)
			{
				this._safeHashHandle.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x06002356 RID: 9046 RVA: 0x000803C3 File Offset: 0x0007E5C3
		[SecuritySafeCritical]
		public override void Initialize()
		{
			if (this._safeHashHandle != null && !this._safeHashHandle.IsClosed)
			{
				this._safeHashHandle.Dispose();
			}
			this._safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, 32772);
		}

		// Token: 0x06002357 RID: 9047 RVA: 0x000803FA File Offset: 0x0007E5FA
		[SecuritySafeCritical]
		protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
		{
			Utils.HashData(this._safeHashHandle, rgb, ibStart, cbSize);
		}

		// Token: 0x06002358 RID: 9048 RVA: 0x0008040A File Offset: 0x0007E60A
		[SecuritySafeCritical]
		protected override byte[] HashFinal()
		{
			return Utils.EndHash(this._safeHashHandle);
		}

		// Token: 0x04000CE3 RID: 3299
		[SecurityCritical]
		private SafeHashHandle _safeHashHandle;
	}
}
