using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
	// Token: 0x02000261 RID: 609
	[ComVisible(true)]
	public class DSASignatureFormatter : AsymmetricSignatureFormatter
	{
		// Token: 0x060021A6 RID: 8614 RVA: 0x000773FE File Offset: 0x000755FE
		public DSASignatureFormatter()
		{
			this._oid = CryptoConfig.MapNameToOID("SHA1", OidGroup.HashAlgorithm);
		}

		// Token: 0x060021A7 RID: 8615 RVA: 0x00077417 File Offset: 0x00075617
		public DSASignatureFormatter(AsymmetricAlgorithm key)
			: this()
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._dsaKey = (DSA)key;
		}

		// Token: 0x060021A8 RID: 8616 RVA: 0x00077439 File Offset: 0x00075639
		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this._dsaKey = (DSA)key;
		}

		// Token: 0x060021A9 RID: 8617 RVA: 0x00077455 File Offset: 0x00075655
		public override void SetHashAlgorithm(string strName)
		{
			if (CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm) != this._oid)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation"));
			}
		}

		// Token: 0x060021AA RID: 8618 RVA: 0x0007747C File Offset: 0x0007567C
		public override byte[] CreateSignature(byte[] rgbHash)
		{
			if (rgbHash == null)
			{
				throw new ArgumentNullException("rgbHash");
			}
			if (this._oid == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
			}
			if (this._dsaKey == null)
			{
				throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
			}
			return this._dsaKey.CreateSignature(rgbHash);
		}

		// Token: 0x04000C49 RID: 3145
		private DSA _dsaKey;

		// Token: 0x04000C4A RID: 3146
		private string _oid;
	}
}
