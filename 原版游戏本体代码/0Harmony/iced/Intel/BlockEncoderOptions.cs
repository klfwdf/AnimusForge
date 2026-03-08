using System;

namespace Iced.Intel
{
	// Token: 0x0200062D RID: 1581
	[Flags]
	internal enum BlockEncoderOptions
	{
		// Token: 0x04001688 RID: 5768
		None = 0,
		// Token: 0x04001689 RID: 5769
		DontFixBranches = 1,
		// Token: 0x0400168A RID: 5770
		ReturnRelocInfos = 2,
		// Token: 0x0400168B RID: 5771
		ReturnNewInstructionOffsets = 4,
		// Token: 0x0400168C RID: 5772
		ReturnConstantOffsets = 8
	}
}
