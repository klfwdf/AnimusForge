using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002E1 RID: 737
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileIOPermissionAccess
	{
		// Token: 0x04000E7E RID: 3710
		NoAccess = 0,
		// Token: 0x04000E7F RID: 3711
		Read = 1,
		// Token: 0x04000E80 RID: 3712
		Write = 2,
		// Token: 0x04000E81 RID: 3713
		Append = 4,
		// Token: 0x04000E82 RID: 3714
		PathDiscovery = 8,
		// Token: 0x04000E83 RID: 3715
		AllAccess = 15
	}
}
