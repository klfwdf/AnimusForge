using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000306 RID: 774
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum SecurityPermissionFlag
	{
		// Token: 0x04000F25 RID: 3877
		NoFlags = 0,
		// Token: 0x04000F26 RID: 3878
		Assertion = 1,
		// Token: 0x04000F27 RID: 3879
		UnmanagedCode = 2,
		// Token: 0x04000F28 RID: 3880
		SkipVerification = 4,
		// Token: 0x04000F29 RID: 3881
		Execution = 8,
		// Token: 0x04000F2A RID: 3882
		ControlThread = 16,
		// Token: 0x04000F2B RID: 3883
		ControlEvidence = 32,
		// Token: 0x04000F2C RID: 3884
		ControlPolicy = 64,
		// Token: 0x04000F2D RID: 3885
		SerializationFormatter = 128,
		// Token: 0x04000F2E RID: 3886
		ControlDomainPolicy = 256,
		// Token: 0x04000F2F RID: 3887
		ControlPrincipal = 512,
		// Token: 0x04000F30 RID: 3888
		ControlAppDomain = 1024,
		// Token: 0x04000F31 RID: 3889
		RemotingConfiguration = 2048,
		// Token: 0x04000F32 RID: 3890
		Infrastructure = 4096,
		// Token: 0x04000F33 RID: 3891
		BindingRedirects = 8192,
		// Token: 0x04000F34 RID: 3892
		AllFlags = 16383
	}
}
