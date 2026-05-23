using System;

namespace System
{
	// Token: 0x02000086 RID: 134
	internal enum DelegateBindingFlags
	{
		// Token: 0x040002F8 RID: 760
		StaticMethodOnly = 1,
		// Token: 0x040002F9 RID: 761
		InstanceMethodOnly,
		// Token: 0x040002FA RID: 762
		OpenDelegateOnly = 4,
		// Token: 0x040002FB RID: 763
		ClosedDelegateOnly = 8,
		// Token: 0x040002FC RID: 764
		NeverCloseOverNull = 16,
		// Token: 0x040002FD RID: 765
		CaselessMatching = 32,
		// Token: 0x040002FE RID: 766
		SkipSecurityChecks = 64,
		// Token: 0x040002FF RID: 767
		RelaxedSignature = 128
	}
}
