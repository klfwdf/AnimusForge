using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000263 RID: 611
	[ComVisible(true)]
	public class HMACMD5 : HMAC
	{
		// Token: 0x060021BD RID: 8637 RVA: 0x000779B0 File Offset: 0x00075BB0
		public HMACMD5()
			: this(Utils.GenerateRandom(64))
		{
		}

		// Token: 0x060021BE RID: 8638 RVA: 0x000779C0 File Offset: 0x00075BC0
		public HMACMD5(byte[] key)
		{
			this.m_hashName = "MD5";
			this.HashSizeValue = 128;
			if (base.GetType() == typeof(HMACMD5))
			{
				this.m_impl = new NativeHmac(CapiNative.AlgorithmID.Md5);
			}
			else
			{
				this.m_hash1 = new MD5CryptoServiceProvider();
				this.m_hash2 = new MD5CryptoServiceProvider();
			}
			base.InitializeKey(key);
		}

		// Token: 0x060021BF RID: 8639 RVA: 0x00077A2F File Offset: 0x00075C2F
		internal sealed override HashAlgorithm InstantiateHash()
		{
			return new MD5CryptoServiceProvider();
		}
	}
}
