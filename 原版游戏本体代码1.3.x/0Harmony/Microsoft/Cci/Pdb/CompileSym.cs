using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003E9 RID: 1001
	internal struct CompileSym
	{
		// Token: 0x04000E77 RID: 3703
		internal uint flags;

		// Token: 0x04000E78 RID: 3704
		internal ushort machine;

		// Token: 0x04000E79 RID: 3705
		internal ushort verFEMajor;

		// Token: 0x04000E7A RID: 3706
		internal ushort verFEMinor;

		// Token: 0x04000E7B RID: 3707
		internal ushort verFEBuild;

		// Token: 0x04000E7C RID: 3708
		internal ushort verMajor;

		// Token: 0x04000E7D RID: 3709
		internal ushort verMinor;

		// Token: 0x04000E7E RID: 3710
		internal ushort verBuild;

		// Token: 0x04000E7F RID: 3711
		internal string verSt;

		// Token: 0x04000E80 RID: 3712
		internal string[] verArgs;
	}
}
