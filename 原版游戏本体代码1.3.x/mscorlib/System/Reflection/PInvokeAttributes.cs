using System;

namespace System.Reflection
{
	// Token: 0x020005F8 RID: 1528
	[Flags]
	[Serializable]
	internal enum PInvokeAttributes
	{
		// Token: 0x04001D16 RID: 7446
		NoMangle = 1,
		// Token: 0x04001D17 RID: 7447
		CharSetMask = 6,
		// Token: 0x04001D18 RID: 7448
		CharSetNotSpec = 0,
		// Token: 0x04001D19 RID: 7449
		CharSetAnsi = 2,
		// Token: 0x04001D1A RID: 7450
		CharSetUnicode = 4,
		// Token: 0x04001D1B RID: 7451
		CharSetAuto = 6,
		// Token: 0x04001D1C RID: 7452
		BestFitUseAssem = 0,
		// Token: 0x04001D1D RID: 7453
		BestFitEnabled = 16,
		// Token: 0x04001D1E RID: 7454
		BestFitDisabled = 32,
		// Token: 0x04001D1F RID: 7455
		BestFitMask = 48,
		// Token: 0x04001D20 RID: 7456
		ThrowOnUnmappableCharUseAssem = 0,
		// Token: 0x04001D21 RID: 7457
		ThrowOnUnmappableCharEnabled = 4096,
		// Token: 0x04001D22 RID: 7458
		ThrowOnUnmappableCharDisabled = 8192,
		// Token: 0x04001D23 RID: 7459
		ThrowOnUnmappableCharMask = 12288,
		// Token: 0x04001D24 RID: 7460
		SupportsLastError = 64,
		// Token: 0x04001D25 RID: 7461
		CallConvMask = 1792,
		// Token: 0x04001D26 RID: 7462
		CallConvWinapi = 256,
		// Token: 0x04001D27 RID: 7463
		CallConvCdecl = 512,
		// Token: 0x04001D28 RID: 7464
		CallConvStdcall = 768,
		// Token: 0x04001D29 RID: 7465
		CallConvThiscall = 1024,
		// Token: 0x04001D2A RID: 7466
		CallConvFastcall = 1280,
		// Token: 0x04001D2B RID: 7467
		MaxValue = 65535
	}
}
