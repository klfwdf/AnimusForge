using System;

namespace System.Security.AccessControl
{
	// Token: 0x0200022C RID: 556
	[Flags]
	public enum RegistryRights
	{
		// Token: 0x04000B92 RID: 2962
		QueryValues = 1,
		// Token: 0x04000B93 RID: 2963
		SetValue = 2,
		// Token: 0x04000B94 RID: 2964
		CreateSubKey = 4,
		// Token: 0x04000B95 RID: 2965
		EnumerateSubKeys = 8,
		// Token: 0x04000B96 RID: 2966
		Notify = 16,
		// Token: 0x04000B97 RID: 2967
		CreateLink = 32,
		// Token: 0x04000B98 RID: 2968
		ExecuteKey = 131097,
		// Token: 0x04000B99 RID: 2969
		ReadKey = 131097,
		// Token: 0x04000B9A RID: 2970
		WriteKey = 131078,
		// Token: 0x04000B9B RID: 2971
		Delete = 65536,
		// Token: 0x04000B9C RID: 2972
		ReadPermissions = 131072,
		// Token: 0x04000B9D RID: 2973
		ChangePermissions = 262144,
		// Token: 0x04000B9E RID: 2974
		TakeOwnership = 524288,
		// Token: 0x04000B9F RID: 2975
		FullControl = 983103
	}
}
