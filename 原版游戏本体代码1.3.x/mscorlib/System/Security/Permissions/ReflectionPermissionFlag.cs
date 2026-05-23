using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000302 RID: 770
	[ComVisible(true)]
	[Flags]
	[Serializable]
	public enum ReflectionPermissionFlag
	{
		// Token: 0x04000F17 RID: 3863
		NoFlags = 0,
		// Token: 0x04000F18 RID: 3864
		[Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
		TypeInformation = 1,
		// Token: 0x04000F19 RID: 3865
		MemberAccess = 2,
		// Token: 0x04000F1A RID: 3866
		[Obsolete("This permission is no longer used by the CLR.")]
		ReflectionEmit = 4,
		// Token: 0x04000F1B RID: 3867
		[ComVisible(false)]
		RestrictedMemberAccess = 8,
		// Token: 0x04000F1C RID: 3868
		[Obsolete("This permission has been deprecated. Use PermissionState.Unrestricted to get full access.")]
		AllFlags = 7
	}
}
