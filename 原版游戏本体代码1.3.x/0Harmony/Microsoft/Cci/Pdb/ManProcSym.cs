using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003F7 RID: 1015
	internal struct ManProcSym
	{
		// Token: 0x04000EB5 RID: 3765
		internal uint parent;

		// Token: 0x04000EB6 RID: 3766
		internal uint end;

		// Token: 0x04000EB7 RID: 3767
		internal uint next;

		// Token: 0x04000EB8 RID: 3768
		internal uint len;

		// Token: 0x04000EB9 RID: 3769
		internal uint dbgStart;

		// Token: 0x04000EBA RID: 3770
		internal uint dbgEnd;

		// Token: 0x04000EBB RID: 3771
		internal uint token;

		// Token: 0x04000EBC RID: 3772
		internal uint off;

		// Token: 0x04000EBD RID: 3773
		internal ushort seg;

		// Token: 0x04000EBE RID: 3774
		internal byte flags;

		// Token: 0x04000EBF RID: 3775
		internal ushort retReg;

		// Token: 0x04000EC0 RID: 3776
		internal string name;
	}
}
