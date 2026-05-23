using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000265 RID: 613
	[ComVisible(true)]
	public class HMACSHA1 : HMAC
	{
		// Token: 0x060021C2 RID: 8642 RVA: 0x00077A80 File Offset: 0x00075C80
		public HMACSHA1()
			: this(Utils.GenerateRandom(64))
		{
		}

		// Token: 0x060021C3 RID: 8643 RVA: 0x00077A8F File Offset: 0x00075C8F
		public HMACSHA1(byte[] key)
			: this(key, false)
		{
		}

		// Token: 0x060021C4 RID: 8644 RVA: 0x00077A9C File Offset: 0x00075C9C
		public HMACSHA1(byte[] key, bool useManagedSha1)
		{
			this.m_hashName = "SHA1";
			this.HashSizeValue = 160;
			if (base.GetType() == typeof(HMACSHA1))
			{
				this.m_impl = new NativeHmac(CapiNative.AlgorithmID.Sha1);
			}
			else if (useManagedSha1)
			{
				this.m_hash1 = new SHA1Managed();
				this.m_hash2 = new SHA1Managed();
			}
			else
			{
				this.m_hash1 = new SHA1CryptoServiceProvider();
				this.m_hash2 = new SHA1CryptoServiceProvider();
			}
			base.InitializeKey(key);
		}

		// Token: 0x060021C5 RID: 8645 RVA: 0x00077B26 File Offset: 0x00075D26
		internal sealed override HashAlgorithm InstantiateHash()
		{
			return new SHA1CryptoServiceProvider();
		}
	}
}
