using System;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000681 RID: 1665
	[Flags]
	internal enum EncFlags1 : uint
	{
		// Token: 0x040034BF RID: 13503
		None = 0U,
		// Token: 0x040034C0 RID: 13504
		Legacy_OpMask = 127U,
		// Token: 0x040034C1 RID: 13505
		Legacy_Op0Shift = 0U,
		// Token: 0x040034C2 RID: 13506
		Legacy_Op1Shift = 7U,
		// Token: 0x040034C3 RID: 13507
		Legacy_Op2Shift = 14U,
		// Token: 0x040034C4 RID: 13508
		Legacy_Op3Shift = 21U,
		// Token: 0x040034C5 RID: 13509
		VEX_OpMask = 63U,
		// Token: 0x040034C6 RID: 13510
		VEX_Op0Shift = 0U,
		// Token: 0x040034C7 RID: 13511
		VEX_Op1Shift = 6U,
		// Token: 0x040034C8 RID: 13512
		VEX_Op2Shift = 12U,
		// Token: 0x040034C9 RID: 13513
		VEX_Op3Shift = 18U,
		// Token: 0x040034CA RID: 13514
		VEX_Op4Shift = 24U,
		// Token: 0x040034CB RID: 13515
		XOP_OpMask = 31U,
		// Token: 0x040034CC RID: 13516
		XOP_Op0Shift = 0U,
		// Token: 0x040034CD RID: 13517
		XOP_Op1Shift = 5U,
		// Token: 0x040034CE RID: 13518
		XOP_Op2Shift = 10U,
		// Token: 0x040034CF RID: 13519
		XOP_Op3Shift = 15U,
		// Token: 0x040034D0 RID: 13520
		EVEX_OpMask = 31U,
		// Token: 0x040034D1 RID: 13521
		EVEX_Op0Shift = 0U,
		// Token: 0x040034D2 RID: 13522
		EVEX_Op1Shift = 5U,
		// Token: 0x040034D3 RID: 13523
		EVEX_Op2Shift = 10U,
		// Token: 0x040034D4 RID: 13524
		EVEX_Op3Shift = 15U,
		// Token: 0x040034D5 RID: 13525
		MVEX_OpMask = 15U,
		// Token: 0x040034D6 RID: 13526
		MVEX_Op0Shift = 0U,
		// Token: 0x040034D7 RID: 13527
		MVEX_Op1Shift = 4U,
		// Token: 0x040034D8 RID: 13528
		MVEX_Op2Shift = 8U,
		// Token: 0x040034D9 RID: 13529
		MVEX_Op3Shift = 12U,
		// Token: 0x040034DA RID: 13530
		IgnoresRoundingControl = 1073741824U,
		// Token: 0x040034DB RID: 13531
		AmdLockRegBit = 2147483648U
	}
}
