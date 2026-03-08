using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000266 RID: 614
	[ComVisible(true)]
	public class HMACSHA256 : HMAC
	{
		// Token: 0x060021C6 RID: 8646 RVA: 0x00077B2D File Offset: 0x00075D2D
		public HMACSHA256()
			: this(Utils.GenerateRandom(64))
		{
		}

		// Token: 0x060021C7 RID: 8647 RVA: 0x00077B3C File Offset: 0x00075D3C
		public HMACSHA256(byte[] key)
		{
			this.m_hashName = "SHA256";
			this.HashSizeValue = 256;
			if (base.GetType() == typeof(HMACSHA256))
			{
				this.m_impl = new NativeHmac(CapiNative.AlgorithmID.Sha256);
			}
			else
			{
				this.m_hash1 = this.InstantiateHash();
				this.m_hash2 = this.InstantiateHash();
			}
			base.InitializeKey(key);
		}

		// Token: 0x060021C8 RID: 8648 RVA: 0x00077BB0 File Offset: 0x00075DB0
		internal sealed override HashAlgorithm InstantiateHash()
		{
			return HMAC.GetHashAlgorithmWithFipsFallback(() => new SHA256Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider"));
		}
	}
}
