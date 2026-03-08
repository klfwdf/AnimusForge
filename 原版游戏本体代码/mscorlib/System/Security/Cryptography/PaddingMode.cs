using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000241 RID: 577
	[ComVisible(true)]
	[Serializable]
	public enum PaddingMode
	{
		// Token: 0x04000BDA RID: 3034
		None = 1,
		// Token: 0x04000BDB RID: 3035
		PKCS7,
		// Token: 0x04000BDC RID: 3036
		Zeros,
		// Token: 0x04000BDD RID: 3037
		ANSIX923,
		// Token: 0x04000BDE RID: 3038
		ISO10126
	}
}
