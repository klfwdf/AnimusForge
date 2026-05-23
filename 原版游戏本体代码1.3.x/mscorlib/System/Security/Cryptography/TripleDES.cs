using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x020002A2 RID: 674
	[ComVisible(true)]
	public abstract class TripleDES : SymmetricAlgorithm
	{
		// Token: 0x060023AB RID: 9131 RVA: 0x00080CBB File Offset: 0x0007EEBB
		protected TripleDES()
		{
			this.KeySizeValue = 192;
			this.BlockSizeValue = 64;
			this.FeedbackSizeValue = this.BlockSizeValue;
			this.LegalBlockSizesValue = TripleDES.s_legalBlockSizes;
			this.LegalKeySizesValue = TripleDES.s_legalKeySizes;
		}

		// Token: 0x17000484 RID: 1156
		// (get) Token: 0x060023AC RID: 9132 RVA: 0x00080CF8 File Offset: 0x0007EEF8
		// (set) Token: 0x060023AD RID: 9133 RVA: 0x00080D28 File Offset: 0x0007EF28
		public override byte[] Key
		{
			get
			{
				if (this.KeyValue == null)
				{
					do
					{
						this.GenerateKey();
					}
					while (TripleDES.IsWeakKey(this.KeyValue));
				}
				return (byte[])this.KeyValue.Clone();
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (!base.ValidKeySize(value.Length * 8))
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
				}
				if (TripleDES.IsWeakKey(value))
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_Weak"), "TripleDES");
				}
				this.KeyValue = (byte[])value.Clone();
				this.KeySizeValue = value.Length * 8;
			}
		}

		// Token: 0x060023AE RID: 9134 RVA: 0x00080D99 File Offset: 0x0007EF99
		public new static TripleDES Create()
		{
			return TripleDES.Create("System.Security.Cryptography.TripleDES");
		}

		// Token: 0x060023AF RID: 9135 RVA: 0x00080DA5 File Offset: 0x0007EFA5
		public new static TripleDES Create(string str)
		{
			return (TripleDES)CryptoConfig.CreateFromName(str);
		}

		// Token: 0x060023B0 RID: 9136 RVA: 0x00080DB4 File Offset: 0x0007EFB4
		public static bool IsWeakKey(byte[] rgbKey)
		{
			if (!TripleDES.IsLegalKeySize(rgbKey))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
			}
			byte[] array = Utils.FixupKeyParity(rgbKey);
			return TripleDES.EqualBytes(array, 0, 8, 8) || (array.Length == 24 && TripleDES.EqualBytes(array, 8, 16, 8));
		}

		// Token: 0x060023B1 RID: 9137 RVA: 0x00080E04 File Offset: 0x0007F004
		private static bool EqualBytes(byte[] rgbKey, int start1, int start2, int count)
		{
			if (start1 < 0)
			{
				throw new ArgumentOutOfRangeException("start1", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (start2 < 0)
			{
				throw new ArgumentOutOfRangeException("start2", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (start1 + count > rgbKey.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
			}
			if (start2 + count > rgbKey.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
			}
			for (int i = 0; i < count; i++)
			{
				if (rgbKey[start1 + i] != rgbKey[start2 + i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060023B2 RID: 9138 RVA: 0x00080E8E File Offset: 0x0007F08E
		private static bool IsLegalKeySize(byte[] rgbKey)
		{
			return rgbKey != null && (rgbKey.Length == 16 || rgbKey.Length == 24);
		}

		// Token: 0x04000CF9 RID: 3321
		private static KeySizes[] s_legalBlockSizes = new KeySizes[]
		{
			new KeySizes(64, 64, 0)
		};

		// Token: 0x04000CFA RID: 3322
		private static KeySizes[] s_legalKeySizes = new KeySizes[]
		{
			new KeySizes(128, 192, 64)
		};
	}
}
