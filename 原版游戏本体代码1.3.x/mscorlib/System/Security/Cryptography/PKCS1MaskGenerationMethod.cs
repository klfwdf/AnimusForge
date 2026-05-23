using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000276 RID: 630
	[ComVisible(true)]
	public class PKCS1MaskGenerationMethod : MaskGenerationMethod
	{
		// Token: 0x06002250 RID: 8784 RVA: 0x0007976F File Offset: 0x0007796F
		public PKCS1MaskGenerationMethod()
		{
			this.HashNameValue = "SHA1";
		}

		// Token: 0x17000446 RID: 1094
		// (get) Token: 0x06002251 RID: 8785 RVA: 0x00079782 File Offset: 0x00077982
		// (set) Token: 0x06002252 RID: 8786 RVA: 0x0007978A File Offset: 0x0007798A
		public string HashName
		{
			get
			{
				return this.HashNameValue;
			}
			set
			{
				this.HashNameValue = value;
				if (this.HashNameValue == null)
				{
					this.HashNameValue = "SHA1";
				}
			}
		}

		// Token: 0x06002253 RID: 8787 RVA: 0x000797A8 File Offset: 0x000779A8
		public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn)
		{
			HashAlgorithm hashAlgorithm = (HashAlgorithm)CryptoConfig.CreateFromName(this.HashNameValue);
			byte[] inputBuffer = new byte[4];
			byte[] array = new byte[cbReturn];
			uint num = 0U;
			for (int i = 0; i < array.Length; i += hashAlgorithm.Hash.Length)
			{
				Utils.ConvertIntToByteArray(num++, ref inputBuffer);
				hashAlgorithm.TransformBlock(rgbSeed, 0, rgbSeed.Length, rgbSeed, 0);
				hashAlgorithm.TransformFinalBlock(inputBuffer, 0, 4);
				byte[] hash = hashAlgorithm.Hash;
				hashAlgorithm.Initialize();
				if (array.Length - i > hash.Length)
				{
					Buffer.BlockCopy(hash, 0, array, i, hash.Length);
				}
				else
				{
					Buffer.BlockCopy(hash, 0, array, i, array.Length - i);
				}
			}
			return array;
		}

		// Token: 0x04000C79 RID: 3193
		private string HashNameValue;
	}
}
