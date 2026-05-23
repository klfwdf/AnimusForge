using System;

namespace System.Security.AccessControl
{
	// Token: 0x02000210 RID: 528
	[Flags]
	public enum CryptoKeyRights
	{
		// Token: 0x04000B16 RID: 2838
		ReadData = 1,
		// Token: 0x04000B17 RID: 2839
		WriteData = 2,
		// Token: 0x04000B18 RID: 2840
		ReadExtendedAttributes = 8,
		// Token: 0x04000B19 RID: 2841
		WriteExtendedAttributes = 16,
		// Token: 0x04000B1A RID: 2842
		ReadAttributes = 128,
		// Token: 0x04000B1B RID: 2843
		WriteAttributes = 256,
		// Token: 0x04000B1C RID: 2844
		Delete = 65536,
		// Token: 0x04000B1D RID: 2845
		ReadPermissions = 131072,
		// Token: 0x04000B1E RID: 2846
		ChangePermissions = 262144,
		// Token: 0x04000B1F RID: 2847
		TakeOwnership = 524288,
		// Token: 0x04000B20 RID: 2848
		Synchronize = 1048576,
		// Token: 0x04000B21 RID: 2849
		FullControl = 2032027,
		// Token: 0x04000B22 RID: 2850
		GenericAll = 268435456,
		// Token: 0x04000B23 RID: 2851
		GenericExecute = 536870912,
		// Token: 0x04000B24 RID: 2852
		GenericWrite = 1073741824,
		// Token: 0x04000B25 RID: 2853
		GenericRead = -2147483648
	}
}
