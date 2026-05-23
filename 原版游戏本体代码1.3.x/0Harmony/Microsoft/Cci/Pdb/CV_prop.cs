using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000380 RID: 896
	[Flags]
	internal enum CV_prop : ushort
	{
		// Token: 0x04000CC9 RID: 3273
		packed = 1,
		// Token: 0x04000CCA RID: 3274
		ctor = 2,
		// Token: 0x04000CCB RID: 3275
		ovlops = 4,
		// Token: 0x04000CCC RID: 3276
		isnested = 8,
		// Token: 0x04000CCD RID: 3277
		cnested = 16,
		// Token: 0x04000CCE RID: 3278
		opassign = 32,
		// Token: 0x04000CCF RID: 3279
		opcast = 64,
		// Token: 0x04000CD0 RID: 3280
		fwdref = 128,
		// Token: 0x04000CD1 RID: 3281
		scoped = 256
	}
}
