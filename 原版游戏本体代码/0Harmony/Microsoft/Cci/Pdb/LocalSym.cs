using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200040E RID: 1038
	internal struct LocalSym
	{
		// Token: 0x04000F4D RID: 3917
		internal uint id;

		// Token: 0x04000F4E RID: 3918
		internal uint typind;

		// Token: 0x04000F4F RID: 3919
		internal ushort flags;

		// Token: 0x04000F50 RID: 3920
		internal uint idParent;

		// Token: 0x04000F51 RID: 3921
		internal uint offParent;

		// Token: 0x04000F52 RID: 3922
		internal uint expr;

		// Token: 0x04000F53 RID: 3923
		internal uint pad0;

		// Token: 0x04000F54 RID: 3924
		internal uint pad1;

		// Token: 0x04000F55 RID: 3925
		internal string name;
	}
}
