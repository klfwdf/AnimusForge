using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000268 RID: 616
	[ComVisible(true)]
	public class HMACSHA512 : HMAC
	{
		// Token: 0x060021CF RID: 8655 RVA: 0x00077D94 File Offset: 0x00075F94
		public HMACSHA512()
			: this(Utils.GenerateRandom(128))
		{
		}

		// Token: 0x060021D0 RID: 8656 RVA: 0x00077DA8 File Offset: 0x00075FA8
		[SecuritySafeCritical]
		public HMACSHA512(byte[] key)
		{
			this.m_hashName = "SHA512";
			this.HashSizeValue = 512;
			base.BlockSizeValue = this.BlockSize;
			if (base.GetType() == typeof(HMACSHA512) && !this.m_useLegacyBlockSize)
			{
				this.m_impl = new NativeHmac(CapiNative.AlgorithmID.Sha512);
			}
			else
			{
				this.m_hash1 = this.InstantiateHash();
				this.m_hash2 = this.InstantiateHash();
			}
			base.InitializeKey(key);
		}

		// Token: 0x1700041E RID: 1054
		// (get) Token: 0x060021D1 RID: 8657 RVA: 0x00077E38 File Offset: 0x00076038
		private int BlockSize
		{
			get
			{
				if (!this.m_useLegacyBlockSize)
				{
					return 128;
				}
				return 64;
			}
		}

		// Token: 0x1700041F RID: 1055
		// (get) Token: 0x060021D2 RID: 8658 RVA: 0x00077E4A File Offset: 0x0007604A
		// (set) Token: 0x060021D3 RID: 8659 RVA: 0x00077E54 File Offset: 0x00076054
		public bool ProduceLegacyHmacValues
		{
			get
			{
				return this.m_useLegacyBlockSize;
			}
			set
			{
				this.m_useLegacyBlockSize = value;
				if (this.m_impl != null && value)
				{
					if (this.m_hashing)
					{
						throw new CryptographicException(Environment.GetResourceString("Cryptography_HashNameSet"));
					}
					this.m_impl.Dispose();
					this.m_impl = null;
					this.m_hash1 = this.InstantiateHash();
					this.m_hash2 = this.InstantiateHash();
				}
				base.BlockSizeValue = this.BlockSize;
				if (this.m_impl == null)
				{
					base.InitializeKey(this.KeyValue);
				}
			}
		}

		// Token: 0x060021D4 RID: 8660 RVA: 0x00077ED8 File Offset: 0x000760D8
		internal sealed override HashAlgorithm InstantiateHash()
		{
			return HMAC.GetHashAlgorithmWithFipsFallback(() => new SHA512Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA512CryptoServiceProvider"));
		}

		// Token: 0x04000C54 RID: 3156
		private bool m_useLegacyBlockSize = Utils._ProduceLegacyHmacValues();
	}
}
