using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000298 RID: 664
	[ComVisible(true)]
	public abstract class SHA512 : HashAlgorithm
	{
		// Token: 0x06002370 RID: 9072 RVA: 0x00080655 File Offset: 0x0007E855
		protected SHA512()
		{
			this.HashSizeValue = 512;
		}

		// Token: 0x06002371 RID: 9073 RVA: 0x00080668 File Offset: 0x0007E868
		public new static SHA512 Create()
		{
			return SHA512.Create("System.Security.Cryptography.SHA512");
		}

		// Token: 0x06002372 RID: 9074 RVA: 0x00080674 File Offset: 0x0007E874
		public new static SHA512 Create(string hashName)
		{
			return (SHA512)CryptoConfig.CreateFromName(hashName);
		}
	}
}
