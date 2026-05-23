using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000604 RID: 1540
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum MethodAttributes
	{
		// Token: 0x04001D69 RID: 7529
		[__DynamicallyInvokable]
		MemberAccessMask = 7,
		// Token: 0x04001D6A RID: 7530
		[__DynamicallyInvokable]
		PrivateScope = 0,
		// Token: 0x04001D6B RID: 7531
		[__DynamicallyInvokable]
		Private = 1,
		// Token: 0x04001D6C RID: 7532
		[__DynamicallyInvokable]
		FamANDAssem = 2,
		// Token: 0x04001D6D RID: 7533
		[__DynamicallyInvokable]
		Assembly = 3,
		// Token: 0x04001D6E RID: 7534
		[__DynamicallyInvokable]
		Family = 4,
		// Token: 0x04001D6F RID: 7535
		[__DynamicallyInvokable]
		FamORAssem = 5,
		// Token: 0x04001D70 RID: 7536
		[__DynamicallyInvokable]
		Public = 6,
		// Token: 0x04001D71 RID: 7537
		[__DynamicallyInvokable]
		Static = 16,
		// Token: 0x04001D72 RID: 7538
		[__DynamicallyInvokable]
		Final = 32,
		// Token: 0x04001D73 RID: 7539
		[__DynamicallyInvokable]
		Virtual = 64,
		// Token: 0x04001D74 RID: 7540
		[__DynamicallyInvokable]
		HideBySig = 128,
		// Token: 0x04001D75 RID: 7541
		[__DynamicallyInvokable]
		CheckAccessOnOverride = 512,
		// Token: 0x04001D76 RID: 7542
		[__DynamicallyInvokable]
		VtableLayoutMask = 256,
		// Token: 0x04001D77 RID: 7543
		[__DynamicallyInvokable]
		ReuseSlot = 0,
		// Token: 0x04001D78 RID: 7544
		[__DynamicallyInvokable]
		NewSlot = 256,
		// Token: 0x04001D79 RID: 7545
		[__DynamicallyInvokable]
		Abstract = 1024,
		// Token: 0x04001D7A RID: 7546
		[__DynamicallyInvokable]
		SpecialName = 2048,
		// Token: 0x04001D7B RID: 7547
		[__DynamicallyInvokable]
		PinvokeImpl = 8192,
		// Token: 0x04001D7C RID: 7548
		[__DynamicallyInvokable]
		UnmanagedExport = 8,
		// Token: 0x04001D7D RID: 7549
		[__DynamicallyInvokable]
		RTSpecialName = 4096,
		// Token: 0x04001D7E RID: 7550
		ReservedMask = 53248,
		// Token: 0x04001D7F RID: 7551
		[__DynamicallyInvokable]
		HasSecurity = 16384,
		// Token: 0x04001D80 RID: 7552
		[__DynamicallyInvokable]
		RequireSecObject = 32768
	}
}
