using System;

namespace Mono.Cecil
{
	// Token: 0x0200028D RID: 653
	[Flags]
	internal enum PropertyAttributes : ushort
	{
		// Token: 0x04000561 RID: 1377
		None = 0,
		// Token: 0x04000562 RID: 1378
		SpecialName = 512,
		// Token: 0x04000563 RID: 1379
		RTSpecialName = 1024,
		// Token: 0x04000564 RID: 1380
		HasDefault = 4096,
		// Token: 0x04000565 RID: 1381
		Unused = 59903
	}
}
