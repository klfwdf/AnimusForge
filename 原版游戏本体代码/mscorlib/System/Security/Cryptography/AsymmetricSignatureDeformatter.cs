using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200024C RID: 588
	[ComVisible(true)]
	public abstract class AsymmetricSignatureDeformatter
	{
		// Token: 0x060020EA RID: 8426
		public abstract void SetKey(AsymmetricAlgorithm key);

		// Token: 0x060020EB RID: 8427
		public abstract void SetHashAlgorithm(string strName);

		// Token: 0x060020EC RID: 8428 RVA: 0x00072D21 File Offset: 0x00070F21
		public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature)
		{
			if (hash == null)
			{
				throw new ArgumentNullException("hash");
			}
			this.SetHashAlgorithm(hash.ToString());
			return this.VerifySignature(hash.Hash, rgbSignature);
		}

		// Token: 0x060020ED RID: 8429
		public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
	}
}
