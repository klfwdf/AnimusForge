using System;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006A7 RID: 1703
	[Flags]
	internal enum LegacyHandlerFlags : uint
	{
		// Token: 0x0400358D RID: 13709
		HandlerReg = 1U,
		// Token: 0x0400358E RID: 13710
		HandlerMem = 2U,
		// Token: 0x0400358F RID: 13711
		Handler66Reg = 4U,
		// Token: 0x04003590 RID: 13712
		Handler66Mem = 8U,
		// Token: 0x04003591 RID: 13713
		HandlerF3Reg = 16U,
		// Token: 0x04003592 RID: 13714
		HandlerF3Mem = 32U,
		// Token: 0x04003593 RID: 13715
		HandlerF2Reg = 64U,
		// Token: 0x04003594 RID: 13716
		HandlerF2Mem = 128U
	}
}
