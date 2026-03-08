using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000404 RID: 1028
	internal struct ProcSymMips
	{
		// Token: 0x04000F09 RID: 3849
		internal uint parent;

		// Token: 0x04000F0A RID: 3850
		internal uint end;

		// Token: 0x04000F0B RID: 3851
		internal uint next;

		// Token: 0x04000F0C RID: 3852
		internal uint len;

		// Token: 0x04000F0D RID: 3853
		internal uint dbgStart;

		// Token: 0x04000F0E RID: 3854
		internal uint dbgEnd;

		// Token: 0x04000F0F RID: 3855
		internal uint regSave;

		// Token: 0x04000F10 RID: 3856
		internal uint fpSave;

		// Token: 0x04000F11 RID: 3857
		internal uint intOff;

		// Token: 0x04000F12 RID: 3858
		internal uint fpOff;

		// Token: 0x04000F13 RID: 3859
		internal uint typind;

		// Token: 0x04000F14 RID: 3860
		internal uint off;

		// Token: 0x04000F15 RID: 3861
		internal ushort seg;

		// Token: 0x04000F16 RID: 3862
		internal byte retReg;

		// Token: 0x04000F17 RID: 3863
		internal byte frameReg;

		// Token: 0x04000F18 RID: 3864
		internal string name;
	}
}
