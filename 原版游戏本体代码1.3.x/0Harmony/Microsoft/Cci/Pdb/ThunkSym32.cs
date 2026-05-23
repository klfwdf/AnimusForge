using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003F9 RID: 1017
	internal struct ThunkSym32
	{
		// Token: 0x04000ED1 RID: 3793
		internal uint parent;

		// Token: 0x04000ED2 RID: 3794
		internal uint end;

		// Token: 0x04000ED3 RID: 3795
		internal uint next;

		// Token: 0x04000ED4 RID: 3796
		internal uint off;

		// Token: 0x04000ED5 RID: 3797
		internal ushort seg;

		// Token: 0x04000ED6 RID: 3798
		internal ushort len;

		// Token: 0x04000ED7 RID: 3799
		internal byte ord;

		// Token: 0x04000ED8 RID: 3800
		internal string name;

		// Token: 0x04000ED9 RID: 3801
		internal byte[] variant;
	}
}
