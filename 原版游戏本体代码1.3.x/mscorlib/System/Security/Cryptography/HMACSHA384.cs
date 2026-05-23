using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000267 RID: 615
	[ComVisible(true)]
	public class HMACSHA384 : HMAC
	{
		// Token: 0x060021C9 RID: 8649 RVA: 0x00077C00 File Offset: 0x00075E00
		public HMACSHA384()
			: this(Utils.GenerateRandom(128))
		{
		}

		// Token: 0x060021CA RID: 8650 RVA: 0x00077C14 File Offset: 0x00075E14
		[SecuritySafeCritical]
		public HMACSHA384(byte[] key)
		{
			this.m_hashName = "SHA384";
			this.HashSizeValue = 384;
			base.BlockSizeValue = this.BlockSize;
			if (base.GetType() == typeof(HMACSHA384) && !this.m_useLegacyBlockSize)
			{
				this.m_impl = new NativeHmac(CapiNative.AlgorithmID.Sha384);
			}
			else
			{
				this.m_hash1 = this.InstantiateHash();
				this.m_hash2 = this.InstantiateHash();
			}
			base.InitializeKey(key);
		}

		// Token: 0x1700041C RID: 1052
		// (get) Token: 0x060021CB RID: 8651 RVA: 0x00077CA4 File Offset: 0x00075EA4
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

		// Token: 0x1700041D RID: 1053
		// (get) Token: 0x060021CC RID: 8652 RVA: 0x00077CB6 File Offset: 0x00075EB6
		// (set) Token: 0x060021CD RID: 8653 RVA: 0x00077CC0 File Offset: 0x00075EC0
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

		// Token: 0x060021CE RID: 8654 RVA: 0x00077D44 File Offset: 0x00075F44
		internal sealed override HashAlgorithm InstantiateHash()
		{
			return HMAC.GetHashAlgorithmWithFipsFallback(() => new SHA384Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA384CryptoServiceProvider"));
		}

		// Token: 0x04000C53 RID: 3155
		private bool m_useLegacyBlockSize = Utils._ProduceLegacyHmacValues();
	}
}
