using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x0200031A RID: 794
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum RegistryPermissionAccess
	{
		// Token: 0x04000F79 RID: 3961
		NoAccess = 0,
		// Token: 0x04000F7A RID: 3962
		Read = 1,
		// Token: 0x04000F7B RID: 3963
		Write = 2,
		// Token: 0x04000F7C RID: 3964
		Create = 4,
		// Token: 0x04000F7D RID: 3965
		AllAccess = 7
	}
}
