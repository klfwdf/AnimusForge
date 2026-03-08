using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000297 RID: 663
	[ComVisible(true)]
	public class SHA384Managed : SHA384
	{
		// Token: 0x0600236A RID: 9066 RVA: 0x000805BA File Offset: 0x0007E7BA
		public SHA384Managed()
		{
			if (CryptoConfig.AllowOnlyFipsAlgorithms && AppContextSwitches.UseLegacyFipsThrow)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
			}
			this._impl = SHA384Managed._factory.CreateInstance();
		}

		// Token: 0x0600236B RID: 9067 RVA: 0x000805F0 File Offset: 0x0007E7F0
		public override void Initialize()
		{
			this._impl.Initialize();
		}

		// Token: 0x0600236C RID: 9068 RVA: 0x000805FD File Offset: 0x0007E7FD
		[SecuritySafeCritical]
		protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
		{
			this._impl.TransformBlock(rgb, ibStart, cbSize, null, 0);
		}

		// Token: 0x0600236D RID: 9069 RVA: 0x00080610 File Offset: 0x0007E810
		[SecuritySafeCritical]
		protected override byte[] HashFinal()
		{
			this._impl.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			return this._impl.Hash;
		}

		// Token: 0x0600236E RID: 9070 RVA: 0x00080630 File Offset: 0x0007E830
		protected override void Dispose(bool disposing)
		{
			this._impl.Dispose();
			base.Dispose(disposing);
		}

		// Token: 0x04000CE7 RID: 3303
		private static readonly CngHashAlgorithmFactory<SHA384> _factory = new CngHashAlgorithmFactory<SHA384>("System.Security.Cryptography.SHA384Cng");

		// Token: 0x04000CE8 RID: 3304
		private SHA384 _impl;
	}
}
