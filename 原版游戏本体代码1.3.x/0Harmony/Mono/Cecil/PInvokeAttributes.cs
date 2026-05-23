using System;

namespace Mono.Cecil
{
	// Token: 0x0200028A RID: 650
	[Flags]
	internal enum PInvokeAttributes : ushort
	{
		// Token: 0x0400054A RID: 1354
		NoMangle = 1,
		// Token: 0x0400054B RID: 1355
		CharSetMask = 6,
		// Token: 0x0400054C RID: 1356
		CharSetNotSpec = 0,
		// Token: 0x0400054D RID: 1357
		CharSetAnsi = 2,
		// Token: 0x0400054E RID: 1358
		CharSetUnicode = 4,
		// Token: 0x0400054F RID: 1359
		CharSetAuto = 6,
		// Token: 0x04000550 RID: 1360
		SupportsLastError = 64,
		// Token: 0x04000551 RID: 1361
		CallConvMask = 1792,
		// Token: 0x04000552 RID: 1362
		CallConvWinapi = 256,
		// Token: 0x04000553 RID: 1363
		CallConvCdecl = 512,
		// Token: 0x04000554 RID: 1364
		CallConvStdCall = 768,
		// Token: 0x04000555 RID: 1365
		CallConvThiscall = 1024,
		// Token: 0x04000556 RID: 1366
		CallConvFastcall = 1280,
		// Token: 0x04000557 RID: 1367
		BestFitMask = 48,
		// Token: 0x04000558 RID: 1368
		BestFitEnabled = 16,
		// Token: 0x04000559 RID: 1369
		BestFitDisabled = 32,
		// Token: 0x0400055A RID: 1370
		ThrowOnUnmappableCharMask = 12288,
		// Token: 0x0400055B RID: 1371
		ThrowOnUnmappableCharEnabled = 4096,
		// Token: 0x0400055C RID: 1372
		ThrowOnUnmappableCharDisabled = 8192
	}
}
