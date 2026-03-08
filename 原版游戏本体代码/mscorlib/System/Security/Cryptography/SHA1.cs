using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000291 RID: 657
	[ComVisible(true)]
	public abstract class SHA1 : HashAlgorithm
	{
		// Token: 0x06002351 RID: 9041 RVA: 0x00080351 File Offset: 0x0007E551
		protected SHA1()
		{
			this.HashSizeValue = 160;
		}

		// Token: 0x06002352 RID: 9042 RVA: 0x00080364 File Offset: 0x0007E564
		public new static SHA1 Create()
		{
			return SHA1.Create("System.Security.Cryptography.SHA1");
		}

		// Token: 0x06002353 RID: 9043 RVA: 0x00080370 File Offset: 0x0007E570
		public new static SHA1 Create(string hashName)
		{
			return (SHA1)CryptoConfig.CreateFromName(hashName);
		}
	}
}
