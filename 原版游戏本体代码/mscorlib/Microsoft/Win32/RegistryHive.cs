using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	// Token: 0x02000011 RID: 17
	[ComVisible(true)]
	[Serializable]
	public enum RegistryHive
	{
		// Token: 0x04000179 RID: 377
		ClassesRoot = -2147483648,
		// Token: 0x0400017A RID: 378
		CurrentUser,
		// Token: 0x0400017B RID: 379
		LocalMachine,
		// Token: 0x0400017C RID: 380
		Users,
		// Token: 0x0400017D RID: 381
		PerformanceData,
		// Token: 0x0400017E RID: 382
		CurrentConfig,
		// Token: 0x0400017F RID: 383
		DynData
	}
}
