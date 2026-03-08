using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003D6 RID: 982
	[Flags]
	internal enum CV_LVARFLAGS : ushort
	{
		// Token: 0x04000E20 RID: 3616
		fIsParam = 1,
		// Token: 0x04000E21 RID: 3617
		fAddrTaken = 2,
		// Token: 0x04000E22 RID: 3618
		fCompGenx = 4,
		// Token: 0x04000E23 RID: 3619
		fIsAggregate = 8,
		// Token: 0x04000E24 RID: 3620
		fIsAggregated = 16,
		// Token: 0x04000E25 RID: 3621
		fIsAliased = 32,
		// Token: 0x04000E26 RID: 3622
		fIsAlias = 64
	}
}
