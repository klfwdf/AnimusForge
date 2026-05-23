using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200027C RID: 636
	[ComVisible(true)]
	[Serializable]
	public struct RSAParameters
	{
		// Token: 0x04000C8E RID: 3214
		public byte[] Exponent;

		// Token: 0x04000C8F RID: 3215
		public byte[] Modulus;

		// Token: 0x04000C90 RID: 3216
		[NonSerialized]
		public byte[] P;

		// Token: 0x04000C91 RID: 3217
		[NonSerialized]
		public byte[] Q;

		// Token: 0x04000C92 RID: 3218
		[NonSerialized]
		public byte[] DP;

		// Token: 0x04000C93 RID: 3219
		[NonSerialized]
		public byte[] DQ;

		// Token: 0x04000C94 RID: 3220
		[NonSerialized]
		public byte[] InverseQ;

		// Token: 0x04000C95 RID: 3221
		[NonSerialized]
		public byte[] D;
	}
}
