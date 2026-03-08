using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000285 RID: 645
	[ComVisible(true)]
	public class RSAOAEPKeyExchangeFormatter : AsymmetricKeyExchangeFormatter
	{
		// Token: 0x060022F8 RID: 8952 RVA: 0x0007DC50 File Offset: 0x0007BE50
		public RSAOAEPKeyExchangeFormatter()
		{
		}

		// Token: 0x060022F9 RID: 8953 RVA: 0x0007DC58 File Offset: 0x0007BE58
		public RSAOAEPKeyExchangeFormatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x17000463 RID: 1123
		// (get) Token: 0x060022FA RID: 8954 RVA: 0x0007DC7A File Offset: 0x0007BE7A
		// (set) Token: 0x060022FB RID: 8955 RVA: 0x0007DC96 File Offset: 0x0007BE96
		public byte[] Parameter
		{
			get
			{
				if (this.ParameterValue != null)
				{
					return (byte[])this.ParameterValue.Clone();
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					this.ParameterValue = (byte[])value.Clone();
					return;
				}
				this.ParameterValue = null;
			}
		}

		// Token: 0x17000464 RID: 1124
		// (get) Token: 0x060022FC RID: 8956 RVA: 0x0007DCB4 File Offset: 0x0007BEB4
		public override string Parameters
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000465 RID: 1125
		// (get) Token: 0x060022FD RID: 8957 RVA: 0x0007DCB7 File Offset: 0x0007BEB7
		// (set) Token: 0x060022FE RID: 8958 RVA: 0x0007DCBF File Offset: 0x0007BEBF
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

		// Token: 0x060022FF RID: 8959 RVA: 0x0007DCC8 File Offset: 0x0007BEC8
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesEncrypt = null;
		}

		// Token: 0x06002300 RID: 8960 RVA: 0x0007DCF0 File Offset: 0x0007BEF0
		[SecuritySafeCritical]
		public override byte[] CreateKeyExchange(byte[] rgbData)
		{
			if (this._rsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			if (this.OverridesEncrypt)
			{
				return this._rsaKey.Encrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
			}
			return Utils.RsaOaepEncrypt(this._rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), RandomNumberGenerator.Create(), rgbData);
		}

		// Token: 0x06002301 RID: 8961 RVA: 0x0007DD4A File Offset: 0x0007BF4A
		public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType)
		{
			return this.CreateKeyExchange(rgbData);
		}

		// Token: 0x17000466 RID: 1126
		// (get) Token: 0x06002302 RID: 8962 RVA: 0x0007DD54 File Offset: 0x0007BF54
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

		// Token: 0x04000CB6 RID: 3254
		private byte[] ParameterValue;

		// Token: 0x04000CB7 RID: 3255
		private RSA _rsaKey;

		// Token: 0x04000CB8 RID: 3256
		private bool? _rsaOverridesEncrypt;

		// Token: 0x04000CB9 RID: 3257
		private RandomNumberGenerator RngValue;
	}
}
