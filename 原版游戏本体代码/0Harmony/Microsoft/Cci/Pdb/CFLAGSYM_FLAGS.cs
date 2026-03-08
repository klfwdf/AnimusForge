using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003E6 RID: 998
	[Flags]
	internal enum CFLAGSYM_FLAGS : ushort
	{
		// Token: 0x04000E62 RID: 3682
		pcode = 1,
		// Token: 0x04000E63 RID: 3683
		floatprec = 6,
		// Token: 0x04000E64 RID: 3684
		floatpkg = 24,
		// Token: 0x04000E65 RID: 3685
		ambdata = 224,
		// Token: 0x04000E66 RID: 3686
		ambcode = 1792,
		// Token: 0x04000E67 RID: 3687
		mode32 = 2048
	}
}
