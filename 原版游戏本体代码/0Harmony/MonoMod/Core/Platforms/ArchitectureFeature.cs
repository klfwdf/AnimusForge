using System;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004F5 RID: 1269
	[Flags]
	internal enum ArchitectureFeature
	{
		// Token: 0x040011B0 RID: 4528
		None = 0,
		// Token: 0x040011B1 RID: 4529
		FixedInstructionSize = 1,
		// Token: 0x040011B2 RID: 4530
		Immediate64 = 2,
		// Token: 0x040011B3 RID: 4531
		CreateAltEntryPoint = 4
	}
}
