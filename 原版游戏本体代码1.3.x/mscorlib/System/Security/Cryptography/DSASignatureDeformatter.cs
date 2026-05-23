using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
	// Token: 0x02000260 RID: 608
	[ComVisible(true)]
	public class DSASignatureDeformatter : AsymmetricSignatureDeformatter
	{
		// Token: 0x060021A1 RID: 8609 RVA: 0x00077331 File Offset: 0x00075531
		public DSASignatureDeformatter()
		{
			this._oid = CryptoConfig.MapNameToOID("SHA1", OidGroup.HashAlgorithm);
		}

		// Token: 0x060021A2 RID: 8610 RVA: 0x0007734A File Offset: 0x0007554A
		public DSASignatureDeformatter(AsymmetricAlgorithm key)
			: this()
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._dsaKey = (DSA)key;
		}

		// Token: 0x060021A3 RID: 8611 RVA: 0x0007736C File Offset: 0x0007556C
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._dsaKey = (DSA)key;
		}

		// Token: 0x060021A4 RID: 8612 RVA: 0x00077388 File Offset: 0x00075588
		public override void SetHashAlgorithm(string strName)
		{
			if (CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm) != this._oid)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation"));
			}
		}

		// Token: 0x060021A5 RID: 8613 RVA: 0x000773B0 File Offset: 0x000755B0
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
			if (this._dsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			return this._dsaKey.VerifySignature(rgbHash, rgbSignature);
		}

		// Token: 0x04000C47 RID: 3143
		private DSA _dsaKey;

		// Token: 0x04000C48 RID: 3144
		private string _oid;
	}
}
