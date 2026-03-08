using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002E4 RID: 740
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum HostProtectionResource
	{
		// Token: 0x04000E93 RID: 3731
		None = 0,
		// Token: 0x04000E94 RID: 3732
		Synchronization = 1,
		// Token: 0x04000E95 RID: 3733
		SharedState = 2,
		// Token: 0x04000E96 RID: 3734
		ExternalProcessMgmt = 4,
		// Token: 0x04000E97 RID: 3735
		SelfAffectingProcessMgmt = 8,
		// Token: 0x04000E98 RID: 3736
		ExternalThreading = 16,
		// Token: 0x04000E99 RID: 3737
		SelfAffectingThreading = 32,
		// Token: 0x04000E9A RID: 3738
		SecurityInfrastructure = 64,
		// Token: 0x04000E9B RID: 3739
		UI = 128,
		// Token: 0x04000E9C RID: 3740
		MayLeakOnAbort = 256,
		// Token: 0x04000E9D RID: 3741
		All = 511
	}
}
