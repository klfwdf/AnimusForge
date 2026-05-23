using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000389 RID: 905
	[Flags]
	internal enum LeafPointerAttr : uint
	{
		// Token: 0x04000CE9 RID: 3305
		ptrtype = 31U,
		// Token: 0x04000CEA RID: 3306
		ptrmode = 224U,
		// Token: 0x04000CEB RID: 3307
		isflat32 = 256U,
		// Token: 0x04000CEC RID: 3308
		isvolatile = 512U,
		// Token: 0x04000CED RID: 3309
		isconst = 1024U,
		// Token: 0x04000CEE RID: 3310
		isunaligned = 2048U,
		// Token: 0x04000CEF RID: 3311
		isrestrict = 4096U
	}
}
