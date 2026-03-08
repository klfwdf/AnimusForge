using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000271 RID: 625
	[ComVisible(true)]
	public abstract class MD5 : HashAlgorithm
	{
		// Token: 0x06002227 RID: 8743 RVA: 0x00078CE1 File Offset: 0x00076EE1
		protected MD5()
		{
			this.HashSizeValue = 128;
		}

		// Token: 0x06002228 RID: 8744 RVA: 0x00078CF4 File Offset: 0x00076EF4
		public new static MD5 Create()
		{
			return MD5.Create("System.Security.Cryptography.MD5");
		}

		// Token: 0x06002229 RID: 8745 RVA: 0x00078D00 File Offset: 0x00076F00
		public new static MD5 Create(string algName)
		{
			return (MD5)CryptoConfig.CreateFromName(algName);
		}
	}
}
