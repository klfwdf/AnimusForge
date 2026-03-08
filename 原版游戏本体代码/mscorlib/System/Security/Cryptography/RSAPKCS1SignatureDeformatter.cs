using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
	// Token: 0x02000288 RID: 648
	[ComVisible(true)]
	public class RSAPKCS1SignatureDeformatter : AsymmetricSignatureDeformatter
	{
		// Token: 0x06002315 RID: 8981 RVA: 0x0007E0B8 File Offset: 0x0007C2B8
		public RSAPKCS1SignatureDeformatter()
		{
		}

		// Token: 0x06002316 RID: 8982 RVA: 0x0007E0C0 File Offset: 0x0007C2C0
		public RSAPKCS1SignatureDeformatter(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
		}

		// Token: 0x06002317 RID: 8983 RVA: 0x0007E0E2 File Offset: 0x0007C2E2
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._rsaKey = (RSA)key;
			this._rsaOverridesVerifyHash = null;
		}

		// Token: 0x06002318 RID: 8984 RVA: 0x0007E10A File Offset: 0x0007C30A
		public override void SetHashAlgorithm(string strName)
		{
			this._strOID = CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm);
		}

		// Token: 0x06002319 RID: 8985 RVA: 0x0007E11C File Offset: 0x0007C31C
		[SecuritySafeCritical]
		public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
		{
			if (rgbHash == null)
			{
				throw new ArgumentNullException("rgbHash");
			}
			if (rgbSignature == null)
			{
				throw new ArgumentNullException("rgbSignature");
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
				return ((RSACryptoServiceProvider)this._rsaKey).VerifyHash(rgbHash, algIdFromOid, rgbSignature);
			}
			if (this.OverridesVerifyHash)
			{
				HashAlgorithmName hashAlgorithm = Utils.OidToHashAlgorithmName(this._strOID);
				return this._rsaKey.VerifyHash(rgbHash, rgbSignature, hashAlgorithm, RSASignaturePadding.Pkcs1);
			}
			byte[] rhs = Utils.RsaPkcs1Padding(this._rsaKey, CryptoConfig.EncodeOID(this._strOID), rgbHash);
			return Utils.CompareBigIntArrays(this._rsaKey.EncryptValue(rgbSignature), rhs);
		}

		// Token: 0x1700046D RID: 1133
		// (get) Token: 0x0600231A RID: 8986 RVA: 0x0007E1F8 File Offset: 0x0007C3F8
		private bool OverridesVerifyHash
		{
			get
			{
				if (this._rsaOverridesVerifyHash == null)
				{
					this._rsaOverridesVerifyHash = new bool?(Utils.DoesRsaKeyOverride(this._rsaKey, "VerifyHash", new Type[]
					{
						typeof(byte[]),
						typeof(byte[]),
						typeof(HashAlgorithmName),
						typeof(RSASignaturePadding)
					}));
				}
				return this._rsaOverridesVerifyHash.Value;
			}
		}

		// Token: 0x04000CC0 RID: 3264
		private RSA _rsaKey;

		// Token: 0x04000CC1 RID: 3265
		private string _strOID;

		// Token: 0x04000CC2 RID: 3266
		private bool? _rsaOverridesVerifyHash;
	}
}
