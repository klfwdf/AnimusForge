using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000296 RID: 662
	[ComVisible(true)]
	public abstract class SHA384 : HashAlgorithm
	{
		// Token: 0x06002367 RID: 9063 RVA: 0x0008058E File Offset: 0x0007E78E
		protected SHA384()
		{
			this.HashSizeValue = 384;
		}

		// Token: 0x06002368 RID: 9064 RVA: 0x000805A1 File Offset: 0x0007E7A1
		public new static SHA384 Create()
		{
			return SHA384.Create("System.Security.Cryptography.SHA384");
		}

		// Token: 0x06002369 RID: 9065 RVA: 0x000805AD File Offset: 0x0007E7AD
		public new static SHA384 Create(string hashName)
		{
			return (SHA384)CryptoConfig.CreateFromName(hashName);
		}
	}
}
