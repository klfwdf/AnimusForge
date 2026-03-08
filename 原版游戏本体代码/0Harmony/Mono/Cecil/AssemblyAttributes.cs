using System;

namespace Mono.Cecil
{
	// Token: 0x020001E8 RID: 488
	[Flags]
	internal enum AssemblyAttributes : uint
	{
		// Token: 0x04000322 RID: 802
		PublicKey = 1U,
		// Token: 0x04000323 RID: 803
		SideBySideCompatible = 0U,
		// Token: 0x04000324 RID: 804
		Retargetable = 256U,
		// Token: 0x04000325 RID: 805
		WindowsRuntime = 512U,
		// Token: 0x04000326 RID: 806
		DisableJITCompileOptimizer = 16384U,
		// Token: 0x04000327 RID: 807
		EnableJITCompileTracking = 32768U
	}
}
