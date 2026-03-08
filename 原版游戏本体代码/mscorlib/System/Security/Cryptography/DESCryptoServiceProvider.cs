using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200025A RID: 602
	[ComVisible(true)]
	public sealed class DESCryptoServiceProvider : DES
	{
		// Token: 0x0600215F RID: 8543 RVA: 0x0007626C File Offset: 0x0007446C
		[SecuritySafeCritical]
		public DESCryptoServiceProvider()
		{
			if (!Utils.HasAlgorithm(26113, 0))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_AlgorithmNotAvailable"));
			}
			this.FeedbackSizeValue = 8;
		}

		// Token: 0x06002160 RID: 8544 RVA: 0x00076298 File Offset: 0x00074498
		[SecuritySafeCritical]
		public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
		{
			if (DES.IsWeakKey(rgbKey))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_Weak"), "DES");
			}
			if (DES.IsSemiWeakKey(rgbKey))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_SemiWeak"), "DES");
			}
			return this._NewEncryptor(rgbKey, this.ModeValue, rgbIV, this.FeedbackSizeValue, CryptoAPITransformMode.Encrypt);
		}

		// Token: 0x06002161 RID: 8545 RVA: 0x000762F4 File Offset: 0x000744F4
		[SecuritySafeCritical]
		public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
		{
			if (DES.IsWeakKey(rgbKey))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_Weak"), "DES");
			}
			if (DES.IsSemiWeakKey(rgbKey))
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_SemiWeak"), "DES");
			}
			return this._NewEncryptor(rgbKey, this.ModeValue, rgbIV, this.FeedbackSizeValue, CryptoAPITransformMode.Decrypt);
		}

		// Token: 0x06002162 RID: 8546 RVA: 0x00076350 File Offset: 0x00074550
		public override void GenerateKey()
		{
			this.KeyValue = new byte[8];
			Utils.StaticRandomNumberGenerator.GetBytes(this.KeyValue);
			while (DES.IsWeakKey(this.KeyValue) || DES.IsSemiWeakKey(this.KeyValue))
			{
				Utils.StaticRandomNumberGenerator.GetBytes(this.KeyValue);
			}
		}

		// Token: 0x06002163 RID: 8547 RVA: 0x000763A5 File Offset: 0x000745A5
		public override void GenerateIV()
		{
			this.IVValue = new byte[8];
			Utils.StaticRandomNumberGenerator.GetBytes(this.IVValue);
		}

		// Token: 0x06002164 RID: 8548 RVA: 0x000763C4 File Offset: 0x000745C4
		[SecurityCritical]
		private ICryptoTransform _NewEncryptor(byte[] rgbKey, CipherMode mode, byte[] rgbIV, int feedbackSize, CryptoAPITransformMode encryptMode)
		{
			int num = 0;
			int[] array = new int[10];
			object[] array2 = new object[10];
			if (mode == CipherMode.OFB)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_OFBNotSupported"));
			}
			if (mode == CipherMode.CFB && feedbackSize != 8)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_CFBSizeNotSupported"));
			}
			if (rgbKey == null)
			{
				rgbKey = new byte[8];
				Utils.StaticRandomNumberGenerator.GetBytes(rgbKey);
			}
			if (mode != CipherMode.CBC)
			{
				array[num] = 4;
				array2[num] = mode;
				num++;
			}
			if (mode != CipherMode.ECB)
			{
				if (rgbIV == null)
				{
					rgbIV = new byte[8];
					Utils.StaticRandomNumberGenerator.GetBytes(rgbIV);
				}
				if (rgbIV.Length < 8)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidIVSize"));
				}
				array[num] = 1;
				array2[num] = rgbIV;
				num++;
			}
			if (mode == CipherMode.OFB || mode == CipherMode.CFB)
			{
				array[num] = 5;
				array2[num] = feedbackSize;
				num++;
			}
			return new CryptoAPITransform(26113, num, array, array2, rgbKey, this.PaddingValue, mode, this.BlockSizeValue, feedbackSize, false, encryptMode);
		}
	}
}
