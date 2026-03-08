using System;

namespace System.Security.AccessControl
{
	// Token: 0x02000218 RID: 536
	[Flags]
	public enum FileSystemRights
	{
		// Token: 0x04000B30 RID: 2864
		ReadData = 1,
		// Token: 0x04000B31 RID: 2865
		ListDirectory = 1,
		// Token: 0x04000B32 RID: 2866
		WriteData = 2,
		// Token: 0x04000B33 RID: 2867
		CreateFiles = 2,
		// Token: 0x04000B34 RID: 2868
		AppendData = 4,
		// Token: 0x04000B35 RID: 2869
		CreateDirectories = 4,
		// Token: 0x04000B36 RID: 2870
		ReadExtendedAttributes = 8,
		// Token: 0x04000B37 RID: 2871
		WriteExtendedAttributes = 16,
		// Token: 0x04000B38 RID: 2872
		ExecuteFile = 32,
		// Token: 0x04000B39 RID: 2873
		Traverse = 32,
		// Token: 0x04000B3A RID: 2874
		DeleteSubdirectoriesAndFiles = 64,
		// Token: 0x04000B3B RID: 2875
		ReadAttributes = 128,
		// Token: 0x04000B3C RID: 2876
		WriteAttributes = 256,
		// Token: 0x04000B3D RID: 2877
		Delete = 65536,
		// Token: 0x04000B3E RID: 2878
		ReadPermissions = 131072,
		// Token: 0x04000B3F RID: 2879
		ChangePermissions = 262144,
		// Token: 0x04000B40 RID: 2880
		TakeOwnership = 524288,
		// Token: 0x04000B41 RID: 2881
		Synchronize = 1048576,
		// Token: 0x04000B42 RID: 2882
		FullControl = 2032127,
		// Token: 0x04000B43 RID: 2883
		Read = 131209,
		// Token: 0x04000B44 RID: 2884
		ReadAndExecute = 131241,
		// Token: 0x04000B45 RID: 2885
		Write = 278,
		// Token: 0x04000B46 RID: 2886
		Modify = 197055
	}
}
