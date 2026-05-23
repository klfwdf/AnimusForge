using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000284 RID: 644
	[ComVisible(true)]
	public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
	{
		// Token: 0x060022F1 RID: 8945 RVA: 0x0007DB41 File Offset: 0x0007BD41
		public RSAOAEPKeyExchangeDeformatter()
		{
		}

		// Token: 0x060022F2 RID: 8946 RVA: 0x0007DB49 File Offset: 0x0007BD49
		public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x17000461 RID: 1121
		// (get) Token: 0x060022F3 RID: 8947 RVA: 0x0007DB6B File Offset: 0x0007BD6B
		// (set) Token: 0x060022F4 RID: 8948 RVA: 0x0007DB6E File Offset: 0x0007BD6E
		public override string Parameters
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x060022F5 RID: 8949 RVA: 0x0007DB70 File Offset: 0x0007BD70
		[SecuritySafeCritical]
		public override byte[] DecryptKeyExchange(byte[] rgbData)
		{
			if (this._rsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			if (this.OverridesDecrypt)
			{
				return this._rsaKey.Decrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
			}
			return Utils.RsaOaepDecrypt(this._rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), rgbData);
		}

		// Token: 0x060022F6 RID: 8950 RVA: 0x0007DBC5 File Offset: 0x0007BDC5
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesDecrypt = null;
		}

		// Token: 0x17000462 RID: 1122
		// (get) Token: 0x060022F7 RID: 8951 RVA: 0x0007DBF0 File Offset: 0x0007BDF0
		private bool OverridesDecrypt
		{
			get
			{
				if (this._rsaOverridesDecrypt == null)
				{
					this._rsaOverridesDecrypt = new bool?(Utils.DoesRsaKeyOverride(this._rsaKey, "Decrypt", new Type[]
					{
						typeof(byte[]),
						typeof(RSAEncryptionPadding)
					}));
				}
				return this._rsaOverridesDecrypt.Value;
			}
		}

		// Token: 0x04000CB4 RID: 3252
		private RSA _rsaKey;

		// Token: 0x04000CB5 RID: 3253
		private bool? _rsaOverridesDecrypt;
	}
}
