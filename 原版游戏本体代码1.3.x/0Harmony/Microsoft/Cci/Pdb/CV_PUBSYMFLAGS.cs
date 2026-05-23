using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003F4 RID: 1012
	[Flags]
	internal enum CV_PUBSYMFLAGS : uint
	{
		// Token: 0x04000EA1 RID: 3745
		fNone = 0U,
		// Token: 0x04000EA2 RID: 3746
		fCode = 1U,
		// Token: 0x04000EA3 RID: 3747
		fFunction = 2U,
		// Token: 0x04000EA4 RID: 3748
		fManaged = 4U,
		// Token: 0x04000EA5 RID: 3749
		fMSIL = 8U
	}
}
