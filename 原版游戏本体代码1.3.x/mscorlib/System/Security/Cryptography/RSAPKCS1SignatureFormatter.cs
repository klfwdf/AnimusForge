using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
	// Token: 0x02000289 RID: 649
	[ComVisible(true)]
	public class RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter
	{
		// Token: 0x0600231B RID: 8987 RVA: 0x0007E272 File Offset: 0x0007C472
		public RSAPKCS1SignatureFormatter()
		{
		}

		// Token: 0x0600231C RID: 8988 RVA: 0x0007E27A File Offset: 0x0007C47A
		public RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x0600231D RID: 8989 RVA: 0x0007E29C File Offset: 0x0007C49C
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesSignHash = null;
		}

		// Token: 0x0600231E RID: 8990 RVA: 0x0007E2C4 File Offset: 0x0007C4C4
		public override void SetHashAlgorithm(string strName)
		{
			this._strOID = CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm);
		}

		// Token: 0x0600231F RID: 8991 RVA: 0x0007E2D4 File Offset: 0x0007C4D4
		[SecuritySafeCritical]
		public override byte[] CreateSignature(byte[] rgbHash)
		{
			if (rgbHash == null)
			{
				throw new ArgumentNullException("rgbHash");
			}
			if (this._strOID == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
			}
			if (this._rsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			if (this._rsaKey is RSACryptoServiceProvider)
			{
				int algIdFromOid = X509Utils.GetAlgIdFromOid(this._strOID, OidGroup.HashAlgorithm);
				return ((RSACryptoServiceProvider)this._rsaKey).SignHash(rgbHash, algIdFromOid);
			}
			if (this.OverridesSignHash)
			{
				HashAlgorithmName hashAlgorithm = Utils.OidToHashAlgorithmName(this._strOID);
				return this._rsaKey.SignHash(rgbHash, hashAlgorithm, RSASignaturePadding.Pkcs1);
			}
			byte[] rgb = Utils.RsaPkcs1Padding(this._rsaKey, CryptoConfig.EncodeOID(this._strOID), rgbHash);
			return this._rsaKey.DecryptValue(rgb);
		}

		// Token: 0x1700046E RID: 1134
		// (get) Token: 0x06002320 RID: 8992 RVA: 0x0007E398 File Offset: 0x0007C598
		private bool OverridesSignHash
		{
			get
			{
				if (this._rsaOverridesSignHash == null)
				{
					this._rsaOverridesSignHash = new bool?(Utils.DoesRsaKeyOverride(this._rsaKey, "SignHash", new Type[]
					{
						typeof(byte[]),
						typeof(HashAlgorithmName),
						typeof(RSASignaturePadding)
					}));
				}
				return this._rsaOverridesSignHash.Value;
			}
		}

		// Token: 0x04000CC3 RID: 3267
		private RSA _rsaKey;

		// Token: 0x04000CC4 RID: 3268
		private string _strOID;

		// Token: 0x04000CC5 RID: 3269
		private bool? _rsaOverridesSignHash;
	}
}
