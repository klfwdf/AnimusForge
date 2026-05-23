using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002AF RID: 687
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum X509KeyStorageFlags
	{
		// Token: 0x04000D97 RID: 3479
		DefaultKeySet = 0,
		// Token: 0x04000D98 RID: 3480
		UserKeySet = 1,
		// Token: 0x04000D99 RID: 3481
		MachineKeySet = 2,
		// Token: 0x04000D9A RID: 3482
		Exportable = 4,
		// Token: 0x04000D9B RID: 3483
		UserProtected = 8,
		// Token: 0x04000D9C RID: 3484
		PersistKeySet = 16,
		// Token: 0x04000D9D RID: 3485
		EphemeralKeySet = 32
	}
}
