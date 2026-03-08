using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200040B RID: 1035
	internal struct FrameProcSym
	{
		// Token: 0x04000F3D RID: 3901
		internal uint cbFrame;

		// Token: 0x04000F3E RID: 3902
		internal uint cbPad;

		// Token: 0x04000F3F RID: 3903
		internal uint offPad;

		// Token: 0x04000F40 RID: 3904
		internal uint cbSaveRegs;

		// Token: 0x04000F41 RID: 3905
		internal uint offExHdlr;

		// Token: 0x04000F42 RID: 3906
		internal ushort secExHdlr;

		// Token: 0x04000F43 RID: 3907
		internal uint flags;
	}
}
