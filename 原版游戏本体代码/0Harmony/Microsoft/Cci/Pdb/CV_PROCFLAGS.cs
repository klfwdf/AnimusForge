using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003D4 RID: 980
	[Flags]
	internal enum CV_PROCFLAGS : byte
	{
		// Token: 0x04000E15 RID: 3605
		CV_PFLAG_NOFPO = 1,
		// Token: 0x04000E16 RID: 3606
		CV_PFLAG_INT = 2,
		// Token: 0x04000E17 RID: 3607
		CV_PFLAG_FAR = 4,
		// Token: 0x04000E18 RID: 3608
		CV_PFLAG_NEVER = 8,
		// Token: 0x04000E19 RID: 3609
		CV_PFLAG_NOTREACHED = 16,
		// Token: 0x04000E1A RID: 3610
		CV_PFLAG_CUST_CALL = 32,
		// Token: 0x04000E1B RID: 3611
		CV_PFLAG_NOINLINE = 64,
		// Token: 0x04000E1C RID: 3612
		CV_PFLAG_OPTDBGINFO = 128
	}
}
