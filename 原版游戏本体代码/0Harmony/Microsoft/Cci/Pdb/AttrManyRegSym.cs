using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003E0 RID: 992
	internal struct AttrManyRegSym
	{
		// Token: 0x04000E4B RID: 3659
		internal uint typind;

		// Token: 0x04000E4C RID: 3660
		internal uint offCod;

		// Token: 0x04000E4D RID: 3661
		internal ushort segCod;

		// Token: 0x04000E4E RID: 3662
		internal ushort flags;

		// Token: 0x04000E4F RID: 3663
		internal byte count;

		// Token: 0x04000E50 RID: 3664
		internal byte[] reg;

		// Token: 0x04000E51 RID: 3665
		internal string name;
	}
}
