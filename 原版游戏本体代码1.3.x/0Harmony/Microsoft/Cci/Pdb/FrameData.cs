using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000424 RID: 1060
	internal struct FrameData
	{
		// Token: 0x04000FAA RID: 4010
		internal uint ulRvaStart;

		// Token: 0x04000FAB RID: 4011
		internal uint cbBlock;

		// Token: 0x04000FAC RID: 4012
		internal uint cbLocals;

		// Token: 0x04000FAD RID: 4013
		internal uint cbParams;

		// Token: 0x04000FAE RID: 4014
		internal uint cbStkMax;

		// Token: 0x04000FAF RID: 4015
		internal uint frameFunc;

		// Token: 0x04000FB0 RID: 4016
		internal ushort cbProlog;

		// Token: 0x04000FB1 RID: 4017
		internal ushort cbSavedRegs;

		// Token: 0x04000FB2 RID: 4018
		internal uint flags;
	}
}
