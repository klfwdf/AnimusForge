using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000953 RID: 2387
	[Serializable]
	internal enum PInvokeMap
	{
		// Token: 0x04002B68 RID: 11112
		NoMangle = 1,
		// Token: 0x04002B69 RID: 11113
		CharSetMask = 6,
		// Token: 0x04002B6A RID: 11114
		CharSetNotSpec = 0,
		// Token: 0x04002B6B RID: 11115
		CharSetAnsi = 2,
		// Token: 0x04002B6C RID: 11116
		CharSetUnicode = 4,
		// Token: 0x04002B6D RID: 11117
		CharSetAuto = 6,
		// Token: 0x04002B6E RID: 11118
		PinvokeOLE = 32,
		// Token: 0x04002B6F RID: 11119
		SupportsLastError = 64,
		// Token: 0x04002B70 RID: 11120
		BestFitMask = 48,
		// Token: 0x04002B71 RID: 11121
		BestFitEnabled = 16,
		// Token: 0x04002B72 RID: 11122
		BestFitDisabled = 32,
		// Token: 0x04002B73 RID: 11123
		BestFitUseAsm = 48,
		// Token: 0x04002B74 RID: 11124
		ThrowOnUnmappableCharMask = 12288,
		// Token: 0x04002B75 RID: 11125
		ThrowOnUnmappableCharEnabled = 4096,
		// Token: 0x04002B76 RID: 11126
		ThrowOnUnmappableCharDisabled = 8192,
		// Token: 0x04002B77 RID: 11127
		ThrowOnUnmappableCharUseAsm = 12288,
		// Token: 0x04002B78 RID: 11128
		CallConvMask = 1792,
		// Token: 0x04002B79 RID: 11129
		CallConvWinapi = 256,
		// Token: 0x04002B7A RID: 11130
		CallConvCdecl = 512,
		// Token: 0x04002B7B RID: 11131
		CallConvStdcall = 768,
		// Token: 0x04002B7C RID: 11132
		CallConvThiscall = 1024,
		// Token: 0x04002B7D RID: 11133
		CallConvFastcall = 1280
	}
}
