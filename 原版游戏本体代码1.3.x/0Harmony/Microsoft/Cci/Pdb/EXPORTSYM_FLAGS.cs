using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000413 RID: 1043
	[Flags]
	internal enum EXPORTSYM_FLAGS : ushort
	{
		// Token: 0x04000F6A RID: 3946
		fConstant = 1,
		// Token: 0x04000F6B RID: 3947
		fData = 2,
		// Token: 0x04000F6C RID: 3948
		fPrivate = 4,
		// Token: 0x04000F6D RID: 3949
		fNoName = 8,
		// Token: 0x04000F6E RID: 3950
		fOrdinal = 16,
		// Token: 0x04000F6F RID: 3951
		fForwarder = 32
	}
}
