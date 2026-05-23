using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000405 RID: 1029
	internal struct ProcSymIa64
	{
		// Token: 0x04000F19 RID: 3865
		internal uint parent;

		// Token: 0x04000F1A RID: 3866
		internal uint end;

		// Token: 0x04000F1B RID: 3867
		internal uint next;

		// Token: 0x04000F1C RID: 3868
		internal uint len;

		// Token: 0x04000F1D RID: 3869
		internal uint dbgStart;

		// Token: 0x04000F1E RID: 3870
		internal uint dbgEnd;

		// Token: 0x04000F1F RID: 3871
		internal uint typind;

		// Token: 0x04000F20 RID: 3872
		internal uint off;

		// Token: 0x04000F21 RID: 3873
		internal ushort seg;

		// Token: 0x04000F22 RID: 3874
		internal ushort retReg;

		// Token: 0x04000F23 RID: 3875
		internal byte flags;

		// Token: 0x04000F24 RID: 3876
		internal string name;
	}
}
