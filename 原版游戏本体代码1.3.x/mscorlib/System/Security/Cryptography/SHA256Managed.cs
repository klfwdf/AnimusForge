using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000295 RID: 661
	[ComVisible(true)]
	public class SHA256Managed : SHA256
	{
		// Token: 0x06002361 RID: 9057 RVA: 0x000804F3 File Offset: 0x0007E6F3
		public SHA256Managed()
		{
			if (CryptoConfig.AllowOnlyFipsAlgorithms && AppContextSwitches.UseLegacyFipsThrow)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
			}
			this._impl = SHA256Managed._factory.CreateInstance();
		}

		// Token: 0x06002362 RID: 9058 RVA: 0x00080529 File Offset: 0x0007E729
		public override void Initialize()
		{
			this._impl.Initialize();
		}

		// Token: 0x06002363 RID: 9059 RVA: 0x00080536 File Offset: 0x0007E736
		protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
		{
			this._impl.TransformBlock(rgb, ibStart, cbSize, null, 0);
		}

		// Token: 0x06002364 RID: 9060 RVA: 0x00080549 File Offset: 0x0007E749
		protected override byte[] HashFinal()
		{
			this._impl.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			return this._impl.Hash;
		}

		// Token: 0x06002365 RID: 9061 RVA: 0x00080569 File Offset: 0x0007E769
		protected override void Dispose(bool disposing)
		{
			this._impl.Dispose();
			base.Dispose(disposing);
		}

		// Token: 0x04000CE5 RID: 3301
		private static readonly CngHashAlgorithmFactory<SHA256> _factory = new CngHashAlgorithmFactory<SHA256>("System.Security.Cryptography.SHA256Cng");

		// Token: 0x04000CE6 RID: 3302
		private SHA256 _impl;
	}
}
