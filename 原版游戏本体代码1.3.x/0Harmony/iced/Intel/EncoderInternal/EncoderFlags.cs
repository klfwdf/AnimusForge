using System;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200067B RID: 1659
	[Flags]
	internal enum EncoderFlags : uint
	{
		// Token: 0x04003493 RID: 13459
		None = 0U,
		// Token: 0x04003494 RID: 13460
		B = 1U,
		// Token: 0x04003495 RID: 13461
		X = 2U,
		// Token: 0x04003496 RID: 13462
		R = 4U,
		// Token: 0x04003497 RID: 13463
		W = 8U,
		// Token: 0x04003498 RID: 13464
		ModRM = 16U,
		// Token: 0x04003499 RID: 13465
		Sib = 32U,
		// Token: 0x0400349A RID: 13466
		REX = 64U,
		// Token: 0x0400349B RID: 13467
		P66 = 128U,
		// Token: 0x0400349C RID: 13468
		P67 = 256U,
		// Token: 0x0400349D RID: 13469
		R2 = 512U,
		// Token: 0x0400349E RID: 13470
		Broadcast = 1024U,
		// Token: 0x0400349F RID: 13471
		HighLegacy8BitRegs = 2048U,
		// Token: 0x040034A0 RID: 13472
		Displ = 4096U,
		// Token: 0x040034A1 RID: 13473
		PF0 = 8192U,
		// Token: 0x040034A2 RID: 13474
		RegIsMemory = 16384U,
		// Token: 0x040034A3 RID: 13475
		MustUseSib = 32768U,
		// Token: 0x040034A4 RID: 13476
		VvvvvShift = 27U,
		// Token: 0x040034A5 RID: 13477
		VvvvvMask = 31U
	}
}
