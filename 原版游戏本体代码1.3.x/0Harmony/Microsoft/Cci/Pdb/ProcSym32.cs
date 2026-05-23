using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003F6 RID: 1014
	internal struct ProcSym32
	{
		// Token: 0x04000EAA RID: 3754
		internal uint parent;

		// Token: 0x04000EAB RID: 3755
		internal uint end;

		// Token: 0x04000EAC RID: 3756
		internal uint next;

		// Token: 0x04000EAD RID: 3757
		internal uint len;

		// Token: 0x04000EAE RID: 3758
		internal uint dbgStart;

		// Token: 0x04000EAF RID: 3759
		internal uint dbgEnd;

		// Token: 0x04000EB0 RID: 3760
		internal uint typind;

		// Token: 0x04000EB1 RID: 3761
		internal uint off;

		// Token: 0x04000EB2 RID: 3762
		internal ushort seg;

		// Token: 0x04000EB3 RID: 3763
		internal byte flags;

		// Token: 0x04000EB4 RID: 3764
		internal string name;
	}
}
