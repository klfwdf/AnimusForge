using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002EA RID: 746
	[ComVisible(true)]
	[Serializable]
	public enum IsolatedStorageContainment
	{
		// Token: 0x04000EC5 RID: 3781
		None,
		// Token: 0x04000EC6 RID: 3782
		DomainIsolationByUser = 16,
		// Token: 0x04000EC7 RID: 3783
		ApplicationIsolationByUser = 21,
		// Token: 0x04000EC8 RID: 3784
		AssemblyIsolationByUser = 32,
		// Token: 0x04000EC9 RID: 3785
		DomainIsolationByMachine = 48,
		// Token: 0x04000ECA RID: 3786
		AssemblyIsolationByMachine = 64,
		// Token: 0x04000ECB RID: 3787
		ApplicationIsolationByMachine = 69,
		// Token: 0x04000ECC RID: 3788
		DomainIsolationByRoamingUser = 80,
		// Token: 0x04000ECD RID: 3789
		AssemblyIsolationByRoamingUser = 96,
		// Token: 0x04000ECE RID: 3790
		ApplicationIsolationByRoamingUser = 101,
		// Token: 0x04000ECF RID: 3791
		AdministerIsolatedStorageByUser = 112,
		// Token: 0x04000ED0 RID: 3792
		UnrestrictedIsolatedStorage = 240
	}
}
