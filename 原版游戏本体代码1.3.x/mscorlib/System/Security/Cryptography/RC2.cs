using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000277 RID: 631
	[ComVisible(true)]
	public abstract class RC2 : SymmetricAlgorithm
	{
		// Token: 0x06002254 RID: 8788 RVA: 0x00079850 File Offset: 0x00077A50
		protected RC2()
		{
			this.KeySizeValue = 128;
			this.BlockSizeValue = 64;
			this.FeedbackSizeValue = this.BlockSizeValue;
			this.LegalBlockSizesValue = RC2.s_legalBlockSizes;
			this.LegalKeySizesValue = RC2.s_legalKeySizes;
		}

		// Token: 0x17000447 RID: 1095
		// (get) Token: 0x06002255 RID: 8789 RVA: 0x0007988D File Offset: 0x00077A8D
		// (set) Token: 0x06002256 RID: 8790 RVA: 0x000798A4 File Offset: 0x00077AA4
		public virtual int EffectiveKeySize
		{
			get
			{
				if (this.EffectiveKeySizeValue == 0)
				{
					return this.KeySizeValue;
				}
				return this.EffectiveKeySizeValue;
			}
			set
			{
				if (value > this.KeySizeValue)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_RC2_EKSKS"));
				}
				if (value == 0)
				{
					this.EffectiveKeySizeValue = value;
					return;
				}
				if (value < 40)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_RC2_EKS40"));
				}
				if (base.ValidKeySize(value))
				{
					this.EffectiveKeySizeValue = value;
					return;
				}
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
			}
		}

		// Token: 0x17000448 RID: 1096
		// (get) Token: 0x06002257 RID: 8791 RVA: 0x0007990A File Offset: 0x00077B0A
		// (set) Token: 0x06002258 RID: 8792 RVA: 0x00079912 File Offset: 0x00077B12
		public override int KeySize
		{
			get
			{
				return this.KeySizeValue;
			}
			set
			{
				if (value < this.EffectiveKeySizeValue)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_RC2_EKSKS"));
				}
				base.KeySize = value;
			}
		}

		// Token: 0x06002259 RID: 8793 RVA: 0x00079934 File Offset: 0x00077B34
		public new static RC2 Create()
		{
			return RC2.Create("System.Security.Cryptography.RC2");
		}

		// Token: 0x0600225A RID: 8794 RVA: 0x00079940 File Offset: 0x00077B40
		public new static RC2 Create(string AlgName)
		{
			return (RC2)CryptoConfig.CreateFromName(AlgName);
		}

		// Token: 0x04000C7A RID: 3194
		protected int EffectiveKeySizeValue;

		// Token: 0x04000C7B RID: 3195
		private static KeySizes[] s_legalBlockSizes = new KeySizes[]
		{
			new KeySizes(64, 64, 0)
		};

		// Token: 0x04000C7C RID: 3196
		private static KeySizes[] s_legalKeySizes = new KeySizes[]
		{
			new KeySizes(40, 1024, 8)
		};
	}
}
