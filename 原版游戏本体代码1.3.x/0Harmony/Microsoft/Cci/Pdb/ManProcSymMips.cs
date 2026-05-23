using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003F8 RID: 1016
	internal struct ManProcSymMips
	{
		// Token: 0x04000EC1 RID: 3777
		internal uint parent;

		// Token: 0x04000EC2 RID: 3778
		internal uint end;

		// Token: 0x04000EC3 RID: 3779
		internal uint next;

		// Token: 0x04000EC4 RID: 3780
		internal uint len;

		// Token: 0x04000EC5 RID: 3781
		internal uint dbgStart;

		// Token: 0x04000EC6 RID: 3782
		internal uint dbgEnd;

		// Token: 0x04000EC7 RID: 3783
		internal uint regSave;

		// Token: 0x04000EC8 RID: 3784
		internal uint fpSave;

		// Token: 0x04000EC9 RID: 3785
		internal uint intOff;

		// Token: 0x04000ECA RID: 3786
		internal uint fpOff;

		// Token: 0x04000ECB RID: 3787
		internal uint token;

		// Token: 0x04000ECC RID: 3788
		internal uint off;

		// Token: 0x04000ECD RID: 3789
		internal ushort seg;

		// Token: 0x04000ECE RID: 3790
		internal byte retReg;

		// Token: 0x04000ECF RID: 3791
		internal byte frameReg;

		// Token: 0x04000ED0 RID: 3792
		internal string name;
	}
}
