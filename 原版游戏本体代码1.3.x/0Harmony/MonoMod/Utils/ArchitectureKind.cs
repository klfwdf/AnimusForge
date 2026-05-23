using System;

namespace MonoMod.Utils
{
	// Token: 0x02000875 RID: 2165
	internal enum ArchitectureKind
	{
		// Token: 0x04003A4E RID: 14926
		Unknown,
		// Token: 0x04003A4F RID: 14927
		Bits64,
		// Token: 0x04003A50 RID: 14928
		x86,
		// Token: 0x04003A51 RID: 14929
		x86_64,
		// Token: 0x04003A52 RID: 14930
		AMD64 = 3,
		// Token: 0x04003A53 RID: 14931
		Arm,
		// Token: 0x04003A54 RID: 14932
		Arm64
	}
}
