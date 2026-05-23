using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000423 RID: 1059
	[Flags]
	internal enum FRAMEDATA_FLAGS : uint
	{
		// Token: 0x04000FA7 RID: 4007
		fHasSEH = 1U,
		// Token: 0x04000FA8 RID: 4008
		fHasEH = 2U,
		// Token: 0x04000FA9 RID: 4009
		fIsFunctionStart = 4U
	}
}
