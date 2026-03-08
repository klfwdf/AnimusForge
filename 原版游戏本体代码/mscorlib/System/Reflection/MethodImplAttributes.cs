using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000607 RID: 1543
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum MethodImplAttributes
	{
		// Token: 0x04001D90 RID: 7568
		[__DynamicallyInvokable]
		CodeTypeMask = 3,
		// Token: 0x04001D91 RID: 7569
		[__DynamicallyInvokable]
		IL = 0,
		// Token: 0x04001D92 RID: 7570
		[__DynamicallyInvokable]
		Native,
		// Token: 0x04001D93 RID: 7571
		[__DynamicallyInvokable]
		OPTIL,
		// Token: 0x04001D94 RID: 7572
		[__DynamicallyInvokable]
		Runtime,
		// Token: 0x04001D95 RID: 7573
		[__DynamicallyInvokable]
		ManagedMask,
		// Token: 0x04001D96 RID: 7574
		[__DynamicallyInvokable]
		Unmanaged = 4,
		// Token: 0x04001D97 RID: 7575
		[__DynamicallyInvokable]
		Managed = 0,
		// Token: 0x04001D98 RID: 7576
		[__DynamicallyInvokable]
		ForwardRef = 16,
		// Token: 0x04001D99 RID: 7577
		[__DynamicallyInvokable]
		PreserveSig = 128,
		// Token: 0x04001D9A RID: 7578
		[__DynamicallyInvokable]
		InternalCall = 4096,
		// Token: 0x04001D9B RID: 7579
		[__DynamicallyInvokable]
		Synchronized = 32,
		// Token: 0x04001D9C RID: 7580
		[__DynamicallyInvokable]
		NoInlining = 8,
		// Token: 0x04001D9D RID: 7581
		[ComVisible(false)]
		[__DynamicallyInvokable]
		AggressiveInlining = 256,
		// Token: 0x04001D9E RID: 7582
		[__DynamicallyInvokable]
		NoOptimization = 64,
		// Token: 0x04001D9F RID: 7583
		SecurityMitigations = 1024,
		// Token: 0x04001DA0 RID: 7584
		MaxMethodImplVal = 65535
	}
}
