using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000393 RID: 915
	internal struct LeafMFunc
	{
		// Token: 0x04000D0F RID: 3343
		internal uint rvtype;

		// Token: 0x04000D10 RID: 3344
		internal uint classtype;

		// Token: 0x04000D11 RID: 3345
		internal uint thistype;

		// Token: 0x04000D12 RID: 3346
		internal byte calltype;

		// Token: 0x04000D13 RID: 3347
		internal byte reserved;

		// Token: 0x04000D14 RID: 3348
		internal ushort parmcount;

		// Token: 0x04000D15 RID: 3349
		internal uint arglist;

		// Token: 0x04000D16 RID: 3350
		internal int thisadjust;
	}
}
