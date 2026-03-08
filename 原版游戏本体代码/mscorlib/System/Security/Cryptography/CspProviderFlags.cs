using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000254 RID: 596
	[ComVisible(true)]
	[Flags]
	[Serializable]
	public enum CspProviderFlags
	{
		// Token: 0x04000C02 RID: 3074
		NoFlags = 0,
		// Token: 0x04000C03 RID: 3075
		UseMachineKeyStore = 1,
		// Token: 0x04000C04 RID: 3076
		UseDefaultKeyContainer = 2,
		// Token: 0x04000C05 RID: 3077
		UseNonExportableKey = 4,
		// Token: 0x04000C06 RID: 3078
		UseExistingKey = 8,
		// Token: 0x04000C07 RID: 3079
		UseArchivableKey = 16,
		// Token: 0x04000C08 RID: 3080
		UseUserProtectedKey = 32,
		// Token: 0x04000C09 RID: 3081
		NoPrompt = 64,
		// Token: 0x04000C0A RID: 3082
		CreateEphemeralKey = 128
	}
}
