using System;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004E3 RID: 1251
	[Flags]
	internal enum AddressKind
	{
		// Token: 0x04001161 RID: 4449
		Rel32 = 0,
		// Token: 0x04001162 RID: 4450
		Rel64 = 2,
		// Token: 0x04001163 RID: 4451
		Abs32 = 1,
		// Token: 0x04001164 RID: 4452
		Abs64 = 3,
		// Token: 0x04001165 RID: 4453
		PrecodeFixupThunkRel32 = 4,
		// Token: 0x04001166 RID: 4454
		PrecodeFixupThunkRel64 = 6,
		// Token: 0x04001167 RID: 4455
		PrecodeFixupThunkAbs32 = 5,
		// Token: 0x04001168 RID: 4456
		PrecodeFixupThunkAbs64 = 7,
		// Token: 0x04001169 RID: 4457
		Indirect = 8,
		// Token: 0x0400116A RID: 4458
		ConstantAddr = 16
	}
}
