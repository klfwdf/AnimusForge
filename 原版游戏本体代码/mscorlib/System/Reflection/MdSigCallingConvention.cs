using System;

namespace System.Reflection
{
	// Token: 0x020005F7 RID: 1527
	[Flags]
	[Serializable]
	internal enum MdSigCallingConvention : byte
	{
		// Token: 0x04001D06 RID: 7430
		CallConvMask = 15,
		// Token: 0x04001D07 RID: 7431
		Default = 0,
		// Token: 0x04001D08 RID: 7432
		C = 1,
		// Token: 0x04001D09 RID: 7433
		StdCall = 2,
		// Token: 0x04001D0A RID: 7434
		ThisCall = 3,
		// Token: 0x04001D0B RID: 7435
		FastCall = 4,
		// Token: 0x04001D0C RID: 7436
		Vararg = 5,
		// Token: 0x04001D0D RID: 7437
		Field = 6,
		// Token: 0x04001D0E RID: 7438
		LocalSig = 7,
		// Token: 0x04001D0F RID: 7439
		Property = 8,
		// Token: 0x04001D10 RID: 7440
		Unmgd = 9,
		// Token: 0x04001D11 RID: 7441
		GenericInst = 10,
		// Token: 0x04001D12 RID: 7442
		Generic = 16,
		// Token: 0x04001D13 RID: 7443
		HasThis = 32,
		// Token: 0x04001D14 RID: 7444
		ExplicitThis = 64
	}
}
