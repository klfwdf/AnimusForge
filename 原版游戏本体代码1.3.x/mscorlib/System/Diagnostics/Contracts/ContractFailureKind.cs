using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000416 RID: 1046
	[__DynamicallyInvokable]
	public enum ContractFailureKind
	{
		// Token: 0x04001712 RID: 5906
		[__DynamicallyInvokable]
		Precondition,
		// Token: 0x04001713 RID: 5907
		[__DynamicallyInvokable]
		Postcondition,
		// Token: 0x04001714 RID: 5908
		[__DynamicallyInvokable]
		PostconditionOnException,
		// Token: 0x04001715 RID: 5909
		[__DynamicallyInvokable]
		Invariant,
		// Token: 0x04001716 RID: 5910
		[__DynamicallyInvokable]
		Assert,
		// Token: 0x04001717 RID: 5911
		[__DynamicallyInvokable]
		Assume
	}
}
