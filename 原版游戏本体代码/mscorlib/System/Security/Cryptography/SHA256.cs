using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000294 RID: 660
	[ComVisible(true)]
	public abstract class SHA256 : HashAlgorithm
	{
		// Token: 0x0600235E RID: 9054 RVA: 0x000804C7 File Offset: 0x0007E6C7
		protected SHA256()
		{
			this.HashSizeValue = 256;
		}

		// Token: 0x0600235F RID: 9055 RVA: 0x000804DA File Offset: 0x0007E6DA
		public new static SHA256 Create()
		{
			return SHA256.Create("System.Security.Cryptography.SHA256");
		}

		// Token: 0x06002360 RID: 9056 RVA: 0x000804E6 File Offset: 0x0007E6E6
		public new static SHA256 Create(string hashName)
		{
			return (SHA256)CryptoConfig.CreateFromName(hashName);
		}
	}
}
