using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002DF RID: 735
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum FileDialogPermissionAccess
	{
		// Token: 0x04000E78 RID: 3704
		None = 0,
		// Token: 0x04000E79 RID: 3705
		Open = 1,
		// Token: 0x04000E7A RID: 3706
		Save = 2,
		// Token: 0x04000E7B RID: 3707
		OpenSave = 3
	}
}
