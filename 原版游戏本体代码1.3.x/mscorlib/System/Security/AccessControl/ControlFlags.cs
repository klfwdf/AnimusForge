using System;

namespace System.Security.AccessControl
{
	// Token: 0x02000237 RID: 567
	[Flags]
	public enum ControlFlags
	{
		// Token: 0x04000BB1 RID: 2993
		None = 0,
		// Token: 0x04000BB2 RID: 2994
		OwnerDefaulted = 1,
		// Token: 0x04000BB3 RID: 2995
		GroupDefaulted = 2,
		// Token: 0x04000BB4 RID: 2996
		DiscretionaryAclPresent = 4,
		// Token: 0x04000BB5 RID: 2997
		DiscretionaryAclDefaulted = 8,
		// Token: 0x04000BB6 RID: 2998
		SystemAclPresent = 16,
		// Token: 0x04000BB7 RID: 2999
		SystemAclDefaulted = 32,
		// Token: 0x04000BB8 RID: 3000
		DiscretionaryAclUntrusted = 64,
		// Token: 0x04000BB9 RID: 3001
		ServerSecurity = 128,
		// Token: 0x04000BBA RID: 3002
		DiscretionaryAclAutoInheritRequired = 256,
		// Token: 0x04000BBB RID: 3003
		SystemAclAutoInheritRequired = 512,
		// Token: 0x04000BBC RID: 3004
		DiscretionaryAclAutoInherited = 1024,
		// Token: 0x04000BBD RID: 3005
		SystemAclAutoInherited = 2048,
		// Token: 0x04000BBE RID: 3006
		DiscretionaryAclProtected = 4096,
		// Token: 0x04000BBF RID: 3007
		SystemAclProtected = 8192,
		// Token: 0x04000BC0 RID: 3008
		RMControlValid = 16384,
		// Token: 0x04000BC1 RID: 3009
		SelfRelative = 32768
	}
}
