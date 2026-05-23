using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200025C RID: 604
	[ComVisible(true)]
	[Serializable]
	public struct DSAParameters
	{
		// Token: 0x04000C30 RID: 3120
		public byte[] P;

		// Token: 0x04000C31 RID: 3121
		public byte[] Q;

		// Token: 0x04000C32 RID: 3122
		public byte[] G;

		// Token: 0x04000C33 RID: 3123
		public byte[] Y;

		// Token: 0x04000C34 RID: 3124
		public byte[] J;

		// Token: 0x04000C35 RID: 3125
		[NonSerialized]
		public byte[] X;

		// Token: 0x04000C36 RID: 3126
		public byte[] Seed;

		// Token: 0x04000C37 RID: 3127
		public int Counter;
	}
}
