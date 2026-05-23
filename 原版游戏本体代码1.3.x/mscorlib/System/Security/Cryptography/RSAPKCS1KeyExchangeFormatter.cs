using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000287 RID: 647
	[ComVisible(true)]
	public class RSAPKCS1KeyExchangeFormatter : AsymmetricKeyExchangeFormatter
	{
		// Token: 0x0600230C RID: 8972 RVA: 0x0007DF10 File Offset: 0x0007C110
		public RSAPKCS1KeyExchangeFormatter()
		{
		}

		// Token: 0x0600230D RID: 8973 RVA: 0x0007DF18 File Offset: 0x0007C118
		public RSAPKCS1KeyExchangeFormatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x1700046A RID: 1130
		// (get) Token: 0x0600230E RID: 8974 RVA: 0x0007DF3A File Offset: 0x0007C13A
		public override string Parameters
		{
			get
			{
				return "<enc:KeyEncryptionMethod enc:Algorithm=\"http://www.microsoft.com/xml/security/algorithm/PKCS1-v1.5-KeyEx\" xmlns:enc=\"http://www.microsoft.com/xml/security/encryption/v1.0\" />";
			}
		}

		// Token: 0x1700046B RID: 1131
		// (get) Token: 0x0600230F RID: 8975 RVA: 0x0007DF41 File Offset: 0x0007C141
		// (set) Token: 0x06002310 RID: 8976 RVA: 0x0007DF49 File Offset: 0x0007C149
		public RandomNumberGenerator Rng
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

		// Token: 0x06002311 RID: 8977 RVA: 0x0007DF52 File Offset: 0x0007C152
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesEncrypt = null;
		}

		// Token: 0x06002312 RID: 8978 RVA: 0x0007DF7C File Offset: 0x0007C17C
		public override byte[] CreateKeyExchange(byte[] rgbData)
		{
			if (this._rsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			byte[] result;
			if (this.OverridesEncrypt)
			{
				result = this._rsaKey.Encrypt(rgbData, RSAEncryptionPadding.Pkcs1);
			}
			else
			{
				int num = this._rsaKey.KeySize / 8;
				if (rgbData.Length + 11 > num)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_EncDataTooBig", new object[] { num - 11 }));
				}
				byte[] array = new byte[num];
				if (this.RngValue == null)
				{
					this.RngValue = RandomNumberGenerator.Create();
				}
				this.Rng.GetNonZeroBytes(array);
				array[0] = 0;
				array[1] = 2;
				array[num - rgbData.Length - 1] = 0;
				Buffer.InternalBlockCopy(rgbData, 0, array, num - rgbData.Length, rgbData.Length);
				result = this._rsaKey.EncryptValue(array);
			}
			return result;
		}

		// Token: 0x06002313 RID: 8979 RVA: 0x0007E04F File Offset: 0x0007C24F
		public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType)
		{
			return this.CreateKeyExchange(rgbData);
		}

		// Token: 0x1700046C RID: 1132
		// (get) Token: 0x06002314 RID: 8980 RVA: 0x0007E058 File Offset: 0x0007C258
		private bool OverridesEncrypt
		{
			get
			{
				if (this._rsaOverridesEncrypt == null)
				{
					this._rsaOverridesEncrypt = new bool?(Utils.DoesRsaKeyOverride(this._rsaKey, "Encrypt", new Type[]
					{
						typeof(byte[]),
						typeof(RSAEncryptionPadding)
					}));
				}
				return this._rsaOverridesEncrypt.Value;
			}
		}

		// Token: 0x04000CBD RID: 3261
		private RandomNumberGenerator RngValue;

		// Token: 0x04000CBE RID: 3262
		private RSA _rsaKey;

		// Token: 0x04000CBF RID: 3263
		private bool? _rsaOverridesEncrypt;
	}
}
