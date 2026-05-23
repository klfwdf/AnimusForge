using System;

namespace Mono.Cecil
{
	// Token: 0x02000281 RID: 641
	[Flags]
	internal enum ModuleAttributes
	{
		// Token: 0x04000501 RID: 1281
		ILOnly = 1,
		// Token: 0x04000502 RID: 1282
		Required32Bit = 2,
		// Token: 0x04000503 RID: 1283
		ILLibrary = 4,
		// Token: 0x04000504 RID: 1284
		StrongNameSigned = 8,
		// Token: 0x04000505 RID: 1285
		Preferred32Bit = 131072
	}
}
