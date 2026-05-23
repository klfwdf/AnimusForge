using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000286 RID: 646
	[ComVisible(true)]
	public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
	{
		// Token: 0x06002303 RID: 8963 RVA: 0x0007DDB4 File Offset: 0x0007BFB4
		public RSAPKCS1KeyExchangeDeformatter()
		{
		}

		// Token: 0x06002304 RID: 8964 RVA: 0x0007DDBC File Offset: 0x0007BFBC
		public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x17000467 RID: 1127
		// (get) Token: 0x06002305 RID: 8965 RVA: 0x0007DDDE File Offset: 0x0007BFDE
		// (set) Token: 0x06002306 RID: 8966 RVA: 0x0007DDE6 File Offset: 0x0007BFE6
		public RandomNumberGenerator RNG
		{
			get
			{
				return this.RngValue;
			}
			set
			{
				this.RngValue = value;
			}
		}

		// Token: 0x17000468 RID: 1128
		// (get) Token: 0x06002307 RID: 8967 RVA: 0x0007DDEF File Offset: 0x0007BFEF
		// (set) Token: 0x06002308 RID: 8968 RVA: 0x0007DDF2 File Offset: 0x0007BFF2
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

		// Token: 0x06002309 RID: 8969 RVA: 0x0007DDF4 File Offset: 0x0007BFF4
		public override byte[] DecryptKeyExchange(byte[] rgbIn)
		{
			if (this._rsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			byte[] array;
			if (this.OverridesDecrypt)
			{
				array = this._rsaKey.Decrypt(rgbIn, RSAEncryptionPadding.Pkcs1);
			}
			else
			{
				byte[] array2 = this._rsaKey.DecryptValue(rgbIn);
				int num = 2;
				while (num < array2.Length && array2[num] != 0)
				{
					num++;
				}
				if (num >= array2.Length)
				{
					throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_PKCS1Decoding"));
				}
				num++;
				array = new byte[array2.Length - num];
				Buffer.InternalBlockCopy(array2, num, array, 0, array.Length);
			}
			return array;
		}

		// Token: 0x0600230A RID: 8970 RVA: 0x0007DE87 File Offset: 0x0007C087
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesDecrypt = null;
		}

		// Token: 0x17000469 RID: 1129
		// (get) Token: 0x0600230B RID: 8971 RVA: 0x0007DEB0 File Offset: 0x0007C0B0
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

		// Token: 0x04000CBA RID: 3258
		private RSA _rsaKey;

		// Token: 0x04000CBB RID: 3259
		private bool? _rsaOverridesDecrypt;

		// Token: 0x04000CBC RID: 3260
		private RandomNumberGenerator RngValue;
	}
}
