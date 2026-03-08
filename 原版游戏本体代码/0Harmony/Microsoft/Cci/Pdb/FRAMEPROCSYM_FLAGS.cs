using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200040A RID: 1034
	[Flags]
	internal enum FRAMEPROCSYM_FLAGS : uint
	{
		// Token: 0x04000F31 RID: 3889
		fHasAlloca = 1U,
		// Token: 0x04000F32 RID: 3890
		fHasSetJmp = 2U,
		// Token: 0x04000F33 RID: 3891
		fHasLongJmp = 4U,
		// Token: 0x04000F34 RID: 3892
		fHasInlAsm = 8U,
		// Token: 0x04000F35 RID: 3893
		fHasEH = 16U,
		// Token: 0x04000F36 RID: 3894
		fInlSpec = 32U,
		// Token: 0x04000F37 RID: 3895
		fHasSEH = 64U,
		// Token: 0x04000F38 RID: 3896
		fNaked = 128U,
		// Token: 0x04000F39 RID: 3897
		fSecurityChecks = 256U,
		// Token: 0x04000F3A RID: 3898
		fAsyncEH = 512U,
		// Token: 0x04000F3B RID: 3899
		fGSNoStackOrdering = 1024U,
		// Token: 0x04000F3C RID: 3900
		fWasInlined = 2048U
	}
}
