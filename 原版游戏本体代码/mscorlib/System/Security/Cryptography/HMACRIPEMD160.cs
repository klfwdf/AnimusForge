using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000264 RID: 612
	[ComVisible(true)]
	public class HMACRIPEMD160 : HMAC
	{
		// Token: 0x060021C0 RID: 8640 RVA: 0x00077A36 File Offset: 0x00075C36
		public HMACRIPEMD160()
			: this(Utils.GenerateRandom(64))
		{
		}

		// Token: 0x060021C1 RID: 8641 RVA: 0x00077A45 File Offset: 0x00075C45
		public HMACRIPEMD160(byte[] key)
		{
			this.m_hashName = "RIPEMD160";
			this.m_hash1 = new RIPEMD160Managed();
			this.m_hash2 = new RIPEMD160Managed();
			this.HashSizeValue = 160;
			base.InitializeKey(key);
		}
	}
}
