using System;

namespace System.Security.Cryptography
{
	// Token: 0x0200029B RID: 667
	internal abstract class RSAPKCS1SignatureDescription : SignatureDescription
	{
		// Token: 0x06002386 RID: 9094 RVA: 0x00080830 File Offset: 0x0007EA30
		protected RSAPKCS1SignatureDescription(string hashAlgorithm, string digestAlgorithm)
		{
			base.KeyAlgorithm = "System.Security.Cryptography.RSA";
			base.DigestAlgorithm = digestAlgorithm;
			base.FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
			base.DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
			this._hashAlgorithm = hashAlgorithm;
		}

		// Token: 0x06002387 RID: 9095 RVA: 0x00080868 File Offset: 0x0007EA68
		public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
		{
			AsymmetricSignatureDeformatter asymmetricSignatureDeformatter = base.CreateDeformatter(key);
			asymmetricSignatureDeformatter.SetHashAlgorithm(this._hashAlgorithm);
			return asymmetricSignatureDeformatter;
		}

		// Token: 0x06002388 RID: 9096 RVA: 0x0008088C File Offset: 0x0007EA8C
		public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
		{
			AsymmetricSignatureFormatter asymmetricSignatureFormatter = base.CreateFormatter(key);
			asymmetricSignatureFormatter.SetHashAlgorithm(this._hashAlgorithm);
			return asymmetricSignatureFormatter;
		}

		// Token: 0x04000CEF RID: 3311
		private string _hashAlgorithm;
	}
}
