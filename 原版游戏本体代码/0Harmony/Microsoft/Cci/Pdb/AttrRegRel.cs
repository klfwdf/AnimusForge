using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000401 RID: 1025
	internal struct AttrRegRel
	{
		// Token: 0x04000EFB RID: 3835
		internal uint off;

		// Token: 0x04000EFC RID: 3836
		internal uint typind;

		// Token: 0x04000EFD RID: 3837
		internal ushort reg;

		// Token: 0x04000EFE RID: 3838
		internal uint offCod;

		// Token: 0x04000EFF RID: 3839
		internal ushort segCod;

		// Token: 0x04000F00 RID: 3840
		internal ushort flags;

		// Token: 0x04000F01 RID: 3841
		internal string name;
	}
}
