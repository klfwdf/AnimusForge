using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200027A RID: 634
	[ComVisible(true)]
	public abstract class RIPEMD160 : HashAlgorithm
	{
		// Token: 0x0600227B RID: 8827 RVA: 0x0007A27C File Offset: 0x0007847C
		protected RIPEMD160()
		{
			this.HashSizeValue = 160;
		}

		// Token: 0x0600227C RID: 8828 RVA: 0x0007A28F File Offset: 0x0007848F
		public new static RIPEMD160 Create()
		{
			return RIPEMD160.Create("System.Security.Cryptography.RIPEMD160");
		}

		// Token: 0x0600227D RID: 8829 RVA: 0x0007A29B File Offset: 0x0007849B
		public new static RIPEMD160 Create(string hashName)
		{
			return (RIPEMD160)CryptoConfig.CreateFromName(hashName);
		}
	}
}
