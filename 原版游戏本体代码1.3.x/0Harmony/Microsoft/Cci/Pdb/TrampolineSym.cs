using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x020003FB RID: 1019
	internal struct TrampolineSym
	{
		// Token: 0x04000EDD RID: 3805
		internal ushort trampType;

		// Token: 0x04000EDE RID: 3806
		internal ushort cbThunk;

		// Token: 0x04000EDF RID: 3807
		internal uint offThunk;

		// Token: 0x04000EE0 RID: 3808
		internal uint offTarget;

		// Token: 0x04000EE1 RID: 3809
		internal ushort sectThunk;

		// Token: 0x04000EE2 RID: 3810
		internal ushort sectTarget;
	}
}
