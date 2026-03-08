using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200028A RID: 650
	[ComVisible(true)]
	public abstract class Rijndael : SymmetricAlgorithm
	{
		// Token: 0x06002321 RID: 8993 RVA: 0x0007E405 File Offset: 0x0007C605
		protected Rijndael()
		{
			this.KeySizeValue = 256;
			this.BlockSizeValue = 128;
			this.FeedbackSizeValue = this.BlockSizeValue;
			this.LegalBlockSizesValue = Rijndael.s_legalBlockSizes;
			this.LegalKeySizesValue = Rijndael.s_legalKeySizes;
		}

		// Token: 0x06002322 RID: 8994 RVA: 0x0007E445 File Offset: 0x0007C645
		public new static Rijndael Create()
		{
			return Rijndael.Create("System.Security.Cryptography.Rijndael");
		}

		// Token: 0x06002323 RID: 8995 RVA: 0x0007E451 File Offset: 0x0007C651
		public new static Rijndael Create(string algName)
		{
			return (Rijndael)CryptoConfig.CreateFromName(algName);
		}

		// Token: 0x04000CC6 RID: 3270
		private static KeySizes[] s_legalBlockSizes = new KeySizes[]
		{
			new KeySizes(128, 256, 64)
		};

		// Token: 0x04000CC7 RID: 3271
		private static KeySizes[] s_legalKeySizes = new KeySizes[]
		{
			new KeySizes(128, 256, 64)
		};
	}
}
