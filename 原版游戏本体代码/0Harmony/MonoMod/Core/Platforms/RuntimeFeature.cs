using System;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000513 RID: 1299
	[Flags]
	internal enum RuntimeFeature
	{
		// Token: 0x040011F3 RID: 4595
		None = 0,
		// Token: 0x040011F4 RID: 4596
		PreciseGC = 1,
		// Token: 0x040011F5 RID: 4597
		CompileMethodHook = 2,
		// Token: 0x040011F6 RID: 4598
		ILDetour = 4,
		// Token: 0x040011F7 RID: 4599
		GenericSharing = 8,
		// Token: 0x040011F8 RID: 4600
		ListGenericInstantiations = 64,
		// Token: 0x040011F9 RID: 4601
		DisableInlining = 16,
		// Token: 0x040011FA RID: 4602
		Uninlining = 32,
		// Token: 0x040011FB RID: 4603
		RequiresMethodPinning = 128,
		// Token: 0x040011FC RID: 4604
		RequiresMethodIdentification = 256,
		// Token: 0x040011FD RID: 4605
		RequiresBodyThunkWalking = 512,
		// Token: 0x040011FE RID: 4606
		HasKnownABI = 1024,
		// Token: 0x040011FF RID: 4607
		RequiresCustomMethodCompile = 2048
	}
}
