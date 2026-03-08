using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003E8 RID: 1000
	[Flags]
	internal enum COMPILESYM_FLAGS : uint
	{
		// Token: 0x04000E6D RID: 3693
		iLanguage = 255U,
		// Token: 0x04000E6E RID: 3694
		fEC = 256U,
		// Token: 0x04000E6F RID: 3695
		fNoDbgInfo = 512U,
		// Token: 0x04000E70 RID: 3696
		fLTCG = 1024U,
		// Token: 0x04000E71 RID: 3697
		fNoDataAlign = 2048U,
		// Token: 0x04000E72 RID: 3698
		fManagedPresent = 4096U,
		// Token: 0x04000E73 RID: 3699
		fSecurityChecks = 8192U,
		// Token: 0x04000E74 RID: 3700
		fHotPatch = 16384U,
		// Token: 0x04000E75 RID: 3701
		fCVTCIL = 32768U,
		// Token: 0x04000E76 RID: 3702
		fMSILModule = 65536U
	}
}
