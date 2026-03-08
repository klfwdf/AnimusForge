using System;

namespace System.Security.Cryptography
{
	// Token: 0x020002A0 RID: 672
	internal class DSASignatureDescription : SignatureDescription
	{
		// Token: 0x0600238D RID: 9101 RVA: 0x000808F6 File Offset: 0x0007EAF6
		public DSASignatureDescription()
		{
			base.KeyAlgorithm = "System.Security.Cryptography.DSACryptoServiceProvider";
			base.DigestAlgorithm = "System.Security.Cryptography.SHA1CryptoServiceProvider";
			base.FormatterAlgorithm = "System.Security.Cryptography.DSASignatureFormatter";
			base.DeformatterAlgorithm = "System.Security.Cryptography.DSASignatureDeformatter";
		}
	}
}
