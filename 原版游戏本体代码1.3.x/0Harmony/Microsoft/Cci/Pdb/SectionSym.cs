using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000411 RID: 1041
	internal struct SectionSym
	{
		// Token: 0x04000F5D RID: 3933
		internal ushort isec;

		// Token: 0x04000F5E RID: 3934
		internal byte align;

		// Token: 0x04000F5F RID: 3935
		internal byte bReserved;

		// Token: 0x04000F60 RID: 3936
		internal uint rva;

		// Token: 0x04000F61 RID: 3937
		internal uint cb;

		// Token: 0x04000F62 RID: 3938
		internal uint characteristics;

		// Token: 0x04000F63 RID: 3939
		internal string name;
	}
}
