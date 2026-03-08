using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000314 RID: 788
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum KeyContainerPermissionFlags
	{
		// Token: 0x04000F5F RID: 3935
		NoFlags = 0,
		// Token: 0x04000F60 RID: 3936
		Create = 1,
		// Token: 0x04000F61 RID: 3937
		Open = 2,
		// Token: 0x04000F62 RID: 3938
		Delete = 4,
		// Token: 0x04000F63 RID: 3939
		Import = 16,
		// Token: 0x04000F64 RID: 3940
		Export = 32,
		// Token: 0x04000F65 RID: 3941
		Sign = 256,
		// Token: 0x04000F66 RID: 3942
		Decrypt = 512,
		// Token: 0x04000F67 RID: 3943
		ViewAcl = 4096,
		// Token: 0x04000F68 RID: 3944
		ChangeAcl = 8192,
		// Token: 0x04000F69 RID: 3945
		AllFlags = 13111
	}
}
