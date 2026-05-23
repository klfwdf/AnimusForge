using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000299 RID: 665
	[ComVisible(true)]
	public class SHA512Managed : SHA512
	{
		// Token: 0x06002373 RID: 9075 RVA: 0x00080681 File Offset: 0x0007E881
		public SHA512Managed()
		{
			if (CryptoConfig.AllowOnlyFipsAlgorithms && AppContextSwitches.UseLegacyFipsThrow)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
			}
			this._impl = SHA512Managed._factory.CreateInstance();
		}

		// Token: 0x06002374 RID: 9076 RVA: 0x000806B7 File Offset: 0x0007E8B7
		public override void Initialize()
		{
			this._impl.Initialize();
		}

		// Token: 0x06002375 RID: 9077 RVA: 0x000806C4 File Offset: 0x0007E8C4
		[SecuritySafeCritical]
		protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
		{
			this._impl.TransformBlock(rgb, ibStart, cbSize, null, 0);
		}

		// Token: 0x06002376 RID: 9078 RVA: 0x000806D7 File Offset: 0x0007E8D7
		[SecuritySafeCritical]
		protected override byte[] HashFinal()
		{
			this._impl.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			return this._impl.Hash;
		}

		// Token: 0x06002377 RID: 9079 RVA: 0x000806F7 File Offset: 0x0007E8F7
		protected override void Dispose(bool disposing)
		{
			this._impl.Dispose();
			base.Dispose(disposing);
		}

		// Token: 0x04000CE9 RID: 3305
		private static readonly CngHashAlgorithmFactory<SHA512> _factory = new CngHashAlgorithmFactory<SHA512>("System.Security.Cryptography.SHA512Cng");

		// Token: 0x04000CEA RID: 3306
		private SHA512 _impl;
	}
}
