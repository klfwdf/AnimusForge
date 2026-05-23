using System;

namespace System.Security.Permissions
{
	// Token: 0x020002E9 RID: 745
	[Serializable]
	internal enum BuiltInPermissionFlag
	{
		// Token: 0x04000EB5 RID: 3765
		EnvironmentPermission = 1,
		// Token: 0x04000EB6 RID: 3766
		FileDialogPermission,
		// Token: 0x04000EB7 RID: 3767
		FileIOPermission = 4,
		// Token: 0x04000EB8 RID: 3768
		IsolatedStorageFilePermission = 8,
		// Token: 0x04000EB9 RID: 3769
		ReflectionPermission = 16,
		// Token: 0x04000EBA RID: 3770
		RegistryPermission = 32,
		// Token: 0x04000EBB RID: 3771
		SecurityPermission = 64,
		// Token: 0x04000EBC RID: 3772
		UIPermission = 128,
		// Token: 0x04000EBD RID: 3773
		PrincipalPermission = 256,
		// Token: 0x04000EBE RID: 3774
		PublisherIdentityPermission = 512,
		// Token: 0x04000EBF RID: 3775
		SiteIdentityPermission = 1024,
		// Token: 0x04000EC0 RID: 3776
		StrongNameIdentityPermission = 2048,
		// Token: 0x04000EC1 RID: 3777
		UrlIdentityPermission = 4096,
		// Token: 0x04000EC2 RID: 3778
		ZoneIdentityPermission = 8192,
		// Token: 0x04000EC3 RID: 3779
		KeyContainerPermission = 16384
	}
}
