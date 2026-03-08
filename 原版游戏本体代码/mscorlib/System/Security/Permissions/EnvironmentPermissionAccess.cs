using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002DC RID: 732
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum EnvironmentPermissionAccess
	{
		// Token: 0x04000E70 RID: 3696
		NoAccess = 0,
		// Token: 0x04000E71 RID: 3697
		Read = 1,
		// Token: 0x04000E72 RID: 3698
		Write = 2,
		// Token: 0x04000E73 RID: 3699
		AllAccess = 3
	}
}
