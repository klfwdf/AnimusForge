using System;

namespace Iced.Intel
{
	// Token: 0x02000639 RID: 1593
	[Flags]
	internal enum StateFlags : uint
	{
		// Token: 0x040029F4 RID: 10740
		IpRel64 = 1U,
		// Token: 0x040029F5 RID: 10741
		IpRel32 = 2U,
		// Token: 0x040029F6 RID: 10742
		HasRex = 8U,
		// Token: 0x040029F7 RID: 10743
		b = 16U,
		// Token: 0x040029F8 RID: 10744
		z = 32U,
		// Token: 0x040029F9 RID: 10745
		IsInvalid = 64U,
		// Token: 0x040029FA RID: 10746
		W = 128U,
		// Token: 0x040029FB RID: 10747
		NoImm = 256U,
		// Token: 0x040029FC RID: 10748
		Addr64 = 512U,
		// Token: 0x040029FD RID: 10749
		BranchImm8 = 1024U,
		// Token: 0x040029FE RID: 10750
		Xbegin = 2048U,
		// Token: 0x040029FF RID: 10751
		Lock = 4096U,
		// Token: 0x04002A00 RID: 10752
		AllowLock = 8192U,
		// Token: 0x04002A01 RID: 10753
		NoMoreBytes = 16384U,
		// Token: 0x04002A02 RID: 10754
		Has66 = 32768U,
		// Token: 0x04002A03 RID: 10755
		MvexSssMask = 7U,
		// Token: 0x04002A04 RID: 10756
		MvexSssShift = 16U,
		// Token: 0x04002A05 RID: 10757
		MvexEH = 524288U,
		// Token: 0x04002A06 RID: 10758
		EncodingMask = 7U,
		// Token: 0x04002A07 RID: 10759
		EncodingShift = 29U
	}
}
