using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000249 RID: 585
	[ComVisible(true)]
	public abstract class AsymmetricAlgorithm : IDisposable
	{
		// Token: 0x060020D3 RID: 8403 RVA: 0x00072C0F File Offset: 0x00070E0F
		public void Dispose()
		{
			this.Clear();
		}

		// Token: 0x060020D4 RID: 8404 RVA: 0x00072C17 File Offset: 0x00070E17
		public void Clear()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060020D5 RID: 8405 RVA: 0x00072C26 File Offset: 0x00070E26
		protected virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x170003EF RID: 1007
		// (get) Token: 0x060020D6 RID: 8406 RVA: 0x00072C28 File Offset: 0x00070E28
		// (set) Token: 0x060020D7 RID: 8407 RVA: 0x00072C30 File Offset: 0x00070E30
		public virtual int KeySize
		{
			get
			{
				return this.KeySizeValue;
			}
			set
			{
				for (int i = 0; i < this.LegalKeySizesValue.Length; i++)
				{
					if (this.LegalKeySizesValue[i].SkipSize == 0)
					{
						if (this.LegalKeySizesValue[i].MinSize == value)
						{
							this.KeySizeValue = value;
							return;
						}
					}
					else
					{
						for (int j = this.LegalKeySizesValue[i].MinSize; j <= this.LegalKeySizesValue[i].MaxSize; j += this.LegalKeySizesValue[i].SkipSize)
						{
							if (j == value)
							{
								this.KeySizeValue = value;
								return;
							}
						}
					}
				}
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
			}
		}

		// Token: 0x170003F0 RID: 1008
		// (get) Token: 0x060020D8 RID: 8408 RVA: 0x00072CC2 File Offset: 0x00070EC2
		public virtual KeySizes[] LegalKeySizes
		{
			get
			{
				return (KeySizes[])this.LegalKeySizesValue.Clone();
			}
		}

		// Token: 0x170003F1 RID: 1009
		// (get) Token: 0x060020D9 RID: 8409 RVA: 0x00072CD4 File Offset: 0x00070ED4
		public virtual string SignatureAlgorithm
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170003F2 RID: 1010
		// (get) Token: 0x060020DA RID: 8410 RVA: 0x00072CDB File Offset: 0x00070EDB
		public virtual string KeyExchangeAlgorithm
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x060020DB RID: 8411 RVA: 0x00072CE2 File Offset: 0x00070EE2
		public static AsymmetricAlgorithm Create()
		{
			return AsymmetricAlgorithm.Create("System.Security.Cryptography.AsymmetricAlgorithm");
		}

		// Token: 0x060020DC RID: 8412 RVA: 0x00072CEE File Offset: 0x00070EEE
		public static AsymmetricAlgorithm Create(string algName)
		{
			return (AsymmetricAlgorithm)CryptoConfig.CreateFromName(algName);
		}

		// Token: 0x060020DD RID: 8413 RVA: 0x00072CFB File Offset: 0x00070EFB
		public virtual void FromXmlString(string xmlString)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060020DE RID: 8414 RVA: 0x00072D02 File Offset: 0x00070F02
		public virtual string ToXmlString(bool includePrivateParameters)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04000BE9 RID: 3049
		protected int KeySizeValue;

		// Token: 0x04000BEA RID: 3050
		protected KeySizes[] LegalKeySizesValue;
	}
}
